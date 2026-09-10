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
        private Dictionary<string, List<ProductLot>> storedCropLots = new Dictionary<string, List<ProductLot>>();
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
            AddCrops(cropId, amount, null);
        }

        public void AddCrops(string cropId, int amount, List<ProductLot> lots)
        {
            if (string.IsNullOrEmpty(cropId) || amount <= 0) return;

            if (!storedCrops.ContainsKey(cropId))
            {
                storedCrops[cropId] = 0;
            }

            storedCrops[cropId] += amount;
            EnsureLotList(cropId);
            if (lots != null && lots.Count > 0)
            {
                int attached = 0;
                for (int i = 0; i < lots.Count; i++)
                {
                    if (lots[i] == null || lots[i].quantity <= 0) continue;
                    ProductLot copy = lots[i].Clone();
                    copy.productId = cropId;
                    ProductPassportService.MergeAdd(storedCropLots[cropId], copy);
                    attached += copy.quantity;
                }
                if (attached < amount)
                {
                    ProductPassportService.MergeAdd(storedCropLots[cropId], ProductPassportService.CreateLegacyLot(cropId, amount - attached));
                }
            }
            else
            {
                ProductPassportService.MergeAdd(storedCropLots[cropId], ProductPassportService.CreateLegacyLot(cropId, amount));
            }

            OnWorkshopInventoryUpdated?.Invoke();
            Refresh3DVisuals();

            Debug.Log($"[WorkshopPalletManager] Atölye Paletine Eklendi: {cropId} +{amount} KG (Toplam: {storedCrops[cropId]} KG)");
        }

        public bool ConsumeCrop(string cropId, int amount)
        {
            return ConsumeCrop(cropId, amount, out _);
        }

        public bool ConsumeCrop(string cropId, int amount, out List<ProductLot> consumedLots)
        {
            consumedLots = new List<ProductLot>();
            if (string.IsNullOrEmpty(cropId) || amount <= 0) return false;
            if (!storedCrops.ContainsKey(cropId) || storedCrops[cropId] < amount) return false;

            EnsureLotList(cropId);
            SyncCounts(cropId);
            if (!storedCrops.ContainsKey(cropId) || storedCrops[cropId] < amount) return false;

            consumedLots = ProductPassportService.TakeFifo(storedCropLots[cropId], amount);
            storedCrops[cropId] -= amount;
            if (storedCrops[cropId] <= 0)
            {
                storedCrops.Remove(cropId);
                storedCropLots.Remove(cropId);
            }

            OnWorkshopInventoryUpdated?.Invoke();
            Refresh3DVisuals();
            return true;
        }

        public void SetAllCrops(Dictionary<string, int> crops)
        {
            SetAllCrops(crops, null);
        }

        public void SetAllCrops(Dictionary<string, int> crops, List<BarnCropSaveData> savedRows)
        {
            storedCrops = (crops != null) ? new Dictionary<string, int>(crops) : new Dictionary<string, int>();
            storedCropLots.Clear();
            foreach (var kvp in storedCrops)
            {
                List<ProductLot> savedLots = null;
                if (savedRows != null)
                {
                    BarnCropSaveData row = savedRows.Find(c => c != null && c.seedId == kvp.Key);
                    if (row != null) savedLots = row.lots;
                }
                storedCropLots[kvp.Key] = ProductPassportService.RestoreLotsOrLegacy(kvp.Key, kvp.Value, savedLots);
            }
            OnWorkshopInventoryUpdated?.Invoke();
            Refresh3DVisuals();
        }

        public List<BarnCropSaveData> ExportCropsForSave()
        {
            List<BarnCropSaveData> rows = new List<BarnCropSaveData>();
            foreach (var kvp in storedCrops)
            {
                if (string.IsNullOrEmpty(kvp.Key) || kvp.Value <= 0) continue;
                EnsureLotList(kvp.Key);
                rows.Add(new BarnCropSaveData
                {
                    seedId = kvp.Key,
                    count = kvp.Value,
                    lots = ProductPassportService.CloneLots(storedCropLots[kvp.Key])
                });
            }
            return rows;
        }

        public string GetPassportSummary(string cropId)
        {
            if (string.IsNullOrEmpty(cropId) || !storedCropLots.ContainsKey(cropId)) return "";
            return ProductPassportService.GetCardText(ProductPassportService.ResolveProductDisplayName(cropId), storedCropLots[cropId]);
        }

        private void EnsureLotList(string cropId)
        {
            if (!storedCropLots.ContainsKey(cropId) || storedCropLots[cropId] == null)
            {
                storedCropLots[cropId] = new List<ProductLot>();
            }
        }

        private void SyncCounts(string cropId)
        {
            EnsureLotList(cropId);
            int lotSum = ProductPassportService.SumLots(storedCropLots[cropId]);
            int count = storedCrops.ContainsKey(cropId) ? storedCrops[cropId] : 0;
            if (count > lotSum)
            {
                ProductPassportService.MergeAdd(storedCropLots[cropId], ProductPassportService.CreateLegacyLot(cropId, count - lotSum));
            }
            else if (count < lotSum)
            {
                ProductPassportService.TakeFifo(storedCropLots[cropId], lotSum - count);
            }
        }

        public void ClearAll()
        {
            storedCrops.Clear();
            storedCropLots.Clear();
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
