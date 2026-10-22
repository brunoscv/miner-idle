using System.Collections;
using MinersIdle.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MinersIdle.Game
{
    /// <summary>
    /// Ponto de entrada do app (cena Bootstrap): configura os quadros, carrega a cena Game e entrega a ela o save (M4).
    /// É aqui que a sessão é montada (CLAUDE.md: composição no GameBootstrap, sem singletons). Abrir a cena Game direto
    /// pula este passo: a partida roda sem save.
    ///
    /// Splash (2026-10-22): enquanto a cena Game carrega, a arte de abertura fica na tela por pelo menos
    /// <see cref="splashSeconds"/> e some num esmaecer antes do jogo aparecer.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public const string GameSceneName = "Game";

        /// <summary>Meta de quadros: o padrão do Unity no Android é 30 fps (medido no Moto G75, 2026-10-21).</summary>
        public const int TargetFrameRate = 60;

        [Tooltip("A arte de abertura (opcional: sem ela, o jogo abre direto).")]
        [SerializeField] CanvasGroup splash;
        [Tooltip("Tempo mínimo da arte de abertura na tela, em segundos.")]
        [SerializeField] float splashSeconds = 2f;
        [SerializeField] float splashFadeSeconds = 0.3f;

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }

        void Start()
        {
            // Sobrevive à troca de cena só até entregar o save.
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadGame());
        }

        IEnumerator LoadGame()
        {
            var load = SceneManager.LoadSceneAsync(GameSceneName);
            load.completed += _ => HandOverSave();
            if (splash == null) yield break;

            // A cena carrega por trás da arte; só entra quando a arte já ficou o tempo mínimo e esmaeceu.
            load.allowSceneActivation = false;
            float shownAt = Time.unscaledTime;
            while (load.progress < 0.9f || Time.unscaledTime - shownAt < splashSeconds) yield return null;

            for (float t = 0f; t < splashFadeSeconds; t += Time.unscaledDeltaTime)
            {
                splash.alpha = 1f - t / splashFadeSeconds;
                yield return null;
            }
            splash.alpha = 0f;
            load.allowSceneActivation = true;
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
