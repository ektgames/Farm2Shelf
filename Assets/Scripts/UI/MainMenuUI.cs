using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Oyun açıldığında karşılayan yüksek kaliteli Low-Poly stilinde Ana Menü Arayüzü.
    /// Türkçe ve İngilizce dil desteği entegreli, canlı dil değiştirme uyumlu.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        private GameObject canvasObj;

        private readonly string[] randomTurkishPlayerNames = new string[]
        {
            "Ahmet Yılmaz", "Elif Demir", "Burak Kaya", "Zeynep Şahin",
            "Mehmet Öztürk", "Ayşe Çelik", "Caner Yıldız", "Gamze Arslan",
            "Serkan Kılıç", "Merve Aydın", "Emre Özkan", "Selin Koç",
            "Deniz Yalçın", "Cem Torun", "Ebru Polat", "Oğuz Güneş",
            "Büşra Kaan", "Kaan Şen", "Volkan Sever", "Hande Çakır"
        };

        private readonly string[] randomEnglishPlayerNames = new string[]
        {
            "Alex Morgan", "Emily Smith", "David Miller", "Sarah Johnson",
            "Michael Brown", "Jessica Davis", "James Wilson", "Chloe Taylor",
            "Oliver Evans", "Sophia Thomas", "Daniel Anderson", "Hannah White"
        };

        private readonly string[] randomRoyaltyFreeCompanyNames = new string[]
        {
            "Yeşil Vadi Market", "Doğal Hasat A.Ş.", "Bereket Tarım", "Anadolu Çiftliği",
            "Taze Hasat Market", "Başak Gıda", "Toprak & Çiftlik", "Güneş Süpermarket",
            "Taze Raf Market", "Organik Pazar", "Köyden Markete", "Vadi Gıda Market",
            "Hasat Zamanı A.Ş.", "Pazar Yeri Market", "Yeşil Bahçe Gıda", "Lezzet Rafı"
        };

        private readonly string[] randomEnglishCompanyNames = new string[]
        {
            "Green Valley Co.", "Fresh Harvest Co.", "Golden Acres Farm",
            "Organic Meadow Co.", "Sunny Field Market", "Pure Nature Foods",
            "Farm2Door Grocery", "Earth Harvest Co.", "Fresh Shelf Market"
        };

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
            if (canvasObj != null)
            {
                BuildUI();
            }
        }

        private void Start()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
                LocalizationManager.Instance.OnLanguageChanged += HandleLanguageChanged;
            }

            // Eğer EKT Reklam İntrosu henüz bitmediyse, introyu bekle. İntro tamamlandığında ShowMenu() otomatik çağrılacaktır.
            if (EKTReklamIntroManager.Instance != null && !EKTReklamIntroManager.HasIntroFinished)
            {
                Time.timeScale = 0f;
                return;
            }

            ShowMenu();
        }

        public void ShowMenu()
        {
            Time.timeScale = 0f; // Ana menüdeyken oyun zamanını duraklat
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.SetHUDVisible(false); // HUD'ı gizle
            }
            BuildUI();
        }

        public void HideMenu()
        {
            Time.timeScale = 1.0f; // Oyun zamanını başlat
            if (canvasObj != null)
            {
                canvasObj.SetActive(false);
            }
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.SetHUDVisible(true); // HUD'ı göster
            }
        }

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);

            canvasObj = new GameObject("Farm2Shelf_MainMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Seçilen 16:9 Sinematik Arka Plan Görseli (Güneşli Sabah İzometrik Çiftlik Kasabası)
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            Texture2D bgTex = Resources.Load<Texture2D>("UI/Backgrounds/MainMenu_Background");
            if (bgTex != null)
            {
                Sprite bgSprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
                bdImg.sprite = bgSprite;
                bdImg.color = Color.white;
            }
            else
            {
                bdImg.color = new Color(0.06f, 0.09f, 0.14f, 0.94f);
            }
            bdImg.raycastTarget = true;

            // Sol Taraf Okunabilirlik Gölge Katmanı (Left Side Vignetting Overlay)
            GameObject leftDimObj = new GameObject("LeftDimOverlay");
            leftDimObj.transform.SetParent(backdrop.transform, false);
            RectTransform ldRect = leftDimObj.AddComponent<RectTransform>();
            ldRect.anchorMin = Vector2.zero;
            ldRect.anchorMax = new Vector2(0.48f, 1f);
            ldRect.sizeDelta = Vector2.zero;

            Image ldImg = leftDimObj.AddComponent<Image>();
            ldImg.sprite = UIStyleUtility.CreateRoundedPillSprite(600, 1080, 0, new Color(0.05f, 0.08f, 0.12f, 0.40f));
            ldImg.raycastTarget = false;

            Font font = GetSafeFont(20);

            // Sol Taraf Ana Menü Kartı Paneli (Low-Poly Glass Container)
            GameObject menuPanel = new GameObject("MainMenu_Panel");
            menuPanel.transform.SetParent(backdrop.transform, false);

            RectTransform pRect = menuPanel.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0f, 0.5f);
            pRect.anchorMax = new Vector2(0f, 0.5f);
            pRect.pivot = new Vector2(0f, 0.5f);
            pRect.anchoredPosition = new Vector2(120f, 0f);
            pRect.sizeDelta = new Vector2(500f, 800f);

            Image pBg = menuPanel.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(500, 800, 24, 3, new Color(0.25f, 0.80f, 0.45f), new Color(0.10f, 0.14f, 0.20f, 0.96f));

            // OYUN LOGO BAŞLIĞI: "FARM2SHELF"
            GameObject logoObj = new GameObject("GameLogoText");
            logoObj.transform.SetParent(menuPanel.transform, false);
            RectTransform lRect = logoObj.AddComponent<RectTransform>();
            lRect.anchoredPosition = new Vector2(0f, 310f);
            lRect.sizeDelta = new Vector2(440f, 90f);

            Text logoTxt = logoObj.AddComponent<Text>();
            logoTxt.font = font;
            logoTxt.text = "FARM<color=#00E676>2</color>SHELF";
            logoTxt.fontSize = 44;
            logoTxt.fontStyle = FontStyle.Bold;
            logoTxt.alignment = TextAnchor.MiddleCenter;
            logoTxt.color = Color.white;

            // Alt Başlık Slogan
            GameObject subObj = new GameObject("SubTitleText");
            subObj.transform.SetParent(menuPanel.transform, false);
            RectTransform sRect = subObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 255f);
            sRect.sizeDelta = new Vector2(440f, 36f);

            Text subTxt = subObj.AddComponent<Text>();
            subTxt.font = font;
            subTxt.text = LocalizationManager.L("MainMenu_Slogan", "Çiftlikten Markete Simülasyonu 🌾🛒", "Farm to Market Simulation 🌾🛒");
            subTxt.fontSize = 18;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.35f, 0.85f, 0.95f);

            // Seperatör Çizgisi
            GameObject sepObj = new GameObject("SeparatorLine");
            sepObj.transform.SetParent(menuPanel.transform, false);
            RectTransform sepRect = sepObj.AddComponent<RectTransform>();
            sepRect.anchoredPosition = new Vector2(0f, 225f);
            sepRect.sizeDelta = new Vector2(400f, 3f);
            Image sepImg = sepObj.AddComponent<Image>();
            sepImg.color = new Color(0.25f, 0.80f, 0.45f, 0.60f);

            // ==================== ANA MENÜ BUTONLARI (5 ADET - SIRASIYLA) ====================
            string[] buttonTitles = new string[]
            {
                LocalizationManager.L("Menu_NewGame", "▶ YENİ OYUN", "▶ NEW GAME"),
                LocalizationManager.L("Menu_LoadGame", "📂 KAYITLI OYUN YÜKLE", "📂 LOAD GAME"),
                LocalizationManager.L("Menu_Settings", "⚙️ AYARLAR", "⚙️ SETTINGS"),
                LocalizationManager.L("Menu_HowToPlay", "❓ NASIL OYNANIR", "❓ HOW TO PLAY"),
                LocalizationManager.L("Menu_Exit", "🚪 ÇIKIŞ", "🚪 EXIT")
            };

            Color[] buttonColors = new Color[]
            {
                new Color(0.20f, 0.75f, 0.35f), // Yeşil
                new Color(0.18f, 0.60f, 0.85f), // Mavi
                new Color(0.55f, 0.35f, 0.75f), // Mor
                new Color(0.95f, 0.65f, 0.15f), // Turuncu
                new Color(0.85f, 0.20f, 0.25f)  // Kırmızı
            };

            float startY = 140f;
            float btnSpacing = 82f;

            for (int i = 0; i < buttonTitles.Length; i++)
            {
                int btnIndex = i;
                GameObject btnObj = new GameObject("MenuBtn_" + i);
                btnObj.transform.SetParent(menuPanel.transform, false);

                RectTransform bRect = btnObj.AddComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(0f, startY - i * btnSpacing);
                bRect.sizeDelta = new Vector2(400f, 62f);

                Image bBg = btnObj.AddComponent<Image>();
                bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(400, 62, 14, buttonColors[i]);

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = bBg;
                btn.onClick.AddListener(() => OnMenuButtonClicked(btnIndex));

                GameObject txtObj = new GameObject("Label");
                txtObj.transform.SetParent(btnObj.transform, false);
                RectTransform tRect2 = txtObj.AddComponent<RectTransform>();
                tRect2.anchorMin = Vector2.zero;
                tRect2.anchorMax = Vector2.one;

                Text txt = txtObj.AddComponent<Text>();
                txt.font = font;
                txt.text = buttonTitles[i];
                txt.fontSize = 20;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.raycastTarget = false;
            }
        }

        private void OnMenuButtonClicked(int index)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            switch (index)
            {
                case 0: // 1. YENİ OYUN
                    ShowNewGameProfileSetupModal();
                    break;
                case 1: // 2. KAYITLI OYUN YÜKLE
                    if (SaveLoadSlotModalUI.Instance != null)
                    {
                        SaveLoadSlotModalUI.Instance.ShowLoadModal();
                    }
                    break;
                case 2: // 3. AYARLAR
                    if (SettingsModalUI.Instance != null)
                    {
                        SettingsModalUI.Instance.ShowModal();
                    }
                    break;
                case 3: // 4. NASIL OYNANIR
                    if (HowToPlayModalUI.Instance != null)
                    {
                        HowToPlayModalUI.Instance.ShowModal();
                    }
                    break;
                case 4: // 5. ÇIKIŞ
                    QuitGame();
                    break;
            }
        }

        private void ShowNewGameProfileSetupModal()
        {
            if (canvasObj == null) return;

            // Varsa eski modalı temizle
            Transform oldModal = canvasObj.transform.Find("NewGame_Profile_Setup_Modal");
            if (oldModal != null) Destroy(oldModal.gameObject);

            // Tam Ekran Arka Plan Karartma (Dim Backdrop)
            GameObject backdrop = new GameObject("NewGame_Profile_Setup_Modal");
            backdrop.transform.SetParent(canvasObj.transform, false);

            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.07f, 0.12f, 0.88f);
            bdImg.raycastTarget = true;

            // Low-Poly Tatlı Modal Kart Paneli (680x540)
            GameObject dialogPanel = new GameObject("DialogPanel");
            dialogPanel.transform.SetParent(backdrop.transform, false);

            RectTransform dpRect = dialogPanel.AddComponent<RectTransform>();
            dpRect.anchorMin = new Vector2(0.5f, 0.5f);
            dpRect.anchorMax = new Vector2(0.5f, 0.5f);
            dpRect.pivot = new Vector2(0.5f, 0.5f);
            dpRect.sizeDelta = new Vector2(680f, 540f);

            Image dpImg = dialogPanel.AddComponent<Image>();
            dpImg.sprite = UIStyleUtility.CreateOutlinePillSprite(680, 540, 24, 3, new Color(0.25f, 0.80f, 0.45f), new Color(0.10f, 0.14f, 0.20f, 0.98f));
            dpImg.raycastTarget = true;

            // Giriş Yaylı Animasyonu
            StartCoroutine(AnimateModalEntrance(dialogPanel.transform));

            // 1. Üst Başlık Şeridi
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(dialogPanel.transform, false);
            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = new Vector2(0f, -12f);
            hRect.sizeDelta = new Vector2(-40f, 45f);

            CreateTextChild(headerObj, LocalizationManager.L("Modal_NewGameTitle", "🌾 YENİ OYUN: KURULUM ✨", "🌾 NEW GAME: SETUP ✨"), 26, FontStyle.Bold, new Color(0.25f, 0.85f, 0.45f));

            // Alt Başlık Açıklama & Canlı Uyarı Metni
            GameObject subHeaderObj = new GameObject("SubHeader");
            subHeaderObj.transform.SetParent(dialogPanel.transform, false);
            RectTransform shRect = subHeaderObj.AddComponent<RectTransform>();
            shRect.anchorMin = new Vector2(0f, 1f);
            shRect.anchorMax = new Vector2(1f, 1f);
            shRect.pivot = new Vector2(0.5f, 1f);
            shRect.anchoredPosition = new Vector2(0f, -54f);
            shRect.sizeDelta = new Vector2(-40f, 32f);

            Text warningText = CreateTextChild(subHeaderObj, LocalizationManager.L("Modal_SubHeader", "Hayalinizdeki çiftlik ve süpermarketi kurmak için bilgilerinizi yazın!", "Enter details to create your dream farm & supermarket!"), 15, FontStyle.Normal, new Color(0.70f, 0.80f, 0.90f));

            // Seperatör Çizgisi
            GameObject sep = new GameObject("SepLine");
            sep.transform.SetParent(dialogPanel.transform, false);
            RectTransform sepRect = sep.AddComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0.06f, 1f);
            sepRect.anchorMax = new Vector2(0.94f, 1f);
            sepRect.anchoredPosition = new Vector2(0f, -88f);
            sepRect.sizeDelta = new Vector2(0f, 2f);
            Image sepImg = sep.AddComponent<Image>();
            sepImg.color = new Color(0.25f, 0.80f, 0.45f, 0.40f);

            bool isEN = LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish;
            string[] nameOptions = isEN ? randomEnglishPlayerNames : randomTurkishPlayerNames;
            string[] compOptions = isEN ? randomEnglishCompanyNames : randomRoyaltyFreeCompanyNames;

            // ================= 1. GİRDİ ALANI: İSİM SOYİSİM =================
            GameObject nameLabelObj = new GameObject("NameLabel");
            nameLabelObj.transform.SetParent(dialogPanel.transform, false);
            RectTransform nlRect = nameLabelObj.AddComponent<RectTransform>();
            nlRect.anchorMin = new Vector2(0.08f, 1f);
            nlRect.anchorMax = new Vector2(0.92f, 1f);
            nlRect.pivot = new Vector2(0f, 1f);
            nlRect.anchoredPosition = new Vector2(0f, -100f);
            nlRect.sizeDelta = new Vector2(0f, 26f);

            Text nlText = CreateTextChild(nameLabelObj, LocalizationManager.L("Modal_NameLabel", "👨‍🌾 Oyuncu İsim & Soyisim:", "👨‍🌾 Player Full Name:"), 17, FontStyle.Bold, new Color(0.95f, 0.85f, 0.35f));
            if (nlText != null) nlText.alignment = TextAnchor.MiddleLeft;

            GameObject nameInputBox = new GameObject("NameInputBox");
            nameInputBox.transform.SetParent(dialogPanel.transform, false);
            RectTransform nibRect = nameInputBox.AddComponent<RectTransform>();
            nibRect.anchorMin = new Vector2(0.08f, 1f);
            nibRect.anchorMax = new Vector2(0.92f, 1f);
            nibRect.pivot = new Vector2(0.5f, 1f);
            nibRect.anchoredPosition = new Vector2(0f, -128f);
            nibRect.sizeDelta = new Vector2(0f, 52f);

            Text nameCounterText = null;
            string namePlaceholder = LocalizationManager.L("Modal_NamePlaceholder", "Örn: Ali Yılmaz...", "Ex: Alex Morgan...");
            InputField nameInputField = CreateInputFieldWithDice(nameInputBox, namePlaceholder, 25, nameOptions, (val, textLen) => {
                if (nameCounterText != null)
                {
                    nameCounterText.text = $"({textLen}/18)";
                    nameCounterText.color = (textLen > 18) ? new Color(0.95f, 0.40f, 0.30f) : new Color(0.60f, 0.70f, 0.80f);
                }
            });

            GameObject nameCounterObj = new GameObject("Counter");
            nameCounterObj.transform.SetParent(nameInputBox.transform, false);
            RectTransform ncRect = nameCounterObj.AddComponent<RectTransform>();
            ncRect.anchorMin = new Vector2(0.83f, 0f);
            ncRect.anchorMax = new Vector2(0.83f, 1f);
            ncRect.pivot = new Vector2(1f, 0.5f);
            ncRect.anchoredPosition = new Vector2(-12f, 0f);
            ncRect.sizeDelta = new Vector2(80f, 0f);

            nameCounterText = CreateTextChild(nameCounterObj, "(0/18)", 14, FontStyle.Bold, new Color(0.60f, 0.70f, 0.80f));
            if (nameCounterText != null) nameCounterText.alignment = TextAnchor.MiddleRight;

            // ================= 2. GİRDİ ALANI: ŞİRKET İSMİ =================
            GameObject companyLabelObj = new GameObject("CompanyLabel");
            companyLabelObj.transform.SetParent(dialogPanel.transform, false);
            RectTransform clRect = companyLabelObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0.08f, 1f);
            clRect.anchorMax = new Vector2(0.92f, 1f);
            clRect.pivot = new Vector2(0f, 1f);
            clRect.anchoredPosition = new Vector2(0f, -196f);
            clRect.sizeDelta = new Vector2(0f, 26f);

            Text clText = CreateTextChild(companyLabelObj, LocalizationManager.L("Modal_CompanyLabel", "🏢 Şirket / Mağaza İsmi:", "🏢 Company / Store Name:"), 17, FontStyle.Bold, new Color(0.40f, 0.88f, 1.0f));
            if (clText != null) clText.alignment = TextAnchor.MiddleLeft;

            GameObject companyInputBox = new GameObject("CompanyInputBox");
            companyInputBox.transform.SetParent(dialogPanel.transform, false);
            RectTransform cibRect = companyInputBox.AddComponent<RectTransform>();
            cibRect.anchorMin = new Vector2(0.08f, 1f);
            cibRect.anchorMax = new Vector2(0.92f, 1f);
            cibRect.pivot = new Vector2(0.5f, 1f);
            cibRect.anchoredPosition = new Vector2(0f, -224f);
            cibRect.sizeDelta = new Vector2(0f, 52f);

            Text companyCounterText = null;
            string compPlaceholder = LocalizationManager.L("Modal_CompanyPlaceholder", "Örn: Yeşil Vadi Market...", "Ex: Green Valley Market...");
            InputField companyInputField = CreateInputFieldWithDice(companyInputBox, compPlaceholder, 25, compOptions, (val, textLen) => {
                if (companyCounterText != null)
                {
                    companyCounterText.text = $"({textLen}/18)";
                    companyCounterText.color = (textLen > 18) ? new Color(0.95f, 0.40f, 0.30f) : new Color(0.60f, 0.70f, 0.80f);
                }
            });

            GameObject companyCounterObj = new GameObject("Counter");
            companyCounterObj.transform.SetParent(companyInputBox.transform, false);
            RectTransform ccRect = companyCounterObj.AddComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0.83f, 0f);
            ccRect.anchorMax = new Vector2(0.83f, 1f);
            ccRect.pivot = new Vector2(1f, 0.5f);
            ccRect.anchoredPosition = new Vector2(-12f, 0f);
            ccRect.sizeDelta = new Vector2(80f, 0f);

            companyCounterText = CreateTextChild(companyCounterObj, "(0/18)", 14, FontStyle.Bold, new Color(0.60f, 0.70f, 0.80f));
            if (companyCounterText != null) companyCounterText.alignment = TextAnchor.MiddleRight;

            // ================= ALT AKSİYON BUTONLARI =================
            GameObject footerObj = new GameObject("Footer");
            footerObj.transform.SetParent(dialogPanel.transform, false);
            RectTransform fRect = footerObj.AddComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0f, 0f);
            fRect.anchorMax = new Vector2(1f, 0f);
            fRect.pivot = new Vector2(0.5f, 0f);
            fRect.anchoredPosition = new Vector2(0f, 25f);
            fRect.sizeDelta = new Vector2(-80f, 65f);

            // 1. OYUNU BAŞLAT BUTONU
            GameObject startBtnObj = new GameObject("StartGameBtn");
            startBtnObj.transform.SetParent(footerObj.transform, false);
            RectTransform sbRect = startBtnObj.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0f, 0f);
            sbRect.anchorMax = new Vector2(0.62f, 1f);
            sbRect.offsetMin = Vector2.zero;
            sbRect.offsetMax = Vector2.zero;

            Image sbBg = startBtnObj.AddComponent<Image>();
            sbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(360, 65, 14, new Color(0.20f, 0.75f, 0.35f));

            Button sbBtn = startBtnObj.AddComponent<Button>();
            sbBtn.targetGraphic = sbBg;

            CreateTextChild(startBtnObj, LocalizationManager.L("Modal_StartBtn", "🚀 Oyunu Başlat!", "🚀 Start Game!"), 20, FontStyle.Bold, Color.white);

            sbBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

                string pName = nameInputField != null ? nameInputField.text.Trim() : "";
                string cName = companyInputField != null ? companyInputField.text.Trim() : "";

                // 1. BOŞ BIRAKILDIYSA OYUNA GİRMESİN KONTROLÜ
                if (string.IsNullOrWhiteSpace(pName) || string.IsNullOrWhiteSpace(cName))
                {
                    if (warningText != null)
                    {
                        warningText.text = LocalizationManager.L(
                            "Modal_EmptyWarning",
                            "⚠️ Lütfen hem Oyuncu Adını hem de Şirket İsmini giriniz!\n(Yandaki 🎲 butonuna basarak rastgele üretebilirsiniz)",
                            "⚠️ Please enter both Player Name and Company Name!\n(Click the 🎲 dice button to generate automatically)"
                        );
                        warningText.color = new Color(0.95f, 0.35f, 0.30f);
                    }
                    return; // KESİNLİKLE OYUNA GİRME!
                }

                // 2. 18 KARAKTER SINIRI AŞILDISA OYUNA GİRMESİN KONTROLÜ
                if (pName.Length > 18 || cName.Length > 18)
                {
                    if (warningText != null)
                    {
                        warningText.text = LocalizationManager.L(
                            "Modal_CharLimitWarning",
                            "⚠️ İsminiz veya Şirket isminiz 18 karakter sınırını aşıyor!\nLütfen daha kısa bir isim giriniz (Maks 18 Karakter).",
                            "⚠️ Player or Company name exceeds the 18 character limit!\nPlease enter a shorter name (Max 18 Chars)."
                        );
                        warningText.color = new Color(0.95f, 0.35f, 0.30f);
                    }
                    return; // KESİNLİKLE OYUNA GİRME!
                }

                if (StoreStatusManager.Instance != null)
                {
                    StoreStatusManager.Instance.SetPlayerAndCompany(pName, cName);
                }

                Destroy(backdrop);
                StartNewGame();
            });

            // 2. İPTAL BUTONU
            GameObject cancelBtnObj = new GameObject("CancelBtn");
            cancelBtnObj.transform.SetParent(footerObj.transform, false);
            RectTransform cbRect = cancelBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.66f, 0f);
            cbRect.anchorMax = new Vector2(1f, 1f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            Image cbBg = cancelBtnObj.AddComponent<Image>();
            cbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(200, 65, 14, new Color(0.40f, 0.45f, 0.52f));

            Button cbBtn = cancelBtnObj.AddComponent<Button>();
            cbBtn.targetGraphic = cbBg;
            cbBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                Destroy(backdrop);
            });

            CreateTextChild(cancelBtnObj, LocalizationManager.L("Modal_CancelBtn", "❌ İptal", "❌ Cancel"), 18, FontStyle.Bold, Color.white);
        }

        private InputField CreateInputFieldWithDice(GameObject parentBox, string placeholder, int characterLimit, string[] randomList, System.Action<string, int> onValueChanged)
        {
            if (parentBox == null) return null;

            // Sol Taraf (%83): InputField Kapsayıcısı
            GameObject inputObj = new GameObject("InputFieldBox");
            inputObj.transform.SetParent(parentBox.transform, false);

            RectTransform inRect = inputObj.AddComponent<RectTransform>();
            inRect.anchorMin = new Vector2(0f, 0f);
            inRect.anchorMax = new Vector2(0.83f, 1f);
            inRect.offsetMin = Vector2.zero;
            inRect.offsetMax = Vector2.zero;

            Image bgImg = inputObj.AddComponent<Image>();
            bgImg.sprite = UIStyleUtility.CreateOutlinePillSprite(440, 52, 12, 2, new Color(0.25f, 0.35f, 0.48f, 0.80f), new Color(0.12f, 0.16f, 0.24f, 0.95f));
            bgImg.raycastTarget = true;

            InputField inputField = inputObj.AddComponent<InputField>();
            inputField.targetGraphic = bgImg;
            inputField.characterLimit = characterLimit;

            Font safeFont = GetSafeFont(16);

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(16, 0);
            phRect.offsetMax = new Vector2(-75, 0);

            Text phText = placeholderObj.AddComponent<Text>();
            if (safeFont != null) try { phText.font = safeFont; } catch {}
            try { phText.fontSize = 16; } catch {}
            try { phText.fontStyle = FontStyle.Italic; } catch {}
            try { phText.color = new Color(0.55f, 0.65f, 0.75f, 0.80f); } catch {}
            try { phText.alignment = TextAnchor.MiddleLeft; } catch {}
            try { phText.text = placeholder ?? ""; } catch {}

            // Text Component
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(16, 0);
            tRect.offsetMax = new Vector2(-75, 0);

            Text txt = textObj.AddComponent<Text>();
            if (safeFont != null) try { txt.font = safeFont; } catch {}
            try { txt.fontSize = 18; } catch {}
            try { txt.fontStyle = FontStyle.Bold; } catch {}
            try { txt.color = Color.white; } catch {}
            try { txt.alignment = TextAnchor.MiddleLeft; } catch {}
            try { txt.text = ""; } catch {}

            inputField.textComponent = txt;
            inputField.placeholder = phText;

            if (onValueChanged != null)
            {
                inputField.onValueChanged.AddListener((val) => onValueChanged.Invoke(val, val.Length));
            }

            // Sağ Taraf (%15): Zar Butonu 🎲
            GameObject diceBtnObj = new GameObject("DiceBtn");
            diceBtnObj.transform.SetParent(parentBox.transform, false);

            RectTransform diceRect = diceBtnObj.AddComponent<RectTransform>();
            diceRect.anchorMin = new Vector2(0.85f, 0f);
            diceRect.anchorMax = new Vector2(1f, 1f);
            diceRect.offsetMin = Vector2.zero;
            diceRect.offsetMax = Vector2.zero;

            Image diceBg = diceBtnObj.AddComponent<Image>();
            diceBg.sprite = UIStyleUtility.CreateRoundedPillSprite(80, 52, 12, new Color(0.85f, 0.55f, 0.15f));
            diceBg.raycastTarget = true;

            Button diceBtn = diceBtnObj.AddComponent<Button>();
            diceBtn.targetGraphic = diceBg;

            CreateTextChild(diceBtnObj, "🎲", 24, FontStyle.Bold, Color.white);

            diceBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

                if (randomList != null && randomList.Length > 0)
                {
                    string picked = randomList[Random.Range(0, randomList.Length)];
                    inputField.text = picked;
                }
            });

            return inputField;
        }

        private Text CreateTextChild(GameObject parentObj, string content, int fontSize, FontStyle style, Color color)
        {
            if (parentObj == null) return null;

            GameObject txtObj = new GameObject("TextLabel");
            txtObj.transform.SetParent(parentObj.transform, false);

            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            Font safeFont = GetSafeFont(fontSize);
            if (safeFont != null) try { txt.font = safeFont; } catch {}

            try { txt.fontSize = fontSize; } catch {}
            try { txt.fontStyle = style; } catch {}
            try { txt.color = color; } catch {}
            try { txt.alignment = TextAnchor.MiddleCenter; } catch {}
            try { txt.raycastTarget = false; } catch {}
            try { txt.text = content ?? ""; } catch {}

            return txt;
        }

        private System.Collections.IEnumerator AnimateModalEntrance(Transform dialogTransform)
        {
            float elapsed = 0f;
            float duration = 0.22f;
            Vector3 startScale = Vector3.one * 0.72f;
            Vector3 targetScale = Vector3.one;

            dialogTransform.localScale = startScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);
                dialogTransform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                yield return null;
            }

            dialogTransform.localScale = targetScale;
        }

        private Font GetSafeFont(int fontSize = 16)
        {
            // 1. Sahnedeki mevcut tüm Text nesnelerinin geçerli fontunu ara
            try
            {
                Text[] sceneTexts = Object.FindObjectsOfType<Text>(true);
                if (sceneTexts != null && sceneTexts.Length > 0)
                {
                    foreach (var st in sceneTexts)
                    {
                        if (st != null && st.font != null) return st.font;
                    }
                }
            }
            catch {}

            // 2. Projedeki tüm yüklenmiş Font kaynaklarını ara
            try
            {
                Font[] loadedFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (loadedFonts != null && loadedFonts.Length > 0)
                {
                    foreach (var f in loadedFonts)
                    {
                        if (f != null) return f;
                    }
                }
            }
            catch {}

            // 3. Dahili Unity kaynak fontlarını dene
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {}
            if (font != null) return font;

            try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch {}
            if (font != null) return font;

            // 4. İşletim sistemi fontları
            try { font = Font.CreateDynamicFontFromOSFont("Arial", fontSize); } catch {}
            if (font != null) return font;

            try { font = Font.CreateDynamicFontFromOSFont("Segoe UI", fontSize); } catch {}
            if (font != null) return font;

            return font;
        }

        private Text CreateTextInModal(GameObject parent, string content, int fontSize, FontStyle style, Color color)
        {
            return CreateTextChild(parent, content, fontSize, style, color);
        }

        private void StartNewGame()
        {
            HideMenu();
            if (EconomyManager.Instance != null) EconomyManager.Instance.SetCredits(400000);
            if (StoreStatusManager.Instance != null) StoreStatusManager.Instance.CloseStore();

            string defaultPlayer = LocalizationManager.L("Default_Player", "Çiftçi Ali", "Farmer Alex");
            string defaultCompany = LocalizationManager.L("Default_Company", "Farm2Shelf Market", "Farm2Shelf Market");

            string pName = (StoreStatusManager.Instance != null && !string.IsNullOrEmpty(StoreStatusManager.Instance.PlayerName)) ? StoreStatusManager.Instance.PlayerName : defaultPlayer;
            string cName = (StoreStatusManager.Instance != null && !string.IsNullOrEmpty(StoreStatusManager.Instance.CompanyName)) ? StoreStatusManager.Instance.CompanyName : defaultCompany;

            ModalManager.ShowModal(
                LocalizationManager.L("Welcome_Title", $"Hoş Geldin, {pName}! 🚀", $"Welcome, {pName}! 🚀"),
                LocalizationManager.L("Welcome_Body", $"<b>{cName}</b> şirketiniz başarıyla kuruldu!\n\nSıfırdan tarlanızı ekin, ürünlerinizi hasat edin, dükkanınızı büyütün ve büyük bir süpermarket zinciri kurun!", $"Your company <b>{cName}</b> has been successfully established!\n\nPlant your fields, harvest crops, expand your store, and build a massive supermarket chain!"),
                LocalizationManager.L("Welcome_Btn", "Çiftliğe Git!", "Go to Farm!")
            );
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
