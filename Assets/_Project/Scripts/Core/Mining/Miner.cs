namespace MinersIdle.Core
{
    public enum MinerState
    {
        /// <summary>Sem cristal na tela: fica onde está.</summary>
        Idle,
        Walking,
        Mining,
    }

    /// <summary>
    /// O anão no mapa. Só dados: quem muda posição, estado e alvo é a <see cref="MiningSimulation"/>; a Apresentação
    /// lê <see cref="Position"/> a cada quadro e desenha (o código é dono do tempo, a animação só ilustra).
    /// </summary>
    public sealed class Miner
    {
        internal Miner(Vec2 position)
        {
            Position = position;
        }

        public Vec2 Position { get; internal set; }
        public MinerState State { get; internal set; }

        /// <summary>O alvo atual; não muda até quebrar (D-27). <c>null</c> quando parado.</summary>
        public Crystal Target { get; internal set; }

        /// <summary>Segundos acumulados desde o último golpe (ou desde que chegou ao alvo).</summary>
        public double HitTimer { get; internal set; }
    }
}
