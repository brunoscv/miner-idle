using MinersIdle.Core;
using MinersIdle.UI;
using UnityEngine;

namespace MinersIdle.Tests
{
    /// <summary>Ajudas dos testes de toque: o estado do jogo numa linha e a janela do raro fora do caminho.</summary>
    static class TestDiagnostics
    {
        /// <summary>
        /// Um toque pode quebrar um cristal raro (a semente muda a cada run, e às vezes o primeiro da fila é raro): a janela
        /// das cartas (D-223) abriria por cima do mapa e engoliria os toques seguintes. Com isto, a primeira carta é
        /// escolhida na hora e a janela fecha. Para os testes que medem o toque, não o raro.
        /// </summary>
        public static void ChooseRareCardsAtOnce(MiningSimulation simulation)
        {
            simulation.RareWindowChanged += window =>
            {
                if (window.IsChoosing) simulation.ChooseRareCard(0);
                else if (window.IsOpen) simulation.CloseRareWindow();
            };
        }

        public static string Of(MiningSimulation simulation)
        {
            var rare = Object.FindAnyObjectByType<RareWindowView>();
            var target = simulation.Miner.Target;
            return $"(nível {simulation.State.Cave}, transição {simulation.IsInFloorTransition}, parede {simulation.Wall != null}, " +
                   $"alvo {(target == null ? "nenhum" : target == simulation.Wall ? "parede" : target.IsRare ? "raro" : "cristal")}, " +
                   $"janela do raro {(rare != null && rare.IsOpen)}, combo {simulation.Combo}, tempo {simulation.Elapsed:0.00})";
        }
    }
}
