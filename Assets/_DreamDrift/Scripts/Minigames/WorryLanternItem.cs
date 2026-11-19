using UnityEngine;
using TMPro;

namespace DreamDrift.Minigames
{
    public class WorryLanternItem : MonoBehaviour
    {
        public SpriteRenderer lanternSprite;
        public SpriteRenderer glowSprite;
        public TMP_Text worldWorryText;

        private float riseSpeed = 1.6f;
        private float swayAmplitude = 0.45f;
        private float swayFrequency = 1.1f;
        private float lifetime = 11f;
        private float age;
        private float startX;
        private float swayPhase;
        private Vector3 initialScale;

        public void Launch(string worryMessage, float launchBoost)
        {
            startX = transform.position.x;
            swayPhase = Random.Range(0f, Mathf.PI * 2f);
            riseSpeed = Mathf.Clamp(1.35f + launchBoost * 1.4f, 1.35f, 3.2f);
            swayAmplitude = Random.Range(0.28f, 0.55f);
            swayFrequency = Random.Range(0.85f, 1.35f);
            initialScale = transform.localScale;

            if (worldWorryText != null)
            {
                string trimmed = string.IsNullOrWhiteSpace(worryMessage) ? "ปล่อยวาง..." : worryMessage.Trim();
                if (trimmed.Length > 26) trimmed = trimmed.Substring(0, 25) + "...";
                worldWorryText.text = trimmed;
            }
        }

        private void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);

            // Smooth upward float + gentle sinusoidal wind sway
            float currentRise = Mathf.Lerp(riseSpeed, 0.85f, t);
            Vector3 pos = transform.position;
            pos.y += currentRise * Time.deltaTime;
            pos.x = startX + Mathf.Sin(age * swayFrequency + swayPhase) * swayAmplitude;
            transform.position = pos;

            // Gentle tilt following sway
            float tilt = Mathf.Cos(age * swayFrequency + swayPhase) * 6f;
            transform.rotation = Quaternion.Euler(0f, 0f, -tilt);

            // Shrink gracefully as it floats high into the night sky
            float scaleMul = Mathf.Lerp(1f, 0.24f, t * t);
            transform.localScale = initialScale * scaleMul;

            // Fade out peacefully near the end
            float alpha = t < 0.72f ? 1f : Mathf.InverseLerp(1f, 0.72f, t);
            if (lanternSprite != null)
            {
                Color c = lanternSprite.color;
                c.a = alpha;
                lanternSprite.color = c;
            }
            if (glowSprite != null)
            {
                float pulse = 0.75f + 0.25f * Mathf.Sin(age * 4.2f + swayPhase);
                Color g = glowSprite.color;
                g.a = alpha * 0.55f * pulse;
                glowSprite.color = g;
            }
            if (worldWorryText != null)
            {
                Color tc = worldWorryText.color;
                tc.a = alpha * 0.92f;
                worldWorryText.color = tc;
            }

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
