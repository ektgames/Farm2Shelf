using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Mobil uyumlu, Notch/Çentik paylı, kavisli yuvarlak ve tam şeffaf sol üst HUD Arayüz Yöneticisi.
    /// Dükkan Aç/Kapat Butonu, 06:00 Saati, Tıklanabilir Stardew Valley Takvimi ve Credit Göstergesi.
    /// Türkçe ve İngilizce çift dilli desteklenir.
    /// </summary>
    public class GameHUDManager : MonoBehaviour
    {
        public static GameHUDManager Instance { get; private set; }

        private Canvas mainCanvas;
        private Image storeButtonBg;
        private Text storeButtonText;
        private Text clockText;
        private Text calendarText;
        private Text weatherText;
        private Text creditsText;
        private Text qualityText;
        private Text pauseButtonText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
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
            RefreshAllDisplays();
        }

        private void Start()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }

            CreateHUDCanvas();
            SubscribeToEvents();
            RefreshAllDisplays();

            // Eğer Ana Menü açıksa veya İntro oynatılıyorsa oyun içi HUD'ı varsayılan olarak gizle
            if (MainMenuUI.Instance != null || (EKTReklamIntroManager.Instance != null && !EKTReklamIntroManager.HasIntroFinished))
            {
                SetHUDVisible(false);
            }
        }

        public void SetHUDVisible(bool visible)
        {
            if (mainCanvas != null)
            {
                mainCanvas.gameObject.SetActive(visible);
            }
        }

        private void CreateHUDCanvas()
        {
            // Eski HUD Canvas varsa temizle
            GameObject existingCanvas = GameObject.Find("Farm2Shelf_HUD_Canvas");
            if (existingCanvas != null) DestroyImmediate(existingCanvas);

            // 1. Canvas Oluşturma
            GameObject canvasObj = new GameObject("Farm2Shelf_HUD_Canvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem & InputModule
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            // Ana Kameraya PhysicsRaycaster
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() == null)
            {
                mainCam.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            }

            // Calendar Popup UI Ekleme
            if (gameObject.GetComponent<CalendarPopupUI>() == null)
            {
                gameObject.AddComponent<CalendarPopupUI>();
            }

            // EKT Phone Tablet Manager Ekleme
            if (gameObject.GetComponent<EKTPhoneManager>() == null)
            {
                gameObject.AddComponent<EKTPhoneManager>();
            }

            CreateLowStockWarningPanel(canvasObj);

            // 2. SOL ÜST ŞEFFAF HUD CONTAINER (NOTCH PAYLI: X = 75, Y = -40)
            GameObject topPanel = new GameObject("TopLeft_HUD_Panel");
            topPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = topPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(75f, -40f); // Telefon kavis/çentik payı!
            panelRect.sizeDelta = new Vector3(1250f, 65f);

            // Şeffaf Arka Plan (Koyu Kutu Yok!)
            Image panelBg = topPanel.AddComponent<Image>();
            panelBg.color = Color.clear;
            panelBg.raycastTarget = false;

            HorizontalLayoutGroup layout = topPanel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;

            // --- 3. DÜKKAN AÇ / KAPAT KAVİSLİ YUVARLAK ŞEFFAF BUTON ---
            CreateStoreToggleButton(topPanel.transform);

            // --- 4. OYUN SAATİ ŞEFFAF ROZET ---
            CreateClockWidget(topPanel.transform);

            // --- 5. TIKLANABİLİR STARDEW VALLEY TAKVİM ROZETİ ---
            CreateCalendarWidget(topPanel.transform);

            // --- 5.5 DİNAMİK HAVA DURUMU ROZETİ ---
            CreateWeatherWidget(topPanel.transform);

            // --- 6. CREDIT PARA ŞEFFAF ROZET ---
            CreateCreditsWidget(topPanel.transform);

            // --- 7. MAĞAZA KALİTE SEVİYESİ YILDIZLI ROZET ---
            CreateQualityWidget(topPanel.transform);

            // --- 8. SAĞ ÜST PAUSE DURAKLATMA BUTONU ---
            CreatePauseButtonWidget(canvasObj.transform);

            // --- 8. SAĞ ALT EKT PHONE TABLET BUTONU ---
            if (GetComponent<EKTPhoneManager>() != null)
            {
                GetComponent<EKTPhoneManager>().CreateBottomRightPhoneButtonOnCanvas(canvasObj.transform);
            }
            else if (EKTPhoneManager.Instance != null)
            {
                EKTPhoneManager.Instance.CreateBottomRightPhoneButtonOnCanvas(canvasObj.transform);
            }
        }

        private void CreatePauseButtonWidget(Transform parent)
        {
            GameObject widget = new GameObject("Widget_PauseButton");
            widget.transform.SetParent(parent, false);

            RectTransform rect = widget.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-40f, -40f);
            rect.sizeDelta = new Vector2(150f, 52f);

            // Low-Poly Çerçeveli Cam Tasarım Arka Plan
            Image bg = widget.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateOutlinePillSprite(150, 52, 26, 2, new Color(0.95f, 0.70f, 0.20f, 0.95f), new Color(0.10f, 0.14f, 0.20f, 0.96f));

            Button btn = widget.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(OnPauseButtonClicked);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(widget.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            pauseButtonText = textObj.AddComponent<Text>();
            pauseButtonText.font = storeButtonText != null ? storeButtonText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pauseButtonText.text = LocalizationManager.L("HUD_Pause", "❚❚ <b>DURAKLAT</b>", "❚❚ <b>PAUSE</b>");
            pauseButtonText.fontSize = 18;
            pauseButtonText.alignment = TextAnchor.MiddleCenter;
            pauseButtonText.color = new Color(0.95f, 0.75f, 0.25f);
            pauseButtonText.raycastTarget = false;
        }

        private void OnPauseButtonClicked()
        {
            if (PauseMenuUI.Instance == null)
            {
                GameObject uiObj = GameObject.Find("UI_Manager") ?? new GameObject("UI_Manager");
                if (uiObj.GetComponent<PauseMenuUI>() == null)
                    uiObj.AddComponent<PauseMenuUI>();
            }

            if (PauseMenuUI.Instance != null)
            {
                PauseMenuUI.Instance.ShowPauseMenu();
            }
        }

        private bool isWaitingForEvacuation = false;
        public bool IsWaitingForEvacuation => isWaitingForEvacuation;
        public void SetWaitingForEvacuation(bool waiting) => isWaitingForEvacuation = waiting;

        private void CreateStoreToggleButton(Transform parent)
        {
            GameObject btnObj = new GameObject("Store_Toggle_Button");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(190f, 48f);

            storeButtonBg = btnObj.AddComponent<Image>();
            storeButtonBg.sprite = UIStyleUtility.CreateOutlinePillSprite(190, 48, 24, 2, new Color(0.95f, 0.25f, 0.25f), new Color(0.12f, 0.14f, 0.18f, 0.75f));

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = storeButtonBg;
            btn.onClick.AddListener(OnStoreButtonClicked);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 2f);
            textRect.offsetMax = new Vector2(-12f, -2f);

            storeButtonText = textObj.AddComponent<Text>();
            storeButtonText.font = UIStyleUtility.GetGlobalFont(22);
            storeButtonText.text = LocalizationManager.L("Store_Closed_Upper", "DÜKKAN KAPALI", "STORE CLOSED");
            storeButtonText.fontSize = 15;
            storeButtonText.resizeTextForBestFit = true;
            storeButtonText.resizeTextMinSize = 10;
            storeButtonText.resizeTextMaxSize = 16;
            storeButtonText.fontStyle = FontStyle.Bold;
            storeButtonText.alignment = TextAnchor.MiddleCenter;
            storeButtonText.color = new Color(1.0f, 0.35f, 0.35f);
            storeButtonText.raycastTarget = false;
        }

        private void CreateClockWidget(Transform parent)
        {
            GameObject widget = new GameObject("Widget_Clock");
            widget.transform.SetParent(parent, false);

            RectTransform rect = widget.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(145f, 52f);

            Image bg = widget.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(145, 52, 25, new Color(0.12f, 0.15f, 0.18f, 0.65f));
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("ClockText");
            textObj.transform.SetParent(widget.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            clockText = textObj.AddComponent<Text>();
            clockText.font = storeButtonText.font;
            clockText.text = "⏰ 06:00";
            clockText.fontSize = 22;
            clockText.resizeTextForBestFit = true;
            clockText.resizeTextMinSize = 13;
            clockText.resizeTextMaxSize = 24;
            clockText.fontStyle = FontStyle.Bold;
            clockText.alignment = TextAnchor.MiddleCenter;
            clockText.color = new Color(1.0f, 0.90f, 0.30f);
            clockText.raycastTarget = false;
        }

        private void CreateCalendarWidget(Transform parent)
        {
            GameObject widget = new GameObject("Widget_Calendar");
            widget.transform.SetParent(parent, false);

            RectTransform rect = widget.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250f, 52f);

            Image bg = widget.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateOutlinePillSprite(250, 52, 25, 2, new Color(0.30f, 0.85f, 0.45f, 0.8f), new Color(0.12f, 0.15f, 0.18f, 0.65f));

            Button btn = widget.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(OnCalendarWidgetClicked);

            GameObject textObj = new GameObject("CalendarText");
            textObj.transform.SetParent(widget.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            calendarText = textObj.AddComponent<Text>();
            calendarText.font = storeButtonText.font;
            calendarText.text = LocalizationManager.L("Calendar_Default", "📅 İLKBAHAR • GÜN 1", "📅 SPRING • DAY 1");
            calendarText.fontSize = 19;
            calendarText.resizeTextForBestFit = true;
            calendarText.resizeTextMinSize = 12;
            calendarText.resizeTextMaxSize = 21;
            calendarText.fontStyle = FontStyle.Bold;
            calendarText.alignment = TextAnchor.MiddleCenter;
            calendarText.color = new Color(0.35f, 0.95f, 0.55f);
            calendarText.raycastTarget = false;
        }

        private void CreateWeatherWidget(Transform parent)
        {
            GameObject widget = new GameObject("Widget_Weather");
            widget.transform.SetParent(parent, false);

            RectTransform rect = widget.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150f, 52f);

            Image bg = widget.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(150, 52, 25, new Color(0.12f, 0.18f, 0.25f, 0.70f));
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("WeatherText");
            textObj.transform.SetParent(widget.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            weatherText = textObj.AddComponent<Text>();
            weatherText.font = storeButtonText.font;
            weatherText.text = LocalizationManager.L("Weather_Sunny", "☀️ GÜNEŞLİ", "☀️ SUNNY");
            weatherText.fontSize = 18;
            weatherText.resizeTextForBestFit = true;
            weatherText.resizeTextMinSize = 11;
            weatherText.resizeTextMaxSize = 20;
            weatherText.fontStyle = FontStyle.Bold;
            weatherText.alignment = TextAnchor.MiddleCenter;
            weatherText.color = new Color(1.0f, 0.90f, 0.35f);
            weatherText.raycastTarget = false;
        }

        private void CreateCreditsWidget(Transform parent)
        {
            GameObject widget = new GameObject("Widget_Credits");
            widget.transform.SetParent(parent, false);

            RectTransform rect = widget.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(195f, 52f);

            Image bg = widget.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(195, 52, 25, new Color(0.12f, 0.15f, 0.18f, 0.65f));
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("CreditsText");
            textObj.transform.SetParent(widget.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            creditsText = textObj.AddComponent<Text>();
            creditsText.font = storeButtonText.font;
            creditsText.text = "💳 500.000C";
            creditsText.fontSize = 20;
            creditsText.resizeTextForBestFit = true;
            creditsText.resizeTextMinSize = 13;
            creditsText.resizeTextMaxSize = 22;
            creditsText.fontStyle = FontStyle.Bold;
            creditsText.alignment = TextAnchor.MiddleCenter;
            creditsText.color = new Color(0.30f, 0.88f, 1.0f);
            creditsText.raycastTarget = false;
        }

        private void CreateQualityWidget(Transform parent)
        {
            GameObject widget = new GameObject("Widget_Quality");
            widget.transform.SetParent(parent, false);

            RectTransform rect = widget.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(235f, 52f);

            Image bg = widget.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(235, 52, 25, new Color(0.18f, 0.15f, 0.08f, 0.75f));
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("QualityText");
            textObj.transform.SetParent(widget.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(12f, 0f);
            tRect.offsetMax = new Vector2(-12f, 0f);

            qualityText = textObj.AddComponent<Text>();
            qualityText.font = storeButtonText.font;
            string initQWord = LocalizationManager.L("Quality_UpperWord", "KALİTE", "QUALITY");
            qualityText.text = $"⭐ {initQWord}: Lv.0 (0P)";
            qualityText.fontSize = 19;
            qualityText.resizeTextForBestFit = true;
            qualityText.resizeTextMinSize = 11;
            qualityText.resizeTextMaxSize = 20;
            qualityText.fontStyle = FontStyle.Bold;
            qualityText.alignment = TextAnchor.MiddleCenter;
            qualityText.color = new Color(1.0f, 0.88f, 0.20f);
            qualityText.raycastTarget = false;
        }

        private void OnStoreButtonClicked()
        {
            if (ModalManager.IsModalOpen) return;

            if (StoreStatusManager.Instance != null)
            {
                bool willBeOpen = !StoreStatusManager.Instance.IsOpen;
                StoreStatusManager.Instance.ToggleStoreStatus();

                if (willBeOpen && TimeManager.Instance != null)
                {
                    TimeManager.Instance.StartDayTimeFlow();
                }
            }
        }

        private void OnCalendarWidgetClicked()
        {
            if (ModalManager.IsModalOpen) return;

            if (CalendarPopupUI.Instance != null)
            {
                CalendarPopupUI.Instance.OpenCalendar();
            }
        }

        private void SubscribeToEvents()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated += HandleTimeUpdated;
                TimeManager.Instance.OnDateUpdated += HandleDateUpdated;
                TimeManager.Instance.OnMidnightRollover += HandleMidnightRollover;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCreditsChanged += HandleCreditsChanged;
            }

            if (WeatherManager.Instance != null)
            {
                WeatherManager.Instance.OnWeatherChanged += HandleWeatherChanged;
            }

            if (StoreQualityManager.Instance != null)
            {
                StoreQualityManager.Instance.OnQualityChanged += HandleQualityChanged;
            }

            if (StoreStatusManager.Instance != null)
            {
                StoreStatusManager.Instance.OnStoreStatusChanged += HandleStoreStatusChanged;
            }
        }

        private void HandleMidnightRollover()
        {
            isWaitingForEvacuation = true;
            ModalManager.ShowModal(
                LocalizationManager.L("Midnight_Title", "🌙 Gece 12:00 (Günün Sonu)", "🌙 Midnight 12:00 (End of Day)"),
                LocalizationManager.L("Midnight_Body", "Saat 24:00 (12:00 AM) oldu! Dükkan otomatik kapatıldı.\n\nİçerideki müşteriler alışverişini bitirip çıktıktan sonra Gün Sonu Z Raporu otomatik açılacaktır.", "It's 12:00 AM! Store has automatically closed.\n\nOnce remaining customers leave, the End of Day Z-Report will open automatically."),
                LocalizationManager.L("Btn_OK", "Tamam", "OK")
            );
        }

        private void HandleTimeUpdated(int hour, int minute)
        {
            if (clockText != null)
            {
                clockText.text = $"⏰ {hour:D2}:{minute:D2}";
            }
        }

        private void HandleDateUpdated(TimeManager.Season season, int day, int year)
        {
            if (calendarText != null)
            {
                string seasonName = "";
                switch (season)
                {
                    case TimeManager.Season.İlkbahar: seasonName = LocalizationManager.L("Season_Spring", "İLKBAHAR", "SPRING"); break;
                    case TimeManager.Season.Yaz: seasonName = LocalizationManager.L("Season_Summer", "YAZ", "SUMMER"); break;
                    case TimeManager.Season.Sonbahar: seasonName = LocalizationManager.L("Season_Autumn", "SONBAHAR", "AUTUMN"); break;
                    case TimeManager.Season.Kış: seasonName = LocalizationManager.L("Season_Winter", "KIŞ", "WINTER"); break;
                }
                string dayWord = LocalizationManager.L("Day_UpperWord", "GÜN", "DAY");
                calendarText.text = $"📅 {seasonName} • {dayWord} {day}";
            }
        }

        private void HandleCreditsChanged(int currentCredits)
        {
            if (creditsText != null)
            {
                creditsText.text = $"💳 {currentCredits:N0}C";
            }
        }

        private void HandleWeatherChanged(WeatherType weather)
        {
            if (weatherText != null)
            {
                switch (weather)
                {
                    case WeatherType.Sunny:
                        weatherText.text = LocalizationManager.L("Weather_Sunny", "☀️ GÜNEŞLİ", "☀️ SUNNY");
                        weatherText.color = new Color(1.0f, 0.90f, 0.35f);
                        break;
                    case WeatherType.Rainy:
                        weatherText.text = LocalizationManager.L("Weather_Rainy", "🌧️ YAĞMURLU", "🌧️ RAINY");
                        weatherText.color = new Color(0.45f, 0.85f, 1.0f);
                        break;
                    case WeatherType.Snowy:
                        weatherText.text = LocalizationManager.L("Weather_Snowy", "❄️ KARLI", "❄️ SNOWY");
                        weatherText.color = new Color(0.92f, 0.96f, 1.0f);
                        break;
                }
            }
        }

        private void HandleQualityChanged(int score, int level)
        {
            if (qualityText != null)
            {
                string qWord = LocalizationManager.L("Quality_UpperWord", "KALİTE", "QUALITY");
                qualityText.text = $"⭐ {qWord}: Lv.{level} ({score}P)";
            }
        }

        private void HandleStoreStatusChanged(bool isOpen)
        {
            if (storeButtonBg != null && storeButtonText != null)
            {
                if (isOpen)
                {
                    storeButtonBg.sprite = UIStyleUtility.CreateOutlinePillSprite(190, 48, 24, 2, new Color(0.20f, 0.85f, 0.45f), new Color(0.12f, 0.14f, 0.18f, 0.75f));
                    storeButtonText.color = new Color(0.30f, 0.95f, 0.50f);
                    storeButtonText.text = LocalizationManager.L("Store_Open_Upper", "DÜKKAN AÇIK", "STORE OPEN");
                }
                else
                {
                    storeButtonBg.sprite = UIStyleUtility.CreateOutlinePillSprite(190, 48, 24, 2, new Color(0.95f, 0.25f, 0.25f), new Color(0.12f, 0.14f, 0.18f, 0.75f));
                    storeButtonText.color = new Color(1.0f, 0.35f, 0.35f);
                    storeButtonText.text = LocalizationManager.L("Store_Closed_Upper", "DÜKKAN KAPALI", "STORE CLOSED");
                }
            }
        }

        private void RefreshAllDisplays()
        {
            if (TimeManager.Instance != null)
            {
                HandleTimeUpdated(TimeManager.Instance.Hour, TimeManager.Instance.Minute);
                HandleDateUpdated(TimeManager.Instance.CurrentSeason, TimeManager.Instance.Day, TimeManager.Instance.Year);
            }
            else if (calendarText != null)
            {
                calendarText.text = LocalizationManager.L("Calendar_Default", "📅 İLKBAHAR • GÜN 1", "📅 SPRING • DAY 1");
            }

            if (EconomyManager.Instance != null)
            {
                HandleCreditsChanged(EconomyManager.Instance.Credits);
            }

            if (WeatherManager.Instance != null)
            {
                HandleWeatherChanged(WeatherManager.Instance.CurrentWeather);
            }

            if (StoreQualityManager.Instance != null)
            {
                HandleQualityChanged(StoreQualityManager.Instance.QualityScore, StoreQualityManager.Instance.QualityLevel);
            }

            if (StoreStatusManager.Instance != null)
            {
                HandleStoreStatusChanged(StoreStatusManager.Instance.IsOpen);
            }
            else if (storeButtonText != null)
            {
                storeButtonText.text = LocalizationManager.L("Store_Closed_Upper", "DÜKKAN KAPALI", "STORE CLOSED");
            }

            if (pauseButtonText != null)
            {
                pauseButtonText.text = LocalizationManager.L("HUD_Pause", "❚❚ <b>DURAKLAT</b>", "❚❚ <b>PAUSE</b>");
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
                TimeManager.Instance.OnDateUpdated -= HandleDateUpdated;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCreditsChanged -= HandleCreditsChanged;
            }

            if (WeatherManager.Instance != null)
            {
                WeatherManager.Instance.OnWeatherChanged -= HandleWeatherChanged;
            }

            if (StoreQualityManager.Instance != null)
            {
                StoreQualityManager.Instance.OnQualityChanged -= HandleQualityChanged;
            }

            if (StoreStatusManager.Instance != null)
            {
                StoreStatusManager.Instance.OnStoreStatusChanged -= HandleStoreStatusChanged;
            }
        }

        private GameObject lowStockPanelRoot;
        private Text lowStockListText;
        private float nextStockScanTime = 0f;

        private Font GetGlobalFont()
        {
            return UIStyleUtility.GetGlobalFont(16);
        }

        private void CreateLowStockWarningPanel(GameObject canvasObj)
        {
            lowStockPanelRoot = new GameObject("HUD_LowStockWarning_TextOnly");
            lowStockPanelRoot.transform.SetParent(canvasObj.transform, false);

            RectTransform pRect = lowStockPanelRoot.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.0f, 0.25f);
            pRect.anchorMax = new Vector2(0.0f, 0.75f);
            pRect.pivot = new Vector2(0.0f, 0.5f);
            pRect.anchoredPosition = new Vector2(115f, 0f);
            pRect.sizeDelta = new Vector2(460f, 0f);

            lowStockListText = lowStockPanelRoot.AddComponent<Text>();
            lowStockListText.font = GetGlobalFont();
            lowStockListText.fontSize = 16;
            lowStockListText.fontStyle = FontStyle.Bold;
            lowStockListText.alignment = TextAnchor.MiddleLeft;
            lowStockListText.color = new Color(1.0f, 0.30f, 0.25f);
            lowStockListText.lineSpacing = 1.25f;
            lowStockListText.raycastTarget = false;

            Shadow shadow = lowStockPanelRoot.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            lowStockPanelRoot.SetActive(false);
        }

        private void Update()
        {
            if (Time.time >= nextStockScanTime)
            {
                nextStockScanTime = Time.time + 1.0f;
                ScanLowStockItems();
            }

            if (isWaitingForEvacuation)
            {
                bool isStoreClosed = (StoreStatusManager.Instance != null && !StoreStatusManager.Instance.IsOpen);
                int activeCustomers = (CustomerShoppingManager.Instance != null) ? CustomerShoppingManager.Instance.ActiveCustomerCount : 0;

                if (isStoreClosed && activeCustomers == 0)
                {
                    if (ModalManager.IsModalOpen)
                    {
                        ModalManager.CloseModal();
                    }

                    isWaitingForEvacuation = false;
                    if (EndOfDayReportModalUI.Instance == null)
                    {
                        GameObject go = new GameObject("EndOfDayReportModalUI");
                        go.AddComponent<EndOfDayReportModalUI>();
                    }

                    if (EndOfDayReportModalUI.Instance != null)
                    {
                        EndOfDayReportModalUI.Instance.ShowReport();
                    }
                }
            }
        }

        private void ScanLowStockItems()
        {
            if (lowStockPanelRoot == null || lowStockListText == null) return;

            var furnitureList = PlacedFurnitureController.AllPlacedFurniture;
            if (furnitureList == null || furnitureList.Count == 0)
            {
                lowStockPanelRoot.SetActive(false);
                return;
            }

            Dictionary<string, int> totalStockByProduct = new Dictionary<string, int>();

            foreach (var f in furnitureList)
            {
                if (f == null || f.rows == null) continue;

                foreach (var rData in f.rows)
                {
                    if (rData == null || rData.IsUnassigned) continue;

                    string pName = rData.productName;
                    if (!totalStockByProduct.ContainsKey(pName))
                    {
                        totalStockByProduct[pName] = 0;
                    }
                    totalStockByProduct[pName] += Mathf.Max(0, rData.currentStock);
                }
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int lowStockCount = 0;

            foreach (var kvp in totalStockByProduct)
            {
                string productName = WholesaleDatabase.GetLocalizedProductName(kvp.Key);
                int totalStock = kvp.Value;

                if (totalStock <= 20)
                {
                    lowStockCount++;
                    string statusText = (totalStock <= 0) ?
                        LocalizationManager.L("Stock_Out", "<color=#FF3333>TÜKENDİ (0 Adet)</color>", "<color=#FF3333>OUT OF STOCK (0 Pcs)</color>") :
                        LocalizationManager.L("Stock_Count", $"{totalStock} Adet", $"{totalStock} Pcs");
                    string totalWord = LocalizationManager.L("Stock_TotalWord", "Toplam", "Total");
                    sb.AppendLine($"• {productName}: {totalWord} {statusText}");

                    if (lowStockCount >= 10) break;
                }
            }

            if (lowStockCount > 0)
            {
                lowStockListText.text = sb.ToString();
                lowStockPanelRoot.SetActive(true);
            }
            else
            {
                lowStockPanelRoot.SetActive(false);
            }
        }
    }
}
