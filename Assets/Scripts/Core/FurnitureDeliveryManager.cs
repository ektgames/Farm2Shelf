using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.UI;
using Farm2Shelf.Environment;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Haritada Mal Kabul Kapısı yanında sabit Ahşap Teslimat Paletini (Delivery Pallet) oluşturan ve
    /// TrendyShop'tan alınan siparişlerin koli (DeliveryBox) olarak palet üstünde birikmesini sağlayan yönetici.
    /// Kamyon yanaşma alanına (X: 13, Z: 2) KESİNLİKLE engel olmaz.
    /// </summary>
    public class FurnitureDeliveryManager : MonoBehaviour
    {
        public static FurnitureDeliveryManager Instance { get; private set; }

        [Header("Palet Konum Ayarları")]
        // X: 11.8 (Mal kabul kapısı yanı), Y: 0.01, Z: 6.0 (Kamyon yanaşma alanı Z: -1..5 dışı!)
        private Vector3 palletPosition = new Vector3(11.8f, 0.01f, 6.0f);

        private GameObject palletObj;
        private Transform boxContainer;
        private readonly List<DeliveryBoxController> activeBoxes = new List<DeliveryBoxController>();
        private DeliveryBoxController currentHoveredBox = null;

        private Material palletWoodMat;
        private Material cardboardMat;
        private Material tapeMat;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            InitMaterials();
            CreateDeliveryPallet();
        }

        private void Update()
        {
            if (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing) return;
            if (IsPointerOverUI())
            {
                ClearHoveredBox();
                return;
            }

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector2 pointerPos = GetPointerPosition();
            Ray ray = mainCam.ScreenPointToRay(pointerPos);

            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                DeliveryBoxController box = null;
                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    box = h.collider.GetComponentInParent<DeliveryBoxController>();
                    if (box == null) box = h.collider.GetComponent<DeliveryBoxController>();
                    if (box != null) break;
                }

                if (box != null)
                {
                    if (currentHoveredBox != box)
                    {
                        ClearHoveredBox();
                        currentHoveredBox = box;
                        currentHoveredBox.ShowHover(true);
                    }

                    if (Farm2Shelf.Utils.TouchInputHelper.IsCleanTapThisFrame(out _))
                    {
                        box.TriggerPlacement();
                    }
                    return;
                }
            }

            ClearHoveredBox();
        }

        private void ClearHoveredBox()
        {
            if (currentHoveredBox != null)
            {
                currentHoveredBox.ShowHover(false);
                currentHoveredBox = null;
            }
        }

        private Vector2 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
                return UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            if (UnityEngine.InputSystem.Pointer.current != null)
                return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            return Vector2.zero;
#else
            try { return Input.mousePosition; }
            catch { return Vector2.zero; }
#endif
        }

        private bool WasPointerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                return true;
            return false;
#else
            try { return Input.GetMouseButtonDown(0); }
            catch { return false; }
