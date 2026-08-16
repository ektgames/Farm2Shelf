using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Environment;
using Farm2Shelf.UI;
using Farm2Shelf.CameraSystem;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Mobilya önizleme (Ghost Preview) ve haritaya yerleştirme yöneticisi.
    /// 'Sadece Mağaza' veya 'Sadece Depo' kısıtlamalarını sıkı bir şekilde denetler.
    /// Mobilyalar, buzdolapları, tezgahlar, kasalar ve tüm dekorasyonların iç içe geçmesini ve duvarlarla çakışmasını engeller.
    /// Geçerli konumda yeşil şeffaf, geçersiz konumda kırmızı şeffaf gösterir.
    /// 'R' tuşu veya mobil Döndür butonu ile 90 derece döndürmeyi sağlar.
    /// </summary>
    public class FurniturePlacementManager : MonoBehaviour
    {
        public static FurniturePlacementManager Instance { get; private set; }

        public bool IsPlacing => isPlacing;
        private bool isPlacing = false;
        private FurnitureType currentType;
        private DeliveryBoxController sourceBox;
        private GameObject ghostObj;
        private float currentYRotation = 0f;

        private Transform placedFurnitureContainer;
        private Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Ekran üzeri kontrol butonları (Mobil Uyumlu HUD)
        private GameObject placementHUDCanvas;
        private Text infoStatusText;

        private float placementStartTime = 0f;
        private Vector3 originalReplacementPos;
        private Quaternion originalReplacementRot;
        private ShelfRowData[] savedReplacementRows;
        private bool isReinstalling = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            GameObject containerObj = GameObject.Find("Placed_Furniture_Container");
            if (containerObj == null) containerObj = new GameObject("Placed_Furniture_Container");
            placedFurnitureContainer = containerObj.transform;

            CreatePlacementHUDUI();
        }

        public void StartPlacement(FurnitureType type, DeliveryBoxController boxController)
        {
            ModalManager.SetModalOpen(false);
            if (FurnitureInfoModalUI.Instance != null) FurnitureInfoModalUI.Instance.CloseModal();

            if (isPlacing) CancelPlacement();

            this.currentType = type;
            this.sourceBox = boxController;
            this.isPlacing = true;
            this.isReinstalling = false;
            this.currentYRotation = 0f;
            this.placementStartTime = Time.time;

            ghostObj = FurnitureModelBuilder.CreateFurnitureModel(type, isGhost: true);
            ghostObj.name = "Ghost_" + type.ToString();

            if (placementHUDCanvas != null) placementHUDCanvas.SetActive(true);

            FurnitureItemDef def = FurnitureDatabase.GetDef(type);
            if (infoStatusText != null && def != null)
            {
                infoStatusText.text = $"🛠️ {def.name} Yerleştiriliyor\n{def.GetZoneText()} | [R/T] Mobilya Döndür | [WASD/QE] Kamera | [Sol Tık] Kur | [Sağ Tık/ESC] İptal";
            }
        }

        public void StartReplacement(FurnitureType type, Vector3 origPos, Quaternion origRot, ShelfRowData[] existingRows = null)
        {
            ModalManager.SetModalOpen(false);
            if (FurnitureInfoModalUI.Instance != null) FurnitureInfoModalUI.Instance.CloseModal();

            if (isPlacing) CancelPlacement();

            this.currentType = type;
            this.sourceBox = null;
            this.isPlacing = true;
            this.isReinstalling = true;
            this.originalReplacementPos = origPos;
            this.originalReplacementRot = origRot;
            this.savedReplacementRows = existingRows;
            this.currentYRotation = origRot.eulerAngles.y;
            this.placementStartTime = Time.time;

            ghostObj = FurnitureModelBuilder.CreateFurnitureModel(type, isGhost: true);
            ghostObj.name = "Ghost_" + type.ToString();

            if (placementHUDCanvas != null) placementHUDCanvas.SetActive(true);

            FurnitureItemDef def = FurnitureDatabase.GetDef(type);
            if (infoStatusText != null && def != null)
            {
                infoStatusText.text = $"🛠️ {def.name} Taşınıyor\n{def.GetZoneText()} | [R/T] Mobilya Döndür | [WASD/QE] Kamera | [Sol Tık] Kur | [Sağ Tık/ESC] Eski Yere İade";
            }
        }

        private void Update()
        {
            if (!isPlacing)
            {
                return;
            }

            ModalManager.SetModalOpen(false);

            if (ghostObj == null) return;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector2 pointerPos = GetPointerPosition();
            Ray ray = mainCam.ScreenPointToRay(pointerPos);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                hitPoint.x = Mathf.Round(hitPoint.x * 4f) / 4f;
                hitPoint.z = Mathf.Round(hitPoint.z * 4f) / 4f;
                hitPoint.y = 0.01f;

                ghostObj.transform.position = hitPoint;
                ghostObj.transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);

                FurnitureItemDef def = FurnitureDatabase.GetDef(currentType);
                bool isZoneValid = IsValidPlacementZone(hitPoint, def != null ? def.zone : FurnitureZone.StoreOnly);
                bool isOverlapping = IsOverlappingAnyObject(hitPoint, currentYRotation, currentType);

                bool isValid = isZoneValid && !isOverlapping;

                Material targetGhostMat = isValid ? FurnitureModelBuilder.ValidGhostMaterial : FurnitureModelBuilder.InvalidGhostMaterial;
                FurnitureModelBuilder.ApplyGhostMaterial(ghostObj, targetGhostMat);

                if (infoStatusText != null && def != null)
                {
                    if (isValid)
                    {
                        infoStatusText.text = $"🛠️ {def.name} Yerleştiriliyor (Konum Uygun ✅)\n{def.GetZoneText()} | [R/T] Döndür | [Sol Tık] Kur | [Sağ Tık/ESC] İptal";
                        infoStatusText.color = Color.white;
                    }
                    else
                    {
                        infoStatusText.text = $"⚠️ GEÇERSİZ KONUM! (Duvar, Geçiş veya Başka Bir Nesne ile İç İçer Çakışıyor!)\n{def.GetZoneText()} | [R/T] Döndür | Temiz ve Boş Bir Alana Taşıyın";
                        infoStatusText.color = new Color(1.0f, 0.45f, 0.45f);
                    }
                }

                if (WasRotatePressed())
                {
                    RotatePlacement(90f);
                }
                else if (WasRotateCCWPressed())
                {
                    RotatePlacement(-90f);
                }

                if (Time.time - placementStartTime > 0.15f && WasLeftClicked() && !IsPointerOverUI())
                {
                    if (isValid)
                    {
                        ConfirmPlacement(hitPoint, Quaternion.Euler(0f, currentYRotation, 0f));
                    }
                    else
                    {
                        Debug.LogWarning($"[Placement] Geçersiz konum! {def.name} duvarlar, kapılar veya başka mobilyalarla çakışıyor!");
                    }
                }
                else if (WasRightClicked() || WasCancelPressed())
                {
                    CancelPlacement();
                }
            }
        }

        // --- HİBRİT INPUT SYSTEM & LEGACY INPUT OKUYUCULARI ---
        private Vector2 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Pointer.current != null)
                return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return Vector2.zero;
