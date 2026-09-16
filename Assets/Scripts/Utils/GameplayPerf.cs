using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Utils
{
    /// <summary>
    /// Oyun ayarlarını değiştirmez. Kamera, duvar önbelleği ve NonAlloc fizik ile
    /// müşteri/personel Update maliyetini düşürür.
    /// </summary>
    public static class GameplayPerf
    {
        private static Camera cachedMain;
        private static float nextCameraCheck;
        private static float nextWallScan;
        private static readonly HashSet<int> pedestrianWalls = new HashSet<int>();
        private static readonly HashSet<int> staffWalls = new HashSet<int>();
        private static readonly RaycastHit[] hits = new RaycastHit[16];
        private static readonly float[] PedestrianAngles = { 25f, -25f, 50f, -50f, 75f, -75f, 100f, -100f };
        private static readonly float[] StaffAngles = { 20f, -20f, 40f, -40f, 60f, -60f, 80f, -80f, 100f, -100f, 120f, -120f };

        public static Camera MainCamera
        {
            get
            {
                if (cachedMain == null || Time.unscaledTime >= nextCameraCheck)
                {
                    cachedMain = Camera.main;
                    nextCameraCheck = Time.unscaledTime + 0.75f;
                }

                return cachedMain;
            }
        }

        public static Vector3 SteerPedestrian(GameObject self, Vector3 pos, Vector3 dir, float step)
        {
            return Steer(self, pos, dir, step, 0.22f, Mathf.Max(0.55f, step + 0.20f), PedestrianAngles, pedestrianWalls);
        }

        public static Vector3 SteerStaff(GameObject self, Vector3 pos, Vector3 dir, float step)
        {
            return Steer(self, pos, dir, step, 0.25f, Mathf.Max(0.60f, step + 0.25f), StaffAngles, staffWalls);
        }

        private static Vector3 Steer(
            GameObject self,
            Vector3 pos,
            Vector3 dir,
            float step,
            float radius,
            float distance,
            float[] angles,
            HashSet<int> walls)
        {
            if (dir.sqrMagnitude < 0.0001f || self == null) return dir;
            EnsureWalls();

            Vector3 origin = pos + Vector3.up * 0.5f;
            if (!HitsWall(origin, dir, radius, distance, self, walls, out _))
                return dir;

            for (int i = 0; i < angles.Length; i++)
            {
                Vector3 test = Quaternion.Euler(0f, angles[i], 0f) * dir;
                if (!HitsWall(origin, test, radius, distance * 0.85f, self, walls, out _))
                    return test.normalized;
            }

            return dir;
        }

        private static bool HitsWall(
            Vector3 origin,
            Vector3 dir,
            float radius,
            float distance,
            GameObject self,
            HashSet<int> walls,
            out Vector3 normal)
        {
            normal = Vector3.zero;
            int n = Physics.SphereCastNonAlloc(origin, radius, dir, hits, distance);
            for (int i = 0; i < n; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || col.isTrigger) continue;
                if (self != null && (col.gameObject == self || col.transform.IsChildOf(self.transform)))
                    continue;
                if (!walls.Contains(col.GetInstanceID())) continue;
                normal = hits[i].normal;
                return true;
            }

            return false;
        }

        public static void InvalidateWalls()
        {
            nextWallScan = 0f;
        }

        private static void EnsureWalls()
        {
            if (Time.unscaledTime < nextWallScan && pedestrianWalls.Count > 0) return;
            nextWallScan = Time.unscaledTime + 4.0f;
            pedestrianWalls.Clear();
            staffWalls.Clear();

            Collider[] cols = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            for (int i = 0; i < cols.Length; i++)
            {
                Collider col = cols[i];
                if (col == null || col.isTrigger) continue;
                string n = col.name;
                bool wall = Has(n, "wall") || Has(n, "duvar") || Has(n, "building") || Has(n, "facade")
                    || Has(n, "partition") || Has(n, "outerwall") || Has(n, "storewall")
                    || Has(n, "storagewall") || Has(n, "roomwall");
                bool fence = Has(n, "fence") || Has(n, "boundary");
                bool skipPed = Has(n, "barrier") || Has(n, "turnstile") || Has(n, "line")
                    || Has(n, "divider") || Has(n, "housing") || Has(n, "border");
                bool staffExtra = Has(n, "barrier") || Has(n, "turnstile") || Has(n, "divider")
                    || Has(n, "border") || Has(n, "housing") || fence;

                int id = col.GetInstanceID();
                if (wall && !skipPed) pedestrianWalls.Add(id);
                if (wall || staffExtra) staffWalls.Add(id);
            }
        }

        private static bool Has(string name, string token)
        {
            return name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
