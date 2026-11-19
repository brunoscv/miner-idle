using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// M9i E6 (#12, de-para aprovado em 2026-11-19, D-339): as peças do Apellon que ainda não estavam no jogo.
    /// <list type="bullet">
    /// <item>o cristal vermelho (<c>icon_crystal_red</c>) no lugar do cristal das lascas pintado de vermelho, em toda a
    /// tela e no preço da loja das permanentes;</item>
    /// <item>o botão laranja (<c>ui_button_rest</c>, 9-slice) nos dois botões de baixo: Descansar e Próximo andar. Aceso,
    /// com o texto em marrom como nos tiles; trancado ou enchendo, o laranja apagado (o que já encheu acende);</item>
    /// <item>a barra de carregamento (<c>ui_loading</c>) nas duas barras de tempo: o anúncio e a escolha do raro;</item>
    /// <item>o ponto <c>cave_meter_t</c> nas marcas dos andares da barra do andar;</item>
    /// <item>o Descansar <b>trancado</b> no jeito do DESCER (pedido do Bruno): o cadeado amarelo (<c>icon_lock</c>, 16×16)
    /// no lugar da escada e os dois textos nas medidas dos dele. Aberto, o Descansar fica como era.</item>
    /// </list>
    /// Roda por último no setup. Cada parte confere se já foi feita: idempotente.
    /// </summary>
    static class IconsV8Setup
    {
        const string Folder = "Assets/_Project/Art/UI";
        const string RedCrystalPath = Folder + "/icon_crystal_red.png";
        const string ButtonPath = Folder + "/ui_button_rest.png";
        const string LoadingPath = Folder + "/ui_loading.png";
        const string LoadingFillPath = Folder + "/ui_loading_fill.png";
        const string TickPath = Folder + "/cave_meter_t.png";
        const string CrystalIconPath = "Assets/_Project/Art/Crystals/icon_crystal.png";
        const string PermanentRowPath = "Assets/_Project/Prefabs/UI/PermanentRow.prefab";

        // As bordas do 9-slice, em pixels de arte (1 px de arte = 1 px da tela de 270), na ordem da Unity: L, B, R, T.
        // O brilho do canto de cima do botão fica inteiro na borda da esquerda (21 px).
        static readonly Vector4 ButtonBorder = new Vector4(21f, 6f, 6f, 6f);
        static readonly Vector4 LoadingBorder = new Vector4(4f, 4f, 4f, 4f);
        const float LoadingInset = 4f;   // a moldura do loading: o miolo começa a 4 px de cada lado
        const float TickSize = 3f;

        static readonly Color Dimmed = new Color(0.55f, 0.55f, 0.55f, 1f);       // o mesmo apagado do Descansar trancado
        static readonly Color BrownText = new Color32(0x3A, 0x20, 0x10, 0xFF);    // o texto dos tiles (E1)
        static readonly Color DimRed = new Color(0.55f, 0.55f, 0.55f, 1f);

        /// <summary>Os sprites com borda: o <see cref="UiSkinSetup"/> não os reconfigura (senão os dois brigam).</summary>
        internal static bool Owns(string path) => path == ButtonPath || path == LoadingPath;

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        public static bool EnsureInScene()
        {
            UiSkinSetup.ConfigureSprite(ButtonPath, ButtonBorder);
            UiSkinSetup.ConfigureSprite(LoadingPath, LoadingBorder);
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var red = Load(RedCrystalPath);
            var button = Load(ButtonPath);
            var loading = Load(LoadingPath);
            var loadingFill = Load(LoadingFillPath);
            var tick = Load(TickPath);
            if (safeArea == null || red == null || button == null || loading == null || loadingFill == null || tick == null)
                return false;

            bool changed = EnsureRedCrystal(safeArea, red);
            changed |= EnsureRedPrice(red);
            changed |= EnsureButtons(button);
            changed |= EnsureBars(loading, loadingFill);
            changed |= EnsureTicks(tick);
            changed |= EnsureLockedRest();
            return changed;
        }

        // ── 1. O cristal vermelho ───────────────────────────────────────────────────────────────────────────────

        static bool IsRedTint(Color color) => color.r > 0.5f && color.g < 0.45f && color.b < 0.45f;

        static bool EnsureRedCrystal(Transform safeArea, Sprite red)
        {
            var crystal = Load(CrystalIconPath);
            int count = 0;
            foreach (var image in safeArea.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != crystal || crystal == null || !IsRedTint(image.color)) continue;
                image.sprite = red;
                image.color = Color.white;
                EditorUtility.SetDirty(image);
                count++;
            }
            if (count == 0) return false;
            Debug.Log($"[Setup] O cristal vermelho do Apellon nas VERMELHAS: {count} ícones (M9i E6).");
            return true;
        }

        /// <summary>O preço da loja das permanentes: o cristal vermelho, e as cores dele de branco (dá) e apagado (não dá).</summary>
        static bool EnsureRedPrice(Sprite red)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PermanentRowPath);
            var view = prefab != null ? prefab.GetComponent<PermanentRowView>() : null;
            if (view == null) return false;
            var current = new SerializedObject(view).FindProperty("priceIcon").objectReferenceValue as Image;
            if (current == null || current.sprite == red) return false;

            var root = PrefabUtility.LoadPrefabContents(PermanentRowPath);
            try
            {
                var so = new SerializedObject(root.GetComponent<PermanentRowView>());
                var icon = (Image)so.FindProperty("priceIcon").objectReferenceValue;
                icon.sprite = red;
                icon.color = Color.white;
                so.FindProperty("buyableIconColor").colorValue = Color.white;
                so.FindProperty("unavailableIconColor").colorValue = DimRed;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PermanentRowPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("[Setup] Loja das permanentes: o preço com o cristal vermelho do Apellon (M9i E6).");
            return true;
        }

        // ── 2. Os botões de baixo ───────────────────────────────────────────────────────────────────────────────

        static bool EnsureButtons(Sprite sprite)
        {
            bool changed = false;
            var rest = Object.FindAnyObjectByType<RestButton>(FindObjectsInactive.Include);
            if (rest != null)
            {
                var so = new SerializedObject(rest);
                var background = so.FindProperty("background").objectReferenceValue as Image;
                if (background != null && background.sprite != sprite)
                {
                    Orange(background, sprite, Color.white);
                    // A cor do texto aceso (o RestButton guarda ela ao abrir); trancado, continua o cinza de antes.
                    if (so.FindProperty("label").objectReferenceValue is TMP_Text label)
                    {
                        label.color = BrownText;
                        EditorUtility.SetDirty(label);
                    }
                    changed = true;
                }
            }

            var next = Object.FindAnyObjectByType<NextFloorButton>(FindObjectsInactive.Include);
            if (next != null)
            {
                var so = new SerializedObject(next);
                var dim = (so.FindProperty("button").objectReferenceValue as Button)?.GetComponent<Image>();
                var lit = (so.FindProperty("lit").objectReferenceValue as RectTransform)?.GetComponent<Image>();
                if (dim != null && lit != null && dim.sprite != sprite)
                {
                    Orange(dim, sprite, Dimmed);
                    Orange(lit, sprite, Color.white);
                    // Cheio (DESCER · ANDAR n), o botão está todo aceso: o texto em marrom. Enchendo, fica o branco.
                    foreach (var field in new[] { "readyTitle", "target" })
                        if (so.FindProperty(field).objectReferenceValue is TMP_Text text)
                        {
                            text.color = BrownText;
                            EditorUtility.SetDirty(text);
                        }
                    changed = true;
                }
            }
            if (changed) Debug.Log("[Setup] Descansar e Próximo andar no botão laranja do Apellon (M9i E6).");
            return changed;
        }

        static void Orange(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = color;
            EditorUtility.SetDirty(image);
        }

        // ── 3. As barras de tempo ───────────────────────────────────────────────────────────────────────────────

        static bool EnsureBars(Sprite frame, Sprite fill)
        {
            int count = 0;
            var ad = Object.FindAnyObjectByType<SimulatedAdScreen>(FindObjectsInactive.Include);
            if (ad != null && Bar(new SerializedObject(ad).FindProperty("barFill").objectReferenceValue as RectTransform, frame, fill))
                count++;
            var rare = Object.FindAnyObjectByType<RareWindowView>(FindObjectsInactive.Include);
            if (rare != null && Bar(new SerializedObject(rare).FindProperty("timerFill").objectReferenceValue as RectTransform, frame, fill))
                count++;
            if (count == 0) return false;
            Debug.Log($"[Setup] A barra de carregamento do Apellon nas barras de tempo: {count} (M9i E6).");
            return true;
        }

        /// <summary>
        /// A barra cujo preenchimento é <paramref name="fillRect"/>: a moldura é o "Bar" acima dele; o miolo (o próprio
        /// preenchimento ou o "Track" que o contém) fica a 4 px da borda; um "Track" com fundo some (a moldura já tem o
        /// miolo escuro).
        /// </summary>
        static bool Bar(RectTransform fillRect, Sprite frame, Sprite fill)
        {
            if (fillRect == null) return false;
            var inner = fillRect;
            var bar = (RectTransform)fillRect.parent;
            if (bar != null && bar.name != "Bar")
            {
                inner = bar;
                bar = (RectTransform)bar.parent;
            }
            var frameImage = bar != null ? bar.GetComponent<Image>() : null;
            if (frameImage == null || frameImage.sprite == frame) return false;

            frameImage.sprite = frame;
            frameImage.type = Image.Type.Sliced;
            frameImage.pixelsPerUnitMultiplier = 1f;
            frameImage.color = Color.white;
            EditorUtility.SetDirty(frameImage);

            inner.offsetMin = new Vector2(LoadingInset, LoadingInset);
            inner.offsetMax = new Vector2(-LoadingInset, -LoadingInset);
            EditorUtility.SetDirty(inner);
            if (inner != fillRect && inner.TryGetComponent<Image>(out var track))
            {
                track.enabled = false;
                EditorUtility.SetDirty(track);
            }

            var fillImage = fillRect.GetComponent<Image>();
            fillImage.sprite = fill;
            fillImage.type = Image.Type.Simple;
            fillImage.color = Color.white;
            EditorUtility.SetDirty(fillImage);
            return true;
        }

        // ── 4. As marcas da barra do andar ──────────────────────────────────────────────────────────────────────

        static bool EnsureTicks(Sprite dot)
        {
            var bar = Object.FindAnyObjectByType<FloorBar>(FindObjectsInactive.Include);
            var template = bar != null ? new SerializedObject(bar).FindProperty("tickTemplate").objectReferenceValue as RectTransform : null;
            var image = template != null ? template.GetComponent<Image>() : null;
            if (image == null || image.sprite == dot) return false;
            image.sprite = dot;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            template.sizeDelta = new Vector2(TickSize, TickSize);
            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(template);
            Debug.Log("[Setup] Barra do andar: as marcas viram o ponto do Apellon (M9i E6).");
            return true;
        }
    
        // ── 5. O Descansar trancado no jeito do DESCER ──────────────────────────────────────────────────────────

        /// <summary>
        /// Um grupo "Locked" dentro do Descansar, fora do layout dele: o cadeado onde fica a escada do DESCER e o título e
        /// a linha de baixo com a fonte, o tamanho e o lugar dos do DESCER (copiados dele, para os dois ficarem iguais).
        /// O <see cref="RestButton"/> liga o grupo só trancado.
        /// </summary>
        static bool EnsureLockedRest()
        {
            var rest = Object.FindAnyObjectByType<RestButton>(FindObjectsInactive.Include);
            var next = Object.FindAnyObjectByType<NextFloorButton>(FindObjectsInactive.Include);
            if (rest == null || next == null) return false;
            var so = new SerializedObject(rest);
            if (so.FindProperty("lockedContent").objectReferenceValue != null) return false;
            var button = (so.FindProperty("background").objectReferenceValue as Image)?.transform;
            var lockSprite = so.FindProperty("lockSprite").objectReferenceValue as Sprite;
            var color = so.FindProperty("lockedTextColor").colorValue;
            var nextSo = new SerializedObject(next);
            var ready = nextSo.FindProperty("ready").objectReferenceValue as GameObject;
            var stair = ready != null ? ready.transform.Find("Arrow") as RectTransform : null;
            var readyTitle = nextSo.FindProperty("readyTitle").objectReferenceValue as TMP_Text;
            var target = nextSo.FindProperty("target").objectReferenceValue as TMP_Text;
            if (button == null || lockSprite == null || stair == null || readyTitle == null || target == null) return false;

            var group = new GameObject("Locked", typeof(RectTransform), typeof(LayoutElement));
            group.transform.SetParent(button, false);
            group.GetComponent<LayoutElement>().ignoreLayout = true;
            var groupRect = (RectTransform)group.transform;
            groupRect.anchorMin = Vector2.zero;
            groupRect.anchorMax = Vector2.one;
            groupRect.offsetMin = groupRect.offsetMax = Vector2.zero;

            var lockIcon = new GameObject("Lock", typeof(RectTransform), typeof(Image));
            lockIcon.transform.SetParent(group.transform, false);
            CopyRect(stair, (RectTransform)lockIcon.transform);
            var image = lockIcon.GetComponent<Image>();
            image.sprite = lockSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var title = CopyText(readyTitle, group.transform, "Title", color);
            var floor = CopyText(target, group.transform, "Floor", color);
            group.SetActive(false);

            GrayboxSetup.SetReferences(rest, ("lockedContent", group), ("lockedTitle", title), ("lockedFloor", floor));
            EditorUtility.SetDirty(rest);
            Debug.Log("[Setup] Descansar trancado no jeito do DESCER: o cadeado amarelo e os textos do mesmo tamanho (M9i E6).");
            return true;
        }

        static void CopyRect(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
        }

        static TMP_Text CopyText(TMP_Text from, Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            CopyRect(from.rectTransform, (RectTransform)go.transform);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = from.font;
            text.fontSharedMaterial = from.fontSharedMaterial;
            text.fontSize = from.fontSize;
            text.enableAutoSizing = false;
            text.fontStyle = from.fontStyle;
            text.alignment = from.alignment;
            text.margin = from.margin;
            text.characterSpacing = from.characterSpacing;
            text.textWrappingMode = from.textWrappingMode;
            text.overflowMode = from.overflowMode;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
