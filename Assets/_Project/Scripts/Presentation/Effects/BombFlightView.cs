using System.Collections.Generic;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// O voo da Bomba (M9g, D-308, D-316): ao lançar, uma bomba sai da faixa e faz um arco até cada cristal no alcance,
    /// no tempo do voo da simulação (0,4 s; com o Tempo ×5 do DBG, mais rápido também). Na chegada, as bombas somem e o
    /// estouro é do <see cref="MiningFeedbackView"/>; aqui só sai o som (o "bum" sintetizado, E3, D-316). Uma troca de andar no ar também as apaga. O código
    /// é dono do tempo: a posição vem de quanto falta do voo, não de um relógio próprio.
    /// </summary>
    public sealed class BombFlightView : MiningViewBase
    {
        [SerializeField] DescentStage stage;
        [SerializeField] Camera worldCamera;
        [SerializeField] SoundBoard soundBoard;
        [Tooltip("De onde a bomba sai: a faixa da bomba, na interface.")]
        [SerializeField] RectTransform launchFrom;
        [Tooltip("Uma bomba por cristal; mais cristais que isso dividem as bombas.")]
        [SerializeField] SpriteRenderer[] bombs = new SpriteRenderer[0];
        [Tooltip("A folha das 8 bombas (48 px por quadro); sem ela, o sprite que já estiver nas bombas.")]
        [SerializeField] Texture2D bombSheet;
        [SerializeField] int bombFrames = 8;
        [Tooltip("Pixels da folha por unidade do mundo das bombas voando.")]
        [SerializeField] float pixelsPerUnit = 24f;
        [Tooltip("A altura do arco, em unidades do mundo 2D (a tela tem ~17 de largura).")]
        [SerializeField] float arcHeight = 3f;
        [Tooltip("Voltas da bomba no ar.")]
        [SerializeField] float spins = 0.75f;

        readonly List<Crystal> _targets = new List<Crystal>();
        Vector3 _start;
        Sprite[] _frames;
        int _shownBomb = -1;

        /// <summary>Quantas bombas estão no ar na tela (para os testes).</summary>
        public int Flying { get; private set; }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.BombThrown += OnThrown;
            simulation.BombLanded += OnLanded;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.BombThrown -= OnThrown;
            simulation.BombLanded -= OnLanded;
        }

        protected override void Rebuild(MiningSimulation simulation) => Hide();

        void OnThrown(IReadOnlyList<Crystal> targets)
        {
            _targets.Clear();
            for (int i = 0; i < targets.Count; i++) _targets.Add(targets[i]);
            _start = LaunchPoint();
            UseBomb(Simulation.BombIndex);
            Flying = Mathf.Min(_targets.Count, bombs.Length);
            for (int i = 0; i < bombs.Length; i++)
            {
                bool on = i < Flying;
                bombs[i].gameObject.SetActive(on);
                if (on) bombs[i].transform.position = _start;
            }
        }

        void OnLanded(IReadOnlyList<Crystal> hit)
        {
            if (hit.Count > 0 && soundBoard != null) soundBoard.Play(Sfx.Explosion);
            Hide();
        }

        void Hide()
        {
            Flying = 0;
            for (int i = 0; i < bombs.Length; i++)
                if (bombs[i] != null) bombs[i].gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var simulation = Simulation;
            if (Flying == 0 || simulation == null) return;
            double length = simulation.Bombs.FlightSeconds;
            float t = length <= 0 ? 1f : Mathf.Clamp01(1f - (float)(simulation.BombFlightLeft / length));
            for (int i = 0; i < Flying; i++)
            {
                var end = stage.CrystalOnScreen(_targets[i]);
                var position = Vector3.Lerp(_start, end, t) + Vector3.up * (arcHeight * Mathf.Sin(t * Mathf.PI));
                var bomb = bombs[i].transform;
                bomb.position = position;
                bomb.rotation = Quaternion.Euler(0f, 0f, -360f * spins * t);
            }
        }

        /// <summary>O meio do botão da faixa, no mundo 2D dos efeitos; sem a faixa, o pé da tela.</summary>
        Vector3 LaunchPoint()
        {
            Vector2 screen = new Vector2(Screen.width * 0.5f, Screen.height * 0.12f);
            if (launchFrom != null)
            {
                var canvas = launchFrom.GetComponentInParent<Canvas>();
                var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                screen = RectTransformUtility.WorldToScreenPoint(camera, launchFrom.TransformPoint(launchFrom.rect.center));
            }
            var world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            return new Vector3(world.x, world.y, 0f);
        }

        /// <summary>O desenho da bomba em uso, recortado da folha uma vez por bomba (só quando troca).</summary>
        void UseBomb(int index)
        {
            if (bombSheet == null || index == _shownBomb) return;
            _frames ??= new Sprite[Mathf.Max(1, bombFrames)];
            index = Mathf.Clamp(index, 0, _frames.Length - 1);
            if (_frames[index] == null)
            {
                float frame = (float)bombSheet.width / _frames.Length;
                _frames[index] = Sprite.Create(bombSheet, new Rect(index * frame, 0f, frame, bombSheet.height),
                                               new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }
            _shownBomb = index;
            for (int i = 0; i < bombs.Length; i++) bombs[i].sprite = _frames[index];
        }
    }
}
