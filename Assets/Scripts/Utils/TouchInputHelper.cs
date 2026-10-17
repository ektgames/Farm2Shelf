using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;
using Farm2Shelf.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Farm2Shelf.Utils
{
    /// <summary>
    /// Mobil Dokunmatik & PC Hibrit Girdi, Kamera Kaydırma & 3D Tıklama Yöneticisi.
    /// Ekranda harita kaydırma (pan/drag), döndürme veya iki parmaklı yakınlaştırma (pinch-zoom) yapılırken
    /// 3D nesnelere (raf, tarla, ambar, personel, müşteri vb.) KAZARA TIKLANMASINI ÖNLER.
    /// 
    /// Kullanıcı ekrana hafifçe dokunup çektiğinde (Clean Tap / Click):
    /// - Sürükleme mesafesi küçükse (<= 25px)
    /// - Çift parmak hareketi yapılmadıysa
    /// - Tıklanabilir bir UI butonunun üzerinde değilse
    /// İlgili 3D nesneye (Raf, Tarla, Ahır, Personel, Müşteri, Kutu) pürüzsüzce ve anında tıklar.
    /// </summary>
    public static class TouchInputHelper
    {
        private static bool cleanTapTriggeredThisFrame;
        private static Vector2 lastTapPosition;
        private static int lastEvaluatedFrame = -1;
        private static float lastGlobalDispatchTime = 0f;
        private static int worldClickSuppressedFrame = -1;

        public const float MaxTapDuration = 0.95f;
        public const float MaxTapDragDistance = 55f; // Ekran pikselleri (Hafif parmak titremelerinde ve hızlı tıklamalarda da tıklamayı %100 yakalar)

        private static GameObject runnerObj;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureRunner();
        }

        public static void EnsureRunner()
        {
            if (runnerObj == null)
            {
                runnerObj = new GameObject("Farm2Shelf_TouchInputRunner");
                Object.DontDestroyOnLoad(runnerObj);
                runnerObj.AddComponent<TouchInputRunner>();
            }

            WorldTapInput.EnsureInitialized();
            if (!runnerObj.activeSelf) runnerObj.SetActive(true);
            var runner = runnerObj.GetComponent<TouchInputRunner>();
            if (runner != null && !runner.enabled) runner.enabled = true;
            UiReliableClick.ConfigureSharedEventSystem();
        }

        public static void SuppressNextTap()
        {
            lastGlobalDispatchTime = Time.unscaledTime;
            worldClickSuppressedFrame = Time.frameCount;
            WorldTapInput.CancelGesture();
            cleanTapTriggeredThisFrame = false;
        }

        public static bool IsWorldInteractionBlocked() => IsWorldInteractionBlocked(GetCurrentPointerPosition());

        public static bool IsWorldInteractionBlocked(Vector2 position)
        {
            if (Time.frameCount == worldClickSuppressedFrame) return true;
            if (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing) return true;
            return WorldInteractionGate.IsBlocked(position);
        }

        public static bool IsCleanTapThisFrame(out Vector2 tapPosition)
        {
            EvaluateFrame();
            tapPosition = lastTapPosition;
            return cleanTapTriggeredThisFrame;
        }

        public static void EvaluateFrame()
        {
            if (lastEvaluatedFrame == Time.frameCount) return;
            lastEvaluatedFrame = Time.frameCount;
            cleanTapTriggeredThisFrame = WorldTapInput.Poll(out lastTapPosition);
            if (cleanTapTriggeredThisFrame && !IsWorldInteractionBlocked(lastTapPosition))
                Dispatch3DClick(lastTapPosition);
        }

        /// <summary>
        /// 3D dünyadaki nesneleri (Personel, Müşteri, Teslimat Kolisi, Raf, Tarla, Ahır)
        /// mobil dokunmatik öncelik sırasına göre pürüzsüzce tıklar.
        /// </summary>
        public static bool Dispatch3DClick(Vector2 screenPos)
        {
            if (IsWorldInteractionBlocked(screenPos)) return false;
            if (Time.unscaledTime >= lastGlobalDispatchTime && Time.unscaledTime - lastGlobalDispatchTime < 0.03f) return false;
            if (Time.unscaledTime >= ModalManager.LastModalCloseTime && Time.unscaledTime - ModalManager.LastModalCloseTime < 0.04f) return false;
            if (screenPos == Vector2.zero) return false;

            Camera cam = Camera.main;
            if (cam == null) return false;

            if (!IsValidScreenPoint(screenPos)) return false;

            Ray ray = cam.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray, 250f);

            // ÖNCELİK 0: Teslimat Kolisi veya Palet Tıklaması (Doğrudan ve Hızlı Erişim)
            if (TryDispatchPalletOrBox(hits))
            {
                return true;
            }

            // ÖNCELİK 1: Karakterler (Personel & Müşteri) - Doğrudan Raycast ve Mobil Dokunmatik Toleransı (SphereCast)
            // 1.a Doğrudan Raycast ile Karakter Kontrolü
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    GameObject go = hit.collider.gameObject;

                    StaffClickableTarget staff = go.GetComponentInParent<StaffClickableTarget>() ?? go.GetComponent<StaffClickableTarget>();
                    if (staff != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        staff.OnStaffClicked();
                        return true;
                    }

                    CustomerClickableTarget customer = go.GetComponentInParent<CustomerClickableTarget>() ?? go.GetComponent<CustomerClickableTarget>();
                    if (customer != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        customer.OnCustomerClicked();
                        return true;
                    }
                }
            }

            // 1.b Mobil Dokunmatik Hassasiyeti için SphereCast (Küçük/hareketli karakterleri kaçırmaz)
            RaycastHit[] sphereHits = Physics.SphereCastAll(ray, 0.45f, 250f);
            if (sphereHits != null && sphereHits.Length > 0)
            {
                System.Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var sHit in sphereHits)
                {
                    if (sHit.collider == null) continue;
                    GameObject sGo = sHit.collider.gameObject;

                    StaffClickableTarget staff = sGo.GetComponentInParent<StaffClickableTarget>() ?? sGo.GetComponent<StaffClickableTarget>();
                    if (staff != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        staff.OnStaffClicked();
                        return true;
                    }

                    CustomerClickableTarget customer = sGo.GetComponentInParent<CustomerClickableTarget>() ?? sGo.GetComponent<CustomerClickableTarget>();
                    if (customer != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        customer.OnCustomerClicked();
                        return true;
                    }
                }
            }

            // ÖNCELİK 2: Mobilyalar, Reyonlar, Tarlalar ve Ahır
            if (hits != null && hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    GameObject go = hit.collider.gameObject;

                    // Reyon / Raf / Dolap / Kasa / Depo Mobilyası
                    PlacedFurnitureController furniture = go.GetComponentInParent<PlacedFurnitureController>() ?? go.GetComponent<PlacedFurnitureController>();
                    if (furniture != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        furniture.OnClickDetected();
                        return true;
                    }

                    // Tarla / Ekim Alanı (Field Plot)
                    FieldPlotController plot = go.GetComponentInParent<FieldPlotController>() ?? go.GetComponent<FieldPlotController>();
                    if (plot != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        plot.OnPlotClicked();
                        return true;
                    }

                    // Ahır (Barn)
                    BarnController barn = go.GetComponentInParent<BarnController>() ?? go.GetComponent<BarnController>();
                    if (barn != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        barn.OnBarnClicked();
                        return true;
                    }

                    LivestockBuildingController liveBld = go.GetComponentInParent<LivestockBuildingController>() ?? go.GetComponent<LivestockBuildingController>();
                    if (liveBld != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        liveBld.OnBuildingClicked();
                        return true;
                    }
                }
            }

            // 2.b Dokunmatik & Tıklama Hassasiyeti için SphereCast (Mobilya & Palet Toleransı)
            if (sphereHits != null && sphereHits.Length > 0)
            {
                foreach (var sHit in sphereHits)
                {
                    if (sHit.collider == null) continue;
                    GameObject sGo = sHit.collider.gameObject;

                    // Mobilya / Raf / Depo Palet Rafı
                    PlacedFurnitureController furniture = sGo.GetComponentInParent<PlacedFurnitureController>() ?? sGo.GetComponent<PlacedFurnitureController>();
                    if (furniture != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        furniture.OnClickDetected();
                        return true;
                    }

                    // Tarla
                    FieldPlotController plot = sGo.GetComponentInParent<FieldPlotController>() ?? sGo.GetComponent<FieldPlotController>();
                    if (plot != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        plot.OnPlotClicked();
                        return true;
                    }

                    // Ahır
                    BarnController barn = sGo.GetComponentInParent<BarnController>() ?? sGo.GetComponent<BarnController>();
                    if (barn != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        barn.OnBarnClicked();
                        return true;
                    }

                    LivestockBuildingController liveBld = sGo.GetComponentInParent<LivestockBuildingController>() ?? sGo.GetComponent<LivestockBuildingController>();
                    if (liveBld != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        liveBld.OnBuildingClicked();
                        return true;
                    }
                }
            }

            // Boş dünyaya (zemin, kaldırım vb.) tıklandığında açık olan profil kartını pürüzsüzce kapat
            CloseAnyOpenProfileCard();
            return false;
        }

        private static bool TryDispatchPalletOrBox(RaycastHit[] hits)
        {
            if (hits == null || hits.Length == 0) return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || hits[i].collider.isTrigger) continue;
                GameObject go = hits[i].collider.gameObject;

                DeliveryBoxController box = go.GetComponentInParent<DeliveryBoxController>() ?? go.GetComponent<DeliveryBoxController>();
                if (box != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    CloseAnyOpenProfileCard();
                    box.TriggerPlacement();
                    return true;
                }

                WorkshopPalletClickable wsPallet = go.GetComponentInParent<WorkshopPalletClickable>() ?? go.GetComponent<WorkshopPalletClickable>();
                if (wsPallet != null || go.name.Contains("Workshop_Pallet") ||
                    (go.transform.parent != null && go.transform.parent.name.Contains("Workshop_Pallet")))
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    CloseAnyOpenProfileCard();
                    PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: true);
                    return true;
                }

                DeliveryPalletClickable pallet = go.GetComponentInParent<DeliveryPalletClickable>() ?? go.GetComponent<DeliveryPalletClickable>();
                if (pallet != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    CloseAnyOpenProfileCard();
                    PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: false);
                    return true;
                }
                // A different solid object is in front: never pick a pallet behind it.
                return false;
            }

            return false;
        }

        private static void CloseAnyOpenProfileCard()
        {
            if (CustomerProfileModalUI.Instance != null && CustomerProfileModalUI.Instance.IsModalOpen)
            {
                CustomerProfileModalUI.Instance.HideModal();
            }
            if (StaffProfileModalUI.Instance != null && StaffProfileModalUI.Instance.IsModalOpen)
            {
                StaffProfileModalUI.Instance.HideModal();
            }
        }

        // One snapshot per frame: never combine legacy mouse edges with Simulator
        // touch coordinates. Keep the touch source through its release frame.
        private static int pointerFrame = -1;
        private static Vector2 pointerPosition;
        private static bool pointerDown, pointerUp, pointerPressed, pointerCancelled, pointerIsTouch;
        private static int pointerTouchCount;
