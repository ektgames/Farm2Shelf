using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// 3D Ahıra tıklandığında açılan Ahır Envanteri ve Markete Gönderim Arayüzü.
    /// </summary>
    public class BarnInventoryModalUI : MonoBehaviour
    {
        public static BarnInventoryModalUI Instance { get; private set; }
        public static bool IsBarnModalOpen => Instance != null && Instance.canvasObj != null && Instance.canvasObj.activeInHierarchy;

        private GameObject canvasObj;
        private Transform listContentTransform;
        private Text capacityText;

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

        private void Update()
        {
            if (IsBarnModalOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                HideModal();
            }
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

            ModalManager.SetModalOpen(true);
            BuildUI();
            RefreshList();
        }

        public void HideModal()
        {
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

            // Modal Paneli (820x660)
            GameObject panelObj = new GameObject("Barn_Panel");
            panelObj.transform.SetParent(backdrop.transform, false);

            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(820f, 660f);

            Image pBg = panelObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 660, 18, 3, new Color(0.30f, 0.75f, 0.35f), new Color(0.10f, 0.14f, 0.18f, 0.98f));
            pBg.raycastTarget = true; // Panel içine tıklamalar backdrop'a sızmaz

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(-150f, 280f);
            tRect.sizeDelta = new Vector2(450f, 50f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = LocalizationManager.L("Barn_Title", "🌾 AHIR ÜRÜN ENVANTERİ", "🌾 BARN CROP INVENTORY");
            tText.fontSize = 24;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleLeft;
            tText.color = new Color(0.35f, 0.85f, 0.40f);
            tText.raycastTarget = false;

            // Kapasite Metni
            GameObject capObj = new GameObject("CapacityText");
            capObj.transform.SetParent(panelObj.transform, false);
            RectTransform cRect = capObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(170f, 280f);
            cRect.sizeDelta = new Vector2(360f, 40f);

            capacityText = capObj.AddComponent<Text>();
            capacityText.font = font;
            capacityText.fontSize = 17;
            capacityText.alignment = TextAnchor.MiddleRight;
            capacityText.color = Color.white;
            capacityText.raycastTarget = false;

            // Kapat Butonu (X)
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(panelObj.transform, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchoredPosition = new Vector2(375f, 280f);
            clRect.sizeDelta = new Vector2(44f, 44f);

            Image clBg = closeObj.AddComponent<Image>();
            clBg.sprite = UIStyleUtility.CreateRoundedPillSprite(44, 44, 10, new Color(0.85f, 0.20f, 0.25f));
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
            clTxt.fontSize = 20;
            clTxt.fontStyle = FontStyle.Bold;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;
            clTxt.raycastTarget = false;

            // Scroll Area
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            RectTransform sRect = scrollObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 15f);
            sRect.sizeDelta = new Vector2(760f, 440f);

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
            vlg.spacing = 12;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cntRect;
            listContentTransform = content.transform;

            listContentTransform = content.transform;

            // Alt SOL Buton: "MARKETE GÖNDER 🚛 (%40 KÂR)"
            GameObject sendBtnObj = new GameObject("SendToMarketBtn");
            sendBtnObj.transform.SetParent(panelObj.transform, false);
            RectTransform sbRect = sendBtnObj.AddComponent<RectTransform>();
            sbRect.anchoredPosition = new Vector2(-190f, -260f);
            sbRect.sizeDelta = new Vector2(360f, 52f);

            Image sbBg = sendBtnObj.AddComponent<Image>();
            sbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(360, 52, 12, new Color(0.20f, 0.75f, 0.35f));

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
            sbTxt.text = LocalizationManager.L("Barn_SendMarket", "🚛 MARKETE GÖNDER (%40 KÂR)", "🚛 SHIP TO STORE (+40% PROFIT)");
            sbTxt.fontSize = 15;
            sbTxt.fontStyle = FontStyle.Bold;
            sbTxt.alignment = TextAnchor.MiddleCenter;
            sbTxt.color = Color.white;
            sbTxt.raycastTarget = false;

            // Alt SAĞ Buton: "⚡ HIZLI SAT (%20 KÂR)"
            GameObject quickSellBtnObj = new GameObject("QuickSellBtn");
            quickSellBtnObj.transform.SetParent(panelObj.transform, false);
            RectTransform qsRect = quickSellBtnObj.AddComponent<RectTransform>();
            qsRect.anchoredPosition = new Vector2(190f, -260f);
            qsRect.sizeDelta = new Vector2(360f, 52f);

            Image qsBg = quickSellBtnObj.AddComponent<Image>();
            qsBg.sprite = UIStyleUtility.CreateRoundedPillSprite(360, 52, 12, new Color(0.95f, 0.65f, 0.15f));
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
            qsTxt.text = LocalizationManager.L("Barn_QuickSell", "⚡ HIZLI SAT (%20 KÂR)", "⚡ INSTANT SELL (+20% PROFIT)");
            qsTxt.fontSize = 15;
            qsTxt.fontStyle = FontStyle.Bold;
            qsTxt.alignment = TextAnchor.MiddleCenter;
            qsTxt.color = Color.white;
            qsTxt.raycastTarget = false;
        }

        private void RefreshList()
        {
            if (listContentTransform == null) return;
            foreach (Transform t in listContentTransform) Destroy(t.gameObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            int currentStored = GardenSeedInventoryManager.Instance.GetTotalBarnStoredAmount();
            int maxCap = GardenSeedInventoryManager.Instance.MaxBarnCapacity;
            capacityText.text = LocalizationManager.L("Barn_Capacity", $"📦 Mahsul Doluluğu: <color=#00E676>{currentStored}</color> / {maxCap} KG", $"📦 Crop Capacity: <color=#00E676>{currentStored}</color> / {maxCap} KG");

            // --- BÖLÜM 1: SAHİP OLUNAN TOHUMLAR (0 KG YER KAPLAMAZ) ---
            GameObject seedHeaderObj = new GameObject("Header_Seeds");
            seedHeaderObj.transform.SetParent(listContentTransform, false);
            RectTransform shRect = seedHeaderObj.AddComponent<RectTransform>();
            Text shTxt = seedHeaderObj.AddComponent<Text>();
            shTxt.font = font;
            shTxt.text = LocalizationManager.L("Barn_HeaderOwnedSeeds", "🌱 SAHİP OLUNAN TOHUM LAR (AHIR KİLERİ — 0 KG YER KAPLAMAZ)", "🌱 OWNED SEEDS (BARN STORAGE — 0 KG TAKES NO SPACE)");
            shTxt.fontSize = 15;
            shTxt.fontStyle = FontStyle.Bold;
            shTxt.alignment = TextAnchor.MiddleLeft;
            shTxt.color = new Color(0.35f, 0.85f, 0.40f);
            shTxt.raycastTarget = false;

            Dictionary<string, int> ownedSeeds = GardenSeedInventoryManager.Instance.GetAllOwnedSeeds();
            if (ownedSeeds == null || ownedSeeds.Count == 0)
            {
                GameObject noSeedObj = new GameObject("NoSeedsMsg");
                noSeedObj.transform.SetParent(listContentTransform, false);
                RectTransform nsRect = noSeedObj.AddComponent<RectTransform>();
                nsRect.sizeDelta = new Vector2(740f, 40f);

                Text nsTxt = noSeedObj.AddComponent<Text>();
                nsTxt.font = font;
                nsTxt.text = LocalizationManager.L("Msg_NoSeedsOwned", "Henüz hiç tohumunuz yok. EKT Tablet -> Tohumlar sekmesinden tohum satın alabilirsiniz.", "You don't have any seeds yet. You can buy seeds from EKT Tablet -> Seeds tab.");
                nsTxt.fontSize = 14;
                nsTxt.alignment = TextAnchor.MiddleLeft;
                nsTxt.color = Color.gray;
                nsTxt.raycastTarget = false;
            }
            else
            {
                foreach (var kvp in ownedSeeds)
                {
                    string seedId = kvp.Key;
                    int seedCount = kvp.Value;
                    GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
                    if (sDef == null || seedCount <= 0) continue;

                    GameObject sRowObj = new GameObject("SeedRow_" + seedId);
                    sRowObj.transform.SetParent(listContentTransform, false);
                    RectTransform srRect = sRowObj.AddComponent<RectTransform>();
                    srRect.sizeDelta = new Vector2(740f, 54f);

                    Image srBg = sRowObj.AddComponent<Image>();
                    srBg.sprite = UIStyleUtility.CreateOutlinePillSprite(740, 54, 10, 1, new Color(0.20f, 0.70f, 0.35f), new Color(0.12f, 0.16f, 0.20f, 0.95f));

                    GameObject txtObj = new GameObject("Txt");
                    txtObj.transform.SetParent(sRowObj.transform, false);
                    RectTransform tRect = txtObj.AddComponent<RectTransform>();
                    tRect.anchoredPosition = new Vector2(-60f, 0f);
                    tRect.sizeDelta = new Vector2(580f, 44f);

                    Text txt = txtObj.AddComponent<Text>();
                    txt.font = font;
                    string zeroKgStr = LocalizationManager.L("Barn_ZeroKg", "(0 KG - Yer Kaplamaz)", "(0 KG - Takes No Space)");
                    txt.text = $"{sDef.iconEmoji}  <b>{sDef.LocalizedName}</b>  <color=#80D8FF>{zeroKgStr}</color>";
                    txt.fontSize = 16;
                    txt.alignment = TextAnchor.MiddleLeft;
                    txt.color = Color.white;

                    GameObject countObj = new GameObject("Count");
                    countObj.transform.SetParent(sRowObj.transform, false);
                    RectTransform cRect = countObj.AddComponent<RectTransform>();
                    cRect.anchoredPosition = new Vector2(260f, 0f);
                    cRect.sizeDelta = new Vector2(180f, 44f);

                    Text cTxt = countObj.AddComponent<Text>();
                    cTxt.font = font;
                    string pcsStr = LocalizationManager.L("Label_Pcs", "Adet", "Pcs");
                    cTxt.text = $"<color=#00E676><b>{seedCount} {pcsStr}</b></color>";
                    cTxt.fontSize = 16;
                    cTxt.alignment = TextAnchor.MiddleRight;
                }
            }

            // --- BÖLÜM 2: BİÇİLEN MAHSULLER (KAPASİTE YER KAPLAR) ---
            GameObject cropHeaderObj = new GameObject("Header_Crops");
            cropHeaderObj.transform.SetParent(listContentTransform, false);
            RectTransform chRect = cropHeaderObj.AddComponent<RectTransform>();
            chRect.sizeDelta = new Vector2(740f, 42f);

            Text chTxt = cropHeaderObj.AddComponent<Text>();
            chTxt.font = font;
            chTxt.text = LocalizationManager.L("Barn_HeaderHarvestedCrops", "🌾 BİÇİLEN MAHSULLER (AHIR DEPO ALANI)", "🌾 HARVESTED CROPS (BARN STORAGE AREA)");
            chTxt.fontSize = 15;
            chTxt.fontStyle = FontStyle.Bold;
            chTxt.alignment = TextAnchor.MiddleLeft;
            chTxt.color = new Color(0.95f, 0.75f, 0.20f);

            Dictionary<string, int> crops = GardenSeedInventoryManager.Instance.GetBarnCropInventory();

            if (crops == null || crops.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyMsg");
                emptyObj.transform.SetParent(listContentTransform, false);
                RectTransform eRect = emptyObj.AddComponent<RectTransform>();
                eRect.sizeDelta = new Vector2(740f, 60f);

                Text eTxt = emptyObj.AddComponent<Text>();
                eTxt.font = font;
                eTxt.text = LocalizationManager.L("Barn_EmptyCropsMsg", "Ahırda henüz hiç biçilmiş mahsul bulunmuyor.\nTarlalarınızdan hasat ettiğiniz ürünler burada birikir!", "There are no harvested crops in the barn yet.\nCrops harvested from your fields will accumulate here!");
                eTxt.fontSize = 15;
                eTxt.alignment = TextAnchor.MiddleLeft;
                eTxt.color = Color.gray;
                return;
            }

            foreach (var kvp in crops)
            {
                string seedId = kvp.Key;
                int count = kvp.Value;
                GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
                if (sDef == null) continue;

                int quickSellUnitPrice = Mathf.Max(1, Mathf.RoundToInt((sDef.unitSalePrice / 1.40f) * 1.20f));

                GameObject rowObj = new GameObject("Row_" + seedId);
                rowObj.transform.SetParent(listContentTransform, false);
                RectTransform rRect = rowObj.AddComponent<RectTransform>();
                rRect.sizeDelta = new Vector2(740f, 60f);

                Image rBg = rowObj.AddComponent<Image>();
                rBg.sprite = UIStyleUtility.CreateOutlinePillSprite(740, 60, 10, 1, new Color(0.25f, 0.30f, 0.35f), new Color(0.14f, 0.18f, 0.22f, 0.95f));

                GameObject txtObj = new GameObject("Txt");
                txtObj.transform.SetParent(rowObj.transform, false);
                RectTransform tRect = txtObj.AddComponent<RectTransform>();
                tRect.anchoredPosition = new Vector2(-60f, 0f);
                tRect.sizeDelta = new Vector2(580f, 48f);

                Text txt = txtObj.AddComponent<Text>();
                txt.font = font;
                string cropShortName = sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "");
                string rowDetailFmt = LocalizationManager.L("Barn_RowDetailFmt", "Markette: {0}C (%40 Kâr) | Hızlı Satış: {1}C (%20 Kâr)", "In Store: {0}C (+40% Profit) | Quick Sell: {1}C (+20% Profit)");
                txt.text = $"{sDef.iconEmoji}  <b>{cropShortName}</b>  — <color=#00E676>{string.Format(rowDetailFmt, sDef.unitSalePrice, quickSellUnitPrice)}</color>";
                txt.fontSize = 15;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = Color.white;

                GameObject countObj = new GameObject("Count");
                countObj.transform.SetParent(rowObj.transform, false);
                RectTransform cRect = countObj.AddComponent<RectTransform>();
                cRect.anchoredPosition = new Vector2(260f, 0f);
                cRect.sizeDelta = new Vector2(180f, 48f);

                Text cTxt = countObj.AddComponent<Text>();
                cTxt.font = font;
                cTxt.text = $"<color=#00E676><b>{count} KG</b></color>";
                cTxt.fontSize = 17;
                cTxt.alignment = TextAnchor.MiddleRight;
            }
        }

        private void OnSendToMarketClicked()
        {
            Dictionary<string, int> crops = GardenSeedInventoryManager.Instance.GetBarnCropInventory();
            string emptyTitle = LocalizationManager.L("Modal_BarnEmpty_Title", "Ahır Boş! ⚠️", "Barn Empty! ⚠️");
            string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
            string btnGreat = LocalizationManager.L("Btn_Great", "Harika!", "Great!");

            if (crops == null || crops.Count == 0)
            {
                string emptyShipBody = LocalizationManager.L("Modal_BarnEmpty_ShipBody", "Markete göndermek için ahırınızda en az 1 adet biçilmiş mahsul bulunmalıdır!", "There must be at least 1 harvested crop in your barn to ship to store!");
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

            // Ahırdaki tüm ürünleri 50'şerli koli paketleri halinde kamyona yükle!
            List<WholesaleProductDef> farmProductList = new List<WholesaleProductDef>();
            List<string> seedKeys = new List<string>(crops.Keys);

            foreach (string seedId in seedKeys)
            {
                int count = crops[seedId];
                GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
                if (sDef == null || count <= 0) continue;

                // 50'lik koli partileri halinde paketle
                while (count > 0)
                {
                    int packAmount = Mathf.Min(50, count);
                    WholesaleProductDef pDef = new WholesaleProductDef(
                        sDef.id,
                        sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", ""),
                        sDef.iconEmoji,
                        FurnitureType.ProduceShelf,
                        1,
                        sDef.unitSalePrice,
                        packAmount,
                        40f
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
                string greenBodyFmt = LocalizationManager.L("Modal_GreenTruck_Body", "Ahırdaki mahsuller {0} koli halinde Yeşil Kamyona yüklendi!\n\nKamyon dükkanın Mal Kabul kapısına yanaşıyor. Reyoncu ürünleri depo rafına indirecektir.", "Harvested crops have been loaded onto the Green Truck in {0} packs!\n\nThe truck is approaching the loading dock. Stockers will unload products to the storage shelf.");
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
                string emptySellBody = LocalizationManager.L("Modal_BarnEmpty_SellBody", "Hızlı satış yapmak için ahırınızda en az 1 adet biçilmiş mahsul bulunmalıdır!", "There must be at least 1 harvested crop in your barn to instant sell!");
                ModalManager.ShowModal(emptyTitle, emptySellBody, btnOk);
                return;
            }

            int totalEarnings = 0;
            int totalKgSold = 0;
            List<string> seedKeys = new List<string>(crops.Keys);
            List<string> summaryDetails = new List<string>();

            foreach (string seedId in seedKeys)
            {
                int count = crops[seedId];
                GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
                if (sDef == null || count <= 0) continue;

                // Markette satılırsa %40 kâr, Hızlı satışta %20 kâr!
                int quickSellUnitPrice = Mathf.Max(1, Mathf.RoundToInt((sDef.unitSalePrice / 1.40f) * 1.20f));
                int earnedForCrop = quickSellUnitPrice * count;
                totalEarnings += earnedForCrop;
                totalKgSold += count;

                string cropName = sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "");
                summaryDetails.Add($"• {sDef.iconEmoji} {cropName}: {count} KG x {quickSellUnitPrice}C = {earnedForCrop:N0}C");

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
                    "Ahırdaki mahsuller %20 kâr marjıyla (market fiyatına göre %20 indirimle) anında tüccarlara satıldı!\n\n<b>Kazanılan Bakiye:</b> <color=#00E676>+{0:N0}C</color>\n<b>Toplam Satılan:</b> {1} KG\n\n<b>Satış Detayları:</b>\n{2}",
                    "Harvested crops in the barn were instantly sold to merchants at a 20% profit margin!\n\n<b>Earned Credits:</b> <color=#00E676>+{0:N0}C</color>\n<b>Total Sold:</b> {1} KG\n\n<b>Sales Details:</b>\n{2}"
                );
                ModalManager.ShowModal(qsTitle, string.Format(qsBodyFmt, totalEarnings, totalKgSold, detailsText), btnGreat);
            }
        }
    }
}
