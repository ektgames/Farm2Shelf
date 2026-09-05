using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Environment;
using Farm2Shelf.UI;
using Farm2Shelf.CameraSystem;
using Farm2Shelf.Utils;

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
        private WorkshopMachineState savedReplacementMachineState;
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
            bool wasVisible = isPlacing && placementHUDCanvas != null && placementHUDCanvas.activeSelf;
            CreatePlacementHUDUI();
            if (placementHUDCanvas != null) placementHUDCanvas.SetActive(wasVisible);
        }

        public void StartPlacement(FurnitureType type, DeliveryBoxController boxController)
        {
            if (EKTPhoneManager.Instance != null) EKTPhoneManager.Instance.ClosePhoneTabletInstant();
            PalletStorageInventoryModalUI.HideModal();
            ModalManager.CloseWorldBlockingOverlays();
            ModalManager.SetModalOpen(false);
            if (FurnitureInfoModalUI.Instance != null) FurnitureInfoModalUI.Instance.CloseModal();
            if (WorkshopMachineModalUI.Instance != null) WorkshopMachineModalUI.Instance.HideModal();

            if (isPlacing) CancelPlacement();

            this.currentType = type;
            this.sourceBox = boxController;
            this.isPlacing = true;
            this.isReinstalling = false;
            this.savedReplacementRows = null;
            this.savedReplacementMachineState = null;
            this.currentYRotation = 0f;
            this.placementStartTime = Time.unscaledTime;

            ghostObj = FurnitureModelBuilder.CreateFurnitureModel(type, isGhost: true);
            ConfigureGhostForPlacement(ghostObj);
            ghostObj.name = "Ghost_" + type.ToString();

            FurnitureItemDef def = FurnitureDatabase.GetDef(type);
            Vector3 startPos = (def != null && def.zone == FurnitureZone.StorageOnly)
                ? new Vector3(7.0f, 0.01f, 4.5f)
                : ((def != null && def.zone == FurnitureZone.WorkshopOnly)
                    ? new Vector3(-55.0f, 0.01f, 5.0f)
                    : new Vector3(-5.0f, 0.01f, 4.0f));

            if (FurnitureDatabase.IsWallMountedDecoration(type))
            {
                GetStoreWallFaces(out _, out _, out _, out float backWallFace);
                if (TrySnapToStoreWall(new Vector3(-5.0f, 0.01f, backWallFace), out Vector3 wallStart, out float wallYaw))
                {
                    startPos = wallStart;
                    currentYRotation = wallYaw;
                }
            }

            ghostObj.transform.position = startPos + Vector3.up * 0.04f;
            ghostObj.transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);

            // Kamerayı otomatik olarak ilgili binaya odakla
            if (IsometricCameraSetup.Instance != null && def != null)
            {
                if (def.zone == FurnitureZone.WorkshopOnly)
                {
                    IsometricCameraSetup.Instance.FocusOn(new Vector3(-55.0f, 0f, 6.0f), true);
                }
                else if (def.zone == FurnitureZone.StorageOnly)
                {
                    IsometricCameraSetup.Instance.FocusOn(new Vector3(7.0f, 0f, 5.0f), true);
                }
                else
                {
                    IsometricCameraSetup.Instance.FocusOn(new Vector3(-5.0f, 0f, 5.0f), true);
                }
            }

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

        public void StartReplacement(
            FurnitureType type, 
            Vector3 origPos, 
            Quaternion origRot, 
            ShelfRowData[] existingRows = null, 
            WorkshopMachineState machineState = null
        )
        {
            ModalManager.CloseWorldBlockingOverlays();
            ModalManager.SetModalOpen(false);
            if (FurnitureInfoModalUI.Instance != null) FurnitureInfoModalUI.Instance.CloseModal();
            if (WorkshopMachineModalUI.Instance != null) WorkshopMachineModalUI.Instance.HideModal();

            if (isPlacing) CancelPlacement();

            this.currentType = type;
            this.sourceBox = null;
            this.isPlacing = true;
            this.isReinstalling = true;
            this.originalReplacementPos = origPos;
            this.originalReplacementRot = origRot;
            this.savedReplacementRows = existingRows;
            this.savedReplacementMachineState = machineState;
            this.currentYRotation = origRot.eulerAngles.y;
            this.placementStartTime = Time.unscaledTime;

            ghostObj = FurnitureModelBuilder.CreateFurnitureModel(type, isGhost: true);
            ConfigureGhostForPlacement(ghostObj);
            ghostObj.name = "Ghost_" + type.ToString();
            ghostObj.transform.position = origPos;
            ghostObj.transform.rotation = origRot;

            SetFloorGridVisible(true);
            if (placementHUDCanvas != null) placementHUDCanvas.SetActive(true);

            FurnitureItemDef def = FurnitureDatabase.GetDef(type);
            if (infoStatusText != null && def != null)
            {
                string stateNote = (machineState != null && (machineState.isProducing || machineState.isReadyToCollect))
                    ? LocalizationManager.L("HUD_ProductionPreserved", " (Üretim Korunuyor ⏳)", " (Production Preserved ⏳)")
                    : "";
                string moveFmt = LocalizationManager.L(
                    "HUD_MovingInfoFmt",
                    "🛠️ {0}{1} Taşınıyor\nEkrana dokunarak taşıyın | Paneldeki [✅ Kur] butonuna basarak kurun",
                    "🛠️ Moving {0}{1}\nDrag on screen to move | Tap [✅ Assemble] to place"
                );
                infoStatusText.text = string.Format(moveFmt, def.LocalizedName, stateNote);
            }
        }

        private static void ConfigureGhostForPlacement(GameObject ghost)
        {
            if (ghost == null) return;
            ghost.SetActive(true);

            foreach (Transform child in ghost.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 2; // Ignore Raycast
            }

            foreach (Collider collider in ghost.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null) continue;
                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }

            foreach (Renderer renderer in ghost.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.sortingOrder = 50;
            }

            FurnitureModelBuilder.ApplyGhostMaterial(ghost, FurnitureModelBuilder.ValidGhostMaterial);
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

            Camera mainCam = (IsometricCameraSetup.Instance != null && IsometricCameraSetup.Instance.Cam != null)
                ? IsometricCameraSetup.Instance.Cam
                : Camera.main;

            if (mainCam != null)
            {
                // Kur tıklamasının eski ekran konumu hayaleti yola kilitlemesin.
                bool placementClickSettled = Time.unscaledTime - placementStartTime > 0.12f;
                if (placementClickSettled &&
                    TouchInputHelper.TryGetPressedPointerPosition(out Vector2 pointerPos) &&
                    !IsPointerOverUIButton(pointerPos))
                {
                    Ray ray = mainCam.ScreenPointToRay(pointerPos);
                    Plane floorPlane = new Plane(Vector3.up, new Vector3(0f, 0.01f, 0f));

                    if (floorPlane.Raycast(ray, out float enter))
                    {
                        Vector3 hitPoint = ray.GetPoint(enter);
                        hitPoint.x = Mathf.Round(hitPoint.x * 4f) / 4f; // 0.25m hassas ızgara yapışması
                        hitPoint.z = Mathf.Round(hitPoint.z * 4f) / 4f;
                        hitPoint.y = 0.01f;

                        // Geniş harita sınırları dahilinde (Dükkan, Depo, Atölye ve Tarla) serbest ve hassas konumlandırma
                        hitPoint.x = Mathf.Clamp(hitPoint.x, -85.0f, 35.0f);
                        hitPoint.z = Mathf.Clamp(hitPoint.z, -35.0f, 65.0f);

                        ghostObj.transform.position = hitPoint;
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

            FurnitureItemDef def = FurnitureDatabase.GetDef(currentType);
            Vector3 currentGhostPos = ghostObj.transform.position;
            bool isWallMounted = FurnitureDatabase.IsWallMountedDecoration(currentType);
            bool isOnWall = true;
            if (isWallMounted)
            {
                if (TrySnapToStoreWall(currentGhostPos, out Vector3 wallPos, out float wallYaw))
                {
                    currentGhostPos = wallPos;
                    currentYRotation = wallYaw;
                    ghostObj.transform.position = wallPos;
                    isOnWall = true;
                }
                else
                {
                    isOnWall = false;
                }
            }

            ghostObj.transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);

            bool isZoneValid = isWallMounted
                ? isOnWall
                : IsValidPlacementZone(currentGhostPos, def != null ? def.zone : FurnitureZone.StoreOnly);
            bool isOverlapping = IsOverlappingAnyObject(currentGhostPos, currentYRotation, currentType);

            bool isValid = isZoneValid && !isOverlapping && isOnWall;

            Material targetGhostMat = isValid ? FurnitureModelBuilder.ValidGhostMaterial : FurnitureModelBuilder.InvalidGhostMaterial;
            FurnitureModelBuilder.ApplyGhostMaterial(ghostObj, targetGhostMat);

            if (infoStatusText != null && def != null)
            {
                if (isValid)
                {
                    string okMsg = LocalizationManager.L(
                        "HUD_PosOK",
                        $"🛠️ {def.LocalizedName} (Konum UYGUN ✅)\nEkrana dokunarak taşıyın | Paneldeki [✅ Kur] butonuna basın",
                        $"🛠️ {def.LocalizedName} (Position VALID ✅)\nDrag on screen | Tap [✅ Assemble] to place"
                    );
                    infoStatusText.text = okMsg;
                    infoStatusText.color = Color.white;
                }
                else if (isWallMounted && !isOnWall)
                {
                    string wallMsg = LocalizationManager.L(
                        "HUD_WallOnlyErr",
                        "⚠️ SADECE DUVAR! Neon Duvar Saati yalnızca dükkan duvarına asılabilir.\nDuvara yaklaştırın",
                        "⚠️ WALL ONLY! The Neon Wall Clock can only be hung on a store wall.\nMove it closer to a wall"
                    );
                    infoStatusText.text = wallMsg;
                    infoStatusText.color = new Color(1.0f, 0.45f, 0.45f);
                }
                else if (!isZoneValid)
                {
                    if (def.zone == FurnitureZone.WorkshopOnly)
                    {
                        string zoneMsg = LocalizationManager.L(
                            "HUD_WorkshopOnlyErr",
                            "⚠️ GEÇERSİZ BÖLGE! (Atölye Makinesi SADECE ATÖLYE BİNASI İÇİNE kurulabilir!)",
                            "⚠️ INVALID ZONE! (Workshop Machine can ONLY be placed inside the WORKSHOP!)"
                        );
                        infoStatusText.text = zoneMsg;
                    }
                    else if (def.zone == FurnitureZone.StorageOnly)
                    {
                        string zoneMsg = LocalizationManager.L(
                            "HUD_StorageOnlyErr",
                            "⚠️ GEÇERSİZ BÖLGE! (Depo Rafı SADECE DEPO kısmına kurulabilir!)",
                            "⚠️ INVALID ZONE! (Storage Shelf can ONLY be placed inside the WAREHOUSE!)"
                        );
                        infoStatusText.text = zoneMsg;
                    }
                    else
                    {
                        string zoneMsg = LocalizationManager.L(
                            "HUD_StoreOnlyErr",
                            "⚠️ GEÇERSİZ BÖLGE! (Bu mobilya SADECE DÜKKAN İÇİNE kurulabilir!)",
                            "⚠️ INVALID ZONE! (This item can ONLY be placed inside the STORE INTERIOR!)"
                        );
                        infoStatusText.text = zoneMsg;
                    }
                    infoStatusText.color = new Color(1.0f, 0.45f, 0.45f);
                }
                else
                {
                    string overlapMsg = LocalizationManager.L(
                        "HUD_OverlapErr",
                        "⚠️ GEÇERSİZ KONUM! (Nesne veya duvarla çakışma var!)\n[🔄 Döndür] ile yön değiştirin veya boş alana taşıyın",
                        "⚠️ INVALID POSITION! (Overlapping with object or wall!)\nTap [🔄 Rotate] or drag to clear space"
                    );
                    infoStatusText.text = overlapMsg;
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
            else if (WasCancelPressed())
            {
                CancelPlacement();
            }
        }

        // --- HİBRİT INPUT SYSTEM & LEGACY INPUT OKUYUCULARI ---
        private bool GetActivePointerScreenPosition(out Vector2 screenPos)
        {
            return TouchInputHelper.TryGetPressedPointerPosition(out screenPos);
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

            // DÜKKAN İÇİ (STORE): X: [-12.6, 2.6], Z: [-2.6, frontWallZ + storeDepth - 0.6]
            bool inStoreX = pos.x >= -12.6f && pos.x <= 2.6f;
            bool inStoreZ = pos.z >= -2.6f && pos.z <= (frontWallZ + storeDepth - 0.6f);
            bool inStore = inStoreX && inStoreZ;

            // DEPO ALANI (STORAGE): X: [3.4, 10.6], Z: [-2.6, frontWallZ + storageDepth - 0.6]
            bool inStorageX = pos.x >= 3.4f && pos.x <= 10.6f;
            bool inStorageZ = pos.z >= -2.6f && pos.z <= (frontWallZ + storageDepth - 0.6f);
            bool inStorage = inStorageX && inStorageZ;

            // ATÖLYE BİNASI İÇİ (WORKSHOP): X: [-66.0, -44.0], Z: [-2.6, frontWallZ + wsDepth - 0.6]
            int wsLevel = (WorkshopManager.Instance != null) ? WorkshopManager.Instance.CurrentWorkshopLevel : 1;
            float wsDepth = (wsLevel == 1) ? 18.0f : ((wsLevel == 2) ? 27.0f : 36.0f);
            bool inWorkshopX = pos.x >= -66.0f && pos.x <= -44.0f;
            bool inWorkshopZ = pos.z >= -2.6f && pos.z <= (frontWallZ + wsDepth - 0.6f);
            bool inWorkshop = inWorkshopX && inWorkshopZ;

            if (zone == FurnitureZone.WorkshopOnly)
            {
                return inWorkshop;
            }
            else if (zone == FurnitureZone.StorageOnly)
            {
                // Depo Rafı: SADECE VE SADECE DEPO KISMINA KOYULABİLİR!
                return inStorage;
            }
            else
            {
                // Diğer Mobilyalar: SADECE VE SADECE DÜKKAN İÇİNE KOYULABİLİR!
                return inStore;
            }
        }

        private void GetStoreWallFaces(out float leftX, out float rightX, out float frontZ, out float backZ)
        {
            EnvironmentBuilder env = EnvironmentBuilder.Instance;
            int level = (env != null) ? env.CurrentUpgradeLevel : 1;
            const float wallHalfThickness = 0.20f;
            float frontWallZ = -3.0f;
            float storeDepth = (level == 1) ? 18.0f : ((level == 2) ? 27.0f : 36.0f);
            float backWallZ = frontWallZ + storeDepth;
            leftX = -13.0f + wallHalfThickness;
            rightX = 3.0f - wallHalfThickness;
            frontZ = frontWallZ + wallHalfThickness;
            backZ = backWallZ - wallHalfThickness;
        }

        private bool TrySnapToStoreWall(Vector3 desired, out Vector3 snapped, out float facingYaw)
        {
            snapped = desired;
            facingYaw = currentYRotation;
            GetStoreWallFaces(out float leftX, out float rightX, out float frontZ, out float backZ);

            float dLeft = Mathf.Abs(desired.x - leftX);
            float dRight = Mathf.Abs(desired.x - rightX);
            float dFront = Mathf.Abs(desired.z - frontZ);
            float dBack = Mathf.Abs(desired.z - backZ);
            float best = Mathf.Min(dLeft, Mathf.Min(dRight, Mathf.Min(dFront, dBack)));
            const float maxSnapDistance = 1.75f;
            if (best > maxSnapDistance) return false;

            float alongMinZ = frontZ + 0.75f;
            float alongMaxZ = backZ - 0.75f;
            float alongMinX = leftX + 0.75f;
            float alongMaxX = rightX - 0.75f;

            if (best == dLeft || Mathf.Approximately(best, dLeft))
            {
                snapped = new Vector3(leftX, 0.01f, Mathf.Clamp(desired.z, alongMinZ, alongMaxZ));
                facingYaw = 90f;
            }
            else if (best == dRight || Mathf.Approximately(best, dRight))
            {
                snapped = new Vector3(rightX, 0.01f, Mathf.Clamp(desired.z, alongMinZ, alongMaxZ));
                facingYaw = -90f;
            }
            else if (best == dFront || Mathf.Approximately(best, dFront))
            {
                snapped = new Vector3(Mathf.Clamp(desired.x, alongMinX, alongMaxX), 0.01f, frontZ);
                facingYaw = 0f;
            }
            else
            {
                snapped = new Vector3(Mathf.Clamp(desired.x, alongMinX, alongMaxX), 0.01f, backZ);
                facingYaw = 180f;
            }

            // Kökü 2 cm duvarın içine çek: saat sırtı duvara gömülür, havada asılı durmaz.
            const float wallEmbed = 0.02f;
            Vector3 intoRoom = Quaternion.Euler(0f, facingYaw, 0f) * Vector3.forward;
            snapped -= intoRoom * wallEmbed;

            if (snapped.z <= -1.8f && snapped.x >= -5.8f && snapped.x <= -4.2f) return false;
            if (snapped.x >= 2.35f && snapped.z >= 0.8f && snapped.z <= 3.4f) return false;
            return true;
        }

        public void RotatePlacement(float deltaAngle = 90f)
        {
            currentYRotation = (currentYRotation + deltaAngle + 360f) % 360f;
        }

        public void ConfirmPlacement(Vector3 pos, Quaternion rot)
        {
            if (!isPlacing) return;

            InstantiatePlacedFurniture(currentType, pos, rot, savedReplacementRows, savedReplacementMachineState);

            if (sourceBox != null && FurnitureDeliveryManager.Instance != null)
            {
                FurnitureDeliveryManager.Instance.RemoveBox(sourceBox);
            }
            else if (IsWorkshopMachine(currentType, out _) && !isReinstalling && WorkshopPalletManager.Instance != null)
            {
                WorkshopPalletManager.Instance.RemoveOneMachineBox(currentType);
            }
            else if (!isReinstalling && FurnitureDeliveryManager.Instance != null)
            {
                FurnitureDeliveryManager.Instance.RemoveOneBoxOfType(currentType);
            }

            CleanupGhost();
            isPlacing = false;
            isReinstalling = false;
            savedReplacementRows = null;
            savedReplacementMachineState = null;
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

                case FurnitureType.GourmetShelf:
                    return new Vector2(1.9f, 1.0f);

                case FurnitureType.WorkshopJamMaker:
                case FurnitureType.WorkshopJuicePress:
                case FurnitureType.WorkshopCannery:
                case FurnitureType.WorkshopDehydrator:
                case FurnitureType.WorkshopOilPress:
                case FurnitureType.WorkshopSaladStation:
                    return new Vector2(2.4f, 1.8f);

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

                case FurnitureType.WallClock:
                    return new Vector2(0.70f, 0.10f);

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

            float halfW = (width / 2f);
            float halfD = (depth / 2f);
            bool isWallMounted = FurnitureDatabase.IsWallMountedDecoration(type);

            Bounds ghostBounds = new Bounds(
                new Vector3(pos.x, isWallMounted ? 1.85f : 0.9f, pos.z),
                new Vector3(Mathf.Max(0.1f, width - 0.10f), isWallMounted ? 0.7f : 1.8f, Mathf.Max(0.1f, depth - 0.10f))
            );

            // 1. DUVAR VE ODA SINIRLARI KONTROLÜ
            EnvironmentBuilder env = EnvironmentBuilder.Instance;
            int level = (env != null) ? env.CurrentUpgradeLevel : 1;
            float frontWallZ = -3.0f;
            float storeDepth = (level == 1) ? 18.0f : ((level == 2) ? 27.0f : 36.0f);
            float storageDepth = (level == 1) ? 9.5f : ((level == 2) ? 14.5f : 19.5f);
            float backWallZ = frontWallZ + storeDepth;
            float storageBackZ = frontWallZ + storageDepth;

            // Ara bölme duvarı (X: 2.85 .. 3.15 arası). Duvar saati bu duvara asılabilir.
            if (!isWallMounted && pos.x + halfW > 2.85f && pos.x - halfW < 3.15f)
            {
                return true;
            }

            // Personel odası ara bölme duvarı (Z: storageBackZ - 0.25 .. storageBackZ + 0.25 arası ve X > 3.0)
            if (pos.x > 3.0f && pos.z + halfD > storageBackZ - 0.25f && pos.z - halfD < storageBackZ + 0.25f)
            {
                return true;
            }

            if (pos.x <= -35.0f)
            {
                // ATÖLYE BİNASI SINIRLARI
                int wsLevel = (WorkshopManager.Instance != null) ? WorkshopManager.Instance.CurrentWorkshopLevel : 1;
                float wsDepth = (wsLevel == 1) ? 18.0f : ((wsLevel == 2) ? 27.0f : 36.0f);
                float wsBackZ = frontWallZ + wsDepth;

                float minX = -66.0f + halfW;
                float maxX = -44.0f - halfW;
                float minZ = -2.6f + halfD;
                float maxZ = (wsBackZ - 0.4f) - halfD;

                if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ)
                {
                    return true;
                }

                // Ön Sürgülü Kapı Ağzı (X: -56.5 .. -53.5, Z: <= -1.8)
                if (pos.z <= -1.8f && pos.x >= -56.5f && pos.x <= -53.5f)
                {
                    return true;
                }

                // Atölye Sabit Hammadde Palet Rafı Alanı (Paletin içine/üzerine makine kurulmasını engeller)
                if (pos.x + halfW > -64.2f && pos.x - halfW < -60.8f && pos.z + halfD > 1.0f && pos.z - halfD < 4.0f)
                {
                    return true;
                }
            }
            else if (pos.x <= 2.85f)
            {
                // MAĞAZA ALANI SINIRLARI
                if (!isWallMounted)
                {
                    float minX = -12.6f + halfW;
                    float maxX = 2.6f - halfW;
                    float minZ = -2.6f + halfD;
                    float maxZ = (backWallZ - 0.4f) - halfD;

                    if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ)
                    {
                        return true;
                    }
                }

                // Ana Dış Giriş Kapısı Ağzı (Geçişi tıkamamak için: X: -5.8 .. -4.2, Z: <= -1.8)
                if (pos.z <= -1.8f && pos.x >= -5.8f && pos.x <= -4.2f)
                {
                    return true;
                }

                // Mağaza-depo geçiş kapısı (doğu duvarı)
                if (isWallMounted && pos.x >= 2.2f && pos.z >= 0.8f && pos.z <= 3.4f)
                {
                    return true;
                }
            }
            else if (pos.x >= 3.15f)
            {
                // DEPO / PERSONEL ALANI SINIRLARI
                float minX = 3.4f + halfW;
                float maxX = 10.6f - halfW;
                float minZ = -2.6f + halfD;
                float maxZ = (backWallZ - 0.4f) - halfD;

                if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ)
                {
                    return true;
                }

                // Depo İçi Geçiş Kapısı Ağzı (X: 3.4..4.0, Z: 1.0..3.0)
                if (pos.x <= 4.0f && pos.z >= 1.0f && pos.z <= 3.0f)
                {
                    return true;
                }
            }
            else if (!isWallMounted)
            {
                return true;
            }

            // 2. MEVCUT YERLEŞTİRİLMİŞ MOBİLYALARLA ÇAKIŞMA (BOUNDS INTERSECTION)
            if (!PlacedFurnitureController.IsWalkableFloorDecoration(type))
            {
                var placedFurniture = PlacedFurnitureController.AllPlacedFurniture;
                int count = placedFurniture.Count;

                for (int i = 0; i < count; i++)
                {
                    var f = placedFurniture[i];
                    if (f == null) continue;
                    if (PlacedFurnitureController.IsWalkableFloorDecoration(f.FurnitureType)) continue;

                    if (isReinstalling && Vector3.Distance(f.transform.position, originalReplacementPos) < 0.2f)
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
                        new Vector3(Mathf.Max(0.1f, fW - 0.10f), 1.8f, Mathf.Max(0.1f, fD - 0.10f))
                    );

                    if (ghostBounds.Intersects(existingBounds))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsFrontOrDoorwayBlocked(Vector3 pos, float rotationY, FurnitureType type)
        {
            return false;
        }

        public void SpawnRestoredFurniture(
            FurnitureType type, 
            Vector3 pos, 
            Quaternion rot, 
            ShelfRowData[] existingRows = null, 
            WorkshopMachineState machineState = null
        )
        {
            InstantiatePlacedFurniture(type, pos, rot, existingRows, machineState);
        }

        private void InstantiatePlacedFurniture(
            FurnitureType type, 
            Vector3 pos, 
            Quaternion rot, 
            ShelfRowData[] existingRows = null, 
            WorkshopMachineState machineState = null
        )
        {
            if (FurnitureDatabase.IsWallMountedDecoration(type) &&
                TrySnapToStoreWall(pos, out Vector3 wallPos, out float wallYaw))
            {
                pos = wallPos;
                rot = Quaternion.Euler(0f, wallYaw, 0f);
            }

            GameObject realFurniture = FurnitureModelBuilder.CreateFurnitureModel(type, isGhost: false);
            realFurniture.name = type.ToString() + "_" + System.Guid.NewGuid().ToString().Substring(0, 5);
            realFurniture.transform.SetParent(placedFurnitureContainer, false);
            realFurniture.transform.position = pos;
            realFurniture.transform.rotation = rot;

            Vector2 footprint = GetFurnitureFootprintSize(type);
            BoxCollider col = realFurniture.AddComponent<BoxCollider>();
            if (FurnitureDatabase.IsWallMountedDecoration(type))
            {
                col.center = new Vector3(0f, 1.85f, 0.04f);
                col.size = new Vector3(footprint.x, 0.7f, 0.12f);
            }
            else
            {
                col.center = new Vector3(0f, 0.9f, 0f);
                col.size = new Vector3(footprint.x, 1.8f, footprint.y);
            }

            PlacedFurnitureController placedCtrl = realFurniture.AddComponent<PlacedFurnitureController>();
            placedCtrl.Setup(type, pos, rot, existingRows);

            if (IsWorkshopMachine(type, out WorkshopMachineType mType))
            {
                WorkshopMachineController wsCtrl = realFurniture.AddComponent<WorkshopMachineController>();
                wsCtrl.machineType = mType;
                if (machineState != null)
                {
                    machineState.ApplyTo(wsCtrl);
                }
            }
        }

        public static bool IsWorkshopMachine(FurnitureType type, out WorkshopMachineType mType)
        {
            switch (type)
            {
                case FurnitureType.WorkshopJamMaker:
                    mType = WorkshopMachineType.JamMaker;
                    return true;
                case FurnitureType.WorkshopJuicePress:
                    mType = WorkshopMachineType.JuiceExtractor;
                    return true;
                case FurnitureType.WorkshopCannery:
                    mType = WorkshopMachineType.Cannery;
                    return true;
                case FurnitureType.WorkshopDehydrator:
                    mType = WorkshopMachineType.Dehydrator;
                    return true;
                case FurnitureType.WorkshopOilPress:
                    mType = WorkshopMachineType.OilPress;
                    return true;
                case FurnitureType.WorkshopSaladStation:
                    mType = WorkshopMachineType.SaladStation;
                    return true;
                default:
                    mType = WorkshopMachineType.JamMaker;
                    return false;
            }
        }

        public void CancelPlacement()
        {
            if (isReinstalling)
            {
                InstantiatePlacedFurniture(currentType, originalReplacementPos, originalReplacementRot, savedReplacementRows, savedReplacementMachineState);
                isReinstalling = false;
                savedReplacementRows = null;
                savedReplacementMachineState = null;
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

        private static readonly List<UnityEngine.EventSystems.RaycastResult> uiRaycastResults = new List<UnityEngine.EventSystems.RaycastResult>(16);

        private bool IsPointerOverUIButton(Vector2 pointerPos)
        {
            if (UnityEngine.EventSystems.EventSystem.current == null || placementHUDCanvas == null) return false;

            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = pointerPos;
            uiRaycastResults.Clear();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, uiRaycastResults);

            for (int i = 0; i < uiRaycastResults.Count; i++)
            {
                var r = uiRaycastResults[i];
                if (r.module is UnityEngine.EventSystems.PhysicsRaycaster ||
                    r.module is UnityEngine.EventSystems.Physics2DRaycaster)
                {
                    continue;
                }

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
            if (GetActivePointerScreenPosition(out Vector2 pointerPos))
            {
                return IsPointerOverUIButton(pointerPos);
            }
            return false;
        }

        public void ConfirmCurrentPlacement()
        {
            if (!isPlacing || ghostObj == null) return;

            Vector3 pos = ghostObj.transform.position;
            Quaternion rot = ghostObj.transform.rotation;

            FurnitureItemDef def = FurnitureDatabase.GetDef(currentType);
            bool isWallMounted = FurnitureDatabase.IsWallMountedDecoration(currentType);
            bool isOnWall = !isWallMounted || TrySnapToStoreWall(pos, out _, out _);
            bool isZoneValid = isWallMounted
                ? isOnWall
                : IsValidPlacementZone(pos, def != null ? def.zone : FurnitureZone.StoreOnly);
            bool isOverlapping = IsOverlappingAnyObject(pos, currentYRotation, currentType);

            if (isZoneValid && !isOverlapping && isOnWall)
            {
                ConfirmPlacement(pos, rot);
            }
            else if (isWallMounted && !isOnWall)
            {
                string warnTitle = LocalizationManager.L("Modal_WallOnly_Title", "Sadece Duvar! ⚠️", "Wall Only! ⚠️");
                string warnBody = LocalizationManager.L("Modal_WallOnly_Body", "Neon Duvar Saati yalnızca dükkan duvarına asılabilir.\n\nLütfen saati bir duvara yaklaştırın.", "The Neon Wall Clock can only be hung on a store wall.\n\nPlease move it closer to a wall.");
                string btnOk = LocalizationManager.L("Btn_OK", "Tamam", "OK");
                ModalManager.ShowModal(warnTitle, warnBody, btnOk);
            }
            else if (!isZoneValid)
            {
                string warnTitle = LocalizationManager.L("Modal_InvalidZone_Title", "Geçersiz Bölge! ⚠️", "Invalid Placement Zone! ⚠️");
                string warnBody = (def != null && def.zone == FurnitureZone.WorkshopOnly)
                    ? LocalizationManager.L("Modal_WorkshopOnly_Body", "Atölye makineleri SADECE VE SADECE ATÖLYE BİNASI İÇİNE yerleştirilebilir!\n\nLütfen makineyi atölye binası içine taşıyın.", "Workshop machines can ONLY be placed inside the WORKSHOP building!\n\nPlease move it inside the workshop area.")
                    : ((def != null && def.zone == FurnitureZone.StorageOnly)
                        ? LocalizationManager.L("Modal_StorageOnly_Body", "Depo rafları SADECE VE SADECE DEPO kısmına yerleştirilebilir!\n\nLütfen mobilyayı depo alanına taşıyın.", "Storage racks can ONLY be placed inside the STORAGE / WAREHOUSE room!\n\nPlease move it inside the storage area.")
                        : LocalizationManager.L("Modal_StoreOnly_Body", "Bu mobilya SADECE VE SADECE DÜKKAN İÇİNE yerleştirilebilir!\n\nDepoya, atölyeye veya personel odasına yerleştirilemez.", "This furniture can ONLY be placed inside the STORE INTERIOR!\n\nIt cannot be placed in the warehouse or workshop."));
                string btnOk = LocalizationManager.L("Btn_OK", "Tamam", "OK");
                ModalManager.ShowModal(warnTitle, warnBody, btnOk);
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
            if (FurnitureDatabase.IsWallMountedDecoration(currentType) &&
                TrySnapToStoreWall(pos, out Vector3 wallPos, out float wallYaw))
            {
                pos = wallPos;
                currentYRotation = wallYaw;
            }
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
            if (floorGridObj != null)
            {
                FurnitureItemDef currentDef = FurnitureDatabase.GetDef(currentType);
                Transform storeQuad = floorGridObj.transform.Find("Store_Grid_Quad");
                if (currentDef != null && currentDef.zone == FurnitureZone.WorkshopOnly)
                {
                    int wsLevel = (WorkshopManager.Instance != null) ? WorkshopManager.Instance.CurrentWorkshopLevel : 1;
                    float wsDepth = (wsLevel == 1) ? 18.0f : ((wsLevel == 2) ? 27.0f : 36.0f);
                    floorGridObj.transform.position = new Vector3(-55.0f, 0.02f, -3.0f + (wsDepth / 2f));
                    if (storeQuad != null) storeQuad.localScale = new Vector3(22.0f, wsDepth, 1f);
                }
                else if (currentDef != null && currentDef.zone == FurnitureZone.StorageOnly)
                {
                    floorGridObj.transform.position = new Vector3(7.0f, 0.02f, 4.5f);
                    if (storeQuad != null) storeQuad.localScale = new Vector3(8.0f, 15.0f, 1f);
                }
                else
                {
                    floorGridObj.transform.position = new Vector3(-5.0f, 0.02f, 6.0f);
                    if (storeQuad != null) storeQuad.localScale = new Vector3(16.0f, 20.0f, 1f);
                }
                floorGridObj.SetActive(visible);
            }
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
            dpRect.sizeDelta = new Vector2(136f, 136f);

            Image dpBg = dpadPanel.AddComponent<Image>();
            dpBg.sprite = UIStyleUtility.CreateOutlinePillSprite(136, 136, 20, 3, new Color(0.15f, 0.75f, 0.95f, 0.95f), new Color(0.08f, 0.12f, 0.18f, 0.96f));
            dpBg.raycastTarget = false;

            // Merkez Bilgi Rozeti (0.25m)
            GameObject centerObj = new GameObject("DPad_Center_Text");
            centerObj.transform.SetParent(dpadPanel.transform, false);
            RectTransform crt = centerObj.AddComponent<RectTransform>();
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(42f, 24f);

            Image cBg = centerObj.AddComponent<Image>();
            cBg.sprite = UIStyleUtility.CreateRoundedPillSprite(42, 24, 6, new Color(0.05f, 0.08f, 0.13f, 0.90f));
            cBg.raycastTarget = false;

            GameObject cTxtObj = new GameObject("Txt");
            cTxtObj.transform.SetParent(centerObj.transform, false);
            RectTransform cTxtR = cTxtObj.AddComponent<RectTransform>();
            cTxtR.anchorMin = Vector2.zero;
            cTxtR.anchorMax = Vector2.one;
            cTxtR.sizeDelta = Vector2.zero;

            Text cText = cTxtObj.AddComponent<Text>();
            cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cText.text = "0.25m";
            cText.fontSize = 11;
            cText.fontStyle = FontStyle.Bold;
            cText.alignment = TextAnchor.MiddleCenter;
            cText.color = new Color(0.35f, 0.85f, 1.0f);
            cText.raycastTarget = false;

            // ⬆️ YUKARI (Z + 0.25m)
            CreateDPadArrowButton(dpadPanel.transform, new Vector2(0f, 44f), "UP", () => NudgeGhost(0f, 0.25f));
            // ⬇️ AŞAĞI (Z - 0.25m)
            CreateDPadArrowButton(dpadPanel.transform, new Vector2(0f, -44f), "DOWN", () => NudgeGhost(0f, -0.25f));
            // ⬅️ SOL (X - 0.25m)
            CreateDPadArrowButton(dpadPanel.transform, new Vector2(-44f, 0f), "LEFT", () => NudgeGhost(-0.25f, 0f));
            // ➡️ SAĞ (X + 0.25m)
            CreateDPadArrowButton(dpadPanel.transform, new Vector2(44f, 0f), "RIGHT", () => NudgeGhost(0.25f, 0f));

            placementHUDCanvas.SetActive(false);
        }

        private GameObject CreateDPadArrowButton(Transform parent, Vector2 pos, string direction, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("DPad_" + direction);
            btnObj.transform.SetParent(parent, false);

            RectTransform r = btnObj.AddComponent<RectTransform>();
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(42f, 42f);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = UIStyleUtility.CreateOutlinePillSprite(42, 42, 10, 2, new Color(0.35f, 0.65f, 0.95f, 0.90f), new Color(0.14f, 0.22f, 0.32f, 0.96f));
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            // Nizami Vektörel Ok İkonu
            GameObject iconObj = new GameObject("Arrow_Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = iconObj.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0.5f, 0.5f);
            tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(22f, 22f);
            tr.anchoredPosition = Vector2.zero;

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = UIStyleUtility.CreateArrowSprite(direction, 64, Color.white);
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

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
