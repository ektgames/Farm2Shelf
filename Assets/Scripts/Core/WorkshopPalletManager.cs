using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Atölye içindeki hammadde palet deposunu (Workshop Pallet Storage) ve içindeki mahsul/makine envanterini yönetir.
    /// Ahırdan aktarılan mahsuller ve tabletten alınan atölye makineleri burada toplanır.
    /// </summary>
    public class WorkshopPalletManager : MonoBehaviour
    {
        public static WorkshopPalletManager Instance { get; private set; }

        private Dictionary<string, int> storedCrops = new Dictionary<string, int>();
        private List<string> pendingMachineBoxes = new List<string>();

        public event Action OnWorkshopInventoryUpdated;

        private Transform boxContainerTransform;
        private List<GameObject> spawned3DBoxes = new List<GameObject>();

        private Material cardboardMat;
        private Material woodPalletMat;
        private Material machineBoxMat;

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

        public void RegisterBoxContainer(Transform container)
        {
            boxContainerTransform = container;
            Refresh3DVisuals();
        }

        public void AddMachineOrders(List<FurnitureType> items)
        {
            if (items == null || items.Count == 0) return;
            foreach (var item in items)
            {
                pendingMachineBoxes.Add(item.ToString());
            }
            OnWorkshopInventoryUpdated?.Invoke();
            Refresh3DVisuals();
            Debug.Log($"[WorkshopPalletManager] Atölye Paletine {items.Count} adet makine kolisi teslim edildi!");
        }

        public void RemoveOneMachineBox(FurnitureType type)
        {
            string tStr = type.ToString();
            int idx = pendingMachineBoxes.IndexOf(tStr);
            if (idx >= 0)
            {
                pendingMachineBoxes.RemoveAt(idx);
                OnWorkshopInventoryUpdated?.Invoke();
                Refresh3DVisuals();
            }
        }

        public Dictionary<FurnitureType, int> GetPendingMachineCounts()
        {
            Dictionary<FurnitureType, int> counts = new Dictionary<FurnitureType, int>();
            foreach (var tStr in pendingMachineBoxes)
            {
                if (Enum.TryParse<FurnitureType>(tStr, out FurnitureType fType))
                {
                    if (!counts.ContainsKey(fType)) counts[fType] = 0;
                    counts[fType]++;
                }
            }
            return counts;
        }

        public List<string> GetPendingMachineBoxTypes()
        {
            return new List<string>(pendingMachineBoxes);
        }

        public void RestorePendingMachineBoxes(List<string> boxTypes)
        {
            pendingMachineBoxes = boxTypes != null ? new List<string>(boxTypes) : new List<string>();
            OnWorkshopInventoryUpdated?.Invoke();
            Refresh3DVisuals();
        }

        public Dictionary<string, int> GetCropInventory()
        {
            return new Dictionary<string, int>(storedCrops);
        }

        public int GetCropAmount(string cropId)
        {
            if (string.IsNullOrEmpty(cropId)) return 0;
            return storedCrops.ContainsKey(cropId) ? storedCrops[cropId] : 0;
        }

        public int GetCropCount(string cropId)
        {
            return GetCropAmount(cropId);
        }

        public bool HasCrop(string cropId, int count = 1)
        {
            return GetCropAmount(cropId) >= count;
        }

        public int GetTotalStoredAmount()
        {
            int total = 0;
            foreach (var kvp in storedCrops) total += kvp.Value;
            return total;
        }

        public void AddCrops(string cropId, int amount)
        {
            if (string.IsNullOrEmpty(cropId) || amount <= 0) return;

            if (!storedCrops.ContainsKey(cropId))
            {
                storedCrops[cropId] = 0;
            }

            storedCrops[cropId] += amount;
            OnWorkshopInventoryUpdated?.Invoke();
            Refresh3DVisuals();

            Debug.Log($"[WorkshopPalletManager] Atölye Paletine Eklendi: {cropId} +{amount} KG (Toplam: {storedCrops[cropId]} KG)");
        }

        public bool ConsumeCrop(string cropId, int amount)
        {
            if (string.IsNullOrEmpty(cropId) || amount <= 0) return false;
            if (!storedCrops.ContainsKey(cropId) || storedCrops[cropId] < amount) return false;

            storedCrops[cropId] -= amount;
            if (storedCrops[cropId] <= 0)
            {
                storedCrops.Remove(cropId);
            }

            OnWorkshopInventoryUpdated?.Invoke();
            Refresh3DVisuals();
            return true;
        }

        public void SetAllCrops(Dictionary<string, int> crops)
        {
            storedCrops = (crops != null) ? new Dictionary<string, int>(crops) : new Dictionary<string, int>();
            OnWorkshopInventoryUpdated?.Invoke();
            Refresh3DVisuals();
        }

        public void ClearAll()
        {
            storedCrops.Clear();
            OnWorkshopInventoryUpdated?.Invoke();
            Refresh3DVisuals();
        }

        private void InitMaterials()
        {
            if (cardboardMat == null)
            {
                cardboardMat = ShaderHelper.CreateLitMaterial(new Color(0.82f, 0.64f, 0.42f), "WorkshopBoxMat");
            }
            if (woodPalletMat == null)
            {
                woodPalletMat = ShaderHelper.CreateLitMaterial(new Color(0.60f, 0.40f, 0.20f), "WorkshopPalletWoodMat");
            }
            if (machineBoxMat == null)
            {
                machineBoxMat = ShaderHelper.CreateLitMaterial(new Color(0.20f, 0.28f, 0.38f), "WorkshopMachineBoxMat");
            }
        }

        public void Refresh3DVisuals()
        {
            if (boxContainerTransform == null) return;

            InitMaterials();

            // Eski kutuları temizle
            foreach (var b in spawned3DBoxes)
            {
                if (b != null)
                {
                    if (Application.isPlaying) Destroy(b);
                    else DestroyImmediate(b);
                }
            }
            spawned3DBoxes.Clear();

            int totalKg = GetTotalStoredAmount();
            int machineCount = pendingMachineBoxes.Count;

            if (totalKg <= 0 && machineCount <= 0) return;

            // 2 Katlı 3x3 Grid Slot Dizilimi
            float slotW = 0.55f;
            float slotD = 0.55f;
            int placed = 0;

            // 1. ÖNCE MAKİNE KOLİLERİ (Öncelikli olarak yerleştirilir)
            for (int m = 0; m < machineCount && placed < 18; m++)
            {
                int layer = placed / 9;
                int rem = placed % 9;
                int row = (rem / 3) - 1;
                int col = (rem % 3) - 1;

                float layerY = 0.25f + (layer * 0.52f);

                GameObject mBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mBox.name = $"MachineBox_{pendingMachineBoxes[m]}_{placed}";
                mBox.transform.SetParent(boxContainerTransform, false);
                mBox.transform.localPosition = new Vector3(col * slotW, layerY, row * slotD);
                mBox.transform.localScale = new Vector3(0.52f, 0.46f, 0.52f);
                mBox.GetComponent<Renderer>().sharedMaterial = machineBoxMat;
                Destroy(mBox.GetComponent<Collider>());

                // Metalik Bant / İkaz Şeridi Detayı
                GameObject tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tape.name = "SteelStrap";
                tape.transform.SetParent(mBox.transform, false);
                tape.transform.localPosition = new Vector3(0f, 0.51f, 0f);
                tape.transform.localScale = new Vector3(0.22f, 0.02f, 1.01f);
                tape.GetComponent<Renderer>().sharedMaterial = ShaderHelper.CreateLitMaterial(new Color(0.95f, 0.65f, 0.15f), "StrapMat");
                Destroy(tape.GetComponent<Collider>());

                spawned3DBoxes.Add(mBox);
                placed++;
            }

            // 2. MAHSUL KOLİLERİ (Her 25 KG için 1 koli)
            int cropBoxCount = Mathf.Clamp(Mathf.CeilToInt(totalKg / 25f), 0, 18 - placed);

            for (int c = 0; c < cropBoxCount && placed < 18; c++)
            {
                int layer = placed / 9;
                int rem = placed % 9;
                int row = (rem / 3) - 1;
                int col = (rem % 3) - 1;

                float layerY = 0.22f + (layer * 0.45f);

                GameObject boxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boxObj.name = $"CropBox_{placed}";
                boxObj.transform.SetParent(boxContainerTransform, false);
                boxObj.transform.localPosition = new Vector3(col * slotW, layerY, row * slotD);
                boxObj.transform.localScale = new Vector3(0.48f, 0.40f, 0.48f);
                boxObj.GetComponent<Renderer>().sharedMaterial = cardboardMat;
                Destroy(boxObj.GetComponent<Collider>());

                // Koli Bant Detayı
                GameObject tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tape.name = "Tape";
                tape.transform.SetParent(boxObj.transform, false);
                tape.transform.localPosition = new Vector3(0f, 0.51f, 0f);
                tape.transform.localScale = new Vector3(0.18f, 0.02f, 1.01f);
                tape.GetComponent<Renderer>().sharedMaterial = woodPalletMat;
                Destroy(tape.GetComponent<Collider>());

                spawned3DBoxes.Add(boxObj);
                placed++;
            }
        }
    }
}
