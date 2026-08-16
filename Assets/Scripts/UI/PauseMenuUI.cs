using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Oyun içi sağ üstteki PAUSE butonuna basıldığında açılan Duraklatma Menüsü.
    /// Türkçe ve İngilizce çift dilli desteklenir.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public static PauseMenuUI Instance { get; private set; }

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

        public void ShowPauseMenu()
        {
            Time.timeScale = 0f; // Oyunu duraklat
            BuildUI();
        }

        public void HideMenu()
        {
            Time.timeScale = 1.0f; // Oyunu devam ettir
            if (canvasObj != null) Destroy(canvasObj);
        }

        public bool IsPauseMenuOpen => canvasObj != null && canvasObj.activeSelf;

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);

            canvasObj = new GameObject("Farm2Shelf_PauseMenu_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Karartma Arka Plan
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.05f, 0.08f, 0.12f, 0.88f);
            bdImg.raycastTarget = true;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Duraklatma Paneli
            GameObject panelObj = new GameObject("Pause_Panel");
            panelObj.transform.SetParent(backdrop.transform, false);

            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(500f, 620f);

            Image pBg = panelObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(500, 620, 20, 3, new Color(0.95f, 0.65f, 0.15f), new Color(0.10f, 0.14f, 0.18f, 0.98f));

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 250f);
            tRect.sizeDelta = new Vector2(440f, 50f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = LocalizationManager.L("Pause_Title", "⏸️ DURAKLATMA MENÜSÜ", "⏸️ PAUSE MENU");
            tText.fontSize = 26;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.95f, 0.65f, 0.15f);

            // ==================== MENÜ BUTONLARI (5 ADET - SIRASIYLA) ====================
            string[] buttonTitles = new string[]
            {
                LocalizationManager.L("Pause_Resume", "▶ OYUNA DEVAM ET", "▶ RESUME GAME"),
                LocalizationManager.L("Pause_Save", "💾 OYUNU KAYDET", "💾 SAVE GAME"),
                LocalizationManager.L("Pause_Load", "📂 KAYITLI OYUN YÜKLE", "📂 LOAD GAME"),
                LocalizationManager.L("Pause_Settings", "⚙️ AYARLAR", "⚙️ SETTINGS"),
                LocalizationManager.L("Pause_MainMenu", "🏠 ANA MENÜYE DÖN / ÇIKIŞ", "🏠 MAIN MENU / EXIT")
            };

            Color[] buttonColors = new Color[]
            {
                new Color(0.20f, 0.75f, 0.35f), // Yeşil
                new Color(0.20f, 0.65f, 0.90f), // Mavi
                new Color(0.95f, 0.65f, 0.15f), // Turuncu
                new Color(0.55f, 0.35f, 0.75f), // Mor
                new Color(0.85f, 0.20f, 0.25f)  // Kırmızı
            };

            float startY = 160f;
            float btnSpacing = 80f;

            for (int i = 0; i < buttonTitles.Length; i++)
            {
                int btnIndex = i;
                GameObject btnObj = new GameObject("PauseBtn_" + i);
                btnObj.transform.SetParent(panelObj.transform, false);

                RectTransform bRect = btnObj.AddComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(0f, startY - i * btnSpacing);
                bRect.sizeDelta = new Vector2(400f, 60f);

                Image bBg = btnObj.AddComponent<Image>();
                bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(400, 60, 14, buttonColors[i]);

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = bBg;
                btn.onClick.AddListener(() => OnPauseButtonClicked(btnIndex));

                GameObject txtObj = new GameObject("Label");
                txtObj.transform.SetParent(btnObj.transform, false);
                RectTransform tRect2 = txtObj.AddComponent<RectTransform>();
                tRect2.anchorMin = Vector2.zero;
                tRect2.anchorMax = Vector2.one;

                Text txt = txtObj.AddComponent<Text>();
                txt.font = font;
                txt.text = buttonTitles[i];
                txt.fontSize = 19;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.raycastTarget = false;
            }
        }

        private void OnPauseButtonClicked(int index)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            switch (index)
            {
                case 0: // 1. OYUNA DEVAM ET
                    HideMenu();
                    break;
                case 1: // 2. OYUNU KAYDET
                    if (SaveLoadSlotModalUI.Instance != null)
                    {
                        SaveLoadSlotModalUI.Instance.ShowSaveModal();
                    }
                    break;
                case 2: // 3. KAYITLI OYUN YÜKLE
                    if (SaveLoadSlotModalUI.Instance != null)
                    {
                        SaveLoadSlotModalUI.Instance.ShowLoadModal();
                    }
                    break;
                case 3: // 4. AYARLAR
                    if (SettingsModalUI.Instance != null)
                    {
                        SettingsModalUI.Instance.ShowModal();
                    }
                    break;
                case 4: // 5. ÇIKIŞ (ANA MENÜ)
                    if (MainMenuUI.Instance != null)
                    {
                        HideMenu();
                        MainMenuUI.Instance.ShowMenu();
                    }
                    break;
            }
        }
    }
}
