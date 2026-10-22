using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Textos que estouram, sobem e somem no mundo ("CRÍTICO!", marcos do combo; M5, E3). Pool de TextMeshPro, movidos
    /// num único <c>Update</c>. O texto chega pronto (vem de dados); aqui só o movimento.
    /// </summary>
    public sealed class FloatingTexts : MonoBehaviour
    {
        const int SortingOrder = 31000;

        [SerializeField] TextMeshPro textTemplate;

        sealed class Item
        {
            public TextMeshPro Text;
            public Vector3 Start;
            public float Age;
            public float Duration;
            public float Rise;
            public float Scale;
            public Color Color;
        }

        readonly List<Item> _active = new List<Item>();
        ObjectPool<Item> _pool;

        public int ActiveCount => _active.Count;

        /// <summary>O último texto mostrado (para testes).</summary>
        public string LastText { get; private set; }

        void Awake()
        {
            textTemplate.gameObject.SetActive(false);
            _pool = new ObjectPool<Item>(
                () =>
                {
                    var text = Instantiate(textTemplate, transform);
                    text.sortingOrder = SortingOrder;
                    return new Item { Text = text };
                },
                item => item.Text.gameObject.SetActive(true),
                item => item.Text.gameObject.SetActive(false),
                item => Destroy(item.Text.gameObject),
                defaultCapacity: 8);
        }

        /// <param name="scale">Tamanho relativo ao modelo (1 = normal).</param>
        public void Show(string text, Vector3 position, Color color, float scale, float duration, float rise)
        {
            var item = _pool.Get();
            item.Text.text = text;
            item.Text.color = color;
            item.Start = new Vector3(position.x, position.y, 0f);
            item.Age = 0f;
            item.Duration = duration;
            item.Rise = rise;
            item.Scale = scale;
            item.Color = color;
            item.Text.transform.position = item.Start;
            item.Text.transform.localScale = Vector3.one * (scale * 1.4f);
            _active.Add(item);
            LastText = text;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var item = _active[i];
                item.Age += dt;
                float t = item.Age / item.Duration;
                if (t >= 1f)
                {
                    _active.RemoveAt(i);
                    _pool.Release(item);
                    continue;
                }

                // Estoura (começa 40% maior e encolhe rápido), sobe e some no fim.
                float pop = 1f + 0.4f * Mathf.Max(0f, 1f - t * 6f);
                item.Text.transform.localScale = Vector3.one * (item.Scale * pop);
                item.Text.transform.position = item.Start + Vector3.up * (item.Rise * t);
                var color = item.Color;
                color.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                item.Text.color = color;
            }
        }
    }
}
