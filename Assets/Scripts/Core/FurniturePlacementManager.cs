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
            if (EKTPhoneManager.Instance != null) EKTPhoneManager.Instance.ClosePhoneTabletInstant();
            PalletStorageInventoryModalUI.HideModal();
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

            FurnitureItemDef def = FurnitureDatabase.GetDef(type);
            Vector3 startPos = (def != null && def.zone == FurnitureZone.StorageOnly)
                ? new Vector3(7.0f, 0.01f, 4.5f)
                : new Vector3(-5.0f, 0.01f, 4.0f);

            ghostObj.transform.position = startPos;
            ghostObj.transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);

            SetFloorGridVisible(true);
            if (placementHUDCanvas != null) placementHUDCanvas.SetActive(true);

            if (infoStatusText != null && def != null)
            {
                string infoFmt = LocalizationManager.L(
                    "HUD_PlacingInfo",
                    "🛠️ {0} Yerleştiriliyor\nEkrana dokunarak taşıyın | Paneldeki [✅ Kur] butonuna basın",
                    "🛠️ Placing {0}\nDrag on screen to move | Tap [✅ Assemble] to place"
                );
                infoStatusText.text = string.Format(infoFmt, def.LocalizedName);
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
            ghostObj.transform.position = origPos;
            ghostObj.transform.rotation = origRot;

            SetFloorGridVisible(true);
            if (placementHUDCanvas != null) placementHUDCanvas.SetActive(true);

            FurnitureItemDef def = FurnitureDatabase.GetDef(type);
            if (infoStatusText != null && def != null)
            {
                infoStatusText.text = $"🛠️ {def.name} Taşınıyor\nEkrana dokunarak taşıyın | Paneldeki [✅ Kur] butonuna basarak kurun";
            }
        }

        private float lastKeyMoveTime = 0f;

        private void Update()
        {
            if (!isPlacing)
            {
                return;
            }

            ModalManager.SetModalOpen(false);

            if (ghostObj == null) return;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // 1. DOKUNMA VEYA FARE TIKLAMASI/SÜRÜKLEMESİ:
                // Ekrana dokunulduğunda veya tıklandığında anında o konuma taşınır.
                // Parmağı/fareyi bıraktığınızda mobilya tam olarak orada kalır!
                if (IsAnyPointerPressed(out Vector2 pointerPos, out bool isTouchInput))
                {
                    if (!IsPointerOverUIButton(pointerPos))
                    {
                        Ray ray = mainCam.ScreenPointToRay(pointerPos);
                        Plane floorPlane = new Plane(Vector3.up, new Vector3(0f, 0.01f, 0f));

                        if (floorPlane.Raycast(ray, out float enter))
                        {
                            Vector3 hitPoint = ray.GetPoint(enter);
                            hitPoint.x = Mathf.Round(hitPoint.x * 4f) / 4f; // 0.25m hassas ızgara yapışması
                            hitPoint.z = Mathf.Round(hitPoint.z * 4f) / 4f;
                            hitPoint.y = 0.01f;

                            // Sınır güvenliği
                            hitPoint.x = Mathf.Clamp(hitPoint.x, -14f, 12f);
                            hitPoint.z = Mathf.Clamp(hitPoint.z, -3f, 25f);

                            ghostObj.transform.position = hitPoint;
                        }
                    }
                }

                // 2. KLAVYE WASD VE YÖN TUŞLARI İLE ADIM ADIM İLERLETME DESTEĞİ:
                Vector3 keyMove = Vector3.zero;
                if (IsKeyHeld(KeyCode.W) || IsKeyHeld(KeyCode.UpArrow)) keyMove.z += 0.25f;
                if (IsKeyHeld(KeyCode.S) || IsKeyHeld(KeyCode.DownArrow)) keyMove.z -= 0.25f;
                if (IsKeyHeld(KeyCode.A) || IsKeyHeld(KeyCode.LeftArrow)) keyMove.x -= 0.25f;
                if (IsKeyHeld(KeyCode.D) || IsKeyHeld(KeyCode.RightArrow)) keyMove.x += 0.25f;

                if (keyMove != Vector3.zero && Time.time - lastKeyMoveTime > 0.10f)
                {
                    lastKeyMoveTime = Time.time;
                    NudgeGhost(keyMove.x, keyMove.z);
                }
            }

            ghostObj.transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);

            FurnitureItemDef def = FurnitureDatabase.GetDef(currentType);
            Vector3 currentGhostPos = ghostObj.transform.position;
            bool isZoneValid = IsValidPlacementZone(currentGhostPos, def != null ? def.zone : FurnitureZone.StoreOnly);
            bool isOverlapping = IsOverlappingAnyObject(currentGhostPos, currentYRotation, currentType);

            bool isValid = isZoneValid && !isOverlapping;

            Material targetGhostMat = isValid ? FurnitureModelBuilder.ValidGhostMaterial : FurnitureModelBuilder.InvalidGhostMaterial;
            FurnitureModelBuilder.ApplyGhostMaterial(ghostObj, targetGhostMat);

            if (infoStatusText != null && def != null)
            {
                if (isValid)
                {
                    infoStatusText.text = $"🛠️ {def.name} Taşınıyor (Konum UYGUN ✅)\nEkrana dokunarak taşıyın | Paneldeki [✅ Kur] butonuna basın";
                    infoStatusText.color = Color.white;
                }
                else
                {
                    infoStatusText.text = $"⚠️ GEÇERSİZ KONUM! (Çakışma var!)\nBoş bir alana taşıyın | [🔄 Döndür] ile yön değiştirin";
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
            else if (WasConfirmKeyPressed())
            {
                ConfirmCurrentPlacement();
            }
            else if (WasRightClicked() || WasCancelPressed())
            {
                CancelPlacement();
            }
        }

        // --- HİBRİT INPUT SYSTEM & LEGACY INPUT OKUYUCULARI ---
        private bool IsAnyPointerPressed(out Vector2 pointerPos, out bool isTouch)
        {
            pointerPos = Vector2.zero;
            isTouch = false;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
                if (touch.press.isPressed)
                {
                    pointerPos = touch.position.ReadValue();
                    isTouch = true;
                    if (pointerPos.sqrMagnitude > 1f) return true;
                }
            }

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                if (UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
                {
                    pointerPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    isTouch = false;
                    if (pointerPos.sqrMagnitude > 1f) return true;
                }
            }

            if (UnityEngine.InputSystem.Pointer.current != null)
            {
                if (UnityEngine.InputSystem.Pointer.current.press.isPressed)
                {
                    pointerPos = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
                    isTouch = false;
                    if (pointerPos.sqrMagnitude > 1f) return true;
                }
            }
#else
            try
            {
                if (Input.touchCount > 0)
                {
                    pointerPos = Input.GetTouch(0).position;
                    isTouch = true;
                    return true;
                }
                if (Input.GetMouseButton(0))
                {
                    pointerPos = (Vector2)Input.mousePosition;
                    isTouch = false;
                    return true;
                }
            }
            catch {}
#endif

            return false;
        }

        private Vector2 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
                if (touch.press.isPressed)
                {
                    return touch.position.ReadValue();
                }
            }
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
#else
            try
            {
                if (Input.touchCount > 0) return Input.GetTouch(0).position;
                Vector3 mPos = Input.mousePosition;
                if (mPos.sqrMagnitude > 0.01f) return new Vector2(mPos.x, mPos.y);
            }
            catch { }
