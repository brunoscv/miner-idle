using System;
using UnityEngine;

namespace MinersIdle.Persistence
{
    /// <summary>
    /// A escolha do DBG → Cálculos (M8 E3b-3, D-143), como fica no arquivo: o balanço (índice da lista do
    /// <c>MiningRunner</c>; 0 = o do GDD) e os números da Picareta trocados no DBG. Campos públicos para o
    /// <c>JsonUtility</c>; os "has" dizem se o número saiu do GDD (sem eles, o JSON não tem como guardar "nenhum").
    /// </summary>
    [Serializable]
    public sealed class CalculationData
    {
        public int balance;
        public bool hasPickaxeCaveGrowth;
        public double pickaxeCaveGrowth;
        public bool hasPickaxeStep;
        public double pickaxeStep;
        /// <summary>A Picareta pela caverna do DBG → Caverna (D-201). Arquivo antigo, sem o campo: <c>false</c>, a do GDD.</summary>
        public bool pickaxeByCave;
    }

    /// <summary>
    /// Guarda a escolha do DBG → Cálculos no aparelho, num arquivo à parte do save (<c>calculations.json</c>): apagar o
    /// save não volta o cálculo, e o save não carrega nada do DBG. Um arquivo ilegível vale como "nenhuma escolha" (o
    /// jogo abre no GDD).
    /// </summary>
    public sealed class CalculationStore
    {
        public const string FileName = "calculations.json";

        readonly ISaveStore _store;

        public CalculationStore(ISaveStore store) => _store = store ?? throw new ArgumentNullException(nameof(store));

        /// <summary>A escolha gravada, ou <c>null</c> se não houver (ou se o arquivo não se lê).</summary>
        public CalculationData Load()
        {
            string text;
            try
            {
                text = _store.Read();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Cálculos] Não deu para ler a escolha do DBG: {e.Message}. Abre no GDD.");
                return null;
            }
            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                return JsonUtility.FromJson<CalculationData>(text);
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning($"[Cálculos] Escolha do DBG ilegível ({e.Message}). Abre no GDD.");
                return null;
            }
        }

        public void Save(CalculationData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _store.Write(JsonUtility.ToJson(data, prettyPrint: true));
        }

        public void Delete() => _store.Delete();
    }
}
