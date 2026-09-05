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
        private static Vector2 pressPosition;
        private static float pressTime;
        private static bool isPressed;
        private static bool isDragging;
        private static bool wasMultiTouch;
        private static bool cleanTapTriggeredThisFrame;
        private static Vector2 lastTapPosition;
        private static int lastEvaluatedFrame = -1;
        private static float lastGlobalDispatchTime = 0f;

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
        }

        public static void SuppressNextTap()
        {
            lastGlobalDispatchTime = Time.unscaledTime;
            cleanTapTriggeredThisFrame = false;
            isPressed = false;
            isDragging = false;
            wasMultiTouch = false;
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
            cleanTapTriggeredThisFrame = false;

            int touchCount = GetTouchCount();
            if (touchCount > 1)
            {
                wasMultiTouch = true;
                isDragging = true;
            }

            Vector2 currentPointerPos = GetCurrentPointerPosition();

            // 1. Dokunma / Tıklama Başlangıcı (Pointer Down)
            if (WasPointerPressedThisFrame())
            {
                isPressed = true;
                isDragging = false;
                pressTime = Time.unscaledTime;
                pressPosition = currentPointerPos;
                wasMultiTouch = (touchCount > 1);
            }

            // 2. Basılı Tutma / Sürükleme Kontrolü (Pointer Hold / Move)
            if (isPressed)
            {
                if (currentPointerPos.sqrMagnitude > 0.001f && pressPosition.sqrMagnitude > 0.001f)
                {
                    float dragDist = Vector2.Distance(currentPointerPos, pressPosition);
                    if (dragDist > MaxTapDragDistance)
                    {
                        isDragging = true;
                    }
                }
            }

            // 3. Dokunma / Tıklama Bitişi (Pointer Up / Release)
            if (WasPointerReleasedThisFrame())
            {
                if (isPressed)
                {
                    isPressed = false;
                    Vector2 effectiveReleasePos = (currentPointerPos.sqrMagnitude > 0.001f) ? currentPointerPos : pressPosition;
                    float dragDist = (pressPosition.sqrMagnitude > 0.001f) ? Vector2.Distance(effectiveReleasePos, pressPosition) : 0f;
                    float duration = Time.unscaledTime - pressTime;

                    if (!wasMultiTouch && !isDragging && dragDist <= MaxTapDragDistance && duration <= MaxTapDuration)
                    {
                        cleanTapTriggeredThisFrame = true;
                        lastTapPosition = (pressPosition.sqrMagnitude > 0.001f) ? pressPosition : effectiveReleasePos;

                        // UI üzerinde değilse, Modal açık değilse ve Mobilya Yerleştirme modunda değilse 3D nesneleri tetikle
                        ModalManager.CloseWorldBlockingOverlays();
                        bool isPauseOpen = (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen);
                        bool isPlacing = (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing);
                        if (!ModalManager.IsModalOpen && !EKTPhoneManager.IsTabletOpen && !isPauseOpen && !isPlacing && !IsPointerOverUI(lastTapPosition))
                        {
                            Dispatch3DClick(lastTapPosition);
                        }
                    }
                }

                isDragging = false;
                wasMultiTouch = false;
            }
        }

        /// <summary>
        /// 3D dünyadaki nesneleri (Personel, Müşteri, Teslimat Kolisi, Raf, Tarla, Ahır)
        /// mobil dokunmatik öncelik sırasına göre pürüzsüzce tıklar.
        /// </summary>
        public static bool Dispatch3DClick(Vector2 screenPos)
        {
            ModalManager.CloseWorldBlockingOverlays();
            bool isPauseOpen = (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen);
            bool isPlacing = (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing);
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || isPauseOpen || isPlacing || IsPointerOverUI(screenPos)) return false;
            if (Time.unscaledTime - lastGlobalDispatchTime < 0.10f) return false;
            if (Time.unscaledTime - ModalManager.LastModalCloseTime < 0.08f) return false;
            if (screenPos == Vector2.zero) return false;

            Camera cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray, 250f);
            RaycastHit[] palletSphereHits = Physics.SphereCastAll(ray, 0.85f, 250f);

            // ÖNCELİK 0: Teslimat Kolisi veya Palet Tıklaması (Doğrudan ve Hızlı Erişim)
            if (TryDispatchPalletOrBox(hits) || TryDispatchPalletOrBox(palletSphereHits))
            {
                return true;
            }

            if (hits != null && hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    GameObject go = hit.collider.gameObject;

                    DeliveryBoxController box = go.GetComponentInParent<DeliveryBoxController>();
                    if (box == null) box = go.GetComponent<DeliveryBoxController>();
                    if (box != null)
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        box.TriggerPlacement();
                        return true;
                    }
                }
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
                }
            }

            // 2.b Dokunmatik & Tıklama Hassasiyeti için SphereCast (Mobilya & Palet Toleransı)
            if (sphereHits != null && sphereHits.Length > 0)
            {
                foreach (var sHit in sphereHits)
                {
                    if (sHit.collider == null) continue;
                    GameObject sGo = sHit.collider.gameObject;

                    // Teslimat Paleti
                    DeliveryPalletClickable pallet = sGo.GetComponentInParent<DeliveryPalletClickable>() ?? sGo.GetComponent<DeliveryPalletClickable>();
                    if (pallet != null || sGo.name.Contains("Pallet") || sGo.name.Contains("Delivery") ||
                        (sGo.transform.parent != null && sGo.transform.parent.name.Contains("Pallet")))
                    {
                        lastGlobalDispatchTime = Time.unscaledTime;
                        CloseAnyOpenProfileCard();
                        PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: false);
                        return true;
                    }

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
                if (hits[i].collider == null) continue;
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
                if (pallet != null || go.name.Contains("Pallet") || go.name.Contains("Delivery") || go.name.Contains("Cargo") ||
                    (go.transform.parent != null && (go.transform.parent.name.Contains("Pallet") || go.transform.parent.name.Contains("Delivery") || go.transform.parent.name.Contains("Cargo"))) ||
                    (go.transform.root != null && (go.transform.root.name.Contains("Pallet") || go.transform.root.name.Contains("Delivery"))))
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    CloseAnyOpenProfileCard();
                    PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: false);
                    return true;
                }
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

        public static int GetTouchCount()
        {
            if (!Application.isMobilePlatform && Input.touchCount == 0)
            {
                return 0;
            }

            try
            {
                if (Input.touchCount > 0) return Input.touchCount;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Touchscreen.current != null)
                {
                    int count = 0;
                    foreach (var touch in Touchscreen.current.touches)
                    {
                        if (touch.press.isPressed) count++;
                    }
                    return count;
                }
            }
            catch { }
