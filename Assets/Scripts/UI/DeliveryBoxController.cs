using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.Core;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Teslimat paleti üzerindeki her bir 3D koli objesini ve etkileşimlerini yönetir.
    /// Tıklandığında Palet Rafı Mobilya Deposu arayüzünü (PalletStorageInventoryModalUI) açar.
    /// </summary>
    public class DeliveryBoxController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        public FurnitureType furnitureType;
        public FurnitureType FurnitureType => furnitureType;

        public void SetupBox(FurnitureType type)
        {
            this.furnitureType = type;
        }

        private void OnMouseDown()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen)) return;
            TriggerPlacement();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen)) return;
            TriggerPlacement();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen)) return;
            TriggerPlacement();
        }

        public void TriggerPlacement()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPauseMenuOpen)) return;
            PalletStorageInventoryModalUI.ShowModal();
        }

        public void ShowHover(bool active)
        {
            // Hover tooltip kaldırıldı
        }
    }
}
