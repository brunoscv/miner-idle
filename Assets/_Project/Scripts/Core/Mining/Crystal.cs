using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// Um grupo de cristais no mapa: um alvo, com uma vida e uma recompensa (D-26).
    /// <see cref="Id"/> é a ordem de nascimento; <see cref="Batch"/> é o número do lote da regra de alvo (D-27).
    /// </summary>
    public sealed class Crystal
    {
        /// <summary>Vida abaixo desta fração da vida máxima conta como zero (arredondamento de <c>double</c>).</summary>
        const double BrokenFraction = 1e-9;

        public Crystal(int id, int batch, Vec2 position, double maxHealth, double reward)
        {
            if (batch < 1) throw new ArgumentOutOfRangeException(nameof(batch), "O lote começa em 1.");
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth), "A vida precisa ser positiva.");
            if (reward < 0) throw new ArgumentOutOfRangeException(nameof(reward), "A recompensa não pode ser negativa.");

            Id = id;
            Batch = batch;
            Position = position;
            MaxHealth = maxHealth;
            Health = maxHealth;
            Reward = reward;
        }

        public int Id { get; }
        public int Batch { get; }
        public Vec2 Position { get; }
        public double MaxHealth { get; }
        public double Health { get; private set; }
        public double Reward { get; }

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

        public override string ToString() => $"Crystal#{Id} (lote {Batch}) {Position} {Health}/{MaxHealth}";
    }
}
