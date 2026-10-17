using Farm2Shelf.Core;
using Farm2Shelf.Environment;
using UnityEngine;
using UnityEngine.UI;

namespace Farm2Shelf.UI
{
    // A session-only tool: the panel receives UI input, the rest of the screen stays interactive.
    public sealed class SeedPlantingModeUI : MonoBehaviour
    {
        private static SeedPlantingModeUI instance;
        private string seedId;
        private FieldPlotController lastPlot;
        private GameObject panel;
        private Text title, hint, changeLabel, finishLabel;
        private int selectionFrame;
        private float refreshAt;
        private bool harvesting;
        private int harvestedPlots;

        public static void BeginHarvest(FieldPlotController plot)
        {
            if (plot == null || plot.State != PlotState.RipeReadyToHarvest) return;
            if (instance == null)
            {
                var root = new GameObject("Held_Harvest_Tool", typeof(RectTransform));
                instance = root.AddComponent<SeedPlantingModeUI>();
                instance.Build();
            }
            instance.harvesting = true;
            instance.harvestedPlots = 0;
            instance.selectionFrame = Time.frameCount;
            instance.HarvestPlot(plot);
            Refresh();
        }

        private void HarvestPlot(FieldPlotController plot)
        {
            lastPlot = plot;
            if (plot.State != PlotState.RipeReadyToHarvest) return;
            plot.HarvestCrop();
            // A full barn leaves the crop intact and the tool selected.
            if (plot.State == PlotState.Empty) harvestedPlots++;
        }

        public static void SelectSeed(string id, FieldPlotController plot)
        {
            if (instance == null)
            {
                var root = new GameObject("Held_Seed_Tool", typeof(RectTransform));
                instance = root.AddComponent<SeedPlantingModeUI>();
                instance.Build();
            }
            instance.seedId = id;
            instance.harvesting = false;
            instance.lastPlot = plot;
            instance.selectionFrame = Time.frameCount;
            Refresh();
        }

        public static bool HandlePlotClick(FieldPlotController plot)
        {
            if (instance == null) return false;
            if (instance.selectionFrame == Time.frameCount) return true;
            if (instance.harvesting)
            {
                instance.HarvestPlot(plot);
                Refresh();
                return true;
            }
            var def = GardenSeedDatabase.GetSeedById(instance.seedId);
            if (def == null || (TimeManager.Instance != null && def.season != TimeManager.Instance.CurrentSeason))
            { Cancel(); return false; }
            instance.lastPlot = plot;
            if (plot.State != PlotState.Empty) return true;
            if (!GardenSeedInventoryManager.Instance.HasSeed(instance.seedId, 1)) return false;
            plot.PlantSeed(instance.seedId);
            Refresh();
            return true;
        }

        public static void Cancel()
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            Destroy(instance.gameObject);
            instance = null;
        }

        private void OnDestroy() { if (instance == this) instance = null; }

        private void Update()
        {
            if (MainMenuUI.IsMenuVisible || FieldPlotController.AllPlots.Count == 0) { Cancel(); return; }
            bool visible = !ModalManager.IsModalOpen && !EKTPhoneManager.IsTabletOpen &&
                !(PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen);
            if (panel.activeSelf != visible) panel.SetActive(visible);
            if (Time.unscaledTime >= refreshAt) { refreshAt = Time.unscaledTime + 0.2f; Refresh(); }
        }

        public static void Refresh()
        {
            if (instance == null) return;
            instance.changeLabel.transform.parent.gameObject.SetActive(!instance.harvesting);
            ((RectTransform)instance.finishLabel.transform.parent).anchoredPosition = new Vector2(230f, instance.harvesting ? 0f : -29f);
            instance.finishLabel.text = LocalizationManager.L("PlantTool_Finish", "Bitir", "Done");
            if (instance.harvesting)
            {
                instance.title.text = "🌾 " + LocalizationManager.L("Btn_HarvestToBarn", "Biç ve Ahıra Koy", "Harvest to Barn");
                instance.hint.text = string.Format(LocalizationManager.L("HarvestTool_Hint", "Olgun ekinlere dokun. Toplanan tarla: {0}", "Tap ripe crops. Plots harvested: {0}"), instance.harvestedPlots);
                return;
            }
            var def = GardenSeedDatabase.GetSeedById(instance.seedId);
            if (def == null || (TimeManager.Instance != null && def.season != TimeManager.Instance.CurrentSeason))
            { Cancel(); return; }
            int count = GardenSeedInventoryManager.Instance.GetSeedCount(instance.seedId);
            instance.title.text = def.iconEmoji + "  " + def.LocalizedName + "  ×" + count;
            instance.hint.text = count > 0
                ? LocalizationManager.L("PlantTool_Hint", "Ekmek için boş tarlalara dokun.", "Tap empty plots to plant.")
                : LocalizationManager.L("PlantTool_Empty", "Tohum bitti. Başka bir tohum seçebilirsin.", "Out of seeds. Select another seed.");
            instance.changeLabel.text = LocalizationManager.L("PlantTool_Change", "Değiştir", "Change");
            instance.finishLabel.text = LocalizationManager.L("PlantTool_Finish", "Bitir", "Done");
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 650;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            panel = new GameObject("SeedInHand", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(690f, 126f);
            panel.GetComponent<Image>().sprite = UIStyleUtility.CreateRoundedPillSprite(690, 126, 20, new Color(0.07f, 0.13f, 0.12f, 0.97f));
            title = MakeText(panel.transform, new Vector2(-110, 25), new Vector2(440, 48), 27);
            hint = MakeText(panel.transform, new Vector2(-110, -26), new Vector2(440, 44), 21);
            changeLabel = MakeButton(new Vector2(230, 28), new Color(0.13f, 0.43f, 0.28f), () =>
            {
                if (lastPlot == null && FieldPlotController.AllPlots.Count > 0) lastPlot = FieldPlotController.AllPlots[0];
                if (lastPlot != null) lastPlot.OpenRadialPlantingMenuDirect();
            });
            finishLabel = MakeButton(new Vector2(230, -29), new Color(0.3f, 0.34f, 0.38f), Cancel);
        }

        private Text MakeButton(Vector2 position, Color color, UnityEngine.Events.UnityAction action)
        {
            var button = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(panel.transform, false);
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(190, 48);
            var image = button.GetComponent<Image>();
            image.sprite = UIStyleUtility.CreateRoundedPillSprite(190, 48, 14, color);
            button.GetComponent<Button>().targetGraphic = image;
            button.GetComponent<Button>().onClick.AddListener(action);
            return MakeText(button.transform, Vector2.zero, new Vector2(174, 44), 23);
        }

        private Text MakeText(Transform parent, Vector2 position, Vector2 size, int fontSize)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = obj.GetComponent<Text>();
            text.font = UIStyleUtility.GetGlobalFont(fontSize);
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = fontSize - 4;
            text.resizeTextMaxSize = fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
    }
}
