using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Farm2Shelf.Core;
using Farm2Shelf.UI;

namespace Farm2Shelf.Environment
{
    public enum PlotState
    {
        Empty,
        PlantedSprout,       // 1. Aşama (Filiz)
        Growing,             // 2. Aşama (Büyüyen Bitki)
        RipeReadyToHarvest,  // 3. Aşama (Olgun Hasat Hazır)
        SpoiledTrash         // Çöp (1 gün biçilmeyince)
    }

    public class FieldPlotController : MonoBehaviour
    {
        public PlotState State { get; private set; } = PlotState.Empty;
        public string PlantedSeedId { get; private set; } = "";
        public int CurrentGrowthDay { get; private set; } = 0;
        public int TotalGrowthDays { get; private set; } = 1;
        public bool NeedsWater { get; private set; } = false;
        public bool WateredToday { get; private set; } = false;

        private GameObject cropMeshObj;
        private GameObject floatingIconCanvas;
        private Image floatingIconImage;
        private Text floatingIconText;

        private static GameObject radialMenuCanvasObj;
        public static bool IsRadialMenuOpen => radialMenuCanvasObj != null;

        private void Start()
        {
            if (GetComponent<Collider>() == null)
            {
                BoxCollider col = gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.2f, 0f);
                col.size = new Vector3(2.2f, 0.4f, 2.2f);
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateUpdated += HandleDateUpdated;
            }

            CreateFloatingIconUI();
            UpdateVisuals();
        }

