using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Os valores do loop de mineração editáveis no Inspector (Arquitetura Técnica, D-T3). O asset do M2 é
    /// <c>Data/Balance_Prototipo</c>: valores de protótipo (D-29), não balanceamento. Os padrões abaixo são a tabela do
    /// plano do Marco 2. O Núcleo recebe uma cópia somente leitura (<see cref="ToRules"/>); nada aqui muda em jogo.
    /// </summary>
    [CreateAssetMenu(menuName = "Miners Idle/Valores de mineração", fileName = "Balance_Prototipo")]
    public sealed class MiningRulesAsset : ScriptableObject
    {
        [Header("Mapa (unidades de mundo; 1 = 16 px)")]
        [SerializeField] Vector2 mapMin = new Vector2(-7.5f, -11f);
        [SerializeField] Vector2 mapMax = new Vector2(7.5f, 10f);
        [SerializeField] Vector2 minerStart = new Vector2(0f, -0.5f);

        [Header("Nascimento de cristais")]
        [Tooltip("Cristais na tela no início (lote 1).")]
        [SerializeField] int initialCrystals = 10;
        [Tooltip("Com este número na tela, para de nascer até alguém quebrar.")]
        [SerializeField] int maxCrystals = 12;
        [Tooltip("Segundos entre nascimentos.")]
        [SerializeField] double spawnInterval = 5;
        [SerializeField] double minCrystalSpacing = 2;
        [SerializeField] double minDistanceFromMiner = 1.5;

        [Header("Cristal")]
        [SerializeField] double crystalHealth = 10;
        [SerializeField] double crystalReward = 1;

        [Header("Anão")]
        [Tooltip("Dano por golpe (DANO no HUD).")]
        [SerializeField] double damage = 1;
        [Tooltip("Segundos entre golpes (VELOCIDADE no HUD).")]
        [SerializeField] double hitInterval = 1;
        [Tooltip("Unidades de mundo por segundo.")]
        [SerializeField] double walkSpeed = 4;
        [Tooltip("Distância do centro do cristal em que o anão para e começa a bater.")]
        [SerializeField] double miningRange = 1;

        public MiningRules ToRules() => new MiningRules(
            new Vec2(mapMin.x, mapMin.y), new Vec2(mapMax.x, mapMax.y), new Vec2(minerStart.x, minerStart.y),
            initialCrystals, maxCrystals, spawnInterval,
            minCrystalSpacing, minDistanceFromMiner,
            crystalHealth, crystalReward,
            damage, hitInterval,
            walkSpeed, miningRange);
    }
}
