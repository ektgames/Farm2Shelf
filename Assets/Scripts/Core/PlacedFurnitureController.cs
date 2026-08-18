using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;
using Farm2Shelf.Utils;

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
            if (ModalManager.IsModalOpen)
            {
                if (!ModalManager.IsAnyModalCanvasActive()) ModalManager.SetModalOpen(false);
                else return;
            }
            if (EKTPhoneManager.IsTabletOpen) return;
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

            if (TouchInputHelper.IsCleanTapThisFrame(out Vector2 pointerPos))
            {
                if (ModalManager.IsModalOpen) return;
                if (EKTPhoneManager.IsTabletOpen) return;
                if (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing) return;

                Camera mainCam = Camera.main;
                if (mainCam == null) return;

                Ray ray = mainCam.ScreenPointToRay(pointerPos);

                RaycastHit[] hits = Physics.RaycastAll(ray, 150f);
                if (hits != null && hits.Length > 0)
                {
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (var h in hits)
                    {
                        if (h.collider == null) continue;
                        PlacedFurnitureController ctrl = h.collider.GetComponentInParent<PlacedFurnitureController>();
                        if (ctrl == null) ctrl = h.collider.GetComponent<PlacedFurnitureController>();

                        if (ctrl == this)
                        {
                            if (Time.time - lastGlobalClickTime >= 0.15f)
                            {
                                lastGlobalClickTime = Time.time;
                                OnClickDetected();
                            }
                            break;
                        }
                    }
                }
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
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
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

                boxCol.center = localCenter;
                boxCol.size = new Vector3(
                    Mathf.Max(1.4f, localSize.x + 0.35f),
                    Mathf.Max(1.8f, localSize.y + 0.35f),
                    Mathf.Max(1.4f, localSize.z + 0.35f)
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
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
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

        public void OnClickDetected()
        {
            if (ModalManager.IsModalOpen)
            {
                if (!ModalManager.IsAnyModalCanvasActive()) ModalManager.SetModalOpen(false);
                else return;
            }
            if (EKTPhoneManager.IsTabletOpen) return;
            if (FurniturePlacementManager.Instance != null && FurniturePlacementManager.Instance.IsPlacing) return;

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
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
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

                // Mevcut kurulu objeyi listeden kaldır ve imha et
                AllPlacedFurniture.Remove(this);
                Destroy(gameObject);

                // Tekrar yerleştirme modunu başlat
                FurniturePlacementManager.Instance.StartReplacement(type, origPos, origRot, currentRows);
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

            // Raf Yükseklik Y Yörüngeleri
            float rowY = 0.35f + targetIndex * 0.48f;
            if (FurnitureType == FurnitureType.StorageShelf) rowY = 0.18f + targetIndex * 0.20f;
            else if (FurnitureType == FurnitureType.Fridge || FurnitureType == FurnitureType.Freezer) rowY = 0.25f + targetIndex * 0.42f;

            // Stok Miktarına Göre Görsel Nesne Sayısı (Tam Doluysa 8 Nesne: 2 Sıra x 4 Sütun)
            float fillRatio = Mathf.Clamp01((float)rData.currentStock / rData.maxCapacity);
            int visualCount = Mathf.Clamp(Mathf.RoundToInt(fillRatio * 8f), 1, 8);
            if (rData.currentStock == rData.maxCapacity) visualCount = 8; // Tam doluysa 8 nesne!

            float[] xOffsets = new float[] { -0.52f, -0.18f, 0.18f, 0.52f };
            float[] zOffsets = new float[] { -0.14f, 0.14f };

            for (int k = 0; k < visualCount; k++)
            {
                int xIndex = k % 4;
                int zIndex = (k / 4) % 2;

                Vector3 localPos = new Vector3(xOffsets[xIndex], rowY, zOffsets[zIndex]);
                Farm2Shelf.Environment.Procedural3DProductBuilder.CreateProduct3DMesh(rowContainer.transform, rData.productName, localPos, FurnitureType == FurnitureType.StorageShelf);
            }

            EnsureChildClickForwarders();
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
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
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
