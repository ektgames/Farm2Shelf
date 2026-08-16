using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    [Serializable]
    public class StockData
    {
        public string tickerSymbol;       // AGRO, GRLG, F2SH, SLAR, BFOD
        public string companyName;        // AGRO-TÜRK TARIM A.Ş.
        public string companyNameEn;
        public string category;           // Tohum & Çiftlik Gübre Sanayii
        public string categoryEn;
        public float currentPrice;        // Anlık Hisse Fiyatı (Coin C)
        public float previousPrice;       // Bir önceki saatlik fiyat
        public float basePrice;           // Başlangıç Baz Fiyatı
        public float volatility;          // Volatite Oranı
        public List<float> priceHistory = new List<float>(); // 24 Saatlik Fiyat Geçmişi

        // Oyuncunun Portföy Durumu
        public int ownedShares;           // Sahip Olunan Hisse Adedi
        public float averageBuyPrice;     // Ortalama Alış Fiyatı (Coin C)
        public float totalInvested;       // Toplam Yatırılan Tutar (Coin C)

        public string LocalizedCompanyName => LocalizationManager.L("Stock_Name_" + tickerSymbol, companyName, !string.IsNullOrEmpty(companyNameEn) ? companyNameEn : companyName);
        public string LocalizedCategory => LocalizationManager.L("Stock_Cat_" + tickerSymbol, category, !string.IsNullOrEmpty(categoryEn) ? categoryEn : category);

        public StockData(string tickerSymbol, string companyName, string companyNameEn, string category, string categoryEn, float basePrice, float volatility)
        {
            this.tickerSymbol = tickerSymbol;
            this.companyName = companyName;
            this.companyNameEn = companyNameEn;
            this.category = category;
            this.categoryEn = categoryEn;
            this.basePrice = basePrice;
            this.currentPrice = basePrice;
            this.previousPrice = basePrice;
            this.volatility = volatility;

            this.ownedShares = 0;
            this.averageBuyPrice = 0f;
            this.totalInvested = 0f;

            // İlk 24 saatlik fiyat geçmişini hafif rastgele eğri ile doldur
            float p = basePrice;
            for (int h = 0; h < 24; h++)
            {
                float var = (UnityEngine.Random.value - 0.48f) * volatility * p;
                p = Mathf.Max(5f, p + var);
                priceHistory.Add(Mathf.Round(p * 100f) / 100f);
            }
            this.currentPrice = priceHistory[priceHistory.Count - 1];
            this.previousPrice = priceHistory[priceHistory.Count - 2];
        }

        public float PriceChangePercent
        {
            get
            {
                if (previousPrice <= 0) return 0f;
                return ((currentPrice - previousPrice) / previousPrice) * 100f;
            }
        }

        public float TotalCurrentValue => ownedShares * currentPrice;
        public float ProfitLoss => TotalCurrentValue - totalInvested;
        public float ProfitLossPercent
        {
            get
            {
                if (totalInvested <= 0) return 0f;
                return (ProfitLoss / totalInvested) * 100f;
            }
        }
    }

    /// <summary>
    /// Farm2Shelf Canlı Borsa & Hisseler Yöneticisi (StockMarketManager).
    /// 5 telifsiz şirketin hisse fiyatlarını oyun saatine göre saat başlarında günceller,
    /// 24 saatlik fiyat eğrisini saklar ve hisse alım/satım portföyünü yönetir.
    /// </summary>
    public class StockMarketManager : MonoBehaviour
    {
        public static StockMarketManager Instance { get; private set; }

        private List<StockData> stocks = new List<StockData>();
        private Dictionary<string, StockData> stockMap = new Dictionary<string, StockData>();

        public event Action OnStockMarketUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeStockMarket();
            }
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnHourPassed += HandleHourPassed;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnHourPassed -= HandleHourPassed;
            }
        }

        private void InitializeStockMarket()
        {
            stocks.Clear();
            stockMap.Clear();

            // 5 Adet Telifsiz Şirket Tanımı
            AddStock(new StockData("AGRO", "AGRO-TÜRK TARIM A.Ş.", "AGRO-TURK FARMING INC.", "Tohum & Çiftlik Gübre Sanayii", "Seed & Farm Fertilizer Industry", 120.00f, 0.05f));
            AddStock(new StockData("GRLG", "GREEN-LOG LOJİSTİK A.Ş.", "GREEN-LOG LOGISTICS INC.", "Kamyon & Filo Taşımacılığı", "Truck & Fleet Transportation", 85.50f, 0.06f));
            AddStock(new StockData("F2SH", "FARM2SHELF RETAIL HOLDİNG", "FARM2SHELF RETAIL HOLDING", "Perakende Süpermarket Zinciri", "Retail Supermarket Chain", 210.00f, 0.04f));
            AddStock(new StockData("SLAR", "SOLAR-POWER ENERJİ A.Ş.", "SOLAR-POWER ENERGY INC.", "Yenilenebilir Güneş Enerjisi", "Renewable Solar Energy", 340.00f, 0.07f));
            AddStock(new StockData("BFOD", "BIO-FOOD GIDA A.Ş.", "BIO-FOOD FOOD INC.", "Organik Konserve & İçecek Gıda", "Organic Canned & Beverage Foods", 65.00f, 0.05f));
        }

        private void AddStock(StockData data)
        {
            stocks.Add(data);
            stockMap[data.tickerSymbol] = data;
        }

        private void HandleHourPassed()
        {
            // Oyun Saatinde Her Saat Başı Fiyatlar Dalgalanır
            foreach (var stock in stocks)
            {
                stock.previousPrice = stock.currentPrice;

                // % -6.5 ile % +8.5 arası rastgele volatilite dalgalanması
                float changeFactor = UnityEngine.Random.Range(-0.065f, 0.085f);
                float newPrice = stock.currentPrice * (1f + changeFactor);
                newPrice = Mathf.Max(5.0f, Mathf.Round(newPrice * 100f) / 100f);

                stock.currentPrice = newPrice;
                stock.priceHistory.Add(newPrice);

                // Son 24 veriyi muhafaza et
                if (stock.priceHistory.Count > 24)
                {
                    stock.priceHistory.RemoveAt(0);
                }
            }

            OnStockMarketUpdated?.Invoke();
            Debug.Log("[StockMarketManager] Saatlik Borsa Fiyatları Güncellendi!");
        }

        // --- HİSSE ALIM SATIM İŞLEMLERİ ---

        public bool BuyShares(string symbol, int shareAmount)
        {
            if (shareAmount <= 0) return false;
            if (!stockMap.TryGetValue(symbol, out StockData stock)) return false;

            int totalCost = Mathf.RoundToInt(stock.currentPrice * shareAmount);

            if (EconomyManager.Instance != null)
            {
                if (!EconomyManager.Instance.SpendCredits(totalCost))
                {
                    return false; // Yetersiz Bakiye
                }
            }

            // Portföy Güncellemesi
            float previousTotalValue = stock.ownedShares * stock.averageBuyPrice;
            stock.ownedShares += shareAmount;
            stock.totalInvested += totalCost;
            stock.averageBuyPrice = (previousTotalValue + totalCost) / stock.ownedShares;

            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordExpense("Borsa Yatırımı", $"Hisse Alındı ({shareAmount} Adet {stock.tickerSymbol} @ {stock.currentPrice:F2}C)", totalCost);
            }

            OnStockMarketUpdated?.Invoke();
            Debug.Log($"[StockMarketManager] Hisse Alındı: {shareAmount} Adet {symbol} (-{totalCost:N0}C)");
            return true;
        }

        public bool SellShares(string symbol, int shareAmount)
        {
            if (shareAmount <= 0) return false;
            if (!stockMap.TryGetValue(symbol, out StockData stock)) return false;
            if (stock.ownedShares < shareAmount) return false; // Yetersiz Hisse

            int totalRevenue = Mathf.RoundToInt(stock.currentPrice * shareAmount);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCredits(totalRevenue);
            }

            stock.ownedShares -= shareAmount;
            if (stock.ownedShares <= 0)
            {
                stock.ownedShares = 0;
                stock.averageBuyPrice = 0f;
                stock.totalInvested = 0f;
            }
            else
            {
                stock.totalInvested = stock.ownedShares * stock.averageBuyPrice;
            }

            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordIncome("Borsa Geliri", $"Hisse Satıldı ({shareAmount} Adet {stock.tickerSymbol} @ {stock.currentPrice:F2}C)", totalRevenue);
            }

            OnStockMarketUpdated?.Invoke();
            Debug.Log($"[StockMarketManager] Hisse Satıldı: {shareAmount} Adet {symbol} (+{totalRevenue:N0}C)");
            return true;
        }

        public List<StockData> GetAllStocks() => stocks;
        public StockData GetStock(string symbol)
        {
            if (stockMap.TryGetValue(symbol, out StockData data)) return data;
            return null;
        }
    }
}
