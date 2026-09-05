using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    public enum CourierDutyState
    {
        OffDuty,
        WalkingToBay,
        WaitingAtBay,
        MountedOnMotorcycle,
        WalkingToExit
    }

    public class CourierSlotState
    {
        public int slotIndex;
        public StaffMember assignedCourier;      // Halihazırda motorun başında/üstünde olan kurye
        public StaffMember incomingCourier;      // Bir sonraki vardiya için yoldan gelen yeni kurye
        public CourierDutyState dutyState = CourierDutyState.OffDuty;
        public GameObject characterObj;          // Yürüyen veya parkta bekleyen kurye modeli
        public List<Transform> leftLimbs;
        public List<Transform> rightLimbs;
        public Coroutine activeRoutine;
    }

    /// <summary>
    /// Satın alınan Kurye Motorsikletlerinin ve Kurye personellerinin
    /// park alanına yerleşimini, diğer dükkan personelleriyle aynı noktadan (Sağ Kaldırım X:15, Z:-4.5)
    /// yürüyerek gelişini, bacak/kol animasyonlarıyla park yerine ulaşıp motora oturmasını,
    /// sipariş dağıtımını ve vardiya bitiminde yine kaldırımdan yürüyerek ayrılışını koordine eder.
    /// </summary>
    public class CourierManager : MonoBehaviour
    {
        private static CourierManager instance;
        public static CourierManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<CourierManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CourierManager");
                        instance = go.AddComponent<CourierManager>();
                    }
                }
                return instance;
            }
        }

        public const int MAX_MOTORCYCLES = 5;
        public const int MOTORCYCLE_PRICE = 3500;

        // 5 Sarı Park Yuvasının Koordinatları (Z: 7.50m - 13.25m, X: 12.50m)
        private readonly Vector3[] slotPositions = new Vector3[]
        {
            new Vector3(12.50f, 0.05f, 8.075f),
            new Vector3(12.50f, 0.05f, 9.225f),
            new Vector3(12.50f, 0.05f, 10.375f),
            new Vector3(12.50f, 0.05f, 11.525f),
            new Vector3(12.50f, 0.05f, 12.675f)
        };

        private readonly Quaternion parkRotation = Quaternion.Euler(0f, 90f, 0f); // Yola dönük duruş

        // Dükkanın diğer personelleriyle BİREBİR AYNI spawn ve ayrılış noktası: Sağ Kaldırım (X: 15.0, Z: -4.5)
        public static readonly Vector3 StaffSpawnAndExitPoint = new Vector3(15.0f, 0.05f, -4.5f);

        [Header("Aktif Motorsikletler")]
        private readonly List<CourierMotorcycleController> spawnedMotorcycles = new List<CourierMotorcycleController>();
        public List<CourierMotorcycleController> SpawnedMotorcycles => spawnedMotorcycles;
        public int OwnedMotorcycleCount => spawnedMotorcycles.Count;

        // Her yuva için durum takipçisi
        private readonly CourierSlotState[] slotStates = new CourierSlotState[MAX_MOTORCYCLES];

        public event Action OnFleetUpdated;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                for (int i = 0; i < MAX_MOTORCYCLES; i++)
                {
                    slotStates[i] = new CourierSlotState { slotIndex = i };
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            BindRuntimeListeners();
        }

        private void Start()
        {
            BindRuntimeListeners();
            SyncCouriersWithTime(true);
        }

        private void BindRuntimeListeners()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeCheckForCouriers;
                TimeManager.Instance.OnTimeUpdated += HandleTimeCheckForCouriers;
                TimeManager.Instance.OnMidnightRollover -= HandleMidnightRollover;
                TimeManager.Instance.OnMidnightRollover += HandleMidnightRollover;
            }

            if (StaffManager.Instance != null)
            {
                StaffManager.Instance.OnCourierStaffListChanged -= HandleCourierListOrShiftChanged;
                StaffManager.Instance.OnCourierStaffListChanged += HandleCourierListOrShiftChanged;
                StaffManager.Instance.OnStaffListChanged -= HandleCourierListOrShiftChanged;
                StaffManager.Instance.OnStaffListChanged += HandleCourierListOrShiftChanged;
            }
        }

        private void HandleMidnightRollover()
        {
            for (int i = 0; i < spawnedMotorcycles.Count; i++)
            {
                var moto = spawnedMotorcycles[i];
                if (moto == null) continue;
                var state = GetStateForMotorcycle(moto);
                if (state.dutyState == CourierDutyState.MountedOnMotorcycle && moto.CourierRiderObj == null)
                {
                    state.dutyState = CourierDutyState.OffDuty;
                }
            }
            SyncCouriersWithTime(false);
        }

        public void RestoreOwnedMotorcycles(int targetCount)
        {
            targetCount = Mathf.Clamp(targetCount, 0, MAX_MOTORCYCLES);

            if (targetCount == 0)
            {
                ResetFleet();
                return;
            }

            // Fazla motorlar varsa temizle
            while (spawnedMotorcycles.Count > targetCount)
            {
                int lastIdx = spawnedMotorcycles.Count - 1;
                var moto = spawnedMotorcycles[lastIdx];
                if (moto != null && moto.gameObject != null)
                {
                    Destroy(moto.gameObject);
                }
                spawnedMotorcycles.RemoveAt(lastIdx);
            }

            // Eksik motorlar varsa spawn et
            while (spawnedMotorcycles.Count < targetCount)
            {
                int nextSlot = spawnedMotorcycles.Count;
                SpawnMotorcycleInSlot(nextSlot);
            }

            PlayerPrefs.SetInt("F2S_OwnedMotorcycles", spawnedMotorcycles.Count);
            PlayerPrefs.Save();

            SyncCouriersWithTime(false);
            OnFleetUpdated?.Invoke();
        }

        public void ResetFleet()
        {
            for (int i = spawnedMotorcycles.Count - 1; i >= 0; i--)
            {
                var moto = spawnedMotorcycles[i];
                if (moto != null && moto.gameObject != null)
                {
                    Destroy(moto.gameObject);
                }
            }
            spawnedMotorcycles.Clear();

            for (int i = 0; i < MAX_MOTORCYCLES; i++)
            {
                if (slotStates[i] != null)
                {
                    if (slotStates[i].activeRoutine != null) StopCoroutine(slotStates[i].activeRoutine);
                    if (slotStates[i].characterObj != null) Destroy(slotStates[i].characterObj);
                    slotStates[i].characterObj = null;
                    slotStates[i].dutyState = CourierDutyState.OffDuty;
                    slotStates[i].assignedCourier = null;
                    slotStates[i].incomingCourier = null;
                }
            }

            PlayerPrefs.SetInt("F2S_OwnedMotorcycles", 0);
            PlayerPrefs.Save();
            OnFleetUpdated?.Invoke();
        }

        private void HandleCourierListOrShiftChanged()
        {
            SyncCouriersWithTime(false);
        }

        public bool CanBuyMotorcycle()
        {
            return OwnedMotorcycleCount < MAX_MOTORCYCLES;
        }

        public bool TryBuyMotorcycle()
        {
            if (!CanBuyMotorcycle()) return false;

            if (EconomyManager.Instance == null || EconomyManager.Instance.Credits < MOTORCYCLE_PRICE)
            {
                return false;
            }

            if (!EconomyManager.Instance.SpendCredits(MOTORCYCLE_PRICE))
            {
                return false;
            }

            if (FinanceManager.Instance != null)
            {
                string cat = LocalizationManager.L("FinCat_Vehicles", "Araçlar & Filo", "Vehicles & Fleet");
                string desc = string.Format(LocalizationManager.L("FinDesc_MotorcycleBuy", "Kurye Motorsikleti #{0} Satın Alımı", "Courier Motorcycle #{0} Purchase"), OwnedMotorcycleCount + 1);
                FinanceManager.Instance.RecordExpense(cat, desc, MOTORCYCLE_PRICE);
            }

            int nextSlot = OwnedMotorcycleCount;
            SpawnMotorcycleInSlot(nextSlot);

            PlayerPrefs.SetInt("F2S_OwnedMotorcycles", OwnedMotorcycleCount);
            PlayerPrefs.Save();

            SyncCouriersWithTime(false);
            OnFleetUpdated?.Invoke();

            return true;
        }

        private void SpawnMotorcycleInSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotPositions.Length) return;

            // Eğer bu slota ait motor zaten listede mevcutsa mükerrer oluşturma!
            for (int s = 0; s < spawnedMotorcycles.Count; s++)
            {
                if (spawnedMotorcycles[s] != null && spawnedMotorcycles[s].SlotIndex == slotIndex)
                {
                    return;
                }
            }

            // Sahnede aynı isimde eski bir nesne kalmışsa temizle
            string motoName = $"Courier_Motorcycle_Slot_{slotIndex + 1}";
            GameObject existing = GameObject.Find(motoName);
            if (existing != null) Destroy(existing);

            Transform[] wheels;
            Light headlight;
            Transform driverSeatMount;

            GameObject motoObj = ProceduralMotorcycleBuilder.CreateCourierMotorcycle(slotIndex, out wheels, out headlight, out driverSeatMount);
            motoObj.name = motoName;
            motoObj.transform.position = slotPositions[slotIndex];
            motoObj.transform.rotation = parkRotation;

            CourierMotorcycleController controller = motoObj.AddComponent<CourierMotorcycleController>();
            controller.Setup(slotIndex, slotPositions[slotIndex], parkRotation, wheels, headlight, driverSeatMount);

            spawnedMotorcycles.Add(controller);
        }

        private void HandleTimeCheckForCouriers(int hour, int minute)
        {
            SyncCouriersWithTime(false);
        }

        public void AutoAssignCouriersToMotorcycles()
        {
            SyncCouriersWithTime(false);
        }

        public bool IsMotorcycleDrivingOnRoad(CourierMotorcycleController moto)
        {
            if (moto == null) return false;
            return moto.CurrentState == MotorcycleState.EnRouteDelivery ||
                   moto.CurrentState == MotorcycleState.DeliveringAtDoorstep ||
                   moto.CurrentState == MotorcycleState.ReturningToStore;
        }

        public bool IsMotorcycleBusy(CourierMotorcycleController moto)
        {
            return IsMotorcycleDrivingOnRoad(moto);
        }

        private static bool SameCourier(StaffMember a, StaffMember b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return true;
            return !string.IsNullOrEmpty(a.id) && a.id == b.id;
        }

        private CourierSlotState GetStateForMotorcycle(CourierMotorcycleController moto)
        {
            int idx = (moto != null) ? moto.SlotIndex : 0;
            if (idx < 0 || idx >= MAX_MOTORCYCLES) idx = 0;
            if (slotStates[idx] == null)
            {
                slotStates[idx] = new CourierSlotState { slotIndex = idx };
            }
            slotStates[idx].slotIndex = idx;
            return slotStates[idx];
        }

        private bool IsCourierVisiblyPresent(CourierSlotState state, CourierMotorcycleController moto)
        {
            if (moto != null && moto.CourierRiderObj != null) return true;
            if (state == null || state.characterObj == null) return false;
            return state.dutyState == CourierDutyState.WalkingToBay || state.dutyState == CourierDutyState.WaitingAtBay;
        }

        private void ClearSlotCharacter(CourierSlotState state)
        {
            if (state == null) return;
            if (state.activeRoutine != null)
            {
                StopCoroutine(state.activeRoutine);
                state.activeRoutine = null;
            }
            if (state.characterObj != null)
            {
                Destroy(state.characterObj);
                state.characterObj = null;
            }
        }

        private void SyncCouriersWithTime(bool isInitialLoad)
        {
            if (StaffManager.Instance == null) return;
            List<StaffMember> couriers = StaffManager.Instance.GetCourierStaffList();

            int currentHour = (TimeManager.Instance != null) ? TimeManager.Instance.Hour : 8;
            int currentMinute = (TimeManager.Instance != null) ? TimeManager.Instance.Minute : 0;

            for (int i = 0; i < spawnedMotorcycles.Count; i++)
            {
                var moto = spawnedMotorcycles[i];
                if (moto == null) continue;

                var state = GetStateForMotorcycle(moto);
                StaffMember scheduledCourier = GetScheduledCourierForSlot(moto.SlotIndex, couriers, currentHour, currentMinute);

                if (isInitialLoad)
                {
                    ClearSlotCharacter(state);
                    if (scheduledCourier != null)
                    {
                        state.assignedCourier = scheduledCourier;
                        state.incomingCourier = null;
                        state.dutyState = CourierDutyState.OffDuty;
                        StartCourierArrival(state, scheduledCourier, moto, true);
                    }
                    else
                    {
                        if (moto.CourierRiderObj != null) moto.UnmountRider();
                        moto.ClearCourier();
                        state.assignedCourier = null;
                        state.incomingCourier = null;
                        state.dutyState = CourierDutyState.OffDuty;
                    }
                    continue;
                }

                if (scheduledCourier != null)
                {
                    bool sameAssigned = SameCourier(state.assignedCourier, scheduledCourier);
                    bool sameIncoming = SameCourier(state.incomingCourier, scheduledCourier);

                    if (IsMotorcycleDrivingOnRoad(moto))
                    {
                        if (!sameAssigned && !sameIncoming &&
                            state.dutyState != CourierDutyState.WalkingToBay &&
                            state.dutyState != CourierDutyState.WaitingAtBay)
                        {
                            state.incomingCourier = scheduledCourier;
                            StartIncomingCourierArrival(state, scheduledCourier, moto);
                        }
                        continue;
                    }

                    if (sameAssigned && IsCourierVisiblyPresent(state, moto))
                    {
                        state.incomingCourier = null;
                        if (state.dutyState == CourierDutyState.WaitingAtBay && moto.CourierRiderObj == null)
                        {
                            MountWaitingCourierOnMotorcycle(state, moto);
                        }
                        continue;
                    }

                    if (!sameAssigned && moto.CourierRiderObj != null)
                    {
                        PerformCourierDismountAndExit(state, moto);
                    }
                    else if (!IsCourierVisiblyPresent(state, moto))
                    {
                        ClearSlotCharacter(state);
                    }

                    state.assignedCourier = scheduledCourier;
                    state.incomingCourier = null;

                    if (state.dutyState == CourierDutyState.WaitingAtBay && state.characterObj != null)
                    {
                        MountWaitingCourierOnMotorcycle(state, moto);
                    }
                    else if (state.dutyState != CourierDutyState.WalkingToBay || state.characterObj == null)
                    {
                        if (state.dutyState == CourierDutyState.MountedOnMotorcycle && moto.CourierRiderObj == null)
                        {
                            state.dutyState = CourierDutyState.OffDuty;
                        }
                        StartCourierArrival(state, scheduledCourier, moto, false);
                    }
                }
                else
                {
                    if (IsMotorcycleDrivingOnRoad(moto))
                    {
                        continue;
                    }

                    if (moto.CourierRiderObj != null)
                    {
                        PerformCourierDismountAndExit(state, moto);
                    }
                    ClearSlotCharacter(state);
                    state.assignedCourier = null;
                    state.incomingCourier = null;
                    state.dutyState = CourierDutyState.OffDuty;
                    moto.ClearCourier();
                }
            }

            OnFleetUpdated?.Invoke();
        }

        private StaffMember GetScheduledCourierForSlot(int slotIndex, List<StaffMember> couriers, int currentHour, int currentMinute)
        {
            if (couriers == null || couriers.Count == 0) return null;

            // SADECE ve SADECE şu anki saat diliminde vardiyası veya erken gelişi aktif olan kuryeler
            List<StaffMember> onDutyCandidates = new List<StaffMember>();
            for (int c = 0; c < couriers.Count; c++)
            {
                var courier = couriers[c];
                if (courier != null && courier.isActive)
                {
                    bool isShiftActive = StaffTaskController.IsStaffShiftActive(courier, currentHour, currentMinute, out _);
                    if (isShiftActive)
                    {
                        onDutyCandidates.Add(courier);
                    }
                }
            }

            // Nöbetteki kuryeler sırayla motor yuvalarına oturur (örneğin 2 kurye varsa sadece slot 0 ve slot 1 dolar)
            if (slotIndex < onDutyCandidates.Count)
            {
                return onDutyCandidates[slotIndex];
            }

            return null; // Slot indexi nöbetteki kurye sayısını aşıyorsa motor BOŞ kalır!
        }

        private void StartIncomingCourierArrival(CourierSlotState state, StaffMember courier, CourierMotorcycleController moto)
        {
            if (state == null || courier == null || moto == null) return;
            if (state.characterObj != null)
            {
                if (state.activeRoutine != null) StopCoroutine(state.activeRoutine);
                Destroy(state.characterObj);
                state.characterObj = null;
            }

            List<Transform> limbsL, limbsR;
            GameObject character = ProceduralStaffModelBuilder.CreateStaffCharacterModel(StaffRole.Kurye, courier.isFemale, out limbsL, out limbsR);
            character.name = $"Courier_Incoming_{courier.name}_Slot_{moto.SlotIndex + 1}";
            state.characterObj = character;
            state.leftLimbs = limbsL;
            state.rightLimbs = limbsR;

            StaffClickableTarget clickTarget = character.GetComponent<StaffClickableTarget>() ?? character.AddComponent<StaffClickableTarget>();
            clickTarget.staffMember = courier;
            clickTarget.courierMoto = moto;

            state.dutyState = CourierDutyState.WalkingToBay;
            character.transform.position = StaffSpawnAndExitPoint;
            character.transform.rotation = Quaternion.identity;

            if (state.activeRoutine != null) StopCoroutine(state.activeRoutine);
            state.activeRoutine = StartCoroutine(CourierWalkToBayRoutine(state, moto, character, moto.HomeParkPosition));
        }

        private void StartCourierArrival(CourierSlotState state, StaffMember courier, CourierMotorcycleController moto, bool instantSpawn)
        {
            if (state == null || courier == null || moto == null) return;
            if (state.dutyState == CourierDutyState.WalkingToBay && state.characterObj != null) return;
            if (state.dutyState == CourierDutyState.MountedOnMotorcycle && moto.CourierRiderObj != null) return;

            if (state.characterObj != null)
            {
                if (state.activeRoutine != null) StopCoroutine(state.activeRoutine);
                Destroy(state.characterObj);
                state.characterObj = null;
            }

            List<Transform> limbsL, limbsR;
            GameObject character = ProceduralStaffModelBuilder.CreateStaffCharacterModel(StaffRole.Kurye, courier.isFemale, out limbsL, out limbsR);
            character.name = $"Courier_{courier.name}_Slot_{state.slotIndex + 1}";
            state.characterObj = character;
            state.leftLimbs = limbsL;
            state.rightLimbs = limbsR;
            state.assignedCourier = courier;

            StaffClickableTarget clickTarget = character.GetComponent<StaffClickableTarget>() ?? character.AddComponent<StaffClickableTarget>();
            clickTarget.staffMember = courier;
            clickTarget.courierMoto = moto;

            if (instantSpawn)
            {
                if (!IsMotorcycleDrivingOnRoad(moto) && moto.CourierRiderObj == null)
                {
                    moto.AssignCourier(courier);
                    moto.MountRider(character);
                    state.dutyState = CourierDutyState.MountedOnMotorcycle;
                    state.characterObj = null;
                    CheckAndDispatchOvernightOrders(moto);
                }
                else
                {
                    character.transform.position = moto.HomeParkPosition + new Vector3(0.5f, 0f, 0f);
                    character.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                    state.dutyState = CourierDutyState.WaitingAtBay;
                }
            }
            else
            {
                state.dutyState = CourierDutyState.WalkingToBay;
                character.transform.position = StaffSpawnAndExitPoint;
                character.transform.rotation = Quaternion.identity;

                if (state.activeRoutine != null) StopCoroutine(state.activeRoutine);
                state.activeRoutine = StartCoroutine(CourierWalkToBayRoutine(state, moto, character, moto.HomeParkPosition));
            }
        }

        private IEnumerator CourierWalkToBayRoutine(CourierSlotState state, CourierMotorcycleController moto, GameObject character, Vector3 targetBayPos)
        {
            if (character == null) yield break;

            float walkSpeed = 2.8f;
            float walkCycleTimer = 0f;

            // Rota Adımları (Sağ Kaldırım Boyunca Yürüyüş):
            // 1. Adım: Kaldırımda motor yuvasının Z hizasına kadar kuzeye yürü (X: 15.0, Z: targetBayPos.z)
            // 2. Adım: Sola dönüp sarı park yuvasına, motorun yanına gir (X: targetBayPos.x + 0.5, Z: targetBayPos.z)
            List<Vector3> waypoints = new List<Vector3>
            {
                new Vector3(15.0f, 0.05f, StaffSpawnAndExitPoint.z),
                new Vector3(15.0f, 0.05f, targetBayPos.z),
                new Vector3(targetBayPos.x + 0.5f, 0.05f, targetBayPos.z)
            };

            for (int w = 1; w < waypoints.Count; w++)
            {
                Vector3 wpTarget = waypoints[w];

                while (character != null && Vector3.Distance(character.transform.position, wpTarget) > 0.20f)
                {
                    Vector3 dir = (wpTarget - character.transform.position).normalized;
                    character.transform.position = Vector3.MoveTowards(character.transform.position, wpTarget, walkSpeed * Time.deltaTime);
                    if (dir != Vector3.zero)
                    {
                        character.transform.rotation = Quaternion.Slerp(character.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 12f * Time.deltaTime);
                    }

                    walkCycleTimer += Time.deltaTime * 8.5f;
                    float legAngle = Mathf.Sin(walkCycleTimer) * 26.0f;

                    if (state != null && state.leftLimbs != null)
                    {
                        foreach (var l in state.leftLimbs) if (l != null) l.localRotation = Quaternion.Euler(legAngle, 0f, 0f);
                    }
                    if (state != null && state.rightLimbs != null)
                    {
                        foreach (var r in state.rightLimbs) if (r != null) r.localRotation = Quaternion.Euler(-legAngle, 0f, 0f);
                    }

                    yield return null;
                }
            }

            if (state != null && state.leftLimbs != null) foreach (var l in state.leftLimbs) if (l != null) l.localRotation = Quaternion.identity;
            if (state != null && state.rightLimbs != null) foreach (var r in state.rightLimbs) if (r != null) r.localRotation = Quaternion.identity;

            // Park yerine ulaşıldı:
            if (character != null && moto != null && state != null)
            {
                if (!IsMotorcycleDrivingOnRoad(moto) && moto.CourierRiderObj == null)
                {
                    StaffMember courierToMount = (state.assignedCourier != null) ? state.assignedCourier : state.incomingCourier;
                    if (courierToMount != null) state.assignedCourier = courierToMount;
                    state.incomingCourier = null;

                    moto.AssignCourier(state.assignedCourier);
                    moto.MountRider(character);
                    state.dutyState = CourierDutyState.MountedOnMotorcycle;
                    state.characterObj = null;
                    CheckAndDispatchOvernightOrders(moto);
                }
                else
                {
                    character.transform.position = targetBayPos + new Vector3(0.5f, 0f, 0f);
                    character.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                    state.dutyState = CourierDutyState.WaitingAtBay;
                }
            }

            if (state != null) state.activeRoutine = null;
            OnFleetUpdated?.Invoke();
        }

        private void MountWaitingCourierOnMotorcycle(CourierSlotState state, CourierMotorcycleController moto)
        {
            if (state == null || moto == null || state.characterObj == null) return;
            if (state.activeRoutine != null)
            {
                StopCoroutine(state.activeRoutine);
                state.activeRoutine = null;
            }

            StaffMember courierToMount = (state.assignedCourier != null) ? state.assignedCourier : state.incomingCourier;
            if (courierToMount != null) state.assignedCourier = courierToMount;
            state.incomingCourier = null;

            moto.AssignCourier(state.assignedCourier);
            moto.MountRider(state.characterObj);
            state.dutyState = CourierDutyState.MountedOnMotorcycle;
            state.characterObj = null;
            CheckAndDispatchOvernightOrders(moto);
            OnFleetUpdated?.Invoke();
        }

        public void CheckAndDispatchOvernightOrders(CourierMotorcycleController moto)
        {
            if (moto == null || moto.LoadedOrders == null || moto.LoadedOrders.Count == 0) return;

            int hour = (TimeManager.Instance != null) ? TimeManager.Instance.Hour : 8;
            if (hour < 8 || hour >= 24) return; // Gece dükkan kapalıyken teslimata çıkılmaz

            bool allGathered = true;
            for (int i = 0; i < moto.LoadedOrders.Count; i++)
            {
                var o = moto.LoadedOrders[i];
                if (o != null && !o.isGatheringCompleted)
                {
                    allGathered = false;
                    break;
                }
            }

            if (allGathered)
            {
                // Siparişler dün geceden eksiksiz hazır: Sabah kuryesi hemen teslimata çıkar!
                moto.DispatchDeliveryTrip();
            }
            else
            {
                // Sipariş eksik: Sabah reyoncusunun kalan ürünleri getirmesini bekler
                moto.CurrentState = MotorcycleState.WaitingForStocker;
            }
        }

        private void PerformCourierDismountAndExit(CourierSlotState state, CourierMotorcycleController moto)
        {
            if (moto == null) return;

            GameObject leavingChar = null;
            if (moto.CourierRiderObj != null)
            {
                leavingChar = moto.CourierRiderObj;
                moto.UnmountRider();
                moto.ClearCourier();
            }

            if (state != null)
            {
                if (leavingChar == null && state.characterObj != null &&
                    (state.dutyState == CourierDutyState.WaitingAtBay || state.dutyState == CourierDutyState.WalkingToBay))
                {
                    leavingChar = state.characterObj;
                    state.characterObj = null;
                }

                if (state.activeRoutine != null)
                {
                    StopCoroutine(state.activeRoutine);
                    state.activeRoutine = null;
                }

                state.dutyState = CourierDutyState.WalkingToExit;
            }

            if (leavingChar != null)
            {
                leavingChar.transform.SetParent(null);
                leavingChar.transform.position = moto.HomeParkPosition + new Vector3(0.5f, 0f, 0f);
                leavingChar.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

                StartCoroutine(IndependentCourierWalkToExitRoutine(leavingChar, leavingChar.transform.position));
            }
        }

        private IEnumerator IndependentCourierWalkToExitRoutine(GameObject character, Vector3 startPos)
        {
            if (character == null) yield break;

            float walkSpeed = 2.8f;
            float walkCycleTimer = 0f;

            List<Transform> leftLimbs = new List<Transform>();
            List<Transform> rightLimbs = new List<Transform>();
            Transform lArm = character.transform.Find("Arm_L");
            Transform rArm = character.transform.Find("Arm_R");
            Transform lLeg = character.transform.Find("Leg_L");
            Transform rLeg = character.transform.Find("Leg_R");
            if (lArm != null) leftLimbs.Add(lArm);
            if (rArm != null) rightLimbs.Add(rArm);
            if (lLeg != null) leftLimbs.Add(lLeg);
            if (rLeg != null) rightLimbs.Add(rLeg);

            // Ayrılış Rotası:
            // 1. Adım: Sarı park yuvasından sağ kaldırıma çık (X: 15.0, Z: startPos.z)
            // 2. Adım: Kaldırım boyunca güneye, personel çıkış/yok olma noktasına yürü (X: 15.0, Z: -4.5)
            List<Vector3> exitWaypoints = new List<Vector3>
            {
                startPos,
                new Vector3(15.0f, 0.05f, startPos.z),
                StaffSpawnAndExitPoint
            };

            for (int w = 1; w < exitWaypoints.Count; w++)
            {
                Vector3 wpTarget = exitWaypoints[w];

                while (character != null && Vector3.Distance(character.transform.position, wpTarget) > 0.20f)
                {
                    Vector3 dir = (wpTarget - character.transform.position).normalized;
                    character.transform.position = Vector3.MoveTowards(character.transform.position, wpTarget, walkSpeed * Time.deltaTime);
                    if (dir != Vector3.zero)
                    {
                        character.transform.rotation = Quaternion.Slerp(character.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 12f * Time.deltaTime);
                    }

                    walkCycleTimer += Time.deltaTime * 8.5f;
                    float legAngle = Mathf.Sin(walkCycleTimer) * 26.0f;

                    foreach (var l in leftLimbs) if (l != null) l.localRotation = Quaternion.Euler(legAngle, 0f, 0f);
                    foreach (var r in rightLimbs) if (r != null) r.localRotation = Quaternion.Euler(-legAngle, 0f, 0f);

                    yield return null;
                }
            }

            if (character != null)
            {
                Destroy(character);
            }
        }

        public void OnMotorcycleReturnedToBay(CourierMotorcycleController moto)
        {
            if (moto == null) return;

            var state = GetStateForMotorcycle(moto);
            if (state == null) return;

            int slotIdx = moto.SlotIndex;

            int currentHour = (TimeManager.Instance != null) ? TimeManager.Instance.Hour : 8;
            int currentMinute = (TimeManager.Instance != null) ? TimeManager.Instance.Minute : 0;

            List<StaffMember> couriers = (StaffManager.Instance != null) ? StaffManager.Instance.GetCourierStaffList() : null;
            StaffMember scheduledCourier = GetScheduledCourierForSlot(slotIdx, couriers, currentHour, currentMinute);

            bool isCurrentShiftActive = (state.assignedCourier != null) && StaffTaskController.IsStaffShiftActive(state.assignedCourier, currentHour, currentMinute, out _);

            if (!SameCourier(state.assignedCourier, scheduledCourier) || !isCurrentShiftActive)
            {
                PerformCourierDismountAndExit(state, moto);

                state.assignedCourier = scheduledCourier;
                state.incomingCourier = null;

                if (scheduledCourier != null)
                {
                    if (state.dutyState == CourierDutyState.WaitingAtBay && state.characterObj != null)
                    {
                        MountWaitingCourierOnMotorcycle(state, moto);
                    }
                    else
                    {
                        state.dutyState = CourierDutyState.OffDuty;
                        StartCourierArrival(state, scheduledCourier, moto, false);
                    }
                }
                else
                {
                    state.dutyState = CourierDutyState.OffDuty;
                }
            }
            else
            {
                // Vardiyası devam eden kurye: Eğer ayakta bekleyen kurye varsa motora binsin
                if (state.dutyState == CourierDutyState.WaitingAtBay && state.characterObj != null)
                {
                    MountWaitingCourierOnMotorcycle(state, moto);
                }
            }

            OnFleetUpdated?.Invoke();
        }

        public CourierMotorcycleController GetAvailableMotorcycleForOrder()
        {
            foreach (var moto in spawnedMotorcycles)
            {
                if (moto != null && moto.CanTakeOrders())
                {
                    return moto;
                }
            }
            return null;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeCheckForCouriers;
                TimeManager.Instance.OnMidnightRollover -= HandleMidnightRollover;
            }

            if (StaffManager.Instance != null)
            {
                StaffManager.Instance.OnCourierStaffListChanged -= HandleCourierListOrShiftChanged;
                StaffManager.Instance.OnStaffListChanged -= HandleCourierListOrShiftChanged;
            }
        }
    }
}
