using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Global UI Modal pencere yöneticisi.
    /// Ekranda herhangi bir modal pop-up (Takvim, Mağaza vb.) açıkken
    /// arka plandaki 3D tıklamaları, kapı etkileşimlerini ve WASD kamera hareketlerini engeller.
    /// Ayrıca dinamik bildirim/uyarı modal pencereleri üretir (ShowModal).
    /// </summary>
    public static class ModalManager
    {
        private static bool modalState = false;

        public static bool IsModalOpen
        {
            get
            {
                if (modalState)
                {
                    // Otomatik Kurtarma Kontrolü (Auto-Recovery Sanity Check):
                    // Ekranda aktif/görünür hiçbir modal canvas yoksa kilitlenmeyi engellemek için modalState = false yap!
                    if (!IsAnyModalCanvasActive())
                    {
                        modalState = false;
                    }
                }
                return modalState;
            }
        }

        public static void SetModalOpen(bool isOpen)
        {
            modalState = isOpen;
            Debug.Log($"[Farm2Shelf] Modal Durumu: {(modalState ? "AÇIK (Arka Plan Kilitli)" : "KAPALI (Arka Plan Serbest)")}");
        }

        private static bool IsAnyModalCanvasActive()
        {
            GameObject globalPopup = GameObject.Find("Global_Modal_Popup_Canvas");
            if (globalPopup != null && globalPopup.activeSelf) return true;

            if (EKTPhoneManager.IsTabletOpen) return true;
            if (Farm2Shelf.Environment.FieldPlotController.IsRadialMenuOpen) return true;
            if (BarnInventoryModalUI.IsBarnModalOpen) return true;

            if (StaffProfileModalUI.Instance != null && StaffProfileModalUI.Instance.IsModalOpen) return true;
            if (CustomerProfileModalUI.Instance != null && CustomerProfileModalUI.Instance.IsModalOpen) return true;
            if (EndOfDayReportModalUI.IsReportModalOpen) return true;

            if (FurnitureInfoModalUI.IsFurnitureModalOpen) return true;
            GameObject furnCanvas = GameObject.Find("Furniture_Info_Modal_Canvas");
            if (furnCanvas != null && furnCanvas.activeSelf) return true;
            GameObject furnGlobalCanvas = GameObject.Find("Global_Furniture_Info_Canvas");
            if (furnGlobalCanvas != null && furnGlobalCanvas.activeSelf) return true;
            GameObject sellConfirmCanvas = GameObject.Find("Sell_Confirm_Modal_Canvas");
            if (sellConfirmCanvas != null && sellConfirmCanvas.activeSelf) return true;

            if (CalendarPopupUI.IsCalendarModalOpen) return true;
            GameObject calCanvas = GameObject.Find("Stardew_Calendar_Modal_Canvas");
            if (calCanvas != null && calCanvas.activeSelf) return true;

            return false;
        }

        /// <summary>
        /// Ekrana başlık, mesaj ve buton içeren şık bir modal iletişim kutusu açar.
        /// </summary>
        public static void ShowModal(string title, string message, string buttonText = "Tamam")
        {
            if (string.IsNullOrEmpty(buttonText) || buttonText == "Tamam")
            {
                buttonText = LocalizationManager.L("Btn_OK", "Tamam", "OK");
            }

            SetModalOpen(true);

            // Varsa eski popup'ı temizle
            GameObject existing = GameObject.Find("Global_Modal_Popup_Canvas");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject canvasObj = new GameObject("Global_Modal_Popup_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Arka Plan Karartma (Overlay Backdrop)
            GameObject backdrop = new GameObject("Modal_Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);
            bdImg.raycastTarget = true;

            // Modal Kutusu
            GameObject boxObj = new GameObject("Modal_Box");
            boxObj.transform.SetParent(backdrop.transform, false);

            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(600f, 320f);

            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.sprite = UIStyleUtility.CreateOutlinePillSprite(600, 320, 16, 2, new Color(0.95f, 0.40f, 0.55f), new Color(0.12f, 0.15f, 0.20f, 0.98f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Başlık
            GameObject titleObj = new GameObject("Modal_Title");
            titleObj.transform.SetParent(boxObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 110f);
            tRect.sizeDelta = new Vector2(540f, 40f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = title;
            tText.fontSize = 22;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.95f, 0.45f, 0.60f);

            // Mesaj Metni
            GameObject msgObj = new GameObject("Modal_Message");
            msgObj.transform.SetParent(boxObj.transform, false);
            RectTransform mRect = msgObj.AddComponent<RectTransform>();
            mRect.anchoredPosition = new Vector2(0f, 15f);
            mRect.sizeDelta = new Vector2(520f, 140f);

            Text mText = msgObj.AddComponent<Text>();
            mText.font = font;
            mText.text = message;
            mText.fontSize = 16;
            mText.alignment = TextAnchor.MiddleCenter;
            mText.color = Color.white;

            // Kapat / Onay Butonu
            GameObject btnObj = new GameObject("Modal_Button");
            btnObj.transform.SetParent(boxObj.transform, false);
            RectTransform bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(0f, -110f);
            bRect.sizeDelta = new Vector2(180f, 44f);

            Image bBg = btnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(180, 44, 10, new Color(0.95f, 0.40f, 0.55f));

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bBg;
            btn.onClick.AddListener(() => {
                SetModalOpen(false);
                Object.Destroy(canvasObj);
            });

            GameObject btnTxtObj = new GameObject("Label");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRect = btnTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            Text btText = btnTxtObj.AddComponent<Text>();
            btText.font = font;
            btText.text = buttonText;
            btText.fontSize = 16;
            btText.fontStyle = FontStyle.Bold;
            btText.alignment = TextAnchor.MiddleCenter;
            btText.color = Color.white;
        }

        /// <summary>
        /// Ekrana başlık, mesaj ve Evet / Hayır butonları içeren şık bir onay pop-up penceresi açar.
        /// </summary>
        public static void ShowConfirmModal(string title, string message, System.Action onConfirm, string confirmText = "Evet", string cancelText = "Hayır", System.Action onCancel = null)
        {
            if (string.IsNullOrEmpty(confirmText) || confirmText == "Evet")
            {
                confirmText = LocalizationManager.L("Btn_Yes", "Evet", "Yes");
            }
            if (string.IsNullOrEmpty(cancelText) || cancelText == "Hayır")
            {
                cancelText = LocalizationManager.L("Btn_No", "Hayır", "No");
            }

            SetModalOpen(true);

            // Varsa eski popup'ı temizle
            GameObject existing = GameObject.Find("Global_Modal_Popup_Canvas");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject canvasObj = new GameObject("Global_Modal_Popup_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Arka Plan Karartma (Overlay Backdrop)
            GameObject backdrop = new GameObject("Modal_Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);
            bdImg.raycastTarget = true;

            // Modal Kutusu
            GameObject boxObj = new GameObject("Modal_Box");
            boxObj.transform.SetParent(backdrop.transform, false);

            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(620f, 340f);

            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.sprite = UIStyleUtility.CreateOutlinePillSprite(620, 340, 16, 2, new Color(0.95f, 0.40f, 0.55f), new Color(0.12f, 0.15f, 0.20f, 0.98f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Başlık
            GameObject titleObj = new GameObject("Modal_Title");
            titleObj.transform.SetParent(boxObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 120f);
            tRect.sizeDelta = new Vector2(560f, 40f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = title;
            tText.fontSize = 26;
            tText.resizeTextForBestFit = true;
            tText.resizeTextMinSize = 16;
            tText.resizeTextMaxSize = 28;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.95f, 0.45f, 0.60f);

            // Mesaj Metni
            GameObject msgObj = new GameObject("Modal_Message");
            msgObj.transform.SetParent(boxObj.transform, false);
            RectTransform mRect = msgObj.AddComponent<RectTransform>();
            mRect.anchoredPosition = new Vector2(0f, 20f);
            mRect.sizeDelta = new Vector2(540f, 150f);

            Text mText = msgObj.AddComponent<Text>();
            mText.font = font;
            mText.text = message;
            mText.fontSize = 19;
            mText.resizeTextForBestFit = true;
            mText.resizeTextMinSize = 13;
            mText.resizeTextMaxSize = 20;
            mText.alignment = TextAnchor.MiddleCenter;
            mText.color = Color.white;

            // Evet / Onay Butonu (Sağ Taraf)
            GameObject confirmBtnObj = new GameObject("Confirm_Button");
            confirmBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform confirmRect = confirmBtnObj.AddComponent<RectTransform>();
            confirmRect.anchoredPosition = new Vector2(110f, -110f);
            confirmRect.sizeDelta = new Vector2(170f, 44f);

            Image confirmBg = confirmBtnObj.AddComponent<Image>();
            confirmBg.sprite = UIStyleUtility.CreateRoundedPillSprite(170, 44, 10, new Color(0.20f, 0.75f, 0.45f));

            Button confirmBtn = confirmBtnObj.AddComponent<Button>();
            confirmBtn.targetGraphic = confirmBg;
            confirmBtn.onClick.AddListener(() => {
                SetModalOpen(false);
                Object.Destroy(canvasObj);
                onConfirm?.Invoke();
            });

            GameObject confirmTxtObj = new GameObject("Label");
            confirmTxtObj.transform.SetParent(confirmBtnObj.transform, false);
            RectTransform cLabelRect = confirmTxtObj.AddComponent<RectTransform>();
            cLabelRect.anchorMin = Vector2.zero;
            cLabelRect.anchorMax = Vector2.one;

            Text confirmTxt = confirmTxtObj.AddComponent<Text>();
            confirmTxt.font = font;
            confirmTxt.text = confirmText;
            confirmTxt.fontSize = 19;
            confirmTxt.resizeTextForBestFit = true;
            confirmTxt.resizeTextMinSize = 12;
            confirmTxt.resizeTextMaxSize = 20;
            confirmTxt.fontStyle = FontStyle.Bold;
            confirmTxt.alignment = TextAnchor.MiddleCenter;
            confirmTxt.color = Color.white;

            // Hayır / İptal Butonu (Sol Taraf)
            GameObject cancelBtnObj = new GameObject("Cancel_Button");
            cancelBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform cancelRect = cancelBtnObj.AddComponent<RectTransform>();
            cancelRect.anchoredPosition = new Vector2(-110f, -110f);
            cancelRect.sizeDelta = new Vector2(170f, 44f);

            Image cancelBg = cancelBtnObj.AddComponent<Image>();
            cancelBg.sprite = UIStyleUtility.CreateRoundedPillSprite(170, 44, 10, new Color(0.40f, 0.45f, 0.55f));

            Button cancelBtn = cancelBtnObj.AddComponent<Button>();
            cancelBtn.targetGraphic = cancelBg;
            cancelBtn.onClick.AddListener(() => {
                SetModalOpen(false);
                Object.Destroy(canvasObj);
                onCancel?.Invoke();
            });

            GameObject cancelTxtObj = new GameObject("Label");
            cancelTxtObj.transform.SetParent(cancelBtnObj.transform, false);
            RectTransform cancelLabelRect = cancelTxtObj.AddComponent<RectTransform>();
            cancelLabelRect.anchorMin = Vector2.zero;
            cancelLabelRect.anchorMax = Vector2.one;

            Text cancelTxt = cancelTxtObj.AddComponent<Text>();
            cancelTxt.font = font;
            cancelTxt.text = cancelText;
            cancelTxt.fontSize = 19;
            cancelTxt.resizeTextForBestFit = true;
            cancelTxt.resizeTextMinSize = 12;
            cancelTxt.resizeTextMaxSize = 20;
            cancelTxt.fontStyle = FontStyle.Bold;
            cancelTxt.alignment = TextAnchor.MiddleCenter;
            cancelTxt.color = Color.white;
        }
    }
}
