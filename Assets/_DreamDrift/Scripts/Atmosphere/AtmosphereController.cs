using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DreamDrift.Atmosphere
{
    public class AtmosphereController : MonoBehaviour
    {
        [Header("Scene Visuals")]
        public SpriteRenderer skyRenderer;
        public SpriteRenderer lakeRenderer;
        public SpriteRenderer moonRenderer;
        public SpriteRenderer campfireFlameRenderer;
        public Transform[] driftingClouds;

        [Header("URP 2D Lights")]
        public Light2D globalLight2D;
        public Light2D campfireLight2D;
        public Light2D moonLight2D;

        [Header("Dynamic Weather & Nature VFX")]
        public ParticleSystem rainParticles;
        public ParticleSystem campfireEmbers;
        public ParticleSystem fireflyParticles;
        public ParticleSystem waterRipples;

        [Header("Transition")]
        public float colorLerpSpeed = 1.8f;

        private Color targetSkyColor = new Color(0.11f, 0.18f, 0.35f, 1f);
        private Color targetLakeColor = new Color(0.15f, 0.25f, 0.45f, 0.92f);
        private Color targetGlobalLight = new Color(0.63f, 0.73f, 1f, 1f);

        private float rainLevel = 0.4f;
        private float fireLevel = 0.6f;
        private float waveLevel = 0.3f;
        private float cricketLevel = 0.3f;

        private Vector3 baseFlameScale = Vector3.one;

        private void Start()
        {
            if (campfireFlameRenderer != null)
            {
                baseFlameScale = campfireFlameRenderer.transform.localScale;
            }
            UpdateVFX();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Smooth color transitions
            if (skyRenderer != null)
                skyRenderer.color = Color.Lerp(skyRenderer.color, targetSkyColor, dt * colorLerpSpeed);

            if (lakeRenderer != null)
            {
                Color shimmer = targetLakeColor * (0.92f + 0.08f * Mathf.Sin(Time.time * (1.2f + waveLevel * 2.5f)));
                shimmer.a = 0.92f;
                lakeRenderer.color = Color.Lerp(lakeRenderer.color, shimmer, dt * colorLerpSpeed * 1.5f);
            }

            if (globalLight2D != null)
                globalLight2D.color = Color.Lerp(globalLight2D.color, targetGlobalLight, dt * colorLerpSpeed);

            // Campfire natural flicker
            float flicker = Mathf.PerlinNoise(Time.time * 5.5f, 1.3f);
            if (campfireLight2D != null)
            {
                float targetIntensity = fireLevel <= 0.02f ? 0f : Mathf.Lerp(0.35f, 2.2f, fireLevel) * (0.85f + flicker * 0.30f);
                campfireLight2D.intensity = Mathf.Lerp(campfireLight2D.intensity, targetIntensity, dt * 8f);
            }

            if (campfireFlameRenderer != null)
            {
                float scaleMul = fireLevel <= 0.02f ? 0f : Mathf.Lerp(0.35f, 1.15f, fireLevel) * (0.92f + flicker * 0.16f);
                campfireFlameRenderer.transform.localScale = Vector3.Lerp(
                    campfireFlameRenderer.transform.localScale,
                    baseFlameScale * scaleMul,
                    dt * 10f);
            }

            // Gentle cloud drift
            if (driftingClouds != null)
            {
                for (int i = 0; i < driftingClouds.Length; i++)
                {
                    Transform c = driftingClouds[i];
                    if (c == null) continue;
                    float speed = 0.08f + (i % 3) * 0.04f;
                    Vector3 p = c.position;
                    p.x += speed * dt;
                    if (p.x > 6.5f) p.x = -6.5f;
                    c.position = p;
                }
            }
        }

        public void ApplyMoodColors(Color skyTop, Color skyBottom, Color globalTint, bool instant = false)
        {
            targetSkyColor = Color.Lerp(skyTop, skyBottom, 0.55f);
            targetSkyColor.a = 1f;
            targetLakeColor = Color.Lerp(skyBottom, globalTint, 0.25f);
            targetLakeColor.a = 0.92f;
            targetGlobalLight = globalTint;

            if (instant)
            {
                if (skyRenderer != null) skyRenderer.color = targetSkyColor;
                if (lakeRenderer != null) lakeRenderer.color = targetLakeColor;
                if (globalLight2D != null) globalLight2D.color = targetGlobalLight;
            }
        }

        public void SetEnvironmentLevels(float rain, float fire, float wave, float cricket)
        {
            rainLevel = Mathf.Clamp01(rain);
            fireLevel = Mathf.Clamp01(fire);
            waveLevel = Mathf.Clamp01(wave);
            cricketLevel = Mathf.Clamp01(cricket);
            UpdateVFX();
        }

        private void UpdateVFX()
        {
            SetEmission(rainParticles, rainLevel > 0.02f ? Mathf.Lerp(10f, 140f, rainLevel) : 0f);
            SetEmission(campfireEmbers, fireLevel > 0.02f ? Mathf.Lerp(2f, 24f, fireLevel) : 0f);
            SetEmission(waterRipples, waveLevel > 0.02f ? Mathf.Lerp(1f, 14f, waveLevel) : 0f);
            SetEmission(fireflyParticles, cricketLevel > 0.02f ? Mathf.Lerp(2f, 22f, cricketLevel) : 0f);
        }

        private static void SetEmission(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            if (rate > 0.01f && !ps.isPlaying) ps.Play();
        }
    }
}
