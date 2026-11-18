using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9c E3: os marcos de andar do GDD v8 valem para sempre a partir do andar mais fundo já alcançado (D-253). No 5, o
    /// avanço automático (P6: a barra cheia desce na hora, com o ajuste ligado; desligado, espera o toque; o último andar
    /// espera o toque) e o estagiário na LOJA (P7: travado com "Andar 5" antes); depois do 10, o raro. O recorde sobrevive
    /// ao descanso. DBG → Andar → Recorde põe o recorde direto.
    /// </summary>
    public class FloorMilestoneTests
    {
        /// <summary>Um andar pede andar × 1 cristal, para quebrar à mão; marcos do v8 (5, 5, 10).</summary>
        static MiningRules Rules(int autoAdvanceFloor = 5, int internFloor = 5, int rareFloor = 10) =>
            new MiningRules(
                initialCrystals: 4, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4,
                miningRange: 1, minCrystals: 4, caveCount: 100, caveHealthGrowth: 1.5, floorBreaks: 1,
                offlineMaxSeconds: 4 * 3600, offlineDeepMaxSeconds: 6 * 3600, offlineDeepFloor: 20,
                autoAdvanceFloor: autoAdvanceFloor, internFloor: internFloor, rareFloor: rareFloor);

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

        static readonly RareCardRule[] Cards =
        {
            new RareCardRule(RareCard.Damage, 2, 60),
            new RareCardRule(RareCard.Speed, 2, 60),
        };

        static PermanentRules Permanents() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1),
            new UpgradeDefinition(10, 2, 100, 5, CostFormula.OffsetExponential),
            new UpgradeDefinition(10, 2, 20, 0.1, CostFormula.OffsetExponential),
            new UpgradeDefinition(10, 2, 20, 0.5, CostFormula.OffsetExponential),
            new UpgradeDefinition(10, 2, 10, 0.05, CostFormula.OffsetExponential),
            new UpgradeDefinition(10, 2, 10, 1, CostFormula.OffsetExponential));

        static MiningSimulation Sim(MiningRules rules = null, double rareChance = 0, PermanentState permanent = null,
                                    GameState state = null) =>
            new MiningSimulation(rules ?? Rules(), new SeededRandom(3), state, WithIntern(),
                                 new StrikeRules(0, 1.5, rare: new RareRules(rareChance, 0, 30, Cards)),
                                 new Permanents(Permanents(), permanent ?? new PermanentState()), new SeededRandom(5));

        static void BreakOne(MiningSimulation sim)
        {
            sim.Tick(0);
            Assert.That(sim.BreakTarget(), Is.True, "um cristal na mira");
        }

        // ── Andar 5: o avanço automático (P6) ──────────────────────────────────────────────────────────────────

        [Test]
        public void AutoAdvance_BeforeFloor5_TheFullBarWaitsForTheTap()
        {
            var sim = Sim();
            Assert.That(sim.AutoAdvance, Is.True, "ligado de fábrica");
            Assert.That(sim.IsAutoAdvanceUnlocked, Is.False);
            BreakOne(sim);
            sim.Tick(1);
            Assert.That(sim.State.Cave, Is.EqualTo(1), "antes do andar 5, ninguém desce sozinho");
            Assert.That(sim.CanAdvance, Is.True);
            Assert.That(sim.IsAutoAdvancing, Is.False);
        }

        [Test]
        public void AutoAdvance_FromFloor5_DescendsTheMomentTheBarFills()
        {
            var sim = Sim();
            sim.SkipToCave(5);
            Assert.That(sim.IsAutoAdvanceUnlocked, Is.True);
            Assert.That(sim.IsAutoAdvancing, Is.True);
            for (int i = 0; i < 4; i++) BreakOne(sim);
            sim.Tick(0);
            Assert.That(sim.State.Cave, Is.EqualTo(5), "4 de 5: ainda não");

            BreakOne(sim);
            sim.Tick(0);
            Assert.That(sim.State.Cave, Is.EqualTo(6), "a barra encheu: desce na hora");
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(0), "a barra zera");
            Assert.That(sim.Permanents.State.DeepestFloor, Is.EqualTo(6));
        }

        [Test]
        public void AutoAdvance_UsesTheSameTransition()
        {
            var sim = Sim();
            sim.SkipToCave(5);
            sim.FloorTransitionSeconds = 2.5;
            sim.FillFloorBar();
            sim.Tick(0);
            Assert.That(sim.State.Cave, Is.EqualTo(6));
            Assert.That(sim.IsInFloorTransition, Is.True, "a mesma transição do toque");
            sim.FillFloorBar();
            sim.Tick(1);
            Assert.That(sim.State.Cave, Is.EqualTo(6), "na transição, nada desce");
            sim.Tick(2);
            Assert.That(sim.State.Cave, Is.EqualTo(7), "acabou a transição: a barra cheia desce de novo");
        }

        [Test]
        public void AutoAdvance_TurnedOff_TheFullBarWaits_AndTurnedOnAgain_Descends()
        {
            var sim = Sim();
            sim.SkipToCave(5);
            sim.AutoAdvance = false;
            Assert.That(sim.IsAutoAdvancing, Is.False);
            sim.FillFloorBar();
            sim.Tick(1);
            Assert.That(sim.State.Cave, Is.EqualTo(5), "desligado: a barra fica cheia (P4)");
            Assert.That(sim.FloorProgress, Is.EqualTo(1));

            sim.AutoAdvance = true;
            sim.Tick(0);
            Assert.That(sim.State.Cave, Is.EqualTo(6), "ligado de novo: desce");
        }

        [Test]
        public void AutoAdvance_TheLastFloor_WaitsForTheTapToTheHeart()
        {
            var sim = Sim();
            sim.SkipToCave(100);
            sim.FillFloorBar();
            sim.Tick(1);
            Assert.That(sim.State.ReachedHeart, Is.False, "o fim da partida é do jogador");
            Assert.That(sim.CanAdvance, Is.True);
            Assert.That(sim.AdvanceFloor(), Is.True);
            Assert.That(sim.State.ReachedHeart, Is.True);
        }

        [Test]
        public void AutoAdvance_ABalanceWithoutIt_NeverDescendsAlone()
        {
            var sim = Sim(Rules(autoAdvanceFloor: 0));
            sim.SkipToCave(50);
            sim.FillFloorBar();
            sim.Tick(1);
            Assert.That(sim.IsAutoAdvanceUnlocked, Is.False);
            Assert.That(sim.State.Cave, Is.EqualTo(50));
        }

        // ── Andar 5: o estagiário na LOJA (P7) ─────────────────────────────────────────────────────────────────

        [Test]
        public void Intern_IsLockedUntilFloor5_WithTheFloorInPlaceOfThePrice()
        {
            var sim = Sim();
            sim.AddShards(10_000);
            Assert.That(sim.IsInternUnlocked, Is.False);
            var preview = sim.Preview(UpgradeId.Intern);
            Assert.That(preview.State, Is.EqualTo(UpgradeButtonState.Locked));
            Assert.That(preview.RequiredFloor, Is.EqualTo(5), "a LOJA mostra \"Andar 5\"");
            Assert.That(sim.Purchase(UpgradeId.Intern), Is.EqualTo(PurchaseResult.Locked));

            sim.SkipToCave(5);
            Assert.That(sim.IsInternUnlocked, Is.True);
            preview = sim.Preview(UpgradeId.Intern);
            Assert.That(preview.State, Is.EqualTo(UpgradeButtonState.Purchasable));
            Assert.That(preview.RequiredFloor, Is.EqualTo(0));
            Assert.That(sim.Purchase(UpgradeId.Intern), Is.EqualTo(PurchaseResult.Purchased));
        }

        [Test]
        public void Intern_TheOtherLines_NeverLockByFloor()
        {
            var sim = Sim();
            sim.AddShards(10_000);
            Assert.That(sim.Preview(UpgradeId.Pickaxe).State, Is.EqualTo(UpgradeButtonState.Purchasable));
            Assert.That(sim.Preview(UpgradeId.Pickaxe).RequiredFloor, Is.EqualTo(0));
            Assert.That(sim.Preview(UpgradeId.InternPickaxe).RequiredFloor, Is.EqualTo(0), "a árvore espera a contratação, não o andar");
        }

        // ── Depois do andar 10: o raro ─────────────────────────────────────────────────────────────────────────

        [Test]
        public void Rare_OnlyBornAfterFloor10()
        {
            var sim = Sim(rareChance: 1);
            Assert.That(sim.IsRareUnlocked, Is.False);
            Assert.That(sim.Crystals.Any(c => c.IsRare), Is.False, "antes do andar 10, nem com 100% de chance");
            sim.SkipToCave(9);
            Assert.That(sim.Crystals.Any(c => c.IsRare), Is.False);

            sim.SkipToCave(10);
            Assert.That(sim.IsRareUnlocked, Is.True);
            Assert.That(sim.Crystals.All(c => c.IsRare), Is.True, "no andar 10 já nascem");
        }

        [Test]
        public void Rare_TheDebugShortcut_StillWorksBeforeFloor10()
        {
            var sim = Sim();
            sim.ForceNextRare = true;
            sim.SkipToCave(2);
            Assert.That(sim.Crystals.Count(c => c.IsRare), Is.EqualTo(1), "DBG → Bônus → Raro agora vale em qualquer andar");
        }

        // ── Para sempre: o recorde ─────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Milestones_SurviveTheRest_BackOnFloor1()
        {
            var sim = Sim(rareChance: 1);
            sim.SkipToCave(12);
            var next = Sim(rareChance: 1, state: sim.Permanents.NewRun(WithIntern()), permanent: sim.Permanents.State);
            Assert.That(next.State.Cave, Is.EqualTo(1));
            Assert.That(next.DeepestFloor, Is.EqualTo(12));
            Assert.That(next.IsAutoAdvanceUnlocked, Is.True, "o 5 vale para sempre");
            Assert.That(next.IsInternUnlocked, Is.True);
            Assert.That(next.IsRareUnlocked, Is.True, "o 10 também");
            Assert.That(next.Crystals.All(c => c.IsRare), Is.True, "o raro nasce já no andar 1");

            next.FillFloorBar();
            next.Tick(0);
            Assert.That(next.State.Cave, Is.EqualTo(2), "e o automático já desce do andar 1");
        }

        [Test]
        public void DebugRecord_SetsTheDeepestFloor_AndBelowTheCurrentFloor_GoesBackToIt()
        {
            var sim = Sim();
            sim.SkipToCave(7);
            Assert.That(sim.SetDeepestFloor(1), Is.True);
            Assert.That(sim.State.Cave, Is.EqualTo(1), "o recorde nunca fica acima de onde se está");
            Assert.That(sim.DeepestFloor, Is.EqualTo(1));
            Assert.That(sim.IsAutoAdvanceUnlocked, Is.False);
            Assert.That(sim.IsInternUnlocked, Is.False);

            Assert.That(sim.SetDeepestFloor(20), Is.True);
            Assert.That(sim.State.Cave, Is.EqualTo(1), "subir o recorde não muda o andar");
            Assert.That(sim.IsRareUnlocked, Is.True);
            Assert.That(OfflineMining.MaxSeconds(sim.Rules, sim.State, sim.Permanents), Is.EqualTo(6 * 3600), "o 20: 6 h");
            Assert.That(sim.SetDeepestFloor(20), Is.False, "já era");
        }
    }
}
