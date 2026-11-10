using System;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Os valores do jogo editáveis no Inspector (Arquitetura Técnica, D-T3): o loop de mineração (M2) e as melhorias (M3).
    /// Os padrões abaixo são os valores de protótipo (<c>Data/Balance_Prototipo</c>, D-29, D-35), das tabelas dos planos
    /// dos Marcos 2 e 3. Desde a E3b do M8 (D-130) o asset ativo é <c>Data/Balance_GDD_v7</c> (os números do GDD v7);
    /// <c>Data/Balance_GDD_v5</c>, <c>Data/Balance_GDD_v1</c> e o protótipo ficam para comparação. O Núcleo recebe cópias somente leitura (<see cref="ToRules"/>, <see cref="ToEconomy"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Miners Idle/Valores de mineração", fileName = "Balance_Prototipo")]
    public sealed class MiningRulesAsset : ScriptableObject
    {
        [Header("Fila de cristais (unidades da trilha; 1 un = 0,5 m na tela, D-58)")]
        [Tooltip("Cristais na fila no início, já na tela.")]
        [SerializeField] int initialCrystals = 10;
        [Tooltip("Distância do anão ao primeiro cristal inicial (D-60).")]
        [SerializeField] double firstCrystalDistance = 3;
        [Tooltip("Com este número na fila, para de nascer até alguém quebrar.")]
        [SerializeField] int maxCrystals = 12;
        [Tooltip("Segundos entre nascimentos.")]
        [SerializeField] double spawnInterval = 5;
        [Tooltip("A fila nunca fica com menos que isto: abaixo, nasce um cristal na hora (D-80). 0 desliga.")]
        [SerializeField] int minCrystals = 4;
        [Tooltip("Espaço mínimo entre dois cristais da fila (D-60).")]
        [SerializeField] double minCrystalSpacing = 2;
        [Tooltip("O cristal novo nasce no fim da fila, a pelo menos esta distância à frente do anão: fora da tela (D-60).")]
        [SerializeField] double spawnDistance = 12;

        [Header("Cristal")]
        [SerializeField] double crystalHealth = 10;
        [SerializeField] double crystalReward = 1;

        [Header("Cavernas (M8; o cristal da caverna c = base × crescimento^(c−1), D-92)")]
        [Tooltip("Número de cavernas. 1 = o jogo não passa de caverna.")]
        [SerializeField] int caveCount = 1;
        [Tooltip("A vida do cristal multiplica por isto a cada caverna.")]
        [SerializeField] double caveHealthGrowth = 1;
        [Tooltip("As lascas do cristal multiplicam por isto a cada caverna.")]
        [SerializeField] double caveRewardGrowth = 1;
        [Tooltip("Cristais a quebrar na caverna para liberar o Letrado dela (D-93).")]
        [SerializeField] int letradoBreaks = 100;
        [Tooltip("A parede da passagem tem esta vez a vida do cristal da caverna nova (D-98).")]
        [SerializeField] double wallHealthFactor = 2;
        [Tooltip("Níveis seguidos no mesmo bioma: 1 = um bioma por caverna (v5); 10 = um a cada 10 níveis (v7, D-129).")]
        [SerializeField] int levelsPerBiome = 1;
        [Tooltip("Offline (M9): até quantas horas fora contam para o estagiário; o resto se perde (GDD v7 §1.1, D-189: 6).")]
        [SerializeField] double offlineMaxHours = 6;

        [Header("Anão")]
        [Tooltip("Dano base por golpe, sem melhorias.")]
        [SerializeField] double damage = 1;
        [Tooltip("Segundos entre golpes, sem melhorias.")]
        [SerializeField] double hitInterval = 1;
        [Tooltip("Corrida, em unidades da trilha por segundo.")]
        [SerializeField] double walkSpeed = 4;
        [Tooltip("Distância do cristal em que o anão para e começa a bater.")]
        [SerializeField] double miningRange = 1;
        [Tooltip("Multiply: dano = base × Picareta.efeito^nível (até o GDD v5). LevelScaled (GDD v7, D-201): dano = base + " +
                 "Picareta.efeito × compras + taxa^compras. CaveScaled (a leitura da D-133, alternativa no DBG → Caverna): " +
                 "dano = base + Picareta.efeito × compras + taxa^(caverna − 1).")]
        [SerializeField] PickaxeFormula pickaxeFormula = PickaxeFormula.Multiply;
        [Tooltip("A taxa da Picareta (configurável): o dano ganha taxa^compras (LevelScaled) ou taxa^(caverna − 1) " +
                 "(CaveScaled). GDD v7: 1,2.")]
        [SerializeField] double pickaxeCaveGrowth = 1.2;

        [Header("Melhorias (custo da próxima compra = inicial × crescimento^nível, ou inicial + passo × nível na fórmula " +
                "Linear; nível = compras feitas)")]
        [Tooltip("Efeito: o dano multiplica por este valor a cada nível (Multiply) ou soma no Dano Base (LevelScaled e CaveScaled).")]
        [SerializeField] UpgradeLineValues pickaxe = new UpgradeLineValues(10, 4, 19, 2);
        [Tooltip("Efeito: segundos a menos no intervalo a cada nível.")]
        [SerializeField] UpgradeLineValues speed = new UpgradeLineValues(15, 3, 10, 0.05);
        [Tooltip("Efeito: fração a mais de lascas por cristal a cada nível (0,01 = 1%).")]
        [SerializeField] UpgradeLineValues mining = new UpgradeLineValues(10, 1.2, 100, 0.01);
        [Tooltip("Efeito: multiplicador do dano depois da compra (compra única).")]
        [SerializeField] UpgradeLineValues doublePickaxe = new UpgradeLineValues(500, 1, 1, 2);
        [Tooltip("Efeito: fração a mais de corrida a cada nível (0,1 = 10%). Limite 0 desliga a linha.")]
        [SerializeField] UpgradeLineValues boots = new UpgradeLineValues(10, 2, 10, 0.1);
        [Tooltip("Letrado (M8): o nível n leva à caverna n+1. Efeito: multiplicador do dano dali em diante. Limite 0 desliga.")]
        [SerializeField] UpgradeLineValues letrado = new UpgradeLineValues(100, 10, 0, 2);
        [Tooltip("Chance de Crítico (GDD v7, D-136). Efeito: chance a mais por nível, no golpe e no toque. Limite 0 desliga.")]
        [SerializeField] UpgradeLineValues critical = new UpgradeLineValues(50, 2, 0, 0.05);
        [Tooltip("Intervalo mínimo entre golpes, em segundos (o do anão e o do estagiário, D-165).")]
        [SerializeField] double minHitInterval = 0.5;

        [Header("Estagiário (M7, GDD v7 §4.1, §5.4; limite 0 desliga: sem estagiário, D-172)")]
        [Tooltip("Contratar o estagiário: compra única (GDD v7: 500). Sem compras dele, bate o efeito da Picareta dele no intervalo base (D-205).")]
        [SerializeField] UpgradeLineValues intern = new UpgradeLineValues(500, 1, 0, 1);
        [Tooltip("Picareta do estagiário. Efeito: o dano dele no nível 1, já contratado e sem compra (GDD v7: 5, D-205); cada compra multiplica pelo crescimento do dano.")]
        [SerializeField] UpgradeLineValues internPickaxe = new UpgradeLineValues(50, 4, 0, 5);
        [Tooltip("O \"2\" de 5 × 2^(nível−1): quanto o dano da Picareta do estagiário multiplica a cada compra.")]
        [SerializeField] double internPickaxeDamageGrowth = 2;
        [Tooltip("Velocidade do estagiário. Efeito: segundos a menos no intervalo dele por nível, a partir do intervalo base (GDD v7: 0,1).")]
        [SerializeField] UpgradeLineValues internSpeed = new UpgradeLineValues(50, 5, 0, 0.1);
        [Tooltip("Mineração do estagiário. Efeito: fração a mais das lascas offline por nível (GDD v7: 1 = +100%; só no offline, D-166).")]
        [SerializeField] UpgradeLineValues internMining = new UpgradeLineValues(50, 1.2, 0, 1);

        [Header("Golpe e toque (valores do GDD: 5%, ×2, 10%, 1 s)")]
        [Tooltip("Chance base de crítico do golpe automático (0–1). GDD v7: 0 (D-136).")]
        [SerializeField] double critChance = 0.05;
        [Tooltip("Chance base de crítico do toque (0–1). GDD v7: 0,05 (D-136); até o v5, igual à do golpe.")]
        [SerializeField] double tapCritChance = 0.05;
        [Tooltip("Quanto o dano multiplica no crítico.")]
        [SerializeField] double critMultiplier = 2;
        [Tooltip("Fração de um golpe que o toque dá (0,1 = 10%).")]
        [SerializeField] double tapFraction = 0.1;
        [Tooltip("Segundos sem toque até o combo zerar.")]
        [SerializeField] double comboWindow = 1;
        [Tooltip("Toques que valem por segundo, somando os dedos; acima disto o toque não dá dano, crítico nem combo (D-119). 0 = sem teto.")]
        [SerializeField] int maxTapsPerSecond = 15;
        [Tooltip("O toque só tira vida de um alvo a até esta distância do anão, em un: o que já está na tela (D-121). " +
                 "3,9 un ≈ 1,95 m: na câmera F1, uma tela 9:16 mostra 1,99 m de chão à frente do anão (as mais altas, mais).")]
        [SerializeField] double tapReach = 3.9;

        [Tooltip("Quando sai o primeiro golpe num cristal (D-124). Wait: um intervalo depois da chegada (o jogo até 2026-11-01); " +
                 "Charge: o relógio do golpe enche também correndo; Immediate: golpe no instante da chegada.")]
        [SerializeField] ArrivalStrike arrivalStrike = ArrivalStrike.Immediate; // escolha do Bruno no teste (D-124)
        [Tooltip("A volta da picareta depois do golpe, em segundos (D-126): se o toque quebra o cristal antes disso, o anão " +
                 "termina o golpe parado e só então corre. Nunca passa do intervalo. 0,72 = os 9 quadros de 80 ms da folha (D-75); 0 desliga.")]
        [SerializeField] double swingRecovery = 0.72;

        [Header("Dia e melhorias permanentes (Marco 8b; custo em lascas vermelhas, D-158)")]
        [Tooltip("Nível a partir do qual o Descansar aparece e as lascas vermelhas começam (D-138, D-152). GDD v7: 10. 0 desliga.")]
        [SerializeField] int restLevel;
        [Tooltip("Os níveis múltiplos disto dão o marco de lascas vermelhas (GDD v7: 10).")]
        [SerializeField] int redShardMilestoneStep = 10;
        [Tooltip("Lascas vermelhas do marco (GDD v7: 5, nos níveis 10, 20, 30 e 40).")]
        [SerializeField] double redShardMilestone = 5;
        [Tooltip("A partir deste nível o marco vale o grande (GDD v7: 50).")]
        [SerializeField] int redShardBigMilestoneFrom = 50;
        [Tooltip("Lascas vermelhas do marco grande (GDD v7: 10, nos níveis 50 a 100).")]
        [SerializeField] double redShardBigMilestone = 10;
        [Tooltip("Lascas vermelhas dos outros níveis a partir do primeiro (GDD v7: 1).")]
        [SerializeField] double redShardPerLevel = 1;
        [Tooltip("Memória. Efeito: cristais a menos na trava do Mapa por nível (GDD v7: 10).")]
        [SerializeField] UpgradeLineValues memory = new UpgradeLineValues(10, 2, 0, 10, CostFormula.OffsetExponential);
        [Tooltip("Sangue da Montanha. Efeito: fração a mais de dano por nível, no dano final (GDD v7: 0,1).")]
        [SerializeField] UpgradeLineValues mountainBlood = new UpgradeLineValues(10, 2, 0, 0.1, CostFormula.OffsetExponential);
        [Tooltip("Coração Ardente. Efeito: fração a mais de lascas por nível (GDD v7: 0,5; 20 níveis = o teto de +1000%).")]
        [SerializeField] UpgradeLineValues burningHeart = new UpgradeLineValues(10, 2, 0, 0.5, CostFormula.OffsetExponential);
        [Tooltip("Dedos Nervosos. Efeito: chance de crítico a mais por nível, no golpe e no toque (GDD v7: 0,05).")]
        [SerializeField] UpgradeLineValues nervousFingers = new UpgradeLineValues(10, 2, 0, 0.05, CostFormula.OffsetExponential);
        [Tooltip("Alma dos Anões. Efeito: níveis de Picareta com que a run começa, por nível (GDD v7: 1).")]
        [SerializeField] UpgradeLineValues dwarfSoul = new UpgradeLineValues(10, 2, 0, 1, CostFormula.OffsetExponential);
        [Tooltip("Treinar Estagiário (M7). Efeito: fração a mais de dano do estagiário por nível (GDD v7: 0,1).")]
        [SerializeField] UpgradeLineValues internTraining = new UpgradeLineValues(10, 2, 0, 0.1, CostFormula.OffsetExponential);
        [Tooltip("Promoção do Estagiário (M7): compra única; a run já começa com ele. Só com o Treinar no limite (GDD v7: 50).")]
        [SerializeField] UpgradeLineValues internPromotion = new UpgradeLineValues(50, 1, 0, 1);

        /// <summary>A fórmula da Picareta deste balanço: a taxa e o incremento do DBG → Cálculos não valem no Multiply (v5).</summary>
        public PickaxeFormula PickaxeFormula => pickaxeFormula;

        /// <summary>A Picareta deste balanço tem taxa e incremento (a do GDD v7, pela compra ou pela caverna).</summary>
        public bool PickaxeTunable => pickaxeFormula != PickaxeFormula.Multiply;

        /// <summary>A Picareta deste balanço é a do GDD v7 pela compra (D-201): só nela o DBG → Caverna troca para a caverna.</summary>
        public bool PickaxeSwitchable => pickaxeFormula == PickaxeFormula.LevelScaled;

        /// <summary>A taxa da Picareta deste balanço (GDD v7: 1,2).</summary>
        public double PickaxeCaveGrowth => pickaxeCaveGrowth;

        /// <summary>O incremento da Picareta deste balanço: Dano Base a mais por compra (GDD v7: 0, D-202; era 1 na D-133).</summary>
        public double PickaxeStep => pickaxe.Effect;

        /// <param name="pickaxeCaveGrowth">A taxa trocada no DBG → Cálculos (E3b-3); <c>null</c> = a do asset.</param>
        /// <param name="pickaxeByCave">
        /// O botão "Picareta" do DBG → Caverna (D-201): a Picareta pela caverna (a leitura da D-133) no lugar da pela compra.
        /// Só vale num balanço com a Picareta pela compra.
        /// </param>
        public MiningRules ToRules(double? pickaxeCaveGrowth = null, bool pickaxeByCave = false) => new MiningRules(
            initialCrystals, firstCrystalDistance, maxCrystals, spawnInterval,
            minCrystalSpacing, spawnDistance,
            crystalHealth, crystalReward,
            damage, hitInterval,
            walkSpeed, miningRange,
            minCrystals,
            caveCount, caveHealthGrowth, caveRewardGrowth, letradoBreaks, wallHealthFactor,
            tapReach, arrivalStrike, swingRecovery,
            pickaxeByCave && PickaxeSwitchable ? PickaxeFormula.CaveScaled : pickaxeFormula,
            PickaxeTunable ? pickaxeCaveGrowth ?? this.pickaxeCaveGrowth : this.pickaxeCaveGrowth,
            levelsPerBiome, offlineMaxHours * 3600);

        /// <summary>O descanso, as lascas vermelhas e as permanentes deste balanço (Marco 8b). Sem descanso no asset, nada disso.</summary>
        public PermanentRules ToPermanents()
        {
            if (restLevel <= 0) return PermanentRules.None;
            var table = new RedShardTable(restLevel, redShardMilestoneStep, redShardMilestone, redShardBigMilestoneFrom,
                                          redShardBigMilestone, redShardPerLevel);
            // Na ordem do PermanentId.
            return new PermanentRules(restLevel, table, memory.ToDefinition(), mountainBlood.ToDefinition(),
                                      burningHeart.ToDefinition(), nervousFingers.ToDefinition(), dwarfSoul.ToDefinition(),
                                      internTraining.ToDefinition(), internPromotion.ToDefinition());
        }

        public StrikeRules ToStrike() =>
            new StrikeRules(critChance, critMultiplier, tapFraction, comboWindow, maxTapsPerSecond, tapCritChance);

        /// <param name="pickaxeStep">
        /// O incremento trocado no DBG → Cálculos (E3b-3); <c>null</c> = o do asset. Não vale no Multiply (v5). Na fórmula
        /// Linear (a "2 × Dano Base" da D-133), o passo do custo cresce na mesma proporção do incremento; na DamageScaled
        /// (D-202), o custo é o multiplicador × o dano da Picareta, então já acompanha o incremento.
        /// </param>
        /// <param name="pickaxeCaveGrowth">
        /// A taxa trocada no DBG → Cálculos; <c>null</c> = a do asset. Só entra no custo da DamageScaled (D-202).
        /// </param>
        public EconomyRules ToEconomy(double? pickaxeStep = null, double? pickaxeCaveGrowth = null) => new EconomyRules(
            PickaxeLine(pickaxeStep, pickaxeCaveGrowth), speed.ToDefinition(), mining.ToDefinition(),
            doublePickaxe.ToDefinition(), boots.ToDefinition(), minHitInterval, letrado.ToDefinition(),
            critical.ToDefinition(), intern.ToDefinition(), internPickaxe.ToDefinition(), internSpeed.ToDefinition(),
            internMining.ToDefinition(), internPickaxeDamageGrowth);

        UpgradeDefinition PickaxeLine(double? step, double? growth)
        {
            var line = pickaxe.ToDefinition();
            if (line.CostFormula == CostFormula.DamageScaled)
            {
                // O custo é o multiplicador × o dano da Picareta pela compra (D-202): o Dano Base, o incremento e a taxa
                // são os do dano, nunca uma cópia no asset. O crescimento salvo na linha não vale.
                bool tuned = PickaxeTunable;
                return new UpgradeDefinition(line.BaseCost,
                    tuned ? growth ?? pickaxeCaveGrowth : pickaxeCaveGrowth, line.MaxLevel,
                    tuned ? step ?? line.Effect : line.Effect, CostFormula.DamageScaled, damage);
            }
            if (!step.HasValue || !PickaxeTunable || step.Value == line.Effect) return line;

            double costGrowth = line.CostFormula == CostFormula.Linear && line.Effect != 0
                ? line.CostGrowth * step.Value / line.Effect
                : line.CostGrowth;
            return new UpgradeDefinition(line.BaseCost, costGrowth, line.MaxLevel, step.Value, line.CostFormula);
        }
    }

    /// <summary>Uma linha de melhoria no Inspector (vira uma <see cref="UpgradeDefinition"/> do Núcleo).</summary>
    [Serializable]
    public sealed class UpgradeLineValues
    {
        [Tooltip("Exponential: inicial × crescimento^nível. Linear: inicial + passo × nível (o passo é o crescimento). " +
                 "OffsetExponential: inicial + crescimento^nível (as permanentes do GDD v7). DamageScaled (só a Picareta, D-202): " +
                 "inicial × o dano da Picareta (Dano Base + efeito × nível + taxa^nível); o crescimento salvo não vale.")]
        [SerializeField] CostFormula costFormula;
        [Tooltip("Custo da 1ª compra.")]
        [SerializeField] double baseCost;
        [Tooltip("Quanto o custo multiplica a cada compra (na Linear, quanto soma).")]
        [SerializeField] double costGrowth;
        [Tooltip("Número máximo de compras (0 desliga a linha).")]
        [SerializeField] int maxLevel;
        [SerializeField] double effect;

        /// <summary>Para o serializador do Unity.</summary>
        public UpgradeLineValues() { }

        public UpgradeLineValues(double baseCost, double costGrowth, int maxLevel, double effect,
                                 CostFormula costFormula = CostFormula.Exponential)
        {
            this.costFormula = costFormula;
            this.baseCost = baseCost;
            this.costGrowth = costGrowth;
            this.maxLevel = maxLevel;
            this.effect = effect;
        }

        public double Effect => effect;

        public UpgradeDefinition ToDefinition() => new UpgradeDefinition(baseCost, costGrowth, maxLevel, effect, costFormula);
    }
}
