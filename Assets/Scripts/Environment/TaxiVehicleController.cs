using System;
using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public enum TaxiDuty
    {
        Parked,
        ToPickup,
        ToDropoff,
        Returning
    }

    public class TaxiVehicleController : MonoBehaviour
    {
        public int SlotIndex { get; private set; }
        public bool IsWorking => duty != TaxiDuty.Parked;
        public TaxiDuty Duty => duty;
        public string JobLabelTr { get; set; } = "";
        public string JobLabelEn { get; set; } = "";

        private readonly List<Transform> wheels = new List<Transform>();
        private readonly List<Vector3> path = new List<Vector3>();
        private Vector3 homePos;
        private Quaternion homeRot;
        private int pathIndex;
        private float speed = 10.5f;
        private TaxiDuty duty = TaxiDuty.Parked;
        private Action onPathDone;
        private Transform passengerRoot;

        public void Setup(int slot, Vector3 parkPos, Quaternion parkRot, List<Transform> wheelList)
        {
            SlotIndex = slot;
            homePos = parkPos;
            homeRot = parkRot;
            wheels.Clear();
            if (wheelList != null) wheels.AddRange(wheelList);
            SnapPark();
        }

        public void SnapPark()
        {
            ClearPassenger();
            duty = TaxiDuty.Parked;
            path.Clear();
            onPathDone = null;
            JobLabelTr = "";
            JobLabelEn = "";
            transform.position = homePos;
            transform.rotation = homeRot;
            SetHeadlights(false);
        }

        public void Drive(List<Vector3> waypoints, TaxiDuty nextDuty, string labelTr, string labelEn, Action arrived)
        {
            path.Clear();
            if (waypoints != null)
            {
                for (int i = 0; i < waypoints.Count; i++)
                {
                    if (!TaxiRoadRouter.IsCampusInterior(waypoints[i])
                        && !TaxiRoadRouter.IsIllegalRiverPoint(waypoints[i]))
                    {
                        path.Add(waypoints[i]);
                    }
                }
            }

            pathIndex = SkipReachedPoints(0);
            if (TaxiRoadRouter.IsCampusInterior(transform.position)
                || TaxiRoadRouter.IsIllegalRiverPoint(transform.position))
            {
                Vector3 safe = TaxiRoadRouter.SnapToNearestLane(transform.position);
                safe.y = CityTrafficManager.GetBridgeElevation(safe.x, safe.z, out _);
                transform.position = safe;
            }

            duty = nextDuty;
            JobLabelTr = labelTr ?? "";
            JobLabelEn = labelEn ?? "";
            onPathDone = arrived;
            SetHeadlights(true);
            if (path.Count < 2 || pathIndex >= path.Count)
            {
                onPathDone?.Invoke();
            }
        }

        private void SetHeadlights(bool on)
        {
            VehicleHeadlightController lights = GetComponent<VehicleHeadlightController>();
            if (lights != null) lights.SetHeadlightsActive(on);
        }

        public void AttachPassenger(GameObject passenger)
        {
            ClearPassenger();
            if (passenger == null) return;
            passengerRoot = passenger.transform;
            passengerRoot.SetParent(transform, false);
            passengerRoot.localPosition = new Vector3(0.28f, 0.05f, 0.05f);
            passengerRoot.localRotation = Quaternion.identity;
            Collider[] cols = passenger.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
        }

        public void DropPassengerAt(Vector3 worldPos)
        {
            if (passengerRoot == null) return;
            passengerRoot.SetParent(null, true);
            passengerRoot.position = new Vector3(worldPos.x, 0.02f, worldPos.z);
            Destroy(passengerRoot.gameObject, 1.25f);
            passengerRoot = null;
        }

        public void ClearPassenger()
        {
            if (passengerRoot != null)
            {
                Destroy(passengerRoot.gameObject);
                passengerRoot = null;
            }
        }

        private void Update()
        {
            if (duty == TaxiDuty.Parked || path.Count < 2) return;
            if (pathIndex >= path.Count)
            {
                FinishPath();
                return;
            }

            Vector3 target = path[pathIndex];
            float y = CityTrafficManager.GetBridgeElevation(target.x, target.z, out _);
            Vector3 flat = new Vector3(target.x, y, target.z);
            Vector3 to = flat - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 1.15f)
            {
                pathIndex++;
                if (pathIndex >= path.Count) FinishPath();
                return;
            }

            Vector3 dir = to / dist;
            Vector3 next = transform.position + dir * Mathf.Min(dist, speed * Time.deltaTime);
            next.y = CityTrafficManager.GetBridgeElevation(next.x, next.z, out _);
            if (TaxiRoadRouter.IsCampusInterior(next) || TaxiRoadRouter.IsIllegalRiverPoint(next))
            {
                pathIndex++;
                return;
            }

            transform.position = next;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir, Vector3.up),
                    Time.deltaTime * 7f);
            }

            float spin = speed * 80f * Time.deltaTime;
            for (int i = 0; i < wheels.Count; i++)
            {
                if (wheels[i] != null) wheels[i].Rotate(Vector3.right, spin, Space.Self);
            }
        }

        private int SkipReachedPoints(int start)
        {
            Vector3 pos = transform.position;
            int i = start;
            while (i < path.Count)
            {
                Vector3 d = path[i] - pos;
                d.y = 0f;
                if (d.magnitude > 1.15f) break;
                i++;
            }

            return i;
        }

        private void FinishPath()
        {
            Action done = onPathDone;
            onPathDone = null;
            if (duty == TaxiDuty.Returning)
            {
                SnapPark();
            }

            done?.Invoke();
        }
    }
}
