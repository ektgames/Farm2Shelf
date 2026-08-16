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
        public string title;
        public int principalAmount;
        public int dailyInstallment;
        public int totalRepayment;
        public int remainingDays;
        public int paidAmount;
        public string startDateFormatted;
        public int initialTermDays;

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

        public int RemainingTotalRepayment => remainingDays * dailyInstallment;

        /// <summary>
        /// Erken Kapatma Hesabı:
        /// - Gün geçmemişse (Aynı Gün): %100 FAİZSİZ Ana Para (principalAmount) ile borç kapatılır!
        /// - Gün geçmişse: Kalan Ana Para + Kalan Günlerin İndirimli Faizi (%50 Faiz İndirimi Tasarrufu!).
        /// </summary>
        public int GetEarlyPayoffAmount()
        {
            if (remainingDays >= initialTermDays)
            {
                // Gün geçmemişse (Aynı gün içinde): Tam anapara ödemesi, 0C faiz!
                return principalAmount;
            }

            int daysPassed = initialTermDays - remainingDays;
            int totalInterest = totalRepayment - principalAmount;

            // Aradan geçen günlerin anapara ve faiz payı
            int dailyPrincipal = Mathf.RoundToInt((float)principalAmount / initialTermDays);
            int remainingPrincipal = principalAmount - (dailyPrincipal * daysPassed);

            int dailyInterest = Mathf.RoundToInt((float)totalInterest / initialTermDays);
            // Gelecek günlerin faizine %50 erken kapatma indirimi
            int discountedRemainingInterest = Mathf.RoundToInt(dailyInterest * remainingDays * 0.50f);

            return Mathf.Max(principalAmount / 10, remainingPrincipal + discountedRemainingInterest);
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

        public bool TakeLoan(BankLoanOffer offer)
        {
            if (offer == null) return false;

            // Krediyi Çek ve Parayı Ekle
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCredits(offer.principalAmount);
            }

            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordIncome("Banka Kredisi", $"Banka Kredisi Çekildi ({offer.title})", offer.principalAmount);
            }

            string dateStr = TimeManager.Instance != null ? TimeManager.Instance.GetFormattedDate() : "GÜN 1";
            string loanId = "LOAN_" + UnityEngine.Random.Range(10000, 99999);

            ActiveLoanData activeLoan = new ActiveLoanData(loanId, offer.title, offer.principalAmount, offer.dailyInstallment, offer.totalRepayment, offer.termDays, dateStr);
            activeLoans.Add(activeLoan);

            OnBankLoansUpdated?.Invoke();
            Debug.Log($"[BankLoanManager] Kredi Çekildi: {offer.title} (+{offer.principalAmount:N0}C)");
            return true;
        }

        public bool PayoffLoanEarly(ActiveLoanData loan)
        {
            if (loan == null || !activeLoans.Contains(loan)) return false;

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
            // Gece Yarısı Otomatik Günlük Taksit Kesintisi
            for (int i = activeLoans.Count - 1; i >= 0; i--)
            {
                ActiveLoanData loan = activeLoans[i];
                int installment = loan.dailyInstallment;

                bool success = false;
                if (EconomyManager.Instance != null)
                {
                    if (EconomyManager.Instance.Credits >= installment)
                    {
                        EconomyManager.Instance.SpendCredits(installment);
                        success = true;
                    }
                    else
                    {
                        // Bakiyesi yetersizse bakiyeyi sıfırlar, kalan borcu kaydeder
                        int currentCreds = EconomyManager.Instance.Credits;
                        EconomyManager.Instance.SpendCredits(currentCreds);
                        success = false;
                    }
                }

                loan.paidAmount += installment;
                loan.remainingDays--;

                if (FinanceManager.Instance != null)
                {
                    string statusNote = success ? "Ödendi" : "Yetersiz Bakiye Zorunlu Tahsilat";
                    FinanceManager.Instance.RecordExpense("Banka Kredisi Taksiti", $"Günlük Taksit ({loan.title} - {statusNote})", installment);
                }

                if (loan.remainingDays <= 0)
                {
                    activeLoans.RemoveAt(i);
                    Debug.Log($"[BankLoanManager] Kredi Tamamen Borçsuz Kapatıldı: {loan.title}");
                }
            }

            OnBankLoansUpdated?.Invoke();
        }

        public List<ActiveLoanData> GetActiveLoans() => activeLoans;
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
