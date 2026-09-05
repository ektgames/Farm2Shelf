using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// 3D Ahıra tıklandığında açılan Ahır Envanteri, Arama ve Detaylı Mahsul Dağıtım / Sevk Arayüzü.
    /// </summary>
    public class BarnInventoryModalUI : MonoBehaviour
    {
        public static BarnInventoryModalUI Instance { get; private set; }
        public static bool IsBarnModalOpen => Instance != null && Instance.canvasObj != null && Instance.canvasObj.activeInHierarchy;

        private GameObject canvasObj;
        private Transform listContentTransform;
        private Text capacityText;
        private Transform closeBtnTransform;

        // Arama Değişkenleri
        private string searchQuery = "";
        private InputField searchInputField;

        // Detaylı Dağıtım Modalı
        private GameObject distributionModalObj;

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
                LocalizationManager.Instance.OnLanguageChanged -= HandleLanguageChanged;
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

        private void Update()
        {
            if (IsBarnModalOpen && WasEscapePressed())
            {
                if (distributionModalObj != null)
                {
                    Destroy(distributionModalObj);
                    distributionModalObj = null;
                }
                else
                {
                    HideModal();
                }
            }
        }

        private bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
            return false;
#else
            try
            {
                return Input.GetKeyDown(KeyCode.Escape);
            }
            catch
            {
                return false;
            }
#endif
        }

        private void HandleLanguageChanged(GameLanguage lang)
        {
            if (IsBarnModalOpen)
            {
                BuildUI();
                RefreshList();
            }
        }

        public void ShowModal()
        {
            // EventSystem Güvencesi
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            searchQuery = "";
            ModalManager.SetModalOpen(true);
            BuildUI();
            RefreshList();
        }

        public void HideModal()
        {
            if (distributionModalObj != null)
            {
                Destroy(distributionModalObj);
                distributionModalObj = null;
            }

            if (canvasObj != null)
            {
                Destroy(canvasObj);
                canvasObj = null;
            }

            GameObject existing = GameObject.Find("Global_Barn_Inventory_Canvas");
            if (existing != null)
            {
                Destroy(existing);
            }

            ModalManager.SetModalOpen(false);
        }

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);
            GameObject existing = GameObject.Find("Global_Barn_Inventory_Canvas");
            if (existing != null) Destroy(existing);

            canvasObj = new GameObject("Global_Barn_Inventory_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Arka Plan Karartma (Overlay Backdrop - Dışına Tıklayınca da Kapatır)
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);
            bdImg.raycastTarget = true;

            Button bdBtn = backdrop.AddComponent<Button>();
            bdBtn.targetGraphic = bdImg;
            bdBtn.onClick.AddListener(HideModal);

            // Modal Paneli (840x670)
            GameObject panelObj = new GameObject("Barn_Panel");
            panelObj.transform.SetParent(backdrop.transform, false);

            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(840f, 670f);

            Image pBg = panelObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(840, 670, 18, 3, new Color(0.30f, 0.75f, 0.35f), new Color(0.10f, 0.14f, 0.18f, 0.98f));
            pBg.raycastTarget = true;

            Font font = UIStyleUtility.GetGlobalFont(16);

            // Başlık (Sol Üst)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(-235f, 285f);
            tRect.sizeDelta = new Vector2(280f, 45f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = LocalizationManager.L("Barn_Title", "🌾 AHIR ENVANTERİ", "🌾 BARN INVENTORY");
            tText.fontSize = 22;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleLeft;
            tText.color = new Color(0.35f, 0.85f, 0.40f);
            tText.raycastTarget = false;

            // Arama Kutusu (Search Input Field - Sağ Üst)
            GameObject searchObj = new GameObject("Search_InputField");
            searchObj.transform.SetParent(panelObj.transform, false);
            RectTransform sRect = searchObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(95f, 285f);
            sRect.sizeDelta = new Vector2(250f, 38f);

            Image searchBg = searchObj.AddComponent<Image>();
            searchBg.sprite = UIStyleUtility.CreateOutlinePillSprite(250, 38, 19, 1, new Color(0.25f, 0.40f, 0.55f), new Color(0.14f, 0.18f, 0.24f, 0.90f));
            searchBg.raycastTarget = true;

            searchInputField = searchObj.AddComponent<InputField>();

            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(searchObj.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(14f, 0f);
            phRect.offsetMax = new Vector2(-14f, 0f);

            Text phText = placeholderObj.AddComponent<Text>();
            phText.font = font;
            phText.text = LocalizationManager.L("Barn_SearchPlaceholder", "🔍 Mahsul ara...", "🔍 Search crops...");
            phText.fontSize = 13;
            phText.fontStyle = FontStyle.Italic;
            phText.color = new Color(0.55f, 0.65f, 0.75f, 0.70f);
            phText.alignment = TextAnchor.MiddleLeft;
            phText.raycastTarget = false;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(searchObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 0f);
            textRect.offsetMax = new Vector2(-14f, 0f);

            Text inputText = textObj.AddComponent<Text>();
            inputText.font = font;
            inputText.fontSize = 14;
            inputText.fontStyle = FontStyle.Bold;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.raycastTarget = false;

            searchInputField.textComponent = inputText;
            searchInputField.placeholder = phText;
            searchInputField.text = searchQuery;
            searchInputField.onValueChanged.AddListener((val) => {
                searchQuery = val.Trim().ToLower();
                RefreshList();
            });

            // Kapat Butonu (✖)
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(panelObj.transform, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchoredPosition = new Vector2(380f, 285f);
            clRect.sizeDelta = new Vector2(44f, 44f);

            Image clBg = closeObj.AddComponent<Image>();
            clBg.sprite = UIStyleUtility.CreateRoundedPillSprite(44, 44, 22, new Color(0.92f, 0.18f, 0.20f, 1f));
            clBg.raycastTarget = true;

            Button clBtn = closeObj.AddComponent<Button>();
            clBtn.targetGraphic = clBg;
            clBtn.onClick.AddListener(HideModal);

            GameObject clTxtObj = new GameObject("X");
            clTxtObj.transform.SetParent(closeObj.transform, false);
            RectTransform cltRect = clTxtObj.AddComponent<RectTransform>();
            cltRect.anchorMin = Vector2.zero;
            cltRect.anchorMax = Vector2.one;

            Text clTxt = clTxtObj.AddComponent<Text>();
            clTxt.font = font;
            clTxt.text = "✖";
            clTxt.fontSize = 24;
            clTxt.fontStyle = FontStyle.Bold;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;
            clTxt.raycastTarget = false;

            Outline clOutline = clTxtObj.AddComponent<Outline>();
            clOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            clOutline.effectDistance = new Vector2(1.5f, -1.5f);

            closeBtnTransform = closeObj.transform;

            // Kapasite Bilgi Şeridi
            GameObject capObj = new GameObject("CapacityBar");
            capObj.transform.SetParent(panelObj.transform, false);
            RectTransform cRect = capObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(0f, 240f);
            cRect.sizeDelta = new Vector2(780f, 32f);

            Image capBg = capObj.AddComponent<Image>();
            capBg.sprite = UIStyleUtility.CreateOutlinePillSprite(780, 32, 10, 1, new Color(0.20f, 0.35f, 0.45f), new Color(0.10f, 0.14f, 0.18f, 0.90f));
            capBg.raycastTarget = false;

            GameObject capTxtObj = new GameObject("CapacityText");
            capTxtObj.transform.SetParent(capObj.transform, false);
            RectTransform ctRect = capTxtObj.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;

            capacityText = capTxtObj.AddComponent<Text>();
            capacityText.font = font;
            capacityText.fontSize = 15;
            capacityText.fontStyle = FontStyle.Bold;
            capacityText.alignment = TextAnchor.MiddleCenter;
            capacityText.color = Color.white;
            capacityText.raycastTarget = false;

            // Scroll Area
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            RectTransform sAreaRect = scrollObj.AddComponent<RectTransform>();
            sAreaRect.anchoredPosition = new Vector2(0f, -8f);
            sAreaRect.sizeDelta = new Vector2(780f, 430f);

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cntRect = content.AddComponent<RectTransform>();
            cntRect.anchorMin = new Vector2(0f, 1f);
            cntRect.anchorMax = new Vector2(1f, 1f);
            cntRect.pivot = new Vector2(0.5f, 1f);
            cntRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cntRect;
            listContentTransform = content.transform;

            // Alt SOL Buton: "MARKETE GÖNDER 🚛 (%40 KÂR)"
            GameObject sendBtnObj = new GameObject("SendToMarketBtn");
            sendBtnObj.transform.SetParent(panelObj.transform, false);
            RectTransform sbRect = sendBtnObj.AddComponent<RectTransform>();
            sbRect.anchoredPosition = new Vector2(-265f, -265f);
            sbRect.sizeDelta = new Vector2(245f, 52f);

            Image sbBg = sendBtnObj.AddComponent<Image>();
            sbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(245, 52, 12, new Color(0.20f, 0.75f, 0.35f));
            sbBg.raycastTarget = true;

            Button sbBtn = sendBtnObj.AddComponent<Button>();
            sbBtn.targetGraphic = sbBg;
            sbBtn.onClick.AddListener(OnSendToMarketClicked);

            GameObject sbTxtObj = new GameObject("Label");
            sbTxtObj.transform.SetParent(sendBtnObj.transform, false);
            RectTransform sbtRect = sbTxtObj.AddComponent<RectTransform>();
            sbtRect.anchorMin = Vector2.zero;
            sbtRect.anchorMax = Vector2.one;

            Text sbTxt = sbTxtObj.AddComponent<Text>();
            sbTxt.font = font;
            sbTxt.text = LocalizationManager.L("Barn_SendMarket", "🚛 TÜMÜNÜ MARKETE\n(%40 KÂR)", "🚛 SHIP ALL TO STORE\n(+40% PROFIT)");
            sbTxt.fontSize = 13;
            sbTxt.fontStyle = FontStyle.Bold;
            sbTxt.alignment = TextAnchor.MiddleCenter;
            sbTxt.color = Color.white;
            sbTxt.raycastTarget = false;

            // Alt ORTA Buton: "🏭 ATÖLYEYE GÖNDER (HAMMADDE)"
            GameObject workshopBtnObj = new GameObject("SendToWorkshopBtn");
            workshopBtnObj.transform.SetParent(panelObj.transform, false);
            RectTransform wbRect = workshopBtnObj.AddComponent<RectTransform>();
            wbRect.anchoredPosition = new Vector2(0f, -265f);
            wbRect.sizeDelta = new Vector2(255f, 52f);

            Image wbBg = workshopBtnObj.AddComponent<Image>();
            wbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(255, 52, 12, new Color(0.95f, 0.55f, 0.15f));
            wbBg.raycastTarget = true;

            Button wbBtn = workshopBtnObj.AddComponent<Button>();
            wbBtn.targetGraphic = wbBg;
            wbBtn.onClick.AddListener(OnSendToWorkshopClicked);

            GameObject wbTxtObj = new GameObject("Label");
            wbTxtObj.transform.SetParent(workshopBtnObj.transform, false);
            RectTransform wbtRect = wbTxtObj.AddComponent<RectTransform>();
            wbtRect.anchorMin = Vector2.zero;
            wbtRect.anchorMax = Vector2.one;

            Text wbTxt = wbTxtObj.AddComponent<Text>();
            wbTxt.font = font;
            wbTxt.text = LocalizationManager.L("Barn_SendWorkshop", "🏭 TÜMÜNÜ ATÖLYEYE\n(HAMMADDE PALETİ)", "🏭 SHIP ALL TO WORKSHOP\n(RAW MATERIAL)");
            wbTxt.fontSize = 13;
            wbTxt.fontStyle = FontStyle.Bold;
            wbTxt.alignment = TextAnchor.MiddleCenter;
            wbTxt.color = Color.white;
            wbTxt.raycastTarget = false;

            // Alt SAĞ Buton: "⚡ HIZLI SAT (%20 KÂR)"
            GameObject quickSellBtnObj = new GameObject("QuickSellBtn");
            quickSellBtnObj.transform.SetParent(panelObj.transform, false);
            RectTransform qsRect = quickSellBtnObj.AddComponent<RectTransform>();
            qsRect.anchoredPosition = new Vector2(265f, -265f);
            qsRect.sizeDelta = new Vector2(245f, 52f);

            Image qsBg = quickSellBtnObj.AddComponent<Image>();
            qsBg.sprite = UIStyleUtility.CreateRoundedPillSprite(245, 52, 12, new Color(0.92f, 0.72f, 0.18f));
            qsBg.raycastTarget = true;

            Button qsBtn = quickSellBtnObj.AddComponent<Button>();
            qsBtn.targetGraphic = qsBg;
            qsBtn.onClick.AddListener(OnQuickSellClicked);

            GameObject qsTxtObj = new GameObject("Label");
            qsTxtObj.transform.SetParent(quickSellBtnObj.transform, false);
            RectTransform qstRect = qsTxtObj.AddComponent<RectTransform>();
            qstRect.anchorMin = Vector2.zero;
            qstRect.anchorMax = Vector2.one;

            Text qsTxt = qsTxtObj.AddComponent<Text>();
            qsTxt.font = font;
            qsTxt.text = LocalizationManager.L("Barn_QuickSell", "⚡ TÜMÜNÜ HIZLI SAT\n(%20 KÂR)", "⚡ INSTANT SELL ALL\n(+20% PROFIT)");
            qsTxt.fontSize = 13;
            qsTxt.fontStyle = FontStyle.Bold;
            qsTxt.alignment = TextAnchor.MiddleCenter;
            qsTxt.color = Color.white;
            qsTxt.raycastTarget = false;

            if (closeBtnTransform != null) closeBtnTransform.SetAsLastSibling();
        }

        private void RefreshList()
        {
            if (listContentTransform == null) return;
            foreach (Transform t in listContentTransform) Destroy(t.gameObject);

            Font font = UIStyleUtility.GetGlobalFont(16);

            // Kapasite Güncelle
            if (capacityText != null && GardenSeedInventoryManager.Instance != null)
            {
                int totalCropKg = GardenSeedInventoryManager.Instance.GetTotalBarnStoredAmount();
                int maxCap = GardenSeedInventoryManager.Instance.MaxBarnCapacity;
                string capFmt = LocalizationManager.L("Barn_CapacityFormat", "📦 Ahır Doluluk Oranı: <color=#{0}><b>{1} / {2} KG</b></color> (Seviye {3})", "📦 Barn Storage Occupancy: <color=#{0}><b>{1} / {2} KG</b></color> (Level {3})");
                string colorHex = (totalCropKg >= maxCap) ? "FF5252" : (totalCropKg >= maxCap * 0.8f ? "FFD700" : "00E676");
                capacityText.text = string.Format(capFmt, colorHex, totalCropKg, maxCap, GardenSeedInventoryManager.Instance.BarnUpgradeLevel);
            }

            // --- BÖLÜM 1: SAHİP OLUNAN TOHUMLAR (0 KG YER KAPLAR) ---
            if (string.IsNullOrEmpty(searchQuery))
            {
                GameObject seedHeaderObj = new GameObject("Header_Seeds");
                seedHeaderObj.transform.SetParent(listContentTransform, false);
                RectTransform shRect = seedHeaderObj.AddComponent<RectTransform>();
                shRect.sizeDelta = new Vector2(760f, 38f);

                Text shTxt = seedHeaderObj.AddComponent<Text>();
                shTxt.font = font;
                shTxt.text = LocalizationManager.L("Barn_HeaderOwnedSeeds", "🌱 SAHİP OLUNAN TOHUMLAR (AHIR KİLERİ — 0 KG YER KAPLAR)", "🌱 OWNED SEEDS (BARN PANTRY — 0 KG SPACE)");
                shTxt.fontSize = 14;
                shTxt.fontStyle = FontStyle.Bold;
                shTxt.alignment = TextAnchor.MiddleLeft;
                shTxt.color = new Color(0.40f, 0.85f, 0.45f);

                Dictionary<string, int> ownedSeeds = GardenSeedInventoryManager.Instance.GetOwnedSeedsInventory();
                if (ownedSeeds == null || ownedSeeds.Count == 0)
                {
                    GameObject emptyObj = new GameObject("EmptySeedMsg");
                    emptyObj.transform.SetParent(listContentTransform, false);
                    RectTransform eRect = emptyObj.AddComponent<RectTransform>();
                    eRect.sizeDelta = new Vector2(760f, 36f);

                    Text eTxt = emptyObj.AddComponent<Text>();
                    eTxt.font = font;
                    eTxt.text = LocalizationManager.L("Barn_EmptySeedsMsg", "Henüz hiç tohumunuz yok. EKT Tablet -> Tohumlar sekmesinden satın alabilirsiniz.", "No seeds in storage. You can purchase from EKT Tablet -> Seeds.");
                    eTxt.fontSize = 13;
                    eTxt.alignment = TextAnchor.MiddleLeft;
                    eTxt.color = new Color(0.65f, 0.75f, 0.85f, 0.80f);
                }
                else
                {
                    foreach (var kvp in ownedSeeds)
                    {
                        string seedId = kvp.Key;
                        int seedCount = kvp.Value;
                        if (seedCount <= 0) continue;
                        GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
                        if (sDef == null) continue;

                        GameObject sRowObj = new GameObject("SeedRow_" + seedId);
                        sRowObj.transform.SetParent(listContentTransform, false);
                        RectTransform srRect = sRowObj.AddComponent<RectTransform>();
                        srRect.sizeDelta = new Vector2(760f, 50f);

                        Image srBg = sRowObj.AddComponent<Image>();
                        srBg.sprite = UIStyleUtility.CreateOutlinePillSprite(760, 50, 10, 1, new Color(0.20f, 0.70f, 0.35f), new Color(0.12f, 0.16f, 0.20f, 0.95f));

                        GameObject txtObj = new GameObject("Txt");
                        txtObj.transform.SetParent(sRowObj.transform, false);
                        RectTransform tRect = txtObj.AddComponent<RectTransform>();
                        tRect.anchoredPosition = new Vector2(-60f, 0f);
                        tRect.sizeDelta = new Vector2(600f, 44f);

                        Text txt = txtObj.AddComponent<Text>();
                        txt.font = font;
                        string zeroKgStr = LocalizationManager.L("Barn_ZeroKg", "(0 KG - Yer Kaplamaz)", "(0 KG - Takes No Space)");
                        txt.text = $"{sDef.iconEmoji}  <b>{sDef.LocalizedName}</b>  <color=#80D8FF>{zeroKgStr}</color>";
                        txt.fontSize = 15;
                        txt.alignment = TextAnchor.MiddleLeft;
                        txt.color = Color.white;

                        GameObject countObj = new GameObject("Count");
                        countObj.transform.SetParent(sRowObj.transform, false);
                        RectTransform cRect = countObj.AddComponent<RectTransform>();
                        cRect.anchoredPosition = new Vector2(270f, 0f);
                        cRect.sizeDelta = new Vector2(180f, 44f);

                        Text cTxt = countObj.AddComponent<Text>();
                        cTxt.font = font;
                        string pcsStr = LocalizationManager.L("Label_Pcs", "Adet", "Pcs");
                        cTxt.text = $"<color=#00E676><b>{seedCount} {pcsStr}</b></color>";
                        cTxt.fontSize = 15;
                        cTxt.alignment = TextAnchor.MiddleRight;
                    }
                }
            }

            // --- BÖLÜM 2: BİÇİLEN MAHSULLER (AHIR DEPOSU) ---
            GameObject cropHeaderObj = new GameObject("Header_Crops");
            cropHeaderObj.transform.SetParent(listContentTransform, false);
            RectTransform chRect = cropHeaderObj.AddComponent<RectTransform>();
            chRect.sizeDelta = new Vector2(760f, 38f);

            Text chTxt = cropHeaderObj.AddComponent<Text>();
            chTxt.font = font;
            chTxt.text = LocalizationManager.L("Barn_HeaderHarvestedCrops", "🌾 BİÇİLEN MAHSULLER (DAĞITIM & SEVKİYAT İÇİN SEÇİNİZ)", "🌾 HARVESTED CROPS (SELECT TO DISTRIBUTE & SHIP)");
            chTxt.fontSize = 14;
            chTxt.fontStyle = FontStyle.Bold;
            chTxt.alignment = TextAnchor.MiddleLeft;
            chTxt.color = new Color(0.95f, 0.75f, 0.20f);

            Dictionary<string, int> crops = GardenSeedInventoryManager.Instance.GetBarnCropInventory();

            if (crops == null || crops.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyMsg");
                emptyObj.transform.SetParent(listContentTransform, false);
                RectTransform eRect = emptyObj.AddComponent<RectTransform>();
                eRect.sizeDelta = new Vector2(760f, 60f);

                Text eTxt = emptyObj.AddComponent<Text>();
                eTxt.font = font;
                eTxt.text = LocalizationManager.L("Barn_EmptyCropsMsg", "Ahırda henüz hiç biçilmiş mahsul bulunmuyor.\nTarlalarınızdan hasat ettiğiniz ürünler burada birikir!", "There are no harvested crops in the barn yet.\nCrops harvested from your fields will accumulate here!");
                eTxt.fontSize = 14;
                eTxt.alignment = TextAnchor.MiddleLeft;
                eTxt.color = Color.gray;
                return;
            }

            int matchedCount = 0;

            foreach (var kvp in crops)
            {
                string seedId = kvp.Key;
                int count = kvp.Value;
                GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
                WorkshopRecipeDef wRecipe = (sDef == null) ? WorkshopMachineDatabase.GetRecipeByOutputId(seedId) : null;
                if (sDef == null && wRecipe == null) continue;
                if (count <= 0) continue;

                // Arama Filtresi Kontrolü
                string itemName = (sDef != null) ? sDef.name : wRecipe.outputNameTr;
                string itemEnName = (sDef != null) ? (!string.IsNullOrEmpty(sDef.nameEn) ? sDef.nameEn : "") : wRecipe.outputNameEn;
                string itemEmoji = (sDef != null) ? sDef.iconEmoji : wRecipe.iconEmoji;

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    if (!itemName.ToLower().Contains(searchQuery) && !itemEnName.ToLower().Contains(searchQuery) && !seedId.ToLower().Contains(searchQuery))
                    {
                        continue;
                    }
                }

                matchedCount++;

                int rawCost = (sDef != null) ? Mathf.Max(1, Mathf.RoundToInt(sDef.unitSalePrice / 1.40f)) : Mathf.Max(1, Mathf.RoundToInt(wRecipe.unitSalePrice / 1.80f));
                int salePrice = (sDef != null) ? sDef.unitSalePrice : wRecipe.unitSalePrice;
                int quickSellUnitPrice = Mathf.Max(1, Mathf.RoundToInt(rawCost * 1.20f));
                string unitLabel = (sDef != null) ? "KG" : LocalizationManager.L("Unit_Pieces", "Adet", "Pcs");
                string itemShortName = (sDef != null) ? sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "") : wRecipe.LocalizedName;

                GameObject rowObj = new GameObject("Row_" + seedId);
                rowObj.transform.SetParent(listContentTransform, false);
                RectTransform rRect = rowObj.AddComponent<RectTransform>();
                rRect.sizeDelta = new Vector2(760f, 64f);

                Image rBg = rowObj.AddComponent<Image>();
                Color rowBorder = (wRecipe != null) ? new Color(0.95f, 0.75f, 0.20f) : new Color(0.25f, 0.35f, 0.45f);
                rBg.sprite = UIStyleUtility.CreateOutlinePillSprite(760, 64, 10, 1, rowBorder, new Color(0.13f, 0.17f, 0.22f, 0.96f));

                // Mahsul Bilgisi (Sol)
                GameObject txtObj = new GameObject("Txt");
                txtObj.transform.SetParent(rowObj.transform, false);
                RectTransform tRect = txtObj.AddComponent<RectTransform>();
                tRect.anchoredPosition = new Vector2(-125f, 0f);
                tRect.sizeDelta = new Vector2(470f, 48f);

                Text txt = txtObj.AddComponent<Text>();
                txt.font = font;

                if (wRecipe != null)
                {
                    string gourmetDetailFmt = LocalizationManager.L("Barn_GourmetRowDetailFmt", "Market: <b>{0}C</b> (%80 Kâr) | Hızlı Satış: <b>{1}C</b> (%20 Kâr)", "Store: <b>{0}C</b> (+80%) | Quick Sell: <b>{1}C</b> (+20%)");
                    string gourmetTag = LocalizationManager.L("Barn_GourmetTag", "(🌟 Lüks Gurme Ürün)", "(🌟 Premium Gourmet Product)");
                    txt.text = $"{itemEmoji}  <b><size=16>{itemShortName}</size></b> <color=#FFD700><size=12>{gourmetTag}</size></color>\n<color=#80D8FF><size=12>{string.Format(gourmetDetailFmt, salePrice, quickSellUnitPrice)}</size></color>";
                }
                else
                {
                    string rowDetailFmt = LocalizationManager.L("Barn_RowDetailFmt", "Market: <b>{0}C</b> (%40 Kâr) | Hızlı Satış: <b>{1}C</b> (%20 Kâr)", "Store: <b>{0}C</b> (+40%) | Quick Sell: <b>{1}C</b> (+20%)");
                    txt.text = $"{itemEmoji}  <b><size=16>{itemShortName}</size></b>\n<color=#80D8FF><size=12>{string.Format(rowDetailFmt, salePrice, quickSellUnitPrice)}</size></color>";
                }
                txt.fontSize = 14;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = Color.white;

                // Miktar (Orta-Sağ)
                GameObject countObj = new GameObject("Count");
                countObj.transform.SetParent(rowObj.transform, false);
                RectTransform cRect = countObj.AddComponent<RectTransform>();
                cRect.anchoredPosition = new Vector2(175f, 0f);
                cRect.sizeDelta = new Vector2(110f, 48f);

                Text cTxt = countObj.AddComponent<Text>();
                cTxt.font = font;
                string countColor = (wRecipe != null) ? "#FFD700" : "#00E676";
                cTxt.text = $"<color={countColor}><b>{count} {unitLabel}</b></color>";
                cTxt.fontSize = 16;
                cTxt.alignment = TextAnchor.MiddleRight;

                // Mahsul Dağıt / Sevk Et Butonu (Sağ)
                GameObject distBtnObj = new GameObject("DistributeBtn");
                distBtnObj.transform.SetParent(rowObj.transform, false);
                RectTransform dbRect = distBtnObj.AddComponent<RectTransform>();
                dbRect.anchoredPosition = new Vector2(295f, 0f);
                dbRect.sizeDelta = new Vector2(120f, 42f);

                Image dbBg = distBtnObj.AddComponent<Image>();
                dbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(120, 42, 10, new Color(0.18f, 0.65f, 0.95f));
                dbBg.raycastTarget = true;

                Button dbBtn = distBtnObj.AddComponent<Button>();
                dbBtn.targetGraphic = dbBg;
                string currentSeedId = seedId;
                int currentAvailableCount = count;
                dbBtn.onClick.AddListener(() => {
                    OpenCropDistributionModal(currentSeedId, currentAvailableCount);
                });

                GameObject dbTxtObj = new GameObject("Label");
                dbTxtObj.transform.SetParent(distBtnObj.transform, false);
                RectTransform dbtRect = dbTxtObj.AddComponent<RectTransform>();
                dbtRect.anchorMin = Vector2.zero;
                dbtRect.anchorMax = Vector2.one;

                Text dbTxt = dbTxtObj.AddComponent<Text>();
                dbTxt.font = font;
                dbTxt.text = LocalizationManager.L("Barn_BtnDistribute", "📦 SEVK ET", "📦 SHIP / DIST");
                dbTxt.fontSize = 13;
                dbTxt.fontStyle = FontStyle.Bold;
                dbTxt.alignment = TextAnchor.MiddleCenter;
                dbTxt.color = Color.white;
                dbTxt.raycastTarget = false;
            }

            if (matchedCount == 0 && !string.IsNullOrEmpty(searchQuery))
            {
                GameObject noMatchObj = new GameObject("NoMatchMsg");
                noMatchObj.transform.SetParent(listContentTransform, false);
                RectTransform nmRect = noMatchObj.AddComponent<RectTransform>();
                nmRect.sizeDelta = new Vector2(760f, 50f);

                        Text nmTxt = noMatchObj.AddComponent<Text>();
                nmTxt.font = font;
                nmTxt.text = string.Format(LocalizationManager.L("Barn_NoMatch", "🔍 \"{0}\" aramasına uygun mahsul bulunamadı.", "🔍 No crops found matching \"{0}\"."), searchQuery);
                nmTxt.fontSize = 14;
                nmTxt.alignment = TextAnchor.MiddleCenter;
                nmTxt.color = new Color(0.85f, 0.70f, 0.30f);
            }
        }

        /// <summary>
        /// Seçilen tek bir mahsul için miktar seçici ve detaylı dağıtım modalını açar.
        /// </summary>
        private void OpenCropDistributionModal(string seedId, int totalAvailable)
        {
            if (distributionModalObj != null) Destroy(distributionModalObj);

            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
            WorkshopRecipeDef wRecipe = (sDef == null) ? WorkshopMachineDatabase.GetRecipeByOutputId(seedId) : null;
            if ((sDef == null && wRecipe == null) || totalAvailable <= 0) return;

            Font font = UIStyleUtility.GetGlobalFont(16);
            string cropShortName = (sDef != null) ? sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "") : wRecipe.LocalizedName;
            string itemEmoji = (sDef != null) ? sDef.iconEmoji : wRecipe.iconEmoji;
            string unitLabel = (sDef != null) ? "KG" : LocalizationManager.L("Unit_Pieces", "Adet", "Pcs");

            distributionModalObj = new GameObject("Distribution_Modal_Backdrop");
            distributionModalObj.transform.SetParent(canvasObj.transform, false);
            RectTransform dmBackdropRect = distributionModalObj.AddComponent<RectTransform>();
            dmBackdropRect.anchorMin = Vector2.zero;
            dmBackdropRect.anchorMax = Vector2.one;
            dmBackdropRect.sizeDelta = Vector2.zero;

            Image dmBackdropImg = distributionModalObj.AddComponent<Image>();
            dmBackdropImg.color = new Color(0f, 0f, 0f, 0.75f);
            dmBackdropImg.raycastTarget = true;

            // Modal Panel (640 x 520)
            GameObject dPanel = new GameObject("Distribution_Panel");
            dPanel.transform.SetParent(distributionModalObj.transform, false);
            RectTransform dpRect = dPanel.AddComponent<RectTransform>();
            dpRect.anchoredPosition = Vector2.zero;
            dpRect.sizeDelta = new Vector2(640f, 520f);

            Image dpBg = dPanel.AddComponent<Image>();
            dpBg.sprite = UIStyleUtility.CreateOutlinePillSprite(640, 520, 18, 3, new Color(0.20f, 0.75f, 0.95f), new Color(0.09f, 0.13f, 0.18f, 0.98f));
            dpBg.raycastTarget = true;

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(dPanel.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 215f);
            tRect.sizeDelta = new Vector2(560f, 40f);

            Text tTxt = titleObj.AddComponent<Text>();
            tTxt.font = font;
            string titleFmt = LocalizationManager.L("Dist_Title", "{0} {1} — Dağıtım & Sevk", "{0} {1} — Distribution & Transfer");
            tTxt.text = string.Format(titleFmt, itemEmoji, cropShortName);
            tTxt.fontSize = 20;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = new Color(0.30f, 0.85f, 1f);

            // Kapat Butonu
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(dPanel.transform, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchoredPosition = new Vector2(280f, 215f);
            clRect.sizeDelta = new Vector2(38f, 38f);

            Image clBg = closeObj.AddComponent<Image>();
            clBg.sprite = UIStyleUtility.CreateRoundedPillSprite(38, 38, 19, new Color(0.92f, 0.20f, 0.22f));
            clBg.raycastTarget = true;

            Button clBtn = closeObj.AddComponent<Button>();
            clBtn.targetGraphic = clBg;
            clBtn.onClick.AddListener(() => {
                if (distributionModalObj != null) Destroy(distributionModalObj);
            });

            GameObject clTxtObj = new GameObject("X");
            clTxtObj.transform.SetParent(closeObj.transform, false);
            RectTransform cltRect = clTxtObj.AddComponent<RectTransform>();
            cltRect.anchorMin = Vector2.zero;
            cltRect.anchorMax = Vector2.one;

            Text clTxt = clTxtObj.AddComponent<Text>();
            clTxt.font = font;
            clTxt.text = "✖";
            clTxt.fontSize = 20;
            clTxt.fontStyle = FontStyle.Bold;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;

            // Mevcut Stok Bilgisi
            GameObject availObj = new GameObject("AvailableStock");
            availObj.transform.SetParent(dPanel.transform, false);
            RectTransform aRect = availObj.AddComponent<RectTransform>();
            aRect.anchoredPosition = new Vector2(0f, 165f);
            aRect.sizeDelta = new Vector2(560f, 30f);

            Text aTxt = availObj.AddComponent<Text>();
            aTxt.font = font;
            string availFmt = LocalizationManager.L("Dist_AvailableStock", "Ahırdaki Mevcut Stok: <color=#00E676><b>{0} {1}</b></color>", "Available in Barn: <color=#00E676><b>{0} {1}</b></color>");
            aTxt.text = string.Format(availFmt, totalAvailable, unitLabel);
            aTxt.fontSize = 15;
            aTxt.alignment = TextAnchor.MiddleCenter;
            aTxt.color = Color.white;

            // Seçilen Miktar State
            int selectedAmount = totalAvailable;

            // Miktar Göstergesi Paneli
            GameObject amtObj = new GameObject("AmountDisplay");
            amtObj.transform.SetParent(dPanel.transform, false);
            RectTransform amtRect = amtObj.AddComponent<RectTransform>();
            amtRect.anchoredPosition = new Vector2(0f, 105f);
            amtRect.sizeDelta = new Vector2(220f, 48f);

            Image amtBg = amtObj.AddComponent<Image>();
            amtBg.sprite = UIStyleUtility.CreateOutlinePillSprite(220, 48, 12, 1, new Color(0.30f, 0.70f, 0.90f), new Color(0.14f, 0.18f, 0.24f, 0.95f));

            GameObject amtTxtObj = new GameObject("AmountText");
            amtTxtObj.transform.SetParent(amtObj.transform, false);
            RectTransform atRect = amtTxtObj.AddComponent<RectTransform>();
            atRect.anchorMin = Vector2.zero;
            atRect.anchorMax = Vector2.one;
            atRect.sizeDelta = Vector2.zero;

            Text amtTxt = amtTxtObj.AddComponent<Text>();
            amtTxt.font = font;
            amtTxt.text = $"<color=#00E676><b>{selectedAmount} {unitLabel}</b></color>";
            amtTxt.fontSize = 22;
            amtTxt.fontStyle = FontStyle.Bold;
            amtTxt.alignment = TextAnchor.MiddleCenter;

            System.Action updateDisplay = () => {
                selectedAmount = Mathf.Clamp(selectedAmount, 1, totalAvailable);
                if (amtTxt != null) amtTxt.text = $"<color=#00E676><b>{selectedAmount} {unitLabel}</b></color>";
            };

            // Buton: -10
            CreateStepperButton(dPanel.transform, font, new Vector2(-230f, 105f), new Vector2(54f, 44f), "-10", () => {
                selectedAmount -= 10;
                updateDisplay();
            });

            // Buton: -1
            CreateStepperButton(dPanel.transform, font, new Vector2(-155f, 105f), new Vector2(54f, 44f), "-1", () => {
                selectedAmount -= 1;
                updateDisplay();
            });

            // Buton: +1
            CreateStepperButton(dPanel.transform, font, new Vector2(155f, 105f), new Vector2(54f, 44f), "+1", () => {
                selectedAmount += 1;
                updateDisplay();
            });

            // Buton: +10
            CreateStepperButton(dPanel.transform, font, new Vector2(230f, 105f), new Vector2(54f, 44f), "+10", () => {
                selectedAmount += 10;
                updateDisplay();
            });

            // Hızlı Yüzde Butonları (%25, %50, %100 - TÜMÜ)
            GameObject presetRow = new GameObject("PresetRow");
            presetRow.transform.SetParent(dPanel.transform, false);
            RectTransform prRect = presetRow.AddComponent<RectTransform>();
            prRect.anchoredPosition = new Vector2(0f, 50f);
            prRect.sizeDelta = new Vector2(480f, 36f);

            CreatePresetButton(presetRow.transform, font, new Vector2(-160f, 0f), "%25", () => {
                selectedAmount = Mathf.Max(1, Mathf.RoundToInt(totalAvailable * 0.25f));
                updateDisplay();
            });

            CreatePresetButton(presetRow.transform, font, new Vector2(0f, 0f), "%50", () => {
                selectedAmount = Mathf.Max(1, Mathf.RoundToInt(totalAvailable * 0.50f));
                updateDisplay();
            });

            CreatePresetButton(presetRow.transform, font, new Vector2(160f, 0f), LocalizationManager.L("Dist_All", "TÜMÜ (%100)", "ALL (100%)"), () => {
                selectedAmount = totalAvailable;
                updateDisplay();
            });

            // 3 BÜYÜK HEDEF SEVKİYAT KARTI

            // 1. MARKETE GÖNDER
            string marketLabel = (wRecipe != null)
                ? LocalizationManager.L("Dist_Btn_MarketGourmet", "🚛 MARKETE GÖNDER (GURME RAFINA DİZİLMEK ÜZERE)", "🚛 SHIP TO STORE (FOR GOURMET SHELF)")
                : LocalizationManager.L("Dist_Btn_Market", "🚛 MARKETE GÖNDER (YEŞİL KAMYONLA %40 KÂR)", "🚛 SHIP TO STORE (GREEN TRUCK +40% PROFIT)");

            CreateDestinationButton(
                dPanel.transform, font,
                new Vector2(0f, -15f),
                new Vector2(520f, 54f),
                new Color(0.20f, 0.75f, 0.35f),
                marketLabel,
                () => {
                    ExecuteSingleCropSendToMarket(seedId, selectedAmount);
                    if (distributionModalObj != null) Destroy(distributionModalObj);
                }
            );

            // 2. ATÖLYEYE GÖNDER (Yalnızca Ham Mahsuller İçin)
            if (sDef != null)
            {
                CreateDestinationButton(
                    dPanel.transform, font,
                    new Vector2(0f, -80f),
                    new Vector2(520f, 54f),
                    new Color(0.95f, 0.55f, 0.15f),
                    LocalizationManager.L("Dist_Btn_Workshop", "🏭 ATÖLYEYE GÖNDER (HAMMADDE PALETİNE DİZ)", "🏭 SHIP TO WORKSHOP (STACK ON RAW PALLET)"),
                    () => {
                        ExecuteSingleCropSendToWorkshop(seedId, selectedAmount);
                        if (distributionModalObj != null) Destroy(distributionModalObj);
                    }
                );
            }

            // 3. HIZLI SAT
            float qsPosY = (sDef != null) ? -145f : -80f;
            string qsLabel = (wRecipe != null)
                ? LocalizationManager.L("Dist_Btn_QuickSellGourmet", "⚡ ANINDA HIZLI SAT (YÜKSEK KÂRLA NAKİT AL)", "⚡ INSTANT QUICK SELL (HIGH PROFIT CASH)")
                : LocalizationManager.L("Dist_Btn_QuickSell", "⚡ ANINDA HIZLI SAT (%20 KÂRLA NAKİT AL)", "⚡ INSTANT QUICK SELL (+20% INSTANT CASH)");

            CreateDestinationButton(
                dPanel.transform, font,
                new Vector2(0f, qsPosY),
                new Vector2(520f, 54f),
                new Color(0.92f, 0.72f, 0.18f),
                qsLabel,
                () => {
                    ExecuteSingleCropQuickSell(seedId, selectedAmount);
                    if (distributionModalObj != null) Destroy(distributionModalObj);
                }
            );

            // İptal Butonu
            float cancelPosY = (sDef != null) ? -205f : -150f;
            CreatePresetButton(dPanel.transform, font, new Vector2(0f, cancelPosY), LocalizationManager.L("Btn_Cancel", "Vazgeç", "Cancel"), () => {
                if (distributionModalObj != null) Destroy(distributionModalObj);
            }, 180f);
        }

        private void CreateStepperButton(Transform parent, Font font, Vector2 pos, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("StepBtn_" + label);
            btnObj.transform.SetParent(parent, false);
            RectTransform bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = pos;
            bRect.sizeDelta = size;

            Image bBg = btnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite((int)size.x, (int)size.y, 8, new Color(0.25f, 0.40f, 0.55f));
            bBg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bBg;
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        private void CreatePresetButton(Transform parent, Font font, Vector2 pos, string label, UnityEngine.Events.UnityAction onClick, float width = 120f)
        {
            GameObject btnObj = new GameObject("PresetBtn_" + label);
            btnObj.transform.SetParent(parent, false);
            RectTransform bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = pos;
            bRect.sizeDelta = new Vector2(width, 36f);

            Image bBg = btnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite((int)width, 36, 10, new Color(0.18f, 0.24f, 0.32f));
            bBg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bBg;
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        private void CreateDestinationButton(Transform parent, Font font, Vector2 pos, Vector2 size, Color color, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("DestBtn");
            btnObj.transform.SetParent(parent, false);
            RectTransform bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = pos;
            bRect.sizeDelta = size;

            Image bBg = btnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite((int)size.x, (int)size.y, 12, color);
            bBg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bBg;
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        private void ExecuteSingleCropSendToMarket(string seedId, int amount)
        {
            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
            WorkshopRecipeDef wRecipe = (sDef == null) ? WorkshopMachineDatabase.GetRecipeByOutputId(seedId) : null;
            if ((sDef == null && wRecipe == null) || amount <= 0) return;

            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
            string btnGreat = LocalizationManager.L("Btn_Great", "Harika!", "Great!");

            bool isAnyTruckOnTheWay = (GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckOnTheWay) ||
                                      (WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckOnTheWay);

            if (isAnyTruckOnTheWay)
            {
                string busyTitle = LocalizationManager.L("Modal_DockBusy_Title", "Teslimat Noktası Dolu! ⚠️", "Delivery Dock Occupied! ⚠️");
                string busyBody = LocalizationManager.L("Modal_DockBusy_Body", "Şu anda yolda veya teslimat noktasında aktif bir kamyon bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni sevkiyat yapılamaz.", "There is currently an active truck on the way or at the dock!\n\nPlease wait until it leaves.");
                ModalManager.ShowModal(busyTitle, busyBody, btnOk);
                return;
            }

            int count = amount;
            List<WholesaleProductDef> farmProductList = new List<WholesaleProductDef>();

            while (count > 0)
            {
                int packAmount = Mathf.Min(50, count);
                int rawCost = (wRecipe != null) ? Mathf.Max(1, Mathf.RoundToInt(wRecipe.unitSalePrice / 1.80f)) : 10;
                WholesaleProductDef pDef = (sDef != null)
                    ? new WholesaleProductDef(
                        sDef.id,
                        sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", ""),
                        sDef.iconEmoji,
                        FurnitureType.ProduceShelf,
                        1,
                        sDef.unitSalePrice,
                        packAmount,
                        40f
                    )
                    : new WholesaleProductDef(
                        wRecipe.outputProductId,
                        wRecipe.outputNameTr,
                        wRecipe.outputNameEn,
                        wRecipe.iconEmoji,
                        FurnitureType.GourmetShelf,
                        1,
                        rawCost,
                        packAmount,
                        80f
                    );

                farmProductList.Add(pDef);
                count -= packAmount;
                GardenSeedInventoryManager.Instance.ConsumeBarnCrop(seedId, packAmount);
            }

            if (farmProductList.Count > 0)
            {
                if (GreenTruckDeliveryManager.Instance != null)
                {
                    GreenTruckDeliveryManager.Instance.DispatchFarmDelivery(farmProductList);
                }

                HideModal();
                string cropShortName = (sDef != null) ? sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "") : wRecipe.LocalizedName;
                string unitLabel = (sDef != null) ? "KG" : LocalizationManager.L("Unit_Pieces", "Adet", "Pcs");
                string greenTitle = LocalizationManager.L("Modal_GreenTruck_Title", "Yeşil Kamyon Yola Çıktı! 🚛", "Green Truck Dispatched! 🚛");
                string greenBodyFmt = LocalizationManager.L("Modal_SingleCrop_GreenTruck_Body", "<b>{0} {1} {2}</b> Yeşil Kamyona yüklendi!\n\nKamyon dükkanın Mal Kabul kapısına yanaşıyor.", "<b>{0} {1} {2}</b> loaded onto Green Truck!\n\nThe truck is approaching the delivery dock.");
                ModalManager.ShowModal(greenTitle, string.Format(greenBodyFmt, amount, unitLabel, cropShortName), btnGreat);
            }
        }

        private void ExecuteSingleCropSendToWorkshop(string seedId, int amount)
        {
            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
            if (sDef == null || amount <= 0) return;

            GardenSeedInventoryManager.Instance.ConsumeBarnCrop(seedId, amount);
            if (WorkshopPalletManager.Instance != null)
            {
                WorkshopPalletManager.Instance.AddCrops(seedId, amount);
            }

            RefreshList();
            string cropShortName = sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "");
            string sucTitle = LocalizationManager.L("Modal_SendWorkshop_SucTitle", "🎉 Atölyeye Gönderildi!", "🎉 Sent to Workshop!");
            string sucBody = string.Format(
                LocalizationManager.L(
                    "Modal_SingleCrop_SendWorkshop_Body",
                    "<b>{0} KG {1}</b> başarıyla Atölye Hammadde Paletine aktarıldı ve koliler palet rafına dizildi.",
                    "<b>{0} KG {1}</b> successfully transferred to Workshop Raw Pallet."
                ),
                amount,
                cropShortName
            );
            ModalManager.ShowModal(sucTitle, sucBody, LocalizationManager.L("Btn_Great", "Harika!", "Awesome!"));
        }

        private void ExecuteSingleCropQuickSell(string seedId, int amount)
        {
            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
            WorkshopRecipeDef wRecipe = (sDef == null) ? WorkshopMachineDatabase.GetRecipeByOutputId(seedId) : null;
            if ((sDef == null && wRecipe == null) || amount <= 0) return;

            int rawCost = (sDef != null) ? Mathf.Max(1, Mathf.RoundToInt(sDef.unitSalePrice / 1.40f)) : Mathf.Max(1, Mathf.RoundToInt(wRecipe.unitSalePrice / 1.80f));
            int quickSellUnitPrice = Mathf.Max(1, Mathf.RoundToInt(rawCost * 1.20f));

            int totalEarnings = quickSellUnitPrice * amount;

            GardenSeedInventoryManager.Instance.ConsumeBarnCrop(seedId, amount);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCredits(totalEarnings);
            }

            RefreshList();
            string cropShortName = (sDef != null) ? sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "") : wRecipe.LocalizedName;
            string unitLabel = (sDef != null) ? "KG" : "Adet";
            string qsTitle = LocalizationManager.L("Modal_QuickSell_Title", "⚡ Hızlı Satış Yapıldı! 💰", "⚡ Quick Sell Completed! 💰");
            string qsBody = string.Format(
                LocalizationManager.L(
                    "Modal_SingleCrop_QuickSell_Body",
                    "<b>{0} {1} {2}</b> anında satıldı!\n\n<b>Kazanılan Bakiye:</b> <color=#00E676>+{3:N0}C</color> (Birim Fiyat: {4}C)",
                    "<b>{0} {1} {2}</b> instantly sold!\n\n<b>Earned Credits:</b> <color=#00E676>+{3:N0}C</color> (Unit Price: {4}C)"
                ),
                amount,
                unitLabel,
                cropShortName,
                totalEarnings,
                quickSellUnitPrice
            );
            ModalManager.ShowModal(qsTitle, qsBody, LocalizationManager.L("Btn_Great", "Harika!", "Great!"));
        }

        private void OnSendToMarketClicked()
        {
            Dictionary<string, int> crops = GardenSeedInventoryManager.Instance.GetBarnCropInventory();
            string emptyTitle = LocalizationManager.L("Modal_BarnEmpty_Title", "Ahır Boş! ⚠️", "Barn Empty! ⚠️");
            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
            string btnGreat = LocalizationManager.L("Btn_Great", "Harika!", "Great!");

            if (crops == null || crops.Count == 0)
            {
                string emptyShipBody = LocalizationManager.L("Modal_BarnEmpty_ShipBody", "Markete göndermek için ahırınızda en az 1 adet mahsul veya atölye ürünü bulunmalıdır!", "There must be at least 1 crop or crafted product in your barn to ship to store!");
                ModalManager.ShowModal(emptyTitle, emptyShipBody, btnOk);
                return;
            }

            bool isAnyTruckOnTheWay = (GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckOnTheWay) ||
                                      (WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckOnTheWay);

            if (isAnyTruckOnTheWay)
            {
                string busyTitle = LocalizationManager.L("Modal_DockBusy_Title", "Teslimat Noktası Dolu! ⚠️", "Delivery Dock Occupied! ⚠️");
                string busyBody = LocalizationManager.L("Modal_DockBusy_Body", "Şu anda yolda veya teslimat noktasında aktif bir kamyon (Toptancı veya Çiftlik Kamyonu) bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni çiftlik sevkiyatı yapılamaz.", "There is currently an active truck (Wholesaler or Farm Truck) on the way or at the loading dock!\n\nNew farm shipments cannot be dispatched until the current truck finishes delivery and leaves.");
                ModalManager.ShowModal(busyTitle, busyBody, btnOk);
                return;
            }

            // Ahırdaki tüm ürünleri (Tarla Mahsulleri + Atölye Gurme Ürünleri) 50'şerli koli paketleri halinde kamyona yükle!
            List<WholesaleProductDef> farmProductList = new List<WholesaleProductDef>();
            List<string> seedKeys = new List<string>(crops.Keys);

            foreach (string seedId in seedKeys)
            {
                int count = crops[seedId];
                if (count <= 0) continue;

                GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
                WorkshopRecipeDef wRecipe = (sDef == null) ? WorkshopMachineDatabase.GetRecipeByOutputId(seedId) : null;
                if (sDef == null && wRecipe == null) continue;

                // 50'lik koli partileri halinde paketle
                while (count > 0)
                {
                    int packAmount = Mathf.Min(50, count);
                    int rawCost = (wRecipe != null) ? Mathf.Max(1, Mathf.RoundToInt(wRecipe.unitSalePrice / 1.80f)) : 10;
                    WholesaleProductDef pDef = (sDef != null)
                        ? new WholesaleProductDef(
                            sDef.id,
                            sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", ""),
                            sDef.iconEmoji,
                            FurnitureType.ProduceShelf,
                            1,
                            sDef.unitSalePrice,
                            packAmount,
                            40f
                        )
                        : new WholesaleProductDef(
                            wRecipe.outputProductId,
                            wRecipe.outputNameTr,
                            wRecipe.outputNameEn,
                            wRecipe.iconEmoji,
                            FurnitureType.GourmetShelf,
                            1,
                            rawCost,
                            packAmount,
                            80f
                        );

                    farmProductList.Add(pDef);
                    count -= packAmount;
                    GardenSeedInventoryManager.Instance.ConsumeBarnCrop(seedId, packAmount);
                }
            }

            if (farmProductList.Count > 0)
            {
                if (GreenTruckDeliveryManager.Instance != null)
                {
                    GreenTruckDeliveryManager.Instance.DispatchFarmDelivery(farmProductList);
                }

                HideModal();
                string greenTitle = LocalizationManager.L("Modal_GreenTruck_Title", "Yeşil Teslimat Kamyonu Yola Çıktı! 🚛", "Green Delivery Truck Dispatched! 🚛");
                string greenBodyFmt = LocalizationManager.L("Modal_GreenTruck_Body", "Ahırdaki tüm ürünler {0} koli halinde Yeşil Kamyona yüklendi!\n\nKamyon dükkanın Mal Kabul kapısına yanaşıyor. Reyoncular ürünleri raflara dizecektir.", "All barn products have been loaded onto the Green Truck in {0} packs!\n\nThe truck is approaching the loading dock.");
                ModalManager.ShowModal(greenTitle, string.Format(greenBodyFmt, farmProductList.Count), btnGreat);
            }
        }

        private void OnQuickSellClicked()
        {
            Dictionary<string, int> crops = GardenSeedInventoryManager.Instance.GetBarnCropInventory();
            string emptyTitle = LocalizationManager.L("Modal_BarnEmpty_Title", "Ahır Boş! ⚠️", "Barn Empty! ⚠️");
            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
            string btnGreat = LocalizationManager.L("Btn_Great", "Harika!", "Great!");

            if (crops == null || crops.Count == 0)
            {
                string emptySellBody = LocalizationManager.L("Modal_BarnEmpty_SellBody", "Hızlı satış yapmak için ahırınızda en az 1 adet mahsul veya atölye ürünü bulunmalıdır!", "There must be at least 1 harvested crop or crafted product in your barn to instant sell!");
                ModalManager.ShowModal(emptyTitle, emptySellBody, btnOk);
                return;
            }

            int totalEarnings = 0;
            int totalItemsSold = 0;
            List<string> seedKeys = new List<string>(crops.Keys);
            List<string> summaryDetails = new List<string>();

            foreach (string seedId in seedKeys)
            {
                int count = crops[seedId];
                if (count <= 0) continue;

                GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
                WorkshopRecipeDef wRecipe = (sDef == null) ? WorkshopMachineDatabase.GetRecipeByOutputId(seedId) : null;
                if (sDef == null && wRecipe == null) continue;

                int rawCost = (sDef != null) ? Mathf.Max(1, Mathf.RoundToInt(sDef.unitSalePrice / 1.40f)) : Mathf.Max(1, Mathf.RoundToInt(wRecipe.unitSalePrice / 1.80f));
                int quickSellUnitPrice = Mathf.Max(1, Mathf.RoundToInt(rawCost * 1.20f));

                int earnedForCrop = quickSellUnitPrice * count;
                totalEarnings += earnedForCrop;
                totalItemsSold += count;

                string cropName = (sDef != null) ? sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "") : wRecipe.LocalizedName;
                string emoji = (sDef != null) ? sDef.iconEmoji : wRecipe.iconEmoji;
                string unitLabel = (sDef != null) ? "KG" : LocalizationManager.L("Unit_Pieces", "Adet", "Pcs");

                summaryDetails.Add($"• {emoji} {cropName}: {count} {unitLabel} x {quickSellUnitPrice}C = {earnedForCrop:N0}C");

                GardenSeedInventoryManager.Instance.ConsumeBarnCrop(seedId, count);
            }

            if (totalEarnings > 0)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddCredits(totalEarnings);
                }

                RefreshList();

                string detailsText = string.Join("\n", summaryDetails);
                string qsTitle = LocalizationManager.L("Modal_QuickSell_Title", "⚡ Hızlı Satış Yapıldı! 💰", "⚡ Quick Sell Completed! 💰");
                string qsBodyFmt = LocalizationManager.L(
                    "Modal_QuickSell_Body",
                    "Ahırdaki tüm ürünler %20 kâr marjıyla anında tüccarlara satıldı!\n\n<b>Kazanılan Bakiye:</b> <color=#00E676>+{0:N0}C</color>\n<b>Toplam Satılan:</b> {1} Adet/KG\n\n<b>Satış Detayları:</b>\n{2}",
                    "All products in the barn were instantly sold to merchants at a 20% profit margin!\n\n<b>Earned Credits:</b> <color=#00E676>+{0:N0}C</color>\n<b>Total Sold:</b> {1} Units/KG\n\n<b>Sales Details:</b>\n{2}"
                );
                ModalManager.ShowModal(qsTitle, string.Format(qsBodyFmt, totalEarnings, totalItemsSold, detailsText), btnGreat);
            }
        }

        private void OnSendToWorkshopClicked()
        {
            Dictionary<string, int> crops = GardenSeedInventoryManager.Instance.GetBarnCropInventory();
            string emptyTitle = LocalizationManager.L("Modal_BarnEmpty_Title", "Ahır Boş! ⚠️", "Barn Empty! ⚠️");
            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
            string btnGreat = LocalizationManager.L("Btn_Great", "Harika!", "Great!");

            if (crops == null || crops.Count == 0)
            {
                string emptyShipBody = LocalizationManager.L(
                    "Modal_BarnEmpty_WorkshopBody",
                    "Atölyeye göndermek için ahırınızda en az 1 adet biçilmiş hammadde mahsulü bulunmalıdır!",
                    "There must be at least 1 harvested raw crop in your barn to transfer to workshop!"
                );
                ModalManager.ShowModal(emptyTitle, emptyShipBody, btnOk);
                return;
            }

            // Yalnızca Ham Mahsulleri (GardenSeedDef) Say ve Filtrele
            int totalRawKg = 0;
            List<string> rawCropKeys = new List<string>();

            foreach (var kvp in crops)
            {
                if (kvp.Value <= 0) continue;
                GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(kvp.Key);
                if (sDef != null)
                {
                    totalRawKg += kvp.Value;
                    rawCropKeys.Add(kvp.Key);
                }
            }

            if (totalRawKg <= 0 || rawCropKeys.Count == 0)
            {
                string noRawTitle = LocalizationManager.L("Modal_NoRawCrops_Title", "Hammadde Mahsulü Bulunamadı! ⚠️", "No Raw Materials Found! ⚠️");
                string noRawBody = LocalizationManager.L(
                    "Modal_NoRawCrops_Body",
                    "Atölye paletine yalnızca tarladan biçilen ham mahsuller (çilek, domates, havuç vb.) aktarılabilir.\n\nAhırdaki işlenmiş gurme ürünler (reçel, un vb.) mamul olduğu için hammadde paletine gönderilemez. Bu ürünleri <b>Markete Gönderebilir</b> veya <b>Hızlı Satabilirsiniz</b>.",
                    "Only raw crops harvested from fields can be transferred to the workshop pallet.\n\nCrafted gourmet items (jam, flour, etc.) are finished products and cannot be transferred to the raw material pallet. You can ship them to the store or quick-sell."
                );
                ModalManager.ShowModal(noRawTitle, noRawBody, btnOk);
                return;
            }

            string confirmTitle = LocalizationManager.L("Modal_SendWorkshop_Title", "🏭 Atölyeye Mahsul Aktarımı", "🏭 Transfer Crops to Workshop");
            string confirmBody = string.Format(
                LocalizationManager.L(
                    "Modal_SendWorkshop_Body",
                    "Ahırınızdaki toplam **{0} KG** hammadde mahsulünü Atölye Hammadde Paletine aktarmak istiyor musunuz?\n\n📦 Aktarılan ürünler atölyede işlenmek üzere palet rafına koliler halinde anında dizilecektir. (Mamul gurme ürünler ahırda korunur).",
                    "Do you want to transfer all **{0} KG** raw crops in your barn to the Workshop Raw Material Pallet?\n\n📦 Transferred crops will be stacked as boxes on the workshop pallet rack."
                ),
                totalRawKg
            );
            string btnConfirm = LocalizationManager.L("Btn_ConfirmTransfer", "Evet, Atölyeye Aktar", "Yes, Transfer to Workshop");
            string btnCancel = LocalizationManager.L("Btn_Cancel", "Vazgeç", "Cancel");

            ModalManager.ShowConfirmModal(confirmTitle, confirmBody, () => {
                int transferredTotal = 0;

                foreach (string seedId in rawCropKeys)
                {
                    if (!crops.ContainsKey(seedId)) continue;
                    int count = crops[seedId];
                    if (count <= 0) continue;

                    GardenSeedInventoryManager.Instance.ConsumeBarnCrop(seedId, count);
                    if (WorkshopPalletManager.Instance != null)
                    {
                        WorkshopPalletManager.Instance.AddCrops(seedId, count);
                    }
                    transferredTotal += count;
                }

                RefreshList();

                string sucTitle = LocalizationManager.L("Modal_SendWorkshop_SucTitle", "🎉 Atölyeye Gönderildi!", "🎉 Sent to Workshop!");
                string sucBody = string.Format(
                    LocalizationManager.L(
                        "Modal_SendWorkshop_SucBody",
                        "Tebrikler! Toplam **{0} KG** hammadde mahsulü Atölye Hammadde Paletine aktarıldı ve koliler palet rafına dizildi.",
                        "Congratulations! Total **{0} KG** raw crops transferred to Workshop Raw Material Pallet."
                    ),
                    transferredTotal
                );
                ModalManager.ShowModal(sucTitle, sucBody, btnGreat);
            }, btnConfirm, btnCancel);
        }
    }
}