#endif
            return 0;
        }

        public static Vector2 GetCurrentPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Touchscreen.current != null)
                {
                    var touch = Touchscreen.current.primaryTouch;
                    if (touch.press.isPressed)
                    {
                        Vector2 touchPos = touch.position.ReadValue();
                        if (touchPos.sqrMagnitude > 0.001f) return touchPos;
                    }
                }
                if (Mouse.current != null)
                {
                    Vector2 mPos = Mouse.current.position.ReadValue();
                    if (mPos.sqrMagnitude > 0.001f) return mPos;
                }
                if (Pointer.current != null && Pointer.current.press.isPressed)
                {
                    Vector2 pPos = Pointer.current.position.ReadValue();
                    if (pPos.sqrMagnitude > 0.001f) return pPos;
                }
            }
            catch { }
#endif
            try
            {
                if (Input.touchCount > 0)
                {
                    return Input.GetTouch(0).position;
                }
                Vector3 mPos = Input.mousePosition;
                if (mPos.sqrMagnitude > 0.001f)
                {
                    return new Vector2(mPos.x, mPos.y);
                }
            }
            catch { }

            return Vector2.zero;
        }

        public static bool IsPointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
                if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
                if (Pointer.current != null && Pointer.current.press.isPressed) return true;
            }
            catch { }
#endif
            try
            {
                if (Input.GetMouseButton(0)) return true;
                if (Input.touchCount > 0)
                {
                    UnityEngine.TouchPhase phase = Input.GetTouch(0).phase;
                    if (phase == UnityEngine.TouchPhase.Began || phase == UnityEngine.TouchPhase.Moved || phase == UnityEngine.TouchPhase.Stationary)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public static bool TryGetPressedPointerPosition(out Vector2 screenPos)
        {
            screenPos = Vector2.zero;
            if (!IsPointerHeld() && !WasPointerPressedThisFrame()) return false;

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                {
                    Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                    if (touchPos.sqrMagnitude > 1f)
                    {
                        screenPos = touchPos;
                        return true;
                    }
                }
                if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                {
                    Vector2 mPos = Mouse.current.position.ReadValue();
                    if (mPos.sqrMagnitude > 1f)
                    {
                        screenPos = mPos;
                        return true;
                    }
                }
                if (Pointer.current != null && Pointer.current.press.isPressed)
                {
                    Vector2 pPos = Pointer.current.position.ReadValue();
                    if (pPos.sqrMagnitude > 1f)
                    {
                        screenPos = pPos;
                        return true;
                    }
                }
            }
            catch { }
#endif
            try
            {
                if (Input.touchCount > 0)
                {
                    screenPos = Input.GetTouch(0).position;
                    if (screenPos.sqrMagnitude > 1f) return true;
                }
                if (Input.GetMouseButton(0))
                {
                    Vector3 mPos = Input.mousePosition;
                    if (mPos.sqrMagnitude > 1f)
                    {
                        screenPos = new Vector2(mPos.x, mPos.y);
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public static bool WasPointerPressedThisFrame()
        {
            try
            {
                if (Input.GetMouseButtonDown(0)) return true;
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began) return true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
                if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) return true;
            }
            catch { }
#endif
            return false;
        }

        public static bool WasPointerReleasedThisFrame()
        {
            try
            {
                if (Input.GetMouseButtonUp(0)) return true;
                if (Input.touchCount > 0 && (Input.GetTouch(0).phase == UnityEngine.TouchPhase.Ended || Input.GetTouch(0).phase == UnityEngine.TouchPhase.Canceled)) return true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) return true;
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) return true;
                if (Pointer.current != null && Pointer.current.press.wasReleasedThisFrame) return true;
            }
            catch { }
#endif
            return false;
        }

        private static readonly List<RaycastResult> cachedRaycastResults = new List<RaycastResult>(32);

        public static bool IsPointerOverUI(Vector2 screenPosition)
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
                if (result.gameObject == null || IsPhysicsWorldHit(result))
                {
                    continue;
                }

                GameObject go = result.gameObject;

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
    public class TouchInputRunner : MonoBehaviour
    {
        private void Awake()
        {
            TouchInputHelper.EnsureRunner();
        }

        private void Update()
        {
            TouchInputHelper.EvaluateFrame();
        }
    }
}
