using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public class CityTrafficManager : MonoBehaviour
    {
        public static CityTrafficManager Instance { get; private set; }

        private const float BASE_SPEED = 10.5f; // m/s (Dengeli ve akıcı hız)
        private const float SAFE_DISTANCE = 16.0f; // Emniyet takip mesafesi

        // Şerit Hizalama Offsetleri (6m Genişlikteki Yolların 1.5m Tam Şerit Merkezleri)
        private const float MAIN_ROAD_EASTBOUND_Z = -10.5f;
        private const float MAIN_ROAD_WESTBOUND_Z = -7.5f;

        private const float WEST_ROAD_NORTHBOUND_X = -73.5f;
        private const float WEST_ROAD_SOUTHBOUND_X = -76.5f;

        private const float EAST_ROAD_NORTHBOUND_X = 76.5f;
        private const float EAST_ROAD_SOUTHBOUND_X = 73.5f;

        private const float NORTH_ROAD_EASTBOUND_Z = 48.5f;
        private const float NORTH_ROAD_WESTBOUND_Z = 51.5f;

        private const float SOUTH_ROAD_EASTBOUND_Z = -56.5f;

        private readonly List<ActiveVehicleData> activeVehicles = new List<ActiveVehicleData>();
        private readonly List<VehicleType> vehicleDeck = new List<VehicleType>();
        private VehicleType lastSpawnedType = (VehicleType)(-1);

        private float spawnTimer = 0f;
        private Transform trafficParentGroup;

        private class ActiveVehicleData
        {
            public GameObject vehicleObj;
            public List<Transform> wheels;
            public VehicleType type;
            public List<Vector3> waypoints;
            public int currentWaypointIndex;
            public float speed;
            public float currentSpeed;

            // Takılma ve Çakışma Önleme Sayacı
            public Vector3 lastCheckPos;
            public float stuckTimer;
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

        private void Start()
        {
            GameObject grpObj = new GameObject("City_Traffic_Group");
            grpObj.transform.SetParent(transform);
            trafficParentGroup = grpObj.transform;

            SpawnInitialTraffic();
        }

        public float GetSpeedLimitInFront(Vector3 currentPos, Vector3 moveDir, float maxDist, float defaultSpeed)
        {
            float targetSpeed = defaultSpeed;
            for (int i = 0; i < activeVehicles.Count; i++)
            {
                var v = activeVehicles[i];
                if (v.vehicleObj == null) continue;

                Vector3 vPos = v.vehicleObj.transform.position;
                Vector3 diff = vPos - currentPos;
                float dist = diff.magnitude;

                if (dist < maxDist)
                {
                    float forwardDot = Vector3.Dot(moveDir.normalized, diff.normalized);
                    if (forwardDot > 0.5f)
                    {
                        float safeSpeedAhead = v.currentSpeed * 0.85f;
                        if (safeSpeedAhead < targetSpeed)
                        {
                            targetSpeed = safeSpeedAhead;
                        }
                    }
                }
            }
            return Mathf.Max(0.5f, targetSpeed);
        }

        private void Update()
        {
            float currentHour = 12f;
            if (TimeManager.Instance != null)
            {
                currentHour = TimeManager.Instance.Hour;
            }

            // Gündüz / Mesai Saatleri (08:00 - 20:00): 12 araç | Gece Saatleri (20:00 - 08:00): 6 araç
            bool isDaytime = (currentHour >= 8f && currentHour < 20f);
            float targetSpawnInterval = isDaytime ? 2.8f : 6.5f;
            int maxAllowedVehicles = isDaytime ? 12 : 6;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= targetSpawnInterval)
            {
                spawnTimer = 0f;
                if (activeVehicles.Count < maxAllowedVehicles)
                {
                    TrySpawnRandomVehicle();
                }
            }

            UpdateActiveVehicles(Time.deltaTime);
        }

        private void SpawnInitialTraffic()
        {
            SpawnRouteVehicle(GetNorthLoopRoute(true));
            SpawnRouteVehicle(GetStraightRoute(false));
            SpawnRouteVehicle(GetSouthLoopRoute());
        }

        private bool IsWholesaleTruckActive()
        {
            bool wholesaleActive = (WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckOnTheWay);
            bool greenActive = (GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckOnTheWay);
            return wholesaleActive || greenActive;
        }

        private VehicleType GetNextUniqueVehicleType()
        {
            if (vehicleDeck.Count == 0)
            {
                VehicleType[] allTypes = (VehicleType[])System.Enum.GetValues(typeof(VehicleType));
                vehicleDeck.AddRange(allTypes);

                for (int i = 0; i < vehicleDeck.Count; i++)
                {
                    int rndIndex = Random.Range(i, vehicleDeck.Count);
                    VehicleType temp = vehicleDeck[i];
                    vehicleDeck[i] = vehicleDeck[rndIndex];
                    vehicleDeck[rndIndex] = temp;
                }
            }

            VehicleType selected = vehicleDeck[0];
            vehicleDeck.RemoveAt(0);

            if (selected == lastSpawnedType && vehicleDeck.Count > 0)
            {
                VehicleType alt = vehicleDeck[0];
                vehicleDeck[0] = selected;
                selected = alt;
            }

            lastSpawnedType = selected;
            return selected;
        }

        private void TrySpawnRandomVehicle()
        {
            // Sipariş kamyonu yoldayken şehir araçları spawn olmaya devam eder (Bloklama kaldırıldı)

            int routeChoice = Random.Range(0, 5);
            List<Vector3> chosenRoute;

            switch (routeChoice)
            {
                case 0: chosenRoute = GetStraightRoute(true); break;
                case 1: chosenRoute = GetStraightRoute(false); break;
                case 2: chosenRoute = GetNorthLoopRoute(true); break;  // Dükkan arkası üst yola girer
                case 3: chosenRoute = GetNorthLoopRoute(false); break; // Dükkan arkası üst yoldan döner
                default: chosenRoute = GetSouthLoopRoute(); break;
            }

            if (chosenRoute == null || chosenRoute.Count < 2) return;

            Vector3 startPos = chosenRoute[0];
            foreach (var v in activeVehicles)
            {
                if (v.vehicleObj != null && Vector3.Distance(v.vehicleObj.transform.position, startPos) < SAFE_DISTANCE)
                {
                    return;
                }
            }

            SpawnRouteVehicle(chosenRoute);
        }

        private void SpawnRouteVehicle(List<Vector3> route)
        {
            if (route == null || route.Count < 2) return;

            VehicleType selectedType = GetNextUniqueVehicleType();

            GameObject carObj = ProceduralCarModelBuilder.CreateVehicleModel(selectedType, out List<Transform> wheels);
            carObj.transform.SetParent(trafficParentGroup, false);

            Vector3 startPos = route[0];
            Vector3 nextPos = route[1];
            Vector3 startDir = (nextPos - startPos).normalized;

            carObj.transform.position = startPos;
            if (startDir != Vector3.zero)
            {
                carObj.transform.rotation = Quaternion.LookRotation(startDir);
            }

            float speed = BASE_SPEED + Random.Range(-1.2f, 1.2f);

            ActiveVehicleData vData = new ActiveVehicleData
            {
                vehicleObj = carObj,
                wheels = wheels,
                type = selectedType,
                waypoints = route,
                currentWaypointIndex = 1,
                speed = speed,
                currentSpeed = speed,
                lastCheckPos = startPos,
                stuckTimer = 0f
            };

            activeVehicles.Add(vData);
        }

        private void UpdateActiveVehicles(float deltaTime)
        {
            for (int i = activeVehicles.Count - 1; i >= 0; i--)
            {
                ActiveVehicleData vData = activeVehicles[i];
                if (vData.vehicleObj == null || vData.waypoints == null || vData.waypoints.Count == 0)
                {
                    if (vData.vehicleObj != null) Destroy(vData.vehicleObj);
                    activeVehicles.RemoveAt(i);
                    continue;
                }

                Vector3 currentPos = vData.vehicleObj.transform.position;
                Vector3 targetWaypoint = vData.waypoints[vData.currentWaypointIndex];
                Vector3 toTarget = targetWaypoint - currentPos;
                float distToTarget = toTarget.magnitude;

                // Hedef Noktaya Ulaşıldı mı?
                if (distToTarget < 2.2f)
                {
                    vData.currentWaypointIndex++;
                    if (vData.currentWaypointIndex >= vData.waypoints.Count)
                    {
                        Destroy(vData.vehicleObj);
                        activeVehicles.RemoveAt(i);
                        continue;
                    }
                    targetWaypoint = vData.waypoints[vData.currentWaypointIndex];
                    toTarget = targetWaypoint - currentPos;
                }

                float targetSpeed = vData.speed;
                Vector3 moveDir = toTarget.normalized;

                // 1. SADECE AYNI YÖNDE İLERLEYEN ÖNDEKİ ARAÇ İÇİN TAKİP MESAFESİ KONTROLÜ (Karşı şeritteki araçlar fren yapmaz!)
                foreach (var other in activeVehicles)
                {
                    if (other == vData || other.vehicleObj == null) continue;

                    Vector3 otherPos = other.vehicleObj.transform.position;
                    Vector3 diff = otherPos - currentPos;
                    float dist = diff.magnitude;

                    if (dist < SAFE_DISTANCE)
                    {
                        Vector3 otherMoveDir = Vector3.forward;
                        if (other.waypoints != null && other.currentWaypointIndex < other.waypoints.Count)
                        {
                            Vector3 otherTarget = other.waypoints[other.currentWaypointIndex];
                            otherMoveDir = (otherTarget - otherPos).normalized;
                        }

                        float sameDirDot = Vector3.Dot(moveDir, otherMoveDir);
                        float forwardDot = Vector3.Dot(moveDir, diff.normalized);

                        // Sadece aynı yönde ve önümüzde olan araç için yavaşla
                        if (sameDirDot > 0.5f && forwardDot > 0.5f)
                        {
                            targetSpeed = Mathf.Min(targetSpeed, other.currentSpeed * 0.8f);
                        }
                    }
                }

                // Hız İvmelenmesi
                vData.currentSpeed = Mathf.MoveTowards(vData.currentSpeed, targetSpeed, 10.0f * deltaTime);

                // Yön Dönüşü (Yumuşak Viraj Dönüşü)
                if (moveDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    vData.vehicleObj.transform.rotation = Quaternion.RotateTowards(vData.vehicleObj.transform.rotation, targetRot, 240f * deltaTime);
                }

                // İlerleme
                Vector3 nextPos = Vector3.MoveTowards(currentPos, targetWaypoint, vData.currentSpeed * deltaTime);
                vData.vehicleObj.transform.position = nextPos;

                // 2. TAKILMA / SIKIŞMA OTOMATİK KURTARMA (Anti-Stuck Safeguard)
                if (Vector3.Distance(currentPos, vData.lastCheckPos) < 0.2f)
                {
                    vData.stuckTimer += deltaTime;
                    if (vData.stuckTimer > 3.5f)
                    {
                        Destroy(vData.vehicleObj);
                        activeVehicles.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    vData.lastCheckPos = currentPos;
                    vData.stuckTimer = 0f;
                }

                // Tekerlekleri Sürüş Hızına Göre Döndür
                if (vData.wheels != null && vData.wheels.Count > 0)
                {
                    float rotAngle = (vData.currentSpeed * deltaTime * 180f) / (Mathf.PI * 0.7f);
                    foreach (var w in vData.wheels)
                    {
                        if (w != null) w.Rotate(Vector3.right * rotAngle, Space.Self);
                    }
                }
            }
        }

        #region Strict Lane Route Definitions

        // 1. DÜZ OTOYOL ROTASI (Tam Şerit Offsetleri)
        private List<Vector3> GetStraightRoute(bool isEastbound)
        {
            List<Vector3> route = new List<Vector3>();
            if (isEastbound)
            {
                route.Add(new Vector3(-180f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0f, MAIN_ROAD_EASTBOUND_Z));
            }
            else
            {
                route.Add(new Vector3(180f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-180f, 0f, MAIN_ROAD_WESTBOUND_Z));
            }
            return route;
        }

        // 2. DÜKKAN VE OTOPARK ARKA/ÜST YOLU ROTASI (Tam Şerit Offsetli Kavisler)
        private List<Vector3> GetNorthLoopRoute(bool isEastboundStart)
        {
            List<Vector3> route = new List<Vector3>();
            if (isEastboundStart)
            {
                route.Add(new Vector3(-180f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-75.0f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0f, -8.5f));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0f, 47.0f));
                route.Add(new Vector3(-72.0f, 0f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(72.0f, 0f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0f, 47.0f));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0f, -8.5f));
                route.Add(new Vector3(75.0f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0f, MAIN_ROAD_EASTBOUND_Z));
            }
            else
            {
                route.Add(new Vector3(180f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(78.0f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, -6.0f));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, 50.0f));
                route.Add(new Vector3(75.0f, 0f, NORTH_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-75.0f, 0f, NORTH_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, 50.0f));
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, -6.0f));
                route.Add(new Vector3(-78.0f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-180f, 0f, MAIN_ROAD_WESTBOUND_Z));
            }
            return route;
        }

        // 3. KASABA GÜNEY SOKAK ROTASI (Tam Şerit Offsetli Kavisler)
        private List<Vector3> GetSouthLoopRoute()
        {
            List<Vector3> route = new List<Vector3>();
            route.Add(new Vector3(-180f, 0f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(-78.0f, 0f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, -12.0f));
            route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, -55.0f));
            route.Add(new Vector3(-75.0f, 0f, SOUTH_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(75.0f, 0f, SOUTH_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, -55.0f));
            route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, -12.0f));
            route.Add(new Vector3(78.0f, 0f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(180f, 0f, MAIN_ROAD_EASTBOUND_Z));
            return route;
        }

        #endregion
    }
}
