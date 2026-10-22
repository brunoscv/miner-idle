namespace MinersIdle.Core
{
    public enum MinerState
    {
        /// <summary>Correndo: até o alvo ou, sem alvo, sem parar (a descida nunca para, D-55).</summary>
        Running,
        Mining,
    }

    /// <summary>
    /// O anão na trilha da descida. Só dados: quem muda distância, estado e alvo é a <see cref="MiningSimulation"/>; a
    /// Apresentação lê <see cref="Distance"/> a cada quadro e desenha (o código é dono do tempo, a animação só ilustra).
    /// </summary>
    public sealed class Miner
    {
        internal Miner() { }

        /// <summary>Unidades corridas desde o início da partida.</summary>
        public double Distance { get; internal set; }

        public MinerState State { get; internal set; }

        /// <summary>O alvo atual: o primeiro da fila; não muda até quebrar. <c>null</c> com a fila vazia.</summary>
        public Crystal Target { get; internal set; }

        /// <summary>Segundos acumulados desde o último golpe (ou desde que chegou ao alvo).</summary>
        public double HitTimer { get; internal set; }
    }
}
