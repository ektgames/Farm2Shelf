using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    public enum TownContractKind
    {
        Residential,
        Cafe,
        Civic,
        Worship,
        Emergency,
        Leisure
    }

    public enum TownContractOfferStatus
    {
        Open = 0,
        Queued = 1,
        Active = 2,
        Completed = 3,
        Failed = 4,
        Declined = 5
    }

    public class TownContractPartner
    {
        public string id;
        public string nameTr;
        public string nameEn;
        public string iconEmoji;
        public Vector3 doorstepPos;
        public TownContractKind kind;
        public FurnitureType[] preferredShelves;
        public int dailyPremium;
        public int dailyQty;

        public string LocalizedName => LocalizationManager.L("TownPartner_" + id, nameTr, nameEn);

        public string LocalizedKind
        {
            get
            {
                switch (kind)
                {
                    case TownContractKind.Cafe: return LocalizationManager.L("TownKind_Cafe", "Kafe / Mutfak", "Cafe / Kitchen");
                    case TownContractKind.Civic: return LocalizationManager.L("TownKind_Civic", "Kamu / Kurum", "Civic / Institution");
                    case TownContractKind.Worship: return LocalizationManager.L("TownKind_Worship", "İbadet / Vakıf", "Worship / Foundation");
                    case TownContractKind.Emergency: return LocalizationManager.L("TownKind_Emergency", "Acil Hizmet", "Emergency Service");
                    case TownContractKind.Leisure: return LocalizationManager.L("TownKind_Leisure", "Eğlence / Spor", "Leisure / Sports");
                    default: return LocalizationManager.L("TownKind_Residential", "Konut / Mahalle", "Residential");
                }
            }
        }
    }

    [Serializable]
    public class TownContractOffer
    {
        public string offerId;
        public string partnerId;
        public int payout;
        public int distanceMeters;
        public List<string> productIds = new List<string>();
        public List<int> quantities = new List<int>();
        public TownContractOfferStatus status = TownContractOfferStatus.Open;

        public int TotalUnits
        {
            get
            {
                int n = 0;
                if (quantities == null) return 0;
                for (int i = 0; i < quantities.Count; i++) n += Mathf.Max(0, quantities[i]);
                return n;
            }
        }
    }

    [Serializable]
    public class TownContractSaveData
    {
        public List<int> contractMotorcycleSlots = new List<int>();
        public List<TownContractOffer> offers = new List<TownContractOffer>();
        public List<string> queueOfferIds = new List<string>();
        public int successCount;
        public int failCount;
        public string lastNoteTr = "";
        public string lastNoteEn = "";
        public bool lastWasCorrect = true;
        public int lastBoardDay = -1;
    }

    /// <summary>
    /// Kasaba binalarından 5'li kontrat teklifi, kabul sırasına göre kuyruk
    /// ve kontrat motorunun tek tek teslimat döngüsünü yönetir.
    /// </summary>
    public class TownContractManager : MonoBehaviour
    {
        public static readonly Vector3 StoreOrigin = new Vector3(6.0f, 0.05f, 6.0f);
        public const int BoardSize = 5;

        private static TownContractManager instance;
        public static TownContractManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<TownContractManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("TownContractManager");
                        instance = go.AddComponent<TownContractManager>();
                    }
                }
                return instance;
            }
        }

        public event Action OnContractsChanged;

        private readonly List<int> contractMotorcycleSlots = new List<int>();
        private readonly List<TownContractOffer> offers = new List<TownContractOffer>();
        private readonly List<string> queueOfferIds = new List<string>();
        private int successCount;
        private int failCount;
        private string lastNoteTr = "";
        private string lastNoteEn = "";
        private bool lastWasCorrect = true;
        private int lastBoardDay = -1;
        private float startNextTimer;

        public IReadOnlyList<int> ContractMotorcycleSlots => contractMotorcycleSlots;
        public IReadOnlyList<TownContractOffer> Offers => offers;
        public IReadOnlyList<string> QueueOfferIds => queueOfferIds;
        public int SuccessCount => successCount;
        public int FailCount => failCount;
        public string LastNoteTr => lastNoteTr;
        public string LastNoteEn => lastNoteEn;
        public bool LastWasCorrect => lastWasCorrect;

        public static readonly TownContractPartner[] Catalog = new TownContractPartner[]
        {
            Make("apt_a", "Kuzey Apartmanları A Blok", "North Apartments Block A", "🏢", new Vector3(-37.5f, 0.05f, 68.0f), TownContractKind.Residential, 90, 3, FurnitureType.ProduceShelf, FurnitureType.Fridge, FurnitureType.Shelf),
            Make("apt_b", "Kuzey Apartmanları B Blok", "North Apartments Block B", "🏢", new Vector3(0.0f, 0.05f, 68.0f), TownContractKind.Residential, 90, 3, FurnitureType.ProduceShelf, FurnitureType.Fridge, FurnitureType.Shelf),
            Make("apt_c", "Kuzey Apartmanları C Blok", "North Apartments Block C", "🏢", new Vector3(0.0f, 0.05f, 98.0f), TownContractKind.Residential, 90, 3, FurnitureType.ProduceShelf, FurnitureType.Fridge, FurnitureType.Shelf),
            Make("apt_d", "Kuzey Apartmanları D Blok", "North Apartments Block D", "🏢", new Vector3(37.5f, 0.05f, 68.0f), TownContractKind.Residential, 90, 3, FurnitureType.ProduceShelf, FurnitureType.Fridge, FurnitureType.Shelf),
            Make("villa_1", "Batı Sahil Villası #1 (Palmiye)", "West Coast Villa #1 (Palm)", "🏡", new Vector3(-112.0f, 0.05f, 22.0f), TownContractKind.Residential, 140, 2, FurnitureType.GourmetShelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("villa_3", "Batı Sahil Villası #3 (Lüks)", "West Coast Villa #3 (Luxury)", "🏡", new Vector3(-150.0f, 0.05f, 22.0f), TownContractKind.Residential, 140, 2, FurnitureType.GourmetShelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("villa_6", "Batı Sahil Villası #6 (Panoramik)", "West Coast Villa #6 (Panoramic)", "🏡", new Vector3(-150.0f, 0.05f, 65.0f), TownContractKind.Residential, 140, 2, FurnitureType.GourmetShelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("villa_9", "Batı Sahil Villası #9 (Bahçeli)", "West Coast Villa #9 (Garden)", "🏡", new Vector3(-188.0f, 0.05f, 108.0f), TownContractKind.Residential, 140, 2, FurnitureType.GourmetShelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("villa_12", "Batı Sahil Villası #12 (Malikane)", "West Coast Villa #12 (Mansion)", "🏡", new Vector3(-188.0f, 0.05f, 151.0f), TownContractKind.Residential, 160, 2, FurnitureType.GourmetShelf, FurnitureType.Fridge, FurnitureType.OrganicFridge),
            Make("house_1", "Kasaba Konutları No:1", "Town House #1", "🏠", new Vector3(-58.0f, 0.05f, -9.0f), TownContractKind.Residential, 80, 2, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("house_2", "Kasaba Konutları No:2", "Town House #2", "🏠", new Vector3(-58.0f, 0.05f, -55.0f), TownContractKind.Residential, 80, 2, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("house_3", "Kasaba Konutları No:3", "Town House #3", "🏠", new Vector3(-32.0f, 0.05f, -9.0f), TownContractKind.Residential, 80, 2, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("house_5", "Kasaba Konutları No:5", "Town House #5", "🏠", new Vector3(32.0f, 0.05f, -9.0f), TownContractKind.Residential, 80, 2, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("house_6", "Kasaba Konutları No:6", "Town House #6", "🏠", new Vector3(32.0f, 0.05f, -55.0f), TownContractKind.Residential, 80, 2, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("house_7", "Kasaba Konutları No:7", "Town House #7", "🏠", new Vector3(58.0f, 0.05f, -9.0f), TownContractKind.Residential, 80, 2, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("townhall", "Belediye Hizmet Binası", "Town Hall", "🏛️", new Vector3(0.0f, 0.05f, -9.0f), TownContractKind.Civic, 150, 4, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.BakeryCounter),
            Make("hospital", "Devlet Hastanesi", "State Hospital", "🏥", new Vector3(-150.0f, 0.05f, -55.0f), TownContractKind.Civic, 180, 4, FurnitureType.Fridge, FurnitureType.OrganicFridge, FurnitureType.ProduceShelf),
            Make("library", "Kasaba Merkez Kütüphanesi", "Town Central Library", "📚", new Vector3(-188.0f, 0.05f, -55.0f), TownContractKind.Civic, 110, 3, FurnitureType.BakeryCounter, FurnitureType.Fridge, FurnitureType.Shelf),
            Make("school", "Atatürk İlkokulu", "Ataturk Primary School", "🏫", new Vector3(-188.0f, 0.05f, -32.0f), TownContractKind.Civic, 160, 5, FurnitureType.BakeryCounter, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("bank", "Şehir Bankası & ATM Merkezi", "City Bank & ATM Center", "🏦", new Vector3(-112.0f, 0.05f, -32.0f), TownContractKind.Civic, 120, 3, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.BakeryCounter),
            Make("cafe_botanic", "Botanik & Bahçe Kafe", "Botanic & Garden Cafe", "🌿", new Vector3(75.0f, 0.05f, -69.0f), TownContractKind.Cafe, 170, 4, FurnitureType.GourmetShelf, FurnitureType.ProduceShelf, FurnitureType.BakeryCounter),
            Make("cafe_books", "Nostalji Kitap & Kahve Evi", "Nostalgia Books & Coffee House", "☕", new Vector3(75.0f, 0.05f, -91.5f), TownContractKind.Cafe, 170, 4, FurnitureType.GourmetShelf, FurnitureType.Fridge, FurnitureType.BakeryCounter),
            Make("cafe_farm", "Çiftlik Patisserie & Bistro", "Farm Patisserie & Bistro", "🍰", new Vector3(75.0f, 0.05f, -114.0f), TownContractKind.Cafe, 180, 4, FurnitureType.GourmetShelf, FurnitureType.BakeryCounter, FurnitureType.ProduceShelf),
            Make("mosque", "Büyük Kasaba Camii", "Grand Town Mosque", "🕌", new Vector3(0.0f, 0.05f, -91.5f), TownContractKind.Worship, 130, 4, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.ProduceShelf),
            Make("mosque_court", "Cami Avlusu & Şadırvan", "Mosque Courtyard & Fountain", "🕌", new Vector3(0.0f, 0.05f, -62.0f), TownContractKind.Worship, 100, 3, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.BakeryCounter),
            Make("fire", "İtfaiye İstasyonu", "Fire Station", "🚒", new Vector3(-150.0f, 0.05f, -69.0f), TownContractKind.Emergency, 130, 3, FurnitureType.Fridge, FurnitureType.Shelf, FurnitureType.BakeryCounter),
            Make("police", "Polis Karakolu", "Police Station", "🚓", new Vector3(-150.0f, 0.05f, -89.0f), TownContractKind.Emergency, 130, 3, FurnitureType.Fridge, FurnitureType.Shelf, FurnitureType.BakeryCounter),
            Make("gas", "Benzin İstasyonu Mini Market", "Gas Station Mini Market", "⛽", new Vector3(-150.0f, 0.05f, -110.0f), TownContractKind.Leisure, 110, 3, FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.BakeryCounter),
            Make("arcade", "Eğlence Merkezi", "Entertainment Center", "🎮", new Vector3(-112.0f, 0.05f, -71.0f), TownContractKind.Leisure, 120, 3, FurnitureType.Fridge, FurnitureType.Freezer, FurnitureType.BakeryCounter),
            Make("stadium", "Şehir Stadyumu", "City Stadium", "🏟️", new Vector3(-112.0f, 0.05f, -102.0f), TownContractKind.Leisure, 150, 5, FurnitureType.Fridge, FurnitureType.BakeryCounter, FurnitureType.ProduceShelf)
        };

        private static TownContractPartner Make(string id, string tr, string en, string icon, Vector3 door, TownContractKind kind, int premium, int qty, params FurnitureType[] shelves)
        {
            return new TownContractPartner
            {
                id = id,
                nameTr = tr,
                nameEn = en,
                iconEmoji = icon,
                doorstepPos = door,
                kind = kind,
                preferredShelves = shelves,
                dailyPremium = premium,
                dailyQty = qty
            };
        }

        public static TownContractPartner GetPartner(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Catalog.Length; i++)
            {
                if (Catalog[i].id == id) return Catalog[i];
            }
            return null;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                BindDayListeners();
                if (CountOpenOffers() == 0 && queueOfferIds.Count == 0)
                {
                    RollDailyBoard(true);
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            BindDayListeners();
        }

        private void Start()
        {
            BindDayListeners();
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnNewDayStarted -= HandleNewDayStarted;
            }
        }

        private void BindDayListeners()
        {
            if (TimeManager.Instance == null) return;
            TimeManager.Instance.OnNewDayStarted -= HandleNewDayStarted;
            TimeManager.Instance.OnNewDayStarted += HandleNewDayStarted;
        }

        private void HandleNewDayStarted(TimeManager.Season season, int day, int year)
        {
            RollDailyBoard(false);
        }

        private void Update()
        {
            if (StoreStatusManager.Instance != null && !StoreStatusManager.Instance.IsOpen) return;
            if (queueOfferIds.Count == 0) return;

            startNextTimer += Time.deltaTime;
            if (startNextTimer < 1.25f) return;
            startNextTimer = 0f;
            TryStartNextQueuedContract();
        }

        public int CountOpenOffers()
        {
            int n = 0;
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i] != null && offers[i].status == TownContractOfferStatus.Open) n++;
            }
            return n;
        }

        public int CountQueued()
        {
            return queueOfferIds.Count;
        }

        public TownContractOffer GetOffer(string offerId)
        {
            if (string.IsNullOrEmpty(offerId)) return null;
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i] != null && offers[i].offerId == offerId) return offers[i];
            }
            return null;
        }

        public bool IsSlotAssignedToContracts(int slotIndex)
        {
            return contractMotorcycleSlots.Contains(slotIndex);
        }

        public float GetFulfillmentRate()
        {
            int total = successCount + failCount;
            if (total <= 0) return 1f;
            return (float)successCount / total;
        }

        public string GetOverallVerdict(bool english)
        {
            int total = successCount + failCount;
            if (total == 0) return english ? "No deliveries yet" : "Henüz teslimat yok";
            float rate = GetFulfillmentRate();
            if (rate >= 0.75f) return english ? "Doing it right" : "Doğru gidiyor";
            if (rate >= 0.45f) return english ? "Mixed / at risk" : "Karışık / riskli";
            return english ? "Doing it wrong" : "Yanlış gidiyor";
        }

        public Color GetOverallVerdictColor()
        {
            int total = successCount + failCount;
            if (total == 0) return new Color(0.95f, 0.85f, 0.30f);
            float rate = GetFulfillmentRate();
            if (rate >= 0.75f) return new Color(0.25f, 0.90f, 0.45f);
            if (rate >= 0.45f) return new Color(1.0f, 0.75f, 0.20f);
            return new Color(1.0f, 0.38f, 0.32f);
        }

        public bool TryAssignMotorcycleToContracts(int slotIndex, out string errorTr, out string errorEn)
        {
            errorTr = "";
            errorEn = "";
            if (CourierManager.Instance == null)
            {
                errorTr = "Filo yöneticisi bulunamadı.";
                errorEn = "Fleet manager is missing.";
                return false;
            }

            CourierMotorcycleController moto = CourierManager.Instance.GetMotorcycleBySlot(slotIndex);
            if (moto == null)
            {
                errorTr = "Bu park yuvasında henüz motorsiklet yok.";
                errorEn = "There is no motorcycle in this bay yet.";
                return false;
            }

            if (IsSlotAssignedToContracts(slotIndex))
            {
                errorTr = "Bu motor zaten kontrat filosunda.";
                errorEn = "This motorcycle is already on the contract fleet.";
                return false;
            }

            if (moto.LoadedOrders != null && moto.LoadedOrders.Count > 0)
            {
                errorTr = "Motorun bagajında sipariş varken kontratlara alınamaz.";
                errorEn = "This bike still has cargo. Wait until deliveries finish.";
                return false;
            }

            if (moto.CurrentState != MotorcycleState.ParkedInBay)
            {
                errorTr = "Motor parkta dururken kontratlara eklenebilir.";
                errorEn = "The motorcycle can only be assigned while parked.";
                return false;
            }

            contractMotorcycleSlots.Add(slotIndex);
            contractMotorcycleSlots.Sort();
            OnContractsChanged?.Invoke();
            CourierManager.Instance.NotifyFleetChanged();
            TryStartNextQueuedContract();
            return true;
        }

        public bool TryUnassignMotorcycleFromContracts(int slotIndex, out string errorTr, out string errorEn)
        {
            errorTr = "";
            errorEn = "";
            if (!IsSlotAssignedToContracts(slotIndex))
            {
                errorTr = "Bu motor kontrat filosunda değil.";
                errorEn = "This motorcycle is not on the contract fleet.";
                return false;
            }

            CourierMotorcycleController moto = CourierManager.Instance != null
                ? CourierManager.Instance.GetMotorcycleBySlot(slotIndex)
                : null;
            if (moto != null)
            {
                if (moto.LoadedOrders != null && moto.LoadedOrders.Count > 0)
                {
                    errorTr = "Kontrat teslimatı bitmeden motor online filoya dönemez.";
                    errorEn = "The bike cannot return to the online fleet until its cargo is delivered.";
                    return false;
                }
                if (moto.CurrentState != MotorcycleState.ParkedInBay)
                {
                    errorTr = "Motor parkta değilken kontratlardan çıkarılamaz.";
                    errorEn = "Unassign is only allowed while the motorcycle is parked.";
                    return false;
                }
            }

            contractMotorcycleSlots.Remove(slotIndex);
            OnContractsChanged?.Invoke();
            if (CourierManager.Instance != null) CourierManager.Instance.NotifyFleetChanged();
            return true;
        }

        public bool AcceptOffer(string offerId, out string errorTr, out string errorEn)
        {
            errorTr = "";
            errorEn = "";
            TownContractOffer offer = GetOffer(offerId);
            if (offer == null || offer.status != TownContractOfferStatus.Open)
            {
                errorTr = "Bu teklif artık seçilemez.";
                errorEn = "This offer is no longer available.";
                return false;
            }

            offer.status = TownContractOfferStatus.Queued;
            queueOfferIds.Add(offer.offerId);
            lastNoteTr = "Kontrat sıraya alındı. Motor eklenince ilk işten başlar.";
            lastNoteEn = "Contract queued. The assigned bike starts with the first job.";
            lastWasCorrect = true;
            OnContractsChanged?.Invoke();
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotifyTownContractQueued();
            }
            TryStartNextQueuedContract();
            return true;
        }

        public bool DeclineOffer(string offerId, out string errorTr, out string errorEn)
        {
            errorTr = "";
            errorEn = "";
            TownContractOffer offer = GetOffer(offerId);
            if (offer == null || offer.status != TownContractOfferStatus.Open)
            {
                errorTr = "Bu teklif reddedilemez.";
                errorEn = "This offer cannot be declined.";
                return false;
            }

            offer.status = TownContractOfferStatus.Declined;
            OnContractsChanged?.Invoke();
            return true;
        }

        public void NotifyMotorcycleParked(CourierMotorcycleController moto)
        {
            if (moto == null) return;
            if (!IsSlotAssignedToContracts(moto.SlotIndex)) return;
            TryStartNextQueuedContract();
        }

        public void TryStartNextQueuedContract()
        {
            if (HasActiveContractOrder()) return;
            if (queueOfferIds.Count == 0) return;
            if (StoreStatusManager.Instance != null && !StoreStatusManager.Instance.IsOpen) return;
            if (OnlineMarketOrderManager.Instance == null) return;

            CourierMotorcycleController moto = GetAvailableContractMotorcycle();
            if (moto == null) return;

            TownContractOffer offer = null;
            while (queueOfferIds.Count > 0 && offer == null)
            {
                TownContractOffer candidate = GetOffer(queueOfferIds[0]);
                if (candidate == null || (candidate.status != TownContractOfferStatus.Queued && candidate.status != TownContractOfferStatus.Active))
                {
                    queueOfferIds.RemoveAt(0);
                    continue;
                }
                offer = candidate;
            }
            if (offer == null) return;

            OnlineCustomerOrder order = BuildOrderFromOffer(offer, moto);
            if (order == null) return;

            offer.status = TownContractOfferStatus.Active;
            OnlineMarketOrderManager.Instance.EnqueueExistingOrder(order);
            OnContractsChanged?.Invoke();
        }

        public void NotifyContractDelivery(OnlineCustomerOrder order, bool isFullDelivery)
        {
            if (order == null || !order.isTownContract) return;

            TownContractOffer offer = GetOffer(order.contractOfferId);
            if (offer == null)
            {
                for (int i = 0; i < offers.Count; i++)
                {
                    if (offers[i] != null && offers[i].partnerId == order.contractPartnerId && offers[i].status == TownContractOfferStatus.Active)
                    {
                        offer = offers[i];
                        break;
                    }
                }
            }

            TownContractPartner partner = GetPartner(order.contractPartnerId);
            bool usedLocal = OrderUsedLocalGoods(order);

            if (offer != null)
            {
                queueOfferIds.Remove(offer.offerId);
            }

            if (isFullDelivery)
            {
                successCount++;
                lastWasCorrect = true;
                int bonus = offer != null ? offer.payout : 80;
                if (usedLocal) bonus += 40;
                if (EconomyManager.Instance != null) EconomyManager.Instance.AddCredits(bonus);
                if (FinanceManager.Instance != null)
                {
                    string desc = string.Format(
                        LocalizationManager.L("FinDesc_TownContractBonus", "Kasaba Kontratı Prim: {0}", "Town Contract Bonus: {0}"),
                        partner != null ? partner.LocalizedName : order.LocalizedDestination);
                    FinanceManager.Instance.RecordIncome(FinanceCategories.TownContracts, desc, bonus);
                }

                if (offer != null) offer.status = TownContractOfferStatus.Completed;
                lastNoteTr = usedLocal
                    ? "Teslimat eksiksiz ve yerel ürünle bitti. Motor park edince sıradaki kontrat yüklenir."
                    : "Teslimat eksiksiz bitti. Motor dükkana dönüp park edince 2. kontrat yüklenir.";
                lastNoteEn = usedLocal
                    ? "Full delivery with local goods. Next contract loads after the bike parks."
                    : "Full delivery done. The next contract loads after the bike returns and parks.";

                if (SocialMediaManager.Instance != null && partner != null)
                {
                    string store = SocialMediaManager.Instance.GetStoreName();
                    SocialMediaManager.Instance.PostCustomerReview(
                        partner.LocalizedName,
                        $"@{store} {partner.nameTr} kontratını eksiksiz teslim etti. 🤝📦",
                        $"@{store} completed the {partner.nameEn} contract in full. 🤝📦",
                        5);
                }
            }
            else
            {
                failCount++;
                lastWasCorrect = false;
                if (offer != null) offer.status = TownContractOfferStatus.Failed;
                lastNoteTr = "Eksik teslimat: bu kontrat bozuldu. Motor park edince sıradaki işe geçer.";
                lastNoteEn = "Short delivery: this contract failed. The bike moves to the next job after parking.";
                if (SocialMediaManager.Instance != null && partner != null)
                {
                    string store = SocialMediaManager.Instance.GetStoreName();
                    SocialMediaManager.Instance.PostCustomerReview(
                        partner.LocalizedName,
                        $"@{store} {partner.nameTr} kontratını eksik götürdü. 📋❌",
                        $"@{store} delivered the {partner.nameEn} contract incomplete. 📋❌",
                        1);
                }
            }

            OnContractsChanged?.Invoke();
        }

        public TownContractSaveData CreateSaveSnapshot()
        {
            TownContractSaveData data = new TownContractSaveData
            {
                contractMotorcycleSlots = new List<int>(contractMotorcycleSlots),
                offers = new List<TownContractOffer>(),
                queueOfferIds = new List<string>(queueOfferIds),
                successCount = successCount,
                failCount = failCount,
                lastNoteTr = lastNoteTr,
                lastNoteEn = lastNoteEn,
                lastWasCorrect = lastWasCorrect,
                lastBoardDay = lastBoardDay
            };
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i] != null) data.offers.Add(CloneOffer(offers[i]));
            }
            return data;
        }

        public void RestoreFromSave(TownContractSaveData data)
        {
            contractMotorcycleSlots.Clear();
            offers.Clear();
            queueOfferIds.Clear();
            successCount = 0;
            failCount = 0;
            lastNoteTr = "";
            lastNoteEn = "";
            lastWasCorrect = true;

            if (data != null)
            {
                if (data.contractMotorcycleSlots != null)
                {
                    for (int i = 0; i < data.contractMotorcycleSlots.Count; i++)
                    {
                        int slot = data.contractMotorcycleSlots[i];
                        if (slot >= 0 && slot < CourierManager.MAX_MOTORCYCLES && !contractMotorcycleSlots.Contains(slot))
                        {
                            contractMotorcycleSlots.Add(slot);
                        }
                    }
                    contractMotorcycleSlots.Sort();
                }

                successCount = data.successCount;
                failCount = data.failCount;
                lastNoteTr = data.lastNoteTr ?? "";
                lastNoteEn = data.lastNoteEn ?? "";
                lastWasCorrect = data.lastWasCorrect;
                lastBoardDay = data.lastBoardDay;

                if (data.offers != null)
                {
                    for (int i = 0; i < data.offers.Count; i++)
                    {
                        if (data.offers[i] == null || string.IsNullOrEmpty(data.offers[i].offerId)) continue;
                        if (GetPartner(data.offers[i].partnerId) == null) continue;
                        offers.Add(CloneOffer(data.offers[i]));
                    }
                }

                if (data.queueOfferIds != null)
                {
                    for (int i = 0; i < data.queueOfferIds.Count; i++)
                    {
                        string id = data.queueOfferIds[i];
                        if (!string.IsNullOrEmpty(id) && GetOffer(id) != null && !queueOfferIds.Contains(id))
                        {
                            queueOfferIds.Add(id);
                        }
                    }
                }
            }

            PruneInvalidMotorcycleSlots();
            if (CountOpenOffers() == 0 && queueOfferIds.Count == 0 && offers.Count == 0)
            {
                RollDailyBoard(true);
            }
            OnContractsChanged?.Invoke();
        }

        public void ResetToDefaults()
        {
            contractMotorcycleSlots.Clear();
            offers.Clear();
            queueOfferIds.Clear();
            successCount = 0;
            failCount = 0;
            lastNoteTr = "";
            lastNoteEn = "";
            lastWasCorrect = true;
            lastBoardDay = -1;
            RollDailyBoard(true);
            OnContractsChanged?.Invoke();
        }

        public void PruneInvalidMotorcycleSlots()
        {
            if (contractMotorcycleSlots.Count == 0) return;
            int owned = CourierManager.Instance != null ? CourierManager.Instance.OwnedMotorcycleCount : 0;
            bool changed = false;
            for (int i = contractMotorcycleSlots.Count - 1; i >= 0; i--)
            {
                int slot = contractMotorcycleSlots[i];
                CourierMotorcycleController moto = CourierManager.Instance != null
                    ? CourierManager.Instance.GetMotorcycleBySlot(slot)
                    : null;
                if (moto == null || slot >= owned)
                {
                    contractMotorcycleSlots.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed) OnContractsChanged?.Invoke();
        }

        public static int MeasureDistanceMeters(Vector3 doorstep)
        {
            return Mathf.Max(8, Mathf.RoundToInt(Vector3.Distance(StoreOrigin, doorstep)));
        }

        private void RollDailyBoard(bool force)
        {
            int today = TimeManager.Instance != null ? TimeManager.Instance.Day : 1;
            if (!force && lastBoardDay == today) return;

            HashSet<string> keepIds = new HashSet<string>();
            List<TownContractOffer> kept = new List<TownContractOffer>();
            for (int i = 0; i < offers.Count; i++)
            {
                TownContractOffer o = offers[i];
                if (o == null) continue;
                bool keep = o.status == TownContractOfferStatus.Active || HasPendingOrderForOffer(o.offerId);
                if (keep)
                {
                    kept.Add(o);
                    keepIds.Add(o.partnerId);
                }
            }

            offers.Clear();
            offers.AddRange(kept);

            for (int i = queueOfferIds.Count - 1; i >= 0; i--)
            {
                TownContractOffer q = GetOffer(queueOfferIds[i]);
                if (q == null || q.status != TownContractOfferStatus.Active)
                {
                    queueOfferIds.RemoveAt(i);
                }
            }

            List<TownContractPartner> pool = new List<TownContractPartner>();
            for (int i = 0; i < Catalog.Length; i++)
            {
                if (!keepIds.Contains(Catalog[i].id)) pool.Add(Catalog[i]);
            }

            int needed = BoardSize;
            for (int n = 0; n < needed && pool.Count > 0; n++)
            {
                int pick = UnityEngine.Random.Range(0, pool.Count);
                TownContractPartner partner = pool[pick];
                pool.RemoveAt(pick);
                TownContractOffer offer = CreateOfferForPartner(partner);
                if (offer != null) offers.Add(offer);
            }

            lastBoardDay = today;
            lastNoteTr = "Yeni gün: 5 yeni kontrat teklifi. Karlı görürsen sıraya al, istemezsen geç.";
            lastNoteEn = "New day: 5 fresh contract offers. Queue them if they look profitable, or skip.";
            OnContractsChanged?.Invoke();
        }

        private bool HasPendingOrderForOffer(string offerId)
        {
            if (string.IsNullOrEmpty(offerId) || OnlineMarketOrderManager.Instance == null) return false;
            List<OnlineCustomerOrder> pending = OnlineMarketOrderManager.Instance.PendingOrders;
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i] != null && pending[i].isTownContract && pending[i].contractOfferId == offerId) return true;
            }
            return false;
        }

        private TownContractOffer CreateOfferForPartner(TownContractPartner partner)
        {
            List<WholesaleProductDef> pool = BuildProductPool(partner);
            if (pool.Count == 0) return null;

            int kinds = UnityEngine.Random.Range(1, 4);
            int remainingQty = UnityEngine.Random.Range(3, 11);
            TownContractOffer offer = new TownContractOffer
            {
                offerId = "KNT-" + UnityEngine.Random.Range(10000, 99999),
                partnerId = partner.id,
                distanceMeters = MeasureDistanceMeters(partner.doorstepPos),
                status = TownContractOfferStatus.Open
            };

            for (int i = 0; i < kinds && remainingQty > 0 && pool.Count > 0; i++)
            {
                int pIdx = UnityEngine.Random.Range(0, pool.Count);
                WholesaleProductDef prod = pool[pIdx];
                pool.RemoveAt(pIdx);
                if (prod == null) continue;
                int qty = Mathf.Clamp(UnityEngine.Random.Range(1, remainingQty + 1), 1, 6);
                remainingQty -= qty;
                offer.productIds.Add(prod.id);
                offer.quantities.Add(qty);
            }

            if (offer.productIds.Count == 0) return null;
            offer.payout = CalculatePayout(partner, offer);
            return offer;
        }

        private static int CalculatePayout(TownContractPartner partner, TownContractOffer offer)
        {
            int kindBonus = 50;
            switch (partner.kind)
            {
                case TownContractKind.Cafe: kindBonus = 85; break;
                case TownContractKind.Civic: kindBonus = 90; break;
                case TownContractKind.Worship: kindBonus = 70; break;
                case TownContractKind.Emergency: kindBonus = 80; break;
                case TownContractKind.Leisure: kindBonus = 75; break;
            }

            int distancePay = Mathf.RoundToInt(offer.distanceMeters * 1.65f);
            int qtyPay = offer.TotalUnits * 14;
            return Mathf.Max(90, kindBonus + distancePay + qtyPay);
        }

        private CourierMotorcycleController GetAvailableContractMotorcycle()
        {
            if (CourierManager.Instance == null) return null;
            for (int i = 0; i < contractMotorcycleSlots.Count; i++)
            {
                CourierMotorcycleController moto = CourierManager.Instance.GetMotorcycleBySlot(contractMotorcycleSlots[i]);
                if (moto != null && moto.CanTakeContractOrder()) return moto;
            }
            return null;
        }

        private bool HasActiveContractOrder()
        {
            if (OnlineMarketOrderManager.Instance == null) return false;
            List<OnlineCustomerOrder> pending = OnlineMarketOrderManager.Instance.PendingOrders;
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i] != null && pending[i].isTownContract) return true;
            }
            return false;
        }

        private OnlineCustomerOrder BuildOrderFromOffer(TownContractOffer offer, CourierMotorcycleController moto)
        {
            TownContractPartner partner = GetPartner(offer.partnerId);
            if (partner == null) return null;

            OnlineCustomerOrder order = new OnlineCustomerOrder();
            order.orderId = offer.offerId;
            order.isTownContract = true;
            order.contractPartnerId = partner.id;
            order.contractOfferId = offer.offerId;
            order.customerName = partner.LocalizedName;
            order.customerHandle = "@" + partner.id;
            order.destinationNameTr = partner.nameTr;
            order.destinationNameEn = partner.nameEn;
            order.targetDoorstepPosition = partner.doorstepPos;
            order.courierDeliveryFee = Mathf.Clamp(Mathf.RoundToInt(offer.distanceMeters * 0.35f), 40, 140);
            order.assignedMotorcycle = moto;

            for (int i = 0; i < offer.productIds.Count; i++)
            {
                WholesaleProductDef prod = WholesaleDatabase.GetProductById(offer.productIds[i]);
                if (prod == null) continue;
                int qty = i < offer.quantities.Count ? Mathf.Max(1, offer.quantities[i]) : 1;
                order.requestedProducts.Add(prod);
                order.requestedQuantities.Add(qty);
                order.gatheredQuantities.Add(0);
                order.totalEstimatedValue += prod.SalePricePerUnit * qty;
            }

            if (order.requestedProducts.Count == 0) return null;
            moto.AssignOrderToCargo(order);
            return order;
        }

        private List<WholesaleProductDef> BuildProductPool(TownContractPartner partner)
        {
            int currentLevel = EnvironmentBuilder.Instance != null ? EnvironmentBuilder.Instance.CurrentUpgradeLevel : 1;
            List<WholesaleProductDef> all = WholesaleDatabase.GetAllProducts();
            List<GardenSeedDef> seeds = GardenSeedDatabase.GetAllSeeds();
            if (seeds != null)
            {
                for (int i = 0; i < seeds.Count; i++)
                {
                    WholesaleProductDef crop = WholesaleDatabase.GetProductById(seeds[i].id);
                    if (crop != null) all.Add(crop);
                }
            }

            List<WholesaleProductDef> preferred = new List<WholesaleProductDef>();
            List<WholesaleProductDef> fallback = new List<WholesaleProductDef>();
            for (int i = 0; i < all.Count; i++)
            {
                WholesaleProductDef prod = all[i];
                if (prod == null || prod.requiredLevel > currentLevel) continue;
                bool matchShelf = false;
                if (partner.preferredShelves != null)
                {
                    for (int s = 0; s < partner.preferredShelves.Length; s++)
                    {
                        if (prod.targetShelfType == partner.preferredShelves[s])
                        {
                            matchShelf = true;
                            break;
                        }
                    }
                }

                if (matchShelf || WholesaleDatabase.IsGourmetOrCrop(prod.id)) preferred.Add(prod);
                else if (prod.isOrderable) fallback.Add(prod);
            }

            return preferred.Count > 0 ? preferred : fallback;
        }

        private static bool OrderUsedLocalGoods(OnlineCustomerOrder order)
        {
            if (order == null) return false;
            for (int i = 0; i < order.requestedProducts.Count; i++)
            {
                WholesaleProductDef prod = order.requestedProducts[i];
                if (prod != null && WholesaleDatabase.IsGourmetOrCrop(prod.id)) return true;
            }
            return false;
        }

        private static TownContractOffer CloneOffer(TownContractOffer src)
        {
            TownContractOffer copy = new TownContractOffer
            {
                offerId = src.offerId,
                partnerId = src.partnerId,
                payout = src.payout,
                distanceMeters = src.distanceMeters,
                status = src.status,
                productIds = src.productIds != null ? new List<string>(src.productIds) : new List<string>(),
                quantities = src.quantities != null ? new List<int>(src.quantities) : new List<int>()
            };
            return copy;
        }
    }
}
