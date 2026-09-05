using System;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    public class FieldPlotDetailModalUI : MonoBehaviour
    {
        private static GameObject modalInstance;
        private static FieldPlotController activePlot;

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

        private void HandleLanguageChanged(GameLanguage lang)
        {
            if (activePlot != null)
            {
                ShowDetail(activePlot);
            }
        }

        public static void ShowDetail(FieldPlotController plot)
        {
            if (plot == null) return;
            activePlot = plot;
            if (modalInstance != null) Destroy(modalInstance);

            ModalManager.SetModalOpen(true);

            modalInstance = new GameObject("Modal_FieldPlotDetail");
            modalInstance.AddComponent<FieldPlotDetailModalUI>();

            Canvas canvas = modalInstance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 750;

            CanvasScaler scaler = modalInstance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            modalInstance.AddComponent<GraphicRaycaster>();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. Dark Backdrop
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(modalInstance.transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0.05f, 0.08f, 0.12f, 0.78f);
            bdImg.raycastTarget = true;

            Button bdBtn = backdrop.AddComponent<Button>();
            bdBtn.onClick.AddListener(CloseModal);

            // 2. Main Box Panel (620 x 540)
            GameObject boxObj = new GameObject("DetailBox");
            boxObj.transform.SetParent(backdrop.transform, false);
            RectTransform bRect = boxObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = Vector2.zero;
            bRect.sizeDelta = new Vector2(620f, 540f);

            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(plot.PlantedSeedId);
            Color themeColor = (sDef != null) ? sDef.cropColor : new Color(0.25f, 0.80f, 0.45f);

            Image bImg = boxObj.AddComponent<Image>();
            bImg.sprite = UIStyleUtility.CreateOutlinePillSprite(620, 540, 20, 3, themeColor, new Color(0.10f, 0.13f, 0.18f, 0.98f));
            bImg.raycastTarget = true;

            // Header Banner
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(boxObj.transform, false);
            RectTransform hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchoredPosition = new Vector2(0f, 225f);
            hRect.sizeDelta = new Vector2(560f, 56f);

            Image hBg = headerObj.AddComponent<Image>();
            hBg.sprite = UIStyleUtility.CreateRoundedPillSprite(560, 56, 14, new Color(0.14f, 0.18f, 0.25f, 0.95f));

            string titleStr;
            if (plot.State == PlotState.Empty)
            {
                titleStr = "🌱 <b>" + LocalizationManager.L("Plot_EmptyTitle", "BOŞ TARLA PARSELİ", "EMPTY FIELD PLOT") + "</b>";
            }
            else if (plot.State == PlotState.SpoiledTrash)
            {
                titleStr = "🗑️ <b>" + LocalizationManager.L("Plot_SpoiledTitle", "ÇÜRÜMÜŞ EKİN BİLGİSİ", "SPOILED CROP INFO") + "</b>";
            }
            else
            {
                string cropName = (sDef != null) ? sDef.LocalizedName : LocalizationManager.L("Plot_CropFallback", "Ekin", "Crop");
                titleStr = $"{sDef?.iconEmoji} <b>{cropName}</b>";
            }

            GameObject hTextObj = new GameObject("HeaderText");
            hTextObj.transform.SetParent(headerObj.transform, false);
            RectTransform htRect = hTextObj.AddComponent<RectTransform>();
            htRect.anchorMin = Vector2.zero;
            htRect.anchorMax = Vector2.one;

            Text hTxt = hTextObj.AddComponent<Text>();
            hTxt.font = font;
            hTxt.text = titleStr;
            hTxt.fontSize = 22;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.color = Color.white;

            // 3. Content Panel
            if (plot.State == PlotState.Empty)
            {
                BuildEmptyPlotContent(boxObj.transform, font, plot);
            }
            else if (plot.State == PlotState.SpoiledTrash)
            {
                BuildSpoiledPlotContent(boxObj.transform, font, plot);
            }
            else
            {
                BuildActiveCropContent(boxObj.transform, font, plot, sDef);
            }

            // 4. Kapat (X) Butonu
            GameObject closeBtnObj = new GameObject("CloseButton_X");
            closeBtnObj.transform.SetParent(boxObj.transform, false);
            RectTransform cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(275f, 235f);
            cRect.sizeDelta = new Vector2(46f, 46f);

            Image cBg = closeBtnObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(46, 46, 23, new Color(0.92f, 0.18f, 0.20f, 1f));
            cBg.raycastTarget = true;

            Button cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.targetGraphic = cBg;
            cBtn.onClick.AddListener(CloseModal);

            GameObject cxObj = new GameObject("X");
            cxObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform cxRect = cxObj.AddComponent<RectTransform>();
            cxRect.anchorMin = Vector2.zero;
            cxRect.anchorMax = Vector2.one;

            Text cxText = cxObj.AddComponent<Text>();
            cxText.font = font;
            cxText.text = "✖";
            cxText.fontSize = 26;
            cxText.fontStyle = FontStyle.Bold;
            cxText.alignment = TextAnchor.MiddleCenter;
            cxText.color = Color.white;
            cxText.raycastTarget = false;

            Outline cxOutline = cxObj.AddComponent<Outline>();
            cxOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            cxOutline.effectDistance = new Vector2(1.5f, -1.5f);

            closeBtnObj.transform.SetAsLastSibling();
        }

        private static void BuildEmptyPlotContent(Transform parent, Font font, FieldPlotController plot)
        {
            // Info Card
            GameObject infoCard = new GameObject("InfoCard");
            infoCard.transform.SetParent(parent, false);
            RectTransform icRect = infoCard.AddComponent<RectTransform>();
            icRect.anchoredPosition = new Vector2(0f, 60f);
            icRect.sizeDelta = new Vector2(560f, 210f);

            Image icBg = infoCard.AddComponent<Image>();
            icBg.sprite = UIStyleUtility.CreateOutlinePillSprite(560, 210, 14, 1, new Color(0.30f, 0.40f, 0.50f, 0.6f), new Color(0.12f, 0.16f, 0.22f, 0.90f));

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(infoCard.transform, false);
            RectTransform tRect = tObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(20f, 20f);
            tRect.offsetMax = new Vector2(-20f, -20f);

            Text txt = tObj.AddComponent<Text>();
            txt.font = font;
            TimeManager.Season curSeason = (TimeManager.Instance != null) ? TimeManager.Instance.CurrentSeason : TimeManager.Season.İlkbahar;
            string seasonName = (TimeManager.Instance != null) ? TimeManager.Instance.GetLocalizedSeasonName(curSeason) : curSeason.ToString();

            txt.text = LocalizationManager.L(
                "Plot_EmptyDesc",
                $"Bu tarla parseli şu anda ekime hazırdır.\n\n" +
                $"• <b>Aktif Mevsim:</b> <color=#00FFA3>{seasonName}</color>\n" +
                $"• <b>Toprak Durumu:</b> ☀️ Kuru (Tohum ekildikten sonra sulanmalıdır)\n" +
                $"• <b>Kapasite:</b> Tek ekimde 30-60 KG arası mahsul verir",
                $"This field plot is ready for planting.\n\n" +
                $"• <b>Active Season:</b> <color=#00FFA3>{seasonName}</color>\n" +
                $"• <b>Soil Condition:</b> ☀️ Dry (Must be watered after seeding)\n" +
                $"• <b>Capacity:</b> Yields 30-60 KG fresh crops per harvest"
            );
            txt.fontSize = 17;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = new Color(0.90f, 0.92f, 0.96f);
            txt.lineSpacing = 1.15f;

            // Butonlar
            CreateActionButton(parent, new Vector2(-130f, -195f), new Vector2(240f, 50f), "🌱 " + LocalizationManager.L("Btn_PlantSeedNow", "Tohum Ek", "Plant Seed"), new Color(0.20f, 0.85f, 0.40f), font, 18, () => {
                CloseModal();
                plot.OpenRadialPlantingMenuDirect();
            });

            CreateActionButton(parent, new Vector2(130f, -195f), new Vector2(240f, 50f), LocalizationManager.L("Btn_Close", "Kapat", "Close"), new Color(0.35f, 0.40f, 0.48f), font, 18, () => {
                CloseModal();
            });
        }

        private static void BuildSpoiledPlotContent(Transform parent, Font font, FieldPlotController plot)
        {
            GameObject infoCard = new GameObject("InfoCard");
            infoCard.transform.SetParent(parent, false);
            RectTransform icRect = infoCard.AddComponent<RectTransform>();
            icRect.anchoredPosition = new Vector2(0f, 60f);
            icRect.sizeDelta = new Vector2(560f, 210f);

            Image icBg = infoCard.AddComponent<Image>();
            icBg.sprite = UIStyleUtility.CreateOutlinePillSprite(560, 210, 14, 1, new Color(0.95f, 0.30f, 0.30f, 0.6f), new Color(0.16f, 0.10f, 0.10f, 0.90f));

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(infoCard.transform, false);
            RectTransform tRect = tObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = new Vector2(20f, 20f);
            tRect.offsetMax = new Vector2(-20f, -20f);

            Text txt = tObj.AddComponent<Text>();
            txt.font = font;
            txt.text = LocalizationManager.L(
                "Plot_SpoiledDesc",
                "⚠️ <b>Bu ekim zamanında biçilmediği için çürümüştür!</b>\n\n" +
                "Hasat vakti gelen ürünler 1 gün içinde toplanmadığında çürür.\n" +
                "Toprağı temizleyerek yeniden taze tohumlar ekebilirsiniz.",
                "⚠️ <b>This crop has withered because it was not harvested in time!</b>\n\n" +
                "Ripe crops spoil if left unharvested for over 1 day.\n" +
                "Clear the soil plot to sow fresh seeds again."
            );
            txt.fontSize = 17;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = new Color(0.95f, 0.75f, 0.75f);
            txt.lineSpacing = 1.15f;

            CreateActionButton(parent, new Vector2(-130f, -195f), new Vector2(240f, 50f), "🗑️ " + LocalizationManager.L("Btn_ClearPlot", "Toprağı Temizle", "Clear Soil"), new Color(0.95f, 0.30f, 0.25f), font, 18, () => {
                plot.ClearSpoiledPlot();
                CloseModal();
            });

            CreateActionButton(parent, new Vector2(130f, -195f), new Vector2(240f, 50f), LocalizationManager.L("Btn_Close", "Kapat", "Close"), new Color(0.35f, 0.40f, 0.48f), font, 18, () => {
                CloseModal();
            });
        }

        private static void BuildActiveCropContent(Transform parent, Font font, FieldPlotController plot, GardenSeedDef sDef)
        {
            if (sDef == null) return;

            // 1. Üç Aşamalı İlerleme Çubuğu Paneli (3-Stage Progress Bar)
            GameObject stagePanel = new GameObject("StageProgressPanel");
            stagePanel.transform.SetParent(parent, false);
            RectTransform spRect = stagePanel.AddComponent<RectTransform>();
            spRect.anchoredPosition = new Vector2(0f, 135f);
            spRect.sizeDelta = new Vector2(560f, 82f);

            Image spBg = stagePanel.AddComponent<Image>();
            spBg.sprite = UIStyleUtility.CreateOutlinePillSprite(560, 82, 14, 1, new Color(0.25f, 0.35f, 0.48f, 0.6f), new Color(0.12f, 0.16f, 0.22f, 0.90f));

            int currentStageNum = (plot.State == PlotState.PlantedSprout) ? 1 : ((plot.State == PlotState.Growing) ? 2 : 3);

            float[] stepX = new float[] { -180f, 0f, 180f };
            string[] stepTitles = new string[]
            {
                LocalizationManager.L("Stage_1", "1. Filiz 🌱", "1. Sprout 🌱"),
                LocalizationManager.L("Stage_2", "2. Gelişme 🌿", "2. Growing 🌿"),
                LocalizationManager.L("Stage_3", "3. Hasat 🧺", "3. Harvest 🧺")
            };

            for (int i = 0; i < 3; i++)
            {
                int stepNum = i + 1;
                bool isCompleted = (stepNum < currentStageNum);
                bool isActive = (stepNum == currentStageNum);

                Color stepCol = isCompleted ? new Color(0.15f, 0.85f, 0.45f) : (isActive ? new Color(1.00f, 0.80f, 0.15f) : new Color(0.40f, 0.45f, 0.52f));
                Color bgCol = isActive ? new Color(0.25f, 0.20f, 0.05f, 0.95f) : (isCompleted ? new Color(0.08f, 0.20f, 0.12f, 0.95f) : new Color(0.10f, 0.12f, 0.16f, 0.95f));

                GameObject pill = new GameObject("Step_" + stepNum);
                pill.transform.SetParent(stagePanel.transform, false);
                RectTransform pRect = pill.AddComponent<RectTransform>();
                pRect.anchoredPosition = new Vector2(stepX[i], 0f);
                pRect.sizeDelta = new Vector2(165f, 54f);

                Image pBg = pill.AddComponent<Image>();
                pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(165, 54, 12, isActive ? 2 : 1, stepCol, bgCol);

                GameObject pTxtObj = new GameObject("Txt");
                pTxtObj.transform.SetParent(pill.transform, false);
                RectTransform ptRect = pTxtObj.AddComponent<RectTransform>();
                ptRect.anchorMin = Vector2.zero;
                ptRect.anchorMax = Vector2.one;

                Text pTxt = pTxtObj.AddComponent<Text>();
                pTxt.font = font;
                pTxt.text = stepTitles[i];
                pTxt.fontSize = 15;
                pTxt.fontStyle = (isActive || isCompleted) ? FontStyle.Bold : FontStyle.Normal;
                pTxt.alignment = TextAnchor.MiddleCenter;
                pTxt.color = stepCol;
            }

            // 2. Detay Kartı (Info Details)
            GameObject detailCard = new GameObject("DetailCard");
            detailCard.transform.SetParent(parent, false);
            RectTransform dcRect = detailCard.AddComponent<RectTransform>();
            dcRect.anchoredPosition = new Vector2(0f, -20f);
            dcRect.sizeDelta = new Vector2(560f, 195f);

            Image dcBg = detailCard.AddComponent<Image>();
            dcBg.sprite = UIStyleUtility.CreateOutlinePillSprite(560, 195, 14, 1, new Color(0.30f, 0.40f, 0.52f, 0.6f), new Color(0.12f, 0.16f, 0.22f, 0.90f));

            GameObject dtObj = new GameObject("Text");
            dtObj.transform.SetParent(detailCard.transform, false);
            RectTransform dtRect = dtObj.AddComponent<RectTransform>();
            dtRect.anchorMin = Vector2.zero;
            dtRect.anchorMax = Vector2.one;
            dtRect.offsetMin = new Vector2(18f, 14f);
            dtRect.offsetMax = new Vector2(-18f, -14f);

            Text dTxt = dtObj.AddComponent<Text>();
            dTxt.font = font;

            int remainingDays = Mathf.Max(0, plot.TotalGrowthDays - plot.CurrentGrowthDay);
            string seasonStr = (TimeManager.Instance != null) ? TimeManager.Instance.GetLocalizedSeasonName(sDef.season) : sDef.season.ToString();

            string waterLine = plot.NeedsWater
                ? LocalizationManager.L("Plot_WaterDry", "<color=#FF5252>☀️ <b>Kuru Toprak (Su Bekliyor!)</b></color>", "<color=#FF5252>☀️ <b>Dry Soil (Needs Water!)</b></color>")
                : LocalizationManager.L("Plot_Watered", "<color=#00FFA3>💧 <b>Sulanmış (Nemli & Islak Toprak)</b></color>", "<color=#00FFA3>💧 <b>Watered (Moist Soil)</b></color>");

            string growthLine = (plot.State == PlotState.RipeReadyToHarvest)
                ? LocalizationManager.L("Plot_GrowthReady", "<color=#FFD700>🎉 <b>HASATA HAZIR! (%100 Olgunlaştı)</b></color>", "<color=#FFD700>🎉 <b>READY TO HARVEST! (100% Ripe)</b></color>")
                : string.Format(
                    LocalizationManager.L("Plot_GrowthRemainingFmt", "<b>Kalan Büyüme Süresi:</b> <color=#00FFA3>{0} Gün</color> (İlerleme: {1}/{2} Gün)", "<b>Remaining Growth Time:</b> <color=#00FFA3>{0} Days</color> (Progress: {1}/{2} Days)"),
                    remainingDays, plot.CurrentGrowthDay, plot.TotalGrowthDays);

            int totalRevenue = sDef.yieldPerPlot * sDef.unitSalePrice;

            dTxt.text = LocalizationManager.L(
                "Plot_ActiveDetails",
                $"• {growthLine}\n" +
                $"• <b>Sulama Durumu:</b> {waterLine}\n" +
                $"• <b>Mevsim & Seviye:</b> {seasonStr} • Seviye {sDef.requiredLevel}\n" +
                $"• <b>Tahmini Hasat Miktarı:</b> <color=#FFFFFF><b>{sDef.yieldPerPlot} Adet</b></color> (Manav Reyonuna Gider)\n" +
                $"• <b>Tahmini Satış Geliri:</b> <color=#FFD700><b>+{totalRevenue:N0} Credit</b></color> (%40 Net Kâr)",
                $"• {growthLine}\n" +
                $"• <b>Water Status:</b> {waterLine}\n" +
                $"• <b>Season & Level:</b> {seasonStr} • Level {sDef.requiredLevel}\n" +
                $"• <b>Estimated Yield:</b> <color=#FFFFFF><b>{sDef.yieldPerPlot} Items</b></color> (Goes to Produce Stand)\n" +
                $"• <b>Estimated Revenue:</b> <color=#FFD700><b>+{totalRevenue:N0} Credit</b></color> (+40% Net Profit)"
            );
            dTxt.fontSize = 16;
            dTxt.lineSpacing = 1.15f;
            dTxt.alignment = TextAnchor.MiddleLeft;
            dTxt.color = new Color(0.92f, 0.94f, 0.97f);

            // 3. Eylem Butonları
            if (plot.State == PlotState.RipeReadyToHarvest)
            {
                CreateActionButton(parent, new Vector2(-130f, -195f), new Vector2(240f, 50f), "🌾 " + LocalizationManager.L("Btn_HarvestToBarn", "Biç ve Ahıra Koy", "Harvest to Barn"), new Color(1.00f, 0.75f, 0.15f), font, 18, () => {
                    plot.HarvestCrop();
                    CloseModal();
                });

                CreateActionButton(parent, new Vector2(130f, -195f), new Vector2(240f, 50f), LocalizationManager.L("Btn_Close", "Kapat", "Close"), new Color(0.35f, 0.40f, 0.48f), font, 18, () => {
                    CloseModal();
                });
            }
            else if (plot.NeedsWater)
            {
                CreateActionButton(parent, new Vector2(-130f, -195f), new Vector2(240f, 50f), "💧 " + LocalizationManager.L("Btn_WaterNow", "Şimdi Sula", "Water Now"), new Color(0.18f, 0.65f, 1.00f), font, 18, () => {
                    plot.WaterCrop();
                    ShowDetail(plot); // Detay penceresini anında sula durumuna güncelle
                });

                CreateActionButton(parent, new Vector2(130f, -195f), new Vector2(240f, 50f), LocalizationManager.L("Btn_Close", "Kapat", "Close"), new Color(0.35f, 0.40f, 0.48f), font, 18, () => {
                    CloseModal();
                });
            }
            else
            {
                CreateActionButton(parent, new Vector2(0f, -195f), new Vector2(260f, 50f), LocalizationManager.L("Btn_OK", "Tamam", "OK"), new Color(0.20f, 0.80f, 0.45f), font, 18, () => {
                    CloseModal();
                });
            }
        }

        private static void CreateActionButton(Transform parent, Vector2 pos, Vector2 size, string text, Color btnColor, Font font, int fontSize, Action onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + text.Replace(" ", ""));
            btnObj.transform.SetParent(parent, false);
            RectTransform bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = pos;
            bRect.sizeDelta = size;

            Image bBg = btnObj.AddComponent<Image>();
            bBg.sprite = UIStyleUtility.CreateRoundedPillSprite(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y), Mathf.RoundToInt(size.y * 0.5f), btnColor);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bBg;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = font;
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        public static void CloseModal()
        {
            if (modalInstance != null)
            {
                Destroy(modalInstance);
                modalInstance = null;
            }
            ModalManager.SetModalOpen(false);
        }

        public static bool IsDetailOpen => modalInstance != null && modalInstance.activeInHierarchy;
    }
}
