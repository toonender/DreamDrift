using UnityEngine;
using UnityEngine.EventSystems;

namespace DreamDrift.UI
{
    /// <summary>
    /// Zero-ghosting, silky smooth panel transition:
    /// Outgoing panel fades out swiftly (no smear), incoming panel glides in smoothly after a brief delay.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SmoothPanelTransition : MonoBehaviour
    {
        public float fadeOutDuration = 0.13f;
        public float fadeInDelay = 0.10f;
        public float fadeInDuration = 0.24f;
        public float slideOffsetY = 14f;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector2 restingPos;
        private bool initialized;
        private bool targetVisible;
        private float progress;
        private float delayRemaining;

        private void EnsureInit()
        {
            if (initialized) return;
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            restingPos = rectTransform.anchoredPosition;
            initialized = true;
        }

        public void SetVisible(bool visible, bool instant = false)
        {
            EnsureInit();
            bool wasVisible = targetVisible;
            targetVisible = visible;

            if (visible && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (visible)
            {
                var texts = GetComponentsInChildren<TMPro.TMP_Text>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null && texts[i].gameObject.activeInHierarchy)
                    {
                        // ForceMeshUpdate invokes ThaiTMPShaper via TEXT_CHANGED_EVENT exactly once
                        texts[i].ForceMeshUpdate();
                    }
                }
            }

            if (instant)
            {
                delayRemaining = 0f;
                progress = visible ? 1f : 0f;
                ApplyState();
                if (!visible) gameObject.SetActive(false);
                return;
            }

            if (visible)
            {
                // Brief stagger so outgoing panel finishes fading out first (zero ghosting/afterimage)
                if (!wasVisible && progress <= 0.05f)
                {
                    delayRemaining = fadeInDelay;
                    canvasGroup.alpha = 0f;
                }
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            else
            {
                delayRemaining = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void Update()
        {
            EnsureInit();

            if (delayRemaining > 0f)
            {
                delayRemaining -= Time.unscaledDeltaTime;
                return;
            }

            float target = targetVisible ? 1f : 0f;
            if (Mathf.Approximately(progress, target))
            {
                if (!targetVisible && gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            float duration = targetVisible ? Mathf.Max(0.05f, fadeInDuration) : Mathf.Max(0.04f, fadeOutDuration);
            float step = Time.unscaledDeltaTime / duration;
            progress = Mathf.MoveTowards(progress, target, step);
            ApplyState();

            if (!targetVisible && progress <= 0.001f)
            {
                gameObject.SetActive(false);
            }
        }

        private void ApplyState()
        {
            if (targetVisible)
            {
                // Smooth EaseOutQuart fade-in with subtle upward float
                float inv = 1f - progress;
                float eased = 1f - inv * inv * inv * inv;
                canvasGroup.alpha = eased;
                rectTransform.anchoredPosition = restingPos + new Vector2(0f, (1f - eased) * -slideOffsetY);
            }
            else
            {
                // Clean, stationary fade-out (no sliding smear / no afterimage)
                float eased = progress * progress;
                canvasGroup.alpha = eased;
                rectTransform.anchoredPosition = restingPos;
            }

            canvasGroup.blocksRaycasts = targetVisible;
            canvasGroup.interactable = targetVisible;
        }
    }

    /// <summary>
    /// Gentle, tactile press-scale micro-interaction for pill buttons & emotion chips.
    /// </summary>
    public class SoftButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float pressedScale = 0.96f;
        public float smoothSpeed = 16f;

        private Vector3 baseScale = Vector3.one;
        private float targetMul = 1f;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData) => targetMul = pressedScale;
        public void OnPointerUp(PointerEventData eventData) => targetMul = 1f;
        public void OnPointerExit(PointerEventData eventData) => targetMul = 1f;

        private void Update()
        {
            Vector3 desired = baseScale * targetMul;
            if ((transform.localScale - desired).sqrMagnitude > 0.00001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, desired, Time.unscaledDeltaTime * smoothSpeed);
            }
        }
    }
}
