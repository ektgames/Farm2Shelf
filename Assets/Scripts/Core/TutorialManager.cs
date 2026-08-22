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
            CurrentStep = TutorialStep.Completed;
            TutorialQuestTrackerUI.HideTracker();
            OnTutorialStepChanged?.Invoke(CurrentStep);
            Debug.Log("[TutorialManager] Eğitim atlandı / serbest oyun modu aktif.");
        }

        public void SetStep(TutorialStep step)
        {
            CurrentStep = step;
            OnTutorialStepChanged?.Invoke(CurrentStep);
            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            if (step == TutorialStep.Completed)
            {
                ShowCompletionModal();
            }
        }

        public void AdvanceToNextStep()
        {
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
        }

        // ==================== AKSİYON TETİKLEYİCİLERİ ====================

        public void NotifyCameraPan()
        {
            if (CurrentStep != TutorialStep.Step1_CameraControls) return;
            DidPanCamera = true;
            CheckStep1Completion();
        }

        public void NotifyCameraZoom()
        {
            if (CurrentStep != TutorialStep.Step1_CameraControls) return;
            DidZoomCamera = true;
            CheckStep1Completion();
        }

        public void NotifyCameraRotate()
        {
            if (CurrentStep != TutorialStep.Step1_CameraControls) return;
            DidRotateCamera = true;
            CheckStep1Completion();
        }

        private void CheckStep1Completion()
        {
            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();
        }

        public void NotifyAppOpened(int appIndex)
        {
            if (CurrentStep != TutorialStep.Step2_ExploreTabletApps) return;
            exploredApps.Add(appIndex);
            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            if (exploredApps.Count >= 5)
            {
                // 5 uygulama da ziyaret edildi
            }
        }

        public bool IsAppExplored(int appIndex) => exploredApps.Contains(appIndex);
        public int ExploredAppsCount => exploredApps.Count;

        public void NotifyStaffHired(StaffRole role)
        {
            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            if (CurrentStep == TutorialStep.Step3_HireStoreStaffAndCallEarly)
            {
                int cashiers = GetStoreRoleCount(StaffRole.Kasiyer);
                int restockers = GetStoreRoleCount(StaffRole.Reyoncu);
                if (cashiers >= 3 && restockers >= 3 && DidCallRestockerEarly)
                {
                    AdvanceToNextStep();
                }
            }
            else if (CurrentStep == TutorialStep.Step8_HireFarmStaffAndShifts)
            {
                int farmers = GetFarmRoleCount(StaffRole.Çiftçi);
                if (farmers >= 3)
                {
                    CheckFarmStaffCompletion();
                }
            }
        }

        public void NotifyStaffCalledEarly()
        {
            if (CurrentStep == TutorialStep.Step3_HireStoreStaffAndCallEarly)
            {
                DidCallRestockerEarly = true;
                OnTutorialProgressUpdated?.Invoke();
                TutorialQuestTrackerUI.RefreshDisplay();

                int cashiers = GetStoreRoleCount(StaffRole.Kasiyer);
                int restockers = GetStoreRoleCount(StaffRole.Reyoncu);
                if (cashiers >= 3 && restockers >= 3)
                {
                    AdvanceToNextStep();
                }
            }
        }

        public void NotifyStaffShiftChanged()
        {
            if (CurrentStep == TutorialStep.Step4_AssignStoreShifts)
            {
                CheckStep4Completion();
            }
            else if (CurrentStep == TutorialStep.Step8_HireFarmStaffAndShifts)
            {
                CheckFarmStaffCompletion();
            }
        }

        private void CheckStep4Completion()
        {
            if (StaffManager.Instance == null) return;
            var staff = StaffManager.Instance.GetActiveStaff();
            if (staff == null || staff.Count < 6) return;

            bool hasMorning = staff.Exists(s => s.shiftHours.Contains("08:00") || s.shiftHours.Contains("Sabah") || s.shiftHours.Contains("Gündüz") || s.shiftHours.Contains("06:00"));
            bool hasEvening = staff.Exists(s => s.shiftHours.Contains("16:00") || s.shiftHours.Contains("Akşam") || s.shiftHours.Contains("14:00") || s.shiftHours.Contains("Gece") || s.shiftHours.Contains("22:00"));

            if (hasMorning && hasEvening)
            {
                AdvanceToNextStep();
            }
        }

        private void CheckFarmStaffCompletion()
        {
            if (StaffManager.Instance == null) return;
            int farmers = GetFarmRoleCount(StaffRole.Çiftçi);
            var farmStaff = StaffManager.Instance.GetFarmStaffList();
            if (farmers >= 3 && farmStaff != null && farmStaff.Count >= 3)
            {
                bool hasMorning = farmStaff.Exists(s => s.shiftHours.Contains("08:00") || s.shiftHours.Contains("Sabah") || s.shiftHours.Contains("Gündüz") || s.shiftHours.Contains("06:00"));
                bool hasEvening = farmStaff.Exists(s => s.shiftHours.Contains("16:00") || s.shiftHours.Contains("Akşam") || s.shiftHours.Contains("14:00") || s.shiftHours.Contains("Gece") || s.shiftHours.Contains("22:00"));

                if (hasMorning && hasEvening)
                {
                    AdvanceToNextStep();
                }
            }
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
            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            if (boughtShelves >= 3 && boughtCartStands >= 1 && boughtStorageShelves >= 3 && boughtCashiers >= 1 && boughtFridges >= 2)
            {
                AdvanceToNextStep();
            }
        }

        public void NotifyFurniturePlaced(FurnitureType type)
        {
            if (CurrentStep != TutorialStep.Step6_UnpackAndPlaceFurniture) return;

            TotalFurniturePlacedInTutorial++;
            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            if (TotalFurniturePlacedInTutorial >= 8)
            {
                AdvanceToNextStep();
            }
        }

        public void NotifyBulkOrderPlaced()
        {
            if (CurrentStep != TutorialStep.Step7_PlaceWholesaleBulkOrder) return;

            DidPlaceBulkOrder = true;
            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            CheckStep7Completion();
        }

        public void NotifyProductAssignedToShelf()
        {
            if (CurrentStep != TutorialStep.Step7_PlaceWholesaleBulkOrder) return;

            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            CheckStep7Completion();
        }

        private void CheckStep7Completion()
        {
            if (DidPlaceBulkOrder && GetMaxAssignedRowsOnAnyShelf() >= 4 && GetMaxAssignedRowsOnAnyFridge() >= 4)
            {
                AdvanceToNextStep();
            }
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

            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            if (DidBuyTomatoSeed && DidBuyCucumberSeed && DidBuyLettuceSeed)
            {
                AdvanceToNextStep();
            }
        }

        public void NotifyCropPlanted(string seedId)
        {
            if (CurrentStep != TutorialStep.Step10_PlantSeedsAndOpenStore) return;

            CropsPlantedInTutorial++;
            OnTutorialProgressUpdated?.Invoke();
            TutorialQuestTrackerUI.RefreshDisplay();

            if (CropsPlantedInTutorial >= 3 && DidOpenStoreInTutorial)
            {
                SetStep(TutorialStep.Completed);
            }
        }

        private void HandleStoreStatusChanged(bool isOpen)
        {
            if (isOpen && CurrentStep == TutorialStep.Step10_PlantSeedsAndOpenStore)
            {
                DidOpenStoreInTutorial = true;
                OnTutorialProgressUpdated?.Invoke();
                TutorialQuestTrackerUI.RefreshDisplay();

                if (CropsPlantedInTutorial >= 1)
                {
                    SetStep(TutorialStep.Completed);
                }
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
            if (staff == null) return false;
            return staff.Exists(s => s != null && s.shiftHours != null && s.shiftHours.Contains(keyword));
        }

        public bool HasFarmShift(string keyword)
        {
            if (StaffManager.Instance == null) return false;
            var staff = StaffManager.Instance.GetFarmStaffList();
            if (staff == null) return false;
            return staff.Exists(s => s != null && s.shiftHours != null && s.shiftHours.Contains(keyword));
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
