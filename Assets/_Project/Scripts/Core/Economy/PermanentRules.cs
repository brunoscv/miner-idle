using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// As melhorias permanentes do anão (Marco 8b, GDD v7 §4.2, D-158): compradas com lascas vermelhas, valem em todas as
    /// runs. A ordem é a da tabela do GDD (e a da aba da loja, D-155). As do estagiário (Treinar, Promoção) entram no M7.
    /// </summary>
    public enum PermanentId
    {
        /// <summary>Memória: cada nível tira <see cref="UpgradeDefinition.Effect"/> cristais da trava do Mapa (GDD v7: 10).</summary>
        Memory,
        /// <summary>Sangue da Montanha: o dano final multiplica por (1 + efeito × nível) (GDD v7: 0,1 = +10%).</summary>
        MountainBlood,
        /// <summary>Coração Ardente: as lascas de cada quebra multiplicam por (1 + efeito × nível) (GDD v7: 0,5 = +50%).</summary>
        BurningHeart,
        /// <summary>Dedos Nervosos: soma efeito × nível na chance de crítico do golpe e do toque (GDD v7: 0,05 = +5%).</summary>
        NervousFingers,
        /// <summary>Alma dos Anões: a run começa com a Picareta em efeito × nível níveis, como se comprada (GDD v7: 1).</summary>
        DwarfSoul,
    }

    /// <summary>
    /// A tabela das lascas vermelhas (D-138): cada nível alcançado na run dá lascas, contado ao entrar nele (depois da
    /// parede). Abaixo do primeiro nível, nada; nos múltiplos do passo, o marco (o grande a partir de
    /// <see cref="BigMilestoneFrom"/>); nos outros, <see cref="PerLevel"/>. GDD v7: a partir do 10; 10, 20, 30, 40 dão 5;
    /// 50…100 dão 10; os outros, 1 (do 1 ao 100: 161).
    /// </summary>
    public sealed class RedShardTable
    {
        public RedShardTable(int firstLevel, int milestoneStep, double milestone, int bigMilestoneFrom, double bigMilestone,
                             double perLevel)
        {
            if (firstLevel < 1) throw new ArgumentOutOfRangeException(nameof(firstLevel));
            if (milestoneStep < 1) throw new ArgumentOutOfRangeException(nameof(milestoneStep));
            if (milestone < 0 || double.IsNaN(milestone)) throw new ArgumentOutOfRangeException(nameof(milestone));
            if (bigMilestone < 0 || double.IsNaN(bigMilestone)) throw new ArgumentOutOfRangeException(nameof(bigMilestone));
            if (perLevel < 0 || double.IsNaN(perLevel)) throw new ArgumentOutOfRangeException(nameof(perLevel));

            FirstLevel = firstLevel;
            MilestoneStep = milestoneStep;
            Milestone = milestone;
            BigMilestoneFrom = bigMilestoneFrom;
            BigMilestone = bigMilestone;
            PerLevel = perLevel;
        }

        /// <summary>O primeiro nível que dá lascas vermelhas (GDD v7: 10).</summary>
        public int FirstLevel { get; }
        /// <summary>Os níveis múltiplos disto dão o marco (GDD v7: 10).</summary>
        public int MilestoneStep { get; }
        /// <summary>Lascas do marco abaixo de <see cref="BigMilestoneFrom"/> (GDD v7: 5).</summary>
        public double Milestone { get; }
        /// <summary>A partir deste nível o marco vale <see cref="BigMilestone"/> (GDD v7: 50).</summary>
        public int BigMilestoneFrom { get; }
        public double BigMilestone { get; }
        /// <summary>Lascas dos níveis que não são marco (GDD v7: 1).</summary>
        public double PerLevel { get; }

        /// <summary>As lascas vermelhas de entrar no nível <paramref name="level"/>.</summary>
        public double At(int level)
        {
            if (level < FirstLevel) return 0;
            if (level % MilestoneStep != 0) return PerLevel;
            return level >= BigMilestoneFrom ? BigMilestone : Milestone;
        }

        /// <summary>
        /// As lascas vermelhas de uma run que chegou ao nível <paramref name="level"/>: a soma da tabela até ele. Numa run o
        /// nível só sobe, então é o que o jogador leva se descansar agora, sem contador à parte.
        /// </summary>
        public double UpTo(int level)
        {
            double total = 0;
            for (int l = FirstLevel; l <= level; l++) total += At(l);
            return total;
        }
    }

    /// <summary>
    /// As regras do dia e das melhorias permanentes (Marco 8b), somente leitura: o nível mínimo de descansar (D-152), a
    /// tabela das lascas vermelhas (D-138) e as linhas permanentes (custo em lascas vermelhas, D-158). Vêm do asset de
    /// valores; sem elas (<see cref="None"/>), não há descanso nem permanentes (o jogo até o GDD v5).
    /// </summary>
    public sealed class PermanentRules
    {
        static readonly UpgradeDefinition Off = new UpgradeDefinition(0, 1, 0, 0);

        readonly UpgradeDefinition[] _lines;

        /// <param name="restLevel">O nível a partir do qual dá para descansar (GDD v7: 10, D-152). 0 desliga o descanso.</param>
        /// <param name="lines">Uma linha por <see cref="PermanentId"/>, na ordem do enum; <c>null</c> = desligada.</param>
        public PermanentRules(int restLevel, RedShardTable redShards, params UpgradeDefinition[] lines)
        {
            if (restLevel < 0) throw new ArgumentOutOfRangeException(nameof(restLevel));
            RestLevel = restLevel;
            RedShards = redShards ?? throw new ArgumentNullException(nameof(redShards));

            int count = Enum.GetValues(typeof(PermanentId)).Length;
            if (lines != null && lines.Length > count) throw new ArgumentOutOfRangeException(nameof(lines));
            _lines = new UpgradeDefinition[count];
            for (int i = 0; i < count; i++) _lines[i] = lines != null && i < lines.Length && lines[i] != null ? lines[i] : Off;
        }

        /// <summary>O nível a partir do qual o botão Descansar aparece (D-152). 0 = sem descanso.</summary>
        public int RestLevel { get; }

        public bool IsRestEnabled => RestLevel > 0;

        public RedShardTable RedShards { get; }

        public UpgradeDefinition Get(PermanentId id) => _lines[(int)id];

        /// <summary>Sem descanso e sem permanentes: o jogo até o GDD v5.</summary>
        public static PermanentRules None { get; } = new PermanentRules(0, new RedShardTable(1, 1, 0, 1, 0, 0));
    }
}
