using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Yaya geçidinin sağ çaprazındaki Otobüs Durağı tabelasının olduğu noktaya
    /// oyun saatine göre HER 2 SAATTE BİR (Örn: 08:00, 10:00, 12:00, 14:00, 16:00, 18:00, 20:00)
    /// Şehir İçi Otobüsün yanaşmasını, yolcu indirmesini ve yoluna devam etmesini yöneten sınıf.
    /// </summary>
    public class CityBusManager : MonoBehaviour
    {
        public static CityBusManager Instance { get; private set; }

        [Header("Otobüs Durağı ve Şerit Ayarları")]
        private Vector3 busStopPos = new Vector3(4.5f, 0.05f, -7.5f); // Yaya geçidinin sağ çaprazı yoldaki durma noktası
        private Vector3 spawnPos = new Vector3(40.0f, 0.05f, -7.5f); // Batı yönlü geliş noktası
        private Vector3 despawnPos = new Vector3(-45.0f, 0.05f, -7.5f); // Batı çıkış noktası

        private int lastArrivalHour = -1;
        private bool isBusActive = false;
        private GameObject currentBusObj;
        private List<Transform> busWheels;
        private GameObject frontDoor;
        private GameObject rearDoor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (TimeManager.Instance == null || StoreStatusManager.Instance == null) return;
            if (!StoreStatusManager.Instance.IsOpen) return;

            int currentHour = TimeManager.Instance.Hour;

            // Sabah 08:00 ile Akşam 22:00 (10:00 PM) arasında Her 2 saatte bir otobüs gelir!
            if (currentHour >= 8 && currentHour <= 22 && currentHour % 2 == 0 && currentHour != lastArrivalHour && !isBusActive)
            {
                lastArrivalHour = currentHour;
                StartCoroutine(BusServiceRoutine());
            }
        }

        private IEnumerator BusServiceRoutine()
        {
            isBusActive = true;

            // 1. 3D Otobüs Modelini Üret
            currentBusObj = ProceduralCityBusBuilder.CreateCityBusModel(out busWheels, out frontDoor, out rearDoor);
            currentBusObj.transform.position = spawnPos;
            currentBusObj.transform.rotation = Quaternion.Euler(0f, 270f, 0f); // Batı yönü

            float driveSpeed = 10.0f;
            float currentSpeed = driveSpeed;

            // 2. Durağa Kadar Yürü / Sür (X: 40 -> 4.5)
            while (currentBusObj != null && currentBusObj.transform.position.x > busStopPos.x)
            {
                float distToStop = currentBusObj.transform.position.x - busStopPos.x;
                currentSpeed = Mathf.Clamp(distToStop * 2.5f, 1.2f, driveSpeed);

                currentBusObj.transform.position = Vector3.MoveTowards(currentBusObj.transform.position, busStopPos, currentSpeed * Time.deltaTime);
                AnimateBusWheels(currentSpeed);

                yield return null;
            }

            if (currentBusObj != null) currentBusObj.transform.position = busStopPos;

            // 3. Durağa Yanaşıldı: Kapıları Aç ve Yolcuları İndir!
            if (frontDoor != null) frontDoor.transform.localPosition = new Vector3(1.16f, 1.3f, 3.2f);
            if (rearDoor != null) rearDoor.transform.localPosition = new Vector3(1.16f, 1.3f, -0.6f);

            SpawnFloatingBusPopup(new Vector3(4.5f, 3.2f, -5.8f), "🚏 Otobüs Durağa Yanaştı (+Yolcular İndiriliyor)");

            // 4. Saat Bazlı Gerçekçi Müşteri Yoğunluğuna Göre Yolcuları Teker Teker İndir
            int currentHour = (TimeManager.Instance != null) ? TimeManager.Instance.Hour : 18;
            int passengerCount = GetBusPassengerCountForHour(currentHour);

            if (CustomerShoppingManager.Instance != null)
            {
                for (int p = 0; p < passengerCount; p++)
                {
                    Vector3 disembarkPos = new Vector3(4.5f + (p * 0.8f), 0.05f, -5.8f);
                    CustomerShoppingManager.Instance.SpawnSingleBusPassenger(disembarkPos);
                    yield return new WaitForSeconds(0.65f); // Yolcular 0.65 saniye aralıklarla insin!
                }
            }

            // Kapıların kapanması için kısa bekleme
            yield return new WaitForSeconds(2.0f);

            // 5. Kapıları Kapat ve Yoluna Devam Et!
            if (frontDoor != null) frontDoor.transform.localPosition = new Vector3(1.16f, 1.3f, 2.6f);
            if (rearDoor != null) rearDoor.transform.localPosition = new Vector3(1.16f, 1.3f, -1.2f);

            yield return new WaitForSeconds(0.5f);

            // 6. Çıkış Noktasına Kadar Sür (X: 4.5 -> -45.0)
            while (currentBusObj != null && currentBusObj.transform.position.x > despawnPos.x)
            {
                float distFromStop = busStopPos.x - currentBusObj.transform.position.x;
                currentSpeed = Mathf.Clamp(distFromStop * 3.0f, 1.5f, driveSpeed);

                currentBusObj.transform.position = Vector3.MoveTowards(currentBusObj.transform.position, despawnPos, currentSpeed * Time.deltaTime);
                AnimateBusWheels(currentSpeed);

                yield return null;
            }

            if (currentBusObj != null)
            {
                Destroy(currentBusObj);
                currentBusObj = null;
            }

            isBusActive = false;
        }

        private void AnimateBusWheels(float speed)
        {
            if (busWheels == null) return;
            float rotAngle = speed * Time.deltaTime * 180f;
            foreach (var w in busWheels)
            {
                if (w != null) w.Rotate(Vector3.right * rotAngle, Space.Self);
            }
        }

        private void SpawnFloatingBusPopup(Vector3 worldPos, string text)
        {
            GameObject popupObj = new GameObject("Popup_BusArrival");
            popupObj.transform.position = worldPos;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 80;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 70f);
            popupObj.transform.localScale = Vector3.one * 0.015f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
            txt.text = text;
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.95f, 0.85f, 0.15f);

            Destroy(popupObj, 2.5f);
        }

        private int GetBusPassengerCountForHour(int hour)
        {
            // Saat bazlı gerçekçi otobüs yolcusu yoğunluk eğrisi
            if (hour == 8) return Random.Range(4, 6);       // Sabah İşe/Okula Gidiş
            if (hour == 10) return Random.Range(2, 4);      // Kuşluk Vakti
            if (hour == 12) return Random.Range(4, 6);      // Öğle Molası Alışverişi
            if (hour == 14) return Random.Range(2, 4);      // Öğleden Sonra Normal
            if (hour == 16) return Random.Range(3, 5);      // Erken İş Çıkışı
            if (hour == 18) return Random.Range(5, 7);      // 🔥 ZİRVE MESAYİ ÇIKIŞI (RUSH HOUR)
            if (hour == 20) return Random.Range(4, 6);      // Akşam İş Çıkışı Devamı
            if (hour == 22) return Random.Range(1, 3);      // Gece Kapanış Otobüsü
            return Random.Range(2, 4);
        }
    }
}
