using UnityEngine;
using System;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Farm2Shelf Ekonomi ve Para (Coin) Yöneticisi.
    /// Oyuncu alışverişle eksiye inemez; gece yarısı zorunlu ödemeler bakiyeyi eksiye çekebilir.
    /// Bakiye -2000C'ye düşünce iflas (game over) açılır.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }
        public const int BankruptcyLimit = -2000;
        public const int StartingCredits = 500000;

        [Header("Ekonomi State")]
        [SerializeField] private int currentCredits = StartingCredits;

        public event Action<int> OnCreditsChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SetCredits(int amount)
        {
            currentCredits = amount;
            OnCreditsChanged?.Invoke(currentCredits);
            EvaluateBankruptcy();
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0) return;
            currentCredits += amount;
            OnCreditsChanged?.Invoke(currentCredits);
        }

        /// <summary>
        /// Oyuncu alışverişi: bakiyeyi eksiye çekemez. Yetersizse false döner.
        /// </summary>
        public bool SpendCredits(int amount)
        {
            if (amount <= 0) return false;
            if (currentCredits < amount) return false;
            currentCredits -= amount;
            OnCreditsChanged?.Invoke(currentCredits);
            return true;
        }

        public bool TrySpendCredits(int amount)
        {
            return SpendCredits(amount);
        }

        public bool CanAfford(int amount)
        {
            return amount > 0 && currentCredits >= amount;
        }

        /// <summary>
        /// Maaş / kredi taksiti gibi zorunlu kesinti. Bakiyeyi eksiye düşürebilir.
        /// </summary>
        public void ForceDeductCredits(int amount)
        {
            if (amount <= 0) return;
            currentCredits -= amount;
            OnCreditsChanged?.Invoke(currentCredits);
            EvaluateBankruptcy();
        }

        private void EvaluateBankruptcy()
        {
            if (currentCredits > BankruptcyLimit) return;
            if (GameOverUI.Instance != null)
            {
                GameOverUI.Instance.ShowIfBankrupt(currentCredits);
            }
        }

        public int Credits => currentCredits;

        public string GetFormattedCredits()
        {
            return $"{currentCredits:N0}C";
        }
    }
}
