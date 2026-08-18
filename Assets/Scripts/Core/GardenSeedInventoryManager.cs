using System;
using System.Collections.Generic;
using UnityEngine;

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
                    case 2: return 1500;
                    case 3: return 4000;
                    default: return 500;
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
            // Oyuna sıfır tohumla başlanır. Oyuncu EKT Tablet'ten satın alır.
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

        public bool AddBarnCrop(string seedId, int amount)
        {
            return TryAddCropToBarn(seedId, amount);
        }

        public bool TryAddCropToBarn(string seedId, int amount)
        {
            if (string.IsNullOrEmpty(seedId) || amount <= 0) return false;
            int currentTotal = GetTotalBarnStoredAmount();
            int spaceLeft = MaxBarnCapacity - currentTotal;
            if (spaceLeft <= 0) return false;

            int amountToAdd = Mathf.Min(spaceLeft, amount);
            if (!barnCropInventory.ContainsKey(seedId)) barnCropInventory[seedId] = 0;
            barnCropInventory[seedId] += amountToAdd;

            OnInventoryUpdated?.Invoke();
            return true;
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
            OnInventoryUpdated?.Invoke();
        }

        public bool ConsumeBarnCrop(string seedId, int amount)
        {
            if (barnCropInventory.ContainsKey(seedId) && barnCropInventory[seedId] >= amount)
            {
                barnCropInventory[seedId] -= amount;
                if (barnCropInventory[seedId] <= 0) barnCropInventory.Remove(seedId);
                OnInventoryUpdated?.Invoke();
                return true;
            }
            return false;
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
