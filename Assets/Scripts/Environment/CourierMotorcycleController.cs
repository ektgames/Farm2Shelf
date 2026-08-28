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

        private Transform[] wheels;
        private Light headlight;
        private float wheelSpinSpeed = 650f;
        private float driveSpeed = 10.5f; // Arabalarla aynı hızda (10.5 m/s)

        // Yol Koordinat Sabitleri (Trafik ve Teslimat Yolu Şeritleri - CityTrafficManager ile %100 Birebir Uyumlu)
        private const float LANE_OFFSET = 1.5f;                 // 6m genişliğindeki çift yönlü yolun tam şerit merkezi (1.5m)
        private const float SHOP_LANE_X = 13.0f;               // Dükkan yanı motor & teslimat yolu merkezi
        private const float SHOP_LANE_NORTHBOUND_X = 13.5f;    // Dükkan yanı kuzeye gidiş şeridi
        private const float SHOP_LANE_SOUTHBOUND_X = 12.5f;    // Dükkan yanı güneye iniş şeridi

        private const float MAIN_ROAD_Z = -9.0f;               // Orta ana otoyol merkezi
        private const float MAIN_ROAD_EASTBOUND_Z = -10.5f;    // Otoyol Doğuya gidiş şeridi (+X)
        private const float MAIN_ROAD_WESTBOUND_Z = -7.5f;     // Otoyol Batıya gidiş şeridi (-X)

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

            if (headlight != null) headlight.enabled = false;
        }

        private void Update()
        {
            // Gece far kontrolü
            if (headlight != null && TimeManager.Instance != null)
            {
                int h = TimeManager.Instance.CurrentHour;
                bool isNight = (h >= 20 || h < 7);
                headlight.enabled = isNight && (CurrentState == MotorcycleState.EnRouteDelivery || CurrentState == MotorcycleState.ReturningToStore || CurrentState == MotorcycleState.DeliveringAtDoorstep);
            }
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
            if (rider != null && DriverSeatMount != null)
            {
                rider.transform.SetParent(DriverSeatMount, false);
                rider.transform.localPosition = Vector3.zero;
                rider.transform.localRotation = Quaternion.identity;

                // Sürüş Oturma Pozu: Bacaklar pedallarda, kollar gidonda
                Transform legL = rider.transform.Find("Leg_L");
                Transform legR = rider.transform.Find("Leg_R");
                Transform armL = rider.transform.Find("Arm_L");
                Transform armR = rider.transform.Find("Arm_R");

                if (legL != null) legL.localRotation = Quaternion.Euler(-55f, 15f, 0f);
                if (legR != null) legR.localRotation = Quaternion.Euler(-55f, -15f, 0f);
                if (armL != null) armL.localRotation = Quaternion.Euler(-45f, 10f, 0f);
                if (armR != null) armR.localRotation = Quaternion.Euler(-45f, -10f, 0f);
            }
        }

        public void UnmountRider()
        {
            if (CourierRiderObj != null)
            {
                Transform legL = CourierRiderObj.transform.Find("Leg_L");
                Transform legR = CourierRiderObj.transform.Find("Leg_R");
                Transform armL = CourierRiderObj.transform.Find("Arm_L");
                Transform armR = CourierRiderObj.transform.Find("Arm_R");

                if (legL != null) legL.localRotation = Quaternion.identity;
                if (legR != null) legR.localRotation = Quaternion.identity;
                if (armL != null) armL.localRotation = Quaternion.identity;
                if (armR != null) armR.localRotation = Quaternion.identity;

                CourierRiderObj.transform.SetParent(null);
                CourierRiderObj = null;
            }
        }

        public void DismountRider() => UnmountRider();

        public bool CanTakeOrders()
        {
            return (CurrentState == MotorcycleState.ParkedInBay || CurrentState == MotorcycleState.WaitingForStocker) && AssignedCourier != null && LoadedOrders.Count < 2;
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
            if (LoadedOrders.Count == 0) return;
            StopAllCoroutines();
            StartCoroutine(DeliveryTripRoutine());
        }

        public void StartDeliveryRoute() => DispatchDeliveryTrip();

        private IEnumerator DeliveryTripRoutine()
        {
            CurrentState = MotorcycleState.EnRouteDelivery;

            Vector3 currentPos = HomeParkPosition;

            // 1. YÜKLENEN HER BİR SİPARİŞ İÇİN HEDEF ADRESE SADECE VE SADECE YOLLAR ÜZERİNDEN SÜRÜŞ
            for (int i = 0; i < LoadedOrders.Count; i++)
            {
                var order = LoadedOrders[i];
                if (order == null) continue;

                Vector3 targetDoorstep = order.targetDoorstepPosition;
                List<Vector3> routeWaypoints = BuildLegWaypoints(currentPos, targetDoorstep);

                for (int w = 0; w < routeWaypoints.Count; w++)
                {
                    yield return MoveToPosition(routeWaypoints[w], driveSpeed);
                }

                currentPos = routeWaypoints.Count > 0 ? routeWaypoints[routeWaypoints.Count - 1] : targetDoorstep;

                // 2. BİNA ÖNÜNDE VE YOL KENARINDA TESLİMAT BEKLEMESİ (3.0 Saniye)
                CurrentState = MotorcycleState.DeliveringAtDoorstep;
                yield return new WaitForSeconds(3.0f);

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
                yield return MoveToPosition(returnWaypoints[r], driveSpeed);
            }

            // Kendi sarı park yuvasına tam yanaş ve park rotasyonuna otur
            yield return MoveToPosition(HomeParkPosition, 4.0f);

            while (Quaternion.Angle(transform.rotation, HomeParkRotation) > 2f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, HomeParkRotation, 12f * Time.deltaTime);
                yield return null;
            }
            transform.rotation = HomeParkRotation;

            CurrentState = MotorcycleState.ParkedInBay;

            if (CourierManager.Instance != null)
            {
                CourierManager.Instance.OnMotorcycleReturnedToBay(this);
            }
        }

        private IEnumerator MoveToPosition(Vector3 targetPos, float speed)
        {
            Vector3 targetFlat = new Vector3(targetPos.x, 0f, targetPos.z);

            while (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), targetFlat) > 0.15f)
            {
                Vector3 currentFlat = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 dir = (targetFlat - currentFlat).normalized;

                if (dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 14f * Time.deltaTime);
                }

                // Şehir trafiğinde öndeki araca göre güvenli hız sınırlaması
                float currentSpeed = speed;
                if (CityTrafficManager.Instance != null && dir != Vector3.zero)
                {
                    currentSpeed = CityTrafficManager.Instance.GetSpeedLimitInFront(transform.position, dir, 10.0f, speed);
                }

                Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);

                // Köprüdeyse yükselme/eğim hesapla
                float slopeY;
                float bridgeY = CityTrafficManager.GetBridgeElevation(nextPos.x, nextPos.z, out slopeY);
                nextPos.y = bridgeY;

                transform.position = nextPos;

                // Tekerlekleri döndür
                if (wheels != null)
                {
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        if (wheels[i] != null)
                        {
                            wheels[i].Rotate(Vector3.right, wheelSpinSpeed * Time.deltaTime, Space.Self);
                        }
                    }
                }

                yield return null;
            }

            float finalSlopeY;
            float finalY = CityTrafficManager.GetBridgeElevation(targetPos.x, targetPos.z, out finalSlopeY);
            transform.position = new Vector3(targetPos.x, finalY, targetPos.z);
        }

        #region Strict Road Graph Router (100% On-Road Navigation)

        private static float GetClosestNorthAvenueX(float x)
        {
            float[] aves = new float[] { -75.0f, -37.5f, 0.0f, 37.5f, 75.0f };
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
            List<Vector3> rawPoints = new List<Vector3>();

            bool isStartAtShop = Mathf.Abs(start.x - SHOP_LANE_X) < 2.0f && start.z >= -2.0f && start.z <= 25.0f;
            bool isDestNorth = dest.z >= 45.0f;
            bool isDestWest = dest.x <= -95.0f;
            bool isDestSouthDistrict = dest.z <= -58.0f;
            bool isDestSouthKasabaPerimeter = dest.z > -58.0f && dest.z <= -45.0f;

            bool isStartNorth = start.z >= 45.0f;
            bool isStartWest = start.x <= -95.0f;
            bool isStartSouthDistrict = start.z <= -58.0f;
            bool isStartSouthKasabaPerimeter = start.z > -58.0f && start.z <= -45.0f;

            if (isStartAtShop)
            {
                if (isDestNorth)
                {
                    // 1. Dükkandan Kuzey Mahallesine Gidiş:
                    // Dükkan teslimat yolu kuzey şeridine çık
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, HomeParkPosition.z));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, NORTH_ROAD_EASTBOUND_Z));

                    float aveX = GetClosestNorthAvenueX(dest.x);

                    if (aveX < SHOP_LANE_X)
                    {
                        // Batıya gidiş sağ şeridi (Z: 51.5)
                        rawPoints.Add(new Vector3(13.0f, 0.05f, NORTH_ROAD_WESTBOUND_Z));
                        rawPoints.Add(new Vector3(aveX + LANE_OFFSET, 0.05f, NORTH_ROAD_WESTBOUND_Z));
                    }
                    else
                    {
                        // Doğuya gidiş sağ şeridi (Z: 48.5)
                        rawPoints.Add(new Vector3(13.0f, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                        rawPoints.Add(new Vector3(aveX - LANE_OFFSET, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                    }

                    // Cadde üzerinde Kuzeye gidiş sağ şeridi (X = aveX + 1.5)
                    rawPoints.Add(new Vector3(aveX + LANE_OFFSET, 0.05f, 53.0f));
                    rawPoints.Add(new Vector3(aveX + LANE_OFFSET, 0.05f, dest.z));
                }
                else
                {
                    // 2. Dükkandan Güneye / Batıya / Kasabaya Çıkış:
                    rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, HomeParkPosition.z));

                    if (isDestWest)
                    {
                        // Ana yola çıkış ve Batıya gidiş sağ şeridi (Z: -7.5)
                        rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                        rawPoints.Add(new Vector3(12.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                        rawPoints.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));

                        // Köprüden Batıya geçiş
                        AddBridgeWaypoints(rawPoints, false);

                        float westAveX = GetClosestWestAvenueX(dest.x);

                        if (dest.z > MAIN_ROAD_Z)
                        {
                            // Villalara Kuzeye gidiş sağ şeridi (X = westAveX + 1.5)
                            rawPoints.Add(new Vector3(westAveX + LANE_OFFSET, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(westAveX + LANE_OFFSET, 0.05f, -6.0f));
                            rawPoints.Add(new Vector3(westAveX + LANE_OFFSET, 0.05f, dest.z));
                        }
                        else
                        {
                            // Kamu binalarına Güneye iniş sağ şeridi (X = westAveX - 1.5)
                            rawPoints.Add(new Vector3(westAveX - LANE_OFFSET, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(westAveX - LANE_OFFSET, 0.05f, -12.0f));
                            rawPoints.Add(new Vector3(westAveX - LANE_OFFSET, 0.05f, dest.z));
                        }
                    }
                    else if (isDestSouthDistrict)
                    {
                        if (dest.x >= 25.0f)
                        {
                            // Doğu Kafe Caddesi (X = 75.0m)
                            rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(14.0f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -12.0f));
                            rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -54.0f));
                            rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, dest.z));
                        }
                        else
                        {
                            // Cami & Şadırvan (X = 0.0m)
                            rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(12.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, -12.0f));
                            rawPoints.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, -53.5f));
                            rawPoints.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(SOUTH_MID_AVENUE_SOUTHBOUND_X, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(SOUTH_MID_AVENUE_SOUTHBOUND_X, 0.05f, -60.0f));
                            rawPoints.Add(new Vector3(SOUTH_MID_AVENUE_SOUTHBOUND_X, 0.05f, dest.z));
                        }
                    }
                    else if (isDestSouthKasabaPerimeter)
                    {
                        if (dest.x < 0)
                        {
                            rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(12.0f, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, -12.0f));
                            rawPoints.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, -53.5f));
                            rawPoints.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(dest.x, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                        }
                        else
                        {
                            rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(14.0f, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -12.0f));
                            rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, -54.0f));
                            rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, SOUTH_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(dest.x, 0.05f, SOUTH_ROAD_WESTBOUND_Z));
                        }
                    }
                    else
                    {
                        // Otoyol kenarı konutlar & Belediye
                        if (dest.x < SHOP_LANE_X)
                        {
                            rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                            rawPoints.Add(new Vector3(dest.x, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                        }
                        else
                        {
                            rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                            rawPoints.Add(new Vector3(dest.x, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                        }
                    }
                }
            }
            else
            {
                // Bir teslimat adresinden diğerine geçiş (Çoklu sipariş aktarımı - Sadece ve sadece yollar)
                if (isStartNorth)
                {
                    float startAveX = GetClosestNorthAvenueX(start.x);
                    rawPoints.Add(new Vector3(startAveX - LANE_OFFSET, 0.05f, start.z));
                    rawPoints.Add(new Vector3(startAveX - LANE_OFFSET, 0.05f, 53.0f));
                    rawPoints.Add(new Vector3(startAveX - LANE_OFFSET, 0.05f, NORTH_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, NORTH_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, 45.0f));
                    rawPoints.Add(new Vector3(WEST_ROAD_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                }
                else if (isStartWest)
                {
                    float startWestAveX = GetClosestWestAvenueX(start.x);
                    if (start.z > MAIN_ROAD_Z)
                    {
                        rawPoints.Add(new Vector3(startWestAveX - LANE_OFFSET, 0.05f, start.z));
                        rawPoints.Add(new Vector3(startWestAveX - LANE_OFFSET, 0.05f, -6.0f));
                    }
                    else
                    {
                        rawPoints.Add(new Vector3(startWestAveX + LANE_OFFSET, 0.05f, start.z));
                        rawPoints.Add(new Vector3(startWestAveX + LANE_OFFSET, 0.05f, -12.0f));
                    }
                    rawPoints.Add(new Vector3(startWestAveX, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                    AddBridgeWaypoints(rawPoints, true);
                }
                else if (isStartSouthDistrict || isStartSouthKasabaPerimeter)
                {
                    if (start.x >= 25.0f)
                    {
                        rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, start.z));
                        rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -12.0f));
                        rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    }
                    else
                    {
                        rawPoints.Add(new Vector3(SOUTH_MID_AVENUE_NORTHBOUND_X, 0.05f, start.z));
                        rawPoints.Add(new Vector3(SOUTH_MID_AVENUE_NORTHBOUND_X, 0.05f, -58.0f));
                        rawPoints.Add(new Vector3(4.0f, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                        rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                        rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -53.0f));
                        rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -12.0f));
                        rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    }
                }

                // Hedef bölgeye giriş
                if (isDestNorth)
                {
                    float destAveX = GetClosestNorthAvenueX(dest.x);
                    rawPoints.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, -4.5f));
                    rawPoints.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, 45.0f));
                    rawPoints.Add(new Vector3(-70.5f, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                    rawPoints.Add(new Vector3(destAveX - LANE_OFFSET, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                    rawPoints.Add(new Vector3(destAveX + LANE_OFFSET, 0.05f, 53.0f));
                    rawPoints.Add(new Vector3(destAveX + LANE_OFFSET, 0.05f, dest.z));
                }
                else if (isDestWest)
                {
                    rawPoints.Add(new Vector3(WEST_ROAD_NORTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    AddBridgeWaypoints(rawPoints, false);

                    float destWestAveX = GetClosestWestAvenueX(dest.x);
                    if (dest.z > MAIN_ROAD_Z)
                    {
                        rawPoints.Add(new Vector3(destWestAveX + LANE_OFFSET, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                        rawPoints.Add(new Vector3(destWestAveX + LANE_OFFSET, 0.05f, -6.0f));
                        rawPoints.Add(new Vector3(destWestAveX + LANE_OFFSET, 0.05f, dest.z));
                    }
                    else
                    {
                        rawPoints.Add(new Vector3(destWestAveX - LANE_OFFSET, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                        rawPoints.Add(new Vector3(destWestAveX - LANE_OFFSET, 0.05f, -12.0f));
                        rawPoints.Add(new Vector3(destWestAveX - LANE_OFFSET, 0.05f, dest.z));
                    }
                }
                else
                {
                    rawPoints.Add(new Vector3(dest.x, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                }
            }

            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < rawPoints.Count; i++)
            {
                Vector3 p = rawPoints[i];
                if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], p) > 0.35f)
                {
                    points.Add(p);
                }
            }

            return points;
        }

        /// <summary>
        /// Teslimat bittiğinde dükkandaki sarı park yuvasına SADECE VE SADECE asfalt yollar ve SAĞDAN AKAN TRAFİK üzerinden geri dönüş rotası üretir.
        /// </summary>
        private List<Vector3> BuildReturnWaypoints(Vector3 start)
        {
            List<Vector3> rawPoints = new List<Vector3>();

            bool isNorth = start.z >= 45.0f;
            bool isWest = start.x <= -95.0f;
            bool isSouthDistrict = start.z <= -58.0f;
            bool isSouthKasabaPerimeter = start.z > -58.0f && start.z <= -45.0f;

            if (isNorth)
            {
                // Kuzey mahalleden dönüş: Cadde üzerinde Güneye geliş sağ şeridi (X = aveX - 1.5)
                float aveX = GetClosestNorthAvenueX(start.x);
                rawPoints.Add(new Vector3(aveX - LANE_OFFSET, 0.05f, start.z));
                rawPoints.Add(new Vector3(aveX - LANE_OFFSET, 0.05f, 53.0f));

                if (aveX < SHOP_LANE_X)
                {
                    // Doğuya gidiş sağ şeridi (Z: 48.5)
                    rawPoints.Add(new Vector3(aveX - LANE_OFFSET, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, NORTH_ROAD_EASTBOUND_Z));
                }
                else
                {
                    // Batıya gidiş sağ şeridi (Z: 51.5)
                    rawPoints.Add(new Vector3(aveX - LANE_OFFSET, 0.05f, NORTH_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, NORTH_ROAD_WESTBOUND_Z));
                }

                // Dükkan teslimat yolundan Güneye iniş şeridi
                rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, 47.0f));
                rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, HomeParkPosition.z));
            }
            else if (isWest)
            {
                // Batı sahil/kamu bölgesinden dönüş:
                float westAveX = GetClosestWestAvenueX(start.x);

                if (start.z > MAIN_ROAD_Z)
                {
                    // Villalardan Güneye iniş sağ şeridi (X = westAveX - 1.5)
                    rawPoints.Add(new Vector3(westAveX - LANE_OFFSET, 0.05f, start.z));
                    rawPoints.Add(new Vector3(westAveX - LANE_OFFSET, 0.05f, -6.0f));
                    rawPoints.Add(new Vector3(westAveX - LANE_OFFSET, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                }
                else
                {
                    // Kamu binalarından Kuzeye çıkış sağ şeridi (X = westAveX + 1.5)
                    rawPoints.Add(new Vector3(westAveX + LANE_OFFSET, 0.05f, start.z));
                    rawPoints.Add(new Vector3(westAveX + LANE_OFFSET, 0.05f, -12.0f));
                    rawPoints.Add(new Vector3(westAveX + LANE_OFFSET, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                }

                // Köprüden Doğuya geçiş
                AddBridgeWaypoints(rawPoints, true);

                // Dükkan teslimat yolu sapağına yaklaşım ve Kuzeye çıkış
                rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, -6.0f));
                rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, HomeParkPosition.z));
            }
            else if (isSouthDistrict)
            {
                if (start.x >= 25.0f)
                {
                    // Doğu Kafe Caddesinden Kuzeye çıkış sağ şeridi (X: 76.5)
                    rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, start.z));
                    rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -12.0f));
                    rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, -6.0f));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, HomeParkPosition.z));
                }
                else
                {
                    // Orta Bulvardan (Cami & Kafe Arası) Kuzeye çıkış sağ şeridi (X: +1.5)
                    rawPoints.Add(new Vector3(SOUTH_MID_AVENUE_NORTHBOUND_X, 0.05f, start.z));
                    rawPoints.Add(new Vector3(SOUTH_MID_AVENUE_NORTHBOUND_X, 0.05f, -58.0f));
                    rawPoints.Add(new Vector3(4.0f, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                    rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                    rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -53.0f));
                    rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -12.0f));
                    rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, -6.0f));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, HomeParkPosition.z));
                }
            }
            else if (isSouthKasabaPerimeter)
            {
                // Kasaba alt çevre yolundan dönüş
                rawPoints.Add(new Vector3(start.x, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, SOUTH_ROAD_EASTBOUND_Z));
                rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -53.0f));
                rawPoints.Add(new Vector3(EAST_ROAD_NORTHBOUND_X, 0.05f, -12.0f));
                rawPoints.Add(new Vector3(EAST_ROAD_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, -6.0f));
                rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, HomeParkPosition.z));
            }
            else
            {
                // Otoyol kenarından dönüş
                if (start.x < SHOP_LANE_X)
                {
                    rawPoints.Add(new Vector3(start.x, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, MAIN_ROAD_EASTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, -6.0f));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, HomeParkPosition.z));
                }
                else
                {
                    rawPoints.Add(new Vector3(start.x, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_SOUTHBOUND_X, 0.05f, MAIN_ROAD_WESTBOUND_Z));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, -6.0f));
                    rawPoints.Add(new Vector3(SHOP_LANE_NORTHBOUND_X, 0.05f, HomeParkPosition.z));
                }
            }

            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < rawPoints.Count; i++)
            {
                Vector3 p = rawPoints[i];
                if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], p) > 0.35f)
                {
                    points.Add(p);
                }
            }

            return points;
        }

        #endregion
    }
}
