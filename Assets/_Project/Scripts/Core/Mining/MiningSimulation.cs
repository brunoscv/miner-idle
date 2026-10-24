using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>
    /// O loop de mineração da descida (M6, D-52): o anão corre até o primeiro cristal da fila, bate a cada intervalo,
    /// quebra, soma lascas e corre para o próximo; sem cristal, continua correndo (D-55). Cristais nascem a cada
    /// intervalo no fim da fila, até o máximo (D-54, D-60). Desde o M3, dano, intervalo, corrida e lascas vêm dos
    /// atributos (<see cref="MinerStats"/>), que as compras (<see cref="Purchase"/>) melhoram. Desde o M5: o toque do
    /// jogador (<see cref="Tap"/>), o crítico e o combo (<see cref="StrikeRules"/>). (Até o M5, o anão andava num mapa
    /// aberto; esse código está na tag <c>v0.5-campo-aberto</c>.)
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
            _track.SpawnInitial(_miner.Distance);
        }

        /// <summary>Um cristal nasceu. Os do início já existem ao construir: quem desenha lê <see cref="Crystals"/> primeiro.</summary>
        public event Action<Crystal> CrystalSpawned;
        /// <summary>O alvo do anão mudou (<c>null</c> = ficou sem alvo).</summary>
        public event Action<Crystal> TargetChanged;
        /// <summary>Golpe no cristal: automático ou toque, com o dano e se foi crítico.</summary>
        public event Action<MiningHit> HitLanded;
        /// <summary>O cristal quebrou e saiu do mapa; a recompensa já foi somada.</summary>
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

        /// <summary>Cristais inteiros na fila, do primeiro (o alvo) ao último.</summary>
        public IReadOnlyList<Crystal> Crystals => _track.Crystals;

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

        /// <summary>Atalho do debug: um cristal agora, no fim da fila, sem esperar o relógio e ignorando o máximo.</summary>
        public Crystal SpawnCrystalNow()
        {
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
            var result = Purchases.TryPurchase(_state, _economy, id);
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

                if (_track.IsBelowMinimum)
                {
                    // Fila no mínimo (D-80): repõe na hora, sem mexer no relógio de nascimento.
                    var crystal = _track.TrySpawn(_miner.Distance);
                    CrystalSpawned?.Invoke(crystal);
                    changed = true;
                }
                else if (!_track.IsFull && _spawnTimer >= _rules.SpawnInterval - Epsilon)
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

            if (!_track.IsFull)
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

            // Fila cheia: o relógio de nascimento espera no limite e nasce assim que alguém quebra.
            _spawnTimer = _track.IsFull ? Math.Min(_spawnTimer + step, _rules.SpawnInterval) : _spawnTimer + step;

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

        void Break(Crystal target)
        {
            _track.Remove(target);
            _state.RegisterBreak(target.Reward * _stats.RewardMultiplier);
            _miner.Target = null;
            _miner.State = MinerState.Running;

            CrystalBroken?.Invoke(target);
            ShardsChanged?.Invoke(_state.Shards);
            TargetChanged?.Invoke(null);
        }
    }
}
