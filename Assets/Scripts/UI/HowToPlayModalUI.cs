using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Nasıl Oynanır Rehber Arayüzü (How To Play Modal UI).
    /// Türkçe ve İngilizce çift dilli desteklenir.
    /// </summary>
    public class HowToPlayModalUI : MonoBehaviour
    {
        public static HowToPlayModalUI Instance { get; private set; }

        private GameObject canvasObj;

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
            if (canvasObj != null && canvasObj.activeSelf)
            {
                BuildUI();
            }
        }

        public void ShowModal()
        {
            BuildUI();
        }

        public void HideModal()
        {
            if (canvasObj != null) Destroy(canvasObj);
        }

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);

            canvasObj = new GameObject("HowToPlay_Modal_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Arka Plan
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.88f);
            bdImg.raycastTarget = true;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Panel (800x600)
            GameObject panelObj = new GameObject("HowToPlay_Panel");
            panelObj.transform.SetParent(backdrop.transform, false);

            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(820f, 620f);

            Image pBg = panelObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 620, 18, 3, new Color(0.95f, 0.65f, 0.15f), new Color(0.10f, 0.14f, 0.18f, 0.98f));

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 260f);
            tRect.sizeDelta = new Vector2(600f, 50f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = LocalizationManager.L("Guide_Title", "❓ NASIL OYNANIR? — OYUN REHBERİ", "❓ HOW TO PLAY? — GAME GUIDE");
            tText.fontSize = 24;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.95f, 0.65f, 0.15f);

            // Kapat Butonu (X)
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(panelObj.transform, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchoredPosition = new Vector2(375f, 260f);
            clRect.sizeDelta = new Vector2(46f, 46f);

            Image clBg = closeObj.AddComponent<Image>();
            clBg.sprite = UIStyleUtility.CreateRoundedPillSprite(46, 46, 23, new Color(0.92f, 0.18f, 0.20f, 1f));
            clBg.raycastTarget = true;

            Button clBtn = closeObj.AddComponent<Button>();
            clBtn.targetGraphic = clBg;
            clBtn.onClick.AddListener(HideModal);

            GameObject clTxtObj = new GameObject("X");
            clTxtObj.transform.SetParent(closeObj.transform, false);
            RectTransform cltRect = clTxtObj.AddComponent<RectTransform>();
            cltRect.anchorMin = Vector2.zero;
            cltRect.anchorMax = Vector2.one;

            Text clTxt = clTxtObj.AddComponent<Text>();
            clTxt.font = font;
            clTxt.text = "✖";
            clTxt.fontSize = 26;
            clTxt.fontStyle = FontStyle.Bold;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;
            clTxt.raycastTarget = false;

            Outline clOutline = clTxtObj.AddComponent<Outline>();
            clOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            clOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Rehber Metni
            GameObject guideObj = new GameObject("GuideText");
            guideObj.transform.SetParent(panelObj.transform, false);
            RectTransform gRect = guideObj.AddComponent<RectTransform>();
            gRect.anchoredPosition = new Vector2(0f, -20f);
            gRect.sizeDelta = new Vector2(740f, 480f);

            Text gTxt = guideObj.AddComponent<Text>();
            gTxt.font = font;
            gTxt.text = LocalizationManager.L(
                "Guide_Content",
                "🌱 <b>1. TARLA EKİMİ VE HASAT:</b> Bahçedeki tarlalara tıklayarak tohum ekin. Mahsuller olgunlaştığında tırpan ile biçin ve Ahır stoğunuza aktarın.\n\n" +
                "🚛 <b>2. MARKETE SEVKİYAT VE HIZLI SATIŞ:</b> Ahıra tıklayarak mahsullerinizi %40 kâr marjıyla Yeşil Kamyon üzerinden dükkanınıza sevk edin veya %20 kâr ile anında nakde çevirin.\n\n" +
                "📦 <b>3. REYON STOKLAMA:</b> Depo rafındaki kolileri mağaza içi raflara dizerek müşterilerinize taze ürünler sunun.\n\n" +
                "💳 <b>4. KASA VE MÜŞTERİ HİZMETLERİ:</b> Kasaya dizilen müşterilerin ödemelerini alın, paranızı katlayın ve mağazanızı büyütün.\n\n" +
                "👥 <b>5. PERSONEL İSTİHDAMI:</b> Reyoncu, Kasiyer, Temizlikçi ve Çiftçi personeller işe alarak işletmenizi otomatize edin!",
                "🌱 <b>1. FARMING & HARVEST:</b> Click garden plots to plant seeds. Once crops mature, harvest them and transfer to Barn storage.\n\n" +
                "🚛 <b>2. STORE SHIPPING & INSTANT SALE:</b> Ship crops to your store via Green Truck for +40% profit margin, or sell instantly for +20% cash.\n\n" +
                "📦 <b>3. SHELF RESTOCKING:</b> Place boxes onto store shelves to provide fresh goods to your customers.\n\n" +
                "💳 <b>4. CHECKOUT & CUSTOMER SERVICE:</b> Process customer payments at the cash register, multiply earnings, and expand your market.\n\n" +
                "👥 <b>5. STAFF MANAGEMENT:</b> Hire Restockers, Cashiers, Cleaners, and Farmers to automate your business!"
            );
            gTxt.fontSize = 17;
            gTxt.alignment = TextAnchor.MiddleLeft;
            gTxt.color = Color.white;

            closeObj.transform.SetAsLastSibling();
        }
    }
}
