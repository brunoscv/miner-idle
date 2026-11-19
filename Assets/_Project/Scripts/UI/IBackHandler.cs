namespace MinersIdle.UI
{
    /// <summary>
    /// M9i E5 (#17): uma tela que responde ao Voltar do Android (<see cref="BackButtonHandler"/>). Devolve <c>true</c> se o
    /// Voltar foi dela: fechou (LOJA, AJUSTES, Boost…) ou ficou parado porque a tela pede uma escolha (raro, offline,
    /// anúncio, aviso de versão). Fechada, devolve <c>false</c> e o Voltar passa adiante.
    /// </summary>
    public interface IBackHandler
    {
        bool HandleBack();
    }
}
