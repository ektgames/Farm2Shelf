using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Teslimat paleti üzerindeki her bir 3D koli objesini ve etkileşimlerini yönetir.
    /// Fare ile üzerine gelindiğinde (Hover) veya mobil dokunmada 3D Türkçe Tooltip etiketi gösterir.
    /// Tıklandığında yerleştirme modunu (Placement Mode) başlatır.
    /// </summary>
    public class DeliveryBoxController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public FurnitureType furnitureType;
        public FurnitureType FurnitureType => furnitureType;

        private GameObject tooltipUI;
        private Canvas tooltipCanvas;
        private Image hoverHighlightImage;
        private bool isHovered = false;

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

        private void HandleLanguageChanged(GameLanguage lang)
        {
            if (tooltipUI != null)
            {
                bool wasActive = tooltipUI.activeSelf;
                Destroy(tooltipUI);
                tooltipUI = null;
                CreateWorldTooltipUI();
                if (tooltipUI != null) tooltipUI.SetActive(wasActive);
            }
        }

        public void SetupBox(FurnitureType type)
        {
            this.furnitureType = type;
            CreateWorldTooltipUI();
        }

        private void CreateWorldTooltipUI()
        {
            FurnitureItemDef def = FurnitureDatabase.GetDef(furnitureType);
            if (def == null) return;

            // World Space Tooltip Canvas
            tooltipUI = new GameObject("Box_Tooltip_Canvas");
            tooltipUI.transform.SetParent(transform, false);
            tooltipUI.transform.localPosition = new Vector3(0f, 0.75f, 0f); // Kolinin 75cm üstü
            tooltipUI.transform.localRotation = Quaternion.Euler(30f, -45f, 0f); // Kameraya açılı görünüm

            tooltipCanvas = tooltipUI.AddComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.WorldSpace;
            tooltipCanvas.sortingOrder = 50;

            RectTransform canvasRect = tooltipUI.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(300f, 130f);
            canvasRect.localScale = new Vector3(0.004f, 0.004f, 0.004f); // Dünya ölçeği

            // Arka Plan Paneli
            GameObject bgObj = new GameObject("Tooltip_Bg");
            bgObj.transform.SetParent(tooltipUI.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = UIStyleUtility.CreateOutlinePillSprite(300, 130, 14, 2, new Color(0.95f, 0.40f, 0.55f), new Color(0.10f, 0.12f, 0.16f, 0.95f));
            bgImg.raycastTarget = false;

            // Başlık (Ürün İsmi)
            GameObject titleObj = new GameObject("Tooltip_Title");
            titleObj.transform.SetParent(bgObj.transform, false);
            RectTransform tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(0f, 32f);
            tRect.sizeDelta = new Vector2(280f, 35f);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string boxPrefix = LocalizationManager.L("Label_BoxPrefix", "Koli:", "Box:");
            titleText.text = $"{def.iconEmoji} {boxPrefix} {def.LocalizedName}";
            titleText.fontSize = 26;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 16;
            titleText.resizeTextMaxSize = 28;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.raycastTarget = false;

            // Kurulum Bölgesi Etiketi
            GameObject zoneObj = new GameObject("Tooltip_Zone");
            zoneObj.transform.SetParent(bgObj.transform, false);
            RectTransform zRect = zoneObj.AddComponent<RectTransform>();
            zRect.anchoredPosition = new Vector2(0f, 0f);
            zRect.sizeDelta = new Vector2(280f, 30f);

            Text zoneText = zoneObj.AddComponent<Text>();
            zoneText.font = titleText.font;
            zoneText.text = def.GetZoneText();
            zoneText.fontSize = 21;
            zoneText.resizeTextForBestFit = true;
            zoneText.resizeTextMinSize = 13;
            zoneText.resizeTextMaxSize = 22;
            zoneText.fontStyle = FontStyle.Bold;
            zoneText.alignment = TextAnchor.MiddleCenter;
            zoneText.color = new Color(0.95f, 0.75f, 0.30f);
            zoneText.raycastTarget = false;

            // İpucu Alt Metni
            GameObject subObj = new GameObject("Tooltip_Sub");
            subObj.transform.SetParent(bgObj.transform, false);
            RectTransform sRect = subObj.AddComponent<RectTransform>();
            sRect.anchoredPosition = new Vector2(0f, -30f);
            sRect.sizeDelta = new Vector2(280f, 25f);

            Text subText = subObj.AddComponent<Text>();
            subText.font = titleText.font;
            subText.text = LocalizationManager.L("Box_ClickToOpenInventory", "👆 Tıkla: Palet Deposunu Aç", "👆 Click: Open Pallet Storage");
            subText.fontSize = 18;
            subText.resizeTextForBestFit = true;
            subText.resizeTextMinSize = 11;
            subText.resizeTextMaxSize = 19;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.80f, 0.85f, 0.90f);
            subText.raycastTarget = false;

            tooltipUI.SetActive(false); // Başlangıçta gizli
        }

        private void Update()
        {
            // Tooltip varsa kameraya doğru döndürme
            if (tooltipUI != null && tooltipUI.activeSelf && Camera.main != null)
            {
                tooltipUI.transform.rotation = Camera.main.transform.rotation;
            }
        }

        // --- HOVER VE MOUSE/TOUCH ETKİLEŞİMLERİ ---

        private void OnMouseEnter()
        {
            ShowHover(true);
        }

        private void OnMouseExit()
        {
            ShowHover(false);
        }

        private void OnMouseDown()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || IsPointerOverUIButton()) return;
            TriggerPlacement();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || IsPointerOverUIButton()) return;
            ShowHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ShowHover(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen) return;
            TriggerPlacement();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen) return;
            TriggerPlacement();
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
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
                return UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            if (UnityEngine.InputSystem.Pointer.current != null)
                return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return Vector2.zero;
#else
            try
            {
                if (Input.touchCount > 0) return Input.GetTouch(0).position;
                return Input.mousePosition;
            }
            catch { return Vector2.zero; }
#endif
        }

        public void ShowHover(bool active)
        {
            isHovered = active;
            if (tooltipUI != null)
            {
                tooltipUI.SetActive(active);
            }
        }

        public void TriggerPlacement()
        {
            ShowHover(false);
            PalletStorageInventoryModalUI.ShowModal();
        }
    }
}
