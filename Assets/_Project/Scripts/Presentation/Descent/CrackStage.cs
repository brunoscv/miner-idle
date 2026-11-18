namespace MinersIdle.Presentation
{
    /// <summary>
    /// O estágio do desenho do cristal pela vida (M9f, D-300): a vida é do núcleo, o estágio só ilustra. Inteiro só com a
    /// vida cheia; depois, por terços: racha 1 acima de 2/3, racha 2 acima de 1/3, racha 3 no último terço. Assim todo
    /// golpe que não quebra já mostra uma rachadura.
    /// </summary>
    public static class CrackStage
    {
        /// <summary>Quantos estágios a folha tem por cor (as colunas: inteiro, racha 1, racha 2, racha 3).</summary>
        public const int Count = 4;

        public static int Of(double health, double maxHealth)
        {
            if (maxHealth <= 0 || health >= maxHealth) return 0;
            double ratio = health / maxHealth;
            if (ratio > 2.0 / 3.0) return 1;
            if (ratio > 1.0 / 3.0) return 2;
            return 3;
        }

        /// <summary>
        /// Um golpe que tirou <paramref name="damage"/> e deixou <paramref name="healthAfter"/> passou para um estágio novo
        /// (D-303)? Golpe que quebrou não conta: a quebra tem o efeito dela.
        /// </summary>
        public static bool Crossed(double healthAfter, double damage, double maxHealth, out int stage)
        {
            stage = Of(healthAfter, maxHealth);
            if (healthAfter <= 0) return false;
            double before = healthAfter + System.Math.Max(0, damage);
            return stage > Of(before > maxHealth ? maxHealth : before, maxHealth);
        }
    }
}
