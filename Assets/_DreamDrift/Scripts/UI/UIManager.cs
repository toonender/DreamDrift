using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DreamDrift.AI;
using DreamDrift.Audio;
using DreamDrift.Core;

namespace DreamDrift.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Services")]
        public GeminiMoodService moodService;
        public SoundscapeManager soundscape;

        [Header("Top Bar")]
        public TMP_Text topMoodPillText;
        public TMP_Text topTimerBadgeText;
        public Button settingsButton;

        [Header("Panels")]
        public GameObject homePanel;
        public GameObject aiMoodPanel;
        public GameObject soundscapePanel;
        public GameObject breathingPanel;
        public GameObject lanternPanel;
        public GameObject settingsModal;

        [Header("Bottom Nav Buttons")]
        public Image navHomeBg;
        public Image navAIBg;
        public Image navSoundBg;
        public Image navBreathBg;
        public Image navLanternBg;

        [Header("Settings Modal")]
        public TMP_InputField apiKeyInputField;
        public Toggle demoModeToggle;
        public Button saveSettingsButton;
        public Button closeSettingsButton;

        private readonly Color navActiveColor = new Color(0.55f, 0.52f, 0.94f, 0.38f);
        private readonly Color navIdleColor = new Color(1f, 1f, 1f, 0.00f);
        private bool firstTransition = true;
        private AppScreen activeNavScreen = AppScreen.Home;

        private void Update()
        {
            float t = Time.unscaledDeltaTime * 12f;
            LerpNav(navHomeBg, activeNavScreen == AppScreen.Home, t);
            LerpNav(navAIBg, activeNavScreen == AppScreen.AIMood, t);
            LerpNav(navSoundBg, activeNavScreen == AppScreen.Soundscape, t);
            LerpNav(navBreathBg, activeNavScreen == AppScreen.Breathing, t);
            LerpNav(navLanternBg, activeNavScreen == AppScreen.WorryLantern, t);
        }

        private void LerpNav(Image img, bool active, float t)
        {
            if (img == null) return;
            Color target = active ? navActiveColor : navIdleColor;
            img.color = Color.Lerp(img.color, target, t);
        }

        private void Awake()
        {
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (saveSettingsButton != null) saveSettingsButton.onClick.AddListener(SaveSettings);
            if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);
        }

        private void Start()
        {
            if (DreamDriftManager.Instance != null)
            {
                DreamDriftManager.Instance.OnScreenChanged += HandleScreenChanged;
                DreamDriftManager.Instance.OnMoodApplied += HandleMoodApplied;
                HandleScreenChanged(DreamDriftManager.Instance.CurrentScreen);
                HandleMoodApplied(DreamDriftManager.Instance.LatestMood);
            }

            if (soundscape != null)
            {
                soundscape.OnSleepTimerTick += HandleTimerTick;
                HandleTimerTick(soundscape.SleepTimerRemaining);
            }

            if (settingsModal != null) settingsModal.SetActive(false);
        }

        private void OnDestroy()
        {
            if (DreamDriftManager.Instance != null)
            {
                DreamDriftManager.Instance.OnScreenChanged -= HandleScreenChanged;
                DreamDriftManager.Instance.OnMoodApplied -= HandleMoodApplied;
            }
            if (soundscape != null)
            {
                soundscape.OnSleepTimerTick -= HandleTimerTick;
            }
        }

        public void OpenHome() => DreamDriftManager.Instance?.SetScreen(AppScreen.Home);
        public void OpenAIMood() => DreamDriftManager.Instance?.SetScreen(AppScreen.AIMood);
        public void OpenSoundscape() => DreamDriftManager.Instance?.SetScreen(AppScreen.Soundscape);
        public void OpenBreathing() => DreamDriftManager.Instance?.SetScreen(AppScreen.Breathing);
        public void OpenLantern() => DreamDriftManager.Instance?.SetScreen(AppScreen.WorryLantern);

        private void HandleScreenChanged(AppScreen screen)
        {
            activeNavScreen = screen;
            SetPanelSmooth(homePanel, screen == AppScreen.Home, firstTransition);
            SetPanelSmooth(aiMoodPanel, screen == AppScreen.AIMood, firstTransition);
            SetPanelSmooth(soundscapePanel, screen == AppScreen.Soundscape, firstTransition);
            SetPanelSmooth(breathingPanel, screen == AppScreen.Breathing, firstTransition);
            SetPanelSmooth(lanternPanel, screen == AppScreen.WorryLantern, firstTransition);

            if (firstTransition)
            {
                SetNavHighlight(navHomeBg, screen == AppScreen.Home);
                SetNavHighlight(navAIBg, screen == AppScreen.AIMood);
                SetNavHighlight(navSoundBg, screen == AppScreen.Soundscape);
                SetNavHighlight(navBreathBg, screen == AppScreen.Breathing);
                SetNavHighlight(navLanternBg, screen == AppScreen.WorryLantern);
            }
            firstTransition = false;
        }

        private static void SetPanelSmooth(GameObject panel, bool visible, bool instant)
        {
            if (panel == null) return;
            SmoothPanelTransition smooth = panel.GetComponent<SmoothPanelTransition>();
            if (smooth != null)
            {
                smooth.SetVisible(visible, instant);
            }
            else
            {
                panel.SetActive(visible);
            }
        }

        private void HandleMoodApplied(MoodAnalysisResult result)
        {
            if (topMoodPillText != null && result != null)
            {
                topMoodPillText.text = $"คืนนี้: {result.moodLabel}";
            }
        }

        private void HandleTimerTick(float remainingSeconds)
        {
            if (topTimerBadgeText == null) return;
            if (remainingSeconds <= 0.1f)
            {
                topTimerBadgeText.text = "Sleep: Off";
            }
            else
            {
                int mins = Mathf.CeilToInt(remainingSeconds / 60f);
                topTimerBadgeText.text = $"Sleep: {mins}m";
            }
        }

        private void OpenSettings()
        {
            if (settingsModal == null) return;
            if (moodService != null)
            {
                if (apiKeyInputField != null) apiKeyInputField.text = moodService.ApiKey;
                if (demoModeToggle != null) demoModeToggle.isOn = moodService.ForceDemoMode;
            }
            settingsModal.SetActive(true);
        }

        private void SaveSettings()
        {
            if (moodService != null)
            {
                if (apiKeyInputField != null) moodService.ApiKey = apiKeyInputField.text;
                if (demoModeToggle != null) moodService.ForceDemoMode = demoModeToggle.isOn;
            }
            CloseSettings();
        }

        private void CloseSettings()
        {
            if (settingsModal != null) settingsModal.SetActive(false);
        }

        private void SetNavHighlight(Image img, bool active)
        {
            if (img != null) img.color = active ? navActiveColor : navIdleColor;
        }
    }
}
