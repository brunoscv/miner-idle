using System;

namespace MinersIdle.Core
{
    /// <summary>Como o dano da Picareta do anão é calculado (<see cref="MinerStats"/>).</summary>
    public enum PickaxeFormula
    {
        /// <summary>Até o GDD v5: <c>dano base × Picareta.efeito^nível</c>.</summary>
        Multiply,
        /// <summary>
        /// GDD v7 e v8 (D-201, D-202): <c>Dano Base + Picareta.efeito × compras + taxa^(nível da Picareta − 1)</c>, com
        /// nível = 1 + compras; o andar não entra. Sem compra, 1 + 0 + taxa^0 = 2. (A leitura pela caverna da D-133 saiu
        /// no v8, D-256.)
        /// </summary>
        LevelScaled = 2,
    }

    /// <summary>Como as lascas do cristal crescem de um andar para o outro.</summary>
    public enum RewardFormula
    {
        /// <summary>Até o GDD v7: <c>lascas base × crescimento^(andar − 1)</c> (<see cref="MiningRules.CaveRewardGrowth"/>).</summary>
        Growth,
        /// <summary>
        /// GDD v8 (D-238): <c>vida do andar × fator × max(1, andar − 1)</c>, com o fator nas lascas base (0,2): andar 1 e 2,
        /// vida × 0,2; andar 3, vida × 0,4; andar 10, vida × 1,8.
        /// </summary>
        FloorScaled,
    }

