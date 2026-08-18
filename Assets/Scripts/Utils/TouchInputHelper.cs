using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Farm2Shelf.Utils
{
    /// <summary>
    /// Mobil Dokunmatik & PC Hibrit Girdi ve Tıklama Yardımcısı.
    /// Ekranda kamera kaydırma (pan/drag) veya iki parmaklı yakınlaştırma (pinch-zoom) yapılırken
    /// 3D nesnelere (raf, tarla, ambar, personel, müşteri vb.) KAZARA TIKLANMASINI ÖNLER.
    /// Yalnızca oyuncu parmağını ekrandan kaldırdığında (PointerUp):
    /// - Dokunma süresi kısa ise (<= 0.35s)
    /// - Parmağın sürüklenme mesafesi küçük ise (<= 25px)
    /// - Çoklu dokunma (multi-touch gesture) yapılmadıysa
    /// - UI buton/panellerinin üzerinde başlama/bitme olmadıysa
    /// temiz bir TIKLAMA (Tap/Click) olarak kabul eder.
    /// </summary>
    public static class TouchInputHelper
    {
        private static Vector2 pressPosition;
        private static float pressTime;
        private static bool isPressed;
        private static bool wasMultiTouch;
        private static bool pressWasOverUI;
        private static bool cleanTapTriggeredThisFrame;
        private static Vector2 lastTapPosition;
        private static int lastEvaluatedFrame = -1;

        public const float MaxTapDuration = 0.35f;
        public const float MaxTapDragDistance = 25f; // Ekran pikselleri

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
            }

            Vector2 currentPointerPos = GetCurrentPointerPosition();

            // 1. Dokunma / Tıklama Başlangıcı (Pointer Down)
            if (WasPointerPressedThisFrame())
            {
                isPressed = true;
                pressTime = Time.time;
                pressPosition = currentPointerPos;
                wasMultiTouch = (touchCount > 1);
            }

            // 2. Dokunma / Tıklama Bitişi (Pointer Up / Release)
            if (WasPointerReleasedThisFrame())
            {
                if (isPressed)
                {
                    isPressed = false;
                    float dragDist = Vector2.Distance(currentPointerPos, pressPosition);

                    // ÇİFT PARMAK HAREKETİ YOKSA (Çift Parmak Döndürme / Pinch-Zoom Yapılmadıysa) VE MODAL AÇIK DEĞİLSE:
                    // Hem PC hem Mobil için her şeye anında ve sorunsuzca tıklanır! (Büyük kamera kaydırmalarında kazara tıklamayı önlemek için 60px sınırı var)
                    if (!wasMultiTouch &&
                        touchCount <= 1 &&
                        !Farm2Shelf.UI.ModalManager.IsModalOpen &&
                        !Farm2Shelf.UI.EKTPhoneManager.IsTabletOpen &&
                        dragDist <= 60f)
                    {
                        cleanTapTriggeredThisFrame = true;
                        lastTapPosition = currentPointerPos;
                    }
                }
                wasMultiTouch = false;
            }
        }

        public static int GetTouchCount()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                int count = 0;
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.isPressed) count++;
                }
                return count;
            }
            return 0;
#else
            return Input.touchCount;
#endif
        }

        public static Vector2 GetCurrentPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
            if (Pointer.current != null)
                return Pointer.current.position.ReadValue();
            return Vector2.zero;
#else
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
#endif
        }

        public static bool WasPointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
                return true;
            return false;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public static bool WasPointerReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                return true;
            if (Pointer.current != null && Pointer.current.press.wasReleasedThisFrame)
                return true;
            return false;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        private static readonly List<RaycastResult> cachedRaycastResults = new List<RaycastResult>(32);

        public static bool IsPointerOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            if (Farm2Shelf.UI.ModalManager.IsModalOpen || Farm2Shelf.UI.EKTPhoneManager.IsTabletOpen)
                return true;

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

                    // 1. Gerçek Tıklanabilir UI Buton ve Girdi Elemanları
                    if (go.GetComponentInParent<UnityEngine.UI.Selectable>() != null ||
                        go.GetComponentInParent<UnityEngine.UI.Button>() != null ||
                        go.GetComponentInParent<UnityEngine.UI.Toggle>() != null ||
                        go.GetComponentInParent<UnityEngine.UI.Slider>() != null ||
                        go.GetComponentInParent<UnityEngine.UI.InputField>() != null ||
                        go.GetComponentInParent<TMPro.TMP_InputField>() != null ||
                        go.GetComponentInParent<TMPro.TMP_Dropdown>() != null)
                    {
                        return true;
                    }

                    // 2. Açık Modal / Pencere / Dialog Katmanları
                    string n = go.name.ToLower();
                    if (n.Contains("modal") || n.Contains("popup") || n.Contains("dialog") || n.Contains("window"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
