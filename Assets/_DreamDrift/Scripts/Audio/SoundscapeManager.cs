using System;
using UnityEngine;
using DreamDrift.Atmosphere;

namespace DreamDrift.Audio
{
    public class SoundscapeManager : MonoBehaviour
    {
        [Header("Ambient Audio Sources")]
        public AudioSource rainSource;
        public AudioSource campfireSource;
        public AudioSource waveSource;
        public AudioSource cricketSource;
        public AudioSource sfxSource;

        [Header("SFX Clips")]
        public AudioClip chimeClip;

        [Header("Linked Atmosphere")]
        public AtmosphereController atmosphere;

        [Range(0f, 1f)] public float rainVolume = 0.45f;
        [Range(0f, 1f)] public float campfireVolume = 0.60f;
        [Range(0f, 1f)] public float waveVolume = 0.25f;
        [Range(0f, 1f)] public float cricketVolume = 0.30f;

        public float SleepTimerRemaining { get; private set; }
        public float SleepTimerTotal { get; private set; }
        public bool IsSleepTimerActive => SleepTimerRemaining > 0f;

        public event Action<float, float, float, float> OnLevelsChanged;
        public event Action<float> OnSleepTimerTick;

        private float masterFade = 1f;

        private void Start()
        {
            EnsurePlaying(rainSource);
            EnsurePlaying(campfireSource);
            EnsurePlaying(waveSource);
            EnsurePlaying(cricketSource);
            ApplyVolumes();
        }

        private void Update()
        {
            if (SleepTimerRemaining > 0f)
            {
                SleepTimerRemaining = Mathf.Max(0f, SleepTimerRemaining - Time.deltaTime);
                OnSleepTimerTick?.Invoke(SleepTimerRemaining);

                // Smooth fade out during the last 30 seconds
                if (SleepTimerRemaining < 30f)
                {
                    masterFade = Mathf.Clamp01(SleepTimerRemaining / 30f);
                }
                else
                {
                    masterFade = 1f;
                }

                ApplyVolumes();
            }
            else if (SleepTimerTotal > 0f)
            {
                masterFade = 0f;
                ApplyVolumes();
            }
        }

        public void SetRain(float v) { rainVolume = Mathf.Clamp01(v); ResetFadeIfZero(); ApplyVolumes(); Notify(); }
        public void SetCampfire(float v) { campfireVolume = Mathf.Clamp01(v); ResetFadeIfZero(); ApplyVolumes(); Notify(); }
        public void SetWave(float v) { waveVolume = Mathf.Clamp01(v); ResetFadeIfZero(); ApplyVolumes(); Notify(); }
        public void SetCricket(float v) { cricketVolume = Mathf.Clamp01(v); ResetFadeIfZero(); ApplyVolumes(); Notify(); }

        public void SetAllChannels(float rain, float fire, float wave, float cricket)
        {
            rainVolume = Mathf.Clamp01(rain);
            campfireVolume = Mathf.Clamp01(fire);
            waveVolume = Mathf.Clamp01(wave);
            cricketVolume = Mathf.Clamp01(cricket);
            ResetFadeIfZero();
            ApplyVolumes();
            Notify();
        }

        public void SetSleepTimerMinutes(int minutes)
        {
            if (minutes <= 0)
            {
                SleepTimerTotal = 0f;
                SleepTimerRemaining = 0f;
                masterFade = 1f;
            }
            else
            {
                SleepTimerTotal = minutes * 60f;
                SleepTimerRemaining = SleepTimerTotal;
                masterFade = 1f;
            }
            ApplyVolumes();
            OnSleepTimerTick?.Invoke(SleepTimerRemaining);
        }

        public void PlayGentleChime(float volume = 0.55f)
        {
            if (sfxSource != null && chimeClip != null)
            {
                sfxSource.PlayOneShot(chimeClip, volume * masterFade);
            }
        }

        private void ResetFadeIfZero()
        {
            if (SleepTimerTotal > 0f && SleepTimerRemaining <= 0f)
            {
                SleepTimerTotal = 0f;
                masterFade = 1f;
            }
        }

        private void ApplyVolumes()
        {
            if (rainSource != null) rainSource.volume = rainVolume * masterFade * 0.75f;
            if (campfireSource != null) campfireSource.volume = campfireVolume * masterFade * 0.85f;
            if (waveSource != null) waveSource.volume = waveVolume * masterFade * 0.75f;
            if (cricketSource != null) cricketSource.volume = cricketVolume * masterFade * 0.55f;

            if (atmosphere != null)
            {
                atmosphere.SetEnvironmentLevels(
                    rainVolume * masterFade,
                    campfireVolume * masterFade,
                    waveVolume * masterFade,
                    cricketVolume * masterFade);
            }
        }

        private void Notify()
        {
            OnLevelsChanged?.Invoke(rainVolume, campfireVolume, waveVolume, cricketVolume);
        }

        private static void EnsurePlaying(AudioSource src)
        {
            if (src != null && src.clip != null && !src.isPlaying)
            {
                src.loop = true;
                src.Play();
            }
        }
    }
}
