using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;
using Farm2Shelf.Environment;

namespace Farm2Shelf.Core
{
    [System.Serializable]
    public class ShelfRowData
    {
        public int rowId;
        public string productName;
        public string productId;
        public int currentStock;
        public int maxCapacity;
        public float unitPrice;

        public ShelfRowData(int rowId, string productName, int currentStock, int maxCapacity, float unitPrice, string productId = "")
        {
            this.rowId = rowId;
            this.productName = productName;
            this.productId = productId;
            this.currentStock = currentStock;
            this.maxCapacity = maxCapacity;
            this.unitPrice = unitPrice;
        }

        public bool IsFull => currentStock >= maxCapacity;
        public bool IsUnassigned => string.IsNullOrEmpty(productName) || productName.StartsWith("Boş");
        public bool IsEmpty => currentStock <= 0;
    }

    /// <summary>
    /// Alt objelerdeki (ör. raf tahtaları, cam kapılar, ürün kutuları) tıklama ve dokunmaları
    /// ebeveyn PlacedFurnitureController nesnesine iletir.
    /// </summary>
    public class ChildClickForwarder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public PlacedFurnitureController parentController;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (parentController != null) parentController.OnPointerDown(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (parentController != null) parentController.OnPointerUp(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (parentController != null) parentController.OnPointerClick(eventData);
        }
    }

    /// <summary>
    /// Haritaya kurulmuş mobilya ve dekorasyonların etkileşim & pasif gelir yöneticisi.
    /// Tek Tık / Dokunma: Doğrudan Ürün/Stok Arayüzünü Açar.
    /// Uzun Basma (0.5 sn): Mobilyayı Ele Alır / Taşıma Moduna Geçirir.
    /// </summary>
    public class PlacedFurnitureController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public FurnitureType FurnitureType { get; private set; }
        public Vector3 OriginalPosition { get; private set; }
        public Quaternion OriginalRotation { get; private set; }

        public ShelfRowData[] rows = new ShelfRowData[4];
        public int TotalEarnedPassiveIncome { get; private set; } = 0;

        private Coroutine longPressCoroutine;
        private Coroutine passiveIncomeCoroutine;
        private bool isLongPressTriggered = false;
        public bool IsLongPressTriggered => isLongPressTriggered;

        private GameObject seasonWarningCanvasObj;

        public static readonly List<PlacedFurnitureController> AllPlacedFurniture = new List<PlacedFurnitureController>();

        private void OnEnable()
        {
            if (!AllPlacedFurniture.Contains(this)) AllPlacedFurniture.Add(this);

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateUpdated += HandleDateUpdatedForSeasonBadge;
            }
        }

        private void OnDisable()
        {
            AllPlacedFurniture.Remove(this);

            if (longPressCoroutine != null)
            {
                StopCoroutine(longPressCoroutine);
                longPressCoroutine = null;
            }

            if (passiveIncomeCoroutine != null)
            {
                StopCoroutine(passiveIncomeCoroutine);
                passiveIncomeCoroutine = null;
            }

            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDateUpdated -= HandleDateUpdatedForSeasonBadge;
            }
        }

        private void OnDestroy()
        {
            AllPlacedFurniture.Remove(this);
        }

        private void HandleDateUpdatedForSeasonBadge(TimeManager.Season season, int day, int year)
        {
            UpdateSeasonWarningBadge();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen) return;
            if (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing) return;

            isLongPressTriggered = false;
            if (longPressCoroutine != null) StopCoroutine(longPressCoroutine);
            longPressCoroutine = StartCoroutine(LongPressRoutine());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (longPressCoroutine != null)
            {
                StopCoroutine(longPressCoroutine);
                longPressCoroutine = null;
            }
        }

        private IEnumerator LongPressRoutine()
        {
            // Mobil ve PC için: 0.50 saniye basılı tutulunca mobilya taşıma moduna girer!
            yield return new WaitForSeconds(0.50f);
            isLongPressTriggered = true;

            // Görsel Geri Bildirim Pop-up
            ShowPickupHapticFeedback();

            // Mobilyayı Ele Al (Taşıma Modu)
            PickUpFurniture();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (isLongPressTriggered) return;

            OnClickDetected();
        }

        public void Setup(FurnitureType type, Vector3 pos, Quaternion rot, ShelfRowData[] existingRows = null)
        {
            this.FurnitureType = type;
            this.OriginalPosition = pos;
            this.OriginalRotation = rot;

            if (existingRows != null && existingRows.Length > 0)
            {
                this.rows = new ShelfRowData[existingRows.Length];
                for (int i = 0; i < existingRows.Length; i++)
                {
                    if (existingRows[i] != null)
                    {
                        this.rows[i] = new ShelfRowData(
                            existingRows[i].rowId,
                            existingRows[i].productName,
                            existingRows[i].currentStock,
                            existingRows[i].maxCapacity,
                            existingRows[i].unitPrice,
                            existingRows[i].productId
                        );
                    }
                }
            }
            else
            {
                InitializeRows();
            }

            // GÖRSEL 3D ÜRÜN MESH'LERİNİ ANINDA ÜRET & YENİLE!
            UpdateAll3DProductMeshes();
            UpdateSeasonWarningBadge();

            EnsureTouchColliders();
            EnsureChildClickForwarders();
            StartPassiveIncomeRoutine();
        }

        public static bool IsWalkableFloorDecoration(FurnitureType type)
        {
            return type == FurnitureType.WelcomeMat || type == FurnitureType.RedCarpet;
        }

        private static float lastGlobalClickTime = 0f;

        private void Update()
        {
            if (seasonWarningCanvasObj != null && Camera.main != null)
            {
                seasonWarningCanvasObj.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private Vector2 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.isPressed)
                return UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            if (UnityEngine.InputSystem.Pointer.current != null)
                return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return Vector2.zero;
#else
            try
            {
                if (Input.touchCount > 0) return Input.GetTouch(0).position;
                return Input.mousePosition;
            }
            catch { return Vector2.zero; }
#endif
        }

        private bool WasPointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;
            if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                return true;
            return false;