#if ENABLE_INPUT_SYSTEM
        public struct CameraTouch
        {
            public Vector2 position, delta;
            public UnityEngine.InputSystem.TouchPhase phase;
        }
        private static CameraTouch[] cameraTouches = new CameraTouch[16];
        private static int trackedTouchId = -1;

        public static CameraTouch GetCameraTouch(int index)
        {
            ReadPointerFrame();
            return index >= 0 && index < pointerTouchCount ? cameraTouches[index] : default;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetGestureState()
        {
            cleanTapTriggeredThisFrame = false;
            pointerDown = pointerUp = pointerPressed = pointerCancelled = pointerIsTouch = false;
            pointerTouchCount = 0;
            pointerFrame = lastEvaluatedFrame = -1;
            lastGlobalDispatchTime = -1f;
            WorldTapInput.CancelGesture();
            worldClickSuppressedFrame = Time.frameCount;
#if ENABLE_INPUT_SYSTEM
            trackedTouchId = -1;
#endif
        }

        private static void ReadPointerFrame()
        {
            if (pointerFrame == Time.frameCount) return;
            pointerFrame = Time.frameCount;
            bool previouslyHeld = pointerPressed;
            bool previouslyTouch = pointerIsTouch;
            pointerDown = pointerUp = pointerPressed = pointerCancelled = pointerIsTouch = false;
            pointerTouchCount = 0;
#if ENABLE_INPUT_SYSTEM
            var screen = Touchscreen.current;
            if (screen != null && screen.enabled)
            {
                if (cameraTouches.Length < screen.touches.Count)
                    cameraTouches = new CameraTouch[screen.touches.Count];
                UnityEngine.InputSystem.Controls.TouchControl selected = null;
                UnityEngine.InputSystem.Controls.TouchControl firstHeld = null;
                UnityEngine.InputSystem.Controls.TouchControl quickTap = null;
                foreach (var touch in screen.touches)
                {
                    var phase = touch.phase.ReadValue();
                    bool held = touch.press.isPressed && phase != UnityEngine.InputSystem.TouchPhase.Ended &&
                        phase != UnityEngine.InputSystem.TouchPhase.Canceled && phase != UnityEngine.InputSystem.TouchPhase.None;
                    if (held)
                    {
                        cameraTouches[pointerTouchCount++] = new CameraTouch
                        { position = touch.position.ReadValue(), delta = touch.delta.ReadValue(), phase = phase };
                        if (firstHeld == null) firstHeld = touch;
                    }
                    if (touch.press.wasPressedThisFrame && touch.press.wasReleasedThisFrame)
                        quickTap = touch;
                    // primaryTouch is synthetic and can remain pressed until every finger
                    // has ended. Track the actual contact, including its release frame.
                    if (touch.touchId.ReadValue() == trackedTouchId &&
                        (held || phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                         phase == UnityEngine.InputSystem.TouchPhase.Canceled)) selected = touch;
                }
                if (selected == null) selected = quickTap ?? firstHeld;
                if (selected != null)
                {
                    int id = selected.touchId.ReadValue();
                    pointerIsTouch = true;
                    pointerPosition = selected.position.ReadValue();
                    var phase = selected.phase.ReadValue();
                    pointerCancelled = phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                    pointerUp = phase == UnityEngine.InputSystem.TouchPhase.Ended || pointerCancelled;
                    pointerPressed = !pointerUp && selected.press.isPressed;
                    // Button edge caches use InputSystem update steps, which may differ
                    // from game frames in Device Simulator. Track actual contact state.
                    pointerDown = (pointerPressed && (!previouslyHeld || !previouslyTouch || trackedTouchId != id)) ||
                        (selected == quickTap && trackedTouchId != id);
                    trackedTouchId = pointerUp ? -1 : id;
                    return;
                }
                trackedTouchId = -1;
            }
            if (previouslyHeld && previouslyTouch)
            {
                // Device reset/lost release: cancel the old gesture instead of keeping
                // it pressed or interpreting a new mouse position as its release.
                pointerIsTouch = pointerUp = pointerCancelled = true;
                return;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.enabled)
            {
                pointerPosition = mouse.position.ReadValue();
                pointerPressed = mouse.leftButton.isPressed;
                pointerDown = pointerPressed && (!previouslyHeld || previouslyTouch);
                pointerUp = !pointerPressed && previouslyHeld && !previouslyTouch;
                if (!previouslyHeld && !pointerPressed && mouse.leftButton.wasPressedThisFrame && mouse.leftButton.wasReleasedThisFrame)
                    pointerDown = pointerUp = true;
            }
#else
            pointerTouchCount = Input.touchCount;
            if (pointerTouchCount > 0)
            {
                var touch = Input.GetTouch(0);
                pointerIsTouch = true;
                pointerPosition = touch.position;
                pointerDown = touch.phase == UnityEngine.TouchPhase.Began;
                pointerCancelled = touch.phase == UnityEngine.TouchPhase.Canceled;
                pointerUp = touch.phase == UnityEngine.TouchPhase.Ended || pointerCancelled;
                pointerPressed = !pointerUp;
            }
            else
            {
                pointerPosition = Input.mousePosition;
                pointerDown = Input.GetMouseButtonDown(0);
                pointerUp = Input.GetMouseButtonUp(0);
                pointerPressed = Input.GetMouseButton(0);
            }
#endif
        }

        public static int GetTouchCount() { ReadPointerFrame(); return pointerTouchCount; }
        public static bool PreferDesktopMouse() { ReadPointerFrame(); return !pointerIsTouch; }
        public static bool IsValidScreenPoint(Vector2 p)
        {
            return !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.x) &&
                   !float.IsInfinity(p.y) && p.x >= 0f && p.y >= 0f && p.x <= Screen.width && p.y <= Screen.height;
        }

        public static bool TryGetMouseScreenPosition(out Vector2 screenPos)
        {
            screenPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null || !Mouse.current.enabled) return false;
            screenPos = Mouse.current.position.ReadValue();
