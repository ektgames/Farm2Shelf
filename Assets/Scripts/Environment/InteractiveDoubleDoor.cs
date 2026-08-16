using UnityEngine;
using Farm2Shelf.UI;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Tamamen otomatik, TIKLANAMAYAN duvara gömülen çift kanatlı kayar kapı mekanizması.
    /// Farenin veya dokunmatik ekranın tıklamalarına HİÇBİR ŞEKİLDE tepki vermez.
    /// SADECE insan/karakter kapı alanına yaklaştığında otomatik olarak duvara gömülerek açılır,
    /// ayrıldığında pürüzsüzce duvarın içinden çıkarak kapanır.
    /// </summary>
    public class InteractiveDoubleDoor : MonoBehaviour
    {
        [Header("Kapı Kanatları")]
        [SerializeField] private Transform leftLeaf;
        [SerializeField] private Transform rightLeaf;

        [Header("Açılma Ayarları")]
        [SerializeField] private float openDistance = 1.45f;
        [SerializeField] private float openSpeed = 6.0f;
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isSlideAlongX = true;

        private Vector3 leftClosedPos;
        private Vector3 rightClosedPos;
        private Vector3 leftOpenPos;
        private Vector3 rightOpenPos;

        private int humanCountInsideTrigger = 0;

        private void Start()
        {
            InitializePositions();
            SetupTriggerCollider();
        }

        public void SetupDoors(Transform left, Transform right, bool slideAlongX = true, float distance = 1.45f)
        {
            leftLeaf = left;
            rightLeaf = right;
            isSlideAlongX = slideAlongX;
            openDistance = distance;

            InitializePositions();
            SetupTriggerCollider();
        }

        private void InitializePositions()
        {
            if (leftLeaf == null || rightLeaf == null) return;

            leftClosedPos = leftLeaf.localPosition;
            rightClosedPos = rightLeaf.localPosition;

            if (isSlideAlongX)
            {
                // Sol kanat sol duvara (-X), Sağ kanat sağ duvara (+X) gömülür
                leftOpenPos = leftClosedPos + new Vector3(-openDistance, 0f, 0f);
                rightOpenPos = rightClosedPos + new Vector3(openDistance, 0f, 0f);
            }
            else
            {
                // Sol kanat sol duvara (-Z), Sağ kanat sağ duvara (+Z) gömülür
                leftOpenPos = leftClosedPos + new Vector3(0f, 0f, -openDistance);
                rightOpenPos = rightClosedPos + new Vector3(0f, 0f, openDistance);
            }
        }

        private void SetupTriggerCollider()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            col.isTrigger = true;
            col.size = new Vector3(4.8f, 3.5f, 4.8f);
            col.center = new Vector3(0f, 1.25f, 0f);
        }

        private void Update()
        {
            if (leftLeaf == null || rightLeaf == null) return;

            // Proximity Fallback: Çevrede 3.5 metre yakınlıkta herhangi bir insan/karakter var mı?
            bool nearbyHuman = (humanCountInsideTrigger > 0);
            if (!nearbyHuman)
            {
                Collider[] nearbyCols = Physics.OverlapSphere(transform.position, 3.5f);
                foreach (var c in nearbyCols)
                {
                    if (c != null && (c.name.Contains("Customer") || c.name.Contains("Staff") || c.name.Contains("Player") || c.CompareTag("Player")))
                    {
                        nearbyHuman = true;
                        break;
                    }
                }
            }

            isOpen = nearbyHuman;

            Vector3 targetLeft = isOpen ? leftOpenPos : leftClosedPos;
            Vector3 targetRight = isOpen ? rightOpenPos : rightClosedPos;

            leftLeaf.localPosition = Vector3.Lerp(leftLeaf.localPosition, targetLeft, Time.deltaTime * openSpeed);
            rightLeaf.localPosition = Vector3.Lerp(rightLeaf.localPosition, targetRight, Time.deltaTime * openSpeed);
        }

        public void OpenDoor()
        {
            if (ModalManager.IsModalOpen) return;
            isOpen = true;
        }

        public void CloseDoor()
        {
            if (ModalManager.IsModalOpen) return;
            isOpen = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ModalManager.IsModalOpen) return;
            humanCountInsideTrigger++;
        }

        private void OnTriggerExit(Collider other)
        {
            if (ModalManager.IsModalOpen) return;
            humanCountInsideTrigger = Mathf.Max(0, humanCountInsideTrigger - 1);
        }
    }
}
