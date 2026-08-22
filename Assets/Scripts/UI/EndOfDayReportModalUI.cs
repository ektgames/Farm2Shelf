using System;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Gün Sonu Z Raporu (Financial End-of-Day Report) Modal Penceresi.
    /// Gün bittiğinde (gece 12 ve müşteriler çıktıktan sonra) oyuncuya
    /// günlük gelir, gider, net kâr/zarar ve kasa bakiyesini döküm halinde sunar.
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
            if (IsReportModalOpen && canvasObj != null)
            {
                IsReportModalOpen = false;
                ShowReport();
            }
        }

        public void ShowReport()
        {
            if (IsReportModalOpen) return;

            IsReportModalOpen = true;
            ModalManager.SetModalOpen(true);

            // Eski canvas varsa temizle
            GameObject existing = GameObject.Find("Global_EndOfDay_ZReport_Canvas");
            if (existing != null) DestroyImmediate(existing);

            canvasObj = new GameObject("Global_EndOfDay_ZReport_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Arka Plan Karartma (Backdrop)
            GameObject backdrop = new GameObject("Modal_Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.07f, 0.12f, 0.88f);
            bdImg.raycastTarget = true;

            // Modal Ana Konteynırı
            GameObject boxObj = new GameObject("Report_Box");
            boxObj.transform.SetParent(backdrop.transform, false);

            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(720f, 620f);

            Image boxImg = boxObj.AddComponent<Image>();
            boxImg.sprite = UIStyleUtility.CreateOutlinePillSprite(720, 620, 24, 2, new Color(0.18f, 0.28f, 0.45f), new Color(0.10f, 0.13f, 0.18f, 0.96f));

            // 1. BAŞLIK ALANI
            GameObject titleObj = new GameObject("Report_Title");
            titleObj.transform.SetParent(boxObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 255f);
            titleRect.sizeDelta = new Vector2(680f, 50f);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = UIStyleUtility.GetGlobalFont(28);
            titleText.text = LocalizationManager.L("ZReport_Title", "📊 GÜN SONU Z RAPORU", "📊 END OF DAY Z-REPORT");
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1.0f, 0.88f, 0.25f);

            // Alt Başlık (Tarih Bilgisi)
            string dateStr = (TimeManager.Instance != null) ? TimeManager.Instance.GetFormattedDate() : "İLKBAHAR • GÜN 1";
            GameObject subObj = new GameObject("Report_SubTitle");
            subObj.transform.SetParent(boxObj.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0f, 215f);
            subRect.sizeDelta = new Vector2(680f, 35f);

            Text subText = subObj.AddComponent<Text>();
            subText.font = titleText.font;
            subText.text = LocalizationManager.L("ZReport_Subtitle", $"[ {dateStr} FİNANSAL VE MAĞAZA ÖZETİ ]", $"[ {dateStr} FINANCIAL & STORE SUMMARY ]");
            subText.fontSize = 16;
            subText.fontStyle = FontStyle.Bold;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.70f, 0.78f, 0.88f);

            // 2. FİNANSAL METRİK KARTLARI
            int revenue = (FinanceManager.Instance != null) ? FinanceManager.Instance.DailyRevenue : 0;
            int expenses = (FinanceManager.Instance != null) ? FinanceManager.Instance.DailyExpenses : 0;
            int netProfit = revenue - expenses;
            int balance = (FinanceManager.Instance != null) ? FinanceManager.Instance.CurrentBalance : 0;

            // GELİR KARTI (Yeşil)
            CreateMetricCard(boxObj.transform, new Vector2(-170f, 115f), LocalizationManager.L("Card_Revenue", "💰 Bugünkü Satış Geliri", "💰 Today's Sales Revenue"), $"{revenue:N0}C", new Color(0.20f, 0.85f, 0.45f));

            // GİDER KARTI (Kırmızı)
            CreateMetricCard(boxObj.transform, new Vector2(170f, 115f), LocalizationManager.L("Card_Expenses", "📦 Bugünkü Harcama & Gider", "📦 Today's Expenses"), $"{expenses:N0}C", new Color(0.95f, 0.30f, 0.30f));

            // NET KÂR / ZARAR KARTI (Mavi/Mor)
            Color netColor = (netProfit >= 0) ? new Color(0.30f, 0.85f, 1.0f) : new Color(1.0f, 0.40f, 0.40f);
            string netPrefix = (netProfit >= 0) ? "+" : "";
            CreateMetricCard(boxObj.transform, new Vector2(-170f, -15f), LocalizationManager.L("Card_NetProfit", "📈 Günlük Net Kâr / Zarar", "📈 Daily Net Profit / Loss"), $"{netPrefix}{netProfit:N0}C", netColor);

            // KASA BAKİYESİ KARTI (Sarı/Gold)
            CreateMetricCard(boxObj.transform, new Vector2(170f, -15f), LocalizationManager.L("Card_Balance", "💳 Güncel Kasa Bakiyesi", "💳 Current Cash Balance"), $"{balance:N0}C", new Color(1.0f, 0.85f, 0.25f));

            // 3. ÖZET BİLGİ PANESİ
            GameObject infoObj = new GameObject("Info_Panel");
            infoObj.transform.SetParent(boxObj.transform, false);
            RectTransform infoRect = infoObj.AddComponent<RectTransform>();
            infoRect.anchoredPosition = new Vector2(0f, -125f);
            infoRect.sizeDelta = new Vector2(660f, 75f);

            Image infoImg = infoObj.AddComponent<Image>();
            infoImg.sprite = UIStyleUtility.CreateOutlinePillSprite(660, 75, 14, 1, new Color(0.25f, 0.35f, 0.50f), new Color(0.12f, 0.16f, 0.22f, 0.85f));

            GameObject infoTxtObj = new GameObject("Text");
            infoTxtObj.transform.SetParent(infoObj.transform, false);
            RectTransform itRect = infoTxtObj.AddComponent<RectTransform>();
            itRect.anchorMin = Vector2.zero;
            itRect.anchorMax = Vector2.one;

            Text infoTxt = infoTxtObj.AddComponent<Text>();
            infoTxt.font = titleText.font;
            infoTxt.text = LocalizationManager.L(
                "ZReport_Info",
                "✨ Günün tüm alışveriş işlemleri ve müşteri tahliyesi tamamlandı.\n'Yeni Güne Başla' butonuna basarak sabah 06:00'ya geçebilirsiniz.",
                "✨ All daily shopping transactions and customer evacuations completed.\nClick 'Start New Day' to proceed to 06:00 AM."
            );
            infoTxt.fontSize = 15;
            infoTxt.alignment = TextAnchor.MiddleCenter;
            infoTxt.color = new Color(0.85f, 0.92f, 1.0f);

            // 4. "YENİ GÜNE BAŞLA" BUTONU
            GameObject btnObj = new GameObject("Start_New_Day_Button");
            btnObj.transform.SetParent(boxObj.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0f, -230f);
            btnRect.sizeDelta = new Vector2(460f, 65f);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = UIStyleUtility.CreateOutlinePillSprite(460, 65, 24, 2, new Color(0.20f, 0.85f, 0.45f), new Color(0.12f, 0.14f, 0.18f, 0.90f));

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            GameObject btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRect = btnTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;

            Text btnTxt = btnTxtObj.AddComponent<Text>();
            btnTxt.font = titleText.font;
            btnTxt.text = LocalizationManager.L(
                "ZReport_BtnNextDay",
                "🌅 ERTESİ GÜNE ATLA & YENİ GÜNE BAŞLA (06:00)",
                "🌅 SKIP TO NEXT DAY & START NEW DAY (06:00 AM)"
            );
            btnTxt.fontSize = 18;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = new Color(0.30f, 0.98f, 0.50f);

            btn.onClick.AddListener(OnStartNewDayClicked);
        }

        private void CreateMetricCard(Transform parent, Vector2 pos, string header, string value, Color valColor)
        {
            GameObject cardObj = new GameObject("MetricCard_" + header);
            cardObj.transform.SetParent(parent, false);

            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchoredPosition = pos;
            cardRect.sizeDelta = new Vector2(310f, 105f);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = UIStyleUtility.CreateOutlinePillSprite(310, 105, 16, 1, new Color(valColor.r * 0.6f, valColor.g * 0.6f, valColor.b * 0.6f, 0.60f), new Color(0.12f, 0.15f, 0.20f, 0.90f));

            // Başlık Metni
            GameObject hObj = new GameObject("Header");
            hObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRect = hObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 25f);
            hRect.sizeDelta = new Vector2(290f, 30f);

            Text hTxt = hObj.AddComponent<Text>();
            hTxt.font = UIStyleUtility.GetGlobalFont(14);
            hTxt.text = header;
            hTxt.fontSize = 14;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.color = new Color(0.78f, 0.85f, 0.95f);

            // Değer Metni
            GameObject vObj = new GameObject("Value");
            vObj.transform.SetParent(cardObj.transform, false);
            RectTransform vRect = vObj.AddComponent<RectTransform>();
            vRect.anchoredPosition = new Vector2(0f, -15f);
            vRect.sizeDelta = new Vector2(290f, 40f);

            Text vTxt = vObj.AddComponent<Text>();
            vTxt.font = hTxt.font;
            vTxt.text = value;
            vTxt.fontSize = 24;
            vTxt.fontStyle = FontStyle.Bold;
            vTxt.alignment = TextAnchor.MiddleCenter;
            vTxt.color = valColor;
        }

        private void OnStartNewDayClicked()
        {
            CloseReport();

            // 1. Günlük Gelir/Gider Sayaçlarını Sıfırla / Yeni Güne Devret
            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.ResetDailyStats();
            }

            // 2. Tahliye Durumu Bayrağını Sıfırla
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.SetWaitingForEvacuation(false);
            }

            // 3. Zamanı Sabah 06:00'ya Geçir ve Günlük İlerlemeyi Yap
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SkipToNextDay06AM();
            }

            // 4. Personellerin sabah modellerini senkronize et
            if (StaffVisualManager.Instance != null)
            {
                StaffVisualManager.Instance.SyncStaff3DModels();
            }
        }

        public void CloseReport()
        {
            if (canvasObj != null) Destroy(canvasObj);
            IsReportModalOpen = false;
            ModalManager.SetModalOpen(false);
        }
    }
}
