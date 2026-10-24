using System;

namespace MinersIdle.Core
{
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
            int minCrystals = 0)
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
    }
}
