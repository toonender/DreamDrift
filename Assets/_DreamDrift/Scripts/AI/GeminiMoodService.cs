using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using DreamDrift.Core;

namespace DreamDrift.AI
{
    public class GeminiMoodService : MonoBehaviour
    {
        private const string ApiKeyPref = "DreamDrift_GeminiApiKey";
        private const string DemoModePref = "DreamDrift_DemoMode";

        [Header("Gemini API Configuration")]
        [Tooltip("Leave empty to configure at runtime via Settings or use Smart Demo Fallback.")]
        [SerializeField] private string defaultApiKey = "";
        [SerializeField] private string modelName = "gemini-2.5-flash";
        [SerializeField] private float timeoutSeconds = 18f;

        private readonly List<string> recentConversation = new List<string>();

        public string ApiKey
        {
            get
            {
                string saved = PlayerPrefs.GetString(ApiKeyPref, "");
                return string.IsNullOrWhiteSpace(saved) ? defaultApiKey : saved;
            }
            set
            {
                PlayerPrefs.SetString(ApiKeyPref, value?.Trim() ?? "");
                PlayerPrefs.Save();
            }
        }

        public bool ForceDemoMode
        {
            get => PlayerPrefs.GetInt(DemoModePref, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(DemoModePref, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public bool IsAnalyzing { get; private set; }

        public void AnalyzeMood(string userText, Action<MoodAnalysisResult, bool, string> onComplete)
        {
            if (IsAnalyzing) return;
            StartCoroutine(AnalyzeCoroutine(userText, onComplete));
        }

        private IEnumerator AnalyzeCoroutine(string userText, Action<MoodAnalysisResult, bool, string> onComplete)
        {
            IsAnalyzing = true;
            string cleaned = string.IsNullOrWhiteSpace(userText) ? "อยากพักผ่อนก่อนนอน" : userText.Trim();

            if (ForceDemoMode || string.IsNullOrWhiteSpace(ApiKey))
            {
                yield return new WaitForSeconds(0.45f);
                MoodAnalysisResult localResult = AnalyzeLocally(cleaned);
                RememberTurn(cleaned, localResult.empatheticReply);
                IsAnalyzing = false;
                onComplete?.Invoke(localResult, false, BuildMixSummary("Smart Local AI", localResult));
                yield break;
            }

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={UnityWebRequest.EscapeURL(ApiKey)}";

            string systemPrompt =
                "You are DreamDrift, a warm, empathetic, positive Thai bedtime companion AI inside a 2D sleep app. " +
                "The user may: (1) request specific ambient sounds (rain/ฝน, campfire/กองไฟ, ocean waves/คลื่นทะเล/น้ำ, night crickets/จิ้งหรีด/ธรรมชาติกลางคืน), " +
                "(2) have a normal friendly chat, (3) ask for encouragement/positive energy, or (4) ask for sleep/stress advice. " +
                "Respond naturally in warm Thai matching what the user asked! If they request specific sounds, set those sound levels high (0.65-0.95) and unrequested ones low (0.0-0.2), and mention the sound adjustment warmly in your reply. " +
                "Return ONLY a valid JSON object with these exact keys: " +
                "moodLabel (short Thai mood/topic title max 22 chars), " +
                "stressScore (integer 0-100), " +
                "empatheticReply (warm natural Thai reply 1-3 short sentences: casual chat, positive encouragement, or helpful advice depending on user's message), " +
                "recommendedMinigame (\"Breathing\" for stress/fatigue/sleep tips or \"WorryLantern\" for overthinking/worries/sadness), " +
                "skyTopHex (dark night hex like #0B132B), " +
                "skyBottomHex (deep twilight hex like #1E2F5E), " +
                "globalLightHex (soft pastel tint hex like #9BB4FF), " +
                "rainLevel (float 0.0-1.0), " +
                "campfireLevel (float 0.0-1.0), " +
                "waveLevel (float 0.0-1.0), " +
                "cricketLevel (float 0.0-1.0).";

            string contextualMessage = BuildContextualPrompt(cleaned);
            string requestJson = BuildRequestPayload(systemPrompt, contextualMessage);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(requestJson);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = Mathf.RoundToInt(timeoutSeconds);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string rawResponse = request.downloadHandler.text;
                    if (TryParseGeminiResponse(rawResponse, out MoodAnalysisResult parsed))
                    {
                        RememberTurn(cleaned, parsed.empatheticReply);
                        IsAnalyzing = false;
                        onComplete?.Invoke(parsed, true, BuildMixSummary("Gemini Live AI", parsed));
                        yield break;
                    }
                }

                // Graceful fallback so live demo never breaks
                MoodAnalysisResult fallback = AnalyzeLocally(cleaned);
                RememberTurn(cleaned, fallback.empatheticReply);
                IsAnalyzing = false;
                onComplete?.Invoke(fallback, false, BuildMixSummary("Smart Local AI", fallback));
            }
        }

        private string BuildContextualPrompt(string latestUserMessage)
        {
            if (recentConversation.Count == 0) return latestUserMessage;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Recent chat context:");
            for (int i = 0; i < recentConversation.Count; i++)
            {
                sb.AppendLine(recentConversation[i]);
            }
            sb.Append("Latest user message: ").Append(latestUserMessage);
            return sb.ToString();
        }

        private void RememberTurn(string userMsg, string aiReply)
        {
            recentConversation.Add("User: " + userMsg);
            recentConversation.Add("AI: " + aiReply);
            while (recentConversation.Count > 6)
            {
                recentConversation.RemoveAt(0);
            }
        }

        private static string BuildMixSummary(string engineName, MoodAnalysisResult r)
        {
            return $"{engineName} • ฝน {Mathf.RoundToInt(r.rainLevel * 100)}% • ไฟ {Mathf.RoundToInt(r.campfireLevel * 100)}% • คลื่น {Mathf.RoundToInt(r.waveLevel * 100)}% • กลางคืน {Mathf.RoundToInt(r.cricketLevel * 100)}%";
        }

        private string BuildRequestPayload(string systemInstruction, string userMessage)
        {
            string sysEsc = EscapeJson(systemInstruction);
            string usrEsc = EscapeJson(userMessage);

            return "{"
                + "\"system_instruction\":{\"parts\":[{\"text\":\"" + sysEsc + "\"}]},"
                + "\"contents\":[{\"role\":\"user\",\"parts\":[{\"text\":\"" + usrEsc + "\"}]}],"
                + "\"generationConfig\":{"
                + "\"temperature\":0.75,"
                + "\"responseMimeType\":\"application/json\""
                + "}"
                + "}";
        }

        private bool TryParseGeminiResponse(string rawJson, out MoodAnalysisResult result)
        {
            result = null;
            try
            {
                GeminiEnvelope envelope = JsonUtility.FromJson<GeminiEnvelope>(rawJson);
                if (envelope == null || envelope.candidates == null || envelope.candidates.Length == 0)
                    return false;

                var parts = envelope.candidates[0]?.content?.parts;
                if (parts == null || parts.Length == 0)
                    return false;

                string text = parts[0].text;
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                text = text.Trim();
                if (text.StartsWith("```"))
                {
                    int firstNewline = text.IndexOf('\n');
                    int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                    if (firstNewline >= 0 && lastFence > firstNewline)
                    {
                        text = text.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
                    }
                }

                MoodAnalysisResult parsed = JsonUtility.FromJson<MoodAnalysisResult>(text);
                if (parsed == null || string.IsNullOrWhiteSpace(parsed.empatheticReply))
                    return false;

                result = parsed;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DreamDrift] Gemini JSON parse fallback: {ex.Message}");
                return false;
            }
        }

