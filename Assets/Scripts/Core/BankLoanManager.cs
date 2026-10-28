using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    [Serializable]
    public class BankLoanOffer
    {
        public string offerId;
        public string title;
        public string description;
        public int principalAmount;      // Ana para (çekilen tutar)
        public int dailyInstallment;     // Günlük taksit tutarı
        public int totalRepayment;       // Toplam geri ödeme tutarı
        public int termDays;             // Vade (varsayılan 10 gün)
        public float interestRatePercent;// Faiz oranı (%)
        public int minStoreLevel;        // Gereken minimum dükkan seviyesi

        public string LocalizedTitle => LocalizationManager.L("Loan_Title_" + offerId, title, GetEnglishTitle(offerId));
        public string LocalizedDescription => LocalizationManager.L("Loan_Desc_" + offerId, description, GetEnglishDescription(offerId));

        public static string GetEnglishTitle(string id)
        {
            switch (id)
            {
                case "LV1_L1": return "Micro Business Loan";
                case "LV1_L2": return "Small Business Loan";
                case "LV1_L3": return "Fast Growth Loan";
                case "LV2_L1": return "Supermarket SME Loan";
                case "LV2_L2": return "Fleet & Warehouse Investment Loan";
                case "LV2_L3": return "Commercial Supply Loan";
                case "LV3_L1": return "Hypermarket Mega Investment Loan";
                case "LV3_L2": return "Farm Automation Loan";
                case "LV3_L3": return "Corporate Holding Loan";
                default: return "Bank Loan";
            }
        }

        public static string GetEnglishDescription(string id)
        {
            switch (id)
            {
                case "LV1_L1": return "Quick cash support for small businesses (10% Interest)";
                case "LV1_L2": return "Medium scale loan for shelf & stock purchases (15% Interest)";
                case "LV1_L3": return "Financing support before store expansion (20% Interest)";
                case "LV2_L1": return "Low-interest commercial growth loan (10% Interest)";
                case "LV2_L2": return "Logistics & staff expansion loan (15% Interest)";
                case "LV2_L3": return "Bulk wholesale shopping financing (20% Interest)";
                case "LV3_L1": return "Corporate retail chain development loan (10% Interest)";
                case "LV3_L2": return "Agriculture & farm machinery investment (15% Interest)";
                case "LV3_L3": return "Maximum financing support (20% Interest)";
                default: return "Bank loan offer";
            }
        }

        public BankLoanOffer(string offerId, string title, string description, int principalAmount, int dailyInstallment, int totalRepayment, int termDays, float interestRatePercent, int minStoreLevel)
        {
            this.offerId = offerId;
            this.title = title;
            this.description = description;
            this.principalAmount = principalAmount;
            this.dailyInstallment = dailyInstallment;
            this.totalRepayment = totalRepayment;
            this.termDays = termDays;
            this.interestRatePercent = interestRatePercent;
            this.minStoreLevel = minStoreLevel;
        }
    }

    [Serializable]
    public class ActiveLoanData
    {
        public string loanId;
        public string offerId; // Stable offer identity; empty in legacy saves.
        public string title;
        public int principalAmount;
        public int dailyInstallment;
        public int totalRepayment;
        public int remainingDays;
        public int paidAmount;
        public string startDateFormatted;
        public int initialTermDays;
        public int lastInstallmentDay = -1;

        public ActiveLoanData(string loanId, string title, int principalAmount, int dailyInstallment, int totalRepayment, int termDays, string startDateFormatted)
        {
            this.loanId = loanId;
            this.title = title;
            this.principalAmount = principalAmount;
            this.dailyInstallment = dailyInstallment;
            this.totalRepayment = totalRepayment;
            this.remainingDays = termDays;
            this.initialTermDays = termDays > 0 ? termDays : 10;
            this.paidAmount = 0;
            this.startDateFormatted = startDateFormatted;
        }

        public string LocalizedTitle => BankLoanOffer.GetEnglishTitle(loanId) != "Bank Loan" 
            ? LocalizationManager.L("Loan_Title_" + loanId, title, BankLoanOffer.GetEnglishTitle(loanId)) 
            : title;

        public int RemainingTotalRepayment => Mathf.Max(0, totalRepayment - paidAmount);

        public void NormalizePaymentState()
        {
            if (initialTermDays <= 0) initialTermDays = 10;
            if (totalRepayment <= 0) totalRepayment = Mathf.Max(principalAmount, dailyInstallment * initialTermDays);
            dailyInstallment = Mathf.Max(1, Mathf.CeilToInt((float)totalRepayment / initialTermDays));
            paidAmount = Mathf.Clamp(paidAmount, 0, totalRepayment);
            remainingDays = Mathf.CeilToInt((float)RemainingTotalRepayment / dailyInstallment);
        }

        /// <summary>
        /// Erken Kapatma Hesabı:
        /// - Gün geçmemişse (Aynı Gün): %100 FAİZSİZ Ana Para (principalAmount) ile borç kapatılır!
        /// - Gün geçmişse: Kalan Ana Para + Kalan Günlerin İndirimli Faizi (%50 Faiz İndirimi Tasarrufu!).
        /// </summary>
        public int GetEarlyPayoffAmount()
        {
            if (paidAmount <= 0)
            {
                // Gün geçmemişse (Aynı gün içinde): Tam anapara ödemesi, 0C faiz!
                return principalAmount;
            }

            if (RemainingTotalRepayment == 0) return 0;
            float remainingFraction = (float)RemainingTotalRepayment / Mathf.Max(1, totalRepayment);
            float remainingPrincipal = principalAmount * remainingFraction;
            float remainingInterest = Mathf.Max(0, totalRepayment - principalAmount) * remainingFraction;
            return Mathf.Clamp(Mathf.RoundToInt(remainingPrincipal + remainingInterest * 0.5f), 0, RemainingTotalRepayment);
        }
    }

    /// <summary>
    /// Farm2Shelf Banka Kredileri Yöneticisi (BankLoanManager).
    /// Dükkan seviyesine özel 3 farklı kredi teklifi sunar, gece yarısında 
    /// otomatik günlük taksit tahsilatlarını yapar ve erken kapatmayı yönetir.
    /// </summary>
    public class BankLoanManager : MonoBehaviour
    {
        public static BankLoanManager Instance { get; private set; }

        private List<ActiveLoanData> activeLoans = new List<ActiveLoanData>();
        public event Action OnBankLoansUpdated;

        public List<ActiveLoanData> GetActiveLoans() => activeLoans;

        public int CurrentOfferLevel => Mathf.Clamp(
            Farm2Shelf.Environment.EnvironmentBuilder.Instance != null
                ? Farm2Shelf.Environment.EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1, 1, 3);

        public void RestoreActiveLoans(List<ActiveLoanData> loans)
        {
            activeLoans.Clear();
            if (loans != null)
            {
                foreach (ActiveLoanData loan in loans)
                {
                    if (loan == null) continue;
                    loan.NormalizePaymentState();
                    // Older saves did not store the payment date. A midnight save
                    // with prior payments must not charge that same day again on resume.
                    if (loan.lastInstallmentDay <= 0 && loan.paidAmount > 0 &&
                        TimeManager.Instance != null && TimeManager.Instance.Hour >= 24)
                        loan.lastInstallmentDay = GetCurrentPaymentDay();
                    if (loan.RemainingTotalRepayment > 0) activeLoans.Add(loan);
                }
            }
            OnBankLoansUpdated?.Invoke();
        }

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

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMidnightRollover -= HandleMidnightRollover;
            }
        }

        // --- MAĞAZA SEVİYESİNE ÖZEL 3 KREDİ TEKLİFİ ---
        public List<BankLoanOffer> GetOffersForStoreLevel(int storeLevel)
        {
            List<BankLoanOffer> offers = new List<BankLoanOffer>();

            if (storeLevel <= 1)
            {
                // Seviye 1: Esnaf Kredileri (5.000C, 10.000C, 15.000C)
                offers.Add(new BankLoanOffer("LV1_L1", "Mikro Esnaf Kredisi", "Küçük işletmeler için hızlı nakit desteği (%10 Faiz)", 5000, 550, 5500, 10, 10f, 1));
                offers.Add(new BankLoanOffer("LV1_L2", "Küçük İşletme Kredisi", "Reyon ve stok alımları için orta ölçekli kredi (%15 Faiz)", 10000, 1150, 11500, 10, 15f, 1));
                offers.Add(new BankLoanOffer("LV1_L3", "Hızlı Büyüme Kredisi", "Dükkan genişletme öncesi finansman desteği (%20 Faiz)", 15000, 1800, 18000, 10, 20f, 1));
            }
            else if (storeLevel == 2)
            {
                // Seviye 2: Süpermarket Kredileri (50.000C, 75.000C, 100.000C)
                offers.Add(new BankLoanOffer("LV2_L1", "Süpermarket KOBİ Kredisi", "Düşük faizli ticari büyüme kredisi (%10 Faiz)", 50000, 5500, 55000, 10, 10f, 2));
                offers.Add(new BankLoanOffer("LV2_L2", "Filo & Depo Yatırım Kredisi", "Lojistik ve personel genişletme kredisi (%15 Faiz)", 75000, 8625, 86250, 10, 15f, 2));
                offers.Add(new BankLoanOffer("LV2_L3", "Ticari Tedarik Kredisi", "Toplu toptan alışveriş finansmanı (%20 Faiz)", 100000, 12000, 120000, 10, 20f, 2));
            }
            else
            {
                // Seviye 3+: Hipermarket Dev Krediler (150.000C, 200.000C, 250.000C)
                offers.Add(new BankLoanOffer("LV3_L1", "Hipermarket Dev Yatırım", "Kurumsal perakende zinciri geliştirme kredisi (%10 Faiz)", 150000, 16500, 165000, 10, 10f, 3));
                offers.Add(new BankLoanOffer("LV3_L2", "Çiftlik Otomasyon Kredisi", "Tarım ve bahçe makine yatırımı (%15 Faiz)", 200000, 23000, 230000, 10, 15f, 3));
                offers.Add(new BankLoanOffer("LV3_L3", "Holding Kurumsal Kredisi", "Maksimum finansman desteği (%20 Faiz)", 250000, 30000, 300000, 10, 20f, 3));
            }

            return offers;
        }

        private static int GetLoanSlot(string offerId, int principalAmount)
        {
            // Offer IDs retain the same L1/L2/L3 slot across store tiers.
            if (!string.IsNullOrEmpty(offerId))
            {
                for (int tier = 1; tier <= 3; tier++)
                    for (int slot = 1; slot <= 3; slot++)
                        if (offerId == $"LV{tier}_L{slot}") return slot;
            }
            // Legacy saves predate offerId; all original principals are unique.
            switch (principalAmount)
            {
                case 5000: case 50000: case 150000: return 1;
                case 10000: case 75000: case 200000: return 2;
                case 15000: case 100000: case 250000: return 3;
                default: return 0;
            }
        }

        public bool HasActiveLoanForOffer(BankLoanOffer offer)
        {
            if (offer == null) return false;
            int requestedSlot = GetLoanSlot(offer.offerId, offer.principalAmount);
            foreach (var loan in activeLoans)
            {
                if (loan == null || loan.RemainingTotalRepayment <= 0) continue;
                if ((requestedSlot > 0 && GetLoanSlot(loan.offerId, loan.principalAmount) == requestedSlot) ||
                    loan.offerId == offer.offerId ||
                    (string.IsNullOrEmpty(loan.offerId) && loan.principalAmount == offer.principalAmount))
                    return true;
            }
            return false;
        }

        public bool TakeLoan(BankLoanOffer offer)
        {
            if (offer == null || EconomyManager.Instance == null) return false;
            // Revalidate against the current tier, even if an old UI callback survives an upgrade.
            string requestedId = offer.offerId;
            offer = GetOffersForStoreLevel(CurrentOfferLevel).Find(candidate => candidate.offerId == requestedId);
            if (offer == null || HasActiveLoanForOffer(offer)) return false;

            string dateStr = TimeManager.Instance != null ? TimeManager.Instance.GetFormattedDate() : "GÜN 1";
            string loanId = "LOAN_" + UnityEngine.Random.Range(10000, 99999);

            ActiveLoanData activeLoan = new ActiveLoanData(loanId, offer.title, offer.principalAmount, offer.dailyInstallment, offer.totalRepayment, offer.termDays, dateStr);
            activeLoan.offerId = offer.offerId;
            activeLoan.NormalizePaymentState();
            if (TimeManager.Instance != null && TimeManager.Instance.Hour >= 24)
                activeLoan.lastInstallmentDay = GetCurrentPaymentDay();
            activeLoans.Add(activeLoan);

            // Register first: credit/finance callbacks must see this offer as occupied.
            // Krediyi Çek ve Parayı Ekle
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCredits(offer.principalAmount);
            }

            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordIncome("Banka Kredisi", $"Banka Kredisi Çekildi ({offer.title})", offer.principalAmount);
            }



            OnBankLoansUpdated?.Invoke();
            Debug.Log($"[BankLoanManager] Kredi Çekildi: {offer.title} (+{offer.principalAmount:N0}C)");
            return true;
        }

        public bool PayoffLoanEarly(ActiveLoanData loan)
        {
            if (loan == null || !activeLoans.Contains(loan) || EconomyManager.Instance == null) return false;

            int payoffAmount = loan.GetEarlyPayoffAmount();

            if (EconomyManager.Instance != null)
            {
                if (!EconomyManager.Instance.SpendCredits(payoffAmount))
                {
                    return false; // Yetersiz bakiye
                }
            }

            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordExpense("Banka Kredisi Ödemesi", $"Kredi Erken Kapatıldı ({loan.title})", payoffAmount);
            }

            activeLoans.Remove(loan);
            OnBankLoansUpdated?.Invoke();
            Debug.Log($"[BankLoanManager] Kredi Erken Kapatıldı: {loan.title} (-{payoffAmount:N0}C)");
            return true;
        }

        private void HandleMidnightRollover()
        {
            ProcessDailyInstallments();
        }

        private static int GetCurrentPaymentDay()
        {
            var time = TimeManager.Instance;
            return time.Year * 120 + (int)time.CurrentSeason * 30 + time.Day;
        }

        public void ProcessDailyInstallments()
        {
            if (TimeManager.Instance == null || EconomyManager.Instance == null) return;
            int paymentDay = GetCurrentPaymentDay();
            // Gece Yarısı Otomatik Günlük Taksit Kesintisi
            for (int i = activeLoans.Count - 1; i >= 0; i--)
            {
                ActiveLoanData loan = activeLoans[i];
                if (loan == null) { activeLoans.RemoveAt(i); continue; }
                loan.NormalizePaymentState();
                if (loan.lastInstallmentDay == paymentDay) continue;
                int installment = Mathf.Min(loan.dailyInstallment, loan.RemainingTotalRepayment);
                if (installment <= 0) { activeLoans.RemoveAt(i); continue; }
                loan.lastInstallmentDay = paymentDay;
                loan.paidAmount += installment;
                loan.NormalizePaymentState();
                EconomyManager.Instance.ForceDeductCredits(installment);

                if (FinanceManager.Instance != null)
                {
                    string description = string.Format(LocalizationManager.L("Loan_InstallmentPaid",
                        "Günlük taksit: {0} • Ödenen: {1:N0}C • Kalan borç: {2:N0}C",
                        "Daily installment: {0} • Paid: {1:N0}C • Remaining debt: {2:N0}C"),
                        loan.LocalizedTitle, installment, loan.RemainingTotalRepayment);
                    FinanceManager.Instance.RecordExpense(FinanceCategories.BankLoanInstallment, description, installment);
                }

                if (loan.RemainingTotalRepayment <= 0)
                {
                    activeLoans.RemoveAt(i);
                    Debug.Log($"[BankLoanManager] Kredi Tamamen Borçsuz Kapatıldı: {loan.title}");
                }
            }

            OnBankLoansUpdated?.Invoke();
        }

        public int TotalActiveLoanDebt
        {
            get
            {
                int total = 0;
                foreach (var l in activeLoans) total += l.RemainingTotalRepayment;
                return total;
            }
        }
    }
}
