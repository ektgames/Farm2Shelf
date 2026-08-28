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

        // Kasaba Alt Yolu (Z = -55m | Gidiş: -56.5f, Geliş: -53.5f)
        private const float SOUTH_ROAD_EASTBOUND_Z = -56.5f;
        private const float SOUTH_ROAD_WESTBOUND_Z = -53.5f;

        // Güney Dış Çevre Yolu (Z = -128m | Gidiş: -129.5f, Geliş: -126.5f)
        private const float SOUTH_OUTER_ROAD_EASTBOUND_Z = -129.5f;
        private const float SOUTH_OUTER_ROAD_WESTBOUND_Z = -126.5f;

        // Güney Orta Bulvar (X = 0m | Kuzeye Gidiş: +1.5f, Güneye İniş: -1.5f)
        private const float SOUTH_MID_AVENUE_NORTHBOUND_X = 1.5f;
        private const float SOUTH_MID_AVENUE_SOUTHBOUND_X = -1.5f;

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

            // Gündüz / Mesai Saatleri (08:00 - 20:00): 18 araç | Gece Saatleri (20:00 - 08:00): 10 araç
            bool isDaytime = (currentHour >= 8f && currentHour < 20f);
            float targetSpawnInterval = isDaytime ? 2.0f : 4.5f;
            int maxAllowedVehicles = isDaytime ? 18 : 10;

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
            SpawnRouteVehicle(GetApartmentDistrictRoute(0));
            SpawnRouteVehicle(GetWestDistrictRoute(0));
            SpawnRouteVehicle(GetSouthDistrictRoute(0));
            SpawnRouteVehicle(GetSouthDistrictRoute(1));
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
            int routeChoice = Random.Range(0, 16);
            List<Vector3> chosenRoute;

            switch (routeChoice)
            {
                case 0: chosenRoute = GetStraightRoute(true); break;
                case 1: chosenRoute = GetStraightRoute(false); break;
                case 2: chosenRoute = GetNorthLoopRoute(true); break;
                case 3: chosenRoute = GetNorthLoopRoute(false); break;
                case 4: chosenRoute = GetApartmentDistrictRoute(0); break; // Kuzey 1. Cadde -> Merkez Bulvarı
                case 5: chosenRoute = GetApartmentDistrictRoute(1); break; // Kuzey 2. Cadde -> 1. Cadde
                case 6: chosenRoute = GetApartmentDistrictRoute(2); break; // Kuzey Merkez Bulvarı -> 2. Cadde
                case 7: chosenRoute = GetWestDistrictRoute(0); break;      // Batı Köprüsü -> Kuzey Lüks Villalar
                case 8: chosenRoute = GetWestDistrictRoute(1); break;      // Batı Köprüsü -> Güney Kamu / Stadyum
                case 9: chosenRoute = GetWestDistrictRoute(2); break;      // Batı Köprüsü -> Batı İlkokul / Kütüphane
                case 10: chosenRoute = GetWestDistrictRoute(3); break;     // Batı Köprüsü -> Tam Batı Çevre Turu
                case 11: chosenRoute = GetSouthDistrictRoute(0); break;    // Güney Cami & Kafe: Orta Bulvar -> Kafe Caddesi
                case 12: chosenRoute = GetSouthDistrictRoute(1); break;    // Güney Cami & Kafe: Kafe Caddesi -> Cami Arkası
                case 13: chosenRoute = GetSouthDistrictRoute(2); break;    // Güney Cami & Kafe: Cami Batısı -> Orta Bulvar
                case 14: chosenRoute = GetSouthDistrictRoute(3); break;    // Büyük Tüm Şehir Turu (Kuzey + Kasaba + Güney + Batı)
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

                // Hedef Noktaya Ulaşıldı mı? (Şeritten sapmaması için hassas mesafe)
                if (distToTarget < 0.9f)
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

                // 1. SADECE AYNI YÖNDE İLERLEYEN ÖNDEKİ ARAÇ İÇİN TAKİP MESAFESİ KONTROLÜ
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

                        if (sameDirDot > 0.5f && forwardDot > 0.5f)
                        {
                            targetSpeed = Mathf.Min(targetSpeed, other.currentSpeed * 0.8f);
                        }
                    }
                }

                // Hız İvmelenmesi
                vData.currentSpeed = Mathf.MoveTowards(vData.currentSpeed, targetSpeed, 10.0f * deltaTime);

                // İlerleme
                Vector3 nextPos = Vector3.MoveTowards(currentPos, targetWaypoint, vData.currentSpeed * deltaTime);

                // Köprü Kavisini ve Yokuş Eğimi (Pitch Angle) Hesaplama
                float slopeY;
                float bridgeY = GetBridgeElevation(nextPos.x, nextPos.z, out slopeY);
                nextPos.y = bridgeY;

                // Yön Dönüşü ve Köprü Eğimine (Yokuş Çıkış/İniş) Uyum Sağlama
                if (moveDir != Vector3.zero)
                {
                    Vector3 tangentDir = moveDir;
                    if (Mathf.Abs(slopeY) > 0.001f && Mathf.Abs(moveDir.x) > 0.05f)
                    {
                        tangentDir = new Vector3(moveDir.x, slopeY * Mathf.Sign(moveDir.x), moveDir.z).normalized;
                    }
                    Quaternion targetRot = Quaternion.LookRotation(tangentDir, Vector3.up);
                    vData.vehicleObj.transform.rotation = Quaternion.RotateTowards(vData.vehicleObj.transform.rotation, targetRot, 360f * deltaTime);
                }

                vData.vehicleObj.transform.position = nextPos;

                // 2. TAKILMA / SIKIŞMA OTOMATİK KURTARMA
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

        // 1. DÜZ OTOYOL ROTASI (Köprü Kavisini Tırmanarak Geçer)
        private List<Vector3> GetStraightRoute(bool isEastbound)
        {
            List<Vector3> route = new List<Vector3>();
            if (isEastbound)
            {
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_EASTBOUND_Z)); // Köprü Zirvesi
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            }
            else
            {
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_WESTBOUND_Z)); // Köprü Zirvesi
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
            }
            return route;
        }

        // 2. DÜKKAN VE OTOPARK ARKA/ÜST YOLU ROTASI
        private List<Vector3> GetNorthLoopRoute(bool isEastboundStart)
        {
            List<Vector3> route = new List<Vector3>();
            if (isEastboundStart)
            {
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-76.5f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0f, -7.5f));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0f, 45.0f));
                route.Add(new Vector3(-70.5f, 0f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(70.5f, 0f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0f, 45.0f));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0f, -7.5f));
                route.Add(new Vector3(76.5f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0f, MAIN_ROAD_EASTBOUND_Z));
            }
            else
            {
                route.Add(new Vector3(180f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(76.5f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, -4.5f));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, 48.5f));
                route.Add(new Vector3(73.5f, 0f, NORTH_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-73.5f, 0f, NORTH_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, 48.5f));
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, -4.5f));
                route.Add(new Vector3(-79.5f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
            }
            return route;
        }

        // 3. KASABA GÜNEY SOKAK ROTASI
        private List<Vector3> GetSouthLoopRoute()
        {
            List<Vector3> route = new List<Vector3>();
            route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(-76.5f, 0f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, -12.0f));
            route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, -54.0f));
            route.Add(new Vector3(-73.5f, 0f, SOUTH_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(73.5f, 0f, SOUTH_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, -54.0f));
            route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, -12.0f));
            route.Add(new Vector3(76.5f, 0f, MAIN_ROAD_EASTBOUND_Z));
            route.Add(new Vector3(180f, 0f, MAIN_ROAD_EASTBOUND_Z));
            return route;
        }

        // 4. KUZEY APARTMANLAR MAHALLESİ VE BULVAR ROTASI (Tam Sağ Şerit Disiplini)
        private List<Vector3> GetApartmentDistrictRoute(int pattern)
        {
            List<Vector3> route = new List<Vector3>();

            if (pattern == 0)
            {
                // Batıdan gel -> Batı Yolu -> 1. Cadde Kuzey Şeridi (X: -36.0) -> Üst Çevre Yolu Doğu Şeridi (Z: 173.5) -> Merkez Bulvar Güney Şeridi (X: -1.5) -> Doğu Yolu -> Otoyol
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-76.5f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0f, -7.5f));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0f, 45.0f));
                route.Add(new Vector3(-70.5f, 0f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-39.0f, 0f, NORTH_ROAD_EASTBOUND_Z));
                // 1. Cadde Kuzey Şeridi (X: -36.0)
                route.Add(new Vector3(-36.0f, 0f, 51.5f));
                route.Add(new Vector3(-36.0f, 0f, 55.0f));
                route.Add(new Vector3(-36.0f, 0f, 112.5f));
                route.Add(new Vector3(-36.0f, 0f, 170.0f));
                // Üst Çevre Yolu Doğu Şeridi (Z: 173.5)
                route.Add(new Vector3(-34.5f, 0f, 173.5f));
                route.Add(new Vector3(-3.0f, 0f, 173.5f));
                // Merkez Bulvar Güney Şeridi (X: -1.5)
                route.Add(new Vector3(-1.5f, 0f, 170.0f));
                route.Add(new Vector3(-1.5f, 0f, 112.5f));
                route.Add(new Vector3(-1.5f, 0f, 55.0f));
                route.Add(new Vector3(-1.5f, 0f, 51.5f));
                // Alt Kuzey Yolu Doğu Şeridi (Z: 48.5)
                route.Add(new Vector3(0f, 0f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(70.5f, 0f, NORTH_ROAD_EASTBOUND_Z));
                // Doğu Yolu Güney Şeridi (X: 73.5)
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0f, 45.0f));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0f, -7.5f));
                route.Add(new Vector3(76.5f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0f, MAIN_ROAD_EASTBOUND_Z));
            }
            else if (pattern == 1)
            {
                // Doğudan gel -> Doğu Yolu -> 2. Cadde Kuzey Şeridi (X: 39.0) -> Üst Çevre Yolu Batı Şeridi (Z: 176.5) -> 1. Cadde Güney Şeridi (X: -39.0) -> Batı Yolu -> Otoyol
                route.Add(new Vector3(180f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(76.5f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, -4.5f));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0f, 48.5f));
                route.Add(new Vector3(73.5f, 0f, NORTH_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(42.0f, 0f, NORTH_ROAD_WESTBOUND_Z));
                // 2. Cadde Kuzey Şeridi (X: 39.0)
                route.Add(new Vector3(39.0f, 0f, 51.5f));
                route.Add(new Vector3(39.0f, 0f, 170.0f));
                // Üst Çevre Yolu Batı Şeridi (Z: 176.5)
                route.Add(new Vector3(36.0f, 0f, 176.5f));
                route.Add(new Vector3(-36.0f, 0f, 176.5f));
                // 1. Cadde Güney Şeridi (X: -39.0)
                route.Add(new Vector3(-39.0f, 0f, 170.0f));
                route.Add(new Vector3(-39.0f, 0f, 51.5f));
                // Kuzey Yol Batı Şeridi
                route.Add(new Vector3(-42.0f, 0f, NORTH_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-73.5f, 0f, NORTH_ROAD_WESTBOUND_Z));
                // Batı Yolu Güney Şeridi
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, 48.5f));
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0f, -4.5f));
                route.Add(new Vector3(-79.5f, 0f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
            }
            else
            {
                // Bulvardan Giriş / Dönüş Turu (X: +1.5 Kuzey Şeridi -> Üst Çevre -> 2. Cadde Güney Şeridi X: 36.0)
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-76.5f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0f, -7.5f));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0f, 45.0f));
                route.Add(new Vector3(-70.5f, 0f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-1.5f, 0f, NORTH_ROAD_EASTBOUND_Z));
                // Merkez Bulvar Kuzey Şeridi (X: +1.5)
                route.Add(new Vector3(1.5f, 0f, 51.5f));
                route.Add(new Vector3(1.5f, 0f, 170.0f));
                // Üst Çevre Yolu Doğu Şeridi (Z: 173.5)
                route.Add(new Vector3(3.0f, 0f, 173.5f));
                route.Add(new Vector3(34.5f, 0f, 173.5f));
                // 2. Cadde Güney Şeridi (X: 36.0)
                route.Add(new Vector3(36.0f, 0f, 170.0f));
                route.Add(new Vector3(36.0f, 0f, 51.5f));
                // Kuzey Yol Doğu Şeridi
                route.Add(new Vector3(39.0f, 0f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(70.5f, 0f, NORTH_ROAD_EASTBOUND_Z));
                // Doğu Yolu Güney Şeridi
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0f, 45.0f));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0f, -7.5f));
                route.Add(new Vector3(76.5f, 0f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0f, MAIN_ROAD_EASTBOUND_Z));
            }

            return route;
        }

        // 5. BATI BÖLGESİ ROTASI (Köprüden Geçiş, Lüks Villalar ve Ana Otoyol)
        private List<Vector3> GetWestDistrictRoute(int pattern)
        {
            List<Vector3> route = new List<Vector3>();

            if (pattern == 0)
            {
                // Sol Uçtan (-340f) Gel -> 4. Cadde -> Kuzey Çevre Yolu -> 1. Cadde -> Köprüden Doğuya Git (180f)
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-224.0f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                // 4. Cadde Kuzey Şeridi (X: -224.5m)
                route.Add(new Vector3(-224.5f, 0.05f, -3.0f));
                route.Add(new Vector3(-224.5f, 0.05f, 172.0f));
                // Kuzey Çevre Yolu Doğu Şeridi (Z: 173.5m)
                route.Add(new Vector3(-222.0f, 0.05f, 173.5f));
                route.Add(new Vector3(-113.5f, 0.05f, 173.5f));
                // 1. Cadde Güney Şeridi (X: -113.5m)
                route.Add(new Vector3(-113.5f, 0.05f, 172.0f));
                route.Add(new Vector3(-113.5f, 0.05f, -6.0f));
                // Otoyola Çıkış (Doğu Şeridi Z: -10.5m - Köprü Tırmanışı)
                route.Add(new Vector3(-108.0f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-102.0f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-95.0f, 1.65f, MAIN_ROAD_EASTBOUND_Z)); // Köprü Zirvesi
                route.Add(new Vector3(-88.0f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-82.0f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            }
            else if (pattern == 1)
            {
                // Doğu Otoyolundan gel (180f) -> Köprüyü Geç -> 2. Cadde Güney Şeridi (X: -151.5) -> Alt Çevre (Z: -126.5) -> 4. Cadde -> Batı Uçta Yok Ol (-340f)
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_WESTBOUND_Z)); // Köprü Zirvesi
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-148.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                // 2. Cadde Güney Şeridi (X: -151.5m - Banka / Arcade / Stadyum Yanı)
                route.Add(new Vector3(-151.5f, 0.05f, -12.0f));
                route.Add(new Vector3(-151.5f, 0.05f, -50.0f));
                route.Add(new Vector3(-151.5f, 0.05f, -122.0f));
                // Alt Çevre Yolu Batı Şeridi (Z: -123.5m)
                route.Add(new Vector3(-154.0f, 0.05f, -123.5f));
                route.Add(new Vector3(-224.5f, 0.05f, -123.5f));
                // 4. Cadde Kuzey Şeridi (X: -224.5m)
                route.Add(new Vector3(-224.5f, 0.05f, -120.0f));
                route.Add(new Vector3(-224.5f, 0.05f, -12.0f));
                // Batı Otoyoluna Çıkış ve Batı Uçta Yok Olma (-340f)
                route.Add(new Vector3(-228.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-340.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
            }
            else if (pattern == 2)
            {
                // Sol Uçtan (-340f) Gel -> 3. Cadde Kuzey (X: -186.5) -> Ara Sokak -> 2. Cadde Güney -> Sol Uçta Yok Ol (-340f)
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-186.5f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                // 3. Cadde Kuzey Şeridi (X: -186.5m - Villalar Yanı)
                route.Add(new Vector3(-186.5f, 0.05f, -3.0f));
                route.Add(new Vector3(-186.5f, 0.05f, 55.0f));
                // Ara Yol Doğu Şeridi (Z: 53.5m)
                route.Add(new Vector3(-184.0f, 0.05f, 53.5f));
                route.Add(new Vector3(-151.5f, 0.05f, 53.5f));
                // 2. Cadde Güney Şeridi (X: -151.5m)
                route.Add(new Vector3(-151.5f, 0.05f, 50.0f));
                route.Add(new Vector3(-151.5f, 0.05f, -6.0f));
                // Batı Otoyoluna Çıkış ve Batı Uçta Yok Olma (-340f)
                route.Add(new Vector3(-154.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-228.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-340.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
            }
            else
            {
                // Tam Batı Çevre Turu:
                // Köprü -> 1. Cadde (X: -110.5) -> Kuzey Çevre (Z: 176.5) -> 4. Cadde (X: -227.5) -> Güney Çevre (Z: -126.5) -> 1. Cadde (X: -110.5) -> Köprü
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                // 1. Cadde Kuzey Şeridi (X: -110.5m)
                route.Add(new Vector3(-110.5f, 0.05f, -3.0f));
                route.Add(new Vector3(-110.5f, 0.05f, 172.0f));
                // Kuzey Çevre Yolu Batı Şeridi (Z: 176.5m)
                route.Add(new Vector3(-114.0f, 0.05f, 176.5f));
                route.Add(new Vector3(-224.0f, 0.05f, 176.5f));
                // 4. Cadde Güney Şeridi (X: -227.5m)
                route.Add(new Vector3(-227.5f, 0.05f, 172.0f));
                route.Add(new Vector3(-227.5f, 0.05f, -122.0f));
                // Güney Çevre Yolu Doğu Şeridi (Z: -126.5m)
                route.Add(new Vector3(-224.0f, 0.05f, -126.5f));
                route.Add(new Vector3(-113.5f, 0.05f, -126.5f));
                // 1. Cadde Kuzey Şeridi (X: -110.5m)
                route.Add(new Vector3(-110.5f, 0.05f, -122.0f));
                route.Add(new Vector3(-110.5f, 0.05f, -12.0f));
                // Otoyol Çıkış (Doğu Şeridi Z: -10.5m)
                route.Add(new Vector3(-108.0f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-102.0f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-95.0f, 1.65f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-88.0f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-82.0f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            }

            return route;
        }

        // 6. GÜNEY CAMİ VE KAFELER MAHALLESİ ROTASI (Büyük Cami, Kafeler Bulvarı ve Çevre Turları)
        private List<Vector3> GetSouthDistrictRoute(int pattern)
        {
            List<Vector3> route = new List<Vector3>();

            if (pattern == 0)
            {
                // Pattern 0: Batıdan Gel -> Köprü -> Kasaba Yolu -> Orta Bulvar (Cami ve Kafe Arasından Güneye İniş) -> Güney Dış Yol -> Doğu Kafe Caddesi -> Otoyol
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-76.5f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                // Batı Yolu Güney Şeridi (X: -76.5m)
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, -12.0f));
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, -54.0f));
                // Kasaba Alt Yolu Doğu Şeridi (Z: -56.5m)
                route.Add(new Vector3(-73.5f, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-4.5f, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                // Orta Bulvar Güney Şeridi (X: -1.5m - Caminin ve Kafelerin Arasından İniş)
                route.Add(new Vector3(SOUTH_MID_AVENUE_SOUTHBOUND_X, 0.05f, -59.0f));
                route.Add(new Vector3(SOUTH_MID_AVENUE_SOUTHBOUND_X, 0.05f, -125.0f));
                // Güney Dış Çevre Yolu Doğu Şeridi (Z: -129.5m)
                route.Add(new Vector3(1.5f, 0.05f, SOUTH_OUTER_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(72.0f, 0.05f, SOUTH_OUTER_ROAD_EASTBOUND_Z));
                // Doğu Caddesi Kuzey Şeridi (X: 76.5m - 3 Kafenin Önünden Geçiş)
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -125.0f));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -59.0f));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -12.0f));
                // Doğu Otoyoluna Çıkış (Z: -10.5m)
                route.Add(new Vector3(78.5f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            }
            else if (pattern == 1)
            {
                // Pattern 1: Doğudan Gel -> Doğu Caddesi (Kafelerin Önünden Güneye İniş) -> Güney Dış Yol -> Batı Caddesi (Cami Batısından Çıkış) -> Köprü -> Batı Otoyolu
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(76.5f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                // Doğu Caddesi Güney Şeridi (X: 73.5m - Kafelerin Önünden Geçiş)
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -12.0f));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -59.0f));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -125.0f));
                // Güney Dış Çevre Yolu Batı Şeridi (Z: -126.5m - Caminin Güney Sınırı)
                route.Add(new Vector3(71.0f, 0.05f, SOUTH_OUTER_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-71.0f, 0.05f, SOUTH_OUTER_ROAD_WESTBOUND_Z));
                // Batı Caddesi Kuzey Şeridi (X: -73.5m - Cami Batısından Kuzeye Çıkış)
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, -125.0f));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, -59.0f));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, -12.0f));
                // Ana Otoyol Batı Şeridine Çıkış (Z: -7.5m - Köprü Tırmanışı)
                route.Add(new Vector3(-79.5f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_WESTBOUND_Z)); // Köprü Zirvesi
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
            }
            else if (pattern == 2)
            {
                // Pattern 2: Doğudan Gel -> Kasaba Yolu -> Batı Caddesi (Güneye İniş) -> Güney Dış Yol -> Orta Bulvar (Kuzeye Çıkış) -> Kasaba Yolu -> Doğu Otoyolu
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(76.5f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -12.0f));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -54.0f));
                // Kasaba Alt Yolu Batı Şeridi (Z: -53.5m)
                route.Add(new Vector3(70.5f, 0.05f, SOUTH_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-71.0f, 0.05f, SOUTH_ROAD_WESTBOUND_Z));
                // Batı Caddesi Güney Şeridi (X: -76.5m - Cami Batısından İniş)
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, -59.0f));
                route.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, -125.0f));
                // Güney Dış Çevre Yolu Doğu Şeridi (Z: -129.5m)
                route.Add(new Vector3(-72.0f, 0.05f, SOUTH_OUTER_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-3.0f, 0.05f, SOUTH_OUTER_ROAD_EASTBOUND_Z));
                // Orta Bulvar Kuzey Şeridi (X: +1.5m - Cami & Kafe Arasından Kuzeye Çıkış)
                route.Add(new Vector3(SOUTH_MID_AVENUE_NORTHBOUND_X, 0.05f, -125.0f));
                route.Add(new Vector3(SOUTH_MID_AVENUE_NORTHBOUND_X, 0.05f, -59.0f));
                // Kasaba Alt Yolu Doğu Şeridi (Z: -56.5m)
                route.Add(new Vector3(4.0f, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(73.5f, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                // Doğu Yolu Kuzey Şeridi (X: 76.5m)
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -54.0f));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -12.0f));
                route.Add(new Vector3(78.5f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            }
            else
            {
                // Pattern 3: Büyük Tüm Şehir Turu (Batı Villalar -> Köprü -> Kuzey Apartmanlar -> Merkez Bulvar -> Kasaba -> Güney Cami & Kafe Bulvarı -> Otoyol)
                route.Add(new Vector3(-340f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-108f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-102f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-95f, 1.65f, MAIN_ROAD_EASTBOUND_Z)); // Köprü Zirvesi
                route.Add(new Vector3(-88f, 0.95f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-82f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-76.5f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                // Batı Yolu Kuzey Şeridi ile Kuzey Mahallesine Çıkış (X: -73.5m)
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, -7.5f));
                route.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, 45.0f));
                route.Add(new Vector3(-70.5f, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(-39.0f, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                // Kuzey 1. Cadde (X: -36.0m)
                route.Add(new Vector3(-36.0f, 0.05f, 51.5f));
                route.Add(new Vector3(-36.0f, 0.05f, 170.0f));
                // Üst Çevre Yolu (Z: 173.5m)
                route.Add(new Vector3(-34.5f, 0.05f, 173.5f));
                route.Add(new Vector3(-3.0f, 0.05f, 173.5f));
                // Kuzey Merkez Bulvarı Güney Şeridi (X: -1.5m)
                route.Add(new Vector3(-1.5f, 0.05f, 170.0f));
                route.Add(new Vector3(-1.5f, 0.05f, 51.5f));
                // Alt Kuzey Yolu Doğu Şeridi (Z: 48.5m) ile Doğu Caddesine Bağlantı
                route.Add(new Vector3(0.0f, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(70.5f, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                // Doğu Caddesi Güney Şeridi (X: 73.5m - Kasaba Yanından Güneye İniş)
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, 45.0f));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -12.0f));
                route.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -54.0f));
                // Kasaba Alt Yolu Batı Şeridi (Z: -53.5m) ile Güney Orta Bulvarına Giriş
                route.Add(new Vector3(70.5f, 0.05f, SOUTH_ROAD_WESTBOUND_Z));
                route.Add(new Vector3(-4.5f, 0.05f, SOUTH_ROAD_WESTBOUND_Z));
                // Güney Bölgesi Orta Bulvarı Güney Şeridi (X: -1.5m - Cami ve Kafe Arası İniş)
                route.Add(new Vector3(SOUTH_MID_AVENUE_SOUTHBOUND_X, 0.05f, -59.0f));
                route.Add(new Vector3(SOUTH_MID_AVENUE_SOUTHBOUND_X, 0.05f, -125.0f));
                // Güney Dış Çevre Yolu Doğu Şeridi (Z: -129.5m)
                route.Add(new Vector3(1.5f, 0.05f, SOUTH_OUTER_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(72.0f, 0.05f, SOUTH_OUTER_ROAD_EASTBOUND_Z));
                // Doğu Caddesi Kuzey Şeridi (X: 76.5m - 3 Kafenin Önünden Çıkış)
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -125.0f));
                route.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -12.0f));
                // Doğu Otoyolu Çıkışı
                route.Add(new Vector3(78.5f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                route.Add(new Vector3(180f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
            }

            return route;
        }

        #endregion

        #region Bridge Elevation & Slope Profile

        /// <summary>
        /// Köprü üzerindeki herhangi bir X, Z noktasındaki pürüzsüz tabliye yüksekliğini ve eğim türevini (slope) döndürür.
        /// Tüm araçlar, kamyonlar ve teslimat arabaları bu metot sayesinde köprü kavisini milimetrik takip eder,
        /// asla köprüye gömülmez, yokuş çıkış ve iniş açılarına (pitch) tam uyum sağlar.
        /// </summary>
        public static float GetBridgeElevation(float x, float z, out float slopeY)
        {
            slopeY = 0f;
            if (x >= -110.0f && x <= -80.0f && Mathf.Abs(z - (-9.0f)) <= 4.5f)
            {
                float t = Mathf.Clamp01((x - (-109.0f)) / 28.0f);
                float archHeight = 1.65f;
                // Parabolik köprü tabliyesi yüksekliği (Deck surface = midY + 0.15f)
                float archY = 0.05f + (4f * archHeight * t * (1f - t)) + 0.15f;

                // Eğim türevi (dy/dx)
                slopeY = (4f * archHeight * (1f - 2f * t)) / 28.0f;

                // Köprü giriş ve çıkışında yer seviyesine (0.05f) yumuşak geçiş
                if (x < -107.0f)
                {
                    float blend = Mathf.Clamp01((x - (-110.0f)) / 3.0f);
                    archY = Mathf.Lerp(0.05f, archY, blend);
                    slopeY *= blend;
                }
                else if (x > -83.0f)
                {
                    float blend = Mathf.Clamp01((-80.0f - x) / 3.0f);
                    archY = Mathf.Lerp(0.05f, archY, blend);
                    slopeY *= blend;
                }

                return archY;
            }

            return 0.05f;
        }

        #endregion
    }
}
