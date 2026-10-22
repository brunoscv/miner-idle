using MinersIdle.Core;
using TMPro;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Um cristal em graybox, reaproveitado pelo pool do <see cref="CrystalFieldView"/>. A barra de vida só aparece no
    /// alvo atual (REQ-CRY-03). O número do cristal só aparece com "Mostrar ordem" do debug.
    /// </summary>
    public sealed class CrystalView : MonoBehaviour
    {
        /// <summary>Barras ficam acima de qualquer cristal ou do anão.</summary>
        const int HealthBarSortingOrder = 30000;

        [SerializeField] SpriteRenderer body;
        [SerializeField] GameObject healthBar;
        [SerializeField] SpriteRenderer healthBack;
        [SerializeField] SpriteRenderer healthFill;
        [SerializeField] TMP_Text batchLabel;

        float _fillWidth;
        float _shake;
        float _shakeAmplitude;

        /// <summary>Duração do tremor do cristal ao levar um golpe (M5, E3).</summary>
        const float ShakeSeconds = 0.12f;

        public Crystal Crystal { get; private set; }

        public bool IsShowingBatch => batchLabel.gameObject.activeSelf;

        void Awake()
        {
            _fillWidth = healthFill.transform.localScale.x;
            healthBack.sortingOrder = HealthBarSortingOrder;
            healthFill.sortingOrder = HealthBarSortingOrder + 1;
        }

        public void Show(Crystal crystal)
        {
            Crystal = crystal;
            batchLabel.SetText("{0}", crystal.Id);
            SetTargeted(false);
            RefreshHealth();
        }

        /// <summary>Põe o cristal no lugar (muda a cada quadro: a fila anda em relação ao anão).</summary>
        public void Place(Vector3 position, int sortingOrder)
        {
            transform.position = position;
            body.sortingOrder = sortingOrder;
        }

        public void SetTargeted(bool targeted) => healthBar.SetActive(targeted);

        /// <summary>O cristal treme rápido ao levar um golpe; só ilustra.</summary>
        public void Shake(float amplitude)
        {
            float current = _shake > 0f ? _shakeAmplitude * _shake : 0f;
            _shakeAmplitude = Mathf.Max(amplitude, current); // golpe fraco não apaga o tremor de um forte
            _shake = 1f;
        }

        void Update()
        {
            if (_shake <= 0f) return;

            _shake = Mathf.Max(0f, _shake - Time.unscaledDeltaTime / ShakeSeconds);
            var offset = _shake > 0f ? Random.insideUnitCircle * (_shakeAmplitude * _shake) : Vector2.zero;
            body.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        }

        public void SetShowBatch(bool show) => batchLabel.gameObject.SetActive(show);

        public void RefreshHealth()
        {
            float ratio = Crystal == null ? 0f : Mathf.Clamp01((float)(Crystal.Health / Crystal.MaxHealth));
            var fill = healthFill.transform;
            fill.localScale = new Vector3(_fillWidth * ratio, fill.localScale.y, 1f);
            // Escala a partir do centro: desloca para a barra encolher da direita para a esquerda.
            fill.localPosition = new Vector3(-_fillWidth * (1f - ratio) * 0.5f, fill.localPosition.y, 0f);
        }

        public void Clear()
        {
            Crystal = null;
            _shake = 0f;
            body.transform.localPosition = Vector3.zero;
        }
    }
}
