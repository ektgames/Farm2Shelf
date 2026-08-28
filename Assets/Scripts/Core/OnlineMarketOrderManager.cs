using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    [Serializable]
    public class OnlineCustomerOrder
    {
        public string orderId;
        public string customerName;
        public string customerHandle;
        public string destinationNameTr;
        public string destinationNameEn;
        public Vector3 targetDoorstepPosition;

        public List<WholesaleProductDef> requestedProducts = new List<WholesaleProductDef>();
        public List<int> requestedQuantities = new List<int>();
        public List<int> gatheredQuantities = new List<int>();

        public int totalEstimatedValue;
        public int courierDeliveryFee = 60;
        public bool isGatheringCompleted = false;
        public bool isAssignedToStocker = false;
        public CourierMotorcycleController assignedMotorcycle;

        public string LocalizedDestination => (LocalizationManager.Instance != null && LocalizationManager.Instance.IsEnglish)
            ? destinationNameEn
            : destinationNameTr;
    }

    /// <summary>
    /// Kasaba ve villalardan gelen online market siparişlerini üreten,
    /// reyoncu malzeme toplama kuyruğunu ve kurye teslimat başarı/ödül/tweet
    /// geri bildirimlerini yöneten merkezi sistem.
    /// </summary>
    public class OnlineMarketOrderManager : MonoBehaviour
    {
        private static OnlineMarketOrderManager instance;
        public static OnlineMarketOrderManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<OnlineMarketOrderManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("OnlineMarketOrderManager");
                        instance = go.AddComponent<OnlineMarketOrderManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Aktif Siparişler")]
        private readonly List<OnlineCustomerOrder> pendingOrders = new List<OnlineCustomerOrder>();
        public List<OnlineCustomerOrder> PendingOrders => pendingOrders;

        public event Action OnOrdersChanged;

        private float orderTimer = 0f;
        private float nextOrderInterval = 25f;

        // Üst üste aynı adrese sipariş gitmesini önleyen deste (Shuffled Deck) ve geçmiş kuyruğu (History Buffer)
        private readonly List<int> destinationDeck = new List<int>();
        private readonly Queue<int> recentDestinationHistory = new Queue<int>();
        private const int MAX_RECENT_HISTORY_COUNT = 8;

        // Zengin Müşteri İsim Havuzları
        private static readonly (string name, string handle)[] femaleCustomers = new (string, string)[]
        {
            ("Selin Aydın", "@selin_aydin"),
            ("Zeynep Kaya", "@zeynep_kaya"),
            ("Elif Yıldız", "@elif_yildiz"),
            ("Ayşe Şahin", "@ayse_sahin"),
            ("Gamze Koç", "@gamze_koc"),
            ("Ece Güler", "@ece_guler"),
            ("Melis Yılmaz", "@melis_yilmaz"),
            ("Defne Aksoy", "@defne_aksoy"),
            ("Büşra Demir", "@busra_demir"),
            ("Derya Çetin", "@derya_cetin"),
            ("İrem Özkan", "@irem_ozkan"),
            ("Sude Karahan", "@sude_karahan")
        };

        private static readonly (string name, string handle)[] maleCustomers = new (string, string)[]
        {
            ("Kerem Yılmaz", "@kerem_yilmaz"),
            ("Ahmet Demir", "@ahmet_demir"),
            ("Mehmet Çelik", "@mehmet_celik"),
            ("Can Öztürk", "@can_ozturk"),
            ("Burak Arslan", "@burak_arslan"),
            ("Tolga Aydın", "@tolga_aydin"),
            ("Murat Bulut", "@murat_bulut"),
            ("Emre Koç", "@emre_koc"),
            ("Kaan Yıldırım", "@kaan_yildirim"),
            ("Oğuzhan Şen", "@oguzhan_sen"),
            ("Alp Bozkurt", "@alp_bozkurt"),
            ("Barış Eren", "@baris_eren")
        };

        // Gerçek Bina ve Yol Kenarı Teslimat Noktaları (Kuzey Apartmanları, Batı Sahil Villaları, Kasaba Evleri, Belediye, Kamu Merkezleri)
        private readonly (string nameTr, string nameEn, Vector3 doorstepPos)[] deliveryDestinations = new (string nameTr, string nameEn, Vector3 doorstepPos)[]
        {
            // 1. KUZEY APARTMANLARI (4 Blok x 4 Sıra)
            ("Kuzey Apartmanları A Blok No:4", "North Apts Block A Apt:4", new Vector3(-37.5f, 0.05f, 68.0f)),
            ("Kuzey Apartmanları A Blok No:12", "North Apts Block A Apt:12", new Vector3(-37.5f, 0.05f, 128.0f)),
            ("Kuzey Apartmanları B Blok No:6", "North Apts Block B Apt:6", new Vector3(0.0f, 0.05f, 68.0f)),
            ("Kuzey Apartmanları B Blok No:15", "North Apts Block B Apt:15", new Vector3(0.0f, 0.05f, 158.0f)),
            ("Kuzey Apartmanları C Blok No:8", "North Apts Block C Apt:8", new Vector3(0.0f, 0.05f, 98.0f)),
            ("Kuzey Apartmanları C Blok No:14", "North Apts Block C Apt:14", new Vector3(0.0f, 0.05f, 128.0f)),
            ("Kuzey Apartmanları D Blok No:2", "North Apts Block D Apt:2", new Vector3(37.5f, 0.05f, 68.0f)),
            ("Kuzey Apartmanları D Blok No:11", "North Apts Block D Apt:11", new Vector3(37.5f, 0.05f, 128.0f)),

            // 2. BATI SAHİL VİLLALARI (Ultra Lüks Bahçeli Villalar)
            ("Batı Sahil Villası #1 (Palmiye)", "West Coast Villa #1 (Palm)", new Vector3(-112.0f, 0.05f, 22.0f)),
            ("Batı Sahil Villası #3 (Lüks)", "West Coast Villa #3 (Luxury)", new Vector3(-150.0f, 0.05f, 22.0f)),
            ("Batı Sahil Villası #6 (Panoramik)", "West Coast Villa #6 (Panoramic)", new Vector3(-150.0f, 0.05f, 65.0f)),
            ("Batı Sahil Villası #9 (Bahçeli)", "West Coast Villa #9 (Garden)", new Vector3(-188.0f, 0.05f, 108.0f)),
            ("Batı Sahil Villası #12 (Malikane)", "West Coast Villa #12 (Mansion)", new Vector3(-188.0f, 0.05f, 151.0f)),

            // 3. KASABA KONUTLARI & MÜSTAKİL EVLER
            ("Kasaba Konutları No:1 (Müstakil)", "Town House #1 (Detached)", new Vector3(-58.0f, 0.05f, -9.0f)),
            ("Kasaba Konutları No:3 (Bahçeli)", "Town House #3 (Garden)", new Vector3(-32.0f, 0.05f, -9.0f)),
            ("Kasaba Konutları No:5 (Çiçekli)", "Town House #5 (Floral)", new Vector3(32.0f, 0.05f, -9.0f)),
            ("Kasaba Konutları No:7 (Güneşli)", "Town House #7 (Sunny)", new Vector3(58.0f, 0.05f, -9.0f)),
            ("Kasaba Konutları No:2 (Güney Bahçe)", "Town House #2 (South Garden)", new Vector3(-58.0f, 0.05f, -55.0f)),
            ("Kasaba Konutları No:6 (Güney Köşk)", "Town House #6 (South Villa)", new Vector3(32.0f, 0.05f, -55.0f)),

            // 4. BELEDİYE & ŞEHİR KAMU MERKEZLERİ
            ("Belediye Hizmet Binası Danışma", "Town Hall Front Desk", new Vector3(0.0f, 0.05f, -9.0f)),
            ("Devlet Hastanesi Acil Servis", "State Hospital Emergency", new Vector3(-150.0f, 0.05f, -55.0f)),
            ("Kasaba Merkez Kütüphanesi", "Town Central Library", new Vector3(-188.0f, 0.05f, -55.0f)),

            // 5. YENİ GÜNEY BÖLGESİ (BÜYÜK CAMİ & KAFELER MAHALLESİ)
            ("🌿 Botanik & Bahçe Kafe", "🌿 Botanic & Garden Cafe", new Vector3(75.0f, 0.05f, -69.0f)),
            ("☕ Nostalji Kitap & Kahve Evi", "☕ Nostalgia Books & Coffee House", new Vector3(75.0f, 0.05f, -91.5f)),
            ("🍰 Çiftlik Patisserie & Bistro", "🍰 Farm Patisserie & Bistro", new Vector3(75.0f, 0.05f, -114.0f)),
            ("🕌 Büyük Kasaba Camii (Vakıf & İdare)", "🕌 Grand Town Mosque (Foundation & Admin)", new Vector3(0.0f, 0.05f, -91.5f)),
            ("🕌 Cami Avlusu & Şadırvan Dinlenme Alanı", "🕌 Mosque Courtyard & Fountain Rest Area", new Vector3(0.0f, 0.05f, -62.0f))
        };

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Yalnızca dükkan açıkken online sipariş düşer
            if (StoreStatusManager.Instance != null && !StoreStatusManager.Instance.IsOpen) return;

            orderTimer += Time.deltaTime;
            if (orderTimer >= nextOrderInterval)
            {
                orderTimer = 0f;
                nextOrderInterval = UnityEngine.Random.Range(25f, 45f);

                TryCreateNewOnlineOrder();
            }
        }

        /// <summary>
        /// Üst üste aynı adrese sipariş çıkmasını engelleyen, son 8 adresi ve motordaki diğer siparişi filtreleyen akıllı adres seçici.
        /// </summary>
        private (string nameTr, string nameEn, Vector3 doorstepPos) GetNextUniqueDestination(CourierMotorcycleController targetMoto)
        {
            if (deliveryDestinations == null || deliveryDestinations.Length == 0)
            {
                return ("Merkez", "Center", Vector3.zero);
            }

            // Deste boşsa veya yetersizse yeniden doldur ve karıştır
            if (destinationDeck.Count == 0)
            {
                for (int i = 0; i < deliveryDestinations.Length; i++)
                {
                    destinationDeck.Add(i);
                }

                // Fisher-Yates Karıştırma (Shuffle)
                for (int i = 0; i < destinationDeck.Count; i++)
                {
                    int rnd = UnityEngine.Random.Range(i, destinationDeck.Count);
                    int temp = destinationDeck[i];
                    destinationDeck[i] = destinationDeck[rnd];
                    destinationDeck[rnd] = temp;
                }
            }

            int chosenIndex = -1;

            // Motorda halihazırda yüklü olan adres(ler)i tespit et (aynı motora aynı adres ikinci kez verilmez)
            HashSet<string> motoExistingDestNames = new HashSet<string>();
            if (targetMoto != null && targetMoto.LoadedOrders != null)
            {
                foreach (var o in targetMoto.LoadedOrders)
                {
                    if (o != null && !string.IsNullOrEmpty(o.destinationNameTr))
                    {
                        motoExistingDestNames.Add(o.destinationNameTr);
                    }
                }
            }

            // 1. Aşama: Hem son teslim edilen geçmişte olmayan hem de motorda bulunmayan bir adres ara
            for (int i = 0; i < destinationDeck.Count; i++)
            {
                int candIdx = destinationDeck[i];
                var candDest = deliveryDestinations[candIdx];

                if (!recentDestinationHistory.Contains(candIdx) && !motoExistingDestNames.Contains(candDest.nameTr))
                {
                    chosenIndex = candIdx;
                    destinationDeck.RemoveAt(i);
                    break;
                }
            }

            // 2. Aşama: Eğer destedeki tüm adaylar geçmişteyse, sadece motordaki adresten farklı olan ilk adresi seç
            if (chosenIndex == -1)
            {
                for (int i = 0; i < destinationDeck.Count; i++)
                {
                    int candIdx = destinationDeck[i];
                    var candDest = deliveryDestinations[candIdx];

                    if (!motoExistingDestNames.Contains(candDest.nameTr))
                    {
                        chosenIndex = candIdx;
                        destinationDeck.RemoveAt(i);
                        break;
                    }
                }
            }

            // 3. Aşama: En kötü senaryoda destenin başındakini al
            if (chosenIndex == -1 && destinationDeck.Count > 0)
            {
                chosenIndex = destinationDeck[0];
                destinationDeck.RemoveAt(0);
            }
            else if (chosenIndex == -1)
            {
                chosenIndex = UnityEngine.Random.Range(0, deliveryDestinations.Length);
            }

            // Geçmiş kuyruğunu güncelle
            recentDestinationHistory.Enqueue(chosenIndex);
            while (recentDestinationHistory.Count > MAX_RECENT_HISTORY_COUNT)
            {
                recentDestinationHistory.Dequeue();
            }

            return deliveryDestinations[chosenIndex];
        }

        public void TryCreateNewOnlineOrder()
        {
            if (CourierManager.Instance == null) return;

            // Müsait ve kuryesi atanmış bir motor var mı?
            CourierMotorcycleController availableMoto = CourierManager.Instance.GetAvailableMotorcycleForOrder();
            if (availableMoto == null) return;

            // Toptancı kataloğundan (Wholesaler) seviyeye uygun standart ürünler seç
            List<WholesaleProductDef> catalog = WholesaleDatabase.GetWholesaleOnlyProducts();
            if (catalog == null || catalog.Count == 0) return;

            int currentLevel = (EnvironmentBuilder.Instance != null) ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            List<WholesaleProductDef> eligibleProducts = catalog.FindAll(p => p != null && p.requiredLevel <= currentLevel);
            if (eligibleProducts.Count == 0) eligibleProducts = catalog;

            OnlineCustomerOrder order = new OnlineCustomerOrder();
            order.orderId = "ORD-" + UnityEngine.Random.Range(1000, 9999);

            // Rastgele Müşteri İsmi ve Handle Havuzundan Seçim
            bool isFemale = (UnityEngine.Random.value > 0.5f);
            if (isFemale)
            {
                var cand = femaleCustomers[UnityEngine.Random.Range(0, femaleCustomers.Length)];
                order.customerName = cand.name;
                order.customerHandle = cand.handle;
            }
            else
            {
                var cand = maleCustomers[UnityEngine.Random.Range(0, maleCustomers.Length)];
                order.customerName = cand.name;
                order.customerHandle = cand.handle;
            }

            // Tekrarsız ve Akıllı Hedef Adres Seçimi
            var dest = GetNextUniqueDestination(availableMoto);
            order.destinationNameTr = dest.nameTr;
            order.destinationNameEn = dest.nameEn;
            order.targetDoorstepPosition = dest.doorstepPos;

            // 1 ila 3 Çeşit Ürün ve 1'er veya 2'şer Adet
            int itemCount = UnityEngine.Random.Range(1, 3);
            int estimatedVal = 0;

            for (int i = 0; i < itemCount; i++)
            {
                WholesaleProductDef prod = eligibleProducts[UnityEngine.Random.Range(0, eligibleProducts.Count)];
                if (!order.requestedProducts.Contains(prod))
                {
                    int qty = UnityEngine.Random.Range(1, 3); // 1 veya 2 adet
                    order.requestedProducts.Add(prod);
                    order.requestedQuantities.Add(qty);
                    order.gatheredQuantities.Add(0); // Başlangıçta 0 toplandı

                    estimatedVal += prod.SalePricePerUnit * qty;
                }
            }

            order.totalEstimatedValue = estimatedVal;
            order.assignedMotorcycle = availableMoto;

            pendingOrders.Add(order);
            availableMoto.AssignOrderToCargo(order);

            OnOrdersChanged?.Invoke();
            Debug.Log($"[Online Market] Yeni Sipariş Geldi: {order.orderId} -> {order.LocalizedDestination} ({order.requestedProducts.Count} Çeşit Ürün)");
        }

        public OnlineCustomerOrder GetNextOrderNeedingStocker()
        {
            for (int i = 0; i < pendingOrders.Count; i++)
            {
                var o = pendingOrders[i];
                if (o != null && !o.isGatheringCompleted && !o.isAssignedToStocker && o.assignedMotorcycle != null)
                {
                    // Sadece park yerinde veya reyoncuyu bekleyen motorlar için ürün toplanır (yoldaki motor kovalanmaz!)
                    if (o.assignedMotorcycle.CurrentState == MotorcycleState.ParkedInBay ||
                        o.assignedMotorcycle.CurrentState == MotorcycleState.WaitingForStocker)
                    {
                        return o;
                    }
                }
            }
            return null;
        }

        public void NotifyOrderGathered(OnlineCustomerOrder order)
        {
            if (order == null || order.assignedMotorcycle == null) return;

            order.isGatheringCompleted = true;
            order.isAssignedToStocker = false;

            // Bu motora atanmış ve hala reyoncu tarafından toplanmayı bekleyen BAŞKA sipariş var mı kontrol et:
            bool hasUncollectedOrderForSameMoto = false;
            for (int i = 0; i < pendingOrders.Count; i++)
            {
                var o = pendingOrders[i];
                if (o != null && o != order && o.assignedMotorcycle == order.assignedMotorcycle && !o.isGatheringCompleted)
                {
                    hasUncollectedOrderForSameMoto = true;
                    break;
                }
            }

            // Eğer motora ait TÜM siparişler (tek adres veya çift adres) reyoncu tarafından yüklendiyse kurye yola çıksın!
            if (!hasUncollectedOrderForSameMoto)
            {
                int hour = (TimeManager.Instance != null) ? TimeManager.Instance.Hour : 8;
                bool isWorkHours = (hour >= 8 && hour < 24);

                if (isWorkHours && order.assignedMotorcycle.AssignedCourier != null)
                {
                    order.assignedMotorcycle.StartDeliveryRoute();
                }
                else
                {
                    // Kapanış saati/gece vakti yüklendiyse sipariş motorun kasasında kalır, ertesi sabah kurye götürür
                    order.assignedMotorcycle.CurrentState = MotorcycleState.ParkedInBay;
                }
            }

            OnOrdersChanged?.Invoke();
        }

        public void NotifyOrderGatheredByStocker(OnlineCustomerOrder order) => NotifyOrderGathered(order);

        public void CompleteOrderDelivery(OnlineCustomerOrder order)
        {
            if (order == null) return;

            pendingOrders.Remove(order);

            int earnedMoney = 0;
            int totalExpectedCount = 0;
            int totalDeliveredCount = 0;

            for (int i = 0; i < order.requestedProducts.Count; i++)
            {
                var prod = order.requestedProducts[i];
                int requested = order.requestedQuantities[i];
                int gathered = (i < order.gatheredQuantities.Count) ? order.gatheredQuantities[i] : requested;

                totalExpectedCount += requested;
                totalDeliveredCount += gathered;

                earnedMoney += prod.SalePricePerUnit * gathered;
            }

            bool isFullDelivery = (totalDeliveredCount >= totalExpectedCount);

            if (isFullDelivery)
            {
                // Kurye teslimat ücretini de ekle (+60C)
                earnedMoney += order.courierDeliveryFee;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCredits(earnedMoney);
            }

            if (FinanceManager.Instance != null)
            {
                string cat = LocalizationManager.L("FinCat_OnlineDelivery", "Online Market & Kurye Geliri", "Online Market & Courier Revenue");
                string desc = isFullDelivery
                    ? string.Format(LocalizationManager.L("FinDesc_DeliveryFull", "Online Sipariş #{0} Tam Teslimat (+Kurye Ücreti)", "Online Order #{0} Full Delivery (+Courier Fee)"), order.orderId)
                    : string.Format(LocalizationManager.L("FinDesc_DeliveryPartial", "Online Sipariş #{0} Kısmi Teslimat", "Online Order #{0} Partial Delivery"), order.orderId);

                FinanceManager.Instance.RecordIncome(cat, desc, earnedMoney);
            }

            // Chirper Sosyal Medya Tweet'i Tetikle
            if (SocialMediaManager.Instance != null)
            {
                string storeName = SocialMediaManager.Instance.GetStoreName();
                if (isFullDelivery)
                {
                    string tweetTr = $"@{storeName} Online Marketten verdiğim siparişi kurye jet hızıyla ve eksiksiz getirdi! Taptaze ürünler, teşekkürler! 🛵💨⭐";
                    string tweetEn = $"My online order from @{storeName} was delivered lightning fast and 100% complete! Super fresh, thank you! 🛵💨⭐";
                    SocialMediaManager.Instance.PostCustomerReview(order.customerName, tweetTr, tweetEn, 5);
                }
                else
                {
                    string tweetTr = $"@{storeName} Online Market siparişimde bazı ürünler eksik geldi. Reyonlarınızda stok kalmadıysa bildirseydiniz keşke... 📦😕";
                    string tweetEn = $"Some items were missing in my @{storeName} online order. Wish you notified me if stock was low... 📦😕";
                    SocialMediaManager.Instance.PostCustomerReview(order.customerName, tweetTr, tweetEn, 2);
                }
            }

            OnOrdersChanged?.Invoke();
            Debug.Log($"[Online Market] Teslimat Tamamlandı: {order.orderId} | Kazanılan: {earnedMoney:N0}C | Eksiksiz mi: {isFullDelivery}");
        }
    }
}
