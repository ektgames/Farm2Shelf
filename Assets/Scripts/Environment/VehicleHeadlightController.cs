using System.Collections.Generic;
using UnityEngine;

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

        private void Start()
        {
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.RegisterVehicleHeadlightController(this);
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
            bool isNight = (DayNightCycleManager.Instance != null) ? DayNightCycleManager.Instance.IsNight : false;
            bool turnOn = isEngineRunning && isNight;

            for (int i = 0; i < spotLights.Count; i++)
            {
                if (spotLights[i] != null)
                {
                    spotLights[i].enabled = turnOn;
                }
            }

            Material targetMat = turnOn ? DayNightCycleManager.HeadlightOnMaterial : DayNightCycleManager.HeadlightOffMaterial;
            if (targetMat != null)
            {
                for (int i = 0; i < headlightRenderers.Count; i++)
                {
                    if (headlightRenderers[i] != null)
                    {
                        headlightRenderers[i].sharedMaterial = targetMat;
                    }
                }
            }
        }
    }
}
