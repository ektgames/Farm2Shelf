using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Farm2Shelf Batı Bölgesi Ultra Detaylı Mimari İnşaatçısı (Masterpiece Procedural West District Builder).
    /// Baştan savma kutular yerine tamamen el işçiliğiyle modellenmiş zengin mimari yapılar:
    /// - Nehir Yatağı & 1 Adet Kavisli 2 Şeritli Yay Köprü (Altından su kesintisiz akar, araçlar üstünden tırmanır)
    /// - 12 Adet Ultra Lüks Villa (Giriş kapıları, kollar, sundurmalar, pencereler, çıtalar, havuz merdiveni, şezlonglar, palmiyeler)
    /// - 8 Adet İkonik Şehir Kamu & Sosyal Tesisi:
    ///   * İlkokul (4 Sütunlu revak, kiremit çatı, saat kulesi, Türk bayrağı, basketbol sahası)
    ///   * Kütüphane & Park (6 İyonik sütun, mermer merdivenler, açık kitap amblemi, okuma pergolaları)
    ///   * Devlet Hastanesi (Acil servis portikosu, kırmızı hilal, ambulans peronu, çatı helipadi)
    ///   * İtfaiye İstasyonu (3 Panjurlu garaj, hortum kulesi, tepe sireni)
    ///   * Polis Merkezi (Mavi siren, emniyet mimarisi)
    ///   * Benzin İstasyonu (Geniş kanopi, 4 detaylı pompa, mini market, totem)
    ///   * Strike Bowling Salonu (3D lobutlar ve top, neon süslemeler)
    ///   * CineStar Sinema Salonu (Işıklı marquee kanopi, afiş panoları, gişe)
    ///   * Kasaba Stadyumu (Çim saha, kaleler, fileler, koltuklu tribünler, 4 dev projektör kulesi)
    /// </summary>
    public static class ProceduralWestDistrictBuilder
    {
        private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        private static Material GetMaterial(string name, Color color, float metallic = 0.15f, float smoothness = 0.35f)
        {
            if (matCache.TryGetValue(name, out Material mat) && mat != null)
            {
                return mat;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
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

        public static void BuildWestDistrict(Transform parent)
        {
            Transform districtRoot = new GameObject("West_District_Complex").transform;
            districtRoot.SetParent(parent, false);

            // 1. NEHİR VE KAVİSLİ KÖPRÜ (ALTINDAN SU AKAN)
            BuildRiverAndArchedBridge(districtRoot);

            // 2. NİZAMİ BATI YOL ŞEBEKESİ, KALDIRIMLAR VE YAYA GEÇİTLERİ
            BuildWestRoadNetwork(districtRoot);

            // 3. 12 ADET DETAYLI LÜKS VİLLA SİTESİ (KUZEY BATI)
            BuildLuxuryVillaEstate(districtRoot);

            // 4. ŞEHİR KAMU VE SOSYAL YAŞAM MERKEZİ (GÜNEY BATI)
            BuildCivicAndSocialDistrict(districtRoot);

            // 5. BATI ANA OTOYOLU UFUK UZANTISI & DESPAWN KORİDORU (X = -226m'den -360m'ye)
            BuildWestHighwayFarExtension(districtRoot);
        }

        #region 1. River and Single Arched Bridge System

        private static void BuildRiverAndArchedBridge(Transform parent)
        {
            GameObject riverGroup = new GameObject("River_And_Bridge_System");
            riverGroup.transform.SetParent(parent, false);

            float riverCenterX = -95.0f;
            float riverWidth = 22.0f;
            float riverMinZ = -135.0f;
            float riverMaxZ = 190.0f;
            float riverLength = riverMaxZ - riverMinZ;
            float riverCenterZ = (riverMinZ + riverMaxZ) / 2f;

            Material waterMat = GetMaterial("RiverWaterVibrantMat", new Color(0.08f, 0.62f, 0.88f, 0.90f), 0.9f, 0.95f);
            Material riverBedMat = GetMaterial("RiverBedDarkStoneMat", new Color(0.22f, 0.24f, 0.26f), 0.1f, 0.2f);
            Material quayMat = GetMaterial("RiverQuayStoneMat", new Color(0.70f, 0.70f, 0.72f), 0.2f, 0.3f);
            Material railingMat = GetMaterial("RiverRailingMat", new Color(0.18f, 0.20f, 0.22f), 0.5f, 0.6f);

            // 1.1 Nehir Yatağı Tabanı (Deep Riverbed Bottom)
            GameObject riverBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            riverBed.name = "River_Bed_Bottom";
            riverBed.transform.SetParent(riverGroup.transform, false);
            riverBed.transform.position = new Vector3(riverCenterX, -1.6f, riverCenterZ);
            riverBed.transform.localScale = new Vector3(riverWidth + 1.0f, 0.4f, riverLength);
            riverBed.GetComponent<Renderer>().sharedMaterial = riverBedMat;
            Object.Destroy(riverBed.GetComponent<Collider>());

            // 1.2 Nehir Canlı Su Yüzeyi (Köprünün altından serbestçe ve pırıl pırıl akan su)
            GameObject riverWater = GameObject.CreatePrimitive(PrimitiveType.Cube);
            riverWater.name = "River_Water_Surface";
            riverWater.transform.SetParent(riverGroup.transform, false);
            riverWater.transform.position = new Vector3(riverCenterX, -0.05f, riverCenterZ);
            riverWater.transform.localScale = new Vector3(riverWidth - 0.2f, 0.10f, riverLength);
            riverWater.GetComponent<Renderer>().sharedMaterial = waterMat;
            Object.Destroy(riverWater.GetComponent<Collider>());

            // 1.3 Doğu ve Batı Rıhtım Duvarları & Korkuluklar
            for (int side = -1; side <= 1; side += 2)
            {
                float qx = riverCenterX + (side * (riverWidth / 2f));

                // Rıhtım Taş Sedde Duvarı
                GameObject quayWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                quayWall.name = $"River_Quay_Wall_{(side > 0 ? "East" : "West")}";
                quayWall.transform.SetParent(riverGroup.transform, false);
                quayWall.transform.position = new Vector3(qx, -0.2f, riverCenterZ);
                quayWall.transform.localScale = new Vector3(0.8f, 1.8f, riverLength);
                quayWall.GetComponent<Renderer>().sharedMaterial = quayMat;
                Object.Destroy(quayWall.GetComponent<Collider>());

                // Rıhtım Üst Küpeştesi
                GameObject quayCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                quayCap.name = $"River_Quay_Cap_{(side > 0 ? "East" : "West")}";
                quayCap.transform.SetParent(riverGroup.transform, false);
                quayCap.transform.position = new Vector3(qx, 0.72f, riverCenterZ);
                quayCap.transform.localScale = new Vector3(1.0f, 0.15f, riverLength);
                quayCap.GetComponent<Renderer>().sharedMaterial = quayMat;
                Object.Destroy(quayCap.GetComponent<Collider>());

                // Emniyet Korkuluğu (Köprü geçişi Z: -15m ile -3m arası hariç)
                float northLen = riverMaxZ - (-3f);
                GameObject northRailing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                northRailing.name = $"River_Railing_North_{(side > 0 ? "East" : "West")}";
                northRailing.transform.SetParent(riverGroup.transform, false);
                northRailing.transform.position = new Vector3(qx, 1.05f, -3f + (northLen / 2f));
                northRailing.transform.localScale = new Vector3(0.10f, 0.50f, northLen);
                northRailing.GetComponent<Renderer>().sharedMaterial = railingMat;
                Object.Destroy(northRailing.GetComponent<Collider>());

                float southLen = (-15f) - riverMinZ;
                GameObject southRailing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                southRailing.name = $"River_Railing_South_{(side > 0 ? "East" : "West")}";
                southRailing.transform.SetParent(riverGroup.transform, false);
                southRailing.transform.position = new Vector3(qx, 1.05f, riverMinZ + (southLen / 2f));
                southRailing.transform.localScale = new Vector3(0.10f, 0.50f, southLen);
                southRailing.GetComponent<Renderer>().sharedMaterial = railingMat;
                Object.Destroy(southRailing.GetComponent<Collider>());
            }

            // 1.4 Kıyı Kordonu Peyzajı (Ağaçlar ve banklar KESİNLİKLE yola taşmaz, rıhtım kaldırımında durur)
            float[] promenadeZs = new float[] { -115f, -85f, -55f, -30f, 25f, 55f, 85f, 115f, 145f, 175f };
            foreach (float pz in promenadeZs)
            {
                // Doğu Yakası Rıhtım Kordonu (X = -81.5m)
                BuildRiverPromenadeTree(riverGroup.transform, new Vector3(-81.5f, 0f, pz));
                BuildPromenadeBench(riverGroup.transform, new Vector3(-82.5f, 0.25f, pz + 4.5f), true);

                // Batı Yakası Rıhtım Kordonu (X = -108.5m)
                BuildRiverPromenadeTree(riverGroup.transform, new Vector3(-108.5f, 0f, pz));
                BuildPromenadeBench(riverGroup.transform, new Vector3(-107.5f, 0.25f, pz + 4.5f), false);
            }

            // 1.5 TEK ADET 2 ŞERİTLİ KAVİSLİ KÖPRÜ (Arch Bridge | Ortaya doğru yükselip inen)
            BuildCurvedArchBridge(riverGroup.transform, new Vector3(riverCenterX, 0f, -9.0f), riverWidth);
        }

        private static void BuildCurvedArchBridge(Transform parent, Vector3 centerPos, float riverSpan)
        {
            GameObject bridgeObj = new GameObject("Curved_Arch_Highway_Bridge");
            bridgeObj.transform.SetParent(parent, false);
            bridgeObj.transform.position = centerPos;

            Material asphaltMat = GetMaterial("BridgeAsphaltMat", new Color(0.20f, 0.20f, 0.22f), 0.1f, 0.2f);
            Material stoneMat = GetMaterial("BridgeStoneMat", new Color(0.72f, 0.72f, 0.75f), 0.2f, 0.4f);
            Material railingMat = GetMaterial("BridgeRailingMat", new Color(0.18f, 0.20f, 0.22f), 0.5f, 0.6f);
            Material stripeMat = GetMaterial("BridgeStripeMat", new Color(0.95f, 0.95f, 0.95f), 0.0f, 0.2f);

            float totalLength = riverSpan + 6.0f; // 28 metre toplam açıklık (X: -80m ile -108m arası)
            float roadDeckWidth = 6.0f; // 2 Şerit Asfalt Genişliği
            float sidewalkWidth = 1.5f; // Sağ ve Sol Yaya Kaldırımı
            float bridgeWidth = roadDeckWidth + (2f * sidewalkWidth); // 9.0 metre tam köprü genişliği
            int segments = 9;
            float segLen = totalLength / segments;
            float maxArchHeight = 1.65f; // Köprü tepe kavis yüksekliği

            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;
                float tMid = (float)(i + 0.5f) / segments;

                float x0 = -totalLength / 2f + t0 * totalLength;
                float x1 = -totalLength / 2f + t1 * totalLength;
                float midX = (x0 + x1) / 2f;

                float y0 = 0.05f + (4f * maxArchHeight * t0 * (1f - t0));
                float y1 = 0.05f + (4f * maxArchHeight * t1 * (1f - t1));
                float midY = 0.05f + (4f * maxArchHeight * tMid * (1f - tMid));

                float angleZ = Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg;

                GameObject segObj = new GameObject($"Bridge_Segment_{i + 1}");
                segObj.transform.SetParent(bridgeObj.transform, false);
                segObj.transform.localPosition = new Vector3(midX, midY, 0f);
                segObj.transform.localRotation = Quaternion.Euler(0f, 0f, angleZ);

                // Asfalt Tabliye (6.0m Genişlik)
                GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deck.name = "Deck";
                deck.transform.SetParent(segObj.transform, false);
                deck.transform.localPosition = Vector3.zero;
                deck.transform.localScale = new Vector3(segLen + 0.1f, 0.30f, roadDeckWidth);
                deck.GetComponent<Renderer>().sharedMaterial = asphaltMat;
                Object.Destroy(deck.GetComponent<Collider>());

                // Orta Şerit Beyaz Çizgisi
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Lane_Stripe";
                stripe.transform.SetParent(segObj.transform, false);
                stripe.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                stripe.transform.localScale = new Vector3(segLen * 0.65f, 0.01f, 0.20f);
                stripe.GetComponent<Renderer>().sharedMaterial = stripeMat;
                Object.Destroy(stripe.GetComponent<Collider>());

                // Kavisli Köprü İçi Yaya Kaldırımları ve Korkuluklar
                for (int dirZ = -1; dirZ <= 1; dirZ += 2)
                {
                    float zSide = dirZ * ((roadDeckWidth / 2f) + (sidewalkWidth / 2f));

                    // Kavisli Taş Kaldırım
                    GameObject sidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sidewalk.name = $"Bridge_Curved_Sidewalk_{dirZ}";
                    sidewalk.transform.SetParent(segObj.transform, false);
                    sidewalk.transform.localPosition = new Vector3(0f, 0.10f, zSide);
                    sidewalk.transform.localScale = new Vector3(segLen + 0.1f, 0.38f, sidewalkWidth);
                    sidewalk.GetComponent<Renderer>().sharedMaterial = stoneMat;
                    Object.Destroy(sidewalk.GetComponent<Collider>());

                    // Taş Bordür / Güvenlik Bariyeri
                    GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    curb.name = "Bridge_Sidewalk_Curb";
                    curb.transform.SetParent(segObj.transform, false);
                    curb.transform.localPosition = new Vector3(0f, 0.20f, dirZ * (roadDeckWidth / 2f));
                    curb.transform.localScale = new Vector3(segLen + 0.1f, 0.16f, 0.15f);
                    curb.GetComponent<Renderer>().sharedMaterial = stoneMat;
                    Object.Destroy(curb.GetComponent<Collider>());

                    // Dış Korkuluk (Railing)
                    GameObject railing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    railing.name = $"Bridge_Railing_{dirZ}";
                    railing.transform.SetParent(segObj.transform, false);
                    railing.transform.localPosition = new Vector3(0f, 0.58f, dirZ * ((bridgeWidth / 2f) - 0.06f));
                    railing.transform.localScale = new Vector3(segLen + 0.1f, 0.65f, 0.12f);
                    railing.GetComponent<Renderer>().sharedMaterial = railingMat;
                    Object.Destroy(railing.GetComponent<Collider>());
                }
            }

            // Su İçindeki Kemer Destek Ayakları (Pillars)
            for (int dirX = -1; dirX <= 1; dirX += 2)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"Bridge_Arch_Pillar_{dirX}";
                pillar.transform.SetParent(bridgeObj.transform, false);
                pillar.transform.localPosition = new Vector3(dirX * 7.5f, 0.15f, 0f);
                pillar.transform.localScale = new Vector3(1.8f, 1.6f, bridgeWidth + 0.4f);
                pillar.GetComponent<Renderer>().sharedMaterial = stoneMat;
                Object.Destroy(pillar.GetComponent<Collider>());
            }

            // 4 Giriş Anıtsal Feneri
            for (int dirX = -1; dirX <= 1; dirX += 2)
            {
                for (int dirZ = -1; dirZ <= 1; dirZ += 2)
                {
                    BuildBridgePylonLamp(bridgeObj.transform, new Vector3(dirX * (totalLength / 2f - 0.5f), 0.2f, dirZ * (bridgeWidth / 2f)));
                }
            }
        }

        private static void BuildBridgePylonLamp(Transform parent, Vector3 localPos)
        {
            GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pylon.name = "Bridge_Entrance_Pylon";
            pylon.transform.SetParent(parent, false);
            pylon.transform.localPosition = localPos + new Vector3(0f, 0.8f, 0f);
            pylon.transform.localScale = new Vector3(0.6f, 1.6f, 0.6f);
            pylon.GetComponent<Renderer>().sharedMaterial = GetMaterial("BridgeStoneMat", new Color(0.72f, 0.72f, 0.75f), 0.2f, 0.4f);
            Object.Destroy(pylon.GetComponent<Collider>());

            GameObject globe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            globe.name = "Bridge_Lamp_Globe";
            globe.transform.SetParent(pylon.transform, false);
            globe.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            globe.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            globe.GetComponent<Renderer>().sharedMaterial = GetMaterial("BridgeLampGlobeMat", new Color(1.0f, 0.95f, 0.75f), 0.1f, 0.9f);
            Object.Destroy(globe.GetComponent<Collider>());

            GameObject lightChild = new GameObject("Bridge_Light");
            lightChild.transform.SetParent(globe.transform, false);
            lightChild.transform.localPosition = Vector3.zero;
            Light pLight = lightChild.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.88f, 0.55f);
            pLight.intensity = 2.4f;
            pLight.range = 10.0f;
            pLight.shadows = LightShadows.None;
            pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStreetLamp(globe, pLight);
            }
        }

        private static void BuildRiverPromenadeTree(Transform parent, Vector3 pos)
        {
            GameObject tree = new GameObject("Promenade_Tree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 1.4f, 0.35f);
            trunk.GetComponent<Renderer>().sharedMaterial = GetMaterial("TreeTrunkMat", new Color(0.38f, 0.25f, 0.15f), 0.1f, 0.2f);
            Object.Destroy(trunk.GetComponent<Collider>());

            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Foliage";
            foliage.transform.SetParent(tree.transform, false);
            foliage.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            foliage.transform.localScale = new Vector3(2.6f, 2.4f, 2.6f);
            foliage.GetComponent<Renderer>().sharedMaterial = GetMaterial("TreeFoliageMat", new Color(0.20f, 0.52f, 0.24f), 0.0f, 0.1f);
            Object.Destroy(foliage.GetComponent<Collider>());
        }

        private static void BuildPromenadeBench(Transform parent, Vector3 pos, bool faceWest)
        {
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Promenade_Bench";
            bench.transform.SetParent(parent, false);
            bench.transform.position = pos;
            bench.transform.localScale = new Vector3(0.6f, 0.40f, 1.8f);
            bench.GetComponent<Renderer>().sharedMaterial = GetMaterial("BenchWoodMat", new Color(0.48f, 0.28f, 0.16f), 0.1f, 0.3f);
            Object.Destroy(bench.GetComponent<Collider>());
        }

        #endregion

        #region 2. Clean West Road Network & Crosswalks

        private static void BuildWestRoadNetwork(Transform parent)
        {
            GameObject roadGroup = new GameObject("West_Road_Network");
            roadGroup.transform.SetParent(parent, false);

            Material asphaltMat = GetMaterial("CleanAsphaltMat", new Color(0.18f, 0.20f, 0.22f), 0.0f, 0.2f);
            Material sidewalkMat = GetMaterial("CleanSidewalkMat", new Color(0.72f, 0.74f, 0.76f), 0.0f, 0.3f);
            Material pureWhiteLineMat = GetMaterial("PureWhiteRoadLineMat", new Color(0.98f, 0.98f, 0.98f), 0.0f, 0.2f);
            Material crosswalkWhiteMat = GetMaterial("ZebraCrosswalkWhiteMat", new Color(0.98f, 0.98f, 0.98f), 0.0f, 0.2f);

            float[] vertAvenues = new float[] { -112.0f, -150.0f, -188.0f, -226.0f };
            float[] horizStreets = new float[] { -125.0f, -55.0f, -9.0f, 175.0f };

            // 2.1 YATAY VE DİKEY ASFALT TABLİYELERİ
            foreach (float cz in horizStreets)
            {
                GameObject roadH = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roadH.name = $"Clean_Road_Horizontal_{cz}";
                roadH.transform.SetParent(roadGroup.transform, false);
                roadH.transform.position = new Vector3(-168.0f, 0.01f, cz);
                roadH.transform.localScale = new Vector3(124.0f, 0.02f, 6.0f);
                roadH.GetComponent<Renderer>().sharedMaterial = asphaltMat;
                Object.Destroy(roadH.GetComponent<Collider>());
            }

            foreach (float ax in vertAvenues)
            {
                GameObject roadV = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roadV.name = $"Clean_Road_Vertical_{ax}";
                roadV.transform.SetParent(roadGroup.transform, false);
                roadV.transform.position = new Vector3(ax, 0.01f, 25.0f);
                roadV.transform.localScale = new Vector3(6.0f, 0.02f, 306.0f);
                roadV.GetComponent<Renderer>().sharedMaterial = asphaltMat;
                Object.Destroy(roadV.GetComponent<Collider>());
            }

            // 2.2 GERÇEK BEYAZ KESİKLİ ŞERİT ÇİZGİLERİ
            // Yatay Yollar İçin Kesikli Çizgiler
            foreach (float cz in horizStreets)
            {
                for (float x = -228.0f; x <= -106.0f; x += 3.6f)
                {
                    bool inJunction = false;
                    foreach (float ax in vertAvenues)
                    {
                        if (Mathf.Abs(x - ax) < 3.6f)
                        {
                            inJunction = true;
                            break;
                        }
                    }
                    if (inJunction) continue;

                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stripe.name = "Lane_Stripe_Horizontal";
                    stripe.transform.SetParent(roadGroup.transform, false);
                    stripe.transform.position = new Vector3(x, 0.022f, cz);
                    stripe.transform.localScale = new Vector3(1.8f, 0.01f, 0.22f);
                    stripe.GetComponent<Renderer>().sharedMaterial = pureWhiteLineMat;
                    Object.Destroy(stripe.GetComponent<Collider>());
                }
            }

            // Dikey Yollar İçin Kesikli Çizgiler
            foreach (float ax in vertAvenues)
            {
                for (float z = -127.0f; z <= 177.0f; z += 3.6f)
                {
                    bool inJunction = false;
                    foreach (float cz in horizStreets)
                    {
                        if (Mathf.Abs(z - cz) < 3.6f)
                        {
                            inJunction = true;
                            break;
                        }
                    }
                    if (inJunction) continue;

                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stripe.name = "Lane_Stripe_Vertical";
                    stripe.transform.SetParent(roadGroup.transform, false);
                    stripe.transform.position = new Vector3(ax, 0.022f, z);
                    stripe.transform.localScale = new Vector3(0.22f, 0.01f, 1.8f);
                    stripe.GetComponent<Renderer>().sharedMaterial = pureWhiteLineMat;
                    Object.Destroy(stripe.GetComponent<Collider>());
                }
            }

            // 2.3 KALDIRIMLAR (KAVŞAKLARDA ASLA YOLA TAŞMAZ, SINIRLARDA VE AĞAÇLARDA KESİNTİSİZ DEVAM EDER)
            // 2.3.1 Dikey Cadde Kaldırımları (İç Segmentler)
            foreach (float ax in vertAvenues)
            {
                // Segment 1 (Güney Alt Blok: Z = -125m ile -55m arası)
                CreateSidewalkBlock(roadGroup.transform, new Vector3(ax - 3.65f, 0.04f, -90.0f), new Vector3(1.3f, 0.08f, 61.4f), sidewalkMat);
                CreateSidewalkBlock(roadGroup.transform, new Vector3(ax + 3.65f, 0.04f, -90.0f), new Vector3(1.3f, 0.08f, 61.4f), sidewalkMat);

                // Segment 2 (Güney Orta Blok: Z = -55m ile -9m arası)
                CreateSidewalkBlock(roadGroup.transform, new Vector3(ax - 3.65f, 0.04f, -32.0f), new Vector3(1.3f, 0.08f, 37.4f), sidewalkMat);
                CreateSidewalkBlock(roadGroup.transform, new Vector3(ax + 3.65f, 0.04f, -32.0f), new Vector3(1.3f, 0.08f, 37.4f), sidewalkMat);

                // Segment 3 (Kuzey Villalar Bloğu: Z = -9m ile 175m arası)
                CreateSidewalkBlock(roadGroup.transform, new Vector3(ax - 3.65f, 0.04f, 83.0f), new Vector3(1.3f, 0.08f, 175.4f), sidewalkMat);
                CreateSidewalkBlock(roadGroup.transform, new Vector3(ax + 3.65f, 0.04f, 83.0f), new Vector3(1.3f, 0.08f, 175.4f), sidewalkMat);
            }

            // 2.3.2 Yatay Yol Kaldırımları (İç Segmentler)
            foreach (float cz in horizStreets)
            {
                // Segment 1 (X: -226m ile -188m arası)
                CreateSidewalkBlock(roadGroup.transform, new Vector3(-207.0f, 0.04f, cz - 3.65f), new Vector3(29.4f, 0.08f, 1.3f), sidewalkMat);
                CreateSidewalkBlock(roadGroup.transform, new Vector3(-207.0f, 0.04f, cz + 3.65f), new Vector3(29.4f, 0.08f, 1.3f), sidewalkMat);

                // Segment 2 (X: -188m ile -150m arası)
                CreateSidewalkBlock(roadGroup.transform, new Vector3(-169.0f, 0.04f, cz - 3.65f), new Vector3(29.4f, 0.08f, 1.3f), sidewalkMat);
                CreateSidewalkBlock(roadGroup.transform, new Vector3(-169.0f, 0.04f, cz + 3.65f), new Vector3(29.4f, 0.08f, 1.3f), sidewalkMat);

                // Segment 3 (X: -150m ile -112m arası)
                CreateSidewalkBlock(roadGroup.transform, new Vector3(-131.0f, 0.04f, cz - 3.65f), new Vector3(29.4f, 0.08f, 1.3f), sidewalkMat);
                CreateSidewalkBlock(roadGroup.transform, new Vector3(-131.0f, 0.04f, cz + 3.65f), new Vector3(29.4f, 0.08f, 1.3f), sidewalkMat);

                // Segment 4 (X: -112m ile -106m arası - Rıhtım Bağlantısı)
                CreateSidewalkBlock(roadGroup.transform, new Vector3(-106.85f, 0.04f, cz - 3.65f), new Vector3(1.7f, 0.08f, 1.3f), sidewalkMat);
                CreateSidewalkBlock(roadGroup.transform, new Vector3(-106.85f, 0.04f, cz + 3.65f), new Vector3(1.7f, 0.08f, 1.3f), sidewalkMat);
            }

            // 2.3.3 Kavşak Köşe Dönüş Kaldırımları
            foreach (float ax in vertAvenues)
            {
                foreach (float cz in horizStreets)
                {
                    CreateSidewalkBlock(roadGroup.transform, new Vector3(ax - 3.65f, 0.04f, cz - 3.65f), new Vector3(1.3f, 0.08f, 1.3f), sidewalkMat);
                    CreateSidewalkBlock(roadGroup.transform, new Vector3(ax + 3.65f, 0.04f, cz - 3.65f), new Vector3(1.3f, 0.08f, 1.3f), sidewalkMat);
                    CreateSidewalkBlock(roadGroup.transform, new Vector3(ax - 3.65f, 0.04f, cz + 3.65f), new Vector3(1.3f, 0.08f, 1.3f), sidewalkMat);
                    CreateSidewalkBlock(roadGroup.transform, new Vector3(ax + 3.65f, 0.04f, cz + 3.65f), new Vector3(1.3f, 0.08f, 1.3f), sidewalkMat);
                }
            }

            // 2.3.4 AĞAÇLARIN VE SINIRLARIN OLDUĞU YERLERDE KESİNTİSİZ TAM KALDIRIM KAPLAMALARI (Yol Gitmeyen Sınırlar)
            // Kuzey Sınırında (Z = 175m) Ağaçlar Tarafı Boydan Boya Kesintisiz Kaldırım
            foreach (float ax in vertAvenues)
            {
                CreateSidewalkBlock(roadGroup.transform, new Vector3(ax, 0.04f, 175.0f + 3.65f), new Vector3(6.0f, 0.08f, 1.3f), sidewalkMat);
            }

            // Güney Sınırında (Z = -125m) Ağaçlar Tarafı Boydan Boya Kesintisiz Kaldırım
            foreach (float ax in vertAvenues)
            {
                CreateSidewalkBlock(roadGroup.transform, new Vector3(ax, 0.04f, -125.0f - 3.65f), new Vector3(6.0f, 0.08f, 1.3f), sidewalkMat);
            }

            // Batı Sınırında (X = -226m) Ana Yol (Z = -9m) Haricinde Ağaçlar Tarafı Boydan Boya Kesintisiz Kaldırım
            foreach (float cz in horizStreets)
            {
                if (Mathf.Abs(cz - (-9.0f)) > 1.0f)
                {
                    CreateSidewalkBlock(roadGroup.transform, new Vector3(-226.0f - 3.65f, 0.04f, cz), new Vector3(1.3f, 0.08f, 6.0f), sidewalkMat);
                }
            }

            // Doğu Rıhtım Sınırında (X = -112m) Köprü Harici (Z != -9m) Nehir Tarafı Kesintisiz Kaldırım
            foreach (float cz in horizStreets)
            {
                if (Mathf.Abs(cz - (-9.0f)) > 1.0f)
                {
                    CreateSidewalkBlock(roadGroup.transform, new Vector3(-112.0f + 3.65f, 0.04f, cz), new Vector3(1.3f, 0.08f, 6.0f), sidewalkMat);
                }
            }

            // 2.4 YAYA GEÇİTLERİ (SADECE YOLUN GERÇEKTEN DEVAM ETTİĞİ KOLLARA EKLENİR - AĞAÇLARA/DUVARA GİTMEZ)
            foreach (float ax in vertAvenues)
            {
                foreach (float cz in horizStreets)
                {
                    // Kuzey Kolu (Sadece kuzeye yol gidiyorsa, yani cz != 175m)
                    if (cz < 174.0f)
                    {
                        BuildZebraCrosswalkUnit(roadGroup.transform, new Vector3(ax, 0.025f, cz + 3.6f), true, crosswalkWhiteMat);
                    }

                    // Güney Kolu (Sadece güneye yol gidiyorsa, yani cz != -125m)
                    if (cz > -124.0f)
                    {
                        BuildZebraCrosswalkUnit(roadGroup.transform, new Vector3(ax, 0.025f, cz - 3.6f), true, crosswalkWhiteMat);
                    }

                    // Doğu Kolu (Sadece doğuya yol gidiyorsa: ax != -112m veya köprü cz == -9m)
                    if (ax < -113.0f || Mathf.Abs(cz - (-9.0f)) < 1.0f)
                    {
                        BuildZebraCrosswalkUnit(roadGroup.transform, new Vector3(ax + 3.6f, 0.025f, cz), false, crosswalkWhiteMat);
                    }

                    // Batı Kolu (Sadece batıya yol gidiyorsa: ax != -226m veya ana yol cz == -9m)
                    if (ax > -225.0f || Mathf.Abs(cz - (-9.0f)) < 1.0f)
                    {
                        BuildZebraCrosswalkUnit(roadGroup.transform, new Vector3(ax - 3.6f, 0.025f, cz), false, crosswalkWhiteMat);
                    }
                }
            }

            // 2.5 Sokak Lambaları
            foreach (float ax in vertAvenues)
            {
                for (float lz = -105f; lz <= 160f; lz += 35f)
                {
                    BuildCleanStreetLamp(roadGroup.transform, new Vector3(ax + 3.65f, 0f, lz));
                }
            }
        }

        private static void BuildWestHighwayFarExtension(Transform parent)
        {
            Transform extGroup = new GameObject("West_Highway_Horizon_Extension").transform;
            extGroup.SetParent(parent, false);

            Material asphaltMat = GetMaterial("WestAsphaltMat", new Color(0.18f, 0.19f, 0.21f), 0.1f, 0.2f);
            Material sidewalkMat = GetMaterial("CleanSidewalkMat", new Color(0.72f, 0.74f, 0.76f), 0.0f, 0.3f);
            Material pureWhiteLineMat = GetMaterial("PureWhiteRoadLineMat", new Color(0.98f, 0.98f, 0.98f), 0.0f, 0.2f);
            Material grassMat = GetMaterial("WestGrassTerrainMat", new Color(0.28f, 0.58f, 0.22f), 0.05f, 0.1f);

            float startX = -226.0f;
            float endX = -360.0f;
            float lenX = Mathf.Abs(endX - startX); // 134m
            float centerX = (startX + endX) / 2.0f; // -293.0f

            // 1. Zemin Çim Kuşağı
            GameObject grassGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grassGround.name = "Highway_Grass_Strip";
            grassGround.transform.SetParent(extGroup, false);
            grassGround.transform.position = new Vector3(centerX, -0.06f, -9.0f);
            grassGround.transform.localScale = new Vector3(lenX + 10.0f, 0.1f, 36.0f);
            grassGround.GetComponent<Renderer>().sharedMaterial = grassMat;
            Object.Destroy(grassGround.GetComponent<Collider>());

            // 2. Asfalt Yol Gövdesi (6m genişlik)
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "West_Highway_Asphalt_Extension";
            road.transform.SetParent(extGroup, false);
            road.transform.position = new Vector3(centerX, 0.01f, -9.0f);
            road.transform.localScale = new Vector3(lenX, 0.02f, 6.0f);
            road.GetComponent<Renderer>().sharedMaterial = asphaltMat;
            Object.Destroy(road.GetComponent<Collider>());

            // 3. Beyaz Kesikli Şerit Çizgileri
            for (float x = startX - 3.6f; x >= endX + 3.6f; x -= 3.6f)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Lane_Stripe_Extension";
                stripe.transform.SetParent(extGroup, false);
                stripe.transform.position = new Vector3(x, 0.022f, -9.0f);
                stripe.transform.localScale = new Vector3(1.8f, 0.01f, 0.22f);
                stripe.GetComponent<Renderer>().sharedMaterial = pureWhiteLineMat;
                Object.Destroy(stripe.GetComponent<Collider>());
            }

            // 4. Kuzey ve Güney Kaldırımları
            CreateSidewalkBlock(extGroup, new Vector3(centerX, 0.04f, -9.0f + 3.65f), new Vector3(lenX, 0.08f, 1.3f), sidewalkMat);
            CreateSidewalkBlock(extGroup, new Vector3(centerX, 0.04f, -9.0f - 3.65f), new Vector3(lenX, 0.08f, 1.3f), sidewalkMat);

            // 5. Yol Kenarı Orman / Ağaç Sırası ve Sokak Lambaları (Ufuk Dekorasyonu)
            for (float x = startX - 10.0f; x >= endX + 10.0f; x -= 14.0f)
            {
                // Kuzey Ağaçları
                BuildRiverPromenadeTree(extGroup, new Vector3(x, 0f, 2.5f));
                // Güney Ağaçları
                BuildRiverPromenadeTree(extGroup, new Vector3(x, 0f, -20.5f));
            }

            for (float x = startX - 25.0f; x >= endX + 25.0f; x -= 35.0f)
            {
                BuildCleanStreetLamp(extGroup, new Vector3(x, 0f, -9.0f + 3.65f));
            }
        }

        private static void CreateSidewalkBlock(Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = "Sidewalk_Block";
            sw.transform.SetParent(parent, false);
            sw.transform.position = pos;
            sw.transform.localScale = size;
            sw.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(sw.GetComponent<Collider>());
        }

        private static void BuildZebraCrosswalkUnit(Transform parent, Vector3 pos, bool acrossVerticalAvenue, Material whiteMat)
        {
            GameObject cw = new GameObject("Zebra_Crosswalk");
            cw.transform.SetParent(parent, false);
            cw.transform.position = pos;

            if (acrossVerticalAvenue)
            {
                // Dikey yolu kesen yaya geçidi (X ekseninde sıralı beyaz çizgiler)
                for (float offset = -2.2f; offset <= 2.2f; offset += 0.70f)
                {
                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stripe.name = "Zebra_Stripe";
                    stripe.transform.SetParent(cw.transform, false);
                    stripe.transform.localPosition = new Vector3(offset, 0f, 0f);
                    stripe.transform.localScale = new Vector3(0.35f, 0.01f, 1.8f);
                    stripe.GetComponent<Renderer>().sharedMaterial = whiteMat;
                    Object.Destroy(stripe.GetComponent<Collider>());
                }
            }
            else
            {
                // Yatay yolu kesen yaya geçidi (Z ekseninde sıralı beyaz çizgiler)
                for (float offset = -2.2f; offset <= 2.2f; offset += 0.70f)
                {
                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stripe.name = "Zebra_Stripe";
                    stripe.transform.SetParent(cw.transform, false);
                    stripe.transform.localPosition = new Vector3(0f, 0f, offset);
                    stripe.transform.localScale = new Vector3(1.8f, 0.01f, 0.35f);
                    stripe.GetComponent<Renderer>().sharedMaterial = whiteMat;
                    Object.Destroy(stripe.GetComponent<Collider>());
                }
            }
        }

        private static void BuildCleanStreetLamp(Transform parent, Vector3 pos)
        {
            GameObject lamp = new GameObject("Clean_Street_Lamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = pos;

            Material poleMat = GetMaterial("CleanLampPoleMat", new Color(0.22f, 0.24f, 0.28f), 0.6f, 0.7f);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(lamp.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            pole.transform.localScale = new Vector3(0.10f, 1.8f, 0.10f);
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;
            Object.Destroy(pole.GetComponent<Collider>());

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(lamp.transform, false);
            arm.transform.localPosition = new Vector3(-0.35f, 3.55f, 0f);
            arm.transform.localScale = new Vector3(0.85f, 0.08f, 0.08f);
            arm.GetComponent<Renderer>().sharedMaterial = poleMat;
            Object.Destroy(arm.GetComponent<Collider>());

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(lamp.transform, false);
            bulb.transform.localPosition = new Vector3(-0.75f, 3.45f, 0f);
            bulb.transform.localScale = new Vector3(0.30f, 0.30f, 0.30f);
            bulb.GetComponent<Renderer>().sharedMaterial = GetMaterial("CleanLampBulbMat", new Color(0.35f, 0.35f, 0.38f), 0.1f, 0.5f);
            Object.Destroy(bulb.GetComponent<Collider>());

            GameObject lightChild = new GameObject("Light");
            lightChild.transform.SetParent(lamp.transform, false);
            lightChild.transform.localPosition = new Vector3(-0.75f, 3.35f, 0f);
            Light pLight = lightChild.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.90f, 0.60f);
            pLight.intensity = 2.4f;
            pLight.range = 12.0f;
            pLight.shadows = LightShadows.None;
            pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStreetLamp(bulb, pLight);
            }
        }

        #endregion

        #region 3. 12 Ultra-Detailed Luxury Villas (Kuzey-Batı Bölgesi)

        private static void BuildLuxuryVillaEstate(Transform parent)
        {
            GameObject estateGroup = new GameObject("Luxury_Villa_Estate_District");
            estateGroup.transform.SetParent(parent, false);

            // 3 Sütun x 4 Satır = 12 Tamamen Benzersiz Lüks Villa
            // Sütun 1: X = -207.0m (4 Adet Villa)
            // Sütun 2: X = -169.0m (4 Adet Villa)
            // Sütun 3: X = -131.0m (4 Adet Villa)
            float[] villaColXs = new float[] { -207.0f, -169.0f, -131.0f };
            float[] villaRowZs = new float[] { 22.0f, 65.0f, 108.0f, 151.0f };
            Vector2 villaParcelSize = new Vector2(27.0f, 33.0f); // Yollara/kaldırımlara asla taşmayan güvenli parsel alanı

            int villaIndex = 1;
            for (int col = 0; col < 3; col++)
            {
                float px = villaColXs[col];
                for (int row = 0; row < 4; row++)
                {
                    float pz = villaRowZs[row];
                    BuildUniqueArtisanVilla(
                        estateGroup.transform,
                        new Vector3(px, 0f, pz),
                        villaParcelSize,
                        villaIndex++
                    );
                }
            }
        }

        private static void BuildUniqueArtisanVilla(
            Transform parent,
            Vector3 parcelCenter,
            Vector2 parcelSize,
            int villaId)
        {
            GameObject villaParcel = new GameObject($"Luxury_Villa_{villaId}");
            villaParcel.transform.SetParent(parent, false);
            villaParcel.transform.position = parcelCenter;

            // 1. ZEMİN ÇİM PARSELİ (Yola taşmayan güvenli yeşil bahçe alanı)
            Material lawnMat = GetMaterial("VillaLawnMat", new Color(0.28f, 0.62f, 0.24f), 0.0f, 0.1f);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Parcel_Lawn";
            ground.transform.SetParent(villaParcel.transform, false);
            ground.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            ground.transform.localScale = new Vector3(parcelSize.x, 0.01f, parcelSize.y);
            ground.GetComponent<Renderer>().sharedMaterial = lawnMat;
            Object.Destroy(ground.GetComponent<Collider>());

            // 2. BEYAZ ÇİT SİSTEMİ (Doğu/Cadde cephesinde giriş kapısı olan nizami beyaz çitler)
            Material whiteFenceMat = GetMaterial("VillaWhiteFenceMat", new Color(0.96f, 0.96f, 0.98f), 0.0f, 0.3f);
            Material whitePillarMat = GetMaterial("VillaWhitePillarMat", new Color(0.92f, 0.93f, 0.95f), 0.1f, 0.4f);
            Material fenceLampMat = GetMaterial("VillaFenceLampMat", new Color(1.0f, 0.95f, 0.75f), 0.1f, 0.9f);
            float gateWidth = 2.6f;
            float gateZ = 0f;
            BuildVillaWhiteFences(villaParcel.transform, parcelSize, gateZ, gateWidth, whiteFenceMat, whitePillarMat, fenceLampMat);

            // 3. VİLLA MİMARİSİ (Arka tarafa konumlandırılmış, havuza bol mesafeli, zengin detaylı)
            Vector3 buildingLocalPos = new Vector3(-6.5f, 0f, 0f);
            Vector3 doorLocalPos;
            int floorCount;
            BuildUniqueVillaStructure(villaParcel.transform, villaId, buildingLocalPos, out doorLocalPos, out floorCount);

            // 4. YOLA VE KALDIRIMA KADAR UZANAN TAŞ YÜRÜYÜŞ YOLU (Bahçe Işıkları ile Donatılmış)
            Material walkwayMat = GetMaterial("VillaWalkwayMat", new Color(0.82f, 0.80f, 0.76f), 0.1f, 0.3f);
            Vector3 walkwayStart = new Vector3(buildingLocalPos.x + doorLocalPos.x, 0.04f, buildingLocalPos.z + doorLocalPos.z);
            Vector3 walkwayEnd = new Vector3(15.35f, 0.04f, gateZ);
            BuildVillaWalkway(villaParcel.transform, walkwayStart, walkwayEnd, walkwayMat);

            // 5. ÖZEL ÖN BAHÇE YÜZME HAVUZU (Villadan 4.5+ Metre Uzakta, Lounge Koltuklu & Şezlonglu)
            Material poolWaterMat = GetMaterial("VillaPoolWaterMat", new Color(0.10f, 0.78f, 0.92f, 0.85f), 0.9f, 0.95f);
            Material poolMarbleMat = GetMaterial("VillaPoolMarbleMat", new Color(0.95f, 0.95f, 0.93f), 0.1f, 0.5f);
            Material deckMat = GetMaterial("VillaDeckWoodMat", new Color(0.68f, 0.45f, 0.25f), 0.1f, 0.4f);
            Vector3 poolPos = new Vector3(5.8f, 0f, -7.2f);
            BuildVillaCustomPool(villaParcel.transform, villaId, poolPos, poolWaterMat, poolMarbleMat, deckMat);

            // 6. ÖN BAHÇE AĞACI & PEYZAJ
            Vector3 treePos = new Vector3(5.8f, 0f, 7.2f);
            BuildVillaCustomTree(villaParcel.transform, villaId, treePos);

            // 7. ÖZEL OTOPARK VE PARK HALİNDE LÜKS ARABA
            BuildVillaPrivateCarport(villaParcel.transform, new Vector3(-6.5f, 0f, -11.5f), villaId);
        }

        #region Villa Private Carport & Parked Car

        private static void BuildVillaPrivateCarport(Transform parent, Vector3 localPos, int villaId)
        {
            GameObject carport = new GameObject("Villa_Private_Carport");
            carport.transform.SetParent(parent, false);
            carport.transform.localPosition = localPos;

            Material asphaltMat = GetMaterial("CarportPavementMat", new Color(0.30f, 0.32f, 0.35f), 0.1f, 0.3f);
            Material pergolaWoodMat = GetMaterial("CarportPergolaMat", new Color(0.20f, 0.22f, 0.24f), 0.2f, 0.5f);

            // Otopark Zemin Taşı
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Carport_Pavement";
            pad.transform.SetParent(carport.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            pad.transform.localScale = new Vector3(6.5f, 0.03f, 3.8f);
            pad.GetComponent<Renderer>().sharedMaterial = asphaltMat;
            Object.Destroy(pad.GetComponent<Collider>());

            // Ahşap Gölgelik Direkleri ve Çatı Izgarası (Pergola Carport)
            for (int dx = -1; dx <= 1; dx += 2)
            {
                for (int dz = -1; dz <= 1; dz += 2)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = "Carport_Post";
                    post.transform.SetParent(carport.transform, false);
                    post.transform.localPosition = new Vector3(dx * 2.9f, 1.25f, dz * 1.6f);
                    post.transform.localScale = new Vector3(0.15f, 2.5f, 0.15f);
                    post.GetComponent<Renderer>().sharedMaterial = pergolaWoodMat;
                    Object.Destroy(post.GetComponent<Collider>());
                }
            }

            for (float x = -2.8f; x <= 2.8f; x += 0.8f)
            {
                GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = "Carport_Roof_Slat";
                slat.transform.SetParent(carport.transform, false);
                slat.transform.localPosition = new Vector3(x, 2.55f, 0f);
                slat.transform.localScale = new Vector3(0.12f, 0.08f, 3.6f);
                slat.GetComponent<Renderer>().sharedMaterial = pergolaWoodMat;
                Object.Destroy(slat.GetComponent<Collider>());
            }

            // Park Halindeki Lüks Villa Arabası
            Color[] carColors = new Color[]
            {
                new Color(0.85f, 0.12f, 0.15f), // Kırmızı Spor
                new Color(0.12f, 0.14f, 0.18f), // Metalik Siyah
                new Color(0.95f, 0.95f, 0.96f), // Kar Beyazı
                new Color(0.15f, 0.45f, 0.85f), // Gece Mavisi
                new Color(0.88f, 0.72f, 0.20f), // Altın Sarısı
                new Color(0.70f, 0.72f, 0.75f)  // Platin Gümüş
            };

            Color bodyCol = carColors[(villaId - 1) % carColors.Length];
            Material carBodyMat = GetMaterial($"ParkedCarBodyMat_{villaId}", bodyCol, 0.7f, 0.85f);
            Material carGlassMat = GetMaterial("ParkedCarGlassMat", new Color(0.15f, 0.20f, 0.25f), 0.8f, 0.95f);
            Material wheelMat = GetMaterial("ParkedCarWheelMat", new Color(0.10f, 0.10f, 0.10f), 0.1f, 0.2f);

            GameObject carObj = new GameObject("Parked_Luxury_Car");
            carObj.transform.SetParent(carport.transform, false);
            carObj.transform.localPosition = new Vector3(0f, 0f, 0f);

            // Gövde Altı
            GameObject carBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carBody.name = "Car_Body";
            carBody.transform.SetParent(carObj.transform, false);
            carBody.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            carBody.transform.localScale = new Vector3(4.2f, 0.50f, 1.85f);
            carBody.GetComponent<Renderer>().sharedMaterial = carBodyMat;
            Object.Destroy(carBody.GetComponent<Collider>());

            // Kabin ve Camlar
            GameObject carCabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carCabin.name = "Car_Cabin";
            carCabin.transform.SetParent(carObj.transform, false);
            carCabin.transform.localPosition = new Vector3(-0.2f, 0.85f, 0f);
            carCabin.transform.localScale = new Vector3(2.2f, 0.45f, 1.60f);
            carCabin.GetComponent<Renderer>().sharedMaterial = carGlassMat;
            Object.Destroy(carCabin.GetComponent<Collider>());

            // 4 Tekerlek
            for (int wx = -1; wx <= 1; wx += 2)
            {
                for (int wz = -1; wz <= 1; wz += 2)
                {
                    GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(carObj.transform, false);
                    wheel.transform.localPosition = new Vector3(wx * 1.3f, 0.28f, wz * 0.92f);
                    wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    wheel.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
                    wheel.GetComponent<Renderer>().sharedMaterial = wheelMat;
                    Object.Destroy(wheel.GetComponent<Collider>());
                }
            }
        }

        #endregion

        #region Villa White Fence System

        private static void BuildVillaWhiteFences(
            Transform parent,
            Vector2 parcelSize,
            float gateZ,
            float gateWidth,
            Material fenceMat,
            Material pillarMat,
            Material lampMat)
        {
            GameObject fenceGroup = new GameObject("White_Perimeter_Fence");
            fenceGroup.transform.SetParent(parent, false);

            float halfW = parcelSize.x / 2f;
            float halfD = parcelSize.y / 2f;

            // 1. Köşe Sütunları
            Vector3[] cornerPositions = new Vector3[]
            {
                new Vector3(-halfW, 0f, -halfD),
                new Vector3(halfW, 0f, -halfD),
                new Vector3(-halfW, 0f, halfD),
                new Vector3(halfW, 0f, halfD)
            };

            foreach (Vector3 cp in cornerPositions)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Fence_Corner_Pillar";
                pillar.transform.SetParent(fenceGroup.transform, false);
                pillar.transform.localPosition = cp + new Vector3(0f, 0.65f, 0f);
                pillar.transform.localScale = new Vector3(0.35f, 1.30f, 0.35f);
                pillar.GetComponent<Renderer>().sharedMaterial = pillarMat;
                Object.Destroy(pillar.GetComponent<Collider>());

                // Sütun Şapkası
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cap.name = "Pillar_Cap";
                cap.transform.SetParent(pillar.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                cap.transform.localScale = new Vector3(1.2f, 0.12f, 1.2f);
                cap.GetComponent<Renderer>().sharedMaterial = pillarMat;
                Object.Destroy(cap.GetComponent<Collider>());
            }

            // 2. Arka Çit (Batı / -X)
            BuildFenceSegment(fenceGroup.transform, new Vector3(-halfW, 0f, 0f), parcelSize.y - 0.7f, false, fenceMat, pillarMat);

            // 3. Yan Çitler (Kuzey / +Z ve Güney / -Z)
            BuildFenceSegment(fenceGroup.transform, new Vector3(0f, 0f, halfD), parcelSize.x - 0.7f, true, fenceMat, pillarMat);
            BuildFenceSegment(fenceGroup.transform, new Vector3(0f, 0f, -halfD), parcelSize.x - 0.7f, true, fenceMat, pillarMat);

            // 4. Ön Çit (Doğu / +X - Caddeye Bakan Cephe | Giriş Kapısı Boşluklu)
            float frontSpanZ = (parcelSize.y - gateWidth) / 2f - 0.35f;
            float topCenterZ = (gateWidth / 2f) + (frontSpanZ / 2f) + 0.18f;
            float btmCenterZ = -((gateWidth / 2f) + (frontSpanZ / 2f) + 0.18f);

            if (frontSpanZ > 1.0f)
            {
                BuildFenceSegment(fenceGroup.transform, new Vector3(halfW, 0f, topCenterZ), frontSpanZ, false, fenceMat, pillarMat);
                BuildFenceSegment(fenceGroup.transform, new Vector3(halfW, 0f, btmCenterZ), frontSpanZ, false, fenceMat, pillarMat);
            }

            // 5. Giriş Kapısı İki Beyaz Sütunu ve Gece Yanan Fenerleri (Doğu Sınırında)
            float[] gatePillarZs = new float[] { gateZ - (gateWidth / 2f), gateZ + (gateWidth / 2f) };
            foreach (float gz in gatePillarZs)
            {
                GameObject gp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gp.name = "Entrance_Gate_Pillar";
                gp.transform.SetParent(fenceGroup.transform, false);
                gp.transform.localPosition = new Vector3(halfW, 0.70f, gz);
                gp.transform.localScale = new Vector3(0.40f, 1.40f, 0.40f);
                gp.GetComponent<Renderer>().sharedMaterial = pillarMat;
                Object.Destroy(gp.GetComponent<Collider>());

                // Sütun Üstü Fener Küresi
                GameObject globe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                globe.name = "Pillar_Lamp_Globe";
                globe.transform.SetParent(gp.transform, false);
                globe.transform.localPosition = new Vector3(0f, 0.58f, 0f);
                globe.transform.localScale = new Vector3(0.70f, 0.20f, 0.70f);
                globe.GetComponent<Renderer>().sharedMaterial = lampMat;
                Object.Destroy(globe.GetComponent<Collider>());

                // Gece Işığı
                GameObject lObj = new GameObject("Gate_Light");
                lObj.transform.SetParent(gp.transform, false);
                lObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                Light gLight = lObj.AddComponent<Light>();
                gLight.type = LightType.Point;
                gLight.color = new Color(1.0f, 0.90f, 0.65f);
                gLight.intensity = 1.8f;
                gLight.range = 7.0f;
                gLight.shadows = LightShadows.None;
                gLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

                if (DayNightCycleManager.Instance != null)
                {
                    DayNightCycleManager.Instance.RegisterStreetLamp(globe, gLight);
                }
            }
        }

        private static void BuildFenceSegment(
            Transform parent,
            Vector3 centerPos,
            float length,
            bool isHorizontal,
            Material fenceMat,
            Material pillarMat)
        {
            GameObject seg = new GameObject("Fence_Segment");
            seg.transform.SetParent(parent, false);
            seg.transform.localPosition = centerPos;

            // Üst ve Alt Yatay Ray
            for (float y = 0.35f; y <= 0.95f; y += 0.55f)
            {
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "Fence_Rail";
                rail.transform.SetParent(seg.transform, false);
                rail.transform.localPosition = new Vector3(0f, y, 0f);
                rail.transform.localScale = isHorizontal ? new Vector3(length, 0.08f, 0.06f) : new Vector3(0.06f, 0.08f, length);
                rail.GetComponent<Renderer>().sharedMaterial = fenceMat;
                Object.Destroy(rail.GetComponent<Collider>());
            }

            // Dikey Beyaz Çıtalar
            float step = 0.55f;
            int slatCount = Mathf.FloorToInt(length / step);
            float startOffset = -((slatCount - 1) * step) / 2f;

            for (int i = 0; i < slatCount; i++)
            {
                float offset = startOffset + (i * step);
                GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = "Fence_Slat";
                slat.transform.SetParent(seg.transform, false);
                slat.transform.localPosition = isHorizontal ? new Vector3(offset, 0.60f, 0f) : new Vector3(0f, 0.60f, offset);
                slat.transform.localScale = isHorizontal ? new Vector3(0.08f, 1.05f, 0.04f) : new Vector3(0.04f, 1.05f, 0.08f);
                slat.GetComponent<Renderer>().sharedMaterial = fenceMat;
                Object.Destroy(slat.GetComponent<Collider>());
            }
        }

        #endregion

        #region Villa Walkway

        private static void BuildVillaWalkway(Transform parent, Vector3 startLocal, Vector3 endLocal, Material mat)
        {
            GameObject pathGroup = new GameObject("Villa_Stone_Pathway");
            pathGroup.transform.SetParent(parent, false);

            float pathWidth = 1.8f;
            Vector3 diff = endLocal - startLocal;
            float totalLen = diff.magnitude;
            int steps = Mathf.Max(4, Mathf.FloorToInt(totalLen / 1.0f));

            Material bollardMat = GetMaterial("BollardLightMat", new Color(0.20f, 0.22f, 0.24f), 0.5f, 0.7f);
            Material bollardGlowMat = GetMaterial("BollardGlowMat", new Color(1.0f, 0.95f, 0.70f), 0.1f, 0.9f);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 stepPos = Vector3.Lerp(startLocal, endLocal, t);

                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = $"Path_Slab_{i}";
                slab.transform.SetParent(pathGroup.transform, false);
                slab.transform.localPosition = stepPos;
                slab.transform.localScale = new Vector3(0.85f, 0.04f, pathWidth);
                slab.GetComponent<Renderer>().sharedMaterial = mat;
                Object.Destroy(slab.GetComponent<Collider>());

                // Yol Kenarı Gece Yanan Modern Bahçe Solar Lambaları (Her 3 adımda bir)
                if (i % 3 == 1 && i < steps - 1)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 bPos = stepPos + new Vector3(0f, 0.20f, side * (pathWidth / 2f + 0.35f));
                        GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        bollard.name = "Pathway_Bollard_Light";
                        bollard.transform.SetParent(pathGroup.transform, false);
                        bollard.transform.localPosition = bPos;
                        bollard.transform.localScale = new Vector3(0.12f, 0.20f, 0.12f);
                        bollard.GetComponent<Renderer>().sharedMaterial = bollardMat;
                        Object.Destroy(bollard.GetComponent<Collider>());

                        GameObject bGlobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        bGlobe.name = "Bollard_Globe";
                        bGlobe.transform.SetParent(bollard.transform, false);
                        bGlobe.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                        bGlobe.transform.localScale = new Vector3(0.85f, 0.35f, 0.85f);
                        bGlobe.GetComponent<Renderer>().sharedMaterial = bollardGlowMat;
                        Object.Destroy(bGlobe.GetComponent<Collider>());
                    }
                }
            }
        }

        #endregion

        #region Villa 12 Distinct Architectural Structures

        private static void BuildUniqueVillaStructure(
            Transform parent,
            int villaId,
            Vector3 buildingLocalPos,
            out Vector3 doorLocalPos,
            out int floorCount)
        {
            GameObject villaRoot = new GameObject($"Villa_Structure_{villaId}");
            villaRoot.transform.SetParent(parent, false);
            villaRoot.transform.localPosition = buildingLocalPos;

            // Renk ve Doku Paleti Kataloğu (12 Farklı Villa)
            Color[] primaryWallColors = new Color[]
            {
                new Color(0.97f, 0.97f, 0.98f), // 1: Kar Beyazı (Minimalist)
                new Color(0.88f, 0.84f, 0.78f), // 2: Doğal Traverten Bej
                new Color(0.24f, 0.26f, 0.28f), // 3: Antrasit Grafit
                new Color(0.92f, 0.93f, 0.95f), // 4: Buz Grisi
                new Color(0.62f, 0.40f, 0.22f), // 5: İskandinav Doğal Ahşap
                new Color(0.95f, 0.95f, 0.96f), // 6: Nehir Kordon Beyazı
                new Color(0.82f, 0.83f, 0.85f), // 7: Modern Platin Gri
                new Color(0.92f, 0.88f, 0.82f), // 8: Klasik Roma Mermeri
                new Color(0.98f, 0.98f, 0.99f), // 9: Bodrum / Ege Kireç Beyazı
                new Color(0.18f, 0.20f, 0.22f), // 10: Loft Siyahı
                new Color(0.35f, 0.32f, 0.28f), // 11: Zen Kömür Taşı
                new Color(0.94f, 0.94f, 0.96f)  // 12: VIP Kordon Köşkü
            };

            Color primaryColor = primaryWallColors[(villaId - 1) % primaryWallColors.Length];
            Material wallMat = GetMaterial($"VillaWallMat_{villaId}", primaryColor, 0.1f, 0.35f);
            Material accentWoodMat = GetMaterial("VillaAccentTeakMat", new Color(0.58f, 0.36f, 0.18f), 0.1f, 0.45f);
            Material darkTrimMat = GetMaterial("VillaDarkTrimMat", new Color(0.16f, 0.18f, 0.20f), 0.5f, 0.6f);
            Material glassMat = GetMaterial("VillaGlassMat", new Color(0.20f, 0.65f, 0.85f, 0.90f), 0.8f, 0.95f);

            // Kat Dağılımı: 6 Adet 2 Katlı, 6 Adet 3 Katlı
            bool is3Floor = (villaId == 3 || villaId == 4 || villaId == 6 || villaId == 8 || villaId == 10 || villaId == 12);
            floorCount = is3Floor ? 3 : 2;

            float villaW = 8.5f;  // X genişliği (Kompakt ve havuza bol mesafeli)
            float villaD = 13.0f; // Z derinliği
            float floorH = 3.2f;
            float totalH = floorCount * floorH;

            // 1. Temel Su Basmanı (Plinth)
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Plinth";
            plinth.transform.SetParent(villaRoot.transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            plinth.transform.localScale = new Vector3(villaW + 0.4f, 0.30f, villaD + 0.4f);
            plinth.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(plinth.GetComponent<Collider>());

            // 2. Zemin Kat Gövdesi
            GameObject groundFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groundFloor.name = "Ground_Floor";
            groundFloor.transform.SetParent(villaRoot.transform, false);
            groundFloor.transform.localPosition = new Vector3(0f, 0.30f + (floorH / 2f), 0f);
            groundFloor.transform.localScale = new Vector3(villaW, floorH, villaD);
            groundFloor.GetComponent<Renderer>().sharedMaterial = wallMat;
            Object.Destroy(groundFloor.GetComponent<Collider>());

            // Kat Silmesi 1
            GameObject slab1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab1.name = "Floor_Slab_1";
            slab1.transform.SetParent(villaRoot.transform, false);
            slab1.transform.localPosition = new Vector3(0f, 0.30f + floorH, 0f);
            slab1.transform.localScale = new Vector3(villaW + 0.5f, 0.20f, villaD + 0.5f);
            slab1.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(slab1.GetComponent<Collider>());

            // 3. 1. Kat (Doğuya / Caddeye Konsol Çıkmalı ve Balkonlu)
            float f1ShiftZ = ((villaId % 2 == 0) ? 0.8f : -0.8f);
            GameObject firstFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            firstFloor.name = "First_Floor";
            firstFloor.transform.SetParent(villaRoot.transform, false);
            firstFloor.transform.localPosition = new Vector3(0.4f, 0.30f + floorH + (floorH / 2f), f1ShiftZ);
            firstFloor.transform.localScale = new Vector3(villaW, floorH, villaD - 0.6f);
            firstFloor.GetComponent<Renderer>().sharedMaterial = (villaId % 3 == 0) ? accentWoodMat : wallMat;
            Object.Destroy(firstFloor.GetComponent<Collider>());

            // 1. Kat Balkonu ve Cam Korkuluk (Doğu / Cadde Cephesi)
            GameObject balconyGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            balconyGlass.name = "First_Floor_Balcony_Glass";
            balconyGlass.transform.SetParent(villaRoot.transform, false);
            balconyGlass.transform.localPosition = new Vector3((villaW / 2f) + 0.45f, 0.30f + floorH + 0.50f, -f1ShiftZ * 3.0f);
            balconyGlass.transform.localScale = new Vector3(0.08f, 0.90f, 4.8f);
            balconyGlass.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(balconyGlass.GetComponent<Collider>());

            // 1. Kat Üstü Ahşap Güneş Kırıcı Pergola
            for (float pz = -2.2f; pz <= 2.2f; pz += 0.8f)
            {
                GameObject pergolaBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pergolaBeam.name = "Balcony_Pergola_Beam";
                pergolaBeam.transform.SetParent(villaRoot.transform, false);
                pergolaBeam.transform.localPosition = new Vector3((villaW / 2f) + 0.6f, 0.30f + (floorH * 2f) + 0.05f, -f1ShiftZ * 3.0f + pz);
                pergolaBeam.transform.localScale = new Vector3(1.4f, 0.08f, 0.08f);
                pergolaBeam.GetComponent<Renderer>().sharedMaterial = accentWoodMat;
                Object.Destroy(pergolaBeam.GetComponent<Collider>());
            }

            // 4. 2. Kat (3 Katlı Villalar İçin Çatı Penthouse / Teras)
            if (is3Floor)
            {
                GameObject slab2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab2.name = "Floor_Slab_2";
                slab2.transform.SetParent(villaRoot.transform, false);
                slab2.transform.localPosition = new Vector3(0f, 0.30f + (floorH * 2f), 0f);
                slab2.transform.localScale = new Vector3(villaW + 0.5f, 0.20f, villaD + 0.5f);
                slab2.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
                Object.Destroy(slab2.GetComponent<Collider>());

                GameObject secondFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                secondFloor.name = "Second_Floor_Penthouse";
                secondFloor.transform.SetParent(villaRoot.transform, false);
                secondFloor.transform.localPosition = new Vector3(-0.4f, 0.30f + (floorH * 2f) + (floorH / 2f), -f1ShiftZ * 0.8f);
                secondFloor.transform.localScale = new Vector3(villaW - 2.0f, floorH, villaD - 3.2f);
                secondFloor.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
                Object.Destroy(secondFloor.GetComponent<Collider>());

                // Çatı Penthouse Teras Camı (Cadde Cephesi)
                GameObject roofGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roofGlass.name = "Rooftop_Terrace_Glass";
                roofGlass.transform.SetParent(villaRoot.transform, false);
                roofGlass.transform.localPosition = new Vector3(2.0f, 0.30f + (floorH * 2f) + 0.50f, f1ShiftZ * 2.6f);
                roofGlass.transform.localScale = new Vector3(0.08f, 0.90f, 4.4f);
                roofGlass.GetComponent<Renderer>().sharedMaterial = glassMat;
                Object.Destroy(roofGlass.GetComponent<Collider>());
            }

            // Çatı Parapeti
            float roofTopY = 0.30f + totalH;
            GameObject roofParapet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofParapet.name = "Roof_Parapet";
            roofParapet.transform.SetParent(villaRoot.transform, false);
            roofParapet.transform.localPosition = new Vector3(0f, roofTopY + 0.15f, 0f);
            roofParapet.transform.localScale = new Vector3(villaW + 0.3f, 0.30f, villaD + 0.3f);
            roofParapet.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(roofParapet.GetComponent<Collider>());

            // Giriş Kapısı ve Sundurması (Doğu / Cadde Cephesinde)
            doorLocalPos = new Vector3((villaW / 2f) + 0.02f, 0.30f, 0f);
            BuildVillaEntranceDoor(villaRoot.transform, doorLocalPos, darkTrimMat, accentWoodMat);

            // Kaliteli Camlar ve Gece Bazılarının Işığı Yanacak Şekilde Windows
            BuildVillaDetailedWindows(villaRoot.transform, villaW, villaD, floorCount, floorH, glassMat, darkTrimMat);

            // Fiziksel Engel & NavMesh
            BoxCollider col = villaRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, totalH / 2f, 0f);
            col.size = new Vector3(villaW + 0.4f, totalH, villaD + 0.4f);

            NavMeshObstacle obs = villaRoot.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.center = col.center;
            obs.size = col.size;
            obs.carving = true;
        }

        private static void BuildVillaEntranceDoor(Transform parent, Vector3 localPos, Material frameMat, Material doorMat)
        {
            GameObject entrance = new GameObject("Entrance_Porch");
            entrance.transform.SetParent(parent, false);
            entrance.transform.localPosition = localPos;

            // Kapı Kasası (Doğuya Bakar)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Door_Frame";
            frame.transform.SetParent(entrance.transform, false);
            frame.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            frame.transform.localScale = new Vector3(0.12f, 2.5f, 1.6f);
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(frame.GetComponent<Collider>());

            // Ahşap Kapı Kanadı
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Door_Panel";
            panel.transform.SetParent(entrance.transform, false);
            panel.transform.localPosition = new Vector3(0.02f, 1.25f, 0f);
            panel.transform.localScale = new Vector3(0.08f, 2.3f, 1.35f);
            panel.GetComponent<Renderer>().sharedMaterial = doorMat;
            Object.Destroy(panel.GetComponent<Collider>());

            // Krom Kapı Kolu
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Door_Handle";
            handle.transform.SetParent(entrance.transform, false);
            handle.transform.localPosition = new Vector3(0.08f, 1.15f, 0.50f);
            handle.transform.localScale = new Vector3(0.06f, 0.35f, 0.08f);
            handle.GetComponent<Renderer>().sharedMaterial = GetMaterial("HandleChromeMat", new Color(0.90f, 0.90f, 0.92f), 0.9f, 0.9f);
            Object.Destroy(handle.GetComponent<Collider>());

            // Giriş Saçağı (Canopy)
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Porch_Canopy";
            canopy.transform.SetParent(entrance.transform, false);
            canopy.transform.localPosition = new Vector3(0.60f, 2.65f, 0f);
            canopy.transform.localScale = new Vector3(1.3f, 0.15f, 2.4f);
            canopy.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(canopy.GetComponent<Collider>());

            // Giriş Sundurma Lambası
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "Porch_Lamp";
            lamp.transform.SetParent(entrance.transform, false);
            lamp.transform.localPosition = new Vector3(0.60f, 2.50f, 0f);
            lamp.transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);
            lamp.GetComponent<Renderer>().sharedMaterial = GetMaterial("PorchLampMat", new Color(1.0f, 0.95f, 0.70f), 0.1f, 0.9f);
            Object.Destroy(lamp.GetComponent<Collider>());

            // Gece Işığı
            GameObject lightChild = new GameObject("Light");
            lightChild.transform.SetParent(entrance.transform, false);
            lightChild.transform.localPosition = new Vector3(0.60f, 2.40f, 0f);
            Light pLight = lightChild.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.90f, 0.65f);
            pLight.intensity = 2.0f;
            pLight.range = 8.0f;
            pLight.shadows = LightShadows.None;
            pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStreetLamp(lamp, pLight);
            }
        }

        private static void BuildVillaDetailedWindows(
            Transform parent,
            float width,
            float depth,
            int floors,
            float floorH,
            Material glassMat,
            Material frameMat)
        {
            for (int f = 0; f < floors; f++)
            {
                float winY = 0.30f + (f * floorH) + 1.55f;
                // Akşam olunca bazılarının ışığı yansın (Gerçekçi Rastgelelik: %65 ihtimalle odada ışık yanar)
                bool isFloorLit = (Random.value < 0.65f);

                if (isFloorLit)
                {
                    GameObject lightObj = new GameObject($"Villa_Interior_Light_F{f + 1}");
                    lightObj.transform.SetParent(parent, false);
                    lightObj.transform.localPosition = new Vector3(0f, winY, 0f);
                    Light pLight = lightObj.AddComponent<Light>();
                    pLight.type = LightType.Point;
                    pLight.color = new Color(1.0f, 0.92f, 0.65f);
                    pLight.intensity = 2.2f;
                    pLight.range = 11.0f;
                    pLight.shadows = LightShadows.None;
                    pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

                    if (DayNightCycleManager.Instance != null)
                    {
                        DayNightCycleManager.Instance.RegisterStoreInteriorLight(pLight);
                    }
                }

                // Ön Panoramik Pencereler (Doğu / Cadde ve Havuz Cephesi)
                bool frontLit = isFloorLit && (Random.value < 0.85f);
                CreateFramedWindow(parent, new Vector3((width / 2f) + 0.04f, winY, -3.0f), new Vector2(3.2f, 2.0f), Vector3.right, frontLit, glassMat, frameMat);
                CreateFramedWindow(parent, new Vector3((width / 2f) + 0.04f, winY, 3.0f), new Vector2(3.2f, 2.0f), Vector3.right, isFloorLit, glassMat, frameMat);

                // Yan Pencereler (Kuzey ve Güney)
                bool northLit = isFloorLit && (Random.value < 0.70f);
                bool southLit = isFloorLit && (Random.value < 0.70f);
                CreateFramedWindow(parent, new Vector3(0f, winY, (depth / 2f) + 0.04f), new Vector2(3.6f, 1.8f), Vector3.forward, northLit, glassMat, frameMat);
                CreateFramedWindow(parent, new Vector3(0f, winY, -(depth / 2f) - 0.04f), new Vector2(3.6f, 1.8f), Vector3.back, southLit, glassMat, frameMat);

                // Arka Pencereler (Batı)
                bool rearLit = isFloorLit && (Random.value < 0.50f);
                CreateFramedWindow(parent, new Vector3(-(width / 2f) - 0.04f, winY, 0f), new Vector2(3.6f, 1.8f), Vector3.left, rearLit, glassMat, frameMat);
            }
        }

        private static void CreateFramedWindow(
            Transform parent,
            Vector3 pos,
            Vector2 size,
            Vector3 outwardDir,
            bool isLitTonight,
            Material glassMat,
            Material frameMat)
        {
            GameObject winGroup = new GameObject("Window_Unit");
            winGroup.transform.SetParent(parent, false);
            winGroup.transform.localPosition = pos;

            bool isXAxis = Mathf.Abs(outwardDir.x) > 0.5f;

            // Siyah/Antrasit Kaliteli Dış Çerçeve
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(winGroup.transform, false);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = isXAxis ? new Vector3(0.12f, size.y + 0.15f, size.x + 0.15f) : new Vector3(size.x + 0.15f, size.y + 0.15f, 0.12f);
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(frame.GetComponent<Collider>());

            // Cam Pane (Gündüz Şeffaf Yansımalı Cam / Gece Işıltılı Sarı Cam)
            GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
            win.name = isLitTonight ? "Apartment_Window_Glass_Lit" : "Apartment_Window_Glass_Dark";
            win.transform.SetParent(winGroup.transform, false);
            win.transform.localPosition = isXAxis ? new Vector3(outwardDir.x * 0.02f, 0f, 0f) : new Vector3(0f, 0f, outwardDir.z * 0.02f);
            win.transform.localScale = isXAxis ? new Vector3(0.06f, size.y, size.x) : new Vector3(size.x, size.y, 0.06f);

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

            // Beyaz/Açık Gri Taş Denizlik (Sill)
            GameObject sill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sill.name = "Window_Sill";
            sill.transform.SetParent(winGroup.transform, false);
            sill.transform.localPosition = isXAxis ? new Vector3(outwardDir.x * 0.06f, -(size.y / 2f) - 0.08f, 0f) : new Vector3(0f, -(size.y / 2f) - 0.08f, outwardDir.z * 0.06f);
            sill.transform.localScale = isXAxis ? new Vector3(0.20f, 0.12f, size.x + 0.25f) : new Vector3(size.x + 0.25f, 0.12f, 0.20f);
            sill.GetComponent<Renderer>().sharedMaterial = frameMat;
            Object.Destroy(sill.GetComponent<Collider>());

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterApartmentWindow(win, isLitTonight);
            }
        }

        #endregion

        #region Villa 12 Custom Pools

        private static void BuildVillaCustomPool(
            Transform parent,
            int poolStyle,
            Vector3 localPos,
            Material waterMat,
            Material marbleMat,
            Material deckMat)
        {
            GameObject poolGroup = new GameObject($"Custom_Pool_Style_{poolStyle}");
            poolGroup.transform.SetParent(parent, false);
            poolGroup.transform.localPosition = localPos;

            // Havuz Boyutları (X genişliği: 5.5m, Z derinliği: 6.5m - Villaya asla temas etmez, bol mesafeli)
            Vector2 poolSize = new Vector2(5.5f, 6.5f);

            // 1. Ahşap / Taş Güverte (Deck)
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Pool_Deck";
            deck.transform.SetParent(poolGroup.transform, false);
            deck.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            deck.transform.localScale = new Vector3(poolSize.x + 2.0f, 0.04f, poolSize.y + 2.0f);
            deck.GetComponent<Renderer>().sharedMaterial = (poolStyle % 2 == 0) ? deckMat : marbleMat;
            Object.Destroy(deck.GetComponent<Collider>());

            // 2. Mermer Küpeşte
            GameObject marble = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marble.name = "Pool_Marble";
            marble.transform.SetParent(poolGroup.transform, false);
            marble.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            marble.transform.localScale = new Vector3(poolSize.x + 0.4f, 0.08f, poolSize.y + 0.4f);
            marble.GetComponent<Renderer>().sharedMaterial = marbleMat;
            Object.Destroy(marble.GetComponent<Collider>());

            // 3. Turkuaz Havuz Suyu
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Pool_Water";
            water.transform.SetParent(poolGroup.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            water.transform.localScale = new Vector3(poolSize.x, 0.04f, poolSize.y);
            water.GetComponent<Renderer>().sharedMaterial = waterMat;
            Object.Destroy(water.GetComponent<Collider>());

            // 4. Gece Su Altı Neon Aydınlatması
            GameObject pLightObj = new GameObject("Pool_Underwater_Light");
            pLightObj.transform.SetParent(poolGroup.transform, false);
            pLightObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            Light poolLight = pLightObj.AddComponent<Light>();
            poolLight.type = LightType.Point;
            poolLight.color = new Color(0.12f, 0.85f, 0.98f);
            poolLight.intensity = 2.8f;
            poolLight.range = 8.5f;
            poolLight.shadows = LightShadows.None;
            poolLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStoreInteriorLight(poolLight);
            }

            // 5. Şezlonglar ve Güneş Şemsiyesi
            Color[] umbrellaColors = new Color[]
            {
                new Color(0.95f, 0.85f, 0.20f), // Sarı
                new Color(0.92f, 0.35f, 0.30f), // Kırmızı/Mercan
                new Color(0.25f, 0.65f, 0.90f), // Mavi
                new Color(0.95f, 0.95f, 0.96f)  // Beyaz
            };

            for (int i = -1; i <= 1; i += 2)
            {
                GameObject lounger = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lounger.name = "Sun_Lounger";
                lounger.transform.SetParent(poolGroup.transform, false);
                lounger.transform.localPosition = new Vector3(i * 1.3f, 0.16f, (poolSize.y / 2f) + 0.55f);
                lounger.transform.localScale = new Vector3(0.70f, 0.22f, 1.6f);
                lounger.GetComponent<Renderer>().sharedMaterial = GetMaterial("SunLoungerMat", new Color(0.95f, 0.95f, 0.96f), 0.0f, 0.3f);
                Object.Destroy(lounger.GetComponent<Collider>());
            }

            GameObject umbrella = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            umbrella.name = "Sun_Umbrella";
            umbrella.transform.SetParent(poolGroup.transform, false);
            umbrella.transform.localPosition = new Vector3(0f, 1.15f, (poolSize.y / 2f) + 0.70f);
            umbrella.transform.localScale = new Vector3(1.9f, 0.12f, 1.9f);
            umbrella.GetComponent<Renderer>().sharedMaterial = GetMaterial($"UmbrellaMat_{poolStyle}", umbrellaColors[poolStyle % umbrellaColors.Length], 0.0f, 0.3f);
            Object.Destroy(umbrella.GetComponent<Collider>());

            // 6. Havuz Kenarı Lüks L-Koltuk Lounge Takımı ve Sehpa
            Material sofaMat = GetMaterial("PoolSofaMat", new Color(0.92f, 0.92f, 0.94f), 0.0f, 0.3f);
            Material tableMat = GetMaterial("PoolTableWoodMat", new Color(0.55f, 0.35f, 0.18f), 0.1f, 0.4f);

            GameObject sofaMain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sofaMain.name = "Poolside_Sofa_Main";
            sofaMain.transform.SetParent(poolGroup.transform, false);
            sofaMain.transform.localPosition = new Vector3(-(poolSize.x / 2f) - 0.60f, 0.25f, 0f);
            sofaMain.transform.localScale = new Vector3(0.90f, 0.45f, 3.2f);
            sofaMain.GetComponent<Renderer>().sharedMaterial = sofaMat;
            Object.Destroy(sofaMain.GetComponent<Collider>());

            GameObject coffeeTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coffeeTable.name = "Poolside_Coffee_Table";
            coffeeTable.transform.SetParent(poolGroup.transform, false);
            coffeeTable.transform.localPosition = new Vector3(-(poolSize.x / 2f) - 0.60f, 0.18f, 2.2f);
            coffeeTable.transform.localScale = new Vector3(0.80f, 0.30f, 0.80f);
            coffeeTable.GetComponent<Renderer>().sharedMaterial = tableMat;
            Object.Destroy(coffeeTable.GetComponent<Collider>());
        }

        #endregion

        #region Villa 12 Custom Trees & Landscaping

        private static void BuildVillaCustomTree(Transform parent, int treeStyle, Vector3 localPos)
        {
            GameObject treeGroup = new GameObject($"Villa_Tree_Style_{treeStyle}");
            treeGroup.transform.SetParent(parent, false);
            treeGroup.transform.localPosition = localPos;

            Material trunkMat = GetMaterial("TreeTrunkMat", new Color(0.42f, 0.28f, 0.16f), 0.1f, 0.2f);
            Material foliageMat = GetMaterial("TreeFoliageMat", new Color(0.22f, 0.58f, 0.20f), 0.0f, 0.2f);
            Material oliveFoliageMat = GetMaterial("OliveFoliageMat", new Color(0.38f, 0.52f, 0.32f), 0.0f, 0.2f);
            Material sakuraFoliageMat = GetMaterial("SakuraFoliageMat", new Color(0.96f, 0.68f, 0.78f), 0.0f, 0.2f);
            Material goldenFoliageMat = GetMaterial("GoldenFoliageMat", new Color(0.85f, 0.68f, 0.22f), 0.0f, 0.2f);

            int type = treeStyle % 4;
            if (type == 0)
            {
                // Palmiye Ağacı (California Fan Palm)
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(treeGroup.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 2.2f, 0f);
                trunk.transform.localScale = new Vector3(0.35f, 2.2f, 0.35f);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
                Object.Destroy(trunk.GetComponent<Collider>());

                GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaves.name = "Palm_Leaves";
                leaves.transform.SetParent(treeGroup.transform, false);
                leaves.transform.localPosition = new Vector3(0f, 4.4f, 0f);
                leaves.transform.localScale = new Vector3(3.4f, 1.2f, 3.4f);
                leaves.GetComponent<Renderer>().sharedMaterial = foliageMat;
                Object.Destroy(leaves.GetComponent<Collider>());
            }
            else if (type == 1)
            {
                // Akdeniz Zeytin Ağacı (Olive Tree)
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(treeGroup.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                trunk.transform.localScale = new Vector3(0.45f, 1.5f, 0.45f);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
                Object.Destroy(trunk.GetComponent<Collider>());

                for (int i = 0; i < 3; i++)
                {
                    float angle = i * 120f * Mathf.Deg2Rad;
                    Vector3 leafOffset = new Vector3(Mathf.Cos(angle) * 0.7f, 3.0f + (i * 0.4f), Mathf.Sin(angle) * 0.7f);
                    GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leaf.name = "Olive_Cluster";
                    leaf.transform.SetParent(treeGroup.transform, false);
                    leaf.transform.localPosition = leafOffset;
                    leaf.transform.localScale = new Vector3(2.0f, 1.6f, 2.0f);
                    leaf.GetComponent<Renderer>().sharedMaterial = oliveFoliageMat;
                    Object.Destroy(leaf.GetComponent<Collider>());
                }
            }
            else if (type == 2)
            {
                // Japon Sakura / Pembe Çiçekli Zen Ağacı
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(treeGroup.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                trunk.transform.localScale = new Vector3(0.35f, 1.6f, 0.35f);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
                Object.Destroy(trunk.GetComponent<Collider>());

                GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaves.name = "Sakura_Crown";
                leaves.transform.SetParent(treeGroup.transform, false);
                leaves.transform.localPosition = new Vector3(0f, 3.4f, 0f);
                leaves.transform.localScale = new Vector3(3.2f, 2.0f, 3.2f);
                leaves.GetComponent<Renderer>().sharedMaterial = sakuraFoliageMat;
                Object.Destroy(leaves.GetComponent<Collider>());
            }
            else
            {
                // İskandinav Çam / Selvi Ağacı (Spruce / Cypress)
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(treeGroup.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                trunk.transform.localScale = new Vector3(0.30f, 1.0f, 0.30f);
                trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
                Object.Destroy(trunk.GetComponent<Collider>());

                for (int l = 0; l < 3; l++)
                {
                    GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cone.name = $"Spruce_Cone_{l}";
                    cone.transform.SetParent(treeGroup.transform, false);
                    cone.transform.localPosition = new Vector3(0f, 2.0f + (l * 1.1f), 0f);
                    float coneW = 2.6f - (l * 0.7f);
                    cone.transform.localScale = new Vector3(coneW, 0.60f, coneW);
                    cone.GetComponent<Renderer>().sharedMaterial = (treeStyle == 12) ? goldenFoliageMat : foliageMat;
                    Object.Destroy(cone.GetComponent<Collider>());
                }
            }

            // Ağaç Altı Çiçek / Çakıl Tarhı
            GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bed.name = "Tree_Bed";
            bed.transform.SetParent(treeGroup.transform, false);
            bed.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            bed.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
            bed.GetComponent<Renderer>().sharedMaterial = GetMaterial("TreeBedStoneMat", new Color(0.75f, 0.72f, 0.68f), 0.1f, 0.3f);
            Object.Destroy(bed.GetComponent<Collider>());
        }

        #endregion

        #endregion

        #region 4. Civic & Social District (South-West)

        private static void BuildCivicAndSocialDistrict(Transform parent)
        {
            GameObject civicGroup = new GameObject("Civic_And_Social_Facilities_District");
            civicGroup.transform.SetParent(parent, false);

            // SOL SÜTUN (X = -207m):
            // [İLK OKUL] (Atatürk İlkokulu - Yola bakan, pencereli, kapılı, bayraklı ve tam hizalı)
            BuildPrimarySchool(civicGroup.transform, new Vector3(-207.0f, 0f, -32.0f), new Vector2(30.0f, 38.0f));
            // [ŞEHİR KÜTÜPHANESİ & OKUMA/OYUN PARKI] (180 derece yola bakan, arka kapılı, taş yollu, salıncaklı, kaykaylı)
            BuildLibraryAndPark(civicGroup.transform, new Vector3(-207.0f, 0f, -90.0f), new Vector2(30.0f, 62.0f));

            // ORTA SÜTUN (X = -169m):
            // [HASTANE] (Devlet Hastanesi - Yola bakan, acil servis kanopili, helipadli ve yola bağlı)
            BuildHospital(civicGroup.transform, new Vector3(-169.0f, 0f, -32.0f), new Vector2(30.0f, 38.0f));
            // [İTFAİYE] (Yola bakan, çift garaj kapılı, hortum kuleli, yola direkt bağlı)
            BuildFireStation(civicGroup.transform, new Vector3(-169.0f, 0f, -69.0f), new Vector2(30.0f, 20.0f));
            // [POLİS KARAKOLU] (Yan sokağa bakan, yola bağlı, beyaz direkli kırmızı bayraklı)
            BuildPoliceStation(civicGroup.transform, new Vector3(-169.0f, 0f, -89.0f), new Vector2(30.0f, 20.0f));
            // [BENZİN İSTASYONU] (En aşağıdaki yola bağlı, 2 pompalı, ışıklı kanopili, mini marketli)
            BuildGasStation(civicGroup.transform, new Vector3(-169.0f, 0f, -110.0f), new Vector2(30.0f, 22.0f));

            // SAĞ SÜTUN (X = -131m - Nehir Kordonu):
            // [ŞEHİR BANKASI & ATM MERKEZİ] (Yola bakan, sütunlu, ATM'li, gece aydınlatmalı, yola bağlı)
            BuildCityBank(civicGroup.transform, new Vector3(-131.0f, 0f, -32.0f), new Vector2(30.0f, 38.0f));
            // [EĞLENCE MERKEZİ] (Yola bakan, neon ışıklı, oyun/arcade merkezli, yola bağlı)
            BuildEntertainmentCenter(civicGroup.transform, new Vector3(-131.0f, 0f, -71.0f), new Vector2(30.0f, 24.0f));
            // [ŞEHİR STADYUMU] (Çitlerle çevrili, ortasahalı, tam çizgili, kaliteli kaleli ve tribünlü)
            BuildFootballStadium(civicGroup.transform, new Vector3(-131.0f, 0f, -102.0f), new Vector2(30.0f, 38.0f));
        }

        #region Civic 1: Atatürk Primary School

        private static void BuildPrimarySchool(Transform parent, Vector3 centerPos, Vector2 parcelSize)
        {
            GameObject schoolObj = new GameObject("Primary_School_Complex");
            schoolObj.transform.SetParent(parent, false);
            schoolObj.transform.position = centerPos;

            Material schoolWallMat = GetMaterial("SchoolWallMat", new Color(0.92f, 0.82f, 0.65f), 0.1f, 0.3f);
            Material roofMat = GetMaterial("SchoolRoofMat", new Color(0.75f, 0.22f, 0.18f), 0.2f, 0.4f);
            Material whiteTrimMat = GetMaterial("SchoolWhiteTrimMat", new Color(0.96f, 0.96f, 0.98f), 0.1f, 0.4f);
            Material darkFrameMat = GetMaterial("SchoolDarkFrameMat", new Color(0.18f, 0.20f, 0.22f), 0.5f, 0.6f);
            Material glassMat = GetMaterial("SchoolGlassMat", new Color(0.20f, 0.65f, 0.85f, 0.90f), 0.8f, 0.95f);
            Material doorWoodMat = GetMaterial("SchoolDoorWoodMat", new Color(0.52f, 0.30f, 0.16f), 0.1f, 0.4f);
            Material flagRedMat = GetMaterial("FlagRedMat", new Color(0.88f, 0.10f, 0.12f), 0.0f, 0.3f);
            Material courtyardMat = GetMaterial("SchoolCourtyardMat", new Color(0.78f, 0.76f, 0.72f), 0.1f, 0.3f);

            float bW = 22.0f;
            float bD = 12.0f;
            float bH = 8.0f;
            float floorH = 3.2f;

            // Ana Okul Binası (Arka bahçe kısmına hizalandı: local Z = -5.5m)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "School_Main_Building";
            building.transform.SetParent(schoolObj.transform, false);
            building.transform.localPosition = new Vector3(0f, bH / 2f, -5.5f);
            building.transform.localScale = new Vector3(bW, bH, bD);
            building.GetComponent<Renderer>().sharedMaterial = schoolWallMat;

            // 1.1 Temel ve Kat Silmesi
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "School_Plinth";
            plinth.transform.SetParent(building.transform, false);
            plinth.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            plinth.transform.localScale = new Vector3(1.02f, 0.10f, 1.02f);
            plinth.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(plinth.GetComponent<Collider>());

            GameObject floorSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorSlab.name = "School_Floor_Slab";
            floorSlab.transform.SetParent(building.transform, false);
            floorSlab.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            floorSlab.transform.localScale = new Vector3(1.02f, 0.05f, 1.02f);
            floorSlab.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(floorSlab.GetComponent<Collider>());

            // 1.2 YOLA BAKAN (+Z KUZEY) ANITSAL GİRİŞ REVAKI & MERDİVENLER
            float frontZ = (bD / 2f);
            GameObject portico = new GameObject("Entrance_Portico");
            portico.transform.SetParent(building.transform, false);
            portico.transform.localPosition = new Vector3(0f, -0.50f, 0.50f);

            // 4 Klasik Beyaz Sütun
            for (int i = 0; i < 4; i++)
            {
                float px = -0.18f + (i * 0.12f);
                GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                column.name = $"Portico_Column_{i + 1}";
                column.transform.SetParent(portico.transform, false);
                column.transform.localPosition = new Vector3(px, 0.25f, 0.14f);
                column.transform.localScale = new Vector3(0.025f, 0.25f, 0.025f);
                column.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
                Object.Destroy(column.GetComponent<Collider>());
            }

            // Revak Çatısı / Alınlık
            GameObject porticoRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            porticoRoof.name = "Portico_Roof";
            porticoRoof.transform.SetParent(portico.transform, false);
            porticoRoof.transform.localPosition = new Vector3(0f, 0.52f, 0.08f);
            porticoRoof.transform.localScale = new Vector3(0.42f, 0.06f, 0.18f);
            porticoRoof.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.Destroy(porticoRoof.GetComponent<Collider>());

            // Geniş Mermer Giriş Merdivenleri (4 Basamak)
            for (int s = 0; s < 4; s++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Entrance_Step_{s + 1}";
                step.transform.SetParent(portico.transform, false);
                step.transform.localPosition = new Vector3(0f, (s * 0.025f) + 0.012f, 0.18f - (s * 0.04f));
                step.transform.localScale = new Vector3(0.38f + (s * 0.03f), 0.025f, 0.08f);
                step.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
                Object.Destroy(step.GetComponent<Collider>());
            }

            // 1.3 ÇİFT KANATLI AHŞAP VE CAMLI GİRİŞ KAPISI
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "School_Door_Frame";
            doorFrame.transform.SetParent(building.transform, false);
            doorFrame.transform.localPosition = new Vector3(0f, -0.32f, 0.51f);
            doorFrame.transform.localScale = new Vector3(0.16f, 0.35f, 0.03f);
            doorFrame.GetComponent<Renderer>().sharedMaterial = darkFrameMat;
            Object.Destroy(doorFrame.GetComponent<Collider>());

            GameObject doorL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorL.name = "Door_Left";
            doorL.transform.SetParent(doorFrame.transform, false);
            doorL.transform.localPosition = new Vector3(-0.24f, 0f, 0.10f);
            doorL.transform.localScale = new Vector3(0.45f, 0.90f, 0.40f);
            doorL.GetComponent<Renderer>().sharedMaterial = doorWoodMat;
            Object.Destroy(doorL.GetComponent<Collider>());

            GameObject doorR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorR.name = "Door_Right";
            doorR.transform.SetParent(doorFrame.transform, false);
            doorR.transform.localPosition = new Vector3(0.24f, 0f, 0.10f);
            doorR.transform.localScale = new Vector3(0.45f, 0.90f, 0.40f);
            doorR.GetComponent<Renderer>().sharedMaterial = doorWoodMat;
            Object.Destroy(doorR.GetComponent<Collider>());

            // 1.4 TÜM CEPHEDE ÇERÇEVELİ SINIF PENCERELERİ (Ön, Arka, Sol, Sağ)
            // Ön Cephe Pencereleri (+Z / Yola Bakan Cephe)
            float[] frontWinXs = new float[] { -0.36f, -0.22f, 0.22f, 0.36f };
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.25f + (f * 0.45f);
                foreach (float wx in frontWinXs)
                {
                    CreateSchoolWindow(building.transform, new Vector3(wx, wy, 0.51f), new Vector3(0.10f, 0.24f, 0.02f), glassMat, whiteTrimMat);
                }
            }

            // Arka Cephe Pencereleri (-Z)
            float[] rearWinXs = new float[] { -0.36f, -0.20f, 0f, 0.20f, 0.36f };
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.25f + (f * 0.45f);
                foreach (float wx in rearWinXs)
                {
                    CreateSchoolWindow(building.transform, new Vector3(wx, wy, -0.51f), new Vector3(0.10f, 0.24f, 0.02f), glassMat, whiteTrimMat);
                }
            }

            // Yan Cephe Pencereleri (-X ve +X)
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.25f + (f * 0.45f);
                for (int s = -1; s <= 1; s += 2)
                {
                    CreateSchoolWindow(building.transform, new Vector3(s * 0.51f, wy, -0.20f), new Vector3(0.02f, 0.24f, 0.16f), glassMat, whiteTrimMat);
                    CreateSchoolWindow(building.transform, new Vector3(s * 0.51f, wy, 0.20f), new Vector3(0.02f, 0.24f, 0.16f), glassMat, whiteTrimMat);
                }
            }

            // 1.5 KİREMİT KIRMA ÇATI & SAAT KULESİ
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "School_Roof";
            roof.transform.SetParent(building.transform, false);
            roof.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            roof.transform.localScale = new Vector3(1.06f, 0.22f, 1.06f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.Destroy(roof.GetComponent<Collider>());

            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "School_Clock_Tower";
            tower.transform.SetParent(building.transform, false);
            tower.transform.localPosition = new Vector3(0f, 0.85f, 0.05f);
            tower.transform.localScale = new Vector3(0.18f, 0.40f, 0.18f);
            tower.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(tower.GetComponent<Collider>());

            // Saat Kadranı
            GameObject clockDial = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            clockDial.name = "Clock_Dial";
            clockDial.transform.SetParent(tower.transform, false);
            clockDial.transform.localPosition = new Vector3(0f, 0.10f, 0.52f);
            clockDial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            clockDial.transform.localScale = new Vector3(0.55f, 0.05f, 0.55f);
            clockDial.GetComponent<Renderer>().sharedMaterial = GetMaterial("ClockDialMat", new Color(0.98f, 0.98f, 0.98f), 0.1f, 0.8f);
            Object.Destroy(clockDial.GetComponent<Collider>());

            // 1.6 YOLA KADAR UZANAN TAŞ YÜRÜYÜŞ YOLU (Girişten Kuzey Kaldırımına Kesintisiz Bağlantı)
            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = "School_Entrance_Walkway";
            path.transform.SetParent(schoolObj.transform, false);
            path.transform.localPosition = new Vector3(0f, 0.01f, 10.0f);
            path.transform.localScale = new Vector3(6.5f, 0.02f, 19.0f);
            path.GetComponent<Renderer>().sharedMaterial = GetMaterial("SchoolWalkwayMat", new Color(0.85f, 0.84f, 0.80f), 0.1f, 0.3f);
            Object.Destroy(path.GetComponent<Collider>());

            // 1.7 BEYAZ BAYRAK DİREĞİ VE KIRMIZI TÜRK BAYRAĞI (Girişin Önünde, Bahçede)
            GameObject flagGroup = new GameObject("Turkish_Flag_Monument");
            flagGroup.transform.SetParent(schoolObj.transform, false);
            flagGroup.transform.localPosition = new Vector3(-5.5f, 0f, 7.5f);

            // Kaide
            GameObject flagBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagBase.name = "Flag_Base";
            flagBase.transform.SetParent(flagGroup.transform, false);
            flagBase.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            flagBase.transform.localScale = new Vector3(1.2f, 0.20f, 1.2f);
            flagBase.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(flagBase.GetComponent<Collider>());

            // Beyaz Direk
            GameObject flagPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagPole.name = "White_Flag_Pole";
            flagPole.transform.SetParent(flagGroup.transform, false);
            flagPole.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            flagPole.transform.localScale = new Vector3(0.12f, 3.6f, 0.12f);
            flagPole.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(flagPole.GetComponent<Collider>());

            // Tepe Altın Küresi
            GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            finial.name = "Pole_Gold_Finial";
            finial.transform.SetParent(flagGroup.transform, false);
            finial.transform.localPosition = new Vector3(0f, 7.45f, 0f);
            finial.transform.localScale = new Vector3(0.30f, 0.30f, 0.30f);
            finial.GetComponent<Renderer>().sharedMaterial = GetMaterial("GoldFinialMat", new Color(0.95f, 0.80f, 0.20f), 0.8f, 0.8f);
            Object.Destroy(finial.GetComponent<Collider>());

            // Kırmızı Dalgalanan Türk Bayrağı
            GameObject flagCloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagCloth.name = "Turkish_Flag_Cloth";
            flagCloth.transform.SetParent(flagGroup.transform, false);
            flagCloth.transform.localPosition = new Vector3(1.25f, 6.4f, 0f);
            flagCloth.transform.localScale = new Vector3(2.4f, 1.6f, 0.04f);
            flagCloth.GetComponent<Renderer>().sharedMaterial = flagRedMat;
            Object.Destroy(flagCloth.GetComponent<Collider>());

            // Bayrak Üzerinde Beyaz Hilal ve Yıldız Sembolü
            GameObject crescent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crescent.name = "Flag_Crescent_Moon";
            crescent.transform.SetParent(flagCloth.transform, false);
            crescent.transform.localPosition = new Vector3(-0.15f, 0f, 0.55f);
            crescent.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            crescent.transform.localScale = new Vector3(0.45f, 0.10f, 0.45f);
            crescent.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(crescent.GetComponent<Collider>());

            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Flag_Star";
            star.transform.SetParent(flagCloth.transform, false);
            star.transform.localPosition = new Vector3(0.20f, 0.05f, 0.55f);
            star.transform.localScale = new Vector3(0.20f, 0.20f, 0.10f);
            star.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(star.GetComponent<Collider>());

            // Okul Bahçesi Bankları
            for (int b = -1; b <= 1; b += 2)
            {
                GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bench.name = "School_Yard_Bench";
                bench.transform.SetParent(schoolObj.transform, false);
                bench.transform.localPosition = new Vector3(b * 7.5f, 0.25f, 5.0f);
                bench.transform.localScale = new Vector3(0.6f, 0.40f, 1.8f);
                bench.GetComponent<Renderer>().sharedMaterial = GetMaterial("BenchWoodMat", new Color(0.48f, 0.28f, 0.16f), 0.1f, 0.3f);
                Object.Destroy(bench.GetComponent<Collider>());
            }

            NavMeshObstacle obs = building.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.carving = true;
        }

        private static void CreateSchoolWindow(Transform parent, Vector3 localPos, Vector3 scale, Material glassMat, Material frameMat)
        {
            GameObject winObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            winObj.name = "School_Window";
            winObj.transform.SetParent(parent, false);
            winObj.transform.localPosition = localPos;
            winObj.transform.localScale = scale;
            winObj.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(winObj.GetComponent<Collider>());
        }

        #endregion

        #region Civic 2: City Library & Reading Park

        private static void BuildLibraryAndPark(Transform parent, Vector3 centerPos, Vector2 parcelSize)
        {
            GameObject libObj = new GameObject("City_Library_And_Park_Complex");
            libObj.transform.SetParent(parent, false);
            libObj.transform.position = centerPos;

            Material marbleMat = GetMaterial("LibMarbleMat", new Color(0.93f, 0.92f, 0.88f), 0.1f, 0.5f);
            Material darkStoneMat = GetMaterial("LibDarkStoneMat", new Color(0.24f, 0.26f, 0.28f), 0.4f, 0.6f);
            Material roofMat = GetMaterial("LibRoofMat", new Color(0.25f, 0.42f, 0.38f), 0.2f, 0.4f);
            Material doorWoodMat = GetMaterial("LibDoorWoodMat", new Color(0.46f, 0.26f, 0.14f), 0.1f, 0.4f);
            Material glassMat = GetMaterial("LibGlassMat", new Color(0.20f, 0.65f, 0.85f, 0.90f), 0.8f, 0.95f);
            Material whiteTrimMat = GetMaterial("LibWhiteTrimMat", new Color(0.96f, 0.96f, 0.98f), 0.1f, 0.4f);
            Material walkwayMat = GetMaterial("LibWalkwayMat", new Color(0.82f, 0.80f, 0.76f), 0.1f, 0.3f);
            Material lawnMat = GetMaterial("LibParkLawnMat", new Color(0.24f, 0.58f, 0.22f), 0.0f, 0.1f);
            Material barkMat = GetMaterial("ParkTreeBarkMat", new Color(0.42f, 0.28f, 0.16f), 0.1f, 0.2f);
            Material leafMat = GetMaterial("ParkTreeLeafMat", new Color(0.18f, 0.52f, 0.18f), 0.0f, 0.2f);
            Material benchWoodMat = GetMaterial("ParkBenchWoodMat", new Color(0.55f, 0.32f, 0.18f), 0.1f, 0.3f);
            Material swingRedMat = GetMaterial("ParkSwingRedMat", new Color(0.85f, 0.25f, 0.20f), 0.2f, 0.5f);
            Material slideYellowMat = GetMaterial("ParkSlideYellowMat", new Color(0.95f, 0.78f, 0.15f), 0.3f, 0.6f);
            Material skatePlywoodMat = GetMaterial("ParkSkateRampMat", new Color(0.72f, 0.60f, 0.45f), 0.1f, 0.4f);

            float bW = 22.0f;
            float bD = 13.0f;
            float bH = 8.5f;

            // 2. KÜTÜPHANE BİNASI (Kuzey tarafında: local Z = 18.0m)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Library_Main_Building";
            building.transform.SetParent(libObj.transform, false);
            building.transform.localPosition = new Vector3(0f, bH / 2f, 18.0f);
            building.transform.localScale = new Vector3(bW, bH, bD);
            building.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 2.1 Kaide ve Kat Silmeleri
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Library_Plinth";
            plinth.transform.SetParent(building.transform, false);
            plinth.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            plinth.transform.localScale = new Vector3(1.02f, 0.10f, 1.02f);
            plinth.GetComponent<Renderer>().sharedMaterial = darkStoneMat;
            Object.Destroy(plinth.GetComponent<Collider>());

            // 2.2 YOLA BAKAN (+Z KUZEY) 6 SÜTUNLU ANITSAL GİRİŞ REVAKI & MERDİVENLER
            GameObject portico = new GameObject("Library_Front_Portico");
            portico.transform.SetParent(building.transform, false);
            portico.transform.localPosition = new Vector3(0f, -0.50f, 0.50f);

            for (int i = 0; i < 6; i++)
            {
                float px = -0.25f + (i * 0.10f);
                GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                column.name = $"Portico_Column_{i + 1}";
                column.transform.SetParent(portico.transform, false);
                column.transform.localPosition = new Vector3(px, 0.25f, 0.12f);
                column.transform.localScale = new Vector3(0.025f, 0.25f, 0.025f);
                column.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
                Object.Destroy(column.GetComponent<Collider>());
            }

            // Klasik Alınlık Çatı
            GameObject pediment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pediment.name = "Portico_Pediment";
            pediment.transform.SetParent(portico.transform, false);
            pediment.transform.localPosition = new Vector3(0f, 0.54f, 0.06f);
            pediment.transform.localScale = new Vector3(0.55f, 0.08f, 0.16f);
            pediment.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.Destroy(pediment.GetComponent<Collider>());

            // Giriş Merdivenleri (4 Mermer Basamak)
            for (int s = 0; s < 4; s++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Entrance_Step_{s + 1}";
                step.transform.SetParent(portico.transform, false);
                step.transform.localPosition = new Vector3(0f, (s * 0.025f) + 0.012f, 0.16f - (s * 0.035f));
                step.transform.localScale = new Vector3(0.48f + (s * 0.02f), 0.025f, 0.07f);
                step.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
                Object.Destroy(step.GetComponent<Collider>());
            }

            // 2.3 ÖN GİRİŞ KAPISI (Yola Bakan Masif Ahşap ve Pirinç Kol)
            GameObject frontDoorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontDoorFrame.name = "Front_Door_Frame";
            frontDoorFrame.transform.SetParent(building.transform, false);
            frontDoorFrame.transform.localPosition = new Vector3(0f, -0.32f, 0.51f);
            frontDoorFrame.transform.localScale = new Vector3(0.18f, 0.35f, 0.03f);
            frontDoorFrame.GetComponent<Renderer>().sharedMaterial = darkStoneMat;
            Object.Destroy(frontDoorFrame.GetComponent<Collider>());

            GameObject frontDoorL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontDoorL.name = "Front_Door_Left";
            frontDoorL.transform.SetParent(frontDoorFrame.transform, false);
            frontDoorL.transform.localPosition = new Vector3(-0.24f, 0f, 0.10f);
            frontDoorL.transform.localScale = new Vector3(0.45f, 0.90f, 0.40f);
            frontDoorL.GetComponent<Renderer>().sharedMaterial = doorWoodMat;
            Object.Destroy(frontDoorL.GetComponent<Collider>());

            GameObject frontDoorR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontDoorR.name = "Front_Door_Right";
            frontDoorR.transform.SetParent(frontDoorFrame.transform, false);
            frontDoorR.transform.localPosition = new Vector3(0.24f, 0f, 0.10f);
            frontDoorR.transform.localScale = new Vector3(0.45f, 0.90f, 0.40f);
            frontDoorR.GetComponent<Renderer>().sharedMaterial = doorWoodMat;
            Object.Destroy(frontDoorR.GetComponent<Collider>());

            // 2.4 ARKA BAHÇE / PARKA AÇILAN ÇİFT CAMLI BAHÇE KAPISI (-Z Parka Bakan)
            GameObject backDoorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backDoorFrame.name = "Garden_Back_Door_Frame";
            backDoorFrame.transform.SetParent(building.transform, false);
            backDoorFrame.transform.localPosition = new Vector3(0f, -0.32f, -0.51f);
            backDoorFrame.transform.localScale = new Vector3(0.18f, 0.35f, 0.03f);
            backDoorFrame.GetComponent<Renderer>().sharedMaterial = darkStoneMat;
            Object.Destroy(backDoorFrame.GetComponent<Collider>());

            GameObject backDoorL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backDoorL.name = "Garden_Door_Left";
            backDoorL.transform.SetParent(backDoorFrame.transform, false);
            backDoorL.transform.localPosition = new Vector3(-0.24f, 0f, -0.10f);
            backDoorL.transform.localScale = new Vector3(0.45f, 0.90f, 0.40f);
            backDoorL.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(backDoorL.GetComponent<Collider>());

            GameObject backDoorR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backDoorR.name = "Garden_Door_Right";
            backDoorR.transform.SetParent(backDoorFrame.transform, false);
            backDoorR.transform.localPosition = new Vector3(0.24f, 0f, -0.10f);
            backDoorR.transform.localScale = new Vector3(0.45f, 0.90f, 0.40f);
            backDoorR.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(backDoorR.GetComponent<Collider>());

            // Arka Bahçe Merdiveni (3 Basamak)
            for (int s = 0; s < 3; s++)
            {
                GameObject bStep = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bStep.name = $"Garden_Step_{s + 1}";
                bStep.transform.SetParent(building.transform, false);
                bStep.transform.localPosition = new Vector3(0f, -0.48f + (s * 0.02f), -0.53f - (s * 0.025f));
                bStep.transform.localScale = new Vector3(0.22f, 0.02f, 0.06f);
                bStep.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
                Object.Destroy(bStep.GetComponent<Collider>());
            }

            // 2.5 TÜM KATLARDA VE CEPHELERDE KALİTELİ OKUMA SALONU PENCERELERİ
            // Ön Cephe Pencereleri (+Z)
            float[] frontWinXs = new float[] { -0.38f, -0.22f, 0.22f, 0.38f };
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.24f + (f * 0.45f);
                foreach (float wx in frontWinXs)
                {
                    CreateSchoolWindow(building.transform, new Vector3(wx, wy, 0.51f), new Vector3(0.10f, 0.25f, 0.02f), glassMat, darkStoneMat);
                }
            }

            // Arka Cephe Pencereleri (-Z Parka Bakan)
            float[] rearWinXs = new float[] { -0.38f, -0.22f, 0.22f, 0.38f };
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.24f + (f * 0.45f);
                foreach (float wx in rearWinXs)
                {
                    CreateSchoolWindow(building.transform, new Vector3(wx, wy, -0.51f), new Vector3(0.10f, 0.25f, 0.02f), glassMat, darkStoneMat);
                }
            }

            // Yan Cephe Pencereleri (-X ve +X)
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.24f + (f * 0.45f);
                for (int s = -1; s <= 1; s += 2)
                {
                    CreateSchoolWindow(building.transform, new Vector3(s * 0.51f, wy, -0.20f), new Vector3(0.02f, 0.25f, 0.16f), glassMat, darkStoneMat);
                    CreateSchoolWindow(building.transform, new Vector3(s * 0.51f, wy, 0.20f), new Vector3(0.02f, 0.25f, 0.16f), glassMat, darkStoneMat);
                }
            }

            // 2.6 KÜTÜPHANE ÇATISI & KUBBELİ IŞIKLIK
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Library_Roof";
            roof.transform.SetParent(building.transform, false);
            roof.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            roof.transform.localScale = new Vector3(1.06f, 0.22f, 1.06f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.Destroy(roof.GetComponent<Collider>());

            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "Library_Skylight_Dome";
            dome.transform.SetParent(building.transform, false);
            dome.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            dome.transform.localScale = new Vector3(0.28f, 0.25f, 0.28f);
            dome.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(dome.GetComponent<Collider>());

            // 2.7 ÖN KAPI TAŞ YÜRÜYÜŞ YOLU (Girişten Kuzey Kaldırımına Bağlantı)
            GameObject frontPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontPath.name = "Library_Front_Walkway";
            frontPath.transform.SetParent(libObj.transform, false);
            frontPath.transform.localPosition = new Vector3(0f, 0.01f, 27.0f);
            frontPath.transform.localScale = new Vector3(5.5f, 0.02f, 7.5f);
            frontPath.GetComponent<Renderer>().sharedMaterial = walkwayMat;
            Object.Destroy(frontPath.GetComponent<Collider>());

            // ==========================================
            // 3. ARKA PARK & ÇOCUK OYUN/KAYKAY ALANI
            // ==========================================

            // 3.1 PARK İÇİ TAŞ YÜRÜME YOLLARI SİSTEMİ
            // A) Kütüphane Arka Kapısından Güneye Uzanan Ana Bulvar
            GameObject mainParkAvenue = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainParkAvenue.name = "Park_Main_Walkway";
            mainParkAvenue.transform.SetParent(libObj.transform, false);
            mainParkAvenue.transform.localPosition = new Vector3(0f, 0.01f, -9.0f);
            mainParkAvenue.transform.localScale = new Vector3(3.5f, 0.02f, 43.0f);
            mainParkAvenue.GetComponent<Renderer>().sharedMaterial = walkwayMat;
            Object.Destroy(mainParkAvenue.GetComponent<Collider>());

            // B) Soldaki Cadde Kaldırımına Bağlantı Yolu (West Connection: X = -14.5m)
            GameObject westPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westPath.name = "Park_West_Connection_Walkway";
            westPath.transform.SetParent(libObj.transform, false);
            westPath.transform.localPosition = new Vector3(-7.5f, 0.01f, -6.0f);
            westPath.transform.localScale = new Vector3(14.5f, 0.02f, 3.0f);
            westPath.GetComponent<Renderer>().sharedMaterial = walkwayMat;
            Object.Destroy(westPath.GetComponent<Collider>());

            // C) Sağdaki Cadde Kaldırımına Bağlantı Yolu (East Connection: X = +14.5m)
            GameObject eastPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastPath.name = "Park_East_Connection_Walkway";
            eastPath.transform.SetParent(libObj.transform, false);
            eastPath.transform.localPosition = new Vector3(7.5f, 0.01f, -6.0f);
            eastPath.transform.localScale = new Vector3(14.5f, 0.02f, 3.0f);
            eastPath.GetComponent<Renderer>().sharedMaterial = walkwayMat;
            Object.Destroy(eastPath.GetComponent<Collider>());

            // D) Merkezi Dairesel Okuma Meydanı
            GameObject plazaCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plazaCircle.name = "Park_Central_Plaza";
            plazaCircle.transform.SetParent(libObj.transform, false);
            plazaCircle.transform.localPosition = new Vector3(0f, 0.015f, -6.0f);
            plazaCircle.transform.localScale = new Vector3(7.5f, 0.02f, 7.5f);
            plazaCircle.GetComponent<Renderer>().sharedMaterial = walkwayMat;
            Object.Destroy(plazaCircle.GetComponent<Collider>());

            // Meydan Ortası Süs Çiçekliği
            GameObject planter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            planter.name = "Plaza_Planter";
            planter.transform.SetParent(plazaCircle.transform, false);
            planter.transform.localPosition = new Vector3(0f, 0.40f, 0f);
            planter.transform.localScale = new Vector3(0.35f, 0.60f, 0.35f);
            planter.GetComponent<Renderer>().sharedMaterial = darkStoneMat;
            Object.Destroy(planter.GetComponent<Collider>());

            // 3.2 PARK BANKLARI (Meydan ve Yollar Boyunca)
            Vector3[] benchPositions = new Vector3[]
            {
                new Vector3(-4.5f, 0.25f, -6.0f),
                new Vector3(4.5f, 0.25f, -6.0f),
                new Vector3(0f, 0.25f, -1.5f),
                new Vector3(0f, 0.25f, -10.5f),
                new Vector3(-2.8f, 0.25f, -22.0f),
                new Vector3(2.8f, 0.25f, -22.0f)
            };

            for (int b = 0; b < benchPositions.Length; b++)
            {
                GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bench.name = $"Park_Reading_Bench_{b + 1}";
                bench.transform.SetParent(libObj.transform, false);
                bench.transform.localPosition = benchPositions[b];
                bench.transform.localScale = new Vector3(1.6f, 0.40f, 0.6f);
                bench.GetComponent<Renderer>().sharedMaterial = benchWoodMat;
                Object.Destroy(bench.GetComponent<Collider>());
            }

            // 3.3 DOĞAL GÖLGELİK PARK AĞAÇLARI (6 Adet)
            Vector3[] treePositions = new Vector3[]
            {
                new Vector3(-10.0f, 0f, 3.0f),
                new Vector3(10.0f, 0f, 3.0f),
                new Vector3(-9.5f, 0f, -15.0f),
                new Vector3(9.5f, 0f, -15.0f),
                new Vector3(-9.5f, 0f, -27.0f),
                new Vector3(9.5f, 0f, -27.0f)
            };

            for (int t = 0; t < treePositions.Length; t++)
            {
                GameObject tree = new GameObject($"Park_Tree_{t + 1}");
                tree.transform.SetParent(libObj.transform, false);
                tree.transform.localPosition = treePositions[t];

                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(tree.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 1.8f, 0f);
                trunk.transform.localScale = new Vector3(0.40f, 1.8f, 0.40f);
                trunk.GetComponent<Renderer>().sharedMaterial = barkMat;
                Object.Destroy(trunk.GetComponent<Collider>());

                GameObject leaves1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaves1.name = "Foliage_Bottom";
                leaves1.transform.SetParent(tree.transform, false);
                leaves1.transform.localPosition = new Vector3(0f, 3.6f, 0f);
                leaves1.transform.localScale = new Vector3(3.2f, 1.6f, 3.2f);
                leaves1.GetComponent<Renderer>().sharedMaterial = leafMat;
                Object.Destroy(leaves1.GetComponent<Collider>());

                GameObject leaves2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaves2.name = "Foliage_Top";
                leaves2.transform.SetParent(tree.transform, false);
                leaves2.transform.localPosition = new Vector3(0f, 4.8f, 0f);
                leaves2.transform.localScale = new Vector3(2.4f, 1.4f, 2.4f);
                leaves2.GetComponent<Renderer>().sharedMaterial = leafMat;
                Object.Destroy(leaves2.GetComponent<Collider>());
            }

            // 3.4 ÇOCUK OYUN ALANI (SALINCAK & KAYDIRAK & KAYKAY RAMPASI)
            GameObject playArea = new GameObject("Children_Play_And_Skate_Area");
            playArea.transform.SetParent(libObj.transform, false);
            playArea.transform.localPosition = new Vector3(0f, 0f, -17.0f);

            // Güvenlikli Kauçuk Zemin
            GameObject playGroundMat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playGroundMat.name = "Playground_Rubber_Mat";
            playGroundMat.transform.SetParent(playArea.transform, false);
            playGroundMat.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            playGroundMat.transform.localScale = new Vector3(18.0f, 0.02f, 10.0f);
            playGroundMat.GetComponent<Renderer>().sharedMaterial = GetMaterial("RubberMat", new Color(0.20f, 0.50f, 0.65f), 0.1f, 0.3f);
            Object.Destroy(playGroundMat.GetComponent<Collider>());

            // A) SALINCAK GRUBU (Sol Tarafta: X = -5.0m)
            GameObject swingGroup = new GameObject("Playground_Swing_Set");
            swingGroup.transform.SetParent(playArea.transform, false);
            swingGroup.transform.localPosition = new Vector3(-5.0f, 0f, 0f);

            // Yan A-Direkleri
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject poleA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                poleA.name = "Swing_A_Pole";
                poleA.transform.SetParent(swingGroup.transform, false);
                poleA.transform.localPosition = new Vector3(k * 1.8f, 1.6f, 0f);
                poleA.transform.localScale = new Vector3(0.08f, 1.6f, 0.08f);
                poleA.GetComponent<Renderer>().sharedMaterial = swingRedMat;
                Object.Destroy(poleA.GetComponent<Collider>());
            }

            // Üst Kiriş
            GameObject topBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topBeam.name = "Swing_Top_Beam";
            topBeam.transform.SetParent(swingGroup.transform, false);
            topBeam.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            topBeam.transform.localScale = new Vector3(4.0f, 0.12f, 0.12f);
            topBeam.GetComponent<Renderer>().sharedMaterial = swingRedMat;
            Object.Destroy(topBeam.GetComponent<Collider>());

            // 2 Salıncak Oturağı ve İpleri
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seat.name = "Swing_Seat";
                seat.transform.SetParent(swingGroup.transform, false);
                seat.transform.localPosition = new Vector3(s * 0.9f, 0.60f, 0f);
                seat.transform.localScale = new Vector3(0.65f, 0.06f, 0.35f);
                seat.GetComponent<Renderer>().sharedMaterial = benchWoodMat;
                Object.Destroy(seat.GetComponent<Collider>());

                for (int c = -1; c <= 1; c += 2)
                {
                    GameObject chain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    chain.name = "Swing_Chain";
                    chain.transform.SetParent(swingGroup.transform, false);
                    chain.transform.localPosition = new Vector3((s * 0.9f) + (c * 0.25f), 1.9f, 0f);
                    chain.transform.localScale = new Vector3(0.02f, 1.3f, 0.02f);
                    chain.GetComponent<Renderer>().sharedMaterial = darkStoneMat;
                    Object.Destroy(chain.GetComponent<Collider>());
                }
            }

            // B) ÇOCUK KAYDIRAĞI (Orta Kısımda: X = 0.5f)
            GameObject slideGroup = new GameObject("Playground_Slide");
            slideGroup.transform.SetParent(playArea.transform, false);
            slideGroup.transform.localPosition = new Vector3(0.5f, 0f, 0f);

            // Çıkış Merdiveni ve Platformu
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Slide_Platform";
            platform.transform.SetParent(slideGroup.transform, false);
            platform.transform.localPosition = new Vector3(0f, 1.6f, -1.5f);
            platform.transform.localScale = new Vector3(1.2f, 0.12f, 1.2f);
            platform.GetComponent<Renderer>().sharedMaterial = swingRedMat;
            Object.Destroy(platform.GetComponent<Collider>());

            for (int p = 0; p < 4; p++)
            {
                float px = (p % 2 == 0) ? -0.5f : 0.5f;
                float pz = (p < 2) ? -2.0f : -1.0f;
                GameObject pCol = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pCol.name = "Platform_Leg";
                pCol.transform.SetParent(slideGroup.transform, false);
                pCol.transform.localPosition = new Vector3(px, 0.8f, pz);
                pCol.transform.localScale = new Vector3(0.06f, 0.8f, 0.06f);
                pCol.GetComponent<Renderer>().sharedMaterial = darkStoneMat;
                Object.Destroy(pCol.GetComponent<Collider>());
            }

            // Sarı Eğimli Kaydırak Oluğu
            GameObject slideChute = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slideChute.name = "Slide_Chute";
            slideChute.transform.SetParent(slideGroup.transform, false);
            slideChute.transform.localPosition = new Vector3(0f, 0.85f, 0.5f);
            slideChute.transform.localRotation = Quaternion.Euler(32f, 0f, 0f);
            slideChute.transform.localScale = new Vector3(0.85f, 0.10f, 2.8f);
            slideChute.GetComponent<Renderer>().sharedMaterial = slideYellowMat;
            Object.Destroy(slideChute.GetComponent<Collider>());

            // C) KAYKAY RAMPASI & KAYKAY DEMİRİ (Sağ Tarafta: X = 5.5f)
            GameObject skateGroup = new GameObject("Skate_Mini_Ramp");
            skateGroup.transform.SetParent(playArea.transform, false);
            skateGroup.transform.localPosition = new Vector3(5.5f, 0f, 0f);

            // Ahşap Kaykay Rampası
            GameObject rampL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampL.name = "Skate_Ramp_Left";
            rampL.transform.SetParent(skateGroup.transform, false);
            rampL.transform.localPosition = new Vector3(0f, 0.40f, -1.4f);
            rampL.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            rampL.transform.localScale = new Vector3(2.4f, 0.15f, 1.8f);
            rampL.GetComponent<Renderer>().sharedMaterial = skatePlywoodMat;
            Object.Destroy(rampL.GetComponent<Collider>());

            GameObject rampR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampR.name = "Skate_Ramp_Right";
            rampR.transform.SetParent(skateGroup.transform, false);
            rampR.transform.localPosition = new Vector3(0f, 0.40f, 1.4f);
            rampR.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            rampR.transform.localScale = new Vector3(2.4f, 0.15f, 1.8f);
            rampR.GetComponent<Renderer>().sharedMaterial = skatePlywoodMat;
            Object.Destroy(rampR.GetComponent<Collider>());

            // Kaykay Grind Rayı (Demir Ray)
            GameObject grindRail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grindRail.name = "Skate_Grind_Rail";
            grindRail.transform.SetParent(skateGroup.transform, false);
            grindRail.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            grindRail.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            grindRail.transform.localScale = new Vector3(0.06f, 1.2f, 0.06f);
            grindRail.GetComponent<Renderer>().sharedMaterial = slideYellowMat;
            Object.Destroy(grindRail.GetComponent<Collider>());

            // 3.5 PARK AYDINLATMA LAMBALARI (Gece Otomatik Yanan 4 Fener)
            Vector3[] lampPositions = new Vector3[]
            {
                new Vector3(-4.0f, 0f, -4.0f),
                new Vector3(4.0f, 0f, -4.0f),
                new Vector3(-4.0f, 0f, -24.0f),
                new Vector3(4.0f, 0f, -24.0f)
            };

            for (int l = 0; l < lampPositions.Length; l++)
            {
                BuildParkStreetLamp(libObj.transform, lampPositions[l]);
            }

            NavMeshObstacle obs = building.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.carving = true;
        }

        private static void BuildParkStreetLamp(Transform parent, Vector3 localPos)
        {
            GameObject lampObj = new GameObject("Park_Lantern");
            lampObj.transform.SetParent(parent, false);
            lampObj.transform.localPosition = localPos;

            Material darkMat = GetMaterial("ParkLampDarkMat", new Color(0.18f, 0.20f, 0.22f), 0.5f, 0.6f);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Lamp_Pole";
            pole.transform.SetParent(lampObj.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            pole.transform.localScale = new Vector3(0.10f, 1.8f, 0.10f);
            pole.GetComponent<Renderer>().sharedMaterial = darkMat;
            Object.Destroy(pole.GetComponent<Collider>());

            GameObject globe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            globe.name = "Lamp_Globe";
            globe.transform.SetParent(lampObj.transform, false);
            globe.transform.localPosition = new Vector3(0f, 3.65f, 0f);
            globe.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            globe.GetComponent<Renderer>().sharedMaterial = GetMaterial("ParkGlobeLitMat", new Color(1.0f, 0.96f, 0.75f), 0.1f, 0.9f);
            Object.Destroy(globe.GetComponent<Collider>());

            GameObject lightObj = new GameObject("Park_Light_Source");
            lightObj.transform.SetParent(globe.transform, false);
            lightObj.transform.localPosition = Vector3.zero;

            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.88f, 0.55f);
            pLight.intensity = 2.2f;
            pLight.range = 9.0f;
            pLight.shadows = LightShadows.None;
            pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStreetLamp(globe, pLight);
            }
        }

        #endregion

        #region Civic 3: Hospital

        private static void BuildHospital(Transform parent, Vector3 centerPos, Vector2 parcelSize)
        {
            GameObject hospObj = new GameObject("Hospital_Complex");
            hospObj.transform.SetParent(parent, false);
            hospObj.transform.position = centerPos;

            Material hospWallMat = GetMaterial("HospWallMat", new Color(0.95f, 0.96f, 0.98f), 0.1f, 0.4f);
            Material redCrossMat = GetMaterial("HospRedCrossMat", new Color(0.90f, 0.12f, 0.12f), 0.0f, 0.5f);
            Material darkTrimMat = GetMaterial("HospDarkTrimMat", new Color(0.20f, 0.22f, 0.25f), 0.4f, 0.6f);
            Material glassMat = GetMaterial("HospGlassMat", new Color(0.20f, 0.65f, 0.85f, 0.90f), 0.8f, 0.95f);
            Material whiteTrimMat = GetMaterial("HospWhiteTrimMat", new Color(0.96f, 0.96f, 0.98f), 0.1f, 0.4f);
            Material drivewayMat = GetMaterial("HospDrivewayMat", new Color(0.35f, 0.38f, 0.42f), 0.1f, 0.3f);
            Material yellowHazardMat = GetMaterial("HospHazardYellowMat", new Color(0.96f, 0.82f, 0.15f), 0.0f, 0.3f);

            float bW = 22.0f;
            float bD = 13.0f;
            float bH = 9.8f;
            float floorH = 3.2f;

            // 2. Ana Hastane Binası (Arka bahçe kısmına hizalandı: local Z = -5.0m)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Hospital_Main_Building";
            building.transform.SetParent(hospObj.transform, false);
            building.transform.localPosition = new Vector3(0f, bH / 2f, -5.0f);
            building.transform.localScale = new Vector3(bW, bH, bD);
            building.GetComponent<Renderer>().sharedMaterial = hospWallMat;

            // 2.1 Temel Su Basmanı ve Kat Silmeleri
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Hospital_Plinth";
            plinth.transform.SetParent(building.transform, false);
            plinth.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            plinth.transform.localScale = new Vector3(1.02f, 0.10f, 1.02f);
            plinth.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(plinth.GetComponent<Collider>());

            for (int f = 1; f <= 2; f++)
            {
                float slabY = -0.50f + (f * 0.333f);
                GameObject floorSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorSlab.name = $"Hospital_Floor_Slab_{f}";
                floorSlab.transform.SetParent(building.transform, false);
                floorSlab.transform.localPosition = new Vector3(0f, slabY, 0f);
                floorSlab.transform.localScale = new Vector3(1.02f, 0.03f, 1.02f);
                floorSlab.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
                Object.Destroy(floorSlab.GetComponent<Collider>());
            }

            // 2.2 YOLA BAKAN (+Z KUZEY) ACİL SERVİS KANOPİSİ & AMBULANS PERONU
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Emergency_Canopy";
            canopy.transform.SetParent(building.transform, false);
            canopy.transform.localPosition = new Vector3(-0.25f, -0.16f, 0.72f);
            canopy.transform.localScale = new Vector3(0.45f, 0.05f, 0.40f);
            canopy.GetComponent<Renderer>().sharedMaterial = redCrossMat;
            Object.Destroy(canopy.GetComponent<Collider>());

            // Kanopi Destek Kolonları
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                col.name = "Canopy_Column";
                col.transform.SetParent(canopy.transform, false);
                col.transform.localPosition = new Vector3(k * 0.42f, -3.5f, 0.42f);
                col.transform.localScale = new Vector3(0.08f, 3.5f, 0.08f);
                col.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
                Object.Destroy(col.GetComponent<Collider>());
            }

            // 2.3 OTOMATİK FOTOSELLİ ÇİFT CAM GİRİŞ KAPISI
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Automatic_Door_Frame";
            doorFrame.transform.SetParent(building.transform, false);
            doorFrame.transform.localPosition = new Vector3(-0.25f, -0.34f, 0.51f);
            doorFrame.transform.localScale = new Vector3(0.20f, 0.30f, 0.03f);
            doorFrame.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(doorFrame.GetComponent<Collider>());

            GameObject glassDoorL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassDoorL.name = "Sliding_Door_Left";
            glassDoorL.transform.SetParent(doorFrame.transform, false);
            glassDoorL.transform.localPosition = new Vector3(-0.24f, 0f, 0.10f);
            glassDoorL.transform.localScale = new Vector3(0.44f, 0.88f, 0.40f);
            glassDoorL.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(glassDoorL.GetComponent<Collider>());

            GameObject glassDoorR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassDoorR.name = "Sliding_Door_Right";
            glassDoorR.transform.SetParent(doorFrame.transform, false);
            glassDoorR.transform.localPosition = new Vector3(0.24f, 0f, 0.10f);
            glassDoorR.transform.localScale = new Vector3(0.44f, 0.88f, 0.40f);
            glassDoorR.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(glassDoorR.GetComponent<Collider>());

            // 2.4 TÜM KATLARDA VE CEPHELERDE KALİTELİ PENCERELER (Ön, Arka, Sol, Sağ)
            // Ön Cephe Pencereleri (+Z / Yola Bakan)
            float[] frontWinXs = new float[] { -0.38f, 0.08f, 0.24f, 0.38f };
            for (int f = 0; f < 3; f++)
            {
                float wy = -0.32f + (f * 0.333f);
                foreach (float wx in frontWinXs)
                {
                    // Giriş kapısı üzerini açık bırak
                    if (f == 0 && wx < 0f) continue;
                    CreateHospitalWindow(building.transform, new Vector3(wx, wy, 0.51f), new Vector3(0.11f, 0.18f, 0.02f), glassMat, darkTrimMat);
                }
            }

            // Arka Cephe Pencereleri (-Z)
            float[] rearWinXs = new float[] { -0.38f, -0.22f, -0.06f, 0.10f, 0.26f, 0.38f };
            for (int f = 0; f < 3; f++)
            {
                float wy = -0.32f + (f * 0.333f);
                foreach (float wx in rearWinXs)
                {
                    CreateHospitalWindow(building.transform, new Vector3(wx, wy, -0.51f), new Vector3(0.10f, 0.18f, 0.02f), glassMat, darkTrimMat);
                }
            }

            // Yan Cephe Pencereleri (-X ve +X)
            for (int f = 0; f < 3; f++)
            {
                float wy = -0.32f + (f * 0.333f);
                for (int s = -1; s <= 1; s += 2)
                {
                    CreateHospitalWindow(building.transform, new Vector3(s * 0.51f, wy, -0.22f), new Vector3(0.02f, 0.18f, 0.18f), glassMat, darkTrimMat);
                    CreateHospitalWindow(building.transform, new Vector3(s * 0.51f, wy, 0.22f), new Vector3(0.02f, 0.18f, 0.18f), glassMat, darkTrimMat);
                }
            }

            // 2.5 ÖN CEPHE 3D KIRMIZI HİLAL / ARTI EMBLEMİ
            GameObject crossH = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossH.name = "Red_Cross_H";
            crossH.transform.SetParent(building.transform, false);
            crossH.transform.localPosition = new Vector3(0.24f, 0.36f, 0.52f);
            crossH.transform.localScale = new Vector3(0.15f, 0.05f, 0.03f);
            crossH.GetComponent<Renderer>().sharedMaterial = redCrossMat;
            Object.Destroy(crossH.GetComponent<Collider>());

            GameObject crossV = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossV.name = "Red_Cross_V";
            crossV.transform.SetParent(building.transform, false);
            crossV.transform.localPosition = new Vector3(0.24f, 0.36f, 0.52f);
            crossV.transform.localScale = new Vector3(0.05f, 0.15f, 0.03f);
            crossV.GetComponent<Renderer>().sharedMaterial = redCrossMat;
            Object.Destroy(crossV.GetComponent<Collider>());

            // 2.6 ÇATI HELİPADİ (Helikopter Pisti)
            GameObject helipad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            helipad.name = "Rooftop_Helipad";
            helipad.transform.SetParent(building.transform, false);
            helipad.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            helipad.transform.localScale = new Vector3(0.45f, 0.02f, 0.45f);
            helipad.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(helipad.GetComponent<Collider>());

            // Helipad Sarı Çember ve "H" Harfi
            GameObject heliCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            heliCircle.name = "Helipad_Circle";
            heliCircle.transform.SetParent(helipad.transform, false);
            heliCircle.transform.localPosition = new Vector3(0f, 0.60f, 0f);
            heliCircle.transform.localScale = new Vector3(0.85f, 0.10f, 0.85f);
            heliCircle.GetComponent<Renderer>().sharedMaterial = yellowHazardMat;
            Object.Destroy(heliCircle.GetComponent<Collider>());

            GameObject heliLetterH1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            heliLetterH1.name = "Heli_Letter_H_1";
            heliLetterH1.transform.SetParent(helipad.transform, false);
            heliLetterH1.transform.localPosition = new Vector3(-0.16f, 0.70f, 0f);
            heliLetterH1.transform.localScale = new Vector3(0.08f, 0.12f, 0.45f);
            heliLetterH1.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(heliLetterH1.GetComponent<Collider>());

            GameObject heliLetterH2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            heliLetterH2.name = "Heli_Letter_H_2";
            heliLetterH2.transform.SetParent(helipad.transform, false);
            heliLetterH2.transform.localPosition = new Vector3(0.16f, 0.70f, 0f);
            heliLetterH2.transform.localScale = new Vector3(0.08f, 0.12f, 0.45f);
            heliLetterH2.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(heliLetterH2.GetComponent<Collider>());

            GameObject heliLetterHBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            heliLetterHBar.name = "Heli_Letter_H_Bar";
            heliLetterHBar.transform.SetParent(helipad.transform, false);
            heliLetterHBar.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            heliLetterHBar.transform.localScale = new Vector3(0.35f, 0.12f, 0.08f);
            heliLetterHBar.GetComponent<Renderer>().sharedMaterial = whiteTrimMat;
            Object.Destroy(heliLetterHBar.GetComponent<Collider>());

            // 3. YOLA KADAR UZANAN ASFALT AMBULANS YOLU & YÜRÜYÜŞ BAĞLANTISI
            GameObject driveway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            driveway.name = "Hospital_Ambulance_Driveway";
            driveway.transform.SetParent(hospObj.transform, false);
            driveway.transform.localPosition = new Vector3(-5.5f, 0.01f, 8.5f);
            driveway.transform.localScale = new Vector3(7.5f, 0.02f, 15.0f);
            driveway.GetComponent<Renderer>().sharedMaterial = drivewayMat;
            Object.Destroy(driveway.GetComponent<Collider>());

            // Yaya Yolu (Sağ tarafta ana kapıya uzanan taş yol)
            GameObject walkway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walkway.name = "Hospital_Pedestrian_Walkway";
            walkway.transform.SetParent(hospObj.transform, false);
            walkway.transform.localPosition = new Vector3(3.5f, 0.01f, 8.5f);
            walkway.transform.localScale = new Vector3(4.5f, 0.02f, 15.0f);
            walkway.GetComponent<Renderer>().sharedMaterial = GetMaterial("SchoolWalkwayMat", new Color(0.85f, 0.84f, 0.80f), 0.1f, 0.3f);
            Object.Destroy(walkway.GetComponent<Collider>());

            // Ambulans Park Yeri İkaz Çizgileri (Sarı Çizgiler)
            for (float s = -1.5f; s <= 1.5f; s += 0.9f)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Ambulance_Bay_Stripe";
                stripe.transform.SetParent(driveway.transform, false);
                stripe.transform.localPosition = new Vector3(0f, 0.55f, s / 15.0f);
                stripe.transform.localScale = new Vector3(0.80f, 0.05f, 0.03f);
                stripe.GetComponent<Renderer>().sharedMaterial = yellowHazardMat;
                Object.Destroy(stripe.GetComponent<Collider>());
            }

            NavMeshObstacle obs = building.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.carving = true;
        }

        private static void CreateHospitalWindow(Transform parent, Vector3 localPos, Vector3 scale, Material glassMat, Material frameMat)
        {
            GameObject winObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            winObj.name = "Hospital_Window";
            winObj.transform.SetParent(parent, false);
            winObj.transform.localPosition = localPos;
            winObj.transform.localScale = scale;
            winObj.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(winObj.GetComponent<Collider>());
        }

        #endregion

        #region Civic 4: Fire Station

        private static void BuildFireStation(Transform parent, Vector3 centerPos, Vector2 parcelSize)
        {
            GameObject fireObj = new GameObject("Fire_Station_Complex");
            fireObj.transform.SetParent(parent, false);
            fireObj.transform.position = centerPos;

            Material redMat = GetMaterial("FireRedMat", new Color(0.85f, 0.16f, 0.14f), 0.2f, 0.5f);
            Material brickMat = GetMaterial("FireBrickMat", new Color(0.72f, 0.32f, 0.24f), 0.1f, 0.3f);
            Material darkTrimMat = GetMaterial("FireDarkTrimMat", new Color(0.20f, 0.22f, 0.24f), 0.4f, 0.6f);
            Material glassMat = GetMaterial("FireGlassMat", new Color(0.20f, 0.65f, 0.85f, 0.90f), 0.8f, 0.95f);
            Material apronMat = GetMaterial("FireApronMat", new Color(0.40f, 0.42f, 0.45f), 0.1f, 0.3f);
            Material yellowHazardMat = GetMaterial("FireYellowHazardMat", new Color(0.96f, 0.82f, 0.15f), 0.0f, 0.3f);

            float bW = 20.0f;
            float bD = 10.5f;
            float bH = 7.8f;

            // 2. Ana İtfaiye Binası (Arka bahçeye hizalandı: local Z = -2.5m)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Fire_Station_Building";
            building.transform.SetParent(fireObj.transform, false);
            building.transform.localPosition = new Vector3(0f, bH / 2f, -2.5f);
            building.transform.localScale = new Vector3(bW, bH, bD);
            building.GetComponent<Renderer>().sharedMaterial = brickMat;

            // 2.1 Kaide ve Çatı Parapeti
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Fire_Plinth";
            plinth.transform.SetParent(building.transform, false);
            plinth.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            plinth.transform.localScale = new Vector3(1.02f, 0.10f, 1.02f);
            plinth.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(plinth.GetComponent<Collider>());

            GameObject parapet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            parapet.name = "Fire_Parapet";
            parapet.transform.SetParent(building.transform, false);
            parapet.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            parapet.transform.localScale = new Vector3(1.02f, 0.08f, 1.02f);
            parapet.GetComponent<Renderer>().sharedMaterial = redMat;
            Object.Destroy(parapet.GetComponent<Collider>());

            // 2.2 YOLA BAKAN (+Z KUZEY) 2 BÜYÜK İTFAİYE GARAJ KAPISI
            float[] bayXs = new float[] { -0.22f, 0.12f };
            for (int b = 0; b < bayXs.Length; b++)
            {
                GameObject bayFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bayFrame.name = $"Truck_Bay_Frame_{b + 1}";
                bayFrame.transform.SetParent(building.transform, false);
                bayFrame.transform.localPosition = new Vector3(bayXs[b], -0.15f, 0.51f);
                bayFrame.transform.localScale = new Vector3(0.28f, 0.65f, 0.04f);
                bayFrame.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
                Object.Destroy(bayFrame.GetComponent<Collider>());

                GameObject bayDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bayDoor.name = $"Truck_Bay_Door_{b + 1}";
                bayDoor.transform.SetParent(bayFrame.transform, false);
                bayDoor.transform.localPosition = new Vector3(0f, 0f, 0.20f);
                bayDoor.transform.localScale = new Vector3(0.90f, 0.92f, 0.40f);
                bayDoor.GetComponent<Renderer>().sharedMaterial = redMat;
                Object.Destroy(bayDoor.GetComponent<Collider>());

                // Garaj Kapısı Cam Izgarası
                GameObject bayGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bayGlass.name = "Bay_Glass_Row";
                bayGlass.transform.SetParent(bayDoor.transform, false);
                bayGlass.transform.localPosition = new Vector3(0f, 0.15f, 0.45f);
                bayGlass.transform.localScale = new Vector3(0.85f, 0.18f, 0.20f);
                bayGlass.GetComponent<Renderer>().sharedMaterial = glassMat;
                Object.Destroy(bayGlass.GetComponent<Collider>());
            }

            // Personel Giriş Kapısı (Sağ tarafta)
            GameObject staffDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            staffDoor.name = "Staff_Entrance_Door";
            staffDoor.transform.SetParent(building.transform, false);
            staffDoor.transform.localPosition = new Vector3(0.38f, -0.32f, 0.51f);
            staffDoor.transform.localScale = new Vector3(0.08f, 0.32f, 0.04f);
            staffDoor.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(staffDoor.GetComponent<Collider>());

            // 2.3 ÜST KAT KOMUTA PENCERELERİ
            for (float wx = -0.38f; wx <= 0.38f; wx += 0.18f)
            {
                CreateFireWindow(building.transform, new Vector3(wx, 0.25f, 0.51f), new Vector3(0.10f, 0.22f, 0.02f), glassMat, darkTrimMat);
                CreateFireWindow(building.transform, new Vector3(wx, 0.25f, -0.51f), new Vector3(0.10f, 0.22f, 0.02f), glassMat, darkTrimMat);
            }

            // 2.4 İTFAİYE VE HORTUM EĞİTİM KULESİ (Sol Köşede)
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Fire_Hose_Drill_Tower";
            tower.transform.SetParent(building.transform, false);
            tower.transform.localPosition = new Vector3(-0.44f, 0.65f, 0.20f);
            tower.transform.localScale = new Vector3(0.16f, 0.95f, 0.24f);
            tower.GetComponent<Renderer>().sharedMaterial = redMat;
            Object.Destroy(tower.GetComponent<Collider>());

            // Kule Tepe Sireni / İkaz Işığı
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Fire_Emergency_Beacon";
            beacon.transform.SetParent(tower.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            beacon.transform.localScale = new Vector3(0.35f, 0.15f, 0.35f);
            beacon.GetComponent<Renderer>().sharedMaterial = GetMaterial("FireBeaconMat", new Color(1.0f, 0.15f, 0.10f), 0.8f, 0.9f);
            Object.Destroy(beacon.GetComponent<Collider>());

            // 3. YOLA KESİNTİSİZ BAĞLANAN ASFALT ÇIKIŞ PİSTİ (Fire Apron)
            GameObject exitApron = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitApron.name = "Fire_Engine_Exit_Apron";
            exitApron.transform.SetParent(fireObj.transform, false);
            exitApron.transform.localPosition = new Vector3(-1.0f, 0.01f, 5.5f);
            exitApron.transform.localScale = new Vector3(14.0f, 0.02f, 8.5f);
            exitApron.GetComponent<Renderer>().sharedMaterial = apronMat;
            Object.Destroy(exitApron.GetComponent<Collider>());

            // Garaj Çıkışı Sarı Emniyet Çizgileri
            for (float sz = -3.0f; sz <= 3.0f; sz += 1.5f)
            {
                GameObject caution = GameObject.CreatePrimitive(PrimitiveType.Cube);
                caution.name = "Caution_Stripe";
                caution.transform.SetParent(exitApron.transform, false);
                caution.transform.localPosition = new Vector3(0f, 0.55f, sz / 8.5f);
                caution.transform.localScale = new Vector3(0.85f, 0.05f, 0.03f);
                caution.GetComponent<Renderer>().sharedMaterial = yellowHazardMat;
                Object.Destroy(caution.GetComponent<Collider>());
            }

            NavMeshObstacle obs = building.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.carving = true;
        }

        private static void CreateFireWindow(Transform parent, Vector3 localPos, Vector3 scale, Material glassMat, Material frameMat)
        {
            GameObject winObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            winObj.name = "Fire_Window";
            winObj.transform.SetParent(parent, false);
            winObj.transform.localPosition = localPos;
            winObj.transform.localScale = scale;
            winObj.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(winObj.GetComponent<Collider>());
        }

        #endregion

        #region Civic 5: Police Station

        private static void BuildPoliceStation(Transform parent, Vector3 centerPos, Vector2 parcelSize)
        {
            GameObject policeObj = new GameObject("Police_Station_Complex");
            policeObj.transform.SetParent(parent, false);
            policeObj.transform.position = centerPos;

            Material policeBlueMat = GetMaterial("PoliceNavyMat", new Color(0.12f, 0.24f, 0.45f), 0.2f, 0.5f);
            Material wallMat = GetMaterial("PoliceWallMat", new Color(0.92f, 0.94f, 0.96f), 0.1f, 0.4f);
            Material darkTrimMat = GetMaterial("PoliceDarkTrimMat", new Color(0.18f, 0.20f, 0.22f), 0.4f, 0.6f);
            Material glassMat = GetMaterial("PoliceGlassMat", new Color(0.20f, 0.65f, 0.85f, 0.90f), 0.8f, 0.95f);
            Material whiteMat = GetMaterial("PoliceWhiteMat", new Color(0.98f, 0.98f, 0.98f), 0.1f, 0.4f);
            Material flagRedMat = GetMaterial("FlagRedMat", new Color(0.88f, 0.10f, 0.12f), 0.0f, 0.3f);
            Material walkwayMat = GetMaterial("PoliceWalkwayMat", new Color(0.80f, 0.80f, 0.78f), 0.1f, 0.3f);

            float bW = 12.0f; // Doğu-Batı Derinliği
            float bD = 15.0f; // Kuzey-Güney Genişliği
            float bH = 7.5f;

            // 2. YAN SOKAĞA (-X BATI) BAKAN POLİS BİNASI (Doğuya yaslandı: local X = 4.0m)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Police_Main_Building";
            building.transform.SetParent(policeObj.transform, false);
            building.transform.localPosition = new Vector3(4.0f, bH / 2f, 0f);
            building.transform.localScale = new Vector3(bW, bH, bD);
            building.GetComponent<Renderer>().sharedMaterial = wallMat;

            // 2.1 Kaide ve Mavi Güvenlik Kuşağı
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Police_Plinth";
            plinth.transform.SetParent(building.transform, false);
            plinth.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            plinth.transform.localScale = new Vector3(1.02f, 0.10f, 1.02f);
            plinth.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.Destroy(plinth.GetComponent<Collider>());

            GameObject blueBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blueBand.name = "Police_Blue_Stripe";
            blueBand.transform.SetParent(building.transform, false);
            blueBand.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            blueBand.transform.localScale = new Vector3(1.02f, 0.12f, 1.02f);
            blueBand.GetComponent<Renderer>().sharedMaterial = policeBlueMat;
            Object.Destroy(blueBand.GetComponent<Collider>());

            // 2.2 YAN SOKAĞA (-X BATI) BAKAN GİRİŞ REVAKI & GÜVENLİK KAPISI
            GameObject portico = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portico.name = "Police_West_Portico";
            portico.transform.SetParent(building.transform, false);
            portico.transform.localPosition = new Vector3(-0.52f, -0.15f, 0f);
            portico.transform.localScale = new Vector3(0.12f, 0.65f, 0.35f);
            portico.GetComponent<Renderer>().sharedMaterial = policeBlueMat;
            Object.Destroy(portico.GetComponent<Collider>());

            // Çift Camlı Güvenlik Giriş Kapısı
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Security_Glass_Door";
            door.transform.SetParent(portico.transform, false);
            door.transform.localPosition = new Vector3(-0.15f, -0.10f, 0f);
            door.transform.localScale = new Vector3(0.25f, 0.75f, 0.70f);
            door.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(door.GetComponent<Collider>());

            // 2.3 CEPHE PENCERELERİ (Batı, Doğu, Kuzey, Güney)
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.20f + (f * 0.45f);
                // Batı Cephesi (-X / Yan Sokağa Bakan)
                CreateFireWindow(building.transform, new Vector3(-0.51f, wy, -0.30f), new Vector3(0.02f, 0.22f, 0.16f), glassMat, darkTrimMat);
                CreateFireWindow(building.transform, new Vector3(-0.51f, wy, 0.30f), new Vector3(0.02f, 0.22f, 0.16f), glassMat, darkTrimMat);

                // Kuzey ve Güney Cepheleri
                CreateFireWindow(building.transform, new Vector3(0f, wy, 0.51f), new Vector3(0.24f, 0.22f, 0.02f), glassMat, darkTrimMat);
                CreateFireWindow(building.transform, new Vector3(0f, wy, -0.51f), new Vector3(0.24f, 0.22f, 0.02f), glassMat, darkTrimMat);
            }

            // 2.4 ÇATI MAVİ POLİS SİRENİ / FENERİ
            GameObject siren = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            siren.name = "Police_Beacon_Light";
            siren.transform.SetParent(building.transform, false);
            siren.transform.localPosition = new Vector3(-0.35f, 0.55f, 0f);
            siren.transform.localScale = new Vector3(0.12f, 0.10f, 0.12f);
            siren.GetComponent<Renderer>().sharedMaterial = GetMaterial("PoliceBeaconMat", new Color(0.10f, 0.60f, 1.0f), 0.5f, 0.9f);
            Object.Destroy(siren.GetComponent<Collider>());

            // 3. YAN SOKAĞA (-X BATI) UZANAN TAŞ YÜRÜYÜŞ YOLU VE OTOPARK
            GameObject westPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westPath.name = "Police_Entrance_Walkway";
            westPath.transform.SetParent(policeObj.transform, false);
            westPath.transform.localPosition = new Vector3(-7.5f, 0.01f, 0f);
            westPath.transform.localScale = new Vector3(14.5f, 0.02f, 6.0f);
            westPath.GetComponent<Renderer>().sharedMaterial = walkwayMat;
            Object.Destroy(westPath.GetComponent<Collider>());

            // 4. ÖNÜNDEKİ BEYAZ DİREK VE KIRMIZI TÜRK BAYRAĞI (Yan Sokağa Bakan Ön Bahçede)
            GameObject flagGroup = new GameObject("Police_Flag_Monument");
            flagGroup.transform.SetParent(policeObj.transform, false);
            flagGroup.transform.localPosition = new Vector3(-9.0f, 0f, 4.5f);

            // Kaide
            GameObject flagBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagBase.name = "Flag_Base";
            flagBase.transform.SetParent(flagGroup.transform, false);
            flagBase.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            flagBase.transform.localScale = new Vector3(1.2f, 0.20f, 1.2f);
            flagBase.GetComponent<Renderer>().sharedMaterial = whiteMat;
            Object.Destroy(flagBase.GetComponent<Collider>());

            // Beyaz Direk
            GameObject flagPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagPole.name = "White_Flag_Pole";
            flagPole.transform.SetParent(flagGroup.transform, false);
            flagPole.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            flagPole.transform.localScale = new Vector3(0.12f, 3.6f, 0.12f);
            flagPole.GetComponent<Renderer>().sharedMaterial = whiteMat;
            Object.Destroy(flagPole.GetComponent<Collider>());

            // Altın Tepe Küresi
            GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            finial.name = "Gold_Finial";
            finial.transform.SetParent(flagGroup.transform, false);
            finial.transform.localPosition = new Vector3(0f, 7.45f, 0f);
            finial.transform.localScale = new Vector3(0.30f, 0.30f, 0.30f);
            finial.GetComponent<Renderer>().sharedMaterial = GetMaterial("GoldFinialMat", new Color(0.95f, 0.80f, 0.20f), 0.8f, 0.8f);
            Object.Destroy(finial.GetComponent<Collider>());

            // Kırmızı Dalgalanan Türk Bayrağı
            GameObject flagCloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagCloth.name = "Turkish_Flag_Cloth";
            flagCloth.transform.SetParent(flagGroup.transform, false);
            flagCloth.transform.localPosition = new Vector3(1.25f, 6.4f, 0f);
            flagCloth.transform.localScale = new Vector3(2.4f, 1.6f, 0.04f);
            flagCloth.GetComponent<Renderer>().sharedMaterial = flagRedMat;
            Object.Destroy(flagCloth.GetComponent<Collider>());

            // Bayrak Hilal & Yıldız Detayı
            GameObject crescent = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crescent.name = "Flag_Crescent_Moon";
            crescent.transform.SetParent(flagCloth.transform, false);
            crescent.transform.localPosition = new Vector3(-0.15f, 0f, 0.55f);
            crescent.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            crescent.transform.localScale = new Vector3(0.45f, 0.10f, 0.45f);
            crescent.GetComponent<Renderer>().sharedMaterial = whiteMat;
            Object.Destroy(crescent.GetComponent<Collider>());

            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Flag_Star";
            star.transform.SetParent(flagCloth.transform, false);
            star.transform.localPosition = new Vector3(0.20f, 0.05f, 0.55f);
            star.transform.localScale = new Vector3(0.20f, 0.20f, 0.10f);
            star.GetComponent<Renderer>().sharedMaterial = whiteMat;
            Object.Destroy(star.GetComponent<Collider>());

            NavMeshObstacle obs = building.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.carving = true;
        }

        #endregion

        #region Civic 6: Gas Station

        private static void BuildGasStation(Transform parent, Vector3 centerPos, Vector2 parcelSize)
        {
            GameObject gasObj = new GameObject("Gas_Station_Complex");
            gasObj.transform.SetParent(parent, false);
            gasObj.transform.position = centerPos;

            Material petrolGreenMat = GetMaterial("PetrolGreenMat", new Color(0.10f, 0.55f, 0.30f), 0.3f, 0.6f);
            Material yellowAccentMat = GetMaterial("PetrolYellowMat", new Color(0.96f, 0.82f, 0.15f), 0.2f, 0.5f);
            Material whiteMat = GetMaterial("PetrolWhiteMat", new Color(0.96f, 0.96f, 0.98f), 0.1f, 0.4f);
            Material pumpDarkMat = GetMaterial("PetrolPumpMat", new Color(0.22f, 0.24f, 0.26f), 0.5f, 0.6f);
            Material glassMat = GetMaterial("PetrolGlassMat", new Color(0.20f, 0.65f, 0.85f, 0.90f), 0.8f, 0.95f);
            Material tarmacMat = GetMaterial("PetrolTarmacMat", new Color(0.32f, 0.34f, 0.36f), 0.0f, 0.2f);

            // 1.1 Asfalt Dolum Pisti (Sadece Kanopi ve Pompalar Altında - Yola Sıfır Taşma)
            GameObject driveway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            driveway.name = "Gas_Station_Driveway_Apron";
            driveway.transform.SetParent(gasObj.transform, false);
            driveway.transform.localPosition = new Vector3(0f, 0.01f, -4.5f);
            driveway.transform.localScale = new Vector3(22.0f, 0.02f, 12.0f);
            driveway.GetComponent<Renderer>().sharedMaterial = tarmacMat;
            Object.Destroy(driveway.GetComponent<Collider>());

            // 2. EN AŞAĞI YOLA BAKAN (-Z GÜNEY) IŞIKLI KANOPİ (local Z = -4.0m)
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Gas_Canopy_Roof";
            canopy.transform.SetParent(gasObj.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 4.8f, -4.0f);
            canopy.transform.localScale = new Vector3(18.0f, 0.65f, 8.5f);
            canopy.GetComponent<Renderer>().sharedMaterial = petrolGreenMat;
            Object.Destroy(canopy.GetComponent<Collider>());

            // Kanopi Sarı LED Şeridi
            GameObject canopyStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopyStripe.name = "Canopy_LED_Stripe";
            canopyStripe.transform.SetParent(canopy.transform, false);
            canopyStripe.transform.localPosition = new Vector3(0f, -0.30f, 0f);
            canopyStripe.transform.localScale = new Vector3(1.02f, 0.15f, 1.02f);
            canopyStripe.GetComponent<Renderer>().sharedMaterial = yellowAccentMat;
            Object.Destroy(canopyStripe.GetComponent<Collider>());

            // 4 Çelik Taşıyıcı Kolon
            for (int dirX = -1; dirX <= 1; dirX += 2)
            {
                for (int dirZ = -1; dirZ <= 1; dirZ += 2)
                {
                    GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    col.name = "Canopy_Pillar";
                    col.transform.SetParent(gasObj.transform, false);
                    col.transform.localPosition = new Vector3(dirX * 6.5f, 2.4f, -4.0f + (dirZ * 2.8f));
                    col.transform.localScale = new Vector3(0.32f, 2.4f, 0.32f);
                    col.GetComponent<Renderer>().sharedMaterial = whiteMat;
                    Object.Destroy(col.GetComponent<Collider>());
                }
            }

            // 3. 2 ADET BENZİN DOLUM POMPASI (Fuel Pump Islands)
            for (int px = -1; px <= 1; px += 2)
            {
                GameObject pumpIsland = new GameObject($"Fuel_Pump_Island_{px}");
                pumpIsland.transform.SetParent(gasObj.transform, false);
                pumpIsland.transform.localPosition = new Vector3(px * 4.2f, 0f, -4.0f);

                // Beton Ada Kaidesi
                GameObject baseIsland = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseIsland.name = "Pump_Base";
                baseIsland.transform.SetParent(pumpIsland.transform, false);
                baseIsland.transform.localPosition = new Vector3(0f, 0.10f, 0f);
                baseIsland.transform.localScale = new Vector3(1.2f, 0.20f, 3.2f);
                baseIsland.GetComponent<Renderer>().sharedMaterial = whiteMat;
                Object.Destroy(baseIsland.GetComponent<Collider>());

                // Dijital Pompa Gövdesi
                GameObject pumpBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pumpBody.name = "Pump_Dispenser";
                pumpBody.transform.SetParent(pumpIsland.transform, false);
                pumpBody.transform.localPosition = new Vector3(0f, 1.10f, 0f);
                pumpBody.transform.localScale = new Vector3(0.70f, 1.80f, 1.6f);
                pumpBody.GetComponent<Renderer>().sharedMaterial = pumpDarkMat;
                Object.Destroy(pumpBody.GetComponent<Collider>());

                // Dijital Ekran ve Hortumlar
                GameObject display = GameObject.CreatePrimitive(PrimitiveType.Cube);
                display.name = "Digital_Display";
                display.transform.SetParent(pumpBody.transform, false);
                display.transform.localPosition = new Vector3(0.52f, 0.20f, 0f);
                display.transform.localScale = new Vector3(0.08f, 0.35f, 0.80f);
                display.GetComponent<Renderer>().sharedMaterial = yellowAccentMat;
                Object.Destroy(display.GetComponent<Collider>());

                // Güvenlik Dubaları
                for (int b = -1; b <= 1; b += 2)
                {
                    GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bollard.name = "Safety_Bollard";
                    bollard.transform.SetParent(pumpIsland.transform, false);
                    bollard.transform.localPosition = new Vector3(0f, 0.40f, b * 1.35f);
                    bollard.transform.localScale = new Vector3(0.12f, 0.40f, 0.12f);
                    bollard.GetComponent<Renderer>().sharedMaterial = yellowAccentMat;
                    Object.Destroy(bollard.GetComponent<Collider>());
                }
            }

            // 4. KANOPİ ALTINDAKİ GECE IŞIKLANDIRMASI (2 Adet Tavan Projektörü)
            for (int lx = -1; lx <= 1; lx += 2)
            {
                GameObject lightObj = new GameObject($"Canopy_Floodlight_{lx}");
                lightObj.transform.SetParent(canopy.transform, false);
                lightObj.transform.localPosition = new Vector3(lx * 0.25f, -0.45f, 0f);

                Light cLight = lightObj.AddComponent<Light>();
                cLight.type = LightType.Point;
                cLight.color = new Color(1.0f, 0.98f, 0.88f);
                cLight.intensity = 3.0f;
                cLight.range = 10.0f;
                cLight.shadows = LightShadows.None;
                cLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

                if (DayNightCycleManager.Instance != null)
                {
                    DayNightCycleManager.Instance.RegisterStoreInteriorLight(cLight);
                }
            }

            // 5. KÜÇÜK MARKET BİNASI (Kuzey tarafında: local Z = 5.5m)
            GameObject store = GameObject.CreatePrimitive(PrimitiveType.Cube);
            store.name = "Gas_Station_Mini_Mart";
            store.transform.SetParent(gasObj.transform, false);
            store.transform.localPosition = new Vector3(0f, 2.1f, 5.5f);
            store.transform.localScale = new Vector3(13.0f, 4.2f, 6.0f);
            store.GetComponent<Renderer>().sharedMaterial = whiteMat;

            // Market Cam Vitrini ve Kapısı
            GameObject storeGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            storeGlass.name = "Mart_Glass_Storefront";
            storeGlass.transform.SetParent(store.transform, false);
            storeGlass.transform.localPosition = new Vector3(0f, -0.10f, -0.51f);
            storeGlass.transform.localScale = new Vector3(0.85f, 0.65f, 0.04f);
            storeGlass.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(storeGlass.GetComponent<Collider>());

            // Market Çatı Işıklı Tabelası
            GameObject storeSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            storeSign.name = "Mart_Roof_Sign";
            storeSign.transform.SetParent(store.transform, false);
            storeSign.transform.localPosition = new Vector3(0f, 0.58f, -0.45f);
            storeSign.transform.localScale = new Vector3(0.55f, 0.20f, 0.10f);
            storeSign.GetComponent<Renderer>().sharedMaterial = petrolGreenMat;
            Object.Destroy(storeSign.GetComponent<Collider>());

            NavMeshObstacle obs = store.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.carving = true;
        }

        #endregion

        #region Civic 7: City Central Bank

        private static void BuildCityBank(Transform parent, Vector3 centerPos, Vector2 parcelSize)
        {
            GameObject bankObj = new GameObject("City_Bank_Complex");
            bankObj.transform.SetParent(parent, false);
            bankObj.transform.position = centerPos;

            Material travertineMat = GetMaterial("BankTravertineMat", new Color(0.94f, 0.93f, 0.90f), 0.1f, 0.5f);
            Material graniteMat = GetMaterial("BankGraniteMat", new Color(0.20f, 0.22f, 0.24f), 0.5f, 0.7f);
            Material goldMat = GetMaterial("BankGoldMat", new Color(0.95f, 0.80f, 0.20f), 0.8f, 0.9f);
            Material glassMat = GetMaterial("BankGlassMat", new Color(0.20f, 0.65f, 0.85f, 0.90f), 0.8f, 0.95f);
            Material whiteMat = GetMaterial("BankWhiteMat", new Color(0.98f, 0.98f, 0.98f), 0.1f, 0.4f);
            Material roofCopperMat = GetMaterial("BankRoofCopperMat", new Color(0.25f, 0.40f, 0.36f), 0.2f, 0.4f);
            Material plazaMat = GetMaterial("BankPlazaGraniteMat", new Color(0.78f, 0.78f, 0.80f), 0.2f, 0.4f);
            Material atmBlueMat = GetMaterial("BankAtmBlueMat", new Color(0.10f, 0.45f, 0.85f), 0.5f, 0.7f);
            Material atmScreenLitMat = GetMaterial("BankAtmScreenLitMat", new Color(0.20f, 0.90f, 0.80f), 0.2f, 0.9f);

            float bW = 22.0f;
            float bD = 13.0f;
            float bH = 8.6f;

            // 2. Ana Banka Binası (Arka bahçe tarafına hizalandı: local Z = -5.0m)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Bank_Main_Building";
            building.transform.SetParent(bankObj.transform, false);
            building.transform.localPosition = new Vector3(0f, bH / 2f, -5.0f);
            building.transform.localScale = new Vector3(bW, bH, bD);
            building.GetComponent<Renderer>().sharedMaterial = travertineMat;

            // 2.1 Granit Kaide & Kat Silmeleri
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Bank_Granite_Plinth";
            plinth.transform.SetParent(building.transform, false);
            plinth.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            plinth.transform.localScale = new Vector3(1.02f, 0.10f, 1.02f);
            plinth.GetComponent<Renderer>().sharedMaterial = graniteMat;
            Object.Destroy(plinth.GetComponent<Collider>());

            GameObject floorSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorSlab.name = "Bank_Floor_Slab";
            floorSlab.transform.SetParent(building.transform, false);
            floorSlab.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            floorSlab.transform.localScale = new Vector3(1.02f, 0.04f, 1.02f);
            floorSlab.GetComponent<Renderer>().sharedMaterial = goldMat;
            Object.Destroy(floorSlab.GetComponent<Collider>());

            // 2.2 YOLA BAKAN (+Z KUZEY) 6 SÜTUNLU ANITSAL MAVİ/ALTIN GİRİŞ REVAKI & MERDİVENLER
            GameObject portico = new GameObject("Bank_Front_Portico");
            portico.transform.SetParent(building.transform, false);
            portico.transform.localPosition = new Vector3(0f, -0.50f, 0.50f);

            for (int i = 0; i < 6; i++)
            {
                float px = -0.25f + (i * 0.10f);
                GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                column.name = $"Bank_Column_{i + 1}";
                column.transform.SetParent(portico.transform, false);
                column.transform.localPosition = new Vector3(px, 0.25f, 0.12f);
                column.transform.localScale = new Vector3(0.026f, 0.25f, 0.026f);
                column.GetComponent<Renderer>().sharedMaterial = whiteMat;
                Object.Destroy(column.GetComponent<Collider>());
            }

            // Klasik Alınlık Çatı
            GameObject pediment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pediment.name = "Bank_Pediment";
            pediment.transform.SetParent(portico.transform, false);
            pediment.transform.localPosition = new Vector3(0f, 0.54f, 0.06f);
            pediment.transform.localScale = new Vector3(0.55f, 0.08f, 0.16f);
            pediment.GetComponent<Renderer>().sharedMaterial = roofCopperMat;
            Object.Destroy(pediment.GetComponent<Collider>());

            // Pediment Üzerinde Altın Amblem ($ / ₺ Kabartması)
            GameObject emblem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emblem.name = "Bank_Gold_Emblem";
            emblem.transform.SetParent(pediment.transform, false);
            emblem.transform.localPosition = new Vector3(0f, 0.05f, 0.52f);
            emblem.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            emblem.transform.localScale = new Vector3(0.40f, 0.10f, 0.40f);
            emblem.GetComponent<Renderer>().sharedMaterial = goldMat;
            Object.Destroy(emblem.GetComponent<Collider>());

            // Geniş Mermer Giriş Merdivenleri (4 Basamak)
            for (int s = 0; s < 4; s++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Bank_Step_{s + 1}";
                step.transform.SetParent(portico.transform, false);
                step.transform.localPosition = new Vector3(0f, (s * 0.025f) + 0.012f, 0.16f - (s * 0.035f));
                step.transform.localScale = new Vector3(0.48f + (s * 0.02f), 0.025f, 0.07f);
                step.GetComponent<Renderer>().sharedMaterial = whiteMat;
                Object.Destroy(step.GetComponent<Collider>());
            }

            // 2.3 BRONZ VE ÇİFT CAMLI DÖNER GÜVENLİK KAPISI
            GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorFrame.name = "Bank_Door_Frame";
            doorFrame.transform.SetParent(building.transform, false);
            doorFrame.transform.localPosition = new Vector3(0f, -0.32f, 0.51f);
            doorFrame.transform.localScale = new Vector3(0.18f, 0.35f, 0.03f);
            doorFrame.GetComponent<Renderer>().sharedMaterial = graniteMat;
            Object.Destroy(doorFrame.GetComponent<Collider>());

            GameObject glassDoorL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassDoorL.name = "Glass_Door_Left";
            glassDoorL.transform.SetParent(doorFrame.transform, false);
            glassDoorL.transform.localPosition = new Vector3(-0.24f, 0f, 0.10f);
            glassDoorL.transform.localScale = new Vector3(0.45f, 0.90f, 0.40f);
            glassDoorL.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(glassDoorL.GetComponent<Collider>());

            GameObject glassDoorR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassDoorR.name = "Glass_Door_Right";
            glassDoorR.transform.SetParent(doorFrame.transform, false);
            glassDoorR.transform.localPosition = new Vector3(0.24f, 0f, 0.10f);
            glassDoorR.transform.localScale = new Vector3(0.45f, 0.90f, 0.40f);
            glassDoorR.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(glassDoorR.GetComponent<Collider>());

            // 2.4 TÜM KATLARDA VE CEPHELERDE KALİTELİ PENCERELER (Ön, Arka, Sol, Sağ)
            // Ön Cephe Pencereleri (+Z)
            float[] frontWinXs = new float[] { -0.38f, -0.22f, 0.22f, 0.38f };
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.24f + (f * 0.45f);
                foreach (float wx in frontWinXs)
                {
                    CreateSchoolWindow(building.transform, new Vector3(wx, wy, 0.51f), new Vector3(0.10f, 0.25f, 0.02f), glassMat, graniteMat);
                }
            }

            // Arka Cephe Pencereleri (-Z)
            float[] rearWinXs = new float[] { -0.38f, -0.20f, 0f, 0.20f, 0.38f };
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.24f + (f * 0.45f);
                foreach (float wx in rearWinXs)
                {
                    CreateSchoolWindow(building.transform, new Vector3(wx, wy, -0.51f), new Vector3(0.10f, 0.25f, 0.02f), glassMat, graniteMat);
                }
            }

            // Yan Cephe Pencereleri (-X ve +X)
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.24f + (f * 0.45f);
                for (int s = -1; s <= 1; s += 2)
                {
                    CreateSchoolWindow(building.transform, new Vector3(s * 0.51f, wy, -0.20f), new Vector3(0.02f, 0.25f, 0.16f), glassMat, graniteMat);
                    CreateSchoolWindow(building.transform, new Vector3(s * 0.51f, wy, 0.20f), new Vector3(0.02f, 0.25f, 0.16f), glassMat, graniteMat);
                }
            }

            // 2.5 BAKIR KIRMA ÇATI
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Bank_Roof";
            roof.transform.SetParent(building.transform, false);
            roof.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            roof.transform.localScale = new Vector3(1.06f, 0.22f, 1.06f);
            roof.GetComponent<Renderer>().sharedMaterial = roofCopperMat;
            Object.Destroy(roof.GetComponent<Collider>());

            // 3. 2 ADET IŞIKLI MODERN BANKA ATM'Sİ (Ön Cephe Sol Tarafında)
            for (int a = 0; a < 2; a++)
            {
                GameObject atm = new GameObject($"Bank_ATM_{a + 1}");
                atm.transform.SetParent(bankObj.transform, false);
                atm.transform.localPosition = new Vector3(-7.5f + (a * 2.2f), 0f, 3.5f);

                // ATM Kabini
                GameObject atmKiosk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                atmKiosk.name = "ATM_Kiosk";
                atmKiosk.transform.SetParent(atm.transform, false);
                atmKiosk.transform.localPosition = new Vector3(0f, 1.25f, 0f);
                atmKiosk.transform.localScale = new Vector3(1.4f, 2.5f, 1.2f);
                atmKiosk.GetComponent<Renderer>().sharedMaterial = atmBlueMat;
                Object.Destroy(atmKiosk.GetComponent<Collider>());

                // Işıklı Ekran
                GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
                screen.name = "ATM_Screen";
                screen.transform.SetParent(atm.transform, false);
                screen.transform.localPosition = new Vector3(0f, 1.45f, 0.62f);
                screen.transform.localScale = new Vector3(0.80f, 0.55f, 0.05f);
                screen.GetComponent<Renderer>().sharedMaterial = atmScreenLitMat;
                Object.Destroy(screen.GetComponent<Collider>());

                // Tuş Takımı ve Para Haznesi
                GameObject keypad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                keypad.name = "ATM_Keypad";
                keypad.transform.SetParent(atm.transform, false);
                keypad.transform.localPosition = new Vector3(0f, 1.05f, 0.65f);
                keypad.transform.localScale = new Vector3(0.70f, 0.15f, 0.18f);
                keypad.GetComponent<Renderer>().sharedMaterial = graniteMat;
                Object.Destroy(keypad.GetComponent<Collider>());

                // Güvenlik Dubası
                GameObject bollard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bollard.name = "ATM_Safety_Bollard";
                bollard.transform.SetParent(atm.transform, false);
                bollard.transform.localPosition = new Vector3(0f, 0.45f, 1.4f);
                bollard.transform.localScale = new Vector3(0.12f, 0.45f, 0.12f);
                bollard.GetComponent<Renderer>().sharedMaterial = goldMat;
                Object.Destroy(bollard.GetComponent<Collider>());
            }

            // 4. YOLA KADAR UZANAN GRANİT GİRİŞ PLAZASI & TAŞ YÜRÜYÜŞ YOLU (Kuzey Kaldırımına Kesintisiz Bağlantı)
            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = "Bank_Entrance_Walkway";
            path.transform.SetParent(bankObj.transform, false);
            path.transform.localPosition = new Vector3(0f, 0.01f, 10.0f);
            path.transform.localScale = new Vector3(7.5f, 0.02f, 19.0f);
            path.GetComponent<Renderer>().sharedMaterial = plazaMat;
            Object.Destroy(path.GetComponent<Collider>());

            // 5. GECE DIŞ CEPHE SPOT AYDINLATMASI (2 Adet Mimari Işık)
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject spotObj = new GameObject($"Bank_Spotlight_{s}");
                spotObj.transform.SetParent(bankObj.transform, false);
                spotObj.transform.localPosition = new Vector3(s * 6.5f, 0.20f, 2.5f);

                Light bLight = spotObj.AddComponent<Light>();
                bLight.type = LightType.Point;
                bLight.color = new Color(1.0f, 0.95f, 0.80f);
                bLight.intensity = 2.5f;
                bLight.range = 10.0f;
                bLight.shadows = LightShadows.None;
                bLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

                if (DayNightCycleManager.Instance != null)
                {
                    DayNightCycleManager.Instance.RegisterStoreInteriorLight(bLight);
                }
            }

            NavMeshObstacle obs = building.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.carving = true;
        }

        #endregion

        #region Civic 8: Entertainment Center

        private static void BuildEntertainmentCenter(Transform parent, Vector3 centerPos, Vector2 parcelSize)
        {
            GameObject arcadeObj = new GameObject("Entertainment_Center_Complex");
            arcadeObj.transform.SetParent(parent, false);
            arcadeObj.transform.position = centerPos;

            Material darkMetalMat = GetMaterial("ArcadeDarkMat", new Color(0.14f, 0.16f, 0.18f), 0.5f, 0.7f);
            Material magentaNeonMat = GetMaterial("ArcadeMagentaMat", new Color(0.92f, 0.10f, 0.65f), 0.2f, 0.8f);
            Material cyanNeonMat = GetMaterial("ArcadeCyanMat", new Color(0.10f, 0.85f, 0.95f), 0.2f, 0.8f);
            Material glassMat = GetMaterial("ArcadeGlassMat", new Color(0.15f, 0.60f, 0.85f, 0.92f), 0.8f, 0.95f);
            Material plazaMat = GetMaterial("ArcadePlazaMat", new Color(0.35f, 0.36f, 0.38f), 0.1f, 0.3f);
            Material yellowAccentMat = GetMaterial("ArcadeYellowMat", new Color(0.96f, 0.82f, 0.15f), 0.2f, 0.5f);

            float bW = 20.0f;
            float bD = 11.0f;
            float bH = 8.2f;

            // 2. Ana Eğlence Merkezi Binası (Arka bahçe tarafına hizalandı: local Z = -3.5m)
            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = "Entertainment_Main_Building";
            building.transform.SetParent(arcadeObj.transform, false);
            building.transform.localPosition = new Vector3(0f, bH / 2f, -3.5f);
            building.transform.localScale = new Vector3(bW, bH, bD);
            building.GetComponent<Renderer>().sharedMaterial = darkMetalMat;

            // 2.1 Neon Çatı Parapeti ve Çerçevesi
            GameObject roofNeon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofNeon.name = "Roof_Neon_Trim";
            roofNeon.transform.SetParent(building.transform, false);
            roofNeon.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            roofNeon.transform.localScale = new Vector3(1.02f, 0.08f, 1.02f);
            roofNeon.GetComponent<Renderer>().sharedMaterial = magentaNeonMat;
            Object.Destroy(roofNeon.GetComponent<Collider>());

            GameObject bandNeon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bandNeon.name = "Mid_Cyan_Neon_Band";
            bandNeon.transform.SetParent(building.transform, false);
            bandNeon.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            bandNeon.transform.localScale = new Vector3(1.02f, 0.04f, 1.02f);
            bandNeon.GetComponent<Renderer>().sharedMaterial = cyanNeonMat;
            Object.Destroy(bandNeon.GetComponent<Collider>());

            // 2.2 YOLA BAKAN (+Z KUZEY) IŞIKLI NEON GİRİŞ PORTALI
            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portal.name = "Arcade_Neon_Entrance_Portal";
            portal.transform.SetParent(building.transform, false);
            portal.transform.localPosition = new Vector3(0f, -0.15f, 0.52f);
            portal.transform.localScale = new Vector3(0.50f, 0.68f, 0.08f);
            portal.GetComponent<Renderer>().sharedMaterial = magentaNeonMat;
            Object.Destroy(portal.GetComponent<Collider>());

            // Çift Camlı Otomatik Giriş Kapısı
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Arcade_Glass_Doors";
            door.transform.SetParent(portal.transform, false);
            door.transform.localPosition = new Vector3(0f, -0.10f, 0.20f);
            door.transform.localScale = new Vector3(0.75f, 0.78f, 0.40f);
            door.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.Destroy(door.GetComponent<Collider>());

            // Giriş Kanopisi
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Portal_Canopy";
            canopy.transform.SetParent(portal.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 0.45f, 0.80f);
            canopy.transform.localScale = new Vector3(1.10f, 0.12f, 1.20f);
            canopy.GetComponent<Renderer>().sharedMaterial = cyanNeonMat;
            Object.Destroy(canopy.GetComponent<Collider>());

            // 2.3 CEPHE PENCERELERİ VE OYUN SALONU CAM VİTRİNLERİ
            for (int f = 0; f < 2; f++)
            {
                float wy = -0.22f + (f * 0.45f);
                // Ön Cephe (+Z)
                CreateSchoolWindow(building.transform, new Vector3(-0.35f, wy, 0.51f), new Vector3(0.18f, 0.25f, 0.02f), glassMat, darkMetalMat);
                CreateSchoolWindow(building.transform, new Vector3(0.35f, wy, 0.51f), new Vector3(0.18f, 0.25f, 0.02f), glassMat, darkMetalMat);

                // Arka Cephe (-Z)
                CreateSchoolWindow(building.transform, new Vector3(-0.30f, wy, -0.51f), new Vector3(0.22f, 0.25f, 0.02f), glassMat, darkMetalMat);
                CreateSchoolWindow(building.transform, new Vector3(0.30f, wy, -0.51f), new Vector3(0.22f, 0.25f, 0.02f), glassMat, darkMetalMat);
            }

            // 2.4 VİTRİNLERDEKİ OYUN MAKİNESİ SİLUETLERİ / REKLAM IŞIKLARI
            for (int v = -1; v <= 1; v += 2)
            {
                GameObject arcadeCab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arcadeCab.name = $"Arcade_Cabinet_Window_{v}";
                arcadeCab.transform.SetParent(building.transform, false);
                arcadeCab.transform.localPosition = new Vector3(v * 0.35f, -0.22f, 0.45f);
                arcadeCab.transform.localScale = new Vector3(0.08f, 0.18f, 0.06f);
                arcadeCab.GetComponent<Renderer>().sharedMaterial = yellowAccentMat;
                Object.Destroy(arcadeCab.GetComponent<Collider>());
            }

            // 4. GECE NEON SPOT AYDINLATMASI (2 Adet Mimari Işık)
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject spotObj = new GameObject($"Arcade_Spotlight_{s}");
                spotObj.transform.SetParent(arcadeObj.transform, false);
                spotObj.transform.localPosition = new Vector3(s * 5.0f, 0.20f, 2.5f);

                Light aLight = spotObj.AddComponent<Light>();
                aLight.type = LightType.Point;
                aLight.color = (s == -1) ? new Color(0.95f, 0.20f, 0.80f) : new Color(0.10f, 0.85f, 0.95f);
                aLight.intensity = 2.8f;
                aLight.range = 11.0f;
                aLight.shadows = LightShadows.None;
                aLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

                if (DayNightCycleManager.Instance != null)
                {
                    DayNightCycleManager.Instance.RegisterStoreInteriorLight(aLight);
                }
            }

            NavMeshObstacle obs = building.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.carving = true;
        }

        #endregion

        #region Civic 9: Football Stadium

        private static void BuildFootballStadium(Transform parent, Vector3 centerPos, Vector2 parcelSize)
        {
            GameObject stadiumObj = new GameObject("Football_Stadium_Complex");
            stadiumObj.transform.SetParent(parent, false);
            stadiumObj.transform.position = centerPos;

            Material pitchMat = GetMaterial("StadiumPitchMat", new Color(0.18f, 0.55f, 0.20f), 0.0f, 0.1f);
            Material stripeMat = GetMaterial("StadiumStripeMat", new Color(0.22f, 0.60f, 0.24f), 0.0f, 0.1f);
            Material standMat = GetMaterial("StadiumStandConcreteMat", new Color(0.40f, 0.42f, 0.46f), 0.2f, 0.4f);
            Material seatBlueMat = GetMaterial("StadiumSeatBlueMat", new Color(0.12f, 0.35f, 0.75f), 0.3f, 0.5f);
            Material whiteLineMat = GetMaterial("StadiumWhiteLineMat", new Color(0.98f, 0.98f, 0.98f), 0.0f, 0.3f);
            Material netMat = GetMaterial("StadiumNetMat", new Color(0.90f, 0.90f, 0.92f, 0.65f), 0.1f, 0.3f);
            Material fenceMat = GetMaterial("StadiumFenceDarkMat", new Color(0.18f, 0.20f, 0.22f), 0.4f, 0.6f);
            Material poleMat = GetMaterial("StadiumFloodlightPoleMat", new Color(0.28f, 0.30f, 0.34f), 0.5f, 0.6f);
            Material roofCanopyMat = GetMaterial("StadiumCanopyRoofMat", new Color(0.85f, 0.86f, 0.88f), 0.2f, 0.5f);

            // 2. ÇİTLERLE ÇEVRİLİ SAHA ALANI (Stadium Perimeter Fence)
            float fenceW = 26.0f;
            float fenceD = 34.0f;
            float fenceH = 2.4f;

            // Çit Direkleri ve Çit Telleri
            GameObject fenceGroup = new GameObject("Stadium_Perimeter_Fence");
            fenceGroup.transform.SetParent(stadiumObj.transform, false);
            fenceGroup.transform.localPosition = Vector3.zero;

            // Kuzey, Güney, Batı, Doğu Çit Panelleri
            CreateFenceLine(fenceGroup.transform, new Vector3(0f, fenceH / 2f, fenceD / 2f), new Vector3(fenceW, fenceH, 0.06f), fenceMat);
            CreateFenceLine(fenceGroup.transform, new Vector3(0f, fenceH / 2f, -fenceD / 2f), new Vector3(fenceW, fenceH, 0.06f), fenceMat);
            CreateFenceLine(fenceGroup.transform, new Vector3(-fenceW / 2f, fenceH / 2f, 0f), new Vector3(0.06f, fenceH, fenceD), fenceMat);
            CreateFenceLine(fenceGroup.transform, new Vector3(fenceW / 2f, fenceH / 2f, 0f), new Vector3(0.06f, fenceH, fenceD), fenceMat);

            // 3. ŞERİTLİ ÇİM FUTBOL SAHASI (Field Turf)
            GameObject pitch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pitch.name = "Football_Pitch_Turf";
            pitch.transform.SetParent(stadiumObj.transform, false);
            pitch.transform.localPosition = new Vector3(1.5f, 0.01f, 0f);
            pitch.transform.localScale = new Vector3(19.0f, 0.04f, 28.0f);
            pitch.GetComponent<Renderer>().sharedMaterial = pitchMat;
            Object.Destroy(pitch.GetComponent<Collider>());

            // Çim Biçme Şeritleri (Alternating Striped Mower Pattern)
            for (int st = -6; st <= 6; st += 2)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Pitch_Mower_Stripe";
                stripe.transform.SetParent(pitch.transform, false);
                stripe.transform.localPosition = new Vector3(0f, 0.52f, st / 13.0f);
                stripe.transform.localScale = new Vector3(0.98f, 0.02f, 1.0f / 13.0f);
                stripe.GetComponent<Renderer>().sharedMaterial = stripeMat;
                Object.Destroy(stripe.GetComponent<Collider>());
            }

            // 4. BELİRGİN VE EKSİKSİZ SAHA ÇİZGİLERİ
            // 4.1 Dış Taç ve Aut Çizgileri
            GameObject boundaryL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundaryL.name = "Touchline_Left";
            boundaryL.transform.SetParent(pitch.transform, false);
            boundaryL.transform.localPosition = new Vector3(-0.47f, 0.55f, 0f);
            boundaryL.transform.localScale = new Vector3(0.015f, 0.02f, 0.94f);
            boundaryL.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
            Object.Destroy(boundaryL.GetComponent<Collider>());

            GameObject boundaryR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundaryR.name = "Touchline_Right";
            boundaryR.transform.SetParent(pitch.transform, false);
            boundaryR.transform.localPosition = new Vector3(0.47f, 0.55f, 0f);
            boundaryR.transform.localScale = new Vector3(0.015f, 0.02f, 0.94f);
            boundaryR.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
            Object.Destroy(boundaryR.GetComponent<Collider>());

            GameObject goalLineN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goalLineN.name = "GoalLine_North";
            goalLineN.transform.SetParent(pitch.transform, false);
            goalLineN.transform.localPosition = new Vector3(0f, 0.55f, 0.47f);
            goalLineN.transform.localScale = new Vector3(0.94f, 0.02f, 0.015f);
            goalLineN.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
            Object.Destroy(goalLineN.GetComponent<Collider>());

            GameObject goalLineS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goalLineS.name = "GoalLine_South";
            goalLineS.transform.SetParent(pitch.transform, false);
            goalLineS.transform.localPosition = new Vector3(0f, 0.55f, -0.47f);
            goalLineS.transform.localScale = new Vector3(0.94f, 0.02f, 0.015f);
            goalLineS.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
            Object.Destroy(goalLineS.GetComponent<Collider>());

            // 4.2 ORTA SAHA ÇİZGİSİ VE ÇEMBERİ (Center Circle & Spot)
            GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name = "Pitch_Halfway_Line";
            centerLine.transform.SetParent(pitch.transform, false);
            centerLine.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            centerLine.transform.localScale = new Vector3(0.94f, 0.02f, 0.015f);
            centerLine.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
            Object.Destroy(centerLine.GetComponent<Collider>());

            GameObject centerCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerCircle.name = "Pitch_Center_Circle";
            centerCircle.transform.SetParent(pitch.transform, false);
            centerCircle.transform.localPosition = new Vector3(0f, 0.56f, 0f);
            centerCircle.transform.localScale = new Vector3(0.26f, 0.02f, 0.18f);
            centerCircle.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
            Object.Destroy(centerCircle.GetComponent<Collider>());

            // Orta Çember İçi Çim Maskesi (Sadece halka görünmesi için)
            GameObject circleInner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            circleInner.name = "Center_Circle_Inner";
            circleInner.transform.SetParent(centerCircle.transform, false);
            circleInner.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            circleInner.transform.localScale = new Vector3(0.88f, 1.02f, 0.88f);
            circleInner.GetComponent<Renderer>().sharedMaterial = pitchMat;
            Object.Destroy(circleInner.GetComponent<Collider>());

            // Başlama Noktası
            GameObject kickoffSpot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            kickoffSpot.name = "Kickoff_Spot";
            kickoffSpot.transform.SetParent(pitch.transform, false);
            kickoffSpot.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            kickoffSpot.transform.localScale = new Vector3(0.035f, 0.03f, 0.025f);
            kickoffSpot.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
            Object.Destroy(kickoffSpot.GetComponent<Collider>());

            // 4.3 CEZA SAHASI VE PENALTI NOKTALARI (Kuzey ve Güney)
            for (int dirZ = -1; dirZ <= 1; dirZ += 2)
            {
                // Ceza Sahası Kutusu
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = $"Penalty_Box_{dirZ}";
                box.transform.SetParent(pitch.transform, false);
                box.transform.localPosition = new Vector3(0f, 0.55f, dirZ * 0.38f);
                box.transform.localScale = new Vector3(0.48f, 0.02f, 0.18f);
                box.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
                Object.Destroy(box.GetComponent<Collider>());

                GameObject boxInner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boxInner.name = $"Penalty_Box_Inner_{dirZ}";
                boxInner.transform.SetParent(box.transform, false);
                boxInner.transform.localPosition = new Vector3(0f, 0.20f, 0f);
                boxInner.transform.localScale = new Vector3(0.94f, 1.02f, 0.86f);
                boxInner.GetComponent<Renderer>().sharedMaterial = pitchMat;
                Object.Destroy(boxInner.GetComponent<Collider>());

                // Penaltı Noktası
                GameObject penSpot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                penSpot.name = $"Penalty_Spot_{dirZ}";
                penSpot.transform.SetParent(pitch.transform, false);
                penSpot.transform.localPosition = new Vector3(0f, 0.58f, dirZ * 0.34f);
                penSpot.transform.localScale = new Vector3(0.025f, 0.03f, 0.018f);
                penSpot.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
                Object.Destroy(penSpot.GetComponent<Collider>());
            }

            // 5. 2 ADET FUTBOL KALESİ DİREKLERİ
            for (int dirZ = -1; dirZ <= 1; dirZ += 2)
            {
                GameObject goalGroup = new GameObject($"Football_Goal_{dirZ}");
                goalGroup.transform.SetParent(stadiumObj.transform, false);
                goalGroup.transform.localPosition = new Vector3(1.5f, 0f, dirZ * 13.5f);

                // Sol ve Sağ Dik Direkler
                for (int dp = -1; dp <= 1; dp += 2)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = $"Goal_Post_{dp}";
                    post.transform.SetParent(goalGroup.transform, false);
                    post.transform.localPosition = new Vector3(dp * 2.05f, 1.2f, 0f);
                    post.transform.localScale = new Vector3(0.15f, 1.2f, 0.15f);
                    post.GetComponent<Renderer>().sharedMaterial = whiteLineMat;
                    Object.Destroy(post.GetComponent<Collider>());
                }
            }

            // 6. SEYİRCİ TRİBÜNLERİ & ÇATISI (Batı Tarafında: local X = -9.2f)
            GameObject standGroup = new GameObject("Stadium_Grandstand");
            standGroup.transform.SetParent(stadiumObj.transform, false);
            standGroup.transform.localPosition = new Vector3(-9.2f, 0f, 0f);

            // 3 Kademeli Beton Tribün
            for (int tier = 0; tier < 3; tier++)
            {
                GameObject tierStep = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tierStep.name = $"Stand_Tier_{tier + 1}";
                tierStep.transform.SetParent(standGroup.transform, false);
                tierStep.transform.localPosition = new Vector3(-tier * 0.90f, (tier * 0.45f) + 0.22f, 0f);
                tierStep.transform.localScale = new Vector3(1.1f, 0.45f, 24.0f);
                tierStep.GetComponent<Renderer>().sharedMaterial = standMat;
                Object.Destroy(tierStep.GetComponent<Collider>());

                // Koltuk Sıraları
                for (float sz = -10.5f; sz <= 10.5f; sz += 1.2f)
                {
                    GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    seat.name = "Spectator_Seat";
                    seat.transform.SetParent(tierStep.transform, false);
                    seat.transform.localPosition = new Vector3(0f, 0.55f, sz / 24.0f);
                    seat.transform.localScale = new Vector3(0.70f, 0.25f, 0.80f / 24.0f);
                    seat.GetComponent<Renderer>().sharedMaterial = seatBlueMat;
                    Object.Destroy(seat.GetComponent<Collider>());
                }
            }

            // Tribün Gölgelik Çatısı (Cantilever Roof)
            GameObject standRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            standRoof.name = "Stand_Protective_Roof";
            standRoof.transform.SetParent(standGroup.transform, false);
            standRoof.transform.localPosition = new Vector3(-0.6f, 3.4f, 0f);
            standRoof.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            standRoof.transform.localScale = new Vector3(3.8f, 0.15f, 25.0f);
            standRoof.GetComponent<Renderer>().sharedMaterial = roofCanopyMat;
            Object.Destroy(standRoof.GetComponent<Collider>());

            // Çatı Taşıyıcı Çelik Direkleri
            for (int p = -1; p <= 1; p++)
            {
                GameObject rPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rPole.name = $"Roof_Pillar_{p}";
                rPole.transform.SetParent(standGroup.transform, false);
                rPole.transform.localPosition = new Vector3(-2.2f, 1.8f, p * 10.0f);
                rPole.transform.localScale = new Vector3(0.15f, 1.8f, 0.15f);
                rPole.GetComponent<Renderer>().sharedMaterial = poleMat;
                Object.Destroy(rPole.GetComponent<Collider>());
            }

            // 7. 4 KÖŞEDE STADYUM AYDINLATMA PROJEKTÖR KULELERİ
            Vector3[] floodlightOffsets = new Vector3[]
            {
                new Vector3(-12.0f, 0f, 15.5f),
                new Vector3(12.0f, 0f, 15.5f),
                new Vector3(-12.0f, 0f, -15.5f),
                new Vector3(12.0f, 0f, -15.5f)
            };

            foreach (Vector3 fPos in floodlightOffsets)
            {
                BuildStadiumFloodlightTower(stadiumObj.transform, fPos, poleMat);
            }

        }

        private static void CreateFenceLine(Transform parent, Vector3 localPos, Vector3 scale, Material fenceMat)
        {
            GameObject fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fence.name = "Fence_Panel";
            fence.transform.SetParent(parent, false);
            fence.transform.localPosition = localPos;
            fence.transform.localScale = scale;
            fence.GetComponent<Renderer>().sharedMaterial = fenceMat;
            Object.Destroy(fence.GetComponent<Collider>());
        }

        private static void BuildStadiumFloodlightTower(Transform parent, Vector3 pos, Material poleMat)
        {
            GameObject tower = new GameObject("Stadium_Floodlight_Tower");
            tower.transform.SetParent(parent, false);
            tower.transform.localPosition = pos;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(tower.transform, false);
            pole.transform.localPosition = new Vector3(0f, 5.0f, 0f);
            pole.transform.localScale = new Vector3(0.35f, 5.0f, 0.35f);
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;
            Object.Destroy(pole.GetComponent<Collider>());

            GameObject lightHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lightHead.name = "Light_Head";
            lightHead.transform.SetParent(tower.transform, false);
            lightHead.transform.localPosition = new Vector3(0f, 10.2f, 0f);
            lightHead.transform.localScale = new Vector3(2.2f, 1.0f, 0.40f);
            lightHead.GetComponent<Renderer>().sharedMaterial = GetMaterial("FloodlightBulbMat", new Color(1.0f, 1.0f, 0.95f), 0.1f, 0.9f);
            Object.Destroy(lightHead.GetComponent<Collider>());

            GameObject pLightObj = new GameObject("Stadium_Light");
            pLightObj.transform.SetParent(tower.transform, false);
            pLightObj.transform.localPosition = new Vector3(0f, 9.8f, 0f);
            Light pLight = pLightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(0.95f, 0.98f, 1.0f);
            pLight.intensity = 3.2f;
            pLight.range = 24.0f;
            pLight.shadows = LightShadows.None;
            pLight.enabled = (DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight);

            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStreetLamp(lightHead, pLight);
            }
        }

        #endregion

        #endregion
    }
}
