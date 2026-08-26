using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Palet rafında veya depoda bekleyen satın alınmış mobilyaların listelendiği,
    /// arama yapılabilen ve tek tıkla önizlemeli kuruluma başlanabilen şık modal arayüz.
    /// </summary>
    public class PalletStorageInventoryModalUI : MonoBehaviour
    {
        public static PalletStorageInventoryModalUI Instance { get; private set; }

        public static bool IsModalOpen => currentCanvasObj != null && currentCanvasObj.activeSelf;

        private static GameObject currentCanvasObj;
        private static Font globalFont;

        private string searchQuery = "";
        private InputField searchInputField;
        private Transform cardsContainer;
        private GameObject emptyStateObj;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private bool isWorkshop = false;

        public static void ShowModal(bool isWorkshopMode = false)
        {
            if (globalFont == null)
            {
                globalFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (currentCanvasObj != null)
            {
                Destroy(currentCanvasObj);
                currentCanvasObj = null;
            }

            ModalManager.SetModalOpen(true);

            currentCanvasObj = new GameObject("Pallet_Storage_Inventory_Canvas");
            Canvas canvas = currentCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 940; // Tabletin ve diğer popupların hemen altında/üstünde net öncelik

            CanvasScaler scaler = currentCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            currentCanvasObj.AddComponent<GraphicRaycaster>();

            PalletStorageInventoryModalUI script = currentCanvasObj.AddComponent<PalletStorageInventoryModalUI>();
            script.isWorkshop = isWorkshopMode;
            script.BuildUI(currentCanvasObj.transform);
        }

        public static void HideModal()
        {
            if (currentCanvasObj != null)
            {
                Destroy(currentCanvasObj);
                currentCanvasObj = null;
            }
            ModalManager.SetModalOpen(false);
        }

        private void Update()
        {
            // ESC ile kapatma desteği
            if (WasEscapePressed())
            {
                HideModal();
            }
        }

        private bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                return true;
            return false;
#else
            try { return Input.GetKeyDown(KeyCode.Escape); }
            catch { return false; }
#endif
        }

        private void BuildUI(Transform root)
        {
            // 1. Tam Ekran Karartma ve Tıklama Engelleyici Backdrop
            GameObject backdrop = new GameObject("Modal_Backdrop");
            backdrop.transform.SetParent(root, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.04f, 0.06f, 0.10f, 0.82f);
            bdImg.raycastTarget = true;

            Button bdBtn = backdrop.AddComponent<Button>();
            bdBtn.targetGraphic = bdImg;
            bdBtn.onClick.AddListener(HideModal);

            // 2. Ana Modal Kutusu (Glassmorphism & Neon Kenarlık)
            GameObject boxObj = new GameObject("Pallet_Modal_Box");
            boxObj.transform.SetParent(backdrop.transform, false);

            RectTransform boxRect = boxObj.AddComponent<RectTransform>();
            boxRect.anchoredPosition = Vector2.zero;
            boxRect.sizeDelta = new Vector2(980f, 620f);

            Image boxBg = boxObj.AddComponent<Image>();
            boxBg.sprite = UIStyleUtility.CreateOutlinePillSprite(980, 620, 24, 2, new Color(0.95f, 0.70f, 0.20f), new Color(0.10f, 0.13f, 0.18f, 0.98f));
            boxBg.raycastTarget = true; // Kutu içindeki tıklamaların arkaya geçmesini önle

            // 3. Üst Başlık ve Arama Çubuğu Barı
            BuildHeaderBar(boxObj.transform);

            // 4. Liste Alanı (Scroll View)
            BuildInventoryScrollView(boxObj.transform);

            // 5. Boş Durum Bilgilendirmesi
            BuildEmptyState(boxObj.transform);

            // 6. İlk Liste Yenilemesi
            RefreshInventoryList();
        }

        private void BuildHeaderBar(Transform parent)
        {
            GameObject headerObj = new GameObject("Header_Bar");
            headerObj.transform.SetParent(parent, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 260f);
            hRect.sizeDelta = new Vector2(920f, 60f);

            // Başlık
            GameObject titleObj = new GameObject("Title_Text");
            titleObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(-230f, 0f);
            tRect.sizeDelta = new Vector2(440f, 50f);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = globalFont;
            titleText.text = isWorkshop
                ? LocalizationManager.L("PalletModal_WorkshopTitle", "🏭 ATÖLYE PALETİ (MAKİNELER & HAMMADDELER)", "🏭 WORKSHOP PALLET (MACHINES & RAW CROPS)")
                : LocalizationManager.L("PalletModal_Title", "📦 PALET RAFI MOBİLYA DEPOSU", "📦 PALLET RACK FURNITURE STORAGE");
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = isWorkshop ? new Color(0.95f, 0.65f, 0.20f) : new Color(1.0f, 0.85f, 0.30f);
            titleText.raycastTarget = false;

            // Arama Kutusu (Search Input Field)
            GameObject searchObj = new GameObject("Search_InputField");
            searchObj.transform.SetParent(headerObj.transform, false);
            RectTransform sRect = searchObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(170f, 0f);
            sRect.sizeDelta = new Vector2(280f, 40f);

            Image searchBg = searchObj.AddComponent<Image>();
            searchBg.sprite = UIStyleUtility.CreateOutlinePillSprite(280, 40, 20, 1, new Color(0.25f, 0.40f, 0.55f), new Color(0.14f, 0.18f, 0.24f, 0.90f));
            searchBg.raycastTarget = true;

            searchInputField = searchObj.AddComponent<InputField>();

            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(searchObj.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(16f, 0f);
            phRect.offsetMax = new Vector2(-16f, 0f);

            Text phText = placeholderObj.AddComponent<Text>();
            phText.font = globalFont;
            phText.text = isWorkshop
                ? LocalizationManager.L("PalletModal_SearchWorkshopPlaceholder", "🔍 Makine veya mahsul ara...", "🔍 Search machine or crop...")
                : LocalizationManager.L("PalletModal_SearchPlaceholder", "🔍 Mobilya ara...", "🔍 Search furniture...");
            phText.fontSize = 14;
            phText.fontStyle = FontStyle.Italic;
            phText.color = new Color(0.55f, 0.65f, 0.75f, 0.70f);
            phText.alignment = TextAnchor.MiddleLeft;
            phText.raycastTarget = false;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(searchObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 0f);
            textRect.offsetMax = new Vector2(-16f, 0f);

            Text inputText = textObj.AddComponent<Text>();
            inputText.font = globalFont;
            inputText.fontSize = 14;
            inputText.fontStyle = FontStyle.Bold;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.raycastTarget = false;

            searchInputField.textComponent = inputText;
            searchInputField.placeholder = phText;
            searchInputField.onValueChanged.AddListener((val) => {
                searchQuery = val.Trim().ToLower();
                RefreshInventoryList();
            });

            // Kapat Butonu (✖)
            GameObject closeBtnObj = new GameObject("Close_Button");
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(430f, 0f);
            cRect.sizeDelta = new Vector2(46f, 46f);

            Image closeBg = closeBtnObj.AddComponent<Image>();
            closeBg.sprite = UIStyleUtility.CreateRoundedPillSprite(46, 46, 23, new Color(0.92f, 0.18f, 0.20f, 1f));
            closeBg.raycastTarget = true;

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeBtn.onClick.AddListener(HideModal);

            GameObject closeTxtObj = new GameObject("Close_Txt");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform ctRect = closeTxtObj.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;

            Text closeTxt = closeTxtObj.AddComponent<Text>();
            closeTxt.font = globalFont;
            closeTxt.text = "✖";
            closeTxt.fontSize = 26;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = Color.white;
            closeTxt.raycastTarget = false;

            Outline ctOutline = closeTxtObj.AddComponent<Outline>();
            ctOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            ctOutline.effectDistance = new Vector2(1.5f, -1.5f);

            closeBtnObj.transform.SetAsLastSibling();
        }

        private void BuildInventoryScrollView(Transform parent)
        {
            GameObject scrollObj = new GameObject("Inventory_ScrollView");
            scrollObj.transform.SetParent(parent, false);

            RectTransform sRect = scrollObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, -30f);
            sRect.sizeDelta = new Vector2(920f, 480f);

            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;

            Image sBg = scrollObj.AddComponent<Image>();
            sBg.sprite = UIStyleUtility.CreateRoundedPillSprite(920, 480, 16, new Color(0.07f, 0.09f, 0.13f, 0.70f));
            sBg.raycastTarget = true;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = new Vector2(-10f, -10f);

            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.white;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vLayout = content.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 10f;
            vLayout.padding = new RectOffset(10, 10, 10, 10);
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRect;
            scrollRect.content = cRect;

            cardsContainer = content.transform;
        }

        private void BuildEmptyState(Transform parent)
        {
            emptyStateObj = new GameObject("Empty_State_Panel");
            emptyStateObj.transform.SetParent(parent, false);

            RectTransform esRect = emptyStateObj.AddComponent<RectTransform>();
            esRect.anchoredPosition = new Vector2(0f, -30f);
            esRect.sizeDelta = new Vector2(800f, 320f);

            GameObject iconObj = new GameObject("Empty_Icon");
            iconObj.transform.SetParent(emptyStateObj.transform, false);
            RectTransform iRect = iconObj.AddComponent<RectTransform>();
            iRect.anchoredPosition = new Vector2(0f, 70f);
            iRect.sizeDelta = new Vector2(300f, 60f);

            Text iconText = iconObj.AddComponent<Text>();
            iconText.font = globalFont;
            iconText.text = isWorkshop ? "🏭" : "📦";
            iconText.fontSize = 44;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = new Color(0.70f, 0.75f, 0.85f);
            iconText.raycastTarget = false;

            GameObject descObj = new GameObject("Empty_Desc");
            descObj.transform.SetParent(emptyStateObj.transform, false);
            RectTransform dRect = descObj.AddComponent<RectTransform>();
            dRect.anchoredPosition = new Vector2(0f, 0f);
            dRect.sizeDelta = new Vector2(700f, 60f);

            Text descText = descObj.AddComponent<Text>();
            descText.font = globalFont;
            descText.text = isWorkshop
                ? LocalizationManager.L(
                    "PalletModal_WorkshopEmptyDesc",
                    "Atölye paletinde kurulu olmayan makine kolisi bulunmuyor.\nTabletten Atölye Makineleri sekmesine giderek yeni makineler satın alabilirsiniz.",
                    "No uninstalled machine boxes on the workshop pallet.\nYou can buy new machines from the Workshop tab in Tablet."
                )
                : LocalizationManager.L(
                    "PalletModal_EmptyDesc",
                    "Palet rafında kurulu olmayan mobilya kolisi bulunmuyor.\nEKT Phone TrendyShop uygulamasından dilediğiniz reyon veya mobilyayı sipariş edebilirsiniz.",
                    "No stored furniture boxes on the pallet rack.\nYou can order shelves and furniture anytime via EKT Phone TrendyShop."
                );
            descText.fontSize = 15;
            descText.alignment = TextAnchor.MiddleCenter;
            descText.color = new Color(0.65f, 0.75f, 0.85f);
            descText.raycastTarget = false;

            // TrendyShop'a Git Butonu
            GameObject shopBtnObj = new GameObject("Shop_Btn");
            shopBtnObj.transform.SetParent(emptyStateObj.transform, false);
            RectTransform bRect = shopBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(0f, -70f);
            bRect.sizeDelta = new Vector2(280f, 44f);

            Image bBg = shopBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateOutlinePillSprite(280, 44, 22, 2, new Color(0.95f, 0.70f, 0.20f), new Color(0.22f, 0.16f, 0.05f, 0.95f));
            bBg.raycastTarget = true;

            Button shopBtn = shopBtnObj.AddComponent<Button>();
            shopBtn.targetGraphic = bBg;
            shopBtn.onClick.AddListener(() => {
                HideModal();
                if (EKTPhoneManager.Instance != null)
                {
                    EKTPhoneManager.Instance.OpenTrendyShopApp();
                }
            });

            GameObject stObj = new GameObject("Shop_Txt");
            stObj.transform.SetParent(shopBtnObj.transform, false);
            RectTransform stRect = stObj.AddComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.sizeDelta = Vector2.zero;

            Text shopText = stObj.AddComponent<Text>();
            shopText.font = globalFont;
            shopText.text = isWorkshop
                ? LocalizationManager.L("PalletModal_OpenWorkshopShopBtn", "📱 Atölye Makinelerine Git", "📱 Open Workshop Machines")
                : LocalizationManager.L("PalletModal_OpenShopBtn", "📱 TrendyShop'a Git", "📱 Open TrendyShop");
            shopText.fontSize = 15;
            shopText.fontStyle = FontStyle.Bold;
            shopText.alignment = TextAnchor.MiddleCenter;
            shopText.color = new Color(1.0f, 0.88f, 0.35f);
            shopText.raycastTarget = false;

            emptyStateObj.SetActive(false);
        }

        public void RefreshInventoryList()
        {
            if (cardsContainer == null) return;

            foreach (Transform child in cardsContainer)
            {
                Destroy(child.gameObject);
            }

            if (isWorkshop)
            {
                RefreshWorkshopPalletInventory();
            }
            else
            {
                RefreshFurniturePalletInventory();
            }
        }

        private void RefreshFurniturePalletInventory()
        {
            Dictionary<FurnitureType, int> pendingCounts = (FurnitureDeliveryManager.Instance != null)
                ? FurnitureDeliveryManager.Instance.GetPendingFurnitureCounts()
                : null;

            if (pendingCounts == null || pendingCounts.Count == 0)
            {
                if (emptyStateObj != null) emptyStateObj.SetActive(true);
                return;
            }

            if (emptyStateObj != null) emptyStateObj.SetActive(false);

            int matchingCards = 0;
            foreach (var kvp in pendingCounts)
            {
                FurnitureType fType = kvp.Key;
                int quantity = kvp.Value;
                FurnitureItemDef def = FurnitureDatabase.GetDef(fType);
                if (def == null) continue;

                string locName = def.LocalizedName;
                string locDesc = def.LocalizedDescription;

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    bool matchName = locName.ToLower().Contains(searchQuery);
                    bool matchDesc = locDesc.ToLower().Contains(searchQuery);
                    bool matchType = fType.ToString().ToLower().Contains(searchQuery);
                    if (!matchName && !matchDesc && !matchType) continue;
                }

                matchingCards++;
                CreateFurnitureInventoryCard(fType, def, quantity);
            }

            if (emptyStateObj != null)
            {
                emptyStateObj.SetActive(matchingCards == 0);
            }
        }

        private void RefreshWorkshopPalletInventory()
        {
            Dictionary<FurnitureType, int> pendingMachines = (WorkshopPalletManager.Instance != null)
                ? WorkshopPalletManager.Instance.GetPendingMachineCounts()
                : new Dictionary<FurnitureType, int>();

            Dictionary<string, int> storedCrops = (WorkshopPalletManager.Instance != null)
                ? WorkshopPalletManager.Instance.GetCropInventory()
                : new Dictionary<string, int>();

            int totalPendingMachines = 0;
            if (pendingMachines != null)
            {
                foreach (var kvp in pendingMachines) totalPendingMachines += kvp.Value;
            }

            int totalKg = (WorkshopPalletManager.Instance != null)
                ? WorkshopPalletManager.Instance.GetTotalStoredAmount()
                : 0;

            bool isTotallyEmpty = totalPendingMachines == 0 && totalKg == 0;
            if (isTotallyEmpty && string.IsNullOrEmpty(searchQuery))
            {
                if (emptyStateObj != null) emptyStateObj.SetActive(true);
                return;
            }

            if (emptyStateObj != null) emptyStateObj.SetActive(false);

            // ================= 1. KATEGORİ: 🏭 KURULMAYI BEKLEYEN MAKİNELER =================
            string machHeaderTitle = LocalizationManager.L("Pallet_Section_Machines", "🏭 KURULMAYI BEKLEYEN MAKİNE KOLİLERİ", "🏭 UNINSTALLED MACHINE BOXES");
            CreateSectionHeader($"{machHeaderTitle} ({totalPendingMachines} {LocalizationManager.L("Unit_Pieces", "Adet", "Pcs")})", new Color(0.95f, 0.65f, 0.20f));

            int matchingMachines = 0;
            if (pendingMachines != null && pendingMachines.Count > 0)
            {
                foreach (var kvp in pendingMachines)
                {
                    FurnitureType fType = kvp.Key;
                    int quantity = kvp.Value;
                    FurnitureItemDef def = FurnitureDatabase.GetDef(fType);
                    if (def == null) continue;

                    string locName = def.LocalizedName;
                    string locDesc = def.LocalizedDescription;

                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        bool matchName = locName.ToLower().Contains(searchQuery);
                        bool matchDesc = locDesc.ToLower().Contains(searchQuery);
                        bool matchType = fType.ToString().ToLower().Contains(searchQuery);
                        if (!matchName && !matchDesc && !matchType) continue;
                    }

                    matchingMachines++;
                    CreateFurnitureInventoryCard(fType, def, quantity);
                }
            }

            if (matchingMachines == 0 && string.IsNullOrEmpty(searchQuery))
            {
                CreateEmptyNoticeCard(LocalizationManager.L("Pallet_NoPendingMachinesNotice", "✅ Palette bekleyen makine kolisi yok. (Satın aldığınız tüm makineler kuruldu).", "✅ No pending machine boxes on the pallet. (All purchased machines are installed)."));
            }

            // ================= 2. KATEGORİ: 🌾 İŞLENMEYE HAZIR HAMMADDE MAHSULLER =================
            string cropHeaderTitle = LocalizationManager.L("Pallet_Section_Crops", "🌾 İŞLENMEYE HAZIR HAMMADDE MAHSULLER", "🌾 RAW CROPS READY FOR PROCESSING");
            CreateSectionHeader($"{cropHeaderTitle} ({totalKg} KG)", new Color(0.35f, 0.85f, 0.40f));

            int matchingCrops = 0;
            if (storedCrops != null && storedCrops.Count > 0)
            {
                foreach (var kvp in storedCrops)
                {
                    string cropId = kvp.Key;
                    int count = kvp.Value;
                    if (count <= 0) continue;

                    GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(cropId);
                    string cropName = (sDef != null) ? sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "") : cropId;

                    if (!string.IsNullOrEmpty(searchQuery))
                    {
                        bool matchName = cropName.ToLower().Contains(searchQuery);
                        bool matchId = cropId.ToLower().Contains(searchQuery);
                        if (!matchName && !matchId) continue;
                    }

                    matchingCrops++;
                    CreateWorkshopCropRow(cropId, count);
                }
            }

            if (matchingCrops == 0 && string.IsNullOrEmpty(searchQuery))
            {
                CreateEmptyNoticeCard(LocalizationManager.L("Pallet_NoCropsNotice", "📦 Palette henüz hammadde mahsulü bulunmuyor. Ahır menüsündeki 'TÜMÜNÜ ATÖLYEYE GÖNDER' butonu ile aktarabilirsiniz.", "📦 No raw crops on the pallet yet. Transfer crops from Barn menu using 'SHIP ALL TO WORKSHOP' button."));
            }

            if (matchingMachines == 0 && matchingCrops == 0 && !string.IsNullOrEmpty(searchQuery))
            {
                CreateEmptyNoticeCard(string.Format(LocalizationManager.L("Pallet_NoSearchMatch", "🔍 \"{0}\" aramasına uygun makine veya mahsul bulunamadı.", "🔍 No machines or crops found matching \"{0}\"."), searchQuery));
            }
        }

        private void CreateSectionHeader(string title, Color accentColor)
        {
            GameObject headerObj = new GameObject("SectionHeader");
            headerObj.transform.SetParent(cardsContainer, false);

            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(890f, 38f);

            LayoutElement elem = headerObj.AddComponent<LayoutElement>();
            elem.minHeight = 38f;
            elem.preferredHeight = 38f;

            Image hBg = headerObj.AddComponent<Image>();
            hBg.sprite = UIStyleUtility.CreateOutlinePillSprite(890, 38, 10, 1, accentColor, new Color(0.08f, 0.12f, 0.18f, 0.95f));

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(headerObj.transform, false);
            RectTransform tRect = tObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(16f, 0f);
            tRect.offsetMax = new Vector2(-16f, 0f);

            Text txt = tObj.AddComponent<Text>();
            txt.font = globalFont;
            txt.text = $"<b>{title}</b>";
            txt.fontSize = 15;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = accentColor;
            txt.raycastTarget = false;
        }

        private void CreateEmptyNoticeCard(string message)
        {
            GameObject emptyObj = new GameObject("EmptyNoticeCard");
            emptyObj.transform.SetParent(cardsContainer, false);

            RectTransform eRect = emptyObj.AddComponent<RectTransform>();
            eRect.sizeDelta = new Vector2(890f, 48f);

            LayoutElement elem = emptyObj.AddComponent<LayoutElement>();
            elem.minHeight = 48f;
            elem.preferredHeight = 48f;

            Image eBg = emptyObj.AddComponent<Image>();
            eBg.sprite = UIStyleUtility.CreateRoundedPillSprite(890, 48, 10, new Color(0.08f, 0.10f, 0.14f, 0.85f));

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(emptyObj.transform, false);
            RectTransform tRect = tObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = tObj.AddComponent<Text>();
            txt.font = globalFont;
            txt.text = message;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Italic;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.65f, 0.72f, 0.80f);
            txt.raycastTarget = false;
        }

        private void CreateWorkshopCropRow(string cropId, int count)
        {
            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(cropId);
            string cropName = (sDef != null) ? sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "") : cropId;
            string emoji = (sDef != null) ? sDef.iconEmoji : "📦";

            GameObject rowObj = new GameObject("CropRow_" + cropId);
            rowObj.transform.SetParent(cardsContainer, false);

            RectTransform rRect = rowObj.AddComponent<RectTransform>();
            rRect.sizeDelta = new Vector2(890f, 64f);

            LayoutElement elem = rowObj.AddComponent<LayoutElement>();
            elem.minHeight = 64f;
            elem.preferredHeight = 64f;

            Image rBg = rowObj.AddComponent<Image>();
            rBg.sprite = UIStyleUtility.CreateOutlinePillSprite(890, 64, 12, 1, new Color(0.30f, 0.70f, 0.40f, 0.7f), new Color(0.12f, 0.16f, 0.22f, 0.95f));

            // Sol İkon Kutusu
            GameObject iconObj = new GameObject("Icon_Box");
            iconObj.transform.SetParent(rowObj.transform, false);
            RectTransform iRect = iconObj.AddComponent<RectTransform>();
            iRect.anchoredPosition = new Vector2(-390f, 0f);
            iRect.sizeDelta = new Vector2(48f, 48f);

            Image iconBg = iconObj.AddComponent<Image>();
            iconBg.sprite = UIStyleUtility.CreateSeedIconSprite(cropId, emoji, new Color(0.35f, 0.85f, 0.40f));
            iconBg.raycastTarget = false;

            // Orta Bilgi Metni
            GameObject infoObj = new GameObject("Info_Panel");
            infoObj.transform.SetParent(rowObj.transform, false);
            RectTransform inRect = infoObj.AddComponent<RectTransform>();
            inRect.anchoredPosition = new Vector2(-75f, 0f);
            inRect.sizeDelta = new Vector2(530f, 52f);

            Text infoTxt = infoObj.AddComponent<Text>();
            infoTxt.font = globalFont;
            string subText = LocalizationManager.L("PalletModal_CropSubtitle", "Atölye Makinelerinde Kullanıma Hazır Depolanan Hammadde", "Raw Material Ready for Workshop Machine Processing");
            infoTxt.text = $"<b>{emoji} {cropName}</b>  <color=#00E676><b>[Hammadde]</b></color>\n<size=13><color=#85A5B8>{subText}</color></size>";
            infoTxt.fontSize = 15;
            infoTxt.alignment = TextAnchor.MiddleLeft;
            infoTxt.color = Color.white;
            infoTxt.raycastTarget = false;

            // Sağ KG Göstergesi
            GameObject countObj = new GameObject("Count_Panel");
            countObj.transform.SetParent(rowObj.transform, false);
            RectTransform cRect = countObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(345f, 0f);
            cRect.sizeDelta = new Vector2(150f, 44f);

            Image cBg = countObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateOutlinePillSprite(150, 44, 10, 1, new Color(0.95f, 0.65f, 0.20f), new Color(0.20f, 0.16f, 0.08f, 0.90f));

            GameObject ctObj = new GameObject("Count_Txt");
            ctObj.transform.SetParent(countObj.transform, false);
            RectTransform ctRect = ctObj.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;

            Text cTxt = ctObj.AddComponent<Text>();
            cTxt.font = globalFont;
            cTxt.text = $"<color=#FFD700><b>{count} KG</b></color>";
            cTxt.fontSize = 17;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.raycastTarget = false;
        }

        private void CreateFurnitureInventoryCard(FurnitureType fType, FurnitureItemDef def, int quantity)
        {
            GameObject cardObj = new GameObject("Pallet_Card_" + fType);
            cardObj.transform.SetParent(cardsContainer, false);

            RectTransform cRect = cardObj.AddComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(890f, 76f);

            LayoutElement elem = cardObj.AddComponent<LayoutElement>();
            elem.minHeight = 76f;
            elem.preferredHeight = 76f;

            Image bg = cardObj.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(890, 76, 14, new Color(0.12f, 0.16f, 0.22f, 0.95f));
            bg.raycastTarget = true;

            // 1. İkon Kutusu (TrendyShop Mağaza Görseli)
            GameObject iconObj = new GameObject("Icon_Box");
            iconObj.transform.SetParent(cardObj.transform, false);
            RectTransform iRect = iconObj.AddComponent<RectTransform>();
            iRect.anchoredPosition = new Vector2(-390f, 0f);
            iRect.sizeDelta = new Vector2(56f, 56f);

            Image iconBg = iconObj.AddComponent<Image>();
            iconBg.sprite = UIStyleUtility.CreateFurnitureIconSprite(def.type);
            iconBg.raycastTarget = false;

            // 2. Mobilya Bilgi Metinleri
            GameObject infoObj = new GameObject("Info_Panel");
            infoObj.transform.SetParent(cardObj.transform, false);
            RectTransform inRect = infoObj.AddComponent<RectTransform>();
            inRect.anchoredPosition = new Vector2(-75f, 0f);
            inRect.sizeDelta = new Vector2(530f, 60f);

            Text infoTxt = infoObj.AddComponent<Text>();
            infoTxt.font = globalFont;
            string zoneColorHex = (def.zone == FurnitureZone.WorkshopOnly) ? "#FFA726" : ((def.zone == FurnitureZone.StorageOnly) ? "#FFB03A" : ((def.zone == FurnitureZone.StoreAndStorage) ? "#50E3C2" : "#54D6FF"));
            string zoneTag = $"<color={zoneColorHex}><b>[{def.GetZoneText()}]</b></color>";
            string qtyTag = (quantity > 1) ? $" <color=#FFD700><b>(x{quantity} Adet)</b></color>" : "";

            infoTxt.text = $"<b>{def.LocalizedName}</b>{qtyTag}  |  {zoneTag}\n<size=13><color=#90A0B5>{def.LocalizedDescription}</color></size>";
            infoTxt.fontSize = 15;
            infoTxt.alignment = TextAnchor.MiddleLeft;
            infoTxt.color = Color.white;
            infoTxt.raycastTarget = false;

            // 3. KUR (ASSEMBLE) Butonu
            GameObject assembleBtnObj = new GameObject("Btn_Assemble");
            assembleBtnObj.transform.SetParent(cardObj.transform, false);

            RectTransform bRect = assembleBtnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(345f, 0f);
            bRect.sizeDelta = new Vector2(150f, 44f);

            Image bBg = assembleBtnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateOutlinePillSprite(150, 44, 22, 2, new Color(0.20f, 0.85f, 0.40f), new Color(0.12f, 0.42f, 0.22f, 0.95f));
            bBg.raycastTarget = true;

            Button btn = assembleBtnObj.AddComponent<Button>();
            btn.targetGraphic = bBg;

            var targetType = fType;
            btn.onClick.AddListener(() => {
                // Modalı temizce kapat
                HideModal();

                // İlgili koliyi bul veya yerleştirmeyi başlat
                DeliveryBoxController box = null;
                if (!isWorkshop && FurnitureDeliveryManager.Instance != null)
                {
                    box = FurnitureDeliveryManager.Instance.GetFirstBoxOfType(targetType);
                }

                if (FurniturePlacementManager.Instance != null)
                {
                    FurniturePlacementManager.Instance.StartPlacement(targetType, box);
                }
            });

            GameObject btObj = new GameObject("Btn_Txt");
            btObj.transform.SetParent(assembleBtnObj.transform, false);
            RectTransform btRect = btObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;

            Text btnTxt = btObj.AddComponent<Text>();
            btnTxt.font = globalFont;
            btnTxt.text = LocalizationManager.L("Btn_AssembleFurniture", "🔨 KUR", "🔨 ASSEMBLE");
            btnTxt.fontSize = 15;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;
            btnTxt.raycastTarget = false;
        }
    }
}
