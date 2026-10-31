using System;
using System.Globalization;

namespace MinersIdle.Core
{
    /// <summary>
    /// Um cristal da fila da descida (M6, D-52): um alvo, com uma vida e uma recompensa. <see cref="Id"/> é a ordem de
    /// nascimento; <see cref="Distance"/> é onde ele está na trilha, em unidades (1 un = 0,5 m na tela, D-58). Desde o M8
    /// (E2), a parede da passagem de caverna também é um alvo da fila (<see cref="IsWall"/>): apanha como um cristal, mas
    /// não paga lascas nem conta nos 100 cristais (D-108).
    /// </summary>
    public sealed class Crystal
    {
        /// <summary>Vida abaixo desta fração da vida máxima conta como zero (arredondamento de <c>double</c>).</summary>
        const double BrokenFraction = 1e-9;

        public Crystal(int id, double distance, double maxHealth, double reward, bool isWall = false)
        {
            if (double.IsNaN(distance) || double.IsInfinity(distance)) throw new ArgumentOutOfRangeException(nameof(distance));
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth), "A vida precisa ser positiva.");
            if (reward < 0) throw new ArgumentOutOfRangeException(nameof(reward), "A recompensa não pode ser negativa.");

            Id = id;
            Distance = distance;
            MaxHealth = maxHealth;
            Health = maxHealth;
            Reward = reward;
            IsWall = isWall;
        }

        public int Id { get; }
        public double Distance { get; }
        public double MaxHealth { get; }
        public double Health { get; private set; }
        public double Reward { get; }

        /// <summary>A parede da passagem de caverna (M8, D-98), não um cristal.</summary>
        public bool IsWall { get; }

        public bool IsBroken => Health <= 0;

        /// <summary>Tira vida (nunca abaixo de zero). Devolve <c>true</c> só no golpe que quebra o cristal.</summary>
        public bool ApplyDamage(double damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage), "O dano não pode ser negativo.");
            if (IsBroken) return false;

            Health = Math.Max(0, Health - damage);
            // Toques de 10% somados em double deixam restos como 1e-16 (0,1 × 10 ≠ 1): resto desprezível conta como zero.
            if (Health <= MaxHealth * BrokenFraction) Health = 0;
            return IsBroken;
        }

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0}#{1} a {2:0.###} un {3}/{4}", IsWall ? "Wall" : "Crystal", Id, Distance, Health, MaxHealth);
    }
}