        public void RestoreCropState(string seedId, int curDay, int totalDays, bool needsWater, bool wateredToday, string stateName)
        {
            this.PlantedSeedId = seedId;
            this.CurrentGrowthDay = curDay;
            this.TotalGrowthDays = Mathf.Max(1, totalDays);
            this.NeedsWater = needsWater;
            this.WateredToday = wateredToday;
            if (System.Enum.TryParse<PlotState>(stateName, out PlotState parsedState))
            {
                this.State = parsedState;
            }
            UpdateVisuals();
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateUpdated -= HandleDateUpdated;
            }
        }

        private void Update()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || IsRadialMenuOpen)
            {
                // UI açıkken tarlaya tıklamayı engelle
            }
            else if (WasPointerPressedThisFrame() || Farm2Shelf.Utils.TouchInputHelper.IsCleanTapThisFrame(out _))
            {
                if (!IsPointerOverUIButton())
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        Vector2 pointerPos = GetPointerPosition();
                        if (pointerPos != Vector2.zero)
                        {
                            Ray ray = mainCam.ScreenPointToRay(pointerPos);
                            RaycastHit[] hits = Physics.RaycastAll(ray, 150f);
                            if (hits != null && hits.Length > 0)
                            {
                                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                                foreach (var h in hits)
                                {
                                    if (h.collider == null) continue;
                                    FieldPlotController plot = h.collider.GetComponentInParent<FieldPlotController>();
                                    if (plot == null) plot = h.collider.GetComponent<FieldPlotController>();

                                    if (plot != null)
                                    {
                                        plot.OnPlotClicked();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (floatingIconCanvas != null && Camera.main != null)
            {
                floatingIconCanvas.transform.rotation = Camera.main.transform.rotation;
                float bobY = 0.65f + Mathf.Sin(Time.time * 3.2f) * 0.08f;
                floatingIconCanvas.transform.position = transform.position + new Vector3(0f, bobY, 0f);
            }
        }

        public void OnPlotClicked()
        {
            switch (State)
            {
                case PlotState.Empty:
                    OpenRadialPlantingMenu();
                    break;

                case PlotState.PlantedSprout:
                case PlotState.Growing:
                case PlotState.RipeReadyToHarvest:
                case PlotState.SpoiledTrash:
                    ShowPlotInfoModal();
                    break;
            }
        }

        public void ShowPlotInfoModal()
        {
            if (State == PlotState.SpoiledTrash)
            {
                ModalManager.ShowConfirmModal(
                    "🗑️ ÇÜRÜMÜŞ EKİN BİLGİSİ",
                    "Bu ekim zamanında biçilmediği için çürümüştür ve maalesef çöp olmuştur.\n\nToprağı temizleyerek yeniden tohum ekebilirsiniz.",
                    () => { ClearSpoiledPlot(); },
                    "🗑️ Temizle",
                    "Kapat"
                );
                return;
            }

            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(PlantedSeedId);
            if (sDef == null) return;

            int remainingDays = Mathf.Max(0, TotalGrowthDays - CurrentGrowthDay);
            string seasonStr = TimeManager.Instance != null ? TimeManager.Instance.GetLocalizedSeasonName(sDef.season) : sDef.season.ToString();

            string waterStatusStr = NeedsWater
                ? LocalizationManager.L("Water_Pending", "<color=#FF5252><b>💧 Su Bekliyor! (Henüz bugün sulanmadı)</b></color>", "<color=#FF5252><b>💧 Needs Water! (Not watered today)</b></color>")
                : LocalizationManager.L("Water_Done", "<color=#00E676><b>✨ Sulanmış (Toprak Nemli)</b></color>", "<color=#00E676><b>✨ Watered (Moist Soil)</b></color>");

            string growthDetailStr = (State == PlotState.RipeReadyToHarvest)
                ? LocalizationManager.L("Growth_Ready", "<color=#00E676><b>🎉 EKİN TAMAMEN OLGUNLAŞTI! HASATA HAZIR!</b></color>", "<color=#00E676><b>🎉 CROP FULLY MATURED! READY TO HARVEST!</b></color>")
                : LocalizationManager.L("Growth_TimeLeft", $"<b>Hasat Vaktine Kalan Süre:</b> <color=#00E676>{remainingDays} Gün</color>\n<b>Büyüme İlerlemesi:</b> {CurrentGrowthDay} / {TotalGrowthDays} Gün Büyüme", $"<b>Time Until Harvest:</b> <color=#00E676>{remainingDays} Days</color>\n<b>Growth Progress:</b> {CurrentGrowthDay} / {TotalGrowthDays} Days");

            string bodyText = LocalizationManager.L(
                "Plot_BodyFormat",
                $"<b>Ekilmiş Mahsul:</b> {sDef.iconEmoji} <b>{sDef.LocalizedName}</b>\n" +
                $"<b>Mevsim:</b> {seasonStr} • <b>Gerekli Seviye:</b> {sDef.requiredLevel}\n\n" +
                $"{growthDetailStr}\n\n" +
                $"<b>Sulama Durumu:</b> {waterStatusStr}\n" +
                $"<b>Tahmini Rekolte:</b> {sDef.yieldPerPlot} KG\n" +
                $"<b>Tahmini Satış Geliri:</b> {sDef.yieldPerPlot * sDef.unitSalePrice:N0}C (%40 Kâr Marjı)",
                $"<b>Planted Crop:</b> {sDef.iconEmoji} <b>{sDef.LocalizedName}</b>\n" +
                $"<b>Season:</b> {seasonStr} • <b>Required Level:</b> {sDef.requiredLevel}\n\n" +
                $"{growthDetailStr}\n\n" +
                $"<b>Water Status:</b> {waterStatusStr}\n" +
                $"<b>Est. Yield:</b> {sDef.yieldPerPlot} KG\n" +
                $"<b>Est. Sales Revenue:</b> {sDef.yieldPerPlot * sDef.unitSalePrice:N0}C (+40% Profit Margin)"
            );

            if (State == PlotState.RipeReadyToHarvest)
            {
                ModalManager.ShowConfirmModal(
                    LocalizationManager.L("Title_HarvestInfo", $"🌾 HASAT BİLGİSİ — {sDef.LocalizedName}", $"🌾 HARVEST INFO — {sDef.LocalizedName}"),
                    bodyText,
                    () => { HarvestCrop(); },
                    LocalizationManager.L("Btn_HarvestToBarn", "🌾 Biç ve Ahıra Koy", "🌾 Harvest to Barn"),
                    LocalizationManager.L("Btn_Close", "Kapat", "Close")
                );
            }
            else if (NeedsWater)
            {
                ModalManager.ShowConfirmModal(
                    LocalizationManager.L("Title_PlotInfo", $"🌱 TARLA BİLGİSİ — {sDef.LocalizedName}", $"🌱 FIELD PLOT INFO — {sDef.LocalizedName}"),
                    bodyText,
                    () => { WaterCrop(); },
                    LocalizationManager.L("Btn_WaterNow", "💧 Şimdi Sula", "💧 Water Now"),
                    LocalizationManager.L("Btn_Close", "Kapat", "Close")
                );
            }
            else
            {
                ModalManager.ShowModal(
                    LocalizationManager.L("Title_PlotInfo", $"🌱 TARLA BİLGİSİ — {sDef.LocalizedName}", $"🌱 FIELD PLOT INFO — {sDef.LocalizedName}"),
                    bodyText,
                    LocalizationManager.L("Btn_OK", "Tamam", "OK")
                );
            }
        }

        public void WaterCrop()
        {
            if (NeedsWater)
            {
                NeedsWater = false;
                WateredToday = true;
                UpdateFloatingIcon();
                ShowPlotPopup(transform.position, "💧 Sulandı! ✨");
            }
        }

        public void HarvestCrop()
        {
            if (State != PlotState.RipeReadyToHarvest) return;
            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(PlantedSeedId);
            if (sDef == null) return;

            int yieldAmount = sDef.yieldPerPlot;
            bool added = GardenSeedInventoryManager.Instance.TryAddCropToBarn(PlantedSeedId, yieldAmount);

            if (added)
            {
                ShowPlotPopup(transform.position, $"🌾 +{yieldAmount} {sDef.name.Replace(" Tohumu", "")} Biçildi! 🧺");
                ResetPlotToEmpty();
            }
            else
            {
                ModalManager.ShowModal("Ahır Dolu! ⚠️", "Ahır envanteri maksimum kapasiteye ulaştı! Lütfen ahırdaki ürünleri dükkana sevk edin veya ahırı geliştirin.", "Tamam");
            }
        }

        public void ClearSpoiledPlot()
        {
            ShowPlotPopup(transform.position, "🗑️ Çürüyen Ekin Temizlendi");
            ResetPlotToEmpty();
        }

        public void PlantSeed(string seedId)
        {
            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
            if (sDef == null) return;

            if (!GardenSeedInventoryManager.Instance.ConsumeSeed(seedId, 1))
            {
                ModalManager.ShowModal("Tohum Yok! ⚠️", "Elinde bu tohumdan kalmadı! EKT Tablet Tohumlar sekmesinden satın alabilirsin.", "Tamam");
                return;
            }

            PlantedSeedId = seedId;
            CurrentGrowthDay = 0;
            TotalGrowthDays = Mathf.Clamp(sDef.growthDays, 1, 5);
            NeedsWater = true;
            WateredToday = false;
            State = PlotState.PlantedSprout;

            UpdateVisuals();
            ShowPlotPopup(transform.position, $"🌱 {sDef.name} Ekildi!");
        }

        public void AdvanceGrowthByFarmerBonus(float boostFactor = 1.25f)
        {
            if (State == PlotState.PlantedSprout || State == PlotState.Growing)
            {
                if (NeedsWater && !WateredToday)
                {
                    WaterCrop(); // Çiftçi sular
                }
            }
            else if (State == PlotState.RipeReadyToHarvest)
            {
                HarvestCrop(); // Çiftçi otomatik biçer
            }
        }

        private void HandleDateUpdated(TimeManager.Season season, int day, int year)
        {
            if (State == PlotState.Empty) return;

            if (State == PlotState.RipeReadyToHarvest)
            {
                // Hasat vakti geçmiş ekin 1 gün beklenirse çürür!
                State = PlotState.SpoiledTrash;
                UpdateVisuals();
                return;
            }

            if (State == PlotState.PlantedSprout || State == PlotState.Growing)
            {
                if (WateredToday || !NeedsWater)
                {
                    CurrentGrowthDay++;
                    if (CurrentGrowthDay >= TotalGrowthDays)
                    {
                        State = PlotState.RipeReadyToHarvest;
                        NeedsWater = false;
                    }
                    else
                    {
                        State = PlotState.Growing;
                        NeedsWater = true;
                        WateredToday = false;
                    }
                }
                else
                {
                    // Sulanmadıysa büyümez, tekrar su ister
                    NeedsWater = true;
                }
                UpdateVisuals();
            }
        }

        private void ResetPlotToEmpty()
        {
            State = PlotState.Empty;
            PlantedSeedId = "";
            CurrentGrowthDay = 0;
            NeedsWater = false;
            WateredToday = false;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (cropMeshObj != null) Destroy(cropMeshObj);

            Renderer ren = GetComponent<Renderer>();
            if (ren != null)
            {
                // Sulanmış toprak koyulaşır
                ren.material.color = NeedsWater ? new Color(0.35f, 0.22f, 0.12f) : new Color(0.20f, 0.12f, 0.06f);
            }

            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(PlantedSeedId);
            Color cropColor = sDef != null ? sDef.cropColor : Color.green;

            switch (State)
            {
                case PlotState.PlantedSprout:
                    // 1. Aşama: Küçük Yeşil Filizler (Sprout)
                    cropMeshObj = new GameObject("Crop_Sprout");
                    cropMeshObj.transform.SetParent(transform, false);
                    cropMeshObj.transform.localPosition = Vector3.zero;

                    for (int i = -1; i <= 1; i += 2)
                    {
                        for (int j = -1; j <= 1; j += 2)
                        {
                            GameObject sprout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            sprout.transform.SetParent(cropMeshObj.transform, false);
                            sprout.transform.localPosition = new Vector3(i * 0.4f, 0.12f, j * 0.4f);
                            sprout.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
                            sprout.GetComponent<Renderer>().material.color = new Color(0.30f, 0.85f, 0.25f);
                            Destroy(sprout.GetComponent<Collider>());
                        }
                    }
                    break;

                case PlotState.Growing:
                    // 2. Aşama: Büyüyen Bitki Yaprakları (Growing)
                    cropMeshObj = new GameObject("Crop_Growing");
                    cropMeshObj.transform.SetParent(transform, false);
                    cropMeshObj.transform.localPosition = Vector3.zero;

                    for (int i = -1; i <= 1; i += 2)
                    {
                        for (int j = -1; j <= 1; j += 2)
                        {
                            GameObject plant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            plant.transform.SetParent(cropMeshObj.transform, false);
                            plant.transform.localPosition = new Vector3(i * 0.45f, 0.28f, j * 0.45f);
                            plant.transform.localScale = new Vector3(0.40f, 0.45f, 0.40f);
                            plant.GetComponent<Renderer>().material.color = new Color(0.20f, 0.70f, 0.22f);
                            Destroy(plant.GetComponent<Collider>());
                        }
                    }
                    break;

                case PlotState.RipeReadyToHarvest:
                    // 3. Aşama: Olgun Hasat Hazır Meyve/Sebze (Ripe)
                    cropMeshObj = new GameObject("Crop_Ripe");
                    cropMeshObj.transform.SetParent(transform, false);
                    cropMeshObj.transform.localPosition = Vector3.zero;

                    for (int i = -1; i <= 1; i += 2)
                    {
                        for (int j = -1; j <= 1; j += 2)
                        {
                            GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            fruit.transform.SetParent(cropMeshObj.transform, false);
                            fruit.transform.localPosition = new Vector3(i * 0.45f, 0.42f, j * 0.45f);
                            fruit.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                            fruit.GetComponent<Renderer>().material.color = cropColor;
                            Destroy(fruit.GetComponent<Collider>());
                        }
                    }
                    break;

                case PlotState.SpoiledTrash:
                    // Çürük / Çöp (Withered Trash)
                    cropMeshObj = new GameObject("Crop_Trash");
                    cropMeshObj.transform.SetParent(transform, false);
                    cropMeshObj.transform.localPosition = Vector3.zero;

                    for (int i = -1; i <= 1; i += 2)
                    {
                        GameObject trash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        trash.transform.SetParent(cropMeshObj.transform, false);
                        trash.transform.localPosition = new Vector3(i * 0.3f, 0.10f, 0f);
                        trash.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f);
                        trash.GetComponent<Renderer>().material.color = new Color(0.25f, 0.18f, 0.12f);
                        Destroy(trash.GetComponent<Collider>());
                    }
                    break;
            }

            UpdateFloatingIcon();
        }

        private void CreateFloatingIconUI()
        {
            if (floatingIconCanvas != null) Destroy(floatingIconCanvas);

            floatingIconCanvas = new GameObject($"Plot_Floating_Icon_Canvas_{name}");
            // Bağımsız Dünya Koordinatları (Parent scale 2.2x0.04 bozulmasını önlemek için null parent)
            floatingIconCanvas.transform.SetParent(null);
            floatingIconCanvas.transform.position = transform.position + new Vector3(0f, 0.65f, 0f);
            floatingIconCanvas.transform.localScale = Vector3.one * 0.0085f;

            Canvas canvas = floatingIconCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 85;

            RectTransform rt = floatingIconCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(270f, 72f);

            GameObject bgObj = new GameObject("Bg");
            bgObj.transform.SetParent(floatingIconCanvas.transform, false);
            RectTransform bRect = bgObj.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.sizeDelta = Vector2.zero;

            floatingIconImage = bgObj.AddComponent<Image>();
            floatingIconImage.sprite = UIStyleUtility.CreateOutlinePillSprite(270, 72, 18, 3, new Color(0.15f, 0.85f, 1.0f), new Color(0.05f, 0.12f, 0.20f, 0.96f));

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(bgObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            floatingIconText = txtObj.AddComponent<Text>();
            floatingIconText.font = font;
            floatingIconText.fontSize = 24;
            floatingIconText.fontStyle = FontStyle.Bold;
            floatingIconText.alignment = TextAnchor.MiddleCenter;
            floatingIconText.color = Color.white;

            UpdateFloatingIcon();
        }

        private void UpdateFloatingIcon()
        {
            if (floatingIconCanvas == null) return;

            if (State == PlotState.Empty)
            {
                floatingIconCanvas.SetActive(false);
                return;
            }

            floatingIconCanvas.SetActive(true);

            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(PlantedSeedId);
            string cropName = sDef != null ? sDef.name.Replace(" Tohumu", "") : "";

            if (State == PlotState.SpoiledTrash)
            {
                floatingIconText.text = "🗑️ ÇÜRÜMÜŞ EKİN";
                if (floatingIconImage != null)
                    floatingIconImage.sprite = UIStyleUtility.CreateOutlinePillSprite(270, 72, 18, 3, new Color(0.95f, 0.30f, 0.25f), new Color(0.18f, 0.08f, 0.08f, 0.96f));
                return;
            }

            if (State == PlotState.RipeReadyToHarvest)
            {
                floatingIconText.text = $"🌾 HASAT ET ({cropName})";
                if (floatingIconImage != null)
                    floatingIconImage.sprite = UIStyleUtility.CreateOutlinePillSprite(270, 72, 18, 3, new Color(1.0f, 0.80f, 0.20f), new Color(0.20f, 0.15f, 0.05f, 0.96f));
                return;
            }

            if (NeedsWater)
            {
                floatingIconText.text = "💧 SU BEKLİYOR!";
                if (floatingIconImage != null)
                    floatingIconImage.sprite = UIStyleUtility.CreateOutlinePillSprite(270, 72, 18, 3, new Color(0.15f, 0.85f, 1.0f), new Color(0.05f, 0.12f, 0.20f, 0.96f));
                return;
            }

            // Büyüyor (Su verilmiş)
            int remDays = Mathf.Max(0, TotalGrowthDays - CurrentGrowthDay);
            floatingIconText.text = $"🌱 {cropName} ({remDays} Gün)";
            if (floatingIconImage != null)
                floatingIconImage.sprite = UIStyleUtility.CreateOutlinePillSprite(270, 72, 18, 3, new Color(0.25f, 0.88f, 0.45f), new Color(0.06f, 0.16f, 0.08f, 0.94f));
        }

        // --- RADYAL TOHUM SEÇİM MENÜSÜ ---
        private void OpenRadialPlantingMenu()
        {
            TimeManager.Season activeSeason = (TimeManager.Instance != null) ? TimeManager.Instance.CurrentSeason : TimeManager.Season.İlkbahar;
            List<GardenSeedDef> seasonSeeds = GardenSeedDatabase.GetSeedsBySeason(activeSeason);
            List<GardenSeedDef> ownedSeasonSeeds = new List<GardenSeedDef>();

            foreach (var s in seasonSeeds)
            {
                if (GardenSeedInventoryManager.Instance.HasSeed(s.id, 1))
                {
                    ownedSeasonSeeds.Add(s);
                }
            }

            if (ownedSeasonSeeds.Count == 0)
            {
                string modalTitle = LocalizationManager.L("Modal_NoSeeds_Title", "Tohum Bulunamadı! ⚠️", "No Seeds Found! ⚠️");
                string seasonName = (TimeManager.Instance != null) ? TimeManager.Instance.GetLocalizedSeasonName(activeSeason) : activeSeason.ToString();
                string modalBody = string.Format(LocalizationManager.L("Modal_NoSeeds_Body", "Elinde {0} mevsimine ait hiç bahçe tohumu kalmadı!\n\nEKT Tablet -> Tohumlar sekmesinden yeni tohum satın alabilirsin.", "You don't have any garden seeds left for {0} season!\n\nYou can buy new seeds from EKT Tablet -> Seeds tab."), seasonName);
                string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
                ModalManager.ShowModal(modalTitle, modalBody, btnOk);
                return;
            }

            ModalManager.SetModalOpen(true);
            BuildRadialMenuUI(ownedSeasonSeeds);
        }

        private void BuildRadialMenuUI(List<GardenSeedDef> seedList)
        {
            if (radialMenuCanvasObj != null) Destroy(radialMenuCanvasObj);

            radialMenuCanvasObj = new GameObject("Radial_Seed_Menu_Canvas");
            Canvas canvas = radialMenuCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;

            CanvasScaler scaler = radialMenuCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            radialMenuCanvasObj.AddComponent<GraphicRaycaster>();

            // Dark Backdrop
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(radialMenuCanvasObj.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.05f, 0.08f, 0.12f, 0.75f);
            bdImg.raycastTarget = true;

            Button bdBtn = backdrop.AddComponent<Button>();
            bdBtn.onClick.AddListener(CloseRadialMenu);

            // Ring Container
            GameObject ringObj = new GameObject("Ring");
            ringObj.transform.SetParent(backdrop.transform, false);
            RectTransform rRect = ringObj.AddComponent<RectTransform>();
            rRect.anchoredPosition = Vector2.zero;
            rRect.sizeDelta = new Vector2(600f, 600f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            int count = seedList.Count;

            // 1. Dinamik Buton ve Yarıçap Hesaplamaları (Tohum Sayısına Göre Şekil Alır)
            float btnSize = Mathf.Clamp(92f - (count * 2.2f), 68f, 92f);
            float singleRingRadius = Mathf.Clamp(70f + (count * 14f), 85f, 155f);

            // 6'dan fazla tohum varsa iç içe 2 dairesel halkaya (Çift Halka) böl, az varsa tek halka yap!
            bool useDoubleRing = (count > 6);
            float maxOuterRadius = useDoubleRing ? 175f : singleRingRadius;

            // 2. Üst Başlık (En dış halkanın yukarısına dinamik oturur)
            GameObject titleObj = new GameObject("TopTitle");
            titleObj.transform.SetParent(ringObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            float titleY = maxOuterRadius + (btnSize * 0.5f) + 20f;
            tRect.anchoredPosition = new Vector2(0f, titleY);
            tRect.sizeDelta = new Vector2(230f, 44f);

            Image tBg = titleObj.AddComponent<Image>();
            tBg.sprite = UIStyleUtility.CreateRoundedPillSprite(230, 44, 22, new Color(0.12f, 0.18f, 0.24f, 0.96f));

            GameObject tTxtObj = new GameObject("Text");
            tTxtObj.transform.SetParent(titleObj.transform, false);
            RectTransform ttRect = tTxtObj.AddComponent<RectTransform>();
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;

            Text tTxt = tTxtObj.AddComponent<Text>();
            tTxt.font = font;
            string selectSeedFmt = LocalizationManager.L("Radial_SelectSeed", "🌱 TOHUM SEÇİNİZ ({0})", "🌱 SELECT SEED ({0})");
            tTxt.text = string.Format(selectSeedFmt, count);
            tTxt.fontSize = 16;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = new Color(0.35f, 0.85f, 0.40f);

            // 3. Tohum Butonlarının Yerleşimi (Dinamik Uyarlanır)
            if (!useDoubleRing)
            {
                // Tek Halka Düzeni (1 - 6 Tohum)
                float angleStep = 360f / count;
                float startAngle = 90f; // 12 yönünden başla

                for (int i = 0; i < count; i++)
                {
                    GardenSeedDef s = seedList[i];
                    float angle = (startAngle - (i * angleStep)) * Mathf.Deg2Rad;
                    Vector2 btnPos = new Vector2(Mathf.Cos(angle) * singleRingRadius, Mathf.Sin(angle) * singleRingRadius);
                    CreateRadialSeedButton(ringObj.transform, s, btnPos, btnSize, font);
                }
            }
            else
            {
                // Çift Halka Düzeni (7+ Tohum - Çiçek Yaprağı Formasyonu)
                int innerCount = Mathf.Min(4, count / 2);
                int outerCount = count - innerCount;

                float innerRadius = 95f;
                float outerRadius = 175f;

                // İç Halka
                float innerStep = 360f / innerCount;
                for (int i = 0; i < innerCount; i++)
                {
                    GardenSeedDef s = seedList[i];
                    float angle = (90f - (i * innerStep)) * Mathf.Deg2Rad;
                    Vector2 btnPos = new Vector2(Mathf.Cos(angle) * innerRadius, Mathf.Sin(angle) * innerRadius);
                    CreateRadialSeedButton(ringObj.transform, s, btnPos, btnSize, font);
                }

                // Dış Halka
                float outerStep = 360f / outerCount;
                float outerOffset = 45f; // Çapraz kaydırma
                for (int i = 0; i < outerCount; i++)
                {
                    GardenSeedDef s = seedList[innerCount + i];
                    float angle = (90f - outerOffset - (i * outerStep)) * Mathf.Deg2Rad;
                    Vector2 btnPos = new Vector2(Mathf.Cos(angle) * outerRadius, Mathf.Sin(angle) * outerRadius);
                    CreateRadialSeedButton(ringObj.transform, s, btnPos, btnSize, font);
                }
            }
        }

        private void CreateRadialSeedButton(Transform parent, GardenSeedDef s, Vector2 pos, float btnSize, Font font)
        {
            GameObject btnObj = new GameObject("SeedBtn_" + s.id);
            btnObj.transform.SetParent(parent, false);
            RectTransform bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = pos;
            bRect.sizeDelta = new Vector2(btnSize, btnSize);

            int cornerRadius = Mathf.RoundToInt(btnSize * 0.5f);
            Image bBg = btnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateOutlinePillSprite(Mathf.RoundToInt(btnSize), Mathf.RoundToInt(btnSize), cornerRadius, 3, s.cropColor, new Color(0.12f, 0.16f, 0.22f, 0.96f));

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bBg;
            string selectedSeedId = s.id;
            btn.onClick.AddListener(() => {
                CloseRadialMenu();
                PlantSeed(selectedSeedId);
            });

            GameObject bTxtObj = new GameObject("Txt");
            bTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRect = bTxtObj.AddComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;

            Text btTxt = bTxtObj.AddComponent<Text>();
            btTxt.font = font;
            int ownedCount = GardenSeedInventoryManager.Instance.GetSeedCount(s.id);
            string cropShortName = s.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "");
            btTxt.fontSize = (btnSize < 75f) ? 10 : 12;
            btTxt.text = $"{s.iconEmoji}\n<b>{cropShortName}</b>\n<color=#00E676>x{ownedCount}</color>";
            btTxt.alignment = TextAnchor.MiddleCenter;
            btTxt.color = Color.white;
        }

        private static void CloseRadialMenu()
        {
            if (radialMenuCanvasObj != null) Destroy(radialMenuCanvasObj);
            ModalManager.SetModalOpen(false);
        }

        private void ShowPlotPopup(Vector3 pos, string text)
        {
            GameObject popupObj = new GameObject("Popup_Plot");
            popupObj.transform.position = pos + Vector3.up * 1.5f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 90;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 60f);
            popupObj.transform.localScale = Vector3.one * 0.013f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = textObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = text;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.20f, 0.85f, 0.35f);

            Destroy(popupObj, 1.8f);
        }

        private bool WasPointerPressedThisFrame()
        {
            try { if (Input.GetMouseButtonDown(0)) return true; } catch { }

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                return true;
#endif

            return false;
        }

        private bool IsPointerOverUIButton()
        {
            if (EventSystem.current == null) return false;

            Vector2 pointerPos = GetPointerPosition();
            var eventData = new UnityEngine.EventSystems.PointerEventData(EventSystem.current)
            {
                position = pointerPos
            };
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject != null && (r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null || r.gameObject.GetComponent<UnityEngine.UI.Button>() != null))
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2 GetPointerPosition()
        {
            try
            {
                Vector3 mPos = Input.mousePosition;
                if (mPos.sqrMagnitude > 0.01f) return new Vector2(mPos.x, mPos.y);
            }
            catch { }

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 mPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                if (mPos.sqrMagnitude > 0.01f) return mPos;
            }
            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                Vector2 pPos = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
                if (pPos.sqrMagnitude > 0.01f) return pPos;
            }
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
            {
                return UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            }
#endif

            return Vector2.zero;
        }
    }
}
