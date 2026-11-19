using UnityEngine;
using TMPro;

namespace DreamDrift.UI
{
    /// <summary>
    /// Global Unity TextMeshPro Thai vowel & tone-mark shaper.
    /// Uses exact glyph ink bounds (prev.topRight.y vs curr.bottomLeft.y) so stacked Thai tone marks
    /// (่ ้ ๊ ๋ ์) sit cleanly right above upper vowels (ั ิ ี ึ ื ็) with natural Thai spacing — never overlapping, never too far.
    /// </summary>
    public class ThaiTMPShaper : MonoBehaviour
    {
        private static bool isRegistered;
        private static bool isAdjusting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            RegisterGlobalHook();
            RefreshAllExistingTexts();
        }

        private void OnEnable()
        {
            RegisterGlobalHook();
            RefreshAllExistingTexts();
        }

        private static void RegisterGlobalHook()
        {
            if (isRegistered) return;
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            isRegistered = true;
        }

        public static void RefreshAllExistingTexts()
        {
            TMP_Text[] allTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
            for (int i = 0; i < allTexts.Length; i++)
            {
                if (allTexts[i] != null && allTexts[i].gameObject.activeInHierarchy)
                {
                    // ForceMeshUpdate triggers TEXT_CHANGED_EVENT exactly once (no double-lift!)
                    allTexts[i].ForceMeshUpdate();
                }
            }
        }

        private static void OnTextChanged(Object obj)
        {
            if (isAdjusting) return;
            if (obj is TMP_Text tmp)
            {
                AdjustThaiMarks(tmp);
            }
        }

        private static void AdjustThaiMarks(TMP_Text tmp)
        {
            if (tmp == null) return;
            TMP_TextInfo textInfo = tmp.textInfo;
            if (textInfo == null || textInfo.characterCount < 2) return;

            bool modified = false;

            for (int i = 1; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo curr = textInfo.characterInfo[i];
                if (!curr.isVisible) continue;

                char c = curr.character;
                bool isToneOrTopMark = IsThaiToneOrTopMark(c);
                bool isUpperVowel = IsThaiUpperVowel(c);

                if (!isToneOrTopMark && !isUpperVowel) continue;

                TMP_CharacterInfo prev = textInfo.characterInfo[i - 1];
                char prevChar = prev.character;
                char prev2Char = (i >= 2) ? textInfo.characterInfo[i - 2].character : '\0';

                float liftY = 0f;
                float shiftX = 0f;
                float fontRef = curr.pointSize > 0.01f ? curr.pointSize : tmp.fontSize;

                // 1. Stacked tone mark above an upper vowel (e.g. นี้, ขึ้น, เรื่อง, ตั้ง, เริ่ม, คลื่น)
                if (isToneOrTopMark && IsThaiUpperVowel(prevChar) && prev.isVisible)
                {
                    // Place tone mark bottom snugly above the upper vowel's top ink edge (+2% gap)
                    float desiredBottomY = prev.topRight.y + (fontRef * 0.02f);
                    float neededLift = desiredBottomY - curr.bottomLeft.y;
                    if (neededLift > 0f)
                    {
                        liftY = Mathf.Min(neededLift, fontRef * 0.30f);
                    }

                    if (IsThaiTallStem(prev2Char))
                    {
                        shiftX = -fontRef * 0.11f;
                    }
                }
                // 2. Tone mark or upper vowel directly over tall-stem consonant (ป ฝ ฟ e.g. ฟ้า, ปัด, ฝึก)
                else if (IsThaiTallStem(prevChar))
                {
                    shiftX = -fontRef * 0.11f;
                }

                if (Mathf.Abs(liftY) > 0.0001f || Mathf.Abs(shiftX) > 0.0001f)
                {
                    int matIndex = curr.materialReferenceIndex;
                    int vertIndex = curr.vertexIndex;
                    if (matIndex < 0 || matIndex >= textInfo.meshInfo.Length) continue;

                    Vector3[] verts = textInfo.meshInfo[matIndex].vertices;
                    if (verts == null || vertIndex + 3 >= verts.Length) continue;

                    Vector3 offset = new Vector3(shiftX, liftY, 0f);
                    verts[vertIndex + 0] += offset;
                    verts[vertIndex + 1] += offset;
                    verts[vertIndex + 2] += offset;
                    verts[vertIndex + 3] += offset;
                    modified = true;
                }
            }

            if (modified)
            {
                isAdjusting = true;
                try
                {
                    tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                }
                finally
                {
                    isAdjusting = false;
                }
            }
        }

        private static bool IsThaiUpperVowel(char c)
        {
            // ั (0E31), ิ (0E34), ี (0E35), ึ (0E36), ื (0E37), ็ (0E47)
            return c == '\u0E31'
                || (c >= '\u0E34' && c <= '\u0E37')
                || c == '\u0E47';
        }

        private static bool IsThaiToneOrTopMark(char c)
        {
            // ่ (0E48), ้ (0E49), ๊ (0E4A), ๋ (0E4B), ์ (0E4C)
            return c >= '\u0E48' && c <= '\u0E4C';
        }

        private static bool IsThaiTallStem(char c)
        {
            // ป (0E1B), ฝ (0E1D), ฟ (0E1F)
            return c == '\u0E1B' || c == '\u0E1D' || c == '\u0E1F';
        }
    }
}
