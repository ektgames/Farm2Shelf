using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Personellerin üzerine tıklandığında ekranın SOL tarafında açılan 
    /// eksiksiz, hatasız ve modern Personel Profil Kartı arayüzü.
    /// </summary>
    public class StaffProfileModalUI : MonoBehaviour
    {
        public static StaffProfileModalUI Instance { get; private set; }

        private GameObject modalContainer;
        private Text nameText;
        private Text genderText;
        private Text ageText;
        private Text roleText;
        private Text shiftSalaryText;
        private Text dutyStatusText;
        private Text avatarEmojiText;
        private Image avatarBgImage;
        private Image avatarPhotoImg;

        private StaffMember currentStaff;
        private static float lastClickTime = 0f;

        private string lastLiveStatus = "";

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
            if (modalContainer != null && modalContainer.activeSelf && currentStaff != null)
            {
                ShowStaffProfile(currentStaff, lastLiveStatus);
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

        private GameObject cardPanel;

        private void BuildUI()
        {
            if (modalContainer != null) return;

            // Ana HUD Canvas'ını Ara veya Özel Profil Canvas'ı Oluştur
            Transform targetCanvasTransform = null;
            GameObject existingHUDCanvas = GameObject.Find("Farm2Shelf_HUD_Canvas");
            if (existingHUDCanvas != null)
            {
                targetCanvasTransform = existingHUDCanvas.transform;
            }
            else
            {
                GameObject mainCanvasObj = GameObject.Find("Staff_Profile_Canvas");
                if (mainCanvasObj == null)
                {
                    mainCanvasObj = new GameObject("Staff_Profile_Canvas");
                    Canvas canvas = mainCanvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 105;

                    CanvasScaler scaler = mainCanvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;

                    mainCanvasObj.AddComponent<GraphicRaycaster>();
                }
                targetCanvasTransform = mainCanvasObj.transform;
            }

            // Kök Modal Kapsayıcı (Ekranı Kaplayan Root)
            modalContainer = new GameObject("Staff_Profile_Modal_CanvasRoot");
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

            // Personel Kartı Paneli (Sol Alt Taraf - Safe Area Desteğiyle)
            cardPanel = new GameObject("Staff_Profile_CardPanel");
            cardPanel.transform.SetParent(modalContainer.transform, false);

            RectTransform mainRect = cardPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0f, 0f);
            mainRect.anchorMax = new Vector2(0f, 0f);
            mainRect.pivot = new Vector2(0f, 0f);
            mainRect.anchoredPosition = new Vector2(115f, 35f); // Mobil kamera çentiğini aşan güvenli sol alt pozisyon
            mainRect.sizeDelta = new Vector2(380f, 540f);

            // Kart Arka Planı (Siyah Cam Doku & Şık Çerçeve)
            Image bgImage = cardPanel.AddComponent<Image>();
            bgImage.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);
            bgImage.raycastTarget = true;

            // Üst Çerçeve Çizgisi Süsü (Neon Cyan Header Line)
            GameObject topDeco = new GameObject("TopDecoLine");
            topDeco.transform.SetParent(cardPanel.transform, false);
            RectTransform topRect = topDeco.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0f, 6f);
            Image topImg = topDeco.AddComponent<Image>();
            topImg.color = new Color(0.15f, 0.85f, 0.95f);

            // Panel Başlığı (PERSONEL PROFİLİ)
            GameObject titleObj = new GameObject("PanelTitle");
            titleObj.transform.SetParent(cardPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(18f, -16f);
            titleRect.sizeDelta = new Vector2(-75f, 36f);

            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleTxt.font == null) titleTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            titleTxt.text = LocalizationManager.L("Staff_Profile_Title", "PERSONEL PROFİLİ 💳", "STAFF PROFILE 💳");
            titleTxt.fontSize = 24;
            titleTxt.resizeTextForBestFit = true;
            titleTxt.resizeTextMinSize = 16;
            titleTxt.resizeTextMaxSize = 25;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = new Color(0.15f, 0.85f, 0.95f);
            titleTxt.raycastTarget = false;

            // Üst Sağ Kapat Butonu (X - Yüksek Kalite ve Dokunmatik Dostu)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(cardPanel.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-10f, -10f);
            closeRect.sizeDelta = new Vector2(44f, 44f);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.90f, 0.20f, 0.22f, 1f);
            closeImg.raycastTarget = true;

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            ColorBlock cb = closeBtn.colors;
            cb.normalColor = new Color(0.90f, 0.20f, 0.22f, 1f);
            cb.highlightedColor = new Color(1.0f, 0.32f, 0.34f, 1f);
            cb.pressedColor = new Color(0.70f, 0.12f, 0.14f, 1f);
            cb.selectedColor = cb.normalColor;
            closeBtn.colors = cb;
            closeBtn.onClick.AddListener(HideModal);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cTxtRect = closeTxtObj.AddComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            Text cTxt = closeTxtObj.AddComponent<Text>();
            cTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cTxt.font == null) cTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 26);
            cTxt.text = "✕";
            cTxt.fontSize = 26;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.raycastTarget = false;

            Outline cOutline = closeTxtObj.AddComponent<Outline>();
            cOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            cOutline.effectDistance = new Vector2(1.5f, -1.5f);

            closeBtnObj.transform.SetAsLastSibling();

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
            avatarBgImage.color = new Color(0.18f, 0.45f, 0.75f);

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

            // Yedek Emoji Metni
            GameObject avTxtObj = new GameObject("AvatarEmoji");
            avTxtObj.transform.SetParent(avatarBox.transform, false);
            RectTransform avtRect = avTxtObj.AddComponent<RectTransform>();
            avtRect.anchorMin = Vector2.zero;
            avtRect.anchorMax = Vector2.one;
            avatarEmojiText = avTxtObj.AddComponent<Text>();
            avatarEmojiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (avatarEmojiText.font == null) avatarEmojiText.font = Font.CreateDynamicFontFromOSFont("Arial", 54);
            avatarEmojiText.fontSize = 54;
            avatarEmojiText.fontStyle = FontStyle.Bold;
            avatarEmojiText.alignment = TextAnchor.MiddleCenter;
            avatarEmojiText.gameObject.SetActive(false);

            // 2. İSİM SOYİSİM METNİ (Hemen Altına)
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cardPanel.transform, false);
            RectTransform nRect = nameObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0f, 1f);
            nRect.anchorMax = new Vector2(1f, 1f);
            nRect.pivot = new Vector2(0.5f, 1f);
            nRect.anchoredPosition = new Vector2(0f, -172f);
            nRect.sizeDelta = new Vector2(-36f, 36f);

            nameText = nameObj.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (nameText.font == null) nameText.font = Font.CreateDynamicFontFromOSFont("Arial", 26);
            nameText.fontSize = 26;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 18;
            nameText.resizeTextMaxSize = 28;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;

            // 2.5 CİNSİYET METNİ (İsim Soyisim'in Tam Altına)
            GameObject genderObj = new GameObject("GenderText");
            genderObj.transform.SetParent(cardPanel.transform, false);
            RectTransform gRect = genderObj.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0f, 1f);
            gRect.anchorMax = new Vector2(1f, 1f);
            gRect.pivot = new Vector2(0.5f, 1f);
            gRect.anchoredPosition = new Vector2(0f, -212f);
            gRect.sizeDelta = new Vector2(-36f, 28f);

            genderText = genderObj.AddComponent<Text>();
            genderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (genderText.font == null) genderText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            genderText.fontSize = 20;
            genderText.resizeTextForBestFit = true;
            genderText.resizeTextMinSize = 15;
            genderText.resizeTextMaxSize = 22;
            genderText.fontStyle = FontStyle.Bold;
            genderText.alignment = TextAnchor.MiddleCenter;
            genderText.color = new Color(0.40f, 0.88f, 1.0f); // Canlı Açık Mavi / Cyan

            // 3. YAŞ METNİ (Cinsiyet'in Hemen Altına)
            GameObject ageObj = new GameObject("AgeText");
            ageObj.transform.SetParent(cardPanel.transform, false);
            RectTransform aRect = ageObj.AddComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0f, 1f);
            aRect.anchorMax = new Vector2(1f, 1f);
            aRect.pivot = new Vector2(0.5f, 1f);
            aRect.anchoredPosition = new Vector2(0f, -244f);
            aRect.sizeDelta = new Vector2(-36f, 28f);

            ageText = ageObj.AddComponent<Text>();
            ageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (ageText.font == null) ageText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            ageText.fontSize = 20;
            ageText.resizeTextForBestFit = true;
            ageText.resizeTextMinSize = 15;
            ageText.resizeTextMaxSize = 22;
            ageText.fontStyle = FontStyle.Bold;
            ageText.alignment = TextAnchor.MiddleCenter;
            ageText.color = new Color(0.98f, 0.88f, 0.30f);

            // 4. MESLEK / GÖREV UNVANI METNİ (Yaş'ın Hemen Altına)
            GameObject roleObj = new GameObject("RoleText");
            roleObj.transform.SetParent(cardPanel.transform, false);
            RectTransform rRect = roleObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0f, 1f);
            rRect.anchorMax = new Vector2(1f, 1f);
            rRect.pivot = new Vector2(0.5f, 1f);
            rRect.anchoredPosition = new Vector2(0f, -276f);
            rRect.sizeDelta = new Vector2(-36f, 32f);

            roleText = roleObj.AddComponent<Text>();
            roleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (roleText.font == null) roleText.font = Font.CreateDynamicFontFromOSFont("Arial", 21);
            roleText.fontSize = 21;
            roleText.resizeTextForBestFit = true;
            roleText.resizeTextMinSize = 15;
            roleText.resizeTextMaxSize = 23;
            roleText.fontStyle = FontStyle.Bold;
            roleText.alignment = TextAnchor.MiddleCenter;
            roleText.color = new Color(0.25f, 0.95f, 0.50f);

            // Seperatör Çizgisi
            GameObject lineObj = new GameObject("DividerLine");
            lineObj.transform.SetParent(cardPanel.transform, false);
            RectTransform lRect = lineObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 1f);
            lRect.anchorMax = new Vector2(0.5f, 1f);
            lRect.pivot = new Vector2(0.5f, 1f);
            lRect.anchoredPosition = new Vector2(0f, -314f);
            lRect.sizeDelta = new Vector2(330f, 2f);
            Image lImg = lineObj.AddComponent<Image>();
            lImg.color = new Color(1f, 1f, 1f, 0.15f);

            // 5. VARDİYA VE MAAŞ DETAYLARI
            GameObject shiftObj = new GameObject("ShiftSalaryText");
            shiftObj.transform.SetParent(cardPanel.transform, false);
            RectTransform sRect = shiftObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 1f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.pivot = new Vector2(0.5f, 1f);
            sRect.anchoredPosition = new Vector2(0f, -322f);
            sRect.sizeDelta = new Vector2(-36f, 44f);

            shiftSalaryText = shiftObj.AddComponent<Text>();
            shiftSalaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (shiftSalaryText.font == null) shiftSalaryText.font = Font.CreateDynamicFontFromOSFont("Arial", 19);
            shiftSalaryText.fontSize = 19;
            shiftSalaryText.resizeTextForBestFit = true;
            shiftSalaryText.resizeTextMinSize = 14;
            shiftSalaryText.resizeTextMaxSize = 20;
            shiftSalaryText.fontStyle = FontStyle.Bold;
            shiftSalaryText.alignment = TextAnchor.MiddleCenter;
            shiftSalaryText.color = new Color(0.90f, 0.93f, 0.98f);

            // 6. ANLIK CANLI GÖREV DURUMU KUTUSU
            GameObject dutyBox = new GameObject("DutyBox");
            dutyBox.transform.SetParent(cardPanel.transform, false);
            RectTransform dRect = dutyBox.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 1f);
            dRect.anchorMax = new Vector2(0.5f, 1f);
            dRect.pivot = new Vector2(0.5f, 1f);
            dRect.anchoredPosition = new Vector2(0f, -370f);
            dRect.sizeDelta = new Vector2(330f, 92f);

            Image dImg = dutyBox.AddComponent<Image>();
            dImg.color = new Color(0.12f, 0.16f, 0.24f, 0.92f);

            GameObject dutyTxtObj = new GameObject("DutyText");
            dutyTxtObj.transform.SetParent(dutyBox.transform, false);
            RectTransform dtRect = dutyTxtObj.AddComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(10f, 6f);
            dtRect.offsetMax = new Vector2(-10f, -6f);

            dutyStatusText = dutyTxtObj.AddComponent<Text>();
            dutyStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (dutyStatusText.font == null) dutyStatusText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            dutyStatusText.fontSize = 18;
            dutyStatusText.resizeTextForBestFit = true;
            dutyStatusText.resizeTextMinSize = 14;
            dutyStatusText.resizeTextMaxSize = 20;
            dutyStatusText.fontStyle = FontStyle.Bold;
            dutyStatusText.alignment = TextAnchor.MiddleCenter;
            dutyStatusText.color = new Color(1f, 0.86f, 0.25f);

            // 7. ALT KAPAT BUTONU ([ ✕ Kapat ])
            GameObject bottomCloseBtn = new GameObject("BottomCloseButton");
            bottomCloseBtn.transform.SetParent(cardPanel.transform, false);
            RectTransform bcr = bottomCloseBtn.AddComponent<RectTransform>();
            bcr.anchorMin = new Vector2(0.5f, 1f);
            bcr.anchorMax = new Vector2(0.5f, 1f);
            bcr.pivot = new Vector2(0.5f, 1f);
            bcr.anchoredPosition = new Vector2(0f, -474f);
            bcr.sizeDelta = new Vector2(330f, 46f);

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
            bcTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bcTxt.font == null) bcTxt.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            bcTxt.text = LocalizationManager.L("Btn_Close", "✕ Kapat", "✕ Close");
            bcTxt.fontSize = 20;
            bcTxt.fontStyle = FontStyle.Bold;
            bcTxt.alignment = TextAnchor.MiddleCenter;
            bcTxt.color = Color.white;
            bcTxt.raycastTarget = false;

            modalContainer.SetActive(false);
        }

        private void UpdatePanelPosition()
        {
            if (cardPanel == null) return;
            RectTransform mainRect = cardPanel.GetComponent<RectTransform>();
            if (mainRect == null) return;

            float safeLeft = 115f; // Mobil kamera çentiğini aşan güvenli X mesafesi
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

        public void ShowStaffProfile(StaffMember staff, string liveDutyStatus)
        {
            if (modalContainer == null)
            {
                BuildUI();
            }
            if (staff == null || modalContainer == null) return;
            currentStaff = staff;
            lastLiveStatus = liveDutyStatus;

            UpdatePanelPosition();

            // Cinsiyet Hesabı & Gerçekçi Avatar Seçimi
            bool isFemale = staff.isFemale || StaffManager.IsFemaleName(staff.name);
            Sprite avatarSprite = ProfileAvatarDatabase.GetStaffAvatar(staff);

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
                        avatarEmojiText.text = isFemale ? "♀" : "♂";
                        avatarEmojiText.color = isFemale ? new Color(1.0f, 0.55f, 0.85f) : new Color(0.35f, 0.85f, 1.0f);
                        avatarEmojiText.gameObject.SetActive(true);
                    }
                }
            }

            genderText.text = isFemale ?
                LocalizationManager.L("Gender_Female", "♀️ Cinsiyet: Kadın", "♀️ Gender: Female") :
                LocalizationManager.L("Gender_Male", "♂️ Cinsiyet: Erkek", "♂️ Gender: Male");

            switch (staff.role)
            {
                case StaffRole.Kasiyer:
                    avatarBgImage.color = new Color(0.15f, 0.45f, 0.85f);
                    roleText.text = LocalizationManager.L("Role_Cashier", "💼 Meslek: Kasiyer (Kasa Sorumlusu)", "💼 Role: Cashier (Checkout Specialist)");
                    break;
                case StaffRole.Reyoncu:
                    avatarBgImage.color = new Color(0.15f, 0.70f, 0.35f);
                    roleText.text = LocalizationManager.L("Role_Restocker", "📦 Meslek: Reyoncu (Raf & Stok Sorumlusu)", "📦 Role: Restocker (Shelf & Stock)");
                    break;
                case StaffRole.Temizlikçi:
                    avatarBgImage.color = new Color(0.85f, 0.55f, 0.15f);
                    roleText.text = LocalizationManager.L("Role_Cleaner", "🧹 Meslek: Temizlik Görevlisi", "🧹 Role: Cleaner (Sanitation)");
                    break;
                case StaffRole.Güvenlik:
                    avatarBgImage.color = new Color(0.80f, 0.18f, 0.18f);
                    roleText.text = LocalizationManager.L("Role_Security", "🛡️ Meslek: Güvenlik Görevlisi", "🛡️ Role: Security Officer");
                    break;
                case StaffRole.Çiftçi:
                case StaffRole.DeneyimliÇiftçi:
                case StaffRole.UstaÇiftlikSorumlusu:
                case StaffRole.TarımOtomasyonUzmanı:
                    avatarBgImage.color = new Color(0.45f, 0.35f, 0.20f);
                    roleText.text = LocalizationManager.L("Role_Farmer", "🌾 Meslek: Çiftçi", "🌾 Role: Farmer");
                    break;
                default:
                    avatarBgImage.color = new Color(0.45f, 0.35f, 0.75f);
                    string roleWord = LocalizationManager.L("Role_Word", "Meslek", "Role");
                    roleText.text = $"📋 {roleWord}: {staff.role}";
                    break;
            }

            // İsim Soyisim & Yaş
            nameText.text = staff.name;
            int staffAge = 22 + (Mathf.Abs(staff.name.GetHashCode()) % 20);
            ageText.text = LocalizationManager.L("Staff_Age", $"🎂 Yaşı: {staffAge} Yaşında", $"🎂 Age: {staffAge} Years Old");

            // Vardiya ve Maaş
            string shiftLabel = LocalizationManager.L("Label_Shift", "Vardiya", "Shift");
            string wageLabel = LocalizationManager.L("Label_DailyWage", "Günlük Maaş", "Daily Wage");
            shiftSalaryText.text = $"⏰ {shiftLabel}: {EKTPhoneManager.GetLocalizedShiftHours(staff.shiftHours)}\n💵 {wageLabel}: {staff.dailySalary}C";

            // Anlık Canlı Görev Durumu
            string statusHeader = LocalizationManager.L("Live_Status_Header", "<color=#00E676>● CANLI DURUM</color>", "<color=#00E676>● LIVE STATUS</color>");
            dutyStatusText.text = $"{statusHeader}\n{liveDutyStatus}";

            if (CustomerProfileModalUI.Instance != null)
            {
                CustomerProfileModalUI.Instance.HideModal();
            }

            modalContainer.SetActive(true);
            ModalManager.SetModalOpen(true);
            lastClickTime = Time.time;
        }

        public void HideModal()
        {
            if (modalContainer != null)
            {
                modalContainer.SetActive(false);
            }
            ModalManager.SetModalOpen(false);
        }

        public bool IsModalOpen => modalContainer != null && modalContainer.activeSelf;
    }
}
