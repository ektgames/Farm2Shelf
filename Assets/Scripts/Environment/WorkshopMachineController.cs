using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.Core;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Atölye binasına yerleştirilen her bir endüstriyel makinenin gerçek zamanlı üretimini,
    /// 3D geri sayım sayacını ve tamamlandığında beliren 3D toplama simgesini yöneten bileşen.
    /// </summary>
    public class WorkshopMachineController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        public static List<WorkshopMachineController> AllPlacedMachines { get; private set; } = new List<WorkshopMachineController>();

        [Header("Makine Bilgileri")]
        public string machineInstanceId;
        public WorkshopMachineType machineType;

        [Header("Üretim Durumu")]
        public bool isProducing = false;
        public bool isReadyToCollect = false;
        public string activeRecipeId = "";
        public float remainingSeconds = 0f;
        public float totalDuration = 0f;

        public float remainingProductionSeconds
        {
            get => remainingSeconds;
            set => remainingSeconds = value;
        }

        public float totalProductionSeconds
        {
            get => totalDuration;
            set => totalDuration = value;
        }

        public void UpdateFloatingBadgeVisual() => UpdateOverheadDisplay();
        public void Update3DStatusDisplay() => UpdateOverheadDisplay();

        // 3D Billboard Badge & Geri Sayım Görselleri
        [System.NonSerialized] private Transform overheadTransform;
        [System.NonSerialized] private Renderer plateRenderer;
        [System.NonSerialized] private TextMesh statusTextMesh;
        [System.NonSerialized] private Transform plateTransform;

        private static Material producingBadgeMat;
        private static Material readyBadgeMat;

        private static void EnsureMaterials()
        {
            if (producingBadgeMat == null)
            {
                producingBadgeMat = ShaderHelper.CreateLitMaterial(new Color(0.08f, 0.24f, 0.48f), "MachineBadgeProducingMat");
            }
            if (readyBadgeMat == null)
            {
                readyBadgeMat = ShaderHelper.CreateLitMaterial(new Color(0.12f, 0.48f, 0.22f), "MachineBadgeReadyMat");
            }
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(machineInstanceId))
            {
                machineInstanceId = System.Guid.NewGuid().ToString().Substring(0, 8);
            }
        }

        private void Start()
        {
            CreateOverheadUI();
        }

        private void OnEnable()
        {
            if (!AllPlacedMachines.Contains(this))
            {
                AllPlacedMachines.Add(this);
            }
        }

        private void OnDisable()
        {
            AllPlacedMachines.Remove(this);
        }

        private void OnDestroy()
        {
            AllPlacedMachines.Remove(this);
        }

        private void Update()
        {
            if (isProducing && remainingSeconds > 0f)
            {
                remainingSeconds -= Time.deltaTime;
                if (remainingSeconds <= 0f)
                {
                    remainingSeconds = 0f;
                    isProducing = false;
                    isReadyToCollect = true;
                    OnProductionCompleted();
                }
                UpdateOverheadDisplay();
            }
        }

        private void LateUpdate()
        {
            if (overheadTransform == null || !overheadTransform.gameObject.activeSelf) return;

            // Kameraya baktır (Isometric Billboard)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                overheadTransform.rotation = mainCam.transform.rotation;
            }

            // Hazır rozetini havada tatlıca yüzdür
            if (isReadyToCollect)
            {
                float floatOffset = Mathf.Sin(Time.time * 3.5f) * 0.08f;
                overheadTransform.localPosition = new Vector3(0f, 2.35f + floatOffset, 0f);
            }
        }

        private void CreateOverheadUI()
        {
            if (overheadTransform != null && statusTextMesh != null && plateRenderer != null)
            {
                UpdateOverheadDisplay();
                return;
            }

            // Eski UGUI Canvas kalıntısı varsa temizle
            Transform oldCanvas = transform.Find("Overhead_Status_Canvas");
            if (oldCanvas != null) DestroyImmediate(oldCanvas.gameObject);

            Transform existing = transform.Find("Overhead_Badge_3D");
            if (existing != null)
            {
                overheadTransform = existing;
                plateTransform = existing.Find("Badge_Plate");
                if (plateTransform != null) plateRenderer = plateTransform.GetComponent<Renderer>();
                Transform txt = existing.Find("Badge_Text");
                if (txt != null) statusTextMesh = txt.GetComponent<TextMesh>();
                if (overheadTransform != null && plateRenderer != null && statusTextMesh != null)
                {
                    UpdateOverheadDisplay();
                    return;
                }
                DestroyImmediate(existing.gameObject);
            }

            EnsureMaterials();

            GameObject badgeObj = new GameObject("Overhead_Badge_3D");
            badgeObj.transform.SetParent(transform, false);
            badgeObj.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            overheadTransform = badgeObj.transform;

            // 1. 3D Arka Plan Paneli
            GameObject plateObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plateObj.name = "Badge_Plate";
            plateObj.transform.SetParent(badgeObj.transform, false);
            plateObj.transform.localPosition = Vector3.zero;
            plateObj.transform.localScale = new Vector3(1.80f, 0.55f, 0.04f);
            plateTransform = plateObj.transform;

            Collider c = plateObj.GetComponent<Collider>();
            if (c != null) Destroy(c);

            plateRenderer = plateObj.GetComponent<Renderer>();
            plateRenderer.sharedMaterial = producingBadgeMat;

            // 2. 3D TextMesh (3D Dünyada Kusursuz Görünen Net Metin)
            GameObject textObj = new GameObject("Badge_Text");
            textObj.transform.SetParent(badgeObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.04f);

            statusTextMesh = textObj.AddComponent<TextMesh>();
            statusTextMesh.fontSize = 54;
            statusTextMesh.characterSize = 0.055f;
            statusTextMesh.alignment = TextAlignment.Center;
            statusTextMesh.anchor = TextAnchor.MiddleCenter;
            statusTextMesh.fontStyle = FontStyle.Bold;
            statusTextMesh.color = Color.white;

            UpdateOverheadDisplay();
        }

        public void UpdateOverheadDisplay()
        {
            if (overheadTransform == null)
            {
                CreateOverheadUI();
                if (overheadTransform == null) return;
            }

            EnsureMaterials();

            if (isReadyToCollect)
            {
                if (!overheadTransform.gameObject.activeSelf) overheadTransform.gameObject.SetActive(true);

                if (plateRenderer != null) plateRenderer.sharedMaterial = readyBadgeMat;
                if (plateTransform != null) plateTransform.localScale = new Vector3(2.0f, 0.65f, 0.04f);

                string readyWord = LocalizationManager.L("WS3D_ReadyWord", "HAZIR!", "READY!");
                string collectWord = LocalizationManager.L("WS3D_CollectWord", "Topla", "Collect");

                if (statusTextMesh != null)
                {
                    statusTextMesh.fontSize = 40;
                    statusTextMesh.characterSize = 0.050f;
                    statusTextMesh.color = new Color(1.0f, 0.98f, 0.75f);
                    statusTextMesh.text = $"★ {readyWord} ★\n[{collectWord}]";
                }
            }
            else if (isProducing)
            {
                if (!overheadTransform.gameObject.activeSelf) overheadTransform.gameObject.SetActive(true);

                if (plateRenderer != null) plateRenderer.sharedMaterial = producingBadgeMat;
                if (plateTransform != null) plateTransform.localScale = new Vector3(1.70f, 0.52f, 0.04f);

                int mins = Mathf.FloorToInt(remainingSeconds / 60f);
                int secs = Mathf.FloorToInt(remainingSeconds % 60f);

                if (statusTextMesh != null)
                {
                    statusTextMesh.fontSize = 54;
                    statusTextMesh.characterSize = 0.055f;
                    statusTextMesh.color = Color.white;
                    statusTextMesh.text = $"{mins:00}:{secs:00}";
                }
            }
            else
            {
                // Boşta dururken rozeti tamamen kapat (boş çerçeve KESİNLİKLE görünmez)
                if (overheadTransform.gameObject.activeSelf)
                {
                    overheadTransform.gameObject.SetActive(false);
                }
            }
        }

        public bool StartProduction(string recipeId)
        {
            WorkshopRecipeDef recipe = WorkshopMachineDatabase.GetRecipeById(recipeId);
            if (recipe == null || isProducing || isReadyToCollect) return false;

            // Hammadde kontrolü
            if (WorkshopPalletManager.Instance == null || !WorkshopPalletManager.Instance.HasCrop(recipe.cropId, recipe.requiredCropKg))
            {
                return false;
            }

            // Hammaddeyi atölye paletinden tüket
            WorkshopPalletManager.Instance.ConsumeCrop(recipe.cropId, recipe.requiredCropKg);

            activeRecipeId = recipeId;
            totalDuration = recipe.durationSeconds;
            remainingSeconds = recipe.durationSeconds;
            isProducing = true;
            isReadyToCollect = false;

            UpdateOverheadDisplay();
            return true;
        }

        private void OnProductionCompleted()
        {
            UpdateOverheadDisplay();
            Debug.Log($"[WorkshopMachine] {machineType} ({machineInstanceId}) üretimi tamamlandı! Ürün toplanmaya hazır.");
        }

        public void CollectFinishedProduct()
        {
            if (!isReadyToCollect) return;

            WorkshopRecipeDef recipe = WorkshopMachineDatabase.GetRecipeById(activeRecipeId);
            if (recipe != null)
            {
                // Üretilen gurme ürünü Ahır Envanterine aktar!
                if (GardenSeedInventoryManager.Instance != null)
                {
                    GardenSeedInventoryManager.Instance.AddBarnCrop(recipe.outputProductId, recipe.outputPackCount);
                }

                string title = LocalizationManager.L("Modal_CollectGourmet_Title", "🎉 Gurme Üretim Toplandı!", "🎉 Gourmet Production Collected!");
                string bodyFmt = LocalizationManager.L(
                    "Modal_CollectGourmet_Body",
                    "Tebrikler! <b>{0} {1}</b> (Toplam {2} Adet) başarıyla üretildi ve **Ahır Envanterinize** aktarıldı!\n\n📦 Ahır menüsünden ürünü Yeşil Kamyonla dükkana sevk edebilir, Gurme Rafına dizebilir veya anında satabilirsiniz.",
                    "Congratulations! <b>{0} {1}</b> ({2} Units) successfully crafted and transferred to your **Barn Storage**!\n\n📦 You can ship it to the store, stock it on the Gourmet Shelf, or instant-sell."
                );
                ModalManager.ShowModal(title, string.Format(bodyFmt, recipe.iconEmoji, recipe.LocalizedName, recipe.outputPackCount), LocalizationManager.L("Btn_Great", "Harika!", "Awesome!"));
            }

            // Sıfırla
            isReadyToCollect = false;
            isProducing = false;
            activeRecipeId = "";
            remainingSeconds = 0f;
            totalDuration = 0f;

            UpdateOverheadDisplay();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleInteraction();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Touch desteği
        }

        public void HandleInteraction()
        {
            if (isReadyToCollect)
            {
                CollectFinishedProduct();
                return;
            }

            WorkshopMachineModalUI.ShowModal(this);
        }

        /// <summary>
        /// Kayıtlı oyundan makine durumunu geri yükler.
        /// </summary>
        public void RestoreState(string recipeId, bool producing, bool ready, float remainingSec, float duration)
        {
            activeRecipeId = recipeId;
            isProducing = producing;
            isReadyToCollect = ready;
            remainingSeconds = remainingSec;
            totalDuration = duration;
            UpdateOverheadDisplay();
        }
    }
}
