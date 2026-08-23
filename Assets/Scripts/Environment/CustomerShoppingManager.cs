using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.UI;

namespace Farm2Shelf.Environment
{
    public class CustomerShoppingManager : MonoBehaviour
    {
        public static CustomerShoppingManager Instance { get; private set; }

        private const float WALK_SPEED = 3.2f; // m/s (İnsan yürüme hızı)
        private float spawnTimer = 0f;
        private Transform customerParentGroup;

        private readonly List<ActiveCustomerData> activeCustomers = new List<ActiveCustomerData>();

        public int ActiveCustomerCount => activeCustomers.Count;
        private readonly List<CustomerType> customerDeck = new List<CustomerType>();
        private CustomerType lastSpawnedType = (CustomerType)(-1);

        public enum VehicleDrivePhase
        {
            None,
            DrivingToEntranceTurnstileApproach,
            PassingThroughEntranceTurnstile,
            NavigatingParkingAisle,
            ParkingInSlot,
            EngineOffShoppingOnFoot,
            ReversingOutSlot,
            DrivingToExitTurnstileApproach,
            PassingThroughExitTurnstile,
            DrivingAwayWest
        }

        private class ActiveCustomerData
        {
            public GameObject customerObj;
            public List<Transform> leftLimbs;
            public List<Transform> rightLimbs;
            public CustomerType type;
            public CustomerProfileData profileData;

            public List<Vector3> waypoints;
            public int currentWaypointIndex;
            public float walkCycleTimer;
            public float stateWaitTimer;

            public bool isShopping;
            public bool isCheckingOut;

            // Araçlı Müşteri Verileri
            public bool hasVehicle;
            public GameObject vehicleObj;
            public List<Transform> vehicleWheels;
            public int parkingSlotIndex = -1;
            public Vector3 parkedSlotPos;
            public float vehicleSpeed;
            public VehicleDrivePhase drivePhase;

            // Müşteri Hizmetleri Danışma Etkileşimi
            public bool visitedCustomerServiceDesk;
            public bool isVisitingCustomerService;

            // Alışveriş Sepeti Sistemi Verileri
            public bool hasShoppingCart;
            public GameObject carriedCartObj;
            public bool hasCartStand;
            public bool hasNoCartWarningShown;
            public bool grabbedCartFromStand;

            // Kasa Kuyruk Sistemi Verileri
            public PlacedFurnitureController assignedCashier;
            public int queueSlotIndex = -1;
            public bool isInCashierQueue;

            // Çoklu Raf & Çeşitli Ürün Alışveriş Takip Verileri
            public HashSet<PlacedFurnitureController> visitedShelvesSet = new HashSet<PlacedFurnitureController>();
            public int totalCartValue;
            public int totalItemsBought;

            // Anti-Stuck Takılma Koruyucusu
            public Vector3 lastTrackedPos;
            public float stuckTimer;

            // Kasiyer Yok Uyarısı Takibi
            public float noCashierWarningTimer;
            public GameObject activeNoCashierPopup;

            // Ödeme Tamamlandı & Çıkış Yapıyor Bayrağı (Tekrar kasaya girmeyi veya takılmayı %100 önler)
            public bool hasPaidAndExiting;
        }

        private readonly bool[] occupiedParkingSlots = new bool[30]; // Seviye 1 (10), Seviye 2 (16), Seviye 3 (22) araç kapasitesi
        private ParkingBarrierTurnstile entranceBarrier;
        private ParkingBarrierTurnstile exitBarrier;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ClearAllCustomers()
        {
            for (int i = activeCustomers.Count - 1; i >= 0; i--)
            {
                var c = activeCustomers[i];
                if (c != null)
                {
                    if (c.customerObj != null) Destroy(c.customerObj);
                    if (c.vehicleObj != null) Destroy(c.vehicleObj);
                    if (c.activeNoCashierPopup != null) Destroy(c.activeNoCashierPopup);
                }
            }
            activeCustomers.Clear();
            for (int i = 0; i < occupiedParkingSlots.Length; i++)
            {
                occupiedParkingSlots[i] = false;
            }
            spawnTimer = 0f;
        }

        private void OnEnable()
        {
            EnvironmentBuilder.OnStoreUpgraded -= HandleStoreUpgraded;
            EnvironmentBuilder.OnStoreUpgraded += HandleStoreUpgraded;
        }

        private void OnDisable()
        {
            EnvironmentBuilder.OnStoreUpgraded -= HandleStoreUpgraded;
        }

        private void HandleStoreUpgraded(int newLevel)
        {
            FindParkingBarriers();
        }

        private void Start()
        {
            GameObject grpObj = new GameObject("Customer_AI_Group");
            grpObj.transform.SetParent(transform);
            customerParentGroup = grpObj.transform;

            FindParkingBarriers();
        }

        private bool IsCustomerServiceStaffWorking()
        {
            if (StaffManager.Instance == null || TimeManager.Instance == null) return false;
            int curHour = TimeManager.Instance.Hour;
            var staffList = StaffManager.Instance.GetActiveStaff();
            foreach (var s in staffList)
            {
                if (s != null && s.role == StaffRole.MüşteriHizmetlisi && s.isActive)
                {
                    if (IsShiftActive(s.shiftHours, curHour)) return true;
                }
            }
            return false;
        }

        private bool IsShiftActive(string shift, int currentHour)
        {
            if (string.IsNullOrEmpty(shift)) return true;
            if (shift.Contains("Sabah") || shift.Contains("Gündüz") || shift.Contains("08:00") || shift.Contains("06:00"))
            {
                return (currentHour >= 8 && currentHour < 16);
            }
            if (shift.Contains("Akşam") || shift.Contains("16:00") || shift.Contains("14:00") || shift.Contains("Gece") || shift.Contains("22:00"))
            {
                return (currentHour >= 16 && (currentHour < 24 || activeCustomers.Count > 0));
            }
            return true;
        }

        private void FindParkingBarriers()
        {
            ParkingBarrierTurnstile[] barriers = Object.FindObjectsByType<ParkingBarrierTurnstile>(FindObjectsSortMode.None);
            foreach (var b in barriers)
            {
                if (b == null) continue;
                if (b.gameObject.name.Contains("Entrance")) entranceBarrier = b;
                else if (b.gameObject.name.Contains("Exit")) exitBarrier = b;
            }
        }

        private int GetAvailableParkingSlot(out Vector3 slotPos)
        {
            slotPos = Vector3.zero;
            int level = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            int totalSlotsCount = (level == 1) ? 10 : ((level == 2) ? 16 : 22);
            int slotsPerRow = totalSlotsCount / 2;

            float totalZLength = (level == 1) ? 20.0f : ((level == 2) ? 29.0f : 38.0f);
            float slotWidthX = 5.2f;
            float zPitch = 2.8f;
            float topPZ = (-3.0f + totalZLength) - 2.8f;

            for (int i = 0; i < totalSlotsCount; i++)
            {
                if (!occupiedParkingSlots[i])
                {
                    if (i < slotsPerRow)
                    {
                        // Sol Sütun (P1..P5 Seviye 1 / P1..P8 Seviye 2 / P1..P11 Seviye 3)
                        float pz = 1.0f + (i * zPitch);
                        float px = -39.0f + (slotWidthX / 2f); // -36.4m
                        slotPos = new Vector3(px, 0.05f, pz);
                    }
                    else
                    {
                        // Sağ Sütun (P6..P10 Seviye 1 / P9..P16 Seviye 2 / P12..P22 Seviye 3)
                        int rightIndex = i - slotsPerRow;
                        float pz = topPZ - ((slotsPerRow - 1 - rightIndex) * zPitch);
                        float px = -15.0f - (slotWidthX / 2f); // -17.6m
                        slotPos = new Vector3(px, 0.05f, pz);
                    }
                    return i;
                }
            }
            return -1;
        }



        private void Update()
        {
            // Dükkan Açık mı ve Saat 08:00 veya sonrası mı Kontrol Et (Müşteriler en erken sabah 08:00'da gelir!)
            bool isStoreOpen = (StoreStatusManager.Instance != null && StoreStatusManager.Instance.IsOpen);
            int currentHour = TimeManager.Instance != null ? TimeManager.Instance.Hour : 8;

            if (isStoreOpen && currentHour >= 8)
            {
                // Müşteriler sabah 08:00'da gelmeye başlar ve saat dilimine göre yoğunlaşır
                bool hasTraffic = GetHourlyCustomerTrafficConfig(currentHour, out float spawnInterval, out int maxActiveLimit);

                if (hasTraffic)
                {
                    spawnTimer += Time.deltaTime;
                    if (spawnTimer >= spawnInterval)
                    {
                        spawnTimer = 0f;
                        if (activeCustomers.Count < maxActiveLimit)
                        {
                            TrySpawnCustomer();
                        }
                    }
                }
            }

            UpdateActiveCustomers(Time.deltaTime);
        }

        private bool GetHourlyCustomerTrafficConfig(int hour, out float spawnInterval, out int maxActiveCustomers)
        {
            // 00:00 - 07:59 (Sabah 08:00'dan önce kesinlikle müşteri gelmez - Dükkan hazırlık & temizlik saatleri)
            if (hour < 8)
            {
                spawnInterval = 9999f;
                maxActiveCustomers = 0;
                return false;
            }
            // 08:00 - 09:59 (Sabah Açılışı - İşe & Okula Gidiş Yoğunluğu)
            else if (hour >= 8 && hour < 10)
            {
                spawnInterval = 4.5f;
                maxActiveCustomers = 8;
                return true;
            }
            // 10:00 - 11:59 (Kuşluk Vakti - Standart Akış)
            else if (hour >= 10 && hour < 12)
            {
                spawnInterval = 7.0f;
                maxActiveCustomers = 6;
                return true;
            }
            // 12:00 - 13:59 (Öğle Molası Alışveriş Zirvesi - YOĞUN)
            else if (hour >= 12 && hour < 14)
            {
                spawnInterval = 3.8f;
                maxActiveCustomers = 10;
                return true;
            }
            // 14:00 - 16:59 (Öğleden Sonra - Normal Düzey)
            else if (hour >= 14 && hour < 17)
            {
                spawnInterval = 6.5f;
                maxActiveCustomers = 7;
                return true;
            }
            // 17:00 - 20:59 (🔥 MESAİ ÇIKIŞI ZİRVE YOĞUNLUĞU - PEAK RUSH HOUR 🔥)
            else if (hour >= 17 && hour < 21)
            {
                spawnInterval = 2.5f; // Hızlı gelen müşteri akını!
                maxActiveCustomers = 14; // Mağaza tıklım tıklım!
                return true;
            }
            // 21:00 - 22:59 (Akşam Sonu Yavaşlayan Akış)
            else if (hour >= 21 && hour < 23)
            {
                spawnInterval = 9.0f;
                maxActiveCustomers = 5;
                return true;
            }
            // 23:00 - 23:59 (Gece Kapanış Sakinliği)
            else if (hour >= 23 && hour < 24)
            {
                spawnInterval = 15.0f;
                maxActiveCustomers = 2;
                return true;
            }
            // 24:00+ (Dükkan Kapalı)
            else
            {
                spawnInterval = 9999f;
                maxActiveCustomers = 0;
                return false;
            }
        }

        public static int GetCustomerTier(CustomerType type)
        {
            string name = type.ToString();
            if (name.StartsWith("L3_")) return 3;
            if (name.StartsWith("L2_")) return 2;
            return 1;
        }

