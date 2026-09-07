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
            pRect.sizeDelta = new Vector2(720f, 620f);
            Image pBg = panel.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(720, 620, 18, 3, new Color(0.95f, 0.72f, 0.20f), new Color(0.10f, 0.14f, 0.18f, 0.98f));
            pBg.raycastTarget = true;

            string title = IsChicken
                ? LocalizationManager.L("Coop_Title", "🐔 TAVUK ÇİFTLİĞİ", "🐔 CHICKEN COOP")
                : LocalizationManager.L("CowBarn_Title", "🐄 İNEK AHIRI", "🐄 COW BARN");
            CreateLabel(panel.transform, font, new Vector2(0f, 265f), new Vector2(640f, 40f), title, 24, Color.white, TextAnchor.MiddleCenter);

            int white = lm != null ? lm.GetOwned(LivestockType.WhiteChicken) : 0;
            int black = lm != null ? lm.GetOwned(LivestockType.BlackChicken) : 0;
            int holstein = lm != null ? lm.GetOwned(LivestockType.HolsteinCow) : 0;
            int brown = lm != null ? lm.GetOwned(LivestockType.BrownCow) : 0;

            string herdText = IsChicken
                ? string.Format(LocalizationManager.L("Coop_HerdFmt", "Beyaz tavuk: {0}   •   Siyah tavuk: {1}   •   Toplam: {2}/{3}", "White chickens: {0}   •   Black chickens: {1}   •   Total: {2}/{3}"), white, black, white + black, LivestockProductDatabase.MaxChickens)
                : string.Format(LocalizationManager.L("CowBarn_HerdFmt", "Siyah-beyaz inek: {0}   •   Kahverengi inek: {1}   •   Toplam: {2}/{3}", "Holstein cows: {0}   •   Brown cows: {1}   •   Total: {2}/{3}"), holstein, brown, holstein + brown, LivestockProductDatabase.MaxCows);
            CreateLabel(panel.transform, font, new Vector2(0f, 215f), new Vector2(640f, 36f), herdText, 16, new Color(0.85f, 0.90f, 0.75f), TextAnchor.MiddleCenter);

            WholesaleProductDef product = LivestockProductDatabase.GetById(ProductId);
            int pack = LivestockProductDatabase.PackSize;
            int crates = lm != null ? lm.GetReadyCrates(IsChicken) : 0;
            int toward = lm != null ? lm.GetUnitsTowardNextCrate(IsChicken) : 0;
            int animals = lm != null ? (IsChicken ? lm.TotalChickens : lm.TotalCows) : 0;
            float hoursLeft = lm != null ? lm.GetHoursUntilNextCrate(IsChicken) : -1f;

            string crateFmt = IsChicken
                ? LocalizationManager.L("Coop_EggsCrateFmt", "{0}  Hazır koli: {1}   •   {2}/{3} yumurta", "{0}  Ready crates: {1}   •   {2}/{3} eggs")
                : LocalizationManager.L("CowBarn_MilkCrateFmt", "{0}  Hazır koli: {1}   •   {2}/{3} litre", "{0}  Ready crates: {1}   •   {2}/{3} litres");
            CreateLabel(panel.transform, font, new Vector2(0f, 168f), new Vector2(640f, 32f), string.Format(crateFmt, product != null ? product.iconEmoji : "", crates, toward, pack), 17, new Color(1f, 0.92f, 0.55f), TextAnchor.MiddleCenter);

            string rateFmt = LocalizationManager.L(
                "Coop_ProdRateFmt",
                "1 koli = {0} adet  •  Gündüz 07–18  •  Hayvan başına ~2,5 günde 1 koli\n{1} hayvan → sonraki koli: {2}",
                "1 crate = {0} pcs  •  Daytime 07–18  •  ~1 crate per animal every 2.5 days\n{1} animals → next crate: {2}");
            CreateLabel(panel.transform, font, new Vector2(0f, 118f), new Vector2(660f, 44f), string.Format(rateFmt, pack, animals, FormatHoursUntilCrate(hoursLeft, animals)), 14, new Color(0.78f, 0.86f, 0.92f), TextAnchor.MiddleCenter);

            selectedCrates = Mathf.Clamp(selectedCrates, crates > 0 ? 1 : 0, Mathf.Max(0, crates));
            Text amountTxt = CreateLabel(panel.transform, font, new Vector2(0f, 58f), new Vector2(240f, 44f), FormatCrateAmount(selectedCrates), 22, Color.white, TextAnchor.MiddleCenter);
            amountTxt.name = "CrateAmountLabel";

            CreateBtn(panel.transform, font, new Vector2(-180f, 58f), new Vector2(90f, 44f), "-1 koli", new Color(0.80f, 0.28f, 0.28f), () => { selectedCrates = Mathf.Max(0, selectedCrates - 1); RefreshAmount(amountTxt); });
            CreateBtn(panel.transform, font, new Vector2(180f, 58f), new Vector2(90f, 44f), "+1 koli", new Color(0.22f, 0.70f, 0.32f), () => { selectedCrates = Mathf.Min(Mathf.Max(0, crates), selectedCrates + 1); RefreshAmount(amountTxt); });
            CreateBtn(panel.transform, font, new Vector2(0f, 8f), new Vector2(200f, 36f), LocalizationManager.L("Dist_All", "TÜMÜ", "ALL"), new Color(0.25f, 0.45f, 0.70f), () => { selectedCrates = Mathf.Max(0, crates); RefreshAmount(amountTxt); });

            int qsCrate = LivestockProductDatabase.GetQuickSellCratePrice(ProductId);
            string qsLabel = string.Format(LocalizationManager.L("Coop_BtnQuickSellCrate", "⚡ ANINDA SAT (%20 KÂR)\n{0} koli ({1} adet) → {2:N0}C", "⚡ INSTANT SELL (+20%)\n{0} crates ({1} pcs) → {2:N0}C"), selectedCrates, selectedCrates * pack, qsCrate * selectedCrates);
            string mktLabel = string.Format(LocalizationManager.L("Coop_BtnMarketCrate", "🚛 DÜKKANA GÖNDER (%40 KÂR)\n{0} koli ({1} adet) → ahır + yeşil kamyon", "🚛 SEND TO STORE (+40%)\n{0} crates ({1} pcs) → barn + green truck"), selectedCrates, selectedCrates * pack);

            Button qsBtn = CreateBtn(panel.transform, font, new Vector2(0f, -70f), new Vector2(560f, 70f), qsLabel, new Color(0.20f, 0.68f, 0.38f), OnQuickSell);
            Button barnBtn = CreateBtn(panel.transform, font, new Vector2(0f, -155f), new Vector2(560f, 70f), mktLabel, new Color(0.18f, 0.55f, 0.85f), OnSendToBarn);
            qsBtn.name = "QuickSellBtn";
            barnBtn.name = "SendBarnBtn";

            CreateBtn(panel.transform, font, new Vector2(0f, -230f), new Vector2(200f, 42f), LocalizationManager.L("Btn_Close", "Kapat", "Close"), new Color(0.45f, 0.22f, 0.22f), HideModal);

            RefreshAmount(amountTxt);
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
            int pack = LivestockProductDatabase.PackSize;
            int qsCrate = LivestockProductDatabase.GetQuickSellCratePrice(ProductId);
            Transform qs = canvasObj.transform.Find("Backdrop/Panel/QuickSellBtn/Label");
            Transform barn = canvasObj.transform.Find("Backdrop/Panel/SendBarnBtn/Label");
            if (qs != null)
            {
                Text t = qs.GetComponent<Text>();
                if (t != null) t.text = string.Format(LocalizationManager.L("Coop_BtnQuickSellCrate", "⚡ ANINDA SAT (%20 KÂR)\n{0} koli ({1} adet) → {2:N0}C", "⚡ INSTANT SELL (+20%)\n{0} crates ({1} pcs) → {2:N0}C"), selectedCrates, selectedCrates * pack, qsCrate * Mathf.Max(0, selectedCrates));
            }
            if (barn != null)
            {
                Text t = barn.GetComponent<Text>();
                if (t != null) t.text = string.Format(LocalizationManager.L("Coop_BtnMarketCrate", "🚛 DÜKKANA GÖNDER (%40 KÂR)\n{0} koli ({1} adet) → ahır + yeşil kamyon", "🚛 SEND TO STORE (+40%)\n{0} crates ({1} pcs) → barn + green truck"), selectedCrates, selectedCrates * pack);
            }

            Transform qsBtnT = canvasObj.transform.Find("Backdrop/Panel/QuickSellBtn");
            Transform barnBtnT = canvasObj.transform.Find("Backdrop/Panel/SendBarnBtn");
            bool ready = crates > 0 && selectedCrates > 0;
            if (qsBtnT != null)
            {
                Button b = qsBtnT.GetComponent<Button>();
                if (b != null) b.interactable = ready;
            }
            if (barnBtnT != null)
            {
                Button b = barnBtnT.GetComponent<Button>();
                if (b != null) b.interactable = ready;
            }
        }

        private void OnQuickSell()
        {
            int crates = selectedCrates;
            int amount = crates * LivestockProductDatabase.PackSize;
            if (crates <= 0 || amount <= 0 || LivestockManager.Instance == null)
            {
                ModalManager.ShowModal(
                    LocalizationManager.L("Coop_NeedCrateTitle", "Koli henüz dolmadı", "Crate not ready"),
                    string.Format(LocalizationManager.L("Coop_NeedCrateBody", "Dükkana göndermek veya satmak için 1 koli ({0} adet) dolmalı.\nHayvan sayısı arttıkça koli daha hızlı dolar.", "You need 1 full crate ({0} pcs) before selling or shipping.\nMore animals fill crates faster."), LivestockProductDatabase.PackSize),
                    LocalizationManager.L("Btn_Ok", "Tamam", "OK"));
                return;
            }

            if (!LivestockManager.Instance.ConsumeCrates(IsChicken, crates)) return;

            int unit = LivestockProductDatabase.GetQuickSellUnitPrice(ProductId);
            int total = unit * amount;
            EconomyManager.Instance?.AddCredits(total);
            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordIncome(
                    LocalizationManager.L("TrxCat_Farm", "Tohum/Çiftlik", "Seeds/Farm"),
                    LocalizationManager.L("TrxDesc_LivestockQuickSell", "Kümes hızlı satışı", "Coop quick sell"),
                    total);
            }

            WholesaleProductDef def = LivestockProductDatabase.GetById(ProductId);
            HideModal();
            ModalManager.ShowModal(
                LocalizationManager.L("Modal_QuickSell_Title", "⚡ Hızlı Satış Yapıldı! 💰", "⚡ Quick Sell Completed! 💰"),
                string.Format(LocalizationManager.L("Coop_QuickSellBodyCrate", "<b>{0} koli ({1} adet) {2}</b> %20 kârla satıldı.\n<color=#00E676>+{3:N0}C</color>", "<b>{0} crates ({1} pcs) {2}</b> sold at +20% profit.\n<color=#00E676>+{3:N0}C</color>"), crates, amount, def != null ? def.LocalizedName : ProductId, total),
                LocalizationManager.L("Btn_Great", "Harika!", "Great!"));
        }

        private void OnSendToBarn()
        {
            int crates = selectedCrates;
            int amount = crates * LivestockProductDatabase.PackSize;
            if (crates <= 0 || amount <= 0 || LivestockManager.Instance == null)
            {
                ModalManager.ShowModal(
                    LocalizationManager.L("Coop_NeedCrateTitle", "Koli henüz dolmadı", "Crate not ready"),
                    string.Format(LocalizationManager.L("Coop_NeedCrateBody", "Dükkana göndermek veya satmak için 1 koli ({0} adet) dolmalı.\nHayvan sayısı arttıkça koli daha hızlı dolar.", "You need 1 full crate ({0} pcs) before selling or shipping.\nMore animals fill crates faster."), LivestockProductDatabase.PackSize),
                    LocalizationManager.L("Btn_Ok", "Tamam", "OK"));
                return;
            }

            GardenSeedInventoryManager barn = GardenSeedInventoryManager.Instance;
            if (barn == null || barn.GetTotalBarnStoredAmount() + amount > barn.MaxBarnCapacity)
            {
                ModalManager.ShowModal(
                    LocalizationManager.L("Coop_BarnFullTitle", "Ahır Dolu", "Barn Full"),
                    LocalizationManager.L("Coop_BarnFullBody", "Ahır kapasitesi yetersiz. Önce ahırdaki ürünleri sevk edin.", "Barn capacity is full. Ship existing goods first."),
                    LocalizationManager.L("Btn_Ok", "Tamam", "OK"));
                return;
            }

            if (!LivestockManager.Instance.ConsumeCrates(IsChicken, crates)) return;

            if (!barn.TryAddCropToBarn(ProductId, amount))
            {
                if (IsChicken) LivestockManager.Instance.AddEggs(amount);
                else LivestockManager.Instance.AddMilk(amount);
                ModalManager.ShowModal(
                    LocalizationManager.L("Coop_BarnFullTitle", "Ahır Dolu", "Barn Full"),
                    LocalizationManager.L("Coop_BarnFullBody", "Ahır kapasitesi yetersiz. Önce ahırdaki ürünleri sevk edin.", "Barn capacity is full. Ship existing goods first."),
                    LocalizationManager.L("Btn_Ok", "Tamam", "OK"));
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
            t.verticalOverflow = VerticalWrapMode.Overflow;
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
            tr.sizeDelta = Vector2.zero;
            Text t = txtObj.AddComponent<Text>();
            t.font = font;
            t.text = label;
            t.fontSize = 16;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            return btn;
        }
    }
}
