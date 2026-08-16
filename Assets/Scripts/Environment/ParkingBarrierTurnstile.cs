using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Otopark giriş bariyeri (Turnike) bileşeni.
    /// Modal pencere açıkken (ModalManager.IsModalOpen == true) tıklamaları ve tetiklenmeyi kilitler.
    /// </summary>
    public class ParkingBarrierTurnstile : MonoBehaviour, IPointerClickHandler
    {
        [Header("Bariyer Bileşenleri")]
        [SerializeField] private Transform barrierArm;
        [SerializeField] private float openAngle = 85f;
        [SerializeField] private float openSpeed = 4f;
        [SerializeField] private bool isOpen = false;

        private Quaternion closedRotation;
        private Quaternion openRotation;

        private void Start()
        {
            InitializeRotations();
            SetupTriggerCollider();
        }

        public void SetupTurnstile(Transform arm)
        {
            barrierArm = arm;
            InitializeRotations();
            SetupTriggerCollider();
        }

        private void InitializeRotations()
        {
            if (barrierArm == null) return;

            closedRotation = barrierArm.localRotation;

            // Kolun uzandığı yön (Sağ / Sol) tespit edilerek her 2 turnikenin de DİK YUKARI (GÖKYÜZÜNE) açılması sağlanır:
            Transform childArm = barrierArm.childCount > 0 ? barrierArm.GetChild(0) : barrierArm;
            float armDirectionX = childArm != null ? childArm.localPosition.x : -1.5f;

            float rotZ = (armDirectionX < 0f) ? -85f : 85f;
            openRotation = closedRotation * Quaternion.Euler(0f, 0f, rotZ);
        }

        private void SetupTriggerCollider()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            col.isTrigger = true;
            col.size = new Vector3(5f, 4f, 5f);
            col.center = Vector3.zero;
        }

        private void Update()
        {
            if (barrierArm == null) return;

            Quaternion targetRot = isOpen ? openRotation : closedRotation;
            barrierArm.localRotation = Quaternion.Slerp(barrierArm.localRotation, targetRot, Time.deltaTime * openSpeed);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // MANÜEL TIKLAMA DEVRE DIŞI: Bariyerler sadece araç yaklaşınca otomatik açılır!
        }

        private void OnMouseDown()
        {
            // MANÜEL TIKLAMA DEVRE DIŞI
        }

        public void ToggleBarrier()
        {
            if (ModalManager.IsModalOpen) return;
            isOpen = !isOpen;
            Debug.Log($"[Farm2Shelf] Otopark Bariyeri {(isOpen ? "AÇILDI" : "KAPANDI")}");
        }

        public void OpenBarrier()
        {
            if (ModalManager.IsModalOpen) return;
            isOpen = true;
        }

        public void CloseBarrier()
        {
            if (ModalManager.IsModalOpen) return;
            isOpen = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ModalManager.IsModalOpen) return;
            OpenBarrier();
        }

        private void OnTriggerExit(Collider other)
        {
            if (ModalManager.IsModalOpen) return;
            CloseBarrier();
        }
    }
}
