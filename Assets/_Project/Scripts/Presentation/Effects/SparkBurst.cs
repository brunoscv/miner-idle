using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Faíscas em graybox (M5, E3): quadradinhos que saem de um ponto, caem um pouco e somem. Vêm de um pool e são movidos
    /// num único <c>Update</c>, sem criar objeto a cada golpe (Arquitetura, D-T13).
    /// </summary>
    public sealed class SparkBurst : MonoBehaviour
    {
        public const float DefaultLifetime = 0.35f;
        public const float DefaultSize = 0.15f;

        /// <summary>Gravidade por unidade de tamanho: faíscas maiores (vista de frente, M6) caem na mesma proporção.</summary>
        const float GravityPerSize = 60f;
        const int SortingOrder = 30500;

        [SerializeField] SpriteRenderer sparkTemplate;

        sealed class Spark
        {
            public SpriteRenderer Renderer;
            public Vector3 Velocity;
            public float Age;
            public float Lifetime;
            public float Gravity;
            public Color Color;
        }

        readonly List<Spark> _active = new List<Spark>();
        ObjectPool<Spark> _pool;

        public int ActiveCount => _active.Count;

        void Awake()
        {
            sparkTemplate.gameObject.SetActive(false);
            _pool = new ObjectPool<Spark>(
                () => new Spark { Renderer = Instantiate(sparkTemplate, transform) },
                spark => spark.Renderer.gameObject.SetActive(true),
                spark => spark.Renderer.gameObject.SetActive(false),
                spark => Destroy(spark.Renderer.gameObject),
                defaultCapacity: 32);
        }

        /// <param name="size">Lado da faísca, em unidades de mundo.</param>
        public void Emit(Vector3 position, int count, Color color, float speed, float size = DefaultSize, float lifetime = DefaultLifetime)
        {
            for (int i = 0; i < count; i++)
            {
                var spark = _pool.Get();
                Vector2 direction = Random.insideUnitCircle.normalized;
                spark.Velocity = new Vector3(direction.x, Mathf.Abs(direction.y) + 0.3f, 0f) * (speed * Random.Range(0.6f, 1f));
                spark.Age = 0f;
                spark.Lifetime = lifetime;
                spark.Gravity = GravityPerSize * size;
                spark.Color = color;
                var t = spark.Renderer.transform;
                t.position = new Vector3(position.x, position.y, 0f);
                t.localScale = new Vector3(size, size, 1f);
                spark.Renderer.color = color;
                spark.Renderer.sortingOrder = SortingOrder;
                _active.Add(spark);
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var spark = _active[i];
                spark.Age += dt;
                if (spark.Age >= spark.Lifetime)
                {
                    _active.RemoveAt(i);
                    _pool.Release(spark);
                    continue;
                }

                spark.Velocity.y -= spark.Gravity * dt;
                spark.Renderer.transform.position += spark.Velocity * dt;
                var color = spark.Color;
                color.a = 1f - spark.Age / spark.Lifetime;
                spark.Renderer.color = color;
            }
        }
    }
}
