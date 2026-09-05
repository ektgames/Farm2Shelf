using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// 3 Slotlu Eksiksiz Oyun Kaydetme ve Yükleme Yöneticisi (Save/Load Master Manager).
    /// PlayerPrefs üzerinde JSON formatında oyundaki TÜM DURUMU (%100 Firesiz):
    /// Bakiye, Mağaza Seviyesi, Dükkan Açık/Kapalı, Saat, Gün, Mevsim, Yıl,
    /// Borsa Portföyü & Hisse Fiyat Geçmişi, Finans Dökümü & Gelir/Gider Kayıtları,
    /// Banka Kredileri, Sosyal Medya & Takipçiler, Kalite Puanı & Seviyesi, Duvar/Zemin Renkleri,
    /// Tohum Envanteri & Ahır Deposu, Mağaza ve Çiftlik Personelleri, Tarladaki Ekinler,
    /// Palette Bekleyen Teslimat Paketleri ve Sahnede Yerleştirilmiş Tüm Mobilya/Rafları
    /// eksiksiz kaydeder ve geri yükler.
    /// </summary>
    public class SaveSystemManager : MonoBehaviour
    {
        public static SaveSystemManager Instance { get; private set; }

        private const string SAVE_SLOT_PREFIX = "Farm2Shelf_SaveSlot_";
        private const string BACKUP_SUFFIX = "_Backup";
        private const int CURRENT_SAVE_FORMAT_VERSION = 3;
        private int activeSessionSlot;
        private float lastAutosaveTime = float.NegativeInfinity;
        private const float AUTOSAVE_DEBOUNCE_SECONDS = 5f;

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

        private IEnumerator Start()
        {
            while (TimeManager.Instance == null)
            {
                yield return null;
            }

            TimeManager.Instance.OnMidnightRollover -= HandleMidnightAutosave;
            TimeManager.Instance.OnMidnightRollover += HandleMidnightAutosave;
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

            SaveGameData data;
            if (TryDeserializeSlot(PlayerPrefs.GetString(key), slotIndex, out data))
            {
                return data;
            }

            string backupKey = key + BACKUP_SUFFIX;
            if (PlayerPrefs.HasKey(backupKey) && TryDeserializeSlot(PlayerPrefs.GetString(backupKey), slotIndex, out data))
            {
                Debug.LogWarning($"[SaveSystemManager] Slot {slotIndex} ana kaydı bozuk; güvenli yedek kullanıldı.");
                return data;
            }

            Debug.LogError($"[SaveSystemManager] Slot {slotIndex} okunamadı; ana kayıt ve yedek geçersiz.");
            return new SaveGameData { slotIndex = slotIndex, isEmptySlot = false, hasLoadError = true };
        }

        /// <summary>
        /// O anki OYUNUN TÜM DURUMUNU (15 Ana Sistem) eksiksiz olarak belirtilen slota kaydeder.
        /// </summary>
        public bool SaveCurrentGame(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > 3) slotIndex = 1;

            SaveGameData saveData = new SaveGameData
            {
                saveFormatVersion = CURRENT_SAVE_FORMAT_VERSION,
                slotIndex = slotIndex,
                isEmptySlot = false,
                saveTimestamp = DateTime.Now.ToString("dd.MM.yyyy - HH:mm")
            };

            // 1. EKONOMİ VE BAKİYE
            if (EconomyManager.Instance != null)
            {
                saveData.playerMoney = EconomyManager.Instance.Credits;
            }
            else
            {
                saveData.playerMoney = 50000;
            }

            // 2. MAĞAZA SEVİYESİ VE TADİLAT RENKLERİ
            if (EnvironmentBuilder.Instance != null)
            {
                saveData.storeLevel = EnvironmentBuilder.Instance.CurrentUpgradeLevel;

                Color wallC = EnvironmentBuilder.Instance.CurrentWallColor;
                saveData.wallColorR = wallC.r;
                saveData.wallColorG = wallC.g;
                saveData.wallColorB = wallC.b;
                saveData.wallColorA = wallC.a;

                Color floorC = EnvironmentBuilder.Instance.CurrentFloorColor;
                saveData.floorColorR = floorC.r;
                saveData.floorColorG = floorC.g;
                saveData.floorColorB = floorC.b;
                saveData.floorColorA = floorC.a;
            }

            // 2.b ATÖLYE SEVİYESİ
            if (WorkshopManager.Instance != null)
            {
                saveData.workshopLevel = WorkshopManager.Instance.CurrentWorkshopLevel;
            }

            // 3. DÜKKAN AÇIK / KAPALI DURUMU VE ŞİRKET BİLGİLERİ
            if (StoreStatusManager.Instance != null)
            {
                saveData.isStoreOpen = StoreStatusManager.Instance.IsOpen;
                saveData.companyName = StoreStatusManager.Instance.CompanyName;
                saveData.playerName = StoreStatusManager.Instance.PlayerName;
            }

            // 4. OYUN ZAMANI, GÜNÜ, MEVSİMİ VE YILI
            if (TimeManager.Instance != null)
            {
                saveData.gameDay = TimeManager.Instance.Day;
                saveData.gameHour = TimeManager.Instance.Hour;
                saveData.gameMinute = TimeManager.Instance.Minute;
                saveData.gameSeason = TimeManager.Instance.CurrentSeason.ToString();
                saveData.gameYear = TimeManager.Instance.Year;
                saveData.isTimePaused = TimeManager.Instance.IsTimePaused;
                saveData.isDayActive = TimeManager.Instance.IsDayActive;
            }

            if (GameHUDManager.Instance != null)
            {
                saveData.isWaitingForEvacuation = GameHUDManager.Instance.IsWaitingForEvacuation;
            }

            // 5. MAĞAZA KALİTE SEVİYESİ VE YILDIZ PUANI
            if (StoreQualityManager.Instance != null)
            {
                saveData.storeQualityScore = StoreQualityManager.Instance.QualityScore;
                saveData.storeQualityLevel = StoreQualityManager.Instance.QualityLevel;
            }

            // 6. BORSA VE PORTFÖY
            if (StockMarketManager.Instance != null)
            {
                List<StockData> stocks = StockMarketManager.Instance.GetAllStocks();
                if (stocks != null)
                {
                    foreach (var s in stocks)
                    {
                        if (s == null) continue;
                        saveData.stockMarket.Add(new StockSaveItem
                        {
                            tickerSymbol = s.tickerSymbol,
                            currentPrice = s.currentPrice,
                            previousPrice = s.previousPrice,
                            ownedShares = s.ownedShares,
                            averageBuyPrice = s.averageBuyPrice,
                            totalInvested = s.totalInvested,
                            priceHistory = new List<float>(s.priceHistory)
                        });
                    }
                }
            }

            // 7. FİNANS VE GELİR/GİDER GEÇMİŞİ
            if (FinanceManager.Instance != null)
            {
                saveData.totalRevenue = FinanceManager.Instance.TotalRevenue;
                saveData.totalExpenses = FinanceManager.Instance.TotalExpenses;
                saveData.dailyRevenue = FinanceManager.Instance.DailyRevenue;
                saveData.dailyExpenses = FinanceManager.Instance.DailyExpenses;
                saveData.monthlyRevenue = FinanceManager.Instance.MonthlyRevenue;
                saveData.monthlyExpenses = FinanceManager.Instance.MonthlyExpenses;

                List<TransactionRecord> logs = FinanceManager.Instance.GetTransactionLog();
                if (logs != null)
                {
                    saveData.transactionLog.AddRange(logs);
                }
            }

            // 8. BANKA KREDİLERİ
            if (BankLoanManager.Instance != null)
            {
                List<ActiveLoanData> loans = BankLoanManager.Instance.GetActiveLoans();
                if (loans != null)
                {
                    saveData.bankLoans.AddRange(loans);
                }
            }

            // 9. SOSYAL MEDYA VE TAKİPÇİLER
            if (SocialMediaManager.Instance != null)
            {
                saveData.socialFollowerCount = SocialMediaManager.Instance.FollowerCount;
                List<SocialTweetData> feed = SocialMediaManager.Instance.GetTweetFeed();
                if (feed != null)
                {
                    saveData.socialFeed.AddRange(feed);
                }
            }

            // 10. TOHUM ENVANTERİ VE AHIR DEPOSU
            if (GardenSeedInventoryManager.Instance != null)
            {
                saveData.barnUpgradeLevel = GardenSeedInventoryManager.Instance.BarnUpgradeLevel;
                saveData.barnCropKg = GardenSeedInventoryManager.Instance.GetTotalBarnStoredAmount();

                Dictionary<string, int> ownedSeeds = GardenSeedInventoryManager.Instance.GetOwnedSeedsInventory();
                if (ownedSeeds != null)
                {
                    foreach (var kvp in ownedSeeds)
                    {
                        saveData.ownedSeeds.Add(new OwnedSeedSaveData { seedId = kvp.Key, count = kvp.Value });
                    }
                }

                Dictionary<string, int> barnCrops = GardenSeedInventoryManager.Instance.GetBarnCropInventory();
                if (barnCrops != null)
                {
                    foreach (var kvp in barnCrops)
                    {
                        saveData.barnCrops.Add(new BarnCropSaveData { seedId = kvp.Key, count = kvp.Value });
                    }
                }
            }

            // 10.b ATÖLYE HAMMADDE PALETİ VE MAKİNE KOLİLERİ
            if (WorkshopPalletManager.Instance != null)
            {
                Dictionary<string, int> wsCrops = WorkshopPalletManager.Instance.GetCropInventory();
                if (wsCrops != null)
                {
                    foreach (var kvp in wsCrops)
                    {
                        saveData.workshopCrops.Add(new BarnCropSaveData { seedId = kvp.Key, count = kvp.Value });
                    }
                }

                List<string> wsMachines = WorkshopPalletManager.Instance.GetPendingMachineBoxTypes();
                if (wsMachines != null)
                {
                    saveData.pendingWorkshopMachineBoxes.AddRange(wsMachines);
                }
            }

            // 11. PALETTE BEKLEYEN TESLİMAT KOLİLERİ
            if (FurnitureDeliveryManager.Instance != null)
            {
                List<string> pendingBoxes = FurnitureDeliveryManager.Instance.GetActiveBoxTypes();
                if (pendingBoxes != null)
                {
                    saveData.pendingDeliveryBoxes.AddRange(pendingBoxes);
                }
            }

            // 12. PERSONEL KADROSU (MAĞAZA VE ÇİFTLİK)
            if (StaffManager.Instance != null)
            {
                List<StaffMember> staffList = StaffManager.Instance.GetActiveStaff();
                saveData.activeStaffCount = staffList != null ? staffList.Count : 0;
                if (staffList != null)
                {
                    foreach (var s in staffList)
                    {
                        if (s == null) continue;
                        bool isEarly = StaffVisualManager.Instance != null && StaffVisualManager.Instance.IsStaffCalledEarlyToday(s.id);
                        saveData.staffList.Add(new StaffSaveData
                        {
                            id = s.id,
                            name = s.name,
                            role = s.role.ToString(),
                            isFemale = s.isFemale,
                            dailySalary = s.dailySalary,
                            shiftHours = s.shiftHours,
                            isActive = s.isActive,
                            isCalledEarly = isEarly
                        });
                    }
                }

                List<StaffMember> farmList = StaffManager.Instance.GetFarmStaffList();
                if (farmList != null)
                {
                    foreach (var fs in farmList)
                    {
                        if (fs == null) continue;
                        bool isEarlyFarm = StaffVisualManager.Instance != null && StaffVisualManager.Instance.IsStaffCalledEarlyToday(fs.id);
                        saveData.farmStaffList.Add(new StaffSaveData
                        {
                            id = fs.id,
                            name = fs.name,
                            role = fs.role.ToString(),
                            isFemale = fs.isFemale,
                            dailySalary = fs.dailySalary,
                            shiftHours = fs.shiftHours,
                            isActive = fs.isActive,
                            isCalledEarly = isEarlyFarm
                        });
                    }
                }

                List<StaffMember> courierList = StaffManager.Instance.GetCourierStaffList();
                if (courierList != null)
                {
                    foreach (var cs in courierList)
                    {
                        if (cs == null) continue;
                        saveData.courierStaffList.Add(new StaffSaveData
                        {
                            id = cs.id,
                            name = cs.name,
                            role = cs.role.ToString(),
                            isFemale = cs.isFemale,
                            dailySalary = cs.dailySalary,
                            shiftHours = cs.shiftHours,
                            isActive = cs.isActive,
                            isCalledEarly = false
                        });
                    }
                }
            }

            // 12.b Kurye Motorsiklet Filosu
            saveData.ownedMotorcycleCount = (CourierManager.Instance != null) ? CourierManager.Instance.OwnedMotorcycleCount : 0;

            if (OnlineMarketOrderManager.Instance != null)
            {
                saveData.onlineOrders = OnlineMarketOrderManager.Instance.CreateSaveSnapshot();
            }

            if (WholesaleTruckManager.Instance != null)
            {
                foreach (var package in WholesaleTruckManager.Instance.PackagesForSave)
                {
                    if (package != null) saveData.wholesaleTruckPackageIds.Add(package.id);
                }
            }

            if (GreenTruckDeliveryManager.Instance != null)
            {
                foreach (var package in GreenTruckDeliveryManager.Instance.PackagesForSave)
                {
                    if (package != null) saveData.greenTruckPackageIds.Add(package.id);
                }
            }

            DeliveryTruckSaveData wholesaleSnap = WholesaleTruckManager.Instance != null
                ? WholesaleTruckManager.Instance.CreateSaveSnapshot()
                : null;
            DeliveryTruckSaveData greenSnap = GreenTruckDeliveryManager.Instance != null
                ? GreenTruckDeliveryManager.Instance.CreateSaveSnapshot()
                : null;
            saveData.activeDeliveryTruck = wholesaleSnap != null ? wholesaleSnap : greenSnap;

            saveData.customProductPrices = WholesaleDatabase.ExportCustomPrices();

            // 13. TARLADAKİ EKİNLER
            var plots = FieldPlotController.AllPlots;
            if (plots != null)
            {
                foreach (var p in plots)
                {
                    if (p == null) continue;
                    saveData.fieldCrops.Add(new CropSaveData
                    {
                        plotName = p.gameObject.name,
                        seedId = p.PlantedSeedId,
                        currentGrowthDay = p.CurrentGrowthDay,
                        totalGrowthDays = p.TotalGrowthDays,
                        needsWater = p.NeedsWater,
                        wateredToday = p.WateredToday,
                        state = p.State.ToString()
                    });
                }
            }

            // 14. SAHNEDEKİ MOBİLYALAR VE RAF STOKLARI
            var placedList = PlacedFurnitureController.AllPlacedFurniture;
            if (placedList != null)
            {
                foreach (var f in placedList)
                {
                    if (f == null || f.gameObject == null) continue;

                    Vector3 pos = f.transform.position;
                    Vector3 rot = f.transform.rotation.eulerAngles;

                    ShelfSaveData fData = new ShelfSaveData
                    {
                        furnitureId = f.gameObject.name,
                        furnitureType = f.FurnitureType.ToString(),
                        posX = pos.x,
                        posY = pos.y,
                        posZ = pos.z,
                        rotX = rot.x,
                        rotY = rot.y,
                        rotZ = rot.z
                    };

                    if (f.rows != null)
                    {
                        foreach (var r in f.rows)
                        {
                            if (r == null) continue;
                            fData.rows.Add(new ShelfSaveRowData
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

                    saveData.furnitureList.Add(fData);
                }
            }

            // 14.b ATÖLYE MAKİNELERİ VE ÜRETİM DURUMLARI
            var wsControllers = GameObject.FindObjectsOfType<Farm2Shelf.Environment.WorkshopMachineController>();
            if (wsControllers != null)
            {
                foreach (var ws in wsControllers)
                {
                    if (ws == null) continue;
                    saveData.workshopMachines.Add(new WorkshopMachineSaveData
                    {
                        instanceId = ws.machineInstanceId,
                        machineType = ws.machineType.ToString(),
                        posX = ws.transform.position.x,
                        posY = ws.transform.position.y,
                        posZ = ws.transform.position.z,
                        rotY = ws.transform.rotation.eulerAngles.y,
                        isProducing = ws.isProducing,
                        isReadyToCollect = ws.isReadyToCollect,
                        activeRecipeId = ws.activeRecipeId,
                        remainingSeconds = ws.remainingProductionSeconds,
                        totalDuration = ws.totalProductionSeconds
                    });
                }
            }

            // 15. EĞİTİM ADIMI (TUTORIAL PROGRESS)
            if (TutorialManager.Instance != null)
            {
                saveData.tutorialStep = TutorialManager.Instance.CurrentStep.ToString();
            }

            try
            {
                string json = JsonUtility.ToJson(saveData, true);
                string key = SAVE_SLOT_PREFIX + slotIndex;
                string backupKey = key + BACKUP_SUFFIX;
                if (PlayerPrefs.HasKey(key))
                {
                    string previousJson = PlayerPrefs.GetString(key);
                    SaveGameData previousData;
                    if (TryDeserializeSlot(previousJson, slotIndex, out previousData))
                    {
                        PlayerPrefs.SetString(backupKey, previousJson);
                    }
                }
                PlayerPrefs.SetString(key, json);
                if (!PlayerPrefs.HasKey(backupKey))
                {
                    PlayerPrefs.SetString(backupKey, json);
                }
                PlayerPrefs.SetInt("Farm2Shelf_LastPlayedSlot", slotIndex);
                PlayerPrefs.Save();
                activeSessionSlot = slotIndex;
                lastAutosaveTime = Time.realtimeSinceStartup;

                Debug.Log($"[SaveSystemManager] Slot {slotIndex} EKSİKSİZ KAYDEDİLDİ! Bakiye: {saveData.playerMoney}C | Mobilya: {saveData.furnitureList.Count} | Personel: {saveData.staffList.Count} ({saveData.activeStaffCount} Aktif) | Tarla: {saveData.fieldCrops.Count}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystemManager] Slot {slotIndex} kaydetme hatası: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Seçilen slot verisini okuyarak TÜM SİSTEMLERİ (%100 Firesiz) kalınan durumdan başlatır.
        /// </summary>
        public bool LoadGameFromSlot(int slotIndex)
        {
            SaveGameData saveData = GetSlotData(slotIndex);
            if (saveData == null || saveData.isEmptySlot)
            {
                Debug.LogWarning($"[SaveSystemManager] Slot {slotIndex} boş olduğu için yüklenemedi!");
                return false;
            }
            if (saveData.hasLoadError)
            {
                Debug.LogError($"[SaveSystemManager] Slot {slotIndex} bozuk olduğu için güvenli şekilde yükleme iptal edildi.");
                return false;
            }

            // 1. Zamanı Sıfırla ve Başlat
            Time.timeScale = 1.0f;

            // 2. Sahnedeki Eski Müşteri ve Personel Modellerini Temizle
            if (CustomerShoppingManager.Instance != null)
            {
                CustomerShoppingManager.Instance.ClearAllCustomers();
            }

            if (StaffVisualManager.Instance != null)
            {
                StaffVisualManager.Instance.ClearAllStaffModels();
            }

            // 3. Mağaza Seviyesi ve Renkleri Yükleme
            if (EnvironmentBuilder.Instance != null)
            {
                if (saveData.storeLevel > 0 && saveData.storeLevel != EnvironmentBuilder.Instance.CurrentUpgradeLevel)
                {
                    EnvironmentBuilder.Instance.UpgradeStoreToLevel(saveData.storeLevel);
                }

                Color wallC = new Color(saveData.wallColorR, saveData.wallColorG, saveData.wallColorB, saveData.wallColorA);
                EnvironmentBuilder.Instance.ApplyWallColor(wallC);

                Color floorC = new Color(saveData.floorColorR, saveData.floorColorG, saveData.floorColorB, saveData.floorColorA);
                EnvironmentBuilder.Instance.ApplyFloorStyle(floorC);
            }

            // 3.b Atölye Seviyesini Yükleme
            if (WorkshopManager.Instance != null && saveData.workshopLevel > 0)
            {
                WorkshopManager.Instance.SetWorkshopLevel(saveData.workshopLevel);
            }

            // 4. Eski Mobilyaları Temizle ve Kayıtlı Mobilyaları / Rafları Yeniden Kur
            var existingFurniture = new List<PlacedFurnitureController>(PlacedFurnitureController.AllPlacedFurniture);
            foreach (var f in existingFurniture)
            {
                if (f != null && f.gameObject != null)
                {
                    PlacedFurnitureController.AllPlacedFurniture.Remove(f);
                    UnityEngine.Object.Destroy(f.gameObject);
                }
            }

            if (FurniturePlacementManager.Instance != null && saveData.furnitureList != null)
            {
                foreach (var sData in saveData.furnitureList)
                {
                    if (sData == null) continue;
                    if (Enum.TryParse<FurnitureType>(sData.furnitureType, out FurnitureType fType))
                    {
                        Vector3 pos = new Vector3(sData.posX, sData.posY, sData.posZ);
                        Quaternion rot = Quaternion.Euler(sData.rotX, sData.rotY, sData.rotZ);

                        ShelfRowData[] rows = null;
                        if (sData.rows != null && sData.rows.Count > 0)
                        {
                            rows = new ShelfRowData[sData.rows.Count];
                            for (int i = 0; i < sData.rows.Count; i++)
                            {
                                var rData = sData.rows[i];
                                rows[i] = new ShelfRowData(
                                    rData.rowId,
                                    rData.productName,
                                    rData.currentStock,
                                    rData.maxCapacity,
                                    rData.unitPrice,
                                    rData.productId
                                );
                            }
                        }

                        FurniturePlacementManager.Instance.SpawnRestoredFurniture(fType, pos, rot, rows);
                    }
                }
            }

            // 4.b Atölye Makineleri Üretim Durumlarını Eşleştir ve Yükle
            if (saveData.workshopMachines != null && saveData.workshopMachines.Count > 0)
            {
                var allWsControllers = GameObject.FindObjectsOfType<Farm2Shelf.Environment.WorkshopMachineController>();
                if (allWsControllers != null)
                {
                    foreach (var ws in allWsControllers)
                    {
                        if (ws == null) continue;
                        var mData = saveData.workshopMachines.Find(m =>
                            !string.IsNullOrEmpty(m.instanceId) &&
                            !string.IsNullOrEmpty(ws.machineInstanceId) &&
                            m.instanceId == ws.machineInstanceId);
                        if (mData == null)
                        {
                            mData = saveData.workshopMachines.Find(m =>
                                Vector3.Distance(ws.transform.position, new Vector3(m.posX, m.posY, m.posZ)) < 0.5f);
                        }
                        if (mData != null)
                        {
                            if (!string.IsNullOrEmpty(mData.instanceId)) ws.machineInstanceId = mData.instanceId;
                            ws.RestoreState(
                                mData.activeRecipeId,
                                mData.isProducing,
                                mData.isReadyToCollect,
                                mData.remainingSeconds,
                                mData.totalDuration);
                        }
                    }
                }
            }

            // 5. Bakiye Yükleme
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SetCredits(saveData.playerMoney);
            }

            // 6. Mağaza Kalite Seviyesi ve Puanı Yükleme
            if (StoreQualityManager.Instance != null)
            {
                StoreQualityManager.Instance.SetQualityData(saveData.storeQualityScore, saveData.storeQualityLevel);
            }

            // 7. Borsa Portföyü ve Hisse Geçmişi Yükleme
            if (StockMarketManager.Instance != null && saveData.stockMarket != null)
            {
                StockMarketManager.Instance.RestoreStockMarketData(saveData.stockMarket);
            }

            // 8. Finans Dökümü ve İşlem Geçmişi Yükleme
            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RestoreFinanceData(
                    saveData.totalRevenue,
                    saveData.totalExpenses,
                    saveData.dailyRevenue,
                    saveData.dailyExpenses,
                    saveData.monthlyRevenue,
                    saveData.monthlyExpenses,
                    saveData.transactionLog
                );
            }

            // 9. Banka Kredileri Yükleme
            if (BankLoanManager.Instance != null && saveData.bankLoans != null)
            {
                BankLoanManager.Instance.RestoreActiveLoans(saveData.bankLoans);
            }

            // 10. Sosyal Medya ve Takipçiler Yükleme
            if (SocialMediaManager.Instance != null)
            {
                SocialMediaManager.Instance.RestoreSocialMediaData(saveData.socialFollowerCount, saveData.socialFeed);
            }

            // 11. Tohum Envanteri ve Ahır Deposu Yükleme
            if (GardenSeedInventoryManager.Instance != null)
            {
                GardenSeedInventoryManager.Instance.SetBarnUpgradeLevel(saveData.barnUpgradeLevel);

                if (saveData.ownedSeeds != null)
                {
                    Dictionary<string, int> seedDict = new Dictionary<string, int>();
                    foreach (var s in saveData.ownedSeeds)
                    {
                        if (s != null && !string.IsNullOrEmpty(s.seedId)) seedDict[s.seedId] = s.count;
                    }
                    GardenSeedInventoryManager.Instance.RestoreOwnedSeeds(seedDict);
                }

                GardenSeedInventoryManager.Instance.ClearBarnInventory();
                if (saveData.barnCrops != null)
                {
                    foreach (var crop in saveData.barnCrops)
                    {
                        if (crop == null || string.IsNullOrEmpty(crop.seedId)) continue;
                        GardenSeedInventoryManager.Instance.AddBarnCrop(crop.seedId, crop.count);
                    }
                }
            }

            // 11.b Atölye Hammadde Paletini ve Bekleyen Makine Kolilerini Yükleme
            if (WorkshopPalletManager.Instance != null)
            {
                WorkshopPalletManager.Instance.ClearAll();
                if (saveData.workshopCrops != null)
                {
                    Dictionary<string, int> wsDict = new Dictionary<string, int>();
                    foreach (var crop in saveData.workshopCrops)
                    {
                        if (crop == null || string.IsNullOrEmpty(crop.seedId)) continue;
                        wsDict[crop.seedId] = crop.count;
                    }
                    WorkshopPalletManager.Instance.SetAllCrops(wsDict);
                }

                if (saveData.pendingWorkshopMachineBoxes != null)
                {
                    WorkshopPalletManager.Instance.RestorePendingMachineBoxes(saveData.pendingWorkshopMachineBoxes);
                }
            }

            // 12. Palette Bekleyen Teslimat Kolilerini Yükleme
            if (FurnitureDeliveryManager.Instance != null && saveData.pendingDeliveryBoxes != null)
            {
                FurnitureDeliveryManager.Instance.RestorePendingBoxes(saveData.pendingDeliveryBoxes);
            }

            // 13. Tarladaki Ekinleri Yükleme (Tüm parselleri eksiksiz ve firesiz güncelle)
            var plots = FieldPlotController.AllPlots;
            if (plots != null)
            {
                foreach (var p in plots)
                {
                    if (p == null) continue;
                    CropSaveData cData = saveData.fieldCrops != null ? saveData.fieldCrops.Find(c => c.plotName == p.gameObject.name) : null;
                    if (cData != null)
                    {
                        p.RestoreCropState(cData.seedId, cData.currentGrowthDay, cData.totalGrowthDays, cData.needsWater, cData.wateredToday, cData.state);
                    }
                    else
                    {
                        p.RestoreCropState("", 0, 1, false, false, "Empty");
                    }
                }
            }

            // 14. Personel Kadrolarını Yükleme (Mağaza ve Çiftlik)
            List<string> earlyCalledIds = new List<string>();
            if (StaffManager.Instance != null)
            {
                if (saveData.staffList != null)
                {
                    List<StaffMember> restoredStaff = new List<StaffMember>();
                    foreach (var sData in saveData.staffList)
                    {
                        if (sData == null) continue;
                        if (Enum.TryParse<StaffRole>(sData.role, out StaffRole parsedRole))
                        {
                            string normShift = StaffManager.NormalizeShift(sData.shiftHours);
                            StaffMember member = new StaffMember(sData.id, sData.name, parsedRole, normShift, sData.dailySalary, sData.isActive, sData.isFemale);
                            restoredStaff.Add(member);
                            if (sData.isCalledEarly) earlyCalledIds.Add(sData.id);
                        }
                    }
                    StaffManager.Instance.SetStaffList(restoredStaff);
                }

                if (saveData.farmStaffList != null)
                {
                    List<StaffMember> restoredFarmStaff = new List<StaffMember>();
                    foreach (var fsData in saveData.farmStaffList)
                    {
                        if (fsData == null) continue;
                        if (Enum.TryParse<StaffRole>(fsData.role, out StaffRole parsedRole))
                        {
                            string normShift = StaffManager.NormalizeShift(fsData.shiftHours);
                            StaffMember member = new StaffMember(fsData.id, fsData.name, parsedRole, normShift, fsData.dailySalary, fsData.isActive, fsData.isFemale);
                            restoredFarmStaff.Add(member);
                            if (fsData.isCalledEarly) earlyCalledIds.Add(fsData.id);
                        }
                    }
                    StaffManager.Instance.SetFarmStaffList(restoredFarmStaff);
                }

                if (saveData.courierStaffList != null)
                {
                    List<StaffMember> restoredCouriers = new List<StaffMember>();
                    foreach (var csData in saveData.courierStaffList)
                    {
                        if (csData == null) continue;
                        string normShift = StaffManager.NormalizeShift(csData.shiftHours);
                        StaffMember member = new StaffMember(csData.id, csData.name, StaffRole.Kurye, normShift, csData.dailySalary, csData.isActive, csData.isFemale);
                        restoredCouriers.Add(member);
                    }
                    StaffManager.Instance.SetCourierStaffList(restoredCouriers);
                }
            }

            if (CourierManager.Instance != null)
            {
                CourierManager.Instance.RestoreOwnedMotorcycles(saveData.ownedMotorcycleCount);
            }

            WholesaleDatabase.RestoreCustomPrices(saveData.customProductPrices);

            if (OnlineMarketOrderManager.Instance != null)
            {
                OnlineMarketOrderManager.Instance.RestoreOrders(saveData.onlineOrders);
            }

            RestorePendingTruckDeliveries(saveData);

            if (StaffVisualManager.Instance != null)
            {
                StaffVisualManager.Instance.RestoreEarlyCalledStaff(earlyCalledIds);
            }

            // 15. Dükkan Açık/Kapalı Durumu ve Şirket İsmi
            if (StoreStatusManager.Instance != null)
            {
                if (!string.IsNullOrEmpty(saveData.companyName))
                {
                    StoreStatusManager.Instance.SetPlayerAndCompany(saveData.playerName, saveData.companyName);
                }

                StoreStatusManager.Instance.RestoreStoreStatus(saveData.isStoreOpen && saveData.gameHour < 24);
            }

            // 16. Oyun Zamanı, Günü, Mevsimi ve Yılı Yükleme
            if (TimeManager.Instance != null)
            {
                if (Enum.TryParse<TimeManager.Season>(saveData.gameSeason, out TimeManager.Season loadedSeason))
                {
                    TimeManager.Instance.SetTimeAndSeason(saveData.gameDay, saveData.gameHour, saveData.gameMinute, loadedSeason, saveData.gameYear);
                }
                else
                {
                    TimeManager.Instance.SetTime(saveData.gameDay, saveData.gameHour, saveData.gameMinute);
                }

                bool canResumeDay = saveData.isStoreOpen && saveData.gameHour < 24 && saveData.isDayActive;
                TimeManager.Instance.RestoreDayFlowState(canResumeDay, saveData.isTimePaused || !saveData.isStoreOpen);
            }

            // 17. Tahliye Durumu ve Gece Z Raporu Açılışı
            if (GameHUDManager.Instance != null)
            {
                GameHUDManager.Instance.SetWaitingForEvacuation(saveData.isWaitingForEvacuation);
            }

            if ((saveData.gameHour >= 24 || (saveData.gameHour == 0 && !saveData.isStoreOpen)) && !saveData.isStoreOpen)
            {
                int activeCustomers = (CustomerShoppingManager.Instance != null) ? CustomerShoppingManager.Instance.ActiveCustomerCount : 0;
                if (activeCustomers == 0 && EndOfDayReportModalUI.Instance != null)
                {
                    EndOfDayReportModalUI.Instance.ShowReport();
                }
            }

            // 18. Personel 3D Modellerini ve AI Görevlerini Senkronize Et (Mobilyalar kurulduktan sonra)
            if (StaffVisualManager.Instance != null)
            {
                StaffVisualManager.Instance.SyncStaff3DModels();
            }
            OnlineMarketOrderManager.Instance?.ResumeReadyDeliveries();

            // 19. Eğitim ve Başlangıç Görevleri Yükleme
            if (TutorialManager.Instance != null && !string.IsNullOrEmpty(saveData.tutorialStep))
            {
                if (Enum.TryParse<TutorialStep>(saveData.tutorialStep, out TutorialStep step))
                {
                    TutorialManager.Instance.RestoreTutorialStep(step);
                }
            }

            // 20. Mağaza Hijyeni & Zemin Çöp Temizliği
            GameObject trashGroup = GameObject.Find("Store_Trash_Group");
            if (trashGroup != null)
            {
                for (int i = trashGroup.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(trashGroup.transform.GetChild(i).gameObject);
                }
            }

            PlayerPrefs.SetInt("Farm2Shelf_LastPlayedSlot", slotIndex);
            PlayerPrefs.Save();
            activeSessionSlot = slotIndex;

            Debug.Log($"[SaveSystemManager] Slot {slotIndex} EKSİKSİZ YÜKLENDİ! Bakiye: {saveData.playerMoney}C | Borsa: {saveData.stockMarket?.Count} | Finans: {saveData.transactionLog?.Count} | Mobilya: {saveData.furnitureList?.Count} | Personel: {saveData.staffList?.Count}");
            return true;
        }

        /// <summary>
        /// Slot verisini siler (Boşaltır).
        /// </summary>
        public void DeleteSlotData(int slotIndex)
        {
            string key = SAVE_SLOT_PREFIX + slotIndex;
            bool changed = false;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                changed = true;
            }
            if (PlayerPrefs.HasKey(key + BACKUP_SUFFIX))
            {
                PlayerPrefs.DeleteKey(key + BACKUP_SUFFIX);
                changed = true;
            }
            if (changed) PlayerPrefs.Save();
            if (activeSessionSlot == slotIndex) activeSessionSlot = 0;
        }

        public int GetLastPlayedSlot()
        {
            return PlayerPrefs.GetInt("Farm2Shelf_LastPlayedSlot", 1);
        }

        public bool HasLastPlayedSlot()
        {
            int slot = PlayerPrefs.GetInt("Farm2Shelf_LastPlayedSlot", 0);
            if (slot < 1 || slot > 3) return false;
            SaveGameData data = GetSlotData(slot);
            return !data.isEmptySlot && !data.hasLoadError;
        }

        public void BeginNewUnsavedSession()
        {
            activeSessionSlot = 0;
        }

        public void ResetRuntimeForNewGame()
        {
            BeginNewUnsavedSession();
            Time.timeScale = 1f;

            StoreStatusManager.Instance?.RestoreStoreStatus(false);
            CustomerShoppingManager.Instance?.ClearAllCustomers();
            StaffVisualManager.Instance?.ClearAllStaffModels();
            StaffTaskController.Instance?.ClearAllStaffAI();

            if (StaffManager.Instance != null)
            {
                StaffManager.Instance.SetStaffList(new List<StaffMember>());
                StaffManager.Instance.SetFarmStaffList(new List<StaffMember>());
                StaffManager.Instance.SetCourierStaffList(new List<StaffMember>());
                StaffManager.Instance.ResetSalaryPaymentState();
            }

            CourierManager.Instance?.ResetFleet();
            OnlineMarketOrderManager.Instance?.ResetToDefaults();
            WholesaleTruckManager.Instance?.ClearAllPackages();
            GreenTruckDeliveryManager.Instance?.ClearPendingDeliveries();
            FurnitureDeliveryManager.Instance?.ClearPendingBoxes();

            if (GardenSeedInventoryManager.Instance != null)
            {
                GardenSeedInventoryManager.Instance.SetBarnUpgradeLevel(1);
                GardenSeedInventoryManager.Instance.ClearBarnInventory();
                GardenSeedInventoryManager.Instance.RestoreOwnedSeeds(new Dictionary<string, int>());
            }
            WorkshopPalletManager.Instance?.ClearAll();
            FieldPlotController.ResetAllPlotsToEmpty();

            foreach (var furniture in new List<PlacedFurnitureController>(PlacedFurnitureController.AllPlacedFurniture))
            {
                if (furniture == null) continue;
                PlacedFurnitureController.AllPlacedFurniture.Remove(furniture);
                Destroy(furniture.gameObject);
            }

            foreach (var machine in new List<WorkshopMachineController>(WorkshopMachineController.AllPlacedMachines))
            {
                if (machine != null) machine.RestoreState("", false, false, 0f, 0f);
            }

            WholesaleDatabase.ResetAllPricesToDefault();
            EconomyManager.Instance?.SetCredits(50000);
            FinanceManager.Instance?.ResetToDefaults();
            StockMarketManager.Instance?.ResetToDefaults();
            BankLoanManager.Instance?.RestoreActiveLoans(new List<ActiveLoanData>());
            SocialMediaManager.Instance?.ResetToDefaults();
            StoreQualityManager.Instance?.SetQualityData(0, 0);

            if (EnvironmentBuilder.Instance != null)
            {
                EnvironmentBuilder.Instance.UpgradeStoreToLevel(1);
                EnvironmentBuilder.Instance.ApplyWallColor(new Color(0.12f, 0.14f, 0.17f, 1f));
                EnvironmentBuilder.Instance.ApplyFloorStyle(new Color(0.85f, 0.72f, 0.53f, 1f));
            }
            WorkshopManager.Instance?.SetWorkshopLevel(1);
            TimeManager.Instance?.ResetToDefaults();
            GameHUDManager.Instance?.SetWaitingForEvacuation(false);
        }

        private bool TryDeserializeSlot(string json, int slotIndex, out SaveGameData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                data = JsonUtility.FromJson<SaveGameData>(json);
                if (data == null) return false;
                NormalizeSaveData(data);
                data.slotIndex = slotIndex;
                data.isEmptySlot = false;
                data.hasLoadError = false;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystemManager] Slot {slotIndex} JSON hatası: {ex.Message}");
                data = null;
                return false;
            }
        }

        private static void NormalizeSaveData(SaveGameData data)
        {
            if (data.saveFormatVersion < 1)
            {
                data.saveFormatVersion = 1;
            }
            data.customProductPrices ??= new List<CustomPriceSaveData>();
            data.onlineOrders ??= new List<OnlineOrderSaveData>();
            data.wholesaleTruckPackageIds ??= new List<string>();
            data.greenTruckPackageIds ??= new List<string>();
            if (data.activeDeliveryTruck != null)
            {
                data.activeDeliveryTruck.remainingPackageIds ??= new List<string>();
                data.activeDeliveryTruck.originalPackageIds ??= new List<string>();
            }
            data.stockMarket ??= new List<StockSaveItem>();
            data.transactionLog ??= new List<TransactionRecord>();
            data.bankLoans ??= new List<ActiveLoanData>();
            data.socialFeed ??= new List<SocialTweetData>();
            data.ownedSeeds ??= new List<OwnedSeedSaveData>();
            data.barnCrops ??= new List<BarnCropSaveData>();
            data.workshopCrops ??= new List<BarnCropSaveData>();
            data.workshopMachines ??= new List<WorkshopMachineSaveData>();
            data.pendingWorkshopMachineBoxes ??= new List<string>();
            data.pendingDeliveryBoxes ??= new List<string>();
            data.staffList ??= new List<StaffSaveData>();
            data.farmStaffList ??= new List<StaffSaveData>();
            data.courierStaffList ??= new List<StaffSaveData>();
            data.fieldCrops ??= new List<CropSaveData>();
            data.furnitureList ??= new List<ShelfSaveData>();
            data.saveFormatVersion = CURRENT_SAVE_FORMAT_VERSION;
        }

        private static List<WholesaleProductDef> ResolveProductIds(IEnumerable<string> productIds)
        {
            List<WholesaleProductDef> products = new List<WholesaleProductDef>();
            if (productIds == null) return products;
            foreach (string productId in productIds)
            {
                WholesaleProductDef product = WholesaleDatabase.GetProductById(productId);
                if (product != null) products.Add(product);
            }
            return products;
        }

        private static void RestorePendingTruckDeliveries(SaveGameData data)
        {
            bool hasLeftoverPose = DeliveryTruckVisuals.TryReadLeftoverTruckPose(out Vector3 leftoverPos, out Quaternion leftoverRot);

            if (WholesaleTruckManager.Instance != null) WholesaleTruckManager.Instance.ClearAllPackages();
            if (GreenTruckDeliveryManager.Instance != null) GreenTruckDeliveryManager.Instance.ClearPendingDeliveries();

            DeliveryTruckSaveData snapshot = data.activeDeliveryTruck;
            if (snapshot != null && snapshot.isActive)
            {
                if (string.Equals(snapshot.truckKind, "Green", System.StringComparison.OrdinalIgnoreCase))
                {
                    GreenTruckDeliveryManager.Instance?.RestoreFromSave(snapshot);
                }
                else
                {
                    WholesaleTruckManager.Instance?.RestoreFromSave(snapshot);
                }
                return;
            }

            List<WholesaleProductDef> wholesalePackages = ResolveProductIds(data.wholesaleTruckPackageIds);
            List<WholesaleProductDef> greenPackages = ResolveProductIds(data.greenTruckPackageIds);

            if (wholesalePackages.Count > 0 && WholesaleTruckManager.Instance != null)
            {
                if (hasLeftoverPose)
                {
                    WholesaleTruckManager.Instance.RestoreFromSave(BuildLegacyTruckSnapshot("Wholesale", leftoverPos, leftoverRot, wholesalePackages));
                }
                else
                {
                    WholesaleTruckManager.Instance.DispatchWholesaleDelivery(wholesalePackages);
                }
                return;
            }

            if (greenPackages.Count > 0 && GreenTruckDeliveryManager.Instance != null)
            {
                if (hasLeftoverPose)
                {
                    GreenTruckDeliveryManager.Instance.RestoreFromSave(BuildLegacyTruckSnapshot("Green", leftoverPos, leftoverRot, greenPackages));
                }
                else
                {
                    GreenTruckDeliveryManager.Instance.DispatchFarmDelivery(greenPackages);
                }
                return;
            }

            if (hasLeftoverPose && WholesaleTruckManager.Instance != null)
            {
                WholesaleTruckManager.Instance.RestoreFromSave(BuildLegacyTruckSnapshot("Wholesale", leftoverPos, leftoverRot, new List<WholesaleProductDef>()));
            }
        }

        private static DeliveryTruckSaveData BuildLegacyTruckSnapshot(
            string truckKind,
            Vector3 position,
            Quaternion rotation,
            List<WholesaleProductDef> packages)
        {
            Vector3 euler = rotation.eulerAngles;
            DeliveryTruckSaveData snapshot = new DeliveryTruckSaveData
            {
                isActive = true,
                truckKind = truckKind,
                phase = DeliveryTruckVisuals.InferPhase(position).ToString(),
                posX = position.x,
                posY = position.y,
                posZ = position.z,
                rotX = euler.x,
                rotY = euler.y,
                rotZ = euler.z,
                doorsOpen = position.z > -3.5f
            };

            if (packages != null)
            {
                foreach (var package in packages)
                {
                    if (package == null) continue;
                    snapshot.remainingPackageIds.Add(package.id);
                    snapshot.originalPackageIds.Add(package.id);
                }
            }

            return snapshot;
        }

        private void HandleMidnightAutosave()
        {
            StartCoroutine(AutosaveAtEndOfFrame());
        }

        private IEnumerator AutosaveAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            TryAutosave();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) TryAutosave();
        }

        private void TryAutosave()
        {
            if (activeSessionSlot < 1 || activeSessionSlot > 3) return;
            if (Time.realtimeSinceStartup - lastAutosaveTime < AUTOSAVE_DEBOUNCE_SECONDS) return;
            SaveCurrentGame(activeSessionSlot);
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMidnightRollover -= HandleMidnightAutosave;
            }
            if (Instance == this) Instance = null;
        }
    }
}
