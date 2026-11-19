using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DreamDrift.Audio;

namespace DreamDrift.Minigames
{
    public class WorryLanternMinigame : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        public TMP_InputField worryInputField;
        public RectTransform previewLanternRect;
        public TMP_Text previewLanternText;
        public RectTransform swipeArrowRect;
        public TMP_Text releasedCounterText;
        public TMP_Text statusHintText;
        public Button quickReleaseButton;

        [Header("World Spawning")]
        public WorryLanternItem lanternPrefab;
        public Transform lanternWorldContainer;
        public Camera mainCamera;
        public SoundscapeManager soundscape;

        [Header("Swipe Settings")]
        public float minSwipeDistance = 90f;

        private Vector2 dragStartPos;
        private Vector2 previewDefaultPos;
        private bool isDragging;
        private int releasedCount;

        private void Awake()
        {
            if (previewLanternRect != null)
            {
                previewDefaultPos = previewLanternRect.anchoredPosition;
            }

            if (worryInputField != null)
            {
                worryInputField.onValueChanged.AddListener(OnWorryTextChanged);
            }

            if (quickReleaseButton != null)
            {
                quickReleaseButton.onClick.AddListener(() => ReleaseLantern(0.65f));
            }
        }

        private void OnEnable()
        {
            if (previewLanternRect != null)
            {
                previewLanternRect.anchoredPosition = previewDefaultPos;
            }
            UpdateCounter();
            OnWorryTextChanged(worryInputField != null ? worryInputField.text : "");
        }

        private void Update()
        {
            if (!isDragging && previewLanternRect != null)
            {
                Vector2 bob = previewDefaultPos + new Vector2(0f, Mathf.Sin(Time.time * 2.1f) * 10f);
                previewLanternRect.anchoredPosition = Vector2.Lerp(previewLanternRect.anchoredPosition, bob, Time.deltaTime * 8f);
            }

            if (swipeArrowRect != null)
            {
                Vector2 arrowPos = swipeArrowRect.anchoredPosition;
                float bounce = Mathf.Sin(Time.time * 3.4f) * 8f;
                swipeArrowRect.localPosition = new Vector3(swipeArrowRect.localPosition.x, bounce, 0f);
            }
        }

        public void SetQuickWorry(string presetText)
        {
            if (worryInputField != null)
            {
                worryInputField.text = presetText;
            }
            OnWorryTextChanged(presetText);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            dragStartPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || previewLanternRect == null) return;
            float deltaY = Mathf.Max(-30f, eventData.position.y - dragStartPos.y);
            float deltaX = Mathf.Clamp((eventData.position.x - dragStartPos.x) * 0.35f, -120f, 120f);
            previewLanternRect.anchoredPosition = previewDefaultPos + new Vector2(deltaX, deltaY * 0.65f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            float deltaY = eventData.position.y - dragStartPos.y;
            if (deltaY >= minSwipeDistance)
            {
                float boost = Mathf.Clamp01((deltaY - minSwipeDistance) / 350f);
                ReleaseLantern(boost);
            }
        }

        public void ReleaseLantern(float boost = 0.6f)
        {
            string worryText = worryInputField != null ? worryInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(worryText))
            {
                worryText = "ความเหนื่อยล้าวันนี้";
            }

            SpawnWorldLantern(worryText, boost);

            releasedCount++;
            UpdateCounter();

            if (soundscape != null)
            {
                soundscape.PlayGentleChime(0.6f);
            }

            if (worryInputField != null)
            {
                worryInputField.text = "";
            }

            if (previewLanternRect != null)
            {
                previewLanternRect.anchoredPosition = previewDefaultPos - new Vector2(0f, 140f);
            }

            if (statusHintText != null)
            {
                statusHintText.text = "ปล่อยความกังวลขึ้นสู่ท้องฟ้าแล้ว...";
            }
        }

        private void SpawnWorldLantern(string worryText, float boost)
        {
            if (lanternPrefab == null) return;

            Camera cam = mainCamera != null ? mainCamera : Camera.main;
            Vector3 spawnPos = new Vector3(Random.Range(-1.2f, 1.2f), 5.5f, 0f);
            if (cam != null)
            {
                spawnPos = new Vector3(Random.Range(-1.2f, 1.2f), cam.transform.position.y - 2.6f, 0f);
            }

            WorryLanternItem item = Instantiate(lanternPrefab, spawnPos, Quaternion.identity, lanternWorldContainer);
            item.gameObject.SetActive(true);
            item.Launch(worryText, boost);
        }

        private void OnWorryTextChanged(string value)
        {
            if (previewLanternText != null)
            {
                previewLanternText.text = string.IsNullOrWhiteSpace(value)
                    ? "พิมพ์เรื่องกังวล\nแล้วปัดขึ้นฟ้า"
                    : value;
            }
        }

        private void UpdateCounter()
        {
            if (releasedCounterText != null)
            {
                releasedCounterText.text = $"ปล่อยความกังวลแล้ว {releasedCount} ดวง";
            }
        }
    }
}
