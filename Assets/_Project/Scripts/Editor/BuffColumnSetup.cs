using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E2 do M9b (mockup aprovado em 2026-11-10): o <c>Data/BuffCatalog</c> com
    /// os quatro marcos do combo (ícones do kit Anvil, nomes, textos do ganho), os sprites do anel e do disco, e na cena
    /// Game a coluna de buffs (<see cref="BuffColumn"/>) à esquerda, logo abaixo do HUD, atrás das janelas. Liga o catálogo
    /// ao feedback (o texto do ganho). Só cria o que falta; os textos ficam no catálogo.
    /// </summary>
    static class BuffColumnSetup
    {
        internal const string CatalogPath = "Assets/_Project/Data/BuffCatalog.asset";
        const string RingPath = "Assets/_Project/Art/UI/buff_ring.png";
        const string DiscPath = "Assets/_Project/Art/UI/buff_disc.png";
        const string ColumnName = "BuffColumn";

        // Layout na referência 270 (D-14), como no mockup: o HUD vai até 42; a coluna começa 6 abaixo.
        const float Top = 48f;
        const float Left = 4f;
        const float SlotSize = 24f;
        const float Gap = 3f;
        const int MaxShown = 7;

        static readonly Color BackColor = new Color32(0x18, 0x10, 0x0A, 0xD2);
        static readonly Color RingColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color InnerColor = new Color32(0x18, 0x10, 0x0A, 0xFF);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color AmberText = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);

        /// <summary>Os marcos do combo (GDD v7 §3.4) no catálogo: marco, ícone do kit Anvil, nome, descrição, texto grande.</summary>
        static readonly (int tier, int icon, string title, string description, string headline)[] ComboEntries =
        {
            (10, 2, "+10% de dano", "Golpes e toques mais fortes", ""),
            (30, 2, "+15% de dano", "Golpes e toques mais fortes", ""),
            (50, 18, "Crítico garantido", "Todo golpe sai crítico", ""),
            (100, 332, "Frenesi", "+50% de dano", "FRENESI!"),
        };

        public static bool EnsureInScene()
        {
            bool changed = EnsureSprites();
            var catalog = EnsureCatalog(ref changed);
            if (catalog == null) return changed;
            changed |= WireFeedback(catalog);

            var safeArea = GameObject.Find("SafeArea")?.transform;
            if (safeArea == null || safeArea.Find(ColumnName) != null) return changed;
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var ring = AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
            var disc = AssetDatabase.LoadAssetAtPath<Sprite>(DiscPath);
            var numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            if (runner == null || ring == null || disc == null)
            {
                Debug.LogWarning($"[Setup] Coluna de buffs não criada: runner {(runner != null)}, anel {(ring != null)}, disco {(disc != null)}.");
                return changed;
            }

            var root = new GameObject(ColumnName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            // Logo depois do HUD: por cima do mapa, atrás da loja, das janelas e do DBG.
            var hud = Object.FindAnyObjectByType<HudView>();
            if (hud != null && hud.transform.parent == safeArea) root.transform.SetSiblingIndex(hud.transform.GetSiblingIndex() + 1);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(Left, -Top);
            rootRect.sizeDelta = new Vector2(SlotSize, MaxShown * (SlotSize + Gap));

            var slots = new GameObject("Slots", typeof(RectTransform), typeof(VerticalLayoutGroup));
            slots.transform.SetParent(root.transform, false);
            var slotsRect = (RectTransform)slots.transform;
            slotsRect.anchorMin = Vector2.zero;
            slotsRect.anchorMax = Vector2.one;
            slotsRect.offsetMin = slotsRect.offsetMax = Vector2.zero;
            var layout = slots.GetComponent<VerticalLayoutGroup>();
            layout.spacing = Gap;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var template = CreateSlot(root.transform, ring, disc, numbers);
            var (detail, title, description, footer) = CreateDetail(root.transform, font);

            var column = root.AddComponent<BuffColumn>();
            GrayboxSetup.SetReferences(column,
                ("runner", runner), ("catalog", catalog), ("slotTemplate", template), ("slots", slotsRect),
                ("detail", detail), ("detailTitle", title), ("detailDescription", description), ("detailFooter", footer));
            Debug.Log("[Setup] Coluna de buffs à esquerda, abaixo do HUD (M9b, E2).");
            return true;
        }

        /// <summary>Um ícone: fundo escuro (recebe o toque), o anel âmbar que esvazia, o miolo, o ícone e os segundos ao lado.</summary>
        static BuffSlot CreateSlot(Transform parent, Sprite ring, Sprite disc, TMP_FontAsset numbers)
        {
            var go = new GameObject("SlotTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(SlotSize, SlotSize);
            var back = go.GetComponent<Image>();
            back.sprite = disc;
            back.color = BackColor; // raycastTarget ligado: o toque na coluna não vira toque no mapa
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = back;

            var ringImage = Layer(go.transform, "Ring", ring, RingColor, SlotSize);
            ringImage.type = Image.Type.Filled;
            ringImage.fillMethod = Image.FillMethod.Radial360;
            ringImage.fillOrigin = (int)Image.Origin360.Top;
            ringImage.fillClockwise = true;
            Layer(go.transform, "Inner", disc, InnerColor, SlotSize - 5f);
            var icon = Layer(go.transform, "Icon", null, Color.white, 16f);
            icon.preserveAspect = true;

            var seconds = GrayboxSetup.CreateHudText(go.transform, "Seconds", "", 5f, TextColor, new Vector2(1f, 0f), new Vector2(1f, 1f));
            var secondsRect = seconds.rectTransform;
            secondsRect.pivot = new Vector2(0f, 0.5f);
            secondsRect.offsetMin = new Vector2(2f, 0f);
            secondsRect.offsetMax = new Vector2(30f, 0f);
            seconds.alignment = TextAlignmentOptions.MidlineLeft;
            if (numbers != null) seconds.font = numbers;

            var slot = go.AddComponent<BuffSlot>();
            GrayboxSetup.SetReferences(slot, ("ring", ringImage), ("icon", icon), ("seconds", seconds),
                                       ("button", button), ("group", go.GetComponent<CanvasGroup>()));
            go.SetActive(false);
            return slot;
        }

        static Image Layer(Transform parent, string name, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>O detalhe ao lado do ícone tocado: o quadro de madeira com o nome (âmbar), o efeito e o marco com o tempo.</summary>
        static (RectTransform, TMP_Text, TMP_Text, TMP_Text) CreateDetail(Transform parent, TMP_FontAsset font)
        {
            var go = new GameObject("Detail", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(SlotSize + 16f, -SlotSize / 2f);
            rect.sizeDelta = new Vector2(112f, 40f);
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiSkinSetup.WoodFolder + "/panels/panel_inset.png");
            image.type = Image.Type.Sliced;
            image.raycastTarget = false; // o detalhe não segura o jogo: o toque nele vale no mapa

            TMP_Text Line(string name, int index, Color color)
            {
                var text = GrayboxSetup.CreateHudText(go.transform, name, "", 7f, color, new Vector2(0f, 1f), new Vector2(1f, 1f));
                var lineRect = text.rectTransform;
                lineRect.pivot = new Vector2(0.5f, 1f);
                lineRect.offsetMin = new Vector2(6f, -4f - 10f * (index + 1));
                lineRect.offsetMax = new Vector2(-4f, -4f - 10f * index);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                if (font != null) text.font = font;
                return text;
            }

            var title = Line("Title", 0, AmberText);
            var description = Line("Description", 1, TextColor);
            var footer = Line("Footer", 2, MutedText);
            go.SetActive(false);
            return (rect, title, description, footer);
        }

        /// <summary>O anel e o disco gerados (48 px) entram como sprites de UI, sem filtro.</summary>
        static bool EnsureSprites()
        {
            bool changed = false;
            foreach (var path in new[] { RingPath, DiscPath })
            {
                if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                // O .meta mínimo da primeira versão fez o Unity importar como Cubemap (textureShape 2): sem Sprite.
                if (importer.textureType == TextureImporterType.Sprite && importer.filterMode == FilterMode.Point
                    && importer.textureShape == TextureImporterShape.Texture2D) continue;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                changed = true;
            }
            return changed;
        }

        static BuffCatalog EnsureCatalog(ref bool changed)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BuffCatalog>(CatalogPath);
            if (catalog != null) return catalog;

            catalog = ScriptableObject.CreateInstance<BuffCatalog>();
            var so = new SerializedObject(catalog);
            var list = so.FindProperty("entries");
            list.arraySize = ComboEntries.Length;
            for (int i = 0; i < ComboEntries.Length; i++)
            {
                var (tier, icon, title, description, headline) = ComboEntries[i];
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("source").enumValueIndex = (int)BonusSource.Combo;
                item.FindPropertyRelative("tier").intValue = tier;
                item.FindPropertyRelative("icon").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Sprite>(UiSkinSetup.IconsFolder + "/Icon" + icon + ".png");
                item.FindPropertyRelative("title").stringValue = title;
                item.FindPropertyRelative("description").stringValue = description;
                item.FindPropertyRelative("headline").stringValue = headline;
            }
            so.FindProperty("secondsFormat").stringValue = "{0}s";
            so.FindProperty("comboFooterFormat").stringValue = "Combo ×{0} · {1}s";
            so.FindProperty("gainDamageFormat").stringValue = "+{0} DANO";
            so.FindProperty("gainCritical").stringValue = "CRÍTICO GARANTIDO";
            so.FindProperty("gainBothFormat").stringValue = "+{0} DANO · CRÍTICO";
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Catálogo dos bônus criado em {CatalogPath}: os quatro marcos do combo (M9b, E2).");
            changed = true;
            return catalog;
        }

        /// <summary>O texto do ganho (D-215) lê o catálogo.</summary>
        static bool WireFeedback(BuffCatalog catalog)
        {
            var feedback = Object.FindAnyObjectByType<MiningFeedbackView>(FindObjectsInactive.Include);
            if (feedback == null) return false;
            var so = new SerializedObject(feedback);
            var field = so.FindProperty("buffs");
            if (field.objectReferenceValue == catalog) return false;
            field.objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Feedback: o texto do ganho do combo lê o catálogo dos bônus (M9b, E2).");
            return true;
        }
    }
}
