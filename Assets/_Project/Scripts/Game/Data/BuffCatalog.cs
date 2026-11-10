using System;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Como cada bônus com tempo aparece (M9b, E2; D-141, D-215): o ícone da coluna da esquerda, o nome e a descrição do
    /// detalhe e o texto grande do ganho ("FRENESI!"), mais os textos do ganho e do tempo. Tudo em pt-BR e em dados:
    /// nenhum texto de interface no código. O asset do jogo é <c>Data/BuffCatalog</c>. O raro (E4) e o AD (E5) entram
    /// aqui como linhas novas.
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
