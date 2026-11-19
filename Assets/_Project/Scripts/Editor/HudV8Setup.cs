using System.IO;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// O HUD do v8 (M9d E3, D-278, mockup aprovado pelo Bruno em 2026-11-18, D-281), na cena Game já montada:
    /// <list type="bullet">
    /// <item>no alto só LASCAS e VERMELHAS (o painel VERMELHAS é uma cópia do LASCAS, com o rubi do Descansar);</item>
    /// <item>DANO e VELOCIDADE mudam para uma faixa no alto da LOJA, acima das abas (os mesmos painéis, com o âmbar dos
    /// bônus); o resto do painel da LOJA desce o tanto da faixa;</item>
    /// <item>o painel ANDAR sai e vira a <see cref="FloorBar"/> à esquerda, com a etiqueta do andar de agora;</item>
    /// <item>a coluna dos bônus passa para o lado da barra.</item>
    /// </list>
    /// Os painéis mudam de lugar, não são recriados: o <see cref="HudView"/> continua dono dos números. Idempotente.
    /// </summary>
    static class HudV8Setup
    {
        const string BarName = "FloorBar";
        const string StatsName = "Stats";

        // Referência 270 de largura (D-14). A barra fica entre o HUD (até 42 px do topo) e a barra de baixo, acima do DBG.
        const float BarLeft = 4f;
        const float BarWidth = 18f;
        const float BarTop = 48f;
        const float BarBottom = 78f;
        const float TrackInsetX = 4f;
        const float TrackInsetY = 5f;
        const float TickWidth = 4f;
        const float RecordReach = 6f;   // o traço do recorde passa da barra para a direita
        const float RecordHeight = 2f;
        const float ArrowSize = 8f;
        const float TagWidth = 36f;
        const float TagHeight = 14f;
        const float TagGap = 6f;        // da faixa à etiqueta (a seta fica nesse vão)
        const float NumberSize = 8f;    // Press Start em múltiplos de 2

        /// <summary>A coluna dos bônus começa depois da etiqueta do andar, para a seta não passar por cima dela.</summary>
        const float BuffLeft = BarLeft + BarWidth + TagGap + TagWidth + 6f;

        // A faixa do DANO e da VELOCIDADE no alto da LOJA, logo abaixo da barra do título.
        const float StatsTop = 18f;
        const float StatsHeight = 34f;
        const float StatsGap = 3f;
        const float StatWidth = 78f;
        const float StatSpacing = 36f;

        static readonly Color Amber = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color White = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color TickColor = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        static readonly Color BarBack = new Color32(0x2A, 0x1C, 0x12, 0xFF);

        /// <summary>Um tom por bioma (os do mockup da E0), de cima para baixo.</summary>
        static readonly Color[] BiomeColors =
        {
            new Color32(120, 98, 80, 255), new Color32(104, 92, 86, 255), new Color32(84, 96, 120, 255),
            new Color32(70, 110, 92, 255), new Color32(128, 84, 62, 255), new Color32(96, 76, 120, 255),
            new Color32(60, 104, 120, 255), new Color32(130, 112, 70, 255), new Color32(120, 60, 60, 255),
            new Color32(150, 40, 40, 255),
        };

        static Sprite Load(string path) => File.Exists(path) ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;

        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var hud = Object.FindAnyObjectByType<HudView>(FindObjectsInactive.Include);
            if (safeArea == null || hud == null || hud.transform.parent != safeArea) return false;

            bool changed = EnsureRedShards(safeArea, hud);
            changed |= EnsureFloorBar(safeArea, hud);
            changed |= EnsureShopStats(safeArea, hud);
            changed |= MoveBuffColumn(safeArea);
            return changed;
        }

        // ── VERMELHAS no HUD ─────────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureRedShards(Transform safeArea, HudView hud)
        {
            var so = new SerializedObject(hud);
            var property = so.FindProperty("redShardsValue");
            if (property.objectReferenceValue != null) return RubyIcon(safeArea, hud.transform.Find("Vermelhas"));

            var lascas = hud.transform.Find("Lascas");
            if (lascas == null) return false;
            var red = Object.Instantiate(lascas.gameObject, hud.transform);
            red.name = "Vermelhas";
            red.transform.SetSiblingIndex(lascas.GetSiblingIndex() + 1);
            if (red.transform.Find("Label")?.GetComponent<TMP_Text>() is TMP_Text label) label.text = "VERMELHAS";
            RubyIcon(safeArea, red.transform);

            var value = red.transform.Find("Row/Value")?.GetComponent<TMP_Text>() ?? red.transform.Find("Value")?.GetComponent<TMP_Text>();
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] HUD: VERMELHAS ao lado de LASCAS (M9d E3, D-278).");
            return true;
        }

        /// <summary>O ícone das VERMELHAS é o do Descansar: o cristal do jogo tingido de vermelho (o desenho e a cor).</summary>
        static bool RubyIcon(Transform safeArea, Transform red)
        {
            var ruby = safeArea.Find("Rest/RestButton/Icon")?.GetComponent<Image>();
            var icon = red != null ? red.Find("Row/Icon")?.GetComponent<Image>() : null;
            if (icon == null || ruby == null || ruby.sprite == null) return false;
            if (icon.sprite == ruby.sprite && icon.color == ruby.color) return false;
            icon.sprite = ruby.sprite;
            icon.color = ruby.color;
            EditorUtility.SetDirty(icon);
            return true;
        }

        // ── A barra do andar ─────────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureFloorBar(Transform safeArea, HudView hud)
        {
            if (safeArea.Find(BarName) != null) return RemoveCavePanel(hud);

            var runner = Object.FindAnyObjectByType<MinersIdle.Game.MiningRunner>();
            var numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);
            var panel = Load(UiSkinSetup.WoodFolder + "/panels/panel");
            var diamond = AssetDatabase.LoadAssetAtPath<Sprite>(GrayboxSetup.DiamondPath);
            if (runner == null) return false;

            var root = new GameObject(BarName, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(safeArea, false);
            root.transform.SetSiblingIndex(hud.transform.GetSiblingIndex() + 1); // por cima do jogo, atrás da loja e das janelas
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.offsetMin = new Vector2(BarLeft, BarBottom);
            rootRect.offsetMax = new Vector2(BarLeft + BarWidth, -BarTop);
            Frame(root.GetComponent<Image>(), panel); // recebe o toque, como os painéis do HUD (M5)

            var track = Child(root.transform, "Track");
            track.anchorMin = Vector2.zero;
            track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(TrackInsetX, TrackInsetY);
            track.offsetMax = new Vector2(-TrackInsetX, -TrackInsetY);
            Paint(track.gameObject.AddComponent<Image>(), BarBack);

            var segments = Child(track, "Segments");
            Stretch(segments);
            var fill = Child(track, "Fill");
            Stretch(fill);
            Paint(fill.gameObject.AddComponent<Image>(), Amber);

            var ticks = Child(track, "Ticks");
            Stretch(ticks);
            var tick = Child(ticks, "TickTemplate");
            AtRightEdge(tick, TrackInsetX - 1f, TickWidth, 1f);
            Paint(tick.gameObject.AddComponent<Image>(), TickColor);
            tick.gameObject.SetActive(false);

            // O recorde: um traço branco que passa da barra, e o número à direita.
            var record = Child(track, "Record");
            record.anchorMin = new Vector2(0f, 0.5f);
            record.anchorMax = new Vector2(1f, 0.5f);
            record.pivot = new Vector2(0.5f, 0.5f);
            record.offsetMin = new Vector2(-2f, -RecordHeight / 2f);
            record.offsetMax = new Vector2(TrackInsetX + RecordReach, RecordHeight / 2f);
            Paint(record.gameObject.AddComponent<Image>(), White);
            var recordValue = Number(record, "Value", numbers, White, TextAlignmentOptions.MidlineLeft);
            var recordValueRect = recordValue.rectTransform;
            recordValueRect.anchorMin = recordValueRect.anchorMax = new Vector2(1f, 0.5f);
            recordValueRect.pivot = new Vector2(0f, 0.5f);
            recordValueRect.anchoredPosition = new Vector2(2f, 0f);
            recordValueRect.sizeDelta = new Vector2(TagWidth, TagHeight);

            // O andar de agora: a seta (um losango meio escondido sob a etiqueta) e a etiqueta de madeira com o número.
            var marker = Child(track, "Marker");
            marker.anchorMin = marker.anchorMax = new Vector2(1f, 0.5f);
            marker.pivot = new Vector2(0f, 0.5f);
            marker.anchoredPosition = Vector2.zero;
            marker.sizeDelta = new Vector2(TrackInsetX + TagGap + TagWidth, TagHeight);
            var arrow = Child(marker, "Arrow");
            arrow.anchorMin = arrow.anchorMax = arrow.pivot = new Vector2(0f, 0.5f);
            arrow.anchoredPosition = new Vector2(TrackInsetX + 1f, 0f);
            arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
            var arrowImage = arrow.gameObject.AddComponent<Image>();
            arrowImage.sprite = diamond;
            Paint(arrowImage, Amber);
            var tag = Child(marker, "Tag");
            tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(0f, 0.5f);
            tag.anchoredPosition = new Vector2(TrackInsetX + TagGap, 0f);
            tag.sizeDelta = new Vector2(TagWidth, TagHeight);
            Frame(tag.gameObject.AddComponent<Image>(), panel);
            var floorValue = Number(tag, "Value", numbers, Amber, TextAlignmentOptions.Center);
            Stretch(floorValue.rectTransform);

            var bar = root.AddComponent<FloorBar>();
            GrayboxSetup.SetReferences(bar, ("runner", runner), ("track", track), ("segments", segments), ("fill", fill),
                ("ticks", ticks), ("tickTemplate", tick), ("record", record), ("recordValue", recordValue), ("marker", marker));
            var so = new SerializedObject(bar);
            var colors = so.FindProperty("biomeColors");
            colors.arraySize = BiomeColors.Length;
            for (int i = 0; i < BiomeColors.Length; i++) colors.GetArrayElementAtIndex(i).colorValue = BiomeColors[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // O número do andar é do HUD (os testes e o resto do jogo já leem dali).
            GrayboxSetup.SetReferences(hud, ("caveValue", floorValue));
            RemoveCavePanel(hud);
            Debug.Log("[Setup] Barra do andar à esquerda, com os biomas, o recorde e o andar de agora (M9d E3, D-278).");
            return true;
        }

        static bool RemoveCavePanel(HudView hud)
        {
            var cave = hud.transform.Find("Caverna");
            if (cave == null) return false;
            Object.DestroyImmediate(cave.gameObject);
            return true;
        }

        // ── DANO e VELOCIDADE no alto da LOJA ────────────────────────────────────────────────────────────────────────

        static bool EnsureShopStats(Transform safeArea, HudView hud)
        {
            var panel = safeArea.Find("Shop/Screen/Panel") as RectTransform;
            var damage = hud.transform.Find("Dano");
            var speed = hud.transform.Find("Velocidade");
            if (panel == null || panel.Find(StatsName) != null || damage == null || speed == null) return false;

            // Tudo o que fica abaixo da barra do título desce o tanto da faixa (as abas, as listas, o aviso da consulta).
            float shift = StatsHeight + StatsGap;
            foreach (Transform child in panel)
            {
                var rect = (RectTransform)child;
                if (rect.anchorMax.y < 1f || rect.offsetMax.y > -StatsTop + 1f) continue;
                if (rect.anchorMin.y >= 1f) rect.offsetMin -= new Vector2(0f, shift);
                rect.offsetMax -= new Vector2(0f, shift);
            }

            var stats = new GameObject(StatsName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            stats.transform.SetParent(panel, false);
            var statsRect = (RectTransform)stats.transform;
            statsRect.anchorMin = new Vector2(0f, 1f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.pivot = new Vector2(0.5f, 1f);
            statsRect.offsetMin = new Vector2(7f, -StatsTop - StatsHeight);
            statsRect.offsetMax = new Vector2(-7f, -StatsTop);
            var layout = stats.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = StatSpacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            foreach (var stat in new[] { damage, speed })
            {
                stat.SetParent(stats.transform, false);
                var element = stat.GetComponent<LayoutElement>() ?? stat.gameObject.AddComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = StatWidth;
            }
            Debug.Log("[Setup] DANO e VELOCIDADE no alto da LOJA; o HUD fica com LASCAS e VERMELHAS (M9d E3, D-278).");
            return true;
        }

        // ── Os bônus ao lado da barra ────────────────────────────────────────────────────────────────────────────────

        static bool MoveBuffColumn(Transform safeArea)
        {
            if (!(safeArea.Find("BuffColumn") is RectTransform column)) return false;
            if (column.anchorMin.x >= 1f) return false; // M9i E1: já foi para a direita (HudColumnV8Setup)
            if (Mathf.Approximately(column.anchoredPosition.x, BuffLeft)) return false;
            column.anchoredPosition = new Vector2(BuffLeft, column.anchoredPosition.y);
            EditorUtility.SetDirty(column);
            Debug.Log("[Setup] A coluna dos bônus passa para o lado da barra do andar (M9d E3).");
            return true;
        }

        // ── Ajudas ───────────────────────────────────────────────────────────────────────────────────────────────────

        static RectTransform Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Uma faixa fina que sai da borda direita da faixa interna (no meio da altura, a barra a posiciona).</summary>
        static void AtRightEdge(RectTransform rect, float x, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, height);
        }

        static void Paint(Image image, Color color)
        {
            image.color = color;
            image.raycastTarget = false;
        }

        /// <summary>A madeira do HUD; sem o kit (fora do git, D-65), um fundo escuro.</summary>
        static void Frame(Image image, Sprite panel)
        {
            if (panel != null)
            {
                image.sprite = panel;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else image.color = BarBack;
        }

        static TMP_Text Number(Transform parent, string name, TMP_FontAsset font, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            text.fontSize = NumberSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.text = "1";
            return text;
        }
    }
}