    /// <summary>
    /// Valores do loop de mineração na descida (M6, D-52), somente leitura. Vêm do asset <c>Balance_Prototipo</c> (D-29:
    /// valores de protótipo, não balanceamento). Distâncias em unidades da trilha (1 un = 0,5 m na tela, D-58); tempos
    /// em segundos.
    /// </summary>
    public sealed class MiningRules
    {
        public MiningRules(
            int initialCrystals, double firstCrystalDistance, int maxCrystals, double spawnInterval,
            double minCrystalSpacing, double spawnDistance,
            double crystalHealth, double crystalReward,
            double damage, double hitInterval,
            double walkSpeed, double miningRange,
            int minCrystals = 0,
            int caveCount = 1, double caveHealthGrowth = 1, double caveRewardGrowth = 1, int floorBreaks = 100,
            double tapReach = double.PositiveInfinity,
            ArrivalStrike arrivalStrike = ArrivalStrike.Wait,
            double swingRecovery = 0,
            PickaxeFormula pickaxeFormula = PickaxeFormula.Multiply, double pickaxeCaveGrowth = 1.2,
            int levelsPerBiome = 1, double offlineMaxSeconds = 6 * 3600,
            RewardFormula rewardFormula = RewardFormula.Growth, double offlineDeepMaxSeconds = 0, int offlineDeepFloor = 0,
            int autoAdvanceFloor = 0, int internFloor = 0, int rareFloor = 0)
        {
            if (initialCrystals < 0) throw new ArgumentOutOfRangeException(nameof(initialCrystals));
            if (firstCrystalDistance < 0) throw new ArgumentOutOfRangeException(nameof(firstCrystalDistance));
            if (maxCrystals < 1 || maxCrystals < initialCrystals) throw new ArgumentOutOfRangeException(nameof(maxCrystals));
            if (spawnInterval <= 0) throw new ArgumentOutOfRangeException(nameof(spawnInterval));
            if (minCrystalSpacing < 0) throw new ArgumentOutOfRangeException(nameof(minCrystalSpacing));
            if (spawnDistance < 0) throw new ArgumentOutOfRangeException(nameof(spawnDistance));
            if (crystalHealth <= 0) throw new ArgumentOutOfRangeException(nameof(crystalHealth));
            if (crystalReward < 0) throw new ArgumentOutOfRangeException(nameof(crystalReward));
            if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (hitInterval <= 0) throw new ArgumentOutOfRangeException(nameof(hitInterval));
            if (walkSpeed <= 0) throw new ArgumentOutOfRangeException(nameof(walkSpeed));
            if (miningRange < 0) throw new ArgumentOutOfRangeException(nameof(miningRange));
            if (minCrystals < 0 || minCrystals > maxCrystals) throw new ArgumentOutOfRangeException(nameof(minCrystals));
            if (caveCount < 1) throw new ArgumentOutOfRangeException(nameof(caveCount));
            if (caveHealthGrowth <= 0 || double.IsNaN(caveHealthGrowth)) throw new ArgumentOutOfRangeException(nameof(caveHealthGrowth));
            if (caveRewardGrowth <= 0 || double.IsNaN(caveRewardGrowth)) throw new ArgumentOutOfRangeException(nameof(caveRewardGrowth));
            if (floorBreaks < 0) throw new ArgumentOutOfRangeException(nameof(floorBreaks));
            if (tapReach < 0 || double.IsNaN(tapReach)) throw new ArgumentOutOfRangeException(nameof(tapReach));
            if (swingRecovery < 0 || double.IsNaN(swingRecovery)) throw new ArgumentOutOfRangeException(nameof(swingRecovery));
            if (!Enum.IsDefined(typeof(PickaxeFormula), pickaxeFormula)) throw new ArgumentOutOfRangeException(nameof(pickaxeFormula));
            if (levelsPerBiome < 1) throw new ArgumentOutOfRangeException(nameof(levelsPerBiome));
            if (offlineMaxSeconds < 0 || double.IsNaN(offlineMaxSeconds)) throw new ArgumentOutOfRangeException(nameof(offlineMaxSeconds));
            if (pickaxeCaveGrowth <= 0 || double.IsNaN(pickaxeCaveGrowth)) throw new ArgumentOutOfRangeException(nameof(pickaxeCaveGrowth));
            if (!Enum.IsDefined(typeof(RewardFormula), rewardFormula)) throw new ArgumentOutOfRangeException(nameof(rewardFormula));
            if (offlineDeepMaxSeconds < 0 || double.IsNaN(offlineDeepMaxSeconds)) throw new ArgumentOutOfRangeException(nameof(offlineDeepMaxSeconds));
            if (offlineDeepFloor < 0) throw new ArgumentOutOfRangeException(nameof(offlineDeepFloor));
            if (autoAdvanceFloor < 0) throw new ArgumentOutOfRangeException(nameof(autoAdvanceFloor));
            if (internFloor < 0) throw new ArgumentOutOfRangeException(nameof(internFloor));
            if (rareFloor < 0) throw new ArgumentOutOfRangeException(nameof(rareFloor));

            InitialCrystals = initialCrystals;
            FirstCrystalDistance = firstCrystalDistance;
            MaxCrystals = maxCrystals;
            SpawnInterval = spawnInterval;
            MinCrystalSpacing = minCrystalSpacing;
            SpawnDistance = spawnDistance;
            CrystalHealth = crystalHealth;
            CrystalReward = crystalReward;
            Damage = damage;
            HitInterval = hitInterval;
            WalkSpeed = walkSpeed;
            MiningRange = miningRange;
            MinCrystals = minCrystals;
            CaveCount = caveCount;
            CaveHealthGrowth = caveHealthGrowth;
            CaveRewardGrowth = caveRewardGrowth;
            FloorBreaks = floorBreaks;
            RewardFormula = rewardFormula;
            OfflineDeepMaxSeconds = offlineDeepMaxSeconds;
            OfflineDeepFloor = offlineDeepFloor;
            AutoAdvanceFloor = autoAdvanceFloor;
            InternFloor = internFloor;
            RareFloor = rareFloor;
            TapReach = tapReach;
            ArrivalStrike = arrivalStrike;
            SwingRecovery = swingRecovery;
            PickaxeFormula = pickaxeFormula;
            PickaxeCaveGrowth = pickaxeCaveGrowth;
            LevelsPerBiome = levelsPerBiome;
            OfflineMaxSeconds = offlineMaxSeconds;
        }

