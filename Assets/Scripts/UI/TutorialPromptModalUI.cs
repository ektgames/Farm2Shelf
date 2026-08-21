using System;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Yeni Oyun başlatıldığında ekranın tam ortasında beliren
    /// tatlı low-poly tarzında 'Eğitime Girmek İstiyor musun?' onay menüsü.
    /// Tamamen çift dillidir (TR / EN).
    /// </summary>
    public class TutorialPromptModalUI : MonoBehaviour
    {
        private static GameObject modalInstance;
        private static Action onAcceptCallback;
        private static Action onDeclineCallback;

        public static void ShowModal(Action onAccept, Action onDecline)
        {
            onAcceptCallback = onAccept;
            onDeclineCallback = onDecline;

            if (modalInstance != null) Destroy(modalInstance);

            modalInstance = new GameObject("Modal_TutorialPrompt");
            modalInstance.AddComponent<TutorialPromptModalUI>();

            Canvas canvas = modalInstance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;

            CanvasScaler scaler = modalInstance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            modalInstance.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 20);

            // 1. Karartma Arka Planı
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(modalInstance.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.07f, 0.11f, 0.85f);
            bdImg.raycastTarget = true;

            // 2. Ana Kart (580 x 440)
            GameObject boxObj = new GameObject("CardBox");
            boxObj.transform.SetParent(backdrop.transform, false);
            RectTransform bRect = boxObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = Vector2.zero;
            bRect.sizeDelta = new Vector2(580f, 440f);

            Image bImg = boxObj.AddComponent<Image>();
            bImg.sprite = UIStyleUtility.CreateOutlinePillSprite(580, 440, 24, 3, new Color(0.20f, 0.85f, 0.55f), new Color(0.10f, 0.13f, 0.18f, 0.98f));

            // Başlık Rozeti (Header Pill)
            GameObject headerObj = new GameObject("HeaderBadge");
            headerObj.transform.SetParent(boxObj.transform, false);
            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 175f);
            hRect.sizeDelta = new Vector2(500f, 54f);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(500, 54, 16, new Color(0.14f, 0.20f, 0.28f, 0.95f));

            GameObject hTextObj = new GameObject("Text");
            hTextObj.transform.SetParent(headerObj.transform, false);
            RectTransform htRect = hTextObj.AddComponent<RectTransform>();
            htRect.anchorMin = Vector2.zero;
            htRect.anchorMax = Vector2.one;

            Text hTxt = hTextObj.AddComponent<Text>();
            hTxt.font = font;
            hTxt.text = "🎓 " + LocalizationManager.L("TutPrompt_Title", "EĞİTİME GİRMEK İSTİYOR MUSUN?", "WOULD YOU LIKE TO PLAY TUTORIAL?");
            hTxt.fontSize = 20;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.color = new Color(0.30f, 0.95f, 0.65f);

            // Açıklama Metni Kutusu (Info Box)
            GameObject descObj = new GameObject("DescBox");
            descObj.transform.SetParent(boxObj.transform, false);
            RectTransform dRect = descObj.AddComponent<RectTransform>();
            dRect.anchoredPosition = new Vector2(0f, 35f);
            dRect.sizeDelta = new Vector2(500f, 180f);

            Image dBg = descObj.AddComponent<Image>();
            dBg.sprite = UIStyleUtility.CreateOutlinePillSprite(500, 180, 14, 1, new Color(0.25f, 0.35f, 0.48f, 0.6f), new Color(0.12f, 0.16f, 0.22f, 0.90f));

            GameObject dTextObj = new GameObject("Text");
            dTextObj.transform.SetParent(descObj.transform, false);
            RectTransform dtRect = dTextObj.AddComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(20f, 15f);
            dtRect.offsetMax = new Vector2(-20f, -15f);

            Text dTxt = dTextObj.AddComponent<Text>();
            dTxt.font = font;
            dTxt.text = LocalizationManager.L(
                "TutPrompt_Desc",
                "<b>Farm2Shelf dünyasına hoş geldin! 🌾🛒</b>\n\n" +
                "Oyunun temel dokunmatik kontrollerini, EKT Tablet uygulamalarını, personel işe alımını, vardiyaları, mobilya kurulumunu, toptancı siparişlerini ve çiftlik tarımını adım adım öğrenmek için <b>10 Adımlı Başlangıç Eğitimine</b> girmek ister misin?",
                "<b>Welcome to Farm2Shelf! 🌾🛒</b>\n\n" +
                "Would you like to start the <b>10-Step Guided Tutorial</b> to learn mobile camera controls, EKT Tablet apps, hiring staff, shifts, furniture setup, wholesale orders, and crop farming step by step?"
            );
            dTxt.fontSize = 15;
            dTxt.lineSpacing = 1.18f;
            dTxt.alignment = TextAnchor.MiddleCenter;
            dTxt.color = new Color(0.92f, 0.94f, 0.97f);

            // ==================== BUTONLAR (EVET & HAYIR) ====================

            // 1. EVET BUTONU (Yeşil)
            CreateButton(boxObj.transform, new Vector2(-130f, -145f), new Vector2(230f, 52f),
                "✅ " + LocalizationManager.L("Btn_YesTutorial", "EVET, BAŞLA!", "YES, START!"),
                new Color(0.20f, 0.82f, 0.42f), font, () => {
                    CloseModal();
                    onAcceptCallback?.Invoke();
                });

            // 2. HAYIR BUTONU (Gri / Kırmızımsı)
            CreateButton(boxObj.transform, new Vector2(130f, -145f), new Vector2(230f, 52f),
                "❌ " + LocalizationManager.L("Btn_NoTutorial", "HAYIR, ATLA", "NO, SKIP"),
                new Color(0.35f, 0.40f, 0.48f), font, () => {
                    CloseModal();
                    onDeclineCallback?.Invoke();
                });
        }

        private static void CreateButton(Transform parent, Vector2 pos, Vector2 size, string text, Color color, Font font, Action onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + text);
            btnObj.transform.SetParent(parent, false);
            RectTransform bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = pos;
            bRect.sizeDelta = size;

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), 16, color);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = text;
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        public static void CloseModal()
        {
            if (modalInstance != null) Destroy(modalInstance);
        }

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
            }
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            if (modalInstance != null)
            {
                ShowModal(onAcceptCallback, onDeclineCallback);
            }
        }
    }
}
