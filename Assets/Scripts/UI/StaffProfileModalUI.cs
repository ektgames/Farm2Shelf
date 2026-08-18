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

            modalContainer = new GameObject("Staff_Profile_Modal_LeftPanel");
            modalContainer.transform.SetParent(targetCanvasTransform, false);

            RectTransform mainRect = modalContainer.AddComponent<RectTransform>();
            // Ekranın SOL ALT Tarafına Sabitle (Telefon kamerasına/çentiğe denk gelmemesi için 180px sağa kaydırıldı)
            mainRect.anchorMin = new Vector2(0f, 0f);
            mainRect.anchorMax = new Vector2(0f, 0f);
            mainRect.pivot = new Vector2(0f, 0f);
            mainRect.anchoredPosition = new Vector2(30f, 30f); // Ekranın SOL ALT Köşesine Sabitlendi
            mainRect.sizeDelta = new Vector2(360f, 500f);        // Şık Kart Genişliği & Yüksekliği

            // Kart Arka Planı (Siyah Cam Doku & Şık Mavi Çerçeve)
            Image bgImage = modalContainer.AddComponent<Image>();
            bgImage.color = new Color(0.10f, 0.12f, 0.18f, 0.96f);

            // Üst Çerçeve Çizgisi Süsü (Gold / Blue Header Line)
            GameObject topDeco = new GameObject("TopDecoLine");
            topDeco.transform.SetParent(modalContainer.transform, false);
            RectTransform topRect = topDeco.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0f, 6f);
            Image topImg = topDeco.AddComponent<Image>();
            topImg.color = new Color(0.15f, 0.75f, 0.95f);

            // Kapat Butonu (X)
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(modalContainer.transform, false);
            RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-12f, -12f);
            closeRect.sizeDelta = new Vector2(36f, 36f);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.color = new Color(0.85f, 0.20f, 0.20f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(HideModal);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cTxtRect = closeTxtObj.AddComponent<RectTransform>();
            cTxtRect.anchorMin = Vector2.zero;
            cTxtRect.anchorMax = Vector2.one;
            Text cTxt = closeTxtObj.AddComponent<Text>();
            cTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cTxt.text = "✕";
            cTxt.fontSize = 20;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;

            // Panel Başlığı (PERSONEL PROFİLİ)
            GameObject titleObj = new GameObject("PanelTitle");
            titleObj.transform.SetParent(modalContainer.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -18f);
            titleRect.sizeDelta = new Vector2(-60f, 32f);

            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.text = LocalizationManager.L("Staff_Profile_Title", "PERSONEL PROFİLİ 💳", "STAFF PROFILE 💳");
            titleTxt.fontSize = 22;
            titleTxt.resizeTextForBestFit = true;
            titleTxt.resizeTextMinSize = 14;
            titleTxt.resizeTextMaxSize = 24;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = new Color(0.15f, 0.85f, 0.95f);

            // 1. PROFİL FOTOĞRAFI KUTUSU (Avatar Box)
            GameObject avatarBox = new GameObject("AvatarBox");
            avatarBox.transform.SetParent(modalContainer.transform, false);
            RectTransform avRect = avatarBox.AddComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0.5f, 1f);
            avRect.anchorMax = new Vector2(0.5f, 1f);
            avRect.pivot = new Vector2(0.5f, 1f);
            avRect.anchoredPosition = new Vector2(0f, -62f);
            avRect.sizeDelta = new Vector2(95f, 95f);

            avatarBgImage = avatarBox.AddComponent<Image>();
            avatarBgImage.color = new Color(0.18f, 0.45f, 0.75f);

            GameObject avTxtObj = new GameObject("AvatarEmoji");
            avTxtObj.transform.SetParent(avatarBox.transform, false);
            RectTransform avtRect = avTxtObj.AddComponent<RectTransform>();
            avtRect.anchorMin = Vector2.zero;
            avtRect.anchorMax = Vector2.one;
            avatarEmojiText = avTxtObj.AddComponent<Text>();
            avatarEmojiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            avatarEmojiText.fontSize = 52;
            avatarEmojiText.fontStyle = FontStyle.Bold;
            avatarEmojiText.alignment = TextAnchor.MiddleCenter;

            // 2. İSİM SOYİSİM METNİ (Hemen Altına)
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(modalContainer.transform, false);
            RectTransform nRect = nameObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0f, 1f);
            nRect.anchorMax = new Vector2(1f, 1f);
            nRect.pivot = new Vector2(0.5f, 1f);
            nRect.anchoredPosition = new Vector2(0f, -168f);
            nRect.sizeDelta = new Vector2(-40f, 34f);

            nameText = nameObj.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 26;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 16;
            nameText.resizeTextMaxSize = 28;
            nameText.fontStyle = FontStyle.Bold;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;

            // 2.5 CİNSİYET METNİ (İsim Soyisim'in Tam Altına)
            GameObject genderObj = new GameObject("GenderText");
            genderObj.transform.SetParent(modalContainer.transform, false);
            RectTransform gRect = genderObj.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0f, 1f);
            gRect.anchorMax = new Vector2(1f, 1f);
            gRect.pivot = new Vector2(0.5f, 1f);
            gRect.anchoredPosition = new Vector2(0f, -204f);
            gRect.sizeDelta = new Vector2(-40f, 26f);

            genderText = genderObj.AddComponent<Text>();
            genderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            genderText.fontSize = 19;
            genderText.resizeTextForBestFit = true;
            genderText.resizeTextMinSize = 12;
            genderText.resizeTextMaxSize = 20;
            genderText.fontStyle = FontStyle.Bold;
            genderText.alignment = TextAnchor.MiddleCenter;
            genderText.color = new Color(0.40f, 0.88f, 1.0f); // Canlı Açık Mavi / Cyan

            // 3. YAŞ METNİ (Cinsiyet'in Hemen Altına)
            GameObject ageObj = new GameObject("AgeText");
            ageObj.transform.SetParent(modalContainer.transform, false);
            RectTransform aRect = ageObj.AddComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0f, 1f);
            aRect.anchorMax = new Vector2(1f, 1f);
            aRect.pivot = new Vector2(0.5f, 1f);
            aRect.anchoredPosition = new Vector2(0f, -232f);
            aRect.sizeDelta = new Vector2(-40f, 26f);

            ageText = ageObj.AddComponent<Text>();
            ageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ageText.fontSize = 19;
            ageText.resizeTextForBestFit = true;
            ageText.resizeTextMinSize = 12;
            ageText.resizeTextMaxSize = 20;
            ageText.alignment = TextAnchor.MiddleCenter;
            ageText.color = new Color(0.95f, 0.85f, 0.25f);

            // 4. MESLEK / GÖREV UNVANI METNİ (Yaş'ın Hemen Altına)
            GameObject roleObj = new GameObject("RoleText");
            roleObj.transform.SetParent(modalContainer.transform, false);
            RectTransform rRect = roleObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0f, 1f);
            rRect.anchorMax = new Vector2(1f, 1f);
            rRect.pivot = new Vector2(0.5f, 1f);
            rRect.anchoredPosition = new Vector2(0f, -260f);
            rRect.sizeDelta = new Vector2(-40f, 28f);

            roleText = roleObj.AddComponent<Text>();
            roleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roleText.fontSize = 20;
            roleText.resizeTextForBestFit = true;
            roleText.resizeTextMinSize = 13;
            roleText.resizeTextMaxSize = 21;
            roleText.fontStyle = FontStyle.Bold;
            roleText.alignment = TextAnchor.MiddleCenter;
            roleText.color = new Color(0.20f, 0.85f, 0.45f);

            // Seperatör Çizgisi
            GameObject lineObj = new GameObject("DividerLine");
            lineObj.transform.SetParent(modalContainer.transform, false);
            RectTransform lRect = lineObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.5f, 1f);
            lRect.anchorMax = new Vector2(0.5f, 1f);
            lRect.pivot = new Vector2(0.5f, 1f);
            lRect.anchoredPosition = new Vector2(0f, -292f);
            lRect.sizeDelta = new Vector2(300f, 2f);
            Image lImg = lineObj.AddComponent<Image>();
            lImg.color = new Color(1f, 1f, 1f, 0.15f);

            // 5. VARDİYA VE MAAŞ DETAYLARI
            GameObject shiftObj = new GameObject("ShiftSalaryText");
            shiftObj.transform.SetParent(modalContainer.transform, false);
            RectTransform sRect = shiftObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 1f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.pivot = new Vector2(0.5f, 1f);
            sRect.anchoredPosition = new Vector2(0f, -302f);
            sRect.sizeDelta = new Vector2(-40f, 48f);

            shiftSalaryText = shiftObj.AddComponent<Text>();
            shiftSalaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            shiftSalaryText.fontSize = 18;
            shiftSalaryText.resizeTextForBestFit = true;
            shiftSalaryText.resizeTextMinSize = 12;
            shiftSalaryText.resizeTextMaxSize = 19;
            shiftSalaryText.alignment = TextAnchor.MiddleCenter;
            shiftSalaryText.color = new Color(0.85f, 0.88f, 0.92f);

            // 6. ANLIK CANLI GÖREV DURUMU KUTUSU
            GameObject dutyBox = new GameObject("DutyBox");
            dutyBox.transform.SetParent(modalContainer.transform, false);
            RectTransform dRect = dutyBox.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.5f, 1f);
            dRect.anchorMax = new Vector2(0.5f, 1f);
            dRect.pivot = new Vector2(0.5f, 1f);
            dRect.anchoredPosition = new Vector2(0f, -365f);
            dRect.sizeDelta = new Vector2(310f, 105f);

            Image dImg = dutyBox.AddComponent<Image>();
            dImg.color = new Color(0.15f, 0.18f, 0.25f, 0.85f);

            GameObject dutyTxtObj = new GameObject("DutyText");
            dutyTxtObj.transform.SetParent(dutyBox.transform, false);
            RectTransform dtRect = dutyTxtObj.AddComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(12f, 8f);
            dtRect.offsetMax = new Vector2(-12f, -8f);

            dutyStatusText = dutyTxtObj.AddComponent<Text>();
            dutyStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dutyStatusText.fontSize = 18;
            dutyStatusText.resizeTextForBestFit = true;
            dutyStatusText.resizeTextMinSize = 12;
            dutyStatusText.resizeTextMaxSize = 19;
            dutyStatusText.alignment = TextAnchor.MiddleCenter;
            dutyStatusText.color = new Color(0.95f, 0.82f, 0.15f);

            modalContainer.SetActive(false);
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

            // Cinsiyet Hesabı & Avatar Seçimi
            bool isFemale = staff.isFemale || StaffManager.IsFemaleName(staff.name);
            avatarEmojiText.text = isFemale ? "♀" : "♂";
            avatarEmojiText.color = isFemale ? new Color(1.0f, 0.55f, 0.85f) : new Color(0.35f, 0.85f, 1.0f);

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
            lastClickTime = Time.time;
        }

        public void HideModal()
        {
            if (modalContainer != null)
            {
                modalContainer.SetActive(false);
            }
        }

        public bool IsModalOpen => modalContainer != null && modalContainer.activeSelf;
    }
}
