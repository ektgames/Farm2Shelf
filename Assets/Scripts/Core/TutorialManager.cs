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
    public class TutorialManager : MonoBehaviour
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
            ShowTutorialPromptModal();
        }

        public void ShowTutorialPromptModal()
        {
            TutorialPromptModalUI.ShowModal(
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
            Debug.Log("[TutorialManager] Eğitim Bölümü Başlatıldı! Adım: Step 1 (Kamera Kontrolleri)");
        }

        public void SkipTutorial()
        {
            didShowCompletionModal = true;
            CurrentStep = TutorialStep.Completed;
            TutorialQuestTrackerUI.HideTracker();
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
            DidCallRestockerEarly = false;
            boughtShelves = 0;
            boughtCartStands = 0;
            boughtStorageShelves = 0;
            boughtCashiers = 0;
            boughtFridges = 0;
            DidCheckoutFurniture = false;
            TotalFurniturePlacedInTutorial = 0;
            DidPlaceBulkOrder = false;
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
            switch (step)
            {
                case TutorialStep.Step1_CameraControls:
                    return DidPanCamera && DidZoomCamera && DidRotateCamera;

                case TutorialStep.Step2_ExploreTabletApps:
                    return ExploredAppsCount >= 5;

                case TutorialStep.Step3_HireStoreStaffAndCallEarly:
                    return GetStoreRoleCount(StaffRole.Kasiyer) >= 2 &&
                           GetStoreRoleCount(StaffRole.Reyoncu) >= 2 &&
                           DidCallRestockerEarly;

                case TutorialStep.Step4_AssignStoreShifts:
                    bool shMorn = HasStoreShift("Sabah") || HasStoreShift("08:00") || HasStoreShift("Gündüz") || HasStoreShift("Morning");
                    bool shEve = HasStoreShift("Akşam") || HasStoreShift("16:00 - 24:00") || HasStoreShift("24:00") || HasStoreShift("Gece") || HasStoreShift("Evening");
                    return shMorn && shEve;

                case TutorialStep.Step5_BuyInitialFurniture:
                    int sh = GetBoughtCount(FurnitureType.Shelf);
                    int cs = GetBoughtCount(FurnitureType.ShoppingCart);
                    int st = GetBoughtCount(FurnitureType.StorageShelf);
                    int ca = GetBoughtCount(FurnitureType.Cashier);
                    int fr = GetBoughtCount(FurnitureType.Fridge);
                    bool targetBought = (sh >= 3 && cs >= 1 && st >= 3 && ca >= 1 && fr >= 2);
                    bool hasPlacedOrCheckedOut = DidCheckoutFurniture || (PlacedFurnitureController.AllPlacedFurniture != null && PlacedFurnitureController.AllPlacedFurniture.Count >= 8);
                    return targetBought || hasPlacedOrCheckedOut;

                case TutorialStep.Step6_UnpackAndPlaceFurniture:
                    return TotalFurniturePlacedInTutorial >= 8 ||
                           (PlacedFurnitureController.AllPlacedFurniture != null && PlacedFurnitureController.AllPlacedFurniture.Count >= 8);

                case TutorialStep.Step7_PlaceWholesaleBulkOrder:
                    return DidPlaceBulkOrder &&
                           GetMaxAssignedRowsOnAnyShelf() >= 4 &&
                           GetMaxAssignedRowsOnAnyFridge() >= 4;

                case TutorialStep.Step8_HireFarmStaffAndShifts:
                    int farmers = GetFarmRoleCount(StaffRole.Çiftçi);
                    bool fMorn = HasFarmShift("Sabah") || HasFarmShift("08:00") || HasFarmShift("Gündüz") || HasFarmShift("Morning");
                    bool fEve = HasFarmShift("Akşam") || HasFarmShift("16:00 - 24:00") || HasFarmShift("24:00") || HasFarmShift("Gece") || HasFarmShift("Evening");
                    return farmers >= 2 && fMorn && fEve;

                case TutorialStep.Step9_BuyStartingSeeds:
                    bool hasTomato = DidBuyTomatoSeed || (GardenSeedInventoryManager.Instance != null && GardenSeedInventoryManager.Instance.GetSeedCount("spring_tomato") > 0);
                    bool hasCucumber = DidBuyCucumberSeed || (GardenSeedInventoryManager.Instance != null && GardenSeedInventoryManager.Instance.GetSeedCount("spring_cucumber") > 0);
                    bool hasLettuce = DidBuyLettuceSeed || (GardenSeedInventoryManager.Instance != null && GardenSeedInventoryManager.Instance.GetSeedCount("spring_lettuce") > 0);
                    return hasTomato && hasCucumber && hasLettuce;

                case TutorialStep.Step10_PlantSeedsAndOpenStore:
                    bool hasPlanted = CropsPlantedInTutorial >= 1 || (FieldPlotController.AllPlots != null && FieldPlotController.AllPlots.Exists(p => p != null && p.State != PlotState.Empty));
                    bool isStoreOpen = DidOpenStoreInTutorial || (StoreStatusManager.Instance != null && StoreStatusManager.Instance.IsOpen);
                    return hasPlanted && isStoreOpen;

                case TutorialStep.Completed:
                    return true;

                default:
                    return false;
            }
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
            if (CurrentStep != TutorialStep.Step1_CameraControls) return;
            DidPanCamera = true;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyCameraZoom()
        {
            if (CurrentStep != TutorialStep.Step1_CameraControls) return;
            DidZoomCamera = true;
            CheckCurrentStepCompletion(false);
        }

        public void NotifyCameraRotate()
        {
            if (CurrentStep != TutorialStep.Step1_CameraControls) return;
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
            if (CurrentStep != TutorialStep.Step3_HireStoreStaffAndCallEarly && CurrentStep != TutorialStep.Step8_HireFarmStaffAndShifts) return;
            CheckCurrentStepCompletion(true);
        }

        public void NotifyStaffCalledEarly()
        {
            if (CurrentStep == TutorialStep.Step3_HireStoreStaffAndCallEarly)
            {
                DidCallRestockerEarly = true;
                CheckCurrentStepCompletion(true);
            }
        }

        public void NotifyStaffShiftChanged()
        {
            if (CurrentStep != TutorialStep.Step4_AssignStoreShifts && CurrentStep != TutorialStep.Step8_HireFarmStaffAndShifts) return;
            CheckCurrentStepCompletion(true);
        }

        public void NotifyFurnitureItemPurchased(FurnitureType type, int count)
        {
            if (CurrentStep != TutorialStep.Step5_BuyInitialFurniture) return;

            switch (type)
            {
                case FurnitureType.Shelf: boughtShelves += count; break;
                case FurnitureType.ShoppingCart: boughtCartStands += count; break;
                case FurnitureType.StorageShelf: boughtStorageShelves += count; break;
                case FurnitureType.Cashier: boughtCashiers += count; break;
                case FurnitureType.Fridge: boughtFridges += count; break;
            }

            DidCheckoutFurniture = true;
            CheckCurrentStepCompletion(true);
        }

        public void NotifyFurniturePlaced(FurnitureType type)
        {
            if (CurrentStep != TutorialStep.Step6_UnpackAndPlaceFurniture) return;

            TotalFurniturePlacedInTutorial++;
            CheckCurrentStepCompletion(true);
        }

        public void NotifyBulkOrderPlaced()
        {
            if (CurrentStep != TutorialStep.Step7_PlaceWholesaleBulkOrder) return;

            DidPlaceBulkOrder = true;
            CheckCurrentStepCompletion(true);
        }

        public void NotifyProductAssignedToShelf()
        {
            if (CurrentStep != TutorialStep.Step7_PlaceWholesaleBulkOrder) return;

            CheckCurrentStepCompletion(true);
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
            if (CurrentStep != TutorialStep.Step9_BuyStartingSeeds) return;

            if (seedId.Contains("tomato")) DidBuyTomatoSeed = true;
            if (seedId.Contains("cucumber")) DidBuyCucumberSeed = true;
            if (seedId.Contains("lettuce")) DidBuyLettuceSeed = true;

            CheckCurrentStepCompletion(true);
        }

        public void NotifyCropPlanted(string seedId)
        {
            if (CurrentStep != TutorialStep.Step10_PlantSeedsAndOpenStore) return;

            CropsPlantedInTutorial++;
            CheckCurrentStepCompletion(true);
        }

        private void HandleStoreStatusChanged(bool isOpen)
        {
            if (isOpen && CurrentStep == TutorialStep.Step10_PlantSeedsAndOpenStore)
            {
                DidOpenStoreInTutorial = true;
                CheckCurrentStepCompletion(true);
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
            switch (type)
            {
                case FurnitureType.Shelf: return boughtShelves;
                case FurnitureType.ShoppingCart: return boughtCartStands;
                case FurnitureType.StorageShelf: return boughtStorageShelves;
                case FurnitureType.Cashier: return boughtCashiers;
                case FurnitureType.Fridge: return boughtFridges;
                default: return 0;
            }
        }

        private void ShowCompletionModal()
        {
            TutorialQuestTrackerUI.HideTracker();

            string title = LocalizationManager.L("Tutorial_End_Title", "🎉 TEBRİKLER! EĞİTİM BİTTİ", "🎉 CONGRATULATIONS! TUTORIAL COMPLETED");
            string body = LocalizationManager.L(
                "Tutorial_End_Body",
                "<b>Harika bir iş çıkardın! 🚀</b>\n\n" +
                "• Temel kontrolleri ve tablet uygulamalarını öğrendin,\n" +
                "• Mağaza ve çiftlik personellerini alıp vardiyalarını düzenledin,\n" +
                "• Reyonları kurup toptancıdan ilk toplu ürün siparişini verdin,\n" +
                "• Tarlaya tohumları ektin ve dükkanın kapılarını müşterilere açtın!\n\n" +
                "Artık dükkanını devasa bir süpermarkete dönüştürmek senin elinde. Bol kazançlar ve iyi eğlenceler!",
                "<b>Awesome job! 🚀</b>\n\n" +
                "• Learned camera controls & tablet applications,\n" +
                "• Hired store & farm staff and configured their shifts,\n" +
                "• Placed shelves and ordered first wholesale bulk goods,\n" +
                "• Planted seeds on field plots and opened the store doors!\n\n" +
                "Now you're ready to grow your business into a massive supermarket empire. Have fun and enjoy!"
            );
            string btnText = LocalizationManager.L("Tutorial_End_Btn", "🚀 Harika! Oyuna Başla", "🚀 Awesome! Start Playing");

            ModalManager.ShowModal(title, body, btnText);
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
                OnTutorialStepChanged?.Invoke(CurrentStep);
                OnTutorialProgressUpdated?.Invoke();
            }
        }
    }
}