        private MoodAnalysisResult AnalyzeLocally(string text)
        {
            string lower = text.ToLowerInvariant();

            // 1. Check explicit soundscape requests ("อยากฟังเสียง...", "เปิดเสียง...", "เงียบๆ")
            bool wantsRain = ContainsAny(lower, "ฝน", "rain", "พายุ", "ฟ้าร้อง");
            bool wantsFire = ContainsAny(lower, "ไฟ", "กองไฟ", "ผิงไฟ", "fire", "campfire", "แคมป์", "อบอุ่น");
            bool wantsWave = ContainsAny(lower, "ทะเล", "คลื่น", "น้ำ", "ชายหาด", "wave", "ocean", "sea", "water");
            bool wantsCricket = ContainsAny(lower, "จิ้งหรีด", "ป่า", "กลางคืน", "แมลง", "หิ่งห้อย", "cricket", "forest", "night");
            bool wantsQuiet = ContainsAny(lower, "เงียบ", "ปิดเสียง", "เบาๆ", "quiet", "mute", "silence");

            if (wantsRain || wantsFire || wantsWave || wantsCricket || wantsQuiet)
            {
                bool heavy = ContainsAny(lower, "หนัก", "ดัง", "แรง", "เยอะ", "heavy", "loud");
                bool soft = ContainsAny(lower, "เบา", "นิด", "อ่อน", "soft", "light", "gentle");
                float targetLevel = heavy ? 0.90f : (soft ? 0.40f : 0.78f);

                float rain = wantsQuiet ? 0.05f : (wantsRain ? targetLevel : 0.08f);
                float fire = wantsQuiet ? 0.08f : (wantsFire ? targetLevel : 0.12f);
                float wave = wantsQuiet ? 0.05f : (wantsWave ? targetLevel : 0.08f);
                float cricket = wantsQuiet ? 0.05f : (wantsCricket ? targetLevel : 0.10f);

                List<string> chosen = new List<string>();
                if (wantsRain) chosen.Add("เสียงฝน");
                if (wantsFire) chosen.Add("เสียงกองไฟอุ่นๆ");
                if (wantsWave) chosen.Add("เสียงคลื่นน้ำ");
                if (wantsCricket) chosen.Add("เสียงธรรมชาติยามค่ำ");

                string reply = wantsQuiet
                    ? "จัดบรรยากาศเงียบสงบให้แล้วนะ หรี่เสียงทุกอย่างลงเบาๆ ให้เธอได้พักสายตาและปล่อยใจให้สบายที่สุดเลย"
                    : $"จัดให้เลย! ปรับหน้าเสียงเป็น{string.Join(" ผสมกับ", chosen)}ให้เรียบร้อยแล้วนะ ลองหลับตาฟังสบายๆ หรือไปปรับละเอียดเพิ่มในหน้าเสียงได้เลย";

                return new MoodAnalysisResult
                {
                    moodLabel = wantsQuiet ? "เงียบสงบ ผ่อนคลาย" : "จัดเสียงตามใจเธอ",
                    stressScore = 25,
                    empatheticReply = reply,
                    recommendedMinigame = wantsFire || wantsCricket ? "WorryLantern" : "Breathing",
                    skyTopHex = wantsWave ? "#081C2E" : (wantsFire ? "#161026" : "#0B152C"),
                    skyBottomHex = wantsWave ? "#143A52" : (wantsFire ? "#32213F" : "#1C2F57"),
                    globalLightHex = wantsFire ? "#F4C29F" : (wantsWave ? "#8FD8E8" : "#A7B9FF"),
                    rainLevel = rain,
                    campfireLevel = fire,
                    waveLevel = wave,
                    cricketLevel = cricket
                };
            }

            // 2. Advice / Sleep Tips ("แนะนำ", "ทำยังไง", "วิธี", "ช่วยหน่อย")
            if (ContainsAny(lower, "แนะนำ", "ทำยังไง", "ทำไง", "วิธี", "ช่วยหน่อย", "หลับยาก", "advice", "tip", "how to"))
            {
                return new MoodAnalysisResult
                {
                    moodLabel = "คำแนะนำก่อนนอน",
                    stressScore = 52,
                    empatheticReply = "ลองวางเรื่องที่ต้องคิดไว้ก่อนสัก 5 นาทีนะ เริ่มจากหายใจแบบ 4-7-8 สัก 3 รอบจะช่วยลดอัตราเต้นหัวใจได้ไวมาก แล้วเปิดเสียงฝนเบาๆ คลอไว้จะหลับง่ายขึ้นเยอะเลย",
                    recommendedMinigame = "Breathing",
                    skyTopHex = "#0A182B",
                    skyBottomHex = "#1A3550",
                    globalLightHex = "#95D5E0",
                    rainLevel = 0.55f,
                    campfireLevel = 0.35f,
                    waveLevel = 0.30f,
                    cricketLevel = 0.20f
                };
            }

            // 3. Encouragement / Positive Energy ("กำลังใจ", "ท้อ", "แย่", "สู้", "ไม่ไหว", "เหนื่อยใจ", "กลัวทำไม่ได้")
            if (ContainsAny(lower, "กำลังใจ", "ท้อ", "แย่", "ไม่ไหว", "เหนื่อยใจ", "กลัวทำไม่ได้", "สู้", "ผิดหวัง", "encourage", "cheer", "positive"))
            {
                return new MoodAnalysisResult
                {
                    moodLabel = "เติมพลังบวกก่อนนอน",
                    stressScore = 62,
                    empatheticReply = "เก่งมากแล้วนะที่ผ่านวันนี้มาได้ถึงตรงนี้ ไม่จำเป็นต้องสมบูรณ์แบบทุกวันหรอก คืนนี้อนุญาตให้ตัวเองพักผ่อนอย่างภูมิใจ พรุ่งนี้ค่อยเริ่มต้นใหม่ด้วยใจที่สดใสกันนะ",
                    recommendedMinigame = "WorryLantern",
                    skyTopHex = "#15112B",
                    skyBottomHex = "#34244C",
                    globalLightHex = "#F2C4A2",
                    rainLevel = 0.25f,
                    campfireLevel = 0.78f,
                    waveLevel = 0.20f,
                    cricketLevel = 0.35f
                };
            }

            // 4. Casual Friendly Chat ("สวัสดี", "หวัดดี", "ชื่ออะไร", "ทำอะไร", "คุย", "เป็นเพื่อน", "hi", "hello")
            if (ContainsAny(lower, "สวัสดี", "หวัดดี", "ดีจ้า", "ชื่ออะไร", "เป็นใคร", "ทำอะไรได้", "คุยเป็นเพื่อน", "เหงาๆ", "hi", "hello", "hey"))
            {
                return new MoodAnalysisResult
                {
                    moodLabel = "คุยเล่นสบายๆ ยามดึก",
                    stressScore = 20,
                    empatheticReply = "สวัสดีตอนดึกนะ! เราคือเพื่อนคุยก่อนนอนของเธอเอง คืนนี้อยากเล่าเรื่องที่เจอมาวันนี้ อยากขอกำลังใจ หรืออยากให้เปิดเสียงฝน/กองไฟ/คลื่นทะเล บอกมาได้เลยนะ",
                    recommendedMinigame = "Breathing",
                    skyTopHex = "#0B132B",
                    skyBottomHex = "#1E2E5A",
                    globalLightHex = "#A8BAFF",
                    rainLevel = 0.30f,
                    campfireLevel = 0.55f,
                    waveLevel = 0.20f,
                    cricketLevel = 0.35f
                };
            }

            // 5. Overthinking / worries -> Worry Lantern
            if (ContainsAny(lower, "คิดมาก", "กังวล", "ฟุ้งซ่าน", "กลัว", "พรุ่งนี้", "สอบ", "งาน", "overthink", "worry", "anxious", "exam", "work"))
            {
                return new MoodAnalysisResult
                {
                    moodLabel = "คิดมาก ฟุ้งซ่าน",
                    stressScore = 76,
                    empatheticReply = "ดูเหมือนในหัวจะมีเรื่องให้คิดเยอะเลยใช่ไหม ลองฝากเรื่องกังวลไว้บนโคมลอยแล้วปัดขึ้นฟ้าไปก่อนนะ คืนนี้ให้สมองได้พักสักหน่อย",
                    recommendedMinigame = "WorryLantern",
                    skyTopHex = "#120E2B",
                    skyBottomHex = "#2E235C",
                    globalLightHex = "#C2AEFF",
                    rainLevel = 0.65f,
                    campfireLevel = 0.45f,
                    waveLevel = 0.25f,
                    cricketLevel = 0.20f
                };
            }

            // 6. Sad / lonely / heavy heart -> Warm campfire + Worry Lantern
            if (ContainsAny(lower, "เศร้า", "เหงา", "ร้องไห้", "เสียใจ", "โดดเดี่ยว", "sad", "lonely", "cry", "down"))
            {
                return new MoodAnalysisResult
                {
                    moodLabel = "ใจล้า ต้องการความอุ่น",
                    stressScore = 68,
                    empatheticReply = "คืนนี้ไม่ต้องแบกความรู้สึกไว้คนเดียวแล้วนะ นั่งผิงไฟอุ่นๆ ตรงนี้กับเรา แล้วค่อยๆ ปล่อยความไม่สบายใจลอยหายไปกับโคมลอยกัน",
                    recommendedMinigame = "WorryLantern",
                    skyTopHex = "#171126",
                    skyBottomHex = "#3A2542",
                    globalLightHex = "#F0B6A8",
                    rainLevel = 0.25f,
                    campfireLevel = 0.85f,
                    waveLevel = 0.15f,
                    cricketLevel = 0.40f
                };
            }

            // 7. High stress / tired / panic / insomnia -> Breathing 4-7-8
            if (ContainsAny(lower, "เครียด", "เหนื่อย", "ปวดหัว", "นอนไม่หลับ", "หัวใจ", "กดดัน", "stress", "tired", "insomnia", "exhausted", "panic"))
            {
                return new MoodAnalysisResult
                {
                    moodLabel = "ตึงเครียด ล้าสะสม",
                    stressScore = 82,
                    empatheticReply = "วันนี้เธอเหนื่อยมามากพอแล้ว วางทุกอย่างลงก่อนนะ มาหายใจช้าๆ ตามจังหวะแสง 4-7-8 ให้หัวใจเต้นช้าลงพร้อมเสียงฝนเย็นๆ กัน",
                    recommendedMinigame = "Breathing",
                    skyTopHex = "#0B1B2B",
                    skyBottomHex = "#183B4E",
                    globalLightHex = "#8EE0D6",
                    rainLevel = 0.80f,
                    campfireLevel = 0.35f,
                    waveLevel = 0.55f,
                    cricketLevel = 0.15f
                };
            }

            // 8. Default warm conversational reply
            return new MoodAnalysisResult
            {
                moodLabel = "ผ่อนคลาย พร้อมเข้านอน",
                stressScore = 28,
                empatheticReply = "ขอบคุณที่เล่าให้ฟังนะ คืนนี้เราอยู่เป็นเพื่อนตรงนี้เสมอ ลองหลับตาฟังเสียงธรรมชาติเบาๆ หรือบอกได้เลยนะถ้าอยากปรับเสียงฝน กองไฟ หรือคลื่นทะเลเพิ่ม",
                recommendedMinigame = "Breathing",
                skyTopHex = "#0A142B",
                skyBottomHex = "#1C2F5C",
                globalLightHex = "#A2BAFF",
                rainLevel = 0.35f,
                campfireLevel = 0.55f,
                waveLevel = 0.25f,
                cricketLevel = 0.35f
            };
        }

        private static bool ContainsAny(string text, params string[] keywords)
        {
            foreach (string k in keywords)
            {
                if (text.Contains(k)) return true;
            }
            return false;
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        [Serializable] private class GeminiEnvelope { public Candidate[] candidates; }
        [Serializable] private class Candidate { public Content content; }
        [Serializable] private class Content { public Part[] parts; }
        [Serializable] private class Part { public string text; }
    }
}
