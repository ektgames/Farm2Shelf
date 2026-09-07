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
                Vector3 pos = new Vector3(
                    UnityEngine.Random.Range(min.x, max.x),
                    0f,
                    UnityEngine.Random.Range(min.z, max.z));
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                LivestockYardAgent agent = go.AddComponent<LivestockYardAgent>();
                agent.Configure(type, min, max, home);
                spawned.Add(agent);
            }
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
