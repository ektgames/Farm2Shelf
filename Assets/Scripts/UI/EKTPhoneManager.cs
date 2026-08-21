using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

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
        private Transform socialMediaFeedContent;
        private int activeSocialTab = 0; // 0: Sana Özel (For You), 1: Yorumlar (Reviews), 2: Profilim (My Tweets)

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

            if (EnvironmentBuilder.Instance != null)
            {
                EnvironmentBuilder.Instance.OnStoreUpgraded += (lvl) => RefreshStoreManagementViews();
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= RefreshAllPhoneDisplays;
                LocalizationManager.Instance.OnLanguageChanged += RefreshAllPhoneDisplays;
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

            int curFarmTab = activeFarmTab;
            int curStoreTab = activeTab;
            int curFinanceTab = activeFinanceTab;
            int curShoppingCat = activeShoppingCategory;
            int curSocialTab = activeSocialTab;

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
                    default:
                        ShowHomeScreen();
                        break;
                }
            }
        }

        public void CreateBottomRightPhoneButtonOnCanvas(Transform parentCanvas = null)
        {
            if (phoneButtonObj != null) Destroy(phoneButtonObj);

            if (parentCanvas == null)
            {
                GameObject hudCanvas = GameObject.Find("Farm2Shelf_HUD_Canvas");
                if (hudCanvas != null) parentCanvas = hudCanvas.transform;
                else
                {
                    Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
                    if (mainCanvas != null) parentCanvas = mainCanvas.transform;
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
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (btnText.font == null) btnText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            globalFont = btnText.font;

            btnText.text = LocalizationManager.L("Btn_EktPhone", "📱 EKT TABLET", "📱 EKT PHONE");
            btnText.fontSize = 17;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = new Color(0.35f, 0.92f, 1.0f);
            btnText.raycastTarget = false;
        }

        private void OnPhoneTabButtonClicked()
        {
            if (isAnimating) return;
            if (ModalManager.IsModalOpen && !IsTabletOpen)
            {
                if (!ModalManager.IsAnyModalCanvasActive())
                {
                    ModalManager.SetModalOpen(false);
                }
                else
                {
                    return;
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
            popCanvas.sortingOrder = 350;

            CanvasScaler scaler = tabletPopupRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            tabletPopupRoot.AddComponent<GraphicRaycaster>();

            // Karartma Arka Plan Katmanı
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
            deviceFrameBg.raycastTarget = false;

            GameObject brandObj = new GameObject("Tablet_Brand_Header");
            brandObj.transform.SetParent(tabletBox.transform, false);

            RectTransform bRect = brandObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(0f, 285f);
            bRect.sizeDelta = new Vector2(300f, 35f);

            Text brandText = brandObj.AddComponent<Text>();
            brandText.font = globalFont;
            brandText.text = "EKT PHONE";
            brandText.fontSize = 20;
            brandText.fontStyle = FontStyle.Bold;
            brandText.alignment = TextAnchor.MiddleCenter;
            brandText.color = new Color(0.90f, 0.92f, 0.95f);
            brandText.raycastTarget = false;

            GameObject closeBtnObj = new GameObject("CloseButton_X");
            closeBtnObj.transform.SetParent(tabletBox.transform, false);

            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(430f, 285f);
            cRect.sizeDelta = new Vector2(36f, 36f);

            Image cBg = closeBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(36, 36, 18, new Color(0.90f, 0.20f, 0.20f, 0.95f));
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
            cxText.text = "X";
            cxText.fontSize = 18;
            cxText.fontStyle = FontStyle.Bold;
            cxText.alignment = TextAnchor.MiddleCenter;
            cxText.color = Color.white;
            cxText.raycastTarget = false;

            GameObject screenObj = new GameObject("Tablet_Screen");
            screenObj.transform.SetParent(tabletBox.transform, false);

            RectTransform screenRect = screenObj.AddComponent<RectTransform>();
            screenRect.anchoredPosition = new Vector2(0f, -15f);
            screenRect.sizeDelta = new Vector2(890f, 540f);

            Image screenBg = screenObj.AddComponent<Image>();
            screenBg.sprite = UIStyleUtility.CreateRoundedPillSprite(890, 540, 16, new Color(0.08f, 0.10f, 0.14f, 0.98f));
            screenBg.raycastTarget = false;

            CreateStatusBar(screenObj.transform);
            CreateHomeScreenView(screenObj.transform);
            CreateStoreManagementAppView(screenObj.transform);
            CreateFarmAppView(screenObj.transform);
            CreateShoppingAppView(screenObj.transform);
            CreateFinanceAppView(screenObj.transform);
            CreateSocialMediaAppView(screenObj.transform);
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
            tText.fontSize = 14;
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
            rText.fontSize = 14;
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
                LocalizationManager.L("App_SocialMedia", "SOSYAL MEDYA", "SOCIAL MEDIA")
            };
            string[] appIcons = new string[] { "🛒", "🌾", "🛍️", "💳", "𝕏" };
            Color[] appColors = new Color[] {
                new Color(0.20f, 0.70f, 0.95f),
                new Color(0.25f, 0.85f, 0.40f),
                new Color(0.95f, 0.40f, 0.55f),
                new Color(0.75f, 0.35f, 0.95f),
                new Color(0.12f, 0.65f, 0.95f)
            };

            for (int i = 0; i < 5; i++)
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
                });

                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(appObj.transform, false);
                RectTransform iRect = iconObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(0f, 18f);
                iRect.sizeDelta = new Vector2(100f, 55f);

                Text iText = iconObj.AddComponent<Text>();
                iText.font = globalFont;
                iText.text = appIcons[i];
                iText.fontSize = 38;
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
                lText.fontSize = (appIndex == 0) ? 12 : 14;
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
            bText.fontSize = 15;
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
            tText.fontSize = 20;
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
                Color borderClr = (i == 0) ? new Color(1.0f, 0.75f, 0.20f) : new Color(0.20f, 0.70f, 0.95f);
                tabBg.sprite = UIStyleUtility.CreateOutlinePillSprite(195, 40, 20, 2, borderClr, new Color(0.12f, 0.16f, 0.22f, 0.85f));
                tabBg.raycastTarget = true;

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
                tabText.fontSize = 15;
                tabText.fontStyle = FontStyle.Bold;
                tabText.alignment = TextAnchor.MiddleCenter;
                tabText.color = (i == 0) ? new Color(1.0f, 0.85f, 0.30f) : new Color(0.35f, 0.90f, 1.0f);
                tabText.raycastTarget = false;
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
            bText.fontSize = 15;
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
            tText.text = LocalizationManager.L("App_FinanceHeader", "💳 FİNANS VE GELİR GİDER", "💳 FINANCE & EARNINGS");
            tText.fontSize = 20;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.75f, 0.35f, 0.95f);
            tText.raycastTarget = false;

            // Sekme Butonları (Y = 170f, alt sınırı 150f)
            CreateFinanceTabs(viewObj.transform);

            // Sabit Üst Kontrol Barı (Arama Çubuğu + Otomatik Fiyat Ayarla Butonu, Y = 120f, alt sınırı 101f)
            GameObject financeProductsControlBarObj = new GameObject("FinanceProductsControlBar");
            financeProductsControlBarObj.transform.SetParent(viewObj.transform, false);

            RectTransform fpcRect = financeProductsControlBarObj.AddComponent<RectTransform>();
            fpcRect.anchoredPosition = new Vector2(0f, 115f);
            fpcRect.sizeDelta = new Vector2(850f, 36f);
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
            phText.fontSize = 14;
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
            inText.fontSize = 14;
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
            apRect.anchoredPosition = new Vector2(250f, 0f);
            apRect.sizeDelta = new Vector2(300f, 38f);

            Image apBg = autoPriceBtnObj.AddComponent<Image>();
            apBg.sprite = UIStyleUtility.CreateRoundedPillSprite(300, 38, 19, new Color(0.20f, 0.70f, 0.45f));
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
            apText.fontSize = 13;
            apText.fontStyle = FontStyle.Bold;
            apText.alignment = TextAnchor.MiddleCenter;
            apText.color = Color.white;
            apText.raycastTarget = false;

            // Scroll Viewport ve Content Kapları (Sekmelerin Altında 20px Temiz Havalandırma Mesafesi)
            financeProductsContent = CreateScrollableViewContainer(viewObj.transform, "FinanceProducts", new Vector2(0f, -80f), new Vector2(850f, 330f), out financeProductsViewportObj);
            financeSummaryContent = CreateScrollableViewContainer(viewObj.transform, "FinanceSummary", new Vector2(0f, -57.5f), new Vector2(850f, 375f), out financeSummaryViewportObj);
            financeHistoryContent = CreateScrollableViewContainer(viewObj.transform, "FinanceHistory", new Vector2(0f, -57.5f), new Vector2(850f, 375f), out financeHistoryViewportObj);
            financeLoansContent = CreateScrollableViewContainer(viewObj.transform, "FinanceLoans", new Vector2(0f, -57.5f), new Vector2(850f, 375f), out financeLoansViewportObj);
            financeStocksContent = CreateScrollableViewContainer(viewObj.transform, "FinanceStocks", new Vector2(0f, -57.5f), new Vector2(850f, 375f), out financeStocksViewportObj);

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
            tRect.anchoredPosition = new Vector2(0f, 175f);
            tRect.sizeDelta = new Vector2(850f, 38f);

            HorizontalLayoutGroup layout = tabsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;

            string[] tabs = new string[] {
                LocalizationManager.L("Tab_Products", "🏷️ Ürünler", "🏷️ Products"),
                LocalizationManager.L("Tab_Summary", "📊 Özet", "📊 Summary"),
                LocalizationManager.L("Tab_History", "📜 İşlem Geçmişi", "📜 History"),
                LocalizationManager.L("Tab_Loans", "🏛️ Banka Kredileri", "🏛️ Bank Loans"),
                LocalizationManager.L("Tab_Stocks", "📈 Borsa & Hisseler", "📈 Stock Market")
            };

            for (int i = 0; i < 5; i++)
            {
                int tabIndex = i;
                GameObject tabBtn = new GameObject("FinanceTab_" + i);
                tabBtn.transform.SetParent(tabsObj.transform, false);

                RectTransform tabRect = tabBtn.AddComponent<RectTransform>();
                tabRect.sizeDelta = new Vector2(162f, 40f);

                Image tabBg = tabBtn.AddComponent<Image>();
                tabBg.sprite = UIStyleUtility.CreateOutlinePillSprite(162, 40, 18, 2, new Color(0.75f, 0.35f, 0.95f), new Color(0.12f, 0.16f, 0.22f, 0.85f));
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
            bText.fontSize = 15;
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
            tText.fontSize = 17;
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
            boText.fontSize = 14;
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
            phText.fontSize = 14;
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
            inText.fontSize = 14;
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
            headerCartButtonText.fontSize = 14;
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
            shoppingCategoryHeaderTitle.fontSize = 22;
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
            shoppingCategoryHeaderSub.fontSize = 16;
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
            shoppingCartSummaryText.fontSize = 16;
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
            Text chTxt = CreateTextInPanel(checkoutBtnObj.transform, Vector2.zero, Vector2.one, checkoutLabel, 14, Color.white);
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
        }

        private void ShowStoreManagementApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(true);

            activeTab = 0;
            RefreshStoreManagementViews();
        }

        private void ShowFinanceApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(true);

            activeFinanceTab = 0;
            RefreshFinanceViews();
        }

        private void ShowFarmApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(true);

            activeFarmTab = 0;
            RefreshFarmViews();
        }

        private void ShowShoppingApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(true);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(false);

            activeShoppingCategory = 0;
            RefreshShoppingViews();
        }

        private void ShowSocialMediaApp()
        {
            if (homeScreenView != null) homeScreenView.gameObject.SetActive(false);
            if (storeMgmtAppView != null) storeMgmtAppView.gameObject.SetActive(false);
            if (financeAppView != null) financeAppView.gameObject.SetActive(false);
            if (farmAppView != null) farmAppView.gameObject.SetActive(false);
            if (shoppingAppView != null) shoppingAppView.gameObject.SetActive(false);
            if (socialMediaAppView != null) socialMediaAppView.gameObject.SetActive(true);

            activeSocialTab = 0;
            RefreshSocialMediaViews();
        }

        private void RefreshShoppingViews()
        {
            RenderShoppingCategoryList();
            RenderShoppingCategoryContent();
        }

        private void RenderShoppingCategoryList()
        {
            if (shoppingCategoryContent == null) return;
            foreach (Transform child in shoppingCategoryContent) Destroy(child.gameObject);

            Color accentColor = new Color(0.95f, 0.40f, 0.55f);

            string[] categories = GetShoppingCategories();

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

                Text catText = CreateTextInPanel(catBtn.transform, Vector2.zero, Vector2.one, categories[i], 14, isActive ? Color.white : new Color(0.80f, 0.85f, 0.90f));
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
                LocalizationManager.L("Cat_Renovation", "🔨 Tadilat", "🔨 Renovation")
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
                if (shoppingCartSummaryPanelObj != null) shoppingCartSummaryPanelObj.SetActive(activeShoppingCategory == 2);

                if (activeShoppingCategory == 4)
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

        private void RenderWholesaleProductList()
        {
            if (furnitureListContent == null) return;
            foreach (Transform child in furnitureListContent) Destroy(child.gameObject);

            int currentLevel = (Farm2Shelf.Environment.EnvironmentBuilder.Instance != null)
                ? Farm2Shelf.Environment.EnvironmentBuilder.Instance.CurrentUpgradeLevel
                : 1;

            List<WholesaleProductDef> items = WholesaleDatabase.GetAllProducts();

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

                Text emptyTxt = CreateTextInPanel(emptyMsgObj.transform, Vector2.zero, Vector2.one, $"🔍 '{currentShoppingSearchQuery}' araması için Toptancı sekmesinde ürün bulunamadı.", 15, Color.gray);
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

                Text titleText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 22f), new Vector2(300f, 24f), $"{def.iconEmoji} {def.LocalizedName} (50)", 16, Color.white);
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleLeft;

                string priceInfoFmt = LocalizationManager.L("Wholesale_PriceInfoFmt", "Toptan Koli Alış: {0:N0}C ({1:N0}C/Birim) | Kâr: +{2:N0}C (%20)", "Wholesale Pack Cost: {0:N0}C ({1:N0}C/Pcs) | Profit: +{2:N0}C (20%)");
                string priceInfo = string.Format(priceInfoFmt, def.TotalPackCost, def.wholesaleUnitPrice, def.TotalPackProfit);
                Text priceText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 0f), new Vector2(300f, 20f), priceInfo, 14, new Color(0.95f, 0.85f, 0.30f));
                priceText.alignment = TextAnchor.MiddleLeft;

                string badgeUnlockedFmt = LocalizationManager.L("Wholesale_UnlockedBadge", "✅ Seviye {0} | {1} (50 Adet)", "✅ Level {0} | {1} (50 Pcs)");
                string badgeLockedFmt = LocalizationManager.L("Wholesale_LockedBadge", "🔒 Seviye {0} Gereklidir | {1}", "🔒 Requires Level {0} | {1}");
                string badgeText = isUnlocked ? string.Format(badgeUnlockedFmt, def.requiredLevel, def.GetTargetShelfText()) : string.Format(badgeLockedFmt, def.requiredLevel, def.GetTargetShelfText());
                Color badgeColor = isUnlocked ? new Color(0.30f, 0.85f, 0.45f) : new Color(0.95f, 0.45f, 0.35f);

                Text subText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -20f), new Vector2(300f, 20f), badgeText, 13, badgeColor);
                subText.alignment = TextAnchor.MiddleLeft;

                // Sağ Kontrol Alanı (Koli Ekle / Adet / Kilitli)
                GameObject ctrlPanel = new GameObject("CtrlPanel");
                ctrlPanel.transform.SetParent(cardObj.transform, false);
                RectTransform cpRect = ctrlPanel.AddComponent<RectTransform>();
                cpRect.anchoredPosition = new Vector2(190f, 0f);
                cpRect.sizeDelta = new Vector2(110f, 50f);

                string targetProdId = def.id;

                if (isUnlocked)
                {
                    if (inCartCount > 0)
                    {
                        // "-" Butonu
                        GameObject minusBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(-35f, 0f), new Vector2(32f, 32f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                            if (wholesaleCart.ContainsKey(targetProdId))
                            {
                                wholesaleCart[targetProdId]--;
                                if (wholesaleCart[targetProdId] <= 0) wholesaleCart.Remove(targetProdId);
                            }
                            RenderWholesaleProductList();
                            UpdateCartSummary();
                        }, 20);

                        // Adet Göstergesi
                        Text countTxt = CreateTextInPanel(ctrlPanel.transform, new Vector2(0f, 0f), new Vector2(30f, 32f), inCartCount.ToString(), 17, Color.white);
                        countTxt.fontStyle = FontStyle.Bold;
                        countTxt.alignment = TextAnchor.MiddleCenter;

                        // "+" Butonu
                        GameObject plusBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(35f, 0f), new Vector2(32f, 32f), "+", new Color(0.30f, 0.75f, 0.40f), () => {
                            if (!wholesaleCart.ContainsKey(targetProdId)) wholesaleCart[targetProdId] = 0;
                            wholesaleCart[targetProdId]++;
                            RenderWholesaleProductList();
                            UpdateCartSummary();
                        }, 20);
                    }
                    else
                    {
                        // "+ Koli Ekle" Butonu
                        string btnAddPackLabel = LocalizationManager.L("Btn_AddPack", "+ Koli Ekle", "+ Add Pack");
                        GameObject addBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(0f, 0f), new Vector2(100f, 34f), btnAddPackLabel, new Color(0.95f, 0.55f, 0.20f), () => {
                            wholesaleCart[targetProdId] = 1;
                            RenderWholesaleProductList();
                            UpdateCartSummary();
                        });
                    }
                }
                else
                {
                    // Kilitli Buton
                    string lockTextStr = LocalizationManager.L("Btn_LockedItem", "🔒 Kilitli", "🔒 Locked");
                    GameObject lockBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(0f, 0f), new Vector2(100f, 34f), lockTextStr, new Color(0.35f, 0.35f, 0.40f), null);
                }
            }

            UpdateCartSummary();
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
                Text titleText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 18f), new Vector2(310f, 24f), $"<b>{def.LocalizedName}</b>  <color=#00E676>{string.Format(inStockFmt, ownedCount)}</color>", 16, canBuy ? Color.white : Color.gray);
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleLeft;

                string statusFmt = LocalizationManager.L("Seed_StatusFmt", "• Büyüme: {0} Gün • Seviye: {1} • 10'lu Paket: {2:N0}C", "• Growth: {0} Days • Level: {1} • 10-Pack: {2:N0}C");
                string statusDetails = string.Format(statusFmt, def.growthDays, def.requiredLevel, def.packPrice);
                Text descText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -6f), new Vector2(310f, 22f), statusDetails, 14, canBuy ? new Color(0.85f, 0.90f, 0.95f) : Color.gray);
                descText.alignment = TextAnchor.MiddleLeft;

                string seasonName = (TimeManager.Instance != null) ? TimeManager.Instance.GetLocalizedSeasonName(def.season) : def.season.ToString();
                string seasonInFmt = LocalizationManager.L("Seed_SeasonIn", "✅ Mevsim: {0}", "✅ Season: {0}");
                string seasonOutFmt = LocalizationManager.L("Seed_SeasonOut", "🔒 Mevsim Dışı ({0})", "🔒 Out of Season ({0})");
                string seasonBadgeStr = isMatchingSeason ? string.Format(seasonInFmt, seasonName) : string.Format(seasonOutFmt, seasonName);
                Color seasonBadgeCol = isMatchingSeason ? new Color(0.35f, 0.85f, 0.45f) : new Color(0.95f, 0.45f, 0.35f);
                Text badgeText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -24f), new Vector2(310f, 20f), seasonBadgeStr, 13, seasonBadgeCol);
                badgeText.alignment = TextAnchor.MiddleLeft;

                // 3. Sağ Kontrol Alanı (Sepete Ekle / - 1 + / Kilitli)
                GameObject ctrlPanel = new GameObject("CtrlPanel");
                ctrlPanel.transform.SetParent(cardObj.transform, false);
                RectTransform cpRect = ctrlPanel.AddComponent<RectTransform>();
                cpRect.anchoredPosition = new Vector2(190f, 0f);
                cpRect.sizeDelta = new Vector2(110f, 40f);

                if (canBuy)
                {
                    int inCartCount = seedCart.ContainsKey(def.id) ? seedCart[def.id] : 0;
                    string targetSeedId = def.id;

                    if (inCartCount > 0)
                    {
                        // "-" Butonu
                        GameObject minusBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(-35f, 0f), new Vector2(32f, 32f), "-", new Color(0.85f, 0.25f, 0.25f), () => {
                            if (seedCart.ContainsKey(targetSeedId))
                            {
                                seedCart[targetSeedId]--;
                                if (seedCart[targetSeedId] <= 0) seedCart.Remove(targetSeedId);
                            }
                            RenderSeedProductList();
                            UpdateCartSummary();
                        }, 20);

                        // Adet Göstergesi (Paket)
                        Text countTxt = CreateTextInPanel(ctrlPanel.transform, new Vector2(0f, 0f), new Vector2(30f, 32f), inCartCount.ToString(), 17, Color.white);
                        countTxt.fontStyle = FontStyle.Bold;
                        countTxt.alignment = TextAnchor.MiddleCenter;

                        // "+" Butonu
                        GameObject plusBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(35f, 0f), new Vector2(32f, 32f), "+", new Color(0.30f, 0.75f, 0.40f), () => {
                            if (!seedCart.ContainsKey(targetSeedId)) seedCart[targetSeedId] = 0;
                            seedCart[targetSeedId]++;
                            RenderSeedProductList();
                            UpdateCartSummary();
                        }, 20);
                    }
                    else
                    {
                        // "+ Sepete Ekle" Butonu
                        string btnAddSeedLabel = LocalizationManager.L("Btn_AddToCart", "+ Sepete Ekle", "+ Add to Cart");
                        GameObject addBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(0f, 0f), new Vector2(105f, 34f), btnAddSeedLabel, new Color(0.20f, 0.75f, 0.35f), () => {
                            seedCart[targetSeedId] = 1;
                            RenderSeedProductList();
                            UpdateCartSummary();
                        });
                    }
                }
                else
                {
                    string outSeasonStr = LocalizationManager.L("Btn_OutOfSeason", "🔒 Mevsim Dışı", "🔒 Off-Season");
                    string reqLvlFmt = LocalizationManager.L("Btn_ReqLevel", "🔒 Seviye {0}", "🔒 Level {0}");
                    string lockTxt = !isMatchingSeason ? outSeasonStr : string.Format(reqLvlFmt, def.requiredLevel);
                    GameObject lockBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(0f, 0f), new Vector2(105f, 34f), lockTxt, new Color(0.35f, 0.35f, 0.40f), null);
                }
            }
            UpdateCartSummary();
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

            Text wTxt = CreateTextInPanel(wallTabBtn.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Sub_Walls", "🎨 Duvarlar", "🎨 Walls"), 15, isWallActive ? Color.white : new Color(0.75f, 0.80f, 0.85f));
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

            Text fTxt = CreateTextInPanel(floorTabBtn.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Sub_Floors", "🧱 Zemin", "🧱 Floors"), 15, isFloorActive ? Color.white : new Color(0.75f, 0.80f, 0.85f));
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

                Text nameText = CreateTextInPanel(infoObj.transform, Vector2.zero, Vector2.one, def.Name, 16, Color.white);
                nameText.fontStyle = FontStyle.Bold;

                string lvlFmt = LocalizationManager.L("Renov_ReqLvl", "Seviye {0} Gerektirir", "Requires Level {0}");
                Text lvlText = CreateTextInPanel(infoObj.transform, Vector2.zero, Vector2.one, string.Format(lvlFmt, def.requiredLevel), 13, isUnlocked ? new Color(0.40f, 0.90f, 0.50f) : new Color(0.95f, 0.40f, 0.40f));

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

                Text priceText = CreateTextInPanel(ctrlObj.transform, Vector2.zero, Vector2.one, $"<b>{def.price:N0}C</b>", 16, new Color(0.30f, 0.90f, 1.0f));
                priceText.alignment = TextAnchor.MiddleRight;
                RectTransform prRt = priceText.GetComponent<RectTransform>();
                prRt.sizeDelta = new Vector2(55f, 35f);

                if (isUnlocked)
                {
                    string useTxt = LocalizationManager.L("Btn_ApplyRenovation", "KULLAN", "APPLY");
                    GameObject applyBtnObj = CreateButtonInPanel(ctrlObj.transform, Vector2.zero, new Vector2(110f, 38f), useTxt, new Color(0.18f, 0.75f, 0.35f), () => {
                        ApplyRenovationItem(def);
                    });
                }
                else
                {
                    string lockTxt = string.Format(LocalizationManager.L("Btn_LockedFmt", "🔒 Lv.{0}", "🔒 Lv.{0}"), def.requiredLevel);
                    GameObject lockBtnObj = CreateButtonInPanel(ctrlObj.transform, Vector2.zero, new Vector2(110f, 38f), lockTxt, new Color(0.35f, 0.35f, 0.40f), null);
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
                string catTitle = (cat == FurnitureCategory.Furniture) ? "Mobilyalar" : "Dekorasyonlar";
                GameObject emptyMsgObj = new GameObject("SearchEmptyMsg");
                emptyMsgObj.transform.SetParent(furnitureListContent, false);
                LayoutElement el = emptyMsgObj.AddComponent<LayoutElement>();
                el.minHeight = 120f;
                el.preferredHeight = 120f;

                Text emptyTxt = CreateTextInPanel(emptyMsgObj.transform, Vector2.zero, Vector2.one, $"🔍 '{currentShoppingSearchQuery}' araması için {catTitle} sekmesinde ürün bulunamadı.", 15, Color.gray);
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

                Text titleText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, 15f), new Vector2(300f, 24f), $"{def.iconEmoji} {def.LocalizedName} ({def.price:N0} Cr)", 16, Color.white);
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.MiddleLeft;

                string badgeUnlockedFmt = LocalizationManager.L("Furn_UnlockedBadge", "✅ Seviye {0} | {1}", "✅ Level {0} | {1}");
                string badgeLockedFmt = LocalizationManager.L("Furn_LockedBadge", "🔒 Seviye {0} Gereklidir | {1}", "🔒 Requires Level {0} | {1}");
                string badgeText = isUnlocked ? string.Format(badgeUnlockedFmt, def.requiredLevel, def.GetZoneText()) : string.Format(badgeLockedFmt, def.requiredLevel, def.GetZoneText());
                Color badgeColor = isUnlocked ? new Color(0.30f, 0.85f, 0.45f) : new Color(0.95f, 0.45f, 0.35f);

                Text subText = CreateTextInPanel(infoPanel.transform, new Vector2(0f, -12f), new Vector2(300f, 22f), badgeText, 14, badgeColor);
                subText.alignment = TextAnchor.MiddleLeft;

                // Sağ Kontrol Alanı (Sepete Ekle / Adet / Kilitli)
                GameObject ctrlPanel = new GameObject("CtrlPanel");
                ctrlPanel.transform.SetParent(cardObj.transform, false);
                RectTransform cpRect = ctrlPanel.AddComponent<RectTransform>();
                cpRect.anchoredPosition = new Vector2(190f, 0f);
                cpRect.sizeDelta = new Vector2(110f, 50f);

                if (isUnlocked)
                {
                    if (inCartCount > 0)
                    {
                        // "-" Butonu
                        GameObject minusBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(-35f, 0f), new Vector2(32f, 32f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                            if (shoppingCart.ContainsKey(def.type))
                            {
                                shoppingCart[def.type]--;
                                if (shoppingCart[def.type] <= 0) shoppingCart.Remove(def.type);
                            }
                            RenderFurnitureList(cat);
                            UpdateCartSummary();
                        }, 20);

                        // Adet Göstergesi
                        Text countTxt = CreateTextInPanel(ctrlPanel.transform, new Vector2(0f, 0f), new Vector2(30f, 32f), inCartCount.ToString(), 17, Color.white);
                        countTxt.fontStyle = FontStyle.Bold;
                        countTxt.alignment = TextAnchor.MiddleCenter;

                        // "+" Butonu
                        GameObject plusBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(35f, 0f), new Vector2(32f, 32f), "+", new Color(0.30f, 0.75f, 0.40f), () => {
                            shoppingCart[def.type]++;
                            RenderFurnitureList(cat);
                            UpdateCartSummary();
                        }, 20);
                    }
                    else
                    {
                        // "Sepete Ekle" Butonu
                        string btnAddToCartLabel = LocalizationManager.L("Btn_AddToCart", "+ Sepete Ekle", "+ Add to Cart");
                        GameObject addBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(0f, 0f), new Vector2(100f, 34f), btnAddToCartLabel, new Color(0.95f, 0.40f, 0.55f), () => {
                            shoppingCart[def.type] = 1;
                            RenderFurnitureList(cat);
                            UpdateCartSummary();
                        });
                    }
                }
                else
                {
                    // Kilitli Buton
                    string lockItemStr = LocalizationManager.L("Btn_LockedItem", "🔒 Kilitli", "🔒 Locked");
                    GameObject lockBtn = CreateButtonInPanel(ctrlPanel.transform, new Vector2(0f, 0f), new Vector2(100f, 34f), lockItemStr, new Color(0.35f, 0.35f, 0.40f), null);
                }
            }

            UpdateCartSummary();
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
                WholesaleProductDef def = WholesaleDatabase.GetProductById(kvp.Key);
                if (def != null)
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

            // Kamyon Yolda Kontrolü (Toptancı ürünleri için)
            if (orderWholesale.Count > 0 && isAnyTruckActive)
            {
                ModalManager.ShowModal("Teslimat Noktası Dolu! ⚠️", "Şu anda yolda veya teslimat noktasında aktif bir kamyon (Toptancı veya Çiftlik Kamyonu) bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni toptan sipariş verilemez.", "Tamam");
                return;
            }

            // Bakiye Kontrolü
            int currentBalance = (EconomyManager.Instance != null) 
                ? EconomyManager.Instance.Credits 
                : ((FinanceManager.Instance != null) ? FinanceManager.Instance.CurrentBalance : 10000);

            if (currentBalance < totalCost)
            {
                ModalManager.ShowModal("Yetersiz Bakiye ⚠️", $"Siparişi tamamlamak için {totalCost:N0}C gereklidir!\nMevcut Bakiyeniz: {currentBalance:N0}C.", "Tamam");
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

            // Mobilya Siparişlerini Palete Gönder
            if (orderFurniture.Count > 0 && FurnitureDeliveryManager.Instance != null)
            {
                FurnitureDeliveryManager.Instance.AddOrdersToPallet(orderFurniture);
                if (TutorialManager.Instance != null)
                {
                    foreach (var kvp in shoppingCart)
                    {
                        TutorialManager.Instance.NotifyFurnitureItemPurchased(kvp.Key, kvp.Value);
                    }
                }
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

            string btnGreat = LocalizationManager.L("Btn_Great", "Harika!", "Great!");
            if (orderWholesale.Count > 0)
            {
                string title = LocalizationManager.L("Modal_TruckDispatched_Title", "Toptancı Kamyonu Yola Çıktı! 🚛", "Wholesaler Truck Dispatched! 🚛");
                string bodyFmt = LocalizationManager.L("Modal_TruckDispatched_Body", "Toplam {0} kalem siparişiniz alındı!\n\nÖzel kapalı kasa kamyon teslimatı kapıya ulaştırıyor.", "Your order of {0} items has been received!\n\nA dedicated box truck is delivering your goods to the loading dock.");
                ModalManager.ShowModal(title, string.Format(bodyFmt, totalItems), btnGreat);
            }
            else
            {
                string title = LocalizationManager.L("Modal_OrderReceived_Title", "Sipariş Alındı! 📦", "Order Placed! 📦");
                string bodyFmt = LocalizationManager.L("Modal_OrderReceived_Body", "Toplam {0} kalem siparişiniz başarıyla alındı ve ödemesi yapıldı!\n\nSatın aldığınız tohumlar ahır kilerinize eklenmiştir.", "Your order of {0} items was successfully placed and paid!\n\nPurchased seeds have been added to your barn storage.");
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
            canvas.sortingOrder = 500; // TABLET EKRANININ (300) ÖNÜNE ÇIKAR

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
            Text tText = CreateTextInPanel(boxObj.transform, new Vector2(0f, 205f), new Vector2(580f, 40f), LocalizationManager.L("Cart_ModalTitle", "🛒 EKT SHOPPING SEPETİM VE ÖDEME", "🛒 EKT SHOPPING MY CART & CHECKOUT"), 24, new Color(0.95f, 0.45f, 0.60f));
            tText.alignment = TextAnchor.MiddleCenter;

            // Kapat (X) Butonu
            GameObject closeBtn = CreateButtonInPanel(boxObj.transform, new Vector2(285f, 205f), new Vector2(36f, 36f), "X", new Color(0.85f, 0.25f, 0.25f), () => {
                Destroy(canvasObj);
            });

            // Bakiye ve Bilgi Paneli
            int currentBalance = (FinanceManager.Instance != null) ? FinanceManager.Instance.CurrentBalance : 500000;
            string balFmt = LocalizationManager.L("Cart_BalanceFmt", "💰 Mevcut Bakiyeniz: {0:N0}C", "💰 Current Balance: {0:N0}C");
            Text balText = CreateTextInPanel(boxObj.transform, new Vector2(0f, 165f), new Vector2(580f, 30f), string.Format(balFmt, currentBalance), 16, new Color(0.30f, 0.85f, 0.50f));
            balText.alignment = TextAnchor.MiddleCenter;

            // İçerik ScrollView
            Transform cartContent = CreateScrollableViewContainer(boxObj.transform, "CartItemList", new Vector2(0f, 0f), new Vector2(590f, 260f), out Transform viewportObj);
            VerticalLayoutGroup vLayout = cartContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 6f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;

            int totalItems = 0;
            int totalCost = 0;

            if (shoppingCart.Count == 0 && wholesaleCart.Count == 0 && seedCart.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyMsg");
                emptyObj.transform.SetParent(cartContent, false);
                LayoutElement el = emptyObj.AddComponent<LayoutElement>();
                el.minHeight = 120f;
                el.preferredHeight = 120f;

                Text emptyTxt = CreateTextInPanel(emptyObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Cart_EmptyMsg", "🛒 Sepetiniz şu anda boş!\nKatalogdan ürün seçerek sepete ekleyebilirsiniz.", "🛒 Your cart is currently empty!\nYou can add items from the catalog."), 16, Color.gray);
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

                    Text nameTxt = CreateTextInPanel(itemRow.transform, new Vector2(-155f, 0f), new Vector2(230f, 40f), $"{def.iconEmoji} {def.LocalizedName}", 16, Color.white);
                    nameTxt.alignment = TextAnchor.MiddleLeft;

                    Text priceTxt = CreateTextInPanel(itemRow.transform, new Vector2(20f, 0f), new Vector2(150f, 40f), $"{count} x {def.price:N0} = {itemTotalCost:N0} Cr", 15, new Color(0.95f, 0.80f, 0.30f));
                    priceTxt.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(142f, 0f), new Vector2(30f, 30f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                        shoppingCart[fType]--;
                        if (shoppingCart[fType] <= 0) shoppingCart.Remove(fType);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        OpenCartModal();
                    }, 20);

                    Text countLabel = CreateTextInPanel(itemRow.transform, new Vector2(174f, 0f), new Vector2(24f, 30f), count.ToString(), 16, Color.white);
                    countLabel.fontStyle = FontStyle.Bold;
                    countLabel.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(206f, 0f), new Vector2(30f, 30f), "+", new Color(0.28f, 0.75f, 0.40f), () => {
                        shoppingCart[fType]++;
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        OpenCartModal();
                    }, 20);

                    // Ürünü Sepetten Tamamen Çıkarma Butonu (X)
                    CreateButtonInPanel(itemRow.transform, new Vector2(252f, 0f), new Vector2(30f, 30f), "✕", new Color(0.82f, 0.22f, 0.22f), () => {
                        shoppingCart.Remove(fType);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        OpenCartModal();
                    }, 17);
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
                    Text nameTxt = CreateTextInPanel(itemRow.transform, new Vector2(-155f, 0f), new Vector2(230f, 40f), $"{def.iconEmoji} {def.LocalizedName} ({pack50Label})", 16, Color.white);
                    nameTxt.alignment = TextAnchor.MiddleLeft;

                    Text priceTxt = CreateTextInPanel(itemRow.transform, new Vector2(20f, 0f), new Vector2(150f, 40f), $"{count} Koli = {itemTotalCost:N0} Cr", 15, new Color(0.95f, 0.75f, 0.30f));
                    priceTxt.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(142f, 0f), new Vector2(30f, 30f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                        wholesaleCart[pId]--;
                        if (wholesaleCart[pId] <= 0) wholesaleCart.Remove(pId);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        OpenCartModal();
                    }, 20);

                    Text countLabel = CreateTextInPanel(itemRow.transform, new Vector2(174f, 0f), new Vector2(24f, 30f), count.ToString(), 16, Color.white);
                    countLabel.fontStyle = FontStyle.Bold;
                    countLabel.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(206f, 0f), new Vector2(30f, 30f), "+", new Color(0.28f, 0.75f, 0.40f), () => {
                        wholesaleCart[pId]++;
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        OpenCartModal();
                    }, 20);

                    // Ürünü Sepetten Tamamen Çıkarma Butonu (X)
                    CreateButtonInPanel(itemRow.transform, new Vector2(252f, 0f), new Vector2(30f, 30f), "✕", new Color(0.82f, 0.22f, 0.22f), () => {
                        wholesaleCart.Remove(pId);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        OpenCartModal();
                    }, 17);
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
                    Text nameTxt = CreateTextInPanel(itemRow.transform, new Vector2(-155f, 0f), new Vector2(230f, 40f), $"{def.iconEmoji} {def.LocalizedName} ({pack10Label})", 16, Color.white);
                    nameTxt.alignment = TextAnchor.MiddleLeft;

                    Text priceTxt = CreateTextInPanel(itemRow.transform, new Vector2(20f, 0f), new Vector2(150f, 40f), $"{count} Pk = {itemTotalCost:N0} Cr", 15, new Color(0.35f, 0.85f, 0.45f));
                    priceTxt.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(142f, 0f), new Vector2(30f, 30f), "-", new Color(0.85f, 0.30f, 0.30f), () => {
                        seedCart[sId]--;
                        if (seedCart[sId] <= 0) seedCart.Remove(sId);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        OpenCartModal();
                    }, 20);

                    Text countLabel = CreateTextInPanel(itemRow.transform, new Vector2(174f, 0f), new Vector2(24f, 30f), count.ToString(), 16, Color.white);
                    countLabel.fontStyle = FontStyle.Bold;
                    countLabel.alignment = TextAnchor.MiddleCenter;

                    CreateButtonInPanel(itemRow.transform, new Vector2(206f, 0f), new Vector2(30f, 30f), "+", new Color(0.28f, 0.75f, 0.40f), () => {
                        seedCart[sId]++;
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        OpenCartModal();
                    }, 20);

                    // Ürünü Sepetten Tamamen Çıkarma Butonu (X)
                    CreateButtonInPanel(itemRow.transform, new Vector2(252f, 0f), new Vector2(30f, 30f), "✕", new Color(0.82f, 0.22f, 0.22f), () => {
                        seedCart.Remove(sId);
                        UpdateCartSummary();
                        RenderShoppingCategoryContent();
                        OpenCartModal();
                    }, 17);
                }
            }

            // Alt Ödeme Alanı (Footer)
            GameObject footerObj = new GameObject("Cart_Footer");
            footerObj.transform.SetParent(boxObj.transform, false);
            RectTransform ftRect = footerObj.AddComponent<RectTransform>();
            ftRect.anchoredPosition = new Vector2(0f, -195f);
            ftRect.sizeDelta = new Vector2(590f, 50f);

            string totalFmt = LocalizationManager.L("Cart_TotalCostFmt", "Toplam Tutar: {0:N0}C", "Total Cost: {0:N0}C");
            Text totalTxt = CreateTextInPanel(footerObj.transform, new Vector2(-130f, 0f), new Vector2(300f, 40f), string.Format(totalFmt, totalCost), 19, new Color(0.95f, 0.85f, 0.30f));
            totalTxt.alignment = TextAnchor.MiddleLeft;

            string payBtnLabel = LocalizationManager.L("Btn_PlaceOrderPay", "💳 ÖDEME YAP VE SİPARİŞ VER", "💳 PLACE ORDER & PAY");
            GameObject payBtn = CreateButtonInPanel(footerObj.transform, new Vector2(165f, 0f), new Vector2(230f, 44f), payBtnLabel, new Color(0.20f, 0.75f, 0.35f), () => {
                Destroy(canvasObj);
                CheckoutShoppingCart();
            });
            payBtn.GetComponent<Button>().interactable = (totalItems > 0);
        }

        private void RefreshFarmViews()
        {
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
                iText.fontSize = 15;
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
                btText.fontSize = 14;
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
                "🏛️ <b>BANKA KREDİLERİ YÖNETİMİ</b>   |   Aktif Krediler: <b>{0} Adet</b>   |   Kalan Toplam Borç: <color=#FFD54F>{1:N0}C</color>\n<size=12><color=#B0BEC5>Gece yarısında (00:00) günlük taksitler otomatik tahsil edilir. İsterseniz kredilerinizi erken ödeyip kapatabilirsiniz.</color></size>",
                "🏛️ <b>BANK LOANS MANAGEMENT</b>   |   Active Loans: <b>{0} Active</b>   |   Remaining Total Debt: <color=#FFD54F>{1:N0}C</color>\n<size=12><color=#B0BEC5>Daily installments are automatically collected at midnight (00:00). You can payoff loans early if desired.</color></size>"
            );
            Text sText = CreateTextInPanel(summaryCard.transform, Vector2.zero, Vector2.one, string.Format(loanSummaryFmt, activeCount, totalDebt), 14, Color.white);
            sText.alignment = TextAnchor.MiddleCenter;

            // 2. AKTİF KREDİLER LİSTESİ (Varsa)
            if (loanMgr != null && activeCount > 0)
            {
                GameObject activeSectionHeader = new GameObject("ActiveLoansHeader");
                activeSectionHeader.transform.SetParent(financeLoansContent, false);
                RectTransform ahRect = activeSectionHeader.AddComponent<RectTransform>();
                ahRect.sizeDelta = new Vector2(820f, 25f);
                Text ahTxt = CreateTextInPanel(activeSectionHeader.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Header_ActiveLoans", "<b>🔴 AKTİF ÖDENMEKTE OLAN KREDİLERİNİZ:</b>", "<b>🔴 YOUR ACTIVE LOANS BEING REPAID:</b>"), 15, new Color(1.0f, 0.80f, 0.30f));
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
                    iTxt.fontSize = 14;
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

                    Text pTxt = CreateTextInPanel(payoffBtn.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_PayoffEarly", "⚡ ERKEN KAPAT", "⚡ PAYOFF EARLY"), 13, Color.white);
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
            Text ohTxt = CreateTextInPanel(offersHeader.transform, Vector2.zero, Vector2.one, string.Format(offersHeaderFmt, storeLevel), 15, new Color(0.25f, 0.85f, 0.45f));
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
                iTxt.fontSize = 14;
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

                Text tTxt = CreateTextInPanel(takeBtn.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_TakeLoan", "💵 KREDİ ÇEK", "💵 CLAIM LOAN"), 14, Color.white);
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
                Text sbText = CreateTextInPanel(stockBtnObj.transform, Vector2.zero, Vector2.one, $"<b>{stock.tickerSymbol}</b>  |  {stock.LocalizedCompanyName}\n{string.Format(priceLabelFmt, stock.currentPrice)}   <color=#{ColorUtility.ToHtmlStringRGB(changeColor)}>{stock.PriceChangePercent:+0.00;-0.00}% {arrow}</color>", 12, Color.white);
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

            string lastPriceFmt = LocalizationManager.L("Stock_LastPriceFmt", "Son Fiyat: <size=15><b>{0:F2}C</b></size>", "Last Price: <size=15><b>{0:F2}C</b></size>");
            Text hTxt = CreateTextInPanel(headerObj.transform, Vector2.zero, Vector2.one, $"📈 <b>{targetStock.LocalizedCompanyName} ({targetStock.tickerSymbol})</b>\n<size=12><color=#B0BEC5>{targetStock.LocalizedCategory}</color></size>   |   {string.Format(lastPriceFmt, targetStock.currentPrice)}  <color=#{ColorUtility.ToHtmlStringRGB(signColor)}><b>({targetStock.PriceChangePercent:+0.00;-0.00}% {signArrow})</b></color>", 14, Color.white);
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

            Text pTxt = CreateTextInPanel(portfolioObj.transform, Vector2.zero, Vector2.one, "", 13, Color.white);
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

                Text qTxt = CreateTextInPanel(qBtnObj.transform, Vector2.zero, Vector2.one, $"{qtyVal}", 12, Color.white);
                qTxt.alignment = TextAnchor.MiddleCenter;
                qTxt.fontStyle = FontStyle.Bold;
            }

            // HİSSE AL (Yeşil) - Çakışmasız Konumlandırma (X = +60f)
            GameObject buyBtnObj = new GameObject("BuySharesBtn");
            buyBtnObj.transform.SetParent(tradeBarObj.transform, false);
            RectTransform buyRect = buyBtnObj.AddComponent<RectTransform>();
            buyRect.anchoredPosition = new Vector2(60f, 0f);
            buyRect.sizeDelta = new Vector2(115f, 36f);

            Image buyBg = buyBtnObj.AddComponent<Image>();
            buyBg.sprite = UIStyleUtility.CreateRoundedPillSprite(115, 36, 18, new Color(0.15f, 0.75f, 0.40f));
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
            Text buyTxt = CreateTextInPanel(buyBtn.transform, Vector2.zero, Vector2.one, string.Format(buySharesFmt, stockTradeQuantity), 12, Color.white);
            buyTxt.alignment = TextAnchor.MiddleCenter;
            buyTxt.fontStyle = FontStyle.Bold;

            // HİSSE SAT (Kırmızı) - Çakışmasız Konumlandırma (X = +180f)
            GameObject sellBtnObj = new GameObject("SellSharesBtn");
            sellBtnObj.transform.SetParent(tradeBarObj.transform, false);
            RectTransform sellRect = sellBtnObj.AddComponent<RectTransform>();
            sellRect.anchoredPosition = new Vector2(180f, 0f);
            sellRect.sizeDelta = new Vector2(115f, 36f);

            Image sellBg = sellBtnObj.AddComponent<Image>();
            sellBg.sprite = UIStyleUtility.CreateRoundedPillSprite(115, 36, 18, new Color(0.90f, 0.30f, 0.25f));
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
            Text sellTxt = CreateTextInPanel(sellBtn.transform, Vector2.zero, Vector2.one, string.Format(sellSharesFmt, stockTradeQuantity), 12, Color.white);
            sellTxt.alignment = TextAnchor.MiddleCenter;
            sellTxt.fontStyle = FontStyle.Bold;
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

            string filterQuery = currentFinanceProductSearchQuery.Trim().ToLower(System.Globalization.CultureInfo.InvariantCulture);

            foreach (var pDef in sortedProducts)
            {
                if (!string.IsNullOrEmpty(filterQuery))
                {
                    bool matchesName = pDef.name.ToLower(System.Globalization.CultureInfo.InvariantCulture).Contains(filterQuery);
                    bool matchesShelf = pDef.GetTargetShelfText().ToLower(System.Globalization.CultureInfo.InvariantCulture).Contains(filterQuery);
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
                iText.text = $"{pDef.iconEmoji} <b>{pDef.LocalizedName}</b>  <color=#FFD700>[Lvl {pDef.requiredLevel}]</color>\n<size=12><color=#8A94A6>{pDef.GetTargetShelfText()}   |   {string.Format(costPerUnitFmt, pDef.wholesaleUnitPrice)}</color></size>";
                iText.fontSize = 14;
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
                    sbBadgeText.text = LocalizationManager.L("Badge_Overpriced", "⚠️ Tepki Çeker!\n<size=11><color=#FF6666>(Yüksek Fiyat)</color></size>", "⚠️ Overpriced!\n<size=11><color=#FF6666>(High Price)</color></size>");
                    sbBadgeText.color = new Color(1.0f, 0.35f, 0.25f);
                }
                else
                {
                    string fairFmt = LocalizationManager.L("Badge_FairPrice", "✅ Makul Fiyat\n<size=11><color=#50E678>(%{0:F0} Kâr Marjı)</color></size>", "✅ Fair Price\n<size=11><color=#50E678>(+{0:F0}% Profit Margin)</color></size>");
                    sbBadgeText.text = string.Format(fairFmt, margin);
                    sbBadgeText.color = new Color(0.35f, 0.90f, 0.50f);
                }
                sbBadgeText.fontSize = 12;
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
                ptText.fontSize = 16;
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
            iText.fontSize = 15;
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
            mText.fontSize = 17;
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
            tText.fontSize = 14;
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
            vText.fontSize = 22;
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
                eText.fontSize = 18;
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
                iText.fontSize = 14;
                iText.fontStyle = FontStyle.Normal;
                iText.alignment = TextAnchor.MiddleLeft;
                iText.color = new Color(0.92f, 0.94f, 0.96f);
            }
        }

        public static string GetLocalizedShiftHours(string shiftStr)
        {
            if (string.IsNullOrEmpty(shiftStr)) return "";
            if (shiftStr.Contains("Gündüz"))
            {
                return LocalizationManager.L("ShiftFull_Day", "☀️ Gündüz (06:00 - 14:00)", "☀️ Day (06:00 - 14:00)");
            }
            if (shiftStr.Contains("Akşam"))
            {
                return LocalizationManager.L("ShiftFull_Evening", "🌇 Akşam (14:00 - 22:00)", "🌇 Evening (14:00 - 22:00)");
            }
            if (shiftStr.Contains("Gece"))
            {
                return LocalizationManager.L("ShiftFull_Night", "🌙 Gece (22:00 - 06:00)", "🌙 Night (22:00 - 06:00)");
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
                GameObject emptyObj = new GameObject("EmptyStateMsg");
                emptyObj.transform.SetParent(staffListContent, false);

                RectTransform eRect = emptyObj.AddComponent<RectTransform>();
                eRect.sizeDelta = new Vector2(820f, 150f);

                Text eText = emptyObj.AddComponent<Text>();
                eText.font = globalFont;
                eText.text = LocalizationManager.L("Msg_NoStaffHired", "ℹ️ Henüz işe alınmış personel bulunmuyor.\n'➕ İşe Alım' sekmesinden yeni personel ekleyebilirsiniz.", "ℹ️ No staff currently hired.\nYou can hire new staff from the '+ Hire Staff' tab.");
                eText.fontSize = 18;
                eText.fontStyle = FontStyle.Bold;
                eText.alignment = TextAnchor.MiddleCenter;
                eText.color = new Color(0.80f, 0.85f, 0.90f);
                eText.raycastTarget = false;
                return;
            }

            for (int r = 0; r < 6; r++)
            {
                StaffRole roleEnum = (StaffRole)r;
                List<StaffMember> roleStaff = staffList.FindAll(s => s.role == roleEnum);
                if (roleStaff.Count == 0) continue;

                GameObject categoryHeader = new GameObject("CategoryHeader_" + roleEnum);
                categoryHeader.transform.SetParent(staffListContent, false);

                RectTransform hRect = categoryHeader.AddComponent<RectTransform>();
                hRect.sizeDelta = new Vector2(820f, 32f);

                Text hText = categoryHeader.AddComponent<Text>();
                hText.font = globalFont;
                string staffCountFmt = LocalizationManager.L("Staff_CountFormat", "{0} ({1} Çalışan)", "{0} ({1} Staff)");
                hText.text = string.Format(staffCountFmt, GetRoleCategoryName(r), roleStaff.Count);
                hText.fontSize = 18;
                hText.fontStyle = FontStyle.Bold;
                hText.alignment = TextAnchor.MiddleLeft;
                hText.color = roleCategoryColors[r];
                hText.raycastTarget = false;

                foreach (var staff in roleStaff)
                {
                    GameObject cardObj = new GameObject("StaffCard_" + staff.id);
                    cardObj.transform.SetParent(staffListContent, false);

                    RectTransform cRect = cardObj.AddComponent<RectTransform>();
                    cRect.sizeDelta = new Vector2(820f, 48f);

                    Image cardBg = cardObj.AddComponent<Image>();
                    cardBg.sprite = UIStyleUtility.CreateRoundedPillSprite(820, 48, 12, new Color(0.14f, 0.18f, 0.24f, 0.90f));
                    cardBg.raycastTarget = false;

                    GameObject infoObj = new GameObject("InfoText");
                    infoObj.transform.SetParent(cardObj.transform, false);

                    RectTransform iRect = infoObj.AddComponent<RectTransform>();
                    iRect.anchoredPosition = new Vector2(-60f, 0f);
                    iRect.sizeDelta = new Vector2(520f, 45f);

                    Text iText = infoObj.AddComponent<Text>();
                    iText.font = globalFont;
                    string staffCardInfoFmt = LocalizationManager.L(
                        "Staff_CardInfoFormat",
                        "👤 {0}   |   ⏰ Vardiya: {1}   |   💰 Maaş: {2}C/Gün",
                        "👤 {0}   |   ⏰ Shift: {1}   |   💰 Salary: {2}C/Day"
                    );
                    iText.text = string.Format(staffCardInfoFmt, staff.name, GetLocalizedShiftHours(staff.shiftHours), staff.dailySalary);
                    iText.fontSize = 14;
                    iText.fontStyle = FontStyle.Bold;
                    iText.alignment = TextAnchor.MiddleLeft;
                    iText.color = new Color(0.92f, 0.94f, 0.96f);
                    iText.raycastTarget = false;

                    // ❌ İŞTEN ÇIKAR (KOV) BUTONU
                    GameObject fireBtnObj = new GameObject("FireBtn");
                    fireBtnObj.transform.SetParent(cardObj.transform, false);
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
                    ftText.text = LocalizationManager.L("Btn_FireStaff", "❌ İŞTEN ÇIKAR", "❌ FIRE STAFF");
                    ftText.fontSize = 13;
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
                string salaryLabelFmt = LocalizationManager.L("Salary_Format", "💰 Maaş: {0}C/Gün", "💰 Salary: {0}C/Day");
                iText.text = $"{GetRoleCategoryName(r)}\n<size=13>{roleDescriptions[r]}</size>\n{string.Format(salaryLabelFmt, salary)}";
                iText.fontSize = 16;
                iText.fontStyle = FontStyle.Bold;
                iText.alignment = TextAnchor.MiddleCenter;
                iText.color = new Color(0.90f, 0.95f, 0.90f);
                iText.raycastTarget = false;

                GameObject hireBtnObj = new GameObject("HireButton_Permanent");
                hireBtnObj.transform.SetParent(cardObj.transform, false);

                RectTransform hRect = hireBtnObj.AddComponent<RectTransform>();
                hRect.anchoredPosition = new Vector2(0f, -48f);
                hRect.sizeDelta = new Vector2(220f, 36f);

                Image hBg = hireBtnObj.AddComponent<Image>();
                hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(220, 36, 18, roleCategoryColors[r] * 0.85f);
                hBg.raycastTarget = true;

                Button hBtn = hireBtnObj.AddComponent<Button>();
                hBtn.targetGraphic = hBg;

                StaffRole targetRole = roleEnum;
                hBtn.onClick.AddListener(() => {
                    if (StaffManager.Instance != null)
                    {
                        StaffManager.Instance.HireStaffByRole(targetRole);
                        RefreshStoreManagementViews();
                    }
                });

                GameObject htObj = new GameObject("Text");
                htObj.transform.SetParent(hireBtnObj.transform, false);
                RectTransform htRect = htObj.AddComponent<RectTransform>();
                htRect.anchorMin = Vector2.zero;
                htRect.anchorMax = Vector2.one;

                Text htText = htObj.AddComponent<Text>();
                htText.font = globalFont;
                htText.text = LocalizationManager.L("Btn_HireStaffRole", "➕ PERSONEL EKLE", "➕ HIRE STAFF");
                htText.fontSize = 15;
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

                RectTransform eRect = emptyObj.AddComponent<RectTransform>();
                eRect.sizeDelta = new Vector2(820f, 150f);

                Text eText = emptyObj.AddComponent<Text>();
                eText.font = globalFont;
                eText.text = LocalizationManager.L("Msg_NoStaffShifts", "ℹ️ Vardiyası ayarlanacak çalışan personel bulunmuyor.\nLütfen önce '➕ İşe Alım' sekmesinden personel ekleyin.", "ℹ️ No active staff to manage shifts.\nPlease hire staff from the '+ Hire Staff' tab first.");
                eText.fontSize = 18;
                eText.fontStyle = FontStyle.Bold;
                eText.alignment = TextAnchor.MiddleCenter;
                eText.color = new Color(0.80f, 0.85f, 0.90f);
                eText.raycastTarget = false;
                return;
            }

            for (int r = 0; r < 6; r++)
            {
                StaffRole roleEnum = (StaffRole)r;
                List<StaffMember> roleStaff = staffList.FindAll(s => s.role == roleEnum);
                if (roleStaff.Count == 0) continue;

                GameObject categoryHeader = new GameObject("ShiftHeader_" + roleEnum);
                categoryHeader.transform.SetParent(shiftListContent, false);

                RectTransform hRect = categoryHeader.AddComponent<RectTransform>();
                hRect.sizeDelta = new Vector2(820f, 32f);

                Text hText = categoryHeader.AddComponent<Text>();
                hText.font = globalFont;
                string shiftHeaderFmt = LocalizationManager.L("Shift_HeaderFormat", "{0} Vardiya Ayarları", "{0} Shift Settings");
                hText.text = string.Format(shiftHeaderFmt, GetRoleCategoryName(r));
                hText.fontSize = 18;
                hText.fontStyle = FontStyle.Bold;
                hText.alignment = TextAnchor.MiddleLeft;
                hText.color = roleCategoryColors[r];
                hText.raycastTarget = false;

                foreach (var staff in roleStaff)
                {
                    GameObject cardObj = new GameObject("ShiftCard_" + staff.id);
                    cardObj.transform.SetParent(shiftListContent, false);

                    RectTransform cRect = cardObj.AddComponent<RectTransform>();
                    cRect.sizeDelta = new Vector2(820f, 52f);

                    Image cardBg = cardObj.AddComponent<Image>();
                    cardBg.sprite = UIStyleUtility.CreateRoundedPillSprite(820, 52, 12, new Color(0.14f, 0.18f, 0.24f, 0.90f));
                    cardBg.raycastTarget = false;

                    GameObject nameObj = new GameObject("NameText");
                    nameObj.transform.SetParent(cardObj.transform, false);

                    RectTransform nRect = nameObj.AddComponent<RectTransform>();
                    nRect.anchoredPosition = new Vector2(-305f, 0f);
                    nRect.sizeDelta = new Vector2(190f, 45f);

                    Text nText = nameObj.AddComponent<Text>();
                    nText.font = globalFont;
                    nText.text = $"👤 {staff.name}\n⏰ {GetLocalizedShiftHours(staff.shiftHours)}";
                    nText.fontSize = 14;
                    nText.fontStyle = FontStyle.Bold;
                    nText.alignment = TextAnchor.MiddleLeft;
                    nText.color = Color.white;
                    nText.raycastTarget = false;

                    // --- SADECE GÜNDÜZ VARDİYASI VE SADECE 06:00 AM & DÜKKAN KAPALIYKEN ERKEN ÇAĞIR BUTONU ---
                    CreateEarlyCallButton(cardObj.transform, staff);

                    CreateShiftOptionButtons(cardObj.transform, staff);
                }
            }
        }

        private void CreateEarlyCallButton(Transform parent, StaffMember staff)
        {
            if (staff == null) return;

            bool isMorningShift = (staff.shiftHours != null && staff.shiftHours.Contains("Gündüz"));
            bool is06AM = (TimeManager.Instance != null && TimeManager.Instance.Hour == 6);
            bool isStoreClosed = (StoreStatusManager.Instance != null && !StoreStatusManager.Instance.IsOpen);

            // SADECE Gündüz vardiyasındaki personeller ve SADECE 06:00 AM & Dükkan Kapalıyken gösterilir!
            if (!isMorningShift || !is06AM || !isStoreClosed) return;

            bool isAlreadyCalled = (StaffVisualManager.Instance != null && StaffVisualManager.Instance.IsStaffCalledEarlyToday(staff.id));

            GameObject btnObj = new GameObject("Btn_EarlyCall_" + staff.id);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-125f, 0f); // İsim metni ile vardiya butonları arasında net boşluk!
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
            txt.fontSize = 12;
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
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleRight;

            string[] shiftNames = new string[] {
                LocalizationManager.L("Shift_Day", "☀️ Gündüz", "☀️ Day"),
                LocalizationManager.L("Shift_Evening", "🌆 Akşam", "🌆 Evening"),
                LocalizationManager.L("Shift_Night", "🌙 Gece", "🌙 Night")
            };
            string[] shiftFullNames = new string[] {
                LocalizationManager.L("ShiftFull_Day", "☀️ Gündüz (06:00 - 14:00)", "☀️ Day (06:00 - 14:00)"),
                LocalizationManager.L("ShiftFull_Evening", "🌆 Akşam (14:00 - 22:00)", "🌆 Evening (14:00 - 22:00)"),
                LocalizationManager.L("ShiftFull_Night", "🌙 Gece (22:00 - 06:00)", "🌙 Night (22:00 - 06:00)")
            };
            string currentShiftStr = staff.shiftHours ?? "";

            for (int i = 0; i < 3; i++)
            {
                string targetShift = shiftFullNames[i];
                bool isCurrentShift = false;
                if (i == 0 && currentShiftStr.Contains("Gündüz")) isCurrentShift = true;
                else if (i == 1 && currentShiftStr.Contains("Akşam")) isCurrentShift = true;
                else if (i == 2 && currentShiftStr.Contains("Gece")) isCurrentShift = true;

                string staffId = staff.id;
                string selectedShift = targetShift;

                GameObject btnObj = new GameObject("ShiftBtn_" + i);
                btnObj.transform.SetParent(optsObj.transform, false);

                RectTransform bRect = btnObj.AddComponent<RectTransform>();
                bRect.sizeDelta = new Vector2(118f, 34f);

                Image bg = btnObj.AddComponent<Image>();
                if (isCurrentShift)
                {
                    bg.sprite = UIStyleUtility.CreateOutlinePillSprite(138, 34, 17, 2, new Color(0.20f, 0.85f, 0.40f), new Color(0.15f, 0.45f, 0.25f, 0.95f));
                }
                else
                {
                    bg.sprite = UIStyleUtility.CreateRoundedPillSprite(138, 34, 17, new Color(0.20f, 0.24f, 0.30f, 0.80f));
                }
                bg.raycastTarget = true;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => {
                    // DÜKKAN AÇIKKEN VARDİYA DEĞİŞTİRİLMESİ ENGELLENİR, DÜKKAN KAPALIYKEN ÖZGÜRCE DEĞİŞTİRİLEBİLİR!
                    bool isStoreOpen = (StoreStatusManager.Instance != null && StoreStatusManager.Instance.IsOpen);

                    if (isStoreOpen)
                    {
                        ModalManager.ShowModal(
                            "Vardiya Değiştirilemez! ⚠️",
                            "Dükkan açıkken çalışan personellerin vardiyası değiştirilemez.\n\nVardiya değişikliklerini dükkan kapalıyken yapabilirsiniz.",
                            "Tamam"
                        );
                        return;
                    }

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
                btnText.text = shiftNames[i];
                btnText.fontSize = 13;
                btnText.fontStyle = FontStyle.Bold;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = isCurrentShift ? Color.white : new Color(0.75f, 0.80f, 0.85f);
                btnText.raycastTarget = false;
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
            bText.fontSize = 15;
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
            tText.fontSize = 20;
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
                tabBg.sprite = UIStyleUtility.CreateOutlinePillSprite(198, 40, 20, 2, new Color(0.25f, 0.85f, 0.40f), new Color(0.12f, 0.16f, 0.22f, 0.85f));
                tabBg.raycastTarget = true;

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
                tabText.fontSize = 15;
                tabText.fontStyle = FontStyle.Bold;
                tabText.alignment = TextAnchor.MiddleCenter;
                tabText.color = new Color(0.40f, 0.95f, 0.55f);
                tabText.raycastTarget = false;
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

        private GameObject CreateButtonInPanel(Transform parent, Vector2 pos, Vector2 size, string text, Color bgColor, UnityEngine.Events.UnityAction onClick, int fontSize = 13)
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
            Text cardText = CreateTextInPanel(cardObj.transform, Vector2.zero, Vector2.one, farmReportText, 15, Color.white);
            cardText.alignment = TextAnchor.MiddleLeft;

            // 2. 3 AŞAMALI AHIR KAPASİTE GELİŞTİRME KARTLARI
            string[] upgradeTitles = new string[] {
                LocalizationManager.L("Barn_Lvl1_Title", "🏡 Ahır Seviye 1 (Temel Depo)", "🏡 Barn Level 1 (Basic Storage)"),
                LocalizationManager.L("Barn_Lvl2_Title", "🛖 Ahır Seviye 2 (Genişletilmiş Depo)", "🛖 Barn Level 2 (Expanded Storage)"),
                LocalizationManager.L("Barn_Lvl3_Title", "🏗️ Ahır Seviye 3 (Dev Çiftlik Silosu & Ahır)", "🏗️ Barn Level 3 (Giant Farm Silo & Barn)")
            };

            string[] upgradeDescs = new string[] {
                LocalizationManager.L("Barn_Lvl1_Desc", "Başlangıç Ahırı. Maksimum 500 KG mahsul depolama kapasitesi sunar.", "Initial Barn. Offers maximum 500 KG crop storage capacity."),
                LocalizationManager.L("Barn_Lvl2_Desc", "Ahır depolama alanını genişleterek maksimum kapasiteyi 1.500 KG seviyesine çıkarır.", "Expands barn storage area to maximum 1,500 KG capacity."),
                LocalizationManager.L("Barn_Lvl3_Desc", "Devasa çiftlik silosu ve ahır. Maksimum mahsul depolama kapasitesini 4.000 KG seviyesine çıkarır.", "Giant farm silo and barn. Increases maximum crop storage capacity to 4,000 KG.")
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

                Text tTitle = CreateTextInPanel(upgCard.transform, new Vector2(-120f, 18f), new Vector2(520f, 30f), upgradeTitles[i], 16, isUnlocked ? new Color(0.40f, 0.95f, 0.60f) : Color.white);
                tTitle.alignment = TextAnchor.MiddleLeft;

                Text tDesc = CreateTextInPanel(upgCard.transform, new Vector2(-120f, -15f), new Vector2(520f, 35f), upgradeDescs[i], 14, new Color(0.80f, 0.85f, 0.90f));
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
                Text bText = CreateTextInPanel(buyBtnObj.transform, Vector2.zero, Vector2.one, btnTextStr, 13, Color.white);
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
            iText.text = LocalizationManager.L("FarmWorker_CardInfo", "🌾 Çiftlik İşçisi\n<size=13>Sulama, çapa & ürün hasadı\n(Erkek / Kadın Rastgele Aday)</size>\n💰 Maaş: 250 Cr/Gün (Gece 12'de)", "🌾 Farm Worker\n<size=13>Watering, hoeing & crop harvesting\n(Random Male / Female Candidate)</size>\n💰 Salary: 250 Cr/Day (At Midnight)");
            iText.fontSize = 16;
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
                    StaffManager.Instance.HireFarmWorker();
                    if (TutorialManager.Instance != null)
                    {
                        TutorialManager.Instance.NotifyStaffHired(StaffRole.Çiftçi);
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
            htText.text = LocalizationManager.L("Btn_HireZeroCost", "➕ İŞE AL (0C)", "➕ HIRE (0C)");
            htText.fontSize = 15;
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

                Text eText = CreateTextInPanel(emptyObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Msg_NoFarmStaff", "👨‍🌾 Çiftlikte çalışan henüz işçi bulunmuyor.\n'2. İşçi İşe Al' sekmesinden yeni çiftçi ekleyebilirsiniz.", "👨‍🌾 No farm workers currently hired.\nYou can hire new farmers from the '2. Hire Staff' tab."), 15, Color.gray);
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
                Text nText = CreateTextInPanel(sCard.transform, new Vector2(-60f, 0f), new Vector2(520f, 45f), string.Format(rowFmt, staff.name, GetLocalizedShiftHours(staff.shiftHours), staff.dailySalary), 14, Color.white);
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
                ftText.fontSize = 13;
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

                Text eText = CreateTextInPanel(emptyObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Msg_NoFarmShiftStaff", "🌾 Vardiyası ayarlanacak çiftlik işçisi bulunmuyor.\nLütfen önce '2. İşçi İşe Al' sekmesinden çiftçi ekleyin.", "🌾 No active farm workers to manage shifts.\nPlease hire farmers from the '2. Hire Staff' tab first."), 15, Color.gray);
                eText.alignment = TextAnchor.MiddleCenter;
                return;
            }

            string[] shiftNames = new string[] {
                LocalizationManager.L("Shift_Day", "☀️ Gündüz", "☀️ Day"),
                LocalizationManager.L("Shift_Evening", "🌆 Akşam", "🌆 Evening"),
                LocalizationManager.L("Shift_Night", "🌙 Gece", "🌙 Night")
            };
            string[] shiftFullNames = new string[] {
                LocalizationManager.L("ShiftFull_Day", "☀️ Gündüz (06:00 - 14:00)", "☀️ Day (06:00 - 14:00)"),
                LocalizationManager.L("ShiftFull_Evening", "🌆 Akşam (14:00 - 22:00)", "🌆 Evening (14:00 - 22:00)"),
                LocalizationManager.L("ShiftFull_Night", "🌙 Gece (22:00 - 06:00)", "🌙 Night (22:00 - 06:00)")
            };

            foreach (var staff in farmList)
            {
                GameObject sCard = new GameObject("Farm_Shift_Card_" + staff.id);
                sCard.transform.SetParent(farmShiftContent, false);

                LayoutElement sElem = sCard.AddComponent<LayoutElement>();
                sElem.minHeight = 52f;
                sElem.preferredHeight = 52f;

                Image bg = sCard.AddComponent<Image>();
                bg.sprite = UIStyleUtility.CreateRoundedPillSprite(830, 52, 12, new Color(0.14f, 0.18f, 0.24f, 0.90f));
                bg.raycastTarget = false;

                Text nText = CreateTextInPanel(sCard.transform, new Vector2(-305f, 0f), new Vector2(190f, 45f), $"👤 {staff.name}\n⏰ {GetLocalizedShiftHours(staff.shiftHours)}", 14, Color.white);
                nText.alignment = TextAnchor.MiddleLeft;

                CreateEarlyCallButton(sCard.transform, staff);

                GameObject optsObj = new GameObject("ShiftOptions");
                optsObj.transform.SetParent(sCard.transform, false);

                RectTransform oRect = optsObj.AddComponent<RectTransform>();
                oRect.anchoredPosition = new Vector2(210f, 0f);
                oRect.sizeDelta = new Vector2(360f, 40f);

                HorizontalLayoutGroup hLayout = optsObj.AddComponent<HorizontalLayoutGroup>();
                hLayout.spacing = 6f;
                hLayout.childAlignment = TextAnchor.MiddleCenter;

                for (int i = 0; i < 3; i++)
                {
                    int shiftIdx = i;
                    string targetShift = shiftFullNames[shiftIdx];
                    bool isCurrentShift = (staff.shiftHours == targetShift);

                    GameObject btnObj = new GameObject("ShiftBtn_" + shiftIdx);
                    btnObj.transform.SetParent(optsObj.transform, false);

                    RectTransform bRect = btnObj.AddComponent<RectTransform>();
                    bRect.sizeDelta = new Vector2(134f, 34f);

                    Image bBg = btnObj.AddComponent<Image>();
                    if (isCurrentShift)
                    {
                        bBg.sprite = UIStyleUtility.CreateOutlinePillSprite(134, 34, 17, 2, new Color(0.25f, 0.85f, 0.40f), new Color(0.15f, 0.45f, 0.25f, 0.95f));
                    }
                    else
                    {
                        bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(134, 34, 17, new Color(0.20f, 0.24f, 0.30f, 0.80f));
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

                    Text btnText = CreateTextInPanel(btnObj.transform, Vector2.zero, Vector2.one, shiftNames[shiftIdx], 13, isCurrentShift ? Color.white : new Color(0.75f, 0.80f, 0.85f));
                    btnText.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        // ==================== TOPLU SİPARİŞ (BULK ORDER) SİSTEMİ ====================

        private int GetProductStockInStore(string productName)
        {
            int totalStock = 0;
            PlacedFurnitureController[] shelves = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            if (shelves != null)
            {
                foreach (var shelf in shelves)
                {
                    if (shelf.rows == null) continue;
                    foreach (var row in shelf.rows)
                    {
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

            List<WholesaleProductDef> allProducts = WholesaleDatabase.GetAllProducts();
            List<WholesaleProductDef> unlockedProducts = allProducts.FindAll(p => p.requiredLevel <= currentLevel);

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
            hRect.anchoredPosition = new Vector2(0f, 210f);
            hRect.sizeDelta = new Vector2(850f, 40f);

            GameObject backBtnObj = new GameObject("BackBtn");
            backBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform bRect = backBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(-400f, 0f);
            bRect.sizeDelta = new Vector2(34f, 34f);

            Image bBg = backBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(34, 34, 17, new Color(0.20f, 0.25f, 0.35f));
            Button bBtn = backBtnObj.AddComponent<Button>();
            bBtn.targetGraphic = bBg;
            bBtn.onClick.AddListener(ShowHomeScreen);

            Text bTxt = CreateTextInPanel(backBtnObj.transform, Vector2.zero, Vector2.one, "◀", 16, Color.white);
            bTxt.alignment = TextAnchor.MiddleCenter;

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 0f);
            tRect.sizeDelta = new Vector2(400f, 40f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = globalFont;
            tText.text = LocalizationManager.L("App_SocialHeader", "𝕏  CHIRPER / SOSYAL MEDYA", "𝕏  CHIRPER / SOCIAL MEDIA");
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

            Text cTxt = CreateTextInPanel(composeBtnObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_PostTweet", "✍️ TWEET AT", "✍️ POST TWEET"), 13, Color.white);
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
            pcRect.anchoredPosition = new Vector2(0f, 85f);
            pcRect.sizeDelta = new Vector2(260f, 190f);

            Image pcBg = profileCard.AddComponent<Image>();
            pcBg.sprite = UIStyleUtility.CreateOutlinePillSprite(260, 190, 16, 2, new Color(0.12f, 0.65f, 0.95f), new Color(0.12f, 0.16f, 0.22f, 0.95f));
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

            Text pcTxt = CreateTextInPanel(profileCard.transform, Vector2.zero, Vector2.one, "", 13, Color.white);
            string profileFmt = LocalizationManager.L(
                "Social_ProfileCardFmt",
                "👨‍🌾 <b>{0}</b> ✔️\n<size=13><color=#80B0FF>{1}</color></size>\n\n🏢 <b>@{2}</b>\n👥 <b>{3:N0}</b> Takipçi  |  ⭐ <b>4.9</b> Puan\n<size=12><color=#A0AAB5>\"Tarladan rafa taptaze ürünler! 🌾✨\"</color></size>\n\n<size=12><color=#00E676>👉 Profile Gitmek İçin Tıkla</color></size>",
                "👨‍🌾 <b>{0}</b> ✔️\n<size=13><color=#80B0FF>{1}</color></size>\n\n🏢 <b>@{2}</b>\n👥 <b>{3:N0}</b> Followers  |  ⭐ <b>4.9</b> Rating\n<size=12><color=#A0AAB5>\"Fresh farm crops straight to your shelf! 🌾✨\"</color></size>\n\n<size=12><color=#00E676>👉 Click to View Profile</color></size>"
            );
            pcTxt.text = string.Format(profileFmt, pName, pHandle, sName.Replace(" ", ""), followers);
            pcTxt.alignment = TextAnchor.MiddleCenter;

            // Trendler Kartı (Alt Yarım)
            GameObject trendCard = new GameObject("TrendCard");
            trendCard.transform.SetParent(leftPanel.transform, false);
            RectTransform tcRect = trendCard.AddComponent<RectTransform>();
            tcRect.anchoredPosition = new Vector2(0f, -90f);
            tcRect.sizeDelta = new Vector2(260f, 175f);

            Image tcBg = trendCard.AddComponent<Image>();
            tcBg.sprite = UIStyleUtility.CreateOutlinePillSprite(260, 175, 16, 1, new Color(0.25f, 0.35f, 0.45f), new Color(0.10f, 0.14f, 0.20f, 0.95f));

            Text tcTxt = CreateTextInPanel(trendCard.transform, Vector2.zero, Vector2.one, "", 12, Color.white);
            string trendFmt = LocalizationManager.L(
                "Social_TrendFmt",
                "🔥 <b>GÜNDEMDEKİ BAŞLIKLAR</b>\n\n" +
                "1️⃣ <b>#FreshShelfMarket</b> <color=#80A0C0>(14.2B)</color>\n" +
                "2️⃣ <b>#HızlıKasa</b> <color=#80A0C0>(9.8B)</color>\n" +
                "3️⃣ <b>#TazeHasat</b> <color=#80A0C0>(6.5B)</color>\n" +
                "4️⃣ <b>#MarketSırası</b> <color=#80A0C0>(4.1B)</color>\n" +
                "5️⃣ <b>#Farm2Shelf</b> <color=#80A0C0>(2.9B)</color>",

                "🔥 <b>TRENDING TOPICS</b>\n\n" +
                "1️⃣ <b>#FreshShelfMarket</b> <color=#80A0C0>(14.2K)</color>\n" +
                "2️⃣ <b>#FastCheckout</b> <color=#80A0C0>(9.8K)</color>\n" +
                "3️⃣ <b>#FreshHarvest</b> <color=#80A0C0>(5.2K)</color>\n" +
                "4️⃣ <b>#StoreQueue</b> <color=#80A0C0>(4.1K)</color>\n" +
                "5️⃣ <b>#Farm2Shelf</b> <color=#80A0C0>(2.9K)</color>"
            );
            tcTxt.text = trendFmt;
            tcTxt.alignment = TextAnchor.MiddleLeft;

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
            tbRect.sizeDelta = new Vector2(570f, 34f);

            string[] tabNames = new string[] {
                LocalizationManager.L("SocialTab_ForYou", "🌐 1. Sana Özel", "🌐 1. For You"),
                LocalizationManager.L("SocialTab_Reviews", "💬 2. Yorumlar", "💬 2. Reviews"),
                LocalizationManager.L("SocialTab_MyTweets", "👤 3. Twitlerim", "👤 3. My Tweets")
            };

            for (int t = 0; t < 3; t++)
            {
                int tabIdx = t;
                GameObject tBtnObj = new GameObject("Tab_" + t);
                tBtnObj.transform.SetParent(tabsBar.transform, false);
                RectTransform tabRect = tBtnObj.AddComponent<RectTransform>();
                tabRect.anchoredPosition = new Vector2(-180f + t * 180f, 0f);
                tabRect.sizeDelta = new Vector2(175f, 34f);

                bool isSel = (activeSocialTab == tabIdx);
                Image tBg = tBtnObj.AddComponent<Image>();
                tBg.sprite = UIStyleUtility.CreateRoundedPillSprite(175, 34, 17, isSel ? new Color(0.12f, 0.65f, 0.95f) : new Color(0.18f, 0.22f, 0.30f));
                socialTabBtnImgs[t] = tBg;

                Button tBtn = tBtnObj.AddComponent<Button>();
                tBtn.targetGraphic = tBg;
                tBtn.onClick.AddListener(() => {
                    activeSocialTab = tabIdx;
                    RefreshSocialMediaViews();
                });

                Text tTxt = CreateTextInPanel(tBtnObj.transform, Vector2.zero, Vector2.one, tabNames[t], 14, Color.white);
                tTxt.alignment = TextAnchor.MiddleCenter;
                tTxt.fontStyle = FontStyle.Bold;
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
                        socialTabBtnImgs[t].sprite = UIStyleUtility.CreateRoundedPillSprite(175, 34, 17, (activeSocialTab == t) ? new Color(0.12f, 0.65f, 0.95f) : new Color(0.18f, 0.22f, 0.30f));
                    }
                }
            }

            if (socialMediaFeedContent == null) return;
            foreach (Transform child in socialMediaFeedContent) Destroy(child.gameObject);

            if (SocialMediaManager.Instance == null) return;

            List<SocialTweetData> tweets = SocialMediaManager.Instance.GetFeed(activeSocialTab);
            foreach (var tweetData in tweets)
            {
                SocialTweetData tweet = tweetData;
                GameObject cardObj = new GameObject("TweetCard_" + tweet.tweetId);
                cardObj.transform.SetParent(socialMediaFeedContent, false);

                RectTransform cRect = cardObj.AddComponent<RectTransform>();
                cRect.sizeDelta = new Vector2(550f, 85f);

                LayoutElement le = cardObj.AddComponent<LayoutElement>();
                le.minHeight = 85f;
                le.preferredHeight = 85f;
                le.flexibleWidth = 1f;

                Color borderColor = tweet.isPlayerTweet ? new Color(0.12f, 0.65f, 0.95f) : (tweet.sentiment == TweetSentiment.Complaint ? new Color(0.90f, 0.35f, 0.25f) : new Color(0.25f, 0.75f, 0.40f));
                Image cBg = cardObj.AddComponent<Image>();
                cBg.sprite = UIStyleUtility.CreateOutlinePillSprite(550, 85, 12, 1, borderColor, new Color(0.12f, 0.16f, 0.22f, 0.96f));

                // Metin Kutusu
                GameObject infoObj = new GameObject("Info");
                infoObj.transform.SetParent(cardObj.transform, false);
                RectTransform iRect = infoObj.AddComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(-40f, 6f);
                iRect.sizeDelta = new Vector2(440f, 68f);

                Text iTxt = infoObj.AddComponent<Text>();
                iTxt.font = globalFont;

                string verifiedMark = tweet.isVerified ? "✔️" : "";
                string sentimentBadge = tweet.sentiment == TweetSentiment.Official
                    ? "<color=#00E676><b>[DUYURU]</b></color>"
                    : (tweet.sentiment == TweetSentiment.Complaint ? "<color=#FF5252><b>[ŞİKAYET]</b></color>" : "<color=#40C4FF><b>[MÜŞTERİ]</b></color>");

                if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish)
                {
                    sentimentBadge = tweet.sentiment == TweetSentiment.Official
                        ? "<color=#00E676><b>[OFFICIAL]</b></color>"
                        : (tweet.sentiment == TweetSentiment.Complaint ? "<color=#FF5252><b>[COMPLAINT]</b></color>" : "<color=#40C4FF><b>[REVIEW]</b></color>");
                }

                iTxt.text = $"{tweet.avatarEmoji} <b>{tweet.authorName}</b> {verifiedMark} <color=#80A0C0>({tweet.authorHandle} • {tweet.LocalizedTime})</color>  {sentimentBadge}\n<size=13>{tweet.LocalizedText}</size>";
                iTxt.fontSize = 13;
                iTxt.alignment = TextAnchor.MiddleLeft;
                iTxt.color = Color.white;

                // Beğen & Repost Butonları (Sağ Taraf)
                GameObject actionsObj = new GameObject("Actions");
                actionsObj.transform.SetParent(cardObj.transform, false);
                RectTransform aRect = actionsObj.AddComponent<RectTransform>();
                aRect.anchoredPosition = new Vector2(215f, -18f);
                aRect.sizeDelta = new Vector2(90f, 30f);

                // Heart Button
                GameObject heartBtnObj = new GameObject("HeartBtn");
                heartBtnObj.transform.SetParent(actionsObj.transform, false);
                RectTransform hRect = heartBtnObj.AddComponent<RectTransform>();
                hRect.anchoredPosition = new Vector2(-22f, 0f);
                hRect.sizeDelta = new Vector2(40f, 26f);

                Image hBg = heartBtnObj.AddComponent<Image>();
                hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(40, 26, 10, tweet.isLikedByPlayer ? new Color(0.90f, 0.25f, 0.35f) : new Color(0.20f, 0.25f, 0.32f));
                Button hBtn = heartBtnObj.AddComponent<Button>();
                hBtn.targetGraphic = hBg;
                hBtn.onClick.AddListener(() => {
                    SocialMediaManager.Instance.ToggleLike(tweet);
                    RefreshSocialMediaViews();
                });

                Text hTxt = CreateTextInPanel(heartBtnObj.transform, Vector2.zero, Vector2.one, $"❤️{tweet.likesCount}", 11, Color.white);
                hTxt.alignment = TextAnchor.MiddleCenter;

                // Repost Button
                GameObject rtBtnObj = new GameObject("RTBtn");
                rtBtnObj.transform.SetParent(actionsObj.transform, false);
                RectTransform rRect = rtBtnObj.AddComponent<RectTransform>();
                rRect.anchoredPosition = new Vector2(22f, 0f);
                rRect.sizeDelta = new Vector2(40f, 26f);

                Image rBg = rtBtnObj.AddComponent<Image>();
                rBg.sprite = UIStyleUtility.CreateRoundedPillSprite(40, 26, 10, tweet.isRetweetedByPlayer ? new Color(0.20f, 0.75f, 0.40f) : new Color(0.20f, 0.25f, 0.32f));
                Button rBtn = rtBtnObj.AddComponent<Button>();
                rBtn.targetGraphic = rBg;
                rBtn.onClick.AddListener(() => {
                    SocialMediaManager.Instance.ToggleRetweet(tweet);
                    RefreshSocialMediaViews();
                });

                Text rTxt = CreateTextInPanel(rtBtnObj.transform, Vector2.zero, Vector2.one, $"🔁{tweet.retweetsCount}", 11, Color.white);
                rTxt.alignment = TextAnchor.MiddleCenter;
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
            tTxt.text = LocalizationManager.L("Compose_Header", "✍️ RESMİ DUYURU TWİTİ SEÇİNİZ (10 FARKLI SEÇENEK)", "✍️ SELECT OFFICIAL TWEET ANNOUNCEMENT (10 OPTIONS)");
            tTxt.fontSize = 22;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = new Color(0.30f, 0.85f, 1.0f);

            // Close Button (Top-Right X)
            GameObject closeBtnObj = new GameObject("CloseXBtn");
            closeBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(355f, 245f);
            cRect.sizeDelta = new Vector2(34f, 34f);

            Image cBg = closeBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(34, 34, 17, new Color(0.85f, 0.25f, 0.25f));
            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBg;
            cBtn.onClick.AddListener(() => Destroy(canvasObj));

            Text cTxt = CreateTextInPanel(closeBtnObj.transform, Vector2.zero, Vector2.one, "✕", 18, Color.white);
            cTxt.alignment = TextAnchor.MiddleCenter;

            // Scroll Area for 10 Tweets
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
                ("🚀 Mağaza Açılışı", "🚀 Grand Opening", $"🚀 Taptaze çiftlik mahsullerimizle dükkanımız hizmetinizde! Hepinizi @{sName} bekliyoruz! 🌾🛒", $"🚀 Grand opening! Fresh farm crops and wide variety of products ready for you at @{sName}! 🌾🛒"),
                ("🏷️ %20 İndirim Kampanyası", "🏷️ 20% Discount Sale", $"🏷️ TÜM ÜRÜNLERDE %20 İNDİRİM! Tarladan rafa taze sebze ve meyveler @{sName} dükkanında özel fiyatla! 🍎🥦", $"🏷️ 20% OFF ALL PRODUCTS! Fresh vegetables and fruits direct from farm to shelf at @{sName}! 🍎🥦"),
                ("⚡ Hızlı Kasa & Kesintisiz Hizmet", "⚡ Fast Checkout & Zero Wait", $"⚡ Ekstra kasalarımız açıldı! Sıra beklemeden taze ve hızlı alışverişin tadını çıkarın! @{sName} ⚡🛒", $"⚡ Extra checkout lines open! Enjoy lightning fast shopping with zero queue wait times at @{sName}! ⚡🛒"),
                ("🌾 %100 Organik Taze Hasat", "🌾 100% Organic Fresh Harvest", $"🌾 Çiftliğimizden bu sabah toplanan %100 organik domates, çilek ve yeşillikler raflarda! @{sName} 🍓🍅", $"🌾 100% organic tomatoes, strawberries and greens harvested this morning are now stocked at @{sName}! 🍓🍅"),
                ("🏪 Yeni Reyonlar & Genişletme", "🏪 Supermarket Expansion", $"🏪 Mağazamızı büyüttük! Soğuk içecekler, fırın ürünleri ve kozmetik reyonlarımız açıldı! @{sName} 🥐🧊", $"🏪 Store expanded! Introducing our brand new cold beverage, bakery and cosmetics aisles at @{sName}! 🥐🧊"),
                ("🌙 Gece İndirimi & Kapanış Fırsatları", "🌙 Late Night Clearance Deal", $"🌙 Gece alışverişi fırsatı! Kapanış öncesi şarküteri ve unlu mamullerde özel indirimler! @{sName} 🌙🥖", $"🌙 Late night clearance deal! Special discounts on deli and bakery products before closing at @{sName}! 🌙🥖"),
                ("👑 VIP Müşteri Sadakat Ödülleri", "👑 VIP Customer Appreciation", $"👑 Sadık müşterilerimize özel sürpriz hediye çekleri ve bonus puan kampanyamız başladı! @{sName} 👑🎁", $"👑 VIP customer appreciation day! Earn bonus points and voucher gifts with every order at @{sName}! 👑🎁"),
                ("🥛 Taze Süt & Şarküteri Reyonu", "🥛 Fresh Dairy & Cold Deli", $"🥛 Günlük taze süt, organik peynir ve tereyağları soğutucu dolaplarımızda sizleri bekliyor! @{sName} 🥛🧀", $"🥛 Daily fresh milk, artisan cheese and organic butter now stocked in refrigerated displays at @{sName}! 🥛🧀"),
                ("🧹 Hijyen & Temizlik Garantisi", "🧹 Sanitation & Cleanliness", $"🧹 Dükkanımızda hijyen ve temizlik standartlarımız %100! Güvenle alışveriş yapabilirsiniz. @{sName} ✨🧹", $"🧹 Top tier store cleanliness and sanitation standards guaranteed for your safe shopping at @{sName}! ✨🧹"),
                ("🎉 Hafta Sonu Tarım Festivali", "🎉 Weekend Harvest Festival", $"🎉 Hafta sonuna özel Çiftlikten Rafa Tarım Festivali başladı! Sürpriz indirimleri kaçırmayın! @{sName} 🎉🌾", $"🎉 Weekend Farm-to-Shelf Harvest Festival is live! Don't miss out on special surprise deals at @{sName}! 🎉🌾")
            };

            for (int i = 0; i < tweetOptions.Length; i++)
            {
                var opt = tweetOptions[i];
                GameObject itemObj = new GameObject("TweetOption_" + i);
                itemObj.transform.SetParent(contentObj.transform, false);

                LayoutElement le = itemObj.AddComponent<LayoutElement>();
                le.minHeight = 78f;
                le.preferredHeight = 78f;
                le.flexibleWidth = 1f;

                Image itemBg = itemObj.AddComponent<Image>();
                itemBg.sprite = UIStyleUtility.CreateOutlinePillSprite(700, 78, 12, 1, new Color(0.15f, 0.55f, 0.85f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

                // Text
                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(itemObj.transform, false);
                RectTransform tItemRect = txtObj.AddComponent<RectTransform>();
                tItemRect.anchoredPosition = new Vector2(-60f, 0f);
                tItemRect.sizeDelta = new Vector2(530f, 66f);

                Text itemTxt = txtObj.AddComponent<Text>();
                itemTxt.font = globalFont;
                string optTitle = LocalizationManager.L("OptTitle_" + i, opt.titleTr, opt.titleEn);
                string optBody = LocalizationManager.L("OptBody_" + i, opt.textTr, opt.textEn);
                itemTxt.text = $"<b><color=#40C4FF>{optTitle}</color></b>\n<size=13>{optBody}</size>";
                itemTxt.fontSize = 13;
                itemTxt.alignment = TextAnchor.MiddleLeft;
                itemTxt.color = Color.white;

                // Post Button
                GameObject postBtnObj = new GameObject("PostBtn");
                postBtnObj.transform.SetParent(itemObj.transform, false);
                RectTransform pBtnRect = postBtnObj.AddComponent<RectTransform>();
                pBtnRect.anchoredPosition = new Vector2(275f, 0f);
                pBtnRect.sizeDelta = new Vector2(105f, 36f);

                Image pBg = postBtnObj.AddComponent<Image>();
                pBg.sprite = UIStyleUtility.CreateRoundedPillSprite(105, 36, 14, new Color(0.12f, 0.65f, 0.95f));
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

                Text pTxt = CreateTextInPanel(postBtnObj.transform, Vector2.zero, Vector2.one, LocalizationManager.L("Btn_PostShort", "🚀 PAYLAŞ", "🚀 POST"), 13, Color.white);
                pTxt.alignment = TextAnchor.MiddleCenter;
                pTxt.fontStyle = FontStyle.Bold;
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
