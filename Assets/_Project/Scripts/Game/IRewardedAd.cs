using System;

namespace MinersIdle.Game
{
    /// <summary>
    /// O "assistir um anúncio para ganhar" do botão AD (M9b E5; D-21, D-218). Hoje só existe o anúncio simulado (o modal de
    /// 30 s "Visualizando AD", <c>MinersIdle.UI.SimulatedAdScreen</c>); quando os anúncios forem definidos, uma rede de
    /// anúncios entra como outra implementação, sem mexer no boost (<c>MiningSimulation.GrantAdBoost</c>).
    /// </summary>
    public interface IRewardedAd
    {
        /// <summary>O anúncio está na tela agora.</summary>
        bool IsShowing { get; }

        /// <summary>Mostra o anúncio; <paramref name="onRewarded"/> só é chamado se o jogador ganhou a recompensa.</summary>
        void Show(Action onRewarded);
    }
}