        public static int GetProductLevel(string productName)
        {
            if (string.IsNullOrEmpty(productName) || productName == "Boş" || productName.StartsWith("Ürün"))
                return 1;

            // 1. Toptan Ürün Veritabanı Kontrolü
            var prodList = WholesaleDatabase.GetAllProducts();
            if (prodList != null)
            {
                for (int i = 0; i < prodList.Count; i++)
                {
                    var p = prodList[i];
                    if (p == null) continue;
                    if (string.Equals(p.name, productName, System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.nameEn, productName, System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.id, productName, System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.LocalizedName, productName, System.StringComparison.OrdinalIgnoreCase) ||
                        productName.IndexOf(p.name, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        p.name.IndexOf(productName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return p.requiredLevel;
                    }
                }
            }

            // 2. Çiftlik / Hasat Mahsulleri Veritabanı Kontrolü
            var seedList = GardenSeedDatabase.GetAllSeeds();
            if (seedList != null)
            {
                for (int i = 0; i < seedList.Count; i++)
                {
                    var s = seedList[i];
                    if (s == null) continue;
                    string cropName = s.name.Replace(" Tohumu", "").Replace(" Seeds", "").Trim();
                    if (string.Equals(s.name, productName, System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s.nameEn, productName, System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cropName, productName, System.StringComparison.OrdinalIgnoreCase) ||
                        productName.IndexOf(cropName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return s.requiredLevel;
                    }
                }
            }

            return 1;
        }

        public static bool IsProductMatchingCustomerTier(string productName, int customerTier)
        {
            int productLevel = GetProductLevel(productName);
            if (customerTier == 1)
            {
                // 1. Seviye müşterisi SADECE 1. seviye ürünleri talep eder ve alır
                return productLevel == 1;
            }
            else if (customerTier == 2)
            {
                // 2. Seviye müşteri grubu 2. seviye ürünleri (ve 1. seviyeyi) talep eder
                return productLevel <= 2;
            }
            else // Tier 3
            {
                // 3. Seviye müşteri grubu 3. seviye ürünleri (ve 1-2'yi) talep eder
                return productLevel <= 3;
            }
        }

        private CustomerType GetNextUniqueCustomerType(int currentLevel)
        {
            if (customerDeck.Count == 0)
            {
                List<CustomerType> available = new List<CustomerType>
                {
                    CustomerType.L1_StudentGirl,
                    CustomerType.L1_CasualBoy,
                    CustomerType.L1_NeighborhoodMom,
                    CustomerType.L1_GrandpaDede,
                    CustomerType.L1_FarmerUncle,
                    CustomerType.L1_GrandmaTeyze,
                    CustomerType.L1_SportsMan,
                    CustomerType.L1_BakeryCustomer,
                    CustomerType.L1_Workman,
                    CustomerType.L1_VillageGirl
                };

                if (currentLevel >= 2)
                {
                    available.Add(CustomerType.L2_OfficeWorker);
                    available.Add(CustomerType.L2_HipsterGuy);
                    available.Add(CustomerType.L2_GymBro);
                    available.Add(CustomerType.L2_DoctorWoman);
                    available.Add(CustomerType.L2_FashionWoman);
                    available.Add(CustomerType.L2_DeliveryCourier);
                    available.Add(CustomerType.L2_BusinessWoman);
                    available.Add(CustomerType.L2_ArtistGirl);
                    available.Add(CustomerType.L2_TechNerd);
                    available.Add(CustomerType.L2_TouristGuy);
                }

                if (currentLevel >= 3)
                {
                    available.Add(CustomerType.L3_CEO_Executive);
                    available.Add(CustomerType.L3_VIP_Influencer);
                    available.Add(CustomerType.L3_RichGentleman);
                    available.Add(CustomerType.L3_BoutiqueLady);
                    available.Add(CustomerType.L3_GamerPro);
                    available.Add(CustomerType.L3_CelebrityActor);
                    available.Add(CustomerType.L3_PilotMan);
                    available.Add(CustomerType.L3_GoldChainRapper);
                    available.Add(CustomerType.L3_JewelryLady);
                    available.Add(CustomerType.L3_BillionaireYacht);
                }

                while (available.Count > 0)
                {
                    int idx = Random.Range(0, available.Count);
                    customerDeck.Add(available[idx]);
                    available.RemoveAt(idx);
                }
            }

            CustomerType selected = customerDeck[0];
            customerDeck.RemoveAt(0);

            if (selected == lastSpawnedType && customerDeck.Count > 0)
            {
                CustomerType alt = customerDeck[0];
                customerDeck[0] = selected;
                selected = alt;
            }

            lastSpawnedType = selected;
            return selected;
        }

        private void TrySpawnCustomer()
        {
            // Sabah 08:00'dan önce kesinlikle müşteri üretilemez!
            if (TimeManager.Instance != null && TimeManager.Instance.Hour < 8) return;
            if (StoreStatusManager.Instance == null || !StoreStatusManager.Instance.IsOpen) return;

            int currentLevel = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            CustomerType selectedType = GetNextUniqueCustomerType(currentLevel);

            int slotIdx = GetAvailableParkingSlot(out Vector3 slotPos);
            bool spawnCarCustomer = (slotIdx >= 0 && Random.value < 0.40f);

            if (spawnCarCustomer)
            {
                occupiedParkingSlots[slotIdx] = true;
                SpawnVehicleCustomer(slotIdx, slotPos, selectedType);
            }
            else
            {
                SpawnPedestrianCustomer(selectedType);
            }
        }

        private void SpawnPedestrianCustomer(CustomerType selectedType)
        {
            List<Vector3> route = BuildCustomerShoppingRoute(out bool willVisitDesk, out bool hasCartStand, selectedType);
            if (route == null || route.Count < 2) return;

            GameObject custObj = ProceduralCustomerModelBuilder.CreateCustomerModel(selectedType, out List<Transform> leftLimbs, out List<Transform> rightLimbs);
            custObj.transform.SetParent(customerParentGroup, false);

            Vector3 startPos = route[0];
            custObj.transform.position = startPos;

            CustomerProfileData profile = CustomerProfileGenerator.GenerateProfile(selectedType);
            CustomerClickableTarget target = custObj.AddComponent<CustomerClickableTarget>();
            target.profileData = profile;

            ActiveCustomerData cData = new ActiveCustomerData
            {
                customerObj = custObj,
                leftLimbs = leftLimbs,
                rightLimbs = rightLimbs,
                type = selectedType,
                profileData = profile,
                waypoints = route,
                currentWaypointIndex = 1,
                walkCycleTimer = Random.Range(0f, 5f),
                stateWaitTimer = 0f,
                isShopping = false,
                isCheckingOut = false,
                hasVehicle = false,
                drivePhase = VehicleDrivePhase.None,
                isVisitingCustomerService = willVisitDesk,
                visitedCustomerServiceDesk = false,
                hasCartStand = hasCartStand
            };

            activeCustomers.Add(cData);
        }

        public void SpawnSingleBusPassenger(Vector3 disembarkPos)
        {
            // Sabah 08:00'dan önce kesinlikle yolcu müşteri üretilemez!
            if (TimeManager.Instance != null && TimeManager.Instance.Hour < 8) return;
            if (StoreStatusManager.Instance == null || !StoreStatusManager.Instance.IsOpen) return;

            int currentLevel = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            CustomerType selectedType = GetNextUniqueCustomerType(currentLevel);

            List<Vector3> route = BuildBusPassengerShoppingRoute(disembarkPos, out bool willVisitDesk, out bool hasCartStand, selectedType);
            if (route == null || route.Count < 2) return;

            GameObject custObj = ProceduralCustomerModelBuilder.CreateCustomerModel(selectedType, out List<Transform> leftLimbs, out List<Transform> rightLimbs);
            custObj.transform.SetParent(customerParentGroup, false);
            custObj.transform.position = disembarkPos;

            CustomerProfileData profile = CustomerProfileGenerator.GenerateProfile(selectedType);
            CustomerClickableTarget target = custObj.AddComponent<CustomerClickableTarget>();
            target.profileData = profile;

            ActiveCustomerData cData = new ActiveCustomerData
            {
                customerObj = custObj,
                leftLimbs = leftLimbs,
                rightLimbs = rightLimbs,
                type = selectedType,
                profileData = profile,
                waypoints = route,
                currentWaypointIndex = 1,
                walkCycleTimer = Random.Range(0f, 5f),
                stateWaitTimer = 0f,
                isShopping = false,
                isCheckingOut = false,
                hasVehicle = false,
                drivePhase = VehicleDrivePhase.None,
                isVisitingCustomerService = willVisitDesk,
                visitedCustomerServiceDesk = false,
                hasCartStand = hasCartStand
            };

            activeCustomers.Add(cData);
        }

        private bool IsShelfStockedForCustomer(PlacedFurnitureController f, int customerTier)
        {
            if (f == null || f.rows == null) return false;
            bool isStoreShelf = (f.FurnitureType == FurnitureType.Shelf || f.FurnitureType == FurnitureType.Fridge || f.FurnitureType == FurnitureType.Freezer || f.FurnitureType == FurnitureType.BakeryCounter || f.FurnitureType == FurnitureType.ProduceShelf || f.FurnitureType == FurnitureType.CosmeticShelf || f.FurnitureType == FurnitureType.ElectronicsShelf || f.FurnitureType == FurnitureType.ButcherCounter);
            if (!isStoreShelf) return false;

            foreach (var r in f.rows)
            {
                if (r != null && !r.IsUnassigned && !string.IsNullOrEmpty(r.productName) && r.currentStock > 0)
                {
                    if (IsProductMatchingCustomerTier(r.productName, customerTier))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool HasAnyStockedShelfForCustomer(int customerTier)
        {
            var allFurniture = PlacedFurnitureController.AllPlacedFurniture;
            int count = allFurniture.Count;
            for (int i = 0; i < count; i++)
            {
                var f = allFurniture[i];
                if (f != null && IsShelfStockedForCustomer(f, customerTier))
                {
                    return true;
                }
            }
            return false;
        }

        private int GetMaxProductLevelOnShelf(PlacedFurnitureController shelf)
        {
            if (shelf == null || shelf.rows == null) return 1;
            int maxLvl = 1;
            foreach (var r in shelf.rows)
            {
                if (r != null && !r.IsEmpty && r.currentStock > 0)
                {
                    int lvl = GetProductLevel(r.productName);
                    if (lvl > maxLvl) maxLvl = lvl;
                }
            }
            return maxLvl;
        }

        private PlacedFurnitureController FindAlternateStockedShelfForCustomer(ActiveCustomerData cData, int customerTier)
        {
            var allFurniture = PlacedFurnitureController.AllPlacedFurniture;
            int count = allFurniture.Count;
            for (int i = 0; i < count; i++)
            {
                var f = allFurniture[i];
                if (f != null && IsShelfStockedForCustomer(f, customerTier))
                {
                    if (cData.visitedShelvesSet == null || !cData.visitedShelvesSet.Contains(f))
                    {
                        return f;
                    }
                }
            }
            return null;
        }

        private void AddRandomShelfWaypoints(List<Vector3> route, IList<PlacedFurnitureController> shelves, int customerTier)
        {
            List<PlacedFurnitureController> validShelves = new List<PlacedFurnitureController>();
            if (shelves != null)
            {
                int sCount = shelves.Count;
                for (int i = 0; i < sCount; i++)
                {
                    var s = shelves[i];
                    if (s != null && IsShelfStockedForCustomer(s, customerTier))
                    {
                        validShelves.Add(s);
                    }
                }
            }

            if (validShelves.Count > 0)
            {
                // Seviyeye öncelik ver (Müşterinin kendi seviyesindeki ürün raflarını başa al)
                validShelves.Sort((a, b) =>
                {
                    int aMaxLvl = GetMaxProductLevelOnShelf(a);
                    int bMaxLvl = GetMaxProductLevelOnShelf(b);
                    int aScore = (aMaxLvl == customerTier) ? 10 : (aMaxLvl < customerTier ? aMaxLvl : 0);
                    int bScore = (bMaxLvl == customerTier) ? 10 : (bMaxLvl < customerTier ? bMaxLvl : 0);
                    return bScore.CompareTo(aScore);
                });

                // Kendi seviyesi içinde karıştır (Fisher-Yates Shuffle)
                for (int i = 0; i < validShelves.Count; i++)
                {
                    var temp = validShelves[i];
                    int randIdx = Random.Range(i, validShelves.Count);
                    validShelves[i] = validShelves[randIdx];
                    validShelves[randIdx] = temp;
                }

                // Oyundaki Yoğunluk Saatlerine Göre Mantıklı Ürün Alışveriş Adet Dağılımı:
                int currentHour = (TimeManager.Instance != null) ? TimeManager.Instance.Hour : 14;
                int targetCount = 1;
                float roll = Random.value;

                if (currentHour >= 16 && currentHour <= 20)
                {
                    if (roll < 0.15f) targetCount = 5;
                    else if (roll < 0.35f) targetCount = 6;
                    else if (roll < 0.60f) targetCount = 7;
                    else if (roll < 0.80f) targetCount = 8;
                    else targetCount = 9;
                }
                else if (currentHour >= 11 && currentHour < 16)
                {
                    if (roll < 0.25f) targetCount = 3;
                    else if (roll < 0.60f) targetCount = 4;
                    else if (roll < 0.85f) targetCount = 5;
                    else targetCount = 6;
                }
                else
                {
                    if (roll < 0.30f) targetCount = 1;
                    else if (roll < 0.65f) targetCount = 2;
                    else if (roll < 0.90f) targetCount = 3;
                    else targetCount = 4;
                }

                targetCount = Mathf.Clamp(targetCount, 1, validShelves.Count);

                for (int i = 0; i < targetCount; i++)
                {
                    Vector3 shelfFrontPos = validShelves[i].GetFrontInteractionPosition(0.75f);
                    route.Add(shelfFrontPos);
                }
            }
        }

        private List<Vector3> BuildCustomerShoppingRoute(out bool willVisitServiceDesk, out bool hasCartStand, CustomerType selectedType)
        {
            List<Vector3> route = new List<Vector3>();
            int customerTier = GetCustomerTier(selectedType);

            // Yaya Giriş Rotası: Sağ Doğu Kaldırımı Spawn Noktası (X=45.0f, Z=-5.0f) -> Cam Kapı Önü Dış Kaldırım (-5.0, -5.0) -> Kapıdan Geçiş (-5.0, -2.5) -> Ana Fuaye (-5.0, -0.5)
            route.Add(new Vector3(45.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-5.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-5.0f, 0.05f, -2.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -0.5f));

            PlacedFurnitureController cartStand = GetActiveShoppingCartStand();
            hasCartStand = (cartStand != null);

            if (hasCartStand)
            {
                route.Add(cartStand.GetFrontInteractionPosition(1.0f));
            }
            else
            {
                // Sepet stantı yoksa kapıdan geri dönüp sol despawn noktasına yürür
                route.Add(new Vector3(-5.0f, 0.05f, -0.5f));
                route.Add(new Vector3(-5.0f, 0.05f, -2.5f));
                route.Add(new Vector3(-5.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-17.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-45.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-85.0f, 0.05f, -5.0f));
                willVisitServiceDesk = false;
                return route;
            }

            var shelves = PlacedFurnitureController.AllPlacedFurniture;
            PlacedFurnitureController serviceDesk = null;

            int shCount = shelves.Count;
            for (int i = 0; i < shCount; i++)
            {
                var s = shelves[i];
                if (s != null && s.FurnitureType == FurnitureType.CustomerServiceDesk)
                {
                    serviceDesk = s;
                    break;
                }
            }

            willVisitServiceDesk = (serviceDesk != null && IsCustomerServiceStaffWorking() && Random.value < 0.45f);
            if (willVisitServiceDesk)
            {
                route.Add(serviceDesk.GetFrontInteractionPosition(1.0f));
            }

            // Müşteri Mağazadaki Raflara SADECE Kendi Seviyesine Uygun Stokta Ürün Varsa Uğrar:
            if (HasAnyStockedShelfForCustomer(customerTier))
            {
                AddRandomShelfWaypoints(route, shelves, customerTier);

                Vector3 checkoutPos = new Vector3(-6.5f, 0.05f, 1.5f);
                for (int i = 0; i < shCount; i++)
                {
                    var s = shelves[i];
                    if (s != null && s.FurnitureType == FurnitureType.Cashier)
                    {
                        checkoutPos = s.GetFrontInteractionPosition(1.0f);
                        break;
                    }
                }
                route.Add(checkoutPos);
            }

            // Çıkış Rotası: Fuaye -> Kapı -> Dış Kaldırım -> Batı Despawn (X = -85.0f)
            route.Add(new Vector3(-5.0f, 0.05f, -0.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -2.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-17.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-45.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-85.0f, 0.05f, -5.0f));

            return route;
        }

        private List<Vector3> BuildBusPassengerShoppingRoute(Vector3 disembarkPos, out bool willVisitServiceDesk, out bool hasCartStand, CustomerType selectedType)
        {
            List<Vector3> route = new List<Vector3>();
            int customerTier = GetCustomerTier(selectedType);

            // 1. Geliş Rotası: Otobüsten İn (4.5, -5.0) -> Dış Kaldırım (-5.0, -5.0) -> Kapıdan Geçiş (-5.0, -2.5) -> Ana Fuaye (-5.0, -0.5)
            route.Add(disembarkPos);
            route.Add(new Vector3(-5.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-5.0f, 0.05f, -2.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -0.5f));

            PlacedFurnitureController cartStand = GetActiveShoppingCartStand();
            hasCartStand = (cartStand != null);

            if (hasCartStand)
            {
                route.Add(cartStand.GetFrontInteractionPosition(1.0f));
            }
            else
            {
                route.Add(new Vector3(-5.0f, 0.05f, -0.5f));
                route.Add(new Vector3(-5.0f, 0.05f, -2.5f));
                route.Add(new Vector3(-5.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-17.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-45.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-85.0f, 0.05f, -5.0f));
                willVisitServiceDesk = false;
                return route;
            }

            var shelves = PlacedFurnitureController.AllPlacedFurniture;
            PlacedFurnitureController serviceDesk = null;

            int busShCount = shelves.Count;
            for (int i = 0; i < busShCount; i++)
            {
                var s = shelves[i];
                if (s != null && s.FurnitureType == FurnitureType.CustomerServiceDesk)
                {
                    serviceDesk = s;
                    break;
                }
            }

            willVisitServiceDesk = (serviceDesk != null && IsCustomerServiceStaffWorking() && Random.value < 0.45f);
            if (willVisitServiceDesk)
            {
                route.Add(serviceDesk.GetFrontInteractionPosition(1.0f));
            }

            if (HasAnyStockedShelfForCustomer(customerTier))
            {
                AddRandomShelfWaypoints(route, shelves, customerTier);

                Vector3 checkoutPos = new Vector3(-6.5f, 0.05f, 1.5f);
                for (int i = 0; i < busShCount; i++)
                {
                    var s = shelves[i];
                    if (s != null && s.FurnitureType == FurnitureType.Cashier)
                    {
                        checkoutPos = s.GetFrontInteractionPosition(1.0f);
                        break;
                    }
                }
                route.Add(checkoutPos);
            }

            // ÇIKIŞ ROTASI: Fuaye -> Kapı -> Dış Kaldırım -> Batı Despawn (X = -85.0f)
            route.Add(new Vector3(-5.0f, 0.05f, -0.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -2.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-17.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-45.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-85.0f, 0.05f, -5.0f));

            return route;
        }

        private void SpawnVehicleCustomer(int slotIndex, Vector3 slotPos, CustomerType selectedType)
        {
            VehicleType carType = (VehicleType)Random.Range((int)VehicleType.SedanRed, (int)VehicleType.ConvertibleCyan + 1);
            GameObject carObj = ProceduralCarModelBuilder.CreateVehicleModel(carType, out List<Transform> wheels);
            carObj.transform.SetParent(customerParentGroup, false);

            Vector3 startCarPos = new Vector3(50.0f, 0.05f, -7.5f);
            carObj.transform.position = startCarPos;
            carObj.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            GameObject custObj = ProceduralCustomerModelBuilder.CreateCustomerModel(selectedType, out List<Transform> leftLimbs, out List<Transform> rightLimbs);
            custObj.transform.SetParent(customerParentGroup, false);
            custObj.transform.position = startCarPos;
            custObj.SetActive(false);

            CustomerProfileData profile = CustomerProfileGenerator.GenerateProfile(selectedType);
            CustomerClickableTarget target = custObj.AddComponent<CustomerClickableTarget>();
            target.profileData = profile;

            ActiveCustomerData cData = new ActiveCustomerData
            {
                customerObj = custObj,
                leftLimbs = leftLimbs,
                rightLimbs = rightLimbs,
                type = selectedType,
                profileData = profile,
                hasVehicle = true,
                vehicleObj = carObj,
                vehicleWheels = wheels,
                parkingSlotIndex = slotIndex,
                parkedSlotPos = slotPos,
                vehicleSpeed = 9.5f,
                drivePhase = VehicleDrivePhase.DrivingToEntranceTurnstileApproach,
                walkCycleTimer = Random.Range(0f, 5f),
                stateWaitTimer = 0f,
                isShopping = false,
                isCheckingOut = false,
                visitedCustomerServiceDesk = false
            };

            activeCustomers.Add(cData);
        }

        private PlacedFurnitureController GetActiveShoppingCartStand()
        {
            var furnitureList = PlacedFurnitureController.AllPlacedFurniture;
            int fCount = furnitureList.Count;
            for (int i = 0; i < fCount; i++)
            {
                var f = furnitureList[i];
                if (f != null && f.FurnitureType == FurnitureType.ShoppingCart)
                {
                    return f;
                }
            }
            return null;
        }

        private void CreateCarriedShoppingCartOnCustomer(ActiveCustomerData cData)
        {
            ClearCarriedCartOnCustomer(cData);
            if (cData == null || cData.customerObj == null) return;

            cData.carriedCartObj = new GameObject("Carried_Shopping_Basket");
            cData.carriedCartObj.transform.SetParent(cData.customerObj.transform, false);
            cData.carriedCartObj.transform.localPosition = new Vector3(0.32f, 0.70f, 0.35f);
            cData.carriedCartObj.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

            Material redMat = FurnitureModelBuilder.RedAccentMaterial;
            Material blackMat = FurnitureModelBuilder.BlackMaterial;

            // Kırmızı Alışveriş Sepeti Gövdesi
            GameObject basket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basket.name = "BasketBody";
            basket.transform.SetParent(cData.carriedCartObj.transform, false);
            basket.transform.localPosition = Vector3.zero;
            basket.transform.localScale = new Vector3(0.36f, 0.24f, 0.30f);
            if (redMat != null) basket.GetComponent<Renderer>().sharedMaterial = redMat;
            Collider c1 = basket.GetComponent<Collider>();
            if (c1 != null) Destroy(c1);

            // Siyah Kulp
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "BasketHandle";
            handle.transform.SetParent(cData.carriedCartObj.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0.14f, -0.12f);
            handle.transform.localScale = new Vector3(0.34f, 0.03f, 0.04f);
            if (blackMat != null) handle.GetComponent<Renderer>().sharedMaterial = blackMat;
            Collider c2 = handle.GetComponent<Collider>();
            if (c2 != null) Destroy(c2);

            // Sepet İçi Ürünler Grubu
            GameObject itemsGroup = new GameObject("ItemsInBasket");
            itemsGroup.transform.SetParent(cData.carriedCartObj.transform, false);
            itemsGroup.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            cData.hasShoppingCart = true;
        }

        private void AddProductItemToCarriedCart(ActiveCustomerData cData, string productName, int itemCount)
        {
            if (cData == null || cData.carriedCartObj == null) return;
            Transform itemsGroup = cData.carriedCartObj.transform.Find("ItemsInBasket");
            if (itemsGroup == null) return;

            int currentItemCount = itemsGroup.childCount;

            for (int i = 0; i < itemCount && currentItemCount + i < 8; i++)
            {
                int index = currentItemCount + i;
                float offsetX = ((index % 2) == 0 ? -0.07f : 0.07f);
                float offsetZ = ((index / 2) == 0 ? -0.06f : ((index / 2) == 1 ? 0f : 0.06f));
                float offsetY = 0.02f + (index / 4) * 0.06f;

                Vector3 localPos = new Vector3(offsetX, offsetY, offsetZ);
                Procedural3DProductBuilder.CreateBasketProduct3DMesh(itemsGroup, productName, localPos, index);
            }
        }

        private void ClearCarriedCartOnCustomer(ActiveCustomerData cData)
        {
            if (cData != null && cData.carriedCartObj != null)
            {
                Destroy(cData.carriedCartObj);
                cData.carriedCartObj = null;
            }
            if (cData != null) cData.hasShoppingCart = false;
        }

        private void ShowNoShoppingCartWarning(Vector3 pos)
        {
            GameObject popupObj = new GameObject("Popup_NoShoppingCart");
            popupObj.transform.position = pos + Vector3.up * 1.9f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340f, 60f);
            popupObj.transform.localScale = Vector3.one * 0.012f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = UIStyleUtility.GetGlobalFont(20);
            txt.text = LocalizationManager.L("Customer_NoCart", "🛒 Alışveriş Sepeti Yok!", "🛒 No Shopping Cart!");
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.95f, 0.25f, 0.25f);

            Destroy(popupObj, 2.5f);
        }

        private void ShowNoCashierWarning(ActiveCustomerData cData)
        {
            if (cData == null || cData.customerObj == null) return;
            if (cData.activeNoCashierPopup != null) return;

            GameObject popupObj = new GameObject("Popup_NoCashier");
            popupObj.transform.position = cData.customerObj.transform.position + Vector3.up * 2.05f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320f, 60f);
            popupObj.transform.localScale = Vector3.one * 0.012f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            UnityEngine.UI.Image bg = popupObj.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(320, 60, 28, new Color(0.18f, 0.08f, 0.08f, 0.85f));

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = UIStyleUtility.GetGlobalFont(20);
            txt.text = LocalizationManager.L("Customer_NoCashier", "⚠️ Kasiyer Yok!", "⚠️ No Cashier!");
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1.0f, 0.35f, 0.35f);

            UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            cData.activeNoCashierPopup = popupObj;
            Destroy(popupObj, 2.2f);
        }

        private void ShowNoProductsWarning(ActiveCustomerData cData)
        {
            if (cData == null || cData.customerObj == null) return;

            GameObject popupObj = new GameObject("Popup_NoProducts");
            popupObj.transform.position = cData.customerObj.transform.position + Vector3.up * 2.05f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360f, 60f);
            popupObj.transform.localScale = Vector3.one * 0.012f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            UnityEngine.UI.Image bg = popupObj.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = UIStyleUtility.CreateRoundedPillSprite(360, 60, 28, new Color(0.22f, 0.08f, 0.08f, 0.88f));

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = UIStyleUtility.GetGlobalFont(20);
            txt.text = LocalizationManager.L("Customer_NoProducts", "⚠️ Alabileceğim Ürün Yok!", "⚠️ No Products In Stock!");
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1.0f, 0.40f, 0.30f);

            UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Destroy(popupObj, 2.5f);
        }

