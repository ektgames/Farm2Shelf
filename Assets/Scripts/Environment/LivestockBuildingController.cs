using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public enum LivestockBuildingKind
    {
        ChickenCoop,
        CowBarn
    }

    public class LivestockBuildingController : MonoBehaviour, IPointerClickHandler
    {
        public LivestockBuildingKind Kind = LivestockBuildingKind.ChickenCoop;

        private void Start()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            if (Kind == LivestockBuildingKind.ChickenCoop)
            {
                col.center = new Vector3(0f, 1.6f, 0f);
                col.size = new Vector3(5.8f, 3.4f, 4.4f);
            }
            else
            {
                col.center = new Vector3(0f, 2.0f, 0f);
                col.size = new Vector3(7.0f, 4.2f, 5.4f);
            }
        }

        public void OnBuildingClicked()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || Time.unscaledTime - ModalManager.LastModalCloseTime < 0.35f) return;

            if (LivestockCoopModalUI.Instance == null)
            {
                GameObject uiObj = GameObject.Find("UI_Manager") ?? new GameObject("UI_Manager");
                if (uiObj.GetComponent<LivestockCoopModalUI>() == null)
                    uiObj.AddComponent<LivestockCoopModalUI>();
            }

            LivestockCoopModalUI.Instance?.ShowModal(Kind);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            OnBuildingClicked();
        }
    }
}
