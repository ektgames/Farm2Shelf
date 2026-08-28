using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Farm2Shelf Güney Kasaba Bölgesi Mimari İnşaatçısı (Procedural Township District Builder).
    /// Kullanıcı mimari planına göre:
    /// - Batı Bloğu: 4 Adet 2-3 Katlı Müstakil Ev (Bahçeli, çitli, yola bağlı)
    /// - Orta Blok: Görkemli Belediye Binası (Town Hall), Saat Kulesi, Bayrak ve Mermer Belediye Meydanı (Havuzlu)
    /// - Doğu Bloğu: 4 Adet 2-3 Katlı Müstakil Ev (Bahçeli, çitli, yola bağlı)
    /// </summary>
    public static class ProceduralTownshipDistrictBuilder
    {
        private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        private static Material GetMaterial(string name, Color color, float metallic = 0.15f, float smoothness = 0.40f)
        {
            if (matCache.TryGetValue(name, out Material mat) && mat != null)
            {
                return mat;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Material newMat = new Material(shader)
            {
                name = name,
                color = color
            };

            if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", color);
            if (newMat.HasProperty("_Color")) newMat.SetColor("_Color", color);
            if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", metallic);
            if (newMat.HasProperty("_Glossiness")) newMat.SetFloat("_Glossiness", smoothness);
            if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", smoothness);

            matCache[name] = newMat;
            return newMat;
        }

        public static void BuildTownshipDistrict(Transform parent)
        {
            Transform districtGroup = new GameObject("Township_Residential_And_Plaza_District").transform;
            districtGroup.SetParent(parent, false);

            // 1. BATI BLOĞU: 4 ADET MÜSTAKİL EV (2 Sütun x 2 Sıra | X: -70m ile -18m)
            float[] westColX = new float[] { -58.0f, -32.0f };
            float rowNorthZ = -25.5f;
            float rowSouthZ = -41.5f;
            Vector2 parcelSize = new Vector2(24.0f, 14.5f);

            // Batı Grubu Kat & Renk Matrisi
            int[,] westFloors = new int[,] { { 3, 2 }, { 2, 3 } }; // [col, row]
            int[,] westColors = new int[,] { { 0, 1 }, { 2, 3 } };

            int parcelCounter = 1;
            for (int col = 0; col < 2; col++)
            {
                float px = westColX[col];

                // Kuzey Sıra (Kuzeydeki otoyol kaldırımına bakar)
                BuildResidentialHouseParcel(
                    districtGroup,
                    new Vector3(px, 0f, rowNorthZ),
                    parcelSize,
                    westFloors[col, 0],
                    westColors[col, 0],
                    parcelCounter++,
                    true // Kuzeye bakar
                );

                // Güney Sıra (Güneydeki kasaba yolu kaldırımına bakar)
                BuildResidentialHouseParcel(
                    districtGroup,
                    new Vector3(px, 0f, rowSouthZ),
                    parcelSize,
                    westFloors[col, 1],
                    westColors[col, 1],
                    parcelCounter++,
                    false // Güneye bakar
                );
            }

            // 2. MERKEZ BLOĞU: BELEDİYE BİNASI VE BELEDİYE MEYDANI (X: -17m ile +17m)
            BuildTownHallAndSquare(districtGroup, new Vector3(0f, 0f, -33.5f), new Vector2(34.0f, 31.0f));

            // 3. DOĞU BLOĞU: 4 ADET MÜSTAKİL EV (2 Sütun x 2 Sıra | X: +18m ile +70m)
            float[] eastColX = new float[] { 32.0f, 58.0f };
            int[,] eastFloors = new int[,] { { 2, 3 }, { 3, 2 } };
            int[,] eastColors = new int[,] { { 4, 5 }, { 6, 7 } };

            for (int col = 0; col < 2; col++)
            {
                float px = eastColX[col];

                // Kuzey Sıra (Kuzeydeki otoyol kaldırımına bakar)
                BuildResidentialHouseParcel(
                    districtGroup,
                    new Vector3(px, 0f, rowNorthZ),
                    parcelSize,
                    eastFloors[col, 0],
                    eastColors[col, 0],
                    parcelCounter++,
                    true // Kuzeye bakar
                );

                // Güney Sıra (Güneydeki kasaba yolu kaldırımına bakar)
                BuildResidentialHouseParcel(
                    districtGroup,
                    new Vector3(px, 0f, rowSouthZ),
                    parcelSize,
                    eastFloors[col, 1],
                    eastColors[col, 1],
                    parcelCounter++,
                    false // Güneye bakar
                );
            }
        }

        #region Residential House Parcel Construction

        private static void BuildResidentialHouseParcel(
            Transform parent,
            Vector3 parcelCenter,
            Vector2 parcelSize,
            int floorCount,
            int colorIndex,
            int houseIndex,
            bool faceNorth)
        {
            GameObject parcelObj = new GameObject($"Town_House_Parcel_{houseIndex}");
            parcelObj.transform.SetParent(parent, false);
            parcelObj.transform.position = parcelCenter;

            // Renk Paletleri
            Color[] houseWallColors = new Color[]
            {
                new Color(0.94f, 0.94f, 0.96f), // 0: Nordik Beyaz
                new Color(0.86f, 0.80f, 0.70f), // 1: Sıcak Ahşap Bej
                new Color(0.68f, 0.78f, 0.70f), // 2: Adaçayı Yeşili
                new Color(0.82f, 0.46f, 0.36f), // 3: Terracotta Kiremit
                new Color(0.62f, 0.74f, 0.84f), // 4: Kırsal Gökyüzü Mavi
                new Color(0.88f, 0.74f, 0.40f), // 5: Sıcak Hardal
                new Color(0.35f, 0.38f, 0.42f), // 6: Modern Antrasit
                new Color(0.82f, 0.78f, 0.74f)  // 7: Doğal Taş Beji
            };

            Color[] roofColors = new Color[]
            {
                new Color(0.72f, 0.22f, 0.18f), // Kırmızı Kiremit
                new Color(0.24f, 0.26f, 0.30f), // Antrasit Arduvaz
                new Color(0.42f, 0.28f, 0.18f), // Çikolata Kahve
                new Color(0.22f, 0.35f, 0.50f)  // Gece Mavisi
            };

            Material wallMat = GetMaterial($"HouseWallMat_{colorIndex}", houseWallColors[colorIndex % houseWallColors.Length]);
            Material roofMat = GetMaterial($"HouseRoofMat_{colorIndex}", roofColors[colorIndex % roofColors.Length]);
            Material plinthMat = GetMaterial("HousePlinthMat", new Color(0.78f, 0.78f, 0.80f), 0.3f, 0.5f);
            Material frameMat = GetMaterial("HouseFrameMat", new Color(0.22f, 0.24f, 0.26f), 0.5f, 0.6f);
            Material doorMat = GetMaterial("HouseDoorWoodMat", new Color(0.48f, 0.30f, 0.16f), 0.1f, 0.3f);
            Material glassMat = GetMaterial("HouseGlassMat", new Color(0.40f, 0.75f, 0.92f, 0.85f), 0.8f, 0.9f);
            Material walkwayMat = GetMaterial("HouseWalkwayStoneMat", new Color(0.75f, 0.73f, 0.70f), 0.1f, 0.2f);
            Material fenceMat = GetMaterial("HouseFenceMat", new Color(0.92f, 0.92f, 0.94f), 0.1f, 0.3f);
            Material chimneyMat = GetMaterial("HouseChimneyBrickMat", new Color(0.68f, 0.28f, 0.22f), 0.1f, 0.2f);

            // 1. Çim Parsel Tabanı
            GameObject lawn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lawn.name = "Parcel_Lawn";
            lawn.transform.SetParent(parcelObj.transform, false);
            lawn.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            lawn.transform.localScale = new Vector3(parcelSize.x - 0.3f, 0.08f, parcelSize.y - 0.3f);
            lawn.GetComponent<Renderer>().sharedMaterial = GetMaterial("HouseLawnGrassMat", new Color(0.32f, 0.58f, 0.26f), 0.0f, 0.1f);
            Object.Destroy(lawn.GetComponent<Collider>());

            // 2. Ev Gövdesi (2 veya 3 Katlı)
            float houseW = 11.0f;
            float houseD = 7.5f;
            float floorH = 2.8f;
            float houseH = floorCount * floorH;

            // Evin parsel içindeki konumu (Yolun ters tarafına doğru yaslanır, önü bahçe kalır)
            float houseOffsetZ = faceNorth ? -2.2f : 2.2f;
            Vector3 housePos = new Vector3(0f, 0f, houseOffsetZ);

            GameObject houseRoot = new GameObject("House_Building_Structure");
            houseRoot.transform.SetParent(parcelObj.transform, false);
            houseRoot.transform.localPosition = housePos;

            // 2.1 Temel Su Basman Taşı
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "House_Plinth";
            plinth.transform.SetParent(houseRoot.transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            plinth.transform.localScale = new Vector3(houseW + 0.3f, 0.40f, houseD + 0.3f);
            plinth.GetComponent<Renderer>().sharedMaterial = plinthMat;
            Object.Destroy(plinth.GetComponent<Collider>());

            // 2.2 Ana Duvar Gövdesi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "House_Wall_Body";
            body.transform.SetParent(houseRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.40f + (houseH / 2f), 0f);
            body.transform.localScale = new Vector3(houseW, houseH, houseD);
            body.GetComponent<Renderer>().sharedMaterial = wallMat;
            Object.Destroy(body.GetComponent<Collider>());

            // 2.3 Kat Ayırıcı Silmeler
            for (int f = 1; f < floorCount; f++)
            {
                float slabY = 0.40f + (f * floorH);
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = $"House_Floor_Trim_{f}";
                slab.transform.SetParent(houseRoot.transform, false);
                slab.transform.localPosition = new Vector3(0f, slabY, 0f);
                slab.transform.localScale = new Vector3(houseW + 0.22f, 0.18f, houseD + 0.22f);
                slab.GetComponent<Renderer>().sharedMaterial = frameMat;
                Object.Destroy(slab.GetComponent<Collider>());
            }

            // 2.4 Kırma / Beşik Eğimli Çatı
            float roofBaseY = 0.40f + houseH;
            float roofH = 2.4f;

            // Çatı Saçağı
            GameObject eaves = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eaves.name = "Roof_Eaves";
            eaves.transform.SetParent(houseRoot.transform, false);
            eaves.transform.localPosition = new Vector3(0f, roofBaseY + 0.10f, 0f);
            eaves.transform.localScale = new Vector3(houseW + 0.6f, 0.20f, houseD + 0.6f);
            eaves.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(eaves.GetComponent<Collider>());

            // Ana Eğimli Çatı Kapağı
            GameObject roofCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofCap.name = "Pitched_Roof_Cap";
            roofCap.transform.SetParent(houseRoot.transform, false);
            roofCap.transform.localPosition = new Vector3(0f, roofBaseY + 0.20f + (roofH / 2f), 0f);
            roofCap.transform.localScale = new Vector3(houseW + 0.35f, roofH, houseD + 0.35f);
            roofCap.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.Destroy(roofCap.GetComponent<Collider>());

            // Çatı Bacası
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimney.name = "Roof_Chimney";
            chimney.transform.SetParent(houseRoot.transform, false);
            chimney.transform.localPosition = new Vector3(houseW * 0.28f, roofBaseY + roofH + 0.6f, houseD * 0.15f);
            chimney.transform.localScale = new Vector3(0.85f, 1.4f, 0.85f);
            chimney.GetComponent<Renderer>().sharedMaterial = chimneyMat;
            Object.Destroy(chimney.GetComponent<Collider>());

            GameObject chimneyCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chimneyCap.name = "Chimney_Cap";
            chimneyCap.transform.SetParent(houseRoot.transform, false);
            chimneyCap.transform.localPosition = new Vector3(houseW * 0.28f, roofBaseY + roofH + 1.35f, houseD * 0.15f);
            chimneyCap.transform.localScale = new Vector3(1.05f, 0.12f, 1.05f);
            chimneyCap.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(chimneyCap.GetComponent<Collider>());

            // 2.5 Pencereler ve Akşam Işıklandırması
            BuildHouseWindows(houseRoot.transform, houseW, houseD, floorCount, floorH, faceNorth, glassMat, frameMat);

            // 2.6 Ana Giriş Kapısı ve Sundurması (Porch)
            BuildHouseEntrance(houseRoot.transform, houseW, houseD, faceNorth, doorMat, plinthMat, frameMat, roofMat);

            // 3. Bahçe Yolu (Giriş kapısından yola uzanan taş yürüyüş yolu)
            BuildHouseWalkway(parcelObj.transform, housePos, houseD, parcelSize.y, faceNorth, walkwayMat);

            // 4. Bahçe Çitleri (Yol çıkışında kapı boşluğu bırakır)
            BuildHouseFences(parcelObj.transform, parcelSize, faceNorth, fenceMat, plinthMat);

            // 5. Bahçe Peyzajı (Ağaç, çiçek tarhı, bahçe lambası, bank)
            BuildHouseGardenLandscape(parcelObj.transform, housePos, houseW, houseD, parcelSize, faceNorth, houseIndex, frameMat);

            // 6. Fiziksel Çarpışma ve NavMesh
            BoxCollider col = houseRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, (houseH + roofH) / 2f, 0f);
            col.size = new Vector3(houseW + 0.5f, houseH + roofH, houseD + 0.5f);

            NavMeshObstacle obstacle = houseRoot.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = col.center;
            obstacle.size = col.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
        }

        private static void BuildHouseWindows(
            Transform parent,
            float width,
            float depth,
            int floorCount,
            float floorH,
            bool faceNorth,
            Material glassMat,
            Material frameMat)
        {
            float halfW = width / 2f;
            float halfD = depth / 2f;

            float frontZ = faceNorth ? halfD : -halfD;
            float rearZ = faceNorth ? -halfD : halfD;
            Vector3 frontDir = faceNorth ? Vector3.forward : Vector3.back;
            Vector3 rearDir = faceNorth ? Vector3.back : Vector3.forward;

            for (int f = 0; f < floorCount; f++)
            {
                float winY = 0.40f + (f * floorH) + 1.45f;

                // Akşam olunca dairenin ışıkları yansın mı? (%60 rastgele olasılık)
                bool isFloorLit = (Random.value < 0.60f);

                // Daire içi oda ışığı (Point Light)
                if (isFloorLit)
                {
                    GameObject lightObj = new GameObject($"House_Interior_Light_F{f + 1}");
                    lightObj.transform.SetParent(parent, false);
                    lightObj.transform.localPosition = new Vector3(0f, winY, 0f);
                    Light pLight = lightObj.AddComponent<Light>();
                    pLight.type = LightType.Point;
                    pLight.color = new Color(1.0f, 0.88f, 0.55f);
                    pLight.intensity = 1.8f;
                    pLight.range = 8.5f;
                    pLight.shadows = LightShadows.None;
                    pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
                    if (DayNightCycleManager.Instance != null)
                    {
                        DayNightCycleManager.Instance.RegisterStoreInteriorLight(pLight);
                    }
                }

                // Ön Cephe Pencereleri (Giriş kapısının sol ve sağında)
                CreateHouseWindowUnit(parent, new Vector3(-3.2f, winY, frontZ), new Vector2(1.6f, 1.4f), frontDir, isFloorLit, glassMat, frameMat);
                CreateHouseWindowUnit(parent, new Vector3(3.2f, winY, frontZ), new Vector2(1.6f, 1.4f), frontDir, isFloorLit, glassMat, frameMat);

                // Üst Katlarda Orta Pencere
                if (f >= 1)
                {
                    CreateHouseWindowUnit(parent, new Vector3(0f, winY, frontZ), new Vector2(1.4f, 1.4f), frontDir, isFloorLit, glassMat, frameMat);
                }

                // Arka Cephe Pencereleri (3 Adet)
                CreateHouseWindowUnit(parent, new Vector3(-3.2f, winY, rearZ), new Vector2(1.6f, 1.4f), rearDir, isFloorLit, glassMat, frameMat);
                CreateHouseWindowUnit(parent, new Vector3(0f, winY, rearZ), new Vector2(1.4f, 1.4f), rearDir, isFloorLit, glassMat, frameMat);
                CreateHouseWindowUnit(parent, new Vector3(3.2f, winY, rearZ), new Vector2(1.6f, 1.4f), rearDir, isFloorLit, glassMat, frameMat);

                // Yan Cephe Pencereleri
                CreateHouseWindowUnit(parent, new Vector3(-halfW, winY, 0f), new Vector2(1.5f, 1.4f), Vector3.left, isFloorLit, glassMat, frameMat);
                CreateHouseWindowUnit(parent, new Vector3(halfW, winY, 0f), new Vector2(1.5f, 1.4f), Vector3.right, isFloorLit, glassMat, frameMat);
            }
        }

        private static void CreateHouseWindowUnit(
            Transform parent,
            Vector3 pos,
            Vector2 size,
            Vector3 outwardDir,
            bool isLitTonight,
            Material glassMat,
            Material frameMat)
        {
            // 1. Pencere Camı (Dış cepheye hafif çıkıntılı, engelsiz)
            GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
            win.name = isLitTonight ? "Apartment_Window_Glass_Lit" : "Apartment_Window_Glass_Dark";
            win.transform.SetParent(parent, false);
            win.transform.localPosition = pos + (outwardDir * 0.05f);

            bool isXAxis = Mathf.Abs(outwardDir.x) > 0.5f;
            win.transform.localScale = isXAxis ? new Vector3(0.08f, size.y, size.x) : new Vector3(size.x, size.y, 0.08f);

            bool isNight = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (isLitTonight && isNight && DayNightCycleManager.WindowGlowOnMaterial != null)
            {
                win.GetComponent<Renderer>().sharedMaterial = DayNightCycleManager.WindowGlowOnMaterial;
            }
            else
            {
                win.GetComponent<Renderer>().sharedMaterial = glassMat;
            }
            Object.Destroy(win.GetComponent<Collider>());

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterApartmentWindow(win, isLitTonight);
            }

            // 2. Pencere Alt Denizliği
            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Window_Sill";
            sill.transform.SetParent(win.transform, false);
            sill.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            sill.transform.localScale = isXAxis ? new Vector3(1.4f, 0.12f, 1.15f) : new Vector3(1.15f, 0.12f, 1.4f);
            sill.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(sill.GetComponent<Collider>());

            // 3. Pencere Çıtası
            GameObject muntin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muntin.name = "Window_Muntin";
            muntin.transform.SetParent(win.transform, false);
            muntin.transform.localPosition = Vector3.zero;
            muntin.transform.localScale = isXAxis ? new Vector3(1.2f, 1.0f, 0.06f) : new Vector3(0.06f, 1.0f, 1.2f);
            muntin.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(muntin.GetComponent<Collider>());
        }

        private static void BuildHouseEntrance(
            Transform parent,
            float width,
            float depth,
            bool faceNorth,
            Material doorMat,
            Material stepMat,
            Material postMat,
            Material porchRoofMat)
        {
            float halfD = depth / 2f;
            float doorZ = faceNorth ? halfD + 0.05f : -halfD - 0.05f;
            float stepDir = faceNorth ? 1f : -1f;

            // Giriş Basamakları
            for (int i = 0; i < 2; i++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Entrance_Step_{i + 1}";
                step.transform.SetParent(parent, false);
                step.transform.localPosition = new Vector3(0f, 0.10f + (i * 0.15f), doorZ + (stepDir * (0.45f - i * 0.20f)));
                step.transform.localScale = new Vector3(2.4f - (i * 0.3f), 0.15f, 0.40f);
                step.GetComponent<Renderer>().sharedMaterial = stepMat;
                Object.Destroy(step.GetComponent<Collider>());
            }

            // Ahşap Kapı
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Main_Door_Leaf";
            door.transform.SetParent(parent, false);
            door.transform.localPosition = new Vector3(0f, 1.40f, doorZ);
            door.transform.localScale = new Vector3(1.3f, 2.1f, 0.10f);
            door.GetComponent<Renderer>().sharedMaterial = doorMat;
            Object.Destroy(door.GetComponent<Collider>());

            // Kapı Sundurması (Porch Canopy)
            GameObject porchRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            porchRoof.name = "Porch_Roof";
            porchRoof.transform.SetParent(parent, false);
            porchRoof.transform.localPosition = new Vector3(0f, 2.70f, doorZ + (stepDir * 0.65f));
            porchRoof.transform.localScale = new Vector3(2.6f, 0.18f, 1.4f);
            porchRoof.GetComponent<Renderer>().sharedMaterial = porchRoofMat;
            Object.Destroy(porchRoof.GetComponent<Collider>());

            // Sundurma Ahşap Direkleri
            for (int dirX = -1; dirX <= 1; dirX += 2)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Porch_Post";
                post.transform.SetParent(parent, false);
                post.transform.localPosition = new Vector3(dirX * 1.15f, 1.35f, doorZ + (stepDir * 1.15f));
                post.transform.localScale = new Vector3(0.12f, 1.35f, 0.12f);
                post.GetComponent<Renderer>().sharedMaterial = postMat;
                Object.Destroy(post.GetComponent<Collider>());
            }
        }

        private static void BuildHouseWalkway(
            Transform parent,
            Vector3 housePos,
            float houseD,
            float parcelD,
            bool faceNorth,
            Material stoneMat)
        {
            float startZ = faceNorth ? housePos.z + (houseD / 2f) + 1.2f : housePos.z - (houseD / 2f) - 1.2f;
            float endZ = faceNorth ? (parcelD / 2f) : -(parcelD / 2f);
            float length = Mathf.Abs(endZ - startZ);
            float centerZ = (startZ + endZ) / 2f;

            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = "House_Garden_Walkway";
            path.transform.SetParent(parent, false);
            path.transform.localPosition = new Vector3(0f, 0.02f, centerZ);
            path.transform.localScale = new Vector3(2.0f, 0.04f, length);
            path.GetComponent<Renderer>().sharedMaterial = stoneMat;
            Object.Destroy(path.GetComponent<Collider>());
        }

        private static void BuildHouseFences(
            Transform parent,
            Vector2 parcelSize,
            bool faceNorth,
            Material fenceMat,
            Material pillarMat)
        {
            float halfW = parcelSize.x / 2f;
            float halfD = parcelSize.y / 2f;
            float gateW = 2.4f;

            // 1. Yan Çitler (Doğu & Batı Çitleri)
            for (int dirX = -1; dirX <= 1; dirX += 2)
            {
                GameObject fenceSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fenceSide.name = $"Fence_Side_{dirX}";
                fenceSide.transform.SetParent(parent, false);
                fenceSide.transform.localPosition = new Vector3(dirX * (halfW - 0.1f), 0.45f, 0f);
                fenceSide.transform.localScale = new Vector3(0.10f, 0.85f, parcelSize.y - 0.2f);
                fenceSide.GetComponent<Renderer>().sharedMaterial = fenceMat;
                Object.Destroy(fenceSide.GetComponent<Collider>());
            }

            // 2. Arka Çit (Yolun ters tarafındaki tam boy çit)
            float backZ = faceNorth ? -halfD + 0.1f : halfD - 0.1f;
            GameObject fenceBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fenceBack.name = "Fence_Back";
            fenceBack.transform.SetParent(parent, false);
            fenceBack.transform.localPosition = new Vector3(0f, 0.45f, backZ);
            fenceBack.transform.localScale = new Vector3(parcelSize.x - 0.2f, 0.85f, 0.10f);
            fenceBack.GetComponent<Renderer>().sharedMaterial = fenceMat;
            Object.Destroy(fenceBack.GetComponent<Collider>());

            // 3. Ön Çit (Yol tarafı - Ortada 2.4m kapı boşluğu bırakır)
            float frontZ = faceNorth ? halfD - 0.1f : -halfD + 0.1f;
            float fenceSegmentW = (parcelSize.x - gateW) / 2f;

            for (int dirX = -1; dirX <= 1; dirX += 2)
            {
                float segCenterX = dirX * (gateW / 2f + fenceSegmentW / 2f);
                GameObject fenceFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fenceFront.name = $"Fence_Front_{dirX}";
                fenceFront.transform.SetParent(parent, false);
                fenceFront.transform.localPosition = new Vector3(segCenterX, 0.45f, frontZ);
                fenceFront.transform.localScale = new Vector3(fenceSegmentW, 0.85f, 0.10f);
                fenceFront.GetComponent<Renderer>().sharedMaterial = fenceMat;
                Object.Destroy(fenceFront.GetComponent<Collider>());

                // Kapı Yanı Taş Sütunları
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"Gate_Pillar_{dirX}";
                pillar.transform.SetParent(parent, false);
                pillar.transform.localPosition = new Vector3(dirX * (gateW / 2f), 0.55f, frontZ);
                pillar.transform.localScale = new Vector3(0.35f, 1.1f, 0.35f);
                pillar.GetComponent<Renderer>().sharedMaterial = pillarMat;
                Object.Destroy(pillar.GetComponent<Collider>());
            }
        }

        private static void BuildHouseGardenLandscape(
            Transform parent,
            Vector3 housePos,
            float houseW,
            float houseD,
            Vector2 parcelSize,
            bool faceNorth,
            int houseIdx,
            Material lampMat)
        {
            float halfW = parcelSize.x / 2f;
            float frontZ = faceNorth ? 3.5f : -3.5f;

            // 1. Bahçe Ağacı (Sol Ön Bahçe)
            GameObject tree = new GameObject("Garden_Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = new Vector3(-halfW + 4.0f, 0f, frontZ);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
            trunk.GetComponent<Renderer>().sharedMaterial = GetMaterial("HouseTreeTrunkMat", new Color(0.42f, 0.28f, 0.16f), 0.1f, 0.2f);
            Object.Destroy(trunk.GetComponent<Collider>());

            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Foliage";
            foliage.transform.SetParent(tree.transform, false);
            foliage.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            foliage.transform.localScale = new Vector3(2.4f, 2.2f, 2.4f);
            foliage.GetComponent<Renderer>().sharedMaterial = GetMaterial("HouseTreeFoliageMat", new Color(0.24f, 0.52f, 0.22f), 0.0f, 0.1f);
            Object.Destroy(foliage.GetComponent<Collider>());

            // 2. Çiçek Tarhı (Sağ Ön Bahçe)
            Color flowerColor = (houseIdx % 2 == 0) ? new Color(0.95f, 0.30f, 0.45f) : new Color(0.95f, 0.85f, 0.20f);
            GameObject flowerBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flowerBed.name = "Flower_Bed";
            flowerBed.transform.SetParent(parent, false);
            flowerBed.transform.localPosition = new Vector3(halfW - 4.5f, 0.12f, frontZ);
            flowerBed.transform.localScale = new Vector3(4.5f, 0.24f, 2.2f);
            flowerBed.GetComponent<Renderer>().sharedMaterial = GetMaterial($"HouseFlowerMat_{houseIdx}", flowerColor, 0.0f, 0.3f);
            Object.Destroy(flowerBed.GetComponent<Collider>());

            // 3. Bahçe Lambası (Yol kenarı)
            BuildGardenLamp(parent, new Vector3(2.2f, 0f, frontZ), lampMat);

            // 4. Bahçe Bankı
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Garden_Bench";
            bench.transform.SetParent(parent, false);
            bench.transform.localPosition = new Vector3(-halfW + 7.5f, 0.28f, frontZ);
            bench.transform.localScale = new Vector3(1.8f, 0.45f, 0.7f);
            bench.GetComponent<Renderer>().sharedMaterial = GetMaterial("HouseBenchMat", new Color(0.50f, 0.32f, 0.18f), 0.1f, 0.3f);
            Object.Destroy(bench.GetComponent<Collider>());
        }

        private static void BuildGardenLamp(Transform parent, Vector3 pos, Material poleMat)
        {
            GameObject lamp = new GameObject("Garden_Post_Lamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = pos;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(lamp.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.2f, 0.08f);
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;
            Object.Destroy(pole.GetComponent<Collider>());

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(lamp.transform, false);
            bulb.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            bulb.transform.localScale = new Vector3(0.30f, 0.30f, 0.30f);
            bulb.GetComponent<Renderer>().sharedMaterial = GetMaterial("GardenLampBulbMat", new Color(0.35f, 0.35f, 0.38f), 0.1f, 0.5f);
            Object.Destroy(bulb.GetComponent<Collider>());

            GameObject lightObj = new GameObject("Lamp_Light");
            lightObj.transform.SetParent(lamp.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.88f, 0.55f);
            pLight.intensity = 2.0f;
            pLight.range = 10.0f;
            pLight.shadows = LightShadows.None;
            pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStreetLamp(bulb, pLight);
            }
        }

        #endregion

        #region Town Hall & Town Square Construction

        private static void BuildTownHallAndSquare(Transform parent, Vector3 centerPos, Vector2 squareSize)
        {
            GameObject squareGroup = new GameObject("Town_Hall_And_Square_Complex");
            squareGroup.transform.SetParent(parent, false);
            squareGroup.transform.position = centerPos;

            Material plazaMarbleMat = GetMaterial("TownHallPlazaMarbleMat", new Color(0.88f, 0.88f, 0.86f), 0.1f, 0.3f);
            Material wallMat = GetMaterial("TownHallWallMarbleMat", new Color(0.94f, 0.92f, 0.88f), 0.15f, 0.45f);
            Material pillarMat = GetMaterial("TownHallPillarMat", new Color(0.96f, 0.96f, 0.95f), 0.1f, 0.5f);
            Material roofMat = GetMaterial("TownHallRoofCopperMat", new Color(0.24f, 0.42f, 0.50f), 0.3f, 0.6f);
            Material goldMat = GetMaterial("TownHallGoldTrimMat", new Color(0.95f, 0.80f, 0.25f), 0.8f, 0.8f);
            Material darkTrimMat = GetMaterial("TownHallDarkTrimMat", new Color(0.22f, 0.24f, 0.28f), 0.4f, 0.6f);

            // 1. Mermer Meydan Zemin Kaplaması
            GameObject plazaFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plazaFloor.name = "Town_Square_Marble_Plaza";
            plazaFloor.transform.SetParent(squareGroup.transform, false);
            plazaFloor.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            plazaFloor.transform.localScale = new Vector3(squareSize.x - 0.4f, 0.04f, squareSize.y - 0.4f);
            plazaFloor.GetComponent<Renderer>().sharedMaterial = plazaMarbleMat;
            Object.Destroy(plazaFloor.GetComponent<Collider>());

            // 2. Belediye Binası (Town Hall Building - Z = -7.5f arkada konumlanır)
            float hallW = 22.0f;
            float hallD = 10.5f;
            float hallH = 8.5f;
            Vector3 hallLocalPos = new Vector3(0f, 0f, -7.5f);

            GameObject hallRoot = new GameObject("Town_Hall_Building");
            hallRoot.transform.SetParent(squareGroup.transform, false);
            hallRoot.transform.localPosition = hallLocalPos;

            // 2.1 Anıtsal Temel & Giriş Merdivenleri (Grand Staircase)
            GameObject basePlinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePlinth.name = "TownHall_Base_Plinth";
            basePlinth.transform.SetParent(hallRoot.transform, false);
            basePlinth.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            basePlinth.transform.localScale = new Vector3(hallW + 0.6f, 0.60f, hallD + 0.6f);
            basePlinth.GetComponent<Renderer>().sharedMaterial = pillarMat;
            Object.Destroy(basePlinth.GetComponent<Collider>());

            // 5 Kademeli Ön Mermer Merdiven
            for (int s = 0; s < 5; s++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Grand_Step_{s + 1}";
                step.transform.SetParent(hallRoot.transform, false);
                step.transform.localPosition = new Vector3(0f, 0.06f + (s * 0.12f), (hallD / 2f) + 1.6f - (s * 0.30f));
                step.transform.localScale = new Vector3(10.0f - (s * 0.4f), 0.12f, 0.40f);
                step.GetComponent<Renderer>().sharedMaterial = pillarMat;
                Object.Destroy(step.GetComponent<Collider>());
            }

            // 2.2 Ana Duvar Gövdesi
            GameObject hallBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hallBody.name = "TownHall_Wall_Body";
            hallBody.transform.SetParent(hallRoot.transform, false);
            hallBody.transform.localPosition = new Vector3(0f, 0.60f + (hallH / 2f), 0f);
            hallBody.transform.localScale = new Vector3(hallW, hallH, hallD);
            hallBody.GetComponent<Renderer>().sharedMaterial = wallMat;
            Object.Destroy(hallBody.GetComponent<Collider>());

            // 2.3 Kat Silmesi
            GameObject midTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midTrim.name = "TownHall_Mid_Trim";
            midTrim.transform.SetParent(hallRoot.transform, false);
            midTrim.transform.localPosition = new Vector3(0f, 0.60f + (hallH * 0.55f), 0f);
            midTrim.transform.localScale = new Vector3(hallW + 0.35f, 0.25f, hallD + 0.35f);
            midTrim.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(midTrim.GetComponent<Collider>());

            // 2.4 Anıtsal Sütunlu Giriş Revakı (Colonnade Portico)
            float porticoH = 6.8f;
            float porticoZ = (hallD / 2f) + 1.2f;

            // 4 Adet Klasik Sütun
            float[] pillarX = new float[] { -3.8f, -1.3f, 1.3f, 3.8f };
            foreach (float px in pillarX)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "Colonnade_Pillar";
                pillar.transform.SetParent(hallRoot.transform, false);
                pillar.transform.localPosition = new Vector3(px, 0.60f + (porticoH / 2f), porticoZ);
                pillar.transform.localScale = new Vector3(0.42f, porticoH / 2f, 0.42f);
                pillar.GetComponent<Renderer>().sharedMaterial = pillarMat;
                Object.Destroy(pillar.GetComponent<Collider>());
            }

            // Sütun Üstü Kemer & Alınlık (Pediment)
            GameObject pediment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pediment.name = "Portico_Pediment";
            pediment.transform.SetParent(hallRoot.transform, false);
            pediment.transform.localPosition = new Vector3(0f, 0.60f + porticoH + 0.65f, porticoZ);
            pediment.transform.localScale = new Vector3(9.2f, 1.3f, 2.8f);
            pediment.GetComponent<Renderer>().sharedMaterial = pillarMat;
            Object.Destroy(pediment.GetComponent<Collider>());

            // 2.5 Çatı ve Saat Kulesi (Clock Tower)
            float roofBaseY = 0.60f + hallH;

            // Ana Çatı
            GameObject mainRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainRoof.name = "TownHall_Main_Roof";
            mainRoof.transform.SetParent(hallRoot.transform, false);
            mainRoof.transform.localPosition = new Vector3(0f, roofBaseY + 1.2f, 0f);
            mainRoof.transform.localScale = new Vector3(hallW + 0.8f, 2.4f, hallD + 0.8f);
            mainRoof.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.Destroy(mainRoof.GetComponent<Collider>());

            // Saat Kulesi Gövdesi
            GameObject clockTower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clockTower.name = "Clock_Tower_Body";
            clockTower.transform.SetParent(hallRoot.transform, false);
            clockTower.transform.localPosition = new Vector3(0f, roofBaseY + 4.2f, 0.8f);
            clockTower.transform.localScale = new Vector3(4.2f, 4.8f, 4.2f);
            clockTower.GetComponent<Renderer>().sharedMaterial = wallMat;
            Object.Destroy(clockTower.GetComponent<Collider>());

            // 4 Cephe Saat Kadranı (Clock Dials)
            Vector3[] clockDialOffsets = new Vector3[]
            {
                new Vector3(0f, 0f, 2.12f),
                new Vector3(0f, 0f, -2.12f),
                new Vector3(2.12f, 0f, 0f),
                new Vector3(-2.12f, 0f, 0f)
            };
            foreach (Vector3 cOffset in clockDialOffsets)
            {
                GameObject dial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                dial.name = "Clock_Face";
                dial.transform.SetParent(clockTower.transform, false);
                dial.transform.localPosition = cOffset;
                dial.transform.localScale = new Vector3(0.55f, 0.05f, 0.55f);
                dial.transform.localRotation = Quaternion.Euler(cOffset.z != 0 ? 90f : 0f, 0f, cOffset.x != 0 ? 90f : 0f);
                dial.GetComponent<Renderer>().sharedMaterial = goldMat;
                Object.Destroy(dial.GetComponent<Collider>());
            }

            // Kule Külahı & Spire (Sivri Çatı)
            GameObject towerSpire = GameObject.CreatePrimitive(PrimitiveType.Cube);
            towerSpire.name = "Clock_Tower_Spire";
            towerSpire.transform.SetParent(hallRoot.transform, false);
            towerSpire.transform.localPosition = new Vector3(0f, roofBaseY + 7.8f, 0.8f);
            towerSpire.transform.localScale = new Vector3(3.2f, 2.8f, 3.2f);
            towerSpire.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.Destroy(towerSpire.GetComponent<Collider>());

            // Bayrak Direği ve Türk Bayrağı
            GameObject flagPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagPole.name = "TownHall_Flag_Pole";
            flagPole.transform.SetParent(hallRoot.transform, false);
            flagPole.transform.localPosition = new Vector3(0f, roofBaseY + 10.5f, 0.8f);
            flagPole.transform.localScale = new Vector3(0.08f, 2.6f, 0.08f);
            flagPole.GetComponent<Renderer>().sharedMaterial = goldMat;
            Object.Destroy(flagPole.GetComponent<Collider>());

            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "National_Flag";
            flag.transform.SetParent(hallRoot.transform, false);
            flag.transform.localPosition = new Vector3(0.9f, roofBaseY + 11.2f, 0.8f);
            flag.transform.localScale = new Vector3(1.6f, 1.0f, 0.04f);
            flag.GetComponent<Renderer>().sharedMaterial = GetMaterial("NationalFlagRedMat", new Color(0.88f, 0.12f, 0.12f), 0.0f, 0.3f);
            Object.Destroy(flag.GetComponent<Collider>());

            // 2.6 Belediye Pencereleri & Gece Işıklandırması
            for (int f = 0; f < 2; f++)
            {
                float winY = 0.60f + (f * 3.6f) + 1.8f;
                // Sol & Sağ Kanat Pencereleri
                CreateHouseWindowUnit(hallRoot.transform, new Vector3(-7.2f, winY, hallD / 2f), new Vector2(1.8f, 2.0f), Vector3.forward, true, GetMaterial("TownHallGlassMat", new Color(0.40f, 0.75f, 0.95f, 0.85f)), darkTrimMat);
                CreateHouseWindowUnit(hallRoot.transform, new Vector3(7.2f, winY, hallD / 2f), new Vector2(1.8f, 2.0f), Vector3.forward, true, GetMaterial("TownHallGlassMat", new Color(0.40f, 0.75f, 0.95f, 0.85f)), darkTrimMat);

                CreateHouseWindowUnit(hallRoot.transform, new Vector3(-7.2f, winY, -hallD / 2f), new Vector2(1.8f, 2.0f), Vector3.back, true, GetMaterial("TownHallGlassMat", new Color(0.40f, 0.75f, 0.95f, 0.85f)), darkTrimMat);
                CreateHouseWindowUnit(hallRoot.transform, new Vector3(0f, winY, -hallD / 2f), new Vector2(1.8f, 2.0f), Vector3.back, true, GetMaterial("TownHallGlassMat", new Color(0.40f, 0.75f, 0.95f, 0.85f)), darkTrimMat);
                CreateHouseWindowUnit(hallRoot.transform, new Vector3(7.2f, winY, -hallD / 2f), new Vector2(1.8f, 2.0f), Vector3.back, true, GetMaterial("TownHallGlassMat", new Color(0.40f, 0.75f, 0.95f, 0.85f)), darkTrimMat);
            }

            // Belediye İçi Gece Işığı
            GameObject hallLightObj = new GameObject("TownHall_Grand_Interior_Light");
            hallLightObj.transform.SetParent(hallRoot.transform, false);
            hallLightObj.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            Light hLight = hallLightObj.AddComponent<Light>();
            hLight.type = LightType.Point;
            hLight.color = new Color(1.0f, 0.90f, 0.60f);
            hLight.intensity = 2.4f;
            hLight.range = 14.0f;
            hLight.shadows = LightShadows.None;
            hLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(hLight);
            }

            // 3D Dünya Etiketi
            Create3DLabel("KASABA BELEDİYESİ", "TOWN HALL", hallRoot.transform, new Vector3(0f, 0.60f + porticoH + 1.8f, porticoZ + 1.2f), new Color(1.0f, 0.88f, 0.35f));

            // Fizik & NavMesh
            BoxCollider hCol = hallRoot.AddComponent<BoxCollider>();
            hCol.center = new Vector3(0f, hallH / 2f, 0f);
            hCol.size = new Vector3(hallW + 1.0f, hallH + 4f, hallD + 3.0f);

            NavMeshObstacle hObs = hallRoot.AddComponent<NavMeshObstacle>();
            hObs.shape = NavMeshObstacleShape.Box;
            hObs.center = hCol.center;
            hObs.size = hCol.size;
            hObs.carving = true;
            hObs.carveOnlyStationary = true;

            // 3. MEYDAN SÜS HAVUZU (Meydanın Merkezinde, Z = +5.0m)
            BuildPlazaFountain(squareGroup.transform, new Vector3(0f, 0f, 5.0f), pillarMat);

            // 4. MEYDAN AYDINLATMALARI VE ÇEVRE PEYZAJI
            BuildPlazaDecorations(squareGroup.transform, squareSize, darkTrimMat);
        }

        private static void BuildPlazaFountain(Transform parent, Vector3 localPos, Material marbleMat)
        {
            GameObject fountain = new GameObject("Plaza_Marble_Fountain");
            fountain.transform.SetParent(parent, false);
            fountain.transform.localPosition = localPos;

            Material waterMat = GetMaterial("FountainWaterMat", new Color(0.20f, 0.55f, 0.85f, 0.75f), 0.9f, 0.95f);

            // Havuz Mermer Çanağı
            GameObject basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basin.name = "Fountain_Basin";
            basin.transform.SetParent(fountain.transform, false);
            basin.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            basin.transform.localScale = new Vector3(6.5f, 0.30f, 6.5f);
            basin.GetComponent<Renderer>().sharedMaterial = marbleMat;
            Object.Destroy(basin.GetComponent<Collider>());

            // Havuz Suyu
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Water_Surface";
            water.transform.SetParent(fountain.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            water.transform.localScale = new Vector3(5.8f, 0.04f, 5.8f);
            water.GetComponent<Renderer>().sharedMaterial = waterMat;
            Object.Destroy(water.GetComponent<Collider>());

            // Orta Su Fıskiyesi Kulesi (2 Kademeli)
            GameObject jetCol = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            jetCol.name = "Jet_Column";
            jetCol.transform.SetParent(fountain.transform, false);
            jetCol.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            jetCol.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            jetCol.GetComponent<Renderer>().sharedMaterial = marbleMat;
            Object.Destroy(jetCol.GetComponent<Collider>());

            GameObject upperBowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upperBowl.name = "Upper_Bowl";
            upperBowl.transform.SetParent(fountain.transform, false);
            upperBowl.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            upperBowl.transform.localScale = new Vector3(2.4f, 0.20f, 2.4f);
            upperBowl.GetComponent<Renderer>().sharedMaterial = marbleMat;
            Object.Destroy(upperBowl.GetComponent<Collider>());

            // Gece Havuz Aydınlatması (Turkuaz-Mavi Su Işığı)
            GameObject fLightObj = new GameObject("Fountain_Water_Light");
            fLightObj.transform.SetParent(fountain.transform, false);
            fLightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            Light fLight = fLightObj.AddComponent<Light>();
            fLight.type = LightType.Point;
            fLight.color = new Color(0.30f, 0.80f, 1.0f);
            fLight.intensity = 2.2f;
            fLight.range = 7.5f;
            fLight.shadows = LightShadows.None;
            fLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(fLight);
            }
        }

        private static void BuildPlazaDecorations(Transform parent, Vector2 squareSize, Material darkMat)
        {
            // 1. Havuz Etrafında 4 Oturma Bankı
            Vector3[] benchOffsets = new Vector3[]
            {
                new Vector3(-4.8f, 0.28f, 5.0f),
                new Vector3(4.8f, 0.28f, 5.0f),
                new Vector3(0f, 0.28f, 9.8f),
                new Vector3(0f, 0.28f, 0.2f)
            };
            foreach (Vector3 bPos in benchOffsets)
            {
                GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bench.name = "Plaza_Bench";
                bench.transform.SetParent(parent, false);
                bench.transform.localPosition = bPos;
                bench.transform.localScale = new Vector3(2.2f, 0.45f, 0.7f);
                bench.GetComponent<Renderer>().sharedMaterial = GetMaterial("PlazaBenchWoodMat", new Color(0.48f, 0.30f, 0.16f), 0.1f, 0.3f);
                Object.Destroy(bench.GetComponent<Collider>());
            }

            // 2. Meydan Ağaçları (4 Köşede Çınar Ağaçları)
            Vector3[] treeOffsets = new Vector3[]
            {
                new Vector3(-12.0f, 0f, 8.5f),
                new Vector3(12.0f, 0f, 8.5f),
                new Vector3(-12.0f, 0f, -1.5f),
                new Vector3(12.0f, 0f, -1.5f)
            };
            foreach (Vector3 tPos in treeOffsets)
            {
                GameObject tree = new GameObject("Plaza_Plane_Tree");
                tree.transform.SetParent(parent, false);
                tree.transform.localPosition = tPos;

                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(tree.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                trunk.transform.localScale = new Vector3(0.45f, 1.4f, 0.45f);
                trunk.GetComponent<Renderer>().sharedMaterial = GetMaterial("PlazaTreeTrunkMat", new Color(0.40f, 0.28f, 0.18f), 0.1f, 0.2f);
                Object.Destroy(trunk.GetComponent<Collider>());

                GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foliage.name = "Foliage";
                foliage.transform.SetParent(tree.transform, false);
                foliage.transform.localPosition = new Vector3(0f, 3.2f, 0f);
                foliage.transform.localScale = new Vector3(2.8f, 2.6f, 2.8f);
                foliage.GetComponent<Renderer>().sharedMaterial = GetMaterial("PlazaTreeFoliageMat", new Color(0.22f, 0.54f, 0.24f), 0.0f, 0.1f);
                Object.Destroy(foliage.GetComponent<Collider>());
            }

            // 3. Meydan Aydınlatma Lambaları (4 Adet)
            Vector3[] lampOffsets = new Vector3[]
            {
                new Vector3(-8.5f, 0f, 11.5f),
                new Vector3(8.5f, 0f, 11.5f),
                new Vector3(-8.5f, 0f, -1.5f),
                new Vector3(8.5f, 0f, -1.5f)
            };
            foreach (Vector3 lPos in lampOffsets)
            {
                BuildGardenLamp(parent, lPos, darkMat);
            }
        }

        private static void Create3DLabel(string trText, string enText, Transform parent, Vector3 localPos, Color color)
        {
            GameObject labelObj = new GameObject("3D_World_Label");
            labelObj.transform.SetParent(parent, false);
            labelObj.transform.localPosition = localPos;

            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = LocalizationManager.Instance != null && LocalizationManager.Instance.CurrentLanguage == GameLanguage.English ? enText : trText;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.085f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textMesh.fontStyle = FontStyle.Bold;

            labelObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        }

        #endregion
    }
}
