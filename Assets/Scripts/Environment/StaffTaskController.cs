using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.UI;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace Farm2Shelf.Environment
{
    public class StaffTaskController : MonoBehaviour
    {
        public static StaffTaskController Instance { get; private set; }

        private const float WALK_SPEED = 2.8f; // m/s
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

            // Güvenlik Özel Volta Devriye Yönü
            public bool securityPatrolForward;

            // Maskot Özel Neşeli Gösteri Zamanlayıcısı
            public float lastMascotCheerTime;

            // Çiftçi Özel Parsel & Çalışma Verisi
            public FieldPlotController targetPlot;
            public bool isFarmerWorkingOnPlot;

            // Personel Dinlenme Odası Mavi Koltuk Oturma Durumu
            public bool isSitting;
            public int assignedSofaSeatIndex = -1;
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
            return data.isCarryingBoxes || data.carriedAmount1 > 0 || data.carriedAmount2 > 0 ||
                   data.carriedProduct1 != null || data.carriedProduct2 != null ||
                   data.isFetchingFromStorage || data.isFetchingFromTruck || data.isUnloadingTruck ||
                   data.targetShelf1 != null || data.targetShelf2 != null || data.targetStorageShelfForDeposit != null ||
                   (data.targetTrashObj != null && data.taskTimer > 0f);
        }

        public static bool IsCashierWorkingAt(PlacedFurnitureController cashier)
        {
            if (Instance == null || Instance.staffTaskList == null || cashier == null) return false;
            Vector3 cashierStaffSpot = cashier.transform.position + cashier.transform.forward * 0.85f;

            for (int i = 0; i < Instance.staffTaskList.Count; i++)
            {
                var task = Instance.staffTaskList[i];
                if (task != null && task.staffObj != null && task.staffMember != null && task.staffMember.role == StaffRole.Kasiyer)
                {
                    if (task.currentState == StaffAIState.WorkingOnTask)
                    {
                        if (Vector3.Distance(task.staffObj.transform.position, cashierStaffSpot) < 1.8f)
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

        public static bool IsStaffShiftActive(StaffMember staff, int currentHour, int currentMinute, out bool isEarlyArrivalWindow)
        {
            isEarlyArrivalWindow = false;
            if (staff == null || !staff.isActive) return false;

            int totalMinsCalc = currentHour * 60 + currentMinute;

            // ⚡ Eğer personel erken göreve çağrıldıysa dükkan kapalı olsa dahi 06:00 - 16:00 arası AKTİFTİR!
            bool isCalledEarly = (StaffVisualManager.Instance != null && StaffVisualManager.Instance.IsStaffCalledEarlyToday(staff.id));
            if (isCalledEarly)
            {
                return (totalMinsCalc >= 360 && totalMinsCalc < 960); // 06:00 - 16:00 (Sabah Hazırlık ve Vardiya Süresi)
            }

            string shift = staff.shiftHours ?? "";

            // 1. SABAH VARDİYASI: 08:00 - 16:00 (30 dk erken geliş / hazırlık: 07:30 - 08:00)
            if (shift.Contains("Sabah") || shift.Contains("Gündüz") || shift.Contains("08:00") || shift.Contains("06:00"))
            {
                if (totalMinsCalc >= 450 && totalMinsCalc < 480) { isEarlyArrivalWindow = true; return true; }
                return (totalMinsCalc >= 480 && totalMinsCalc < 960);
            }
            // 2. AKŞAM VARDİYASI: 16:00 - 24:00 (30 dk erken geliş / hazırlık: 15:30 - 16:00)
            else if (shift.Contains("Akşam") || shift.Contains("16:00") || shift.Contains("14:00") || shift.Contains("Gece") || shift.Contains("22:00"))
            {
                if (totalMinsCalc >= 930 && totalMinsCalc < 960) { isEarlyArrivalWindow = true; return true; }
                if (totalMinsCalc >= 960 && totalMinsCalc < 1440) return true;
                // Gece 24:00 ve sonrası dükkanda müşteri tahliyesi sürüyorsa personeller çalışmaya devam eder:
                int activeCustCount = CustomerShoppingManager.Instance != null ? CustomerShoppingManager.Instance.ActiveCustomerCount : 0;
                if ((totalMinsCalc >= 1440 || currentHour >= 24) && activeCustCount > 0) return true;
                return false;
            }

            return false;
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
        }

        private readonly List<SofaSeatSlot> activeSofaSeats = new List<SofaSeatSlot>();

        private void RefreshSofaSeatsForCurrentLevel()
        {
            int level = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;

            float storeDepth = (level == 1) ? 18.0f : ((level == 2) ? 27.0f : 36.0f);
            float backWallZ = -3.0f + storeDepth;

            float storageDepth = (level == 1) ? 9.5f : ((level == 2) ? 14.5f : 19.5f);
            float startZ = -3.0f + storageDepth;

            int expectedSeats = (level == 1) ? 5 : ((level == 2) ? 7 : 8);
            if (activeSofaSeats.Count == expectedSeats) return;

            // Mevcut oturanları koruyarak listeyi yenile
            List<StaffTaskData> existingOccupants = new List<StaffTaskData>();
            foreach (var s in activeSofaSeats)
            {
                if (s.currentOccupant != null) existingOccupants.Add(s.currentOccupant);
            }

            activeSofaSeats.Clear();

            if (level == 1)
            {
                // LEVEL 1: 3'lü Koltuk (Arka Duvara Yaslı, Yüzü Odaya Dönük)
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 0, seatPosition = new Vector3(6.30f, 0.35f, backWallZ - 0.70f), facingDirection = new Vector3(0f, 0f, -1f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 1, seatPosition = new Vector3(7.00f, 0.35f, backWallZ - 0.70f), facingDirection = new Vector3(0f, 0f, -1f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 2, seatPosition = new Vector3(7.70f, 0.35f, backWallZ - 0.70f), facingDirection = new Vector3(0f, 0f, -1f) });

                // Mola Masası Sandalyeleri (startZ + 2.5f)
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 3, seatPosition = new Vector3(4.10f, 0.35f, startZ + 2.5f), facingDirection = new Vector3(1f, 0f, 0f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 4, seatPosition = new Vector3(5.50f, 0.35f, startZ + 2.5f), facingDirection = new Vector3(-1f, 0f, 0f) });
            }
            else if (level == 2)
            {
                // LEVEL 2: 3'lü Koltuk (backWallZ - 0.70f) & 2'li Koltuk (backWallZ - 4.90f)
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 0, seatPosition = new Vector3(4.30f, 0.35f, backWallZ - 0.70f), facingDirection = new Vector3(0f, 0f, -1f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 1, seatPosition = new Vector3(5.00f, 0.35f, backWallZ - 0.70f), facingDirection = new Vector3(0f, 0f, -1f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 2, seatPosition = new Vector3(5.70f, 0.35f, backWallZ - 0.70f), facingDirection = new Vector3(0f, 0f, -1f) });

                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 3, seatPosition = new Vector3(4.55f, 0.35f, backWallZ - 4.90f), facingDirection = new Vector3(0f, 0f, 1f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 4, seatPosition = new Vector3(5.45f, 0.35f, backWallZ - 4.90f), facingDirection = new Vector3(0f, 0f, 1f) });

                // Masa Sandalyeleri
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 5, seatPosition = new Vector3(4.10f, 0.35f, startZ + 2.5f), facingDirection = new Vector3(1f, 0f, 0f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 6, seatPosition = new Vector3(5.50f, 0.35f, startZ + 2.5f), facingDirection = new Vector3(-1f, 0f, 0f) });
            }
            else
            {
                // LEVEL 3: 3'lü VIP Koltuk + 2'li VIP Koltuk + 1'li VIP Tekli Koltuk
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 0, seatPosition = new Vector3(4.30f, 0.35f, backWallZ - 0.70f), facingDirection = new Vector3(0f, 0f, -1f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 1, seatPosition = new Vector3(5.00f, 0.35f, backWallZ - 0.70f), facingDirection = new Vector3(0f, 0f, -1f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 2, seatPosition = new Vector3(5.70f, 0.35f, backWallZ - 0.70f), facingDirection = new Vector3(0f, 0f, -1f) });

                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 3, seatPosition = new Vector3(4.55f, 0.35f, backWallZ - 4.90f), facingDirection = new Vector3(0f, 0f, 1f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 4, seatPosition = new Vector3(5.45f, 0.35f, backWallZ - 4.90f), facingDirection = new Vector3(0f, 0f, 1f) });

                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 5, seatPosition = new Vector3(3.80f, 0.35f, backWallZ - 2.80f), facingDirection = new Vector3(1f, 0f, 0f) });

                // Masa Sandalyeleri
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 6, seatPosition = new Vector3(4.10f, 0.35f, startZ + 3.8f), facingDirection = new Vector3(1f, 0f, 0f) });
                activeSofaSeats.Add(new SofaSeatSlot { seatIndex = 7, seatPosition = new Vector3(5.50f, 0.35f, startZ + 3.8f), facingDirection = new Vector3(-1f, 0f, 0f) });
            }

            // Oturanları yeniden eşle
            for (int i = 0; i < existingOccupants.Count && i < activeSofaSeats.Count; i++)
            {
                var occ = existingOccupants[i];
                if (occ != null && occ.staffObj != null && staffTaskList.Contains(occ) && !occ.isLeavingShift)
                {
                    activeSofaSeats[i].currentOccupant = occ;
                    occ.assignedSofaSeatIndex = i;
                }
            }
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
                        occ.assignedSofaSeatIndex != seat.seatIndex ||
                        (occ.currentState != StaffAIState.WaitingInBreakRoom &&
                         occ.currentState != StaffAIState.WaitingAtLocker &&
                         !occ.isSitting))
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
                if (seat.currentOccupant == null)
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

        private void FreeSofaSeat(StaffTaskData data)
        {
            if (data == null) return;

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
                    (waitingStaff.currentState == StaffAIState.WaitingInBreakRoom || waitingStaff.assignedSofaSeatIndex < 0))
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

                float distToSeat = Vector3.Distance(data.staffObj.transform.position, targetSitPos);
                if (distToSeat > 0.35f)
                {
                    // Koltuğa doğru yürü
                    data.isSitting = false;
                    data.staffObj.transform.position = Vector3.MoveTowards(data.staffObj.transform.position, targetSitPos, 2.5f * deltaTime);

                    Vector3 moveDir = (targetSitPos - data.staffObj.transform.position).normalized;
                    if (moveDir != Vector3.zero)
                    {
                        data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(moveDir), 360f * deltaTime);
                    }

                    data.walkCycleTimer += deltaTime * 8.0f;
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
                    data.staffObj.transform.position = Vector3.MoveTowards(data.staffObj.transform.position, standPos, 2.5f * deltaTime);
                    Vector3 moveDir = (standPos - data.staffObj.transform.position).normalized;
                    if (moveDir != Vector3.zero)
                    {
                        data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(moveDir), 360f * deltaTime);
                    }
                    data.walkCycleTimer += deltaTime * 8.0f;
                    AnimateLimbs(data, Mathf.Sin(data.walkCycleTimer) * 28.0f);
                }
                else
                {
                    data.staffObj.transform.position = standPos;
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

            // Kollar uyluklara ve kucağa rahatça yaslanır
            Transform lArm = data.leftLimbs[1];
            Transform rArm = data.rightLimbs[1];

            lArm.localRotation = Quaternion.Euler(-25f, 0f, 15f);
            rArm.localRotation = Quaternion.Euler(-25f, 0f, -15f);
        }

        private Vector3 GetBreakRoomStandingPosition(int staffIndex)
        {
            int level = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            float storeDepth = (level == 1) ? 18.0f : ((level == 2) ? 27.0f : 36.0f);
            float backWallZ = -3.0f + storeDepth;

            float storageDepth = (level == 1) ? 9.5f : ((level == 2) ? 14.5f : 19.5f);
            float storageBackZ = -3.0f + storageDepth;
            float staffDepth = backWallZ - storageBackZ;

            float centerZ = storageBackZ + (staffDepth * 0.45f);

            float offsetX = ((staffIndex % 3) - 1) * 0.70f;
            float offsetZ = (staffIndex / 3) * 0.70f;
            return new Vector3(7.0f + offsetX, 0.05f, centerZ + offsetZ);
        }

        private Vector3 GetBreakRoomTargetPosition(StaffTaskData data)
        {
            if (data != null && TryAssignSofaSeat(data) && data.assignedSofaSeatIndex >= 0 && data.assignedSofaSeatIndex < activeSofaSeats.Count)
            {
                return activeSofaSeats[data.assignedSofaSeatIndex].seatPosition;
            }
            int staffIdx = (data != null) ? staffTaskList.IndexOf(data) : 0;
            return GetBreakRoomStandingPosition(staffIdx >= 0 ? staffIdx : 0);
        }

        public void UnregisterStaffAI(string staffId)
        {
            StaffTaskData data = staffTaskList.Find(s => s.staffMember != null && s.staffMember.id == staffId);
            if (data != null)
            {
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
                                     data.carriedProduct1 != null || data.carriedProduct2 != null ||
                                     data.isFetchingFromStorage || data.isFetchingFromTruck || data.isUnloadingTruck ||
                                     (data.targetTrashObj != null && data.taskTimer > 0f);

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
                        data.staffObj.transform.position = Vector3.MoveTowards(data.staffObj.transform.position, lockerStandPos, 3.0f * deltaTime);
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
                            int curMin = (TimeManager.Instance != null) ? TimeManager.Instance.Minute : 0;
                            bool shouldDispatchEarly = (data.staffMember.role == StaffRole.Kasiyer && curMin >= 55);

                            if (!isEarlyArrival || shouldDispatchEarly)
                            {
                                AssignStaffToTaskPosition(data);
                            }
                            else
                            {
                                data.currentState = StaffAIState.WaitingInBreakRoom;
                            }
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
                        Vector3 sideHandoverPos = targetDesk.transform.position + targetDesk.transform.forward * 0.85f + targetDesk.transform.right * 0.65f;
                        data.staffObj.transform.position = Vector3.MoveTowards(data.staffObj.transform.position, sideHandoverPos, 3.0f * deltaTime);
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
            List<Vector3> route = new List<Vector3>();
            route.Add(startPos);

            int level = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            float storageDepth = (level == 1) ? 9.5f : ((level == 2) ? 14.5f : 19.5f);
            float storageBackZ = -3.0f + storageDepth; // Seviye 1: 6.5f, Seviye 2: 11.5f, Seviye 3: 16.5f

            Vector3 staffDoorOutside = new Vector3(7.0f, 0.05f, storageBackZ - 0.75f); // Depo Tarafı
            Vector3 staffDoorInside  = new Vector3(7.0f, 0.05f, storageBackZ + 0.75f); // Personel Odası Tarafı

            Vector3 storageDoorStorageSide = new Vector3(3.6f, 0.05f, 2.0f);
            Vector3 storageDoorStoreSide   = new Vector3(2.2f, 0.05f, 2.0f);

            Vector3 storageAislePos = new Vector3(7.0f, 0.05f, 2.0f);
            Vector3 storeFoyerPos   = new Vector3(-5.0f, 0.05f, 0.5f);
            Vector3 storeOutsidePos = new Vector3(-5.0f, 0.05f, -4.5f);

            bool startInStaffRoom = (startPos.x > 2.8f && startPos.z >= (storageBackZ - 0.1f));
            bool endInStaffRoom   = (endPos.x > 2.8f && endPos.z >= (storageBackZ - 0.1f));

            bool startInStorage   = (startPos.x > 2.8f && startPos.z < (storageBackZ - 0.1f) && startPos.z > -2.8f);
            bool endInStorage     = (endPos.x > 2.8f && endPos.z < (storageBackZ - 0.1f) && endPos.z > -2.8f);

            bool startInStore     = (startPos.x <= 2.8f && startPos.z > -2.8f);
            bool endInStore       = (endPos.x <= 2.8f && endPos.z > -2.8f);

            bool startOutside     = (startPos.z <= -2.8f);
            bool endOutside       = (endPos.z <= -2.8f);

            // 1. PERSONEL ODASI <-> DEPO GEÇİŞİ
            if (startInStaffRoom && endInStorage)
            {
                route.Add(staffDoorInside);
                route.Add(staffDoorOutside);
                route.Add(storageAislePos);
            }
            else if (startInStorage && endInStaffRoom)
            {
                route.Add(storageAislePos);
                route.Add(staffDoorOutside);
                route.Add(staffDoorInside);
            }
            // 2. PERSONEL ODASI <-> DÜKKAN / DIŞARI GEÇİŞİ
            else if (startInStaffRoom && (endInStore || endOutside))
            {
                route.Add(staffDoorInside);
                route.Add(staffDoorOutside);
                route.Add(storageAislePos);
                route.Add(storageDoorStorageSide);
                route.Add(storageDoorStoreSide);
                route.Add(storeFoyerPos);
                if (endOutside)
                {
                    route.Add(storeOutsidePos);
                }
            }
            else if ((startInStore || startOutside) && endInStaffRoom)
            {
                if (startOutside)
                {
                    route.Add(storeOutsidePos);
                }
                route.Add(storeFoyerPos);
                route.Add(storageDoorStoreSide);
                route.Add(storageDoorStorageSide);
                route.Add(storageAislePos);
                route.Add(staffDoorOutside);
                route.Add(staffDoorInside);
            }
            // 3. DEPO <-> DÜKKAN / DIŞARI GEÇİŞİ
            else if (startInStorage && (endInStore || endOutside))
            {
                route.Add(storageDoorStorageSide);
                route.Add(storageDoorStoreSide);
                route.Add(storeFoyerPos);
                if (endOutside)
                {
                    route.Add(storeOutsidePos);
                }
            }
            else if ((startInStore || startOutside) && endInStorage)
            {
                if (startOutside)
                {
                    route.Add(storeOutsidePos);
                }
                route.Add(storeFoyerPos);
                route.Add(storageDoorStoreSide);
                route.Add(storageDoorStorageSide);
                route.Add(storageAislePos);
            }
            // 4. DÜKKAN <-> DIŞARI GEÇİŞİ
            else if (startInStore && endOutside)
            {
                route.Add(new Vector3(-5.0f, 0.05f, -1.0f));
                route.Add(storeOutsidePos);
            }
            else if (startOutside && endInStore)
            {
                route.Add(storeOutsidePos);
                route.Add(new Vector3(-5.0f, 0.05f, -1.0f));
            }

            route.Add(endPos);
            return route;
        }

        public static List<PlacedFurnitureController> GetAllCashierCounters()
        {
            var allFurniture = PlacedFurnitureController.AllPlacedFurniture;
            List<PlacedFurnitureController> cashiers = new List<PlacedFurnitureController>();
            int fCount = allFurniture.Count;
            for (int i = 0; i < fCount; i++)
            {
                var f = allFurniture[i];
                if (f != null && f.FurnitureType == FurnitureType.Cashier)
                {
                    cashiers.Add(f);
                }
            }
            // Kasaları X koordinatına göre (soldan sağa) deterministik sırala
            cashiers.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            return cashiers;
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
                        return myCashier.transform.position + myCashier.transform.forward * 0.85f;
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
                    foreach (var f in furnitureList)
                    {
                        if (f != null && f.FurnitureType == FurnitureType.CustomerServiceDesk)
                        {
                            return f.transform.position + f.transform.forward * 0.45f;
                        }
                    }
                    return new Vector3(-8.0f, 0.05f, 2.0f);

                case StaffRole.Maskot:
                    int mascotIdx = staffTaskList.FindIndex(s => s != null && s.staffMember != null && s.staffMember.role == StaffRole.Maskot);
                    if (mascotIdx < 0) mascotIdx = 0;
                    // Dükkan Giriş Kapısı Önündeki DIŞ KALDIRIM Noktası (Z = -4.5f Dış Yaya Yolu)
                    return new Vector3(1.5f + (mascotIdx * 1.8f), 0.05f, -4.5f);

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

        private void ExecuteMascotDanceTask(StaffTaskData data, float deltaTime)
        {
            if (data == null || data.staffObj == null) return;

            // Kapı önündeki yaya kaldırımında YOLA (Asfalt Ana Yola Doğru 0°) dönük durur
            Quaternion targetRot = Quaternion.Euler(0f, 0f, 0f);
            data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, targetRot, 360f * deltaTime);

            // Prosedürel Maskot Dans Animasyonu:
            // Ayaklar tam olarak kaldırım zeminine (Y = 0.05f) temas eder, tatlı ritmik bir esneme yapar
            float bounceY = Mathf.Abs(Mathf.Sin(Time.time * 5.0f)) * 0.025f;
            Vector3 curPos = data.staffObj.transform.position;
            curPos.y = 0.05f + bounceY;
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
                string mascotText = isFemale ? "✨ Sevimli Tavşan Dans Ediyor! 🐰 ✨" : "✨ Neşeli Ayı Dans Ediyor! 🐻 ✨";
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

            if (dist < 0.65f)
            {
                // Tarlanın başına ulaşıldı: 5 ile 10 saniye arası sürecek çalışma/sulama/kontrol sürecini başlat
                data.isFarmerWorkingOnPlot = true;
                data.taskTimer = Random.Range(5.5f, 9.5f); // 5 - 10 saniye sürsün!
                if (data.waypoints != null) data.waypoints.Clear();
                data.currentWaypointIndex = 0;
                return;
            }

            Vector3 moveDir = toTarget.normalized;
            float stepDist = 2.4f * deltaTime;
            Vector3 avoidanceDir = CalculateAvoidanceDirection(data.staffObj, currentPos, moveDir, stepDist);

            if (avoidanceDir != Vector3.zero)
            {
                data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, Quaternion.LookRotation(avoidanceDir), 360f * deltaTime);
            }
            data.staffObj.transform.position = Vector3.MoveTowards(currentPos, currentPos + avoidanceDir, stepDist);

            data.walkCycleTimer += deltaTime * 8.5f;
            AnimateLimbs(data, Mathf.Sin(data.walkCycleTimer) * 26.0f);
        }

        private void ExecuteCustomerServiceDeskTask(StaffTaskData data, float deltaTime)
        {
            ResetLimbsToRest(data);

            var furnitureList = PlacedFurnitureController.AllPlacedFurniture;
            int count = furnitureList.Count;
            for (int i = 0; i < count; i++)
            {
                var f = furnitureList[i];
                if (f != null && f.FurnitureType == FurnitureType.CustomerServiceDesk)
                {
                    data.staffObj.transform.position = f.transform.position + f.transform.forward * 0.45f;
                    data.staffObj.transform.rotation = Quaternion.LookRotation(-f.transform.forward);
                    break;
                }
            }
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
                Vector3 myCashierPos = myCashier.transform.position + myCashier.transform.forward * 0.85f;
                Quaternion myCashierRot = Quaternion.LookRotation(-myCashier.transform.forward);

                data.staffObj.transform.position = myCashierPos;
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

        private void CreateCarriedBoxesOnStaff(StaffTaskData data, int count)
        {
            ClearCarriedBoxesOnStaff(data);
            if (data.staffObj == null || count <= 0) return;

            data.carriedBoxesRoot = new GameObject("CarriedBoxes");
            data.carriedBoxesRoot.transform.SetParent(data.staffObj.transform, false);
            data.carriedBoxesRoot.transform.localPosition = new Vector3(0f, 0.75f, 0.40f);
            data.carriedBoxesRoot.transform.localRotation = Quaternion.identity;

            Material boxMat = FurnitureModelBuilder.CardboardBoxMaterial;

            if (count == 1)
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Box_1";
                box.transform.SetParent(data.carriedBoxesRoot.transform, false);
                box.transform.localPosition = new Vector3(0f, 0f, 0f);
                box.transform.localScale = new Vector3(0.38f, 0.24f, 0.36f);
                if (boxMat != null) box.GetComponent<Renderer>().sharedMaterial = boxMat;
                Collider c = box.GetComponent<Collider>();
                if (c != null) Destroy(c);
            }
            else if (count >= 2)
            {
                // 2 Koli: Sol Elde ve Sağ Elde Yanyana 2 Adet Koli Taşıma!
                GameObject boxL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boxL.name = "Box_Left";
                boxL.transform.SetParent(data.carriedBoxesRoot.transform, false);
                boxL.transform.localPosition = new Vector3(-0.24f, 0f, 0f);
                boxL.transform.localScale = new Vector3(0.34f, 0.24f, 0.34f);
                if (boxMat != null) boxL.GetComponent<Renderer>().sharedMaterial = boxMat;
                Collider cL = boxL.GetComponent<Collider>();
                if (cL != null) Destroy(cL);

                GameObject boxR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boxR.name = "Box_Right";
                boxR.transform.SetParent(data.carriedBoxesRoot.transform, false);
                boxR.transform.localPosition = new Vector3(0.24f, 0f, 0f);
                boxR.transform.localScale = new Vector3(0.34f, 0.24f, 0.34f);
                if (boxMat != null) boxR.GetComponent<Renderer>().sharedMaterial = boxMat;
                Collider cR = boxR.GetComponent<Collider>();
                if (cR != null) Destroy(cR);
            }

            data.isCarryingBoxes = true;
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

            data.targetShelf2 = null;
            data.targetRowId2 = -1;
            data.carriedAmount2 = 0;

            data.sourceStorageShelf = null;
            data.sourceStorageRowId = -1;
            data.sourceStorageShelf2 = null;
            data.sourceStorageRowId2 = -1;
        }

        private static bool IsStoreShelf(FurnitureType t)
        {
            return t == FurnitureType.Shelf || t == FurnitureType.Fridge || t == FurnitureType.Freezer ||
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

        private void ShowStockPopup(Vector3 pos, string text = "+Stok Tamamlandı 📦")
        {
            GameObject popupObj = new GameObject("Popup_StockRefill");
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

            // 2. Noktada İnceleme / Bekleme & Koli İşlemi Aşaması
            if (data.taskTimer > 0f)
            {
                data.taskTimer -= deltaTime;
                ResetLimbsToRest(data);

                if (data.taskTimer <= 0f)
                {
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

                            if (data.carriedProduct1 != null)
                            {
                                WholesaleTruckManager.DepositPackageToStorageShelf(data.carriedProduct1, out _, out _);
                                data.carriedProduct1 = null;
                            }
                            if (data.carriedProduct2 != null)
                            {
                                WholesaleTruckManager.DepositPackageToStorageShelf(data.carriedProduct2, out _, out _);
                                data.carriedProduct2 = null;
                            }

                            ShowStockPopup(popupPos, "+Depoya İndirildi 📦");
                            ClearCarriedBoxesOnStaff(data);
                            data.targetStorageShelfForDeposit = null;

                            bool stillHasPackages = (WholesaleTruckManager.Instance != null &&
                                WholesaleTruckManager.Instance.IsTruckAtDockWaitingForUnload &&
                                WholesaleTruckManager.Instance.PendingTruckPackages != null &&
                                WholesaleTruckManager.Instance.PendingTruckPackages.Count > 0)
                                ||
                                (GreenTruckDeliveryManager.Instance != null &&
                                GreenTruckDeliveryManager.Instance.IsTruckAtDockWaitingForUnload &&
                                GreenTruckDeliveryManager.Instance.PendingTruckPackages != null &&
                                GreenTruckDeliveryManager.Instance.PendingTruckPackages.Count > 0);

                            if (stillHasPackages)
                            {
                                data.isFetchingFromTruck = true;
                                Vector3 truckDockPos = new Vector3(13.0f, 0.05f, 2.0f);
                                data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, truckDockPos);
                                data.currentWaypointIndex = 1;
                            }
                            else
                            {
                                data.isUnloadingTruck = false;
                            }
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

                        int totalBoxesFetched = 0;

                        // 1. SOL EL KOLİSİ ALIMI (Box 1)
                        if (data.sourceStorageShelf != null && data.sourceStorageRowId >= 0 && data.sourceStorageRowId < data.sourceStorageShelf.rows.Length)
                        {
                            ShelfRowData sRow1 = data.sourceStorageShelf.rows[data.sourceStorageRowId];
                            if (sRow1 != null && sRow1.currentStock > 0 && data.targetShelf1 != null && data.targetRowId1 >= 0 && data.targetRowId1 < data.targetShelf1.rows.Length)
                            {
                                ShelfRowData tRow1 = data.targetShelf1.rows[data.targetRowId1];
                                // Hiç ürün yoksa 50 Adet (Tam Koli), %60'ın altındaysa %40'ı kadar (20 Adet)
                                int desiredAmount1 = (tRow1.currentStock == 0) ? tRow1.maxCapacity : Mathf.RoundToInt(tRow1.maxCapacity * 0.40f);
                                desiredAmount1 = Mathf.Min(desiredAmount1, tRow1.maxCapacity - tRow1.currentStock);
                                desiredAmount1 = Mathf.Max(1, desiredAmount1);

                                int amountToTake1 = Mathf.Min(sRow1.currentStock, desiredAmount1);
                                sRow1.currentStock = Mathf.Max(0, sRow1.currentStock - amountToTake1);
                                if (sRow1.currentStock <= 0 && data.sourceStorageShelf.FurnitureType == FurnitureType.StorageShelf)
                                {
                                    sRow1.productName = "";
                                    sRow1.productId = "";
                                    sRow1.unitPrice = 0f;
                                }
                                data.carriedAmount1 = amountToTake1;
                                data.sourceStorageShelf.UpdateRow3DProductMeshes(data.sourceStorageRowId + 1);
                                totalBoxesFetched++;
                            }
                        }

                        // 2. SAĞ EL KOLİSİ ALIMI (Box 2)
                        if (data.sourceStorageShelf2 != null && data.sourceStorageRowId2 >= 0 && data.sourceStorageRowId2 < data.sourceStorageShelf2.rows.Length)
                        {
                            ShelfRowData sRow2 = data.sourceStorageShelf2.rows[data.sourceStorageRowId2];
                            if (sRow2 != null && sRow2.currentStock > 0 && data.targetShelf2 != null && data.targetRowId2 >= 0 && data.targetRowId2 < data.targetShelf2.rows.Length)
                            {
                                ShelfRowData tRow2 = data.targetShelf2.rows[data.targetRowId2];
                                int currentFill2 = (data.targetShelf2 == data.targetShelf1 && data.targetRowId2 == data.targetRowId1)
                                    ? tRow2.currentStock + data.carriedAmount1
                                    : tRow2.currentStock;

                                int desiredAmount2 = (currentFill2 == 0) ? tRow2.maxCapacity : Mathf.RoundToInt(tRow2.maxCapacity * 0.40f);
                                desiredAmount2 = Mathf.Min(desiredAmount2, tRow2.maxCapacity - currentFill2);
                                desiredAmount2 = Mathf.Max(1, desiredAmount2);

                                int amountToTake2 = Mathf.Min(sRow2.currentStock, desiredAmount2);
                                sRow2.currentStock = Mathf.Max(0, sRow2.currentStock - amountToTake2);
                                if (sRow2.currentStock <= 0 && data.sourceStorageShelf2.FurnitureType == FurnitureType.StorageShelf)
                                {
                                    sRow2.productName = "";
                                    sRow2.productId = "";
                                    sRow2.unitPrice = 0f;
                                }
                                data.carriedAmount2 = amountToTake2;
                                data.sourceStorageShelf2.UpdateRow3DProductMeshes(data.sourceStorageRowId2 + 1);
                                totalBoxesFetched++;
                            }
                        }
                        
                        // B) Eğer 2. özel atama yapılmadıysa ama depoda stok varsa 2. elimize de koli paketi al!
                        if (totalBoxesFetched < 2)
                        {
                            var allShelves = PlacedFurnitureController.AllPlacedFurniture;
                            int count = allShelves.Count;
                            for (int i = 0; i < count; i++)
                            {
                                var storage = allShelves[i];
                                if (storage == null || storage.rows == null || storage.FurnitureType != FurnitureType.StorageShelf) continue;
                                for (int sr = 0; sr < storage.rows.Length; sr++)
                                {
                                    var sRowExtra = storage.rows[sr];
                                    if (sRowExtra != null && sRowExtra.currentStock > 0)
                                    {
                                        int extraAmount = Mathf.Min(sRowExtra.currentStock, 20);
                                        sRowExtra.currentStock = Mathf.Max(0, sRowExtra.currentStock - extraAmount);
                                        if (sRowExtra.currentStock <= 0)
                                        {
                                            sRowExtra.productName = "";
                                            sRowExtra.productId = "";
                                            sRowExtra.unitPrice = 0f;
                                        }
                                        data.carriedAmount2 = extraAmount;
                                        data.sourceStorageShelf2 = storage;
                                        data.sourceStorageRowId2 = sr;

                                        // 2. Koli için mağaza raflarında eksik stoklu uygun sıra bul:
                                        if (data.targetShelf2 == null)
                                        {
                                            foreach (var storeShelf in allShelves)
                                            {
                                                if (storeShelf != null && storeShelf.rows != null && IsStoreShelf(storeShelf.FurnitureType))
                                                {
                                                    for (int rId = 0; rId < storeShelf.rows.Length; rId++)
                                                    {
                                                        var rInfo = storeShelf.rows[rId];
                                                        bool isMatchingProduct = rInfo != null && !rInfo.IsUnassigned && (rInfo.productName == sRowExtra.productName || (!string.IsNullOrEmpty(rInfo.productId) && rInfo.productId == sRowExtra.productId));
                                                        if (isMatchingProduct)
                                                        {
                                                            int curFill = (storeShelf == data.targetShelf1 && rId == data.targetRowId1) ? rInfo.currentStock + data.carriedAmount1 : rInfo.currentStock;
                                                            if (curFill < rInfo.maxCapacity)
                                                            {
                                                                data.targetShelf2 = storeShelf;
                                                                data.targetRowId2 = rId;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                if (data.targetShelf2 != null) break;
                                            }

                                            if (data.targetShelf2 == null)
                                            {
                                                data.targetShelf2 = data.targetShelf1;
                                                data.targetRowId2 = data.targetRowId1;
                                            }
                                        }

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
                            int boxMeshCount = Mathf.Max(2, totalBoxesFetched);
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
                                rData1.currentStock = Mathf.Min(rData1.maxCapacity, rData1.currentStock + data.carriedAmount1);
                                data.targetShelf1.UpdateRow3DProductMeshes(data.targetRowId1);
                                Vector3 popupPos = data.targetShelf1.GetFrontInteractionPosition(1.2f);
                                ShowStockPopup(popupPos, $"+{data.carriedAmount1} Stok (1. Koli) 📦");
                            }
                        }

                        data.targetShelf1 = null;
                        data.targetRowId1 = -1;
                        data.carriedAmount1 = 0;
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
                                rData2.currentStock = Mathf.Min(rData2.maxCapacity, rData2.currentStock + data.carriedAmount2);
                                data.targetShelf2.UpdateRow3DProductMeshes(data.targetRowId2);
                                Vector3 popupPos = data.targetShelf2.GetFrontInteractionPosition(1.2f);
                                ShowStockPopup(popupPos, $"+{data.carriedAmount2} Stok (2. Koli) 📦");
                            }
                        }

                        data.targetShelf2 = null;
                        data.targetRowId2 = -1;
                        data.carriedAmount2 = 0;
                        ClearCarriedBoxesOnStaff(data);
                    }
                }
                return;
            }

            // ==================== 3. GÖREV ARAMA & ATAMA ====================
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
                        ModalManager.ShowModal("Depo Rafı Gerekli! ⚠️", "Toptancı kamyonundaki kolilerin indirilebilmesi için mağazanıza en az 1 adet Depo Rafı (Storage Shelf) kurulmalıdır!\n\nReyoncu çalışan depo rafı kurulana kadar kamyonu boşaltamaz.", "Tamam");
                    }
                    ResetLimbsToRest(data);
                    return;
                }

                FreeSofaSeat(data);
                data.isSitting = false;
                ResetLimbsToRest(data);

                data.isUnloadingTruck = true;
                data.isFetchingFromTruck = true;
                Vector3 truckDockPos = new Vector3(13.0f, 0.05f, 2.0f);

                data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, truckDockPos);
                data.currentWaypointIndex = 1;
                return;
            }

            // GÖREV 2: Depo Rafından Mağaza Raflarına Koli Taşıma
            // (Erken çağrılan veya vardiyada olan reyoncu, dükkan kapalı olsa bile sabah hazırlığı için rafları doldurur; sadece gece 24:00 sonrası zorunlu dinlenmeye geçer)
            bool isStoreClosedNight = (TimeManager.Instance != null && TimeManager.Instance.Hour >= 24);
            if (isStoreClosedNight)
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

            // GÖREV 2: Depo Rafından Mağaza Raflarına 2 ADET KOLİ TAŞIMA (Aynı veya Farklı Ürün/Raf)
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
                                bool isMatch1 = sRow != null && sRow.currentStock > 0 && (sRow.productName == rData.productName || (!string.IsNullOrEmpty(sRow.productId) && sRow.productId == rData.productId));
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
                                    bool isMatch2 = sRow != null && (sRow.productName == rData.productName || (!string.IsNullOrEmpty(sRow.productId) && sRow.productId == rData.productId));
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

            // ==================== 3. DOLDURULACAK RAF YOKSA PERSONEL DİNLENME ODASINDA BEKLE & KOLTUĞA OTUR ====================
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

        private void ExecuteCleanerTask(StaffTaskData data, float deltaTime)
        {
            if (data.waypoints != null && data.currentWaypointIndex < data.waypoints.Count)
            {
                FollowWaypoints(data, deltaTime, onComplete: () => {
                    data.taskTimer = 2.0f;
                });
                return;
            }

            if (data.taskTimer > 0f)
            {
                data.taskTimer -= deltaTime;
                float mopAngle = Mathf.Sin(Time.time * 12.0f) * 35.0f;
                AnimateLimbs(data, mopAngle);

                if (data.taskTimer <= 0f)
                {
                    if (data.targetTrashObj != null && StoreCleanlinessManager.Instance != null)
                    {
                        Vector3 trashPos = data.targetTrashObj.transform.position;
                        StoreCleanlinessManager.Instance.CleanTrashItem(data.targetTrashObj);

                        if (StoreQualityManager.Instance != null)
                        {
                            StoreQualityManager.Instance.AddQualityScore(5, trashPos, "Çöp Temizlendi!");
                        }
                    }
                    data.targetTrashObj = null;
                }
                return;
            }

            bool isStoreClosedNightCleaner = (StoreStatusManager.Instance != null && !StoreStatusManager.Instance.IsOpen) || (TimeManager.Instance != null && TimeManager.Instance.Hour >= 24);
            if (isStoreClosedNightCleaner)
            {
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

            if (StoreCleanlinessManager.Instance != null)
            {
                GameObject nearestTrash = StoreCleanlinessManager.Instance.GetNearestTrashItem(data.staffObj.transform.position, out float dist);
                if (nearestTrash != null && dist < 35.0f)
                {
                    FreeSofaSeat(data);
                    data.isSitting = false;
                    ResetLimbsToRest(data);
                    data.targetTrashObj = nearestTrash;
                    data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, nearestTrash.transform.position);
                    data.currentWaypointIndex = 1;
                    return;
                }
            }

            // Temizlenecek çöp yoksa Personel Dinlenme Odasındaki Mavi Koltuklara Geç veya Ayakta Bekle!
            Vector3 restSpot = GetBreakRoomTargetPosition(data);

            if (Vector3.Distance(data.staffObj.transform.position, restSpot) > 1.8f && (data.waypoints == null || data.currentWaypointIndex >= data.waypoints.Count))
            {
                data.waypoints = BuildStructuredStaffWaypoints(data.staffObj.transform.position, restSpot);
                data.currentWaypointIndex = 1;
                return;
            }

            ExecuteBreakRoomRestAndSeating(data, deltaTime);
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
                    Vector3 nextPos = Vector3.MoveTowards(guardPos, thiefPos, 4.4f * deltaTime);
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

            Vector3 nextPosNorm = Vector3.MoveTowards(currentPos, targetPatrolPos, 1.8f * deltaTime);
            data.staffObj.transform.position = nextPosNorm;

            data.walkCycleTimer += deltaTime * 6.5f;
            float legAngle = Mathf.Sin(data.walkCycleTimer) * 22.0f;
            AnimateLimbs(data, legAngle);
        }

        #endregion

        #region Helper Movement & Animations

        private static bool IsSolidObstacle(Collider col, GameObject selfObj)
        {
            if (col == null || col.isTrigger) return false;
            if (selfObj != null && (col.gameObject == selfObj || col.transform.IsChildOf(selfObj.transform))) return false;

            // SADECE VE SADECE DUVARLAR (WALLS / BUILDINGS / DIVIDERS) KATI ENGELDİR!
            // Dükkan ve bina duvarları haricindeki mobilyalar, raflar, buzdolapları, stantlar, kasalar, tezgahlar, koliler, paletler, müşteriler ve personeller engellere takılmaz.
            string n = col.name.ToLower();
            if (n.Contains("wall") || n.Contains("duvar") || n.Contains("building") ||
                n.Contains("fence") || n.Contains("facade") || n.Contains("partition") ||
                n.Contains("divider") || n.Contains("boundary") || n.Contains("border") ||
                n.Contains("outerwall") || n.Contains("storewall") || n.Contains("storagewall") ||
                n.Contains("roomwall") || n.Contains("barrier") || n.Contains("turnstile") ||
                n.Contains("housing"))
            {
                return true;
            }

            // DÜKKAN İÇİNDEKİ HER ŞEYİN (Raf, Dolap, Tezgah, Kasa, Koli, Palet, Müşteri, Personel vb.) İÇİNDEN GEÇİLİR!
            return false;
        }

        private Vector3 CalculateAvoidanceDirection(GameObject staffObj, Vector3 currentPos, Vector3 desiredDir, float stepDist)
        {
            if (desiredDir == Vector3.zero || staffObj == null) return desiredDir;

            float checkRadius = 0.25f; // Gövde yarıçapı
            float checkDistance = Mathf.Max(0.60f, stepDist + 0.25f);
            Vector3 rayStart = currentPos + Vector3.up * 0.5f;

            RaycastHit[] hits = Physics.SphereCastAll(rayStart, checkRadius, desiredDir, checkDistance);
            bool hitObstacle = false;

            if (hits != null && hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (IsSolidObstacle(hit.collider, staffObj))
                    {
                        hitObstacle = true;
                        break;
                    }
                }
            }

            if (!hitObstacle) return desiredDir;

            // DUVARA YAKINSA DUVAR YÜZEYİ BOYUNCA AÇISAL ARAMA
            float[] checkAngles = new float[] { 20f, -20f, 40f, -40f, 60f, -60f, 80f, -80f, 100f, -100f, 120f, -120f, 140f, -140f };
            foreach (float angle in checkAngles)
            {
                Vector3 testDir = Quaternion.Euler(0f, angle, 0f) * desiredDir;
                RaycastHit[] testHits = Physics.SphereCastAll(rayStart, checkRadius, testDir, checkDistance * 0.80f);
                bool testHitObstacle = false;

                if (testHits != null && testHits.Length > 0)
                {
                    foreach (var th in testHits)
                    {
                        if (IsSolidObstacle(th.collider, staffObj))
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

            return desiredDir;
        }

        private void FollowWaypoints(StaffTaskData data, float deltaTime, System.Action onComplete)
        {
            if (data.waypoints == null || data.currentWaypointIndex >= data.waypoints.Count || data.staffObj == null)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 currentPos = data.staffObj.transform.position;
            Vector3 targetWaypoint = data.waypoints[data.currentWaypointIndex];
            Vector3 toTarget = targetWaypoint - currentPos;

            // Hedef Noktayı Kaydet
            if (data.waypoints != null && data.waypoints.Count > 0)
            {
                data.finalDestination = data.waypoints[data.waypoints.Count - 1];
            }

            bool isFinalWaypoint = (data.currentWaypointIndex == data.waypoints.Count - 1);
            float arrivalThreshold = isFinalWaypoint ? 0.95f : 0.85f;

            // 1. VARMA KONTROLÜ
            if (toTarget.magnitude < arrivalThreshold)
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
            }

            // 2. YALNIZCA GERÇEKTEN DURAĞAN HALE GELDİĞİNDE (0.75 SANİYE TAKILDIĞINDA) ROTA YENİLE
            if (Time.time - data.lastStuckCheckTime > 0.75f)
            {
                float movedDist = Vector3.Distance(currentPos, data.lastStuckCheckPos);
                data.lastStuckCheckPos = currentPos;
                data.lastStuckCheckTime = Time.time;

                if (movedDist < 0.12f)
                {
                    data.stuckTimer += 0.75f;
                    if (data.stuckTimer >= 0.75f && data.finalDestination != Vector3.zero)
                    {
                        List<Vector3> freshRoute = BuildStructuredStaffWaypoints(currentPos, data.finalDestination);
                        if (freshRoute != null && freshRoute.Count > 1)
                        {
                            data.waypoints = freshRoute;
                            data.currentWaypointIndex = 1;
                            targetWaypoint = data.waypoints[data.currentWaypointIndex];
                            toTarget = targetWaypoint - currentPos;
                        }
                        data.stuckTimer = 0f;
                    }
                }
                else
                {
                    data.stuckTimer = 0f;
                }
            }

            Vector3 moveDir = toTarget.normalized;

            // 3. İLERLEME VE KAÇINMA YÖNÜ HESAPLAMA (14 Açılı Dairesel Kaçınma & Duvar Kayması)
            float stepDist = WALK_SPEED * deltaTime;
            Vector3 avoidanceDir = CalculateAvoidanceDirection(data.staffObj, currentPos, moveDir, stepDist);

            if (avoidanceDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(avoidanceDir);
                data.staffObj.transform.rotation = Quaternion.RotateTowards(data.staffObj.transform.rotation, targetRot, 360f * deltaTime);
            }

            Vector3 nextPos = Vector3.MoveTowards(currentPos, currentPos + avoidanceDir, stepDist);
            data.staffObj.transform.position = nextPos;

            data.walkCycleTimer += deltaTime * 8.5f;
            float legAngle = Mathf.Sin(data.walkCycleTimer) * 26.0f;
            AnimateLimbs(data, legAngle);
        }

        private void StartExitRoute(StaffTaskData data)
        {
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
            if (data.leftLimbs != null)
            {
                foreach (var l in data.leftLimbs)
                {
                    if (l != null) l.localRotation = Quaternion.Euler(angle, 0f, 0f);
                }
            }
            if (data.rightLimbs != null)
            {
                foreach (var r in data.rightLimbs)
                {
                    if (r != null) r.localRotation = Quaternion.Euler(-angle, 0f, 0f);
                }
            }
        }

        private void ResetLimbsToRest(StaffTaskData data)
        {
            if (data.leftLimbs != null)
            {
                foreach (var l in data.leftLimbs)
                {
                    if (l != null) l.localRotation = Quaternion.identity;
                }
            }
            if (data.rightLimbs != null)
            {
                foreach (var r in data.rightLimbs)
                {
                    if (r != null) r.localRotation = Quaternion.identity;
                }
            }
        }

        #endregion
    }
}
