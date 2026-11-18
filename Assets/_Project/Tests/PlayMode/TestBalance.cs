using MinersIdle.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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

        /// <summary>
        /// M9g (D-311): o v5 com uma bomba de teste, no lugar do toque que quebrava o cristal no meio da picaretada
        /// (D-126). Uma cópia em memória (o asset não muda): por padrão, tira 100% e chega na hora (voo 0), com alcance
        /// de 1,5 un (o anão parado a 1 un do alvo pega só ele).
        /// </summary>
        public static MiningRulesAsset GddV5WithBomb(double fraction = 1, double flightSeconds = 0, double reach = 1.5)
        {
            var copy = Object.Instantiate(GddV5);
            var values = new SerializedObject(copy);
            var fractions = values.FindProperty("bombFractions");
            fractions.arraySize = 1;
            fractions.GetArrayElementAtIndex(0).doubleValue = fraction;
            values.FindProperty("bombChargeSeconds").doubleValue = 5;
            values.FindProperty("bombFlightSeconds").doubleValue = flightSeconds;
            values.FindProperty("bombReach").doubleValue = reach;
            values.ApplyModifiedPropertiesWithoutUndo();
            return copy;
        }
    }
}
