using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Çiftlik alanında duran 3D Ahır binasını tıklanabilir hale getiren bileşen.
    /// Tıklandığında Ahır Envanteri modal penceresini açar.
    /// </summary>
    public class BarnController : MonoBehaviour
    {
        private void Start()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 2.5f, 0f);
                col.size = new Vector3(8.0f, 5.0f, 8.0f);
            }
        }

        private void Update()
        {
            if (WasPointerPressedThisFrame() && !IsPointerOverUIButton() && !ModalManager.IsModalOpen && !EKTPhoneManager.IsTabletOpen)
            {
                Camera mainCam = Camera.main;
                if (mainCam == null) return;

                Vector2 pointerPos = GetPointerPosition();
                Ray ray = mainCam.ScreenPointToRay(pointerPos);

                RaycastHit[] hits = Physics.RaycastAll(ray, 150f);
                if (hits != null && hits.Length > 0)
                {
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (var h in hits)
                    {
                        if (h.collider == null) continue;
                        BarnController barn = h.collider.GetComponentInParent<BarnController>();
                        if (barn == null) barn = h.collider.GetComponent<BarnController>();

                        if (barn == this)
                        {
                            OnBarnClicked();
                            break;
                        }
                    }
                }
            }
        }

        private void OnBarnClicked()
        {
            if (BarnInventoryModalUI.Instance == null)
            {
                GameObject uiObj = GameObject.Find("UI_Manager") ?? new GameObject("UI_Manager");
                if (uiObj.GetComponent<BarnInventoryModalUI>() == null)
                    uiObj.AddComponent<BarnInventoryModalUI>();
            }

            if (BarnInventoryModalUI.Instance != null)
            {
                BarnInventoryModalUI.Instance.ShowModal();
            }
        }

        private bool WasPointerPressedThisFrame()
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
            try
            {
                if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                    return true;
            }
            catch {}

            try
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                    return true;
            }
            catch {}

            return false;
#endif
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
    }
}
