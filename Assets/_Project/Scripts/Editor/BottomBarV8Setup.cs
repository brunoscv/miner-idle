using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// A E7 do M9i (#19, D-340), na cena Game já montada: a faixa da bomba vai para o fundo (12 px) e o Descansar e o
    /// Próximo andar sobem para cima dela (6 px de vão), juntos no meio, com 3 px entre eles: os dois ocupam a largura
    /// exata da faixa (94 + 3 + 94 = 191). O aviso do Descansar trancado vai junto com o botão.
    /// <para>
    /// As cenas novas já nascem assim (as medidas moram no <see cref="HudSkinSetup"/> e no <see cref="BombSetup"/>);
    /// este setup leva as cenas antigas. Roda depois dos outros. Idempotente.
    /// </para>
    /// </summary>
    static class BottomBarV8Setup
    {
        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            if (safeArea == null) return false;

            bool changed = false;
            if (safeArea.Find("Rest/RestButton") is RectTransform rest && HudSkinSetup.PlaceOnTheBar(rest, right: false))
            {
                EditorUtility.SetDirty(rest);
                changed = true;
            }
            if (safeArea.Find("NextFloor/Bar") is RectTransform next && HudSkinSetup.PlaceOnTheBar(next, right: true))
            {
                EditorUtility.SetDirty(next);
                changed = true;
            }
            if (safeArea.Find("Rest/Notice") is RectTransform notice && HudColumnV8Setup.PlaceRestNotice(notice))
            {
                EditorUtility.SetDirty(notice);
                changed = true;
            }
            if (safeArea.Find(BombSetup.RootName) is RectTransform bomb)
            {
                var position = new Vector2(0f, BombSetup.SliderBottom);
                if (bomb.anchoredPosition != position)
                {
                    bomb.anchoredPosition = position;
                    EditorUtility.SetDirty(bomb);
                    changed = true;
                }
            }

            if (changed)
                Debug.Log("[Setup] A bomba embaixo e o Descansar e o Próximo andar em cima dela, na largura da faixa (M9i E7, D-340).");
            return changed;
        }
    }
}
