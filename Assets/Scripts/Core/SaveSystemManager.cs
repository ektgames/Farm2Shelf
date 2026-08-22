using System;
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
        /// O anki OYUNUN TÜM DURUMUNU (15 Ana Sistem) eksiksiz olarak belirtilen slota kaydeder.
        /// </summary>
        public bool SaveCurrentGame(int slotIndex)
        {
            if (slotIndex < 1 || slotIndex > 3) slotIndex = 1;

            SaveGameData saveData = new SaveGameData
            {
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
                saveData.playerMoney = 400000;
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
            }

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

            // 15. EĞİTİM ADIMI (TUTORIAL PROGRESS)
            if (TutorialManager.Instance != null)
            {
                saveData.tutorialStep = TutorialManager.Instance.CurrentStep.ToString();
            }

            try
            {
                string json = JsonUtility.ToJson(saveData, true);
                string key = SAVE_SLOT_PREFIX + slotIndex;
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.SetInt("Farm2Shelf_LastPlayedSlot", slotIndex);
                PlayerPrefs.Save();

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
            }

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

                if (saveData.isStoreOpen && saveData.gameHour < 24)
                {
                    StoreStatusManager.Instance.OpenStore();
                }
                else
                {
                    StoreStatusManager.Instance.CloseStore();
                }
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

                // Gece 24:00 (12:00 AM) veya dükkan kapalı durumunda zaman akışını KESİNLİKLE duraklat
                if (saveData.gameHour >= 24 || (saveData.gameHour == 0 && !saveData.isStoreOpen) || saveData.isTimePaused || !saveData.isStoreOpen)
                {
                    TimeManager.Instance.SetTimePaused(true);
                }
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

            Debug.Log($"[SaveSystemManager] Slot {slotIndex} EKSİKSİZ YÜKLENDİ! Bakiye: {saveData.playerMoney}C | Borsa: {saveData.stockMarket?.Count} | Finans: {saveData.transactionLog?.Count} | Mobilya: {saveData.furnitureList?.Count} | Personel: {saveData.staffList?.Count}");
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

        public int GetLastPlayedSlot()
        {
            return PlayerPrefs.GetInt("Farm2Shelf_LastPlayedSlot", 1);
        }

        public bool HasLastPlayedSlot()
        {
            int slot = PlayerPrefs.GetInt("Farm2Shelf_LastPlayedSlot", 0);
            return slot >= 1 && slot <= 3 && !GetSlotData(slot).isEmptySlot;
        }
    }
}
