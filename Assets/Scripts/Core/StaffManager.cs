using System;
using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Core
{
    public enum StaffRole
    {
        Kasiyer,
        Reyoncu,
        Temizlikçi,
        Güvenlik,
        MüşteriHizmetlisi,
        Maskot,
        Çiftçi,
        DeneyimliÇiftçi,
        UstaÇiftlikSorumlusu,
        TarımOtomasyonUzmanı
    }

    [Serializable]
    public class StaffMember
    {
        public string id;
        public string name;
        public StaffRole role;
        public string shiftHours;
        public int dailySalary;
        public bool isActive;
        public bool isFemale;

        public StaffMember(string id, string name, StaffRole role, string shiftHours, int dailySalary, bool isActive = true, bool isFemale = false)
        {
            this.id = id;
            this.name = name;
            this.role = role;
            this.shiftHours = shiftHours;
            this.dailySalary = dailySalary;
            this.isActive = isActive;
            this.isFemale = isFemale;
        }
    }

    /// <summary>
    /// Farm2Shelf Personel Kadrosu, İşe Alım ve Gece Yarısı Maaş Ödeme Yöneticisi.
    /// Kalıcı 0 TL ilk ücretli işe alım ve 00:00 otomatik maaş ödemesini yönetir.
    /// </summary>
    public class StaffManager : MonoBehaviour
    {
        public static StaffManager Instance { get; private set; }

        [Header("Personel Kadrosu")]
        private List<StaffMember> activeStaffList = new List<StaffMember>();

        private static readonly string[] maleFirstNamesTr = new string[] {
            "Ahmet", "Mehmet", "Burak", "Caner", "Murat", "Emre", "Oğuz", "Kaan", "Serkan", "Volkan", "Mert", "Hakan", "Bora", "Cem", "Yusuf"
        };
        private static readonly string[] maleFirstNamesEn = new string[] {
            "John", "James", "Robert", "Michael", "William", "David", "Richard", "Charles", "Joseph", "Thomas", "Christopher", "Daniel", "Matthew", "Anthony", "Mark"
        };

        private static readonly string[] femaleFirstNamesTr = new string[] {
            "Elif", "Ayşe", "Zeynep", "Selin", "Gizem", "Seda", "Merve", "Büşra", "Ceren", "Derya", "Ebru", "Gamze", "Hande", "İrem", "Kübra"
        };
        private static readonly string[] femaleFirstNamesEn = new string[] {
            "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica", "Sarah", "Karen", "Lisa", "Nancy", "Betty", "Margaret", "Sandra"
        };

        private static readonly string[] lastNamesTr = new string[] {
            "Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Arslan", "Öztürk", "Yıldız", "Kılıç", "Aydın", "Özdemir", "Tekin", "Güneş", "Korkmaz"
        };
        private static readonly string[] lastNamesEn = new string[] {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson"
        };

        public event Action OnStaffListChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private bool paidSalariesToday = false;

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMidnightRollover += PayDailySalaries;
                TimeManager.Instance.OnTimeUpdated += HandleTimeCheckForSalaries;
            }
        }

        private void HandleTimeCheckForSalaries(int hour, int minute)
        {
            // Saat 12:00 (Öğle 12:00) olduğunda günlük maaş ödemelerini gerçekleştir
            if (hour == 12 && minute == 0 && !paidSalariesToday)
            {
                paidSalariesToday = true;
                PayDailySalaries();
            }
            else if (hour != 12)
            {
                paidSalariesToday = false;
            }
        }

        public static bool IsFemaleName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return false;
            string firstName = fullName.Split(' ')[0];
            foreach (var fn in femaleFirstNamesTr)
            {
                if (string.Equals(fn, firstName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            foreach (var fn in femaleFirstNamesEn)
            {
                if (string.Equals(fn, firstName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public StaffMember HireStaffByRole(StaffRole role)
        {
            string newId = "S" + UnityEngine.Random.Range(200, 999);

            bool isEnglish = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            string[] maleNames = isEnglish ? maleFirstNamesEn : maleFirstNamesTr;
            string[] femaleNames = isEnglish ? femaleFirstNamesEn : femaleFirstNamesTr;
            string[] surnames = isEnglish ? lastNamesEn : lastNamesTr;

            bool pickFemale = UnityEngine.Random.value > 0.5f;
            string selectedFirstName = pickFemale 
                ? femaleNames[UnityEngine.Random.Range(0, femaleNames.Length)]
                : maleNames[UnityEngine.Random.Range(0, maleNames.Length)];
            
            string randomName = selectedFirstName + " " + surnames[UnityEngine.Random.Range(0, surnames.Length)];
            string defaultShift = "☀️ Sabah (08:00 - 16:00)";
            int dailySalary = GetRoleDailySalary(role);

            StaffMember newStaff = new StaffMember(newId, randomName, role, defaultShift, dailySalary, true, pickFemale);
            activeStaffList.Add(newStaff);

            OnStaffListChanged?.Invoke();

            Debug.Log($"[Farm2Shelf] Personel Eklendi: {newStaff.name} ({(newStaff.isFemale ? "Kadın" : "Erkek")} - {newStaff.role}) - Günlük Maaş: {newStaff.dailySalary} Credit");
            return newStaff;
        }

        public int GetRoleDailySalary(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Kasiyer: return 120;
                case StaffRole.Reyoncu: return 100;
                case StaffRole.Temizlikçi: return 90;
                case StaffRole.Güvenlik: return 150;
                case StaffRole.MüşteriHizmetlisi: return 130;
                case StaffRole.Maskot: return 180;
                case StaffRole.Çiftçi: return 250;
                case StaffRole.DeneyimliÇiftçi: return 450;
                case StaffRole.UstaÇiftlikSorumlusu: return 850;
                case StaffRole.TarımOtomasyonUzmanı: return 1200;
                default: return 100;
            }
        }

        public int GetRoleHireFee(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Kasiyer: return 500;
                case StaffRole.Reyoncu: return 400;
                case StaffRole.Temizlikçi: return 350;
                case StaffRole.Güvenlik: return 600;
                case StaffRole.MüşteriHizmetlisi: return 550;
                case StaffRole.Maskot: return 800;
                case StaffRole.Çiftçi: return 1200;
                case StaffRole.DeneyimliÇiftçi: return 2500;
                case StaffRole.UstaÇiftlikSorumlusu: return 5000;
                case StaffRole.TarımOtomasyonUzmanı: return 8500;
                default: return 500;
            }
        }

        [Header("Çiftlik Personel Kadrosu (Ayrı Havuz)")]
        private List<StaffMember> farmStaffList = new List<StaffMember>();

        public event Action OnFarmStaffListChanged;

        public List<StaffMember> GetFarmStaffList() => farmStaffList;

        public static string NormalizeShift(string shift)
        {
            if (string.IsNullOrEmpty(shift)) return "☀️ Sabah (08:00 - 16:00)";

            // Önce Sabah / Gündüz / Morning / 08:00 kontrolleri
            if (shift.Contains("Sabah") || shift.Contains("Gündüz") || shift.Contains("Morning") || shift.Contains("Day") || shift.Contains("08:00") || shift.Contains("06:00"))
            {
                return "☀️ Sabah (08:00 - 16:00)";
            }

            // Akşam / Gece / Evening / Night kontrolleri
            if (shift.Contains("Akşam") || shift.Contains("Evening") || shift.Contains("Gece") || shift.Contains("Night") || shift.Contains("22:00") || shift.Contains("24:00") || shift.Contains("14:00"))
            {
                return "🌆 Akşam (16:00 - 24:00)";
            }

            return "☀️ Sabah (08:00 - 16:00)";
        }

        public void SetFarmStaffList(List<StaffMember> newList)
        {
            farmStaffList.Clear();
            if (newList != null)
            {
                foreach (var fs in newList)
                {
                    if (fs != null)
                    {
                        fs.shiftHours = NormalizeShift(fs.shiftHours);
                        farmStaffList.Add(fs);
                    }
                }
            }
            OnStaffListChanged?.Invoke();
            OnFarmStaffListChanged?.Invoke();
        }

        public StaffMember HireFarmWorker()
        {
            // Benzersiz Rastgele İsim Üret (Erkek veya Kadın, İsim Tekrarı Olmaz)
            string randomName = GenerateUniqueFarmWorkerName();
            string newId = "FS" + UnityEngine.Random.Range(100, 999);
            string defaultShift = "☀️ Sabah (08:00 - 16:00)";
            int dailySalary = GetRoleDailySalary(StaffRole.Çiftçi);

            StaffMember newStaff = new StaffMember(newId, randomName, StaffRole.Çiftçi, defaultShift, dailySalary, true);
            farmStaffList.Add(newStaff);

            OnStaffListChanged?.Invoke();
            OnFarmStaffListChanged?.Invoke();

            Debug.Log($"[Çiftlik İşe Alım] Yeni Çiftçi İşe Alındı: {newStaff.name} (Anında para kesilmedi, gece 00:00'da {dailySalary} Cr maaş kesilecek)");
            return newStaff;
        }

        private string GenerateUniqueFarmWorkerName()
        {
            bool isEnglish = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English;
            string[] maleNames = isEnglish ? maleFirstNamesEn : maleFirstNamesTr;
            string[] femaleNames = isEnglish ? femaleFirstNamesEn : femaleFirstNamesTr;
            string[] surnames = isEnglish ? lastNamesEn : lastNamesTr;

            int maxAttempts = 50;
            for (int i = 0; i < maxAttempts; i++)
            {
                bool isFemale = UnityEngine.Random.value > 0.5f;
                string fName = isFemale
                    ? femaleNames[UnityEngine.Random.Range(0, femaleNames.Length)]
                    : maleNames[UnityEngine.Random.Range(0, maleNames.Length)];
                string lName = surnames[UnityEngine.Random.Range(0, surnames.Length)];

                string candidateName = $"{fName} {lName}";
                if (!farmStaffList.Exists(s => s.name == candidateName))
                {
                    return candidateName;
                }
            }
            return isEnglish ? $"Worker {UnityEngine.Random.Range(100, 999)}" : $"Çiftçi {UnityEngine.Random.Range(100, 999)}";
        }

        public void FireFarmWorker(string staffId)
        {
            StaffMember staff = farmStaffList.Find(s => s.id == staffId);
            if (staff != null)
            {
                farmStaffList.Remove(staff);
                OnStaffListChanged?.Invoke();
                OnFarmStaffListChanged?.Invoke();
                Debug.Log($"[StaffManager] Çiftçi {staff.name} işten çıkarıldı.");
            }
        }

        public void UpdateFarmStaffShift(string staffId, string newShift)
        {
            StaffMember staff = farmStaffList.Find(s => s.id == staffId);
            if (staff != null)
            {
                staff.shiftHours = NormalizeShift(newShift);
                OnStaffListChanged?.Invoke();
                OnFarmStaffListChanged?.Invoke();
                Debug.Log($"[StaffManager] Çiftçi {staff.name} vardiyası güncellendi: {staff.shiftHours}");
            }
        }

        public void PayDailySalaries()
        {
            int totalPayroll = 0;

            // 1. Mağaza Personeli Maaşları
            foreach (var staff in activeStaffList)
            {
                if (staff.isActive) totalPayroll += staff.dailySalary;
            }

            // 2. Çiftlik İşçileri Maaşları (Gece 12'de Kesilir)
            int farmPayroll = 0;
            foreach (var farmStaff in farmStaffList)
            {
                if (farmStaff.isActive) farmPayroll += farmStaff.dailySalary;
            }
            totalPayroll += farmPayroll;

            if (totalPayroll > 0 && EconomyManager.Instance != null)
            {
                bool paid = EconomyManager.Instance.SpendCredits(totalPayroll);
                if (paid)
                {
                    if (FinanceManager.Instance != null)
                    {
                        FinanceManager.Instance.RecordExpense("Maaş", $"Gece Yarısı Maaş Ödemesi ({activeStaffList.Count} Mağaza, {farmStaffList.Count} Çiftçi)", totalPayroll);
                    }
                    Debug.Log($"[GECE YARISI MAAŞ ÖDEMESİ 00:00] {activeStaffList.Count} Mağaza + {farmStaffList.Count} Çiftlik çalışanına toplam {totalPayroll} Credit günlük maaş ödendi.");
                }
                else
                {
                    Debug.LogWarning($"[GECE YARISI MAAŞ ÖDEMESİ 00:00] Yetersiz bakiye! {totalPayroll} Credit personel maaşı ödenemedi.");
                }
            }
        }

        public void UpdateStaffShift(string staffId, string newShift)
        {
            StaffMember staff = activeStaffList.Find(s => s.id == staffId);
            if (staff != null)
            {
                staff.shiftHours = NormalizeShift(newShift);
                OnStaffListChanged?.Invoke();
                Debug.Log($"[Farm2Shelf] {staff.name} vardiyası değiştirildi: {staff.shiftHours}");
            }
        }

        public List<StaffMember> hiredStaffList => activeStaffList;

        public StaffMember HireStaff(string customName, string roleCategory, float dailySalary, float hireFee)
        {
            string newId = "SF" + UnityEngine.Random.Range(200, 999);
            string defaultShift = "☀️ Sabah (08:00 - 16:00)";
            StaffMember newStaff = new StaffMember(newId, customName, StaffRole.Reyoncu, defaultShift, Mathf.RoundToInt(dailySalary), true);
            activeStaffList.Add(newStaff);
            OnStaffListChanged?.Invoke();
            return newStaff;
        }

        public void FireStaff(string staffId)
        {
            StaffMember staff = activeStaffList.Find(s => s.id == staffId);
            if (staff != null)
            {
                activeStaffList.Remove(staff);
                OnStaffListChanged?.Invoke();
                Debug.Log($"[StaffManager] {staff.name} işten çıkarıldı.");
            }
        }

        public bool HasActiveRestocker()
        {
            if (activeStaffList == null || activeStaffList.Count == 0) return false;
            foreach (var staff in activeStaffList)
            {
                if (staff != null && staff.isActive && staff.role == StaffRole.Reyoncu)
                {
                    return true;
                }
            }
            return false;
        }

        public List<StaffMember> GetActiveStaff() => activeStaffList;

        public void SetStaffList(List<StaffMember> newList)
        {
            activeStaffList.Clear();
            if (newList != null)
            {
                foreach (var s in newList)
                {
                    if (s != null)
                    {
                        s.shiftHours = NormalizeShift(s.shiftHours);
                        activeStaffList.Add(s);
                    }
                }
            }
            OnStaffListChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnMidnightRollover -= PayDailySalaries;
                TimeManager.Instance.OnTimeUpdated -= HandleTimeCheckForSalaries;
            }
        }
    }
}
