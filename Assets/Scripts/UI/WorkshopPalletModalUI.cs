using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Atölye içindeki Hammadde Paletine tıklandığında açılan envanter modalı.
    /// Atölyede depolanan tüm mahsul kolilerini/hammaddeleri listeler.
    /// </summary>
    public class WorkshopPalletModalUI : MonoBehaviour
    {
        public static WorkshopPalletModalUI Instance { get; private set; }
        public static bool IsModalOpen => Instance != null && Instance.canvasObj != null && Instance.canvasObj.activeInHierarchy;

        private GameObject canvasObj;
        private Transform listContentTransform;
        private Text totalStoredText;

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
            if (IsModalOpen && WasEscapePressed())
            {
                HideModal();
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
            if (IsModalOpen)
            {
                BuildUI();
                RefreshList();
            }
        }

        public static void ShowModal()
        {
            if (Instance == null)
            {
                GameObject obj = new GameObject("WorkshopPalletModalUI");
                Instance = obj.AddComponent<WorkshopPalletModalUI>();
            }

            Instance.OpenUI();
        }

        public static void HideModal()
        {
            if (Instance != null)
            {
                Instance.CloseUI();
            }
        }

        private void OpenUI()
        {
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

        private void CloseUI()
        {
            if (canvasObj != null)
            {
                Destroy(canvasObj);
                canvasObj = null;
            }

            GameObject existing = GameObject.Find("Global_Workshop_Pallet_Canvas");
            if (existing != null) Destroy(existing);

            ModalManager.SetModalOpen(false);
        }

        private void BuildUI()
        {
            if (canvasObj != null) Destroy(canvasObj);
            GameObject existing = GameObject.Find("Global_Workshop_Pallet_Canvas");
            if (existing != null) Destroy(existing);

            canvasObj = new GameObject("Global_Workshop_Pallet_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 960;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Karartma Arka Plan
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
            bdBtn.onClick.AddListener(CloseUI);

            // Panel (800x600)
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(backdrop.transform, false);

            RectTransform pRect = panelObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = Vector2.zero;
            pRect.sizeDelta = new Vector2(800f, 580f);

            Image pBg = panelObj.AddComponent<Image>();
            pBg.sprite = UIStyleUtility.CreateOutlinePillSprite(800, 580, 18, 3, new Color(0.95f, 0.60f, 0.15f), new Color(0.10f, 0.14f, 0.18f, 0.98f));
            pBg.raycastTarget = true;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Başlık
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(-120f, 245f);
            tRect.sizeDelta = new Vector2(480f, 45f);

            Text tText = titleObj.AddComponent<Text>();
            tText.font = font;
            tText.text = LocalizationManager.L("WorkshopPallet_Title", "🏭 ATÖLYE HAMMADDE PALETİ", "🏭 WORKSHOP RAW MATERIAL PALLET");
            tText.fontSize = 23;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleLeft;
            tText.color = new Color(0.95f, 0.65f, 0.20f);
            tText.raycastTarget = false;

            // Toplam Stok Metni
            GameObject totalObj = new GameObject("TotalText");
            totalObj.transform.SetParent(panelObj.transform, false);
            RectTransform totRect = totalObj.AddComponent<RectTransform>();
            totRect.anchoredPosition = new Vector2(170f, 245f);
            totRect.sizeDelta = new Vector2(300f, 40f);

            totalStoredText = totalObj.AddComponent<Text>();
            totalStoredText.font = font;
            totalStoredText.fontSize = 17;
            totalStoredText.alignment = TextAnchor.MiddleRight;
            totalStoredText.color = Color.white;
            totalStoredText.raycastTarget = false;

            // Kapat Butonu (X)
            GameObject closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(panelObj.transform, false);
            RectTransform clRect = closeObj.AddComponent<RectTransform>();
            clRect.anchoredPosition = new Vector2(365f, 245f);
            clRect.sizeDelta = new Vector2(44f, 44f);

            Image clBg = closeObj.AddComponent<Image>();
            clBg.sprite = UIStyleUtility.CreateRoundedPillSprite(44, 44, 22, new Color(0.92f, 0.18f, 0.20f, 1f));
            clBg.raycastTarget = true;

            Button clBtn = closeObj.AddComponent<Button>();
            clBtn.targetGraphic = clBg;
            clBtn.onClick.AddListener(CloseUI);

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

            // Scroll View
            GameObject scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(panelObj.transform, false);
            RectTransform sRect = scrollObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, -20f);
            sRect.sizeDelta = new Vector2(740f, 440f);

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
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = cntRect;
            listContentTransform = content.transform;
        }

        private void RefreshList()
        {
            if (listContentTransform == null) return;
            foreach (Transform t in listContentTransform) Destroy(t.gameObject);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            int totalKg = (WorkshopPalletManager.Instance != null) ? WorkshopPalletManager.Instance.GetTotalStoredAmount() : 0;
            if (totalStoredText != null)
            {
                totalStoredText.text = LocalizationManager.L("WorkshopPallet_TotalStored", $"📦 Toplam Hammadde: <color=#FFA726>{totalKg}</color> KG", $"📦 Total Raw Materials: <color=#FFA726>{totalKg}</color> KG");
            }

            Dictionary<string, int> crops = (WorkshopPalletManager.Instance != null) ? WorkshopPalletManager.Instance.GetCropInventory() : new Dictionary<string, int>();

            if (crops == null || crops.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyMsg");
                emptyObj.transform.SetParent(listContentTransform, false);
                RectTransform eRect = emptyObj.AddComponent<RectTransform>();
                eRect.sizeDelta = new Vector2(720f, 80f);

                Text eTxt = emptyObj.AddComponent<Text>();
                eTxt.font = font;
                eTxt.text = LocalizationManager.L(
                    "WorkshopPallet_EmptyMsg",
                    "Atölye paletinde henüz hiç hammadde bulunmuyor.\nAhır arayüzündeki 'ATÖLYEYE GÖNDER' butonu ile mahsullerinizi buraya aktarabilirsiniz!",
                    "There are no raw materials on the workshop pallet yet.\nYou can transfer crops here using 'SHIP TO WORKSHOP' button in the Barn menu!"
                );
                eTxt.fontSize = 15;
                eTxt.alignment = TextAnchor.MiddleCenter;
                eTxt.color = Color.gray;
                return;
            }

            foreach (var kvp in crops)
            {
                string cropId = kvp.Key;
                int count = kvp.Value;

                GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(cropId);
                string cropName = (sDef != null) ? sDef.LocalizedName.Replace(" Tohumu", "").Replace(" Seeds", "").Replace(" Seed", "") : cropId;
                string emoji = (sDef != null) ? sDef.iconEmoji : "📦";

                GameObject rowObj = new GameObject("Row_" + cropId);
                rowObj.transform.SetParent(listContentTransform, false);
                RectTransform rRect = rowObj.AddComponent<RectTransform>();
                rRect.sizeDelta = new Vector2(720f, 54f);

                Image rBg = rowObj.AddComponent<Image>();
                rBg.sprite = UIStyleUtility.CreateOutlinePillSprite(720, 54, 10, 1, new Color(0.35f, 0.40f, 0.45f), new Color(0.14f, 0.18f, 0.22f, 0.95f));

                GameObject txtObj = new GameObject("Txt");
                txtObj.transform.SetParent(rowObj.transform, false);
                RectTransform tRect = txtObj.AddComponent<RectTransform>();
                tRect.anchoredPosition = new Vector2(-80f, 0f);
                tRect.sizeDelta = new Vector2(500f, 44f);

                Text txt = txtObj.AddComponent<Text>();
                txt.font = font;
                txt.text = $"{emoji}  <b>{cropName}</b>  <color=#B0BEC5>(İşlenmeye Hazır Hammadde / Koli)</color>";
                txt.fontSize = 16;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = Color.white;

                GameObject countObj = new GameObject("Count");
                countObj.transform.SetParent(rowObj.transform, false);
                RectTransform cRect = countObj.AddComponent<RectTransform>();
                cRect.anchoredPosition = new Vector2(240f, 0f);
                cRect.sizeDelta = new Vector2(200f, 44f);

                Text cTxt = countObj.AddComponent<Text>();
                cTxt.font = font;
                cTxt.text = $"<color=#FFA726><b>{count} KG</b></color>";
                cTxt.fontSize = 18;
                cTxt.alignment = TextAnchor.MiddleRight;
            }
        }
    }
}