#endif

            return Vector2.zero;
        }

        private bool IsKeyHeld(KeyCode code)
        {
            try { if (Input.GetKey(code)) return true; } catch { }

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (code == KeyCode.W && UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) return true;
                if (code == KeyCode.S && UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) return true;
                if (code == KeyCode.A && UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) return true;
                if (code == KeyCode.D && UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) return true;
                if (code == KeyCode.UpArrow && UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed) return true;
                if (code == KeyCode.DownArrow && UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed) return true;
                if (code == KeyCode.LeftArrow && UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed) return true;
                if (code == KeyCode.RightArrow && UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed) return true;
            }
#endif

            return false;
        }

        private bool WasLeftClickHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
                return true;
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
                return true;
            if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.isPressed)
                return true;
            return false;
#else
            try { return Input.GetMouseButton(0); }
            catch { return false; }
#endif
        }

        private bool WasCleanTap()
        {
            return Farm2Shelf.Utils.TouchInputHelper.IsCleanTapThisFrame(out _);
        }

        private bool WasConfirmKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
               (UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame))
                return true;
            return false;
#else
            try { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space); }
            catch { return false; }
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
            else if (FurnitureDeliveryManager.Instance != null)
            {
                FurnitureDeliveryManager.Instance.RemoveOneBoxOfType(currentType);
            }

            CleanupGhost();
            isPlacing = false;
            isReinstalling = false;
            savedReplacementRows = null;
            SetFloorGridVisible(false);
            if (placementHUDCanvas != null) placementHUDCanvas.SetActive(false);

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotifyFurniturePlaced(currentType);
            }

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

            float minXEdge = pos.x - halfW;
            float maxXEdge = pos.x + halfW;
            float minZEdge = pos.z - halfD;
            float maxZEdge = pos.z + halfD;

            if (zone == FurnitureZone.StoreOnly)
            {
                float minX = -12.6f + halfW;
                float maxX = 2.6f - halfW;
                float minZ = -2.5f + halfD;
                float maxZ = (backWallZ - 0.5f) - halfD;

                if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ)
                {
                    return true;
                }

                // DUVARA YAKINLIK VE SIKIŞMA KONTROLÜ (ZORUNLU GEÇİŞ KORİDORU VEYA SIFIR YASLANMA):
                // Karakterlerin mobilya ile duvar arasına sıkışmaması için:
                // - Duvar ile mobilya arasında ya tam sıfır kalmalı (< 0.18m)
                // - Ya da en az 0.95m yürüme koridoru boşluğu bulunmalıdır!
                if (!PlacedFurnitureController.IsWalkableFloorDecoration(type))
                {
                    // Sol Duvar Sıkışma Kontrolü (x = -12.8f)
                    float gapLeft = minXEdge - (-12.8f);
                    if (gapLeft > 0.18f && gapLeft < 0.95f) return true;

                    // Sağ Bölme Duvarı Sıkışma Kontrolü (x = 2.8f)
                    float gapRight = 2.8f - maxXEdge;
                    if (gapRight > 0.18f && gapRight < 0.95f) return true;

                    // Arka Duvar Sıkışma Kontrolü (z = backWallZ - 0.2f)
                    float gapBack = (backWallZ - 0.2f) - maxZEdge;
                    if (gapBack > 0.18f && gapBack < 0.95f) return true;
                }

                // Ana Giriş Kapısı Koridoru
                if (pos.z <= -1.6f && pos.x >= -6.8f && pos.x <= -3.2f)
                {
                    return true;
                }
            }
            else if (zone == FurnitureZone.StorageOnly)
            {
                float minX = 3.4f + halfW;
                float maxX = 10.6f - halfW;
                float minZ = -2.5f + halfD;
                float maxZ = (storageBackZ - 0.5f) - halfD;

                if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ)
                {
                    return true;
                }

                if (!PlacedFurnitureController.IsWalkableFloorDecoration(type))
                {
                    // Depo Sol Duvar Sıkışma Kontrolü (x = 3.2f)
                    float gapStorageLeft = minXEdge - 3.2f;
                    if (gapStorageLeft > 0.18f && gapStorageLeft < 0.95f) return true;

                    // Depo Sağ Duvar Sıkışma Kontrolü (x = 10.8f)
                    float gapStorageRight = 10.8f - maxXEdge;
                    if (gapStorageRight > 0.18f && gapStorageRight < 0.95f) return true;

                    // Depo Arka Duvar Sıkışma Kontrolü
                    float gapStorageBack = (storageBackZ - 0.2f) - maxZEdge;
                    if (gapStorageBack > 0.18f && gapStorageBack < 0.95f) return true;
                }

                if (pos.x <= 4.4f && pos.z >= -0.2f && pos.z <= 4.0f)
                {
                    return true;
                }
            }

            // 2. MEVCUT YERLEŞTİRİLMİŞ TÜM MOBİLYALAR / DEKORASYONLAR İLE ÇAKIŞMA VE DAR SIKIŞMA KORİDORU KONTROLÜ
            bool isCurrentWalkable = PlacedFurnitureController.IsWalkableFloorDecoration(type);
            var placedFurniture = PlacedFurnitureController.AllPlacedFurniture;
            int count = placedFurniture.Count;

            for (int i = 0; i < count; i++)
            {
                var f = placedFurniture[i];
                if (f == null) continue;

                if (isReinstalling && Vector3.Distance(f.transform.position, originalReplacementPos) < 0.2f)
                {
                    continue;
                }

                Vector2 fFootprint = GetFurnitureFootprintSize(f.FurnitureType);
                float fAngleRad = f.transform.eulerAngles.y * Mathf.Deg2Rad;
                bool fRotated = (Mathf.Abs(Mathf.Sin(fAngleRad)) > 0.5f);
                float fW = fRotated ? fFootprint.y : fFootprint.x;
                float fD = fRotated ? fFootprint.x : fFootprint.y;

                float fMinX = f.transform.position.x - fW / 2f;
                float fMaxX = f.transform.position.x + fW / 2f;
                float fMinZ = f.transform.position.z - fD / 2f;
                float fMaxZ = f.transform.position.z + fD / 2f;

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
                    fMinX = existingBounds.min.x;
                    fMaxX = existingBounds.max.x;
                    fMinZ = existingBounds.min.z;
                    fMaxZ = existingBounds.max.z;
                }

                // A. Doğrudan Gövde Çakışması (Overlap) Kontrolü
                if (ghostBounds.Intersects(existingBounds))
                {
                    return true;
                }

                // B. İki Katı Mobilya Arasındaki Dar Sıkışma Koridoru Kontrolü (0.18m ile 0.88m Arasındaki Dar Boşluklar YASAK!):
                if (!isCurrentWalkable && !PlacedFurnitureController.IsWalkableFloorDecoration(f.FurnitureType))
                {
                    float gapX = Mathf.Max(0f, Mathf.Max(minXEdge - fMaxX, fMinX - maxXEdge));
                    float gapZ = Mathf.Max(0f, Mathf.Max(minZEdge - fMaxZ, fMinZ - maxZEdge));

                    bool isXAligned = (minZEdge < fMaxZ && maxZEdge > fMinZ);
                    bool isZAligned = (minXEdge < fMaxX && maxXEdge > fMinX);

                    if (isXAligned && gapX > 0.18f && gapX < 0.88f)
                    {
                        return true;
                    }
                    if (isZAligned && gapZ > 0.18f && gapZ < 0.88f)
                    {
                        return true;
                    }
                }
            }

            // 3. KAPI VEYA OK YÖNÜ KORİDOR GEÇİŞİ TIKALI MI?
            if (IsFrontOrDoorwayBlocked(pos, rotationY, type))
            {
                return true;
            }

            return false;
        }

        public bool IsFrontOrDoorwayBlocked(Vector3 pos, float rotationY, FurnitureType type)
        {
            if (PlacedFurnitureController.IsWalkableFloorDecoration(type)) return false;

            Vector3 frontDir = Quaternion.Euler(0f, rotationY, 0f) * Vector3.forward;
            float checkDist = 0.5f; // Ok yönü hafif mesafe kontrolü
            Vector3 frontCheckPos = pos + frontDir * checkDist;

            // A. Kapı Önleri Geçiş Koridoru (Dükkan Ana Girişi Tam Önü)
            // Ana Kapı Geçiş Yolu (-6.0f ile -4.0f arası, z <= -1.2f)
            if (pos.x >= -6.0f && pos.x <= -4.0f && pos.z <= -1.2f)
            {
                return true;
            }

            // B. Depo Kapı Geçiş Yolu (x: 2.2f - 4.2f, z: 0.0f - 3.5f)
            if (pos.x >= 2.2f && pos.x <= 4.2f && pos.z >= 0.0f && pos.z <= 3.5f)
            {
                return true;
            }

            // C. Diğer Mobilyaların Ön Ok Yönüne / Gövdesine Çakışma Kontrolü
            var allFurniture = PlacedFurnitureController.AllPlacedFurniture;
            int fCount = allFurniture.Count;
            for (int i = 0; i < fCount; i++)
            {
                var f = allFurniture[i];
                if (f == null) continue;
                if (PlacedFurnitureController.IsWalkableFloorDecoration(f.FurnitureType)) continue;

                if (isReinstalling && Vector3.Distance(f.transform.position, originalReplacementPos) < 0.2f)
                {
                    continue;
                }

                // Sırt Sırta Koyma İstisnası:
                // Eğer iki raf sırt sırta bakıyorsa (yani bu rafın ön ok yönü diğer rafın arkasına denk gelmiyorsa), sırt sırta koymaya izin verilir.
                Vector2 fFootprint = GetFurnitureFootprintSize(f.FurnitureType);
                float fAngleRad = f.transform.eulerAngles.y * Mathf.Deg2Rad;
                bool fRotated = (Mathf.Abs(Mathf.Sin(fAngleRad)) > 0.5f);
                float fW = fRotated ? fFootprint.y : fFootprint.x;
                float fD = fRotated ? fFootprint.x : fFootprint.y;

                Bounds fBodyBounds = new Bounds(
                    new Vector3(f.transform.position.x, 0.9f, f.transform.position.z),
                    new Vector3(fW - 0.10f, 1.8f, fD - 0.10f)
                );

                if (fBodyBounds.Contains(new Vector3(frontCheckPos.x, 0.9f, frontCheckPos.z)))
                {
                    // Ok yönü doğrudan diğer rafın gövdesine basıyor ve geçiş koridorunu tıkıyorsa engelle!
                    Vector3 otherFrontDir = f.transform.forward;
                    float dotDir = Vector3.Dot(frontDir, otherFrontDir);

                    // Eğer zıt yöne (sırt sırta) bakmıyorlarsa engelle!
                    if (dotDir > -0.7f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void SpawnRestoredFurniture(FurnitureType type, Vector3 pos, Quaternion rot, ShelfRowData[] existingRows = null)
        {
            InstantiatePlacedFurniture(type, pos, rot, existingRows);
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
            SetFloorGridVisible(false);
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

        private bool IsPointerOverUIButton(Vector2 pointerPos)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null || placementHUDCanvas == null) return false;

            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = pointerPos;
            List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject != null && r.gameObject.transform.IsChildOf(placementHUDCanvas.transform))
                {
                    if (r.gameObject.GetComponentInParent<Button>() != null || r.gameObject.GetComponent<Button>() != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsPointerOverUI()
        {
            return IsPointerOverUIButton(GetPointerPosition());
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
                string warnTitle = LocalizationManager.L("Modal_InvalidPlacement_Title", "Geçersiz Konum! ⚠️", "Invalid Location! ⚠️");
                string warnBody = LocalizationManager.L("Modal_InvalidPlacement_Body", "Seçtiğiniz konum duvarlar, kapılar veya başka bir mobilya ile çakışıyor!\n\nLütfen mobilyayı temiz ve boş bir alana taşıyın.", "The chosen spot overlaps with walls, doors, or other furniture!\n\nPlease move the furniture to an open and clear space.");
                string btnOk = LocalizationManager.L("Btn_OK", "Tamam", "OK");
                ModalManager.ShowModal(warnTitle, warnBody, btnOk);
            }
        }

        public void NudgeGhost(float dx, float dz)
        {
            if (ghostObj == null || !isPlacing) return;
            Vector3 pos = ghostObj.transform.position;
            pos.x = Mathf.Round((pos.x + dx) * 4f) / 4f;
            pos.z = Mathf.Round((pos.z + dz) * 4f) / 4f;
            pos.y = 0.01f;
            ghostObj.transform.position = pos;
        }

        private GameObject floorGridObj;

        private void CreateFloorGridOverlay()
        {
            if (floorGridObj != null) return;

            floorGridObj = new GameObject("Placement_FloorGrid_Overlay");
            floorGridObj.transform.position = new Vector3(-5.0f, 0.02f, 6.0f);
            floorGridObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject storeQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            storeQuad.name = "Store_Grid_Quad";
            storeQuad.transform.SetParent(floorGridObj.transform, false);
            storeQuad.transform.localPosition = Vector3.zero;
            storeQuad.transform.localScale = new Vector3(16.0f, 20.0f, 1f);

            DestroyImmediate(storeQuad.GetComponent<Collider>());

            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Standard");

            Material gridMat = new Material(s);
            gridMat.name = "Floor_Grid_Material";
            Texture2D gridTex = UIStyleUtility.GetFloorGridTexture();
            gridMat.mainTexture = gridTex;
            if (gridMat.HasProperty("_BaseMap")) gridMat.SetTexture("_BaseMap", gridTex);
            if (gridMat.HasProperty("_BaseColor")) gridMat.SetColor("_BaseColor", Color.white);
            gridMat.color = Color.white;

            gridMat.mainTextureScale = new Vector2(16f, 20f);
            if (gridMat.HasProperty("_BaseMap")) gridMat.SetTextureScale("_BaseMap", new Vector2(16f, 20f));

            gridMat.SetFloat("_Surface", 1);
            gridMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            gridMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            gridMat.SetInt("_ZWrite", 0);
            gridMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            gridMat.EnableKeyword("_ALPHABLEND_ON");
            gridMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            storeQuad.GetComponent<Renderer>().sharedMaterial = gridMat;

            floorGridObj.SetActive(false);
        }

        private void SetFloorGridVisible(bool visible)
        {
            if (floorGridObj == null) CreateFloorGridOverlay();
            if (floorGridObj != null) floorGridObj.SetActive(visible);
        }

        private void CreatePlacementHUDUI()
        {
            GameObject existing = GameObject.Find("Placement_HUD_Canvas");
            if (existing != null) DestroyImmediate(existing);

            placementHUDCanvas = new GameObject("Placement_HUD_Canvas");
            Canvas canvas = placementHUDCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950; // En üst öncelikli katman (Tablet ve diğer HUD'ların önünde net görünür)

            CanvasScaler scaler = placementHUDCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            placementHUDCanvas.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("HUD_BottomBar");
            panel.transform.SetParent(placementHUDCanvas.transform, false);

            RectTransform pRect = panel.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0f);
            pRect.anchorMax = new Vector2(0.5f, 0f);
            pRect.pivot = new Vector2(0.5f, 0f);
            pRect.anchoredPosition = new Vector2(-75f, 25f);
            pRect.sizeDelta = new Vector2(760f, 85f);

            Image bg = panel.AddComponent<Image>();
            bg.sprite = UIStyleUtility.CreateOutlinePillSprite(760, 85, 16, 2, new Color(0.95f, 0.40f, 0.55f), new Color(0.12f, 0.15f, 0.20f, 0.95f));
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("HUD_InfoText");
            textObj.transform.SetParent(panel.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchoredPosition = new Vector2(-180f, 0f);
            tRect.sizeDelta = new Vector2(360f, 70f);

            infoStatusText = textObj.AddComponent<Text>();
            infoStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoStatusText.raycastTarget = false;
            infoStatusText.text = LocalizationManager.L("HUD_PlacingGeneric", "🛠️ Mobilya Yerleştiriliyor...", "🛠️ Placing Furniture...");
            infoStatusText.fontSize = 15;
            infoStatusText.alignment = TextAnchor.MiddleCenter;
            infoStatusText.color = Color.white;

            // 1. KUR BUTONU (Yeşil - Dokunulan Yere Kurmayı Onaylar)
            string btnAssemble = LocalizationManager.L("Btn_AssembleHUD", "✅ Kur", "✅ Assemble");
            CreateHUDButton(panel.transform, new Vector2(65f, 0f), new Vector2(110f, 55f), btnAssemble, new Color(0.18f, 0.78f, 0.38f), () => {
                ConfirmCurrentPlacement();
            });

            // 2. DÖNDÜR BUTONU (Mavi - 90 Derece Döndürür)
            string btnRotate = LocalizationManager.L("Btn_RotateHUD", "🔄 Döndür", "🔄 Rotate");
            CreateHUDButton(panel.transform, new Vector2(185f, 0f), new Vector2(110f, 55f), btnRotate, new Color(0.20f, 0.55f, 0.88f), () => {
                RotatePlacement();
            });

            // 3. İPTAL BUTONU (Kırmızı - Eski Konumuna Veya Envantere İade Eder)
            string btnCancel = LocalizationManager.L("Btn_CancelHUD", "❌ İptal", "❌ Cancel");
            CreateHUDButton(panel.transform, new Vector2(305f, 0f), new Vector2(110f, 55f), btnCancel, new Color(0.88f, 0.25f, 0.25f), () => {
                CancelPlacement();
            });

            // 🎮 4. MİNİ D-PAD (Hassas İnce Ayar Ok Tuşları: 0.25m Adımlarla Hizalama)
            GameObject dpadPanel = new GameObject("HUD_DPad_Panel");
            dpadPanel.transform.SetParent(placementHUDCanvas.transform, false);

            RectTransform dpRect = dpadPanel.AddComponent<RectTransform>();
            dpRect.anchorMin = new Vector2(0.5f, 0f);
            dpRect.anchorMax = new Vector2(0.5f, 0f);
            dpRect.pivot = new Vector2(0.5f, 0f);
            dpRect.anchoredPosition = new Vector2(380f, 25f);
            dpRect.sizeDelta = new Vector2(130f, 130f);

            Image dpBg = dpadPanel.AddComponent<Image>();
            dpBg.sprite = UIStyleUtility.CreateOutlinePillSprite(130, 130, 18, 2, new Color(0.20f, 0.70f, 0.95f, 0.85f), new Color(0.10f, 0.13f, 0.18f, 0.95f));
            dpBg.raycastTarget = false;

            // Merkez Bilgi Rozeti (0.25m)
            GameObject centerObj = new GameObject("DPad_Center_Text");
            centerObj.transform.SetParent(dpadPanel.transform, false);
            RectTransform crt = centerObj.AddComponent<RectTransform>();
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(40f, 40f);

            Text cText = centerObj.AddComponent<Text>();
            cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cText.text = "📐\n<size=9>0.25m</size>";
            cText.fontSize = 12;
            cText.fontStyle = FontStyle.Bold;
            cText.alignment = TextAnchor.MiddleCenter;
            cText.color = new Color(0.40f, 0.85f, 1.0f);
            cText.raycastTarget = false;

            // ⬆️ YUKARI (Z + 0.25m)
            CreateDPadArrowButton(dpadPanel.transform, new Vector2(0f, 42f), "⬆️", () => NudgeGhost(0f, 0.25f));
            // ⬇️ AŞAĞI (Z - 0.25m)
            CreateDPadArrowButton(dpadPanel.transform, new Vector2(0f, -42f), "⬇️", () => NudgeGhost(0f, -0.25f));
            // ⬅️ SOL (X - 0.25m)
            CreateDPadArrowButton(dpadPanel.transform, new Vector2(-42f, 0f), "⬅️", () => NudgeGhost(-0.25f, 0f));
            // ➡️ SAĞ (X + 0.25m)
            CreateDPadArrowButton(dpadPanel.transform, new Vector2(42f, 0f), "➡️", () => NudgeGhost(0.25f, 0f));

            placementHUDCanvas.SetActive(false);
        }

        private GameObject CreateDPadArrowButton(Transform parent, Vector2 pos, string arrow, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("DPad_" + arrow);
            btnObj.transform.SetParent(parent, false);

            RectTransform r = btnObj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(46f, 46f);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = UIStyleUtility.CreateRoundedPillSprite(46, 46, 12, new Color(0.18f, 0.26f, 0.38f, 0.95f));
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Arrow");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = txtObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = arrow;
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            return btnObj;
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
            img.raycastTarget = true;

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
            txt.raycastTarget = false;

            return btnObj;
        }
    }
}
