using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>
    /// Regras do golpe e do toque (Marco 5), somente leitura: crítico (REQ-MIN-03), dano do toque (REQ-MIN-02, D-47) e
    /// janela do combo (REQ-MIN-04). Os valores vêm do asset; os do GDD são 5%, ×2, 10% e 1 s. Desde 2026-11-01, o teto
    /// de toques por segundo (D-119).
    /// </summary>
    public sealed class StrikeRules
    {
        /// <param name="tapCritChance">Chance base do toque; <c>null</c> = a mesma do golpe (o jogo até o GDD v5).</param>
        /// <param name="comboBonuses">
        /// Os marcos do combo que dão bônus ao parar de tocar (M9b, GDD v7 §3.4); <c>null</c> = nenhum (o jogo até o M9).
        /// Cada marco aparece uma vez.
        /// </param>
        public StrikeRules(double critChance, double critMultiplier, double tapFraction, double comboWindow,
                           int maxTapsPerSecond = 0, double? tapCritChance = null,
                           IEnumerable<ComboBonusRule> comboBonuses = null)
        {
            if (double.IsNaN(critChance) || critChance < 0 || critChance > 1) throw new ArgumentOutOfRangeException(nameof(critChance));
            double tapCrit = tapCritChance ?? critChance;
            if (double.IsNaN(tapCrit) || tapCrit < 0 || tapCrit > 1) throw new ArgumentOutOfRangeException(nameof(tapCritChance));
            if (double.IsNaN(critMultiplier) || critMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(critMultiplier));
            if (double.IsNaN(tapFraction) || tapFraction < 0) throw new ArgumentOutOfRangeException(nameof(tapFraction));
            if (double.IsNaN(comboWindow) || comboWindow <= 0) throw new ArgumentOutOfRangeException(nameof(comboWindow));
            if (maxTapsPerSecond < 0) throw new ArgumentOutOfRangeException(nameof(maxTapsPerSecond));

            CritChance = critChance;
            TapCritChance = tapCrit;
            CritMultiplier = critMultiplier;
            TapFraction = tapFraction;
            ComboWindow = comboWindow;
            MaxTapsPerSecond = maxTapsPerSecond;

            var bonuses = comboBonuses == null ? new List<ComboBonusRule>() : new List<ComboBonusRule>(comboBonuses);
            bonuses.Sort((a, b) => a.Combo.CompareTo(b.Combo));
            for (int i = 1; i < bonuses.Count; i++)
                if (bonuses[i].Combo == bonuses[i - 1].Combo)
                    throw new ArgumentException($"O marco ×{bonuses[i].Combo} do combo aparece duas vezes.", nameof(comboBonuses));
            ComboBonuses = bonuses;
        }

        /// <summary>Chance base de crítico do golpe automático (0–1; GDD v7: 0%, D-136).</summary>
        public double CritChance { get; }
        /// <summary>Chance base de crítico do toque (0–1; GDD v7: 5%, D-136). Até o GDD v5, a mesma do golpe.</summary>
        public double TapCritChance { get; }
        /// <summary>Quanto o dano multiplica no crítico.</summary>
        public double CritMultiplier { get; }
        /// <summary>Fração de um golpe do principal que o toque dá (0,1 = 10%).</summary>
        public double TapFraction { get; }
        /// <summary>Segundos de jogo sem toque até o combo zerar.</summary>
        public double ComboWindow { get; }
        /// <summary>
        /// Toques que valem em qualquer janela de 1 s de jogo, somando todos os dedos (D-119: 15). O toque além disso é
        /// ignorado: sem dano, sem crítico, sem combo. 0 = sem teto (os testes do M5).
        /// </summary>
        public int MaxTapsPerSecond { get; }
        /// <summary>Os marcos do combo com bônus (M9b), do menor para o maior; vazio = sem bônus de combo.</summary>
        public IReadOnlyList<ComboBonusRule> ComboBonuses { get; }

        /// <summary>Sem crítico nem teto; toque e combo como no GDD. É o padrão de quem não passa regras (testes do M2–M4).</summary>
        public static StrikeRules NoCritical() => new StrikeRules(0, 2, 0.1, 1);
    }

    /// <summary>Um golpe que acertou um cristal: de quem veio, quanto tirou e se foi crítico.</summary>
    public readonly struct MiningHit
    {
        public MiningHit(Crystal crystal, double damage, bool isTap, bool isCritical)
        {
            Crystal = crystal;
            Damage = damage;
            IsTap = isTap;
            IsCritical = isCritical;
        }

        public Crystal Crystal { get; }
        public double Damage { get; }
        /// <summary><c>true</c> = toque do jogador; <c>false</c> = golpe automático do anão.</summary>
        public bool IsTap { get; }
        public bool IsCritical { get; }
    }
}
