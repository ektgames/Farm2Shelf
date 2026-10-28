using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    // Register after construction, even if the day/night manager starts later than the map.
    public sealed class RailwayStationLighting : MonoBehaviour
    {
        public readonly List<GameObject> Bulbs = new List<GameObject>();
        public readonly List<Light> Lights = new List<Light>();
        public readonly List<GameObject> Windows = new List<GameObject>();

        IEnumerator Start()
        {
            while (DayNightCycleManager.Instance == null) yield return null;
            var manager = DayNightCycleManager.Instance;
            for (int i = 0; i < Lights.Count; i++)
                manager.RegisterStreetLamp(Bulbs[i], Lights[i]);
            foreach (var window in Windows)
                manager.RegisterBuildingWindow(window);
        }
    }
}
