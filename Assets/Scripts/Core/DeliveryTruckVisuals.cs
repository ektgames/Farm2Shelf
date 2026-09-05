using UnityEngine;

namespace Farm2Shelf.Core
{
    /// <summary>
    /// Teslimat kamyonlarının ortak rota noktaları, kayıt pozundan aşama tahmini
    /// ve sahnede kalan donmuş kamyon görsellerinin temizliği.
    /// </summary>
    public static class DeliveryTruckVisuals
    {
        public static readonly Vector3 StartPos = new Vector3(180f, 0.05f, -7.5f);
        public static readonly Vector3 JunctionPos = new Vector3(13.0f, 0.05f, -7.5f);
        public static readonly Vector3 DockPos = new Vector3(13.0f, 0.05f, 1.5f);
        public static readonly Vector3 DespawnPos = new Vector3(-340.0f, 0.05f, -7.5f);
        public static readonly Quaternion FacingWest = Quaternion.Euler(0f, -90f, 0f);
        public static readonly Quaternion FacingNorth = Quaternion.Euler(0f, 0f, 0f);

        public const string WholesaleTruckName = "WholesaleDeliveryTruck";
        public const string GreenTruckName = "GreenFarmDeliveryTruck";
        public const string LegacyTruckName = "Wholesale_Box_Truck";

        public static void DestroyStrayTrucksAndPopups()
        {
            DestroyNamed("WholesaleDeliveryTruck");
            DestroyNamed("Wholesale_Box_Truck");
            DestroyNamed("GreenFarmDeliveryTruck");
            DestroyNamed("Popup_TruckStatus");
            DestroyNamed("Popup_GreenTruckStatus");
        }

        public static bool TryReadLeftoverTruckPose(out Vector3 position, out Quaternion rotation)
        {
            GameObject leftover = GameObject.Find(WholesaleTruckName);
            if (leftover == null) leftover = GameObject.Find(GreenTruckName);
            if (leftover == null) leftover = GameObject.Find(LegacyTruckName);

            if (leftover == null)
            {
                position = StartPos;
                rotation = FacingWest;
                return false;
            }

            position = leftover.transform.position;
            rotation = leftover.transform.rotation;
            return true;
        }

        public static DeliveryTruckPhase ParsePhase(string phase, Vector3 position)
        {
            if (!string.IsNullOrEmpty(phase) && System.Enum.TryParse(phase, out DeliveryTruckPhase parsed))
            {
                return parsed;
            }

            return InferPhase(position);
        }

        public static DeliveryTruckPhase InferPhase(Vector3 position)
        {
            if (position.z > -3.5f)
            {
                return DeliveryTruckPhase.Unloading;
            }

            if (position.x > 20f)
            {
                return DeliveryTruckPhase.Approaching;
            }

            if (position.x < 8f)
            {
                return DeliveryTruckPhase.Departing;
            }

            return DeliveryTruckPhase.Approaching;
        }

        private static void DestroyNamed(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            while (go != null)
            {
                go.name = objectName + "_Destroying";
                Object.Destroy(go);
                go = GameObject.Find(objectName);
            }
        }
    }
}
