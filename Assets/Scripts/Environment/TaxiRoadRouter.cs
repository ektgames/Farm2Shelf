using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Taksi için asfalt şerit grafı: durak, kuzey siteler, güneydoğu, kafe, kasaba ve batı.
    /// </summary>
    public static class TaxiRoadRouter
    {
        private const float Lane = 1.5f;
        private const float HwyEastZ = -10.5f;
        private const float HwyWestZ = -7.5f;
        private const float NorthEastZ = 48.5f;
        private const float NorthWestZ = 51.5f;
        private const float DriveX = 93.5f;
        private const float ConnectZ = 50.0f;

        public static List<Vector3> BuildRoute(Vector3 from, Vector3 to)
        {
            List<Vector3> pts = new List<Vector3>();
            Add(pts, from);
            AppendExitToHub(pts, from);
            AppendHubToDest(pts, to);
            Add(pts, new Vector3(to.x, 0.05f, to.z));
            return Compact(pts);
        }

        private static void AppendExitToHub(List<Vector3> pts, Vector3 from)
        {
            if (from.x >= 80.0f && from.x <= 110.0f && from.z >= 18.0f && from.z <= 46.0f)
            {
                Add(pts, new Vector3(DriveX, 0.05f, from.z));
                Add(pts, new Vector3(DriveX, 0.05f, 38.0f));
                Add(pts, new Vector3(DriveX, 0.05f, ConnectZ));
                return;
            }

            if (from.z >= 45.0f)
            {
                float ave = ClosestNorthAve(from.x);
                Add(pts, new Vector3(ave - Lane, 0.05f, from.z));
                Add(pts, new Vector3(ave - Lane, 0.05f, ConnectZ));
                return;
            }

            if (from.z <= -58.0f)
            {
                if (from.x >= 90.0f)
                {
                    float ave = ClosestNorthAve(from.x);
                    Add(pts, new Vector3(ave + Lane, 0.05f, from.z));
                    Add(pts, new Vector3(ave + Lane, 0.05f, HwyWestZ));
                }
                else if (from.x >= 25.0f)
                {
                    Add(pts, new Vector3(76.5f, 0.05f, from.z));
                    Add(pts, new Vector3(76.5f, 0.05f, HwyWestZ));
                }
                else
                {
                    Add(pts, new Vector3(1.5f, 0.05f, from.z));
                    Add(pts, new Vector3(1.5f, 0.05f, -54.0f));
                    Add(pts, new Vector3(76.5f, 0.05f, HwyWestZ));
                }

                return;
            }

            if (from.x <= -95.0f)
            {
                float west = ClosestWestAve(from.x);
                Add(pts, new Vector3(west, 0.05f, from.z));
                Add(pts, new Vector3(west, 0.05f, HwyEastZ));
                Add(pts, new Vector3(-79.5f, 0.05f, HwyEastZ));
                return;
            }

            if (from.x >= 90.0f)
            {
                float ave = ClosestNorthAve(from.x);
                Add(pts, new Vector3(ave - Lane, 0.05f, from.z));
                Add(pts, new Vector3(ave - Lane, 0.05f, ConnectZ));
                return;
            }

            Add(pts, new Vector3(from.x, 0.05f, HwyWestZ));
        }

        private static void AppendHubToDest(List<Vector3> pts, Vector3 dest)
        {
            if (dest.z >= 45.0f)
            {
                float ave = ClosestNorthAve(dest.x);
                Add(pts, new Vector3(DriveX, 0.05f, ConnectZ));
                Add(pts, new Vector3(ave + Lane, 0.05f, NorthEastZ));
                Add(pts, new Vector3(ave + Lane, 0.05f, dest.z));
                return;
            }

            if (dest.z <= -58.0f)
            {
                if (dest.x >= 90.0f)
                {
                    float ave = ClosestNorthAve(dest.x);
                    Add(pts, new Vector3(ave - Lane, 0.05f, ConnectZ));
                    Add(pts, new Vector3(ave - Lane, 0.05f, dest.z));
                }
                else if (dest.x >= 25.0f)
                {
                    Add(pts, new Vector3(114.0f, 0.05f, ConnectZ));
                    Add(pts, new Vector3(114.0f, 0.05f, HwyEastZ));
                    Add(pts, new Vector3(73.5f, 0.05f, HwyEastZ));
                    Add(pts, new Vector3(73.5f, 0.05f, dest.z));
                }
                else
                {
                    Add(pts, new Vector3(114.0f, 0.05f, ConnectZ));
                    Add(pts, new Vector3(114.0f, 0.05f, HwyWestZ));
                    Add(pts, new Vector3(-1.5f, 0.05f, HwyWestZ));
                    Add(pts, new Vector3(-1.5f, 0.05f, dest.z));
                }

                return;
            }

            if (dest.x <= -95.0f)
            {
                Add(pts, new Vector3(114.0f, 0.05f, ConnectZ));
                Add(pts, new Vector3(114.0f, 0.05f, HwyWestZ));
                Add(pts, new Vector3(-79.5f, 0.05f, HwyWestZ));
                float west = ClosestWestAve(dest.x);
                Add(pts, new Vector3(west, 0.05f, HwyWestZ));
                Add(pts, new Vector3(west, 0.05f, dest.z));
                return;
            }

            if (dest.x >= 90.0f)
            {
                float ave = ClosestNorthAve(dest.x);
                Add(pts, new Vector3(ave + Lane, 0.05f, ConnectZ));
                Add(pts, new Vector3(ave + Lane, 0.05f, dest.z));
                return;
            }

            Add(pts, new Vector3(114.0f, 0.05f, ConnectZ));
            Add(pts, new Vector3(114.0f, 0.05f, HwyEastZ));
            Add(pts, new Vector3(dest.x, 0.05f, HwyEastZ));
        }

        public static List<Vector3> BuildReturnToStand(Vector3 from, Vector3 standSlot)
        {
            List<Vector3> pts = BuildRoute(from, new Vector3(DriveX, 0.05f, 38.0f));
            Add(pts, new Vector3(DriveX, 0.05f, 38.0f));
            Add(pts, new Vector3(standSlot.x, 0.05f, 38.0f));
            Add(pts, new Vector3(standSlot.x, standSlot.y, standSlot.z));
            return Compact(pts);
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

        private static float ClosestWestAve(float x)
        {
            float[] aves = { -112.0f, -150.0f, -188.0f, -226.0f };
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
    }
}
