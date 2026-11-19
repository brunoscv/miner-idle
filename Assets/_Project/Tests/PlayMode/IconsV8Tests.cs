using System.Collections;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9i E6 (de-para aprovado em 2026-11-19, D-339), na cena Game de verdade: as peças do Apellon que faltavam. O
    /// cristal vermelho nas VERMELHAS (sem tinta), o botão laranja no Descansar e no Próximo andar, a barra de
    /// carregamento no anúncio e no raro, o ponto nas marcas da barra do andar. E o Recuo do DBG só na câmera F1 (#5).
    /// </summary>
    public class IconsV8Tests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        Transform _safeArea;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _safeArea = Object.FindAnyObjectByType<SafeAreaController>().transform;
        }

        [Test]
        public void TheRedShards_UseTheAppellonRedCrystal_Untinted()
        {
            int red = 0;
            foreach (var image in _safeArea.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null) continue;
                if (image.sprite.name == "icon_crystal")
                    Assert.That(image.color.r > 0.5f && image.color.g < 0.45f && image.color.b < 0.45f, Is.False,
                                image.transform.parent.name + "/" + image.name + ": o cristal roxo pintado de vermelho");
                if (image.sprite.name != "icon_crystal_red") continue;
                red++;
                Assert.That(image.color, Is.EqualTo(Color.white), image.name + ": sem tinta");
            }
            Assert.That(red, Is.GreaterThanOrEqualTo(5), "HUD, saldo da LOJA, Descansar, descanso e loja das permanentes");

            var row = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/PermanentRow.prefab");
            var price = new SerializedObject(row.GetComponent<PermanentRowView>()).FindProperty("priceIcon").objectReferenceValue as Image;
            Assert.That(price.sprite.name, Is.EqualTo("icon_crystal_red"), "o preço das permanentes");
        }

        [UnityTest]
        public IEnumerator TheBottomButtons_AreTheOrangeButton_DimmedWhileLockedOrFilling()
        {
            var rest = Object.FindAnyObjectByType<RestButton>(FindObjectsInactive.Include);
            var restImage = _safeArea.Find("Rest/RestButton").GetComponent<Image>();
            Assert.That(restImage.sprite.name, Is.EqualTo("ui_button_rest"));
            Assert.That(restImage.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(rest.IsLocked, Is.True, "no andar 1, trancado");
            Assert.That(restImage.color.r, Is.LessThan(0.7f), "apagado");

            var next = Object.FindAnyObjectByType<NextFloorButton>(FindObjectsInactive.Include);
            var dim = _safeArea.Find("NextFloor/Bar/NextFloorButton").GetComponent<Image>();
            var lit = _safeArea.Find("NextFloor/Bar/NextFloorButton/Fill/Lit").GetComponent<Image>();
            Assert.That(dim.sprite.name, Is.EqualTo("ui_button_rest"));
            Assert.That(lit.sprite.name, Is.EqualTo("ui_button_rest"));
            Assert.That(dim.color.r, Is.LessThan(0.7f), "enchendo: o laranja apagado");
            Assert.That(lit.color, Is.EqualTo(Color.white), "o que encheu acende");

            // Cheio no andar 1 (antes do automático, que no andar 10 já desceria sozinho): o DESCER todo aceso.
            _runner.Simulation.FillFloorBar();
            yield return null;
            Assert.That(next.IsReady, Is.True);

            _runner.Simulation.SkipToCave(_runner.Simulation.RestLevel);
            yield return null;
            yield return null;
            Assert.That(rest.IsLocked, Is.False);
            Assert.That(restImage.color, Is.EqualTo(Color.white), "aberto: aceso");
        }

        [UnityTest]
        public IEnumerator TheLockedRest_LooksLikeTheDescer_AndTheOpenOneStaysAsItWas()
        {
            var rest = Object.FindAnyObjectByType<RestButton>(FindObjectsInactive.Include);
            var button = _safeArea.Find("Rest/RestButton");
            var locked = button.Find("Locked");
            var next = Object.FindAnyObjectByType<NextFloorButton>(FindObjectsInactive.Include);
            var nextSo = new SerializedObject(next);
            var arrow = (RectTransform)((GameObject)nextSo.FindProperty("ready").objectReferenceValue).transform.Find("Arrow");
            var readyTitle = (TMPro.TMP_Text)nextSo.FindProperty("readyTitle").objectReferenceValue;
            var target = (TMPro.TMP_Text)nextSo.FindProperty("target").objectReferenceValue;
            var value = button.Find("Value").GetComponent<TMPro.TMP_Text>();
            var valueFont = value.font;

            Assert.That(rest.IsLocked, Is.True, "no andar 1, trancado");
            Assert.That(rest.IsLockedContentShown, Is.True);
            var lockImage = locked.Find("Lock").GetComponent<Image>();
            Assert.That(lockImage.sprite.name, Is.EqualTo("icon_lock"));
            Assert.That(lockImage.sprite.rect.size, Is.EqualTo(new Vector2(16f, 16f)), "o cadeado amarelo de 16×16");
            Assert.That(lockImage.rectTransform.rect.size, Is.EqualTo(arrow.rect.size), "do tamanho da escada");
            Assert.That(lockImage.rectTransform.anchoredPosition, Is.EqualTo(arrow.anchoredPosition), "no lugar da escada");
            var title = locked.Find("Title").GetComponent<TMPro.TMP_Text>();
            var floor = locked.Find("Floor").GetComponent<TMPro.TMP_Text>();
            Assert.That(title.text, Is.EqualTo("DESCANSAR"));
            Assert.That(title.font, Is.EqualTo(readyTitle.font));
            Assert.That(title.fontSize, Is.EqualTo(readyTitle.fontSize), "o título do tamanho do DESCER");
            Assert.That(floor.text, Is.EqualTo("ANDAR 10"));
            Assert.That(floor.font, Is.EqualTo(target.font));
            Assert.That(floor.fontSize, Is.EqualTo(target.fontSize), "a linha de baixo do tamanho do \"ANDAR n\"");
            Assert.That(button.Find("Icon").gameObject.activeSelf, Is.False, "o ícone de antes some");
            Assert.That(button.Find("Label").gameObject.activeSelf, Is.False);
            Assert.That(value.gameObject.activeSelf, Is.False);

            _runner.Simulation.SkipToCave(_runner.Simulation.RestLevel);
            yield return null;
            Assert.That(rest.IsLocked, Is.False);
            Assert.That(rest.IsLockedContentShown, Is.False, "aberto, o Descansar fica como era");
            Assert.That(button.Find("Icon").gameObject.activeSelf, Is.True);
            Assert.That(button.Find("Label").gameObject.activeSelf, Is.True);
            Assert.That(value.gameObject.activeSelf, Is.True);
            Assert.That(value.font, Is.EqualTo(valueFont), "as lascas na fonte dos números, como antes");
        }

        [Test]
        public void TheTimeBars_AreTheLoadingBar()
        {
            foreach (var path in new[] { "AdScreen/Screen/Panel/Bar", "RareWindow/Screen/Panel/Choosing/Bar" })
            {
                var bar = _safeArea.Find(path);
                Assert.That(bar, Is.Not.Null, path);
                Assert.That(bar.GetComponent<Image>().sprite.name, Is.EqualTo("ui_loading"), path);
                Assert.That(((RectTransform)bar).rect.height, Is.EqualTo(12f), "a altura do desenho do Apellon");
                Image fill = null;
                foreach (var image in bar.GetComponentsInChildren<Image>(true))
                    if (image.name == "Fill") fill = image;
                Assert.That(fill, Is.Not.Null, path + "/Fill");
                Assert.That(fill.sprite.name, Is.EqualTo("ui_loading_fill"));
                Assert.That(fill.rectTransform.rect.height, Is.EqualTo(4f).Within(0.01f), "o miolo da moldura");
            }
        }

        [Test]
        public void TheFloorBarMarks_AreTheAppellonDots()
        {
            var bar = Object.FindAnyObjectByType<FloorBar>();
            Assert.That(bar.TickCount, Is.GreaterThan(0));
            int dots = 0;
            foreach (var image in bar.GetComponentsInChildren<Image>())
            {
                if (!image.name.StartsWith("Tick") || image.sprite == null) continue;
                Assert.That(image.sprite.name, Is.EqualTo("cave_meter_t"));
                Assert.That(image.rectTransform.sizeDelta, Is.EqualTo(new Vector2(3f, 3f)));
                dots++;
            }
            Assert.That(dots, Is.EqualTo(bar.TickCount));
        }

        [UnityTest]
        public IEnumerator TheSetbackButton_OnlyShows_OnTheF1Camera()
        {
            var panel = Object.FindAnyObjectByType<DescentCameraPanel>(FindObjectsInactive.Include);
            var stage = Object.FindAnyObjectByType<DescentStage>();
            panel.Toggle();
            yield return null;
            Assert.That(panel.IsF1Camera, Is.False, "o jogo abre na câmera do v8");
            Assert.That(panel.IsSetbackShown, Is.False, "o Recuo some na do v8 (era \"Recuo ?\")");

            stage.Framing = DescentFraming.F1;
            yield return new WaitForSecondsRealtime(0.7f); // o painel se atualiza a cada 0,5 s
            Assert.That(panel.IsSetbackShown, Is.True, "na F1 ele volta");
            panel.Toggle();
        }
    }
}
