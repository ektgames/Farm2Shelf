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
    public class BarnController : MonoBehaviour, IPointerClickHandler
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
            // Tıklama ve etkileşimler TouchInputHelper merkezi sistemi üzerinden yönetilir
        }

        public void OnBarnClicked()
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen) return;
            OnBarnClicked();
        }

        private bool WasPointerPressedThisFrame()
        {
            try { if (Input.GetMouseButtonDown(0)) return true; } catch { }

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                return true;
#endif

            return false;
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
            try
            {
                Vector3 mPos = Input.mousePosition;
                if (mPos.sqrMagnitude > 0.01f) return new Vector2(mPos.x, mPos.y);
            }
            catch { }

#if ENABLE_INPUT_SYSTEM
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
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
            {
                return UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            }
#endif

            return Vector2.zero;
        }
    }
}
