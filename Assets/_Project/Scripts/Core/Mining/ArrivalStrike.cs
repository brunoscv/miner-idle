namespace MinersIdle.Core
{
    /// <summary>
    /// Quando sai o primeiro golpe num cristal (D-124, "golpe fluido"). Até 2026-11-01 o relógio do golpe zerava na
    /// chegada: o anão chegava e ficava um intervalo inteiro parado antes de bater (<see cref="Wait"/>).
    /// </summary>
    public enum ArrivalStrike
    {
        /// <summary>O relógio zera na chegada: o primeiro golpe sai um intervalo depois (o jogo até a D-124).</summary>
        Wait,

        /// <summary>
        /// O relógio do golpe enche também correndo, até um intervalo (A1): quem correu o intervalo inteiro chega batendo;
        /// com o cristal perto, espera só o que falta. Nunca mais de um golpe por intervalo.
        /// </summary>
        Charge,

        /// <summary>O primeiro golpe sai no instante da chegada (A2); os seguintes, a cada intervalo.</summary>
        Immediate,
    }
}
