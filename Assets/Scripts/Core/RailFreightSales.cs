using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Core
{
    [Serializable]
    public sealed class RailCargo
    {
        public string productId;
        public int quantity;
        public int unitPrice;
        public List<ProductLot> lots = new List<ProductLot>();
    }

    [Serializable]
    public sealed class RailFreightSaveData
    {
        public List<RailCargo> waiting = new List<RailCargo>();
        public List<RailCargo> onboard = new List<RailCargo>();
        public int lastCollectionDay = -1;
        public int onboardDay = -1;
    }

    // The manifest belongs to the save, not the station model (which can be rebuilt).
    public static class RailFreightSales
    {
        public const float ArrivalMinute = 750f;
        public const float ExitMinute = 810f;
        static RailFreightSaveData state = new RailFreightSaveData();
        public static IReadOnlyList<RailCargo> Waiting => state.waiting;
        public static IReadOnlyList<RailCargo> Onboard => state.onboard;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { state = new RailFreightSaveData(); }
        public static RailFreightSaveData Capture() => JsonUtility.FromJson<RailFreightSaveData>(JsonUtility.ToJson(state));
        public static void Restore(RailFreightSaveData saved)
        {
            state = saved == null ? new RailFreightSaveData() : JsonUtility.FromJson<RailFreightSaveData>(JsonUtility.ToJson(saved));
            state.waiting ??= new List<RailCargo>();
            state.onboard ??= new List<RailCargo>();
        }
        public static int UnitPrice(string id)
        {
            var seed = GardenSeedDatabase.GetSeedById(id);
            var recipe = seed == null ? WorkshopMachineDatabase.GetRecipeByOutputId(id) : null;
            var livestock = seed == null && recipe == null ? LivestockProductDatabase.GetById(id) : null;
            if (livestock != null) return LivestockProductDatabase.GetQuickSellUnitPrice(id);
            if (seed == null && recipe == null) return 0;
            int raw = Mathf.Max(1, Mathf.RoundToInt(seed != null ? seed.unitSalePrice / 1.4f : recipe.unitSalePrice / 1.8f));
            return Mathf.Max(1, Mathf.RoundToInt(raw * 1.2f));
        }
        public static int Total(IReadOnlyList<RailCargo> cargo)
        {
            int total = 0;
            foreach (var item in cargo) total += item.quantity * item.unitPrice;
            return total;
        }
        public static bool SendFromBarn(string id, int quantity)
        {
            // Advance the collection cutoff BEFORE inserting a new shipment.
            AdvanceClock();
            int price = UnitPrice(id);
            var barn = GardenSeedInventoryManager.Instance;
            if (barn == null || quantity <= 0 || price <= 0) return false;
            if (!barn.ConsumeBarnCrop(id, quantity, out List<ProductLot> lots)) return false;
            var existing = state.waiting.Find(x => x.productId == id && x.unitPrice == price);
            if (existing == null) state.waiting.Add(new RailCargo { productId = id, quantity = quantity, unitPrice = price, lots = lots ?? new List<ProductLot>() });
            else { existing.quantity += quantity; existing.lots ??= new List<ProductLot>(); if (lots != null) existing.lots.AddRange(lots); }
            return true;
        }
        public static void AdvanceClock()
        {
            var clock = TimeManager.Instance;
            if (clock == null) return;
            int day = (clock.Year - 1) * 120 + (int)clock.CurrentSeason * 30 + clock.Day;
            Advance(day, clock.MinuteOfDayPrecise);
        }
        public static void Advance(int day, float minute)
        {
            // First settle an older train, including a saved game resumed after its departure.
            if (state.onboard.Count > 0 && (day > state.onboardDay || day == state.onboardDay && minute >= ExitMinute))
                Settle();
            if (day > state.lastCollectionDay && minute >= ArrivalMinute && state.onboard.Count == 0)
            {
                state.lastCollectionDay = day;
                state.onboardDay = day;
                state.onboard.AddRange(state.waiting);
                state.waiting.Clear();
            }
            if (state.onboard.Count > 0 && day == state.onboardDay && minute >= ExitMinute) Settle();
        }
        static void Settle()
        {
            if (EconomyManager.Instance == null) return;
            int payment = Total(state.onboard);
            state.onboard.Clear();
            state.onboardDay = -1;
            EconomyManager.Instance.AddCredits(payment);
            FinanceManager.Instance?.RecordIncome(FinanceCategories.RailSales,
                LocalizationManager.L("RailSaleIncome", "Tren ile mahsul satışı", "Rail freight crop sales"), payment);
        }
    }
}