        private bool HasAnyStockedShelfInStore()
        {
            return HasAnyStockedShelfForCustomer(3);
        }

        private List<Vector3> BuildDirectExitRoute(ActiveCustomerData cData)
        {
            cData.hasPaidAndExiting = true;
            List<Vector3> route = new List<Vector3>();
            route.Add(cData.customerObj.transform.position);
            route.Add(new Vector3(-5.0f, 0.05f, -0.5f)); // Dükkan İçi Fuaye
            route.Add(new Vector3(-5.0f, 0.05f, -2.5f)); // Cam Kapı Geçişi
            route.Add(new Vector3(-5.0f, 0.05f, -5.0f)); // Dış Kaldırım

            if (cData.hasVehicle)
            {
                int level = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
                int slotsPerRow = ((level == 1) ? 10 : ((level == 2) ? 16 : 22)) / 2;
                bool isLeftSlot = (cData.parkingSlotIndex < slotsPerRow);
                Vector3 driverDoor = cData.parkedSlotPos + (isLeftSlot ? Vector3.right * 1.5f : Vector3.left * 1.5f);

                route.Add(new Vector3(-17.0f, 0.05f, -5.0f)); // Turnike Yaya Geçidi
                route.Add(new Vector3(-17.0f, 0.05f, -0.5f)); // Turnike Geçiş Ara Noktası
                route.Add(new Vector3(-17.0f, 0.05f, 1.5f));  // Turnike Giriş Boğazı
                route.Add(new Vector3(-27.0f, 0.05f, 1.5f));  // Otopark Boğazı
                route.Add(new Vector3(-27.0f, 0.05f, cData.parkedSlotPos.z)); // Otopark Orta Yolu
                route.Add(driverDoor); // Sürücü Kapısı (Biniş)
            }
            else
            {
                route.Add(new Vector3(-17.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-45.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-85.0f, 0.05f, -5.0f)); // Despawn
            }

            return route;
        }

