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
    /// Parte do <see cref="ProjectSetup"/> para o M9g, com a cena Game aberta: a faixa da Bomba no centro, acima do
    /// Descansar e do Próximo Andar, nos dois estilos dos mockups da E0 (o trilho no quadrado, A+C, ligado; o destravar, C,
    /// desligado), o voo da bomba no mundo (<see cref="BombFlightView"/>) e o menu DBG → Bomba
    /// (<see cref="BombDebugPanel"/>). As peças da faixa são as PNGs de <c>Art/UI/Bomb</c> (1 px = 1 px da referência).
    /// Na E3, a linha Bomba no catálogo da loja (descanso e consulta da LOJA, D-314). Só cria o que falta.
    /// </summary>
    static class BombSetup
    {
        const string ArtFolder = "Assets/_Project/Art/UI/Bomb/";
        const string RootName = "BombSlider";
        const string TrackStyleName = "TrackStyle";
        const string UnlockStyleName = "UnlockStyle";
        const string FlightName = "BombFlight";

        /// <summary>Faixa no centro, logo acima da barra de baixo (y 12 a 48), com 4 px de folga.</summary>
        const float SliderBottom = 52f;
        const float TrackWidth = 191f, TrackHeight = 45f;
        const float UnlockHeight = 35f;

        const string ReadyLabel = "ARRASTE";
        const float ReadySize = 33f / 4f; // 8,25: a grade da Pixelify
        const float SecondsSize = 8f;     // a grade da Press Start

        const int FlightBombs = 4;
        const int FlightSortingOrder = 30400; // abaixo das faíscas (30500), acima dos cristais

        static readonly Color AmberText = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color WhiteText = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        static readonly string[] Pieces =
        {
            "bomb_ac_slot", "bomb_ac_lit", "bomb_ac_knob_on", "bomb_ac_knob_off", "bomb_ac_cart_on", "bomb_ac_cart_off",
            "bomb_c_slot", "bomb_c_fill", "bomb_c_knob_on", "bomb_c_knob_off", "bomb_chevrons", "bomb_star_on", "bomb_star_off",
        };

        // O menu DBG → Bomba.
        const string FillName = "BombFill", FillLabel = "Encher";
        const string ThrowName = "BombThrow", ThrowLabel = "Lançar";
        const string NextName = "BombNext", NextLabel = "Próxima bomba";
        const string AutoName = "BombAuto", AutoLabel = "Automática";
        const string StyleName = "BombStyle", StyleLabel = "Estilo";

        // A linha Bomba no descanso e na LOJA (E3, D-314, mockup m9g-e0-descanso).
        const string ShopCatalogPath = "Assets/_Project/Data/ShopCatalog.asset";
        const string RowTitle = "Bomba";
        const string RowEffect = "{0} {1} » {2}";
        const string RowMaxed = "{0} {1}";
        static readonly string[] BombNames =
            { "Pequena", "Pequena Vermelha", "Normal", "Normal Vermelha", "Dinamite", "Pacote", "Caixa", "Barril" };

        static Sprite Piece(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + name + ".png");

        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            if (safeArea == null || runner == null) return false;

            ConfigureArt();
            bool changed = EnsureShopRow();
            changed |= EnsureSlider(safeArea, runner);
            changed |= EnsureFlight(safeArea, runner);
            changed |= EnsureDebugMenu(safeArea, runner);
            return changed;
        }

        /// <summary>As peças da faixa e a folha das bombas em pixel art (a pele da UI só as pega na rodada seguinte).</summary>
        static void ConfigureArt()
        {
            int changed = 0;
            foreach (var name in Pieces)
                if (System.IO.File.Exists(ArtFolder + name + ".png")) changed += UiSkinSetup.ConfigureSprite(ArtFolder + name + ".png", Vector4.zero);
            if (System.IO.File.Exists(BombArtSetup.SheetPath)) changed += UiSkinSetup.ConfigureSprite(BombArtSetup.SheetPath, Vector4.zero);
            if (changed > 0) Debug.Log($"[Setup] Bomba: peças da faixa configuradas (pixel art): {changed}.");
        }

        // ── A linha Bomba (E3) ───────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// A linha Bomba no catálogo das permanentes, logo depois do Atalho (antes da Promoção, como no mockup): "Pequena 10%
        /// » 20%", o ícone da bomba comprada (da folha) e a moldura dourada. A % vem do balanço, pela simulação.
        /// </summary>
        static bool EnsureShopRow()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            if (catalog == null) return false;
            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("permanentEntries");
            if (entries.arraySize == 0) return false;
            int index = entries.arraySize;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var existing = (MinersIdle.Core.PermanentId)entries.GetArrayElementAtIndex(i).FindPropertyRelative("permanent").enumValueIndex;
                if (existing == MinersIdle.Core.PermanentId.Bomb) return false;
                if (existing == MinersIdle.Core.PermanentId.Shortcut) index = i + 1;
            }

            entries.InsertArrayElementAtIndex(index);
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("permanent").enumValueIndex = (int)MinersIdle.Core.PermanentId.Bomb;
            entry.FindPropertyRelative("title").stringValue = RowTitle;
            entry.FindPropertyRelative("effectFormat").stringValue = RowEffect;
            entry.FindPropertyRelative("maxedFormat").stringValue = RowMaxed;
            entry.FindPropertyRelative("lockedFormat").stringValue = "";
            entry.FindPropertyRelative("style").enumValueIndex = (int)ShopCatalog.EffectStyle.Percent;
            var names = entry.FindPropertyRelative("levelNames");
            names.arraySize = BombNames.Length;
            for (int i = 0; i < BombNames.Length; i++) names.GetArrayElementAtIndex(i).stringValue = BombNames[i];
            entry.FindPropertyRelative("levelSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(BombArtSetup.SheetPath);
            entry.FindPropertyRelative("levelFrames").intValue = BombNames.Length;
            var (icon, frame) = PermanentShopSetup.ArtFor(MinersIdle.Core.PermanentId.Bomb);
            entry.FindPropertyRelative("icon").objectReferenceValue = icon;
            entry.FindPropertyRelative("frame").objectReferenceValue = frame;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Descanso e LOJA: a linha Bomba depois do Atalho (M9g E3, D-314).");
            return true;
        }

        // ── A faixa ──────────────────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureSlider(Transform safeArea, MiningRunner runner)
        {
            if (safeArea.Find(RootName) != null) return false;
            if (Piece("bomb_ac_slot") == null || Piece("bomb_c_slot") == null)
            {
                Debug.LogWarning($"[Setup] Bomba: as peças da faixa não estão em {ArtFolder}; a faixa fica para a próxima rodada.");
                return false;
            }
            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(BombArtSetup.SheetPath);
            var game = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            var numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);

            var root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            // Logo depois do Próximo Andar: as janelas (loja, descanso) continuam por cima.
            var nextFloor = safeArea.Find("NextFloor");
            if (nextFloor != null) root.transform.SetSiblingIndex(nextFloor.GetSiblingIndex() + 1);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, SliderBottom);
            rootRect.sizeDelta = new Vector2(TrackWidth, TrackHeight);

            // A+C: o trilho com dormentes e o quadrado dourado com a vagoneta e a bomba (o escolhido pelo Bruno).
            var track = Style(root.transform, TrackStyleName, TrackHeight, runner, sheet, game, numbers,
                              slot: "bomb_ac_slot", lit: "bomb_ac_lit", knobOn: "bomb_ac_knob_on", knobOff: "bomb_ac_knob_off",
                              knobSize: new Vector2(41f, 43f), bombOffset: new Vector2(0.5f, 4.5f), cart: true,
                              chevronsY: 6f, starPosition: new Vector2(72f, 8f), textY: 6f, fillFollowsKnob: false);
            // C: destravar, a faixa laranja segue o botão redondo.
            var unlock = Style(root.transform, UnlockStyleName, UnlockHeight, runner, sheet, game, numbers,
                               slot: "bomb_c_slot", lit: "bomb_c_fill", knobOn: "bomb_c_knob_on", knobOff: "bomb_c_knob_off",
                               knobSize: new Vector2(37f, 37f), bombOffset: new Vector2(0.5f, 1.5f), cart: false,
                               chevronsY: 0f, starPosition: new Vector2(72f, 0f), textY: 0f, fillFollowsKnob: true);
            track.SetActive(true);
            unlock.SetActive(false);

            Debug.Log("[Setup] Bomba: a faixa no centro, acima da barra de baixo, nos estilos A+C e C (M9g, D-310).");
            return true;
        }

        static GameObject Style(Transform root, string name, float height, MiningRunner runner, Texture2D sheet,
                                TMP_FontAsset game, TMP_FontAsset numbers, string slot, string lit, string knobOn,
                                string knobOff, Vector2 knobSize, Vector2 bombOffset, bool cart, float chevronsY,
                                Vector2 starPosition, float textY, bool fillFollowsKnob)
        {
            var style = new GameObject(name, typeof(RectTransform));
            style.transform.SetParent(root, false);
            var styleRect = (RectTransform)style.transform;
            styleRect.anchorMin = styleRect.anchorMax = styleRect.pivot = new Vector2(0.5f, 0.5f);
            styleRect.anchoredPosition = Vector2.zero;
            styleRect.sizeDelta = new Vector2(TrackWidth, height);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(style.transform, false);
            Stretch((RectTransform)content.transform);

            var slotImage = Picture(content.transform, "Slot", Piece(slot));
            Stretch(slotImage.rectTransform);
            slotImage.raycastTarget = true; // o dedo pega a faixa; tocar nela não vira toque no mapa

            var fill = Picture(content.transform, "Fill", Piece(lit));
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            var star = Picture(content.transform, "Star", Piece("bomb_star_off"));
            Center(star.rectTransform, starPosition, new Vector2(9f, 9f));

            var ready = Text(content.transform, "Ready", ReadyLabel, ReadySize, AmberText, game, new Vector2(20f, textY));
            var seconds = Text(content.transform, "Value", "5s", SecondsSize, WhiteText, numbers, new Vector2(20f, textY));

            var chevrons = Picture(content.transform, "Chevrons", Piece("bomb_chevrons"));
            Center(chevrons.rectTransform, new Vector2(0f, chevronsY), new Vector2(18f, 8f));

            var knob = new GameObject("Knob", typeof(RectTransform));
            knob.transform.SetParent(content.transform, false);
            var knobRect = (RectTransform)knob.transform;
            Center(knobRect, Vector2.zero, knobSize);
            var back = Picture(knob.transform, "Back", Piece(knobOff));
            Stretch(back.rectTransform);

            var bombObject = new GameObject("Bomb", typeof(RectTransform), typeof(RawImage));
            bombObject.transform.SetParent(knob.transform, false);
            var bomb = bombObject.GetComponent<RawImage>();
            bomb.texture = sheet;
            bomb.raycastTarget = false;
            Center(bomb.rectTransform, bombOffset, new Vector2(20f, 28f));

            Image cartImage = null;
            if (cart)
            {
                cartImage = Picture(knob.transform, "Cart", Piece("bomb_ac_cart_off"));
                Stretch(cartImage.rectTransform); // na frente da bomba
            }

            var slider = style.AddComponent<BombSlider>();
            GrayboxSetup.SetReferences(slider, ("runner", runner), ("content", content), ("track", styleRect),
                ("knob", knobRect), ("knobImage", back), ("knobOn", Piece(knobOn)), ("knobOff", Piece(knobOff)),
                ("fill", fill), ("bomb", bomb), ("bombSheet", sheet), ("chevrons", chevrons), ("star", star),
                ("starOn", Piece("bomb_star_on")), ("starOff", Piece("bomb_star_off")),
                ("readyText", ready), ("secondsText", seconds));
            if (cart)
                GrayboxSetup.SetReferences(slider, ("cartImage", cartImage), ("cartOn", Piece("bomb_ac_cart_on")),
                                           ("cartOff", Piece("bomb_ac_cart_off")));
            var serialized = new SerializedObject(slider);
            serialized.FindProperty("fillFollowsKnob").boolValue = fillFollowsKnob;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return style;
        }

        static Image Picture(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text Text(Transform parent, string name, string text, float size, Color color, TMP_FontAsset font,
                             Vector2 position)
        {
            var label = GrayboxSetup.CreateHudText(parent, name, text, size, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Center(label.rectTransform, position, new Vector2(90f, 16f));
            if (font != null) label.font = font;
            return label;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // ── O voo ────────────────────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureFlight(Transform safeArea, MiningRunner runner)
        {
            var effects = runner.transform.Find("Effects");
            if (effects == null || effects.Find(FlightName) != null) return false;

            var flight = new GameObject(FlightName);
            flight.transform.SetParent(effects, false);
            var bombs = new SpriteRenderer[FlightBombs];
            for (int i = 0; i < bombs.Length; i++)
            {
                bombs[i] = GrayboxSetup.CreateSprite("Bomb" + i, flight.transform, null, Color.white, Vector3.zero, Vector3.one);
                bombs[i].sortingOrder = FlightSortingOrder;
                bombs[i].gameObject.SetActive(false);
            }

            var view = flight.AddComponent<BombFlightView>();
            GrayboxSetup.SetReferences(view, ("runner", runner), ("stage", Object.FindAnyObjectByType<DescentStage>()),
                ("worldCamera", Camera.main), ("soundBoard", Object.FindAnyObjectByType<SoundBoard>(FindObjectsInactive.Include)),
                ("launchFrom", safeArea.Find(RootName) as RectTransform),
                ("bombSheet", AssetDatabase.LoadAssetAtPath<Texture2D>(BombArtSetup.SheetPath)));
            var serialized = new SerializedObject(view);
            var list = serialized.FindProperty("bombs");
            list.arraySize = bombs.Length;
            for (int i = 0; i < bombs.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = bombs[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup] Bomba: o voo em arco até os cristais (M9g, D-308).");
            return true;
        }

        // ── DBG → Bomba ──────────────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureDebugMenu(Transform safeArea, MiningRunner runner)
        {
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.BombMenuName) : null;
            if (menu == null || menu.GetComponent<BombDebugPanel>() != null) return false;

            var grid = menu.Find("Content/Buttons");
            var fill = MakeButton(grid, FillName, FillLabel, 0);
            var throwButton = MakeButton(grid, ThrowName, ThrowLabel, 1);
            var next = MakeButton(grid, NextName, NextLabel, 2);
            var auto = MakeButton(grid, AutoName, AutoLabel, 3);
            var style = MakeButton(grid, StyleName, StyleLabel, 4);
            grid.Find("Close")?.SetAsLastSibling();

            var panel = menu.gameObject.AddComponent<BombDebugPanel>();
            GrayboxSetup.SetReferences(panel, ("runner", runner), ("fillButton", fill), ("throwButton", throwButton),
                ("nextButton", next), ("autoButton", auto), ("styleButton", style),
                ("trackSlider", safeArea.Find(RootName + "/" + TrackStyleName)?.gameObject),
                ("unlockSlider", safeArea.Find(RootName + "/" + UnlockStyleName)?.gameObject));
            Debug.Log("[Setup] DBG → Bomba: Encher, Lançar, Próxima bomba, Automática e Estilo (A+C ou C).");
            return true;
        }

        static Button MakeButton(Transform grid, string name, string label, int index)
        {
            var existing = grid.Find(name);
            var button = existing != null ? existing.GetComponent<Button>() : DebugPanelSetup.CreateButton(grid, name, label);
            button.transform.SetSiblingIndex(index);
            return button;
        }
    }
}
