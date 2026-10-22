using MinersIdle.Core;
using MinersIdle.Game;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Base das views do mapa: acompanha a partida do <see cref="MiningRunner"/> (inclusive quando ela é reiniciada) e
    /// garante que os eventos da simulação sejam assinados em <c>OnEnable</c> e cancelados em <c>OnDisable</c>.
    /// </summary>
    public abstract class MiningViewBase : MonoBehaviour
    {
        [SerializeField] protected MiningRunner runner;

        protected MiningSimulation Simulation { get; private set; }

        protected virtual void OnEnable()
        {
            if (runner == null)
            {
                Debug.LogError($"[{GetType().Name}] Sem MiningRunner.", this);
                return;
            }

            runner.SimulationStarted += Attach;
            if (runner.Simulation != null) Attach(runner.Simulation);
        }

        protected virtual void OnDisable()
        {
            if (runner != null) runner.SimulationStarted -= Attach;
            Detach();
        }

        void Attach(MiningSimulation simulation)
        {
            Detach();
            Simulation = simulation;
            Subscribe(simulation);
            Rebuild(simulation);
        }

        void Detach()
        {
            if (Simulation == null) return;
            Unsubscribe(Simulation);
            Simulation = null;
        }

        protected virtual void Subscribe(MiningSimulation simulation) { }
        protected virtual void Unsubscribe(MiningSimulation simulation) { }

        /// <summary>Desenha o estado atual da partida (chamado ao conectar e a cada reinício).</summary>
        protected abstract void Rebuild(MiningSimulation simulation);

        /// <summary>Altura de mundo em que o anão fica parado na vista de cima (M6, E2).</summary>
        public const float MinerY = 8f;

        /// <summary>
        /// M6 (E2): enquanto a cena é vista de cima, a fila da descida é desenhada numa linha vertical. O anão fica parado
        /// em <see cref="MinerY"/>; os cristais ficam abaixo dele e sobem conforme ele corre (1 un da trilha = 1 unidade de
        /// mundo). O corredor 3D entra na E3.
        /// </summary>
        public static Vector3 TrackToWorld(double distance, double minerDistance) =>
            new Vector3(0f, MinerY - (float)(distance - minerDistance), 0f);

        /// <summary>Visão de cima em 3/4: quem está mais embaixo na tela fica na frente.</summary>
        protected static int SortingOrderFor(float worldY) => -(int)System.Math.Round(worldY * 100);
    }
}
