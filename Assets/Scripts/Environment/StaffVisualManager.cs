using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.UI;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace Farm2Shelf.Environment
{
    public class StaffVisualManager : MonoBehaviour
    {
        public static StaffVisualManager Instance { get; private set; }

        private readonly Dictionary<string, GameObject> activeStaffModels = new Dictionary<string, GameObject>();
        private Transform staffGroupTransform;
        private Action<int, int> timeUpdateHandler;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private readonly HashSet<string> earlyCalledStaffIds = new HashSet<string>();

        public bool IsStaffCalledEarlyToday(string staffId) => earlyCalledStaffIds.Contains(staffId);

        public List<string> GetEarlyCalledStaffIds()
        {
            return new List<string>(earlyCalledStaffIds);
        }

        public void RestoreEarlyCalledStaff(List<string> staffIds)
        {
            earlyCalledStaffIds.Clear();
            if (staffIds != null)
            {
                foreach (var id in staffIds)
                {
                    if (!string.IsNullOrEmpty(id)) earlyCalledStaffIds.Add(id);
                }
            }
        }

        public void ClearAllStaffModels()
        {
            foreach (var kvp in activeStaffModels)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            activeStaffModels.Clear();
            earlyCalledStaffIds.Clear();

            if (StaffTaskController.Instance != null)
            {
                StaffTaskController.Instance.ClearAllStaffAI();
            }
        }

        public bool ForceSpawnStaffEarly(StaffMember staff)
        {
            if (staff == null) return false;

            if (EconomyManager.Instance != null && EconomyManager.Instance.Credits < 50)
            {
                ModalManager.ShowModal("Yetersiz Bakiye! ⚠️", "Erken mesai çağırısı için en az 50C bakiye gereklidir.", "Tamam");
                return false;
            }

            if (earlyCalledStaffIds.Contains(staff.id))
            {
                ModalManager.ShowModal("Zaten Görevde! ℹ️", $"{staff.name} zaten dükkan kapalıyken göreve çağrılmıştır.", "Tamam");
                return false;
            }

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SpendCredits(50);
            }

            earlyCalledStaffIds.Add(staff.id);
            if (!activeStaffModels.ContainsKey(staff.id) || activeStaffModels[staff.id] == null)
            {
                SpawnStaff3DModel(staff);
            }

            ModalManager.ShowModal(
                "⚡ Erken Mesai Başlatıldı!",
                $"{staff.name} 50 CR mesai ücreti ödenerek dükkan kapalıyken ön hazırlık için göreve çağrıldı!\n\nPersonel odasında üstünü değiştirip dükkan ve reyon hazırlıklarına başladı.",
                "Tamam"
            );

            SyncStaff3DModels();
            return true;
        }

        private void Start()
        {
            GameObject grp = new GameObject("Staff_3D_Models_Group");
            grp.transform.SetParent(transform);
            staffGroupTransform = grp.transform;

            if (StaffManager.Instance != null)
            {
                StaffManager.Instance.OnStaffListChanged += SyncStaff3DModels;
            }

            if (StoreStatusManager.Instance != null)
            {
                StoreStatusManager.Instance.OnStoreStatusChanged += (isOpen) => SyncStaff3DModels();
            }

            if (TimeManager.Instance != null)
            {
                timeUpdateHandler = (hour, min) => {
                    if (min % 5 == 0) SyncStaff3DModels();
                };
                TimeManager.Instance.OnTimeUpdated += timeUpdateHandler;
                TimeManager.Instance.OnMidnightRollover += () => {
                    earlyCalledStaffIds.Clear();
                    SyncStaff3DModels();
                };
            }

            SyncStaff3DModels();
        }

        private void OnDestroy()
        {
            if (StaffManager.Instance != null)
            {
                StaffManager.Instance.OnStaffListChanged -= SyncStaff3DModels;
            }

            if (TimeManager.Instance != null && timeUpdateHandler != null)
            {
                TimeManager.Instance.OnTimeUpdated -= timeUpdateHandler;
            }
        }

        public void SyncStaff3DModels()
        {
            if (StaffManager.Instance == null) return;

            int currentHour = TimeManager.Instance != null ? TimeManager.Instance.Hour : 6;
            int currentMinute = TimeManager.Instance != null ? TimeManager.Instance.Minute : 0;
            int totalMins = currentHour * 60 + currentMinute;

            List<StaffMember> activeStaffList = StaffManager.Instance.GetActiveStaff();
            List<StaffMember> farmStaffList = StaffManager.Instance.GetFarmStaffList();

            // 1. Vardiya bitiş saatini geçmiş erken çağrıları temizle (Sabah personeli: 16:00 / 960 dk, Akşam personeli: 24:00 / 1440 dk)
            if (earlyCalledStaffIds.Count > 0 && activeStaffList != null)
            {
                List<string> idsToClear = new List<string>();
                foreach (var id in earlyCalledStaffIds)
                {
                    var s = activeStaffList.Find(x => x.id == id);
                    if (s == null && farmStaffList != null) s = farmStaffList.Find(x => x.id == id);
                    if (s != null)
                    {
                        string sft = s.shiftHours ?? "";
                        bool isEve = sft.Contains("Akşam") || sft.Contains("Evening") || sft.Contains("Gece") || sft.Contains("Night") || sft.Contains("16:00 - 24:00") || sft.Contains("24:00");
                        if (!isEve && totalMins >= 960) idsToClear.Add(id); // Sabah vardiyası bitti (16:00)
                        else if (isEve && totalMins >= 1440) idsToClear.Add(id); // Akşam vardiyası bitti (24:00)
                    }
                }
                foreach (var id in idsToClear) earlyCalledStaffIds.Remove(id);
            }

            HashSet<string> eligibleStaffIds = new HashSet<string>();

            // 2. DÜKKAN PERSONELİ (Kasiyer, Reyoncu, Temizlikçi, Güvenlik, Danışma, Maskot)
            if (activeStaffList != null)
            {
                foreach (var s in activeStaffList)
                {
                    if (s == null || !s.isActive) continue;

                    bool isLeaving = (StaffTaskController.Instance != null && StaffTaskController.Instance.IsStaffLeavingShift(s.id));
                    if (isLeaving) continue; // Çıkış yapan veya ayrılan personel asla tekrar spawn edilemez!

                    bool isCarrying = (StaffTaskController.Instance != null && StaffTaskController.Instance.IsStaffCarryingInHandTask(s.id));
                    bool isEligible = StaffTaskController.IsStaffShiftActive(s, currentHour, currentMinute, out _);

                    if (isEligible || isCarrying)
                    {
                        eligibleStaffIds.Add(s.id);
                    }
                }
            }

            // 3. ÇİFTLİK PERSONELİ (Çiftçiler)
            if (farmStaffList != null)
            {
                foreach (var s in farmStaffList)
                {
                    if (s == null || !s.isActive) continue;

                    bool isLeaving = (StaffTaskController.Instance != null && StaffTaskController.Instance.IsStaffLeavingShift(s.id));
                    if (isLeaving) continue;

                    bool isCarrying = (StaffTaskController.Instance != null && StaffTaskController.Instance.IsStaffCarryingInHandTask(s.id));
                    bool isEligible = StaffTaskController.IsStaffShiftActive(s, currentHour, currentMinute, out _);

                    if (isEligible || isCarrying)
                    {
                        eligibleStaffIds.Add(s.id);
                    }
                }
            }

            foreach (var id in eligibleStaffIds)
            {
                if (!activeStaffModels.ContainsKey(id) || activeStaffModels[id] == null)
                {
                    StaffMember s = activeStaffList != null ? activeStaffList.Find(x => x.id == id) : null;
                    if (s == null && farmStaffList != null) s = farmStaffList.Find(x => x.id == id);
                    if (s != null) SpawnStaff3DModel(s);
                }
            }

            // Vardiyası Biten veya Dükkandan Ayrılan Modelleri Dışarı Çıkar (Exit Route)
            List<string> idsToExit = new List<string>();
            foreach (var kvp in activeStaffModels)
            {
                if (!eligibleStaffIds.Contains(kvp.Key))
                {
                    idsToExit.Add(kvp.Key);
                }
            }

            foreach (var id in idsToExit)
            {
                if (StaffTaskController.Instance != null)
                {
                    StaffTaskController.Instance.StartExitForStaff(id);
                }
                activeStaffModels.Remove(id);
            }
        }

        private void SpawnStaff3DModel(StaffMember staff)
        {
            if (staff == null) return;
            if (StaffTaskController.Instance != null && StaffTaskController.Instance.IsStaffLeavingShift(staff.id)) return;

            bool isFemale = staff.isFemale || StaffManager.IsFemaleName(staff.name);
            GameObject modelObj = ProceduralStaffModelBuilder.CreateStaffModel(staff.role, isFemale, out List<Transform> leftLimbs, out List<Transform> rightLimbs);
            modelObj.transform.SetParent(staffGroupTransform, false);

            // Başlangıç Konumu: Çiftçiler Çiftlik Evi Kapı Önünde Spawn Olur, Dükkan İşçileri Sağ Kaldırımda
            bool isFarmer = (staff.role == StaffRole.Çiftçi || staff.role == StaffRole.DeneyimliÇiftçi || staff.role == StaffRole.UstaÇiftlikSorumlusu || staff.role == StaffRole.TarımOtomasyonUzmanı);
            if (isFarmer)
            {
                modelObj.transform.position = new Vector3(25.0f, 0.05f, 32.5f);
            }
            else
            {
                modelObj.transform.position = new Vector3(15.0f, 0.05f, -4.5f);
            }

            // Personel Tıklama Hedefi Bileşenini Bağla (Sol Taraf Profil Kartı Açmak İçin)
            StaffClickableTarget target = modelObj.AddComponent<StaffClickableTarget>();
            target.staffMember = staff;

            // Gelişmiş Personel Yapay Zeka Görev Denetleyicisine Kaydet
            if (StaffTaskController.Instance != null)
            {
                StaffTaskController.Instance.RegisterStaffAI(staff, modelObj, leftLimbs, rightLimbs);
            }

            activeStaffModels[staff.id] = modelObj;
        }
    }
}
