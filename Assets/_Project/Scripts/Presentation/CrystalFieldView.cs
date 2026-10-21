using System.Collections.Generic;
using MinersIdle.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Desenha os cristais do mapa: um <see cref="CrystalView"/> por cristal vivo, reaproveitados por pool porque cristais
    /// nascem e somem o tempo todo (Arquitetura Técnica, D-T13). Só reage a eventos da simulação.
    /// </summary>
    public sealed class CrystalFieldView : MiningViewBase
    {
        [SerializeField] CrystalView crystalPrefab;

        readonly Dictionary<int, CrystalView> _active = new Dictionary<int, CrystalView>();
        ObjectPool<CrystalView> _pool;
        CrystalView _target;

        /// <summary>Cristais desenhados agora (para testes: deve bater com a simulação).</summary>
        public int ActiveCount => _active.Count;

        void Awake()
        {
            _pool = new ObjectPool<CrystalView>(
                createFunc: () => Instantiate(crystalPrefab, transform),
                actionOnGet: view => view.gameObject.SetActive(true),
                actionOnRelease: view =>
                {
                    view.Clear();
                    view.gameObject.SetActive(false);
                },
                actionOnDestroy: view => Destroy(view.gameObject),
                defaultCapacity: 16);
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.CrystalSpawned += OnSpawned;
            simulation.CrystalBroken += OnBroken;
            simulation.HitLanded += OnHit;
            simulation.TargetChanged += OnTargetChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.CrystalSpawned -= OnSpawned;
            simulation.CrystalBroken -= OnBroken;
            simulation.HitLanded -= OnHit;
            simulation.TargetChanged -= OnTargetChanged;
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            foreach (var view in _active.Values) _pool.Release(view);
            _active.Clear();
            _target = null;

            var crystals = simulation.Crystals;
            for (int i = 0; i < crystals.Count; i++) OnSpawned(crystals[i]);
            OnTargetChanged(simulation.Miner.Target);
        }

        void OnSpawned(Crystal crystal)
        {
            var view = _pool.Get();
            view.Show(crystal, SortingOrderFor(crystal.Position));
            _active[crystal.Id] = view;
        }

        void OnBroken(Crystal crystal)
        {
            if (!_active.TryGetValue(crystal.Id, out var view)) return;

            if (view == _target) _target = null;
            _active.Remove(crystal.Id);
            _pool.Release(view);
        }

        void OnHit(Crystal crystal, double damage)
        {
            if (_active.TryGetValue(crystal.Id, out var view)) view.RefreshHealth();
        }

        void OnTargetChanged(Crystal target)
        {
            if (_target != null) _target.SetTargeted(false);
            _target = null;

            if (target != null && _active.TryGetValue(target.Id, out var view))
            {
                view.SetTargeted(true);
                _target = view;
            }
        }
    }
}
