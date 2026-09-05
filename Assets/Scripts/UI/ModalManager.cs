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
        private static GameObject currentGlobalPopupCanvas;
        public static bool IsGlobalPopupOpen => currentGlobalPopupCanvas != null && currentGlobalPopupCanvas.activeInHierarchy;

        public static float LastModalCloseTime { get; private set; } = -1f;

        public static bool IsModalOpen => modalState || IsAnyModalCanvasActive();

        public static void SetModalOpen(bool isOpen)
        {
            if (modalState && !isOpen)
            {
                LastModalCloseTime = Time.unscaledTime;
                Farm2Shelf.Utils.TouchInputHelper.SuppressNextTap();
            }
            modalState = isOpen;
        }

        public static void CloseModal()
        {
            if (currentGlobalPopupCanvas != null)
            {
                currentGlobalPopupCanvas.SetActive(false);
                Object.Destroy(currentGlobalPopupCanvas);
                currentGlobalPopupCanvas = null;
            }

            if (EndOfDayReportModalUI.IsReportModalOpen)
            {
                return;
            }

            SetModalOpen(false);
        }

        public static bool IsAnyModalCanvasActive()
        {
            if (currentGlobalPopupCanvas != null && currentGlobalPopupCanvas.activeInHierarchy) return true;
            if (EKTPhoneManager.IsTabletOpen) return true;
            if (Farm2Shelf.Environment.FieldPlotController.IsRadialMenuOpen) return true;
            if (BarnInventoryModalUI.IsBarnModalOpen) return true;
            if (PalletStorageInventoryModalUI.IsModalOpen) return true;
            if (EndOfDayReportModalUI.IsReportModalOpen) return true;
            if (FurnitureInfoModalUI.IsFurnitureModalOpen) return true;
            if (CalendarPopupUI.IsCalendarModalOpen) return true;
            if (FieldPlotDetailModalUI.IsDetailOpen) return true;
            if (TutorialPromptModalUI.IsPromptOpen) return true;
            if (SettingsModalUI.Instance != null && SettingsModalUI.Instance.IsSettingsOpen) return true;
            if (SaveLoadSlotModalUI.Instance != null && SaveLoadSlotModalUI.Instance.IsModalOpen) return true;
            if (HowToPlayModalUI.Instance != null && HowToPlayModalUI.Instance.IsModalOpen) return true;
            if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen) return true;

            return false;
        }

        public static void ClearStaleModalState()
        {
            if (modalState && !IsAnyModalCanvasActive())
            {
                SetModalOpen(false);
            }
        }

        /// <summary>
        /// Ana menü Ayarlar / Rehber katmanları oyun sahnesine sızarsa
        /// 3D tıklamayı (palet, koli, raf) tamamen kilitler. Yerleştirme ve dünya
        /// etkileşiminden önce bu artıkları kapatır.
        /// </summary>
        public static void CloseWorldBlockingOverlays()
        {
            if (MainMenuUI.IsMenuVisible)
            {
                ClearStaleModalState();
                return;
            }

            if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen)
            {
                ClearStaleModalState();
                return;
            }

            if (SettingsModalUI.Instance != null && SettingsModalUI.Instance.IsSettingsOpen)
            {
                SettingsModalUI.Instance.HideModal();
            }
            if (HowToPlayModalUI.Instance != null && HowToPlayModalUI.Instance.IsModalOpen)
            {
                HowToPlayModalUI.Instance.HideModal();
            }

            DestroyOrphanCanvas("Settings_Modal_Canvas");
            DestroyOrphanCanvas("HowToPlay_Modal_Canvas");
            ClearStaleModalState();
        }

        private static void DestroyOrphanCanvas(string objectName)
        {
            GameObject leftover = GameObject.Find(objectName);
            if (leftover != null)
            {
                leftover.SetActive(false);
                Object.Destroy(leftover);
            }
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
            if (currentGlobalPopupCanvas != null)
            {
                Object.DestroyImmediate(currentGlobalPopupCanvas);
                currentGlobalPopupCanvas = null;
            }

            GameObject canvasObj = new GameObject("Global_Modal_Popup_Canvas");
            currentGlobalPopupCanvas = canvasObj;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

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
            bRect.sizeDelta = new Vector2(220f, 48f);

            Image bBg = btnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(220, 48, 12, new Color(0.95f, 0.40f, 0.55f));

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bBg;
            btn.onClick.AddListener(() => {
                if (canvasObj != null) canvasObj.SetActive(false);
                if (currentGlobalPopupCanvas == canvasObj) currentGlobalPopupCanvas = null;
                SetModalOpen(false);
                if (canvasObj != null) Object.Destroy(canvasObj);
            });

            GameObject btnTxtObj = new GameObject("Label");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRect = btnTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = new Vector2(12f, 2f);
            btRect.offsetMax = new Vector2(-12f, -2f);

            Text btText = btnTxtObj.AddComponent<Text>();
            btText.font = font;
            btText.text = buttonText;
            btText.fontSize = 17;
            btText.resizeTextForBestFit = true;
            btText.resizeTextMinSize = 10;
            btText.resizeTextMaxSize = 17;
            btText.horizontalOverflow = HorizontalWrapMode.Wrap;
            btText.verticalOverflow = VerticalWrapMode.Truncate;
            btText.fontStyle = FontStyle.Bold;
            btText.alignment = TextAnchor.MiddleCenter;
            btText.color = Color.white;

            // Kapat (X) Butonu (Üst Sağ)
            GameObject closeBtnObj = new GameObject("CloseButton_X");
            closeBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(265f, 125f);
            cRect.sizeDelta = new Vector2(40f, 40f);

            Image cBg = closeBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(40, 40, 20, new Color(0.92f, 0.18f, 0.20f, 1f));
            cBg.raycastTarget = true;

            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBg;
            cBtn.onClick.AddListener(() => {
                if (canvasObj != null) canvasObj.SetActive(false);
                if (currentGlobalPopupCanvas == canvasObj) currentGlobalPopupCanvas = null;
                SetModalOpen(false);
                if (canvasObj != null) Object.Destroy(canvasObj);
            });

            GameObject cxObj = new GameObject("X");
            cxObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cxRect = cxObj.AddComponent<RectTransform>();
            cxRect.anchorMin = Vector2.zero;
            cxRect.anchorMax = Vector2.one;

            Text cxText = cxObj.AddComponent<Text>();
            cxText.font = font;
            cxText.text = "✖";
            cxText.fontSize = 22;
            cxText.fontStyle = FontStyle.Bold;
            cxText.alignment = TextAnchor.MiddleCenter;
            cxText.color = Color.white;
            cxText.raycastTarget = false;

            closeBtnObj.transform.SetAsLastSibling();
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
            if (currentGlobalPopupCanvas != null)
            {
                Object.DestroyImmediate(currentGlobalPopupCanvas);
                currentGlobalPopupCanvas = null;
            }

            GameObject canvasObj = new GameObject("Global_Modal_Popup_Canvas");
            currentGlobalPopupCanvas = canvasObj;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

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
            boxRect.sizeDelta = new Vector2(660f, 350f);

            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.sprite = UIStyleUtility.CreateOutlinePillSprite(660, 350, 16, 2, new Color(0.95f, 0.40f, 0.55f), new Color(0.12f, 0.15f, 0.20f, 0.98f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Başlık
            GameObject titleObj = new GameObject("Modal_Title");
            titleObj.transform.SetParent(boxObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 125f);
            tRect.sizeDelta = new Vector2(580f, 40f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = title;
            tText.fontSize = 24;
            tText.resizeTextForBestFit = true;
            tText.resizeTextMinSize = 16;
            tText.resizeTextMaxSize = 26;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.95f, 0.45f, 0.60f);

            // Mesaj Metni
            GameObject msgObj = new GameObject("Modal_Message");
            msgObj.transform.SetParent(boxObj.transform, false);
            RectTransform mRect = msgObj.AddComponent<RectTransform>();
            mRect.anchoredPosition = new Vector2(0f, 20f);
            mRect.sizeDelta = new Vector2(580f, 150f);

            Text mText = msgObj.AddComponent<Text>();
            mText.font = font;
            mText.text = message;
            mText.fontSize = 18;
            mText.resizeTextForBestFit = true;
            mText.resizeTextMinSize = 12;
            mText.resizeTextMaxSize = 19;
            mText.alignment = TextAnchor.MiddleCenter;
            mText.color = Color.white;

            // Evet / Onay Butonu (Sağ Taraf)
            GameObject confirmBtnObj = new GameObject("Confirm_Button");
            confirmBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform confirmRect = confirmBtnObj.AddComponent<RectTransform>();
            confirmRect.anchoredPosition = new Vector2(150f, -115f);
            confirmRect.sizeDelta = new Vector2(260f, 48f);

            Image confirmBg = confirmBtnObj.AddComponent<Image>();
            confirmBg.sprite = UIStyleUtility.CreateRoundedPillSprite(260, 48, 12, new Color(0.20f, 0.75f, 0.45f));

            Button confirmBtn = confirmBtnObj.AddComponent<Button>();
            confirmBtn.targetGraphic = confirmBg;
            confirmBtn.onClick.AddListener(() => {
                if (currentGlobalPopupCanvas == canvasObj) currentGlobalPopupCanvas = null;
                SetModalOpen(false);
                Object.Destroy(canvasObj);
                onConfirm?.Invoke();
            });

            GameObject confirmTxtObj = new GameObject("Label");
            confirmTxtObj.transform.SetParent(confirmBtnObj.transform, false);
            RectTransform cLabelRect = confirmTxtObj.AddComponent<RectTransform>();
            cLabelRect.anchorMin = Vector2.zero;
            cLabelRect.anchorMax = Vector2.one;
            cLabelRect.offsetMin = new Vector2(10f, 2f);
            cLabelRect.offsetMax = new Vector2(-10f, -2f);

            Text confirmTxt = confirmTxtObj.AddComponent<Text>();
            confirmTxt.font = font;
            confirmTxt.text = confirmText;
            confirmTxt.fontSize = 16;
            confirmTxt.resizeTextForBestFit = true;
            confirmTxt.resizeTextMinSize = 10;
            confirmTxt.resizeTextMaxSize = 17;
            confirmTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            confirmTxt.verticalOverflow = VerticalWrapMode.Truncate;
            confirmTxt.fontStyle = FontStyle.Bold;
            confirmTxt.alignment = TextAnchor.MiddleCenter;
            confirmTxt.color = Color.white;

            // Hayır / İptal Butonu (Sol Taraf)
            GameObject cancelBtnObj = new GameObject("Cancel_Button");
            cancelBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform cancelRect = cancelBtnObj.AddComponent<RectTransform>();
            cancelRect.anchoredPosition = new Vector2(-150f, -115f);
            cancelRect.sizeDelta = new Vector2(260f, 48f);

            Image cancelBg = cancelBtnObj.AddComponent<Image>();
            cancelBg.sprite = UIStyleUtility.CreateRoundedPillSprite(260, 48, 12, new Color(0.40f, 0.45f, 0.55f));

            Button cancelBtn = cancelBtnObj.AddComponent<Button>();
            cancelBtn.targetGraphic = cancelBg;
            cancelBtn.onClick.AddListener(() => {
                if (currentGlobalPopupCanvas == canvasObj) currentGlobalPopupCanvas = null;
                SetModalOpen(false);
                Object.Destroy(canvasObj);
                onCancel?.Invoke();
            });

            GameObject cancelTxtObj = new GameObject("Label");
            cancelTxtObj.transform.SetParent(cancelBtnObj.transform, false);
            RectTransform cancelLabelRect = cancelTxtObj.AddComponent<RectTransform>();
            cancelLabelRect.anchorMin = Vector2.zero;
            cancelLabelRect.anchorMax = Vector2.one;
            cancelLabelRect.offsetMin = new Vector2(10f, 2f);
            cancelLabelRect.offsetMax = new Vector2(-10f, -2f);

            Text cancelTxt = cancelTxtObj.AddComponent<Text>();
            cancelTxt.font = font;
            cancelTxt.text = cancelText;
            cancelTxt.fontSize = 16;
            cancelTxt.resizeTextForBestFit = true;
            cancelTxt.resizeTextMinSize = 10;
            cancelTxt.resizeTextMaxSize = 17;
            cancelTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            cancelTxt.verticalOverflow = VerticalWrapMode.Truncate;
            cancelTxt.fontStyle = FontStyle.Bold;
            cancelTxt.alignment = TextAnchor.MiddleCenter;
            cancelTxt.color = Color.white;

            // Kapat (X) Butonu (Üst Sağ)
            GameObject closeBtnObj = new GameObject("CloseButton_X");
            closeBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(295f, 140f);
            cRect.sizeDelta = new Vector2(40f, 40f);

            Image cBg = closeBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(40, 40, 20, new Color(0.92f, 0.18f, 0.20f, 1f));
            cBg.raycastTarget = true;

            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBg;
            cBtn.onClick.AddListener(() => {
                if (currentGlobalPopupCanvas == canvasObj) currentGlobalPopupCanvas = null;
                SetModalOpen(false);
                Object.Destroy(canvasObj);
                onCancel?.Invoke();
            });

            GameObject cxObj = new GameObject("X");
            cxObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cxRect = cxObj.AddComponent<RectTransform>();
            cxRect.anchorMin = Vector2.zero;
            cxRect.anchorMax = Vector2.one;

            Text cxText = cxObj.AddComponent<Text>();
            cxText.font = font;
            cxText.text = "✖";
            cxText.fontSize = 22;
            cxText.fontStyle = FontStyle.Bold;
            cxText.alignment = TextAnchor.MiddleCenter;
            cxText.color = Color.white;
            cxText.raycastTarget = false;

            closeBtnObj.transform.SetAsLastSibling();
        }
    }
}