#endif
        }

        private bool IsPointerOverUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;

            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = GetPointerPosition();
            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject != null && r.gameObject.GetComponentInParent<Button>() != null)
                {
                    return true; // Sadece ekrandaki aktif bir UI Butonuna (ör. EKT Phone, Dükkan Aç/Kapat) tıklanıyorsa
                }
            }

            return false;
        }

        private void InitMaterials()
        {
            if (palletWoodMat != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Lightweight Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            palletWoodMat = new Material(shader);
            palletWoodMat.color = new Color(0.60f, 0.40f, 0.20f); // Ahşap palet rengi

            cardboardMat = new Material(shader);
            cardboardMat.color = new Color(0.78f, 0.62f, 0.42f); // Karton koli rengi

            tapeMat = new Material(shader);
            tapeMat.color = new Color(0.85f, 0.75f, 0.40f); // Koli bandı
        }

        private void CreateDeliveryPallet()
        {
            if (palletObj != null) return;

            palletObj = new GameObject("Delivery_Pallet_Root");
            palletObj.transform.position = palletPosition;

            GameObject palletBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palletBase.name = "EuroPallet_Base";
            palletBase.transform.SetParent(palletObj.transform, false);
            palletBase.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            palletBase.transform.localScale = new Vector3(1.6f, 0.12f, 1.6f);
            palletBase.GetComponent<Renderer>().sharedMaterial = palletWoodMat;
            Destroy(palletBase.GetComponent<Collider>());

            for (float x = -0.6f; x <= 0.6f; x += 0.4f)
            {
                GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = "Pallet_Slat";
                slat.transform.SetParent(palletObj.transform, false);
                slat.transform.localPosition = new Vector3(x, 0.13f, 0f);
                slat.transform.localScale = new Vector3(0.12f, 0.03f, 1.6f);
                slat.GetComponent<Renderer>().sharedMaterial = palletWoodMat;
                Destroy(slat.GetComponent<Collider>());
            }

            GameObject labelObj = new GameObject("Pallet_Label");
            labelObj.transform.SetParent(palletObj.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.16f, 0.85f);
            labelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            boxContainer = new GameObject("Delivery_Boxes_Container").transform;
            boxContainer.SetParent(palletObj.transform, false);
        }

        public void AddOrdersToPallet(List<FurnitureType> items)
        {
            if (items == null || items.Count == 0) return;

            foreach (var itemType in items)
            {
                CreateCargoBoxOnPallet(itemType);
            }

            ReorganizeBoxesOnPallet();
        }

        public DeliveryBoxController CreateCargoBoxOnPallet(FurnitureType type)
        {
            InitMaterials();

            GameObject boxObj = new GameObject("CargoBox_" + type.ToString());
            boxObj.transform.SetParent(boxContainer, false);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "BoxBody";
            body.transform.SetParent(boxObj.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            body.GetComponent<Renderer>().sharedMaterial = cardboardMat;
            Destroy(body.GetComponent<Collider>());

            GameObject tapeX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tapeX.name = "TapeX";
            tapeX.transform.SetParent(boxObj.transform, false);
            tapeX.transform.localPosition = new Vector3(0f, 0.23f, 0f);
            tapeX.transform.localScale = new Vector3(0.56f, 0.01f, 0.12f);
            tapeX.GetComponent<Renderer>().sharedMaterial = tapeMat;
            Destroy(tapeX.GetComponent<Collider>());

            BoxCollider col = boxObj.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = new Vector3(0.58f, 0.48f, 0.58f);

            DeliveryBoxController controller = boxObj.AddComponent<DeliveryBoxController>();
            controller.SetupBox(type);

            activeBoxes.Add(controller);
            ReorganizeBoxesOnPallet();

            return controller;
        }

        /// <summary>
        /// Kolileri palet üzerinde 2x2 ızgara şeklinde üst üste düzenli olarak istifler.
        /// </summary>
        public void ReorganizeBoxesOnPallet()
        {
            // Temizlik (Yok olmuş nesneleri listeden çıkar)
            activeBoxes.RemoveAll(b => b == null || b.gameObject == null);

            Vector3[] gridOffsets = new Vector3[]
            {
                new Vector3(-0.38f, 0f, -0.38f),
                new Vector3( 0.38f, 0f, -0.38f),
                new Vector3(-0.38f, 0f,  0.38f),
                new Vector3( 0.38f, 0f,  0.38f)
            };

            float boxHeight = 0.48f;
            float baseOffsetY = 0.38f; // Palet üst yüksekliği

            for (int i = 0; i < activeBoxes.Count; i++)
            {
                int gridIdx = i % 4;
                int layerIdx = i / 4;

                Vector3 targetLocalPos = gridOffsets[gridIdx] + new Vector3(0f, baseOffsetY + (layerIdx * boxHeight), 0f);
                activeBoxes[i].transform.localPosition = targetLocalPos;
                activeBoxes[i].transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Yerleştirme moduna geçilen koliyi paletten kaldırır.
        /// </summary>
        public void RemoveBox(DeliveryBoxController controller)
        {
            if (controller != null && activeBoxes.Contains(controller))
            {
                activeBoxes.Remove(controller);
                Destroy(controller.gameObject);
                ReorganizeBoxesOnPallet();
            }
        }
    }
}
