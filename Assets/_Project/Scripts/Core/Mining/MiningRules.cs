using System;

namespace MinersIdle.Core
{
    /// <summary>Como o dano da Picareta do anão é calculado (<see cref="MinerStats"/>).</summary>
    public enum PickaxeFormula
    {
        /// <summary>Até o GDD v5: <c>dano base × Picareta.efeito^nível</c>.</summary>
        Multiply,
        /// <summary>
        /// GDD v7 (D-133): <c>Dano Base + Picareta.efeito × nível + taxa^(caverna − 1)</c>. O Dano Base é o dano base das
        /// regras; cada compra soma o efeito nele; a taxa é <see cref="MiningRules.PickaxeCaveGrowth"/>.
        /// </summary>
        CaveScaled,
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
            int caveCount = 1, double caveHealthGrowth = 1, double caveRewardGrowth = 1, int letradoBreaks = 100,
            double wallHealthFactor = 2,
            double tapReach = double.PositiveInfinity,
            ArrivalStrike arrivalStrike = ArrivalStrike.Wait,
            double swingRecovery = 0,
            PickaxeFormula pickaxeFormula = PickaxeFormula.Multiply, double pickaxeCaveGrowth = 1.2,
            int levelsPerBiome = 1)
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
            if (letradoBreaks < 0) throw new ArgumentOutOfRangeException(nameof(letradoBreaks));
            if (wallHealthFactor <= 0 || double.IsNaN(wallHealthFactor)) throw new ArgumentOutOfRangeException(nameof(wallHealthFactor));
            if (tapReach < 0 || double.IsNaN(tapReach)) throw new ArgumentOutOfRangeException(nameof(tapReach));
            if (swingRecovery < 0 || double.IsNaN(swingRecovery)) throw new ArgumentOutOfRangeException(nameof(swingRecovery));
            if (!Enum.IsDefined(typeof(PickaxeFormula), pickaxeFormula)) throw new ArgumentOutOfRangeException(nameof(pickaxeFormula));
            if (levelsPerBiome < 1) throw new ArgumentOutOfRangeException(nameof(levelsPerBiome));
            if (pickaxeCaveGrowth <= 0 || double.IsNaN(pickaxeCaveGrowth)) throw new ArgumentOutOfRangeException(nameof(pickaxeCaveGrowth));

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
            LetradoBreaks = letradoBreaks;
            WallHealthFactor = wallHealthFactor;
            TapReach = tapReach;
            ArrivalStrike = arrivalStrike;
            SwingRecovery = swingRecovery;
            PickaxeFormula = pickaxeFormula;
            PickaxeCaveGrowth = pickaxeCaveGrowth;
            LevelsPerBiome = levelsPerBiome;
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

        /// <summary>Número de cavernas (M8; GDD v5: 10). Com 1, o jogo não passa de caverna.</summary>
        public int CaveCount { get; }
        /// <summary>A vida do cristal multiplica por isto a cada caverna (D-92: 8).</summary>
        public double CaveHealthGrowth { get; }
        /// <summary>As lascas do cristal multiplicam por isto a cada caverna (D-92: 3).</summary>
        public double CaveRewardGrowth { get; }
        /// <summary>Cristais a quebrar na caverna para liberar o Letrado dela (D-93: 100).</summary>
        public int LetradoBreaks { get; }
        /// <summary>A vida da parede é isto × a vida do cristal da caverna nova (D-98: 2).</summary>
        public double WallHealthFactor { get; }

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

        /// <summary>Como o dano da Picareta é calculado: o do GDD v5 ou o do v7, que cresce com a caverna (D-133).</summary>
        public PickaxeFormula PickaxeFormula { get; }
        /// <summary>A "taxa" da D-133: no <see cref="Core.PickaxeFormula.CaveScaled"/>, o dano ganha taxa^(caverna − 1) (GDD v7: 1,2).</summary>
        public double PickaxeCaveGrowth { get; }

        /// <summary>
        /// Quantos níveis (cavernas) seguidos ficam no mesmo bioma: 1 até o GDD v5 (um bioma por caverna, D-102), 10 no GDD
        /// v7 (100 níveis, um bioma a cada 10, D-129).
        /// </summary>
        public int LevelsPerBiome { get; }

        /// <summary>O bioma do nível <paramref name="level"/>, a partir de 0: (nível − 1) ÷ <see cref="LevelsPerBiome"/>.</summary>
        public int BiomeOf(int level) => (Math.Max(1, level) - 1) / LevelsPerBiome;

        /// <summary>Vida do cristal na caverna <paramref name="cave"/> (1 = a primeira): base × crescimento^(c−1).</summary>
        public double CrystalHealthIn(int cave) => CrystalHealth * Math.Pow(CaveHealthGrowth, cave - 1);

        /// <summary>Lascas do cristal na caverna <paramref name="cave"/>: base × crescimento^(c−1).</summary>
        public double CrystalRewardIn(int cave) => CrystalReward * Math.Pow(CaveRewardGrowth, cave - 1);

        /// <summary>Vida da parede que leva à caverna <paramref name="cave"/> (D-98): fator × a vida do cristal dela.</summary>
        public double WallHealthIn(int cave) => WallHealthFactor * CrystalHealthIn(cave);
    }
}
