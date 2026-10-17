using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Araç farlarını ve spot ışıklarını yöneten bileşen.
    /// Müşteriler otoparka park ettiğinde farları otomatik söndürür,
    /// alışveriş bitip arabaya binildiğinde farları tekrar yakar.
    /// </summary>
    public class VehicleHeadlightController : MonoBehaviour
    {
        public bool isEngineRunning = true;
        public List<Light> spotLights = new List<Light>();
        public List<Renderer> headlightRenderers = new List<Renderer>();
        private DayNightCycleManager registeredCycle;
        private float nextRefreshTime;

        private void Start()
        {
            if (DayNightCycleManager.Instance != null)
            {
                registeredCycle = DayNightCycleManager.Instance;
                DayNightCycleManager.Instance.RegisterVehicleHeadlightController(this);
            }
            UpdateHeadlights();
        }

        private void OnEnable()
        {
            nextRefreshTime = 0f;
        }

        private void OnDisable()
        {
            UpdateHeadlights();
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.25f;
            // Runtime-spawned and restored vehicles must also bind when the cycle
            // manager becomes available after their Start method.
            if (DayNightCycleManager.Instance != null && registeredCycle != DayNightCycleManager.Instance)
            {
                registeredCycle = DayNightCycleManager.Instance;
                registeredCycle.RegisterVehicleHeadlightController(this);
            }
            UpdateHeadlights();
        }

        public void SetHeadlightsActive(bool active)
        {
            isEngineRunning = active;
            UpdateHeadlights();
        }

        public void UpdateHeadlights()
        {
            bool isNight = DayNightCycleManager.Instance != null && DayNightCycleManager.Instance.IsNight;
            if (TimeManager.Instance != null)
            {
                float hour = TimeManager.Instance.Hour + TimeManager.Instance.Minute / 60f;
                isNight = hour >= 19.5f || hour < 6.5f;
            }
            bool turnOn = isActiveAndEnabled && isEngineRunning && isNight;

            for (int i = 0; i < spotLights.Count; i++)
            {
                if (spotLights[i] != null)
                {
                    if (spotLights[i].enabled != turnOn) spotLights[i].enabled = turnOn;
                }
            }

            Material targetMat = turnOn ? DayNightCycleManager.HeadlightOnMaterial : DayNightCycleManager.HeadlightOffMaterial;
            if (targetMat != null)
            {
                for (int i = 0; i < headlightRenderers.Count; i++)
                {
                    if (headlightRenderers[i] != null && headlightRenderers[i].sharedMaterial != targetMat)
                    {
                        headlightRenderers[i].sharedMaterial = targetMat;
                    }
                }
            }
        }
    }
}
