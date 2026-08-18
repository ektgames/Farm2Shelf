using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Core;
using Farm2Shelf.UI;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Oyundaki Erkek Hırsız (Shoplifter) Yapay Zekasını ve Güvenlik Görevlisi Kovalamaca Sistemini Yönetir.
    /// Hırsız gün içinde nadiren dükkana gelir, raflara yanaşıp ürün çalar ve kaçar.
    /// Güvenlik vardiyadaysa peşine düşer, %90 ihtimalle yakalar ve çalınan ürünlerin parası ANINDA hesaba yatar!
    /// </summary>
    public class ShoplifterManager : MonoBehaviour
    {
        public static ShoplifterManager Instance { get; private set; }

        public enum ShoplifterState
        {
            EnteringStore,
            StealingFromShelf,
            FleeingStore,
            CapturedBySecurity,
            Escaped
        }

        public class ShoplifterData
        {
            public GameObject thiefObj;
            public ShoplifterState state;
            public PlacedFurnitureController targetShelf;
            public int targetRowId;
            public string stolenProductName = "Ürün";
            public int stolenProductValue = 200;
            public GameObject carriedStolenBox;

            public List<Transform> leftLimbs = new List<Transform>();
            public List<Transform> rightLimbs = new List<Transform>();
            public float walkCycleTimer;

            public float taskTimer;
            public bool isCaught;
            public bool isEscaped;
            public List<Vector3> waypoints;
            public int currentWaypointIndex;
        }

        private readonly List<ShoplifterData> activeShoplifters = new List<ShoplifterData>();
        private float nextSpawnTimer = 0f;
        private int currentDayTracked = -1;
        private int dailySpawnCount = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject mgr = new GameObject("ShoplifterManager_AutoInit");
                mgr.AddComponent<ShoplifterManager>();
            }
        }

        private void Start()
        {
            // Dükkan açılır açılmaz hırsız gelmesin!
            nextSpawnTimer = Time.time + Random.Range(120f, 240f);
        }

        private void ScheduleNextSpawn()
        {
            nextSpawnTimer = Time.time + Random.Range(180f, 300f);
        }

        private void Update()
        {
            if (StoreStatusManager.Instance == null || !StoreStatusManager.Instance.IsOpen) return;

            // Gün Takibi ve Günlük 2 Hırsız Limiti Sıfırlama:
            if (TimeManager.Instance != null)
            {
                int gameDay = TimeManager.Instance.Day;
                if (gameDay != currentDayTracked)
                {
                    currentDayTracked = gameDay;
                    dailySpawnCount = 0;
                }
            }

            // GÜNDE EN FAZLA KESİNLİKLE 2 KERE HIRSIZ GELEBİLİR!
            if (dailySpawnCount >= 2) return;

            // Zamanı gelince ve aktif hırsız yoksa yenisini başlat
            if (Time.time >= nextSpawnTimer && activeShoplifters.Count == 0)
            {
                ScheduleNextSpawn();

                int currentHour = (TimeManager.Instance != null) ? TimeManager.Instance.Hour : 17;

                // 1. Hırsızlık Gelişi: İkindi / Akşam Üstü (15:00 - 18:00)
                // 2. Hırsızlık Gelişi: Geç Gece Saatleri (18:00 - 22:00)
                if (dailySpawnCount == 0 && currentHour >= 15)
                {
                    TrySpawnShoplifter();
                    dailySpawnCount++;
                }
                else if (dailySpawnCount == 1 && currentHour >= 18)
                {
                    TrySpawnShoplifter();
                    dailySpawnCount++;
                }
            }

            float dt = Time.deltaTime;
            for (int i = activeShoplifters.Count - 1; i >= 0; i--)
            {
                var sData = activeShoplifters[i];
                if (sData == null || sData.thiefObj == null)
                {
                    activeShoplifters.RemoveAt(i);
                    continue;
                }

                UpdateShoplifter(sData, dt);
            }
        }

        public bool HasActiveFleeingThief(out ShoplifterData thief)
        {
            thief = null;
            foreach (var s in activeShoplifters)
            {
                if (s != null && s.state == ShoplifterState.FleeingStore && !s.isCaught && !s.isEscaped)
                {
                    thief = s;
                    return true;
                }
            }
            return false;
        }

        private void TrySpawnShoplifter()
        {
            PlacedFurnitureController[] shelves = Object.FindObjectsByType<PlacedFurnitureController>(FindObjectsSortMode.None);
            PlacedFurnitureController targetShelf = null;
            int targetRow = -1;

            foreach (var s in shelves)
            {
                if (s == null || s.rows == null) continue;
                if (s.FurnitureType == FurnitureType.Shelf || s.FurnitureType == FurnitureType.ProduceShelf || s.FurnitureType == FurnitureType.Fridge || s.FurnitureType == FurnitureType.BakeryCounter || s.FurnitureType == FurnitureType.CosmeticShelf || s.FurnitureType == FurnitureType.ButcherCounter)
                {
                    for (int r = 0; r < s.rows.Length; r++)
                    {
                        if (s.rows[r] != null && !s.rows[r].IsUnassigned && s.rows[r].currentStock > 0)
                        {
                            targetShelf = s;
                            targetRow = r;
                            break;
                        }
                    }
                }
                if (targetShelf != null) break;
            }

            if (targetShelf == null) return; // Çalınacak dolu raf yoksa gelme

            Vector3 spawnPos = new Vector3(-22.0f, 0.05f, -4.5f);
            GameObject thiefObj = CreateMaleShoplifter3DModel(spawnPos);

            ShoplifterData data = new ShoplifterData
            {
                thiefObj = thiefObj,
                state = ShoplifterState.EnteringStore,
                targetShelf = targetShelf,
                targetRowId = targetRow,
                isCaught = false,
                isEscaped = false
            };

            // Rota: Spawn ➔ Fuaye ➔ Rafa yaklaşma
            Vector3 shelfFront = targetShelf.GetFrontInteractionPosition(1.2f);
            data.waypoints = new List<Vector3>
            {
                spawnPos,
                new Vector3(-5.0f, 0.05f, -1.0f),
                shelfFront
            };
            data.currentWaypointIndex = 1;

            ExtractLimbs(thiefObj, data);
            activeShoplifters.Add(data);
        }

        private void UpdateShoplifter(ShoplifterData data, float dt)
        {
            switch (data.state)
            {
                case ShoplifterState.EnteringStore:
                    MoveShoplifter(data, dt, 2.6f, () => {
                        data.state = ShoplifterState.StealingFromShelf;
                        data.taskTimer = 1.2f;
                    });
                    break;

                case ShoplifterState.StealingFromShelf:
                    data.taskTimer -= dt;
                    float armSwing = Mathf.Sin(Time.time * 12.0f) * 30.0f;
                    AnimateLimbs(data, armSwing);

                    if (data.taskTimer <= 0f)
                    {
                        ExecuteTheft(data);
                        data.state = ShoplifterState.FleeingStore;

                        // Kaçış rotası: Mevcut konum ➔ Fuaye ➔ Dış Kaldırım
                        data.waypoints = new List<Vector3>
                        {
                            data.thiefObj.transform.position,
                            new Vector3(-5.0f, 0.05f, -1.0f),
                            new Vector3(-22.0f, 0.05f, -4.5f)
                        };
                        data.currentWaypointIndex = 1;
                    }
                    break;

                case ShoplifterState.FleeingStore:
                    MoveShoplifter(data, dt, 3.8f, () => {
                        // Güvenliğe yakalanmadan haritadan çıktı!
                        if (!data.isCaught)
                        {
                            data.isEscaped = true;
                            ShowFloatingNotice(data.thiefObj.transform.position, "⚠️ Hırsız Ürünle Kaçtı!", new Color(0.95f, 0.20f, 0.20f));
                            Destroy(data.thiefObj);
                            data.state = ShoplifterState.Escaped;
                        }
                    });
                    break;
            }
        }

        private void ExecuteTheft(ShoplifterData data)
        {
            if (data.targetShelf != null && data.targetRowId >= 0 && data.targetRowId < data.targetShelf.rows.Length)
            {
                var rData = data.targetShelf.rows[data.targetRowId];
                if (rData != null && rData.currentStock > 0)
                {
                    rData.currentStock = Mathf.Max(0, rData.currentStock - 5); // 5 ürün çal
                    data.targetShelf.UpdateRow3DProductMeshes(data.targetRowId + 1);

                    data.stolenProductName = rData.productName;
                    int unitPrice = (rData.unitPrice > 0) ? Mathf.RoundToInt(rData.unitPrice) : 25;
                    data.stolenProductValue = Mathf.Max(150, unitPrice * 5);

                    CreateStolenBoxInHands(data);
                    ShowFloatingNotice(data.targetShelf.transform.position, $"🚨 Hırsızlık! (-{data.stolenProductName})", new Color(0.95f, 0.25f, 0.15f));
                }
            }
        }

        public void CatchShoplifterBySecurity(ShoplifterData data, Vector3 guardPos)
        {
            if (data == null || data.isCaught || data.isEscaped) return;
            data.isCaught = true;

            // %90 YAKALAMA İHTİMALİ
            float roll = Random.value;
            if (roll <= 0.90f)
            {
                // YAKALANDI! Çalınan ürünler satılmış sayılır ve ANINDA kasamıza yatar!
                int reward = data.stolenProductValue;

                if (EconomyManager.Instance != null) EconomyManager.Instance.AddCredits(reward);
                if (FinanceManager.Instance != null) FinanceManager.Instance.RecordIncome("Satış", $"Hırsızdan Kurtarılan Ürün ({data.stolenProductName})", reward);

                ShowFloatingNotice(guardPos, $"👮 Hırsız Yakalandı! +{reward:N0} Cr Kasaya Yattı 💰", new Color(0.30f, 0.95f, 0.45f));

                if (data.carriedStolenBox != null) Destroy(data.carriedStolenBox);
                if (data.thiefObj != null) Destroy(data.thiefObj);
                data.state = ShoplifterState.CapturedBySecurity;
            }
            else
            {
                // %10 Şansla hırsız son anda kurtuldu
                ShowFloatingNotice(guardPos, "💨 Hırsız Son Anda Sıyrıldı!", new Color(0.95f, 0.60f, 0.15f));
            }
        }

        private void MoveShoplifter(ShoplifterData data, float dt, float speed, System.Action onReachEnd)
        {
            if (data.waypoints == null || data.currentWaypointIndex >= data.waypoints.Count)
            {
                onReachEnd?.Invoke();
                return;
            }

            Vector3 currentPos = data.thiefObj.transform.position;
            Vector3 target = data.waypoints[data.currentWaypointIndex];
            Vector3 dir = target - currentPos;

            if (dir.magnitude < 0.6f)
            {
                data.currentWaypointIndex++;
                if (data.currentWaypointIndex >= data.waypoints.Count)
                {
                    onReachEnd?.Invoke();
                    return;
                }
                target = data.waypoints[data.currentWaypointIndex];
                dir = target - currentPos;
            }

            Vector3 moveDir = dir.normalized;
            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                data.thiefObj.transform.rotation = Quaternion.RotateTowards(data.thiefObj.transform.rotation, targetRot, 360f * dt);
            }

            data.thiefObj.transform.position = Vector3.MoveTowards(currentPos, target, speed * dt);

            data.walkCycleTimer += dt * (speed * 2.5f);
            float legAngle = Mathf.Sin(data.walkCycleTimer) * 25.0f;
            AnimateLimbs(data, legAngle);
        }

        private void CreateStolenBoxInHands(ShoplifterData data)
        {
            if (data.carriedStolenBox != null) Destroy(data.carriedStolenBox);

            GameObject boxRoot = new GameObject("StolenBox_Root");
            boxRoot.transform.SetParent(data.thiefObj.transform, false);
            boxRoot.transform.localPosition = new Vector3(0f, 0.85f, 0.40f);

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "StolenBox";
            box.transform.SetParent(boxRoot.transform, false);
            box.transform.localScale = new Vector3(0.40f, 0.35f, 0.40f);

            Material mat = FurnitureModelBuilder.CardboardBoxMaterial;
            if (mat != null) box.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(box.GetComponent<Collider>());

            data.carriedStolenBox = boxRoot;
        }

        private GameObject CreateMaleShoplifter3DModel(Vector3 pos)
        {
            GameObject root = new GameObject("Shoplifter_Male");
            root.transform.position = pos;

            Material hoodieMat = CreateMat("Shoplifter_Hoodie", new Color(0.12f, 0.12f, 0.15f));
            Material skinMat = CreateMat("Shoplifter_Skin", new Color(0.85f, 0.65f, 0.52f));
            Material pantsMat = CreateMat("Shoplifter_Pants", new Color(0.08f, 0.08f, 0.10f));
            Material maskMat = CreateMat("Shoplifter_Mask", new Color(0.05f, 0.05f, 0.05f));
            Material beanieMat = CreateMat("Shoplifter_Beanie", new Color(0.75f, 0.15f, 0.15f));

            // Gövde (Geniş Erkek Siyah Hoodie)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            torso.transform.localScale = new Vector3(0.50f, 0.65f, 0.30f);
            torso.GetComponent<Renderer>().sharedMaterial = hoodieMat;
            Destroy(torso.GetComponent<Collider>());

            // Kafa & Hırsız Maskesi
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            head.transform.localScale = new Vector3(0.32f, 0.34f, 0.32f);
            head.GetComponent<Renderer>().sharedMaterial = skinMat;
            Destroy(head.GetComponent<Collider>());

            // Siyah Yüz Maskesi
            GameObject mask = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mask.name = "Mask";
            mask.transform.SetParent(head.transform, false);
            mask.transform.localPosition = new Vector3(0f, -0.10f, 0.35f);
            mask.transform.localScale = new Vector3(0.85f, 0.50f, 0.40f);
            mask.GetComponent<Renderer>().sharedMaterial = maskMat;
            Destroy(mask.GetComponent<Collider>());

            // Kırmızı Bere (Beanie)
            GameObject beanie = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beanie.name = "BeanieCap";
            beanie.transform.SetParent(head.transform, false);
            beanie.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            beanie.transform.localScale = new Vector3(0.95f, 0.25f, 0.95f);
            beanie.GetComponent<Renderer>().sharedMaterial = beanieMat;
            Destroy(beanie.GetComponent<Collider>());

            // Sol / Sağ Kollar & Bacaklar
            CreateLimb(root, "Arm_L", new Vector3(-0.32f, 1.05f, 0f), new Vector3(0.12f, 0.55f, 0.12f), hoodieMat);
            CreateLimb(root, "Arm_R", new Vector3( 0.32f, 1.05f, 0f), new Vector3(0.12f, 0.55f, 0.12f), hoodieMat);
            CreateLimb(root, "Leg_L", new Vector3(-0.14f, 0.42f, 0f), new Vector3(0.15f, 0.65f, 0.15f), pantsMat);
            CreateLimb(root, "Leg_R", new Vector3( 0.14f, 0.42f, 0f), new Vector3(0.15f, 0.65f, 0.15f), pantsMat);

            // Baş Üstü Hırsız Uyarısı Tag'i
            CreateOverheadTag(root, "🥷 Hırsız!");

            return root;
        }

        private void CreateLimb(GameObject parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            limb.name = name;
            limb.transform.SetParent(parent.transform, false);
            limb.transform.localPosition = localPos;
            limb.transform.localScale = localScale;
            limb.GetComponent<Renderer>().sharedMaterial = mat;
            Destroy(limb.GetComponent<Collider>());
        }

        private void ExtractLimbs(GameObject root, ShoplifterData data)
        {
            foreach (Transform child in root.transform)
            {
                if (child.name.Contains("_L")) data.leftLimbs.Add(child);
                else if (child.name.Contains("_R")) data.rightLimbs.Add(child);
            }
        }

        private void AnimateLimbs(ShoplifterData data, float angle)
        {
            foreach (var l in data.leftLimbs)
            {
                if (l != null) l.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }
            foreach (var r in data.rightLimbs)
            {
                if (r != null) r.localRotation = Quaternion.Euler(-angle, 0f, 0f);
            }
        }

        private void CreateOverheadTag(GameObject parent, string tagText)
        {
            GameObject tagObj = new GameObject("Overhead_Tag");
            tagObj.transform.SetParent(parent.transform, false);
            tagObj.transform.localPosition = new Vector3(0f, 2.0f, 0f);

            Canvas canvas = tagObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 70;

            RectTransform rt = tagObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240f, 50f);
            tagObj.transform.localScale = Vector3.one * 0.012f;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(tagObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = textObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
            txt.text = tagText;
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.95f, 0.20f, 0.20f);
        }

        private void ShowFloatingNotice(Vector3 pos, string msg, Color color)
        {
            GameObject popupObj = new GameObject("Popup_Notice");
            popupObj.transform.position = pos + Vector3.up * 2.2f;

            Canvas canvas = popupObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 80;

            RectTransform rt = popupObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(350f, 65f);
            popupObj.transform.localScale = Vector3.one * 0.014f;

            if (Camera.main != null) popupObj.transform.rotation = Camera.main.transform.rotation;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform, false);

            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            Text txt = textObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
            txt.text = msg;
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;

            Destroy(popupObj, 2.5f);
        }

        private Material CreateMat(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.name = name;
            mat.color = color;
            return mat;
        }
    }
}
