using System;

namespace MinersIdle.Presentation
{
    /// <summary>Os sons do feedback de mineração (M5, E4). A ordem é o índice no pool de clipes.</summary>
    public enum Sfx
    {
        Hit,
        Tap,
        Critical,
        Break,
        Purchase,
        Milestone,
    }

    /// <summary>
    /// Bipes provisórios (M5, E4) sintetizados por código: ondas simples com varredura de frequência e um pouco de ruído,
    /// no espírito dos jogos de 8 bits. Gerados uma vez ao abrir a cena, nunca durante o jogo; os sons de verdade entram
    /// com a arte, depois do Art Spike. Determinístico (o ruído usa uma semente fixa), para os testes.
    /// </summary>
    public static class SfxSynth
    {
        public static readonly int Count = Enum.GetValues(typeof(Sfx)).Length;

        const float AttackSeconds = 0.003f; // sem ataque, o começo da onda estala

        enum Wave { Square, Triangle, Noise }

        readonly struct Note
        {
            public readonly float Start, Duration, FromHz, ToHz, Volume;
            public readonly Wave Wave;

            public Note(float start, float duration, Wave wave, float fromHz, float toHz, float volume)
            {
                Start = start;
                Duration = duration;
                Wave = wave;
                FromHz = fromHz;
                ToHz = toHz;
                Volume = volume;
            }
        }

        /// <summary>Amostras mono entre -1 e 1; começa e termina em silêncio.</summary>
        public static float[] Render(Sfx sfx, int sampleRate)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            return Mix(NotesOf(sfx), sampleRate);
        }

        static Note[] NotesOf(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Hit: // "tum" grave da picareta do anão
                    return new[]
                    {
                        new Note(0f, 0.08f, Wave.Square, 200f, 90f, 0.18f),
                        new Note(0f, 0.04f, Wave.Noise, 0f, 0f, 0.22f),
                    };
                case Sfx.Tap: // "tic" agudo e curto do dedo
                    return new[]
                    {
                        new Note(0f, 0.045f, Wave.Triangle, 1100f, 800f, 0.35f),
                        new Note(0f, 0.02f, Wave.Noise, 0f, 0f, 0.12f),
                    };
                case Sfx.Critical: // duas notas subindo, com estalo
                    return new[]
                    {
                        new Note(0f, 0.06f, Wave.Square, 880f, 880f, 0.15f),
                        new Note(0.06f, 0.12f, Wave.Square, 1320f, 1400f, 0.15f),
                        new Note(0f, 0.05f, Wave.Noise, 0f, 0f, 0.2f),
                    };
                case Sfx.Break: // estouro: ruído e um tom despencando
                    return new[]
                    {
                        new Note(0f, 0.2f, Wave.Noise, 0f, 0f, 0.3f),
                        new Note(0f, 0.22f, Wave.Square, 420f, 70f, 0.18f),
                    };
                case Sfx.Purchase: // "moeda": si e mi
                    return new[]
                    {
                        new Note(0f, 0.07f, Wave.Square, 988f, 988f, 0.14f),
                        new Note(0.07f, 0.2f, Wave.Square, 1319f, 1319f, 0.14f),
                    };
                case Sfx.Milestone: // arpejo de dó maior subindo
                    return new[]
                    {
                        new Note(0f, 0.08f, Wave.Square, 523f, 523f, 0.12f),
                        new Note(0.05f, 0.08f, Wave.Square, 659f, 659f, 0.12f),
                        new Note(0.1f, 0.08f, Wave.Square, 784f, 784f, 0.12f),
                        new Note(0.15f, 0.2f, Wave.Square, 1047f, 1047f, 0.12f),
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(sfx), sfx, null);
            }
        }

        static float[] Mix(Note[] notes, int sampleRate)
        {
            float length = 0f;
            foreach (var note in notes) length = Math.Max(length, note.Start + note.Duration);

            var samples = new float[(int)Math.Ceiling(length * sampleRate)];
            uint noise = 0x9E3779B9u;
            foreach (var note in notes)
            {
                int first = (int)(note.Start * sampleRate);
                int count = (int)(note.Duration * sampleRate);
                double phase = 0;
                for (int i = 0; i < count && first + i < samples.Length; i++)
                {
                    float t = i / (float)count;
                    double hz = note.FromHz + (note.ToHz - note.FromHz) * t;
                    phase += hz / sampleRate;
                    phase -= Math.Floor(phase);

                    float value;
                    switch (note.Wave)
                    {
                        case Wave.Square: value = phase < 0.5 ? 1f : -1f; break;
                        case Wave.Triangle: value = (float)(4 * Math.Abs(phase - 0.5) - 1); break;
                        default:
                            noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5; // xorshift32
                            value = noise / (float)uint.MaxValue * 2f - 1f;
                            break;
                    }

                    samples[first + i] += value * note.Volume * Envelope(i / (float)sampleRate, t);
                }
            }

            for (int i = 0; i < samples.Length; i++) samples[i] = Math.Max(-1f, Math.Min(1f, samples[i]));
            return samples;
        }

        /// <summary>Ataque curto e queda até zero no fim da nota (sem estalo no corte).</summary>
        static float Envelope(float seconds, float t)
        {
            float attack = Math.Min(1f, seconds / AttackSeconds);
            float decay = 1f - t;
            return attack * decay * decay;
        }
    }
}
