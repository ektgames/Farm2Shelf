using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

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
        }

        private readonly bool[] occupiedParkingSlots = new bool[10];
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
            if (shift.Contains("Gündüz")) return (currentHour >= 6 && currentHour < 14);
            if (shift.Contains("Akşam")) return (currentHour >= 14 && currentHour < 22);
            if (shift.Contains("Gece")) return (currentHour >= 22 || currentHour < 6);
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
            float[] slotZCoords = new float[] { 1.0f, 3.8f, 6.6f, 9.4f, 12.2f };

            for (int i = 0; i < 10; i++)
            {
                if (!occupiedParkingSlots[i])
                {
                    int row = i % 5;
                    float z = slotZCoords[row];
                    float x = (i < 5) ? -36.4f : -17.6f; // P1-P5 sol kutu (-36.4m), P6-P10 sağ kutu (-17.6m)
                    slotPos = new Vector3(x, 0.05f, z);
                    return i;
                }
            }
            return -1;
        }



        private void Update()
        {
            // Dükkan Açık mı Kontrol Et
            bool isStoreOpen = (StoreStatusManager.Instance != null && StoreStatusManager.Instance.IsOpen);
            int currentHour = TimeManager.Instance != null ? TimeManager.Instance.Hour : 6;

            if (isStoreOpen)
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
            // 06:00 - 07:59 (Erken Sabah Açılışı - Seyrek & Sakin)
            if (hour >= 6 && hour < 8)
            {
                spawnInterval = 12.0f;
                maxActiveCustomers = 3;
                return true;
            }
            // 08:00 - 09:59 (Sabah İşe & Okula Gidiş Yoğunluğu)
            else if (hour >= 8 && hour < 10)
            {
                spawnInterval = 5.0f;
                maxActiveCustomers = 7;
                return true;
            }
            // 10:00 - 11:59 (Kuşluk Vakti - Standart Akış)
            else if (hour >= 10 && hour < 12)
            {
                spawnInterval = 8.0f;
                maxActiveCustomers = 5;
                return true;
            }
            // 12:00 - 13:59 (Öğle Molası Alışveriş Zirvesi - YOĞUN)
            else if (hour >= 12 && hour < 14)
            {
                spawnInterval = 4.0f;
                maxActiveCustomers = 9;
                return true;
            }
            // 14:00 - 16:59 (Öğleden Sonra - Normal Düzey)
            else if (hour >= 14 && hour < 17)
            {
                spawnInterval = 7.5f;
                maxActiveCustomers = 6;
                return true;
            }
            // 17:00 - 20:29 (🔥 MESAİ ÇIKIŞI ZİRVE YOĞUNLUĞU - PEAK RUSH HOUR 🔥)
            else if (hour >= 17 && hour < 21)
            {
                spawnInterval = 2.8f; // Hızlı gelen müşteri akını!
                maxActiveCustomers = 13; // Mağaza tıklım tıklım!
                return true;
            }
            // 21:00 - 22:29 (Akşam Sonu Yavaşlayan Akış)
            else if (hour >= 21 && hour < 23)
            {
                spawnInterval = 10.0f;
                maxActiveCustomers = 4;
                return true;
            }
            // 22:30 - 23:59 (Gece Kapanış Sakinliği)
            else
            {
                spawnInterval = 16.0f;
                maxActiveCustomers = 2;
                return true;
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
                    CustomerType.L1_SportsMan
                };

                if (currentLevel >= 2)
                {
                    available.Add(CustomerType.L2_OfficeWorker);
                    available.Add(CustomerType.L2_HipsterGuy);
                    available.Add(CustomerType.L2_GymBro);
                    available.Add(CustomerType.L2_DoctorWoman);
                    available.Add(CustomerType.L2_FashionWoman);
                }

                if (currentLevel >= 3)
                {
                    available.Add(CustomerType.L3_CEO_Executive);
                    available.Add(CustomerType.L3_VIP_Influencer);
                    available.Add(CustomerType.L3_RichGentleman);
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
            List<Vector3> route = BuildCustomerShoppingRoute(out bool willVisitDesk, out bool hasCartStand);
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
            CustomerType selectedType = (CustomerType)Random.Range(0, 5);
            List<Vector3> route = BuildBusPassengerShoppingRoute(disembarkPos, out bool willVisitDesk, out bool hasCartStand);
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

        private bool IsShelfStocked(PlacedFurnitureController f)
        {
            if (f == null || f.rows == null) return false;
            bool isStoreShelf = (f.FurnitureType == FurnitureType.Shelf || f.FurnitureType == FurnitureType.Fridge || f.FurnitureType == FurnitureType.Freezer || f.FurnitureType == FurnitureType.BakeryCounter || f.FurnitureType == FurnitureType.ProduceShelf || f.FurnitureType == FurnitureType.CosmeticShelf || f.FurnitureType == FurnitureType.ElectronicsShelf || f.FurnitureType == FurnitureType.ButcherCounter);
            if (!isStoreShelf) return false;

            foreach (var r in f.rows)
            {
                if (r != null && !r.IsUnassigned && !string.IsNullOrEmpty(r.productName) && r.currentStock > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void AddRandomShelfWaypoints(List<Vector3> route, PlacedFurnitureController[] shelves)
        {
            List<PlacedFurnitureController> validShelves = new List<PlacedFurnitureController>();
            if (shelves != null)
            {
                foreach (var s in shelves)
                {
                    if (s != null && IsShelfStocked(s))
                    {
                        validShelves.Add(s);
                    }
                }
            }

            if (validShelves.Count > 0)
            {
                // Karıştır (Fisher-Yates Shuffle)
                for (int i = 0; i < validShelves.Count; i++)
                {
                    var temp = validShelves[i];
                    int randIdx = Random.Range(i, validShelves.Count);
                    validShelves[i] = validShelves[randIdx];
                    validShelves[randIdx] = temp;
                }

                // Oyundaki Yoğunluk Saatlerine Göre Mantıklı Ürün Alışveriş Adet Dağılımı:
                // - Sakin Saatler (08:00 - 10:59 & 21:00 - 22:00): 1 - 4 Parça Ürün
                // - Normal Saatler (11:00 - 15:59): 3 - 6 Parça Ürün
                // - Zirve / Yoğun Akşam Saatleri (16:00 - 20:59): 5 - 9 Parça Ürün (Geniş Dolu Sepetler!)
                int currentHour = (TimeManager.Instance != null) ? TimeManager.Instance.Hour : 14;
                int targetCount = 1;
                float roll = Random.value;

                if (currentHour >= 16 && currentHour <= 20)
                {
                    // Yoğun İş Çıkışı / Akşam (5, 6, 7, 8, 9 Ürün)
                    if (roll < 0.15f) targetCount = 5;
                    else if (roll < 0.35f) targetCount = 6;
                    else if (roll < 0.60f) targetCount = 7;
                    else if (roll < 0.80f) targetCount = 8;
                    else targetCount = 9;
                }
                else if (currentHour >= 11 && currentHour < 16)
                {
                    // Normal Gündüz / Öğle (3, 4, 5, 6 Ürün)
                    if (roll < 0.25f) targetCount = 3;
                    else if (roll < 0.60f) targetCount = 4;
                    else if (roll < 0.85f) targetCount = 5;
                    else targetCount = 6;
                }
                else
                {
                    // Sakin Sabah / Gece (1, 2, 3, 4 Ürün)
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

        private List<Vector3> BuildCustomerShoppingRoute(out bool willVisitServiceDesk, out bool hasCartStand)
        {
            List<Vector3> route = new List<Vector3>();

            route.Add(new Vector3(15.0f, 0.05f, -4.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -4.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -1.0f));

            PlacedFurnitureController cartStand = GetActiveShoppingCartStand();
            hasCartStand = (cartStand != null);

            if (hasCartStand)
            {
                route.Add(cartStand.GetFrontInteractionPosition(1.0f));
            }
            else
            {
                route.Add(new Vector3(-5.0f, 0.05f, -1.0f));
                route.Add(new Vector3(-5.0f, 0.05f, -4.5f));
                route.Add(new Vector3(-15.0f, 0.05f, -4.5f));
                route.Add(new Vector3(-75.0f, 0.05f, -4.5f));
                willVisitServiceDesk = false;
                return route;
            }

            PlacedFurnitureController[] shelves = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            PlacedFurnitureController serviceDesk = null;

            foreach (var s in shelves)
            {
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

            // Müşteri Mağazadaki Rastgele 1-3 Farklı Rafa Uğrar!
            AddRandomShelfWaypoints(route, shelves);

            Vector3 checkoutPos = new Vector3(-15.0f, 0.05f, 2.5f);
            foreach (var s in shelves)
            {
                if (s != null && s.FurnitureType == FurnitureType.Cashier)
                {
                    checkoutPos = s.GetFrontInteractionPosition(1.0f);
                    break;
                }
            }
            route.Add(checkoutPos);

            route.Add(new Vector3(-5.0f, 0.05f, -1.0f));
            route.Add(new Vector3(-5.0f, 0.05f, -4.5f));
            route.Add(new Vector3(-20.0f, 0.05f, -4.5f));
            route.Add(new Vector3(-75.0f, 0.05f, -4.5f));

            return route;
        }

        private List<Vector3> BuildBusPassengerShoppingRoute(Vector3 disembarkPos, out bool willVisitServiceDesk, out bool hasCartStand)
        {
            List<Vector3> route = new List<Vector3>();

            // 1. Geliş Rotası: Otobüsten İn (4.5, -5.8) -> Yaya Geçidi -> Cam Giriş Kapısı (-5.0, -1.8)
            route.Add(disembarkPos);
            route.Add(new Vector3(-2.5f, 0.05f, -5.8f)); // Yaya geçidi başlangıcı
            route.Add(new Vector3(-5.0f, 0.05f, -5.8f)); // Yaya geçidi tam ortası
            route.Add(new Vector3(-5.0f, 0.05f, -1.8f)); // Cam Giriş Kapısı

            PlacedFurnitureController cartStand = GetActiveShoppingCartStand();
            hasCartStand = (cartStand != null);

            if (hasCartStand)
            {
                route.Add(cartStand.GetFrontInteractionPosition(1.0f));
            }
            else
            {
                route.Add(new Vector3(-5.0f, 0.05f, -1.8f));
                route.Add(new Vector3(-5.0f, 0.05f, -5.8f));  // Kaldırıma in
                route.Add(new Vector3(-18.0f, 0.05f, -5.8f)); // Sol tarafa doğru yürü
                route.Add(new Vector3(-38.0f, 0.05f, -5.8f)); // Yok olma noktası
                willVisitServiceDesk = false;
                return route;
            }

            PlacedFurnitureController[] shelves = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            PlacedFurnitureController serviceDesk = null;

            foreach (var s in shelves)
            {
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

            // Otobüs Yolcusu Mağazadaki Rastgele 1-3 Farklı Rafa Uğrar!
            AddRandomShelfWaypoints(route, shelves);

            Vector3 checkoutPos = new Vector3(-15.0f, 0.05f, 2.5f);
            foreach (var s in shelves)
            {
                if (s != null && s.FurnitureType == FurnitureType.Cashier)
                {
                    checkoutPos = s.GetFrontInteractionPosition(1.0f);
                    break;
                }
            }
            route.Add(checkoutPos);

            // ÇIKIŞ ROTASI: Çıkış kapısından çıkıp SOL TARAFA doğru yürüyüp yok olma yerinde yok olma!
            route.Add(new Vector3(-5.0f, 0.05f, -1.8f));  // Cam Çıkış Kapısı
            route.Add(new Vector3(-5.0f, 0.05f, -5.8f));  // Ana Kaldırım
            route.Add(new Vector3(-18.0f, 0.05f, -5.8f)); // Sol tarafa doğru yürü
            route.Add(new Vector3(-38.0f, 0.05f, -5.8f)); // Yok olma noktası

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
            PlacedFurnitureController[] furnitureList = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            foreach (var f in furnitureList)
            {
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
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            txt.text = "🛒 Alışveriş Sepeti Yok!";
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.95f, 0.25f, 0.25f);

            Destroy(popupObj, 2.5f);
        }



        private List<Vector3> BuildCustomerShoppingRoute(out bool willVisitServiceDesk)
        {
            return BuildCustomerShoppingRoute(out willVisitServiceDesk, out _);
        }

        private List<Vector3> BuildCustomerShoppingRoute()
        {
            return BuildCustomerShoppingRoute(out _);
        }

        private void UpdateActiveCustomers(float deltaTime)
        {
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
        }

        private List<Vector3> BuildVehicleCustomerWalkingRoute(Vector3 slotPos, int slotIndex, out bool willVisitServiceDesk, out bool hasCartStand)
        {
            List<Vector3> route = new List<Vector3>();

            Vector3 driverDoorPos = slotPos + (slotIndex < 5 ? Vector3.right * 1.2f : Vector3.left * 1.2f);
            route.Add(driverDoorPos);                             // 0. Sürücü Kapısı İniş Noktası
            route.Add(new Vector3(-25.5f, 0.05f, slotPos.z));     // 1. Otopark İç Yolu (Duvara takılmayı %100 önler)
            route.Add(new Vector3(-25.5f, 0.05f, -4.5f));          // 2. Turnikelerin Yanı (Giriş Turnikesinden Kaldırıma Geçiş)
            route.Add(new Vector3(-5.0f, 0.05f, -4.5f));           // 3. Cam Kapı Dış Giriş Kaldırımı
            route.Add(new Vector3(-5.0f, 0.05f, -1.0f));           // 4. Ana Fuaye İç Giriş

            PlacedFurnitureController cartStand = GetActiveShoppingCartStand();
            hasCartStand = (cartStand != null);

            if (hasCartStand)
            {
                route.Add(cartStand.GetFrontInteractionPosition(1.0f));
            }
            else
            {
                // Alışveriş sepeti stantı yoksa müşteri alışveriş yapamaz! Kapıda bekleyip arabasına döner.
                route.Add(new Vector3(-5.0f, 0.05f, -1.0f));
                route.Add(new Vector3(-5.0f, 0.05f, -4.5f));
                route.Add(new Vector3(-25.5f, 0.05f, -4.5f));
                route.Add(new Vector3(-25.5f, 0.05f, slotPos.z));
                route.Add(driverDoorPos);
                willVisitServiceDesk = false;
                return route;
            }

            PlacedFurnitureController[] shelves = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            PlacedFurnitureController serviceDesk = null;

            foreach (var s in shelves)
            {
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

            AddRandomShelfWaypoints(route, shelves);

            Vector3 checkoutPos = new Vector3(-15.0f, 0.05f, 2.5f);
            foreach (var s in shelves)
            {
                if (s != null && s.FurnitureType == FurnitureType.Cashier)
                {
                    checkoutPos = s.GetFrontInteractionPosition(1.0f);
                    break;
                }
            }
            route.Add(checkoutPos);

            route.Add(new Vector3(-5.0f, 0.05f, -1.0f));           // Ana Fuaye Çıkış
            route.Add(new Vector3(-5.0f, 0.05f, -4.5f));           // Cam Kapı Dış Çıkış
            route.Add(new Vector3(-25.5f, 0.05f, -4.5f));          // Turnikelerin Yanı (Kaldırımdan Otoparka Geçiş)
            route.Add(new Vector3(-25.5f, 0.05f, slotPos.z));     // Otopark İç Yolu
            route.Add(driverDoorPos);                             // Sürücü Kapısı (Arabaya Biniş)

            return route;
        }

        private void UpdateVehicleCustomer(ActiveCustomerData cData, float deltaTime, int index)
        {
            if (cData.vehicleObj == null)
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
                    // 4. Park Yerine Hizalanıp Durma (P1..P5: -90°, P6..P10: 90°)
                    float targetAngle = (cData.parkingSlotIndex < 5) ? -90f : 90f;
                    MoveVehicle(cData, currentCarPos, cData.parkedSlotPos, Quaternion.Euler(0f, targetAngle, 0f), deltaTime);

                    if (Vector3.Distance(currentCarPos, cData.parkedSlotPos) < 0.3f)
                    {
                        cData.vehicleObj.transform.position = cData.parkedSlotPos;
                        cData.vehicleObj.transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);

                        // Sürücü Araçtan İner:
                        Vector3 driverDoorPos = cData.parkedSlotPos + (cData.parkingSlotIndex < 5 ? Vector3.right * 1.2f : Vector3.left * 1.2f);
                        if (cData.customerObj != null)
                        {
                            cData.customerObj.transform.position = driverDoorPos;
                            cData.customerObj.SetActive(true);
                        }

                        cData.waypoints = BuildVehicleCustomerWalkingRoute(cData.parkedSlotPos, cData.parkingSlotIndex, out cData.isVisitingCustomerService, out cData.hasCartStand);
                        cData.currentWaypointIndex = 1;
                        cData.drivePhase = VehicleDrivePhase.EngineOffShoppingOnFoot;
                    }
                    break;

                case VehicleDrivePhase.EngineOffShoppingOnFoot:
                    // 5. Müşteri Yürüyerek Dükkana Girer, Alışveriş Yapar ve Arabasına Dönüş Yapar
                    UpdatePedestrianCustomer(cData, deltaTime, index);

                    if (cData.currentWaypointIndex >= cData.waypoints.Count - 1)
                    {
                        // Sürücü Arabaya Biner:
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
                        if (cData.parkingSlotIndex >= 0 && cData.parkingSlotIndex < 10)
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
            PlacedFurnitureController[] allFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            PlacedFurnitureController bestCashier = null;
            int minQueueCount = int.MaxValue;

            foreach (var f in allFurniture)
            {
                if (f != null && f.FurnitureType == FurnitureType.Cashier)
                {
                    if (!cashierQueues.ContainsKey(f))
                    {
                        cashierQueues[f] = new List<ActiveCustomerData>();
                    }

                    cashierQueues[f].RemoveAll(c => c == null || c.customerObj == null);

                    int qCount = cashierQueues[f].Count;
                    if (qCount < minQueueCount)
                    {
                        minQueueCount = qCount;
                        bestCashier = f;
                    }
                }
            }

            return bestCashier;
        }

        public static Vector3 GetCashierSlotWorldPosition(PlacedFurnitureController cashier, int slotIndex)
        {
            if (cashier == null) return Vector3.zero;

            // Kasa Mobilyasının Önündeki Turkuaz Ok Çizgisi Üzerinde Hizalama (Straight Queue on Arrow Line):
            // Slot 0 (Kasa Tezgahı Ödeme Noktası): Local (-0.35, 0.05, -0.75)
            // Slot 1, 2, 3, 4... (Sıradaki Müşteriler - Ok Çizgisi Üzerinde Sola Doğru): Local (-0.35 - slotIndex * 0.90, 0.05, -0.75)
            float offsetLeft = slotIndex * 0.90f;
            return cashier.transform.TransformPoint(new Vector3(-0.35f - offsetLeft, 0.05f, -0.75f));
        }

        private void EnqueueCustomerAtCashier(ActiveCustomerData cData, PlacedFurnitureController cashier)
        {
            if (cData == null || cashier == null) return;

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

            // Eğer slot 0'daki müşteri kasadan aşırı uzaktaysa (> 4.5m) ama sırada bekleyen daha yakın müşteri varsa, uzaktaki müşteriyi kuyruktan çıkar!
            if (qList.Count > 1)
            {
                ActiveCustomerData headCustomer = qList[0];
                float headDist = Vector3.Distance(headCustomer.customerObj.transform.position, cashier.transform.position);
                if (headDist > 4.5f)
                {
                    headCustomer.isInCashierQueue = false;
                    headCustomer.assignedCashier = null;
                    headCustomer.queueSlotIndex = -1;
                    qList.RemoveAt(0);
                }
            }

            for (int i = 0; i < qList.Count; i++)
            {
                ActiveCustomerData c = qList[i];
                int oldIndex = c.queueSlotIndex;
                c.queueSlotIndex = i;

                Vector3 slotPos = GetCashierSlotWorldPosition(cashier, i);

                if (c.waypoints != null && c.waypoints.Count > 0)
                {
                    c.waypoints[c.waypoints.Count - 1] = slotPos;
                }

                // Öne geçen (ör. slot 0'a ilerleyen) müşterinin duraksamasını kaldır ki hemen ödemeye geçsin!
                if (i == 0 && oldIndex != 0)
                {
                    c.stateWaitTimer = 0f;
                }
            }
        }

        private void ShowPaymentPopup(Vector3 pos, string text = "+Ödeme Yapıldı 💳")
        {
            GameObject popupObj = new GameObject("Popup_Payment");
            popupObj.transform.position = pos + Vector3.up * 1.8f;

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
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
            txt.text = text;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.20f, 0.85f, 0.35f);

            Destroy(popupObj, 1.5f);
        }

        private void UpdatePedestrianCustomer(ActiveCustomerData cData, float deltaTime, int index)
        {
            if (cData.customerObj == null || cData.waypoints == null || cData.waypoints.Count == 0) return;
            if (cData.stateWaitTimer > 0f)
            {
                cData.stateWaitTimer -= deltaTime;
                ResetLimbsToRest(cData);

                // Kasada beklerken yüzünü ok yönünde ödeme noktasına döner
                if (cData.isInCashierQueue && cData.assignedCashier != null)
                {
                    Vector3 faceDir;
                    if (cData.queueSlotIndex == 0)
                    {
                        // Slot 0: Kasa Tezgahına Bak (Masa İleri Yönü)
                        faceDir = cData.assignedCashier.transform.forward;
                    }
                    else
                    {
                        // Slot 1, 2, 3...: Ok çizgisi üzerinde ödeme yapan müşteriye / ileriye bak
                        faceDir = cData.assignedCashier.transform.right;
                    }

                    faceDir.y = 0f;
                    if (faceDir != Vector3.zero)
                    {
                        cData.customerObj.transform.rotation = Quaternion.RotateTowards(cData.customerObj.transform.rotation, Quaternion.LookRotation(faceDir), 360f * deltaTime);
                    }
                }
                else
                {
                    PlacedFurnitureController[] allFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
                    PlacedFurnitureController nearest = null;
                    float minDist = 3.0f;
                    foreach (var f in allFurniture)
                    {
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

                // Ödeme tamamlandığında sepeti bırak, kuyruktan ayrıl ve çıkışa yürü!
                if (cData.isCheckingOut && cData.stateWaitTimer <= 0f)
                {
                    PlacedFurnitureController activeCashier = cData.assignedCashier;
                    ClearCarriedCartOnCustomer(cData);
                    DequeueCustomerFromCashier(cData);

                    cData.isCheckingOut = false;

                    List<Vector3> exitWaypoints = new List<Vector3>();
                    exitWaypoints.Add(cData.customerObj.transform.position);

                    if (activeCashier != null)
                    {
                        // 1. Kasadan açık koridora doğru temiz adım atma noktası (Kasaya takılmayı %100 önler)
                        Vector3 stepOutPos = activeCashier.transform.TransformPoint(new Vector3(0f, 0.05f, -1.85f));
                        exitWaypoints.Add(stepOutPos);
                    }

                    exitWaypoints.Add(new Vector3(-5.0f, 0.05f, -1.0f)); // Dükkan Ana Cam Kapı Fuayesi
                    exitWaypoints.Add(new Vector3(-5.0f, 0.05f, -4.5f)); // Dış Kaldırım
                    exitWaypoints.Add(new Vector3(-15.0f, 0.05f, -4.5f));
                    exitWaypoints.Add(new Vector3(-75.0f, 0.05f, -4.5f)); // Despawn

                    cData.waypoints = exitWaypoints;
                    cData.currentWaypointIndex = 1;
                    cData.stateWaitTimer = 0f;
                }
                return;
            }

            Vector3 currentPos = cData.customerObj.transform.position;
            Vector3 targetWaypoint = cData.waypoints[cData.currentWaypointIndex];
            Vector3 toTarget = targetWaypoint - currentPos;
            float distToTarget = toTarget.magnitude;

            if (distToTarget < 0.6f)
            {
                PlacedFurnitureController cartStand = GetActiveShoppingCartStand();

                if (!cData.hasCartStand || cartStand == null)
                {
                    // ❌ DÜKKANDA ALIŞVERİŞ SEPETİ YOK!
                    // Kapıdan içeri adım atıldığı anda (z >= -2.5f ve z <= 0.0f) durup başının üstünde uyarı ver ve dükkandan çık!
                    if (!cData.hasNoCartWarningShown && (targetWaypoint.z >= -2.5f && targetWaypoint.z <= 0.0f))
                    {
                        cData.hasNoCartWarningShown = true;
                        ShowNoShoppingCartWarning(cData.customerObj.transform.position);
                        cData.stateWaitTimer = 2.5f;
                    }
                }
                else
                {
                    // ✅ DÜKKANDA SEPET STANTI VAR:
                    Vector3 cartStandInteractionPos = cartStand.GetFrontInteractionPosition(1.0f);
                    bool isAtCartStand = (Vector3.Distance(targetWaypoint, cartStandInteractionPos) < 0.65f);

                    // 1. TAM OLARAK SEPET STANTININ ÖNÜNE ULAŞILDIĞINDA SEPET ALMA!
                    if (!cData.grabbedCartFromStand && isAtCartStand)
                    {
                        cData.grabbedCartFromStand = true;
                        cData.stateWaitTimer = 1.0f;
                        CreateCarriedShoppingCartOnCustomer(cData);
                    }
                    else if (cData.grabbedCartFromStand)
                    {
                        PlacedFurnitureController serviceDesk = null;
                        PlacedFurnitureController cashierFurniture = null;
                        PlacedFurnitureController[] allFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
                        foreach (var f in allFurniture)
                        {
                            if (f != null)
                            {
                                if (f.FurnitureType == FurnitureType.CustomerServiceDesk) serviceDesk = f;
                                else if (f.FurnitureType == FurnitureType.Cashier) cashierFurniture = f;
                            }
                        }

                        bool isAtServiceDesk = (serviceDesk != null && Vector3.Distance(targetWaypoint, serviceDesk.GetFrontInteractionPosition(1.0f)) < 0.65f);
                        
                        // Kasa Alanına Yanaşıldığında veya Alışveriş Sonlandığında Otomatik En Uygun Kasa Kuyruğuna Gir!
                        // SADECE VE SADECE ALIVERİŞ SEPETİ OLAN VE KASAYA YANAŞAN / ALIŞVERİŞİ BİTEN MÜŞTERİLER KASAYA DİZİLİR!
                        float distToBestCashier = 999f;
                        PlacedFurnitureController bestCashier = FindBestCashierForCustomer();
                        if (bestCashier != null)
                        {
                            distToBestCashier = Vector3.Distance(cData.customerObj.transform.position, bestCashier.transform.position);
                        }

                        bool isNearCashierOrDone = (distToBestCashier < 6.5f || targetWaypoint.z <= 2.0f || (cData.visitedShelvesSet != null && cData.visitedShelvesSet.Count >= 1));

                        if (cData.grabbedCartFromStand && cData.carriedCartObj != null && isNearCashierOrDone && !cData.isInCashierQueue)
                        {
                            if (bestCashier != null)
                            {
                                EnqueueCustomerAtCashier(cData, bestCashier);
                            }
                        }

                        // Eğer sepeti yoksa veya kasadan aşırı uzaklaştıysa (> 10m) ANINDA kuyruktan çıkar!
                        if (cData.isInCashierQueue)
                        {
                            float distToCurrentCashier = (cData.assignedCashier != null) ? Vector3.Distance(cData.customerObj.transform.position, cData.assignedCashier.transform.position) : 999f;
                            if (!cData.grabbedCartFromStand || cData.carriedCartObj == null || distToCurrentCashier > 10.0f)
                            {
                                DequeueCustomerFromCashier(cData);
                            }
                        }

                        Vector3 targetSlotPos = (cData.assignedCashier != null) ? GetCashierSlotWorldPosition(cData.assignedCashier, cData.queueSlotIndex) : Vector3.zero;
                        float distToSlot = (cData.assignedCashier != null) ? Vector3.Distance(cData.customerObj.transform.position, targetSlotPos) : 999f;
                        bool isAtCashierSlot = (cData.assignedCashier != null && distToSlot < (cData.queueSlotIndex == 0 ? 1.35f : 0.85f));

                        // 2. Müşteri Hizmetleri Masasına Uğrama
                        if (cData.isVisitingCustomerService && !cData.visitedCustomerServiceDesk && isAtServiceDesk)
                        {
                            cData.visitedCustomerServiceDesk = true;
                            cData.stateWaitTimer = 1.5f;
                        }
                        // 3. Raftan Alışveriş Yapma (Farklı Rafları Gezme ve Çeşit Çeşit Ürün Toplama)
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
                        // 4. Kasada Kuyruk & Ödeme Yapma
                        else if (cData.isInCashierQueue && (isAtCashierSlot || (cData.queueSlotIndex == 0 && distToSlot < 1.45f)))
                        {
                            if (cData.queueSlotIndex == 0)
                            {
                                // SADECE VE SADECE KASADA KASİYER VARSA ÖDEME YAPILIR!
                                bool isCashierWorking = (cData.assignedCashier != null && StaffTaskController.IsCashierWorkingAt(cData.assignedCashier));
                                if (isCashierWorking)
                                {
                                    if (!cData.isCheckingOut)
                                    {
                                        cData.isCheckingOut = true;
                                        cData.stateWaitTimer = 0.3f;
                                        ClearCarriedCartOnCustomer(cData);

                                        int paymentAmount = Mathf.Max(35, cData.totalCartValue);
                                        if (cData.visitedCustomerServiceDesk) paymentAmount += Random.Range(50, 100);

                                        if (EconomyManager.Instance != null) EconomyManager.Instance.AddCredits(paymentAmount);
                                        if (FinanceManager.Instance != null) FinanceManager.Instance.RecordIncome("Satış", $"Müşteri Alışverişi ({cData.totalItemsBought} Parça Ürün)", paymentAmount);

                                        ShowPaymentPopup(cData.customerObj.transform.position, $"+{paymentAmount}C Ödeme Yapıldı 💳");

                                        // KASADA KALİTE PUANI HESAPLAMA (DÜKKAN TEMİZ İSE +15 YILDIZ, KİRLİ İSE -10 YILDIZ!):
                                        if (StoreQualityManager.Instance != null)
                                        {
                                            bool isClean = (StoreCleanlinessManager.Instance == null || StoreCleanlinessManager.Instance.GetNearestTrashItem(cData.customerObj.transform.position, out float trashDist) == null);
                                            if (isClean)
                                            {
                                                StoreQualityManager.Instance.AddQualityScore(15, cData.customerObj.transform.position, "Temiz Dükkan!");
                                            }
                                            else
                                            {
                                                StoreQualityManager.Instance.SubtractQualityScore(10, cData.customerObj.transform.position, "Kirli Dükkan!");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Kasiyer henüz kasaya gelmediyse müşteri sırada bekler
                                    cData.stateWaitTimer = 0.5f;
                                    return;
                                }
                            }
                            else
                            {
                                // KUYRUKTAKİ DİĞER MÜŞTERİLER (SLOT 1, 2, 3...) SIRADA KUSURSUZCA HİZALANIP BEKLER!
                                cData.stateWaitTimer = 0.2f;
                            }
                            return;
                        }
                    }
                }

                cData.currentWaypointIndex++;
                if (cData.currentWaypointIndex >= cData.waypoints.Count)
                {
                    DequeueCustomerFromCashier(cData);
                    ClearCarriedCartOnCustomer(cData);

                    if (SocialMediaManager.Instance != null && Random.value < 0.35f)
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
                        }
                        else
                        {
                            SocialMediaManager.Instance.AddCustomerTweet(
                                cName, cEmoji, cColor, isVIP, TweetSentiment.Praise,
                                $"@{sName} dükkanına uğradım, reyonlar ve fiyatlar harika görünüyordu! 🌿👍",
                                $"Stopped by @{sName}, shelves and prices were great! 🌿👍"
                            );
                        }
                    }

                    if (!cData.hasVehicle) { Destroy(cData.customerObj); activeCustomers.RemoveAt(index); }
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

            // SADECE VE SADECE DUVARLAR (WALLS / BUILDINGS / DIVIDERS) KATI ENGELDİR!
            // Müşteriler dükkan ve bina dış/iç duvarlarından kesinlikle geçemezler!
            if (n.Contains("wall") || n.Contains("duvar") || n.Contains("building") ||
                n.Contains("fence") || n.Contains("facade") || n.Contains("partition") ||
                n.Contains("divider") || n.Contains("boundary") || n.Contains("border") ||
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
            PlacedFurnitureController[] allFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            PlacedFurnitureController nearest = null;
            float minDistance = 2.5f;

            foreach (var f in allFurniture)
            {
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

            List<ShelfRowData> populatedRows = new List<ShelfRowData>();
            foreach (var r in shelf.rows)
            {
                if (r != null && !r.IsEmpty && r.currentStock > 0)
                {
                    populatedRows.Add(r);
                }
            }

            if (populatedRows.Count == 0) return;

            // Raftaki ürün sıralarını karıştır (Fisher-Yates Shuffle) - Tüm ürünlerin (1., 2., 3., 4. raf) eşit satılmasını sağla
            for (int i = 0; i < populatedRows.Count; i++)
            {
                var temp = populatedRows[i];
                int randIdx = Random.Range(i, populatedRows.Count);
                populatedRows[i] = populatedRows[randIdx];
                populatedRows[randIdx] = temp;
            }

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
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
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
