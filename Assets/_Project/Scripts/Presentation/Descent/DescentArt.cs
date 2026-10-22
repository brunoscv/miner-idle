using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// A arte provisória da descida (M6, E1), desenhada por código uma vez ao abrir a cena, como os bipes do M5
    /// (<see cref="SfxSynth"/>): o anão e o estagiário de frente (4 quadros de corrida e 4 de golpe), os cristais nas
    /// cinco cores do mockup e as texturas de pedra, madeira e teto do corredor. É o mesmo desenho do simulador da
    /// proposta; a arte de verdade vem do Art Spike. Determinística (o "ruído" das pedras é um hash fixo).
    /// </summary>
    public sealed class DescentArt
    {
        /// <summary>Densidade dos sprites: ~2 pixels da tela de 270 px por pixel do sprite, na distância do anão.</summary>
        public const float SpritePixelsPerMeter = 64f;

        /// <summary>Densidade das texturas do corredor (uma pedra de 1 m tem 32 pixels).</summary>
        public const int CorridorPixelsPerMeter = 32;

        /// <summary>Metros cobertos por uma repetição das texturas do corredor (nas duas direções).</summary>
        public const float CorridorTileMeters = 2f;

        public const int Frames = 4;

        /// <summary>Tamanho do sprite do anão em metros: largo, para caber a picareta erguida dos dois lados do pivô.</summary>
        public static readonly Vector2 DwarfSize = new Vector2(2.4f, 2f);
        public static readonly Vector2 CrystalSize = new Vector2(0.84f, 0.8f);
        public static readonly Vector2 DecorSize = new Vector2(0.3f, 0.32f);
        public static readonly Vector2 LanternSize = new Vector2(0.2f, 0.26f);
        public static readonly Vector2 ShadowSize = new Vector2(0.7f, 0.16f);

        /// <summary>Cores dos cristais do mockup (claro, escuro): branco, dourado, azul, roxo e vermelho.</summary>
        public static readonly Color32[,] CrystalColors =
        {
            { new Color32(0xEC, 0xEB, 0xF4, 0xFF), new Color32(0xB9, 0xB6, 0xCC, 0xFF) },
            { new Color32(0xF6, 0xC9, 0x33, 0xFF), new Color32(0xC4, 0x8A, 0x12, 0xFF) },
            { new Color32(0x4F, 0x82, 0xF2, 0xFF), new Color32(0x2A, 0x4B, 0xB0, 0xFF) },
            { new Color32(0xB5, 0x52, 0xF2, 0xFF), new Color32(0x7A, 0x2A, 0xB0, 0xFF) },
            { new Color32(0xF2, 0x50, 0x5F, 0xFF), new Color32(0xB0, 0x2A, 0x3A, 0xFF) },
        };

        public static int CrystalColorCount => CrystalColors.GetLength(0);

        public Texture2D[] DwarfRun { get; private set; }
        public Texture2D[] DwarfMine { get; private set; }
        public Texture2D[] InternRun { get; private set; }
        public Texture2D[] InternMine { get; private set; }
        public Texture2D[] Crystals { get; private set; }
        public Texture2D[] Decor { get; private set; }
        public Texture2D Lantern { get; private set; }
        public Texture2D Shadow { get; private set; }
        public Texture2D White { get; private set; }
        public Texture2D Floor { get; private set; }
        public Texture2D Wall { get; private set; }
        public Texture2D Ceiling { get; private set; }
        public Texture2D Wood { get; private set; }

        /// <summary>Ângulos da picareta em cada quadro do golpe (radianos, sentido horário na tela): erguida, descendo, no cristal, voltando.</summary>
        static readonly float[] SwingAngles = { -1.55f, 0.3f, 2.2f, 0.6f };

        /// <summary>Picareta no ombro, correndo.</summary>
        const float CarryAngle = -0.75f;

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        public static DescentArt Create()
        {
            var art = new DescentArt
            {
                DwarfRun = new Texture2D[Frames],
                DwarfMine = new Texture2D[Frames],
                InternRun = new Texture2D[Frames],
                InternMine = new Texture2D[Frames],
                Crystals = new Texture2D[CrystalColorCount],
                Decor = new Texture2D[CrystalColorCount],
            };

            var dwarf = new Palette(new Color32(0xD9, 0xA5, 0x20, 0xFF), new Color32(0xB8, 0x40, 0x2A, 0xFF), lamp: true);
            var intern = new Palette(new Color32(0x7F, 0x95, 0xB0, 0xFF), new Color32(0x6B, 0x3F, 0x1F, 0xFF), lamp: false);
            for (int i = 0; i < Frames; i++)
            {
                float phase = i * Mathf.PI * 0.5f;
                art.DwarfRun[i] = Sprite($"DwarfRun{i}", DwarfSize, (x, y) => DwarfPixel(x, y, dwarf, phase, CarryAngle, true));
                art.DwarfMine[i] = Sprite($"DwarfMine{i}", DwarfSize, (x, y) => DwarfPixel(x, y, dwarf, 0f, SwingAngles[i], false));
                art.InternRun[i] = Sprite($"InternRun{i}", DwarfSize, (x, y) => DwarfPixel(x, y, intern, phase + 1.3f, CarryAngle, true));
                art.InternMine[i] = Sprite($"InternMine{i}", DwarfSize, (x, y) => DwarfPixel(x, y, intern, 0f, SwingAngles[i] + 0.4f, false));
            }

            for (int c = 0; c < CrystalColorCount; c++)
            {
                var light = CrystalColors[c, 0];
                var dark = CrystalColors[c, 1];
                art.Crystals[c] = Sprite($"Crystal{c}", CrystalSize, (x, y) => CrystalPixel(x, y, light, dark));
                art.Decor[c] = Sprite($"Decor{c}", DecorSize, (x, y) => DecorPixel(x, y, light, dark));
            }

            art.Lantern = Sprite("Lantern", LanternSize, LanternPixel);
            art.Shadow = ShadowTexture();
            art.White = Solid("White", 4, 4, new Color32(0xFF, 0xFF, 0xFF, 0xFF));
            art.Floor = Tile("Floor", FloorPixel);
            art.Wall = Tile("Wall", WallPixel);
            art.Ceiling = Tile("Ceiling", CeilingPixel);
            art.Wood = Tile("Wood", WoodPixel);
            return art;
        }

        /// <summary>Libera as texturas (ao fechar a cena).</summary>
        public void Destroy()
        {
            DestroyAll(DwarfRun);
            DestroyAll(DwarfMine);
            DestroyAll(InternRun);
            DestroyAll(InternMine);
            DestroyAll(Crystals);
            DestroyAll(Decor);
            DestroyAll(new[] { Lantern, Shadow, White, Floor, Wall, Ceiling, Wood });
        }

        static void DestroyAll(Texture2D[] textures)
        {
            if (textures == null) return;
            foreach (var texture in textures)
                if (texture != null) Object.Destroy(texture);
        }

        // ---------------------------------------------------------------------------------------------------------
        // Texturas

        /// <summary>Sprite com o pivô no meio da base: (x, y) em metros, x = 0 no centro, y = 0 no chão.</summary>
        static Texture2D Sprite(string name, Vector2 size, Func<float, float, Color32> paint)
        {
            int width = Mathf.RoundToInt(size.x * SpritePixelsPerMeter);
            int height = Mathf.RoundToInt(size.y * SpritePixelsPerMeter);
            var pixels = new Color32[width * height];
            for (int py = 0; py < height; py++)
            for (int px = 0; px < width; px++)
            {
                float x = (px + 0.5f) / SpritePixelsPerMeter - size.x * 0.5f;
                float y = (py + 0.5f) / SpritePixelsPerMeter;
                pixels[py * width + px] = paint(x, y);
            }

            return Build(name, width, height, pixels, TextureWrapMode.Clamp, mipmaps: false);
        }

        /// <summary>Textura que se repete a cada <see cref="CorridorTileMeters"/> nas duas direções; (px, py) em pixels.</summary>
        static Texture2D Tile(string name, Func<int, int, Color32> paint)
        {
            int size = Mathf.RoundToInt(CorridorTileMeters * CorridorPixelsPerMeter);
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
                pixels[py * size + px] = paint(px, py);

            // Mipmaps com filtro "point": de longe a pedra não cintila tanto (R-25).
            return Build(name, size, size, pixels, TextureWrapMode.Repeat, mipmaps: true);
        }

        static Texture2D Solid(string name, int width, int height, Color32 color)
        {
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            return Build(name, width, height, pixels, TextureWrapMode.Clamp, mipmaps: false);
        }

        static Texture2D Build(string name, int width, int height, Color32[] pixels, TextureWrapMode wrap, bool mipmaps)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipmaps)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = wrap,
            };
            texture.SetPixels32(pixels);
            texture.Apply(mipmaps, makeNoLongerReadable: true);
            return texture;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Anão (o mesmo desenho do simulador da proposta, em metros, y para cima)

        readonly struct Palette
        {
            public readonly Color32 Helmet, Beard;
            public readonly bool Lamp;

            public Palette(Color32 helmet, Color32 beard, bool lamp)
            {
                Helmet = helmet;
                Beard = beard;
                Lamp = lamp;
            }
        }

        static readonly Color32 Trousers = new Color32(0x4A, 0x3A, 0x2C, 0xFF);
        static readonly Color32 Boots = new Color32(0x2A, 0x1C, 0x12, 0xFF);
        static readonly Color32 Tunic = new Color32(0x7A, 0x4A, 0x2A, 0xFF);
        static readonly Color32 Belt = new Color32(0x3A, 0x24, 0x12, 0xFF);
        static readonly Color32 Buckle = new Color32(0xE0, 0xB0, 0x40, 0xFF);
        static readonly Color32 Sleeve = new Color32(0x6A, 0x3E, 0x22, 0xFF);
        static readonly Color32 Skin = new Color32(0xE8, 0xB4, 0x8A, 0xFF);
        static readonly Color32 Eyes = new Color32(0x2A, 0x1A, 0x10, 0xFF);
        static readonly Color32 LampGlow = new Color32(0xFF, 0xEC, 0xA8, 0xFF);
        static readonly Color32 LampLight = new Color32(0xFF, 0xF6, 0xC0, 0xFF);
        static readonly Color32 Handle = new Color32(0x8A, 0x5A, 0x30, 0xFF);
        static readonly Color32 Steel = new Color32(0x9A, 0xA0, 0xA8, 0xFF);

        static readonly Vector2[] BeardShape = { new(-0.2f, 1f), new(0.2f, 1f), new(0.13f, 0.7f), new(0f, 0.6f), new(-0.13f, 0.7f) };
        static readonly Vector2[] PickHead = { new(0.66f, -0.2f), new(0.74f, -0.2f), new(0.76f, 0.2f), new(0.72f, 0.24f), new(0.66f, 0f) };

        /// <param name="phase">Fase da passada (radianos); só vale correndo.</param>
        /// <param name="angle">Ângulo do braço da picareta, na tela (y para baixo), como no simulador.</param>
        static Color32 DwarfPixel(float x, float y, Palette palette, float phase, float angle, bool running)
        {
            float s = Mathf.Sin(phase);
            float bob = running ? Mathf.Abs(s) * 0.04f : 0f;
            float lift1 = running ? Mathf.Max(0f, s) * 0.09f : 0f;
            float lift2 = running ? Mathf.Max(0f, -s) * 0.09f : 0f;
            float swing = running ? s * 0.06f : 0f;
            y -= bob;

            var c = Clear;
            if (Rect(x, y, -0.2f, 0.08f + lift1, -0.05f, 0.46f)) c = Trousers;
            if (Rect(x, y, 0.05f, 0.08f + lift2, 0.2f, 0.46f)) c = Trousers;
            if (Rect(x, y, -0.22f, lift1, -0.03f, 0.1f + lift1)) c = Boots;
            if (Rect(x, y, 0.03f, lift2, 0.22f, 0.1f + lift2)) c = Boots;
            if (RoundRect(x, y, -0.3f, 0.42f, 0.3f, 0.96f, 0.08f)) c = Tunic;
            if (Rect(x, y, -0.3f, 0.47f, 0.3f, 0.53f)) c = Belt;
            if (Rect(x, y, -0.05f, 0.46f, 0.05f, 0.54f)) c = Buckle;
            if (Rect(x, y, -0.41f, 0.58f - swing, -0.29f, 0.92f - swing)) c = Sleeve;
            if (Rect(x, y, -0.41f, 0.52f - swing, -0.29f, 0.6f - swing)) c = Skin;
            if (RoundRect(x, y, -0.17f, 0.93f, 0.17f, 1.17f, 0.05f)) c = Skin;
            if (Polygon(x, y, BeardShape)) c = palette.Beard;
            if (Rect(x, y, -0.09f, 1.05f, -0.05f, 1.09f) || Rect(x, y, 0.05f, 1.05f, 0.09f, 1.09f)) c = Eyes;
            if ((y >= 1.12f && Circle(x, y, 0f, 1.12f, 0.21f)) || Rect(x, y, -0.24f, 1.09f, 0.24f, 1.14f)) c = palette.Helmet;
            if (palette.Lamp)
            {
                if (Circle(x, y, 0f, 1.22f, 0.075f)) c = LampGlow;
                if (Circle(x, y, 0f, 1.22f, 0.045f)) c = LampLight;
            }

            // Braço da picareta: o ponto vai para o sistema do braço (u ao longo dele, v de lado), girado em volta do ombro.
            float sx = x - 0.33f;
            float sy = -(y - 0.88f);
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            float u = cos * sx + sin * sy;
            float v = -sin * sx + cos * sy;
            if (u >= 0f && u <= 0.3f && Mathf.Abs(v) <= 0.055f) c = Sleeve;
            if (u >= 0.27f && u <= 0.35f && Mathf.Abs(v) <= 0.06f) c = Skin;
            if (u >= 0.3f && u <= 0.72f && Mathf.Abs(v) <= 0.025f) c = Handle;
            if (Polygon(u, v, PickHead)) c = Steel;
            return c;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Cristais, lanterna, sombra

        static readonly Color32 Rock = new Color32(0x4A, 0x3A, 0x30, 0xFF);
        static readonly Color32 Highlight = new Color32(0xFF, 0xFF, 0xFF, 0xFF);

        static readonly Vector2[] ShardLeft = { new(-0.32f, 0.02f), new(-0.34f, 0.3f), new(-0.22f, 0.44f), new(-0.1f, 0.2f), new(-0.12f, 0.02f) };
        static readonly Vector2[] ShardRight = { new(0.32f, 0.02f), new(0.34f, 0.3f), new(0.22f, 0.44f), new(0.1f, 0.2f), new(0.12f, 0.02f) };
        static readonly Vector2[] ShardCenter = { new(-0.13f, 0.02f), new(-0.15f, 0.46f), new(0f, 0.72f), new(0.15f, 0.46f), new(0.13f, 0.02f) };
        static readonly Vector2[] FacetRight = { new(0f, 0.72f), new(0.15f, 0.46f), new(0.13f, 0.02f), new(0.02f, 0.02f) };

        static Color32 CrystalPixel(float x, float y, Color32 light, Color32 dark)
        {
            var c = Clear;
            if (Ellipse(x, y, 0f, 0.03f, 0.34f, 0.08f)) c = Rock;
            if (Polygon(x, y, ShardLeft) || Polygon(x, y, ShardRight)) c = dark;
            if (Polygon(x, y, ShardCenter)) c = light;
            if (Polygon(x, y, FacetRight)) c = dark;
            if (Rect(x, y, -0.08f, 0.25f, -0.04f, 0.5f)) c = Highlight;
            return c;
        }

        static readonly Vector2[] DecorBig = { new(-0.12f, 0f), new(-0.02f, 0.3f), new(0.06f, 0f) };
        static readonly Vector2[] DecorSmall = { new(0.02f, 0f), new(0.09f, 0.18f), new(0.14f, 0f) };

        static Color32 DecorPixel(float x, float y, Color32 light, Color32 dark)
        {
            if (Polygon(x, y, DecorBig)) return light;
            if (Polygon(x, y, DecorSmall)) return dark;
            return Clear;
        }

        static readonly Color32 LanternFrame = new Color32(0x3A, 0x24, 0x12, 0xFF);
        static readonly Color32 LanternFire = new Color32(0xFF, 0xC0, 0x60, 0xFF);
        static readonly Color32 LanternCore = new Color32(0xFF, 0xF0, 0xB0, 0xFF);

        static Color32 LanternPixel(float x, float y)
        {
            var c = Clear;
            if (Rect(x, y, -0.07f, 0.02f, 0.07f, 0.22f)) c = LanternFrame;
            if (Rect(x, y, -0.05f, 0.05f, 0.05f, 0.18f)) c = LanternFire;
            if (Rect(x, y, -0.02f, 0.08f, 0.02f, 0.14f)) c = LanternCore;
            return c;
        }

        /// <summary>Sombra pontilhada (meio a meio), o "transparente" do pixel art sem ordenação.</summary>
        static Texture2D ShadowTexture()
        {
            const int width = 32, height = 8;
            var pixels = new Color32[width * height];
            var shade = new Color32(0x0C, 0x08, 0x06, 0xFF);
            for (int py = 0; py < height; py++)
            for (int px = 0; px < width; px++)
            {
                float nx = (px + 0.5f) / width * 2f - 1f;
                float ny = (py + 0.5f) / height * 2f - 1f;
                bool inside = nx * nx + ny * ny <= 1f;
                pixels[py * width + px] = inside && ((px + py) & 1) == 0 ? shade : Clear;
            }
            return Build("Shadow", width, height, pixels, TextureWrapMode.Clamp, mipmaps: false);
        }

        // ---------------------------------------------------------------------------------------------------------
        // Corredor (pixels; a textura cobre 2 m × 2 m)

        static Color32 FloorPixel(int px, int py)
        {
            // Lajes de 1 m × 0,5 m em fileiras desencontradas, com junta escura de 1 pixel.
            int row = py / 16;
            int shifted = px + (row % 2) * 16;
            int slab = (shifted / 32) % 2;
            bool joint = py % 16 == 0 || shifted % 32 == 0;
            if (joint) return new Color32(0x3A, 0x2A, 0x20, 0xFF);
            int tone = (int)(Hash(slab, row) * 18f) + Speckle(px, py, 6);
            return Shade(96, 72, 54, tone);
        }

        static Color32 WallPixel(int px, int py)
        {
            // Pedras de 1 m × 0,5 m (u ao longo do corredor, v para cima), fileiras desencontradas.
            int row = py / 16;
            int shifted = px + (row % 2) * 16;
            int block = (shifted / 32) % 2;
            bool joint = py % 16 == 0 || py % 16 == 15 || shifted % 32 == 0;
            if (joint) return new Color32(0x28, 0x1C, 0x16, 0xFF);
            int tone = (int)(Hash(block + 7, row) * 24f) + Speckle(px, py, 8);
            return Shade(70, 52, 40, tone);
        }

        static Color32 CeilingPixel(int px, int py) => Shade(34, 24, 18, Speckle(px, py, 10));

        static Color32 WoodPixel(int px, int py)
        {
            bool grain = (px + (py / 7)) % 5 == 0;
            int tone = grain ? -18 : Speckle(px, py, 6);
            return Shade(110, 70, 36, tone);
        }

        static Color32 Shade(int r, int g, int b, int tone) =>
            new Color32((byte)Mathf.Clamp(r + tone, 0, 255), (byte)Mathf.Clamp(g + tone, 0, 255), (byte)Mathf.Clamp(b + tone, 0, 255), 0xFF);

        static int Speckle(int px, int py, int amount) => (int)((Hash(px, py) - 0.5f) * amount);

        /// <summary>Hash fixo em [0, 1): o mesmo do simulador.</summary>
        public static float Hash(float a, float b)
        {
            float s = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        // ---------------------------------------------------------------------------------------------------------
        // Formas

        static bool Rect(float x, float y, float x0, float y0, float x1, float y1) => x >= x0 && x <= x1 && y >= y0 && y <= y1;

        static bool RoundRect(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            if (!Rect(x, y, x0, y0, x1, y1)) return false;
            float cx = Mathf.Clamp(x, x0 + r, x1 - r);
            float cy = Mathf.Clamp(y, y0 + r, y1 - r);
            return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
        }

        static bool Circle(float x, float y, float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

        static bool Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float nx = (x - cx) / rx, ny = (y - cy) / ry;
            return nx * nx + ny * ny <= 1f;
        }

        static bool Polygon(float x, float y, Vector2[] points)
        {
            bool inside = false;
            for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            {
                var a = points[i];
                var b = points[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
