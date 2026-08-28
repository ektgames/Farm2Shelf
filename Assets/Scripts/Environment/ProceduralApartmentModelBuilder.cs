using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Kuzey mahallesi için 3, 4 ve 5 katlı düşük poligonlu (low-poly), gerçekçi mimariye sahip,
    /// balkonlu, giriş saçaklı, pencereli, çatı üniteli, bahçe çitli ve bağlantı yollu apartman binaları üretir.
    /// </summary>
    public static class ProceduralApartmentModelBuilder
    {
        // Renk Paleti (Modern & Gerçekçi Şehir Mimarisi)
        private static readonly Color[] FacadeColors = new Color[]
        {
            new Color(0.86f, 0.54f, 0.42f), // 0: Sıcak Kiremit / Terracotta
            new Color(0.38f, 0.44f, 0.52f), // 1: Şık Antrasit / Arduvaz Gri
            new Color(0.88f, 0.84f, 0.76f), // 2: Modern Kum Beji / Krem
            new Color(0.42f, 0.58f, 0.52f), // 3: Adaçayı Yeşili / Sage
            new Color(0.34f, 0.48f, 0.64f), // 4: Kıyı Mavisi / Modern Navy
            new Color(0.92f, 0.80f, 0.62f), // 5: Sıcak Güneş Sarısı / Hardal Bej
            new Color(0.55f, 0.50f, 0.48f), // 6: Doğal Taş Grisi / Warm Taupe
            new Color(0.78f, 0.42f, 0.46f)  // 7: Pastel Gül Kurusu / Muted Coral
        };

        private static Material basePlinthMat;
        private static Material glassWindowMat;
        private static Material frameDarkMat;
        private static Material roofGravelMat;
        private static Material roofDetailMat;
        private static Material balconyRailMat;
        private static Material fenceWallMat;
        private static Material fenceWoodMat;
        private static Material gardenPathMat;
        private static Material gardenGrassMat;
        private static Material flowerBedMat;
        private static Material trunkMat;
        private static Material foliageMat;
        private static Material doorCanopyMat;

        private static readonly Dictionary<int, Material> FacadeMatCache = new Dictionary<int, Material>();

        private static void EnsureMaterials()
        {
            if (basePlinthMat == null) basePlinthMat = CreateMat("Apt_PlinthMat", new Color(0.22f, 0.24f, 0.28f));
            if (glassWindowMat == null) glassWindowMat = CreateMat("Apt_GlassMat", new Color(0.20f, 0.65f, 0.85f, 0.90f), 0.1f, 0.3f);
            if (frameDarkMat == null) frameDarkMat = CreateMat("Apt_FrameMat", new Color(0.15f, 0.16f, 0.18f));
            if (roofGravelMat == null) roofGravelMat = CreateMat("Apt_RoofGravelMat", new Color(0.30f, 0.32f, 0.35f));
            if (roofDetailMat == null) roofDetailMat = CreateMat("Apt_RoofDetailMat", new Color(0.45f, 0.48f, 0.52f));
            if (balconyRailMat == null) balconyRailMat = CreateMat("Apt_BalconyRailMat", new Color(0.18f, 0.20f, 0.22f));
            if (fenceWallMat == null) fenceWallMat = CreateMat("Apt_FenceWallMat", new Color(0.72f, 0.74f, 0.76f));
            if (fenceWoodMat == null) fenceWoodMat = CreateMat("Apt_FenceWoodMat", new Color(0.55f, 0.36f, 0.20f));
            if (gardenPathMat == null) gardenPathMat = CreateMat("Apt_GardenPathMat", new Color(0.80f, 0.78f, 0.74f));
            if (gardenGrassMat == null) gardenGrassMat = CreateMat("Apt_GardenGrassMat", new Color(0.32f, 0.68f, 0.32f));
            if (flowerBedMat == null) flowerBedMat = CreateMat("Apt_FlowerBedMat", new Color(0.92f, 0.25f, 0.35f));
            if (trunkMat == null) trunkMat = CreateMat("Apt_TrunkMat", new Color(0.40f, 0.26f, 0.15f));
            if (foliageMat == null) foliageMat = CreateMat("Apt_FoliageMat", new Color(0.18f, 0.58f, 0.24f));
            if (doorCanopyMat == null) doorCanopyMat = CreateMat("Apt_DoorCanopyMat", new Color(0.12f, 0.14f, 0.16f));
        }

        private static Material GetFacadeMaterial(int colorIndex)
        {
            int idx = Mathf.Abs(colorIndex) % FacadeColors.Length;
            if (!FacadeMatCache.TryGetValue(idx, out Material mat) || mat == null)
            {
                mat = CreateMat($"Apt_FacadeMat_{idx}", FacadeColors[idx]);
                FacadeMatCache[idx] = mat;
            }
            return mat;
        }

        private static Material CreateMat(string name, Color color, float metallic = 0f, float smoothness = 0.1f)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Diffuse");
            Material m = new Material(s) { name = name };
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        /// <summary>
        /// Bir apartman parseli (bina, bahçe, çitler, giriş yolu, peyzaj) oluşturur.
        /// </summary>
        public static GameObject BuildApartmentParcel(
            Transform parent,
            Vector3 parcelCenter,
            Vector2 parcelSize, // örn: (28m x 24m)
            int floorCount,     // 3, 4 veya 5 kat
            int colorVariant,
            int parcelIndex,
            bool entranceFacesEast // true: Doğuya (+X), false: Batıya (-X) bakar
        )
        {
            EnsureMaterials();

            GameObject parcelObj = new GameObject($"Apartment_Parcel_{parcelIndex + 1}_F{floorCount}");
            parcelObj.transform.SetParent(parent, false);
            parcelObj.transform.position = parcelCenter;

            Material facadeMat = GetFacadeMaterial(colorVariant);

            // 1. Parsel Bahçe Zemini
            GameObject garden = GameObject.CreatePrimitive(PrimitiveType.Cube);
            garden.name = "Parcel_Garden_Lawn";
            garden.transform.SetParent(parcelObj.transform, false);
            garden.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            garden.transform.localScale = new Vector3(parcelSize.x - 0.2f, 0.08f, parcelSize.y - 0.2f);
            garden.GetComponent<Renderer>().sharedMaterial = gardenGrassMat;
            Object.Destroy(garden.GetComponent<Collider>());

            // 2. Bina Boyutları
            float buildingW = Mathf.Clamp(parcelSize.x - 10f, 15f, 18f); // X genişliği (16m)
            float buildingD = Mathf.Clamp(parcelSize.y - 8f, 13f, 16f);  // Z derinliği (14m)
            float floorH = 3.1f;
            float totalBuildingH = floorCount * floorH;

            // Binayı parselin giriş olmayan tarafına doğru hafif yasla ki giriş bahçesi geniş kalsın
            float buildingOffsetX = entranceFacesEast ? -2.2f : 2.2f;
            Vector3 buildingLocalPos = new Vector3(buildingOffsetX, 0f, 0f);

            GameObject buildingRoot = new GameObject("Apartment_Building_Structure");
            buildingRoot.transform.SetParent(parcelObj.transform, false);
            buildingRoot.transform.localPosition = buildingLocalPos;

            // 2.1 Zemin Süpürgeliği / Plinth (0.5m)
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Building_Plinth_Base";
            plinth.transform.SetParent(buildingRoot.transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            plinth.transform.localScale = new Vector3(buildingW + 0.3f, 0.5f, buildingD + 0.3f);
            plinth.GetComponent<Renderer>().sharedMaterial = basePlinthMat;
            Object.Destroy(plinth.GetComponent<Collider>());

            // 2.2 Ana Bina Gövdesi
            GameObject mainBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainBody.name = "Building_Main_Body";
            mainBody.transform.SetParent(buildingRoot.transform, false);
            mainBody.transform.localPosition = new Vector3(0f, totalBuildingH / 2f, 0f);
            mainBody.transform.localScale = new Vector3(buildingW, totalBuildingH, buildingD);
            mainBody.GetComponent<Renderer>().sharedMaterial = facadeMat;
            Object.Destroy(mainBody.GetComponent<Collider>());

            // 2.3 Kat Ayırıcı Beyaz/Antrasit Şeritler (Horizontal Floor Slabs)
            for (int f = 1; f < floorCount; f++)
            {
                float slabY = f * floorH;
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = $"Floor_Divider_Slab_{f}";
                slab.transform.SetParent(buildingRoot.transform, false);
                slab.transform.localPosition = new Vector3(0f, slabY, 0f);
                slab.transform.localScale = new Vector3(buildingW + 0.25f, 0.22f, buildingD + 0.25f);
                slab.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
                Object.Destroy(slab.GetComponent<Collider>());
            }

            // 2.4 Çatı Korkuluğu / Parapet ve Çatı Zemin Kaplaması
            GameObject roofFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofFloor.name = "Roof_Gravel_Deck";
            roofFloor.transform.SetParent(buildingRoot.transform, false);
            roofFloor.transform.localPosition = new Vector3(0f, totalBuildingH + 0.05f, 0f);
            roofFloor.transform.localScale = new Vector3(buildingW - 0.4f, 0.1f, buildingD - 0.4f);
            roofFloor.GetComponent<Renderer>().sharedMaterial = roofGravelMat;
            Object.Destroy(roofFloor.GetComponent<Collider>());

            // Parapet Duvarları (Çatı kenar korkuluk duvarı)
            CreateParapetWalls(buildingRoot.transform, buildingW, buildingD, totalBuildingH, basePlinthMat);

            // Çatı Asansör Kulesi ve Havalandırma / Güneş Panelleri
            CreateRooftopFeatures(buildingRoot.transform, totalBuildingH);

            // 2.5 Kat Pencereleri ve Balkonlar
            BuildWindowsAndBalconies(buildingRoot.transform, buildingW, buildingD, floorCount, floorH, entranceFacesEast);

            // 2.6 Ana Giriş Kapısı & Giriş Saçağı (Canopy)
            BuildMainEntrance(buildingRoot.transform, buildingW, buildingD, entranceFacesEast);

            // 3. Bahçe Yolu (Giriş kapısından parsel çıkışına / cadde kaldırımına uzanır)
            BuildGardenWalkwayAndGate(parcelObj.transform, buildingLocalPos, buildingW, buildingD, parcelSize, entranceFacesEast);

            // 4. Çevre Bahçe Çitleri & Duvarları (Yol çıkışında kapı boşluğu bırakır)
            BuildPerimeterFences(parcelObj.transform, parcelSize, entranceFacesEast);

            // 5. Bahçe Peyzajı (Ağaçlar, çiçek tarhları, modern sokak lambası, bahçe bankı)
            BuildGardenLandscaping(parcelObj.transform, buildingLocalPos, buildingW, buildingD, parcelSize, entranceFacesEast, parcelIndex);

            // 6. Fiziksel Çarpışma ve NavMesh Obstacle (Binanın tamamını kaplayan optimize kutu)
            BoxCollider col = buildingRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, totalBuildingH / 2f, 0f);
            col.size = new Vector3(buildingW + 0.6f, totalBuildingH, buildingD + 0.6f);

            NavMeshObstacle obstacle = buildingRoot.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = col.center;
            obstacle.size = col.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;

            return parcelObj;
        }

        private static void CreateParapetWalls(Transform parent, float width, float depth, float topY, Material mat)
        {
            float pH = 0.70f;
            float pThick = 0.25f;

            // Kuzey ve Güney Parapet
            for (int dir = -1; dir <= 1; dir += 2)
            {
                GameObject pZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pZ.name = "Parapet_Z";
                pZ.transform.SetParent(parent, false);
                pZ.transform.localPosition = new Vector3(0f, topY + (pH / 2f), dir * (depth / 2f - pThick / 2f));
                pZ.transform.localScale = new Vector3(width, pH, pThick);
                pZ.GetComponent<Renderer>().sharedMaterial = mat;
                Object.Destroy(pZ.GetComponent<Collider>());
            }

            // Doğu ve Batı Parapet
            for (int dir = -1; dir <= 1; dir += 2)
            {
                GameObject pX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pX.name = "Parapet_X";
                pX.transform.SetParent(parent, false);
                pX.transform.localPosition = new Vector3(dir * (width / 2f - pThick / 2f), topY + (pH / 2f), 0f);
                pX.transform.localScale = new Vector3(pThick, pH, depth - (pThick * 2f));
                pX.GetComponent<Renderer>().sharedMaterial = mat;
                Object.Destroy(pX.GetComponent<Collider>());
            }
        }

        private static void CreateRooftopFeatures(Transform parent, float topY)
        {
            // Asansör Makine Odası / Penthouse (Çatı Kulesi)
            GameObject elevatorTower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            elevatorTower.name = "Roof_Elevator_Penthouse";
            elevatorTower.transform.SetParent(parent, false);
            elevatorTower.transform.localPosition = new Vector3(1.2f, topY + 1.25f, 0.8f);
            elevatorTower.transform.localScale = new Vector3(4.2f, 2.5f, 3.6f);
            elevatorTower.GetComponent<Renderer>().sharedMaterial = basePlinthMat;
            Object.Destroy(elevatorTower.GetComponent<Collider>());

            // Havalandırma / AC Üniteleri (2 Adet)
            for (int i = 0; i < 2; i++)
            {
                GameObject hvac = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hvac.name = $"Roof_HVAC_Unit_{i + 1}";
                hvac.transform.SetParent(parent, false);
                hvac.transform.localPosition = new Vector3(-3.2f + (i * 2.8f), topY + 0.65f, -2.5f);
                hvac.transform.localScale = new Vector3(1.6f, 1.3f, 1.4f);
                hvac.GetComponent<Renderer>().sharedMaterial = roofDetailMat;
                Object.Destroy(hvac.GetComponent<Collider>());
            }

            // Güneş Panelleri (Eğimli 3'lü Panel Grubu)
            for (int s = 0; s < 3; s++)
            {
                GameObject solar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                solar.name = $"Roof_Solar_Panel_{s + 1}";
                solar.transform.SetParent(parent, false);
                solar.transform.localPosition = new Vector3(-3.8f + (s * 2.4f), topY + 0.45f, 2.8f);
                solar.transform.localScale = new Vector3(1.8f, 0.08f, 1.4f);
                solar.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
                solar.GetComponent<Renderer>().sharedMaterial = glassWindowMat;
                Object.Destroy(solar.GetComponent<Collider>());
            }
        }

        private static void BuildWindowsAndBalconies(Transform parent, float width, float depth, int floors, float floorH, bool entranceEast)
        {
            float halfW = width / 2f;
            float halfD = depth / 2f;

            for (int f = 0; f < floors; f++)
            {
                float floorBaseY = f * floorH;
                float windowCenterY = floorBaseY + 1.65f;

                // Her katta 2 bağımsız daire (Kuzey Daire & Güney Daire)
                // Akşam olunca bazılarının ışıkları yanar (%55 olasılıkla bağımsız)
                bool isFlatNorthLit = (Random.value < 0.55f);
                bool isFlatSouthLit = (Random.value < 0.55f);

                // Daire içi aydınlatma ışıkları (Akşam olunca dairenin içinden dışarı sıcak sarı ışık yayar)
                if (isFlatNorthLit)
                {
                    GameObject lightObj = new GameObject($"Apartment_Flat_Light_F{f + 1}_North");
                    lightObj.transform.SetParent(parent, false);
                    lightObj.transform.localPosition = new Vector3(0f, windowCenterY, halfD * 0.5f);
                    Light pLight = lightObj.AddComponent<Light>();
                    pLight.type = LightType.Point;
                    pLight.color = new Color(1.0f, 0.88f, 0.55f);
                    pLight.intensity = 1.6f;
                    pLight.range = 9.0f;
                    pLight.shadows = LightShadows.None;
                    pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
                    if (DayNightCycleManager.Instance != null)
                    {
                        DayNightCycleManager.Instance.RegisterStoreInteriorLight(pLight);
                    }
                }

                if (isFlatSouthLit)
                {
                    GameObject lightObj = new GameObject($"Apartment_Flat_Light_F{f + 1}_South");
                    lightObj.transform.SetParent(parent, false);
                    lightObj.transform.localPosition = new Vector3(0f, windowCenterY, -halfD * 0.5f);
                    Light pLight = lightObj.AddComponent<Light>();
                    pLight.type = LightType.Point;
                    pLight.color = new Color(1.0f, 0.88f, 0.55f);
                    pLight.intensity = 1.6f;
                    pLight.range = 9.0f;
                    pLight.shadows = LightShadows.None;
                    pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
                    if (DayNightCycleManager.Instance != null)
                    {
                        DayNightCycleManager.Instance.RegisterStoreInteriorLight(pLight);
                    }
                }

                // 1. Yan Cephe Pencereleri (Kuzey & Güney Cepheler)
                for (float x = -halfW + 2.5f; x <= halfW - 2.5f; x += 3.4f)
                {
                    // Kuzey Cephe (Kuzey daireye ait)
                    CreateWindow(parent, new Vector3(x, windowCenterY, halfD + 0.02f), new Vector2(1.5f, 1.5f), Vector3.forward, isFlatNorthLit);
                    // Güney Cephe (Güney daireye ait)
                    CreateWindow(parent, new Vector3(x, windowCenterY, -halfD - 0.02f), new Vector2(1.5f, 1.5f), Vector3.back, isFlatSouthLit);
                }

                // 2. Ana Giriş ve Arka Cephe (Doğu & Batı Cepheler)
                float entranceSideX = entranceEast ? halfW : -halfW;
                float rearSideX = entranceEast ? -halfW : halfW;
                Vector3 entranceNormal = entranceEast ? Vector3.right : Vector3.left;
                Vector3 rearNormal = entranceEast ? Vector3.left : Vector3.right;

                // Üst Katlarda Balkonlar (1. Kattan itibaren)
                if (f >= 1)
                {
                    // Ön Cephe Balkonu - Kuzey Daire (Z = +2.4f)
                    BuildBalcony(parent, new Vector3(entranceSideX, floorBaseY + 0.15f, 2.4f), new Vector2(1.5f, 2.8f), entranceNormal);
                    CreateWindow(parent, new Vector3(entranceSideX + (entranceNormal.x * 0.02f), floorBaseY + 1.25f, 2.4f), new Vector2(1.8f, 2.3f), entranceNormal, isFlatNorthLit);

                    // Ön Cephe Balkonu - Güney Daire (Z = -2.4f)
                    BuildBalcony(parent, new Vector3(entranceSideX, floorBaseY + 0.15f, -2.4f), new Vector2(1.5f, 2.8f), entranceNormal);
                    CreateWindow(parent, new Vector3(entranceSideX + (entranceNormal.x * 0.02f), floorBaseY + 1.25f, -2.4f), new Vector2(1.8f, 2.3f), entranceNormal, isFlatSouthLit);
                }

                // Arka Cephe Pencereleri (Her katta 3 pencere)
                for (float z = -halfD + 3.0f; z <= halfD - 3.0f; z += 3.6f)
                {
                    bool isLit = (z >= 0f) ? isFlatNorthLit : isFlatSouthLit;
                    CreateWindow(parent, new Vector3(rearSideX + (rearNormal.x * 0.02f), windowCenterY, z), new Vector2(1.6f, 1.5f), rearNormal, isLit);
                }
            }
        }

        private static void CreateWindow(Transform parent, Vector3 pos, Vector2 size, Vector3 outwardDir, bool isLitTonight)
        {
            // 1. Pencere Camı (Dış cephede tam görünür, söve/duvar altında kalmaz)
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
                win.GetComponent<Renderer>().sharedMaterial = glassWindowMat;
            }
            Object.Destroy(win.GetComponent<Collider>());

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterApartmentWindow(win, isLitTonight);
            }

            // 2. Pencere Alt Denizliği / Sövesi (Camı kapatmaz, camın hemen altında zarif mimari denizlik)
            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Window_Sill";
            sill.transform.SetParent(win.transform, false);
            sill.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            sill.transform.localScale = isXAxis ? new Vector3(1.4f, 0.12f, 1.15f) : new Vector3(1.15f, 0.12f, 1.4f);
            sill.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
            Object.Destroy(sill.GetComponent<Collider>());

            // 3. Pencere Dikey Kanat Ayracı (İnce mimari çıta)
            GameObject divider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            divider.name = "Window_Muntin";
            divider.transform.SetParent(win.transform, false);
            divider.transform.localPosition = Vector3.zero;
            divider.transform.localScale = isXAxis ? new Vector3(1.2f, 1.0f, 0.06f) : new Vector3(0.06f, 1.0f, 1.2f);
            divider.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
            Object.Destroy(divider.GetComponent<Collider>());
        }

        private static void BuildBalcony(Transform parent, Vector3 basePos, Vector2 balconySize, Vector3 outwardDir)
        {
            // balconySize: (X Derinlik/Çıkıntı, Z Uzunluk)
            float depth = balconySize.x;
            float length = balconySize.y;
            float railH = 0.95f;

            // 1. Balkon Taban Betonu
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Balcony_Floor_Slab";
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = basePos + (outwardDir * (depth / 2f));
            floor.transform.localScale = new Vector3(depth, 0.22f, length);
            floor.GetComponent<Renderer>().sharedMaterial = basePlinthMat;
            Object.Destroy(floor.GetComponent<Collider>());

            // 2. Balkon Korkulukları (Modern Koyu Metal & Cam Panel)
            GameObject railFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railFront.name = "Balcony_Front_Rail";
            railFront.transform.SetParent(parent, false);
            railFront.transform.localPosition = basePos + (outwardDir * depth) + new Vector3(0f, railH / 2f, 0f);
            railFront.transform.localScale = new Vector3(0.08f, railH, length);
            railFront.GetComponent<Renderer>().sharedMaterial = balconyRailMat;
            Object.Destroy(railFront.GetComponent<Collider>());

            // Yan Korkuluklar (Kuzey & Güney)
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject railSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railSide.name = "Balcony_Side_Rail";
                railSide.transform.SetParent(parent, false);
                railSide.transform.localPosition = basePos + (outwardDir * (depth / 2f)) + new Vector3(0f, railH / 2f, side * (length / 2f));
                railSide.transform.localScale = new Vector3(depth, railH, 0.08f);
                railSide.GetComponent<Renderer>().sharedMaterial = balconyRailMat;
                Object.Destroy(railSide.GetComponent<Collider>());
            }
        }

        private static void BuildMainEntrance(Transform parent, float width, float depth, bool entranceEast)
        {
            float halfW = width / 2f;
            float entranceX = entranceEast ? halfW : -halfW;
            Vector3 outDir = entranceEast ? Vector3.right : Vector3.left;

            // 1. Giriş Merdiveni / Podesti
            GameObject steps = GameObject.CreatePrimitive(PrimitiveType.Cube);
            steps.name = "Entrance_Stairs_Platform";
            steps.transform.SetParent(parent, false);
            steps.transform.localPosition = new Vector3(entranceX + (outDir.x * 0.9f), 0.20f, 0f);
            steps.transform.localScale = new Vector3(1.8f, 0.40f, 3.4f);
            steps.GetComponent<Renderer>().sharedMaterial = gardenPathMat;
            Object.Destroy(steps.GetComponent<Collider>());

            // 2. Çift Kanatlı Modern Cam Giriş Kapısı
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Entrance_Glass_Double_Door";
            door.transform.SetParent(parent, false);
            door.transform.localPosition = new Vector3(entranceX + (outDir.x * 0.04f), 1.35f, 0f);
            door.transform.localScale = new Vector3(0.12f, 2.5f, 2.6f);
            door.GetComponent<Renderer>().sharedMaterial = glassWindowMat;
            Object.Destroy(door.GetComponent<Collider>());

            // Kapı Kasası
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Entrance_Door_Frame";
            frame.transform.SetParent(door.transform, false);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(1.2f, 1.08f, 1.12f);
            frame.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
            Object.Destroy(frame.GetComponent<Collider>());

            // 3. Giriş Saçağı / Modern Metal Gölgelik (Canopy)
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Entrance_Modern_Canopy";
            canopy.transform.SetParent(parent, false);
            canopy.transform.localPosition = new Vector3(entranceX + (outDir.x * 1.3f), 2.85f, 0f);
            canopy.transform.localScale = new Vector3(2.6f, 0.16f, 3.8f);
            canopy.GetComponent<Renderer>().sharedMaterial = doorCanopyMat;
            Object.Destroy(canopy.GetComponent<Collider>());

            // Saçak Taşıyıcı Çelik Halat / Destek Çubukları
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rod.name = "Canopy_Support_Rod";
                rod.transform.SetParent(parent, false);
                rod.transform.localPosition = new Vector3(entranceX + (outDir.x * 1.1f), 3.45f, s * 1.6f);
                rod.transform.localScale = new Vector3(0.06f, 0.75f, 0.06f);
                rod.transform.localRotation = Quaternion.Euler(0f, 0f, outDir.x * -35f);
                rod.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
                Object.Destroy(rod.GetComponent<Collider>());
            }
        }

        private static void BuildGardenWalkwayAndGate(Transform parcelParent, Vector3 bldgLocalPos, float bldgW, float bldgD, Vector2 parcelSize, bool entranceEast)
        {
            float halfParcelX = parcelSize.x / 2f;
            float bldgEdgeX = bldgLocalPos.x + (entranceEast ? (bldgW / 2f) : -(bldgW / 2f));
            float targetGateX = entranceEast ? halfParcelX : -halfParcelX;
            float walkLength = Mathf.Abs(targetGateX - bldgEdgeX);
            float walkCenterX = (bldgEdgeX + targetGateX) / 2f;

            // Yürüyüş Parke Yolu (Giriş kapısından caddeye kadar)
            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = "Walkway_Path_To_Street";
            path.transform.SetParent(parcelParent, false);
            path.transform.localPosition = new Vector3(walkCenterX, 0.01f, 0f);
            path.transform.localScale = new Vector3(walkLength, 0.06f, 3.0f);
            path.GetComponent<Renderer>().sharedMaterial = gardenPathMat;
            Object.Destroy(path.GetComponent<Collider>());
        }

        private static void BuildPerimeterFences(Transform parcelParent, Vector2 parcelSize, bool entranceEast)
        {
            float halfX = parcelSize.x / 2f;
            float halfZ = parcelSize.y / 2f;
            float fenceH = 1.15f;
            float gateOpeningWidth = 3.6f;

            Transform fenceGroup = new GameObject("Parcel_Perimeter_Fences").transform;
            fenceGroup.SetParent(parcelParent, false);

            // 1. Kuzey ve Güney Sınır Çitleri (Tam kapalı)
            for (int dir = -1; dir <= 1; dir += 2)
            {
                CreateFenceSegment(fenceGroup, new Vector3(0f, fenceH / 2f, dir * halfZ), new Vector3(parcelSize.x, fenceH, 0.22f));
            }

            // 2. Arka Sınır Çiti (Girişin tersi taraf - Tam kapalı)
            float rearX = entranceEast ? -halfX : halfX;
            CreateFenceSegment(fenceGroup, new Vector3(rearX, fenceH / 2f, 0f), new Vector3(0.22f, fenceH, parcelSize.y));

            // 3. Ön Cadde Cephesi Çiti (Giriş kapısı açıklığı bırakır)
            float frontX = entranceEast ? halfX : -halfX;
            float sideFenceLength = (parcelSize.y - gateOpeningWidth) / 2f;

            // Kapının Solu ve Sağı
            CreateFenceSegment(fenceGroup, new Vector3(frontX, fenceH / 2f, -halfZ + (sideFenceLength / 2f)), new Vector3(0.22f, fenceH, sideFenceLength));
            CreateFenceSegment(fenceGroup, new Vector3(frontX, fenceH / 2f, halfZ - (sideFenceLength / 2f)), new Vector3(0.22f, fenceH, sideFenceLength));

            // Kapı Giriş Sütunları (2 Adet Şık Taş Direk)
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Gate_Stone_Pillar";
                pillar.transform.SetParent(fenceGroup, false);
                pillar.transform.localPosition = new Vector3(frontX, 0.70f, s * (gateOpeningWidth / 2f));
                pillar.transform.localScale = new Vector3(0.50f, 1.40f, 0.50f);
                pillar.GetComponent<Renderer>().sharedMaterial = basePlinthMat;
                Object.Destroy(pillar.GetComponent<Collider>());
            }
        }

        private static void CreateFenceSegment(Transform parent, Vector3 localPos, Vector3 localScale)
        {
            // Alt Beton Kaide (0.4m) + Üst Ahşap/Metal Çit
            GameObject fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fence.name = "Fence_Section";
            fence.transform.SetParent(parent, false);
            fence.transform.localPosition = localPos;
            fence.transform.localScale = localScale;
            fence.GetComponent<Renderer>().sharedMaterial = fenceWallMat;

            // NavMesh ve Çarpışma
            BoxCollider bc = fence.GetComponent<BoxCollider>();
            if (bc != null)
            {
                NavMeshObstacle obs = fence.AddComponent<NavMeshObstacle>();
                obs.shape = NavMeshObstacleShape.Box;
                obs.center = bc.center;
                obs.size = bc.size;
                obs.carving = true;
            }
        }

        private static void BuildGardenLandscaping(Transform parcelParent, Vector3 bldgPos, float bldgW, float bldgD, Vector2 parcelSize, bool entranceEast, int parcelIndex)
        {
            Transform landGroup = new GameObject("Garden_Landscaping").transform;
            landGroup.SetParent(parcelParent, false);

            float halfX = parcelSize.x / 2f;
            float halfZ = parcelSize.y / 2f;
            float frontX = entranceEast ? (halfX - 3.5f) : (-halfX + 3.5f);
            float rearX = entranceEast ? (-halfX + 3.0f) : (halfX - 3.0f);

            // 1. Bahçe Ağaçları (Ön ve Arka bahçe köşeleri)
            CreateGardenTree(landGroup, new Vector3(frontX, 0f, halfZ - 3.5f), 1.2f);
            CreateGardenTree(landGroup, new Vector3(frontX, 0f, -halfZ + 3.5f), 1.1f);
            CreateGardenTree(landGroup, new Vector3(rearX, 0f, halfZ - 3.0f), 1.0f);
            CreateGardenTree(landGroup, new Vector3(rearX, 0f, -halfZ + 3.0f), 1.3f);

            // 2. Çiçek Tarhları (Renkli çiçek adacıkları)
            CreateFlowerPatch(landGroup, new Vector3(frontX + (entranceEast ? -1.8f : 1.8f), 0.08f, 3.2f));
            CreateFlowerPatch(landGroup, new Vector3(frontX + (entranceEast ? -1.8f : 1.8f), 0.08f, -3.2f));

            // 3. Bahçe Dinlenme Bankı
            CreateGardenBench(landGroup, new Vector3(frontX, 0f, 4.8f), entranceEast ? 180f : 0f);

            // 4. Bahçe Aydınlatma Direği (Sokak / Bahçe Lambası)
            CreateGardenLamppost(landGroup, new Vector3(frontX + (entranceEast ? 1.0f : -1.0f), 0f, -2.6f));
        }

        private static void CreateGardenTree(Transform parent, Vector3 localPos, float scale)
        {
            GameObject tree = new GameObject("Garden_Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = localPos;
            tree.transform.localScale = Vector3.one * scale;

            // Gövde
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
            Object.Destroy(trunk.GetComponent<Collider>());

            // Yaprak Katmanları (2 Kademeli Low-Poly Koni/Küre)
            GameObject foliage1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage1.name = "Foliage_Lower";
            foliage1.transform.SetParent(tree.transform, false);
            foliage1.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            foliage1.transform.localScale = new Vector3(2.4f, 2.0f, 2.4f);
            foliage1.GetComponent<Renderer>().sharedMaterial = foliageMat;
            Object.Destroy(foliage1.GetComponent<Collider>());

            GameObject foliage2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage2.name = "Foliage_Upper";
            foliage2.transform.SetParent(tree.transform, false);
            foliage2.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            foliage2.transform.localScale = new Vector3(1.8f, 1.6f, 1.8f);
            foliage2.GetComponent<Renderer>().sharedMaterial = foliageMat;
            Object.Destroy(foliage2.GetComponent<Collider>());
        }

        private static void CreateFlowerPatch(Transform parent, Vector3 localPos)
        {
            GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Flower_Bed";
            bed.transform.SetParent(parent, false);
            bed.transform.localPosition = localPos;
            bed.transform.localScale = new Vector3(2.2f, 0.16f, 1.6f);
            bed.GetComponent<Renderer>().sharedMaterial = flowerBedMat;
            Object.Destroy(bed.GetComponent<Collider>());
        }

        private static void CreateGardenBench(Transform parent, Vector3 localPos, float rotY)
        {
            GameObject bench = new GameObject("Garden_Bench");
            bench.transform.SetParent(parent, false);
            bench.transform.localPosition = localPos;
            bench.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

            // Oturak
            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Bench_Seat";
            seat.transform.SetParent(bench.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            seat.transform.localScale = new Vector3(0.65f, 0.08f, 1.8f);
            seat.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;
            Object.Destroy(seat.GetComponent<Collider>());

            // Sırtlık
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Bench_Back";
            back.transform.SetParent(bench.transform, false);
            back.transform.localPosition = new Vector3(-0.28f, 0.82f, 0f);
            back.transform.localScale = new Vector3(0.08f, 0.65f, 1.8f);
            back.GetComponent<Renderer>().sharedMaterial = fenceWoodMat;
            Object.Destroy(back.GetComponent<Collider>());

            // Ayaklar
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = "Bench_Leg";
                leg.transform.SetParent(bench.transform, false);
                leg.transform.localPosition = new Vector3(0f, 0.22f, i * 0.75f);
                leg.transform.localScale = new Vector3(0.60f, 0.44f, 0.10f);
                leg.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
                Object.Destroy(leg.GetComponent<Collider>());
            }
        }

        private static void CreateGardenLamppost(Transform parent, Vector3 localPos)
        {
            GameObject lamp = new GameObject("Garden_Lamppost");
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = localPos;

            // Direk
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Lamp_Pole";
            pole.transform.SetParent(lamp.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            pole.transform.localScale = new Vector3(0.12f, 1.6f, 0.12f);
            pole.GetComponent<Renderer>().sharedMaterial = frameDarkMat;
            Object.Destroy(pole.GetComponent<Collider>());

            // Işık Başlığı
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Lamp_Bulb_Globe";
            head.transform.SetParent(lamp.transform, false);
            head.transform.localPosition = new Vector3(0f, 3.25f, 0f);
            head.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            head.GetComponent<Renderer>().sharedMaterial = glassWindowMat;
            Object.Destroy(head.GetComponent<Collider>());
        }
    }
}
