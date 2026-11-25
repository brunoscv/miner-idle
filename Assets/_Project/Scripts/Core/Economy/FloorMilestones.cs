using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// Os marcos de andar do GDD v8.4 que mexem nas permanentes (M9j E2, §4.3): valem para sempre a partir da primeira vez
    /// que o andar é alcançado (D-253, pelo recorde). 0 num andar = o marco não existe (o v8.2 e os balanços de antes).
    /// </summary>
    public sealed class FloorMilestones
    {
        /// <param name="internStartFloor">A partir deste recorde, todo dia novo começa com o estagiário (v8.4: 15; no lugar da Promoção).</param>
        /// <param name="redShardsDoubleFloor">A partir deste recorde, as lascas vermelhas do descanso dobram (v8.4: 45; D-370).</param>
        /// <param name="discountFloor">A partir deste recorde, todo andar pede <paramref name="discount"/> cristais a menos (v8.4: 60).</param>
        /// <param name="discount">Os cristais a menos no requisito de todo andar (v8.4: 100; soma com a Memória).</param>
        public FloorMilestones(int internStartFloor, int redShardsDoubleFloor, int discountFloor, double discount)
        {
            if (internStartFloor < 0) throw new ArgumentOutOfRangeException(nameof(internStartFloor));
            if (redShardsDoubleFloor < 0) throw new ArgumentOutOfRangeException(nameof(redShardsDoubleFloor));
            if (discountFloor < 0) throw new ArgumentOutOfRangeException(nameof(discountFloor));
            if (double.IsNaN(discount) || double.IsInfinity(discount) || discount < 0)
                throw new ArgumentOutOfRangeException(nameof(discount));
            InternStartFloor = internStartFloor;
            RedShardsDoubleFloor = redShardsDoubleFloor;
            DiscountFloor = discountFloor;
            Discount = discount;
        }

        public int InternStartFloor { get; }
        public int RedShardsDoubleFloor { get; }
        public int DiscountFloor { get; }
        public double Discount { get; }

        /// <summary>Se o marco do andar <paramref name="floor"/> já foi alcançado com o recorde <paramref name="deepest"/>.</summary>
        public static bool Reached(int floor, int deepest) => floor > 0 && deepest >= floor;

        /// <summary>Sem nenhum desses marcos (até o v8.2).</summary>
        public static FloorMilestones None { get; } = new FloorMilestones(0, 0, 0, 0);
    }
}
