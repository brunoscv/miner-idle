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
    ///
    /// Marco 8b, E4 (D-155): duas abas, as melhorias da run e as permanentes (compradas com lascas vermelhas). Na aba das
    /// permanentes, o banco no lugar das lascas e as linhas na ordem da tabela, com as que chegaram ao limite no fim. O
    /// "!" da LOJA acende com qualquer compra possível nas duas abas; o de uma aba, quando ela não é a aberta. Sem
    /// permanentes no balanço (o GDD v5), a aba some.
    ///
    /// M7, E3 (D-170, D-174): a 3ª aba, ESTAGIÁRIO, à direita das outras e só com ele contratado: as melhorias dele, com o
    /// saldo de lascas no alto. Na aba da run, a compra única já feita (o Estagiário contratado) desce para o fim.
    /// </summary>
    public sealed class ShopView : MiningViewBase
    {
        public enum Tab { Run, Permanents, Intern }

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

        [Header("Abas (Marco 8b, E4, D-155)")]
        [SerializeField] Button runTabButton;
        [SerializeField] TMP_Text runTabLabel;
        [SerializeField] GameObject runTabBadge;
        [SerializeField] Button permanentsTabButton;
        [SerializeField] TMP_Text permanentsTabLabel;
        [SerializeField] GameObject permanentsTabBadge;
        [SerializeField] Sprite activeTabSprite;
        [SerializeField] Sprite inactiveTabSprite;
        [SerializeField] Color activeTabTextColor = Color.white;
        [SerializeField] Color inactiveTabTextColor = Color.gray;

        [Header("Aba das permanentes")]
        [SerializeField] PermanentRowView permanentRowPrefab;
        [SerializeField] Transform permanentRowsParent;
        [Tooltip("O banco de lascas vermelhas na barra de título (o cristal vermelho e o número), no lugar das lascas.")]
        [SerializeField] GameObject redBalance;
        [SerializeField] TMP_Text redBalanceValue;

        [Header("Aba do estagiário (M7, E3, D-170)")]
        [SerializeField] Button internTabButton;
        [SerializeField] TMP_Text internTabLabel;
        [SerializeField] GameObject internTabBadge;
        [SerializeField] Transform internRowsParent;

        readonly List<UpgradeRowView> _rows = new List<UpgradeRowView>();
        readonly List<UpgradeRowView> _internRows = new List<UpgradeRowView>();
        readonly List<PermanentRowView> _permanentRows = new List<PermanentRowView>();

        public bool IsOpen => screen.activeSelf;
        public bool IsBadgeShown => badge.activeSelf;
        public IReadOnlyList<UpgradeRowView> Rows => _rows;
        /// <summary>As linhas das permanentes na ordem da tabela (a ordem na tela é a dos irmãos no pai).</summary>
        public IReadOnlyList<PermanentRowView> PermanentRows => _permanentRows;
        public IReadOnlyList<UpgradeRowView> InternRows => _internRows;
        public Tab ActiveTab { get; private set; }
        public bool IsRunTabBadgeShown => runTabBadge.activeSelf;
        public bool IsPermanentsTabBadgeShown => permanentsTabBadge.activeSelf;
        public bool IsPermanentsTabShown => permanentsTabButton.gameObject.activeSelf;
        public bool IsRunListShown => rowsParent.gameObject.activeInHierarchy;
        public bool IsInternTabShown => internTabButton.gameObject.activeSelf;
        public bool IsInternTabBadgeShown => internTabBadge.activeSelf;
        public bool IsInternListShown => internRowsParent.gameObject.activeInHierarchy;
        public string RedBalanceText => redBalanceValue.text;

        void Awake()
        {
            title.text = catalog.ShopTitle;
            foreach (var entry in catalog.Entries)
            {
                var row = Instantiate(rowPrefab, rowsParent);
                row.Bind(catalog, entry, Buy);
                _rows.Add(row);
            }
            foreach (var entry in catalog.PermanentEntries)
            {
                var row = Instantiate(permanentRowPrefab, permanentRowsParent);
                row.Bind(catalog, entry, BuyPermanent);
                _permanentRows.Add(row);
            }
            foreach (var entry in catalog.InternEntries)
            {
                var row = Instantiate(rowPrefab, internRowsParent);
                row.Bind(catalog, entry, Buy);
                _internRows.Add(row);
            }
            runTabLabel.text = catalog.RunTabLabel;
            permanentsTabLabel.text = catalog.PermanentsTabLabel;
            internTabLabel.text = catalog.InternTabLabel;
            ShowTab(Tab.Run);
            screen.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            openButton.onClick.AddListener(Open);
            backButton.onClick.AddListener(Close);
            runTabButton.onClick.AddListener(SelectRunTab);
            permanentsTabButton.onClick.AddListener(SelectPermanentsTab);
            internTabButton.onClick.AddListener(SelectInternTab);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            openButton.onClick.RemoveListener(Open);
            backButton.onClick.RemoveListener(Close);
            runTabButton.onClick.RemoveListener(SelectRunTab);
            permanentsTabButton.onClick.RemoveListener(SelectPermanentsTab);
            internTabButton.onClick.RemoveListener(SelectInternTab);
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged += OnShardsChanged;
            simulation.StatsChanged += OnStatsChanged;
            simulation.CaveChanged += OnCaveChanged;
            simulation.PermanentsChanged += Refresh;
            simulation.InternChanged += OnInternChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged -= OnShardsChanged;
            simulation.StatsChanged -= OnStatsChanged;
            simulation.CaveChanged -= OnCaveChanged;
            simulation.PermanentsChanged -= Refresh;
            simulation.InternChanged -= OnInternChanged;
        }

        protected override void Rebuild(MiningSimulation simulation) => Refresh();

        /// <summary>O botão LOJA: abre na aba das melhorias da run.</summary>
        public void Open() => Open(Tab.Run);

        /// <summary>O PERMANENTES do resumo do dia (D-154): abre na aba das permanentes.</summary>
        public void OpenPermanents() => Open(Tab.Permanents);

        public void Open(Tab tab)
        {
            screen.SetActive(true);
            ShowTab(tab);
            Refresh();
        }

        public void Close() => screen.SetActive(false);

        public void SelectTab(Tab tab)
        {
            if (tab == ActiveTab) return;
            ShowTab(tab);
            Refresh();
        }

        void SelectRunTab() => SelectTab(Tab.Run);
        void SelectPermanentsTab() => SelectTab(Tab.Permanents);
        void SelectInternTab() => SelectTab(Tab.Intern);

        void ShowTab(Tab tab)
        {
            ActiveTab = tab;
            bool permanents = tab == Tab.Permanents;
            SetActive(rowsParent.gameObject, tab == Tab.Run);
            SetActive(permanentRowsParent.gameObject, permanents);
            SetActive(internRowsParent.gameObject, tab == Tab.Intern);
            SetActive(balance.gameObject, !permanents);
            SetActive(redBalance, permanents);
            PaintTab(runTabButton, runTabLabel, tab == Tab.Run);
            PaintTab(permanentsTabButton, permanentsTabLabel, permanents);
            PaintTab(internTabButton, internTabLabel, tab == Tab.Intern);
        }

        void PaintTab(Button tab, TMP_Text label, bool active)
        {
            var sprite = active ? activeTabSprite : inactiveTabSprite;
            if (sprite != null) tab.image.sprite = sprite;
            label.color = active ? activeTabTextColor : inactiveTabTextColor;
        }

        void Buy(UpgradeId id) => Simulation?.Purchase(id); // a compra avisa ShardsChanged, e a loja se atualiza

        void BuyPermanent(PermanentId id) => Simulation?.PurchasePermanent(id); // avisa PermanentsChanged

        void OnShardsChanged(double shards) => Refresh();
        void OnStatsChanged(MinerStats stats) => Refresh();
        void OnCaveChanged(int cave) => Refresh();
        void OnInternChanged(InternStats stats) => Refresh();

        void Refresh()
        {
            var simulation = Simulation;
            if (simulation == null) return;

            // Só as linhas que a loja mostra: uma linha sem lugar na loja (as do estagiário antes da aba dele, M7) não
            // acende o "!" de algo que o jogador não acha.
            bool anyRun = AnyPurchasable(simulation, _rows);
            // A aba dele só existe com ele contratado (D-170); antes, as compras dele nem aparecem (Purchases.Blocked).
            bool internShown = simulation.IsInternHired && _internRows.Count > 0;
            bool anyIntern = internShown && AnyPurchasable(simulation, _internRows);
            bool anyPermanent = false, hasPermanents = false;
            foreach (var row in _permanentRows)
            {
                var preview = simulation.PreviewPermanent(row.Permanent);
                hasPermanents |= preview.IsEnabled;
                anyPermanent |= preview.State == UpgradeButtonState.Purchasable;
            }
            badge.SetActive(anyRun || anyPermanent || anyIntern);

            // Sem permanentes no balanço (GDD v5), a aba some e a loja fica na da run.
            SetActive(permanentsTabButton.gameObject, hasPermanents);
            if (!hasPermanents && ActiveTab == Tab.Permanents) ShowTab(Tab.Run);
            // Demitido (o atalho do debug) ou num descanso sem a Promoção: a aba some e a loja volta para a da run.
            SetActive(internTabButton.gameObject, internShown);
            if (!internShown && ActiveTab == Tab.Intern) ShowTab(Tab.Run);
            SetActive(runTabBadge, anyRun && ActiveTab != Tab.Run);
            SetActive(permanentsTabBadge, anyPermanent && ActiveTab != Tab.Permanents);
            SetActive(internTabBadge, anyIntern && ActiveTab != Tab.Intern);

            if (!IsOpen) return;

            if (ActiveTab != Tab.Permanents)
            {
                balance.text = string.Format(catalog.BalanceFormat, NumberFormatter.FormatShards(simulation.State.Shards));
                var rows = ActiveTab == Tab.Run ? _rows : _internRows;
                foreach (var row in rows)
                    if (!row.IsLocked) row.Refresh(simulation.Preview(row.Upgrade));
                if (ActiveTab == Tab.Run) SortRunRows();
                return;
            }

            redBalanceValue.text = NumberFormatter.FormatShards(simulation.Permanents.State.RedShards);
            foreach (var row in _permanentRows) row.Refresh(simulation.PreviewPermanent(row.Permanent));
            SortPermanentRows();
        }

        static bool AnyPurchasable(MiningSimulation simulation, List<UpgradeRowView> rows)
        {
            foreach (var row in rows)
                if (!row.IsLocked && simulation.Preview(row.Upgrade).State == UpgradeButtonState.Purchasable) return true;
            return false;
        }

        /// <summary>Na ordem do catálogo; a compra única já feita (o Estagiário contratado) no fim (D-174).</summary>
        void SortRunRows()
        {
            int index = 0;
            foreach (var row in _rows)
                if (!row.IsDone) Place(row.transform, ref index);
            foreach (var row in _rows)
                if (row.IsDone) Place(row.transform, ref index);
        }

        /// <summary>As que ainda sobem primeiro, na ordem da tabela; as que chegaram ao limite no fim (D-155).</summary>
        void SortPermanentRows()
        {
            int index = 0;
            foreach (var row in _permanentRows)
                if (!row.IsMaxed) Place(row.transform, ref index);
            foreach (var row in _permanentRows)
                if (row.IsMaxed) Place(row.transform, ref index);
        }

        static void Place(Transform row, ref int index)
        {
            if (row.GetSiblingIndex() != index) row.SetSiblingIndex(index);
            index++;
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target.activeSelf != active) target.SetActive(active);
        }
    }
}
