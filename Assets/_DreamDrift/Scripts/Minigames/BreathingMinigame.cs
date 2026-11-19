using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DreamDrift.Audio;

namespace DreamDrift.Minigames
{
    public class BreathingMinigame : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum BreathPhase
        {
            Ready,
            Inhale,
            Hold,
            Exhale
        }

        [Header("4-7-8 Rhythm Durations (Seconds)")]
        public float inhaleDuration = 4f;
        public float holdDuration = 7f;
        public float exhaleDuration = 8f;

        [Header("UI Visuals")]
        public RectTransform orbOuter;
        public RectTransform orbCore;
        public Image orbOuterImage;
        public Image orbCoreImage;
        public Image ringFillImage;
        public TMP_Text phaseTitleText;
        public TMP_Text countdownText;
        public TMP_Text instructionText;
        public TMP_Text cycleCounterText;
        public Slider calmnessSlider;

        [Header("Audio")]
        public SoundscapeManager soundscape;

        [Header("Colors")]
        public Color readyColor = new Color(0.72f, 0.80f, 1f, 0.65f);
        public Color inhaleColor = new Color(0.55f, 0.88f, 0.96f, 0.85f);
        public Color holdColor = new Color(1f, 0.89f, 0.55f, 0.90f);
        public Color exhaleColor = new Color(0.76f, 0.68f, 1f, 0.80f);

        private BreathPhase phase = BreathPhase.Ready;
        private float phaseTimer;
        private bool isHolding;
        private int completedCycles;
        private float calmness = 0.2f;
        private float currentOrbScale = 0.72f;
        private Color currentOrbColor;

        private void OnEnable()
        {
            ResetSession();
        }

        public void ResetSession()
        {
            phase = BreathPhase.Ready;
            phaseTimer = 0f;
            isHolding = false;
            currentOrbScale = 0.72f;
            currentOrbColor = readyColor;
            UpdateLabels();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isHolding = true;
            if (phase == BreathPhase.Ready)
            {
                EnterPhase(BreathPhase.Inhale);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isHolding = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float targetScale = 0.72f;
            Color targetColor = readyColor;
            float fillProgress = 0f;

            switch (phase)
            {
                case BreathPhase.Ready:
                    targetScale = 0.72f + Mathf.Sin(Time.time * 1.8f) * 0.04f;
                    targetColor = readyColor;
                    fillProgress = 0f;
                    break;

                case BreathPhase.Inhale:
                    if (isHolding)
                    {
                        phaseTimer += dt;
                    }
                    else
                    {
                        // Gentle pause/drift back if user lifts early
                        phaseTimer = Mathf.Max(0f, phaseTimer - dt * 0.5f);
                    }
                    fillProgress = Mathf.Clamp01(phaseTimer / inhaleDuration);
                    targetScale = Mathf.Lerp(0.72f, 1.32f, Mathf.SmoothStep(0f, 1f, fillProgress));
                    targetColor = inhaleColor;
                    if (phaseTimer >= inhaleDuration)
                    {
                        EnterPhase(BreathPhase.Hold);
                    }
                    break;

                case BreathPhase.Hold:
                    phaseTimer += dt;
                    fillProgress = Mathf.Clamp01(phaseTimer / holdDuration);
                    targetScale = 1.32f + Mathf.Sin(Time.time * 3.5f) * 0.03f;
                    targetColor = holdColor;
                    if (phaseTimer >= holdDuration)
                    {
                        EnterPhase(BreathPhase.Exhale);
                    }
                    break;

                case BreathPhase.Exhale:
                    // Exhale progresses smoothly when user releases finger (or slowly if still holding)
                    float speed = isHolding ? 0.35f : 1f;
                    phaseTimer += dt * speed;
                    fillProgress = Mathf.Clamp01(phaseTimer / exhaleDuration);
                    targetScale = Mathf.Lerp(1.32f, 0.72f, Mathf.SmoothStep(0f, 1f, fillProgress));
                    targetColor = exhaleColor;
                    if (phaseTimer >= exhaleDuration)
                    {
                        completedCycles++;
                        calmness = Mathf.Clamp01(calmness + 0.2f);
                        if (calmnessSlider != null) calmnessSlider.value = calmness;
                        if (soundscape != null) soundscape.PlayGentleChime(0.45f);
                        EnterPhase(isHolding ? BreathPhase.Inhale : BreathPhase.Ready);
                    }
                    break;
            }

            currentOrbScale = Mathf.Lerp(currentOrbScale, targetScale, dt * 6f);
            currentOrbColor = Color.Lerp(currentOrbColor, targetColor, dt * 4f);

            if (orbOuter != null) orbOuter.localScale = Vector3.one * (currentOrbScale * 1.15f);
            if (orbCore != null) orbCore.localScale = Vector3.one * currentOrbScale;
            if (orbOuterImage != null)
            {
                Color outer = currentOrbColor;
                outer.a = 0.28f;
                orbOuterImage.color = outer;
            }
            if (orbCoreImage != null) orbCoreImage.color = currentOrbColor;
            if (ringFillImage != null) ringFillImage.fillAmount = fillProgress;

            UpdateCountdown();
        }

        private void EnterPhase(BreathPhase next)
        {
            phase = next;
            phaseTimer = 0f;
            if (next != BreathPhase.Ready && soundscape != null)
            {
                soundscape.PlayGentleChime(0.30f);
            }
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (cycleCounterText != null)
                cycleCounterText.text = $"ทำสำเร็จแล้ว {completedCycles} รอบ";

            if (calmnessSlider != null)
                calmnessSlider.value = calmness;

            if (phaseTitleText == null || instructionText == null) return;

            switch (phase)
            {
                case BreathPhase.Ready:
                    phaseTitleText.text = "พร้อมฝึกหายใจ 4-7-8";
                    instructionText.text = "แตะหน้าจอค้างไว้ เพื่อเริ่มสูดลมหายใจเข้า";
                    break;
                case BreathPhase.Inhale:
                    phaseTitleText.text = "หายใจเข้าลึกๆ";
                    instructionText.text = "แตะหน้าจอค้างไว้ 4 วินาที";
                    break;
                case BreathPhase.Hold:
                    phaseTitleText.text = "กลั้นลมหายใจไว้เบาๆ";
                    instructionText.text = "ผ่อนคลายไหล่ ค้างไว้ 7 วินาที";
                    break;
                case BreathPhase.Exhale:
                    phaseTitleText.text = "ค่อยๆ ผ่อนลมหายใจออก";
                    instructionText.text = "ปล่อยนิ้วออก ช้าๆ 8 วินาที";
                    break;
            }
        }

        private void UpdateCountdown()
        {
            if (countdownText == null) return;
            switch (phase)
            {
                case BreathPhase.Ready:
                    countdownText.text = "แตะค้าง";
                    break;
                case BreathPhase.Inhale:
                    countdownText.text = Mathf.CeilToInt(Mathf.Max(0.1f, inhaleDuration - phaseTimer)).ToString();
                    break;
                case BreathPhase.Hold:
                    countdownText.text = Mathf.CeilToInt(Mathf.Max(0.1f, holdDuration - phaseTimer)).ToString();
                    break;
                case BreathPhase.Exhale:
                    countdownText.text = Mathf.CeilToInt(Mathf.Max(0.1f, exhaleDuration - phaseTimer)).ToString();
                    break;
            }
        }
    }
}
