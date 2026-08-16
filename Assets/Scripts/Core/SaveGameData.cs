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
        public string plotId;
        public string seedId;
        public float growthProgress;
        public bool isWatered;
        public bool isReadyToHarvest;
    }

    [Serializable]
    public class BarnCropSaveData
    {
        public string seedId;
        public int count;
    }

    /// <summary>
    /// Oyuncunun tüm oyun durumunu (bakiye, seviye, zaman, dükkan durumu, personeller, 
    /// ahır stokları ve yerleştirilen mobilyalar) JSON formatında saklayan kayıt verisi.
    /// </summary>
    [Serializable]
    public class SaveGameData
    {
        public int slotIndex;
        public string saveTimestamp;       // ör. 15.08.2026 - 19:45
        public bool isEmptySlot = true;

        // Oyun Durumu Özeti
        public string playerName = "Çiftçi Ali";
        public string companyName = "Farm2Shelf Market";
        public int playerMoney;             // Bakiyeniz (ör. 400,000 TL)
        public int storeLevel;              // Mağaza Seviyesi (1, 2, 3)
        public bool isStoreOpen;            // Dükkan Açık/Kapalı 🟢/🔴
        public int gameDay;                 // Günü (ör. Gün 4)
        public int gameHour;                // Saati (ör. 14)
        public int gameMinute;              // Dakikası (ör. 30)

        // Detaylı İstatistikler
        public int activeStaffCount;        // Çalışan Personel Sayısı
        public int barnCropKg;              // Ahırdaki Mahsul Miktarı (KG)

        // Detaylı Oyun Verileri
        public List<StaffSaveData> staffList = new List<StaffSaveData>();
        public List<BarnCropSaveData> barnCrops = new List<BarnCropSaveData>();
        public List<ShelfSaveData> furnitureList = new List<ShelfSaveData>();
        public List<CropSaveData> fieldCrops = new List<CropSaveData>();
    }
}
