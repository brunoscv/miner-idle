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

        /// <summary>
        /// Folha da beira (M1b E11): grade de 4 × 4 quadros de 64 px (1 m), cada peça com o pé no fundo do quadro. Pedras
        /// nos quadros 0–8, cascalho em 9–12, enfeites (mato, pedra com musgo) em 13–15.
        /// </summary>
        const int EdgeSheetColumns = 4;
        const int EdgeRockFrames = 9;
        const int PebbleFirstFrame = 9;
        const int PebbleFrames = 4;
        const int DecorFirstFrame = 13;
        const int DecorFrames = 3;
        const float EdgeFrameMeters = 1f;

        /// <summary>Cascalho desenhado ao mesmo tempo, nos dois lados.</summary>
        const int PebbleSlots = 200;

        /// <summary>Quantas beiras a prévia tem (A–D).</summary>
        public const int EdgeLooks = 4;

        /// <summary>Picaretada de despedida: só se o anão estava junto do cristal (metros), e quanto dura (segundos).</summary>
        const double FinisherReach = 1.0;
        const double FinisherFrameSeconds = 0.05;
        double FinisherSeconds => (dwarfMineWindUp.Length + dwarfMineStrike.Length) * FinisherFrameSeconds;

        /// <summary>
        /// Folga, em metros, entre o anão na F1 (5 m) e o esteio mais próximo da câmera que ainda é desenhado. Com o recuo
        /// (D-148) o limite fica onde está: os esteios continuam perto da câmera, agora também na frente do anão.
        /// </summary>
        const float SupportClearance = 0.6f;
        /// <summary>Largura do poste no desenho do esteio (12 dos 96 px do <c>mine_wood</c>).</summary>
        const float SupportPostFraction = 12f / 96f;

        /// <summary>Cristais desenhados ao mesmo tempo: o máximo da fila mais os que o debug põe além dele.</summary>
        const int CrystalSlots = 20;

        /// <summary>
        /// A parede da passagem (M8, E2) em graybox: blocos de cristal de lado a lado do corredor, mais altos nas pontas.
        /// No meio ficam baixos, como um cristal da fila, para não esconder o anão que bate nela.
        /// </summary>
        static readonly float[] WallBlockHeights = { 1.6f, 1.15f, 0.9f, 0.8f, 0.9f, 1.15f, 1.6f };
        /// <summary>Quanto cada bloco é mais largo que a sua faixa do corredor (os blocos se encostam).</summary>
        const float WallBlockOverlap = 1.35f;
        static readonly Vector2 WallHealthBarSize = new Vector2(1.2f, 0.08f);
        const float WallHealthBarHeight = 1.15f;

        /// <summary>
        /// A troca de caverna (D-111): a descida escurece na primeira metade, o bioma troca no escuro e clareia na segunda.
        /// </summary>
        public const float CaveFadeSeconds = 0.5f;
        const float CaveFadeDarkness = 0.9f;

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
        static readonly int EdgeShadeId = Shader.PropertyToID("_EdgeShade");
        static readonly int MoundId = Shader.PropertyToID("_Mound");
        static readonly int MoundShapeId = Shader.PropertyToID("_MoundShape");

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

        [Tooltip("Os cristais do Bruno (D-77), em revezamento pelo id: na parede e na fila de bioma sem cristal próprio (D-116). " +
                 "Vazio = o cristal gerado por código.")]
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

        [Header("Golpe fluido (D-125): só ilustra; o golpe sai na hora da regra")]
        [Tooltip("Liga o hit-stop, o preparo do golpe correndo, a freada, a respiração e a arrancada (o DBG → Câmera → Golpe compara).")]
        [SerializeField] bool strikeFeel = true;
        [Tooltip("Segundos de jogo com o quadro do impacto parado, a cada golpe do anão (hit-stop). O resto do golpe vem depois.")]
        [SerializeField, Min(0f)] float hitStopSeconds = 0.06f;
        [Tooltip("O mesmo no golpe que quebra o cristal: a descida também para e depois alcança a regra (a arrancada).")]
        [SerializeField, Min(0f)] float breakStopSeconds = 0.1f;
        [Tooltip("O hit-stop nunca passa desta fração do intervalo do golpe: golpes rápidos, paradas curtas.")]
        [SerializeField, Range(0f, 1f)] float hitStopMaxShare = 0.25f;
        [Tooltip("Depois da parada da quebra, o corredor alcança a regra com esta constante de tempo (segundos; ~3× isto até alcançar).")]
        [SerializeField, Min(0.01f)] float catchUpSeconds = 0.08f;
        [Tooltip("Na arrancada a corrida toca mais rápido: +isto no começo, caindo enquanto o corredor alcança a regra.")]
        [SerializeField, Min(0f)] float launchRunBoost = 0.8f;
        [Tooltip("Freada: ao chegar no cristal o anão achata esta fração da altura e volta ao normal em arrivalSquashSeconds.")]
        [SerializeField, Range(0f, 0.3f)] float arrivalSquash = 0.08f;
        [SerializeField, Min(0.01f)] float arrivalSquashSeconds = 0.15f;
        [Tooltip("Respiração na pose parada entre os golpes: sobe e desce esta altura (unidades do jogo), tantas vezes por segundo.")]
        [SerializeField, Min(0f)] float breathHeight = 0.02f;
        [SerializeField, Min(0.1f)] float breathRate = 1.2f;

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

        [Header("Esteio baixo, prévia (2026-10-31)")]
        [Tooltip("O mesmo esteio mais baixo (linhas do meio dos postes tiradas), com a largura de supportSize. Com 4,5 m a " +
                 "viga fica acima da câmera e nunca aparece.")]
        [SerializeField] SupportOption[] supportOptions;
        [Tooltip("0 = o esteio de hoje (4,5 m, lampião desenhado nele); 1 em diante = supportOptions, com o lampião solto.")]
        [SerializeField, Min(0)] int supportLook;
        [Tooltip("Lampião solto, preso no poste da esquerda (a alça encosta no lado esquerdo da imagem).")]
        [SerializeField] Texture2D lampTexture;
        [Tooltip("Lado do lampião solto, em metros (fixo: não cresce com o esteio).")]
        [SerializeField, Min(0.05f)] float lampSize = 0.45f;
        [Tooltip("Do alto do esteio até o alto do lampião, em metros (a viga tem ~0,42 m).")]
        [SerializeField, Min(0f)] float lampDrop = 0.55f;

        [Tooltip("Pedra da beira do caminho, no encontro do chão com a parede, dos dois lados. Vazio = sem pedras.")]
        [SerializeField] Texture2D edgeRockTexture;
        [Tooltip("Metros entre uma pedra e a próxima, em cada lado.")]
        [SerializeField, Min(0.2f)] float edgeRockSpacing = 0.7f;
        [Tooltip("Largura e altura da pedra média, em metros (cada pedra varia de 70% a 130%).")]
        [SerializeField] Vector2 edgeRockSize = new Vector2(0.5f, 0.44f);
        [Tooltip("Fração da pedra abaixo da base do desenho (linhas transparentes): a pedra desce isso para tocar o chão.")]
        [SerializeField, Range(0f, 0.5f)] float edgeRockFootGap;

        [Header("Beira do caminho, prévia (M1b E11)")]
        [Tooltip("0 = A (como era), 1 = B (sombra no canto), 2 = C (+ pedras variadas e cascalho), 3 = D (+ enfeites). " +
                 "Cada uma soma à anterior.")]
        [SerializeField, Range(0, 3)] int edgeLook;
        [Tooltip("Pedras, cascalho e enfeites da beira (grade de 4 × 4 quadros de 64 px). Vazio = C e D não mudam nada.")]
        [SerializeField] Texture2D edgeSheet;
        [Tooltip("Até onde a sombra do canto entra no chão e sobe na parede, em metros.")]
        [SerializeField, Min(0.1f)] float edgeShadeFloorMeters = 1.3f;
        [SerializeField, Min(0.1f)] float edgeShadeWallMeters = 0.9f;
        [Tooltip("Quanto escurece bem no canto (0 = nada, 1 = preto).")]
        [SerializeField, Range(0f, 1f)] float edgeShadeStrength = 0.75f;

        [Header("Montinho de areia nas beiras (D-146; só nos biomas com Edge Mound)")]
        [Tooltip("A opção ao abrir (o botão \"Monte\" do DBG → Câmera passa por elas): 0 = chão plano, 1 = arco (∩), " +
                 "2 = platô (laterais curvas, topo achatado).")]
        [SerializeField, Range(0, 2)] int moundOption = 1;
        [Tooltip("Altura do montinho, em metros (o arco no meio, o platô no topo).")]
        [SerializeField, Min(0f)] float moundHeight = 0.2f;
        [Tooltip("Platô: fração da largura do montinho que cada lateral ocupa (0,3 = 30% para subir, 30% para descer).")]
        [SerializeField, Range(0.05f, 0.5f)] float plateauSide = 0.3f;
        [Tooltip("Platô: arredondado das quinas (0 = vivas, 1 = bem redondas).")]
        [SerializeField, Range(0f, 1f)] float plateauRoundness = 0.6f;
        [Tooltip("Até onde o montinho entra no chão a partir da parede, em metros (0,5 = um tile, o a e o e).")]
        [SerializeField, Min(0.05f)] float moundWidth = 0.5f;
        [Tooltip("Quanto a inclinação do montinho clareia o lado virado para o meio do corredor (0 = nada).")]
        [SerializeField, Range(0f, 4f)] float moundLight = 1.5f;

        [SerializeField] Color fogColor = new Color32(0x14, 0x0D, 0x09, 0xFF);
        [SerializeField] float fogStart = 3f;
        [SerializeField] float fogEnd = 25f;

        DescentArt _art;
        Mesh _quad, _flat, _box, _corridor;
        Material _floor, _wall, _ceiling, _wood, _sprites, _screenMaterial;
        MeshFilter _corridorFilter;
        int _biome = -1;
        Texture2D _floorStrip;
        BiomeDefinition _floorStripBiome;
        int _floorStripColumns;
        bool _corridorHasCeiling;
        float _corridorWidth;
        float _corridorMound = -1f;
        int _corridorMoundLook = -1;

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
        readonly DescentPiece[] _pebbles = new DescentPiece[PebbleSlots];
        readonly DescentPiece[] _wallBlocks = new DescentPiece[WallBlockHeights.Length];

        RenderTexture _target;
        bool _hitThisTarget;
        bool _lastHitWasTap;
        bool _tapHitWhileStanding;
        double _lastSwingAt = double.NegativeInfinity;
        Crystal _preparedTarget;
        double _finisherStart = double.NegativeInfinity;
        Crystal _shaking;
        float _shake;
        float _shakeAmplitude;
        int _drawnCrystals;
        bool _wallShown;
        int _pendingBiome = -1;
        float _caveFade = -1f;

        // Golpe fluido (D-125), tudo no relógio do jogo. A descida desenhada pode ficar um pouco atrás da regra (_lag, em un)
        // depois da parada da quebra; ela alcança em seguida, e a corrida toca mais rápido enquanto isso (a arrancada).
        double _lastElapsed;
        double _runClock;
        double _holdUntil = double.NegativeInfinity;
        double _holdLength;
        bool _holdingRun;
        double _heldDistance;
        double _lag;
        double _lagPeak;
        double _arrivedAt = double.NegativeInfinity;
        bool _wasRunning;

        public Camera SceneCamera => sceneCamera;

        /// <summary>Um esteio da prévia: a textura (já mais baixa) e a altura dela, em metros.</summary>
        [Serializable]
        public sealed class SupportOption
        {
            public Texture2D texture;
            public float height;
        }

        /// <summary>Quantas opções de esteio o botão do DBG percorre (a de hoje e as da prévia).</summary>
        public int SupportLooks => 1 + (supportOptions != null ? supportOptions.Length : 0);

        /// <summary>O esteio mostrado (DBG; não fica salvo): 0 = o de hoje, 1 em diante = os mais baixos da prévia.</summary>
        public int SupportLook
        {
            get => supportLook;
            set => supportLook = Mathf.Clamp(value, 0, SupportLooks - 1);
        }

        /// <summary>A opção da prévia em uso, ou null no esteio de hoje (ou sem a textura dela).</summary>
        SupportOption CurrentSupportOption =>
            supportLook > 0 && supportOptions != null && supportLook <= supportOptions.Length
            && supportOptions[supportLook - 1].texture != null ? supportOptions[supportLook - 1] : null;

        /// <summary>A altura do esteio mostrado agora, em metros (para testes).</summary>
        public float SupportHeight => CurrentSupportOption is SupportOption option ? option.height : supportSize.y;

        /// <summary>As opções do botão "Monte" do DBG (D-146): chão plano, arco (∩) e platô.</summary>
        public const int MoundLooks = 3;

        /// <summary>A opção do montinho (DBG; não fica salvo): 0 = chão plano, 1 = arco (∩), 2 = platô.</summary>
        public int MoundLook
        {
            get => moundOption;
            set
            {
                moundOption = Mathf.Clamp(value, 0, MoundLooks - 1);
                EnsureCorridor();
            }
        }

        /// <summary>A forma do montinho na opção de agora (no chão plano, a do arco, sem efeito).</summary>
        public MoundForm MoundForm =>
            new MoundForm(moundOption == 2 ? MoundShape.Plateau : MoundShape.Arch, plateauSide, plateauRoundness);

        /// <summary>A altura do montinho nas beiras agora, em metros: 0 no chão plano ou se o bioma não tem montinho.</summary>
        public float MoundHeight => Biome != null && Biome.EdgeMound && moundOption > 0 ? moundHeight : 0f;

        /// <summary>
        /// A beira do caminho da prévia (M1b E11): 0 = A (como era), 1 = B (sombra no canto), 2 = C (+ pedras variadas e
        /// cascalho), 3 = D (+ enfeites).
        /// </summary>
        public int EdgeLook
        {
            get => edgeLook;
            set
            {
                edgeLook = Mathf.Clamp(value, 0, EdgeLooks - 1);
                ApplyEdgeShade();
            }
        }

        /// <summary>A imagem da descida (baixa resolução).</summary>
        public RenderTexture Output => _target;

        public int TargetWidth => _target != null ? _target.width : 0;
        public int TargetHeight => _target != null ? _target.height : 0;

        /// <summary>Proporção da tela (largura/altura), a que o enquadramento usa.</summary>
        public float Aspect => _target != null ? (float)_target.width / _target.height : DescentFraming.ReferenceAspect;

        /// <summary>Cristais da fila que têm desenho (os que estão atrás da câmera contam: só não aparecem ainda).</summary>
        public int DrawnCrystals => _drawnCrystals;

        /// <summary>Se a parede da passagem está desenhada no corredor agora (para testes).</summary>
        public bool IsWallShown => _wallShown;

        /// <summary>Se a troca de caverna (escurecer, trocar o bioma, clarear) está tocando agora.</summary>
        public bool IsChangingCave => _caveFade >= 0f;

        /// <summary>Quanto a descida está escurecida agora (0 = normal).</summary>
        public float Darkness { get; private set; }

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

        /// <summary>
        /// O nome do bioma do nível <paramref name="cave"/> (1 = o primeiro), ou null sem bioma para ele. Um bioma por
        /// caverna até o GDD v5; um a cada 10 níveis no v7 (<see cref="MiningRules.BiomeOf"/>, D-129).
        /// </summary>
        public string BiomeName(int cave)
        {
            int index = BiomeIndexOf(cave);
            return index >= 0 && index < biomes.Length && biomes[index] != null ? biomes[index].DisplayName : null;
        }

        /// <summary>O índice do bioma do nível <paramref name="cave"/> pelas regras da partida (sem partida: um por caverna).</summary>
        public int BiomeIndexOf(int cave) => Simulation != null ? Simulation.Rules.BiomeOf(cave) : cave - 1;

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
            EnsureCorridor(); // o montinho é do bioma
            ApplyFloor();
            _wall.SetColor(BaseColorId, biome.WallTint);
            _ceiling.SetColor(BaseColorId, biome.WallTint);
            ApplyFog(biome.FogColor);
        }

        /// <summary>
        /// O chão do bioma mostrado: a faixa de tiles de parede a parede quando ele tem os 5 (D-145), senão a textura dele (ou
        /// a do campo, ou a gerada por código).
        /// </summary>
        void ApplyFloor()
        {
            var biome = Biome;
            if (biome != null && biome.HasFloorTiles && ApplyFloorStrip(biome)) return;
            var texture = biome != null && biome.FloorTexture != null ? biome.FloorTexture : floorTexture;
            SetSurfaceTexture(_floor, texture, _art.Floor);
        }

        /// <summary>
        /// A faixa na largura do corredor de agora: tantos tiles quantos cabem com <see cref="texturePixelsPerMeter"/> (3 m e
        /// tiles de 32 px = 6), esticada para a beira esquerda e a direita caírem certinho nas paredes. Refeita só quando o
        /// bioma ou a largura muda.
        /// </summary>
        bool ApplyFloorStrip(BiomeDefinition biome)
        {
            var tiles = biome.FloorTiles;
            float width = framing.corridorWidth;
            int columns = Mathf.Max(4, Mathf.RoundToInt(width * texturePixelsPerMeter / tiles[0].width));
            if (_floorStrip == null || _floorStripBiome != biome || _floorStripColumns != columns)
            {
                if (_floorStrip != null) Destroy(_floorStrip);
                _floorStrip = DescentArt.FloorStrip(tiles, columns);
                _floorStripBiome = biome;
                _floorStripColumns = columns;
            }
            if (_floorStrip == null) return false;

            // O u do chão é x / CorridorTileMeters (DescentMeshes.Corridor), de −w/2 a w/2: com escala tile/w e meio de
            // deslocamento a faixa vai de 0 (parede esquerda) a 1 (direita). Ao longo, uma fileira a cada w/colunas metros
            // (tiles quadrados).
            float tile = DescentArt.CorridorTileMeters;
            _floor.SetTexture(BaseMapId, _floorStrip);
            _floor.SetTextureScale(BaseMapId, new Vector2(tile / width, tile * columns / width));
            _floor.SetTextureOffset(BaseMapId, new Vector2(0.5f, 0f));
            return true;
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

        /// <summary>
        /// Golpe fluido (D-125): hit-stop, preparo do golpe correndo, freada, respiração e arrancada. Só ilustra; o DBG
        /// desliga para comparar (não fica salvo).
        /// </summary>
        public bool StrikeFeel
        {
            get => strikeFeel;
            set => strikeFeel = value;
        }

        /// <summary>
        /// O anão aparece golpeando com a regra dizendo que ele corre (D-125): levantando a picareta nos últimos passos ou
        /// parado no impacto da quebra (para testes).
        /// </summary>
        public bool ShowsStrikeWhileRunning { get; private set; }

        /// <summary>Se o anão está levantando a picareta nos últimos passos até o cristal (D-125; para testes).</summary>
        public bool IsPreparing { get; private set; }

        /// <summary>Se a parada da quebra pela picareta segurava a descida no último desenho (D-125; para testes).</summary>
        public bool IsHoldingBreak { get; private set; }

        /// <summary>Unidades que a descida desenhada está atrás da regra agora: só depois da parada da quebra (para testes).</summary>
        public double DrawnLag => _lag;

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
            // O v do chão cresce com z (DescentMeshes.Corridor): a rolagem tem o sinal trocado para andar junto da trilha.
            _floor = CreateSurface("DescentFloor", floorTexture, _art.Floor, new Vector2(0f, -uvPerMeter), Color.white);
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
            for (int i = 0; i < PebbleSlots; i++)
            {
                _pebbles[i] = new DescentPiece(transform, "Pebble", _quad, _sprites, _art.White);
                _pebbles[i].Visible = false;
            }
            for (int i = 0; i < _wallBlocks.Length; i++)
            {
                _wallBlocks[i] = new DescentPiece(transform, "WallBlock", _quad, _sprites, _art.Crystals[0]);
                _wallBlocks[i].Visible = false;
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
            ApplyBiome(0);

            EnsureTarget();
            ApplyFraming();
        }

        /// <summary>Jogando, mexer no montinho pelo Inspector (forma, largura, luz) já refaz o corredor (D-146).</summary>
        void OnValidate()
        {
            if (!Application.isPlaying || _corridorFilter == null || _floor == null) return;
            _corridorMound = -1f;
            EnsureCorridor();
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
            if (_floorStrip != null) Destroy(_floorStrip);

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
            simulation.CaveChanged += OnCaveChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.HitLanded -= OnHit;
            simulation.TargetChanged -= OnTargetChanged;
            simulation.CrystalBroken -= OnCrystalBroken;
            simulation.CaveChanged -= OnCaveChanged;
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            _hitThisTarget = false;
            _lastHitWasTap = false;
            _tapHitWhileStanding = false;
            _lastSwingAt = double.NegativeInfinity;
            _preparedTarget = null;
            _finisherStart = double.NegativeInfinity;
            _shaking = null;
            _shake = 0f;

            _lastElapsed = simulation.Elapsed;
            _runClock = simulation.Elapsed * Mathf.Max(1f, runAnimationFps);
            _holdUntil = double.NegativeInfinity;
            _holdLength = 0;
            _holdingRun = false;
            _lag = _lagPeak = 0;
            _arrivedAt = double.NegativeInfinity;
            _wasRunning = simulation.Miner.State == MinerState.Running;
            ShowsStrikeWhileRunning = false;

            // Ao abrir (ou trocar de partida) o bioma vem na hora, sem a troca escurecida.
            _caveFade = -1f;
            _pendingBiome = -1;
            SetDarkness(0f);
            int biome = simulation.Rules.BiomeOf(simulation.State.Cave);
            if (biome != _biome) ApplyBiome(biome);
        }

        /// <summary>
        /// M8: o nível em que o anão está mostra o bioma dele (D-102; no GDD v7 um bioma a cada 10 níveis, D-129). Desde a
        /// E2 a troca é escurecida (D-111): o bioma novo entra no meio de <see cref="CaveFadeSeconds"/>, no escuro. Um nível
        /// novo no mesmo bioma não escurece.
        /// </summary>
        void OnCaveChanged(int cave)
        {
            int biome = BiomeIndexOf(cave);
            if (biome == (_pendingBiome >= 0 ? _pendingBiome : _biome)) return; // os 100 cristais do debug: mesma caverna
            _pendingBiome = biome;
            if (_caveFade < 0f || _caveFade >= CaveFadeSeconds * 0.5f) _caveFade = 0f; // já escurecendo: segue
        }

        /// <summary>Anda a troca de caverna: escurece, troca o bioma no escuro, clareia.</summary>
        void UpdateCaveFade()
        {
            if (_caveFade < 0f) return;

            _caveFade += Time.unscaledDeltaTime;
            float half = CaveFadeSeconds * 0.5f;
            if (_caveFade >= half && _pendingBiome >= 0)
            {
                ApplyBiome(_pendingBiome);
                _pendingBiome = -1;
            }

            if (_caveFade >= CaveFadeSeconds)
            {
                _caveFade = -1f;
                SetDarkness(0f);
                return;
            }
            float dark = _caveFade < half ? _caveFade / half : 1f - (_caveFade - half) / half;
            SetDarkness(Mathf.Clamp01(dark) * CaveFadeDarkness);
        }

        /// <summary>Escurece só a descida (o quadro do corredor); faíscas, estilhaços e a faixa ficam por cima, claros.</summary>
        void SetDarkness(float darkness)
        {
            if (_screenMaterial == null) return;
            Darkness = darkness;
            _screenMaterial.SetColor(BaseColorId, Color.Lerp(Color.white, Color.black, darkness));
        }

        void OnHit(MiningHit hit)
        {
            if (!hit.IsTap)
            {
                _hitThisTarget = true;
                _lastSwingAt = Simulation.Elapsed;
                StartHold(hitStopSeconds);
            }
            else
            {
                // O estado antes da quebra: a simulação avisa o golpe antes de tirar o cristal e pôr o anão para correr.
                _tapHitWhileStanding = Simulation.Miner.State == MinerState.Mining;
            }
            _lastHitWasTap = hit.IsTap;
        }

        /// <summary>
        /// O toque do jogador quebrou o cristal com o anão parado nele antes da picareta descer (pela regra ele já pode
        /// correr): o anão faz mesmo assim uma picaretada completa e rápida (pedido do Bruno). Se ele já tinha batido nesse
        /// cristal, ou ainda corria até ele, não: segue correndo (D-125; com o golpe na chegada, a despedida repetia o golpe
        /// e o anão deslizava com a picareta). Quebra pela picareta (D-125): o impacto fica parado um pouco mais e a descida
        /// para junto; depois alcança a regra. Só ilustra; nenhuma regra muda.
        /// </summary>
        void OnCrystalBroken(Crystal crystal)
        {
            var simulation = Simulation;
            if (simulation == null) return;
            if (!_lastHitWasTap)
            {
                if (!strikeFeel) return;
                StartHold(breakStopSeconds);
                _holdingRun = true;
                _heldDistance = ShownDistance;
                return;
            }
            if (!_tapHitWhileStanding || _hitThisTarget) return;
            double gap = (crystal.Distance - simulation.Miner.Distance) * metersPerUnit;
            if (gap <= FinisherReach) _finisherStart = simulation.Elapsed;
        }

        /// <summary>Hit-stop (D-125): o quadro do impacto fica parado, no máximo uma fração do intervalo do golpe.</summary>
        void StartHold(float seconds)
        {
            var simulation = Simulation;
            if (!strikeFeel || simulation == null) return;
            _holdLength = Math.Min(seconds, simulation.Stats.HitInterval * hitStopMaxShare);
            _holdUntil = simulation.Elapsed + _holdLength;
        }

        bool IsHolding => strikeFeel && Simulation.Elapsed < _holdUntil;

        /// <summary>Onde o anão aparece na trilha (un): a regra, menos o que a descida desenhada ainda não alcançou.</summary>
        double ShownDistance => Simulation.Miner.Distance - _lag;

        /// <summary>
        /// Anda o golpe fluido um quadro (D-125): a parada da quebra segura a descida; depois dela a descida alcança a
        /// regra, com a corrida mais rápida no começo (a arrancada); marca a chegada ao cristal (a freada).
        /// </summary>
        void UpdateStrikeFeel(MiningSimulation simulation)
        {
            double now = simulation.Elapsed;
            double dt = Math.Max(0, now - _lastElapsed);
            _lastElapsed = now;

            if (IsHolding && _holdingRun)
            {
                _lag = Math.Max(0, simulation.Miner.Distance - _heldDistance);
                _lagPeak = _lag;
            }
            else
            {
                _holdingRun = false;
                if (_lag > 0)
                {
                    _lag *= Math.Exp(-dt / catchUpSeconds);
                    if (_lag < 1e-4) _lag = _lagPeak = 0;
                }
            }

            double boost = strikeFeel && _lagPeak > 0 ? launchRunBoost * _lag / _lagPeak : 0;
            _runClock += dt * Mathf.Max(1f, runAnimationFps) * (1 + boost);

            bool running = simulation.Miner.State == MinerState.Running;
            if (_wasRunning && !running) _arrivedAt = now;
            _wasRunning = running;
        }

        /// <summary>Se a picaretada de despedida está tocando agora (para testes).</summary>
        public bool IsFinishing => Simulation != null && Simulation.Elapsed - _finisherStart < FinisherSeconds;

        void OnTargetChanged(Crystal target)
        {
            _hitThisTarget = false;
            _preparedTarget = null;
        }

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
            UpdateCaveFade();
            Layout();
        }

        // -------------------------------------------------------------------------------------------------------------
        // Posições

        /// <summary>Metros do anão até o cristal (z do cristal = distância da câmera ao anão − isto).</summary>
        float Ahead(double crystalDistance) =>
            (float)((crystalDistance - ShownDistance) * metersPerUnit);

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
            material.SetTextureOffset(BaseMapId, Vector2.zero); // a faixa de tiles (D-145) desloca; as outras não
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
            ApplyFogDistance();

            EnsureCorridor();
            ApplyEdgeShade();
        }

        /// <summary>Quanto a névoa recua (m): o mesmo que o anão está além dos 5 m da câmera F1.</summary>
        public float FogShift => framing.distance - DescentFraming.Validated.distance;

        /// <summary>
        /// A névoa conta da câmera; com o anão mais longe (o recuo do DBG, D-148, ou a Distância), ela recua junto, para o
        /// anão e os cristais em volta dele ficarem com a mesma luz de hoje.
        /// </summary>
        void ApplyFogDistance()
        {
            Shader.SetGlobalFloat(FogStartId, fogStart + FogShift);
            Shader.SetGlobalFloat(FogEndId, fogEnd + FogShift);
        }

        /// <summary>
        /// Refaz a malha do corredor só quando muda o teto (câmera acima dele), a largura ou o montinho das beiras (D-146), e
        /// passa o montinho para a luz do shader do chão.
        /// </summary>
        void EnsureCorridor()
        {
            if (_corridorFilter == null) return;
            // O teto some quando a câmera sobe acima dele.
            bool ceiling = framing.height < WallHeight - 0.1f;
            float mound = MoundHeight;
            var form = MoundForm;
            bool widthChanged = !Mathf.Approximately(_corridorWidth, framing.corridorWidth);
            if (_corridor == null || ceiling != _corridorHasCeiling || widthChanged || !Mathf.Approximately(_corridorMound, mound)
                || _corridorMoundLook != moundOption)
            {
                if (_corridor != null) Destroy(_corridor);
                _corridor = DescentMeshes.Corridor(framing.corridorWidth, WallHeight, CorridorNear, CorridorFar, ceiling,
                                                   moundWidth, mound, form);
                _corridorFilter.sharedMesh = _corridor;
                _corridorHasCeiling = ceiling;
                _corridorWidth = framing.corridorWidth;
                _corridorMound = mound;
                _corridorMoundLook = moundOption;
                if (widthChanged && _biome >= 0) ApplyFloor(); // a faixa de tiles acompanha a largura
            }
            // x = meia largura, y = largura do montinho, z = altura, w = força da luz; z = 0 desliga a conta no shader.
            _floor.SetVector(MoundId, new Vector4(framing.corridorWidth * 0.5f, Mathf.Min(moundWidth, framing.corridorWidth * 0.5f),
                                                  mound, moundLight));
            // x = forma (0 arco, 1 platô), y = lateral do platô, z = arredondado das quinas.
            _floor.SetVector(MoundShapeId, new Vector4(form.shape == MoundShape.Plateau ? 1f : 0f,
                                                       Mathf.Clamp(form.plateauSide, 0.01f, 0.5f), Mathf.Clamp01(form.plateauRoundness), 0f));
        }

        /// <summary>A sombra do canto (B em diante) nos materiais do chão e da parede; desligada, o shader pula a conta.</summary>
        void ApplyEdgeShade()
        {
            if (_floor == null) return;
            float half = framing.corridorWidth * 0.5f;
            bool on = edgeLook >= 1;
            _floor.SetVector(EdgeShadeId, on ? new Vector4(half, edgeShadeFloorMeters, edgeShadeStrength, 0f) : Vector4.zero);
            _wall.SetVector(EdgeShadeId, on ? new Vector4(half, edgeShadeWallMeters, edgeShadeStrength, 1f) : Vector4.zero);
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
            UpdateStrikeFeel(simulation);
            float distance = framing.distance;
            double run = ShownDistance * metersPerUnit;
            bool running = miner.State == MinerState.Running;
            double front = run + distance; // posição na trilha (em metros) que está em z = 0, a câmera
            Shader.SetGlobalFloat(ScrollId, (float)(front % ScrollPeriod));

            bool sheets = dwarfRunSheet != null && dwarfMineSheet != null && dwarfRunCycle.Length > 0;
            int runFrameCount = sheets ? dwarfRunCycle.Length : DescentArt.Frames;
            int runFrame = (int)Math.Floor(_runClock);
            float runPhase = (float)(_runClock / runFrameCount * Math.PI * 2.0);

            bool low = sheets && UseLowDwarf && lowDwarfRunSheet != null && lowDwarfMineSheet != null;
            bool striking = false; // a regra diz que corre, mas o anão aparece golpeando (D-125)
            bool holdingBreak = running && IsHolding && _holdingRun;
            bool resting = false;
            if (sheets)
            {
                int mine;
                if (running) mine = dwarfMineRest;
                else if (miner.State == MinerState.Recovering)
                    mine = FollowThroughFrame(simulation, simulation.Elapsed - _lastSwingAt); // termina o golpe (D-126)
                else mine = MineFrame(simulation);
                bool finishing = IsFinishing && (running || mine == dwarfMineRest);
                if (finishing) mine = FinisherFrame(simulation.Elapsed - _finisherStart);
                else if (holdingBreak)
                {
                    mine = DwarfImpactFrame; // a parada da quebra
                    striking = true;
                }
                else if (running && strikeFeel && PreparingFrame(simulation, out int windUp))
                {
                    mine = windUp; // levanta a picareta nos últimos passos
                    striking = true;
                }
                bool mining = !running || finishing || striking;
                resting = !running && !finishing && mine == dwarfMineRest;
                DwarfFrame = mining ? mine : dwarfRunCycle[runFrame % dwarfRunCycle.Length];
                DwarfSheet = mining ? (low ? lowDwarfMineSheet : dwarfMineSheet) : (low ? lowDwarfRunSheet : dwarfRunSheet);
                _dwarf.SetFrame(DwarfSheet, DwarfFrame, mining ? dwarfMineColumns : dwarfRunColumns);
            }
            else if (!running) _dwarf.SetTexture(_art.DwarfMine[SwingFrame(miner, simulation.Stats.HitInterval)]);
            else _dwarf.SetTexture(_art.DwarfRun[runFrame & 3]);
            ShowsStrikeWhileRunning = striking;
            IsHoldingBreak = holdingBreak;
            IsPreparing = striking && !holdingBreak;

            float bounce = running && !striking ? Mathf.Abs(Mathf.Sin(runPhase)) * Mathf.Max(0f, runBounceHeight) : 0f;
            if (resting && strikeFeel) bounce = Breath(simulation.Elapsed - _arrivedAt);
            var lean = Quaternion.Euler(framing.tilt * spriteLean, 0f, 0f);
            PlaceOnFeet(_dwarf, new Vector3(0f, bounce, distance), lean,
                        Squashed(low ? lowDwarfSize : dwarfSize, simulation.Elapsed - _arrivedAt),
                        low ? lowDwarfFootGap : dwarfFootGap);
            PlaceShadow(_dwarfShadow, 0f, distance, DescentArt.ShadowSize.x);
            LayoutIntern(sheets, running, runFrame, bounce, distance);

            var crystals = simulation.Crystals;
            _drawnCrystals = Mathf.Min(crystals.Count, CrystalSlots);
            bool wallShown = false;
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
                if (crystal.IsWall)
                {
                    _crystals[i].Visible = _crystalShadows[i].Visible = false;
                    LayoutWall(crystal, z, shake, lean);
                    wallShown = true;
                    continue;
                }

                // Uma textura atribuída no Inspector substitui o cristal provisório; mantém-se a arte antiga como fallback.
                _crystals[i].SetTexture(CrystalTexture(crystal.Id));
                _crystals[i].Place(new Vector3(shake, 0f, z), crystalSize);
                _crystals[i].Transform.localRotation = lean;
                PlaceShadow(_crystalShadows[i], 0f, z, DescentArt.ShadowSize.x * 1.1f);
            }

            if (!wallShown && _wallShown)
                for (int k = 0; k < _wallBlocks.Length; k++) _wallBlocks[k].Visible = false;
            _wallShown = wallShown;

            LayoutHealthBar(miner.Target, distance);
            LayoutPosts(front);
            LayoutEdgeRocks(front);
        }

        /// <summary>A parede (graybox): os blocos de cristal de lado a lado do corredor, tremendo juntos no golpe.</summary>
        void LayoutWall(Crystal wall, float z, float shake, Quaternion lean)
        {
            float width = framing.corridorWidth;
            float lane = width / _wallBlocks.Length;
            for (int k = 0; k < _wallBlocks.Length; k++)
            {
                var block = _wallBlocks[k];
                block.SetTexture(MixedCrystalTexture(wall.Id + k));
                float x = -width * 0.5f + lane * (k + 0.5f);
                float front = (k & 1) == 1 ? 0.06f : 0f; // um sim, um não, um pouco à frente: sem briga de profundidade
                block.Place(new Vector3(x + shake, 0f, z - front), new Vector2(lane * WallBlockOverlap, WallBlockHeights[k]));
                block.Transform.localRotation = lean;
            }
        }

        bool HasCrystalArt => crystalTextures != null && crystalTextures.Length > 0;

        /// <summary>
        /// A textura do cristal da fila: o da caverna mostrada (D-116, uma cor por caverna) ou, sem ele no bioma, o
        /// revezamento pelo id (a mesma regra dos estilhaços).
        /// </summary>
        public Texture CrystalTexture(int id)
        {
            var biome = Biome;
            return biome != null && biome.CrystalTexture != null ? biome.CrystalTexture : MixedCrystalTexture(id);
        }

        /// <summary>A cor dos estilhaços do cristal da fila: a do cristal que aparece nela.</summary>
        public Color CrystalShardColor(int id)
        {
            var biome = Biome;
            return biome != null && biome.CrystalTexture != null ? biome.CrystalShardColor : MixedCrystalShardColor(id);
        }

        /// <summary>Os cristais da lista em revezamento pelo id (D-77): hoje só a parede, que mistura as cores.</summary>
        public Texture MixedCrystalTexture(int id)
        {
            if (HasCrystalArt)
            {
                var texture = crystalTextures[id % crystalTextures.Length];
                if (texture != null) return texture;
            }
            return _art.Crystals[id % DescentArt.CrystalColorCount];
        }

        /// <summary>A cor dos estilhaços de <see cref="MixedCrystalTexture"/>.</summary>
        public Color MixedCrystalShardColor(int id)
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
        int MineFrame(MiningSimulation simulation)
        {
            var miner = simulation.Miner;
            double hitInterval = simulation.Stats.HitInterval;
            double untilHit = hitInterval - miner.HitTimer;
            if (WindUpFrame(untilHit, MineFrameSeconds(hitInterval), out int windUp)) return windUp;
            return _hitThisTarget ? FollowThroughFrame(simulation, miner.HitTimer) : dwarfMineRest;
        }

        /// <summary>
        /// O quadro da volta da picareta, <paramref name="sinceHit"/> segundos depois do golpe; a pose parada quando ela
        /// acabou. É o mesmo com o cristal vivo e com o cristal quebrado pelo toque no meio dela (D-126): o anão fica parado
        /// até o fim, pela regra.
        /// </summary>
        int FollowThroughFrame(MiningSimulation simulation, double sinceHit)
        {
            if (dwarfMineStrike.Length == 0) return dwarfMineRest;
            // Com o hit-stop (D-125), o impacto fica parado e o resto do golpe vem depois dele.
            double hold = strikeFeel ? _holdLength : 0;
            double since = sinceHit - hold;
            int strike = since <= 0 ? 0 : (int)(since / FollowThroughFrameSeconds(simulation, hold));
            return strike < dwarfMineStrike.Length ? dwarfMineStrike[strike] : dwarfMineRest;
        }

        /// <summary>
        /// Quanto dura cada quadro da volta da picareta: o de <see cref="MineFrameSeconds"/>, mas cabendo na volta da
        /// regra (<see cref="MiningSimulation.SwingRecovery"/>, D-126) junto com o impacto parado; assim o último quadro
        /// acaba quando o anão volta a correr.
        /// </summary>
        double FollowThroughFrameSeconds(MiningSimulation simulation, double hold)
        {
            double frame = MineFrameSeconds(simulation.Stats.HitInterval);
            double recovery = simulation.SwingRecovery;
            return recovery > hold ? Math.Min(frame, (recovery - hold) / dwarfMineStrike.Length) : frame;
        }

        /// <summary>Quanto dura cada quadro do golpe: no máximo <see cref="dwarfMineFrameSeconds"/>, comprimido para caber no intervalo.</summary>
        double MineFrameSeconds(double hitInterval)
        {
            int total = dwarfMineWindUp.Length + dwarfMineStrike.Length;
            return hitInterval > 0 ? Math.Min(dwarfMineFrameSeconds, hitInterval / Math.Max(1, total)) : dwarfMineFrameSeconds;
        }

        /// <summary>O quadro de levantar a picareta, se faltam menos que os quadros de levantar até o golpe.</summary>
        bool WindUpFrame(double untilHit, double frame, out int windUpFrame)
        {
            int windUp = dwarfMineWindUp.Length;
            windUpFrame = dwarfMineRest;
            if (windUp == 0 || untilHit >= windUp * frame) return false;
            windUpFrame = dwarfMineWindUp[Mathf.Clamp(windUp - 1 - (int)(untilHit / frame), 0, windUp - 1)];
            return true;
        }

        /// <summary>
        /// Preparo (D-125): correndo para um cristal em que vai chegar batendo, o anão levanta a picareta nos últimos passos,
        /// para o impacto cair junto com a chegada. Com o golpe um intervalo depois da chegada (Wait), nunca acontece.
        /// </summary>
        bool PreparingFrame(MiningSimulation simulation, out int windUpFrame)
        {
            windUpFrame = dwarfMineRest;
            var target = simulation.Miner.Target;
            if (target == null || !CanPrepare(simulation, target)) return false;
            if (!WindUpFrame(simulation.SecondsToNextHit, MineFrameSeconds(simulation.Stats.HitInterval), out windUpFrame))
                return false;
            _preparedTarget = target; // começou: vai até a chegada
            return true;
        }

        /// <summary>
        /// Com o jogador tocando (combo ativo), o toque costuma quebrar o cristal antes da chegada, e o levantar começava e
        /// era cortado a cada cristal (achado do Bruno em 2026-11-02). Aí o anão só levanta se o alvo aguenta os toques que
        /// cabem no levantar, no teto de toques por segundo (D-119) e sem crítico; uma vez começado, vai até o fim.
        /// </summary>
        bool CanPrepare(MiningSimulation simulation, Crystal target)
        {
            if (simulation.Combo == 0 || target == _preparedTarget) return true;
            var strike = simulation.Strike;
            if (strike.MaxTapsPerSecond <= 0) return false;
            double windUp = dwarfMineWindUp.Length * MineFrameSeconds(simulation.Stats.HitInterval);
            double taps = Math.Ceiling(windUp * strike.MaxTapsPerSecond);
            return target.Health > taps * simulation.Stats.Damage * strike.TapFraction;
        }

        /// <summary>Respiração na pose parada (D-125): sobe e desce devagar desde a chegada, começando do chão.</summary>
        float Breath(double sinceArrival) =>
            breathHeight * 0.5f * (1f - Mathf.Cos((float)(sinceArrival * breathRate * 2.0 * Math.PI)));

        /// <summary>Freada (D-125): o anão achata ao chegar no cristal e volta ao normal; os pés ficam no chão.</summary>
        Vector2 Squashed(Vector2 size, double sinceArrival)
        {
            if (!strikeFeel || sinceArrival < 0 || sinceArrival >= arrivalSquashSeconds) return size;
            float squash = arrivalSquash * (1f - (float)(sinceArrival / arrivalSquashSeconds));
            return new Vector2(size.x * (1f + squash * 0.5f), size.y * (1f - squash));
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

        /// <summary>
        /// A barra de vida só no alvo (REQ-CRY-03), acima dele; encolhe da direita para a esquerda. Na parede (M8), maior e
        /// acima dos blocos do meio.
        /// </summary>
        void LayoutHealthBar(Crystal target, float distance)
        {
            // Na frente do cristal (e dos blocos da frente da parede).
            float z = target != null ? distance - Ahead(target.Distance) - (target.IsWall ? 0.1f : 0.05f) : float.NegativeInfinity;
            bool show = z >= CorridorNear;
            _healthBack.Visible = _healthFill.Visible = show;
            if (!show) return;

            bool wall = target.IsWall;
            var size = wall ? WallHealthBarSize : HealthBarSize;
            float height = wall ? WallHealthBarHeight : HealthBarHeight;
            float ratio = Mathf.Clamp01((float)(target.Health / target.MaxHealth));
            _healthBack.Place(new Vector3(0f, height, z), size);
            float width = size.x * ratio;
            _healthFill.Place(new Vector3(-(size.x - width) * 0.5f, height, z - 0.01f),
                              new Vector2(Mathf.Max(0.001f, width), size.y));
            _healthBack.Transform.localRotation = _healthFill.Transform.localRotation = _dwarf.Transform.localRotation;
        }

        void PlaceShadow(DescentPiece shadow, float x, float z, float width) =>
            shadow.Place(new Vector3(x, 0.01f, z), new Vector3(width, 1f, DescentArt.ShadowSize.y));

        /// <summary>
        /// Esteios a cada 4 m (caixas de madeira ou o sprite do esteio inteiro), com uma lanterna alternando de lado; com o
        /// esteio com lampião (D-102), o lampião vem nele, a cada N esteios conforme o bioma. No esteio baixo da prévia, o
        /// lampião é solto, preso no poste da esquerda num tamanho fixo.
        /// Nenhum esteio a menos de 5,6 m da câmera (M1b: o mais próximo cobria o anão inteiro). O limite é o da F1, não o do
        /// anão: com o recuo (D-148) o anão vai para o fundo e os esteios entre ele e a câmera continuam lá.
        /// </summary>
        /// <summary>O esteio mais perto da câmera que é desenhado (m): 0,6 m além do anão na F1, ou do anão se ele estiver mais perto.</summary>
        public float NearestSupport => Mathf.Min(framing.distance, DescentFraming.Validated.distance) + SupportClearance;

        void LayoutPosts(double front)
        {
            float halfWidth = framing.corridorWidth * 0.5f;
            bool beams = framing.height < WallHeight + 2f;
            bool sprite = supportTexture != null;
            var option = sprite ? CurrentSupportOption : null;
            var supportArt = option != null ? option.texture : supportTexture;
            var size = option != null ? new Vector2(supportSize.x, option.height) : supportSize;
            bool looseLamp = option != null && lampTexture != null;
            // Com o esteio com lampião (D-102), a luz vem da arte e as lanternas desenhadas por código saem.
            bool lampSprites = sprite && option == null && supportLampTexture != null;
            int lampEvery = Biome != null ? Biome.LampEvery : 0;
            // O lampião solto: a alça na face de dentro do poste da esquerda (12 dos 96 px do esteio), abaixo da viga.
            var lampScale = new Vector3(lampSize, lampSize, 1f);
            float lampX = -size.x * 0.5f + size.x * SupportPostFraction + lampSize * 0.5f;
            float lampY = size.y - lampDrop - lampSize;
            float nearest = NearestSupport;
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
                bool lampHere = lampEvery > 0 && (postIndex % lampEvery + lampEvery) % lampEvery == 0;
                if (sprite && !inFront)
                {
                    _supports[i].SetTexture(lampSprites && lampHere ? supportLampTexture : supportArt);
                    _supports[i].Place(new Vector3(0f, 0f, z), size);
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

                if (looseLamp)
                {
                    if (lampHere && !inFront)
                    {
                        _lanterns[i].SetTexture(lampTexture);
                        _lanterns[i].Place(new Vector3(lampX, lampY, z - 0.03f), lampScale);
                    }
                    else _lanterns[i].Visible = false;
                    continue;
                }
                if (lampSprites)
                {
                    _lanterns[i].Visible = false;
                    continue;
                }
                _lanterns[i].SetTexture(_art.Lantern);
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
            bool rocks = Biome == null || Biome.EdgeRocks; // o bioma pode ficar sem pedras (D-145)
            if (rocks && edgeLook >= 2 && edgeSheet != null)
            {
                LayoutVariedEdge(front);
                return;
            }
            for (int i = 0; i < PebbleSlots; i++) _pebbles[i].Visible = false;
            if (rocks && edgeRockTexture != null)
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

        /// <summary>
        /// Prévia C e D (M1b E11): a beira sem fila. Pedras de várias formas e tamanhos, espelhadas ao acaso e algumas
        /// mais para dentro do chão, metade enterrada na parede; cascalho espalhado, mais denso junto da parede; em D, um
        /// enfeite (mato, pedra com musgo) no lugar de uma pedra de vez em quando. Tudo fixo pela posição na trilha.
        /// </summary>
        void LayoutVariedEdge(double front)
        {
            float halfWidth = framing.corridorWidth * 0.5f;
            const float rockSpacing = 0.55f;
            int used = 0;
            double first = Math.Ceiling((front - CorridorFar) / rockSpacing);
            double last = Math.Floor((front - CorridorNear) / rockSpacing);
            for (double k = first; k <= last && used < EdgeRockSlots - 1; k++)
            for (int side = -1; side <= 1; side += 2)
            {
                float seed = (float)k * 2f + side;
                if (DescentArt.Hash(seed, 21) < 0.2f) continue;
                bool decor = edgeLook >= 3 && DescentArt.Hash(seed, 29) < 0.12f;
                int frame = decor ? DecorFirstFrame + (int)(DescentArt.Hash(seed, 31) * DecorFrames * 0.999f)
                                  : (int)(DescentArt.Hash(seed, 23) * EdgeRockFrames * 0.999f);
                float scale = EdgeFrameMeters * (1.1f + DescentArt.Hash(seed, 33) * 0.5f); // a de hoje (25 px) fica com 0,43–0,62 m
                float flip = DescentArt.Hash(seed, 35) < 0.5f ? -1f : 1f;
                float inset = DescentArt.Hash(seed, 27) * DescentArt.Hash(seed, 37) * 0.15f; // quase todas junto da parede
                float z = (float)(front - k * rockSpacing) + (DescentArt.Hash(seed, 25) - 0.5f) * rockSpacing * 0.6f;
                float x = side * (halfWidth - 0.12f * scale - inset);
                var rock = _edgeRocks[used++];
                rock.SetFrame(edgeSheet, frame, EdgeSheetColumns);
                rock.Place(new Vector3(x, -0.02f, z), new Vector3(scale * flip, scale, 1f));
            }
            for (int i = used; i < EdgeRockSlots; i++) _edgeRocks[i].Visible = false;

            const float pebbleSpacing = 0.3f;
            used = 0;
            first = Math.Ceiling((front - CorridorFar) / pebbleSpacing);
            last = Math.Floor((front - CorridorNear) / pebbleSpacing);
            for (double k = first; k <= last && used < PebbleSlots - 1; k++)
            for (int side = -1; side <= 1; side += 2)
            {
                float seed = (float)k * 2f + side + 0.5f;
                if (DescentArt.Hash(seed, 41) < 0.5f) continue;
                float spread = DescentArt.Hash(seed, 43);
                float inset = 0.08f + spread * spread * 0.5f; // mais cascalho junto da parede, pouco no meio
                float z = (float)(front - k * pebbleSpacing) + (DescentArt.Hash(seed, 45) - 0.5f) * pebbleSpacing;
                float scale = EdgeFrameMeters * (0.8f + DescentArt.Hash(seed, 47) * 0.4f);
                float flip = DescentArt.Hash(seed, 49) < 0.5f ? -1f : 1f;
                var pebble = _pebbles[used++];
                pebble.SetFrame(edgeSheet, PebbleFirstFrame + (int)(DescentArt.Hash(seed, 51) * PebbleFrames * 0.999f),
                                EdgeSheetColumns);
                pebble.Place(new Vector3(side * (halfWidth - inset), -0.01f, z), new Vector3(scale * flip, scale, 1f));
            }
            for (int i = used; i < PebbleSlots; i++) _pebbles[i].Visible = false;
        }
    }
}
