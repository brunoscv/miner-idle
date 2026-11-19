using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9d E3 (D-278, D-281): o HUD do v8 na cena Game. No alto, só LASCAS e VERMELHAS; o andar numa barra à esquerda,
    /// com o andar de agora, o recorde, um trecho por bioma e os andares que liberam algo; DANO e VELOCIDADE no alto da
    /// LOJA, acima das abas.
    /// </summary>
    public class HudV8Tests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        HudView _hud;
        FloorBar _bar;
        Transform _safeArea;

        MiningSimulation Simulation => _runner.Simulation;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _hud = Object.FindAnyObjectByType<HudView>();
            _bar = Object.FindAnyObjectByType<FloorBar>();
            _safeArea = Object.FindAnyObjectByType<SafeAreaController>().transform;
            Assert.That(_bar, Is.Not.Null, "a cena precisa da barra do andar (rode o setup do projeto)");
        }

        [Test]
        public void TheHud_ShowsOnlyShardsAndRedShards()
        {
            var hud = _hud.transform;
            Assert.That(hud.Find("Lascas"), Is.Not.Null);
            Assert.That(hud.Find("Vermelhas"), Is.Not.Null);
            foreach (var gone in new[] { "Dano", "Caverna", "Velocidade" })
                Assert.That(hud.Find(gone), Is.Null, gone + " saiu do HUD (D-278)");

            // O ícone das VERMELHAS é o do Descansar (o cristal tingido de vermelho), não o roxo das LASCAS. No 10 o
            // Descansar abre (antes dele mostra o cadeado, M9i E1).
            Simulation.SkipToCave(10);
            var ruby = _safeArea.Find("Rest/RestButton/Icon")?.GetComponent<UnityEngine.UI.Image>();
            var icon = hud.Find("Vermelhas/Row/Icon")?.GetComponent<UnityEngine.UI.Image>();
            if (ruby != null && icon != null) Assert.That(icon.color, Is.EqualTo(ruby.color), "o rubi");
        }

        /// <summary>
        /// M9i E2 (D-335): a barra é a borda do Apellon, centrada no vão entre o HUD e a faixa da bomba (97 px de baixo,
        /// na referência 270): o mesmo vão em cima e embaixo.
        /// </summary>
        [Test]
        public void FloorBar_IsTheApellonMeter_CenteredBetweenTheHudAndTheBomb()
        {
            var bar = (RectTransform)_bar.transform;
            Assert.That(bar.GetComponent<UnityEngine.UI.Image>().sprite.name, Is.EqualTo("floor_meter"));
            var safe = (RectTransform)_safeArea;
            float scale = safe.lossyScale.y;
            var corners = new Vector3[4];
            bar.GetWorldCorners(corners);
            float barTop = corners[1].y, barBottom = corners[0].y;
            ((RectTransform)_hud.transform).GetWorldCorners(corners);
            float hudBottom = corners[0].y;
            safe.GetWorldCorners(corners);
            float bombTop = corners[0].y + 97f * scale;
            float above = (hudBottom - barTop) / scale, below = (barBottom - bombTop) / scale;
            // Na tela do batch (640×480, deitada) o vão fica negativo: a barra é mais alta que o espaço. Centrada, os
            // dois lados continuam iguais; no celular (9:16, 9:20) os dois são positivos.
            Assert.That(above, Is.EqualTo(below).Within(1.5f), "o mesmo vão em cima e embaixo");
        }

        [Test]
        public void RedShards_FollowTheBank()
        {
            Assert.That(_hud.RedShardsText, Is.EqualTo(NumberFormatter.FormatShards(Simulation.Permanents.State.RedShards)));
            Simulation.AddRedShards(31);
            Assert.That(_hud.RedShardsText, Is.EqualTo(NumberFormatter.FormatShards(Simulation.Permanents.State.RedShards)));
            Assert.That(Simulation.Permanents.State.RedShards, Is.GreaterThanOrEqualTo(31));
        }

        [Test]
        public void TheFloorBar_FollowsTheDescentAndTheRecord()
        {
            var rules = Simulation.Rules;
            int biomes = (rules.CaveCount + rules.LevelsPerBiome - 1) / rules.LevelsPerBiome;
            Assert.That(_bar.SegmentCount, Is.EqualTo(biomes), "um trecho por bioma");
            Assert.That(_bar.TickCount, Is.GreaterThan(0), "os andares que liberam algo");
            Assert.That(_bar.ShownFloor, Is.EqualTo(Simulation.State.Cave));

            Simulation.SkipToCave(12);
            Assert.That(_bar.ShownFloor, Is.EqualTo(12));
            Assert.That(_hud.CaveText, Is.EqualTo("12"), "a etiqueta da seta");
            Assert.That(_bar.FillAmount, Is.EqualTo(12f / rules.CaveCount).Within(0.001f), "o âmbar até o andar 12");
            Assert.That(_bar.IsRecordShown, Is.False, "o recorde é o andar de agora: só a seta");

            Simulation.SetDeepestFloor(25);
            Assert.That(_bar.ShownRecord, Is.EqualTo(25));
            Assert.That(_bar.IsRecordShown, Is.True);
            Assert.That(_bar.RecordText, Is.EqualTo("25"));
            Assert.That(_bar.ShownFloor, Is.EqualTo(12), "o recorde não move a seta");
        }

        [Test]
        public void DamageAndSpeed_LiveAtTheTopOfTheShop_AboveTheTabs()
        {
            var panel = _safeArea.Find("Shop/Screen/Panel");
            var stats = panel.Find("Stats");
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.Find("Dano"), Is.Not.Null);
            Assert.That(stats.Find("Velocidade"), Is.Not.Null);

            var statsRect = (RectTransform)stats;
            var tabs = (RectTransform)panel.Find("Tabs");
            Assert.That(tabs.offsetMax.y, Is.LessThanOrEqualTo(statsRect.offsetMin.y), "as abas embaixo da faixa");

            Assert.That(_hud.DamageText, Is.EqualTo(NumberFormatter.Format(Simulation.Stats.Damage)));
            Assert.That(_hud.SpeedText, Is.EqualTo(NumberFormatter.FormatSeconds(Simulation.Stats.HitInterval)));
        }
    }
}
