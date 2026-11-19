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
            /// <summary>Fração mostrada em %: 0,05 → "+5%" (a chance do Crítico, D-136; os bônus das permanentes, D-158).</summary>
            ChancePercent,
            /// <summary>Fração mostrada em % sem sinal: 0,1 → "10%" (a bomba da linha Bomba, M9g, D-314).</summary>
            Percent,
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
            [Tooltip("Compra única já feita: o texto do botão (vazio = o do catálogo). O Estagiário diz contratado (M7).")]
            [SerializeField] string boughtLabel;
            [Tooltip("O nível da linha começa em 1 (GDD v7, convenção do Apellon: nível 1 = sem compra): a loja mostra " +
                     "1 + compras de 1 + limite (\"nv 1/50\"). As Picaretas do anão e do estagiário (D-207).")]
            [SerializeField] bool levelFromOne;

            public bool Locked => locked;
            public UpgradeId Upgrade => upgrade;
            public string Title => title;
            public string EffectFormat => effectFormat;
            public EffectStyle Style => style;
            public Color IconColor => iconColor;
            public Sprite Icon => icon;
            public Sprite Frame => frame;
            public string BoughtLabel => boughtLabel;
            /// <summary>O nível mostrado começa em 1: 1 + compras de 1 + limite (D-207).</summary>
            public bool LevelFromOne => levelFromOne;
        }

        /// <summary>
        /// Uma linha da aba das permanentes (Marco 8b, E4, D-155). O valor do efeito é o que a linha soma no nível
        /// (<see cref="PermanentPreview.CurrentValue"/>): cristais a menos na trava, a fração do bônus, níveis de Picareta.
        /// </summary>
        [Serializable]
        public sealed class PermanentEntry
        {
            [SerializeField] PermanentId permanent;
            [SerializeField] string title;
            [Tooltip("{0} = valor agora, {1} = depois da compra.")]
            [SerializeField] string effectFormat;
            [Tooltip("No limite: {0} = o valor final.")]
            [SerializeField] string maxedFormat;
            [SerializeField] EffectStyle style;
            [SerializeField] Sprite icon;
            [Tooltip("Moldura do ícone (cor de raridade). Vazio = sem moldura.")]
            [SerializeField] Sprite frame;
            [Tooltip("Linha que espera outra (a Promoção, M7): {0} = o nível que a outra precisa ter.")]
            [SerializeField] string lockedFormat;
            [Tooltip("Um nome por nível (a Bomba, M9g, D-314: o nível 0 é a Pequena). Com nomes, os textos recebem {0} = o " +
                     "nome de agora, {1} = o valor de agora, {2} = o de depois, {3} = o nome de depois. Vazio = sem nomes.")]
            [SerializeField] string[] levelNames = Array.Empty<string>();
            [Tooltip("Uma folha com um quadro por nível, lado a lado (a Bomba: bombs.png); o ícone segue o nível. Vazio = o ícone fixo.")]
            [SerializeField] Texture2D levelSheet;
            [SerializeField] int levelFrames;

            public PermanentId Permanent => permanent;
            public string Title => title;
            public string EffectFormat => effectFormat;
            public string MaxedFormat => maxedFormat;
            public EffectStyle Style => style;
            public Sprite Icon => icon;
            public Sprite Frame => frame;
            public string LockedFormat => lockedFormat;
            public System.Collections.Generic.IReadOnlyList<string> LevelNames => levelNames ?? Array.Empty<string>();
            public bool HasLevelNames => levelNames != null && levelNames.Length > 0;
            public Texture2D LevelSheet => levelSheet;
            public int LevelFrames => levelFrames;
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
        [Tooltip("Botão de uma compra que custa 0 (o Mapa, GDD v7, D-175): no lugar do preço \"0\". Vazio = mostra o 0.")]
        [SerializeField] string freeLabel;
        [Tooltip("Botão de uma linha travada por um marco de andar (o Estagiário, M9c E3, P7): {0} = o andar que libera.")]
        [SerializeField] string floorLockedFormat;
        [SerializeField] Entry[] entries = Array.Empty<Entry>();

        [Header("Abas (Marco 8b, E4, D-155)")]
        [SerializeField] string runTabLabel;
        [SerializeField] string permanentsTabLabel;
        [Tooltip("No alto da aba das permanentes durante a run (M9d E2, D-276): só consulta.")]
        [SerializeField] string permanentsConsultNote;
        [Tooltip("As linhas da aba das permanentes, na ordem da tabela do GDD (as que chegam ao limite descem para o fim).")]
        [SerializeField] PermanentEntry[] permanentEntries = Array.Empty<PermanentEntry>();

        [Header("Loja rápida no DBG (M9i E8, D-341): a proposta para o Apellon")]
        [Tooltip("O nível no card da loja rápida. {0} = nível (como na LOJA: a Picareta conta do 1).")]
        [SerializeField] string quickLevelFormat = "Nv {0}";
        [Tooltip("As melhorias da loja rápida, na ordem dos cards (só as da aba MELHORIAS).")]
        [SerializeField] UpgradeId[] quickUpgrades = { UpgradeId.Pickaxe, UpgradeId.Speed, UpgradeId.Mining, UpgradeId.Critical };

        [Header("Aba do estagiário (M7, E3, D-170, D-174)")]
        [SerializeField] string internTabLabel;
        [Tooltip("As melhorias dele, na ordem da aba (só aparecem com ele contratado).")]
        [SerializeField] Entry[] internEntries = Array.Empty<Entry>();

        public string ShopTitle => shopTitle;
        public string BalanceFormat => balanceFormat;
        public string LevelFormat => levelFormat;
        public string MaxedLabel => maxedLabel;
        public string PermanentsConsultNote => permanentsConsultNote;
        public string BoughtLabel => boughtLabel;
        public string LockedLabel => lockedLabel;
        public string BonusPercentFormat => bonusPercentFormat;
        public string FreeLabel => freeLabel;
        public string FloorLockedFormat => floorLockedFormat;
        public System.Collections.Generic.IReadOnlyList<Entry> Entries => entries;
        public string RunTabLabel => runTabLabel;
        public string PermanentsTabLabel => permanentsTabLabel;
        public System.Collections.Generic.IReadOnlyList<PermanentEntry> PermanentEntries => permanentEntries;
        public string InternTabLabel => internTabLabel;
        public System.Collections.Generic.IReadOnlyList<Entry> InternEntries => internEntries;
        public string QuickLevelFormat => quickLevelFormat;
        public System.Collections.Generic.IReadOnlyList<UpgradeId> QuickUpgrades => quickUpgrades ?? Array.Empty<UpgradeId>();

        /// <summary>A linha da aba MELHORIAS de uma melhoria (null se ela não está na loja).</summary>
        public Entry Find(UpgradeId upgrade)
        {
            foreach (var entry in entries)
                if (entry != null && !entry.Locked && entry.Upgrade == upgrade) return entry;
            return null;
        }

        /// <summary>Um valor de efeito no estilo da linha (o único formatador da loja, nas duas abas).</summary>
        public string FormatValue(EffectStyle style, double value)
        {
            switch (style)
            {
                case EffectStyle.Seconds:
                    return NumberFormatter.FormatSeconds(value);
                case EffectStyle.BonusPercent:
                    return string.Format(bonusPercentFormat, NumberFormatter.Format((value - 1) * 100));
                case EffectStyle.ChancePercent:
                    return string.Format(bonusPercentFormat, NumberFormatter.Format(value * 100));
                case EffectStyle.Percent:
                    return NumberFormatter.FormatPercent(value);
                default:
                    return NumberFormatter.Format(value);
            }
        }
    }
}