        /// <summary>Cristais na fila no início (já na tela).</summary>
        public int InitialCrystals { get; }
        /// <summary>Distância do anão ao primeiro cristal inicial (D-60: 3 un).</summary>
        public double FirstCrystalDistance { get; }
        /// <summary>Com este número na fila, para de nascer até alguém quebrar.</summary>
        public int MaxCrystals { get; }
        public double SpawnInterval { get; }
        /// <summary>
        /// A fila nunca fica com menos que isto (D-80): abaixo, nasce um cristal na hora, sem esperar o relógio. Com o
        /// anão forte, ele quebra mais rápido do que o relógio repõe, e sem isto ficaria correndo sem cristais. 0 desliga.
        /// </summary>
        public int MinCrystals { get; }
        /// <summary>Espaço mínimo entre dois cristais da fila (D-60: 2 un).</summary>
        public double MinCrystalSpacing { get; }
        /// <summary>O cristal novo nasce a pelo menos esta distância à frente do anão, fora da tela (D-60: 12 un).</summary>
        public double SpawnDistance { get; }

        public double CrystalHealth { get; }
        public double CrystalReward { get; }

        /// <summary>Dano base por golpe, sem melhorias (o atual está em <see cref="MinerStats.Damage"/>).</summary>
        public double Damage { get; }
        /// <summary>Segundos entre golpes, sem melhorias (o atual está em <see cref="MinerStats.HitInterval"/>).</summary>
        public double HitInterval { get; }
        /// <summary>Corrida base, em un/s, sem melhorias (a atual está em <see cref="MinerStats.WalkSpeed"/>).</summary>
        public double WalkSpeed { get; }
        /// <summary>O anão para a esta distância do cristal e começa a bater.</summary>
        public double MiningRange { get; }

        /// <summary>Número de andares (M8: cavernas; GDD v8: 100). Com 1, o jogo não passa de andar.</summary>
        public int CaveCount { get; }
        /// <summary>A vida do cristal multiplica por isto a cada caverna (D-92: 8).</summary>
        public double CaveHealthGrowth { get; }
        /// <summary>As lascas do cristal multiplicam por isto a cada caverna (D-92: 3) na <see cref="Core.RewardFormula.Growth"/>.</summary>
        public double CaveRewardGrowth { get; }
        /// <summary>Como as lascas crescem por andar: a do crescimento (até o v7) ou a do v8 (D-238).</summary>
        public RewardFormula RewardFormula { get; }
        /// <summary>
        /// Cristais por andar para o Próximo Andar (GDD v8, D-239: 10): o andar <i>n</i> pede <c>n × isto</c>, menos a
        /// Memória (<see cref="Permanents.FloorRequirement"/>).
        /// </summary>
        public int FloorBreaks { get; }

        /// <summary>
        /// O toque só tira vida de um alvo a até esta distância do anão: o alvo que já está na tela (D-121). Mais longe, o
        /// toque só conta no combo. Infinito = o toque vale a qualquer distância (a D-48a, antes de 2026-11-01).
        /// </summary>
        public double TapReach { get; }

        /// <summary>Quando sai o primeiro golpe num cristal (D-124). <see cref="Core.ArrivalStrike.Wait"/> = o jogo até 2026-11-01.</summary>
        public ArrivalStrike ArrivalStrike { get; }

        /// <summary>
        /// Quanto dura a volta da picareta depois do golpe, em segundos (D-126): se o toque quebra o cristal antes disso, o
        /// anão termina o golpe parado e só então corre. Nunca passa do intervalo do golpe. 0 = corre na hora (o jogo até
        /// 2026-11-02).
        /// </summary>
        public double SwingRecovery { get; }

        /// <summary>Como o dano da Picareta é calculado: o do GDD v5, o do v7 pelo nível da compra (D-201) ou pela caverna (D-133).</summary>
        public PickaxeFormula PickaxeFormula { get; }
        /// <summary>
        /// A "taxa" da Picareta (GDD v7: 1,2): o dano ganha taxa^(nível da Picareta − 1) no
        /// <see cref="Core.PickaxeFormula.LevelScaled"/> e taxa^(caverna − 1) no <see cref="Core.PickaxeFormula.CaveScaled"/>.
        /// </summary>
        public double PickaxeCaveGrowth { get; }

