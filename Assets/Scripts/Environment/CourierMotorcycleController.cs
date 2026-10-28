using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public enum MotorcycleState
    {
        ParkedInBay,
        WaitingForStocker,
        Departing,
        EnRouteDelivery,
        DeliveringAtDoorstep,
        ReturningToStore
    }

    /// <summary>
    /// Satın alınan kurye motorsikletinin durumunu, sürücüsünü, gece farlarını,
    /// tekerlek dönüşlerini ve online teslimat adreslerine sürüş rotasını yönetir.
    /// Hedef adrese göre teslimat kamyonunun girdiği güney yoldan (Z: -7.5) veya
    /// yukarı kuzey yoldan (Z: 48.5) araba gibi şeritli giriş/çıkış yapar.
    /// </summary>
    public class CourierMotorcycleController : MonoBehaviour
    {
        public int SlotIndex { get; private set; }
        public Vector3 HomeParkPosition { get; private set; }
        public Quaternion HomeParkRotation { get; private set; }
        public MotorcycleState CurrentState { get; set; } = MotorcycleState.ParkedInBay;

        public StaffMember AssignedCourier { get; private set; }
        public GameObject CourierRiderObj { get; private set; }
        public Transform DriverSeatMount { get; private set; }

        public List<OnlineCustomerOrder> LoadedOrders { get; private set; } = new List<OnlineCustomerOrder>();
        public bool IsDeliveryTripInProgress { get; private set; }
        private bool TrafficReady => GetComponent<RoadTrafficAgent>() is RoadTrafficAgent agent && !agent.Pending;

        // The parking frontage has only one clear aisle; reserve it during manoeuvres.
        private static CourierMotorcycleController parkingAisleOwner;
        private bool enteredParkingAisle;
        private static bool InParkingAisle(Vector3 p) => p.x > 11f && p.x < 16f && p.z > -2f && p.z < 52f;
        private void ReleaseParkingAisle()
        {
            if (parkingAisleOwner == this) parkingAisleOwner = null;
            enteredParkingAisle = false;
        }
        private Transform[] wheels;
        private Light headlight;
        private VehicleHeadlightController headlightRig;
        private Transform gripLeft;
        private Transform gripRight;
        private float wheelSpinSpeed = 650f;
        private float driveSpeed = 10.5f; // Arabalarla aynı hızda (10.5 m/s)

        // Yol Koordinat Sabitleri (Trafik ve Teslimat Yolu Şeritleri - CityTrafficManager ile %100 Birebir Uyumlu)
        private const float LANE_OFFSET = 1.5f;                 // 6m genişliğindeki çift yönlü yolun tam şerit merkezi (1.5m)
        private const float SHOP_LANE_X = 13.0f;               // Dükkan yanı motor & teslimat yolu merkezi
        private const float SHOP_LANE_NORTHBOUND_X = 14.7f;    // Reserved single aisle outside the parked bikes.
        private const float SHOP_LANE_SOUTHBOUND_X = 14.7f;

        private const float MAIN_ROAD_Z = MainBoulevard.Center;               // Orta ana otoyol merkezi
        private const float MAIN_ROAD_EASTBOUND_Z = MainBoulevard.EastOuter;    // Otoyol Doğuya gidiş şeridi (+X)
        private const float MAIN_ROAD_WESTBOUND_Z = MainBoulevard.WestOuter;     // Otoyol Batıya gidiş şeridi (-X)

        private const float WEST_ROAD_X = -75.0f;              // Batı bağlantı caddesi merkezi
        private const float WEST_ROAD_NORTHBOUND_X = -73.5f;   // Batı yolu Kuzeye gidiş şeridi (+Z)
        private const float WEST_ROAD_SOUTHBOUND_X = -76.5f;   // Batı yolu Güneye iniş şeridi (-Z)

        private const float EAST_ROAD_X = 75.0f;               // Doğu bağlantı caddesi merkezi
        private const float EAST_ROAD_NORTHBOUND_X = 76.5f;    // Doğu yolu Kuzeye gidiş şeridi (+Z)
        private const float EAST_ROAD_SOUTHBOUND_X = 73.5f;    // Doğu yolu Güneye iniş şeridi (-Z)

        private const float NORTH_ROAD_Z = 50.0f;              // Kuzey çevre yolu merkezi
        private const float NORTH_ROAD_EASTBOUND_Z = 48.5f;    // Kuzey yolu Doğuya gidiş şeridi (+X)
        private const float NORTH_ROAD_WESTBOUND_Z = 51.5f;    // Kuzey yolu Batıya gidiş şeridi (-X)

        private const float SOUTH_ROAD_Z = -55.0f;             // Kasaba alt çevre yolu merkezi
        private const float SOUTH_ROAD_EASTBOUND_Z = -56.5f;   // Kasaba alt yolu Doğuya gidiş şeridi (+X)
        private const float SOUTH_ROAD_WESTBOUND_Z = -53.5f;   // Kasaba alt yolu Batıya gidiş şeridi (-X)

        private const float SOUTH_MID_AVENUE_X = 0.0f;         // Güney Orta Bulvar (Cami & Kafe Arası)
        private const float SOUTH_MID_AVENUE_NORTHBOUND_X = 1.5f;
        private const float SOUTH_MID_AVENUE_SOUTHBOUND_X = -1.5f;

        public void Setup(int slotIdx, Vector3 homePos, Quaternion homeRot, Transform[] wheelRefs, Light headlightRef, Transform driverMount)
        {
            SlotIndex = slotIdx;
            HomeParkPosition = homePos;
            HomeParkRotation = homeRot;
            transform.position = homePos;
            transform.rotation = homeRot;
            RoadTrafficAgent.Register(transform);
            wheels = wheelRefs;
            headlight = headlightRef;
            DriverSeatMount = driverMount;

            // Motosiklet Tıklama Collider'ı ve Personel Profil Kartı Hedefi
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            col.center = new Vector3(0f, 0.75f, 0f);
            col.size = new Vector3(1.1f, 1.6f, 2.2f);

            StaffClickableTarget motoClick = GetComponent<StaffClickableTarget>() ?? gameObject.AddComponent<StaffClickableTarget>();
            motoClick.courierMoto = this;

            gripLeft = transform.Find("Grip_L");
            gripRight = transform.Find("Grip_R");
            headlightRig = GetComponent<VehicleHeadlightController>();

            if (headlight != null)
            {
                Farm2Shelf.Utils.LightingPipelineBinder.ConfigureRealtimeLight(headlight);
                headlight.enabled = false;
            }

            ApplyHeadlights(false);
        }

        private void Update()
        {
            bool isMoving =
                CurrentState == MotorcycleState.Departing ||
                CurrentState == MotorcycleState.EnRouteDelivery ||
                CurrentState == MotorcycleState.DeliveringAtDoorstep ||
                CurrentState == MotorcycleState.ReturningToStore;
            ApplyHeadlights(isMoving);
        }

        private void ApplyHeadlights(bool isMoving)
        {
            if (headlightRig != null)
            {
                headlightRig.SetHeadlightsActive(isMoving);
                return;
            }

            if (headlight == null) return;

            bool isNight;
            if (DayNightCycleManager.Instance != null)
            {
                isNight = DayNightCycleManager.Instance.IsNight;
            }
            else if (TimeManager.Instance != null)
            {
                float h = TimeManager.Instance.Hour + TimeManager.Instance.Minute / 60f;
                isNight = (h >= 19.5f || h < 6.5f);
            }
            else
            {
                isNight = false;
            }

            headlight.enabled = isNight && isMoving;
        }

        public void AssignCourier(StaffMember courier)
        {
            AssignedCourier = courier;
            StaffClickableTarget motoClick = GetComponent<StaffClickableTarget>() ?? gameObject.AddComponent<StaffClickableTarget>();
            motoClick.staffMember = courier;
            motoClick.courierMoto = this;
        }

        public void ClearCourier()
        {
            AssignedCourier = null;
            StaffClickableTarget motoClick = GetComponent<StaffClickableTarget>();
            if (motoClick != null) motoClick.staffMember = null;

            if (CourierRiderObj != null)
            {
                Destroy(CourierRiderObj);
                CourierRiderObj = null;
            }
        }

        public void MountRider(GameObject rider)
        {
            CourierRiderObj = rider;
            if (rider == null) return;

            if (DriverSeatMount == null)
            {
                Transform found = transform.Find("Driver_Seat_MountPoint");
                if (found == null) found = FindDeep(transform, "Driver_Seat_MountPoint");
                DriverSeatMount = found != null ? found : transform;
            }

            rider.transform.SetParent(DriverSeatMount, false);
            if (rider.TryGetComponent<PedestrianNavigator>(out var walkingAgent)) walkingAgent.enabled = false;
            ApplyMountedRiderPose();
        }

        private void LateUpdate()
        {
            if (CourierRiderObj != null && CourierRiderObj.transform.parent != null)
            {
                ApplyMountedRiderPose();
            }
        }

        private void ApplyMountedRiderPose()
        {
            GameObject rider = CourierRiderObj;
            if (rider == null) return;

            const float sitScale = 0.70f;
            rider.transform.localScale = new Vector3(sitScale, sitScale, sitScale);

            Transform legL = FindDeep(rider.transform, "Leg_L");
            Transform legR = FindDeep(rider.transform, "Leg_R");
            Transform armL = FindDeep(rider.transform, "Arm_L");
            Transform armR = FindDeep(rider.transform, "Arm_R");

            Vector3 hipLocal = legL != null ? legL.localPosition : new Vector3(0f, 1.10f, 0f);
            Vector3 scaledHip = Vector3.Scale(hipLocal, rider.transform.localScale);
            Quaternion sitLean = Quaternion.Euler(7f, 0f, 0f);
            // Kalça sele ortasında kalsın; öne yatış ayaktan değil kalçadan uygulanır.
            Vector3 hipOnSeat = new Vector3(0f, 0.04f, -0.10f);
            rider.transform.localRotation = sitLean;
            rider.transform.localPosition = hipOnSeat - sitLean * scaledHip;

            CapsuleCollider riderCol = rider.GetComponent<CapsuleCollider>();
            if (riderCol != null) riderCol.enabled = false;

            if (legL != null) legL.localRotation = Quaternion.Euler(-64f, 10f, 28f);
            if (legR != null) legR.localRotation = Quaternion.Euler(-64f, -10f, -28f);

            if (gripLeft == null) gripLeft = transform.Find("Grip_L");
            if (gripRight == null) gripRight = transform.Find("Grip_R");

            if (gripLeft != null && armL != null)
            {
                AimLimbToward(armL, gripLeft.position);
            }
            else if (armL != null)
            {
                armL.localRotation = Quaternion.Euler(-68f, 18f, 16f);
            }

            if (gripRight != null && armR != null)
            {
                AimLimbToward(armR, gripRight.position);
            }
            else if (armR != null)
            {
                armR.localRotation = Quaternion.Euler(-68f, -18f, -16f);
            }
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            if (root == null) return null;
            if (root.name == objectName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), objectName);
                if (found != null) return found;
            }
            return null;
        }

        private static void AimLimbToward(Transform limbPivot, Vector3 worldTarget)
        {
            Vector3 toTarget = worldTarget - limbPivot.position;
            if (toTarget.sqrMagnitude < 0.0001f) return;
            limbPivot.rotation = Quaternion.FromToRotation(Vector3.down, toTarget.normalized);
        }

        public void UnmountRider()
        {
            if (CourierRiderObj != null)
            {
                Transform legL = FindDeep(CourierRiderObj.transform, "Leg_L");
                Transform legR = FindDeep(CourierRiderObj.transform, "Leg_R");
                Transform armL = FindDeep(CourierRiderObj.transform, "Arm_L");
                Transform armR = FindDeep(CourierRiderObj.transform, "Arm_R");

                if (legL != null) legL.localRotation = Quaternion.identity;
                if (legR != null) legR.localRotation = Quaternion.identity;
                if (armL != null) armL.localRotation = Quaternion.identity;
                if (armR != null) armR.localRotation = Quaternion.identity;

                CapsuleCollider riderCol = CourierRiderObj.GetComponent<CapsuleCollider>();
                if (riderCol != null) riderCol.enabled = true;

                CourierRiderObj.transform.localScale = Vector3.one;
                CourierRiderObj.transform.SetParent(null);
                if (CourierRiderObj.TryGetComponent<PedestrianNavigator>(out var walkingAgent)) walkingAgent.enabled = true;
                CourierRiderObj = null;
            }
        }

        public void DismountRider() => UnmountRider();

        public bool CanTakeOrders()
        {
            if (!OnlineMarketOrderManager.CanReceiveNewOrders) return false;
            return TrafficReady && (CurrentState == MotorcycleState.ParkedInBay || CurrentState == MotorcycleState.WaitingForStocker)
                && AssignedCourier != null
                && LoadedOrders.Count < 2;
        }

        public bool CanTakeContractOrder()
        {
            if (!OnlineMarketOrderManager.CanReceiveNewOrders) return false;
            return TrafficReady && (CurrentState == MotorcycleState.ParkedInBay)
                && AssignedCourier != null
                && (LoadedOrders == null || LoadedOrders.Count == 0);
        }

        public void SetWaitingForStocker(OnlineCustomerOrder order)
        {
            CurrentState = MotorcycleState.WaitingForStocker;
            if (!LoadedOrders.Contains(order))
            {
                LoadedOrders.Add(order);
            }
        }

        public void AssignOrderToCargo(OnlineCustomerOrder order) => SetWaitingForStocker(order);

        public void DispatchDeliveryTrip()
        {
            if (OnlineMarketOrderManager.IsCourierDayClosed) return;
            if (LoadedOrders.Count == 0) return;
            if (IsDeliveryTripInProgress) return;
            if (!TrafficReady) return;
            StopAllCoroutines();
            StartCoroutine(DeliveryTripRoutine());
        }

        public void StartDeliveryRoute() => DispatchDeliveryTrip();

        private IEnumerator DeliveryTripRoutine()
        {
            IsDeliveryTripInProgress = true;
            CurrentState = MotorcycleState.EnRouteDelivery;

            Vector3 currentPos = transform.position;

            // 1. YÜKLENEN HER BİR SİPARİŞ İÇİN HEDEF ADRESE SADECE VE SADECE YOLLAR ÜZERİNDEN SÜRÜŞ
            for (int i = 0; i < LoadedOrders.Count; i++)
            {
                var order = LoadedOrders[i];
                if (order == null) continue;

                CurrentState=MotorcycleState.EnRouteDelivery;
                Vector3 targetDoorstep = order.targetDoorstepPosition;
                List<Vector3> routeWaypoints = BuildLegWaypoints(currentPos, targetDoorstep);

                for (int w = 0; w < routeWaypoints.Count; w++)
                {
                    RoadTrafficAgent.SetRoute(transform, routeWaypoints, w);
                    yield return MoveToPosition(routeWaypoints[w], driveSpeed);
                }

                currentPos = routeWaypoints.Count > 0 ? routeWaypoints[routeWaypoints.Count - 1] : targetDoorstep;

                // Real ten-second handoff; pausing the game also pauses the handoff.
                CurrentState = MotorcycleState.DeliveringAtDoorstep;
                float handoff=0f;
                while(handoff<10f)
                {if(Time.deltaTime>0f)handoff+=Time.unscaledDeltaTime;yield return null;}

                // Teslimatı Tamamla ve Sonuçları İşle
                if (OnlineMarketOrderManager.Instance != null)
                {
                    OnlineMarketOrderManager.Instance.CompleteOrderDelivery(order);
                }
            }

            LoadedOrders.Clear();

            // 3. DÜKKANA GELİŞ ŞERİDİNDEN GERİ DÖNÜŞ SÜRÜŞÜ
            CurrentState = MotorcycleState.ReturningToStore;

            List<Vector3> returnWaypoints = BuildReturnWaypoints(currentPos);
            for (int r = 0; r < returnWaypoints.Count; r++)
            {
                RoadTrafficAgent.SetRoute(transform, returnWaypoints, r);
                yield return MoveToPosition(returnWaypoints[r], driveSpeed);
            }

            // Kendi sarı park yuvasına tam yanaş ve park rotasyonuna otur
            // Turn in the aisle, then reverse straight into the bay. Turning
            // between the parked neighbours would sweep through their bodies.
            while (Quaternion.Angle(transform.rotation, HomeParkRotation) > 2f)
            {
                transform.rotation = RoadTrafficAgent.Rotate(transform, Quaternion.RotateTowards(transform.rotation, HomeParkRotation, 150f * Time.deltaTime));
                yield return null;
            }
            yield return MoveToPosition(HomeParkPosition, 2.0f, true);

            while (Quaternion.Angle(transform.rotation, HomeParkRotation) > 2f)
            {
                transform.rotation = Farm2Shelf.Environment.RoadTrafficAgent.Rotate(transform, Quaternion.Slerp(transform.rotation, HomeParkRotation, 12f * Time.deltaTime));
                yield return null;
            }
            transform.rotation = HomeParkRotation;

            CurrentState = MotorcycleState.ParkedInBay;
            ReleaseParkingAisle();
            RoadTrafficAgent.SetRoute(transform, null, 0);
            IsDeliveryTripInProgress = false;

            if (CourierManager.Instance != null)
            {
                CourierManager.Instance.OnMotorcycleReturnedToBay(this);
            }
        }

        private void OnDisable() => ReleaseParkingAisle();

        private void OnDestroy()
        {
            ReleaseParkingAisle();
            IsDeliveryTripInProgress = false;
        }

        private IEnumerator MoveToPosition(Vector3 targetPos, float speed, bool reverse = false)
        {
            if (InParkingAisle(transform.position) || InParkingAisle(targetPos))
            {
                while (parkingAisleOwner != null && parkingAisleOwner != this) yield return null;
                parkingAisleOwner = this;
                enteredParkingAisle |= InParkingAisle(transform.position);
            }
            Vector3 targetFlat = new Vector3(targetPos.x, 0f, targetPos.z);

            while (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), targetFlat) > 0.15f)
            {
                Vector3 currentFlat = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 dir = (targetFlat - currentFlat).normalized;

                if (!reverse && dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Farm2Shelf.Environment.RoadTrafficAgent.Rotate(transform, Quaternion.Slerp(transform.rotation, targetRot, 14f * Time.deltaTime));
                }

                // Şehir trafiğinde öndeki araca göre güvenli hız sınırlaması
                float currentSpeed = speed;
                if (MainBoulevard.OnRoad(transform.position.z) && MainBoulevard.Opening(transform.position.x) && Mathf.Abs(dir.z)>.15f)
                    currentSpeed = Mathf.Min(currentSpeed,3f);
                if (CityTrafficManager.Instance != null && dir != Vector3.zero)
                {
                    currentSpeed = CityTrafficManager.Instance.GetSpeedLimitInFront(transform.position, dir, 10.0f, currentSpeed);
                }

                Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);

                // Köprüdeyse yükselme/eğim hesapla
                float slopeY;
                float bridgeY = CityTrafficManager.GetBridgeElevation(nextPos.x, nextPos.z, out slopeY);
                nextPos.y = bridgeY;

                Vector3 beforeMove = transform.position;
                transform.position = RoadTrafficAgent.Move(transform, nextPos);
                if (parkingAisleOwner == this)
                {
                    enteredParkingAisle |= InParkingAisle(transform.position);
                    if (enteredParkingAisle && !InParkingAisle(transform.position)) ReleaseParkingAisle();
                }

                // Tekerlekleri döndür
                if (wheels != null)
                {
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        if (wheels[i] != null)
                        {
                            wheels[i].Rotate(Vector3.right, (transform.position-beforeMove).magnitude * wheelSpinSpeed / driveSpeed * (reverse ? -1f : 1f), Space.Self);
                        }
                    }
                }

                yield return null;
            }

            float finalSlopeY;
            float finalY = CityTrafficManager.GetBridgeElevation(targetPos.x, targetPos.z, out finalSlopeY);
            transform.position = RoadTrafficAgent.Move(transform, new Vector3(targetPos.x, finalY, targetPos.z));
        }

        #region Strict Road Graph Router (100% On-Road Navigation)

        private static float GetClosestNorthAvenueX(float x)
        {
            if (x >= 86.0f)
                return x <= 135.0f ? 112.5f : 150.0f;

            float[] aves = new float[] { -75.0f, -37.5f, 0.0f, 37.5f, 75.0f, 112.5f, 150.0f };
            float closest = aves[0];
            float minDist = Mathf.Abs(x - closest);
            for (int i = 1; i < aves.Length; i++)
            {
                float d = Mathf.Abs(x - aves[i]);
                if (d < minDist)
                {
                    minDist = d;
                    closest = aves[i];
                }
            }
            return closest;
        }

        private static float GetClosestWestAvenueX(float x)
        {
            float[] aves = new float[] { -112.0f, -150.0f, -188.0f, -226.0f };
            float closest = aves[0];
            float minDist = Mathf.Abs(x - closest);
            for (int i = 1; i < aves.Length; i++)
            {
                float d = Mathf.Abs(x - aves[i]);
                if (d < minDist)
                {
                    minDist = d;
                    closest = aves[i];
                }
            }
            return closest;
        }

        private static void AddBridgeWaypoints(List<Vector3> list, bool isEastbound)
        {
            if (isEastbound)
            {
                list.Add(new Vector3(-108.0f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                list.Add(new Vector3(-102.0f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                list.Add(new Vector3(-95.0f, 1.65f, MAIN_ROAD_EASTBOUND_Z)); // Köprü Zirvesi
                list.Add(new Vector3(-88.0f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                list.Add(new Vector3(-82.0f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            }
            else
            {
                list.Add(new Vector3(-82.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                list.Add(new Vector3(-88.0f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                list.Add(new Vector3(-95.0f, 1.65f, MAIN_ROAD_WESTBOUND_Z)); // Köprü Zirvesi
                list.Add(new Vector3(-102.0f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                list.Add(new Vector3(-108.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
            }
        }

        /// <summary>
        /// Karayolu şebekesinde verilen başlangıç noktasından (start) hedef teslimat noktasına (dest)
        /// SADECE VE SADECE asfalt yollardan ve SAĞDAN AKAN TRAFİK kurallarına göre rota noktaları üretir.
        /// </summary>
        private List<Vector3> BuildLegWaypoints(Vector3 start, Vector3 dest)
        {
            bool leavingStore = Mathf.Abs(start.x - SHOP_LANE_X) < 3f && start.z >= -2f && start.z <= 25f;
            if (!leavingStore) return CourierRoadNetwork.BuildDeliveryRoute(start,dest);
            Vector3 aisle = new Vector3(SHOP_LANE_NORTHBOUND_X, .05f, start.z);
            var points = CourierRoadNetwork.BuildDeliveryRoute(aisle,dest);
            points.Insert(0, start);
            return points;
        }

        /// <summary>
        /// Teslimat bittiğinde dükkandaki sarı park yuvasına SADECE VE SADECE asfalt yollar ve SAĞDAN AKAN TRAFİK üzerinden geri dönüş rotası üretir.
        /// </summary>
        private List<Vector3> BuildReturnWaypoints(Vector3 start)
        {
            return CourierRoadNetwork.BuildRoute(start,
                new Vector3(SHOP_LANE_NORTHBOUND_X, .05f, HomeParkPosition.z));
        }

        #endregion
    }
}
