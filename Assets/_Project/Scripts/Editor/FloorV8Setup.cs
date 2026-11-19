using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// A E2 do M9i (barra do andar, escada e transição; mockup aprovado em 2026-11-19, D-335), na cena Game já montada:
    /// <list type="bullet">
    /// <item>a <see cref="FloorBar"/> com a borda <c>cave_meter</c> do Apellon (24×256 na tela de 270, 2 px por pixel de
    /// arte), <b>centrada</b> no espaço livre entre o HUD e a faixa da bomba; as cores dos biomas e o âmbar no miolo; a
    /// seta <c>cave_meter_arrow</c> do tamanho da marca antiga (1 px por pixel de arte);</item>
    /// <item>a escada <c>stair</c> no DESCER do Próximo Andar, a 24×24 (75%: 3 px de aparelho por pixel de arte no G75);</item>
    /// <item>a faixa do andar (<see cref="CaveBanner"/>) numa placa de madeira pendurada por duas cordas, com o texto
    /// ~2× maior em marrom escuro (D-332).</item>
    /// </list>
    /// Os outros setups não refazem nada disso (cada um checa se já fez). Idempotente.
    /// </summary>
    static class FloorV8Setup
    {
        const string ArtFolder = "Assets/_Project/Art/UI";
        const string MeterPath = ArtFolder + "/floor_meter.png";
        const string ArrowPath = ArtFolder + "/floor_arrow.png";
        const string StairPath = ArtFolder + "/icon_stair.png";
        const string PlatePath = ArtFolder + "/plate_wood.png";
        const string RopePath = ArtFolder + "/rope.png";
        const string NailPath = ArtFolder + "/nail.png";

        // A barra, na referência 270 (D-14). O miolo do desenho: 8 px de cada lado, 6 em cima e 8 embaixo.
        const float BarLeft = 4f;
        const float BarWidth = 24f, BarHeight = 256f;
        const float InsetSide = 8f, InsetTop = 6f, InsetBottom = 8f;
        const float HudBottom = 46f;   // o HUD (M9i E1)
        const float BombTop = 97f;     // a faixa da bomba, contada de baixo (BombSetup: 52 + 45)
        // Centrada no vão entre os dois: o centro fica (BombTop − HudBottom)/2 acima do meio da tela (25,5 → 26 px).
        static readonly float BarCenterOffset = Mathf.Round((BombTop - HudBottom) / 2f);
        const float ArrowWidth = 11f, ArrowHeight = 15f; // a seta a 1 px por pixel de arte (22×30 no PNG)
        const float ArrowLeft = InsetSide - 1f;          // a ponta encosta na borda da barra
        const float TagLeft = InsetSide + 9f;
        const float TagWidth = 36f;
        const float TickLeft = InsetSide + 1f;
        const float RecordReach = 4f;

        // A escada: 24×24 na tela de 270, no lugar da seta (o centro fica onde estava o da seta).
        const float StairSize = 24f, StairLeft = 6f;

        // A placa, em unidades do mundo (16 px de 270 por unidade): 176×60 px.
        const float PixelsPerUnit = 16f;
        static readonly Vector2 PlateSize = new Vector2(176f, 60f) / PixelsPerUnit;
        const float RopeInset = 28f / PixelsPerUnit;   // das pontas da placa até cada corda
        const float RopeLength = 40f;                  // passa do alto da tela
        const float NailDrop = 5f / PixelsPerUnit;     // do alto da placa até o prego
        const float BannerFontSize = 10f;              // era 4,5 (~2,2×)
        static readonly Color PlateText = new Color32(0x3A, 0x20, 0x10, 0xFF);

        public static bool EnsureInScene()
        {
            bool changed = EnsureSprites();
            var safeArea = GameObject.Find("SafeArea")?.transform;
            if (safeArea != null)
            {
                changed |= EnsureBar(safeArea);
                changed |= EnsureStair(safeArea);
            }
            changed |= EnsurePlate();
            return changed;
        }

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        /// <summary>Os sprites que este setup configura (PPU 16, a placa com borda): o setup da UI deixa eles de fora.</summary>
        internal static bool Owns(string path) =>
            path == MeterPath || path == ArrowPath || path == StairPath || path == PlatePath || path == RopePath || path == NailPath;

        // ── Os sprites ───────────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureSprites()
        {
            bool changed = false;
            foreach (var path in new[] { MeterPath, ArrowPath, StairPath, PlatePath, RopePath, NailPath })
            {
                if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                bool sliced = path == PlatePath;
                bool fullRect = sliced || path == RopePath;
                var border = sliced ? new Vector4(6f, 6f, 6f, 6f) : Vector4.zero;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (importer.textureType == TextureImporterType.Sprite && importer.filterMode == FilterMode.Point
                    && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed
                    && Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) && importer.spriteBorder == border
                    && settings.spriteMeshType == (fullRect ? SpriteMeshType.FullRect : settings.spriteMeshType))
                    continue;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.spriteBorder = border;
                importer.ReadTextureSettings(settings);
                if (fullRect) settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                changed = true;
            }
            return changed;
        }

        // ── A barra do andar ─────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureBar(Transform safeArea)
        {
            var root = safeArea.Find("FloorBar") as RectTransform;
            var meter = Load(MeterPath);
            var bar = root != null ? root.GetComponent<FloorBar>() : null;
            if (bar == null || meter == null) return false;
            var image = root.GetComponent<Image>();
            if (image.sprite == meter) return false;

            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 0.5f);
            root.sizeDelta = new Vector2(BarWidth, BarHeight);
            root.anchoredPosition = new Vector2(BarLeft, BarCenterOffset);
            image.sprite = meter;
            image.type = Image.Type.Simple;
            image.color = Color.white;

            var so = new SerializedObject(bar);
            if (so.FindProperty("track").objectReferenceValue is RectTransform track)
            {
                track.offsetMin = new Vector2(InsetSide, InsetBottom);
                track.offsetMax = new Vector2(-InsetSide, -InsetTop);
            }
            if (so.FindProperty("tickTemplate").objectReferenceValue is RectTransform tick)
                tick.anchoredPosition = new Vector2(TickLeft, tick.anchoredPosition.y);
            if (so.FindProperty("record").objectReferenceValue is RectTransform record)
                record.offsetMax = new Vector2(InsetSide + RecordReach, record.offsetMax.y);
            if (so.FindProperty("marker").objectReferenceValue is RectTransform marker)
            {
                marker.sizeDelta = new Vector2(TagLeft + TagWidth, Mathf.Max(marker.sizeDelta.y, ArrowHeight));
                if (marker.Find("Arrow") is RectTransform arrow)
                {
                    arrow.anchoredPosition = new Vector2(ArrowLeft, 0f);
                    arrow.sizeDelta = new Vector2(ArrowWidth, ArrowHeight);
                    var arrowImage = arrow.GetComponent<Image>();
                    arrowImage.sprite = Load(ArrowPath);
                    arrowImage.color = Color.white;
                    arrowImage.preserveAspect = false;
                }
                if (marker.Find("Tag") is RectTransform tag) tag.anchoredPosition = new Vector2(TagLeft, 0f);
            }
            EditorUtility.SetDirty(root.gameObject);
            Debug.Log("[Setup] Barra do andar com a borda e a seta do Apellon, no meio do vão entre o HUD e a bomba (M9i E2).");
            return true;
        }

        // ── A escada ─────────────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureStair(Transform safeArea)
        {
            var view = safeArea.GetComponentInChildren<NextFloorButton>(true);
            var ready = view != null ? new SerializedObject(view).FindProperty("ready").objectReferenceValue as GameObject : null;
            var arrow = ready != null ? ready.transform.Find("Arrow") as RectTransform : null;
            var stair = Load(StairPath);
            if (arrow == null || stair == null) return false;
            var image = arrow.GetComponent<Image>();
            if (image.sprite == stair) return false;
            image.sprite = stair;
            image.preserveAspect = true;
            arrow.anchoredPosition = new Vector2(StairLeft, 0f);
            arrow.sizeDelta = new Vector2(StairSize, StairSize);
            EditorUtility.SetDirty(arrow.gameObject);
            Debug.Log("[Setup] A escada do Apellon no DESCER do Próximo Andar (M9i E2).");
            return true;
        }

        // ── A placa da transição ─────────────────────────────────────────────────────────────────────────────────

        static bool EnsurePlate()
        {
            var banner = Object.FindAnyObjectByType<CaveBanner>(FindObjectsInactive.Include);
            if (banner == null) return false;
            var so = new SerializedObject(banner);
            var parts = so.FindProperty("plate");
            var label = so.FindProperty("label").objectReferenceValue as TextMeshPro;
            if (label == null || parts.arraySize > 0) return false;
            var plateSprite = Load(PlatePath);
            if (plateSprite == null) return false;

            label.fontSize = BannerFontSize;
            label.color = PlateText;
            int order = label.sortingOrder;
            int layer = label.sortingLayerID;

            SpriteRenderer Part(string name, Sprite sprite, SpriteDrawMode mode, Vector2 size, Vector2 position, int sorting)
            {
                var go = new GameObject(name, typeof(SpriteRenderer));
                go.transform.SetParent(label.transform, false);
                go.transform.localPosition = position;
                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.drawMode = mode;
                if (mode != SpriteDrawMode.Simple) renderer.size = size;
                renderer.sortingLayerID = layer;
                renderer.sortingOrder = sorting;
                return renderer;
            }

            float top = PlateSize.y / 2f;
            float ropeX = PlateSize.x / 2f - RopeInset;
            var rope = Load(RopePath);
            var nail = Load(NailPath);
            var list = new[]
            {
                Part("RopeLeft", rope, SpriteDrawMode.Tiled, new Vector2(3f / PixelsPerUnit, RopeLength),
                     new Vector2(-ropeX, top - NailDrop + RopeLength / 2f), order - 3),
                Part("RopeRight", rope, SpriteDrawMode.Tiled, new Vector2(3f / PixelsPerUnit, RopeLength),
                     new Vector2(ropeX, top - NailDrop + RopeLength / 2f), order - 3),
                Part("Plate", plateSprite, SpriteDrawMode.Sliced, PlateSize, Vector2.zero, order - 2),
                Part("NailLeft", nail, SpriteDrawMode.Simple, Vector2.zero, new Vector2(-ropeX, top - NailDrop), order - 1),
                Part("NailRight", nail, SpriteDrawMode.Simple, Vector2.zero, new Vector2(ropeX, top - NailDrop), order - 1),
            };
            parts.arraySize = list.Length;
            for (int i = 0; i < list.Length; i++) parts.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(label);
            EditorUtility.SetDirty(banner);
            Debug.Log("[Setup] O andar da transição numa placa de madeira pendurada, com o texto maior (M9i E2, D-332).");
            return true;
        }
    }
}
