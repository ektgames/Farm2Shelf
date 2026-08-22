using System;
using System.Collections.Generic;

namespace Farm2Shelf.Core
{
    [Serializable]
    public class StaffSaveData
    {
        public string id;
        public string name;
        public string role;
        public bool isFemale;
        public int dailySalary;
        public string shiftHours;
        public bool isActive;
        public bool isCalledEarly;
    }

    [Serializable]
    public class ShelfSaveRowData
    {
        public int rowId;
        public string productId;
        public string productName;
        public string iconEmoji;
        public float unitPrice;
        public int currentStock;
        public int maxCapacity;
    }

    [Serializable]
    public class ShelfSaveData
    {
        public string furnitureId;
        public string furnitureType;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ;
        public List<ShelfSaveRowData> rows = new List<ShelfSaveRowData>();
    }

    [Serializable]
    public class CropSaveData
    {
        public string plotName;
        public string seedId;
        public int currentGrowthDay;
        public int totalGrowthDays;
        public bool needsWater;
        public bool wateredToday;
        public string state;
    }

    [Serializable]
    public class BarnCropSaveData
    {
        public string seedId;
        public int count;
    }

    [Serializable]
    public class StockSaveItem
    {
        public string tickerSymbol;
        public float currentPrice;
        public float previousPrice;
        public int ownedShares;
        public float averageBuyPrice;
        public float totalInvested;
        public List<float> priceHistory = new List<float>();
    }

    [Serializable]
    public class OwnedSeedSaveData
    {
        public string seedId;
        public int count;
    }

    /// <summary>
    /// Oyuncunun TÜM OYUN DURUMUNU (Borsa Hisseleri & Portföy, Finans Kayıtları & İşlem Dökümü,
    /// Banka Kredileri, Sosyal Medya & Takipçiler, Bakiye, Mağaza Seviyesi, Saat, Gün, Mevsim, Yıl,
    /// Dükkan Kalite Puanı, Duvar/Zemin Renkleri, Mağaza ve Çiftlik Personelleri, Ahır Deposu & Tohumlar,
    /// Tarladaki Ekinler, Paletteki Teslimat Paketleri ve Tüm Mobilya/Raflar)
    /// JSON formatında %100 eksiksiz ve firesiz saklayan kayıt verisi.
    /// </summary>
    [Serializable]
    public class SaveGameData
    {
        public int slotIndex;
        public string saveTimestamp;       // ör. 18.08.2026 - 15:30
        public bool isEmptySlot = true;

        // Oyun Durumu Özeti
        public string playerName = "Çiftçi Ali";
        public string companyName = "Farm2Shelf Market";
        public int playerMoney;             // Bakiye (ör. 400,000 TL)
        public int storeLevel;              // Mağaza Seviyesi (1, 2, 3)
        public bool isStoreOpen;            // Dükkan Açık/Kapalı 🟢/🔴
        public int gameDay;                 // Günü (ör. Gün 4)
        public int gameHour;                // Saati (ör. 14)
        public int gameMinute;              // Dakikası (ör. 30)
        public string gameSeason = "İlkbahar"; // Mevsim
        public int gameYear = 1;            // Yıl
        public bool isTimePaused = true;    // Zaman Duraklatıldı mı?
        public bool isDayActive = false;    // Gün Aktif mi?
        public bool isWaitingForEvacuation = false; // Gece 24:00 tahliyesi bekleniyor mu?

        // Kalite & Dekorasyon (Tadilat)
        public int storeQualityScore;       // Yıldız Kalite Puanı
        public int storeQualityLevel;       // Kalite Seviyesi
        public float wallColorR = 0.12f, wallColorG = 0.14f, wallColorB = 0.17f, wallColorA = 1.0f; // Duvar Boyası
        public float floorColorR = 0.85f, floorColorG = 0.72f, floorColorB = 0.53f, floorColorA = 1.0f; // Zemin

        // Borsa & Yatırımlar
        public List<StockSaveItem> stockMarket = new List<StockSaveItem>();

        // Finans Dökümü & İstatistikler
        public int totalRevenue;
        public int totalExpenses;
        public int dailyRevenue;
        public int dailyExpenses;
        public int monthlyRevenue;
        public int monthlyExpenses;
        public List<TransactionRecord> transactionLog = new List<TransactionRecord>();

        // Banka Kredileri
        public List<ActiveLoanData> bankLoans = new List<ActiveLoanData>();

        // Sosyal Medya
        public int socialFollowerCount = 1420;
        public List<SocialTweetData> socialFeed = new List<SocialTweetData>();

        // Tohum Envanteri & Ahır Deposu
        public int barnUpgradeLevel = 1;
        public int barnCropKg;
        public List<OwnedSeedSaveData> ownedSeeds = new List<OwnedSeedSaveData>();
        public List<BarnCropSaveData> barnCrops = new List<BarnCropSaveData>();

        // Palette Bekleyen Teslimat Kolileri
        public List<string> pendingDeliveryBoxes = new List<string>();

        // Personel Kadrosu (Mağaza & Çiftlik)
        public int activeStaffCount;
        public List<StaffSaveData> staffList = new List<StaffSaveData>();
        public List<StaffSaveData> farmStaffList = new List<StaffSaveData>();

        // Tarladaki Ekinler
        public List<CropSaveData> fieldCrops = new List<CropSaveData>();

        // Yerleştirilen Mobilyalar ve Raf Stokları
        public List<ShelfSaveData> furnitureList = new List<ShelfSaveData>();

        // Eğitim & Başlangıç Görevleri
        public string tutorialStep = "None";
    }
}
