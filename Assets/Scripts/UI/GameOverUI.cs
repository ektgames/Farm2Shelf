using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Bakiye -2000C'ye düşünce tüm tıklamaları kilitleyen iflas / game over ekranı.
    /// Yalnızca Ana Menüye Dön butonu çalışır.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        public const int BankruptcyThreshold = -2000;

        public static GameOverUI Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.canvasObj != null && Instance.canvasObj.activeInHierarchy;

        private GameObject canvasObj;
        private bool isGameOver;

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

        public void ShowIfBankrupt(int credits)
        {
            if (credits > BankruptcyThreshold)
            {
                return;
            }

            Show();
        }

        public void Show()
        {
            if (isGameOver && IsOpen) return;
            isGameOver = true;

            if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen)
            {
                PauseMenuUI.Instance.HideMenu();
            }

            if (EKTPhoneManager.Instance != null && EKTPhoneManager.IsTabletOpen)
            {
                EKTPhoneManager.Instance.ClosePhoneTabletInstant();
            }

            BuildUI();
            Time.timeScale = 0f;
            ModalManager.SetModalOpen(true);
        }

        public void Hide()
        {
            isGameOver = false;
            if (canvasObj != null)
            {
                Destroy(canvasObj);
                canvasObj = null;
            }
        }

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);

            canvasObj = new GameObject("Farm2Shelf_GameOver_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject blocker = new GameObject("FullScreenBlocker");
            blocker.transform.SetParent(canvasObj.transform, false);
            RectTransform blockerRect = blocker.AddComponent<RectTransform>();
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            Image blockerImg = blocker.AddComponent<Image>();
            blockerImg.color = new Color(0.04f, 0.05f, 0.07f, 0.94f);
            blockerImg.raycastTarget = true;

            Button eatClicks = blocker.AddComponent<Button>();
            eatClicks.transition = Selectable.Transition.None;
            eatClicks.targetGraphic = blockerImg;

            GameObject panel = new GameObject("GameOverPanel");
            panel.transform.SetParent(blocker.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 480f);

            Image panelBg = panel.AddComponent<Image>();
            panelBg.sprite = UIStyleUtility.CreateOutlinePillSprite(720, 480, 28, 3, new Color(0.92f, 0.32f, 0.28f, 0.95f), new Color(0.10f, 0.12f, 0.16f, 0.98f));
            panelBg.raycastTarget = true;

            Font font = UIStyleUtility.GetGlobalFont(24);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 170f);
            titleRect.sizeDelta = new Vector2(660f, 56f);
            Text title = titleObj.AddComponent<Text>();
            title.font = font;
            title.fontSize = 30;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1.0f, 0.55f, 0.40f);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.raycastTarget = false;
            title.text = LocalizationManager.L(
                "GameOver_Title",
                "İFLAS! Kasadaki son kuruş da maaş kuyruğuna girdi",
                "BANKRUPT! Payroll ate the last coin in the till");

            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(panel.transform, false);
            RectTransform bodyRect = bodyObj.AddComponent<RectTransform>();
            bodyRect.anchoredPosition = new Vector2(0f, 20f);
            bodyRect.sizeDelta = new Vector2(640f, 240f);
            Text body = bodyObj.AddComponent<Text>();
            body.font = font;
            body.fontSize = 18;
            body.alignment = TextAnchor.UpperCenter;
            body.color = new Color(0.90f, 0.92f, 0.95f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.raycastTarget = false;
            int money = EconomyManager.Instance != null ? EconomyManager.Instance.Credits : BankruptcyThreshold;
            body.text = string.Format(
                LocalizationManager.L(
                    "GameOver_Body",
                    "Personel 'maaşımız var mı?' diye sordu. Sen 'var gibiydi' dedin. Gece yarısı bordro kasayı eksiye çekti ve bakiye {0:N0}C oldu.\n\n-2000C eşiği aşıldı: tedarikçiler kahve molasına çıktı, tabela utancından eğildi, bu tur bitti hemşerim.\n\nAlışverişle eksiye inemezdin; bunu yalnızca gece yarısı zorunlu ödemeler yapar. Yeni bir hayata ana menüden başla — bu sefer bordroyu saymayı unutma.",
                    "Staff asked 'are we getting paid?' You said 'probably.' Midnight payroll shoved the till into the red at {0:N0}C.\n\nYou crossed -2000C: suppliers went for coffee, the sign sagged in shame, this run is over pal.\n\nYou can't shop into debt — only mandatory midnight payouts can. Start a new life from the main menu, and maybe count payroll first."),
                money);

            GameObject btnObj = new GameObject("MainMenuButton");
            btnObj.transform.SetParent(panel.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0f, -175f);
            btnRect.sizeDelta = new Vector2(420f, 58f);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.sprite = UIStyleUtility.CreateRoundedPillSprite(420, 58, 16, new Color(0.82f, 0.22f, 0.28f));
            btnBg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            btn.onClick.AddListener(ReturnToMainMenu);

            GameObject btnTxtObj = new GameObject("Label");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTxtRect = btnTxtObj.AddComponent<RectTransform>();
            btnTxtRect.anchorMin = Vector2.zero;
            btnTxtRect.anchorMax = Vector2.one;
            Text btnTxt = btnTxtObj.AddComponent<Text>();
            btnTxt.font = font;
            btnTxt.fontSize = 20;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;
            btnTxt.raycastTarget = false;
            btnTxt.text = LocalizationManager.L("GameOver_MainMenu", "ANA MENÜYE DÖN", "RETURN TO MAIN MENU");
        }

        private void ReturnToMainMenu()
        {
            Hide();
            ModalManager.SetModalOpen(false);

            if (SaveSystemManager.Instance != null)
            {
                SaveSystemManager.Instance.ResetRuntimeForNewGame();
            }
            else if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SetCredits(50000);
            }

            if (MainMenuUI.Instance != null)
            {
                MainMenuUI.Instance.ShowMenu();
            }
            else
            {
                Time.timeScale = 0f;
            }
        }
    }
}
