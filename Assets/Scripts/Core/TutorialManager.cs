using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;
using Farm2Shelf.UI;

namespace Farm2Shelf.Core
{
    public enum TutorialStep
    {
        None = 0,
        Step1_CameraControls = 1,
        Step2_ExploreTabletApps = 2,
        Step3_HireStoreStaffAndCallEarly = 3,
        Step4_AssignStoreShifts = 4,
        Step5_BuyInitialFurniture = 5,
        Step6_UnpackAndPlaceFurniture = 6,
        Step7_PlaceWholesaleBulkOrder = 7,
        Step8_HireFarmStaffAndShifts = 8,
        Step9_BuyStartingSeeds = 9,
        Step10_PlantSeedsAndOpenStore = 10,
        Completed = 11
    }

    /// <summary>
    /// Farm2Shelf 10 Adımlı İnteraktif Başlangıç ve Eğitim (Tutorial) Yöneticisi.
    /// Mobil dokunmatik kontrollerini, mağaza yönetimini, personel alımını, vardiyaları,
    /// mobilya kurulumunu, toptancı toplu siparişini ve reyon ürün dizimini,
    /// çiftlik yönetimini, tohum ekimini ve dükkan açılışını adım adım öğreten %100 çift dilli tam entegre sistem.
    /// </summary>
    public partial class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        public TutorialStep CurrentStep { get; private set; } = TutorialStep.None;
        public bool IsTutorialActive => CurrentStep != TutorialStep.None && CurrentStep != TutorialStep.Completed;

        // Step 1: Kamera Kontrolleri
        public bool DidPanCamera { get; private set; }
        public bool DidZoomCamera { get; private set; }
        public bool DidRotateCamera { get; private set; }

        // Step 2: Tablet Uygulamaları Keşfi
        private HashSet<int> exploredApps = new HashSet<int>();

        // Step 3: Personel & Erken Çağır
        public bool DidCallRestockerEarly { get; private set; }

        // Step 5: Mobilya Sepet Alımı
        private int boughtShelves = 0;
        private int boughtCartStands = 0;
        private int boughtStorageShelves = 0;
        private int boughtCashiers = 0;
        private int boughtFridges = 0;
        public bool DidCheckoutFurniture { get; private set; }

        // Step 6: Mobilya Yerleşimi
        public int TotalFurniturePlacedInTutorial { get; private set; }

        // Step 7: Toplu Sipariş & Reyon Dizimi
        public bool DidPlaceBulkOrder { get; private set; }
        public bool DidAcceptTownContract { get; private set; }

        // Step 9: Tohum Alımı
        public bool DidBuyTomatoSeed { get; private set; }
        public bool DidBuyCucumberSeed { get; private set; }
        public bool DidBuyLettuceSeed { get; private set; }

        // Step 10: Tohum Ekimi & Dükkan Açılışı
        public int CropsPlantedInTutorial { get; private set; }
        public bool DidOpenStoreInTutorial { get; private set; }

        public event Action<TutorialStep> OnTutorialStepChanged;
        public event Action OnTutorialProgressUpdated;

