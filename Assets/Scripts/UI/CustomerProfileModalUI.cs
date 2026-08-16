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

        public bool IsModalOpen => modalContainer != null && modalContainer.activeSelf;

        private CustomerProfileData currentCustomerProfile;

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

            // Arka Plan Karartma (Overlay Backdrop - Dışarıya Tıklayınca Kapatır)
            GameObject backdropObj = new GameObject("Backdrop");
            backdropObj.transform.SetParent(modalContainer.transform, false);
            RectTransform bdRect = backdropObj.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;

            Image bdImg = backdropObj.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.45f);
            bdImg.raycastTarget = true;

            Button bdBtn = backdropObj.AddComponent<Button>();
            bdBtn.targetGraphic = bdImg;
            bdBtn.onClick.AddListener(HideModal);

            // Müşteri Kartı Paneli (Sol Alt Taraf)
            cardPanel = new GameObject("Customer_Profile_CardPanel");
            cardPanel.transform.SetParent(modalContainer.transform, false);

            RectTransform mainRect = cardPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0f, 0f);
            mainRect.anchorMax = new Vector2(0f, 0f);
            mainRect.pivot = new Vector2(0f, 0f);
            mainRect.anchoredPosition = new Vector2(180f, 25f);
            mainRect.sizeDelta = new Vector2(360f, 440f);

            // Kart Arka Planı
            Image bgImage = cardPanel.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.14f, 0.22f, 0.98f);
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

            // Üst Sağ Kapat Butonu (X)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(cardPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-10f, -10f);
            closeRect.sizeDelta = new Vector2(38f, 38f);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.85f, 0.20f, 0.20f);
            closeImg.raycastTarget = true;

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(HideModal);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cTxtRect = closeTxtObj.AddComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            Text cTxt = closeTxtObj.AddComponent<Text>();
            cTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cTxt.font == null) cTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
            cTxt.text = "✕";
            cTxt.fontSize = 22;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.raycastTarget = false;

            // Panel Başlığı (MÜŞTERİ PROFİLİ)
            GameObject titleObj = new GameObject("PanelTitle");
            titleObj.transform.SetParent(cardPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(18f, -18f);
            titleRect.sizeDelta = new Vector2(-70f, 32f);

            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleTxt.font == null) titleTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
            titleTxt.text = LocalizationManager.L("Customer_Profile_Title", "MÜŞTERİ PROFİLİ 🛒", "CUSTOMER PROFILE 🛒");
            titleTxt.fontSize = 22;
            titleTxt.resizeTextForBestFit = true;
            titleTxt.resizeTextMinSize = 14;
            titleTxt.resizeTextMaxSize = 24;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = new Color(0.75f, 0.45f, 0.95f);

            // 1. PROFİL FOTOĞRAFI KUTUSU (Avatar Box)
            GameObject avatarBox = new GameObject("AvatarBox");
            avatarBox.transform.SetParent(cardPanel.transform, false);
            RectTransform avRect = avatarBox.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0.5f, 1f);
            avRect.anchorMax = new Vector2(0.5f, 1f);
            avRect.pivot = new Vector2(0.5f, 1f);
            avRect.anchoredPosition = new Vector2(0f, -58f);
            avRect.sizeDelta = new Vector2(90f, 90f);

            avatarBgImage = avatarBox.AddComponent<Image>();
            avatarBgImage.color = new Color(0.25f, 0.35f, 0.65f);

            GameObject avTxtObj = new GameObject("AvatarEmoji");
            avTxtObj.transform.SetParent(avatarBox.transform, false);
            RectTransform avtRect = avTxtObj.AddComponent<RectTransform>();
            avtRect.anchorMin = Vector2.zero;
            avtRect.anchorMax = Vector2.one;
            avatarEmojiText = avTxtObj.AddComponent<Text>();
            avatarEmojiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (avatarEmojiText.font == null) avatarEmojiText.font = Font.CreateDynamicFontFromOSFont("Arial", 48);
            avatarEmojiText.fontSize = 48;
            avatarEmojiText.fontStyle = FontStyle.Bold;
            avatarEmojiText.alignment = TextAnchor.MiddleCenter;

            // 2. İSİM SOYİSİM METNİ
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardPanel.transform, false);
            RectTransform nRect = nameObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0f, 1f);
            nRect.anchorMax = new Vector2(1f, 1f);
            nRect.pivot = new Vector2(0.5f, 1f);
            nRect.anchoredPosition = new Vector2(0f, -158f);
            nRect.sizeDelta = new Vector2(-40f, 32f);

            nameText = nameObj.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (nameText.font == null) nameText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            nameText.fontSize = 24;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 16;
            nameText.resizeTextMaxSize = 26;
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
            gRect.anchoredPosition = new Vector2(0f, -194f);
            gRect.sizeDelta = new Vector2(-40f, 26f);

            genderText = genderObj.AddComponent<Text>();
            genderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (genderText.font == null) genderText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            genderText.fontSize = 18;
            genderText.resizeTextForBestFit = true;
            genderText.resizeTextMinSize = 12;
            genderText.resizeTextMaxSize = 20;
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
            aRect.anchoredPosition = new Vector2(0f, -222f);
            aRect.sizeDelta = new Vector2(-40f, 26f);

            ageText = ageObj.AddComponent<Text>();
            ageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (ageText.font == null) ageText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            ageText.fontSize = 18;
            ageText.resizeTextForBestFit = true;
            ageText.resizeTextMinSize = 12;
            ageText.resizeTextMaxSize = 20;
            ageText.alignment = TextAnchor.MiddleCenter;
            ageText.color = new Color(0.95f, 0.85f, 0.25f);

            // 5. MESLEK METNİ
            GameObject occObj = new GameObject("OccupationText");
            occObj.transform.SetParent(cardPanel.transform, false);
            RectTransform oRect = occObj.AddComponent<RectTransform>();
            oRect.anchorMin = new Vector2(0f, 1f);
            oRect.anchorMax = new Vector2(1f, 1f);
            oRect.pivot = new Vector2(0.5f, 1f);
            oRect.anchoredPosition = new Vector2(0f, -250f);
            oRect.sizeDelta = new Vector2(-40f, 28f);

            occupationText = occObj.AddComponent<Text>();
            occupationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (occupationText.font == null) occupationText.font = Font.CreateDynamicFontFromOSFont("Arial", 19);
            occupationText.fontSize = 19;
            occupationText.resizeTextForBestFit = true;
            occupationText.resizeTextMinSize = 13;
            occupationText.resizeTextMaxSize = 21;
            occupationText.fontStyle = FontStyle.Bold;
            occupationText.alignment = TextAnchor.MiddleCenter;
            occupationText.color = new Color(0.20f, 0.85f, 0.45f);

            // 6. ALT KAPAT BUTONU ([ ❌ Kapat ])
            GameObject bottomCloseBtn = new GameObject("BottomCloseButton");
            bottomCloseBtn.transform.SetParent(cardPanel.transform, false);
            RectTransform bcr = bottomCloseBtn.AddComponent<RectTransform>();
            bcr.anchorMin = new Vector2(0.10f, 0.05f);
            bcr.anchorMax = new Vector2(0.90f, 0.16f);
            bcr.offsetMin = Vector2.zero;
            bcr.offsetMax = Vector2.zero;

            Image bcImg = bottomCloseBtn.AddComponent<Image>();
            bcImg.color = new Color(0.35f, 0.40f, 0.48f);
            bcImg.raycastTarget = true;

            Button bcBtn = bottomCloseBtn.AddComponent<Button>();
            bcBtn.targetGraphic = bcImg;
            bcBtn.onClick.AddListener(HideModal);

            GameObject bcTxtObj = new GameObject("Text");
            bcTxtObj.transform.SetParent(bottomCloseBtn.transform, false);
            RectTransform bctRect = bcTxtObj.AddComponent<RectTransform>();
            bctRect.anchorMin = Vector2.zero;
            bctRect.anchorMax = Vector2.one;
            Text bcTxt = bcTxtObj.AddComponent<Text>();
            bcTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bcTxt.font == null) bcTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            bcTxt.text = LocalizationManager.L("Btn_Close", "❌ Kapat", "❌ Close");
            bcTxt.fontSize = 16;
            bcTxt.fontStyle = FontStyle.Bold;
            bcTxt.alignment = TextAnchor.MiddleCenter;
            bcTxt.color = Color.white;
            bcTxt.raycastTarget = false;

            modalContainer.SetActive(false);
        }

        public void ShowCustomerProfile(CustomerProfileData profile)
        {
            if (profile == null) return;
            currentCustomerProfile = profile;
            if (modalContainer == null) BuildUI();

            if (StaffProfileModalUI.Instance != null)
            {
                StaffProfileModalUI.Instance.HideModal();
            }

            modalContainer.SetActive(true);
            ModalManager.SetModalOpen(true);

            // 1. Profil Fotoğrafı & Emoji
            if (avatarEmojiText != null) avatarEmojiText.text = profile.avatarEmoji;
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

        public void HideModal()
        {
            if (modalContainer != null)
            {
                modalContainer.SetActive(false);
            }
            ModalManager.SetModalOpen(false);
        }
    }
}
