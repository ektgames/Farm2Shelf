using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Teslimat paleti üzerindeki her bir 3D koli objesini ve etkileşimlerini yönetir.
    /// Tıklandığında Palet Rafı Mobilya Deposu arayüzünü (PalletStorageInventoryModalUI) açar.
    /// </summary>
    public class DeliveryBoxController : MonoBehaviour, IPointerClickHandler
    {
        public FurnitureType furnitureType;
        public FurnitureType FurnitureType => furnitureType;

        public void SetupBox(FurnitureType type)
        {
            this.furnitureType = type;
        }

        private static bool CanOpenFromWorld()
        {
            return !TouchInputHelper.IsWorldInteractionBlocked();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.dragging || eventData.button != PointerEventData.InputButton.Left) return;
            TouchInputHelper.Dispatch3DClick(eventData.position);
        }

        public void TriggerPlacement()
        {
            if (!CanOpenFromWorld()) return;
            PalletStorageInventoryModalUI.ShowModal();
        }

        public void ShowHover(bool active)
        {
            // Hover tooltip kaldırıldı
        }
    }
}
