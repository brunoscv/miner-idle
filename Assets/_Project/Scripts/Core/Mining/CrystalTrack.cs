using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>
    /// A fila de cristais da descida (M6, D-52, D-54, D-60): cristais em ordem de distância na trilha. O que nasce entra no
    /// <b>fim da fila</b>, a pelo menos <see cref="MiningRules.SpawnDistance"/> à frente do anão (fora da tela) e a
    /// <see cref="MiningRules.MinCrystalSpacing"/> do último. Os iniciais começam na tela, a partir de
    /// <see cref="MiningRules.FirstCrystalDistance"/>. Sem sorteio: a mesma partida se repete.
    /// </summary>
    public sealed class CrystalTrack
    {
        readonly MiningRules _rules;
        readonly List<Crystal> _crystals;
        int _nextId = 1;

        public CrystalTrack(MiningRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _crystals = new List<Crystal>(rules.MaxCrystals);
        }

        /// <summary>Cristais inteiros, do mais perto (o da frente) ao mais longe.</summary>
        public IReadOnlyList<Crystal> Crystals => _crystals;

        public int Count => _crystals.Count;
        public bool IsFull => _crystals.Count >= _rules.MaxCrystals;

        /// <summary>O primeiro da fila (o alvo do anão), ou <c>null</c> com a fila vazia.</summary>
        public Crystal Front => _crystals.Count > 0 ? _crystals[0] : null;

        /// <summary>A fila do início da partida, já na tela: o primeiro a <c>FirstCrystalDistance</c>, os outros a cada espaço.</summary>
        public void SpawnInitial(double minerDistance)
        {
            for (int i = 0; i < _rules.InitialCrystals && !IsFull; i++)
                Add(minerDistance + _rules.FirstCrystalDistance + i * _rules.MinCrystalSpacing);
        }

        /// <summary>
        /// Um cristal no fim da fila. <c>null</c> se a fila está cheia, a menos de <paramref name="ignoreMax"/> (usado pelo
        /// debug).
        /// </summary>
        public Crystal TrySpawn(double minerDistance, bool ignoreMax = false)
        {
            if (IsFull && !ignoreMax) return null;

            double at = minerDistance + _rules.SpawnDistance;
            if (_crystals.Count > 0) at = Math.Max(at, _crystals[_crystals.Count - 1].Distance + _rules.MinCrystalSpacing);
            return Add(at);
        }

        /// <summary>Cria um cristal na distância dada, no seu lugar da fila, sem checar espaço nem máximo (testes e debug).</summary>
        public Crystal SpawnAt(double distance)
        {
            var crystal = new Crystal(_nextId++, distance, _rules.CrystalHealth, _rules.CrystalReward);
            int index = _crystals.Count;
            while (index > 0 && _crystals[index - 1].Distance > distance) index--;
            _crystals.Insert(index, crystal);
            return crystal;
        }

        internal void Remove(Crystal crystal) => _crystals.Remove(crystal);

        Crystal Add(double distance)
        {
            var crystal = new Crystal(_nextId++, distance, _rules.CrystalHealth, _rules.CrystalReward);
            _crystals.Add(crystal);
            return crystal;
        }
    }
}
