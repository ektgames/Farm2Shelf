using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Farm2Shelf.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Ayarlar Arayüzü (Settings Modal UI).
    /// Arka plan müzikleri (BGM), ses efektleri (SFX) ve Canlı Türkçe / İngilizce Dil Seçim Paneli içerir.
    /// Dil değiştirildiğinde tüm ekranlar anında seçilen dille güncellenir.
    /// </summary>
    public class SettingsModalUI : MonoBehaviour
    {
        public static SettingsModalUI Instance { get; private set; }
        public bool IsSettingsOpen => canvasObj != null && canvasObj.activeInHierarchy;

        private GameObject canvasObj;
        private Text titleText;
        private Text currentTrackText;
        private Text nextTrackButtonText;
        private Text bgmVolText;
        private Text sfxVolText;
        private Image bgmMuteImg;
        private Image sfxMuteImg;
        private Text bgmMuteText;
        private Text sfxMuteText;
        private Text sfxTitleText;
        private Text languageTitleText;
        private Text infoText;
        private Image turkishButtonImage;
        private Image englishButtonImage;
        private Transform closeBtnTransform;

        private static readonly Color LanguageSelectedColor = new Color(0.20f, 0.75f, 0.35f, 1f);
        private static readonly Color LanguageIdleColor = new Color(0.22f, 0.28f, 0.36f, 1f);

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
            BindLocalization();
        }

        private void BindLocalization()
        {
            LocalizationManager localization = LocalizationManager.EnsureForGameplay();
            if (localization == null) return;
            localization.OnLanguageChanged -= HandleLanguageChanged;
            localization.OnLanguageChanged += HandleLanguageChanged;
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
                RefreshLocalizedTexts();
            }
        }

        private void SelectLanguage(GameLanguage language)
        {
            BindLocalization();
            LocalizationManager localization = LocalizationManager.EnsureForGameplay();
            if (localization == null) return;

            if (localization.CurrentLanguage != language)
            {
                localization.SetLanguage(language);
            }

            RefreshLocalizedTexts();
        }

        public void ShowModal()
        {
            BindLocalization();
            BuildUI();
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnTrackChanged -= HandleTrackChanged;
                AudioManager.Instance.OnTrackChanged += HandleTrackChanged;
            }
        }

        public void HideModal()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnTrackChanged -= HandleTrackChanged;
            }
            if (canvasObj != null)
            {
                canvasObj.SetActive(false);
                Destroy(canvasObj);
                canvasObj = null;
            }
            titleText = null;
            currentTrackText = null;
            nextTrackButtonText = null;
            bgmVolText = null;
            sfxVolText = null;
            bgmMuteImg = null;
            sfxMuteImg = null;
            bgmMuteText = null;
            sfxMuteText = null;
            sfxTitleText = null;
            languageTitleText = null;
            infoText = null;
            turkishButtonImage = null;
            englishButtonImage = null;
            closeBtnTransform = null;
        }

        private void HandleTrackChanged(int trackNum, string trackTitle)
        {
            if (currentTrackText != null)
            {
                int totalTracks = AudioManager.Instance != null ? AudioManager.Instance.TotalTracks : 12;
                string trackLabel = LocalizationManager.L("Track_Label", "Parça", "Track");
                currentTrackText.text = $"🎵 <b>{trackLabel} {trackNum}/{totalTracks}:</b> {trackTitle}";
            }
        }

        private void BuildUI()
        {
            if (canvasObj != null)
            {
                canvasObj.SetActive(false);
                Destroy(canvasObj);
                canvasObj = null;
            }

            canvasObj = new GameObject("Settings_Modal_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            // Arka Plan
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);
            bdImg.raycastTarget = true;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Panel (720x620)
            GameObject panelObj = new GameObject("Settings_Panel");
            panelObj.transform.SetParent(backdrop.transform, false);

            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(720f, 620f);

            Image pBg = panelObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(720, 620, 20, 3, new Color(0.55f, 0.35f, 0.75f), new Color(0.10f, 0.14f, 0.18f, 0.98f));
            pBg.raycastTarget = false;

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 260f);
            tRect.sizeDelta = new Vector2(500f, 50f);

            titleText = titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.text = LocalizationManager.L("Settings_Title", "⚙️ OYUN VE SES AYARLARI", "⚙️ GAME & AUDIO SETTINGS");
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.75f, 0.45f, 0.95f);
            titleText.raycastTarget = false;

            // Kapat Butonu (X)
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(panelObj.transform, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchoredPosition = new Vector2(320f, 260f);
            clRect.sizeDelta = new Vector2(46f, 46f);

            Image clBg = closeObj.AddComponent<Image>();
            clBg.sprite = UIStyleUtility.CreateRoundedPillSprite(46, 46, 23, new Color(0.92f, 0.18f, 0.20f, 1f));
            clBg.raycastTarget = true;

            Button clBtn = closeObj.AddComponent<Button>();
            clBtn.targetGraphic = clBg;
            clBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                HideModal();
            });

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

            closeBtnTransform = closeObj.transform;

            // ==================== BÖLÜM 1: 10 PARÇALIK BGM MÜZİK KONTROLÜ ====================
            GameObject musicBox = new GameObject("MusicControlBox");
            musicBox.transform.SetParent(panelObj.transform, false);
            RectTransform mbRect = musicBox.AddComponent<RectTransform>();
            mbRect.anchoredPosition = new Vector2(0f, 175f);
            mbRect.sizeDelta = new Vector2(640f, 105f);

            Image mbBg = musicBox.AddComponent<Image>();
            mbBg.sprite = UIStyleUtility.CreateOutlinePillSprite(640, 105, 18, 2, new Color(0.35f, 0.70f, 0.95f), new Color(0.12f, 0.16f, 0.22f, 0.95f));
            mbBg.raycastTarget = false;

            // Şu an çalan parça metni
            GameObject trackObj = new GameObject("TrackTitle");
            trackObj.transform.SetParent(musicBox.transform, false);
            RectTransform trRect = trackObj.AddComponent<RectTransform>();
            trRect.anchoredPosition = new Vector2(-80f, 18f);
            trRect.sizeDelta = new Vector2(440f, 40f);

            currentTrackText = trackObj.AddComponent<Text>();
            currentTrackText.font = font;
            int trNum = AudioManager.Instance != null ? AudioManager.Instance.CurrentTrackIndex : 1;
            string trTitle = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTrackTitle() : "İlham Veren Akustik Folk 🌾";
            string trackTextLabel = LocalizationManager.L("Track_Label", "Parça", "Track");
            int totalTracksCount = AudioManager.Instance != null ? AudioManager.Instance.TotalTracks : 12;
            currentTrackText.text = $"🎵 <b>{trackTextLabel} {trNum}/{totalTracksCount}:</b> {trTitle}";
            currentTrackText.fontSize = 16;
            currentTrackText.alignment = TextAnchor.MiddleLeft;
            currentTrackText.color = new Color(0.35f, 0.85f, 0.95f);
            currentTrackText.raycastTarget = false;

            // Sonraki Şarkı Butonu (⏭️)
            GameObject nextTrackBtnObj = new GameObject("NextTrackBtn");
            nextTrackBtnObj.transform.SetParent(musicBox.transform, false);
            RectTransform ntRect = nextTrackBtnObj.AddComponent<RectTransform>();
            ntRect.anchoredPosition = new Vector2(230f, 18f);
            ntRect.sizeDelta = new Vector2(140f, 40f);

            Image ntBg = nextTrackBtnObj.AddComponent<Image>();
            ntBg.sprite = UIStyleUtility.CreateRoundedPillSprite(140, 40, 20, new Color(0.20f, 0.65f, 0.90f));
            ntBg.color = Color.white;

            Button ntBtn = nextTrackBtnObj.AddComponent<Button>();
            ntBtn.targetGraphic = ntBg;
            ntBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayButtonClick();
                    AudioManager.Instance.NextTrack();
                }
            });

            GameObject ntTxtObj = new GameObject("Label");
            ntTxtObj.transform.SetParent(nextTrackBtnObj.transform, false);
            RectTransform nttRect = ntTxtObj.AddComponent<RectTransform>();
            nttRect.anchorMin = Vector2.zero;
            nttRect.anchorMax = Vector2.one;

            nextTrackButtonText = ntTxtObj.AddComponent<Text>();
            nextTrackButtonText.font = font;
            nextTrackButtonText.text = LocalizationManager.L("Btn_NextTrack", "⏭️ SONRAKİ", "⏭️ NEXT");
            nextTrackButtonText.fontSize = 14;
            nextTrackButtonText.fontStyle = FontStyle.Bold;
            nextTrackButtonText.alignment = TextAnchor.MiddleCenter;
            nextTrackButtonText.color = Color.white;
            nextTrackButtonText.raycastTarget = false;

            // BGM MUTE / SESSİZ DÜĞMESİ
            GameObject bgmMuteBtnObj = new GameObject("BGMMuteBtn");
            bgmMuteBtnObj.transform.SetParent(musicBox.transform, false);
            RectTransform bmmRect = bgmMuteBtnObj.AddComponent<RectTransform>();
            bmmRect.anchoredPosition = new Vector2(-200f, -24f);
            bmmRect.sizeDelta = new Vector2(180f, 38f);

            bgmMuteImg = bgmMuteBtnObj.AddComponent<Image>();
            bool isBgmMuted = AudioManager.Instance != null && AudioManager.Instance.IsBGMMuted;
            bgmMuteImg.sprite = UIStyleUtility.CreateRoundedPillSprite(180, 38, 19, isBgmMuted ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.20f, 0.75f, 0.35f));
            bgmMuteImg.color = Color.white;

            Button bmmBtn = bgmMuteBtnObj.AddComponent<Button>();
            bmmBtn.targetGraphic = bgmMuteImg;
            bmmBtn.onClick.AddListener(OnToggleBGMMuteClicked);

            GameObject bmmTxtObj = new GameObject("Label");
            bmmTxtObj.transform.SetParent(bgmMuteBtnObj.transform, false);
            RectTransform bmmtRect = bmmTxtObj.AddComponent<RectTransform>();
            bmmtRect.anchorMin = Vector2.zero;
            bmmtRect.anchorMax = Vector2.one;

            bgmMuteText = bmmTxtObj.AddComponent<Text>();
            bgmMuteText.font = font;
            bgmMuteText.text = isBgmMuted ? LocalizationManager.L("BGM_Off", "🔇 MÜZİK: KAPALI", "🔇 MUSIC: OFF") : LocalizationManager.L("BGM_On", "🔊 MÜZİK: AÇIK", "🔊 MUSIC: ON");
            bgmMuteText.fontSize = 14;
            bgmMuteText.fontStyle = FontStyle.Bold;
            bgmMuteText.alignment = TextAnchor.MiddleCenter;
            bgmMuteText.color = Color.white;
            bgmMuteText.raycastTarget = false;

            // BGM VOLUME HIZLI DEĞİŞTİRME BUTONLARI (+ / -)
            GameObject bgmLessBtn = CreateVolButton(musicBox.transform, new Vector2(20f, -24f), "-", () => ChangeBGMVolume(-0.1f));
            GameObject bgmVolLabelObj = new GameObject("BGMVolLabel");
            bgmVolLabelObj.transform.SetParent(musicBox.transform, false);
            RectTransform bvlRect = bgmVolLabelObj.AddComponent<RectTransform>();
            bvlRect.anchoredPosition = new Vector2(100f, -24f);
            bvlRect.sizeDelta = new Vector2(120f, 38f);

            bgmVolText = bgmVolLabelObj.AddComponent<Text>();
            bgmVolText.font = font;
            float curBgmVol = AudioManager.Instance != null ? AudioManager.Instance.BGMVolume : 0.6f;
            string volWord = LocalizationManager.L("Vol_Word", "Ses", "Vol");
            string volPercentFmt = LocalizationManager.L("Vol_PercentFmt", "%{0}", "{0}%");
            bgmVolText.text = $"{volWord}: {string.Format(volPercentFmt, Mathf.RoundToInt(curBgmVol * 100))}";
            bgmVolText.fontSize = 15;
            bgmVolText.alignment = TextAnchor.MiddleCenter;
            bgmVolText.color = Color.white;
            bgmVolText.raycastTarget = false;

            GameObject bgmMoreBtn = CreateVolButton(musicBox.transform, new Vector2(180f, -24f), "+", () => ChangeBGMVolume(0.1f));

            // ==================== BÖLÜM 2: SES EFEKTLERİ (SFX) KONTROLÜ ====================
            GameObject sfxBox = new GameObject("SFXControlBox");
            sfxBox.transform.SetParent(panelObj.transform, false);
            RectTransform sbRect = sfxBox.AddComponent<RectTransform>();
            sbRect.anchoredPosition = new Vector2(0f, 55f);
            sbRect.sizeDelta = new Vector2(640f, 105f);

            Image sbBg = sfxBox.AddComponent<Image>();
            sbBg.sprite = UIStyleUtility.CreateOutlinePillSprite(640, 105, 18, 2, new Color(0.95f, 0.65f, 0.15f), new Color(0.12f, 0.16f, 0.22f, 0.95f));
            sbBg.raycastTarget = false;

            GameObject sfxTitleObj = new GameObject("SFXTitle");
            sfxTitleObj.transform.SetParent(sfxBox.transform, false);
            RectTransform stRect = sfxTitleObj.AddComponent<RectTransform>();
            stRect.anchoredPosition = new Vector2(-150f, 18f);
            stRect.sizeDelta = new Vector2(300f, 40f);

            sfxTitleText = sfxTitleObj.AddComponent<Text>();
            sfxTitleText.font = font;
            sfxTitleText.text = LocalizationManager.L("SFX_Title", "🔔 <b>SES EFEKTLERİ (SFX):</b>", "🔔 <b>SOUND EFFECTS (SFX):</b>");
            sfxTitleText.fontSize = 16;
            sfxTitleText.alignment = TextAnchor.MiddleLeft;
            sfxTitleText.color = new Color(0.95f, 0.65f, 0.15f);
            sfxTitleText.raycastTarget = false;

            // SFX MUTE / SESSİZ DÜĞMESİ
            GameObject sfxMuteBtnObj = new GameObject("SFXMuteBtn");
            sfxMuteBtnObj.transform.SetParent(sfxBox.transform, false);
            RectTransform smmRect = sfxMuteBtnObj.AddComponent<RectTransform>();
            smmRect.anchoredPosition = new Vector2(-200f, -24f);
            smmRect.sizeDelta = new Vector2(180f, 38f);

            sfxMuteImg = sfxMuteBtnObj.AddComponent<Image>();
            bool isSfxMuted = AudioManager.Instance != null && AudioManager.Instance.IsSFXMuted;
            sfxMuteImg.sprite = UIStyleUtility.CreateRoundedPillSprite(180, 38, 19, isSfxMuted ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.20f, 0.75f, 0.35f));
            sfxMuteImg.color = Color.white;

            Button smmBtn = sfxMuteBtnObj.AddComponent<Button>();
            smmBtn.targetGraphic = sfxMuteImg;
            smmBtn.onClick.AddListener(OnToggleSFXMuteClicked);

            GameObject smmTxtObj = new GameObject("Label");
            smmTxtObj.transform.SetParent(sfxMuteBtnObj.transform, false);
            RectTransform smmtRect = smmTxtObj.AddComponent<RectTransform>();
            smmtRect.anchorMin = Vector2.zero;
            smmtRect.anchorMax = Vector2.one;

            sfxMuteText = smmTxtObj.AddComponent<Text>();
            sfxMuteText.font = font;
            sfxMuteText.text = isSfxMuted ? LocalizationManager.L("SFX_Off", "🔇 EFEKT: KAPALI", "🔇 SFX: OFF") : LocalizationManager.L("SFX_On", "🔊 EFEKT: AÇIK", "🔊 SFX: ON");
            sfxMuteText.fontSize = 14;
            sfxMuteText.fontStyle = FontStyle.Bold;
            sfxMuteText.alignment = TextAnchor.MiddleCenter;
            sfxMuteText.color = Color.white;
            sfxMuteText.raycastTarget = false;

            // SFX VOLUME HIZLI DEĞİŞTİRME BUTONLARI (+ / -)
            GameObject sfxLessBtn = CreateVolButton(sfxBox.transform, new Vector2(20f, -24f), "-", () => ChangeSFXVolume(-0.1f));
            GameObject sfxVolLabelObj = new GameObject("SFXVolLabel");
            sfxVolLabelObj.transform.SetParent(sfxBox.transform, false);
            RectTransform svlRect = sfxVolLabelObj.AddComponent<RectTransform>();
            svlRect.anchoredPosition = new Vector2(100f, -24f);
            svlRect.sizeDelta = new Vector2(120f, 38f);

            sfxVolText = sfxVolLabelObj.AddComponent<Text>();
            sfxVolText.font = font;
            float curSfxVol = AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 0.8f;
            sfxVolText.text = $"{volWord}: {string.Format(volPercentFmt, Mathf.RoundToInt(curSfxVol * 100))}";
            sfxVolText.fontSize = 15;
            sfxVolText.alignment = TextAnchor.MiddleCenter;
            sfxVolText.color = Color.white;
            sfxVolText.raycastTarget = false;

            GameObject sfxMoreBtn = CreateVolButton(sfxBox.transform, new Vector2(180f, -24f), "+", () => ChangeSFXVolume(0.1f));

            // ==================== BÖLÜM 3: OYUN DİLİ (GAME LANGUAGE) KONTROLÜ ====================
            GameObject langBox = new GameObject("LanguageControlBox");
            langBox.transform.SetParent(panelObj.transform, false);
            RectTransform lbRect = langBox.AddComponent<RectTransform>();
            lbRect.anchoredPosition = new Vector2(0f, -65f);
            lbRect.sizeDelta = new Vector2(640f, 105f);

            Image lbBg = langBox.AddComponent<Image>();
            lbBg.sprite = UIStyleUtility.CreateOutlinePillSprite(640, 105, 18, 2, new Color(0.25f, 0.80f, 0.45f), new Color(0.12f, 0.16f, 0.22f, 0.95f));
            lbBg.raycastTarget = false;

            GameObject langTitleObj = new GameObject("LangTitle");
            langTitleObj.transform.SetParent(langBox.transform, false);
            RectTransform ltRect = langTitleObj.AddComponent<RectTransform>();
            ltRect.anchoredPosition = new Vector2(0f, 28f);
            ltRect.sizeDelta = new Vector2(500f, 28f);

            languageTitleText = langTitleObj.AddComponent<Text>();
            languageTitleText.font = font;
            languageTitleText.text = LocalizationManager.L("Lang_Title", "🌐 <b>OYUN DİLİ / GAME LANGUAGE:</b>", "🌐 <b>GAME LANGUAGE / OYUN DİLİ:</b>");
            languageTitleText.fontSize = 16;
            languageTitleText.alignment = TextAnchor.MiddleCenter;
            languageTitleText.color = new Color(0.35f, 0.90f, 0.55f);
            languageTitleText.raycastTarget = false;

            Transform languageLayer = CreateLanguageLayer(panelObj.transform);
            turkishButtonImage = CreateLanguageButton(languageLayer, new Vector2(-155f, -87f), "Türkçe", GameLanguage.Turkish);
            englishButtonImage = CreateLanguageButton(languageLayer, new Vector2(155f, -87f), "English", GameLanguage.English);
            RefreshLanguageButtons();

            // ==================== BÖLÜM 4: BİLGİ SEKMESİ ====================
            GameObject infoObj = new GameObject("InfoBox");
            infoObj.transform.SetParent(panelObj.transform, false);
            RectTransform iRect = infoObj.AddComponent<RectTransform>();
            iRect.anchoredPosition = new Vector2(0f, -210f);
            iRect.sizeDelta = new Vector2(640f, 110f);

            infoText = infoObj.AddComponent<Text>();
            infoText.font = font;
            infoText.fontSize = 14;
            infoText.alignment = TextAnchor.MiddleCenter;
            infoText.color = Color.white;
            infoText.raycastTarget = false;
            infoText.supportRichText = true;
            RefreshInfoText();

            if (turkishButtonImage != null) turkishButtonImage.transform.SetAsLastSibling();
            if (englishButtonImage != null) englishButtonImage.transform.SetAsLastSibling();
            if (closeBtnTransform != null) closeBtnTransform.SetAsLastSibling();
        }

        private static Transform CreateLanguageLayer(Transform parent)
        {
            GameObject layer = new GameObject("LanguageButtonLayer");
            layer.transform.SetParent(parent, false);
            RectTransform rect = layer.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Canvas layerCanvas = layer.AddComponent<Canvas>();
            layerCanvas.overrideSorting = true;
            layerCanvas.sortingOrder = 1300;
            layer.AddComponent<GraphicRaycaster>();
            return layer.transform;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<InputSystemUIInputModule>();
#else
            esObj.AddComponent<StandaloneInputModule>();
#endif
        }

        private Image CreateLanguageButton(Transform parent, Vector2 pos, string label, GameLanguage language)
        {
            GameObject btnObj = new GameObject(language == GameLanguage.Turkish ? "TRLangBtn" : "ENLangBtn");
            btnObj.transform.SetParent(parent, false);

            RectTransform buttonRect = btnObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = pos;
            buttonRect.sizeDelta = new Vector2(280f, 52f);

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(280, 52, 26, LanguageIdleColor);
            bg.color = Color.white;
            bg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.interactable = true;
            btn.transition = Selectable.Transition.None;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                SelectLanguage(language);
            });

            LanguagePickButton picker = btnObj.AddComponent<LanguagePickButton>();
            picker.Setup(language, SelectLanguage);

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = label;
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            return bg;
        }

        private void RefreshLanguageButtons()
        {
            LocalizationManager loc = LocalizationManager.EnsureForGameplay();
            bool isTurkish = loc == null || loc.IsTurkish;
            ApplyLanguageButtonVisual(turkishButtonImage, isTurkish);
            ApplyLanguageButtonVisual(englishButtonImage, !isTurkish);
        }

        private static void ApplyLanguageButtonVisual(Image image, bool selected)
        {
            if (image == null) return;
            image.sprite = UIStyleUtility.CreateRoundedPillSprite(280, 52, 26, selected ? LanguageSelectedColor : LanguageIdleColor);
            image.color = Color.white;
        }

        private void RefreshInfoText()
        {
            if (infoText == null) return;
            infoText.text = LocalizationManager.L(
                "Info_Text",
                "📱 <b>Kontrol & Grafikler:</b> Mobil Dokunmatik (Kaydır / Pinch-Zoom / Döndür)\n" +
                "🖥️ <b>Grafik Kalitesi:</b> Yüksek (Low-Poly Ultra)\n\n" +
                "<color=#80D8FF>💡 Tüm ses seviyeleri, müzik ve dil tercihleri otomatik kaydedilir!</color>",
                "📱 <b>Controls & Graphics:</b> Mobile Touch (Swipe / Pinch-Zoom / Twist)\n" +
                "🖥️ <b>Graphic Quality:</b> High (Low-Poly Ultra)\n\n" +
                "<color=#80D8FF>💡 All audio, music, and language settings are automatically saved!</color>"
            );
        }

        private void RefreshLocalizedTexts()
        {
            if (titleText != null)
            {
                titleText.text = LocalizationManager.L("Settings_Title", "⚙️ OYUN VE SES AYARLARI", "⚙️ GAME & AUDIO SETTINGS");
            }

            if (currentTrackText != null)
            {
                int trNum = AudioManager.Instance != null ? AudioManager.Instance.CurrentTrackIndex : 1;
                string trTitle = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTrackTitle() : "İlham Veren Akustik Folk 🌾";
                string trackTextLabel = LocalizationManager.L("Track_Label", "Parça", "Track");
                int totalTracksCount = AudioManager.Instance != null ? AudioManager.Instance.TotalTracks : 12;
                currentTrackText.text = $"🎵 <b>{trackTextLabel} {trNum}/{totalTracksCount}:</b> {trTitle}";
            }

            if (nextTrackButtonText != null)
            {
                nextTrackButtonText.text = LocalizationManager.L("Btn_NextTrack", "⏭️ SONRAKİ", "⏭️ NEXT");
            }

            bool isBgmMuted = AudioManager.Instance != null && AudioManager.Instance.IsBGMMuted;
            if (bgmMuteText != null)
            {
                bgmMuteText.text = isBgmMuted
                    ? LocalizationManager.L("BGM_Off", "🔇 MÜZİK: KAPALI", "🔇 MUSIC: OFF")
                    : LocalizationManager.L("BGM_On", "🔊 MÜZİK: AÇIK", "🔊 MUSIC: ON");
            }

            bool isSfxMuted = AudioManager.Instance != null && AudioManager.Instance.IsSFXMuted;
            if (sfxMuteText != null)
            {
                sfxMuteText.text = isSfxMuted
                    ? LocalizationManager.L("SFX_Off", "🔇 EFEKT: KAPALI", "🔇 SFX: OFF")
                    : LocalizationManager.L("SFX_On", "🔊 EFEKT: AÇIK", "🔊 SFX: ON");
            }

            if (sfxTitleText != null)
            {
                sfxTitleText.text = LocalizationManager.L("SFX_Title", "🔔 <b>SES EFEKTLERİ (SFX):</b>", "🔔 <b>SOUND EFFECTS (SFX):</b>");
            }

            if (languageTitleText != null)
            {
                languageTitleText.text = LocalizationManager.L("Lang_Title", "🌐 <b>OYUN DİLİ / GAME LANGUAGE:</b>", "🌐 <b>GAME LANGUAGE / OYUN DİLİ:</b>");
            }

            string volWord = LocalizationManager.L("Vol_Word", "Ses", "Vol");
            string volPercentFmt = LocalizationManager.L("Vol_PercentFmt", "%{0}", "{0}%");
            if (bgmVolText != null && AudioManager.Instance != null)
            {
                bgmVolText.text = $"{volWord}: {string.Format(volPercentFmt, Mathf.RoundToInt(AudioManager.Instance.BGMVolume * 100))}";
            }
            if (sfxVolText != null && AudioManager.Instance != null)
            {
                sfxVolText.text = $"{volWord}: {string.Format(volPercentFmt, Mathf.RoundToInt(AudioManager.Instance.SFXVolume * 100))}";
            }

            RefreshInfoText();
            RefreshLanguageButtons();
        }

        private GameObject CreateVolButton(Transform parent, Vector2 pos, string label, System.Action onClick)
        {
            GameObject btnObj = new GameObject("VolBtn_" + label);
            btnObj.transform.SetParent(parent, false);

            RectTransform r = btnObj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(40f, 38f);

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(40, 38, 19, new Color(0.25f, 0.35f, 0.45f));
            bg.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                onClick?.Invoke();
            });

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = txtObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = label;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            return btnObj;
        }

        private void OnToggleBGMMuteClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
                AudioManager.Instance.ToggleBGMMute();
                bool isMuted = AudioManager.Instance.IsBGMMuted;
                if (bgmMuteImg != null)
                {
                    bgmMuteImg.sprite = UIStyleUtility.CreateRoundedPillSprite(180, 38, 19, isMuted ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.20f, 0.75f, 0.35f));
                    bgmMuteImg.color = Color.white;
                }
                bgmMuteText.text = isMuted ? LocalizationManager.L("BGM_Off", "🔇 MÜZİK: KAPALI", "🔇 MUSIC: OFF") : LocalizationManager.L("BGM_On", "🔊 MÜZİK: AÇIK", "🔊 MUSIC: ON");
            }
        }

        private void OnToggleSFXMuteClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
                AudioManager.Instance.ToggleSFXMute();
                bool isMuted = AudioManager.Instance.IsSFXMuted;
                if (sfxMuteImg != null)
                {
                    sfxMuteImg.sprite = UIStyleUtility.CreateRoundedPillSprite(180, 38, 19, isMuted ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.20f, 0.75f, 0.35f));
                    sfxMuteImg.color = Color.white;
                }
                sfxMuteText.text = isMuted ? LocalizationManager.L("SFX_Off", "🔇 EFEKT: KAPALI", "🔇 SFX: OFF") : LocalizationManager.L("SFX_On", "🔊 EFEKT: AÇIK", "🔊 SFX: ON");
            }
        }

        private void ChangeBGMVolume(float delta)
        {
            if (AudioManager.Instance != null)
            {
                float newVol = Mathf.Clamp01(AudioManager.Instance.BGMVolume + delta);
                AudioManager.Instance.SetBGMVolume(newVol);
                string volWord = LocalizationManager.L("Vol_Word", "Ses", "Vol");
                string volPercentFmt = LocalizationManager.L("Vol_PercentFmt", "%{0}", "{0}%");
                if (bgmVolText != null) bgmVolText.text = $"{volWord}: {string.Format(volPercentFmt, Mathf.RoundToInt(newVol * 100))}";
            }
        }

        private void ChangeSFXVolume(float delta)
        {
            if (AudioManager.Instance != null)
            {
                float newVol = Mathf.Clamp01(AudioManager.Instance.SFXVolume + delta);
                AudioManager.Instance.SetSFXVolume(newVol);
                string volWord = LocalizationManager.L("Vol_Word", "Ses", "Vol");
                string volPercentFmt = LocalizationManager.L("Vol_PercentFmt", "%{0}", "{0}%");
                if (sfxVolText != null) sfxVolText.text = $"{volWord}: {string.Format(volPercentFmt, Mathf.RoundToInt(newVol * 100))}";
            }
        }
    }

    public class LanguagePickButton : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        private GameLanguage language;
        private System.Action<GameLanguage> onPicked;
        private bool pickedThisPress;

        public void Setup(GameLanguage selectedLanguage, System.Action<GameLanguage> callback)
        {
            language = selectedLanguage;
            onPicked = callback;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pickedThisPress = true;
            onPicked?.Invoke(language);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (pickedThisPress)
            {
                pickedThisPress = false;
                return;
            }
            onPicked?.Invoke(language);
        }
    }
}
