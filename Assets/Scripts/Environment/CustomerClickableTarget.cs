using UnityEngine;
using Farm2Shelf.UI;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Müşteri 3D modellerine eklenen tıklama algılayıcı bileşen.
    /// PC (Fare Tıklaması) ve Mobil (Dokunma) ile sorunsuzca çalışır.
    /// Tıklandığında ekranın sol alt kısmında Müşteri Profil Kartı açılır.
    /// </summary>
    public class CustomerClickableTarget : MonoBehaviour
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
            if (WasPointerPressedThisFrame() && !IsPointerOverUIButton() && !EKTPhoneManager.IsTabletOpen && !ModalManager.IsModalOpen)
            {
                Camera mainCam = Camera.main;
                if (mainCam == null) return;

                Vector2 pointerPos = GetPointerPosition();
                Ray ray = mainCam.ScreenPointToRay(pointerPos);

                RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
                if (hits != null && hits.Length > 0)
                {
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (var h in hits)
                    {
                        if (h.collider == null) continue;
                        CustomerClickableTarget target = h.collider.GetComponentInParent<CustomerClickableTarget>();
                        if (target == null) target = h.collider.GetComponent<CustomerClickableTarget>();

                        if (target == this)
                        {
                            OnCustomerClicked();
                            break;
                        }
                    }
                }
            }
        }

        private void OnCustomerClicked()
        {
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
