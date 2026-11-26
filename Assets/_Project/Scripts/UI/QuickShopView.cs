using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using UnityEngine;

namespace MinersIdle.UI
{
    /// <summary>
    /// A loja rápida (M9i E8, D-341; proposta do Bruno para o Apellon, ligada só pelo DBG): os quatro cards da aba
    /// MELHORIAS (Picareta, Velocidade, Mineração, Crítico) logo acima do Descansar e do DESCER. As mesmas compras e os
    /// mesmos custos da LOJA (<see cref="MiningSimulation.Purchase"/>); nenhuma regra muda.
    /// <para>
    /// Com a janela do raro aberta, os cards ficam por cima do escurecido dela e continuam tocáveis (P10): o
    /// <see cref="Canvas"/> deles passa a ordenar sozinho, acima do de fora. Fechada, voltam ao lugar deles na
    /// hierarquia (abaixo das janelas da LOJA, do AD e dos AJUSTES).
    /// </para>
    /// </summary>
    public sealed class QuickShopView : MiningViewBase
    {
        [SerializeField] ShopCatalog catalog;
        [SerializeField] QuickShopCard[] cards = System.Array.Empty<QuickShopCard>();
        [Tooltip("O Canvas próprio dos cards: ordena por cima da janela do raro enquanto ela está aberta.")]
        [SerializeField] Canvas overlay;
        [Tooltip("Quantos acima do Canvas de fora, com o raro aberto.")]
        [SerializeField] int rareSortingOffset = 1;

        bool _bound;
        bool[] _hasEntry = System.Array.Empty<bool>();

        public System.Collections.Generic.IReadOnlyList<QuickShopCard> Cards => cards;
        /// <summary>Os cards estão por cima do escurecido do raro.</summary>
        public bool IsAboveRareWindow => overlay != null && overlay.overrideSorting;

        void Awake() => Bind();

        void Bind()
        {
            if (_bound || catalog == null) return;
            _bound = true;
            var upgrades = catalog.QuickUpgrades;
            _hasEntry = new bool[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                var entry = i < upgrades.Count ? catalog.Find(upgrades[i]) : null;
                _hasEntry[i] = entry != null && cards[i] != null;
                if (!_hasEntry[i])
                {
                    if (cards[i] != null) cards[i].gameObject.SetActive(false);
                    continue;
                }
                cards[i].Bind(catalog, entry, Buy);
            }
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged += OnShardsChanged;
            simulation.StatsChanged += OnStatsChanged;
            simulation.CaveChanged += OnCaveChanged;
            simulation.RareWindowChanged += OnRareWindowChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged -= OnShardsChanged;
            simulation.StatsChanged -= OnStatsChanged;
            simulation.CaveChanged -= OnCaveChanged;
            simulation.RareWindowChanged -= OnRareWindowChanged;
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            Refresh();
            OnRareWindowChanged(simulation.RareWindow);
        }

        void Buy(UpgradeId id) => Simulation?.Purchase(id); // a compra avisa ShardsChanged, e os cards se atualizam

        // Lascas pelo dano (M9k): o saldo muda a cada golpe; os botões se refazem no máximo a cada ShardsRefreshGap
        // pelo saldo (uma compra, o andar e os atributos refazem na hora).
        const float ShardsRefreshGap = 0.1f;
        bool _shardsDirty;
        float _nextShardsRefresh;

        void OnShardsChanged(double shards) => _shardsDirty = true;

        void LateUpdate()
        {
            if (!_shardsDirty || Time.unscaledTime < _nextShardsRefresh) return;
            _shardsDirty = false;
            _nextShardsRefresh = Time.unscaledTime + ShardsRefreshGap;
            Refresh();
        }
        void OnStatsChanged(MinerStats stats) => Refresh();
        void OnCaveChanged(int cave) => Refresh();

        void Refresh()
        {
            var simulation = Simulation;
            if (simulation == null) return;
            Bind();
            for (int i = 0; i < cards.Length; i++)
                if (_hasEntry[i]) cards[i].Refresh(simulation.Preview(cards[i].Upgrade));
        }

        void OnRareWindowChanged(RareWindow window)
        {
            if (overlay == null) return;
            bool above = window.IsOpen;
            if (overlay.overrideSorting == above) return;
            overlay.overrideSorting = above;
            var root = overlay.transform.parent != null ? overlay.transform.parent.GetComponentInParent<Canvas>() : null;
            if (above) overlay.sortingOrder = (root != null ? root.sortingOrder : 0) + rareSortingOffset;
        }
    }
}
