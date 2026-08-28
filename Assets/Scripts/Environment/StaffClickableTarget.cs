using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.Core;
using Farm2Shelf.UI;

namespace Farm2Shelf.Environment
{
    public class StaffClickableTarget : MonoBehaviour, IPointerClickHandler
    {
        public StaffMember staffMember;
        public StaffTaskController.StaffTaskData taskData;

        private void Start()
        {
            // Tıklama tespiti için CapsuleCollider kontrolü ve mobil uyumlu genişletilmiş boyut
            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<CapsuleCollider>();
            }
            col.center = new Vector3(0f, 1.0f, 0f);
            col.radius = Mathf.Max(col.radius, 0.55f);
            col.height = Mathf.Max(col.height, 2.0f);
        }

        private void Update()
        {
            // Tıklama ve etkileşimler TouchInputHelper merkezi sistemi üzerinden yönetilir
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            bool isPauseOpen = (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen);
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || isPauseOpen) return;
            OnStaffClicked();
        }

        public CourierMotorcycleController courierMoto;

        public void OnStaffClicked()
        {
            bool isPauseOpen = (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen);
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || isPauseOpen) return;
            if (staffMember == null && taskData != null && taskData.staffMember != null)
            {
                staffMember = taskData.staffMember;
            }

            if (staffMember == null && courierMoto != null && courierMoto.AssignedCourier != null)
            {
                staffMember = courierMoto.AssignedCourier;
            }

            if (staffMember == null) return;

            string liveStatusText = "Mağaza Görevinde";

            if (staffMember.role == StaffRole.Kurye)
            {
                if (courierMoto == null)
                {
                    courierMoto = GetComponentInParent<CourierMotorcycleController>() ?? GetComponent<CourierMotorcycleController>();
                }

                if (courierMoto != null)
                {
                    switch (courierMoto.CurrentState)
                    {
                        case MotorcycleState.ParkedInBay:
                            liveStatusText = "Motor Park Yuvasında Yeni Sipariş Bekliyor 🛵";
                            break;
                        case MotorcycleState.WaitingForStocker:
                            liveStatusText = "Reyoncunun Sipariş Kolilerini Motora Yüklemesini Bekliyor 📦🛵";
                            break;
                        case MotorcycleState.EnRouteDelivery:
                            liveStatusText = "Müşterinin Adresine Motorsikletle Hızlı Teslimatta ⚡🛵";
                            break;
                        case MotorcycleState.DeliveringAtDoorstep:
                            liveStatusText = "Bina Kapısında Sipariş Paketini Müşteriye Teslim Ediyor 🏡✨";
                            break;
                        case MotorcycleState.ReturningToStore:
                            liveStatusText = "Teslimatı Tamamladı, Dükkana Geri Dönüyor 🏪🛵";
                            break;
                        default:
                            liveStatusText = "Online Market Teslimat Görevinde 🛵";
                            break;
                    }
                }
                else
                {
                    liveStatusText = "Online Market Teslimat Görevinde 🛵";
                }
            }
            else if (taskData != null)
            {
                switch (taskData.currentState)
                {
                    case StaffTaskController.StaffAIState.WorkingOnTask:
                        if (staffMember.role == StaffRole.Kasiyer)
                            liveStatusText = "Kasada Ödeme Alıyor & Müşteriye Hizmet Ediyor 💳";
                        else if (staffMember.role == StaffRole.Reyoncu)
                            liveStatusText = "Depo Rafından Kolileri Alıp Dükkan Raflarını Düzenliyor 📦";
                        else if (staffMember.role == StaffRole.Temizlikçi)
                            liveStatusText = "Mağaza Zeminini ve Çevreyi Temizliyor 🧹";
                        else if (staffMember.role == StaffRole.Güvenlik)
                            liveStatusText = "Mağaza İçi ve Otopark Güvenlik Devriyesinde 🛡️";
                        else
                            liveStatusText = "Müşteri Hizmetleri Masasında Destek Veriyor 💁‍♀️";
                        break;
                    case StaffTaskController.StaffAIState.WaitingInBreakRoom:
                        liveStatusText = "Personel Odasında Kahve Molasında ve Dinleniyor ☕";
                        break;
                    case StaffTaskController.StaffAIState.WalkingToBreakRoom:
                        liveStatusText = "Vardiya Sonrası Personel Odasına Yürüyor 🚶‍♂️";
                        break;
                    case StaffTaskController.StaffAIState.ProceedingToTask:
                        liveStatusText = "Vardiya Başlangıcı Görev Yerine İlerliyor 💼";
                        break;
                    case StaffTaskController.StaffAIState.HandingOverShift:
                        liveStatusText = "Kasa Başında Yeni Kasiyere Devir Teslim Yapıyor 🤝";
                        break;
                    case StaffTaskController.StaffAIState.WalkingToLeftExit:
                        liveStatusText = "Mesai Bitti, Evine Doğru Yürüyor 🏡";
                        break;
                    default:
                        liveStatusText = "Görev Alanında Çalışıyor 📋";
                        break;
                }
            }

            if (StaffProfileModalUI.Instance == null)
            {
                GameObject uiObj = GameObject.Find("UI_Manager") ?? new GameObject("UI_Manager");
                if (uiObj.GetComponent<StaffProfileModalUI>() == null)
                    uiObj.AddComponent<StaffProfileModalUI>();
            }

            if (StaffProfileModalUI.Instance != null)
            {
                StaffProfileModalUI.Instance.ShowStaffProfile(staffMember, liveStatusText);
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
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began))
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
