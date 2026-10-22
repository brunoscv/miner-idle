using System;

namespace MinersIdle.Presentation
{
    /// <summary>O que aconteceu num passo da <see cref="DescentPreview"/>, para o desenho reagir (faíscas, estilhaços).</summary>
    public struct DescentStep
    {
        /// <summary>A picareta acertou o cristal da frente neste passo.</summary>
        public bool Hit;
        /// <summary>O cristal da frente quebrou neste passo.</summary>
        public bool Broke;
        /// <summary>Onde estava, na trilha, o cristal que quebrou.</summary>
        public double BrokeAt;
        public int BrokeColor;
    }

    /// <summary>
    /// O "loop" da cena da descida (M6, E1): <b>só visual</b>, como o simulador da proposta (D-51). O anão corre, para a
    /// <see cref="Reach"/> do cristal da frente, bate <see cref="HitsToBreak"/> vezes no ritmo de <see cref="SwingSeconds"/>,
    /// quebra e volta a correr. Não há lascas, compras nem regra de jogo: a fila de verdade, no núcleo, é a E2.
    ///
    /// Distâncias em metros na trilha: <see cref="Progress"/> é quanto o anão já correu; cada cristal tem a sua posição
    /// à frente. Sem alocação por passo (fila circular de tamanho fixo).
    /// </summary>
    public sealed class DescentPreview
    {
        public const int Capacity = 8;
        public const int HitsToBreak = 3;
        public const float SwingSeconds = 0.55f;

        /// <summary>Fração do balanço em que a picareta chega ao cristal (o 3º quadro do golpe).</summary>
        public const float HitPhase = 0.68f;

        /// <summary>O anão para a esta distância do cristal.</summary>
        public const double Reach = 0.85;

        /// <summary>Até onde, à frente do anão, a fila tem cristais (passa da câmera com folga).</summary>
        public const double LookAhead = 30;

        const double FirstCrystalAt = 4;
        const double MinGap = 3.6;
        const double GapRange = 1.6;

        readonly double[] _crystalAt = new double[Capacity];
        readonly int[] _crystalColor = new int[Capacity];
        int _first;
        int _count;
        int _spawned;
        double _nextAt = FirstCrystalAt;

        public DescentPreview()
        {
            Refill();
        }

        /// <summary>Velocidade de corrida na tela, em m/s (D-58: 4 un/s da regra × 0,5 m).</summary>
        public float RunSpeed { get; set; } = 2f;

        /// <summary>Metros corridos desde o início.</summary>
        public double Progress { get; private set; }

        public bool Mining { get; private set; }

        /// <summary>Segundos desde que o anão começou a bater no cristal da frente.</summary>
        public float SwingTime { get; private set; }

        public int Hits { get; private set; }

        /// <summary>Cristais quebrados desde o início.</summary>
        public int Broken { get; private set; }

        public int CrystalCount => _count;

        /// <summary>Posição na trilha do <paramref name="index"/>-ésimo cristal da fila (0 = o da frente).</summary>
        public double CrystalAt(int index) => _crystalAt[Slot(index)];

        public int CrystalColor(int index) => _crystalColor[Slot(index)];

        /// <summary>Quadro do golpe (0 a 3) pelo tempo do balanço.</summary>
        public int SwingFrame
        {
            get
            {
                float q = SwingTime / SwingSeconds;
                q -= (float)Math.Floor(q);
                if (q < 0.55f) return 0;
                if (q < HitPhase) return 1;
                if (q < 0.85f) return 2;
                return 3;
            }
        }

        public DescentStep Step(float dt)
        {
            var step = new DescentStep();
            if (dt <= 0f) return step;

            if (!Mining)
            {
                Progress += RunSpeed * dt;
                if (_count > 0 && _crystalAt[_first] - Progress <= Reach)
                {
                    Progress = _crystalAt[_first] - Reach;
                    Mining = true;
                    SwingTime = 0f;
                    Hits = 0;
                }
            }
            else
            {
                SwingTime += dt;
                // Golpes que já passaram pela fase do acerto desde o começo do balanço.
                int due = SwingTime >= HitPhase * SwingSeconds ? (int)Math.Floor(SwingTime / SwingSeconds - HitPhase) + 1 : 0;
                if (due > Hits && Hits < HitsToBreak)
                {
                    Hits = Math.Min(due, HitsToBreak);
                    step.Hit = true;
                }

                if (Hits >= HitsToBreak && SwingTime >= HitsToBreak * SwingSeconds)
                {
                    step.Broke = true;
                    step.BrokeAt = _crystalAt[_first];
                    step.BrokeColor = _crystalColor[_first];
                    _first = (_first + 1) % Capacity;
                    _count--;
                    Broken++;
                    Mining = false;
                }
            }

            Refill();
            return step;
        }

        void Refill()
        {
            while (_count < Capacity && _nextAt < Progress + LookAhead)
            {
                int slot = Slot(_count);
                _crystalAt[slot] = _nextAt;
                _crystalColor[slot] = _spawned % DescentArt.CrystalColorCount;
                _count++;
                _nextAt += MinGap + DescentArt.Hash(_spawned, 3) * GapRange;
                _spawned++;
            }
        }

        int Slot(int index) => (_first + index) % Capacity;
    }
}
