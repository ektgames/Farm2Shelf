using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Farm2Shelf.Environment;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    [Serializable]
    public class TutorialJourneySave
    {
        public int version = 2;
        public List<string> purchasedTypes = new List<string>();
        public List<int> purchasedCounts = new List<int>();
        public List<string> earlyStaff = new List<string>();
        public List<string> plantedSeeds = new List<string>();
        public List<int> plantedCounts = new List<int>();
        public List<int> apps = new List<int>();
        public bool pan, zoom, rotate, bulk, contract, chickenSeen, cowSeen, barnSeen;
        public bool tomato, cucumber, lettuce;
        public bool plantingFinished;
        public int tour;
    }

    public partial class TutorialManager
    {
        public static readonly FurnitureType[] RequiredFurniture = {
            FurnitureType.Shelf, FurnitureType.ProduceShelf, FurnitureType.GourmetShelf,
            FurnitureType.ShoppingCart, FurnitureType.StorageShelf, FurnitureType.Cashier,
            FurnitureType.CustomerServiceDesk, FurnitureType.Fridge, FurnitureType.OrganicFridge
        };
        public static readonly int[] RequiredFurnitureCounts = { 3, 1, 1, 1, 4, 1, 1, 2, 1 };
        public static readonly StaffRole[] RequiredStoreRoles = {
            StaffRole.Kasiyer, StaffRole.Reyoncu, StaffRole.Temizlikçi, StaffRole.Güvenlik, StaffRole.MüşteriHizmetlisi, StaffRole.Maskot
        };
        private readonly Dictionary<FurnitureType, int> journeyPurchases = new Dictionary<FurnitureType, int>();
        private readonly Dictionary<string, int> journeyPlants = new Dictionary<string, int>();
        private readonly HashSet<string> earlyStaffIds = new HashSet<string>();
        public bool ChickenVisited { get; private set; }
        public bool CowVisited { get; private set; }
        public bool BarnVisited { get; private set; }
        public int TourStop { get; private set; }
        public int JourneyStep => (int)CurrentStep;
        public int EarlyRestockers => earlyStaffIds.Count;
        public int Purchased(FurnitureType type) => journeyPurchases.TryGetValue(type, out int count) ? count : 0;
        public int Planted(string id) => journeyPlants.TryGetValue(id, out int count) ? count : 0;
        public int Placed(FurnitureType type) => PlacedFurnitureController.AllPlacedFurniture.Count(f => f != null && f.FurnitureType == type);
        public bool FurnitureBought => RequiredFurniture.Select((type, i) => Purchased(type) >= RequiredFurnitureCounts[i]).All(x => x);
        public bool FurnitureInstalled => RequiredFurniture.Select((type, i) => Placed(type) >= RequiredFurnitureCounts[i]).All(x => x);
        public bool SeedsBought => DidBuyTomatoSeed && DidBuyCucumberSeed && DidBuyLettuceSeed;
        public bool SeedsPlanted => Planted("spring_tomato") >= 10 && Planted("spring_cucumber") >= 10 && Planted("spring_lettuce") >= 10;
        public bool PlantingFinished { get; private set; }
        public void NotifyPlantingFinished()
        {
            if (JourneyStep != 7 || !SeedsPlanted) return;
            PlantingFinished = true;
            CheckCurrentStepCompletion(false);
        }
        public bool StoreHired => RequiredStoreRoles.All(r => GetStoreRoleCount(r) >= (r == StaffRole.Reyoncu ? 4 : 2));

        public int ShiftCount(int group, StaffRole role, bool evening)
        {
            if (StaffManager.Instance == null) return 0;
            var staff = group == 0 ? StaffManager.Instance.GetActiveStaff() :
                group == 1 ? StaffManager.Instance.GetFarmStaffList() : StaffManager.Instance.GetCourierStaffList();
            return staff.Count(s => s != null && s.role == role &&
                StaffManager.NormalizeShift(s.shiftHours).Contains(evening ? "24:00" : "08:00"));
        }
        public bool RoleShiftsReady(int group, StaffRole role, int each = 1) =>
            ShiftCount(group, role, false) >= each && ShiftCount(group, role, true) >= each;
        public bool StoreShiftsReady => RequiredStoreRoles.All(r => RoleShiftsReady(0, r, r == StaffRole.Reyoncu ? 2 : 1));
        public bool FarmersReady => RoleShiftsReady(1, StaffRole.Çiftçi);
        public int TutorialMotorcycles => CourierManager.Instance != null ? CourierManager.Instance.OwnedMotorcycleCount : 0;
        public bool FirstMotorcycleAssigned => TownContractManager.Instance != null && TownContractManager.Instance.IsSlotAssignedToContracts(0);
        public int CourierGenderCount(bool female) => StaffManager.Instance == null ? 0 :
            StaffManager.Instance.GetCourierStaffList().Count(s => s != null && s.isFemale == female);
        public bool CouriersHired => CourierGenderCount(true) >= 2 && CourierGenderCount(false) >= 2;
        public bool CouriersReady => CouriersHired && RoleShiftsReady(2, StaffRole.Kurye, 2);
        public int RecruitmentRemaining(int group, StaffRole role)
        {
            if (JourneyStep == 4 && group == 0)
            {
                int target = Array.IndexOf(RequiredStoreRoles, role) < 0 ? 0 : role == StaffRole.Reyoncu ? 4 : 2;
                return Mathf.Max(0, target - GetStoreRoleCount(role));
            }
            if (JourneyStep == 3 && group == 2 && StaffManager.Instance != null)
                return Mathf.Max(0, 4 - StaffManager.Instance.GetCourierStaffList().Count);
            if (JourneyStep == 7 && group == 1) return Mathf.Max(0, 2 - GetFarmRoleCount(StaffRole.Çiftçi));
            return int.MaxValue;
        }

        public int StockedFurniture(FurnitureType type)
        {
            return PlacedFurnitureController.AllPlacedFurniture.Count(f => f != null && f.FurnitureType == type &&
                f.rows != null && f.rows.Length >= 4 && f.rows.Take(4).All(r => r != null && !r.IsUnassigned && !string.IsNullOrEmpty(r.productName)));
        }

        private bool JourneyComplete(int step)
        {
            switch (step)
            {
                case 1: return DidPanCamera && DidZoomCamera && DidRotateCamera;
                case 2: return ExploredAppsCount >= 8;
                case 3: return TutorialMotorcycles >= 2 && FirstMotorcycleAssigned && HasAcceptedTownContract() && CouriersReady;
                case 4: return StoreHired && StoreShiftsReady && EarlyRestockers >= 2;
                case 5: return FurnitureBought && FurnitureInstalled;
                case 6: return DidPlaceBulkOrder && StockedFurniture(FurnitureType.Shelf) >= 3 && StockedFurniture(FurnitureType.Fridge) >= 2;
                case 7: return FarmersReady && SeedsBought && SeedsPlanted && PlantingFinished;
                case 8: return LivestockManager.Instance != null && LivestockManager.Instance.TotalChickens >= 6 &&
                    LivestockManager.Instance.TotalCows >= 3 && ChickenVisited && CowVisited &&
                    (LivestockCoopModalUI.Instance == null || !LivestockCoopModalUI.Instance.IsModalOpen);
                case 9: return BarnVisited && TourStop >= 5;
                case 10: return DidOpenStoreInTutorial || (StoreStatusManager.Instance != null && StoreStatusManager.Instance.IsOpen);
                case 11: return true;
                default: return false;
            }
        }

        public void NotifyEarlyStaff(StaffMember staff)
        {
            if (JourneyStep != 4 || staff == null || staff.role != StaffRole.Reyoncu ||
                !StaffManager.NormalizeShift(staff.shiftHours).Contains("08:00")) return;
            earlyStaffIds.Add(staff.id);
            DidCallRestockerEarly = earlyStaffIds.Count >= 2;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyBuildingVisited(string building)
        {
            if (JourneyStep == 8)
            {
                if (building == "Chicken_Coop") ChickenVisited = true;
                if (building == "Cow_Barn") CowVisited = true;
            }
            if (JourneyStep == 9 && building == "Farm_Barn") BarnVisited = true;
            CheckCurrentStepCompletion(false);
        }

        public void AcknowledgeTourStop()
        {
            if (JourneyStep != 9 || ModalManager.IsModalOpen || (TourStop == 0 && !BarnVisited)) return;
            TourStop = Mathf.Min(5, TourStop + 1);
            CheckCurrentStepCompletion(false);
        }

        public string JourneyTitle(int step)
        {
            string[] tr = { "", "Kamera kontrolleri", "Tableti keşfet", "Online market ve kuryeler",
                "Mağaza ekibini kur", "Mobilyaları satın al ve kur", "Reyonları ürünle doldur",
                "Çiftçiler ve 30 tohum", "Tavuk ve inek çiftliği", "Kasabanın üretim ağı", "Dükkanı aç" };
            string[] en = { "", "Camera controls", "Explore the tablet", "Online market and couriers",
                "Build your store team", "Buy and install furniture", "Stock the shelves",
                "Farmers and 30 seeds", "Chickens and cows", "Town production network", "Open the store" };
            return step >= 1 && step <= 10 ? LocalizationManager.L("MayorTitle" + step, tr[step], en[step]) : "";
        }

        public string JourneyInstruction(int step)
        {
            if (step == 7 && SeedsPlanted && !PlantingFinished)
                return LocalizationManager.L("MayorFinishPlanting", "30 tohumu ektin! Alttaki Bitir düğmesine basarak ekimi tamamla; sonra hayvan çiftliğine geçeceğiz.", "All 30 seeds are planted! Press Done below to finish planting before moving on to livestock.");
            string[] tr = { "",
                "Haritayı kaydır, yakınlaştır/uzaklaştır ve kamerayı döndür.",
                "EKT Tablet'i aç. Ana ekrandaki sekiz uygulamayı sırayla ziyaret et.",
                "Alışveriş > Araçlar: 2 kurye motoru satın al. Online Market > Kontratlar: 1 numaralı motoru kontratlara ekle ve bir kontratı sıraya al. İşe Alım: 2 kadın, 2 erkek kurye seçip onayla. Vardiyalar: 2 sabah, 2 akşam.",
                "2 kasiyer, 4 reyoncu, 2 temizlikçi, 2 güvenlik, 2 müşteri hizmetlisi ve 2 maskot seç; Onayla'ya bas. Her rolün yarısı sabah, yarısı akşam. Sabah vardiyasındaki iki reyoncuyu erken çağır.",
                "Yalnızca listedeki mobilyalar bu göreve sayılır. Siparişi tamamla; teslimat paletinden her birini alıp kur. Diğer mobilyalar hedeflerin yerine geçmez.",
                "Alışveriş > Toplu Sipariş. Ardından 3 normal rafın ve 2 normal buzdolabının dört sırasına ürün ata. Toplu ürün ataması da kullanılabilir; diğer reyonlar sonraya kalabilir.",
                "Çiftlik > İşe Alım: 2 çiftçi, biri sabah biri akşam. Tohumlar'dan domates, salatalık ve marul için birer paket al. Tarlalara her türden 10, toplam 30 tohum ek.",
                "Alışveriş > Hayvanlar: 6 tavuk ve 3 inek satın al. Kümesi ve inek ahırını ziyaret et. Daha sonra hayvanları yapıların kapasitesine kadar artırabilirsin.",
                "Önce ahıra dokun. Ürün depolama, atölye, taksi ve tren istasyonu hakkında bilgi al; her durakta Devam'a bas. Sonra mağazana döneceğiz.",
                "Hazırsın! Sol üstteki Dükkan Kapalı butonuna basıp mağazanı aç."
            };
            string[] en = { "",
                "Pan the map, zoom in/out and rotate the camera.",
                "Open EKT Tablet and visit its eight apps.",
                "Shopping > Vehicles: buy 2 courier motorcycles. Online Market > Contracts: assign motorcycle #1 and queue a contract. Hire and confirm 2 female and 2 male couriers. Assign 2 morning and 2 evening shifts.",
                "Select and confirm 2 cashiers, 4 restockers, 2 cleaners, 2 security guards, 2 customer service staff and 2 mascots. Split each role equally between morning and evening. Call both morning restockers in early.",
                "Only the listed furniture counts. Complete the order, then unpack and install each item from the delivery pallet. Other furniture cannot replace the targets.",
                "Shopping > Bulk Order. Assign products to all four rows of 3 standard shelves and 2 standard fridges. Bulk assignment is allowed; other shelves can wait.",
                "Farm > Hire: 2 farmers, one morning and one evening. Buy a packet each of tomato, cucumber and lettuce seeds. Plant 10 of each, 30 in total.",
                "Shopping > Animals: buy 6 chickens and 3 cows. Visit the coop and cow barn. You can expand to each building's capacity later.",
                "Tap the barn first. Learn about storage, workshops, taxis and the railway. Press Continue at each stop, then return to your store.",
                "You are ready! Tap Store Closed at the top left to open your store."
            };
            return step >= 1 && step <= 10 ? LocalizationManager.L("MayorInstruction" + step, tr[step], en[step]) : "";
        }

        public string JourneyChecklist(int step)
        {
            string Check(bool done, string text) => (done ? "<color=#42ED96>✓ " : "• ") + text + (done ? "</color>" : "");
            string L(string tr, string en) => LocalizationManager.L("MayorCheck_" + en, tr, en);
            var lines = new List<string>();
            if (step == 1) { lines.Add(Check(DidPanCamera, L("Kaydır", "Pan"))); lines.Add(Check(DidZoomCamera, L("Yakınlaştır", "Zoom"))); lines.Add(Check(DidRotateCamera, L("Döndür", "Rotate"))); }
            if (step == 2) lines.Add(Check(ExploredAppsCount >= 8, L("Uygulamalar", "Apps") + $" {ExploredAppsCount}/8"));
            if (step == 3) {
                lines.Add(Check(TutorialMotorcycles >= 2, L("Kurye motoru", "Courier motorcycles") + $" {Mathf.Min(TutorialMotorcycles, 2)}/2"));
                lines.Add(Check(FirstMotorcycleAssigned, L("1 numaralı motoru kontratlara ekle", "Assign motorcycle #1 to contracts")));
                lines.Add(Check(HasAcceptedTownContract(), L("Kontratı sıraya al", "Queue a contract")));
                lines.Add(Check(CourierGenderCount(true) >= 2, L("Kadın kurye", "Female couriers") + $" {Mathf.Min(CourierGenderCount(true),2)}/2"));
                lines.Add(Check(CourierGenderCount(false) >= 2, L("Erkek kurye", "Male couriers") + $" {Mathf.Min(CourierGenderCount(false),2)}/2"));
                lines.Add(Check(CouriersReady, L("Vardiyalar: 2 sabah + 2 akşam", "Shifts: 2 morning + 2 evening")));
            }
            if (step == 4) {
                foreach (var role in RequiredStoreRoles) {
                    int each = role == StaffRole.Reyoncu ? 2 : 1;
                    lines.Add(Check(RoleShiftsReady(0, role, each), RoleLabel(role) + $" {ShiftCount(0,role,false)}/{each} + {ShiftCount(0,role,true)}/{each}"));
                }
                lines.Add(Check(EarlyRestockers >= 2, L("Erken gelen reyoncu", "Early restockers") + $" {EarlyRestockers}/2"));
            }
            if (step == 5) for (int i = 0; i < RequiredFurniture.Length; i++) {
                var t = RequiredFurniture[i]; int n = RequiredFurnitureCounts[i];
                lines.Add(Check(Purchased(t) >= n, FurnitureDatabase.GetDef(t).LocalizedName + $" {Mathf.Min(Purchased(t),n)}/{n}") +
                    "  " + Check(Placed(t) >= n, L("Kur", "Install") + $" {Mathf.Min(Placed(t),n)}/{n}"));
            }
            if (step == 6) {
                lines.Add(Check(DidPlaceBulkOrder, L("Toplu sipariş", "Bulk order")));
                lines.Add(Check(StockedFurniture(FurnitureType.Shelf) >= 3, L("Dolu raf", "Assigned shelves") + $" {StockedFurniture(FurnitureType.Shelf)}/3"));
                lines.Add(Check(StockedFurniture(FurnitureType.Fridge) >= 2, L("Dolu buzdolabı", "Assigned fridges") + $" {StockedFurniture(FurnitureType.Fridge)}/2"));
            }
            if (step == 7) {
                lines.Add(Check(FarmersReady, L("Çiftçi: 1 sabah + 1 akşam", "Farmers: 1 morning + 1 evening")));
                lines.Add(Check(SeedsBought, L("Üç tohum paketi satın al", "Buy the three seed packets")));
                foreach (string id in new[] {"spring_tomato","spring_cucumber","spring_lettuce"})
                    lines.Add(Check(Planted(id) >= 10, GardenSeedDatabase.GetSeedById(id).LocalizedName + $" {Planted(id)}/10"));
                lines.Add(Check(PlantingFinished, L("Ekim sonunda Bitir'e bas", "Press Done after planting")));
            }
            if (step == 8) {
                int chickens = LivestockManager.Instance != null ? LivestockManager.Instance.TotalChickens : 0;
                int cows = LivestockManager.Instance != null ? LivestockManager.Instance.TotalCows : 0;
                lines.Add(Check(chickens >= 6, L("Tavuk", "Chickens") + $" {chickens}/6"));
                lines.Add(Check(cows >= 3, L("İnek", "Cows") + $" {cows}/3"));
                lines.Add(Check(ChickenVisited && CowVisited, L("İki yapıyı ziyaret et", "Visit both buildings")));
            }
            if (step == 9) { lines.Add(Check(BarnVisited, L("Ahıra dokun", "Tap the barn"))); lines.Add(Check(TourStop >= 5, L("Bilgilendirme durakları", "Information stops") + $" {TourStop}/5")); }
            if (step == 10) lines.Add(Check(DidOpenStoreInTutorial, L("Mağazayı aç", "Open your store")));
            return string.Join("\n", lines);
        }

        public static string RoleLabel(StaffRole role)
        {
            string[] en = { "Cashiers", "Restockers", "Cleaners", "Security", "Customer service", "Mascots" };
            return LocalizationManager.L("MayorRole" + role, role.ToString(), (int)role < en.Length ? en[(int)role] : role.ToString());
        }

        public TutorialJourneySave CaptureJourney()
        {
            return new TutorialJourneySave {
                purchasedTypes = journeyPurchases.Keys.Select(t => t.ToString()).ToList(), purchasedCounts = journeyPurchases.Values.ToList(),
                plantedSeeds = journeyPlants.Keys.ToList(), plantedCounts = journeyPlants.Values.ToList(), earlyStaff = earlyStaffIds.ToList(),
                apps = exploredApps.ToList(), pan = DidPanCamera, zoom = DidZoomCamera, rotate = DidRotateCamera,
                bulk = DidPlaceBulkOrder, contract = DidAcceptTownContract, tomato = DidBuyTomatoSeed,
                cucumber = DidBuyCucumberSeed, lettuce = DidBuyLettuceSeed, chickenSeen = ChickenVisited,
                cowSeen = CowVisited, barnSeen = BarnVisited, tour = TourStop, plantingFinished = PlantingFinished
            };
        }

        public void RestoreJourney(TutorialJourneySave data)
        {
            ResetProgress();
            if (data == null) return;
            PlantingFinished = data.plantingFinished;
            for (int i = 0; i < Math.Min(data.purchasedTypes.Count, data.purchasedCounts.Count); i++)
                if (Enum.TryParse(data.purchasedTypes[i], out FurnitureType t)) journeyPurchases[t] = data.purchasedCounts[i];
            for (int i = 0; i < Math.Min(data.plantedSeeds.Count, data.plantedCounts.Count); i++)
                journeyPlants[data.plantedSeeds[i]] = data.plantedCounts[i];
            foreach (var id in data.earlyStaff) earlyStaffIds.Add(id);
            foreach (int app in data.apps) exploredApps.Add(app);
            DidPanCamera=data.pan; DidZoomCamera=data.zoom; DidRotateCamera=data.rotate;
            DidPlaceBulkOrder=data.bulk; DidAcceptTownContract=data.contract;
            DidBuyTomatoSeed=data.tomato; DidBuyCucumberSeed=data.cucumber; DidBuyLettuceSeed=data.lettuce;
            ChickenVisited=data.chickenSeen; CowVisited=data.cowSeen; BarnVisited=data.barnSeen; TourStop=data.tour;
        }
    }
}
