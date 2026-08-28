using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;
using Farm2Shelf.CameraSystem;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Sağ alt köşede yer alan EKT PHONE tırnaklı butonu ve basıldığında sinematik animasyonla
    /// açılan gerçekçi Tablet cihazı arayüz yöneticisi.
    /// Kademeli sırayla açılan Seviye 2 (18 Araç) ve Seviye 3 (26 Araç) dükkan & dinamik otopark genişletme arayüzü.
    /// </summary>
    public class EKTPhoneManager : MonoBehaviour
    {
        public static EKTPhoneManager Instance { get; private set; }
        public static bool IsTabletOpen => Instance != null && Instance.tabletPopupRoot != null && Instance.tabletPopupRoot.activeSelf;

        private GameObject phoneButtonObj;
        private GameObject tabletPopupRoot;
        private RectTransform tabletBoxRect;
        private Image overlayImage;

        private Transform homeScreenView;
        private Transform storeMgmtAppView;
        private Transform financeAppView;
        private Transform farmAppView;
        private Transform shoppingAppView;
        private Transform socialMediaAppView;
        private Transform workshopsAppView;
        private Transform workshopUpgradeContent;
        private Transform workshopManagementViewportObj;
        private Transform workshopMachinesContent;
        private Transform workshopMachinesViewportObj;
        private Transform virtualMarketAppView;
        private Transform virtualMarketContent;
        private Transform virtualMarketViewportObj;

        // Online Market ScrollRect Content Transform'ları (4 Sekme)
        private Transform onlineMarketFleetContent;
        private Transform onlineMarketStaffContent;
        private Transform onlineMarketCandidateContent;
        private Transform onlineMarketShiftContent;

        private Transform onlineMarketFleetViewportObj;
        private Transform onlineMarketStaffViewportObj;
        private Transform onlineMarketCandidateViewportObj;
        private Transform onlineMarketShiftViewportObj;

        private int activeOnlineMarketTab = 0; // 0: Filo & Siparişler, 1: Kadro, 2: İşe Alım, 3: Vardiyalar
        private Image[] onlineMarketTabBtnImgs = new Image[4];
        private Text[] onlineMarketTabBtnTexts = new Text[4];

        private int activeWorkshopTab = 0; // 0: Atölye Binası (Geliştirme), 1: Makine Yönetimi
        private float lastWorkshopLiveRefreshTime = 0f;
        private Transform socialMediaFeedContent;
        private int activeSocialTab = 0; // 0: Sana Özel (For You), 1: Yorumlar (Reviews), 2: Profilim (My Tweets)
        private Text socialProfileCardTxt;
        private Text socialTrendCardTxt;

        // Shopping App Content & Category Transform'ları
        private Transform shoppingCategoryContent;
        private Transform shoppingCategoryViewportObj;
        private Transform shoppingMainContentArea;
        private Text shoppingCategoryHeaderTitle;
        private Text shoppingCategoryHeaderSub;
        private int activeShoppingCategory = 0;
        private int activeRenovationSubTab = 0;

        private Transform furnitureListContent;
        private Transform furnitureViewportObj;
        private readonly Dictionary<FurnitureType, int> shoppingCart = new Dictionary<FurnitureType, int>();
        private readonly Dictionary<string, int> wholesaleCart = new Dictionary<string, int>();
        private readonly Dictionary<string, int> seedCart = new Dictionary<string, int>();

        private string currentShoppingSearchQuery = "";
        private InputField shoppingSearchInputField;

        private Text shoppingCartSummaryText;
        private Text headerCartButtonText;
        private Button checkoutCartButton;
        private GameObject shoppingCartSummaryPanelObj;

        private readonly string[] shoppingCategories = new string[] {
            "🛋️ Mobilyalar",
            "🎨 Dekorasyonlar",
            "📦 Toptancı",
            "🌱 Tohumlar"
        };

        // Mağaza Yönetimi ScrollRect Content Transform'ları (4 Sekme)
        private Transform upgradeListContent;
        private Transform staffListContent;
        private Transform candidateListContent;
        private Transform shiftListContent;

        private Transform upgradeViewportObj;
        private Transform staffViewportObj;
        private Transform candidateViewportObj;
        private Transform shiftViewportObj;

        // Çiftlik Yönetimi ScrollRect Content Transform'ları (4 Sekme)
        private Transform farmOverviewContent;
        private Transform farmCandidateContent;
        private Transform farmStaffContent;
        private Transform farmShiftContent;

        private Transform farmOverviewViewportObj;
        private Transform farmCandidateViewportObj;
        private Transform farmStaffViewportObj;
        private Transform farmShiftViewportObj;

        // Finans Yönetimi ScrollRect Content Transform'ları
        private Transform financeProductsContent;
        private Transform financeSummaryContent;
        private Transform financeHistoryContent;
        private Transform financeLoansContent;
        private Transform financeStocksContent;

        private Transform financeProductsViewportObj;
        private Transform financeSummaryViewportObj;
        private Transform financeHistoryViewportObj;
        private Transform financeLoansViewportObj;
        private Transform financeStocksViewportObj;
        private Transform financeProductsControlBar;
        private string currentFinanceProductSearchQuery = "";
        private string selectedStockTicker = "AGRO";
        private int stockTradeQuantity = 10;

        private Font globalFont;
        private bool isAnimating = false;
        private int activeTab = 0; // 0: Marketi Geliştir (EN SOLDA), 1: Personel Kadrosu, 2: İşe Alım, 3: Vardiyalar
        private int activeFinanceTab = 0; // Finans: 0: Ürünler (EN SOLDA), 1: Özet Dashboard, 2: İşlem Geçmişi
        private Image[] socialTabBtnImgs = new Image[3];
        private int activeFarmTab = 0; // Çiftlik: 0: Genel Durum, 1: İşe Alım, 2: Çalışanlar, 3: Vardiyalar
        private Image[] storeTabBtnImgs = new Image[4];
        private Text[] storeTabBtnTexts = new Text[4];
        private Image[] farmTabBtnImgs = new Image[4];
        private Text[] farmTabBtnTexts = new Text[4];
        private Transform tabletCloseButtonTransform;

        private void EnsureCloseButtonOnTop()
        {
            if (tabletCloseButtonTransform != null)
            {
                tabletCloseButtonTransform.SetAsLastSibling();
            }
        }

        private string GetRoleCategoryName(int index)
        {
            switch (index)
            {
                case 0: return LocalizationManager.L("Role_Cashiers", "🛒 Kasiyerler", "🛒 Cashiers");
                case 1: return LocalizationManager.L("Role_Stockers", "📦 Reyoncular", "📦 Stockers");
                case 2: return LocalizationManager.L("Role_Cleaners", "🧹 Temizlikçiler", "🧹 Janitors");
                case 3: return LocalizationManager.L("Role_Guards", "🛡️ Güvenlikler", "🛡️ Security Guards");
                case 4: return LocalizationManager.L("Role_CustomerService", "💬 Müşteri Hizmetlileri", "💬 Customer Support");
                case 5: return LocalizationManager.L("Role_Mascots", "🎭 Maskotlar", "🎭 Mascots");
                default: return "";
            }
        }

        private readonly Color[] roleCategoryColors = new Color[] {
            new Color(0.20f, 0.70f, 0.95f),
            new Color(0.25f, 0.85f, 0.40f),
            new Color(0.95f, 0.75f, 0.15f),
            new Color(0.90f, 0.35f, 0.30f),
            new Color(0.75f, 0.35f, 0.95f),
            new Color(0.95f, 0.45f, 0.75f)
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
                LocalizationManager.Instance.OnLanguageChanged -= RefreshAllPhoneDisplays;
                LocalizationManager.Instance.OnLanguageChanged += RefreshAllPhoneDisplays;
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshAllPhoneDisplays;
            }
        }

        private void Start()
        {
            CreateBottomRightPhoneButtonOnCanvas();

            if (StaffManager.Instance != null)
            {
                StaffManager.Instance.OnStaffListChanged += RefreshStoreManagementViews;
            }

            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.OnFinanceUpdated += RefreshFinanceViews;
            }

            if (BankLoanManager.Instance != null)
            {
                BankLoanManager.Instance.OnBankLoansUpdated += RefreshFinanceViews;
            }

            if (StockMarketManager.Instance != null)
            {
                StockMarketManager.Instance.OnStockMarketUpdated += RefreshFinanceViews;
            }

            if (CourierManager.Instance != null)
            {
                CourierManager.Instance.OnFleetUpdated += RefreshVirtualMarketViews;
            }

            if (OnlineMarketOrderManager.Instance != null)
            {
                OnlineMarketOrderManager.Instance.OnOrdersChanged += RefreshVirtualMarketViews;
            }

            EnvironmentBuilder.OnStoreUpgraded += (lvl) => RefreshStoreManagementViews();
            WorkshopManager.OnWorkshopUpgraded += (lvl) => RefreshWorkshopsViews();

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshAllPhoneDisplays;
                LocalizationManager.Instance.OnLanguageChanged += RefreshAllPhoneDisplays;
            }

            if (SocialMediaManager.Instance != null)
            {
                SocialMediaManager.Instance.OnFeedUpdated -= RefreshSocialMediaViews;
                SocialMediaManager.Instance.OnFeedUpdated += RefreshSocialMediaViews;
            }
        }

        private void RefreshAllPhoneDisplays(GameLanguage lang = GameLanguage.Turkish)
        {
            bool wasOpen = IsTabletOpen;
            int activeApp = 0;
            if (storeMgmtAppView != null && storeMgmtAppView.gameObject.activeSelf) activeApp = 1;
            else if (farmAppView != null && farmAppView.gameObject.activeSelf) activeApp = 2;
            else if (shoppingAppView != null && shoppingAppView.gameObject.activeSelf) activeApp = 3;
            else if (financeAppView != null && financeAppView.gameObject.activeSelf) activeApp = 4;
            else if (socialMediaAppView != null && socialMediaAppView.gameObject.activeSelf) activeApp = 5;
            else if (workshopsAppView != null && workshopsAppView.gameObject.activeSelf) activeApp = 6;
            else if (virtualMarketAppView != null && virtualMarketAppView.gameObject.activeSelf) activeApp = 7;

            int curFarmTab = activeFarmTab;
            int curStoreTab = activeTab;
            int curFinanceTab = activeFinanceTab;
            int curShoppingCat = activeShoppingCategory;
            int curSocialTab = activeSocialTab;
            int curWorkshopTab = activeWorkshopTab;
            int curOnlineMarketTab = activeOnlineMarketTab;

            if (tabletPopupRoot != null)
            {
                Destroy(tabletPopupRoot);
                tabletPopupRoot = null;
                homeScreenView = null;
                storeMgmtAppView = null;
                financeAppView = null;
                farmAppView = null;
                shoppingAppView = null;
                socialMediaAppView = null;
                workshopsAppView = null;
                workshopUpgradeContent = null;
                workshopManagementViewportObj = null;
                virtualMarketAppView = null;
                virtualMarketContent = null;
                virtualMarketViewportObj = null;
                onlineMarketFleetContent = null;
                onlineMarketStaffContent = null;
                onlineMarketCandidateContent = null;
                onlineMarketShiftContent = null;
                onlineMarketFleetViewportObj = null;
                onlineMarketStaffViewportObj = null;
                onlineMarketCandidateViewportObj = null;
                onlineMarketShiftViewportObj = null;
            }

            CreateBottomRightPhoneButtonOnCanvas();

            if (wasOpen)
            {
                OpenPhoneTablet();
                switch (activeApp)
                {
                    case 1:
                        ShowStoreManagementApp();
                        activeTab = curStoreTab;
                        RefreshStoreManagementViews();
                        break;
                    case 2:
                        ShowFarmApp();
                        activeFarmTab = curFarmTab;
                        RefreshFarmViews();
                        break;
                    case 3:
                        ShowShoppingApp();
                        activeShoppingCategory = curShoppingCat;
                        RefreshShoppingViews();
                        break;
                    case 4:
                        ShowFinanceApp();
                        activeFinanceTab = curFinanceTab;
                        RefreshFinanceViews();
                        break;
                    case 5:
                        ShowSocialMediaApp();
                        activeSocialTab = curSocialTab;
                        RefreshSocialMediaViews();
                        break;
                    case 6:
                        ShowWorkshopsApp();
                        activeWorkshopTab = curWorkshopTab;
                        RefreshWorkshopsViews();
                        break;
                    case 7:
                        ShowVirtualMarketApp();
                        activeOnlineMarketTab = curOnlineMarketTab;
                        RefreshVirtualMarketViews();
                        break;
                    default:
                        ShowHomeScreen();
                        break;
                }
            }
        }

        public void CreateBottomRightPhoneButtonOnCanvas(Transform parentCanvas = null)
        {
            if (phoneButtonObj != null) Destroy(phoneButtonObj);

            if (parentCanvas == null || parentCanvas.gameObject == null)
            {
                GameObject hudCanvas = GameObject.Find("Farm2Shelf_HUD_Canvas");
                if (hudCanvas != null)
                {
                    parentCanvas = hudCanvas.transform;
                }
                else
                {
                    Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    foreach (var c in allCanvases)
                    {
                        if (c != null && (c.name.Contains("HUD") || c.sortingOrder == 100))
                        {
                            parentCanvas = c.transform;
                            break;
                        }
                    }
                    if (parentCanvas == null && allCanvases.Length > 0)
                    {
                        parentCanvas = allCanvases[0].transform;
                    }
                }
            }

            if (parentCanvas == null) return;

            phoneButtonObj = new GameObject("EKT_PHONE_Tab_Button");
            phoneButtonObj.transform.SetParent(parentCanvas, false);

            RectTransform rect = phoneButtonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-35f, 35f);
            rect.sizeDelta = new Vector2(175f, 52f);

            Image bg = phoneButtonObj.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateOutlinePillSprite(175, 52, 26, 3, new Color(0.20f, 0.85f, 1.0f, 0.95f), new Color(0.10f, 0.14f, 0.20f, 0.90f));
            bg.raycastTarget = true;

            Button btn = phoneButtonObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(OnPhoneTabButtonClicked);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(phoneButtonObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text btnText = textObj.AddComponent<Text>();
            btnText.font = UIStyleUtility.GetGlobalFont(18);
            globalFont = btnText.font;

            btnText.text = LocalizationManager.L("Btn_EktPhone", "📱 EKT TABLET", "📱 EKT PHONE");
            btnText.fontSize = 20;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = new Color(0.35f, 0.92f, 1.0f);
            btnText.raycastTarget = false;
        }

        private void OnPhoneTabButtonClicked()
        {
            if (isAnimating) return;
            if (IsTabletOpen)
            {
                ClosePhoneTablet();
                return;
            }
            if (ModalManager.IsModalOpen)
            {
                if (!ModalManager.IsAnyModalCanvasActive())
                {
                    ModalManager.SetModalOpen(false);
                }
                else
                {
                    ModalManager.CloseModal();
                }
            }
            OpenPhoneTablet();
        }

        public void OpenPhoneTablet()
        {
            StopAllCoroutines();
            isAnimating = false;

            if (tabletPopupRoot == null)
            {
                CreateTabletModalUI();
            }

            if (tabletBoxRect != null)
            {
                tabletBoxRect.anchoredPosition = Vector2.zero;
                tabletBoxRect.localScale = Vector3.one;
            }

            ShowHomeScreen();
            tabletPopupRoot.SetActive(true);
            ModalManager.SetModalOpen(true);
            StartCoroutine(AnimateTabletOpen());
        }

        public void OpenTrendyShopApp()
        {
            OpenPhoneTablet();
            ShowShoppingApp();
        }

        public void ClosePhoneTablet()
        {
            if (isAnimating || tabletPopupRoot == null) return;
            StartCoroutine(AnimateTabletClose());
        }

        public void ClosePhoneTabletInstant()
        {
            StopAllCoroutines();
            isAnimating = false;
            if (tabletPopupRoot != null) tabletPopupRoot.SetActive(false);
            if (overlayImage != null) overlayImage.color = new Color(0f, 0f, 0f, 0f);
            ModalManager.SetModalOpen(false);
        }

        private IEnumerator AnimateTabletOpen()
        {
            isAnimating = true;

            float duration = 0.28f;
            float elapsed = 0f;

            Vector2 startPos = new Vector2(300f, -200f);
            Vector2 endPos = Vector2.zero;

            Vector3 startScale = new Vector3(0.2f, 0.2f, 1f);
            Vector3 endScale = Vector3.one;

            if (overlayImage != null) overlayImage.color = new Color(0f, 0f, 0f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (tabletBoxRect != null)
                {
                    tabletBoxRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                    tabletBoxRect.localScale = Vector3.Lerp(startScale, endScale, t);
                }
                if (overlayImage != null)
                {
                    overlayImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.75f, t));
                }

                yield return null;
            }

            if (tabletBoxRect != null)
            {
                tabletBoxRect.anchoredPosition = endPos;
                tabletBoxRect.localScale = endScale;
            }
            if (overlayImage != null)
            {
                overlayImage.color = new Color(0f, 0f, 0f, 0.75f);
            }

            isAnimating = false;
        }

        private IEnumerator AnimateTabletClose()
        {
            isAnimating = true;

            float duration = 0.22f;
            float elapsed = 0f;

            Vector2 startPos = Vector2.zero;
            Vector2 endPos = new Vector2(300f, -200f);

            Vector3 startScale = Vector3.one;
            Vector3 endScale = new Vector3(0.2f, 0.2f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (tabletBoxRect != null)
                {
                    tabletBoxRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                    tabletBoxRect.localScale = Vector3.Lerp(startScale, endScale, t);
                }
                if (overlayImage != null)
                {
                    overlayImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.75f, 0f, t));
                }

                yield return null;
            }

            if (tabletPopupRoot != null) tabletPopupRoot.SetActive(false);
            ModalManager.SetModalOpen(false);

            isAnimating = false;
        }

        private void CreateTabletModalUI()
        {
            if (tabletPopupRoot != null) Destroy(tabletPopupRoot);

            tabletPopupRoot = new GameObject("EKT_Phone_Tablet_Popup_Canvas");
            Canvas popCanvas = tabletPopupRoot.AddComponent<Canvas>();
            popCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            popCanvas.sortingOrder = 900;

            CanvasScaler scaler = tabletPopupRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            tabletPopupRoot.AddComponent<GraphicRaycaster>();

            // Karartma Arka Plan Katmanı (Dışına dokunulduğunda tableti kapatır ve arka plan tıklamalarını %100 bloke eder)
            GameObject overlayObj = new GameObject("Tablet_Backdrop_Overlay");
            overlayObj.transform.SetParent(tabletPopupRoot.transform, false);

            RectTransform rootRect = overlayObj.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.75f);
            overlayImage.raycastTarget = true; // TABLET AÇIKKEN ARKA PLANDAKİ HİÇBİR NESNEYE TIKLANAMASIN!

            Button overlayBtn = overlayObj.AddComponent<Button>();
            overlayBtn.targetGraphic = overlayImage;
            overlayBtn.onClick.AddListener(ClosePhoneTablet);

            GameObject tabletBox = new GameObject("Tablet_Device_Body");
            tabletBox.transform.SetParent(tabletPopupRoot.transform, false);

            tabletBoxRect = tabletBox.AddComponent<RectTransform>();
            tabletBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            tabletBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            tabletBoxRect.pivot = new Vector2(0.5f, 0.5f);
            tabletBoxRect.anchoredPosition = Vector2.zero;
            tabletBoxRect.sizeDelta = new Vector2(940f, 620f);
            tabletBoxRect.localScale = Vector3.one;

            Image deviceFrameBg = tabletBox.AddComponent<Image>();
            deviceFrameBg.sprite = UIStyleUtility.CreateOutlinePillSprite(940, 620, 36, 8, new Color(0.35f, 0.40f, 0.45f), new Color(0.12f, 0.14f, 0.17f, 0.98f));
            deviceFrameBg.raycastTarget = true; // Tablet kasasına tıklamalar arkaya sızmasın

            GameObject brandObj = new GameObject("Tablet_Brand_Header");
            brandObj.transform.SetParent(tabletBox.transform, false);

            RectTransform bRect = brandObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(0f, 285f);
            bRect.sizeDelta = new Vector2(300f, 35f);

            Text brandText = brandObj.AddComponent<Text>();
            brandText.font = globalFont;
            brandText.text = "EKT PHONE";
            brandText.fontSize = 24;
            brandText.fontStyle = FontStyle.Bold;
            brandText.alignment = TextAnchor.MiddleCenter;
            brandText.color = new Color(0.90f, 0.92f, 0.95f);
            brandText.raycastTarget = false;

            GameObject closeBtnObj = new GameObject("CloseButton_X");
            closeBtnObj.transform.SetParent(tabletBox.transform, false);

            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(430f, 282f);
            cRect.sizeDelta = new Vector2(46f, 46f);

            Image cBg = closeBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(46, 46, 23, new Color(0.92f, 0.18f, 0.20f, 1f));
            cBg.raycastTarget = true;

            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBg;
            cBtn.onClick.AddListener(ClosePhoneTablet);

            GameObject cxObj = new GameObject("X");
            cxObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cxRect = cxObj.AddComponent<RectTransform>();
            cxRect.anchorMin = Vector2.zero;
            cxRect.anchorMax = Vector2.one;

            Text cxText = cxObj.AddComponent<Text>();
            cxText.font = globalFont;
            cxText.text = "✖";
            cxText.fontSize = 26;
            cxText.fontStyle = FontStyle.Bold;
            cxText.alignment = TextAnchor.MiddleCenter;
            cxText.color = Color.white;
            cxText.raycastTarget = false;

            Outline cxOutline = cxObj.AddComponent<Outline>();
            cxOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            cxOutline.effectDistance = new Vector2(1.5f, -1.5f);

            tabletCloseButtonTransform = closeBtnObj.transform;

            GameObject screenObj = new GameObject("Tablet_Screen");
            screenObj.transform.SetParent(tabletBox.transform, false);

            RectTransform screenRect = screenObj.AddComponent<RectTransform>();
            screenRect.anchoredPosition = new Vector2(0f, -15f);
            screenRect.sizeDelta = new Vector2(890f, 540f);

            Image screenBg = screenObj.AddComponent<Image>();
            screenBg.sprite = UIStyleUtility.CreateRoundedPillSprite(890, 540, 16, new Color(0.08f, 0.10f, 0.14f, 0.98f));
            screenBg.raycastTarget = true; // Tablet ekranının boş alanlarına tıklamalar arkaya sızmasın

            CreateStatusBar(screenObj.transform);
            CreateHomeScreenView(screenObj.transform);
            CreateStoreManagementAppView(screenObj.transform);
            CreateFarmAppView(screenObj.transform);
            CreateShoppingAppView(screenObj.transform);
            CreateFinanceAppView(screenObj.transform);
            CreateSocialMediaAppView(screenObj.transform);
            CreateWorkshopsAppView(screenObj.transform);
            CreateVirtualMarketAppView(screenObj.transform);

            // Kırmızı X Kapat Butonunu KESİNLİKLE En Üst Katmana Çıkar
            EnsureCloseButtonOnTop();
        }

        private void CreateStatusBar(Transform parent)
        {
            GameObject barObj = new GameObject("OS_Status_Bar");
            barObj.transform.SetParent(parent, false);

            RectTransform bRect = barObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(0f, 245f);
            bRect.sizeDelta = new Vector2(850f, 30f);

            GameObject timeObj = new GameObject("Time");
            timeObj.transform.SetParent(barObj.transform, false);
            RectTransform tRect = timeObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(-380f, 0f);
            tRect.sizeDelta = new Vector2(120f, 30f);

            Text tText = timeObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = "06:00 AM";
            tText.fontSize = 17;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleLeft;
            tText.color = new Color(0.90f, 0.92f, 0.95f);
            tText.raycastTarget = false;

            GameObject statusRightObj = new GameObject("StatusRight");
            statusRightObj.transform.SetParent(barObj.transform, false);
            RectTransform rRect = statusRightObj.AddComponent<RectTransform>();
            rRect.anchoredPosition = new Vector2(350f, 0f);
            rRect.sizeDelta = new Vector2(150f, 30f);

            Text rText = statusRightObj.AddComponent<Text>();
            rText.font = globalFont;
            rText.text = "📶 5G   🔋 98%";
            rText.fontSize = 17;
            rText.fontStyle = FontStyle.Bold;
            rText.alignment = TextAnchor.MiddleRight;
            rText.color = new Color(0.90f, 0.92f, 0.95f);
            rText.raycastTarget = false;
        }

        private void CreateHomeScreenView(Transform parent)
        {
            GameObject viewObj = new GameObject("HomeScreenView");
            viewObj.transform.SetParent(parent, false);

            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;

            homeScreenView = viewObj.transform;

            GameObject appsContainer = new GameObject("Apps_Grid");
            appsContainer.transform.SetParent(viewObj.transform, false);

            RectTransform gridRect = appsContainer.AddComponent<RectTransform>();
            gridRect.anchoredPosition = new Vector2(0f, -20f);
            gridRect.sizeDelta = new Vector2(850f, 420f);

            GridLayoutGroup grid = appsContainer.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(152f, 150f);
            grid.spacing = new Vector2(16f, 20f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            string[] appNames = new string[] {
                LocalizationManager.L("App_StoreMgmt", "MAĞAZA YÖNETİMİ", "STORE MGMT"),
                LocalizationManager.L("App_Farm", "ÇİFTLİK", "FARM MGMT"),
                LocalizationManager.L("App_Shopping", "ALIŞVERİŞ", "EKT SHOPPING"),
                LocalizationManager.L("App_Finance", "FİNANS", "FINANCE"),
                LocalizationManager.L("App_SocialMedia", "SOSYAL MEDYA", "SOCIAL MEDIA"),
                LocalizationManager.L("App_Workshops", "ATÖLYELER", "WORKSHOPS"),
                LocalizationManager.L("App_OnlineMarket", "ONLİNE MARKET", "ONLINE MARKET")
            };
            string[] appIcons = new string[] { "🛒", "🌾", "🛍️", "💳", "𝕏", "🏭", "🌐" };
            Color[] appColors = new Color[] {
                new Color(0.20f, 0.70f, 0.95f),
                new Color(0.25f, 0.85f, 0.40f),
                new Color(0.95f, 0.40f, 0.55f),
                new Color(0.75f, 0.35f, 0.95f),
                new Color(0.12f, 0.65f, 0.95f),
                new Color(0.95f, 0.60f, 0.15f),
                new Color(0.00f, 0.85f, 0.65f)
            };

            for (int i = 0; i < 7; i++)
            {
                GameObject appObj = new GameObject("App_" + appNames[i]);
                appObj.transform.SetParent(appsContainer.transform, false);

                Image appBg = appObj.AddComponent<Image>();
                appBg.sprite = UIStyleUtility.CreateOutlinePillSprite(152, 150, 20, 2, appColors[i], new Color(0.12f, 0.15f, 0.20f, 0.85f));
                appBg.raycastTarget = true;

                Button btn = appObj.AddComponent<Button>();
                btn.targetGraphic = appBg;
                int appIndex = i;
                btn.onClick.AddListener(() => {
                    if (TutorialManager.Instance != null) TutorialManager.Instance.NotifyAppOpened(appIndex);
                    if (appIndex == 0) ShowStoreManagementApp();
                    else if (appIndex == 1) ShowFarmApp();
                    else if (appIndex == 2) ShowShoppingApp();
                    else if (appIndex == 3) ShowFinanceApp();
                    else if (appIndex == 4) ShowSocialMediaApp();
                    else if (appIndex == 5) ShowWorkshopsApp();
                    else if (appIndex == 6) ShowVirtualMarketApp();
                });

                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(appObj.transform, false);
                RectTransform iRect = iconObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(0f, 18f);
                iRect.sizeDelta = new Vector2(100f, 55f);

                Text iText = iconObj.AddComponent<Text>();
                iText.font = globalFont;
                iText.text = appIcons[i];
                iText.fontSize = 48;
                iText.alignment = TextAnchor.MiddleCenter;
                iText.color = Color.white;
                iText.raycastTarget = false;

                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(appObj.transform, false);
                RectTransform lRect = labelObj.AddComponent<RectTransform>();
                lRect.anchoredPosition = new Vector2(0f, -42f);
                lRect.sizeDelta = new Vector2(148f, 32f);

                Text lText = labelObj.AddComponent<Text>();
                lText.font = globalFont;
                lText.text = appNames[i];
                lText.fontSize = (appIndex == 0) ? 15 : 17;
                lText.fontStyle = FontStyle.Bold;
                lText.alignment = TextAnchor.MiddleCenter;
                lText.color = appColors[i];
                lText.raycastTarget = false;
            }
        }

        private void CreateStoreManagementAppView(Transform parent)
        {
            GameObject viewObj = new GameObject("StoreManagementAppView");
            viewObj.transform.SetParent(parent, false);

            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;

            storeMgmtAppView = viewObj.transform;

            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(viewObj.transform, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 205f);
            hRect.sizeDelta = new Vector2(850f, 40f);

            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform bRect = backBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(-360f, 0f);
            bRect.sizeDelta = new Vector2(130f, 36f);

            Image bBg = backBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(130, 36, 18, new Color(0.20f, 0.25f, 0.32f, 0.90f));
            bBg.raycastTarget = true;

            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;
            bBtn.onClick.AddListener(ShowHomeScreen);

            GameObject bTextObj = new GameObject("Text");
            bTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform btRect = bTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;

            Text bText = bTextObj.AddComponent<Text>();
            bText.font = globalFont;
            bText.text = LocalizationManager.L("Btn_HomeScreen", "← Ana Ekran", "← Home Screen");
            bText.fontSize = 18;
            bText.fontStyle = FontStyle.Bold;
            bText.alignment = TextAnchor.MiddleCenter;
            bText.color = new Color(0.35f, 0.85f, 1.0f);
            bText.raycastTarget = false;

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 0f);
            tRect.sizeDelta = new Vector2(400f, 40f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = LocalizationManager.L("Header_StoreMgmt", "🛒 MAĞAZA YÖNETİMİ", "🛒 STORE MANAGEMENT");
            tText.fontSize = 24;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(1.0f, 0.85f, 0.25f);
            tText.raycastTarget = false;

            CreateStoreMgmtTabs(viewObj.transform);

            upgradeListContent = CreateScrollableViewContainer(viewObj.transform, "UpgradeList", new Vector2(0f, -50f), new Vector2(850f, 350f), out upgradeViewportObj);
            staffListContent = CreateScrollableViewContainer(viewObj.transform, "StaffList", new Vector2(0f, -50f), new Vector2(850f, 350f), out staffViewportObj);
            candidateListContent = CreateScrollableViewContainer(viewObj.transform, "CandidateList", new Vector2(0f, -50f), new Vector2(850f, 350f), out candidateViewportObj);
            shiftListContent = CreateScrollableViewContainer(viewObj.transform, "ShiftList", new Vector2(0f, -50f), new Vector2(850f, 350f), out shiftViewportObj);

            VerticalLayoutGroup uLayout = upgradeListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            uLayout.spacing = 15f;
            uLayout.childControlWidth = true;
            uLayout.childControlHeight = false;

            VerticalLayoutGroup sLayout = staffListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            sLayout.spacing = 10f;
            sLayout.childControlWidth = true;
            sLayout.childControlHeight = false;

            GridLayoutGroup cGrid = candidateListContent.gameObject.AddComponent<GridLayoutGroup>();
            cGrid.cellSize = new Vector2(265f, 160f);
            cGrid.spacing = new Vector2(15f, 15f);
            cGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cGrid.constraintCount = 3;

            VerticalLayoutGroup shiftLayout = shiftListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            shiftLayout.spacing = 10f;
            shiftLayout.childControlWidth = true;
            shiftLayout.childControlHeight = false;

            viewObj.SetActive(false);
        }

        private void CreateStoreMgmtTabs(Transform parent)
        {
            GameObject tabsObj = new GameObject("MgmtTabs");
            tabsObj.transform.SetParent(parent, false);

            RectTransform tRect = tabsObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 170f);
            tRect.sizeDelta = new Vector2(850f, 40f);

            HorizontalLayoutGroup layout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleCenter;

            string[] tabs = new string[] {
                LocalizationManager.L("Tab_UpgradeStore", "🏢 Marketi Geliştir", "🏢 Upgrade Store"),
                LocalizationManager.L("Tab_StaffList", "👥 Personel Kadrosu", "👥 Staff List"),
                LocalizationManager.L("Tab_HireStaff", "➕ İşe Alım", "➕ Hire Staff"),
                LocalizationManager.L("Tab_Shifts", "⏰ Vardiyalar", "⏰ Shifts")
            };

            for (int i = 0; i < 4; i++)
            {
                int tabIndex = i;
                GameObject tabBtn = new GameObject("Tab_" + i);
                tabBtn.transform.SetParent(tabsObj.transform, false);

                RectTransform tabRect = tabBtn.AddComponent<RectTransform>();
                tabRect.sizeDelta = new Vector2(195f, 40f);

                Image tabBg = tabBtn.AddComponent<Image>();
                tabBg.raycastTarget = true;
                storeTabBtnImgs[i] = tabBg;

                Button btn = tabBtn.AddComponent<Button>();
                btn.targetGraphic = tabBg;
                btn.onClick.AddListener(() => {
                    activeTab = tabIndex;
                    RefreshStoreManagementViews();
                });

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(tabBtn.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;

                Text tabText = textObj.AddComponent<Text>();
                tabText.font = globalFont;
                tabText.text = tabs[i];
                tabText.fontSize = 18;
                tabText.fontStyle = FontStyle.Bold;
                tabText.alignment = TextAnchor.MiddleCenter;
                tabText.raycastTarget = false;
                storeTabBtnTexts[i] = tabText;
            }

            UpdateStoreTabVisuals();
        }

        private void UpdateStoreTabVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                if (storeTabBtnImgs[i] == null) continue;
                bool isActive = (activeTab == i);
                if (isActive)
                {
                    storeTabBtnImgs[i].sprite = UIStyleUtility.CreateOutlinePillSprite(195, 40, 20, 2, new Color(1.0f, 0.75f, 0.20f), new Color(0.24f, 0.18f, 0.08f, 0.95f));
                    if (storeTabBtnTexts[i] != null) storeTabBtnTexts[i].color = new Color(1.0f, 0.88f, 0.35f);
                }
                else
                {
                    storeTabBtnImgs[i].sprite = UIStyleUtility.CreateOutlinePillSprite(195, 40, 20, 1, new Color(0.20f, 0.35f, 0.50f, 0.65f), new Color(0.10f, 0.14f, 0.18f, 0.85f));
                    if (storeTabBtnTexts[i] != null) storeTabBtnTexts[i].color = new Color(0.55f, 0.70f, 0.85f);
                }
            }
        }

        private void CreateFinanceAppView(Transform parent)
        {
            GameObject viewObj = new GameObject("FinanceAppView");
            viewObj.transform.SetParent(parent, false);

            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;

            financeAppView = viewObj.transform;

            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(viewObj.transform, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 212f);
            hRect.sizeDelta = new Vector2(850f, 36f);

            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform bRect = backBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(-360f, 0f);
            bRect.sizeDelta = new Vector2(130f, 34f);

            Image bBg = backBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(130, 34, 17, new Color(0.20f, 0.25f, 0.32f, 0.90f));
            bBg.raycastTarget = true;

            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;
            bBtn.onClick.AddListener(ShowHomeScreen);

            GameObject bTextObj = new GameObject("Text");
            bTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform btRect = bTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;

            Text bText = bTextObj.AddComponent<Text>();
            bText.font = globalFont;
            bText.text = LocalizationManager.L("Btn_HomeScreen", "← Ana Ekran", "← Home Screen");
            bText.fontSize = 16;
            bText.fontStyle = FontStyle.Bold;
            bText.alignment = TextAnchor.MiddleCenter;
            bText.color = new Color(0.35f, 0.85f, 1.0f);
            bText.raycastTarget = false;

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 0f);
            tRect.sizeDelta = new Vector2(450f, 36f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = LocalizationManager.L("App_FinanceHeader", "💳 FİNANS VE GELİR GİDER", "💳 FINANCE & EARNINGS");
            tText.fontSize = 22;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.75f, 0.35f, 0.95f);
            tText.raycastTarget = false;

            // Sekme Butonları (Y = 168f)
            CreateFinanceTabs(viewObj.transform);

            // Sabit Üst Kontrol Barı (Arama Çubuğu + Otomatik Fiyat Ayarla Butonu, Y = 118f)
            GameObject financeProductsControlBarObj = new GameObject("FinanceProductsControlBar");
            financeProductsControlBarObj.transform.SetParent(viewObj.transform, false);

            RectTransform fpcRect = financeProductsControlBarObj.AddComponent<RectTransform>();
            fpcRect.anchoredPosition = new Vector2(0f, 118f);
            fpcRect.sizeDelta = new Vector2(850f, 38f);
            financeProductsControlBar = financeProductsControlBarObj.transform;

            // Arama Çubuğu (Sol Taraf)
            GameObject searchBoxObj = new GameObject("FinanceSearchInputBox");
            searchBoxObj.transform.SetParent(financeProductsControlBarObj.transform, false);
            RectTransform sbRect = searchBoxObj.AddComponent<RectTransform>();
            sbRect.anchoredPosition = new Vector2(-160f, 0f);
            sbRect.sizeDelta = new Vector2(460f, 38f);

            Image sbBg = searchBoxObj.AddComponent<Image>();
            sbBg.sprite = UIStyleUtility.CreateOutlinePillSprite(460, 38, 19, 1, new Color(0.75f, 0.35f, 0.95f), new Color(0.14f, 0.16f, 0.22f, 0.95f));

            InputField searchInput = searchBoxObj.AddComponent<InputField>();

            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(searchBoxObj.transform, false);
            RectTransform phRect = phObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(14f, 0f);
            phRect.offsetMax = new Vector2(-14f, 0f);

            Text phText = phObj.AddComponent<Text>();
            phText.font = globalFont;
            phText.text = LocalizationManager.L("Placeholder_FinanceSearch", "🔍 Ürün İsmi veya Kategori Ara...", "🔍 Search Product Name or Category...");
            phText.fontSize = 16;
            phText.fontStyle = FontStyle.Italic;
            phText.alignment = TextAnchor.MiddleLeft;
            phText.color = new Color(0.65f, 0.70f, 0.75f);
            searchInput.placeholder = phText;

            GameObject inTextObj = new GameObject("Text");
            inTextObj.transform.SetParent(searchBoxObj.transform, false);
            RectTransform inRect = inTextObj.AddComponent<RectTransform>();
            inRect.anchorMin = Vector2.zero;
            inRect.anchorMax = Vector2.one;
            inRect.offsetMin = new Vector2(14f, 0f);
            inRect.offsetMax = new Vector2(-14f, 0f);

            Text inText = inTextObj.AddComponent<Text>();
            inText.font = globalFont;
            inText.fontSize = 16;
            inText.fontStyle = FontStyle.Bold;
            inText.alignment = TextAnchor.MiddleLeft;
            inText.color = Color.white;
            searchInput.textComponent = inText;

            searchInput.onValueChanged.AddListener((val) => {
                currentFinanceProductSearchQuery = val;
                RenderFinanceProductsList();
            });

            // Otomatik Fiyat Ayarla Butonu (Sağ Taraf)
            GameObject autoPriceBtnObj = new GameObject("AutoPriceButton");
            autoPriceBtnObj.transform.SetParent(financeProductsControlBarObj.transform, false);

            RectTransform apRect = autoPriceBtnObj.AddComponent<RectTransform>();
            apRect.anchoredPosition = new Vector2(255f, 0f);
            apRect.sizeDelta = new Vector2(310f, 38f);

            Image apBg = autoPriceBtnObj.AddComponent<Image>();
            apBg.sprite = UIStyleUtility.CreateRoundedPillSprite(310, 38, 19, new Color(0.20f, 0.70f, 0.45f));
            apBg.raycastTarget = true;

            Button apBtn = autoPriceBtnObj.AddComponent<Button>();
            apBtn.targetGraphic = apBg;
            apBtn.onClick.AddListener(() => {
                WholesaleDatabase.ResetAllPricesToDefault();
                RenderFinanceProductsList();
                string autoTitle = LocalizationManager.L("Modal_PricesUpdated_Title", "Fiyatlar Güncellendi! ⚡", "Prices Updated! ⚡");
                string autoBody = LocalizationManager.L("Modal_PricesUpdated_Body", "Tüm ürünlerin satış fiyatı varsayılan %20 kâr marjına ayarlandı.", "All product sale prices reset to default 20% profit margin.");
                string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
                ModalManager.ShowModal(autoTitle, autoBody, btnOk);
            });

            GameObject aptObj = new GameObject("Text");
            aptObj.transform.SetParent(autoPriceBtnObj.transform, false);
            RectTransform aptRect = aptObj.AddComponent<RectTransform>();
            aptRect.anchorMin = Vector2.zero;
            aptRect.anchorMax = Vector2.one;

            Text apText = aptObj.AddComponent<Text>();
            apText.font = globalFont;
            apText.text = LocalizationManager.L("Btn_AutoPricing", "⚡ Otomatik Fiyatlandırma (%20 Kâr)", "⚡ Auto Pricing (+20% Profit)");
            apText.fontSize = 15;
            apText.fontStyle = FontStyle.Bold;
            apText.alignment = TextAnchor.MiddleCenter;
            apText.color = Color.white;
            apText.raycastTarget = false;

            // Scroll Viewport ve Content Kapları
            financeProductsContent = CreateScrollableViewContainer(viewObj.transform, "FinanceProducts", new Vector2(0f, -80f), new Vector2(850f, 330f), out financeProductsViewportObj);
            financeSummaryContent = CreateScrollableViewContainer(viewObj.transform, "FinanceSummary", new Vector2(0f, -60f), new Vector2(850f, 370f), out financeSummaryViewportObj);
            financeHistoryContent = CreateScrollableViewContainer(viewObj.transform, "FinanceHistory", new Vector2(0f, -60f), new Vector2(850f, 370f), out financeHistoryViewportObj);
            financeLoansContent = CreateScrollableViewContainer(viewObj.transform, "FinanceLoans", new Vector2(0f, -60f), new Vector2(850f, 370f), out financeLoansViewportObj);
            financeStocksContent = CreateScrollableViewContainer(viewObj.transform, "FinanceStocks", new Vector2(0f, -60f), new Vector2(850f, 370f), out financeStocksViewportObj);

            VerticalLayoutGroup productsLayout = financeProductsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            productsLayout.spacing = 10f;
            productsLayout.childControlWidth = true;
            productsLayout.childControlHeight = false;

            VerticalLayoutGroup summaryLayout = financeSummaryContent.gameObject.AddComponent<VerticalLayoutGroup>();
            summaryLayout.spacing = 15f;
            summaryLayout.childControlWidth = true;
            summaryLayout.childControlHeight = false;

            VerticalLayoutGroup historyLayout = financeHistoryContent.gameObject.AddComponent<VerticalLayoutGroup>();
            historyLayout.spacing = 8f;
            historyLayout.childControlWidth = true;
            historyLayout.childControlHeight = false;

            VerticalLayoutGroup loansLayout = financeLoansContent.gameObject.AddComponent<VerticalLayoutGroup>();
            loansLayout.spacing = 12f;
            loansLayout.childControlWidth = true;
            loansLayout.childControlHeight = false;

            viewObj.SetActive(false);
        }

        private void CreateFinanceTabs(Transform parent)
        {
            GameObject tabsObj = new GameObject("FinanceTabs");
            tabsObj.transform.SetParent(parent, false);

            RectTransform tRect = tabsObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 168f);
            tRect.sizeDelta = new Vector2(850f, 38f);

            HorizontalLayoutGroup layout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleCenter;

            string[] tabs = new string[] {
                LocalizationManager.L("Tab_Products", "🏷️ Ürünler", "🏷️ Products"),
                LocalizationManager.L("Tab_Summary", "📊 Özet", "📊 Summary"),
                LocalizationManager.L("Tab_History", "📜 İşlem Geçmişi", "📜 History"),
                LocalizationManager.L("Tab_Loans", "🏛️ Krediler", "🏛️ Bank Loans"),
                LocalizationManager.L("Tab_Stocks", "📈 Borsa & Hisse", "📈 Stock Market")
            };

            for (int i = 0; i < 5; i++)
            {
                int tabIndex = i;
                GameObject tabBtn = new GameObject("FinanceTab_" + i);
                tabBtn.transform.SetParent(tabsObj.transform, false);

                RectTransform tabRect = tabBtn.AddComponent<RectTransform>();
                tabRect.sizeDelta = new Vector2(164f, 38f);

                Image tabBg = tabBtn.AddComponent<Image>();
                tabBg.sprite = UIStyleUtility.CreateOutlinePillSprite(164, 38, 18, 2, new Color(0.75f, 0.35f, 0.95f), new Color(0.12f, 0.16f, 0.22f, 0.85f));
                tabBg.raycastTarget = true;

                Button btn = tabBtn.AddComponent<Button>();
                btn.targetGraphic = tabBg;
                btn.onClick.AddListener(() => {
                    activeFinanceTab = tabIndex;
                    RefreshFinanceViews();
                });

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(tabBtn.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;

                Text tabText = textObj.AddComponent<Text>();
                tabText.font = globalFont;
                tabText.text = tabs[i];
                tabText.fontSize = 15;
                tabText.fontStyle = FontStyle.Bold;
                tabText.alignment = TextAnchor.MiddleCenter;
                tabText.color = new Color(0.85f, 0.55f, 1.0f);
                tabText.horizontalOverflow = HorizontalWrapMode.Overflow;
                tabText.raycastTarget = false;
            }
        }

        private Transform CreateScrollableViewContainer(Transform parent, string name, Vector2 pos, Vector2 size, out Transform viewportTransform)
        {
            GameObject viewportObj = new GameObject(name + "_Viewport");
            viewportObj.transform.SetParent(parent, false);

            RectTransform vRect = viewportObj.AddComponent<RectTransform>();
            vRect.anchoredPosition = pos;
            vRect.sizeDelta = size;

            // Arka planda dokunmatik ve fare sürüklemesi (Touch & PC Drag) için şeffaf grafik ekle
            Image vBg = viewportObj.AddComponent<Image>();
            vBg.color = new Color(0.05f, 0.08f, 0.12f, 0.01f);
            vBg.raycastTarget = true;

            viewportObj.AddComponent<RectMask2D>();

            ScrollRect scrollRect = viewportObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 50f;

            GameObject contentObj = new GameObject(name + "_Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            RectTransform cRect = contentObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.anchoredPosition = Vector2.zero;
            cRect.sizeDelta = new Vector2(0f, 0f);

            scrollRect.content = cRect;
            scrollRect.viewport = vRect;

            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            viewportTransform = viewportObj.transform;
            return contentObj.transform;
        }

        private void CreateShoppingAppView(Transform parent)
        {
            GameObject viewObj = new GameObject("ShoppingAppView");
            viewObj.transform.SetParent(parent, false);

            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;

            shoppingAppView = viewObj.transform;

            // Header Bar
            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(viewObj.transform, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 205f);
            hRect.sizeDelta = new Vector2(850f, 40f);

            // Geri Butonu
            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform bRect = backBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(-360f, 0f);
            bRect.sizeDelta = new Vector2(130f, 36f);

            Image bBg = backBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(130, 36, 18, new Color(0.20f, 0.25f, 0.32f, 0.90f));
            bBg.raycastTarget = true;

            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;
            bBtn.onClick.AddListener(ShowHomeScreen);

            GameObject bTextObj = new GameObject("Text");
            bTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform btRect = bTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;

            Text bText = bTextObj.AddComponent<Text>();
            bText.font = globalFont;
            bText.text = LocalizationManager.L("Btn_HomeScreen", "← Ana Ekran", "← Home Screen");
            bText.fontSize = 18;
            bText.fontStyle = FontStyle.Bold;
            bText.alignment = TextAnchor.MiddleCenter;
            bText.color = new Color(0.95f, 0.40f, 0.55f);
            bText.raycastTarget = false;

            // Başlık
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(-210f, 0f);
            tRect.sizeDelta = new Vector2(150f, 40f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = "🛍️ TRENDYSHOP";
            tText.fontSize = 22;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleLeft;
            tText.color = new Color(0.95f, 0.45f, 0.60f);
            tText.raycastTarget = false;

            // Toplu Sipariş Butonu (Arama Çubuğunun Sol Tarafında)
            GameObject bulkOrderBtnObj = new GameObject("BulkOrderButton");
            bulkOrderBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform boRect = bulkOrderBtnObj.AddComponent<RectTransform>();
            boRect.anchoredPosition = new Vector2(5f, 0f);
            boRect.sizeDelta = new Vector2(140f, 36f);

            Image boBg = bulkOrderBtnObj.AddComponent<Image>();
            boBg.sprite = UIStyleUtility.CreateRoundedPillSprite(140, 36, 18, new Color(0.18f, 0.68f, 0.45f));
            boBg.raycastTarget = true;

            Button boBtn = bulkOrderBtnObj.AddComponent<Button>();
            boBtn.targetGraphic = boBg;
            boBtn.onClick.AddListener(OnBulkOrderButtonClicked);

            GameObject boTxtObj = new GameObject("Text");
            boTxtObj.transform.SetParent(bulkOrderBtnObj.transform, false);
            RectTransform botRect = boTxtObj.AddComponent<RectTransform>();
            botRect.anchorMin = Vector2.zero;
            botRect.anchorMax = Vector2.one;

            Text boText = boTxtObj.AddComponent<Text>();
            boText.font = globalFont;
            boText.text = LocalizationManager.L("Btn_BulkOrder", "📦 Toplu Sipariş", "📦 Bulk Order");
            boText.fontSize = 16;
            boText.fontStyle = FontStyle.Bold;
            boText.alignment = TextAnchor.MiddleCenter;
            boText.color = Color.white;
            boText.raycastTarget = false;

            // Ürün Arama Çubuğu (Sepet Butonunun Sol Tarafında)
            GameObject searchBoxObj = new GameObject("SearchInputBox");
            searchBoxObj.transform.SetParent(headerObj.transform, false);
            RectTransform sbRect = searchBoxObj.AddComponent<RectTransform>();
            sbRect.anchoredPosition = new Vector2(190f, 0f);
            sbRect.sizeDelta = new Vector2(190f, 36f);

            Image sbBg = searchBoxObj.AddComponent<Image>();
            sbBg.sprite = UIStyleUtility.CreateOutlinePillSprite(190, 36, 18, 1, new Color(0.95f, 0.40f, 0.55f), new Color(0.14f, 0.16f, 0.22f, 0.95f));

            shoppingSearchInputField = searchBoxObj.AddComponent<InputField>();

            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(searchBoxObj.transform, false);
            RectTransform phRect = phObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(12f, 0f);
            phRect.offsetMax = new Vector2(-12f, 0f);

            Text phText = phObj.AddComponent<Text>();
            phText.font = globalFont;
            phText.text = LocalizationManager.L("Placeholder_SearchProduct", "🔍 Ürün Ara...", "🔍 Search Product...");
            phText.fontSize = 16;
            phText.fontStyle = FontStyle.Italic;
            phText.alignment = TextAnchor.MiddleLeft;
            phText.color = new Color(0.65f, 0.70f, 0.75f);
            shoppingSearchInputField.placeholder = phText;

            GameObject inTextObj = new GameObject("Text");
            inTextObj.transform.SetParent(searchBoxObj.transform, false);
            RectTransform inRect = inTextObj.AddComponent<RectTransform>();
            inRect.anchorMin = Vector2.zero;
            inRect.anchorMax = Vector2.one;
            inRect.offsetMin = new Vector2(12f, 0f);
            inRect.offsetMax = new Vector2(-12f, 0f);

            Text inText = inTextObj.AddComponent<Text>();
            inText.font = globalFont;
            inText.fontSize = 16;
            inText.fontStyle = FontStyle.Bold;
            inText.alignment = TextAnchor.MiddleLeft;
            inText.color = Color.white;
            shoppingSearchInputField.textComponent = inText;

            shoppingSearchInputField.onValueChanged.AddListener((val) => {
                currentShoppingSearchQuery = val;
                RenderShoppingCategoryContent();
            });

            // Sepet Butonu (Sağ Üst)
            GameObject cartBtnObj = new GameObject("CartButton");
            cartBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform cRect = cartBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(355f, 0f);
            cRect.sizeDelta = new Vector2(115f, 36f);

            Image cBg = cartBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(115, 36, 18, new Color(0.95f, 0.40f, 0.55f));
            cBg.raycastTarget = true;

            Button cBtn = cartBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBg;
            cBtn.onClick.AddListener(() => {
                OpenCartModal();
            });

            GameObject ctObj = new GameObject("Text");
            ctObj.transform.SetParent(cartBtnObj.transform, false);
            RectTransform ctRect = ctObj.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;

            headerCartButtonText = ctObj.AddComponent<Text>();
            headerCartButtonText.font = globalFont;
            headerCartButtonText.text = "🛒 SEPET (0)";
            headerCartButtonText.fontSize = 17;
            headerCartButtonText.fontStyle = FontStyle.Bold;
            headerCartButtonText.alignment = TextAnchor.MiddleCenter;
            headerCartButtonText.color = Color.white;
            headerCartButtonText.raycastTarget = false;

            // SOL DİKEY KATEGORİ MENÜSÜ (ScrollRect)
            shoppingCategoryContent = CreateScrollableViewContainer(viewObj.transform, "ShopCategoryList", new Vector2(-305f, -30f), new Vector2(230f, 430f), out shoppingCategoryViewportObj);

            VerticalLayoutGroup catLayout = shoppingCategoryContent.gameObject.AddComponent<VerticalLayoutGroup>();
            catLayout.spacing = 8f;
            catLayout.childControlWidth = true;
            catLayout.childControlHeight = false;

            // SAĞ ANA İÇERİK ALANI (Placeholder)
            GameObject mainPanel = new GameObject("Shopping_Main_Panel");
            mainPanel.transform.SetParent(viewObj.transform, false);

            RectTransform mRect = mainPanel.AddComponent<RectTransform>();
            mRect.anchoredPosition = new Vector2(115f, -30f);
            mRect.sizeDelta = new Vector2(560f, 430f);

            Image mBg = mainPanel.AddComponent<Image>();
            mBg.sprite = UIStyleUtility.CreateOutlinePillSprite(560, 430, 16, 2, new Color(0.95f, 0.40f, 0.55f), new Color(0.12f, 0.15f, 0.20f, 0.95f));
            mBg.raycastTarget = false;

            shoppingMainContentArea = mainPanel.transform;

            // Kategori İçerik Başlığı
            GameObject headerTitleObj = new GameObject("CategoryHeaderTitle");
            headerTitleObj.transform.SetParent(mainPanel.transform, false);
            RectTransform htRect = headerTitleObj.AddComponent<RectTransform>();
            htRect.anchoredPosition = new Vector2(0f, 175f);
            htRect.sizeDelta = new Vector2(520f, 40f);

            shoppingCategoryHeaderTitle = headerTitleObj.AddComponent<Text>();
            shoppingCategoryHeaderTitle.font = globalFont;
            shoppingCategoryHeaderTitle.text = "🛋️ Mobilyalar";
            shoppingCategoryHeaderTitle.fontSize = 26;
            shoppingCategoryHeaderTitle.fontStyle = FontStyle.Bold;
            shoppingCategoryHeaderTitle.alignment = TextAnchor.MiddleLeft;
            shoppingCategoryHeaderTitle.color = new Color(0.95f, 0.45f, 0.60f);

            // Mobilyalar İçin Scrollable Liste Alanı
            furnitureListContent = CreateScrollableViewContainer(mainPanel.transform, "FurnitureList", new Vector2(0f, -15f), new Vector2(530f, 310f), out furnitureViewportObj);
            VerticalLayoutGroup furnLayout = furnitureListContent.gameObject.AddComponent<VerticalLayoutGroup>();
            furnLayout.spacing = 8f;
            furnLayout.childControlWidth = true;
            furnLayout.childControlHeight = false;

            // Kategori Alt Bilgi / Placeholder İçerik
            GameObject headerSubObj = new GameObject("CategoryHeaderSub");
            headerSubObj.transform.SetParent(mainPanel.transform, false);
            RectTransform hsRect = headerSubObj.AddComponent<RectTransform>();
            hsRect.anchoredPosition = new Vector2(0f, 0f);
            hsRect.sizeDelta = new Vector2(520f, 280f);

            shoppingCategoryHeaderSub = headerSubObj.AddComponent<Text>();
            shoppingCategoryHeaderSub.font = globalFont;
            shoppingCategoryHeaderSub.text = "🛋️ MOBİLYALAR KATALOĞU\n\nBu kategorinin içeriği henüz boş.\nBirlikte eklemek istediğiniz ürünleri belirleyebilirsiniz.";
            shoppingCategoryHeaderSub.fontSize = 18;
            shoppingCategoryHeaderSub.fontStyle = FontStyle.Normal;
            shoppingCategoryHeaderSub.alignment = TextAnchor.MiddleCenter;
            shoppingCategoryHeaderSub.color = new Color(0.85f, 0.90f, 0.95f);

            // ALT SEPET BARI (Cart Summary Bar)
            GameObject cartPanel = new GameObject("Cart_Summary_Panel");
            shoppingCartSummaryPanelObj = cartPanel;
            cartPanel.transform.SetParent(mainPanel.transform, false);
            RectTransform cartPanelRect = cartPanel.AddComponent<RectTransform>();
            cartPanelRect.anchoredPosition = new Vector2(0f, -182f);
            cartPanelRect.sizeDelta = new Vector2(530f, 48f);

            Image cartBg = cartPanel.AddComponent<Image>();
            cartBg.sprite = UIStyleUtility.CreateOutlinePillSprite(530, 48, 12, 2, new Color(0.95f, 0.40f, 0.55f), new Color(0.16f, 0.18f, 0.24f, 0.95f));

            GameObject cartTextObj = new GameObject("CartSummaryText");
            cartTextObj.transform.SetParent(cartPanel.transform, false);
            RectTransform cartTextRect = cartTextObj.AddComponent<RectTransform>();
            cartTextRect.anchoredPosition = new Vector2(-70f, 0f);
            cartTextRect.sizeDelta = new Vector2(340f, 40f);

            shoppingCartSummaryText = cartTextObj.AddComponent<Text>();
            shoppingCartSummaryText.font = globalFont;
            shoppingCartSummaryText.text = "🛒 Sepet: 0 Ürün (0C)";
            shoppingCartSummaryText.fontSize = 18;
            shoppingCartSummaryText.fontStyle = FontStyle.Bold;
            shoppingCartSummaryText.alignment = TextAnchor.MiddleLeft;
            shoppingCartSummaryText.color = Color.white;

            // Siparişi Tamamla Butonu
            GameObject checkoutBtnObj = new GameObject("CheckoutBtn");
            checkoutBtnObj.transform.SetParent(cartPanel.transform, false);
            RectTransform chRect = checkoutBtnObj.AddComponent<RectTransform>();
            chRect.anchoredPosition = new Vector2(185f, 0f);
            chRect.sizeDelta = new Vector2(130f, 36f);

            Image chBg = checkoutBtnObj.AddComponent<Image>();
            chBg.sprite = UIStyleUtility.CreateRoundedPillSprite(130, 36, 10, new Color(0.20f, 0.75f, 0.35f));

            checkoutCartButton = checkoutBtnObj.AddComponent<Button>();
            checkoutCartButton.targetGraphic = chBg;
            checkoutCartButton.onClick.AddListener(() => {
                OpenCartModal();
            });

            string checkoutLabel = LocalizationManager.L("Btn_Checkout", "Siparişi Tamamla", "Checkout Order");
            Text chTxt = CreateTextInPanel(checkoutBtnObj.transform, Vector2.zero, Vector2.one, checkoutLabel, 17, Color.white);
            chTxt.alignment = TextAnchor.MiddleCenter;

            viewObj.SetActive(false);
        }

        private void ShowHomeScreen()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(true);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(false);
            if (workshopsAppView != null) workshopsAppView.gameObject.SetActive(false);
            if (virtualMarketAppView != null) virtualMarketAppView.gameObject.SetActive(false);
            EnsureCloseButtonOnTop();
        }

        private void ShowStoreManagementApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(false);
            if (workshopsAppView != null) workshopsAppView.gameObject.SetActive(false);
            if (virtualMarketAppView != null) virtualMarketAppView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(true);

            activeTab = 0;
            RefreshStoreManagementViews();
            EnsureCloseButtonOnTop();
        }

        private void ShowFinanceApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(false);
            if (workshopsAppView != null) workshopsAppView.gameObject.SetActive(false);
            if (virtualMarketAppView != null) virtualMarketAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(true);

            activeFinanceTab = 0;
            RefreshFinanceViews();
            EnsureCloseButtonOnTop();
        }

        private void ShowFarmApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(false);
            if (workshopsAppView != null) workshopsAppView.gameObject.SetActive(false);
            if (virtualMarketAppView != null) virtualMarketAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(true);

            activeFarmTab = 0;
            RefreshFarmViews();
            EnsureCloseButtonOnTop();
        }

        private void ShowShoppingApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(false);
            if (workshopsAppView != null) workshopsAppView.gameObject.SetActive(false);
            if (virtualMarketAppView != null) virtualMarketAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(true);

            activeShoppingCategory = 0;
            RefreshShoppingViews();
            EnsureCloseButtonOnTop();
        }

        private void ShowSocialMediaApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (workshopsAppView != null) workshopsAppView.gameObject.SetActive(false);
            if (virtualMarketAppView != null) virtualMarketAppView.gameObject.SetActive(false);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(true);

            activeSocialTab = 0;
            RefreshSocialMediaViews();
            EnsureCloseButtonOnTop();
        }

        private void ShowWorkshopsApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(false);
            if (virtualMarketAppView != null) virtualMarketAppView.gameObject.SetActive(false);
            if (workshopsAppView != null) workshopsAppView.gameObject.SetActive(true);

            activeWorkshopTab = 0;
            RefreshWorkshopsViews();
            EnsureCloseButtonOnTop();
        }

        public void ShowVirtualMarketApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(false);
            if (workshopsAppView != null) workshopsAppView.gameObject.SetActive(false);
            if (virtualMarketAppView != null) virtualMarketAppView.gameObject.SetActive(true);

            RefreshVirtualMarketViews();
            EnsureCloseButtonOnTop();
        }

        private void CreateVirtualMarketAppView(Transform parent)
        {
            GameObject viewObj = new GameObject("VirtualMarketAppView");
            viewObj.transform.SetParent(parent, false);

            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;

            virtualMarketAppView = viewObj.transform;

            // 1. ÜST BAŞLIK ŞERİDİ
            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(viewObj.transform, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 205f);
            hRect.sizeDelta = new Vector2(850f, 40f);

            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform bRect = backBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(-360f, 0f);
            bRect.sizeDelta = new Vector2(130f, 36f);

            Image bBg = backBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(130, 36, 18, new Color(0.20f, 0.25f, 0.32f, 0.90f));
            bBg.raycastTarget = true;

            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;
            bBtn.onClick.AddListener(ShowHomeScreen);

            GameObject bTextObj = new GameObject("Text");
            bTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform btRect = bTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;

            Text bText = bTextObj.AddComponent<Text>();
            bText.font = globalFont;
            bText.text = LocalizationManager.L("Btn_HomeScreen", "← Ana Ekran", "← Home Screen");
            bText.fontSize = 18;
            bText.fontStyle = FontStyle.Bold;
            bText.alignment = TextAnchor.MiddleCenter;
            bText.color = new Color(0.35f, 0.85f, 1.0f);
            bText.raycastTarget = false;

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 0f);
            tRect.sizeDelta = new Vector2(400f, 40f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = LocalizationManager.L("Header_OnlineMarket", "🌐 ONLİNE MARKET", "🌐 ONLINE MARKET");
            tText.fontSize = 24;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.00f, 0.85f, 0.65f);
            tText.raycastTarget = false;

            // 2. 4'LÜ SEKME ÇUBUĞU
            CreateOnlineMarketTabs(viewObj.transform);

            // 3. 4 AYRI SCROLLABLE VIEWPORT
            onlineMarketFleetContent = CreateScrollableViewContainer(viewObj.transform, "FleetList", new Vector2(0f, -50f), new Vector2(850f, 350f), out onlineMarketFleetViewportObj);
            onlineMarketStaffContent = CreateScrollableViewContainer(viewObj.transform, "StaffList", new Vector2(0f, -50f), new Vector2(850f, 350f), out onlineMarketStaffViewportObj);
            onlineMarketCandidateContent = CreateScrollableViewContainer(viewObj.transform, "CandidateList", new Vector2(0f, -50f), new Vector2(850f, 350f), out onlineMarketCandidateViewportObj);
            onlineMarketShiftContent = CreateScrollableViewContainer(viewObj.transform, "ShiftList", new Vector2(0f, -50f), new Vector2(850f, 350f), out onlineMarketShiftViewportObj);

            VerticalLayoutGroup fLayout = onlineMarketFleetContent.gameObject.AddComponent<VerticalLayoutGroup>();
            fLayout.spacing = 12f;
            fLayout.childControlWidth = true;
            fLayout.childControlHeight = false;

            VerticalLayoutGroup sLayout = onlineMarketStaffContent.gameObject.AddComponent<VerticalLayoutGroup>();
            sLayout.spacing = 10f;
            sLayout.childControlWidth = true;
            sLayout.childControlHeight = false;

            GridLayoutGroup cGrid = onlineMarketCandidateContent.gameObject.AddComponent<GridLayoutGroup>();
            cGrid.cellSize = new Vector2(400f, 160f);
            cGrid.spacing = new Vector2(20f, 20f);
            cGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cGrid.constraintCount = 2;

            VerticalLayoutGroup shiftLayout = onlineMarketShiftContent.gameObject.AddComponent<VerticalLayoutGroup>();
            shiftLayout.spacing = 10f;
            shiftLayout.childControlWidth = true;
            shiftLayout.childControlHeight = false;

            viewObj.SetActive(false);
        }

        private void CreateOnlineMarketTabs(Transform parent)
        {
            GameObject tabsObj = new GameObject("OnlineMarketTabs");
            tabsObj.transform.SetParent(parent, false);

            RectTransform tRect = tabsObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 170f);
            tRect.sizeDelta = new Vector2(850f, 40f);

            HorizontalLayoutGroup layout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleCenter;

            string[] tabs = new string[] {
                LocalizationManager.L("Tab_OM_Fleet", "🛵 Filo & Siparişler", "🛵 Fleet & Orders"),
                LocalizationManager.L("Tab_OM_Staff", "👥 Personel Kadrosu", "👥 Staff List"),
                LocalizationManager.L("Tab_OM_Recruit", "📋 İşe Alım", "📋 Recruitment"),
                LocalizationManager.L("Tab_OM_Shifts", "⏰ Vardiyalar", "⏰ Shifts")
            };

            for (int i = 0; i < 4; i++)
            {
                int tabIndex = i;
                GameObject tabBtn = new GameObject("OM_Tab_" + i);
                tabBtn.transform.SetParent(tabsObj.transform, false);

                RectTransform tabRect = tabBtn.AddComponent<RectTransform>();
                tabRect.sizeDelta = new Vector2(195f, 40f);

                Image tabBg = tabBtn.AddComponent<Image>();
                tabBg.raycastTarget = true;
                onlineMarketTabBtnImgs[i] = tabBg;

                Button btn = tabBtn.AddComponent<Button>();
                btn.targetGraphic = tabBg;
                btn.onClick.AddListener(() => {
                    activeOnlineMarketTab = tabIndex;
                    RefreshVirtualMarketViews();
                });

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(tabBtn.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;

                Text tabText = textObj.AddComponent<Text>();
                tabText.font = globalFont;
                tabText.text = tabs[i];
                tabText.fontSize = 17;
                tabText.fontStyle = FontStyle.Bold;
                tabText.alignment = TextAnchor.MiddleCenter;
                tabText.raycastTarget = false;
                onlineMarketTabBtnTexts[i] = tabText;
            }

            UpdateOnlineMarketTabVisuals();
        }

        private void UpdateOnlineMarketTabVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                if (onlineMarketTabBtnImgs[i] == null) continue;
                bool isActive = (activeOnlineMarketTab == i);
                if (isActive)
                {
                    onlineMarketTabBtnImgs[i].sprite = UIStyleUtility.CreateOutlinePillSprite(195, 40, 20, 2, new Color(0.00f, 0.85f, 0.65f), new Color(0.06f, 0.22f, 0.18f, 0.95f));
                    if (onlineMarketTabBtnTexts[i] != null) onlineMarketTabBtnTexts[i].color = new Color(0.20f, 1.0f, 0.80f);
                }
                else
                {
                    onlineMarketTabBtnImgs[i].sprite = UIStyleUtility.CreateRoundedPillSprite(195, 40, 20, new Color(0.12f, 0.16f, 0.22f, 0.85f));
                    if (onlineMarketTabBtnTexts[i] != null) onlineMarketTabBtnTexts[i].color = new Color(0.70f, 0.78f, 0.85f);
                }
            }
        }

        private void RefreshVirtualMarketViews()
        {
            UpdateOnlineMarketTabVisuals();

            if (onlineMarketFleetViewportObj != null) onlineMarketFleetViewportObj.gameObject.SetActive(activeOnlineMarketTab == 0);
            if (onlineMarketStaffViewportObj != null) onlineMarketStaffViewportObj.gameObject.SetActive(activeOnlineMarketTab == 1);
            if (onlineMarketCandidateViewportObj != null) onlineMarketCandidateViewportObj.gameObject.SetActive(activeOnlineMarketTab == 2);
            if (onlineMarketShiftViewportObj != null) onlineMarketShiftViewportObj.gameObject.SetActive(activeOnlineMarketTab == 3);

            if (activeOnlineMarketTab == 0) RenderOnlineMarketFleetView();
            else if (activeOnlineMarketTab == 1) RenderOnlineMarketStaffView();
            else if (activeOnlineMarketTab == 2) RenderOnlineMarketCandidateView();
            else if (activeOnlineMarketTab == 3) RenderOnlineMarketShiftView();
        }

        private void RenderOnlineMarketFleetView()
        {
            if (onlineMarketFleetContent == null) return;
            foreach (Transform child in onlineMarketFleetContent) Destroy(child.gameObject);

            int ownedCount = (CourierManager.Instance != null) ? CourierManager.Instance.OwnedMotorcycleCount : 0;
            int maxSlots = CourierManager.MAX_MOTORCYCLES;

            // 5 Park Yuvasının Canlı Kartları
            for (int i = 0; i < maxSlots; i++)
            {
                int slotIdx = i;
                bool isOwned = (CourierManager.Instance != null && slotIdx < CourierManager.Instance.SpawnedMotorcycles.Count);
                CourierMotorcycleController moto = isOwned ? CourierManager.Instance.SpawnedMotorcycles[slotIdx] : null;

                GameObject cardObj = new GameObject("FleetCard_Slot_" + (slotIdx + 1));
                cardObj.transform.SetParent(onlineMarketFleetContent, false);

                LayoutElement le = cardObj.AddComponent<LayoutElement>();
                le.minHeight = 96f;
                le.preferredHeight = 96f;

                Image cardBg = cardObj.AddComponent<Image>();
                Color borderClr = isOwned ? new Color(0.00f, 0.85f, 0.65f, 0.8f) : new Color(0.30f, 0.35f, 0.45f, 0.5f);
                Color bgClr = isOwned ? new Color(0.10f, 0.15f, 0.22f, 0.95f) : new Color(0.08f, 0.10f, 0.14f, 0.80f);
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 96, 14, 1, borderClr, bgClr);

                // Sol İkon
                GameObject iconBox = new GameObject("IconBox");
                iconBox.transform.SetParent(cardObj.transform, false);
                RectTransform ibRect = iconBox.AddComponent<RectTransform>();
                ibRect.anchoredPosition = new Vector2(-360f, 0f);
                ibRect.sizeDelta = new Vector2(58f, 58f);

                Image ibBg = iconBox.AddComponent<Image>();
                ibBg.sprite = UIStyleUtility.CreateRoundedPillSprite(58, 58, 14, isOwned ? new Color(0.12f, 0.25f, 0.32f) : new Color(0.15f, 0.18f, 0.22f));

                Text icoTxt = CreateTextInPanel(iconBox.transform, Vector2.zero, Vector2.one, isOwned ? "🛵" : "🔒", 28, Color.white);
                icoTxt.alignment = TextAnchor.MiddleCenter;

                // Orta Bilgi Alanı
                GameObject infoPanel = new GameObject("InfoPanel");
                infoPanel.transform.SetParent(cardObj.transform, false);
                RectTransform ipRect = infoPanel.AddComponent<RectTransform>();
                ipRect.anchoredPosition = new Vector2(-30f, 0f);
                ipRect.sizeDelta = new Vector2(560f, 85f);

                if (isOwned && moto != null)
                {
                    string statusStr;
                    Color statusColor;
                    if (moto.CurrentState == MotorcycleState.ParkedInBay) { statusStr = LocalizationManager.L("OM_Status_Parked", "🟢 Parkta / Müsait", "🟢 Parked / Ready"); statusColor = new Color(0.20f, 0.90f, 0.40f); }
                    else if (moto.CurrentState == MotorcycleState.WaitingForStocker) { statusStr = LocalizationManager.L("OM_Status_Loading", "🟡 Reyoncu Sipariş Yüklüyor...", "🟡 Stocker Loading Items..."); statusColor = new Color(1.0f, 0.85f, 0.20f); }
                    else if (moto.CurrentState == MotorcycleState.EnRouteDelivery) { statusStr = LocalizationManager.L("OM_Status_EnRoute", "🔵 Dağıtımda (Adrese Gidiyor)", "🔵 En Route Delivery"); statusColor = new Color(0.30f, 0.80f, 1.0f); }
                    else if (moto.CurrentState == MotorcycleState.DeliveringAtDoorstep) { statusStr = LocalizationManager.L("OM_Status_Doorstep", "📦 Kapıda Teslim Ediliyor...", "📦 Delivering at Doorstep..."); statusColor = new Color(1.0f, 0.60f, 0.20f); }
                    else { statusStr = LocalizationManager.L("OM_Status_Returning", "🔄 Dükkana Geri Dönüyor", "🔄 Returning to Store"); statusColor = new Color(0.40f, 0.90f, 0.70f); }

                    string driverStr = (moto.AssignedCourier != null)
                        ? $"{moto.AssignedCourier.name} ({moto.AssignedCourier.shiftHours})"
                        : LocalizationManager.L("OM_NoDriver", "⚠️ Kurye Atanmadı (Sürücü Bekleniyor)", "⚠️ No Courier Assigned (Waiting)");

                    string cargoStr = (moto.LoadedOrders.Count > 0)
                        ? string.Format(LocalizationManager.L("OM_OrdersLoadedFmt", "📦 Bagajda {0} Adres Siparişi Var", "📦 {0} Address Orders in Bag"), moto.LoadedOrders.Count)
                        : LocalizationManager.L("OM_BagEmpty", "📦 Bagaj Boş (Yeni Sipariş Bekleniyor)", "📦 Cargo Bag Empty (Waiting for Orders)");

                    string motoTitleFmt = LocalizationManager.L("OM_MotoTitleFmt", "🛵 Motorsiklet #{0} (Park Yuvası #{0})", "🛵 Motorcycle #{0} (Bay #{0})");
                    Text titleTxt = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 24f), new Vector2(560f, 22f), string.Format(motoTitleFmt, slotIdx + 1), 18, Color.white);
                    titleTxt.fontStyle = FontStyle.Bold;
                    titleTxt.alignment = TextAnchor.MiddleLeft;

                    Text statusTxt = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 2f), new Vector2(560f, 20f), $"{statusStr}   |   <b>{driverStr}</b>", 15, statusColor);
                    statusTxt.alignment = TextAnchor.MiddleLeft;

                    Text cargoTxt = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -20f), new Vector2(560f, 20f), cargoStr, 14, new Color(0.80f, 0.85f, 0.92f));
                    cargoTxt.alignment = TextAnchor.MiddleLeft;
                }
                else
                {
                    string emptyTitleFmt = LocalizationManager.L("OM_SlotEmptyTitleFmt", "🔒 Park Yuvası #{0} (Boş)", "🔒 Parking Bay #{0} (Empty)");
                    Text titleTxt = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 14f), new Vector2(560f, 24f), string.Format(emptyTitleFmt, slotIdx + 1), 18, new Color(0.70f, 0.75f, 0.82f));
                    titleTxt.fontStyle = FontStyle.Bold;
                    titleTxt.alignment = TextAnchor.MiddleLeft;

                    string buyHint = LocalizationManager.L("OM_BuyHint", "Alışveriş -> 'Araçlar' sekmesinden yeni motorsiklet satın alabilirsiniz.", "You can purchase a new motorcycle from Shopping -> 'Vehicles'.");
                    Text descTxt = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -12f), new Vector2(560f, 22f), buyHint, 15, new Color(0.50f, 0.55f, 0.65f));
                    descTxt.alignment = TextAnchor.MiddleLeft;
                }
            }
        }

        private void RenderOnlineMarketStaffView()
        {
            if (onlineMarketStaffContent == null) return;
            foreach (Transform child in onlineMarketStaffContent) Destroy(child.gameObject);

            List<StaffMember> couriers = (StaffManager.Instance != null) ? StaffManager.Instance.GetCourierStaffList() : new List<StaffMember>();

            if (couriers.Count == 0)
            {
                GameObject emptyCard = new GameObject("EmptyStaffCard");
                emptyCard.transform.SetParent(onlineMarketStaffContent, false);
                LayoutElement le = emptyCard.AddComponent<LayoutElement>();
                le.minHeight = 220f;
                le.preferredHeight = 220f;

                Image emptyBg = emptyCard.AddComponent<Image>();
                emptyBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 220, 16, 1, new Color(0.35f, 0.45f, 0.55f, 0.6f), new Color(0.10f, 0.13f, 0.18f, 0.90f));

                string noStaffText = LocalizationManager.L(
                    "OM_NoStaffText",
                    "<b><size=20><color=#FFD54F>👥 Henüz İşe Alınmış Kurye Bulunmuyor</color></size></b>\n\n" +
                    "<size=15><color=#CFD8DC>'İşe Alım' sekmesine geçerek kadın veya erkek kurye adaylarını işe alabilirsiniz.\n" +
                    "Kuryeler vardiyalarından 30 dk önce gelip satın aldığınız motorlara binecektir.</color></size>",
                    "<b><size=20><color=#FFD54F>👥 No Couriers Hired Yet</color></size></b>\n\n" +
                    "<size=15><color=#CFD8DC>Go to the 'Recruitment' tab to hire female or male courier candidates.\n" +
                    "Couriers arrive 30 mins before their shift and mount your purchased motorcycles.</color></size>"
                );

                Text emptyTxt = CreateTextInPanel(emptyCard.transform, Vector2.zero, Vector2.one, noStaffText, 16, Color.white);
                emptyTxt.alignment = TextAnchor.MiddleCenter;
                emptyTxt.lineSpacing = 1.2f;
                return;
            }

            for (int i = 0; i < couriers.Count; i++)
            {
                StaffMember courier = couriers[i];
                if (courier == null) continue;

                int courierIdx = i;
                GameObject cardObj = new GameObject("CourierCard_" + courier.id);
                cardObj.transform.SetParent(onlineMarketStaffContent, false);

                LayoutElement le = cardObj.AddComponent<LayoutElement>();
                le.minHeight = 90f;
                le.preferredHeight = 90f;

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 90, 14, 1, new Color(0.00f, 0.85f, 0.65f, 0.7f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

                // Sol Avatar Kutusu
                GameObject avBox = new GameObject("AvatarBox");
                avBox.transform.SetParent(cardObj.transform, false);
                RectTransform avRect = avBox.AddComponent<RectTransform>();
                avRect.anchoredPosition = new Vector2(-360f, 0f);
                avRect.sizeDelta = new Vector2(56f, 56f);

                Image avImg = avBox.AddComponent<Image>();
                avImg.sprite = ProfileAvatarDatabase.GetStaffAvatarSprite(StaffRole.Kurye, courier.isFemale, courier.name);

                // Orta Bilgi Alanı
                GameObject infoPanel = new GameObject("InfoPanel");
                infoPanel.transform.SetParent(cardObj.transform, false);
                RectTransform ipRect = infoPanel.AddComponent<RectTransform>();
                ipRect.anchoredPosition = new Vector2(-30f, 0f);
                ipRect.sizeDelta = new Vector2(500f, 75f);

                string assignedMotoStr = (CourierManager.Instance != null && courierIdx < CourierManager.Instance.SpawnedMotorcycles.Count)
                    ? $"🛵 Motorsiklet #{courierIdx + 1}"
                    : "⚠️ Motor Bekleniyor (Boşta)";

                Text titleTxt = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 20f), new Vector2(500f, 22f), $"<b>{courier.name}</b>  |  <color=#00E676>Kurye</color>  |  <color=#80D8FF>{assignedMotoStr}</color>", 17, Color.white);
                titleTxt.alignment = TextAnchor.MiddleLeft;

                string subStr = $"{courier.shiftHours}   |   <b>Maaş: {courier.dailySalary:N0}C / Gün</b>";
                Text subTxt = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -12f), new Vector2(500f, 20f), subStr, 14, new Color(0.85f, 0.90f, 0.95f));
                subTxt.alignment = TextAnchor.MiddleLeft;

                // Sağ Kovma (İşten Çıkar) Butonu
                GameObject fireBtnObj = new GameObject("FireBtn");
                fireBtnObj.transform.SetParent(cardObj.transform, false);
                RectTransform fbRect = fireBtnObj.AddComponent<RectTransform>();
                fbRect.anchoredPosition = new Vector2(330f, 0f);
                fbRect.sizeDelta = new Vector2(110f, 38f);

                Image fbBg = fireBtnObj.AddComponent<Image>();
                fbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(110, 38, 14, new Color(0.85f, 0.22f, 0.22f));

                Button fbBtn = fireBtnObj.AddComponent<Button>();
                fbBtn.targetGraphic = fbBg;
                fbBtn.onClick.AddListener(() => {
                    string confirmTitle = LocalizationManager.L("Modal_FireCourier_Title", "İşten Çıkarma Onayı", "Dismissal Confirmation");
                    string confirmBody = string.Format(LocalizationManager.L("Modal_FireCourier_Body", "**{0}** isimli kurye personelini işten çıkarmak istiyor musunuz?", "Are you sure you want to dismiss courier **{0}**?"), courier.name);
                    string btnFire = LocalizationManager.L("Btn_ConfirmFire", "Evet, İşten Çıkar", "Yes, Dismiss");
                    string btnCancel = LocalizationManager.L("Btn_Cancel", "Vazgeç", "Cancel");

                    ModalManager.ShowConfirmModal(confirmTitle, confirmBody, () => {
                        if (StaffManager.Instance != null)
                        {
                            StaffManager.Instance.FireCourier(courier.id);
                            if (CourierManager.Instance != null) CourierManager.Instance.AutoAssignCouriersToMotorcycles();
                            RefreshVirtualMarketViews();
                        }
                    }, btnFire, btnCancel);
                });

                Text fbTxt = CreateTextInPanel(fireBtnObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_Fire", "❌ İşten Çıkar", "❌ Dismiss"), 14, Color.white);
                fbTxt.alignment = TextAnchor.MiddleCenter;
                fbTxt.fontStyle = FontStyle.Bold;
            }
        }

        private void RenderOnlineMarketCandidateView()
        {
            if (onlineMarketCandidateContent == null) return;
            foreach (Transform child in onlineMarketCandidateContent) Destroy(child.gameObject);

            // 2 Aday Kartı (1 Kadın Kurye, 1 Erkek Kurye)
            (string nameTr, string nameEn, bool isFemale)[] candidates = new (string, string, bool)[]
            {
                ("Selin Aydın", "Lisa Martinez", true),
                ("Burak Çelik", "David Wilson", false)
            };

            int hireFee = (StaffManager.Instance != null) ? StaffManager.Instance.GetRoleHireFee(StaffRole.Kurye) : 450;
            int dailySalary = (StaffManager.Instance != null) ? StaffManager.Instance.GetRoleDailySalary(StaffRole.Kurye) : 120;

            for (int i = 0; i < candidates.Length; i++)
            {
                var cand = candidates[i];
                bool isFemale = cand.isFemale;
                string candName = (LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish) ? cand.nameEn : cand.nameTr;

                GameObject cardObj = new GameObject("CandidateCard_" + i);
                cardObj.transform.SetParent(onlineMarketCandidateContent, false);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(400, 160, 16, 1, new Color(0.00f, 0.85f, 0.65f, 0.8f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

                // Sol Avatar Kutusu
                GameObject avBox = new GameObject("AvatarBox");
                avBox.transform.SetParent(cardObj.transform, false);
                RectTransform avRect = avBox.AddComponent<RectTransform>();
                avRect.anchoredPosition = new Vector2(-135f, 15f);
                avRect.sizeDelta = new Vector2(68f, 68f);

                Image avImg = avBox.AddComponent<Image>();
                avImg.sprite = ProfileAvatarDatabase.GetStaffAvatarSprite(StaffRole.Kurye, isFemale, candName);

                // Bilgi Alanı
                GameObject infoObj = new GameObject("InfoObj");
                infoObj.transform.SetParent(cardObj.transform, false);
                RectTransform ipRect = infoObj.AddComponent<RectTransform>();
                ipRect.anchoredPosition = new Vector2(40f, 15f);
                ipRect.sizeDelta = new Vector2(250f, 75f);

                string roleStr = LocalizationManager.L("OM_RoleCourier", "🛵 Kurye (Teslimat Sorumlusu)", "🛵 Courier (Delivery Driver)");
                Text nameTxt = CreateTextInPanel(infoObj.transform, new Vector2(0f, 22f), new Vector2(250f, 22f), $"<b>{candName}</b>", 18, Color.white);
                nameTxt.alignment = TextAnchor.MiddleLeft;

                Text roleTxt = CreateTextInPanel(infoObj.transform, new Vector2(0f, 2f), new Vector2(250f, 18f), roleStr, 14, new Color(0.00f, 0.85f, 0.65f));
                roleTxt.alignment = TextAnchor.MiddleLeft;

                string salFmt = LocalizationManager.L("OM_SalaryFmt", "Maaş: {0:N0}C/Gün (Gece 00:00) | İşe Alım: Ücretsiz", "Salary: {0:N0}C/Day (At 00:00) | Hire: Free");
                Text salTxt = CreateTextInPanel(infoObj.transform, new Vector2(0f, -18f), new Vector2(250f, 18f), string.Format(salFmt, dailySalary), 13, new Color(0.95f, 0.85f, 0.30f));
                salTxt.alignment = TextAnchor.MiddleLeft;

                // Alt İşe Al Butonu
                GameObject hireBtnObj = new GameObject("HireBtn");
                hireBtnObj.transform.SetParent(cardObj.transform, false);
                RectTransform hbRect = hireBtnObj.AddComponent<RectTransform>();
                hbRect.anchoredPosition = new Vector2(0f, -50f);
                hbRect.sizeDelta = new Vector2(360f, 38f);

                Image hbBg = hireBtnObj.AddComponent<Image>();
                hbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(360, 38, 14, new Color(0.12f, 0.70f, 0.38f));

                Button hbBtn = hireBtnObj.AddComponent<Button>();
                hbBtn.targetGraphic = hbBg;
                hbBtn.onClick.AddListener(() => {
                    if (StaffManager.Instance != null)
                    {
                        StaffMember hired = StaffManager.Instance.HireCourier(isFemale);
                        if (CourierManager.Instance != null) CourierManager.Instance.AutoAssignCouriersToMotorcycles();

                        ModalManager.ShowModal(
                            LocalizationManager.L("Modal_Hire_SuccessTitle", "🎉 Kurye İşe Alındı!", "🎉 Courier Hired!"),
                            string.Format(LocalizationManager.L("Modal_Hire_SuccessBody", "{0} başarıyla kurye kadrosuna katıldı.\n\nGünlük maaşı ({1:N0}C) gece 00:00'da ödenecektir. Vardiyasından 30 dk önce gelip motorsikletine binecektir.", "{0} successfully joined the courier team.\n\nDaily salary ({1:N0}C) will be paid at midnight 00:00. They arrive 30 mins before shift to mount their bike."), candName, dailySalary),
                            LocalizationManager.L("Btn_Ok", "Harika!", "Awesome!")
                        );

                        activeOnlineMarketTab = 1; // Kadro sekmesine geç
                        RefreshVirtualMarketViews();
                    }
                });

                string hireBtnText = LocalizationManager.L("Btn_HireCourierFree", "✅ İşe Al (Ücretsiz Başlangıç)", "✅ Hire (Free Start)");
                Text hbTxt = CreateTextInPanel(hireBtnObj.transform, Vector2.zero, Vector2.one, hireBtnText, 15, Color.white);
                hbTxt.alignment = TextAnchor.MiddleCenter;
                hbTxt.fontStyle = FontStyle.Bold;
            }
        }

        private void RenderOnlineMarketShiftView()
        {
            if (onlineMarketShiftContent == null) return;
            foreach (Transform child in onlineMarketShiftContent) Destroy(child.gameObject);

            List<StaffMember> couriers = (StaffManager.Instance != null) ? StaffManager.Instance.GetCourierStaffList() : new List<StaffMember>();

            if (couriers.Count == 0)
            {
                GameObject emptyCard = new GameObject("EmptyShiftCard");
                emptyCard.transform.SetParent(onlineMarketShiftContent, false);
                LayoutElement le = emptyCard.AddComponent<LayoutElement>();
                le.minHeight = 180f;
                le.preferredHeight = 180f;

                Image emptyBg = emptyCard.AddComponent<Image>();
                emptyBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 180, 16, 1, new Color(0.35f, 0.45f, 0.55f, 0.6f), new Color(0.10f, 0.13f, 0.18f, 0.90f));

                string noShiftTxt = LocalizationManager.L(
                    "OM_NoShiftText",
                    "<b><size=18><color=#FFD54F>⏰ Henüz Vardiya Ayarlanacak Kurye Bulunmuyor</color></size></b>\n\n" +
                    "<size=15><color=#CFD8DC>'İşe Alım' sekmesinden kurye personeli istihdam ettikten sonra buradan vardiya saatlerini seçebilirsiniz.</color></size>",
                    "<b><size=18><color=#FFD54F>⏰ No Couriers Available to Schedule Shifts</color></size></b>\n\n" +
                    "<size=15><color=#CFD8DC>After hiring couriers from 'Recruitment', you can configure their working shifts here.</color></size>"
                );

                Text txt = CreateTextInPanel(emptyCard.transform, Vector2.zero, Vector2.one, noShiftTxt, 16, Color.white);
                txt.alignment = TextAnchor.MiddleCenter;
                txt.lineSpacing = 1.2f;
                return;
            }

            for (int i = 0; i < couriers.Count; i++)
            {
                StaffMember courier = couriers[i];
                if (courier == null) continue;

                GameObject cardObj = new GameObject("ShiftCard_" + courier.id);
                cardObj.transform.SetParent(onlineMarketShiftContent, false);

                LayoutElement le = cardObj.AddComponent<LayoutElement>();
                le.minHeight = 84f;
                le.preferredHeight = 84f;

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 84, 14, 1, new Color(0.00f, 0.85f, 0.65f, 0.6f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

                // Sol Avatar & İsim
                GameObject avBox = new GameObject("AvatarBox");
                avBox.transform.SetParent(cardObj.transform, false);
                RectTransform avRect = avBox.AddComponent<RectTransform>();
                avRect.anchoredPosition = new Vector2(-360f, 0f);
                avRect.sizeDelta = new Vector2(52f, 52f);

                Image avImg = avBox.AddComponent<Image>();
                avImg.sprite = ProfileAvatarDatabase.GetStaffAvatarSprite(StaffRole.Kurye, courier.isFemale, courier.name);

                GameObject nameObj = new GameObject("NameObj");
                nameObj.transform.SetParent(cardObj.transform, false);
                RectTransform npRect = nameObj.AddComponent<RectTransform>();
                npRect.anchoredPosition = new Vector2(-150f, 0f);
                npRect.sizeDelta = new Vector2(300f, 60f);

                Text nameTxt = CreateTextInPanel(nameObj.transform, Vector2.zero, Vector2.one, $"<b>{courier.name}</b>\n<size=13><color=#80D8FF>Mevcut: {courier.shiftHours}</color></size>", 16, Color.white);
                nameTxt.alignment = TextAnchor.MiddleLeft;

                // Sağ 2 Vardiya Butonu (Sabah / Akşam)
                string[] shiftOptions = new string[] {
                    "☀️ Sabah (08:00 - 16:00)",
                    "🌆 Akşam (16:00 - 24:00)"
                };

                for (int s = 0; s < 2; s++)
                {
                    string targetShift = shiftOptions[s];
                    bool isCurShift = courier.shiftHours.Contains(s == 0 ? "Sabah" : "Akşam");

                    GameObject sBtnObj = new GameObject("ShiftBtn_" + s);
                    sBtnObj.transform.SetParent(cardObj.transform, false);
                    RectTransform sbRect = sBtnObj.AddComponent<RectTransform>();
                    sbRect.anchoredPosition = new Vector2(170f + (s * 155f), 0f);
                    sbRect.sizeDelta = new Vector2(145f, 38f);

                    Image sbBg = sBtnObj.AddComponent<Image>();
                    sbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(145, 38, 14, isCurShift ? new Color(0.00f, 0.85f, 0.65f) : new Color(0.20f, 0.25f, 0.35f));

                    Button sbBtn = sBtnObj.AddComponent<Button>();
                    sbBtn.targetGraphic = sbBg;
                    sbBtn.onClick.AddListener(() => {
                        if (StaffManager.Instance != null)
                        {
                            StaffManager.Instance.UpdateCourierShift(courier.id, targetShift);
                            RefreshVirtualMarketViews();
                        }
                    });

                    Text sbTxt = CreateTextInPanel(sBtnObj.transform, Vector2.zero, Vector2.one, (s == 0) ? "☀️ Sabah" : "🌆 Akşam", 14, isCurShift ? Color.black : Color.white);
                    sbTxt.alignment = TextAnchor.MiddleCenter;
                    sbTxt.fontStyle = FontStyle.Bold;
                }
            }
        }

        private void CreateWorkshopsAppView(Transform parent)
        {
            GameObject viewObj = new GameObject("WorkshopsAppView");
            viewObj.transform.SetParent(parent, false);

            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;

            workshopsAppView = viewObj.transform;

            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(viewObj.transform, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 205f);
            hRect.sizeDelta = new Vector2(850f, 40f);

            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform bRect = backBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(-360f, 0f);
            bRect.sizeDelta = new Vector2(130f, 36f);

            Image bBg = backBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(130, 36, 18, new Color(0.20f, 0.25f, 0.32f, 0.90f));
            bBg.raycastTarget = true;

            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;
            bBtn.onClick.AddListener(ShowHomeScreen);

            GameObject bTextObj = new GameObject("Text");
            bTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform btRect = bTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;

            Text bText = bTextObj.AddComponent<Text>();
            bText.font = globalFont;
            bText.text = LocalizationManager.L("Btn_HomeScreen", "← Ana Ekran", "← Home Screen");
            bText.fontSize = 18;
            bText.fontStyle = FontStyle.Bold;
            bText.alignment = TextAnchor.MiddleCenter;
            bText.color = new Color(0.95f, 0.60f, 0.15f);
            bText.raycastTarget = false;

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 0f);
            tRect.sizeDelta = new Vector2(400f, 40f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = LocalizationManager.L("Header_Workshops", "🏭 ATÖLYELER", "🏭 WORKSHOPS");
            tText.fontSize = 24;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.95f, 0.65f, 0.20f);
            tText.raycastTarget = false;

            CreateWorkshopTabs(viewObj.transform);

            // Tab 0: Atölye Binası Seviye Geliştirmeleri
            workshopUpgradeContent = CreateScrollableViewContainer(viewObj.transform, "WorkshopUpgradeList", new Vector2(0f, -50f), new Vector2(850f, 350f), out workshopManagementViewportObj);

            VerticalLayoutGroup uLayout = workshopUpgradeContent.gameObject.AddComponent<VerticalLayoutGroup>();
            uLayout.spacing = 15f;
            uLayout.childControlWidth = true;
            uLayout.childControlHeight = false;

            // Tab 1: Makine Yönetimi (Kurulan Tüm Makinelerin Canlı Listesi)
            workshopMachinesContent = CreateScrollableViewContainer(viewObj.transform, "WorkshopMachinesList", new Vector2(0f, -50f), new Vector2(850f, 350f), out workshopMachinesViewportObj);

            VerticalLayoutGroup mLayout = workshopMachinesContent.gameObject.AddComponent<VerticalLayoutGroup>();
            mLayout.spacing = 12f;
            mLayout.childControlWidth = true;
            mLayout.childControlHeight = false;

            viewObj.SetActive(false);
        }

        private void CreateWorkshopTabs(Transform parent)
        {
            GameObject tabsObj = new GameObject("WorkshopTabs");
            tabsObj.transform.SetParent(parent, false);

            RectTransform tRect = tabsObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 170f);
            tRect.sizeDelta = new Vector2(850f, 40f);

            HorizontalLayoutGroup layout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleLeft;

            string[] tabs = new string[] {
                LocalizationManager.L("Tab_WorkshopBuilding", "🏢 Atölye Binası", "🏢 Workshop Building"),
                LocalizationManager.L("Tab_WorkshopMachines", "⚙️ Makine Yönetimi", "⚙️ Machine Management")
            };

            for (int i = 0; i < tabs.Length; i++)
            {
                int tabIndex = i;
                GameObject tabBtn = new GameObject("WorkshopTab_" + i);
                tabBtn.transform.SetParent(tabsObj.transform, false);

                RectTransform tabRect = tabBtn.AddComponent<RectTransform>();
                tabRect.sizeDelta = new Vector2(250f, 38f);

                Image tabBg = tabBtn.AddComponent<Image>();
                tabBg.sprite = UIStyleUtility.CreateOutlinePillSprite(250, 38, 18, 2, new Color(0.95f, 0.60f, 0.15f), new Color(0.12f, 0.16f, 0.22f, 0.85f));
                tabBg.raycastTarget = true;

                Button btn = tabBtn.AddComponent<Button>();
                btn.targetGraphic = tabBg;
                btn.onClick.AddListener(() => {
                    activeWorkshopTab = tabIndex;
                    RefreshWorkshopsViews();
                });

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(tabBtn.transform, false);
                RectTransform txtRect = textObj.AddComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;

                Text btnText = textObj.AddComponent<Text>();
                btnText.font = globalFont;
                btnText.text = tabs[i];
                btnText.fontSize = 16;
                btnText.fontStyle = FontStyle.Bold;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = Color.white;
                btnText.raycastTarget = false;
            }
        }

        private void RefreshWorkshopsViews()
        {
            if (workshopsAppView == null) return;

            Transform tabsTransform = workshopsAppView.Find("WorkshopTabs");
            if (tabsTransform != null)
            {
                for (int i = 0; i < tabsTransform.childCount; i++)
                {
                    Transform tabBtn = tabsTransform.GetChild(i);
                    Image img = tabBtn.GetComponent<Image>();
                    Text txt = tabBtn.GetComponentInChildren<Text>();
                    if (i == activeWorkshopTab)
                    {
                        img.sprite = UIStyleUtility.CreateRoundedPillSprite(250, 38, 18, new Color(0.95f, 0.60f, 0.15f));
                        txt.color = Color.white;
                    }
                    else
                    {
                        img.sprite = UIStyleUtility.CreateOutlinePillSprite(250, 38, 18, 2, new Color(0.95f, 0.60f, 0.15f), new Color(0.12f, 0.16f, 0.22f, 0.85f));
                        txt.color = new Color(0.80f, 0.85f, 0.90f);
                    }
                }
            }

            if (workshopManagementViewportObj != null)
            {
                workshopManagementViewportObj.gameObject.SetActive(activeWorkshopTab == 0);
            }
            if (workshopMachinesViewportObj != null)
            {
                workshopMachinesViewportObj.gameObject.SetActive(activeWorkshopTab == 1);
            }

            if (activeWorkshopTab == 0)
            {
                RenderWorkshopUpgradeList();
            }
            else if (activeWorkshopTab == 1)
            {
                RenderWorkshopMachinesList();
            }
        }

        private void RenderWorkshopUpgradeList()
        {
            if (workshopUpgradeContent == null) return;

            foreach (Transform child in workshopUpgradeContent)
            {
                Destroy(child.gameObject);
            }

            int currentLevel = (WorkshopManager.Instance != null) ? WorkshopManager.Instance.CurrentWorkshopLevel : 1;

            string[] stageNames = new string[] {
                LocalizationManager.L("Workshop_Stage1_Name", "Seviye 1: Başlangıç Atölyesi (18m Derinlik)", "Level 1: Starter Workshop (18m Depth)"),
                LocalizationManager.L("Workshop_Stage2_Name", "Seviye 2: Genişletilmiş Atölye (27m Derinlik)", "Level 2: Expanded Workshop (27m Depth)"),
                LocalizationManager.L("Workshop_Stage3_Name", "Seviye 3: Mega Sanayi Kompleksi (43m Tam Derinlik)", "Level 3: Mega Industrial Complex (43m Full Depth)")
            };

            int[] targetLevels = new int[] { 1, 2, 3 };
            int[] upgradeCosts = new int[] { 0, 7500, 20000 };

            string[] descriptions = new string[] {
                LocalizationManager.L("Workshop_Stage1_Desc", "• 25m genişlik x 18m derinlikte temel üretim binası.\n• Otomatik çift kanatlı cam kayar kapı ve modern pencereler.\n• İçi boş, üretime ve tezgahlara hazır epoksi zemin.", "• 25m width x 18m depth base production building.\n• Automatic double sliding glass door & modern windows.\n• Empty interior with epoxy floor ready for crafting."),
                LocalizationManager.L("Workshop_Stage2_Desc", "• Bina arkaya doğru +9.0m uzatılarak toplam 27m derinliğe ulaşır.\n• Sağ ve sol cepheye ekstra endüstriyel pencereler eklenir.\n• Ekstra tavan aydınlatmaları ve genişletilmiş üretim alanı.", "• Building expands +9.0m backwards reaching 27m depth.\n• Extra industrial windows added to both side walls.\n• Additional ceiling lighting & expanded production area."),
                LocalizationManager.L("Workshop_Stage3_Desc", "• Bina Kuzey Çevre Yoluna kadar (+16.0m) uzatılarak 43m devasa boyuta ulaşır.\n• Maksimum üretim kapasitesi ve tam boy sanayi binası.\n• Tüm parseli kaplayan birinci sınıf modern fabrika mimarisi.", "• Expands all the way to North Ring Road (+16.0m) reaching 43m.\n• Maximum crafting capacity & full-scale industrial plant.\n• Premium modern factory architecture spanning full plot.")
            };

            for (int i = 0; i < 3; i++)
            {
                int targetLvl = targetLevels[i];
                int cost = upgradeCosts[i];

                bool isUnlocked = (currentLevel >= targetLvl);
                bool canUpgradeNow = (currentLevel == targetLvl - 1);
                bool isLocked = (currentLevel < targetLvl - 1);

                GameObject cardObj = new GameObject("WorkshopCard_" + targetLvl);
                cardObj.transform.SetParent(workshopUpgradeContent, false);

                RectTransform cRect = cardObj.AddComponent<RectTransform>();
                cRect.sizeDelta = new Vector2(820f, 105f);

                Color borderColor;
                if (isUnlocked) borderColor = new Color(0.20f, 0.85f, 0.40f);
                else if (canUpgradeNow) borderColor = new Color(0.95f, 0.60f, 0.15f);
                else borderColor = new Color(0.40f, 0.45f, 0.55f);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 105, 16, 2, borderColor, new Color(0.12f, 0.16f, 0.22f, 0.95f));
                cardBg.raycastTarget = false;

                GameObject infoObj = new GameObject("InfoText");
                infoObj.transform.SetParent(cardObj.transform, false);

                RectTransform iRect = infoObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(-110f, 0f);
                iRect.sizeDelta = new Vector2(550f, 95f);

                Text iText = infoObj.AddComponent<Text>();
                iText.font = globalFont;
                string costWord = LocalizationManager.L("Label_Cost", "Ücret", "Cost");
                string costStr = (cost == 0) ? LocalizationManager.L("Label_FreeStart", "Başlangıç Seviyesi", "Starter Level") : $"{cost:N0}C";
                iText.text = $"🏭 <b>{stageNames[i]}</b>   |   <b>{costWord}: {costStr}</b>\n{descriptions[i]}";
                iText.fontSize = 16;
                iText.fontStyle = FontStyle.Normal;
                iText.alignment = TextAnchor.MiddleLeft;
                iText.color = isLocked ? new Color(0.60f, 0.65f, 0.72f) : new Color(0.92f, 0.94f, 0.98f);
                iText.raycastTarget = false;

                GameObject btnObj = new GameObject("UpgradeButton");
                btnObj.transform.SetParent(cardObj.transform, false);

                RectTransform bRect = btnObj.AddComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(270f, 0f);
                bRect.sizeDelta = new Vector2(210f, 44f);

                Image bBg = btnObj.AddComponent<Image>();
                Color btnClr;
                string btnLabelText;

                if (isUnlocked)
                {
                    btnClr = new Color(0.20f, 0.50f, 0.30f, 0.70f);
                    btnLabelText = LocalizationManager.L("Btn_CurrentLevel", "✔ MEVCUT SEVİYE", "✔ CURRENT LEVEL");
                }
                else if (canUpgradeNow)
                {
                    btnClr = new Color(0.95f, 0.55f, 0.15f, 0.95f);
                    btnLabelText = LocalizationManager.L("Btn_UpgradeNow", "🚀 GELİŞTİR (GÜNCELLE)", "🚀 UPGRADE NOW");
                }
                else
                {
                    btnClr = new Color(0.30f, 0.35f, 0.40f, 0.60f);
                    btnLabelText = LocalizationManager.L("Btn_LockedFmtShort", "🔒 KİLİTLİ", "🔒 LOCKED");
                }

                bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(210, 44, 12, btnClr);
                bBg.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = bBg;

                if (canUpgradeNow)
                {
                    btn.onClick.AddListener(() => {
                        int playerMoney = (EconomyManager.Instance != null) ? EconomyManager.Instance.Credits : 0;
                        if (playerMoney < cost)
                        {
                            string lowTitle = LocalizationManager.L("Modal_LowBalance_Title", "Yetersiz Bakiye ⚠️", "Insufficient Balance ⚠️");
                            string lowBody = string.Format(LocalizationManager.L("Modal_LowBalance_UpgradeBody", "Bu atölye geliştirmesi için {0:N0}C gereklidir!\nMevcut Bakiyeniz: {1:N0}C.", "You need {0:N0}C for this workshop upgrade!\nCurrent Balance: {1:N0}C."), cost, playerMoney);
                            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
                            ModalManager.ShowModal(lowTitle, lowBody, btnOk);
                            return;
                        }

                        string confirmTitle = LocalizationManager.L("Modal_WorkshopUpgrade_Title", "🏭 Atölye Geliştirme Onayı", "🏭 Workshop Upgrade Confirmation");
                        string confirmBody = string.Format(LocalizationManager.L("Modal_WorkshopUpgrade_Body", "Atölyenizi **{0}** aşamasına yükseltmek istiyor musunuz?\n\n💰 **İnşaat Maliyeti:** {1:N0}C\n📐 **Yeni Boyut:** 25m Genişlik x {2}m Derinlik\n\nBu işlem onaylandığında atölye binanız harita üzerinde anında genişletilecektir.", "Do you want to upgrade your workshop to **{0}**?\n\n💰 **Construction Cost:** {1:N0}C\n📐 **New Size:** 25m Width x {2}m Depth\n\nYour workshop building will be expanded on the map immediately upon confirmation."), stageNames[targetLvl - 1], cost, (targetLvl == 2) ? 27 : 43);
                        string btnConfirm = LocalizationManager.L("Btn_ConfirmUpgrade", "Evet, İnşaatı Başlat", "Yes, Start Construction");
                        string btnCancel = LocalizationManager.L("Btn_Cancel", "Vazgeç", "Cancel");

                        ModalManager.ShowConfirmModal(confirmTitle, confirmBody, () => {
                            if (WorkshopManager.Instance != null && WorkshopManager.Instance.UpgradeWorkshop(targetLvl))
                            {
                                RefreshWorkshopsViews();
                                string sucTitle = LocalizationManager.L("Modal_WorkshopUpgrade_SuccessTitle", "🎉 Atölye Genişletildi!", "🎉 Workshop Expanded!");
                                string sucBody = string.Format(LocalizationManager.L("Modal_WorkshopUpgrade_SuccessBody", "Tebrikler! Atölyeniz başarıyla **{0}** seviyesine genişletildi.", "Congratulations! Your workshop has been successfully expanded to **{0}**."), stageNames[targetLvl - 1]);
                                string sucOk = LocalizationManager.L("Btn_Ok", "Harika!", "Awesome!");
                                ModalManager.ShowModal(sucTitle, sucBody, sucOk);
                            }
                        }, btnConfirm, btnCancel);
                    });
                }

                GameObject btnTxtObj = new GameObject("Text");
                btnTxtObj.transform.SetParent(btnObj.transform, false);

                RectTransform btRect = btnTxtObj.AddComponent<RectTransform>();
                btRect.anchorMin = Vector2.zero;
                btRect.anchorMax = Vector2.one;

                Text btnTxt = btnTxtObj.AddComponent<Text>();
                btnTxt.font = globalFont;
                btnTxt.text = btnLabelText;
                btnTxt.fontSize = 15;
                btnTxt.fontStyle = FontStyle.Bold;
                btnTxt.alignment = TextAnchor.MiddleCenter;
                btnTxt.color = isLocked ? new Color(0.60f, 0.65f, 0.70f) : Color.white;
                btnTxt.raycastTarget = false;
            }
        }

        private void RenderWorkshopMachinesList()
        {
            if (workshopMachinesContent == null) return;

            foreach (Transform child in workshopMachinesContent)
            {
                Destroy(child.gameObject);
            }

            // Sahnedeki tüm kurulu makineleri topla
            List<WorkshopMachineController> placedMachines = new List<WorkshopMachineController>();
            if (WorkshopMachineController.AllPlacedMachines != null)
            {
                for (int i = 0; i < WorkshopMachineController.AllPlacedMachines.Count; i++)
                {
                    var m = WorkshopMachineController.AllPlacedMachines[i];
                    if (m != null) placedMachines.Add(m);
                }
            }

            // 0 Makine varsa Empty State göster
            if (placedMachines.Count == 0)
            {
                GameObject emptyObj = new GameObject("Empty_State_Panel");
                emptyObj.transform.SetParent(workshopMachinesContent, false);

                RectTransform emptyRect = emptyObj.AddComponent<RectTransform>();
                emptyRect.sizeDelta = new Vector2(820f, 260f);

                Image emptyBg = emptyObj.AddComponent<Image>();
                emptyBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 260, 18, 2, new Color(0.35f, 0.45f, 0.60f, 0.65f), new Color(0.10f, 0.13f, 0.18f, 0.95f));
                emptyBg.raycastTarget = false;

                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(emptyObj.transform, false);
                RectTransform iRect = iconObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(0f, 65f);
                iRect.sizeDelta = new Vector2(100f, 50f);
                Text iconTxt = iconObj.AddComponent<Text>();
                iconTxt.font = globalFont;
                iconTxt.text = "🏭";
                iconTxt.fontSize = 44;
                iconTxt.alignment = TextAnchor.MiddleCenter;

                GameObject titleObj = new GameObject("Title");
                titleObj.transform.SetParent(emptyObj.transform, false);
                RectTransform tRect = titleObj.AddComponent<RectTransform>();
                tRect.anchoredPosition = new Vector2(0f, 15f);
                tRect.sizeDelta = new Vector2(600f, 35f);
                Text tTxt = titleObj.AddComponent<Text>();
                tTxt.font = globalFont;
                tTxt.text = LocalizationManager.L("WS_EmptyMachinesTitle", "Henüz Kurulu Atölye Makinesi Bulunmuyor", "No Workshop Machines Installed Yet");
                tTxt.fontSize = 18;
                tTxt.fontStyle = FontStyle.Bold;
                tTxt.alignment = TextAnchor.MiddleCenter;
                tTxt.color = new Color(0.95f, 0.70f, 0.20f);

                GameObject descObj = new GameObject("Desc");
                descObj.transform.SetParent(emptyObj.transform, false);
                RectTransform dRect = descObj.AddComponent<RectTransform>();
                dRect.anchoredPosition = new Vector2(0f, -30f);
                dRect.sizeDelta = new Vector2(650f, 50f);
                Text dTxt = descObj.AddComponent<Text>();
                dTxt.font = globalFont;
                dTxt.text = LocalizationManager.L(
                    "WS_EmptyMachinesDesc",
                    "TrendyShop Alışveriş uygulamasından 'Atölye Makineleri' satın alabilir ve Atölye Paletinden istediğiniz konuma kurabilirsiniz.",
                    "You can purchase 'Workshop Machines' from the TrendyShop and assemble them inside your workshop from the pallet."
                );
                dTxt.fontSize = 14;
                dTxt.alignment = TextAnchor.MiddleCenter;
                dTxt.color = new Color(0.75f, 0.82f, 0.90f);

                // Alışverişe Git Butonu
                GameObject shopBtnObj = new GameObject("GoToShopBtn");
                shopBtnObj.transform.SetParent(emptyObj.transform, false);
                RectTransform sbRect = shopBtnObj.AddComponent<RectTransform>();
                sbRect.anchoredPosition = new Vector2(0f, -85f);
                sbRect.sizeDelta = new Vector2(260f, 42f);

                Image sbBg = shopBtnObj.AddComponent<Image>();
                sbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(260, 42, 12, new Color(0.95f, 0.55f, 0.15f));
                sbBg.raycastTarget = true;

                Button sbBtn = shopBtnObj.AddComponent<Button>();
                sbBtn.targetGraphic = sbBg;
                sbBtn.onClick.AddListener(() => {
                    activeShoppingCategory = 11; // Atölye Makineleri Kategorisi
                    ShowShoppingApp();
                });

                GameObject sbTxtObj = new GameObject("Text");
                sbTxtObj.transform.SetParent(shopBtnObj.transform, false);
                RectTransform sbtRect = sbTxtObj.AddComponent<RectTransform>();
                sbtRect.anchorMin = Vector2.zero;
                sbtRect.anchorMax = Vector2.one;
                Text sbTxt = sbTxtObj.AddComponent<Text>();
                sbTxt.font = globalFont;
                sbTxt.text = LocalizationManager.L("WS_Btn_GoToShop", "🛍️ Atölye Makinelerine Git", "🛍️ Go to Workshop Machines");
                sbTxt.fontSize = 15;
                sbTxt.fontStyle = FontStyle.Bold;
                sbTxt.alignment = TextAnchor.MiddleCenter;
                sbTxt.color = Color.white;

                return;
            }

            // Kurulu makineleri türlerine göre numaralandır
            Dictionary<WorkshopMachineType, int> machineSeq = new Dictionary<WorkshopMachineType, int>();

            for (int i = 0; i < placedMachines.Count; i++)
            {
                WorkshopMachineController machine = placedMachines[i];
                if (machine == null) continue;

                int seq = machineSeq.GetValueOrDefault(machine.machineType, 0) + 1;
                machineSeq[machine.machineType] = seq;

                WorkshopMachineDef mDef = WorkshopMachineDatabase.GetMachineByType(machine.machineType);
                string mName = (mDef != null) ? mDef.LocalizedName : "Atölye Makinesi";

                GameObject cardObj = new GameObject("MachineCard_" + i);
                cardObj.transform.SetParent(workshopMachinesContent, false);

                RectTransform cRect = cardObj.AddComponent<RectTransform>();
                cRect.sizeDelta = new Vector2(820f, 95f);

                Color borderClr;
                if (machine.isReadyToCollect) borderClr = new Color(0.20f, 0.85f, 0.40f);
                else if (machine.isProducing) borderClr = new Color(0.95f, 0.60f, 0.15f);
                else borderClr = new Color(0.35f, 0.45f, 0.58f, 0.85f);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 95, 14, 2, borderClr, new Color(0.11f, 0.14f, 0.20f, 0.96f));
                cardBg.raycastTarget = false;

                // 1. İkon Kutusu
                GameObject iconBox = new GameObject("Icon_Box");
                iconBox.transform.SetParent(cardObj.transform, false);
                RectTransform ibRect = iconBox.AddComponent<RectTransform>();
                ibRect.anchoredPosition = new Vector2(-360f, 0f);
                ibRect.sizeDelta = new Vector2(60f, 60f);

                Image ibImg = iconBox.AddComponent<Image>();
                FurnitureType fType = GetFurnitureTypeForMachine(machine.machineType);
                ibImg.sprite = UIStyleUtility.CreateFurnitureIconSprite(fType);
                ibImg.raycastTarget = false;

                // 2. Makine ve Durum Bilgi Paneli
                GameObject infoObj = new GameObject("Info_Panel");
                infoObj.transform.SetParent(cardObj.transform, false);
                RectTransform inRect = infoObj.AddComponent<RectTransform>();
                inRect.anchoredPosition = new Vector2(-50f, 0f);
                inRect.sizeDelta = new Vector2(520f, 80f);

                Text infoTxt = infoObj.AddComponent<Text>();
                infoTxt.font = globalFont;
                infoTxt.fontSize = 15;
                infoTxt.alignment = TextAnchor.MiddleLeft;
                infoTxt.raycastTarget = false;

                if (machine.isReadyToCollect)
                {
                    WorkshopRecipeDef rDef = WorkshopMachineDatabase.GetRecipeById(machine.activeRecipeId);
                    string rName = (rDef != null) ? rDef.LocalizedName : "Ürün";
                    string rEmoji = (rDef != null) ? rDef.iconEmoji : "✨";
                    string readyTag = LocalizationManager.L("WS_StatusReadyTag", "HAZIR!", "READY!");
                    string readySub = LocalizationManager.L("WS_StatusReadySub", "Üretim tamamlandı, doğrudan ahıra aktarabilirsiniz.", "Crafting complete, you can collect it to barn storage.");
                    infoTxt.text = $"<b>{mName} #{seq}</b>   |   <color=#00E676><b>🎉 {rEmoji} {rName} {readyTag}</b></color>\n<size=13><color=#90CAF9>{readySub}</color></size>";
                }
                else if (machine.isProducing)
                {
                    WorkshopRecipeDef rDef = WorkshopMachineDatabase.GetRecipeById(machine.activeRecipeId);
                    string rName = (rDef != null) ? rDef.LocalizedName : "Ürün";
                    string rEmoji = (rDef != null) ? rDef.iconEmoji : "⏳";
                    int mins = Mathf.FloorToInt(machine.remainingSeconds / 60f);
                    int secs = Mathf.FloorToInt(machine.remainingSeconds % 60f);
                    string craftTag = LocalizationManager.L("WS_StatusCraftingTag", "Üretiliyor:", "Crafting:");
                    string craftSub = LocalizationManager.L("WS_StatusCraftingSub", "Gerçek zamanlı üretim devam ediyor...", "Real-time production in progress...");
                    infoTxt.text = $"<b>{mName} #{seq}</b>   |   <color=#FFA726><b>⏳ {rEmoji} {rName} {craftTag}</b></color> <color=#00FFD5><b>{mins:00}:{secs:00}</b></color>\n<size=13><color=#B0BEC5>{craftSub}</color></size>";
                }
                else
                {
                    string idleTag = LocalizationManager.L("WS_StatusIdleTag", "Boşta (Üretim Bekliyor)", "Idle (Waiting for Crafting)");
                    string idleSub = LocalizationManager.L("WS_StatusIdleSub", "Hammadde seçip yeni bir gurme üretimi başlatabilirsiniz.", "Select raw material to start crafting.");
                    infoTxt.text = $"<b>{mName} #{seq}</b>   |   <color=#80D8FF><b>💤 {idleTag}</b></color>\n<size=13><color=#78909C>{idleSub}</color></size>";
                }

                // 3. Aksiyon Butonu
                GameObject btnObj = new GameObject("Action_Btn");
                btnObj.transform.SetParent(cardObj.transform, false);
                RectTransform bRect = btnObj.AddComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(285f, 0f);
                bRect.sizeDelta = new Vector2(210f, 44f);

                Image bBg = btnObj.AddComponent<Image>();
                Color btnCol;
                string btnTextStr;

                var targetMachine = machine;
                if (machine.isReadyToCollect)
                {
                    btnCol = new Color(0.18f, 0.78f, 0.38f);
                    btnTextStr = LocalizationManager.L("Btn_CollectToBarn", "📦 AHIRA TOPLA", "📦 COLLECT TO BARN");
                }
                else if (machine.isProducing)
                {
                    btnCol = new Color(0.18f, 0.55f, 0.85f);
                    btnTextStr = LocalizationManager.L("Btn_FocusMachine", "🔍 MAKİNEYE GİT", "🔍 FOCUS MACHINE");
                }
                else
                {
                    btnCol = new Color(0.95f, 0.55f, 0.15f);
                    btnTextStr = LocalizationManager.L("Btn_StartCrafting", "▶️ ÜRETİM BAŞLAT", "▶️ START CRAFT");
                }

                bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(210, 44, 12, btnCol);
                bBg.raycastTarget = true;

                Button actionBtn = btnObj.AddComponent<Button>();
                actionBtn.targetGraphic = bBg;

                if (machine.isReadyToCollect)
                {
                    actionBtn.onClick.AddListener(() => {
                        targetMachine.CollectFinishedProduct();
                        RefreshWorkshopsViews();
                    });
                }
                else if (machine.isProducing)
                {
                    actionBtn.onClick.AddListener(() => {
                        ClosePhoneTabletInstant();
                        if (IsometricCameraSetup.Instance != null)
                        {
                            IsometricCameraSetup.Instance.FocusOn(targetMachine.transform.position);
                        }
                    });
                }
                else
                {
                    actionBtn.onClick.AddListener(() => {
                        ClosePhoneTabletInstant();
                        WorkshopMachineModalUI.ShowModal(targetMachine);
                    });
                }

                GameObject btObj = new GameObject("Text");
                btObj.transform.SetParent(btnObj.transform, false);
                RectTransform btRect = btObj.AddComponent<RectTransform>();
                btRect.anchorMin = Vector2.zero;
                btRect.anchorMax = Vector2.one;

                Text btnTxt = btObj.AddComponent<Text>();
                btnTxt.font = globalFont;
                btnTxt.text = btnTextStr;
                btnTxt.fontSize = 14;
                btnTxt.fontStyle = FontStyle.Bold;
                btnTxt.alignment = TextAnchor.MiddleCenter;
                btnTxt.color = Color.white;
                btnTxt.raycastTarget = false;

                // 4. Şık Ayraç Çizgisi (Her makine arasında karışıklığı önleyen divider)
                GameObject divObj = new GameObject("Row_Divider_" + i);
                divObj.transform.SetParent(workshopMachinesContent, false);
                RectTransform divRect = divObj.AddComponent<RectTransform>();
                divRect.sizeDelta = new Vector2(810f, 2f);

                Image divImg = divObj.AddComponent<Image>();
                divImg.sprite = UIStyleUtility.CreateRoundedPillSprite(810, 2, 1, new Color(0.25f, 0.35f, 0.50f, 0.40f));
                divImg.raycastTarget = false;
            }
        }

        private FurnitureType GetFurnitureTypeForMachine(WorkshopMachineType mType)
        {
            switch (mType)
            {
                case WorkshopMachineType.JamMaker: return FurnitureType.WorkshopJamMaker;
                case WorkshopMachineType.JuiceExtractor: return FurnitureType.WorkshopJuicePress;
                case WorkshopMachineType.Cannery: return FurnitureType.WorkshopCannery;
                case WorkshopMachineType.Dehydrator: return FurnitureType.WorkshopDehydrator;
                case WorkshopMachineType.OilPress: return FurnitureType.WorkshopOilPress;
                case WorkshopMachineType.SaladStation: return FurnitureType.WorkshopSaladStation;
                default: return FurnitureType.WorkshopJamMaker;
            }
        }

        private void RefreshShoppingViews()
        {
            RenderShoppingCategoryList();
            RenderShoppingCategoryContent();
        }

        private void RenderShoppingCategoryList()
        {
            if (shoppingCategoryContent == null) return;

            Color accentColor = new Color(0.95f, 0.40f, 0.55f);
            string[] categories = GetShoppingCategories();

            // Eğer butonlar zaten mevcutsa yeniden oluşturmak yerine sadece stilleri ve renkleri güncelle (0 ms gecikme)
            if (shoppingCategoryContent.childCount == categories.Length)
            {
                for (int i = 0; i < categories.Length; i++)
                {
                    bool isActive = (i == activeShoppingCategory);
                    Transform btnTrans = shoppingCategoryContent.GetChild(i);
                    Image catBg = btnTrans.GetComponent<Image>();
                    if (catBg != null)
                    {
                        catBg.sprite = isActive
                            ? UIStyleUtility.CreateOutlinePillSprite(230, 46, 14, 2, accentColor, new Color(0.35f, 0.12f, 0.20f, 0.95f))
                            : UIStyleUtility.CreateRoundedPillSprite(230, 46, 14, new Color(0.14f, 0.18f, 0.24f, 0.85f));
                    }
                    Text catText = btnTrans.GetComponentInChildren<Text>();
                    if (catText != null)
                    {
                        catText.text = categories[i];
                        catText.color = isActive ? Color.white : new Color(0.80f, 0.85f, 0.90f);
                    }
                }
                return;
            }

            foreach (Transform child in shoppingCategoryContent) Destroy(child.gameObject);

            for (int i = 0; i < categories.Length; i++)
            {
                int catIdx = i;
                bool isActive = (i == activeShoppingCategory);

                GameObject catBtn = new GameObject("CategoryBtn_" + i);
                catBtn.transform.SetParent(shoppingCategoryContent, false);

                LayoutElement lElem = catBtn.AddComponent<LayoutElement>();
                lElem.minHeight = 46f;
                lElem.preferredHeight = 46f;

                Image catBg = catBtn.AddComponent<Image>();
                if (isActive)
                {
                    catBg.sprite = UIStyleUtility.CreateOutlinePillSprite(230, 46, 14, 2, accentColor, new Color(0.35f, 0.12f, 0.20f, 0.95f));
                }
                else
                {
                    catBg.sprite = UIStyleUtility.CreateRoundedPillSprite(230, 46, 14, new Color(0.14f, 0.18f, 0.24f, 0.85f));
                }
                catBg.raycastTarget = true;

                Button btn = catBtn.AddComponent<Button>();
                btn.targetGraphic = catBg;
                btn.onClick.AddListener(() => {
                    activeShoppingCategory = catIdx;
                    RefreshShoppingViews();
                });

                Text catText = CreateTextInPanel(catBtn.transform, Vector2.zero, Vector2.one, categories[i], 17, isActive ? Color.white : new Color(0.80f, 0.85f, 0.90f));
                catText.alignment = TextAnchor.MiddleLeft;
                RectTransform tRect = catText.GetComponent<RectTransform>();
                tRect.anchoredPosition = new Vector2(12f, 0f);
            }
        }

        private string[] GetShoppingCategories()
        {
            return new string[] {
                LocalizationManager.L("Cat_Furniture", "🛋️ Mobilyalar", "🛋️ Furniture"),
                LocalizationManager.L("Cat_Decoration", "🎨 Dekorasyonlar", "🎨 Decorations"),
                LocalizationManager.L("Cat_Wholesale", "📦 Toptancı", "📦 Wholesaler"),
                LocalizationManager.L("Cat_Seeds", "🌱 Tohumlar", "🌱 Seeds"),
                LocalizationManager.L("Cat_Renovation", "🔨 Tadilat", "🔨 Renovation"),
                LocalizationManager.L("Cat_Workshop", "🏭 Atölye Makineleri", "🏭 Workshop Machines"),
                LocalizationManager.L("Cat_Vehicles", "🛵 Araçlar", "🛵 Vehicles")
            };
        }

        private void RenderShoppingCategoryContent()
        {
            string[] categories = GetShoppingCategories();
            if (activeShoppingCategory >= 0 && activeShoppingCategory < categories.Length)
            {
                string catName = categories[activeShoppingCategory];
                if (shoppingCategoryHeaderTitle != null)
                {
                    shoppingCategoryHeaderTitle.text = catName;
                }

                if (shoppingCategoryHeaderSub != null) shoppingCategoryHeaderSub.gameObject.SetActive(false);
                if (furnitureViewportObj != null) furnitureViewportObj.gameObject.SetActive(true);
                if (shoppingCartSummaryPanelObj != null) shoppingCartSummaryPanelObj.SetActive(activeShoppingCategory != 4 && activeShoppingCategory != 6);

                if (activeShoppingCategory == 6)
                {
                    RenderVehiclesList();
                }
                else if (activeShoppingCategory == 5)
                {
                    RenderFurnitureList(FurnitureCategory.Workshop);
                }
                else if (activeShoppingCategory == 4)
                {
                    RenderRenovationList();
                }
                else if (activeShoppingCategory == 3)
                {
                    RenderSeedProductList();
                }
                else if (activeShoppingCategory == 2)
                {
                    RenderWholesaleProductList();
                }
                else
                {
                    FurnitureCategory targetCat = (activeShoppingCategory == 0) ? FurnitureCategory.Furniture : FurnitureCategory.Decoration;
                    RenderFurnitureList(targetCat);
                }
            }
        }

        private void RenderVehiclesList()
        {
            if (furnitureListContent == null) return;
            foreach (Transform child in furnitureListContent) Destroy(child.gameObject);

            int ownedCount = (CourierManager.Instance != null) ? CourierManager.Instance.OwnedMotorcycleCount : 0;
            int maxCount = CourierManager.MAX_MOTORCYCLES;

            GameObject cardObj = new GameObject("VehicleCard_CourierMotorcycle");
            cardObj.transform.SetParent(furnitureListContent, false);

            LayoutElement lElem = cardObj.AddComponent<LayoutElement>();
            lElem.minHeight = 110f;
            lElem.preferredHeight = 110f;

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(520, 110, 14, 1, new Color(0.12f, 0.75f, 0.95f, 0.7f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

            // Sol Emoji İkonu Kutusu
            GameObject iconBox = new GameObject("IconBox");
            iconBox.transform.SetParent(cardObj.transform, false);
            RectTransform ibRect = iconBox.AddComponent<RectTransform>();
            ibRect.anchoredPosition = new Vector2(-215f, 0f);
            ibRect.sizeDelta = new Vector2(64f, 64f);

            Image ibBg = iconBox.AddComponent<Image>();
            ibBg.sprite = UIStyleUtility.CreateRoundedPillSprite(64, 64, 14, new Color(0.15f, 0.22f, 0.32f));

            Text icoTxt = CreateTextInPanel(iconBox.transform, Vector2.zero, Vector2.one, "🛵", 32, Color.white);
            icoTxt.alignment = TextAnchor.MiddleCenter;

            // Orta Bilgi Alanı
            GameObject infoPanel = new GameObject("InfoPanel");
            infoPanel.transform.SetParent(cardObj.transform, false);
            RectTransform ipRect = infoPanel.AddComponent<RectTransform>();
            ipRect.anchoredPosition = new Vector2(-15f, 0f);
            ipRect.sizeDelta = new Vector2(310f, 95f);

            string titleStr = LocalizationManager.L("Veh_MotoTitle", "🛵 Kurye Motorsikleti", "🛵 Courier Motorcycle");
            Text titleText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 26f), new Vector2(310f, 24f), titleStr, 20, Color.white);
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;

            string priceFmt = LocalizationManager.L("Veh_PriceFmt", "Fiyat: {0:N0}C | Kapasite: {1}/{2} Adet", "Price: {0:N0}C | Fleet: {1}/{2} Bikes");
            Text priceText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 2f), new Vector2(310f, 20f), string.Format(priceFmt, CourierManager.MOTORCYCLE_PRICE, ownedCount, maxCount), 16, new Color(0.95f, 0.85f, 0.30f));
            priceText.alignment = TextAnchor.MiddleLeft;

            string descStr = LocalizationManager.L("Veh_MotoDesc", "⚡ Hızlı Dağıtım | Termal Koli Sepeti | Gece Farı", "⚡ Fast Delivery | Thermal Cargo Box | Night Light");
            Text subText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -22f), new Vector2(310f, 20f), descStr, 14, new Color(0.40f, 0.80f, 1.0f));
            subText.alignment = TextAnchor.MiddleLeft;

            // Sağ Satın Alma Butonu
            GameObject btnObj = new GameObject("BuyVehicleBtn");
            btnObj.transform.SetParent(cardObj.transform, false);
            RectTransform bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(195f, 0f);
            bRect.sizeDelta = new Vector2(120f, 44f);

            bool canBuy = (ownedCount < maxCount);
            Image bBg = btnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(120, 44, 14, canBuy ? new Color(0.15f, 0.70f, 0.35f) : new Color(0.35f, 0.38f, 0.45f));

            if (canBuy)
            {
                Button bBtn = btnObj.AddComponent<Button>();
                bBtn.targetGraphic = bBg;
                bBtn.onClick.AddListener(() => {
                    if (CourierManager.Instance != null && CourierManager.Instance.TryBuyMotorcycle())
                    {
                        ModalManager.ShowModal(
                            LocalizationManager.L("Modal_MotoBuy_Title", "Motorsiklet Satın Alındı! 🛵", "Motorcycle Purchased! 🛵"),
                            LocalizationManager.L("Modal_MotoBuy_Desc", "Kurye motorsikleti satın alındı ve dükkan yanındaki sarı park yerine yerleştirildi!\n\nOnline Market uygulamasından kurye personeli atayarak teslimatlara başlayabilirsiniz.", "Courier motorcycle purchased and parked at the delivery bay!\n\nAssign a courier in the Online Market app to start delivery operations."),
                            LocalizationManager.L("Btn_Ok", "Harika!", "Awesome!")
                        );
                        RefreshShoppingViews();
                    }
                    else
                    {
                        ModalManager.ShowModal(
                            LocalizationManager.L("Modal_NotEnough_Title", "Yetersiz Bakiye! ⚠️", "Insufficient Credits! ⚠️"),
                            LocalizationManager.L("Modal_NotEnough_Desc", $"Motorsiklet satın alabilmek için {CourierManager.MOTORCYCLE_PRICE:N0}C bakiyeniz olmalıdır.", $"You need {CourierManager.MOTORCYCLE_PRICE:N0}C to purchase a motorcycle."),
                            LocalizationManager.L("Btn_Ok", "Tamam", "OK")
                        );
                    }
                });

                Text btnTxt = CreateTextInPanel(btnObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_Buy", "🛒 Satın Al", "🛒 Buy"), 16, Color.white);
                btnTxt.alignment = TextAnchor.MiddleCenter;
                btnTxt.fontStyle = FontStyle.Bold;
            }
            else
            {
                Text btnTxt = CreateTextInPanel(btnObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_MaxLimit", "🔒 Dolu (5/5)", "🔒 Max (5/5)"), 15, Color.white);
                btnTxt.alignment = TextAnchor.MiddleCenter;
                btnTxt.fontStyle = FontStyle.Bold;
            }

            UpdateCartSummary();
        }

        private void RenderWholesaleProductList()
        {
            if (furnitureListContent == null) return;
            foreach (Transform child in furnitureListContent) Destroy(child.gameObject);

            int currentLevel = (Farm2Shelf.Environment.EnvironmentBuilder.Instance != null)
                ? Farm2Shelf.Environment.EnvironmentBuilder.Instance.CurrentUpgradeLevel
                : 1;

            List<WholesaleProductDef> items = WholesaleDatabase.GetWholesaleOnlyProducts();

            // Aktif Sekmedeki Toptancı Ürünlerini Arama Filtresine Göre Süzme
            string query = string.IsNullOrEmpty(currentShoppingSearchQuery) ? "" : currentShoppingSearchQuery.Trim().ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
            if (!string.IsNullOrEmpty(query))
            {
                items = items.FindAll(i => 
                    i.name.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(query) || 
                    i.GetTargetShelfText().ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(query)
                );
            }

            if (items.Count == 0)
            {
                GameObject emptyMsgObj = new GameObject("SearchEmptyMsg");
                emptyMsgObj.transform.SetParent(furnitureListContent, false);
                LayoutElement el = emptyMsgObj.AddComponent<LayoutElement>();
                el.minHeight = 120f;
                el.preferredHeight = 120f;

                Text emptyTxt = CreateTextInPanel(emptyMsgObj.transform, Vector2.zero, Vector2.one, $"🔍 '{currentShoppingSearchQuery}' araması için Toptancı sekmesinde ürün bulunamadı.", 17, Color.gray);
                emptyTxt.alignment = TextAnchor.MiddleCenter;
                UpdateCartSummary();
                return;
            }

            foreach (var item in items)
            {
                WholesaleProductDef def = item;
                bool isUnlocked = (currentLevel >= def.requiredLevel);
                int inCartCount = wholesaleCart.ContainsKey(def.id) ? wholesaleCart[def.id] : 0;

                GameObject cardObj = new GameObject("WholesaleCard_" + def.id);
                cardObj.transform.SetParent(furnitureListContent, false);

                LayoutElement lElem = cardObj.AddComponent<LayoutElement>();
                lElem.minHeight = 84f;
                lElem.preferredHeight = 84f;

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(520, 84, 12, 1, new Color(0.95f, 0.55f, 0.20f, 0.6f), new Color(0.14f, 0.16f, 0.22f, 0.90f));

                // Sol Emoji İkonu Kutusu
                GameObject iconBox = new GameObject("IconBox");
                iconBox.transform.SetParent(cardObj.transform, false);
                RectTransform ibRect = iconBox.AddComponent<RectTransform>();
                ibRect.anchoredPosition = new Vector2(-225f, 0f);
                ibRect.sizeDelta = new Vector2(50f, 50f);

                Image ibBg = iconBox.AddComponent<Image>();
                ibBg.sprite = UIStyleUtility.CreateWholesaleIconSprite(def.id, def.iconEmoji, new Color(0.95f, 0.55f, 0.20f));

                // Orta Bilgi Alanı (Başlık, Toptan Alış vs Tavsiye Satış %20 Kâr, Seviye Kilit Rozeti)
                GameObject infoPanel = new GameObject("InfoPanel");
                infoPanel.transform.SetParent(cardObj.transform, false);
                RectTransform ipRect = infoPanel.AddComponent<RectTransform>();
                ipRect.anchoredPosition = new Vector2(-30f, 0f);
                ipRect.sizeDelta = new Vector2(300f, 75f);

                Text titleText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 22f), new Vector2(300f, 24f), $"{def.iconEmoji} {def.LocalizedName} (50)", 19, Color.white);
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleLeft;

                string priceInfoFmt = LocalizationManager.L("Wholesale_PriceInfoFmt", "Toptan Koli Alış: {0:N0}C ({1:N0}C/Birim) | Kâr: +{2:N0}C (%20)", "Wholesale Pack Cost: {0:N0}C ({1:N0}C/Pcs) | Profit: +{2:N0}C (20%)");
                string priceInfo = string.Format(priceInfoFmt, def.TotalPackCost, def.wholesaleUnitPrice, def.TotalPackProfit);
                Text priceText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 0f), new Vector2(300f, 20f), priceInfo, 16, new Color(0.95f, 0.85f, 0.30f));
                priceText.alignment = TextAnchor.MiddleLeft;

                string badgeUnlockedFmt = LocalizationManager.L("Wholesale_UnlockedBadge", "✅ Seviye {0} | {1} (50 Adet)", "✅ Level {0} | {1} (50 Pcs)");
                string badgeLockedFmt = LocalizationManager.L("Wholesale_LockedBadge", "🔒 Seviye {0} Gereklidir | {1}", "🔒 Requires Level {0} | {1}");
                string badgeText = isUnlocked ? string.Format(badgeUnlockedFmt, def.requiredLevel, def.GetTargetShelfText()) : string.Format(badgeLockedFmt, def.requiredLevel, def.GetTargetShelfText());
                Color badgeColor = isUnlocked ? new Color(0.30f, 0.85f, 0.45f) : new Color(0.95f, 0.45f, 0.35f);

                Text subText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -20f), new Vector2(300f, 20f), badgeText, 15, badgeColor);
                subText.alignment = TextAnchor.MiddleLeft;

                // Sağ Kontrol Alanı
                GameObject ctrlPanel = new GameObject("CtrlPanel");
                ctrlPanel.transform.SetParent(cardObj.transform, false);
                RectTransform cpRect = ctrlPanel.AddComponent<RectTransform>();
                cpRect.anchoredPosition = new Vector2(190f, 0f);
                cpRect.sizeDelta = new Vector2(130f, 50f);

                UpdateWholesaleCardControls(ctrlPanel.transform, def, isUnlocked);
            }

            UpdateCartSummary();
        }

        private void UpdateWholesaleCardControls(Transform ctrlParent, WholesaleProductDef def, bool isUnlocked)
        {
            if (ctrlParent == null || def == null) return;
            if (!def.isOrderable || !WholesaleDatabase.IsProductWholesaleOrderable(def.id))
            {
                // Çiftlik mahsulleri ve Atölye gurme ürünleri toptancıdan veya market alışverişinden sipariş edilemez!
                return;
            }
            foreach (Transform child in ctrlParent) Destroy(child.gameObject);

            string targetProdId = def.id;
            int inCartCount = wholesaleCart.ContainsKey(targetProdId) ? wholesaleCart[targetProdId] : 0;

            if (isUnlocked)
            {
                if (inCartCount > 0)
                {
                    // "-" Butonu (Büyütülmüş & Net Okunabilir)
                    GameObject minusBtn = CreateButtonInPanel(ctrlParent, new Vector2(-44f, 0f), new Vector2(40f, 40f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                        if (wholesaleCart.ContainsKey(targetProdId))
                        {
                            wholesaleCart[targetProdId]--;
                            if (wholesaleCart[targetProdId] <= 0) wholesaleCart.Remove(targetProdId);
                        }
                        UpdateWholesaleCardControls(ctrlParent, def, isUnlocked);
                        UpdateCartSummary();
                    }, 28);

                    // Adet Göstergesi (Büyütülmüş Sayı)
                    Text countTxt = CreateTextInPanel(ctrlParent, new Vector2(0f, 0f), new Vector2(36f, 40f), inCartCount.ToString(), 24, Color.white);
                    countTxt.fontStyle = FontStyle.Bold;
                    countTxt.alignment = TextAnchor.MiddleCenter;

                    // "+" Butonu (Büyütülmüş & Net Okunabilir)
                    GameObject plusBtn = CreateButtonInPanel(ctrlParent, new Vector2(44f, 0f), new Vector2(40f, 40f), "+", new Color(0.30f, 0.75f, 0.40f), () => {
                        if (!wholesaleCart.ContainsKey(targetProdId)) wholesaleCart[targetProdId] = 0;
                        wholesaleCart[targetProdId]++;
                        UpdateWholesaleCardControls(ctrlParent, def, isUnlocked);
                        UpdateCartSummary();
                    }, 28);
                }
                else
                {
                    // "+ Koli Ekle" Butonu
                    string btnAddPackLabel = LocalizationManager.L("Btn_AddPack", "+ Koli Ekle", "+ Add Pack");
                    GameObject addBtn = CreateButtonInPanel(ctrlParent, new Vector2(0f, 0f), new Vector2(110f, 38f), btnAddPackLabel, new Color(0.95f, 0.55f, 0.20f), () => {
                        wholesaleCart[targetProdId] = 1;
                        UpdateWholesaleCardControls(ctrlParent, def, isUnlocked);
                        UpdateCartSummary();
                    }, 17);
                }
            }
            else
            {
                // Kilitli Buton
                string lockTextStr = LocalizationManager.L("Btn_LockedItem", "🔒 Kilitli", "🔒 Locked");
                GameObject lockBtn = CreateButtonInPanel(ctrlParent, new Vector2(0f, 0f), new Vector2(100f, 34f), lockTextStr, new Color(0.35f, 0.35f, 0.40f), null, 15);
            }
        }

        private void RenderSeedProductList()
        {
            if (furnitureListContent == null) return;
            foreach (Transform child in furnitureListContent) Destroy(child.gameObject);

            int currentLevel = (Farm2Shelf.Environment.EnvironmentBuilder.Instance != null) ? Farm2Shelf.Environment.EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            TimeManager.Season currentSeason = (TimeManager.Instance != null) ? TimeManager.Instance.CurrentSeason : TimeManager.Season.İlkbahar;

            List<GardenSeedDef> items = GardenSeedDatabase.GetAllSeeds();

            // Akıllı Dinamik Sıralama:
            // 1. Satın Alınabilir Tohumlar (Hem Mevsimi Uygun Hem Seviyesi Yeterli) EN ÜSTTE
            // 2. Mevsimi Uyan Tohumlar
            // 3. Seviye Gereksinimine Göre Küçükten Büyüğe (Kilitliler En Altta)
            items.Sort((a, b) => {
                bool canBuyA = (a.season == currentSeason) && (currentLevel >= a.requiredLevel);
                bool canBuyB = (b.season == currentSeason) && (currentLevel >= b.requiredLevel);

                if (canBuyA != canBuyB) return canBuyB.CompareTo(canBuyA);

                bool isSeasonA = (a.season == currentSeason);
                bool isSeasonB = (b.season == currentSeason);
                if (isSeasonA != isSeasonB) return isSeasonB.CompareTo(isSeasonA);

                if (a.requiredLevel != b.requiredLevel) return a.requiredLevel.CompareTo(b.requiredLevel);
                return a.growthDays.CompareTo(b.growthDays);
            });

            string query = string.IsNullOrEmpty(currentShoppingSearchQuery) ? "" : currentShoppingSearchQuery.Trim().ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
            if (!string.IsNullOrEmpty(query))
            {
                items = items.FindAll(i => i.name.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(query));
            }

            foreach (var sDef in items)
            {
                GardenSeedDef def = sDef;
                bool isMatchingSeason = (def.season == currentSeason);
                bool isLevelUnlocked = (currentLevel >= def.requiredLevel);
                bool canBuy = isMatchingSeason && isLevelUnlocked;

                int ownedCount = GardenSeedInventoryManager.Instance.GetSeedCount(def.id);

                GameObject cardObj = new GameObject("SeedCard_" + def.id);
                cardObj.transform.SetParent(furnitureListContent, false);

                LayoutElement cElem = cardObj.AddComponent<LayoutElement>();
                cElem.minHeight = 84f;
                cElem.preferredHeight = 84f;

                Image cardBg = cardObj.AddComponent<Image>();
                Color outlineCol = canBuy ? def.cropColor : Color.gray;
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(520, 84, 12, 1, outlineCol, new Color(0.14f, 0.16f, 0.22f, 0.90f));

                // 1. Sol İkon Kutusu
                GameObject iconBox = new GameObject("IconBox");
                iconBox.transform.SetParent(cardObj.transform, false);
                RectTransform ibRect = iconBox.AddComponent<RectTransform>();
                ibRect.anchoredPosition = new Vector2(-225f, 0f);
                ibRect.sizeDelta = new Vector2(46f, 46f);

                Image ibBg = iconBox.AddComponent<Image>();
                ibBg.sprite = UIStyleUtility.CreateSeedIconSprite(def.id, def.iconEmoji, outlineCol);

                // 2. Orta Bilgi Alanı
                GameObject infoPanel = new GameObject("InfoPanel");
                infoPanel.transform.SetParent(cardObj.transform, false);
                RectTransform ipRect = infoPanel.AddComponent<RectTransform>();
                ipRect.anchoredPosition = new Vector2(-20f, 0f);
                ipRect.sizeDelta = new Vector2(310f, 75f);

                string inStockFmt = LocalizationManager.L("Seed_InStockFmt", "(Stokta: {0})", "(In Stock: {0})");
                Text titleText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 18f), new Vector2(310f, 24f), $"<b>{def.LocalizedName}</b>  <color=#00E676>{string.Format(inStockFmt, ownedCount)}</color>", 19, canBuy ? Color.white : Color.gray);
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleLeft;

                string statusFmt = LocalizationManager.L("Seed_StatusFmt", "• Büyüme: {0} Gün • Seviye: {1} • 10'lu Paket: {2:N0}C", "• Growth: {0} Days • Level: {1} • 10-Pack: {2:N0}C");
                string statusDetails = string.Format(statusFmt, def.growthDays, def.requiredLevel, def.packPrice);
                Text descText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -6f), new Vector2(310f, 22f), statusDetails, 16, canBuy ? new Color(0.85f, 0.90f, 0.95f) : Color.gray);
                descText.alignment = TextAnchor.MiddleLeft;

                string seasonName = (TimeManager.Instance != null) ? TimeManager.Instance.GetLocalizedSeasonName(def.season) : def.season.ToString();
                string seasonInFmt = LocalizationManager.L("Seed_SeasonIn", "✅ Mevsim: {0}", "✅ Season: {0}");
                string seasonOutFmt = LocalizationManager.L("Seed_SeasonOut", "🔒 Mevsim Dışı ({0})", "🔒 Out of Season ({0})");
                string seasonBadgeStr = isMatchingSeason ? string.Format(seasonInFmt, seasonName) : string.Format(seasonOutFmt, seasonName);
                Color seasonBadgeCol = isMatchingSeason ? new Color(0.35f, 0.85f, 0.45f) : new Color(0.95f, 0.45f, 0.35f);
                Text badgeText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -24f), new Vector2(310f, 20f), seasonBadgeStr, 15, seasonBadgeCol);
                badgeText.alignment = TextAnchor.MiddleLeft;

                // 3. Sağ Kontrol Alanı (Sepete Ekle / - 1 + / Kilitli)
                GameObject ctrlPanel = new GameObject("CtrlPanel");
                ctrlPanel.transform.SetParent(cardObj.transform, false);
                RectTransform cpRect = ctrlPanel.AddComponent<RectTransform>();
                cpRect.anchoredPosition = new Vector2(190f, 0f);
                cpRect.sizeDelta = new Vector2(130f, 50f);

                UpdateSeedCardControls(ctrlPanel.transform, def, canBuy);
            }
            UpdateCartSummary();
        }

        private void UpdateSeedCardControls(Transform ctrlParent, GardenSeedDef def, bool canBuy)
        {
            if (ctrlParent == null || def == null) return;
            foreach (Transform child in ctrlParent) Destroy(child.gameObject);

            if (canBuy)
            {
                int inCartCount = seedCart.ContainsKey(def.id) ? seedCart[def.id] : 0;
                string targetSeedId = def.id;

                if (inCartCount > 0)
                {
                    // "-" Butonu (Büyütülmüş & Net Okunabilir)
                    GameObject minusBtn = CreateButtonInPanel(ctrlParent, new Vector2(-44f, 0f), new Vector2(40f, 40f), "-", new Color(0.85f, 0.25f, 0.25f), () => {
                        if (seedCart.ContainsKey(targetSeedId))
                        {
                            seedCart[targetSeedId]--;
                            if (seedCart[targetSeedId] <= 0) seedCart.Remove(targetSeedId);
                        }
                        UpdateSeedCardControls(ctrlParent, def, canBuy);
                        UpdateCartSummary();
                    }, 28);

                    // Adet Göstergesi (Büyütülmüş Sayı)
                    Text countTxt = CreateTextInPanel(ctrlParent, new Vector2(0f, 0f), new Vector2(36f, 40f), inCartCount.ToString(), 24, Color.white);
                    countTxt.fontStyle = FontStyle.Bold;
                    countTxt.alignment = TextAnchor.MiddleCenter;

                    // "+" Butonu (Büyütülmüş & Net Okunabilir)
                    GameObject plusBtn = CreateButtonInPanel(ctrlParent, new Vector2(44f, 0f), new Vector2(40f, 40f), "+", new Color(0.30f, 0.75f, 0.40f), () => {
                        if (!seedCart.ContainsKey(targetSeedId)) seedCart[targetSeedId] = 0;
                        seedCart[targetSeedId]++;
                        UpdateSeedCardControls(ctrlParent, def, canBuy);
                        UpdateCartSummary();
                    }, 28);
                }
                else
                {
                    // "+ Sepete Ekle" Butonu
                    string btnAddSeedLabel = LocalizationManager.L("Btn_AddToCart", "+ Sepete Ekle", "+ Add to Cart");
                    GameObject addBtn = CreateButtonInPanel(ctrlParent, new Vector2(0f, 0f), new Vector2(110f, 38f), btnAddSeedLabel, new Color(0.20f, 0.75f, 0.35f), () => {
                        seedCart[targetSeedId] = 1;
                        UpdateSeedCardControls(ctrlParent, def, canBuy);
                        UpdateCartSummary();
                    }, 17);
                }
            }
            else
            {
                TimeManager.Season currentSeason = (TimeManager.Instance != null) ? TimeManager.Instance.CurrentSeason : TimeManager.Season.İlkbahar;
                bool isMatchingSeason = (def.season == currentSeason);
                string outSeasonStr = LocalizationManager.L("Btn_OutOfSeason", "🔒 Mevsim Dışı", "🔒 Off-Season");
                string reqLvlFmt = LocalizationManager.L("Btn_ReqLevel", "🔒 Seviye {0}", "🔒 Level {0}");
                string lockTxt = !isMatchingSeason ? outSeasonStr : string.Format(reqLvlFmt, def.requiredLevel);
                GameObject lockBtn = CreateButtonInPanel(ctrlParent, new Vector2(0f, 0f), new Vector2(105f, 34f), lockTxt, new Color(0.35f, 0.35f, 0.40f), null, 15);
            }
        }

        private void RenderRenovationList()
        {
            if (furnitureListContent == null) return;
            foreach (Transform child in furnitureListContent) Destroy(child.gameObject);

            int currentLevel = (Farm2Shelf.Environment.EnvironmentBuilder.Instance != null)
                ? Farm2Shelf.Environment.EnvironmentBuilder.Instance.CurrentUpgradeLevel
                : 1;

            // 1. ZEMİN VE DUVARLAR SUB-TAB BAR (SOL ÜST KISIMDA KUTULAR)
            GameObject subTabBarObj = new GameObject("RenovationSubTabBar");
            subTabBarObj.transform.SetParent(furnitureListContent, false);

            LayoutElement subTabLe = subTabBarObj.AddComponent<LayoutElement>();
            subTabLe.minHeight = 48f;
            subTabLe.preferredHeight = 48f;

            HorizontalLayoutGroup subTabHlg = subTabBarObj.AddComponent<HorizontalLayoutGroup>();
            subTabHlg.spacing = 14;
            subTabHlg.childAlignment = TextAnchor.MiddleLeft;
            subTabHlg.childControlWidth = false;
            subTabHlg.childControlHeight = false;

            // DUVARLAR SEKMESİ (Sub-Tab 0)
            GameObject wallTabBtn = new GameObject("SubTab_Walls");
            wallTabBtn.transform.SetParent(subTabBarObj.transform, false);
            RectTransform wallTabRt = wallTabBtn.AddComponent<RectTransform>();
            wallTabRt.sizeDelta = new Vector2(160f, 40f);

            Image wallTabBg = wallTabBtn.AddComponent<Image>();
            bool isWallActive = (activeRenovationSubTab == 0);
            wallTabBg.sprite = isWallActive
                ? UIStyleUtility.CreateOutlinePillSprite(160, 40, 14, 2, new Color(0.95f, 0.75f, 0.20f), new Color(0.25f, 0.18f, 0.08f, 0.95f))
                : UIStyleUtility.CreateRoundedPillSprite(160, 40, 14, new Color(0.14f, 0.18f, 0.24f, 0.85f));

            Button wBtn = wallTabBtn.AddComponent<Button>();
            wBtn.targetGraphic = wallTabBg;
            wBtn.onClick.AddListener(() => {
                activeRenovationSubTab = 0;
                RenderShoppingCategoryContent();
            });

            Text wTxt = CreateTextInPanel(wallTabBtn.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Sub_Walls", "🎨 Duvarlar", "🎨 Walls"), 18, isWallActive ? Color.white : new Color(0.75f, 0.80f, 0.85f));
            wTxt.alignment = TextAnchor.MiddleCenter;

            // ZEMİN SEKMESİ (Sub-Tab 1)
            GameObject floorTabBtn = new GameObject("SubTab_Floors");
            floorTabBtn.transform.SetParent(subTabBarObj.transform, false);
            RectTransform floorTabRt = floorTabBtn.AddComponent<RectTransform>();
            floorTabRt.sizeDelta = new Vector2(160f, 40f);

            Image floorTabBg = floorTabBtn.AddComponent<Image>();
            bool isFloorActive = (activeRenovationSubTab == 1);
            floorTabBg.sprite = isFloorActive
                ? UIStyleUtility.CreateOutlinePillSprite(160, 40, 14, 2, new Color(0.95f, 0.75f, 0.20f), new Color(0.25f, 0.18f, 0.08f, 0.95f))
                : UIStyleUtility.CreateRoundedPillSprite(160, 40, 14, new Color(0.14f, 0.18f, 0.24f, 0.85f));

            Button fBtn = floorTabBtn.AddComponent<Button>();
            fBtn.targetGraphic = floorTabBg;
            fBtn.onClick.AddListener(() => {
                activeRenovationSubTab = 1;
                RenderShoppingCategoryContent();
            });

            Text fTxt = CreateTextInPanel(floorTabBtn.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Sub_Floors", "🧱 Zemin", "🧱 Floors"), 18, isFloorActive ? Color.white : new Color(0.75f, 0.80f, 0.85f));
            fTxt.alignment = TextAnchor.MiddleCenter;

            // 2. ÜRÜN LİSTESİ HESAPLAMA
            List<RenovationItemDef> items = (activeRenovationSubTab == 0)
                ? RenovationDatabase.GetWallPaints()
                : RenovationDatabase.GetFloorStyles();

            string query = string.IsNullOrEmpty(currentShoppingSearchQuery) ? "" : currentShoppingSearchQuery.Trim().ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
            if (!string.IsNullOrEmpty(query))
            {
                items = items.FindAll(i => i.Name.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(query));
            }

            foreach (var item in items)
            {
                RenovationItemDef def = item;
                bool isUnlocked = (currentLevel >= def.requiredLevel);

                GameObject cardObj = new GameObject("RenovationCard_" + def.id);
                cardObj.transform.SetParent(furnitureListContent, false);

                LayoutElement cardLe = cardObj.AddComponent<LayoutElement>();
                cardLe.minHeight = 85f;
                cardLe.preferredHeight = 85f;

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateRoundedPillSprite(720, 85, 16, new Color(0.12f, 0.15f, 0.20f, 0.90f));

                HorizontalLayoutGroup cardHlg = cardObj.AddComponent<HorizontalLayoutGroup>();
                cardHlg.padding = new RectOffset(10, 10, 8, 8);
                cardHlg.spacing = 10;
                cardHlg.childAlignment = TextAnchor.MiddleLeft;
                cardHlg.childControlWidth = false;
                cardHlg.childControlHeight = false;

                // RENK KUTUSU (50x50)
                GameObject previewObj = new GameObject("PreviewBox");
                previewObj.transform.SetParent(cardObj.transform, false);
                RectTransform prevRt = previewObj.AddComponent<RectTransform>();
                prevRt.sizeDelta = new Vector2(50f, 50f);

                Image prevBg = previewObj.AddComponent<Image>();
                prevBg.color = def.itemColor;
                prevBg.sprite = UIStyleUtility.CreateOutlinePillSprite(50, 50, 10, 2, Color.white, def.itemColor);

                Text iconEmojiTxt = CreateTextInPanel(previewObj.transform, Vector2.zero, Vector2.one, def.iconEmoji, 20, Color.white);
                iconEmojiTxt.alignment = TextAnchor.MiddleCenter;

                // İSİM VE SEVİYE BİLGİSİ (170x50)
                GameObject infoObj = new GameObject("InfoPanel");
                infoObj.transform.SetParent(cardObj.transform, false);
                RectTransform infoRt = infoObj.AddComponent<RectTransform>();
                infoRt.sizeDelta = new Vector2(170f, 50f);

                VerticalLayoutGroup infoVlg = infoObj.AddComponent<VerticalLayoutGroup>();
                infoVlg.spacing = 2;
                infoVlg.childAlignment = TextAnchor.MiddleLeft;
                infoVlg.childControlWidth = true;

                Text nameText = CreateTextInPanel(infoObj.transform, Vector2.zero, Vector2.one, def.Name, 19, Color.white);
                nameText.fontStyle = FontStyle.Bold;

                string lvlFmt = LocalizationManager.L("Renov_ReqLvl", "Seviye {0} Gerektirir", "Requires Level {0}");
                Text lvlText = CreateTextInPanel(infoObj.transform, Vector2.zero, Vector2.one, string.Format(lvlFmt, def.requiredLevel), 15, isUnlocked ? new Color(0.40f, 0.90f, 0.50f) : new Color(0.95f, 0.40f, 0.40f));

                // SAĞ KISIM: FİYAT VE "KULLAN" BUTONU (180x50)
                GameObject ctrlObj = new GameObject("ControlPanel");
                ctrlObj.transform.SetParent(cardObj.transform, false);
                RectTransform ctrlRt = ctrlObj.AddComponent<RectTransform>();
                ctrlRt.sizeDelta = new Vector2(180f, 50f);

                HorizontalLayoutGroup ctrlHlg = ctrlObj.AddComponent<HorizontalLayoutGroup>();
                ctrlHlg.spacing = 8;
                ctrlHlg.childAlignment = TextAnchor.MiddleRight;
                ctrlHlg.childControlWidth = false;
                ctrlHlg.childControlHeight = false;

                Text priceText = CreateTextInPanel(ctrlObj.transform, Vector2.zero, Vector2.one, $"<b>{def.price:N0}C</b>", 18, new Color(0.30f, 0.90f, 1.0f));
                priceText.alignment = TextAnchor.MiddleRight;
                RectTransform prRt = priceText.GetComponent<RectTransform>();
                prRt.sizeDelta = new Vector2(55f, 35f);

                if (isUnlocked)
                {
                    string useTxt = LocalizationManager.L("Btn_ApplyRenovation", "KULLAN", "APPLY");
                    GameObject applyBtnObj = CreateButtonInPanel(ctrlObj.transform, Vector2.zero, new Vector2(110f, 38f), useTxt, new Color(0.18f, 0.75f, 0.35f), () => {
                        ApplyRenovationItem(def);
                    }, 16);
                }
                else
                {
                    string lockTxt = string.Format(LocalizationManager.L("Btn_LockedFmt", "🔒 Lv.{0}", "🔒 Lv.{0}"), def.requiredLevel);
                    GameObject lockBtnObj = CreateButtonInPanel(ctrlObj.transform, Vector2.zero, new Vector2(110f, 38f), lockTxt, new Color(0.35f, 0.35f, 0.40f), null, 15);
                }
            }
        }

        private void ApplyRenovationItem(RenovationItemDef item)
        {
            if (item == null) return;

            int playerMoney = (EconomyManager.Instance != null) ? EconomyManager.Instance.Credits : 0;
            if (playerMoney < item.price)
            {
                ModalManager.ShowModal(
                    LocalizationManager.L("Renov_NoMoney_Title", "⚠️ Yetersiz Bakiye", "⚠️ Insufficient Balance"),
                    LocalizationManager.L("Renov_NoMoney_Body", $"Bu tadilat için {item.price:N0}C gereklidir. Mevcut paranız yetersiz.", $"You need {item.price:N0}C for this renovation. Your balance is insufficient."),
                    LocalizationManager.L("Btn_OK", "Tamam", "OK")
                );
                return;
            }

            // 1. KASADAN PARAYI ANINDA DÜŞ!
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SpendCredits(item.price);
            }
            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordExpense("Tadilat", $"{item.Name} Uygulaması", item.price);
            }

            // 2. DÜKKAN DUVAR/ZEMİNİNE ANINDA UYGULA!
            if (Farm2Shelf.Environment.EnvironmentBuilder.Instance != null)
            {
                if (item.type == RenovationType.WallPaint)
                {
                    Farm2Shelf.Environment.EnvironmentBuilder.Instance.ApplyWallColor(item.itemColor);
                }
                else if (item.type == RenovationType.FloorStyle)
                {
                    Farm2Shelf.Environment.EnvironmentBuilder.Instance.ApplyFloorStyle(item.itemColor);
                }
            }

            // 3. ALIŞVERİŞ ARAYÜZÜNÜ KESİN VE ANINDA KAPAT!
            ClosePhoneTabletInstant();

            // 4. BİLDİRİM GÖSTER!
            string successTitle = (item.type == RenovationType.WallPaint)
                ? LocalizationManager.L("Renov_Wall_Success_Title", "🎨 Duvarlar Boyandı!", "🎨 Walls Painted!")
                : LocalizationManager.L("Renov_Floor_Success_Title", "🧱 Zemin Yenilendi!", "🧱 Floor Renovated!");

            string successBody = (item.type == RenovationType.WallPaint)
                ? LocalizationManager.L("Renov_Wall_Success_Body", $"Mağaza duvarları '{item.Name}' ile başarıyla boyandı. Kasadan {item.price:N0}C düştü.", $"Store walls successfully painted with '{item.Name}'. {item.price:N0}C deducted.")
                : LocalizationManager.L("Renov_Floor_Success_Body", $"Mağaza zemini '{item.Name}' ile başarıyla yenilendi. Kasadan {item.price:N0}C düştü.", $"Store floor successfully renovated with '{item.Name}'. {item.price:N0}C deducted.");

            ModalManager.ShowModal(successTitle, successBody, LocalizationManager.L("Btn_OK", "Tamam", "OK"));
        }

        private void RenderFurnitureList()
        {
            FurnitureCategory targetCat = (activeShoppingCategory == 0) ? FurnitureCategory.Furniture : FurnitureCategory.Decoration;
            RenderFurnitureList(targetCat);
        }

        private void RenderFurnitureList(FurnitureCategory cat)
        {
            if (furnitureListContent == null) return;
            foreach (Transform child in furnitureListContent) Destroy(child.gameObject);

            int currentLevel = (Farm2Shelf.Environment.EnvironmentBuilder.Instance != null)
                ? Farm2Shelf.Environment.EnvironmentBuilder.Instance.CurrentUpgradeLevel
                : 1;

            List<FurnitureItemDef> items = FurnitureDatabase.GetDefsByCategory(cat);

            // Aktif Sekmedeki Ürünleri Arama Filtresine Göre Süzme (Türkçe Karakter Uyumlu)
            string query = string.IsNullOrEmpty(currentShoppingSearchQuery) ? "" : currentShoppingSearchQuery.Trim().ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
            if (!string.IsNullOrEmpty(query))
            {
                items = items.FindAll(i => 
                    i.name.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(query) || 
                    i.description.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(query)
                );
            }

            if (items.Count == 0)
            {
                string catTitle = (cat == FurnitureCategory.Furniture) ? "Mobilyalar" : (cat == FurnitureCategory.Workshop ? "Atölye Makineleri" : "Dekorasyonlar");
                GameObject emptyMsgObj = new GameObject("SearchEmptyMsg");
                emptyMsgObj.transform.SetParent(furnitureListContent, false);
                LayoutElement el = emptyMsgObj.AddComponent<LayoutElement>();
                el.minHeight = 120f;
                el.preferredHeight = 120f;

                Text emptyTxt = CreateTextInPanel(emptyMsgObj.transform, Vector2.zero, Vector2.one, $"🔍 '{currentShoppingSearchQuery}' araması için {catTitle} sekmesinde ürün bulunamadı.", 17, Color.gray);
                emptyTxt.alignment = TextAnchor.MiddleCenter;
                UpdateCartSummary();
                return;
            }

            foreach (var item in items)
            {
                FurnitureItemDef def = item;
                bool isUnlocked = (currentLevel >= def.requiredLevel);
                int inCartCount = shoppingCart.ContainsKey(def.type) ? shoppingCart[def.type] : 0;

                GameObject cardObj = new GameObject("FurnitureCard_" + def.type);
                cardObj.transform.SetParent(furnitureListContent, false);

                LayoutElement lElem = cardObj.AddComponent<LayoutElement>();
                lElem.minHeight = 72f;
                lElem.preferredHeight = 72f;

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(520, 72, 12, 1, new Color(0.95f, 0.40f, 0.55f, 0.6f), new Color(0.14f, 0.16f, 0.22f, 0.90f));

                // Sol Emoji İkonu Kutusu
                GameObject iconBox = new GameObject("IconBox");
                iconBox.transform.SetParent(cardObj.transform, false);
                RectTransform ibRect = iconBox.AddComponent<RectTransform>();
                ibRect.anchoredPosition = new Vector2(-225f, 0f);
                ibRect.sizeDelta = new Vector2(50f, 50f);

                Image ibBg = iconBox.AddComponent<Image>();
                ibBg.sprite = UIStyleUtility.CreateFurnitureIconSprite(def.type);

                // Orta Bilgi Alanı (Başlık, Açıklama, Seviye Kilit Rozeti)
                GameObject infoPanel = new GameObject("InfoPanel");
                infoPanel.transform.SetParent(cardObj.transform, false);
                RectTransform ipRect = infoPanel.AddComponent<RectTransform>();
                ipRect.anchoredPosition = new Vector2(-30f, 0f);
                ipRect.sizeDelta = new Vector2(300f, 60f);

                Text titleText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 15f), new Vector2(300f, 24f), $"{def.iconEmoji} {def.LocalizedName} ({def.price:N0} Cr)", 19, Color.white);
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleLeft;

                string badgeUnlockedFmt = LocalizationManager.L("Furn_UnlockedBadge", "✅ Seviye {0} | {1}", "✅ Level {0} | {1}");
                string badgeLockedFmt = LocalizationManager.L("Furn_LockedBadge", "🔒 Seviye {0} Gereklidir | {1}", "🔒 Requires Level {0} | {1}");
                string badgeText = isUnlocked ? string.Format(badgeUnlockedFmt, def.requiredLevel, def.GetZoneText()) : string.Format(badgeLockedFmt, def.requiredLevel, def.GetZoneText());
                Color badgeColor = isUnlocked ? new Color(0.30f, 0.85f, 0.45f) : new Color(0.95f, 0.45f, 0.35f);

                Text subText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -12f), new Vector2(300f, 22f), badgeText, 16, badgeColor);
                subText.alignment = TextAnchor.MiddleLeft;

                // Sağ Kontrol Alanı (Sepete Ekle / Adet / Kilitli)
                GameObject ctrlPanel = new GameObject("CtrlPanel");
                ctrlPanel.transform.SetParent(cardObj.transform, false);
                RectTransform cpRect = ctrlPanel.AddComponent<RectTransform>();
                cpRect.anchoredPosition = new Vector2(190f, 0f);
                cpRect.sizeDelta = new Vector2(130f, 50f);

                UpdateFurnitureCardControls(ctrlPanel.transform, def, isUnlocked);
            }

            UpdateCartSummary();
        }

        private void UpdateFurnitureCardControls(Transform ctrlParent, FurnitureItemDef def, bool isUnlocked)
        {
            if (ctrlParent == null || def == null) return;
            foreach (Transform child in ctrlParent) Destroy(child.gameObject);

            int inCartCount = shoppingCart.ContainsKey(def.type) ? shoppingCart[def.type] : 0;

            if (isUnlocked)
            {
                if (inCartCount > 0)
                {
                    // "-" Butonu (Büyütülmüş & Net Okunabilir)
                    GameObject minusBtn = CreateButtonInPanel(ctrlParent, new Vector2(-44f, 0f), new Vector2(40f, 40f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                        if (shoppingCart.ContainsKey(def.type))
                        {
                            shoppingCart[def.type]--;
                            if (shoppingCart[def.type] <= 0) shoppingCart.Remove(def.type);
                        }
                        UpdateFurnitureCardControls(ctrlParent, def, isUnlocked);
                        UpdateCartSummary();
                    }, 28);

                    // Adet Göstergesi (Büyütülmüş Sayı)
                    Text countTxt = CreateTextInPanel(ctrlParent, new Vector2(0f, 0f), new Vector2(36f, 40f), inCartCount.ToString(), 24, Color.white);
                    countTxt.fontStyle = FontStyle.Bold;
                    countTxt.alignment = TextAnchor.MiddleCenter;

                    // "+" Butonu (Büyütülmüş & Net Okunabilir)
                    GameObject plusBtn = CreateButtonInPanel(ctrlParent, new Vector2(44f, 0f), new Vector2(40f, 40f), "+", new Color(0.30f, 0.75f, 0.40f), () => {
                        shoppingCart[def.type]++;
                        UpdateFurnitureCardControls(ctrlParent, def, isUnlocked);
                        UpdateCartSummary();
                    }, 28);
                }
                else
                {
                    // "Sepete Ekle" Butonu
                    string btnAddToCartLabel = LocalizationManager.L("Btn_AddToCart", "+ Sepete Ekle", "+ Add to Cart");
                    GameObject addBtn = CreateButtonInPanel(ctrlParent, new Vector2(0f, 0f), new Vector2(110f, 38f), btnAddToCartLabel, new Color(0.95f, 0.40f, 0.55f), () => {
                        shoppingCart[def.type] = 1;
                        UpdateFurnitureCardControls(ctrlParent, def, isUnlocked);
                        UpdateCartSummary();
                    }, 17);
                }
            }
            else
            {
                // Kilitli Buton
                string lockItemStr = LocalizationManager.L("Btn_LockedItem", "🔒 Kilitli", "🔒 Locked");
                GameObject lockBtn = CreateButtonInPanel(ctrlParent, new Vector2(0f, 0f), new Vector2(100f, 34f), lockItemStr, new Color(0.35f, 0.35f, 0.40f), null, 15);
            }
        }

        private void UpdateCartSummary()
        {
            int totalItems = 0;
            int totalCost = 0;

            foreach (var kvp in shoppingCart)
            {
                FurnitureItemDef def = FurnitureDatabase.GetDef(kvp.Key);
                if (def != null)
                {
                    totalItems += kvp.Value;
                    totalCost += def.price * kvp.Value;
                }
            }

            foreach (var kvp in wholesaleCart)
            {
                WholesaleProductDef def = WholesaleDatabase.GetProductById(kvp.Key);
                if (def != null)
                {
                    totalItems += kvp.Value;
                    totalCost += def.TotalPackCost * kvp.Value;
                }
            }

            foreach (var kvp in seedCart)
            {
                GardenSeedDef def = GardenSeedDatabase.GetSeedById(kvp.Key);
                if (def != null)
                {
                    totalItems += kvp.Value;
                    totalCost += def.packPrice * kvp.Value;
                }
            }

            if (shoppingCartSummaryText != null)
            {
                string cartSummaryFmt = LocalizationManager.L("Cart_SummaryFmt", "🛒 Sepet: {0} Kalem ({1:N0}C)", "🛒 Cart: {0} Items ({1:N0}C)");
                shoppingCartSummaryText.text = string.Format(cartSummaryFmt, totalItems, totalCost);
            }

            if (headerCartButtonText != null)
            {
                string headerCartBtnFmt = LocalizationManager.L("Cart_HeaderBtnFmt", "🛒 SEPET ({0})", "🛒 CART ({0})");
                headerCartButtonText.text = string.Format(headerCartBtnFmt, totalItems);
            }

            if (checkoutCartButton != null)
            {
                checkoutCartButton.interactable = (totalItems > 0);
            }
        }

        private void CheckoutShoppingCart()
        {
            bool isAnyTruckActive = (WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckOnTheWay) ||
                                    (GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckOnTheWay);

            if (wholesaleCart.Count > 0 && isAnyTruckActive)
            {
                ModalManager.ShowModal("Teslimat Noktası Dolu! ⚠️", "Şu anda yolda veya teslimat noktasında aktif bir kamyon (Toptancı veya Çiftlik Kamyonu) bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni toptan sipariş verilemez.", "Tamam");
                return;
            }

            int totalItems = 0;
            int totalCost = 0;
            List<FurnitureType> orderFurniture = new List<FurnitureType>();
            List<WholesaleProductDef> orderWholesale = new List<WholesaleProductDef>();

            foreach (var kvp in shoppingCart)
            {
                FurnitureItemDef def = FurnitureDatabase.GetDef(kvp.Key);
                if (def != null)
                {
                    totalItems += kvp.Value;
                    totalCost += def.price * kvp.Value;
                    for (int i = 0; i < kvp.Value; i++)
                    {
                        orderFurniture.Add(kvp.Key);
                    }
                }
            }

            foreach (var kvp in wholesaleCart)
            {
                if (!WholesaleDatabase.IsProductWholesaleOrderable(kvp.Key)) continue;
                WholesaleProductDef def = WholesaleDatabase.GetProductById(kvp.Key);
                if (def != null && def.isOrderable)
                {
                    totalItems += kvp.Value;
                    totalCost += def.TotalPackCost * kvp.Value;
                    for (int i = 0; i < kvp.Value; i++)
                    {
                        orderWholesale.Add(def);
                    }
                }
            }

            foreach (var kvp in seedCart)
            {
                GardenSeedDef def = GardenSeedDatabase.GetSeedById(kvp.Key);
                if (def != null)
                {
                    totalItems += kvp.Value;
                    totalCost += def.packPrice * kvp.Value;
                }
            }

            if (totalItems == 0) return;

            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
            string btnGreat = LocalizationManager.L("Btn_Great", "Harika!", "Great!");

            // Kamyon Yolda Kontrolü (Toptancı ürünleri için)
            if (orderWholesale.Count > 0 && isAnyTruckActive)
            {
                string busyTitle = LocalizationManager.L("Modal_DockBusy_Title", "Teslimat Noktası Dolu! ⚠️", "Delivery Dock Occupied! ⚠️");
                string busyBody = LocalizationManager.L("Modal_DockBusy_Body", "Şu anda yolda veya teslimat noktasında aktif bir kamyon bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni toptan sipariş verilemez.", "There is currently an active truck on the way or at the dock!\n\nPlease wait until it leaves.");
                ModalManager.ShowModal(busyTitle, busyBody, btnOk);
                return;
            }

            // Bakiye Kontrolü
            int currentBalance = (EconomyManager.Instance != null) 
                ? EconomyManager.Instance.Credits 
                : ((FinanceManager.Instance != null) ? FinanceManager.Instance.CurrentBalance : 10000);

            if (currentBalance < totalCost)
            {
                string noBalTitle = LocalizationManager.L("Modal_NoBalance_Title", "Yetersiz Bakiye ⚠️", "Insufficient Balance ⚠️");
                string noBalBodyFmt = LocalizationManager.L("Modal_NoBalance_Body", "Siparişi tamamlamak için {0:N0}C gereklidir!\nMevcut Bakiyeniz: {1:N0}C.", "You need {0:N0}C to complete this order!\nCurrent Balance: {1:N0}C.");
                ModalManager.ShowModal(noBalTitle, string.Format(noBalBodyFmt, totalCost, currentBalance), btnOk);
                return;
            }

            // Paradan Düş (EconomyManager)
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SpendCredits(totalCost);
            }

            // Harcama Kaydı (FinanceManager)
            if (FinanceManager.Instance != null)
            {
                string catName = LocalizationManager.L("TrxCat_Wholesale", "Toptan/Alışveriş", "Wholesale/Shopping");
                string descFmt = LocalizationManager.L("TrxDesc_OrderFmt", "Toptancı & Mobilya & Tohum Siparişi ({0} Kalem)", "Wholesale & Furniture & Seed Order ({0} Items)");
                FinanceManager.Instance.RecordExpense(catName, string.Format(descFmt, totalItems), totalCost);
            }

            // Mobilya ve Atölye Makinelerini Ayrıştır
            List<FurnitureType> storeFurnitureOrders = new List<FurnitureType>();
            List<FurnitureType> workshopMachineOrders = new List<FurnitureType>();

            foreach (var fType in orderFurniture)
            {
                if (FurniturePlacementManager.IsWorkshopMachine(fType, out _))
                {
                    workshopMachineOrders.Add(fType);
                }
                else
                {
                    storeFurnitureOrders.Add(fType);
                }
            }

            // Mağaza Mobilyalarını Mal Kabul Paletine Gönder
            if (storeFurnitureOrders.Count > 0 && FurnitureDeliveryManager.Instance != null)
            {
                FurnitureDeliveryManager.Instance.AddOrdersToPallet(storeFurnitureOrders);
                if (TutorialManager.Instance != null)
                {
                    foreach (var kvp in shoppingCart)
                    {
                        if (!FurniturePlacementManager.IsWorkshopMachine(kvp.Key, out _))
                        {
                            TutorialManager.Instance.NotifyFurnitureItemPurchased(kvp.Key, kvp.Value);
                        }
                    }
                }
            }

            // Atölye Makinelerini Doğrudan Atölye Paletine Gönder!
            if (workshopMachineOrders.Count > 0 && WorkshopPalletManager.Instance != null)
            {
                WorkshopPalletManager.Instance.AddMachineOrders(workshopMachineOrders);
            }

            // Toptancı Siparişlerini Özel Kapalı Kasa Kamyon Kuryesine Gönder
            if (orderWholesale.Count > 0 && WholesaleTruckManager.Instance != null)
            {
                WholesaleTruckManager.Instance.DispatchWholesaleDelivery(orderWholesale);
            }

            // Satın Alınan Tohumları Doğrudan Ahır Tohum Envanterine Ekle!
            foreach (var kvp in seedCart)
            {
                if (kvp.Value > 0)
                {
                    GardenSeedInventoryManager.Instance.AddSeeds(kvp.Key, kvp.Value * 10);
                    if (TutorialManager.Instance != null)
                    {
                        TutorialManager.Instance.NotifySeedPurchased(kvp.Key, kvp.Value);
                    }
                }
            }

            shoppingCart.Clear();
            wholesaleCart.Clear();
            seedCart.Clear();
            RenderShoppingCategoryContent();

            if (orderWholesale.Count > 0)
            {
                string title = LocalizationManager.L("Modal_TruckDispatched_Title", "Toptancı Kamyonu Yola Çıktı! 🚛", "Wholesaler Truck Dispatched! 🚛");
                string bodyFmt = LocalizationManager.L("Modal_TruckDispatched_Body", "Toplam {0} kalem siparişiniz alındı!\n\nÖzel kapalı kasa kamyon teslimatı kapıya ulaştırıyor.", "Your order of {0} items has been received!\n\nA dedicated box truck is delivering your goods to the loading dock.");
                ModalManager.ShowModal(title, string.Format(bodyFmt, totalItems), btnGreat);
            }
            else if (workshopMachineOrders.Count > 0 && storeFurnitureOrders.Count == 0 && seedCart.Count == 0)
            {
                string wsTitle = LocalizationManager.L("Modal_WorkshopDelivered_Title", "Atölye Makineleri Teslim Edildi! 🏭", "Workshop Machines Delivered! 🏭");
                string wsBody = LocalizationManager.L("Modal_WorkshopDelivered_Body", "Satın aldığınız endüstriyel makineler doğrudan <b>Atölye Paletine</b> teslim edildi.\n\nAtölye binasına gidip palete dokunarak makinelerinizi hemen kurabilirsiniz!", "Purchased industrial machines have been delivered directly to the <b>Workshop Pallet</b>.\n\nVisit the workshop and click the pallet to assemble them!");
                ModalManager.ShowModal(wsTitle, wsBody, btnGreat);
            }
            else if (workshopMachineOrders.Count > 0)
            {
                string mixedTitle = LocalizationManager.L("Modal_MixedDelivered_Title", "Siparişiniz Teslim Edildi! 📦", "Order Delivered! 📦");
                string mixedBody = LocalizationManager.L("Modal_MixedDelivered_Body", "Siparişiniz başarıyla tamamlandı!\n\n• Mağaza Mobilyaları: <b>Mal Kabul Paletinde</b>\n• Atölye Makineleri: <b>Atölye Paletinde</b>\n• Tohumlar: <b>Ahır Envanterinde</b>", "Your order has been completed!\n\n• Store Furniture: <b>Loading Dock Pallet</b>\n• Workshop Machines: <b>Workshop Pallet</b>\n• Seeds: <b>Barn Storage</b>");
                ModalManager.ShowModal(mixedTitle, mixedBody, btnGreat);
            }
            else
            {
                string title = LocalizationManager.L("Modal_OrderReceived_Title", "Sipariş Alındı! 📦", "Order Placed! 📦");
                string bodyFmt = LocalizationManager.L("Modal_OrderReceived_Body", "Toplam {0} kalem siparişiniz başarıyla alındı ve ödemesi yapıldı!", "Your order of {0} items was successfully placed and paid!");
                ModalManager.ShowModal(title, string.Format(bodyFmt, totalItems), btnGreat);
            }
        }

        /// <summary>
        /// Sağ üst Sepet butonuna veya Siparişi Tamamla butonuna tıklandığında açılan detaylı Sepetim & Ödeme Pop-up penceresi.
        /// </summary>
        private void OpenCartModal()
        {
            GameObject existing = GameObject.Find("TrendyShop_Cart_Modal");
            if (existing != null) DestroyImmediate(existing);

            GameObject canvasObj = new GameObject("TrendyShop_Cart_Modal");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950; // TABLET EKRANININ (900) ÖNÜNE ÇIKAR

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Karartma Arka Plan
            GameObject backdrop = new GameObject("Cart_Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.05f, 0.08f, 0.12f, 0.80f);
            bdImg.raycastTarget = true;

            // Modal Kutusu
            GameObject boxObj = new GameObject("Cart_Box");
            boxObj.transform.SetParent(backdrop.transform, false);

            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(650f, 480f);

            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.sprite = UIStyleUtility.CreateOutlinePillSprite(650, 480, 18, 2, new Color(0.95f, 0.40f, 0.55f), new Color(0.12f, 0.15f, 0.20f, 0.98f));

            // Başlık
            Text tText = CreateTextInPanel(boxObj.transform, new Vector2(0f, 205f), new Vector2(580f, 40f), LocalizationManager.L("Cart_ModalTitle", "🛒 EKT SHOPPING SEPETİM VE ÖDEME", "🛒 EKT SHOPPING MY CART & CHECKOUT"), 28, new Color(0.95f, 0.45f, 0.60f));
            tText.alignment = TextAnchor.MiddleCenter;

            // Kapat (X) Butonu
            GameObject closeBtn = CreateButtonInPanel(boxObj.transform, new Vector2(295f, 205f), new Vector2(40f, 40f), "✖", new Color(0.92f, 0.18f, 0.20f), () => {
                Destroy(canvasObj);
            }, 22);
            closeBtn.transform.SetAsLastSibling();

            // Bakiye ve Bilgi Paneli
            int currentBalance = (FinanceManager.Instance != null) ? FinanceManager.Instance.CurrentBalance : 50000;
            string balFmt = LocalizationManager.L("Cart_BalanceFmt", "💰 Mevcut Bakiyeniz: {0:N0}C", "💰 Current Balance: {0:N0}C");
            Text balText = CreateTextInPanel(boxObj.transform, new Vector2(0f, 165f), new Vector2(580f, 30f), string.Format(balFmt, currentBalance), 19, new Color(0.30f, 0.85f, 0.50f));
            balText.alignment = TextAnchor.MiddleCenter;

            // İçerik ScrollView
            Transform cartContent = CreateScrollableViewContainer(boxObj.transform, "CartItemList", new Vector2(0f, 0f), new Vector2(590f, 260f), out Transform viewportObj);
            VerticalLayoutGroup vLayout = cartContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 6f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;

            // Alt Ödeme Alanı (Footer)
            GameObject footerObj = new GameObject("Cart_Footer");
            footerObj.transform.SetParent(boxObj.transform, false);
            RectTransform ftRect = footerObj.AddComponent<RectTransform>();
            ftRect.anchoredPosition = new Vector2(0f, -195f);
            ftRect.sizeDelta = new Vector2(590f, 50f);

            Text totalTxt = CreateTextInPanel(footerObj.transform, new Vector2(-130f, 0f), new Vector2(300f, 40f), "", 22, new Color(0.95f, 0.85f, 0.30f));
            totalTxt.alignment = TextAnchor.MiddleLeft;

            string payBtnLabel = LocalizationManager.L("Btn_PlaceOrderPay", "💳 ÖDEME YAP VE SİPARİŞ VER", "💳 PLACE ORDER & PAY");
            GameObject payBtnObj = CreateButtonInPanel(footerObj.transform, new Vector2(165f, 0f), new Vector2(230f, 44f), payBtnLabel, new Color(0.20f, 0.75f, 0.35f), () => {
                Destroy(canvasObj);
                CheckoutShoppingCart();
            }, 17);
            Button payBtn = payBtnObj.GetComponent<Button>();

            RenderCartModalItems(cartContent, totalTxt, payBtn);
        }

        private void RenderCartModalItems(Transform cartContent, Text totalTxt, Button payBtn)
        {
            if (cartContent == null) return;
            foreach (Transform child in cartContent) Destroy(child.gameObject);

            int totalItems = 0;
            int totalCost = 0;

            if (shoppingCart.Count == 0 && wholesaleCart.Count == 0 && seedCart.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyMsg");
                emptyObj.transform.SetParent(cartContent, false);
                LayoutElement el = emptyObj.AddComponent<LayoutElement>();
                el.minHeight = 120f;
                el.preferredHeight = 120f;

                Text emptyTxt = CreateTextInPanel(emptyObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Cart_EmptyMsg", "🛒 Sepetiniz şu anda boş!\nKatalogdan ürün seçerek sepete ekleyebilirsiniz.", "🛒 Your cart is currently empty!\nYou can add items from the catalog."), 18, Color.gray);
                emptyTxt.alignment = TextAnchor.MiddleCenter;
            }
            else
            {
                // Mobilya Ürünleri
                foreach (var kvp in new Dictionary<FurnitureType, int>(shoppingCart))
                {
                    FurnitureType fType = kvp.Key;
                    int count = kvp.Value;
                    FurnitureItemDef def = FurnitureDatabase.GetDef(fType);
                    if (def == null) continue;

                    int itemTotalCost = def.price * count;
                    totalItems += count;
                    totalCost += itemTotalCost;

                    GameObject itemRow = new GameObject("CartRow_" + fType);
                    itemRow.transform.SetParent(cartContent, false);

                    LayoutElement rElem = itemRow.AddComponent<LayoutElement>();
                    rElem.minHeight = 54f;
                    rElem.preferredHeight = 54f;

                    Image rBg = itemRow.AddComponent<Image>();
                    rBg.sprite = UIStyleUtility.CreateRoundedPillSprite(580, 54, 10, new Color(0.18f, 0.22f, 0.30f));

                    Text nameTxt = CreateTextInPanel(itemRow.transform, new Vector2(-155f, 0f), new Vector2(230f, 40f), $"{def.iconEmoji} {def.LocalizedName}", 18, Color.white);
                    nameTxt.alignment = TextAnchor.MiddleLeft;

                    Text priceTxt = CreateTextInPanel(itemRow.transform, new Vector2(20f, 0f), new Vector2(150f, 40f), $"{count} x {def.price:N0} = {itemTotalCost:N0} Cr", 17, new Color(0.95f, 0.80f, 0.30f));
                    priceTxt.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(138f, 0f), new Vector2(36f, 36f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                        shoppingCart[fType]--;
                        if (shoppingCart[fType] <= 0) shoppingCart.Remove(fType);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        RenderCartModalItems(cartContent, totalTxt, payBtn);
                    }, 26);

                    Text countLabel = CreateTextInPanel(itemRow.transform, new Vector2(174f, 0f), new Vector2(30f, 36f), count.ToString(), 22, Color.white);
                    countLabel.fontStyle = FontStyle.Bold;
                    countLabel.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(210f, 0f), new Vector2(36f, 36f), "+", new Color(0.28f, 0.75f, 0.40f), () => {
                        shoppingCart[fType]++;
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        RenderCartModalItems(cartContent, totalTxt, payBtn);
                    }, 26);

                    // Ürünü Sepetten Tamamen Çıkarma Butonu (X)
                    CreateButtonInPanel(itemRow.transform, new Vector2(254f, 0f), new Vector2(36f, 36f), "✕", new Color(0.82f, 0.22f, 0.22f), () => {
                        shoppingCart.Remove(fType);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        RenderCartModalItems(cartContent, totalTxt, payBtn);
                    }, 19);
                }

                // Toptan Ürün Kolileri
                foreach (var kvp in new Dictionary<string, int>(wholesaleCart))
                {
                    string pId = kvp.Key;
                    int count = kvp.Value;
                    WholesaleProductDef def = WholesaleDatabase.GetProductById(pId);
                    if (def == null) continue;

                    int itemTotalCost = def.TotalPackCost * count;
                    totalItems += count;
                    totalCost += itemTotalCost;

                    GameObject itemRow = new GameObject("CartRow_WS_" + pId);
                    itemRow.transform.SetParent(cartContent, false);

                    LayoutElement rElem = itemRow.AddComponent<LayoutElement>();
                    rElem.minHeight = 54f;
                    rElem.preferredHeight = 54f;

                    Image rBg = itemRow.AddComponent<Image>();
                    rBg.sprite = UIStyleUtility.CreateRoundedPillSprite(580, 54, 10, new Color(0.25f, 0.18f, 0.15f));

                    string pack50Label = LocalizationManager.L("Cart_Pack50", "50'li Koli", "Pack of 50");
                    Text nameTxt = CreateTextInPanel(itemRow.transform, new Vector2(-155f, 0f), new Vector2(230f, 40f), $"{def.iconEmoji} {def.LocalizedName} ({pack50Label})", 18, Color.white);
                    nameTxt.alignment = TextAnchor.MiddleLeft;

                    Text priceTxt = CreateTextInPanel(itemRow.transform, new Vector2(20f, 0f), new Vector2(150f, 40f), $"{count} Koli = {itemTotalCost:N0} Cr", 17, new Color(0.95f, 0.75f, 0.30f));
                    priceTxt.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(138f, 0f), new Vector2(36f, 36f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                        wholesaleCart[pId]--;
                        if (wholesaleCart[pId] <= 0) wholesaleCart.Remove(pId);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        RenderCartModalItems(cartContent, totalTxt, payBtn);
                    }, 26);

                    Text countLabel = CreateTextInPanel(itemRow.transform, new Vector2(174f, 0f), new Vector2(30f, 36f), count.ToString(), 22, Color.white);
                    countLabel.fontStyle = FontStyle.Bold;
                    countLabel.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(210f, 0f), new Vector2(36f, 36f), "+", new Color(0.28f, 0.75f, 0.40f), () => {
                        wholesaleCart[pId]++;
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        RenderCartModalItems(cartContent, totalTxt, payBtn);
                    }, 26);

                    // Ürünü Sepetten Tamamen Çıkarma Butonu (X)
                    CreateButtonInPanel(itemRow.transform, new Vector2(254f, 0f), new Vector2(36f, 36f), "✕", new Color(0.82f, 0.22f, 0.22f), () => {
                        wholesaleCart.Remove(pId);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        RenderCartModalItems(cartContent, totalTxt, payBtn);
                    }, 19);
                }

                // Tohum Paketleri
                foreach (var kvp in new Dictionary<string, int>(seedCart))
                {
                    string sId = kvp.Key;
                    int count = kvp.Value;
                    GardenSeedDef def = GardenSeedDatabase.GetSeedById(sId);
                    if (def == null) continue;

                    int itemTotalCost = def.packPrice * count;
                    totalItems += count;
                    totalCost += itemTotalCost;

                    GameObject itemRow = new GameObject("CartRow_Seed_" + sId);
                    itemRow.transform.SetParent(cartContent, false);

                    LayoutElement rElem = itemRow.AddComponent<LayoutElement>();
                    rElem.minHeight = 54f;
                    rElem.preferredHeight = 54f;

                    Image rBg = itemRow.AddComponent<Image>();
                    rBg.sprite = UIStyleUtility.CreateRoundedPillSprite(580, 54, 10, new Color(0.15f, 0.25f, 0.18f));

                    string pack10Label = LocalizationManager.L("Cart_Pack10", "10'lu Paket", "Pack of 10");
                    Text nameTxt = CreateTextInPanel(itemRow.transform, new Vector2(-155f, 0f), new Vector2(230f, 40f), $"{def.iconEmoji} {def.LocalizedName} ({pack10Label})", 18, Color.white);
                    nameTxt.alignment = TextAnchor.MiddleLeft;

                    Text priceTxt = CreateTextInPanel(itemRow.transform, new Vector2(20f, 0f), new Vector2(150f, 40f), $"{count} Pk = {itemTotalCost:N0} Cr", 17, new Color(0.35f, 0.85f, 0.45f));
                    priceTxt.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(138f, 0f), new Vector2(36f, 36f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                        seedCart[sId]--;
                        if (seedCart[sId] <= 0) seedCart.Remove(sId);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        RenderCartModalItems(cartContent, totalTxt, payBtn);
                    }, 26);

                    Text countLabel = CreateTextInPanel(itemRow.transform, new Vector2(174f, 0f), new Vector2(30f, 36f), count.ToString(), 22, Color.white);
                    countLabel.fontStyle = FontStyle.Bold;
                    countLabel.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(210f, 0f), new Vector2(36f, 36f), "+", new Color(0.28f, 0.75f, 0.40f), () => {
                        seedCart[sId]++;
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        RenderCartModalItems(cartContent, totalTxt, payBtn);
                    }, 26);

                    // Ürünü Sepetten Tamamen Çıkarma Butonu (X)
                    CreateButtonInPanel(itemRow.transform, new Vector2(254f, 0f), new Vector2(36f, 36f), "✕", new Color(0.82f, 0.22f, 0.22f), () => {
                        seedCart.Remove(sId);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        RenderCartModalItems(cartContent, totalTxt, payBtn);
                    }, 19);
                }
            }

            string totalFmt = LocalizationManager.L("Cart_TotalCostFmt", "Toplam Tutar: {0:N0}C", "Total Cost: {0:N0}C");
            if (totalTxt != null) totalTxt.text = string.Format(totalFmt, totalCost);
            if (payBtn != null) payBtn.interactable = (totalItems > 0);
        }

        private void RefreshFarmViews()
        {
            UpdateFarmTabVisuals();

            if (farmOverviewViewportObj != null) farmOverviewViewportObj.gameObject.SetActive(activeFarmTab == 0);
            if (farmStaffViewportObj != null) farmStaffViewportObj.gameObject.SetActive(activeFarmTab == 1);
            if (farmCandidateViewportObj != null) farmCandidateViewportObj.gameObject.SetActive(activeFarmTab == 2);
            if (farmShiftViewportObj != null) farmShiftViewportObj.gameObject.SetActive(activeFarmTab == 3);

            if (activeFarmTab == 0) RenderFarmOverview();
            else if (activeFarmTab == 1) RenderFarmStaffList();
            else if (activeFarmTab == 2) RenderFarmCandidateList();
            else if (activeFarmTab == 3) RenderFarmShiftList();
        }

        private void RefreshStoreManagementViews()
        {
            if (StaffManager.Instance == null) return;

            UpdateStoreTabVisuals();

            if (upgradeViewportObj != null) upgradeViewportObj.gameObject.SetActive(activeTab == 0);
            if (staffViewportObj != null) staffViewportObj.gameObject.SetActive(activeTab == 1);
            if (candidateViewportObj != null) candidateViewportObj.gameObject.SetActive(activeTab == 2);
            if (shiftViewportObj != null) shiftViewportObj.gameObject.SetActive(activeTab == 3);

            if (activeTab == 0) RenderStoreUpgradeList();
            else if (activeTab == 1) RenderCategorizedStaffList();
            else if (activeTab == 2) RenderPermanentRoleRecruitmentList();
            else if (activeTab == 3) RenderCategorizedShiftManagementList();
        }

        private void RenderStoreUpgradeList()
        {
            if (upgradeListContent == null) return;

            foreach (Transform child in upgradeListContent)
            {
                Destroy(child.gameObject);
            }

            int currentLevel = EnvironmentBuilder.Instance != null ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;

            string[] stageNames = new string[] {
                LocalizationManager.L("Upgrade_Stage2_Name", "Seviye 2 Geliştirme (Geniş Süpermarket)", "Level 2 Expansion (Grand Supermarket)"),
                LocalizationManager.L("Upgrade_Stage3_Name", "Seviye 3 Geliştirme (Mega Hipermarket)", "Level 3 Expansion (Mega Hypermarket)")
            };

            int[] targetLevels = new int[] { 2, 3 };
            int[] upgradeCosts = new int[] { 5000, 15000 };

            string[] descriptions = new string[] {
                LocalizationManager.L("Upgrade_Stage2_Desc", "• Mağaza tabanı & arka duvarlar +5.0m yukarı doğru genişler.\n• Depo ve personel odası büyür.\n• Otopark park çizgileri ve kapasitesi 18 ARACA yükselir.", "• Store floor & rear walls expand +5.0m upwards.\n• Storage and staff room expand.\n• Parking lines & capacity expand to 18 VEHICLES."),
                LocalizationManager.L("Upgrade_Stage3_Desc", "• Mağaza tabanı & depo +10.0m devasa boyuta ulaşır.\n• Personel odası 2 katına çıkar.\n• Otopark park çizgileri ve kapasitesi 26 ARACA ulaşır.", "• Store floor & storage reach massive +10.0m expansion.\n• Staff room doubles in size.\n• Parking lines & capacity reach 26 VEHICLES.")
            };

            for (int i = 0; i < 2; i++)
            {
                int targetLvl = targetLevels[i];
                int cost = upgradeCosts[i];

                bool isUnlocked = (currentLevel >= targetLvl);
                bool canUpgradeNow = (currentLevel == targetLvl - 1);
                bool isLocked = (currentLevel < targetLvl - 1);

                GameObject cardObj = new GameObject("UpgradeCard_" + targetLvl);
                cardObj.transform.SetParent(upgradeListContent, false);

                RectTransform cRect = cardObj.AddComponent<RectTransform>();
                cRect.sizeDelta = new Vector2(820f, 110f);

                Color borderColor;
                if (isUnlocked) borderColor = new Color(0.20f, 0.85f, 0.40f);
                else if (canUpgradeNow) borderColor = new Color(1.0f, 0.75f, 0.20f);
                else borderColor = new Color(0.40f, 0.45f, 0.55f);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 110, 16, 2, borderColor, new Color(0.12f, 0.16f, 0.22f, 0.95f));
                cardBg.raycastTarget = false;

                GameObject infoObj = new GameObject("InfoText");
                infoObj.transform.SetParent(cardObj.transform, false);

                RectTransform iRect = infoObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(-110f, 0f);
                iRect.sizeDelta = new Vector2(550f, 100f);

                Text iText = infoObj.AddComponent<Text>();
                iText.font = globalFont;
                string costWord = LocalizationManager.L("Label_Cost", "Ücret", "Cost");
                iText.text = $"🏢 <b>{stageNames[i]}</b>   |   <b>{costWord}: {cost:N0}C</b>\n{descriptions[i]}";
                iText.fontSize = 17;
                iText.fontStyle = FontStyle.Normal;
                iText.alignment = TextAnchor.MiddleLeft;
                iText.color = isLocked ? new Color(0.60f, 0.65f, 0.72f) : new Color(0.92f, 0.94f, 0.98f);
                iText.raycastTarget = false;

                GameObject btnObj = new GameObject("UpgradeButton");
                btnObj.transform.SetParent(cardObj.transform, false);

                RectTransform bRect = btnObj.AddComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(270f, 0f);
                bRect.sizeDelta = new Vector2(210f, 44f);

                Image bBg = btnObj.AddComponent<Image>();
                Color btnClr;
                string btnLabelText;

                if (isUnlocked)
                {
                    btnClr = new Color(0.20f, 0.50f, 0.30f, 0.70f);
                    btnLabelText = LocalizationManager.L("Btn_CurrentLevel", "✔ MEVCUT SEVİYE", "✔ CURRENT LEVEL");
                }
                else if (canUpgradeNow)
                {
                    btnClr = new Color(1.0f, 0.65f, 0.10f, 0.95f);
                    btnLabelText = LocalizationManager.L("Btn_UpgradeNow", "🚀 GELİŞTİR (GÜNCELLE)", "🚀 UPGRADE NOW");
                }
                else
                {
                    btnClr = new Color(0.25f, 0.28f, 0.35f, 0.60f);
                    string lockFmt = LocalizationManager.L("Btn_LockedPrevLvl", "🔒 ÖNCE SEVİYE {0}", "🔒 REQUIRES LEVEL {0}");
                    btnLabelText = string.Format(lockFmt, targetLvl - 1);
                }

                bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(210, 44, 22, btnClr);
                bBg.raycastTarget = canUpgradeNow;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = bBg;
                btn.interactable = canUpgradeNow;

                int passLvl = targetLvl;
                int passCost = cost;
                btn.onClick.AddListener(() => {
                    if (EnvironmentBuilder.Instance != null)
                    {
                        bool success = EnvironmentBuilder.Instance.TryUpgradeStore(passLvl, passCost);
                        if (!success)
                        {
                            Debug.LogWarning("[Farm2Shelf] Mağaza geliştirme için yetersiz bakiye (Credit)!");
                        }
                    }
                });

                GameObject btObj = new GameObject("Text");
                btObj.transform.SetParent(btnObj.transform, false);
                RectTransform btRect = btObj.AddComponent<RectTransform>();
                btRect.anchorMin = Vector2.zero;
                btRect.anchorMax = Vector2.one;

                Text btText = btObj.AddComponent<Text>();
                btText.font = globalFont;
                btText.text = btnLabelText;
                btText.fontSize = 16;
                btText.fontStyle = FontStyle.Bold;
                btText.alignment = TextAnchor.MiddleCenter;
                btText.color = canUpgradeNow ? Color.white : new Color(0.70f, 0.75f, 0.80f);
                btText.raycastTarget = false;
            }
        }

        private void RefreshFinanceViews()
        {
            if (FinanceManager.Instance == null) return;

            if (financeProductsControlBar != null) financeProductsControlBar.gameObject.SetActive(activeFinanceTab == 0);
            if (financeProductsViewportObj != null) financeProductsViewportObj.gameObject.SetActive(activeFinanceTab == 0);
            if (financeSummaryViewportObj != null) financeSummaryViewportObj.gameObject.SetActive(activeFinanceTab == 1);
            if (financeHistoryViewportObj != null) financeHistoryViewportObj.gameObject.SetActive(activeFinanceTab == 2);
            if (financeLoansViewportObj != null) financeLoansViewportObj.gameObject.SetActive(activeFinanceTab == 3);
            if (financeStocksViewportObj != null) financeStocksViewportObj.gameObject.SetActive(activeFinanceTab == 4);

            if (activeFinanceTab == 0) RenderFinanceProductsList();
            else if (activeFinanceTab == 1) RenderFinanceSummaryDashboard();
            else if (activeFinanceTab == 2) RenderFinanceTransactionHistory();
            else if (activeFinanceTab == 3) RenderFinanceBankLoans();
            else if (activeFinanceTab == 4) RenderFinanceStockMarket();
        }

        private void RenderFinanceBankLoans()
        {
            if (financeLoansContent == null) return;
            foreach (Transform child in financeLoansContent) Destroy(child.gameObject);

            int storeLevel = EnvironmentBuilder.Instance != null ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            BankLoanManager loanMgr = BankLoanManager.Instance;

            // 1. ÖZET ROZET KARTI (Aktif Kredi Borçları)
            GameObject summaryCard = new GameObject("LoansSummaryCard");
            summaryCard.transform.SetParent(financeLoansContent, false);
            RectTransform sRect = summaryCard.AddComponent<RectTransform>();
            sRect.sizeDelta = new Vector2(820f, 70f);

            Image sBg = summaryCard.AddComponent<Image>();
            sBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 70, 16, 2, new Color(0.95f, 0.70f, 0.20f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

            int totalDebt = loanMgr != null ? loanMgr.TotalActiveLoanDebt : 0;
            int activeCount = loanMgr != null ? loanMgr.GetActiveLoans().Count : 0;

            string loanSummaryFmt = LocalizationManager.L(
                "Loans_SummaryHeaderFmt",
                "🏛️ <b>BANKA KREDİLERİ YÖNETİMİ</b>   |   Aktif Krediler: <b>{0} Adet</b>   |   Kalan Toplam Borç: <color=#FFD54F>{1:N0}C</color>\n<size=14><color=#B0BEC5>Gece yarısında (00:00) günlük taksitler otomatik tahsil edilir. İsterseniz kredilerinizi erken ödeyip kapatabilirsiniz.</color></size>",
                "🏛️ <b>BANK LOANS MANAGEMENT</b>   |   Active Loans: <b>{0} Active</b>   |   Remaining Total Debt: <color=#FFD54F>{1:N0}C</color>\n<size=14><color=#B0BEC5>Daily installments are automatically collected at midnight (00:00). You can payoff loans early if desired.</color></size>"
            );
            Text sText = CreateTextInPanel(summaryCard.transform, Vector2.zero, Vector2.one, string.Format(loanSummaryFmt, activeCount, totalDebt), 17, Color.white);
            sText.alignment = TextAnchor.MiddleCenter;

            // 2. AKTİF KREDİLER LİSTESİ (Varsa)
            if (loanMgr != null && activeCount > 0)
            {
                GameObject activeSectionHeader = new GameObject("ActiveLoansHeader");
                activeSectionHeader.transform.SetParent(financeLoansContent, false);
                RectTransform ahRect = activeSectionHeader.AddComponent<RectTransform>();
                ahRect.sizeDelta = new Vector2(820f, 25f);
                Text ahTxt = CreateTextInPanel(activeSectionHeader.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Header_ActiveLoans", "<b>🔴 AKTİF ÖDENMEKTE OLAN KREDİLERİNİZ:</b>", "<b>🔴 YOUR ACTIVE LOANS BEING REPAID:</b>"), 18, new Color(1.0f, 0.80f, 0.30f));
                ahTxt.alignment = TextAnchor.MiddleLeft;

                foreach (var activeLoan in loanMgr.GetActiveLoans())
                {
                    ActiveLoanData lData = activeLoan;
                    GameObject activeCard = new GameObject("ActiveCard_" + lData.loanId);
                    activeCard.transform.SetParent(financeLoansContent, false);
                    RectTransform acRect = activeCard.AddComponent<RectTransform>();
                    acRect.sizeDelta = new Vector2(820f, 85f);

                    Image acBg = activeCard.AddComponent<Image>();
                    acBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 85, 14, 2, new Color(0.20f, 0.70f, 0.95f), new Color(0.14f, 0.18f, 0.25f, 0.95f));

                    GameObject infoObj = new GameObject("Info");
                    infoObj.transform.SetParent(activeCard.transform, false);
                    RectTransform iRect = infoObj.AddComponent<RectTransform>();
                    iRect.anchoredPosition = new Vector2(-100f, 0f);
                    iRect.sizeDelta = new Vector2(580f, 75f);
                    Text iTxt = infoObj.AddComponent<Text>();
                    iTxt.font = globalFont;
                    int earlyCost = lData.GetEarlyPayoffAmount();
                    string sameDayBadge = LocalizationManager.L("Badge_SameDayNoInterest", "<color=#00E676><b>(⚡ Aynı Gün Faizsiz!)</b></color>", "<color=#00E676><b>(⚡ Same Day Interest-Free!)</b></color>");
                    string discountBadge = LocalizationManager.L("Badge_InterestSavings", "<color=#FFD54F><b>(🎉 Faiz Tasarruflu!)</b></color>", "<color=#FFD54F><b>(🎉 Interest Savings!)</b></color>");
                    string earlyBadge = (lData.remainingDays >= lData.initialTermDays) ? sameDayBadge : discountBadge;

                    string activeLoanRowFmt = LocalizationManager.L(
                        "ActiveLoan_RowFmt",
                        "💳 <b>{0}</b> ({1})\n  • Ana Para: <b>{2:N0}C</b>   |   Günlük Taksit: <b>{3:N0}C</b>\n  • Kalan Vade: <b>{4} Gün</b>   |   Erken Kapatma Tutarı: <color=#00E676><b>{5:N0}C</b></color> {6}",
                        "💳 <b>{0}</b> ({1})\n  • Principal: <b>{2:N0}C</b>   |   Daily Installment: <b>{3:N0}C</b>\n  • Remaining Term: <b>{4} Days</b>   |   Early Payoff Amount: <color=#00E676><b>{5:N0}C</b></color> {6}"
                    );
                    iTxt.text = string.Format(activeLoanRowFmt, lData.LocalizedTitle, lData.startDateFormatted, lData.principalAmount, lData.dailyInstallment, lData.remainingDays, earlyCost, earlyBadge);
                    iTxt.fontSize = 17;
                    iTxt.alignment = TextAnchor.MiddleLeft;
                    iTxt.color = Color.white;

                    // Erken Kapat Butonu (Sağ Taraf Sabit Konumlandırma)
                    GameObject payoffBtn = new GameObject("PayoffBtn");
                    payoffBtn.transform.SetParent(activeCard.transform, false);
                    RectTransform pRect = payoffBtn.AddComponent<RectTransform>();
                    pRect.anchoredPosition = new Vector2(280f, 0f);
                    pRect.sizeDelta = new Vector2(170f, 40f);

                    Image pBg = payoffBtn.AddComponent<Image>();
                    pBg.sprite = UIStyleUtility.CreateRoundedPillSprite(170, 40, 20, new Color(0.90f, 0.35f, 0.25f));
                    Button pBtn = payoffBtn.AddComponent<Button>();
                    pBtn.targetGraphic = pBg;
                    pBtn.onClick.AddListener(() => {
                        int costNeeded = lData.GetEarlyPayoffAmount();
                        bool ok = loanMgr.PayoffLoanEarly(lData);
                        if (!ok)
                        {
                            string errTitle = LocalizationManager.L("Modal_LowBalance_Title", "Yetersiz Bakiye! ⚠️", "Insufficient Balance! ⚠️");
                            string errBody = string.Format(LocalizationManager.L("Modal_LowBalance_PayoffBody", "Krediyi erken kapatmak için {0:N0}C bakiyeniz olmalıdır.", "You must have {0:N0}C balance to payoff the loan early."), costNeeded);
                            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
                            ModalManager.ShowModal(errTitle, errBody, btnOk);
                        }
                        else
                        {
                            string succTitle = LocalizationManager.L("Modal_LoanPayoff_Title", "Kredi Erken Kapatıldı! 🎉", "Loan Paid Off Early! 🎉");
                            string succBody = string.Format(LocalizationManager.L("Modal_LoanPayoff_Body", "<b>{0}</b> kredisinin erken ödemesi ({1:N0}C) yapıldı ve borç tamamen kapatıldı!", "Early payoff for <b>{0}</b> ({1:N0}C) completed and debt fully settled!"), lData.title, costNeeded);
                            string btnGreat = LocalizationManager.L("Btn_Great", "Harika!", "Great!");
                            ModalManager.ShowModal(succTitle, succBody, btnGreat);
                        }
                    });

                    Text pTxt = CreateTextInPanel(payoffBtn.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_PayoffEarly", "⚡ ERKEN KAPAT", "⚡ PAYOFF EARLY"), 16, Color.white);
                    pTxt.alignment = TextAnchor.MiddleCenter;
                    pTxt.fontStyle = FontStyle.Bold;
                }
            }

            // 3. MEVCUT SEVİYE KREDİ TEKLİFLERİ
            GameObject offersHeader = new GameObject("OffersHeader");
            offersHeader.transform.SetParent(financeLoansContent, false);
            RectTransform ohRect = offersHeader.AddComponent<RectTransform>();
            ohRect.sizeDelta = new Vector2(820f, 25f);
            string offersHeaderFmt = LocalizationManager.L("Header_LevelLoansFmt", "<b>🟢 SEVİYE {0} BANKA KREDİ SEÇENEKLERİ:</b>", "<b>🟢 LEVEL {0} BANK LOAN OPTIONS:</b>");
            Text ohTxt = CreateTextInPanel(offersHeader.transform, Vector2.zero, Vector2.one, string.Format(offersHeaderFmt, storeLevel), 18, new Color(0.25f, 0.85f, 0.45f));
            ohTxt.alignment = TextAnchor.MiddleLeft;

            List<BankLoanOffer> offers = loanMgr != null ? loanMgr.GetOffersForStoreLevel(storeLevel) : new List<BankLoanOffer>();
            foreach (var offer in offers)
            {
                BankLoanOffer ofr = offer;
                GameObject offerCard = new GameObject("OfferCard_" + ofr.offerId);
                offerCard.transform.SetParent(financeLoansContent, false);
                RectTransform ocRect = offerCard.AddComponent<RectTransform>();
                ocRect.sizeDelta = new Vector2(820f, 95f);

                Image ocBg = offerCard.AddComponent<Image>();
                ocBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 95, 16, 2, new Color(0.25f, 0.80f, 0.45f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

                GameObject infoObj = new GameObject("Info");
                infoObj.transform.SetParent(offerCard.transform, false);
                RectTransform iRect = infoObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(-100f, 0f);
                iRect.sizeDelta = new Vector2(580f, 85f);
                Text iTxt = infoObj.AddComponent<Text>();
                iTxt.font = globalFont;
                string offerRowFmt = LocalizationManager.L(
                    "Offer_RowFmt",
                    "💰 <b>{0}</b>  <color=#A0AAB5>({1})</color>\n  • Çekilecek Net Tutar: <color=#00E676><b>{2:N0}C</b></color>   |   Faiz: <b>%{3:F0}</b>\n  • Günlük Taksit: <b>{4:N0}C</b> (10 Gün)   |   Toplam Geri Ödeme: <color=#FFD54F><b>{5:N0}C</b></color>",
                    "💰 <b>{0}</b>  <color=#A0AAB5>({1})</color>\n  • Net Loan Amount: <color=#00E676><b>{2:N0}C</b></color>   |   Interest: <b>{3:F0}%</b>\n  • Daily Installment: <b>{4:N0}C</b> (10 Days)   |   Total Repayment: <color=#FFD54F><b>{5:N0}C</b></color>"
                );
                iTxt.text = string.Format(offerRowFmt, ofr.LocalizedTitle, ofr.LocalizedDescription, ofr.principalAmount, ofr.interestRatePercent, ofr.dailyInstallment, ofr.totalRepayment);
                iTxt.fontSize = 17;
                iTxt.alignment = TextAnchor.MiddleLeft;
                iTxt.color = Color.white;

                // Kredi Çek Butonu
                GameObject takeBtn = new GameObject("TakeBtn");
                takeBtn.transform.SetParent(offerCard.transform, false);
                RectTransform tRect = takeBtn.AddComponent<RectTransform>();
                tRect.anchoredPosition = new Vector2(280f, 0f);
                tRect.sizeDelta = new Vector2(170f, 42f);

                Image tBg = takeBtn.AddComponent<Image>();
                tBg.sprite = UIStyleUtility.CreateRoundedPillSprite(170, 42, 21, new Color(0.20f, 0.75f, 0.40f));
                Button tBtn = takeBtn.AddComponent<Button>();
                tBtn.targetGraphic = tBg;
                tBtn.onClick.AddListener(() => {
                    if (loanMgr != null)
                    {
                        loanMgr.TakeLoan(ofr);
                        string modalTitle = LocalizationManager.L("Modal_LoanTaken_Title", "Kredi Çekildi! 🏦", "Loan Claimed! 🏦");
                        string modalBodyFmt = LocalizationManager.L(
                            "Modal_LoanTaken_Body",
                            "<b>{0}</b> başarıyla onaylandı ve {1:N0}C hesabınıza yatırıldı.\nHer gece yarısı {2:N0}C taksit tahsil edilecektir.",
                            "<b>{0}</b> successfully approved and {1:N0}C deposited to your account.\n{2:N0}C daily installment will be collected every midnight."
                        );
                        string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
                        ModalManager.ShowModal(modalTitle, string.Format(modalBodyFmt, ofr.LocalizedTitle, ofr.principalAmount, ofr.dailyInstallment), btnOk);
                    }
                });

                Text tTxt = CreateTextInPanel(takeBtn.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_TakeLoan", "💵 KREDİ ÇEK", "💵 CLAIM LOAN"), 17, Color.white);
                tTxt.alignment = TextAnchor.MiddleCenter;
                tTxt.fontStyle = FontStyle.Bold;
            }
        }

        private void RenderFinanceStockMarket()
        {
            if (financeStocksContent == null) return;
            foreach (Transform child in financeStocksContent) Destroy(child.gameObject);

            StockMarketManager stockMgr = StockMarketManager.Instance;
            if (stockMgr == null) return;

            // Üst Sınır Hizalı Konteyner (Pivot: 0.5, 1.0 -> Sekmelerin altında temiz başlangıç)
            GameObject containerObj = new GameObject("StockMarketContainer");
            containerObj.transform.SetParent(financeStocksContent, false);
            RectTransform cRect = containerObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 1f);
            cRect.anchorMax = new Vector2(0.5f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.anchoredPosition = new Vector2(0f, 0f);
            cRect.sizeDelta = new Vector2(820f, 370f);

            // Sol Taraf: Kaydırmalı 5 Şirket Listesi (Top-Aligned, Yükseklik 370px)
            GameObject leftListScrollObj = new GameObject("StockLeftListScroll");
            leftListScrollObj.transform.SetParent(containerObj.transform, false);
            RectTransform sllRect = leftListScrollObj.AddComponent<RectTransform>();
            sllRect.anchorMin = new Vector2(0.5f, 1f);
            sllRect.anchorMax = new Vector2(0.5f, 1f);
            sllRect.pivot = new Vector2(0.5f, 1f);
            sllRect.anchoredPosition = new Vector2(-255f, 0f);
            sllRect.sizeDelta = new Vector2(300f, 370f);

            Image sllBg = leftListScrollObj.AddComponent<Image>();
            sllBg.color = new Color(0.05f, 0.08f, 0.12f, 0.01f);
            sllBg.raycastTarget = true;

            ScrollRect leftScrollRect = leftListScrollObj.AddComponent<ScrollRect>();
            leftScrollRect.horizontal = false;
            leftScrollRect.vertical = true;
            leftScrollRect.movementType = ScrollRect.MovementType.Elastic;

            GameObject leftViewport = new GameObject("Viewport");
            leftViewport.transform.SetParent(leftListScrollObj.transform, false);
            RectTransform lvRect = leftViewport.AddComponent<RectTransform>();
            lvRect.anchorMin = Vector2.zero;
            lvRect.anchorMax = Vector2.one;
            lvRect.sizeDelta = Vector2.zero;

            Image lvImg = leftViewport.AddComponent<Image>();
            lvImg.color = Color.white;
            Mask mask = leftViewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject leftListContent = new GameObject("Content");
            leftListContent.transform.SetParent(leftViewport.transform, false);
            RectTransform lcRect = leftListContent.AddComponent<RectTransform>();
            lcRect.anchorMin = new Vector2(0f, 1f);
            lcRect.anchorMax = new Vector2(1f, 1f);
            lcRect.pivot = new Vector2(0.5f, 1f);
            lcRect.sizeDelta = new Vector2(0f, 0f);

            leftScrollRect.viewport = lvRect;
            leftScrollRect.content = lcRect;

            VerticalLayoutGroup llLayout = leftListContent.AddComponent<VerticalLayoutGroup>();
            llLayout.spacing = 8f;
            llLayout.childControlWidth = true;
            llLayout.childControlHeight = false;

            ContentSizeFitter csf = leftListContent.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            List<StockData> stocks = stockMgr.GetAllStocks();
            foreach (var sData in stocks)
            {
                StockData stock = sData;
                bool isSelected = (stock.tickerSymbol == selectedStockTicker);

                GameObject stockBtnObj = new GameObject("StockBtn_" + stock.tickerSymbol);
                stockBtnObj.transform.SetParent(leftListContent.transform, false);
                RectTransform sbRect = stockBtnObj.AddComponent<RectTransform>();
                sbRect.sizeDelta = new Vector2(285f, 58f);

                Color borderColor = isSelected ? new Color(0.20f, 0.85f, 1.0f) : new Color(0.35f, 0.40f, 0.50f);
                Color bgColor = isSelected ? new Color(0.15f, 0.25f, 0.38f, 0.95f) : new Color(0.12f, 0.15f, 0.20f, 0.90f);

                Image sbBg = stockBtnObj.AddComponent<Image>();
                sbBg.sprite = UIStyleUtility.CreateOutlinePillSprite(285, 58, 12, isSelected ? 2 : 1, borderColor, bgColor);

                Button btn = stockBtnObj.AddComponent<Button>();
                btn.targetGraphic = sbBg;
                btn.onClick.AddListener(() => {
                    selectedStockTicker = stock.tickerSymbol;
                    RenderFinanceStockMarket();
                });

                Color changeColor = stock.PriceChangePercent >= 0 ? new Color(0.0f, 0.90f, 0.45f) : new Color(1.0f, 0.32f, 0.32f);
                string arrow = stock.PriceChangePercent >= 0 ? "▲" : "▼";

                string priceLabelFmt = LocalizationManager.L("Stock_PriceLabelFmt", "Fiyat: <b>{0:F2}C</b>", "Price: <b>{0:F2}C</b>");
                Text sbText = CreateTextInPanel(stockBtnObj.transform, Vector2.zero, Vector2.one, $"<b>{stock.tickerSymbol}</b>  |  {stock.LocalizedCompanyName}\n{string.Format(priceLabelFmt, stock.currentPrice)}   <color=#{ColorUtility.ToHtmlStringRGB(changeColor)}>{stock.PriceChangePercent:+0.00;-0.00}% {arrow}</color>", 15, Color.white);
                sbText.alignment = TextAnchor.MiddleLeft;
                sbText.rectTransform.offsetMin = new Vector2(10f, 0f);
            }

            // Sağ Taraf: Detaylı Hisse Grafiği ve Al/Sat Paneli (Top-Aligned, Yükseklik 385px)
            StockData targetStock = stockMgr.GetStock(selectedStockTicker) ?? stocks[0];

            GameObject rightDetailObj = new GameObject("StockRightDetail");
            rightDetailObj.transform.SetParent(containerObj.transform, false);
            RectTransform rdRect = rightDetailObj.AddComponent<RectTransform>();
            rdRect.anchorMin = new Vector2(0.5f, 1f);
            rdRect.anchorMax = new Vector2(0.5f, 1f);
            rdRect.pivot = new Vector2(0.5f, 1f);
            rdRect.anchoredPosition = new Vector2(155f, 0f);
            rdRect.sizeDelta = new Vector2(500f, 370f);

            Image rdBg = rightDetailObj.AddComponent<Image>();
            rdBg.sprite = UIStyleUtility.CreateOutlinePillSprite(500, 370, 16, 2, new Color(0.20f, 0.85f, 1.0f), new Color(0.10f, 0.14f, 0.20f, 0.96f));

            // Başlık Kartı (Top: -10f)
            GameObject headerObj = new GameObject("HeaderInfo");
            headerObj.transform.SetParent(rightDetailObj.transform, false);
            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 1f);
            hRect.anchorMax = new Vector2(0.5f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = new Vector2(0f, -10f);
            hRect.sizeDelta = new Vector2(470f, 45f);

            Color signColor = targetStock.PriceChangePercent >= 0 ? new Color(0.0f, 0.90f, 0.45f) : new Color(1.0f, 0.32f, 0.32f);
            string signArrow = targetStock.PriceChangePercent >= 0 ? "▲" : "▼";

            string lastPriceFmt = LocalizationManager.L("Stock_LastPriceFmt", "Son Fiyat: <size=18><b>{0:F2}C</b></size>", "Last Price: <size=18><b>{0:F2}C</b></size>");
            Text hTxt = CreateTextInPanel(headerObj.transform, Vector2.zero, Vector2.one, $"📈 <b>{targetStock.LocalizedCompanyName} ({targetStock.tickerSymbol})</b>\n<size=14><color=#B0BEC5>{targetStock.LocalizedCategory}</color></size>   |   {string.Format(lastPriceFmt, targetStock.currentPrice)}  <color=#{ColorUtility.ToHtmlStringRGB(signColor)}><b>({targetStock.PriceChangePercent:+0.00;-0.00}% {signArrow})</b></color>", 17, Color.white);
            hTxt.alignment = TextAnchor.MiddleLeft;

            // --- 24 SAATLİK GERÇEKÇİ CANLI ÇİZGİ/BAR GRAFİĞİ (Top: -60f, Yükseklik 140px) ---
            GameObject chartCanvas = new GameObject("StockPriceChartCanvas");
            chartCanvas.transform.SetParent(rightDetailObj.transform, false);
            RectTransform chartRect = chartCanvas.AddComponent<RectTransform>();
            chartRect.anchorMin = new Vector2(0.5f, 1f);
            chartRect.anchorMax = new Vector2(0.5f, 1f);
            chartRect.pivot = new Vector2(0.5f, 1f);
            chartRect.anchoredPosition = new Vector2(0f, -60f);
            chartRect.sizeDelta = new Vector2(470f, 140f);

            Image chartBg = chartCanvas.AddComponent<Image>();
            chartBg.sprite = UIStyleUtility.CreateRoundedPillSprite(470, 140, 10, new Color(0.06f, 0.09f, 0.14f, 0.95f));

            CreateStockPriceChart(chartCanvas.transform, targetStock.priceHistory, signColor);

            // --- PORTFÖY & İŞLEM KARTI (Top: -210f) ---
            GameObject portfolioObj = new GameObject("PortfolioCard");
            portfolioObj.transform.SetParent(rightDetailObj.transform, false);
            RectTransform pRect = portfolioObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 1f);
            pRect.anchorMax = new Vector2(0.5f, 1f);
            pRect.pivot = new Vector2(0.5f, 1f);
            pRect.anchoredPosition = new Vector2(0f, -210f);
            pRect.sizeDelta = new Vector2(470f, 65f);

            Color plColor = targetStock.ProfitLoss >= 0 ? new Color(0.0f, 0.90f, 0.45f) : new Color(1.0f, 0.32f, 0.32f);

            Text pTxt = CreateTextInPanel(portfolioObj.transform, Vector2.zero, Vector2.one, "", 16, Color.white);
            string portFmt = LocalizationManager.L(
                "Stock_PortfolioFmt",
                "💼 <b>PORTFÖYÜNÜZ:</b>   Sahip Olunan: <b>{0:N0} Adet</b>   |   Ort. Alış: <b>{1:F2}C</b>\n  • Toplam Yatırılan: <b>{2:N0}C</b>   |   Güncel Kâr/Zarar: <color=#{3}><b>{4:N0}C ({5:+0.0;-0.0}%)</b></color>",
                "💼 <b>YOUR PORTFOLIO:</b>   Owned: <b>{0:N0} Shares</b>   |   Avg Cost: <b>{1:F2}C</b>\n  • Total Investment: <b>{2:N0}C</b>   |   Current Profit/Loss: <color=#{3}><b>{4:N0}C ({5:+0.0;-0.0}%)</b></color>"
            );
            pTxt.text = string.Format(portFmt, targetStock.ownedShares, targetStock.averageBuyPrice, targetStock.totalInvested, ColorUtility.ToHtmlStringRGB(plColor), targetStock.ProfitLoss, targetStock.ProfitLossPercent);
            pTxt.alignment = TextAnchor.MiddleLeft;

            // --- AL/SAT BUTONLARI & ADET SEÇİCİ (Top: -285f) ---
            GameObject tradeBarObj = new GameObject("TradeBar");
            tradeBarObj.transform.SetParent(rightDetailObj.transform, false);
            RectTransform tbRect = tradeBarObj.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.5f, 1f);
            tbRect.anchorMax = new Vector2(0.5f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.anchoredPosition = new Vector2(0f, -285f);
            tbRect.sizeDelta = new Vector2(470f, 40f);

            // Adet Düğmeleri (10, 50, 100, 500)
            int[] qtyOptions = new int[] { 10, 50, 100, 500 };
            for (int q = 0; q < qtyOptions.Length; q++)
            {
                int qtyVal = qtyOptions[q];
                GameObject qBtnObj = new GameObject("QtyBtn_" + qtyVal);
                qBtnObj.transform.SetParent(tradeBarObj.transform, false);
                RectTransform qRect = qBtnObj.AddComponent<RectTransform>();
                qRect.anchoredPosition = new Vector2(-190f + q * 48f, 0f);
                qRect.sizeDelta = new Vector2(42f, 34f);

                bool isSelQty = (stockTradeQuantity == qtyVal);
                Image qBg = qBtnObj.AddComponent<Image>();
                qBg.sprite = UIStyleUtility.CreateRoundedPillSprite(42, 34, 10, isSelQty ? new Color(0.20f, 0.70f, 0.95f) : new Color(0.20f, 0.25f, 0.32f));

                Button qBtn = qBtnObj.AddComponent<Button>();
                qBtn.targetGraphic = qBg;
                qBtn.onClick.AddListener(() => {
                    stockTradeQuantity = qtyVal;
                    RenderFinanceStockMarket();
                });

                Text qTxt = CreateTextInPanel(qBtnObj.transform, Vector2.zero, Vector2.one, $"{qtyVal}", 15, Color.white);
                qTxt.alignment = TextAnchor.MiddleCenter;
                qTxt.fontStyle = FontStyle.Bold;
            }

            // HİSSE AL (Yeşil) - Çakışmasız Konumlandırma (X = +55f)
            GameObject buyBtnObj = new GameObject("BuySharesBtn");
            buyBtnObj.transform.SetParent(tradeBarObj.transform, false);
            RectTransform buyRect = buyBtnObj.AddComponent<RectTransform>();
            buyRect.anchoredPosition = new Vector2(55f, 0f);
            buyRect.sizeDelta = new Vector2(120f, 36f);

            Image buyBg = buyBtnObj.AddComponent<Image>();
            buyBg.sprite = UIStyleUtility.CreateRoundedPillSprite(120, 36, 18, new Color(0.15f, 0.75f, 0.40f));
            Button buyBtn = buyBtnObj.AddComponent<Button>();
            buyBtn.targetGraphic = buyBg;
            buyBtn.onClick.AddListener(() => {
                bool ok = stockMgr.BuyShares(targetStock.tickerSymbol, stockTradeQuantity);
                if (!ok)
                {
                    string errTitle = LocalizationManager.L("Modal_LowBalance_Title", "Yetersiz Bakiye! ⚠️", "Insufficient Balance! ⚠️");
                    string errBody = string.Format(LocalizationManager.L("Modal_LowBalance_StockBody", "{0} Adet {1} hissesi almak için bakiyeniz yetersizdir.", "Insufficient balance to purchase {0} shares of {1}."), stockTradeQuantity, targetStock.tickerSymbol);
                    string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
                    ModalManager.ShowModal(errTitle, errBody, btnOk);
                }
            });

            string buySharesFmt = LocalizationManager.L("Btn_BuyShares", "📈 HİSSE AL ({0})", "📈 BUY SHARES ({0})");
            Text buyTxt = CreateTextInPanel(buyBtn.transform, Vector2.zero, Vector2.one, string.Format(buySharesFmt, stockTradeQuantity), 14, Color.white);
            buyTxt.alignment = TextAnchor.MiddleCenter;
            buyTxt.fontStyle = FontStyle.Bold;
            buyTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

            // HİSSE SAT (Kırmızı) - Çakışmasız Konumlandırma (X = +180f)
            GameObject sellBtnObj = new GameObject("SellSharesBtn");
            sellBtnObj.transform.SetParent(tradeBarObj.transform, false);
            RectTransform sellRect = sellBtnObj.AddComponent<RectTransform>();
            sellRect.anchoredPosition = new Vector2(180f, 0f);
            sellRect.sizeDelta = new Vector2(120f, 36f);

            Image sellBg = sellBtnObj.AddComponent<Image>();
            sellBg.sprite = UIStyleUtility.CreateRoundedPillSprite(120, 36, 18, new Color(0.90f, 0.30f, 0.25f));
            Button sellBtn = sellBtnObj.AddComponent<Button>();
            sellBtn.targetGraphic = sellBg;
            sellBtn.onClick.AddListener(() => {
                bool ok = stockMgr.SellShares(targetStock.tickerSymbol, stockTradeQuantity);
                if (!ok)
                {
                    string errTitle = LocalizationManager.L("Modal_LowShares_Title", "Yetersiz Hisse! ⚠️", "Insufficient Shares! ⚠️");
                    string errBody = string.Format(LocalizationManager.L("Modal_LowShares_Body", "Portföyünüzde satacak {0} adet {1} hissesi bulunmamaktadır.", "You do not own {0} shares of {1} to sell."), stockTradeQuantity, targetStock.tickerSymbol);
                    string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
                    ModalManager.ShowModal(errTitle, errBody, btnOk);
                }
            });

            string sellSharesFmt = LocalizationManager.L("Btn_SellShares", "📉 HİSSE SAT ({0})", "📉 SELL SHARES ({0})");
            Text sellTxt = CreateTextInPanel(sellBtn.transform, Vector2.zero, Vector2.one, string.Format(sellSharesFmt, stockTradeQuantity), 14, Color.white);
            sellTxt.alignment = TextAnchor.MiddleCenter;
            sellTxt.fontStyle = FontStyle.Bold;
            sellTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void CreateStockPriceChart(Transform parent, List<float> history, Color lineTrendColor)
        {
            if (history == null || history.Count == 0) return;

            float minP = float.MaxValue;
            float maxP = float.MinValue;
            foreach (float p in history)
            {
                if (p < minP) minP = p;
                if (p > maxP) maxP = p;
            }
            if (Mathf.Approximately(minP, maxP)) maxP += 1f;

            float chartWidth = 440f;
            float chartHeight = 100f;
            float stepX = chartWidth / (history.Count - 1);

            for (int i = 0; i < history.Count; i++)
            {
                float val = history[i];
                float normalizedY = (val - minP) / (maxP - minP);
                float posX = -chartWidth / 2f + i * stepX;
                float posY = -chartHeight / 2f + normalizedY * chartHeight;

                // Dikey Trend Sütun Çizgisi
                GameObject barObj = new GameObject("ChartBar_" + i);
                barObj.transform.SetParent(parent, false);

                RectTransform bRect = barObj.AddComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(posX, (posY - chartHeight / 2f) / 2f);
                bRect.sizeDelta = new Vector2(10f, Mathf.Max(6f, posY + chartHeight / 2f));

                Image bImg = barObj.AddComponent<Image>();
                bImg.color = new Color(lineTrendColor.r, lineTrendColor.g, lineTrendColor.b, 0.40f + (normalizedY * 0.50f));
            }
        }

        private void RenderFinanceProductsList()
        {
            if (financeProductsContent == null) return;

            foreach (Transform child in financeProductsContent)
            {
                Destroy(child.gameObject);
            }

            // ÜRÜN LİSTESİ (Seviyeye Göre Sıralı)
            List<WholesaleProductDef> products = WholesaleDatabase.GetAllProducts();
            if (products == null || products.Count == 0) return;

            // Seviyeye göre yukarıdan aşağı doğru sırala
            List<WholesaleProductDef> sortedProducts = new List<WholesaleProductDef>(products);
            sortedProducts.Sort((a, b) => {
                if (a.requiredLevel != b.requiredLevel) return a.requiredLevel.CompareTo(b.requiredLevel);
                return a.name.CompareTo(b.name);
            });

            string filterQuery = string.IsNullOrEmpty(currentFinanceProductSearchQuery) ? "" : currentFinanceProductSearchQuery.Trim().ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));

            foreach (var pDef in sortedProducts)
            {
                if (!string.IsNullOrEmpty(filterQuery))
                {
                    bool matchesName = pDef.LocalizedName.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(filterQuery) ||
                                       pDef.name.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(filterQuery) ||
                                       (!string.IsNullOrEmpty(pDef.nameEn) && pDef.nameEn.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(filterQuery));
                    bool matchesShelf = pDef.GetTargetShelfText().ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).Contains(filterQuery);
                    if (!matchesName && !matchesShelf) continue;
                }

                int currentSalePrice = pDef.CurrentSalePrice;
                bool overpriced = pDef.IsOverpriced;
                float margin = pDef.CurrentProfitMarginPercent;

                GameObject cardObj = new GameObject("ProductCard_" + pDef.id);
                cardObj.transform.SetParent(financeProductsContent, false);

                RectTransform cRect = cardObj.AddComponent<RectTransform>();
                cRect.sizeDelta = new Vector2(820f, 65f);

                Color borderColor = overpriced ? new Color(0.95f, 0.35f, 0.20f) : new Color(0.20f, 0.70f, 0.85f);
                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 65, 14, 2, borderColor, new Color(0.12f, 0.16f, 0.22f, 0.95f));
                cardBg.raycastTarget = true;

                // Sol Taraf: İkon, İsim, Seviye & Reyon
                GameObject infoObj = new GameObject("InfoContainer");
                infoObj.transform.SetParent(cardObj.transform, false);

                RectTransform iRect = infoObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(-210f, 0f);
                iRect.sizeDelta = new Vector2(360f, 55f);

                Text iText = infoObj.AddComponent<Text>();
                iText.font = globalFont;
                string costPerUnitFmt = LocalizationManager.L("Wholesale_CostPerUnitFmt", "Alış: {0:N0} Cr/Adet", "Cost: {0:N0} Cr/Pcs");
                iText.text = $"{pDef.iconEmoji} <b>{pDef.LocalizedName}</b>  <color=#FFD700>[Lvl {pDef.requiredLevel}]</color>\n<size=14><color=#8A94A6>{pDef.GetTargetShelfText()}   |   {string.Format(costPerUnitFmt, pDef.wholesaleUnitPrice)}</color></size>";
                iText.fontSize = 17;
                iText.fontStyle = FontStyle.Normal;
                iText.alignment = TextAnchor.MiddleLeft;
                iText.color = Color.white;
                iText.raycastTarget = false;

                // Orta/Sağ: Tepki Çeker / Kâr Durumu Rozeti
                GameObject statusBadgeObj = new GameObject("StatusBadge");
                statusBadgeObj.transform.SetParent(cardObj.transform, false);

                RectTransform sbBadgeRect = statusBadgeObj.AddComponent<RectTransform>();
                sbBadgeRect.anchoredPosition = new Vector2(40f, 0f);
                sbBadgeRect.sizeDelta = new Vector2(170f, 32f);

                Text sbBadgeText = statusBadgeObj.AddComponent<Text>();
                sbBadgeText.font = globalFont;
                if (overpriced)
                {
                    sbBadgeText.text = LocalizationManager.L("Badge_Overpriced", "⚠️ Tepki Çeker!\n<size=13><color=#FF6666>(Yüksek Fiyat)</color></size>", "⚠️ Overpriced!\n<size=13><color=#FF6666>(High Price)</color></size>");
                    sbBadgeText.color = new Color(1.0f, 0.35f, 0.25f);
                }
                else
                {
                    if (pDef.targetShelfType == FurnitureType.GourmetShelf || pDef.profitMarginPercent >= 70f)
                    {
                        string gourmetFmt = LocalizationManager.L("Badge_GourmetPrice", "🌟 Lüks Gurme\n<size=13><color=#FFD700>(+%{0:F0} Kâr Marjı)</color></size>", "🌟 Gourmet\n<size=13><color=#FFD700>(+{0:F0}% Profit)</color></size>");
                        sbBadgeText.text = string.Format(gourmetFmt, margin);
                        sbBadgeText.color = new Color(1.0f, 0.85f, 0.25f);
                    }
                    else
                    {
                        string fairFmt = LocalizationManager.L("Badge_FairPrice", "✅ Makul Fiyat\n<size=13><color=#50E678>(%{0:F0} Kâr Marjı)</color></size>", "✅ Fair Price\n<size=13><color=#50E678>(+{0:F0}% Profit Margin)</color></size>");
                        sbBadgeText.text = string.Format(fairFmt, margin);
                        sbBadgeText.color = new Color(0.35f, 0.90f, 0.50f);
                    }
                }
                sbBadgeText.fontSize = 14;
                sbBadgeText.fontStyle = FontStyle.Bold;
                sbBadgeText.alignment = TextAnchor.MiddleCenter;
                sbBadgeText.raycastTarget = false;

                // Sağ Taraf: Fiyat Düzenleme Butonları (- / Satış Fiyatı / +)
                GameObject priceCtrlObj = new GameObject("PriceController");
                priceCtrlObj.transform.SetParent(cardObj.transform, false);

                RectTransform pcRect = priceCtrlObj.AddComponent<RectTransform>();
                pcRect.anchoredPosition = new Vector2(285f, 0f);
                pcRect.sizeDelta = new Vector2(210f, 40f);

                // "-" Butonu
                string pId = pDef.id;
                int unitCost = pDef.wholesaleUnitPrice;
                GameObject minusBtn = CreateButtonInPanel(priceCtrlObj.transform, new Vector2(-70f, 0f), new Vector2(34f, 34f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                    int newPrice = Mathf.Max(unitCost, WholesaleDatabase.GetProductSalePrice(pId) - 1);
                    WholesaleDatabase.SetProductSalePrice(pId, newPrice);
                    RenderFinanceProductsList();
                }, 20);

                // Fiyat Etiketi
                GameObject priceTxtObj = new GameObject("PriceDisplay");
                priceTxtObj.transform.SetParent(priceCtrlObj.transform, false);
                RectTransform ptRect = priceTxtObj.AddComponent<RectTransform>();
                ptRect.anchoredPosition = new Vector2(0f, 0f);
                ptRect.sizeDelta = new Vector2(95f, 34f);

                Text ptText = priceTxtObj.AddComponent<Text>();
                ptText.font = globalFont;
                ptText.text = $"{currentSalePrice:N0}C";
                ptText.fontSize = 20;
                ptText.fontStyle = FontStyle.Bold;
                ptText.alignment = TextAnchor.MiddleCenter;
                ptText.color = new Color(1.0f, 0.88f, 0.35f);
                ptText.raycastTarget = false;

                // "+" Butonu
                GameObject plusBtn = CreateButtonInPanel(priceCtrlObj.transform, new Vector2(70f, 0f), new Vector2(34f, 34f), "+", new Color(0.25f, 0.75f, 0.40f), () => {
                    int newPrice = WholesaleDatabase.GetProductSalePrice(pId) + 1;
                    WholesaleDatabase.SetProductSalePrice(pId, newPrice);
                    RenderFinanceProductsList();
                }, 20);
            }
        }

        private void RenderFinanceSummaryDashboard()
        {
            if (financeSummaryContent == null) return;

            foreach (Transform child in financeSummaryContent)
            {
                Destroy(child.gameObject);
            }

            FinanceManager fin = FinanceManager.Instance;

            GameObject topCardsObj = new GameObject("TopCardsGrid");
            topCardsObj.transform.SetParent(financeSummaryContent, false);

            RectTransform tcRect = topCardsObj.AddComponent<RectTransform>();
            tcRect.sizeDelta = new Vector2(820f, 95f);

            GridLayoutGroup tcGrid = topCardsObj.AddComponent<GridLayoutGroup>();
            tcGrid.cellSize = new Vector2(260f, 95f);
            tcGrid.spacing = new Vector2(20f, 0f);

            Color profitColor = fin.NetProfit >= 0 ? new Color(0.20f, 0.85f, 0.40f) : new Color(0.90f, 0.25f, 0.20f);
            CreateSummaryCard(topCardsObj.transform, LocalizationManager.L("Card_NetProfit", "💚 NET KÂR", "💚 NET PROFIT"), $"{fin.NetProfit:N0}C", profitColor);
            CreateSummaryCard(topCardsObj.transform, LocalizationManager.L("Card_TotalRevenue", "📈 TOPLAM GELİR", "📈 TOTAL REVENUE"), $"{fin.TotalRevenue:N0}C", new Color(0.20f, 0.75f, 0.95f));
            CreateSummaryCard(topCardsObj.transform, LocalizationManager.L("Card_TotalExpenses", "📉 TOPLAM GİDER", "📉 TOTAL EXPENSE"), $"{fin.TotalExpenses:N0}C", new Color(0.95f, 0.65f, 0.15f));

            GameObject detailsCardObj = new GameObject("PerformanceDetailsCard");
            detailsCardObj.transform.SetParent(financeSummaryContent, false);

            RectTransform dcRect = detailsCardObj.AddComponent<RectTransform>();
            dcRect.sizeDelta = new Vector2(820f, 130f);

            Image dcBg = detailsCardObj.AddComponent<Image>();
            dcBg.sprite = UIStyleUtility.CreateRoundedPillSprite(820, 130, 16, new Color(0.12f, 0.16f, 0.22f, 0.95f));
            dcBg.raycastTarget = false;

            GameObject infoObj = new GameObject("InfoText");
            infoObj.transform.SetParent(detailsCardObj.transform, false);

            RectTransform iRect = infoObj.AddComponent<RectTransform>();
            iRect.anchoredPosition = new Vector2(20f, 0f);
            iRect.sizeDelta = new Vector2(780f, 120f);

            Text iText = infoObj.AddComponent<Text>();
            iText.font = globalFont;
            string finDetailsFmt = LocalizationManager.L(
                "Fin_DetailsFormat",
                "☀️ <b>GÜNLÜK FİNANS ÖZETİ (BUGÜN):</b>\n  • Gelir: +{0:N0}C   |   Gider: -{1:N0}C   |   <b>Günlük Net Kâr: {2:N0}C</b>\n\n📅 <b>AYLIK (MEVSİMLİK) PERFORMANS:</b>\n  • Aylık Gelir: +{3:N0}C   |   Aylık Gider: -{4:N0}C   |   <b>Aylık Net Kâr: {5:N0}C</b>",
                "☀️ <b>DAILY FINANCE SUMMARY (TODAY):</b>\n  • Revenue: +{0:N0}C   |   Expense: -{1:N0}C   |   <b>Daily Net Profit: {2:N0}C</b>\n\n📅 <b>MONTHLY (SEASONAL) PERFORMANCE:</b>\n  • Monthly Revenue: +{3:N0}C   |   Monthly Expense: -{4:N0}C   |   <b>Monthly Net Profit: {5:N0}C</b>"
            );
            iText.text = string.Format(finDetailsFmt, fin.DailyRevenue, fin.DailyExpenses, fin.DailyNetProfit, fin.MonthlyRevenue, fin.MonthlyExpenses, fin.MonthlyNetProfit);
            iText.fontSize = 18;
            iText.fontStyle = FontStyle.Normal;
            iText.alignment = TextAnchor.MiddleLeft;
            iText.color = new Color(0.90f, 0.94f, 0.98f);
            iText.raycastTarget = false;

            GameObject marginCardObj = new GameObject("MarginCard");
            marginCardObj.transform.SetParent(financeSummaryContent, false);

            RectTransform mcRect = marginCardObj.AddComponent<RectTransform>();
            mcRect.sizeDelta = new Vector2(820f, 65f);

            Image mcBg = marginCardObj.AddComponent<Image>();
            mcBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 65, 14, 2, new Color(0.75f, 0.35f, 0.95f), new Color(0.14f, 0.18f, 0.25f, 0.95f));
            mcBg.raycastTarget = false;

            GameObject mInfoObj = new GameObject("InfoText");
            mInfoObj.transform.SetParent(marginCardObj.transform, false);

            RectTransform miRect = mInfoObj.AddComponent<RectTransform>();
            miRect.anchorMin = Vector2.zero;
            miRect.anchorMax = Vector2.one;

            Text mText = mInfoObj.AddComponent<Text>();
            mText.font = globalFont;
            string marginFmt = LocalizationManager.L("Fin_MarginFormat", "📊 <b>İŞLETME KÂR MARJI & VERİMLİLİK ORANI:</b>   %{0:F1}", "📊 <b>BUSINESS PROFIT MARGIN & EFFICIENCY RATE:</b>   %{0:F1}");
            mText.text = string.Format(marginFmt, fin.ProfitMargin);
            mText.fontSize = 20;
            mText.fontStyle = FontStyle.Bold;
            mText.alignment = TextAnchor.MiddleCenter;
            mText.color = new Color(0.85f, 0.60f, 1.0f);
            mText.raycastTarget = false;
        }

        private void CreateSummaryCard(Transform parent, string title, string value, Color accentColor)
        {
            GameObject cardObj = new GameObject("Card_" + title);
            cardObj.transform.SetParent(parent, false);

            Image bg = cardObj.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateOutlinePillSprite(260, 95, 16, 2, accentColor, new Color(0.12f, 0.16f, 0.22f, 0.95f));
            bg.raycastTarget = false;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 20f);
            tRect.sizeDelta = new Vector2(240f, 30f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = title;
            tText.fontSize = 17;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = accentColor;
            tText.raycastTarget = false;

            GameObject valObj = new GameObject("Value");
            valObj.transform.SetParent(cardObj.transform, false);
            RectTransform vRect = valObj.AddComponent<RectTransform>();
            vRect.anchoredPosition = new Vector2(0f, -15f);
            vRect.sizeDelta = new Vector2(240f, 40f);

            Text vText = valObj.AddComponent<Text>();
            vText.font = globalFont;
            vText.text = value;
            vText.fontSize = 26;
            vText.fontStyle = FontStyle.Bold;
            vText.alignment = TextAnchor.MiddleCenter;
            vText.color = Color.white;
            vText.raycastTarget = false;
        }

        private void RenderFinanceTransactionHistory()
        {
            if (financeHistoryContent == null) return;

            foreach (Transform child in financeHistoryContent)
            {
                Destroy(child.gameObject);
            }

            List<TransactionRecord> history = FinanceManager.Instance.GetTransactionHistory();

            if (history == null || history.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyHistoryMsg");
                emptyObj.transform.SetParent(financeHistoryContent, false);

                RectTransform eRect = emptyObj.AddComponent<RectTransform>();
                eRect.sizeDelta = new Vector2(820f, 150f);

                Text eText = emptyObj.AddComponent<Text>();
                eText.font = globalFont;
                eText.text = LocalizationManager.L("Msg_EmptyHistory", "ℹ️ Henüz kaydedilmiş işlem dökümü bulunmuyor.", "ℹ️ No transaction history recorded yet.");
                eText.fontSize = 20;
                eText.fontStyle = FontStyle.Bold;
                eText.alignment = TextAnchor.MiddleCenter;
                eText.color = new Color(0.80f, 0.85f, 0.90f);
                eText.raycastTarget = false;
                return;
            }

            foreach (var trx in history)
            {
                GameObject cardObj = new GameObject("TrxCard_" + trx.id);
                cardObj.transform.SetParent(financeHistoryContent, false);

                RectTransform cRect = cardObj.AddComponent<RectTransform>();
                cRect.sizeDelta = new Vector2(820f, 48f);

                string sign = trx.isIncome ? "+" : "-";

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateRoundedPillSprite(820, 48, 12, new Color(0.14f, 0.18f, 0.24f, 0.90f));
                cardBg.raycastTarget = false;

                GameObject infoObj = new GameObject("InfoText");
                infoObj.transform.SetParent(cardObj.transform, false);

                RectTransform iRect = infoObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(15f, 0f);
                iRect.sizeDelta = new Vector2(780f, 45f);

                Text iText = infoObj.AddComponent<Text>();
                iText.font = globalFont;

                string timeStampLoc = trx.timeStamp
                    .Replace("İLKBAHAR", LocalizationManager.L("Season_Spring", "İLKBAHAR", "SPRING"))
                    .Replace("YAZ", LocalizationManager.L("Season_Summer", "YAZ", "SUMMER"))
                    .Replace("SONBAHAR", LocalizationManager.L("Season_Autumn", "SONBAHAR", "AUTUMN"))
                    .Replace("KIŞ", LocalizationManager.L("Season_Winter", "KIŞ", "WINTER"))
                    .Replace("GÜN", LocalizationManager.L("Label_Day", "GÜN", "DAY"));

                string categoryLoc = trx.category
                    .Replace("Toptan/Alışveriş", LocalizationManager.L("TrxCat_Wholesale", "Toptan/Alışveriş", "Wholesale/Shopping"))
                    .Replace("Geliştirme", LocalizationManager.L("TrxCat_Expansion", "Geliştirme", "Expansion"))
                    .Replace("Borsa Yatırımı", LocalizationManager.L("TrxCat_Stock", "Borsa Yatırımı", "Stock Investment"))
                    .Replace("Borsa Geliri", LocalizationManager.L("TrxCat_StockIncome", "Borsa Geliri", "Stock Revenue"))
                    .Replace("Maaş", LocalizationManager.L("TrxCat_Salary", "Maaş", "Salary"))
                    .Replace("Banka Kredisi Taksiti", LocalizationManager.L("TrxCat_BankLoanInst", "Banka Kredisi Taksiti", "Bank Loan Installment"))
                    .Replace("Banka Kredisi Ödemesi", LocalizationManager.L("TrxCat_BankLoanPayoff", "Banka Kredisi Ödemesi", "Bank Loan Payoff"))
                    .Replace("Banka Kredisi", LocalizationManager.L("TrxCat_BankLoan", "Banka Kredisi", "Bank Loan"))
                    .Replace("Satış", LocalizationManager.L("TrxCat_Sales", "Satış", "Sales"))
                    .Replace("Pasif Gelir", LocalizationManager.L("TrxCat_Passive", "Pasif Gelir", "Passive Income"));

                string descLoc = trx.description
                    .Replace("Toptancı & Mobilya & Tohum Siparişi", LocalizationManager.L("TrxDesc_OrderShort", "Toptancı & Mobilya & Tohum Siparişi", "Wholesale & Furniture & Seed Order"))
                    .Replace("Toplu Sipariş", LocalizationManager.L("TrxDesc_BulkOrderShort", "Toplu Sipariş", "Bulk Order"))
                    .Replace("Kalem", LocalizationManager.L("Label_Items", "Kalem", "Items"))
                    .Replace("Koli", LocalizationManager.L("Label_Packs", "Koli", "Packs"))
                    .Replace("İndirimli", LocalizationManager.L("Label_Discounted", "İndirimli", "Discounted"))
                    .Replace("Market Seviye", LocalizationManager.L("Label_StoreLevel", "Market Seviye", "Store Level"))
                    .Replace("Genişletme", LocalizationManager.L("Label_Expansion", "Genişletme", "Expansion"))
                    .Replace("Hisse Alındı", LocalizationManager.L("Label_BoughtShares", "Hisse Alındı", "Bought Shares"))
                    .Replace("Hisse Satıldı", LocalizationManager.L("Label_SoldShares", "Hisse Satıldı", "Sold Shares"))
                    .Replace("Adet", LocalizationManager.L("Label_Pcs", "Adet", "Pcs"))
                    .Replace("Gece Yarısı Maaş Ödemesi", LocalizationManager.L("Label_MidnightSalary", "Gece Yarısı Maaş Ödemesi", "Midnight Payroll"))
                    .Replace("Mağaza", LocalizationManager.L("Label_StoreStaff", "Mağaza", "Store"))
                    .Replace("Çiftçi", LocalizationManager.L("Label_FarmStaff", "Çiftçi", "Farmer"))
                    .Replace("Kredi Erken Kapatıldı", LocalizationManager.L("Label_LoanEarlyClosed", "Kredi Erken Kapatıldı", "Loan Paid Off Early"))
                    .Replace("Günlük Taksit", LocalizationManager.L("Label_DailyInstallment", "Günlük Taksit", "Daily Installment"))
                    .Replace("Banka Kredisi Çekildi", LocalizationManager.L("Label_LoanClaimed", "Banka Kredisi Çekildi", "Bank Loan Claimed"))
                    .Replace("Müşteri Alışverişi", LocalizationManager.L("Label_CustomerShopping", "Müşteri Alışverişi", "Customer Purchase"))
                    .Replace("Parça Ürün", LocalizationManager.L("Label_ItemsBought", "Parça Ürün", "Items Bought"))
                    .Replace("Satışı (%50 İade)", LocalizationManager.L("Label_SaleRefund", "Satışı (%50 İade)", "Sale (50% Refund)"))
                    .Replace("Hırsızdan Kurtarılan Ürün", LocalizationManager.L("Label_StolenRecovered", "Hırsızdan Kurtarılan Ürün", "Recovered Stolen Item"))
                    .Replace("Pasif Satış", LocalizationManager.L("Label_PassiveSale", "Pasif Satış", "Passive Sale"));

                iText.text = $"🕒 <color=#A0A8B5>{timeStampLoc}</color>  |  <b>[{categoryLoc}]</b>  {descLoc}   ➜   <b><color={(trx.isIncome ? "#32E664" : "#F54848")}>{sign}{trx.amount:N0}C</color></b>";
                iText.fontSize = 17;
                iText.fontStyle = FontStyle.Normal;
                iText.alignment = TextAnchor.MiddleLeft;
                iText.color = new Color(0.92f, 0.94f, 0.96f);
            }
        }

        public static string GetLocalizedShiftHours(string shiftStr)
        {
            if (string.IsNullOrEmpty(shiftStr)) return "";
            if (shiftStr.Contains("Sabah") || shiftStr.Contains("Gündüz") || shiftStr.Contains("Morning") || shiftStr.Contains("Day") || shiftStr.Contains("08:00") || shiftStr.Contains("06:00"))
            {
                return LocalizationManager.L("ShiftFull_Morning", "☀️ Sabah (08:00 - 16:00)", "☀️ Morning (08:00 - 16:00)");
            }
            if (shiftStr.Contains("Akşam") || shiftStr.Contains("Evening") || shiftStr.Contains("Gece") || shiftStr.Contains("Night") || shiftStr.Contains("22:00") || shiftStr.Contains("24:00") || shiftStr.Contains("14:00"))
            {
                return LocalizationManager.L("ShiftFull_Evening", "🌆 Akşam (16:00 - 24:00)", "🌆 Evening (16:00 - 24:00)");
            }
            return shiftStr;
        }

        private void RenderCategorizedStaffList()
        {
            if (staffListContent == null) return;

            foreach (Transform child in staffListContent)
            {
                Destroy(child.gameObject);
            }

            List<StaffMember> staffList = StaffManager.Instance.GetActiveStaff();

            if (staffList == null || staffList.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyStaffMsg");
                emptyObj.transform.SetParent(staffListContent, false);

                LayoutElement eElem = emptyObj.AddComponent<LayoutElement>();
                eElem.minHeight = 100f;

                Text eText = CreateTextInPanel(emptyObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Msg_NoStaffHired", "Henüz işe alınmış personeliniz bulunmuyor.\n'➕ İşe Alım' sekmesinden personel ekleyebilirsiniz.", "No active staff members yet.\nYou can recruit staff from the '➕ Hire Staff' tab."), 17, Color.gray);
                eText.alignment = TextAnchor.MiddleCenter;
                return;
            }

            for (int r = 0; r < 6; r++)
            {
                StaffRole roleEnum = (StaffRole)r;
                List<StaffMember> roleStaff = staffList.FindAll(s => s.role == roleEnum);
                if (roleStaff.Count == 0) continue;

                GameObject headerObj = new GameObject("RoleHeader_" + r);
                headerObj.transform.SetParent(staffListContent, false);

                RectTransform hRect = headerObj.AddComponent<RectTransform>();
                hRect.sizeDelta = new Vector2(820f, 32f);

                LayoutElement hElem = headerObj.AddComponent<LayoutElement>();
                hElem.minHeight = 32f;
                hElem.preferredHeight = 32f;

                Image hBg = headerObj.AddComponent<Image>();
                hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(820, 32, 8, roleCategoryColors[r] * 0.35f);
                hBg.raycastTarget = false;

                Text hText = CreateTextInPanel(headerObj.transform, new Vector2(10f, 0f), new Vector2(800f, 30f), $"<b>{GetRoleCategoryName(r)} ({roleStaff.Count} Kişi)</b>", 16, roleCategoryColors[r]);
                hText.alignment = TextAnchor.MiddleLeft;

                foreach (var staff in roleStaff)
                {
                    GameObject cardObj = new GameObject("StaffCard_" + staff.id);
                    cardObj.transform.SetParent(staffListContent, false);

                    RectTransform cRect = cardObj.AddComponent<RectTransform>();
                    cRect.sizeDelta = new Vector2(820f, 52f);

                    LayoutElement cElem = cardObj.AddComponent<LayoutElement>();
                    cElem.minHeight = 52f;
                    cElem.preferredHeight = 52f;

                    Image cardBg = cardObj.AddComponent<Image>();
                    cardBg.sprite = UIStyleUtility.CreateRoundedPillSprite(820, 52, 12, new Color(0.14f, 0.18f, 0.24f, 0.90f));
                    cardBg.raycastTarget = false;

                    GameObject infoObj = new GameObject("InfoText");
                    infoObj.transform.SetParent(cardObj.transform, false);

                    RectTransform iRect = infoObj.AddComponent<RectTransform>();
                    iRect.anchoredPosition = new Vector2(-120f, 0f);
                    iRect.sizeDelta = new Vector2(540f, 45f);

                    Text iText = infoObj.AddComponent<Text>();
                    iText.font = globalFont;
                    string staffCardInfoFmt = LocalizationManager.L(
                        "StaffCard_InfoFmt",
                        "👤 {0}   |   ⏰ Vardiya: {1}   |   💰 Günlük Maaş: {2}C",
                        "👤 {0}   |   ⏰ Shift: {1}   |   💰 Salary: {2}C/Day"
                    );
                    iText.text = string.Format(staffCardInfoFmt, staff.name, GetLocalizedShiftHours(staff.shiftHours), staff.dailySalary);
                    iText.fontSize = 16;
                    iText.fontStyle = FontStyle.Bold;
                    iText.alignment = TextAnchor.MiddleLeft;
                    iText.color = Color.white;
                    iText.raycastTarget = false;

                    GameObject fireBtnObj = new GameObject("FireBtn");
                    fireBtnObj.transform.SetParent(cardObj.transform, false);

                    RectTransform fRect = fireBtnObj.AddComponent<RectTransform>();
                    fRect.anchoredPosition = new Vector2(330f, 0f);
                    fRect.sizeDelta = new Vector2(120f, 34f);

                    Image fBg = fireBtnObj.AddComponent<Image>();
                    fBg.sprite = UIStyleUtility.CreateRoundedPillSprite(120, 34, 17, new Color(0.75f, 0.20f, 0.20f, 0.90f));
                    fBg.raycastTarget = true;

                    Button fBtn = fireBtnObj.AddComponent<Button>();
                    fBtn.targetGraphic = fBg;
                    var currentStaffId = staff.id;
                    fBtn.onClick.AddListener(() => {
                        StaffManager.Instance.FireStaff(currentStaffId);
                        RenderCategorizedStaffList();
                    });

                    GameObject ftObj = new GameObject("Text");
                    ftObj.transform.SetParent(fireBtnObj.transform, false);
                    RectTransform ftRect = ftObj.AddComponent<RectTransform>();
                    ftRect.anchorMin = Vector2.zero;
                    ftRect.anchorMax = Vector2.one;

                    Text ftText = ftObj.AddComponent<Text>();
                    ftText.font = globalFont;
                    ftText.text = LocalizationManager.L("Btn_FireStaff", "🚫 İşten Çıkar", "🚫 Dismiss");
                    ftText.fontSize = 14;
                    ftText.fontStyle = FontStyle.Bold;
                    ftText.alignment = TextAnchor.MiddleCenter;
                    ftText.color = Color.white;
                    ftText.raycastTarget = false;
                }
            }
        }

        private void RenderPermanentRoleRecruitmentList()
        {
            if (candidateListContent == null) return;

            foreach (Transform child in candidateListContent)
            {
                Destroy(child.gameObject);
            }

            string[] roleDescriptions = new string[] {
                LocalizationManager.L("RoleDesc_Cashier", "Kasa ve ödemeler", "Checkout & payments"),
                LocalizationManager.L("RoleDesc_Stocker", "Depodan reyon dizimi", "Shelf restocking from warehouse"),
                LocalizationManager.L("RoleDesc_Cleaner", "Hijyen ve temizlik", "Store sanitation & hygiene"),
                LocalizationManager.L("RoleDesc_Guard", "Otopark & Güvenlik", "Parking & store security"),
                LocalizationManager.L("RoleDesc_Support", "Müşteri ilişkileri", "Customer assistance"),
                LocalizationManager.L("RoleDesc_Mascot", "Reklam & Tanıtım şovu", "Advertising & promotion show")
            };

            for (int r = 0; r < 6; r++)
            {
                StaffRole roleEnum = (StaffRole)r;
                int salary = (StaffManager.Instance != null) ? StaffManager.Instance.GetRoleDailySalary(roleEnum) : 100;

                GameObject cardObj = new GameObject("RoleCard_" + r);
                cardObj.transform.SetParent(candidateListContent, false);

                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(265, 160, 16, 2, roleCategoryColors[r], new Color(0.12f, 0.16f, 0.22f, 0.90f));
                cardBg.raycastTarget = false;

                GameObject infoObj = new GameObject("InfoText");
                infoObj.transform.SetParent(cardObj.transform, false);

                RectTransform iRect = infoObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(0f, 25f);
                iRect.sizeDelta = new Vector2(245f, 90f);

                Text iText = infoObj.AddComponent<Text>();
                iText.font = globalFont;
                iText.text = $"<b>{GetRoleCategoryName(r)}</b>\n<size=14><color=#A0B0C0>{roleDescriptions[r]}</color></size>\n\n<color=#4CD964>💰 Günlük Maaş: {salary} Credit</color>";
                iText.fontSize = 16;
                iText.alignment = TextAnchor.MiddleCenter;
                iText.color = Color.white;
                iText.raycastTarget = false;

                GameObject hireBtnObj = new GameObject("HireBtn");
                hireBtnObj.transform.SetParent(cardObj.transform, false);

                RectTransform hRect = hireBtnObj.AddComponent<RectTransform>();
                hRect.anchoredPosition = new Vector2(0f, -48f);
                hRect.sizeDelta = new Vector2(210f, 36f);

                Image hBg = hireBtnObj.AddComponent<Image>();
                hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(210, 36, 18, new Color(0.18f, 0.55f, 0.28f, 0.95f));
                hBg.raycastTarget = true;

                Button hBtn = hireBtnObj.AddComponent<Button>();
                hBtn.targetGraphic = hBg;
                var currentRole = roleEnum;
                hBtn.onClick.AddListener(() => {
                    StaffMember hired = StaffManager.Instance.HireStaffByRole(currentRole);
                    if (hired != null)
                    {
                        if (TutorialManager.Instance != null)
                        {
                            TutorialManager.Instance.NotifyStaffHired(currentRole);
                        }

                        string title = LocalizationManager.L("Modal_HireSuccess_Title", "İşe Alım Başarılı! 🎉", "Recruitment Successful! 🎉");
                        string roleName = GetRoleCategoryName((int)currentRole);
                        string bodyFmt = LocalizationManager.L(
                            "Modal_StoreHireSuccess_Body",
                            "<b>{0}</b> başarıyla <b>{1}</b> pozisyonunda işe alındı!\n\nGünlük Maaş: <b>{2} Credit</b> (Gece 00:00'da kesilir).\n\nVardiya ayarlarını 'Vardiyalar' sekmesinden düzenleyebilirsiniz.",
                            "<b>{0}</b> was successfully hired for the position of <b>{1}</b>!\n\nDaily Salary: <b>{2} Credit</b> (Deducted at midnight 00:00).\n\nYou can manage shift schedules from the 'Shifts' tab."
                        );
                        string okBtn = LocalizationManager.L("Btn_OK", "Tamam", "OK");

                        ModalManager.ShowModal(
                            title,
                            string.Format(bodyFmt, hired.name, roleName, hired.dailySalary),
                            okBtn
                        );
                    }
                });

                GameObject htObj = new GameObject("Text");
                htObj.transform.SetParent(hireBtnObj.transform, false);
                RectTransform htRect = htObj.AddComponent<RectTransform>();
                htRect.anchorMin = Vector2.zero;
                htRect.anchorMax = Vector2.one;

                Text htText = htObj.AddComponent<Text>();
                htText.font = globalFont;
                htText.text = LocalizationManager.L("Btn_HireStaff", "➕ İŞE AL", "➕ HIRE STAFF");
                htText.fontSize = 16;
                htText.fontStyle = FontStyle.Bold;
                htText.alignment = TextAnchor.MiddleCenter;
                htText.color = Color.white;
                htText.raycastTarget = false;
            }
        }

        private void RenderCategorizedShiftManagementList()
        {
            if (shiftListContent == null) return;

            foreach (Transform child in shiftListContent)
            {
                Destroy(child.gameObject);
            }

            List<StaffMember> staffList = StaffManager.Instance.GetActiveStaff();

            if (staffList == null || staffList.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyShiftMsg");
                emptyObj.transform.SetParent(shiftListContent, false);

                LayoutElement eElem = emptyObj.AddComponent<LayoutElement>();
                eElem.minHeight = 100f;

                Text eText = CreateTextInPanel(emptyObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Msg_NoShiftStaff", "Vardiyası ayarlanacak personel bulunmuyor.\nLütfen önce '➕ İşe Alım' sekmesinden personel ekleyin.", "No active staff members to manage shifts.\nPlease recruit staff from the '➕ Hire Staff' tab first."), 17, Color.gray);
                eText.alignment = TextAnchor.MiddleCenter;
                return;
            }

            for (int r = 0; r < 6; r++)
            {
                StaffRole roleEnum = (StaffRole)r;
                List<StaffMember> roleStaff = staffList.FindAll(s => s.role == roleEnum);
                if (roleStaff.Count == 0) continue;

                GameObject headerObj = new GameObject("RoleShiftHeader_" + r);
                headerObj.transform.SetParent(shiftListContent, false);

                RectTransform hRect = headerObj.AddComponent<RectTransform>();
                hRect.sizeDelta = new Vector2(820f, 32f);

                LayoutElement hElem = headerObj.AddComponent<LayoutElement>();
                hElem.minHeight = 32f;
                hElem.preferredHeight = 32f;

                Image hBg = headerObj.AddComponent<Image>();
                hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(820, 32, 8, roleCategoryColors[r] * 0.35f);
                hBg.raycastTarget = false;

                Text hText = CreateTextInPanel(headerObj.transform, new Vector2(10f, 0f), new Vector2(800f, 30f), $"<b>{GetRoleCategoryName(r)} Vardiya Düzeni ({roleStaff.Count} Kişi)</b>", 16, roleCategoryColors[r]);
                hText.alignment = TextAnchor.MiddleLeft;

                foreach (var staff in roleStaff)
                {
                    GameObject cardObj = new GameObject("ShiftCard_" + staff.id);
                    cardObj.transform.SetParent(shiftListContent, false);

                    RectTransform cRect = cardObj.AddComponent<RectTransform>();
                    cRect.sizeDelta = new Vector2(820f, 52f);

                    LayoutElement cElem = cardObj.AddComponent<LayoutElement>();
                    cElem.minHeight = 52f;
                    cElem.preferredHeight = 52f;

                    Image cardBg = cardObj.AddComponent<Image>();
                    cardBg.sprite = UIStyleUtility.CreateRoundedPillSprite(820, 52, 12, new Color(0.14f, 0.18f, 0.24f, 0.90f));
                    cardBg.raycastTarget = false;

                    GameObject nameObj = new GameObject("StaffNameText");
                    nameObj.transform.SetParent(cardObj.transform, false);

                    RectTransform nRect = nameObj.AddComponent<RectTransform>();
                    nRect.anchoredPosition = new Vector2(-265f, 0f);
                    nRect.sizeDelta = new Vector2(270f, 45f);

                    Text nText = nameObj.AddComponent<Text>();
                    nText.font = globalFont;
                    nText.text = $"👤 {staff.name}\n⏰ {GetLocalizedShiftHours(staff.shiftHours)}";
                    nText.fontSize = 15;
                    nText.fontStyle = FontStyle.Bold;
                    nText.alignment = TextAnchor.MiddleLeft;
                    nText.color = Color.white;
                    nText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    nText.verticalOverflow = VerticalWrapMode.Truncate;
                    nText.raycastTarget = false;

                    // --- SADECE VE SADECE REYONCU İÇİN SABAH VARDİYASINDA 06:00 - 08:00 AM & DÜKKAN KAPALIYKEN ERKEN ÇAĞIR BUTONU ---
                    CreateEarlyCallButton(cardObj.transform, staff);

                    CreateShiftOptionButtons(cardObj.transform, staff);
                }
            }
        }

        private void CreateEarlyCallButton(Transform parent, StaffMember staff)
        {
            if (staff == null) return;

            // SADECE VE SADECE REYONCU (Restocker) İÇİN ERKEN ÇAĞIR BUTONU BULUNUR!
            if (staff.role != StaffRole.Reyoncu) return;

            bool isMorningShift = (staff.shiftHours != null && (staff.shiftHours.Contains("Sabah") || staff.shiftHours.Contains("Gündüz") || staff.shiftHours.Contains("08:00") || staff.shiftHours.Contains("06:00")));
            bool isEarlyMorning = (TimeManager.Instance != null && TimeManager.Instance.Hour >= 6 && TimeManager.Instance.Hour < 8);
            bool isStoreClosed = (StoreStatusManager.Instance != null && !StoreStatusManager.Instance.IsOpen);

            // SADECE Sabah vardiyasındaki reyoncular ve SADECE 06:00 - 08:00 AM & Dükkan Kapalıyken gösterilir!
            if (!isMorningShift || !isEarlyMorning || !isStoreClosed) return;

            bool isAlreadyCalled = (StaffVisualManager.Instance != null && StaffVisualManager.Instance.IsStaffCalledEarlyToday(staff.id));

            GameObject btnObj = new GameObject("Btn_EarlyCall_" + staff.id);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-110f, 0f);
            rect.sizeDelta = new Vector2(138f, 32f);

            Image bg = btnObj.AddComponent<Image>();
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(6f, 1f);
            tRect.offsetMax = new Vector2(-6f, -1f);

            Text txt = textObj.AddComponent<Text>();
            txt.font = globalFont;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;

            if (isAlreadyCalled)
            {
                bg.sprite = UIStyleUtility.CreateRoundedPillSprite(138, 32, 16, new Color(0.18f, 0.45f, 0.25f, 0.90f));
                txt.text = LocalizationManager.L("Btn_OnDuty", "✅ Görevde", "✅ On Duty");
                txt.color = new Color(0.40f, 0.95f, 0.55f);
                btn.interactable = false;
            }
            else
            {
                bg.sprite = UIStyleUtility.CreateOutlinePillSprite(138, 32, 16, 2, new Color(1.0f, 0.70f, 0.20f), new Color(0.22f, 0.16f, 0.05f, 0.95f));
                txt.text = LocalizationManager.L("Btn_CallEarly", "⚡ Erken Çağır (50C)", "⚡ Call Early (50C)");
                txt.color = new Color(1.0f, 0.88f, 0.35f);

                btn.onClick.AddListener(() => {
                    if (StaffVisualManager.Instance != null)
                    {
                        bool success = StaffVisualManager.Instance.ForceSpawnStaffEarly(staff);
                        if (success)
                        {
                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyStaffCalledEarly();
                            }
                            RenderCategorizedShiftManagementList();
                        }
                    }
                });
            }
        }

        private void CreateShiftOptionButtons(Transform parent, StaffMember staff)
        {
            GameObject optsObj = new GameObject("ShiftOptions");
            optsObj.transform.SetParent(parent, false);

            RectTransform oRect = optsObj.AddComponent<RectTransform>();
            oRect.anchoredPosition = new Vector2(200f, 0f);
            oRect.sizeDelta = new Vector2(380f, 40f);

            HorizontalLayoutGroup layout = optsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleRight;

            string[] shiftNames = new string[] {
                LocalizationManager.L("Shift_Morning", "Sabah", "Morning"),
                LocalizationManager.L("Shift_Evening", "Akşam", "Evening")
            };
            string[] shiftFullNames = new string[] {
                LocalizationManager.L("ShiftFull_Morning", "☀️ Sabah (08:00 - 16:00)", "☀️ Morning (08:00 - 16:00)"),
                LocalizationManager.L("ShiftFull_Evening", "🌆 Akşam (16:00 - 24:00)", "🌆 Evening (16:00 - 24:00)")
            };
            string currentShiftStr = staff.shiftHours ?? "";

            bool isEvening = currentShiftStr.Contains("Akşam") || currentShiftStr.Contains("Evening") || currentShiftStr.Contains("Gece") || currentShiftStr.Contains("Night") || currentShiftStr.Contains("16:00 - 24:00") || currentShiftStr.Contains("24:00");
            bool isMorning = !isEvening;

            for (int i = 0; i < 2; i++)
            {
                int shiftIndex = i;
                string targetShift = shiftFullNames[shiftIndex];
                bool isCurrentShift = (shiftIndex == 0) ? isMorning : isEvening;

                string staffId = staff.id;
                string selectedShift = targetShift;

                GameObject btnObj = new GameObject("ShiftBtn_" + shiftIndex);
                btnObj.transform.SetParent(optsObj.transform, false);

                RectTransform bRect = btnObj.AddComponent<RectTransform>();
                bRect.sizeDelta = new Vector2(118f, 34f);

                Image bg = btnObj.AddComponent<Image>();
                if (isCurrentShift)
                {
                    bg.sprite = UIStyleUtility.CreateOutlinePillSprite(118, 34, 17, 2, new Color(0.20f, 0.85f, 0.40f), new Color(0.12f, 0.42f, 0.22f, 0.95f));
                }
                else
                {
                    bg.sprite = UIStyleUtility.CreateOutlinePillSprite(118, 34, 17, 1, new Color(0.25f, 0.35f, 0.48f, 0.75f), new Color(0.12f, 0.16f, 0.22f, 0.85f));
                }
                bg.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => {
                    if (StaffManager.Instance != null)
                    {
                        StaffManager.Instance.UpdateStaffShift(staffId, selectedShift);
                        if (TutorialManager.Instance != null)
                        {
                            TutorialManager.Instance.NotifyStaffShiftChanged();
                        }
                        RenderCategorizedShiftManagementList();
                    }
                });

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
                RectTransform tRect = textObj.AddComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;

                Text btnText = textObj.AddComponent<Text>();
                btnText.font = globalFont;
                btnText.text = shiftNames[shiftIndex];
                btnText.fontSize = 15;
                btnText.fontStyle = FontStyle.Bold;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = isCurrentShift ? Color.white : new Color(0.75f, 0.80f, 0.85f);
                btnText.raycastTarget = false;
            }
        }

        private void UpdateShiftButtonsVisual(Transform optsTransform, bool isMorningSelected)
        {
            if (optsTransform == null) return;
            for (int k = 0; k < 2; k++)
            {
                Transform btnChild = optsTransform.Find("ShiftBtn_" + k);
                if (btnChild != null)
                {
                    bool isCur = (k == 0) ? isMorningSelected : !isMorningSelected;
                    Image bg = btnChild.GetComponent<Image>();
                    if (bg != null)
                    {
                        bg.sprite = isCur
                            ? UIStyleUtility.CreateOutlinePillSprite(118, 34, 17, 2, new Color(0.20f, 0.85f, 0.40f), new Color(0.12f, 0.42f, 0.22f, 0.95f))
                            : UIStyleUtility.CreateOutlinePillSprite(118, 34, 17, 1, new Color(0.25f, 0.35f, 0.48f, 0.75f), new Color(0.12f, 0.16f, 0.22f, 0.85f));
                    }
                    Text txt = btnChild.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        txt.color = isCur ? Color.white : new Color(0.75f, 0.80f, 0.85f);
                    }
                }
            }
        }

        private void CreateFarmAppView(Transform parent)
        {
            GameObject viewObj = new GameObject("FarmAppView");
            viewObj.transform.SetParent(parent, false);

            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;

            farmAppView = viewObj.transform;

            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(viewObj.transform, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 205f);
            hRect.sizeDelta = new Vector2(850f, 40f);

            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(headerObj.transform, false);

            RectTransform bRect = backBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(-360f, 0f);
            bRect.sizeDelta = new Vector2(130f, 36f);

            Image bBg = backBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(130, 36, 18, new Color(0.20f, 0.25f, 0.32f, 0.90f));
            bBg.raycastTarget = true;

            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;
            bBtn.onClick.AddListener(ShowHomeScreen);

            GameObject bTextObj = new GameObject("Text");
            bTextObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform btRect = bTextObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;

            Text bText = bTextObj.AddComponent<Text>();
            bText.font = globalFont;
            bText.text = LocalizationManager.L("Btn_HomeScreen", "← Ana Ekran", "← Home Screen");
            bText.fontSize = 18;
            bText.fontStyle = FontStyle.Bold;
            bText.alignment = TextAnchor.MiddleCenter;
            bText.color = new Color(0.35f, 0.85f, 1.0f);
            bText.raycastTarget = false;

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 0f);
            tRect.sizeDelta = new Vector2(400f, 40f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = LocalizationManager.L("App_FarmHeader", "🌾 ÇİFTLİK YÖNETİMİ & TARIM", "🌾 FARM MANAGEMENT & AGRICULTURE");
            tText.fontSize = 24;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.25f, 0.85f, 0.40f);
            tText.raycastTarget = false;

            CreateFarmTabs(viewObj.transform);

            farmOverviewContent = CreateScrollableViewContainer(viewObj.transform, "FarmOverview", new Vector2(0f, -50f), new Vector2(850f, 350f), out farmOverviewViewportObj);
            farmCandidateContent = CreateScrollableViewContainer(viewObj.transform, "FarmCandidates", new Vector2(0f, -50f), new Vector2(850f, 350f), out farmCandidateViewportObj);
            farmStaffContent = CreateScrollableViewContainer(viewObj.transform, "FarmStaff", new Vector2(0f, -50f), new Vector2(850f, 350f), out farmStaffViewportObj);
            farmShiftContent = CreateScrollableViewContainer(viewObj.transform, "FarmShifts", new Vector2(0f, -50f), new Vector2(850f, 350f), out farmShiftViewportObj);

            VerticalLayoutGroup oLayout = farmOverviewContent.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.spacing = 15f;
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = false;

            GridLayoutGroup cGrid = farmCandidateContent.gameObject.AddComponent<GridLayoutGroup>();
            cGrid.cellSize = new Vector2(265f, 160f);
            cGrid.spacing = new Vector2(15f, 15f);
            cGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cGrid.constraintCount = 3;

            VerticalLayoutGroup sLayout = farmStaffContent.gameObject.AddComponent<VerticalLayoutGroup>();
            sLayout.spacing = 10f;
            sLayout.childControlWidth = true;
            sLayout.childControlHeight = false;

            VerticalLayoutGroup shiftLayout = farmShiftContent.gameObject.AddComponent<VerticalLayoutGroup>();
            shiftLayout.spacing = 12f;
            shiftLayout.childControlWidth = true;
            shiftLayout.childControlHeight = false;

            viewObj.SetActive(false);
        }

        private void CreateFarmTabs(Transform parent)
        {
            GameObject tabsObj = new GameObject("FarmTabs");
            tabsObj.transform.SetParent(parent, false);

            RectTransform tRect = tabsObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 155f);
            tRect.sizeDelta = new Vector2(850f, 44f);

            HorizontalLayoutGroup layout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;

            string[] tabs = new string[] {
                LocalizationManager.L("Tab_Overview", "📊 1. Genel Durum", "📊 1. Overview"),
                LocalizationManager.L("Tab_FarmStaff", "👥 2. Personel Kadrosu", "👥 2. Staff List"),
                LocalizationManager.L("Tab_FarmHire", "➕ 3. İşe Alım", "➕ 3. Hire Staff"),
                LocalizationManager.L("Tab_FarmShifts", "⏰ 4. Vardiyalar", "⏰ 4. Shifts")
            };

            for (int i = 0; i < 4; i++)
            {
                int tabIndex = i;
                GameObject tabBtn = new GameObject("FarmTab_" + i);
                tabBtn.transform.SetParent(tabsObj.transform, false);

                RectTransform tabRect = tabBtn.AddComponent<RectTransform>();
                tabRect.sizeDelta = new Vector2(198f, 40f);

                Image tabBg = tabBtn.AddComponent<Image>();
                tabBg.raycastTarget = true;
                farmTabBtnImgs[i] = tabBg;

                Button btn = tabBtn.AddComponent<Button>();
                btn.targetGraphic = tabBg;
                btn.onClick.AddListener(() => {
                    activeFarmTab = tabIndex;
                    RefreshFarmViews();
                });

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(tabBtn.transform, false);
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;

                Text tabText = textObj.AddComponent<Text>();
                tabText.font = globalFont;
                tabText.text = tabs[i];
                tabText.fontSize = 18;
                tabText.fontStyle = FontStyle.Bold;
                tabText.alignment = TextAnchor.MiddleCenter;
                tabText.raycastTarget = false;
                farmTabBtnTexts[i] = tabText;
            }

            UpdateFarmTabVisuals();
        }

        private void UpdateFarmTabVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                if (farmTabBtnImgs[i] == null) continue;
                bool isActive = (activeFarmTab == i);
                if (isActive)
                {
                    farmTabBtnImgs[i].sprite = UIStyleUtility.CreateOutlinePillSprite(198, 40, 20, 2, new Color(0.25f, 0.85f, 0.40f), new Color(0.08f, 0.25f, 0.12f, 0.95f));
                    if (farmTabBtnTexts[i] != null) farmTabBtnTexts[i].color = new Color(0.40f, 0.95f, 0.55f);
                }
                else
                {
                    farmTabBtnImgs[i].sprite = UIStyleUtility.CreateOutlinePillSprite(198, 40, 20, 1, new Color(0.20f, 0.35f, 0.50f, 0.65f), new Color(0.10f, 0.14f, 0.18f, 0.85f));
                    if (farmTabBtnTexts[i] != null) farmTabBtnTexts[i].color = new Color(0.55f, 0.70f, 0.85f);
                }
            }
        }

        private Text CreateTextInPanel(Transform parent, Vector2 anchoredPos, Vector2 size, string text, int fontSize, Color color)
        {
            GameObject txtObj = new GameObject("Text_Panel");
            txtObj.transform.SetParent(parent, false);

            RectTransform rect = txtObj.AddComponent<RectTransform>();
            if (anchoredPos == Vector2.zero && size == Vector2.one)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }
            else
            {
                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = size;
            }

            Text txt = txtObj.AddComponent<Text>();
            txt.font = globalFont;
            txt.text = text;
            txt.fontSize = fontSize;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.fontStyle = FontStyle.Bold;
            txt.color = color;
            txt.raycastTarget = false;
            return txt;
        }

        private GameObject CreateButtonInPanel(Transform parent, Vector2 pos, Vector2 size, string text, Color bgColor, UnityEngine.Events.UnityAction onClick, int fontSize = 16)
        {
            GameObject btnObj = new GameObject("Btn_" + text);
            btnObj.transform.SetParent(parent, false);

            RectTransform r = btnObj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = UIStyleUtility.CreateRoundedPillSprite((int)size.x, (int)size.y, 8, bgColor);
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            Text txt = CreateTextInPanel(btnObj.transform, Vector2.zero, Vector2.one, text, fontSize, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;

            return btnObj;
        }

        private void RenderFarmOverview()
        {
            if (farmOverviewContent == null) return;
            foreach (Transform child in farmOverviewContent) Destroy(child.gameObject);

            // 1. ÇİFTLİK GENEL DURUM ÖZET KARTI
            GameObject cardObj = new GameObject("Farm_Status_Card");
            cardObj.transform.SetParent(farmOverviewContent, false);

            LayoutElement cardLayout = cardObj.AddComponent<LayoutElement>();
            cardLayout.minHeight = 150f;
            cardLayout.preferredHeight = 150f;

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(830, 150, 16, 2, new Color(0.25f, 0.85f, 0.40f), new Color(0.12f, 0.16f, 0.20f, 0.90f));

            string farmReportText = LocalizationManager.L(
                "Farm_ReportText",
                "🌾 ÇİFTLİK GENEL SAĞLIK VE ÜRETİM RAPORU\n" +
                "• Ekilebilir Tarla Parselleri: 36/36 Aktif Verimli Parsel (Toprak Sağlığı: %98)\n" +
                "• Çiftlik Tesisleri: Çiftlik Evi (Aktif), Ahır & Tahıl Silosu (1,500 KG Kapasite), Çiftlik Gölü\n" +
                "• Günlük Tahmini Rekolte: 450 KG Buğday / Domates / Mısır / Ayçiçeği\n" +
                "• Günlük Tahmini Tarımsal Gelir: 12.500C / Gün | Sulama Verimliliği: %100",
                "🌾 FARM GENERAL HEALTH & PRODUCTION REPORT\n" +
                "• Plantable Field Plots: 36/36 Active Fertile Plots (Soil Health: 98%)\n" +
                "• Farm Facilities: Farmhouse (Active), Barn & Grain Silo (1,500 KG Capacity), Farm Pond\n" +
                "• Est. Daily Yield: 450 KG Wheat / Tomatoes / Corn / Sunflowers\n" +
                "• Est. Daily Ag. Income: 12,500C / Day | Watering Efficiency: 100%"
            );
            Text cardText = CreateTextInPanel(cardObj.transform, Vector2.zero, Vector2.one, farmReportText, 17, Color.white);
            cardText.alignment = TextAnchor.MiddleLeft;

            // 2. 3 AŞAMALI AHIR KAPASİTE GELİŞTİRME KARTLARI
            string[] upgradeTitles = new string[] {
                LocalizationManager.L("Barn_Lvl1_Title", "🏡 Ahır Seviye 1 (Temel Depo)", "🏡 Barn Level 1 (Basic Storage)"),
                LocalizationManager.L("Barn_Lvl2_Title", "🛖 Ahır Seviye 2 (Genişletilmiş Depo)", "🛖 Barn Level 2 (Expanded Storage)"),
                LocalizationManager.L("Barn_Lvl3_Title", "🏗️ Ahır Seviye 3 (Dev Çiftlik Silosu & Ahır)", "🏗️ Barn Level 3 (Giant Farm Silo & Barn)")
            };

            string[] upgradeDescs = new string[] {
                LocalizationManager.L("Barn_Lvl1_Desc", "Başlangıç Ahırı. Maksimum 1.000 KG mahsul depolama kapasitesi sunar.", "Initial Barn. Offers maximum 1,000 KG crop storage capacity."),
                LocalizationManager.L("Barn_Lvl2_Desc", "Ahır depolama alanını genişleterek maksimum kapasiteyi 2.500 KG seviyesine çıkarır.", "Expands barn storage area to maximum 2,500 KG capacity."),
                LocalizationManager.L("Barn_Lvl3_Desc", "Devasa çiftlik silosu ve ahır. Maksimum mahsul depolama kapasitesini 5.000 KG seviyesine çıkarır.", "Giant farm silo and barn. Increases maximum crop storage capacity to 5,000 KG.")
            };

            float[] upgradeCosts = new float[] { 0f, 15000f, 35000f };

            int currentBarnLvl = GardenSeedInventoryManager.Instance.BarnUpgradeLevel;

            for (int i = 0; i < 3; i++)
            {
                int tierLvl = i + 1;
                bool isUnlocked = (currentBarnLvl >= tierLvl);
                bool isNextToBuy = (currentBarnLvl + 1 == tierLvl);

                GameObject upgCard = new GameObject("Farm_Upgrade_Card_" + i);
                upgCard.transform.SetParent(farmOverviewContent, false);

                LayoutElement uElem = upgCard.AddComponent<LayoutElement>();
                uElem.minHeight = 85f;
                uElem.preferredHeight = 85f;

                Image uBg = upgCard.AddComponent<Image>();
                Color outlineColor = isUnlocked ? new Color(0.20f, 0.75f, 0.35f) : (isNextToBuy ? new Color(0.95f, 0.65f, 0.15f) : Color.gray);
                uBg.sprite = UIStyleUtility.CreateOutlinePillSprite(830, 85, 14, 2, outlineColor, new Color(0.14f, 0.18f, 0.22f, 0.90f));

                Text tTitle = CreateTextInPanel(upgCard.transform, new Vector2(-120f, 18f), new Vector2(520f, 30f), upgradeTitles[i], 19, isUnlocked ? new Color(0.40f, 0.95f, 0.60f) : Color.white);
                tTitle.alignment = TextAnchor.MiddleLeft;

                Text tDesc = CreateTextInPanel(upgCard.transform, new Vector2(-120f, -15f), new Vector2(520f, 35f), upgradeDescs[i], 16, new Color(0.80f, 0.85f, 0.90f));
                tDesc.alignment = TextAnchor.MiddleLeft;

                // Satın Al Butonu
                GameObject buyBtnObj = new GameObject("BuyBtn");
                buyBtnObj.transform.SetParent(upgCard.transform, false);

                RectTransform bRect = buyBtnObj.AddComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(320f, 0f);
                bRect.sizeDelta = new Vector2(150f, 42f);

                Image bBg = buyBtnObj.AddComponent<Image>();
                Color bCol = isUnlocked ? new Color(0.15f, 0.55f, 0.25f) : (isNextToBuy ? new Color(0.90f, 0.55f, 0.15f) : new Color(0.30f, 0.35f, 0.40f));
                bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(150, 42, 21, bCol);
                bBg.raycastTarget = true;

                if (isNextToBuy)
                {
                    Button bBtn = buyBtnObj.AddComponent<Button>();
                    bBtn.targetGraphic = bBg;
                    int buyIdx = i;
                    bBtn.onClick.AddListener(() => {
                        if (FinanceManager.Instance != null && FinanceManager.Instance.SpendMoney(upgradeCosts[buyIdx], upgradeTitles[buyIdx]))
                        {
                            GardenSeedInventoryManager.Instance.UpgradeBarn();
                            string modalTitle = LocalizationManager.L("Modal_BarnUpgraded_Title", "Ahır Geliştirildi! 🏗️", "Barn Upgraded! 🏗️");
                            string modalBody = LocalizationManager.L("Modal_BarnUpgraded_Body", $"{upgradeTitles[buyIdx]} aktif edildi!\n\nYeni Ahır Kapasitesi: {GardenSeedInventoryManager.Instance.MaxBarnCapacity} KG", $"{upgradeTitles[buyIdx]} activated!\n\nNew Barn Capacity: {GardenSeedInventoryManager.Instance.MaxBarnCapacity} KG");
                            string btnOk = LocalizationManager.L("Btn_Great", "Harika", "Great");
                            ModalManager.ShowModal(modalTitle, modalBody, btnOk);
                            RenderFarmOverview();
                        }
                    });
                }

                string activeText = LocalizationManager.L("Btn_ActiveOwned", "AKTİF / SAHİPSİN", "ACTIVE / OWNED");
                string upgradeFmt = LocalizationManager.L("Btn_UpgradeCostFmt", "GELİŞTİR\n{0:N0}C", "UPGRADE\n{0:N0}C");
                string lockedText = LocalizationManager.L("Btn_Locked", "🔒 KİLİTLİ", "🔒 LOCKED");
                string btnTextStr = isUnlocked ? activeText : (isNextToBuy ? string.Format(upgradeFmt, upgradeCosts[i]) : lockedText);
                Text bText = CreateTextInPanel(buyBtnObj.transform, Vector2.zero, Vector2.one, btnTextStr, 16, Color.white);
                bText.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void RenderFarmCandidateList()
        {
            if (farmCandidateContent == null) return;
            foreach (Transform child in farmCandidateContent) Destroy(child.gameObject);

            Color farmColor = new Color(0.25f, 0.85f, 0.40f);

            GameObject cardObj = new GameObject("FarmRoleCard_Worker");
            cardObj.transform.SetParent(farmCandidateContent, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(265, 160, 16, 2, farmColor, new Color(0.12f, 0.16f, 0.22f, 0.90f));
            cardBg.raycastTarget = false;

            GameObject infoObj = new GameObject("InfoText");
            infoObj.transform.SetParent(cardObj.transform, false);

            RectTransform iRect = infoObj.AddComponent<RectTransform>();
            iRect.anchoredPosition = new Vector2(0f, 25f);
            iRect.sizeDelta = new Vector2(245f, 90f);

            Text iText = infoObj.AddComponent<Text>();
            iText.font = globalFont;
            iText.text = LocalizationManager.L("FarmWorker_CardInfo", "🌾 Çiftlik İşçisi\n<size=14>Sulama, çapa & ürün hasadı\n(Erkek / Kadın Rastgele Aday)</size>\n💰 Maaş: 250 Cr/Gün (Gece 12'de)", "🌾 Farm Worker\n<size=14>Watering, hoeing & crop harvesting\n(Random Male / Female Candidate)</size>\n💰 Salary: 250 Cr/Day (At Midnight)");
            iText.fontSize = 18;
            iText.fontStyle = FontStyle.Bold;
            iText.alignment = TextAnchor.MiddleCenter;
            iText.color = new Color(0.40f, 0.95f, 0.55f);
            iText.raycastTarget = false;

            GameObject hireBtnObj = new GameObject("HireButton_Farm");
            hireBtnObj.transform.SetParent(cardObj.transform, false);

            RectTransform hRect = hireBtnObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, -48f);
            hRect.sizeDelta = new Vector2(220f, 36f);

            Image hBg = hireBtnObj.AddComponent<Image>();
            hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(220, 36, 18, farmColor * 0.85f);
            hBg.raycastTarget = true;

            Button hBtn = hireBtnObj.AddComponent<Button>();
            hBtn.targetGraphic = hBg;
            hBtn.onClick.AddListener(() => {
                if (StaffManager.Instance != null)
                {
                    StaffMember hired = StaffManager.Instance.HireFarmWorker();
                    if (hired != null)
                    {
                        if (TutorialManager.Instance != null)
                        {
                            TutorialManager.Instance.NotifyStaffHired(StaffRole.Çiftçi);
                        }

                        string title = LocalizationManager.L("Modal_HireSuccess_Title", "İşe Alım Başarılı! 🎉", "Recruitment Successful! 🎉");
                        string roleName = LocalizationManager.L("Role_FarmerName", "Çiftlik İşçisi (Çiftçi)", "Farm Worker (Farmer)");
                        string bodyFmt = LocalizationManager.L(
                            "Modal_FarmHireSuccess_Body",
                            "<b>{0}</b> başarıyla <b>{1}</b> pozisyonunda işe alındı!\n\nGünlük Maaş: <b>{2} Credit</b> (Gece 00:00'da kesilir).\n\nVardiya ayarlarını 'Vardiyalar' sekmesinden düzenleyebilirsiniz.",
                            "<b>{0}</b> was successfully hired for the position of <b>{1}</b>!\n\nDaily Salary: <b>{2} Credit</b> (Deducted at midnight 00:00).\n\nYou can manage shift schedules from the 'Shifts' tab."
                        );
                        string okBtn = LocalizationManager.L("Btn_OK", "Tamam", "OK");

                        ModalManager.ShowModal(
                            title,
                            string.Format(bodyFmt, hired.name, roleName, hired.dailySalary),
                            okBtn
                        );
                    }
                }
                RenderFarmCandidateList();
            });

            GameObject htObj = new GameObject("Text");
            htObj.transform.SetParent(hireBtnObj.transform, false);
            RectTransform htRect = htObj.AddComponent<RectTransform>();
            htRect.anchorMin = Vector2.zero;
            htRect.anchorMax = Vector2.one;

            Text htText = htObj.AddComponent<Text>();
            htText.font = globalFont;
            htText.text = LocalizationManager.L("Btn_HireStaff", "➕ İŞE AL", "➕ HIRE STAFF");
            htText.fontSize = 18;
            htText.fontStyle = FontStyle.Bold;
            htText.alignment = TextAnchor.MiddleCenter;
            htText.color = Color.white;
            htText.raycastTarget = false;
        }

        private void RenderFarmStaffList()
        {
            if (farmStaffContent == null) return;
            foreach (Transform child in farmStaffContent) Destroy(child.gameObject);

            List<StaffMember> farmList = (StaffManager.Instance != null) ? StaffManager.Instance.GetFarmStaffList() : new List<StaffMember>();

            if (farmList == null || farmList.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyInfo");
                emptyObj.transform.SetParent(farmStaffContent, false);
                LayoutElement eElem = emptyObj.AddComponent<LayoutElement>();
                eElem.minHeight = 100f;

                Text eText = CreateTextInPanel(emptyObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Msg_NoFarmStaff", "👨‍🌾 Çiftlikte çalışan henüz işçi bulunmuyor.\n'2. İşçi İşe Al' sekmesinden yeni çiftçi ekleyebilirsiniz.", "👨‍🌾 No farm workers currently hired.\nYou can hire new farmers from the '2. Hire Staff' tab."), 17, Color.gray);
                eText.alignment = TextAnchor.MiddleCenter;
                return;
            }

            foreach (var staff in farmList)
            {
                GameObject sCard = new GameObject("Farm_Staff_" + staff.id);
                sCard.transform.SetParent(farmStaffContent, false);

                LayoutElement sElem = sCard.AddComponent<LayoutElement>();
                sElem.minHeight = 48f;
                sElem.preferredHeight = 48f;

                Image bg = sCard.AddComponent<Image>();
                bg.sprite = UIStyleUtility.CreateRoundedPillSprite(830, 48, 12, new Color(0.14f, 0.18f, 0.24f, 0.90f));
                bg.raycastTarget = false;

                string rowFmt = LocalizationManager.L("FarmStaff_RowFmt", "👨‍🌾 {0}   |   ⏰ Vardiya: {1}   |   💰 Maaş: {2}C/Gün (Gece 12)", "👨‍🌾 {0}   |   ⏰ Shift: {1}   |   💰 Salary: {2}C/Day (Midnight)");
                Text nText = CreateTextInPanel(sCard.transform, new Vector2(-60f, 0f), new Vector2(520f, 45f), string.Format(rowFmt, staff.name, GetLocalizedShiftHours(staff.shiftHours), staff.dailySalary), 17, Color.white);
                nText.alignment = TextAnchor.MiddleLeft;

                // ❌ İŞTEN ÇIKAR (KOV) BUTONU
                GameObject fireBtnObj = new GameObject("FireBtn");
                fireBtnObj.transform.SetParent(sCard.transform, false);
                RectTransform fRect = fireBtnObj.AddComponent<RectTransform>();
                fRect.anchoredPosition = new Vector2(340f, 0f);
                fRect.sizeDelta = new Vector2(115f, 34f);

                Image fBg = fireBtnObj.AddComponent<Image>();
                fBg.sprite = UIStyleUtility.CreateRoundedPillSprite(115, 34, 16, new Color(0.90f, 0.25f, 0.25f));
                fBg.raycastTarget = true;

                Button fBtn = fireBtnObj.AddComponent<Button>();
                fBtn.targetGraphic = fBg;
                var currentStaffId = staff.id;
                fBtn.onClick.AddListener(() => {
                    StaffManager.Instance.FireFarmWorker(currentStaffId);
                    RenderFarmStaffList();
                });

                GameObject ftObj = new GameObject("Text");
                ftObj.transform.SetParent(fireBtnObj.transform, false);
                RectTransform ftRect = ftObj.AddComponent<RectTransform>();
                ftRect.anchorMin = Vector2.zero;
                ftRect.anchorMax = Vector2.one;

                Text ftText = ftObj.AddComponent<Text>();
                ftText.font = globalFont;
                ftText.text = LocalizationManager.L("Btn_FireStaff", "❌ İŞTEN ÇIKAR", "❌ FIRE STAFF");
                ftText.fontSize = 16;
                ftText.fontStyle = FontStyle.Bold;
                ftText.alignment = TextAnchor.MiddleCenter;
                ftText.color = Color.white;
                ftText.raycastTarget = false;
            }
        }

        private void RenderFarmShiftList()
        {
            if (farmShiftContent == null) return;
            foreach (Transform child in farmShiftContent) Destroy(child.gameObject);

            List<StaffMember> farmList = (StaffManager.Instance != null) ? StaffManager.Instance.GetFarmStaffList() : new List<StaffMember>();

            if (farmList == null || farmList.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyShiftMsg");
                emptyObj.transform.SetParent(farmShiftContent, false);

                LayoutElement eElem = emptyObj.AddComponent<LayoutElement>();
                eElem.minHeight = 100f;

                Text eText = CreateTextInPanel(emptyObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Msg_NoFarmShiftStaff", "🌾 Vardiyası ayarlanacak çiftlik işçisi bulunmuyor.\nLütfen önce '2. İşçi İşe Al' sekmesinden çiftçi ekleyin.", "🌾 No active farm workers to manage shifts.\nPlease hire farmers from the '2. Hire Staff' tab first."), 17, Color.gray);
                eText.alignment = TextAnchor.MiddleCenter;
                return;
            }

            string[] shiftNames = new string[] {
                LocalizationManager.L("Shift_Morning", "Sabah", "Morning"),
                LocalizationManager.L("Shift_Evening", "Akşam", "Evening")
            };
            string[] shiftFullNames = new string[] {
                LocalizationManager.L("ShiftFull_Morning", "☀️ Sabah (08:00 - 16:00)", "☀️ Morning (08:00 - 16:00)"),
                LocalizationManager.L("ShiftFull_Evening", "🌆 Akşam (16:00 - 24:00)", "🌆 Evening (16:00 - 24:00)")
            };

            foreach (var staff in farmList)
            {
                GameObject sCard = new GameObject("Farm_Shift_Card_" + staff.id);
                sCard.transform.SetParent(farmShiftContent, false);

                RectTransform sRect = sCard.AddComponent<RectTransform>();
                sRect.sizeDelta = new Vector2(820f, 52f);

                LayoutElement sElem = sCard.AddComponent<LayoutElement>();
                sElem.minHeight = 52f;
                sElem.preferredHeight = 52f;

                Image bg = sCard.AddComponent<Image>();
                bg.sprite = UIStyleUtility.CreateRoundedPillSprite(820, 52, 12, new Color(0.14f, 0.18f, 0.24f, 0.90f));
                bg.raycastTarget = false;

                Text nText = CreateTextInPanel(sCard.transform, new Vector2(-265f, 0f), new Vector2(270f, 45f), $"👤 {staff.name}\n⏰ {GetLocalizedShiftHours(staff.shiftHours)}", 15, Color.white);
                nText.alignment = TextAnchor.MiddleLeft;
                nText.horizontalOverflow = HorizontalWrapMode.Overflow;
                nText.verticalOverflow = VerticalWrapMode.Truncate;

                CreateEarlyCallButton(sCard.transform, staff);

                GameObject optsObj = new GameObject("ShiftOptions");
                optsObj.transform.SetParent(sCard.transform, false);

                RectTransform oRect = optsObj.AddComponent<RectTransform>();
                oRect.anchoredPosition = new Vector2(200f, 0f);
                oRect.sizeDelta = new Vector2(380f, 40f);

                HorizontalLayoutGroup hLayout = optsObj.AddComponent<HorizontalLayoutGroup>();
                hLayout.spacing = 8f;
                hLayout.childAlignment = TextAnchor.MiddleRight;

                string currentFarmShiftStr = staff.shiftHours ?? "";

                bool isFarmEvening = currentFarmShiftStr.Contains("Akşam") || currentFarmShiftStr.Contains("Evening") || currentFarmShiftStr.Contains("Gece") || currentFarmShiftStr.Contains("Night") || currentFarmShiftStr.Contains("16:00 - 24:00") || currentFarmShiftStr.Contains("24:00");
                bool isFarmMorning = !isFarmEvening;

                for (int i = 0; i < 2; i++)
                {
                    int shiftIdx = i;
                    string targetShift = shiftFullNames[shiftIdx];
                    bool isCurrentShift = (shiftIdx == 0) ? isFarmMorning : isFarmEvening;

                    GameObject btnObj = new GameObject("ShiftBtn_" + shiftIdx);
                    btnObj.transform.SetParent(optsObj.transform, false);

                    RectTransform bRect = btnObj.AddComponent<RectTransform>();
                    bRect.sizeDelta = new Vector2(118f, 34f);

                    Image bBg = btnObj.AddComponent<Image>();
                    if (isCurrentShift)
                    {
                        bBg.sprite = UIStyleUtility.CreateOutlinePillSprite(118, 34, 17, 2, new Color(0.20f, 0.85f, 0.40f), new Color(0.12f, 0.42f, 0.22f, 0.95f));
                    }
                    else
                    {
                        bBg.sprite = UIStyleUtility.CreateOutlinePillSprite(118, 34, 17, 1, new Color(0.30f, 0.40f, 0.52f, 0.70f), new Color(0.14f, 0.18f, 0.24f, 0.85f));
                    }
                    bBg.raycastTarget = true;

                    Button btn = btnObj.AddComponent<Button>();
                    btn.targetGraphic = bBg;
                    var currentStaffId = staff.id;
                    btn.onClick.AddListener(() => {
                        if (StaffManager.Instance != null)
                        {
                            StaffManager.Instance.UpdateFarmStaffShift(currentStaffId, targetShift);
                            if (TutorialManager.Instance != null)
                            {
                                TutorialManager.Instance.NotifyStaffShiftChanged();
                            }
                            RenderFarmShiftList();
                        }
                    });

                    Text btnText = CreateTextInPanel(btnObj.transform, Vector2.zero, Vector2.one, shiftNames[shiftIdx], 15, isCurrentShift ? Color.white : new Color(0.70f, 0.78f, 0.88f));
                    btnText.fontStyle = FontStyle.Bold;
                    btnText.alignment = TextAnchor.MiddleCenter;
                    btnText.raycastTarget = false;
                }
            }
        }

        // ==================== TOPLU SİPARİŞ (BULK ORDER) SİSTEMİ ====================

        private int GetProductStockInStore(string productName)
        {
            int totalStock = 0;
            var shelves = PlacedFurnitureController.AllPlacedFurniture;
            if (shelves != null)
            {
                int sCount = shelves.Count;
                for (int i = 0; i < sCount; i++)
                {
                    var shelf = shelves[i];
                    if (shelf == null || shelf.rows == null) continue;
                    int rCount = shelf.rows.Length;
                    for (int j = 0; j < rCount; j++)
                    {
                        var row = shelf.rows[j];
                        if (row != null && row.productName == productName)
                        {
                            totalStock += row.currentStock;
                        }
                    }
                }
            }
            return totalStock;
        }

        private void OnBulkOrderButtonClicked()
        {
            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");

            // 1. Toptancı / Çiftlik Kamyonu Yolda mı Kontrol Et
            bool isAnyTruckActiveBulk = (WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckOnTheWay) ||
                                        (GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckOnTheWay);

            if (isAnyTruckActiveBulk)
            {
                string truckActiveTitle = LocalizationManager.L("Modal_TruckActive_Title", "Teslimat Noktası Dolu! ⚠️", "Delivery Point Occupied! ⚠️");
                string truckActiveBody = LocalizationManager.L("Modal_TruckActive_Body", "Şu anda yolda veya teslimat noktasında aktif bir kamyon (Toptancı veya Çiftlik Kamyonu) bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni toplu sipariş verilemez.", "There is an active delivery truck currently en route or at the delivery point!\n\nNew bulk orders cannot be placed until the truck completes delivery and departs.");
                ModalManager.ShowModal(truckActiveTitle, truckActiveBody, btnOk);
                return;
            }

            // 2. Mevcut Seviyeye Uygun Toptan Ürünleri Getir
            int currentLevel = (Farm2Shelf.Environment.EnvironmentBuilder.Instance != null) 
                ? Farm2Shelf.Environment.EnvironmentBuilder.Instance.CurrentUpgradeLevel 
                : 1;

            List<WholesaleProductDef> wholesaleOnly = WholesaleDatabase.GetWholesaleOnlyProducts();
            List<WholesaleProductDef> unlockedProducts = wholesaleOnly.FindAll(p => p.requiredLevel <= currentLevel && p.isOrderable);

            if (unlockedProducts.Count == 0)
            {
                string noProdTitle = LocalizationManager.L("Modal_NoProduct_Title", "Ürün Bulunamadı ⚠️", "No Products Found ⚠️");
                string noProdBody = LocalizationManager.L("Modal_NoProduct_Body", "Seviyenize uygun toptan ürün bulunamadı.", "No wholesale products available for your current level.");
                ModalManager.ShowModal(noProdTitle, noProdBody, btnOk);
                return;
            }

            // 3. %20 İndirimli Fiyat Hesaplama (%20 daha karlı toplu sipariş)
            int GetDiscountedPackCost(WholesaleProductDef p) => Mathf.RoundToInt(p.TotalPackCost * 0.80f);

            int totalAllCost = 0;
            int totalAllStandardCost = 0;
            foreach (var p in unlockedProducts)
            {
                totalAllCost += GetDiscountedPackCost(p);
                totalAllStandardCost += p.TotalPackCost;
            }

            // 4. Oyuncunun Parası ve Bütçe Kontrolü
            int currentBalance = (EconomyManager.Instance != null) 
                ? EconomyManager.Instance.Credits 
                : ((FinanceManager.Instance != null) ? FinanceManager.Instance.CurrentBalance : 0);

            List<WholesaleProductDef> orderList = new List<WholesaleProductDef>();
            int totalCost = 0;
            int totalStandardCost = 0;
            bool isLimitedByBudget = false;

            if (currentBalance >= totalAllCost)
            {
                // Paramız tüm ürünlere yetiyor -> 1'er adet koli halinde Hepsini Siparişe Ekle!
                foreach (var p in unlockedProducts)
                {
                    orderList.Add(p);
                    totalCost += GetDiscountedPackCost(p);
                    totalStandardCost += p.TotalPackCost;
                }
                isLimitedByBudget = false;
            }
            else
            {
                // Paramız tüm ürünlere YETMİYORSA -> Stoğu en az olan ürünlerden başla!
                isLimitedByBudget = true;
                
                // Ürünleri mağazadaki güncel stok miktarına göre küçükten büyüğe sırala
                unlockedProducts.Sort((a, b) => {
                    int stockA = GetProductStockInStore(a.name);
                    int stockB = GetProductStockInStore(b.name);
                    if (stockA != stockB) return stockA.CompareTo(stockB);
                    return a.TotalPackCost.CompareTo(b.TotalPackCost); // Stoklar eşitse ucuz olan öne
                });

                int remainingBalance = currentBalance;
                foreach (var p in unlockedProducts)
                {
                    int packCost = GetDiscountedPackCost(p);
                    if (remainingBalance >= packCost)
                    {
                        orderList.Add(p);
                        remainingBalance -= packCost;
                        totalCost += packCost;
                        totalStandardCost += p.TotalPackCost;
                    }
                }
            }

            // 5. Yetersiz Bakiye Kontrolü (Hiç 1 koli bile alamıyorsa)
            if (orderList.Count == 0)
            {
                string nsTitle = LocalizationManager.L("Modal_LowBalance_Title", "Yetersiz Bakiye ⚠️", "Insufficient Balance ⚠️");
                string nsBody = string.Format(LocalizationManager.L("Modal_LowBalance_Body", "Toplu sipariş verebilmek için en az 1 koli ürün almaya yetecek paranız olmalıdır!\n\nMevcut Bakiyeniz: {0:N0}C.", "You must have enough credits to buy at least 1 pack to place a bulk order!\n\nCurrent Balance: {0:N0}C."), currentBalance);
                ModalManager.ShowModal(nsTitle, nsBody, btnOk);
                return;
            }

            int savings = totalStandardCost - totalCost;

            // 6. Ekrana Uyarı Çıkar (Evet / Hayır Pop-up)
            string modalTitle = LocalizationManager.L("Modal_BulkConfirm_Title", "📦 Toplu Sipariş Onayı", "📦 Bulk Order Confirmation");
            string modalMessage = isLimitedByBudget
                ? string.Format(LocalizationManager.L("Modal_BulkConfirm_BudgetFmt", "Bakiyeniz tüm ürünlere yetmediği için **en az stoğu kalan {0} çeşit** üründen 1'er koli sipariş seçildi.\n\n💰 **Normal Tutar:** {1:N0}C\n🏷️ **%20 Toplu İndirimli:** {2:N0}C\n🎉 **Net Kâr / Tasarruf:** {3:N0}C (%20 Avantaj!)\n\nToptancı kamyonunun **{0} koli** ürünü doğrudan depoya indirmesini onaylıyor musunuz?", "Due to your balance, **{0} lowest-stock items** were selected for 1-pack orders.\n\n💰 **Regular Price:** {1:N0}C\n🏷️ **20% Bulk Discounted:** {2:N0}C\n🎉 **Net Savings:** {3:N0}C (20% Advantage!)\n\nDo you confirm dispatching the delivery truck with **{0} packs** of products directly to your warehouse?"), orderList.Count, totalStandardCost, totalCost, savings)
                : string.Format(LocalizationManager.L("Modal_BulkConfirm_FullFmt", "Seviyenize uygun **{0} çeşit** ürünün tamamından 1'er koli sipariş verilecek.\n\n💰 **Normal Tutar:** {1:N0}C\n🏷️ **%20 Toplu İndirimli:** {2:N0}C\n🎉 **Net Kâr / Tasarruf:** {3:N0}C (%20 Avantaj!)\n\nToptancı kamyonunun **{0} koli** ürünü doğrudan depoya indirmesini onaylıyor musunuz?", "1 pack of each of the **{0} available product types** for your level will be ordered.\n\n💰 **Regular Price:** {1:N0}C\n🏷️ **20% Bulk Discounted:** {2:N0}C\n🎉 **Net Savings:** {3:N0}C (20% Advantage!)\n\nDo you confirm dispatching the delivery truck with **{0} packs** of products directly to your warehouse?"), orderList.Count, totalStandardCost, totalCost, savings);

            ModalManager.ShowConfirmModal(
                modalTitle,
                modalMessage,
                onConfirm: () => ConfirmBulkOrder(orderList, totalCost, savings),
                confirmText: LocalizationManager.L("Btn_ConfirmBulkOrder", "Evet, Sipariş Ver", "Yes, Place Order"),
                cancelText: LocalizationManager.L("Btn_Cancel", "Vazgeç", "Cancel")
            );
        }

        private void ConfirmBulkOrder(List<WholesaleProductDef> orderList, int totalCost, int savings)
        {
            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");

            // 1. Kamyon Yolda mı Tekrar Kontrol Et (Evet'e tıklandığı an)
            bool isAnyTruckActiveConfirm = (WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckOnTheWay) ||
                                           (GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckOnTheWay);

            if (isAnyTruckActiveConfirm)
            {
                string truckActiveTitle = LocalizationManager.L("Modal_TruckActive_Title", "Teslimat Noktası Dolu! ⚠️", "Delivery Point Occupied! ⚠️");
                string truckActiveBody = LocalizationManager.L("Modal_TruckActive_Body", "Şu anda yolda veya teslimat noktasında aktif bir kamyon (Toptancı veya Çiftlik Kamyonu) bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni toplu sipariş verilemez.", "There is an active delivery truck currently en route or at the delivery point!\n\nNew bulk orders cannot be placed until the truck completes delivery and departs.");
                ModalManager.ShowModal(truckActiveTitle, truckActiveBody, btnOk);
                return;
            }

            // 2. Toptancı Kamyonunu Sevk Etmeyi Dene
            bool dispatchSuccess = false;
            if (WholesaleTruckManager.Instance != null)
            {
                dispatchSuccess = WholesaleTruckManager.Instance.DispatchWholesaleDelivery(orderList);
            }

            if (!dispatchSuccess)
            {
                // Kamyon yoldaysa veya sevk edilemediyse para harcanmaz ve onay mesajı gösterilmez.
                return;
            }

            // 3. Yalnızca Kamyon Yola Çıktıysa Para Harcaması ve Finans Kaydı Yap
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SpendCredits(totalCost);
            }

            if (FinanceManager.Instance != null)
            {
                string catName = LocalizationManager.L("TrxCat_Wholesale", "Toptan/Alışveriş", "Wholesale/Shopping");
                string descFmt = LocalizationManager.L("TrxDesc_BulkOrderFmt", "Toplu Sipariş (%20 İndirimli - {0} Koli)", "Bulk Order (20% Discounted - {0} Packs)");
                FinanceManager.Instance.RecordExpense(catName, string.Format(descFmt, orderList.Count), totalCost);
            }

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotifyBulkOrderPlaced();
            }

            // Arayüzü Güncelle
            RenderShoppingCategoryContent();

            string successTitle = LocalizationManager.L("Modal_BulkSuccess_Title", "Toplu Sipariş Alındı! 🚛", "Bulk Order Placed! 🚛");
            string successBody = string.Format(LocalizationManager.L("Modal_BulkSuccess_Body", "Toplu siparişiniz başarıyla alındı!\n\n📦 Toplam **{0} Koli** ({1} Adet Ürün)\n💰 Ödenen Tutar: **{2:N0}C** (%20 İndirimli)\n🎉 Sağlanan Tasarruf: **{3:N0}C**\n\nToptancı kamyonu Mal Kabul kapısına teslimat yapmak üzere yola çıktı!", "Your bulk order has been successfully placed!\n\n📦 Total **{0} Packs** ({1} Total Items)\n💰 Paid Amount: **{2:N0}C** (20% Discounted)\n🎉 Savings Achieved: **{3:N0}C**\n\nThe wholesaler delivery truck is on its way to Goods Receipt loading dock!"), orderList.Count, orderList.Count * 50, totalCost, savings);

            ModalManager.ShowModal(successTitle, successBody, btnOk);
        }

        private void CreateSocialMediaAppView(Transform parent)
        {
            GameObject viewObj = new GameObject("SocialMediaApp_View");
            viewObj.transform.SetParent(parent, false);

            RectTransform vRect = viewObj.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;

            socialMediaAppView = viewObj.transform;

            // 1. ÜST BAŞLIK ŞERİDİ
            GameObject headerObj = new GameObject("SocialHeader");
            headerObj.transform.SetParent(viewObj.transform, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 212f);
            hRect.sizeDelta = new Vector2(850f, 36f);

            GameObject backBtnObj = new GameObject("BackBtn");
            backBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform bRect = backBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(-360f, 0f);
            bRect.sizeDelta = new Vector2(130f, 34f);

            Image bBg = backBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(130, 34, 17, new Color(0.20f, 0.25f, 0.32f, 0.90f));
            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;
            bBtn.onClick.AddListener(ShowHomeScreen);

            Text bTxt = CreateTextInPanel(backBtnObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_HomeScreen", "← Ana Ekran", "← Home Screen"), 16, new Color(0.35f, 0.85f, 1.0f));
            bTxt.alignment = TextAnchor.MiddleCenter;
            bTxt.fontStyle = FontStyle.Bold;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 0f);
            tRect.sizeDelta = new Vector2(400f, 36f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = LocalizationManager.L("App_SocialHeader", "CHIRPER / SOSYAL MEDYA", "CHIRPER / SOCIAL MEDIA");
            tText.fontSize = 22;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.12f, 0.70f, 0.95f);
            tText.raycastTarget = false;

            // Tweet At / Duyuru Butonu
            GameObject composeBtnObj = new GameObject("ComposeBtn");
            composeBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform cRect = composeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(350f, 0f);
            cRect.sizeDelta = new Vector2(140f, 36f);

            Image cBg = composeBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(140, 36, 18, new Color(0.12f, 0.65f, 0.95f));
            Button cBtn = composeBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBg;
            cBtn.onClick.AddListener(ShowComposeTweetModal);

            Text cTxt = CreateTextInPanel(composeBtnObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_PostTweet", "✍️ GÖNDERİ PAYLAŞ", "✍️ NEW POST"), 15, Color.white);
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.fontStyle = FontStyle.Bold;

            // 2. SOL PANEL (PROFİL & TRENDLER)
            GameObject leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(viewObj.transform, false);
            RectTransform lpRect = leftPanel.AddComponent<RectTransform>();
            lpRect.anchoredPosition = new Vector2(-285f, -25f);
            lpRect.sizeDelta = new Vector2(260f, 370f);

            // Profil Kartı (Üst Yarım)
            GameObject profileCard = new GameObject("ProfileCard");
            profileCard.transform.SetParent(leftPanel.transform, false);
            RectTransform pcRect = profileCard.AddComponent<RectTransform>();
            pcRect.anchoredPosition = new Vector2(0f, 88f);
            pcRect.sizeDelta = new Vector2(260f, 184f);

            Image pcBg = profileCard.AddComponent<Image>();
            pcBg.sprite = UIStyleUtility.CreateOutlinePillSprite(260, 184, 16, 2, new Color(0.12f, 0.65f, 0.95f), new Color(0.12f, 0.16f, 0.22f, 0.95f));
            Button pcBtn = profileCard.AddComponent<Button>();
            pcBtn.targetGraphic = pcBg;
            pcBtn.onClick.AddListener(() => {
                activeSocialTab = 2; // Profilim sekmesine geç
                RefreshSocialMediaViews();
            });

            string pName = SocialMediaManager.Instance != null ? SocialMediaManager.Instance.GetPlayerFullName() : "Alex Morgan";
            string pHandle = SocialMediaManager.Instance != null ? SocialMediaManager.Instance.GetPlayerHandle() : "@AlexMorgan";
            string sName = SocialMediaManager.Instance != null ? SocialMediaManager.Instance.GetStoreName() : "Fresh Shelf Market";
            int followers = SocialMediaManager.Instance != null ? SocialMediaManager.Instance.FollowerCount : 1420;

            Text pcTxt = CreateTextInPanel(profileCard.transform, Vector2.zero, Vector2.one, "", 15, Color.white);
            string profileFmt = LocalizationManager.L(
                "Social_ProfileCardFmt",
                "<b><size=18>{0}</size></b>\n<size=13><color=#80B0FF>{1}</color></size>\n\n<color=#00E676><b>@{2}</b></color>\n<b><size=15>{3:N0}</size></b> Takipçi  •  <b><color=#FFD700>4.9 ★</color></b>\n<size=12><color=#A0AAB5>\"Tarladan rafa taptaze mahsuller!\"</color></size>\n\n<size=13><color=#00E676><b>Profile Gitmek İçin Dokun</b></color></size>",
                "<b><size=18>{0}</size></b>\n<size=13><color=#80B0FF>{1}</color></size>\n\n<color=#00E676><b>@{2}</b></color>\n<b><size=15>{3:N0}</size></b> Followers  •  <b><color=#FFD700>4.9 ★</color></b>\n<size=12><color=#A0AAB5>\"Fresh farm crops to your shelves!\"</color></size>\n\n<size=13><color=#00E676><b>Tap to View Profile</b></color></size>"
            );
            pcTxt.text = string.Format(profileFmt, pName, pHandle, sName.Replace(" ", ""), followers);
            pcTxt.alignment = TextAnchor.MiddleCenter;
            pcTxt.lineSpacing = 1.1f;

            socialProfileCardTxt = pcTxt;

            // Trendler Kartı (Alt Yarım)
            GameObject trendCard = new GameObject("TrendCard");
            trendCard.transform.SetParent(leftPanel.transform, false);
            RectTransform tcRect = trendCard.AddComponent<RectTransform>();
            tcRect.anchoredPosition = new Vector2(0f, -92f);
            tcRect.sizeDelta = new Vector2(260f, 170f);

            Image tcBg = trendCard.AddComponent<Image>();
            tcBg.sprite = UIStyleUtility.CreateOutlinePillSprite(260, 170, 16, 1, new Color(0.25f, 0.35f, 0.45f), new Color(0.10f, 0.14f, 0.20f, 0.95f));

            Text tcTxt = CreateTextInPanel(trendCard.transform, Vector2.zero, Vector2.one, "", 14, Color.white);
            RectTransform tcTxtRect = tcTxt.GetComponent<RectTransform>();
            tcTxtRect.offsetMin = new Vector2(14f, 8f);
            tcTxtRect.offsetMax = new Vector2(-14f, -8f);

            tcTxt.text = SocialMediaManager.Instance != null ? SocialMediaManager.Instance.GetDailyTrendsFormatted() : "";
            tcTxt.alignment = TextAnchor.MiddleCenter;
            tcTxt.lineSpacing = 1.12f;
            socialTrendCardTxt = tcTxt;

            // 3. SAĞ PANEL (SEKMELER & AKIŞ)
            GameObject rightPanel = new GameObject("RightFeedPanel");
            rightPanel.transform.SetParent(viewObj.transform, false);
            RectTransform rpRect = rightPanel.AddComponent<RectTransform>();
            rpRect.anchoredPosition = new Vector2(135f, -25f);
            rpRect.sizeDelta = new Vector2(570f, 370f);

            // Sekme Düğmeleri (Üst)
            GameObject tabsBar = new GameObject("FeedTabsBar");
            tabsBar.transform.SetParent(rightPanel.transform, false);
            RectTransform tbRect = tabsBar.AddComponent<RectTransform>();
            tbRect.anchoredPosition = new Vector2(0f, 165f);
            tbRect.sizeDelta = new Vector2(570f, 36f);

            string[] tabNames = new string[] {
                LocalizationManager.L("SocialTab_ForYou", "1. Sana Özel", "1. For You"),
                LocalizationManager.L("SocialTab_Reviews", "2. Yorumlar", "2. Reviews"),
                LocalizationManager.L("SocialTab_MyTweets", "3. Gönderilerim", "3. My Posts")
            };

            for (int t = 0; t < 3; t++)
            {
                int tabIdx = t;
                GameObject tBtnObj = new GameObject("Tab_" + t);
                tBtnObj.transform.SetParent(tabsBar.transform, false);
                RectTransform tabRect = tBtnObj.AddComponent<RectTransform>();
                tabRect.anchoredPosition = new Vector2(-182f + t * 182f, 0f);
                tabRect.sizeDelta = new Vector2(178f, 36f);

                bool isSel = (activeSocialTab == tabIdx);
                Image tBg = tBtnObj.AddComponent<Image>();
                tBg.sprite = UIStyleUtility.CreateRoundedPillSprite(178, 36, 12, isSel ? new Color(0.12f, 0.65f, 0.95f) : new Color(0.15f, 0.20f, 0.28f));
                socialTabBtnImgs[t] = tBg;

                Button tBtn = tBtnObj.AddComponent<Button>();
                tBtn.targetGraphic = tBg;
                tBtn.onClick.AddListener(() => {
                    activeSocialTab = tabIdx;
                    RefreshSocialMediaViews();
                });

                Text tTxt = CreateTextInPanel(tBtnObj.transform, Vector2.zero, Vector2.one, tabNames[t], 15, isSel ? Color.white : new Color(0.70f, 0.78f, 0.88f));
                tTxt.alignment = TextAnchor.MiddleCenter;
                tTxt.fontStyle = FontStyle.Bold;
                tTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            // Scrollable Feed Area
            GameObject feedScrollObj = new GameObject("FeedScrollArea");
            feedScrollObj.transform.SetParent(rightPanel.transform, false);
            RectTransform fsRect = feedScrollObj.AddComponent<RectTransform>();
            fsRect.anchoredPosition = new Vector2(0f, -20f);
            fsRect.sizeDelta = new Vector2(570f, 320f);

            Image fsBg = feedScrollObj.AddComponent<Image>();
            fsBg.color = new Color(0.06f, 0.08f, 0.12f, 0.01f);
            fsBg.raycastTarget = true;

            ScrollRect scrollRect = feedScrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(feedScrollObj.transform, false);
            RectTransform vpRect = viewportObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;

            Image vpImg = viewportObj.AddComponent<Image>();
            vpImg.color = Color.white;
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject feedContentObj = new GameObject("Content");
            feedContentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform fcRect = feedContentObj.AddComponent<RectTransform>();
            fcRect.anchorMin = new Vector2(0f, 1f);
            fcRect.anchorMax = new Vector2(1f, 1f);
            fcRect.pivot = new Vector2(0.5f, 1f);
            fcRect.sizeDelta = new Vector2(0f, 0f);

            scrollRect.viewport = vpRect;
            scrollRect.content = fcRect;

            VerticalLayoutGroup vlg = feedContentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ContentSizeFitter csf = feedContentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            socialMediaFeedContent = feedContentObj.transform;

            viewObj.SetActive(false);
        }

        private void RefreshSocialMediaViews()
        {
            if (socialTabBtnImgs != null)
            {
                for (int t = 0; t < socialTabBtnImgs.Length; t++)
                {
                    if (socialTabBtnImgs[t] != null)
                    {
                        bool isSel = (activeSocialTab == t);
                        socialTabBtnImgs[t].sprite = UIStyleUtility.CreateRoundedPillSprite(178, 36, 12, isSel ? new Color(0.12f, 0.65f, 0.95f) : new Color(0.15f, 0.20f, 0.28f));
                    }
                }
            }

            // Sol Panel Profil ve Gündem Kartlarını Canlı Güncelle
            if (socialProfileCardTxt != null && SocialMediaManager.Instance != null)
            {
                string pName = SocialMediaManager.Instance.GetPlayerFullName();
                string pHandle = SocialMediaManager.Instance.GetPlayerHandle();
                string sName = SocialMediaManager.Instance.GetStoreName();
                int followers = SocialMediaManager.Instance.FollowerCount;
                float rating = SocialMediaManager.Instance.GetStoreRating();

                string profileFmt = LocalizationManager.L(
                    "Social_ProfileCardFmt",
                    "<b><size=18>{0}</size></b>\n<size=13><color=#80B0FF>{1}</color></size>\n\n<color=#00E676><b>@{2}</b></color>\n<b><size=15>{3:N0}</size></b> Takipçi  •  <b><color=#FFD700>{4:F1} ★</color></b>\n<size=12><color=#A0AAB5>\"Tarladan rafa taptaze mahsuller!\"</color></size>\n\n<size=13><color=#00E676><b>Profile Gitmek İçin Dokun</b></color></size>",
                    "<b><size=18>{0}</size></b>\n<size=13><color=#80B0FF>{1}</color></size>\n\n<color=#00E676><b>@{2}</b></color>\n<b><size=15>{3:N0}</size></b> Followers  •  <b><color=#FFD700>{4:F1} ★</color></b>\n<size=12><color=#A0AAB5>\"Fresh farm crops to your shelves!\"</color></size>\n\n<size=13><color=#00E676><b>Tap to View Profile</b></color></size>"
                );
                socialProfileCardTxt.text = string.Format(profileFmt, pName, pHandle, sName.Replace(" ", ""), followers, rating);
            }

            if (socialTrendCardTxt != null && SocialMediaManager.Instance != null)
            {
                socialTrendCardTxt.text = SocialMediaManager.Instance.GetDailyTrendsFormatted();
            }

            if (socialMediaFeedContent == null) return;
            foreach (Transform child in socialMediaFeedContent) Destroy(child.gameObject);

            if (SocialMediaManager.Instance == null) return;

            // 3. Gönderilerim sekmesinde üstte Yeni Gönderi Butonu
            if (activeSocialTab == 2)
            {
                GameObject newTweetBanner = new GameObject("NewTweetBanner");
                newTweetBanner.transform.SetParent(socialMediaFeedContent, false);
                RectTransform bRect = newTweetBanner.AddComponent<RectTransform>();
                bRect.sizeDelta = new Vector2(550f, 46f);
                LayoutElement bLe = newTweetBanner.AddComponent<LayoutElement>();
                bLe.minHeight = 46f;
                bLe.preferredHeight = 46f;
                bLe.flexibleWidth = 1f;

                Image bImg = newTweetBanner.AddComponent<Image>();
                bImg.sprite = UIStyleUtility.CreateRoundedPillSprite(550, 46, 23, new Color(0.12f, 0.65f, 0.95f));
                Button bBtn = newTweetBanner.AddComponent<Button>();
                bBtn.targetGraphic = bImg;
                bBtn.onClick.AddListener(ShowComposeTweetModal);

                Text bTxt = CreateTextInPanel(newTweetBanner.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_ComposeBanner", "✍️ YENİ GÖNDERİ PAYLAŞ (+Takipçi Kazan)", "✍️ POST NEW UPDATE (+Gain Followers)"), 16, Color.white);
                bTxt.alignment = TextAnchor.MiddleCenter;
                bTxt.fontStyle = FontStyle.Bold;
            }

            List<SocialTweetData> tweets = SocialMediaManager.Instance.GetFeed(activeSocialTab);

            // Eğer sekme boşsa bilgilendirme kartı göster
            if (tweets.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyTabNotice");
                emptyObj.transform.SetParent(socialMediaFeedContent, false);
                RectTransform eRect = emptyObj.AddComponent<RectTransform>();
                eRect.sizeDelta = new Vector2(550f, 100f);
                LayoutElement eLe = emptyObj.AddComponent<LayoutElement>();
                eLe.minHeight = 100f;
                eLe.preferredHeight = 100f;

                Image eBg = emptyObj.AddComponent<Image>();
                eBg.sprite = UIStyleUtility.CreateOutlinePillSprite(550, 100, 14, 1, new Color(0.20f, 0.35f, 0.50f), new Color(0.10f, 0.14f, 0.20f, 0.95f));

                string emptyMsg = (activeSocialTab == 1)
                    ? LocalizationManager.L("Social_NoCommentsYet", "Henüz yayınlanmış bir duyuru veya müşteri yorumu bulunmuyor.\nTwitlerim sekmesinden yeni bir duyuru paylaşabilirsiniz! 📢", "No announcements or customer comments found yet.\nYou can post an announcement from My Tweets! 📢")
                    : LocalizationManager.L("Social_NoTweetsYet", "Henüz bir twit paylaşmadınız.\nYukarıdaki butona tıklayarak ilk duyurunuzu yapın! ✍️", "You haven't posted any tweets yet.\nTap the button above to post your first tweet! ✍️");

                Text eTxt = CreateTextInPanel(emptyObj.transform, Vector2.zero, Vector2.one, emptyMsg, 15, new Color(0.70f, 0.85f, 1.0f));
                eTxt.alignment = TextAnchor.MiddleCenter;
                return;
            }

            foreach (var tweetData in tweets)
            {
                SocialTweetData tweet = tweetData;
                bool isReviewTab = (activeSocialTab == 1);
                bool hasComments = isReviewTab && (tweet.comments != null && tweet.comments.Count > 0);
                int commentCount = (tweet.comments != null) ? tweet.comments.Count : 0;

                float cardHeight = isReviewTab
                    ? (108f + (commentCount * 56f) + 12f)
                    : 106f;

                GameObject cardObj = new GameObject("TweetCard_" + tweet.tweetId);
                cardObj.transform.SetParent(socialMediaFeedContent, false);

                RectTransform cRect = cardObj.AddComponent<RectTransform>();
                cRect.sizeDelta = new Vector2(550f, cardHeight);

                LayoutElement le = cardObj.AddComponent<LayoutElement>();
                le.minHeight = cardHeight;
                le.preferredHeight = cardHeight;
                le.flexibleWidth = 1f;

                Color borderColor = tweet.isPlayerTweet ? new Color(0.12f, 0.65f, 0.95f) : (tweet.sentiment == TweetSentiment.Complaint ? new Color(0.90f, 0.35f, 0.25f) : (tweet.sentiment == TweetSentiment.Praise ? new Color(0.25f, 0.75f, 0.40f) : new Color(0.25f, 0.35f, 0.45f)));
                Image cBg = cardObj.AddComponent<Image>();
                cBg.sprite = UIStyleUtility.CreateOutlinePillSprite(550, (int)cardHeight, 14, 1, borderColor, new Color(0.12f, 0.16f, 0.22f, 0.96f));

                // 1. SOL AVATAR İKONU
                GameObject avatarObj = new GameObject("Avatar");
                avatarObj.transform.SetParent(cardObj.transform, false);
                RectTransform avRect = avatarObj.AddComponent<RectTransform>();
                avRect.anchorMin = new Vector2(0f, isReviewTab ? 1f : 0.5f);
                avRect.anchorMax = new Vector2(0f, isReviewTab ? 1f : 0.5f);
                avRect.pivot = new Vector2(0.5f, 0.5f);
                avRect.anchoredPosition = new Vector2(34f, isReviewTab ? -48f : 0f);
                avRect.sizeDelta = new Vector2(46f, 46f);

                Image avBg = avatarObj.AddComponent<Image>();
                avBg.sprite = UIStyleUtility.CreateRoundedPillSprite(46, 46, 23, tweet.avatarBgColor);

                Text avTxt = CreateTextInPanel(avatarObj.transform, Vector2.zero, Vector2.one, string.IsNullOrEmpty(tweet.avatarEmoji) ? "👤" : tweet.avatarEmoji, 24, Color.white);
                avTxt.alignment = TextAnchor.MiddleCenter;

                // 2. ORTA METİN ALANI
                GameObject infoObj = new GameObject("Info");
                infoObj.transform.SetParent(cardObj.transform, false);
                RectTransform iRect = infoObj.AddComponent<RectTransform>();
                if (isReviewTab)
                {
                    iRect.anchorMin = new Vector2(0f, 1f);
                    iRect.anchorMax = new Vector2(1f, 1f);
                    iRect.pivot = new Vector2(0f, 1f);
                    iRect.anchoredPosition = new Vector2(66f, -6f);
                    iRect.sizeDelta = new Vector2(-205f, 88f);
                }
                else
                {
                    iRect.anchorMin = Vector2.zero;
                    iRect.anchorMax = Vector2.one;
                    iRect.offsetMin = new Vector2(66f, 8f);
                    iRect.offsetMax = new Vector2(-140f, -8f);
                }

                Text iTxt = infoObj.AddComponent<Text>();
                iTxt.font = globalFont;

                string verifiedMark = tweet.isVerified ? "<color=#00E676><b>[ONAYLI]</b></color>" : "";
                string sentimentBadge = tweet.sentiment == TweetSentiment.Official
                    ? "<color=#00E676><b>[DUYURU]</b></color>"
                    : (tweet.sentiment == TweetSentiment.Complaint ? "<color=#FF5252><b>[ŞİKAYET]</b></color>" : (tweet.sentiment == TweetSentiment.Praise ? "<color=#40C4FF><b>[MÜŞTERİ]</b></color>" : "<color=#FFD54F><b>[GÜNDEM]</b></color>"));

                if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish)
                {
                    verifiedMark = tweet.isVerified ? "<color=#00E676><b>[VERIFIED]</b></color>" : "";
                    sentimentBadge = tweet.sentiment == TweetSentiment.Official
                        ? "<color=#00E676><b>[OFFICIAL]</b></color>"
                        : (tweet.sentiment == TweetSentiment.Complaint ? "<color=#FF5252><b>[COMPLAINT]</b></color>" : (tweet.sentiment == TweetSentiment.Praise ? "<color=#40C4FF><b>[REVIEW]</b></color>" : "<color=#FFD54F><b>[TREND]</b></color>"));
                }

                iTxt.text = $"<b><size=15>{tweet.authorName}</size></b> {verifiedMark} <color=#80B0FF><size=12>({tweet.authorHandle} • {tweet.LocalizedTime})</size></color>  {sentimentBadge}\n<size=14><color=#F0F6FC>{tweet.LocalizedText}</color></size>";
                iTxt.fontSize = 14;
                iTxt.lineSpacing = 1.15f;
                iTxt.alignment = TextAnchor.MiddleLeft;
                iTxt.color = Color.white;

                // 3. SAĞ ETKİLEŞİM BUTONLARI (BEĞENİ & RETWEET)
                GameObject actionsObj = new GameObject("Actions");
                actionsObj.transform.SetParent(cardObj.transform, false);
                RectTransform aRect = actionsObj.AddComponent<RectTransform>();
                aRect.anchorMin = new Vector2(1f, isReviewTab ? 1f : 0.5f);
                aRect.anchorMax = new Vector2(1f, isReviewTab ? 1f : 0.5f);
                aRect.pivot = new Vector2(1f, 0.5f);
                aRect.anchoredPosition = new Vector2(-12f, isReviewTab ? -48f : 0f);
                aRect.sizeDelta = new Vector2(120f, 40f);

                // Heart Button
                GameObject heartBtnObj = new GameObject("HeartBtn");
                heartBtnObj.transform.SetParent(actionsObj.transform, false);
                RectTransform hRect = heartBtnObj.AddComponent<RectTransform>();
                hRect.anchoredPosition = new Vector2(-32f, 0f);
                hRect.sizeDelta = new Vector2(56f, 34f);

                Image hBg = heartBtnObj.AddComponent<Image>();
                hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(56, 34, 14, tweet.isLikedByPlayer ? new Color(0.90f, 0.25f, 0.35f) : new Color(0.20f, 0.25f, 0.32f));
                Button hBtn = heartBtnObj.AddComponent<Button>();
                hBtn.targetGraphic = hBg;
                hBtn.onClick.AddListener(() => {
                    SocialMediaManager.Instance.ToggleLike(tweet);
                    RefreshSocialMediaViews();
                });

                Text hTxt = CreateTextInPanel(heartBtnObj.transform, Vector2.zero, Vector2.one, $"❤️ {tweet.likesCount}", 13, Color.white);
                hTxt.alignment = TextAnchor.MiddleCenter;
                hTxt.fontStyle = FontStyle.Bold;

                // Repost Button
                GameObject rtBtnObj = new GameObject("RTBtn");
                rtBtnObj.transform.SetParent(actionsObj.transform, false);
                RectTransform rRect = rtBtnObj.AddComponent<RectTransform>();
                rRect.anchoredPosition = new Vector2(32f, 0f);
                rRect.sizeDelta = new Vector2(56f, 34f);

                Image rBg = rtBtnObj.AddComponent<Image>();
                rBg.sprite = UIStyleUtility.CreateRoundedPillSprite(56, 34, 14, tweet.isRetweetedByPlayer ? new Color(0.20f, 0.75f, 0.40f) : new Color(0.20f, 0.25f, 0.32f));
                Button rBtn = rtBtnObj.AddComponent<Button>();
                rBtn.targetGraphic = rBg;
                rBtn.onClick.AddListener(() => {
                    SocialMediaManager.Instance.ToggleRetweet(tweet);
                    RefreshSocialMediaViews();
                });

                Text rTxt = CreateTextInPanel(rtBtnObj.transform, Vector2.zero, Vector2.one, $"🔄 {tweet.retweetsCount}", 13, Color.white);
                rTxt.alignment = TextAnchor.MiddleCenter;
                rTxt.fontStyle = FontStyle.Bold;

                // 4. SADECE 2. YORUMLAR SEKMESİNDE: MÜŞTERİ YORUMLARI LİSTESİ
                if (hasComments)
                {
                    GameObject commentsGroup = new GameObject("CommentsGroup");
                    commentsGroup.transform.SetParent(cardObj.transform, false);
                    RectTransform cgRect = commentsGroup.AddComponent<RectTransform>();
                    cgRect.anchorMin = new Vector2(0f, 0f);
                    cgRect.anchorMax = new Vector2(1f, 0f);
                    cgRect.pivot = new Vector2(0.5f, 0f);
                    cgRect.anchoredPosition = new Vector2(0f, 8f);
                    cgRect.sizeDelta = new Vector2(530f, commentCount * 56f);

                    for (int c = 0; c < tweet.comments.Count; c++)
                    {
                        var cmt = tweet.comments[c];
                        GameObject cmtObj = new GameObject("Comment_" + c);
                        cmtObj.transform.SetParent(commentsGroup.transform, false);
                        RectTransform cmtRect = cmtObj.AddComponent<RectTransform>();
                        cmtRect.anchorMin = new Vector2(0.5f, 1f);
                        cmtRect.anchorMax = new Vector2(0.5f, 1f);
                        cmtRect.pivot = new Vector2(0.5f, 1f);
                        cmtRect.anchoredPosition = new Vector2(0f, -c * 56f);
                        cmtRect.sizeDelta = new Vector2(530f, 50f);

                        Image cmtBg = cmtObj.AddComponent<Image>();
                        cmtBg.sprite = UIStyleUtility.CreateOutlinePillSprite(530, 50, 10, 1, new Color(0.25f, 0.45f, 0.65f, 0.70f), new Color(0.08f, 0.12f, 0.18f, 0.95f));

                        // Yorum Avatarı
                        GameObject cmtAvObj = new GameObject("CmtAvatar");
                        cmtAvObj.transform.SetParent(cmtObj.transform, false);
                        RectTransform cmtAvRect = cmtAvObj.AddComponent<RectTransform>();
                        cmtAvRect.anchorMin = new Vector2(0f, 0.5f);
                        cmtAvRect.anchorMax = new Vector2(0f, 0.5f);
                        cmtAvRect.pivot = new Vector2(0.5f, 0.5f);
                        cmtAvRect.anchoredPosition = new Vector2(24f, 0f);
                        cmtAvRect.sizeDelta = new Vector2(34f, 34f);
                        Image cmtAvBg = cmtAvObj.AddComponent<Image>();
                        cmtAvBg.sprite = UIStyleUtility.CreateRoundedPillSprite(34, 34, 17, cmt.avatarBgColor);
                        Text cmtAvTxt = CreateTextInPanel(cmtAvObj.transform, Vector2.zero, Vector2.one, cmt.avatarEmoji, 18, Color.white);
                        cmtAvTxt.alignment = TextAnchor.MiddleCenter;

                        // Yorum Metin Kutusu
                        GameObject cmtInfoObj = new GameObject("CmtInfo");
                        cmtInfoObj.transform.SetParent(cmtObj.transform, false);
                        RectTransform cmtInfoRect = cmtInfoObj.AddComponent<RectTransform>();
                        cmtInfoRect.anchorMin = Vector2.zero;
                        cmtInfoRect.anchorMax = Vector2.one;
                        cmtInfoRect.offsetMin = new Vector2(48f, 2f);
                        cmtInfoRect.offsetMax = new Vector2(-65f, -2f);

                        Text cmtTxt = cmtInfoObj.AddComponent<Text>();
                        cmtTxt.font = globalFont;
                        cmtTxt.text = $"<b><size=13>{cmt.authorName}</size></b> <color=#80B0FF><size=11>({cmt.authorHandle} • {cmt.LocalizedTime})</size></color>\n<size=13><color=#E0F0FF>{cmt.LocalizedText}</color></size>";
                        cmtTxt.fontSize = 13;
                        cmtTxt.lineSpacing = 1.1f;
                        cmtTxt.alignment = TextAnchor.MiddleLeft;
                        cmtTxt.color = Color.white;

                        // Yorum Beğeni Rozeti (Sağ)
                        GameObject cmtLikeObj = new GameObject("CmtLike");
                        cmtLikeObj.transform.SetParent(cmtObj.transform, false);
                        RectTransform cmtLikeRect = cmtLikeObj.AddComponent<RectTransform>();
                        cmtLikeRect.anchorMin = new Vector2(1f, 0.5f);
                        cmtLikeRect.anchorMax = new Vector2(1f, 0.5f);
                        cmtLikeRect.pivot = new Vector2(1f, 0.5f);
                        cmtLikeRect.anchoredPosition = new Vector2(-8f, 0f);
                        cmtLikeRect.sizeDelta = new Vector2(54f, 26f);
                        Text cmtLikeTxt = CreateTextInPanel(cmtLikeObj.transform, Vector2.zero, Vector2.one, $"<color=#FF8080>❤️ {cmt.likesCount}</color>", 12, Color.white);
                        cmtLikeTxt.alignment = TextAnchor.MiddleCenter;
                        cmtLikeTxt.fontStyle = FontStyle.Bold;
                    }
                }
            }
        }

        private void ShowComposeTweetModal()
        {
            GameObject existing = GameObject.Find("Global_Compose_Tweet_Canvas");
            if (existing != null) DestroyImmediate(existing);

            GameObject canvasObj = new GameObject("Global_Compose_Tweet_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Backdrop
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;
            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
            bdImg.raycastTarget = true;

            // Modal Box
            GameObject boxObj = new GameObject("Box");
            boxObj.transform.SetParent(backdrop.transform, false);
            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(760f, 560f);

            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.sprite = UIStyleUtility.CreateOutlinePillSprite(760, 560, 20, 2, new Color(0.12f, 0.65f, 0.95f), new Color(0.10f, 0.14f, 0.20f, 0.98f));

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(boxObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 240f);
            tRect.sizeDelta = new Vector2(700f, 40f);

            Text tTxt = titleObj.AddComponent<Text>();
            tTxt.font = globalFont;
            tTxt.text = LocalizationManager.L("Compose_Header", "GÖNDERİ PAYLAŞ (30 SEÇENEK)", "CREATE POST (30 OPTIONS)");
            tTxt.fontSize = 25;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = new Color(0.30f, 0.85f, 1.0f);

            // Close Button (Top-Right X)
            GameObject closeBtnObj = new GameObject("CloseXBtn");
            closeBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(355f, 245f);
            cRect.sizeDelta = new Vector2(40f, 40f);

            Image cBg = closeBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(40, 40, 20, new Color(0.92f, 0.18f, 0.20f, 1f));
            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBg;
            cBtn.onClick.AddListener(() => Destroy(canvasObj));

            Text cTxt = CreateTextInPanel(closeBtnObj.transform, Vector2.zero, Vector2.one, "✖", 24, Color.white);
            cTxt.alignment = TextAnchor.MiddleCenter;

            // Scroll Area for 30 Tweets
            GameObject scrollObj = new GameObject("TweetListScroll");
            scrollObj.transform.SetParent(boxObj.transform, false);
            RectTransform sRect = scrollObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, -20f);
            sRect.sizeDelta = new Vector2(720f, 450f);

            Image sBg = scrollObj.AddComponent<Image>();
            sBg.color = new Color(0.05f, 0.07f, 0.10f, 0.40f);

            ScrollRect sRectComp = scrollObj.AddComponent<ScrollRect>();
            sRectComp.horizontal = false;
            sRectComp.vertical = true;
            sRectComp.movementType = ScrollRect.MovementType.Clamped;

            GameObject vpObj = new GameObject("Viewport");
            vpObj.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = vpObj.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vpObj.AddComponent<Image>().color = Color.white;
            Mask vpMask = vpObj.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(vpObj.transform, false);
            RectTransform cntRect = contentObj.AddComponent<RectTransform>();
            cntRect.anchorMin = new Vector2(0f, 1f);
            cntRect.anchorMax = new Vector2(1f, 1f);
            cntRect.pivot = new Vector2(0.5f, 1f);

            sRectComp.viewport = vpRect;
            sRectComp.content = cntRect;

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            string sName = SocialMediaManager.Instance != null ? SocialMediaManager.Instance.GetStoreName() : "Fresh Shelf Market";

            (string titleTr, string titleEn, string textTr, string textEn)[] tweetOptions = new (string, string, string, string)[]
            {
                ("🚀 Mağaza Açılışı", "Grand Opening", $"Taptaze çiftlik mahsullerimizle dükkanımız hizmetinizde! Hepinizi @{sName} bekliyoruz! 🌾🛒", $"Grand opening! Fresh farm crops and wide variety of products ready for you at @{sName}! 🌾🛒"),
                ("🔥 %20 İndirim Kampanyası", "20% Discount Sale", $"TÜM ÜRÜNLERDE %20 İNDİRİM! Tarladan rafa taze sebze ve meyveler @{sName} dükkanında özel fiyatla! 🏷️🎉", $"20% OFF ALL PRODUCTS! Fresh vegetables and fruits direct from farm to shelf at @{sName}! 🏷️🎉"),
                ("⚡ Hızlı Kasa & Kesintisiz Hizmet", "Fast Checkout & Zero Wait", $"Ekstra kasalarımız açıldı! Sıra beklemeden taze ve hızlı alışverişin tadını çıkarın! @{sName} ⚡😊", $"Extra checkout lines open! Enjoy lightning fast shopping with zero queue wait times at @{sName}! ⚡😊"),
                ("🌱 %100 Organik Taze Hasat", "100% Organic Fresh Harvest", $"Çiftliğimizden bu sabah toplanan %100 organik domates, çilek ve yeşillikler raflarda! @{sName} 🍅🍓", $"100% organic tomatoes, strawberries and greens harvested this morning are now stocked at @{sName}! 🍅🍓"),
                ("🏢 Yeni Reyonlar & Genişletme", "Supermarket Expansion", $"Mağazamızı büyüttük! Soğuk içecekler, fırın ürünleri ve yeni reyonlarımız açıldı! @{sName} 🥖🥤", $"Store expanded! Introducing our brand new cold beverage, bakery and fresh aisles at @{sName}! 🥖🥤"),
                ("🌙 Gece İndirimi & Fırsatlar", "Late Night Clearance Deal", $"Gece alışverişi fırsatı! Kapanış öncesi şarküteri ve unlu mamullerde özel indirimler! @{sName} 🌙✨", $"Late night clearance deal! Special discounts on deli and bakery products before closing at @{sName}! 🌙✨"),
                ("👑 VIP Müşteri Sadakat Ödülleri", "VIP Customer Appreciation", $"Sadık müşterilerimize özel sürpriz hediye çekleri ve bonus puan kampanyamız başladı! @{sName} 🎁🌟", $"VIP customer appreciation day! Earn bonus points and voucher gifts with every order at @{sName}! 🎁🌟"),
                ("🧀 Taze Süt & Şarküteri Reyonu", "Fresh Dairy & Cold Deli", $"Günlük taze süt, organik peynir ve tereyağları soğutucu dolaplarımızda sizleri bekliyor! @{sName} 🥛🧀", $"Daily fresh milk, artisan cheese and organic butter now stocked in refrigerated displays at @{sName}! 🥛🧀"),
                ("✨ Hijyen & Temizlik Garantisi", "Sanitation & Cleanliness", $"Dükkanımızda hijyen ve temizlik standartlarımız %100! Güvenle alışveriş yapabilirsiniz. @{sName} ✨🧹", $"Top tier store cleanliness and sanitation standards guaranteed for your safe shopping at @{sName}! ✨🧹"),
                ("🌾 Hafta Sonu Tarım Festivali", "Weekend Harvest Festival", $"Hafta sonuna özel Çiftlikten Rafa Tarım Festivali başladı! Sürpriz indirimleri kaçırmayın! @{sName} 🎪🌾", $"Weekend Farm-to-Shelf Harvest Festival is live! Don't miss out on special surprise deals at @{sName}! 🎪🌾"),
                ("🥒 Salatalık Krizi Çözüldü (Esprili)", "Cucumber Crisis Solved", $"Saksıda yetiştirmeye gerek kalmadı; en çıtır salatalıklar tarla fiyatına @{sName} reyonlarında! 🥒😂", $"No need to plant a greenhouse at home; crispiest cucumbers are at @{sName} at farm prices! 🥒😂"),
                ("🏎️ Drift Yapmayan Arabalar", "Smooth Shopping Carts", $"Tüm market arabalarımızın tekerlekleri yağlandı! Artık Formula 1 gibi değil, ipek gibi kayıyor @{sName} 🏎️🛒", $"All shopping cart wheels just got oiled! Glide smoothly through the aisles at @{sName} 🏎️🛒"),
                ("🥖 Sıcak Ekmek & Diyet Alarmı", "Warm Bread vs Diet", $"Fırınımızdan çıkan sıcacık ekmek kokusu diyet bozdurabilir, sorumluluk kabul etmiyoruz! @{sName} 🥖🤤", $"Fresh baked warm bread scent may break your diet, we take zero responsibility! @{sName} 🥖🤤"),
                ("🍉 Karpuz Vurma Uzmanları", "Watermelon Tapping Masters", $"Gözü kapalı en tatlı karpuzu seçebilen dedeler ve uzmanlar manav reyonumuza davetlidir! @{sName} 🍉👂", $"National watermelon tapping experts are invited to our fruit section to find the sweetest ones! @{sName} 🍉👂"),
                ("🧊 Gece 02:00 Buzdolabı Nöbeti", "2 AM Fridge Club", $"Gece dolabı açıp boş boş bakanlar için rafları en leziz gece atıştırmalıklarıyla doldurduk! @{sName} 🧊👀", $"For everyone staring at empty fridges at 2 AM, our shelves are stocked with midnight snacks! @{sName} 🧊👀"),
                ("⚡ Pit Stop Hızında Kasiyerler", "Pit Stop Cashiers", $"Kasiyerlerimiz ürünleri öyle hızlı okutuyor ki poşeti açmaya zamanınız kalmayabilir! @{sName} ⚡🛍️", $"Our cashiers scan items at supersonic speed, you better have your grocery bags ready! @{sName} ⚡🛍️"),
                ("🪙 1 Kuruş Arama Çilesine Son", "Exact Change No More", $"Cebinizde arkeolojik kazı yapmanıza gerek yok, temassız ödeyin geçin! @{sName} 💳🪙", $"No need to dig for pocket change, tap your contactless card and breeze through! @{sName} 💳🪙"),
                ("🥑 Kusursuz Olgun Avokado", "Perfect Ripe Avocado", $"15 dakika dedektiflik yapmaya son! Tam kıvamında yumuşacık organik avokadolar raflarda! @{sName} 🥑🥑", $"No more 15-minute detective work; perfectly ripe organic avocados are ready at @{sName}! 🥑🥑"),
                ("☕ ASMR Kahve Molası", "ASMR Coffee Break", $"Kahve otomatımızın taze çekirdek öğütme sesi eşliğinde reyonları gezmeye bekleriz! @{sName} ☕🎶", $"Enjoy browsing aisles with the soothing ASMR sound of freshly ground bean coffee at @{sName}! ☕🎶"),
                ("🍓 Çilek Reçeli Sevdalıları", "Strawberry Jam Lovers", $"Evde 5 kavanoz reçeliniz olsa bile bu çileklerin kokusuna dayanamayıp bir tane daha alacaksınız! @{sName} 🍓🤤", $"Even if you have 5 jam jars at home, the aroma of our farm strawberries will make you buy one more! @{sName} 🍓🤤"),
                ("🐠 Akvaryum Önü Terapi Seansı", "Aquarium Therapy Session", $"Girişteki devasa akvaryumumuzda balıkları izlerken günün tüm stresini unutun! @{sName} 🐠🌿", $"Unwind and leave daily stress behind while admiring exotic fish in our store aquarium at @{sName}! 🐠🌿"),
                ("🍦 Dondurma Dolabı Acil Durum", "Ice Cream Freezer SOS", $"Çikolatalı mı vanilyalı mı karar veremeyenler için ikisini de indirimli yaptık! @{sName} 🍦🍨", $"Can't decide between chocolate or vanilla? We put both on special discount at @{sName}! 🍦🍨"),
                ("📝 Unutulan Alışveriş Listeleri", "Forgotten Grocery Lists", $"Alışveriş listesini evde unutanlar üzülmesin, reyon düzenimiz size ne alacağınızı hatırlatır! @{sName} 📝🧠", $"Forgot your shopping list at home? Our organized aisles will remind you of everything! @{sName} 📝🧠"),
                ("🚗 Park Yeri Arama Derdine Son", "Effortless Parking", $"Genişletilmiş çift turnikeli otoparkımızda yeriniz her zaman hazır! Park edin, rahatça alışveriş yapın @{sName} 🚗🅿️", $"Spacious parking lot always has a spot waiting for you! Park and shop with peace of mind at @{sName} 🚗🅿️"),
                ("🍋 C Vitamini Patlaması", "Vitamin C Surge", $"Taze sıkılmış narenciye reyonumuzdan geçerken bile enerjinizin yükseldiğini hissedeceksiniz! @{sName} 🍊🍋", $"Feel an instant natural energy surge just passing by our freshly squeezed citrus stand at @{sName}! 🍊🍋"),
                ("📦 Koli Açma Terapisi", "Box Unboxing Therapy", $"Bugün çiftliğimizden gelen onlarca taze koli reyonlara dizildi, tazelik kokusu dükkanı sardı! @{sName} 📦🌾", $"Dozens of fresh crop crates straight from the farm stocked on shelves today! @{sName} 📦🌾"),
                ("💛 Sarı Etiket Avcıları", "Yellow Tag Hunters", $"Günün en tatlı sarı indirim etiketleri reyonlara asıldı, acele eden kazanır! @{sName} 🏷️💛", $"Bright yellow discount tags just placed across aisles, early birds get the best deals at @{sName}! 🏷️💛"),
                ("🌻 Ayçiçeği Tarlalarından", "From Sunflower Fields", $"Doğal güneşle olgunlaşan en taze tarla mahsulleri doğrudan raflarımızda! @{sName} 🌻🌾", $"Farm crops ripened under natural sunshine delivered straight to our shelves at @{sName}! 🌻🌾"),
                ("🍫 Gizli Çikolata Kaçamağı", "Secret Chocolate Day", $"Brokoli alırken yanına minik bir çikolata ekleyenler... Sizi anlıyoruz ve destekliyoruz! @{sName} 🥦🍫", $"To everyone secretly slipping a chocolate bar next to their healthy broccoli... We salute you! @{sName} 🥦🍫"),
                ("⭐ Mahallenin Yıldız Marketi", "Neighborhood 5-Star Market", $"Tarladan rafa tazelik ve güler yüzlü hizmetle sizlerleyiz. Bizi tercih ettiğiniz için teşekkürler! @{sName} ⭐💖", $"Farm-to-shelf freshness with friendly neighborhood service. Thank you for choosing @{sName}! ⭐💖")
            };

            for (int i = 0; i < tweetOptions.Length; i++)
            {
                var opt = tweetOptions[i];
                GameObject itemObj = new GameObject("TweetOption_" + i);
                itemObj.transform.SetParent(contentObj.transform, false);

                LayoutElement le = itemObj.AddComponent<LayoutElement>();
                le.minHeight = 84f;
                le.preferredHeight = 84f;
                le.flexibleWidth = 1f;

                Image itemBg = itemObj.AddComponent<Image>();
                itemBg.sprite = UIStyleUtility.CreateOutlinePillSprite(700, 84, 12, 1, new Color(0.15f, 0.55f, 0.85f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

                // Text
                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(itemObj.transform, false);
                RectTransform tItemRect = txtObj.AddComponent<RectTransform>();
                tItemRect.anchoredPosition = new Vector2(-60f, 0f);
                tItemRect.sizeDelta = new Vector2(535f, 74f);

                Text itemTxt = txtObj.AddComponent<Text>();
                itemTxt.font = globalFont;
                string optTitle = LocalizationManager.L("OptTitle_" + i, opt.titleTr, opt.titleEn);
                string optBody = LocalizationManager.L("OptBody_" + i, opt.textTr, opt.textEn);
                itemTxt.text = $"<b><color=#40C4FF>{optTitle}</color></b>\n<size=14><color=#F0F6FC>{optBody}</color></size>";
                itemTxt.fontSize = 15;
                itemTxt.lineSpacing = 1.15f;
                itemTxt.alignment = TextAnchor.MiddleLeft;
                itemTxt.color = Color.white;

                // Post Button
                GameObject postBtnObj = new GameObject("PostBtn");
                postBtnObj.transform.SetParent(itemObj.transform, false);
                RectTransform pBtnRect = postBtnObj.AddComponent<RectTransform>();
                pBtnRect.anchoredPosition = new Vector2(280f, 0f);
                pBtnRect.sizeDelta = new Vector2(100f, 38f);

                Image pBg = postBtnObj.AddComponent<Image>();
                pBg.sprite = UIStyleUtility.CreateRoundedPillSprite(100, 38, 14, new Color(0.12f, 0.65f, 0.95f));
                Button pBtn = postBtnObj.AddComponent<Button>();
                pBtn.targetGraphic = pBg;

                string trText = opt.textTr;
                string enText = opt.textEn;
                pBtn.onClick.AddListener(() => {
                    if (SocialMediaManager.Instance != null)
                    {
                        SocialMediaManager.Instance.PostPlayerAnnouncement(trText, enText);
                    }
                    activeSocialTab = 2; // Twitlerim sekmesine geç!
                    Destroy(canvasObj);
                    RefreshSocialMediaViews();
                });

                Text pTxt = CreateTextInPanel(postBtnObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_PostShort", "PAYLAŞ", "POST"), 15, Color.white);
                pTxt.alignment = TextAnchor.MiddleCenter;
                pTxt.fontStyle = FontStyle.Bold;
            }

            closeBtnObj.transform.SetAsLastSibling();
        }

        private void Update()
        {
            if (tabletPopupRoot != null && tabletPopupRoot.activeSelf && workshopsAppView != null && workshopsAppView.gameObject.activeSelf && activeWorkshopTab == 1)
            {
                if (Time.unscaledTime - lastWorkshopLiveRefreshTime >= 1.0f)
                {
                    lastWorkshopLiveRefreshTime = Time.unscaledTime;
                    RefreshWorkshopsViews();
                }
            }
        }

        private void OnDestroy()
        {
            if (StaffManager.Instance != null)
            {
                StaffManager.Instance.OnStaffListChanged -= RefreshStoreManagementViews;
            }

            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.OnFinanceUpdated -= RefreshFinanceViews;
            }

            if (BankLoanManager.Instance != null)
            {
                BankLoanManager.Instance.OnBankLoansUpdated -= RefreshFinanceViews;
            }

            if (StockMarketManager.Instance != null)
            {
                StockMarketManager.Instance.OnStockMarketUpdated -= RefreshFinanceViews;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshAllPhoneDisplays;
            }

            if (tabletPopupRoot != null)
            {
                Destroy(tabletPopupRoot);
            }
        }
    }
}
