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
        public static readonly List<FieldPlotController> AllPlots = new List<FieldPlotController>();

        public PlotState State { get; private set; } = PlotState.Empty;
        public string PlantedSeedId { get; private set; } = "";
        public int CurrentGrowthDay { get; private set; } = 0;
        public int TotalGrowthDays { get; private set; } = 1;
        public bool NeedsWater { get; private set; } = false;
        public bool WateredToday { get; private set; } = false;

        private GameObject cropMeshObj;

        private static GameObject radialMenuCanvasObj;
        public static bool IsRadialMenuOpen => radialMenuCanvasObj != null;

        private void OnEnable()
        {
            if (!AllPlots.Contains(this)) AllPlots.Add(this);

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnNewDayStarted -= HandleDateUpdated;
                TimeManager.Instance.OnNewDayStarted += HandleDateUpdated;
            }
        }

        private void OnDisable()
        {
            AllPlots.Remove(this);

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnNewDayStarted -= HandleDateUpdated;
            }
        }

        private void Start()
        {
            if (!AllPlots.Contains(this)) AllPlots.Add(this);

            if (GetComponent<Collider>() == null)
            {
                BoxCollider col = gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.2f, 0f);
                col.size = new Vector3(2.2f, 0.4f, 2.2f);
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnNewDayStarted -= HandleDateUpdated;
                TimeManager.Instance.OnNewDayStarted += HandleDateUpdated;
            }

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
            AllPlots.Remove(this);
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnNewDayStarted -= HandleDateUpdated;
            }
        }

        private void Update()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || IsRadialMenuOpen || FieldPlotDetailModalUI.IsDetailOpen)
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

                                    if (plot == this)
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
        }

        public void OnPlotClicked()
        {
            FieldPlotDetailModalUI.ShowDetail(this);
        }

        public void WaterCrop()
        {
            if (NeedsWater || !WateredToday)
            {
                NeedsWater = false;
                WateredToday = true;
                UpdateVisuals();
                string waterPop = LocalizationManager.L("Plot_WateredPopup", "💧 Sulandı! ✨", "💧 Watered! ✨");
                ShowPlotPopup(transform.position, waterPop);
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
                string cropShortName = sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "");
                string harvestFmt = LocalizationManager.L("Plot_HarvestedPopup", "🌾 +{0} {1} Biçildi! 🧺", "🌾 +{0} {1} Harvested! 🧺");
                ShowPlotPopup(transform.position, string.Format(harvestFmt, yieldAmount, cropShortName));
                ResetPlotToEmpty();
            }
            else
            {
                string barnFullTitle = LocalizationManager.L("Modal_BarnFullTitle", "Ahır Dolu! ⚠️", "Barn Full! ⚠️");
                string barnFullBody = LocalizationManager.L("Modal_BarnFullBody", "Ahır envanteri maksimum kapasiteye ulaştı! Lütfen ahırdaki ürünleri dükkana sevk edin veya ahırı geliştirin.", "Barn inventory has reached maximum capacity! Please transfer products to the store or upgrade the barn.");
                string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
                ModalManager.ShowModal(barnFullTitle, barnFullBody, btnOk);
            }
        }

        public void ClearSpoiledPlot()
        {
            string clearedPop = LocalizationManager.L("Plot_ClearedPopup", "🗑️ Çürüyen Ekin Temizlendi", "🗑️ Withered Crop Cleared");
            ShowPlotPopup(transform.position, clearedPop);
            ResetPlotToEmpty();
        }

        public void PlantSeed(string seedId)
        {
            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
            if (sDef == null) return;

            if (!GardenSeedInventoryManager.Instance.ConsumeSeed(seedId, 1))
            {
                string noSeedTitle = LocalizationManager.L("Modal_NoSeedTitle", "Tohum Yok! ⚠️", "No Seeds! ⚠️");
                string noSeedBody = LocalizationManager.L("Modal_NoSeedBody", "Elinde bu tohumdan kalmadı! EKT Tablet Tohumlar sekmesinden satın alabilirsin.", "You have no more of this seed! You can buy more from EKT Tablet Seeds tab.");
                string btnOk = LocalizationManager.L("Btn_Ok", "Tamam", "OK");
                ModalManager.ShowModal(noSeedTitle, noSeedBody, btnOk);
                return;
            }

            PlantedSeedId = seedId;
            CurrentGrowthDay = 0;
            TotalGrowthDays = Mathf.Clamp(sDef.growthDays, 1, 5);
            NeedsWater = true;
            WateredToday = false;
            State = PlotState.PlantedSprout;

            UpdateVisuals();
            string plantFmt = LocalizationManager.L("Plot_PlantedPopup", "🌱 {0} Ekildi!", "🌱 {0} Planted!");
            ShowPlotPopup(transform.position, string.Format(plantFmt, sDef.LocalizedName));

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotifyCropPlanted(seedId);
            }
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
            else if (State == PlotState.SpoiledTrash)
            {
                ClearSpoiledPlot(); // Çiftçi çürüyen ekini temizler ve ekilebilir alana dönüştürür
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
                // Toprak Görseli: Sulanmamış/Kuru = Açık Sıcak Kahverengi, Sulanmış/Nemli = Koyu Islak Kahverengi
                bool isDry = (State == PlotState.Empty) || NeedsWater;
                ren.material.color = isDry ? new Color(0.42f, 0.28f, 0.16f) : new Color(0.16f, 0.10f, 0.05f);
            }

            if (State != PlotState.Empty)
            {
                cropMeshObj = new GameObject("Crop3D_" + State);
                cropMeshObj.transform.SetParent(transform, false);
                cropMeshObj.transform.localPosition = new Vector3(0f, 0.52f, 0f);

                Vector3 pScale = transform.localScale;
                Vector3 invScale = new Vector3(
                    pScale.x > 0.001f ? 1f / pScale.x : 1f,
                    pScale.y > 0.001f ? 1f / pScale.y : 1f,
                    pScale.z > 0.001f ? 1f / pScale.z : 1f
                );
                cropMeshObj.transform.localScale = invScale;

                ProceduralCrop3DBuilder.BuildCrop3D(cropMeshObj.transform, PlantedSeedId, State);
            }
        }

        public void OpenRadialPlantingMenuDirect()
        {
            OpenRadialPlantingMenu();
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
            rRect.sizeDelta = new Vector2(800f, 800f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            int count = seedList.Count;

            // 1. Dinamik Buton ve Yarıçap Hesaplamaları (Tohum Sayısına Göre Şekil Alır - Genişletilmiş & Okunaklı)
            float btnSize = Mathf.Clamp(144f - (count * 3.5f), 120f, 140f);
            float singleRingRadius = Mathf.Clamp(115f + (count * 20f), 140f, 215f);

            // 6'dan fazla tohum varsa iç içe 2 dairesel halkaya (Çift Halka) böl, az varsa tek halka yap!
            bool useDoubleRing = (count > 6);
            if (useDoubleRing)
            {
                btnSize = 104f;
            }
            float maxOuterRadius = useDoubleRing ? 230f : singleRingRadius;

            // 2. Üst Başlık (En dış halkanın yukarısına dinamik oturur)
            GameObject titleObj = new GameObject("TopTitle");
            titleObj.transform.SetParent(ringObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            float titleY = maxOuterRadius + (btnSize * 0.5f) + 28f;
            tRect.anchoredPosition = new Vector2(0f, titleY);
            tRect.sizeDelta = new Vector2(300f, 54f);

            Image tBg = titleObj.AddComponent<Image>();
            tBg.sprite = UIStyleUtility.CreateRoundedPillSprite(300, 54, 27, new Color(0.08f, 0.12f, 0.18f, 0.97f));

            GameObject tTxtObj = new GameObject("Text");
            tTxtObj.transform.SetParent(titleObj.transform, false);
            RectTransform ttRect = tTxtObj.AddComponent<RectTransform>();
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;

            Text tTxt = tTxtObj.AddComponent<Text>();
            tTxt.font = font;
            string selectSeedFmt = LocalizationManager.L("Radial_SelectSeed", "🌱 TOHUM SEÇİNİZ ({0})", "🌱 SELECT SEED ({0})");
            tTxt.text = string.Format(selectSeedFmt, count);
            tTxt.fontSize = 20;
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

                float innerRadius = 130f;
                float outerRadius = 230f;

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
            Color btnBgColor = Color.Lerp(new Color(0.08f, 0.10f, 0.14f, 0.97f), s.cropColor, 0.20f);
            bBg.sprite = UIStyleUtility.CreateOutlinePillSprite(Mathf.RoundToInt(btnSize), Mathf.RoundToInt(btnSize), cornerRadius, 4, s.cropColor, btnBgColor);

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
            btRect.offsetMin = new Vector2(6f, 6f);
            btRect.offsetMax = new Vector2(-6f, -6f);

            Text btTxt = bTxtObj.AddComponent<Text>();
            btTxt.font = font;
            int ownedCount = GardenSeedInventoryManager.Instance.GetSeedCount(s.id);
            string cropShortName = s.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "");

            int emojiSize = (btnSize >= 120f) ? 26 : ((btnSize >= 100f) ? 22 : 18);
            int nameSize = (btnSize >= 120f) ? 17 : ((btnSize >= 100f) ? 15 : 13);
            int countSize = (btnSize >= 120f) ? 16 : ((btnSize >= 100f) ? 14 : 12);

            btTxt.fontSize = nameSize;
            btTxt.lineSpacing = 1.08f;
            btTxt.text = $"<size={emojiSize}>{s.iconEmoji}</size>\n<b>{cropShortName}</b>\n<size={countSize}><color=#00FFA3>x{ownedCount}</color></size>";
            btTxt.alignment = TextAnchor.MiddleCenter;
            btTxt.color = Color.white;
            btTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            btTxt.verticalOverflow = VerticalWrapMode.Truncate;
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
