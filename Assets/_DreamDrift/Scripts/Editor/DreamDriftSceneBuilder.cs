#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TextCore;
using TMPro;
using DreamDrift.AI;
using DreamDrift.Atmosphere;
using DreamDrift.Audio;
using DreamDrift.Core;
using DreamDrift.Minigames;
using DreamDrift.UI;

namespace DreamDrift.Editor
{
    public static class DreamDriftSceneBuilder
    {
        private const string ScenePath = "Assets/_DreamDrift/Scenes/DreamDrift_Main.unity";
        private const string PrefabsPath = "Assets/_DreamDrift/Prefabs";

        // Midnight-Aura Sleep App Palette (Calm / Loona / BetterSleep inspired)
        private static readonly Color GlassSheetBg = new Color(0.055f, 0.075f, 0.145f, 0.80f);
        private static readonly Color GlassSurfaceBg = new Color(0.115f, 0.150f, 0.265f, 0.68f);
        private static readonly Color DockBg = new Color(0.050f, 0.068f, 0.135f, 0.86f);
        private static readonly Color SoftOffWhite = new Color(0.935f, 0.925f, 0.975f, 1f);
        private static readonly Color MutedSilver = new Color(0.675f, 0.715f, 0.845f, 0.92f);
        private static readonly Color TwilightAmber = new Color(0.905f, 0.725f, 0.415f, 0.96f);
        private static readonly Color DreamyIndigo = new Color(0.495f, 0.485f, 0.895f, 0.94f);
        private static readonly Color SoftSageMint = new Color(0.515f, 0.855f, 0.775f, 1f);
        private static readonly Color DustyPeach = new Color(0.925f, 0.625f, 0.565f, 1f);
        private static readonly Color DeepInk = new Color(0.065f, 0.085f, 0.165f, 1f);

        private static TMP_FontAsset fontBody;
        private static TMP_FontAsset fontBodyBold;
        private static TMP_FontAsset fontDisplay;
        private static Sprite spriteCard;
        private static Sprite spritePill;
        private static Sprite spriteOutlineChip;
        private static Sprite spriteGradientBtn;
        private static Sprite spriteCircle;