        private float checkTimer = 0f;
        private float autoAdvanceTimer = 0f;
        private bool didShowCompletionModal = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (StoreStatusManager.Instance != null)
            {
                StoreStatusManager.Instance.OnStoreStatusChanged += HandleStoreStatusChanged;
            }
        }

        private void OnDestroy()
        {
            if (StoreStatusManager.Instance != null)
            {
                StoreStatusManager.Instance.OnStoreStatusChanged -= HandleStoreStatusChanged;
            }
        }

        private void Update()
        {
            if (!IsTutorialActive) return;

            checkTimer += Time.unscaledDeltaTime;
            if (checkTimer >= 0.35f)
            {
                checkTimer = 0f;
                TutorialQuestTrackerUI.RefreshDisplay();

                if (IsCurrentStepComplete())
                {
                    autoAdvanceTimer += 0.35f;
                    // Tüm alt hedefler bittiğinde 1.2 saniye sonra otomatik geçiş
                    if (autoAdvanceTimer >= 1.2f)
                    {
                        autoAdvanceTimer = 0f;
                        AdvanceToNextStep();
                    }
                }
                else
                {
                    autoAdvanceTimer = 0f;
                }
            }
        }

        // ==================== EĞİTİM BAŞLATMA / İPTAL ====================

        public void PromptTutorialOnNewGame()
        {
            ResetProgress();
            SetStep(TutorialStep.None);
            TutorialMentorGuideUI.HideGuide();
            ShowTutorialPromptModal();
        }

        public void ShowTutorialPromptModal()
        {
            MayorWelcomeUI.Show(
                onAccept: () => {
                    StartTutorial();
                },
                onDecline: () => {
                    SkipTutorial();
                }
            );
        }

        public void StartTutorial()
        {
            ResetProgress();
            SetStep(TutorialStep.Step1_CameraControls);
            TutorialQuestTrackerUI.ShowTracker();
            TutorialMentorGuideUI.ShowGuide(false);
            Debug.Log("[TutorialManager] Eğitim Bölümü Başlatıldı! Adım: Step 1 (Kamera Kontrolleri)");
        }

        public void RequestSkipTutorial()
        {
            if (!IsTutorialActive) return;
            if (TutorialPromptModalUI.IsPromptOpen) return;

            TutorialPromptModalUI.ShowSkipConfirmModal(
                onConfirmSkip: SkipTutorial,
                onStay: null);
        }

        public void SkipTutorial()
        {
            didShowCompletionModal = true;
            CurrentStep = TutorialStep.Completed;
            TutorialQuestTrackerUI.HideTracker();
            TutorialMentorGuideUI.HideGuide();
            OnTutorialStepChanged?.Invoke(CurrentStep);
            Debug.Log("[TutorialManager] Eğitim atlandı / serbest oyun modu aktif.");
        }

        public void SetStep(TutorialStep step)
        {
            TutorialStep prevStep = CurrentStep;
            CurrentStep = step;
            autoAdvanceTimer = 0f;
            OnTutorialStepChanged?.Invoke(CurrentStep);
            OnTutorialProgressUpdated?.Invoke();

            if (step == TutorialStep.Completed)
            {
                TutorialQuestTrackerUI.HideTracker();
                TutorialMentorGuideUI.HideGuide();
                // Sadece Step 10'u fiilen tamamlayarak bitişe ulaşıldıysa tebrikler modalını göster
                if (prevStep == TutorialStep.Step10_PlantSeedsAndOpenStore && !didShowCompletionModal)
                {
                    didShowCompletionModal = true;
                    ShowCompletionModal();
                }
            }
            else if (step == TutorialStep.None)
            {
                TutorialQuestTrackerUI.HideTracker();
            }
            else
            {
                TutorialQuestTrackerUI.ShowTracker();
                TutorialQuestTrackerUI.RefreshDisplay();
                // Eğer bu adıma girildiğinde şartlar zaten sağlanmışsa hemen doğrula
                CheckCurrentStepCompletion(false);
            }
        }

        public void AdvanceToNextStep()
        {
            if (!IsTutorialActive) return;

            if (CurrentStep >= TutorialStep.Step10_PlantSeedsAndOpenStore)
            {
                SetStep(TutorialStep.Completed);
            }
            else
            {
                SetStep(CurrentStep + 1);
            }
        }

        private void ResetProgress()
        {
            DidPanCamera = false;
            DidZoomCamera = false;
            DidRotateCamera = false;
            exploredApps.Clear();
            journeyPurchases.Clear(); journeyPlants.Clear(); earlyStaffIds.Clear();
            PlantingFinished = false;
            ChickenVisited = CowVisited = BarnVisited = false; TourStop = 0;
            DidCallRestockerEarly = false;
            boughtShelves = 0;
            boughtCartStands = 0;
            boughtStorageShelves = 0;
            boughtCashiers = 0;
            boughtFridges = 0;
            DidCheckoutFurniture = false;
            TotalFurniturePlacedInTutorial = 0;
            DidPlaceBulkOrder = false;
            DidAcceptTownContract = false;
            DidBuyTomatoSeed = false;
            DidBuyCucumberSeed = false;
            DidBuyLettuceSeed = false;
            CropsPlantedInTutorial = 0;
            DidOpenStoreInTutorial = false;
            autoAdvanceTimer = 0f;
            didShowCompletionModal = false;
        }

        // ==================== MERKEZİ DOĞRULAMA (STEP EVALUATION) ====================

        public bool IsCurrentStepComplete()
        {
            return IsStepComplete(CurrentStep);
        }

        public bool IsStepComplete(TutorialStep step)
        {
            return JourneyComplete((int)step);
        }

        public void CheckCurrentStepCompletion(bool allowImmediateAdvance = true)
        {
            if (!IsTutorialActive) return;

            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            if (allowImmediateAdvance && IsCurrentStepComplete())
            {
                AdvanceToNextStep();
            }
        }

        // ==================== AKSİYON TETİKLEYİCİLERİ ====================

        public void NotifyCameraPan()
        {
            if (CurrentStep != TutorialStep.Step1_CameraControls || DidPanCamera) return;
            DidPanCamera = true;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyCameraZoom()
        {
            if (CurrentStep != TutorialStep.Step1_CameraControls || DidZoomCamera) return;
            DidZoomCamera = true;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyCameraRotate()
        {
            if (CurrentStep != TutorialStep.Step1_CameraControls || DidRotateCamera) return;
            DidRotateCamera = true;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyAppOpened(int appIndex)
        {
            if (CurrentStep != TutorialStep.Step2_ExploreTabletApps) return;
            exploredApps.Add(appIndex);
            CheckCurrentStepCompletion(false);
        }

        public bool IsAppExplored(int appIndex) => exploredApps.Contains(appIndex);
        public int ExploredAppsCount => exploredApps.Count;

        public void NotifyStaffHired(StaffRole role)
        {
            if (JourneyStep != 3 && JourneyStep != 4 && JourneyStep != 7) return;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyStaffCalledEarly()
        {
            if (CurrentStep == TutorialStep.Step3_HireStoreStaffAndCallEarly)
            {
                DidCallRestockerEarly = true;
                CheckCurrentStepCompletion(false);
            }
        }

        public void NotifyStaffShiftChanged()
        {
            if (JourneyStep != 3 && JourneyStep != 4 && JourneyStep != 7) return;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyFurnitureItemPurchased(FurnitureType type, int count)
        {
            if (JourneyStep != 5 || count <= 0 || System.Array.IndexOf(RequiredFurniture, type) < 0) return;
            journeyPurchases[type] = Purchased(type) + count;
            DidCheckoutFurniture = true;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyFurniturePlaced(FurnitureType type)
        {
            if (JourneyStep != 5) return;

            TotalFurniturePlacedInTutorial++;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyBulkOrderPlaced()
        {
            if (JourneyStep != 6) return;

            DidPlaceBulkOrder = true;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyProductAssignedToShelf()
        {
            if (JourneyStep != 6) return;

            CheckCurrentStepCompletion(false);
        }

        public void NotifyTownContractQueued()
        {
            if (JourneyStep != 3) return;
            DidAcceptTownContract = true;
            CheckCurrentStepCompletion(false);
        }

        public bool HasAcceptedTownContract()
        {
            if (DidAcceptTownContract) return true;
            if (TownContractManager.Instance == null) return false;
            if (TownContractManager.Instance.QueueOfferIds != null && TownContractManager.Instance.QueueOfferIds.Count > 0) return true;
            if (TownContractManager.Instance.SuccessCount > 0) return true;
            var offers = TownContractManager.Instance.Offers;
            if (offers == null) return false;
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i] == null) continue;
                if (offers[i].status == TownContractOfferStatus.Queued
                    || offers[i].status == TownContractOfferStatus.Active
                    || offers[i].status == TownContractOfferStatus.Completed)
                {
                    return true;
                }
            }
            return false;
        }

        public int GetMaxAssignedRowsOnAnyShelf()
        {
            int maxAssigned = 0;
            if (PlacedFurnitureController.AllPlacedFurniture != null)
            {
                foreach (var f in PlacedFurnitureController.AllPlacedFurniture)
                {
                    if (f == null || f.FurnitureType != FurnitureType.Shelf || f.rows == null) continue;
                    int count = 0;
                    for (int i = 0; i < f.rows.Length; i++)
                    {
                        if (f.rows[i] != null && !f.rows[i].IsUnassigned && !string.IsNullOrEmpty(f.rows[i].productName))
                        {
                            count++;
                        }
                    }
                    if (count > maxAssigned) maxAssigned = count;
                }
            }
            return Mathf.Min(4, maxAssigned);
        }

        public int GetMaxAssignedRowsOnAnyFridge()
        {
            int maxAssigned = 0;
            if (PlacedFurnitureController.AllPlacedFurniture != null)
            {
                foreach (var f in PlacedFurnitureController.AllPlacedFurniture)
                {
                    if (f == null || f.FurnitureType != FurnitureType.Fridge || f.rows == null) continue;
                    int count = 0;
                    for (int i = 0; i < f.rows.Length; i++)
                    {
                        if (f.rows[i] != null && !f.rows[i].IsUnassigned && !string.IsNullOrEmpty(f.rows[i].productName))
                        {
                            count++;
                        }
                    }
                    if (count > maxAssigned) maxAssigned = count;
                }
            }
            return Mathf.Min(4, maxAssigned);
        }

        public void NotifySeedPurchased(string seedId, int count)
        {
            if (JourneyStep != 7 || count <= 0) return;

            if (seedId.Contains("tomato")) DidBuyTomatoSeed = true;
            if (seedId.Contains("cucumber")) DidBuyCucumberSeed = true;
            if (seedId.Contains("lettuce")) DidBuyLettuceSeed = true;

            CheckCurrentStepCompletion(false);
        }

        public void NotifyCropPlanted(string seedId)
        {
            if (JourneyStep != 7) return;

            journeyPlants[seedId] = Planted(seedId) + 1;
            CropsPlantedInTutorial++;
            CheckCurrentStepCompletion(false);
        }

        private void HandleStoreStatusChanged(bool isOpen)
        {
            if (isOpen && CurrentStep == TutorialStep.Step10_PlantSeedsAndOpenStore)
            {
                DidOpenStoreInTutorial = true;
                CheckCurrentStepCompletion(false);
            }
        }

        // ==================== YARDIMCI SORGULAR ====================

        public int GetStoreRoleCount(StaffRole role)
        {
            if (StaffManager.Instance == null) return 0;
            var list = StaffManager.Instance.GetActiveStaff();
            if (list == null) return 0;
            return list.FindAll(s => s != null && s.role == role).Count;
        }

        public int GetFarmRoleCount(StaffRole role)
        {
            if (StaffManager.Instance == null) return 0;
            var list = StaffManager.Instance.GetFarmStaffList();
            if (list == null) return 0;
            return list.FindAll(s => s != null && s.role == role).Count;
        }

        public bool HasStoreShift(string keyword)
        {
            if (StaffManager.Instance == null) return false;
            var staff = StaffManager.Instance.GetActiveStaff();
            if (staff == null || staff.Count == 0) return false;
            return staff.Exists(s => s != null && s.shiftHours != null && s.shiftHours.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public bool HasFarmShift(string keyword)
        {
            if (StaffManager.Instance == null) return false;
            var staff = StaffManager.Instance.GetFarmStaffList();
            if (staff == null || staff.Count == 0) return false;
            return staff.Exists(s => s != null && s.shiftHours != null && s.shiftHours.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public int GetBoughtCount(FurnitureType type)
        {
            return Purchased(type);
        }

        private void ShowCompletionModal()
        {
            TutorialQuestTrackerUI.HideTracker();
            ModalManager.ShowModal(
                LocalizationManager.L("Mayor_Complete_Title", "Tebrikler! Eğitim tamamlandı", "Congratulations! Tutorial completed"),
                LocalizationManager.L("Mayor_Complete_Body",
                    "Tableti keşfettin; kontratını, kurye ve mağaza ekiplerini hazırladın. Mobilyalarını kurdun, raflarını ürünlerle eşleştirdin, çiftçilerini görevlendirdin ve 30 tohum ektin. Hayvan çiftliklerini ve kasabanın üretim ağını tanıdın. Mağazan artık açık. Bundan sonra kararlar senin!",
                    "You explored the tablet, arranged a contract and staffed your courier and store teams. You installed furniture, assigned shelf products, scheduled farmers and planted 30 seeds. You visited the animal farms and the town production network. Your store is now open. The next decisions are yours!"),
                LocalizationManager.L("Mayor_Complete_Button", "Oyuna devam et", "Continue playing"));
        }

        public void RestoreTutorialStep(TutorialStep step)
        {
            CurrentStep = step;
            if (CurrentStep == TutorialStep.Completed)
            {
                didShowCompletionModal = true;
            }

            if (CurrentStep == TutorialStep.None || CurrentStep == TutorialStep.Completed)
            {
                TutorialQuestTrackerUI.HideTracker();
            }
            else
            {
                TutorialQuestTrackerUI.ShowTracker();
                TutorialMentorGuideUI.ShowGuide(false);
                OnTutorialStepChanged?.Invoke(CurrentStep);
                OnTutorialProgressUpdated?.Invoke();
            }
        }
    }
}
