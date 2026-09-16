using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Taksi = CityTraffic ile aynı şeritler. Yalnızca eksen hizalı yol parçaları;
    /// nehir yalnızca otoyol köprüsünden; çimen / dükkan kestirmesi yok.
    /// </summary>
    public static class TaxiRoadRouter
    {
        private const float Y = 0.05f;
        public const float DriveX = 93.5f;

        private const float HwyE = -10.5f;
        private const float HwyW = -7.5f;
        private const float NorthE = 48.5f;
        private const float NorthW = 51.5f;
        private const float SouthE = -56.5f;
        private const float SouthW = -53.5f;
        private const float OuterE = -129.5f;
        private const float OuterW = -126.5f;
        private const float TopE = 173.5f;
        private const float TopW = 176.5f;

        private const float RiverMinX = -106.0f;
        private const float RiverMaxX = -84.0f;
        private const float Step = 4.0f;
        private const int GraphGen = 8;

        private static readonly List<Vector2> nodes = new List<Vector2>(128);
        private static readonly List<List<int>> edges = new List<List<int>>(128);
        private static readonly List<RoadSeg> segs = new List<RoadSeg>(64);
        private static int builtGen = -1;

        private struct RoadSeg
        {
            public float x0, z0, x1, z1;
        }

        public static List<Vector3> BuildRoute(Vector3 from, Vector3 to)
        {
            EnsureGraph();
            List<Vector3> pts = new List<Vector3>();

            if (InStandLot(from))
            {
                Add(pts, Flat(from));
                Add(pts, Flat(new Vector3(DriveX, Y, from.z)));
                Add(pts, Flat(new Vector3(DriveX, Y, 38f)));
                from = new Vector3(DriveX, Y, 50f);
            }

            if (InStandLot(to))
            {
                AppendLanePath(pts, SnapToLane(from), Flat(new Vector3(DriveX, Y, 50f)));
                Add(pts, Flat(new Vector3(DriveX, Y, 38f)));
                Add(pts, Flat(new Vector3(to.x, Y, 38f)));
                Add(pts, Flat(to));
                return DensifyOnLanes(pts);
            }

            Vector3 a = SnapToLane(from);
            Vector3 b = SnapToLane(to);
            AppendLanePath(pts, a, b);
            return DensifyOnLanes(pts);
        }

        public static void GetPickupSpots(Vector3 doorstep, out Vector3 sidewalkWait, out Vector3 taxiStop)
        {
            EnsureGraph();
            if (InStandLot(doorstep))
            {
                sidewalkWait = new Vector3(90.4f, 0.02f, 20.6f);
                taxiStop = new Vector3(DriveX, Y, 32.0f);
                return;
            }

            taxiStop = SnapToLane(doorstep);
            Vector3 toward = Flat(doorstep) - taxiStop;
            toward.y = 0f;
            if (toward.sqrMagnitude < 0.36f) toward = new Vector3(0f, 0f, 1f);
            sidewalkWait = taxiStop + toward.normalized * 4.8f;
            sidewalkWait.y = 0.02f;
            taxiStop.y = Y;
            if (IsIllegalRiverPoint(sidewalkWait)) sidewalkWait = taxiStop;
        }

        public static List<Vector3> BuildReturnToStand(Vector3 from, Vector3 standSlot)
        {
            return BuildRoute(from, standSlot);
        }

        public static bool IsIllegalRiverPoint(Vector3 p)
        {
            return InRiverBand(p) && !OnHighwayBridge(p);
        }

        public static Vector3 SnapToNearestLane(Vector3 p)
        {
            EnsureGraph();
            return SnapToLane(p);
        }

        public static bool IsOnRoad(Vector3 p, float maxDist)
        {
            EnsureGraph();
            if (IsCampusInterior(p)) return false;
            Vector3 s = SnapToLane(p);
            s.y = p.y;
            Vector3 d = p - s;
            d.y = 0f;
            return d.magnitude <= maxDist;
        }

        public static bool IsCampusInterior(Vector3 p)
        {
            if (InStandLot(p)) return false;
            if (p.x >= 11.0f && p.x <= 15.5f) return false;
            if (p.x >= -78.0f && p.x <= -72.0f) return false;
            if (p.x >= 72.0f && p.x <= 78.0f) return false;
            if (Mathf.Abs(p.z - HwyE) <= 3.2f || Mathf.Abs(p.z - HwyW) <= 3.2f) return false;
            if (Mathf.Abs(p.z - NorthE) <= 3.2f || Mathf.Abs(p.z - NorthW) <= 3.2f) return false;
            bool store = p.x >= -14.0f && p.x <= 11.0f && p.z >= -3.2f && p.z <= 38.0f;
            bool parking = p.x >= -41.0f && p.x <= -14.0f && p.z >= -3.2f && p.z <= 40.0f;
            bool workshop = p.x >= -68.0f && p.x <= -42.0f && p.z >= -3.2f && p.z <= 40.0f;
            bool farm = p.x >= 16.0f && p.x <= 52.0f && p.z >= -2.0f && p.z <= 36.0f;
            return store || parking || workshop || farm;
        }

        public static float ClosestNorthAve(float x)
        {
            if (x >= 86.0f) return x <= 135.0f ? 112.5f : 150.0f;
            float[] aves = { -75.0f, -37.5f, 0.0f, 37.5f, 75.0f, 112.5f, 150.0f };
            float best = aves[0];
            float min = Mathf.Abs(x - best);
            for (int i = 1; i < aves.Length; i++)
            {
                float d = Mathf.Abs(x - aves[i]);
                if (d < min)
                {
                    min = d;
                    best = aves[i];
                }
            }

            return best;
        }

        private static void EnsureGraph()
        {
            if (builtGen == GraphGen && nodes.Count > 0) return;
            builtGen = GraphGen;
            nodes.Clear();
            edges.Clear();
            segs.Clear();
            BuildSegments();
            CollectNodes();
            ConnectAlongSegments();
        }

        private static void BuildSegments()
        {
            AddEW(-340f, 180f, -9f);
            AddEW(-230f, -112f, 50f);
            AddEW(-75f, 160f, 50f);
            AddEW(-230f, -112f, 175f);
            AddEW(-37.5f, 160f, 175f);
            AddEW(-230f, -112f, -55f);
            AddEW(-75f, 160f, -55f);
            AddEW(-230f, -112f, -128f);
            AddEW(-75f, 160f, -128f);

            AddNS(-226f, -128f, 175f);
            AddNS(-188f, -128f, 175f);
            AddNS(-150f, -128f, 175f);
            AddNS(-112f, -128f, 175f);
            AddNS(-75f, -128f, 50f);
            AddNS(-37.5f, 50f, 175f);
            AddNS(0f, -128f, -9f);
            AddNS(0f, 50f, 175f);
            AddNS(13f, -9f, 50f);
            AddNS(37.5f, 50f, 175f);
            AddNS(75f, -128f, 50f);
            AddNS(DriveX, 18f, 50f);
            AddNS(112.5f, -128f, 175f);
            AddNS(150f, -128f, 175f);
        }

        private static void AddEW(float x0, float x1, float z)
        {
            segs.Add(new RoadSeg { x0 = x0, z0 = z, x1 = x1, z1 = z });
        }

        private static void AddNS(float x, float z0, float z1)
        {
            segs.Add(new RoadSeg { x0 = x, z0 = z0, x1 = x, z1 = z1 });
        }

        private static void CollectNodes()
        {
            for (int i = 0; i < segs.Count; i++)
            {
                RoadSeg s = segs[i];
                AddNode(s.x0, s.z0);
                AddNode(s.x1, s.z1);
            }

            for (int i = 0; i < segs.Count; i++)
            {
                for (int j = i + 1; j < segs.Count; j++)
                {
                    if (!TryIntersect(segs[i], segs[j], out float x, out float z)) continue;
                    AddNode(x, z);
                }
            }
        }

        private static bool TryIntersect(RoadSeg a, RoadSeg b, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            bool aEw = Mathf.Abs(a.z0 - a.z1) < 0.01f;
            bool bEw = Mathf.Abs(b.z0 - b.z1) < 0.01f;
            if (aEw == bEw) return false;
            if (aEw)
            {
                x = b.x0;
                z = a.z0;
                return OnSeg(a, x, z) && OnSeg(b, x, z);
            }

            x = a.x0;
            z = b.z0;
            return OnSeg(a, x, z) && OnSeg(b, x, z);
        }

        private static bool OnSeg(RoadSeg s, float x, float z)
        {
            float minX = Mathf.Min(s.x0, s.x1) - 0.05f;
            float maxX = Mathf.Max(s.x0, s.x1) + 0.05f;
            float minZ = Mathf.Min(s.z0, s.z1) - 0.05f;
            float maxZ = Mathf.Max(s.z0, s.z1) + 0.05f;
            return x >= minX && x <= maxX && z >= minZ && z <= maxZ
                && (Mathf.Abs(s.z0 - s.z1) < 0.01f ? Mathf.Abs(z - s.z0) < 0.2f : Mathf.Abs(x - s.x0) < 0.2f);
        }

        private static void ConnectAlongSegments()
        {
            for (int s = 0; s < segs.Count; s++)
            {
                List<int> on = new List<int>();
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (OnSeg(segs[s], nodes[i].x, nodes[i].y)) on.Add(i);
                }

                on.Sort((p, q) =>
                {
                    if (Mathf.Abs(segs[s].z0 - segs[s].z1) < 0.01f)
                        return nodes[p].x.CompareTo(nodes[q].x);
                    return nodes[p].y.CompareTo(nodes[q].y);
                });

                for (int i = 0; i < on.Count - 1; i++)
                {
                    int a = on[i];
                    int b = on[i + 1];
                    if (a == b) continue;
                    if ((nodes[a] - nodes[b]).sqrMagnitude < 0.04f) continue;
                    Vector3 mid = new Vector3(
                        (nodes[a].x + nodes[b].x) * 0.5f,
                        Y,
                        (nodes[a].y + nodes[b].y) * 0.5f);
                    if (IsCampusInterior(mid)) continue;
                    if (!edges[a].Contains(b)) edges[a].Add(b);
                    if (!edges[b].Contains(a)) edges[b].Add(a);
                }
            }
        }

        private static void AppendLanePath(List<Vector3> pts, Vector3 fromLane, Vector3 toLane)
        {
            Add(pts, fromLane);
            if (SameLane(fromLane, toLane))
            {
                AppendAlongLane(pts, fromLane, toLane);
                Add(pts, toLane);
                return;
            }

            int start = NearestNodeOnSameRoads(fromLane);
            int goal = NearestNodeOnSameRoads(toLane);
            List<int> chain = FindPath(start, goal);
            if (chain.Count < 2)
            {
                chain = ForcedDetour(fromLane, toLane);
            }

            for (int i = 0; i < chain.Count - 1; i++)
            {
                Vector2 na = nodes[chain[i]];
                Vector2 nb = nodes[chain[i + 1]];
                if (!ShareAxis(na, nb)) continue;
                Vector3 a = LanePoint(na, nb, true);
                Vector3 b = LanePoint(na, nb, false);
                Add(pts, a);
                AppendAlongLane(pts, a, b);
                Add(pts, b);
            }

            if (pts.Count == 0 || SameLane(pts[pts.Count - 1], toLane))
            {
                if (pts.Count > 0) AppendAlongLane(pts, pts[pts.Count - 1], toLane);
                Add(pts, toLane);
            }
            else
            {
                Vector3 last = pts[pts.Count - 1];
                Vector3 corner = PickRoadCorner(last, toLane);
                if (SameLane(last, corner))
                {
                    AppendAlongLane(pts, last, corner);
                    Add(pts, corner);
                    last = corner;
                }

                if (SameLane(last, toLane))
                {
                    AppendAlongLane(pts, last, toLane);
                    Add(pts, toLane);
                }
                else
                {
                    Add(pts, SnapToLane(toLane));
                }
            }
        }

        private static bool ShareAxis(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) < 0.2f || Mathf.Abs(a.y - b.y) < 0.2f;
        }

        private static List<int> ForcedDetour(Vector3 from, Vector3 to)
        {
            int nFrom = NearestNode(from);
            int nTo = NearestNode(to);
            bool fromWest = from.x < -95f;
            bool toWest = to.x < -95f;
            int westHwy = FindNode(-112f, -9f);
            int eastHwy = FindNode(-75f, -9f);
            List<int> all = new List<int>();
            if (fromWest != toWest && westHwy >= 0 && eastHwy >= 0)
            {
                if (fromWest)
                {
                    ConcatPath(all, FindPath(nFrom, westHwy));
                    ConcatPath(all, FindPath(westHwy, eastHwy));
                    ConcatPath(all, FindPath(eastHwy, nTo));
                }
                else
                {
                    ConcatPath(all, FindPath(nFrom, eastHwy));
                    ConcatPath(all, FindPath(eastHwy, westHwy));
                    ConcatPath(all, FindPath(westHwy, nTo));
                }

                return all;
            }

            int mid = FindNode(75f, -9f);
            if (mid < 0) mid = FindNode(0f, -9f);
            if (mid >= 0)
            {
                ConcatPath(all, FindPath(nFrom, mid));
                ConcatPath(all, FindPath(mid, nTo));
                return all;
            }

            ConcatPath(all, FindPath(nFrom, nTo));
            return all;
        }

        private static void ConcatPath(List<int> dst, List<int> src)
        {
            if (src == null || src.Count == 0) return;
            int start = 0;
            if (dst.Count > 0 && dst[dst.Count - 1] == src[0]) start = 1;
            for (int i = start; i < src.Count; i++) dst.Add(src[i]);
        }

        private static bool SameLane(Vector3 a, Vector3 b)
        {
            return (Mathf.Abs(a.x - b.x) < 2.2f && ShareNsRoad(a.x, b.x))
                || (Mathf.Abs(a.z - b.z) < 2.2f && ShareEwRoad(a.z, b.z));
        }

        private static bool ShareEwRoad(float z0, float z1)
        {
            return RoadGroupZ(z0) > -900f && Mathf.Abs(RoadGroupZ(z0) - RoadGroupZ(z1)) < 0.2f;
        }

        private static bool ShareNsRoad(float x0, float x1)
        {
            return RoadGroupX(x0) > -900f && Mathf.Abs(RoadGroupX(x0) - RoadGroupX(x1)) < 0.2f;
        }

        private static float RoadGroupZ(float z)
        {
            if (Mathf.Abs(z - HwyE) < 2.2f || Mathf.Abs(z - HwyW) < 2.2f || Mathf.Abs(z - (-9f)) < 2.2f) return -9f;
            if (Mathf.Abs(z - NorthE) < 2.2f || Mathf.Abs(z - NorthW) < 2.2f || Mathf.Abs(z - 50f) < 2.2f) return 50f;
            if (Mathf.Abs(z - SouthE) < 2.2f || Mathf.Abs(z - SouthW) < 2.2f || Mathf.Abs(z - (-55f)) < 2.2f) return -55f;
            if (Mathf.Abs(z - OuterE) < 2.2f || Mathf.Abs(z - OuterW) < 2.2f || Mathf.Abs(z - (-128f)) < 2.2f) return -128f;
            if (Mathf.Abs(z - TopE) < 2.2f || Mathf.Abs(z - TopW) < 2.2f || Mathf.Abs(z - 175f) < 2.2f) return 175f;
            if (Mathf.Abs(z - 38f) < 2.2f) return 38f;
            return -999f;
        }

        private static float RoadGroupX(float x)
        {
            float[] cx = { -226f, -188f, -150f, -112f, -75f, -37.5f, 0f, 13f, 37.5f, 75f, DriveX, 112.5f, 150f };
            float best = -999f;
            float min = 2.4f;
            for (int i = 0; i < cx.Length; i++)
            {
                float d = Mathf.Abs(x - cx[i]);
                if (d < min)
                {
                    min = d;
                    best = cx[i];
                }
            }

            return best;
        }

        private static Vector3 LanePoint(Vector2 a, Vector2 b, bool start)
        {
            Vector2 p = start ? a : b;
            if (Mathf.Abs(a.y - b.y) < 0.2f)
            {
                float zLane = b.x > a.x ? EastboundZ(a.y) : WestboundZ(a.y);
                return new Vector3(p.x, Y, zLane);
            }

            float xLane = b.y > a.y ? NorthboundX(a.x) : SouthboundX(a.x);
            return new Vector3(xLane, Y, p.y);
        }

        private static float EastboundZ(float roadZ)
        {
            if (Approximately(roadZ, -9f)) return HwyE;
            if (Approximately(roadZ, -55f)) return SouthE;
            if (Approximately(roadZ, -128f)) return OuterE;
            if (Approximately(roadZ, 50f)) return NorthE;
            if (Approximately(roadZ, 175f)) return TopE;
            return roadZ;
        }

        private static float WestboundZ(float roadZ)
        {
            if (Approximately(roadZ, -9f)) return HwyW;
            if (Approximately(roadZ, -55f)) return SouthW;
            if (Approximately(roadZ, -128f)) return OuterW;
            if (Approximately(roadZ, 50f)) return NorthW;
            if (Approximately(roadZ, 175f)) return TopW;
            return roadZ;
        }

        private static float NorthboundX(float roadX)
        {
            if (Approximately(roadX, DriveX)) return DriveX;
            if (Approximately(roadX, -226f)) return -224.5f;
            if (Approximately(roadX, -188f)) return -186.5f;
            if (Approximately(roadX, -150f)) return -148.5f;
            if (Approximately(roadX, -112f)) return -110.5f;
            if (Approximately(roadX, -75f)) return -73.5f;
            if (Approximately(roadX, -37.5f)) return -36.0f;
            if (Approximately(roadX, 0f)) return 1.5f;
            if (Approximately(roadX, 13f)) return 13.5f;
            if (Approximately(roadX, 37.5f)) return 39.0f;
            if (Approximately(roadX, 75f)) return 76.5f;
            if (Approximately(roadX, 112.5f)) return 114.0f;
            if (Approximately(roadX, 150f)) return 151.5f;
            return roadX + 1.5f;
        }

        private static float SouthboundX(float roadX)
        {
            if (Approximately(roadX, DriveX)) return DriveX;
            if (Approximately(roadX, -226f)) return -227.5f;
            if (Approximately(roadX, -188f)) return -189.5f;
            if (Approximately(roadX, -150f)) return -151.5f;
            if (Approximately(roadX, -112f)) return -113.5f;
            if (Approximately(roadX, -75f)) return -76.5f;
            if (Approximately(roadX, -37.5f)) return -39.0f;
            if (Approximately(roadX, 0f)) return -1.5f;
            if (Approximately(roadX, 13f)) return 12.5f;
            if (Approximately(roadX, 37.5f)) return 36.0f;
            if (Approximately(roadX, 75f)) return 73.5f;
            if (Approximately(roadX, 112.5f)) return 111.0f;
            if (Approximately(roadX, 150f)) return 148.5f;
            return roadX - 1.5f;
        }

        private static void AppendAlongLane(List<Vector3> pts, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            d.y = 0f;
            float len = d.magnitude;
            if (len < Step) return;
            int n = Mathf.FloorToInt(len / Step);
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)(n + 1);
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y = Y;
                if (OnHighwayBridge(p))
                    p.y = CityTrafficManager.GetBridgeElevation(p.x, p.z, out _);
                Add(pts, p);
            }
        }

        private static List<Vector3> DensifyOnLanes(List<Vector3> src)
        {
            List<Vector3> outPts = new List<Vector3>();
            if (src == null || src.Count == 0) return outPts;
            Add(outPts, SnapIfOffRoad(src[0]));
            for (int i = 1; i < src.Count; i++)
            {
                Vector3 a = outPts[outPts.Count - 1];
                Vector3 b = SnapIfOffRoad(src[i]);
                if (IsIllegalRiverPoint(b)) continue;
                if (SegmentLeavesRoad(a, b))
                {
                    Vector3 corner = PickRoadCorner(a, b);
                    if (!SegmentLeavesRoad(a, corner))
                    {
                        AppendAlongLane(outPts, a, corner);
                        Add(outPts, corner);
                        a = corner;
                    }

                    if (SegmentLeavesRoad(a, b))
                    {
                        AppendLanePath(outPts, a, b);
                        continue;
                    }
                }

                AppendAlongLane(outPts, a, b);
                Add(outPts, b);
            }

            return Compact(outPts);
        }

        private static Vector3 SnapIfOffRoad(Vector3 p)
        {
            if (InStandLot(p)) return Flat(p);
            if (IsIllegalRiverPoint(p)) return SnapToLane(p);
            Vector3 s = SnapToLane(p);
            Vector3 d = p - s;
            d.y = 0f;
            return d.magnitude > 6f ? s : Flat(p);
        }

        private static bool SegmentLeavesRoad(Vector3 a, Vector3 b)
        {
            if (IsCampusInterior(a) || IsCampusInterior(b)) return true;
            if (IsIllegalRiverPoint(a) || IsIllegalRiverPoint(b)) return true;
            if (CrossesRiverOffBridge(a, b)) return true;
            if (SameLane(a, b)) return false;
            float dx = Mathf.Abs(a.x - b.x);
            float dz = Mathf.Abs(a.z - b.z);
            if (dx > 2.5f && dz > 2.5f) return true;
            Vector3 mid = (a + b) * 0.5f;
            return !IsOnRoad(mid, 5.5f);
        }

        private static Vector3 PickRoadCorner(Vector3 a, Vector3 b)
        {
            Vector3 xz = new Vector3(a.x, Y, b.z);
            Vector3 zx = new Vector3(b.x, Y, a.z);
            bool xzOk = IsOnRoad(xz, 5.5f) && !CrossesRiverOffBridge(a, xz) && !CrossesRiverOffBridge(xz, b);
            bool zxOk = IsOnRoad(zx, 5.5f) && !CrossesRiverOffBridge(a, zx) && !CrossesRiverOffBridge(zx, b);
            if (xzOk && !zxOk) return xz;
            if (zxOk && !xzOk) return zx;
            if (xzOk && zxOk)
            {
                float dxz = DistToLane(xz);
                float dzx = DistToLane(zx);
                return dxz <= dzx ? xz : zx;
            }

            return SnapToLane(a);
        }

        private static float DistToLane(Vector3 p)
        {
            Vector3 s = SnapToLane(p);
            Vector3 d = p - s;
            d.y = 0f;
            return d.magnitude;
        }

        private static bool CrossesRiverOffBridge(Vector3 a, Vector3 b)
        {
            bool hwy = OnHighwayBridge(a) && OnHighwayBridge(b)
                || (Mathf.Abs(RoadGroupZ(a.z) - (-9f)) < 0.2f && Mathf.Abs(RoadGroupZ(b.z) - (-9f)) < 0.2f);
            if (hwy) return false;
            float minX = Mathf.Min(a.x, b.x);
            float maxX = Mathf.Max(a.x, b.x);
            return minX < RiverMaxX && maxX > RiverMinX;
        }

        private static bool InRiverBand(Vector3 p)
        {
            return p.x >= RiverMinX && p.x <= RiverMaxX && p.z >= -135f && p.z <= 190f;
        }

        private static bool OnHighwayBridge(Vector3 p)
        {
            return InRiverBand(p) && Mathf.Abs(p.z - (-9f)) <= 8f;
        }

        private static Vector3 SnapToLane(Vector3 p)
        {
            EnsureGraph();
            Vector3 best = Flat(p);
            float bestD = float.MaxValue;
            for (int i = 0; i < segs.Count; i++)
            {
                Vector3 q = ProjectOnSeg(p, segs[i]);
                if (IsIllegalRiverPoint(q) || IsCampusInterior(q)) continue;
                float d = Vector3.Distance(Flat(p), q);
                if (d < bestD)
                {
                    bestD = d;
                    best = q;
                }
            }

            if (bestD > 1e8f && nodes.Count > 0)
            {
                int n = NearestNode(p);
                best = Flat(new Vector3(nodes[n].x, Y, nodes[n].y));
            }

            return ApplyLaneOffset(best);
        }

        private static Vector3 ApplyLaneOffset(Vector3 p)
        {
            float gz = RoadGroupZ(p.z);
            if (gz > -900f)
            {
                p.z = EastboundZ(gz);
                p.y = Y;
                return p;
            }

            float gx = RoadGroupX(p.x);
            if (gx > -900f)
            {
                p.x = NorthboundX(gx);
                p.y = Y;
            }

            return p;
        }

        private static Vector3 ProjectOnSeg(Vector3 p, RoadSeg s)
        {
            Vector3 a = new Vector3(s.x0, Y, s.z0);
            Vector3 b = new Vector3(s.x1, Y, s.z1);
            Vector3 ab = b - a;
            ab.y = 0f;
            float denom = ab.sqrMagnitude;
            if (denom < 0.0001f) return a;
            float t = Mathf.Clamp01(Vector3.Dot(Flat(p) - a, ab) / denom);
            return a + ab * t;
        }

        private static List<int> FindPath(int start, int goal)
        {
            List<int> chain = new List<int>();
            if (start < 0 || goal < 0 || start >= nodes.Count || goal >= nodes.Count)
            {
                if (start >= 0) chain.Add(start);
                return chain;
            }

            if (start == goal)
            {
                chain.Add(start);
                return chain;
            }

            int n = nodes.Count;
            float[] dist = new float[n];
            int[] prev = new int[n];
            bool[] used = new bool[n];
            for (int i = 0; i < n; i++)
            {
                dist[i] = float.MaxValue;
                prev[i] = -1;
            }

            dist[start] = 0f;
            for (int step = 0; step < n; step++)
            {
                int u = -1;
                float best = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (!used[i] && dist[i] < best)
                    {
                        best = dist[i];
                        u = i;
                    }
                }

                if (u < 0) break;
                if (u == goal) break;
                used[u] = true;
                List<int> next = edges[u];
                for (int k = 0; k < next.Count; k++)
                {
                    int v = next[k];
                    float nd = dist[u] + Vector2.Distance(nodes[u], nodes[v]);
                    if (nd < dist[v])
                    {
                        dist[v] = nd;
                        prev[v] = u;
                    }
                }
            }

            if (dist[goal] >= float.MaxValue * 0.5f)
            {
                chain.Add(start);
                return chain;
            }

            for (int cur = goal; cur >= 0; cur = prev[cur])
            {
                chain.Add(cur);
                if (cur == start) break;
            }

            chain.Reverse();
            return chain;
        }

        private static int NearestNode(Vector3 p)
        {
            int best = 0;
            float bestD = float.MaxValue;
            Vector2 q = new Vector2(p.x, p.z);
            for (int i = 0; i < nodes.Count; i++)
            {
                float d = (nodes[i] - q).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            return best;
        }

        private static int NearestNodeOnSameRoads(Vector3 p)
        {
            int best = NearestNode(p);
            float bestD = float.MaxValue;
            float gx = RoadGroupX(p.x);
            float gz = RoadGroupZ(p.z);
            Vector2 q = new Vector2(p.x, p.z);
            for (int i = 0; i < nodes.Count; i++)
            {
                bool same = (gx > -900f && Mathf.Abs(nodes[i].x - gx) < 0.2f)
                    || (gz > -900f && Mathf.Abs(nodes[i].y - gz) < 0.2f);
                if (!same) continue;
                float d = (nodes[i] - q).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            return best;
        }

        private static int FindNode(float x, float z)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (Approximately(nodes[i].x, x) && Approximately(nodes[i].y, z))
                    return i;
            }

            return -1;
        }

        private static void AddNode(float x, float z)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (Approximately(nodes[i].x, x) && Approximately(nodes[i].y, z))
                    return;
            }

            nodes.Add(new Vector2(x, z));
            edges.Add(new List<int>(8));
        }

        private static bool InStandLot(Vector3 p)
        {
            return p.x >= 80.0f && p.x <= 110.0f && p.z >= 18.0f && p.z <= 46.0f;
        }

        private static Vector3 Flat(Vector3 p)
        {
            return new Vector3(p.x, Y, p.z);
        }

        private static void Add(List<Vector3> pts, Vector3 p)
        {
            if (pts.Count == 0 || Vector3.Distance(pts[pts.Count - 1], p) > 0.4f)
                pts.Add(p);
        }

        private static List<Vector3> Compact(List<Vector3> src)
        {
            List<Vector3> outPts = new List<Vector3>();
            for (int i = 0; i < src.Count; i++)
                Add(outPts, src[i]);
            return outPts;
        }

        private static bool Approximately(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.15f;
        }
    }
}
