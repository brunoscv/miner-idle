using System.Collections.Generic;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Som provisório da mineração (M5, E4): um bipe para golpe, toque, crítico, quebra, compra e marco do combo. Só
    /// escuta a simulação. Os clipes são sintetizados uma vez no <c>Awake</c> (<see cref="SfxSynth"/>) e tocam num pool
    /// fixo de <see cref="AudioSource"/>: nada é criado durante o jogo. Desligável no DBG; o volume vem dos AJUSTES (M9, E4).
    /// </summary>
    public sealed class SoundBoard : MiningViewBase
    {
        const int FallbackSampleRate = 48000;

        [Tooltip("Quantos sons podem tocar ao mesmo tempo; com todas ocupadas, um é cortado (rodízio).")]
        [SerializeField, Min(1)] int voices = 8;
        [SerializeField, Range(0f, 1f)] float volume = 0.8f;
        [Tooltip("Variação aleatória de altura no golpe e no toque, para a repetição não cansar o ouvido.")]
        [SerializeField, Range(0f, 0.2f)] float pitchJitter = 0.06f;
        [Tooltip("Intervalo mínimo, em segundos reais, para repetir o mesmo som (evita a parede de som com tempo ×20).")]
        [SerializeField, Min(0f)] float minRepeatSeconds = 0.03f;

        AudioClip[] _clips;
        AudioSource[] _sources;
        float[] _lastPlayed;
        int[] _played;
        int _nextVoice;

        public bool SoundEnabled { get; set; } = true;

        /// <summary>O volume dos efeitos, de 0 a 1: o dos AJUSTES (M9, E4), aplicado pelo <c>SettingsEffects</c>.</summary>
        public float Volume
        {
            get => volume;
            set => volume = Mathf.Clamp01(value);
        }
        public int Voices => _sources.Length;

        /// <summary>Quantas vezes o som tocou nesta cena (para os testes; não depende de haver placa de som).</summary>
        public int PlayedCount(Sfx sfx) => _played[(int)sfx];

        public AudioClip ClipOf(Sfx sfx) => _clips[(int)sfx];

        void Awake()
        {
            int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : FallbackSampleRate;
            _clips = new AudioClip[SfxSynth.Count];
            for (int i = 0; i < _clips.Length; i++)
            {
                var sfx = (Sfx)i;
                var samples = SfxSynth.Render(sfx, rate);
                _clips[i] = AudioClip.Create(sfx.ToString(), samples.Length, 1, rate, false);
                _clips[i].SetData(samples, 0);
            }

            _sources = new AudioSource[voices];
            for (int i = 0; i < _sources.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }

            _lastPlayed = new float[_clips.Length];
            for (int i = 0; i < _lastPlayed.Length; i++) _lastPlayed[i] = float.NegativeInfinity;
            _played = new int[_clips.Length];
        }

        void OnDestroy()
        {
            if (_clips == null) return;
            foreach (var clip in _clips) Destroy(clip);
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.HitLanded += OnHit;
            simulation.CrystalBroken += OnBroken;
            simulation.UpgradePurchased += OnPurchased;
            simulation.ComboMilestone += OnComboMilestone;
            simulation.ComboBonusesGranted += OnBonusesGranted;
            simulation.RareWindowChanged += OnRareWindowChanged;
            simulation.AdBoostGranted += OnAdBoostGranted;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.HitLanded -= OnHit;
            simulation.CrystalBroken -= OnBroken;
            simulation.UpgradePurchased -= OnPurchased;
            simulation.ComboMilestone -= OnComboMilestone;
            simulation.ComboBonusesGranted -= OnBonusesGranted;
            simulation.RareWindowChanged -= OnRareWindowChanged;
            simulation.AdBoostGranted -= OnAdBoostGranted;
        }

        protected override void Rebuild(MiningSimulation simulation) { }

        void OnHit(MiningHit hit)
        {
            Play(hit.IsTap ? Sfx.Tap : Sfx.Hit, 1f + Random.Range(-pitchJitter, pitchJitter));
            if (hit.IsCritical) Play(Sfx.Critical);
        }

        void OnBroken(Crystal crystal) => Play(Sfx.Break);
        void OnPurchased(UpgradeId id, int level) => Play(Sfx.Purchase);
        void OnComboMilestone(int combo) => Play(Sfx.Milestone);
        /// <summary>O ganho do combo (M9b, D-215) usa o som de marco que já existe.</summary>
        void OnBonusesGranted(int combo, IReadOnlyList<ComboBonusRule> granted) => Play(Sfx.Milestone);

        /// <summary>Um raro quebrou (D-225): a janela abre, ou ganha um na fila, com o som de marco.</summary>
        void OnRareWindowChanged(RareWindow window)
        {
            if (window.IsChoosing) Play(Sfx.Milestone);
        }

        /// <summary>O boost do AD entrou (D-231): o som de marco.</summary>
        void OnAdBoostGranted() => Play(Sfx.Milestone);

        public void Play(Sfx sfx, float pitch = 1f)
        {
            if (!SoundEnabled) return;

            int index = (int)sfx;
            float now = Time.unscaledTime;
            if (now - _lastPlayed[index] < minRepeatSeconds) return;
            _lastPlayed[index] = now;
            _played[index]++;

            var source = NextVoice();
            source.clip = _clips[index];
            source.pitch = pitch;
            source.volume = volume;
            source.Play();
        }

        /// <summary>A próxima voz livre, em rodízio; se todas estão tocando, corta a da vez.</summary>
        AudioSource NextVoice()
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                var source = _sources[(_nextVoice + i) % _sources.Length];
                if (source.isPlaying) continue;
                _nextVoice = (_nextVoice + i + 1) % _sources.Length;
                return source;
            }

            var next = _sources[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _sources.Length;
            return next;
        }
    }
}
