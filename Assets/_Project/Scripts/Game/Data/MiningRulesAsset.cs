using System;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Os valores do jogo editáveis no Inspector (Arquitetura Técnica, D-T3): o loop de mineração (M2) e as melhorias (M3).
    /// O asset ativo é <c>Data/Balance_Prototipo</c>: valores de protótipo (D-29, D-35), não balanceamento; os padrões
    /// abaixo são as tabelas dos planos dos Marcos 2 e 3. <c>Data/Balance_GDD_v1</c> guarda os números literais do GDD
    /// para comparação. O Núcleo recebe cópias somente leitura (<see cref="ToRules"/>, <see cref="ToEconomy"/>).
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
        [Tooltip("Espaço mínimo entre dois cristais da fila (D-60).")]
        [SerializeField] double minCrystalSpacing = 2;
        [Tooltip("O cristal novo nasce no fim da fila, a pelo menos esta distância à frente do anão: fora da tela (D-60).")]
        [SerializeField] double spawnDistance = 12;

        [Header("Cristal")]
        [SerializeField] double crystalHealth = 10;
        [SerializeField] double crystalReward = 1;

        [Header("Anão")]
        [Tooltip("Dano base por golpe, sem melhorias.")]
        [SerializeField] double damage = 1;
        [Tooltip("Segundos entre golpes, sem melhorias.")]
        [SerializeField] double hitInterval = 1;
        [Tooltip("Corrida, em unidades da trilha por segundo.")]
        [SerializeField] double walkSpeed = 4;
        [Tooltip("Distância do cristal em que o anão para e começa a bater.")]
        [SerializeField] double miningRange = 1;

        [Header("Melhorias (custo da próxima compra = inicial × crescimento^nível; nível = compras feitas)")]
        [Tooltip("Efeito: o dano multiplica por este valor a cada nível.")]
        [SerializeField] UpgradeLineValues pickaxe = new UpgradeLineValues(10, 4, 19, 2);
        [Tooltip("Efeito: segundos a menos no intervalo a cada nível.")]
        [SerializeField] UpgradeLineValues speed = new UpgradeLineValues(15, 3, 10, 0.05);
        [Tooltip("Efeito: fração a mais de lascas por cristal a cada nível (0,01 = 1%).")]
        [SerializeField] UpgradeLineValues mining = new UpgradeLineValues(10, 1.2, 100, 0.01);
        [Tooltip("Efeito: multiplicador do dano depois da compra (compra única).")]
        [SerializeField] UpgradeLineValues doublePickaxe = new UpgradeLineValues(500, 1, 1, 2);
        [Tooltip("Efeito: fração a mais de corrida a cada nível (0,1 = 10%). Limite 0 desliga a linha.")]
        [SerializeField] UpgradeLineValues boots = new UpgradeLineValues(10, 2, 10, 0.1);
        [Tooltip("Intervalo mínimo entre golpes, em segundos.")]
        [SerializeField] double minHitInterval = 0.5;

        [Header("Golpe e toque (valores do GDD: 5%, ×2, 10%, 1 s)")]
        [Tooltip("Chance de crítico por golpe e por toque (0–1).")]
        [SerializeField] double critChance = 0.05;
        [Tooltip("Quanto o dano multiplica no crítico.")]
        [SerializeField] double critMultiplier = 2;
        [Tooltip("Fração de um golpe que o toque dá (0,1 = 10%).")]
        [SerializeField] double tapFraction = 0.1;
        [Tooltip("Segundos sem toque até o combo zerar.")]
        [SerializeField] double comboWindow = 1;

        public MiningRules ToRules() => new MiningRules(
            initialCrystals, firstCrystalDistance, maxCrystals, spawnInterval,
            minCrystalSpacing, spawnDistance,
            crystalHealth, crystalReward,
            damage, hitInterval,
            walkSpeed, miningRange);

        public StrikeRules ToStrike() => new StrikeRules(critChance, critMultiplier, tapFraction, comboWindow);

        public EconomyRules ToEconomy() => new EconomyRules(
            pickaxe.ToDefinition(), speed.ToDefinition(), mining.ToDefinition(),
            doublePickaxe.ToDefinition(), boots.ToDefinition(), minHitInterval);
    }

    /// <summary>Uma linha de melhoria no Inspector (vira uma <see cref="UpgradeDefinition"/> do Núcleo).</summary>
    [Serializable]
    public sealed class UpgradeLineValues
    {
        [Tooltip("Custo da 1ª compra.")]
        [SerializeField] double baseCost;
        [Tooltip("Quanto o custo multiplica a cada compra.")]
        [SerializeField] double costGrowth;
        [Tooltip("Número máximo de compras (0 desliga a linha).")]
        [SerializeField] int maxLevel;
        [SerializeField] double effect;

        /// <summary>Para o serializador do Unity.</summary>
        public UpgradeLineValues() { }

        public UpgradeLineValues(double baseCost, double costGrowth, int maxLevel, double effect)
        {
            this.baseCost = baseCost;
            this.costGrowth = costGrowth;
            this.maxLevel = maxLevel;
            this.effect = effect;
        }

        public UpgradeDefinition ToDefinition() => new UpgradeDefinition(baseCost, costGrowth, maxLevel, effect);
    }
}
