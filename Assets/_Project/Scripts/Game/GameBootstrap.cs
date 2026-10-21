using UnityEngine;
using UnityEngine.SceneManagement;

namespace MinersIdle.Game
{
    /// <summary>
    /// Ponto de entrada do app (cena Bootstrap). No Marco 1 só leva à cena Game; carregar balanceamento, save e
    /// montar a sessão entra nos marcos seguintes (ver Arquitetura Técnica, seção 4).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public const string GameSceneName = "Game";

        /// <summary>Meta de quadros: o padrão do Unity no Android é 30 fps (medido no Moto G75, 2026-10-21).</summary>
        public const int TargetFrameRate = 60;

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }

        void Start()
        {
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
