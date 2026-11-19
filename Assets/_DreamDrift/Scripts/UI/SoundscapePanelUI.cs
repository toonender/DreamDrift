using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DreamDrift.Audio;

namespace DreamDrift.UI
{
    public class SoundscapePanelUI : MonoBehaviour
    {
        [Header("Audio Controller")]
        public SoundscapeManager soundscape;

        [Header("Sliders")]
        public Slider rainSlider;
        public Slider campfireSlider;
        public Slider waveSlider;
        public Slider cricketSlider;

        [Header("Sleep Timer UI")]
        public TMP_Text timerStatusText;

        private bool suppressCallbacks;

        private void Awake()
        {
            if (rainSlider != null) rainSlider.onValueChanged.AddListener(v => { if (!suppressCallbacks && soundscape != null) soundscape.SetRain(v); });
            if (campfireSlider != null) campfireSlider.onValueChanged.AddListener(v => { if (!suppressCallbacks && soundscape != null) soundscape.SetCampfire(v); });
            if (waveSlider != null) waveSlider.onValueChanged.AddListener(v => { if (!suppressCallbacks && soundscape != null) soundscape.SetWave(v); });
            if (cricketSlider != null) cricketSlider.onValueChanged.AddListener(v => { if (!suppressCallbacks && soundscape != null) soundscape.SetCricket(v); });
        }

        private void OnEnable()
        {
            if (soundscape != null)
            {
                soundscape.OnLevelsChanged += SyncSliders;
                soundscape.OnSleepTimerTick += UpdateTimerText;
                SyncSliders(soundscape.rainVolume, soundscape.campfireVolume, soundscape.waveVolume, soundscape.cricketVolume);
                UpdateTimerText(soundscape.SleepTimerRemaining);
            }
        }

        private void OnDisable()
        {
            if (soundscape != null)
            {
                soundscape.OnLevelsChanged -= SyncSliders;
                soundscape.OnSleepTimerTick -= UpdateTimerText;
            }
        }

        public void SetSleepTimer(int minutes)
        {
            if (soundscape != null)
            {
                soundscape.SetSleepTimerMinutes(minutes);
            }
        }

        private void SyncSliders(float rain, float fire, float wave, float cricket)
        {
            suppressCallbacks = true;
            if (rainSlider != null) rainSlider.value = rain;
            if (campfireSlider != null) campfireSlider.value = fire;
            if (waveSlider != null) waveSlider.value = wave;
            if (cricketSlider != null) cricketSlider.value = cricket;
            suppressCallbacks = false;
        }

        private void UpdateTimerText(float remainingSeconds)
        {
            if (timerStatusText == null) return;
            if (remainingSeconds <= 0.1f)
            {
                timerStatusText.text = "ตั้งเวลาปิดเสียง: ปิดอยู่";
                return;
            }

            int mins = Mathf.FloorToInt(remainingSeconds / 60f);
            int secs = Mathf.FloorToInt(remainingSeconds % 60f);
            timerStatusText.text = $"จะค่อยๆ หรี่เสียงใน {mins:00}:{secs:00}";
        }
    }
}