#else
            try
            {
                if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                    return true;
            }
            catch {}

            try
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began))
                    return true;
            }
            catch {}

            return false;
#endif
        }

        private bool IsPointerOverUIButton()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;

            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = GetPointerPosition()
            };
            System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject != null)
                {
                    Canvas parentCanvas = r.gameObject.GetComponentInParent<Canvas>();
                    if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace) continue;

                    if (r.gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null || r.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void EnsureTouchColliders()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] != null) combinedBounds.Encapsulate(renderers[i].bounds);
                }

                BoxCollider boxCol = GetComponent<BoxCollider>();
                if (boxCol == null) boxCol = gameObject.AddComponent<BoxCollider>();

                Vector3 localCenter = transform.InverseTransformPoint(combinedBounds.center);
                Vector3 localSize = transform.InverseTransformVector(combinedBounds.size);
                localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

                float extraX = (FurnitureType == FurnitureType.StorageShelf) ? 0.65f : 0.35f;
                float extraY = (FurnitureType == FurnitureType.StorageShelf) ? 0.65f : 0.35f;
                float extraZ = (FurnitureType == FurnitureType.StorageShelf) ? 0.65f : 0.35f;

                boxCol.center = localCenter;
                boxCol.size = new Vector3(
                    Mathf.Max(1.5f, localSize.x + extraX),
                    Mathf.Max(2.0f, localSize.y + extraY),
                    Mathf.Max(1.5f, localSize.z + extraZ)
                );

                if (IsWalkableFloorDecoration(FurnitureType))
                {
                    boxCol.isTrigger = true;

                    Collider[] childColliders = GetComponentsInChildren<Collider>();
                    foreach (var c in childColliders)
                    {
                        if (c != null) c.isTrigger = true;
                    }
                }
                else
                {
                    boxCol.isTrigger = false;

                    UnityEngine.AI.NavMeshObstacle navObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
                    if (navObstacle == null) navObstacle = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    navObstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                    navObstacle.center = boxCol.center;
                    navObstacle.size = boxCol.size;
                    navObstacle.carving = true;
                    navObstacle.carveOnlyStationary = false;
                    navObstacle.carvingTimeToStationary = 0.1f;
                }
            }

            EnsureChildClickForwarders();
        }

        public void EnsureChildClickForwarders()
        {
            Collider[] childColliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in childColliders)
            {
                if (col == null || col.gameObject == this.gameObject) continue;
                if (col.GetComponent<ChildClickForwarder>() == null)
                {
                    ChildClickForwarder forwarder = col.gameObject.AddComponent<ChildClickForwarder>();
                    forwarder.parentController = this;
                }
            }
        }

        private void InitializeRows()
        {
            int rowCount = (FurnitureType == FurnitureType.StorageShelf) ? 10 : 4;
            int capacityPerRow = 50; // TÜM RAF, DOLAP VE TEZGAHLAR 50 ADET (1 KOLİ) SIĞACAK ŞEKİLDE AYARLANDI

            rows = new ShelfRowData[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                rows[i] = new ShelfRowData(i + 1, "Boş", 0, capacityPerRow, 0f);
            }
        }

        private void StartPassiveIncomeRoutine()
        {
            if (passiveIncomeCoroutine != null) StopCoroutine(passiveIncomeCoroutine);

            FurnitureItemDef def = FurnitureDatabase.GetDef(FurnitureType);
            if (def != null && def.category == FurnitureCategory.Decoration && def.passiveIncomePerUse > 0)
            {
                passiveIncomeCoroutine = StartCoroutine(PassiveIncomeLoop(def));
            }
        }

        private IEnumerator PassiveIncomeLoop(FurnitureItemDef def)
        {
            // İlk gelir için kısa bir açılış beklemesi
            yield return new WaitForSeconds(Random.Range(3f, 7f));

            while (true)
            {
                // Müşteri etkileşim simülasyonu (8 - 15 saniyede bir)
                float waitTime = Random.Range(8f, 15f);
                yield return new WaitForSeconds(waitTime);

                int income = def.passiveIncomePerUse;
                TotalEarnedPassiveIncome += income;

                // Cüzdana ve finans kayıtlarına ekle
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddCredits(income);
                }
                if (FinanceManager.Instance != null)
                {
                    FinanceManager.Instance.RecordIncome("Pasif Gelir", $"Pasif Satış ({def.name})", income);
                }

                // 3D Süzülen Metin Efekti Göster
                SpawnFloatingIncomeText(income, def.iconEmoji);
            }
        }

        private void SpawnFloatingIncomeText(int amount, string emoji)
        {
            GameObject popupObj = new GameObject("Popup_PassiveIncome");
            popupObj.transform.position = transform.position + Vector3.up * 1.6f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 60f);
            popupObj.transform.localScale = Vector3.one * 0.012f;

            if (Camera.main != null)
            {
                popupObj.transform.rotation = Camera.main.transform.rotation;
            }

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = UIStyleUtility.GetGlobalFont(22);
            txt.text = $"+{amount:N0} Cr {emoji}";
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.30f, 0.95f, 0.45f);

            StartCoroutine(AnimateFloatingText(popupObj, txt));
        }

        private IEnumerator AnimateFloatingText(GameObject popup, UnityEngine.UI.Text txt)
        {
            float duration = 1.4f;
            float elapsed = 0f;
            Vector3 startPos = popup.transform.position;
            Color startColor = txt.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (popup != null)
                {
                    popup.transform.position = startPos + Vector3.up * (t * 1.3f);
                    if (Camera.main != null) popup.transform.rotation = Camera.main.transform.rotation;
                }

                if (txt != null)
                {
                    txt.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                }

                yield return null;
            }

            if (popup != null) Destroy(popup);
        }

        private void OnMouseDown()
        {
            if (isLongPressTriggered) return;
            OnClickDetected();
        }

        public void OnClickDetected()
        {
            if (ModalManager.IsModalOpen || EKTPhoneManager.IsTabletOpen || Time.unscaledTime - ModalManager.LastModalCloseTime < 0.05f) return;
            if (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing) return;

            // Eğer bir atölye makinesiyse doğrudan makine üretim arayüzünü / toplama eylemini aç!
            Farm2Shelf.Environment.WorkshopMachineController wsMachine = GetComponent<Farm2Shelf.Environment.WorkshopMachineController>();
            if (wsMachine != null)
            {
                wsMachine.HandleInteraction();
                return;
            }

            // TEK TIK / DOKUNMA -> KESİNLİKLE TAŞIMA MODUNA GİRMEZ, SADECE ARAYÜZÜ AÇAR!
            if (FurnitureInfoModalUI.Instance != null)
            {
                FurnitureInfoModalUI.Instance.ShowModal(this);
            }
        }

        private void ShowPickupHapticFeedback()
        {
            GameObject popupObj = new GameObject("Popup_PickupHaptic");
            popupObj.transform.position = transform.position + Vector3.up * 1.8f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 60f);
            popupObj.transform.localScale = Vector3.one * 0.012f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
            txt.font = UIStyleUtility.GetGlobalFont(22);
            txt.text = "🛠️ Mobilya Taşınıyor";
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.95f, 0.50f, 0.15f);

            Destroy(popupObj, 1.2f);
        }

        public void UpdateAll3DProductMeshes()
        {
            if (rows == null || rows.Length == 0) return;
            for (int i = 0; i < rows.Length; i++)
            {
                UpdateRow3DProductMeshes(i + 1);
            }
        }

        public void PickUpFurniture()
        {
            if (FurniturePlacementManager.Instance != null && !FurniturePlacementManager.Instance.IsPlacing)
            {
                FurnitureType type = this.FurnitureType;
                Vector3 origPos = this.OriginalPosition;
                Quaternion origRot = this.OriginalRotation;

                ShelfRowData[] currentRows = null;
                if (this.rows != null)
                {
                    currentRows = new ShelfRowData[this.rows.Length];
                    for (int i = 0; i < this.rows.Length; i++)
                    {
                        if (this.rows[i] != null)
                        {
                            currentRows[i] = new ShelfRowData(
                                this.rows[i].rowId,
                                this.rows[i].productName,
                                this.rows[i].currentStock,
                                this.rows[i].maxCapacity,
                                this.rows[i].unitPrice,
                                this.rows[i].productId
                            );
                        }
                    }
                }

                // Atölye makinesinin aktif üretim durumunu, tarifini ve kalan süresini koru
                WorkshopMachineState machineState = null;
                WorkshopMachineController wsMachine = GetComponent<WorkshopMachineController>();
                if (wsMachine != null)
                {
                    machineState = new WorkshopMachineState(wsMachine);
                }

                // Mevcut kurulu objeyi listeden kaldır ve imha et
                AllPlacedFurniture.Remove(this);
                Destroy(gameObject);

                // Tekrar yerleştirme modunu başlat
                FurniturePlacementManager.Instance.StartReplacement(type, origPos, origRot, currentRows, machineState);
            }
        }

        public void UpdateRow3DProductMeshes(int rowId)
        {
            if (rows == null || rows.Length == 0) return;

            int targetIndex = rowId;
            if (targetIndex >= 1 && targetIndex <= rows.Length)
            {
                targetIndex = targetIndex - 1; // 1-tabanlı indeksi 0-tabanlı indekse çevir
            }
            else if (targetIndex < 0 || targetIndex >= rows.Length)
            {
                return; // Sınır dışı
            }

            ShelfRowData rData = rows[targetIndex];
            int displayRowNumber = targetIndex + 1;
            string containerName = $"Product_Meshes_Row_{displayRowNumber}";

            Transform oldContainer = transform.Find(containerName);
            if (oldContainer != null) Destroy(oldContainer.gameObject);

            // Depo Rafında stok 0 veya altına indiyse satır verisini de tamamen sıfırla (Atanmamış Boş Koli Yeri yap)!
            if (FurnitureType == FurnitureType.StorageShelf && rData != null && (rData.currentStock <= 0 || rData.IsEmpty))
            {
                rData.productName = "";
                rData.productId = "";
                rData.unitPrice = 0f;
                rData.currentStock = 0;
            }

            if (rData == null || rData.IsEmpty || rData.currentStock <= 0 || rData.IsUnassigned) return;

            GameObject rowContainer = new GameObject(containerName);
            rowContainer.transform.SetParent(transform, false);

            GetShelfRowGridPlacement(FurnitureType, targetIndex, rData.currentStock, rData.maxCapacity, out List<Vector3> positions, out Quaternion rotation, out float scale);

            for (int k = 0; k < positions.Count; k++)
            {
                Farm2Shelf.Environment.Procedural3DProductBuilder.CreateProduct3DMesh(
                    rowContainer.transform, 
                    rData.productName, 
                    positions[k], 
                    rotation, 
                    scale, 
                    FurnitureType == FurnitureType.StorageShelf
                );
            }

            EnsureChildClickForwarders();
        }

        private void GetShelfRowGridPlacement(
            FurnitureType type, 
            int rowIndex, 
            int currentStock, 
            int maxCapacity, 
            out List<Vector3> positions, 
            out Quaternion rotation, 
            out float scale)
        {
            positions = new List<Vector3>();
            rotation = Quaternion.identity;
            scale = 1.0f;

            float fillRatio = Mathf.Clamp01((float)currentStock / Mathf.Max(1, maxCapacity));

            switch (type)
            {
                case FurnitureType.Shelf:
                {
                    // Standart Teşhir Rafı (w=1.8m, h=2.0m, d=0.6m) - 4 Kat
                    float[] shelfY = new float[] { 0.33f, 0.93f, 1.53f, 2.05f };
                    float y = (rowIndex < shelfY.Length) ? shelfY[rowIndex] : (0.33f + rowIndex * 0.58f);
                    
                    float[] xCols = new float[] { -0.55f, -0.275f, 0f, 0.275f, 0.55f };
                    float[] zRanks = new float[] { -0.09f, 0.09f };
                    scale = 0.95f;

                    int maxSlots = xCols.Length * zRanks.Length; // 10 nesne
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zRanks.Length;
                        positions.Add(new Vector3(xCols[xIdx], y, zRanks[zIdx]));
                    }
                    break;
                }

                case FurnitureType.ProduceShelf:
                {
                    // Manav Rafı (3 Eğimli Ahşap Kasa, 15° Açılı)
                    float[] yHeights = new float[] { 0.44f, 0.89f, 1.29f, 1.68f };
                    float[] zDepths = new float[] { -0.10f, 0.00f, 0.10f, 0.18f };
                    float y = (rowIndex < yHeights.Length) ? yHeights[rowIndex] : 0.44f;
                    float zCenter = (rowIndex < zDepths.Length) ? zDepths[rowIndex] : 0.0f;
                    
                    rotation = (rowIndex < 3) ? Quaternion.Euler(15f, 0f, 0f) : Quaternion.identity;
                    scale = 0.92f;

                    float[] xCols = new float[] { -0.56f, -0.28f, 0f, 0.28f, 0.56f };
                    float[] zLocal = new float[] { -0.08f, 0.08f };

                    int maxSlots = xCols.Length * zLocal.Length; // 10 nesne
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zLocal.Length;
                        Vector3 offset = new Vector3(xCols[xIdx], 0f, zLocal[zIdx]);
                        Vector3 rotOffset = rotation * offset;
                        positions.Add(new Vector3(rotOffset.x, y + rotOffset.y, zCenter + rotOffset.z));
                    }
                    break;
                }

                case FurnitureType.BakeryCounter:
                {
                    // Fırın & Pasta Tezgahı (Cam Fanus İçi Ahşap Tepsiler)
                    float y = (rowIndex < 2) ? 0.84f : 1.09f;
                    float zCenter = (rowIndex < 2) ? -0.06f : 0.04f;
                    float xOffsetCenter = (rowIndex % 2 == 0) ? -0.38f : 0.38f;
                    scale = 0.88f;

                    float[] xCols = new float[] { -0.22f, 0f, 0.22f };
                    float[] zRanks = new float[] { -0.08f, 0.08f };

                    int maxSlots = xCols.Length * zRanks.Length; // 6 nesne per row
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zRanks.Length;
                        positions.Add(new Vector3(xOffsetCenter + xCols[xIdx], y, zCenter + zRanks[zIdx]));
                    }
                    break;
                }

                case FurnitureType.Fridge:
                {
                    // Ticari Camlı Meşrubat & Sütlük Dolabı (w=1.4m, h=2.2m) - 4 Kat
                    float[] shelfY = new float[] { 0.38f, 0.82f, 1.26f, 1.70f };
                    float y = (rowIndex < shelfY.Length) ? shelfY[rowIndex] : 0.38f;
                    scale = 0.90f;

                    float[] xCols = new float[] { -0.36f, -0.12f, 0.12f, 0.36f };
                    float[] zRanks = new float[] { -0.12f, 0.06f };

                    int maxSlots = xCols.Length * zRanks.Length; // 8 nesne
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zRanks.Length;
                        positions.Add(new Vector3(xCols[xIdx], y, zRanks[zIdx]));
                    }
                    break;
                }

                case FurnitureType.Freezer:
                {
                    // Sandık Dondurucu (İç taban y=0.48f, 4 Bölme)
                    float y = 0.48f;
                    float xCenter = (rowIndex % 2 == 0) ? -0.42f : 0.42f;
                    float zCenter = (rowIndex < 2) ? -0.18f : 0.18f;
                    scale = 0.82f;

                    float[] xCols = new float[] { -0.22f, 0f, 0.22f };
                    float[] zRanks = new float[] { -0.07f, 0.07f };

                    int maxSlots = xCols.Length * zRanks.Length; // 6 nesne per quadrant
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zRanks.Length;
                        positions.Add(new Vector3(xCenter + xCols[xIdx], y, zCenter + zRanks[zIdx]));
                    }
                    break;
                }

                case FurnitureType.ButcherCounter:
                {
                    // Kasap Reyonu Çelik Vitrini (y=0.72f, 4 Paslanmaz Tepsi)
                    float y = 0.72f;
                    float[] trayX = new float[] { -0.68f, -0.23f, 0.23f, 0.68f };
                    float xCenter = (rowIndex < trayX.Length) ? trayX[rowIndex] : 0f;
                    scale = 0.82f;

                    float[] xCols = new float[] { -0.07f, 0.07f };
                    float[] zRanks = new float[] { -0.16f, 0.0f, 0.16f };

                    int maxSlots = xCols.Length * zRanks.Length; // 6 nesne
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zRanks.Length;
                        positions.Add(new Vector3(xCenter + xCols[xIdx], y, -0.08f + zRanks[zIdx]));
                    }
                    break;
                }

                case FurnitureType.CosmeticShelf:
                {
                    // Kozmetik & Bakım Cam Rafı (w=1.6m, h=2.1m) - 4 Kat
                    float[] shelfY = new float[] { 0.44f, 0.89f, 1.34f, 1.79f };
                    float y = (rowIndex < shelfY.Length) ? shelfY[rowIndex] : 0.44f;
                    scale = 0.88f;

                    float[] xCols = new float[] { -0.46f, -0.23f, 0f, 0.23f, 0.46f };
                    float[] zRanks = new float[] { -0.08f, 0.08f };

                    int maxSlots = xCols.Length * zRanks.Length; // 10 nesne
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zRanks.Length;
                        positions.Add(new Vector3(xCols[xIdx], y, zRanks[zIdx]));
                    }
                    break;
                }

                case FurnitureType.ElectronicsShelf:
                {
                    // Elektronik Cam Vitrini (w=1.7m, h=2.1m) - 4 Kat
                    float[] shelfY = new float[] { 0.54f, 1.09f, 1.64f, 1.95f };
                    float y = (rowIndex < shelfY.Length) ? shelfY[rowIndex] : 0.54f;
                    scale = 0.88f;

                    float[] xCols = new float[] { -0.46f, -0.15f, 0.15f, 0.46f };
                    float[] zRanks = new float[] { -0.09f, 0.09f };

                    int maxSlots = xCols.Length * zRanks.Length; // 8 nesne
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zRanks.Length;
                        positions.Add(new Vector3(xCols[xIdx], y, zRanks[zIdx]));
                    }
                    break;
                }

                case FurnitureType.StorageShelf:
                {
                    // Depo Ağır Yük Palet Rafı (10 Bölme, 3 Kat)
                    float y = 0.28f;
                    float xPos = 0f;

                    if (rowIndex < 3)
                    {
                        y = 0.28f;
                        float[] xTier0 = new float[] { -0.60f, 0f, 0.60f };
                        xPos = xTier0[rowIndex % 3];
                    }
                    else if (rowIndex < 7)
                    {
                        y = 1.18f;
                        float[] xTier1 = new float[] { -0.66f, -0.22f, 0.22f, 0.66f };
                        xPos = xTier1[(rowIndex - 3) % 4];
                    }
                    else
                    {
                        y = 2.08f;
                        float[] xTier2 = new float[] { -0.60f, 0f, 0.60f };
                        xPos = xTier2[(rowIndex - 7) % 3];
                    }

                    scale = 1.0f;
                    positions.Add(new Vector3(xPos, y, 0f));
                    if (currentStock > 25)
                    {
                        positions.Add(new Vector3(xPos, y + 0.16f, 0f)); // 2. Kat İstifli Koli
                    }
                    break;
                }

                case FurnitureType.GourmetShelf:
                {
                    // Lüks Gurme Reyonu (w=1.6m, h=2.1m, d=0.6m) - 4 Kat Sıcak Ahşap & LED Işıklı
                    float[] shelfY = new float[] { 0.40f, 0.86f, 1.32f, 1.78f };
                    float y = (rowIndex < shelfY.Length) ? shelfY[rowIndex] : (0.40f + rowIndex * 0.46f);
                    scale = 0.92f;

                    float[] xCols = new float[] { -0.48f, -0.24f, 0f, 0.24f, 0.48f };
                    float[] zRanks = new float[] { -0.09f, 0.09f };

                    int maxSlots = xCols.Length * zRanks.Length; // 10 nesne
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zRanks.Length;
                        positions.Add(new Vector3(xCols[xIdx], y, zRanks[zIdx]));
                    }
                    break;
                }

                default:
                {
                    float y = 0.35f + rowIndex * 0.45f;
                    float[] xCols = new float[] { -0.40f, 0f, 0.40f };
                    float[] zRanks = new float[] { -0.08f, 0.08f };

                    int maxSlots = xCols.Length * zRanks.Length;
                    int visibleCount = (currentStock >= maxCapacity) ? maxSlots : Mathf.Clamp(Mathf.CeilToInt(fillRatio * maxSlots), currentStock > 0 ? 1 : 0, maxSlots);

                    for (int k = 0; k < visibleCount; k++)
                    {
                        int xIdx = k % xCols.Length;
                        int zIdx = (k / xCols.Length) % zRanks.Length;
                        positions.Add(new Vector3(xCols[xIdx], y, zRanks[zIdx]));
                    }
                    break;
                }
            }
        }

        public Vector3 GetFrontInteractionPosition(float offset = 0.75f)
        {
            return transform.position - transform.forward * offset;
        }

        public Quaternion GetFrontFacingRotation()
        {
            return Quaternion.LookRotation(transform.forward);
        }

        public void UpdateSeasonWarningBadge()
        {
            if (FurnitureType != FurnitureType.ProduceShelf) return;

            TimeManager.Season currentSeason = (TimeManager.Instance != null) ? TimeManager.Instance.CurrentSeason : TimeManager.Season.İlkbahar;

            bool hasOutdatedSeasonProduct = false;
            if (rows != null)
            {
                foreach (var r in rows)
                {
                    if (r == null || r.IsUnassigned) continue;
                    GardenSeedDef seedDef = GardenSeedDatabase.GetSeedById(r.productId);
                    if (seedDef != null && seedDef.season != currentSeason)
                    {
                        hasOutdatedSeasonProduct = true;
                        break;
                    }
                }
            }

            if (hasOutdatedSeasonProduct)
            {
                if (seasonWarningCanvasObj == null)
                {
                    seasonWarningCanvasObj = new GameObject("Season_Warning_Badge_3D");
                    seasonWarningCanvasObj.transform.SetParent(transform, false);
                    seasonWarningCanvasObj.transform.localPosition = new Vector3(0f, 2.15f, 0f);

                    Canvas canvas = seasonWarningCanvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.WorldSpace;
                    seasonWarningCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

                    RectTransform rt = seasonWarningCanvasObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(360f, 80f);
                    seasonWarningCanvasObj.transform.localScale = Vector3.one * 0.008f;

                    if (Camera.main != null) seasonWarningCanvasObj.transform.rotation = Camera.main.transform.rotation;

                    GameObject bgObj = new GameObject("Badge_Bg");
                    bgObj.transform.SetParent(seasonWarningCanvasObj.transform, false);
                    RectTransform bgRect = bgObj.AddComponent<RectTransform>();
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.sizeDelta = Vector2.zero;

                    UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
                    bgImg.color = new Color(0.85f, 0.20f, 0.10f, 0.95f);

                    GameObject textObj = new GameObject("Text");
                    textObj.transform.SetParent(bgObj.transform, false);
                    RectTransform tRect = textObj.AddComponent<RectTransform>();
                    tRect.anchorMin = Vector2.zero;
                    tRect.anchorMax = Vector2.one;
                    tRect.sizeDelta = Vector2.zero;

                    UnityEngine.UI.Text txt = textObj.AddComponent<UnityEngine.UI.Text>();
                    txt.font = UIStyleUtility.GetGlobalFont(20);
                    txt.text = "⚠️ MEVSİM DEĞİŞTİ!\n(Yeni Ürünleri Düzenleyin)";
                    txt.fontSize = 20;
                    txt.fontStyle = FontStyle.Bold;
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.color = Color.yellow;
                }
            }
            else
            {
                if (seasonWarningCanvasObj != null)
                {
                    Destroy(seasonWarningCanvasObj);
                    seasonWarningCanvasObj = null;
                }
            }
        }
    }
}
