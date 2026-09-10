using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    public class GardenSeedInventoryManager : MonoBehaviour
    {
        private static GardenSeedInventoryManager instance;
        public static GardenSeedInventoryManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityEngine.Object.FindFirstObjectByType<GardenSeedInventoryManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GardenSeedInventoryManager");
                        instance = go.AddComponent<GardenSeedInventoryManager>();
                    }
                }
                return instance;
            }
        }

        // Tohum Envanteri (seedId -> sahip olunan tohum adedi)
        private Dictionary<string, int> ownedSeeds = new Dictionary<string, int>();

        // Ahır Ürün Envanteri (seedId -> biçilen mahsul adedi)
        private Dictionary<string, int> barnCropInventory = new Dictionary<string, int>();
        private Dictionary<string, List<ProductLot>> barnCropLots = new Dictionary<string, List<ProductLot>>();

        // Ahır Geliştirme Seviyesi (1: 500 KG, 2: 1500 KG, 3: 4000 KG)
        public int BarnUpgradeLevel { get; private set; } = 1;

        public Dictionary<string, int> GetOwnedSeedsInventory() => new Dictionary<string, int>(ownedSeeds);

        public void SetBarnUpgradeLevel(int level)
        {
            BarnUpgradeLevel = Mathf.Clamp(level, 1, 3);
            OnInventoryUpdated?.Invoke();
        }

        public void RestoreOwnedSeeds(Dictionary<string, int> seeds)
        {
            ownedSeeds.Clear();
            if (seeds != null)
            {
                foreach (var kvp in seeds)
                {
                    ownedSeeds[kvp.Key] = kvp.Value;
                }
            }
            OnInventoryUpdated?.Invoke();
        }

        public event Action OnInventoryUpdated;

        public int MaxBarnCapacity
        {
            get
            {
                switch (BarnUpgradeLevel)
                {
                    case 2: return 2500;
                    case 3: return 5000;
                    default: return 1000;
                }
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitDefaultSeeds();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitDefaultSeeds()
        {
            // Ahır varsayılan olarak tamamen boş başlar (0 KG).
            // Yalnızca oyuncunun tarlalarına ektiği ve hasat ettiği mahsuller burada birikir.
            barnCropInventory.Clear();
            barnCropLots.Clear();
        }

        public void AddSeeds(string seedId, int count)
        {
            if (string.IsNullOrEmpty(seedId) || count <= 0) return;
            if (!ownedSeeds.ContainsKey(seedId)) ownedSeeds[seedId] = 0;
            ownedSeeds[seedId] += count;
            OnInventoryUpdated?.Invoke();
        }

        public bool HasSeed(string seedId, int count = 1)
        {
            return ownedSeeds.ContainsKey(seedId) && ownedSeeds[seedId] >= count;
        }

        public bool ConsumeSeed(string seedId, int count = 1)
        {
            if (HasSeed(seedId, count))
            {
                ownedSeeds[seedId] -= count;
                if (ownedSeeds[seedId] <= 0) ownedSeeds.Remove(seedId);
                OnInventoryUpdated?.Invoke();
                return true;
            }
            return false;
        }

        public int GetSeedCount(string seedId)
        {
            return ownedSeeds.ContainsKey(seedId) ? ownedSeeds[seedId] : 0;
        }

        public Dictionary<string, int> GetAllOwnedSeeds()
        {
            return new Dictionary<string, int>(ownedSeeds);
        }

        // --- AHIR ENVANTERİ YÖNETİMİ ---
        public int GetTotalBarnStoredAmount()
        {
            int total = 0;
            foreach (var kvp in barnCropInventory) total += kvp.Value;
            return total;
        }

        public int GetBarnFreeSpace()
        {
            return Mathf.Max(0, MaxBarnCapacity - GetTotalBarnStoredAmount());
        }

        public bool CanAddToBarn(int amount)
        {
            return amount > 0 && GetBarnFreeSpace() >= amount;
        }

        public bool AddBarnCrop(string seedId, int amount)
        {
            return TryAddCropToBarn(seedId, amount);
        }

        public bool TryAddCropToBarn(string seedId, int amount)
        {
            return TryAddCropToBarn(seedId, amount, null);
        }

        public bool TryAddCropToBarn(string seedId, int amount, ProductLot lot)
        {
            if (string.IsNullOrEmpty(seedId) || amount <= 0) return false;
            if (!CanAddToBarn(amount)) return false;

            if (!barnCropInventory.ContainsKey(seedId)) barnCropInventory[seedId] = 0;
            barnCropInventory[seedId] += amount;
            EnsureBarnLotList(seedId);
            if (lot != null)
            {
                ProductLot copy = lot.Clone();
                copy.productId = seedId;
                copy.quantity = amount;
                ProductPassportService.MergeAdd(barnCropLots[seedId], copy);
            }
            else
            {
                ProductPassportService.MergeAdd(barnCropLots[seedId], ProductPassportService.CreateLegacyLot(seedId, amount));
            }
            SyncBarnCounts(seedId);
            OnInventoryUpdated?.Invoke();
            return true;
        }

        public void RestoreBarnCrops(Dictionary<string, int> crops)
        {
            RestoreBarnCrops(crops, null);
        }

        public void RestoreBarnCrops(Dictionary<string, int> crops, List<BarnCropSaveData> savedRows)
        {
            barnCropInventory.Clear();
            barnCropLots.Clear();
            if (crops != null)
            {
                foreach (var kvp in crops)
                {
                    if (string.IsNullOrEmpty(kvp.Key) || kvp.Value <= 0) continue;
                    barnCropInventory[kvp.Key] = kvp.Value;
                    List<ProductLot> savedLots = null;
                    if (savedRows != null)
                    {
                        BarnCropSaveData row = savedRows.Find(c => c != null && c.seedId == kvp.Key);
                        if (row != null) savedLots = row.lots;
                    }
                    barnCropLots[kvp.Key] = ProductPassportService.RestoreLotsOrLegacy(kvp.Key, kvp.Value, savedLots);
                }
            }
            OnInventoryUpdated?.Invoke();
        }

        public List<BarnCropSaveData> ExportBarnCropsForSave()
        {
            List<BarnCropSaveData> rows = new List<BarnCropSaveData>();
            foreach (var kvp in barnCropInventory)
            {
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Value <= 0) continue;
                EnsureBarnLotList(kvp.Key);
                rows.Add(new BarnCropSaveData
                {
                    seedId = kvp.Key,
                    count = kvp.Value,
                    lots = ProductPassportService.CloneLots(barnCropLots[kvp.Key])
                });
            }
            return rows;
        }

        public string GetBarnPassportSummary(string seedId)
        {
            if (string.IsNullOrEmpty(seedId) || !barnCropLots.ContainsKey(seedId)) return "";
            return ProductPassportService.GetCardText(ProductPassportService.ResolveProductDisplayName(seedId), barnCropLots[seedId]);
        }

        private void EnsureBarnLotList(string seedId)
        {
            if (!barnCropLots.ContainsKey(seedId) || barnCropLots[seedId] == null)
            {
                barnCropLots[seedId] = new List<ProductLot>();
            }
        }

        private void SyncBarnCounts(string seedId)
        {
            if (string.IsNullOrEmpty(seedId)) return;
            EnsureBarnLotList(seedId);
            int lotSum = ProductPassportService.SumLots(barnCropLots[seedId]);
            int count = barnCropInventory.ContainsKey(seedId) ? barnCropInventory[seedId] : 0;
            if (count <= 0)
            {
                barnCropInventory.Remove(seedId);
                barnCropLots.Remove(seedId);
                return;
            }

            if (count > lotSum)
            {
                ProductPassportService.MergeAdd(barnCropLots[seedId], ProductPassportService.CreateLegacyLot(seedId, count - lotSum));
            }
            else if (count < lotSum)
            {
                ProductPassportService.TakeFifo(barnCropLots[seedId], lotSum - count);
            }

            lotSum = ProductPassportService.SumLots(barnCropLots[seedId]);
            if (lotSum < count)
            {
                ProductPassportService.MergeAdd(barnCropLots[seedId], ProductPassportService.CreateLegacyLot(seedId, count - lotSum));
                lotSum = ProductPassportService.SumLots(barnCropLots[seedId]);
            }

            barnCropInventory[seedId] = Mathf.Max(count, lotSum);
        }

        public static void ShowBarnFullModal()
        {
            ModalManager.ShowModal(
                LocalizationManager.L("Modal_BarnFullTitle", "Ahır Dolu! ⚠️", "Barn Full! ⚠️"),
                LocalizationManager.L("Modal_BarnFullBody", "Ahır kapasitesi doldu. Ürünler ziyan edilmedi.\n\nAhırdaki mahsul veya atölye ürünlerini dükkana sevk edin, satın veya ahırı geliştirin; sonra tekrar deneyin.", "Barn capacity is full. Nothing was wasted.\n\nShip, sell, or upgrade barn storage first, then try again."),
                LocalizationManager.L("Btn_Ok", "Tamam", "OK"));
        }

        public int GetBarnCropCount(string seedId)
        {
            return barnCropInventory.ContainsKey(seedId) ? barnCropInventory[seedId] : 0;
        }

        public Dictionary<string, int> GetBarnCropInventory()
        {
            return new Dictionary<string, int>(barnCropInventory);
        }

        public void ClearBarnInventory()
        {
            barnCropInventory.Clear();
            barnCropLots.Clear();
            OnInventoryUpdated?.Invoke();
        }

        public bool ConsumeBarnCrop(string seedId, int amount)
        {
            return ConsumeBarnCrop(seedId, amount, out _);
        }

        public bool ConsumeBarnCrop(string seedId, int amount, out List<ProductLot> consumedLots)
        {
            consumedLots = new List<ProductLot>();
            if (string.IsNullOrEmpty(seedId) || amount <= 0) return false;
            if (!barnCropInventory.ContainsKey(seedId) || barnCropInventory[seedId] < amount) return false;

            EnsureBarnLotList(seedId);
            SyncBarnCounts(seedId);
            if (!barnCropInventory.ContainsKey(seedId) || barnCropInventory[seedId] < amount) return false;

            consumedLots = ProductPassportService.TakeFifo(barnCropLots[seedId], amount);
            int taken = ProductPassportService.SumLots(consumedLots);
            if (taken < amount)
            {
                ProductPassportService.MergeAdd(consumedLots, ProductPassportService.CreateLegacyLot(seedId, amount - taken));
            }

            barnCropInventory[seedId] -= amount;
            if (barnCropInventory[seedId] <= 0)
            {
                barnCropInventory.Remove(seedId);
                barnCropLots.Remove(seedId);
            }
            OnInventoryUpdated?.Invoke();
            return true;
        }

        public bool UpgradeBarn()
        {
            if (BarnUpgradeLevel < 3)
            {
                BarnUpgradeLevel++;
                OnInventoryUpdated?.Invoke();
                return true;
            }
            return false;
        }
    }
}
