using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.UI;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Ahırdan dükkana gönderilen çiftlik mahsullerini taşıyan Yeşil Kasa Teslimat Kamyonu Lojistiği.
    /// Kamyon Mal Kabul kapısına yanaşır, reyoncu kolileri depoya indirir.
    /// </summary>
    public class GreenTruckDeliveryManager : MonoBehaviour
    {
        private static GreenTruckDeliveryManager instance;
        public static GreenTruckDeliveryManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindFirstObjectByType<GreenTruckDeliveryManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GreenTruckDeliveryManager");
                        instance = go.AddComponent<GreenTruckDeliveryManager>();
                    }
                }
                return instance;
            }
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

        public bool DispatchFarmDelivery(List<WholesaleProductDef> farmProductList)
        {
            bool isAnyActive = IsTruckOnTheWay || (WholesaleTruckManager.Instance != null && WholesaleTruckManager.Instance.IsTruckOnTheWay);
            if (isAnyActive)
            {
                ModalManager.ShowModal("Teslimat Noktası Dolu! ⚠️", "Şu anda yolda veya teslimat noktasında aktif bir kamyon (Toptancı veya Çiftlik Kamyonu) bulunmaktadır!\n\nKamyon teslimatı tamamlayıp ayrılana kadar yeni çiftlik sevkiyatı yapılamaz.", "Tamam");
                return false;
            }

            IsTruckOnTheWay = true;
            StartCoroutine(GreenTruckLifecycleRoutine(farmProductList));
            return true;
        }

        private IEnumerator GreenTruckLifecycleRoutine(List<WholesaleProductDef> farmList)
        {
            Transform[] wheels;
            Transform rearDoors;
            // Parlak Yeşil Kabin ve Yeşil Şeritli Kasa
            GameObject truckObj = WholesaleTruckModelBuilder.CreateTruckModel(out wheels, out rearDoors, new Color(0.15f, 0.65f, 0.25f), new Color(0.20f, 0.85f, 0.35f));

            Vector3 startPos = new Vector3(180f, 0.05f, -7.5f);
            Vector3 junctionPos = new Vector3(13.0f, 0.05f, -7.5f);
            Vector3 dockPos = new Vector3(13.0f, 0.05f, 1.5f);
            Vector3 despawnPos = new Vector3(-180f, 0.05f, -7.5f);

            truckObj.transform.position = startPos;
            truckObj.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            float driveSpeed = 10.5f;
            float turnSpeed = 8.5f;
            float wheelRotateSpeed = 600.0f;
            float currentSpeed = driveSpeed;

            // 1. Sağ Uçtan Sapağa İlerle
            while (truckObj != null && Vector3.Distance(truckObj.transform.position, junctionPos) > 0.3f)
            {
                truckObj.transform.position = Vector3.MoveTowards(truckObj.transform.position, junctionPos, currentSpeed * Time.deltaTime);
                RotateWheels(wheels, wheelRotateSpeed * Time.deltaTime);
                yield return null;
            }

            if (truckObj != null) truckObj.transform.position = junctionPos;

            // 2. Kuzeye Dön ve Doka Gir
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

            // 3. Kapıları Aç ve Malzemeleri Depo Rafına İndir (Tüm Koliler Biten Kadar Bekle)
            if (rearDoors != null) rearDoors.localRotation = Quaternion.Euler(0f, 35f, 0f);

            PendingTruckPackages = (farmList != null) ? new List<WholesaleProductDef>(farmList) : new List<WholesaleProductDef>();
            int totalPacks = PendingTruckPackages.Count;
            int totalUnits = totalPacks * 50;

            IsTruckAtDockWaitingForUnload = true;

            GameObject statusTextObj = CreateTruckStatusText(truckObj, $"🚛 ÇİFTLİK MAL KABUL: Reyoncu İndirmesi Bekleniyor... ({totalPacks} Koli / {totalUnits} Adet)");

            while (PendingTruckPackages.Count > 0)
            {
                int remainingPacks = PendingTruckPackages.Count;
                int remainingUnits = remainingPacks * 50;
                int unloadedUnits = totalUnits - remainingUnits;

                bool hasRestocker = (StaffManager.Instance != null && StaffManager.Instance.HasActiveRestocker());

                if (!hasRestocker)
                {
                    string noStockerFmt = LocalizationManager.L("FarmTruck_NoStocker", "⚠️ REYONCU YOK! (Çiftlik Mal Kabul Bekliyor):\n📦 {0} Koli Bekliyor - Lütfen Reyoncu İşe Alın!", "⚠️ NO STOCKER! (Farm Delivery Waiting):\n📦 {0} Packs Waiting - Please Hire a Stocker!");
                    UpdateTruckStatusText(statusTextObj, string.Format(noStockerFmt, remainingPacks));
                    yield return new WaitForSeconds(1.0f);
                }
                else
                {
                    string unloadingFmt = LocalizationManager.L("FarmTruck_Unloading", "🚛 ÇİFTLİK MAL KABUL (Reyoncu Kolileri İndiriyor):\n📦 {0} Koli Kamyonda Bekliyor ({1} / {2} Adet İndirildi)", "🚛 FARM GOODS RECEIPT (Stocker Unloading Boxes):\n📦 {0} Packs Waiting on Truck ({1} / {2} Pcs Unloaded)");
                    UpdateTruckStatusText(statusTextObj, string.Format(unloadingFmt, remainingPacks, unloadedUnits, totalUnits));
                    yield return new WaitForSeconds(0.6f);
                }
            }

            IsTruckAtDockWaitingForUnload = false;
            string allUnloadedFmt = LocalizationManager.L("FarmTruck_AllUnloaded", "✅ TÜM {0} ADET ÇİFTLİK MAHSULÜ İNDİRİLDİ!\nKamyon Ayrılıyor...", "✅ ALL {0} FARM CROPS UNLOADED!\nTruck Departing...");
            UpdateTruckStatusText(statusTextObj, string.Format(allUnloadedFmt, totalUnits));
            yield return new WaitForSeconds(3.0f);

            if (statusTextObj != null) Destroy(statusTextObj);
            if (rearDoors != null) rearDoors.localRotation = Quaternion.identity;
            yield return new WaitForSeconds(1.2f);

            // 4. Geri Geri Çık ve Despawn Ol
            while (truckObj != null && Vector3.Distance(truckObj.transform.position, junctionPos) > 0.3f)
            {
                truckObj.transform.position = Vector3.MoveTowards(truckObj.transform.position, junctionPos, (turnSpeed * 0.7f) * Time.deltaTime);
                RotateWheels(wheels, -wheelRotateSpeed * Time.deltaTime);
                yield return null;
            }

            if (truckObj != null) truckObj.transform.position = junctionPos;

            Quaternion targetRotWest = Quaternion.Euler(0f, -90f, 0f);
            while (truckObj != null && Quaternion.Angle(truckObj.transform.rotation, targetRotWest) > 2f)
            {
                truckObj.transform.rotation = Quaternion.Slerp(truckObj.transform.rotation, targetRotWest, 12f * Time.deltaTime);
                yield return null;
            }
            if (truckObj != null) truckObj.transform.rotation = targetRotWest;

            while (truckObj != null && Vector3.Distance(truckObj.transform.position, despawnPos) > 0.3f)
            {
                truckObj.transform.position = Vector3.MoveTowards(truckObj.transform.position, despawnPos, driveSpeed * Time.deltaTime);
                RotateWheels(wheels, wheelRotateSpeed * Time.deltaTime);
                yield return null;
            }

            if (truckObj != null) Destroy(truckObj);
            IsTruckOnTheWay = false;
        }

        private void RotateWheels(Transform[] wheels, float deltaAngle)
        {
            if (wheels == null) return;
            foreach (var w in wheels)
            {
                if (w != null) w.Rotate(Vector3.right, deltaAngle, Space.Self);
            }
        }

        private GameObject CreateTruckStatusText(GameObject parent, string text)
        {
            GameObject popupObj = new GameObject("Popup_GreenTruckStatus");
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
            txt.color = new Color(0.20f, 0.95f, 0.35f);
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
