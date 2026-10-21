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

        /// <summary>Unidade do Núcleo = unidade de mundo (16 px).</summary>
        protected static Vector3 ToWorld(Vec2 position) => new Vector3((float)position.X, (float)position.Y, 0f);

        /// <summary>Visão de cima em 3/4: quem está mais embaixo na tela fica na frente.</summary>
        protected static int SortingOrderFor(Vec2 position) => -(int)System.Math.Round(position.Y * 100);
    }
}
