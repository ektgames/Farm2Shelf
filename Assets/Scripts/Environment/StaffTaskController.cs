using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace Farm2Shelf.Environment
{
    public class StaffTaskController : MonoBehaviour
    {
        public static StaffTaskController Instance { get; private set; }

        private const float WALK_SPEED = 2.8f * 1.15f; // m/s
        private readonly List<StaffTaskData> staffTaskList = new List<StaffTaskData>();

        public enum StaffAIState
        {
            SpawningRightCorner,
            WalkingToBreakRoom,
            WaitingAtLocker,
            WaitingInBreakRoom,
            ProceedingToTask,
            WorkingOnTask,
            HandingOverShift,
            WalkingToLeftExit,
            Despawned
        }

        public class StaffTaskData
        {
            public StaffMember staffMember;
            public GameObject staffObj;
            public List<Transform> leftLimbs;
            public List<Transform> rightLimbs;

            public StaffAIState currentState;
            public List<Vector3> waypoints;
            public int currentWaypointIndex;
            public float walkCycleTimer;
            public float taskTimer;
            public bool isLeavingShift;
            public Vector3 finalDestination;
            public float stuckTimer;
            public Vector3 lastStuckCheckPos;
            public float lastStuckCheckTime;

            // Reyoncu Özel Eşik Stoğu Verileri (Kamyon ve Depo Taşıma)
            public PlacedFurnitureController targetShelf;
            public int missingItemCount;
            public bool isFetchingFromStorage;
            public bool isUnloadingTruck;
            public bool isFetchingFromTruck;

            public WholesaleProductDef carriedProduct1;
            public WholesaleProductDef carriedProduct2;
            public int carriedAmount1;
            public int carriedAmount2;
            public List<ProductLot> carriedLots1 = new List<ProductLot>();
            public List<ProductLot> carriedLots2 = new List<ProductLot>();

            public PlacedFurnitureController targetShelf1;
            public int targetRowId1 = -1;
            public PlacedFurnitureController targetShelf2;
            public int targetRowId2 = -1;

            public PlacedFurnitureController sourceStorageShelf;
            public int sourceStorageRowId = -1;
            public PlacedFurnitureController sourceStorageShelf2;
            public int sourceStorageRowId2 = -1;
            public PlacedFurnitureController targetStorageShelfForDeposit;

            public GameObject carriedBoxesRoot;
            public bool isCarryingBoxes;

            // Temizlikçi Özel Çöp Verisi
            public GameObject targetTrashObj;
            public Vector3 cleaningStand;
            public float nextCleaningSearch;
            public float cleaningUnreachableSince=-1f;
            public GameObject cleaningTool;
            public bool cleaningJobActive;
            public PlacedFurnitureController serviceDesk;
            public Vector3 serviceApproach;
            public Vector3 serviceApproachLocal;
            public float nextServiceSearch;

            // Güvenlik Özel Volta Devriye Yönü
            public bool securityPatrolForward;

            // Maskot Özel Neşeli Gösteri Zamanlayıcısı
            public float lastMascotCheerTime;

            // Çiftçi Özel Parsel & Çalışma Verisi
            public FieldPlotController targetPlot;
            public bool isFarmerWorkingOnPlot;

            // Personel Dinlenme Odası Mavi Koltuk Oturma Durumu
            public bool isSitting;
            public bool isSeatedAtServiceDesk;
            public float serviceDeskFloorY;
            public int assignedSofaSeatIndex = -1;

            // Online Market Kurye Siparişi Toplama Verisi
            public OnlineCustomerOrder targetOnlineOrder;
            public bool isGatheringForOnlineOrder;
            public List<OnlineOrderGatherItem> onlineGatherItems = new List<OnlineOrderGatherItem>();
            public int currentOnlineGatherIndex;
            public bool isCarryingDeliveryBoxToMotorcycle;
        }

        public bool HasActiveCourierPreparation(OnlineCustomerOrder order = null)
        {
            foreach (var data in staffTaskList)
            {
                if (data == null || data.staffObj == null || !data.staffObj.activeInHierarchy) continue;
                if (order != null && data.targetOnlineOrder != order) continue;
                if (data.targetOnlineOrder != null &&
                    (data.isGatheringForOnlineOrder || data.isCarryingDeliveryBoxToMotorcycle)) return true;
            }
            return false;
        }

        public class OnlineOrderGatherItem
        {
            public PlacedFurnitureController shelf;
            public int rowIndex;
            public WholesaleProductDef productDef;
            public int productIndexInOrder;
            public int amountToGather;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool HasActiveOnlineOrderCourierWork()
        {
            if (staffTaskList == null || staffTaskList.Count == 0) return false;
            for (int i = 0; i < staffTaskList.Count; i++)
            {
                var data = staffTaskList[i];
                if (data == null || data.staffObj == null) continue;
                if (data.isGatheringForOnlineOrder || data.isCarryingDeliveryBoxToMotorcycle || data.targetOnlineOrder != null)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasLeavingStaff()
        {
            if (staffTaskList == null || staffTaskList.Count == 0) return false;
            for (int i = 0; i < staffTaskList.Count; i++)
            {
                var data = staffTaskList[i];
                if (data != null && data.staffObj != null && data.isLeavingShift && data.currentState != StaffAIState.Despawned)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasActiveInHandTasks()
        {
            if (staffTaskList == null || staffTaskList.Count == 0) return false;
            for (int i = 0; i < staffTaskList.Count; i++)
            {
                var data = staffTaskList[i];
                if (data == null || data.staffObj == null) continue;
                if (data.isCarryingBoxes || data.carriedAmount1 > 0 || data.carriedAmount2 > 0 ||
                    data.carriedProduct1 != null || data.carriedProduct2 != null ||
                    data.isFetchingFromStorage || data.isFetchingFromTruck || data.isUnloadingTruck ||
                    data.isGatheringForOnlineOrder || data.targetOnlineOrder != null ||
                    data.targetShelf1 != null || data.targetShelf2 != null || data.targetStorageShelfForDeposit != null ||
                    (data.targetTrashObj != null && data.taskTimer > 0f))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsStaffCarryingInHandTask(string staffId)
        {
            if (staffTaskList == null || staffTaskList.Count == 0) return false;
            StaffTaskData data = staffTaskList.Find(s => s.staffMember != null && s.staffMember.id == staffId);
            if (data == null || data.staffObj == null) return false;
            // SADECE bizzat elinde taşınan fiziksel koli/paket varsa true döner!
            return data.isCarryingBoxes || data.carriedAmount1 > 0 || data.carriedAmount2 > 0 || data.isCarryingDeliveryBoxToMotorcycle;
        }

        public bool IsStaffLeavingShift(string staffId)
        {
            if (staffTaskList == null || staffTaskList.Count == 0) return false;
            StaffTaskData data = staffTaskList.Find(s => s.staffMember != null && s.staffMember.id == staffId);
            if (data == null) return false;
            return data.isLeavingShift ||
                   data.currentState == StaffAIState.WalkingToLeftExit ||
                   data.currentState == StaffAIState.Despawned;
        }

        public static bool IsCashierWorkingAt(PlacedFurnitureController cashier)
        {
            if (Instance == null || Instance.staffTaskList == null || cashier == null) return false;
            Vector3 cashierStaffSpot = FurnitureModelBuilder.GetCashierStaffPosition(cashier.transform);

            for (int i = 0; i < Instance.staffTaskList.Count; i++)
            {
                var task = Instance.staffTaskList[i];
                if (task != null && task.staffObj != null && task.staffMember != null && task.staffMember.role == StaffRole.Kasiyer)
                {
                    if (task.currentState == StaffAIState.WorkingOnTask)
                    {
                        if (Vector3.Distance(task.staffObj.transform.position, cashierStaffSpot) < 0.3f && PedestrianNavigation.Clear(task.staffObj.transform.position, cashierStaffSpot))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool IsStaffShiftActive(StaffMember staff, int currentHour, out bool isEarlyArrivalWindow)
        {
            int minute = (TimeManager.Instance != null) ? TimeManager.Instance.Minute : 0;
            return IsStaffShiftActive(staff, currentHour, minute, out isEarlyArrivalWindow);
        }

        public static bool IsEveningStaffShift(StaffMember staff)
        {
            if (staff == null) return false;
            string shift = staff.shiftHours ?? "";
            return shift.Contains("Akşam") || shift.Contains("Evening") || shift.Contains("Gece") ||
                   shift.Contains("Night") || shift.Contains("16:00 - 24:00") || shift.Contains("24:00");
        }

        public static bool IsStaffShiftActive(StaffMember staff, int currentHour, int currentMinute, out bool isEarlyArrivalWindow)
        {
            isEarlyArrivalWindow = false;
            if (staff == null || !staff.isActive) return false;

            int totalMinsCalc = currentHour * 60 + currentMinute;

            // Akşam Vardiyası Tespiti:
            // "☀️ Sabah (08:00 - 16:00)" -> Sabah Vardiyası
            // "🌆 Akşam (16:00 - 24:00)" -> Akşam Vardiyası
            bool isEveningShift = IsEveningStaffShift(staff);

            // ⚡ Eğer personel erken göreve çağrıldıysa:
            // Sabah personeli: Vardiya bitişi olan 16:00'a (960 dk) kadar KESİNTİSİZ AKTİFTİR!
            // Akşam personeli: Gece 24:00'e (1440 dk) kadar KESİNTİSİZ AKTİFTİR!
            bool isCalledEarly = (StaffVisualManager.Instance != null && StaffVisualManager.Instance.IsStaffCalledEarlyToday(staff.id));
            if (isCalledEarly)
            {
                if (isEveningShift)
                {
                    return (totalMinsCalc < 1440);
                }
                else
                {
                    return (totalMinsCalc < 960);
                }
            }

            // 1. SABAH VARDİYASI: 08:00 - 16:00 (30 dk erken geliş / hazırlık: 07:30 - 08:00)
            if (!isEveningShift)
            {
                if (totalMinsCalc >= 450 && totalMinsCalc < 480) { isEarlyArrivalWindow = true; return true; }
                return (totalMinsCalc >= 480 && totalMinsCalc < 960);
            }
            // 2. AKŞAM VARDİYASI: 16:00 - 24:00 (30 dk erken geliş / hazırlık: 15:30 - 16:00)
            else
            {
                if (totalMinsCalc >= 930 && totalMinsCalc < 960) { isEarlyArrivalWindow = true; return true; }
                if (totalMinsCalc >= 960 && totalMinsCalc < 1440) return true;
                // Gece 24:00 ve sonrası dükkanda müşteri tahliyesi veya kurye teslimatı sürüyorsa personeller çalışmaya devam eder:
                int activeCustCount = CustomerShoppingManager.Instance != null ? CustomerShoppingManager.Instance.ActiveCustomerCount : 0;
                bool courierNightWork = CourierManager.Instance != null && CourierManager.Instance.HasOutstandingNightWork();
                if ((totalMinsCalc >= 1440 || currentHour >= 24) && (activeCustCount > 0 || courierNightWork)) return true;
                return false;
            }
        }

        public static void GetStaffLockerPosition(int staffIndex, out Vector3 lockerStandPos, out float lockerFacingY)
        {
            lockerFacingY = 90f; // Dolap kapaklarına (Sağ duvara) bakar

            int level = 1;
            if (EnvironmentBuilder.Instance != null)
            {
                level = EnvironmentBuilder.Instance.CurrentUpgradeLevel;
            }

            int doorCount = 5;
            float centerZ = 10.75f;
            float lockerX = 10.65f;

            if (level == 2)
            {
                doorCount = 8;
                centerZ = 21.2f;
            }
            else if (level >= 3)
            {
                doorCount = 10;
                centerZ = 29.5f;
            }

            float lockerWidth = doorCount * 0.55f;
            float startDoorZ = centerZ - (lockerWidth / 2f) + 0.275f;

            int assignedLocker = (staffIndex >= 0) ? (staffIndex % doorCount) : 0;
            int overflowGroup = (staffIndex >= 0) ? (staffIndex / doorCount) : 0;

            float doorZ = startDoorZ + (assignedLocker * 0.55f);

            // Ekstra dolap dolması halinde (ör. 5 dolaba 6. personel): Dolap 1 önünde hafif yan yana duruş offseti
            float zOffset = overflowGroup * 0.20f;

            // Dolap kapaklarının 0.75m önü (içinden geçilmeyen odaya bakan standing pos)
            float standX = lockerX - 0.75f; // 9.90f
            lockerStandPos = new Vector3(standX, 0.05f, doorZ + zOffset);
        }

        public void RegisterStaffAI(StaffMember member, GameObject obj, List<Transform> leftLimbs, List<Transform> rightLimbs)
        {
            if (member == null || obj == null) return;

            StaffTaskData existing = staffTaskList.Find(s => s.staffMember != null && s.staffMember.id == member.id);
            if (existing != null)
            {
                existing.staffObj = obj;
                existing.leftLimbs = leftLimbs;
                existing.rightLimbs = rightLimbs;
                return;
            }

            int staffIndex = staffTaskList.Count;
            GetStaffLockerPosition(staffIndex, out Vector3 lockerStandPos, out float lockerFacingY);

            bool isFarmer = (member.role == StaffRole.Çiftçi || member.role == StaffRole.DeneyimliÇiftçi || member.role == StaffRole.UstaÇiftlikSorumlusu || member.role == StaffRole.TarımOtomasyonUzmanı);
            if (isFarmer)
            {
                // ÇİFTÇİLER KESİNLİKLE DÜKKANA GİRMEZ! Çiftlik evi kapı önünde spawn olup tarlalara geçerler.
                Vector3 farmHousePos = new Vector3(25.0f, 0.05f, 32.5f);
                obj.transform.position = farmHousePos;

                StaffTaskData fData = new StaffTaskData
                {
                    staffMember = member,
                    staffObj = obj,
                    leftLimbs = leftLimbs,
                    rightLimbs = rightLimbs,
                    currentState = StaffAIState.ProceedingToTask,
                    waypoints = new List<Vector3> { farmHousePos },
                    currentWaypointIndex = 0,
                    walkCycleTimer = Random.Range(0f, 5f),
                    isLeavingShift = false
                };
                staffTaskList.Add(fData);
                return;
            }

            // DİREKT SAĞ KALDIRIMDAN VE KAPILARDAN GEÇİŞ ROTASI (Dinamik Yapılandırılmış Rota)
            Vector3 spawnStartPos = new Vector3(15.0f, 0.05f, -4.5f);
            List<Vector3> doorEntryRoute = BuildStructuredStaffWaypoints(spawnStartPos, lockerStandPos);

            obj.transform.position = spawnStartPos;

            StaffTaskData data = new StaffTaskData
            {
                staffMember = member,
                staffObj = obj,
                leftLimbs = leftLimbs,
                rightLimbs = rightLimbs,
                currentState = StaffAIState.WalkingToBreakRoom,
                waypoints = doorEntryRoute,
                currentWaypointIndex = 1,
                walkCycleTimer = Random.Range(0f, 5f),
                taskTimer = 0f,
                securityPatrolForward = true,
                isLeavingShift = false
            };

            staffTaskList.Add(data);

            StaffClickableTarget target = obj.GetComponent<StaffClickableTarget>();
            if (target == null) target = obj.AddComponent<StaffClickableTarget>();
            target.staffMember = member;
            target.taskData = data;
        }

        // ==================== PERSONEL DİNLENME ODASI KOLTUK & SANDALYE OTURMA SİSTEMİ ====================
        public class SofaSeatSlot
        {
            public int seatIndex;
            public Vector3 seatPosition;
            public Vector3 facingDirection;
            public StaffTaskData currentOccupant;
            public float unavailableUntil;
            public int approachRevision=-1;
            public Vector3 cachedApproach;
        }

        private readonly List<SofaSeatSlot> activeSofaSeats = new List<SofaSeatSlot>();

        private int roomSeatRevision=-1;
        private void RefreshSofaSeatsForCurrentLevel()
        {
            if(roomSeatRevision==StaffRoomVisuals.Revision)return;
            foreach(var old in activeSofaSeats)
                if(old.currentOccupant!=null){var person=old.currentOccupant;if(person.isSitting && person.staffObj!=null){var p=person.staffObj.transform.position;p.y=.05f;person.staffObj.transform.position=p;}person.assignedSofaSeatIndex=-1;person.isSitting=false;ResetLimbsToRest(person);}
            activeSofaSeats.Clear();roomSeatRevision=StaffRoomVisuals.Revision;
            foreach(var anchor in StaffRoomVisuals.Seats)
                if(anchor!=null && anchor.gameObject.activeInHierarchy)
                    activeSofaSeats.Add(new SofaSeatSlot{seatIndex=activeSofaSeats.Count,seatPosition=anchor.position,facingDirection=anchor.forward});
        }

        private void CleanupStaleSofaOccupants()
        {
            for (int i = 0; i < activeSofaSeats.Count; i++)
            {
                var seat = activeSofaSeats[i];
                if (seat.currentOccupant != null)
                {
                    var occ = seat.currentOccupant;
                    if (occ.staffObj == null ||
                        !staffTaskList.Contains(occ) ||
                        occ.isLeavingShift ||
                        occ.currentState == StaffAIState.WalkingToLeftExit ||
                        occ.currentState == StaffAIState.Despawned ||
                        occ.assignedSofaSeatIndex != seat.seatIndex)
                    {
                        if (occ.assignedSofaSeatIndex == seat.seatIndex)
                        {
                            occ.assignedSofaSeatIndex = -1;
                            occ.isSitting = false;
                        }
                        seat.currentOccupant = null;
                    }
                }
            }
        }

        private bool TryAssignSofaSeat(StaffTaskData data)
        {
            if (data == null) return false;
            RefreshSofaSeatsForCurrentLevel();
            CleanupStaleSofaOccupants();

            // Zaten koltukta oturuyorsa/atanmışsa devam et
            if (data.assignedSofaSeatIndex >= 0 && data.assignedSofaSeatIndex < activeSofaSeats.Count)
            {
                var currentSeat = activeSofaSeats[data.assignedSofaSeatIndex];
                if (currentSeat.currentOccupant == data) return true;
            }

            // Boş koltuk ara:
            for (int i = 0; i < activeSofaSeats.Count; i++)
            {
                var seat = activeSofaSeats[i];
                if (seat.currentOccupant == null && Time.unscaledTime>=seat.unavailableUntil && !PedestrianNavigation.IsBlocked(GetSeatApproach(seat)))
                {
                    seat.currentOccupant = data;
                    data.assignedSofaSeatIndex = seat.seatIndex;
                    return true;
                }
            }

            // Tüm koltuklar doluysa
            data.assignedSofaSeatIndex = -1;
            return false;
        }

        private static readonly float[] SeatApproachRadii={1f,1.35f,1.7f};
        private static readonly float[] SeatApproachAngles={0f,90f,-90f,180f};
        private static Vector3 GetSeatApproach(SofaSeatSlot seat)
        {
            int revision=PedestrianNavigation.Version;
            if(seat.approachRevision==revision)return seat.cachedApproach;
            seat.approachRevision=revision;
            Vector3 center = seat.seatPosition;
            center.y = 0.05f;
            // Chairs can face a table: select an accessible side rather than the tabletop.
            foreach (float radius in SeatApproachRadii)
                foreach (float angle in SeatApproachAngles)
                {
                    Vector3 point = center + Quaternion.Euler(0, angle, 0) * seat.facingDirection.normalized * radius;
                    if (!PedestrianNavigation.IsBlocked(point) && PedestrianNavigation.Clear(center, point, true))return seat.cachedApproach=point;
                }
            return seat.cachedApproach=center+seat.facingDirection.normalized;
        }

        private void FreeSofaSeat(StaffTaskData data)
        {
            if (data == null) return;

            if (data.isSitting && data.staffObj != null && data.assignedSofaSeatIndex >= 0 && data.assignedSofaSeatIndex < activeSofaSeats.Count)
            {
                var actor = data.staffObj;
                Vector3 floor = actor.transform.position;
                floor.y = 0.05f;
                actor.transform.position = floor;
                var navigator = actor.GetComponent<PedestrianNavigator>() ?? actor.AddComponent<PedestrianNavigator>();
                navigator.BeginSeatExit(GetSeatApproach(activeSofaSeats[data.assignedSofaSeatIndex]));
            }

            if (data.assignedSofaSeatIndex >= 0 && data.assignedSofaSeatIndex < activeSofaSeats.Count)
            {
                var seat = activeSofaSeats[data.assignedSofaSeatIndex];
                if (seat.currentOccupant == data)
                {
                    seat.currentOccupant = null;
                }
            }

            for (int i = 0; i < activeSofaSeats.Count; i++)
            {
                if (activeSofaSeats[i].currentOccupant == data)
                {
                    activeSofaSeats[i].currentOccupant = null;
                }
            }

            data.assignedSofaSeatIndex = -1;
            data.isSitting = false;
            ResetLimbsToRest(data);

            // Koltuk boşaldı! Ayakta bekleyen işsiz personel var mı kontrol et ve ilk boş kalana koltuğu ver!
            foreach (var waitingStaff in staffTaskList)
            {
                if (waitingStaff != null && waitingStaff != data && waitingStaff.staffObj != null &&
                    waitingStaff.currentState == StaffAIState.WaitingInBreakRoom)
                {
                    if (waitingStaff.assignedSofaSeatIndex < 0 && TryAssignSofaSeat(waitingStaff))
                    {
                        break;
                    }
                }
            }
        }

        private void ExecuteBreakRoomRestAndSeating(StaffTaskData data, float deltaTime)
        {
            if (data == null || data.staffObj == null) return;

            // Koltuk veya sandalye ataması dene
            bool hasSeat = TryAssignSofaSeat(data);

            if (hasSeat && data.assignedSofaSeatIndex >= 0 && data.assignedSofaSeatIndex < activeSofaSeats.Count)
            {
                var seat = activeSofaSeats[data.assignedSofaSeatIndex];
                Vector3 targetSitPos = seat.seatPosition;
                // The root is at the feet; align the thigh underside, not the feet, to the cushion.
                float hip=data.leftLimbs!=null && data.leftLimbs.Count>0 ? data.leftLimbs[0].localPosition.y : .75f;
                targetSitPos.y-=(hip-.10f)*data.staffObj.transform.lossyScale.y;

                Vector3 approach = GetSeatApproach(seat);
                float distToSeat = data.isSitting ? 0f : Vector3.Distance(data.staffObj.transform.position, approach);
                if (!data.isSitting && (distToSeat > 0.15f || !PedestrianNavigation.Clear(data.staffObj.transform.position, approach)))
                {
                    // Koltuğa doğru yürü
                    data.isSitting = false;
                    data.staffObj.transform.position = PedestrianNavigation.Move(data.staffObj, approach, 2.5f * 1.15f * deltaTime);
                    var navigation=data.staffObj.GetComponent<PedestrianNavigator>();
                    if(navigation!=null && navigation.Unreachable)
                    {seat.unavailableUntil=Time.unscaledTime+3f;FreeSofaSeat(data);return;}

                    Vector3 moveDir = (targetSitPos - data.staffObj.transform.position).normalized;
                    if (moveDir != Vector3.zero)
                    {
                        data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(moveDir), 360f * deltaTime);
                    }

                    data.walkCycleTimer += deltaTime * 8.0f * 1.15f;
                    float legAngle = Mathf.Sin(data.walkCycleTimer) * 28.0f;
                    AnimateLimbs(data, legAngle);
                }
                else
                {
                    // Koltuğa/Sandalyeye Ulaşıldı: OTUR & YÜZÜNÜ KOLTUK YÖNÜNE DÖN!
                    data.isSitting = true;
                    data.staffObj.transform.position = targetSitPos;
                    data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(seat.facingDirection), 360f * deltaTime);

                    ApplySittingPose(data);
                }
            }
            else
            {
                // Koltuklar ve Sandalyeler Doluysa: Odanın Orta Alanında Ayakta Rahatça Bekle!
                data.isSitting = false;
                ResetLimbsToRest(data);

                int idx = staffTaskList.IndexOf(data);
                Vector3 standPos = GetBreakRoomStandingPosition(idx >= 0 ? idx : 0);

                float distToStand = Vector3.Distance(data.staffObj.transform.position, standPos);
                if (distToStand > 0.30f)
                {
                    data.staffObj.transform.position = PedestrianNavigation.Move(data.staffObj, standPos, 2.5f * 1.15f * deltaTime);
                    Vector3 moveDir = (standPos - data.staffObj.transform.position).normalized;
                    if (moveDir != Vector3.zero)
                    {
                        data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(moveDir), 360f * deltaTime);
                    }
                    data.walkCycleTimer += deltaTime * 8.0f * 1.15f;
                    AnimateLimbs(data, Mathf.Sin(data.walkCycleTimer) * 28.0f);
                }
                else
                {
                    data.staffObj.transform.position = PedestrianNavigation.Move(data.staffObj, standPos, WALK_SPEED * deltaTime);
                    data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.Euler(0f, 180f, 0f), 360f * deltaTime);
                }
            }
        }

        private void ApplySittingPose(StaffTaskData data)
        {
            if (data == null || data.leftLimbs == null || data.rightLimbs == null) return;
            if (data.leftLimbs.Count < 2 || data.rightLimbs.Count < 2) return;

            // Bacaklar kalçadan 80 derece öne bükülür (Doğal oturma pozisyonu)
            Transform lLeg = data.leftLimbs[0];
            Transform rLeg = data.rightLimbs[0];

            lLeg.localRotation = Quaternion.Euler(-80f, 0f, 0f);
            rLeg.localRotation = Quaternion.Euler(-80f, 0f, 0f);
            Transform leftKnee = lLeg.Find("ServiceKnee");
            Transform rightKnee = rLeg.Find("ServiceKnee");
            if (leftKnee != null) leftKnee.localRotation = Quaternion.Euler(80f, 0f, 0f);
            if (rightKnee != null) rightKnee.localRotation = Quaternion.Euler(80f, 0f, 0f);

            // Kollar uyluklara ve kucağa rahatça yaslanır
            Transform lArm = data.leftLimbs[1];
            Transform rArm = data.rightLimbs[1];

            lArm.localRotation = Quaternion.Euler(-25f, 0f, 15f);
            rArm.localRotation = Quaternion.Euler(-25f, 0f, -15f);
        }

        private readonly List<Vector3> breakRoomStandingSlots = new List<Vector3>();
        private int standingSlotsRevision = -1;
        private int standingSlotsLevel = -1;

        private Vector3 GetBreakRoomStandingPosition(int staffIndex)
        {
            int level = EnvironmentBuilder.Instance != null ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            int revision = PedestrianNavigation.Version;
            if (standingSlotsRevision != revision || standingSlotsLevel != level)
            {
                breakRoomStandingSlots.Clear();
                float back = -3f + (level == 1 ? 18f : level == 2 ? 27f : 36f);
                float front = -3f + (level == 1 ? 9.5f : level == 2 ? 14.5f : 19.5f);
                // Keep the central door/locker aisle clear; never extend waiting slots through the back wall.
                for (float z = front + 1.5f; z <= back - 1.2f; z += .8f)
                    foreach (float x in new[] { 8.2f, 9f, 6f, 5.2f })
                    {
                        Vector3 slot = new Vector3(x, .05f, z);
                        if (!PedestrianNavigation.IsBlocked(slot)) breakRoomStandingSlots.Add(slot);
                    }
                standingSlotsRevision = revision;
                standingSlotsLevel = level;
            }
            if (breakRoomStandingSlots.Count > 0)
                return breakRoomStandingSlots[Mathf.Max(0, staffIndex) % breakRoomStandingSlots.Count];
            return new Vector3(7f, .05f, -3f + (level == 1 ? 9.5f : level == 2 ? 14.5f : 19.5f) + .8f);
        }

        private Vector3 GetBreakRoomTargetPosition(StaffTaskData data)
        {
            if (data != null && TryAssignSofaSeat(data) && data.assignedSofaSeatIndex >= 0 && data.assignedSofaSeatIndex < activeSofaSeats.Count)
            {
                return GetSeatApproach(activeSofaSeats[data.assignedSofaSeatIndex]);
            }
            int staffIdx = (data != null) ? staffTaskList.IndexOf(data) : 0;
            return GetBreakRoomStandingPosition(staffIdx >= 0 ? staffIdx : 0);
        }

        public void UnregisterStaffAI(string staffId)
        {
            StaffTaskData data = staffTaskList.Find(s => s.staffMember != null && s.staffMember.id == staffId);
            if (data != null)
            {
                if (data.targetOnlineOrder != null)
                {
                    data.targetOnlineOrder.isAssignedToStocker = false;
                    data.targetOnlineOrder = null;
                }
                data.isGatheringForOnlineOrder = false;
                data.isCarryingDeliveryBoxToMotorcycle = false;
                data.onlineGatherItems.Clear();

                FreeSofaSeat(data);
                ClearCarriedBoxesOnStaff(data);
                if (data.staffObj != null) Destroy(data.staffObj);
                staffTaskList.Remove(data);
            }
        }

        public void ClearAllStaffAI()
        {
            for (int i = staffTaskList.Count - 1; i >= 0; i--)
            {
                var data = staffTaskList[i];
                if (data != null)
                {
                    if (data.targetOnlineOrder != null)
                    {
                        data.targetOnlineOrder.isAssignedToStocker = false;
                        data.targetOnlineOrder = null;
                    }
                    data.isGatheringForOnlineOrder = false;
                    data.isCarryingDeliveryBoxToMotorcycle = false;
                    data.onlineGatherItems.Clear();

                    FreeSofaSeat(data);
                    ClearCarriedBoxesOnStaff(data);
                    if (data.staffObj != null) Destroy(data.staffObj);
                }
            }
            staffTaskList.Clear();
        }

        public bool IsStaffRegistered(string staffId)
        {
            return staffTaskList.Exists(s => s.staffMember != null && s.staffMember.id == staffId && s.staffObj != null);
        }

        public void StartExitForStaff(string staffId)
        {
            StaffTaskData data = staffTaskList.Find(s => s.staffMember != null && s.staffMember.id == staffId);
            if (data != null && data.currentState != StaffAIState.WalkingToLeftExit)
            {
                if (data.isCarryingBoxes || data.carriedAmount1 > 0 || data.carriedAmount2 > 0 || data.carriedProduct1 != null || data.carriedProduct2 != null)
                {
                    return; // Elindeki kolileri raflara yerleştirmeyi bitirene kadar çıkışa geçme
                }
                StartExitRoute(data);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            int currentHour = TimeManager.Instance != null ? TimeManager.Instance.Hour : 6;
            int currentMinute = TimeManager.Instance != null ? TimeManager.Instance.Minute : 0;

            for (int i = staffTaskList.Count - 1; i >= 0; i--)
            {
                StaffTaskData data = staffTaskList[i];
                if (data.staffObj == null)
                {
                    staffTaskList.RemoveAt(i);
                    continue;
                }

                bool isEligible = IsStaffShiftActive(data.staffMember, currentHour, currentMinute, out bool isEarlyArrival);

                bool hasInHandTask = data.isCarryingBoxes || data.carriedAmount1 > 0 || data.carriedAmount2 > 0 ||
                                     data.carriedProduct1 != null || data.carriedProduct2 != null || data.isCarryingDeliveryBoxToMotorcycle ||
                                     data.isGatheringForOnlineOrder || data.targetOnlineOrder != null;

                if (!isEligible && !hasInHandTask && data.currentState != StaffAIState.WalkingToLeftExit && data.currentState != StaffAIState.HandingOverShift && !data.isLeavingShift)
                {
                    bool isFarmer = (data.staffMember != null && (data.staffMember.role == StaffRole.Çiftçi || data.staffMember.role == StaffRole.DeneyimliÇiftçi || data.staffMember.role == StaffRole.UstaÇiftlikSorumlusu || data.staffMember.role == StaffRole.TarımOtomasyonUzmanı));
                    if (isFarmer)
                    {
                        // Çiftçiler vardiyası bitince çiftlik evinin önüne gidip orada yok olur
                        StartExitRoute(data);
                    }
                    else
                    {
                        // Dükkan personeli vardiyası bitince önce soyunma dolabına gidip üstünü değişir, sonra dışarı çıkıp yok olur
                        data.isLeavingShift = true;
                        ClearCarriedBoxesOnStaff(data);
                        FreeSofaSeat(data);

                        int staffIdx = staffTaskList.IndexOf(data);
                        GetStaffLockerPosition(staffIdx >= 0 ? staffIdx : 0, out Vector3 lockerStandPos, out _);

                        data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, lockerStandPos);
                        data.currentWaypointIndex = 1;
                        data.currentState = StaffAIState.WalkingToBreakRoom;
                    }
                }

                UpdateStaffBehavior(data, deltaTime, currentHour, isEarlyArrival);
            }
        }

        private void UpdateStaffBehavior(StaffTaskData data, float deltaTime, int currentHour, bool isEarlyArrival)
        {
            switch (data.currentState)
            {
                case StaffAIState.WalkingToBreakRoom:
                    FollowWaypoints(data, deltaTime, onComplete: () => {
                        data.currentState = StaffAIState.WaitingAtLocker;
                        data.taskTimer = Random.Range(1.5f, 2.0f); // 1.5 - 2.0 saniye dolap başında üst değiştirme / hazırlık
                    });
                    break;

                case StaffAIState.WaitingAtLocker:
                    ResetLimbsToRest(data);
                    data.taskTimer -= deltaTime;

                    int staffIdx = staffTaskList.IndexOf(data);
                    if (staffIdx >= 0 && data.staffObj != null)
                    {
                        GetStaffLockerPosition(staffIdx, out Vector3 lockerStandPos, out float lockerFacingY);
                        data.staffObj.transform.position = PedestrianNavigation.Move(data.staffObj, lockerStandPos, 3.0f * 1.15f * deltaTime);
                        data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.Euler(0f, lockerFacingY, 0f), 360f * deltaTime);
                    }

                    if (data.taskTimer <= 0f)
                    {
                        if (data.isLeavingShift)
                        {
                            StartExitRoute(data);
                        }
                        else
                        {
                            // İşe yeni gelen veya erken çağrılan personel: Doğrudan görev pozisyonuna başla!
                            AssignStaffToTaskPosition(data);
                        }
                    }
                    break;

                case StaffAIState.WaitingInBreakRoom:
                    int curMinute = (TimeManager.Instance != null) ? TimeManager.Instance.Minute : 0;
                    bool dispatchEarlyFromRest = (data.staffMember.role == StaffRole.Kasiyer && curMinute >= 55);

                    bool isTruckWaitingForRestocker = (data.staffMember.role == StaffRole.Reyoncu) &&
                        ((WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckAtDockWaitingForUnload && WholesaleTruckManager.Instance.PendingTruckPackages != null && WholesaleTruckManager.Instance.PendingTruckPackages.Count > 0) ||
                         (GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckAtDockWaitingForUnload && GreenTruckDeliveryManager.Instance.PendingTruckPackages != null && GreenTruckDeliveryManager.Instance.PendingTruckPackages.Count > 0));

                    if (!isEarlyArrival || dispatchEarlyFromRest || isTruckWaitingForRestocker)
                    {
                        FreeSofaSeat(data);
                        data.isSitting = false;
                        AssignStaffToTaskPosition(data);
                    }
                    else
                    {
                        ExecuteBreakRoomRestAndSeating(data, deltaTime);
                    }
                    break;

                case StaffAIState.ProceedingToTask:
                    if(data.staffMember.role==StaffRole.Temizlikçi && !data.isLeavingShift)
                    {data.currentState=StaffAIState.WorkingOnTask;ExecuteCleanerTask(data,deltaTime);break;}
                    FollowWaypoints(data, deltaTime, onComplete: () => {
                        data.currentState = StaffAIState.WorkingOnTask;
                    });
                    break;

                case StaffAIState.WorkingOnTask:
                    ExecuteRoleWorkTask(data, deltaTime);
                    break;

                case StaffAIState.HandingOverShift:
                    data.taskTimer -= deltaTime;
                    ResetLimbsToRest(data);

                    // Kasa başında devir teslim selamlaşması: Devreden kasiyer tezgahın yanında bekler
                    var furnitureCheck = PlacedFurnitureController.AllPlacedFurniture;
                    PlacedFurnitureController targetDesk = null;
                    int fCheckCount = furnitureCheck.Count;
                    for (int i = 0; i < fCheckCount; i++)
                    {
                        var f = furnitureCheck[i];
                        if (f != null && f.FurnitureType == FurnitureType.Cashier)
                        {
                            targetDesk = f;
                            break;
                        }
                    }

                    if (targetDesk != null)
                    {
                        Vector3 sideHandoverPos = FurnitureModelBuilder.GetCashierStaffPosition(targetDesk.transform) + targetDesk.transform.right * 0.65f;
                        data.staffObj.transform.position = PedestrianNavigation.Move(data.staffObj, sideHandoverPos, 3.0f * 1.15f * deltaTime);
                        data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(-targetDesk.transform.right), 360f * deltaTime);
                    }

                    if (data.taskTimer <= 0f)
                    {
                        StartExitRoute(data);
                    }
                    break;

                case StaffAIState.WalkingToLeftExit:
                    FollowWaypoints(data, deltaTime, onComplete: () => {
                        FreeSofaSeat(data);
                        data.currentState = StaffAIState.Despawned;
                        if (data.staffObj != null) Destroy(data.staffObj);
                        staffTaskList.Remove(data);
                    });
                    break;
            }
        }

        public static List<Vector3> BuildStructuredStaffWaypoints(Vector3 startPos, Vector3 endPos)
        {
            // Goods are handed over on the building-facing side, never inside
            // the truck or via the driveway on its opposite side.
            Vector3 pickup = DeliveryTruckVisuals.UnloadPosition;
            Vector3 goodsInside = new Vector3(9.2f, 0.05f, 2f);
            Vector3 goodsThreshold = new Vector3(11.2f, 0.05f, 2f);
            Vector3 sideApproach = new Vector3(pickup.x, 0.05f, 2f);
            bool toPickup = Vector3.Distance(endPos, pickup) < .1f;
            bool fromPickup = Vector3.Distance(startPos, pickup) < .65f;
            if (toPickup || fromPickup)
            {
                if (toPickup && fromPickup) return new List<Vector3> { startPos, pickup };
                if (toPickup)
                {
                    var pickupRoute = BuildStructuredStaffWaypoints(startPos, goodsInside);
                    pickupRoute.Add(goodsThreshold);
                    pickupRoute.Add(sideApproach);
                    pickupRoute.Add(pickup);
                    return pickupRoute;
                }
                var outgoing = new List<Vector3> { startPos, sideApproach, goodsThreshold };
                outgoing.AddRange(BuildStructuredStaffWaypoints(goodsInside, endPos));
                return outgoing;
            }
            List<Vector3> rawRoute = new List<Vector3>();
            rawRoute.Add(startPos);

            int level = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            float storageDepth = (level == 1) ? 9.5f : ((level == 2) ? 14.5f : 19.5f);
            float storageBackZ = -3.0f + storageDepth; // Seviye 1: 6.5f, Seviye 2: 11.5f, Seviye 3: 16.5f

            Vector3 staffDoorOutside = new Vector3(7.0f, 0.05f, storageBackZ - 0.75f); // Depo Tarafı
            Vector3 staffDoorInside  = new Vector3(7.0f, 0.05f, storageBackZ + 0.75f); // Personel Odası Tarafı

            Vector3 storageDoorStorageSide = new Vector3(3.6f, 0.05f, 2.0f);
            Vector3 storageDoorStoreSide   = new Vector3(2.2f, 0.05f, 2.0f);

            // Teslimat Kamyonu & Mal Kabul Geniş Çift Kapısı (Right Wall X: 11.0f, Z: 2.0f)
            Vector3 goodsDoorStorageSide = new Vector3(9.2f, 0.05f, 2.0f); // Depo içi
            Vector3 goodsDoorThreshold   = new Vector3(11.2f, 0.05f, 2.0f); // Kapı eşiği
            Vector3 eastDrivewayPos      = new Vector3(14.0f, 0.05f, 2.0f); // Dış asfalt / kaldırım koridoru

            Vector3 storageAislePos = new Vector3(7.0f, 0.05f, 2.0f);
            Vector3 storeFoyerPos   = new Vector3(-5.0f, 0.05f, 0.5f);
            Vector3 storeOutsidePos = new Vector3(-5.0f, 0.05f, -4.5f);

            // BÖLGE TESPİTLERİ (Katı Bina Duvarlarını Hatasız Ayıran Çakışmasız Koordinatlar)
            bool startInEastOutside = (startPos.x >= 10.8f && startPos.z > -2.8f);
            bool endInEastOutside   = (endPos.x >= 10.8f && endPos.z > -2.8f);

            bool startInStaffRoom = (startPos.x > 2.8f && startPos.x < 10.8f && startPos.z >= (storageBackZ - 0.1f));
            bool endInStaffRoom   = (endPos.x > 2.8f && endPos.x < 10.8f && endPos.z >= (storageBackZ - 0.1f));

            bool startInStorage   = (startPos.x > 2.8f && startPos.x < 10.8f && startPos.z < (storageBackZ - 0.1f) && startPos.z > -2.8f);
            bool endInStorage     = (endPos.x > 2.8f && endPos.x < 10.8f && endPos.z < (storageBackZ - 0.1f) && endPos.z > -2.8f);

            bool startInStore     = (startPos.x <= 2.8f && startPos.z > -2.8f);
            bool endInStore       = (endPos.x <= 2.8f && endPos.z > -2.8f);

            bool startInFrontOutside = (startPos.z <= -2.8f);
            bool endInFrontOutside   = (endPos.z <= -2.8f);

            // 1. SAĞ DIŞ ALAN (MOTOR PARKI / MAL KABUL DIŞI) <-> SAĞ DIŞ ALAN
            if (startInEastOutside && endInEastOutside)
            {
                rawRoute.Add(new Vector3(14.0f, 0.05f, startPos.z));
                rawRoute.Add(new Vector3(14.0f, 0.05f, endPos.z));
            }
            // 2. SAĞ DIŞ ALAN <-> PERSONEL DİNLENME ODASI
            else if (startInEastOutside && endInStaffRoom)
            {
                rawRoute.Add(new Vector3(14.0f, 0.05f, startPos.z));
                rawRoute.Add(eastDrivewayPos);
                rawRoute.Add(goodsDoorThreshold);
                rawRoute.Add(goodsDoorStorageSide);
                rawRoute.Add(storageAislePos);
                rawRoute.Add(staffDoorOutside);
                rawRoute.Add(staffDoorInside);
            }
            else if (startInStaffRoom && endInEastOutside)
            {
                rawRoute.Add(staffDoorInside);
                rawRoute.Add(staffDoorOutside);
                rawRoute.Add(storageAislePos);
                rawRoute.Add(goodsDoorStorageSide);
                rawRoute.Add(goodsDoorThreshold);
                rawRoute.Add(eastDrivewayPos);
                rawRoute.Add(new Vector3(14.0f, 0.05f, endPos.z));
            }
            // 3. SAĞ DIŞ ALAN <-> DEPO
            else if (startInEastOutside && endInStorage)
            {
                rawRoute.Add(new Vector3(14.0f, 0.05f, startPos.z));
                rawRoute.Add(eastDrivewayPos);
                rawRoute.Add(goodsDoorThreshold);
                rawRoute.Add(goodsDoorStorageSide);
                rawRoute.Add(storageAislePos);
            }
            else if (startInStorage && endInEastOutside)
            {
                rawRoute.Add(storageAislePos);
                rawRoute.Add(goodsDoorStorageSide);
                rawRoute.Add(goodsDoorThreshold);
                rawRoute.Add(eastDrivewayPos);
                rawRoute.Add(new Vector3(14.0f, 0.05f, endPos.z));
            }
            // 4. SAĞ DIŞ ALAN <-> MAĞAZA / REYON ALANI
            else if (startInEastOutside && endInStore)
            {
                rawRoute.Add(new Vector3(14.0f, 0.05f, startPos.z));
                rawRoute.Add(eastDrivewayPos);
                rawRoute.Add(goodsDoorThreshold);
                rawRoute.Add(goodsDoorStorageSide);
                rawRoute.Add(storageAislePos);
                rawRoute.Add(storageDoorStorageSide);
                rawRoute.Add(storageDoorStoreSide);
                rawRoute.Add(storeFoyerPos);
            }
            else if (startInStore && endInEastOutside)
            {
                rawRoute.Add(storeFoyerPos);
                rawRoute.Add(storageDoorStoreSide);
                rawRoute.Add(storageDoorStorageSide);
                rawRoute.Add(storageAislePos);
                rawRoute.Add(goodsDoorStorageSide);
                rawRoute.Add(goodsDoorThreshold);
                rawRoute.Add(eastDrivewayPos);
                rawRoute.Add(new Vector3(14.0f, 0.05f, endPos.z));
            }
            // 5. SAĞ DIŞ ALAN <-> ÖN DIŞ KALDIRIM / OTOPARK
            else if (startInEastOutside && endInFrontOutside)
            {
                rawRoute.Add(new Vector3(14.0f, 0.05f, startPos.z));
                rawRoute.Add(eastDrivewayPos);
                rawRoute.Add(new Vector3(14.0f, 0.05f, -4.5f));
            }
            else if (startInFrontOutside && endInEastOutside)
            {
                rawRoute.Add(new Vector3(14.0f, 0.05f, -4.5f));
                rawRoute.Add(eastDrivewayPos);
                rawRoute.Add(new Vector3(14.0f, 0.05f, endPos.z));
            }
            // 6. PERSONEL ODASI <-> DEPO GEÇİŞİ
            else if (startInStaffRoom && endInStorage)
            {
                rawRoute.Add(staffDoorInside);
                rawRoute.Add(staffDoorOutside);
                rawRoute.Add(storageAislePos);
            }
            else if (startInStorage && endInStaffRoom)
            {
                rawRoute.Add(storageAislePos);
                rawRoute.Add(staffDoorOutside);
                rawRoute.Add(staffDoorInside);
            }
            // 7. PERSONEL ODASI <-> DÜKKAN / ÖN DIŞ GEÇİŞİ
            else if (startInStaffRoom && (endInStore || endInFrontOutside))
            {
                rawRoute.Add(staffDoorInside);
                rawRoute.Add(staffDoorOutside);
                rawRoute.Add(storageAislePos);
                rawRoute.Add(storageDoorStorageSide);
                rawRoute.Add(storageDoorStoreSide);
                rawRoute.Add(storeFoyerPos);
                if (endInFrontOutside)
                {
                    rawRoute.Add(storeOutsidePos);
                }
            }
            else if ((startInStore || startInFrontOutside) && endInStaffRoom)
            {
                if (startInFrontOutside)
                {
                    rawRoute.Add(storeOutsidePos);
                }
                rawRoute.Add(storeFoyerPos);
                rawRoute.Add(storageDoorStoreSide);
                rawRoute.Add(storageDoorStorageSide);
                rawRoute.Add(storageAislePos);
                rawRoute.Add(staffDoorOutside);
                rawRoute.Add(staffDoorInside);
            }
            // 8. DEPO <-> DÜKKAN / ÖN DIŞ GEÇİŞİ
            else if (startInStorage && (endInStore || endInFrontOutside))
            {
                rawRoute.Add(storageDoorStorageSide);
                rawRoute.Add(storageDoorStoreSide);
                rawRoute.Add(storeFoyerPos);
                if (endInFrontOutside)
                {
                    rawRoute.Add(storeOutsidePos);
                }
            }
            else if ((startInStore || startInFrontOutside) && endInStorage)
            {
                if (startInFrontOutside)
                {
                    rawRoute.Add(storeOutsidePos);
                }
                rawRoute.Add(storeFoyerPos);
                rawRoute.Add(storageDoorStoreSide);
                rawRoute.Add(storageDoorStorageSide);
                rawRoute.Add(storageAislePos);
            }
            // 9. DÜKKAN <-> ÖN DIŞ GEÇİŞİ
            else if (startInStore && endInFrontOutside)
            {
                rawRoute.Add(new Vector3(-5.0f, 0.05f, -1.0f));
                rawRoute.Add(storeOutsidePos);
            }
            else if (startInFrontOutside && endInStore)
            {
                rawRoute.Add(storeOutsidePos);
                rawRoute.Add(new Vector3(-5.0f, 0.05f, -1.0f));
            }

            rawRoute.Add(endPos);

            // ROTA TEMİZLEME (Aynı veya çok yakın ara noktaları ayıkla)
            List<Vector3> route = new List<Vector3>();
            for (int i = 0; i < rawRoute.Count; i++)
            {
                Vector3 pt = rawRoute[i];
                if (route.Count == 0 || Vector3.Distance(route[route.Count - 1], pt) > 0.25f)
                {
                    route.Add(pt);
                }
            }
            if (route.Count == 0 || Vector3.Distance(route[route.Count - 1], endPos) > 0.08f)
            {
                route.Add(endPos);
            }

            return route;
        }

        private static readonly List<PlacedFurnitureController> cashierCache = new List<PlacedFurnitureController>(8);
        private static float cashierCacheTime = -1f;

        public static List<PlacedFurnitureController> GetAllCashierCounters()
        {
            if (Time.unscaledTime - cashierCacheTime < 0.35f && cashierCache.Count > 0)
                return cashierCache;

            cashierCache.Clear();
            var allFurniture = PlacedFurnitureController.AllPlacedFurniture;
            int fCount = allFurniture.Count;
            for (int i = 0; i < fCount; i++)
            {
                var f = allFurniture[i];
                if (f != null && f.FurnitureType == FurnitureType.Cashier)
                {
                    cashierCache.Add(f);
                }
            }

            cashierCache.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            cashierCacheTime = Time.unscaledTime;
            return cashierCache;
        }

        public List<StaffTaskData> GetActiveWorkingCashiers()
        {
            List<StaffTaskData> cashiers = new List<StaffTaskData>();
            foreach (var s in staffTaskList)
            {
                if (s != null && s.staffMember != null && s.staffMember.role == StaffRole.Kasiyer &&
                    s.currentState != StaffAIState.WalkingToLeftExit && s.currentState != StaffAIState.Despawned)
                {
                    cashiers.Add(s);
                }
            }
            cashiers.Sort((a, b) => string.Compare(a.staffMember.id, b.staffMember.id, StringComparison.Ordinal));
            return cashiers;
        }

        private void AssignStaffToTaskPosition(StaffTaskData data)
        {
            FreeSofaSeat(data);
            if(data.staffMember!=null && data.staffMember.role==StaffRole.Temizlikçi)
            {
                data.waypoints=null;data.currentWaypointIndex=0;data.currentState=StaffAIState.WorkingOnTask;
                data.nextCleaningSearch=0;return;
            }
            Vector3 taskPos = GetTaskPositionForStaff(data);

            // Eğer Kasiyer ise ve dükkanda atanacak boş kasa yoksa (tüm kasalar doluysa), doğrudan WaitingInBreakRoom durumuna geçip dinlenme odasında beklesin:
            if (data.staffMember != null && data.staffMember.role == StaffRole.Kasiyer)
            {
                List<PlacedFurnitureController> cashiers = GetAllCashierCounters();
                List<StaffTaskData> activeCashiers = GetActiveWorkingCashiers();
                int myIdx = activeCashiers.IndexOf(data);
                if (myIdx >= cashiers.Count)
                {
                    data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, taskPos);
                    data.currentWaypointIndex = 1;
                    data.currentState = StaffAIState.WaitingInBreakRoom;
                    return;
                }
            }

            bool isFarmer = (data.staffMember != null && (data.staffMember.role == StaffRole.Çiftçi || data.staffMember.role == StaffRole.DeneyimliÇiftçi || data.staffMember.role == StaffRole.UstaÇiftlikSorumlusu || data.staffMember.role == StaffRole.TarımOtomasyonUzmanı));

            if (isFarmer)
            {
                // ÇİFTÇİLER ASLA DÜKKANA GİRMEZ! Çiftlik evi önünden doğrudan tarlaya yürürler.
                data.waypoints = new List<Vector3>
                {
                    data.staffObj.transform.position,
                    taskPos
                };
            }
            else
            {
                data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, taskPos);
            }
            data.currentWaypointIndex = 1;
            data.currentState = StaffAIState.ProceedingToTask;
        }

        private Vector3 GetTaskPositionForStaff(StaffTaskData data)
        {
            if (data == null || data.staffMember == null) return new Vector3(-6.5f, 0.05f, 1.8f);

            var furnitureList = PlacedFurnitureController.AllPlacedFurniture;

            switch (data.staffMember.role)
            {
                case StaffRole.Kasiyer:
                {
                    List<PlacedFurnitureController> cashiers = GetAllCashierCounters();
                    List<StaffTaskData> activeCashiers = GetActiveWorkingCashiers();
                    int myIdx = activeCashiers.IndexOf(data);
                    if (myIdx < 0) myIdx = 0;

                    // Eğer dükkanda bu kasiyere atanabilecek bir kasa varsa, o kasanın arkasına yürü:
                    if (myIdx < cashiers.Count)
                    {
                        PlacedFurnitureController myCashier = cashiers[myIdx];
                        return FurnitureModelBuilder.GetCashierStaffPosition(myCashier.transform);
                    }
                    else
                    {
                        // Kasa sayısı yetersizse (örn: 1 kasa var ama 2 kasiyer var), fazlalık kasiyer dinlenme odasında bekler!
                        return GetBreakRoomTargetPosition(data);
                    }
                }

                case StaffRole.Reyoncu:
                    foreach (var f in furnitureList)
                    {
                        if (f != null && f.FurnitureType == FurnitureType.StorageShelf)
                        {
                            return f.GetFrontInteractionPosition(1.2f);
                        }
                    }
                    return new Vector3(-10.0f, 0.05f, 4.0f);

                case StaffRole.Temizlikçi:
                    int cleanerIdx = staffTaskList.FindIndex(s => s != null && s.staffMember != null && s.staffMember.role == StaffRole.Temizlikçi);
                    StaffTaskData cleanerData = (cleanerIdx >= 0 && cleanerIdx < staffTaskList.Count) ? staffTaskList[cleanerIdx] : null;
                    return GetBreakRoomTargetPosition(cleanerData);

                case StaffRole.Güvenlik:
                    return new Vector3(-18.8f, 0.05f, -4.5f);

                case StaffRole.MüşteriHizmetlisi:
                    if (TryAssignServiceDesk(data)) return data.serviceApproach;
                    return GetBreakRoomTargetPosition(data);

                case StaffRole.Maskot:
                    return GetMascotPosition(data);

                case StaffRole.Çiftçi:
                case StaffRole.DeneyimliÇiftçi:
                case StaffRole.UstaÇiftlikSorumlusu:
                case StaffRole.TarımOtomasyonUzmanı:
                    return new Vector3(33.0f, 0.05f, 3.5f); // Çiftlik Evi Önü Spawn/Noktası

                default:
                    return new Vector3(-16.0f, 0.05f, 3.0f);
            }
        }

        private void ExecuteRoleWorkTask(StaffTaskData data, float deltaTime)
        {
            switch (data.staffMember.role)
            {
                case StaffRole.Kasiyer:
                    ExecuteCashierTask(data, deltaTime);
                    break;

                case StaffRole.Reyoncu:
                    ExecuteRestockerTask(data, deltaTime);
                    break;

                case StaffRole.Temizlikçi:
                    ExecuteCleanerTask(data, deltaTime);
                    break;

                case StaffRole.Güvenlik:
                    ExecuteSecurityPatrolTask(data, deltaTime);
                    break;

                case StaffRole.MüşteriHizmetlisi:
                    ExecuteCustomerServiceDeskTask(data, deltaTime);
                    break;

                case StaffRole.Maskot:
                    ExecuteMascotDanceTask(data, deltaTime);
                    break;

                case StaffRole.Çiftçi:
                case StaffRole.DeneyimliÇiftçi:
                case StaffRole.UstaÇiftlikSorumlusu:
                case StaffRole.TarımOtomasyonUzmanı:
                    ExecuteFarmerTask(data, deltaTime);
                    break;

                default:
                    ResetLimbsToRest(data);
                    break;
            }
        }

        private Vector3 GetMascotPosition(StaffTaskData data)
        {
            int rank=0;
            foreach(var other in staffTaskList)
            {
                if(other==data)break;
                if(other!=null && other.staffObj!=null && other.staffMember!=null && other.staffMember.role==StaffRole.Maskot && !other.isLeavingShift)rank++;
            }
            // Shop front-right corner, inside the 3 m pavement and west of the loading lane.
            return new Vector3(9.6f-Mathf.Min(rank,5)*1.1f,.17f,-4.1f);
        }

        private void ExecuteMascotDanceTask(StaffTaskData data, float deltaTime)
        {
            if (data == null || data.staffObj == null) return;

            Vector3 station=GetMascotPosition(data),delta=station-data.staffObj.transform.position;delta.y=0;
            if(delta.sqrMagnitude>.04f)
            {
                data.waypoints=new List<Vector3>{data.staffObj.transform.position,station};data.currentWaypointIndex=1;
                FollowWaypoints(data,deltaTime,null);return;
            }
            Quaternion targetRot = Quaternion.Euler(0f, 180f, 0f);
            data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, targetRot, 360f * deltaTime);

            // Prosedürel Maskot Dans Animasyonu:
            // Ayaklar tam olarak kaldırım zeminine (Y = 0.05f) temas eder, tatlı ritmik bir esneme yapar
            float bounceY = Mathf.Abs(Mathf.Sin(Time.time * 5.0f)) * 0.025f;
            Vector3 curPos = data.staffObj.transform.position;
            curPos.y = station.y + bounceY;
            data.staffObj.transform.position = curPos;

            // 2. Kollar Yukarıda Neşeli El Sallama & Dans
            if (data.leftLimbs != null && data.leftLimbs.Count >= 2 && data.rightLimbs != null && data.rightLimbs.Count >= 2)
            {
                Transform lArm = data.leftLimbs[1];  // Arm_L
                Transform rArm = data.rightLimbs[1];  // Arm_R

                float waveL = Mathf.Sin(Time.time * 7.0f) * 22.0f;
                float waveR = Mathf.Cos(Time.time * 7.0f) * 22.0f;

                lArm.localRotation = Quaternion.Euler(130f + waveL, 0f, 30f);
                rArm.localRotation = Quaternion.Euler(130f + waveR, 0f, -30f);
            }

            // 3. Neşeli Pop-up Gösterimi (Her 5.5 saniyede bir)
            if (Time.time - data.lastMascotCheerTime > 5.5f)
            {
                data.lastMascotCheerTime = Time.time;
                bool isFemale = (data.staffMember != null && (data.staffMember.isFemale || StaffManager.IsFemaleName(data.staffMember.name)));
                string mascotText = isFemale
                    ? LocalizationManager.L("StaffPopup_BunnyDance", "✨ Sevimli Tavşan Dans Ediyor! 🐰 ✨", "✨ Cute Bunny Is Dancing! 🐰 ✨")
                    : LocalizationManager.L("StaffPopup_BearDance", "✨ Neşeli Ayı Dans Ediyor! 🐻 ✨", "✨ Cheerful Bear Is Dancing! 🐻 ✨");
                ShowStockPopup(data.staffObj.transform.position, mascotText);
            }
        }

        private static FieldPlotController GetNearestPlot(Vector3 fromPos, List<FieldPlotController> plotList)
        {
            if (plotList == null || plotList.Count == 0) return null;
            FieldPlotController nearest = null;
            float minDist = float.MaxValue;
            for (int i = 0; i < plotList.Count; i++)
            {
                var p = plotList[i];
                if (p == null) continue;
                float d = Vector3.Distance(fromPos, p.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = p;
                }
            }
            return nearest ?? plotList[0];
        }

        private void ExecuteFarmerTask(StaffTaskData data, float deltaTime)
        {
            if (data == null || data.staffObj == null) return;

            var plots = FieldPlotController.AllPlots;
            if (plots == null || plots.Count == 0)
            {
                ResetLimbsToRest(data);
                return;
            }

            // 1. PARSEL BAŞINDA ÇALIŞMA / SULAMA / KONTROL EVRESİ (5 - 10 Saniye)
            if (data.isFarmerWorkingOnPlot)
            {
                if (data.taskTimer > 0f)
                {
                    data.taskTimer -= deltaTime;

                    // Parsele doğru dön
                    if (data.targetPlot != null)
                    {
                        Vector3 lookDir = (data.targetPlot.transform.position - data.staffObj.transform.position).normalized;
                        lookDir.y = 0f;
                        if (lookDir != Vector3.zero)
                        {
                            data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(lookDir), 360f * deltaTime);
                        }
                    }

                    // Ritmik Çapa / Sulama / Mahsul Kontrolü Kol Animasyonu
                    if (data.leftLimbs != null && data.leftLimbs.Count >= 2 && data.rightLimbs != null && data.rightLimbs.Count >= 2)
                    {
                        Transform lArm = data.leftLimbs[1];  // Arm_L
                        Transform rArm = data.rightLimbs[1];  // Arm_R

                        float workSwing = Mathf.Sin(Time.time * 5.5f) * 22.0f;
                        lArm.localRotation = Quaternion.Euler(38f + workSwing, 15f, 0f);
                        rArm.localRotation = Quaternion.Euler(38f - workSwing, -15f, 0f);
                    }
                    return;
                }

                // Çalışma süresi (5-10s) tamamlandı! Eylemi gerçekleştir
                if (data.targetPlot != null)
                {
                    if (data.targetPlot.NeedsWater && !data.targetPlot.WateredToday)
                    {
                        data.targetPlot.WaterCrop(); // Sulama işlemi tamamlandı
                    }
                    else if (data.targetPlot.State == PlotState.SpoiledTrash)
                    {
                        data.targetPlot.ClearSpoiledPlot(); // Çürümüş ekin temizlendi
                    }
                }

                data.isFarmerWorkingOnPlot = false;
                data.targetPlot = null;
                if (data.waypoints != null) data.waypoints.Clear();
                data.currentWaypointIndex = 0;
                data.taskTimer = Random.Range(0.8f, 1.6f); // Bir sonraki parsele geçmeden önce kısa mola
                ResetLimbsToRest(data);
                return;
            }

            // 2. KISA GEÇİŞ MOLA ZAMANLAYICISI
            if (data.taskTimer > 0f)
            {
                data.taskTimer -= deltaTime;
                ResetLimbsToRest(data);
                return;
            }

            // 3. YENİ PARSEL HEDEFİ BELİRLEME (Sürekli Devriye & Kontrol)
            if (data.waypoints == null || data.waypoints.Count == 0 || data.currentWaypointIndex >= data.waypoints.Count)
            {
                // Çiftçi Akıllı Parsel Arama & Öncelik Sıralaması:
                // 1. Öncelik: Sulanması Gereken Ekinler
                // 2. Öncelik: Çürümüş Ekinler (Temizlenmeli)
                // 3. Öncelik: Büyüyen / Ekili Ekinler (Mahsul kontrolü için)
                // 4. Öncelik: Tüm Tarlalar (Toprak kontrol devriyesi için - Asla boş durmaz!)
                List<FieldPlotController> needWaterPlots = new List<FieldPlotController>();
                List<FieldPlotController> spoiledPlots = new List<FieldPlotController>();
                List<FieldPlotController> growingPlots = new List<FieldPlotController>();
                List<FieldPlotController> allValidPlots = new List<FieldPlotController>();

                for (int i = 0; i < plots.Count; i++)
                {
                    var p = plots[i];
                    if (p == null) continue;
                    allValidPlots.Add(p);

                    if (p.State == PlotState.SpoiledTrash)
                    {
                        spoiledPlots.Add(p);
                    }
                    else if ((p.State == PlotState.PlantedSprout || p.State == PlotState.Growing) && (p.NeedsWater || !p.WateredToday))
                    {
                        needWaterPlots.Add(p);
                    }
                    else if (p.State == PlotState.PlantedSprout || p.State == PlotState.Growing || p.State == PlotState.RipeReadyToHarvest)
                    {
                        growingPlots.Add(p);
                    }
                }

                FieldPlotController targetPlot = null;
                if (needWaterPlots.Count > 0)
                {
                    targetPlot = GetNearestPlot(data.staffObj.transform.position, needWaterPlots);
                }
                else if (spoiledPlots.Count > 0)
                {
                    targetPlot = GetNearestPlot(data.staffObj.transform.position, spoiledPlots);
                }
                else if (growingPlots.Count > 0)
                {
                    targetPlot = growingPlots[Random.Range(0, growingPlots.Count)];
                }
                else if (allValidPlots.Count > 0)
                {
                    targetPlot = allValidPlots[Random.Range(0, allValidPlots.Count)];
                }

                if (targetPlot != null)
                {
                    data.targetPlot = targetPlot;
                    Vector3 plotTargetPos = targetPlot.transform.position;
                    data.waypoints = new List<Vector3>
                    {
                        data.staffObj.transform.position,
                        plotTargetPos
                    };
                    data.currentWaypointIndex = 1;
                    data.isFarmerWorkingOnPlot = false;
                }
            }

            // 4. PARSELE DOĞRU YÜRÜYÜŞ
            Vector3 currentPos = data.staffObj.transform.position;
            Vector3 targetPos = (data.waypoints != null && data.currentWaypointIndex < data.waypoints.Count) ? data.waypoints[data.currentWaypointIndex] : currentPos;
            Vector3 toTarget = targetPos - currentPos;
            float dist = toTarget.magnitude;

            if (dist < 0.25f && PedestrianNavigation.Clear(currentPos, targetPos))
            {
                // Tarlanın başına ulaşıldı: 5 ile 10 saniye arası sürecek çalışma/sulama/kontrol sürecini başlat
                data.isFarmerWorkingOnPlot = true;
                data.taskTimer = Random.Range(5.5f, 9.5f); // 5 - 10 saniye sürsün!
                if (data.waypoints != null) data.waypoints.Clear();
                data.currentWaypointIndex = 0;
                return;
            }

            Vector3 moveDir = toTarget.normalized;
            float stepDist = 2.4f * 1.15f * deltaTime;
            Vector3 navigatedPosition = PedestrianNavigation.Move(data.staffObj, targetPos, stepDist);
            Vector3 avoidanceDir = navigatedPosition - currentPos;

            if (avoidanceDir != Vector3.zero)
            {
                data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(avoidanceDir), 360f * deltaTime);
            }
            data.staffObj.transform.position = navigatedPosition;

            data.walkCycleTimer += deltaTime * 8.5f * 1.15f;
            AnimateLimbs(data, Mathf.Sin(data.walkCycleTimer) * 26.0f);
        }

        private bool TryAssignServiceDesk(StaffTaskData data)
        {
            if (data.serviceDesk != null)
                data.serviceApproach = data.serviceDesk.transform.TransformPoint(data.serviceApproachLocal);
            if (data.serviceDesk != null && data.serviceDesk.isActiveAndEnabled &&
                !PedestrianNavigation.IsBlocked(data.serviceApproach)) return true;
            ReleaseServiceDeskSeat(data);
            data.serviceDesk = null;
            if (Time.unscaledTime < data.nextServiceSearch) return false;
            data.nextServiceSearch = Time.unscaledTime + 1f;
            foreach (var desk in PlacedFurnitureController.AllPlacedFurniture)
            {
                if (desk == null || !desk.isActiveAndEnabled || desk.FurnitureType != FurnitureType.CustomerServiceDesk) continue;
                bool occupied = false;
                foreach (var other in staffTaskList)
                    if (other != null && other != data && other.staffObj != null && other.serviceDesk == desk &&
                        (other.currentState == StaffAIState.ProceedingToTask || other.currentState == StaffAIState.WorkingOnTask))
                    { occupied = true; break; }
                if (occupied) continue;
                Vector2 size = PedestrianNavigation.Footprint(desk.FurnitureType);
                for (int slot=0;slot<3;slot++)
                {
                    float side=slot==0?0f:(slot==1?1f:-1f)*size.x*.35f;
                    Vector3 point=desk.transform.TransformPoint(new Vector3(side,0,size.y*.5f+.35f));
                    point.y=desk.transform.position.y+.05f;
                    if (PedestrianNavigation.IsBlocked(point) || PedestrianNavigation.Route(data.staffObj.transform.position,point)==null) continue;
                    data.serviceDesk=desk;data.serviceApproach=point;
                    data.serviceApproachLocal=desk.transform.InverseTransformPoint(point);return true;
                }
            }
            return false;
        }

        private void ExecuteCustomerServiceDeskTask(StaffTaskData data, float deltaTime)
        {
            if (!data.isSeatedAtServiceDesk) ResetLimbsToRest(data);

            if (!TryAssignServiceDesk(data)) { ResetLimbsToRest(data); return; }
            {
                var f = data.serviceDesk;
                {
                    Transform seat = f.transform.Find("CustomerServiceVisual/StaffSeat");
                    Transform facing = f.transform.Find("CustomerServiceVisual/StaffFacing");
                    Vector3 seatPosition = seat != null ? seat.position : f.transform.TransformPoint(new Vector3(0f, 0.53f, 0.45f));
                    Vector3 forward = facing != null && seat != null ? facing.position - seat.position : -f.transform.forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude > 0.001f) data.staffObj.transform.rotation = Quaternion.LookRotation(forward);

                    if (!data.isSeatedAtServiceDesk)
                    {
                        Vector3 approach = data.serviceApproach;
                        approach.y = data.staffObj.transform.position.y;
                        if (Vector3.Distance(data.staffObj.transform.position, approach) > .15f || !PedestrianNavigation.Clear(data.staffObj.transform.position, approach))
                        {
                            data.staffObj.transform.position = PedestrianNavigation.Move(data.staffObj, approach, WALK_SPEED * deltaTime);
                            return;
                        }
                    }
                    // Align the actual hip pivots with the cushion, allowing for thigh thickness.
                    float hipHeight = 0.80f;
                    if (data.leftLimbs != null && data.leftLimbs.Count > 0 && data.leftLimbs[0] != null)
                        hipHeight = data.staffObj.transform.InverseTransformPoint(data.leftLimbs[0].position).y;
                    float scaleY = data.staffObj.transform.lossyScale.y;
                    data.staffObj.transform.position = seatPosition + Vector3.up * ((0.10f - hipHeight) * scaleY);
                    data.serviceDeskFloorY = f.transform.position.y + 0.05f;
                    data.isSeatedAtServiceDesk = true;
                    ApplySittingPose(data);
                    // Keep thighs horizontal and lower legs vertical at the desk.
                    PoseServiceDeskLeg(data.leftLimbs);
                    PoseServiceDeskLeg(data.rightLimbs);
                    return;
                }
            }
            ResetLimbsToRest(data);
        }

        private static void PoseServiceDeskLeg(List<Transform> limbs)
        {
            if (limbs == null || limbs.Count == 0 || limbs[0] == null) return;
            limbs[0].localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Transform knee = limbs[0].Find("ServiceKnee");
            if (knee != null) knee.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        #region Role Task Executions

        private void ExecuteCashierTask(StaffTaskData data, float deltaTime)
        {
            ResetLimbsToRest(data);

            List<PlacedFurnitureController> cashierFurnitureList = GetAllCashierCounters();
            List<StaffTaskData> activeCashierList = GetActiveWorkingCashiers();
            int myCashierIndex = activeCashierList.IndexOf(data);
            if (myCashierIndex < 0) myCashierIndex = 0;

            // Eğer bu kasiyere atanacak bir kasa varsa o kasaya geçip çalışır:
            if (myCashierIndex < cashierFurnitureList.Count)
            {
                FreeSofaSeat(data);
                PlacedFurnitureController myCashier = cashierFurnitureList[myCashierIndex];
                Vector3 myCashierPos = FurnitureModelBuilder.GetCashierStaffPosition(myCashier.transform);
                Quaternion myCashierRot = Quaternion.LookRotation(-myCashier.transform.forward);

                if (Vector3.Distance(data.staffObj.transform.position, myCashierPos) > 0.08f ||
                    !PedestrianNavigation.Clear(data.staffObj.transform.position, myCashierPos))
                {
                    Vector3 previous = data.staffObj.transform.position;
                    data.staffObj.transform.position = PedestrianNavigation.Move(data.staffObj, myCashierPos, WALK_SPEED * deltaTime);
                    Vector3 walking = data.staffObj.transform.position - previous;
                    if (walking.sqrMagnitude > 0.000001f)
                        data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(walking), 360f * deltaTime);
                    return;
                }
                data.staffObj.transform.rotation = myCashierRot;

                // KASA BAŞINDA KESİNTİSİZ DEVİR TESLİM KONTROLÜ:
                if (TimeManager.Instance != null)
                {
                    int curHour = TimeManager.Instance.Hour;
                    int curMin = TimeManager.Instance.Minute;
                    bool isShiftActive = IsStaffShiftActive(data.staffMember, curHour, curMin, out bool isEarly);

                    bool isEndingShift = !isShiftActive || (curMin >= 55 && (curHour == 13 || curHour == 21 || curHour == 5));

                    if (isEndingShift)
                    {
                        StaffTaskData incomingCashier = staffTaskList.Find(other =>
                            other != data &&
                            other.staffMember != null &&
                            other.staffMember.role == StaffRole.Kasiyer &&
                            other.currentState != StaffAIState.WalkingToLeftExit &&
                            other.currentState != StaffAIState.Despawned &&
                            (other.currentState == StaffAIState.WorkingOnTask ||
                             (other.currentState == StaffAIState.ProceedingToTask && Vector3.Distance(other.staffObj.transform.position, myCashierPos) < 2.5f))
                        );

                        if (incomingCashier != null)
                        {
                            data.currentState = StaffAIState.HandingOverShift;
                            data.taskTimer = 3.5f;
                        }
                    }
                }
            }
            else
            {
                // Dükkandaki kasa sayısından daha fazla kasiyer vardiyada ise, fazlalık personeller dinlenme odasında bekler!
                ExecuteBreakRoomRestAndSeating(data, deltaTime);
            }
        }

        private float lastStorageWarningTime = 0f;

        private static Material carriedTapeMat;
        private static Material carriedFlapMat;
        private static readonly Quaternion CarryArmRotL = Quaternion.Euler(-38f, 12f, 6f);
        private static readonly Quaternion CarryArmRotR = Quaternion.Euler(-38f, -12f, -6f);

        private void CreateCarriedBoxesOnStaff(StaffTaskData data, int count)
        {
            ClearCarriedBoxesOnStaff(data);
            if (data.staffObj == null || count <= 0) return;

            data.carriedBoxesRoot = new GameObject("CarriedBoxes");
            data.carriedBoxesRoot.transform.SetParent(data.staffObj.transform, false);
            data.carriedBoxesRoot.transform.localPosition = Vector3.zero;
            data.carriedBoxesRoot.transform.localRotation = Quaternion.identity;

            if (count == 1)
            {
                CreateHandheldCrate(data.carriedBoxesRoot.transform, "Box_1",
                    new Vector3(0f, 1.22f, 0.38f), new Vector3(0.40f, 0.24f, 0.32f), 4f);
            }
            else
            {
                CreateHandheldCrate(data.carriedBoxesRoot.transform, "Box_Left",
                    new Vector3(0f, 1.12f, 0.38f), new Vector3(0.38f, 0.22f, 0.30f), 3f);
                CreateHandheldCrate(data.carriedBoxesRoot.transform, "Box_Right",
                    new Vector3(0.01f, 1.35f, 0.36f), new Vector3(0.36f, 0.22f, 0.28f), 2f);
            }

            ApplyCarryArmPose(data);
            data.isCarryingBoxes = true;
        }

        private static void CreateHandheldCrate(Transform parent, string name, Vector3 localPos, Vector3 size, float tiltX)
        {
            EnsureCarriedBoxMaterials();

            GameObject crate = new GameObject(name);
            crate.transform.SetParent(parent, false);
            crate.transform.localPosition = localPos;
            crate.transform.localRotation = Quaternion.Euler(tiltX, 7f, 2f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(crate.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = size;
            if (FurnitureModelBuilder.CardboardBoxMaterial != null)
                body.GetComponent<Renderer>().sharedMaterial = FurnitureModelBuilder.CardboardBoxMaterial;
            DestroyCollider(body);

            GameObject tapeX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tapeX.name = "TapeX";
            tapeX.transform.SetParent(crate.transform, false);
            tapeX.transform.localPosition = new Vector3(0f, size.y * 0.51f, 0f);
            tapeX.transform.localScale = new Vector3(size.x * 1.02f, 0.012f, size.z * 0.18f);
            tapeX.GetComponent<Renderer>().sharedMaterial = carriedTapeMat;
            DestroyCollider(tapeX);

            GameObject tapeZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tapeZ.name = "TapeZ";
            tapeZ.transform.SetParent(crate.transform, false);
            tapeZ.transform.localPosition = new Vector3(0f, size.y * 0.51f, 0f);
            tapeZ.transform.localScale = new Vector3(size.x * 0.16f, 0.012f, size.z * 1.02f);
            tapeZ.GetComponent<Renderer>().sharedMaterial = carriedTapeMat;
            DestroyCollider(tapeZ);

            GameObject flapL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flapL.name = "FlapL";
            flapL.transform.SetParent(crate.transform, false);
            flapL.transform.localPosition = new Vector3(-size.x * 0.22f, size.y * 0.52f, 0f);
            flapL.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            flapL.transform.localScale = new Vector3(size.x * 0.46f, 0.016f, size.z * 0.92f);
            flapL.GetComponent<Renderer>().sharedMaterial = carriedFlapMat;
            DestroyCollider(flapL);

            GameObject flapR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flapR.name = "FlapR";
            flapR.transform.SetParent(crate.transform, false);
            flapR.transform.localPosition = new Vector3(size.x * 0.22f, size.y * 0.52f, 0f);
            flapR.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            flapR.transform.localScale = new Vector3(size.x * 0.46f, 0.016f, size.z * 0.92f);
            flapR.GetComponent<Renderer>().sharedMaterial = carriedFlapMat;
            DestroyCollider(flapR);

            GameObject strap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strap.name = "SideLabel";
            strap.transform.SetParent(crate.transform, false);
            strap.transform.localPosition = new Vector3(0f, 0.01f, size.z * 0.51f);
            strap.transform.localScale = new Vector3(size.x * 0.55f, size.y * 0.28f, 0.01f);
            strap.GetComponent<Renderer>().sharedMaterial = carriedTapeMat;
            DestroyCollider(strap);
        }

        private static void EnsureCarriedBoxMaterials()
        {
            if (carriedTapeMat != null && carriedFlapMat != null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            carriedTapeMat = new Material(shader);
            carriedTapeMat.color = new Color(0.90f, 0.78f, 0.28f);
            carriedFlapMat = new Material(shader);
            carriedFlapMat.color = new Color(0.68f, 0.50f, 0.30f);
        }

        private static void DestroyCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }

        private static void ApplyCarryArmPose(StaffTaskData data)
        {
            if (data == null) return;
            SetArmPose(data.leftLimbs, CarryArmRotL);
            SetArmPose(data.rightLimbs, CarryArmRotR);
        }

        private static void SetArmPose(List<Transform> limbs, Quaternion rot)
        {
            if (limbs == null) return;
            for (int i = 0; i < limbs.Count; i++)
            {
                Transform t = limbs[i];
                if (t != null && t.name.StartsWith("Arm", StringComparison.Ordinal))
                    t.localRotation = rot;
            }
        }

        private void RemoveOneCarriedBox(StaffTaskData data)
        {
            if (data.carriedBoxesRoot != null)
            {
                Transform right = data.carriedBoxesRoot.transform.Find("Box_Right");
                Transform left = data.carriedBoxesRoot.transform.Find("Box_Left");
                Transform box1 = data.carriedBoxesRoot.transform.Find("Box_1");

                if (right != null) Destroy(right.gameObject);
                else if (left != null) Destroy(left.gameObject);
                else if (box1 != null) Destroy(box1.gameObject);

                int remaining = data.carriedBoxesRoot.transform.childCount - 1;
                if (remaining <= 0)
                {
                    data.isCarryingBoxes = false;
                }
            }
        }

        private void ClearCarriedBoxesOnStaff(StaffTaskData data)
        {
            if (data.carriedBoxesRoot != null)
            {
                Destroy(data.carriedBoxesRoot);
                data.carriedBoxesRoot = null;
            }
            data.isCarryingBoxes = false;
        }

        private void ResetRestockerTargetFields(StaffTaskData data)
        {
            data.targetShelf1 = null;
            data.targetRowId1 = -1;
            data.carriedAmount1 = 0;
            data.carriedLots1 = new List<ProductLot>();

            data.targetShelf2 = null;
            data.targetRowId2 = -1;
            data.carriedAmount2 = 0;
            data.carriedLots2 = new List<ProductLot>();

            data.sourceStorageShelf = null;
            data.sourceStorageRowId = -1;
            data.sourceStorageShelf2 = null;
            data.sourceStorageRowId2 = -1;
        }

        private static void ReturnUndeliveredPackageToTruck(WholesaleProductDef pack)
        {
            if (pack == null) return;
            if (GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckOnTheWay)
            {
                GreenTruckDeliveryManager.Instance.ReturnPackageToTruck(pack);
                return;
            }
            if (WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckOnTheWay)
            {
                WholesaleTruckManager.Instance.ReturnPackageToTruck(pack);
            }
        }

        private static bool CanRestockFrom(ShelfRowData source, ShelfRowData target)
        {
            if (source == null || target == null || target.IsUnassigned || target.IsFull) return false;
            ProductPassportService.EnsureRowLots(source);
            ProductPassportService.SyncShelfRowIdentity(target);
            if (!ProductPassportService.RowHoldsProduct(source, target.productId, target.productName)) return false;
            foreach (ProductLot lot in source.lots)
            {
                if (lot != null && lot.quantity > 0 && lot.productId != target.productId) return false;
            }
            return source.currentStock > 0;
        }

        private static bool TryPlaceRestockLots(PlacedFurnitureController shelf, int rowIndex, List<ProductLot> carried)
        {
            ShelfRowData target = shelf.rows[rowIndex];
            ProductPassportService.SyncShelfRowIdentity(target);
            // The player may change the assignment while the stocker is walking.
            // Only matching lots may enter the selected row; keep every rejected unit.
            for (int i = carried.Count - 1; i >= 0; i--)
            {
                ProductLot lot = carried[i];
                if (lot == null || lot.quantity <= 0) { carried.RemoveAt(i); continue; }
                if (target.IsUnassigned || string.IsNullOrEmpty(target.productId) || lot.productId != target.productId) continue;
                int count = Mathf.Min(lot.quantity, Mathf.Max(0, target.maxCapacity - target.currentStock));
                if (count <= 0) continue;
                var accepted = new List<ProductLot> { lot.Clone(count) };
                int added = ProductPassportService.AddStock(target, accepted, target.maxCapacity);
                lot.quantity -= added;
                if (lot.quantity == 0) carried.RemoveAt(i);
            }
            shelf.UpdateRow3DProductMeshes(rowIndex + 1);

            // Return leftovers to compatible warehouse rows. If storage is full,
            // retain the box and retry instead of deleting stock or changing a selection.
            foreach (var storage in PlacedFurnitureController.AllPlacedFurniture)
            {
                if (storage == null || storage.FurnitureType != FurnitureType.StorageShelf || storage.rows == null) continue;
                for (int r = 0; r < storage.rows.Length && carried.Count > 0; r++)
                {
                    ShelfRowData row = storage.rows[r];
                    if (row == null || row.IsFull) continue;
                    ProductLot lot = carried[0];
                    WholesaleProductDef product = WholesaleDatabase.GetProductById(lot.productId);
                    if (product == null) continue;
                    if (row.currentStock <= 0)
                    {
                        ProductPassportService.ClearRowContents(row);
                        row.productId = product.id;
                        row.productName = product.name;
                        row.unitPrice = product.SalePricePerUnit;
                    }
                    if (!ProductPassportService.RowHoldsProduct(row, product.id, product.name)) continue;
                    int count = Mathf.Min(lot.quantity, row.maxCapacity - row.currentStock);
                    var returned = new List<ProductLot> { lot.Clone(count) };
                    lot.quantity -= ProductPassportService.AddStock(row, returned, row.maxCapacity);
                    if (lot.quantity <= 0) carried.RemoveAt(0);
                    storage.UpdateRow3DProductMeshes(r + 1);
                }
            }
            return carried.Count == 0;
        }

        private static bool IsStoreShelf(FurnitureType t)
        {
            return t == FurnitureType.Shelf || t == FurnitureType.Fridge || t == FurnitureType.OrganicFridge || t == FurnitureType.Freezer ||
                   t == FurnitureType.ProduceShelf || t == FurnitureType.BakeryCounter ||
                   t == FurnitureType.CosmeticShelf || t == FurnitureType.ElectronicsShelf ||
                   t == FurnitureType.ButcherCounter || t == FurnitureType.GourmetShelf;
        }

        private Vector3 GetStorageShelfPosition()
        {
            var shelves = PlacedFurnitureController.AllPlacedFurniture;
            int count = shelves.Count;
            for (int i = 0; i < count; i++)
            {
                var f = shelves[i];
                if (f != null && f.FurnitureType == FurnitureType.StorageShelf)
                {
                    return f.GetFrontInteractionPosition(1.2f);
                }
            }
            return new Vector3(-10.0f, 0.05f, 4.0f);
        }

        private void ShowStockPopup(Vector3 pos, string text = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                text = LocalizationManager.L("StaffPopup_StockComplete", "+Stok Tamamlandı 📦", "+Stock Refilled 📦");
            }
            GameObject popupObj = new GameObject("Popup_StockRefill");
            popupObj.transform.position = pos + Vector3.up * 1.8f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340f, 60f);
            popupObj.transform.localScale = Vector3.one * 0.012f;

            if (GameplayPerf.MainCamera != null) popupObj.transform.rotation = GameplayPerf.MainCamera.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = textObj.AddComponent<Text>();
            txt.font = UIStyleUtility.GetGlobalFont(22);
            txt.text = text;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.20f, 0.85f, 0.35f);

            Destroy(popupObj, 1.5f);
        }

        private bool IsShelfRowClaimedByOtherRestocker(StaffTaskData currentData, PlacedFurnitureController storeShelf, int rowId)
        {
            if (storeShelf == null || rowId < 0 || staffTaskList == null) return false;
            for (int i = 0; i < staffTaskList.Count; i++)
            {
                var other = staffTaskList[i];
                if (other == null || other == currentData || other.staffMember == null || other.staffMember.role != StaffRole.Reyoncu) continue;

                if ((other.targetShelf1 == storeShelf && other.targetRowId1 == rowId) ||
                    (other.targetShelf2 == storeShelf && other.targetRowId2 == rowId))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsStorageRowClaimedByOtherRestocker(StaffTaskData currentData, PlacedFurnitureController storageShelf, int storageRowId)
        {
            if (storageShelf == null || storageRowId < 0 || staffTaskList == null) return false;
            int timesClaimed = 0;
            for (int i = 0; i < staffTaskList.Count; i++)
            {
                var other = staffTaskList[i];
                if (other == null || other == currentData || other.staffMember == null || other.staffMember.role != StaffRole.Reyoncu) continue;

                if (other.sourceStorageShelf == storageShelf && other.sourceStorageRowId == storageRowId) timesClaimed++;
                if (other.sourceStorageShelf2 == storageShelf && other.sourceStorageRowId2 == storageRowId) timesClaimed++;
            }

            if (storageRowId < storageShelf.rows.Length)
            {
                var sRow = storageShelf.rows[storageRowId];
                if (sRow != null)
                {
                    int remainingStock = sRow.currentStock - (timesClaimed * 20);
                    return remainingStock <= 0;
                }
            }
            return false;
        }

        private void ExecuteRestockerTask(StaffTaskData data, float deltaTime)
        {
            // 1. Yol İzleme Aşaması
            if (data.waypoints != null && data.currentWaypointIndex < data.waypoints.Count)
            {
                FollowWaypoints(data, deltaTime, onComplete: () => {
                    data.taskTimer = 1.2f; // Noktaya ulaşınca 1.2 saniye koli alma/bırakma beklemesi
                });
                return;
            }

            // A completed/stale route is not proof of arrival at the current furniture.
            // Keep courier collection/loading on its existing path.
            if (!data.isGatheringForOnlineOrder && !data.isCarryingDeliveryBoxToMotorcycle)
            {
                PlacedFurnitureController destination = data.isUnloadingTruck
                    ? (data.isFetchingFromTruck ? null : data.targetStorageShelfForDeposit)
                    : data.isFetchingFromStorage ? data.sourceStorageShelf
                    : data.targetShelf1 != null ? data.targetShelf1 : data.targetShelf2;
                bool hasDestination = destination != null || (data.isUnloadingTruck && data.isFetchingFromTruck);
                if (hasDestination)
                {
                    Vector3 point = destination != null ? destination.GetFrontInteractionPosition(1.2f) : DeliveryTruckVisuals.UnloadPosition;
                    Vector3 distance = point - data.staffObj.transform.position;
                    distance.y = 0f;
                    if (distance.sqrMagnitude > 0.45f * 0.45f)
                    {
                        data.taskTimer = 0f;
                        data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, point);
                        data.currentWaypointIndex = 1;
                        return;
                    }
                    if (data.taskTimer <= 0f) data.taskTimer = 1.2f;
                }
            }

            // 2. Noktada İnceleme / Bekleme & Koli İşlemi Aşaması
            if (data.taskTimer > 0f)
            {
                data.taskTimer -= deltaTime;
                ResetLimbsToRest(data);
                if (data.isUnloadingTruck && data.isFetchingFromTruck)
                    data.staffObj.transform.rotation = Quaternion.LookRotation(Vector3.right);

                if (data.taskTimer <= 0f)
                {
                    if (data.isGatheringForOnlineOrder && data.targetOnlineOrder != null)
                    {
                        if (data.isCarryingDeliveryBoxToMotorcycle)
                        {
                            // A) MOTORSİKLETİN YANINA ULAŞILDI: Motora doğru dön ve koli bagajına yükle!
                            if (data.targetOnlineOrder.assignedMotorcycle != null)
                            {
                                Vector3 dirToMoto = data.targetOnlineOrder.assignedMotorcycle.transform.position - data.staffObj.transform.position;
                                dirToMoto.y = 0f;
                                if (dirToMoto != Vector3.zero) data.staffObj.transform.rotation = Quaternion.LookRotation(dirToMoto);
                            }

                            var finishedOrder = data.targetOnlineOrder;
                            data.targetOnlineOrder = null;
                            data.isGatheringForOnlineOrder = false;
                            data.isCarryingDeliveryBoxToMotorcycle = false;
                            data.onlineGatherItems.Clear();
                            data.currentOnlineGatherIndex = 0;
                            ClearCarriedBoxesOnStaff(data);

                            if (OnlineMarketOrderManager.Instance != null && finishedOrder != null)
                            {
                                OnlineMarketOrderManager.Instance.NotifyOrderGatheredByStocker(finishedOrder);
                            }

                            Vector3 popupPos = (data.staffObj != null) ? data.staffObj.transform.position : Vector3.zero;
                            ShowStockPopup(popupPos, LocalizationManager.L("StaffPopup_OrderLoaded", "🛵 Sipariş Motora Yüklendi! ✨", "🛵 Order Loaded onto Motorcycle! ✨"));

                            // Reyoncu işini bitirdi: Derhal Mal Kabul kapısından dükkan/depo içine dönüş rotasını başlat!
                            Vector3 returnRestSpot = GetBreakRoomTargetPosition(data);
                            data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, returnRestSpot);
                            data.currentWaypointIndex = 1;
                            data.taskTimer = 0.5f;
                            return;
                        }
                        else
                        {
                            // B) REYON VEYA DEPO RAFINA ULAŞILDI: Ürünü raftan topla!
                            if (data.onlineGatherItems != null && data.currentOnlineGatherIndex < data.onlineGatherItems.Count)
                            {
                                var gatherItem = data.onlineGatherItems[data.currentOnlineGatherIndex];
                                if (gatherItem != null && gatherItem.shelf != null)
                                {
                                    Vector3 faceDir = gatherItem.shelf.transform.forward;
                                    faceDir.y = 0f;
                                    if (faceDir != Vector3.zero) data.staffObj.transform.rotation = Quaternion.LookRotation(faceDir);

                                    if (gatherItem.rowIndex >= 0 && gatherItem.rowIndex < gatherItem.shelf.rows.Length)
                                    {
                                        var row = gatherItem.shelf.rows[gatherItem.rowIndex];
                                        if (row != null)
                                        {
                                            int actualTake = Mathf.Min(gatherItem.amountToGather, row.currentStock);
                                            ProductPassportService.RemoveStock(row, actualTake);
                                            if (row.currentStock <= 0 && gatherItem.shelf.FurnitureType == FurnitureType.StorageShelf)
                                            {
                                                row.productName = "";
                                                row.productId = "";
                                                row.unitPrice = 0f;
                                            }
                                            gatherItem.shelf.UpdateRow3DProductMeshes(gatherItem.rowIndex + 1);

                                            if (gatherItem.productIndexInOrder >= 0 && gatherItem.productIndexInOrder < data.targetOnlineOrder.gatheredQuantities.Count)
                                            {
                                                data.targetOnlineOrder.gatheredQuantities[gatherItem.productIndexInOrder] += actualTake;
                                            }

                                            string prodName = gatherItem.productDef != null ? gatherItem.productDef.LocalizedName : LocalizationManager.L("Label_Product", "Ürün", "Product");
                                            ShowStockPopup(data.staffObj.transform.position, string.Format(LocalizationManager.L("StaffPopup_ProductCollectedFmt", "+{0} {1} Toplandı 🛍️", "+{0} {1} Collected 🛍️"), actualTake, prodName));
                                        }
                                    }
                                }

                                data.currentOnlineGatherIndex++;
                            }

                            // Sırada toplanacak başka raf var mı?
                            if (data.onlineGatherItems != null && data.currentOnlineGatherIndex < data.onlineGatherItems.Count)
                            {
                                var nextItem = data.onlineGatherItems[data.currentOnlineGatherIndex];
                                if (nextItem != null && nextItem.shelf != null)
                                {
                                    Vector3 nextShelfPos = nextItem.shelf.GetFrontInteractionPosition(1.2f);
                                    data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, nextShelfPos);
                                    data.currentWaypointIndex = 1;
                                    return;
                                }
                            }

                            // Tüm ürünler raflardan toplandı: Şimdi koliyi eline al ve motora yönel!
                            CreateCarriedBoxesOnStaff(data, 1);
                            data.isCarryingDeliveryBoxToMotorcycle = true;

                            Vector3 motoPos = (data.targetOnlineOrder != null && data.targetOnlineOrder.assignedMotorcycle != null)
                                ? data.targetOnlineOrder.assignedMotorcycle.transform.position + new Vector3(0.40f, 0f, 0f)
                                : new Vector3(13.0f, 0.05f, 8.0f);

                            data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, motoPos);
                            data.currentWaypointIndex = 1;
                            ShowStockPopup(data.staffObj.transform.position, LocalizationManager.L("StaffPopup_OrderPacked", "📦 Sipariş Paketlendi -> Motora Gidiyor", "📦 Order Packed -> Heading to Motorcycle"));
                            return;
                        }
                    }

                    if (data.isUnloadingTruck)
                    {
                        if (data.isFetchingFromTruck)
                        {
                            // A) KAMYONUN YANINA ULAŞILDI: Kamyondan 2 koli çek!
                            data.isFetchingFromTruck = false;
                            data.carriedProduct1 = null;
                            data.carriedProduct2 = null;

                            int fetchedCount = 0;
                            if (WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckAtDockWaitingForUnload)
                            {
                                if (WholesaleTruckManager.Instance.TryFetchPackageFromTruck(out WholesaleProductDef p1))
                                {
                                    data.carriedProduct1 = p1;
                                    fetchedCount++;
                                }
                                if (WholesaleTruckManager.Instance.TryFetchPackageFromTruck(out WholesaleProductDef p2))
                                {
                                    data.carriedProduct2 = p2;
                                    fetchedCount++;
                                }
                            }

                            if (fetchedCount == 0 && GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckAtDockWaitingForUnload)
                            {
                                if (GreenTruckDeliveryManager.Instance.TryFetchPackageFromTruck(out WholesaleProductDef p1))
                                {
                                    data.carriedProduct1 = p1;
                                    fetchedCount++;
                                }
                                if (GreenTruckDeliveryManager.Instance.TryFetchPackageFromTruck(out WholesaleProductDef p2))
                                {
                                    data.carriedProduct2 = p2;
                                    fetchedCount++;
                                }
                            }

                            if (fetchedCount > 0)
                            {
                                // Reyoncunun ellerinde 2 koli objesi oluştur (Sol El ve Sağ El)
                                CreateCarriedBoxesOnStaff(data, fetchedCount);

                                PlacedFurnitureController targetStorage = WholesaleTruckManager.GetNextAvailableStorageShelfForProduct(data.carriedProduct1);
                                if (targetStorage == null && data.carriedProduct2 != null)
                                {
                                    targetStorage = WholesaleTruckManager.GetNextAvailableStorageShelfForProduct(data.carriedProduct2);
                                }

                                data.targetStorageShelfForDeposit = targetStorage;

                                Vector3 targetStoragePos = (targetStorage != null)
                                    ? targetStorage.GetFrontInteractionPosition(1.2f)
                                    : GetStorageShelfPosition();

                                data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, targetStoragePos);
                                data.currentWaypointIndex = 1;
                            }
                            else
                            {
                                data.isUnloadingTruck = false;
                                ClearCarriedBoxesOnStaff(data);
                            }
                        }
                        else
                        {
                            // B) TAM OLARAK HEDEF DEPO RAFININ ÖNÜNE ULAŞILDI: Kamyondan taşınan kolileri Depo Rafına boşalt!
                            if (data.targetStorageShelfForDeposit != null)
                            {
                                Vector3 faceDir = data.targetStorageShelfForDeposit.transform.forward;
                                faceDir.y = 0f;
                                if (faceDir != Vector3.zero) data.staffObj.transform.rotation = Quaternion.LookRotation(faceDir);
                            }

                            Vector3 popupPos = (data.targetStorageShelfForDeposit != null)
                                ? data.targetStorageShelfForDeposit.GetFrontInteractionPosition(1.2f)
                                : GetStorageShelfPosition();

                            bool deposited = false;
                            if (data.carriedProduct1 != null)
                            {
                                int before = data.carriedProduct1.packQuantity;
                                bool finished = WholesaleTruckManager.DepositPackageToStorageShelf(data.carriedProduct1, data.targetStorageShelfForDeposit, out _, out _);
                                deposited |= data.carriedProduct1.packQuantity < before;
                                if (finished) data.carriedProduct1 = null;
                            }
                            if (data.carriedProduct2 != null)
                            {
                                int before = data.carriedProduct2.packQuantity;
                                bool finished = WholesaleTruckManager.DepositPackageToStorageShelf(data.carriedProduct2, data.targetStorageShelfForDeposit, out _, out _);
                                deposited |= data.carriedProduct2.packQuantity < before;
                                if (finished) data.carriedProduct2 = null;
                            }

                            if (deposited)
                                ShowStockPopup(popupPos, LocalizationManager.L("StaffPopup_UnloadedWarehouse", "+Depoya İndirildi 📦", "+Unloaded to Warehouse 📦"));

                            // Remaining cargo goes to another warehouse shelf only after walking there.
                            var nextStorage = WholesaleTruckManager.GetNextAvailableStorageShelfForProduct(data.carriedProduct1);
                            if (nextStorage == null)
                                nextStorage = WholesaleTruckManager.GetNextAvailableStorageShelfForProduct(data.carriedProduct2);
                            if (nextStorage != null)
                            {
                                data.targetStorageShelfForDeposit = nextStorage;
                                CreateCarriedBoxesOnStaff(data, (data.carriedProduct1 != null ? 1 : 0) + (data.carriedProduct2 != null ? 1 : 0));
                                data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, nextStorage.GetFrontInteractionPosition(1.2f));
                                data.currentWaypointIndex = 1;
                                return;
                            }

                            // Preserve undeliverable quantities if storage filled up during transport.
                            if (data.carriedProduct1 != null) ReturnUndeliveredPackageToTruck(data.carriedProduct1);
                            if (data.carriedProduct2 != null) ReturnUndeliveredPackageToTruck(data.carriedProduct2);
                            data.carriedProduct1 = null;
                            data.carriedProduct2 = null;
                            ClearCarriedBoxesOnStaff(data);
                            data.targetStorageShelfForDeposit = null;

                            // Re-evaluate warehouse capacity before taking another truck task.
                            data.isUnloadingTruck = false;
                            data.isFetchingFromTruck = false;
                        }
                    }
                    else if (data.isFetchingFromStorage)
                    {
                        // C) DEPO RAFINDAN KARTON KOLİ PAKETLERİ ALINDI (İKİ ELİ DE DOLU - SOL EL VE SAĞ EL!)
                        data.isFetchingFromStorage = false;

                        if (data.sourceStorageShelf != null)
                        {
                            Vector3 faceDir = data.sourceStorageShelf.transform.forward;
                            faceDir.y = 0f;
                            if (faceDir != Vector3.zero) data.staffObj.transform.rotation = Quaternion.LookRotation(faceDir);
                        }

                        // 1. SOL EL KOLİSİ ALIMI (Box 1)
                        int totalBoxesFetched = 0;
                        if (data.sourceStorageShelf != null && data.sourceStorageRowId >= 0 && data.sourceStorageRowId < data.sourceStorageShelf.rows.Length)
                        {
                            ShelfRowData sRow1 = data.sourceStorageShelf.rows[data.sourceStorageRowId];
                            if (sRow1 != null && sRow1.currentStock > 0 && data.targetShelf1 != null && data.targetRowId1 >= 0 && data.targetRowId1 < data.targetShelf1.rows.Length
                                && CanRestockFrom(sRow1, data.targetShelf1.rows[data.targetRowId1]))
                            {
                                ShelfRowData tRow1 = data.targetShelf1.rows[data.targetRowId1];
                                // Hiç ürün yoksa 50 Adet (Tam Koli), %60'ın altındaysa %40'ı kadar (20 Adet)
                                int desiredAmount1 = (tRow1.currentStock == 0) ? tRow1.maxCapacity : Mathf.RoundToInt(tRow1.maxCapacity * 0.40f);
                                desiredAmount1 = Mathf.Min(desiredAmount1, tRow1.maxCapacity - tRow1.currentStock);
                                desiredAmount1 = Mathf.Max(1, desiredAmount1);

                                int amountToTake1 = Mathf.Min(sRow1.currentStock, desiredAmount1);
                                data.carriedLots1 = ProductPassportService.RemoveStock(sRow1, amountToTake1);
                                if (sRow1.currentStock <= 0 && data.sourceStorageShelf.FurnitureType == FurnitureType.StorageShelf)
                                {
                                    sRow1.productName = "";
                                    sRow1.productId = "";
                                    sRow1.unitPrice = 0f;
                                }
                                data.carriedAmount1 = amountToTake1;
                                totalBoxesFetched++;
                                data.sourceStorageShelf.UpdateRow3DProductMeshes(data.sourceStorageRowId + 1);
                            }
                        }

                        // 2. SAĞ EL KOLİSİ ALIMI (Box 2)
                        if (data.sourceStorageShelf2 != null && data.sourceStorageShelf2 == data.sourceStorageShelf && data.sourceStorageRowId2 >= 0 && data.sourceStorageRowId2 < data.sourceStorageShelf2.rows.Length)
                        {
                            ShelfRowData sRow2 = data.sourceStorageShelf2.rows[data.sourceStorageRowId2];
                            if (sRow2 != null && sRow2.currentStock > 0 && data.targetShelf2 != null && data.targetRowId2 >= 0 && data.targetRowId2 < data.targetShelf2.rows.Length
                                && CanRestockFrom(sRow2, data.targetShelf2.rows[data.targetRowId2])
                                && (data.targetShelf2 != data.targetShelf1 || data.targetRowId2 != data.targetRowId1
                                    || data.targetShelf2.rows[data.targetRowId2].currentStock + data.carriedAmount1 < data.targetShelf2.rows[data.targetRowId2].maxCapacity))
                            {
                                ShelfRowData tRow2 = data.targetShelf2.rows[data.targetRowId2];
                                int currentFill2 = (data.targetShelf2 == data.targetShelf1 && data.targetRowId2 == data.targetRowId1)
                                    ? tRow2.currentStock + data.carriedAmount1
                                    : tRow2.currentStock;

                                int desiredAmount2 = (currentFill2 == 0) ? tRow2.maxCapacity : Mathf.RoundToInt(tRow2.maxCapacity * 0.40f);
                                desiredAmount2 = Mathf.Min(desiredAmount2, tRow2.maxCapacity - currentFill2);
                                desiredAmount2 = Mathf.Max(1, desiredAmount2);

                                int amountToTake2 = Mathf.Min(sRow2.currentStock, desiredAmount2);
                                data.carriedLots2 = ProductPassportService.RemoveStock(sRow2, amountToTake2);
                                if (sRow2.currentStock <= 0 && data.sourceStorageShelf2.FurnitureType == FurnitureType.StorageShelf)
                                {
                                    sRow2.productName = "";
                                    sRow2.productId = "";
                                    sRow2.unitPrice = 0f;
                                }
                                data.carriedAmount2 = amountToTake2;
                                totalBoxesFetched++;
                                data.sourceStorageShelf2.UpdateRow3DProductMeshes(data.sourceStorageRowId2 + 1);
                            }
                        }
                        
                        // B) Eğer 2. özel atama yapılmadıysa ama depoda stok varsa 2. elimize de koli paketi al!
                        if (data.carriedAmount1 > 0 && data.carriedAmount2 == 0)
                        {
                            var allShelves = PlacedFurnitureController.AllPlacedFurniture;
                            int count = allShelves.Count;
                            for (int i = 0; i < count; i++)
                            {
                                var storage = allShelves[i];
                                if (storage == null || storage.rows == null || storage.FurnitureType != FurnitureType.StorageShelf) continue;
                                if (storage != data.sourceStorageShelf) continue;
                                for (int sr = 0; sr < storage.rows.Length; sr++)
                                {
                                    var sRowExtra = storage.rows[sr];
                                    if (sRowExtra != null && sRowExtra.currentStock > 0 && !IsStorageRowClaimedByOtherRestocker(data, storage, sr))
                                    {
                                        int extraAmount = Mathf.Min(sRowExtra.currentStock, 20);
                                        string extraName = sRowExtra.productName;
                                        string extraId = sRowExtra.productId;
                                        float extraPrice = sRowExtra.unitPrice;

                                        PlacedFurnitureController extraTarget = null;
                                        int extraTargetRow = -1;
                                        foreach (var storeShelf in allShelves)
                                        {
                                            if (storeShelf == null || storeShelf.rows == null || !IsStoreShelf(storeShelf.FurnitureType)) continue;
                                            for (int rId = 0; rId < storeShelf.rows.Length; rId++)
                                            {
                                                var rInfo = storeShelf.rows[rId];
                                                if (rInfo == null || rInfo.IsUnassigned) continue;
                                                if (!CanRestockFrom(sRowExtra, rInfo)) continue;
                                                if (IsShelfRowClaimedByOtherRestocker(data, storeShelf, rId)) continue;
                                                int curFill = (storeShelf == data.targetShelf1 && rId == data.targetRowId1) ? rInfo.currentStock + data.carriedAmount1 : rInfo.currentStock;
                                                if (curFill < rInfo.maxCapacity)
                                                {
                                                    extraTarget = storeShelf;
                                                    extraTargetRow = rId;
                                                    break;
                                                }
                                            }
                                            if (extraTarget != null) break;
                                        }

                                        if (extraTarget == null) continue;
                                        int reserved = extraTarget == data.targetShelf1 && extraTargetRow == data.targetRowId1 ? data.carriedAmount1 : 0;
                                        var extraRow = extraTarget.rows[extraTargetRow];
                                        extraAmount = Mathf.Min(extraAmount, extraRow.maxCapacity - extraRow.currentStock - reserved);
                                        if (extraAmount <= 0) continue;
                                        List<ProductLot> extraLots = ProductPassportService.RemoveStock(sRowExtra, extraAmount);

                                        if (sRowExtra.currentStock <= 0)
                                        {
                                            sRowExtra.productName = "";
                                            sRowExtra.productId = "";
                                            sRowExtra.unitPrice = 0f;
                                        }
                                        data.carriedLots2 = extraLots;
                                        data.carriedAmount2 = extraAmount;
                                        data.sourceStorageShelf2 = storage;
                                        data.sourceStorageRowId2 = sr;
                                        data.targetShelf2 = extraTarget;
                                        data.targetRowId2 = extraTargetRow;

                                        storage.UpdateRow3DProductMeshes(sr + 1);
                                        totalBoxesFetched++;
                                        break;
                                    }
                                }
                                if (totalBoxesFetched >= 2) break;
                            }
                        }

                        if (totalBoxesFetched > 0)
                        {
                            // Reyoncunun İKİ ELİ DE DOLU (Sol El ve Sağ El) 2 Koli Oluştur!
                            int boxMeshCount = totalBoxesFetched;
                            CreateCarriedBoxesOnStaff(data, boxMeshCount);

                            Vector3 targetPos = data.targetShelf1.GetFrontInteractionPosition(1.2f);
                            data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, targetPos);
                            data.currentWaypointIndex = 1;
                            return;
                        }

                        ClearCarriedBoxesOnStaff(data);
                        ResetRestockerTargetFields(data);
                    }
                    else if (data.targetShelf1 != null)
                    {
                        // D) 1. MAĞAZA RAFINA BİZZAT ULAŞILDI: 1. Koliyi Rafına Boşalt!
                        if (data.targetShelf1 != null)
                        {
                            Vector3 faceDir = data.targetShelf1.transform.forward;
                            faceDir.y = 0f;
                            if (faceDir != Vector3.zero) data.staffObj.transform.rotation = Quaternion.LookRotation(faceDir);
                        }

                        if (data.targetRowId1 >= 0 && data.targetRowId1 < data.targetShelf1.rows.Length && data.carriedAmount1 > 0)
                        {
                            ShelfRowData rData1 = data.targetShelf1.rows[data.targetRowId1];
                            if (rData1 != null)
                            {
                                int stockBefore = rData1.currentStock;
                                bool finished = TryPlaceRestockLots(data.targetShelf1, data.targetRowId1, data.carriedLots1);
                                Vector3 popupPos = data.targetShelf1.GetFrontInteractionPosition(1.2f);
                                if (rData1.currentStock > stockBefore)
                                    ShowStockPopup(popupPos, string.Format(LocalizationManager.L("StaffPopup_FirstBoxFmt", "+{0} Stok (1. Koli) 📦", "+{0} Stock (Box 1) 📦"), rData1.currentStock - stockBefore));
                                if (!finished) return;
                            }
                        }

                        data.targetShelf1 = null;
                        data.targetRowId1 = -1;
                        data.carriedAmount1 = 0;
                        data.carriedLots1 = new List<ProductLot>();
                        RemoveOneCarriedBox(data);

                        // EĞER 2. KOLİ VARSA, 2. MAĞAZA RAFINA DOĞRU YÜRÜ:
                        if (data.targetShelf2 != null && data.carriedAmount2 > 0)
                        {
                            Vector3 targetPos2 = data.targetShelf2.GetFrontInteractionPosition(1.2f);
                            data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, targetPos2);
                            data.currentWaypointIndex = 1;
                            return;
                        }
                        else
                        {
                            data.targetShelf2 = null;
                            data.targetRowId2 = -1;
                            data.carriedAmount2 = 0;
                            ClearCarriedBoxesOnStaff(data);
                        }
                    }
                    else if (data.targetShelf2 != null)
                    {
                        // E) 2. MAĞAZA RAFINA BİZZAT ULAŞILDI: 2. Koliyi Rafına Boşalt!
                        if (data.targetShelf2 != null)
                        {
                            Vector3 faceDir = data.targetShelf2.transform.forward;
                            faceDir.y = 0f;
                            if (faceDir != Vector3.zero) data.staffObj.transform.rotation = Quaternion.LookRotation(faceDir);
                        }

                        if (data.targetRowId2 >= 0 && data.targetRowId2 < data.targetShelf2.rows.Length && data.carriedAmount2 > 0)
                        {
                            ShelfRowData rData2 = data.targetShelf2.rows[data.targetRowId2];
                            if (rData2 != null)
                            {
                                int stockBefore = rData2.currentStock;
                                bool finished = TryPlaceRestockLots(data.targetShelf2, data.targetRowId2, data.carriedLots2);
                                Vector3 popupPos = data.targetShelf2.GetFrontInteractionPosition(1.2f);
                                if (rData2.currentStock > stockBefore)
                                    ShowStockPopup(popupPos, string.Format(LocalizationManager.L("StaffPopup_SecondBoxFmt", "+{0} Stok (2. Koli) 📦", "+{0} Stock (Box 2) 📦"), rData2.currentStock - stockBefore));
                                if (!finished) return;
                            }
                        }

                        data.targetShelf2 = null;
                        data.targetRowId2 = -1;
                        data.carriedAmount2 = 0;
                        data.carriedLots2 = new List<ProductLot>();
                        ClearCarriedBoxesOnStaff(data);
                    }
                }
                return;
            }

            // ==================== 3. GÖREV ARAMA & ATAMA ====================
            if (TimeManager.Instance != null)
            {
                int curHour = TimeManager.Instance.Hour;
                int curMin = TimeManager.Instance.Minute;
                bool isShiftActive = IsStaffShiftActive(data.staffMember, curHour, curMin, out bool isEarly);
                bool hasOnlineCourierTask = data.isGatheringForOnlineOrder || data.isCarryingDeliveryBoxToMotorcycle || data.targetOnlineOrder != null;
                if ((!isShiftActive || data.isLeavingShift) && !hasOnlineCourierTask)
                {
                    // Personelin vardiyası bitmiş: Asla yeni koli görevi arama, dinlenme odasına/çıkışa geç!
                    ExecuteBreakRoomRestAndSeating(data, deltaTime);
                    return;
                }
            }

            var shelves = PlacedFurnitureController.AllPlacedFurniture;

            bool isTruckWaitingToUnload = (WholesaleTruckManager.Instance != null &&
                WholesaleTruckManager.Instance.IsTruckAtDockWaitingForUnload &&
                WholesaleTruckManager.Instance.PendingTruckPackages != null &&
                WholesaleTruckManager.Instance.PendingTruckPackages.Count > 0)
                ||
                (GreenTruckDeliveryManager.Instance != null &&
                GreenTruckDeliveryManager.Instance.IsTruckAtDockWaitingForUnload &&
                GreenTruckDeliveryManager.Instance.PendingTruckPackages != null &&
                GreenTruckDeliveryManager.Instance.PendingTruckPackages.Count > 0);

            // GÖREV 1: Kamyondan Depoya Koli Taşıma (DÜKKAN KAPALI OLSA DAHİ EN YÜKSEK ÖNCELİK!)
            if (isTruckWaitingToUnload)
            {
                PlacedFurnitureController storageShelf = null;
                foreach (var f in shelves)
                {
                    if (f != null && f.FurnitureType == FurnitureType.StorageShelf)
                    {
                        storageShelf = f;
                        break;
                    }
                }

                if (storageShelf == null)
                {
                    if (Time.time - lastStorageWarningTime > 12f)
                    {
                        lastStorageWarningTime = Time.time;
                        ModalManager.ShowModal(
                            LocalizationManager.L("Modal_StorageShelfRequired_Title", "Depo Rafı Gerekli! ⚠️", "Storage Shelf Required! ⚠️"),
                            LocalizationManager.L("Modal_StorageShelfRequired_Body", "Toptancı kamyonundaki kolilerin indirilebilmesi için mağazanıza en az 1 adet Depo Rafı (Storage Shelf) kurulmalıdır!\n\nReyoncu çalışan depo rafı kurulana kadar kamyonu boşaltamaz.", "At least one Storage Shelf must be installed before boxes can be unloaded from the truck.\n\nThe stocker cannot unload it until a storage shelf is available."),
                            LocalizationManager.L("Btn_OK", "Tamam", "OK"));
                    }
                    ResetLimbsToRest(data);
                    return;
                }

                var pending = WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckAtDockWaitingForUnload
                    ? WholesaleTruckManager.Instance.PendingTruckPackages
                    : GreenTruckDeliveryManager.Instance.PendingTruckPackages;
                bool canUnload = false;
                if (pending != null)
                {
                    foreach (var package in pending)
                    {
                        if (WholesaleTruckManager.GetNextAvailableStorageShelfForProduct(package) == null) continue;
                        canUnload = true;
                        break;
                    }
                }
                if (canUnload)
                {
                    FreeSofaSeat(data);
                    data.isSitting = false;
                    ResetLimbsToRest(data);

                    data.isUnloadingTruck = true;
                    data.isFetchingFromTruck = true;
                    Vector3 truckDockPos = DeliveryTruckVisuals.UnloadPosition;

                    data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, truckDockPos);
                    data.currentWaypointIndex = 1;
                    return;
                }
            }

            // GÖREV 2: Depo Rafından Mağaza Raflarına Koli Taşıma
            // (Erken çağrılan veya vardiyada olan reyoncu, dükkan kapalı olsa bile sabah hazırlığı için rafları doldurur; sadece gece 24:00 sonrası zorunlu dinlenmeye geçer)
            bool isStoreClosedNight = (TimeManager.Instance != null && TimeManager.Instance.Hour >= 24);
            bool finishingOnlineCourierLoad = data.isGatheringForOnlineOrder || data.isCarryingDeliveryBoxToMotorcycle || data.targetOnlineOrder != null;
            bool orderNeedsStocker = OnlineMarketOrderManager.Instance != null &&
                OnlineMarketOrderManager.Instance.GetNextOrderNeedingStocker() != null;
            if (isStoreClosedNight && !finishingOnlineCourierLoad && !orderNeedsStocker)
            {
                ClearCarriedBoxesOnStaff(data);
                Vector3 restSpotNight = GetBreakRoomTargetPosition(data);

                if (Vector3.Distance(data.staffObj.transform.position, restSpotNight) > 1.8f && (data.waypoints == null || data.currentWaypointIndex >= data.waypoints.Count))
                {
                    data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, restSpotNight);
                    data.currentWaypointIndex = 1;
                    return;
                }

                ExecuteBreakRoomRestAndSeating(data, deltaTime);
                return;
            }

            if (finishingOnlineCourierLoad)
            {
                return;
            }

            // GÖREV 2: Depo Rafından Mağaza Raflarına 2 ADET KOLİ TAŞIMA (Aynı veya Farklı Ürün/Raf)
            // Gece 24:00'da raf doldurma yok; bekleyen kurye siparişi varsa doğrudan yükleme görevine geçilir.
            if (!isStoreClosedNight)
            {
            ResetRestockerTargetFields(data);

            // 1. Koli Arama: Raf bomboşsa (0/50) VEYA stok %60'ın altına düşmüşse (<= 30/50)
            foreach (var s in shelves)
            {
                if (s == null || s.rows == null || !IsStoreShelf(s.FurnitureType)) continue;

                for (int r = 0; r < s.rows.Length; r++)
                {
                    ShelfRowData rData = s.rows[r];
                    if (rData == null || rData.IsUnassigned || string.IsNullOrEmpty(rData.productName)) continue;

                    // EĞER BU RAF VE SIRA BAŞKA BİR REYONCU TARAFINDAN HEDEFLENDİYDSE ATLA! (GÖREV DAĞILIMI)
                    if (IsShelfRowClaimedByOtherRestocker(data, s, r)) continue;

                    if (rData.currentStock == 0 || rData.currentStock <= Mathf.RoundToInt(rData.maxCapacity * 0.60f))
                    {
                        foreach (var storage in shelves)
                        {
                            if (storage == null || storage.rows == null || storage.FurnitureType != FurnitureType.StorageShelf) continue;
                            for (int sr = 0; sr < storage.rows.Length; sr++)
                            {
                                var sRow = storage.rows[sr];
                                bool isMatch1 = CanRestockFrom(sRow, rData);
                                if (isMatch1 && !IsStorageRowClaimedByOtherRestocker(data, storage, sr))
                                {
                                    data.targetShelf1 = s;
                                    data.targetRowId1 = r;
                                    data.sourceStorageShelf = storage;
                                    data.sourceStorageRowId = sr;
                                    break;
                                }
                            }
                            if (data.targetShelf1 != null) break;
                        }
                    }
                    if (data.targetShelf1 != null) break;
                }
                if (data.targetShelf1 != null) break;
            }

            // 2. Koli Arama (İkinci El İçin Farklı veya Aynı Ürün/Raf)
            if (data.targetShelf1 != null)
            {
                foreach (var s in shelves)
                {
                    if (s == null || s.rows == null || !IsStoreShelf(s.FurnitureType)) continue;

                    for (int r = 0; r < s.rows.Length; r++)
                    {
                        ShelfRowData rData = s.rows[r];
                        if (rData == null || rData.IsUnassigned || string.IsNullOrEmpty(rData.productName)) continue;

                        if ((s != data.targetShelf1 || r != data.targetRowId1) && IsShelfRowClaimedByOtherRestocker(data, s, r)) continue;

                        int estimatedStockAfterBox1 = rData.currentStock;
                        if (s == data.targetShelf1 && r == data.targetRowId1)
                        {
                            int expectedBox1Fill = (rData.currentStock == 0) ? rData.maxCapacity : Mathf.RoundToInt(rData.maxCapacity * 0.40f);
                            estimatedStockAfterBox1 += expectedBox1Fill;
                        }

                        if (estimatedStockAfterBox1 < rData.maxCapacity)
                        {
                            foreach (var storage in shelves)
                            {
                                if (storage == null || storage.rows == null || storage.FurnitureType != FurnitureType.StorageShelf) continue;
                                for (int sr = 0; sr < storage.rows.Length; sr++)
                                {
                                    var sRow = storage.rows[sr];
                                    bool isMatch2 = CanRestockFrom(sRow, rData);
                                    if (storage != data.sourceStorageShelf) continue;
                                    int availableStorage = isMatch2 ? sRow.currentStock : 0;
                                    if (storage == data.sourceStorageShelf && sr == data.sourceStorageRowId) availableStorage -= 20;

                                    if (availableStorage > 0 && !IsStorageRowClaimedByOtherRestocker(data, storage, sr))
                                    {
                                        data.targetShelf2 = s;
                                        data.targetRowId2 = r;
                                        data.sourceStorageShelf2 = storage;
                                        data.sourceStorageRowId2 = sr;
                                        break;
                                    }
                                }
                                if (data.targetShelf2 != null) break;
                            }
                        }
                        if (data.targetShelf2 != null) break;
                    }
                    if (data.targetShelf2 != null) break;
                }
            }

            if (data.targetShelf1 != null && data.sourceStorageShelf != null)
            {
                FreeSofaSeat(data);
                data.isSitting = false;
                ResetLimbsToRest(data);

                data.isFetchingFromStorage = true;
                Vector3 storagePos = data.sourceStorageShelf.GetFrontInteractionPosition(1.2f);

                data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, storagePos);
                data.currentWaypointIndex = 1;
                return;
            }
            }

            // ==================== GÖREV 3: ONLINE SİPARİŞ ÜRÜNLERİNİ REYONLARDAN TOPLAYIP MOTORA YÜKLEME ====================
            if (OnlineMarketOrderManager.Instance != null)
            {
                OnlineCustomerOrder onlineOrder = OnlineMarketOrderManager.Instance.GetNextOrderNeedingStocker();
                if (onlineOrder != null && onlineOrder.assignedMotorcycle != null)
                {
                    // Siparişi tek bir reyoncuya rezerve et (2. reyoncu aynı siparişi alamaz ve stokları ziyan etmez!)
                    onlineOrder.isAssignedToStocker = true;

                    FreeSofaSeat(data);
                    data.isSitting = false;
                    ResetLimbsToRest(data);
                    ClearCarriedBoxesOnStaff(data); // Henüz elinde koli YOK!

                    data.isGatheringForOnlineOrder = true;
                    data.isCarryingDeliveryBoxToMotorcycle = false;
                    data.targetOnlineOrder = onlineOrder;
                    data.onlineGatherItems.Clear();
                    data.currentOnlineGatherIndex = 0;

                    // Siparişteki her ürün için dükkan reyonlarını ve depo raflarını tara
                    for (int p = 0; p < onlineOrder.requestedProducts.Count; p++)
                    {
                        var reqProd = onlineOrder.requestedProducts[p];
                        int reqQty = onlineOrder.requestedQuantities[p];
                        int plannedGather = (onlineOrder.gatheredQuantities != null && p < onlineOrder.gatheredQuantities.Count)
                            ? Mathf.Clamp(onlineOrder.gatheredQuantities[p], 0, reqQty)
                            : 0;

                        // 1. Önce Mağaza Raflarından ara, sonra Depo Raflarından ara
                        foreach (var f in shelves)
                        {
                            if (f == null || f.rows == null) continue;
                            for (int r = 0; r < f.rows.Length; r++)
                            {
                                var row = f.rows[r];
                                if (row != null && row.currentStock > 0 && (row.productName == reqProd.name || row.productName == reqProd.nameEn || (!string.IsNullOrEmpty(row.productId) && row.productId == reqProd.id)))
                                {
                                    // Bu sırada bu görevde henüz rezerve edilmiş miktar
                                    int alreadyPlannedInThisRow = 0;
                                    for (int k = 0; k < data.onlineGatherItems.Count; k++)
                                    {
                                        if (data.onlineGatherItems[k].shelf == f && data.onlineGatherItems[k].rowIndex == r)
                                            alreadyPlannedInThisRow += data.onlineGatherItems[k].amountToGather;
                                    }

                                    int availableInRow = row.currentStock - alreadyPlannedInThisRow;
                                    if (availableInRow > 0)
                                    {
                                        int take = Mathf.Min(reqQty - plannedGather, availableInRow);
                                        data.onlineGatherItems.Add(new OnlineOrderGatherItem
                                        {
                                            shelf = f,
                                            rowIndex = r,
                                            productDef = reqProd,
                                            productIndexInOrder = p,
                                            amountToGather = take
                                        });
                                        plannedGather += take;
                                    }
                                }
                                if (plannedGather >= reqQty) break;
                            }
                            if (plannedGather >= reqQty) break;
                        }
                    }

                    // Eğer toplanacak raflar bulunduysa ilk rafa doğru yürüyüşü başlat
                    if (data.onlineGatherItems.Count > 0)
                    {
                        var firstItem = data.onlineGatherItems[0];
                        Vector3 firstShelfPos = firstItem.shelf.GetFrontInteractionPosition(1.2f);
                        data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, firstShelfPos);
                        data.currentWaypointIndex = 1;
                        ShowStockPopup(data.staffObj.transform.position, LocalizationManager.L("StaffPopup_GatheringOrder", "🛍️ Sipariş Raflardan Toplanıyor...", "🛍️ Gathering Order from Shelves..."));
                        return;
                    }
                    else
                    {
                        // Dükkanda hiç stok yoksa doğrudan teslimat kutusunu hazırlayıp motora gitsin
                        CreateCarriedBoxesOnStaff(data, 1);
                        data.isCarryingDeliveryBoxToMotorcycle = true;
                        Vector3 motoPos = (onlineOrder.assignedMotorcycle != null)
                            ? onlineOrder.assignedMotorcycle.transform.position + new Vector3(0.40f, 0f, 0f)
                            : new Vector3(13.0f, 0.05f, 8.0f);
                        data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, motoPos);
                        data.currentWaypointIndex = 1;
                        ShowStockPopup(data.staffObj.transform.position, LocalizationManager.L("StaffPopup_OrderToMotorcycle", "📦 Sipariş Paketi -> Motora Gidiyor", "📦 Order Package -> Heading to Motorcycle"));
                        return;
                    }
                }
            }

            // ==================== 4. GÖREV YOKSA PERSONEL DİNLENME ODASINDA BEKLE & KOLTUĞA OTUR ====================
            ClearCarriedBoxesOnStaff(data);
            Vector3 restSpot = GetBreakRoomTargetPosition(data);

            if (Vector3.Distance(data.staffObj.transform.position, restSpot) > 1.8f && (data.waypoints == null || data.currentWaypointIndex >= data.waypoints.Count))
            {
                data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, restSpot);
                data.currentWaypointIndex = 1;
                return;
            }

            ExecuteBreakRoomRestAndSeating(data, deltaTime);
        }

        private bool TryChooseCleaningTask(StaffTaskData data)
        {
            var manager=StoreCleanlinessManager.Instance;
            if(manager==null || Time.unscaledTime<data.nextCleaningSearch)return false;
            data.nextCleaningSearch=Time.unscaledTime+1f;
            GameObject best=null;Vector3 bestStand=default;List<Vector3> bestPath=null;
            float bestDistance=float.PositiveInfinity;
            var nearbyTrash=new List<GameObject>(manager.TrashItems);
            Vector3 searchOrigin=data.staffObj.transform.position;
            if(data.isSitting && data.assignedSofaSeatIndex>=0 && data.assignedSofaSeatIndex<activeSofaSeats.Count)
                searchOrigin=GetSeatApproach(activeSofaSeats[data.assignedSofaSeatIndex]);
            nearbyTrash.Sort((a,b)=>(a!=null?(a.transform.position-searchOrigin).sqrMagnitude:float.PositiveInfinity)
                .CompareTo(b!=null?(b.transform.position-searchOrigin).sqrMagnitude:float.PositiveInfinity));
            foreach(var trash in nearbyTrash)
            {
                if(trash==null || !trash.activeInHierarchy)continue;
                bool claimed=false;
                foreach(var worker in staffTaskList)
                    if(worker!=null && worker!=data && worker.staffObj!=null && worker.targetTrashObj==trash &&
                       !worker.isLeavingShift && worker.currentState!=StaffAIState.WalkingToLeftExit && worker.currentState!=StaffAIState.Despawned)
                    {claimed=true;break;}
                if(claimed)continue;
                float distance=(trash.transform.position-searchOrigin).sqrMagnitude;
                if(distance>=bestDistance)break; // Sorted: no more full path searches after the nearest reachable stain.
                Vector3 toward=data.staffObj.transform.position-trash.transform.position;toward.y=0;
                if(toward.sqrMagnitude<.01f)toward=Vector3.forward;
                for(int slot=0;slot<8;slot++)
                {
                    Vector3 stand=trash.transform.position+Quaternion.Euler(0,slot*45f,0)*toward.normalized*.65f;
                    stand.y=data.staffObj.transform.position.y;
                    if(PedestrianNavigation.IsBlocked(stand))continue;
                    // The brush must also reach the stain without crossing a cabinet/wall.
                    Vector3 brushEdge=Vector3.Lerp(stand,trash.transform.position,.65f);
                    if(!PedestrianNavigation.Clear(stand,brushEdge))continue;
                    var route=PedestrianNavigation.Route(searchOrigin,stand);
                    if(route==null)continue;
                    best=trash;bestStand=stand;bestPath=route;bestDistance=distance;break;
                }
            }
            if(best==null)return false;
            FreeSofaSeat(data);data.isSitting=false;ResetLimbsToRest(data);
            data.cleaningUnreachableSince=-1;data.targetTrashObj=best;data.cleaningJobActive=true;data.cleaningStand=bestStand;data.taskTimer=0;
            data.waypoints=new List<Vector3>{data.staffObj.transform.position};
            data.waypoints.AddRange(bestPath);data.currentWaypointIndex=1;
            return true;
        }

        private static void ClearCleaningTool(StaffTaskData data)
        {
            if(data.cleaningTool!=null)Destroy(data.cleaningTool);
            data.cleaningTool=null;
        }

        private void AnimateCleaning(StaffTaskData data)
        {
            ResetLimbsToRest(data);
            Vector3 target=data.targetTrashObj.transform.position;
            Vector3 forward=target-data.staffObj.transform.position;forward.y=0;
            if(forward.sqrMagnitude>.001f)data.staffObj.transform.rotation=Quaternion.LookRotation(forward);
            float sweep=Mathf.Sin((3f-data.taskTimer)*7f);
            // Only arms work; feet remain planted next to the stain.
            foreach(var limbs in new[]{data.leftLimbs,data.rightLimbs})
                if(limbs!=null)foreach(var limb in limbs)
                    if(limb!=null && limb.name.ToLowerInvariant().Contains("arm"))
                        limb.localRotation=Quaternion.Euler(-38f+sweep*12f,0f,0f);
            if(data.cleaningTool==null)
            {
                data.cleaningTool=new GameObject("Cleaner_Mop");
                data.cleaningTool.transform.SetParent(data.staffObj.transform,false);
                foreach(string part in new[]{"Handle","Head"})
                {
                    var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=part;
                    obj.transform.SetParent(data.cleaningTool.transform,false);
                    var col=obj.GetComponent<Collider>();if(col!=null){col.enabled=false;Destroy(col);}
                }
            }
            Vector3 head=target+Vector3.up*.04f+data.staffObj.transform.right*sweep*.16f;
            Vector3 hand=data.staffObj.transform.position+Vector3.up*.9f+data.staffObj.transform.forward*.25f;
            Transform handle=data.cleaningTool.transform.Find("Handle");
            handle.position=(head+hand)*.5f;
            handle.rotation=Quaternion.FromToRotation(Vector3.up,hand-head);
            handle.localScale=new Vector3(.025f,Vector3.Distance(head,hand),.025f);
            Transform brush=data.cleaningTool.transform.Find("Head");
            brush.position=head;brush.rotation=data.staffObj.transform.rotation;
            brush.localScale=new Vector3(.32f,.045f,.12f);
        }

        private void ExecuteCleanerTask(StaffTaskData data, float deltaTime)
        {
            bool shopOpen=StoreStatusManager.Instance!=null && StoreStatusManager.Instance.IsOpen;
            if(!shopOpen && data.cleaningJobActive)
            {
                data.targetTrashObj=null;data.cleaningJobActive=false;data.waypoints=null;data.taskTimer=0;
                ClearCleaningTool(data);ResetLimbsToRest(data);
            }
            if(data.cleaningJobActive && data.targetTrashObj==null)
            {
                data.cleaningJobActive=false;data.waypoints=null;data.taskTimer=0;
                ClearCleaningTool(data);ResetLimbsToRest(data);
            }
            if(data.targetTrashObj!=null)
            {
                var cleanerNavigation=data.staffObj.GetComponent<PedestrianNavigator>();
                if(cleanerNavigation!=null && cleanerNavigation.Unreachable)
                {
                    if(data.cleaningUnreachableSince<0)data.cleaningUnreachableSince=Time.unscaledTime;
                    if(Time.unscaledTime-data.cleaningUnreachableSince>2f)
                    {
                        data.targetTrashObj=null;data.cleaningJobActive=false;data.waypoints=null;data.taskTimer=0;
                        data.cleaningUnreachableSince=-1;data.nextCleaningSearch=Time.unscaledTime+.5f;
                        ClearCleaningTool(data);ResetLimbsToRest(data);return;
                    }
                }
                else data.cleaningUnreachableSince=-1;
                if(PedestrianNavigation.IsBlocked(data.cleaningStand))
                {
                    data.targetTrashObj=null;data.waypoints=null;data.taskTimer=0;ClearCleaningTool(data);
                    return;
                }
                if(data.waypoints!=null && data.currentWaypointIndex<data.waypoints.Count)
                {FollowWaypoints(data,deltaTime,()=>data.taskTimer=0f);return;}
                Vector3 delta=data.cleaningStand-data.staffObj.transform.position;delta.y=0;
                if(delta.sqrMagnitude>.04f)
                {
                    data.staffObj.transform.position=PedestrianNavigation.Move(data.staffObj,data.cleaningStand,WALK_SPEED*deltaTime);
                    ResetLimbsToRest(data);return;
                }
                if(data.taskTimer<=0f)data.taskTimer=3f;
                data.taskTimer-=deltaTime;AnimateCleaning(data);
                if(data.taskTimer<=0f)
                {
                    Vector3 position=data.targetTrashObj.transform.position;
                    if(StoreCleanlinessManager.Instance!=null)
                    {
                        StoreCleanlinessManager.Instance.CleanTrashItem(data.targetTrashObj);
                        if(StoreQualityManager.Instance!=null)
                            StoreQualityManager.Instance.AddQualityScore(5,position,LocalizationManager.L("Quality_TrashCleaned","Çöp Temizlendi!","Trash Cleaned!"));
                    }
                    data.targetTrashObj=null;ClearCleaningTool(data);ResetLimbsToRest(data);
                }
                return;
            }
            ClearCleaningTool(data);data.taskTimer=0f;
            // Work requires both the active shift (Update) and an open shop.
            if(shopOpen && !data.isLeavingShift && TryChooseCleaningTask(data))return;
            if(data.waypoints!=null && data.currentWaypointIndex<data.waypoints.Count)
            {FollowWaypoints(data,deltaTime,null);return;}
            Vector3 rest=GetBreakRoomTargetPosition(data);
            if(Vector3.Distance(data.staffObj.transform.position,rest)>1.8f)
            {data.waypoints=BuildStructuredStaffWaypoints(data.staffObj.transform.position,rest);data.currentWaypointIndex=1;return;}
            ExecuteBreakRoomRestAndSeating(data,deltaTime);
        }

        private void ExecuteSecurityPatrolTask(StaffTaskData data, float deltaTime)
        {
            // Kaçan aktif bir hırsız var mı kontrol et!
            if (ShoplifterManager.Instance != null && ShoplifterManager.Instance.HasActiveFleeingThief(out var thiefData))
            {
                if (thiefData != null && thiefData.thiefObj != null)
                {
                    Vector3 thiefPos = thiefData.thiefObj.transform.position;
                    Vector3 guardPos = data.staffObj.transform.position;
                    Vector3 dirToThief = thiefPos - guardPos;

                    if (dirToThief != Vector3.zero)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dirToThief.normalized);
                        data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, targetRot, 480f * deltaTime);
                    }

                    // Hırsızın peşine hızlı koşma (4.4 m/s)
                    Vector3 nextPos = PedestrianNavigation.Move(data.staffObj, thiefPos, 4.4f * deltaTime);
                    data.staffObj.transform.position = nextPos;

                    data.walkCycleTimer += deltaTime * 12.0f;
                    float runLegAngle = Mathf.Sin(data.walkCycleTimer) * 35.0f;
                    AnimateLimbs(data, runLegAngle);

                    // 1.6m yakınına ulaşınca YAKALAMA DENE!
                    if (Vector3.Distance(guardPos, thiefPos) < 1.6f)
                    {
                        ShoplifterManager.Instance.CatchShoplifterBySecurity(thiefData, guardPos);
                    }
                    return;
                }
            }

            // Normal Volta Devriyesi (Turnikeler ile Cam Giriş Kapısı Arasında)
            Vector3 turnstileGatePos = new Vector3(-18.8f, 0.05f, -4.5f);
            Vector3 mainDoorPos = new Vector3(-5.0f, 0.05f, -4.5f);

            Vector3 targetPatrolPos = data.securityPatrolForward ? mainDoorPos : turnstileGatePos;
            Vector3 currentPos = data.staffObj.transform.position;
            Vector3 toTarget = targetPatrolPos - currentPos;

            if (toTarget.magnitude < 0.5f)
            {
                data.securityPatrolForward = !data.securityPatrolForward;
                targetPatrolPos = data.securityPatrolForward ? mainDoorPos : turnstileGatePos;
                toTarget = targetPatrolPos - currentPos;
            }

            Vector3 moveDir = toTarget.normalized;
            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, targetRot, 360f * deltaTime);
            }

            Vector3 nextPosNorm = PedestrianNavigation.Move(data.staffObj, targetPatrolPos, 1.8f * 1.15f * deltaTime);
            data.staffObj.transform.position = nextPosNorm;

            data.walkCycleTimer += deltaTime * 6.5f * 1.15f;
            float legAngle = Mathf.Sin(data.walkCycleTimer) * 22.0f;
            AnimateLimbs(data, legAngle);
        }

        #endregion

        #region Helper Movement & Animations

        private Vector3 CalculateAvoidanceDirection(GameObject staffObj, Vector3 currentPos, Vector3 desiredDir, float stepDist)
        {
            return GameplayPerf.SteerStaff(staffObj, currentPos, desiredDir, stepDist);
        }

        private void FollowWaypoints(StaffTaskData data, float deltaTime, System.Action onComplete)
        {
            if (data.waypoints == null || data.currentWaypointIndex >= data.waypoints.Count || data.staffObj == null)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 currentPos = data.staffObj.transform.position;
            while (data.currentWaypointIndex < data.waypoints.Count - 1 &&
                PedestrianNavigation.IsBlocked(data.waypoints[data.currentWaypointIndex])) data.currentWaypointIndex++;
            Vector3 targetWaypoint = data.waypoints[data.currentWaypointIndex];
            Vector3 toTarget = targetWaypoint - currentPos;
            toTarget.y = 0f;

            // Hedef Noktayı Kaydet
            if (data.waypoints != null && data.waypoints.Count > 0)
            {
                data.finalDestination = data.waypoints[data.waypoints.Count - 1];
            }

            bool isFinalWaypoint = (data.currentWaypointIndex == data.waypoints.Count - 1);
            float arrivalThreshold = isFinalWaypoint ? 0.2f : 0.35f;

            // 1. VARMA KONTROLÜ
            if (toTarget.magnitude < arrivalThreshold && PedestrianNavigation.Clear(currentPos, targetWaypoint))
            {
                data.currentWaypointIndex++;
                data.stuckTimer = 0f;
                data.lastStuckCheckPos = currentPos;

                if (data.currentWaypointIndex >= data.waypoints.Count)
                {
                    data.waypoints = null;
                    onComplete?.Invoke();
                    return;
                }
                targetWaypoint = data.waypoints[data.currentWaypointIndex];
                toTarget = targetWaypoint - currentPos;
                toTarget.y = 0f;
            }

            Vector3 moveDir = toTarget.normalized;

            // Follow a collision-cleared path around placed furniture and walls.
            float stepDist = Mathf.Min(WALK_SPEED * deltaTime, toTarget.magnitude);
            Vector3 navigatedPosition = PedestrianNavigation.Move(data.staffObj, targetWaypoint, stepDist);
            Vector3 avoidanceDir = navigatedPosition - currentPos;

            if (avoidanceDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(avoidanceDir);
                data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, targetRot, 360f * deltaTime);
            }

            Vector3 nextPos = navigatedPosition;
            data.staffObj.transform.position = nextPos;

            float travelled = (nextPos-currentPos).magnitude;
            if (travelled < .0001f) { ResetLimbsToRest(data); return; }
            data.walkCycleTimer += travelled / WALK_SPEED * 8.5f * 1.15f;
            float legAngle = Mathf.Sin(data.walkCycleTimer) * 26.0f;
            AnimateLimbs(data, legAngle);
        }

        private void StartExitRoute(StaffTaskData data)
        {
            ReleaseServiceDeskSeat(data);
            data.serviceDesk=null;
            data.targetTrashObj=null;
            data.cleaningJobActive=false;
            ClearCleaningTool(data);
            FreeSofaSeat(data);
            Vector3 startPos = data.staffObj != null ? data.staffObj.transform.position : new Vector3(25.0f, 0.05f, 32.5f);
            List<Vector3> doorExitRoute = new List<Vector3> { startPos };

            bool isFarmer = (data.staffMember != null && (data.staffMember.role == StaffRole.Çiftçi || data.staffMember.role == StaffRole.DeneyimliÇiftçi || data.staffMember.role == StaffRole.UstaÇiftlikSorumlusu || data.staffMember.role == StaffRole.TarımOtomasyonUzmanı));

            if (isFarmer)
            {
                // ÇİFTÇİLER: Vardiyaları bitince çiftlik evinin kapı önüne gidip orada yok olurlar.
                doorExitRoute.Add(new Vector3(25.0f, 0.05f, 32.5f)); // Çiftlik Evi Kapı Önü Despawn Noktası
            }
            else
            {
                float entranceZ = 6.0f;
                if (EnvironmentBuilder.Instance != null)
                {
                    int level = EnvironmentBuilder.Instance.CurrentUpgradeLevel;
                    if (level == 2) entranceZ = 11.0f;
                    else if (level >= 3) entranceZ = 16.0f;
                }

                // Dükkan personeli soyunma odasında üstünü değiştirdi; kapıları geçip dışarıda yok olsun
                if (startPos.x > 2.5f)
                {
                    doorExitRoute.Add(new Vector3(7.0f, 0.05f, entranceZ)); // Personel Odası Kapısı
                    doorExitRoute.Add(new Vector3(3.0f, 0.05f, 2.0f));       // Depo Kapısı
                }

                doorExitRoute.Add(new Vector3(-5.0f, 0.05f, -0.5f));  // 1. Ana Fuaye (İçeride)
                doorExitRoute.Add(new Vector3(-5.0f, 0.05f, -2.5f));  // 2. Cam Kapı Geçişi
                doorExitRoute.Add(new Vector3(-5.0f, 0.05f, -5.0f));  // 3. Dış Kaldırım
                doorExitRoute.Add(new Vector3(-17.0f, 0.05f, -5.0f)); // 4. Turnike Yaya Geçidi
                doorExitRoute.Add(new Vector3(-45.0f, 0.05f, -5.0f)); // 5. Batı Kaldırım
                doorExitRoute.Add(new Vector3(-85.0f, 0.05f, -5.0f)); // 6. Despawn
            }

            data.waypoints = doorExitRoute;
            data.currentWaypointIndex = (doorExitRoute.Count > 1) ? 1 : 0;
            data.currentState = StaffAIState.WalkingToLeftExit;
        }

        private void AnimateLimbs(StaffTaskData data, float angle)
        {
            ReleaseServiceDeskSeat(data);
            AnimateLimbSide(data.leftLimbs, angle, true, data != null && data.isCarryingBoxes);
            AnimateLimbSide(data.rightLimbs, -angle, false, data != null && data.isCarryingBoxes);
        }

        private static void AnimateLimbSide(List<Transform> limbs, float angle, bool left, bool carrying)
        {
            if (limbs == null) return;
            for (int i = 0; i < limbs.Count; i++)
            {
                Transform t = limbs[i];
                if (t == null) continue;
                if (t.name.StartsWith("Leg_", StringComparison.Ordinal))
                {
                    Transform knee = t.Find("ServiceKnee");
                    if (knee != null) knee.localRotation = Quaternion.identity;
                }
                if (carrying && t.name.StartsWith("Arm", StringComparison.Ordinal))
                {
                    t.localRotation = left ? CarryArmRotL : CarryArmRotR;
                }
                else
                {
                    t.localRotation = Quaternion.Euler(angle, 0f, 0f);
                }
            }
        }

        private void ResetLimbsToRest(StaffTaskData data)
        {
            ReleaseServiceDeskSeat(data);
            if (data != null && data.isCarryingBoxes)
            {
                AnimateLimbSide(data.leftLimbs, 0f, true, false);
                AnimateLimbSide(data.rightLimbs, 0f, false, false);
                ApplyCarryArmPose(data);
                return;
            }

            AnimateLimbSide(data != null ? data.leftLimbs : null, 0f, true, false);
            AnimateLimbSide(data != null ? data.rightLimbs : null, 0f, false, false);
        }

        private static void ReleaseServiceDeskSeat(StaffTaskData data)
        {
            if (data == null || !data.isSeatedAtServiceDesk || data.staffObj == null) return;
            data.serviceDesk = null;
            Vector3 position = data.staffObj.transform.position;
            position = data.serviceApproach;
            position.y = data.serviceDeskFloorY;
            data.staffObj.transform.position = position;
            data.isSeatedAtServiceDesk = false;
        }

        #endregion
    }
}
