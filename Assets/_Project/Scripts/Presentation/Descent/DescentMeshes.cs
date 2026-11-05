using System;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// As malhas da descida (M6, E1), feitas por código: o quadrado dos sprites (pivô no meio da base), a caixa dos esteios
    /// e o corredor (chão, paredes e teto, cada um num submesh com o seu material).
    /// </summary>
    public static class DescentMeshes
    {
        public const int FloorSubmesh = 0;
        public const int WallSubmesh = 1;
        public const int CeilingSubmesh = 2;

        /// <summary>Quadrado de 1 m de pé, virado para a câmera: x de −0,5 a 0,5, y de 0 a 1.</summary>
        public static Mesh Quad()
        {
            var mesh = new Mesh { name = "DescentQuad" };
            mesh.SetVertices(new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0.5f, 1f, 0f), new Vector3(-0.5f, 1f, 0f) });
            mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Quadrado deitado no chão, centrado: x e z de −0,5 a 0,5 (a sombra).</summary>
        public static Mesh FlatQuad()
        {
            var mesh = new Mesh { name = "DescentFlatQuad" };
            mesh.SetVertices(new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) });
            mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Caixa de 1 m centrada na base (y de 0 a 1), com a textura inteira em cada face.</summary>
        public static Mesh Box()
        {
            var corners = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 1f, -0.5f), new Vector3(-0.5f, 1f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 1f, 0.5f), new Vector3(-0.5f, 1f, 0.5f),
            };
            int[][] faces =
            {
                new[] { 0, 1, 2, 3 }, new[] { 5, 4, 7, 6 }, new[] { 4, 0, 3, 7 },
                new[] { 1, 5, 6, 2 }, new[] { 3, 2, 6, 7 }, new[] { 4, 5, 1, 0 },
            };

            var vertices = new Vector3[24];
            var uvs = new Vector2[24];
            var triangles = new int[36];
            for (int f = 0; f < faces.Length; f++)
            {
                for (int k = 0; k < 4; k++) vertices[f * 4 + k] = corners[faces[f][k]];
                uvs[f * 4] = new Vector2(0f, 0f);
                uvs[f * 4 + 1] = new Vector2(1f, 0f);
                uvs[f * 4 + 2] = new Vector2(1f, 1f);
                uvs[f * 4 + 3] = new Vector2(0f, 1f);
                int t = f * 6, v = f * 4;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }

            var mesh = new Mesh { name = "DescentBox", vertices = vertices, uv = uvs, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// O corredor parado de <paramref name="zNear"/> a <paramref name="zFar"/> (a câmera está em z = 0). Quem se move é a
        /// textura: o uv ao longo do corredor é −z (no chão, +z), em repetições de <see cref="DescentArt.CorridorTileMeters"/>, e o
        /// shader soma a distância corrida (<c>_DescentScroll</c>). Sem <paramref name="ceiling"/>, o teto fica vazio (câmera
        /// acima dele).
        ///
        /// Com <paramref name="moundHeight"/> &gt; 0, o chão sobe em curva nos últimos <paramref name="moundWidth"/> metros de
        /// cada lado até a parede (o montinho de areia da D-146): igual ao longo de todo o corredor, porque a malha fica parada
        /// e só a textura anda. O u continua x / tile, então a faixa de tiles não sai do lugar. A forma do montinho é
        /// <paramref name="mound"/> (<see cref="MoundProfile"/>).
        /// </summary>
        public static Mesh Corridor(float width, float height, float zNear, float zFar, bool ceiling,
                                    float moundWidth = 0f, float moundHeight = 0f, MoundForm mound = default)
        {
            float hw = width * 0.5f;
            float tile = DescentArt.CorridorTileMeters;
            bool hasMound = moundHeight > 0f && moundWidth > 0f;
            moundWidth = Mathf.Min(moundWidth, hw);
            int columns = hasMound ? 2 * (MoundSegments + 1) : 2;
            const int floorStart = 12; // 0–3 teto, 4–11 paredes, depois o chão
            var vertices = new Vector3[floorStart + columns * 2];
            var uvs = new Vector2[vertices.Length];

            void Put(int i, float x, float y, float z, float u, float v)
            {
                vertices[i] = new Vector3(x, y, z);
                uvs[i] = new Vector2(u / tile, v / tile);
            }

            // Teto: u atravessa o corredor, v corre ao longo dele.
            Put(0, -hw, height, zNear, -hw, -zNear); Put(1, hw, height, zNear, hw, -zNear);
            Put(2, hw, height, zFar, hw, -zFar); Put(3, -hw, height, zFar, -hw, -zFar);
            // Paredes: u corre ao longo do corredor, v sobe.
            Put(4, -hw, 0f, zNear, -zNear, 0f); Put(5, -hw, 0f, zFar, -zFar, 0f);
            Put(6, -hw, height, zFar, -zFar, height); Put(7, -hw, height, zNear, -zNear, height);
            Put(8, hw, 0f, zNear, -zNear, 0f); Put(9, hw, 0f, zFar, -zFar, 0f);
            Put(10, hw, height, zFar, -zFar, height); Put(11, hw, height, zNear, -zNear, height);

            // Chão: colunas de parede a parede, cada uma com um vértice perto e um longe. No chão, v cresce para longe da
            // câmera: o alto do PNG fica no alto da tela, como foi desenhado (com −z, o desenho aparecia de cabeça para baixo).
            var floor = new int[(columns - 1) * 6];
            for (int c = 0; c < columns; c++)
            {
                float x, y = 0f;
                if (!hasMound) x = c == 0 ? -hw : hw;
                else
                {
                    // Metade esquerda: da parede (t = 1) até o pé do monte (t = 0); a direita, espelhada.
                    bool left = c <= MoundSegments;
                    int k = left ? c : columns - 1 - c;
                    float t = 1f - (float)k / MoundSegments;
                    float fromWall = (1f - t) * moundWidth;
                    x = left ? -hw + fromWall : hw - fromWall;
                    y = moundHeight * MoundProfile(t, mound);
                }
                int near = floorStart + c * 2;
                Put(near, x, y, zNear, x, zNear);
                Put(near + 1, x, y, zFar, x, zFar);
                if (c == 0) continue;
                // A coluna anterior (a) e esta (b), perto e longe (+1): a mesma volta do chão plano de antes.
                int a = near - 2, b = near, i6 = (c - 1) * 6;
                floor[i6] = a; floor[i6 + 1] = b + 1; floor[i6 + 2] = b;
                floor[i6 + 3] = a; floor[i6 + 4] = a + 1; floor[i6 + 5] = b + 1;
            }

            var mesh = new Mesh { name = "DescentCorridor", subMeshCount = 3 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(floor, FloorSubmesh);
            mesh.SetTriangles(new[] { 4, 5, 6, 4, 6, 7, 8, 10, 9, 8, 11, 10 }, WallSubmesh);
            mesh.SetTriangles(ceiling ? new[] { 0, 1, 2, 0, 2, 3 } : Array.Empty<int>(), CeilingSubmesh);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Divisões da curva do montinho de cada lado (D-146): o platô tem laterais curtas e precisa de mais.</summary>
        public const int MoundSegments = 24;

        /// <summary>
        /// A altura do montinho (0 a 1) a <paramref name="t"/> do pé (0) até a parede (1), no corte de um lado (D-146).
        /// <see cref="MoundShape.Arch"/>: um ∩, alto e arredondado no meio (seno). <see cref="MoundShape.Plateau"/>: sobe numa
        /// lateral curta, fica reto em cima e desce na outra, com as quinas um pouco arredondadas. Os dois voltam ao chão
        /// junto da parede. O shader repete as mesmas contas para a luz.
        /// </summary>
        public static float MoundProfile(float t, MoundForm mound)
        {
            t = Mathf.Clamp01(t);
            if (mound.shape == MoundShape.Arch) return Mathf.Sin(Mathf.PI * t);
            float side = Mathf.Clamp(mound.plateauSide, 0.01f, 0.5f);
            return RoundedRamp(t / side, mound.plateauRoundness) * RoundedRamp((1f - t) / side, mound.plateauRoundness);
        }

        /// <summary>
        /// Rampa de 0 a 1 com as pontas arredondadas: <paramref name="roundness"/> 0 = reta (quina viva), 1 = smoothstep
        /// (bem redonda).
        /// </summary>
        public static float RoundedRamp(float u, float roundness)
        {
            u = Mathf.Clamp01(u);
            return Mathf.Lerp(u, u * u * (3f - 2f * u), Mathf.Clamp01(roundness));
        }
    }

    /// <summary>A forma do montinho de areia das beiras (D-146), no corte de um lado do corredor.</summary>
    public enum MoundShape
    {
        /// <summary>Um ∩: sobe, arredonda em cima e desce até o chão de novo junto da parede.</summary>
        Arch,
        /// <summary>Um platô: laterais curvas e curtas, topo achatado, quinas um pouco arredondadas.</summary>
        Plateau,
    }

    /// <summary>A forma do montinho e, no platô, o tamanho das laterais e o arredondado das quinas (D-146).</summary>
    [Serializable]
    public struct MoundForm
    {
        public MoundShape shape;
        [Tooltip("Platô: fração da largura do montinho que cada lateral ocupa (0,3 = 30% para subir, 30% para descer).")]
        [Range(0.05f, 0.5f)] public float plateauSide;
        [Tooltip("Platô: 0 = quinas vivas, 1 = bem redondas.")]
        [Range(0f, 1f)] public float plateauRoundness;

        public MoundForm(MoundShape shape, float plateauSide = 0.3f, float plateauRoundness = 0.6f)
        {
            this.shape = shape;
            this.plateauSide = plateauSide;
            this.plateauRoundness = plateauRoundness;
        }
    }

    /// <summary>
    /// Um objeto desenhado com o material da descida, com textura e cor próprias por <see cref="MaterialPropertyBlock"/>
    /// (trocar de quadro não cria material nem aloca). Usado para sprites, sombras, esteios e partículas.
    /// </summary>
    public sealed class DescentPiece
    {
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMapSTId = Shader.PropertyToID("_BaseMap_ST");

        readonly MeshRenderer _renderer;
        readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        Texture _texture;
        int _frame = -1;
        Color _color = Color.white;
        bool _visible = true;

        public Transform Transform { get; }

        public DescentPiece(Transform parent, string name, Mesh mesh, Material material, Texture texture)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            Transform = go.transform;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            _renderer = go.GetComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _texture = texture;
            Apply();
        }

        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value) return;
                _visible = value;
                _renderer.enabled = value;
            }
        }

        public void SetTexture(Texture texture)
        {
            if (texture == _texture && _frame < 0) return;
            _texture = texture;
            _frame = -1;
            _block.SetVector(BaseMapSTId, new Vector4(1f, 1f, 0f, 0f));
            Apply();
        }

        /// <summary>
        /// Um quadro de uma folha em grade de quadros quadrados (da esquerda para a direita, de cima para baixo), sem criar
        /// textura nem material por quadro: só muda o recorte da textura (_BaseMap_ST).
        /// </summary>
        public void SetFrame(Texture sheet, int index, int columns)
        {
            if (sheet == null || (sheet == _texture && index == _frame)) return;
            _frame = index;
            int rows = Mathf.Max(1, sheet.height * columns / Mathf.Max(1, sheet.width));
            int column = index % columns, row = index / columns;
            _texture = sheet;
            _block.SetVector(BaseMapSTId, new Vector4(1f / columns, 1f / rows, (float)column / columns, (float)(rows - 1 - row) / rows));
            Apply();
        }

        public void SetColor(Color color)
        {
            if (color == _color) return;
            _color = color;
            Apply();
        }

        /// <summary>Mostra em <paramref name="position"/> (pivô no meio da base) com o tamanho em metros.</summary>
        public void Place(Vector3 position, Vector2 size) => Place(position, new Vector3(size.x, size.y, 1f));

        /// <summary>Mostra com a escala dada nos três eixos (caixas e sombras deitadas).</summary>
        public void Place(Vector3 position, Vector3 scale)
        {
            Transform.localPosition = position;
            Transform.localScale = scale;
            Visible = true;
        }

        void Apply()
        {
            if (_texture != null) _block.SetTexture(BaseMapId, _texture);
            _block.SetColor(BaseColorId, _color);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
