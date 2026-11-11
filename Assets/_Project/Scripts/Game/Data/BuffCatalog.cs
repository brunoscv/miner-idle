using System;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Como cada bônus com tempo aparece (M9b, E2; D-141, D-215): o ícone da coluna da esquerda, o nome e a descrição do
    /// detalhe e o texto grande do ganho ("FRENESI!"), mais os textos do ganho e do tempo. Tudo em pt-BR e em dados:
    /// nenhum texto de interface no código. O asset do jogo é <c>Data/BuffCatalog</c>. Desde a E4, as cartas do cristal
    /// raro (origem <see cref="BonusSource.Rare"/>, tier = a <see cref="RareCard"/>) com o texto da carta, para quem vale e
    /// o texto que sobe ao escolher, e os textos da janela (D-225). O AD (E5) entra aqui como linha nova.
    /// </summary>
    [CreateAssetMenu(menuName = "Miners Idle/Catálogo dos bônus", fileName = "BuffCatalog")]
    public sealed class BuffCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] BonusSource source;
            [Tooltip("O marco que dá o bônus (10, 30, 50, 100 no combo).")]
            [SerializeField] int tier;
            [SerializeField] Sprite icon;
            [Tooltip("A primeira linha do detalhe (âmbar).")]
            [SerializeField] string title;
            [Tooltip("A segunda linha do detalhe.")]
            [SerializeField] string description;
            [Tooltip("Texto grande ao ganhar, com um tremor leve (\"FRENESI!\"). Vazio = só o texto do ganho.")]
            [SerializeField] string headline;
            [Tooltip("Raro: o texto que sobe do anão ao escolher a carta (\"DANO ×2\").")]
            [SerializeField] string gain;
            [Tooltip("Raro: o efeito escrito na carta da janela (pode ter quebra de linha).")]
            [SerializeField, TextArea(2, 4)] string cardText;
            [Tooltip("Raro: a linha de baixo da carta, para quem vale (\"Vale p/ o estagiário\").")]
            [SerializeField] string cardNote;
            [Tooltip("Raro: a carta vale também para o estagiário (a linha de baixo em verde).")]
            [SerializeField] bool forIntern;

            /// <summary>Para o serializador do Unity.</summary>
            public Entry() { }

            public Entry(BonusSource source, int tier, Sprite icon, string title, string description, string headline = "")
            {
                this.source = source;
                this.tier = tier;
                this.icon = icon;
                this.title = title;
                this.description = description;
                this.headline = headline;
            }

            public BonusSource Source => source;
            public int Tier => tier;
            public Sprite Icon => icon;
            public string Title => title;
            public string Description => description;
            public string Headline => headline;
            public string Gain => gain;
            public string CardText => cardText;
            public string CardNote => cardNote;
            public bool ForIntern => forIntern;
        }

        [SerializeField] Entry[] entries = new Entry[0];

        [Header("Textos")]
        [Tooltip("Os segundos ao lado do ícone. {0} = segundos que faltam.")]
        [SerializeField] string secondsFormat;
        [Tooltip("A terceira linha do detalhe. {0} = o marco do combo, {1} = segundos que faltam.")]
        [SerializeField] string comboFooterFormat;
        [Tooltip("O texto que sobe do contador ao ganhar só dano. {0} = a soma, \"25%\".")]
        [SerializeField] string gainDamageFormat;
        [Tooltip("Ao ganhar só o crítico garantido.")]
        [SerializeField] string gainCritical;
        [Tooltip("Ao ganhar dano e o crítico garantido juntos. {0} = a soma do dano.")]
        [SerializeField] string gainBothFormat;

        [Header("Cristal raro (M9b E4, D-225)")]
        [Tooltip("A terceira linha do detalhe de uma carta do raro. {0} = segundos que faltam.")]
        [SerializeField] string rareFooterFormat;
        [Tooltip("O título da janela.")]
        [SerializeField] string rareTitle;
        [Tooltip("Debaixo do título, enquanto o jogador escolhe. {0} = a duração das cartas, \"60 s\".")]
        [SerializeField] string rareChooseFormat;
        [Tooltip("Debaixo do título, quando o jogo sorteou (D-186).")]
        [SerializeField] string rarePicked;
        [Tooltip("Embaixo da barra do tempo.")]
        [SerializeField] string rareHint;
        [Tooltip("Raros esperando na fila (D-221). {0} = quantos.")]
        [SerializeField] string rareQueueFormat;
        [Tooltip("O botão de fechar a janela do sorteio.")]
        [SerializeField] string rareClose;
        [Tooltip("A duração na carta e no título. {0} = segundos.")]
        [SerializeField] string cardSecondsFormat;

        public string RareFooterFormat => rareFooterFormat;
        public string RareTitle => rareTitle;
        public string RareChooseFormat => rareChooseFormat;
        public string RarePicked => rarePicked;
        public string RareHint => rareHint;
        public string RareQueueFormat => rareQueueFormat;
        public string RareClose => rareClose;
        public string CardSecondsFormat => cardSecondsFormat;

        public string SecondsFormat => secondsFormat;
        public string ComboFooterFormat => comboFooterFormat;
        public string GainDamageFormat => gainDamageFormat;
        public string GainCritical => gainCritical;
        public string GainBothFormat => gainBothFormat;

        /// <summary>A linha de um bônus; <c>null</c> se o catálogo não o tem (aparece sem ícone).</summary>
        public Entry Find(BonusSource source, int tier)
        {
            if (entries == null) return null;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].Source == source && entries[i].Tier == tier) return entries[i];
            return null;
        }
    }
}
