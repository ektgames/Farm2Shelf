using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        public const float MaxTapDragDistance = 38f; // Ekran pikselleri (Hafif parmak titremelerinde de tıklamayı yakalar)

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

                        // UI üzerinde değilse ve Modal açık değilse doğrudan 3D nesneleri tetikle
                        bool isPauseOpen = (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen);
                        if (!ModalManager.IsModalOpen && !EKTPhoneManager.IsTabletOpen && !isPauseOpen && !IsPointerOverUI(lastTapPosition))
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
        /// 3D dünyadaki nesneleri (Raf, Tarla, Ahır, Personel, Müşteri, Teslimat Kolisi)
        /// kameraya olan mesafelerine göre en yakından uzağa sıralayarak pürüzsüzce tıklar.
        /// </summary>
        public static bool Dispatch3DClick(Vector2 screenPos)
        {
            bool isPauseOpen = (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen);
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || isPauseOpen || IsPointerOverUI(screenPos)) return false;
            if (Time.unscaledTime - lastGlobalDispatchTime < 0.20f) return false;
            if (Time.unscaledTime - ModalManager.LastModalCloseTime < 0.35f) return false;
            if (screenPos == Vector2.zero) return false;

            Camera cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(screenPos);
            RaycastHit[] hits = Physics.RaycastAll(ray, 250f);
            if (hits == null || hits.Length == 0) return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // ÖNCELİK 0: Teslimat Kolisi veya Palet Tıklaması (Doğrudan ve Hızlı Erişim)
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                GameObject go = hit.collider.gameObject;

                DeliveryBoxController box = go.GetComponentInParent<DeliveryBoxController>();
                if (box == null) box = go.GetComponent<DeliveryBoxController>();
                if (box != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    box.TriggerPlacement();
                    return true;
                }

                WorkshopPalletClickable wsPallet = go.GetComponentInParent<WorkshopPalletClickable>();
                if (wsPallet == null) wsPallet = go.GetComponent<WorkshopPalletClickable>();
                if (wsPallet != null || go.name.Contains("Workshop_Pallet") || (go.transform.parent != null && go.transform.parent.name.Contains("Workshop_Pallet")))
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: true);
                    return true;
                }

                DeliveryPalletClickable pallet = go.GetComponentInParent<DeliveryPalletClickable>();
                if (pallet == null) pallet = go.GetComponent<DeliveryPalletClickable>();
                if (pallet != null || go.name.Contains("Pallet") || go.name.Contains("Delivery") || go.name.Contains("Cargo") ||
                    (go.transform.parent != null && (go.transform.parent.name.Contains("Pallet") || go.transform.parent.name.Contains("Delivery") || go.transform.parent.name.Contains("Cargo"))) ||
                    (go.transform.root != null && (go.transform.root.name.Contains("Pallet") || go.transform.root.name.Contains("Delivery"))))
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: false);
                    return true;
                }
            }

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                GameObject go = hit.collider.gameObject;

                // 1. Reyon / Raf / Dolap / Kasa / Depo Mobilyası
                PlacedFurnitureController furniture = go.GetComponentInParent<PlacedFurnitureController>();
                if (furniture == null) furniture = go.GetComponent<PlacedFurnitureController>();
                if (furniture != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    furniture.OnClickDetected();
                    return true;
                }

                // 2. Tarla / Ekim Alanı (Field Plot)
                FieldPlotController plot = go.GetComponentInParent<FieldPlotController>();
                if (plot == null) plot = go.GetComponent<FieldPlotController>();
                if (plot != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    plot.OnPlotClicked();
                    return true;
                }

                // 3. Ahır (Barn)
                BarnController barn = go.GetComponentInParent<BarnController>();
                if (barn == null) barn = go.GetComponent<BarnController>();
                if (barn != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    barn.OnBarnClicked();
                    return true;
                }

                // 4. Personel (Staff)
                StaffClickableTarget staff = go.GetComponentInParent<StaffClickableTarget>();
                if (staff == null) staff = go.GetComponent<StaffClickableTarget>();
                if (staff != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    staff.OnStaffClicked();
                    return true;
                }

                // 5. Müşteri (Customer)
                CustomerClickableTarget customer = go.GetComponentInParent<CustomerClickableTarget>();
                if (customer == null) customer = go.GetComponent<CustomerClickableTarget>();
                if (customer != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    customer.OnCustomerClicked();
                    return true;
                }

                // 6. Teslimat Kolisi / Palet Kutu (Delivery Box)
                DeliveryBoxController box = go.GetComponentInParent<DeliveryBoxController>();
                if (box == null) box = go.GetComponent<DeliveryBoxController>();
                if (box != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    box.TriggerPlacement();
                    return true;
                }

                // 7. Teslimat Paleti (Delivery Pallet)
                DeliveryPalletClickable pallet = go.GetComponentInParent<DeliveryPalletClickable>();
                if (pallet == null) pallet = go.GetComponent<DeliveryPalletClickable>();
                if (pallet != null)
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: false);
                    return true;
                }

                // 7.b Atölye Hammadde Paleti (Workshop Pallet)
                WorkshopPalletClickable wsPallet = go.GetComponentInParent<WorkshopPalletClickable>();
                if (wsPallet == null) wsPallet = go.GetComponent<WorkshopPalletClickable>();
                if (wsPallet != null || go.name.Contains("Workshop_Pallet") || (go.transform.parent != null && go.transform.parent.name.Contains("Workshop_Pallet")))
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: true);
                    return true;
                }

                if (go.name.Contains("Pallet") || (go.transform.parent != null && go.transform.parent.name.Contains("Pallet")))
                {
                    lastGlobalDispatchTime = Time.unscaledTime;
                    PalletStorageInventoryModalUI.ShowModal(isWorkshopMode: false);
                    return true;
                }
            }

            return false;
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

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Touchscreen.current != null)
                {
                    var touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                    if (touchPos.sqrMagnitude > 0.001f) return touchPos;
                }
                if (Pointer.current != null)
                {
                    var pPos = Pointer.current.position.ReadValue();
                    if (pPos.sqrMagnitude > 0.001f) return pPos;
                }
                if (Mouse.current != null)
                {
                    var mPos = Mouse.current.position.ReadValue();
                    if (mPos.sqrMagnitude > 0.001f) return mPos;
                }
            }
            catch { }
#endif
            return Vector2.zero;
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
                if (result.gameObject != null)
                {
                    GameObject go = result.gameObject;

                    // 1. Herhangi bir modal, tablet veya pause menüsü açıksa tüm UI arkasını kilitler
                    if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen))
                    {
                        return true;
                    }

                    // 2. Modal açık değilken sadece tıklanabilir interaktif buton/UI öğeleri 3D tıklamayı engeller
                    if (go.GetComponentInParent<UnityEngine.UI.Selectable>() != null ||
                        go.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                        go.GetComponentInParent<UnityEngine.UI.Toggle>() != null ||
                        go.GetComponentInParent<UnityEngine.UI.Slider>() != null ||
                        go.GetComponentInParent<UnityEngine.UI.InputField>() != null ||
                        go.GetComponentInParent<UnityEngine.UI.ScrollRect>() != null)
                    {
                        return true;
                    }
                }
            }

            return false;
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
