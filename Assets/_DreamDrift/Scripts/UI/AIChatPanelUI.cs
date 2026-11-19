using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DreamDrift.AI;
using DreamDrift.Core;

namespace DreamDrift.UI
{
    public class AIChatPanelUI : MonoBehaviour
    {
        [Header("Services")]
        public GeminiMoodService moodService;

        [Header("Input Area")]
        public TMP_InputField feelingInputField;
        public Button sendButton;
        public TMP_Text sendButtonLabel;

        [Header("Response Card")]
        public TMP_Text aiReplyText;
        public TMP_Text moodBadgeText;
        public TMP_Text stressScoreText;
        public TMP_Text sourceModeText;
        public Button launchRecommendedButton;
        public TMP_Text launchRecommendedLabel;

        private void Awake()
        {
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(SubmitFeeling);
            }

            if (feelingInputField != null)
            {
                feelingInputField.onSubmit.AddListener(_ => SubmitFeeling());
            }

            if (launchRecommendedButton != null)
            {
                launchRecommendedButton.onClick.AddListener(() =>
                {
                    if (DreamDriftManager.Instance != null)
                    {
                        DreamDriftManager.Instance.OpenRecommendedMinigame();
                    }
                });
            }
        }

        private void Start()
        {
            if (DreamDriftManager.Instance != null && DreamDriftManager.Instance.LatestMood != null)
            {
                DisplayResult(DreamDriftManager.Instance.LatestMood, "พร้อมวิเคราะห์อารมณ์ก่อนนอน");
            }
        }

        public void SelectQuickChip(string promptText)
        {
            if (feelingInputField != null)
            {
                feelingInputField.text = promptText;
            }
            SubmitFeeling();
        }

        public void SubmitFeeling()
        {
            if (moodService == null || moodService.IsAnalyzing) return;

            string text = feelingInputField != null ? feelingInputField.text : "";
            if (feelingInputField != null) feelingInputField.text = "";
            SetLoading(true);

            moodService.AnalyzeMood(text, (result, isLiveGemini, statusNote) =>
            {
                SetLoading(false);
                if (DreamDriftManager.Instance != null)
                {
                    DreamDriftManager.Instance.ApplyMoodResult(result);
                }
                DisplayResult(result, statusNote);
            });
        }

        private void SetLoading(bool loading)
        {
            if (sendButton != null) sendButton.interactable = !loading;
            if (sendButtonLabel != null) sendButtonLabel.text = loading ? "กำลังวิเคราะห์..." : "ส่งให้ AI ปรับบรรยากาศ";
            if (loading && aiReplyText != null)
            {
                aiReplyText.text = "กำลังรับฟังความรู้สึก และจัดเตรียมฉากกับเสียงธรรมชาติให้เธอนะ...";
            }
        }

        private void DisplayResult(MoodAnalysisResult result, string statusNote)
        {
            if (result == null) return;

            if (aiReplyText != null) aiReplyText.text = result.empatheticReply;
            if (moodBadgeText != null) moodBadgeText.text = $"อารมณ์: {result.moodLabel}";
            if (stressScoreText != null) stressScoreText.text = $"ระดับความตึงเครียด: {result.stressScore}%";
            if (sourceModeText != null) sourceModeText.text = statusNote;

            if (launchRecommendedLabel != null)
            {
                bool isLantern = result.GetRecommendedType() == RecommendedMinigameType.WorryLantern;
                launchRecommendedLabel.text = isLantern
                    ? "เริ่มมินิเกมแนะนำ: ปัดโคมความกังวล ->"
                    : "เริ่มมินิเกมแนะนำ: ฝึกหายใจ 4-7-8 ->";
            }
        }
    }
}
