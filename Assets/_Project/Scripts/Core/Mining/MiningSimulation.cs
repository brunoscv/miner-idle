using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>
    /// O loop de mineração da descida (M6, D-52): o anão corre até o primeiro cristal da fila, bate a cada intervalo,
    /// quebra, soma lascas e corre para o próximo; sem cristal, continua correndo (D-55). Cristais nascem a cada
    /// intervalo no fim da fila, até o máximo (D-54, D-60). Desde o M3, dano, intervalo, corrida e lascas vêm dos
    /// atributos (<see cref="MinerStats"/>), que as compras (<see cref="Purchase"/>) melhoram. Desde o M5: o crítico
    /// (<see cref="StrikeRules"/>); o toque e o combo do M5 saíram no M9g (D-311), e a Bomba entrou no lugar. Desde o M8: os andares (no v8, M9c:
    /// o Próximo Andar, <see cref="AdvanceFloor"/>). Desde o M7: o estagiário, que bate no mesmo cristal do anão com o relógio dele e
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

        readonly MiningRules _rules;
        readonly CrystalTrack _track;
        readonly Miner _miner;
        readonly GameState _state;
        readonly EconomyRules _economy;
        readonly StrikeRules _strike;
        readonly IRandom _critRandom;
        // O sorteio do raro e das cartas (M9b E4), à parte do crítico: os críticos de uma semente não mudam.
        readonly IRandom _rareRandom;
        readonly Permanents _permanents;
        bool _rested;

        MinerStats _stats;
        InternStats _internStats;
        // O relógio do golpe do estagiário (M7, D-168): conta enquanto o anão bate; correndo, só enche no modo A1.
        double _internTimer;
        long _internHits;
        double _spawnTimer;
        // A transição de andar (M9c E0, D-266): a duração escolhida e quanto falta da que está correndo.
        double _floorTransitionSeconds;
        double _floorTransitionLeft;
        double _floorTransitionLength;

        // A Bomba (M9g, D-240, D-307, D-308, D-313): segundos de carga, a bomba no ar (quanto falta e quanto ela tira) e as
        // listas reaproveitadas dos cristais no alcance e dos que a explosão quebrou (sem alocar a cada bomba).
        double _bombCharge;
        bool _bombInFlight;
        double _bombFlightLeft;
        double _bombFlightFraction;
        readonly List<Crystal> _bombTargets = new List<Crystal>();
        readonly List<Crystal> _bombBroken = new List<Crystal>();

        // D-126: se a picareta já desceu no alvo atual, e quanto falta do golpe depois que a bomba quebrou o cristal.
        bool _swungAtTarget;
        double _recoveryLeft;

        /// <param name="random">
        /// Sorteio do crítico: desde a fila (M6) os cristais nascem em lugares fixos, então a fila não depende dele.
        /// </param>
        /// <param name="economy">Melhorias à venda. Sem ela, nenhuma linha vende e os atributos são os valores base (o jogo do M2).</param>
        /// <param name="strike">Crítico, raro, anúncios e bomba. Sem elas, não há crítico (o jogo do M2–M4).</param>
        /// <param name="permanents">
        /// O dia e as melhorias permanentes (Marco 8b), que passam de uma run para a outra. Sem elas, não há descanso nem
        /// permanentes (o jogo até o GDD v5).
        /// </param>
        /// <param name="rareRandom">
        /// O sorteio do cristal raro e das cartas (M9b E4), separado do crítico. Sem ele, uma semente fixa (os testes).
        /// </param>
        /// <param name="rareRhythm">O ritmo do raro (D-185), já para os cristais do começo da run.</param>
        public MiningSimulation(MiningRules rules, IRandom random, GameState state = null, EconomyRules economy = null,
                                StrikeRules strike = null, Permanents permanents = null, IRandom rareRandom = null,
                                RareRhythm rareRhythm = RareRhythm.Gdd)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _critRandom = random ?? throw new ArgumentNullException(nameof(random));
            _rareRandom = rareRandom ?? new SeededRandom(RareFallbackSeed);
            RareRhythm = rareRhythm;
            _strike = strike ?? StrikeRules.NoCritical();
            _state = state ?? new GameState();
            // Sem economia, o mínimo do intervalo é o próprio intervalo base: os atributos ficam iguais aos valores base.
            _economy = economy ?? EconomyRules.None(minHitInterval: rules.HitInterval);
            _permanents = permanents ?? Permanents.None();
            _bombCharge = BombChargeSeconds; // D-313: a run começa com a bomba cheia
            _permanents.State.ReachFloor(_state.Cave); // uma partida aberta num andar já conta para o recorde
            _stats = ComputeStats();
            _internStats = ComputeInternStats();
            ArrivalStrike = rules.ArrivalStrike;
            _miner = new Miner();
            _track = new CrystalTrack(rules) { Spacing = _permanents.CrystalSpacing(rules) };
            _track.SetCrystalValues(rules.CrystalHealthIn(_state.Cave), rules.CrystalRewardIn(_state.Cave));
            _track.RollRare = RollRare;
            _track.SpawnInitial(_miner.Distance);
        }

        /// <summary>A semente do raro quando ninguém passa um gerador (os testes): qualquer número fixo serve.</summary>
        const ulong RareFallbackSeed = 0x5241_5245UL;

        /// <summary>
        /// Um cristal nasceu. Os do início já existem ao construir: quem desenha lê <see cref="Crystals"/> primeiro. A fila
        /// nova de um andar (M9c) também avisa por aqui, cristal por cristal, depois de a antiga sair.
        /// </summary>
        public event Action<Crystal> CrystalSpawned;
        /// <summary>O alvo do anão mudou (<c>null</c> = ficou sem alvo).</summary>
        public event Action<Crystal> TargetChanged;
        /// <summary>Golpe do anão no cristal, com o dano e se foi crítico.</summary>
        public event Action<MiningHit> HitLanded;
        /// <summary>O cristal quebrou e saiu do mapa; a recompensa já foi somada.</summary>
        public event Action<Crystal> CrystalBroken;
        /// <summary>O total de lascas mudou.</summary>
        public event Action<double> ShardsChanged;
        /// <summary>Uma melhoria foi comprada, com o nível novo.</summary>
        public event Action<UpgradeId, int> UpgradePurchased;
        /// <summary>Os atributos do anão mudaram (DANO e VELOCIDADE do HUD).</summary>
        public event Action<MinerStats> StatsChanged;
        /// <summary>
        /// Golpe do estagiário (M7) no alvo do anão: nunca crítico (D-167). Sai à parte do <see cref="HitLanded"/>, que
        /// continua só do anão: quem desenha o anão não confunde os dois.
        /// </summary>
        public event Action<MiningHit> InternHitLanded;
        /// <summary>O estagiário foi contratado, demitido ou mudou de atributos (compra, Treinar, atalhos do debug).</summary>
        public event Action<InternStats> InternChanged;
        /// <summary>
        /// Os bônus com tempo mudaram (M9b): entrou, acabou, o save trouxe ou o debug limpou. O dano já foi recalculado
        /// (<see cref="StatsChanged"/> sai antes).
        /// </summary>
        public event Action BonusesChanged;
        /// <summary>
        /// O andar ou a barra dele mudou fora de uma quebra: o Próximo Andar e os atalhos do debug. A quebra comum já avisa
        /// por <see cref="CrystalBroken"/>.
        /// </summary>
        public event Action<int> CaveChanged;
        /// <summary>
        /// O coração da montanha (D-112): no v8, o Próximo Andar com a barra cheia no último andar (D-258). Sai uma vez por
        /// partida; o jogo segue no último andar, sem passagem.
        /// </summary>
        public event Action HeartReached;
        /// <summary>
        /// O banco de lascas vermelhas ou os níveis das permanentes mudaram (Marco 8b): compra, atalhos do debug. O
        /// descanso não avisa por aqui: depois dele a run é outra (<see cref="Rest"/>).
        /// </summary>
        public event Action PermanentsChanged;

        /// <summary>A volta do offline esperando o COLETAR mudou: chegou uma, somou outra ou foi coletada (M9, E2).</summary>
        public event Action<OfflineReturn> OfflineReturnChanged;

        /// <summary>
        /// A janela do cristal raro mudou (M9b E4): abriu, entrou um raro na fila, a carta saiu no sorteio, fechou ou abriu
        /// a próxima da fila. Não sai a cada quadro: o tempo que falta se lê em <see cref="RareWindow"/>.
        /// </summary>
        public event Action<RareWindow> RareWindowChanged;

        /// <summary>
        /// Uma carta do raro entrou (escolhida, sorteada ou pelo debug; D-225): o texto que sobe do anão. Os atributos e a
        /// coluna já mudaram (<see cref="BonusesChanged"/> sai antes).
        /// </summary>
        public event Action<RareCard> RareCardApplied;

        /// <summary>
        /// O boost do AD entrou (M9b E5, D-231): o texto que sobe do anão e o som. Os atributos e a coluna já mudaram
        /// (<see cref="BonusesChanged"/> sai antes).
        /// </summary>
        public event Action AdBoostGranted;

        /// <summary>Um anúncio do Boost foi assistido (ou renovado) e os bônus dele entraram (M9e E2).</summary>
        public event Action<AdKind> AdWatched;

        /// <summary>
        /// A transição de andar começou (M9c E0, D-266): a caverna já é a nova (sai antes do <see cref="CaveChanged"/>) e,
        /// até <see cref="FloorTransitionEnded"/>, nada corre. O número é a caverna nova.
        /// </summary>
        public event Action<int> FloorTransitionStarted;

        /// <summary>A transição de andar acabou: o jogo volta a correr.</summary>
        public event Action FloorTransitionEnded;

        /// <summary>
        /// A bomba saiu (M9g, D-308), com os cristais no alcance agora (para onde ela voa). A lista é reaproveitada: quem
        /// escuta copia o que precisar.
        /// </summary>
        public event Action<IReadOnlyList<Crystal>> BombThrown;
        /// <summary>
        /// A bomba tirou vida de um cristal na chegada (M9g): a % da vida máxima, sem crítico. Sai à parte do
        /// <see cref="HitLanded"/>, que continua do anão.
        /// </summary>
        public event Action<MiningHit> BombHitLanded;
        /// <summary>
        /// A bomba explodiu (M9g), com os cristais que ela atingiu (vazia se a fila mudou no ar, numa troca de andar). Vem
        /// depois de cada <see cref="BombHitLanded"/> e antes das quebras. A lista é reaproveitada.
        /// </summary>
        public event Action<IReadOnlyList<Crystal>> BombLanded;

        public MiningRules Rules => _rules;
        public EconomyRules Economy => _economy;
        public StrikeRules Strike => _strike;

        /// <summary>O dia e as melhorias permanentes (Marco 8b): os mesmos de uma run para a outra.</summary>
        public Permanents Permanents => _permanents;

        /// <summary>Atalho do debug: todo golpe sai crítico.</summary>
        public bool ForceCritical { get; set; }

        /// <summary>Os bônus com tempo valendo agora (M9b; os mesmos do <see cref="GameState"/>, que vão para o save).</summary>
        public ActiveBonuses Bonuses => _state.Bonuses;

        /// <summary>
        /// Quantos raros nascem (D-185): o do documento (15% por cristal) ou o lento do DBG → Cálculos (2%, sem raro novo
        /// com janela aberta ou carta ativa, D-226). Vale para os cristais que nascerem daqui em diante.
        /// </summary>
        public RareRhythm RareRhythm { get; set; }

        /// <summary>Atalho do debug ("Próximo raro", D-226): o próximo cristal que nascer na fila é raro.</summary>
        public bool ForceNextRare { get; set; }

        /// <summary>
        /// Atalho do debug ("Contador 5 s", D-285): o contador do raro com este intervalo, no lugar do das regras. 0 = o das
        /// regras. Só vale num balanço com contador (GDD v8).
        /// </summary>
        public double RareIntervalOverride { get; set; }

        /// <summary>Atalho do debug ("Tier 2", M9e E1): as cartas no tier 2, sem o Raro Dourado.</summary>
        public bool ForceRareTier2 { get; set; }

        /// <summary>Os segundos de run entre um raro e o próximo agora (o do DBG ou o das regras); 0 sem contador.</summary>
        public double RareInterval =>
            !_strike.Rare.UsesCounter ? 0 : RareIntervalOverride > 0 ? RareIntervalOverride : _strike.Rare.Interval;

        /// <summary>O contador do raro anda: balanço com contador, raro no balanço e o andar dele alcançado (D-283).</summary>
        public bool IsRareCounting => _strike.Rare.IsEnabled && _strike.Rare.UsesCounter && IsRareUnlocked;

        /// <summary>
        /// Segundos até o raro (o DBG mostra, D-285): 0 com ele já esperando o próximo cristal nascer. Infinito quando o
        /// contador não anda.
        /// </summary>
        public double RareCountdown => IsRareCounting ? Math.Max(0, RareInterval - _state.RareTimer) : double.PositiveInfinity;

        /// <summary>
        /// O tier das cartas do raro (GDD v8, D-243, D-286): 2 com o Raro Dourado comprado (ou o atalho do DBG), se o
        /// balanço tem o tier 2; senão 1. O anúncio do tier 2 por 1 h entra na E2.
        /// </summary>
        public int RareTier =>
            _strike.Rare.HasTier2 && (ForceRareTier2 || _permanents.HasGoldenRare || _state.Bonuses.HasRareTier2Ad) ? 2 : 1;

        /// <summary>A janela do cristal raro (M9b E4): fechada (<c>default</c>), escolhendo, ou mostrando o sorteio.</summary>
        public RareWindow RareWindow => _state.RareWindow;

        /// <summary>
        /// Quando sai o primeiro golpe num cristal (D-124). Começa no valor de <see cref="MiningRules.ArrivalStrike"/>; o DBG
        /// troca no meio da partida para comparar.
        /// </summary>
        public ArrivalStrike ArrivalStrike { get; set; }

        /// <summary>
        /// Segundos de jogo até o próximo golpe do anão, se nada mudar (sem bomba nem compra): a apresentação começa a
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
        /// intervalo do golpe. A apresentação toca o golpe nesse tempo, com o cristal vivo ou quebrado pela bomba.
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

        /// <summary>Cristais inteiros na fila, do primeiro (o alvo) ao último.</summary>
        public IReadOnlyList<Crystal> Crystals => _track.Crystals;

        /// <summary>Segundos de jogo simulados.</summary>
        public double Elapsed { get; private set; }

        /// <summary>
        /// Quanto dura a transição de andar, em segundos de jogo (M9c E0, D-266/D-267). 0 = sem transição (o jogo até o
        /// M9c): a caverna nova começa na hora. Quem escolhe é a apresentação (a transição do DBG); vale para a próxima troca.
        /// </summary>
        public double FloorTransitionSeconds
        {
            get => _floorTransitionSeconds;
            set
            {
                if (double.IsNaN(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                _floorTransitionSeconds = value;
            }
        }

        /// <summary>
        /// Se a transição de andar está correndo: o tempo de jogo anda, mas nada mais (golpe, estagiário, nascimento pelo
        /// relógio, tempo dos bônus e da janela do raro, bomba).
        /// </summary>
        public bool IsInFloorTransition => _floorTransitionLeft > 0;

        /// <summary>Segundos de jogo que faltam da transição de andar (0 fora dela). A animação lê daqui.</summary>
        public double FloorTransitionLeft => _floorTransitionLeft;

        /// <summary>A duração da transição que está correndo (a de quando ela começou; 0 fora dela).</summary>
        public double FloorTransitionLength => _floorTransitionLength;

        // ── Marcos de andar (M9c E3, D-253) ──────────────────────────────────────────────────────────────────────────

        /// <summary>O andar mais fundo já alcançado (o recorde das permanentes): sobrevive ao descanso e liga os marcos.</summary>
        public int DeepestFloor => Purchases.DeepestFloor(_state, _permanents);

        /// <summary>
        /// O ajuste "Avanço automático" (P6; ligado de fábrica, vem dos AJUSTES). Só vale depois do marco
        /// (<see cref="IsAutoAdvanceUnlocked"/>): então a barra cheia desce sozinha, na hora, com a mesma transição.
        /// </summary>
        public bool AutoAdvance { get; set; } = true;

        /// <summary>
        /// Descansando (M9d, D-265): a run nova está montada, mas <b>parada</b> até o INICIAR. O tempo não anda (nem o anão,
        /// nem o estagiário, nem as cartas e bônus, nem o tempo do dia), a bomba e as compras da run não valem, o offline
        /// não conta (D-275) e as permanentes <b>só</b> se compram aqui (§4.2). Quem dirige a partida põe na criação e, no
        /// INICIAR, monta outra sem ele (com as permanentes que o jogador comprou: a Alma, a Promoção, o Atalho).
        /// </summary>
        public bool IsResting { get; set; }

        /// <summary>A distância entre cristais desta run, em unidades (com o Atalho, M9d).</summary>
        public double CrystalSpacing => _track.Spacing;

        /// <summary>Se o avanço automático já existe (GDD v8: depois do andar 5, para sempre).</summary>
        public bool IsAutoAdvanceUnlocked => _rules.IsAutoAdvanceUnlocked(DeepestFloor);

        /// <summary>Se o jogo desce sozinho agora: o marco alcançado e o ajuste ligado (o botão mostra "AUTO").</summary>
        public bool IsAutoAdvancing => AutoAdvance && IsAutoAdvanceUnlocked;

        /// <summary>Se a LOJA já contrata o estagiário (GDD v8: depois do andar 5, P7).</summary>
        public bool IsInternUnlocked => _rules.IsInternUnlocked(DeepestFloor);

        /// <summary>Se o cristal raro já nasce (GDD v8: depois do andar 10).</summary>
        public bool IsRareUnlocked => _rules.IsRareUnlocked(DeepestFloor);

        /// <summary>
        /// Atalho do debug (DBG → Andar → Recorde, M9c E3): o andar mais fundo direto, também para baixo. Abaixo do andar
        /// de agora, a partida volta a ele antes (o recorde nunca fica acima de onde se está). <c>false</c> se já era.
        /// </summary>
        public bool SetDeepestFloor(int floor)
        {
            floor = Math.Max(1, Math.Min(floor, _rules.CaveCount));
            if (floor < _state.Cave) GoToFloor(floor);
            if (floor == _permanents.State.DeepestFloor) return false;
            _permanents.State.SetDeepestFloor(floor);
            PermanentsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// A barra encheu com o automático valendo (P6): desce. O último andar espera o toque, porque o coração da
        /// montanha é o fim da partida.
        /// </summary>
        bool ShouldAutoAdvance => IsAutoAdvancing && _state.Cave < _rules.CaveCount && CanAdvance;

        /// <summary>Avança <paramref name="dt"/> segundos de jogo.</summary>
        public void Tick(double dt)
        {
            if (double.IsNaN(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt), "O tempo não anda para trás.");
            if (IsResting) return; // D-265: descansando, nada corre

            double remaining = dt;
            int steps = 0;
            while (true)
            {
                if (_floorTransitionLeft > 0 && remaining > 0)
                {
                    // A transição de andar (D-266): só o relógio do jogo anda.
                    double pause = Math.Min(remaining, _floorTransitionLeft);
                    Elapsed += pause;
                    _state.AddPlayTime(pause);
                    remaining -= pause;
                    _floorTransitionLeft -= pause;
                    if (_floorTransitionLeft <= Epsilon) EndFloorTransition();
                    continue;
                }
                if (_floorTransitionLeft > 0) break; // sem tempo sobrando, ainda na transição

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
            if (IsResting) return default; // D-275: descansando, o jogo está parado; nem o estagiário trabalha
            // A conta antes de o tempo dos bônus correr: a carta do raro que estava ativa vale para o estagiário até acabar
            // (D-181, D-190). A janela do raro não anda: espera o jogador (D-222).
            // GDD v8 (D-284): um raro a cada intervalo do contador de offline, quebrado pelo estagiário, com uma carta
            // sorteada do tier que vale para ele. O contador da run não anda (D-283).
            double counted = secondsAway <= 0 || double.IsInfinity(secondsAway) ? 0 : Math.Min(secondsAway, OfflineMaxSeconds);
            var rares = OfflineRareCards(counted);
            var report = OfflineMining.Compute(_rules, _economy, _state, _permanents, secondsAway, IsAutoAdvancing, _strike.Ads, rares);

            // Os bônus com tempo correm com o app fechado (D-181, D-214), sem o teto do offline; relógio voltado não devolve tempo.
            if (secondsAway > 0 && !double.IsInfinity(secondsAway)) AdvanceBonuses(secondsAway);
            ApplyOfflineFloor(report);
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

        /// <summary>
        /// As cartas dos raros do offline (D-284): um a cada <see cref="RareInterval"/> de <paramref name="counted"/>, com o
        /// estagiário contratado e o raro liberado; a carta é sorteada no gerador do raro, no tier daquele instante (o
        /// anúncio do tier 2 pode acabar no meio). <c>null</c> sem nenhum.
        /// </summary>
        List<OfflineRareCard> OfflineRareCards(double counted)
        {
            var rare = _strike.Rare;
            if (counted <= 0 || !IsRareCounting || !_internStats.IsHired || rare.Cards.Count == 0) return null;
            double interval = RareInterval;
            long count = (long)Math.Floor(counted / interval + 1e-9);
            if (count <= 0) return null;
            bool permanentTier2 = rare.HasTier2 && (ForceRareTier2 || _permanents.HasGoldenRare);
            double tier2AdLeft = 0;
            var all = _state.Bonuses.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Kind == BonusKind.RareTier2) tier2AdLeft = Math.Max(tier2AdLeft, all[i].SecondsLeft);

            var cards = new List<OfflineRareCard>((int)Math.Min(count, 100_000));
            for (long k = 1; k <= count && k <= 100_000; k++)
            {
                double at = k * interval;
                var card = rare.Cards[_rareRandom.NextInt(0, rare.Cards.Count)].Card;
                int tier = permanentTier2 || (rare.HasTier2 && tier2AdLeft > at) ? 2 : 1;
                cards.Add(new OfflineRareCard(at, rare.Get(card, tier)));
            }
            return cards;
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
            if (IsResting) return PurchaseResult.Locked; // a run ainda não começou
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
        /// Atalho do debug (M9j E3): até <paramref name="count"/> níveis de graça na linha da run, como compras sem custo
        /// (respeita o limite, a trava do andar e o piso do golpe, D-371). Devolve quantos subiram.
        /// </summary>
        public int GrantUpgradeLevels(UpgradeId id, int count)
        {
            if (IsResting) return 0;
            int raised = 0;
            while (raised < count && Purchases.NextCost(_state, _economy, id, _rules, _permanents) != null
                   && Purchases.Blocked(_state, _rules, id, _permanents) == null)
            {
                _state.RaiseLevel(id);
                raised++;
            }
            if (raised == 0) return 0;

            _stats = ComputeStats();
            UpgradePurchased?.Invoke(id, _state.LevelOf(id));
            StatsChanged?.Invoke(_stats);
            if (id == UpgradeId.Intern || EconomyRules.IsInternTree(id)) OnInternChanged();
            return raised;
        }

        // ── A Bomba (M9g; GDD v8 §2.1, D-240, D-255, D-307, D-308, D-309, D-313) ──────────────────────────────────

        /// <summary>As regras da bomba deste balanço (<see cref="BombRules.None"/> sem bomba).</summary>
        public BombRules Bombs => _strike.Bombs;

        /// <summary>O balanço tem bomba (GDD v8).</summary>
        public bool HasBomb => _strike.Bombs.IsEnabled;

        /// <summary>A bomba em uso: 0 = a Pequena; cada compra da linha Bomba passa para a próxima (D-240).</summary>
        public int BombIndex => Math.Max(0, Math.Min(_strike.Bombs.Count - 1, _permanents.BombLevel));

        /// <summary>A fração da vida máxima que a bomba em uso tira (a Pequena: 0,1).</summary>
        public double BombFraction => HasBomb ? _strike.Bombs.FractionAt(BombIndex) : 0;

        /// <summary>
        /// Quanto a bomba leva para carregar: a carga do balanço (5 s) menos o Bombardeador (GDD v8.4, M9j E3: 0,5 s por
        /// nível), nunca abaixo de um décimo da do balanço.
        /// </summary>
        public double BombChargeSeconds =>
            Math.Max(_strike.Bombs.ChargeSeconds * 0.1, _strike.Bombs.ChargeSeconds - _permanents.BombChargeReduction);

        /// <summary>A carga da bomba, de 0 a 1.</summary>
        public double BombCharge => HasBomb ? Math.Min(1, _bombCharge / BombChargeSeconds) : 0;

        /// <summary>Segundos de jogo até a bomba encher (0 cheia).</summary>
        public double BombChargeLeft => HasBomb ? Math.Max(0, BombChargeSeconds - _bombCharge) : 0;

        /// <summary>A bomba está cheia.</summary>
        public bool IsBombCharged => HasBomb && _bombCharge >= BombChargeSeconds - Epsilon;

        /// <summary>Uma bomba no ar (D-308).</summary>
        public bool IsBombInFlight => _bombInFlight;

        /// <summary>Segundos de jogo até a bomba no ar explodir (0 sem bomba no ar).</summary>
        public double BombFlightLeft => _bombInFlight ? _bombFlightLeft : 0;

        /// <summary>
        /// Atalho do debug (DBG → Bomba → Bomba automática): lança sozinha sempre que enche e há cristal no alcance.
        /// </summary>
        public bool AutoBomb { get; set; }

        /// <summary>
        /// Se a bomba sai agora: cheia, sem outra no ar, fora do descanso e da transição, com algum cristal no alcance
        /// (D-307: sem cristal, não sai).
        /// </summary>
        public bool CanThrowBomb =>
            IsBombCharged && !_bombInFlight && !IsResting && _floorTransitionLeft <= 0 && HasCrystalInBombReach;

        /// <summary>Algum cristal da fila está no alcance da bomba (<see cref="BombRules.Reach"/> à frente do anão).</summary>
        public bool HasCrystalInBombReach
        {
            get
            {
                var front = _track.Front;
                return front != null && InBombReach(front);
            }
        }

        bool InBombReach(Crystal crystal) => crystal.Distance - _miner.Distance <= _strike.Bombs.Reach + Epsilon;

        /// <summary>
        /// Lança a bomba (D-310: o deslize da esquerda para a direita): a carga zera e a bomba voa
        /// <see cref="BombRules.FlightSeconds"/>; o dano entra na chegada, nos cristais que estiverem no alcance (D-308).
        /// <c>false</c> se ela não sai agora (<see cref="CanThrowBomb"/>).
        /// </summary>
        public bool ThrowBomb()
        {
            if (!CanThrowBomb) return false;
            _bombCharge = 0;
            _bombInFlight = true;
            _bombFlightLeft = _strike.Bombs.FlightSeconds;
            _bombFlightFraction = BombFraction;
            CollectBombTargets();
            BombThrown?.Invoke(_bombTargets);
            if (_bombFlightLeft <= Epsilon) LandBomb();
            return true;
        }

        /// <summary>Atalho do debug (DBG → Bomba → Encher): a bomba cheia na hora.</summary>
        public void FillBomb()
        {
            if (HasBomb) _bombCharge = BombChargeSeconds;
        }

        /// <summary>Os cristais no alcance da bomba agora, do mais perto ao mais longe, na lista reaproveitada.</summary>
        void CollectBombTargets()
        {
            _bombTargets.Clear();
            var crystals = _track.Crystals;
            for (int i = 0; i < crystals.Count && InBombReach(crystals[i]); i++) _bombTargets.Add(crystals[i]);
        }

        /// <summary>
        /// A bomba chega (D-307, D-308): cada cristal no alcance perde a fração da vida máxima, sem crítico nem bônus
        /// (D-255); quem zerar quebra e paga como qualquer quebra (D-309), depois de todos apanharem.
        /// </summary>
        void LandBomb()
        {
            _bombInFlight = false;
            _bombFlightLeft = 0;
            CollectBombTargets();
            _bombBroken.Clear();
            for (int i = 0; i < _bombTargets.Count; i++)
            {
                var crystal = _bombTargets[i];
                double damage = crystal.MaxHealth * _bombFlightFraction;
                bool broke = crystal.ApplyDamage(damage);
                BombHitLanded?.Invoke(new MiningHit(crystal, damage, false));
                if (broke) _bombBroken.Add(crystal);
            }
            BombLanded?.Invoke(_bombTargets);
            for (int i = 0; i < _bombBroken.Count; i++)
            {
                var crystal = _bombBroken[i];
                if (crystal == _miner.Target) Break(crystal, midSwing: true); // o anão termina o golpe antes de correr (D-126)
                else BreakAhead(crystal);
            }
        }

        /// <summary>
        /// A bomba quebrou um cristal da fila que não é o alvo (M9g): sai da fila, paga e conta na barra como qualquer
        /// quebra (D-309), sem mexer no anão.
        /// </summary>
        void BreakAhead(Crystal crystal)
        {
            _track.Remove(crystal);
            _state.RegisterBreak(crystal.Reward * _stats.RewardMultiplier);
            CrystalBroken?.Invoke(crystal);
            ShardsChanged?.Invoke(_state.Shards);
            if (crystal.IsRare) OpenRareWindow();
        }

        /// <summary>A troca de andar com a bomba no ar (M9g): a fila em que ela ia cair saiu, então ela some sem explodir.</summary>
        void CancelBomb()
        {
            if (!_bombInFlight) return;
            _bombInFlight = false;
            _bombFlightLeft = 0;
            _bombTargets.Clear();
            BombLanded?.Invoke(_bombTargets);
        }

        /// <summary>
        /// Os cristais que o andar atual pede para o Próximo Andar (GDD v8, D-239): <c>andar × 10</c> menos a Memória, no
        /// mínimo 0.
        /// </summary>
        public long RequiredBreaks => _permanents.FloorRequirement(_rules, _state.Cave);

        /// <summary>A barra do Próximo Andar, de 0 a 1: cristais quebrados no andar ÷ o requisito; cheia, fica em 1 (D-259).</summary>
        public double FloorProgress
        {
            get
            {
                long required = RequiredBreaks;
                return required <= 0 ? 1 : Math.Min(1, (double)_state.CaveBreaks / required);
            }
        }

        /// <summary>
        /// Se o Próximo Andar vale agora: a barra cheia, fora de uma transição e antes do coração da montanha. No último
        /// andar ele leva ao coração (D-258).
        /// </summary>
        public bool CanAdvance =>
            !_state.ReachedHeart && _floorTransitionLeft <= 0 && _state.CaveBreaks >= RequiredBreaks;

        /// <summary>
        /// O Próximo Andar (M9c, GDD v8): abre a transição (D-266), muda de andar, zera a barra (D-259) e refaz a fila com
        /// a vida e as lascas do andar novo. No último andar, leva ao coração da montanha (D-258). <c>false</c> se a barra
        /// não está cheia.
        /// </summary>
        public bool AdvanceFloor()
        {
            if (!CanAdvance) return false;
            if (_state.Cave >= _rules.CaveCount)
            {
                _state.ReachHeart();
                HeartReached?.Invoke();
                return true;
            }
            EnterCave(_state.Cave + 1);
            return true;
        }

        /// <summary>Atalho do debug (DBG → Andar → Encher a barra): os cristais do andar contados, sem quebrar.</summary>
        public void FillFloorBar()
        {
            long goal = RequiredBreaks;
            if (_state.CaveBreaks >= goal) return;
            _state.SetCaveBreaks(goal);
            CaveChanged?.Invoke(_state.Cave);
        }

        /// <summary>
        /// Se o botão Descansar vale agora (Marco 8b, D-152): o balanço tem descanso e a run chegou ao nível mínimo (GDD v7:
        /// 10). Depois de descansar, esta run acabou: não vale mais.
        /// </summary>
        public bool CanRest => !_rested && _permanents.Rules.IsRestEnabled && _state.Cave >= _permanents.Rules.RestLevel;

        /// <summary>
        /// O Descansar ainda trancado (M9i E1, #15): o balanço tem descanso, a run não descansou e ainda não chegou ao andar
        /// do descanso (<see cref="RestLevel"/>). A barra mostra o botão apagado, com o andar que falta.
        /// </summary>
        public bool IsRestLocked => !_rested && _permanents.Rules.IsRestEnabled && _state.Cave < _permanents.Rules.RestLevel;

        /// <summary>O andar a partir do qual dá para descansar (v8: 10).</summary>
        public int RestLevel => _permanents.Rules.RestLevel;

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
            if (_rested || IsResting || (!force && !CanRest)) return null;
            _rested = true;
            return _permanents.Rest(_state);
        }

        /// <summary>
        /// Compra o próximo nível da melhoria permanente com lascas vermelhas (Marco 8b). O efeito vale na hora (D-155):
        /// atributos recalculados e, para a Memória, a trava do Mapa (avisa por <see cref="CaveChanged"/>, como os atalhos).
        /// </summary>
        public PurchaseResult PurchasePermanent(PermanentId id)
        {
            if (!IsResting) return PurchaseResult.Locked; // §4.2, D-265: só no descanso
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

        /// <summary>
        /// Atalho do debug (DBG → Dia, M9d): um nível a mais na permanente, de graça; no limite, volta a 0. Devolve o nível
        /// novo. Não respeita a trava da Promoção (é do debug).
        /// </summary>
        public int CyclePermanent(PermanentId id)
        {
            var line = _permanents.Rules.Get(id);
            int level = _permanents.State.LevelOf(id);
            int next = line.IsEnabled && level < line.MaxLevel ? level + 1 : 0;
            _permanents.State.SetLevel(id, next);
            OnPermanentsChanged();
            return next;
        }

        void OnPermanentsChanged()
        {
            _track.Spacing = _permanents.CrystalSpacing(_rules); // o Atalho vale para os cristais que nascem depois
            _stats = ComputeStats();
            PermanentsChanged?.Invoke();
            StatsChanged?.Invoke(_stats);
            CaveChanged?.Invoke(_state.Cave); // a trava do Mapa pode ter mudado (Memória)
            OnInternChanged(); // o Treinar muda o dano dele
        }

        /// <summary>
        /// Os atributos do anão com os bônus de agora: o dano × (1 + a soma dos +% ativos), depois do Sangue da Montanha
        /// (D-179), × as cartas de dano do raro (D-219); o intervalo ÷ a carta "Rápido", depois do piso (D-180); as lascas ×
        /// a carta "Lascas ×2"; a chance de crítico + a carta "Crítico". A loja compara sem eles (<see cref="Preview"/>); do
        /// estagiário, só as cartas do raro (<see cref="ComputeInternStats"/>).
        /// </summary>
        MinerStats ComputeStats()
        {
            var stats = MinerStats.Compute(_rules, _economy, _state, _permanents);
            var bonuses = _state.Bonuses;
            if (bonuses.Count == 0) return stats;
            return new MinerStats(stats.Damage * (1 + bonuses.DamagePercent) * bonuses.DamageMultiplier,
                                  stats.HitInterval / bonuses.IntervalDivisor, stats.WalkSpeed,
                                  stats.RewardMultiplier * bonuses.ShardsMultiplier, stats.CritBonus + bonuses.CritChance);
        }

        // ── Bônus com tempo (M9b) ─────────────────────────────────────────────────────────────────────────────────

        /// <summary>Atalho do debug: tira todos os bônus. <c>false</c> se não havia nenhum.</summary>
        public bool ClearBonuses()
        {
            if (!_state.Bonuses.Clear()) return false;
            OnBonusesChanged();
            return true;
        }

        void AdvanceBonuses(double seconds)
        {
            if (_state.Bonuses.Count == 0) return;
            _state.Bonuses.Advance(seconds);
            if (_state.Bonuses.RemoveExpired(Epsilon)) OnBonusesChanged();
        }

        void OnBonusesChanged()
        {
            _stats = ComputeStats();
            StatsChanged?.Invoke(_stats);
            // As cartas do raro valem para o estagiário (D-190): o dano e o intervalo dele mudam junto.
            var intern = ComputeInternStats();
            if (intern.Damage != _internStats.Damage || intern.HitInterval != _internStats.HitInterval)
            {
                _internStats = intern;
                InternChanged?.Invoke(_internStats);
            }
            BonusesChanged?.Invoke();
        }

        /// <summary>O estagiário com as cartas do raro ativas (dano × e intervalo ÷, D-190); o AD não vale (D-177).</summary>
        InternStats ComputeInternStats()
        {
            var stats = InternStats.Compute(_rules, _economy, _state, _permanents);
            if (!stats.IsHired) return stats;
            double damage = _state.Bonuses.InternDamageMultiplier, divisor = _state.Bonuses.InternIntervalDivisor;
            if (damage == 1 && divisor == 1) return stats;
            return new InternStats(true, stats.Damage * damage, stats.HitInterval / divisor, stats.OfflineRewardMultiplier);
        }

        // ── Cristal raro (M9b E4) ────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Se o cristal que nasce agora é raro (D-184, D-185): o atalho do debug, ou o sorteio no gerador do raro. No ritmo
        /// lento, nenhum raro enquanto houver janela aberta ou carta ativa.
        /// </summary>
        bool RollRare()
        {
            if (ForceNextRare)
            {
                ForceNextRare = false;
                _state.ResetRareTimer(); // um raro nasceu: o contador recomeça (D-283)
                return true;
            }
            var rare = _strike.Rare;
            if (!rare.IsEnabled || !IsRareUnlocked) return false; // GDD v8: só depois do andar 10 (M9c E3)
            if (rare.UsesCounter)
            {
                // GDD v8 (D-283): o contador cheio faz raro o cristal que nasce agora; nunca dois raros na fila.
                if (_state.RareTimer < RareInterval - Epsilon || _track.HasRare) return false;
                _state.ResetRareTimer();
                return true;
            }
            double chance = rare.Chance;
            if (RareRhythm == RareRhythm.Slow)
            {
                if (_state.RareWindow.IsOpen || _state.Bonuses.HasRareCard) return false;
                chance = rare.SlowChance;
            }
            return chance > 0 && _rareRandom.NextDouble() < chance;
        }

        /// <summary>
        /// Um raro quebrou (ou o atalho "Janela agora" do debug): abre a janela com 2 cartas diferentes; com uma já aberta,
        /// este espera na fila (D-221). <c>false</c> se o balanço não tem raro.
        /// </summary>
        public bool OpenRareWindow()
        {
            if (!_strike.Rare.IsEnabled) return false;
            var window = _state.RareWindow;
            _state.RareWindow = window.IsOpen ? window.WithQueued(window.Queued + 1) : DrawWindow(0);
            RareWindowChanged?.Invoke(_state.RareWindow);
            return true;
        }

        /// <summary>
        /// O jogador tocou na carta <paramref name="index"/> (0 ou 1) da janela: ela entra e a janela fecha, ou abre a
        /// próxima da fila (D-221). <c>false</c> fora da escolha.
        /// </summary>
        public bool ChooseRareCard(int index)
        {
            var window = _state.RareWindow;
            if (!window.IsChoosing || index < 0 || index > 1) return false;
            ApplyRareCard(window.CardAt(index));
            NextRareWindow(window.Queued);
            return true;
        }

        /// <summary>FECHAR na janela do sorteio (D-186): fecha, ou abre a próxima da fila. <c>false</c> se não há o que fechar.</summary>
        public bool CloseRareWindow()
        {
            var window = _state.RareWindow;
            if (!window.IsOpen || window.IsChoosing) return false;
            NextRareWindow(window.Queued);
            return true;
        }

        // ── Boost do AD (M9b E5) ─────────────────────────────────────────────────────────────────────────────────────

        /// <summary>O boost do AD está ativo (o botão mostra quando ele volta, D-188, D-230).</summary>
        public bool HasAdBoost => _state.Bonuses.AdBoost.HasValue;

        /// <summary>Segundos que faltam do boost do AD; 0 sem ele.</summary>
        public double AdBoostSecondsLeft => _state.Bonuses.AdBoost?.SecondsLeft ?? 0;

        /// <summary>O botão AD pode ser usado: o balanço tem o boost e nenhum está ativo (D-188).</summary>
        public bool CanWatchAd => _strike.Ad.IsEnabled && !HasAdBoost;

        /// <summary>
        /// O "anúncio" terminou (o RECOMPENSA, D-218) ou o atalho do debug (D-234): o intervalo do anão ÷ 2,5 por 30 s,
        /// depois do piso (D-180), não para o estagiário (D-177). <c>false</c> sem AD no balanço ou com um ativo (D-188).
        /// </summary>
        public bool GrantAdBoost()
        {
            if (!CanWatchAd) return false;
            var ad = _strike.Ad;
            _state.Bonuses.AddOrRenew(new ActiveBonus(BonusKind.IntervalDivisor, ad.SpeedDivisor, ad.Seconds, ad.Seconds,
                                                      BonusSource.Ad, 0));
            OnBonusesChanged();
            AdBoostGranted?.Invoke();
            return true;
        }

        // ── Os anúncios do Boost (M9e E2, GDD v8.2 §3.5) ────────────────────────────────────────────────────────────

        /// <summary>
        /// Atalho do debug ("Anúncios curtos", M9e E2): as durações dos anúncios divididas por isto (60: 20 min viram 20 s),
        /// para ver acabar. 1 = as do balanço. Vale para o próximo anúncio assistido.
        /// </summary>
        public double AdDurationDivisor
        {
            get => _adDurationDivisor;
            set
            {
                if (double.IsNaN(value) || value < 1) throw new ArgumentOutOfRangeException(nameof(value));
                _adDurationDivisor = value;
            }
        }

        double _adDurationDivisor = 1;

        /// <summary>
        /// O anúncio pode ser assistido: o balanço tem ele. Com um ativo do mesmo tipo, de novo renova (D-244). O do tier 2
        /// do raro (D-250) só a partir do andar do raro, com o tier 2 no balanço e sem o Raro Dourado.
        /// </summary>
        public bool CanWatch(AdKind kind)
        {
            var ads = _strike.Ads;
            if (kind != AdKind.RareTier2) return ads.Has(kind);
            return ads.HasTier2 && _strike.Rare.IsEnabled && _strike.Rare.HasTier2 && IsRareUnlocked && !_permanents.HasGoldenRare;
        }

        /// <summary>Segundos que faltam do anúncio; 0 sem ele.</summary>
        public double AdSecondsLeft(AdKind kind) => _state.Bonuses.AdSecondsLeft(kind);

        /// <summary>
        /// O anúncio terminou (o RECOMPENSA, D-218) ou o atalho do debug: os bônus dele entram por um bônus por atributo
        /// (dano, lascas e dano do crítico ×, chance de crítico +), para o anão e, menos o crítico, para o estagiário
        /// (D-246). O mesmo anúncio de novo renova o tempo, sem somar (D-244); anúncios diferentes juntos: vale o maior em
        /// cada atributo (D-251). O do tier 2 deixa as cartas do raro no tier 2 enquanto durar. <c>false</c> se não pode.
        /// </summary>
        public bool WatchAd(AdKind kind)
        {
            if (!CanWatch(kind)) return false;
            var bonuses = _state.Bonuses;
            int tier = (int)kind;
            if (kind == AdKind.RareTier2)
            {
                double seconds = _strike.Ads.Tier2Seconds / _adDurationDivisor;
                bonuses.AddOrRenew(new ActiveBonus(BonusKind.RareTier2, 1, seconds, seconds, BonusSource.Ad, tier));
            }
            else
            {
                var offer = _strike.Ads.Get(kind);
                double seconds = offer.Seconds / _adDurationDivisor;
                bonuses.AddOrRenew(new ActiveBonus(BonusKind.DamageMultiplier, offer.Multiplier, seconds, seconds, BonusSource.Ad, tier));
                bonuses.AddOrRenew(new ActiveBonus(BonusKind.ShardsMultiplier, offer.Multiplier, seconds, seconds, BonusSource.Ad, tier));
                bonuses.AddOrRenew(new ActiveBonus(BonusKind.CritDamageMultiplier, offer.Multiplier, seconds, seconds, BonusSource.Ad, tier));
                if (offer.CritChance > 0)
                    bonuses.AddOrRenew(new ActiveBonus(BonusKind.CritChance, offer.CritChance, seconds, seconds, BonusSource.Ad, tier));
            }
            OnBonusesChanged();
            AdWatched?.Invoke(kind);
            return true;
        }

        /// <summary>
        /// O teto do offline agora (D-253, D-254): 4 h (6 h a partir do andar 20) e, com anúncios ativos, as horas extras
        /// deles, até 8 h. É o que conta se o app fechar agora.
        /// </summary>
        public double OfflineMaxSeconds => OfflineMining.MaxSeconds(_rules, _state, _permanents, _strike.Ads);

        /// <summary>Atalho do debug (D-226): a carta direto, sem janela. <c>false</c> se o balanço não tem essa carta.</summary>
        public bool GrantRareCard(RareCard card)
        {
            if (!_strike.Rare.Has(card)) return false;
            ApplyRareCard(card);
            return true;
        }

        /// <summary>2 cartas diferentes das que o balanço tem, sorteadas (D-190).</summary>
        RareWindow DrawWindow(int queued)
        {
            var cards = _strike.Rare.Cards;
            int count = cards.Count;
            int first = _rareRandom.NextInt(0, count);
            int second = _rareRandom.NextInt(0, count - 1);
            if (second >= first) second++;
            return new RareWindow(cards[first].Card, cards[second].Card, _strike.Rare.ChooseSeconds, queued: queued);
        }

        void NextRareWindow(int queued)
        {
            _state.RareWindow = queued > 0 && _strike.Rare.IsEnabled ? DrawWindow(queued - 1) : default;
            RareWindowChanged?.Invoke(_state.RareWindow);
        }

        /// <summary>Os 30 s acabaram sem escolha (D-186): sorteia uma das 2, aplica e deixa a janela mostrando, até o FECHAR.</summary>
        void AutoPickRareCard()
        {
            var window = _state.RareWindow;
            int pick = _rareRandom.NextInt(0, 2);
            _state.RareWindow = window.WithAutoPick(pick);
            ApplyRareCard(window.CardAt(pick));
            RareWindowChanged?.Invoke(_state.RareWindow);
        }

        /// <summary>A carta entra por 60 s (D-190); igual a uma ativa, só renova o tempo.</summary>
        void ApplyRareCard(RareCard card)
        {
            var rule = _strike.Rare.Get(card, RareTier); // o tier 2 com o Raro Dourado (D-286)
            _state.Bonuses.AddOrRenew(new ActiveBonus(rule.Kind, rule.Value, rule.Seconds, rule.Seconds, BonusSource.Rare, (int)card));
            OnBonusesChanged();
            RareCardApplied?.Invoke(card);
        }

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
        /// Atalho do debug: o próximo andar agora, como o Próximo Andar, mas sem a barra cheia. No último andar, nada muda.
        /// <c>false</c> se não há andar depois.
        /// </summary>
        public bool SkipToNextCave() => SkipToCave(_state.Cave + 1);

        /// <summary>
        /// Atalho do debug: direto para o andar <paramref name="cave"/> (no máximo o último), como o Próximo Andar.
        /// <c>false</c> se ele não fica mais abaixo.
        /// </summary>
        public bool SkipToCave(int cave)
        {
            cave = Math.Min(cave, _rules.CaveCount);
            if (cave <= _state.Cave) return false;
            EnterCave(cave);
            return true;
        }

        /// <summary>
        /// Atalho do debug (DBG → Andar → Ir ao andar…, M9c E1): qualquer andar de 1 ao último, acima ou abaixo do atual,
        /// como o Próximo Andar (transição, barra zerada, fila nova). Subir deixa o coração para trás. <c>false</c> se já
        /// está nele.
        /// </summary>
        public bool GoToFloor(int floor)
        {
            floor = Math.Max(1, Math.Min(floor, _rules.CaveCount));
            if (floor == _state.Cave) return false;
            if (floor < _rules.CaveCount) _state.LeaveHeart();
            EnterCave(floor);
            return true;
        }

        /// <summary>
        /// A barra e o andar da volta (M9c E4, D-246): os cristais do estagiário contam na barra e, com o automático, ele
        /// chega mais fundo. Offline não tem transição (D-191): o andar novo já está lá, com a fila de lá.
        /// </summary>
        void ApplyOfflineFloor(OfflineReport report)
        {
            if (!report.Hired || report.ToFloor < _state.Cave) return;
            bool moved = report.ToFloor > _state.Cave;
            if (!moved && report.EndBreaks == _state.CaveBreaks) return;
            if (moved) EnterCave(report.ToFloor, transition: false);
            _state.SetCaveBreaks(report.EndBreaks);
            CaveChanged?.Invoke(_state.Cave);
        }

        /// <summary>
        /// Entra no andar <paramref name="cave"/>: a transição abre primeiro (quem desenha ainda vê a fila antiga, para
        /// trocá-la no escuro), o andar muda, a barra zera, o recorde sobe, e a fila do andar novo nasce na tela, com a
        /// vida e as lascas de lá (o anão fica onde está e corre até o primeiro quando a transição acaba).
        /// </summary>
        void EnterCave(int cave, bool transition = true)
        {
            if (transition && _floorTransitionSeconds > 0) BeginFloorTransition(cave);
            _state.EnterCave(cave);
            _permanents.State.ReachFloor(cave);
            _track.SetCrystalValues(_rules.CrystalHealthIn(cave), _rules.CrystalRewardIn(cave));
            RefreshQueue();
            _stats = ComputeStats();
            StatsChanged?.Invoke(_stats);
            CaveChanged?.Invoke(cave);
        }

        /// <summary>A fila do andar velho sai; a do novo nasce como a do começo da partida, na tela (D-60).</summary>
        void RefreshQueue()
        {
            CancelBomb();
            if (_miner.Target != null)
            {
                _miner.Target = null;
                _miner.State = MinerState.Running;
                _recoveryLeft = 0;
                _swungAtTarget = false;
                TargetChanged?.Invoke(null);
            }
            _track.Clear();
            _spawnTimer = 0;
            _track.SpawnInitial(_miner.Distance);
            for (int i = 0; i < _track.Crystals.Count; i++) CrystalSpawned?.Invoke(_track.Crystals[i]);
            while (_track.IsBelowMinimum)
            {
                var crystal = _track.TrySpawn(_miner.Distance);
                if (crystal == null) break;
                CrystalSpawned?.Invoke(crystal);
            }
        }

        /// <summary>
        /// Abre a transição de andar (M9c E0, D-266): o jogo para até o tempo acabar. Uma transição correndo recomeça
        /// (atalho do debug no meio de outra).
        /// </summary>
        void BeginFloorTransition(int cave)
        {
            _floorTransitionLength = _floorTransitionSeconds;
            _floorTransitionLeft = _floorTransitionSeconds;
            FloorTransitionStarted?.Invoke(cave);
        }

        void EndFloorTransition()
        {
            _floorTransitionLeft = 0;
            _floorTransitionLength = 0;
            FloorTransitionEnded?.Invoke();
        }

        /// <summary>
        /// Crítico: a chance base do golpe (<see cref="StrikeRules"/>) mais a da linha Chance de Crítico
        /// (<see cref="MinerStats.CritBonus"/>, D-136). Com chance 0, nenhum número é sorteado.
        /// </summary>
        bool RollCritical()
        {
            if (ForceCritical || _state.Bonuses.GuaranteedCritical) return true; // o crítico garantido (D-178)
            double chance = CritChance();
            if (chance >= 1) return true; // 100% ou mais: sempre, sem sortear
            return chance > 0 && _critRandom.NextDouble() < chance;
        }

        /// <summary>A chance de crítico do golpe: a base (<see cref="StrikeRules"/>) mais a das linhas e cartas.</summary>
        double CritChance() => _strike.CritChance + _stats.CritBonus;

        /// <summary>
        /// O multiplicador do crítico: o das regras (GDD v8: ×1,5) mais o Dedo Fogoso (v8.4: +0,05 por nível, D-365). A chance
        /// acima de 100% não dá mais dano (D-366, sai a D-249): no v8.4 a chance para em 100%.
        /// A carta "Dano de Crítico ×2" do v8 (D-243) multiplica o resultado: ×1,5 vira ×3.
        /// </summary>
        double CritMultiplier() =>
            (_strike.CritMultiplier + _permanents.CritDamageBonus) * _state.Bonuses.CritDamageMultiplier;

        /// <summary>O que a loja mostra da linha agora: nível, custo, estado do botão e efeito "de → para".</summary>
        public UpgradePreview Preview(UpgradeId id) => UpgradePreview.For(_rules, _economy, _state, id, _permanents);

        /// <summary>O que a aba das permanentes mostra da linha (Marco 8b, E4).</summary>
        public PermanentPreview PreviewPermanent(PermanentId id)
        {
            var preview = PermanentPreview.For(_permanents, id, shopOpen: IsResting);
            // A Bomba (M9g, D-314): o valor é a fração da bomba em uso e a da próxima (a Pequena: 0,1 » 0,2), não o nível.
            if (id != PermanentId.Bomb || !HasBomb) return preview;
            int index = BombIndex;
            return preview.WithValues(_strike.Bombs.FractionAt(index), _strike.Bombs.FractionAt(preview.IsMaxed ? index : index + 1));
        }

        /// <summary>Resolve tudo o que vence agora, na ordem: nascimento, alvo (o primeiro da fila), golpe.</summary>
        void ProcessDueEvents()
        {
            bool changed;
            do
            {
                changed = false;
                if (_floorTransitionLeft > 0) return; // descendo de andar (D-266): nada vence até ela acabar

                if (ShouldAutoAdvance)
                {
                    EnterCave(_state.Cave + 1); // o avanço automático (P6): na hora em que a barra enche
                    changed = true;
                    continue;
                }

                if (_state.Bonuses.Count > 0 && _state.Bonuses.RemoveExpired(Epsilon))
                    OnBonusesChanged();

                if (_state.RareWindow.IsChoosing && _state.RareWindow.SecondsLeft <= Epsilon)
                {
                    AutoPickRareCard(); // 30 s sem escolha (D-186)
                    changed = true;
                }

                if (_bombInFlight && _bombFlightLeft <= Epsilon)
                {
                    LandBomb(); // D-308: o dano na chegada
                    changed = true;
                    continue; // a explosão pode ter enchido a barra (avanço automático) ou trocado o alvo
                }

                if (AutoBomb && CanThrowBomb)
                {
                    ThrowBomb(); // DBG → Bomba automática
                    changed = true;
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
                    if (_floorTransitionLeft > 0) return; // o golpe quebrou a parede: o estagiário espera a transição
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

            next = Math.Min(next, _state.Bonuses.NextExpiry);
            if (_state.RareWindow.IsChoosing) next = Math.Min(next, _state.RareWindow.SecondsLeft);
            if (_bombInFlight) next = Math.Min(next, Math.Max(0, _bombFlightLeft));
            if (AutoBomb && HasBomb && !IsBombCharged) next = Math.Min(next, BombChargeLeft); // a bomba automática sai cheia

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
            _state.Bonuses.Advance(step); // o fim de cada um é evento: some no ProcessDueEvents
            // Os 30 s da janela do raro só correm com o jogo aberto (D-222): o offline não passa por aqui.
            if (_state.RareWindow.IsChoosing) _state.RareWindow = _state.RareWindow.WithSecondsLeft(_state.RareWindow.SecondsLeft - step);
            // O contador do raro (D-283) só anda aqui: parado no descanso, na transição de andar e com o app fechado.
            if (IsRareCounting) _state.AdvanceRareTimer(step, RareInterval);

            // A bomba (D-313): a carga anda com a run (parada no descanso e na transição, que não passam por aqui) e a do
            // ar chega mais perto.
            if (HasBomb && _bombCharge < BombChargeSeconds)
                _bombCharge = Math.Min(BombChargeSeconds, _bombCharge + step);
            if (_bombInFlight) _bombFlightLeft = Math.Max(0, _bombFlightLeft - step);

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

        /// <summary>O relógio de nascimento espera: fila cheia.</summary>
        bool SpawnWaits => _track.IsFull;

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
                // Ainda terminando o golpe (D-126): o alvo novo já vale para a bomba; ele corre quando terminar.
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
            bool critical = RollCritical();
            double damage = _stats.Damage * (critical ? CritMultiplier() : 1);

            _state.RegisterHit();
            _swungAtTarget = true;
            bool broke = target.ApplyDamage(damage);
            HitLanded?.Invoke(new MiningHit(target, damage, critical));

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
            InternHitLanded?.Invoke(new MiningHit(target, damage, false));
            if (broke) Break(target);
        }

        /// <summary>
        /// A quebra de um cristal paga e conta na barra do andar. Quebrado pela bomba no meio da picaretada, o anão termina o
        /// golpe antes de correr (D-126, que era do toque); quebrado pela picareta, ou antes de ela descer nele, corre na hora.
        /// </summary>
        void Break(Crystal target, bool midSwing = false)
        {
            double recovery = SwingRecovery - _miner.HitTimer;
            bool finishSwing = midSwing && _miner.State == MinerState.Mining && _swungAtTarget && recovery > Epsilon;

            _track.Remove(target);
            _state.RegisterBreak(target.Reward * _stats.RewardMultiplier);
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
            ShardsChanged?.Invoke(_state.Shards);
            TargetChanged?.Invoke(null);
            if (target.IsRare) OpenRareWindow(); // D-184: a janela abre, sem pausar
        }
    }
}
