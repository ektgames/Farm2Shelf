using UnityEngine;
using System;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Farm2Shelf Ekonomi ve Para (Coin) Yöneticisi.
    /// Başlangıç parası 500.000C (Coin) olarak belirlenmiştir.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Ekonomi State")]
        [SerializeField] private int currentCredits = 50000; // Başlangıç Parası: 50.000C!

        public event Action<int> OnCreditsChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SetCredits(int amount)
        {
            currentCredits = Mathf.Max(0, amount);
            OnCreditsChanged?.Invoke(currentCredits);
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0) return;
            currentCredits += amount;
            OnCreditsChanged?.Invoke(currentCredits);
        }

        public bool SpendCredits(int amount)
        {
            if (amount <= 0) return false;
            if (currentCredits >= amount)
            {
                currentCredits -= amount;
                OnCreditsChanged?.Invoke(currentCredits);
                return true;
            }
            return false;
        }

        public bool TrySpendCredits(int amount)
        {
            return SpendCredits(amount);
        }

        public int Credits => currentCredits;

        public string GetFormattedCredits()
        {
            return $"{currentCredits:N0}C";
        }
    }
}
