using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Sağ-orta sürüklenebilir günlük çark ikonu ve 10 dilimli şans çarkı modalı.
    /// Mobil dokunmatik ve PC fare ile tam uyumludur.
    /// </summary>
    public class DailySpinWheelUI : MonoBehaviour
    {
        public static DailySpinWheelUI Instance { get; private set; }

        public bool IsWheelOpen => canvasObj != null && canvasObj.activeInHierarchy;

        private const float IconSize = 92f;
        private const float WheelSize = 460f;
        private const int SliceCount = 10;
        private const float SliceAngle = 360f / SliceCount;

        private static readonly Color[] SliceColors =
        {
            new Color(0.95f, 0.72f, 0.12f),
            new Color(0.92f, 0.32f, 0.28f),
            new Color(0.22f, 0.72f, 0.92f),
            new Color(0.38f, 0.82f, 0.42f),
            new Color(0.78f, 0.42f, 0.92f),
            new Color(0.95f, 0.55f, 0.18f),
            new Color(0.28f, 0.52f, 0.95f),
            new Color(0.95f, 0.38f, 0.62f),
            new Color(0.18f, 0.78f, 0.62f),
            new Color(0.62f, 0.68f, 0.78f)
        };

        private Canvas hudCanvas;
        private RectTransform iconRect;
        private Image iconBg;
        private Text iconBadgeText;
        private GameObject canvasObj;
        private RectTransform wheelRect;
        private Button spinButton;
        private Text spinButtonLabel;
        private Text statusText;
        private Text titleText;
        private GameObject resultPanel;
        private Text resultText;
        private bool isSpinning;
        private float midnightCheckTimer;

        public static void EnsureInstance()
        {
            if (Instance != null) return;

            GameObject uiManagerObj = GameObject.Find("UI_Manager");
            if (uiManagerObj == null)
            {
                uiManagerObj = new GameObject("UI_Manager");
            }

            if (uiManagerObj.GetComponent<DailySpinWheelUI>() == null)
            {
                uiManagerObj.AddComponent<DailySpinWheelUI>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            midnightCheckTimer += Time.unscaledDeltaTime;
            if (midnightCheckTimer < 15f) return;
            midnightCheckTimer = 0f;
            RefreshHudIcon();
            if (IsWheelOpen && !isSpinning)
            {
                RefreshModalState();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) RefreshHudIcon();
        }

        public void AttachToHud(Canvas canvas)
        {
            DailySpinWheelManager.EnsureInstance();
            hudCanvas = canvas;
            if (hudCanvas == null) return;

            if (iconRect != null && iconRect)
            {
                Destroy(iconRect.gameObject);
                iconRect = null;
            }

            CreateHudIcon(hudCanvas.transform);
            ApplySavedIconPosition();
            RefreshHudIcon();
        }

        public void SyncHudFromRuntime()
        {
            ApplySavedIconPosition();
            RefreshHudIcon();
        }

        public void RefreshHudIcon()
        {
            if (iconBg == null || iconBadgeText == null) return;
            DailySpinWheelManager.EnsureInstance();
            bool canSpin = DailySpinWheelManager.Instance.CanSpinToday();
            iconBg.color = canSpin ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.92f);
            iconBadgeText.gameObject.SetActive(!canSpin);
            iconBadgeText.text = "✓";
        }

        private void CreateHudIcon(Transform parent)
        {
            GameObject iconObj = new GameObject("Widget_DailySpinWheel");
            iconObj.transform.SetParent(parent, false);
            iconObj.transform.SetAsLastSibling();

            iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(1f, 0.5f);
            iconRect.anchorMax = new Vector2(1f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            iconRect.anchoredPosition = new Vector2(DailySpinWheelManager.DefaultIconPosX, DailySpinWheelManager.DefaultIconPosY);

            Image hitArea = iconObj.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;

            GameObject visualObj = new GameObject("WheelVisual");
            visualObj.transform.SetParent(iconObj.transform, false);
            RectTransform visualRect = visualObj.AddComponent<RectTransform>();
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.offsetMin = Vector2.zero;
            visualRect.offsetMax = Vector2.zero;

            iconBg = visualObj.AddComponent<Image>();
            iconBg.sprite = CreateMiniWheelSprite();
            iconBg.color = Color.white;
            iconBg.raycastTarget = false;
            iconBg.preserveAspect = true;

            Outline outline = visualObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.10f, 0.14f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            DailySpinIconDrag drag = iconObj.AddComponent<DailySpinIconDrag>();
            drag.Initialize(this, iconRect, hudCanvas);

            GameObject badgeObj = new GameObject("UsedBadge");
            badgeObj.transform.SetParent(iconObj.transform, false);
            RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(4f, 4f);
            badgeRect.sizeDelta = new Vector2(28f, 28f);

            Image badgeBg = badgeObj.AddComponent<Image>();
            badgeBg.sprite = UIStyleUtility.CreateRoundedPillSprite(28, 28, 14, new Color(0.18f, 0.72f, 0.38f, 0.96f));
            badgeBg.raycastTarget = false;

            GameObject badgeLabel = new GameObject("Label");
            badgeLabel.transform.SetParent(badgeObj.transform, false);
            RectTransform blRect = badgeLabel.AddComponent<RectTransform>();
            blRect.anchorMin = Vector2.zero;
            blRect.anchorMax = Vector2.one;
            blRect.offsetMin = Vector2.zero;
            blRect.offsetMax = Vector2.zero;

            iconBadgeText = badgeLabel.AddComponent<Text>();
            iconBadgeText.font = UIStyleUtility.GetGlobalFont(18);
            iconBadgeText.fontSize = 16;
            iconBadgeText.alignment = TextAnchor.MiddleCenter;
            iconBadgeText.color = Color.white;
            iconBadgeText.raycastTarget = false;
            iconBadgeText.text = "✓";
            badgeObj.SetActive(false);
        }

        private void ApplySavedIconPosition()
        {
            if (iconRect == null) return;
            DailySpinWheelManager.EnsureInstance();
            DailySpinWheelManager manager = DailySpinWheelManager.Instance;
            if (manager.IconPosSaved)
            {
                iconRect.anchoredPosition = new Vector2(manager.IconPosX, manager.IconPosY);
            }
            else
            {
                iconRect.anchoredPosition = new Vector2(DailySpinWheelManager.DefaultIconPosX, DailySpinWheelManager.DefaultIconPosY);
            }

            ClampIconToCanvas();
        }

        public void OnIconClicked()
        {
            if (MainMenuUI.IsMenuVisible) return;
            if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen) return;
            if (EndOfDayReportModalUI.IsReportModalOpen) return;
            if (IsWheelOpen) return;

            AudioManager.Instance?.PlayModalOpen();
            ShowWheelModal();
        }

        public void OnIconPositionChanged(Vector2 anchoredPosition)
        {
            DailySpinWheelManager.EnsureInstance();
            DailySpinWheelManager.Instance.SetIconPosition(anchoredPosition.x, anchoredPosition.y);
            DailySpinWheelManager.Instance.PersistNow();
        }

        public void ClampIconToCanvas()
        {
            if (iconRect == null || hudCanvas == null) return;
            RectTransform canvasRect = hudCanvas.transform as RectTransform;
            if (canvasRect == null) return;

            Vector2 canvasSize = canvasRect.rect.size;
            float halfW = iconRect.rect.width * 0.5f;
            float halfH = iconRect.rect.height * 0.5f;
            float pad = 18f;

            float minX = -canvasSize.x + halfW + pad;
            float maxX = -halfW - pad;
            float minY = -canvasSize.y * 0.5f + halfH + pad;
            float maxY = canvasSize.y * 0.5f - halfH - pad;

            Vector2 pos = iconRect.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            iconRect.anchoredPosition = pos;
        }

        private void ShowWheelModal()
        {
            HideWheelModal();
            DailySpinWheelManager.EnsureInstance();
            ModalManager.SetModalOpen(true);

            canvasObj = new GameObject("DailySpinWheel_Modal_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1080;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            Font font = UIStyleUtility.GetGlobalFont(22);

            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;
            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.88f);
            bdImg.raycastTarget = true;

            GameObject panelObj = new GameObject("Wheel_Panel");
            panelObj.transform.SetParent(backdrop.transform, false);
            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f);
            pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0.5f);
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(680f, 820f);

            Image pBg = panelObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(680, 820, 28, 3, new Color(0.95f, 0.72f, 0.18f), new Color(0.09f, 0.12f, 0.16f, 0.98f));
            pBg.raycastTarget = true;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 360f);
            tRect.sizeDelta = new Vector2(600f, 52f);
            titleText = titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 32;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.86f, 0.35f);
            titleText.raycastTarget = false;
            titleText.supportRichText = true;

            GameObject statusObj = new GameObject("Status");
            statusObj.transform.SetParent(panelObj.transform, false);
            RectTransform sRect = statusObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 312f);
            sRect.sizeDelta = new Vector2(600f, 36f);
            statusText = statusObj.AddComponent<Text>();
            statusText.font = font;
            statusText.fontSize = 18;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(0.82f, 0.88f, 0.94f);
            statusText.raycastTarget = false;

            CreateWheelVisual(panelObj.transform);

            GameObject pointer = new GameObject("Pointer");
            pointer.transform.SetParent(panelObj.transform, false);
            RectTransform ptrRect = pointer.AddComponent<RectTransform>();
            ptrRect.anchoredPosition = new Vector2(0f, 248f);
            ptrRect.sizeDelta = new Vector2(42f, 52f);
            Text pointerText = pointer.AddComponent<Text>();
            pointerText.font = font;
            pointerText.fontSize = 40;
            pointerText.alignment = TextAnchor.MiddleCenter;
            pointerText.color = new Color(1f, 0.92f, 0.35f);
            pointerText.text = "▼";
            pointerText.raycastTarget = false;

            GameObject spinObj = new GameObject("SpinButton");
            spinObj.transform.SetParent(panelObj.transform, false);
            RectTransform spinRect = spinObj.AddComponent<RectTransform>();
            spinRect.anchoredPosition = new Vector2(0f, -318f);
            spinRect.sizeDelta = new Vector2(320f, 64f);
            Image spinBg = spinObj.AddComponent<Image>();
            spinButton = spinObj.AddComponent<Button>();
            spinButton.targetGraphic = spinBg;
            spinButton.onClick.AddListener(OnSpinClicked);

            GameObject spinLabelObj = new GameObject("Label");
            spinLabelObj.transform.SetParent(spinObj.transform, false);
            RectTransform slRect = spinLabelObj.AddComponent<RectTransform>();
            slRect.anchorMin = Vector2.zero;
            slRect.anchorMax = Vector2.one;
            slRect.offsetMin = Vector2.zero;
            slRect.offsetMax = Vector2.zero;
            spinButtonLabel = spinLabelObj.AddComponent<Text>();
            spinButtonLabel.font = font;
            spinButtonLabel.fontSize = 24;
            spinButtonLabel.alignment = TextAnchor.MiddleCenter;
            spinButtonLabel.color = new Color(0.12f, 0.10f, 0.06f);
            spinButtonLabel.raycastTarget = false;
            spinButtonLabel.supportRichText = true;

            CreateCloseButton(panelObj.transform);
            CreateResultPanel(panelObj.transform);
            RefreshModalState();
        }

        private void CreateWheelVisual(Transform parent)
        {
            GameObject wheelHolder = new GameObject("WheelHolder");
            wheelHolder.transform.SetParent(parent, false);
            RectTransform holderRect = wheelHolder.AddComponent<RectTransform>();
            holderRect.anchoredPosition = new Vector2(0f, 20f);
            holderRect.sizeDelta = new Vector2(WheelSize, WheelSize);

            GameObject wheelObj = new GameObject("Wheel");
            wheelObj.transform.SetParent(wheelHolder.transform, false);
            wheelRect = wheelObj.AddComponent<RectTransform>();
            wheelRect.anchorMin = new Vector2(0.5f, 0.5f);
            wheelRect.anchorMax = new Vector2(0.5f, 0.5f);
            wheelRect.pivot = new Vector2(0.5f, 0.5f);
            wheelRect.sizeDelta = new Vector2(WheelSize, WheelSize);
            wheelRect.localEulerAngles = Vector3.zero;

            Image wheelImg = wheelObj.AddComponent<Image>();
            wheelImg.sprite = CreateFullWheelSprite();
            wheelImg.raycastTarget = false;

            Font font = UIStyleUtility.GetGlobalFont(16);
            float labelRadius = WheelSize * 0.32f;
            for (int i = 0; i < SliceCount; i++)
            {
                float centerDeg = (i + 0.5f) * SliceAngle;
                float rad = centerDeg * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Sin(rad) * labelRadius, Mathf.Cos(rad) * labelRadius);

                GameObject labelObj = new GameObject($"SliceLabel_{i}");
                labelObj.transform.SetParent(wheelObj.transform, false);
                RectTransform lRect = labelObj.AddComponent<RectTransform>();
                lRect.anchoredPosition = pos;
                lRect.sizeDelta = new Vector2(92f, 40f);
                lRect.localEulerAngles = new Vector3(0f, 0f, -centerDeg);

                Text label = labelObj.AddComponent<Text>();
                label.font = font;
                label.fontSize = 15;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
                label.supportRichText = true;
                int amount = DailySpinWheelManager.PrizeAmounts[i];
                label.text = $"<b>{amount:N0}C</b>";

                Shadow shadow = labelObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
                shadow.effectDistance = new Vector2(1f, -1f);
            }

            GameObject hub = new GameObject("Hub");
            hub.transform.SetParent(wheelHolder.transform, false);
            RectTransform hubRect = hub.AddComponent<RectTransform>();
            hubRect.sizeDelta = new Vector2(78f, 78f);
            Image hubImg = hub.AddComponent<Image>();
            hubImg.sprite = UIStyleUtility.CreateRoundedPillSprite(78, 78, 39, new Color(0.16f, 0.18f, 0.22f, 1f));
            hubImg.raycastTarget = false;

            GameObject hubLabelObj = new GameObject("HubLabel");
            hubLabelObj.transform.SetParent(hub.transform, false);
            RectTransform hlRect = hubLabelObj.AddComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            Text hubLabel = hubLabelObj.AddComponent<Text>();
            hubLabel.font = font;
            hubLabel.fontSize = 28;
            hubLabel.alignment = TextAnchor.MiddleCenter;
            hubLabel.color = new Color(1f, 0.84f, 0.28f);
            hubLabel.text = "🎡";
            hubLabel.raycastTarget = false;
        }

        private void CreateCloseButton(Transform parent)
        {
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(parent, false);
            RectTransform cRect = closeObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(1f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(1f, 1f);
            cRect.anchoredPosition = new Vector2(-18f, -18f);
            cRect.sizeDelta = new Vector2(52f, 52f);

            Image cBg = closeObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateOutlinePillSprite(52, 52, 16, 2, new Color(0.95f, 0.35f, 0.40f), new Color(0.22f, 0.10f, 0.12f, 0.96f));
            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.targetGraphic = cBg;
            closeBtn.onClick.AddListener(OnCloseClicked);

            GameObject cLabelObj = new GameObject("Label");
            cLabelObj.transform.SetParent(closeObj.transform, false);
            RectTransform clRect = cLabelObj.AddComponent<RectTransform>();
            clRect.anchorMin = Vector2.zero;
            clRect.anchorMax = Vector2.one;
            Text cLabel = cLabelObj.AddComponent<Text>();
            cLabel.font = UIStyleUtility.GetGlobalFont(22);
            cLabel.fontSize = 26;
            cLabel.alignment = TextAnchor.MiddleCenter;
            cLabel.color = Color.white;
            cLabel.text = "✕";
            cLabel.raycastTarget = false;
        }

        private void CreateResultPanel(Transform parent)
        {
            resultPanel = new GameObject("ResultPanel");
            resultPanel.transform.SetParent(parent, false);
            RectTransform rRect = resultPanel.AddComponent<RectTransform>();
            rRect.anchoredPosition = new Vector2(0f, 20f);
            rRect.sizeDelta = new Vector2(420f, 180f);

            Image rBg = resultPanel.AddComponent<Image>();
            rBg.sprite = UIStyleUtility.CreateOutlinePillSprite(420, 180, 18, 2, new Color(0.95f, 0.78f, 0.20f), new Color(0.08f, 0.10f, 0.14f, 0.96f));
            rBg.raycastTarget = true;

            GameObject rtObj = new GameObject("ResultText");
            rtObj.transform.SetParent(resultPanel.transform, false);
            RectTransform rtRect = rtObj.AddComponent<RectTransform>();
            rtRect.anchoredPosition = new Vector2(0f, 28f);
            rtRect.sizeDelta = new Vector2(380f, 90f);
            resultText = rtObj.AddComponent<Text>();
            resultText.font = UIStyleUtility.GetGlobalFont(22);
            resultText.fontSize = 22;
            resultText.alignment = TextAnchor.MiddleCenter;
            resultText.color = new Color(1f, 0.92f, 0.55f);
            resultText.supportRichText = true;
            resultText.raycastTarget = false;

            GameObject okObj = new GameObject("OkButton");
            okObj.transform.SetParent(resultPanel.transform, false);
            RectTransform okRect = okObj.AddComponent<RectTransform>();
            okRect.anchoredPosition = new Vector2(0f, -52f);
            okRect.sizeDelta = new Vector2(180f, 46f);
            Image okBg = okObj.AddComponent<Image>();
            okBg.sprite = UIStyleUtility.CreateOutlinePillSprite(180, 46, 16, 2, new Color(0.30f, 0.82f, 0.45f), new Color(0.10f, 0.22f, 0.14f, 0.96f));
            Button okBtn = okObj.AddComponent<Button>();
            okBtn.targetGraphic = okBg;
            okBtn.onClick.AddListener(OnCloseClicked);

            GameObject okLabelObj = new GameObject("Label");
            okLabelObj.transform.SetParent(okObj.transform, false);
            RectTransform olRect = okLabelObj.AddComponent<RectTransform>();
            olRect.anchorMin = Vector2.zero;
            olRect.anchorMax = Vector2.one;
            Text okLabel = okLabelObj.AddComponent<Text>();
            okLabel.font = UIStyleUtility.GetGlobalFont(20);
            okLabel.fontSize = 20;
            okLabel.alignment = TextAnchor.MiddleCenter;
            okLabel.color = Color.white;
            okLabel.text = LocalizationManager.L("Btn_OK", "Tamam", "OK");
            okLabel.raycastTarget = false;

            resultPanel.SetActive(false);
        }

        private void RefreshModalState()
        {
            if (spinButton == null || spinButtonLabel == null || statusText == null) return;

            bool canSpin = DailySpinWheelManager.Instance != null && DailySpinWheelManager.Instance.CanSpinToday();
            titleText.text = LocalizationManager.L("DailySpin_Title", "<b>GÜNLÜK ŞANS ÇARKI</b>", "<b>DAILY FORTUNE WHEEL</b>");

            Image spinBg = spinButton.targetGraphic as Image;
            if (canSpin && !isSpinning)
            {
                statusText.text = LocalizationManager.L("DailySpin_Ready", "Bugün 1 çevirme hakkın var. Ödül anında kasana eklenir.", "You have 1 spin today. The prize is added to your cash instantly.");
                spinButton.interactable = true;
                spinButtonLabel.text = LocalizationManager.L("DailySpin_Spin", "<b>ÇEVİR</b>", "<b>SPIN</b>");
                if (spinBg != null)
                {
                    spinBg.sprite = UIStyleUtility.CreateOutlinePillSprite(320, 64, 22, 2, new Color(1f, 0.84f, 0.22f), new Color(0.95f, 0.78f, 0.18f, 1f));
                }
            }
            else if (isSpinning)
            {
                statusText.text = LocalizationManager.L("DailySpin_Spinning", "Çark dönüyor...", "The wheel is spinning...");
                spinButton.interactable = false;
                spinButtonLabel.text = LocalizationManager.L("DailySpin_Wait", "<b>...</b>", "<b>...</b>");
            }
            else
            {
                statusText.text = LocalizationManager.L("DailySpin_Used", "Bugünkü hakkını kullandın. Yarın tekrar gel!", "You already spun today. Come back tomorrow!");
                spinButton.interactable = false;
                spinButtonLabel.text = LocalizationManager.L("DailySpin_Tomorrow", "<b>YARIN</b>", "<b>TOMORROW</b>");
                if (spinBg != null)
                {
                    spinBg.sprite = UIStyleUtility.CreateOutlinePillSprite(320, 64, 22, 2, new Color(0.45f, 0.50f, 0.58f), new Color(0.28f, 0.32f, 0.38f, 0.95f));
                }
            }
        }

        private void OnSpinClicked()
        {
            if (isSpinning) return;
            DailySpinWheelManager.EnsureInstance();
            if (!DailySpinWheelManager.Instance.CanSpinToday())
            {
                RefreshModalState();
                return;
            }

            AudioManager.Instance?.PlayButtonClick();
            if (!DailySpinWheelManager.Instance.TryConsumeSpin(out int prizeIndex, out int prizeAmount))
            {
                RefreshModalState();
                return;
            }

            RefreshHudIcon();
            isSpinning = true;
            RefreshModalState();
            StartCoroutine(SpinAnimationRoutine(prizeIndex, prizeAmount));
        }

        private IEnumerator SpinAnimationRoutine(int prizeIndex, int prizeAmount)
        {
            if (wheelRect == null)
            {
                ShowResult(prizeAmount);
                yield break;
            }

            float extraSpins = UnityEngine.Random.Range(4, 7);
            float sliceJitter = UnityEngine.Random.Range(-10f, 10f);
            // Dilimler saat yönünde 12'den başlar. Unity Z+ saat yönünün tersidir;
            // kazanan dilimi üstteki oka getirmek için Z pozitif (CCW) döndürülür.
            float targetZ = extraSpins * 360f + (prizeIndex + 0.5f) * SliceAngle + sliceJitter;
            float startZ = Normalize180(wheelRect.localEulerAngles.z);
            while (targetZ < startZ + 360f)
            {
                targetZ += 360f;
            }

            float duration = UnityEngine.Random.Range(3.2f, 4.4f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float z = Mathf.Lerp(startZ, targetZ, eased);
                wheelRect.localEulerAngles = new Vector3(0f, 0f, z);
                yield return null;
            }

            wheelRect.localEulerAngles = new Vector3(0f, 0f, targetZ);
            yield return new WaitForSecondsRealtime(0.35f);
            ShowResult(prizeAmount);
        }

        private void ShowResult(int prizeAmount)
        {
            isSpinning = false;
            RefreshModalState();
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
                resultText.text = LocalizationManager.L(
                    "DailySpin_Won",
                    $"<b>Tebrikler!</b>\nKasana <b>{prizeAmount:N0}C</b> eklendi.",
                    $"<b>Congratulations!</b>\n<b>{prizeAmount:N0}C</b> added to your cash.");
            }
        }

        private void OnCloseClicked()
        {
            AudioManager.Instance?.PlayModalClose();
            HideWheelModal();
        }

        public void HideWheelModal()
        {
            StopAllCoroutines();
            isSpinning = false;
            if (canvasObj != null)
            {
                canvasObj.SetActive(false);
                Destroy(canvasObj);
                canvasObj = null;
            }

            wheelRect = null;
            spinButton = null;
            spinButtonLabel = null;
            statusText = null;
            titleText = null;
            resultPanel = null;
            resultText = null;
            ModalManager.SetModalOpen(false);
            RefreshHudIcon();
        }

        private static float Normalize180(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Sprite CreateMiniWheelSprite()
        {
            return BuildWheelTexture(128, true);
        }

        private static Sprite CreateFullWheelSprite()
        {
            return BuildWheelTexture(512, false);
        }

        private static Sprite BuildWheelTexture(int size, bool thickRim)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float outerR = size * 0.49f;
            float innerR = size * (thickRim ? 0.16f : 0.07f);
            float rim = thickRim ? size * 0.06f : size * 0.018f;
            float dividerWidth = thickRim ? 0.035f : 0.012f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    int idx = y * size + x;
                    if (dist > outerR)
                    {
                        pixels[idx] = Color.clear;
                        continue;
                    }

                    if (dist > outerR - rim)
                    {
                        pixels[idx] = new Color(1f, 0.86f, 0.28f, 1f);
                        continue;
                    }

                    if (dist < innerR)
                    {
                        pixels[idx] = new Color(0.16f, 0.18f, 0.22f, 1f);
                        continue;
                    }

                    float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;
                    int slice = Mathf.Clamp(Mathf.FloorToInt(angle / SliceAngle), 0, SliceCount - 1);
                    float inSlice = (angle % SliceAngle) / SliceAngle;
                    Color c = SliceColors[slice];
                    if (inSlice < dividerWidth || inSlice > 1f - dividerWidth)
                    {
                        c = new Color(1f, 0.95f, 0.80f, 1f);
                    }

                    pixels[idx] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private class DailySpinIconDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private const float DragThresholdPixels = 18f;

            private DailySpinWheelUI owner;
            private RectTransform rect;
            private Canvas canvas;
            private bool dragged;
            private bool pointerHeld;
            private Vector2 pressScreenPos;

            public void Initialize(DailySpinWheelUI ui, RectTransform target, Canvas hostCanvas)
            {
                owner = ui;
                rect = target;
                canvas = hostCanvas;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                pointerHeld = true;
                dragged = false;
                pressScreenPos = eventData.position;
                if (rect != null)
                {
                    rect.SetAsLastSibling();
                }
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                pressScreenPos = eventData.position;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (rect == null || canvas == null) return;

                if (!dragged && Vector2.Distance(eventData.position, pressScreenPos) >= DragThresholdPixels)
                {
                    dragged = true;
                }

                if (!dragged) return;

                float scale = canvas.scaleFactor <= 0.01f ? 1f : canvas.scaleFactor;
                rect.anchoredPosition += eventData.delta / scale;
                owner?.ClampIconToCanvas();
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                owner?.ClampIconToCanvas();
                if (dragged && rect != null)
                {
                    owner?.OnIconPositionChanged(rect.anchoredPosition);
                    TouchInputHelper.SuppressNextTap();
                }
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                bool wasHeld = pointerHeld;
                pointerHeld = false;
                if (!wasHeld || dragged) return;
                if (eventData.dragging && Vector2.Distance(eventData.position, pressScreenPos) >= DragThresholdPixels)
                {
                    return;
                }

                owner?.OnIconClicked();
            }
        }
    }
}
