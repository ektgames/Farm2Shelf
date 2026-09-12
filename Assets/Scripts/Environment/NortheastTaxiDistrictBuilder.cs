using UnityEngine;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Kuzey mahallenin doğuya simetrik uzantısı: aynı cadde aralığı (37.5m),
    /// Z=50 / Z=175 yatay yollar, kaldırım arası çitli parseller.
    /// Yaya geçidi kuralı: her zaman YATAY, kaldırım Z hizasında, dikey yolu karşılar.
    /// </summary>
    public static class NortheastTaxiDistrictBuilder
    {
        public const float ConnectZ = 50.0f;
        public const float TopZ = 175.0f;
        public const float AvenueA = 112.5f;
        public const float AvenueB = 150.0f;
        public const float CafeOppositeStreetZ = -128.0f;

        public static readonly Vector3[] DefaultParkingSlots = new Vector3[]
        {
            new Vector3(83.5f, 0.02f, 28.5f),
            new Vector3(88.5f, 0.02f, 28.5f),
            new Vector3(93.5f, 0.02f, 28.5f),
            new Vector3(98.5f, 0.02f, 28.5f),
            new Vector3(103.5f, 0.02f, 28.5f)
        };

        public static Vector3[] BuildPatrolWaypoints()
        {
            return new Vector3[]
            {
                new Vector3(93.5f, 0.02f, 38.0f),
                new Vector3(93.5f, 0.02f, ConnectZ),
                new Vector3(AvenueA, 0.02f, ConnectZ),
                new Vector3(AvenueA, 0.02f, TopZ),
                new Vector3(AvenueB, 0.02f, TopZ),
                new Vector3(AvenueB, 0.02f, ConnectZ),
                new Vector3(AvenueB, 0.02f, -10.5f),
                new Vector3(AvenueB, 0.02f, CafeOppositeStreetZ),
                new Vector3(AvenueA, 0.02f, CafeOppositeStreetZ),
                new Vector3(AvenueA, 0.02f, ConnectZ),
                new Vector3(93.5f, 0.02f, ConnectZ)
            };
        }

        public static void Build(Transform environmentRoot)
        {
            if (environmentRoot == null) return;

            Transform root = new GameObject("Northeast_Taxi_District").transform;
            root.SetParent(environmentRoot, false);

            Material roadMat = MakeMat("NE_RoadMat", new Color(0.18f, 0.20f, 0.22f));
            Material lineMat = MakeMat("NE_RoadLineMat", new Color(0.95f, 0.80f, 0.15f));
            Material walkMat = MakeMat("NE_SidewalkMat", new Color(0.70f, 0.72f, 0.75f));
            Material grassMat = MakeMat("NE_GrassMat", new Color(0.28f, 0.62f, 0.28f));
            Material zebraMat = MakeMat("NE_ZebraMat", new Color(0.96f, 0.96f, 0.98f));
            Material lotGrass = MakeMat("NE_EmptyLotGrass", new Color(0.30f, 0.64f, 0.30f));
            Material fenceMat = MakeMat("NE_SiteFence", new Color(0.72f, 0.74f, 0.76f));
            Material woodMat = MakeMat("NE_SiteWood", new Color(0.55f, 0.36f, 0.20f));
            Material asphaltLot = MakeMat("NE_LotAsphalt", new Color(0.16f, 0.17f, 0.19f));
            Material bayLine = MakeMat("NE_BayLine", new Color(0.96f, 0.82f, 0.12f));
            Material wallMat = MakeMat("NE_DepotWall", new Color(0.24f, 0.26f, 0.30f));
            Material yellowMat = MakeMat("NE_DepotYellow", new Color(0.94f, 0.76f, 0.10f));
            Material roofMat = MakeMat("NE_DepotRoof", new Color(0.18f, 0.19f, 0.22f));
            Material glassMat = MakeMat("NE_DepotGlass", new Color(0.25f, 0.45f, 0.58f));
            Material canopyMat = MakeMat("NE_Canopy", new Color(0.88f, 0.72f, 0.12f));
            Material trimMat = MakeMat("NE_Trim", new Color(0.12f, 0.13f, 0.15f));

            CreateBox(root, "NE_Grass_Pad", new Vector3(116.0f, -0.15f, 92.0f), new Vector3(80.0f, 0.10f, 174.0f), grassMat);

            float[] avenues = { AvenueA, AvenueB };
            float roadStartZ = 47.0f;
            float roadEndZ = 178.0f;
            float roadLenZ = roadEndZ - roadStartZ;
            float roadCenterZ = (roadStartZ + roadEndZ) * 0.5f;
            const float upperWalkSouth = 53.75f;
            const float upperWalkNorth = 171.25f;
            float swLenZ = upperWalkNorth - upperWalkSouth;
            float swCenterZ = (upperWalkSouth + upperWalkNorth) * 0.5f;

            foreach (float ax in avenues)
            {
                CreateRoadZ(root, $"NE_Ave_X_{ax:0}", ax, roadCenterZ, roadLenZ, roadMat, lineMat, roadStartZ, roadEndZ);
                CreateBox(root, $"NE_AveWalk_L_{ax:0}", new Vector3(ax - 3.75f, 0.05f, swCenterZ), new Vector3(1.5f, 0.20f, swLenZ), walkMat);
                CreateBox(root, $"NE_AveWalk_R_{ax:0}", new Vector3(ax + 3.75f, 0.05f, swCenterZ), new Vector3(1.5f, 0.20f, swLenZ), walkMat);

                for (float z = roadStartZ + 15.0f; z <= roadEndZ - 15.0f; z += 28.0f)
                {
                    BuildLamp(root, new Vector3(ax - 3.75f, 0.05f, z), true);
                    BuildLamp(root, new Vector3(ax + 3.75f, 0.05f, z + 14.0f), false);
                }

                PlaceHorizontalSidewalkCrosswalk(root, ax, 46.25f, 1.5f, zebraMat);
                PlaceHorizontalSidewalkCrosswalk(root, ax, 53.75f, 1.5f, zebraMat);
                PlaceHorizontalSidewalkCrosswalk(root, ax, 171.25f, 1.5f, zebraMat);
                PlaceHorizontalSidewalkCrosswalk(root, ax, -4.5f, 3.0f, zebraMat);
            }

            // Yatay yol son caddenin doğu kenarına (X=153) kadar asfalt — çıkmaz yok, kavşak tamam
            CreateRoadX(root, "NE_Connect_Z50", 115.5f, ConnectZ, 75.0f, roadMat, lineMat, true, avenues);
            CreateRoadX(root, "NE_Connect_Z175", 115.5f, TopZ, 75.0f, roadMat, lineMat, true, avenues);

            CreateWalkXWithGaps(root, 46.25f, 81.0f, 146.4f, walkMat, new float[] { 93.5f, AvenueA, AvenueB });
            CreateWalkXWithGaps(root, 53.75f, 81.0f, 146.4f, walkMat, avenues);
            CreateWalkXWithGaps(root, 171.25f, 81.0f, 146.4f, walkMat, avenues);
            CreateBox(root, "NE_TopOuterWalk_Full", new Vector3(117.0f, 0.05f, 178.75f), new Vector3(72.0f, 0.20f, 1.5f), walkMat);

            // Son cadde doğu kaldırımını Z=50 kavşağında aşağı mahalleye birleştir (çıkmaz yok)
            CreateBox(root, "NE_AveB_EastWalk_Z50Join", new Vector3(AvenueB + 3.75f, 0.05f, 50.0f), new Vector3(1.5f, 0.20f, 7.5f), walkMat);
            CreateBox(root, "NE_AveB_EastWalk_Z175Join", new Vector3(AvenueB + 3.75f, 0.05f, 175.0f), new Vector3(1.5f, 0.20f, 7.5f), walkMat);

            PlaceHorizontalSidewalkCrosswalk(root, 93.5f, 46.25f, 1.5f, zebraMat);

            BuildApartmentGrid(root);
            BuildLowerGreenNeighborhood(root, roadMat, lineMat, walkMat, lotGrass);
            BuildCafeOppositeSoutheastRoads(root, roadMat, lineMat, walkMat, zebraMat);
            BuildTaxiStand(root, asphaltLot, bayLine, wallMat, yellowMat, roofMat, glassMat, canopyMat, trimMat, fenceMat, woodMat);
        }

        private static void BuildApartmentGrid(Transform parent)
        {
            Transform aptRoot = new GameObject("NE_Apartment_Blocks").transform;
            aptRoot.SetParent(parent, false);

            float[] colX = { 93.75f, 131.25f };
            float[] rowZ = { 68.0f, 98.0f, 128.0f, 158.0f };
            Vector2 parcelSize = new Vector2(27.0f, 23.0f);
            int[,] floors =
            {
                { 4, 5, 3, 4 },
                { 5, 3, 5, 4 }
            };
            int[,] colors =
            {
                { 1, 4, 2, 0 },
                { 6, 3, 7, 5 }
            };

            int index = 200;
            for (int c = 0; c < colX.Length; c++)
            {
                bool faceEast = c == 0;
                for (int r = 0; r < rowZ.Length; r++)
                {
                    ProceduralApartmentModelBuilder.BuildApartmentParcel(
                        aptRoot,
                        new Vector3(colX[c], 0f, rowZ[r]),
                        parcelSize,
                        floors[c, r],
                        colors[c, r],
                        index++,
                        faceEast
                    );
                }
            }
        }

        private static void BuildLowerGreenNeighborhood(
            Transform parent,
            Material roadMat,
            Material lineMat,
            Material walkMat,
            Material lotGrass)
        {
            Transform lower = new GameObject("NE_Lower_Green_Neighborhood").transform;
            lower.SetParent(parent, false);

            float[] avenues = { AvenueA, AvenueB };
            const float highwayJoinZ = -6.0f;
            const float northZ = 47.0f;
            float lenZ = northZ - highwayJoinZ;
            float centerZ = (highwayJoinZ + northZ) * 0.5f;

            foreach (float ax in avenues)
            {
                CreateRoadZ(lower, $"NE_LowerAve_X_{ax:0}", ax, centerZ, lenZ, roadMat, lineMat, highwayJoinZ, northZ);
                CreateBox(lower, $"NE_LowerWalk_L_{ax:0}", new Vector3(ax - 3.75f, 0.05f, 21.625f), new Vector3(1.5f, 0.20f, 49.25f), walkMat);
                CreateBox(lower, $"NE_LowerWalk_R_{ax:0}", new Vector3(ax + 3.75f, 0.05f, 21.625f), new Vector3(1.5f, 0.20f, 49.25f), walkMat);
            }

            CreateRoadZ(lower, "NE_Drive_Stand", 93.5f, 39.0f, 16.0f, roadMat, lineMat, 31.0f, 47.0f);
            CreateBox(lower, "NE_OpenLand_East", new Vector3(131.25f, 0.02f, 20.5f), new Vector3(26.0f, 0.08f, 33.0f), lotGrass);
        }

        /// <summary>
        /// Kafelerin karşısı (X=75 doğusu, Z=-55..-128): üstten inen dikey caddeler + altta sağa yatay yol.
        /// Mevcut cami/kafe geometrisine dokunulmaz.
        /// </summary>
        private static void BuildCafeOppositeSoutheastRoads(
            Transform parent,
            Material roadMat,
            Material lineMat,
            Material walkMat,
            Material zebraMat)
        {
            Transform se = new GameObject("SE_Cafe_Opposite_Roads").transform;
            se.SetParent(parent, false);

            float[] avenues = { AvenueA, AvenueB };
            const float hwySouthZ = -15.0f;
            const float bottomZ = CafeOppositeStreetZ;
            float lenZ = hwySouthZ - bottomZ;
            float centerZ = (hwySouthZ + bottomZ) * 0.5f;

            foreach (float ax in avenues)
            {
                CreateRoadZ(se, $"SE_Ave_X_{ax:0}", ax, centerZ, lenZ, roadMat, lineMat, bottomZ, hwySouthZ);
                CreateBox(se, $"SE_AveWalk_L_{ax:0}", new Vector3(ax - 3.75f, 0.05f, -70.0f), new Vector3(1.5f, 0.20f, 108.0f), walkMat);
                CreateBox(se, $"SE_AveWalk_R_{ax:0}", new Vector3(ax + 3.75f, 0.05f, -70.0f), new Vector3(1.5f, 0.20f, 108.0f), walkMat);
                PlaceHorizontalSidewalkCrosswalk(se, ax, -124.0f, 2.0f, zebraMat);
                PlaceHorizontalSidewalkCrosswalk(se, ax, -13.5f, 3.0f, zebraMat);
            }

            CreateRoadX(se, "SE_Bottom_Street", 115.5f, bottomZ, 75.0f, roadMat, lineMat, true, avenues);

            CreateWalkXWithGaps(se, -124.0f, 81.0f, 153.0f, walkMat, avenues, 2.0f);
            CreateBox(se, "SE_BottomWalk_South", new Vector3(121.5f, 0.05f, -132.5f), new Vector3(81.0f, 0.20f, 3.0f), walkMat);

            // Sağ alt köşe: kaldırım doğuya uzanır ve yukarı birleşir
            CreateBox(se, "SE_SECorner_South", new Vector3(156.75f, 0.05f, -132.5f), new Vector3(7.5f, 0.20f, 3.0f), walkMat);
            CreateBox(se, "SE_SECorner_Up", new Vector3(153.75f, 0.05f, -128.0f), new Vector3(1.5f, 0.20f, 8.0f), walkMat);
        }

        private static void BuildTaxiStand(
            Transform parent,
            Material asphaltLot,
            Material bayLine,
            Material wallMat,
            Material yellowMat,
            Material roofMat,
            Material glassMat,
            Material canopyMat,
            Material trimMat,
            Material fenceMat,
            Material woodMat)
        {
            Transform stand = new GameObject("Taxi_Stand_Complex").transform;
            stand.SetParent(parent, false);

            CreateBox(stand, "Stand_Lot", new Vector3(93.5f, 0.01f, 26.5f), new Vector3(24.0f, 0.08f, 17.2f), asphaltLot);
            BuildTaxiStandFences(stand, fenceMat, woodMat);

            for (int i = 0; i < DefaultParkingSlots.Length; i++)
            {
                Transform bay = new GameObject($"Taxi_Parking_Slot_{i + 1}").transform;
                bay.SetParent(stand, false);
                bay.position = DefaultParkingSlots[i];
                CreateBox(bay, "Bay_L", new Vector3(-1.15f, 0.02f, 0f), new Vector3(0.08f, 0.03f, 4.4f), bayLine);
                CreateBox(bay, "Bay_R", new Vector3(1.15f, 0.02f, 0f), new Vector3(0.08f, 0.03f, 4.4f), bayLine);
                CreateBox(bay, "Bay_Back", new Vector3(0f, 0.02f, -2.15f), new Vector3(2.3f, 0.03f, 0.08f), bayLine);
            }

            GameObject building = new GameObject("Taxi_Stand_Building");
            building.transform.SetParent(stand, false);
            building.transform.position = new Vector3(93.5f, 0f, 16.2f);

            CreateBox(building.transform, "Office_Body", new Vector3(0f, 2.15f, 0f), new Vector3(18.0f, 4.3f, 7.2f), wallMat);
            CreateBox(building.transform, "Plinth", new Vector3(0f, 0.22f, 0f), new Vector3(18.4f, 0.44f, 7.5f), trimMat);
            CreateBox(building.transform, "Office_Stripe", new Vector3(0f, 2.85f, 3.64f), new Vector3(18.2f, 0.50f, 0.08f), yellowMat);
            CreateBox(building.transform, "Roof", new Vector3(0f, 4.40f, 0f), new Vector3(19.0f, 0.28f, 8.0f), roofMat);
            CreateBox(building.transform, "Canopy", new Vector3(0f, 3.40f, 4.6f), new Vector3(20.0f, 0.16f, 5.0f), canopyMat);
            CreateBox(building.transform, "Canopy_Post_L", new Vector3(-8.4f, 1.65f, 6.4f), new Vector3(0.26f, 3.3f, 0.26f), yellowMat);
            CreateBox(building.transform, "Canopy_Post_R", new Vector3(8.4f, 1.65f, 6.4f), new Vector3(0.26f, 3.3f, 0.26f), yellowMat);
            CreateBox(building.transform, "Door", new Vector3(0f, 1.20f, 3.64f), new Vector3(1.7f, 2.2f, 0.10f), trimMat);
            CreateBox(building.transform, "Window_L", new Vector3(-5.0f, 2.20f, 3.64f), new Vector3(3.0f, 1.45f, 0.08f), glassMat);
            CreateBox(building.transform, "Window_R", new Vector3(5.0f, 2.20f, 3.64f), new Vector3(3.0f, 1.45f, 0.08f), glassMat);
            CreateBox(building.transform, "Sign_Board", new Vector3(0f, 3.62f, 3.70f), new Vector3(7.2f, 0.80f, 0.12f), yellowMat);

            Collider[] childCols = building.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < childCols.Length; i++)
            {
                if (childCols[i] != null && childCols[i].gameObject != building)
                {
                    UnityEngine.Object.Destroy(childCols[i]);
                }
            }

            TaxiStandClickable click = building.AddComponent<TaxiStandClickable>();
            click.EnsureCollider();
        }

        private static void BuildTaxiStandFences(Transform stand, Material fenceMat, Material woodMat)
        {
            const float cx = 93.5f;
            const float cz = 26.5f;
            const float w = 23.6f;
            const float d = 16.6f;
            const float hx = w * 0.5f;
            const float hz = d * 0.5f;
            const float gate = 6.4f;
            const float wing = (w - gate) * 0.5f;

            CreateBox(stand, "StandFence_S", new Vector3(cx, 0.55f, cz - hz), new Vector3(w, 1.1f, 0.14f), fenceMat);
            CreateBox(stand, "StandFence_W", new Vector3(cx - hx, 0.55f, cz), new Vector3(0.14f, 1.1f, d), fenceMat);
            CreateBox(stand, "StandFence_E", new Vector3(cx + hx, 0.55f, cz), new Vector3(0.14f, 1.1f, d), fenceMat);
            CreateBox(stand, "StandFence_N_L", new Vector3(cx - (gate * 0.5f + wing * 0.5f), 0.55f, cz + hz), new Vector3(wing, 1.1f, 0.14f), fenceMat);
            CreateBox(stand, "StandFence_N_R", new Vector3(cx + (gate * 0.5f + wing * 0.5f), 0.55f, cz + hz), new Vector3(wing, 1.1f, 0.14f), fenceMat);
            CreateBox(stand, "StandGatePost_L", new Vector3(cx - gate * 0.5f, 0.75f, cz + hz), new Vector3(0.22f, 1.5f, 0.22f), woodMat);
            CreateBox(stand, "StandGatePost_R", new Vector3(cx + gate * 0.5f, 0.75f, cz + hz), new Vector3(0.22f, 1.5f, 0.22f), woodMat);
        }

        private static void CreateWalkXWithGaps(Transform parent, float z, float xStart, float xEnd, Material mat, float[] skipX, float walkDepth = 1.5f)
        {
            const float halfGap = 3.0f;
            float cursor = xStart;
            if (skipX != null)
            {
                for (int i = 0; i < skipX.Length; i++)
                {
                    float left = skipX[i] - halfGap;
                    float right = skipX[i] + halfGap;
                    if (left > cursor + 0.8f)
                    {
                        float len = left - cursor;
                        CreateBox(parent, "NE_WalkX", new Vector3((cursor + left) * 0.5f, 0.05f, z), new Vector3(len, 0.20f, walkDepth), mat);
                    }
                    cursor = Mathf.Max(cursor, right);
                }
            }

            if (xEnd > cursor + 0.8f)
            {
                float len = xEnd - cursor;
                CreateBox(parent, "NE_WalkX", new Vector3((cursor + xEnd) * 0.5f, 0.05f, z), new Vector3(len, 0.20f, walkDepth), mat);
            }
        }

        private static void BuildLamp(Transform parent, Vector3 pos, bool faceRight)
        {
            Material pole = MakeMat("NE_LampPole", new Color(0.18f, 0.20f, 0.24f));
            Material bulb = MakeMat("NE_LampBulb", new Color(0.35f, 0.35f, 0.38f));

            GameObject lamp = new GameObject("NE_StreetLamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = pos;

            CreateBox(lamp.transform, "Pole", new Vector3(0f, 1.7f, 0f), new Vector3(0.10f, 3.4f, 0.10f), pole);
            float dir = faceRight ? 1f : -1f;
            CreateBox(lamp.transform, "Arm", new Vector3(dir * 0.25f, 3.35f, 0f), new Vector3(0.6f, 0.08f, 0.10f), pole);

            GameObject bulbGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulbGo.name = "Bulb";
            bulbGo.transform.SetParent(lamp.transform, false);
            bulbGo.transform.localPosition = new Vector3(dir * 0.50f, 3.25f, 0f);
            bulbGo.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            bulbGo.GetComponent<Renderer>().sharedMaterial = bulb;
            UnityEngine.Object.Destroy(bulbGo.GetComponent<Collider>());

            GameObject lightChild = new GameObject("StreetLamp_Light");
            lightChild.transform.SetParent(lamp.transform, false);
            lightChild.transform.localPosition = new Vector3(dir * 0.50f, 3.1f, 0f);
            Light point = lightChild.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(1.0f, 0.88f, 0.55f);
            point.intensity = 2.5f;
            point.range = 14.0f;
            point.shadows = LightShadows.None;
            point.enabled = false;
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterStreetLamp(bulbGo, point);
            }
        }

        /// <summary>
        /// Yaya geçidi daima yataydır: kaldırımın Z hizasında, dikey asfaltı karşıdan karşıya bağlar.
        /// </summary>
        private static void PlaceHorizontalSidewalkCrosswalk(
            Transform parent,
            float avenueX,
            float sidewalkZ,
            float sidewalkWidth,
            Material mat)
        {
            // Sadece asfalt genişliği (6m); kaldırımın üstüne binmez
            for (float o = -2.70f; o <= 2.70f; o += 0.54f)
            {
                CreateBox(parent, "NE_SidewalkZebra", new Vector3(avenueX + o, 0.025f, sidewalkZ), new Vector3(0.32f, 0.02f, sidewalkWidth), mat);
            }
        }

        private static void CreateRoadX(Transform parent, string name, float x, float z, float length, Material road, Material line, bool dashed, float[] skipX)
        {
            CreateBox(parent, name, new Vector3(x, -0.05f, z), new Vector3(length, 0.10f, 6.0f), road);
            if (!dashed) return;
            float start = x - length * 0.5f + 3f;
            float end = x + length * 0.5f - 3f;
            for (float lx = start; lx <= end; lx += 3.0f)
            {
                bool skip = false;
                if (skipX != null)
                {
                    for (int i = 0; i < skipX.Length; i++)
                    {
                        if (Mathf.Abs(lx - skipX[i]) < 3.2f) { skip = true; break; }
                    }
                }
                if (skip) continue;
                CreateBox(parent, name + "_Line", new Vector3(lx, 0.01f, z), new Vector3(1.8f, 0.02f, 0.25f), line);
            }
        }

        private static void CreateRoadZ(Transform parent, string name, float x, float z, float length, Material road, Material line, float lineStartZ, float lineEndZ)
        {
            CreateBox(parent, name, new Vector3(x, -0.05f, z), new Vector3(6.0f, 0.10f, length), road);
            for (float lz = lineStartZ + 4.0f; lz <= lineEndZ - 4.0f; lz += 3.0f)
            {
                if (Mathf.Abs(lz - ConnectZ) < 3.2f || Mathf.Abs(lz - TopZ) < 3.2f || Mathf.Abs(lz - CafeOppositeStreetZ) < 3.2f || Mathf.Abs(lz + 9.0f) < 3.2f) continue;
                CreateBox(parent, name + "_Line", new Vector3(x, 0.01f, lz), new Vector3(0.25f, 0.02f, 1.8f), line);
            }
        }

        private static void CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Material MakeMat(string name, Color color)
        {
            Shader s = ShaderHelper.GetLitShader();
            if (s == null) s = Shader.Find("Standard");
            Material m = new Material(s) { name = name };
            ShaderHelper.BindOpaqueColorMaps(m, color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.18f);
            return m;
        }
    }
}
