using System;
using UnityEngine;

namespace DreamDrift.Core
{
    public enum AppScreen
    {
        Home = 0,
        AIMood = 1,
        Soundscape = 2,
        Breathing = 3,
        WorryLantern = 4
    }

    public enum RecommendedMinigameType
    {
        Breathing,
        WorryLantern
    }

    [Serializable]
    public class MoodAnalysisResult
    {
        public string moodLabel = "สงบ พร้อมพักผ่อน";
        public int stressScore = 35;
        public string empatheticReply = "คืนนี้ค่อยๆ ปล่อยวางเรื่องราวทั้งวันลงนะ ให้เสียงธรรมชาติและแสงไฟอุ่นๆ อยู่เป็นเพื่อนก่อนนอน";
        public string recommendedMinigame = "Breathing";
        public string skyTopHex = "#0B132B";
        public string skyBottomHex = "#1C2E59";
        public string globalLightHex = "#9BB4FF";
        public float rainLevel = 0.45f;
        public float campfireLevel = 0.60f;
        public float waveLevel = 0.25f;
        public float cricketLevel = 0.30f;

        public RecommendedMinigameType GetRecommendedType()
        {
            if (!string.IsNullOrEmpty(recommendedMinigame) &&
                recommendedMinigame.IndexOf("lantern", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return RecommendedMinigameType.WorryLantern;
            }
            return RecommendedMinigameType.Breathing;
        }

        public Color GetColorOrDefault(string hex, Color fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color parsed))
            {
                return parsed;
            }
            return fallback;
        }
    }
}
