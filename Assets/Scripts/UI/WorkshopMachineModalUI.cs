using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Atölye makinelerine tıklandığında açılan ve tarif seçimi, hammadde kontrolü,
    /// gerçek zamanlı geri sayım takibi ve üretim başlatma işlemlerini yöneten modal arayüz.
    /// </summary>
    public class WorkshopMachineModalUI : MonoBehaviour
    {
        public static WorkshopMachineModalUI Instance { get; private set; }

        private GameObject canvasObj;
        private WorkshopMachineController activeMachine;
        private Transform listContentTransform;
        private Text statusHeaderText;
        private Text machineTitleText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
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

        private void HandleLanguageChanged(GameLanguage language)
        {
            if (canvasObj != null && activeMachine != null) BuildUI();
        }

        private void Update()
        {
            if (canvasObj != null && activeMachine != null && activeMachine.isProducing)
            {
                UpdateLiveStatusHeader();
            }
        }

        public static void ShowModal(WorkshopMachineController machine)
        {
            if (Instance == null)
            {
                GameObject host = new GameObject("WorkshopMachineModalUI_Host");
                Instance = host.AddComponent<WorkshopMachineModalUI>();
            }

            Instance.OpenModalInternal(machine);
        }

        private void OpenModalInternal(WorkshopMachineController machine)
        {
            activeMachine = machine;
            if (activeMachine == null) return;

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
            activeMachine = null;
            ModalManager.SetModalOpen(false);
        }

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);

            canvasObj = new GameObject("WorkshopMachineModal_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 960;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Font font = UIStyleUtility.GetGlobalFont(16);

            // 1. Arka Plan Karartma (Dışına tıklayınca kapat)
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

            // 2. Ana Modal Paneli (900x700)
            GameObject panelObj = new GameObject("Modal_Panel");
            panelObj.transform.SetParent(backdrop.transform, false);
            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(900f, 700f);

            Image pBg = panelObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(900, 700, 18, 3, new Color(0.95f, 0.65f, 0.15f), new Color(0.12f, 0.15f, 0.20f, 0.98f));
            pBg.raycastTarget = true;

            WorkshopMachineDef mDef = WorkshopMachineDatabase.GetMachineByType(activeMachine.machineType);
            string mTitle = mDef != null ? $"{mDef.iconEmoji} {mDef.LocalizedName}" : LocalizationManager.L("WS_MachineFallback", "🏭 Atölye Makinesi", "🏭 Workshop Machine");

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(-40f, 300f);
            tRect.sizeDelta = new Vector2(720f, 48f);

            machineTitleText = titleObj.AddComponent<Text>();
            machineTitleText.font = font;
            machineTitleText.text = mTitle;
            machineTitleText.fontSize = 25;
            machineTitleText.fontStyle = FontStyle.Bold;
            machineTitleText.color = new Color(1.0f, 0.85f, 0.30f);
            machineTitleText.alignment = TextAnchor.MiddleLeft;

            // Kapat Butonu (✖)
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(panelObj.transform, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchoredPosition = new Vector2(405f, 300f);
            clRect.sizeDelta = new Vector2(48f, 48f);

            Image clBg = closeObj.AddComponent<Image>();
            clBg.sprite = UIStyleUtility.CreateRoundedPillSprite(48, 48, 24, new Color(0.92f, 0.18f, 0.20f, 1f));

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
            clTxt.fontSize = 26;
            clTxt.fontStyle = FontStyle.Bold;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;

            // 3. Durum Bilgi Şeridi
            GameObject statusObj = new GameObject("StatusBar");
            statusObj.transform.SetParent(panelObj.transform, false);
            RectTransform sRect = statusObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, 245f);
            sRect.sizeDelta = new Vector2(840f, 44f);

            Image sBg = statusObj.AddComponent<Image>();
            sBg.sprite = UIStyleUtility.CreateOutlinePillSprite(840, 44, 14, 1, new Color(0.25f, 0.40f, 0.55f), new Color(0.14f, 0.18f, 0.24f, 0.95f));

            GameObject stObj = new GameObject("StatusText");
            stObj.transform.SetParent(statusObj.transform, false);
            RectTransform stRect = stObj.AddComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;

            statusHeaderText = stObj.AddComponent<Text>();
            statusHeaderText.font = font;
            statusHeaderText.fontSize = 17;
            statusHeaderText.fontStyle = FontStyle.Bold;
            statusHeaderText.alignment = TextAnchor.MiddleCenter;
            statusHeaderText.color = Color.white;

            UpdateLiveStatusHeader();

            // 4. ScrollView ve Liste
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            RectTransform sAreaRect = scrollObj.AddComponent<RectTransform>();
            sAreaRect.anchoredPosition = new Vector2(0f, -25f);
            sAreaRect.sizeDelta = new Vector2(840f, 470f);

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
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cntRect;
            scrollRect.viewport = vpRect;
            listContentTransform = content.transform;

            PopulateRecipesList(font);
        }

        private void UpdateLiveStatusHeader()
        {
            if (statusHeaderText == null || activeMachine == null) return;

            if (activeMachine.isReadyToCollect)
            {
                WorkshopRecipeDef r = WorkshopMachineDatabase.GetRecipeById(activeMachine.activeRecipeId);
                string rName = r != null ? r.LocalizedName : LocalizationManager.L("WS_GourmetFallback", "Gurme Ürün", "Gourmet Product");
                string doneFmt = LocalizationManager.L("WS_Header_DoneFmt", "🎉 {0} Üretimi Tamamlandı! (Toplanmaya Hazır)", "🎉 {0} Craft Completed! (Ready to Collect)");
                statusHeaderText.text = $"<color=#00E676><b>{string.Format(doneFmt, rName)}</b></color>";
            }
            else if (activeMachine.isProducing)
            {
                WorkshopRecipeDef r = WorkshopMachineDatabase.GetRecipeById(activeMachine.activeRecipeId);
                string rName = r != null ? r.LocalizedName : LocalizationManager.L("WS_ProductionFallback", "Üretim", "Production");
                int mins = Mathf.FloorToInt(activeMachine.remainingSeconds / 60f);
                int secs = Mathf.FloorToInt(activeMachine.remainingSeconds % 60f);
                float pct = 1f - (activeMachine.remainingSeconds / Mathf.Max(1f, activeMachine.totalDuration));
                string progFmt = LocalizationManager.L("WS_Header_ProgFmt", "⏳ <b>{0}</b> Üretiliyor... <color=#80D8FF><b>Kalan: {1:00}:{2:00} (%{3})</b></color>", "⏳ <b>{0}</b> Crafting... <color=#80D8FF><b>Remaining: {1:00}:{2:00} ({3}%)</b></color>");
                statusHeaderText.text = string.Format(progFmt, rName, mins, secs, Mathf.RoundToInt(pct * 100));
            }
            else
            {
                statusHeaderText.text = LocalizationManager.L("WS_Header_SelectRecipe", "📋 Üretim yapmak istediğiniz gurme ürünü seçin:", "📋 Choose a gourmet recipe to produce:");
            }
        }

        private void PopulateRecipesList(Font font)
        {
            if (listContentTransform == null || activeMachine == null) return;

            foreach (Transform child in listContentTransform) Destroy(child.gameObject);

            // Eğer makine üretiyorsa veya hazırsa tek büyük durum kartı göster
            if (activeMachine.isReadyToCollect || activeMachine.isProducing)
            {
                RenderActiveProductionCard(font);
                return;
            }

            // Makine boştaysa: Bu makinenin tüm tariflerini listele
            List<WorkshopRecipeDef> recipes = WorkshopMachineDatabase.GetRecipesForMachine(activeMachine.machineType);

            foreach (var recipe in recipes)
            {
                WorkshopRecipeDef rDef = recipe;
                int palletCropCount = (WorkshopPalletManager.Instance != null) ? WorkshopPalletManager.Instance.GetCropCount(rDef.cropId) : 0;
                bool hasEnoughCrops = palletCropCount >= rDef.requiredCropKg;

                GardenSeedDef seedDef = GardenSeedDatabase.GetSeedById(rDef.cropId);
                string cropName = seedDef != null ? seedDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "") : rDef.cropId;

                GameObject cardObj = new GameObject("RecipeCard_" + rDef.recipeId);
                cardObj.transform.SetParent(listContentTransform, false);

                LayoutElement lElem = cardObj.AddComponent<LayoutElement>();
                lElem.minHeight = 104f;
                lElem.preferredHeight = 104f;

                Image cardBg = cardObj.AddComponent<Image>();
                Color borderColor = hasEnoughCrops ? new Color(0.20f, 0.85f, 0.40f) : new Color(0.40f, 0.45f, 0.50f);
                cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 104, 16, 2, borderColor, new Color(0.14f, 0.17f, 0.23f, 0.95f));

                // 1. Sol Emoji Kutusu (Büyük ve Belirgin)
                GameObject iconBox = new GameObject("IconBox");
                iconBox.transform.SetParent(cardObj.transform, false);
                RectTransform ibRect = iconBox.AddComponent<RectTransform>();
                ibRect.anchoredPosition = new Vector2(-345f, 0f);
                ibRect.sizeDelta = new Vector2(66f, 66f);

                Image ibBg = iconBox.AddComponent<Image>();
                ibBg.sprite = UIStyleUtility.CreateRoundedPillSprite(66, 66, 14, new Color(0.22f, 0.28f, 0.38f));

                GameObject iconTxtObj = new GameObject("Emoji");
                iconTxtObj.transform.SetParent(iconBox.transform, false);
                RectTransform itRect = iconTxtObj.AddComponent<RectTransform>();
                itRect.anchorMin = Vector2.zero;
                itRect.anchorMax = Vector2.one;

                Text iconTxt = iconTxtObj.AddComponent<Text>();
                iconTxt.font = font;
                iconTxt.text = rDef.iconEmoji;
                iconTxt.fontSize = 36;
                iconTxt.alignment = TextAnchor.MiddleCenter;

                // 2. Orta Bilgiler (Ürün Adı, Gerekli Hammadde vs Paletteki Stok, Süre, Satış Değeri)
                GameObject infoObj = new GameObject("Info");
                infoObj.transform.SetParent(cardObj.transform, false);
                RectTransform infRect = infoObj.AddComponent<RectTransform>();
                infRect.anchoredPosition = new Vector2(-40f, 0f);
                infRect.sizeDelta = new Vector2(500f, 85f);

                Text infTxt = infoObj.AddComponent<Text>();
                infTxt.font = font;
                int durationMins = Mathf.CeilToInt(rDef.durationSeconds / 60f);
                string stockColor = hasEnoughCrops ? "#00E676" : "#FF5252";

                string rawFmt = LocalizationManager.L("WS_Card_RawFmt", "🌾 Gerekli: <b>{0} KG {1}</b> | Palette: <color={2}><b>{3} KG</b></color>", "🌾 Required: <b>{0} KG {1}</b> | Pallet: <color={2}><b>{3} KG</b></color>");
                string timeFmt = LocalizationManager.L("WS_Card_TimeFmt", "⏳ Süre: <b>{0} dk</b> | 💰 Değer: <color=#FFD700><b>{1}C</b></color> (Paket: {2} Adet)", "⏳ Time: <b>{0} min</b> | 💰 Value: <color=#FFD700><b>{1}C</b></color> (Pack: {2} Pcs)");

                string infoStr = $"<size=20><b>{rDef.LocalizedName}</b></size>\n" +
                                 string.Format(rawFmt, rDef.requiredCropKg, cropName, stockColor, palletCropCount) + "\n" +
                                 string.Format(timeFmt, durationMins, rDef.unitSalePrice, rDef.outputPackCount);

                infTxt.text = infoStr;
                infTxt.fontSize = 15;
                infTxt.alignment = TextAnchor.MiddleLeft;
                infTxt.color = Color.white;

                // 3. Sağ "ÜRETİME BAŞLA" Butonu (Genişletilmiş ve Büyütülmüş)
                GameObject startBtnObj = new GameObject("StartBtn");
                startBtnObj.transform.SetParent(cardObj.transform, false);
                RectTransform sbRect = startBtnObj.AddComponent<RectTransform>();
                sbRect.anchoredPosition = new Vector2(300f, 0f);
                sbRect.sizeDelta = new Vector2(175f, 54f);

                Image sbBg = startBtnObj.AddComponent<Image>();
                Color btnColor = hasEnoughCrops ? new Color(0.18f, 0.75f, 0.35f) : new Color(0.32f, 0.36f, 0.42f);
                sbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(175, 54, 12, btnColor);

                Button sbBtn = startBtnObj.AddComponent<Button>();
                sbBtn.targetGraphic = sbBg;
                sbBtn.interactable = hasEnoughCrops;

                string currentRecipeId = rDef.recipeId;
                sbBtn.onClick.AddListener(() => {
                    if (activeMachine != null && activeMachine.StartProduction(currentRecipeId))
                    {
                        HideModal();
                    }
                });

                GameObject sbtObj = new GameObject("BtnText");
                sbtObj.transform.SetParent(startBtnObj.transform, false);
                RectTransform sbtRect = sbtObj.AddComponent<RectTransform>();
                sbtRect.anchorMin = Vector2.zero;
                sbtRect.anchorMax = Vector2.one;

                Text sbtTxt = sbtObj.AddComponent<Text>();
                sbtTxt.font = font;
                sbtTxt.text = hasEnoughCrops ? LocalizationManager.L("WS_BtnStart", "⚙️ ÜRETİME BAŞLA", "⚙️ START CRAFT") : LocalizationManager.L("WS_BtnNoStock", "❌ YETERSİZ HAMMADDE", "❌ NO CROPS");
                sbtTxt.fontSize = 14;
                sbtTxt.fontStyle = FontStyle.Bold;
                sbtTxt.alignment = TextAnchor.MiddleCenter;
                sbtTxt.color = Color.white;
            }
        }

        private void RenderActiveProductionCard(Font font)
        {
            WorkshopRecipeDef rDef = WorkshopMachineDatabase.GetRecipeById(activeMachine.activeRecipeId);
            if (rDef == null) return;

            GameObject cardObj = new GameObject("ActiveStatusCard");
            cardObj.transform.SetParent(listContentTransform, false);

            LayoutElement lElem = cardObj.AddComponent<LayoutElement>();
            lElem.minHeight = 330f;
            lElem.preferredHeight = 330f;

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = UIStyleUtility.CreateOutlinePillSprite(820, 330, 16, 2, new Color(0.95f, 0.70f, 0.20f), new Color(0.14f, 0.17f, 0.24f, 0.98f));

            // Büyük Emoji
            GameObject emojiObj = new GameObject("BigEmoji");
            emojiObj.transform.SetParent(cardObj.transform, false);
            RectTransform eRect = emojiObj.AddComponent<RectTransform>();
            eRect.anchoredPosition = new Vector2(0f, 95f);
            eRect.sizeDelta = new Vector2(120f, 90f);

            Text eTxt = emojiObj.AddComponent<Text>();
            eTxt.font = font;
            eTxt.text = rDef.iconEmoji;
            eTxt.fontSize = 64;
            eTxt.alignment = TextAnchor.MiddleCenter;

            // Açıklama Metni
            GameObject descObj = new GameObject("StatusDesc");
            descObj.transform.SetParent(cardObj.transform, false);
            RectTransform dRect = descObj.AddComponent<RectTransform>();
            dRect.anchoredPosition = new Vector2(0f, 15f);
            dRect.sizeDelta = new Vector2(720f, 65f);

            Text dTxt = descObj.AddComponent<Text>();
            dTxt.font = font;
            dTxt.fontSize = 20;
            dTxt.alignment = TextAnchor.MiddleCenter;
            dTxt.color = Color.white;

            if (activeMachine.isReadyToCollect)
            {
                string readyFmt = LocalizationManager.L("WS_Card_ReadyDesc", "🎉 <b>{0}</b> üretimi tamamlandı!\nToplam <b>{1} Adet</b> gurme ürün toplanmayı bekliyor.", "🎉 <b>{0}</b> crafting complete!\nA total of <b>{1} pcs</b> gourmet goods are ready to collect.");
                dTxt.text = string.Format(readyFmt, rDef.LocalizedName, rDef.outputPackCount);

                // Yeşil Ahıra Topla Butonu
                GameObject collectBtnObj = new GameObject("CollectBtn");
                collectBtnObj.transform.SetParent(cardObj.transform, false);
                RectTransform cbRect = collectBtnObj.AddComponent<RectTransform>();
                cbRect.anchoredPosition = new Vector2(0f, -80f);
                cbRect.sizeDelta = new Vector2(320f, 58f);

                Image cbBg = collectBtnObj.AddComponent<Image>();
                cbBg.sprite = UIStyleUtility.CreateRoundedPillSprite(320, 58, 14, new Color(0.10f, 0.85f, 0.35f));

                Button cbBtn = collectBtnObj.AddComponent<Button>();
                cbBtn.targetGraphic = cbBg;
                cbBtn.onClick.AddListener(() => {
                    if (activeMachine != null)
                    {
                        activeMachine.CollectFinishedProduct();
                        HideModal();
                    }
                });

                GameObject cbtObj = new GameObject("CollectText");
                cbtObj.transform.SetParent(collectBtnObj.transform, false);
                RectTransform cbtRect = cbtObj.AddComponent<RectTransform>();
                cbtRect.anchorMin = Vector2.zero;
                cbtRect.anchorMax = Vector2.one;

                Text cbtTxt = cbtObj.AddComponent<Text>();
                cbtTxt.font = font;
                cbtTxt.text = LocalizationManager.L("WS_Btn_CollectBarn", "📦 ÜRÜNLERİ AHIRA TOPLA", "📦 COLLECT TO BARN");
                cbtTxt.fontSize = 18;
                cbtTxt.fontStyle = FontStyle.Bold;
                cbtTxt.alignment = TextAnchor.MiddleCenter;
                cbtTxt.color = Color.white;
            }
            else
            {
                int mins = Mathf.FloorToInt(activeMachine.remainingSeconds / 60f);
                int secs = Mathf.FloorToInt(activeMachine.remainingSeconds % 60f);
                string procFmt = LocalizationManager.L("WS_Card_ProcDesc", "⚙️ <b>{0}</b> şu anda işleniyor...\nKalan Süre: <color=#00E676><b>{1:00}:{2:00}</b></color> (Paket: {3} Adet | Değer: {4}C / Adet)", "⚙️ <b>{0}</b> is currently processing...\nRemaining: <color=#00E676><b>{1:00}:{2:00}</b></color> (Pack: {3} Pcs | Value: {4}C / Pc)");
                dTxt.text = string.Format(procFmt, rDef.LocalizedName, mins, secs, rDef.outputPackCount, rDef.unitSalePrice);
            }
        }
    }
}