#else
            try { return Input.mousePosition; }
            catch { return Vector2.zero; }
#endif
        }

        private bool WasLeftClicked()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            return false;
#else
            try { return Input.GetMouseButtonDown(0); }
            catch { return false; }
#endif
        }

        private bool WasRightClicked()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                return true;
            return false;
#else
            try { return Input.GetMouseButtonDown(1); }
            catch { return false; }
#endif
        }

        private bool WasRotatePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                return true;
            return false;
#else
            try { return Input.GetKeyDown(KeyCode.R); }
            catch { return false; }
#endif
        }

        private bool WasRotateCCWPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
                return true;
            return false;
#else
            try { return Input.GetKeyDown(KeyCode.T); }
            catch { return false; }
#endif
        }

        private bool WasCancelPressed()
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

        public bool IsValidPlacementZone(Vector3 pos, FurnitureZone zone)
        {
            EnvironmentBuilder env = EnvironmentBuilder.Instance;
            int level = (env != null) ? env.CurrentUpgradeLevel : 1;

            float frontWallZ = -3.0f;
            float storeDepth = (level == 1) ? 18.0f : ((level == 2) ? 27.0f : 36.0f);
            float storageDepth = (level == 1) ? 9.5f : ((level == 2) ? 14.5f : 19.5f);

            if (zone == FurnitureZone.StoreOnly)
            {
                bool xValid = pos.x >= -12.5f && pos.x <= 2.5f;
                bool zValid = pos.z >= -2.5f && pos.z <= (frontWallZ + storeDepth - 0.8f);
                return xValid && zValid;
            }
            else if (zone == FurnitureZone.StorageOnly)
            {
                bool xValid = pos.x >= 3.5f && pos.x <= 10.5f;
                bool zValid = pos.z >= -2.5f && pos.z <= (frontWallZ + storageDepth - 0.8f);
                return xValid && zValid;
            }

            return false;
        }

        public void RotatePlacement(float deltaAngle = 90f)
        {
            currentYRotation = (currentYRotation + deltaAngle + 360f) % 360f;
        }

        public void ConfirmPlacement(Vector3 pos, Quaternion rot)
        {
            if (!isPlacing) return;

            InstantiatePlacedFurniture(currentType, pos, rot, savedReplacementRows);

            if (sourceBox != null && FurnitureDeliveryManager.Instance != null)
            {
                FurnitureDeliveryManager.Instance.RemoveBox(sourceBox);
            }

            CleanupGhost();
            isPlacing = false;
            isReinstalling = false;
            savedReplacementRows = null;
            if (placementHUDCanvas != null) placementHUDCanvas.SetActive(false);

            Debug.Log($"[FurniturePlacement] {currentType} başarıyla kuruldu! Konum: {pos}");
        }

        public Vector2 GetFurnitureFootprintSize(FurnitureType type)
        {
            switch (type)
            {
                case FurnitureType.Shelf:
                case FurnitureType.Fridge:
                case FurnitureType.CosmeticShelf:
                case FurnitureType.ProduceShelf:
                case FurnitureType.BakeryCounter:
                case FurnitureType.ButcherCounter:
                case FurnitureType.ElectronicsShelf:
                    return new Vector2(1.6f, 1.0f);

                case FurnitureType.Freezer:
                    return new Vector2(1.8f, 1.0f);

                case FurnitureType.StorageShelf:
                    return new Vector2(2.2f, 1.1f);

                case FurnitureType.Cashier:
                    return new Vector2(2.0f, 1.2f);

                case FurnitureType.CustomerServiceDesk:
                    return new Vector2(1.8f, 1.1f);

                case FurnitureType.ShoppingCart:
                    return new Vector2(1.2f, 0.9f);

                case FurnitureType.PlantPot:
                case FurnitureType.PottedPalm:
                case FurnitureType.TrashCan:
                case FurnitureType.GumballMachine:
                case FurnitureType.WaterDispenser:
                    return new Vector2(0.7f, 0.7f);

                case FurnitureType.BenchWood:
                    return new Vector2(1.5f, 0.7f);

                case FurnitureType.DividerFence:
                    return new Vector2(1.5f, 0.4f);

                case FurnitureType.CoffeeMachine:
                case FurnitureType.VendingSnack:
                case FurnitureType.IceCreamCart:
                    return new Vector2(1.2f, 0.9f);

                case FurnitureType.WelcomeMat:
                case FurnitureType.RedCarpet:
                    return new Vector2(1.5f, 1.0f);

                case FurnitureType.FountainSmall:
                    return new Vector2(1.8f, 1.8f);

                default:
                    return new Vector2(1.2f, 1.0f);
            }
        }

        public bool IsOverlappingAnyObject(Vector3 pos, float rotationY, FurnitureType type)
        {
            Vector2 size = GetFurnitureFootprintSize(type);
            float angleRad = rotationY * Mathf.Deg2Rad;
            bool isRotated = (Mathf.Abs(Mathf.Sin(angleRad)) > 0.5f);
            float width = isRotated ? size.y : size.x;
            float depth = isRotated ? size.x : size.y;

            float halfW = (width / 2f) + 0.05f;
            float halfD = (depth / 2f) + 0.05f;

            Bounds ghostBounds = new Bounds(
                new Vector3(pos.x, 0.9f, pos.z),
                new Vector3(width - 0.04f, 1.8f, depth - 0.04f)
            );

            // 1. DUVAR VE GEÇİŞ ALANI KONTROLLERİ
            EnvironmentBuilder env = EnvironmentBuilder.Instance;
            int level = (env != null) ? env.CurrentUpgradeLevel : 1;
            float frontWallZ = -3.0f;
            float storeDepth = (level == 1) ? 18.0f : ((level == 2) ? 27.0f : 36.0f);
            float storageDepth = (level == 1) ? 9.5f : ((level == 2) ? 14.5f : 19.5f);
            float backWallZ = frontWallZ + storeDepth;
            float storageBackZ = frontWallZ + storageDepth;

            FurnitureItemDef def = FurnitureDatabase.GetDef(type);
            FurnitureZone zone = (def != null) ? def.zone : FurnitureZone.StoreOnly;

            if (zone == FurnitureZone.StoreOnly)
            {
                float minX = -12.5f + halfW;
                float maxX = 2.5f - halfW;
                float minZ = -2.5f + halfD;
                float maxZ = (backWallZ - 0.6f) - halfD;

                if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ)
                {
                    return true;
                }

                if (pos.z <= -1.8f && pos.x >= -6.8f && pos.x <= -3.2f)
                {
                    return true;
                }
            }
            else if (zone == FurnitureZone.StorageOnly)
            {
                float minX = 3.5f + halfW;
                float maxX = 10.5f - halfW;
                float minZ = -2.5f + halfD;
                float maxZ = (storageBackZ - 0.6f) - halfD;

                if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ)
                {
                    return true;
                }

                if (pos.x <= 4.2f && pos.z >= 0.2f && pos.z <= 3.8f)
                {
                    return true;
                }
            }

            // 2. MEVCUT YERLEŞTİRİLMİŞ TÜM MOBİLYALAR / DEKORASYONLAR İLE ÇAKIŞMA KONTROLÜ
            PlacedFurnitureController[] placedFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            foreach (var f in placedFurniture)
            {
                if (f == null) continue;

                if (isReinstalling && savedReplacementRows != null && Vector3.Distance(f.transform.position, originalReplacementPos) < 0.2f)
                {
                    continue;
                }

                Vector2 fFootprint = GetFurnitureFootprintSize(f.FurnitureType);
                float fAngleRad = f.transform.eulerAngles.y * Mathf.Deg2Rad;
                bool fRotated = (Mathf.Abs(Mathf.Sin(fAngleRad)) > 0.5f);
                float fW = fRotated ? fFootprint.y : fFootprint.x;
                float fD = fRotated ? fFootprint.x : fFootprint.y;

                Bounds existingBounds = new Bounds(
                    new Vector3(f.transform.position.x, 0.9f, f.transform.position.z),
                    new Vector3(fW - 0.04f, 1.8f, fD - 0.04f)
                );

                BoxCollider bCol = f.GetComponent<BoxCollider>();
                if (bCol != null)
                {
                    existingBounds = bCol.bounds;
                    existingBounds.center = new Vector3(existingBounds.center.x, 0.9f, existingBounds.center.z);
                    existingBounds.size = new Vector3(existingBounds.size.x - 0.04f, 1.8f, existingBounds.size.z - 0.04f);
                }

                if (ghostBounds.Intersects(existingBounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void InstantiatePlacedFurniture(FurnitureType type, Vector3 pos, Quaternion rot, ShelfRowData[] existingRows = null)
        {
            GameObject realFurniture = FurnitureModelBuilder.CreateFurnitureModel(type, isGhost: false);
            realFurniture.name = type.ToString() + "_" + System.Guid.NewGuid().ToString().Substring(0, 5);
            realFurniture.transform.SetParent(placedFurnitureContainer, false);
            realFurniture.transform.position = pos;
            realFurniture.transform.rotation = rot;

            Vector2 footprint = GetFurnitureFootprintSize(type);
            BoxCollider col = realFurniture.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.size = new Vector3(footprint.x, 1.8f, footprint.y);

            PlacedFurnitureController placedCtrl = realFurniture.AddComponent<PlacedFurnitureController>();
            placedCtrl.Setup(type, pos, rot, existingRows);
        }

        public void CancelPlacement()
        {
            if (isReinstalling)
            {
                InstantiatePlacedFurniture(currentType, originalReplacementPos, originalReplacementRot, savedReplacementRows);
                isReinstalling = false;
                savedReplacementRows = null;
            }

            CleanupGhost();
            isPlacing = false;
            if (placementHUDCanvas != null) placementHUDCanvas.SetActive(false);
            Debug.Log("[FurniturePlacement] Yerleştirme iptal edildi.");
        }

        private void CleanupGhost()
        {
            if (ghostObj != null)
            {
                Destroy(ghostObj);
                ghostObj = null;
            }
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
                    return true;
                }
            }

            return false;
        }

        public void ConfirmCurrentPlacement()
        {
            if (!isPlacing || ghostObj == null) return;

            Vector3 pos = ghostObj.transform.position;
            Quaternion rot = ghostObj.transform.rotation;

            FurnitureItemDef def = FurnitureDatabase.GetDef(currentType);
            bool isZoneValid = IsValidPlacementZone(pos, def != null ? def.zone : FurnitureZone.StoreOnly);
            bool isOverlapping = IsOverlappingAnyObject(pos, currentYRotation, currentType);

            if (isZoneValid && !isOverlapping)
            {
                ConfirmPlacement(pos, rot);
            }
            else
            {
                ModalManager.ShowModal("Geçersiz Konum! ⚠️", "Seçtiğiniz konum duvarlar, kapılar veya başka bir mobilya ile çakışıyor!\n\nLütfen mobilyayı temiz ve boş bir alana taşıyın.", "Tamam");
            }
        }

        private void CreatePlacementHUDUI()
        {
            GameObject existing = GameObject.Find("Placement_HUD_Canvas");
            if (existing != null) DestroyImmediate(existing);

            placementHUDCanvas = new GameObject("Placement_HUD_Canvas");
            Canvas canvas = placementHUDCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;

            CanvasScaler scaler = placementHUDCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            placementHUDCanvas.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("HUD_BottomBar");
            panel.transform.SetParent(placementHUDCanvas.transform, false);

            RectTransform pRect = panel.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0f);
            pRect.anchorMax = new Vector2(0.5f, 0f);
            pRect.pivot = new Vector2(0.5f, 0f);
            pRect.anchoredPosition = new Vector2(0f, 40f);
            pRect.sizeDelta = new Vector2(880f, 90f);

            Image bg = panel.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateOutlinePillSprite(880, 90, 16, 2, new Color(0.95f, 0.40f, 0.55f), new Color(0.12f, 0.15f, 0.20f, 0.95f));
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("HUD_InfoText");
            textObj.transform.SetParent(panel.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(-190f, 0f);
            tRect.sizeDelta = new Vector2(460f, 75f);

            infoStatusText = textObj.AddComponent<Text>();
            infoStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoStatusText.raycastTarget = false;
            infoStatusText.text = "🛠️ Mobilya Yerleştiriliyor...";
            infoStatusText.fontSize = 18;
            infoStatusText.alignment = TextAnchor.MiddleCenter;
            infoStatusText.color = Color.white;

            // 1. KUR / YERLEŞTİR BUTONU (Mobil ve PC için Ekran Üzeri Yeşil Onay Butonu)
            CreateHUDButton(panel.transform, new Vector2(120f, 0f), new Vector2(110f, 55f), "✅ Kur", new Color(0.20f, 0.78f, 0.35f), () => {
                ConfirmCurrentPlacement();
            });

            // 2. DÖNDÜR BUTONU (Mobil ve PC için Mavi Döndürme Butonu)
            CreateHUDButton(panel.transform, new Vector2(245f, 0f), new Vector2(110f, 55f), "🔄 Döndür", new Color(0.20f, 0.55f, 0.85f), () => {
                RotatePlacement();
            });

            // 3. İPTAL BUTONU (Kırmızı İptal Butonu)
            CreateHUDButton(panel.transform, new Vector2(370f, 0f), new Vector2(110f, 55f), "❌ İptal", new Color(0.85f, 0.25f, 0.25f), () => {
                CancelPlacement();
            });

            placementHUDCanvas.SetActive(false);
        }

        private GameObject CreateHUDButton(Transform parent, Vector2 pos, Vector2 size, string label, Color bgCol, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("HUD_Btn_" + label);
            btnObj.transform.SetParent(parent, false);

            RectTransform r = btnObj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = UIStyleUtility.CreateRoundedPillSprite((int)size.x, (int)size.y, 10, bgCol);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = txtObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = label;
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btnObj;
        }
    }
}
