using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public class TaxiStandClickable : MonoBehaviour, IPointerClickHandler
    {
        public void EnsureCollider()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 2.2f, 2.2f);
            col.size = new Vector3(24.0f, 4.6f, 14.0f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.dragging) return;
            OpenJobs();
        }

        private void OnMouseDown()
        {
            OpenJobs();
        }

        private static void OpenJobs()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || Time.unscaledTime - ModalManager.LastModalCloseTime < 0.35f)
            {
                return;
            }

            if (EKTPhoneManager.Instance != null)
            {
                EKTPhoneManager.Instance.OpenJobsApp();
            }
        }
    }
}
