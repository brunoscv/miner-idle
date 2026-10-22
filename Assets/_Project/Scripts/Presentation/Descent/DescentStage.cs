using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// A cena da descida (M6, E1; D-50, D-51): a câmera de frente para o anão, o corredor de caverna em perspectiva
    /// passando por ele e a fila de cristais vindo pela frente, renderizados numa textura de baixa resolução (o "3D com
    /// cara de pixel art" da proposta). O loop é <b>só visual</b> (<see cref="DescentPreview"/>); as regras entram na E2.
    ///
    /// O anão corre parado: a câmera e o anão não se movem. O que anda é a textura do corredor (rolagem no shader) e os
    /// objetos da trilha (cristais, esteios, lanternas), desenhados em relação ao anão: z = distância − (posição − corrido).
    /// Tudo é criado uma vez ao abrir a cena e reaproveitado; um quadro não aloca memória.
    /// </summary>
    public sealed class DescentStage : MonoBehaviour
    {
        /// <summary>Teto do tempo de um quadro: depois de uma pausa do app a cena não pula metros de uma vez.</summary>
        public const float MaxFrameSeconds = 0.1f;

        public const float WallHeight = 3.2f;
        const float CorridorNear = -1f;
        const float CorridorFar = 36f;

        /// <summary>A textura rola em módulo disto (múltiplo da repetição de 2 m), para o uv não perder precisão.</summary>
        const double ScrollPeriod = 8;

        const float PostSpacing = 4f;
        const float PostWidth = 0.22f;
        const float BeamHeight = 0.28f;
        const float LanternHeight = 1.5f;
        const int PostSlots = 11;
        const int DecorSlots = 16;
        const int ParticleSlots = 48;

        /// <summary>Com o estagiário, o anão vai um pouco para a esquerda e ele corre à direita, um pouco atrás.</summary>
        const float DwarfXWithIntern = -0.3f;
        const float InternX = 0.55f;
        const float InternBehind = 0.5f;

        /// <summary>Radianos de passada por metro corrido (o mesmo do simulador).</summary>
        const double StridePerMeter = 5;

        const float JiggleOnHit = 0.08f;

        static readonly int ScrollId = Shader.PropertyToID("_DescentScroll");
        static readonly int FogColorId = Shader.PropertyToID("_DescentFogColor");
        static readonly int FogStartId = Shader.PropertyToID("_DescentFogStart");
        static readonly int FogEndId = Shader.PropertyToID("_DescentFogEnd");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int ScrollAxisId = Shader.PropertyToID("_ScrollAxis");

        static readonly Color SparkColor = new Color(1f, 0.82f, 0.48f);

        [SerializeField] Shader shader;
        [SerializeField] Camera sceneCamera;

        [Tooltip("Câmera validada pelo Bruno (D-50). Ajustável no DBG.")]
        [SerializeField] DescentFraming framing = DescentFraming.Validated;

        [Tooltip("Velocidade de corrida na tela, em m/s (D-58: 4 un/s da regra × 0,5 m).")]
        [SerializeField] float runSpeed = 2f;

        [Tooltip("Largura da textura de render, em pixels (D-14: 270). 0 = resolução do aparelho.")]
        [SerializeField] int internalWidth = 270;

        [SerializeField] bool showIntern = true;

        [SerializeField] Color fogColor = new Color32(0x14, 0x0D, 0x09, 0xFF);
        [SerializeField] float fogStart = 3f;
        [SerializeField] float fogEnd = 25f;

        struct Particle
        {
            public double At;
            public float X, Y, VelocityX, VelocityY, VelocityAt, Life, Size;
        }

        DescentArt _art;
        DescentPreview _preview;
        Mesh _quad, _flat, _box, _corridor;
        Material _floor, _wall, _ceiling, _wood, _sprites;
        MeshFilter _corridorFilter;
        bool _corridorHasCeiling;
        float _corridorWidth;

        DescentPiece _dwarf, _dwarfShadow, _intern, _internShadow;
        readonly DescentPiece[] _crystals = new DescentPiece[DescentPreview.Capacity];
        readonly DescentPiece[] _crystalShadows = new DescentPiece[DescentPreview.Capacity];
        readonly DescentPiece[] _postsLeft = new DescentPiece[PostSlots];
        readonly DescentPiece[] _postsRight = new DescentPiece[PostSlots];
        readonly DescentPiece[] _beams = new DescentPiece[PostSlots];
        readonly DescentPiece[] _lanterns = new DescentPiece[PostSlots];
        readonly DescentPiece[] _decor = new DescentPiece[DecorSlots];
        readonly DescentPiece[] _particlePieces = new DescentPiece[ParticleSlots];
        readonly Particle[] _particles = new Particle[ParticleSlots];
        int _nextParticle;

        RenderTexture _target;
        float _jiggle;

        public DescentPreview Preview => _preview;
        public Camera SceneCamera => sceneCamera;

        /// <summary>A imagem da cena (baixa resolução); a interface a mostra na tela inteira.</summary>
        public RenderTexture Output => _target;

        public int TargetWidth => _target != null ? _target.width : 0;
        public int TargetHeight => _target != null ? _target.height : 0;
        public float Aspect => _target != null ? (float)_target.width / _target.height : DescentFraming.ReferenceAspect;

        /// <summary>Multiplicador do tempo (testes).</summary>
        public float TimeScale { get; set; } = 1f;

        public bool Paused { get; set; }

        public DescentFraming Framing
        {
            get => framing;
            set
            {
                framing = value.Clamped();
                ApplyFraming();
            }
        }

        public float RunSpeed
        {
            get => runSpeed;
            set
            {
                runSpeed = Mathf.Clamp(value, 0.5f, 6f);
                if (_preview != null) _preview.RunSpeed = runSpeed;
            }
        }

        /// <summary>Largura da textura de render em pixels; 0 = a do aparelho.</summary>
        public int InternalWidth
        {
            get => internalWidth;
            set => internalWidth = Mathf.Max(0, value);
        }

        public bool ShowIntern
        {
            get => showIntern;
            set => showIntern = value;
        }

        public void ResetFraming()
        {
            Framing = DescentFraming.Validated;
            RunSpeed = 2f;
        }

        void Awake()
        {
            if (shader == null || sceneCamera == null)
            {
                Debug.LogError("[Descida] Falta o shader ou a câmera no DescentStage (rodar o setup do projeto).", this);
                enabled = false;
                return;
            }

            framing = framing.Clamped();
            _art = DescentArt.Create();
            _preview = new DescentPreview { RunSpeed = runSpeed };

            _quad = DescentMeshes.Quad();
            _flat = DescentMeshes.FlatQuad();
            _box = DescentMeshes.Box();

            float uvPerMeter = 1f / DescentArt.CorridorTileMeters;
            _floor = CreateMaterial("DescentFloor", _art.Floor, new Vector2(0f, uvPerMeter));
            _wall = CreateMaterial("DescentWall", _art.Wall, new Vector2(uvPerMeter, 0f));
            _ceiling = CreateMaterial("DescentCeiling", _art.Ceiling, new Vector2(0f, uvPerMeter));
            _wood = CreateMaterial("DescentWood", _art.Wood, Vector2.zero);
            _sprites = CreateMaterial("DescentSprites", _art.White, Vector2.zero);

            var corridor = new GameObject("Corridor", typeof(MeshFilter), typeof(MeshRenderer));
            corridor.transform.SetParent(transform, false);
            _corridorFilter = corridor.GetComponent<MeshFilter>();
            var corridorRenderer = corridor.GetComponent<MeshRenderer>();
            corridorRenderer.sharedMaterials = new[] { _floor, _wall, _ceiling };
            corridorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            for (int i = 0; i < PostSlots; i++)
            {
                _postsLeft[i] = new DescentPiece(transform, "PostLeft", _box, _wood, _art.Wood);
                _postsRight[i] = new DescentPiece(transform, "PostRight", _box, _wood, _art.Wood);
                _beams[i] = new DescentPiece(transform, "Beam", _box, _wood, _art.Wood);
                _lanterns[i] = new DescentPiece(transform, "Lantern", _quad, _sprites, _art.Lantern);
            }
            for (int i = 0; i < DecorSlots; i++)
                _decor[i] = new DescentPiece(transform, "WallCrystal", _quad, _sprites, _art.Decor[0]);
            for (int i = 0; i < DescentPreview.Capacity; i++)
            {
                _crystalShadows[i] = new DescentPiece(transform, "CrystalShadow", _flat, _sprites, _art.Shadow);
                _crystals[i] = new DescentPiece(transform, "Crystal", _quad, _sprites, _art.Crystals[0]);
            }
            _dwarfShadow = new DescentPiece(transform, "DwarfShadow", _flat, _sprites, _art.Shadow);
            _dwarf = new DescentPiece(transform, "Dwarf", _quad, _sprites, _art.DwarfRun[0]);
            _internShadow = new DescentPiece(transform, "InternShadow", _flat, _sprites, _art.Shadow);
            _intern = new DescentPiece(transform, "Intern", _quad, _sprites, _art.InternRun[0]);
            for (int i = 0; i < ParticleSlots; i++)
            {
                _particlePieces[i] = new DescentPiece(transform, "Particle", _quad, _sprites, _art.White);
                _particlePieces[i].Visible = false;
            }

            // Diferente do Material.SetColor, o global não converte a cor para o espaço linear: sem isto o fundo sai cinza.
            Shader.SetGlobalColor(FogColorId, QualitySettings.activeColorSpace == ColorSpace.Linear ? fogColor.linear : fogColor);
            Shader.SetGlobalFloat(FogStartId, fogStart);
            Shader.SetGlobalFloat(FogEndId, fogEnd);
            sceneCamera.backgroundColor = fogColor;

            EnsureTarget();
            ApplyFraming();
            Layout();
        }

        void OnDestroy()
        {
            if (sceneCamera != null) sceneCamera.targetTexture = null;
            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
            }

            foreach (var material in new[] { _floor, _wall, _ceiling, _wood, _sprites })
                if (material != null) Destroy(material);
            foreach (var mesh in new[] { _quad, _flat, _box, _corridor })
                if (mesh != null) Destroy(mesh);
            _art?.Destroy();
        }

        void Update()
        {
            EnsureTarget();
            float dt = Paused ? 0f : Mathf.Min(Time.unscaledDeltaTime, MaxFrameSeconds) * TimeScale;
            Advance(dt);
        }

        /// <summary>Um passo da cena: o loop visual, as partículas e a posição de tudo. É o que cada quadro faz.</summary>
        public void Advance(float dt)
        {
            var step = _preview.Step(dt);
            float dwarfX = showIntern ? DwarfXWithIntern : 0f;

            if (step.Hit)
            {
                _jiggle = JiggleOnHit;
                Burst(7, _preview.CrystalAt(0), dwarfX, 0.5f, 2f, 1f, 1.5f, 0.4f, 0.05f, SparkColor);
            }

            if (step.Broke)
            {
                _jiggle = 0f;
                Color shard = DescentArt.CrystalColors[step.BrokeColor, 0];
                Burst(16, step.BrokeAt, dwarfX, 0.35f, 3f, 1f, 2.5f, 0.7f, 0.08f, shard);
            }

            _jiggle *= Mathf.Pow(0.02f, dt);
            UpdateParticles(dt);
            Layout();
        }

        Material CreateMaterial(string name, Texture texture, Vector2 scrollAxis)
        {
            var material = new Material(shader) { name = name };
            material.SetTexture(BaseMapId, texture);
            material.SetVector(ScrollAxisId, scrollAxis);
            return material;
        }

        void ApplyFraming()
        {
            if (sceneCamera == null) return;

            sceneCamera.transform.SetPositionAndRotation(new Vector3(0f, framing.height, 0f), Quaternion.Euler(framing.tilt, 0f, 0f));
            sceneCamera.fieldOfView = framing.VerticalFieldOfView(Aspect);

            // O teto some quando a câmera sobe acima dele (o preset "de cima" do simulador).
            bool ceiling = framing.height < WallHeight - 0.1f;
            if (_corridorFilter != null && (_corridor == null || ceiling != _corridorHasCeiling || !Mathf.Approximately(_corridorWidth, framing.corridorWidth)))
            {
                if (_corridor != null) Destroy(_corridor);
                _corridor = DescentMeshes.Corridor(framing.corridorWidth, WallHeight, CorridorNear, CorridorFar, ceiling);
                _corridorFilter.sharedMesh = _corridor;
                _corridorHasCeiling = ceiling;
                _corridorWidth = framing.corridorWidth;
            }
        }

        /// <summary>A textura de render acompanha a proporção da tela; recriada só quando o tamanho muda.</summary>
        void EnsureTarget()
        {
            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);
            int width = internalWidth > 0 ? Mathf.Min(internalWidth, screenWidth) : screenWidth;
            int height = Mathf.Max(1, Mathf.RoundToInt(width * (float)screenHeight / screenWidth));
            if (_target != null && _target.width == width && _target.height == height) return;

            if (_target != null)
            {
                sceneCamera.targetTexture = null;
                _target.Release();
                Destroy(_target);
            }

            _target = new RenderTexture(width, height, 24) { name = "DescentTarget", filterMode = FilterMode.Point };
            _target.Create();
            sceneCamera.targetTexture = _target;
            sceneCamera.fieldOfView = framing.VerticalFieldOfView(Aspect);
        }

        void Layout()
        {
            float distance = framing.distance;
            double progress = _preview.Progress;
            double front = progress + distance; // posição na trilha que está em z = 0 (a câmera)
            Shader.SetGlobalFloat(ScrollId, (float)(front % ScrollPeriod));

            float dwarfX = showIntern ? DwarfXWithIntern : 0f;
            bool mining = _preview.Mining;
            int runFrame = (int)Math.Floor(progress * StridePerMeter / (Math.PI * 0.5)) & 3;
            int swingFrame = _preview.SwingFrame;

            _dwarf.SetTexture(mining ? _art.DwarfMine[swingFrame] : _art.DwarfRun[runFrame]);
            _dwarf.Place(new Vector3(dwarfX, 0f, distance), DescentArt.DwarfSize);
            PlaceShadow(_dwarfShadow, dwarfX, distance, DescentArt.ShadowSize.x);

            _intern.Visible = _internShadow.Visible = showIntern;
            if (showIntern)
            {
                _intern.SetTexture(mining ? _art.InternMine[(swingFrame + 1) & 3] : _art.InternRun[runFrame]);
                _intern.Place(new Vector3(InternX, 0f, distance + InternBehind), DescentArt.DwarfSize);
                PlaceShadow(_internShadow, InternX, distance + InternBehind, DescentArt.ShadowSize.x);
            }

            for (int i = 0; i < DescentPreview.Capacity; i++)
            {
                float z = i < _preview.CrystalCount ? (float)(front - _preview.CrystalAt(i)) : float.NegativeInfinity;
                if (z < CorridorNear)
                {
                    _crystals[i].Visible = _crystalShadows[i].Visible = false;
                    continue;
                }

                float shake = i == 0 && mining ? Mathf.Sin(Time.unscaledTime * 45f) * _jiggle : 0f;
                _crystals[i].SetTexture(_art.Crystals[_preview.CrystalColor(i)]);
                _crystals[i].Place(new Vector3(dwarfX + shake, 0f, z), DescentArt.CrystalSize);
                PlaceShadow(_crystalShadows[i], dwarfX, z, DescentArt.ShadowSize.x * 1.1f);
            }

            LayoutPosts(front);
            LayoutDecor(front);
        }

        void PlaceShadow(DescentPiece shadow, float x, float z, float width) =>
            shadow.Place(new Vector3(x, 0.01f, z), new Vector3(width, 1f, DescentArt.ShadowSize.y));

        /// <summary>Esteios e viga a cada 4 m, com uma lanterna alternando de lado.</summary>
        void LayoutPosts(double front)
        {
            float halfWidth = framing.corridorWidth * 0.5f;
            bool beams = framing.height < WallHeight + 2f;
            double first = Math.Ceiling((front - CorridorFar) / PostSpacing) * PostSpacing;

            for (int i = 0; i < PostSlots; i++)
            {
                double k = first + i * PostSpacing;
                float z = (float)(front - k);
                if (z < CorridorNear)
                {
                    _postsLeft[i].Visible = _postsRight[i].Visible = _beams[i].Visible = _lanterns[i].Visible = false;
                    continue;
                }

                var post = new Vector3(PostWidth, WallHeight, PostWidth);
                _postsLeft[i].Place(new Vector3(-halfWidth + PostWidth * 0.5f, 0f, z), post);
                _postsRight[i].Place(new Vector3(halfWidth - PostWidth * 0.5f, 0f, z), post);
                if (beams) _beams[i].Place(new Vector3(0f, WallHeight - BeamHeight, z), new Vector3(framing.corridorWidth, BeamHeight, PostWidth));
                else _beams[i].Visible = false;

                float side = ((long)Math.Round(k / PostSpacing) & 1) == 0 ? -1f : 1f;
                float lanternX = side * (halfWidth - PostWidth - DescentArt.LanternSize.x * 0.5f);
                _lanterns[i].Place(new Vector3(lanternX, LanternHeight, z - PostWidth * 0.5f - 0.02f), DescentArt.LanternSize);
            }
        }

        /// <summary>Cristais incrustados nas paredes, em ~1 de cada 5 metros (sorteio fixo por metro, como no simulador).</summary>
        void LayoutDecor(double front)
        {
            float halfWidth = framing.corridorWidth * 0.5f;
            int used = 0;
            double from = Math.Ceiling(front - CorridorFar);
            double to = Math.Floor(front - CorridorNear);
            for (double k = from; k <= to && used < DecorSlots; k++)
            {
                if (DescentArt.Hash((float)k, 7) >= 0.2f) continue;

                float side = DescentArt.Hash((float)k, 9) < 0.5f ? -1f : 1f;
                float y = 0.5f + DescentArt.Hash((float)k, 11) * 1.6f;
                int color = (int)(((long)k % DescentArt.CrystalColorCount + DescentArt.CrystalColorCount) % DescentArt.CrystalColorCount);
                var piece = _decor[used++];
                piece.SetTexture(_art.Decor[color]);
                piece.Place(new Vector3(side * (halfWidth - DescentArt.DecorSize.x * 0.5f - 0.01f), y, (float)(front - k - 0.5)), DescentArt.DecorSize);
            }

            for (int i = used; i < DecorSlots; i++) _decor[i].Visible = false;
        }

        void Burst(int count, double at, float x, float y, float spreadX, float minUp, float extraUp, float life, float size, Color color)
        {
            for (int n = 0; n < count; n++)
            {
                int i = _nextParticle;
                _nextParticle = (_nextParticle + 1) % ParticleSlots;
                _particles[i] = new Particle
                {
                    At = at,
                    X = x + (Random.value - 0.5f) * 0.3f,
                    Y = y,
                    VelocityX = (Random.value - 0.5f) * spreadX,
                    VelocityY = minUp + Random.value * extraUp,
                    VelocityAt = (Random.value - 0.5f) * 1.5f,
                    Life = life,
                    Size = size,
                };
                _particlePieces[i].SetColor(color);
            }
        }

        void UpdateParticles(float dt)
        {
            double front = _preview.Progress + framing.distance;
            for (int i = 0; i < ParticleSlots; i++)
            {
                ref var p = ref _particles[i];
                if (p.Life <= 0f)
                {
                    _particlePieces[i].Visible = false;
                    continue;
                }

                p.X += p.VelocityX * dt;
                p.Y += p.VelocityY * dt;
                p.VelocityY -= 6f * dt;
                p.At += p.VelocityAt * dt;
                p.Life -= dt;
                if (p.Y < 0f)
                {
                    p.Y = 0f;
                    p.VelocityY *= -0.3f;
                }

                _particlePieces[i].Place(new Vector3(p.X, p.Y, (float)(front - p.At)), new Vector2(p.Size, p.Size));
            }
        }
    }
}
