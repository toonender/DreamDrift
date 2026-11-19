using System;
using UnityEngine;
using DreamDrift.Atmosphere;
using DreamDrift.Audio;

namespace DreamDrift.Core
{
    public class DreamDriftManager : MonoBehaviour
    {
        public static DreamDriftManager Instance { get; private set; }

        [Header("Core References")]
        public Camera mainCamera;
        public AtmosphereController atmosphere;
        public SoundscapeManager soundscape;

        [Header("Camera Pan Settings")]
        public float homeCameraY = 0f;
        public float lanternCameraY = 8.5f;
        public float cameraSmoothSpeed = 3.2f;

        public AppScreen CurrentScreen { get; private set; } = AppScreen.Home;
        public MoodAnalysisResult LatestMood { get; private set; } = new MoodAnalysisResult();

        public event Action<AppScreen> OnScreenChanged;
        public event Action<MoodAnalysisResult> OnMoodApplied;

        private float targetCameraY;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            targetCameraY = homeCameraY;
            ApplyMoodResult(LatestMood, instant: true);
            SetScreen(AppScreen.Home);
        }

        private void Update()
        {
            if (mainCamera != null)
            {
                Vector3 pos = mainCamera.transform.position;
                pos.y = Mathf.Lerp(pos.y, targetCameraY, Time.deltaTime * cameraSmoothSpeed);
                mainCamera.transform.position = pos;
            }
        }

        public void SetScreen(AppScreen screen)
        {
            CurrentScreen = screen;
            targetCameraY = (screen == AppScreen.WorryLantern) ? lanternCameraY : homeCameraY;
            OnScreenChanged?.Invoke(screen);
        }

        public void OpenRecommendedMinigame()
        {
            if (LatestMood == null)
            {
                SetScreen(AppScreen.Breathing);
                return;
            }

            SetScreen(LatestMood.GetRecommendedType() == RecommendedMinigameType.WorryLantern
                ? AppScreen.WorryLantern
                : AppScreen.Breathing);
        }

        public void ApplyMoodResult(MoodAnalysisResult result, bool instant = false)
        {
            if (result == null) return;
            LatestMood = result;

            if (atmosphere != null)
            {
                atmosphere.ApplyMoodColors(
                    result.GetColorOrDefault(result.skyTopHex, new Color(0.04f, 0.07f, 0.17f)),
                    result.GetColorOrDefault(result.skyBottomHex, new Color(0.11f, 0.18f, 0.35f)),
                    result.GetColorOrDefault(result.globalLightHex, new Color(0.61f, 0.71f, 1f)),
                    instant);
            }

            if (soundscape != null)
            {
                soundscape.SetAllChannels(
                    Mathf.Clamp01(result.rainLevel),
                    Mathf.Clamp01(result.campfireLevel),
                    Mathf.Clamp01(result.waveLevel),
                    Mathf.Clamp01(result.cricketLevel));
            }

            OnMoodApplied?.Invoke(result);
        }
    }
}
