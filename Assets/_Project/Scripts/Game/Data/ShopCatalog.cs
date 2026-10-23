using System;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Textos e ordem da loja (S03, Marco 4), em pt-BR e em dados: nenhum texto de interface no código. Cada linha é uma
    /// melhoria do Núcleo ou uma linha bloqueada ("Em breve", D-42). O asset do jogo é <c>Data/ShopCatalog</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Miners Idle/Catálogo da loja", fileName = "ShopCatalog")]
    public sealed class ShopCatalog : ScriptableObject
    {
        /// <summary>Como formatar os valores do efeito "de → para".</summary>
        public enum EffectStyle
        {
            /// <summary>Número simples (dano, caminhada).</summary>
            Number,
            /// <summary>Segundos com duas casas (intervalo do golpe).</summary>
            Seconds,
            /// <summary>Multiplicador mostrado como bônus em %: 1,08 → "+8%" (Mineração).</summary>
            BonusPercent,
        }

        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Linha ainda não existe no jogo: aparece apagada, com o texto de bloqueio.")]
            [SerializeField] bool locked;
            [SerializeField] UpgradeId upgrade;
            [SerializeField] string title;
            [Tooltip("{0} = valor agora, {1} = depois da compra. Em linha bloqueada, a descrição.")]
            [SerializeField] string effectFormat;
            [SerializeField] EffectStyle style;
            [Tooltip("Cor do ícone provisório (graybox), usada quando não há ícone.")]
            [SerializeField] Color iconColor = Color.white;
            [Tooltip("Ícone da melhoria (M1b E3). Vazio = quadrado na cor acima.")]
            [SerializeField] Sprite icon;
            [Tooltip("Moldura do ícone, na cor de raridade da melhoria (M1b E3). Vazio = sem moldura.")]
            [SerializeField] Sprite frame;

            public bool Locked => locked;
            public UpgradeId Upgrade => upgrade;
            public string Title => title;
            public string EffectFormat => effectFormat;
            public EffectStyle Style => style;
            public Color IconColor => iconColor;
            public Sprite Icon => icon;
            public Sprite Frame => frame;
        }

        [SerializeField] string shopTitle;
        [Tooltip("{0} = saldo de lascas.")]
        [SerializeField] string balanceFormat;
        [Tooltip("{0} = nível, {1} = limite.")]
        [SerializeField] string levelFormat;
        [SerializeField] string maxedLabel;
        [Tooltip("Compra única já feita (Picareta Dupla).")]
        [SerializeField] string boughtLabel;
        [SerializeField] string lockedLabel;
        [Tooltip("Bônus em % (Mineração). {0} = número; ex.: \"+{0}%\".")]
        [SerializeField] string bonusPercentFormat;
        [SerializeField] Entry[] entries = Array.Empty<Entry>();

        public string ShopTitle => shopTitle;
        public string BalanceFormat => balanceFormat;
        public string LevelFormat => levelFormat;
        public string MaxedLabel => maxedLabel;
        public string BoughtLabel => boughtLabel;
        public string LockedLabel => lockedLabel;
        public string BonusPercentFormat => bonusPercentFormat;
        public System.Collections.Generic.IReadOnlyList<Entry> Entries => entries;
    }
}
