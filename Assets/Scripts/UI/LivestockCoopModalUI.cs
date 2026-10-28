using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    public class LivestockCoopModalUI : MonoBehaviour
    {
        public static LivestockCoopModalUI Instance { get; private set; }

        private GameObject canvasObj;
        private RectTransform panelRect;
        private Canvas modalCanvas;
        public bool IsModalOpen => canvasObj != null && canvasObj.activeInHierarchy;
        private LivestockBuildingKind currentKind;
        private int selectedCrates = 1;

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

        public void ShowModal(LivestockBuildingKind kind)
        {
            currentKind = kind;
            selectedCrates = 1;
            ModalManager.SetModalOpen(true);
            BuildUI();
            TutorialManager.Instance?.NotifyBuildingVisited(kind == LivestockBuildingKind.ChickenCoop ? "Chicken_Coop" : "Cow_Barn");
        }

        public void HideModal()
        {
            if (canvasObj != null)
            {
                Destroy(canvasObj);
                canvasObj = null;
            }
            ModalManager.SetModalOpen(false);
        }

        private void Update()
        {
            if (canvasObj != null && canvasObj.activeInHierarchy)
            {
                FitPanelToSafeArea();
                try
                {
                    if (Input.GetKeyDown(KeyCode.Escape)) HideModal();
                }
                catch { }
            }
        }

        private bool IsChicken => currentKind == LivestockBuildingKind.ChickenCoop;

        private string ProductId => IsChicken ? LivestockProductDatabase.EggId : LivestockProductDatabase.MilkId;

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);

            canvasObj = new GameObject("Global_Livestock_Building_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            modalCanvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 955;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            Font font = UIStyleUtility.GetGlobalFont(16);
            LivestockManager lm = LivestockManager.Instance;

            GameObject backdrop = CreateFill(canvasObj.transform, "Backdrop", new Color(0.04f, 0.06f, 0.10f, 0.85f), true);
            backdrop.AddComponent<Button>().onClick.AddListener(HideModal);

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(backdrop.transform, false);
            RectTransform pRect = panel.AddComponent<RectTransform>();
            panelRect = pRect;
            pRect.sizeDelta = new Vector2(860f, 720f);
            Image pBg = panel.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(860, 720, 18, 3, new Color(0.95f, 0.72f, 0.20f), new Color(0.10f, 0.14f, 0.18f, 0.98f));
            pBg.raycastTarget = true;

            UIStyleUtility.CreateCornerCloseButton(panel.transform, HideModal, 64f);

            string title = IsChicken
                ? LocalizationManager.L("Coop_Title", "🐔 TAVUK ÇİFTLİĞİ", "🐔 CHICKEN COOP")
                : LocalizationManager.L("CowBarn_Title", "🐄 İNEK AHIRI", "🐄 COW BARN");
            CreateLabel(panel.transform, font, new Vector2(0f, 300f), new Vector2(700f, 48f), title, 32, Color.white, TextAnchor.MiddleCenter);

            int white = lm != null ? lm.GetOwned(LivestockType.WhiteChicken) : 0;
            int black = lm != null ? lm.GetOwned(LivestockType.BlackChicken) : 0;
            int holstein = lm != null ? lm.GetOwned(LivestockType.HolsteinCow) : 0;
            int brown = lm != null ? lm.GetOwned(LivestockType.BrownCow) : 0;

            string herdText = IsChicken
                ? string.Format(LocalizationManager.L("Coop_HerdFmt", "Beyaz tavuk: {0}   •   Siyah tavuk: {1}   •   Toplam: {2}/{3}", "White chickens: {0}   •   Black chickens: {1}   •   Total: {2}/{3}"), white, black, white + black, LivestockProductDatabase.MaxChickens)
                : string.Format(LocalizationManager.L("CowBarn_HerdFmt", "Siyah-beyaz inek: {0}   •   Kahverengi inek: {1}   •   Toplam: {2}/{3}", "Holstein cows: {0}   •   Brown cows: {1}   •   Total: {2}/{3}"), holstein, brown, holstein + brown, LivestockProductDatabase.MaxCows);
            CreateLabel(panel.transform, font, new Vector2(0f, 235f), new Vector2(760f, 64f), herdText, 24, new Color(0.85f, 0.90f, 0.75f), TextAnchor.MiddleCenter);

            WholesaleProductDef product = LivestockProductDatabase.GetById(ProductId);
            int pack = LivestockProductDatabase.PackSize;
            int crates = lm != null ? lm.GetReadyCrates(IsChicken) : 0;
            int toward = lm != null ? lm.GetUnitsTowardNextCrate(IsChicken) : 0;
            int animals = lm != null ? (IsChicken ? lm.TotalChickens : lm.TotalCows) : 0;
            float hoursLeft = lm != null ? lm.GetHoursUntilNextCrate(IsChicken) : -1f;

            string crateFmt = IsChicken
                ? LocalizationManager.L("Coop_EggsCrateFmt", "{0}  Hazır koli: {1}   •   {2}/{3} yumurta", "{0}  Ready crates: {1}   •   {2}/{3} eggs")
                : LocalizationManager.L("CowBarn_MilkCrateFmt", "{0}  Hazır koli: {1}   •   {2}/{3} litre", "{0}  Ready crates: {1}   •   {2}/{3} litres");
            CreateLabel(panel.transform, font, new Vector2(0f, 167f), new Vector2(760f, 44f), string.Format(crateFmt, product != null ? product.iconEmoji : "", crates, toward, pack), 27, new Color(1f, 0.92f, 0.55f), TextAnchor.MiddleCenter);

            string rateFmt = LocalizationManager.L(
                "Coop_ProdRateFmt",
                "1 koli = {0} adet  •  Gündüz 07–18  •  Hayvan başına ~2,5 günde 1 koli\n{1} hayvan → sonraki koli: {2}",
                "1 crate = {0} pcs  •  Daytime 07–18  •  ~1 crate per animal every 2.5 days\n{1} animals → next crate: {2}");
            CreateLabel(panel.transform, font, new Vector2(0f, 93f), new Vector2(760f, 88f), string.Format(rateFmt, pack, animals, FormatHoursUntilCrate(hoursLeft, animals)), 22, new Color(0.78f, 0.86f, 0.92f), TextAnchor.MiddleCenter);

            selectedCrates = Mathf.Clamp(selectedCrates, crates > 0 ? 1 : 0, Mathf.Max(0, crates));
            Text amountTxt = CreateLabel(panel.transform, font, new Vector2(0f, 0f), new Vector2(260f, 64f), FormatCrateAmount(selectedCrates), 30, Color.white, TextAnchor.MiddleCenter);
            amountTxt.name = "CrateAmountLabel";

            CreateBtn(panel.transform, font, new Vector2(-255f, 0f), new Vector2(190f, 68f), LocalizationManager.L("Livestock_LessCrate", "−1 koli", "−1 crate"), new Color(0.80f, 0.28f, 0.28f), () => { selectedCrates = Mathf.Max(0, selectedCrates - 1); RefreshAmount(amountTxt); });
            CreateBtn(panel.transform, font, new Vector2(255f, 0f), new Vector2(190f, 68f), LocalizationManager.L("Livestock_MoreCrate", "+1 koli", "+1 crate"), new Color(0.22f, 0.70f, 0.32f), () => { selectedCrates = Mathf.Min(Mathf.Max(0, crates), selectedCrates + 1); RefreshAmount(amountTxt); });
            CreateBtn(panel.transform, font, new Vector2(0f, -73f), new Vector2(280f, 58f), LocalizationManager.L("Dist_All", "TÜMÜ", "ALL"), new Color(0.25f, 0.45f, 0.70f), () => { selectedCrates = Mathf.Max(0, crates); RefreshAmount(amountTxt); });

            string barnLabel = FormatBarnLabel(selectedCrates);
            Button barnBtn = CreateBtn(panel.transform, font, new Vector2(0f, -162f), new Vector2(740f, 100f), barnLabel, new Color(0.18f, 0.55f, 0.85f), OnSendToBarn);
            barnBtn.name = "SendBarnBtn";

            CreateBtn(panel.transform, font, new Vector2(0f, -285f), new Vector2(280f, 64f), LocalizationManager.L("Btn_Close", "Kapat", "Close"), new Color(0.45f, 0.22f, 0.22f), HideModal);

            RefreshAmount(amountTxt);
            Canvas.ForceUpdateCanvases();
            FitPanelToSafeArea();
        }

        private void FitPanelToSafeArea()
        {
            if (panelRect == null || modalCanvas == null) return;
            Rect safe = Screen.safeArea;
            float scale = Mathf.Max(.001f, modalCanvas.scaleFactor);
            float fit = Mathf.Min(1f, Mathf.Min(
                Mathf.Max(1f, safe.width / scale - 32f) / 860f,
                Mathf.Max(1f, safe.height / scale - 32f) / 720f));
            panelRect.localScale = Vector3.one * fit;
            panelRect.anchoredPosition = (safe.center - new Vector2(Screen.width, Screen.height) * .5f) / scale;
        }

        private static string FormatCrateAmount(int crates)
        {
            return string.Format(LocalizationManager.L("Coop_CrateAmt", "{0} koli", "{0} crates"), crates);
        }

        private static string FormatHoursUntilCrate(float hours, int animals)
        {
            if (animals <= 0) return LocalizationManager.L("Coop_NoAnimals", "hayvan yok", "no animals");
            if (hours <= 0.05f) return LocalizationManager.L("Coop_CrateSoon", "hemen doluyor", "almost ready");
            int rounded = Mathf.Max(1, Mathf.CeilToInt(hours));
            return string.Format(LocalizationManager.L("Coop_HoursFmt", "~{0} oyun saati", "~{0} game hours"), rounded);
        }

        private void RefreshAmount(Text amountTxt)
        {
            int crates = LivestockManager.Instance != null ? LivestockManager.Instance.GetReadyCrates(IsChicken) : 0;
            selectedCrates = Mathf.Clamp(selectedCrates, crates > 0 ? 1 : 0, crates);
            if (amountTxt != null) amountTxt.text = FormatCrateAmount(selectedCrates);

            if (canvasObj == null) return;
            Transform barn = canvasObj.transform.Find("Backdrop/Panel/SendBarnBtn/Label");
            if (barn != null)
            {
                Text t = barn.GetComponent<Text>();
                if (t != null) t.text = FormatBarnLabel(selectedCrates);
            }
            Transform barnBtnT = canvasObj.transform.Find("Backdrop/Panel/SendBarnBtn");
            bool ready = crates > 0 && selectedCrates > 0;
            if (barnBtnT != null)
            {
                Button b = barnBtnT.GetComponent<Button>();
                if (b != null) b.interactable = ready;
            }
        }

        private static string FormatBarnLabel(int crates)
        {
            return string.Format(LocalizationManager.L("Livestock_SendBarnOnly", "AHIRA GÖNDER\n{0} koli ({1} adet)", "SEND TO BARN\n{0} crates ({1} units)"), crates, crates * LivestockProductDatabase.PackSize);
        }

        private void OnSendToBarn()
        {
            int crates = selectedCrates;
            int amount = crates * LivestockProductDatabase.PackSize;
            if (crates <= 0 || amount <= 0 || LivestockManager.Instance == null)
            {
                ModalManager.ShowModal(
                    LocalizationManager.L("Coop_NeedCrateTitle", "Koli henüz dolmadı", "Crate not ready"),
                    string.Format(LocalizationManager.L("Livestock_NeedBarnCrate", "Ahıra göndermek için 1 koli ({0} adet) dolmalı.\nHayvan sayısı arttıkça koli daha hızlı dolar.", "You need 1 full crate ({0} units) to send to the barn.\nMore animals fill crates faster."), LivestockProductDatabase.PackSize),
                    LocalizationManager.L("Btn_Ok", "Tamam", "OK"));
                return;
            }

            GardenSeedInventoryManager barn = GardenSeedInventoryManager.Instance;
            if (barn == null || !barn.CanAddToBarn(amount))
            {
                GardenSeedInventoryManager.ShowBarnFullModal();
                return;
            }

            if (!LivestockManager.Instance.ConsumeCrates(IsChicken, crates)) return;

            if (!barn.TryAddCropToBarn(ProductId, amount, ProductPassportService.CreateLivestockLot(ProductId, amount, IsChicken)))
            {
                if (IsChicken) LivestockManager.Instance.AddEggs(amount);
                else LivestockManager.Instance.AddMilk(amount);
                GardenSeedInventoryManager.ShowBarnFullModal();
                return;
            }

            WholesaleProductDef def = LivestockProductDatabase.GetById(ProductId);
            HideModal();
            ModalManager.ShowModal(
                LocalizationManager.L("Coop_SentBarnTitle", "Ahıra Gönderildi", "Sent to Barn"),
                string.Format(LocalizationManager.L("Coop_SentBarnBodyCrate", "<b>{0} koli ({1} adet) {2}</b> ahır envanterine eklendi.\nAhırdan yeşil kamyonla dükkana %40 kârla gönderebilirsiniz.", "<b>{0} crates ({1} pcs) {2}</b> added to the barn.\nShip them to the store with the green truck (+40%)."), crates, amount, def != null ? def.LocalizedName : ProductId),
                LocalizationManager.L("Btn_Great", "Harika!", "Great!"));
        }

        private static GameObject CreateFill(Transform parent, string name, Color color, bool raycast)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return go;
        }

        private static Text CreateLabel(Transform parent, Font font, Vector2 pos, Vector2 size, string text, int fontSize, Color color, TextAnchor align)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(18, fontSize - 4);
            t.resizeTextMaxSize = fontSize;
            return t;
        }

        private static Button CreateBtn(Transform parent, Font font, Vector2 pos, Vector2 size, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.sprite = UIStyleUtility.CreateRoundedPillSprite(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), 12, color);
            img.raycastTarget = true;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(action);

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(go.transform, false);
            RectTransform tr = txtObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(16f, 8f);
            tr.offsetMax = new Vector2(-16f, -8f);
            Text t = txtObj.AddComponent<Text>();
            t.font = font;
            t.text = label;
            t.fontSize = 26;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 22;
            t.resizeTextMaxSize = 26;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            return btn;
        }
    }
}
