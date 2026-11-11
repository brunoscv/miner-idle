using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9b, E4: o cristal raro (GDD v7 §3.5; D-97, D-184…D-186, D-190, D-219…D-224). Um cristal que nasce pode ser raro
    /// (sorteio próprio, com semente); quebrá-lo abre a janela com 2 cartas diferentes e 30 s para escolher; sem escolha, o
    /// jogo sorteia; com uma aberta, o raro espera na fila. Cada carta vale 60 s (repetida, renova), para o anão e, menos o
    /// crítico, para o estagiário (também com o app fechado). A janela vai para o save; o descanso a apaga.
    /// </summary>
    public class RareCrystalTests
    {
        const string GddV7Path = "Assets/_Project/Data/Balance_GDD_v7.asset";

        static readonly RareCardRule[] Cards =
        {
            new RareCardRule(RareCard.Damage, 2, 60),
            new RareCardRule(RareCard.Speed, 2, 60),
            new RareCardRule(RareCard.Shards, 2, 60),
            new RareCardRule(RareCard.Critical, 0.25, 60),
        };

        static MiningRules Rules(int initial = 0, double health = 1e9, double spawnInterval = 1e9) =>
            new MiningRules(
                initialCrystals: initial, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: spawnInterval,
                minCrystalSpacing: 2, spawnDistance: 12, crystalHealth: health, crystalReward: 1, damage: 1, hitInterval: 1,
                walkSpeed: 4, miningRange: 1);

        /// <summary>O raro e o marco ×10 do combo (+10% por 5 s), para ver a ordem do dano (D-219).</summary>
        static StrikeRules Strike(double chance = 0.15, double slowChance = 0.02, IEnumerable<RareCardRule> cards = null) =>
            new StrikeRules(0, 2, 0.1, 1, comboBonuses: new[] { new ComboBonusRule(10, BonusKind.DamagePercent, 0.10, 5) },
                            rare: new RareRules(chance, slowChance, 30, cards ?? Cards));

        static EconomyRules WithIntern() => new EconomyRules(
            pickaxe: new UpgradeDefinition(2, 2, 49, 1, CostFormula.Linear),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            intern: new UpgradeDefinition(500, 1, 1, 1),
            internPickaxe: new UpgradeDefinition(50, 4, 19, 5),
            internSpeed: new UpgradeDefinition(50, 5, 19, 0.1),
            internMining: new UpgradeDefinition(50, 1.2, 10, 1),
            internPickaxeGrowth: 2);

        static UpgradeDefinition Line(int max, double effect) => new UpgradeDefinition(10, 2, max, effect, CostFormula.OffsetExponential);

        static PermanentRules V7Permanents() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1),
            Line(10, 10), Line(20, 0.1), Line(20, 0.5), Line(10, 0.05), Line(10, 1),
            Line(10, 0.1), new UpgradeDefinition(50, 1, 1, 1));

        static MiningSimulation Sim(MiningRules rules = null, StrikeRules strike = null, GameState state = null,
                                    ulong rareSeed = 11, RareRhythm rhythm = RareRhythm.Gdd) =>
            new MiningSimulation(rules ?? Rules(), new SeededRandom(7), state, WithIntern(), strike ?? Strike(),
                                 new Permanents(V7Permanents(), new PermanentState()), new SeededRandom(rareSeed), rhythm);

        static RareCard[] ActiveCards(MiningSimulation sim) =>
            sim.Bonuses.All.Where(b => b.Source == BonusSource.Rare).Select(b => (RareCard)b.Tier).OrderBy(c => c).ToArray();

        // ── O sorteio ───────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void ChanceOne_EveryCrystalIsRare_ChanceZero_None()
        {
            Assert.That(Sim(Rules(initial: 5), Strike(chance: 1)).Crystals.All(c => c.IsRare), Is.True);
            Assert.That(Sim(Rules(initial: 5), Strike(chance: 0)).Crystals.Any(c => c.IsRare), Is.False);
            Assert.That(Sim(Rules(initial: 5), StrikeRules.NoCritical()).Crystals.Any(c => c.IsRare), Is.False,
                        "sem cartas no balanço, sem raro");
        }

        [Test]
        public void TheSameSeed_GivesTheSameRareCrystals_AndTheCritDrawIsApart()
        {
            bool[] Pattern(ulong seed) => Sim(Rules(initial: 12), Strike(chance: 0.5), rareSeed: seed)
                                          .Crystals.Select(c => c.IsRare).ToArray();

            var first = Pattern(3);
            Assert.That(Pattern(3), Is.EqualTo(first));
            Assert.That(first.Count(r => r), Is.InRange(1, 11), "com 50%, 12 cristais não saem todos iguais nesta semente");
        }

        [Test]
        public void ForceNextRare_MakesOnlyTheNextCrystalRare()
        {
            var sim = Sim(Rules(spawnInterval: 1), Strike(chance: 0));
            sim.ForceNextRare = true;
            sim.Tick(1.5);

            Assert.That(sim.Crystals.Count(c => c.IsRare), Is.EqualTo(1));
            Assert.That(sim.ForceNextRare, Is.False);
        }

        // ── A janela ────────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void BreakingARare_OpensTheWindow_WithTwoDifferentCards_AndThirtySeconds()
        {
            var sim = Sim(Rules(initial: 1, health: 1), Strike(chance: 1));
            var events = new List<RareWindow>();
            sim.RareWindowChanged += events.Add;

            sim.Tick(3);

            Assert.That(sim.State.Breaks, Is.GreaterThanOrEqualTo(1));
            var window = sim.RareWindow;
            Assert.That(window.IsChoosing, Is.True);
            Assert.That(window.First, Is.Not.EqualTo(window.Second));
            Assert.That(window.SecondsLeft, Is.LessThan(30).And.GreaterThan(26), "o tempo corre desde a quebra");
            Assert.That(events, Has.Count.EqualTo(1));
        }

        [Test]
        public void TheDraw_AlwaysGivesTwoDifferentCardsFromTheBalance()
        {
            var sim = Sim(strike: Strike(cards: new[] { Cards[0], Cards[3] }));
            for (int i = 0; i < 40; i++)
            {
                sim.OpenRareWindow();
                var window = sim.RareWindow;
                Assert.That(new[] { window.First, window.Second }, Is.EquivalentTo(new[] { RareCard.Damage, RareCard.Critical }));
                sim.ChooseRareCard(i % 2);
            }
        }

        [Test]
        public void Choosing_AppliesTheCardForSixtySeconds_AndClosesTheWindow()
        {
            var sim = Sim();
            var applied = new List<RareCard>();
            sim.RareCardApplied += applied.Add;
            sim.OpenRareWindow();
            var card = sim.RareWindow.Second;

            Assert.That(sim.ChooseRareCard(1), Is.True);

            Assert.That(sim.RareWindow.IsOpen, Is.False);
            Assert.That(applied, Is.EqualTo(new[] { card }));
            var bonus = sim.Bonuses.All.Single();
            Assert.That(bonus.Source, Is.EqualTo(BonusSource.Rare));
            Assert.That(bonus.Tier, Is.EqualTo((int)card));
            Assert.That(bonus.SecondsLeft, Is.EqualTo(60));
            Assert.That(sim.ChooseRareCard(0), Is.False, "sem janela, nada a escolher");
        }

        [Test]
        public void ThirtySecondsWithoutChoice_TheGamePicks_AtTheExactTime_AndWaitsForClose()
        {
            var sim = Sim();
            sim.OpenRareWindow();
            var cards = new[] { sim.RareWindow.First, sim.RareWindow.Second };
            double pickedAt = double.NaN;
            sim.RareCardApplied += _ => pickedAt = sim.Elapsed;

            for (int i = 0; i < 100; i++) sim.Tick(1.0 / 3); // ×20 a 60 qps: 33,3 s

            Assert.That(pickedAt, Is.EqualTo(30).Within(1e-9));
            var window = sim.RareWindow;
            Assert.That(window.IsOpen && !window.IsChoosing && window.AutoPicked, Is.True, "mostra o sorteio até o FECHAR");
            Assert.That(ActiveCards(sim), Is.EqualTo(new[] { cards[window.Chosen] }));
            Assert.That(sim.Bonuses.All.Single().SecondsLeft, Is.EqualTo(60 - (sim.Elapsed - 30)).Within(1e-9), "a carta já corre");
            Assert.That(sim.ChooseRareCard(0), Is.False, "depois do sorteio, só FECHAR");

            Assert.That(sim.CloseRareWindow(), Is.True);
            Assert.That(sim.RareWindow.IsOpen, Is.False);
        }

        [Test]
        public void ARareWhileTheWindowIsOpen_WaitsInTheQueue()
        {
            var sim = Sim();
            sim.OpenRareWindow();
            sim.Tick(10);
            sim.OpenRareWindow();

            Assert.That(sim.RareWindow.Queued, Is.EqualTo(1));
            Assert.That(sim.RareWindow.SecondsLeft, Is.EqualTo(20).Within(1e-9), "a fila não mexe no tempo da janela");

            sim.ChooseRareCard(0);
            Assert.That(sim.RareWindow.IsChoosing, Is.True, "a próxima da fila abre");
            Assert.That(sim.RareWindow.Queued, Is.EqualTo(0));
            Assert.That(sim.RareWindow.SecondsLeft, Is.EqualTo(30), "com os 30 s inteiros");

            sim.ChooseRareCard(0);
            Assert.That(sim.RareWindow.IsOpen, Is.False);
        }

        [Test]
        public void TheSameCardAgain_RenewsTheTime_WithoutDoublingTheEffect()
        {
            var sim = Sim();
            sim.GrantRareCard(RareCard.Damage);
            sim.Tick(40);
            sim.GrantRareCard(RareCard.Damage);

            Assert.That(sim.Bonuses.Count, Is.EqualTo(1));
            Assert.That(sim.Bonuses.All[0].SecondsLeft, Is.EqualTo(60));
            Assert.That(sim.Stats.Damage, Is.EqualTo(2).Within(1e-12));
        }

        // ── As cartas (D-219) ───────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Damage_DoublesTheDwarfAfterTheCombo_AndTheIntern()
        {
            var sim = Sim();
            sim.HireIntern();
            double intern = sim.InternStats.Damage;
            sim.GrantComboBonuses(10); // +10%

            sim.GrantRareCard(RareCard.Damage);

            Assert.That(sim.Stats.Damage, Is.EqualTo(1.1 * 2).Within(1e-12));
            Assert.That(sim.InternStats.Damage, Is.EqualTo(intern * 2).Within(1e-12));
        }

        [Test]
        public void Speed_HalvesTheInterval_ForTheDwarfAndTheIntern()
        {
            var sim = Sim();
            sim.HireIntern();
            double dwarf = sim.Stats.HitInterval, intern = sim.InternStats.HitInterval;
            var internChanges = new List<InternStats>();
            sim.InternChanged += internChanges.Add;

            sim.GrantRareCard(RareCard.Speed);

            Assert.That(sim.Stats.HitInterval, Is.EqualTo(dwarf / 2).Within(1e-12));
            Assert.That(sim.InternStats.HitInterval, Is.EqualTo(intern / 2).Within(1e-12));
            Assert.That(internChanges, Has.Count.EqualTo(1), "a tela do estagiário fica sabendo");

            sim.Tick(61);
            Assert.That(sim.Stats.HitInterval, Is.EqualTo(dwarf).Within(1e-12), "acabou: volta");
            Assert.That(sim.InternStats.HitInterval, Is.EqualTo(intern).Within(1e-12));
        }

        [Test]
        public void Shards_DoublesEveryBreak()
        {
            var plain = Sim(Rules(initial: 1, health: 1));
            var doubled = Sim(Rules(initial: 1, health: 1));
            doubled.GrantRareCard(RareCard.Shards);

            plain.Tick(3);
            doubled.Tick(3);

            Assert.That(plain.State.Shards, Is.GreaterThan(0));
            Assert.That(doubled.State.Shards, Is.EqualTo(plain.State.Shards * 2).Within(1e-9));
        }

        [Test]
        public void Critical_AddsTwentyFivePoints_ForTheDwarfOnly()
        {
            var sim = Sim();
            sim.HireIntern();
            double crit = sim.Stats.CritBonus;
            var intern = sim.InternStats;

            sim.GrantRareCard(RareCard.Critical);

            Assert.That(sim.Stats.CritBonus, Is.EqualTo(crit + 0.25).Within(1e-12));
            Assert.That(sim.InternStats.Damage, Is.EqualTo(intern.Damage));
            Assert.That(sim.InternStats.HitInterval, Is.EqualTo(intern.HitInterval));
        }

        [Test]
        public void TheShopPreview_ComparesWithoutTheCards()
        {
            var sim = Sim();
            var before = sim.Preview(UpgradeId.Pickaxe);
            sim.GrantRareCard(RareCard.Damage);
            var after = sim.Preview(UpgradeId.Pickaxe);

            Assert.That(after.CurrentValue, Is.EqualTo(before.CurrentValue));
            Assert.That(after.NextValue, Is.EqualTo(before.NextValue));
        }

        // ── App fechado, save e descanso ────────────────────────────────────────────────────────────────────────────

        [Test]
        public void ClosedApp_TheInternGetsTheCard_UntilItEnds()
        {
            OfflineReport Away(RareCard? card, double seconds)
            {
                var sim = Sim(Rules(health: 1)); // o estagiário quebra um cristal por golpe
                sim.HireIntern();
                if (card.HasValue) sim.GrantRareCard(card.Value);
                return sim.ApplyOffline(seconds);
            }

            var plainMinute = Away(null, 60);
            Assert.That(plainMinute.Shards, Is.GreaterThan(0));
            Assert.That(Away(RareCard.Shards, 60).Shards, Is.EqualTo(plainMinute.Shards * 2).Within(1e-9), "a carta o minuto inteiro");
            Assert.That(Away(RareCard.Critical, 60).Shards, Is.EqualTo(plainMinute.Shards), "o crítico não vale para ele");

            double plainHour = Away(null, 3600).Shards;
            double withCard = Away(RareCard.Shards, 3600).Shards;
            Assert.That(withCard, Is.GreaterThan(plainHour).And.LessThan(plainHour * 1.1), "só o 1º minuto dobra");

            var sim2 = Sim();
            sim2.GrantRareCard(RareCard.Damage);
            sim2.ApplyOffline(90);
            Assert.That(sim2.Bonuses.Count, Is.EqualTo(0), "o tempo da carta corre com o app fechado");
        }

        [Test]
        public void ClosedApp_TheWindowWaits()
        {
            var sim = Sim();
            sim.OpenRareWindow();
            sim.ApplyOffline(3600);

            Assert.That(sim.RareWindow.IsChoosing, Is.True);
            Assert.That(sim.RareWindow.SecondsLeft, Is.EqualTo(30), "os 30 s só correm com o app aberto (D-222)");
        }

        [Test]
        public void Save_KeepsTheWindow_AndAVersion6SaveOpensWithout()
        {
            string dir = Path.Combine(Path.GetTempPath(), "MinersIdleRareSave_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new FileSaveStore(dir);
                var sim = Sim();
                sim.GrantRareCard(RareCard.Speed);
                sim.OpenRareWindow();
                sim.Tick(12);
                sim.OpenRareWindow();
                new SaveService(store).Save(sim.State);

                var loaded = new SaveService(store).Load(WithIntern(), out var status);
                Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
                Assert.That(File.ReadAllText(store.FilePath), Does.Match(@"""version"":\s*7"));
                Assert.That(loaded.RareWindow, Is.EqualTo(sim.RareWindow));
                var reopened = Sim(state: loaded);
                Assert.That(reopened.RareWindow.SecondsLeft, Is.EqualTo(18).Within(1e-9));
                Assert.That(reopened.RareWindow.Queued, Is.EqualTo(1));
                Assert.That(reopened.Stats.HitInterval, Is.EqualTo(sim.Stats.HitInterval).Within(1e-12), "a carta volta com o save");

                File.WriteAllText(store.FilePath, "{ \"version\": 6, \"shards\": 3.0, \"rareWindow\": { \"open\": true, " +
                                                  "\"first\": \"Damage\", \"second\": \"Speed\", \"secondsLeft\": 9 } }");
                Assert.That(new SaveService(store).Load(WithIntern(), out _).RareWindow.IsOpen, Is.False, "o v6 não tinha janela");
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
        }

        [Test]
        public void Restore_ClosesABrokenWindow()
        {
            Assert.That(RareWindow.Restore(true, 0, 0, 10, -1, false, 0).IsOpen, Is.False, "cartas iguais");
            Assert.That(RareWindow.Restore(true, 0, 9, 10, -1, false, 0).IsOpen, Is.False, "carta desconhecida");
            Assert.That(RareWindow.Restore(false, 0, 1, 10, -1, false, 0).IsOpen, Is.False);
            Assert.That(RareWindow.Restore(true, 0, 1, double.NaN, -1, false, 0).SecondsLeft, Is.EqualTo(0));
            var chosenButNotPicked = RareWindow.Restore(true, 0, 1, 10, 1, false, 0);
            Assert.That(chosenButNotPicked.IsChoosing, Is.True, "escolhida sem sorteio: volta a escolher");
        }

        /// <summary>D-224: o descanso termina as cartas e apaga a janela e a fila.</summary>
        [Test]
        public void Rest_TheNewRunHasNoCardsNorWindow()
        {
            var sim = Sim();
            sim.GrantRareCard(RareCard.Damage);
            sim.OpenRareWindow();
            sim.OpenRareWindow();
            Assert.That(sim.Rest(force: true), Is.Not.Null);

            var next = sim.Permanents.NewRun(WithIntern());

            Assert.That(next.Bonuses.Count, Is.EqualTo(0));
            Assert.That(next.RareWindow.IsOpen, Is.False);
        }

        // ── O ritmo lento do DBG (D-185) ────────────────────────────────────────────────────────────────────────────

        [Test]
        public void SlowRhythm_UsesTheSlowChance_AndNoRareWithACardActive()
        {
            Assert.That(Sim(Rules(initial: 6), Strike(chance: 1, slowChance: 0), rhythm: RareRhythm.Slow)
                        .Crystals.Any(c => c.IsRare), Is.False, "no ritmo lento vale a chance lenta");

            var sim = Sim(Rules(spawnInterval: 10), Strike(chance: 0, slowChance: 1), rhythm: RareRhythm.Slow);
            sim.GrantRareCard(RareCard.Damage);
            sim.Tick(55);
            Assert.That(sim.Crystals.Count, Is.GreaterThan(0));
            Assert.That(sim.Crystals.Any(c => c.IsRare), Is.False, "carta ativa: nenhum raro novo");

            sim.Tick(30);
            Assert.That(sim.Crystals.Any(c => c.IsRare), Is.True, "a carta acabou: volta a sair");
        }

        [Test]
        public void SlowRhythm_NoRareWithTheWindowOpen()
        {
            var sim = Sim(Rules(spawnInterval: 5), Strike(chance: 0, slowChance: 1), rhythm: RareRhythm.Slow);
            sim.OpenRareWindow();
            sim.Tick(25);
            Assert.That(sim.Crystals.Count, Is.GreaterThan(0));
            Assert.That(sim.Crystals.Any(c => c.IsRare), Is.False);
        }

        // ── Regras e dados ──────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void RareRules_SortsTheCards_AndNeedsTwo()
        {
            var rules = new RareRules(0.15, 0.02, 30, Cards.Reverse());
            Assert.That(rules.Cards.Select(c => c.Card), Is.EqualTo(new[] { RareCard.Damage, RareCard.Speed, RareCard.Shards, RareCard.Critical }));
            Assert.That(rules.IsEnabled, Is.True);
            Assert.That(new RareRules(0.15, 0.02, 30, new[] { Cards[0] }).IsEnabled, Is.False, "1 carta não faz janela de 2");
            Assert.That(RareRules.None().IsEnabled, Is.False);
            var sim = Sim(strike: new StrikeRules(0, 2, 0.1, 1, rare: new RareRules(1, 1, 30, new[] { Cards[0] })));
            Assert.That(sim.OpenRareWindow(), Is.False);
        }

        [Test]
        public void GddV7Balance_HasTheRareCrystal()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7Path);
            Assert.That(asset, Is.Not.Null);
            var rare = asset.ToStrike().Rare;
            const string hint = "rode o setup do projeto (Tools → Miners Idle → Projeto → Aplicar configuração do projeto)";
            Assert.That(rare.Chance, Is.EqualTo(0.15), hint);
            Assert.That(rare.SlowChance, Is.EqualTo(0.02), hint);
            Assert.That(rare.ChooseSeconds, Is.EqualTo(30), hint);
            Assert.That(rare.Cards.Select(c => (c.Card, c.Value, c.Seconds)),
                        Is.EqualTo(Cards.Select(c => (c.Card, c.Value, c.Seconds))), hint);
        }
    }
}
