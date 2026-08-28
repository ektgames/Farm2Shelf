using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Müşterilerin üzerine tıklandığında açılan
    /// Müşteri Profil Kartı arayüzü.
    /// Dışarıya tıklama (Backdrop), Üst Sağ X butonu ve Alt Kapat butonu ile sorunsuz kapatılabilir.
    /// </summary>
    public class CustomerProfileModalUI : MonoBehaviour
    {
        public static CustomerProfileModalUI Instance { get; private set; }

        private GameObject modalContainer;
        private GameObject cardPanel;
        private Text nameText;
        private Text ageText;
        private Text genderText;
        private Text occupationText;
        private Text avatarEmojiText;
        private Image avatarBgImage;
        private Image avatarPhotoImg;

        private CustomerProfileData currentCustomerProfile;
        private Transform closeButtonTransform;

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
            if (modalContainer != null && modalContainer.activeSelf && currentCustomerProfile != null)
            {
                ShowCustomerProfile(currentCustomerProfile);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            if (modalContainer != null) return;

            // Ana HUD Canvas'ını Ara veya Özel Canvas Oluştur
            Transform targetCanvasTransform = null;
            GameObject existingHUDCanvas = GameObject.Find("Farm2Shelf_HUD_Canvas");
            if (existingHUDCanvas != null)
            {
                targetCanvasTransform = existingHUDCanvas.transform;
            }
            else
            {
                GameObject mainCanvasObj = GameObject.Find("Customer_Profile_Canvas");
                if (mainCanvasObj == null)
                {
                    mainCanvasObj = new GameObject("Customer_Profile_Canvas");
                    Canvas canvas = mainCanvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 106;

                    CanvasScaler scaler = mainCanvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;

                    mainCanvasObj.AddComponent<GraphicRaycaster>();
                }
                targetCanvasTransform = mainCanvasObj.transform;
            }

            // Kök Modal Kapsayıcı (Ekranı Kaplayan Root)
            modalContainer = new GameObject("Customer_Profile_Modal_CanvasRoot");
            modalContainer.transform.SetParent(targetCanvasTransform, false);

            RectTransform rootRect = modalContainer.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Arka Plan Karartma (Overlay Backdrop - Şeffaf ve 3D Tıklamaları Engellemez)
            GameObject backdropObj = new GameObject("Backdrop");
            backdropObj.transform.SetParent(modalContainer.transform, false);
            RectTransform bdRect = backdropObj.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;

            Image bdImg = backdropObj.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.20f);
            bdImg.raycastTarget = false; // 3D dünyadaki müşteri, personel ve nesne tıklamalarını KESİNLİKLE engellemez

            // Müşteri Kartı Paneli (Sol Alt Taraf - Safe Area Desteğiyle)
            cardPanel = new GameObject("Customer_Profile_CardPanel");
            cardPanel.transform.SetParent(modalContainer.transform, false);

            RectTransform mainRect = cardPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0f, 0f);
            mainRect.anchorMax = new Vector2(0f, 0f);
            mainRect.pivot = new Vector2(0f, 0f);
            mainRect.anchoredPosition = new Vector2(115f, 35f); // Mobil kamera çentiğini aşan güvenli sol alt pozisyon
            mainRect.sizeDelta = new Vector2(380f, 490f);

            // Kart Arka Planı
            Image bgImage = cardPanel.AddComponent<Image>();
            bgImage.color = new Color(0.11f, 0.13f, 0.20f, 0.98f);
            bgImage.raycastTarget = true;

            // Üst Çerçeve Çizgisi Süsü (Neon Purple Header Line)
            GameObject topDeco = new GameObject("TopDecoLine");
            topDeco.transform.SetParent(cardPanel.transform, false);
            RectTransform topRect = topDeco.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0f, 6f);
            Image topImg = topDeco.AddComponent<Image>();
            topImg.color = new Color(0.65f, 0.35f, 0.95f);

            // Panel Başlığı (MÜŞTERİ PROFİLİ)
            GameObject titleObj = new GameObject("PanelTitle");
            titleObj.transform.SetParent(cardPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(18f, -16f);
            titleRect.sizeDelta = new Vector2(-75f, 36f);

            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = UIStyleUtility.GetGlobalFont(24);
            titleTxt.text = LocalizationManager.L("Customer_Profile_Title", "MÜŞTERİ PROFİLİ 🛒", "CUSTOMER PROFILE 🛒");
            titleTxt.fontSize = 24;
            titleTxt.resizeTextForBestFit = true;
            titleTxt.resizeTextMinSize = 16;
            titleTxt.resizeTextMaxSize = 25;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = new Color(0.80f, 0.50f, 1.0f);
            titleTxt.raycastTarget = false;

            // Üst Sağ Kapat Butonu (X - Yüksek Kalite ve Dokunmatik Dostu)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(cardPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-10f, -10f);
            closeRect.sizeDelta = new Vector2(46f, 46f);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.sprite = UIStyleUtility.CreateRoundedPillSprite(46, 46, 23, new Color(0.92f, 0.18f, 0.20f, 1f));
            closeImg.raycastTarget = true;

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            ColorBlock cb = closeBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.0f, 0.85f, 0.85f, 1f);
            cb.pressedColor = new Color(0.80f, 0.70f, 0.70f, 1f);
            cb.selectedColor = cb.normalColor;
            closeBtn.colors = cb;
            closeBtn.onClick.AddListener(HideModal);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cTxtRect = closeTxtObj.AddComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            Text cTxt = closeTxtObj.AddComponent<Text>();
            cTxt.font = UIStyleUtility.GetGlobalFont(26);
            cTxt.text = "✖";
            cTxt.fontSize = 26;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.raycastTarget = false;

            Outline cOutline = closeTxtObj.AddComponent<Outline>();
            cOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            cOutline.effectDistance = new Vector2(1.5f, -1.5f);

            closeButtonTransform = closeBtnObj.transform;

            // 1. PROFİL FOTOĞRAFI KUTUSU (Realistic Portrait Avatar Box)
            GameObject avatarBox = new GameObject("AvatarBox");
            avatarBox.transform.SetParent(cardPanel.transform, false);
            RectTransform avRect = avatarBox.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0.5f, 1f);
            avRect.anchorMax = new Vector2(0.5f, 1f);
            avRect.pivot = new Vector2(0.5f, 1f);
            avRect.anchoredPosition = new Vector2(0f, -60f);
            avRect.sizeDelta = new Vector2(106f, 106f);

            avatarBgImage = avatarBox.AddComponent<Image>();
            avatarBgImage.color = new Color(0.20f, 0.25f, 0.35f, 1f); // Şık Çerçeve Kenarlığı

            // İç Fotoğraf Katmanı (Gerçekçi Vesikalık Fotoğraf)
            GameObject photoObj = new GameObject("AvatarPhoto");
            photoObj.transform.SetParent(avatarBox.transform, false);
            RectTransform pRect = photoObj.AddComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.offsetMin = new Vector2(3f, 3f);
            pRect.offsetMax = new Vector2(-3f, -3f);

            avatarPhotoImg = photoObj.AddComponent<Image>();
            avatarPhotoImg.preserveAspect = true;
            avatarPhotoImg.type = Image.Type.Simple;

            // Yedek / Rozet Emoji Metni
            GameObject avTxtObj = new GameObject("AvatarEmoji");
            avTxtObj.transform.SetParent(avatarBox.transform, false);
            RectTransform avtRect = avTxtObj.AddComponent<RectTransform>();
            avtRect.anchorMin = Vector2.zero;
            avtRect.anchorMax = Vector2.one;
            avatarEmojiText = avTxtObj.AddComponent<Text>();
            avatarEmojiText.font = UIStyleUtility.GetGlobalFont(54);
            avatarEmojiText.fontSize = 54;
            avatarEmojiText.fontStyle = FontStyle.Bold;
            avatarEmojiText.alignment = TextAnchor.MiddleCenter;
            avatarEmojiText.gameObject.SetActive(false); // Fotoğraf varsa gizli

            // 2. İSİM SOYİSİM METNİ
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardPanel.transform, false);
            RectTransform nRect = nameObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0f, 1f);
            nRect.anchorMax = new Vector2(1f, 1f);
            nRect.pivot = new Vector2(0.5f, 1f);
            nRect.anchoredPosition = new Vector2(0f, -172f);
            nRect.sizeDelta = new Vector2(-36f, 36f);

            nameText = nameObj.AddComponent<Text>();
            nameText.font = UIStyleUtility.GetGlobalFont(26);
            nameText.fontSize = 26;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 18;
            nameText.resizeTextMaxSize = 28;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;

            // 3. CİNSİYET METNİ
            GameObject genderObj = new GameObject("GenderText");
            genderObj.transform.SetParent(cardPanel.transform, false);
            RectTransform gRect = genderObj.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0f, 1f);
            gRect.anchorMax = new Vector2(1f, 1f);
            gRect.pivot = new Vector2(0.5f, 1f);
            gRect.anchoredPosition = new Vector2(0f, -212f);
            gRect.sizeDelta = new Vector2(-36f, 28f);

            genderText = genderObj.AddComponent<Text>();
            genderText.font = UIStyleUtility.GetGlobalFont(20);
            genderText.fontSize = 20;
            genderText.resizeTextForBestFit = true;
            genderText.resizeTextMinSize = 15;
            genderText.resizeTextMaxSize = 22;
            genderText.fontStyle = FontStyle.Bold;
            genderText.alignment = TextAnchor.MiddleCenter;
            genderText.color = new Color(0.40f, 0.88f, 1.0f);

            // 4. YAŞ METNİ
            GameObject ageObj = new GameObject("AgeText");
            ageObj.transform.SetParent(cardPanel.transform, false);
            RectTransform aRect = ageObj.AddComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0f, 1f);
            aRect.anchorMax = new Vector2(1f, 1f);
            aRect.pivot = new Vector2(0.5f, 1f);
            aRect.anchoredPosition = new Vector2(0f, -244f);
            aRect.sizeDelta = new Vector2(-36f, 28f);

            ageText = ageObj.AddComponent<Text>();
            ageText.font = UIStyleUtility.GetGlobalFont(20);
            ageText.fontSize = 20;
            ageText.resizeTextForBestFit = true;
            ageText.resizeTextMinSize = 15;
            ageText.resizeTextMaxSize = 22;
            ageText.fontStyle = FontStyle.Bold;
            ageText.alignment = TextAnchor.MiddleCenter;
            ageText.color = new Color(0.98f, 0.88f, 0.30f);

            // 5. MESLEK METNİ
            GameObject occObj = new GameObject("OccupationText");
            occObj.transform.SetParent(cardPanel.transform, false);
            RectTransform oRect = occObj.AddComponent<RectTransform>();
            oRect.anchorMin = new Vector2(0f, 1f);
            oRect.anchorMax = new Vector2(1f, 1f);
            oRect.pivot = new Vector2(0.5f, 1f);
            oRect.anchoredPosition = new Vector2(0f, -276f);
            oRect.sizeDelta = new Vector2(-36f, 32f);

            occupationText = occObj.AddComponent<Text>();
            occupationText.font = UIStyleUtility.GetGlobalFont(21);
            occupationText.fontSize = 21;
            occupationText.resizeTextForBestFit = true;
            occupationText.resizeTextMinSize = 15;
            occupationText.resizeTextMaxSize = 23;
            occupationText.fontStyle = FontStyle.Bold;
            occupationText.alignment = TextAnchor.MiddleCenter;
            occupationText.color = new Color(0.25f, 0.95f, 0.50f);

            // Seperatör Çizgisi
            GameObject lineObj = new GameObject("DividerLine");
            lineObj.transform.SetParent(cardPanel.transform, false);
            RectTransform lRect = lineObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 1f);
            lRect.anchorMax = new Vector2(0.5f, 1f);
            lRect.pivot = new Vector2(0.5f, 1f);
            lRect.anchoredPosition = new Vector2(0f, -316f);
            lRect.sizeDelta = new Vector2(320f, 2f);
            Image lImg = lineObj.AddComponent<Image>();
            lImg.color = new Color(1f, 1f, 1f, 0.15f);

            // 5.5 MAĞAZA DURUMU ETİKETİ (Status Badge)
            GameObject badgeBox = new GameObject("StatusBadgeBox");
            badgeBox.transform.SetParent(cardPanel.transform, false);
            RectTransform badgeRect = badgeBox.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 1f);
            badgeRect.anchoredPosition = new Vector2(0f, -332f);
            badgeRect.sizeDelta = new Vector2(320f, 64f);

            Image badgeImg = badgeBox.AddComponent<Image>();
            badgeImg.color = new Color(0.16f, 0.20f, 0.30f, 0.85f);

            GameObject badgeTxtObj = new GameObject("StatusBadgeText");
            badgeTxtObj.transform.SetParent(badgeBox.transform, false);
            RectTransform btRect = badgeTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.offsetMin = new Vector2(8f, 4f);
            btRect.offsetMax = new Vector2(-8f, -4f);

            Text badgeTxt = badgeTxtObj.AddComponent<Text>();
            badgeTxt.font = UIStyleUtility.GetGlobalFont(18);
            badgeTxt.text = LocalizationManager.L("Customer_Status_Badge", "<color=#00E5FF>● MAĞAZA ZİYARETÇİSİ</color>\nTaze reyonları inceliyor 🛒", "<color=#00E5FF>● STORE VISITOR</color>\nBrowsing fresh shelves 🛒");
            badgeTxt.fontSize = 18;
            badgeTxt.resizeTextForBestFit = true;
            badgeTxt.resizeTextMinSize = 13;
            badgeTxt.resizeTextMaxSize = 19;
            badgeTxt.fontStyle = FontStyle.Bold;
            badgeTxt.alignment = TextAnchor.MiddleCenter;
            badgeTxt.color = new Color(0.85f, 0.90f, 0.98f);

            // 6. ALT KAPAT BUTONU ([ ✕ Kapat ])
            GameObject bottomCloseBtn = new GameObject("BottomCloseButton");
            bottomCloseBtn.transform.SetParent(cardPanel.transform, false);
            RectTransform bcr = bottomCloseBtn.AddComponent<RectTransform>();
            bcr.anchorMin = new Vector2(0.5f, 1f);
            bcr.anchorMax = new Vector2(0.5f, 1f);
            bcr.pivot = new Vector2(0.5f, 1f);
            bcr.anchoredPosition = new Vector2(0f, -418f);
            bcr.sizeDelta = new Vector2(320f, 48f);

            Image bcImg = bottomCloseBtn.AddComponent<Image>();
            bcImg.color = new Color(0.26f, 0.32f, 0.42f, 1f);
            bcImg.raycastTarget = true;

            Button bcBtn = bottomCloseBtn.AddComponent<Button>();
            bcBtn.targetGraphic = bcImg;
            ColorBlock bcb = bcBtn.colors;
            bcb.normalColor = new Color(0.26f, 0.32f, 0.42f, 1f);
            bcb.highlightedColor = new Color(0.34f, 0.42f, 0.54f, 1f);
            bcb.pressedColor = new Color(0.18f, 0.22f, 0.30f, 1f);
            bcb.selectedColor = bcb.normalColor;
            bcBtn.colors = bcb;
            bcBtn.onClick.AddListener(HideModal);

            GameObject bcTxtObj = new GameObject("Text");
            bcTxtObj.transform.SetParent(bottomCloseBtn.transform, false);
            RectTransform bctRect = bcTxtObj.AddComponent<RectTransform>();
            bctRect.anchorMin = Vector2.zero;
            bctRect.anchorMax = Vector2.one;
            Text bcTxt = bcTxtObj.AddComponent<Text>();
            bcTxt.font = UIStyleUtility.GetGlobalFont(20);
            bcTxt.text = LocalizationManager.L("Btn_Close", "✕ Kapat", "✕ Close");
            bcTxt.fontSize = 20;
            bcTxt.fontStyle = FontStyle.Bold;
            bcTxt.alignment = TextAnchor.MiddleCenter;
            bcTxt.color = Color.white;
            bcTxt.raycastTarget = false;

            if (closeButtonTransform != null) closeButtonTransform.SetAsLastSibling();
            modalContainer.SetActive(false);
        }

        private void UpdatePanelPosition()
        {
            if (cardPanel == null) return;
            RectTransform mainRect = cardPanel.GetComponent<RectTransform>();
            if (mainRect == null) return;

            float safeLeft = 115f; // Telefon kamerasından/çentiğinden uzak güvenli X tabanı
            if (Screen.safeArea.x > 0 && Screen.width > 0)
            {
                float canvasScale = 1920f / Screen.width;
                float dynamicSafeX = Screen.safeArea.x * canvasScale;
                safeLeft = Mathf.Max(115f, dynamicSafeX + 25f);
            }

            float safeBottom = 35f;
            if (Screen.safeArea.y > 0 && Screen.height > 0)
            {
                float canvasScaleY = 1080f / Screen.height;
                float dynamicSafeY = Screen.safeArea.y * canvasScaleY;
                safeBottom = Mathf.Max(35f, dynamicSafeY + 15f);
            }

            mainRect.anchoredPosition = new Vector2(safeLeft, safeBottom);
        }

        public void ShowCustomerProfile(CustomerProfileData profile)
        {
            if (profile == null) return;
            currentCustomerProfile = profile;
            if (modalContainer == null) BuildUI();

            UpdatePanelPosition();

            if (StaffProfileModalUI.Instance != null)
            {
                StaffProfileModalUI.Instance.HideModal();
            }

            if (closeButtonTransform != null) closeButtonTransform.SetAsLastSibling();
            modalContainer.SetActive(true);

            // 1. Gerçekçi Profil Fotoğrafı (Vesikalık)
            Sprite avatarSprite = ProfileAvatarDatabase.GetCustomerAvatar(profile);
            if (avatarPhotoImg != null)
            {
                if (avatarSprite != null)
                {
                    avatarPhotoImg.sprite = avatarSprite;
                    avatarPhotoImg.gameObject.SetActive(true);
                    if (avatarEmojiText != null) avatarEmojiText.gameObject.SetActive(false);
                }
                else
                {
                    avatarPhotoImg.gameObject.SetActive(false);
                    if (avatarEmojiText != null)
                    {
                        avatarEmojiText.text = profile.avatarEmoji;
                        avatarEmojiText.gameObject.SetActive(true);
                    }
                }
            }
            if (avatarBgImage != null) avatarBgImage.color = profile.avatarBgColor;

            // 2. İsim Soyisim
            if (nameText != null) nameText.text = profile.fullName;

            // 3. Cinsiyet
            string genderLabel = LocalizationManager.L("Label_Gender", "Cinsiyet", "Gender");
            if (genderText != null) genderText.text = $"{genderLabel}: {profile.LocalizedGenderText}";

            // 4. Yaş
            string ageLabel = LocalizationManager.L("Label_Age", "Yaş", "Age");
            if (ageText != null) ageText.text = $"{ageLabel}: {profile.age}";

            // 5. Meslek
            string occLabel = LocalizationManager.L("Label_Occupation", "Meslek", "Occupation");
            if (occupationText != null) occupationText.text = $"{occLabel}: {profile.LocalizedOccupationText}";
        }

        public bool IsModalOpen => modalContainer != null && modalContainer.activeSelf;

        public void HideModal()
        {
            if (modalContainer != null)
            {
                modalContainer.SetActive(false);
            }
        }
    }
}