        /// <summary>
        /// Quantos níveis (cavernas) seguidos ficam no mesmo bioma: 1 até o GDD v5 (um bioma por caverna, D-102), 10 no GDD
        /// v7 (100 níveis, um bioma a cada 10, D-129).
        /// </summary>
        public int LevelsPerBiome { get; }

        /// <summary>Teto do tempo offline que conta (M9, D-189: 6 h; GDD v8: 4 h); o que passar disso se perde.</summary>
        public double OfflineMaxSeconds { get; }
        /// <summary>O teto do offline depois do andar <see cref="OfflineDeepFloor"/> (GDD v8: 6 h). 0 = o mesmo de sempre.</summary>
        public double OfflineDeepMaxSeconds { get; }
        /// <summary>O andar que, alcançado uma vez, sobe o teto do offline (GDD v8: 20, D-253). 0 = nunca.</summary>
        public int OfflineDeepFloor { get; }

        /// <summary>O teto do offline para quem já chegou ao andar <paramref name="deepestFloor"/> (GDD v8: 4 h; 6 h do 20 em diante).</summary>
        public double OfflineMaxSecondsFor(int deepestFloor) =>
            OfflineDeepFloor > 0 && OfflineDeepMaxSeconds > 0 && deepestFloor >= OfflineDeepFloor ? OfflineDeepMaxSeconds : OfflineMaxSeconds;

        // ── Marcos de andar (M9c E3, D-253): valem para sempre a partir do andar mais fundo já alcançado ──────────────

        /// <summary>O andar que libera o avanço automático (GDD v8: 5, P6). 0 = o balanço não tem avanço automático.</summary>
        public int AutoAdvanceFloor { get; }
        /// <summary>O andar que libera contratar o estagiário na LOJA (GDD v8: 5, P7). 0 = desde o começo.</summary>
        public int InternFloor { get; }
        /// <summary>O andar depois do qual o cristal raro nasce (GDD v8: 10). 0 = desde o começo.</summary>
        public int RareFloor { get; }

        /// <summary>Se o avanço automático já existe para quem chegou ao andar <paramref name="deepestFloor"/>.</summary>
        public bool IsAutoAdvanceUnlocked(int deepestFloor) => AutoAdvanceFloor > 0 && deepestFloor >= AutoAdvanceFloor;
        /// <summary>Se a LOJA já contrata o estagiário para quem chegou ao andar <paramref name="deepestFloor"/>.</summary>
        public bool IsInternUnlocked(int deepestFloor) => deepestFloor >= InternFloor;
        /// <summary>Se o raro já nasce para quem chegou ao andar <paramref name="deepestFloor"/>.</summary>
        public bool IsRareUnlocked(int deepestFloor) => deepestFloor >= RareFloor;

        /// <summary>
        /// Cristais que o andar <paramref name="floor"/> pede para o Próximo Andar, antes da Memória: <c>andar × </c>
        /// <see cref="FloorBreaks"/> (GDD v8: o andar 1 pede 10, o 100 pede 1.000).
        /// </summary>
        public long FloorBreaksIn(int floor) => (long)Math.Max(1, floor) * FloorBreaks;

        /// <summary>O bioma do nível <paramref name="level"/>, a partir de 0: (nível − 1) ÷ <see cref="LevelsPerBiome"/>.</summary>
        public int BiomeOf(int level) => (Math.Max(1, level) - 1) / LevelsPerBiome;

        /// <summary>Vida do cristal na caverna <paramref name="cave"/> (1 = a primeira): base × crescimento^(c−1).</summary>
        public double CrystalHealthIn(int cave) => CrystalHealth * Math.Pow(CaveHealthGrowth, cave - 1);

        /// <summary>
        /// Lascas do cristal no andar <paramref name="cave"/>: base × crescimento^(c−1) até o v7; no v8 (D-238), vida do
        /// andar × base × max(1, andar − 1).
        /// </summary>
        public double CrystalRewardIn(int cave) => RewardFormula == RewardFormula.FloorScaled
            ? CrystalHealthIn(cave) * CrystalReward * Math.Max(1, cave - 1)
            : CrystalReward * Math.Pow(CaveRewardGrowth, cave - 1);
    }
}
