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

            string liveStatusText = LocalizationManager.L("StaffStatus_StoreDuty", "Mağaza Görevinde", "On Store Duty");

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
                            liveStatusText = LocalizationManager.L("StaffStatus_CourierParked", "Motor Park Yuvasında Yeni Sipariş Bekliyor 🛵", "Waiting for a New Order at the Motorcycle Bay 🛵");
                            break;
                        case MotorcycleState.WaitingForStocker:
                            liveStatusText = LocalizationManager.L("StaffStatus_CourierLoading", "Reyoncunun Sipariş Kolilerini Motora Yüklemesini Bekliyor 📦🛵", "Waiting for the Stocker to Load Order Boxes 📦🛵");
                            break;
                        case MotorcycleState.EnRouteDelivery:
                            liveStatusText = LocalizationManager.L("StaffStatus_CourierEnRoute", "Müşterinin Adresine Motorsikletle Hızlı Teslimatta ⚡🛵", "Making an Express Delivery to the Customer ⚡🛵");
                            break;
                        case MotorcycleState.DeliveringAtDoorstep:
                            liveStatusText = LocalizationManager.L("StaffStatus_CourierDoorstep", "Bina Kapısında Sipariş Paketini Müşteriye Teslim Ediyor 🏡✨", "Delivering the Order at the Customer's Door 🏡✨");
                            break;
                        case MotorcycleState.ReturningToStore:
                            liveStatusText = LocalizationManager.L("StaffStatus_CourierReturning", "Teslimatı Tamamladı, Dükkana Geri Dönüyor 🏪🛵", "Delivery Complete, Returning to the Store 🏪🛵");
                            break;
                        default:
                            liveStatusText = LocalizationManager.L("StaffStatus_CourierDuty", "Online Market Teslimat Görevinde 🛵", "On Online Market Delivery Duty 🛵");
                            break;
                    }
                }
                else
                {
                    liveStatusText = LocalizationManager.L("StaffStatus_CourierDuty", "Online Market Teslimat Görevinde 🛵", "On Online Market Delivery Duty 🛵");
                }
            }
            else if (taskData != null)
            {
                switch (taskData.currentState)
                {
                    case StaffTaskController.StaffAIState.WorkingOnTask:
                        if (staffMember.role == StaffRole.Kasiyer)
                            liveStatusText = LocalizationManager.L("StaffStatus_CashierWorking", "Kasada Ödeme Alıyor & Müşteriye Hizmet Ediyor 💳", "Processing Payments and Serving Customers 💳");
                        else if (staffMember.role == StaffRole.Reyoncu)
                            liveStatusText = LocalizationManager.L("StaffStatus_StockerWorking", "Depo Rafından Kolileri Alıp Dükkan Raflarını Düzenliyor 📦", "Restocking Store Shelves from the Warehouse 📦");
                        else if (staffMember.role == StaffRole.Temizlikçi)
                            liveStatusText = LocalizationManager.L("StaffStatus_CleanerWorking", "Mağaza Zeminini ve Çevreyi Temizliyor 🧹", "Cleaning the Store Floor and Surroundings 🧹");
                        else if (staffMember.role == StaffRole.Güvenlik)
                            liveStatusText = LocalizationManager.L("StaffStatus_GuardWorking", "Mağaza İçi ve Otopark Güvenlik Devriyesinde 🛡️", "Patrolling the Store and Parking Lot 🛡️");
                        else
                            liveStatusText = LocalizationManager.L("StaffStatus_SupportWorking", "Müşteri Hizmetleri Masasında Destek Veriyor 💁‍♀️", "Assisting Customers at the Service Desk 💁‍♀️");
                        break;
                    case StaffTaskController.StaffAIState.WaitingInBreakRoom:
                        liveStatusText = LocalizationManager.L("StaffStatus_OnBreak", "Personel Odasında Kahve Molasında ve Dinleniyor ☕", "Taking a Coffee Break in the Staff Room ☕");
                        break;
                    case StaffTaskController.StaffAIState.WalkingToBreakRoom:
                        liveStatusText = LocalizationManager.L("StaffStatus_ToBreakRoom", "Vardiya Sonrası Personel Odasına Yürüyor 🚶‍♂️", "Walking to the Staff Room After the Shift 🚶‍♂️");
                        break;
                    case StaffTaskController.StaffAIState.ProceedingToTask:
                        liveStatusText = LocalizationManager.L("StaffStatus_ToTask", "Vardiya Başlangıcı Görev Yerine İlerliyor 💼", "Heading to the Workstation at Shift Start 💼");
                        break;
                    case StaffTaskController.StaffAIState.HandingOverShift:
                        liveStatusText = LocalizationManager.L("StaffStatus_Handover", "Kasa Başında Yeni Kasiyere Devir Teslim Yapıyor 🤝", "Handing Over the Checkout to the Next Cashier 🤝");
                        break;
                    case StaffTaskController.StaffAIState.WalkingToLeftExit:
                        liveStatusText = LocalizationManager.L("StaffStatus_Leaving", "Mesai Bitti, Evine Doğru Yürüyor 🏡", "Shift Over, Heading Home 🏡");
                        break;
                    default:
                        liveStatusText = LocalizationManager.L("StaffStatus_Working", "Görev Alanında Çalışıyor 📋", "Working at the Assigned Station 📋");
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
