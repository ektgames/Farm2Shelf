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
            if (waypoints != null) path.AddRange(waypoints);
            pathIndex = 0;
            duty = nextDuty;
            JobLabelTr = labelTr ?? "";
            JobLabelEn = labelEn ?? "";
            onPathDone = arrived;
            SetHeadlights(true);
            if (path.Count < 2)
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
            if (dist < 0.55f)
            {
                pathIndex++;
                if (pathIndex >= path.Count) FinishPath();
                return;
            }

            Vector3 dir = to / dist;
            Vector3 next = transform.position + dir * speed * Time.deltaTime;
            next.y = CityTrafficManager.GetBridgeElevation(next.x, next.z, out _);
            transform.position = next;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), Time.deltaTime * 7f);

            float spin = speed * 80f * Time.deltaTime;
            for (int i = 0; i < wheels.Count; i++)
            {
                if (wheels[i] != null) wheels[i].Rotate(Vector3.right, spin, Space.Self);
            }
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
