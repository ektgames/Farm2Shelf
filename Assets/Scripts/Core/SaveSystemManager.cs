using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// 3 Slotlu Oyun Kaydetme ve Yükleme Yöneticisi (Save/Load Manager).
    /// PlayerPrefs üzerinde JSON formatında oyundaki tüm durumu saklar ve yükler.
    /// </summary>
    public class SaveSystemManager : MonoBehaviour
    {
        public static SaveSystemManager Instance { get; private set; }

        private const string SAVE_SLOT_PREFIX = "Farm2Shelf_SaveSlot_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Belirtilen slot numarası (1, 2, 3) için kayıtlı oyun verisini okur.
        /// </summary>
        public SaveGameData GetSlotData(int slotIndex)
        {
            string key = SAVE_SLOT_PREFIX + slotIndex;
            if (!PlayerPrefs.HasKey(key))
            {
                return new SaveGameData
                {
                    slotIndex = slotIndex,
                    isEmptySlot = true
                };
            }

            try
            {
                string json = PlayerPrefs.GetString(key);
                SaveGameData data = JsonUtility.FromJson<SaveGameData>(json);
                if (data != null)
                {
                    data.slotIndex = slotIndex;
                    data.isEmptySlot = false;
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystemManager] Slot {slotIndex} okuma hatası: " + ex.Message);
            }

            return new SaveGameData { slotIndex = slotIndex, isEmptySlot = true };
        }

        /// <summary>
        /// O anki oyun durumunu (bakiye, mağaza seviyesi, zaman, dükkan durumu, personeller vb.) belirtilen slota kaydeder.
        /// </summary>
        public bool SaveCurrentGame(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > 3) return false;

            SaveGameData saveData = new SaveGameData
            {
                slotIndex = slotIndex,
                isEmptySlot = false,
                saveTimestamp = DateTime.Now.ToString("dd.MM.yyyy - HH:mm")
            };

            // 1. Ekonomi ve Bakiye
            if (EconomyManager.Instance != null)
            {
                saveData.playerMoney = EconomyManager.Instance.Credits;
            }
            else
            {
                saveData.playerMoney = 400000;
            }

            // 2. Mağaza Seviyesi
            if (EnvironmentBuilder.Instance != null)
            {
                saveData.storeLevel = EnvironmentBuilder.Instance.CurrentUpgradeLevel;
            }
            else
            {
                saveData.storeLevel = 1;
            }

            // 3. Dükkan Açık/Kapalı Durumu
            if (StoreStatusManager.Instance != null)
            {
                saveData.isStoreOpen = StoreStatusManager.Instance.IsOpen;
            }

            // 4. Oyun Saati ve Günü
            if (TimeManager.Instance != null)
            {
                saveData.gameDay = TimeManager.Instance.Day;
                saveData.gameHour = TimeManager.Instance.Hour;
                saveData.gameMinute = TimeManager.Instance.Minute;
            }
            else
            {
                saveData.gameDay = 1;
                saveData.gameHour = 8;
                saveData.gameMinute = 0;
            }

            // 5. Personel Listesi
            if (StaffManager.Instance != null)
            {
                List<StaffMember> staffList = StaffManager.Instance.GetActiveStaff();
                saveData.activeStaffCount = staffList != null ? staffList.Count : 0;
                if (staffList != null)
                {
                    foreach (var s in staffList)
                    {
                        if (s == null) continue;
                        saveData.staffList.Add(new StaffSaveData
                        {
                            id = s.id,
                            name = s.name,
                            role = s.role.ToString(),
                            isFemale = s.isFemale,
                            dailySalary = s.dailySalary,
                            shiftHours = s.shiftHours,
                            isActive = s.isActive
                        });
                    }
                }
            }

            // 6. Ahır Mahsul Stoğu
            if (GardenSeedInventoryManager.Instance != null)
            {
                saveData.barnCropKg = GardenSeedInventoryManager.Instance.GetTotalBarnStoredAmount();
                Dictionary<string, int> crops = GardenSeedInventoryManager.Instance.GetBarnCropInventory();
                if (crops != null)
                {
                    foreach (var kvp in crops)
                    {
                        saveData.barnCrops.Add(new BarnCropSaveData
                        {
                            seedId = kvp.Key,
                            count = kvp.Value
                        });
                    }
                }
            }

            // 7. Yerleştirilen Mobilyalar ve Raf Stokları
            PlacedFurnitureController[] furnitureList = UnityEngine.Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            if (furnitureList != null)
            {
                foreach (var f in furnitureList)
                {
                    if (f == null) continue;
                    ShelfSaveData sData = new ShelfSaveData
                    {
                        furnitureId = f.name,
                        furnitureType = f.FurnitureType.ToString(),
                        posX = f.transform.position.x,
                        posY = f.transform.position.y,
                        posZ = f.transform.position.z,
                        rotX = f.transform.eulerAngles.x,
                        rotY = f.transform.eulerAngles.y,
                        rotZ = f.transform.eulerAngles.z
                    };

                    if (f.rows != null)
                    {
                        foreach (var r in f.rows)
                        {
                            if (r == null) continue;
                            sData.rows.Add(new ShelfSaveRowData
                            {
                                rowId = r.rowId,
                                productId = r.productId,
                                productName = r.productName,
                                iconEmoji = "",
                                unitPrice = r.unitPrice,
                                currentStock = r.currentStock,
                                maxCapacity = r.maxCapacity
                            });
                        }
                    }

                    saveData.furnitureList.Add(sData);
                }
            }

            try
            {
                string json = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(SAVE_SLOT_PREFIX + slotIndex, json);
                PlayerPrefs.Save();
                Debug.Log($"[SaveSystemManager] Slot {slotIndex} başarıyla kaydedildi! Tarih: " + saveData.saveTimestamp);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystemManager] Slot {slotIndex} kaydedilemedi: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Seçilen slot numarasındaki veriyi okuyarak oyunu bıraktığı durumdan başlatır.
        /// </summary>
        public bool LoadGameFromSlot(int slotIndex)
        {
            SaveGameData saveData = GetSlotData(slotIndex);
            if (saveData == null || saveData.isEmptySlot)
            {
                Debug.LogWarning($"[SaveSystemManager] Slot {slotIndex} boş olduğu için yüklenemedi!");
                return false;
            }

            // 1. Zamanı Sıfırla ve Başlat
            Time.timeScale = 1.0f;

            // 2. Bakiye Yükleme
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SetCredits(saveData.playerMoney);
            }

            // 3. Mağaza Seviyesi Yükleme
            if (EnvironmentBuilder.Instance != null && saveData.storeLevel != EnvironmentBuilder.Instance.CurrentUpgradeLevel)
            {
                EnvironmentBuilder.Instance.UpgradeStoreToLevel(saveData.storeLevel);
            }

            // 4. Dükkan Açık/Kapalı Durumu
            if (StoreStatusManager.Instance != null)
            {
                if (saveData.isStoreOpen) StoreStatusManager.Instance.OpenStore();
                else StoreStatusManager.Instance.CloseStore();
            }

            // 5. Oyun Zamanı ve Günü Yükleme
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetTime(saveData.gameDay, saveData.gameHour, saveData.gameMinute);
            }

            // 6. Ahır Mahsul Stokları Yükleme
            if (GardenSeedInventoryManager.Instance != null && saveData.barnCrops != null)
            {
                foreach (var crop in saveData.barnCrops)
                {
                    if (crop == null || string.IsNullOrEmpty(crop.seedId)) continue;
                    GardenSeedInventoryManager.Instance.AddBarnCrop(crop.seedId, crop.count);
                }
            }

            // 7. Personel Yapısını Güncelleme
            if (StaffVisualManager.Instance != null)
            {
                StaffVisualManager.Instance.SyncStaff3DModels();
            }

            Debug.Log($"[SaveSystemManager] Slot {slotIndex} başarıyla yüklendi! Bakiye: {saveData.playerMoney}C");
            return true;
        }

        /// <summary>
        /// Slot verisini siler (Boşaltır).
        /// </summary>
        public void DeleteSlotData(int slotIndex)
        {
            string key = SAVE_SLOT_PREFIX + slotIndex;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}
