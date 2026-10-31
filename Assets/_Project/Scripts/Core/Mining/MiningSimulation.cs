using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>
    /// O loop de mineração da descida (M6, D-52): o anão corre até o primeiro cristal da fila, bate a cada intervalo,
    /// quebra, soma lascas e corre para o próximo; sem cristal, continua correndo (D-55). Cristais nascem a cada
    /// intervalo no fim da fila, até o máximo (D-54, D-60). Desde o M3, dano, intervalo, corrida e lascas vêm dos
    /// atributos (<see cref="MinerStats"/>), que as compras (<see cref="Purchase"/>) melhoram. Desde o M5: o toque do
    /// jogador (<see cref="Tap"/>), o crítico e o combo (<see cref="StrikeRules"/>). Desde o M8: as cavernas e a parede da
    /// passagem (<see cref="PassCave"/>). (Até o M5, o anão andava num mapa aberto; esse código está na tag
    /// <c>v0.5-campo-aberto</c>.)
    ///
    /// O tempo é do código (Arquitetura Técnica, D-T2): <see cref="Tick"/> avança de evento em evento (chegada, golpe,
    /// nascimento), então um <c>Tick(600)</c> dá o mesmo resultado que 36.000 <c>Tick(1/60)</c>. É o que permite o
    /// ×20 do debug, o simulador e, no futuro, o progresso offline.
    /// </summary>
    public sealed class MiningSimulation
    {
        /// <summary>Folga numérica para "chegou" e "deu a hora": evita ficar preso a 1e-15 de um evento.</summary>
        const double Epsilon = 1e-9;

        /// <summary>Trava contra laço infinito por bug: 1 h de jogo tem poucos milhares de eventos.</summary>
        const int MaxStepsPerTick = 10_000_000;

        readonly MiningRules _rules;
        readonly CrystalTrack _track;
        readonly Miner _miner;
        readonly GameState _state;
        readonly EconomyRules _economy;
        readonly StrikeRules _strike;
        readonly IRandom _critRandom;

        MinerStats _stats;
        double _spawnTimer;
        int _combo;
        double _lastTapTime;

        /// <param name="random">
        /// Sorteio do crítico, o único da partida: desde a fila (M6) os cristais nascem em lugares fixos, então com ou sem
        /// toques a fila é a mesma.
        /// </param>
        /// <param name="economy">Melhorias à venda. Sem ela, nenhuma linha vende e os atributos são os valores base (o jogo do M2).</param>
        /// <param name="strike">Crítico, toque e combo. Sem elas, não há crítico (o jogo do M2–M4).</param>
        public MiningSimulation(MiningRules rules, IRandom random, GameState state = null, EconomyRules economy = null,
                                StrikeRules strike = null)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _critRandom = random ?? throw new ArgumentNullException(nameof(random));
            _strike = strike ?? StrikeRules.NoCritical();
            _state = state ?? new GameState();
            // Sem economia, o mínimo do intervalo é o próprio intervalo base: os atributos ficam iguais aos valores base.
            _economy = economy ?? EconomyRules.None(minHitInterval: rules.HitInterval);
            _stats = MinerStats.Compute(_rules, _economy, _state);
            _miner = new Miner();
            _track = new CrystalTrack(rules);
            _track.SetCrystalValues(rules.CrystalHealthIn(_state.Cave), rules.CrystalRewardIn(_state.Cave));
            _track.SpawnInitial(_miner.Distance);
        }

        /// <summary>
        /// Um cristal nasceu. Os do início já existem ao construir: quem desenha lê <see cref="Crystals"/> primeiro. A parede
        /// da passagem (M8) também avisa por aqui (<see cref="Crystal.IsWall"/>).
        /// </summary>
        public event Action<Crystal> CrystalSpawned;
        /// <summary>O alvo do anão mudou (<c>null</c> = ficou sem alvo).</summary>
        public event Action<Crystal> TargetChanged;
        /// <summary>Golpe no cristal: automático ou toque, com o dano e se foi crítico.</summary>
        public event Action<MiningHit> HitLanded;
        /// <summary>
        /// O cristal quebrou e saiu do mapa; a recompensa já foi somada. A parede (M8) também: sem recompensa, e a caverna
        /// muda logo depois (<see cref="CaveChanged"/>).
        /// </summary>
        public event Action<Crystal> CrystalBroken;
        /// <summary>O total de lascas mudou.</summary>
        public event Action<double> ShardsChanged;
        /// <summary>Uma melhoria foi comprada, com o nível novo.</summary>
        public event Action<UpgradeId, int> UpgradePurchased;
        /// <summary>Os atributos do anão mudaram (DANO e VELOCIDADE do HUD).</summary>
        public event Action<MinerStats> StatsChanged;
        /// <summary>O combo mudou (0 = zerou depois da janela sem toque).</summary>
        public event Action<int> ComboChanged;
        /// <summary>O combo chegou a um múltiplo de 10 (o "pop" especial).</summary>
        public event Action<int> ComboMilestone;
        /// <summary>
        /// A caverna ou o progresso da trava do Letrado mudou fora de uma quebra (M8): passagem de caverna e atalhos do
        /// debug. A quebra comum já avisa por <see cref="CrystalBroken"/>.
        /// </summary>
        public event Action<int> CaveChanged;
        /// <summary>
        /// A última parede caiu (M8, E3; D-112): o coração da montanha. Sai uma vez por partida; o jogo segue na última
        /// caverna, sem passagem.
        /// </summary>
        public event Action HeartReached;

        public MiningRules Rules => _rules;
        public EconomyRules Economy => _economy;
        public StrikeRules Strike => _strike;

        /// <summary>Toques seguidos, cada um a menos de <see cref="StrikeRules.ComboWindow"/> do anterior.</summary>
        public int Combo => _combo;

        /// <summary>Atalho do debug: todo golpe e todo toque saem críticos.</summary>
        public bool ForceCritical { get; set; }

        /// <summary>Atributos atuais do anão: valores base + melhorias.</summary>
        public MinerStats Stats => _stats;
        public GameState State => _state;
        public Miner Miner => _miner;

        /// <summary>Cristais inteiros na fila, do primeiro (o alvo) ao último; a parede de pé, se houver, é o último.</summary>
        public IReadOnlyList<Crystal> Crystals => _track.Crystals;

        /// <summary>A parede da passagem de pé (M8, E2), ou <c>null</c>.</summary>
        public Crystal Wall => _track.Wall;

        /// <summary>Segundos de jogo simulados.</summary>
        public double Elapsed { get; private set; }

        /// <summary>Avança <paramref name="dt"/> segundos de jogo.</summary>
        public void Tick(double dt)
        {
            if (double.IsNaN(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt), "O tempo não anda para trás.");

            double remaining = dt;
            int steps = 0;
            while (true)
            {
                ProcessDueEvents();
                if (remaining <= 0) break;

                double step = Math.Min(remaining, TimeToNextEvent());
                Advance(step);
                remaining -= step;

                if (++steps > MaxStepsPerTick)
                    throw new InvalidOperationException("MiningSimulation.Tick não avança: o laço de eventos travou.");
            }
        }

        /// <summary>
        /// Cria um cristal na distância dada da trilha, no seu lugar da fila, ignorando espaço e máximo (testes e debug).
        /// Um cristal posto à frente do alvo atual não troca o alvo: ele só muda depois de quebrar.
        /// </summary>
        public Crystal SpawnCrystalAt(double distance)
        {
            var crystal = _track.SpawnAt(distance);
            CrystalSpawned?.Invoke(crystal);
            return crystal;
        }

        /// <summary>
        /// Atalho do debug: um cristal agora, no fim da fila, sem esperar o relógio e ignorando o máximo. Com a parede de pé,
        /// nada nasce (<c>null</c>): o cristal ficaria depois dela.
        /// </summary>
        public Crystal SpawnCrystalNow()
        {
            if (_track.Wall != null) return null;
            var crystal = _track.TrySpawn(_miner.Distance, ignoreMax: true);
            CrystalSpawned?.Invoke(crystal);
            return crystal;
        }

        /// <summary>
        /// Atalho do debug: quebra o alvo atual na hora, pelo mesmo caminho de uma quebra normal (paga a recompensa, conta
        /// a quebra, mas não conta golpe). O próximo alvo é escolhido no próximo <see cref="Tick"/>. <c>false</c> sem alvo.
        /// </summary>
        public bool BreakTarget()
        {
            var target = _miner.Target;
            if (target == null) return false;

            target.ApplyDamage(target.Health);
            Break(target);
            return true;
        }

        /// <summary>Atalho do debug: soma lascas sem quebrar cristal.</summary>
        public void AddShards(double amount)
        {
            if (amount < 0 || double.IsNaN(amount)) throw new ArgumentOutOfRangeException(nameof(amount));

            _state.AddShards(amount);
            ShardsChanged?.Invoke(_state.Shards);
        }

        /// <summary>
        /// Compra a próxima melhoria da linha (REQ-ECO-08). Se der certo, os atributos novos valem a partir de agora:
        /// golpe com intervalo menor sai no próximo <see cref="Tick"/> se o tempo acumulado já passou dele; caminhada mais
        /// rápida vale do ponto onde o anão está. Sem compra, nada muda e nenhum evento sai.
        /// </summary>
        public PurchaseResult Purchase(UpgradeId id)
        {
            var result = Purchases.TryPurchase(_state, _economy, id, _rules);
            if (result != PurchaseResult.Purchased) return result;

            _stats = MinerStats.Compute(_rules, _economy, _state);
            UpgradePurchased?.Invoke(id, _state.LevelOf(id));
            StatsChanged?.Invoke(_stats);
            ShardsChanged?.Invoke(_state.Shards);
            return result;
        }

        /// <summary>
        /// Toque do jogador (REQ-MIN-02, D-47, D-48): soma 1 no combo e, se o anão tem alvo (batendo ou correndo até ele),
        /// tira dele 10% de um golpe (com a Picareta Dupla), podendo ser crítico. Quebra pelo toque paga como qualquer
        /// quebra. Sem alvo, só o combo conta.
        /// </summary>
        public void Tap()
        {
            _combo++;
            _lastTapTime = Elapsed;
            ComboChanged?.Invoke(_combo);
            if (_combo % 10 == 0) ComboMilestone?.Invoke(_combo);

            var target = _miner.Target;
            if (target == null) return;

            bool critical = RollCritical();
            double damage = _stats.Damage * _strike.TapFraction * (critical ? _strike.CritMultiplier : 1);
            bool broke = target.ApplyDamage(damage);
            HitLanded?.Invoke(new MiningHit(target, damage, isTap: true, isCritical: critical));
            if (broke) Break(target);
        }

        /// <summary>
        /// Se o botão de passar de caverna vale agora (M8, D-110): o Letrado da caverna atual comprado e nenhuma parede de
        /// pé. Na última caverna, o último Letrado leva à última parede, a do coração da montanha (D-112); depois dela, não
        /// há mais passagem.
        /// </summary>
        public bool CanPassCave =>
            _economy.Get(UpgradeId.Letrado).IsEnabled && _state.LevelOf(UpgradeId.Letrado) >= _state.Cave
            && !_state.ReachedHeart && _track.Wall == null;

        /// <summary>
        /// Começa a passagem para a próxima caverna (M8, E2): põe a parede no fim da fila, a
        /// <see cref="MiningRules.SpawnDistance"/> à frente do anão, onde os cristais nascem (D-107). Os cristais até ela o
        /// anão quebra normalmente; os de depois somem; nada nasce enquanto ela está de pé. A vida dela é a da D-98; ela não
        /// paga lascas nem conta nos 100 cristais (D-108). A caverna só muda quando ela quebra. A última parede (D-112) leva
        /// a vida da mesma fórmula, como se houvesse uma caverna a mais. <c>false</c> se não pode passar.
        /// </summary>
        public bool PassCave()
        {
            if (!CanPassCave) return false;

            var target = _miner.Target;
            double at = _miner.Distance + _rules.SpawnDistance;
            if (target != null) at = Math.Max(at, target.Distance + _rules.MinCrystalSpacing); // o alvo fica antes dela
            var wall = _track.PlaceWall(at, _rules.WallHealthIn(_state.Cave + 1), target);
            CrystalSpawned?.Invoke(wall);
            return true;
        }

        /// <summary>Atalho do debug: a trava do Letrado cumprida (os cristais da caverna, sem quebrar).</summary>
        public void FillCaveBreaks()
        {
            if (_state.CaveBreaks >= _rules.LetradoBreaks) return;
            _state.SetCaveBreaks(_rules.LetradoBreaks);
            CaveChanged?.Invoke(_state.Cave);
        }

        /// <summary>
        /// Atalho do debug: a próxima caverna agora, sem parede, como se tivesse comprado o Letrado (de graça) e passado.
        /// Uma parede de pé sai junto. Na última caverna, nada muda. <c>false</c> se não há caverna depois.
        /// </summary>
        public bool SkipToNextCave() => SkipToCave(_state.Cave + 1);

        /// <summary>
        /// Atalho do debug (M8, E3): direto para a caverna <paramref name="cave"/> (no máximo a última), com os Letrados das
        /// cavernas de antes de graça, sem parede. <c>false</c> se ela não fica mais abaixo.
        /// </summary>
        public bool SkipToCave(int cave)
        {
            cave = Math.Min(cave, _rules.CaveCount);
            if (cave <= _state.Cave) return false;
            while (_state.LevelOf(UpgradeId.Letrado) < cave - 1) _state.RaiseLevel(UpgradeId.Letrado);

            var wall = _track.Wall;
            if (wall != null)
            {
                _track.Remove(wall);
                if (_miner.Target == wall)
                {
                    _miner.Target = null;
                    _miner.State = MinerState.Running;
                    TargetChanged?.Invoke(null);
                }
            }
            EnterCave(cave);
            return true;
        }

        void EnterCave(int cave)
        {
            _state.EnterCave(cave);
            _track.SetCrystalValues(_rules.CrystalHealthIn(cave), _rules.CrystalRewardIn(cave));
            _stats = MinerStats.Compute(_rules, _economy, _state);
            StatsChanged?.Invoke(_stats);
            CaveChanged?.Invoke(cave);
        }

        bool RollCritical() =>
            ForceCritical || (_strike.CritChance > 0 && _critRandom.NextDouble() < _strike.CritChance);

        /// <summary>O que a loja mostra da linha agora: nível, custo, estado do botão e efeito "de → para".</summary>
        public UpgradePreview Preview(UpgradeId id) => UpgradePreview.For(_rules, _economy, _state, id);

        /// <summary>Resolve tudo o que vence agora, na ordem: nascimento, alvo (o primeiro da fila), golpe.</summary>
        void ProcessDueEvents()
        {
            bool changed;
            do
            {
                changed = false;

                if (_combo > 0 && Elapsed >= _lastTapTime + _strike.ComboWindow - Epsilon)
                {
                    _combo = 0;
                    ComboChanged?.Invoke(0);
                }

                bool canSpawn = _track.Wall == null; // parede de pé (D-107): nada nasce até ela cair
                if (canSpawn && _track.IsBelowMinimum)
                {
                    // Fila no mínimo (D-80): repõe na hora, sem mexer no relógio de nascimento.
                    var crystal = _track.TrySpawn(_miner.Distance);
                    CrystalSpawned?.Invoke(crystal);
                    changed = true;
                }
                else if (canSpawn && !_track.IsFull && _spawnTimer >= _rules.SpawnInterval - Epsilon)
                {
                    _spawnTimer = Math.Max(0, _spawnTimer - _rules.SpawnInterval);
                    // Nasce antes de avisar: com "?.Invoke(TrySpawn())", sem ninguém escutando, o nascimento seria pulado.
                    var crystal = _track.TrySpawn(_miner.Distance);
                    CrystalSpawned?.Invoke(crystal);
                    changed = true;
                }

                if (_miner.Target == null && _track.Front != null)
                {
                    StartTarget(_track.Front);
                    changed = true;
                }

                if (_miner.State == MinerState.Mining && _miner.HitTimer >= _stats.HitInterval - Epsilon)
                {
                    _miner.HitTimer = Math.Max(0, _miner.HitTimer - _stats.HitInterval);
                    Hit();
                    changed = true;
                }
            } while (changed);
        }

        double TimeToNextEvent()
        {
            double next = double.PositiveInfinity;

            if (!SpawnWaits)
                next = Math.Max(0, _rules.SpawnInterval - _spawnTimer);

            if (_combo > 0)
                next = Math.Min(next, Math.Max(0, _lastTapTime + _strike.ComboWindow - Elapsed));

            switch (_miner.State)
            {
                case MinerState.Running when _miner.Target != null:
                    next = Math.Min(next, Math.Max(0, ToGo() / _stats.WalkSpeed));
                    break;
                case MinerState.Mining:
                    next = Math.Min(next, Math.Max(0, _stats.HitInterval - _miner.HitTimer));
                    break;
            }

            return next;
        }

        void Advance(double step)
        {
            if (step <= 0) return;

            Elapsed += step;
            _state.AddPlayTime(step);

            // Fila cheia ou parede de pé: o relógio de nascimento espera no limite e nasce assim que der.
            _spawnTimer = SpawnWaits ? Math.Min(_spawnTimer + step, _rules.SpawnInterval) : _spawnTimer + step;

            switch (_miner.State)
            {
                case MinerState.Running:
                    Run(step);
                    break;
                case MinerState.Mining:
                    _miner.HitTimer += step;
                    break;
            }
        }

        /// <summary>O relógio de nascimento espera: fila cheia ou parede de pé.</summary>
        bool SpawnWaits => _track.IsFull || _track.Wall != null;

        /// <summary>Quanto falta correr até parar no alcance do alvo (negativo: já passou do ponto).</summary>
        double ToGo() => _miner.Target.Distance - _rules.MiningRange - _miner.Distance;

        void Run(double step)
        {
            double travel = _stats.WalkSpeed * step;
            if (_miner.Target == null)
            {
                _miner.Distance += travel; // sem cristal, a descida continua (D-55)
                return;
            }

            double toGo = ToGo();
            if (travel >= toGo - Epsilon)
            {
                // Chegou: para exatamente no alcance (nunca volta para trás, se o cristal foi posto atrás dele).
                _miner.Distance = Math.Max(_miner.Distance, _miner.Target.Distance - _rules.MiningRange);
                _miner.State = MinerState.Mining;
                _miner.HitTimer = 0;
            }
            else
            {
                _miner.Distance += travel;
            }
        }

        void StartTarget(Crystal target)
        {
            _miner.Target = target;
            _miner.HitTimer = 0;
            _miner.State = ToGo() <= Epsilon ? MinerState.Mining : MinerState.Running;
            TargetChanged?.Invoke(target);
        }

        void Hit()
        {
            var target = _miner.Target;
            bool critical = RollCritical();
            double damage = _stats.Damage * (critical ? _strike.CritMultiplier : 1);

            _state.RegisterHit();
            bool broke = target.ApplyDamage(damage);
            HitLanded?.Invoke(new MiningHit(target, damage, isTap: false, isCritical: critical));

            if (broke) Break(target);
        }

        /// <summary>
        /// A quebra de um cristal paga e conta; a da parede (M8) não, e leva à caverna seguinte ou, na última, ao coração
        /// da montanha (D-112).
        /// </summary>
        void Break(Crystal target)
        {
            _track.Remove(target);
            if (!target.IsWall) _state.RegisterBreak(target.Reward * _stats.RewardMultiplier);
            _miner.Target = null;
            _miner.State = MinerState.Running;

            CrystalBroken?.Invoke(target);
            if (!target.IsWall) ShardsChanged?.Invoke(_state.Shards);
            TargetChanged?.Invoke(null);
            if (!target.IsWall) return;
            if (_state.Cave < _rules.CaveCount)
            {
                EnterCave(_state.Cave + 1);
                return;
            }
            _state.ReachHeart();
            HeartReached?.Invoke();
        }
    }
}
