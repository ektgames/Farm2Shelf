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
                LocalizationManager.L("GuideLib_Title", "📚 REHBER KÜTÜPHANESİ", "📚 GUIDE LIBRARY"),
                26, FontStyle.Bold, new Color(0.95f, 0.65f, 0.15f), TextAnchor.MiddleCenter);

            CreateLabel(panelObj.transform, new Vector2(0f, 280f), new Vector2(980f, 28f),
                LocalizationManager.L("GuideLib_Subtitle", "Merak ettiğin konuya bas, adım adım öğren.", "Tap a topic to learn it step by step."),
                15, FontStyle.Normal, new Color(0.78f, 0.82f, 0.88f), TextAnchor.MiddleCenter);

            GameObject closeObj = CreateColorButton(panelObj.transform, new Vector2(548f, 318f), new Vector2(46f, 46f),
                new Color(0.92f, 0.18f, 0.20f, 1f), "✖", 24, HideModal);
            closeObj.transform.SetAsLastSibling();

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
                case GuideTopic.Overview: return LocalizationManager.L("GuideT_Overview", "🏠 Başlangıç Özeti", "🏠 Getting Started");
                case GuideTopic.Controls: return LocalizationManager.L("GuideT_Controls", "🎮 Kontroller", "🎮 Controls");
                case GuideTopic.Tablet: return LocalizationManager.L("GuideT_Tablet", "📱 EKT Tablet", "📱 EKT Tablet");
                case GuideTopic.Farm: return LocalizationManager.L("GuideT_Farm", "🌱 Çiftlik ve Hasat", "🌱 Farm & Harvest");
                case GuideTopic.Barn: return LocalizationManager.L("GuideT_Barn", "🏚️ Ahır ve Sevkiyat", "🏚️ Barn & Shipping");
                case GuideTopic.Store: return LocalizationManager.L("GuideT_Store", "🏪 Dükkan ve Reyonlar", "🏪 Store & Shelves");
                case GuideTopic.Wholesale: return LocalizationManager.L("GuideT_Wholesale", "🚛 Toptancı Siparişi", "🚛 Wholesale Orders");
                case GuideTopic.Checkout: return LocalizationManager.L("GuideT_Checkout", "💳 Kasa ve Müşteriler", "💳 Checkout & Customers");
                case GuideTopic.Staff: return LocalizationManager.L("GuideT_Staff", "👥 Personel", "👥 Staff");
                case GuideTopic.Furniture: return LocalizationManager.L("GuideT_Furniture", "🪑 Mobilya ve Dekor", "🪑 Furniture & Decor");
                case GuideTopic.Workshop: return LocalizationManager.L("GuideT_Workshop", "🏭 Atölye Sistemi", "🏭 Workshop System");
                case GuideTopic.JamMaker: return LocalizationManager.L("GuideT_Jam", "🍓 Reçel Kazanı", "🍓 Jam Boiler");
                case GuideTopic.JuicePress: return LocalizationManager.L("GuideT_Juice", "🧃 Sıkma Presi", "🧃 Juice Press");
                case GuideTopic.Cannery: return LocalizationManager.L("GuideT_Cannery", "🥫 Konserve Ünitesi", "🥫 Cannery");
                case GuideTopic.Dehydrator: return LocalizationManager.L("GuideT_Dry", "🍿 Kurutma Fırını", "🍿 Dehydrator");
                case GuideTopic.OilPress: return LocalizationManager.L("GuideT_Oil", "🫒 Yağ Presi", "🫒 Oil Press");
                case GuideTopic.SaladStation: return LocalizationManager.L("GuideT_Salad", "🥗 Salata İstasyonu", "🥗 Salad Station");
                case GuideTopic.Finance: return LocalizationManager.L("GuideT_Finance", "💰 Para ve Zaman", "💰 Money & Time");
                case GuideTopic.Expansion: return LocalizationManager.L("GuideT_Expand", "📈 Büyüme ve Seviyeler", "📈 Growth & Upgrades");
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
                "<b>Farm2Shelf</b> bir çiftlik-market simülasyonu. Mahsul ekersin, hasat edersin, dükkana sevk eder veya atölyede gurme ürüne çevirirsin. Müşteriler raftan alır, kasada öder.\n\n" +
                "<b>İlk gün kısa yol:</b>\n" +
                "1. Kamerayı dene, haritayı tanı.\n" +
                "2. Sağ alttaki <b>EKT Tablet</b> ile personel işe al ve vardiya ver.\n" +
                "3. Alışverişten reyon, kasa, depo rafı ve tohum al.\n" +
                "4. Teslimat paletinden mobilyaları kur.\n" +
                "5. Toptan sipariş ver veya tarlaya ek.\n" +
                "6. Raflara ürün ata, dükkanı <b>AÇIK</b> yap.\n\n" +
                "Soldaki butonlardan merak ettiğin konuyu aç. Atölye sayfalarında her makinenin <b>hangi mahsulü kullandığı</b> yazılıdır.",
                "<b>Farm2Shelf</b> is a farm-to-market sim. Grow crops, harvest them, ship to the store, or refine them in the workshop. Customers pick from shelves and pay at checkout.\n\n" +
                "<b>First-day shortcut:</b>\n" +
                "1. Learn the camera and explore the map.\n" +
                "2. Open the <b>EKT Tablet</b> (bottom right), hire staff, set shifts.\n" +
                "3. Buy shelves, a register, storage racks, and seeds.\n" +
                "4. Unpack furniture from the delivery pallet.\n" +
                "5. Place a wholesale order or plant fields.\n" +
                "6. Assign products to shelves, then set the store to <b>OPEN</b>.\n\n" +
                "Use the buttons on the left. Workshop pages list <b>exactly which crops</b> each machine uses."
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
                "Sağ alttaki <b>📱 EKT TABLET</b> işletmenin beynidir. Beş uygulama vardır:\n\n" +
                "🛒 <b>Mağaza Yönetimi</b> — Dükkan personeli işe al, vardiya ver, erken çağır, kadroyu gör.\n" +
                "🌾 <b>Çiftlik</b> — Çiftçi işe al, tarla ve tohum işlerini yönet.\n" +
                "🛍️ <b>Alışveriş (TrendyShop)</b> — Tohum, mobilya, dekor ve toptan ürün satın al. Toplu sipariş butonu buradadır.\n" +
                "💳 <b>Finans</b> — Gelir-gider, hisse senedi ve nakit akışını izle.\n" +
                "𝕏 <b>Sosyal Medya</b> — Müşteri yorumları ve mağaza itibarı.\n\n" +
                "Yeni oyunda eğitim bu tableti adım adım açtırır. Tableti istediğin zaman kapatıp dünyaya dönebilirsin.",
                "The <b>📱 EKT TABLET</b> at the bottom right is your operations hub. It has five apps:\n\n" +
                "🛒 <b>Store Management</b> — Hire store staff, set shifts, call someone in early, review the roster.\n" +
                "🌾 <b>Farm</b> — Hire farmers and manage field work.\n" +
                "🛍️ <b>Shopping (TrendyShop)</b> — Buy seeds, furniture, decor, and wholesale goods. Bulk order lives here.\n" +
                "💳 <b>Finance</b> — Track income, expenses, stocks, and cash flow.\n" +
                "𝕏 <b>Social</b> — Customer posts and store reputation.\n\n" +
                "The tutorial walks these apps on a new game. You can close the tablet at any time and return to the world."
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
                "Gün bitince rapor gelir: ciro, gider, kâr. Maaşlar günde bir kez ödenir.",
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
                sb.Append(machine.iconEmoji).Append(" <b>").Append(machine.LocalizedName).Append("</b>\n");
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
                sb.Append(machine.iconEmoji).Append(" <b>").Append(machine.LocalizedName).Append("</b>\n");
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
                sb.Append(r.iconEmoji).Append(' ');
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
                "Zaman oyun içinde akar. Dükkanı gece kapatmak personel ve müşteri döngüsünü durdurur. Gün sonunda rapor kaydetmeden önce bak.\n\n" +
                "Kayıt menüden veya otomatik kayıtla tutulur. Yükleyince kamyonlar, stok ve eğitim adımı korunur.",
                "HUD cash is your live balance. Tablet ➔ <b>Finance</b> shows income, costs, and investments.\n\n" +
                "• Sales add cash at checkout.\n" +
                "• Seeds, furniture, wholesale, and wages spend cash.\n" +
                "• Wages are paid <b>once per day</b>.\n" +
                "• You can trade stocks; they are risky.\n\n" +
                "Time flows in-game. Closing the store at night pauses the customer loop. Read the end-of-day report before you save.\n\n" +
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
            return obj;
        }
    }
}
