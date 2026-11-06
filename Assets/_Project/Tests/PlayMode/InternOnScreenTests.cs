using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>
    /// E2 do M7: o estagiário na tela da cena Game (GDD v7). Contratar pelo DBG mostra o boneco; descansar sem a Promoção
    /// o esconde; com a Promoção ele já está lá na run nova. A picaretada dele segue os golpes dele no núcleo (o código é
    /// dono do tempo, a animação só ilustra), e o dano sai no cristal num número menor e cinza (D-171).
    /// </summary>
    public class InternOnScreenTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        DebugPanel _panel;
        MiningRunner _runner;
        DescentStage _stage;
        MeshRenderer _internRenderer;

        MiningSimulation Simulation => _runner.Simulation;

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _panel = Object.FindAnyObjectByType<DebugPanel>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _stage = Object.FindAnyObjectByType<DescentStage>();
            yield return null;
            if (_stage.DwarfFrame < 0) Assert.Ignore("Sem as folhas do anão na cena (rode o setup).");
            if (!Simulation.Economy.HasIntern) Assert.Ignore("O balanço ativo não tem estagiário (rode o setup: GDD v7).");
            _internRenderer = _stage.transform.Find("Intern").GetComponent<MeshRenderer>();
        }

        void Click(string name)
        {
            var target = _panel.transform.Find("InternMenu/Content/Buttons/" + name);
            Assert.That(target, Is.Not.Null, "botão não encontrado: " + name);
            target.GetComponent<Button>().onClick.Invoke();
        }

        /// <summary>Dois quadros: o desenho (LateUpdate) vem depois do comando.</summary>
        static IEnumerator Frames()
        {
            yield return null;
            yield return null;
        }

        bool InternShown => _stage.InternFrame >= 0 && _internRenderer.enabled;

        [UnityTest]
        public IEnumerator HiringFromTheDbg_ShowsTheIntern()
        {
            yield return Load();
            Assert.That(InternShown, Is.False, "sem contratar, escondido");

            Click("Hire");
            yield return Frames();
            Assert.That(InternShown, Is.True, "contratado pelo DBG, aparece");
        }

        [UnityTest]
        public IEnumerator RestingWithoutPromotion_HidesTheIntern()
        {
            yield return Load();
            Click("Hire");
            yield return Frames();
            Assert.That(InternShown, Is.True);

            _runner.Rest(force: true);
            yield return Frames();
            Assert.That(Simulation.IsInternHired, Is.False);
            Assert.That(InternShown, Is.False, "sem a Promoção, ele vai embora no descanso");
        }

        [UnityTest]
        public IEnumerator WithPromotion_TheInternIsThereInTheNewRun()
        {
            yield return Load();
            Click("Promote");
            yield return Frames();
            Assert.That(InternShown, Is.False, "a Promoção vale na run seguinte");

            _runner.Rest(force: true);
            yield return Frames();
            Assert.That(InternShown, Is.True, "a run nova já começa com ele (D-169)");
        }

        /// <summary>
        /// Um golpe dele no núcleo dispara a picaretada dele na tela: no quadro desenhado logo depois do golpe, o boneco
        /// está no quadro de impacto. E o dano sobe do cristal num número cinza. No nível 1 o anão quebra cada cristal com
        /// um golpe (e bate primeiro, no mesmo instante): no 30 o cristal aguenta vários.
        /// </summary>
        [UnityTest]
        public IEnumerator InternHitInTheCore_TriggersHisSwing_AndAGreyNumber()
        {
            yield return Load();
            var feedback = Object.FindAnyObjectByType<MiningFeedbackView>();
            var floating = Object.FindAnyObjectByType<FloatingTexts>();
            Click("Hire");
            Simulation.SkipToCave(30);
            yield return Frames();

            int hits = 0;
            string shown = null;
            string expected = null;
            void OnInternHit(MiningHit hit)
            {
                hits++;
                shown = floating.LastText; // a vista do feedback se inscreveu antes: o número já saiu
                expected = NumberFormatter.Format(hit.Damage);
            }
            Simulation.InternHitLanded += OnInternHit;
            int numbersBefore = feedback.InternNumbersShown;

            int checkedHits = 0;
            float timeout = Time.realtimeSinceStartup + 20f;
            while (checkedHits < 2 && Time.realtimeSinceStartup < timeout)
            {
                int before = hits;
                yield return null; // volta depois do Update deste quadro: o golpe dele pode ter acontecido agora
                if (hits == before || Simulation.Miner.State != MinerState.Mining) continue;
                Assert.That(shown, Is.EqualTo(expected), "o dano dele no cristal");
                yield return null; // o desenho (LateUpdate) do quadro do golpe roda depois desta volta
                Assert.That(_stage.InternFrame, Is.EqualTo(_stage.DwarfImpactFrame), "a picaretada dele, no instante do golpe dele");
                Assert.That(_stage.LastInternHitAt, Is.GreaterThan(0));
                checkedHits++;
            }
            Simulation.InternHitLanded -= OnInternHit;

            Assert.That(checkedHits, Is.GreaterThan(0), "viu golpes do estagiário");
            Assert.That(feedback.InternNumbersShown, Is.GreaterThan(numbersBefore), "os números dele subiram");
        }

        /// <summary>O número dele é cinza (D-171), e não a cor de nenhum dos efeitos do anão.</summary>
        [Test]
        public void InternNumber_IsGrey()
        {
            var color = MiningFeedbackView.InternNumberColor;
            Assert.That(Mathf.Abs(color.r - color.g), Is.LessThan(0.05f));
            Assert.That(Mathf.Abs(color.g - color.b), Is.LessThan(0.05f));
        }
    }
}
