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
    /// passagem (<see cref="PassCave"/>). Desde o M7: o estagiário, que bate no mesmo cristal do anão com o relógio dele e
    /// avança junto (<see cref="InternHitLanded"/>). (Até o M5, o anão andava num mapa aberto; esse código está na tag
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

        /// <summary>A janela do teto de toques (D-119), em segundos de jogo.</summary>
        const double TapRateWindow = 1;

        readonly MiningRules _rules;
        readonly CrystalTrack _track;
        readonly Miner _miner;
        readonly GameState _state;
        readonly EconomyRules _economy;
        readonly StrikeRules _strike;
        readonly IRandom _critRandom;
        readonly Permanents _permanents;
        bool _rested;

        MinerStats _stats;
        InternStats _internStats;
        // O relógio do golpe do estagiário (M7, D-168): conta enquanto o anão bate; correndo, só enche no modo A1.
        double _internTimer;
        long _internHits;
        double _spawnTimer;
        int _combo;
        double _lastTapTime;

        // D-126: se a picareta já desceu no alvo atual, e quanto falta do golpe depois que o toque quebrou o cristal.
        bool _swungAtTarget;
        double _recoveryLeft;

        // Instantes dos últimos toques que valeram (D-119), em anel: cheio, o cursor aponta o mais antigo.
        readonly double[] _tapTimes;
        int _tapCursor;
        int _tapCount;

        /// <param name="random">
        /// Sorteio do crítico, o único da partida: desde a fila (M6) os cristais nascem em lugares fixos, então com ou sem
        /// toques a fila é a mesma.
        /// </param>
        /// <param name="economy">Melhorias à venda. Sem ela, nenhuma linha vende e os atributos são os valores base (o jogo do M2).</param>
        /// <param name="strike">Crítico, toque e combo. Sem elas, não há crítico (o jogo do M2–M4).</param>
        /// <param name="permanents">
        /// O dia e as melhorias permanentes (Marco 8b), que passam de uma run para a outra. Sem elas, não há descanso nem
        /// permanentes (o jogo até o GDD v5).
        /// </param>
        public MiningSimulation(MiningRules rules, IRandom random, GameState state = null, EconomyRules economy = null,
                                StrikeRules strike = null, Permanents permanents = null)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _critRandom = random ?? throw new ArgumentNullException(nameof(random));
            _strike = strike ?? StrikeRules.NoCritical();
            _tapTimes = new double[_strike.MaxTapsPerSecond];
            _state = state ?? new GameState();
            // Sem economia, o mínimo do intervalo é o próprio intervalo base: os atributos ficam iguais aos valores base.
            _economy = economy ?? EconomyRules.None(minHitInterval: rules.HitInterval);
            _permanents = permanents ?? Permanents.None();
            _stats = ComputeStats();
            _internStats = ComputeInternStats();
            ArrivalStrike = rules.ArrivalStrike;
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
        /// <summary>
        /// Golpe do estagiário (M7) no alvo do anão: nunca crítico (D-167). Sai à parte do <see cref="HitLanded"/>, que
        /// continua só do anão e do toque: quem desenha o anão não confunde os dois.
        /// </summary>
        public event Action<MiningHit> InternHitLanded;
        /// <summary>O estagiário foi contratado, demitido ou mudou de atributos (compra, Treinar, atalhos do debug).</summary>
        public event Action<InternStats> InternChanged;
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
        /// <summary>
        /// O banco de lascas vermelhas ou os níveis das permanentes mudaram (Marco 8b): compra, atalhos do debug. O
        /// descanso não avisa por aqui: depois dele a run é outra (<see cref="Rest"/>).
        /// </summary>
        public event Action PermanentsChanged;

        /// <summary>A volta do offline esperando o COLETAR mudou: chegou uma, somou outra ou foi coletada (M9, E2).</summary>
        public event Action<OfflineReturn> OfflineReturnChanged;

        public MiningRules Rules => _rules;
        public EconomyRules Economy => _economy;
        public StrikeRules Strike => _strike;

        /// <summary>O dia e as melhorias permanentes (Marco 8b): os mesmos de uma run para a outra.</summary>
        public Permanents Permanents => _permanents;

        /// <summary>Toques seguidos, cada um a menos de <see cref="StrikeRules.ComboWindow"/> do anterior.</summary>
        public int Combo => _combo;

        /// <summary>Atalho do debug: todo golpe e todo toque saem críticos.</summary>
        public bool ForceCritical { get; set; }

        /// <summary>
        /// Quando sai o primeiro golpe num cristal (D-124). Começa no valor de <see cref="MiningRules.ArrivalStrike"/>; o DBG
        /// troca no meio da partida para comparar.
        /// </summary>
        public ArrivalStrike ArrivalStrike { get; set; }

        /// <summary>
        /// Segundos de jogo até o próximo golpe do anão, se nada mudar (sem toque nem compra): a apresentação começa a
        /// levantar a picareta antes dele, ainda correndo. Infinito sem alvo.
        /// </summary>
        public double SecondsToNextHit
        {
            get
            {
                if (_miner.Target == null) return double.PositiveInfinity;
                double charge = Math.Max(0, _stats.HitInterval - _miner.HitTimer);
                if (_miner.State == MinerState.Mining) return charge;

                // Terminando o golpe (D-126), ele só corre depois.
                double run = _recoveryLeft + Math.Max(0, ToGo()) / _stats.WalkSpeed;
                switch (ArrivalStrike)
                {
                    case ArrivalStrike.Immediate: return run;
                    case ArrivalStrike.Charge: return Math.Max(run, charge);
                    default: return run + _stats.HitInterval;
                }
            }
        }

        /// <summary>
        /// Quanto dura a volta da picareta agora (D-126): <see cref="MiningRules.SwingRecovery"/>, nunca mais que o
        /// intervalo do golpe. A apresentação toca o golpe nesse tempo, com o cristal vivo ou quebrado pelo toque.
        /// </summary>
        public double SwingRecovery => Math.Min(_rules.SwingRecovery, _stats.HitInterval);

        /// <summary>Atributos atuais do anão: valores base + melhorias.</summary>
        public MinerStats Stats => _stats;

        /// <summary>Atributos atuais do estagiário (M7): contratado ou não, dano, intervalo, lascas offline.</summary>
        public InternStats InternStats => _internStats;

        /// <summary>Se o estagiário está contratado nesta run.</summary>
        public bool IsInternHired => _internStats.IsHired;

        /// <summary>Segundos acumulados no relógio do golpe do estagiário (para a apresentação e os testes).</summary>
        public double InternHitTimer => _internTimer;

        /// <summary>Golpes do estagiário nesta partida (DBG → Estagiário; não vai para o save).</summary>
        public long InternHits => _internHits;
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
        /// O offline (M9, E1): o estagiário minerou sozinho por <paramref name="secondsAway"/> (até o teto das regras) no
        /// nível de agora (<see cref="OfflineMining"/>). As lascas entram no saldo e nas ganhas do dia; o nível, a trava do
        /// Mapa e a fila de cristais não mudam (D-189). Sem estagiário, ou com o relógio voltado, nada entra.
        ///
        /// E2 (D-195, D-197): com <paramref name="noticeSeconds"/> &gt; 0 e o tempo que contou a partir dele, as lascas não
        /// entram agora: esperam o COLETAR do modal "Bem-vindo de volta" (<see cref="PendingOffline"/>,
        /// <see cref="CollectOffline"/>). Abaixo dele, entram direto, sem aviso.
        /// </summary>
        public OfflineReport ApplyOffline(double secondsAway, double noticeSeconds = 0)
        {
            var report = OfflineMining.Compute(_rules, _economy, _state, _permanents, secondsAway);
            if (report.Shards <= 0) return report;
            if (noticeSeconds > 0 && report.SecondsCounted >= noticeSeconds)
            {
                _state.AddPendingOffline(report);
                OfflineReturnChanged?.Invoke(_state.PendingOffline);
                return report;
            }
            _state.AddOfflineShards(report.Shards);
            ShardsChanged?.Invoke(_state.Shards);
            return report;
        }

        /// <summary>A volta esperando o COLETAR (M9, E2); vazia quando não há.</summary>
        public OfflineReturn PendingOffline => _state.PendingOffline;

        /// <summary>
        /// COLETAR (D-197): as lascas da volta entram no saldo e nas ganhas do dia, uma vez só. Devolve quanto entrou (0 sem
        /// volta pendente).
        /// </summary>
        public double CollectOffline()
        {
            if (!_state.PendingOffline.HasShards) return 0;
            double shards = _state.CollectPendingOffline();
            OfflineReturnChanged?.Invoke(_state.PendingOffline);
            ShardsChanged?.Invoke(_state.Shards);
            return shards;
        }

        /// <summary>
        /// Compra a próxima melhoria da linha (REQ-ECO-08). Se der certo, os atributos novos valem a partir de agora:
        /// golpe com intervalo menor sai no próximo <see cref="Tick"/> se o tempo acumulado já passou dele; caminhada mais
        /// rápida vale do ponto onde o anão está. Sem compra, nada muda e nenhum evento sai.
        /// </summary>
        public PurchaseResult Purchase(UpgradeId id)
        {
            var result = Purchases.TryPurchase(_state, _economy, id, _rules, _permanents);
            if (result != PurchaseResult.Purchased) return result;

            _stats = ComputeStats();
            UpgradePurchased?.Invoke(id, _state.LevelOf(id));
            StatsChanged?.Invoke(_stats);
            ShardsChanged?.Invoke(_state.Shards);
            if (id == UpgradeId.Intern || EconomyRules.IsInternTree(id)) OnInternChanged();
            return result;
        }

        /// <summary>
        /// Toque do jogador (REQ-MIN-02, D-47, D-48): soma 1 no combo e, se o anão tem alvo (batendo ou correndo até ele),
        /// tira dele a fração de um golpe do <see cref="StrikeRules.TapFraction"/> (com a Picareta Dupla), podendo ser
        /// crítico. Quebra pelo toque paga como qualquer quebra. Sem alvo, ou com ele além do
        /// <see cref="MiningRules.TapReach"/> (ainda fora da tela, D-121), só o combo conta. Acima do teto de toques por
        /// segundo (D-119), o toque não vale nada: devolve <c>false</c> e nenhum evento sai.
        /// </summary>
        public bool Tap()
        {
            if (!TakeTapSlot()) return false;

            _combo++;
            _lastTapTime = Elapsed;
            ComboChanged?.Invoke(_combo);
            if (_combo % 10 == 0) ComboMilestone?.Invoke(_combo);

            var target = _miner.Target;
            if (target == null || target.Distance - _miner.Distance > _rules.TapReach + Epsilon) return true;

            bool critical = RollCritical(isTap: true);
            double damage = _stats.Damage * _strike.TapFraction * (critical ? _strike.CritMultiplier : 1);
            bool broke = target.ApplyDamage(damage);
            HitLanded?.Invoke(new MiningHit(target, damage, isTap: true, isCritical: critical));
            if (broke) Break(target, byTap: true);
            return true;
        }

        /// <summary>
        /// O teto da D-119: com <see cref="StrikeRules.MaxTapsPerSecond"/> toques valendo no último segundo de jogo, o
        /// próximo espera o mais antigo deles sair da janela. Toques no mesmo instante (vários dedos num quadro) contam
        /// um a um.
        /// </summary>
        bool TakeTapSlot()
        {
            int max = _tapTimes.Length;
            if (max == 0) return true;
            if (_tapCount == max && Elapsed < _tapTimes[_tapCursor] + TapRateWindow - Epsilon) return false;

            _tapTimes[_tapCursor] = Elapsed;
            _tapCursor = (_tapCursor + 1) % max;
            if (_tapCount < max) _tapCount++;
            return true;
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

        /// <summary>Os cristais que a trava do Mapa pede agora: os das regras, menos os da Memória (Marco 8b).</summary>
        public long MapLock => Purchases.MapLock(_rules, _permanents);

        /// <summary>Atalho do debug: a trava do Letrado cumprida (os cristais da caverna, sem quebrar).</summary>
        public void FillCaveBreaks()
        {
            long goal = MapLock;
            if (_state.CaveBreaks >= goal) return;
            _state.SetCaveBreaks(goal);
            CaveChanged?.Invoke(_state.Cave);
        }

        /// <summary>
        /// Se o botão Descansar vale agora (Marco 8b, D-152): o balanço tem descanso e a run chegou ao nível mínimo (GDD v7:
        /// 10). Depois de descansar, esta run acabou: não vale mais.
        /// </summary>
        public bool CanRest => !_rested && _permanents.Rules.IsRestEnabled && _state.Cave >= _permanents.Rules.RestLevel;

        /// <summary>Esta run já terminou num descanso (<see cref="Rest"/>); quem a dirige começa outra.</summary>
        public bool HasRested => _rested;

        /// <summary>As lascas vermelhas que o jogador leva se descansar agora (D-138: a tabela até o nível atual).</summary>
        public double PendingRedShards => _permanents.Pending(_state.Cave);

        /// <summary>Quantas lascas vermelhas o próximo nível somaria (o modal mostra, D-153); 0 no último nível.</summary>
        public double NextLevelRedShards =>
            _permanents.Rules.IsRestEnabled && _state.Cave < _rules.CaveCount ? _permanents.Rules.RedShards.At(_state.Cave + 1) : 0;

        /// <summary>
        /// Descansar (Marco 8b, D-137): as lascas vermelhas pendentes vão para o banco, o dia avança e esta run acaba (a
        /// simulação continua de pé, mas <see cref="CanRest"/> fica falso; a run nova sai de
        /// <see cref="Core.Permanents.NewRun"/>). <paramref name="force"/> = o atalho do debug, em qualquer nível. <c>null</c>
        /// se não dá para descansar.
        /// </summary>
        public RestSummary? Rest(bool force = false)
        {
            if (_rested || (!force && !CanRest)) return null;
            _rested = true;
            return _permanents.Rest(_state);
        }

        /// <summary>
        /// Compra o próximo nível da melhoria permanente com lascas vermelhas (Marco 8b). O efeito vale na hora (D-155):
        /// atributos recalculados e, para a Memória, a trava do Mapa (avisa por <see cref="CaveChanged"/>, como os atalhos).
        /// </summary>
        public PurchaseResult PurchasePermanent(PermanentId id)
        {
            var result = _permanents.TryPurchase(id);
            if (result == PurchaseResult.Purchased) OnPermanentsChanged();
            return result;
        }

        /// <summary>Atalho do debug: soma lascas vermelhas ao banco.</summary>
        public void AddRedShards(double amount)
        {
            if (amount < 0 || double.IsNaN(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
            _permanents.State.AddRedShards(amount);
            PermanentsChanged?.Invoke();
        }

        /// <summary>Atalho do debug: um nível de graça em cada permanente que ainda sobe. <c>false</c> se todas no limite.</summary>
        public bool GrantPermanentLevels()
        {
            if (!_permanents.GrantLevels()) return false;
            OnPermanentsChanged();
            return true;
        }

        /// <summary>Atalho do debug: banco, níveis e dia das permanentes do zero; a run continua.</summary>
        public void ResetPermanents()
        {
            _permanents.State.Clear();
            OnPermanentsChanged();
        }

        void OnPermanentsChanged()
        {
            _stats = ComputeStats();
            PermanentsChanged?.Invoke();
            StatsChanged?.Invoke(_stats);
            CaveChanged?.Invoke(_state.Cave); // a trava do Mapa pode ter mudado (Memória)
            OnInternChanged(); // o Treinar muda o dano dele
        }

        MinerStats ComputeStats() => MinerStats.Compute(_rules, _economy, _state, _permanents);

        InternStats ComputeInternStats() => InternStats.Compute(_rules, _economy, _state, _permanents);

        /// <summary>
        /// Recalcula o estagiário e avisa. Contratado agora, ele nasce no cristal atual do anão (§5.1): se o anão já está
        /// batendo, o relógio dele começa como numa chegada (D-168).
        /// </summary>
        void OnInternChanged()
        {
            bool wasHired = _internStats.IsHired;
            _internStats = ComputeInternStats();
            if (_internStats.IsHired && !wasHired)
            {
                _internTimer = 0;
                if (_miner.State == MinerState.Mining) InternArrives();
            }
            InternChanged?.Invoke(_internStats);
        }

        // ── Atalhos do debug do estagiário (M7, DBG → Estagiário) ───────────────────────────────────────────────

        /// <summary>Atalho do debug: contrata o estagiário de graça. <c>false</c> se já está contratado ou o balanço não o tem.</summary>
        public bool HireIntern()
        {
            if (!_economy.HasIntern || _internStats.IsHired) return false;
            _state.RaiseLevel(UpgradeId.Intern);
            OnInternChanged();
            return true;
        }

        /// <summary>Atalho do debug: demite o estagiário; a árvore dele volta ao zero. <c>false</c> se não estava contratado.</summary>
        public bool FireIntern()
        {
            if (_state.LevelOf(UpgradeId.Intern) == 0) return false;
            foreach (UpgradeId id in Enum.GetValues(typeof(UpgradeId)))
                if (id == UpgradeId.Intern || EconomyRules.IsInternTree(id)) _state.ResetLevel(id);
            _internTimer = 0;
            OnInternChanged();
            return true;
        }

        /// <summary>
        /// Atalho do debug: um nível de graça em cada linha da árvore do estagiário que ainda sobe; contrata antes, se
        /// preciso. <c>false</c> se nada mudou.
        /// </summary>
        public bool GrantInternLevels()
        {
            bool any = HireIntern();
            if (!_internStats.IsHired) return any;
            foreach (UpgradeId id in Enum.GetValues(typeof(UpgradeId)))
            {
                if (!EconomyRules.IsInternTree(id) || _economy.Get(id).IsMaxed(_state.LevelOf(id))) continue;
                _state.RaiseLevel(id);
                any = true;
            }
            if (any) OnInternChanged();
            return any;
        }

        /// <summary>Atalho do debug: +1 nível de graça na permanente (respeita o limite e a trava da Promoção).</summary>
        public bool GrantPermanentLevel(PermanentId id)
        {
            if (!_permanents.GrantLevel(id)) return false;
            OnPermanentsChanged();
            return true;
        }

        /// <summary>Atalho do debug: o Treinar até o limite e a Promoção, de graça. <c>false</c> se nada mudou.</summary>
        public bool PromoteIntern()
        {
            bool any = false;
            while (_permanents.GrantLevel(PermanentId.InternTraining)) any = true;
            any |= _permanents.GrantLevel(PermanentId.InternPromotion);
            if (any) OnPermanentsChanged();
            return any;
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
                    _recoveryLeft = 0;
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
            _stats = ComputeStats();
            StatsChanged?.Invoke(_stats);
            CaveChanged?.Invoke(cave);
        }

        /// <summary>
        /// Crítico: a chance base do golpe ou do toque (<see cref="StrikeRules"/>) mais a da linha Chance de Crítico
        /// (<see cref="MinerStats.CritBonus"/>, D-136). Com chance 0, nenhum número é sorteado.
        /// </summary>
        bool RollCritical(bool isTap)
        {
            if (ForceCritical) return true;
            double chance = (isTap ? _strike.TapCritChance : _strike.CritChance) + _stats.CritBonus;
            return chance > 0 && _critRandom.NextDouble() < chance;
        }

        /// <summary>O que a loja mostra da linha agora: nível, custo, estado do botão e efeito "de → para".</summary>
        public UpgradePreview Preview(UpgradeId id) => UpgradePreview.For(_rules, _economy, _state, id, _permanents);

        /// <summary>O que a aba das permanentes mostra da linha (Marco 8b, E4).</summary>
        public PermanentPreview PreviewPermanent(PermanentId id) => PermanentPreview.For(_permanents, id);

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

                if (_miner.State == MinerState.Recovering && _recoveryLeft <= Epsilon)
                {
                    // Terminou o golpe (D-126): corre, ou já bate se o próximo está no alcance.
                    _recoveryLeft = 0;
                    _miner.State = MinerState.Running;
                    if (_miner.Target != null && ToGo() <= Epsilon) BeginMining();
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

                // O estagiário depois do anão (no mesmo instante, o anão primeiro): se o golpe do anão quebrou o cristal, o
                // anão já está correndo e o estagiário não bate no que caiu; o golpe dele fica para o próximo cristal.
                if (InternCanHit && _internTimer >= _internStats.HitInterval - Epsilon)
                {
                    _internTimer = Math.Max(0, _internTimer - _internStats.HitInterval);
                    InternHit();
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
                case MinerState.Recovering:
                    next = Math.Min(next, Math.Max(0, _recoveryLeft));
                    break;
            }
            if (InternCanHit) next = Math.Min(next, Math.Max(0, _internStats.HitInterval - _internTimer));

            return next;
        }

        void Advance(double step)
        {
            if (step <= 0) return;

            Elapsed += step;
            _state.AddPlayTime(step);

            // Fila cheia ou parede de pé: o relógio de nascimento espera no limite e nasce assim que der.
            _spawnTimer = SpawnWaits ? Math.Min(_spawnTimer + step, _rules.SpawnInterval) : _spawnTimer + step;

            if (_internStats.IsHired) AdvanceInternTimer(step);

            switch (_miner.State)
            {
                case MinerState.Running:
                    // A1 (D-124): o relógio do golpe enche correndo, até um intervalo; a chegada não o zera.
                    if (ArrivalStrike == ArrivalStrike.Charge)
                        _miner.HitTimer = Math.Min(_miner.HitTimer + step, _stats.HitInterval);
                    Run(step);
                    break;
                case MinerState.Mining:
                    _miner.HitTimer += step;
                    break;
                case MinerState.Recovering:
                    // Parado, terminando o golpe (D-126). O relógio do golpe conta desde a picaretada (A1 continua enchendo).
                    _recoveryLeft = Math.Max(0, _recoveryLeft - step);
                    _miner.HitTimer = Math.Min(_miner.HitTimer + step, _stats.HitInterval);
                    break;
            }
        }

        /// <summary>
        /// O anão chegou ao alcance do alvo e começa a bater. O relógio do golpe fica como a <see cref="ArrivalStrike"/>
        /// manda (D-124): zerado (o primeiro golpe um intervalo depois), como encheu correndo, ou cheio (golpe na hora).
        /// </summary>
        void BeginMining()
        {
            _miner.State = MinerState.Mining;
            switch (ArrivalStrike)
            {
                case ArrivalStrike.Charge:
                    _miner.HitTimer = Math.Min(_miner.HitTimer, _stats.HitInterval);
                    break;
                case ArrivalStrike.Immediate:
                    _miner.HitTimer = _stats.HitInterval;
                    break;
                default:
                    _miner.HitTimer = 0;
                    break;
            }
            if (_internStats.IsHired) InternArrives();
        }

        /// <summary>O estagiário bate só com o anão batendo, no alvo dele (§5.2).</summary>
        bool InternCanHit => _internStats.IsHired && _miner.State == MinerState.Mining && _miner.Target != null;

        /// <summary>O estagiário chegou ao alcance junto com o anão: o relógio dele segue o mesmo modo (D-168, D-124).</summary>
        void InternArrives()
        {
            switch (ArrivalStrike)
            {
                case ArrivalStrike.Charge:
                    _internTimer = Math.Min(_internTimer, _internStats.HitInterval);
                    break;
                case ArrivalStrike.Immediate:
                    _internTimer = _internStats.HitInterval;
                    break;
                default:
                    _internTimer = 0;
                    break;
            }
        }

        /// <summary>
        /// O relógio do estagiário: com o anão batendo, conta; correndo (ou terminando o golpe, D-126), só enche no modo A1,
        /// até um intervalo, como o do anão.
        /// </summary>
        void AdvanceInternTimer(double step)
        {
            if (_miner.State == MinerState.Mining && _miner.Target != null)
                _internTimer += step;
            else if (ArrivalStrike == ArrivalStrike.Charge)
                _internTimer = Math.Min(_internTimer + step, _internStats.HitInterval);
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
                BeginMining();
            }
            else
            {
                _miner.Distance += travel;
            }
        }

        void StartTarget(Crystal target)
        {
            _miner.Target = target;
            _swungAtTarget = false;
            if (_miner.State == MinerState.Recovering)
            {
                // Ainda terminando o golpe (D-126): o alvo novo já vale para o toque; ele corre quando terminar.
            }
            else if (ToGo() <= Epsilon)
            {
                BeginMining();
            }
            else
            {
                _miner.State = MinerState.Running;
                if (ArrivalStrike != ArrivalStrike.Charge)
                {
                    _miner.HitTimer = 0;
                    _internTimer = 0;
                }
            }
            TargetChanged?.Invoke(target);
        }

        void Hit()
        {
            var target = _miner.Target;
            bool critical = RollCritical(isTap: false);
            double damage = _stats.Damage * (critical ? _strike.CritMultiplier : 1);

            _state.RegisterHit();
            _swungAtTarget = true;
            bool broke = target.ApplyDamage(damage);
            HitLanded?.Invoke(new MiningHit(target, damage, isTap: false, isCritical: critical));

            if (broke) Break(target);
        }

        /// <summary>
        /// Golpe do estagiário no alvo do anão (M7): o dano dele, sem crítico (D-167). Se quebrar, é uma quebra como as
        /// outras (D-95): paga uma vez, com a Mineração do anão (D-166), e conta na trava do Mapa; os dois correm juntos.
        /// </summary>
        void InternHit()
        {
            var target = _miner.Target;
            double damage = _internStats.Damage;
            _internHits++;
            bool broke = target.ApplyDamage(damage);
            InternHitLanded?.Invoke(new MiningHit(target, damage, isTap: false, isCritical: false));
            if (broke) Break(target);
        }

        /// <summary>
        /// A quebra de um cristal paga e conta; a da parede (M8) não, e leva à caverna seguinte ou, na última, ao coração
        /// da montanha (D-112). Quebrado pelo toque no meio da picaretada, o anão termina o golpe antes de correr (D-126);
        /// quebrado pela picareta, ou antes de ela descer nele, corre na hora.
        /// </summary>
        void Break(Crystal target, bool byTap = false)
        {
            double recovery = SwingRecovery - _miner.HitTimer;
            bool finishSwing = byTap && _miner.State == MinerState.Mining && _swungAtTarget && recovery > Epsilon;

            _track.Remove(target);
            if (!target.IsWall) _state.RegisterBreak(target.Reward * _stats.RewardMultiplier);
            _miner.Target = null;
            if (finishSwing)
            {
                _miner.State = MinerState.Recovering;
                _recoveryLeft = recovery;
            }
            else if (_miner.State != MinerState.Recovering)
            {
                _miner.State = MinerState.Running;
            }

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
