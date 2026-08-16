using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Core
{
    [Serializable]
    public class TransactionRecord
    {
        public string id;
        public string timeStamp;
        public string category; // Satış, Maaş, Lojistik, Tohum/Çiftlik, Başlangıç Sermayesi
        public string description;
        public int amount;
        public bool isIncome;

        public TransactionRecord(string id, string timeStamp, string category, string description, int amount, bool isIncome)
        {
            this.id = id;
            this.timeStamp = timeStamp;
            this.category = category;
            this.description = description;
            this.amount = amount;
            this.isIncome = isIncome;
        }
    }

    /// <summary>
    /// Farm2Shelf Finansal Gelir, Gider, Günlük/Aylık Kâr ve İşlem Dökümü Yöneticisi.
    /// Bütün harcamaları, satış gelirlerini, net kârları ve işlem geçmişini kaydeder.
    /// </summary>
    public class FinanceManager : MonoBehaviour
    {
        public static FinanceManager Instance { get; private set; }

        [Header("Finansal State")]
        private int totalRevenue = 0;
        private int totalExpenses = 0;

        private int dailyRevenue = 0;
        private int dailyExpenses = 0;

        private int monthlyRevenue = 0;
        private int monthlyExpenses = 0;

        private List<TransactionRecord> transactionLog = new List<TransactionRecord>();

        public event Action OnFinanceUpdated;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMidnightRollover += HandleMidnightRollover;
            }
        }

        private void RecordTransactionInternal(string category, string description, int amount, bool isIncome)
        {
            string timeStr = TimeManager.Instance != null ? TimeManager.Instance.GetFormattedTime() : "06:00";
            string defaultDate = LocalizationManager.L("Date_DefaultInit", "İLKBAHAR • GÜN 1", "SPRING • DAY 1");
            string dateStr = TimeManager.Instance != null ? TimeManager.Instance.GetFormattedDate() : defaultDate;

            string fullTimeStamp = $"{dateStr} {timeStr}";
            string recId = "TRX" + UnityEngine.Random.Range(1000, 9999);

            TransactionRecord record = new TransactionRecord(recId, fullTimeStamp, category, description, amount, isIncome);
            transactionLog.Insert(0, record); // En yeni işlem üstte!

            if (isIncome)
            {
                totalRevenue += amount;
                dailyRevenue += amount;
                monthlyRevenue += amount;
            }
            else
            {
                totalExpenses += amount;
                dailyExpenses += amount;
                monthlyExpenses += amount;
            }
        }

        public void RecordIncome(string category, string description, int amount)
        {
            if (amount <= 0) return;
            RecordTransactionInternal(category, description, amount, true);
            OnFinanceUpdated?.Invoke();
        }

        public void RecordExpense(string category, string description, int amount)
        {
            if (amount <= 0) return;
            RecordTransactionInternal(category, description, amount, false);
            OnFinanceUpdated?.Invoke();
        }

        public bool SpendMoney(float amount, string description)
        {
            int intAmount = Mathf.RoundToInt(amount);
            if (EconomyManager.Instance != null)
            {
                if (!EconomyManager.Instance.SpendCredits(intAmount)) return false;
            }
            RecordExpense("Tohum/Çiftlik", description, intAmount);
            return true;
        }

        private void HandleMidnightRollover()
        {
            // Gece yarısında Z Raporu için veriler muhafaza edilir
            OnFinanceUpdated?.Invoke();
        }

        public void ResetDailyStats()
        {
            dailyRevenue = 0;
            dailyExpenses = 0;
            OnFinanceUpdated?.Invoke();
        }

        // --- HESAPLANAN FİNANSAL METRİKLER ---
        public int CurrentBalance => EconomyManager.Instance != null ? EconomyManager.Instance.Credits : 500000;
        public int TotalRevenue => totalRevenue;
        public int TotalExpenses => totalExpenses;
        public int NetProfit => totalRevenue - totalExpenses;

        public int DailyRevenue => dailyRevenue;
        public int DailyExpenses => dailyExpenses;
        public int DailyNetProfit => dailyRevenue - dailyExpenses;

        public int MonthlyRevenue => monthlyRevenue;
        public int MonthlyExpenses => monthlyExpenses;
        public int MonthlyNetProfit => monthlyRevenue - monthlyExpenses;

        public float ProfitMargin
        {
            get
            {
                if (totalRevenue <= 0) return 0f;
                return ((float)NetProfit / totalRevenue) * 100f;
            }
        }

        public List<TransactionRecord> GetTransactionHistory() => transactionLog;

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMidnightRollover -= HandleMidnightRollover;
            }
        }
    }
}