        private List<Vector3> BuildExitRoute(ActiveCustomerData cData, PlacedFurnitureController activeCashier)
        {
            cData.hasPaidAndExiting = true;
            List<Vector3> exitWaypoints = new List<Vector3>();
            Vector3 startPos = cData.customerObj.transform.position;
            exitWaypoints.Add(startPos);

            if (activeCashier != null)
            {
                // Kasadan çıkış: Tezgahın sol açık tarafına (Local -X = -1.2m) adım atıp sıradan ve tezgahtan uzaklaşır:
                Vector3 stepAsidePos = activeCashier.transform.TransformPoint(new Vector3(-1.2f, 0.05f, -0.75f));
                stepAsidePos.x = Mathf.Clamp(stepAsidePos.x, -12.0f, 2.2f);
                stepAsidePos.z = Mathf.Clamp(stepAsidePos.z, -1.8f, 32.0f);
                stepAsidePos.y = 0.05f;
                exitWaypoints.Add(stepAsidePos);
            }

            exitWaypoints.Add(new Vector3(-5.0f, 0.05f, -0.5f)); // Dükkan İçi Fuaye
            exitWaypoints.Add(new Vector3(-5.0f, 0.05f, -2.5f)); // Cam Kapı Geçişi
            exitWaypoints.Add(new Vector3(-5.0f, 0.05f, -5.0f)); // Dış Kaldırım

            if (cData.hasVehicle)
            {
                int level = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
                int slotsPerRow = ((level == 1) ? 10 : ((level == 2) ? 16 : 22)) / 2;
                bool isLeftSlot = (cData.parkingSlotIndex < slotsPerRow);
                Vector3 driverDoor = cData.parkedSlotPos + (isLeftSlot ? Vector3.right * 1.5f : Vector3.left * 1.5f);

                exitWaypoints.Add(new Vector3(-17.0f, 0.05f, -5.0f)); // Turnike Yaya Geçidi
                exitWaypoints.Add(new Vector3(-17.0f, 0.05f, -0.5f)); // Turnike Geçiş Ara Noktası
                exitWaypoints.Add(new Vector3(-17.0f, 0.05f, 1.5f));  // Turnike Giriş Boğazı
                exitWaypoints.Add(new Vector3(-27.0f, 0.05f, 1.5f));  // Otopark Boğazı
                exitWaypoints.Add(new Vector3(-27.0f, 0.05f, cData.parkedSlotPos.z)); // Otopark Orta Yolu
                exitWaypoints.Add(driverDoor); // Sürücü Kapısı (Biniş)
            }
            else
            {
                // Yaya / Otobüs Müşterisi: Kaldırımdan batıya doğru yürüyüp yok olur
                exitWaypoints.Add(new Vector3(-17.0f, 0.05f, -5.0f));
                exitWaypoints.Add(new Vector3(-45.0f, 0.05f, -5.0f));
                exitWaypoints.Add(new Vector3(-85.0f, 0.05f, -5.0f)); // Despawn
            }

            return exitWaypoints;
        }

        private void ShowPaymentPopup(Vector3 pos, string text)
        {
            GameObject popupObj = new GameObject("Popup_Payment");
            popupObj.transform.position = pos + Vector3.up * 1.95f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 55;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380f, 65f);
            popupObj.transform.localScale = Vector3.one * 0.011f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = UIStyleUtility.GetGlobalFont(22);
            txt.text = text;
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.35f, 0.95f, 0.45f);
            txt.raycastTarget = false;

            UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.8f, -1.8f);

            Destroy(popupObj, 1.6f);
        }

        private IEnumerator ProcessDynamicCheckoutScanning(ActiveCustomerData cData, PlacedFurnitureController cashier, int scanCount, int paymentAmount)
        {
            if (cData == null || cData.customerObj == null) yield break;

            Vector3 cashierPos = (cashier != null) ? cashier.transform.position : cData.customerObj.transform.position;

            for (int i = 0; i < scanCount; i++)
            {
                if (cData == null || cData.customerObj == null) yield break;

                // Dinamik Barkod Bip Sesi (Her üründe doğal frekans varyasyonu)
                if (AudioManager.Instance != null)
                {
                    float pitchVariation = Random.Range(0.96f, 1.05f);
                    AudioManager.Instance.PlayBarcodeBeep(pitchVariation);
                }

                // Kasa tezgahı üzerinde minik görsel barkod tarama efekti
                ShowBarcodeScanFlash(cashierPos);

                yield return new WaitForSeconds(0.22f);
            }

            if (cData == null || cData.customerObj == null) yield break;

            // Bütün ürünler okutuldu -> Kasa / Para sesi ve pop-up
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCoins();
            }

            string paymentMsg = LocalizationManager.L("Payment_Success", "Ödeme Yapıldı", "Payment Completed");
            Vector3 custPos = cData.customerObj.transform.position;
            ShowPaymentPopup(custPos, $"+{paymentAmount}C {paymentMsg} 💳");

            // Çıkış Rotasını Başlat
            ClearCarriedCartOnCustomer(cData);
            DequeueCustomerFromCashier(cData);

            cData.isCheckingOut = false;
            cData.hasPaidAndExiting = true;

            cData.waypoints = BuildExitRoute(cData, cashier);
            cData.currentWaypointIndex = 1;
            cData.stateWaitTimer = 0f;
        }

        private void ShowBarcodeScanFlash(Vector3 cashierPos)
        {
            GameObject flashObj = new GameObject("Popup_ScanBeep");
            flashObj.transform.position = cashierPos + Vector3.up * 1.35f + Vector3.forward * 0.1f;

            Canvas canvas = flashObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            RectTransform rt = flashObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 40f);
            flashObj.transform.localScale = Vector3.one * 0.009f;

            if (Camera.main != null) flashObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(flashObj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = UIStyleUtility.GetGlobalFont(18);
            txt.text = "🏷️ BİP!";
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.20f, 1.0f, 0.90f);
            txt.raycastTarget = false;

            UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            Destroy(flashObj, 0.28f);
        }



        private List<Vector3> BuildCustomerShoppingRoute(out bool willVisitServiceDesk)
        {
            return BuildCustomerShoppingRoute(out willVisitServiceDesk, out _, CustomerType.L1_CasualBoy);
        }

        private List<Vector3> BuildCustomerShoppingRoute()
        {
            return BuildCustomerShoppingRoute(out _, out _, CustomerType.L1_CasualBoy);
        }

        private void UpdateActiveCustomers(float deltaTime)
        {
            int prevCount = activeCustomers.Count;
            for (int i = activeCustomers.Count - 1; i >= 0; i--)
            {
                ActiveCustomerData cData = activeCustomers[i];
                if (cData == null)
                {
                    activeCustomers.RemoveAt(i);
                    continue;
                }

                if (cData.hasVehicle)
                {
                    UpdateVehicleCustomer(cData, deltaTime, i);
                }
                else
                {
                    UpdatePedestrianCustomer(cData, deltaTime, i);
                }
            }

            if (prevCount > 0 && activeCustomers.Count == 0)
            {
                // Son müşteri de alanı terk etti: Eğer dükkan kapalıysa (gece 24:00 tahliyesi) personellerin çıkışını derhal tetikle
                if (StaffVisualManager.Instance != null)
                {
                    StaffVisualManager.Instance.SyncStaff3DModels();
                }
            }
        }

        private List<Vector3> BuildVehicleCustomerWalkingRoute(Vector3 slotPos, int slotIndex, out bool willVisitServiceDesk, out bool hasCartStand, CustomerType selectedType)
        {
            List<Vector3> route = new List<Vector3>();
            int customerTier = GetCustomerTier(selectedType);

            int level = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            int slotsPerRow = ((level == 1) ? 10 : ((level == 2) ? 16 : 22)) / 2;
            bool isLeftSlot = (slotIndex < slotsPerRow);

            // Sürücü Kapısı İniş Noktası (Sol otopark için sağa x=-33.5m, Sağ otopark için sola x=-20.5m otopark orta yolu tarafına):
            Vector3 driverDoorPos = slotPos + (isLeftSlot ? Vector3.right * 1.5f : Vector3.left * 1.5f);
            route.Add(driverDoorPos);                             // 0. Sürücü Kapısı
            route.Add(new Vector3(-27.0f, 0.05f, slotPos.z));     // 1. Otopark Orta Yolu
            route.Add(new Vector3(-27.0f, 0.05f, 1.5f));          // 2. Otopark Boğazı
            route.Add(new Vector3(-17.0f, 0.05f, 1.5f));          // 3. Turnike Giriş Boğazı
            route.Add(new Vector3(-17.0f, 0.05f, -0.5f));         // 4. Turnike Geçiş Ara Noktası (Duvara toslamayı %100 önler)
            route.Add(new Vector3(-17.0f, 0.05f, -5.0f));         // 5. Turnike Yaya Geçidi / Dış Kaldırım
            route.Add(new Vector3(-5.0f, 0.05f, -5.0f));          // 6. Ana Cam Kapı Önü Dış Kaldırım
            route.Add(new Vector3(-5.0f, 0.05f, -2.5f));          // 7. Cam Kapı Geçişi
            route.Add(new Vector3(-5.0f, 0.05f, -0.5f));          // 8. Ana Fuaye İç Giriş

            PlacedFurnitureController cartStand = GetActiveShoppingCartStand();
            hasCartStand = (cartStand != null);

            if (hasCartStand)
            {
                route.Add(cartStand.GetFrontInteractionPosition(1.0f));
            }
            else
            {
                // Sepet stantı yoksa arabasına döner
                route.Add(new Vector3(-5.0f, 0.05f, -0.5f));
                route.Add(new Vector3(-5.0f, 0.05f, -2.5f));
                route.Add(new Vector3(-5.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-17.0f, 0.05f, -5.0f));
                route.Add(new Vector3(-17.0f, 0.05f, -0.5f));
                route.Add(new Vector3(-17.0f, 0.05f, 1.5f));
                route.Add(new Vector3(-27.0f, 0.05f, 1.5f));
                route.Add(new Vector3(-27.0f, 0.05f, slotPos.z));
                route.Add(driverDoorPos);
                willVisitServiceDesk = false;
                return route;
            }

            var shelves = PlacedFurnitureController.AllPlacedFurniture;
            PlacedFurnitureController serviceDesk = null;

            int vShCount = shelves.Count;
            for (int i = 0; i < vShCount; i++)
            {
                var s = shelves[i];
                if (s != null && s.FurnitureType == FurnitureType.CustomerServiceDesk)
                {
                    serviceDesk = s;
                    break;
                }
            }

            willVisitServiceDesk = (serviceDesk != null && IsCustomerServiceStaffWorking() && Random.value < 0.45f);
            if (willVisitServiceDesk)
            {
                route.Add(serviceDesk.GetFrontInteractionPosition(1.0f));
            }

            if (HasAnyStockedShelfForCustomer(customerTier))
            {
                AddRandomShelfWaypoints(route, shelves, customerTier);

                Vector3 checkoutPos = new Vector3(-6.5f, 0.05f, 1.5f);
                foreach (var s in shelves)
                {
                    if (s != null && s.FurnitureType == FurnitureType.Cashier)
                    {
                        checkoutPos = s.GetFrontInteractionPosition(1.0f);
                        break;
                    }
                }
                route.Add(checkoutPos);
            }

            // Arabaya Dönüş Rotası:
            route.Add(new Vector3(-5.0f, 0.05f, -0.5f));          // Ana Fuaye Çıkış
            route.Add(new Vector3(-5.0f, 0.05f, -2.5f));          // Cam Kapı Geçişi
            route.Add(new Vector3(-5.0f, 0.05f, -5.0f));          // Dış Kaldırım
            route.Add(new Vector3(-17.0f, 0.05f, -5.0f));         // Turnike Yaya Geçidi
            route.Add(new Vector3(-17.0f, 0.05f, -0.5f));         // Turnike Geçiş Ara Noktası
            route.Add(new Vector3(-17.0f, 0.05f, 1.5f));          // Turnike Giriş Boğazı
            route.Add(new Vector3(-27.0f, 0.05f, 1.5f));          // Otopark Boğazı
            route.Add(new Vector3(-27.0f, 0.05f, slotPos.z));     // Otopark Orta Yolu
            route.Add(driverDoorPos);                             // Sürücü Kapısı (Arabaya Biniş)

            return route;
        }

        private void UpdateVehicleCustomer(ActiveCustomerData cData, float deltaTime, int index)
        {
            if (cData == null || cData.vehicleObj == null)
            {
                if (cData != null && cData.customerObj != null) Destroy(cData.customerObj);
                if (cData != null && cData.vehicleObj != null) Destroy(cData.vehicleObj);
                activeCustomers.RemoveAt(index);
                return;
            }

            if (entranceBarrier == null || exitBarrier == null)
            {
                FindParkingBarriers();
            }

            Vector3 currentCarPos = cData.vehicleObj.transform.position;

            switch (cData.drivePhase)
            {
                case VehicleDrivePhase.DrivingToEntranceTurnstileApproach:
                    // 1. Ana Yolda İlerleme: Otoyoldan (Z = -7.5f) Giriş Turnikesi Şerit Merkezine (X = -15.5f, Z = -7.5f) Yanaşır (Batıya Bakar -90°)
                    Vector3 entranceTurnNode = new Vector3(-15.5f, 0.05f, -7.5f);
                    MoveVehicle(cData, currentCarPos, entranceTurnNode, Quaternion.Euler(0f, -90f, 0f), deltaTime);

                    if (Vector3.Distance(currentCarPos, entranceTurnNode) < 0.4f)
                    {
                        cData.drivePhase = VehicleDrivePhase.PassingThroughEntranceTurnstile;
                    }
                    break;

                case VehicleDrivePhase.PassingThroughEntranceTurnstile:
                    // 2. Giriş Turnikesine Dönüş & Geçiş: Sağ Turnike Şeridinde Kuzeye Döner (0°) ve X = -15.5f Şeridinden Bariyer & Yaya Geçidini Geçerek Otopark Boğazına (Z = 1.5f) İlerler!
                    Vector3 entranceTurnstileThroat = new Vector3(-15.5f, 0.05f, 1.5f);

                    // Bariyer Açma/Kapama Tetiklemesi (Z = -6.5m ile 0.0m arası barrier açılır)
                    if (currentCarPos.z >= -6.5f && currentCarPos.z <= 0.0f && entranceBarrier != null)
                    {
                        entranceBarrier.OpenBarrier();
                    }
                    else if (currentCarPos.z > 0.0f && entranceBarrier != null)
                    {
                        entranceBarrier.CloseBarrier();
                    }

                    MoveVehicle(cData, currentCarPos, entranceTurnstileThroat, Quaternion.Euler(0f, 0f, 0f), deltaTime);

                    if (Vector3.Distance(currentCarPos, entranceTurnstileThroat) < 0.4f)
                    {
                        cData.drivePhase = VehicleDrivePhase.NavigatingParkingAisle;
                    }
                    break;

                case VehicleDrivePhase.NavigatingParkingAisle:
                    // 3. Otopark İç Koridoruna Manevra: Boğazdan (X = -15.5f, Z = 1.5f) Orta Yola (X = -27.0f, Z = slotPos.z) Geçiş Yapılır
                    Vector3 aisleEntranceNode = new Vector3(-27.0f, 0.05f, 1.5f);
                    Vector3 targetAisleNode = new Vector3(-27.0f, 0.05f, cData.parkedSlotPos.z);

                    if (Vector3.Distance(currentCarPos, aisleEntranceNode) > 0.6f && currentCarPos.x > -25.5f)
                    {
                        MoveVehicle(cData, currentCarPos, aisleEntranceNode, Quaternion.Euler(0f, -90f, 0f), deltaTime);
                    }
                    else
                    {
                        float aisleAngle = (cData.parkedSlotPos.z >= 1.5f) ? 0f : 180f;
                        MoveVehicle(cData, currentCarPos, targetAisleNode, Quaternion.Euler(0f, aisleAngle, 0f), deltaTime);

                        if (Vector3.Distance(currentCarPos, targetAisleNode) < 0.4f)
                        {
                            cData.drivePhase = VehicleDrivePhase.ParkingInSlot;
                        }
                    }
                    break;

                case VehicleDrivePhase.ParkingInSlot:
                    // 4. Park Yerine Hizalanıp Durma (Sol Sütun: -90°, Sağ Sütun: 90°)
                    int level = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
                    int slotsPerRow = ((level == 1) ? 10 : ((level == 2) ? 16 : 22)) / 2;
                    bool isLeftSlot = (cData.parkingSlotIndex < slotsPerRow);
                    float targetAngle = isLeftSlot ? -90f : 90f;
                    MoveVehicle(cData, currentCarPos, cData.parkedSlotPos, Quaternion.Euler(0f, targetAngle, 0f), deltaTime);

                    if (Vector3.Distance(currentCarPos, cData.parkedSlotPos) < 0.3f)
                    {
                        cData.vehicleObj.transform.position = cData.parkedSlotPos;
                        cData.vehicleObj.transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);

                        // Sürücü Araçtan İner (Sol otopark için sağa x=-33.5m, Sağ otopark için sola x=-20.5m otopark orta yolu tarafına):
                        Vector3 driverDoorPos = cData.parkedSlotPos + (isLeftSlot ? Vector3.right * 1.5f : Vector3.left * 1.5f);
                        if (cData.customerObj != null)
                        {
                            cData.customerObj.transform.position = driverDoorPos;
                            cData.customerObj.SetActive(true);
                        }

                        cData.waypoints = BuildVehicleCustomerWalkingRoute(cData.parkedSlotPos, cData.parkingSlotIndex, out cData.isVisitingCustomerService, out cData.hasCartStand, cData.type);
                        cData.currentWaypointIndex = 1;
                        cData.drivePhase = VehicleDrivePhase.EngineOffShoppingOnFoot;
                    }
                    break;

                case VehicleDrivePhase.EngineOffShoppingOnFoot:
                    // 5. Müşteri Yürüyerek Dükkana Girer, Alışveriş Yapar ve Arabasına Dönüş Yapar
                    UpdatePedestrianCustomer(cData, deltaTime, index);

                    if (cData.hasPaidAndExiting && cData.currentWaypointIndex >= cData.waypoints.Count - 1)
                    {
                        // Sürücü Arabaya Biner (Yalnızca ödemesini yapıp arabasına ulaştığında):
                        if (cData.customerObj != null) cData.customerObj.SetActive(false);
                        cData.drivePhase = VehicleDrivePhase.ReversingOutSlot;
                    }
                    break;

                case VehicleDrivePhase.ReversingOutSlot:
                    // 6. Park Yerinden Çıkış: Otopark Orta Yoluna (X = -27.0f, Z = slotPos.z) Çıkıp Güneye (180°) Hizalanır
                    Vector3 exitDrivewayPos = new Vector3(-27.0f, 0.05f, cData.parkedSlotPos.z);
                    MoveVehicle(cData, currentCarPos, exitDrivewayPos, Quaternion.Euler(0f, 180f, 0f), deltaTime);

                    if (Vector3.Distance(currentCarPos, exitDrivewayPos) < 0.5f)
                    {
                        cData.drivePhase = VehicleDrivePhase.DrivingToExitTurnstileApproach;
                    }
                    break;

                case VehicleDrivePhase.DrivingToExitTurnstileApproach:
                    // 7. Çıkış Turnike Boğazına Yaklaşma: Otopark Orta Yolundan (X = -18.5f, Z = 1.5f) Çıkış Şeridine Hizalanır (180° Güneye Bakar)
                    Vector3 exitTurnstileNode = new Vector3(-18.5f, 0.05f, 1.5f);
                    MoveVehicle(cData, currentCarPos, exitTurnstileNode, Quaternion.Euler(0f, 180f, 0f), deltaTime);

                    if (Vector3.Distance(currentCarPos, exitTurnstileNode) < 0.5f)
                    {
                        cData.drivePhase = VehicleDrivePhase.PassingThroughExitTurnstile;
                    }
                    break;

                case VehicleDrivePhase.PassingThroughExitTurnstile:
                    // 8. Çıkış Turnikesinden Geçiş: GÜNEYE BAKARAK (180°), X = -18.5f Çıkış Şeridinden Bariyer & Yaya Geçidini Geçer ve Z = -7.5f Otoyol Şeridine İner!
                    Vector3 mainRoadExitJunction = new Vector3(-18.5f, 0.05f, -7.5f);

                    // Bariyer Açma/Kapama Tetiklemesi (Z = 1.0m ile -4.5m arası barrier açılır)
                    if (currentCarPos.z <= 1.0f && currentCarPos.z >= -4.5f && exitBarrier != null)
                    {
                        exitBarrier.OpenBarrier();
                    }
                    else if (currentCarPos.z < -4.5f && exitBarrier != null)
                    {
                        exitBarrier.CloseBarrier();
                    }

                    MoveVehicle(cData, currentCarPos, mainRoadExitJunction, Quaternion.Euler(0f, 180f, 0f), deltaTime);

                    if (Vector3.Distance(currentCarPos, mainRoadExitJunction) < 0.4f || currentCarPos.z <= -7.2f)
                    {
                        cData.drivePhase = VehicleDrivePhase.DrivingAwayWest;
                    }
                    break;

                case VehicleDrivePhase.DrivingAwayWest:
                    // 9. Ana Yolda Batıya Dönüş & Despawn: Otoyol Şeridinde (Z = -7.5f) Sağa Döner (Batıya -90°) ve Sürerek Ayrılır!
                    Vector3 despawnRoadPos = new Vector3(-180.0f, 0.05f, -7.5f);
                    MoveVehicle(cData, currentCarPos, despawnRoadPos, Quaternion.Euler(0f, -90f, 0f), deltaTime);

                    if (Vector3.Distance(currentCarPos, despawnRoadPos) < 3.0f || currentCarPos.x <= -170.0f)
                    {
                        if (cData.parkingSlotIndex >= 0 && cData.parkingSlotIndex < occupiedParkingSlots.Length)
                        {
                            occupiedParkingSlots[cData.parkingSlotIndex] = false;
                        }
                        if (cData.vehicleObj != null) Destroy(cData.vehicleObj);
                        if (cData.customerObj != null) Destroy(cData.customerObj);
                        activeCustomers.RemoveAt(index);
                    }
                    break;
            }
        }

        private void MoveVehicle(ActiveCustomerData cData, Vector3 currentPos, Vector3 targetPos, Quaternion targetRot, float deltaTime)
        {
            Vector3 diff = targetPos - currentPos;
            if (diff.magnitude > 0.05f)
            {
                cData.vehicleObj.transform.position = Vector3.MoveTowards(currentPos, targetPos, cData.vehicleSpeed * deltaTime);
                cData.vehicleObj.transform.rotation = Quaternion.Slerp(cData.vehicleObj.transform.rotation, targetRot, 10f * deltaTime);
                if (cData.vehicleWheels != null) foreach (var w in cData.vehicleWheels) if (w != null) w.Rotate(Vector3.right * 600f * deltaTime, Space.Self);
            }
        }

        private readonly Dictionary<PlacedFurnitureController, List<ActiveCustomerData>> cashierQueues = new Dictionary<PlacedFurnitureController, List<ActiveCustomerData>>();

        private PlacedFurnitureController FindBestCashierForCustomer()
        {
            List<PlacedFurnitureController> allCashiers = StaffTaskController.GetAllCashierCounters();
            if (allCashiers == null || allCashiers.Count == 0) return null;

            PlacedFurnitureController bestStaffedCashier = null;
            int minStaffedQueue = int.MaxValue;

            PlacedFurnitureController bestAnyCashier = null;
            int minAnyQueue = int.MaxValue;

            foreach (var f in allCashiers)
            {
                if (f != null)
                {
                    if (!cashierQueues.ContainsKey(f))
                    {
                        cashierQueues[f] = new List<ActiveCustomerData>();
                    }

                    cashierQueues[f].RemoveAll(c => c == null || c.customerObj == null);
                    int qCount = cashierQueues[f].Count;

                    bool isStaffed = StaffTaskController.IsCashierWorkingAt(f);
                    if (isStaffed && qCount < minStaffedQueue)
                    {
                        minStaffedQueue = qCount;
                        bestStaffedCashier = f;
                    }

                    if (qCount < minAnyQueue)
                    {
                        minAnyQueue = qCount;
                        bestAnyCashier = f;
                    }
                }
            }

            // Personel çalışan açık bir kasa varsa öncelikle onu ve en az kuyruğu olanı seç!
            return (bestStaffedCashier != null) ? bestStaffedCashier : bestAnyCashier;
        }

        public static Vector3 GetCashierSlotWorldPosition(PlacedFurnitureController cashier, int slotIndex)
        {
            if (cashier == null) return new Vector3(-6.5f, 0.05f, 1.5f);

            // Zemin Turkuaz L-Kuyruk Oku Üzerinde Birebir ve Kusursuz Tek Sıra Hizalama (Exact L-Arrow Alignment):
            Vector3 localSlotPos;
            if (slotIndex <= 0)
            {
                // Slot 0: Kasa Tezgahı & Barkod Okuyucu Önü (Ödeme Yapan Kişi)
                localSlotPos = new Vector3(0f, 0.05f, -0.75f);
            }
            else if (slotIndex == 1)
            {
                // Slot 1: L-Dönüş Köşesi (2. Sıradaki Müşteri - Slot 0'ın hemen sağ arkası)
                localSlotPos = new Vector3(0.95f, 0.05f, -0.75f);
            }
            else
            {
                // Slot 2, 3, 4, 5...: Turkuaz Düz Koridor Şeridi Boyunca Tek Sıra (Her müşteri arası 0.95m net mesafe)
                localSlotPos = new Vector3(0.95f, 0.05f, -0.75f - (slotIndex - 1) * 0.95f);
            }

            Vector3 rawWorldPos = cashier.transform.TransformPoint(localSlotPos);

            // Dükkan içi sınır kenetleme (X: -12.0 ile +2.2 arası, Z: -2.2 ile +32.0 arası):
            rawWorldPos.x = Mathf.Clamp(rawWorldPos.x, -12.0f, 2.2f);
            rawWorldPos.z = Mathf.Clamp(rawWorldPos.z, -2.2f, 32.0f);
            rawWorldPos.y = 0.05f;

            return rawWorldPos;
        }

        private void EnqueueCustomerAtCashier(ActiveCustomerData cData, PlacedFurnitureController cashier)
        {
            if (cData == null || cashier == null || cData.hasPaidAndExiting) return;

            // SEPETİ VEYA ÜRÜNÜ OLMAYAN MÜŞTERİ KASAYA KESİNLİKLE GİREMEZ!
            if (!cData.grabbedCartFromStand || cData.carriedCartObj == null) return;

            if (cData.isInCashierQueue && cData.assignedCashier == cashier) return;

            if (cData.isInCashierQueue) DequeueCustomerFromCashier(cData);

            if (!cashierQueues.ContainsKey(cashier))
            {
                cashierQueues[cashier] = new List<ActiveCustomerData>();
            }

            cashierQueues[cashier].RemoveAll(c => c == null || c.customerObj == null);
            cashierQueues[cashier].Add(cData);

            cData.assignedCashier = cashier;
            cData.isInCashierQueue = true;
            cData.queueSlotIndex = cashierQueues[cashier].IndexOf(cData);

            Vector3 slotPos = GetCashierSlotWorldPosition(cashier, cData.queueSlotIndex);
            cData.waypoints = new List<Vector3> { cData.customerObj.transform.position, slotPos };
            cData.currentWaypointIndex = 1;
            cData.stateWaitTimer = 0f;

            UpdateCashierQueuePositions(cashier);
        }

        private void DequeueCustomerFromCashier(ActiveCustomerData cData)
        {
            if (cData == null) return;
            if (cData.assignedCashier != null && cashierQueues.ContainsKey(cData.assignedCashier))
            {
                cashierQueues[cData.assignedCashier].Remove(cData);
                UpdateCashierQueuePositions(cData.assignedCashier);
            }

            cData.assignedCashier = null;
            cData.isInCashierQueue = false;
            cData.queueSlotIndex = -1;
        }

        private void UpdateCashierQueuePositions(PlacedFurnitureController cashier)
        {
            if (cashier == null || !cashierQueues.ContainsKey(cashier)) return;

            cashierQueues[cashier].RemoveAll(c => c == null || c.customerObj == null || !c.isInCashierQueue || !c.grabbedCartFromStand || c.carriedCartObj == null);
            List<ActiveCustomerData> qList = cashierQueues[cashier];

            for (int i = 0; i < qList.Count; i++)
            {
                ActiveCustomerData c = qList[i];
                int oldIndex = c.queueSlotIndex;
                c.queueSlotIndex = i;

                Vector3 slotPos = GetCashierSlotWorldPosition(cashier, i);

                // Sırası ilerleyen her müşteriye doğrudan yeni slot hedefi tanımla (İç içe geçmeyi %100 engeller):
                if (oldIndex != i || c.waypoints == null || c.waypoints.Count == 0)
                {
                    c.waypoints = new List<Vector3> { c.customerObj.transform.position, slotPos };
                    c.currentWaypointIndex = 1;
                    c.stateWaitTimer = 0f;
                }
                else
                {
                    c.waypoints[c.waypoints.Count - 1] = slotPos;
                }
            }
        }

        private void UpdatePedestrianCustomer(ActiveCustomerData cData, float deltaTime, int index)
        {
            if (cData.customerObj == null || cData.waypoints == null || cData.waypoints.Count == 0) return;
            if (cData.stateWaitTimer > 0f)
            {
                cData.stateWaitTimer -= deltaTime;
                ResetLimbsToRest(cData);

                // Kasada beklerken yüzünü turkuaz L-ok yönünde ödeme noktasına döner
                if (cData.isInCashierQueue && cData.assignedCashier != null)
                {
                    Vector3 faceDir;
                    if (cData.queueSlotIndex == 0)
                    {
                        // Slot 0: Kasa Tezgahına ve Barkod Okuyucuya Bak (Masa İleri Yönü)
                        faceDir = cData.assignedCashier.transform.forward;
                    }
                    else if (cData.queueSlotIndex == 1)
                    {
                        // Slot 1: L-Dönüş Köşesinde Sola (Slot 0'daki Ödeme Yapan Kişiye) Bak
                        faceDir = -cData.assignedCashier.transform.right;
                    }
                    else
                    {
                        // Slot 2, 3, 4...: Ok Çizgisi Boyunca İleriye (Önündeki Sıradaki Müşteriye) Bak
                        faceDir = cData.assignedCashier.transform.forward;
                    }

                    faceDir.y = 0f;
                    if (faceDir != Vector3.zero)
                    {
                        cData.customerObj.transform.rotation = Quaternion.RotateTowards(cData.customerObj.transform.rotation, Quaternion.LookRotation(faceDir), 360f * deltaTime);
                    }
                }
                else
                {
                    var allFurniture = PlacedFurnitureController.AllPlacedFurniture;
                    PlacedFurnitureController nearest = null;
                    float minDist = 3.0f;
                    int furnCount = allFurniture.Count;
                    for (int i = 0; i < furnCount; i++)
                    {
                        var f = allFurniture[i];
                        if (f != null)
                        {
                            float d = Vector3.Distance(f.transform.position, cData.customerObj.transform.position);
                            if (d < minDist) { minDist = d; nearest = f; }
                        }
                    }
                    if (nearest != null)
                    {
                        Quaternion targetFaceRot = nearest.GetFrontFacingRotation();
                        cData.customerObj.transform.rotation = Quaternion.RotateTowards(cData.customerObj.transform.rotation, targetFaceRot, 360f * deltaTime);
                    }
                }

                // Ödeme tamamlandığında sepeti bırak, kuyruktan ayrıl ve çıkış rotasını başlat!
                if (cData.isCheckingOut && cData.stateWaitTimer <= 0f)
                {
                    PlacedFurnitureController activeCashier = cData.assignedCashier;
                    ClearCarriedCartOnCustomer(cData);
                    DequeueCustomerFromCashier(cData);

                    cData.isCheckingOut = false;
                    cData.hasPaidAndExiting = true;

                    cData.waypoints = BuildExitRoute(cData, activeCashier);
                    cData.currentWaypointIndex = 1;
                    cData.stateWaitTimer = 0f;
                }
                return;
            }

            if (cData.isCheckingOut && cData.stateWaitTimer <= 0f)
            {
                PlacedFurnitureController activeCashier = cData.assignedCashier;
                ClearCarriedCartOnCustomer(cData);
                DequeueCustomerFromCashier(cData);

                cData.isCheckingOut = false;
                cData.hasPaidAndExiting = true;

                cData.waypoints = BuildExitRoute(cData, activeCashier);
                cData.currentWaypointIndex = 1;
                cData.stateWaitTimer = 0f;
                return;
            }

            Vector3 currentPos = cData.customerObj.transform.position;

            // 1. Kasada Kuyrukta İken Kendi Slotuna Doğru İlerleme / Bekleme
            if (cData.isInCashierQueue)
            {
                Vector3 targetSlotPos = (cData.assignedCashier != null) ? GetCashierSlotWorldPosition(cData.assignedCashier, cData.queueSlotIndex) : Vector3.zero;
                float distToSlot = (cData.assignedCashier != null) ? Vector3.Distance(currentPos, targetSlotPos) : 999f;
                bool isAtCashierSlot = (cData.assignedCashier != null && distToSlot < 0.65f);

                if (isAtCashierSlot)
                {
                    if (cData.queueSlotIndex == 0)
                    {
                        // SADECE VE SADECE 1. SIRADAKİ (SLOT 0) KİŞİ KASAYA ULAŞTIĞINDA ÖDEME YAPABİLİR!
                        bool isCashierWorking = (cData.assignedCashier != null && StaffTaskController.IsCashierWorkingAt(cData.assignedCashier));
                        if (isCashierWorking)
                        {
                            if (cData.activeNoCashierPopup != null)
                            {
                                Destroy(cData.activeNoCashierPopup);
                                cData.activeNoCashierPopup = null;
                            }

                            if (!cData.isCheckingOut)
                            {
                                cData.isCheckingOut = true;
                                int scanCount = Mathf.Clamp(cData.totalItemsBought, 1, 6);
                                cData.stateWaitTimer = (scanCount * 0.22f) + 0.35f;

                                int paymentAmount = Mathf.Max(1, cData.totalCartValue);
                                if (cData.visitedCustomerServiceDesk) paymentAmount += Random.Range(50, 100);

                                if (EconomyManager.Instance != null) EconomyManager.Instance.AddCredits(paymentAmount);
                                if (FinanceManager.Instance != null) FinanceManager.Instance.RecordIncome("Satış", $"Müşteri Alışverişi ({cData.totalItemsBought} Parça Ürün)", paymentAmount);

                                // KASADA KALİTE PUANI HESAPLAMA:
                                if (StoreQualityManager.Instance != null)
                                {
                                    bool isClean = (StoreCleanlinessManager.Instance == null || StoreCleanlinessManager.Instance.GetNearestTrashItem(currentPos, out float trashDist) == null);
                                    if (isClean)
                                    {
                                        StoreQualityManager.Instance.AddQualityScore(15, currentPos, LocalizationManager.L("Quality_CleanStore", "Temiz Dükkan!", "Clean Store!"));
                                    }
                                    else
                                    {
                                        StoreQualityManager.Instance.SubtractQualityScore(10, currentPos, LocalizationManager.L("Quality_DirtyStore", "Kirli Dükkan!", "Dirty Store!"));
                                    }

                                    if (cData.totalItemsBought >= 4)
                                    {
                                        StoreQualityManager.Instance.AddQualityScore(10, currentPos, LocalizationManager.L("Quality_FullCart", "Dolu Sepet!", "Full Cart!"));
                                    }
                                    if (cData.visitedCustomerServiceDesk)
                                    {
                                        StoreQualityManager.Instance.AddQualityScore(10, currentPos, LocalizationManager.L("Quality_ServiceDesk", "Danışma Memnuniyeti!", "Customer Service!"));
                                    }
                                }

                                StartCoroutine(ProcessDynamicCheckoutScanning(cData, cData.assignedCashier, scanCount, paymentAmount));
                            }
                        }
                        else
                        {
                            // ❌ KASADA KASİYER YOK!
                            // Müşteri KESİNLİKLE ödeme yapamaz, sıranın başında kasiyeri bekler!
                            cData.noCashierWarningTimer -= deltaTime;
                            if (cData.noCashierWarningTimer <= 0f)
                            {
                                cData.noCashierWarningTimer = 2.5f;
                                ShowNoCashierWarning(cData);
                            }

                            cData.stateWaitTimer = 0.5f;
                            return;
                        }
                    }
                    else
                    {
                        // KUYRUKTAKİ DİĞER MÜŞTERİLER (SLOT 1, 2, 3...) KENDİ SLOTUNDA BEKLER, ASLA ÖDEME YAPAMAZ!
                        cData.stateWaitTimer = 0.25f;
                    }
                    return;
                }
                else
                {
                    // Slota doğru adım at
                    Vector3 queueStepDir = (targetSlotPos - currentPos).normalized;
                    cData.customerObj.transform.position = Vector3.MoveTowards(currentPos, targetSlotPos, WALK_SPEED * deltaTime);
                    if (queueStepDir != Vector3.zero)
                    {
                        cData.customerObj.transform.rotation = Quaternion.RotateTowards(cData.customerObj.transform.rotation, Quaternion.LookRotation(queueStepDir), 360f * deltaTime);
                    }
                    cData.walkCycleTimer += deltaTime * 8.5f;
                    AnimateHumanLimbs(cData, Mathf.Sin(cData.walkCycleTimer) * 26.0f);
                    return;
                }
            }

            Vector3 targetWaypoint = cData.waypoints[cData.currentWaypointIndex];
            Vector3 toTarget = targetWaypoint - currentPos;
            float distToTarget = toTarget.magnitude;

            // Anti-Stuck & Duvar Kurtarma Koruyucusu (Müşteri yürürken bir noktada takılırsa açık alana kaydırılır ve ilerletilir; sırada beklerken tetiklenmez):
            if (!cData.isInCashierQueue && cData.stateWaitTimer <= 0f && Vector3.Distance(currentPos, cData.lastTrackedPos) < 0.08f)
            {
                cData.stuckTimer += deltaTime;
                if (cData.stuckTimer > 1.2f && cData.stuckTimer <= 2.2f)
                {
                    Vector3 nudgeDir = (targetWaypoint - currentPos).normalized;
                    if (nudgeDir != Vector3.zero)
                    {
                        cData.customerObj.transform.position += nudgeDir * (1.2f * deltaTime);
                    }
                }
                else if (cData.stuckTimer > 2.2f)
                {
                    cData.stuckTimer = 0f;
                    cData.currentWaypointIndex++;
                    if (cData.currentWaypointIndex >= cData.waypoints.Count)
                    {
                        if (!cData.hasVehicle)
                        {
                            Destroy(cData.customerObj);
                            activeCustomers.RemoveAt(index);
                            return;
                        }
                    }
                    return;
                }
            }
            else
            {
                cData.stuckTimer = 0f;
                cData.lastTrackedPos = currentPos;
            }

            if (distToTarget < (cData.hasPaidAndExiting ? 0.85f : 0.65f))
            {
                if (cData.hasPaidAndExiting)
                {
                    // ✅ Ödemesini tamamlamış müşteri: Doğrudan çıkış noktalarını takip edip dükkandan ayrılır!
                    cData.currentWaypointIndex++;
                    if (cData.currentWaypointIndex >= cData.waypoints.Count)
                    {
                        DequeueCustomerFromCashier(cData);
                        ClearCarriedCartOnCustomer(cData);

                        if (!cData.hasVehicle)
                        {
                            Destroy(cData.customerObj);
                            activeCustomers.RemoveAt(index);
                        }
                        return;
                    }
                    targetWaypoint = cData.waypoints[cData.currentWaypointIndex];
                    toTarget = targetWaypoint - currentPos;
                }
                else
                {
                    PlacedFurnitureController cartStand = GetActiveShoppingCartStand();

                    if (!cData.hasCartStand || cartStand == null)
                    {
                        // ❌ DÜKKANDA ALIŞVERİŞ SEPETİ YOK!
                        if (!cData.hasNoCartWarningShown && (targetWaypoint.z >= -2.5f && targetWaypoint.z <= 0.0f))
                        {
                            cData.hasNoCartWarningShown = true;
                            ShowNoShoppingCartWarning(cData.customerObj.transform.position);
                            cData.stateWaitTimer = 2.5f;

                            if (StoreQualityManager.Instance != null)
                            {
                                StoreQualityManager.Instance.SubtractQualityScore(10, cData.customerObj.transform.position, "Sepet Yok!");
                            }
                        }
                    }
                    else
                    {
                        // ✅ DÜKKANDA SEPET STANTI VAR:
                        Vector3 cartStandInteractionPos = cartStand.GetFrontInteractionPosition(1.0f);
                        bool isAtCartStand = (Vector3.Distance(targetWaypoint, cartStandInteractionPos) < 0.65f);

                        // 1. TAM OLARAK SEPET STANTININ ÖNÜNE ULAŞILDIĞINDA:
                        if (!cData.grabbedCartFromStand && isAtCartStand)
                        {
                            int customerTier = GetCustomerTier(cData.type);
                            bool hasStockedShelves = HasAnyStockedShelfForCustomer(customerTier);
                            if (!hasStockedShelves)
                            {
                                // ❌ DÜKKANDA MÜŞTERİNİN SEVİYESİNE AİT HİÇ ÜRÜN YOK!
                                ShowNoProductsWarning(cData);
                                ClearCarriedCartOnCustomer(cData);
                                cData.grabbedCartFromStand = false;
                                cData.stateWaitTimer = 1.2f;

                                if (StoreQualityManager.Instance != null)
                                {
                                    StoreQualityManager.Instance.SubtractQualityScore(10, cData.customerObj.transform.position, LocalizationManager.L("Quality_EmptyShelves", "Boş Raflar!", "Empty Shelves!"));
                                }

                                cData.waypoints = BuildDirectExitRoute(cData);
                                cData.currentWaypointIndex = 1;
                                return;
                            }
                            else
                            {
                                cData.grabbedCartFromStand = true;
                                cData.stateWaitTimer = 1.0f;
                                CreateCarriedShoppingCartOnCustomer(cData);
                            }
                        }
                        else if (cData.grabbedCartFromStand)
                        {
                            PlacedFurnitureController serviceDesk = null;
                            var allFurniture = PlacedFurnitureController.AllPlacedFurniture;
                            int fCount = allFurniture.Count;
                            for (int i = 0; i < fCount; i++)
                            {
                                var f = allFurniture[i];
                                if (f != null && f.FurnitureType == FurnitureType.CustomerServiceDesk)
                                {
                                    serviceDesk = f;
                                    break;
                                }
                            }

                            bool isAtServiceDesk = (serviceDesk != null && Vector3.Distance(targetWaypoint, serviceDesk.GetFrontInteractionPosition(1.0f)) < 0.65f);

                            // 2. Müşteri Hizmetleri Masasına Uğrama
                            if (cData.isVisitingCustomerService && !cData.visitedCustomerServiceDesk && isAtServiceDesk)
                            {
                                cData.visitedCustomerServiceDesk = true;
                                cData.stateWaitTimer = 1.5f;
                            }
                            // 3. Raftan Alışveriş Yapma
                            else if (!isAtServiceDesk && !cData.isInCashierQueue && (targetWaypoint.z > 0.5f))
                            {
                                PlacedFurnitureController targetShelf = FindNearestShelfToPosition(targetWaypoint);
                                if (targetShelf != null && (cData.visitedShelvesSet == null || !cData.visitedShelvesSet.Contains(targetShelf)))
                                {
                                    if (cData.visitedShelvesSet == null) cData.visitedShelvesSet = new HashSet<PlacedFurnitureController>();
                                    cData.visitedShelvesSet.Add(targetShelf);
                                    cData.stateWaitTimer = Random.Range(1.8f, 3.2f);
                                    ProcessCustomerShoppingAtShelf(cData, targetShelf);
                                }
                            }

                            // Alışveriş Bittiğinde veya Kasa Alanına Yanaşıldığında:
                            bool isApproachingCheckout = (targetWaypoint.z <= 2.0f || cData.currentWaypointIndex >= cData.waypoints.Count - 6);
                            if (isApproachingCheckout && !cData.isInCashierQueue)
                            {
                                if (cData.totalItemsBought <= 0)
                                {
                                    int customerTier = GetCustomerTier(cData.type);
                                    PlacedFurnitureController backupShelf = FindAlternateStockedShelfForCustomer(cData, customerTier);
                                    if (backupShelf != null)
                                    {
                                        Vector3 backupPos = backupShelf.GetFrontInteractionPosition(0.75f);
                                        cData.waypoints.Insert(cData.currentWaypointIndex, backupPos);
                                        return;
                                    }

                                    // ❌ Raflarda ürün bulunamadı! Sepeti bırak, uyarı göster ve dükkanı terk et:
                                    ShowNoProductsWarning(cData);
                                    ClearCarriedCartOnCustomer(cData);
                                    cData.grabbedCartFromStand = false;

                                    if (StoreQualityManager.Instance != null)
                                    {
                                        StoreQualityManager.Instance.SubtractQualityScore(10, cData.customerObj.transform.position, LocalizationManager.L("Quality_EmptyShelves", "Boş Raflar!", "Empty Shelves!"));
                                    }

                                    cData.waypoints = BuildDirectExitRoute(cData);
                                    cData.currentWaypointIndex = 1;
                                    cData.stateWaitTimer = 1.0f;
                                    return;
                                }
                                else
                                {
                                    // ✅ Sepette ürün var! En uygun açık kasada sıraya gir:
                                    PlacedFurnitureController bestCashier = FindBestCashierForCustomer();
                                    if (bestCashier != null)
                                    {
                                        EnqueueCustomerAtCashier(cData, bestCashier);
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    cData.currentWaypointIndex++;
                }

                if (cData.currentWaypointIndex >= cData.waypoints.Count)
                {
                    DequeueCustomerFromCashier(cData);
                    ClearCarriedCartOnCustomer(cData);

                    if (SocialMediaManager.Instance != null && Random.value < 0.35f && cData.totalItemsBought > 0)
                    {
                        string cName = cData.profileData != null ? cData.profileData.fullName : "Deniz Yıldız";
                        string cEmoji = cData.profileData != null ? cData.profileData.avatarEmoji : "🛒";
                        Color cColor = cData.profileData != null ? cData.profileData.avatarBgColor : new Color(0.20f, 0.60f, 0.85f);
                        bool isVIP = (cData.type.ToString().Contains("VIP") || cData.type.ToString().Contains("Fashion"));

                        string sName = SocialMediaManager.Instance.GetStoreName();
                        if (cData.totalItemsBought > 4)
                        {
                            SocialMediaManager.Instance.AddCustomerTweet(
                                cName, cEmoji, cColor, isVIP, TweetSentiment.Praise,
                                $"Bugün @{sName} dükkanından tam {cData.totalItemsBought} parça harika ürün aldım! Taptaze 🛒🌾",
                                $"Just bought {cData.totalItemsBought} fresh items from @{sName}! Absolutely loving it 🛒🌾"
                            );

                            if (StoreQualityManager.Instance != null)
                            {
                                StoreQualityManager.Instance.AddQualityScore(15, cData.customerObj.transform.position, "Sosyal Medya Övgüsü!");
                            }
                        }
                        else
                        {
                            SocialMediaManager.Instance.AddCustomerTweet(
                                cName, cEmoji, cColor, isVIP, TweetSentiment.Praise,
                                $"@{sName} dükkanına uğradım, reyonlar ve fiyatlar harika görünüyordu! 🌿👍",
                                $"Stopped by @{sName}, shelves and prices were great! 🌿👍"
                            );

                            if (StoreQualityManager.Instance != null)
                            {
                                StoreQualityManager.Instance.AddQualityScore(10, cData.customerObj.transform.position, "Müşteri Memnuniyeti!");
                            }
                        }
                    }

                    if (!cData.hasVehicle)
                    {
                        // SADECE VE SADECE EN SOL DESPAWN NOKTASINA (X = -85.0f) ULAŞTIĞINDA YOK OLUR!
                        Destroy(cData.customerObj);
                        activeCustomers.RemoveAt(index);
                    }
                    return;
                }
                targetWaypoint = cData.waypoints[cData.currentWaypointIndex];
                toTarget = targetWaypoint - currentPos;
            }

            float moveSpeed = cData.visitedCustomerServiceDesk ? (WALK_SPEED * 1.25f) : WALK_SPEED;
            Vector3 moveDir = toTarget.normalized;
            float stepDist = moveSpeed * deltaTime;
            Vector3 avoidanceDir = CalculateCustomerAvoidanceDirection(cData.customerObj, currentPos, moveDir, stepDist);

            if (avoidanceDir != Vector3.zero) cData.customerObj.transform.rotation = Quaternion.RotateTowards(cData.customerObj.transform.rotation, Quaternion.LookRotation(avoidanceDir), 360f * deltaTime);
            cData.customerObj.transform.position = Vector3.MoveTowards(currentPos, currentPos + avoidanceDir, stepDist);
            cData.walkCycleTimer += deltaTime * (cData.visitedCustomerServiceDesk ? 10.6f : 8.5f);
            AnimateHumanLimbs(cData, Mathf.Sin(cData.walkCycleTimer) * 26.0f);
        }

        private static bool IsSolidObstacle(Collider col, GameObject selfObj)
        {
            if (col == null || col.isTrigger) return false;
            if (selfObj != null && (col.gameObject == selfObj || col.transform.IsChildOf(selfObj.transform))) return false;

            string n = col.name.ToLower();

            // Turnike, bariyer kolları, yer çizgileri ve zemin sınırları yayaların hareketini engellemez:
            if (n.Contains("barrier") || n.Contains("turnstile") || n.Contains("line") || n.Contains("divider") || n.Contains("housing") || n.Contains("border"))
            {
                return false;
            }

            // SADECE VE SADECE GERÇEK DUVARLAR (Store Walls, Outer Walls, Buildings, Facades, Partitions) KATI ENGELDİR!
            if (n.Contains("wall") || n.Contains("duvar") || n.Contains("building") ||
                n.Contains("facade") || n.Contains("partition") ||
                n.Contains("outerwall") || n.Contains("storewall") || n.Contains("storagewall") ||
                n.Contains("roomwall"))
            {
                return true;
            }

            // Dükkan içindeki raflar, mobilyalar, stantlar, kasalar, koliler, paletler, müşteriler ve personeller içinden rahatça geçilebilir.
            return false;
        }

        private Vector3 CalculateCustomerAvoidanceDirection(GameObject customerObj, Vector3 currentPos, Vector3 desiredDir, float stepDist)
        {
            if (desiredDir == Vector3.zero || customerObj == null) return desiredDir;

            float checkRadius = 0.22f;
            float checkDistance = Mathf.Max(0.55f, stepDist + 0.20f);
            Vector3 rayStart = currentPos + Vector3.up * 0.5f;

            RaycastHit[] hits = Physics.SphereCastAll(rayStart, checkRadius, desiredDir, checkDistance);
            bool hitObstacle = false;
            Vector3 hitNormal = Vector3.zero;

            if (hits != null && hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (IsSolidObstacle(hit.collider, customerObj))
                    {
                        hitObstacle = true;
                        hitNormal = hit.normal;
                        break;
                    }
                }
            }

            if (!hitObstacle) return desiredDir;

            float[] checkAngles = new float[] { 25f, -25f, 50f, -50f, 75f, -75f, 100f, -100f };
            foreach (float angle in checkAngles)
            {
                Vector3 testDir = Quaternion.Euler(0f, angle, 0f) * desiredDir;
                RaycastHit[] testHits = Physics.SphereCastAll(rayStart, checkRadius, testDir, checkDistance * 0.85f);
                bool testHitObstacle = false;

                if (testHits != null && testHits.Length > 0)
                {
                    foreach (var th in testHits)
                    {
                        if (IsSolidObstacle(th.collider, customerObj))
                        {
                            testHitObstacle = true;
                            break;
                        }
                    }
                }

                if (!testHitObstacle)
                {
                    return testDir.normalized;
                }
            }

            if (hitNormal != Vector3.zero)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(desiredDir, hitNormal).normalized;
                if (slideDir.sqrMagnitude > 0.01f)
                {
                    return slideDir;
                }
            }

            return desiredDir;
        }

        private PlacedFurnitureController FindNearestShelfToPosition(Vector3 pos)
        {
            var allFurniture = PlacedFurnitureController.AllPlacedFurniture;
            PlacedFurnitureController nearest = null;
            float minDistance = 2.5f;
            int count = allFurniture.Count;

            for (int i = 0; i < count; i++)
            {
                var f = allFurniture[i];
                if (f == null) continue;
                if (f.FurnitureType == FurnitureType.Shelf || f.FurnitureType == FurnitureType.Fridge || f.FurnitureType == FurnitureType.Freezer || f.FurnitureType == FurnitureType.BakeryCounter || f.FurnitureType == FurnitureType.ProduceShelf || f.FurnitureType == FurnitureType.CosmeticShelf || f.FurnitureType == FurnitureType.ElectronicsShelf || f.FurnitureType == FurnitureType.ButcherCounter)
                {
                    float dist = Vector3.Distance(pos, f.GetFrontInteractionPosition(1.2f));
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = f;
                    }
                }
            }
            return nearest;
        }

        private void ProcessCustomerShoppingAtShelf(ActiveCustomerData cData, PlacedFurnitureController shelf)
        {
            if (cData == null || cData.customerObj == null || shelf == null || shelf.rows == null) return;

            int customerTier = GetCustomerTier(cData.type);
            List<ShelfRowData> populatedRows = new List<ShelfRowData>();
            foreach (var r in shelf.rows)
            {
                if (r != null && !r.IsEmpty && r.currentStock > 0)
                {
                    if (IsProductMatchingCustomerTier(r.productName, customerTier))
                    {
                        populatedRows.Add(r);
                    }
                }
            }

            if (populatedRows.Count == 0) return;

            // Kendi seviyesindeki ürünlere öncelik ver (ör. Tier 2 müşterisi 2. seviye ürünleri öncelikli talep etsin)
            populatedRows.Sort((a, b) =>
            {
                int aLvl = GetProductLevel(a.productName);
                int bLvl = GetProductLevel(b.productName);
                int aScore = (aLvl == customerTier) ? 10 : aLvl;
                int bScore = (bLvl == customerTier) ? 10 : bLvl;
                return bScore.CompareTo(aScore);
            });

            int rowsToPick = Mathf.Min(Random.Range(1, 3), populatedRows.Count);
            for (int i = 0; i < rowsToPick; i++)
            {
                var rData = populatedRows[i];
                int buyCount = cData.visitedCustomerServiceDesk ? Random.Range(2, 5) : Random.Range(1, 4);
                buyCount = Mathf.Min(buyCount, rData.currentStock);

                if (buyCount > 0)
                {
                    rData.currentStock = Mathf.Max(0, rData.currentStock - buyCount);
                    shelf.UpdateRow3DProductMeshes(rData.rowId);

                    int itemUnitPrice = (rData.unitPrice > 0) ? Mathf.RoundToInt(rData.unitPrice) : 25;
                    int cost = itemUnitPrice * buyCount;

                    cData.totalCartValue += cost;
                    cData.totalItemsBought += buyCount;
                    cData.isShopping = true;

                    if (cData.hasShoppingCart)
                    {
                        AddProductItemToCarriedCart(cData, rData.productName, buyCount);
                    }

                    ShowShoppingPickPopup(cData.customerObj.transform.position, $"🛒 {rData.productName} ({buyCount} Adet)");

                    if (StoreQualityManager.Instance != null && Random.value < 0.40f)
                    {
                        StoreQualityManager.Instance.AddQualityScore(2, cData.customerObj.transform.position, "Taze Ürün!");
                    }
                }
            }

            if (StoreCleanlinessManager.Instance != null && cData.customerObj != null && Random.value < 0.25f)
            {
                StoreCleanlinessManager.Instance.TrySpawnCustomerTrash(cData.customerObj.transform.position);
            }
        }

        private void ShowShoppingPickPopup(Vector3 pos, string text)
        {
            GameObject popupObj = new GameObject("Popup_ShoppingPick");
            popupObj.transform.position = pos + Vector3.up * 1.95f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 55;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 65f);
            popupObj.transform.localScale = Vector3.one * 0.011f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = UIStyleUtility.GetGlobalFont(22);
            txt.text = text;
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1.0f, 0.95f, 0.40f); // Parlak açık sarı-beyaz renk

            // Arka plan kutusu olmadan yazının net görünmesi için belirgin siyah kontur (Outline)
            UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.8f, -1.8f);

            Destroy(popupObj, 1.4f);
        }

        private void AnimateHumanLimbs(ActiveCustomerData cData, float angle)
        {
            if (cData.leftLimbs != null)
            {
                foreach (var l in cData.leftLimbs)
                {
                    if (l != null) l.localRotation = Quaternion.Euler(angle, 0f, 0f);
                }
            }
            if (cData.rightLimbs != null)
            {
                foreach (var r in cData.rightLimbs)
                {
                    if (r != null) r.localRotation = Quaternion.Euler(-angle, 0f, 0f);
                }
            }
        }

        private void ResetLimbsToRest(ActiveCustomerData cData)
        {
            if (cData.leftLimbs != null)
            {
                foreach (var l in cData.leftLimbs)
                {
                    if (l != null) l.localRotation = Quaternion.identity;
                }
            }
            if (cData.rightLimbs != null)
            {
                foreach (var r in cData.rightLimbs)
                {
                    if (r != null) r.localRotation = Quaternion.identity;
                }
            }
        }
    }
}
