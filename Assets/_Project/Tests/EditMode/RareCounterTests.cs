using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9e E1: o raro do GDD v8 (§3.4; D-243, D-283, D-285, D-286). Um contador de 60 s de run (parado no descanso e na
    /// transição de andar, e só a partir do andar 10) faz raro o próximo cristal que nasce, e recomeça; nunca dois raros na
    /// fila. As cartas do v8 (Dano ×2, Lascas ×2, +10% de crítico, Dano de Crítico ×2) valem 30 s; repetir reinicia. O tier
    /// 2 (×4, ×4, +20%, ×4) vem do Raro Dourado (500 vermelhas, a partir do andar 10) ou do atalho do DBG.
    /// </summary>
    public class RareCounterTests
    {
        const string GddV8Path = "Assets/_Project/Data/Balance_GDD_v8.asset";

        static readonly RareCardRule[] Tier1 =
        {
            new RareCardRule(RareCard.Damage, 2, 30),
            new RareCardRule(RareCard.Shards, 2, 30),
            new RareCardRule(RareCard.Critical, 0.10, 30),
            new RareCardRule(RareCard.CritDamage, 2, 30),
        };

        static readonly RareCardRule[] Tier2 =
        {
            new RareCardRule(RareCard.Damage, 4, 30),
            new RareCardRule(RareCard.Shards, 4, 30),
            new RareCardRule(RareCard.Critical, 0.20, 30),
            new RareCardRule(RareCard.CritDamage, 4, 30),
        };

        /// <summary>Cristais que não quebram sozinhos e não nascem pelo relógio: só o <c>SpawnCrystalNow</c> faz nascer.</summary>
        static MiningRules Rules(int initial = 0, double health = 1e9) =>
            new MiningRules(
                initialCrystals: initial, firstCrystalDistance: 3, maxCrystals: 40, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: health, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4,
                miningRange: 1, caveCount: 100, caveHealthGrowth: 1, floorBreaks: 1, rareFloor: 10);

        static StrikeRules Strike(double critChance = 0) =>
            new StrikeRules(critChance, 1.5, 0.33, 1, rare: new RareRules(0, 0, 30, Tier1, interval: 60, tier2Cards: Tier2));

        static readonly UpgradeDefinition Off = new UpgradeDefinition(0, 1, 0, 0);

        /// <summary>Só o Raro Dourado ligado: 500, compra única, a partir do andar 10.</summary>
        static PermanentRules Permanents() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1), 0, 10,
            Off, Off, Off, Off, Off, Off, Off, Off, Off, new UpgradeDefinition(500, 1, 1, 1));

        static MiningSimulation Sim(MiningRules rules = null, StrikeRules strike = null, PermanentState permanent = null) =>
            new MiningSimulation(rules ?? Rules(), new SeededRandom(3), null, null, strike ?? Strike(),
                                 new Permanents(Permanents(), permanent ?? new PermanentState()), new SeededRandom(5));

        /// <summary>Um andar com o raro: o 10, direto (sem transição).</summary>
        static MiningSimulation AtFloor10(MiningRules rules = null, StrikeRules strike = null)
        {
            var sim = Sim(rules, strike);
            sim.SkipToCave(10);
            return sim;
        }

        static ActiveBonus Card(MiningSimulation sim, RareCard card) =>
            sim.Bonuses.All.Single(b => b.Source == BonusSource.Rare && b.Tier == (int)card);

        // ── O contador (D-283) ──────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void TheCounter_DoesNotExistBeforeFloor10()
        {
            var sim = Sim();
            Assert.That(sim.IsRareCounting, Is.False);
            sim.Tick(300);
            Assert.That(sim.State.RareTimer, Is.EqualTo(0), "antes do andar 10, o contador não anda");
            Assert.That(sim.RareCountdown, Is.EqualTo(double.PositiveInfinity));
            Assert.That(sim.SpawnCrystalNow().IsRare, Is.False);

            sim.SkipToCave(10);
            Assert.That(sim.IsRareCounting, Is.True);
            Assert.That(sim.RareCountdown, Is.EqualTo(60).Within(1e-9), "no andar 10 começa do zero");
        }

        [Test]
        public void AtZero_TheNextCrystalBornIsRare_AndTheCounterStartsOver()
        {
            var sim = AtFloor10();
            sim.Tick(59);
            Assert.That(sim.RareCountdown, Is.EqualTo(1).Within(1e-6));
            Assert.That(sim.SpawnCrystalNow().IsRare, Is.False, "falta 1 s");

            sim.Tick(1);
            Assert.That(sim.RareCountdown, Is.EqualTo(0).Within(1e-6));
            sim.Tick(30);
            Assert.That(sim.RareCountdown, Is.EqualTo(0), "cheio, espera o próximo cristal nascer");

            Assert.That(sim.SpawnCrystalNow().IsRare, Is.True, "no zero, o próximo que nasce é raro");
            Assert.That(sim.State.RareTimer, Is.EqualTo(0), "e o contador recomeça");
            Assert.That(sim.RareCountdown, Is.EqualTo(60).Within(1e-9));
            Assert.That(sim.SpawnCrystalNow().IsRare, Is.False, "só aquele");
        }

        [Test]
        public void NeverTwoRaresOnTheTrack()
        {
            var sim = AtFloor10(Rules(initial: 1));
            sim.Tick(60);
            var rare = sim.SpawnCrystalNow();
            Assert.That(rare.IsRare, Is.True);

            sim.Tick(60);
            Assert.That(sim.RareCountdown, Is.EqualTo(0).Within(1e-6));
            Assert.That(sim.SpawnCrystalNow().IsRare, Is.False, "um raro ainda na fila: o próximo espera");
            Assert.That(sim.Crystals.Count(c => c.IsRare), Is.EqualTo(1));

            // Quebra até o raro sair da fila; a janela abre, e o jogo segue.
            while (sim.Crystals.Contains(rare))
            {
                sim.Tick(0); // o alvo seguinte entra no laço de eventos
                Assert.That(sim.BreakTarget(), Is.True, "um cristal na mira");
            }
            if (sim.RareWindow.IsChoosing) sim.ChooseRareCard(0);
            Assert.That(sim.SpawnCrystalNow().IsRare, Is.True, "fora o raro, o contador cheio vale no próximo");
        }

        [Test]
        public void TheCounter_StopsWhileResting_AndInTheFloorTransition()
        {
            var sim = AtFloor10();
            sim.Tick(20);
            sim.IsResting = true;
            sim.Tick(100);
            Assert.That(sim.State.RareTimer, Is.EqualTo(20).Within(1e-9), "descansando, parado (D-265)");
            sim.IsResting = false;

            sim.FloorTransitionSeconds = 10;
            sim.SkipToCave(11);
            Assert.That(sim.IsInFloorTransition, Is.True);
            sim.Tick(10);
            Assert.That(sim.IsInFloorTransition, Is.False);
            Assert.That(sim.State.RareTimer, Is.EqualTo(20).Within(1e-9), "na transição, só o relógio do jogo anda");

            sim.Tick(5);
            Assert.That(sim.State.RareTimer, Is.EqualTo(25).Within(1e-9));
        }

        [Test]
        public void TheDebugCounter_FiveSeconds_AndBack()
        {
            var sim = AtFloor10();
            sim.RareIntervalOverride = 5;
            Assert.That(sim.RareInterval, Is.EqualTo(5));
            sim.Tick(5);
            Assert.That(sim.SpawnCrystalNow().IsRare, Is.True, "Contador 5 s");

            sim.RareIntervalOverride = 0;
            Assert.That(sim.RareInterval, Is.EqualTo(60), "desligado, o das regras");
            sim.Tick(5);
            Assert.That(sim.RareCountdown, Is.EqualTo(55).Within(1e-6));
        }

        [Test]
        public void ForceNextRare_AlsoRestartsTheCounter()
        {
            var sim = AtFloor10();
            sim.Tick(40);
            sim.ForceNextRare = true;
            Assert.That(sim.SpawnCrystalNow().IsRare, Is.True);
            Assert.That(sim.State.RareTimer, Is.EqualTo(0), "um raro nasceu: o contador recomeça");
        }

        [Test]
        public void TheCounter_RestartsWithTheRun_AndRestoresFromTheSave()
        {
            Assert.That(new GameState().RareTimer, Is.EqualTo(0));
            var restored = GameState.Restore(0, 0, 0, _ => 0, EconomyRules.None(), rareTimer: 42.5);
            Assert.That(restored.RareTimer, Is.EqualTo(42.5));
            Assert.That(GameState.Restore(0, 0, 0, _ => 0, EconomyRules.None(), rareTimer: -3).RareTimer, Is.EqualTo(0));
            Assert.That(GameState.Restore(0, 0, 0, _ => 0, EconomyRules.None(), rareTimer: double.NaN).RareTimer, Is.EqualTo(0));
        }

        // ── As cartas do v8 (D-243) ─────────────────────────────────────────────────────────────────────────────

        [Test]
        public void TheV8Cards_Tier1_ThirtySeconds()
        {
            var sim = AtFloor10();
            foreach (var rule in Tier1) Assert.That(sim.GrantRareCard(rule.Card), Is.True);
            Assert.That(sim.GrantRareCard(RareCard.Speed), Is.False, "o golpe rápido saiu no v8");

            Assert.That(Card(sim, RareCard.Damage).Value, Is.EqualTo(2));
            Assert.That(Card(sim, RareCard.Shards).Value, Is.EqualTo(2));
            Assert.That(Card(sim, RareCard.Critical).Value, Is.EqualTo(0.10));
            Assert.That(Card(sim, RareCard.CritDamage).Value, Is.EqualTo(2));
            Assert.That(sim.Bonuses.All.All(b => b.SecondsLeft == 30), Is.True, "30 s cada");
            Assert.That(sim.Bonuses.CritDamageMultiplier, Is.EqualTo(2));
            Assert.That(sim.Bonuses.CritChance, Is.EqualTo(0.10).Within(1e-12));

            sim.Tick(30);
            Assert.That(sim.Bonuses.Count, Is.EqualTo(0), "acabam juntas");
        }

        [Test]
        public void RepeatingACard_RestartsTheTime_WithoutAddingTheEffect()
        {
            var sim = AtFloor10();
            sim.GrantRareCard(RareCard.Shards);
            sim.Tick(20);
            Assert.That(Card(sim, RareCard.Shards).SecondsLeft, Is.EqualTo(10).Within(1e-9));
            sim.GrantRareCard(RareCard.Shards);
            Assert.That(Card(sim, RareCard.Shards).SecondsLeft, Is.EqualTo(30), "reinicia, não soma");
            Assert.That(sim.Bonuses.ShardsMultiplier, Is.EqualTo(2), "e o efeito não soma");
        }

        [Test]
        public void CritDamageCard_DoublesTheCriticalMultiplier_OnlyForTheDwarf()
        {
            double FirstHit(bool withCard)
            {
                var sim = AtFloor10(Rules(initial: 1));
                sim.ForceCritical = true;
                if (withCard) sim.GrantRareCard(RareCard.CritDamage);
                var hits = new List<MiningHit>();
                sim.HitLanded += hits.Add;
                sim.Tick(3);
                Assert.That(hits, Is.Not.Empty);
                Assert.That(hits[0].IsCritical, Is.True);
                return hits[0].Damage;
            }

            double plain = FirstHit(false);
            Assert.That(plain, Is.GreaterThan(0));
            Assert.That(FirstHit(true), Is.EqualTo(2 * plain).Within(1e-9), "a carta dobra: ×3");
            Assert.That(AtFloor10().Bonuses.InternDamageMultiplier, Is.EqualTo(1), "nada para o estagiário (D-167)");
        }

        // ── O tier 2 (D-286) ────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Tier2_FromTheDebug_GivesTheTier2Values()
        {
            var sim = AtFloor10();
            Assert.That(sim.RareTier, Is.EqualTo(1));
            sim.ForceRareTier2 = true;
            Assert.That(sim.RareTier, Is.EqualTo(2));
            foreach (var rule in Tier1) sim.GrantRareCard(rule.Card);
            Assert.That(Card(sim, RareCard.Damage).Value, Is.EqualTo(4));
            Assert.That(Card(sim, RareCard.Shards).Value, Is.EqualTo(4));
            Assert.That(Card(sim, RareCard.Critical).Value, Is.EqualTo(0.20));
            Assert.That(Card(sim, RareCard.CritDamage).Value, Is.EqualTo(4));

            sim.ForceRareTier2 = false;
            sim.GrantRareCard(RareCard.Damage);
            Assert.That(Card(sim, RareCard.Damage).Value, Is.EqualTo(2), "a mesma carta no tier 1 troca o valor e o tempo");
        }

        [Test]
        public void GoldenRare_LockedBeforeFloor10_ThenBoughtWith500_GivesTier2ForGood()
        {
            var sim = Sim();
            sim.AddRedShards(600);
            sim.IsResting = true;
            Assert.That(sim.Permanents.IsLocked(PermanentId.GoldenRare), Is.True);
            Assert.That(sim.Permanents.RequiredLevel(PermanentId.GoldenRare), Is.EqualTo(10), "o andar que ele pede");
            Assert.That(sim.PurchasePermanent(PermanentId.GoldenRare), Is.EqualTo(PurchaseResult.Locked));

            sim.SetDeepestFloor(10);
            Assert.That(sim.Permanents.IsLocked(PermanentId.GoldenRare), Is.False);
            Assert.That(sim.Permanents.NextCost(PermanentId.GoldenRare), Is.EqualTo(500));
            Assert.That(sim.PurchasePermanent(PermanentId.GoldenRare), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.Permanents.State.RedShards, Is.EqualTo(100));
            Assert.That(sim.PurchasePermanent(PermanentId.GoldenRare), Is.EqualTo(PurchaseResult.MaxLevel), "compra única");
            Assert.That(sim.RareTier, Is.EqualTo(2));

            // Na run seguinte, continua.
            var next = Sim(permanent: sim.Permanents.State);
            Assert.That(next.RareTier, Is.EqualTo(2));
        }

        [Test]
        public void WithoutTier2InTheBalance_TheTierStays1()
        {
            var sim = AtFloor10(strike: new StrikeRules(0, 1.5, 0.33, 1, rare: new RareRules(0, 0, 30, Tier1, interval: 60)));
            sim.ForceRareTier2 = true;
            Assert.That(sim.RareTier, Is.EqualTo(1));
            sim.GrantRareCard(RareCard.Damage);
            Assert.That(Card(sim, RareCard.Damage).Value, Is.EqualTo(2));
        }

        [Test]
        public void Rules_Tier2CardsMustBeInTier1()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new RareRules(0, 0, 30, new[] { Tier1[0], Tier1[1] }, 60, new[] { Tier2[2] }));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new RareRules(0, 0, 30, Tier1, -1));
            Assert.That(new RareRules(0.15, 0.02, 30, Tier1).UsesCounter, Is.False, "sem intervalo, pela chance (o v7)");
        }

        /// <summary>O {0} dos textos das cartas no catálogo: o efeito no tier de agora.</summary>
        [Test]
        public void TheEffectText_ForTheCardTexts()
        {
            Assert.That(NumberFormatter.FormatEffect(BonusKind.DamageMultiplier, 2), Is.EqualTo("×2"));
            Assert.That(NumberFormatter.FormatEffect(BonusKind.ShardsMultiplier, 4), Is.EqualTo("×4"));
            Assert.That(NumberFormatter.FormatEffect(BonusKind.CritDamageMultiplier, 2), Is.EqualTo("×2"));
            Assert.That(NumberFormatter.FormatEffect(BonusKind.CritChance, 0.10), Is.EqualTo("+10%"));
            Assert.That(NumberFormatter.FormatEffect(BonusKind.CritChance, 0.20), Is.EqualTo("+20%"));
        }

        // ── O balanço do v8 ─────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void TheV8Balance_HasTheCounter_TheCards_AndTheGoldenRare()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV8Path);
            Assert.That(asset, Is.Not.Null, "rode o setup do projeto");
            var rare = asset.ToStrike().Rare;
            Assert.That(rare.Interval, Is.EqualTo(60), "D-283");
            Assert.That(rare.Cards.Select(c => c.Card),
                        Is.EqualTo(new[] { RareCard.Damage, RareCard.Shards, RareCard.Critical, RareCard.CritDamage }));
            Assert.That(rare.Cards.Select(c => c.Value), Is.EqualTo(new[] { 2, 2, 0.10, 2 }));
            Assert.That(rare.Cards.All(c => c.Seconds == 30), Is.True, "D-243: 30 s");
            Assert.That(rare.HasTier2, Is.True);
            Assert.That(rare.Cards.Select(c => rare.Get(c.Card, 2).Value), Is.EqualTo(new[] { 4, 4, 0.20, 4 }));

            var permanents = asset.ToPermanents();
            var golden = permanents.Get(PermanentId.GoldenRare);
            Assert.That(golden.MaxLevel, Is.EqualTo(1));
            Assert.That(golden.CostAt(0), Is.EqualTo(500), "D-286");
            Assert.That(permanents.GoldenRareFloor, Is.EqualTo(10));
        }
    }
}
