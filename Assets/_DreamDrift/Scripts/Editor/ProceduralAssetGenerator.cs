#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace DreamDrift.Editor
{
    public static class ProceduralAssetGenerator
    {
        public const string RootPath = "Assets/_DreamDrift";
        public const string SpritesPath = RootPath + "/Art/Sprites";
        public const string AudioPath = RootPath + "/Audio";
        public const string FontsPath = RootPath + "/Fonts";

        public static void GenerateAllAssets()
        {
            Directory.CreateDirectory(SpritesPath);
            Directory.CreateDirectory(AudioPath);
            Directory.CreateDirectory(FontsPath);

            GenerateSprites();
            GenerateAudioClips();

            AssetDatabase.Refresh();
            ConfigureSpriteImporters();
            GenerateTMPFonts();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DreamDrift] Procedural sprites, ambient WAV loops, and Thai/Eng TMP fonts generated successfully!");
        }

        private static void GenerateSprites()
        {
            // 1. Sky Vertical Gradient
            SaveTexture(SpritesPath + "/SkyGradient.png", CreateTexture(64, 512, (x, y, w, h) =>
            {
                float t = (float)y / (h - 1);
                Color bottom = new Color(0.75f, 0.85f, 1f, 1f);
                Color top = new Color(0.28f, 0.36f, 0.62f, 1f);
                return Color.Lerp(bottom, top, Mathf.Pow(t, 0.85f));
            }));

            // 2. Soft Radial Glow
            SaveTexture(SpritesPath + "/SoftGlow.png", CreateTexture(128, 128, (x, y, w, h) =>
            {
                float dx = (x + 0.5f) / w * 2f - 1f;
                float dy = (y + 0.5f) / h * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a;
                return new Color(1f, 1f, 1f, a);
            }));

            // 3. UI Circle
            SaveTexture(SpritesPath + "/UI_Circle.png", CreateTexture(128, 128, (x, y, w, h) =>
            {
                float dx = (x + 0.5f) / w * 2f - 1f;
                float dy = (y + 0.5f) / h * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((0.94f - d) * 28f);
                return new Color(1f, 1f, 1f, a);
            }));

            // 4. Breathing Ring Outline
            SaveTexture(SpritesPath + "/RingOutline.png", CreateTexture(256, 256, (x, y, w, h) =>
            {
                float dx = (x + 0.5f) / w * 2f - 1f;
                float dy = (y + 0.5f) / h * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = 1f - Mathf.Abs(d - 0.86f) / 0.055f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(ring * 2f));
            }));

            // 5. Crescent Moon (Cozy storybook warm cream)
            SaveTexture(SpritesPath + "/CrescentMoon.png", CreateTexture(256, 256, (x, y, w, h) =>
            {
                float dx = (x + 0.5f) / w * 2f - 1f;
                float dy = (y + 0.5f) / h * 2f - 1f;
                float outer = Mathf.Sqrt(dx * dx + dy * dy);
                float inner = Mathf.Sqrt((dx - 0.28f) * (dx - 0.28f) + (dy - 0.16f) * (dy - 0.16f));
                float moonA = Mathf.Clamp01((0.72f - outer) * 26f) * Mathf.Clamp01((inner - 0.58f) * 24f);
                float glowA = Mathf.Clamp01(1f - outer) * 0.22f;
                float a = Mathf.Max(moonA, glowA);
                Color col = Color.Lerp(new Color(1f, 0.94f, 0.72f, 1f), new Color(1f, 0.98f, 0.88f, 1f), moonA);
                col.a = a;
                return col;
            }));

            // 6. 4-Point Storybook Twinkling Star
            SaveTexture(SpritesPath + "/StarTwinkle.png", CreateTexture(64, 64, (x, y, w, h) =>
            {
                float dx = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float dy = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                float star = Mathf.Pow(dx, 0.55f) + Mathf.Pow(dy, 0.55f);
                float a = Mathf.Clamp01((0.88f - star) * 4.5f);
                return new Color(1f, 0.97f, 0.82f, a);
            }));

            // 7. Rain Drop Streak
            SaveTexture(SpritesPath + "/RainDrop.png", CreateTexture(16, 64, (x, y, w, h) =>
            {
                float dx = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float ty = (float)y / (h - 1);
                float a = Mathf.Clamp01((1f - dx * 1.4f)) * Mathf.Sin(ty * Mathf.PI);
                return new Color(0.82f, 0.92f, 1f, a * 0.85f);
            }));

            // 8. Mountain Silhouette Back
            SaveTexture(SpritesPath + "/MountainBack.png", CreateTexture(512, 256, (x, y, w, h) =>
            {
                float u = (float)x / w;
                float v = (float)y / h;
                float ridge = 0.36f
                    + 0.22f * Mathf.Sin(u * Mathf.PI * 2.2f + 0.4f)
                    + 0.14f * Mathf.Sin(u * Mathf.PI * 5.1f + 1.5f)
                    + 0.06f * Mathf.Sin(u * Mathf.PI * 11f);
                float a = Mathf.Clamp01((ridge - v) * 60f);
                return new Color(0.14f, 0.20f, 0.36f, a);
            }));

            // 9. Pine Forest Horizon
            SaveTexture(SpritesPath + "/PineForest.png", CreateTexture(512, 256, (x, y, w, h) =>
            {
                float u = (float)x / w;
                float v = (float)y / h;
                // Open gap in center for lake view
                float edgeMask = Mathf.Clamp01(Mathf.Abs(u - 0.5f) * 2.5f);
                float treeWave = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 18f)) * 0.24f + Mathf.Abs(Mathf.Sin(u * Mathf.PI * 31f + 0.7f)) * 0.12f;
                float height = 0.12f + edgeMask * (0.28f + treeWave);
                float a = Mathf.Clamp01((height - v) * 65f);
                return new Color(0.06f, 0.10f, 0.21f, a);
            }));

            // 10. Lake Surface
            SaveTexture(SpritesPath + "/LakeSurface.png", CreateTexture(256, 128, (x, y, w, h) =>
            {
                float v = (float)y / h;
                Color deep = new Color(0.16f, 0.28f, 0.48f, 1f);
                Color horizon = new Color(0.32f, 0.48f, 0.72f, 1f);
                return Color.Lerp(deep, horizon, v);
            }));

            // 11. Campsite Foreground Shore
            SaveTexture(SpritesPath + "/CampsiteGround.png", CreateTexture(512, 256, (x, y, w, h) =>
            {
                float u = (float)x / w;
                float v = (float)y / h;
                float hill = 0.52f + 0.14f * Mathf.Cos((u - 0.5f) * Mathf.PI * 1.4f) - 0.06f * Mathf.Sin(u * Mathf.PI * 3f);
                float a = Mathf.Clamp01((hill - v) * 50f);
                Color col = Color.Lerp(new Color(0.07f, 0.08f, 0.14f, 1f), new Color(0.16f, 0.14f, 0.20f, 1f), v);
                col.a = a;
                return col;
            }));

            // 12. Cozy Tent
            SaveTexture(SpritesPath + "/CozyTent.png", CreateTexture(256, 192, (x, y, w, h) =>
            {
                float u = (float)x / w;
                float v = (float)y / h;
                float peak = 1f - Mathf.Abs(u - 0.5f) * 2.25f;
                float tentA = (v < peak * 0.85f && v > 0.08f) ? 1f : 0f;
                // Warm glowing doorway triangle
                float doorPeak = 1f - Mathf.Abs(u - 0.54f) * 5.2f;
                bool isDoor = v < doorPeak * 0.55f && v > 0.08f;
                if (tentA < 0.5f) return Color.clear;
                return isDoor ? new Color(1f, 0.78f, 0.40f, 1f) : new Color(0.18f, 0.15f, 0.24f, 1f);
            }));

            // 13. Campfire Logs
            SaveTexture(SpritesPath + "/CampfireLogs.png", CreateTexture(160, 80, (x, y, w, h) =>
            {
                float u = (float)x / w * 2f - 1f;
                float v = (float)y / h * 2f - 1f;
                float d1 = Mathf.Abs(v - u * 0.28f);
                float d2 = Mathf.Abs(v + u * 0.28f);
                bool log = (Mathf.Abs(u) < 0.75f && (d1 < 0.22f || d2 < 0.22f));
                return log ? new Color(0.32f, 0.18f, 0.11f, 1f) : Color.clear;
            }));

            // 14. Campfire Flame
            SaveTexture(SpritesPath + "/CampfireFlame.png", CreateTexture(128, 160, (x, y, w, h) =>
            {
                float u = (float)x / w * 2f - 1f;
                float v = (float)y / h;
                float widthAtV = Mathf.Sin(Mathf.Pow(v, 0.65f) * Mathf.PI) * 0.65f;
                float d = Mathf.Abs(u) / Mathf.Max(0.01f, widthAtV);
                float a = Mathf.Clamp01((1f - d) * 3.2f) * Mathf.Clamp01(v * 6f);
                Color outer = new Color(1f, 0.48f, 0.12f, a);
                Color inner = new Color(1f, 0.92f, 0.45f, a);
                return Color.Lerp(outer, inner, Mathf.Clamp01(1f - d * 1.4f - v * 0.45f));
            }));

            // 15. Fluffy Storybook Cloud
            SaveTexture(SpritesPath + "/CloudSoft.png", CreateTexture(256, 128, (x, y, w, h) =>
            {
                float u = (float)x / w;
                float v = (float)y / h;
                float c1 = 1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.32f, 0.42f)) * 3.8f;
                float c2 = 1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.52f, 0.54f)) * 3.1f;
                float c3 = 1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.70f, 0.40f)) * 4.0f;
                float a = Mathf.Clamp01(Mathf.Max(c1, Mathf.Max(c2, c3)) * 5f);
                if (v < 0.24f) a *= Mathf.Clamp01((v - 0.15f) * 12f);
                return new Color(0.55f, 0.52f, 0.78f, a * 0.55f);
            }));

            // 16. Thai Sky Lantern (Worry Lantern)
            SaveTexture(SpritesPath + "/WorryLantern.png", CreateTexture(192, 240, (x, y, w, h) =>
            {
                float u = (float)x / w * 2f - 1f;
                float v = (float)y / h;
                // Slightly wider at top, gently tapered at bottom
                float halfW = Mathf.Lerp(0.46f, 0.74f, Mathf.Sin(Mathf.Clamp01(v) * Mathf.PI * 0.82f));
                bool inBody = v > 0.16f && v < 0.90f && Mathf.Abs(u) < halfW;
                if (!inBody)
                {
                    // Soft golden halo outside
                    float dist = Vector2.Distance(new Vector2(u, (v - 0.5f) * 1.2f), Vector2.zero);
                    float halo = Mathf.Clamp01(1f - dist * 0.95f);
                    return new Color(1f, 0.68f, 0.22f, halo * halo * 0.35f);
                }

                float centerGlow = 1f - Mathf.Sqrt(u * u * 0.9f + (v - 0.35f) * (v - 0.35f) * 2.2f);
                Color rim = new Color(0.95f, 0.44f, 0.12f, 0.96f);
                Color core = new Color(1f, 0.94f, 0.62f, 1f);
                Color col = Color.Lerp(rim, core, Mathf.Clamp01(centerGlow));
                // Subtle lantern ribs
                if (Mathf.Abs(u) > halfW - 0.04f || v < 0.20f)
                {
                    col = Color.Lerp(col, new Color(0.55f, 0.22f, 0.08f, 1f), 0.45f);
                }
                return col;
            }));

            // 17. Luxury Frosted Glass Card (9-sliced: subtle frost gradient + luminous top-edge rim)
            SaveTexture(SpritesPath + "/UI_RoundedCard.png", CreateGlassRect(160, 160, 40f, 2.2f, 0.24f, 0.08f, true));

            // 18. Luxury Frosted Glass Pill (9-sliced)
            SaveTexture(SpritesPath + "/UI_RoundedPill.png", CreateGlassRect(160, 112, 52f, 2.0f, 0.26f, 0.10f, true));

            // 18b. Delicate Outlined Emotion Chip (9-sliced: airy low fill + crisp moonlight rim)
            SaveTexture(SpritesPath + "/UI_OutlineChip.png", CreateGlassRect(160, 104, 48f, 2.2f, 0.34f, 0.18f, false));

            // 18c. Smooth Gradient Glow Button (9-sliced: soft top sheen + subtle depth)
            SaveTexture(SpritesPath + "/UI_GradientBtn.png", CreateGradientPill(160, 112, 52f));

            // 19. Swipe Up Arrow Icon
            SaveTexture(SpritesPath + "/Icon_SwipeUp.png", CreateTexture(96, 128, (x, y, w, h) =>
            {
                float u = (float)x / w * 2f - 1f;
                float v = (float)y / h;
                bool shaft = Mathf.Abs(u) < 0.14f && v > 0.12f && v < 0.68f;
                bool head = v >= 0.58f && v <= 0.92f && Mathf.Abs(u) < (0.92f - v) * 1.9f;
                if (!shaft && !head) return Color.clear;
                float fade = Mathf.Clamp01(v * 1.3f);
                return new Color(1f, 0.95f, 0.80f, fade);
            }));

            // 20. Simple Minimalist Cozy Icons (Rain, Fire, Wave, Cricket)
            SaveTexture(SpritesPath + "/Icon_Rain.png", CreateRainIcon(96));
            SaveTexture(SpritesPath + "/Icon_Fire.png", CreateFireIcon(96));
            SaveTexture(SpritesPath + "/Icon_Wave.png", CreateWaveIcon(96));
            SaveTexture(SpritesPath + "/Icon_Cricket.png", CreateCricketIcon(96));
        }

        private static Texture2D CreateGlassRect(int w, int h, float radius, float borderThickness, float topRimAlpha, float bottomRimAlpha, bool solidTintableFill)
        {
            return CreateTexture(w, h, (x, y, tw, th) =>
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, tw - radius);
                float cy = Mathf.Clamp(py, radius, th - radius);
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float shapeAlpha = Mathf.Clamp01((radius - dist) * 1.6f);
                if (shapeAlpha <= 0.001f) return Color.clear;

                float v = (float)y / (th - 1); // 0 bottom -> 1 top
                bool isBorder = dist > (radius - borderThickness);

                if (isBorder)
                {
                    float rimStrength = Mathf.Lerp(bottomRimAlpha, topRimAlpha, Mathf.Pow(v, 1.3f));
                    return new Color(0.88f, 0.91f, 1f, Mathf.Clamp01(shapeAlpha * (0.45f + rimStrength)));
                }

                if (solidTintableFill)
                {
                    // Subtle vertical depth (slightly lighter top frost, deeper bottom)
                    float lum = Mathf.Lerp(0.86f, 1.0f, v);
                    return new Color(lum, lum, 1f, shapeAlpha);
                }
                else
                {
                    // Airy semi-transparent chip interior
                    return new Color(1f, 1f, 1f, shapeAlpha * 0.32f);
                }
            });
        }

        private static Texture2D CreateGradientPill(int w, int h, float radius)
        {
            return CreateTexture(w, h, (x, y, tw, th) =>
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, tw - radius);
                float cy = Mathf.Clamp(py, radius, th - radius);
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                float shapeAlpha = Mathf.Clamp01((radius - dist) * 1.6f);
                if (shapeAlpha <= 0.001f) return Color.clear;

                float u = (float)x / (tw - 1);
                float v = (float)y / (th - 1);
                // Soft diagonal sheen for human-crafted tactile depth
                float sheen = Mathf.Lerp(0.88f, 1.06f, v * 0.7f + (1f - u) * 0.3f);
                bool topHighlight = dist > (radius - 2.2f) && v > 0.5f;
                if (topHighlight) sheen = 1.15f;
                return new Color(Mathf.Min(1f, sheen), Mathf.Min(1f, sheen), Mathf.Min(1f, sheen), shapeAlpha);
            });
        }

        private static Texture2D CreateRainIcon(int size)
        {
            return CreateTexture(size, size, (x, y, w, h) =>
            {
                float u = (float)x / w;
                float v = (float)y / h;
                float c1 = 1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.35f, 0.64f)) * 5.2f;
                float c2 = 1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.52f, 0.72f)) * 4.5f;
                float c3 = 1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.68f, 0.62f)) * 5.6f;
                bool cloud = Mathf.Max(c1, Mathf.Max(c2, c3)) > 0f && v > 0.50f;
                bool d1 = Mathf.Abs(u - 0.34f) < 0.035f && v > 0.20f && v < 0.42f;
                bool d2 = Mathf.Abs(u - 0.50f) < 0.035f && v > 0.14f && v < 0.38f;
                bool d3 = Mathf.Abs(u - 0.66f) < 0.035f && v > 0.22f && v < 0.42f;
                return (cloud || d1 || d2 || d3) ? Color.white : Color.clear;
            });
        }

        private static Texture2D CreateFireIcon(int size)
        {
            return CreateTexture(size, size, (x, y, w, h) =>
            {
                float u = (float)x / w * 2f - 1f;
                float v = (float)y / h;
                float widthAtV = Mathf.Sin(Mathf.Pow(Mathf.Clamp01((v - 0.12f) / 0.78f), 0.7f) * Mathf.PI) * 0.55f;
                bool flame = v > 0.15f && v < 0.88f && Mathf.Abs(u) < widthAtV;
                bool innerCut = v > 0.24f && v < 0.52f && Mathf.Abs(u) < widthAtV * 0.38f;
                return (flame && !innerCut) ? Color.white : Color.clear;
            });
        }

        private static Texture2D CreateWaveIcon(int size)
        {
            return CreateTexture(size, size, (x, y, w, h) =>
            {
                float u = (float)x / w;
                float v = (float)y / h;
                if (u < 0.12f || u > 0.88f) return Color.clear;
                float w1 = 0.62f + 0.08f * Mathf.Sin(u * Mathf.PI * 3.5f);
                float w2 = 0.36f + 0.08f * Mathf.Sin(u * Mathf.PI * 3.5f + 1.1f);
                bool onWave = Mathf.Abs(v - w1) < 0.055f || Mathf.Abs(v - w2) < 0.055f;
                return onWave ? Color.white : Color.clear;
            });
        }

        private static Texture2D CreateCricketIcon(int size)
        {
            // Minimalist crescent moon + star icon for night crickets/fireflies
            return CreateTexture(size, size, (x, y, w, h) =>
            {
                float u = (float)x / w * 2f - 1f;
                float v = (float)y / h * 2f - 1f;
                float dOuter = Vector2.Distance(new Vector2(u, v), new Vector2(-0.1f, 0f));
                float dInner = Vector2.Distance(new Vector2(u, v), new Vector2(0.18f, 0.14f));
                bool moon = dOuter < 0.62f && dInner > 0.48f;
                float starDist = Mathf.Abs(u - 0.45f) + Mathf.Abs(v - 0.45f);
                bool star = starDist < 0.20f;
                return (moon || star) ? Color.white : Color.clear;
            });
        }

        private static void GenerateAudioClips()
        {
            const int sampleRate = 22050;
            const int loopSeconds = 6;
            int totalSamples = sampleRate * loopSeconds;

            // 1. Rain Loop (filtered pink-ish soft rain)
            float[] rain = new float[totalSamples];
            System.Random rng = new System.Random(101);
            float b0 = 0, b1 = 0, b2 = 0;
            for (int i = 0; i < totalSamples; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                b0 = 0.997f * b0 + white * 0.029f;
                b1 = 0.985f * b1 + white * 0.032f;
                b2 = 0.950f * b2 + white * 0.048f;
                float sample = (b0 + b1 + b2 + white * 0.06f) * 0.45f;
                rain[i] = Mathf.Clamp(sample, -0.8f, 0.8f);
            }
            ApplySeamlessLoopFade(rain, sampleRate / 4);
            SaveWav(AudioPath + "/RainLoop.wav", rain, sampleRate);

            // 2. Campfire Loop (warm low rumble + crisp crackles)
            float[] fire = new float[totalSamples];
            float lp = 0f;
            float crackleEnv = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp = lp * 0.96f + white * 0.04f;
                if (rng.NextDouble() < 0.0014) crackleEnv = (float)(0.4 + rng.NextDouble() * 0.6);
                crackleEnv *= 0.975f;
                float pop = crackleEnv * white * 0.55f;
                fire[i] = Mathf.Clamp(lp * 2.4f + pop, -0.85f, 0.85f);
            }
            ApplySeamlessLoopFade(fire, sampleRate / 4);
            SaveWav(AudioPath + "/CampfireLoop.wav", fire, sampleRate);

            // 3. Ocean Waves Loop (gentle rhythmic swell)
            float[] wave = new float[totalSamples];
            float wlp = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float swell = 0.35f + 0.65f * (0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 2f));
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                wlp = wlp * 0.92f + white * 0.08f;
                wave[i] = Mathf.Clamp(wlp * swell * 1.9f, -0.85f, 0.85f);
            }
            ApplySeamlessLoopFade(wave, sampleRate / 4);
            SaveWav(AudioPath + "/OceanWaveLoop.wav", wave, sampleRate);

            // 4. Night Crickets Loop (soft rhythmic 4.1kHz chirps)
            float[] cricket = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float sec = (float)i / sampleRate;
                float chirpGate = Mathf.Max(0f, Mathf.Sin(sec * Mathf.PI * 2.5f));
                float pulse = Mathf.Max(0f, Mathf.Sin(sec * Mathf.PI * 38f));
                float tone = Mathf.Sin(sec * Mathf.PI * 2f * 4100f) * 0.6f + Mathf.Sin(sec * Mathf.PI * 2f * 4180f) * 0.4f;
                cricket[i] = tone * chirpGate * chirpGate * pulse * 0.18f;
            }
            ApplySeamlessLoopFade(cricket, sampleRate / 4);
            SaveWav(AudioPath + "/CricketLoop.wav", cricket, sampleRate);

            // 5. Gentle Bell Chime SFX (1.8 seconds)
            int chimeSamples = (int)(sampleRate * 1.8f);
            float[] chime = new float[chimeSamples];
            for (int i = 0; i < chimeSamples; i++)
            {
                float sec = (float)i / sampleRate;
                float env = Mathf.Exp(-sec * 2.8f) * Mathf.Clamp01(sec * 80f);
                float tone = Mathf.Sin(sec * Mathf.PI * 2f * 528f) * 0.55f
                           + Mathf.Sin(sec * Mathf.PI * 2f * 792f) * 0.25f
                           + Mathf.Sin(sec * Mathf.PI * 2f * 1056f) * 0.12f;
                chime[i] = tone * env * 0.55f;
            }
            SaveWav(AudioPath + "/GentleChime.wav", chime, sampleRate);
        }

        private static void ApplySeamlessLoopFade(float[] samples, int fadeSamples)
        {
            int len = samples.Length;
            fadeSamples = Mathf.Min(fadeSamples, len / 4);
            for (int i = 0; i < fadeSamples; i++)
            {
                float t = (float)i / fadeSamples;
                samples[i] = Mathf.Lerp(samples[len - fadeSamples + i], samples[i], t);
            }
        }

        private static void ConfigureSpriteImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;

                if (path.EndsWith("UI_RoundedCard.png"))
                    importer.spriteBorder = new Vector4(42, 42, 42, 42);
                else if (path.EndsWith("UI_RoundedPill.png") || path.EndsWith("UI_GradientBtn.png"))
                    importer.spriteBorder = new Vector4(54, 42, 54, 42);
                else if (path.EndsWith("UI_OutlineChip.png"))
                    importer.spriteBorder = new Vector4(50, 38, 50, 38);

                importer.SaveAndReimport();
            }
        }

        private static void GenerateTMPFonts()
        {
            // Clean Thai/Eng fallback (Leelawadee UI) for any rare symbol
            TMP_FontAsset fallbackUi = CreateDynamicTMPFont(FontsPath + "/LeelawUI.ttf", FontsPath + "/LeelawUI SDF.asset", 0.02f);

            // MaliThin-Regular.ttf crafted into 2 crisp weights (clean counters + clear tone marks)
            TMP_FontAsset maliRegular = CreateDynamicTMPFont(FontsPath + "/MaliThin-Regular.ttf", FontsPath + "/Mali-SoftRegular SDF.asset", 0.11f);
            TMP_FontAsset maliMedium = CreateDynamicTMPFont(FontsPath + "/MaliThin-Regular.ttf", FontsPath + "/Mali-SoftMedium SDF.asset", 0.17f);

            if (fallbackUi != null)
            {
                if (maliRegular != null)
                {
                    maliRegular.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallbackUi };
                    EditorUtility.SetDirty(maliRegular);
                }
                if (maliMedium != null)
                {
                    maliMedium.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallbackUi };
                    EditorUtility.SetDirty(maliMedium);
                }
            }

            // Cute Bedtime Storybook Display font (TA GonNon FreeHand) for Hero & Screen Titles
            TMP_FontAsset displayBold = CreateDynamicTMPFont(FontsPath + "/TAGonNonFreeHand-Bold.ttf", FontsPath + "/TAGonNonFreeHand-Bold SDF.asset", -0.04f);
            if (displayBold != null && maliMedium != null)
            {
                displayBold.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { maliMedium };
                EditorUtility.SetDirty(displayBold);
            }
        }

        private static string BuildThaiAndAsciiCharset()
        {
            var sb = new System.Text.StringBuilder(256);
            // Printable ASCII (32..126)
            for (int c = 32; c <= 126; c++) sb.Append((char)c);
            // Full Thai Unicode block (U+0E01..U+0E5B: all consonants, vowels, tone marks ่ ้ ๊ ๋ ั ิ ี ึ ื ุ ู ็ ์ ำ)
            for (int c = 0x0E01; c <= 0x0E5B; c++) sb.Append((char)c);
            // Common bullets/dashes/ellipsis
            sb.Append("•–—…“”‘’");
            return sb.ToString();
        }

        private static TMP_FontAsset CreateDynamicTMPFont(string sourceTtfPath, string targetAssetPath, float faceDilate)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(targetAssetPath);
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceTtfPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"[DreamDrift] Font not found at {sourceTtfPath}");
                return null;
            }

            // High-res 2048x2048 SDF atlas (110pt, padding 12) so delicate Thai vowels & tone marks are razor sharp
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                110,
                12,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null) return null;

            fontAsset.isMultiAtlasTexturesEnabled = true;

            // Pre-populate ALL Thai consonants, vowels, tone marks & OpenType GPOS mark/mkmk features!
            fontAsset.TryAddCharacters(BuildThaiAndAsciiCharset(), out _, true);

            AssetDatabase.CreateAsset(fontAsset, targetAssetPath);

            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    if (fontAsset.atlasTextures[i] != null)
                    {
                        fontAsset.atlasTextures[i].name = fontAsset.name + " Atlas " + i;
                        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                    }
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                if (Mathf.Abs(faceDilate) > 0.001f && fontAsset.material.HasProperty(ShaderUtilities.ID_FaceDilate))
                {
                    fontAsset.material.SetFloat(ShaderUtilities.ID_FaceDilate, faceDilate);
                }
                if (fontAsset.material.HasProperty(ShaderUtilities.ID_OutlineSoftness))
                {
                    fontAsset.material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.0f);
                }
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static Texture2D CreateTexture(int width, int height, Func<int, int, int, int, Color> pixelFunc)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = pixelFunc(x, y, width, height);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static void SaveTexture(string path, Texture2D tex)
        {
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(path, png);
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void SaveWav(string path, float[] samples, int sampleRate)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int byteRate = sampleRate * 2;
                int dataSize = samples.Length * 2;

                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1); // PCM
                bw.Write((short)1); // Mono
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)2); // BlockAlign
                bw.Write((short)16); // BitsPerSample
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);

                for (int i = 0; i < samples.Length; i++)
                {
                    short val = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                    bw.Write(val);
                }
            }
        }
    }
}
#endif
