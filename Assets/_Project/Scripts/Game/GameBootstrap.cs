using MinersIdle.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MinersIdle.Game
{
    /// <summary>
    /// Ponto de entrada do app (cena Bootstrap): configura os quadros, carrega a cena Game e entrega a ela o save (M4).
    /// É aqui que a sessão é montada (CLAUDE.md: composição no GameBootstrap, sem singletons). Abrir a cena Game direto
    /// pula este passo: a partida roda sem save.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public const string BootstrapSceneName = "Bootstrap";
        public const string GameSceneName = "Game";

        /// <summary>A cena da descida em corrida (M6, E1): só visual, aberta pelo DBG do jogo.</summary>
        public const string DescentSceneName = "Descida";

        /// <summary>Meta de quadros: o padrão do Unity no Android é 30 fps (medido no Moto G75, 2026-10-21).</summary>
        public const int TargetFrameRate = 60;

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }

        void Start()
        {
            // Sobrevive à troca de cena só até entregar o save.
            DontDestroyOnLoad(gameObject);
            SceneManager.LoadSceneAsync(GameSceneName).completed += _ => HandOverSave();
        }

        void HandOverSave()
        {
            var runner = FindAnyObjectByType<MiningRunner>(); // uma vez, na troca de cena
            if (runner != null)
                runner.UseSave(new SaveService(new FileSaveStore(Application.persistentDataPath)));
            else
                Debug.LogError("[Bootstrap] A cena Game não tem MiningRunner: o jogo vai rodar sem save.");

            Destroy(gameObject);
        }
    }
}
