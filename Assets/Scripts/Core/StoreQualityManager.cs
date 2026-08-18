using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Oyundaki Mağaza Kalite Seviyesi ve Yıldız Puanı (Quality Level & Star Score) Yönetim Sınıfı.
    /// Dükkan temizliği, ürün bulma/bulamama durumlarına göre kalite puanı artar/düşer.
    /// Kalite puanı ve seviyesi sonsuza kadar sınırsız şekilde büyüyebilir.
    /// </summary>
    public class StoreQualityManager : MonoBehaviour
    {
        public static StoreQualityManager Instance { get; private set; }

        public int QualityScore { get; private set; } = 0;
        public int QualityLevel { get; private set; } = 0;

        public event Action<int, int> OnQualityChanged; // (currentScore, currentLevel)

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Kalite puanını artırır ve gerekiyorsa seviye atlatır.
        /// </summary>
        public void AddQualityScore(int amount, Vector3 worldPos, string reason = "")
        {
            if (amount <= 0) return;

            QualityScore += amount;
            CheckLevelUp();

            OnQualityChanged?.Invoke(QualityScore, QualityLevel);
            ShowFloatingQualityPopup(worldPos, $"+{amount} ⭐", new Color(0.95f, 0.85f, 0.20f), reason);
        }

        /// <summary>
        /// Kalite puanını düşürür (Minimum 0 puana kadar).
        /// </summary>
        public void SubtractQualityScore(int amount, Vector3 worldPos, string reason = "")
        {
            if (amount <= 0) return;

            QualityScore = Mathf.Max(0, QualityScore - amount);
            CheckLevelUp();

            OnQualityChanged?.Invoke(QualityScore, QualityLevel);
            ShowFloatingQualityPopup(worldPos, $"-{amount} ⚠️", new Color(0.95f, 0.30f, 0.25f), reason);
        }

        /// <summary>
        /// Seviye eşiği hesabı (Sonsuz seviye ilerlemesi):
        /// Lv 0: 0 - 99 Puan
        /// Lv 1: 100 - 249 Puan
        /// Lv 2: 250 - 449 Puan
        /// Lv N: Formülle katlanarak sonsuza kadar artar.
        /// </summary>
        private void CheckLevelUp()
        {
            int oldLevel = QualityLevel;
            int calculatedLevel = CalculateLevelFromScore(QualityScore);

            if (calculatedLevel != oldLevel)
            {
                QualityLevel = calculatedLevel;
                if (calculatedLevel > oldLevel)
                {
                    Debug.Log($"[StoreQualityManager] TEBRİKLER! Mağaza Kalite Seviyesi Yükseldi: Seviye {QualityLevel} ⭐");
                }
            }
        }

        public static int CalculateLevelFromScore(int score)
        {
            if (score <= 0) return 0;
            
            int lvl = 0;
            int required = 100;
            int step = 150;

            while (score >= required)
            {
                lvl++;
                required += step + (lvl * 50);
            }
            return lvl;
        }

        public void SetQualityData(int score, int level)
        {
            QualityScore = Mathf.Max(0, score);
            QualityLevel = Mathf.Max(0, level);
            OnQualityChanged?.Invoke(QualityScore, QualityLevel);
        }

        private void ShowFloatingQualityPopup(Vector3 worldPos, string text, Color color, string reason)
        {
            GameObject popupObj = new GameObject("Popup_Quality_Feedback");
            popupObj.transform.position = worldPos + Vector3.up * 2.1f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 90;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320f, 65f);
            popupObj.transform.localScale = Vector3.one * 0.012f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            
            string fullText = string.IsNullOrEmpty(reason) ? text : $"{text}\n<size=14>{reason}</size>";
            txt.text = fullText;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;

            StartCoroutine(AnimateQualityPopup(popupObj));
        }

        private IEnumerator AnimateQualityPopup(GameObject popupObj)
        {
            float duration = 1.6f;
            float elapsed = 0f;
            Vector3 startPos = popupObj.transform.position;
            Vector3 endPos = startPos + Vector3.up * 1.2f;

            CanvasGroup group = popupObj.AddComponent<CanvasGroup>();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (popupObj != null)
                {
                    popupObj.transform.position = Vector3.Lerp(startPos, endPos, t);
                    if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;
                    if (group != null) group.alpha = 1f - (t * t);
                }

                yield return null;
            }

            if (popupObj != null) Destroy(popupObj);
        }
    }
}
