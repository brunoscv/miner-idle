using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// A cara de uma caverna na descida (M1b E8; D-72, D-102): chão, tinta da rocha, neblina e lampiões. Só visual: a regra
    /// da caverna (vida, lascas) fica no núcleo. O setup cria as 10 a partir dos tiles do Apellon; a troca jogando é do M8.
    /// </summary>
    [CreateAssetMenu(menuName = "Miners Idle/Bioma", fileName = "Bioma")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [Tooltip("Nome da caverna (coluna \"Sensação\" do GDD v5 §6), mostrado no DBG.")]
        [SerializeField] string displayName;
        [Tooltip("Chão da caverna (textura que repete; o setup gera com a paleta e a decoração da caverna).")]
        [SerializeField] Texture2D floorTexture;
        [Tooltip("Tinta da parede e do teto (acima de 1 clareia).")]
        [SerializeField] Color wallTint = Color.white;
        [Tooltip("Cor da neblina do fundo do corredor.")]
        [SerializeField] Color fogColor = new Color32(0x14, 0x0D, 0x09, 0xFF);
        [Tooltip("Um esteio com lampião a cada N (0 = nenhum).")]
        [SerializeField, Min(0)] int lampEvery;

        public string DisplayName => displayName;
        public Texture2D FloorTexture => floorTexture;
        public Color WallTint => wallTint;
        public Color FogColor => fogColor;
        public int LampEvery => lampEvery;
    }
}
