using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// A lasca que sai do cristal quebrado e voa <b>devagar</b> até o LASCAS do HUD (REQ-CRY-04; M5, E3). O saldo já subiu
    /// na hora da quebra (a regra não espera a animação); a chegada só avisa o HUD para pulsar.
    /// </summary>
    public sealed class ShardFlight : MonoBehaviour
    {
        const float Duration = 0.9f;
        const int SortingOrder = 31500;

        [SerializeField] SpriteRenderer shardTemplate;
        [Tooltip("Para onde a lasca voa: o indicador LASCAS do HUD.")]
        [SerializeField] RectTransform target;
        [SerializeField] Camera worldCamera;

        sealed class Shard
        {
            public SpriteRenderer Renderer;
            public Vector3 From;
            public float Age;
        }

        readonly List<Shard> _active = new List<Shard>();
        ObjectPool<Shard> _pool;

        public int ActiveCount => _active.Count;

        /// <summary>Uma lasca chegou ao HUD.</summary>
        public event Action Arrived;

        void Awake()
        {
            shardTemplate.gameObject.SetActive(false);
            _pool = new ObjectPool<Shard>(
                () =>
                {
                    var renderer = Instantiate(shardTemplate, transform);
                    renderer.sortingOrder = SortingOrder;
                    return new Shard { Renderer = renderer };
                },
                shard => shard.Renderer.gameObject.SetActive(true),
                shard => shard.Renderer.gameObject.SetActive(false),
                shard => Destroy(shard.Renderer.gameObject),
                defaultCapacity: 8);
        }

        public void Launch(Vector3 from)
        {
            var shard = _pool.Get();
            shard.From = new Vector3(from.x, from.y, 0f);
            shard.Age = 0f;
            shard.Renderer.transform.position = shard.From;
            _active.Add(shard);
        }

        void Update()
        {
            if (_active.Count == 0) return;

            Vector3 to = TargetWorldPosition();
            float dt = Time.unscaledDeltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var shard = _active[i];
                shard.Age += dt;
                float t = Mathf.Clamp01(shard.Age / Duration);
                // Sai devagar e acelera no fim, com um arco para cima.
                float eased = t * t;
                Vector3 position = Vector3.Lerp(shard.From, to, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.2f);
                shard.Renderer.transform.position = position;

                if (t < 1f) continue;
                _active.RemoveAt(i);
                _pool.Release(shard);
                Arrived?.Invoke();
            }
        }

        Vector3 TargetWorldPosition()
        {
            if (target == null || worldCamera == null) return Vector3.zero;
            var screen = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
            var world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            return new Vector3(world.x, world.y, 0f);
        }
    }
}
