using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// A janela do cristal raro (M9b E4; D-186, D-190, D-221, D-222): as 2 cartas diferentes, o tempo que falta para
    /// escolher (só corre com o app aberto), a carta que saiu (escolhida ou sorteada) e quantos raros esperam na fila.
    /// Fica no <see cref="GameState"/>: vai para o save e some no descanso (D-224). <c>default</c> = fechada.
    /// </summary>
    public readonly struct RareWindow
    {
        public RareWindow(RareCard first, RareCard second, double secondsLeft, int chosen = -1, bool autoPicked = false,
                          int queued = 0)
        {
            IsOpen = true;
            First = first;
            Second = second;
            SecondsLeft = Math.Max(0, secondsLeft);
            Chosen = chosen < 0 ? -1 : Math.Min(chosen, 1);
            AutoPicked = autoPicked && chosen >= 0;
            Queued = Math.Max(0, queued);
        }

        public bool IsOpen { get; }
        public RareCard First { get; }
        public RareCard Second { get; }
        /// <summary>Segundos para escolher; parado depois que a carta saiu.</summary>
        public double SecondsLeft { get; }
        /// <summary>A carta que saiu (0 ou 1), ou -1 enquanto o jogador escolhe.</summary>
        public int Chosen { get; }
        /// <summary>A carta saiu no sorteio dos 30 s (D-186): a janela fica aberta mostrando, até o FECHAR.</summary>
        public bool AutoPicked { get; }
        /// <summary>Raros quebrados com esta janela aberta, esperando a vez (D-221).</summary>
        public int Queued { get; }

        /// <summary>Aberta, esperando a escolha (o relógio corre).</summary>
        public bool IsChoosing => IsOpen && Chosen < 0;

        public RareCard CardAt(int index) => index == 0 ? First : Second;

        internal RareWindow WithSecondsLeft(double seconds) => new RareWindow(First, Second, seconds, Chosen, AutoPicked, Queued);
        internal RareWindow WithQueued(int queued) => new RareWindow(First, Second, SecondsLeft, Chosen, AutoPicked, queued);
        internal RareWindow WithAutoPick(int chosen) => new RareWindow(First, Second, 0, chosen, true, Queued);

        /// <summary>Uma janela de um save, corrigida: cartas que não existem ou iguais fecham a janela.</summary>
        public static RareWindow Restore(bool isOpen, int first, int second, double secondsLeft, int chosen, bool autoPicked,
                                         int queued)
        {
            if (!isOpen || !Enum.IsDefined(typeof(RareCard), first) || !Enum.IsDefined(typeof(RareCard), second) || first == second)
                return default;
            // Tempo estragado: a janela sorteia no próximo instante (D-186), em vez de ficar aberta para sempre.
            double left = double.IsNaN(secondsLeft) || double.IsInfinity(secondsLeft) ? 0 : Math.Max(0, secondsLeft);
            // Escolhida, mas sem ser no sorteio, a janela já teria fechado: só o sorteio fica aberto para o FECHAR.
            if (chosen >= 0 && !autoPicked) chosen = -1;
            return new RareWindow((RareCard)first, (RareCard)second, left, chosen, autoPicked, queued);
        }
    }
}
