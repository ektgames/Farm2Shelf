using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Environment;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Her mevsimin 30. günü sabah 10:00'da takım elbiseli müfettiş dükkana müşteri gibi girer,
    /// rafları gezip not alır. Temizlik, stok ve fiyat uygunluğuna göre cüzi ödül veya ağır ceza kesilir.
    /// Rapor takvimde 30. günde görünür.
    /// </summary>
    public class SeasonalInspectorManager : MonoBehaviour
    {
        public static SeasonalInspectorManager Instance { get; private set; }

        public const int InspectorDay = 30;
        public const int ArrivalHour = 10;
        public const int DepartureHour = 11;
        public const int PassBonusEach = 120;
        public const int FailPenaltyEach = 450;

        private readonly List<InspectorVisitSaveData> visits = new List<InspectorVisitSaveData>();

        private GameObject inspectorObj;
        private List<Transform> leftLimbs;
        private List<Transform> rightLimbs;
        private List<Vector3> waypoints;
        private int waypointIndex;
        private float noteTimer;
        private float walkCycleTimer;
        private bool spawnedThisVisit;
        private bool settledThisVisit;
        private bool takingNotes;
        private bool waitingForDeparture;
        private int exitStartIndex;
        private string inspectorDisplayName = "Harun Yıldız";

        public IReadOnlyList<InspectorVisitSaveData> Visits => visits;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            BindListeners();
        }

        private void Start()
        {
            BindListeners();
            TrySpawnInspector();
        }

        private void BindListeners()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
                TimeManager.Instance.OnTimeUpdated += HandleTimeUpdated;
                TimeManager.Instance.OnMidnightRollover -= HandleMidnight;
                TimeManager.Instance.OnMidnightRollover += HandleMidnight;
                TimeManager.Instance.OnNewDayStarted -= HandleNewDay;
                TimeManager.Instance.OnNewDayStarted += HandleNewDay;
            }

            if (StoreStatusManager.Instance != null)
            {
                StoreStatusManager.Instance.OnStoreStatusChanged -= HandleStoreStatus;
                StoreStatusManager.Instance.OnStoreStatusChanged += HandleStoreStatus;
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeUpdated -= HandleTimeUpdated;
                TimeManager.Instance.OnMidnightRollover -= HandleMidnight;
                TimeManager.Instance.OnNewDayStarted -= HandleNewDay;
            }

            if (StoreStatusManager.Instance != null)
            {
                StoreStatusManager.Instance.OnStoreStatusChanged -= HandleStoreStatus;
            }

            if (Instance == this) Instance = null;
        }

        private void HandleNewDay(TimeManager.Season season, int day, int year)
        {
            if (day == 1)
            {
                int prevSeason = ((int)season + 3) % 4;
                int prevYear = ((int)season == 0) ? year - 1 : year;
                if (prevYear >= 1 && !HasCompletedVisit(prevYear, prevSeason))
                {
                    InspectorVisitSaveData missed = EvaluateStore(true);
                    missed.year = prevYear;
                    missed.season = prevSeason;
                    missed.completed = true;
                    missed.inspectorName = inspectorDisplayName;
                    visits.Add(missed);
                    ApplyPayout(missed);
                    ShowResultModal(missed);
                }
            }

            spawnedThisVisit = false;
            settledThisVisit = HasCompletedVisit(year, (int)season);
            if (day != InspectorDay)
            {
                DespawnInspector(false);
            }
        }

        private void HandleStoreStatus(bool isOpen)
        {
            if (isOpen) TrySpawnInspector();
        }

        private void HandleTimeUpdated(int hour, int minute)
        {
            TrySpawnInspector();
            if (waitingForDeparture && HasReachedDepartureTime())
            {
                BeginDepartureWalk();
            }
        }

        private void HandleMidnight()
        {
            if (!IsInspectorDayToday()) return;
            if (settledThisVisit || HasCompletedVisitForToday()) return;

            if (inspectorObj != null)
            {
                SettleInspection(false);
                DespawnInspector(false);
            }
            else
            {
                SettleInspection(true);
            }
        }

        public static bool IsInspectorCalendarDay(int day)
        {
            return day == InspectorDay;
        }

        public bool IsInspectorDayToday()
        {
            return TimeManager.Instance != null && TimeManager.Instance.Day == InspectorDay;
        }

        public InspectorVisitSaveData GetVisit(int year, TimeManager.Season season)
        {
            int seasonIdx = (int)season;
            for (int i = 0; i < visits.Count; i++)
            {
                InspectorVisitSaveData v = visits[i];
                if (v != null && v.year == year && v.season == seasonIdx && v.completed)
                {
                    return v;
                }
            }
            return null;
        }

        public bool HasCompletedVisit(int year, int season)
        {
            for (int i = 0; i < visits.Count; i++)
            {
                InspectorVisitSaveData v = visits[i];
                if (v != null && v.year == year && v.season == season && v.completed) return true;
            }
            return false;
        }

        private bool HasCompletedVisitForToday()
        {
            if (TimeManager.Instance == null) return false;
            return HasCompletedVisit(TimeManager.Instance.Year, (int)TimeManager.Instance.CurrentSeason);
        }

        private void TrySpawnInspector()
        {
            if (TimeManager.Instance == null) return;
            if (TimeManager.Instance.Day != InspectorDay) return;
            if (TimeManager.Instance.Hour < ArrivalHour) return;
            if (TimeManager.Instance.Hour >= DepartureHour) return;
            if (TimeManager.Instance.Hour >= 24) return;
            if (StoreStatusManager.Instance == null || !StoreStatusManager.Instance.IsOpen) return;
            if (spawnedThisVisit || inspectorObj != null) return;
            if (HasCompletedVisitForToday()) return;

            SpawnInspector();
        }

        private void SpawnInspector()
        {
            spawnedThisVisit = true;
            settledThisVisit = false;
            takingNotes = false;
            waitingForDeparture = false;
            noteTimer = 0f;
            waypointIndex = 1;
            walkCycleTimer = 0f;

            waypoints = BuildInspectionRoute();
            if (waypoints == null || waypoints.Count < 2)
            {
                waypoints = BuildFallbackRoute();
                exitStartIndex = Mathf.Max(1, waypoints.Count - 2);
            }

            inspectorObj = ProceduralCustomerModelBuilder.CreateCustomerModel(CustomerType.L3_SeasonalInspector, out leftLimbs, out rightLimbs);
            inspectorObj.name = "Seasonal_Health_Inspector";
            inspectorObj.transform.position = waypoints[0];
            inspectorObj.transform.rotation = Quaternion.identity;

            CustomerProfileData profile = CustomerProfileGenerator.GenerateProfile(CustomerType.L3_SeasonalInspector);
            profile.fullName = inspectorDisplayName;
            profile.occupation = LocalizationManager.L("Occ_Inspector", "Gıda ve Hijyen Müfettişi", "Food & Hygiene Inspector");
            profile.age = 44;
            profile.isFemale = false;
            profile.avatarEmoji = "🕴️";

            CustomerClickableTarget click = inspectorObj.GetComponent<CustomerClickableTarget>() ?? inspectorObj.AddComponent<CustomerClickableTarget>();
            click.profileData = profile;

            if (CalendarPopupUI.Instance != null && CalendarPopupUI.IsCalendarModalOpen)
            {
                CalendarPopupUI.Instance.RefreshInspectorMarkers();
            }
        }

        private List<Vector3> BuildInspectionRoute()
        {
            List<Vector3> route = new List<Vector3>
            {
                new Vector3(45.0f, 0.05f, -5.0f),
                new Vector3(-5.0f, 0.05f, -5.0f),
                new Vector3(-5.0f, 0.05f, -2.5f),
                new Vector3(-5.0f, 0.05f, -0.5f)
            };

            List<PlacedFurnitureController> shelves = PlacedFurnitureController.AllPlacedFurniture;
            int added = 0;
            if (shelves != null)
            {
                for (int i = 0; i < shelves.Count && added < 5; i++)
                {
                    PlacedFurnitureController f = shelves[i];
                    if (f == null || !IsRetailDisplay(f.FurnitureType)) continue;
                    route.Add(f.GetFrontInteractionPosition(0.95f));
                    added++;
                }
            }

            if (added == 0)
            {
                route.Add(new Vector3(-3.5f, 0.05f, 4.0f));
                route.Add(new Vector3(-6.0f, 0.05f, 8.0f));
            }

            Vector3 checkout = new Vector3(-6.5f, 0.05f, 1.5f);
            if (shelves != null)
            {
                for (int i = 0; i < shelves.Count; i++)
                {
                    if (shelves[i] != null && shelves[i].FurnitureType == FurnitureType.Cashier)
                    {
                        checkout = shelves[i].GetFrontInteractionPosition(1.0f);
                        break;
                    }
                }
            }
            route.Add(checkout);

            route.Add(new Vector3(-5.0f, 0.05f, -0.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -2.5f));
            route.Add(new Vector3(-5.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-45.0f, 0.05f, -5.0f));
            route.Add(new Vector3(-85.0f, 0.05f, -5.0f));
            exitStartIndex = Mathf.Max(1, route.Count - 5);
            return route;
        }

        private static List<Vector3> BuildFallbackRoute()
        {
            return new List<Vector3>
            {
                new Vector3(45.0f, 0.05f, -5.0f),
                new Vector3(-5.0f, 0.05f, -5.0f),
                new Vector3(-5.0f, 0.05f, -0.5f),
                new Vector3(-6.5f, 0.05f, 1.5f),
                new Vector3(-5.0f, 0.05f, -5.0f),
                new Vector3(-85.0f, 0.05f, -5.0f)
            };
        }

        public static bool IsRetailDisplay(FurnitureType type)
        {
            return type == FurnitureType.Shelf
                || type == FurnitureType.ProduceShelf
                || type == FurnitureType.Fridge
                || type == FurnitureType.OrganicFridge
                || type == FurnitureType.Freezer
                || type == FurnitureType.BakeryCounter
                || type == FurnitureType.ButcherCounter
                || type == FurnitureType.CosmeticShelf
                || type == FurnitureType.ElectronicsShelf
                || type == FurnitureType.GourmetShelf;
        }

        private void Update()
        {
            if (inspectorObj == null || waypoints == null || waypointIndex >= waypoints.Count) return;

            float dt = Time.deltaTime;
            if (waitingForDeparture)
            {
                ResetLimbs();
                if (HasReachedDepartureTime()) BeginDepartureWalk();
                return;
            }

            if (takingNotes)
            {
                noteTimer -= dt;
                ResetLimbs();
                if (noteTimer <= 0f)
                {
                    takingNotes = false;
                    OnInteriorStopFinished();
                }
                return;
            }

            Vector3 target = waypoints[waypointIndex];
            Vector3 pos = inspectorObj.transform.position;
            float dist = Vector3.Distance(pos, target);
            if (dist <= 0.22f)
            {
                bool interiorStop = waypointIndex >= 4 && waypointIndex < exitStartIndex;
                if (interiorStop)
                {
                    takingNotes = true;
                    noteTimer = 2.4f;
                    ShowNotePopup(inspectorObj.transform.position);
                }
                else
                {
                    waypointIndex++;
                    if (waypointIndex >= waypoints.Count)
                    {
                        if (!settledThisVisit) SettleInspection(false);
                        DespawnInspector(false);
                    }
                }
                return;
            }

            Vector3 dir = (target - pos);
            dir.y = 0f;
            inspectorObj.transform.position = Vector3.MoveTowards(pos, target, 2.6f * dt);
            if (dir.sqrMagnitude > 0.0001f)
            {
                inspectorObj.transform.rotation = Quaternion.Slerp(
                    inspectorObj.transform.rotation,
                    Quaternion.LookRotation(dir.normalized, Vector3.up),
                    10f * dt);
            }

            walkCycleTimer += dt * 8.2f;
            float leg = Mathf.Sin(walkCycleTimer) * 24f;
            if (leftLimbs != null)
            {
                for (int i = 0; i < leftLimbs.Count; i++)
                    if (leftLimbs[i] != null) leftLimbs[i].localRotation = Quaternion.Euler(leg, 0f, 0f);
            }
            if (rightLimbs != null)
            {
                for (int i = 0; i < rightLimbs.Count; i++)
                    if (rightLimbs[i] != null) rightLimbs[i].localRotation = Quaternion.Euler(-leg, 0f, 0f);
            }
        }

        private void OnInteriorStopFinished()
        {
            bool lastInteriorStop = waypointIndex >= exitStartIndex - 1 && waypointIndex < exitStartIndex;
            if (lastInteriorStop)
            {
                if (!settledThisVisit) SettleInspection(false);
                if (HasReachedDepartureTime())
                {
                    BeginDepartureWalk();
                }
                else
                {
                    waitingForDeparture = true;
                }
                return;
            }

            waypointIndex++;
            if (waypointIndex >= waypoints.Count)
            {
                if (!settledThisVisit) SettleInspection(false);
                DespawnInspector(false);
            }
        }

        private static bool HasReachedDepartureTime()
        {
            if (TimeManager.Instance == null) return false;
            return TimeManager.Instance.Hour >= DepartureHour;
        }

        private void BeginDepartureWalk()
        {
            waitingForDeparture = false;
            takingNotes = false;
            if (waypoints == null || inspectorObj == null) return;
            if (!settledThisVisit) SettleInspection(false);
            waypointIndex = Mathf.Clamp(exitStartIndex, 0, waypoints.Count - 1);
        }

        private void ResetLimbs()
        {
            if (leftLimbs != null)
            {
                for (int i = 0; i < leftLimbs.Count; i++)
                    if (leftLimbs[i] != null) leftLimbs[i].localRotation = Quaternion.identity;
            }
            if (rightLimbs != null)
            {
                for (int i = 0; i < rightLimbs.Count; i++)
                    if (rightLimbs[i] != null) rightLimbs[i].localRotation = Quaternion.identity;
            }
        }

        private void ShowNotePopup(Vector3 pos)
        {
            GameObject popup = new GameObject("Inspector_NotePopup");
            popup.transform.position = pos + Vector3.up * 2.15f;
            TextMesh tm = popup.AddComponent<TextMesh>();
            tm.text = LocalizationManager.L("Inspector_TakingNotes", "📝 Not alıyor...", "📝 Taking notes...");
            tm.fontSize = 28;
            tm.characterSize = 0.045f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.95f, 0.88f, 0.35f);
            Destroy(popup, 2.1f);
        }

        private void SettleInspection(bool storeWasClosed)
        {
            if (settledThisVisit) return;
            if (TimeManager.Instance == null) return;
            if (HasCompletedVisitForToday())
            {
                settledThisVisit = true;
                return;
            }

            settledThisVisit = true;

            InspectorVisitSaveData report = EvaluateStore(storeWasClosed);
            report.year = TimeManager.Instance.Year;
            report.season = (int)TimeManager.Instance.CurrentSeason;
            report.completed = true;
            report.inspectorName = inspectorDisplayName;

            visits.RemoveAll(v => v != null && v.year == report.year && v.season == report.season);
            visits.Add(report);

            ApplyPayout(report);
            ShowResultModal(report);

            if (CalendarPopupUI.Instance != null && CalendarPopupUI.IsCalendarModalOpen)
            {
                CalendarPopupUI.Instance.RefreshInspectorMarkers();
            }
        }

        public InspectorVisitSaveData EvaluateStore(bool storeWasClosed)
        {
            int trash = 0;
            if (StoreCleanlinessManager.Instance != null)
            {
                trash = StoreCleanlinessManager.Instance.ActiveTrashCount;
            }

            int stocked = 0;
            int empty = 0;
            int overpriced = 0;
            int priced = 0;

            List<PlacedFurnitureController> furniture = PlacedFurnitureController.AllPlacedFurniture;
            if (furniture != null)
            {
                for (int i = 0; i < furniture.Count; i++)
                {
                    PlacedFurnitureController f = furniture[i];
                    if (f == null || !IsRetailDisplay(f.FurnitureType) || f.rows == null) continue;
                    for (int r = 0; r < f.rows.Length; r++)
                    {
                        ShelfRowData row = f.rows[r];
                        if (row == null || row.IsUnassigned) continue;
                        if (row.currentStock > 0) stocked++;
                        else empty++;

                        WholesaleProductDef def = WholesaleDatabase.GetProductById(row.productId);
                        if (def == null) continue;
                        priced++;
                        if (def.IsOverpriced) overpriced++;
                    }
                }
            }

            bool cleanPass = !storeWasClosed && trash <= 0;
            bool shelfPass = !storeWasClosed && stocked > 0 && stocked >= empty;
            bool pricePass;
            if (storeWasClosed)
            {
                pricePass = false;
            }
            else if (priced > 0)
            {
                pricePass = overpriced * 2 <= priced;
            }
            else
            {
                pricePass = stocked > 0;
            }

            if (storeWasClosed)
            {
                cleanPass = false;
                shelfPass = false;
                pricePass = false;
            }

            int passCount = (cleanPass ? 1 : 0) + (shelfPass ? 1 : 0) + (pricePass ? 1 : 0);
            int net = passCount * PassBonusEach - (3 - passCount) * FailPenaltyEach;

            InspectorVisitSaveData data = new InspectorVisitSaveData
            {
                cleanlinessPassed = cleanPass,
                shelvesPassed = shelfPass,
                pricesPassed = pricePass,
                passCount = passCount,
                netAmount = net,
                trashCount = trash,
                stockedRows = stocked,
                emptyRows = empty,
                overpricedCount = overpriced,
                pricedProductCount = priced
            };

            if (storeWasClosed)
            {
                data.noteCleanTr = "Dükkan kapalıydı; hijyen denetimi yapılamadı.";
                data.noteCleanEn = "Store was closed; hygiene could not be inspected.";
                data.noteShelfTr = "Kapalı dükkanda reyon kontrolü yapılamadı.";
                data.noteShelfEn = "Shelves could not be checked while the store was closed.";
                data.notePriceTr = "Fiyat etiketleri görülemedi.";
                data.notePriceEn = "Price tags could not be reviewed.";
            }
            else
            {
                data.noteCleanTr = cleanPass
                    ? "Zemin temiz, çöp/leke yok."
                    : string.Format("Dükkan kirli: {0} çöp/leke bulundu.", trash);
                data.noteCleanEn = cleanPass
                    ? "Floors are clean, no litter found."
                    : string.Format("Store is dirty: {0} litter stain(s) found.", trash);

                data.noteShelfTr = shelfPass
                    ? string.Format("Raflar yeterince dolu ({0} dolu / {1} boş sıra).", stocked, empty)
                    : (stocked == 0
                        ? "Teşhir rafları boş veya ürün atanmamış."
                        : string.Format("Raflar yetersiz: {0} dolu, {1} boş sıra.", stocked, empty));
                data.noteShelfEn = shelfPass
                    ? string.Format("Shelves are adequately stocked ({0} full / {1} empty rows).", stocked, empty)
                    : (stocked == 0
                        ? "Display shelves are empty or unassigned."
                        : string.Format("Shelves are thin: {0} full, {1} empty rows.", stocked, empty));

                data.notePriceTr = pricePass
                    ? "Satış fiyatları makul aralıkta."
                    : (priced == 0
                        ? "Fiyatı denetlenecek ürün bulunamadı."
                        : string.Format("{0}/{1} ürün aşırı pahalı etiketlenmiş.", overpriced, priced));
                data.notePriceEn = pricePass
                    ? "Sale prices are within a fair range."
                    : (priced == 0
                        ? "No priced products were available to review."
                        : string.Format("{0}/{1} products are overpriced.", overpriced, priced));
            }

            data.summaryTr = string.Format("Sonuç: {0}/3 madde uygun. Net: {1:+#;-#;0}C", passCount, net);
            data.summaryEn = string.Format("Result: {0}/3 checks passed. Net: {1:+#;-#;0}C", passCount, net);
            return data;
        }

        private void ApplyPayout(InspectorVisitSaveData report)
        {
            if (report == null || EconomyManager.Instance == null) return;

            int net = report.netAmount;
            string desc = LocalizationManager.L(
                "Inspector_LedgerDesc",
                string.Format("Mevsim sonu müfettiş denetimi ({0}/3)", report.passCount),
                string.Format("Season-end inspector visit ({0}/3)", report.passCount));

            if (net > 0)
            {
                EconomyManager.Instance.AddCredits(net);
                FinanceManager.Instance?.RecordIncome(FinanceCategories.Inspection, desc, net);
            }
            else if (net < 0)
            {
                EconomyManager.Instance.ForceDeductCredits(-net);
                FinanceManager.Instance?.RecordExpense(FinanceCategories.Inspection, desc, -net);
            }
        }

        private void ShowResultModal(InspectorVisitSaveData report)
        {
            if (report == null) return;
            bool en = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            string body = BuildReportBody(report, en);
            string title = LocalizationManager.L("Inspector_ReportTitle", "🕴️ Müfettiş Raporu", "🕴️ Inspector Report");
            ModalManager.ShowModal(title, body, LocalizationManager.L("Btn_OK", "Tamam", "OK"));
        }

        public static string BuildReportBody(InspectorVisitSaveData report, bool english)
        {
            if (report == null) return "";
            string clean = english ? report.noteCleanEn : report.noteCleanTr;
            string shelf = english ? report.noteShelfEn : report.noteShelfTr;
            string price = english ? report.notePriceEn : report.notePriceTr;
            string summary = english ? report.summaryEn : report.summaryTr;
            string pass = english ? "Pass" : "Uygun";
            string fail = english ? "Fail" : "Uygun değil";

            return string.Format(
                "{0}\n\n🧹 {1}: {2}\n{3}\n\n📦 {4}: {5}\n{6}\n\n💰 {7}: {8}\n{9}\n\n<b>{10}</b>",
                english ? "Season-end food inspector notes:" : "Mevsim sonu gıda müfettişi notları:",
                english ? "Cleanliness" : "Temizlik",
                report.cleanlinessPassed ? pass : fail,
                clean,
                english ? "Shelves" : "Raflar",
                report.shelvesPassed ? pass : fail,
                shelf,
                english ? "Prices" : "Fiyatlar",
                report.pricesPassed ? pass : fail,
                price,
                summary);
        }

        private void DespawnInspector(bool settleIfNeeded)
        {
            waitingForDeparture = false;
            takingNotes = false;
            if (settleIfNeeded && !settledThisVisit && IsInspectorDayToday())
            {
                SettleInspection(false);
            }

            if (inspectorObj != null)
            {
                Destroy(inspectorObj);
                inspectorObj = null;
            }
            leftLimbs = null;
            rightLimbs = null;
            waypoints = null;
        }

        public List<InspectorVisitSaveData> ExportForSave()
        {
            return new List<InspectorVisitSaveData>(visits);
        }

        public void RestoreFromSave(List<InspectorVisitSaveData> saved)
        {
            visits.Clear();
            DespawnInspector(false);
            spawnedThisVisit = false;
            settledThisVisit = false;
            if (saved != null)
            {
                for (int i = 0; i < saved.Count; i++)
                {
                    if (saved[i] != null) visits.Add(saved[i]);
                }
            }

            if (HasCompletedVisitForToday())
            {
                spawnedThisVisit = true;
                settledThisVisit = true;
            }
        }

        public void ResetToDefaults()
        {
            visits.Clear();
            spawnedThisVisit = false;
            settledThisVisit = false;
            DespawnInspector(false);
        }
    }
}
