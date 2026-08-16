using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

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

        private GameObject canvasObj;
        private Text currentTrackText;
        private Text bgmVolText;
        private Text sfxVolText;
        private Image bgmMuteImg;
        private Image sfxMuteImg;
        private Text bgmMuteText;
        private Text sfxMuteText;

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
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnTrackChanged += HandleTrackChanged;
            }
        }

        public void HideModal()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.OnTrackChanged -= HandleTrackChanged;
            }
            if (canvasObj != null) Destroy(canvasObj);
        }

        private void HandleTrackChanged(int trackNum, string trackTitle)
        {
            if (currentTrackText != null)
            {
                string trackLabel = LocalizationManager.L("Track_Label", "Parça", "Track");
                currentTrackText.text = $"🎵 <b>{trackLabel} {trackNum}/10:</b> {trackTitle}";
            }
        }

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);

            canvasObj = new GameObject("Settings_Modal_Canvas");
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

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 260f);
            tRect.sizeDelta = new Vector2(500f, 50f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = LocalizationManager.L("Settings_Title", "⚙️ OYUN VE SES AYARLARI", "⚙️ GAME & AUDIO SETTINGS");
            tText.fontSize = 24;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.75f, 0.45f, 0.95f);

            // Kapat Butonu (X)
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(panelObj.transform, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchoredPosition = new Vector2(320f, 260f);
            clRect.sizeDelta = new Vector2(40f, 40f);

            Image clBg = closeObj.AddComponent<Image>();
            clBg.sprite = UIStyleUtility.CreateRoundedPillSprite(40, 40, 8, new Color(0.85f, 0.20f, 0.25f));

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
            clTxt.fontSize = 18;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;
            clTxt.raycastTarget = false;

            // ==================== BÖLÜM 1: 10 PARÇALIK BGM MÜZİK KONTROLÜ ====================
            GameObject musicBox = new GameObject("MusicControlBox");
            musicBox.transform.SetParent(panelObj.transform, false);
            RectTransform mbRect = musicBox.AddComponent<RectTransform>();
            mbRect.anchoredPosition = new Vector2(0f, 175f);
            mbRect.sizeDelta = new Vector2(640f, 105f);

            Image mbBg = musicBox.AddComponent<Image>();
            mbBg.sprite = UIStyleUtility.CreateOutlinePillSprite(640, 105, 12, 1, new Color(0.35f, 0.70f, 0.95f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

            // Şu an çalan parça metni
            GameObject trackObj = new GameObject("TrackTitle");
            trackObj.transform.SetParent(musicBox.transform, false);
            RectTransform trRect = trackObj.AddComponent<RectTransform>();
            trRect.anchoredPosition = new Vector2(-80f, 18f);
            trRect.sizeDelta = new Vector2(440f, 40f);

            currentTrackText = trackObj.AddComponent<Text>();
            currentTrackText.font = font;
            int trNum = AudioManager.Instance != null ? AudioManager.Instance.CurrentTrackIndex : 1;
            string trTitle = AudioManager.Instance != null ? AudioManager.Instance.GetCurrentTrackTitle() : "Çiftlikte Sabah Güneşi 🌾";
            string trackTextLabel = LocalizationManager.L("Track_Label", "Parça", "Track");
            currentTrackText.text = $"🎵 <b>{trackTextLabel} {trNum}/10:</b> {trTitle}";
            currentTrackText.fontSize = 16;
            currentTrackText.alignment = TextAnchor.MiddleLeft;
            currentTrackText.color = new Color(0.35f, 0.85f, 0.95f);

            // Sonraki Şarkı Butonu (⏭️)
            GameObject nextTrackBtnObj = new GameObject("NextTrackBtn");
            nextTrackBtnObj.transform.SetParent(musicBox.transform, false);
            RectTransform ntRect = nextTrackBtnObj.AddComponent<RectTransform>();
            ntRect.anchoredPosition = new Vector2(230f, 18f);
            ntRect.sizeDelta = new Vector2(140f, 40f);

            Image ntBg = nextTrackBtnObj.AddComponent<Image>();
            ntBg.sprite = UIStyleUtility.CreateRoundedPillSprite(140, 40, 10, new Color(0.20f, 0.65f, 0.90f));

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

            Text ntTxt = ntTxtObj.AddComponent<Text>();
            ntTxt.font = font;
            ntTxt.text = LocalizationManager.L("Btn_NextTrack", "⏭️ SONRAKİ", "⏭️ NEXT");
            ntTxt.fontSize = 14;
            ntTxt.fontStyle = FontStyle.Bold;
            ntTxt.alignment = TextAnchor.MiddleCenter;
            ntTxt.color = Color.white;
            ntTxt.raycastTarget = false;

            // BGM MUTE / SESSİZ DÜĞMESİ
            GameObject bgmMuteBtnObj = new GameObject("BGMMuteBtn");
            bgmMuteBtnObj.transform.SetParent(musicBox.transform, false);
            RectTransform bmmRect = bgmMuteBtnObj.AddComponent<RectTransform>();
            bmmRect.anchoredPosition = new Vector2(-200f, -24f);
            bmmRect.sizeDelta = new Vector2(180f, 38f);

            bgmMuteImg = bgmMuteBtnObj.AddComponent<Image>();
            bool isBgmMuted = AudioManager.Instance != null && AudioManager.Instance.IsBGMMuted;
            bgmMuteImg.sprite = UIStyleUtility.CreateRoundedPillSprite(180, 38, 8, isBgmMuted ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.20f, 0.75f, 0.35f));

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

            GameObject bgmMoreBtn = CreateVolButton(musicBox.transform, new Vector2(180f, -24f), "+", () => ChangeBGMVolume(0.1f));

            // ==================== BÖLÜM 2: SES EFEKTLERİ (SFX) KONTROLÜ ====================
            GameObject sfxBox = new GameObject("SFXControlBox");
            sfxBox.transform.SetParent(panelObj.transform, false);
            RectTransform sbRect = sfxBox.AddComponent<RectTransform>();
            sbRect.anchoredPosition = new Vector2(0f, 55f);
            sbRect.sizeDelta = new Vector2(640f, 105f);

            Image sbBg = sfxBox.AddComponent<Image>();
            sbBg.sprite = UIStyleUtility.CreateOutlinePillSprite(640, 105, 12, 1, new Color(0.95f, 0.65f, 0.15f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

            GameObject sfxTitleObj = new GameObject("SFXTitle");
            sfxTitleObj.transform.SetParent(sfxBox.transform, false);
            RectTransform stRect = sfxTitleObj.AddComponent<RectTransform>();
            stRect.anchoredPosition = new Vector2(-150f, 18f);
            stRect.sizeDelta = new Vector2(300f, 40f);

            Text stTxt = sfxTitleObj.AddComponent<Text>();
            stTxt.font = font;
            stTxt.text = LocalizationManager.L("SFX_Title", "🔔 <b>SES EFEKTLERİ (SFX):</b>", "🔔 <b>SOUND EFFECTS (SFX):</b>");
            stTxt.fontSize = 16;
            stTxt.alignment = TextAnchor.MiddleLeft;
            stTxt.color = new Color(0.95f, 0.65f, 0.15f);

            // SFX MUTE / SESSİZ DÜĞMESİ
            GameObject sfxMuteBtnObj = new GameObject("SFXMuteBtn");
            sfxMuteBtnObj.transform.SetParent(sfxBox.transform, false);
            RectTransform smmRect = sfxMuteBtnObj.AddComponent<RectTransform>();
            smmRect.anchoredPosition = new Vector2(-200f, -24f);
            smmRect.sizeDelta = new Vector2(180f, 38f);

            sfxMuteImg = sfxMuteBtnObj.AddComponent<Image>();
            bool isSfxMuted = AudioManager.Instance != null && AudioManager.Instance.IsSFXMuted;
            sfxMuteImg.sprite = UIStyleUtility.CreateRoundedPillSprite(180, 38, 8, isSfxMuted ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.20f, 0.75f, 0.35f));

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

            GameObject sfxMoreBtn = CreateVolButton(sfxBox.transform, new Vector2(180f, -24f), "+", () => ChangeSFXVolume(0.1f));

            // ==================== BÖLÜM 3: OYUN DİLİ (GAME LANGUAGE) KONTROLÜ ====================
            GameObject langBox = new GameObject("LanguageControlBox");
            langBox.transform.SetParent(panelObj.transform, false);
            RectTransform lbRect = langBox.AddComponent<RectTransform>();
            lbRect.anchoredPosition = new Vector2(0f, -65f);
            lbRect.sizeDelta = new Vector2(640f, 105f);

            Image lbBg = langBox.AddComponent<Image>();
            lbBg.sprite = UIStyleUtility.CreateOutlinePillSprite(640, 105, 12, 1, new Color(0.25f, 0.80f, 0.45f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

            GameObject langTitleObj = new GameObject("LangTitle");
            langTitleObj.transform.SetParent(langBox.transform, false);
            RectTransform ltRect = langTitleObj.AddComponent<RectTransform>();
            ltRect.anchoredPosition = new Vector2(0f, 20f);
            ltRect.sizeDelta = new Vector2(500f, 32f);

            Text ltTxt = langTitleObj.AddComponent<Text>();
            ltTxt.font = font;
            ltTxt.text = LocalizationManager.L("Lang_Title", "🌐 <b>OYUN DİLİ / GAME LANGUAGE:</b>", "🌐 <b>GAME LANGUAGE / OYUN DİLİ:</b>");
            ltTxt.fontSize = 16;
            ltTxt.alignment = TextAnchor.MiddleCenter;
            ltTxt.color = new Color(0.35f, 0.90f, 0.55f);

            bool isTR = LocalizationManager.Instance == null || LocalizationManager.Instance.IsTurkish;

            // 1. TÜRKÇE BUTONU
            GameObject trBtnObj = new GameObject("TRLangBtn");
            trBtnObj.transform.SetParent(langBox.transform, false);
            RectTransform trbRect = trBtnObj.AddComponent<RectTransform>();
            trbRect.anchoredPosition = new Vector2(-120f, -20f);
            trbRect.sizeDelta = new Vector2(210f, 44f);

            Image trbBg = trBtnObj.AddComponent<Image>();
            Color trColor = isTR ? new Color(0.20f, 0.75f, 0.35f) : new Color(0.22f, 0.28f, 0.36f);
            trbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(210, 44, 10, trColor);

            Button trBtn = trBtnObj.AddComponent<Button>();
            trBtn.targetGraphic = trbBg;
            trBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                LocalizationManager.Instance.SetLanguage(GameLanguage.Turkish);
                BuildUI();
            });

            GameObject trTxtObj = new GameObject("Label");
            trTxtObj.transform.SetParent(trBtnObj.transform, false);
            RectTransform trtRect = trTxtObj.AddComponent<RectTransform>();
            trtRect.anchorMin = Vector2.zero;
            trtRect.anchorMax = Vector2.one;

            Text trTxt = trTxtObj.AddComponent<Text>();
            trTxt.font = font;
            trTxt.text = "🇹🇷 Türkçe";
            trTxt.fontSize = 16;
            trTxt.fontStyle = FontStyle.Bold;
            trTxt.alignment = TextAnchor.MiddleCenter;
            trTxt.color = Color.white;
            trTxt.raycastTarget = false;

            // 2. İNGİLİZCE BUTONU
            GameObject enBtnObj = new GameObject("ENLangBtn");
            enBtnObj.transform.SetParent(langBox.transform, false);
            RectTransform enbRect = enBtnObj.AddComponent<RectTransform>();
            enbRect.anchoredPosition = new Vector2(120f, -20f);
            enbRect.sizeDelta = new Vector2(210f, 44f);

            Image enbBg = enBtnObj.AddComponent<Image>();
            Color enColor = !isTR ? new Color(0.20f, 0.75f, 0.35f) : new Color(0.22f, 0.28f, 0.36f);
            enbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(210, 44, 10, enColor);

            Button enBtn = enBtnObj.AddComponent<Button>();
            enBtn.targetGraphic = enbBg;
            enBtn.onClick.AddListener(() => {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
                LocalizationManager.Instance.SetLanguage(GameLanguage.English);
                BuildUI();
            });

            GameObject enTxtObj = new GameObject("Label");
            enTxtObj.transform.SetParent(enBtnObj.transform, false);
            RectTransform entRect = enTxtObj.AddComponent<RectTransform>();
            entRect.anchorMin = Vector2.zero;
            entRect.anchorMax = Vector2.one;

            Text enTxt = enTxtObj.AddComponent<Text>();
            enTxt.font = font;
            enTxt.text = "🇬🇧 English";
            enTxt.fontSize = 16;
            enTxt.fontStyle = FontStyle.Bold;
            enTxt.alignment = TextAnchor.MiddleCenter;
            enTxt.color = Color.white;
            enTxt.raycastTarget = false;

            // ==================== BÖLÜM 4: BİLGİ SEKMESİ ====================
            GameObject infoObj = new GameObject("InfoBox");
            infoObj.transform.SetParent(panelObj.transform, false);
            RectTransform iRect = infoObj.AddComponent<RectTransform>();
            iRect.anchoredPosition = new Vector2(0f, -210f);
            iRect.sizeDelta = new Vector2(640f, 110f);

            Text infoTxt = infoObj.AddComponent<Text>();
            infoTxt.font = font;
            infoTxt.text = LocalizationManager.L(
                "Info_Text",
                "📱 <b>Kontrol & Grafikler:</b> Karma (PC Klavye/Fare + Dokunmatik)\n" +
                "🖥️ <b>Grafik Kalitesi:</b> Yüksek (Low-Poly Ultra)\n\n" +
                "<color=#80D8FF>💡 Tüm ses seviyeleri, müzik ve dil tercihleri otomatik kaydedilir!</color>",
                "📱 <b>Controls & Graphics:</b> Hybrid (PC Mouse/Keyboard + Mobile Touch)\n" +
                "🖥️ <b>Graphic Quality:</b> High (Low-Poly Ultra)\n\n" +
                "<color=#80D8FF>💡 All audio, music, and language settings are automatically saved!</color>"
            );
            infoTxt.fontSize = 14;
            infoTxt.alignment = TextAnchor.MiddleCenter;
            infoTxt.color = Color.white;
        }

        private GameObject CreateVolButton(Transform parent, Vector2 pos, string label, System.Action onClick)
        {
            GameObject btnObj = new GameObject("VolBtn_" + label);
            btnObj.transform.SetParent(parent, false);

            RectTransform r = btnObj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(40f, 38f);

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(40, 38, 8, new Color(0.25f, 0.35f, 0.45f));

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
                bgmMuteImg.color = isMuted ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.20f, 0.75f, 0.35f);
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
                sfxMuteImg.color = isMuted ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.20f, 0.75f, 0.35f);
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
}
