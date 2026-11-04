using MinersIdle.Game;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Balanços para os testes de tela. O jogo roda o GDD v7 (D-130), onde o cristal da caverna 1 morre num golpe; os
    /// testes que olham o golpe, a parada e a pose do anão precisam de cristais que aguentem vários golpes e usam o v5.
    /// </summary>
    static class TestBalance
    {
        public const string GddV5Path = "Assets/_Project/Data/Balance_GDD_v5.asset";

        public static MiningRulesAsset GddV5
        {
            get
            {
                var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV5Path);
                Assert.That(asset, Is.Not.Null, GddV5Path + " (rode o setup do projeto)");
                return asset;
            }
        }
    }
}
