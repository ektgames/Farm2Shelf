using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Gün sonu Z raporu: bugünün hikâyesi ve yalnızca "Ertesi güne atla" ile kapanır.
    /// </summary>
    public class EndOfDayReportModalUI : MonoBehaviour
    {
        public static EndOfDayReportModalUI Instance { get; private set; }

        private GameObject canvasObj;
        public static bool IsReportModalOpen { get; private set; } = false;

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
            if (IsReportModalOpen)
            {
                BuildReportCanvas();
            }
        }

        private void Update()
        {
            if (!IsReportModalOpen) return;

            if (canvasObj == null || !canvasObj.activeInHierarchy)
            {
                BuildReportCanvas();
            }
        }

        public void ShowReport()
        {
            if (IsReportModalOpen && canvasObj != null && canvasObj.activeInHierarchy) return;
            BuildReportCanvas();
        }

        private void BuildReportCanvas()
        {
            if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen)
            {
                PauseMenuUI.Instance.HideMenu();
            }

            IsReportModalOpen = true;
            ModalManager.SetModalOpen(true);

            if (EKTPhoneManager.Instance != null && EKTPhoneManager.IsTabletOpen)
            {
                EKTPhoneManager.Instance.ClosePhoneTabletInstant();
            }

            if (canvasObj != null) Destroy(canvasObj);

            GameObject existing = GameObject.Find("Global_EndOfDay_ZReport_Canvas");
            if (existing != null) DestroyImmediate(existing);

            canvasObj = new GameObject("Global_EndOfDay_ZReport_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5200;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject backdrop = new GameObject("Modal_Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.03f, 0.05f, 0.09f, 0.94f);
            bdImg.raycastTarget = true;

            GameObject boxObj = new GameObject("Report_Box");
            boxObj.transform.SetParent(backdrop.transform, false);
            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(880f, 960f);

            Image boxImg = boxObj.AddComponent<Image>();
            boxImg.sprite = UIStyleUtility.CreateOutlinePillSprite(880, 960, 24, 2, new Color(0.22f, 0.55f, 0.38f), new Color(0.08f, 0.11f, 0.16f, 0.98f));
            boxImg.raycastTarget = false;

            Font font = UIStyleUtility.GetGlobalFont(22);
            string dateStr = TimeManager.Instance != null ? TimeManager.Instance.GetFormattedDate() : "İLKBAHAR • GÜN 1";
            string storeName = StoreStatusManager.Instance != null ? StoreStatusManager.Instance.CompanyName : "Farm2Shelf Market";

            GameObject titleObj = new GameObject("Report_Title");
            titleObj.transform.SetParent(boxObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -18f);
            titleRect.sizeDelta = new Vector2(820f, 48f);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.text = LocalizationManager.L("ZReport_Title", "GÜN SONU DEFTERİ", "END OF DAY LEDGER");
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1.0f, 0.88f, 0.32f);
            titleText.raycastTarget = false;

            GameObject subObj = new GameObject("Report_SubTitle");
            subObj.transform.SetParent(boxObj.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0f, -64f);
            subRect.sizeDelta = new Vector2(820f, 36f);

            Text subText = subObj.AddComponent<Text>();
            subText.font = font;
            subText.text = $"{storeName}  •  {dateStr}";
            subText.fontSize = 16;
            subText.fontStyle = FontStyle.Bold;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.72f, 0.82f, 0.92f);
            subText.raycastTarget = false;

            int revenue = FinanceManager.Instance != null ? FinanceManager.Instance.DailyRevenue : 0;
            int expenses = FinanceManager.Instance != null ? FinanceManager.Instance.DailyExpenses : 0;
            int netProfit = revenue - expenses;
            int balance = FinanceManager.Instance != null ? FinanceManager.Instance.CurrentBalance : 0;
            Color netColor = netProfit >= 0 ? new Color(0.35f, 0.90f, 0.70f) : new Color(1.0f, 0.42f, 0.42f);
            string netPrefix = netProfit >= 0 ? "+" : "";

            CreateMetricStrip(boxObj.transform, font, revenue, expenses, netPrefix, netProfit, netColor, balance);

            GameObject scrollRoot = new GameObject("Story_Scroll");
            scrollRoot.transform.SetParent(boxObj.transform, false);
            RectTransform scrollRectTf = scrollRoot.AddComponent<RectTransform>();
            scrollRectTf.anchorMin = new Vector2(0.5f, 0f);
            scrollRectTf.anchorMax = new Vector2(0.5f, 1f);
            scrollRectTf.pivot = new Vector2(0.5f, 0.5f);
            scrollRectTf.anchoredPosition = new Vector2(0f, 18f);
            scrollRectTf.sizeDelta = new Vector2(820f, -250f);

            Image scrollBg = scrollRoot.AddComponent<Image>();
            scrollBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 640, 16, 1, new Color(0.20f, 0.32f, 0.42f, 0.70f), new Color(0.07f, 0.10f, 0.14f, 0.92f));
            scrollBg.raycastTarget = true;

            ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            scroll.inertia = true;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollRoot.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(16f, 14f);
            vpRect.offsetMax = new Vector2(-16f, -14f);
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.01f);
            vpImg.raycastTarget = true;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 800f);

            GameObject storyObj = new GameObject("StoryText");
            storyObj.transform.SetParent(content.transform, false);
            RectTransform storyRect = storyObj.AddComponent<RectTransform>();
            storyRect.anchorMin = new Vector2(0f, 1f);
            storyRect.anchorMax = new Vector2(1f, 1f);
            storyRect.pivot = new Vector2(0.5f, 1f);
            storyRect.anchoredPosition = Vector2.zero;
            storyRect.sizeDelta = new Vector2(0f, 10f);

            Text storyTxt = storyObj.AddComponent<Text>();
            storyTxt.font = font;
            storyTxt.fontSize = 17;
            storyTxt.alignment = TextAnchor.UpperLeft;
            storyTxt.color = new Color(0.90f, 0.94f, 0.98f);
            storyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            storyTxt.verticalOverflow = VerticalWrapMode.Overflow;
            storyTxt.raycastTarget = false;
            storyTxt.lineSpacing = 1.12f;
            storyTxt.text = BuildDayStory();

            ContentSizeFitter fitter = storyObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(4, 4, 4, 24);

            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRect;
            scroll.content = contentRect;

            GameObject btnObj = new GameObject("Start_New_Day_Button");
            btnObj.transform.SetParent(boxObj.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0f);
            btnRect.anchorMax = new Vector2(0.5f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0f, 22f);
            btnRect.sizeDelta = new Vector2(640f, 72f);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = UIStyleUtility.CreateOutlinePillSprite(640, 72, 22, 2, new Color(0.22f, 0.88f, 0.48f), new Color(0.10f, 0.28f, 0.16f, 0.96f));
            btnImg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.transition = Selectable.Transition.ColorTint;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(OnStartNewDayClicked);

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRect = btnTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = Vector2.zero;
            btRect.offsetMax = Vector2.zero;

            Text btnTxt = btnTxtObj.AddComponent<Text>();
            btnTxt.font = font;
            btnTxt.text = LocalizationManager.L(
                "ZReport_BtnNextDay",
                "ERTESİ GÜNE ATLA  (06:00)",
                "SKIP TO NEXT DAY  (06:00 AM)"
            );
            btnTxt.fontSize = 22;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = new Color(0.88f, 1.0f, 0.90f);
            btnTxt.raycastTarget = false;
            btnTxt.resizeTextForBestFit = true;
            btnTxt.resizeTextMinSize = 16;
            btnTxt.resizeTextMaxSize = 22;

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(btnObj);
            }
        }

        private static void CreateMetricStrip(Transform parent, Font font, int revenue, int expenses, string netPrefix, int netProfit, Color netColor, int balance)
        {
            GameObject strip = new GameObject("Metric_Strip");
            strip.transform.SetParent(parent, false);
            RectTransform stripRect = strip.AddComponent<RectTransform>();
            stripRect.anchorMin = new Vector2(0.5f, 1f);
            stripRect.anchorMax = new Vector2(0.5f, 1f);
            stripRect.pivot = new Vector2(0.5f, 1f);
            stripRect.anchoredPosition = new Vector2(0f, -108f);
            stripRect.sizeDelta = new Vector2(820f, 78f);

            Image stripImg = strip.AddComponent<Image>();
            stripImg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 78, 14, 1, new Color(0.25f, 0.38f, 0.48f, 0.55f), new Color(0.10f, 0.14f, 0.18f, 0.90f));
            stripImg.raycastTarget = false;

            string line = LocalizationManager.L(
                "ZReport_MetricStrip",
                $"Gelir {revenue:N0}C   ·   Gider {expenses:N0}C   ·   Net {netPrefix}{netProfit:N0}C   ·   Kasa {balance:N0}C",
                $"Revenue {revenue:N0}C   ·   Spend {expenses:N0}C   ·   Net {netPrefix}{netProfit:N0}C   ·   Cash {balance:N0}C"
            );

            GameObject txtObj = new GameObject("StripText");
            txtObj.transform.SetParent(strip.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(12f, 4f);
            tRect.offsetMax = new Vector2(-12f, -4f);

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = line;
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = netColor;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 12;
            txt.resizeTextMaxSize = 16;
        }

        private static string BuildDayStory()
        {
            bool en = LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish;
            StringBuilder sb = new StringBuilder(2048);

            string store = StoreStatusManager.Instance != null ? StoreStatusManager.Instance.CompanyName : "Farm2Shelf Market";
            string slogan = StoreStatusManager.Instance != null ? StoreStatusManager.Instance.GetResolvedSlogan() : "";
            BrandIdentity identity = StoreStatusManager.Instance != null ? StoreStatusManager.Instance.Identity : BrandIdentity.LocalProducer;
            string identityName = StoreStatusManager.GetIdentityDisplayName(identity);
            string dateStr = TimeManager.Instance != null ? TimeManager.Instance.GetFormattedDate() : "";
            string weather = GetWeatherLine(en);

            sb.AppendLine(en
                ? $"{store} closed the shutters. Identity: {identityName}. Slogan on the fascia: \"{slogan}\"."
                : $"{store} kepenkleri indirdi. Kimlik: {identityName}. Tabeladaki slogan: \"{slogan}\".");
            sb.AppendLine();
            sb.AppendLine(en
                ? $"The ledger for {dateStr} is in. {weather}"
                : $"{dateStr} defteri kapatıldı. {weather}");
            sb.AppendLine();

            int revenue = FinanceManager.Instance != null ? FinanceManager.Instance.DailyRevenue : 0;
            int expenses = FinanceManager.Instance != null ? FinanceManager.Instance.DailyExpenses : 0;
            int net = revenue - expenses;
            int balance = FinanceManager.Instance != null ? FinanceManager.Instance.CurrentBalance : 0;

            List<TransactionRecord> today = FinanceManager.Instance != null
                ? FinanceManager.Instance.GetTransactionsForDate(dateStr)
                : new List<TransactionRecord>();

            int saleCount = 0;
            int saleSum = 0;
            int localSales = 0;
            int wholesaleSales = 0;
            int mixedSales = 0;
            int onlineSum = 0;
            int onlineCount = 0;
            int contractSum = 0;
            int contractCount = 0;
            int taxiSum = 0;
            int taxiCount = 0;
            int salarySum = 0;
            Dictionary<string, int> expenseByCat = new Dictionary<string, int>();

            for (int i = 0; i < today.Count; i++)
            {
                TransactionRecord rec = today[i];
                if (rec == null) continue;
                if (rec.isIncome)
                {
                    if (rec.category == FinanceCategories.Sales)
                    {
                        saleCount++;
                        saleSum += rec.amount;
                        string d = rec.description ?? "";
                        bool loc = d.IndexOf("Yerel", StringComparison.OrdinalIgnoreCase) >= 0 || d.IndexOf("Local harvest", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool wh = d.IndexOf("Toptan", StringComparison.OrdinalIgnoreCase) >= 0 || d.IndexOf("Wholesale", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (loc && wh) mixedSales++;
                        else if (loc) localSales++;
                        else if (wh) wholesaleSales++;
                    }
                    else if (rec.category == FinanceCategories.OnlineDelivery)
                    {
                        onlineCount++;
                        onlineSum += rec.amount;
                    }
                    else if (rec.category == FinanceCategories.TownContracts)
                    {
                        contractCount++;
                        contractSum += rec.amount;
                    }
                    else if (rec.category == FinanceCategories.TaxiIncome)
                    {
                        taxiCount++;
                        taxiSum += rec.amount;
                    }
                }
                else
                {
                    string cat = string.IsNullOrEmpty(rec.category) ? "?" : rec.category;
                    if (!expenseByCat.ContainsKey(cat)) expenseByCat[cat] = 0;
                    expenseByCat[cat] += rec.amount;
                    if (cat == FinanceCategories.Salary || cat == FinanceCategories.Overtime) salarySum += rec.amount;
                }
            }

            if (saleCount == 0)
            {
                sb.AppendLine(en
                    ? "The checkout stayed quiet. No in-store baskets were rung up."
                    : "Kasa sessiz kaldı. Dükkanda fiş kesilmedi.");
            }
            else
            {
                sb.AppendLine(en
                    ? $"Checkout rang {saleCount} basket(s) for {saleSum:N0}C."
                    : $"Kasada {saleCount} sepet okundu, {saleSum:N0}C girdi.");
                if (localSales + wholesaleSales + mixedSales > 0)
                {
                    sb.AppendLine(en
                        ? $"Passport mix: {localSales} local harvest, {wholesaleSales} wholesale, {mixedSales} mixed."
                        : $"Pasaport dökümü: {localSales} yerel hasat, {wholesaleSales} toptan, {mixedSales} karışık sepet.");
                }
            }
            sb.AppendLine();

            switch (identity)
            {
                case BrandIdentity.NeighborhoodMarket:
                    sb.AppendLine(en
                        ? "As a neighborhood value market, price-sensitive shoppers set the pace today."
                        : "Ekonomik mahalle marketi olarak fiyat hassas müşteri bugünün ritmini belirledi.");
                    break;
                case BrandIdentity.GourmetWorkshop:
                    sb.AppendLine(en
                        ? "As a gourmet workshop, crafted lots were the story you wanted on the till."
                        : "Gurme atölye kimliğiyle kasanın asıl hikâyesi işlenmiş ve gurme lotlardı.");
                    break;
                default:
                    sb.AppendLine(en
                        ? "As a local producer, the till preferred passport-tracked harvest over generic wholesale."
                        : "Yerel üretici olarak kasa, pasaportlu hasadı jenerik toptanın önüne koydu.");
                    break;
            }
            sb.AppendLine();

            if (onlineCount > 0)
            {
                sb.AppendLine(en
                    ? $"Online / courier runs closed {onlineCount} delivery payout(s) for {onlineSum:N0}C."
                    : $"Online market / kurye {onlineCount} teslimat ödemesi getirdi: {onlineSum:N0}C.");
            }
            else
            {
                sb.AppendLine(en
                    ? "No online courier payouts hit the ledger."
                    : "Online kurye ödemesi deftere girmedi.");
            }

            if (TownContractManager.Instance != null)
            {
                int ok = TownContractManager.Instance.SuccessCount;
                int fail = TownContractManager.Instance.FailCount;
                if (contractCount > 0)
                {
                    sb.AppendLine(en
                        ? $"Town contracts paid {contractCount} bonus(es) totaling {contractSum:N0}C."
                        : $"Kasaba kontratları {contractCount} prim yazdı: {contractSum:N0}C.");
                }
                sb.AppendLine(en
                    ? $"Contract record to date: {ok} completed, {fail} short."
                    : $"Kontrat sicili: {ok} tamamlandı, {fail} eksik kaldı.");
                string note = en ? TownContractManager.Instance.LastNoteEn : TownContractManager.Instance.LastNoteTr;
                if (!string.IsNullOrWhiteSpace(note))
                {
                    sb.AppendLine(note);
                }
            }
            sb.AppendLine();

            if (taxiCount > 0)
            {
                sb.AppendLine(en
                    ? $"Taxi stand closed {taxiCount} fare(s) for {taxiSum:N0}C."
                    : $"Taksi durağı {taxiCount} yolculuk ücreti yazdı: {taxiSum:N0}C.");
            }
            else
            {
                sb.AppendLine(en
                    ? "No taxi fares were booked today."
                    : "Bugün taksi ücreti yazılmadı.");
            }
            sb.AppendLine();

            if (expenseByCat.Count == 0)
            {
                sb.AppendLine(en
                    ? "No expenses were booked today besides what already sits in the totals."
                    : "Bugün kalem kalem gider yazılmadı; özet yukarıdaki toplamda.");
            }
            else
            {
                sb.AppendLine(en ? "Money that left the till:" : "Kasadan çıkanlar:");
                foreach (var kv in expenseByCat)
                {
                    sb.AppendLine($"  • {FinanceCategories.Localize(kv.Key)}: {kv.Value:N0}C");
                }
            }

            if (salarySum > 0)
            {
                sb.AppendLine(en
                    ? $"Payroll / overtime tonight: {salarySum:N0}C."
                    : $"Bu gece maaş / mesai: {salarySum:N0}C.");
            }
            sb.AppendLine();

            int praise = 0;
            int complaint = 0;
            if (SocialMediaManager.Instance != null)
            {
                var feed = SocialMediaManager.Instance.GetTweetFeed();
                if (feed != null)
                {
                    for (int i = 0; i < feed.Count; i++)
                    {
                        if (feed[i] == null) continue;
                        if (feed[i].sentiment == TweetSentiment.Praise) praise++;
                        else if (feed[i].sentiment == TweetSentiment.Complaint) complaint++;
                    }
                }
            }

            sb.AppendLine(en
                ? $"Town chatter on the feed: {praise} warm notes, {complaint} complaints (open timeline, not reset at dusk)."
                : $"Mahalle akışında {praise} övgü, {complaint} şikayet görünüyor (zaman çizelgesi gece sıfırlanmaz).");

            int stale = ProductPassportService.CountStaleUnitsOnShelves();
            if (stale > 0)
            {
                sb.AppendLine(en
                    ? $"{stale} stale unit(s) are still on display. Tomorrow those lots will drag price and tweets."
                    : $"Rafta hâlâ {stale} bayat birim var. Yarın fiyatı ve tweet'leri aşağı çeker.");
            }
            else
            {
                sb.AppendLine(en
                    ? "No stale passport lots were counted on the sales floor at closing."
                    : "Kapanışta satış reyonunda bayat pasaport lotu sayılmadı.");
            }
            sb.AppendLine();

            if (net >= 0)
            {
                sb.AppendLine(en
                    ? $"The day finished in the black: {net:N0}C. Cash on hand is {balance:N0}C."
                    : $"Gün artıda kapandı: {net:N0}C. Kasada {balance:N0}C var.");
            }
            else
            {
                sb.AppendLine(en
                    ? $"The day finished in the red: {net:N0}C. Cash on hand is {balance:N0}C. Watch the bankruptcy line."
                    : $"Gün eksiyle kapandı: {net:N0}C. Kasada {balance:N0}C kaldı. İflas eşiğini izleyin.");
            }
            sb.AppendLine();
            sb.AppendLine(en
                ? "The street is dark. Only Skip to next day opens 06:00. Nothing else on this page will move the clock."
                : "Cadde karardı. Saati 06:00'ya yalnızca Ertesi güne atla götürür. Bu sayfada başka bir dokunuş işe yaramaz.");

            return sb.ToString();
        }

        private static string GetWeatherLine(bool en)
        {
            if (WeatherManager.Instance == null)
            {
                return en ? "Sky: not logged." : "Hava kaydı yok.";
            }

            switch (WeatherManager.Instance.CurrentWeather)
            {
                case WeatherType.Rainy:
                    return en ? "Rain sat on the glass all evening." : "Akşam boyu yağmur cama vurdu.";
                case WeatherType.Snowy:
                    return en ? "Snow dusted the parking lot at close." : "Kapanışta otoparka kar düştü.";
                default:
                    return en ? "It was a clear-sky close." : "Kapanış açık havada oldu.";
            }
        }

        private void OnStartNewDayClicked()
        {
            if (canvasObj != null)
            {
                Destroy(canvasObj);
                canvasObj = null;
            }
            IsReportModalOpen = false;
            ModalManager.SetModalOpen(false);

            if (StoreStatusManager.Instance != null && StoreStatusManager.Instance.IsOpen)
            {
                StoreStatusManager.Instance.CloseStore();
            }

            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.ResetDailyStats();
            }

            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.SetWaitingForEvacuation(false);
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SkipToNextDay06AM();
            }

            if (StaffVisualManager.Instance != null)
            {
                StaffVisualManager.Instance.SyncStaff3DModels();
            }
        }

        public void CloseReport()
        {
            // Z raporu yalnızca "Ertesi Güne Atla" ile kapanır.
        }
    }
}
