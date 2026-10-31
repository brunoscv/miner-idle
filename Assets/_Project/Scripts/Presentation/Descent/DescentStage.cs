using System;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// A descida em corrida na cena do jogo (M6; D-50, D-52, D-53): a câmera de frente para o anão, o corredor de caverna
    /// em perspectiva passando por ele e a fila de cristais da simulação vindo pela frente. Desde a E3, quem dirige é a
    /// <see cref="MiningSimulation"/>: esta view só lê a distância do anão e dos cristais e desenha (o código é dono do
    /// tempo e da regra; a animação só ilustra).
    ///
    /// O 3D fica longe da cena 2D (em y = −1000, para a câmera de cima não o ver) e é renderizado numa textura de baixa
    /// resolução (270 px, D-14), mostrada num quadro atrás de tudo pela câmera da tela, a mesma dos efeitos e do tremor.
    /// O anão corre parado: o que anda é a textura do corredor (rolagem no shader) e os objetos da trilha, desenhados em
    /// relação a ele, na escala da D-58. Tudo é criado uma vez e reaproveitado; um quadro não aloca memória.
    /// </summary>
    public sealed class DescentStage : MiningViewBase
    {
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

        /// <summary>Pedras da beira do caminho desenhadas ao mesmo tempo (os dois lados, ao longo de todo o corredor).</summary>
        const int EdgeRockSlots = 120;

        /// <summary>Picaretada de despedida: só se o anão estava junto do cristal (metros), e quanto dura (segundos).</summary>
        const double FinisherReach = 1.0;
        const double FinisherFrameSeconds = 0.05;
        double FinisherSeconds => (dwarfMineWindUp.Length + dwarfMineStrike.Length) * FinisherFrameSeconds;

        /// <summary>Folga, em metros, entre o anão e o esteio mais próximo da câmera que ainda é desenhado.</summary>
        const float SupportClearance = 0.6f;

        /// <summary>Cristais desenhados ao mesmo tempo: o máximo da fila mais os que o debug põe além dele.</summary>
        const int CrystalSlots = 20;

        /// <summary>Tremor do cristal ao levar golpe (o feedback chama <see cref="Shake"/>).</summary>
        const float ShakeSeconds = 0.12f;

        /// <summary>A barra de vida do alvo (REQ-CRY-03), em metros, acima do cristal.</summary>
        static readonly Vector2 HealthBarSize = new Vector2(0.46f, 0.05f); // mais discreta (pedido do Bruno)
        const float HealthBarHeight = 0.9f;
        static readonly Color HealthBackColor = new Color32(0x22, 0x26, 0x2A, 0xFF);
        static readonly Color HealthFillColor = new Color32(0x5F, 0xC4, 0x6E, 0xFF); // verde mais suave

        /// <summary>
        /// O quadro da descida sai um pouco maior que a tela, para o tremor de tela não mostrar a borda; o campo de visão
        /// cresce na mesma proporção, então o enquadramento visível continua o da D-50.
        /// </summary>
        const float Overscan = 1.06f;

        static readonly int ScrollId = Shader.PropertyToID("_DescentScroll");
        static readonly int FogColorId = Shader.PropertyToID("_DescentFogColor");
        static readonly int FogStartId = Shader.PropertyToID("_DescentFogStart");
        static readonly int FogEndId = Shader.PropertyToID("_DescentFogEnd");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int ScrollAxisId = Shader.PropertyToID("_ScrollAxis");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Shader shader;
        [Tooltip("Shader do quadro que mostra a descida na tela (URP Unlit).")]
        [SerializeField] Shader screenShader;
        [SerializeField] Camera sceneCamera;
        [Tooltip("A câmera da tela (a de cima, ortográfica): o quadro da descida fica atrás de tudo que ela desenha.")]
        [SerializeField] Camera screenCamera;

        [Tooltip("Câmera validada pelo Bruno (F1, D-69; antes D-50). Ajustável no DBG.")]
        [SerializeField] DescentFraming framing = DescentFraming.Validated;

        [Tooltip("Metros do corredor por unidade da regra (D-58).")]
        [SerializeField] float metersPerUnit = 0.5f;

        [Tooltip("Largura da textura de render, em pixels (D-14: 270). 0 = resolução do aparelho.")]
        [SerializeField] int internalWidth = 270;

        [Tooltip("Os cristais da fila, em revezamento pelo id (D-77: os do Bruno). Vazio = o cristal gerado por código.")]
        [SerializeField] Texture2D[] crystalTextures;
        [Tooltip("A cor dos estilhaços de cada cristal da lista acima (mesma ordem).")]
        [SerializeField] Color[] crystalShardColors;

        [Tooltip("Largura e altura dos cristais da fila, em unidades do jogo.")]
        [SerializeField] Vector2 crystalSize = new Vector2(0.84f, 0.8f);

        [Header("Anão (M1b E6, D-75: folhas do PixelLab; vazio = desenho gerado por código)")]
        [Tooltip("Folha da corrida: quadros quadrados em grade, da esquerda para a direita e de cima para baixo.")]
        [SerializeField] Texture2D dwarfRunSheet;
        [SerializeField, Min(1)] int dwarfRunColumns = 3;
        [Tooltip("Quadros da corrida, em ordem. Na folha do PixelLab, o 0 e o 1 são a pose parada. A passada tem dois " +
                 "pulos (4 e 8); cada perna fica 3 quadros (2-3-4 e 6-7-8). O 5 sobra e deixava uma perna mais longa.")]
        [SerializeField] int[] dwarfRunCycle = { 2, 3, 4, 6, 7, 8 };

        [Tooltip("Folha da mineração, no mesmo formato.")]
        [SerializeField] Texture2D dwarfMineSheet;
        [SerializeField, Min(1)] int dwarfMineColumns = 4;
        [Tooltip("Quadros que antecedem o golpe (levantar a picareta), tocados logo antes do instante do dano.")]
        [SerializeField] int[] dwarfMineWindUp = { 1, 2, 3 };
        [Tooltip("Quadros a partir do impacto: o primeiro aparece no instante do dano; depois volta à pose parada.")]
        [SerializeField] int[] dwarfMineStrike = { 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        [Tooltip("Pose parada (esperando o próximo golpe).")]
        [SerializeField] int dwarfMineRest;
        [Tooltip("Tempo máximo de cada quadro do golpe, em segundos (com golpes rápidos, a animação se comprime).")]
        [SerializeField, Min(0.01f)] float dwarfMineFrameSeconds = 0.08f;

        [Tooltip("Quanto o anão, o estagiário, os cristais e a barra de vida se inclinam para ficar de frente para a " +
                 "câmera (1 = de frente), girando em volta dos pés. Sem isso, a câmera de cima achata as figuras.")]
        [SerializeField, Range(0f, 1f)] float spriteLean = 1f;

        [Tooltip("Largura e altura do anão, em unidades do jogo (quadros quadrados: largura = altura).")]
        [SerializeField] Vector2 dwarfSize = new Vector2(2.4f, 2f);
        [Tooltip("Fração do quadro abaixo dos pés (o quadro desce isso para o pé tocar o chão).")]
        [SerializeField, Range(0f, 0.5f)] float dwarfFootGap;

        [Header("Anão com menos pixels (só pelo DBG, para comparar; a mesma grade e os mesmos quadros das folhas acima)")]
        [SerializeField] Texture2D lowDwarfRunSheet;
        [SerializeField] Texture2D lowDwarfMineSheet;
        [Tooltip("Largura e altura do quadro: o desenho enche mais o quadro de 64 px que o de 256, então o quadro é menor.")]
        [SerializeField] Vector2 lowDwarfSize = new Vector2(1.94f, 1.94f);
        [SerializeField, Range(0f, 0.5f)] float lowDwarfFootGap;

        [Header("Estagiário (M1b E7, D-76: o anão grisalho; só pelo DBG até o M7)")]
        [SerializeField] Texture2D internRunSheet;
        [SerializeField] Texture2D internMineSheet;
        [Tooltip("Lugar do estagiário em relação ao anão, em metros: x para o lado, y para trás (D-57).")]
        [SerializeField] Vector2 internOffset = new Vector2(0.85f, 0.6f);
        [Tooltip("Quadros de defasagem da corrida do estagiário (para não correrem em espelho).")]
        [SerializeField] int internRunLag = 3;

        [Tooltip("Quadros de corrida exibidos por segundo. A animação segue o relógio do jogo.")]
        [SerializeField, Min(1f)] float runAnimationFps = 8f;

        [Tooltip("Altura extra do balanço do anão durante a corrida, em unidades do jogo. Use 0 para desativar.")]
        [SerializeField, Min(0f)] float runBounceHeight = 0.04f;

        [Header("Arte do corredor (M1b E4; vazio = desenho gerado por código)")]
        [Tooltip("Chão sem bioma (a lista de biomas vazia).")]
        [SerializeField] Texture2D floorTexture;
        [Tooltip("As 10 cavernas (M1b E8, D-102): a primeira abre o jogo; o DBG → Câmera → Bioma passa por elas (não fica " +
                 "salvo; a troca jogando é do M8).")]
        [SerializeField] BiomeDefinition[] biomes = Array.Empty<BiomeDefinition>();
        [SerializeField] Texture2D wallTexture;
        [SerializeField] Texture2D ceilingTexture;
        [Tooltip("Pixels de textura por metro de corredor (64 = perto da densidade do anão).")]
        [SerializeField, Min(1f)] float texturePixelsPerMeter = 64f;
        [Tooltip("Tinta da parede e do teto (acima de 1 clareia).")]
        [SerializeField] Color wallTint = Color.white;
        [Tooltip("Esteio inteiro (postes e viga) como sprite, no lugar das caixas de madeira. Vazio = caixas.")]
        [SerializeField] Texture2D supportTexture;
        [Tooltip("O mesmo esteio com lampião, a cada N esteios conforme o bioma (D-102). Com ele, as lanternas desenhadas " +
                 "por código saem.")]
        [SerializeField] Texture2D supportLampTexture;
        [Tooltip("Largura e altura do esteio em sprite, em metros.")]
        [SerializeField] Vector2 supportSize = new Vector2(3.4f, 4.5f);
        [Tooltip("Pedra da beira do caminho, no encontro do chão com a parede, dos dois lados. Vazio = sem pedras.")]
        [SerializeField] Texture2D edgeRockTexture;
        [Tooltip("Metros entre uma pedra e a próxima, em cada lado.")]
        [SerializeField, Min(0.2f)] float edgeRockSpacing = 0.7f;
        [Tooltip("Largura e altura da pedra média, em metros (cada pedra varia de 70% a 130%).")]
        [SerializeField] Vector2 edgeRockSize = new Vector2(0.5f, 0.44f);
        [Tooltip("Fração da pedra abaixo da base do desenho (linhas transparentes): a pedra desce isso para tocar o chão.")]
        [SerializeField, Range(0f, 0.5f)] float edgeRockFootGap;

        [SerializeField] Color fogColor = new Color32(0x14, 0x0D, 0x09, 0xFF);
        [SerializeField] float fogStart = 3f;
        [SerializeField] float fogEnd = 25f;

        DescentArt _art;
        Mesh _quad, _flat, _box, _corridor;
        Material _floor, _wall, _ceiling, _wood, _sprites, _screenMaterial;
        MeshFilter _corridorFilter;
        int _biome = -1;
        bool _corridorHasCeiling;
        float _corridorWidth;

        Transform _screen;
        int _screenWidth, _screenHeight;
        float _screenOrthoSize;

        DescentPiece _dwarf, _dwarfShadow, _healthBack, _healthFill, _intern, _internShadow;
        readonly DescentPiece[] _crystals = new DescentPiece[CrystalSlots];
        readonly DescentPiece[] _crystalShadows = new DescentPiece[CrystalSlots];
        readonly DescentPiece[] _postsLeft = new DescentPiece[PostSlots];
        readonly DescentPiece[] _postsRight = new DescentPiece[PostSlots];
        readonly DescentPiece[] _beams = new DescentPiece[PostSlots];
        readonly DescentPiece[] _lanterns = new DescentPiece[PostSlots];
        readonly DescentPiece[] _supports = new DescentPiece[PostSlots];
        readonly DescentPiece[] _edgeRocks = new DescentPiece[EdgeRockSlots];

        RenderTexture _target;
        bool _hitThisTarget;
        bool _lastHitWasTap;
        double _finisherStart = double.NegativeInfinity;
        Crystal _shaking;
        float _shake;
        float _shakeAmplitude;
        int _drawnCrystals;

        public Camera SceneCamera => sceneCamera;

        /// <summary>A imagem da descida (baixa resolução).</summary>
        public RenderTexture Output => _target;

        public int TargetWidth => _target != null ? _target.width : 0;
        public int TargetHeight => _target != null ? _target.height : 0;

        /// <summary>Proporção da tela (largura/altura), a que o enquadramento usa.</summary>
        public float Aspect => _target != null ? (float)_target.width / _target.height : DescentFraming.ReferenceAspect;

        /// <summary>Cristais da fila que têm desenho (os que estão atrás da câmera contam: só não aparecem ainda).</summary>
        public int DrawnCrystals => _drawnCrystals;

        /// <summary>O quadro que mostra a descida na tela (para testes).</summary>
        public Transform ScreenQuad => _screen;

        public float MetersPerUnit => metersPerUnit;

        /// <summary>Mostra o estagiário ao lado do anão (DBG; o estagiário de verdade é do M7).</summary>
        public bool ShowIntern { get; set; }

        /// <summary>Troca o anão pelas folhas com menos pixels (DBG, para comparar; não fica salvo).</summary>
        public bool UseLowDwarf { get; set; }

        /// <summary>A textura do chão agora (null = a gerada por código; para testes).</summary>
        public Texture FloorTexture => _floor != null ? _floor.GetTexture(BaseMapId) : null;

        /// <summary>O bioma mostrado agora (null sem a lista de biomas).</summary>
        public BiomeDefinition Biome => _biome >= 0 ? biomes[_biome] : null;

        /// <summary>Passa para o próximo bioma (DBG; volta à caverna 1 depois da 10; não fica salvo).</summary>
        public void NextBiome()
        {
            if (biomes.Length > 0) ApplyBiome((_biome + 1) % biomes.Length);
        }

        /// <summary>Chão, tinta da rocha, neblina e lampiões da caverna <paramref name="index"/> (0 = caverna 1).</summary>
        public void ApplyBiome(int index)
        {
            if (_floor == null || index < 0 || index >= biomes.Length || biomes[index] == null) return;
            _biome = index;
            var biome = biomes[index];
            SetSurfaceTexture(_floor, biome.FloorTexture != null ? biome.FloorTexture : floorTexture, _art.Floor);
            _wall.SetColor(BaseColorId, biome.WallTint);
            _ceiling.SetColor(BaseColorId, biome.WallTint);
            ApplyFog(biome.FogColor);
        }

        void ApplyFog(Color color)
        {
            Shader.SetGlobalColor(FogColorId, QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color);
            sceneCamera.backgroundColor = color;
        }

        /// <summary>A folha do anão mostrada agora (null sem as folhas do PixelLab; para testes).</summary>
        public Texture DwarfSheet { get; private set; }

        /// <summary>O quadro da folha do estagiário mostrado agora (−1 escondido; para testes).</summary>
        public int InternFrame { get; private set; } = -1;

        /// <summary>O quadro da folha do anão mostrado agora (−1 sem as folhas do PixelLab; para testes).</summary>
        public int DwarfFrame { get; private set; } = -1;

        /// <summary>O primeiro quadro do golpe: aparece no instante do dano (para testes).</summary>
        public int DwarfImpactFrame => dwarfMineStrike.Length > 0 ? dwarfMineStrike[0] : -1;

        public DescentFraming Framing
        {
            get => framing;
            set
            {
                framing = value.Clamped();
                ApplyFraming();
            }
        }

        /// <summary>Largura da textura de render em pixels; 0 = a do aparelho.</summary>
        public int InternalWidth
        {
            get => internalWidth;
            set => internalWidth = Mathf.Max(0, value);
        }

        public void ResetFraming() => Framing = DescentFraming.Validated;

        void Awake()
        {
            if (shader == null || screenShader == null || sceneCamera == null || screenCamera == null)
            {
                Debug.LogError("[Descida] Faltam shaders ou câmeras no DescentStage (rodar o setup do projeto).", this);
                enabled = false;
                return;
            }

            framing = framing.Clamped();
            _art = DescentArt.Create();
            _quad = DescentMeshes.Quad();
            _flat = DescentMeshes.FlatQuad();
            _box = DescentMeshes.Box();

            float uvPerMeter = 1f / DescentArt.CorridorTileMeters;
            _floor = CreateSurface("DescentFloor", floorTexture, _art.Floor, new Vector2(0f, uvPerMeter), Color.white);
            _wall = CreateSurface("DescentWall", wallTexture, _art.Wall, new Vector2(uvPerMeter, 0f), wallTint);
            _ceiling = CreateSurface("DescentCeiling", ceilingTexture, _art.Ceiling, new Vector2(0f, uvPerMeter), wallTint);
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
                _supports[i] = new DescentPiece(transform, "Support", _quad, _sprites,
                                                supportTexture != null ? supportTexture : (Texture)_art.White);
                _supports[i].Visible = false;
            }
            for (int i = 0; i < EdgeRockSlots; i++)
            {
                _edgeRocks[i] = new DescentPiece(transform, "EdgeRock", _quad, _sprites,
                                                 edgeRockTexture != null ? edgeRockTexture : (Texture)_art.White);
                _edgeRocks[i].Visible = false;
            }
            for (int i = 0; i < CrystalSlots; i++)
            {
                _crystalShadows[i] = new DescentPiece(transform, "CrystalShadow", _flat, _sprites, _art.Shadow);
                _crystals[i] = new DescentPiece(transform, "Crystal", _quad, _sprites, _art.Crystals[0]);
                _crystals[i].Visible = _crystalShadows[i].Visible = false;
            }
            _dwarfShadow = new DescentPiece(transform, "DwarfShadow", _flat, _sprites, _art.Shadow);
            _dwarf = new DescentPiece(transform, "Dwarf", _quad, _sprites, _art.DwarfRun[0]);
            _internShadow = new DescentPiece(transform, "InternShadow", _flat, _sprites, _art.Shadow);
            _intern = new DescentPiece(transform, "Intern", _quad, _sprites, _art.InternRun[0]);
            _intern.Visible = _internShadow.Visible = false;
            _healthBack = new DescentPiece(transform, "HealthBack", _quad, _sprites, _art.White);
            _healthFill = new DescentPiece(transform, "HealthFill", _quad, _sprites, _art.White);
            _healthBack.SetColor(HealthBackColor);
            _healthFill.SetColor(HealthFillColor);

            _screenMaterial = new Material(screenShader) { name = "DescentScreen" };
            var screen = new GameObject("DescentScreen", typeof(MeshFilter), typeof(MeshRenderer));
            _screen = screen.transform;
            screen.GetComponent<MeshFilter>().sharedMesh = _quad;
            var screenRenderer = screen.GetComponent<MeshRenderer>();
            screenRenderer.sharedMaterial = _screenMaterial;
            screenRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            screenRenderer.sortingOrder = short.MinValue; // atrás de todos os sprites

            ApplyFog(fogColor);
            Shader.SetGlobalFloat(FogStartId, fogStart);
            Shader.SetGlobalFloat(FogEndId, fogEnd);
            ApplyBiome(0);

            EnsureTarget();
            ApplyFraming();
        }

        void OnDestroy()
        {
            if (sceneCamera != null) sceneCamera.targetTexture = null;
            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
            }
            if (_screen != null) Destroy(_screen.gameObject);

            foreach (var material in new[] { _floor, _wall, _ceiling, _wood, _sprites, _screenMaterial })
                if (material != null) Destroy(material);
            foreach (var mesh in new[] { _quad, _flat, _box, _corridor })
                if (mesh != null) Destroy(mesh);
            _art?.Destroy();
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.HitLanded += OnHit;
            simulation.TargetChanged += OnTargetChanged;
            simulation.CrystalBroken += OnCrystalBroken;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.HitLanded -= OnHit;
            simulation.TargetChanged -= OnTargetChanged;
            simulation.CrystalBroken -= OnCrystalBroken;
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            _hitThisTarget = false;
            _lastHitWasTap = false;
            _finisherStart = double.NegativeInfinity;
            _shaking = null;
            _shake = 0f;
        }

        void OnHit(MiningHit hit)
        {
            if (!hit.IsTap) _hitThisTarget = true;
            _lastHitWasTap = hit.IsTap;
        }

        /// <summary>
        /// O toque do jogador quebrou o cristal antes da picareta do anão descer (pela regra ele já pode correr): o anão
        /// faz mesmo assim uma picaretada completa e rápida (pedido do Bruno). Só ilustra; nenhuma regra muda.
        /// </summary>
        void OnCrystalBroken(Crystal crystal)
        {
            var simulation = Simulation;
            if (!_lastHitWasTap || simulation == null) return;
            double gap = (crystal.Distance - simulation.Miner.Distance) * metersPerUnit;
            if (gap <= FinisherReach) _finisherStart = simulation.Elapsed;
        }

        /// <summary>Se a picaretada de despedida está tocando agora (para testes).</summary>
        public bool IsFinishing => Simulation != null && Simulation.Elapsed - _finisherStart < FinisherSeconds;

        void OnTargetChanged(Crystal target) => _hitThisTarget = false;

        /// <summary>O cristal treme rápido ao levar um golpe (feedback do M5); só ilustra.</summary>
        public void Shake(Crystal crystal, float amplitude)
        {
            float current = _shaking == crystal && _shake > 0f ? _shakeAmplitude * _shake : 0f;
            _shaking = crystal;
            _shakeAmplitude = Mathf.Max(amplitude, current); // golpe fraco não apaga o tremor de um forte
            _shake = 1f;
        }

        void LateUpdate()
        {
            EnsureTarget();
            FitScreen();
            Draw();
        }

        /// <summary>Põe tudo no lugar a partir da simulação. É o que cada quadro faz (os testes medem que não aloca).</summary>
        public void Draw()
        {
            if (Simulation == null) return;

            if (_shake > 0f) _shake = Mathf.Max(0f, _shake - Time.unscaledDeltaTime / ShakeSeconds);
            Layout();
        }

        // -------------------------------------------------------------------------------------------------------------
        // Posições

        /// <summary>Metros do anão até o cristal (z do cristal = distância da câmera ao anão − isto).</summary>
        float Ahead(double crystalDistance) =>
            (float)((crystalDistance - Simulation.Miner.Distance) * metersPerUnit);

        /// <summary>Onde um cristal está no corredor (coordenadas locais do palco).</summary>
        public Vector3 CrystalLocalPosition(Crystal crystal) =>
            new Vector3(0f, 0f, framing.distance - (Simulation != null ? Ahead(crystal.Distance) : 0f));

        /// <summary>
        /// Um ponto do corredor na tela do jogo (no mundo 2D da câmera de cima, z = 0), para os efeitos 2D (faíscas,
        /// textos, a lasca voando) saírem do lugar certo.
        /// </summary>
        public Vector3 ToScreenWorld(Vector3 localPosition)
        {
            var viewport = sceneCamera.WorldToViewportPoint(transform.TransformPoint(localPosition));
            var size = _screen.localScale;
            var bottom = _screen.position; // o quadro tem o pivô no meio da base
            return new Vector3(bottom.x + (viewport.x - 0.5f) * size.x, bottom.y + viewport.y * size.y, 0f);
        }

        /// <summary>O meio de um cristal na tela do jogo (efeitos de golpe e de quebra).</summary>
        public Vector3 CrystalOnScreen(Crystal crystal) => ToScreenWorld(CrystalLocalPosition(crystal) + Vector3.up * 0.35f);

        /// <summary>O alto do capacete do anão na tela do jogo (combo e crítico aparecem em volta dele).</summary>
        public Vector3 MinerOnScreen => ToScreenWorld(new Vector3(0f, DescentFraming.DwarfHeight, framing.distance));

        // -------------------------------------------------------------------------------------------------------------
        // Câmera, textura e quadro na tela

        /// <summary>
        /// Chão, parede ou teto: a textura do campo (com a escala de <see cref="texturePixelsPerMeter"/>) ou, se vazio, a
        /// gerada por código.
        /// </summary>
        Material CreateSurface(string name, Texture2D texture, Texture2D fallback, Vector2 scrollAxis, Color tint)
        {
            var material = CreateMaterial(name, fallback, scrollAxis);
            SetSurfaceTexture(material, texture, fallback);
            material.SetColor(BaseColorId, tint);
            return material;
        }

        void SetSurfaceTexture(Material material, Texture2D texture, Texture2D fallback)
        {
            material.SetTexture(BaseMapId, texture != null ? texture : fallback);
            // O material repete a cada CorridorTileMeters; a textura cobre width / pixelsPorMetro metros.
            float scale = texture != null ? DescentArt.CorridorTileMeters * texturePixelsPerMeter / texture.width : 1f;
            material.SetTextureScale(BaseMapId, new Vector2(scale, scale));
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

            sceneCamera.transform.localPosition = new Vector3(0f, framing.height, 0f);
            sceneCamera.transform.localRotation = Quaternion.Euler(framing.tilt, 0f, 0f);
            sceneCamera.fieldOfView = RenderFieldOfView();

            // O teto some quando a câmera sobe acima dele.
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

        /// <summary>O campo de visão da D-50 para esta tela, aumentado pela margem do tremor (<see cref="Overscan"/>).</summary>
        float RenderFieldOfView()
        {
            float halfTan = Mathf.Tan(framing.VerticalFieldOfView(Aspect) * 0.5f * Mathf.Deg2Rad) * Overscan;
            return 2f * Mathf.Atan(halfTan) * Mathf.Rad2Deg;
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
            sceneCamera.fieldOfView = RenderFieldOfView();
            _screenMaterial.SetTexture(BaseMapId, _target);
        }

        /// <summary>O quadro cobre a tela da câmera de cima (com a margem), atrás de todos os sprites.</summary>
        void FitScreen()
        {
            if (Screen.width == _screenWidth && Screen.height == _screenHeight && Mathf.Approximately(screenCamera.orthographicSize, _screenOrthoSize))
                return;

            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            _screenOrthoSize = screenCamera.orthographicSize;

            float height = screenCamera.orthographicSize * 2f * Overscan;
            float width = height * Screen.width / Mathf.Max(1, Screen.height);
            var cameraPosition = screenCamera.transform.position;
            // O quadro tem o pivô no meio da base: desce meia altura para ficar centrado.
            _screen.position = new Vector3(cameraPosition.x, cameraPosition.y - height * 0.5f, screenCamera.farClipPlane * 0.5f);
            _screen.localScale = new Vector3(width, height, 1f);
        }

        // -------------------------------------------------------------------------------------------------------------
        // Desenho

        void Layout()
        {
            var simulation = Simulation;
            var miner = simulation.Miner;
            float distance = framing.distance;
            double run = miner.Distance * metersPerUnit;
            bool running = miner.State == MinerState.Running;
            double front = run + distance; // posição na trilha (em metros) que está em z = 0, a câmera
            Shader.SetGlobalFloat(ScrollId, (float)(front % ScrollPeriod));

            bool sheets = dwarfRunSheet != null && dwarfMineSheet != null && dwarfRunCycle.Length > 0;
            int runFrameCount = sheets ? dwarfRunCycle.Length : DescentArt.Frames;
            int runFrame = (int)Math.Floor(simulation.Elapsed * Mathf.Max(1f, runAnimationFps));
            float runPhase = (float)(simulation.Elapsed * Mathf.Max(1f, runAnimationFps) / runFrameCount * Math.PI * 2.0);

            bool low = sheets && UseLowDwarf && lowDwarfRunSheet != null && lowDwarfMineSheet != null;
            if (sheets)
            {
                int mine = running ? dwarfMineRest : MineFrame(miner, simulation.Stats.HitInterval);
                bool finishing = IsFinishing && (running || mine == dwarfMineRest);
                bool mining = !running || finishing;
                if (finishing) mine = FinisherFrame(simulation.Elapsed - _finisherStart);
                DwarfFrame = mining ? mine : dwarfRunCycle[runFrame % dwarfRunCycle.Length];
                DwarfSheet = mining ? (low ? lowDwarfMineSheet : dwarfMineSheet) : (low ? lowDwarfRunSheet : dwarfRunSheet);
                _dwarf.SetFrame(DwarfSheet, DwarfFrame, mining ? dwarfMineColumns : dwarfRunColumns);
            }
            else if (!running) _dwarf.SetTexture(_art.DwarfMine[SwingFrame(miner, simulation.Stats.HitInterval)]);
            else _dwarf.SetTexture(_art.DwarfRun[runFrame & 3]);

            float bounce = running ? Mathf.Abs(Mathf.Sin(runPhase)) * Mathf.Max(0f, runBounceHeight) : 0f;
            var lean = Quaternion.Euler(framing.tilt * spriteLean, 0f, 0f);
            PlaceOnFeet(_dwarf, new Vector3(0f, bounce, distance), lean,
                        low ? lowDwarfSize : dwarfSize, low ? lowDwarfFootGap : dwarfFootGap);
            PlaceShadow(_dwarfShadow, 0f, distance, DescentArt.ShadowSize.x);
            LayoutIntern(sheets, running, runFrame, bounce, distance);

            var crystals = simulation.Crystals;
            _drawnCrystals = Mathf.Min(crystals.Count, CrystalSlots);
            for (int i = 0; i < CrystalSlots; i++)
            {
                float z = i < crystals.Count ? distance - Ahead(crystals[i].Distance) : float.NegativeInfinity;
                if (z < CorridorNear)
                {
                    _crystals[i].Visible = _crystalShadows[i].Visible = false;
                    continue;
                }

                var crystal = crystals[i];
                float shake = crystal == _shaking && _shake > 0f ? Mathf.Sin(Time.unscaledTime * 90f) * _shakeAmplitude * _shake : 0f;
                // Uma textura atribuída no Inspector substitui o cristal provisório; mantém-se a arte antiga como fallback.
                _crystals[i].SetTexture(CrystalTexture(crystal.Id));
                _crystals[i].Place(new Vector3(shake, 0f, z), crystalSize);
                _crystals[i].Transform.localRotation = lean;
                PlaceShadow(_crystalShadows[i], 0f, z, DescentArt.ShadowSize.x * 1.1f);
            }

            LayoutHealthBar(miner.Target, distance);
            LayoutPosts(front);
            LayoutEdgeRocks(front);
        }

        bool HasCrystalArt => crystalTextures != null && crystalTextures.Length > 0;

        /// <summary>A textura do cristal pelo id: os cristais da lista em revezamento (a mesma regra dos estilhaços).</summary>
        public Texture CrystalTexture(int id)
        {
            if (HasCrystalArt)
            {
                var texture = crystalTextures[id % crystalTextures.Length];
                if (texture != null) return texture;
            }
            return _art.Crystals[id % DescentArt.CrystalColorCount];
        }

        /// <summary>A cor dos estilhaços do cristal pelo id: a do cristal que aparece na fila.</summary>
        public Color CrystalShardColor(int id)
        {
            if (HasCrystalArt && crystalShardColors != null && crystalShardColors.Length == crystalTextures.Length)
                return crystalShardColors[id % crystalShardColors.Length];
            return DescentArt.CrystalColors[id % DescentArt.CrystalColorCount, 0];
        }

        /// <summary>
        /// O estagiário (D-76): as folhas grisalhas, ao lado e um pouco atrás do anão (D-57), no mesmo relógio. A corrida
        /// vem defasada; o golpe acompanha o do anão (o golpe próprio dele é do M7).
        /// </summary>
        void LayoutIntern(bool sheets, bool running, int runFrame, float bounce, float distance)
        {
            bool show = ShowIntern && sheets && internRunSheet != null && internMineSheet != null;
            _intern.Visible = _internShadow.Visible = show;
            if (!show)
            {
                InternFrame = -1;
                return;
            }

            bool mining = !running || IsFinishing;
            InternFrame = mining ? DwarfFrame : dwarfRunCycle[(runFrame + internRunLag) % dwarfRunCycle.Length];
            _intern.SetFrame(mining ? internMineSheet : internRunSheet, InternFrame, mining ? dwarfMineColumns : dwarfRunColumns);
            float z = distance + internOffset.y;
            PlaceOnFeet(_intern, new Vector3(internOffset.x, bounce, z), _dwarf.Transform.localRotation, dwarfSize, dwarfFootGap);
            PlaceShadow(_internShadow, internOffset.x, z, DescentArt.ShadowSize.x);
        }

        /// <summary>
        /// O anão (ou o estagiário) inclinado para a câmera girando em volta dos pés: os pés ficam em
        /// <paramref name="feet"/>, em cima da sombra. Girar em volta da base do quadro (abaixo dos pés, pela folga da
        /// folha) jogava os pés para trás da sombra, e o anão parecia flutuar.
        /// </summary>
        void PlaceOnFeet(DescentPiece piece, Vector3 feet, Quaternion lean, Vector2 size, float footGap)
        {
            piece.Place(feet - lean * new Vector3(0f, footGap * size.y, 0f), size);
            piece.Transform.localRotation = lean;
        }

        /// <summary>A picaretada de despedida: levantar e bater em sequência, rápido.</summary>
        int FinisherFrame(double elapsed)
        {
            int index = (int)(elapsed / FinisherFrameSeconds);
            if (index < dwarfMineWindUp.Length) return dwarfMineWindUp[index];
            index -= dwarfMineWindUp.Length;
            return index < dwarfMineStrike.Length ? dwarfMineStrike[index] : dwarfMineRest;
        }

        /// <summary>
        /// Quadro da folha de mineração pelo relógio do golpe (D-75): os quadros de levantar tocam logo antes do golpe, o
        /// de impacto aparece no instante do dano e os seguintes depois dele; no resto do intervalo, a pose parada. Cada
        /// quadro dura no máximo <see cref="dwarfMineFrameSeconds"/>; com golpes rápidos, a animação se comprime para caber
        /// no intervalo. Antes do primeiro golpe no cristal, só o levantar (esperando o golpe).
        /// </summary>
        int MineFrame(Miner miner, double hitInterval)
        {
            int total = dwarfMineWindUp.Length + dwarfMineStrike.Length;
            double frame = hitInterval > 0 ? Math.Min(dwarfMineFrameSeconds, hitInterval / Math.Max(1, total)) : dwarfMineFrameSeconds;
            double untilHit = hitInterval - miner.HitTimer;
            int windUp = dwarfMineWindUp.Length;
            if (windUp > 0 && untilHit < windUp * frame)
                return dwarfMineWindUp[Mathf.Clamp(windUp - 1 - (int)(untilHit / frame), 0, windUp - 1)];
            if (_hitThisTarget)
            {
                int strike = (int)(miner.HitTimer / frame);
                if (strike < dwarfMineStrike.Length) return dwarfMineStrike[strike];
            }
            return dwarfMineRest;
        }

        /// <summary>
        /// Quadro do golpe pelo relógio do golpe da simulação: a picareta desce no instante do golpe (quadro 2), volta
        /// (3), fica erguida (0) e começa a descer pouco antes do próximo (1). Antes do primeiro golpe no cristal, fica
        /// erguida esperando.
        /// </summary>
        int SwingFrame(Miner miner, double hitInterval)
        {
            float phase = hitInterval > 0 ? (float)(miner.HitTimer / hitInterval) : 0f;
            if (phase >= 0.87f) return 1;
            if (!_hitThisTarget) return 0;
            if (phase < 0.15f) return 2;
            if (phase < 0.35f) return 3;
            return 0;
        }

        /// <summary>A barra de vida só no alvo (REQ-CRY-03), acima dele; encolhe da direita para a esquerda.</summary>
        void LayoutHealthBar(Crystal target, float distance)
        {
            float z = target != null ? distance - Ahead(target.Distance) - 0.05f : float.NegativeInfinity; // na frente do cristal
            bool show = z >= CorridorNear;
            _healthBack.Visible = _healthFill.Visible = show;
            if (!show) return;

            float ratio = Mathf.Clamp01((float)(target.Health / target.MaxHealth));
            _healthBack.Place(new Vector3(0f, HealthBarHeight, z), HealthBarSize);
            float width = HealthBarSize.x * ratio;
            _healthFill.Place(new Vector3(-(HealthBarSize.x - width) * 0.5f, HealthBarHeight, z - 0.01f),
                              new Vector2(Mathf.Max(0.001f, width), HealthBarSize.y));
            _healthBack.Transform.localRotation = _healthFill.Transform.localRotation = _dwarf.Transform.localRotation;
        }

        void PlaceShadow(DescentPiece shadow, float x, float z, float width) =>
            shadow.Place(new Vector3(x, 0.01f, z), new Vector3(width, 1f, DescentArt.ShadowSize.y));

        /// <summary>
        /// Esteios a cada 4 m (caixas de madeira ou o sprite do esteio inteiro), com uma lanterna alternando de lado; com o
        /// esteio com lampião (D-102), o lampião vem nele, a cada N esteios conforme o bioma.
        /// Nenhum esteio entre a câmera e o anão (M1b: o mais próximo cobria o anão inteiro).
        /// </summary>
        void LayoutPosts(double front)
        {
            float halfWidth = framing.corridorWidth * 0.5f;
            bool beams = framing.height < WallHeight + 2f;
            bool sprite = supportTexture != null;
            // Com o esteio com lampião (D-102), a luz vem da arte e as lanternas desenhadas por código saem.
            bool lampSprites = sprite && supportLampTexture != null;
            int lampEvery = Biome != null ? Biome.LampEvery : 0;
            float nearest = framing.distance + SupportClearance;
            double first = Math.Ceiling((front - CorridorFar) / PostSpacing) * PostSpacing;

            for (int i = 0; i < PostSlots; i++)
            {
                double k = first + i * PostSpacing;
                float z = (float)(front - k);
                if (z < CorridorNear)
                {
                    _postsLeft[i].Visible = _postsRight[i].Visible = _beams[i].Visible = _lanterns[i].Visible = false;
                    _supports[i].Visible = false;
                    continue;
                }

                bool inFront = z < nearest;
                if (sprite || inFront)
                    _postsLeft[i].Visible = _postsRight[i].Visible = _beams[i].Visible = false;
                long postIndex = (long)Math.Round(k / PostSpacing);
                if (sprite && !inFront)
                {
                    bool lamp = lampSprites && lampEvery > 0 && (postIndex % lampEvery + lampEvery) % lampEvery == 0;
                    _supports[i].SetTexture(lamp ? supportLampTexture : supportTexture);
                    _supports[i].Place(new Vector3(0f, 0f, z), supportSize);
                }
                else _supports[i].Visible = false;

                if (!sprite && !inFront)
                {
                    var post = new Vector3(PostWidth, WallHeight, PostWidth);
                    _postsLeft[i].Place(new Vector3(-halfWidth + PostWidth * 0.5f, 0f, z), post);
                    _postsRight[i].Place(new Vector3(halfWidth - PostWidth * 0.5f, 0f, z), post);
                    if (beams) _beams[i].Place(new Vector3(0f, WallHeight - BeamHeight, z), new Vector3(framing.corridorWidth, BeamHeight, PostWidth));
                    else _beams[i].Visible = false;
                }

                if (lampSprites)
                {
                    _lanterns[i].Visible = false;
                    continue;
                }
                float side = (postIndex & 1) == 0 ? -1f : 1f;
                float lanternX = side * (halfWidth - PostWidth - DescentArt.LanternSize.x * 0.5f);
                _lanterns[i].Place(new Vector3(lanternX, LanternHeight, z - PostWidth * 0.5f - 0.02f), DescentArt.LanternSize);
            }
        }

        /// <summary>
        /// Pedras na beira do caminho, no encontro do chão com a parede, dos dois lados e por todo o corredor: disfarçam a
        /// emenda entre chão, parede e esteios e desenham o caminho em perspectiva. Tamanho e posição sorteados de forma
        /// fixa por posição na trilha (a mesma pedra está sempre no mesmo lugar); algumas faltam, para não ficar em fila.
        /// </summary>
        void LayoutEdgeRocks(double front)
        {
            int used = 0;
            if (edgeRockTexture != null)
            {
                float halfWidth = framing.corridorWidth * 0.5f;
                double first = Math.Ceiling((front - CorridorFar) / edgeRockSpacing);
                double last = Math.Floor((front - CorridorNear) / edgeRockSpacing);
                for (double k = first; k <= last && used < EdgeRockSlots - 1; k++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float seed = (float)k * 2f + side;
                    if (DescentArt.Hash(seed, 21) < 0.18f) continue; // uma falha aqui e ali
                    float scale = 0.8f + DescentArt.Hash(seed, 23) * 0.4f;
                    var size = edgeRockSize * scale;
                    // Pouca variação de lugar: com muita, umas pedras pareciam mais fundas ou mais baixas que as outras.
                    float jitter = (DescentArt.Hash(seed, 25) - 0.5f) * edgeRockSpacing * 0.3f;
                    float z = (float)(front - k * edgeRockSpacing) + jitter;
                    float inset = DescentArt.Hash(seed, 27) * 0.05f;
                    float x = side * (halfWidth - size.x * 0.4f - inset);
                    _edgeRocks[used++].Place(new Vector3(x, -edgeRockFootGap * size.y, z), size);
                }
            }
            for (int i = used; i < EdgeRockSlots; i++) _edgeRocks[i].Visible = false;
        }
    }
}