        [MenuItem("Tools/DreamDrift/Build Complete Project (One-Click)")]
        public static void BuildCompleteProject()
        {
            ProceduralAssetGenerator.GenerateAllAssets();

            Directory.CreateDirectory("Assets/_DreamDrift/Scenes");
            Directory.CreateDirectory(PrefabsPath);

            fontBody = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProceduralAssetGenerator.FontsPath + "/Mali-SoftRegular SDF.asset");
            fontBodyBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProceduralAssetGenerator.FontsPath + "/Mali-SoftMedium SDF.asset");
            fontDisplay = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProceduralAssetGenerator.FontsPath + "/TAGonNonFreeHand-Bold SDF.asset");
            if (fontBodyBold == null) fontBodyBold = fontBody;
            if (fontDisplay == null) fontDisplay = fontBodyBold;

            spriteCard = LoadSprite("UI_RoundedCard");
            spritePill = LoadSprite("UI_RoundedPill");
            spriteOutlineChip = LoadSprite("UI_OutlineChip");
            spriteGradientBtn = LoadSprite("UI_GradientBtn");
            spriteCircle = LoadSprite("UI_Circle");

            WorryLanternItem lanternPrefab = CreateWorryLanternPrefab();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Main Camera (Portrait 9:16 friendly)
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.6f;
            cam.backgroundColor = new Color(0.04f, 0.07f, 0.16f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            // 2. Managers Root
            GameObject managersGo = new GameObject("DreamDrift_Managers");
            DreamDriftManager manager = managersGo.AddComponent<DreamDriftManager>();
            GeminiMoodService aiService = managersGo.AddComponent<GeminiMoodService>();
            SoundscapeManager soundscape = managersGo.AddComponent<SoundscapeManager>();
            managersGo.AddComponent<ThaiTMPShaper>();

            // 3. 2D Bedtime World + URP 2D Lights + Particle Systems
            AtmosphereController atmosphere = Build2DWorld(out Transform lanternContainer);

            // 4. Audio Sources
            SetupAudioSources(managersGo, soundscape);

            // Wire Core Managers
            manager.mainCamera = cam;
            manager.atmosphere = atmosphere;
            manager.soundscape = soundscape;
            soundscape.atmosphere = atmosphere;

            // 5. EventSystem (compatible with New Input System or Legacy)
            CreateEventSystem();

            // 6. Full Mobile Portrait UI Canvas
            BuildUI(cam, aiService, soundscape, lanternPrefab, lanternContainer);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DreamDrift] Complete Unity 6 URP 2D project built successfully! Press Play to test DreamDrift.");
        }

        private static AtmosphereController Build2DWorld(out Transform lanternContainer)
        {
            GameObject world = new GameObject("DreamDrift_2DWorld");
            AtmosphereController atm = world.AddComponent<AtmosphereController>();

            // Sky Dome spanning both Home (y=0) and Worry Lantern Sky (y=8.5)
            SpriteRenderer sky = CreateSpriteObj("SkyGradient", world.transform, LoadSprite("SkyGradient"), new Vector3(0, 4.5f, 0), new Vector3(14f, 26f, 1f), -10, Color.white);
            atm.skyRenderer = sky;

            // Crescent Moon (refined size matching mockup)
            SpriteRenderer moon = CreateSpriteObj("CrescentMoon", world.transform, LoadSprite("CrescentMoon"), new Vector3(1.85f, 3.65f, 0), new Vector3(0.58f, 0.58f, 1f), -8, Color.white);
            atm.moonRenderer = moon;

            // Twinkling Stars scattered across both lower & upper sky
            GameObject starsRoot = new GameObject("Stars");
            starsRoot.transform.SetParent(world.transform);
            Sprite starSpr = LoadSprite("StarTwinkle");
            System.Random rng = new System.Random(42);
            for (int i = 0; i < 42; i++)
            {
                float sx = (float)(rng.NextDouble() * 8.4 - 4.2);
                float sy = (float)(rng.NextDouble() * 14.5 + 0.8);
                float sc = (float)(0.08 + rng.NextDouble() * 0.16);
                CreateSpriteObj($"Star_{i}", starsRoot.transform, starSpr, new Vector3(sx, sy, 0), new Vector3(sc, sc, 1f), -9, new Color(1f, 0.96f, 0.8f, 0.85f));
            }

            // Drifting Soft Clouds (especially cozy in the upper Worry Lantern sky!)
            Sprite cloudSpr = LoadSprite("CloudSoft");
            Transform[] clouds = new Transform[5];
            Vector3[] cloudPositions =
            {
                new Vector3(-2.4f, 6.2f, 0),
                new Vector3(2.2f, 7.8f, 0),
                new Vector3(-1.5f, 10.2f, 0),
                new Vector3(2.6f, 11.4f, 0),
                new Vector3(0.0f, 4.4f, 0)
            };
            for (int i = 0; i < clouds.Length; i++)
            {
                float scale = 1.6f + (i % 2) * 0.7f;
                SpriteRenderer sr = CreateSpriteObj($"Cloud_{i}", world.transform, cloudSpr, cloudPositions[i], new Vector3(scale, scale * 0.75f, 1f), -7, new Color(0.65f, 0.62f, 0.90f, 0.65f));
                clouds[i] = sr.transform;
            }
            atm.driftingClouds = clouds;

            // Background Mountains
            CreateSpriteObj("Mountains", world.transform, LoadSprite("MountainBack"), new Vector3(0f, -0.35f, 0), new Vector3(2.4f, 2.2f, 1f), -6, new Color(0.75f, 0.82f, 1f, 1f));

            // Lake Surface
            SpriteRenderer lake = CreateSpriteObj("LakeSurface", world.transform, LoadSprite("LakeSurface"), new Vector3(0f, -1.85f, 0), new Vector3(4.5f, 1.9f, 1f), -5, new Color(0.22f, 0.36f, 0.58f, 0.92f));
            atm.lakeRenderer = lake;

            // Pine Forest Framing
            CreateSpriteObj("PineForest", world.transform, LoadSprite("PineForest"), new Vector3(0f, -0.4f, 0), new Vector3(2.2f, 2.4f, 1f), -4, Color.white);

            // Foreground Campsite Ground
            CreateSpriteObj("CampsiteGround", world.transform, LoadSprite("CampsiteGround"), new Vector3(0f, -4.3f, 0), new Vector3(2.3f, 1.9f, 1f), -2, Color.white);

            // Cozy Tent
            CreateSpriteObj("CozyTent", world.transform, LoadSprite("CozyTent"), new Vector3(-1.8f, -3.05f, 0), new Vector3(0.85f, 0.85f, 1f), -1, Color.white);

            // Campfire Logs + Flame
            CreateSpriteObj("CampfireLogs", world.transform, LoadSprite("CampfireLogs"), new Vector3(0.15f, -3.55f, 0), new Vector3(0.62f, 0.62f, 1f), 0, Color.white);
            SpriteRenderer flame = CreateSpriteObj("CampfireFlame", world.transform, LoadSprite("CampfireFlame"), new Vector3(0.15f, -3.22f, 0), new Vector3(0.55f, 0.62f, 1f), 1, Color.white);
            atm.campfireFlameRenderer = flame;

            // URP 2D Lights
            GameObject lightsRoot = new GameObject("URP_2D_Lights");
            lightsRoot.transform.SetParent(world.transform);

            GameObject globalLightGo = new GameObject("GlobalLight2D");
            globalLightGo.transform.SetParent(lightsRoot.transform);
            Light2D globalLight = globalLightGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 0.78f;
            globalLight.color = new Color(0.68f, 0.78f, 1f, 1f);
            atm.globalLight2D = globalLight;

            GameObject fireLightGo = new GameObject("CampfireLight2D");
            fireLightGo.transform.SetParent(lightsRoot.transform);
            fireLightGo.transform.position = new Vector3(0.15f, -3.15f, 0f);
            Light2D fireLight = fireLightGo.AddComponent<Light2D>();
            fireLight.lightType = Light2D.LightType.Point;
            fireLight.pointLightOuterRadius = 3.6f;
            fireLight.pointLightInnerRadius = 0.4f;
            fireLight.intensity = 1.4f;
            fireLight.color = new Color(1f, 0.64f, 0.28f, 1f);
            atm.campfireLight2D = fireLight;

            GameObject moonLightGo = new GameObject("MoonLight2D");
            moonLightGo.transform.SetParent(lightsRoot.transform);
            moonLightGo.transform.position = moon.transform.position;
            Light2D moonLight = moonLightGo.AddComponent<Light2D>();
            moonLight.lightType = Light2D.LightType.Point;
            moonLight.pointLightOuterRadius = 4.2f;
            moonLight.intensity = 0.55f;
            moonLight.color = new Color(1f, 0.94f, 0.75f, 1f);
            atm.moonLight2D = moonLight;

            // Particle Systems (Rain, Embers, Fireflies, Water Ripples)
            GameObject vfxRoot = new GameObject("NatureVFX");
            vfxRoot.transform.SetParent(world.transform);

            Material softGlowMat = CreateParticleMaterial("SoftGlow");
            Material rainMat = CreateParticleMaterial("RainDrop");

            atm.rainParticles = CreateRainParticles(vfxRoot.transform, rainMat);
            atm.campfireEmbers = CreateEmberParticles(vfxRoot.transform, softGlowMat);
            atm.fireflyParticles = CreateFireflyParticles(vfxRoot.transform, softGlowMat);
            atm.waterRipples = CreateWaterRippleParticles(vfxRoot.transform, softGlowMat);

            // Container for launched Worry Lanterns
            GameObject lanternHolder = new GameObject("WorryLanterns_Active");
            lanternHolder.transform.SetParent(world.transform);
            lanternContainer = lanternHolder.transform;

            return atm;
        }

        private static ParticleSystem CreateRainParticles(Transform parent, Material mat)
        {
            GameObject go = new GameObject("RainParticles");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, 14f, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2.2f;
            main.startSpeed = 11f;
            main.startSize = 0.35f;
            main.startColor = new Color(0.75f, 0.88f, 1f, 0.45f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9f, 0.5f, 1f);
            go.transform.rotation = Quaternion.Euler(0, 0, -98f);

            var emission = ps.emission;
            emission.rateOverTime = 50f;

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = mat;
            rend.sortingOrder = 5;
            return ps;
        }

        private static ParticleSystem CreateEmberParticles(Transform parent, Material mat)
        {
            GameObject go = new GameObject("CampfireEmbers");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0.15f, -3.1f, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2.4f;
            main.startSpeed = 0.7f;
            main.startSize = 0.16f;
            main.startColor = new Color(1f, 0.72f, 0.25f, 0.9f);
            main.gravityModifier = -0.15f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = mat;
            rend.sortingOrder = 4;
            return ps;
        }

        private static ParticleSystem CreateFireflyParticles(Transform parent, Material mat)
        {
            GameObject go = new GameObject("FireflyParticles");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, -1.8f, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 5.5f;
            main.startSpeed = 0.22f;
            main.startSize = 0.22f;
            main.startColor = new Color(0.95f, 1f, 0.58f, 0.8f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(6.5f, 3.2f, 1f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.6f;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = mat;
            rend.sortingOrder = 4;
            return ps;
        }

        private static ParticleSystem CreateWaterRippleParticles(Transform parent, Material mat)
        {
            GameObject go = new GameObject("WaterRipples");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, -1.9f, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2.6f;
            main.startSpeed = 0.15f;
            main.startSize = 0.45f;
            main.startColor = new Color(0.75f, 0.90f, 1f, 0.22f);

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5.5f, 0.8f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = mat;
            rend.sortingOrder = -3;
            return ps;
        }

        private static Material CreateParticleMaterial(string spriteName)
        {
            Directory.CreateDirectory("Assets/_DreamDrift/Art/Materials");
            string matPath = $"Assets/_DreamDrift/Art/Materials/Mat_{spriteName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Sprites/Default");
                mat = new Material(shader);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ProceduralAssetGenerator.SpritesPath}/{spriteName}.png");
                if (tex != null)
                {
                    mat.mainTexture = tex;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                }
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        private static void SetupAudioSources(GameObject host, SoundscapeManager soundscape)
        {
            soundscape.rainSource = CreateAudioSource(host, "Audio_Rain", "RainLoop", true);
            soundscape.campfireSource = CreateAudioSource(host, "Audio_Campfire", "CampfireLoop", true);
            soundscape.waveSource = CreateAudioSource(host, "Audio_Waves", "OceanWaveLoop", true);
            soundscape.cricketSource = CreateAudioSource(host, "Audio_Crickets", "CricketLoop", true);
            soundscape.sfxSource = CreateAudioSource(host, "Audio_SFX", null, false);
            soundscape.chimeClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProceduralAssetGenerator.AudioPath + "/GentleChime.wav");
        }

        private static AudioSource CreateAudioSource(GameObject parent, string name, string clipName, bool loop)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.loop = loop;
            src.playOnAwake = loop;
            src.spatialBlend = 0f;
            if (!string.IsNullOrEmpty(clipName))
            {
                src.clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{ProceduralAssetGenerator.AudioPath}/{clipName}.wav");
            }
            return src;
        }

        private static WorryLanternItem CreateWorryLanternPrefab()
        {
            string prefabPath = PrefabsPath + "/WorryLantern.prefab";
            GameObject root = new GameObject("WorryLantern");
            WorryLanternItem item = root.AddComponent<WorryLanternItem>();

            SpriteRenderer lanternSr = root.AddComponent<SpriteRenderer>();
            lanternSr.sprite = LoadSprite("WorryLantern");
            lanternSr.sortingOrder = 6;
            root.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            item.lanternSprite = lanternSr;

            GameObject glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            SpriteRenderer glowSr = glowGo.AddComponent<SpriteRenderer>();
            glowSr.sprite = LoadSprite("SoftGlow");
            glowSr.color = new Color(1f, 0.75f, 0.32f, 0.55f);
            glowSr.sortingOrder = 5;
            glowGo.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
            item.glowSprite = glowSr;

            GameObject textGo = new GameObject("WorryText");
            textGo.transform.SetParent(root.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0.05f, -0.05f);
            TextMeshPro tmp = textGo.AddComponent<TextMeshPro>();
            if (fontBodyBold != null) tmp.font = fontBodyBold;
            tmp.fontSize = 2.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.26f, 0.12f, 0.04f, 0.95f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = new Vector2(1.4f, 1.2f);
            tmp.sortingOrder = 7;
            item.worldWorryText = tmp;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved.GetComponent<WorryLanternItem>();
        }

        private static void BuildUI(Camera cam, GeminiMoodService aiService, SoundscapeManager soundscape, WorryLanternItem lanternPrefab, Transform lanternContainer)
        {
            GameObject canvasGo = new GameObject("DreamDrift_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            UIManager ui = canvasGo.AddComponent<UIManager>();
            ui.moodService = aiService;
            ui.soundscape = soundscape;

            // --- TOP BAR (Airy floating frosted pills with generous safe-area margin) ---
            RectTransform topBar = CreateRect("TopBar", canvasGo.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -48), new Vector2(-64, 96));

            GameObject moodPill = CreateCard("MoodPill", topBar, spritePill, GlassSheetBg, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(540, 82));
            ui.topMoodPillText = CreateText("MoodText", moodPill.transform, "คืนนี้: สงบ พร้อมพักผ่อน", 34, SoftOffWhite, true, TextAlignmentOptions.Left);
            Stretch(ui.topMoodPillText.rectTransform, 32, 24, 8, 8);

            GameObject timerPill = CreateCard("TimerPill", topBar, spritePill, GlassSheetBg, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-132, 0), new Vector2(212, 82));
            ui.topTimerBadgeText = CreateText("TimerText", timerPill.transform, "Sleep: Off", 30, TwilightAmber, true, TextAlignmentOptions.Center);
            Stretch(ui.topTimerBadgeText.rectTransform, 12, 12, 8, 8);

            Button settingsBtn = CreateChipButton("SettingsBtn", topBar, "ตั้งค่า", 30, SoftOffWhite, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(118, 82));
            ui.settingsButton = settingsBtn;

            // --- 1. HOME PANEL (Minimalist Loona/Calm style: lets the campfire & night sky breathe!) ---
            GameObject homePanel = CreateFullPanel("HomePanel", canvasGo.transform, true);
            ui.homePanel = homePanel;

            // Floating title directly over starry sky (no heavy opaque card!)
            TMP_Text brandTitle = CreateDisplayText("BrandTitle", homePanel.transform, "DreamDrift", 136, TwilightAmber, TextAlignmentOptions.Center);
            SetAnchored(brandTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(900, 150));

            TMP_Text brandSub = CreateText("BrandSub", homePanel.transform, "พักใจไว้ตรงนี้นะ คืนนี้ให้แสงไฟและเสียงธรรมชาติกล่อมเธอนอน", 36, SoftOffWhite, false, TextAlignmentOptions.Center);
            SetAnchored(brandSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -346), new Vector2(840, 110));

            // Single-Intent Floating Prompt Pill above the bottom dock
            Button homeAiCta = CreateGradientButton("HomeAI_CTA", homePanel.transform, "คืนนี้รู้สึกยังไงบ้าง? คุยกับ AI ปรับบรรยากาศ", 36, TwilightAmber, DeepInk, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 224), new Vector2(880, 112));
            UnityEventTools.AddPersistentListener(homeAiCta.onClick, ui.OpenAIMood);

            // --- 2. AI MOOD PANEL (Airy Glassmorphic Bottom Sheet leaving top sky visible) ---
            GameObject aiPanel = CreateFullPanel("AIMoodPanel", canvasGo.transform, true);
            ui.aiMoodPanel = aiPanel;
            AIChatPanelUI aiChat = aiPanel.AddComponent<AIChatPanelUI>();
            aiChat.moodService = aiService;

            GameObject aiCard = CreateCard("AIChatCard", aiPanel.transform, spriteCard, GlassSheetBg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 198), new Vector2(1010, 1190));

            // Subtle iOS-style sheet handle pill
            CreateImageObj("SheetHandle", aiCard.transform, spritePill, new Color(1f, 1f, 1f, 0.24f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(116, 10));

            TMP_Text aiHeader = CreateDisplayText("AIHeader", aiCard.transform, "AI เพื่อนคุยก่อนนอน", 88, SoftOffWhite, TextAlignmentOptions.Center);
            SetAnchored(aiHeader.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(900, 96));

            // Compact Frosted Status Pills (Mood + Stress)
            GameObject moodChip = CreateCard("MoodStatusPill", aiCard.transform, spriteOutlineChip, new Color(0.55f, 0.52f, 0.95f, 0.45f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -150), new Vector2(530, 66));
            aiChat.moodBadgeText = CreateText("MoodBadge", moodChip.transform, "อารมณ์: สงบ พร้อมพักผ่อน", 32, SoftOffWhite, true, TextAlignmentOptions.Center);
            Stretch(aiChat.moodBadgeText.rectTransform, 18, 18, 4, 4);

            GameObject stressChip = CreateCard("StressStatusPill", aiCard.transform, spriteOutlineChip, new Color(0.92f, 0.60f, 0.54f, 0.45f), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-46, -150), new Vector2(368, 66));
            aiChat.stressScoreText = CreateText("StressScore", stressChip.transform, "ความเครียด 35%", 32, DustyPeach, true, TextAlignmentOptions.Center);
            Stretch(aiChat.stressScoreText.rectTransform, 14, 14, 4, 4);

            // Airy Reply Bubble with generous breathing room
            GameObject replyBox = CreateCard("ReplyBubble", aiCard.transform, spriteCard, GlassSurfaceBg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -236), new Vector2(918, 268));
            aiChat.aiReplyText = CreateText("AIReplyText", replyBox.transform, "วันนี้เป็นยังไงบ้าง เล่าให้เราฟังได้เลยนะ เดี๋ยวช่วยจัดแสง เสียงฝน และมินิเกมที่เหมาะกับคืนนี้ให้", 38, SoftOffWhite, false, TextAlignmentOptions.TopLeft);
            Stretch(aiChat.aiReplyText.rectTransform, 40, 40, 32, 28);

            aiChat.sourceModeText = CreateText("SourceMode", aiCard.transform, "Smart Local AI", 26, MutedSilver, false, TextAlignmentOptions.Right);
            SetAnchored(aiChat.sourceModeText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-50, -514), new Vector2(820, 38));

            // 1-Tap Recommended Minigame CTA (Dreamy Bioluminescent Indigo)
            Button launchRecBtn = CreateGradientButton("LaunchRecommendedBtn", aiCard.transform, "เริ่มมินิเกมแนะนำ: ฝึกหายใจ 4-7-8  ->", 36, DreamyIndigo, SoftOffWhite, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -562), new Vector2(918, 98));
            aiChat.launchRecommendedButton = launchRecBtn;
            aiChat.launchRecommendedLabel = launchRecBtn.GetComponentInChildren<TMP_Text>();

            // Delicate Outlined Quick Chips (showcasing Sound Request, Encouragement, Advice & Mood)
            string[] chipTexts = { "อยากฟังเสียงฝนกับกองไฟ", "ขอกำลังใจก่อนนอน", "คิดมาก นอนไม่หลับ", "แนะนำวิธีหลับง่าย" };
            Vector2[] chipPos = { new Vector2(-232, -690), new Vector2(232, -690), new Vector2(-232, -784), new Vector2(232, -784) };
            for (int i = 0; i < chipTexts.Length; i++)
            {
                string prompt = chipTexts[i];
                Button chip = CreateChipButton($"Chip_{i}", aiCard.transform, prompt, 30, SoftOffWhite, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), chipPos[i], new Vector2(448, 80));
                UnityEventTools.AddStringPersistentListener(chip.onClick, aiChat.SelectQuickChip, prompt);
            }

            // Sleek Input Field + Twilight Amber Send Button
            aiChat.feelingInputField = CreateInputField("FeelingInput", aiCard.transform, "คุยเล่น, ขอกำลังใจ หรือบอกเสียงที่อยากฟังคืนนี้...", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 162), new Vector2(918, 122));

            Button sendBtn = CreateGradientButton("SendAIBtn", aiCard.transform, "ส่งให้ AI ปรับบรรยากาศ", 38, TwilightAmber, DeepInk, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 38), new Vector2(918, 102));
            aiChat.sendButton = sendBtn;
            aiChat.sendButtonLabel = sendBtn.GetComponentInChildren<TMP_Text>();

            // --- 3. SOUNDSCAPE PANEL (BetterSleep Inspired — sleek sheet, top 55% live scene visible) ---
            GameObject soundPanel = CreateFullPanel("SoundscapePanel", canvasGo.transform, true);
            ui.soundscapePanel = soundPanel;
            SoundscapePanelUI soundUI = soundPanel.AddComponent<SoundscapePanelUI>();
            soundUI.soundscape = soundscape;

            GameObject soundCard = CreateCard("SoundMixerCard", soundPanel.transform, spriteCard, GlassSheetBg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 198), new Vector2(1010, 790));
            CreateImageObj("SheetHandle", soundCard.transform, spritePill, new Color(1f, 1f, 1f, 0.24f), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(116, 10));

            TMP_Text soundTitle = CreateDisplayText("SoundTitle", soundCard.transform, "มิกซ์เสียงธรรมชาติ", 84, SoftOffWhite, TextAlignmentOptions.Center);
            SetAnchored(soundTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(880, 92));

            soundUI.rainSlider = CreateSoundSliderRow("Row_Rain", soundCard.transform, LoadSprite("Icon_Rain"), "เสียงฝนตก", -158, new Color(0.56f, 0.78f, 0.98f, 1f));
            soundUI.campfireSlider = CreateSoundSliderRow("Row_Fire", soundCard.transform, LoadSprite("Icon_Fire"), "เสียงกองไฟ", -268, TwilightAmber);
            soundUI.waveSlider = CreateSoundSliderRow("Row_Wave", soundCard.transform, LoadSprite("Icon_Wave"), "เสียงคลื่นน้ำ", -378, SoftSageMint);
            soundUI.cricketSlider = CreateSoundSliderRow("Row_Cricket", soundCard.transform, LoadSprite("Icon_Cricket"), "เสียงกลางคืน", -488, DreamyIndigo);

            soundUI.timerStatusText = CreateText("TimerStatus", soundCard.transform, "ตั้งเวลาปิดเสียง: ปิดอยู่", 32, MutedSilver, false, TextAlignmentOptions.Center);
            SetAnchored(soundUI.timerStatusText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 124), new Vector2(880, 48));

            int[] timerMins = { 0, 5, 15, 30 };
            string[] timerLabels = { "ปิด", "5 นาที", "15 นาที", "30 นาที" };
            for (int i = 0; i < timerMins.Length; i++)
            {
                float x = -324f + i * 216f;
                Button tBtn = CreateChipButton($"Timer_{timerMins[i]}", soundCard.transform, timerLabels[i], 32, SoftOffWhite, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(x, 34), new Vector2(198, 74));
                UnityEventTools.AddIntPersistentListener(tBtn.onClick, soundUI.SetSleepTimer, timerMins[i]);
            }

            // --- 4. BREATHING MINIGAME PANEL (Calm / Apple Mindfulness Minimalist Style) ---
            GameObject breathPanel = CreateFullPanel("BreathingPanel", canvasGo.transform, true);
            ui.breathingPanel = breathPanel;

            Image breathTouchBg = breathPanel.AddComponent<Image>();
            breathTouchBg.color = new Color(0.03f, 0.05f, 0.12f, 0.45f);
            BreathingMinigame breathGame = breathPanel.AddComponent<BreathingMinigame>();
            breathGame.soundscape = soundscape;

            breathGame.phaseTitleText = CreateDisplayText("PhaseTitle", breathPanel.transform, "ฝึกหายใจ 4-7-8", 92, SoftOffWhite, TextAlignmentOptions.Center);
            SetAnchored(breathGame.phaseTitleText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -195), new Vector2(920, 108));

            breathGame.instructionText = CreateText("InstructionText", breathPanel.transform, "แตะหน้าจอค้างไว้ เพื่อเริ่มสูดลมหายใจเข้าช้าๆ", 36, MutedSilver, false, TextAlignmentOptions.Center);
            SetAnchored(breathGame.instructionText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -308), new Vector2(880, 72));

            // Concentric Soft Glowing Aura Orb
            GameObject orbOuterGo = CreateImageObj("OrbOuter", breathPanel.transform, LoadSprite("SoftGlow"), new Color(0.58f, 0.72f, 1f, 0.30f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(680, 680));
            breathGame.orbOuter = orbOuterGo.GetComponent<RectTransform>();
            breathGame.orbOuterImage = orbOuterGo.GetComponent<Image>();

            GameObject orbCoreGo = CreateImageObj("OrbCore", breathPanel.transform, spriteCircle, new Color(0.58f, 0.78f, 0.96f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(360, 360));
            breathGame.orbCore = orbCoreGo.GetComponent<RectTransform>();
            breathGame.orbCoreImage = orbCoreGo.GetComponent<Image>();

            GameObject ringGo = CreateImageObj("ProgressRing", breathPanel.transform, LoadSprite("RingOutline"), TwilightAmber, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(470, 470));
            Image ringImg = ringGo.GetComponent<Image>();
            ringImg.type = Image.Type.Filled;
            ringImg.fillMethod = Image.FillMethod.Radial360;
            ringImg.fillOrigin = (int)Image.Origin360.Top;
            ringImg.fillClockwise = true;
            ringImg.fillAmount = 0f;
            breathGame.ringFillImage = ringImg;

            breathGame.countdownText = CreateText("CountdownText", breathPanel.transform, "แตะค้าง", 54, DeepInk, true, TextAlignmentOptions.Center);
            SetAnchored(breathGame.countdownText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(320, 120));

            GameObject breathFooter = CreateCard("BreathFooter", breathPanel.transform, spriteCard, GlassSheetBg, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 212), new Vector2(920, 186));
            breathGame.cycleCounterText = CreateText("CycleCounter", breathFooter.transform, "ทำสำเร็จแล้ว 0 รอบ", 36, TwilightAmber, true, TextAlignmentOptions.Center);
            SetAnchored(breathGame.cycleCounterText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(800, 52));

            TMP_Text calmLbl = CreateText("CalmLabel", breathFooter.transform, "ความผ่อนคลายของหัวใจ", 30, MutedSilver, false, TextAlignmentOptions.Left);
            SetAnchored(calmLbl.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(46, 66), new Vector2(460, 42));

            breathGame.calmnessSlider = CreateStyledSlider("CalmnessSlider", breathFooter.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(828, 16), SoftSageMint, false);
            breathGame.calmnessSlider.interactable = false;

            // --- 5. WORRY LANTERN MINIGAME PANEL (Open Night Sky + Compact Glass Bar) ---
            GameObject lanternPanel = CreateFullPanel("LanternPanel", canvasGo.transform, true);
            ui.lanternPanel = lanternPanel;

            Image lanternSwipeCatcher = lanternPanel.AddComponent<Image>();
            lanternSwipeCatcher.color = new Color(0f, 0f, 0f, 0.01f);
            WorryLanternMinigame lanternGame = lanternPanel.AddComponent<WorryLanternMinigame>();
            lanternGame.lanternPrefab = lanternPrefab;
            lanternGame.lanternWorldContainer = lanternContainer;
            lanternGame.mainCamera = cam;
            lanternGame.soundscape = soundscape;

            GameObject lanternCounterPill = CreateCard("LanternCounterPill", lanternPanel.transform, spritePill, GlassSheetBg, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -180), new Vector2(560, 74));
            lanternGame.releasedCounterText = CreateText("ReleasedCounter", lanternCounterPill.transform, "ปล่อยความกังวลแล้ว 0 ดวง", 34, TwilightAmber, true, TextAlignmentOptions.Center);
            Stretch(lanternGame.releasedCounterText.rectTransform, 16, 16, 4, 4);

            lanternGame.statusHintText = CreateText("StatusHint", lanternPanel.transform, "เขียนเรื่องไม่สบายใจลงโคม แล้วปัดขึ้นสู่ท้องฟ้า", 34, SoftOffWhite, false, TextAlignmentOptions.Center);
            SetAnchored(lanternGame.statusHintText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -270), new Vector2(920, 56));

            GameObject previewLantern = CreateImageObj("PreviewLantern", lanternPanel.transform, LoadSprite("WorryLantern"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(360, 450));
            lanternGame.previewLanternRect = previewLantern.GetComponent<RectTransform>();
            lanternGame.previewLanternText = CreateText("PreviewLanternText", previewLantern.transform, "พิมพ์เรื่องกังวล\nแล้วปัดขึ้นฟ้า", 32, new Color(0.28f, 0.12f, 0.04f, 0.95f), true, TextAlignmentOptions.Center);
            Stretch(lanternGame.previewLanternText.rectTransform, 48, 48, 90, 90);

            GameObject arrowHolder = new GameObject("SwipeArrowHolder");
            arrowHolder.transform.SetParent(lanternPanel.transform, false);
            RectTransform arrowHolderRt = arrowHolder.AddComponent<RectTransform>();
            SetAnchored(arrowHolderRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 520), new Vector2(110, 130));
            GameObject arrowImg = CreateImageObj("SwipeArrow", arrowHolder.transform, LoadSprite("Icon_SwipeUp"), new Color(1f, 0.92f, 0.68f, 0.78f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(82, 110));
            lanternGame.swipeArrowRect = arrowImg.GetComponent<RectTransform>();

            GameObject lanternCard = CreateCard("LanternInputCard", lanternPanel.transform, spriteCard, GlassSheetBg, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 198), new Vector2(990, 264));
            lanternGame.worryInputField = CreateInputField("WorryInput", lanternCard.transform, "พิมพ์เรื่องที่กังวลใจคืนนี้...", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(906, 102));

            Button releaseBtn = CreateGradientButton("QuickReleaseBtn", lanternCard.transform, "ปล่อยโคมขึ้นฟ้า", 36, DustyPeach, DeepInk, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(906, 92));
            lanternGame.quickReleaseButton = releaseBtn;

            // --- FLOATING FROSTED BOTTOM DOCK ---
            GameObject navBar = CreateCard("BottomNavBar", canvasGo.transform, spritePill, DockBg, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(1010, 124));
            HorizontalLayoutGroup hlg = navBar.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 14, 14);
            hlg.spacing = 10;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            ui.navHomeBg = CreateNavButton("Nav_Home", navBar.transform, "หน้าหลัก", ui.OpenHome);
            ui.navAIBg = CreateNavButton("Nav_AI", navBar.transform, "AI คุย", ui.OpenAIMood);
            ui.navSoundBg = CreateNavButton("Nav_Sound", navBar.transform, "เสียง", ui.OpenSoundscape);
            ui.navBreathBg = CreateNavButton("Nav_Breath", navBar.transform, "หายใจ", ui.OpenBreathing);
            ui.navLanternBg = CreateNavButton("Nav_Lantern", navBar.transform, "โคมลอย", ui.OpenLantern);

            // --- SETTINGS MODAL (Gemini API Key) ---
            GameObject settingsModal = CreateFullPanel("SettingsModal", canvasGo.transform, false);
            Image modalDim = settingsModal.AddComponent<Image>();
            modalDim.color = new Color(0.02f, 0.03f, 0.08f, 0.80f);
            ui.settingsModal = settingsModal;

            GameObject modalCard = CreateCard("SettingsCard", settingsModal.transform, spriteCard, GlassSheetBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(930, 680));
            TMP_Text setTitle = CreateDisplayText("SettingsTitle", modalCard.transform, "ตั้งค่า Gemini API", 82, TwilightAmber, TextAlignmentOptions.Center);
            SetAnchored(setTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(820, 96));

            TMP_Text setDesc = CreateText("SettingsDesc", modalCard.transform, "ใส่ Google Gemini API Key เพื่อวิเคราะห์อารมณ์แบบเรียลไทม์ (หากเว้นว่างจะใช้ Smart Local AI อัตโนมัติ)", 32, MutedSilver, false, TextAlignmentOptions.Center);
            SetAnchored(setDesc.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -142), new Vector2(820, 100));

            ui.apiKeyInputField = CreateInputField("ApiKeyInput", modalCard.transform, "AIzaSy...", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -268), new Vector2(820, 102));

            ui.demoModeToggle = CreateSimpleToggle("DemoModeToggle", modalCard.transform, "บังคับใช้ Demo Mode (ออฟไลน์)", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -402), new Vector2(820, 70));

            ui.saveSettingsButton = CreateGradientButton("SaveSettingsBtn", modalCard.transform, "บันทึก", 36, TwilightAmber, DeepInk, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-210, 44), new Vector2(380, 94));
            ui.closeSettingsButton = CreateChipButton("CloseSettingsBtn", modalCard.transform, "ปิด", 36, SoftOffWhite, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(210, 44), new Vector2(380, 94));
        }

        private static Image CreateNavButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image bg = go.AddComponent<Image>();
            bg.sprite = spritePill;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(1f, 1f, 1f, 0.0f);

            Button btn = go.AddComponent<Button>();
            go.AddComponent<SoftButtonFeedback>();
            UnityEventTools.AddPersistentListener(btn.onClick, action);

            TMP_Text txt = CreateText("Label", go.transform, label, 34, SoftOffWhite, true, TextAlignmentOptions.Center);
            Stretch(txt.rectTransform, 6, 6, 4, 4);
            return bg;
        }

        private static Slider CreateSoundSliderRow(string name, Transform parent, Sprite icon, string label, float posY, Color accentColor)
        {
            RectTransform row = CreateRect(name, parent, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, posY), new Vector2(920, 96));

            // Glowing circular icon badge (BetterSleep style)
            Color badgeBg = accentColor;
            badgeBg.a = 0.22f;
            GameObject badge = CreateImageObj("IconBadge", row, spriteCircle, badgeBg, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(78, 78));
            CreateImageObj("Icon", badge.transform, icon, accentColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46));

            TMP_Text lbl = CreateText("Label", row, label, 34, SoftOffWhite, true, TextAlignmentOptions.Left);
            SetAnchored(lbl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(106, 0), new Vector2(260, 56));

            // Refined slim slider track (16px height) + soft glowing knob
            return CreateStyledSlider("Slider", row, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(520, 44), accentColor, true);
        }

        private static Slider CreateStyledSlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color fillColor, bool withKnob)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            SetAnchored(rt, anchorMin, anchorMax, pivot, pos, size);

            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;

            float trackPad = withKnob ? 14f : 0f;
            GameObject bgGo = CreateImageObj("Background", go.transform, spritePill, new Color(1f, 1f, 1f, 0.14f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bgGo.GetComponent<Image>().type = Image.Type.Sliced;
            Stretch(bgGo.GetComponent<RectTransform>(), 0, 0, trackPad, trackPad);

            RectTransform fillArea = CreateRect("Fill Area", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(fillArea, 0, 0, trackPad, trackPad);

            GameObject fillGo = CreateImageObj("Fill", fillArea, spritePill, fillColor, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            fillGo.GetComponent<Image>().type = Image.Type.Sliced;
            slider.fillRect = fillGo.GetComponent<RectTransform>();

            if (withKnob)
            {
                RectTransform handleArea = CreateRect("Handle Slide Area", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                Stretch(handleArea, 18, 18, 0, 0);

                GameObject handleGo = CreateImageObj("Handle", handleArea, spriteCircle, SoftOffWhite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
                slider.handleRect = handleGo.GetComponent<RectTransform>();
                slider.targetGraphic = handleGo.GetComponent<Image>();
            }

            return slider;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholderStr, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateCard(name, parent, spriteCard, new Color(0.095f, 0.125f, 0.235f, 0.85f), anchorMin, anchorMax, pivot, pos, size);
            TMP_InputField input = go.AddComponent<TMP_InputField>();

            RectTransform textArea = CreateRect("Text Area", go.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(textArea, 34, 34, 6, 6);
            RectMask2D mask = textArea.gameObject.AddComponent<RectMask2D>();
            // Extra vertical headroom so stacked Thai vowels & tone marks (่ ้ ึ ื ุ ู) are never clipped
            mask.padding = new Vector4(0f, -18f, 0f, -18f);

            TMP_Text placeholder = CreateText("Placeholder", textArea, placeholderStr, 34, new Color(0.72f, 0.76f, 0.90f, 0.52f), false, TextAlignmentOptions.Left);
            Stretch(placeholder.rectTransform, 0, 0, 0, 0);

            TMP_Text textComp = CreateText("Text", textArea, "", 36, SoftOffWhite, false, TextAlignmentOptions.Left);
            Stretch(textComp.rectTransform, 0, 0, 0, 0);

            input.textViewport = textArea;
            input.textComponent = textComp;
            input.placeholder = placeholder;
            input.caretColor = TwilightAmber;
            input.selectionColor = new Color(0.62f, 0.58f, 0.96f, 0.30f);
            return input;
        }

        private static Toggle CreateSimpleToggle(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            SetAnchored(rt, anchorMin, anchorMax, pivot, pos, size);

            Toggle toggle = go.AddComponent<Toggle>();
            GameObject box = CreateImageObj("Background", go.transform, spriteCard, GlassSurfaceBg, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(54, 54));
            box.GetComponent<Image>().type = Image.Type.Sliced;

            GameObject check = CreateImageObj("Checkmark", box.transform, spriteCircle, TwilightAmber, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28, 28));

            TMP_Text lbl = CreateText("Label", go.transform, label, 34, SoftOffWhite, false, TextAlignmentOptions.Left);
            SetAnchored(lbl.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(86, 0), new Vector2(-100, 56));

            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            return toggle;
        }

        private static void CreateEventSystem()
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();

            Type inputSysType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSysType != null)
            {
                esGo.AddComponent(inputSysType);
            }
            else
            {
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private static GameObject CreateFullPanel(string name, Transform parent, bool withSmoothTransition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            Stretch(rt, 0, 0, 0, 0);
            if (withSmoothTransition)
            {
                go.AddComponent<CanvasGroup>();
                go.AddComponent<SmoothPanelTransition>();
            }
            return go;
        }

        private static GameObject CreateCard(string name, Transform parent, Sprite spr, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateImageObj(name, parent, spr, color, anchorMin, anchorMax, pivot, pos, size);
            Image img = go.GetComponent<Image>();
            img.type = Image.Type.Sliced;
            return go;
        }

        private static Button CreateGradientButton(string name, Transform parent, string label, float fontSize, Color bgColor, Color textColor, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateCard(name, parent, spriteGradientBtn, bgColor, anchorMin, anchorMax, pivot, pos, size);
            Button btn = go.AddComponent<Button>();
            go.AddComponent<SoftButtonFeedback>();
            TMP_Text txt = CreateText("Label", go.transform, label, fontSize, textColor, true, TextAlignmentOptions.Center);
            Stretch(txt.rectTransform, 22, 22, 6, 6);
            return btn;
        }

        private static Button CreateChipButton(string name, Transform parent, string label, float fontSize, Color textColor, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateCard(name, parent, spriteOutlineChip, new Color(0.75f, 0.80f, 1f, 0.55f), anchorMin, anchorMax, pivot, pos, size);
            Button btn = go.AddComponent<Button>();
            go.AddComponent<SoftButtonFeedback>();
            TMP_Text txt = CreateText("Label", go.transform, label, fontSize, textColor, true, TextAlignmentOptions.Center);
            Stretch(txt.rectTransform, 16, 16, 6, 6);
            return btn;
        }

        private static GameObject CreateImageObj(string name, Transform parent, Sprite spr, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            SetAnchored(rt, anchorMin, anchorMax, pivot, pos, size);
            Image img = go.AddComponent<Image>();
            img.sprite = spr;
            img.color = color;
            return go;
        }

        private static TMP_Text CreateDisplayText(string name, Transform parent, string content, float fontSize, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (fontDisplay != null) tmp.font = fontDisplay;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.extraPadding = true;
            tmp.characterSpacing = 3f;
            EnableThaiOpenTypeFeatures(tmp);
            return tmp;
        }

        private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize, Color color, bool bold, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset f = bold && fontBodyBold != null ? fontBodyBold : fontBody;
            if (f != null) tmp.font = f;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.extraPadding = true;
            tmp.characterSpacing = 0.8f;
            tmp.lineSpacing = 22f;
            EnableThaiOpenTypeFeatures(tmp);
            return tmp;
        }

        private static void EnableThaiOpenTypeFeatures(TMP_Text tmp)
        {
            // Enable OpenType GPOS mark-to-base & mark-to-mark so Thai tone marks (่ ้ ๊ ๋) stack cleanly above vowels (ิ ี ึ ื ั)
            tmp.fontFeatures = new System.Collections.Generic.List<OTL_FeatureTag>
            {
                OTL_FeatureTag.kern,
                OTL_FeatureTag.mark,
                OTL_FeatureTag.mkmk
            };
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            SetAnchored(rt, anchorMin, anchorMax, pivot, pos, size);
            return rt;
        }

        private static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static SpriteRenderer CreateSpriteObj(string name, Transform parent, Sprite sprite, Vector3 pos, Vector3 scale, int sortingOrder, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = color;
            return sr;
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ProceduralAssetGenerator.SpritesPath}/{name}.png");
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            updated[0] = new EditorBuildSettingsScene(scenePath, true);
            Array.Copy(scenes, 0, updated, 1, scenes.Length);
            EditorBuildSettings.scenes = updated;
        }
    }
}
#endif