#else
            screenPos = Input.mousePosition;
#endif
            return IsValidScreenPoint(screenPos);
        }

        public static bool IsMousePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.enabled && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }

        public static Vector2 GetCurrentPointerPosition() { ReadPointerFrame(); return pointerPosition; }
        public static bool IsPointerHeld() { ReadPointerFrame(); return pointerPressed; }
        public static bool WasPointerPressedThisFrame() { ReadPointerFrame(); return pointerDown; }
        public static bool WasPointerReleasedThisFrame() { ReadPointerFrame(); return pointerUp; }
        public static bool TryGetPressedPointerPosition(out Vector2 screenPos)
        {
            ReadPointerFrame();
            screenPos = pointerPosition;
            return (pointerPressed || pointerDown) && IsValidScreenPoint(screenPos);
        }

        private static readonly List<RaycastResult> cachedRaycastResults = new List<RaycastResult>(32);

        public static bool IsPointerOverScrollRect(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            cachedRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, cachedRaycastResults);
            int count = cachedRaycastResults.Count;
            for (int i = 0; i < count; i++)
            {
                var result = cachedRaycastResults[i];
                if (result.gameObject == null || IsPhysicsWorldHit(result)) continue;
                if (result.gameObject.GetComponentInParent<ScrollRect>() != null)
                    return true;
            }
            return false;
        }

        public static bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (MainMenuUI.IsMenuVisible) return true;
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            cachedRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, cachedRaycastResults);

            int count = cachedRaycastResults.Count;
            for (int i = 0; i < count; i++)
            {
                var result = cachedRaycastResults[i];
                if (result.gameObject == null || IsPhysicsWorldHit(result))
                {
                    continue;
                }

                GameObject go = result.gameObject;
                TutorialSpotlightOverlay spotlight = go.GetComponent<TutorialSpotlightOverlay>();
                if (spotlight != null)
                {
                    if (spotlight.BlocksScreenPoint(screenPosition)) return true;
                    continue;
                }
                if (go.GetComponentInParent<TutorialMentorGuideUI>() != null &&
                    go.GetComponentInParent<Button>() == null &&
                    go.GetComponentInParent<Toggle>() == null)
                {
                    continue;
                }

                // 1. Gerçek UI grafiği + açık modal/tablet/pause: dünya tıklamasını kilitle
                if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen))
                {
                    return true;
                }

                // 2. Modal açık değilken sadece tıklanabilir interaktif buton/UI öğeleri 3D tıklamayı engeller
                if (go.GetComponentInParent<Selectable>() != null ||
                    go.GetComponentInParent<Button>() != null ||
                    go.GetComponentInParent<Toggle>() != null ||
                    go.GetComponentInParent<Slider>() != null ||
                    go.GetComponentInParent<InputField>() != null ||
                    go.GetComponentInParent<ScrollRect>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPhysicsWorldHit(RaycastResult result)
        {
            return result.module is PhysicsRaycaster || result.module is Physics2DRaycaster;
        }
    }

    /// <summary>
    /// TouchInputHelper için her karede girdi değerlendirmesini garanti altına alan koşucu.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class TouchInputRunner : MonoBehaviour
    {
        private void OnDisable() => WorldTapInput.CancelGesture();
        private void OnApplicationFocus(bool focused) => TouchInputHelper.ResetGestureState();
        private void OnApplicationPause(bool paused) => TouchInputHelper.ResetGestureState();
        private void Awake()
        {
            TouchInputHelper.EnsureRunner();
        }

        private void Update()
        {
            TouchInputHelper.EvaluateFrame();
        }

        private void LateUpdate()
        {
            UiReliableClick.Tick();
        }
    }
}
