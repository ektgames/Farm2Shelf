using UnityEngine;
using System;
using Farm2Shelf.UI;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Farm2Shelf Dükkan Açık / Kapalı Durum Yöneticisi.
    /// </summary>
    public class StoreStatusManager : MonoBehaviour
    {
        public static StoreStatusManager Instance { get; private set; }

        [Header("Dükkan Durumu")]
        [SerializeField] private bool isOpen = false;

        public string PlayerName { get; private set; } = "Çiftçi Ali";
        public string CompanyName { get; private set; } = "Farm2Shelf Market";

        public event Action<bool> OnStoreStatusChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SetPlayerAndCompany(string playerName, string companyName)
        {
            if (!string.IsNullOrWhiteSpace(playerName)) PlayerName = playerName.Trim();
            if (!string.IsNullOrWhiteSpace(companyName)) CompanyName = companyName.Trim();
            Debug.Log($"[Farm2Shelf] Yeni Oyuncu Kurulumu: {PlayerName} | {CompanyName}");
        }

        public void ToggleStoreStatus()
        {
            SetStoreStatus(!isOpen);
        }

        public void SetStoreStatus(bool status)
        {
            if (status && !isOpen)
            {
                // Dükkan AÇILMAYA çalışılıyor: Önceki vardiyadan ayrılan personeller henüz tamamen çıkış yapmadıysa engelle!
                if (StaffTaskController.Instance != null && StaffTaskController.Instance.HasLeavingStaff())
                {
                    bool isEnglish = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;

                    string title = isEnglish ? "Shift Exit In Progress! ⏳" : "Vardiya Bitiş Süreci Tamamlanmadı! ⏳";
                    string msg = isEnglish ?
                        "Staff members from the previous shift have not fully left the premises yet.\n\nPlease wait for staff members to reach the exit point and leave before opening the store again." :
                        "Önceki vardiyadan ayrılan personeller henüz dükkandan ve alandan tamamen çıkış yapmadı.\n\nLütfen personellerin çıkış noktasına ulaşıp ayrılmasını bekleyin ve ardından dükkanı tekrar açın.";
                    string btnText = isEnglish ? "OK" : "Tamam";

                    ModalManager.ShowModal(title, msg, btnText);
                    return;
                }
            }

            if (isOpen != status)
            {
                isOpen = status;
                Debug.Log($"[Farm2Shelf] Dükkan Durumu: {(isOpen ? "AÇIK" : "KAPALI")}");
                if (isOpen && TimeManager.Instance != null)
                {
                    TimeManager.Instance.StartDayTimeFlow();
                }
                OnStoreStatusChanged?.Invoke(isOpen);
            }
        }

        public void OpenStore() => SetStoreStatus(true);
        public void CloseStore() => SetStoreStatus(false);

        public bool IsOpen => isOpen;
    }
}
