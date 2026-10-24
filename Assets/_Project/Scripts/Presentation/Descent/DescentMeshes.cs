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
        /// textura: o uv ao longo do corredor é −z (em repetições de <see cref="DescentArt.CorridorTileMeters"/>), e o
        /// shader soma a distância corrida (<c>_DescentScroll</c>). Sem <paramref name="ceiling"/>, o teto fica vazio (câmera
        /// acima dele).
        /// </summary>
        public static Mesh Corridor(float width, float height, float zNear, float zFar, bool ceiling)
        {
            float hw = width * 0.5f;
            float tile = DescentArt.CorridorTileMeters;
            var vertices = new Vector3[16];
            var uvs = new Vector2[16];

            void Put(int i, float x, float y, float z, float u, float v)
            {
                vertices[i] = new Vector3(x, y, z);
                uvs[i] = new Vector2(u / tile, v / tile);
            }

            // Chão e teto: u atravessa o corredor, v corre ao longo dele.
            Put(0, -hw, 0f, zNear, -hw, -zNear); Put(1, hw, 0f, zNear, hw, -zNear);
            Put(2, hw, 0f, zFar, hw, -zFar); Put(3, -hw, 0f, zFar, -hw, -zFar);
            Put(4, -hw, height, zNear, -hw, -zNear); Put(5, hw, height, zNear, hw, -zNear);
            Put(6, hw, height, zFar, hw, -zFar); Put(7, -hw, height, zFar, -hw, -zFar);
            // Paredes: u corre ao longo do corredor, v sobe.
            Put(8, -hw, 0f, zNear, -zNear, 0f); Put(9, -hw, 0f, zFar, -zFar, 0f);
            Put(10, -hw, height, zFar, -zFar, height); Put(11, -hw, height, zNear, -zNear, height);
            Put(12, hw, 0f, zNear, -zNear, 0f); Put(13, hw, 0f, zFar, -zFar, 0f);
            Put(14, hw, height, zFar, -zFar, height); Put(15, hw, height, zNear, -zNear, height);

            var mesh = new Mesh { name = "DescentCorridor", subMeshCount = 3 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, FloorSubmesh);
            mesh.SetTriangles(new[] { 8, 9, 10, 8, 10, 11, 12, 14, 13, 12, 15, 14 }, WallSubmesh);
            mesh.SetTriangles(ceiling ? new[] { 4, 5, 6, 4, 6, 7 } : Array.Empty<int>(), CeilingSubmesh);
            mesh.RecalculateBounds();
            return mesh;
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
            if (texture == _texture) return;
            _texture = texture;
            _block.SetVector(BaseMapSTId, new Vector4(1f, 1f, 0f, 0f));
            Apply();
        }

        /// <summary>
        /// Um quadro de uma folha em grade de quadros quadrados (da esquerda para a direita, de cima para baixo), sem criar
        /// textura nem material por quadro: só muda o recorte da textura (_BaseMap_ST).
        /// </summary>
        public void SetFrame(Texture sheet, int index, int columns)
        {
            if (sheet == null) return;
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
