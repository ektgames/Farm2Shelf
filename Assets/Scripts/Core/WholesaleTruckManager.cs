using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.UI;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Toptancı siparişlerinde çalışan Kapalı Kasa Kamyon Lojistik Yönetimi.
    /// Kamyona tıklama tamamen kaldırılmıştır. Kamyon Mal Kabul kapısı önünde durur,
    /// üstündeki canlı bilgi paneli ile tüm 50'li koli malzemelerinin stoklara indirilmesini
    /// adım adım bekler ve indirme tamamen tamamlanınca kapılarını kapatıp ayrılır.
    /// </summary>
    public class WholesaleTruckManager : MonoBehaviour
    {
        private static WholesaleTruckManager instance;
        public static WholesaleTruckManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<WholesaleTruckManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("WholesaleTruckManager");
                        instance = go.AddComponent<WholesaleTruckManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        public bool IsTruckOnTheWay { get; private set; } = false;
        public bool IsTruckAtDockWaitingForUnload { get; private set; } = false;
        public List<WholesaleProductDef> PendingTruckPackages { get; private set; } = new List<WholesaleProductDef>();
        private readonly List<WholesaleProductDef> activeDeliveryPackages = new List<WholesaleProductDef>();
        private readonly List<WholesaleProductDef> originalDeliveryPackages = new List<WholesaleProductDef>();
        private DeliveryTruckPhase currentPhase = DeliveryTruckPhase.Approaching;
        private GameObject activeTruck;
        public IReadOnlyList<WholesaleProductDef> PackagesForSave =>
            PendingTruckPackages.Count > 0 ? PendingTruckPackages : activeDeliveryPackages;

        public bool TryFetchPackageFromTruck(out WholesaleProductDef pDef)
        {
            pDef = null;
            if (PendingTruckPackages != null && PendingTruckPackages.Count > 0)
            {
                pDef = PendingTruckPackages[0];
                PendingTruckPackages.RemoveAt(0);
                string fetchedProductId = pDef != null ? pDef.id : null;
                int activeIndex = activeDeliveryPackages.FindIndex(p => p != null && p.id == fetchedProductId);
                if (activeIndex >= 0) activeDeliveryPackages.RemoveAt(activeIndex);
                return true;
            }
            return false;
        }

        public void ClearAllPackages()
        {
            StopAllCoroutines();
            IsTruckOnTheWay = false;
            IsTruckAtDockWaitingForUnload = false;
            currentPhase = DeliveryTruckPhase.Approaching;
            if (PendingTruckPackages != null) PendingTruckPackages.Clear();
            activeDeliveryPackages.Clear();
            originalDeliveryPackages.Clear();
            if (activeTruck != null)
            {
                Destroy(activeTruck);
                activeTruck = null;
            }
            DeliveryTruckVisuals.DestroyStrayTrucksAndPopups();
        }

        public DeliveryTruckSaveData CreateSaveSnapshot()
        {
            if (!IsTruckOnTheWay) return null;

            Vector3 pos = activeTruck != null ? activeTruck.transform.position : DeliveryTruckVisuals.StartPos;
            Vector3 euler = activeTruck != null ? activeTruck.transform.eulerAngles : DeliveryTruckVisuals.FacingWest.eulerAngles;

            DeliveryTruckSaveData data = new DeliveryTruckSaveData
            {
                isActive = true,
                truckKind = "Wholesale",
                phase = currentPhase.ToString(),
                posX = pos.x,
                posY = pos.y,
                posZ = pos.z,
                rotX = euler.x,
                rotY = euler.y,
                rotZ = euler.z,
                doorsOpen = currentPhase == DeliveryTruckPhase.Unloading
            };

            IReadOnlyList<WholesaleProductDef> remaining = PackagesForSave;
            if (remaining != null)
            {
                foreach (var package in remaining)
                {
                    if (package != null) data.remainingPackageIds.Add(package.id);
                }
            }

            foreach (var package in originalDeliveryPackages)
            {
                if (package != null) data.originalPackageIds.Add(package.id);
            }

            return data;
        }

        public void RestoreFromSave(DeliveryTruckSaveData data)
        {
            if (data == null) return;

            List<WholesaleProductDef> remaining = ResolveSavedPackages(data.remainingPackageIds);
            List<WholesaleProductDef> original = ResolveSavedPackages(data.originalPackageIds);
            if (original.Count == 0) original.AddRange(remaining);

            Vector3 pos = new Vector3(data.posX, data.posY, data.posZ);
            Quaternion rot = Quaternion.Euler(data.rotX, data.rotY, data.rotZ);
            DeliveryTruckPhase phase = DeliveryTruckVisuals.ParsePhase(data.phase, pos);

            IsTruckOnTheWay = true;
            currentPhase = phase;
            originalDeliveryPackages.Clear();
            originalDeliveryPackages.AddRange(original);
            activeDeliveryPackages.Clear();
            activeDeliveryPackages.AddRange(remaining.Count > 0 ? remaining : original);
            if (phase >= DeliveryTruckPhase.Unloading)
            {
                PendingTruckPackages = new List<WholesaleProductDef>(remaining);
            }

            StartCoroutine(TruckLifecycleRoutine(remaining, original, phase, pos, rot));
        }

        private static List<WholesaleProductDef> ResolveSavedPackages(List<string> productIds)
        {
            List<WholesaleProductDef> products = new List<WholesaleProductDef>();
            if (productIds == null) return products;
            foreach (string productId in productIds)
            {
                WholesaleProductDef product = WholesaleDatabase.GetProductById(productId);
                if (product != null) products.Add(product);
            }
            return products;
        }

        public static bool DepositPackageToStorageShelf(WholesaleProductDef pDef, out PlacedFurnitureController usedShelf, out int usedRow)
        {
            usedShelf = null;
            usedRow = -1;
            if (pDef == null) return false;

            var placedFurniture = PlacedFurnitureController.AllPlacedFurniture;

            foreach (var f in placedFurniture)
            {
                if (f == null || f.rows == null || f.FurnitureType != FurnitureType.StorageShelf) continue;
                for (int i = 0; i < f.rows.Length; i++)
                {
                    var row = f.rows[i];
                    if (row != null && (row.IsUnassigned || row.IsEmpty || row.productName == pDef.name))
                    {
                        int spaceLeft = row.maxCapacity - row.currentStock;
                        if (spaceLeft > 0)
                        {
                            row.productName = pDef.name;
                            row.productId = pDef.id;
                            row.unitPrice = WholesaleDatabase.GetProductSalePrice(pDef.id);

                            int amountToAdd = Mathf.Min(spaceLeft, pDef.packQuantity);
                            row.currentStock += amountToAdd;
                            f.UpdateRow3DProductMeshes(row.rowId);
                            usedShelf = f;
                            usedRow = i;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Toptancıdan 50'li koli siparişi verildiğinde kamyon lojistiğini başlatır.
        /// </summary>
        public bool DispatchWholesaleDelivery(List<WholesaleProductDef> orderList)
        {
            bool isAnyActive = IsTruckOnTheWay || (GreenTruckDeliveryManager.Instance != null && GreenTruckDeliveryManager.Instance.IsTruckOnTheWay);
            if (isAnyActive)
            {
                ModalManager.ShowModal(
                    LocalizationManager.L("Modal_DockBusy_Title", "Teslimat Noktası Dolu! ⚠️", "Delivery Dock Occupied! ⚠️"),
                    LocalizationManager.L("Modal_DockBusy_WholesaleBody", "Şu anda yolda veya teslimat noktasında aktif bir kamyon (Toptancı veya Çiftlik Kamyonu) bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni toptan sipariş verilemez.", "A wholesaler or farm truck is currently en route or at the delivery dock.\n\nWait until it completes delivery and leaves before placing another wholesale order."),
                    LocalizationManager.L("Btn_OK", "Tamam", "OK"));
                return false;
            }

            IsTruckOnTheWay = true;
            currentPhase = DeliveryTruckPhase.Approaching;
            originalDeliveryPackages.Clear();
            if (orderList != null) originalDeliveryPackages.AddRange(orderList);
            activeDeliveryPackages.Clear();
            if (orderList != null) activeDeliveryPackages.AddRange(orderList);
            StartCoroutine(TruckLifecycleRoutine(
                orderList ?? new List<WholesaleProductDef>(),
                originalDeliveryPackages,
                DeliveryTruckPhase.Approaching,
                DeliveryTruckVisuals.StartPos,
                DeliveryTruckVisuals.FacingWest));
            return true;
        }

        private IEnumerator TruckLifecycleRoutine(
            List<WholesaleProductDef> remainingPackages,
            List<WholesaleProductDef> originalPackages,
            DeliveryTruckPhase startPhase,
            Vector3 spawnPos,
            Quaternion spawnRot)
        {
            Transform[] wheels;
            Transform rearDoors;
            GameObject truckObj = WholesaleTruckModelBuilder.CreateTruckModel(out wheels, out rearDoors);
            truckObj.name = DeliveryTruckVisuals.WholesaleTruckName;
            activeTruck = truckObj;

            Vector3 junctionPos = DeliveryTruckVisuals.JunctionPos;
            Vector3 dockPos = DeliveryTruckVisuals.DockPos;

            truckObj.transform.position = spawnPos;
            truckObj.transform.rotation = spawnRot;

            float driveSpeed = 10.5f;
            float turnSpeed = 8.5f;
            float wheelRotateSpeed = 600.0f;
            float truckCurrentSpeed = driveSpeed;
            DeliveryTruckPhase phase = startPhase;
            GameObject statusTextObj = null;
            int originalPackCount = originalPackages != null ? originalPackages.Count : (remainingPackages != null ? remainingPackages.Count : 0);
            int totalUnits = originalPackCount * 50;

            if (phase <= DeliveryTruckPhase.Approaching)
            {
                currentPhase = DeliveryTruckPhase.Approaching;
                while (truckObj != null && Vector3.Distance(truckObj.transform.position, junctionPos) > 0.3f)
                {
                    float targetLimit = driveSpeed;
                    if (CityTrafficManager.Instance != null)
                    {
                        targetLimit = CityTrafficManager.Instance.GetSpeedLimitInFront(truckObj.transform.position, Vector3.left, 16.0f, driveSpeed);
                    }
                    truckCurrentSpeed = Mathf.MoveTowards(truckCurrentSpeed, targetLimit, 9.0f * Time.deltaTime);
                    truckObj.transform.position = Vector3.MoveTowards(truckObj.transform.position, junctionPos, truckCurrentSpeed * Time.deltaTime);
                    RotateWheels(wheels, (truckCurrentSpeed / driveSpeed) * wheelRotateSpeed * Time.deltaTime);
                    yield return null;
                }
                if (truckObj != null) truckObj.transform.position = junctionPos;
                phase = DeliveryTruckPhase.TurningToDock;
            }

            if (phase <= DeliveryTruckPhase.TurningToDock)
            {
                currentPhase = DeliveryTruckPhase.TurningToDock;
                Quaternion targetRotNorth = DeliveryTruckVisuals.FacingNorth;
                while (truckObj != null && Quaternion.Angle(truckObj.transform.rotation, targetRotNorth) > 2f)
                {
                    truckObj.transform.rotation = Quaternion.Slerp(truckObj.transform.rotation, targetRotNorth, 12f * Time.deltaTime);
                    yield return null;
                }
                if (truckObj != null) truckObj.transform.rotation = targetRotNorth;
                phase = DeliveryTruckPhase.EnteringDock;
            }

            if (phase <= DeliveryTruckPhase.EnteringDock)
            {
                currentPhase = DeliveryTruckPhase.EnteringDock;
                while (truckObj != null && Vector3.Distance(truckObj.transform.position, dockPos) > 0.2f)
                {
                    truckObj.transform.position = Vector3.MoveTowards(truckObj.transform.position, dockPos, turnSpeed * Time.deltaTime);
                    RotateWheels(wheels, wheelRotateSpeed * Time.deltaTime);
                    yield return null;
                }
                if (truckObj != null) truckObj.transform.position = dockPos;
                phase = DeliveryTruckPhase.Unloading;
            }

            if (phase <= DeliveryTruckPhase.Unloading)
            {
                currentPhase = DeliveryTruckPhase.Unloading;
                if (rearDoors != null) rearDoors.localRotation = Quaternion.Euler(0f, 35f, 0f);
                if (PendingTruckPackages == null || PendingTruckPackages.Count == 0)
                {
                    PendingTruckPackages = remainingPackages != null
                        ? new List<WholesaleProductDef>(remainingPackages)
                        : new List<WholesaleProductDef>();
                }

                IsTruckAtDockWaitingForUnload = PendingTruckPackages.Count > 0;
                string initialFmt = LocalizationManager.L("Truck_InitialStatusFmt", "🚛 MAL KABUL: Reyoncu İndirmesi Bekleniyor... ({0} Koli / {1} Adet)", "🚛 GOODS RECEIPT: Waiting for Stocker... ({0} Packs / {1} Pcs)");
                statusTextObj = CreateTruckStatusText(truckObj, string.Format(initialFmt, PendingTruckPackages.Count, PendingTruckPackages.Count * 50));

                while (PendingTruckPackages.Count > 0)
                {
                    int remainingPacks = PendingTruckPackages.Count;
                    int remainingUnits = remainingPacks * 50;
                    int unloadedUnits = totalUnits - remainingUnits;
                    bool hasRestocker = StaffManager.Instance != null && StaffManager.Instance.HasActiveRestocker();

                    if (!hasRestocker)
                    {
                        string noStockerFmt = LocalizationManager.L("Truck_NoStocker", "⚠️ REYONCU YOK! (Mal Kabul Bekliyor):\n📦 {0} Koli Bekliyor - Lütfen Reyoncu İşe Alın!", "⚠️ NO STOCKER! (Goods Receipt Waiting):\n📦 {0} Packs Waiting - Please Hire a Stocker!");
                        UpdateTruckStatusText(statusTextObj, string.Format(noStockerFmt, remainingPacks));
                        yield return new WaitForSeconds(1.0f);
                    }
                    else
                    {
                        string unloadingFmt = LocalizationManager.L("Truck_Unloading", "🚛 MAL KABUL (Reyoncu Kolileri İndiriyor):\n📦 {0} Koli Kamyonda Bekliyor ({1} / {2} Adet İndirildi)", "🚛 GOODS RECEIPT (Stocker Unloading Boxes):\n📦 {0} Packs Waiting on Truck ({1} / {2} Pcs Unloaded)");
                        UpdateTruckStatusText(statusTextObj, string.Format(unloadingFmt, remainingPacks, unloadedUnits, totalUnits));
                        yield return new WaitForSeconds(0.6f);
                    }
                }

                IsTruckAtDockWaitingForUnload = false;
                currentPhase = DeliveryTruckPhase.LeavingDock;
                string allUnloadedFmt = LocalizationManager.L("Truck_AllUnloaded", "✅ TÜM {0} ADET ÜRÜN REYONCULAR TARAFINDAN İNDİRİLDİ!\nKamyon Ayrılıyor...", "✅ ALL {0} ITEMS UNLOADED BY STOCKERS!\nTruck Departing...");
                UpdateTruckStatusText(statusTextObj, string.Format(allUnloadedFmt, totalUnits));
                yield return new WaitForSeconds(3.0f);
                if (statusTextObj != null) Destroy(statusTextObj);
                statusTextObj = null;
                if (rearDoors != null) rearDoors.localRotation = Quaternion.identity;
                yield return new WaitForSeconds(1.2f);
                phase = DeliveryTruckPhase.LeavingDock;
            }

            if (phase <= DeliveryTruckPhase.LeavingDock)
            {
                currentPhase = DeliveryTruckPhase.LeavingDock;
                if (rearDoors != null) rearDoors.localRotation = Quaternion.identity;
                if (statusTextObj != null) Destroy(statusTextObj);
                while (truckObj != null && Vector3.Distance(truckObj.transform.position, junctionPos) > 0.3f)
                {
                    truckObj.transform.position = Vector3.MoveTowards(truckObj.transform.position, junctionPos, (turnSpeed * 0.7f) * Time.deltaTime);
                    RotateWheels(wheels, -wheelRotateSpeed * Time.deltaTime);
                    yield return null;
                }
                if (truckObj != null) truckObj.transform.position = junctionPos;
                phase = DeliveryTruckPhase.TurningToDepart;
            }

            if (phase <= DeliveryTruckPhase.TurningToDepart)
            {
                currentPhase = DeliveryTruckPhase.TurningToDepart;
                Quaternion targetRotWest = DeliveryTruckVisuals.FacingWest;
                while (truckObj != null && Quaternion.Angle(truckObj.transform.rotation, targetRotWest) > 2f)
                {
                    truckObj.transform.rotation = Quaternion.Slerp(truckObj.transform.rotation, targetRotWest, 12f * Time.deltaTime);
                    yield return null;
                }
                if (truckObj != null) truckObj.transform.rotation = targetRotWest;
                phase = DeliveryTruckPhase.Departing;
            }

            if (phase <= DeliveryTruckPhase.Departing)
            {
                currentPhase = DeliveryTruckPhase.Departing;
                truckCurrentSpeed = driveSpeed;
                Vector3 finalDespawnPos = DeliveryTruckVisuals.DespawnPos;
                while (truckObj != null && truckObj.transform.position.x > -339.5f)
                {
                    float targetLimit = driveSpeed;
                    if (CityTrafficManager.Instance != null)
                    {
                        targetLimit = CityTrafficManager.Instance.GetSpeedLimitInFront(truckObj.transform.position, Vector3.left, 16.0f, driveSpeed);
                    }
                    truckCurrentSpeed = Mathf.MoveTowards(truckCurrentSpeed, targetLimit, 9.0f * Time.deltaTime);

                    Vector3 currentPos = truckObj.transform.position;
                    Vector3 nextPos = Vector3.MoveTowards(currentPos, finalDespawnPos, truckCurrentSpeed * Time.deltaTime);
                    float slopeY;
                    float bridgeY = CityTrafficManager.GetBridgeElevation(nextPos.x, nextPos.z, out slopeY);
                    nextPos.y = bridgeY;
                    Vector3 tangentDir = new Vector3(-1f, slopeY * -1f, 0f).normalized;
                    Quaternion targetRot = Quaternion.LookRotation(tangentDir, Vector3.up);
                    truckObj.transform.rotation = Quaternion.RotateTowards(truckObj.transform.rotation, targetRot, 360f * Time.deltaTime);
                    truckObj.transform.position = nextPos;
                    RotateWheels(wheels, (truckCurrentSpeed / driveSpeed) * wheelRotateSpeed * Time.deltaTime);
                    yield return null;
                }
            }

            if (truckObj != null) Destroy(truckObj);
            activeTruck = null;
            IsTruckOnTheWay = false;
            IsTruckAtDockWaitingForUnload = false;
            activeDeliveryPackages.Clear();
            originalDeliveryPackages.Clear();
            if (PendingTruckPackages != null) PendingTruckPackages.Clear();
        }

        private void RotateWheels(Transform[] wheels, float deltaAngle)
        {
            if (wheels == null) return;
            foreach (var w in wheels)
            {
                if (w != null) w.Rotate(Vector3.right * deltaAngle, Space.Self);
            }
        }

        public static PlacedFurnitureController GetNextAvailableStorageShelfForProduct(WholesaleProductDef pDef)
        {
            if (pDef == null) return null;
            var placedFurniture = PlacedFurnitureController.AllPlacedFurniture;
            foreach (var f in placedFurniture)
            {
                if (f == null || f.rows == null || f.FurnitureType != FurnitureType.StorageShelf) continue;
                for (int i = 0; i < f.rows.Length; i++)
                {
                    var row = f.rows[i];
                    if (row != null && (row.IsUnassigned || row.IsEmpty || row.productName == pDef.name))
                    {
                        int spaceLeft = row.maxCapacity - row.currentStock;
                        if (spaceLeft > 0)
                        {
                            return f;
                        }
                    }
                }
            }
            return null;
        }

        private bool TryDepositProductPack(WholesaleProductDef pDef, out int depositedUnits)
        {
            depositedUnits = 0;
            if (pDef == null) return true;

            var placedFurniture = PlacedFurnitureController.AllPlacedFurniture;

            // 1. ÖNCELİK: Mağazada Oyuncunun Bizzat Atadığı Rafı / Dolabı Bul (Atanmamış / Boş Raflara Asla Rastgele Ürün Koyulmaz!)
            foreach (var f in placedFurniture)
            {
                if (f == null || f.rows == null || f.FurnitureType == FurnitureType.StorageShelf) continue;
                if (f.FurnitureType == pDef.targetShelfType)
                {
                    for (int i = 0; i < f.rows.Length; i++)
                    {
                        var row = f.rows[i];
                        if (row != null && !row.IsUnassigned && row.productName == pDef.name)
                        {
                            int spaceLeft = row.maxCapacity - row.currentStock;
                            if (spaceLeft > 0)
                            {
                                int amountToAdd = Mathf.Min(spaceLeft, pDef.packQuantity);
                                row.currentStock += amountToAdd;
                                depositedUnits = amountToAdd;
                                f.UpdateRow3DProductMeshes(row.rowId);
                                return true;
                            }
                        }
                    }
                }
            }

            // 2. İKİNCİ ÖNCELİK: Mağaza Rafında Yer Yoksa veya Ürün Atanmamışsa -> Depo Rafına (Storage Shelf) Bırak!
            foreach (var f in placedFurniture)
            {
                if (f == null || f.rows == null || f.FurnitureType != FurnitureType.StorageShelf) continue;
                for (int i = 0; i < f.rows.Length; i++)
                {
                    var row = f.rows[i];
                    if (row != null && (row.IsUnassigned || row.IsEmpty || row.productName == pDef.name))
                    {
                        int spaceLeft = row.maxCapacity - row.currentStock;
                        if (spaceLeft > 0)
                        {
                            row.productName = pDef.name;
                            row.unitPrice = WholesaleDatabase.GetProductSalePrice(pDef.id);

                            int amountToAdd = Mathf.Min(spaceLeft, pDef.packQuantity);
                            row.currentStock += amountToAdd;
                            depositedUnits = amountToAdd;
                            f.UpdateRow3DProductMeshes(row.rowId);
                            return true;
                        }
                    }
                }
            }

            return false; // Depoda veya atanmış rafta yer yoksa kamyon beklemeye geçer!
        }

        private GameObject CreateTruckStatusText(GameObject parent, string text)
        {
            GameObject popupObj = new GameObject("Popup_TruckStatus");
            popupObj.transform.position = parent.transform.position + Vector3.up * 3.5f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, 90f);
            popupObj.transform.localScale = Vector3.one * 0.015f;

            if (Camera.main != null)
            {
                popupObj.transform.rotation = Camera.main.transform.rotation;
            }

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = txtObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = UIStyleUtility.GetGlobalFont(22);
            txt.text = text;
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.98f, 0.85f, 0.20f);
            txt.raycastTarget = false;

            return popupObj;
        }

        private void UpdateTruckStatusText(GameObject popupObj, string newText)
        {
            if (popupObj != null)
            {
                UnityEngine.UI.Text txt = popupObj.GetComponentInChildren<UnityEngine.UI.Text>();
                if (txt != null) txt.text = newText;
            }
        }
    }
}
