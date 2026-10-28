using System;
using System.Collections;
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
        public const string BrandTr = "Sarı Vadi Taksi";
        public const string BrandEn = "Sunny Valley Cab";
        private const int BaseFare = 28;
        private const int FarePerMeter = 1;

        public bool StandOwned { get; private set; }
        public int OwnedTaxiCount { get; private set; }
        public int TodayTripCount { get; private set; }
        public int TodayIncome { get; private set; }

        public event Action OnTaxiFleetChanged;

        private readonly List<TaxiVehicleController> taxis = new List<TaxiVehicleController>();
        private readonly Dictionary<int,GameObject> jobWaiters=new Dictionary<int,GameObject>();
        private readonly List<GameObject> waitingPassengers = new List<GameObject>();
        private readonly Dictionary<int, int> taxiPickupStop = new Dictionary<int, int>();
        private readonly HashSet<int> reservedPickupStops = new HashSet<int>();
        private readonly Dictionary<int, int> taxiRestUntilMinute = new Dictionary<int, int>();
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
            taxiRestUntilMinute.Clear();
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
            int desiredOut = GetDesiredWorkingCount(hour);
            int outNow = WorkingCount;
            if (outNow >= desiredOut)
            {
                if (nextDispatchMinute < stamp)
                    nextDispatchMinute = stamp + GetDispatchGapMinutes(hour);
                return;
            }

            if (nextDispatchMinute < 0)
            {
                // Sabah açılışında filoyu bir anda basma: ilk çağrı 08:10–08:22 civarı.
                int firstWait = (hour == ShiftStartHour)
                    ? UnityEngine.Random.Range(10, 23)
                    : UnityEngine.Random.Range(6, 14);
                nextDispatchMinute = stamp + firstWait;
            }

            if (stamp < nextDispatchMinute) return;
            if (TryDispatchCall(stamp))
                nextDispatchMinute = stamp + GetDispatchGapMinutes(hour);
            else
                nextDispatchMinute = stamp + UnityEngine.Random.Range(5, 9);
        }

        private int GetDesiredWorkingCount(int hour)
        {
            int owned = OwnedTaxiCount;
            if (owned <= 0) return 0;
            if (hour < ShiftStartHour || hour >= ShiftEndHour) return 0;

            // Saat dilimine göre duraktaki gerçek talep: sabah 08:00'de tüm filo çıkmaz.
            if (hour == 8) return Mathf.Min(owned, 1);
            if (hour == 9) return Mathf.Min(owned, 2);
            if (hour >= 10 && hour < 12) return Mathf.Min(owned, 1);
            if (hour >= 12 && hour < 14) return Mathf.Min(owned, Mathf.Max(1, Mathf.CeilToInt(owned * 0.4f)));
            if (hour >= 14 && hour < 17) return Mathf.Min(owned, Mathf.Max(1, Mathf.CeilToInt(owned * 0.35f)));
            if (hour >= 17 && hour < 19) return Mathf.Min(owned, Mathf.Max(1, Mathf.CeilToInt(owned * 0.55f)));
            if (hour >= 19 && hour < 21) return Mathf.Min(owned, Mathf.Max(1, Mathf.CeilToInt(owned * 0.45f)));
            return Mathf.Min(owned, 1);
        }

        private static int GetDispatchGapMinutes(int hour)
        {
            if (hour >= 17 && hour < 19) return UnityEngine.Random.Range(10, 16);
            if (hour >= 12 && hour < 14) return UnityEngine.Random.Range(12, 18);
            if (hour == 9 || (hour >= 19 && hour < 21)) return UnityEngine.Random.Range(12, 20);
            if (hour == 8 || hour >= 21) return UnityEngine.Random.Range(16, 24);
            return UnityEngine.Random.Range(14, 22);
        }

        private static int GetStandRestMinutes(int hour)
        {
            if (hour >= 17 && hour < 19) return UnityEngine.Random.Range(12, 20);
            if (hour >= 12 && hour < 14) return UnityEngine.Random.Range(14, 22);
            return UnityEngine.Random.Range(18, 32);
        }

        private static int GetCurrentStamp()
        {
            if (TimeManager.Instance == null) return 8 * 60;
            return TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute;
        }

        private bool IsTaxiRested(int slot, int stamp)
        {
            if (!taxiRestUntilMinute.TryGetValue(slot, out int until)) return true;
            return stamp >= until;
        }

        private void ScheduleStandRest(TaxiVehicleController taxi)
        {
            if (taxi == null) return;
            int hour = TimeManager.Instance != null ? TimeManager.Instance.CurrentHour : 12;
            taxiRestUntilMinute[taxi.SlotIndex] = GetCurrentStamp() + GetStandRestMinutes(hour);
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
                int stamp = GetCurrentStamp();
                if (!IsTaxiRested(t.SlotIndex, stamp))
                {
                    return LocalizationManager.L("Jobs_TaxiResting", "Durağında bekliyor (sonraki çağrı)", "Waiting at the stand (next call)");
                }

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
            int stamp = GetCurrentStamp();
            List<TaxiVehicleController> ready = new List<TaxiVehicleController>(MAX_TAXIS);
            for (int i = 0; i < taxis.Count; i++)
            {
                TaxiVehicleController t = taxis[i];
                if (t == null || t.IsWorking) continue;
                if (!IsTaxiRested(t.SlotIndex, stamp)) continue;
                ready.Add(t);
            }

            if (ready.Count == 0) return null;
            return ready[UnityEngine.Random.Range(0, ready.Count)];
        }

        private bool TryPickStops(out int pickIndex, out int dropIndex)
        {
            pickIndex = 0;
            dropIndex = 0;
            if (stops.Length < 2) return false;

            int attempts = Mathf.Max(8, stops.Length * 2);
            for (int n = 0; n < attempts; n++)
            {
                int a = UnityEngine.Random.Range(0, stops.Length);
                if (reservedPickupStops.Contains(a) && reservedPickupStops.Count < stops.Length - 1)
                    continue;

                int b = UnityEngine.Random.Range(0, stops.Length);
                if (b == a) b = (b + 1) % stops.Length;
                pickIndex = a;
                dropIndex = b;
                return true;
            }

            pickIndex = UnityEngine.Random.Range(0, stops.Length);
            dropIndex = (pickIndex + 1 + UnityEngine.Random.Range(0, stops.Length - 1)) % stops.Length;
            return pickIndex != dropIndex;
        }

        private void ReservePickup(TaxiVehicleController taxi, int stopIndex)
        {
            if (taxi == null) return;
            ReleasePickup(taxi);
            taxiPickupStop[taxi.SlotIndex] = stopIndex;
            reservedPickupStops.Add(stopIndex);
        }

        private void ReleasePickup(TaxiVehicleController taxi)
        {
            if (taxi == null) return;
            if (!taxiPickupStop.TryGetValue(taxi.SlotIndex, out int stopIndex)) return;
            taxiPickupStop.Remove(taxi.SlotIndex);
            reservedPickupStops.Remove(stopIndex);
        }

        private bool TryDispatchCall()
        {
            return TryDispatchCall(GetCurrentStamp());
        }

        private bool TryDispatchCall(int stamp)
        {
            if (WorkingCount >= GetDesiredWorkingCount(TimeManager.Instance != null ? TimeManager.Instance.CurrentHour : 12))
                return false;

            TaxiVehicleController taxi = FindIdleTaxi();
            if (taxi == null || !TryPickStops(out int a, out int b)) return false;

            var pick = stops[a];
            var drop = stops[b];
            TaxiRoadRouter.GetPickupSpots(pick.pos, out Vector3 pickWait, out Vector3 pickStop);
            TaxiRoadRouter.GetPickupSpots(drop.pos, out Vector3 dropWait, out Vector3 dropStop);
            int meters = Mathf.Max(22, Mathf.RoundToInt(Vector3.Distance(pickStop, dropStop)));
            int fare = Mathf.Clamp(BaseFare + meters * FarePerMeter, 40, 380);

            GameObject waiter = SpawnWaitingPassenger(pickWait, pickStop);
            waitingPassengers.Add(waiter);
            jobWaiters[taxi.SlotIndex]=waiter;
            ReservePickup(taxi, a);

            List<Vector3> toPick = TaxiRoadRouter.BuildRoute(taxi.transform.position, pickStop);
            taxi.Drive(toPick, TaxiDuty.ToPickup,
                string.Format("Müşteri alınıyor → {0}", pick.nameTr),
                string.Format("Picking up → {0}", pick.nameEn),
                () => StartCoroutine(CompletePickupThenGo(taxi, waiter, dropStop, dropWait, fare, meters, pick.nameTr, pick.nameEn, drop.nameTr, drop.nameEn)));

            OnTaxiFleetChanged?.Invoke();
            return true;
        }

        private IEnumerator CompletePickupThenGo(
            TaxiVehicleController taxi,
            GameObject waiter,
            Vector3 dropStop,
            Vector3 dropWait,
            int fare,
            int meters,
            string pickTr,
            string pickEn,
            string dropTr,
            string dropEn)
        {
            yield return new WaitForSeconds(0.85f);
            if (waiter != null && taxi != null)
            {
                yield return WalkPassengerToTaxi(waiter, taxi);
                waitingPassengers.Remove(waiter);
                Destroy(waiter);
            }

            ReleasePickup(taxi);
            if (taxi == null) yield break;
            if (closingShift || !IsCurrentlyOnShift)
            {
                FinishDropAndReturn(taxi, dropWait, 0, 0, dropTr, dropEn, true);
                yield break;
            }

            List<Vector3> toDrop = TaxiRoadRouter.BuildRoute(taxi.transform.position, dropStop);
            taxi.Drive(toDrop, TaxiDuty.ToDropoff,
                string.Format("{0} → {1}  •  {2:N0} C", pickTr, dropTr, fare),
                string.Format("{0} → {1}  •  {2:N0} C", pickEn, dropEn, fare),
                () => StartCoroutine(HoldAtDropThenReturn(taxi, dropWait, fare, meters, dropTr, dropEn)));
            OnTaxiFleetChanged?.Invoke();
        }

        private IEnumerator HoldAtDropThenReturn(TaxiVehicleController taxi, Vector3 dropWait, int fare, int meters, string dropTr, string dropEn)
        {
            yield return new WaitForSeconds(2.15f);
            FinishDropAndReturn(taxi, dropWait, fare, meters, dropTr, dropEn, false);
        }

        private IEnumerator WalkPassengerToTaxi(GameObject waiter, TaxiVehicleController taxi)
        {
            if (waiter == null || taxi == null) yield break;
            Vector3 start = waiter.transform.position;
            Vector3 door = taxi.transform.position + taxi.transform.right * 1.15f + Vector3.up * 0.02f;
            float elapsed = 0f;
            const float duration = 0.75f;
            while (elapsed < duration && waiter != null && taxi != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                waiter.transform.position = Vector3.Lerp(start, door, t);
                Vector3 look = door - waiter.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                {
                    waiter.transform.rotation = Quaternion.Slerp(waiter.transform.rotation, Quaternion.LookRotation(look), t);
                }

                yield return null;
            }
        }

        private void FinishDropAndReturn(TaxiVehicleController taxi, Vector3 dropPos, int fare, int meters, string dropTr, string dropEn, bool cancelledPickup)
        {
            if (taxi == null) return;
            ReleasePickup(taxi);
            taxi.ClearPassenger();

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
                () =>
                {
                    ScheduleStandRest(taxi);
                    OnTaxiFleetChanged?.Invoke();
                });
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
                    ReleasePickup(taxi);
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

        private GameObject SpawnWaitingPassenger(Vector3 sidewalk, Vector3 taxiStop)
        {
            CustomerType type = (CustomerType)UnityEngine.Random.Range(0, (int)CustomerType.L3_LuxuryCollector);
            GameObject go = ProceduralCustomerModelBuilder.CreateCustomerModel(type, out _, out _);
            go.name = "Taxi_Waiting_Passenger";
            go.transform.position = new Vector3(sidewalk.x, 0.02f, sidewalk.z);
            Vector3 look = taxiStop - sidewalk;
            look.y = 0f;
            go.transform.rotation = look.sqrMagnitude > 0.05f
                ? Quaternion.LookRotation(look)
                : Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
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
            taxiRestUntilMinute[OwnedTaxiCount - 1] = GetCurrentStamp() + UnityEngine.Random.Range(12, 22);
            if (nextDispatchMinute < 0)
                nextDispatchMinute = GetCurrentStamp() + UnityEngine.Random.Range(8, 16);
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
            nextDispatchMinute = -1;
            taxiRestUntilMinute.Clear();
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

        private void RecoverGridlockedTaxi(TaxiVehicleController taxi)
        {
            if(taxi==null)return;
            int slot=taxi.SlotIndex;ReleasePickup(taxi);
            if(jobWaiters.TryGetValue(slot,out var waiter))
            {waitingPassengers.Remove(waiter);if(waiter!=null)Destroy(waiter);jobWaiters.Remove(slot);}
            taxis.Remove(taxi);taxi.gameObject.SetActive(false);Destroy(taxi.gameObject);
            SpawnParkedTaxi(slot);OnTaxiFleetChanged?.Invoke();
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
            RoadTrafficAgent.Register(ctrl.transform).OnGridlock=()=>RecoverGridlockedTaxi(ctrl);
        }

        private void ClearWaiting()
        {
            for (int i = waitingPassengers.Count - 1; i >= 0; i--)
            {
                if (waitingPassengers[i] != null) Destroy(waitingPassengers[i]);
            }

            waitingPassengers.Clear();
            taxiPickupStop.Clear();
            reservedPickupStops.Clear();
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
