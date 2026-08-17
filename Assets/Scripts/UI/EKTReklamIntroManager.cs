using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// EKT REKLAM Barkodlu Şirket Açılış İntrosu (EKT GAMES Splash Sequence).
    /// Oyun ilk başlatıldığında ekrana gelen, kırmızı lazer taramalı barkod,
    /// bip sesi ve neon parlamalı "EKT GAMES PRESENTS" açılış jeneriğidir.
    /// Tamamlandığında veya dokunulduğunda doğrudan Ana Menüye geçiş yapar.
    /// </summary>
    public class EKTReklamIntroManager : MonoBehaviour
    {
        public static EKTReklamIntroManager Instance { get; private set; }
        public static bool HasIntroFinished { get; private set; } = false;

        private bool introPlayed = false;
        private GameObject blackCurtainObj;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (!HasIntroFinished)
                {
                    CreateInstantBlackCurtain();
                }
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void CreateInstantBlackCurtain()
        {
            if (blackCurtainObj != null) return;
            blackCurtainObj = new GameObject("[EKT_Intro_BlackCurtain]");
            DontDestroyOnLoad(blackCurtainObj);
            Canvas c = blackCurtainObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 1500; // 3D Sahnenin üzerinde, İntro UI'ının altında yer alır

            CanvasScaler scaler = blackCurtainObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Image img = blackCurtainObj.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = true;
        }

        private void Start()
        {
            if (!introPlayed && !HasIntroFinished)
            {
                StartCoroutine(PlayEktIntroSequenceRoutine());
            }
        }

        public void PlayIntroManually()
        {
            HasIntroFinished = false;
            CreateInstantBlackCurtain();
            StartCoroutine(PlayEktIntroSequenceRoutine());
        }

        private IEnumerator PlayEktIntroSequenceRoutine()
        {
            introPlayed = true;
            Time.timeScale = 0f; // Intro boyunca zamanı duraklat

            // Ana Menüyü intro esnasında gizli tut
            if (MainMenuUI.Instance != null)
            {
                MainMenuUI.Instance.HideMenu();
            }

            GameObject canvasObj = new GameObject("EKT_Reklam_Intro_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 3000; // İntro Katmanı (En Üstte Görünür)

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Geçici siyah perdeyi kaldır, çünkü artık intro UI'ı siyah paneliyle ekranda!
            if (blackCurtainObj != null)
            {
                Destroy(blackCurtainObj);
            }

            // Siyah Arka Plan Paneli
            GameObject introPanel = new GameObject("IntroPanel");
            introPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtIntro = introPanel.AddComponent<RectTransform>();
            rtIntro.anchorMin = Vector2.zero;
            rtIntro.anchorMax = Vector2.one;
            rtIntro.sizeDelta = Vector2.zero;

            Image bg = introPanel.AddComponent<Image>();
            bg.color = Color.black;

            CanvasGroup cg = introPanel.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            Font font = GetSafeFont();

            // Barkod Konteyner Alanı
            GameObject barcodeContainer = new GameObject("BarcodeContainer");
            barcodeContainer.transform.SetParent(introPanel.transform, false);
            RectTransform rtBarContainer = barcodeContainer.AddComponent<RectTransform>();
            rtBarContainer.anchorMin = new Vector2(0.5f, 0.5f);
            rtBarContainer.anchorMax = new Vector2(0.5f, 0.5f);
            rtBarContainer.pivot = new Vector2(0.5f, 0.5f);
            rtBarContainer.anchoredPosition = new Vector2(0f, 60f);
            rtBarContainer.sizeDelta = new Vector2(240f, 60f);

            CanvasGroup barcodeCg = barcodeContainer.AddComponent<CanvasGroup>();

            // Barkod Çizgileri
            int[] barPattern = { 2, 4, 1, 1, 3, 2, 5, 1, 2, 4, 1, 3, 2, 1, 5, 2, 3, 1, 4, 2, 1, 3, 2 };
            float totalPatternWidth = 0f;
            foreach (int w in barPattern) totalPatternWidth += w * 3f + 2f;

            float curX = -totalPatternWidth / 2f;
            for (int i = 0; i < barPattern.Length; i++)
            {
                float w = barPattern[i] * 3f;
                if (i % 2 == 0)
                {
                    GameObject barGo = new GameObject($"Bar_{i}");
                    barGo.transform.SetParent(barcodeContainer.transform, false);
                    RectTransform rtBar = barGo.AddComponent<RectTransform>();
                    rtBar.anchorMin = new Vector2(0.5f, 0.5f);
                    rtBar.anchorMax = new Vector2(0.5f, 0.5f);
                    rtBar.pivot = new Vector2(0f, 0.5f);
                    rtBar.anchoredPosition = new Vector2(curX, 0f);
                    rtBar.sizeDelta = new Vector2(w, 60f);

                    Image barImg = barGo.AddComponent<Image>();
                    barImg.color = new Color(0.85f, 0.85f, 0.9f, 1f);
                }
                curX += w + 2f;
            }

            // Barkod Alt Yazısı: EKT-7928-GAMES
            GameObject barcodeLabelGo = new GameObject("BarcodeLabel");
            barcodeLabelGo.transform.SetParent(barcodeContainer.transform, false);
            RectTransform rtBarLabel = barcodeLabelGo.AddComponent<RectTransform>();
            rtBarLabel.anchorMin = new Vector2(0.5f, 0f);
            rtBarLabel.anchorMax = new Vector2(0.5f, 0f);
            rtBarLabel.pivot = new Vector2(0.5f, 1f);
            rtBarLabel.anchoredPosition = new Vector2(0f, -5f);
            rtBarLabel.sizeDelta = new Vector2(240f, 20f);

            Text barcodeLabelTxt = barcodeLabelGo.AddComponent<Text>();
            barcodeLabelTxt.text = "EKT-7928-GAMES";
            if (font != null) barcodeLabelTxt.font = font;
            barcodeLabelTxt.fontSize = 12;
            barcodeLabelTxt.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            barcodeLabelTxt.alignment = TextAnchor.MiddleCenter;

            // Kırmızı Lazer Tarayıcı Çizgisi
            GameObject laserGlowGo = new GameObject("LaserGlow");
            laserGlowGo.transform.SetParent(barcodeContainer.transform, false);
            RectTransform rtLaserGlow = laserGlowGo.AddComponent<RectTransform>();
            rtLaserGlow.anchorMin = new Vector2(0.5f, 1f);
            rtLaserGlow.anchorMax = new Vector2(0.5f, 1f);
            rtLaserGlow.pivot = new Vector2(0.5f, 0.5f);
            rtLaserGlow.anchoredPosition = new Vector2(0f, 0f);
            rtLaserGlow.sizeDelta = new Vector2(280f, 7f);
            Image laserGlowImg = laserGlowGo.AddComponent<Image>();
            laserGlowImg.color = new Color(1f, 0f, 0.1f, 0.35f);

            GameObject laserCoreGo = new GameObject("LaserCore");
            laserCoreGo.transform.SetParent(laserGlowGo.transform, false);
            RectTransform rtLaserCore = laserCoreGo.AddComponent<RectTransform>();
            rtLaserCore.anchorMin = Vector2.zero;
            rtLaserCore.anchorMax = Vector2.one;
            rtLaserCore.sizeDelta = Vector2.zero;
            Image laserCoreImg = laserCoreGo.AddComponent<Image>();
            laserCoreImg.color = new Color(1f, 0.3f, 0.3f, 1f);

            // Neon Başlık "EKT GAMES"
            GameObject neonGlowGo = new GameObject("NeonGlow");
            neonGlowGo.transform.SetParent(introPanel.transform, false);
            RectTransform rtNeonGlow = neonGlowGo.AddComponent<RectTransform>();
            rtNeonGlow.anchorMin = new Vector2(0.5f, 0.5f);
            rtNeonGlow.anchorMax = new Vector2(0.5f, 0.5f);
            rtNeonGlow.pivot = new Vector2(0.5f, 0.5f);
            rtNeonGlow.anchoredPosition = new Vector2(0f, -40f);
            rtNeonGlow.sizeDelta = new Vector2(600f, 80f);

            Text neonGlowTxt = neonGlowGo.AddComponent<Text>();
            neonGlowTxt.text = "EKT GAMES";
            if (font != null) neonGlowTxt.font = font;
            neonGlowTxt.fontSize = 46;
            neonGlowTxt.fontStyle = FontStyle.Bold;
            neonGlowTxt.alignment = TextAnchor.MiddleCenter;
            neonGlowTxt.color = new Color(0f, 0.7f, 1f, 0f);

            Outline neonOutline = neonGlowGo.AddComponent<Outline>();
            neonOutline.effectColor = new Color(0f, 0.4f, 0.8f, 0.3f);
            neonOutline.effectDistance = new Vector2(3f, 3f);

            GameObject neonCoreGo = new GameObject("NeonCore");
            neonCoreGo.transform.SetParent(neonGlowGo.transform, false);
            RectTransform rtNeonCore = neonCoreGo.AddComponent<RectTransform>();
            rtNeonCore.anchorMin = Vector2.zero;
            rtNeonCore.anchorMax = Vector2.one;
            rtNeonCore.sizeDelta = Vector2.zero;

            Text neonCoreTxt = neonCoreGo.AddComponent<Text>();
            neonCoreTxt.text = "EKT GAMES";
            if (font != null) neonCoreTxt.font = font;
            neonCoreTxt.fontSize = 44;
            neonCoreTxt.fontStyle = FontStyle.Bold;
            neonCoreTxt.alignment = TextAnchor.MiddleCenter;
            neonCoreTxt.color = new Color(1f, 1f, 1f, 0f);

            // Alt Başlık: PRESENTS
            GameObject presentsGo = new GameObject("PresentsSubtitle");
            presentsGo.transform.SetParent(introPanel.transform, false);
            RectTransform rtPresents = presentsGo.AddComponent<RectTransform>();
            rtPresents.anchorMin = new Vector2(0.5f, 0.5f);
            rtPresents.anchorMax = new Vector2(0.5f, 0.5f);
            rtPresents.pivot = new Vector2(0.5f, 0.5f);
            rtPresents.anchoredPosition = new Vector2(0f, -110f);
            rtPresents.sizeDelta = new Vector2(400f, 30f);

            Text presentsTxt = presentsGo.AddComponent<Text>();
            presentsTxt.text = "PRESENTS";
            if (font != null) presentsTxt.font = font;
            presentsTxt.fontSize = 14;
            presentsTxt.color = new Color(0.6f, 0.6f, 0.7f, 0f);
            presentsTxt.alignment = TextAnchor.MiddleCenter;

            try
            {
                // Animasyon Döngüsü (Toplam 4.5 saniye)
                float elapsed = 0f;
                bool hasBeeped = false;

                while (elapsed < 4.5f)
                {
                    elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);

                    // Dokunma / Tıklama ile atlama kontrolü (1.2 saniyeden sonra)
                    bool skipPressed = false;
#if ENABLE_INPUT_SYSTEM
                    if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        skipPressed = true;
                    }
                    if (UnityEngine.InputSystem.Keyboard.current != null &&
                        (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame))
                    {
                        skipPressed = true;
                    }
                    if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                    {
                        skipPressed = true;
                    }
#else
                    try
                    {
                        skipPressed = Input.GetMouseButtonDown(0) || Input.touchCount > 0;
                    }
                    catch { }
#endif
                    if (skipPressed && elapsed > 1.2f)
                    {
                        break;
                    }

                    // Aşama 1: Barkod Tarama (0.0s - 2.0s)
                    if (elapsed < 2.0f)
                    {
                        float scanAlpha = Mathf.Clamp01((2.0f - elapsed) / 0.3f);
                        barcodeCg.alpha = scanAlpha;

                        float laserProgress = Mathf.Clamp01(elapsed / 1.5f);
                        rtLaserGlow.anchoredPosition = new Vector2(0f, -laserProgress * 60f);

                        if (elapsed >= 1.2f && !hasBeeped)
                        {
                            hasBeeped = true;
                            if (AudioManager.Instance != null)
                            {
                                AudioManager.Instance.PlayTabletTap();
                            }
                        }
                    }
                    else
                    {
                        barcodeContainer.SetActive(false);
                    }

                    // Aşama 2: Neon Titreşimli Logo Belirme (1.3s+)
                    if (elapsed >= 1.3f)
                    {
                        float logoAlpha = Mathf.Clamp01((elapsed - 1.3f) / 0.4f);

                        bool isFlickerOn = true;
                        if (elapsed > 1.3f && elapsed < 2.1f)
                        {
                            isFlickerOn = (Random.value > 0.35f);
                        }

                        float pulse = Mathf.PingPong(Time.unscaledTime * 2.0f, 1f);
                        float neonIntensity = 0.6f + pulse * 0.4f;
                        if (elapsed > 1.3f && elapsed < 2.1f && !isFlickerOn)
                        {
                            neonIntensity = 0.05f;
                        }

                        neonGlowTxt.color = new Color(0f, 0.7f, 1f, neonIntensity * 0.8f * logoAlpha);
                        neonCoreTxt.color = new Color(1f, 1f, 1f, logoAlpha * (isFlickerOn ? 1f : 0.1f));

                        float presentsAlpha = Mathf.Clamp01((elapsed - 1.4f) / 0.6f);
                        presentsTxt.color = new Color(0.6f, 0.6f, 0.7f, presentsAlpha);
                    }

                    // Aşama 3: Yumuşak Karartma (3.8s+)
                    if (elapsed >= 3.8f)
                    {
                        float fadeOutAlpha = Mathf.Clamp01((4.5f - elapsed) / 0.7f);
                        cg.alpha = fadeOutAlpha;
                    }

                    yield return null;
                }
            }
            finally
            {
                // Temizlik, İntro Bitiş Bayrağı ve Ana Menüye Geçiş
                HasIntroFinished = true;

                try
                {
                    if (MainMenuUI.Instance != null)
                    {
                        MainMenuUI.Instance.ShowMenu();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                try
                {
                    if (blackCurtainObj != null)
                    {
                        Destroy(blackCurtainObj);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                try
                {
                    if (canvasObj != null)
                    {
                        Destroy(canvasObj);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private Font GetSafeFont()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch {}
            if (font != null) return font;

            try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch {}
            if (font != null) return font;

            try { font = Font.CreateDynamicFontFromOSFont("Arial", 16); } catch {}
            if (font != null) return font;

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

            return font;
        }
    }
}
