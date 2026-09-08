using System;
using UnityEngine;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Günlük şans çarkı iş mantığı: günde bir çevirme, ödül seçimi, kasa ve kayıt senkronu.
    /// Gerçek takvim günü kullanılır (oyun içi günden bağımsız).
    /// </summary>
    public class DailySpinWheelManager : MonoBehaviour
    {
        public static DailySpinWheelManager Instance { get; private set; }

        public static readonly int[] PrizeAmounts =
        {
            15000, 8000, 5000, 3000, 2000, 1500, 1000, 750, 500, 250
        };

        private static readonly int[] PrizeWeights =
        {
            3, 5, 8, 10, 12, 14, 16, 18, 14, 10
        };

        public const float DefaultIconPosX = -72f;
        public const float DefaultIconPosY = 0f;

        private string lastDailySpinDate = "";
        private float iconPosX = DefaultIconPosX;
        private float iconPosY = DefaultIconPosY;
        private bool iconPosSaved;

        public string LastDailySpinDate => lastDailySpinDate;
        public float IconPosX => iconPosX;
        public float IconPosY => iconPosY;
        public bool IconPosSaved => iconPosSaved;
        public int SliceCount => PrizeAmounts.Length;

        public static void EnsureInstance()
        {
            if (Instance != null) return;

            GameObject managersObj = GameObject.Find("Core_Managers");
            if (managersObj == null)
            {
                managersObj = new GameObject("Core_Managers");
            }

            if (managersObj.GetComponent<DailySpinWheelManager>() == null)
            {
                managersObj.AddComponent<DailySpinWheelManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static string TodayKey()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        public bool CanSpinToday()
        {
            if (string.IsNullOrEmpty(lastDailySpinDate)) return true;
            return !string.Equals(lastDailySpinDate, TodayKey(), StringComparison.Ordinal);
        }

        public int PickWeightedPrizeIndex()
        {
            int total = 0;
            for (int i = 0; i < PrizeWeights.Length; i++)
            {
                total += PrizeWeights[i];
            }

            int roll = UnityEngine.Random.Range(0, Mathf.Max(1, total));
            int cumulative = 0;
            for (int i = 0; i < PrizeWeights.Length; i++)
            {
                cumulative += PrizeWeights[i];
                if (roll < cumulative) return i;
            }

            return PrizeAmounts.Length - 1;
        }

        public bool TryConsumeSpin(out int prizeIndex, out int prizeAmount)
        {
            prizeIndex = -1;
            prizeAmount = 0;
            if (!CanSpinToday()) return false;

            prizeIndex = PickWeightedPrizeIndex();
            prizeAmount = PrizeAmounts[prizeIndex];
            lastDailySpinDate = TodayKey();

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCredits(prizeAmount);
            }

            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordIncome(
                    FinanceCategories.DailySpin,
                    LocalizationManager.L("FinDesc_DailySpin", "Günlük Şans Çarkı Ödülü", "Daily Fortune Wheel Reward"),
                    prizeAmount);
            }

            AudioManager.Instance?.PlayCoins();
            PersistNow();
            return true;
        }

        public void SetIconPosition(float x, float y)
        {
            iconPosX = x;
            iconPosY = y;
            iconPosSaved = true;
        }

        public void CaptureToSave(SaveGameData saveData)
        {
            if (saveData == null) return;
            saveData.lastDailySpinDate = lastDailySpinDate ?? "";
            saveData.dailySpinIconPosX = iconPosX;
            saveData.dailySpinIconPosY = iconPosY;
            saveData.dailySpinIconPosSaved = iconPosSaved;
        }

        public void RestoreFromSave(SaveGameData saveData)
        {
            if (saveData == null)
            {
                ResetForNewGame();
                return;
            }

            lastDailySpinDate = saveData.lastDailySpinDate ?? "";
            if (saveData.dailySpinIconPosSaved)
            {
                iconPosX = saveData.dailySpinIconPosX;
                iconPosY = saveData.dailySpinIconPosY;
                iconPosSaved = true;
            }
            else
            {
                iconPosX = DefaultIconPosX;
                iconPosY = DefaultIconPosY;
                iconPosSaved = false;
            }
        }

        public void ResetForNewGame()
        {
            lastDailySpinDate = "";
            iconPosX = DefaultIconPosX;
            iconPosY = DefaultIconPosY;
            iconPosSaved = false;
        }

        public void PersistNow()
        {
            if (SaveSystemManager.Instance == null) return;
            int slot = SaveSystemManager.Instance.GetActiveSessionSlot();
            if (slot < 1 || slot > 3) return;
            SaveSystemManager.Instance.SaveCurrentGame(slot);
        }
    }
}
