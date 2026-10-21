using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Um grupo de cristais em graybox, reaproveitado pelo pool do <see cref="CrystalFieldView"/>. A barra de vida só
    /// aparece no alvo atual (REQ-CRY-03).
    /// </summary>
    public sealed class CrystalView : MonoBehaviour
    {
        /// <summary>Barras ficam acima de qualquer cristal ou do anão.</summary>
        const int HealthBarSortingOrder = 30000;

        [SerializeField] SpriteRenderer body;
        [SerializeField] GameObject healthBar;
        [SerializeField] SpriteRenderer healthBack;
        [SerializeField] SpriteRenderer healthFill;

        float _fillWidth;

        public Crystal Crystal { get; private set; }

        void Awake()
        {
            _fillWidth = healthFill.transform.localScale.x;
            healthBack.sortingOrder = HealthBarSortingOrder;
            healthFill.sortingOrder = HealthBarSortingOrder + 1;
        }

        public void Show(Crystal crystal, int sortingOrder)
        {
            Crystal = crystal;
            transform.position = new Vector3((float)crystal.Position.X, (float)crystal.Position.Y, 0f);
            body.sortingOrder = sortingOrder;
            SetTargeted(false);
            RefreshHealth();
        }

        public void SetTargeted(bool targeted) => healthBar.SetActive(targeted);

        public void RefreshHealth()
        {
            float ratio = Crystal == null ? 0f : Mathf.Clamp01((float)(Crystal.Health / Crystal.MaxHealth));
            var fill = healthFill.transform;
            fill.localScale = new Vector3(_fillWidth * ratio, fill.localScale.y, 1f);
            // Escala a partir do centro: desloca para a barra encolher da direita para a esquerda.
            fill.localPosition = new Vector3(-_fillWidth * (1f - ratio) * 0.5f, fill.localPosition.y, 0f);
        }

        public void Clear() => Crystal = null;
    }
}
