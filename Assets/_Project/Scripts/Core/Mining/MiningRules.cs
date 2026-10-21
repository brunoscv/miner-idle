using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// Valores do loop de mineração no mapa, somente leitura. No M2 vêm do asset <c>Balance_Prototipo</c> (D-29: valores de
    /// protótipo, não balanceamento). Distâncias em unidades de mundo; tempos em segundos.
    /// </summary>
    public sealed class MiningRules
    {
        public MiningRules(
            Vec2 mapMin, Vec2 mapMax, Vec2 minerStart,
            int initialCrystals, int maxCrystals, double spawnInterval,
            double minCrystalSpacing, double minDistanceFromMiner,
            double crystalHealth, double crystalReward,
            double damage, double hitInterval,
            double walkSpeed, double miningRange)
        {
            if (mapMax.X <= mapMin.X || mapMax.Y <= mapMin.Y) throw new ArgumentException("A área do mapa está vazia.");
            if (minerStart.X < mapMin.X || minerStart.X > mapMax.X || minerStart.Y < mapMin.Y || minerStart.Y > mapMax.Y)
                throw new ArgumentException("O anão precisa começar dentro do mapa.");
            if (initialCrystals < 0) throw new ArgumentOutOfRangeException(nameof(initialCrystals));
            if (maxCrystals < 1 || maxCrystals < initialCrystals) throw new ArgumentOutOfRangeException(nameof(maxCrystals));
            if (spawnInterval <= 0) throw new ArgumentOutOfRangeException(nameof(spawnInterval));
            if (minCrystalSpacing < 0) throw new ArgumentOutOfRangeException(nameof(minCrystalSpacing));
            if (minDistanceFromMiner < 0) throw new ArgumentOutOfRangeException(nameof(minDistanceFromMiner));
            if (crystalHealth <= 0) throw new ArgumentOutOfRangeException(nameof(crystalHealth));
            if (crystalReward < 0) throw new ArgumentOutOfRangeException(nameof(crystalReward));
            if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (hitInterval <= 0) throw new ArgumentOutOfRangeException(nameof(hitInterval));
            if (walkSpeed <= 0) throw new ArgumentOutOfRangeException(nameof(walkSpeed));
            if (miningRange < 0) throw new ArgumentOutOfRangeException(nameof(miningRange));

            MapMin = mapMin;
            MapMax = mapMax;
            MinerStart = minerStart;
            InitialCrystals = initialCrystals;
            MaxCrystals = maxCrystals;
            SpawnInterval = spawnInterval;
            MinCrystalSpacing = minCrystalSpacing;
            MinDistanceFromMiner = minDistanceFromMiner;
            CrystalHealth = crystalHealth;
            CrystalReward = crystalReward;
            Damage = damage;
            HitInterval = hitInterval;
            WalkSpeed = walkSpeed;
            MiningRange = miningRange;
        }

        /// <summary>Canto inferior esquerdo da área onde cristais nascem.</summary>
        public Vec2 MapMin { get; }
        /// <summary>Canto superior direito da área onde cristais nascem.</summary>
        public Vec2 MapMax { get; }
        public Vec2 MinerStart { get; }

        /// <summary>Cristais na tela no início (todos no lote 1).</summary>
        public int InitialCrystals { get; }
        /// <summary>Com este número na tela, para de nascer até alguém quebrar.</summary>
        public int MaxCrystals { get; }
        public double SpawnInterval { get; }
        public double MinCrystalSpacing { get; }
        public double MinDistanceFromMiner { get; }

        public double CrystalHealth { get; }
        public double CrystalReward { get; }

        /// <summary>Dano por golpe (o DANO do HUD).</summary>
        public double Damage { get; }
        /// <summary>Segundos entre golpes (a VELOCIDADE do HUD).</summary>
        public double HitInterval { get; }
        public double WalkSpeed { get; }
        /// <summary>O anão para a esta distância do centro do cristal e começa a bater.</summary>
        public double MiningRange { get; }
    }
}
