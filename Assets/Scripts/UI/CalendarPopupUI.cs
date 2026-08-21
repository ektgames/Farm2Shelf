using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Stardew Valley Tarzı 30 Günlük Tıklanabilir Takvim Penceresi (Modal UI).
    /// Bağımsız ScreenSpaceOverlay Canvas üzerinde 1920x1080 duyarlı arayüz olarak açılır.
    /// SADECE VE SADECE sağ üstteki kırmızı (X) butonu ile kapanabilir (ESC ve dış tıklama ile KAPANMAZ!).
    /// Oyuncu tüm mevsim sekmelerine ve 30 günün her birine özgürce tıklayıp inceleyebilir.
    /// </summary>
    public class CalendarPopupUI : MonoBehaviour
    {
        public static CalendarPopupUI Instance { get; private set; }
        public static bool IsCalendarModalOpen => Instance != null && Instance.popupRoot != null && Instance.popupRoot.activeSelf;

        private GameObject popupCanvasObj;
        private GameObject popupRoot;
        private RectTransform modalBoxRect;
        private Image overlayImage;
        private Text titleText;
        private Text footerSummaryText;
        private Transform gridContainer;
        private Transform tabsContainer;
        private TimeManager.Season selectedSeason;
        private int selectedDay = -1; // Oyuncunun tıkladığı seçili gün (-1 ise güncel gün)
        private Font globalFont;
        private bool isAnimating = false;

        private readonly string[] rawSeasonKeys = new string[] { "İlkbahar", "Yaz", "Sonbahar", "Kış" };
        private readonly Color[] seasonColors = new Color[] {
            new Color(0.20f, 0.85f, 0.45f), // İlkbahar Yeşil
            new Color(1.00f, 0.70f, 0.15f), // Yaz Sarı/Turuncu
            new Color(0.92f, 0.45f, 0.15f), // Sonbahar Kızıl/Kahve
            new Color(0.25f, 0.75f, 1.00f)  // Kış Mavi
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnEnable()
        {
            if (LocalizationManager.Instance != null)
            {
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
            bool wasOpen = popupCanvasObj != null && popupCanvasObj.activeSelf;
            if (popupCanvasObj != null)
            {
                Destroy(popupCanvasObj);
                popupCanvasObj = null;
                popupRoot = null;
            }
            if (wasOpen)
            {
                OpenCalendar();
            }
        }

        public void OpenCalendar()
        {
            if (isAnimating) return;

            if (popupCanvasObj == null || popupRoot == null)
            {
                CreateCalendarModal();
            }

            if (TimeManager.Instance != null)
            {
                selectedSeason = TimeManager.Instance.CurrentSeason;
                selectedDay = TimeManager.Instance.Day;
            }
            else
            {
                selectedSeason = TimeManager.Season.İlkbahar;
                selectedDay = 1;
            }

            RefreshGrid();
            popupRoot.SetActive(true);
            ModalManager.SetModalOpen(true);

            StartCoroutine(AnimateOpen());
        }

        public void CloseCalendar()
        {
            if (isAnimating || popupRoot == null || !popupRoot.activeSelf) return;

            StartCoroutine(AnimateClose());
        }

        private IEnumerator AnimateOpen()
        {
            isAnimating = true;
            float duration = 0.25f;
            float elapsed = 0f;

            Vector3 startScale = new Vector3(0.70f, 0.70f, 1f);
            Vector3 endScale = Vector3.one;

            overlayImage.color = new Color(0.04f, 0.07f, 0.12f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                modalBoxRect.localScale = Vector3.Lerp(startScale, endScale, t);
                overlayImage.color = new Color(0.04f, 0.07f, 0.12f, Mathf.Lerp(0f, 0.80f, t));
                yield return null;
            }

            modalBoxRect.localScale = endScale;
            overlayImage.color = new Color(0.04f, 0.07f, 0.12f, 0.80f);
            isAnimating = false;
        }

        private IEnumerator AnimateClose()
        {
            isAnimating = true;
            float duration = 0.20f;
            float elapsed = 0f;

            Vector3 startScale = Vector3.one;
            Vector3 endScale = new Vector3(0.70f, 0.70f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                modalBoxRect.localScale = Vector3.Lerp(startScale, endScale, t);
                overlayImage.color = new Color(0.04f, 0.07f, 0.12f, Mathf.Lerp(0.80f, 0f, t));
                yield return null;
            }

            popupRoot.SetActive(false);
            ModalManager.SetModalOpen(false);
            isAnimating = false;
        }

        private string GetSeasonTabName(int index)
        {
            switch (index)
            {
                case 0: return LocalizationManager.L("Season_TabSpring", "İlkbahar", "Spring");
                case 1: return LocalizationManager.L("Season_TabSummer", "Yaz", "Summer");
                case 2: return LocalizationManager.L("Season_TabAutumn", "Sonbahar", "Autumn");
                case 3: return LocalizationManager.L("Season_TabWinter", "Kış", "Winter");
                default: return "";
            }
        }

        private string GetSeasonRawName(TimeManager.Season season)
        {
            switch (season)
            {
                case TimeManager.Season.İlkbahar: return LocalizationManager.L("Season_Spring", "İLKBAHAR", "SPRING");
                case TimeManager.Season.Yaz: return LocalizationManager.L("Season_Summer", "YAZ", "SUMMER");
                case TimeManager.Season.Sonbahar: return LocalizationManager.L("Season_Autumn", "SONBAHAR", "AUTUMN");
                case TimeManager.Season.Kış: return LocalizationManager.L("Season_Winter", "KIŞ", "WINTER");
                default: return season.ToString().ToUpper();
            }
        }

        private void CreateCalendarModal()
        {
            if (popupCanvasObj != null) Destroy(popupCanvasObj);

            popupCanvasObj = new GameObject("Stardew_Calendar_Modal_Canvas");
            Canvas canvas = popupCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 280;

            CanvasScaler scaler = popupCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            popupCanvasObj.AddComponent<GraphicRaycaster>();

            // Modal Root Container
            popupRoot = new GameObject("Calendar_Popup_Root");
            popupRoot.transform.SetParent(popupCanvasObj.transform, false);

            RectTransform rootRect = popupRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            overlayImage = popupRoot.AddComponent<Image>();
            overlayImage.color = new Color(0.04f, 0.07f, 0.12f, 0.80f);
            overlayImage.raycastTarget = true;

            // Centered Modal Window Box (900 x 650 px)
            GameObject modalBox = new GameObject("Modal_Window_Box");
            modalBox.transform.SetParent(popupRoot.transform, false);

            modalBoxRect = modalBox.AddComponent<RectTransform>();
            modalBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalBoxRect.pivot = new Vector2(0.5f, 0.5f);
            modalBoxRect.sizeDelta = new Vector2(900f, 650f);
            modalBoxRect.anchoredPosition = Vector2.zero;

            Image boxBg = modalBox.AddComponent<Image>();
            boxBg.sprite = UIStyleUtility.CreateOutlinePillSprite(900, 650, 24, 4, new Color(0.95f, 0.75f, 0.20f, 0.90f), new Color(0.10f, 0.13f, 0.17f, 0.98f));
            boxBg.raycastTarget = true;

            globalFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (globalFont == null) globalFont = Font.CreateDynamicFontFromOSFont("Arial", 22);

            // 1. Üst Başlık (Header Bar)
            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(modalBox.transform, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 285f);
            hRect.sizeDelta = new Vector2(840f, 50f);

            titleText = headerObj.AddComponent<Text>();
            titleText.font = globalFont;
            titleText.text = "TAKVİM";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleText.verticalOverflow = VerticalWrapMode.Overflow;
            titleText.color = new Color(1.0f, 0.88f, 0.25f);
            titleText.raycastTarget = false;

            // Kapat (X) Butonu
            GameObject closeBtnObj = new GameObject("CloseButton_X");
            closeBtnObj.transform.SetParent(modalBox.transform, false);

            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(420f, 285f);
            cRect.sizeDelta = new Vector2(44f, 44f);

            Image cBg = closeBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(44, 44, 22, new Color(0.92f, 0.22f, 0.22f, 0.98f));
            cBg.raycastTarget = true;

            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBg;
            cBtn.onClick.AddListener(CloseCalendar);

            GameObject cTextObj = new GameObject("X");
            cTextObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cxRect = cTextObj.AddComponent<RectTransform>();
            cxRect.anchorMin = Vector2.zero;
            cxRect.anchorMax = Vector2.one;

            Text cxText = cTextObj.AddComponent<Text>();
            cxText.font = globalFont;
            cxText.text = "✕";
            cxText.fontSize = 24;
            cxText.fontStyle = FontStyle.Bold;
            cxText.alignment = TextAnchor.MiddleCenter;
            cxText.color = Color.white;
            cxText.raycastTarget = false;

            // 2. Mevsim Seçim Sekmeleri Barı
            GameObject tabsObj = new GameObject("SeasonTabs");
            tabsObj.transform.SetParent(modalBox.transform, false);

            RectTransform tabsRect = tabsObj.AddComponent<RectTransform>();
            tabsRect.anchoredPosition = new Vector2(0f, 226f);
            tabsRect.sizeDelta = new Vector2(840f, 48f);

            HorizontalLayoutGroup layout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;

            tabsContainer = tabsObj.transform;

            // 3. 30 Günlük Takvim Grid Alanı
            GameObject gridObj = new GameObject("CalendarGrid");
            gridObj.transform.SetParent(modalBox.transform, false);

            RectTransform gRect = gridObj.AddComponent<RectTransform>();
            gRect.anchoredPosition = new Vector2(0f, -14f);
            gRect.sizeDelta = new Vector2(840f, 395f);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(130f, 68f);
            grid.spacing = new Vector2(10f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

            gridContainer = gridObj.transform;

            // 4. Alt Bilgi Çubuğu (Footer Bar)
            GameObject footerObj = new GameObject("FooterBar");
            footerObj.transform.SetParent(modalBox.transform, false);

            RectTransform fRect = footerObj.AddComponent<RectTransform>();
            fRect.anchoredPosition = new Vector2(0f, -282f);
            fRect.sizeDelta = new Vector2(840f, 44f);

            footerSummaryText = footerObj.AddComponent<Text>();
            footerSummaryText.font = globalFont;
            footerSummaryText.text = "";
            footerSummaryText.fontSize = 19;
            footerSummaryText.fontStyle = FontStyle.Bold;
            footerSummaryText.alignment = TextAnchor.MiddleCenter;
            footerSummaryText.horizontalOverflow = HorizontalWrapMode.Overflow;
            footerSummaryText.verticalOverflow = VerticalWrapMode.Overflow;
            footerSummaryText.color = new Color(0.92f, 0.95f, 1.0f);
            footerSummaryText.raycastTarget = false;
        }

        private void RenderSeasonTabs()
        {
            if (tabsContainer == null) return;

            foreach (Transform child in tabsContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < 4; i++)
            {
                TimeManager.Season seasonEnum = (TimeManager.Season)i;
                bool isSelected = (selectedSeason == seasonEnum);

                GameObject tabBtn = new GameObject("Tab_" + rawSeasonKeys[i]);
                tabBtn.transform.SetParent(tabsContainer, false);

                RectTransform tabRect = tabBtn.AddComponent<RectTransform>();
                tabRect.sizeDelta = new Vector2(195f, 46f);

                Image tabBg = tabBtn.AddComponent<Image>();
                tabBg.raycastTarget = true;

                Color baseColor = seasonColors[i];

                if (isSelected)
                {
                    tabBg.sprite = UIStyleUtility.CreateOutlinePillSprite(195, 46, 23, 3, baseColor, new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f));
                }
                else
                {
                    tabBg.sprite = UIStyleUtility.CreateRoundedPillSprite(195, 46, 23, new Color(0.14f, 0.17f, 0.22f, 0.85f));
                }

                Button btn = tabBtn.AddComponent<Button>();
                btn.targetGraphic = tabBg;
                btn.onClick.AddListener(() => {
                    selectedSeason = seasonEnum;
                    selectedDay = 1;
                    RefreshGrid();
                });

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(tabBtn.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;

                Text tabText = textObj.AddComponent<Text>();
                tabText.font = globalFont;
                tabText.text = GetSeasonTabName(i);
                tabText.fontSize = isSelected ? 19 : 17;
                tabText.fontStyle = FontStyle.Bold;
                tabText.alignment = TextAnchor.MiddleCenter;
                tabText.horizontalOverflow = HorizontalWrapMode.Overflow;
                tabText.verticalOverflow = VerticalWrapMode.Overflow;
                tabText.color = isSelected ? Color.white : baseColor * 0.90f;
                tabText.raycastTarget = false;

                if (isSelected)
                {
                    GameObject indicator = new GameObject("Active_Indicator_Line");
                    indicator.transform.SetParent(tabBtn.transform, false);

                    RectTransform indRect = indicator.AddComponent<RectTransform>();
                    indRect.anchorMin = new Vector2(0.15f, 0f);
                    indRect.anchorMax = new Vector2(0.85f, 0f);
                    indRect.pivot = new Vector2(0.5f, 0f);
                    indRect.anchoredPosition = new Vector2(0f, 3f);
                    indRect.sizeDelta = new Vector2(0f, 4f);

                    Image indImg = indicator.AddComponent<Image>();
                    indImg.color = baseColor;
                    indImg.raycastTarget = false;
                }
            }
        }

        private void RefreshGrid()
        {
            if (gridContainer == null) return;

            RenderSeasonTabs();

            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }

            int activeDay = (TimeManager.Instance != null) ? TimeManager.Instance.Day : 1;
            TimeManager.Season activeSeason = (TimeManager.Season)((TimeManager.Instance != null) ? (int)TimeManager.Instance.CurrentSeason : 0);
            int activeYear = (TimeManager.Instance != null) ? TimeManager.Instance.Year : 1;

            Color themeColor = seasonColors[(int)selectedSeason];
            
            string stardewTitleFormat = LocalizationManager.L(
                "Calendar_TitleFormat",
                "TAKVİM — YIL {0}, {1} (30 GÜN)",
                "CALENDAR — YEAR {0}, {1} (30 DAYS)"
            );
            titleText.text = string.Format(stardewTitleFormat, activeYear, GetSeasonRawName(selectedSeason));
            titleText.color = themeColor;

            // Alt bilgi güncellemesi
            UpdateFooterInfo(activeSeason, activeDay);

            for (int day = 1; day <= 30; day++)
            {
                int currentDayNum = day;
                GameObject cardObj = new GameObject("Day_" + currentDayNum);
                cardObj.transform.SetParent(gridContainer, false);

                bool isToday = (selectedSeason == activeSeason && currentDayNum == activeDay);
                bool isSelectedDay = (currentDayNum == selectedDay);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.raycastTarget = true;

                if (isToday)
                {
                    cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(130, 68, 14, 3, new Color(1.0f, 0.85f, 0.20f), new Color(0.25f, 0.45f, 0.25f, 0.95f));
                }
                else if (isSelectedDay)
                {
                    cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(130, 68, 14, 2, themeColor, new Color(0.20f, 0.26f, 0.35f, 0.95f));
                }
                else
                {
                    cardBg.sprite = UIStyleUtility.CreateRoundedPillSprite(130, 68, 14, new Color(0.15f, 0.19f, 0.24f, 0.88f));
                }

                Button dayBtn = cardObj.AddComponent<Button>();
                dayBtn.targetGraphic = cardBg;
                dayBtn.onClick.AddListener(() => {
                    selectedDay = currentDayNum;
                    RefreshGrid();
                });

                // Gün Sayısı Metni
                GameObject dayTextObj = new GameObject("DayText");
                dayTextObj.transform.SetParent(cardObj.transform, false);

                RectTransform dRect = dayTextObj.AddComponent<RectTransform>();
                dRect.anchorMin = new Vector2(0f, 0.42f);
                dRect.anchorMax = new Vector2(1f, 0.98f);
                dRect.offsetMin = Vector2.zero;
                dRect.offsetMax = Vector2.zero;

                Text dText = dayTextObj.AddComponent<Text>();
                dText.font = globalFont;
                string dayPrefix = LocalizationManager.L("Day_UpperWord", "GÜN", "DAY");
                dText.text = isToday ? $"★ {dayPrefix} {currentDayNum} ★" : $"{dayPrefix} {currentDayNum}";
                dText.fontSize = isToday ? 17 : 16;
                dText.fontStyle = FontStyle.Bold;
                dText.alignment = TextAnchor.MiddleCenter;
                dText.horizontalOverflow = HorizontalWrapMode.Overflow;
                dText.verticalOverflow = VerticalWrapMode.Overflow;
                dText.color = isToday ? new Color(1.0f, 0.95f, 0.35f) : (isSelectedDay ? Color.white : new Color(0.92f, 0.95f, 0.98f));
                dText.raycastTarget = false;

                // O Günün Hava Durumu Tahmini (Güneşli, Yağmurlu, Karlı)
                Farm2Shelf.Environment.WeatherType dayWeather = Farm2Shelf.Environment.WeatherManager.GetWeatherForecastForDay(selectedSeason, currentDayNum, activeYear);
                string weatherLabel = "";
                Color weatherCol = Color.white;

                switch (dayWeather)
                {
                    case Farm2Shelf.Environment.WeatherType.Sunny:
                        weatherLabel = LocalizationManager.L("Weather_Label_Sunny", "Güneşli", "Sunny");
                        weatherCol = new Color(1.0f, 0.88f, 0.30f);
                        break;
                    case Farm2Shelf.Environment.WeatherType.Rainy:
                        weatherLabel = LocalizationManager.L("Weather_Label_Rainy", "Yağmurlu", "Rainy");
                        weatherCol = new Color(0.40f, 0.85f, 1.0f);
                        break;
                    case Farm2Shelf.Environment.WeatherType.Snowy:
                        weatherLabel = LocalizationManager.L("Weather_Label_Snowy", "Karlı", "Snowy");
                        weatherCol = new Color(0.85f, 0.95f, 1.0f);
                        break;
                }

                if (isToday)
                {
                    weatherCol = new Color(0.45f, 0.98f, 0.55f);
                }

                GameObject weatherObj = new GameObject("WeatherBadgeText");
                weatherObj.transform.SetParent(cardObj.transform, false);

                RectTransform wRect = weatherObj.AddComponent<RectTransform>();
                wRect.anchorMin = new Vector2(0f, 0.04f);
                wRect.anchorMax = new Vector2(1f, 0.46f);
                wRect.offsetMin = Vector2.zero;
                wRect.offsetMax = Vector2.zero;

                Text wText = weatherObj.AddComponent<Text>();
                wText.font = globalFont;
                wText.text = weatherLabel;
                wText.fontSize = 14;
                wText.fontStyle = FontStyle.Bold;
                wText.alignment = TextAnchor.MiddleCenter;
                wText.horizontalOverflow = HorizontalWrapMode.Overflow;
                wText.verticalOverflow = VerticalWrapMode.Overflow;
                wText.color = weatherCol;
                wText.raycastTarget = false;
            }
        }

        private void UpdateFooterInfo(TimeManager.Season activeSeason, int activeDay)
        {
            if (footerSummaryText == null) return;

            string selectedSeasonStr = GetSeasonRawName(selectedSeason);
            string activeSeasonStr = GetSeasonRawName(activeSeason);
            string eventDetail = "";

            if (selectedSeason == activeSeason && selectedDay == activeDay)
            {
                eventDetail = LocalizationManager.L("Cal_FooterToday", "BUGÜN (Oyunun Güncel Tarihi)", "TODAY (Current Game Date)");
            }
            else if (selectedDay == 1)
            {
                eventDetail = LocalizationManager.L("Cal_FooterDay1", $"{selectedSeasonStr} Mevsiminin 1. Günü (Başlangıç)", $"{selectedSeasonStr} Season Day 1 (Start)");
            }
            else if (selectedDay == 30)
            {
                eventDetail = LocalizationManager.L("Cal_FooterDay30", $"{selectedSeasonStr} Mevsiminin 30. Günü (Son Gün - Sezon Sonu)", $"{selectedSeasonStr} Season Day 30 (Last Day - Season End)");
            }
            else if (selectedDay > 0)
            {
                eventDetail = LocalizationManager.L("Cal_FooterDayN", $"{selectedSeasonStr} Mevsimi, Gün {selectedDay}", $"{selectedSeasonStr} Season, Day {selectedDay}");
            }

            footerSummaryText.text = LocalizationManager.L(
                "Cal_FooterSummaryFormat",
                $"Seçilen: {eventDetail}  •  Güncel Tarih: {activeSeasonStr}, Gün {activeDay}",
                $"Selected: {eventDetail}  •  Current Date: {activeSeasonStr}, Day {activeDay}"
            );
        }
    }
}
