using System;
using UnityEngine;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Bağımsız Atölye Yöneticisi (Workshop Manager).
    /// Atölye binasının 3 aşamalı geliştirme seviyelerini (1: Başlangıç Atölyesi, 2: Genişletilmiş Atölye, 3: Mega Sanayi Kompleksi),
    /// yükseltme maliyetlerini, bakiye harcamalarını ve UI/Harita olay bildirimlerini bağımsız olarak yönetir.
    /// </summary>
    public class WorkshopManager : MonoBehaviour
    {
        public static WorkshopManager Instance { get; private set; }

        [Header("Atölye Seviyesi")]
        [SerializeField] private int currentWorkshopLevel = 1; // 1, 2, 3

        public int CurrentWorkshopLevel => currentWorkshopLevel;

        public static event Action<int> OnWorkshopUpgraded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Hedef seviyeye geliştirme maliyetini döner.
        /// </summary>
        public int GetUpgradeCost(int targetLevel)
        {
            switch (targetLevel)
            {
                case 2: return 7500;
                case 3: return 20000;
                default: return 0;
            }
        }

        /// <summary>
        /// Belirtilen hedef seviyeye yükseltme işlemini gerçekleştirir.
        /// </summary>
        public bool UpgradeWorkshop(int targetLevel)
        {
            if (targetLevel != currentWorkshopLevel + 1 || targetLevel > 3)
            {
                return false;
            }

            int cost = GetUpgradeCost(targetLevel);
            int currentCredits = (EconomyManager.Instance != null) ? EconomyManager.Instance.Credits : 0;

            if (currentCredits < cost)
            {
                return false;
            }

            // 1. Bakiyeden Düş
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.SpendCredits(cost);
            }

            // 2. Finans Dökümüne Gider Kaydet
            if (FinanceManager.Instance != null)
            {
                string catName = LocalizationManager.L("TrxCat_Upgrade", "Geliştirme / İnşaat", "Expansion / Construction");
                string descFmt = LocalizationManager.L("TrxDesc_WorkshopUpgradeFmt", "Atölye Seviye {0} Genişletme İnşaatı", "Workshop Level {0} Expansion Construction");
                FinanceManager.Instance.RecordExpense(catName, string.Format(descFmt, targetLevel), cost);
            }

            // 3. Seviyeyi Güncelle ve Haritayı Yeniden İnşa Et
            currentWorkshopLevel = targetLevel;

            if (WorkshopBuilder.Instance != null)
            {
                WorkshopBuilder.Instance.BuildWorkshop(currentWorkshopLevel);
            }

            OnWorkshopUpgraded?.Invoke(currentWorkshopLevel);
            Debug.Log($"[WorkshopManager] Atölye Seviye {currentWorkshopLevel} Başarıyla Yükseltildi!");
            return true;
        }

        /// <summary>
        /// Kayıtlı oyundan atölye seviyesini geri yükler.
        /// </summary>
        public void SetWorkshopLevel(int level)
        {
            currentWorkshopLevel = Mathf.Clamp(level, 1, 3);

            if (WorkshopBuilder.Instance != null)
            {
                WorkshopBuilder.Instance.BuildWorkshop(currentWorkshopLevel);
            }

            OnWorkshopUpgraded?.Invoke(currentWorkshopLevel);
        }
    }
}
