using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;

namespace Farm2Shelf.Environment
{
    public class CustomerClickableTarget : MonoBehaviour, IPointerClickHandler
    {
        public CustomerProfileData profileData;

        private void Start()
        {
            // Tıklama tespiti için CapsuleCollider kontrolü
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.95f, 0f);
                col.radius = 0.45f;
                col.height = 1.9f;
            }
        }

        private void Update()
        {
            // Tıklama ve etkileşimler TouchInputHelper merkezi sistemi üzerinden yönetilir
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || Time.unscaledTime - ModalManager.LastModalCloseTime < 0.35f) return;
            OnCustomerClicked();
        }

        public void OnCustomerClicked()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || Time.unscaledTime - ModalManager.LastModalCloseTime < 0.35f) return;
            if (profileData == null) return;

            if (CustomerProfileModalUI.Instance == null)
            {
                GameObject uiObj = GameObject.Find("UI_Manager") ?? new GameObject("UI_Manager");
                if (uiObj.GetComponent<CustomerProfileModalUI>() == null)
                    uiObj.AddComponent<CustomerProfileModalUI>();
            }

            if (CustomerProfileModalUI.Instance != null)
            {
                CustomerProfileModalUI.Instance.ShowCustomerProfile(profileData);
            }
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

        private bool IsPointerOverUIButton()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;

            Vector2 pointerPos = GetPointerPosition();
            var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = pointerPos
            };
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject != null)
                {
                    if (r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null || r.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
                        return true;
                    if (r.gameObject.GetComponentInParent<UnityEngine.UI.InputField>() != null || r.gameObject.GetComponent<UnityEngine.UI.InputField>() != null)
                        return true;
                    if (r.gameObject.GetComponentInParent<UnityEngine.UI.Toggle>() != null || r.gameObject.GetComponent<UnityEngine.UI.Toggle>() != null)
                        return true;
                    if (r.gameObject.name.Contains("CloseButton"))
                        return true;
                }
            }

            return false;
        }
    }
}
