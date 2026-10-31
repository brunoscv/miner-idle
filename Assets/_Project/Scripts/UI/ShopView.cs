using System.Collections.Generic;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// A loja (S03, Marco 4) e o botão LOJA com o selo "!". Abrir a loja escurece e bloqueia o mapa por trás, mas
    /// <b>não pausa</b> a mineração (REQ-UI-04, P0): nada aqui mexe no tempo. Cada toque em Comprar faz no máximo uma
    /// compra (REQ-ECO-08). As linhas são criadas uma vez, a partir do <see cref="ShopCatalog"/>, e só se atualizam por
    /// evento (lascas, atributos, compra), nunca por quadro.
    /// </summary>
    public sealed class ShopView : MiningViewBase
    {
        [SerializeField] ShopCatalog catalog;
        [SerializeField] UpgradeRowView rowPrefab;
        [SerializeField] Transform rowsParent;

        [Tooltip("Tudo o que aparece com a loja aberta: o fundo que escurece e bloqueia o mapa, e o painel.")]
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text balance;
        [SerializeField] Button backButton;

        [SerializeField] Button openButton;
        [Tooltip("O \"!\" do botão LOJA: aparece quando alguma melhoria pode ser comprada.")]
        [SerializeField] GameObject badge;

        static readonly UpgradeId[] Lines = (UpgradeId[])System.Enum.GetValues(typeof(UpgradeId));

        readonly List<UpgradeRowView> _rows = new List<UpgradeRowView>();

        public bool IsOpen => screen.activeSelf;
        public bool IsBadgeShown => badge.activeSelf;
        public IReadOnlyList<UpgradeRowView> Rows => _rows;

        void Awake()
        {
            title.text = catalog.ShopTitle;
            foreach (var entry in catalog.Entries)
            {
                var row = Instantiate(rowPrefab, rowsParent);
                row.Bind(catalog, entry, Buy);
                _rows.Add(row);
            }
            screen.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            openButton.onClick.AddListener(Open);
            backButton.onClick.AddListener(Close);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            openButton.onClick.RemoveListener(Open);
            backButton.onClick.RemoveListener(Close);
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged += OnShardsChanged;
            simulation.StatsChanged += OnStatsChanged;
            simulation.CaveChanged += OnCaveChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged -= OnShardsChanged;
            simulation.StatsChanged -= OnStatsChanged;
            simulation.CaveChanged -= OnCaveChanged;
        }

        protected override void Rebuild(MiningSimulation simulation) => Refresh();

        public void Open()
        {
            screen.SetActive(true);
            Refresh();
        }

        public void Close() => screen.SetActive(false);

        void Buy(UpgradeId id) => Simulation?.Purchase(id); // a compra avisa ShardsChanged, e a loja se atualiza

        void OnShardsChanged(double shards) => Refresh();
        void OnStatsChanged(MinerStats stats) => Refresh();
        void OnCaveChanged(int cave) => Refresh();

        void Refresh()
        {
            var simulation = Simulation;
            if (simulation == null) return;

            bool anyPurchasable = false;
            foreach (var id in Lines)
                anyPurchasable |= simulation.Preview(id).State == UpgradeButtonState.Purchasable;
            badge.SetActive(anyPurchasable);

            if (!IsOpen) return;

            balance.text = string.Format(catalog.BalanceFormat, NumberFormatter.FormatShards(simulation.State.Shards));
            foreach (var row in _rows)
                if (!row.IsLocked) row.Refresh(simulation.Preview(row.Upgrade));
        }
    }
}
