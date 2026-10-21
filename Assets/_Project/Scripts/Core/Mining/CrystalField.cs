using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>
    /// Os cristais vivos no mapa e onde nasce o próximo (REQ-MAP-02): dentro da área, a uma distância mínima dos outros
    /// cristais e do anão. Também numera os lotes (D-27): o que nasce no início é o lote 1; cada nascimento seguinte
    /// recebe o próximo número.
    /// </summary>
    public sealed class CrystalField
    {
        /// <summary>Tentativas de achar um lugar livre; se todas falharem, o nascimento é pulado.</summary>
        public const int PlacementAttempts = 30;

        readonly MiningRules _rules;
        readonly IRandom _random;
        readonly List<Crystal> _crystals;

        int _nextId = 1;
        int _nextBatch = 1;

        public CrystalField(MiningRules rules, IRandom random)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _crystals = new List<Crystal>(rules.MaxCrystals);
        }

        /// <summary>Cristais inteiros, na ordem de nascimento.</summary>
        public IReadOnlyList<Crystal> Crystals => _crystals;

        public int Count => _crystals.Count;
        public bool IsFull => _crystals.Count >= _rules.MaxCrystals;

        /// <summary>Cria até <paramref name="count"/> cristais, todos no mesmo (próximo) lote. Devolve quantos nasceram.</summary>
        public int SpawnBatch(int count, Vec2 minerPosition)
        {
            int batch = _nextBatch++;
            int created = 0;
            for (int i = 0; i < count && !IsFull; i++)
            {
                if (!TryFindFreePosition(minerPosition, out var position)) break;
                Add(batch, position);
                created++;
            }
            return created;
        }

        /// <summary>Um cristal num lugar livre, com lote próprio. <c>null</c> se o mapa está cheio ou sem espaço.</summary>
        public Crystal TrySpawn(Vec2 minerPosition)
        {
            if (IsFull || !TryFindFreePosition(minerPosition, out var position)) return null;
            return Add(_nextBatch++, position);
        }

        /// <summary>Cria um cristal na posição dada, com lote próprio, sem checar espaço (testes e debug).</summary>
        public Crystal SpawnAt(Vec2 position) => Add(_nextBatch++, position);

        internal void Remove(Crystal crystal) => _crystals.Remove(crystal);

        Crystal Add(int batch, Vec2 position)
        {
            var crystal = new Crystal(_nextId++, batch, position, _rules.CrystalHealth, _rules.CrystalReward);
            _crystals.Add(crystal);
            return crystal;
        }

        bool TryFindFreePosition(Vec2 minerPosition, out Vec2 position)
        {
            double spacing2 = _rules.MinCrystalSpacing * _rules.MinCrystalSpacing;
            double fromMiner2 = _rules.MinDistanceFromMiner * _rules.MinDistanceFromMiner;

            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                var candidate = new Vec2(
                    _random.Range(_rules.MapMin.X, _rules.MapMax.X),
                    _random.Range(_rules.MapMin.Y, _rules.MapMax.Y));

                if (Vec2.DistanceSquared(candidate, minerPosition) < fromMiner2) continue;

                bool free = true;
                for (int i = 0; i < _crystals.Count; i++)
                {
                    if (Vec2.DistanceSquared(candidate, _crystals[i].Position) < spacing2)
                    {
                        free = false;
                        break;
                    }
                }

                if (free)
                {
                    position = candidate;
                    return true;
                }
            }

            position = default;
            return false;
        }
    }
}
