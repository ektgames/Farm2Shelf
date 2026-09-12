using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    public class TaxiFleetManager : MonoBehaviour
    {
        public static TaxiFleetManager Instance { get; private set; }

        public const int STAND_PRICE = 45000;
        public const int TAXI_PRICE = 9000;
        public const int MAX_TAXIS = 5;
        public const int ShiftStartHour = 8;
        public const int ShiftEndHour = 22;
        private const int BaseFare = 35;
        private const int FarePerMeter = 2;

        public bool StandOwned { get; private set; }
        public int OwnedTaxiCount { get; private set; }
        public int TodayTripCount { get; private set; }
        public int TodayIncome { get; private set; }

        public event Action OnTaxiFleetChanged;

        private readonly List<TaxiVehicleController> taxis = new List<TaxiVehicleController>();
        private readonly List<GameObject> waitingPassengers = new List<GameObject>();
        private Transform fleetRoot;
        private int nextDispatchMinute = -1;
        private bool closingShift;
        private bool timeHooked;
        private readonly (string nameTr, string nameEn, Vector3 pos)[] stops = BuildStops();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            EnsureRoot();
        }

        private void OnEnable()
        {
            BindTime(true);
        }

        private void OnDisable()
        {
            BindTime(false);
        }

        private void Start()
        {
            BindTime(true);
            RebuildVisualFleet();
        }

        private void Update()
        {
            if (!timeHooked && TimeManager.Instance != null)
                BindTime(true);
        }

        private void BindTime(bool on)
        {
            if (TimeManager.Instance == null) return;
            TimeManager.Instance.OnTimeUpdated -= HandleTime;
            TimeManager.Instance.OnNewDayStarted -= HandleNewDay;
            TimeManager.Instance.OnMidnightRollover -= HandleMidnight;
            if (on)
            {
                TimeManager.Instance.OnTimeUpdated += HandleTime;
                TimeManager.Instance.OnNewDayStarted += HandleNewDay;
                TimeManager.Instance.OnMidnightRollover += HandleMidnight;
                timeHooked = true;
            }
            else
            {
                timeHooked = false;
            }
        }

        private void HandleNewDay(TimeManager.Season season, int day, int year)
        {
            TodayTripCount = 0;
            TodayIncome = 0;
            closingShift = false;
            nextDispatchMinute = -1;
            OnTaxiFleetChanged?.Invoke();
        }

        private void HandleMidnight()
        {
            closingShift = true;
            RecallAllForClose();
        }

        private void HandleTime(int hour, int minute)
        {
            if (!StandOwned || OwnedTaxiCount <= 0) return;

            if (!IsShiftOpen(hour, minute))
            {
                if (!closingShift)
                {
                    closingShift = true;
                    RecallAllForClose();
                }

                return;
            }

            closingShift = false;
            int stamp = hour * 60 + minute;
            if (nextDispatchMinute < 0) nextDispatchMinute = stamp + UnityEngine.Random.Range(3, 8);
            if (stamp < nextDispatchMinute) return;
            if (TryDispatchCall()) nextDispatchMinute = stamp + UnityEngine.Random.Range(6, 14);
            else nextDispatchMinute = stamp + 2;
        }

        public static bool IsShiftOpen(int hour, int minute)
        {
            if (hour < ShiftStartHour) return false;
            return hour < ShiftEndHour;
        }

        public bool IsCurrentlyOnShift
        {
            get
            {
                if (TimeManager.Instance == null) return false;
                return IsShiftOpen(TimeManager.Instance.CurrentHour, TimeManager.Instance.CurrentMinute);
            }
        }

        public int WorkingCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < taxis.Count; i++)
                {
                    if (taxis[i] != null && taxis[i].IsWorking) n++;
                }

                return n;
            }
        }

        public int IdleCount => Mathf.Max(0, OwnedTaxiCount - WorkingCount);

        public string GetTaxiStatusLocalized(int slot)
        {
            TaxiVehicleController t = GetTaxi(slot);
            if (t == null) return "";
            if (!t.IsWorking)
            {
                return LocalizationManager.L("Jobs_TaxiParked", "Sarı park yerinde bekliyor", "Waiting in the yellow bay");
            }

            if (!string.IsNullOrEmpty(t.JobLabelTr) || !string.IsNullOrEmpty(t.JobLabelEn))
            {
                return LocalizationManager.L("Jobs_TaxiJobLine", t.JobLabelTr, t.JobLabelEn);
            }

            return LocalizationManager.L("Jobs_TaxiBusy", "Yolda", "On the road");
        }

        private TaxiVehicleController GetTaxi(int slot)
        {
            for (int i = 0; i < taxis.Count; i++)
            {
                if (taxis[i] != null && taxis[i].SlotIndex == slot) return taxis[i];
            }

            return null;
        }

        private TaxiVehicleController FindIdleTaxi()
        {
            for (int i = 0; i < taxis.Count; i++)
            {
                if (taxis[i] != null && !taxis[i].IsWorking) return taxis[i];
            }

            return null;
        }

        private bool TryDispatchCall()
        {
            TaxiVehicleController taxi = FindIdleTaxi();
            if (taxi == null || stops.Length < 2) return false;

            int a = UnityEngine.Random.Range(0, stops.Length);
            int b = UnityEngine.Random.Range(0, stops.Length);
            if (b == a) b = (b + 1) % stops.Length;

            var pick = stops[a];
            var drop = stops[b];
            int meters = Mathf.Max(18, Mathf.RoundToInt(Vector3.Distance(pick.pos, drop.pos)));
            int fare = Mathf.Clamp(BaseFare + meters * FarePerMeter, 50, 850);

            GameObject waiter = SpawnWaitingPassenger(pick.pos);
            waitingPassengers.Add(waiter);

            List<Vector3> toPick = TaxiRoadRouter.BuildRoute(taxi.transform.position, pick.pos);
            taxi.Drive(toPick, TaxiDuty.ToPickup,
                string.Format("Müşteri alınıyor → {0}", pick.nameTr),
                string.Format("Picking up → {0}", pick.nameEn),
                () =>
                {
                    if (waiter != null)
                    {
                        waitingPassengers.Remove(waiter);
                        taxi.AttachPassenger(waiter);
                    }

                    if (closingShift || !IsCurrentlyOnShift)
                    {
                        FinishDropAndReturn(taxi, drop.pos, 0, 0, drop.nameTr, drop.nameEn, true);
                        return;
                    }

                    List<Vector3> toDrop = TaxiRoadRouter.BuildRoute(taxi.transform.position, drop.pos);
                    taxi.Drive(toDrop, TaxiDuty.ToDropoff,
                        string.Format("{0} → {1}  •  {2:N0} C", pick.nameTr, drop.nameTr, fare),
                        string.Format("{0} → {1}  •  {2:N0} C", pick.nameEn, drop.nameEn, fare),
                        () => FinishDropAndReturn(taxi, drop.pos, fare, meters, drop.nameTr, drop.nameEn, false));
                    OnTaxiFleetChanged?.Invoke();
                });

            OnTaxiFleetChanged?.Invoke();
            return true;
        }

        private void FinishDropAndReturn(TaxiVehicleController taxi, Vector3 dropPos, int fare, int meters, string dropTr, string dropEn, bool cancelledPickup)
        {
            if (taxi == null) return;
            taxi.DropPassengerAt(dropPos);

            if (!cancelledPickup && fare > 0)
            {
                EconomyManager.Instance?.AddCredits(fare);
                TodayTripCount++;
                TodayIncome += fare;
                if (FinanceManager.Instance != null)
                {
                    string desc = LocalizationManager.L(
                        "TrxDesc_TaxiFare",
                        string.Format("Taksi yolculuğu → {0} ({1} m)", dropTr, meters),
                        string.Format("Taxi ride → {0} ({1} m)", dropEn, meters));
                    FinanceManager.Instance.RecordIncome(FinanceCategories.TaxiIncome, desc, fare);
                }
            }

            Vector3 home = NortheastTaxiDistrictBuilder.DefaultParkingSlots[Mathf.Clamp(taxi.SlotIndex, 0, MAX_TAXIS - 1)];
            List<Vector3> homePath = TaxiRoadRouter.BuildReturnToStand(taxi.transform.position, home);
            taxi.Drive(homePath, TaxiDuty.Returning,
                LocalizationManager.L("Jobs_TaxiReturning", "Durağa dönüyor", "Returning to stand"),
                "Returning to stand",
                () => OnTaxiFleetChanged?.Invoke());
            OnTaxiFleetChanged?.Invoke();
        }

        private void RecallAllForClose()
        {
            for (int i = waitingPassengers.Count - 1; i >= 0; i--)
            {
                if (waitingPassengers[i] != null) Destroy(waitingPassengers[i]);
            }

            waitingPassengers.Clear();

            for (int i = 0; i < taxis.Count; i++)
            {
                TaxiVehicleController taxi = taxis[i];
                if (taxi == null || !taxi.IsWorking) continue;
                if (taxi.Duty == TaxiDuty.ToPickup)
                {
                    taxi.ClearPassenger();
                    Vector3 home = NortheastTaxiDistrictBuilder.DefaultParkingSlots[Mathf.Clamp(taxi.SlotIndex, 0, MAX_TAXIS - 1)];
                    taxi.Drive(TaxiRoadRouter.BuildReturnToStand(taxi.transform.position, home), TaxiDuty.Returning,
                        LocalizationManager.L("Jobs_TaxiClosing", "Vardiya kapandı, durağa dönüyor", "Shift ended, returning to stand"),
                        "Shift ended, returning to stand",
                        () => OnTaxiFleetChanged?.Invoke());
                }
                else if (taxi.Duty == TaxiDuty.ToDropoff)
                {
                    taxi.JobLabelTr = LocalizationManager.L("Jobs_TaxiLastDrop", "Son yolcuyu bırakıp durağa dönecek", "Dropping last rider, then stand");
                    taxi.JobLabelEn = "Dropping last rider, then stand";
                }
            }

            OnTaxiFleetChanged?.Invoke();
        }

        private GameObject SpawnWaitingPassenger(Vector3 door)
        {
            CustomerType type = (CustomerType)UnityEngine.Random.Range(0, (int)CustomerType.L3_LuxuryCollector);
            GameObject go = ProceduralCustomerModelBuilder.CreateCustomerModel(type, out _, out _);
            go.name = "Taxi_Waiting_Passenger";
            go.transform.position = new Vector3(door.x, 0.02f, door.z);
            go.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            Collider[] cols = go.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
            return go;
        }

        private void EnsureRoot()
        {
            if (fleetRoot != null) return;
            GameObject go = GameObject.Find("Taxi_Fleet_Root");
            if (go == null) go = new GameObject("Taxi_Fleet_Root");
            go.transform.SetParent(transform, false);
            fleetRoot = go.transform;
        }

        public bool CanBuyStand()
        {
            return !StandOwned && EconomyManager.Instance != null && EconomyManager.Instance.Credits >= STAND_PRICE;
        }

        public bool TryBuyStand()
        {
            if (StandOwned) return false;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendCredits(STAND_PRICE))
                return false;

            StandOwned = true;
            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordExpense(
                    FinanceCategories.Taxi,
                    LocalizationManager.L("TrxDesc_TaxiStandBuy", "Taksi durağı satın alımı", "Taxi stand purchase"),
                    STAND_PRICE);
            }

            RebuildVisualFleet();
            OnTaxiFleetChanged?.Invoke();
            return true;
        }

        public bool CanBuyTaxi()
        {
            return StandOwned && OwnedTaxiCount < MAX_TAXIS
                && EconomyManager.Instance != null
                && EconomyManager.Instance.Credits >= TAXI_PRICE;
        }

        public bool TryBuyTaxi()
        {
            if (!StandOwned || OwnedTaxiCount >= MAX_TAXIS) return false;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendCredits(TAXI_PRICE))
                return false;

            OwnedTaxiCount++;
            if (FinanceManager.Instance != null)
            {
                FinanceManager.Instance.RecordExpense(
                    FinanceCategories.Taxi,
                    string.Format(
                        LocalizationManager.L("TrxDesc_TaxiBuyFmt", "Taksi #{0} satın alımı", "Taxi #{0} purchase"),
                        OwnedTaxiCount),
                    TAXI_PRICE);
            }

            SpawnParkedTaxi(OwnedTaxiCount - 1);
            OnTaxiFleetChanged?.Invoke();
            return true;
        }

        public void ResetFleet()
        {
            StandOwned = false;
            OwnedTaxiCount = 0;
            TodayTripCount = 0;
            TodayIncome = 0;
            closingShift = false;
            ClearWaiting();
            RebuildVisualFleet();
            OnTaxiFleetChanged?.Invoke();
        }

        public void Restore(bool standOwned, int taxiCount)
        {
            StandOwned = standOwned;
            OwnedTaxiCount = Mathf.Clamp(taxiCount, 0, MAX_TAXIS);
            if (!StandOwned) OwnedTaxiCount = 0;
            RebuildVisualFleet();
            OnTaxiFleetChanged?.Invoke();
        }

        public void RebuildVisualFleet()
        {
            EnsureRoot();
            ClearWaiting();
            for (int i = fleetRoot.childCount - 1; i >= 0; i--)
                Destroy(fleetRoot.GetChild(i).gameObject);
            taxis.Clear();

            if (!StandOwned || OwnedTaxiCount <= 0) return;

            for (int i = 0; i < OwnedTaxiCount; i++)
                SpawnParkedTaxi(i);
        }

        private void SpawnParkedTaxi(int slot)
        {
            EnsureRoot();
            Vector3[] slots = NortheastTaxiDistrictBuilder.DefaultParkingSlots;
            if (slot < 0 || slot >= slots.Length) return;
            if (GetTaxi(slot) != null) return;

            GameObject taxiObj = ProceduralTaxiModelBuilder.CreateTaxi(out List<Transform> wheels);
            taxiObj.name = $"Taxi_{slot + 1}";
            taxiObj.transform.SetParent(fleetRoot, false);
            taxiObj.transform.position = slots[slot];
            taxiObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            TaxiVehicleController ctrl = taxiObj.AddComponent<TaxiVehicleController>();
            ctrl.Setup(slot, slots[slot], Quaternion.Euler(0f, 0f, 0f), wheels);
            taxis.Add(ctrl);
        }

        private void ClearWaiting()
        {
            for (int i = waitingPassengers.Count - 1; i >= 0; i--)
            {
                if (waitingPassengers[i] != null) Destroy(waitingPassengers[i]);
            }

            waitingPassengers.Clear();
        }

        private static (string nameTr, string nameEn, Vector3 pos)[] BuildStops()
        {
            List<(string, string, Vector3)> list = new List<(string, string, Vector3)>();
            TownContractPartner[] cat = TownContractManager.Catalog;
            for (int i = 0; i < cat.Length; i++)
            {
                if (cat[i] == null) continue;
                list.Add((cat[i].nameTr, cat[i].nameEn, cat[i].doorstepPos));
            }

            return list.ToArray();
        }
    }
}
