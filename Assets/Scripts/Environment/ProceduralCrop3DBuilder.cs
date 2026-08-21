using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    public static class ProceduralCrop3DBuilder
    {
        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        private static Material GetMaterial(string key, Color color, float metallic = 0.0f, float smoothness = 0.4f)
        {
            if (materialCache.TryGetValue(key, out Material mat) && mat != null)
            {
                return mat;
            }

            Shader shader = ShaderHelper.GetLitShader() 
                ?? Shader.Find("Universal Render Pipeline/Lit") 
                ?? Shader.Find("Lightweight Render Pipeline/Lit") 
                ?? Shader.Find("Standard");
            if (shader == null) return null;

            mat = new Material(shader)
            {
                name = "CropMat_" + key,
                color = color
            };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            materialCache[key] = mat;
            return mat;
        }

        public static void BuildCrop3D(Transform parent, string seedId, PlotState state)
        {
            if (parent == null || state == PlotState.Empty) return;

            GardenSeedDef sDef = GardenSeedDatabase.GetSeedById(seedId);
            Color cropCol = (sDef != null) ? sDef.cropColor : Color.green;

            // Dört İç Tarla Noktası (Tarlanın sınırları içinde, kenarlardan güvenli mesafede)
            Vector3[] plotSpots = new Vector3[]
            {
                new Vector3(-0.35f, 0f, -0.35f),
                new Vector3( 0.35f, 0f, -0.35f),
                new Vector3(-0.35f, 0f,  0.35f),
                new Vector3( 0.35f, 0f,  0.35f)
            };

            switch (state)
            {
                case PlotState.PlantedSprout:
                    BuildStage1Sprouts(parent, seedId, cropCol, plotSpots);
                    break;

                case PlotState.Growing:
                    BuildStage2Growing(parent, seedId, cropCol, plotSpots);
                    break;

                case PlotState.RipeReadyToHarvest:
                    BuildStage3Ripe(parent, seedId, cropCol, plotSpots);
                    break;

                case PlotState.SpoiledTrash:
                    BuildSpoiledTrash(parent, plotSpots);
                    break;
            }

            // Canlı Doğa Rüzgar Animasyonu (Tarlaya özel ölçeği korur)
            CropSwayAnimation anim = parent.GetComponent<CropSwayAnimation>();
            if (anim == null)
            {
                anim = parent.gameObject.AddComponent<CropSwayAnimation>();
            }
            anim.SetTargetScale(parent.localScale);
        }

        #region Stage 1: Çimlenme & Filiz (PlantedSprout)

        private static void BuildStage1Sprouts(Transform parent, string seedId, Color cropCol, Vector3[] spots)
        {
            Material stemMat = GetMaterial("Sprout_Stem", new Color(0.38f, 0.82f, 0.28f));
            Material leafMat = GetMaterial("Sprout_Leaf_" + seedId, Color.Lerp(new Color(0.40f, 0.88f, 0.30f), cropCol, 0.20f));
            Material moundMat = GetMaterial("Soil_Mound", new Color(0.28f, 0.18f, 0.10f));

            foreach (var spot in spots)
            {
                // Küçük Toprak Tepeciği (Tarlanın içinde derli toplu)
                GameObject mound = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mound.transform.SetParent(parent, false);
                mound.transform.localPosition = spot + new Vector3(0f, 0.03f, 0f);
                mound.transform.localScale = new Vector3(0.35f, 0.04f, 0.35f);
                if (moundMat != null) mound.GetComponent<Renderer>().sharedMaterial = moundMat;
                Object.Destroy(mound.GetComponent<Collider>());

                // 2 Minik Yükselen Filiz Sapı
                for (int s = 0; s < 2; s++)
                {
                    float offsetX = (s == 0 ? -0.06f : 0.06f);
                    float offsetZ = (s == 0 ? 0.04f : -0.04f);

                    GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stem.transform.SetParent(parent, false);
                    stem.transform.localPosition = spot + new Vector3(offsetX, 0.12f, offsetZ);
                    stem.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
                    stem.transform.localRotation = Quaternion.Euler(s == 0 ? -12f : 12f, s * 90f, 0f);
                    if (stemMat != null) stem.GetComponent<Renderer>().sharedMaterial = stemMat;
                    Object.Destroy(stem.GetComponent<Collider>());

                    // Kotiledon / Tohum Yaprakları
                    GameObject leaf1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leaf1.transform.SetParent(parent, false);
                    leaf1.transform.localPosition = spot + new Vector3(offsetX - 0.04f, 0.22f, offsetZ);
                    leaf1.transform.localScale = new Vector3(0.09f, 0.025f, 0.06f);
                    leaf1.transform.localRotation = Quaternion.Euler(-25f, 45f, 0f);
                    if (leafMat != null) leaf1.GetComponent<Renderer>().sharedMaterial = leafMat;
                    Object.Destroy(leaf1.GetComponent<Collider>());

                    GameObject leaf2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leaf2.transform.SetParent(parent, false);
                    leaf2.transform.localPosition = spot + new Vector3(offsetX + 0.04f, 0.22f, offsetZ);
                    leaf2.transform.localScale = new Vector3(0.09f, 0.025f, 0.06f);
                    leaf2.transform.localRotation = Quaternion.Euler(25f, -45f, 0f);
                    if (leafMat != null) leaf2.GetComponent<Renderer>().sharedMaterial = leafMat;
                    Object.Destroy(leaf2.GetComponent<Collider>());
                }
            }
        }

        #endregion

        #region Stage 2: Gelişme & Çiçeklenme (Growing)

        private static void BuildStage2Growing(Transform parent, string seedId, Color cropCol, Vector3[] spots)
        {
            Material bushMat = GetMaterial("Growing_Foliage", new Color(0.20f, 0.68f, 0.24f));
            Material stemMat = GetMaterial("Growing_Stem", new Color(0.32f, 0.58f, 0.18f));
            Material woodTrellisMat = GetMaterial("Wood_Trellis", new Color(0.55f, 0.38f, 0.20f));
            Material budMat = GetMaterial("Growing_Bud_" + seedId, Color.Lerp(Color.yellow, cropCol, 0.45f));

            bool isClimbingPlant = seedId.Contains("pea") || seedId.Contains("bean") || seedId.Contains("grape") || seedId.Contains("tomato");
            bool isTallStalk = seedId.Contains("corn") || seedId.Contains("sunflower") || seedId.Contains("asparagus");
            bool isRootVegetable = seedId.Contains("carrot") || seedId.Contains("potato") || seedId.Contains("radish") || seedId.Contains("beet") || seedId.Contains("turnip") || seedId.Contains("onion") || seedId.Contains("garlic");

            foreach (var spot in spots)
            {
                if (isClimbingPlant)
                {
                    // Sırık / Ahşap Kazık
                    GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pole.transform.SetParent(parent, false);
                    pole.transform.localPosition = spot + new Vector3(0f, 0.45f, 0f);
                    pole.transform.localScale = new Vector3(0.045f, 0.45f, 0.045f);
                    if (woodTrellisMat != null) pole.GetComponent<Renderer>().sharedMaterial = woodTrellisMat;
                    Object.Destroy(pole.GetComponent<Collider>());

                    // Tırmanan Yaprak Demetleri
                    for (int l = 0; l < 3; l++)
                    {
                        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        foliage.transform.SetParent(parent, false);
                        foliage.transform.localPosition = spot + new Vector3((l % 2 == 0 ? 0.08f : -0.08f), 0.22f + (l * 0.25f), (l == 1 ? 0.06f : -0.06f));
                        foliage.transform.localScale = new Vector3(0.26f, 0.22f, 0.26f);
                        if (bushMat != null) foliage.GetComponent<Renderer>().sharedMaterial = bushMat;
                        Object.Destroy(foliage.GetComponent<Collider>());
                    }
                }
                else if (isTallStalk)
                {
                    // Dikey Uzun Sap ve Geniş Yapraklar
                    GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stalk.transform.SetParent(parent, false);
                    stalk.transform.localPosition = spot + new Vector3(0f, 0.40f, 0f);
                    stalk.transform.localScale = new Vector3(0.06f, 0.40f, 0.06f);
                    if (stemMat != null) stalk.GetComponent<Renderer>().sharedMaterial = stemMat;
                    Object.Destroy(stalk.GetComponent<Collider>());

                    for (int a = 0; a < 4; a++)
                    {
                        float angle = a * 90f;
                        GameObject stalkLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        stalkLeaf.transform.SetParent(parent, false);
                        stalkLeaf.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.16f, 0.35f + (a * 0.08f), 0f);
                        stalkLeaf.transform.localScale = new Vector3(0.24f, 0.02f, 0.10f);
                        stalkLeaf.transform.localRotation = Quaternion.Euler(20f, angle, -25f);
                        if (bushMat != null) stalkLeaf.GetComponent<Renderer>().sharedMaterial = bushMat;
                        Object.Destroy(stalkLeaf.GetComponent<Collider>());
                    }
                }
                else if (isRootVegetable)
                {
                    // Kök Bitkileri: Topraktan Yükselen Gür Yaprak Rozeti
                    for (int r = 0; r < 5; r++)
                    {
                        float angle = r * 72f;
                        GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        leaf.transform.SetParent(parent, false);
                        leaf.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.14f, 0.20f, 0f);
                        leaf.transform.localScale = new Vector3(0.22f, 0.03f, 0.09f);
                        leaf.transform.localRotation = Quaternion.Euler(-35f, angle, 0f);
                        if (bushMat != null) leaf.GetComponent<Renderer>().sharedMaterial = bushMat;
                        Object.Destroy(leaf.GetComponent<Collider>());
                    }
                }
                else
                {
                    // Standart Çalı / Gövde Formasyonu (Biber, Patlıcan, Çilek, Lahana, Brokoli vb.)
                    GameObject centerBush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    centerBush.transform.SetParent(parent, false);
                    centerBush.transform.localPosition = spot + new Vector3(0f, 0.26f, 0f);
                    centerBush.transform.localScale = new Vector3(0.42f, 0.32f, 0.42f);
                    if (bushMat != null) centerBush.GetComponent<Renderer>().sharedMaterial = bushMat;
                    Object.Destroy(centerBush.GetComponent<Collider>());

                    // Çiçek Tomurcukları
                    for (int b = 0; b < 3; b++)
                    {
                        float bAngle = b * 120f;
                        GameObject bud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        bud.transform.SetParent(parent, false);
                        bud.transform.localPosition = spot + Quaternion.Euler(0f, bAngle, 0f) * new Vector3(0.16f, 0.32f, 0f);
                        bud.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
                        if (budMat != null) bud.GetComponent<Renderer>().sharedMaterial = budMat;
                        Object.Destroy(bud.GetComponent<Collider>());
                    }
                }
            }
        }

        #endregion

        #region Stage 3: Tam Olgunlaşma & Hasat Modelleri (RipeReadyToHarvest - 40 Tohum)

        private static void BuildStage3Ripe(Transform parent, string seedId, Color cropCol, Vector3[] spots)
        {
            Material foliageMat = GetMaterial("Ripe_Foliage", new Color(0.15f, 0.60f, 0.18f));
            Material darkFoliageMat = GetMaterial("Ripe_DarkFoliage", new Color(0.08f, 0.45f, 0.15f));
            Material stemMat = GetMaterial("Ripe_Stem", new Color(0.35f, 0.55f, 0.20f));
            Material woodMat = GetMaterial("Wood_Trellis", new Color(0.55f, 0.38f, 0.20f));
            Material fruitMat = GetMaterial("Fruit_" + seedId, cropCol, 0.0f, 0.6f);

            foreach (var spot in spots)
            {
                switch (seedId)
                {
                    // ==================== İLKBAHAR ====================
                    case "spring_tomato":
                        BuildTrellisWithFruits(parent, spot, woodMat, foliageMat, fruitMat, 4, PrimitiveType.Sphere, new Vector3(0.16f, 0.16f, 0.16f));
                        break;

                    case "spring_cucumber":
                        BuildBushWithCylinders(parent, spot, foliageMat, fruitMat, 3, new Vector3(0.10f, 0.32f, 0.10f), 35f);
                        break;

                    case "spring_lettuce":
                        BuildLayeredCabbageOrLettuce(parent, spot, fruitMat, foliageMat, 6, 0.44f);
                        break;

                    case "spring_strawberry":
                        BuildStrawberryBush(parent, spot, foliageMat, fruitMat, 5);
                        break;

                    case "spring_carrot":
                    case "autumn_wintercarrot":
                    case "winter_carrot":
                        BuildRootVegetable(parent, spot, foliageMat, fruitMat, 3, true);
                        break;

                    case "spring_radish":
                    case "winter_radish":
                        BuildRootVegetable(parent, spot, foliageMat, fruitMat, 3, false);
                        break;

                    case "spring_spinach":
                    case "winter_chard":
                    case "winter_arugula":
                    case "winter_cress":
                        BuildLeafyGreensCluster(parent, spot, fruitMat, 7);
                        break;

                    case "spring_pea":
                    case "summer_greenbean":
                        BuildTrellisWithPods(parent, spot, woodMat, foliageMat, fruitMat, 5);
                        break;

                    case "spring_artichoke":
                        BuildArtichokePlant(parent, spot, foliageMat, fruitMat);
                        break;

                    case "spring_asparagus":
                        BuildAsparagusShoots(parent, spot, fruitMat, 6);
                        break;

                    // ==================== YAZ ====================
                    case "summer_pepper":
                        BuildPepperBush(parent, spot, foliageMat, fruitMat, 4);
                        break;

                    case "summer_zucchini":
                        BuildBushWithCylinders(parent, spot, foliageMat, fruitMat, 3, new Vector3(0.14f, 0.36f, 0.14f), 45f);
                        break;

                    case "summer_eggplant":
                        BuildEggplantBush(parent, spot, foliageMat, fruitMat, 3);
                        break;

                    case "summer_corn":
                        BuildCornStalk(parent, spot, stemMat, foliageMat, fruitMat);
                        break;

                    case "summer_okra":
                        BuildOkraPlant(parent, spot, foliageMat, fruitMat);
                        break;

                    case "summer_melon":
                        BuildMelonOrWatermelon(parent, spot, foliageMat, fruitMat, false);
                        break;

                    case "summer_watermelon":
                        BuildMelonOrWatermelon(parent, spot, foliageMat, fruitMat, true);
                        break;

                    case "summer_sunflower":
                        BuildSunflower(parent, spot, stemMat, foliageMat, fruitMat);
                        break;

                    case "summer_grape":
                        BuildGrapeArbor(parent, spot, woodMat, foliageMat, fruitMat);
                        break;

                    // ==================== SONBAHAR ====================
                    case "autumn_potato":
                        BuildPotatoMound(parent, spot, foliageMat, fruitMat);
                        break;

                    case "autumn_onion":
                    case "autumn_garlic":
                    case "winter_garlic":
                        BuildBulbVegetable(parent, spot, foliageMat, fruitMat, 4);
                        break;

                    case "autumn_turnip":
                    case "autumn_beet":
                        BuildRoundRootCrop(parent, spot, foliageMat, fruitMat, 3);
                        break;

                    case "autumn_cabbage":
                    case "winter_cabbage":
                        BuildLayeredCabbageOrLettuce(parent, spot, fruitMat, darkFoliageMat, 7, 0.50f);
                        break;

                    case "autumn_pumpkin":
                        BuildPumpkinVine(parent, spot, foliageMat, fruitMat);
                        break;

                    case "autumn_broccoli":
                        BuildBroccoliHead(parent, spot, darkFoliageMat, fruitMat);
                        break;

                    case "autumn_cauliflower":
                        BuildCauliflowerHead(parent, spot, foliageMat, fruitMat);
                        break;

                    // ==================== KIŞ ====================
                    case "winter_greenhousestrawberry":
                        BuildStrawberryBush(parent, spot, foliageMat, fruitMat, 6);
                        break;

                    case "winter_leek":
                        BuildLeekStalks(parent, spot, fruitMat, 4);
                        break;

                    case "winter_brusselssprout":
                        BuildBrusselsSproutsStalk(parent, spot, stemMat, foliageMat, fruitMat);
                        break;

                    default:
                        // Genel Şık Standart Mahsul Kümesi
                        BuildGenericFruitBush(parent, spot, foliageMat, fruitMat);
                        break;
                }
            }
        }

        #endregion

        #region Specific 3D Crop Model Generators

        private static void BuildTrellisWithFruits(Transform parent, Vector3 spot, Material woodMat, Material folMat, Material fruitMat, int fruitCount, PrimitiveType pType, Vector3 fruitScale)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(parent, false);
            pole.transform.localPosition = spot + new Vector3(0f, 0.55f, 0f);
            pole.transform.localScale = new Vector3(0.05f, 0.55f, 0.05f);
            if (woodMat != null) pole.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.Destroy(pole.GetComponent<Collider>());

            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.transform.SetParent(parent, false);
            foliage.transform.localPosition = spot + new Vector3(0f, 0.50f, 0f);
            foliage.transform.localScale = new Vector3(0.50f, 0.65f, 0.50f);
            if (folMat != null) foliage.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(foliage.GetComponent<Collider>());

            for (int i = 0; i < fruitCount; i++)
            {
                float angle = i * (360f / fruitCount);
                GameObject fruit = GameObject.CreatePrimitive(pType);
                fruit.transform.SetParent(parent, false);
                fruit.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.24f, 0.35f + (i * 0.12f), 0f);
                fruit.transform.localScale = fruitScale;
                if (fruitMat != null) fruit.GetComponent<Renderer>().sharedMaterial = fruitMat;
                Object.Destroy(fruit.GetComponent<Collider>());
            }
        }

        private static void BuildStrawberryBush(Transform parent, Vector3 spot, Material folMat, Material fruitMat, int berryCount)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = spot + new Vector3(0f, 0.18f, 0f);
            bush.transform.localScale = new Vector3(0.52f, 0.26f, 0.52f);
            if (folMat != null) bush.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(bush.GetComponent<Collider>());

            for (int i = 0; i < berryCount; i++)
            {
                float angle = i * (360f / berryCount);
                GameObject berry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                berry.transform.SetParent(parent, false);
                berry.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.22f, 0.12f, 0f);
                berry.transform.localScale = new Vector3(0.12f, 0.16f, 0.12f);
                berry.transform.localRotation = Quaternion.Euler(45f, angle, 0f);
                if (fruitMat != null) berry.GetComponent<Renderer>().sharedMaterial = fruitMat;
                Object.Destroy(berry.GetComponent<Collider>());
            }
        }

        private static void BuildRootVegetable(Transform parent, Vector3 spot, Material folMat, Material rootMat, int count, bool isCarrot)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count);
                Vector3 subSpot = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.12f, 0f, 0f);

                // Topraktan fırlayan kök ucu
                GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                root.transform.SetParent(parent, false);
                root.transform.localPosition = subSpot + new Vector3(0f, 0.08f, 0f);
                root.transform.localScale = isCarrot ? new Vector3(0.12f, 0.12f, 0.12f) : new Vector3(0.16f, 0.09f, 0.16f);
                if (rootMat != null) root.GetComponent<Renderer>().sharedMaterial = rootMat;
                Object.Destroy(root.GetComponent<Collider>());

                // Üst Yapraklar
                for (int l = 0; l < 4; l++)
                {
                    float lAngle = l * 90f;
                    GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    leaf.transform.SetParent(parent, false);
                    leaf.transform.localPosition = subSpot + Quaternion.Euler(0f, lAngle, 0f) * new Vector3(0.08f, 0.22f, 0f);
                    leaf.transform.localScale = new Vector3(0.16f, 0.02f, 0.06f);
                    leaf.transform.localRotation = Quaternion.Euler(-30f, lAngle, 0f);
                    if (folMat != null) leaf.GetComponent<Renderer>().sharedMaterial = folMat;
                    Object.Destroy(leaf.GetComponent<Collider>());
                }
            }
        }

        private static void BuildLayeredCabbageOrLettuce(Transform parent, Vector3 spot, Material coreMat, Material outerMat, int leafCount, float scale)
        {
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(parent, false);
            head.transform.localPosition = spot + new Vector3(0f, scale * 0.45f, 0f);
            head.transform.localScale = new Vector3(scale, scale * 0.85f, scale);
            if (coreMat != null) head.GetComponent<Renderer>().sharedMaterial = coreMat;
            Object.Destroy(head.GetComponent<Collider>());

            for (int i = 0; i < leafCount; i++)
            {
                float angle = i * (360f / leafCount);
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.transform.SetParent(parent, false);
                leaf.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(scale * 0.38f, scale * 0.25f, 0f);
                leaf.transform.localScale = new Vector3(scale * 0.55f, scale * 0.18f, scale * 0.45f);
                leaf.transform.localRotation = Quaternion.Euler(30f, angle, 0f);
                if (outerMat != null) leaf.GetComponent<Renderer>().sharedMaterial = outerMat;
                Object.Destroy(leaf.GetComponent<Collider>());
            }
        }

        private static void BuildCornStalk(Transform parent, Vector3 spot, Material stemMat, Material folMat, Material cornMat)
        {
            GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stalk.transform.SetParent(parent, false);
            stalk.transform.localPosition = spot + new Vector3(0f, 0.65f, 0f);
            stalk.transform.localScale = new Vector3(0.08f, 0.65f, 0.08f);
            if (stemMat != null) stalk.GetComponent<Renderer>().sharedMaterial = stemMat;
            Object.Destroy(stalk.GetComponent<Collider>());

            // Mısır Koçanları
            for (int c = 0; c < 2; c++)
            {
                float side = (c == 0 ? 0.12f : -0.12f);
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                ear.transform.SetParent(parent, false);
                ear.transform.localPosition = spot + new Vector3(side, 0.55f + (c * 0.20f), 0f);
                ear.transform.localScale = new Vector3(0.10f, 0.18f, 0.10f);
                ear.transform.localRotation = Quaternion.Euler(0f, 0f, c == 0 ? -30f : 30f);
                if (cornMat != null) ear.GetComponent<Renderer>().sharedMaterial = cornMat;
                Object.Destroy(ear.GetComponent<Collider>());
            }

            // Geniş Mısır Yaprakları
            for (int l = 0; l < 4; l++)
            {
                float lAngle = l * 90f;
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.transform.SetParent(parent, false);
                leaf.transform.localPosition = spot + Quaternion.Euler(0f, lAngle, 0f) * new Vector3(0.24f, 0.40f + (l * 0.15f), 0f);
                leaf.transform.localScale = new Vector3(0.38f, 0.02f, 0.12f);
                leaf.transform.localRotation = Quaternion.Euler(25f, lAngle, -35f);
                if (folMat != null) leaf.GetComponent<Renderer>().sharedMaterial = folMat;
                Object.Destroy(leaf.GetComponent<Collider>());
            }
        }

        private static void BuildMelonOrWatermelon(Transform parent, Vector3 spot, Material folMat, Material melonMat, bool isWatermelon)
        {
            // Geniş sarmaşık yaprakları
            GameObject vine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vine.transform.SetParent(parent, false);
            vine.transform.localPosition = spot + new Vector3(0f, 0.12f, 0f);
            vine.transform.localScale = new Vector3(0.58f, 0.16f, 0.58f);
            if (folMat != null) vine.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(vine.GetComponent<Collider>());

            // İri Karpuz / Kavun Gövdesi
            GameObject melon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            melon.transform.SetParent(parent, false);
            melon.transform.localPosition = spot + new Vector3(0.08f, 0.22f, 0.05f);
            melon.transform.localScale = isWatermelon ? new Vector3(0.38f, 0.34f, 0.45f) : new Vector3(0.34f, 0.34f, 0.34f);
            if (melonMat != null) melon.GetComponent<Renderer>().sharedMaterial = melonMat;
            Object.Destroy(melon.GetComponent<Collider>());
        }

        private static void BuildPumpkinVine(Transform parent, Vector3 spot, Material folMat, Material pumpkinMat)
        {
            GameObject vine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vine.transform.SetParent(parent, false);
            vine.transform.localPosition = spot + new Vector3(0f, 0.14f, 0f);
            vine.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
            if (folMat != null) vine.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(vine.GetComponent<Collider>());

            // İri Nervürlü Balkabağı
            GameObject pumpkin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pumpkin.transform.SetParent(parent, false);
            pumpkin.transform.localPosition = spot + new Vector3(0f, 0.24f, 0f);
            pumpkin.transform.localScale = new Vector3(0.48f, 0.38f, 0.48f);
            if (pumpkinMat != null) pumpkin.GetComponent<Renderer>().sharedMaterial = pumpkinMat;
            Object.Destroy(pumpkin.GetComponent<Collider>());

            // Yeşil Sap
            Material stemMat = GetMaterial("Pumpkin_Stem", new Color(0.20f, 0.50f, 0.15f));
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(parent, false);
            stem.transform.localPosition = spot + new Vector3(0f, 0.45f, 0f);
            stem.transform.localScale = new Vector3(0.06f, 0.08f, 0.06f);
            if (stemMat != null) stem.GetComponent<Renderer>().sharedMaterial = stemMat;
            Object.Destroy(stem.GetComponent<Collider>());
        }

        private static void BuildSunflower(Transform parent, Vector3 spot, Material stemMat, Material folMat, Material flowerMat)
        {
            GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stalk.transform.SetParent(parent, false);
            stalk.transform.localPosition = spot + new Vector3(0f, 0.65f, 0f);
            stalk.transform.localScale = new Vector3(0.06f, 0.65f, 0.06f);
            if (stemMat != null) stalk.GetComponent<Renderer>().sharedMaterial = stemMat;
            Object.Destroy(stalk.GetComponent<Collider>());

            // Büyük Sarı Çiçek Taç Yaprakları
            GameObject petals = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            petals.transform.SetParent(parent, false);
            petals.transform.localPosition = spot + new Vector3(0f, 1.25f, 0.08f);
            petals.transform.localScale = new Vector3(0.48f, 0.03f, 0.48f);
            petals.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            if (flowerMat != null) petals.GetComponent<Renderer>().sharedMaterial = flowerMat;
            Object.Destroy(petals.GetComponent<Collider>());

            // Kahverengi Çekirdek Merkezi
            Material seedCenterMat = GetMaterial("Sunflower_Center", new Color(0.35f, 0.20f, 0.10f));
            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            center.transform.SetParent(parent, false);
            center.transform.localPosition = spot + new Vector3(0f, 1.25f, 0.10f);
            center.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);
            center.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            if (seedCenterMat != null) center.GetComponent<Renderer>().sharedMaterial = seedCenterMat;
            Object.Destroy(center.GetComponent<Collider>());
        }

        private static void BuildGrapeArbor(Transform parent, Vector3 spot, Material woodMat, Material folMat, Material grapeMat)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.SetParent(parent, false);
            post.transform.localPosition = spot + new Vector3(0f, 0.50f, 0f);
            post.transform.localScale = new Vector3(0.06f, 0.50f, 0.06f);
            if (woodMat != null) post.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.Destroy(post.GetComponent<Collider>());

            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.transform.SetParent(parent, false);
            foliage.transform.localPosition = spot + new Vector3(0f, 0.70f, 0f);
            foliage.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);
            if (folMat != null) foliage.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(foliage.GetComponent<Collider>());

            // Sarkan Üzüm Salkımları
            for (int g = 0; g < 3; g++)
            {
                float angle = g * 120f;
                GameObject bunch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bunch.transform.SetParent(parent, false);
                bunch.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.18f, 0.52f, 0f);
                bunch.transform.localScale = new Vector3(0.15f, 0.22f, 0.15f);
                if (grapeMat != null) bunch.GetComponent<Renderer>().sharedMaterial = grapeMat;
                Object.Destroy(bunch.GetComponent<Collider>());
            }
        }

        private static void BuildEggplantBush(Transform parent, Vector3 spot, Material folMat, Material eggMat, int count = 3)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = spot + new Vector3(0f, 0.28f, 0f);
            bush.transform.localScale = new Vector3(0.48f, 0.35f, 0.48f);
            if (folMat != null) bush.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(bush.GetComponent<Collider>());

            for (int e = 0; e < count; e++)
            {
                float angle = e * (360f / count);
                GameObject eggplant = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                eggplant.transform.SetParent(parent, false);
                eggplant.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.20f, 0.20f, 0f);
                eggplant.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
                eggplant.transform.localRotation = Quaternion.Euler(35f, angle, 0f);
                if (eggMat != null) eggplant.GetComponent<Renderer>().sharedMaterial = eggMat;
                Object.Destroy(eggplant.GetComponent<Collider>());
            }
        }

        private static void BuildPepperBush(Transform parent, Vector3 spot, Material folMat, Material pepperMat, int count)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = spot + new Vector3(0f, 0.26f, 0f);
            bush.transform.localScale = new Vector3(0.46f, 0.34f, 0.46f);
            if (folMat != null) bush.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(bush.GetComponent<Collider>());

            for (int p = 0; p < count; p++)
            {
                float angle = p * (360f / count);
                GameObject pepper = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                pepper.transform.SetParent(parent, false);
                pepper.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.18f, 0.20f, 0f);
                pepper.transform.localScale = new Vector3(0.09f, 0.18f, 0.09f);
                pepper.transform.localRotation = Quaternion.Euler(45f, angle, 0f);
                if (pepperMat != null) pepper.GetComponent<Renderer>().sharedMaterial = pepperMat;
                Object.Destroy(pepper.GetComponent<Collider>());
            }
        }

        private static void BuildBroccoliHead(Transform parent, Vector3 spot, Material folMat, Material brocMat)
        {
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(parent, false);
            stem.transform.localPosition = spot + new Vector3(0f, 0.18f, 0f);
            stem.transform.localScale = new Vector3(0.12f, 0.18f, 0.12f);
            if (folMat != null) stem.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(stem.GetComponent<Collider>());

            // Ağaçsı Brokoli Taçları
            for (int f = 0; f < 5; f++)
            {
                float angle = f * 72f;
                GameObject floret = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                floret.transform.SetParent(parent, false);
                floret.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.12f, 0.35f, 0f);
                floret.transform.localScale = new Vector3(0.20f, 0.18f, 0.20f);
                if (brocMat != null) floret.GetComponent<Renderer>().sharedMaterial = brocMat;
                Object.Destroy(floret.GetComponent<Collider>());
            }

            GameObject mainFloret = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mainFloret.transform.SetParent(parent, false);
            mainFloret.transform.localPosition = spot + new Vector3(0f, 0.42f, 0f);
            mainFloret.transform.localScale = new Vector3(0.26f, 0.20f, 0.26f);
            if (brocMat != null) mainFloret.GetComponent<Renderer>().sharedMaterial = brocMat;
            Object.Destroy(mainFloret.GetComponent<Collider>());
        }

        private static void BuildCauliflowerHead(Transform parent, Vector3 spot, Material folMat, Material cauliMat)
        {
            // Çevreleyen Geniş Yeşil Yapraklar
            for (int l = 0; l < 5; l++)
            {
                float angle = l * 72f;
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.transform.SetParent(parent, false);
                leaf.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.20f, 0.20f, 0f);
                leaf.transform.localScale = new Vector3(0.26f, 0.15f, 0.20f);
                leaf.transform.localRotation = Quaternion.Euler(35f, angle, 0f);
                if (folMat != null) leaf.GetComponent<Renderer>().sharedMaterial = folMat;
                Object.Destroy(leaf.GetComponent<Collider>());
            }

            // Beyaz Karnabahar Başlığı
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(parent, false);
            head.transform.localPosition = spot + new Vector3(0f, 0.30f, 0f);
            head.transform.localScale = new Vector3(0.38f, 0.30f, 0.38f);
            if (cauliMat != null) head.GetComponent<Renderer>().sharedMaterial = cauliMat;
            Object.Destroy(head.GetComponent<Collider>());
        }

        private static void BuildAsparagusShoots(Transform parent, Vector3 spot, Material aspMat, int shootCount)
        {
            for (int s = 0; s < shootCount; s++)
            {
                float angle = s * (360f / shootCount);
                GameObject shoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shoot.transform.SetParent(parent, false);
                shoot.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.10f, 0.30f, 0f);
                shoot.transform.localScale = new Vector3(0.045f, 0.30f, 0.045f);
                shoot.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), angle, Random.Range(-8f, 8f));
                if (aspMat != null) shoot.GetComponent<Renderer>().sharedMaterial = aspMat;
                Object.Destroy(shoot.GetComponent<Collider>());
            }
        }

        private static void BuildArtichokePlant(Transform parent, Vector3 spot, Material folMat, Material artMat)
        {
            GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stalk.transform.SetParent(parent, false);
            stalk.transform.localPosition = spot + new Vector3(0f, 0.25f, 0f);
            stalk.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
            if (folMat != null) stalk.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(stalk.GetComponent<Collider>());

            // İri Enginar Başı
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(parent, false);
            head.transform.localPosition = spot + new Vector3(0f, 0.50f, 0f);
            head.transform.localScale = new Vector3(0.35f, 0.40f, 0.35f);
            if (artMat != null) head.GetComponent<Renderer>().sharedMaterial = artMat;
            Object.Destroy(head.GetComponent<Collider>());
        }

        private static void BuildPotatoMound(Transform parent, Vector3 spot, Material folMat, Material potMat)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = spot + new Vector3(0f, 0.22f, 0f);
            bush.transform.localScale = new Vector3(0.48f, 0.28f, 0.48f);
            if (folMat != null) bush.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(bush.GetComponent<Collider>());

            // Toprak kenarından çıkan patatesler
            for (int p = 0; p < 3; p++)
            {
                float angle = p * 120f;
                GameObject potato = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                potato.transform.SetParent(parent, false);
                potato.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.18f, 0.08f, 0f);
                potato.transform.localScale = new Vector3(0.16f, 0.12f, 0.14f);
                if (potMat != null) potato.GetComponent<Renderer>().sharedMaterial = potMat;
                Object.Destroy(potato.GetComponent<Collider>());
            }
        }

        private static void BuildBulbVegetable(Transform parent, Vector3 spot, Material folMat, Material bulbMat, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count);
                Vector3 subSpot = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.12f, 0f, 0f);

                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.transform.SetParent(parent, false);
                bulb.transform.localPosition = subSpot + new Vector3(0f, 0.10f, 0f);
                bulb.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
                if (bulbMat != null) bulb.GetComponent<Renderer>().sharedMaterial = bulbMat;
                Object.Destroy(bulb.GetComponent<Collider>());

                GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stalk.transform.SetParent(parent, false);
                stalk.transform.localPosition = subSpot + new Vector3(0f, 0.28f, 0f);
                stalk.transform.localScale = new Vector3(0.035f, 0.18f, 0.035f);
                if (folMat != null) stalk.GetComponent<Renderer>().sharedMaterial = folMat;
                Object.Destroy(stalk.GetComponent<Collider>());
            }
        }

        private static void BuildRoundRootCrop(Transform parent, Vector3 spot, Material folMat, Material rootMat, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count);
                Vector3 subSpot = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.14f, 0f, 0f);

                GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                root.transform.SetParent(parent, false);
                root.transform.localPosition = subSpot + new Vector3(0f, 0.12f, 0f);
                root.transform.localScale = new Vector3(0.20f, 0.18f, 0.20f);
                if (rootMat != null) root.GetComponent<Renderer>().sharedMaterial = rootMat;
                Object.Destroy(root.GetComponent<Collider>());

                GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaves.transform.SetParent(parent, false);
                leaves.transform.localPosition = subSpot + new Vector3(0f, 0.30f, 0f);
                leaves.transform.localScale = new Vector3(0.22f, 0.18f, 0.22f);
                if (folMat != null) leaves.GetComponent<Renderer>().sharedMaterial = folMat;
                Object.Destroy(leaves.GetComponent<Collider>());
            }
        }

        private static void BuildLeekStalks(Transform parent, Vector3 spot, Material leekMat, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count);
                GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stalk.transform.SetParent(parent, false);
                stalk.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.10f, 0.32f, 0f);
                stalk.transform.localScale = new Vector3(0.06f, 0.32f, 0.06f);
                stalk.transform.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), angle, 0f);
                if (leekMat != null) stalk.GetComponent<Renderer>().sharedMaterial = leekMat;
                Object.Destroy(stalk.GetComponent<Collider>());
            }
        }

        private static void BuildBrusselsSproutsStalk(Transform parent, Vector3 spot, Material stemMat, Material folMat, Material sproutMat)
        {
            GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stalk.transform.SetParent(parent, false);
            stalk.transform.localPosition = spot + new Vector3(0f, 0.45f, 0f);
            stalk.transform.localScale = new Vector3(0.07f, 0.45f, 0.07f);
            if (stemMat != null) stalk.GetComponent<Renderer>().sharedMaterial = stemMat;
            Object.Destroy(stalk.GetComponent<Collider>());

            for (int s = 0; s < 6; s++)
            {
                float angle = s * 60f;
                GameObject miniSprout = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                miniSprout.transform.SetParent(parent, false);
                miniSprout.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.08f, 0.20f + (s * 0.10f), 0f);
                miniSprout.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
                if (sproutMat != null) miniSprout.GetComponent<Renderer>().sharedMaterial = sproutMat;
                Object.Destroy(miniSprout.GetComponent<Collider>());
            }
        }

        private static void BuildOkraPlant(Transform parent, Vector3 spot, Material folMat, Material okraMat)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = spot + new Vector3(0f, 0.25f, 0f);
            bush.transform.localScale = new Vector3(0.42f, 0.30f, 0.42f);
            if (folMat != null) bush.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(bush.GetComponent<Collider>());

            // Yukarı doğru bakan konik bamyalar
            for (int o = 0; o < 3; o++)
            {
                float angle = o * 120f;
                GameObject okra = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                okra.transform.SetParent(parent, false);
                okra.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.14f, 0.40f, 0f);
                okra.transform.localScale = new Vector3(0.05f, 0.14f, 0.05f);
                okra.transform.localRotation = Quaternion.Euler(-15f, angle, 0f);
                if (okraMat != null) okra.GetComponent<Renderer>().sharedMaterial = okraMat;
                Object.Destroy(okra.GetComponent<Collider>());
            }
        }

        private static void BuildTrellisWithPods(Transform parent, Vector3 spot, Material woodMat, Material folMat, Material podMat, int podCount)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(parent, false);
            pole.transform.localPosition = spot + new Vector3(0f, 0.50f, 0f);
            pole.transform.localScale = new Vector3(0.045f, 0.50f, 0.045f);
            if (woodMat != null) pole.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.Destroy(pole.GetComponent<Collider>());

            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = spot + new Vector3(0f, 0.45f, 0f);
            bush.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
            if (folMat != null) bush.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(bush.GetComponent<Collider>());

            for (int p = 0; p < podCount; p++)
            {
                float angle = p * (360f / podCount);
                GameObject pod = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                pod.transform.SetParent(parent, false);
                pod.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.20f, 0.28f + (p * 0.10f), 0f);
                pod.transform.localScale = new Vector3(0.06f, 0.16f, 0.06f);
                pod.transform.localRotation = Quaternion.Euler(30f, angle, 0f);
                if (podMat != null) pod.GetComponent<Renderer>().sharedMaterial = podMat;
                Object.Destroy(pod.GetComponent<Collider>());
            }
        }

        private static void BuildLeafyGreensCluster(Transform parent, Vector3 spot, Material leafMat, int leafCount)
        {
            for (int l = 0; l < leafCount; l++)
            {
                float angle = l * (360f / leafCount);
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.transform.SetParent(parent, false);
                leaf.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.14f, 0.18f, 0f);
                leaf.transform.localScale = new Vector3(0.24f, 0.02f, 0.10f);
                leaf.transform.localRotation = Quaternion.Euler(-30f, angle, 0f);
                if (leafMat != null) leaf.GetComponent<Renderer>().sharedMaterial = leafMat;
                Object.Destroy(leaf.GetComponent<Collider>());
            }
        }

        private static void BuildBushWithCylinders(Transform parent, Vector3 spot, Material folMat, Material fruitMat, int count, Vector3 scale, float tilt)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = spot + new Vector3(0f, 0.22f, 0f);
            bush.transform.localScale = new Vector3(0.50f, 0.28f, 0.50f);
            if (folMat != null) bush.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(bush.GetComponent<Collider>());

            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count);
                GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fruit.transform.SetParent(parent, false);
                fruit.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.20f, 0.14f, 0f);
                fruit.transform.localScale = scale;
                fruit.transform.localRotation = Quaternion.Euler(tilt, angle, 0f);
                if (fruitMat != null) fruit.GetComponent<Renderer>().sharedMaterial = fruitMat;
                Object.Destroy(fruit.GetComponent<Collider>());
            }
        }

        private static void BuildGenericFruitBush(Transform parent, Vector3 spot, Material folMat, Material fruitMat)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = spot + new Vector3(0f, 0.28f, 0f);
            bush.transform.localScale = new Vector3(0.46f, 0.35f, 0.46f);
            if (folMat != null) bush.GetComponent<Renderer>().sharedMaterial = folMat;
            Object.Destroy(bush.GetComponent<Collider>());

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fruit.transform.SetParent(parent, false);
                fruit.transform.localPosition = spot + Quaternion.Euler(0f, angle, 0f) * new Vector3(0.18f, 0.25f, 0f);
                fruit.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
                if (fruitMat != null) fruit.GetComponent<Renderer>().sharedMaterial = fruitMat;
                Object.Destroy(fruit.GetComponent<Collider>());
            }
        }

        #endregion

        #region Spoiled Trash

        private static void BuildSpoiledTrash(Transform parent, Vector3[] spots)
        {
            Material trashMat = GetMaterial("Spoiled_Trash", new Color(0.24f, 0.16f, 0.10f));
            Material moldMat = GetMaterial("Spoiled_Mold", new Color(0.40f, 0.35f, 0.22f));

            foreach (var spot in spots)
            {
                GameObject mound = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mound.transform.SetParent(parent, false);
                mound.transform.localPosition = spot + new Vector3(0f, 0.08f, 0f);
                mound.transform.localScale = new Vector3(0.38f, 0.10f, 0.38f);
                mound.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 90f), 0f);
                if (trashMat != null) mound.GetComponent<Renderer>().sharedMaterial = trashMat;
                Object.Destroy(mound.GetComponent<Collider>());

                GameObject witheredStem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                witheredStem.transform.SetParent(parent, false);
                witheredStem.transform.localPosition = spot + new Vector3(0.04f, 0.12f, -0.04f);
                witheredStem.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
                witheredStem.transform.localRotation = Quaternion.Euler(45f, 30f, 0f);
                if (moldMat != null) witheredStem.GetComponent<Renderer>().sharedMaterial = moldMat;
                Object.Destroy(witheredStem.GetComponent<Collider>());
            }
        }

        #endregion
    }
}
