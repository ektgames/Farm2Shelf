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
        public string category;
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

    [Serializable]
    public class FinanceCategoryTotalSave
    {
        public string category;
        public int income;
        public int expense;
    }

    public static class FinanceCategories
    {
        public const string Sales = "Satış";
        public const string Salary = "Maaş";
        public const string Farm = "Tohum/Çiftlik";
        public const string Wholesale = "Toptan/Alışveriş";
        public const string Furniture = "Mobilya";
        public const string Animals = "Hayvan";
        public const string Renovation = "Tadilat";
        public const string Expansion = "Geliştirme";
        public const string OnlineDelivery = "Online Market & Kurye Geliri";
        public const string TownContracts = "Kasaba Kontratları";
        public const string Vehicles = "Araçlar & Filo";
        public const string StocksBuy = "Borsa Yatırımı";
        public const string StocksSell = "Borsa Geliri";
        public const string BankLoan = "Banka Kredisi";
        public const string BankLoanPayoff = "Banka Kredisi Ödemesi";
        public const string BankLoanInstallment = "Banka Kredisi Taksiti";
        public const string Passive = "Pasif Gelir";
        public const string DailySpin = "Günlük Çark";
        public const string Overtime = "Personel Mesai";
        public const string Inspection = "Müfettiş Denetimi";

        public static string Localize(string category)
        {
            if (string.IsNullOrEmpty(category)) return category;
            switch (category)
            {
                case Sales: return LocalizationManager.L("TrxCat_Sales", "Satış", "Sales");
                case Salary: return LocalizationManager.L("TrxCat_Salary", "Maaş", "Salary");
                case Farm: return LocalizationManager.L("TrxCat_Farm", "Tohum/Çiftlik", "Seeds/Farm");
                case Wholesale: return LocalizationManager.L("TrxCat_Wholesale", "Toptan/Alışveriş", "Wholesale/Shopping");
                case Furniture: return LocalizationManager.L("TrxCat_Furniture", "Mobilya", "Furniture");
                case Animals: return LocalizationManager.L("TrxCat_Animals", "Hayvan", "Livestock");
                case Renovation: return LocalizationManager.L("TrxCat_Renovation", "Tadilat", "Renovation");
                case Expansion: return LocalizationManager.L("TrxCat_Expansion", "Geliştirme", "Expansion");
                case OnlineDelivery: return LocalizationManager.L("TrxCat_OnlineDelivery", "Online Market & Kurye Geliri", "Online Market & Courier Revenue");
                case TownContracts: return LocalizationManager.L("TrxCat_TownContracts", "Kasaba Kontratları", "Town Contracts");
                case Vehicles: return LocalizationManager.L("TrxCat_Vehicles", "Araçlar & Filo", "Vehicles & Fleet");
                case StocksBuy: return LocalizationManager.L("TrxCat_Stock", "Borsa Yatırımı", "Stock Investment");
                case StocksSell: return LocalizationManager.L("TrxCat_StockIncome", "Borsa Geliri", "Stock Revenue");
                case BankLoanInstallment: return LocalizationManager.L("TrxCat_BankLoanInst", "Banka Kredisi Taksiti", "Bank Loan Installment");
                case BankLoanPayoff: return LocalizationManager.L("TrxCat_BankLoanPayoff", "Banka Kredisi Ödemesi", "Bank Loan Payoff");
                case BankLoan: return LocalizationManager.L("TrxCat_BankLoan", "Banka Kredisi", "Bank Loan");
                case Passive: return LocalizationManager.L("TrxCat_Passive", "Pasif Gelir", "Passive Income");
                case DailySpin: return LocalizationManager.L("TrxCat_DailySpin", "Günlük Çark", "Daily Wheel");
                case Overtime: return LocalizationManager.L("TrxCat_Overtime", "Personel Mesai", "Staff Overtime");
                case Inspection: return LocalizationManager.L("TrxCat_Inspection", "Müfettiş Denetimi", "Inspector Visit");
                default: return category;
            }
        }

        public static bool IsProtected(string category)
        {
            if (string.IsNullOrEmpty(category)) return false;
            return category == Salary
                || category == Overtime
                || category == BankLoan
                || category == BankLoanPayoff
                || category == BankLoanInstallment;
        }
    }

    /// <summary>
    /// Farm2Shelf Finansal Gelir, Gider, Günlük/Aylık Kâr ve İşlem Dökümü Yöneticisi.
    /// </summary>
    public class FinanceManager : MonoBehaviour
    {
        public static FinanceManager Instance { get; private set; }

        private const int MaxTransactionLog = 500;
        private int nextRecordSerial = 1;

        private int totalRevenue = 0;
        private int totalExpenses = 0;
        private int dailyRevenue = 0;
        private int dailyExpenses = 0;
        private int monthlyRevenue = 0;
        private int monthlyExpenses = 0;

        private readonly List<TransactionRecord> transactionLog = new List<TransactionRecord>();
        private readonly Dictionary<string, int> categoryIncome = new Dictionary<string, int>();
        private readonly Dictionary<string, int> categoryExpense = new Dictionary<string, int>();

        public int TotalRevenue => totalRevenue;
        public int TotalExpenses => totalExpenses;
        public int DailyRevenue => dailyRevenue;
        public int DailyExpenses => dailyExpenses;
        public int MonthlyRevenue => monthlyRevenue;
        public int MonthlyExpenses => monthlyExpenses;
        public List<TransactionRecord> GetTransactionLog() => transactionLog;

        public event Action OnFinanceUpdated;

        public void RestoreFinanceData(int totRev, int totExp, int dRev, int dExp, int mRev, int mExp, List<TransactionRecord> logs)
        {
            RestoreFinanceData(totRev, totExp, dRev, dExp, mRev, mExp, logs, null);
        }

        public void RestoreFinanceData(int totRev, int totExp, int dRev, int dExp, int mRev, int mExp, List<TransactionRecord> logs, List<FinanceCategoryTotalSave> categoryTotals)
        {
            totalRevenue = totRev;
            totalExpenses = totExp;
            dailyRevenue = dRev;
            dailyExpenses = dExp;
            monthlyRevenue = mRev;
            monthlyExpenses = mExp;
            transactionLog.Clear();
            categoryIncome.Clear();
            categoryExpense.Clear();
            if (logs != null)
            {
                transactionLog.AddRange(logs);
            }

            if (categoryTotals != null && categoryTotals.Count > 0)
            {
                foreach (FinanceCategoryTotalSave row in categoryTotals)
                {
                    if (row == null || string.IsNullOrEmpty(row.category)) continue;
                    if (row.income > 0) categoryIncome[row.category] = row.income;
                    if (row.expense > 0) categoryExpense[row.category] = row.expense;
                }
            }
            else
            {
                RebuildCategoryTotalsFromLog();
            }

            OnFinanceUpdated?.Invoke();
        }

        public List<FinanceCategoryTotalSave> ExportCategoryTotals()
        {
            Dictionary<string, FinanceCategoryTotalSave> map = new Dictionary<string, FinanceCategoryTotalSave>();
            foreach (var kvp in categoryIncome)
            {
                map[kvp.Key] = new FinanceCategoryTotalSave { category = kvp.Key, income = kvp.Value, expense = 0 };
            }
            foreach (var kvp in categoryExpense)
            {
                if (!map.TryGetValue(kvp.Key, out FinanceCategoryTotalSave row))
                {
                    row = new FinanceCategoryTotalSave { category = kvp.Key };
                    map[kvp.Key] = row;
                }
                row.expense = kvp.Value;
            }
            return new List<FinanceCategoryTotalSave>(map.Values);
        }

        public List<FinanceCategoryTotalSave> GetSortedCategoryTotals()
        {
            List<FinanceCategoryTotalSave> list = ExportCategoryTotals();
            list.Sort((a, b) => (b.income + b.expense).CompareTo(a.income + a.expense));
            return list;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            BindTimeEvents();
        }

        private void OnEnable()
        {
            BindTimeEvents();
        }

        private void BindTimeEvents()
        {
            if (TimeManager.Instance == null) return;
            TimeManager.Instance.OnMidnightRollover -= HandleMidnightRollover;
            TimeManager.Instance.OnMidnightRollover += HandleMidnightRollover;
            TimeManager.Instance.OnNewDayStarted -= HandleNewDayStarted;
            TimeManager.Instance.OnNewDayStarted += HandleNewDayStarted;
        }

        private void RecordTransactionInternal(string category, string description, int amount, bool isIncome)
        {
            string timeStr = TimeManager.Instance != null ? TimeManager.Instance.GetFormattedTime() : "06:00";
            string defaultDate = LocalizationManager.L("Date_DefaultInit", "İLKBAHAR • GÜN 1", "SPRING • DAY 1");
            string dateStr = TimeManager.Instance != null ? TimeManager.Instance.GetFormattedDate() : defaultDate;

            string fullTimeStamp = $"{dateStr} {timeStr}";
            string recId = "TRX" + nextRecordSerial.ToString("D6");
            nextRecordSerial++;

            TransactionRecord record = new TransactionRecord(recId, fullTimeStamp, category, description, amount, isIncome);
            transactionLog.Insert(0, record);
            TrimTransactionLog();

            if (isIncome)
            {
                totalRevenue += amount;
                dailyRevenue += amount;
                monthlyRevenue += amount;
                AddCategoryAmount(categoryIncome, category, amount);
            }
            else
            {
                totalExpenses += amount;
                dailyExpenses += amount;
                monthlyExpenses += amount;
                AddCategoryAmount(categoryExpense, category, amount);
            }
        }

        private static void AddCategoryAmount(Dictionary<string, int> map, string category, int amount)
        {
            if (string.IsNullOrEmpty(category) || amount <= 0) return;
            if (!map.ContainsKey(category)) map[category] = 0;
            map[category] += amount;
        }

        private void TrimTransactionLog()
        {
            while (transactionLog.Count > MaxTransactionLog)
            {
                int removeIndex = -1;
                for (int i = transactionLog.Count - 1; i >= 0; i--)
                {
                    if (!FinanceCategories.IsProtected(transactionLog[i].category))
                    {
                        removeIndex = i;
                        break;
                    }
                }
                if (removeIndex < 0) removeIndex = transactionLog.Count - 1;
                transactionLog.RemoveAt(removeIndex);
            }
        }

        private void RebuildCategoryTotalsFromLog()
        {
            categoryIncome.Clear();
            categoryExpense.Clear();
            for (int i = 0; i < transactionLog.Count; i++)
            {
                TransactionRecord rec = transactionLog[i];
                if (rec == null || rec.amount <= 0) continue;
                if (rec.isIncome) AddCategoryAmount(categoryIncome, rec.category, rec.amount);
                else AddCategoryAmount(categoryExpense, rec.category, rec.amount);
            }
        }

        public void RecordIncome(string category, string description, int amount)
        {
            if (amount <= 0) return;
            RecordTransactionInternal(string.IsNullOrEmpty(category) ? FinanceCategories.Sales : category, description, amount, true);
            OnFinanceUpdated?.Invoke();
        }

        public void RecordExpense(string category, string description, int amount)
        {
            if (amount <= 0) return;
            RecordTransactionInternal(string.IsNullOrEmpty(category) ? FinanceCategories.Farm : category, description, amount, false);
            OnFinanceUpdated?.Invoke();
        }

        public bool SpendMoney(float amount, string description)
        {
            return SpendMoney(amount, description, FinanceCategories.Farm);
        }

        public bool SpendMoney(float amount, string description, string category)
        {
            int intAmount = Mathf.RoundToInt(amount);
            if (EconomyManager.Instance != null)
            {
                if (!EconomyManager.Instance.SpendCredits(intAmount)) return false;
            }
            RecordExpense(string.IsNullOrEmpty(category) ? FinanceCategories.Farm : category, description, intAmount);
            return true;
        }

        private void HandleMidnightRollover()
        {
            OnFinanceUpdated?.Invoke();
        }

        private void HandleNewDayStarted(TimeManager.Season season, int day, int year)
        {
            if (day != 1) return;
            monthlyRevenue = 0;
            monthlyExpenses = 0;
            OnFinanceUpdated?.Invoke();
        }

        public void ResetDailyStats()
        {
            dailyRevenue = 0;
            dailyExpenses = 0;
            OnFinanceUpdated?.Invoke();
        }

        public void ResetToDefaults()
        {
            totalRevenue = 0;
            totalExpenses = 0;
            dailyRevenue = 0;
            dailyExpenses = 0;
            monthlyRevenue = 0;
            monthlyExpenses = 0;
            transactionLog.Clear();
            categoryIncome.Clear();
            categoryExpense.Clear();
            OnFinanceUpdated?.Invoke();
        }

        public int CurrentBalance => EconomyManager.Instance != null ? EconomyManager.Instance.Credits : 50000;
        public int NetProfit => totalRevenue - totalExpenses;
        public int DailyNetProfit => dailyRevenue - dailyExpenses;
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

        public List<TransactionRecord> GetTransactionsForDate(string dateStamp)
        {
            List<TransactionRecord> result = new List<TransactionRecord>();
            if (string.IsNullOrEmpty(dateStamp)) return result;
            for (int i = 0; i < transactionLog.Count; i++)
            {
                TransactionRecord rec = transactionLog[i];
                if (rec == null || string.IsNullOrEmpty(rec.timeStamp)) continue;
                if (rec.timeStamp.IndexOf(dateStamp, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(rec);
                }
            }
            return result;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMidnightRollover -= HandleMidnightRollover;
                TimeManager.Instance.OnNewDayStarted -= HandleNewDayStarted;
            }
        }
    }
}
