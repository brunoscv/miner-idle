using System.Collections.Generic;
using MinersIdle.Core;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// A barra do andar à esquerda (M9d E3, D-278, GDD v8.2 §7.3): do andar 1, em cima, ao último, embaixo. Um trecho de
    /// cor por bioma; em âmbar, o que já se desceu nesta run; a seta com a etiqueta do andar de agora (o número é do
    /// <see cref="HudView"/>); um traço branco no recorde, com o número; e tracinhos nos andares que liberam algo (o
    /// avanço automático, o estagiário, o raro e o offline maior, das regras). Atualiza por evento, nunca por quadro.
    /// Os trechos e os tracinhos saem das regras de agora: o DBG → Cálculo troca o jogo e a barra se refaz.
    /// </summary>
    public sealed class FloorBar : MiningViewBase
    {
        [Tooltip("A faixa interna: tudo se posiciona nela, do topo (andar 1) à base (o último andar).")]
        [SerializeField] RectTransform track;
        [Tooltip("Onde ficam os trechos dos biomas (criados ao abrir, um por bioma).")]
        [SerializeField] RectTransform segments;
        [Tooltip("O que já se desceu nesta run (âmbar), do topo até o andar de agora.")]
        [SerializeField] RectTransform fill;
        [Tooltip("Onde ficam os tracinhos dos andares que liberam algo.")]
        [SerializeField] RectTransform ticks;
        [Tooltip("Um tracinho, copiado para cada andar que libera algo.")]
        [SerializeField] RectTransform tickTemplate;
        [Tooltip("O traço do recorde e o número dele.")]
        [SerializeField] RectTransform record;
        [SerializeField] TMP_Text recordValue;
        [Tooltip("A seta e a etiqueta do andar de agora.")]
        [SerializeField] RectTransform marker;
        [Tooltip("A cor de cada bioma, de cima para baixo (repete se houver mais biomas).")]
        [SerializeField] Color[] biomeColors = new Color[0];

        readonly List<Image> _segments = new List<Image>();
        readonly List<RectTransform> _ticks = new List<RectTransform>();
        int _floorCount;

        /// <summary>O andar onde está a seta (para testes).</summary>
        public int ShownFloor { get; private set; }
        /// <summary>O andar do traço do recorde (para testes).</summary>
        public int ShownRecord { get; private set; }
        /// <summary>Se o traço do recorde aparece: some quando o recorde é o andar de agora (para testes).</summary>
        public bool IsRecordShown => record != null && record.gameObject.activeSelf;
        /// <summary>O texto do recorde (para testes).</summary>
        public string RecordText => recordValue != null ? recordValue.text : null;
        /// <summary>Quanto da barra está em âmbar, de 0 a 1 (para testes).</summary>
        public float FillAmount => fill != null ? 1f - fill.anchorMin.y : 0f;
        /// <summary>Quantos trechos de bioma e quantos tracinhos a barra tem (para testes).</summary>
        public int SegmentCount => _segments.Count;
        public int TickCount => _ticks.Count;

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged += OnCaveChanged;
            simulation.PermanentsChanged += OnPermanentsChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged -= OnCaveChanged;
            simulation.PermanentsChanged -= OnPermanentsChanged;
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            BuildSegments(simulation.Rules);
            BuildTicks(simulation.Rules);
            Refresh();
        }

        void OnCaveChanged(int cave) => Refresh();
        void OnPermanentsChanged() => Refresh();

        void Refresh()
        {
            if (Simulation == null) return;
            ShownFloor = Simulation.State.Cave;
            ShownRecord = Simulation.DeepestFloor;

            if (fill != null)
            {
                fill.anchorMin = new Vector2(0f, 1f - Bottom(ShownFloor));
                fill.anchorMax = Vector2.one;
                fill.offsetMin = fill.offsetMax = Vector2.zero;
            }
            Place(marker, Center(ShownFloor));

            bool showRecord = ShownRecord > ShownFloor;
            if (record != null && record.gameObject.activeSelf != showRecord) record.gameObject.SetActive(showRecord);
            if (showRecord)
            {
                Place(record, Center(ShownRecord));
                if (recordValue != null) recordValue.text = NumberFormatter.Format(ShownRecord);
            }
        }

        /// <summary>Onde o andar termina, de 0 (topo) a 1 (base).</summary>
        float Bottom(int floor) => _floorCount <= 0 ? 0f : Mathf.Clamp01((float)floor / _floorCount);

        /// <summary>O meio do andar, de 0 (topo) a 1 (base).</summary>
        float Center(int floor) => _floorCount <= 0 ? 0f : Mathf.Clamp01((floor - 0.5f) / _floorCount);

        static void Place(RectTransform target, float fromTop)
        {
            if (target == null) return;
            float y = 1f - fromTop;
            target.anchorMin = new Vector2(target.anchorMin.x, y);
            target.anchorMax = new Vector2(target.anchorMax.x, y);
            target.anchoredPosition = new Vector2(target.anchoredPosition.x, 0f);
        }

        void BuildSegments(MiningRules rules)
        {
            _floorCount = Mathf.Max(1, rules.CaveCount);
            int perBiome = Mathf.Max(1, rules.LevelsPerBiome);
            int count = (_floorCount + perBiome - 1) / perBiome;
            if (segments == null) return;

            while (_segments.Count < count)
            {
                var go = new GameObject("Biome" + (_segments.Count + 1), typeof(RectTransform), typeof(Image));
                go.transform.SetParent(segments, false);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                _segments.Add(image);
            }
            while (_segments.Count > count)
            {
                Destroy(_segments[_segments.Count - 1].gameObject);
                _segments.RemoveAt(_segments.Count - 1);
            }

            for (int i = 0; i < count; i++)
            {
                var rect = _segments[i].rectTransform;
                float top = (float)(i * perBiome) / _floorCount;
                float bottom = Mathf.Min(1f, (float)((i + 1) * perBiome) / _floorCount);
                rect.anchorMin = new Vector2(0f, 1f - bottom);
                rect.anchorMax = new Vector2(1f, 1f - top);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                _segments[i].color = biomeColors.Length > 0 ? biomeColors[i % biomeColors.Length] : Color.gray;
            }
        }

        void BuildTicks(MiningRules rules)
        {
            if (ticks == null || tickTemplate == null) return;
            var floors = new SortedSet<int>();
            foreach (int floor in new[] { rules.AutoAdvanceFloor, rules.InternFloor, rules.RareFloor, rules.OfflineDeepFloor })
                if (floor > 1 && floor <= _floorCount) floors.Add(floor);

            while (_ticks.Count < floors.Count)
            {
                var tick = Instantiate(tickTemplate, ticks);
                tick.gameObject.SetActive(true);
                _ticks.Add(tick);
            }
            while (_ticks.Count > floors.Count)
            {
                Destroy(_ticks[_ticks.Count - 1].gameObject);
                _ticks.RemoveAt(_ticks.Count - 1);
            }

            int i = 0;
            foreach (int floor in floors)
            {
                _ticks[i].name = "Tick" + floor;
                Place(_ticks[i], Center(floor));
                i++;
            }
        }
    }
}
