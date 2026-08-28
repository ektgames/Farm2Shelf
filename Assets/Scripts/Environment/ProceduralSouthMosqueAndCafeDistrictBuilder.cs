using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Farm2Shelf Güney Bölgesi Mimari İnşaatçısı:
    /// - Batı Parseli: 2 Minareli Bahçeli Büyük Cami (Mermer Şadırvanlı Avlu, Kubbeler, Vitray Camlar, Yürüyüş Yolu)
    /// - Orta Cadde: Cami ve Kafeler arasından geçen dikey cadde ve kaldırımlar (X: 0.0m)
    /// - Doğu Parseli: Doğu Caddesine bakan (X = +75m) 3 Adet Tek Katlı Şık Kafe (Çitli, Verandalı, Işıklı Tabelalı)
    /// - Gece/Gündüz Işıklandırma Entegrasyonu ve NavMesh Engelleri.
    /// </summary>
    public static class ProceduralSouthMosqueAndCafeDistrictBuilder
    {
        private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        private static Material GetMaterial(string name, Color color, float metallic = 0.10f, float smoothness = 0.40f, bool isEmissive = false, Color emissionColor = default)
        {
            if (matCache.TryGetValue(name, out Material cached) && cached != null)
            {
                return cached;
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

            if (isEmissive)
            {
                Color emCol = (emissionColor == default) ? color * 2.5f : emissionColor;
                if (newMat.HasProperty("_EmissionColor"))
                {
                    newMat.SetColor("_EmissionColor", emCol);
                    newMat.EnableKeyword("_EMISSION");
                }
                newMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            matCache[name] = newMat;
            return newMat;
        }

        public static void BuildDistrict(Transform parent)
        {
            Transform districtGroup = new GameObject("South_Mosque_And_Cafe_District").transform;
            districtGroup.SetParent(parent, false);

            // 1. GÜNEY YOL ŞEBEKESİ VE KALDIRIMLAR
            BuildSouthRoadNetworkAndSidewalks(districtGroup);

            // 2. BATI PARSELİ: BAHÇELİ 2 MİNARELİ BÜYÜK CAMİ (X: -71m ile -4m arası, Z: -58m ile -124m arası)
            BuildGrandMosqueAndCourtyard(districtGroup, new Vector3(-37.5f, 0f, -91.5f));

            // 3. DOĞU PARSELİ: DOĞU CADDESİNE BAKAN 3 ADET TEK KATLI KAFE (X: +4m ile +71m arası)
            BuildThreeSingleStoryCafes(districtGroup, new Vector3(37.5f, 0f, -91.5f));
        }

        #region 1. South Road Network & Sidewalks

        private static void BuildSouthRoadNetworkAndSidewalks(Transform parent)
        {
            Transform roadGroup = new GameObject("South_Road_Network").transform;
            roadGroup.SetParent(parent, false);

            Material roadMat = GetMaterial("SouthRoadAsphaltMat", new Color(0.18f, 0.20f, 0.22f), 0.0f, 0.35f);
            Material lineMat = GetMaterial("SouthRoadLineYellowMat", new Color(0.95f, 0.80f, 0.15f), 0.0f, 0.2f);
            Material swMat = GetMaterial("SouthSidewalkConcreteMat", new Color(0.70f, 0.72f, 0.75f), 0.0f, 0.4f);
            Material darkMat = GetMaterial("SouthLampMetalMat", new Color(0.15f, 0.16f, 0.18f), 0.7f, 0.6f);
            Material crosswalkMat = GetMaterial("SouthCrosswalkWhiteMat", new Color(0.95f, 0.95f, 0.95f), 0.0f, 0.2f);

            // ==========================================
            // A) DİKEY VE YATAY ASFALT CADDELER
            // ==========================================

            // 1. ORTA DİKEY CADDE (X = 0.0m | Z: -55m ile -128m arası, Genişlik: 6m -> X: -3m .. +3m)
            GameObject midRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midRoad.name = "South_Middle_Avenue_Road";
            midRoad.transform.SetParent(roadGroup, false);
            midRoad.transform.position = new Vector3(0f, -0.05f, -91.5f);
            midRoad.transform.localScale = new Vector3(6.0f, 0.1f, 73.0f);
            midRoad.GetComponent<Renderer>().sharedMaterial = roadMat;

            for (float z = -58.0f; z >= -125.0f; z -= 3.0f)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Road_Line";
                line.transform.SetParent(roadGroup, false);
                line.transform.position = new Vector3(0f, 0.01f, z);
                line.transform.localScale = new Vector3(0.25f, 0.02f, 1.8f);
                line.GetComponent<Renderer>().sharedMaterial = lineMat;
            }

            // 2. BATI DİKEY CADDE UZATMASI (X = -75.0m | Z: -55m ile -128m, Genişlik: 6m -> X: -78m .. -72m)
            GameObject westRoadExt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westRoadExt.name = "West_Road_South_Extension";
            westRoadExt.transform.SetParent(roadGroup, false);
            westRoadExt.transform.position = new Vector3(-75.0f, -0.05f, -91.5f);
            westRoadExt.transform.localScale = new Vector3(6.0f, 0.1f, 73.0f);
            westRoadExt.GetComponent<Renderer>().sharedMaterial = roadMat;

            for (float z = -58.0f; z >= -125.0f; z -= 3.0f)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Road_Line";
                line.transform.SetParent(roadGroup, false);
                line.transform.position = new Vector3(-75.0f, 0.01f, z);
                line.transform.localScale = new Vector3(0.25f, 0.02f, 1.8f);
                line.GetComponent<Renderer>().sharedMaterial = lineMat;
            }

            // 3. DOĞU DİKEY CADDE UZATMASI (X = +75.0m | Z: -55m ile -128m, Genişlik: 6m -> X: +72m .. +78m)
            GameObject eastRoadExt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastRoadExt.name = "East_Road_South_Extension";
            eastRoadExt.transform.SetParent(roadGroup, false);
            eastRoadExt.transform.position = new Vector3(75.0f, -0.05f, -91.5f);
            eastRoadExt.transform.localScale = new Vector3(6.0f, 0.1f, 73.0f);
            eastRoadExt.GetComponent<Renderer>().sharedMaterial = roadMat;

            for (float z = -58.0f; z >= -125.0f; z -= 3.0f)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Road_Line";
                line.transform.SetParent(roadGroup, false);
                line.transform.position = new Vector3(75.0f, 0.01f, z);
                line.transform.localScale = new Vector3(0.25f, 0.02f, 1.8f);
                line.GetComponent<Renderer>().sharedMaterial = lineMat;
            }

            // 4. EN GÜNEY DIŞ ÇEVRE YOLU (Z = -128.0m | X: -78m ile +78m arası, Genişlik: 6m -> Z: -131m .. -125m)
            GameObject bottomRoad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottomRoad.name = "South_Outer_Perimeter_Road";
            bottomRoad.transform.SetParent(roadGroup, false);
            bottomRoad.transform.position = new Vector3(0f, -0.05f, -128.0f);
            bottomRoad.transform.localScale = new Vector3(156.0f, 0.1f, 6.0f);
            bottomRoad.GetComponent<Renderer>().sharedMaterial = roadMat;

            for (float x = -75.0f; x <= 75.0f; x += 3.0f)
            {
                if (Mathf.Abs(x) < 3.2f || Mathf.Abs(x - (-75f)) < 3.2f || Mathf.Abs(x - 75f) < 3.2f) continue;

                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Road_Line";
                line.transform.SetParent(roadGroup, false);
                line.transform.position = new Vector3(x, 0.01f, -128.0f);
                line.transform.localScale = new Vector3(1.8f, 0.02f, 0.25f);
                line.GetComponent<Renderer>().sharedMaterial = lineMat;
            }

            // ==========================================
            // B) SOL PARSEL (CAMİ): ÇEVRELEYEN VE KÖŞELERİ DÖNEN KALDIRIMLAR
            // Sınırlar: X: -72m .. -3m | Z: -58m .. -125m
            // ==========================================
            Transform mosqueSwGroup = new GameObject("Mosque_Parcel_Wrapping_Sidewalks").transform;
            mosqueSwGroup.SetParent(roadGroup, false);

            // 1. Kuzey Kaldırımı (Üst yol boyunca yatay, köşelerde batı ve doğu kaldırımlarıyla birleşir)
            GameObject mSwNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mSwNorth.name = "Mosque_Sidewalk_North";
            mSwNorth.transform.SetParent(mosqueSwGroup, false);
            mSwNorth.transform.position = new Vector3(-37.5f, 0.05f, -59.0f);
            mSwNorth.transform.localScale = new Vector3(69.0f, 0.20f, 2.0f);
            mSwNorth.GetComponent<Renderer>().sharedMaterial = swMat;

            // 2. Güney Kaldırımı (Alt çevre yolu boyunca yatay)
            GameObject mSwSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mSwSouth.name = "Mosque_Sidewalk_South";
            mSwSouth.transform.SetParent(mosqueSwGroup, false);
            mSwSouth.transform.position = new Vector3(-37.5f, 0.05f, -124.0f);
            mSwSouth.transform.localScale = new Vector3(69.0f, 0.20f, 2.0f);
            mSwSouth.GetComponent<Renderer>().sharedMaterial = swMat;

            // 3. Batı Kaldırımı (Batı yolu boyunca dikey - yolu takip edip kuzey ve güneyde direkt döner)
            GameObject mSwWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mSwWest.name = "Mosque_Sidewalk_West";
            mSwWest.transform.SetParent(mosqueSwGroup, false);
            mSwWest.transform.position = new Vector3(-71.0f, 0.05f, -91.5f);
            mSwWest.transform.localScale = new Vector3(2.0f, 0.20f, 67.0f);
            mSwWest.GetComponent<Renderer>().sharedMaterial = swMat;

            // 4. Doğu Kaldırımı (Orta yol sol kenarı boyunca dikey - yolu takip edip direkt döner)
            GameObject mSwEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mSwEast.name = "Mosque_Sidewalk_East_MiddleAvenue";
            mSwEast.transform.SetParent(mosqueSwGroup, false);
            mSwEast.transform.position = new Vector3(-4.0f, 0.05f, -91.5f);
            mSwEast.transform.localScale = new Vector3(2.0f, 0.20f, 67.0f);
            mSwEast.GetComponent<Renderer>().sharedMaterial = swMat;

            // ==========================================
            // C) SAĞ PARSEL (KAFELER): ÇEVRELEYEN VE KÖŞELERİ DÖNEN KALDIRIMLAR
            // Sınırlar: X: +3m .. +72m | Z: -58m .. -125m
            // ==========================================
            Transform cafeSwGroup = new GameObject("Cafes_Parcel_Wrapping_Sidewalks").transform;
            cafeSwGroup.SetParent(roadGroup, false);

            // 1. Kuzey Kaldırımı (Üst yol boyunca yatay, orta yol ve doğu yoluyla birleşir)
            GameObject cSwNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cSwNorth.name = "Cafes_Sidewalk_North";
            cSwNorth.transform.SetParent(cafeSwGroup, false);
            cSwNorth.transform.position = new Vector3(37.5f, 0.05f, -59.0f);
            cSwNorth.transform.localScale = new Vector3(69.0f, 0.20f, 2.0f);
            cSwNorth.GetComponent<Renderer>().sharedMaterial = swMat;

            // 2. Güney Kaldırımı (Alt çevre yolu boyunca yatay)
            GameObject cSwSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cSwSouth.name = "Cafes_Sidewalk_South";
            cSwSouth.transform.SetParent(cafeSwGroup, false);
            cSwSouth.transform.position = new Vector3(37.5f, 0.05f, -124.0f);
            cSwSouth.transform.localScale = new Vector3(69.0f, 0.20f, 2.0f);
            cSwSouth.GetComponent<Renderer>().sharedMaterial = swMat;

            // 3. Batı Kaldırımı (Orta yol sağ kenarı boyunca dikey - yolu takip edip direkt döner)
            GameObject cSwWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cSwWest.name = "Cafes_Sidewalk_West_MiddleAvenue";
            cSwWest.transform.SetParent(cafeSwGroup, false);
            cSwWest.transform.position = new Vector3(4.0f, 0.05f, -91.5f);
            cSwWest.transform.localScale = new Vector3(2.0f, 0.20f, 67.0f);
            cSwWest.GetComponent<Renderer>().sharedMaterial = swMat;

            // 4. Doğu Kaldırımı (Doğu yolu sol kenarı boyunca dikey - kafelerin önü)
            GameObject cSwEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cSwEast.name = "Cafes_Sidewalk_East";
            cSwEast.transform.SetParent(cafeSwGroup, false);
            cSwEast.transform.position = new Vector3(71.0f, 0.05f, -91.5f);
            cSwEast.transform.localScale = new Vector3(2.0f, 0.20f, 67.0f);
            cSwEast.GetComponent<Renderer>().sharedMaterial = swMat;

            // ==========================================
            // D) EN DIŞ ÇEVRE KALDIRIMLARI (HARİTA SINIRLARI)
            // ==========================================
            // 1. En Batı Dış Kaldırım
            GameObject outerSwWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outerSwWest.name = "South_Outer_Sidewalk_West";
            outerSwWest.transform.SetParent(roadGroup, false);
            outerSwWest.transform.position = new Vector3(-79.5f, 0.05f, -91.5f);
            outerSwWest.transform.localScale = new Vector3(3.0f, 0.20f, 73.0f);
            outerSwWest.GetComponent<Renderer>().sharedMaterial = swMat;

            // 2. En Doğu Dış Kaldırım
            GameObject outerSwEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outerSwEast.name = "South_Outer_Sidewalk_East";
            outerSwEast.transform.SetParent(roadGroup, false);
            outerSwEast.transform.position = new Vector3(79.5f, 0.05f, -91.5f);
            outerSwEast.transform.localScale = new Vector3(3.0f, 0.20f, 73.0f);
            outerSwEast.GetComponent<Renderer>().sharedMaterial = swMat;

            // 3. En Güney Dış Kaldırım (Yolun Dış / Alt Kenarı)
            GameObject outerSwSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outerSwSouth.name = "South_Outer_Sidewalk_Bottom";
            outerSwSouth.transform.SetParent(roadGroup, false);
            outerSwSouth.transform.position = new Vector3(0f, 0.05f, -132.5f);
            outerSwSouth.transform.localScale = new Vector3(162.0f, 0.20f, 3.0f);
            outerSwSouth.GetComponent<Renderer>().sharedMaterial = swMat;

            // ==========================================
            // E) KAVŞAK YAYA GEÇİTLERİ (ZEBRA ÇİZGİLERİ)
            // ==========================================
            // Orta Cadde Kuzey Girişi Yaya Geçidi (Z: -56.5m, X: -2.5m .. +2.5m)
            Transform midNorthCrosswalk = new GameObject("Crosswalk_Mid_Avenue_North").transform;
            midNorthCrosswalk.SetParent(roadGroup, false);
            for (float x = -2.4f; x <= 2.4f; x += 0.8f)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Zebra_Stripe";
                stripe.transform.SetParent(midNorthCrosswalk, false);
                stripe.transform.position = new Vector3(x, 0.02f, -56.5f);
                stripe.transform.localScale = new Vector3(0.45f, 0.01f, 2.0f);
                stripe.GetComponent<Renderer>().sharedMaterial = crosswalkMat;
            }

            // ==========================================
            // F) CADDE VE KALDIRIM SOKAK AYDINLATMALARI
            // ==========================================
            float[] lampZ = new float[] { -68.0f, -91.5f, -115.0f };
            foreach (float lz in lampZ)
            {
                // Orta cadde lambaları (Kaldırımın üzerine hizalı)
                BuildStreetLamp(roadGroup, new Vector3(-4.0f, 0.05f, lz), true, darkMat);
                BuildStreetLamp(roadGroup, new Vector3(4.0f, 0.05f, lz), false, darkMat);

                // Dış cadde lambaları
                BuildStreetLamp(roadGroup, new Vector3(-71.0f, 0.05f, lz), false, darkMat);
                BuildStreetLamp(roadGroup, new Vector3(71.0f, 0.05f, lz), true, darkMat);
            }
        }

        #endregion

        #region 2. Grand Mosque & Courtyard (Bahçeli 2 Minareli Büyük Cami)

        private static void BuildGrandMosqueAndCourtyard(Transform parent, Vector3 parcelCenter)
        {
            GameObject mosqueParcel = new GameObject("Parcel_Grand_Mosque_And_Courtyard");
            mosqueParcel.transform.SetParent(parent, false);
            mosqueParcel.transform.position = parcelCenter;

            // Materyaller
            Material stoneWallMat = GetMaterial("MosqueLimestoneWallMat", new Color(0.92f, 0.90f, 0.86f), 0.05f, 0.35f);
            Material marbleMat = GetMaterial("MosqueWhiteMarbleMat", new Color(0.96f, 0.96f, 0.97f), 0.15f, 0.60f);
            Material domeLeadMat = GetMaterial("MosqueDomeLeadMat", new Color(0.38f, 0.44f, 0.48f), 0.25f, 0.50f);
            Material goldAlemMat = GetMaterial("MosqueGoldAlemMat", new Color(0.95f, 0.82f, 0.25f), 0.85f, 0.75f, true, new Color(0.95f, 0.80f, 0.20f) * 1.5f);
            Material woodDoorMat = GetMaterial("MosqueCarvedWoodDoorMat", new Color(0.38f, 0.22f, 0.12f), 0.05f, 0.30f);
            Material stainedGlassMat = GetMaterial("MosqueStainedGlassMat", new Color(0.30f, 0.65f, 0.85f, 0.90f), 0.10f, 0.80f);
            Material darkTrimMat = GetMaterial("MosqueDarkTrimMat", new Color(0.22f, 0.24f, 0.28f), 0.2f, 0.4f);
            Material courtyardTileMat = GetMaterial("MosqueCourtyardTileMat", new Color(0.85f, 0.84f, 0.82f), 0.05f, 0.45f);
            Material lawnGrassMat = GetMaterial("MosqueGardenLawnMat", new Color(0.24f, 0.58f, 0.24f), 0.0f, 0.2f);
            Material pathwayStoneMat = GetMaterial("MosqueWalkwayStoneMat", new Color(0.78f, 0.76f, 0.72f), 0.05f, 0.50f);

            // --- A) PARSEL ÇİM VE AVLU TABANI (X: -32m ile +32m, Z: -31m ile +31m) ---
            GameObject gardenBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gardenBase.name = "Garden_Lawn_Base";
            gardenBase.transform.SetParent(mosqueParcel.transform, false);
            gardenBase.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            gardenBase.transform.localScale = new Vector3(64.0f, 0.05f, 62.0f);
            gardenBase.GetComponent<Renderer>().sharedMaterial = lawnGrassMat;

            // Mermer Avlu Meydanı (Ön tarafta: Z: -3m ile +25m arası)
            GameObject courtyardBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            courtyardBase.name = "Marble_Courtyard_Plaza";
            courtyardBase.transform.SetParent(mosqueParcel.transform, false);
            courtyardBase.transform.localPosition = new Vector3(0f, 0.01f, 10.0f);
            courtyardBase.transform.localScale = new Vector3(48.0f, 0.06f, 32.0f);
            courtyardBase.GetComponent<Renderer>().sharedMaterial = courtyardTileMat;

            // --- B) CAMİ ANA İBADETHANE BİNASI (Merkez Z = -8.0m) ---
            Vector3 buildingLocalPos = new Vector3(0f, 0f, -8.0f);
            GameObject mosqueBuilding = new GameObject("Mosque_Main_Sanctuary");
            mosqueBuilding.transform.SetParent(mosqueParcel.transform, false);
            mosqueBuilding.transform.localPosition = buildingLocalPos;

            // 1. Mermer Kaide (Sub-Base Plinth)
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Mosque_Plinth";
            plinth.transform.SetParent(mosqueBuilding.transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            plinth.transform.localScale = new Vector3(26.0f, 0.70f, 22.0f);
            plinth.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 2. Ana Gövde Duvarları (Kare/Sekizgen Taş Yapı - 24m x 20m x 9m)
            GameObject mainWalls = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainWalls.name = "Main_Prayer_Hall_Walls";
            mainWalls.transform.SetParent(mosqueBuilding.transform, false);
            mainWalls.transform.localPosition = new Vector3(0f, 5.20f, 0f);
            mainWalls.transform.localScale = new Vector3(24.0f, 9.0f, 20.0f);
            mainWalls.GetComponent<Renderer>().sharedMaterial = stoneWallMat;

            // 3. Kubbe Kasnağı (Octagonal Drum)
            GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "Dome_Drum_Octagon";
            drum.transform.SetParent(mosqueBuilding.transform, false);
            drum.transform.localPosition = new Vector3(0f, 10.5f, 0f);
            drum.transform.localScale = new Vector3(14.0f, 1.2f, 14.0f);
            drum.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 4. BÜYÜK MERKEZİ KUBBE (Grand Central Dome - Çap 13.5m, Yükseklik 6m)
            GameObject centralDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            centralDome.name = "Grand_Central_Dome";
            centralDome.transform.SetParent(mosqueBuilding.transform, false);
            centralDome.transform.localPosition = new Vector3(0f, 13.2f, 0f);
            centralDome.transform.localScale = new Vector3(13.5f, 8.5f, 13.5f);
            centralDome.GetComponent<Renderer>().sharedMaterial = domeLeadMat;

            // Kubbe Tepesi Altın Hilal / Alem
            BuildGoldenCrescentAlem(mosqueBuilding.transform, new Vector3(0f, 18.0f, 0f), 2.2f, goldAlemMat);

            // 5. 4 KÖŞE YARIM KUBBESİ (Corner Semi-Domes)
            Vector3[] cornerDomeOffsets = new Vector3[]
            {
                new Vector3(-7.5f, 10.0f, -6.5f),
                new Vector3(7.5f, 10.0f, -6.5f),
                new Vector3(-7.5f, 10.0f, 6.5f),
                new Vector3(7.5f, 10.0f, 6.5f)
            };
            foreach (var cdPos in cornerDomeOffsets)
            {
                GameObject sDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sDome.name = "Semi_Dome_Corner";
                sDome.transform.SetParent(mosqueBuilding.transform, false);
                sDome.transform.localPosition = cdPos;
                sDome.transform.localScale = new Vector3(6.5f, 4.5f, 6.5f);
                sDome.GetComponent<Renderer>().sharedMaterial = domeLeadMat;

                BuildGoldenCrescentAlem(mosqueBuilding.transform, cdPos + Vector3.up * 2.6f, 1.2f, goldAlemMat);
            }

            // 6. REVAKLI SON CEMAAT YERİ (Ön Cephe Portikosu - Kuzeye/Bahçeye Bakan Z: +10m)
            float porticoZ = 10.0f + 1.8f;
            GameObject porticoRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            porticoRoof.name = "Portico_Roof";
            porticoRoof.transform.SetParent(mosqueBuilding.transform, false);
            porticoRoof.transform.localPosition = new Vector3(0f, 5.8f, porticoZ);
            porticoRoof.transform.localScale = new Vector3(22.0f, 0.6f, 4.2f);
            porticoRoof.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // Revak Küçük Kubbeleri (5 Adet)
            for (int k = -2; k <= 2; k++)
            {
                GameObject pDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pDome.name = $"Portico_Dome_{k + 2}";
                pDome.transform.SetParent(mosqueBuilding.transform, false);
                pDome.transform.localPosition = new Vector3(k * 4.4f, 6.4f, porticoZ);
                pDome.transform.localScale = new Vector3(3.6f, 2.2f, 3.6f);
                pDome.GetComponent<Renderer>().sharedMaterial = domeLeadMat;

                BuildGoldenCrescentAlem(mosqueBuilding.transform, new Vector3(k * 4.4f, 7.6f, porticoZ), 0.75f, goldAlemMat);
            }

            // Revak Mermer Sütunları (6 Adet)
            for (int c = 0; c < 6; c++)
            {
                float colX = -10.0f + (c * 4.0f);
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Portico_Pillar_{c + 1}";
                pillar.transform.SetParent(mosqueBuilding.transform, false);
                pillar.transform.localPosition = new Vector3(colX, 3.0f, porticoZ + 1.8f);
                pillar.transform.localScale = new Vector3(0.55f, 2.6f, 0.55f);
                pillar.GetComponent<Renderer>().sharedMaterial = marbleMat;

                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cap.name = "Pillar_Capital";
                cap.transform.SetParent(pillar.transform, false);
                cap.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                cap.transform.localScale = new Vector3(1.4f, 0.25f, 1.4f);
                cap.GetComponent<Renderer>().sharedMaterial = marbleMat;
            }

            // 7. GÖRKEMLİ TAÇ KAPI (Grand Portal - Bahçeye Açılan Ana Kapı)
            GameObject portalFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portalFrame.name = "Grand_Portal_Arch";
            portalFrame.transform.SetParent(mosqueBuilding.transform, false);
            portalFrame.transform.localPosition = new Vector3(0f, 3.2f, 10.1f);
            portalFrame.transform.localScale = new Vector3(4.8f, 5.5f, 0.50f);
            portalFrame.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // Oymalı Ahşap Çift Kanat Kapı
            GameObject woodenDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            woodenDoor.name = "Carved_Wooden_Main_Door";
            woodenDoor.transform.SetParent(portalFrame.transform, false);
            woodenDoor.transform.localPosition = new Vector3(0f, -0.15f, 0.15f);
            woodenDoor.transform.localScale = new Vector3(0.68f, 0.72f, 0.30f);
            woodenDoor.GetComponent<Renderer>().sharedMaterial = woodDoorMat;

            // Altın Kapı Kolları
            GameObject handleL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handleL.name = "Door_Knocker_L";
            handleL.transform.SetParent(woodenDoor.transform, false);
            handleL.transform.localPosition = new Vector3(-0.22f, 0f, 0.6f);
            handleL.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            handleL.GetComponent<Renderer>().sharedMaterial = goldAlemMat;

            GameObject handleR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handleR.name = "Door_Knocker_R";
            handleR.transform.SetParent(woodenDoor.transform, false);
            handleR.transform.localPosition = new Vector3(0.22f, 0f, 0.6f);
            handleR.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            handleR.GetComponent<Renderer>().sharedMaterial = goldAlemMat;

            // Giriş Üstü Sıcak Fener Lambası
            GameObject entranceLightObj = new GameObject("Mosque_Entrance_Lantern_Light");
            entranceLightObj.transform.SetParent(portalFrame.transform, false);
            entranceLightObj.transform.localPosition = new Vector3(0f, 0.45f, 0.8f);
            Light eLight = entranceLightObj.AddComponent<Light>();
            eLight.type = LightType.Point;
            eLight.color = new Color(1.0f, 0.88f, 0.55f);
            eLight.intensity = 1.6f;
            eLight.range = 6.0f;
            eLight.shadows = LightShadows.None;
            eLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(eLight);
            }

            // 8. 2 KATLI KEMERLİ VİTRAY PENCERELER (Tüm Cepheler)
            BuildMosqueWindows(mosqueBuilding.transform, stainedGlassMat, marbleMat);

            // 9. CAMİ İÇİ GÖRKEMLİ AVİZE AYDINLATMASI (Gece Pencerelerden Süzülen Yumuşak Işık)
            GameObject interiorLightObj = new GameObject("Mosque_Grand_Chandelier_Interior_Light");
            interiorLightObj.transform.SetParent(mosqueBuilding.transform, false);
            interiorLightObj.transform.localPosition = new Vector3(0f, 5.5f, 0f);
            Light inLight = interiorLightObj.AddComponent<Light>();
            inLight.type = LightType.Point;
            inLight.color = new Color(1.0f, 0.90f, 0.65f);
            inLight.intensity = 1.6f;
            inLight.range = 14.0f;
            inLight.renderMode = LightRenderMode.ForcePixel;
            inLight.shadows = LightShadows.None;
            inLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(inLight);
            }

            // 10. 3D TABELA ETİKETİ
            Create3DLabel("BÜYÜK KASABA CAMİİ 🕌", "GRAND TOWN MOSQUE 🕌", mosqueBuilding.transform, new Vector3(0f, 6.8f, porticoZ + 1.2f), new Color(1.0f, 0.90f, 0.40f));

            // Fizik & NavMesh Obstacle
            BoxCollider mCol = mosqueBuilding.AddComponent<BoxCollider>();
            mCol.center = new Vector3(0f, 6.0f, 1.0f);
            mCol.size = new Vector3(27.0f, 14.0f, 25.0f);

            NavMeshObstacle mObs = mosqueBuilding.AddComponent<NavMeshObstacle>();
            mObs.shape = NavMeshObstacleShape.Box;
            mObs.center = mCol.center;
            mObs.size = mCol.size;
            mObs.carving = true;
            mObs.carveOnlyStationary = true;

            // --- C) 2 ADET GÖRKEMLİ ÇİFT ŞEREFELİ MİNARE (24 Metre) ---
            // 1. Batı Minaresi (Kuzeybatı Köşesi)
            BuildDetailedMinaret(mosqueParcel.transform, buildingLocalPos + new Vector3(-13.5f, 0f, 9.5f), "Minaret_West", marbleMat, domeLeadMat, goldAlemMat);
            // 2. Doğu Minaresi (Kuzeydoğu Köşesi)
            BuildDetailedMinaret(mosqueParcel.transform, buildingLocalPos + new Vector3(13.5f, 0f, 9.5f), "Minaret_East", marbleMat, domeLeadMat, goldAlemMat);

            // --- D) AVLU ŞADIRVANI (Mermer Abdest Havuzu - Avlu Merkezinde Z = +10.0m) ---
            BuildAblutionFountain(mosqueParcel.transform, new Vector3(0f, 0.05f, 10.0f), marbleMat, domeLeadMat, goldAlemMat, woodDoorMat);

            // --- E) BAHÇE YÜRÜYÜŞ YOLLARI (Ana Kapıdan Kaldırımlara Bağlantı) ---
            BuildMosqueWalkways(mosqueParcel.transform, pathwayStoneMat);

            // --- F) AVLU VE BAHÇE PEYZAJI (Servi Ağaçları, Çiçekler, Banklar, Çitler) ---
            BuildMosqueGardenLandscape(mosqueParcel.transform, darkTrimMat, marbleMat);
        }

        private static void BuildDetailedMinaret(Transform parent, Vector3 localPos, string minaretName, Material marbleMat, Material leadMat, Material goldMat)
        {
            GameObject minaret = new GameObject(minaretName);
            minaret.transform.SetParent(parent, false);
            minaret.transform.localPosition = localPos;

            // 1. Kare Kaide (Square Base - 3.2m x 3.2m x 6m)
            GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBlock.name = "Minaret_Base";
            baseBlock.transform.SetParent(minaret.transform, false);
            baseBlock.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            baseBlock.transform.localScale = new Vector3(3.2f, 6.0f, 3.2f);
            baseBlock.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 2. Sekizgen Geçiş Pabucu (Transition Octagon)
            GameObject transition = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            transition.name = "Transition_Octagon";
            transition.transform.SetParent(minaret.transform, false);
            transition.transform.localPosition = new Vector3(0f, 6.8f, 0f);
            transition.transform.localScale = new Vector3(2.4f, 1.6f, 2.4f);
            transition.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 3. Alt Minare Gövdesi (Lower Cylindrical Shaft - Çap 1.6m x 7m)
            GameObject lowerShaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lowerShaft.name = "Lower_Shaft";
            lowerShaft.transform.SetParent(minaret.transform, false);
            lowerShaft.transform.localPosition = new Vector3(0f, 11.2f, 0f);
            lowerShaft.transform.localScale = new Vector3(1.6f, 3.6f, 1.6f);
            lowerShaft.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 4. 1. Şerefe (Alt Balkon & Korkuluk - Y = 15.0m)
            GameObject balcony1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            balcony1.name = "Balcony_1_Platform";
            balcony1.transform.SetParent(minaret.transform, false);
            balcony1.transform.localPosition = new Vector3(0f, 15.0f, 0f);
            balcony1.transform.localScale = new Vector3(2.8f, 0.40f, 2.8f);
            balcony1.GetComponent<Renderer>().sharedMaterial = marbleMat;

            GameObject railing1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            railing1.name = "Balcony_1_Railing";
            railing1.transform.SetParent(minaret.transform, false);
            railing1.transform.localPosition = new Vector3(0f, 15.5f, 0f);
            railing1.transform.localScale = new Vector3(2.7f, 0.50f, 2.7f);
            railing1.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 1. Şerefe Gece Kandil Işığı
            GameObject bLight1Obj = new GameObject("Balcony1_Night_Lamp");
            bLight1Obj.transform.SetParent(balcony1.transform, false);
            bLight1Obj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Light bLight1 = bLight1Obj.AddComponent<Light>();
            bLight1.type = LightType.Point;
            bLight1.color = new Color(1.0f, 0.88f, 0.40f);
            bLight1.intensity = 1.3f;
            bLight1.range = 5.0f;
            bLight1.shadows = LightShadows.None;
            bLight1.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(bLight1);
            }

            // 5. Üst Minare Gövdesi (Upper Shaft - Çap 1.4m x 5m)
            GameObject upperShaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            upperShaft.name = "Upper_Shaft";
            upperShaft.transform.SetParent(minaret.transform, false);
            upperShaft.transform.localPosition = new Vector3(0f, 18.2f, 0f);
            upperShaft.transform.localScale = new Vector3(1.4f, 2.6f, 1.4f);
            upperShaft.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 6. 2. Şerefe (Üst Balkon - Y = 21.0m)
            GameObject balcony2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            balcony2.name = "Balcony_2_Platform";
            balcony2.transform.SetParent(minaret.transform, false);
            balcony2.transform.localPosition = new Vector3(0f, 21.0f, 0f);
            balcony2.transform.localScale = new Vector3(2.4f, 0.35f, 2.4f);
            balcony2.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 2. Şerefe Gece Kandil Işığı
            GameObject bLight2Obj = new GameObject("Balcony2_Night_Lamp");
            bLight2Obj.transform.SetParent(balcony2.transform, false);
            bLight2Obj.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            Light bLight2 = bLight2Obj.AddComponent<Light>();
            bLight2.type = LightType.Point;
            bLight2.color = new Color(1.0f, 0.88f, 0.40f);
            bLight2.intensity = 1.3f;
            bLight2.range = 5.0f;
            bLight2.shadows = LightShadows.None;
            bLight2.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(bLight2);
            }

            // 7. Petek ve Külah (Conical Spire Cone - Y = 23.5m)
            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = "Minaret_Cone_Spire";
            spire.transform.SetParent(minaret.transform, false);
            spire.transform.localPosition = new Vector3(0f, 23.5f, 0f);
            spire.transform.localScale = new Vector3(1.2f, 2.4f, 1.2f);
            spire.GetComponent<Renderer>().sharedMaterial = leadMat;

            // Külah Tepesi Altın Alem
            BuildGoldenCrescentAlem(minaret.transform, new Vector3(0f, 26.2f, 0f), 1.5f, goldMat);

            // Fizik Collider
            BoxCollider col = minaret.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 12f, 0f);
            col.size = new Vector3(3.4f, 25f, 3.4f);

            NavMeshObstacle obs = minaret.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.center = col.center;
            obs.size = col.size;
            obs.carving = true;
            obs.carveOnlyStationary = true;
        }

        private static void BuildAblutionFountain(Transform parent, Vector3 localPos, Material marbleMat, Material leadDomeMat, Material goldMat, Material woodMat)
        {
            GameObject fountain = new GameObject("Courtyard_Ablution_Fountain_Sadirvan");
            fountain.transform.SetParent(parent, false);
            fountain.transform.localPosition = localPos;

            Material waterMat = GetMaterial("SadirvanWaterMat", new Color(0.20f, 0.65f, 0.90f, 0.85f), 0.9f, 0.95f);

            // 1. Mermer Şadırvan Havuz Çanağı (Sekizgen Havuz - Çap 6.5m)
            GameObject basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basin.name = "Water_Basin";
            basin.transform.SetParent(fountain.transform, false);
            basin.transform.localPosition = new Vector3(0f, 0.40f, 0f);
            basin.transform.localScale = new Vector3(6.5f, 0.40f, 6.5f);
            basin.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // Havuz İçi Su Yüzeyi
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Water_Surface";
            water.transform.SetParent(fountain.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            water.transform.localScale = new Vector3(5.6f, 0.04f, 5.6f);
            water.GetComponent<Renderer>().sharedMaterial = waterMat;

            // 2. Orta Fıskiye Mermer Sütunu
            GameObject centerSpout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerSpout.name = "Center_Spout";
            centerSpout.transform.SetParent(fountain.transform, false);
            centerSpout.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            centerSpout.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
            centerSpout.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 3. Şadırvan Kubbesi ve Çatısı (8 Mermer Sütun Üzerinde)
            GameObject roofDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            roofDome.name = "Sadirvan_Canopy_Dome";
            roofDome.transform.SetParent(fountain.transform, false);
            roofDome.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            roofDome.transform.localScale = new Vector3(7.2f, 2.2f, 7.2f);
            roofDome.GetComponent<Renderer>().sharedMaterial = leadDomeMat;

            BuildGoldenCrescentAlem(fountain.transform, new Vector3(0f, 5.0f, 0f), 0.9f, goldMat);

            // 8 Sütun
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (Mathf.PI * 2f / 8f);
                float px = Mathf.Cos(angle) * 3.2f;
                float pz = Mathf.Sin(angle) * 3.2f;

                GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                col.name = $"Sadirvan_Pillar_{i + 1}";
                col.transform.SetParent(fountain.transform, false);
                col.transform.localPosition = new Vector3(px, 1.9f, pz);
                col.transform.localScale = new Vector3(0.28f, 1.8f, 0.28f);
                col.GetComponent<Renderer>().sharedMaterial = marbleMat;

                // Ahşap Abdest Taburesi
                float sx = Mathf.Cos(angle) * 3.9f;
                float sz = Mathf.Sin(angle) * 3.9f;
                GameObject stool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stool.name = $"Ablution_Stool_{i + 1}";
                stool.transform.SetParent(fountain.transform, false);
                stool.transform.localPosition = new Vector3(sx, 0.22f, sz);
                stool.transform.localScale = new Vector3(0.6f, 0.22f, 0.6f);
                stool.GetComponent<Renderer>().sharedMaterial = woodMat;
            }

            // Şadırvan İçi Turkuaz Su Işığı
            GameObject fLightObj = new GameObject("Sadirvan_Water_Night_Light");
            fLightObj.transform.SetParent(fountain.transform, false);
            fLightObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            Light fLight = fLightObj.AddComponent<Light>();
            fLight.type = LightType.Point;
            fLight.color = new Color(0.35f, 0.85f, 1.0f);
            fLight.intensity = 1.3f;
            fLight.range = 5.5f;
            fLight.shadows = LightShadows.None;
            fLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(fLight);
            }
        }

        private static void BuildGoldenCrescentAlem(Transform parent, Vector3 localPos, float scale, Material goldMat)
        {
            GameObject alem = new GameObject("Golden_Crescent_Alem");
            alem.transform.SetParent(parent, false);
            alem.transform.localPosition = localPos;
            alem.transform.localScale = Vector3.one * scale;

            // Mil Direği
            GameObject spindle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spindle.name = "Alem_Spindle";
            spindle.transform.SetParent(alem.transform, false);
            spindle.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            spindle.transform.localScale = new Vector3(0.08f, 0.4f, 0.08f);
            spindle.GetComponent<Renderer>().sharedMaterial = goldMat;
            Object.Destroy(spindle.GetComponent<Collider>());

            // Hilal (Crescent)
            GameObject crescent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crescent.name = "Alem_Hilal";
            crescent.transform.SetParent(alem.transform, false);
            crescent.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            crescent.transform.localScale = new Vector3(0.40f, 0.40f, 0.12f);
            crescent.GetComponent<Renderer>().sharedMaterial = goldMat;
            Object.Destroy(crescent.GetComponent<Collider>());
        }

        private static void BuildMosqueWindows(Transform parent, Material stainedGlassMat, Material frameMat)
        {
            // 4 Cephede 2 Katlı Kemerli Vitray Pencereler
            float[] windowX = new float[] { -6.5f, 0f, 6.5f };
            float[] windowY = new float[] { 3.2f, 6.8f };

            // Kuzey ve Güney Cepheleri (Z: +10.0m ve -10.0m dış duvar yüzeyleri)
            foreach (float wx in windowX)
            {
                foreach (float wy in windowY)
                {
                    if (wy < 4.0f && Mathf.Abs(wx) < 1.0f) continue; // Taç kapı yeri

                    CreateStainedGlassWindow(parent, new Vector3(wx, wy, 10.0f), new Vector2(1.6f, 2.4f), Vector3.forward, stainedGlassMat, frameMat);
                    CreateStainedGlassWindow(parent, new Vector3(wx, wy, -10.0f), new Vector2(1.6f, 2.4f), Vector3.back, stainedGlassMat, frameMat);
                }
            }

            // Doğu ve Batı Cepheleri (X: +12.0m ve -12.0m dış duvar yüzeyleri)
            float[] windowZ = new float[] { -5.5f, 0f, 5.5f };
            foreach (float wz in windowZ)
            {
                foreach (float wy in windowY)
                {
                    CreateStainedGlassWindow(parent, new Vector3(12.0f, wy, wz), new Vector2(1.6f, 2.4f), Vector3.right, stainedGlassMat, frameMat);
                    CreateStainedGlassWindow(parent, new Vector3(-12.0f, wy, wz), new Vector2(1.6f, 2.4f), Vector3.left, stainedGlassMat, frameMat);
                }
            }
        }

        private static void CreateStainedGlassWindow(Transform parent, Vector3 localPos, Vector2 size, Vector3 normal, Material glassMat, Material frameMat)
        {
            GameObject win = new GameObject("Stained_Glass_Window");
            win.transform.SetParent(parent, false);
            win.transform.localPosition = localPos;
            win.transform.localRotation = Quaternion.LookRotation(normal);

            // 1. Taş Kemer Çerçevesi (Duvardan 0.08m dışarı taşar, derinlik = 0.16m)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Arch_Frame";
            frame.transform.SetParent(win.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.08f);
            frame.transform.localScale = new Vector3(size.x + 0.30f, size.y + 0.30f, 0.16f);
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(frame.GetComponent<Collider>());

            // 2. Vitray Cam Paneli (Çerçevenin içinde 0.06m gömülü durur, derinlik = 0.02m)
            // Asla duvar veya çerçeve yüzeyiyle çakışmaz (Z-Fighting ve titreme %100 engellendi)
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Glass_Pane";
            glass.transform.SetParent(win.transform, false);
            glass.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            glass.transform.localScale = new Vector3(size.x - 0.10f, size.y - 0.10f, 0.02f);
            glass.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(glass.GetComponent<Collider>());

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterBuildingWindow(glass);
            }
        }

        private static void BuildMosqueWalkways(Transform parent, Material pathMat)
        {
            Transform walkwayGroup = new GameObject("Mosque_Pedestrian_Walkways").transform;
            walkwayGroup.SetParent(parent, false);

            // 1. Ana Kapıdan Kuzey Kaldırımına (Üst Yola) Düz Bağlantı Yolu (Z: 0m -> Z: +31m, Genişlik = 3.6m)
            GameObject northPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northPath.name = "Walkway_Main_To_North_Sidewalk";
            northPath.transform.SetParent(walkwayGroup, false);
            northPath.transform.localPosition = new Vector3(0f, 0.02f, 16.5f);
            northPath.transform.localScale = new Vector3(3.6f, 0.04f, 31.0f);
            northPath.GetComponent<Renderer>().sharedMaterial = pathMat;

            // 2. Şadırvandan Doğu Kaldırımına (Orta Yola) Bağlantı Yolu (X: 0m -> X: +32m)
            GameObject eastPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastPath.name = "Walkway_To_East_Mid_Road_Sidewalk";
            eastPath.transform.SetParent(walkwayGroup, false);
            eastPath.transform.localPosition = new Vector3(16.0f, 0.02f, 10.0f);
            eastPath.transform.localScale = new Vector3(32.0f, 0.04f, 3.2f);
            eastPath.GetComponent<Renderer>().sharedMaterial = pathMat;

            // 3. Şadırvandan Batı Kaldırımına Bağlantı Yolu (X: 0m -> X: -32m)
            GameObject westPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westPath.name = "Walkway_To_West_Road_Sidewalk";
            westPath.transform.SetParent(walkwayGroup, false);
            westPath.transform.localPosition = new Vector3(-16.0f, 0.02f, 10.0f);
            westPath.transform.localScale = new Vector3(32.0f, 0.04f, 3.2f);
            westPath.GetComponent<Renderer>().sharedMaterial = pathMat;
        }

        private static void BuildMosqueGardenLandscape(Transform parent, Material darkMat, Material marbleMat)
        {
            Transform gardenGroup = new GameObject("Mosque_Garden_Landscape").transform;
            gardenGroup.SetParent(parent, false);

            // 1. Servi Ağaçları (Italian Cypress Trees - İbadethane ve Avlu Etrafında 10 Adet)
            Vector3[] cypressPos = new Vector3[]
            {
                new Vector3(-24.0f, 0f, 24.0f),
                new Vector3(24.0f, 0f, 24.0f),
                new Vector3(-24.0f, 0f, -4.0f),
                new Vector3(24.0f, 0f, -4.0f),
                new Vector3(-24.0f, 0f, -22.0f),
                new Vector3(24.0f, 0f, -22.0f),
                new Vector3(-12.0f, 0f, -24.0f),
                new Vector3(12.0f, 0f, -24.0f),
                new Vector3(-8.0f, 0f, 26.0f),
                new Vector3(8.0f, 0f, 26.0f)
            };
            foreach (var pos in cypressPos)
            {
                BuildCypressTree(gardenGroup, pos);
            }

            // 2. Avlu Oturma Bankları (Mermer Kaideli Ahşap Banklar - 6 Adet)
            Vector3[] benchPos = new Vector3[]
            {
                new Vector3(-6.0f, 0.02f, 17.5f),
                new Vector3(6.0f, 0.02f, 17.5f),
                new Vector3(-16.0f, 0.02f, 15.0f),
                new Vector3(16.0f, 0.02f, 15.0f),
                new Vector3(-16.0f, 0.02f, 5.0f),
                new Vector3(16.0f, 0.02f, 5.0f)
            };
            foreach (var bPos in benchPos)
            {
                BuildGardenBench(gardenGroup, bPos);
            }

            // 3. Avlu Aydınlatma Fenerleri (Osmanlı Bahçe Lambaları - 6 Adet)
            Vector3[] lampPos = new Vector3[]
            {
                new Vector3(-8.0f, 0.02f, 16.0f),
                new Vector3(8.0f, 0.02f, 16.0f),
                new Vector3(-8.0f, 0.02f, 4.0f),
                new Vector3(8.0f, 0.02f, 4.0f),
                new Vector3(-20.0f, 0.02f, 10.0f),
                new Vector3(20.0f, 0.02f, 10.0f)
            };
            foreach (var lPos in lampPos)
            {
                BuildGardenLantern(gardenGroup, lPos, darkMat);
            }
        }

        private static void BuildCypressTree(Transform parent, Vector3 localPos)
        {
            GameObject tree = new GameObject("Cypress_Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = localPos;

            Material trunkMat = GetMaterial("CypressTrunkMat", new Color(0.35f, 0.24f, 0.15f), 0.0f, 0.2f);
            Material foliageMat = GetMaterial("CypressFoliageMat", new Color(0.12f, 0.38f, 0.16f), 0.0f, 0.15f);

            // Gövde
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
            Object.Destroy(trunk.GetComponent<Collider>());

            // İnce Uzun Servi Tacı (2 Kademeli Koni/Elips)
            GameObject foliage1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage1.name = "Foliage_Lower";
            foliage1.transform.SetParent(tree.transform, false);
            foliage1.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            foliage1.transform.localScale = new Vector3(1.6f, 4.5f, 1.6f);
            foliage1.GetComponent<Renderer>().sharedMaterial = foliageMat;
            Object.Destroy(foliage1.GetComponent<Collider>());

            GameObject foliage2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage2.name = "Foliage_Top";
            foliage2.transform.SetParent(tree.transform, false);
            foliage2.transform.localPosition = new Vector3(0f, 6.2f, 0f);
            foliage2.transform.localScale = new Vector3(1.0f, 3.0f, 1.0f);
            foliage2.GetComponent<Renderer>().sharedMaterial = foliageMat;
            Object.Destroy(foliage2.GetComponent<Collider>());
        }

        #endregion

        #region 3. Three Single-Story Cafes (Doğu Caddesine Bakan 3 Kafe)

        private static void BuildThreeSingleStoryCafes(Transform parent, Vector3 parcelCenter)
        {
            GameObject cafeParcel = new GameObject("Parcel_Three_Single_Story_Cafes");
            cafeParcel.transform.SetParent(parent, false);
            cafeParcel.transform.position = parcelCenter;

            // 1. KUZEY KAFESİ: 🌿 BOTANİK & BAHÇE KAFE (Z = +22.0m)
            BuildBotanicGardenCafe(cafeParcel.transform, new Vector3(0f, 0f, 22.0f));

            // 2. ORTA KAFE: ☕ NOSTALJİ KİTAP & KAHVE EVİ (Z = 0.0m)
            BuildNostalgiaBookCafe(cafeParcel.transform, new Vector3(0f, 0f, 0.0f));

            // 3. GÜNEY KAFESİ: 🍰 ÇİFTLİK PATISSERIE & BISTRO (Z = -22.0m)
            BuildFarmhousePatisserieCafe(cafeParcel.transform, new Vector3(0f, 0f, -22.0f));
        }

        // --- KAFE 1: BOTANİK KAFE ---
        private static void BuildBotanicGardenCafe(Transform parent, Vector3 localPos)
        {
            GameObject cafeRoot = new GameObject("Cafe_1_Botanic_Garden");
            cafeRoot.transform.SetParent(parent, false);
            cafeRoot.transform.localPosition = localPos;

            Material wallMat = GetMaterial("BotanicCafeWallMat", new Color(0.78f, 0.85f, 0.78f), 0.05f, 0.40f); // Adaçayı Yeşili
            Material woodDeckMat = GetMaterial("BotanicDeckMat", new Color(0.82f, 0.65f, 0.45f), 0.05f, 0.35f);
            Material glassMat = GetMaterial("BotanicGlassMat", new Color(0.35f, 0.75f, 0.90f, 0.85f), 0.1f, 0.8f);
            Material frameMat = GetMaterial("BotanicFrameMat", new Color(0.20f, 0.28f, 0.22f), 0.3f, 0.5f);
            Material roofMat = GetMaterial("BotanicRoofMat", new Color(0.25f, 0.35f, 0.28f), 0.1f, 0.3f);
            Material signNeonMat = GetMaterial("BotanicSignNeonMat", new Color(0.35f, 0.95f, 0.45f), 0.2f, 0.8f, true, new Color(0.30f, 0.95f, 0.40f) * 2.8f);

            // A) Parsel Zemin & Ahşap Veranda (X: -32m ile +32m, Z: -10m ile +10m)
            // Bina sol tarafta (X: -14m), Açık Hava Verandası sağ tarafta (X: +10m - Doğu Caddesi Önü)
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Outdoor_Patio_Deck";
            deck.transform.SetParent(cafeRoot.transform, false);
            deck.transform.localPosition = new Vector3(10.0f, 0.02f, 0f);
            deck.transform.localScale = new Vector3(26.0f, 0.06f, 18.0f);
            deck.GetComponent<Renderer>().sharedMaterial = woodDeckMat;

            // B) Tek Katlı Kafe Binası (X: -14.0m, Genişlik: 18m, Derinlik: 15m, Yükseklik: 4.2m)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Building_Structure";
            building.transform.SetParent(cafeRoot.transform, false);
            building.transform.localPosition = new Vector3(-14.0f, 2.10f, 0f);
            building.transform.localScale = new Vector3(18.0f, 4.20f, 15.0f);
            building.GetComponent<Renderer>().sharedMaterial = wallMat;

            // C) Eğimli Modern Çatı
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Modern_Roof";
            roof.transform.SetParent(cafeRoot.transform, false);
            roof.transform.localPosition = new Vector3(-14.0f, 4.45f, 0f);
            roof.transform.localScale = new Vector3(19.2f, 0.50f, 16.2f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMat;

            // D) DOĞU CEPHE VİTRİN CAMLARI VE ÇİFT CAM KAPI (Doğuya / Yola Bakan Yüz X: -5.0m)
            BuildEastFacingStorefront(cafeRoot.transform, new Vector3(-5.0f, 1.8f, 0f), 13.0f, glassMat, frameMat);

            // E) DOĞU CEPHE ŞIK IŞIKLI TABELA
            BuildGlowingCafeSign(cafeRoot.transform, new Vector3(-4.85f, 3.7f, 0f), "🌿 BOTANİK KAFE", signNeonMat, new Color(0.35f, 0.95f, 0.45f));

            // F) AÇIK HAVA MASALARI, SANDALYELER VE ŞEMSİYELER (Doğu Verandasında)
            BuildOutdoorCafeTables(cafeRoot.transform, new Vector3(10.0f, 0.05f, 0f), new Color(0.25f, 0.65f, 0.35f), 4);

            // G) ÇEVRE AHŞAP ÇİT VE DOĞU YOLUNA BAĞLANTI KAPISI (X: +32m'ye açılan kapı)
            BuildPerimeterFenceWithEastGate(cafeRoot.transform, 19.0f, new Color(0.85f, 0.70f, 0.50f));

            // H) GECE İÇ VE DIŞ AYDINLATMA
            BuildCafeLighting(cafeRoot.transform, new Vector3(-14.0f, 3.2f, 0f), new Vector3(10.0f, 3.2f, 0f), new Color(0.85f, 1.0f, 0.75f));

            // NavMesh Obstacle
            AddBuildingPhysicsAndObstacle(building, new Vector3(19.0f, 5.0f, 16.0f));
        }

        // --- KAFE 2: NOSTALJİ KİTAP & KAHVE ---
        private static void BuildNostalgiaBookCafe(Transform parent, Vector3 localPos)
        {
            GameObject cafeRoot = new GameObject("Cafe_2_Nostalgia_Book_Coffee");
            cafeRoot.transform.SetParent(parent, false);
            cafeRoot.transform.localPosition = localPos;

            Material wallMat = GetMaterial("NostalgiaBrickWallMat", new Color(0.72f, 0.38f, 0.28f), 0.05f, 0.30f); // Sıcak Tuğla
            Material patioMat = GetMaterial("NostalgiaCobbleMat", new Color(0.60f, 0.62f, 0.65f), 0.05f, 0.40f);
            Material glassMat = GetMaterial("NostalgiaGlassMat", new Color(0.40f, 0.70f, 0.85f, 0.85f), 0.1f, 0.8f);
            Material frameMat = GetMaterial("NostalgiaWoodFrameMat", new Color(0.28f, 0.18f, 0.12f), 0.1f, 0.3f);
            Material roofMat = GetMaterial("NostalgiaRoofMat", new Color(0.22f, 0.26f, 0.32f), 0.1f, 0.3f);
            Material signNeonMat = GetMaterial("NostalgiaSignNeonMat", new Color(1.0f, 0.82f, 0.35f), 0.2f, 0.8f, true, new Color(1.0f, 0.80f, 0.30f) * 2.8f);

            // A) Taş Teras Tabanı
            GameObject patio = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patio.name = "Outdoor_Patio_Cobble";
            patio.transform.SetParent(cafeRoot.transform, false);
            patio.transform.localPosition = new Vector3(10.0f, 0.02f, 0f);
            patio.transform.localScale = new Vector3(26.0f, 0.06f, 18.0f);
            patio.GetComponent<Renderer>().sharedMaterial = patioMat;

            // B) Tek Katlı Kafe Binası
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Building_Structure";
            building.transform.SetParent(cafeRoot.transform, false);
            building.transform.localPosition = new Vector3(-14.0f, 2.10f, 0f);
            building.transform.localScale = new Vector3(18.0f, 4.20f, 15.0f);
            building.GetComponent<Renderer>().sharedMaterial = wallMat;

            // C) Çatı
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Classic_Mansard_Roof";
            roof.transform.SetParent(cafeRoot.transform, false);
            roof.transform.localPosition = new Vector3(-14.0f, 4.45f, 0f);
            roof.transform.localScale = new Vector3(19.2f, 0.50f, 16.2f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMat;

            // D) DOĞU CEPHE VİTRİN CAMLARI VE KAPI
            BuildEastFacingStorefront(cafeRoot.transform, new Vector3(-5.0f, 1.8f, 0f), 13.0f, glassMat, frameMat);

            // Koyu Lacivert & Hardal Tente
            BuildCafeAwning(cafeRoot.transform, new Vector3(-4.6f, 3.2f, 0f), 13.5f, new Color(0.18f, 0.25f, 0.42f));

            // E) DOĞU CEPHE ŞIK IŞIKLI TABELA
            BuildGlowingCafeSign(cafeRoot.transform, new Vector3(-4.85f, 3.8f, 0f), "☕ NOSTALJİ KAHVE", signNeonMat, new Color(1.0f, 0.85f, 0.35f));

            // F) AÇIK HAVA BİSTRO MASALARI (Ferforje)
            BuildOutdoorCafeTables(cafeRoot.transform, new Vector3(10.0f, 0.05f, 0f), new Color(0.18f, 0.25f, 0.42f), 4);

            // G) FERFORJE ÇİT VE DOĞU YOLUNA BAĞLANTI KAPISI
            BuildPerimeterFenceWithEastGate(cafeRoot.transform, 19.0f, new Color(0.25f, 0.26f, 0.28f));

            // H) GECE AYDINLATMASI
            BuildCafeLighting(cafeRoot.transform, new Vector3(-14.0f, 3.2f, 0f), new Vector3(10.0f, 3.2f, 0f), new Color(1.0f, 0.88f, 0.60f));

            AddBuildingPhysicsAndObstacle(building, new Vector3(19.0f, 5.0f, 16.0f));
        }

        // --- KAFE 3: ÇİFTLİK PATISSERIE & BISTRO ---
        private static void BuildFarmhousePatisserieCafe(Transform parent, Vector3 localPos)
        {
            GameObject cafeRoot = new GameObject("Cafe_3_Farmhouse_Patisserie");
            cafeRoot.transform.SetParent(parent, false);
            cafeRoot.transform.localPosition = localPos;

            Material wallMat = GetMaterial("PatisserieCreamWallMat", new Color(0.95f, 0.92f, 0.84f), 0.05f, 0.40f); // Krem & Vanilya
            Material patioMat = GetMaterial("PatisserieTileMat", new Color(0.90f, 0.84f, 0.78f), 0.05f, 0.50f);
            Material glassMat = GetMaterial("PatisserieGlassMat", new Color(0.45f, 0.75f, 0.90f, 0.85f), 0.1f, 0.8f);
            Material frameMat = GetMaterial("PatisserieWhiteFrameMat", new Color(0.98f, 0.98f, 0.98f), 0.1f, 0.5f);
            Material roofMat = GetMaterial("PatisserieTerracottaRoofMat", new Color(0.78f, 0.42f, 0.30f), 0.05f, 0.35f);
            Material signNeonMat = GetMaterial("PatisserieSignNeonMat", new Color(1.0f, 0.55f, 0.75f), 0.2f, 0.8f, true, new Color(1.0f, 0.50f, 0.70f) * 2.8f);

            // A) Taş Veranda Tabanı
            GameObject patio = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patio.name = "Outdoor_Patio_Tile";
            patio.transform.SetParent(cafeRoot.transform, false);
            patio.transform.localPosition = new Vector3(10.0f, 0.02f, 0f);
            patio.transform.localScale = new Vector3(26.0f, 0.06f, 18.0f);
            patio.GetComponent<Renderer>().sharedMaterial = patioMat;

            // B) Tek Katlı Kafe Binası
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Building_Structure";
            building.transform.SetParent(cafeRoot.transform, false);
            building.transform.localPosition = new Vector3(-14.0f, 2.10f, 0f);
            building.transform.localScale = new Vector3(18.0f, 4.20f, 15.0f);
            building.GetComponent<Renderer>().sharedMaterial = wallMat;

            // C) Kiremit Çatı
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Terracotta_Roof";
            roof.transform.SetParent(cafeRoot.transform, false);
            roof.transform.localPosition = new Vector3(-14.0f, 4.45f, 0f);
            roof.transform.localScale = new Vector3(19.2f, 0.50f, 16.2f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMat;

            // D) DOĞU CEPHE VİTRİN CAMLARI VE KAPI
            BuildEastFacingStorefront(cafeRoot.transform, new Vector3(-5.0f, 1.8f, 0f), 13.0f, glassMat, frameMat);

            // Pembe & Krem Çizgili Tente
            BuildCafeAwning(cafeRoot.transform, new Vector3(-4.6f, 3.2f, 0f), 13.5f, new Color(0.92f, 0.45f, 0.60f));

            // E) DOĞU CEPHE ŞIK IŞIKLI TABELA
            BuildGlowingCafeSign(cafeRoot.transform, new Vector3(-4.85f, 3.8f, 0f), "🍰 ÇİFTLİK BİSTRO", signNeonMat, new Color(1.0f, 0.60f, 0.80f));

            // F) AÇIK HAVA BİSTRO MASALARI (Pembe Şemsiyeli)
            BuildOutdoorCafeTables(cafeRoot.transform, new Vector3(10.0f, 0.05f, 0f), new Color(0.92f, 0.45f, 0.60f), 4);

            // G) BEYAZ ÇİT VE DOĞU YOLUNA BAĞLANTI KAPISI
            BuildPerimeterFenceWithEastGate(cafeRoot.transform, 19.0f, new Color(0.95f, 0.95f, 0.95f));

            // H) GECE AYDINLATMASI
            BuildCafeLighting(cafeRoot.transform, new Vector3(-14.0f, 3.2f, 0f), new Vector3(10.0f, 3.2f, 0f), new Color(1.0f, 0.90f, 0.75f));

            AddBuildingPhysicsAndObstacle(building, new Vector3(19.0f, 5.0f, 16.0f));
        }

        #endregion

        #region Helper Builders (Vitrinler, Tabelalar, Masalar, Çitler, Işıklar)

        private static void BuildEastFacingStorefront(Transform parent, Vector3 localPos, float width, Material glassMat, Material frameMat)
        {
            GameObject storefront = new GameObject("East_Facing_Storefront");
            storefront.transform.SetParent(parent, false);
            storefront.transform.localPosition = localPos;

            // Çift Camlı Giriş Kapısı (Merkezde)
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Door_Frame";
            doorFrame.transform.SetParent(storefront.transform, false);
            doorFrame.transform.localPosition = new Vector3(0f, 0f, 0f);
            doorFrame.transform.localScale = new Vector3(0.20f, 2.6f, 2.2f);
            doorFrame.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(doorFrame.GetComponent<Collider>());

            GameObject doorGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorGlass.name = "Door_Glass";
            doorGlass.transform.SetParent(doorFrame.transform, false);
            doorGlass.transform.localPosition = new Vector3(0.02f, 0f, 0f);
            doorGlass.transform.localScale = new Vector3(0.20f, 0.90f, 0.85f);
            doorGlass.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(doorGlass.GetComponent<Collider>());

            // Sol ve Sağ Geniş Vitrin Camları
            float winWidth = (width - 3.0f) / 2.0f;
            float[] offsetsZ = new float[] { -winWidth / 2f - 1.5f, winWidth / 2f + 1.5f };
            foreach (float oz in offsetsZ)
            {
                GameObject winFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                winFrame.name = "Window_Frame";
                winFrame.transform.SetParent(storefront.transform, false);
                winFrame.transform.localPosition = new Vector3(0f, 0.15f, oz);
                winFrame.transform.localScale = new Vector3(0.18f, 2.3f, winWidth);
                winFrame.GetComponent<Renderer>().sharedMaterial = frameMat;
                Object.Destroy(winFrame.GetComponent<Collider>());

                GameObject winGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                winGlass.name = "Window_Glass";
                winGlass.transform.SetParent(winFrame.transform, false);
                winGlass.transform.localPosition = new Vector3(0.02f, 0f, 0f);
                winGlass.transform.localScale = new Vector3(0.20f, 0.88f, 0.92f);
                winGlass.GetComponent<Renderer>().sharedMaterial = glassMat;
                Object.Destroy(winGlass.GetComponent<Collider>());

                if (DayNightCycleManager.Instance != null)
                {
                    DayNightCycleManager.Instance.RegisterBuildingWindow(winGlass);
                }
            }
        }

        private static void BuildCafeAwning(Transform parent, Vector3 localPos, float width, Color color)
        {
            GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awning.name = "Cafe_Fabric_Awning";
            awning.transform.SetParent(parent, false);
            awning.transform.localPosition = localPos;
            awning.transform.localScale = new Vector3(1.6f, 0.20f, width);
            awning.transform.localRotation = Quaternion.Euler(0f, 0f, -18f); // Dışa eğimli
            awning.GetComponent<Renderer>().sharedMaterial = GetMaterial($"AwningMat_{color.r:F2}_{color.g:F2}", color, 0.0f, 0.2f);
            Object.Destroy(awning.GetComponent<Collider>());
        }

        private static void BuildGlowingCafeSign(Transform parent, Vector3 localPos, string text, Material neonMat, Color textColor)
        {
            GameObject signRoot = new GameObject("Glowing_Cafe_Sign_3D");
            signRoot.transform.SetParent(parent, false);
            signRoot.transform.localPosition = localPos;

            // Arka Işıklı Tabela Panosu
            GameObject signBacking = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBacking.name = "Sign_Backing_Board";
            signBacking.transform.SetParent(signRoot.transform, false);
            signBacking.transform.localPosition = new Vector3(0.05f, 0f, 0f);
            signBacking.transform.localScale = new Vector3(0.12f, 1.10f, 8.5f);
            signBacking.GetComponent<Renderer>().sharedMaterial = GetMaterial("SignBackingDarkMat", new Color(0.12f, 0.14f, 0.16f), 0.4f, 0.5f);
            Object.Destroy(signBacking.GetComponent<Collider>());

            // Parlayan Neon Kenarlık Çerçevesi
            GameObject neonBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonBorder.name = "Neon_Border_Frame";
            neonBorder.transform.SetParent(signRoot.transform, false);
            neonBorder.transform.localPosition = new Vector3(0.12f, 0f, 0f);
            neonBorder.transform.localScale = new Vector3(0.08f, 1.18f, 8.6f);
            neonBorder.GetComponent<Renderer>().sharedMaterial = neonMat;
            Object.Destroy(neonBorder.GetComponent<Collider>());

            // 3D Yazı Metni (Doğu Yönüne +X Bakar)
            GameObject labelObj = new GameObject("Sign_Text");
            labelObj.transform.SetParent(signRoot.transform, false);
            labelObj.transform.localPosition = new Vector3(0.20f, 0f, 0f);
            labelObj.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // Doğuya bakar

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 32;
            tm.characterSize = 0.085f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = textColor;
            tm.fontStyle = FontStyle.Bold;

            // Gece Tabelayı Aydınlatan LED Spot
            GameObject spotObj = new GameObject("Sign_SpotLight");
            spotObj.transform.SetParent(signRoot.transform, false);
            spotObj.transform.localPosition = new Vector3(1.2f, 0f, 0f);
            Light sLight = spotObj.AddComponent<Light>();
            sLight.type = LightType.Point;
            sLight.color = textColor;
            sLight.intensity = 2.4f;
            sLight.range = 6.5f;
            sLight.shadows = LightShadows.None;
            sLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(sLight);
            }
        }

        private static void BuildOutdoorCafeTables(Transform parent, Vector3 patioCenter, Color umbrellaColor, int count)
        {
            Transform tablesGroup = new GameObject("Outdoor_Seating_Tables").transform;
            tablesGroup.SetParent(parent, false);
            tablesGroup.transform.localPosition = patioCenter;

            Material woodMat = GetMaterial("CafeTableWoodMat", new Color(0.45f, 0.30f, 0.18f), 0.1f, 0.35f);
            Material metalMat = GetMaterial("CafeChairMetalMat", new Color(0.18f, 0.20f, 0.22f), 0.7f, 0.6f);
            Material umbrellaMat = GetMaterial($"UmbrellaMat_{umbrellaColor.r:F2}", umbrellaColor, 0.0f, 0.3f);

            Vector3[] tableOffsets = new Vector3[]
            {
                new Vector3(-6.0f, 0f, 4.5f),
                new Vector3(6.0f, 0f, 4.5f),
                new Vector3(-6.0f, 0f, -4.5f),
                new Vector3(6.0f, 0f, -4.5f)
            };

            for (int i = 0; i < Mathf.Min(count, tableOffsets.Length); i++)
            {
                Vector3 tPos = tableOffsets[i];
                GameObject setObj = new GameObject($"Table_Set_{i + 1}");
                setObj.transform.SetParent(tablesGroup, false);
                setObj.transform.localPosition = tPos;

                // Masa Tablası
                GameObject tableTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tableTop.name = "Table_Top";
                tableTop.transform.SetParent(setObj.transform, false);
                tableTop.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                tableTop.transform.localScale = new Vector3(1.5f, 0.06f, 1.5f);
                tableTop.GetComponent<Renderer>().sharedMaterial = woodMat;
                Object.Destroy(tableTop.GetComponent<Collider>());

                // Masa Ayağı
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Table_Leg";
                leg.transform.SetParent(setObj.transform, false);
                leg.transform.localPosition = new Vector3(0f, 0.37f, 0f);
                leg.transform.localScale = new Vector3(0.12f, 0.37f, 0.12f);
                leg.GetComponent<Renderer>().sharedMaterial = metalMat;
                Object.Destroy(leg.GetComponent<Collider>());

                // Güneş Şemsiyesi (Parasol)
                GameObject umbrellaPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                umbrellaPole.name = "Umbrella_Pole";
                umbrellaPole.transform.SetParent(setObj.transform, false);
                umbrellaPole.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                umbrellaPole.transform.localScale = new Vector3(0.06f, 1.4f, 0.06f);
                umbrellaPole.GetComponent<Renderer>().sharedMaterial = metalMat;
                Object.Destroy(umbrellaPole.GetComponent<Collider>());

                GameObject umbrellaCanopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                umbrellaCanopy.name = "Umbrella_Canopy";
                umbrellaCanopy.transform.SetParent(setObj.transform, false);
                umbrellaCanopy.transform.localPosition = new Vector3(0f, 2.45f, 0f);
                umbrellaCanopy.transform.localScale = new Vector3(2.6f, 0.60f, 2.6f);
                umbrellaCanopy.GetComponent<Renderer>().sharedMaterial = umbrellaMat;
                Object.Destroy(umbrellaCanopy.GetComponent<Collider>());

                // 3 Adet Sandalye (Masayı Çevreleyen)
                for (int c = 0; c < 3; c++)
                {
                    float angle = c * (Mathf.PI * 2f / 3f);
                    float cx = Mathf.Cos(angle) * 1.1f;
                    float cz = Mathf.Sin(angle) * 1.1f;

                    GameObject chair = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chair.name = $"Chair_{c + 1}";
                    chair.transform.SetParent(setObj.transform, false);
                    chair.transform.localPosition = new Vector3(cx, 0.45f, cz);
                    chair.transform.localScale = new Vector3(0.48f, 0.45f, 0.48f);
                    chair.GetComponent<Renderer>().sharedMaterial = woodMat;
                    chair.transform.localRotation = Quaternion.LookRotation(new Vector3(-cx, 0f, -cz));
                    Object.Destroy(chair.GetComponent<Collider>());
                }
            }
        }

        private static void BuildPerimeterFenceWithEastGate(Transform parent, float lotDepth, Color fenceColor)
        {
            Transform fenceGroup = new GameObject("Cafe_Perimeter_Fence").transform;
            fenceGroup.SetParent(parent, false);

            Material fenceMat = GetMaterial($"FenceColorMat_{fenceColor.r:F2}", fenceColor, 0.1f, 0.35f);
            float halfD = lotDepth / 2f;

            // Kuzey ve Güney Sınır Çitleri (X: -22m ile +23m arası)
            GameObject northFence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northFence.name = "Fence_North";
            northFence.transform.SetParent(fenceGroup, false);
            northFence.transform.localPosition = new Vector3(0.5f, 0.55f, halfD);
            northFence.transform.localScale = new Vector3(45.0f, 1.10f, 0.15f);
            northFence.GetComponent<Renderer>().sharedMaterial = fenceMat;
            Object.Destroy(northFence.GetComponent<Collider>());

            GameObject southFence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            southFence.name = "Fence_South";
            southFence.transform.SetParent(fenceGroup, false);
            southFence.transform.localPosition = new Vector3(0.5f, 0.55f, -halfD);
            southFence.transform.localScale = new Vector3(45.0f, 1.10f, 0.15f);
            southFence.GetComponent<Renderer>().sharedMaterial = fenceMat;
            Object.Destroy(southFence.GetComponent<Collider>());

            // Doğu Sınır Çiti (X = +23.0m) - Ortasında 3.5 Metrelik Kaldırıma Açılan Kapı Boşluğu Bırakılır!
            float sideSegD = (lotDepth - 4.0f) / 2.0f;

            GameObject eastFenceTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastFenceTop.name = "Fence_East_Top";
            eastFenceTop.transform.SetParent(fenceGroup, false);
            eastFenceTop.transform.localPosition = new Vector3(23.0f, 0.55f, halfD - (sideSegD / 2f));
            eastFenceTop.transform.localScale = new Vector3(0.15f, 1.10f, sideSegD);
            eastFenceTop.GetComponent<Renderer>().sharedMaterial = fenceMat;
            Object.Destroy(eastFenceTop.GetComponent<Collider>());

            GameObject eastFenceBottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastFenceBottom.name = "Fence_East_Bottom";
            eastFenceBottom.transform.SetParent(fenceGroup, false);
            eastFenceBottom.transform.localPosition = new Vector3(23.0f, 0.55f, -halfD + (sideSegD / 2f));
            eastFenceBottom.transform.localScale = new Vector3(0.15f, 1.10f, sideSegD);
            eastFenceBottom.GetComponent<Renderer>().sharedMaterial = fenceMat;
            Object.Destroy(eastFenceBottom.GetComponent<Collider>());

            // Kapı Yanı Dekoratif Giriş Sütunları
            GameObject post1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post1.name = "Gate_Post_1";
            post1.transform.SetParent(fenceGroup, false);
            post1.transform.localPosition = new Vector3(23.0f, 0.90f, 2.0f);
            post1.transform.localScale = new Vector3(0.35f, 0.90f, 0.35f);
            post1.GetComponent<Renderer>().sharedMaterial = fenceMat;
            Object.Destroy(post1.GetComponent<Collider>());

            GameObject post2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post2.name = "Gate_Post_2";
            post2.transform.SetParent(fenceGroup, false);
            post2.transform.localPosition = new Vector3(23.0f, 0.90f, -2.0f);
            post2.transform.localScale = new Vector3(0.35f, 0.90f, 0.35f);
            post2.GetComponent<Renderer>().sharedMaterial = fenceMat;
            Object.Destroy(post2.GetComponent<Collider>());

            // Kapıdan Doğu Kaldırımına Bağlantı Yolu (X: 23m -> 33m)
            GameObject walkwayToEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walkwayToEast.name = "Path_To_East_Sidewalk";
            walkwayToEast.transform.SetParent(fenceGroup, false);
            walkwayToEast.transform.localPosition = new Vector3(28.0f, 0.02f, 0f);
            walkwayToEast.transform.localScale = new Vector3(10.0f, 0.04f, 3.6f);
            walkwayToEast.GetComponent<Renderer>().sharedMaterial = GetMaterial("CafeEastWalkwayMat", new Color(0.75f, 0.72f, 0.68f), 0.05f, 0.45f);
        }

        private static void BuildCafeLighting(Transform parent, Vector3 indoorPos, Vector3 outdoorPos, Color lightColor)
        {
            // İç Mekan Işığı
            GameObject inLightObj = new GameObject("Cafe_Indoor_Warm_Light");
            inLightObj.transform.SetParent(parent, false);
            inLightObj.transform.localPosition = indoorPos;
            Light inLight = inLightObj.AddComponent<Light>();
            inLight.type = LightType.Point;
            inLight.color = lightColor;
            inLight.intensity = 2.6f;
            inLight.range = 14.0f;
            inLight.shadows = LightShadows.None;
            inLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(inLight);
            }

            // Açık Hava Teras Işığı
            GameObject outLightObj = new GameObject("Cafe_Outdoor_Warm_Light");
            outLightObj.transform.SetParent(parent, false);
            outLightObj.transform.localPosition = outdoorPos;
            Light outLight = outLightObj.AddComponent<Light>();
            outLight.type = LightType.Point;
            outLight.color = new Color(1.0f, 0.88f, 0.65f);
            outLight.intensity = 2.2f;
            outLight.range = 12.0f;
            outLight.shadows = LightShadows.None;
            outLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(outLight);
            }
        }

        private static void AddBuildingPhysicsAndObstacle(GameObject buildingObj, Vector3 size)
        {
            if (buildingObj == null) return;

            BoxCollider col = buildingObj.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = buildingObj.AddComponent<BoxCollider>();
            }
            col.center = Vector3.zero;
            col.size = Vector3.one;

            NavMeshObstacle obs = buildingObj.GetComponent<NavMeshObstacle>();
            if (obs == null)
            {
                obs = buildingObj.AddComponent<NavMeshObstacle>();
            }
            obs.shape = NavMeshObstacleShape.Box;
            obs.center = Vector3.zero;
            obs.size = Vector3.one;
            obs.carving = true;
            obs.carveOnlyStationary = true;
        }

        private static void BuildStreetLamp(Transform parent, Vector3 localPos, bool faceRight, Material metalMat)
        {
            GameObject lamp = new GameObject("Street_Lamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = localPos;

            float xDir = faceRight ? 1f : -1f;

            // Direk
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(lamp.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            pole.transform.localScale = new Vector3(0.12f, 1.8f, 0.12f);
            pole.GetComponent<Renderer>().sharedMaterial = metalMat;
            Object.Destroy(pole.GetComponent<Collider>());

            // Kol
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(lamp.transform, false);
            arm.transform.localPosition = new Vector3(xDir * 0.4f, 3.5f, 0f);
            arm.transform.localScale = new Vector3(0.8f, 0.08f, 0.08f);
            arm.GetComponent<Renderer>().sharedMaterial = metalMat;
            Object.Destroy(arm.GetComponent<Collider>());

            // Ampul
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(lamp.transform, false);
            bulb.transform.localPosition = new Vector3(xDir * 0.75f, 3.4f, 0f);
            bulb.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            bulb.GetComponent<Renderer>().sharedMaterial = GetMaterial("LampBulbOffMat", new Color(0.4f, 0.4f, 0.4f), 0f, 0.5f);
            Object.Destroy(bulb.GetComponent<Collider>());

            // Işık
            GameObject lightObj = new GameObject("Point_Light");
            lightObj.transform.SetParent(bulb.transform, false);
            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.88f, 0.55f);
            pLight.intensity = 2.4f;
            pLight.range = 14.0f;
            pLight.shadows = LightShadows.None;
            pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStreetLamp(bulb, pLight);
            }
        }

        private static void BuildGardenLantern(Transform parent, Vector3 localPos, Material darkMat)
        {
            GameObject lantern = new GameObject("Garden_Lantern");
            lantern.transform.SetParent(parent, false);
            lantern.transform.localPosition = localPos;

            // Direk
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Lantern_Post";
            post.transform.SetParent(lantern.transform, false);
            post.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            post.transform.localScale = new Vector3(0.12f, 1.1f, 0.12f);
            post.GetComponent<Renderer>().sharedMaterial = darkMat;
            Object.Destroy(post.GetComponent<Collider>());

            // Fener Başlığı
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Lantern_Cap";
            cap.transform.SetParent(lantern.transform, false);
            cap.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            cap.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
            cap.GetComponent<Renderer>().sharedMaterial = darkMat;
            Object.Destroy(cap.GetComponent<Collider>());

            // Ampul & Işık
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(cap.transform, false);
            bulb.transform.localPosition = Vector3.zero;
            bulb.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            bulb.GetComponent<Renderer>().sharedMaterial = GetMaterial("LanternBulbMat", new Color(1.0f, 0.90f, 0.50f), 0f, 0.5f);
            Object.Destroy(bulb.GetComponent<Collider>());

            GameObject lObj = new GameObject("Light");
            lObj.transform.SetParent(bulb.transform, false);
            Light pLight = lObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.88f, 0.55f);
            pLight.intensity = 1.3f;
            pLight.range = 5.5f;
            pLight.shadows = LightShadows.None;
            pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStreetLamp(bulb, pLight);
            }
        }

        private static void BuildGardenBench(Transform parent, Vector3 localPos)
        {
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Garden_Bench";
            bench.transform.SetParent(parent, false);
            bench.transform.localPosition = localPos + new Vector3(0f, 0.35f, 0f);
            bench.transform.localScale = new Vector3(2.4f, 0.45f, 0.70f);
            bench.GetComponent<Renderer>().sharedMaterial = GetMaterial("BenchWoodMat", new Color(0.48f, 0.30f, 0.16f), 0.1f, 0.35f);
            Object.Destroy(bench.GetComponent<Collider>());
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
