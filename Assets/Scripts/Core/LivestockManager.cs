using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public class LivestockManager : MonoBehaviour
    {
        public static LivestockManager Instance { get; private set; }

        public const float ChickenMinX = 53.0f;
        public const float ChickenMaxX = 65.8f;
        public const float ChickenMinZ = 2.8f;
        public const float ChickenMaxZ = 18.8f;
        public static readonly Vector3 ChickenHome = new Vector3(58.6f, 0f, 10.8f);

        public const float CowMinX = 53.0f;
        public const float CowMaxX = 65.8f;
        public const float CowMinZ = 23.6f;
        public const float CowMaxZ = 39.8f;
        public static readonly Vector3 CowHome = new Vector3(59.2f, 0f, 33.6f);

        // EnvironmentBuilder kümes 5.4x3.6, ahır 6.6x4.8; kapılar -Z (bahçe) yüzünde.
        private const float ChickenFootprintHalfX = 3.45f;
        private const float ChickenFootprintHalfZ = 2.45f;
        private const float CowFootprintHalfX = 4.15f;
        private const float CowFootprintHalfZ = 3.20f;

        private readonly Dictionary<LivestockType, int> owned = new Dictionary<LivestockType, int>();
        private int eggCount;
        private int milkCount;
        private float eggFrac;
        private float milkFrac;
        private Transform animalRoot;
        private readonly List<LivestockYardAgent> spawned = new List<LivestockYardAgent>();

        public event Action OnLivestockChanged;

        public int EggCount => eggCount;
        public int MilkCount => milkCount;
        public float EggFrac => eggFrac;
        public float MilkFrac => milkFrac;

        public int GetReadyCrates(bool chicken)
        {
            int units = chicken ? eggCount : milkCount;
            return units / LivestockProductDatabase.PackSize;
        }

        public int GetUnitsTowardNextCrate(bool chicken)
        {
            int units = chicken ? eggCount : milkCount;
            return units % LivestockProductDatabase.PackSize;
        }

        public float GetHoursUntilNextCrate(bool chicken)
        {
            int animals = chicken ? TotalChickens : TotalCows;
            float rate = LivestockProductDatabase.GetExactUnitsPerHour(animals);
            if (rate <= 0.01f) return -1f;
            float have = GetUnitsTowardNextCrate(chicken) + (chicken ? eggFrac : milkFrac);
            float need = LivestockProductDatabase.PackSize - have;
            if (need <= 0.01f) return 0f;
            return need / rate;
        }

        public bool ShouldAnimalsStayInside
        {
            get
            {
                int hour = TimeManager.Instance != null ? TimeManager.Instance.Hour : 8;
                if (hour >= 19 || hour < 6) return true;
                if (WeatherManager.Instance == null) return false;
                return WeatherManager.Instance.CurrentWeather == WeatherType.Rainy
                    || WeatherManager.Instance.CurrentWeather == WeatherType.Snowy;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            foreach (LivestockType t in Enum.GetValues(typeof(LivestockType)))
            {
                owned[t] = 0;
            }
        }

        private void OnEnable()
        {
            BindTimeEvents();
        }

        private void Start()
        {
            BindTimeEvents();
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnHourPassed -= HandleHourPassed;
            }
        }

        private void BindTimeEvents()
        {
            if (TimeManager.Instance == null) return;
            TimeManager.Instance.OnHourPassed -= HandleHourPassed;
            TimeManager.Instance.OnHourPassed += HandleHourPassed;
        }

        public int GetOwned(LivestockType type)
        {
            return owned.ContainsKey(type) ? owned[type] : 0;
        }

        public int TotalChickens => GetOwned(LivestockType.WhiteChicken) + GetOwned(LivestockType.BlackChicken);
        public int TotalCows => GetOwned(LivestockType.HolsteinCow) + GetOwned(LivestockType.BrownCow);

        public int RemainingSlots(LivestockType type)
        {
            LivestockShopDef def = LivestockProductDatabase.GetShopDef(type);
            if (def == null) return 0;
            return def.isChicken
                ? LivestockProductDatabase.MaxChickens - TotalChickens
                : LivestockProductDatabase.MaxCows - TotalCows;
        }

        public bool CanAdd(LivestockType type, int count)
        {
            return count > 0 && RemainingSlots(type) >= count;
        }

        public bool GrantAnimals(LivestockType type, int count)
        {
            if (!CanAdd(type, count)) return false;
            owned[type] = GetOwned(type) + count;
            SpawnMissingVisuals();
            OnLivestockChanged?.Invoke();
            return true;
        }

        public bool ConsumeEggs(int amount)
        {
            if (amount <= 0 || eggCount < amount) return false;
            eggCount -= amount;
            OnLivestockChanged?.Invoke();
            return true;
        }

        public bool ConsumeMilk(int amount)
        {
            if (amount <= 0 || milkCount < amount) return false;
            milkCount -= amount;
            OnLivestockChanged?.Invoke();
            return true;
        }

        public bool ConsumeCrates(bool chicken, int crateCount)
        {
            int units = crateCount * LivestockProductDatabase.PackSize;
            return chicken ? ConsumeEggs(units) : ConsumeMilk(units);
        }

        public void AddEggs(int amount)
        {
            if (amount <= 0) return;
            eggCount = Mathf.Min(LivestockProductDatabase.MaxEggStorage, eggCount + amount);
            OnLivestockChanged?.Invoke();
        }

        public void AddMilk(int amount)
        {
            if (amount <= 0) return;
            milkCount = Mathf.Min(LivestockProductDatabase.MaxMilkStorage, milkCount + amount);
            OnLivestockChanged?.Invoke();
        }

        public void NotifyYardsReady()
        {
            SpawnMissingVisuals();
        }

        public void RestoreState(int whiteChicken, int blackChicken, int holstein, int brown, int eggs, int milk, float eggProgress = 0f, float milkProgress = 0f)
        {
            owned[LivestockType.WhiteChicken] = Mathf.Max(0, whiteChicken);
            owned[LivestockType.BlackChicken] = Mathf.Max(0, blackChicken);
            owned[LivestockType.HolsteinCow] = Mathf.Max(0, holstein);
            owned[LivestockType.BrownCow] = Mathf.Max(0, brown);
            eggCount = Mathf.Clamp(eggs, 0, LivestockProductDatabase.MaxEggStorage);
            milkCount = Mathf.Clamp(milk, 0, LivestockProductDatabase.MaxMilkStorage);
            int extraEggs = Mathf.Max(0, Mathf.FloorToInt(eggProgress));
            int extraMilk = Mathf.Max(0, Mathf.FloorToInt(milkProgress));
            if (extraEggs > 0) eggCount = Mathf.Min(LivestockProductDatabase.MaxEggStorage, eggCount + extraEggs);
            if (extraMilk > 0) milkCount = Mathf.Min(LivestockProductDatabase.MaxMilkStorage, milkCount + extraMilk);
            eggFrac = Mathf.Clamp01(eggProgress - extraEggs);
            milkFrac = Mathf.Clamp01(milkProgress - extraMilk);
            ClampOwnedToCaps();
            SpawnMissingVisuals();
            OnLivestockChanged?.Invoke();
        }

        public void ResetToDefaults()
        {
            foreach (LivestockType t in Enum.GetValues(typeof(LivestockType)))
            {
                owned[t] = 0;
            }
            eggCount = 0;
            milkCount = 0;
            eggFrac = 0f;
            milkFrac = 0f;
            ClearVisuals();
            OnLivestockChanged?.Invoke();
        }

        private void ClampOwnedToCaps()
        {
            int chickens = TotalChickens;
            if (chickens > LivestockProductDatabase.MaxChickens)
            {
                int overflow = chickens - LivestockProductDatabase.MaxChickens;
                TrimType(LivestockType.BlackChicken, ref overflow);
                TrimType(LivestockType.WhiteChicken, ref overflow);
            }
            int cows = TotalCows;
            if (cows > LivestockProductDatabase.MaxCows)
            {
                int overflow = cows - LivestockProductDatabase.MaxCows;
                TrimType(LivestockType.BrownCow, ref overflow);
                TrimType(LivestockType.HolsteinCow, ref overflow);
            }
        }

        private void TrimType(LivestockType type, ref int overflow)
        {
            if (overflow <= 0) return;
            int have = GetOwned(type);
            int cut = Mathf.Min(have, overflow);
            owned[type] = have - cut;
            overflow -= cut;
        }

        private void HandleHourPassed()
        {
            if (TimeManager.Instance == null) return;
            int hour = TimeManager.Instance.Hour;
            if (hour < LivestockProductDatabase.ProductionHourStart || hour > LivestockProductDatabase.ProductionHourEnd) return;

            bool changed = false;
            int chickens = TotalChickens;
            int cows = TotalCows;
            if (chickens > 0 && eggCount < LivestockProductDatabase.MaxEggStorage)
            {
                eggFrac += LivestockProductDatabase.GetExactUnitsPerHour(chickens);
                int whole = Mathf.FloorToInt(eggFrac);
                if (whole > 0)
                {
                    eggCount = Mathf.Min(LivestockProductDatabase.MaxEggStorage, eggCount + whole);
                    eggFrac -= whole;
                    changed = true;
                }
            }
            if (cows > 0 && milkCount < LivestockProductDatabase.MaxMilkStorage)
            {
                milkFrac += LivestockProductDatabase.GetExactUnitsPerHour(cows);
                int whole = Mathf.FloorToInt(milkFrac);
                if (whole > 0)
                {
                    milkCount = Mathf.Min(LivestockProductDatabase.MaxMilkStorage, milkCount + whole);
                    milkFrac -= whole;
                    changed = true;
                }
            }
            if (changed)
            {
                OnLivestockChanged?.Invoke();
            }
        }

        private void SpawnMissingVisuals()
        {
            EnsureRoot();
            ClearVisuals();

            SpawnGroup(LivestockType.WhiteChicken, true);
            SpawnGroup(LivestockType.BlackChicken, true);
            SpawnGroup(LivestockType.HolsteinCow, false);
            SpawnGroup(LivestockType.BrownCow, false);
        }

        private void SpawnGroup(LivestockType type, bool chicken)
        {
            int count = GetOwned(type);
            Vector3 min = chicken
                ? new Vector3(ChickenMinX, 0f, ChickenMinZ)
                : new Vector3(CowMinX, 0f, CowMinZ);
            Vector3 max = chicken
                ? new Vector3(ChickenMaxX, 0f, ChickenMaxZ)
                : new Vector3(CowMaxX, 0f, CowMaxZ);
            Vector3 home = chicken ? ChickenHome : CowHome;

            for (int i = 0; i < count; i++)
            {
                GameObject go = LivestockModelBuilder.BuildAnimal(type, animalRoot);
                LivestockYardAgent agent = go.AddComponent<LivestockYardAgent>();
                agent.Configure(type, min, max, home, i);
                spawned.Add(agent);
            }
        }

        public static void GetBuildingFootprint(bool chicken, out Vector3 bMin, out Vector3 bMax)
        {
            Vector3 home = chicken ? ChickenHome : CowHome;
            float hx = chicken ? ChickenFootprintHalfX : CowFootprintHalfX;
            float hz = chicken ? ChickenFootprintHalfZ : CowFootprintHalfZ;
            bMin = new Vector3(home.x - hx, 0f, home.z - hz);
            bMax = new Vector3(home.x + hx, 0f, home.z + hz);
        }

        public static Vector3 GetDoorOutside(bool chicken, float lateralOffset = 0f)
        {
            Vector3 home = chicken ? ChickenHome : CowHome;
            GetBuildingFootprint(chicken, out Vector3 bMin, out Vector3 bMax);
            float maxOffset = chicken ? 0.35f : 0.70f;
            float x = Mathf.Clamp(home.x + lateralOffset, home.x - maxOffset, home.x + maxOffset);
            float z = bMin.z - (chicken ? 1.05f : 1.25f);
            return new Vector3(x, 0f, z);
        }

        public static Vector3 GetDoorInside(bool chicken, float lateralOffset = 0f)
        {
            Vector3 home = chicken ? ChickenHome : CowHome;
            float maxOffset = chicken ? 0.28f : 0.55f;
            float x = Mathf.Clamp(home.x + lateralOffset, home.x - maxOffset, home.x + maxOffset);
            float z = home.z - (chicken ? 0.55f : 0.70f);
            return new Vector3(x, 0f, z);
        }

        public static bool IsInDoorCorridor(bool chicken, Vector3 pos)
        {
            Vector3 home = chicken ? ChickenHome : CowHome;
            Vector3 outside = GetDoorOutside(chicken);
            Vector3 inside = GetDoorInside(chicken);
            float halfW = chicken ? 0.70f : 1.45f;
            float minZ = Mathf.Min(outside.z, inside.z) - 0.15f;
            float maxZ = Mathf.Max(outside.z, inside.z) + 0.20f;
            return Mathf.Abs(pos.x - home.x) <= halfW && pos.z >= minZ && pos.z <= maxZ;
        }

        public static bool IsInsideBuilding(bool chicken, Vector3 pos)
        {
            GetBuildingFootprint(chicken, out Vector3 bMin, out Vector3 bMax);
            return pos.x >= bMin.x && pos.x <= bMax.x && pos.z >= bMin.z && pos.z <= bMax.z;
        }

        public static Vector3 ConstrainToYard(bool chicken, Vector3 pos, Vector3 min, Vector3 max, bool allowDoorTransit)
        {
            pos.x = Mathf.Clamp(pos.x, min.x, max.x);
            pos.z = Mathf.Clamp(pos.z, min.z, max.z);
            pos.y = 0f;

            if (allowDoorTransit && IsInDoorCorridor(chicken, pos))
            {
                return pos;
            }

            if (!IsInsideBuilding(chicken, pos))
            {
                return pos;
            }

            GetBuildingFootprint(chicken, out Vector3 bMin, out Vector3 bMax);
            if (IsInDoorCorridor(chicken, pos))
            {
                pos.z = bMin.z - 0.35f;
                pos.x = Mathf.Clamp(pos.x, min.x, max.x);
                pos.z = Mathf.Clamp(pos.z, min.z, max.z);
                return pos;
            }

            float dLeft = pos.x - bMin.x;
            float dRight = bMax.x - pos.x;
            float dFront = pos.z - bMin.z;
            float dBack = bMax.z - pos.z;
            float nearest = Mathf.Min(dLeft, Mathf.Min(dRight, Mathf.Min(dFront, dBack)));
            const float push = 0.18f;
            if (nearest == dFront) pos.z = bMin.z - push;
            else if (nearest == dBack) pos.z = bMax.z + push;
            else if (nearest == dLeft) pos.x = bMin.x - push;
            else pos.x = bMax.x + push;

            pos.x = Mathf.Clamp(pos.x, min.x, max.x);
            pos.z = Mathf.Clamp(pos.z, min.z, max.z);
            return pos;
        }

        public static Vector3 RandomYardPoint(bool chicken, Vector3 min, Vector3 max)
        {
            GetBuildingFootprint(chicken, out Vector3 bMin, out Vector3 bMax);
            int sector = UnityEngine.Random.Range(0, 4);
            Vector3 p = Vector3.zero;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                int s = (sector + attempt) % 4;
                if (s == 0)
                {
                    p = new Vector3(UnityEngine.Random.Range(min.x, max.x), 0f, UnityEngine.Random.Range(min.z, Mathf.Max(min.z + 0.2f, bMin.z - 0.55f)));
                }
                else if (s == 1)
                {
                    p = new Vector3(UnityEngine.Random.Range(min.x, max.x), 0f, UnityEngine.Random.Range(Mathf.Min(max.z - 0.2f, bMax.z + 0.55f), max.z));
                }
                else if (s == 2)
                {
                    p = new Vector3(UnityEngine.Random.Range(min.x, Mathf.Max(min.x + 0.2f, bMin.x - 0.55f)), 0f, UnityEngine.Random.Range(min.z, max.z));
                }
                else
                {
                    p = new Vector3(UnityEngine.Random.Range(Mathf.Min(max.x - 0.2f, bMax.x + 0.55f), max.x), 0f, UnityEngine.Random.Range(min.z, max.z));
                }

                p = ConstrainToYard(chicken, p, min, max, false);
                if (!IsInsideBuilding(chicken, p) && !IsInDoorCorridor(chicken, p))
                {
                    return p;
                }
            }

            return GetDoorOutside(chicken);
        }

        public static bool PathHitsBuilding(bool chicken, Vector3 from, Vector3 dest)
        {
            for (int i = 1; i <= 8; i++)
            {
                Vector3 p = Vector3.Lerp(from, dest, i / 8f);
                if (IsInsideBuilding(chicken, p) && !IsInDoorCorridor(chicken, p))
                {
                    return true;
                }
            }
            return false;
        }

        public static Vector3 SlideMove(bool chicken, Vector3 from, Vector3 delta, Vector3 min, Vector3 max, bool doorTransit)
        {
            Vector3 desired = from + delta;
            Vector3 full = ConstrainToYard(chicken, desired, min, max, doorTransit);
            if (doorTransit || !IsInsideBuilding(chicken, full))
            {
                return full;
            }

            Vector3 xOnly = ConstrainToYard(chicken, new Vector3(from.x + delta.x, 0f, from.z), min, max, doorTransit);
            if (doorTransit || !IsInsideBuilding(chicken, xOnly))
            {
                return xOnly;
            }

            Vector3 zOnly = ConstrainToYard(chicken, new Vector3(from.x, 0f, from.z + delta.z), min, max, doorTransit);
            if (doorTransit || !IsInsideBuilding(chicken, zOnly))
            {
                return zOnly;
            }

            return ConstrainToYard(chicken, from, min, max, false);
        }

        public static Vector3 NextWaypointAroundBuilding(bool chicken, Vector3 from, Vector3 destination, Vector3 min, Vector3 max)
        {
            GetBuildingFootprint(chicken, out Vector3 bMin, out Vector3 bMax);
            Vector3 dest = destination;
            dest.y = 0f;
            from.y = 0f;

            bool fromSouth = from.z < bMin.z - 0.02f;
            bool fromNorth = from.z > bMax.z + 0.02f;
            bool destSouth = dest.z < bMin.z - 0.02f;
            bool destNorth = dest.z > bMax.z + 0.02f;
            bool fromWest = from.x < bMin.x - 0.02f;
            bool fromEast = from.x > bMax.x + 0.02f;
            bool destWest = dest.x < bMin.x - 0.02f;
            bool destEast = dest.x > bMax.x + 0.02f;
            bool crossZ = (fromSouth && destNorth) || (fromNorth && destSouth);
            bool crossX = (fromWest && destEast) || (fromEast && destWest);
            bool fromInside = IsInsideBuilding(chicken, from) && !IsInDoorCorridor(chicken, from);
            bool hits = PathHitsBuilding(chicken, from, dest);

            if (!crossZ && !crossX && !fromInside && !hits)
            {
                return dest;
            }

            float centerX = (bMin.x + bMax.x) * 0.5f;
            bool goEast = from.x >= centerX;
            if (fromEast) goEast = true;
            if (fromWest) goEast = false;
            float sideX = goEast
                ? Mathf.Min(max.x, bMax.x + 0.85f)
                : Mathf.Max(min.x, bMin.x - 0.85f);

            if (fromInside || hits || (from.x > bMin.x - 0.2f && from.x < bMax.x + 0.2f && from.z > bMin.z - 0.2f && from.z < bMax.z + 0.2f))
            {
                if (Mathf.Abs(from.x - sideX) > 0.22f)
                {
                    return new Vector3(sideX, 0f, Mathf.Clamp(from.z, min.z, max.z));
                }
            }

            if (Mathf.Abs(from.z - dest.z) > 0.28f && (crossZ || hits))
            {
                return new Vector3(from.x, 0f, dest.z);
            }

            if (crossX)
            {
                float centerZ = (bMin.z + bMax.z) * 0.5f;
                bool goNorth = from.z >= centerZ;
                if (fromNorth) goNorth = true;
                if (fromSouth) goNorth = false;
                float sideZ = goNorth
                    ? Mathf.Min(max.z, bMax.z + 0.50f)
                    : Mathf.Max(min.z, bMin.z - 0.50f);
                if (Mathf.Abs(from.z - sideZ) > 0.28f)
                {
                    return new Vector3(Mathf.Clamp(from.x, min.x, max.x), 0f, sideZ);
                }
            }

            return dest;
        }

        private void EnsureRoot()
        {
            if (animalRoot != null) return;
            GameObject existing = GameObject.Find("Livestock_Animals_Root");
            if (existing != null)
            {
                animalRoot = existing.transform;
                return;
            }
            GameObject root = new GameObject("Livestock_Animals_Root");
            animalRoot = root.transform;
        }

        private void ClearVisuals()
        {
            spawned.Clear();
            if (animalRoot == null) return;
            for (int i = animalRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = animalRoot.GetChild(i);
                if (child == null) continue;
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }
}
