using UnityEngine;
using System;

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
            isOpen = !isOpen;
            Debug.Log($"[Farm2Shelf] Dükkan Durumu: {(isOpen ? "AÇIK" : "KAPALI")}");
            if (isOpen && TimeManager.Instance != null)
            {
                TimeManager.Instance.StartDayTimeFlow();
            }
            OnStoreStatusChanged?.Invoke(isOpen);
        }

        public void SetStoreStatus(bool status)
        {
            if (isOpen != status)
            {
                isOpen = status;
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
