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

        public bool TryFetchPackageFromTruck(out WholesaleProductDef pDef)
        {
            pDef = null;
            if (PendingTruckPackages != null && PendingTruckPackages.Count > 0)
            {
                pDef = PendingTruckPackages[0];
                PendingTruckPackages.RemoveAt(0);
                return true;
            }
            return false;
        }

        public static bool DepositPackageToStorageShelf(WholesaleProductDef pDef, out PlacedFurnitureController usedShelf, out int usedRow)
        {
            usedShelf = null;
            usedRow = -1;
            if (pDef == null) return false;

            PlacedFurnitureController[] placedFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);

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
                ModalManager.ShowModal("Teslimat Noktası Dolu! ⚠️", "Şu anda yolda veya teslimat noktasında aktif bir kamyon (Toptancı veya Çiftlik Kamyonu) bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni toptan sipariş verilemez.", "Tamam");
                return false;
            }

            IsTruckOnTheWay = true;
            StartCoroutine(TruckLifecycleRoutine(orderList));
            return true;
        }

        private IEnumerator TruckLifecycleRoutine(List<WholesaleProductDef> orderList)
        {
            // 1. KAMYON MODELİNİ EN SAĞ UÇ NOKTADA DOĞUR (Spawn: X: 180, Y: 0.05, Z: -7.5)
            Transform[] wheels;
            Transform rearDoors;
            GameObject truckObj = WholesaleTruckModelBuilder.CreateTruckModel(out wheels, out rearDoors);
            
            Vector3 startPos = new Vector3(180f, 0.05f, -7.5f);
            Vector3 junctionPos = new Vector3(13.0f, 0.05f, -7.5f);   // Mal Kabul Sapağı
            Vector3 dockPos = new Vector3(13.0f, 0.05f, 1.5f);       // Mal Kabul İndirme Alanı
            Vector3 despawnPos = new Vector3(-180f, 0.05f, -7.5f);   // Harita Sonu Despawn

            truckObj.transform.position = startPos;
            truckObj.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            float driveSpeed = 10.5f; // Şehir trafiğiyle uyumlu temel hız
            float turnSpeed = 8.5f;
            float wheelRotateSpeed = 600.0f;
            float truckCurrentSpeed = driveSpeed;

            // 2. AŞAMA: ANA YOLDA SAĞ UÇTAN MAL KABUL SAPAĞINA KADAR İLERLE
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

            // 3. AŞAMA: KUZEYE DÖN VE MAL KABUL ALANINA GİR
            Quaternion targetRotNorth = Quaternion.Euler(0f, 0f, 0f);
            while (truckObj != null && Quaternion.Angle(truckObj.transform.rotation, targetRotNorth) > 2f)
            {
                truckObj.transform.rotation = Quaternion.Slerp(truckObj.transform.rotation, targetRotNorth, 12f * Time.deltaTime);
                yield return null;
            }
            if (truckObj != null) truckObj.transform.rotation = targetRotNorth;

            while (truckObj != null && Vector3.Distance(truckObj.transform.position, dockPos) > 0.2f)
            {
                truckObj.transform.position = Vector3.MoveTowards(truckObj.transform.position, dockPos, turnSpeed * Time.deltaTime);
                RotateWheels(wheels, wheelRotateSpeed * Time.deltaTime);
                yield return null;
            }

            if (truckObj != null) truckObj.transform.position = dockPos;

            // 4. AŞAMA: MAL KABUL DOKUNDA DUR VE KAPILARI AÇ
            if (rearDoors != null)
            {
                rearDoors.localRotation = Quaternion.Euler(0f, 35f, 0f);
            }
            PendingTruckPackages = (orderList != null) ? new List<WholesaleProductDef>(orderList) : new List<WholesaleProductDef>();
            int totalPacks = PendingTruckPackages.Count;
            int totalUnits = totalPacks * 50;

            IsTruckAtDockWaitingForUnload = true;

            GameObject statusTextObj = CreateTruckStatusText(truckObj, $"🚛 MAL KABUL: Reyoncu İndirmesi Bekleniyor... ({totalPacks} Koli / {totalUnits} Adet)");

            while (PendingTruckPackages.Count > 0)
            {
                int remainingPacks = PendingTruckPackages.Count;
                int remainingUnits = remainingPacks * 50;
                int unloadedUnits = totalUnits - remainingUnits;

                bool hasRestocker = (StaffManager.Instance != null && StaffManager.Instance.HasActiveRestocker());

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

            // 5. AŞAMA: TÜM MALZEMELER REYONCULAR TARAFINDAN DEPOYA BİZZAT İNDİRİLDİKTEN SONRA AYRIL
            IsTruckAtDockWaitingForUnload = false;
            string allUnloadedFmt = LocalizationManager.L("Truck_AllUnloaded", "✅ TÜM {0} ADET ÜRÜN REYONCULAR TARAFINDAN İNDİRİLDİ!\nKamyon Ayrılıyor...", "✅ ALL {0} ITEMS UNLOADED BY STOCKERS!\nTruck Departing...");
            UpdateTruckStatusText(statusTextObj, string.Format(allUnloadedFmt, totalUnits));
            yield return new WaitForSeconds(3.0f);

            if (statusTextObj != null) Destroy(statusTextObj);
            if (rearDoors != null) rearDoors.localRotation = Quaternion.identity; // Kapıları kapat
            yield return new WaitForSeconds(1.2f);

            // 6. AŞAMA: GERİ GERİ SAPAĞA ÇIK (Z: 1.5 -> Z: -7.5)
            while (truckObj != null && Vector3.Distance(truckObj.transform.position, junctionPos) > 0.3f)
            {
                truckObj.transform.position = Vector3.MoveTowards(truckObj.transform.position, junctionPos, (turnSpeed * 0.7f) * Time.deltaTime);
                RotateWheels(wheels, -wheelRotateSpeed * Time.deltaTime);
                yield return null;
            }

            if (truckObj != null) truckObj.transform.position = junctionPos;

            // 7. AŞAMA: SOLA DÖN VE EN SOLDA DESPAWN OL
            Quaternion targetRotWest = Quaternion.Euler(0f, -90f, 0f);
            while (truckObj != null && Quaternion.Angle(truckObj.transform.rotation, targetRotWest) > 2f)
            {
                truckObj.transform.rotation = Quaternion.Slerp(truckObj.transform.rotation, targetRotWest, 12f * Time.deltaTime);
                yield return null;
            }
            if (truckObj != null) truckObj.transform.rotation = targetRotWest;

            truckCurrentSpeed = driveSpeed;
            while (truckObj != null && Vector3.Distance(truckObj.transform.position, despawnPos) > 0.3f)
            {
                float targetLimit = driveSpeed;
                if (CityTrafficManager.Instance != null)
                {
                    targetLimit = CityTrafficManager.Instance.GetSpeedLimitInFront(truckObj.transform.position, Vector3.left, 16.0f, driveSpeed);
                }
                truckCurrentSpeed = Mathf.MoveTowards(truckCurrentSpeed, targetLimit, 9.0f * Time.deltaTime);

                truckObj.transform.position = Vector3.MoveTowards(truckObj.transform.position, despawnPos, truckCurrentSpeed * Time.deltaTime);
                RotateWheels(wheels, (truckCurrentSpeed / driveSpeed) * wheelRotateSpeed * Time.deltaTime);
                yield return null;
            }

            // 8. TEMİZLİK VE KİLİT AÇMA
            if (truckObj != null) Destroy(truckObj);
            IsTruckOnTheWay = false;

            Debug.Log("[WholesaleTruck] Kamyon tüm 50'li koli malzemelerini eksiksiz indirdikten sonra ayrıldı.");
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
            PlacedFurnitureController[] placedFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
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

            PlacedFurnitureController[] placedFurniture = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);

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
