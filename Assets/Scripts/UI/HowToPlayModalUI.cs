using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Ana menü Rehber Kütüphanesi. Oyuncu merak ettiği konuya basarak
    /// detaylı, çift dilli anlatımı ve atölye tariflerini okur.
    /// </summary>
    public class HowToPlayModalUI : MonoBehaviour
    {
        private enum GuideTopic
        {
            Overview,
            Controls,
            Tablet,
            Passport,
            TownContracts,
            TaxiStand,
            Brand,
            EndOfDay,
            Inspector,
            Farm,
            Barn,
            Store,
            Wholesale,
            Checkout,
            Staff,
            Furniture,
            Workshop,
            JamMaker,
            JuicePress,
            Cannery,
            Dehydrator,
            OilPress,
            SaladStation,
            Finance,
            Expansion
        }

        public static HowToPlayModalUI Instance { get; private set; }
        public bool IsModalOpen => canvasObj != null && canvasObj.activeInHierarchy;

        private GameObject canvasObj;
        private GuideTopic currentTopic = GuideTopic.Overview;
        private readonly List<Image> topicButtonImages = new List<Image>();
        private readonly List<GuideTopic> topicOrder = new List<GuideTopic>();
        private Text articleTitleText;
        private Text articleBodyText;
        private RectTransform articleContentRect;
        private Font uiFont;

        private static readonly Color TopicIdle = new Color(0.16f, 0.20f, 0.26f, 0.96f);
        private static readonly Color TopicSelected = new Color(0.92f, 0.58f, 0.12f, 1f);

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
            if (canvasObj != null && canvasObj.activeSelf)
            {
                BuildUI();
            }
        }

        public void ShowModal()
        {
            currentTopic = GuideTopic.Overview;
            BuildUI();
        }

        public void HideModal()
        {
            if (canvasObj != null) Destroy(canvasObj);
            canvasObj = null;
            topicButtonImages.Clear();
            topicOrder.Clear();
            articleTitleText = null;
            articleBodyText = null;
            articleContentRect = null;
        }

        private void BuildUI()
        {
            GuideTopic keepTopic = currentTopic;
            if (canvasObj != null) Destroy(canvasObj);
            topicButtonImages.Clear();
            topicOrder.Clear();

            canvasObj = new GameObject("HowToPlay_Modal_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;
            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);
            bdImg.raycastTarget = true;

            GameObject panelObj = new GameObject("HowToPlay_Panel");
            panelObj.transform.SetParent(backdrop.transform, false);
            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(1180f, 720f);
            Image pBg = panelObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(1180, 720, 18, 3, new Color(0.95f, 0.65f, 0.15f), new Color(0.09f, 0.12f, 0.16f, 0.98f));

            CreateLabel(panelObj.transform, new Vector2(0f, 318f), new Vector2(900f, 44f),
                LocalizationManager.L("GuideLib_Title", "REHBER KÜTÜPHANESİ", "GUIDE LIBRARY"),
                26, FontStyle.Bold, new Color(0.95f, 0.65f, 0.15f), TextAnchor.MiddleCenter);

            CreateLabel(panelObj.transform, new Vector2(0f, 280f), new Vector2(980f, 28f),
                LocalizationManager.L("GuideLib_Subtitle", "Merak ettiğin konuya bas, adım adım öğren.", "Tap a topic to learn it step by step."),
                15, FontStyle.Normal, new Color(0.78f, 0.82f, 0.88f), TextAnchor.MiddleCenter);

            UIStyleUtility.CreateCornerCloseButton(panelObj.transform, HideModal, 52f);

            BuildTopicList(panelObj.transform);
            BuildArticlePane(panelObj.transform);
            SelectTopic(keepTopic);
        }

        private void BuildTopicList(Transform parent)
        {
            GameObject scrollObj = new GameObject("TopicScroll");
            scrollObj.transform.SetParent(parent, false);
            RectTransform sRect = scrollObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(-392f, -28f);
            sRect.sizeDelta = new Vector2(340f, 560f);
            Image sBg = scrollObj.AddComponent<Image>();
            sBg.color = new Color(0.07f, 0.09f, 0.12f, 0.75f);
            sBg.raycastTarget = true;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = new Vector2(8f, 8f);
            vpRect.offsetMax = new Vector2(-8f, -8f);
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRect;
            scroll.content = cRect;

            GuideTopic[] topics =
            {
                GuideTopic.Overview,
                GuideTopic.Controls,
                GuideTopic.Tablet,
                GuideTopic.Passport,
                GuideTopic.TownContracts,
                GuideTopic.TaxiStand,
                GuideTopic.Brand,
                GuideTopic.EndOfDay,
                GuideTopic.Inspector,
                GuideTopic.Farm,
                GuideTopic.Barn,
                GuideTopic.Store,
                GuideTopic.Wholesale,
                GuideTopic.Checkout,
                GuideTopic.Staff,
                GuideTopic.Furniture,
                GuideTopic.Workshop,
                GuideTopic.JamMaker,
                GuideTopic.JuicePress,
                GuideTopic.Cannery,
                GuideTopic.Dehydrator,
                GuideTopic.OilPress,
                GuideTopic.SaladStation,
                GuideTopic.Finance,
                GuideTopic.Expansion
            };

            for (int i = 0; i < topics.Length; i++)
            {
                GuideTopic topic = topics[i];
                topicOrder.Add(topic);

                GameObject btnObj = new GameObject("Topic_" + topic);
                btnObj.transform.SetParent(content.transform, false);
                LayoutElement le = btnObj.AddComponent<LayoutElement>();
                le.minHeight = 52f;
                le.preferredHeight = 52f;

                Image bg = btnObj.AddComponent<Image>();
                bg.sprite = UIStyleUtility.CreateRoundedPillSprite(300, 52, 12, TopicIdle);
                bg.raycastTarget = true;
                topicButtonImages.Add(bg);

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                    SelectTopic(topic);
                });

                GameObject txtObj = new GameObject("Label");
                txtObj.transform.SetParent(btnObj.transform, false);
                RectTransform tRect = txtObj.AddComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.offsetMin = new Vector2(10f, 0f);
                tRect.offsetMax = new Vector2(-10f, 0f);
                Text txt = txtObj.AddComponent<Text>();
                txt.font = uiFont;
                txt.text = GetTopicTitle(topic);
                txt.fontSize = 15;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = Color.white;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow = VerticalWrapMode.Overflow;
                txt.raycastTarget = false;
            }
        }

        private void BuildArticlePane(Transform parent)
        {
            GameObject pane = new GameObject("ArticlePane");
            pane.transform.SetParent(parent, false);
            RectTransform paneRect = pane.AddComponent<RectTransform>();
            paneRect.anchoredPosition = new Vector2(178f, -28f);
            paneRect.sizeDelta = new Vector2(760f, 560f);
            Image paneBg = pane.AddComponent<Image>();
            paneBg.color = new Color(0.07f, 0.09f, 0.12f, 0.80f);

            GameObject titleObj = new GameObject("ArticleTitle");
            titleObj.transform.SetParent(pane.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 246f);
            titleRect.sizeDelta = new Vector2(720f, 40f);
            articleTitleText = titleObj.AddComponent<Text>();
            articleTitleText.font = uiFont;
            articleTitleText.fontSize = 20;
            articleTitleText.fontStyle = FontStyle.Bold;
            articleTitleText.alignment = TextAnchor.MiddleLeft;
            articleTitleText.color = new Color(1f, 0.86f, 0.45f);
            articleTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            articleTitleText.verticalOverflow = VerticalWrapMode.Overflow;

            GameObject scrollObj = new GameObject("ArticleScroll");
            scrollObj.transform.SetParent(pane.transform, false);
            RectTransform sRect = scrollObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, -24f);
            sRect.sizeDelta = new Vector2(736f, 496f);

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            articleContentRect = content.AddComponent<RectTransform>();
            articleContentRect.anchorMin = new Vector2(0f, 1f);
            articleContentRect.anchorMax = new Vector2(1f, 1f);
            articleContentRect.pivot = new Vector2(0.5f, 1f);
            articleContentRect.sizeDelta = new Vector2(0f, 40f);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 24);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject bodyObj = new GameObject("ArticleBody");
            bodyObj.transform.SetParent(content.transform, false);
            articleBodyText = bodyObj.AddComponent<Text>();
            articleBodyText.font = uiFont;
            articleBodyText.fontSize = 16;
            articleBodyText.alignment = TextAnchor.UpperLeft;
            articleBodyText.color = new Color(0.93f, 0.95f, 0.97f);
            articleBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            articleBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            articleBodyText.supportRichText = true;
            articleBodyText.lineSpacing = 1.08f;
            articleBodyText.raycastTarget = false;

            ContentSizeFitter bodyFit = bodyObj.AddComponent<ContentSizeFitter>();
            bodyFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRect;
            scroll.content = articleContentRect;
        }

        private void SelectTopic(GuideTopic topic)
        {
            currentTopic = topic;
            if (articleTitleText != null) articleTitleText.text = GetTopicTitle(topic);
            if (articleBodyText != null) articleBodyText.text = GetTopicBody(topic);
            if (articleContentRect != null) articleContentRect.anchoredPosition = new Vector2(0f, 0f);

            for (int i = 0; i < topicButtonImages.Count; i++)
            {
                if (topicButtonImages[i] == null) continue;
                bool selected = i < topicOrder.Count && topicOrder[i] == topic;
                Color col = selected ? TopicSelected : TopicIdle;
                topicButtonImages[i].sprite = UIStyleUtility.CreateRoundedPillSprite(300, 52, 12, col);
                topicButtonImages[i].color = Color.white;
            }
        }

        private static string GetTopicTitle(GuideTopic topic)
        {
            switch (topic)
            {
                case GuideTopic.Overview: return LocalizationManager.L("GuideT_Overview", "Başlangıç Özeti", "Getting Started");
                case GuideTopic.Controls: return LocalizationManager.L("GuideT_Controls", "Kontroller", "Controls");
                case GuideTopic.Tablet: return LocalizationManager.L("GuideT_Tablet", "EKT Tablet", "EKT Tablet");
                case GuideTopic.Passport: return LocalizationManager.L("GuideT_Passport", "Ürün Pasaportu", "Product Passport");
                case GuideTopic.TownContracts: return LocalizationManager.L("GuideT_Contracts", "Kasaba Kontratları", "Town Contracts");
                case GuideTopic.TaxiStand: return LocalizationManager.L("GuideT_Taxi", "Taksi Durağı", "Taxi Stand");
                case GuideTopic.Brand: return LocalizationManager.L("GuideT_Brand", "Marka ve Tabela", "Brand & Sign");
                case GuideTopic.EndOfDay: return LocalizationManager.L("GuideT_EOD", "Gün Sonu Defteri", "End of Day Ledger");
                case GuideTopic.Inspector: return LocalizationManager.L("GuideT_Inspector", "Mevsim Müfettişi", "Season Inspector");
                case GuideTopic.Farm: return LocalizationManager.L("GuideT_Farm", "Çiftlik ve Hasat", "Farm & Harvest");
                case GuideTopic.Barn: return LocalizationManager.L("GuideT_Barn", "Ahır ve Sevkiyat", "Barn & Shipping");
                case GuideTopic.Store: return LocalizationManager.L("GuideT_Store", "Dükkan ve Reyonlar", "Store & Shelves");
                case GuideTopic.Wholesale: return LocalizationManager.L("GuideT_Wholesale", "Toptancı Siparişi", "Wholesale Orders");
                case GuideTopic.Checkout: return LocalizationManager.L("GuideT_Checkout", "Kasa ve Müşteriler", "Checkout & Customers");
                case GuideTopic.Staff: return LocalizationManager.L("GuideT_Staff", "Personel", "Staff");
                case GuideTopic.Furniture: return LocalizationManager.L("GuideT_Furniture", "Mobilya ve Dekor", "Furniture & Decor");
                case GuideTopic.Workshop: return LocalizationManager.L("GuideT_Workshop", "Atölye Sistemi", "Workshop System");
                case GuideTopic.JamMaker: return LocalizationManager.L("GuideT_Jam", "Reçel Kazanı", "Jam Boiler");
                case GuideTopic.JuicePress: return LocalizationManager.L("GuideT_Juice", "Sıkma Presi", "Juice Press");
                case GuideTopic.Cannery: return LocalizationManager.L("GuideT_Cannery", "Konserve Ünitesi", "Cannery");
                case GuideTopic.Dehydrator: return LocalizationManager.L("GuideT_Dry", "Kurutma Fırını", "Dehydrator");
                case GuideTopic.OilPress: return LocalizationManager.L("GuideT_Oil", "Yağ Presi", "Oil Press");
                case GuideTopic.SaladStation: return LocalizationManager.L("GuideT_Salad", "Salata İstasyonu", "Salad Station");
                case GuideTopic.Finance: return LocalizationManager.L("GuideT_Finance", "Para ve Zaman", "Money & Time");
                case GuideTopic.Expansion: return LocalizationManager.L("GuideT_Expand", "Büyüme ve Seviyeler", "Growth & Upgrades");
                default: return "";
            }
        }

        private static string GetTopicBody(GuideTopic topic)
        {
            switch (topic)
            {
                case GuideTopic.Overview: return GetOverviewBody();
                case GuideTopic.Controls: return GetControlsBody();
                case GuideTopic.Tablet: return GetTabletBody();
                case GuideTopic.Passport: return GetPassportBody();
                case GuideTopic.TownContracts: return GetTownContractsBody();
                case GuideTopic.TaxiStand: return GetTaxiStandBody();
                case GuideTopic.Brand: return GetBrandBody();
                case GuideTopic.EndOfDay: return GetEndOfDayBody();
                case GuideTopic.Inspector: return GetInspectorBody();
                case GuideTopic.Farm: return GetFarmBody();
                case GuideTopic.Barn: return GetBarnBody();
                case GuideTopic.Store: return GetStoreBody();
                case GuideTopic.Wholesale: return GetWholesaleBody();
                case GuideTopic.Checkout: return GetCheckoutBody();
                case GuideTopic.Staff: return GetStaffBody();
                case GuideTopic.Furniture: return GetFurnitureBody();
                case GuideTopic.Workshop: return GetWorkshopOverviewBody();
                case GuideTopic.JamMaker: return BuildMachineArticle(WorkshopMachineType.JamMaker);
                case GuideTopic.JuicePress: return BuildMachineArticle(WorkshopMachineType.JuiceExtractor);
                case GuideTopic.Cannery: return BuildMachineArticle(WorkshopMachineType.Cannery);
                case GuideTopic.Dehydrator: return BuildMachineArticle(WorkshopMachineType.Dehydrator);
                case GuideTopic.OilPress: return BuildMachineArticle(WorkshopMachineType.OilPress);
                case GuideTopic.SaladStation: return BuildMachineArticle(WorkshopMachineType.SaladStation);
                case GuideTopic.Finance: return GetFinanceBody();
                case GuideTopic.Expansion: return GetExpansionBody();
                default: return "";
            }
        }

        private static string GetOverviewBody()
        {
            return LocalizationManager.L(
                "GuideB_Overview",
                "<b>Farm2Shelf</b> mahallenin gıda markasını sen kurarsın. Hasadı izle, işle, vitrine koy, kasabaya kuryeyle teslim et.\n\n" +
                "<b>İlk gün kısa yol:</b>\n" +
                "1. Yeni oyunda kimlik, renk ve slogan seç; tabelan buna boyanır.\n" +
                "2. Kamerayı dene, EKT tabletin 7 uygulamasını aç.\n" +
                "3. Online Market ➔ Kontratlar: bir teklifi sıraya al.\n" +
                "4. Personel, reyon, toptan sipariş; rafa ürün ata (pasaport etiketi).\n" +
                "5. Tohum ek, dükkanı aç. Gece defteri bugünü yazar, yalnızca Ertesi güne atla 06:00'ya götürür.\n\n" +
                "Soldaki konulardan pasaport, <b>kontratlar</b>, <b>taksi durağı</b>, marka ve gün sonunu da oku.",
                "<b>Farm2Shelf</b> is you building the neighborhood food brand. Trace harvest, process it, put it on the shelf, deliver it by courier.\n\n" +
                "<b>First-day shortcut:</b>\n" +
                "1. Pick identity, color, and slogan; your sign follows.\n" +
                "2. Learn the camera, open all 7 EKT apps.\n" +
                "3. Online Market ➔ Contracts: queue one offer.\n" +
                "4. Staff, shelves, wholesale; assign products (passport label).\n" +
                "5. Plant seeds, open the store. The night ledger writes the day; only Skip to next day jumps to 06:00.\n\n" +
                "Use the left topics for passports, <b>contracts</b>, <b>taxi stand</b>, brand, and end of day."
            );
        }

        private static string GetControlsBody()
        {
            return LocalizationManager.L(
                "GuideB_Controls",
                "<b>Mobil</b>\n" +
                "• Haritayı kaydır: tek parmak sürükle.\n" +
                "• Yakınlaştır / uzaklaştır: iki parmakla kıstır-aç.\n" +
                "• Kamerayı döndür: iki parmağı dairesel çevir.\n" +
                "• Nesne seç: tarla, raf, koli, personel veya butona dokun.\n\n" +
                "<b>PC</b>\n" +
                "• Kaydır: farenin sol tuşuyla sürükle veya WASD / ok tuşları.\n" +
                "• Yakınlaştır: fare tekerleği.\n" +
                "• Döndür: farenin sağ tuşu veya iki parmak jesti (dokunmatik ekranda).\n" +
                "• Seç / onayla: sol tık.\n" +
                "• Mobilya döndür: <b>R</b> veya ekrandaki Döndür butonu.\n\n" +
                "HUD üstte para, gün, saat ve dükkan açık/kapalı durumunu gösterir. Altta EKT Tablet her zaman elinin altındadır.",
                "<b>Mobile</b>\n" +
                "• Pan: drag with one finger.\n" +
                "• Zoom: pinch / spread.\n" +
                "• Rotate camera: twist two fingers.\n" +
                "• Select: tap a field, shelf, box, staff member, or UI button.\n\n" +
                "<b>PC</b>\n" +
                "• Pan: left-drag or WASD / arrow keys.\n" +
                "• Zoom: mouse wheel.\n" +
                "• Rotate: right-drag, or two-finger twist on a touch screen.\n" +
                "• Select / confirm: left click.\n" +
                "• Rotate furniture: <b>R</b> or the on-screen Rotate button.\n\n" +
                "The HUD shows money, day, time, and open/closed status. The EKT Tablet stays at the bottom of the screen."
            );
        }

        private static string GetTabletBody()
        {
            return LocalizationManager.L(
                "GuideB_Tablet",
                "Sağ alttaki <b>📱 EKT TABLET</b> işletmenin beynidir. Yedi uygulama vardır:\n\n" +
                "🛒 <b>Mağaza Yönetimi</b> — Personel, vardiya, erken çağır.\n" +
                "🌾 <b>Çiftlik</b> — Çiftçi ve tarla.\n" +
                "🛍️ <b>Alışveriş</b> — Tohum, mobilya, toptan, araç.\n" +
                "💳 <b>Finans</b> — Gelir-gider ve nakit.\n" +
                "𝕏 <b>Sosyal</b> — Tweet'ler, marka sloganı, itibar.\n" +
                "🏭 <b>Atölye</b> — Makineler ve gurme üretim.\n" +
                "🌐 <b>Online Market</b> — Filo, kurye ve <b>Kasaba Kontratları</b>.\n" +
                "🚕 <b>İşler</b> — Kuzeydoğu <b>Taksi Durağı</b> (bağımsız ek gelir).\n\n" +
                "Yeni oyunda eğitim bu tableti adım adım açtırır.",
                "The <b>📱 EKT TABLET</b> at the bottom right is HQ. It has seven apps:\n\n" +
                "🛒 <b>Store Management</b> — Hire, shifts, call early.\n" +
                "🌾 <b>Farm</b> — Farmers and fields.\n" +
                "🛍️ <b>Shopping</b> — Seeds, furniture, wholesale, vehicles.\n" +
                "💳 <b>Finance</b> — Income, spend, cash.\n" +
                "𝕏 <b>Social</b> — Tweets, brand slogan, reputation.\n" +
                "🏭 <b>Workshop</b> — Machines and gourmet output.\n" +
                "🌐 <b>Online Market</b> — Fleet, courier, and <b>Town Contracts</b>.\n" +
                "🚕 <b>Jobs</b> — Northeast <b>Taxi Stand</b> (independent extra income).\n\n" +
                "The tutorial walks these apps on a new game."
            );
        }

        private static string GetPassportBody()
        {
            return LocalizationManager.L(
                "GuideB_Passport",
                "Her stok yığını sadece isim + adet değildir. <b>Pasaport</b> şunu taşır: nereden geldi (tarla / ahır / atölye / toptan), hangi gün ve saatte, hangi havada.\n\n" +
                "Rafta küçük etiket bunu gösterir. Kasa fişinde <b>Yerel hasat</b> veya <b>Toptan</b> yazar. Taze hasat daha pahalı satılır; bayatlayan lot şikayet tweet'i üretir.\n\n" +
                "Markan da fiyatı çarpar: yerel üretici hasada prim, mahalle marketi toptana daha yumuşak, gurme atölye işlenmiş ürüne prim verir.",
                "A stock pile is not only name + count. The <b>passport</b> stores origin (field / barn / workshop / wholesale), day, hour, and weather.\n\n" +
                "The shelf label shows it. The receipt prints <b>Local harvest</b> or <b>Wholesale</b>. Fresh harvest sells higher; stale lots spark complaint tweets.\n\n" +
                "Your brand multiplies price too: local producer boosts harvest, neighborhood market is kinder to wholesale, gourmet workshop boosts crafted lots."
            );
        }

        private static string GetTownContractsBody()
        {
            return LocalizationManager.L(
                "GuideB_Contracts",
                "<b>Nereye bakılır?</b>\n" +
                "EKT Tablet ➔ <b>Online Market ➔ Kontratlar</b>.\n\n" +
                "<b>Ne işe yarar?</b>\n" +
                "Kasaba binaları (apartman, kafe, cami, belediye, taksi ofisi, güneydoğu parseller…) her gün <b>5 teklif</b> asar. Kalıcı imza yoktur. Bitirdiğin veya <b>Geç</b> dediğin teklif o gün yenilenmez; yeni 5’li ertesi gün gelir.\n\n" +
                "<b>Nasıl alınır?</b>\n" +
                "1. Teklif kartında uzaklık (m), <b>kontrat ücreti</b>, ürün listesi ve primleri oku.\n" +
                "2. Karlı görürsen <b>Sıraya Al</b>. İstemezsen <b>Geç</b>.\n" +
                "3. Filo sekmesinden satın alınmış bir motoru <b>Kontratlara Ekle</b>. Bu motor yalnızca kontrat kuyruğuna çalışır.\n" +
                "4. Motor tek iş taşır: yükler, adrese gider, dükkana dönüp park eder; sonra sıradaki kontrat yüklenir.\n\n" +
                "<b>Para (ekransız tutar = ödenen tutar)</b>\n" +
                "• Eksiksiz teslimatta karttaki <b>kontrat ücreti</b> aynen ödenir.\n" +
                "• Aynı anda kartta yazan <b>mesafe primi</b> eklenir.\n" +
                "• Ürünler yerel hasatsa (çiftlik / gurme) ve gerçekten toplanıp gittiyse <b>+40 C yerel ürün primi</b> eklenir.\n" +
                "• Eksik teslimatta kontrat ücreti ve primler yazılmaz; kontrat bozulur.\n\n" +
                "<b>Finans’ta nasıl görünür?</b>\n" +
                "Hepsi <b>Kasaba Kontratları</b> kategorisindedir. Online siparişle karışmaz. Satırlar ayrıdır: kontrat ücreti, mesafe primi, yerel ürün primi. Böylece kazancı ayırt edersin.\n\n" +
                "<b>Kurallar</b>\n" +
                "• Dükkan kapalıyken motor yola çıkmaz; saat akmıyorsa çağrı da ilerlemez.\n" +
                "• Kontrat motoru online market sırasına karışmaz.\n" +
                "• Stok yoksa teslimat eksik kalır — rafta / depoda ürün bulundur.\n" +
                "• Eğitimde bir teklifi sıraya almak yeter.",
                "<b>Where?</b>\n" +
                "EKT Tablet ➔ <b>Online Market ➔ Contracts</b>.\n\n" +
                "<b>What is it?</b>\n" +
                "Town buildings (apartments, cafes, mosque, town hall, taxi office, southeast lots…) post <b>5 offers</b> each day. There is no permanent signature. Finished or <b>Skipped</b> offers do not refresh that day; a new set of 5 arrives tomorrow.\n\n" +
                "<b>How to take one</b>\n" +
                "1. Read distance (m), the <b>contract fee</b>, the product list, and the bonuses on the card.\n" +
                "2. <b>Queue It</b> if it looks profitable, or <b>Skip</b>.\n" +
                "3. On the Fleet tab, <b>Add to Contracts</b> an owned motorcycle. That bike only runs the contract queue.\n" +
                "4. A contract bike carries one job: load, deliver, return and park; then the next contract loads.\n\n" +
                "<b>Pay (the card amount is what you receive)</b>\n" +
                "• On a full delivery you are paid the <b>contract fee</b> shown on the card.\n" +
                "• The card’s <b>distance bonus</b> is added at the same time.\n" +
                "• If the goods are local harvest (farm / gourmet) and were actually picked and delivered, you also get a <b>+40 C local-goods bonus</b>.\n" +
                "• A short delivery pays no fee and no bonuses; the contract fails.\n\n" +
                "<b>How it looks in Finance</b>\n" +
                "Everything is posted under <b>Town Contracts</b>, never as an online order. Lines are split: contract fee, distance bonus, local-goods bonus — so you can tell the money apart.\n\n" +
                "<b>Rules</b>\n" +
                "• Bikes do not leave while the store is closed; if time is paused, work does not advance.\n" +
                "• A contract bike does not mix with the online-market queue.\n" +
                "• Missing stock means a short delivery — keep product on shelves / in storage.\n" +
                "• In the tutorial, queuing one offer is enough."
            );
        }

        private static string GetTaxiStandBody()
        {
            return LocalizationManager.L(
                "GuideB_Taxi",
                "<b>Nereye bakılır?</b>\n" +
                "EKT Tablet ➔ <b>İşler</b>. Haritada durak, kuzeydoğu mahallede (otoyolun üstü, sarı park yerleri) durur.\n\n" +
                "<b>Bağımsız iş</b>\n" +
                "Taksi durağı dükkan, çiftlik ve atölyeden <b>tamamen ayrıdır</b>. Seviye atlamak, reyon kurmak veya atölye büyütmek durak fiyatını, vardiyayı veya geliri değiştirmez; taksiyi bozmaz.\n\n" +
                "<b>Nasıl açılır?</b>\n" +
                "1. İşler uygulamasında durağı satın al (<b>45.000 C</b>). Personel gerekmez.\n" +
                "2. Sayfanın altından sarı taksi al (<b>9.000 C</b>, en fazla 5). Her taksi kendi sarı park yuvasında bekler.\n" +
                "3. Dükkanı aç; saat <b>08:00–22:00</b> arasında çağrı gelir.\n\n" +
                "<b>Bir yolculuk nasıl işler?</b>\n" +
                "• Boştaki taksi sarı yerden çıkar, kasabadaki bir müşteriyi alır.\n" +
                "• Onu başka bir adrese bırakır.\n" +
                "• Ücret mesafeye göredir (yaklaşık 50–850 C).\n" +
                "• Taksi kendi park yuvasına döner ve bir sonraki çağrıyı bekler.\n\n" +
                "<b>22:00 kuralı</b>\n" +
                "Yeni çağrı kesilir. Taksi yoldaysa yolcuyu bırakır, sonra durağa döner ve park eder. Gece boyunca sarı yerde bekler.\n\n" +
                "<b>Finans</b>\n" +
                "• Durak ve taksi alımları gider: kategori <b>Taksi</b>.\n" +
                "• Her tamamlanan yolculuk gelir: kategori <b>Taksi Geliri</b>.\n" +
                "Tablet ➔ Finans ➔ özet ve işlem geçmişinde, gün sonu defterinde de görünür. Dükkan cirosu ve kontratlarla karışmaz.\n\n" +
                "<b>İpuçları</b>\n" +
                "• Saat yalnızca dükkan açıkken akar; durak da o zaman çalışır.\n" +
                "• Ne kadar çok taksin varsa o kadar çok çağrı karşılanır.\n" +
                "• İşler ekranında vardiya, bugünkü yolculuk/gelir ve her taksinin (park / yolda / dönüş) durumunu izlersin.",
                "<b>Where?</b>\n" +
                "EKT Tablet ➔ <b>Jobs</b>. On the map the stand sits in the northeast district (above the highway, yellow bays).\n\n" +
                "<b>Independent business</b>\n" +
                "The taxi stand is <b>fully separate</b> from the store, farm, and workshop. Upgrading those does not change stand price, shift hours, or fares, and it will not break the taxis.\n\n" +
                "<b>How to open it</b>\n" +
                "1. In Jobs, buy the stand (<b>45,000 C</b>). No staff required.\n" +
                "2. Buy a yellow taxi at the bottom of the page (<b>9,000 C</b>, max 5). Each taxi waits in its own yellow bay.\n" +
                "3. Open the store; calls run from <b>08:00 to 22:00</b>.\n\n" +
                "<b>How a trip works</b>\n" +
                "• An idle taxi leaves its yellow bay and picks up a rider in town.\n" +
                "• It drops them at another address.\n" +
                "• The fare is based on distance (about 50–850 C).\n" +
                "• The taxi returns to its own bay and waits for the next call.\n\n" +
                "<b>22:00 rule</b>\n" +
                "New calls stop. If a taxi is on the road it drops the rider, then returns to the stand and parks. It stays in the yellow bay overnight.\n\n" +
                "<b>Finance</b>\n" +
                "• Stand and taxi purchases are expenses under <b>Taxi</b>.\n" +
                "• Each finished ride is income under <b>Taxi Income</b>.\n" +
                "You will see them in Tablet ➔ Finance (summary and history) and on the end-of-day ledger. They do not mix with store sales or town contracts.\n\n" +
                "<b>Tips</b>\n" +
                "• Time only flows while the store is open, so the stand only works then.\n" +
                "• More taxis cover more calls.\n" +
                "• The Jobs screen shows shift status, trips/income today, and each taxi (parked / on a job / returning)."
            );
        }

        private static string GetBrandBody()
        {
            return LocalizationManager.L(
                "GuideB_Brand",
                "Yeni oyunda üç kimlikten birini seçersin: <b>Yerel üretici</b>, <b>Ekonomik mahalle marketi</b>, <b>Gurme atölye</b>. Renk ve kısa slogan tabelaya, vitrin ışığına ve sosyal profile işler.\n\n" +
                "Harita geometrisi değişmez. Kimlik satış fiyatını ve müşterinin pahalı tepkisini etkiler. Tweet tonu da markaya göre kayar.",
                "On a new game you pick one identity: <b>Local producer</b>, <b>Neighborhood value market</b>, or <b>Gourmet workshop</b>. Color and slogan tint the sign, interior light, and social profile.\n\n" +
                "Map geometry does not change. Identity changes sale price and how shoppers react to high prices. Tweet tone follows the brand."
            );
        }

        private static string GetEndOfDayBody()
        {
            return LocalizationManager.L(
                "GuideB_EOD",
                "Saat 24:00'te dükkan kapanır. Müşteri ve yoldaki kurye bitince <b>gün sonu defteri</b> açılır.\n\n" +
                "Sayfada bugünün hikâyesi durur: kasa, pasaport karışımı, kurye, kontrat, gider, tweet, bayat stok, net kâr. Alttaki yeşil <b>Ertesi güne atla</b> saati 06:00'ya alır. Başka bir yere basmak defteri kapatmaz.",
                "At 24:00 the store closes. When customers and road couriers finish, the <b>end-of-day ledger</b> opens.\n\n" +
                "It writes today's story: till, passport mix, courier, contracts, spend, tweets, stale stock, net. The green <b>Skip to next day</b> button jumps to 06:00. Tapping anywhere else will not close it."
            );
        }

        private static string GetInspectorBody()
        {
            return LocalizationManager.L(
                "GuideB_Inspector",
                "Her mevsimin <b>30. günü</b> takvimde 🕴️ müfettiş görünür. Üstüne basınca notlarını okursun.\n\n" +
                "Dükkan açıksa sabah <b>10:00</b>'da takım elbiseli, jöleli saçlı müfettiş müşteri gibi girer, rafları gezer ve not alır. Denetim <b>11:00</b>'de biter; kapıdan çıkıp gider.\n" +
                "• Dükkan temiz mi?\n• Raflar dolu mu, boş mu?\n• Fiyatlar uygun mu?\n\n" +
                "Uyan her madde <b>cüzi ödül</b> (+120C), uymayan her madde <b>ağır ceza</b> (−450C) getirir. Dükkan o gün hiç açılmazsa üç madde de ceza yazar.\n\n" +
                "Harita değişmez. Müfettiş normal yaya girişini kullanır.",
                "On day <b>30</b> of each season the calendar shows a 🕴️ inspector. Tap him to read the notes.\n\n" +
                "If the store is open he arrives at <b>10:00</b> in a suit and gelled hair, walks the aisles like a shopper, and scores. The visit ends at <b>11:00</b>; he walks out the door.\n" +
                "• Is the store clean?\n• Are shelves stocked or empty?\n• Are prices fair?\n\n" +
                "Each pass pays a <b>small bonus</b> (+120C); each fail a <b>heavy fine</b> (−450C). If you never open that day, all three fail.\n\n" +
                "The map does not change. He uses the normal pedestrian entrance."
            );
        }

        private static string GetFarmBody()
        {
            return LocalizationManager.L(
                "GuideB_Farm",
                "Çiftlik dükkanın sağındaki tarla alanıdır.\n\n" +
                "1. Tablette <b>Alışveriş ➔ Tohumlar</b> ile mevsime uygun tohum al.\n" +
                "2. Boş tarlaya dokun, tohumu seç, ek.\n" +
                "3. Mahsul büyür. Olgunlaşınca tarlaya tekrar dokunup hasat et.\n" +
                "4. Hasat <b>Ahır stoğuna</b> gider.\n\n" +
                "<b>Çiftçiler</b> ekim ve hasatı otomatikleştirir. Sabah (08:00–16:00) ve akşam (16:00–24:00) vardiyası vardır.\n\n" +
                "Mevsim değişince bazı tohumlar ekilemez. Sera / kış tohumları ayrıdır. Atölye tarifleri belirli mahsulleri ister; ekmeden önce Reçel, Konserve gibi sayfalara bak.",
                "The farm sits to the right of the store.\n\n" +
                "1. Buy seasonal seeds in Tablet <b>Shopping ➔ Seeds</b>.\n" +
                "2. Tap an empty plot, pick a seed, plant it.\n" +
                "3. When the crop is ripe, tap the plot again to harvest.\n" +
                "4. Harvest goes into <b>Barn storage</b>.\n\n" +
                "<b>Farmers</b> automate planting and harvest. Shifts are morning (08:00–16:00) and evening (16:00–24:00).\n\n" +
                "Some seeds cannot be planted out of season. Greenhouse / winter seeds are separate. Workshop recipes need specific crops — check the Jam, Cannery, and other pages before you plant."
            );
        }

        private static string GetBarnBody()
        {
            return LocalizationManager.L(
                "GuideB_Barn",
                "Ahıra dokununca stoğunu görürsün. Her mahsul için üç yol vardır:\n\n" +
                "🚛 <b>Markete gönder (%40 kâr)</b> — Yeşil çiftlik kamyonu mahsulü dükkan / depo stoğuna taşır. Reyonlara çiftlik ürünü koymak için en kârlı yoldur.\n" +
                "💵 <b>Anında sat (%20 kâr)</b> — Nakde çevirir, rafta yer kaplamaz. Acil nakit için iyidir.\n" +
                "🏭 <b>Atölyeye hammadde</b> — Mahsulü atölye paletine gönderirsin. Makineler buradaki kiloyu kullanır.\n\n" +
                "Kamyon yoldayken veya rampa doluyken yeni sevkiyat bekler. Kayıt yüklenince kamyon kaldığı yerden devam eder.\n\n" +
                "İpucu: Aynı mahsul hem rafta satılır hem atölyede işlenir. Gurme ürünler genelde daha pahalıya gider.",
                "Tap the barn to open storage. Each crop has three paths:\n\n" +
                "🚛 <b>Ship to store (+40% margin)</b> — The green farm truck moves crops into store / warehouse stock. Best profit if you will shelf farm goods.\n" +
                "💵 <b>Instant sell (+20%)</b> — Turns crops into cash. Use it when you need money now.\n" +
                "🏭 <b>Workshop feedstock</b> — Send kilos to the workshop pallet. Machines consume that stock.\n\n" +
                "If a truck is already on the road or the dock is busy, wait. Loaded saves resume the truck from where it stopped.\n\n" +
                "Tip: The same crop can be sold on shelves or refined. Gourmet outputs usually sell for more."
            );
        }

        private static string GetStoreBody()
        {
            return LocalizationManager.L(
                "GuideB_Store",
                "Dükkan sola, depo sağdaki bölmede, personel odası deponun arkasındadır.\n\n" +
                "<b>Reyon / dolap kurmak:</b> Teslimat paletindeki koliye dokun, hayalet önizlemeyi sürükle, <b>Kur</b>. Yeşil = uygun, kırmızı = çakışma. Depo rafları yalnızca depoya konur.\n\n" +
                "<b>Ürün atamak:</b> Rafa dokun ➔ her sıraya ürün seç ➔ Rafa koy. Reyoncu, depodaki kolileri bu sıraya taşır.\n\n" +
                "<b>Neon Duvar Saati</b> yalnızca duvara asılır. Tavan spotu reyonların üstüne renkli ışık düşürür.\n\n" +
                "Dükkan <b>KAPALI</b> iken müşteri girmez; stok ve düzen için kullan. Hazır olunca HUD’dan aç.\n\n" +
                "Boş veya yanlış ürünlü raf satış kaçırır. Manav rafı çiftlik mahsulüne, gurme reyonu atölye ürünlerine özeldir.",
                "The store is on the left, storage is the room to the right, and the staff room is behind storage.\n\n" +
                "<b>Place fixtures:</b> Tap a delivery box, drag the ghost, tap <b>Assemble</b>. Green = valid, red = blocked. Storage racks belong in the warehouse only.\n\n" +
                "<b>Assign products:</b> Tap a shelf ➔ pick an item per row ➔ place it. Restockers then move warehouse boxes onto that row.\n\n" +
                "The <b>Neon Wall Clock</b> hangs on walls only. Ceiling spotlights throw colored light onto aisles.\n\n" +
                "While the store is <b>CLOSED</b>, customers stay out — use that time to stock. Open it from the HUD when ready.\n\n" +
                "Empty or mismatched shelves lose sales. Produce displays are for farm crops; gourmet racks are for workshop goods."
            );
        }

        private static string GetWholesaleBody()
        {
            return LocalizationManager.L(
                "GuideB_Wholesale",
                "Toptancı, kendi üretmediğin market ürünlerini getirir (süt, atıştırmalık, içecek, temizlik vb.).\n\n" +
                "1. Tablet ➔ <b>Alışveriş ➔ Toplu Sipariş</b> veya ürün listesinden seç.\n" +
                "2. Mavi toptancı kamyonu mal kabul kapısına gelir.\n" +
                "3. Reyoncu kolileri indirir, depo raflarına dizer.\n" +
                "4. Sen reyonlara ürün atarsın; reyoncu raftan boşalanları doldurur.\n\n" +
                "Rampa meşgulse (yeşil veya mavi kamyon varken) yeni sipariş durur. Kayıt yüklenince kamyon kaldığı fazdan devam eder.\n\n" +
                "Çiftlik mahsulü yeşil kamyonla, toptan ürün mavi kamyonla gelir. İkisini karıştırma.",
                "Wholesale brings grocery goods you do not grow (dairy, snacks, drinks, household, and more).\n\n" +
                "1. Tablet ➔ <b>Shopping ➔ Bulk Order</b>, or pick items from the catalog.\n" +
                "2. The blue wholesale truck arrives at Goods Receipt.\n" +
                "3. Restockers unload boxes onto warehouse racks.\n" +
                "4. You assign products to store shelves; restockers refill empty rows.\n\n" +
                "If the dock is busy (green or blue truck), new orders wait. Saves resume the truck from its last phase.\n\n" +
                "Farm crops arrive on the green truck. Wholesale goods arrive on the blue truck. Do not mix the two flows."
            );
        }

        private static string GetCheckoutBody()
        {
            return LocalizationManager.L(
                "GuideB_Checkout",
                "Dükkan açıkken müşteriler girer, raftan ürün alır, kasaya gider.\n\n" +
                "• <b>Kasiyer</b> kuyruğu eritir. Kasiyer yoksa sen kasaya bakmak zorunda kalırsın ve satış yavaşlar.\n" +
                "• <b>Müşteri Hizmetleri masası</b> alışverişi hızlandırır ve ekstra ürün satışını artırır.\n" +
                "• Maskot ve vitrin dekoru itibarı / trafiği destekler.\n" +
                "• Hırsız çıkabilir; güvenlik personeli yakalar.\n\n" +
                "Boş raf = müşteri eli boş döner. Fiyat ve stok tablet + raf penceresinden izlenir.\n\n" +
                "Gün bitince defter gelir: ciro, gider, pasaport, kontrat, tweet. Maaşlar günde bir kez ödenir. Defteri yalnızca Ertesi güne atla kapatır.",
                "When the store is open, customers enter, take shelf items, and queue at checkout.\n\n" +
                "• <b>Cashiers</b> clear the line. With none hired, checkout crawls.\n" +
                "• A <b>Customer Service desk</b> speeds shopping and boosts extra purchases.\n" +
                "• Mascots and front decor help reputation / traffic.\n" +
                "• Shoplifters can appear; security staff stop them.\n\n" +
                "Empty shelves send customers home empty-handed. Prices and stock are on the tablet and shelf windows.\n\n" +
                "End of day shows revenue, costs, and profit. Wages are paid once per day."
            );
        }

        private static string GetStaffBody()
        {
            return LocalizationManager.L(
                "GuideB_Staff",
                "Tablet ➔ Mağaza veya Çiftlik ➔ <b>İşe Alım</b>.\n\n" +
                "🛒 <b>Kasiyer</b> — Kasayı çalıştırır.\n" +
                "📦 <b>Reyoncu</b> — Kamyon indirir, depodan rafa taşır.\n" +
                "🧹 <b>Temizlikçi</b> — Kir ve döküntüleri temizler.\n" +
                "🛡️ <b>Güvenlik</b> — Hırsızla ilgilenir.\n" +
                "💬 <b>Müşteri hizmetleri</b> — Kuyruğu ve ekstra satışı iyileştirir.\n" +
                "🎭 <b>Maskot</b> — Dikkat ve atmosfer.\n" +
                "🌾 <b>Çiftçi</b> — Eker ve hasat eder.\n\n" +
                "<b>Vardiya:</b> Sabah 08:00–16:00, Akşam 16:00–24:00. İhtiyacın olan saate dağıt.\n" +
                "<b>Erken çağır:</b> Vardiyası gelmemiş birini hemen sahaya sokar (ücretli).\n\n" +
                "Maaş her gün bir kez kesilir. İşe almadan önce nakit bırak.",
                "Tablet ➔ Store or Farm ➔ <b>Hire</b>.\n\n" +
                "🛒 <b>Cashier</b> — Runs the register.\n" +
                "📦 <b>Restocker</b> — Unloads trucks and fills shelves from storage.\n" +
                "🧹 <b>Janitor</b> — Cleans messes.\n" +
                "🛡️ <b>Security</b> — Handles shoplifters.\n" +
                "💬 <b>Customer service</b> — Speeds shopping and extra sales.\n" +
                "🎭 <b>Mascot</b> — Attention and atmosphere.\n" +
                "🌾 <b>Farmer</b> — Plants and harvests.\n\n" +
                "<b>Shifts:</b> Morning 08:00–16:00, Evening 16:00–24:00. Cover the hours you need.\n" +
                "<b>Call early:</b> Brings someone in before their shift (paid).\n\n" +
                "Wages are deducted once per day. Keep cash before hiring."
            );
        }

        private static string GetFurnitureBody()
        {
            return LocalizationManager.L(
                "GuideB_Furniture",
                "Tablet ➔ Alışveriş ➔ <b>Mobilyalar / Dekor</b>. Sipariş teslimat paletine koli olarak düşer.\n\n" +
                "<b>Mağaza:</b> Standart reyon, manav, gurme reyon, buzdolabı, dondurucu, kasa, sepet standı, müşteri masası.\n" +
                "<b>Depo:</b> Depo rafları yalnızca depo odasına kurulur.\n" +
                "<b>Atölye:</b> Reçel kazanı, pres, konserve, fırın, yağ presi, salata ünitesi yalnızca atölye binasına konur.\n\n" +
                "Yerleştirirken zemindeki ızgara ve renkli hayalet sana yol gösterir. Kurulmuş mobilyaya tekrar dokunup taşıyabilir veya içeriğini düzenleyebilirsin.\n\n" +
                "Duvar saati duvara yapışır. Halı ve paspas yere serilir, üzerine yürünür.",
                "Tablet ➔ Shopping ➔ <b>Furniture / Decor</b>. Orders arrive as boxes on the delivery pallet.\n\n" +
                "<b>Store:</b> Standard / produce / gourmet shelves, fridge, freezer, register, cart stand, service desk.\n" +
                "<b>Warehouse:</b> Storage racks go in the warehouse only.\n" +
                "<b>Workshop:</b> Jam boiler, press, cannery, oven, oil press, and salad station go in the workshop building only.\n\n" +
                "The floor grid and colored ghost preview guide placement. Tap placed furniture again to move it or edit contents.\n\n" +
                "The wall clock mounts on walls. Mats and carpets sit on the floor and can be walked on."
            );
        }

        private static string GetWorkshopOverviewBody()
        {
            bool en = LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(LocalizationManager.L(
                "GuideB_WorkshopIntro",
                "<b>Atölye</b> haritanın solundaki ayrı binadır. Çiftlik mahsulünü yüksek fiyatlı gurme ürüne çevirir.\n\n" +
                "<b>Akış:</b>\n" +
                "1. Mahsulü hasat et (Ahır).\n" +
                "2. Ahırdan <b>atölye paletine</b> kilo gönder.\n" +
                "3. Atölyeye makine kur (Alışveriş ➔ Mobilya / Atölye).\n" +
                "4. Makineye dokun, tarifi seç, üret.\n" +
                "5. Bitince topla; ürünler stoğa düşer, gurme reyona konur.\n\n" +
                "Her tarif <b>belirli bir mahsul + kilo</b> ister. Palette o mahsul yoksa üretim başlamaz.\n\n" +
                "<b>Makineler ve hammaddeler:</b>\n\n",
                "The <b>workshop</b> is the separate building on the left. It turns farm crops into higher-priced gourmet goods.\n\n" +
                "<b>Flow:</b>\n" +
                "1. Harvest into the Barn.\n" +
                "2. Send kilos from the Barn to the <b>workshop pallet</b>.\n" +
                "3. Place a machine (Shopping ➔ Furniture / Workshop).\n" +
                "4. Tap the machine, pick a recipe, start production.\n" +
                "5. Collect when ready; stock the gourmet shelf.\n\n" +
                "Each recipe needs a <b>specific crop and weight</b>. Production will not start if the pallet is short.\n\n" +
                "<b>Machines and ingredients:</b>\n\n"
            ));

            foreach (WorkshopMachineDef machine in WorkshopMachineDatabase.GetAllMachines())
            {
                sb.Append("<b>").Append(machine.LocalizedName).Append("</b>\n");
                List<WorkshopRecipeDef> recipes = WorkshopMachineDatabase.GetRecipesForMachine(machine.type);
                HashSet<string> seen = new HashSet<string>();
                List<string> crops = new List<string>();
                for (int i = 0; i < recipes.Count; i++)
                {
                    string cropName = GetCropDisplayName(recipes[i].cropId);
                    if (seen.Add(cropName)) crops.Add(cropName);
                }
                sb.Append(en ? "Uses: " : "Kullanır: ");
                sb.Append(string.Join(en ? ", " : ", ", crops));
                sb.Append("\n\n");
            }

            sb.Append(LocalizationManager.L(
                "GuideB_WorkshopOutro",
                "Tek bir makinenin tüm tarifleri için soldan o makine butonuna bas.",
                "Open a machine button on the left for every recipe on that unit."
            ));
            return sb.ToString();
        }

        private static string BuildMachineArticle(WorkshopMachineType type)
        {
            WorkshopMachineDef machine = WorkshopMachineDatabase.GetMachineByType(type);
            List<WorkshopRecipeDef> recipes = WorkshopMachineDatabase.GetRecipesForMachine(type);
            bool en = LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (machine != null)
            {
                sb.Append("<b>").Append(machine.LocalizedName).Append("</b>\n");
                sb.Append(machine.LocalizedDesc).Append("\n\n");
                sb.Append(en
                    ? "Place this machine only inside the workshop. Feed it from the workshop pallet.\n\n"
                    : "Bu makine yalnızca atölye binasına kurulur. Hammadde atölye paletinden gelir.\n\n");
            }

            sb.Append(en
                ? "<b>Recipes (crop ➜ gourmet product)</b>\n\n"
                : "<b>Tarifler (mahsul ➜ gurme ürün)</b>\n\n");

            for (int i = 0; i < recipes.Count; i++)
            {
                WorkshopRecipeDef r = recipes[i];
                string cropName = GetCropDisplayName(r.cropId);
                int mins = Mathf.Max(1, Mathf.RoundToInt(r.durationSeconds / 60f));
                sb.Append("<b>").Append(cropName).Append("</b>");
                sb.Append(en ? "  (" : "  (");
                sb.Append(r.requiredCropKg);
                sb.Append(en ? " kg)  ➜  " : " kg)  ➜  ");
                sb.Append(r.LocalizedName);
                sb.Append(en ? "  •  " : "  •  ");
                sb.Append(r.outputPackCount);
                sb.Append(en ? " packs  •  ~" : " paket  •  ~");
                sb.Append(mins);
                sb.Append(en ? " min  •  $" : " dk  •  $");
                sb.Append(r.unitSalePrice);
                sb.Append(en ? "/unit\n" : "/adet\n");
            }

            sb.Append(en
                ? "\nIf the pallet does not have enough of that exact crop, the Start button stays locked."
                : "\nPaletinde o mahsul yeterli değilse Üret butonu kilitli kalır.");
            return sb.ToString();
        }

        private static string GetFinanceBody()
        {
            return LocalizationManager.L(
                "GuideB_Finance",
                "HUD’daki nakit anlık bakiyendir. Tablet ➔ <b>Finans</b> gelir, gider ve yatırımı gösterir.\n\n" +
                "• Satışlar kasadan nakit ekler.\n" +
                "• Tohum, mobilya, toptan sipariş ve maaş nakit düşer.\n" +
                "• Maaş <b>günde bir kez</b> ödenir.\n" +
                "• Hisse senedi alıp satabilirsin; risklidir.\n\n" +
                "Zaman oyun içinde akar. Gece 24:00'te defter açılır; yalnızca Ertesi güne atla 06:00'ya götürür.\n\n" +
                "Kayıt menüden veya otomatik kayıtla tutulur. Yükleyince kamyonlar, stok ve eğitim adımı korunur.",
                "HUD cash is your live balance. Tablet ➔ <b>Finance</b> shows income, costs, and investments.\n\n" +
                "• Sales add cash at checkout.\n" +
                "• Seeds, furniture, wholesale, and wages spend cash.\n" +
                "• Wages are paid <b>once per day</b>.\n" +
                "• You can trade stocks; they are risky.\n\n" +
                "Time flows in-game. At 24:00 the ledger opens; only Skip to next day jumps to 06:00.\n\n" +
                "Saves (manual or auto) keep trucks, stock, and tutorial step."
            );
        }

        private static string GetExpansionBody()
        {
            return LocalizationManager.L(
                "GuideB_Expansion",
                "Para biriktikçe dükkanı, depoyu ve atölyeyi büyütürsün. Seviye atlayınca:\n\n" +
                "• Dükkan derinliği artar (daha fazla reyon alanı).\n" +
                "• Depo ve personel odası genişler.\n" +
                "• Atölyeye ek pencereler ve üretim alanı gelir.\n" +
                "• Yeni mobilya / dekor kilitleri açılır (dondurucu, kasap, elektronik, lüks dekor).\n\n" +
                "Büyütmeden önce mevcut alanı doldur: boş dükkan masrafı karşılamaz.\n\n" +
                "Hedef zinciri: stoklu raflar ➔ istikrarlı kasa ➔ atölye gurmesi ➔ genişleme. Soldaki konular bu zincirin her halkasını anlatır.",
                "As you earn, you expand the store, warehouse, and workshop. A level-up typically:\n\n" +
                "• Deepens the store (more aisle space).\n" +
                "• Grows storage and the staff room.\n" +
                "• Adds workshop windows and production room.\n" +
                "• Unlocks later furniture / decor (freezer, butcher, electronics, luxury pieces).\n\n" +
                "Fill the space you have first — an empty larger shop costs more than it earns.\n\n" +
                "A solid loop: stocked shelves ➔ steady checkout ➔ workshop gourmet ➔ expand. The topics on the left cover each link."
            );
        }

        private static string GetCropDisplayName(string cropId)
        {
            GardenSeedDef seed = GardenSeedDatabase.GetSeedById(cropId);
            if (seed == null) return cropId;
            return seed.LocalizedName
                .Replace(" Tohumu", "")
                .Replace(" Seeds", "")
                .Replace(" Seed", "");
        }

        private static void CreateLabel(Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, FontStyle style, Color color, TextAnchor align)
        {
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Text txt = obj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
        }

        private static GameObject CreateColorButton(Transform parent, Vector2 pos, Vector2 size, Color color, string label, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            GameObject obj = new GameObject("Btn");
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image img = obj.AddComponent<Image>();
            img.sprite = UIStyleUtility.CreateRoundedPillSprite(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), Mathf.RoundToInt(Mathf.Min(size.x, size.y) * 0.5f), color);
            img.raycastTarget = true;
            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(obj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            if (label == "✖" || label == "✕" || label == "❌")
            {
                UIStyleUtility.BindCloseMark(txt);
            }
            return obj;
        }
    }
}
