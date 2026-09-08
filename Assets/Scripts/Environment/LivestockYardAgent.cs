using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public class LivestockYardAgent : MonoBehaviour
    {
        private enum YardState
        {
            Wander,
            Sit,
            GoToDoor,
            EnterInside,
            Hidden,
            ExitToYard
        }

        public LivestockType AnimalType;
        public bool IsChicken;

        private Vector3 minBound;
        private Vector3 maxBound;
        private Vector3 doorOutside;
        private Vector3 doorInside;
        private Vector3 targetPos;
        private YardState state;
        private float stateTimer;
        private float walkSpeed;
        private float bobPhase;
        private float sitYOffset;
        private float enterDelay;
        private bool queuedEnter;

        private float stuckTimer;
        private Vector3 lastStuckPos;

        public void Configure(LivestockType type, Vector3 min, Vector3 max, Vector3 home, int spawnIndex)
        {
            AnimalType = type;
            IsChicken = type == LivestockType.WhiteChicken || type == LivestockType.BlackChicken;
            minBound = min;
            maxBound = max;
            float doorOffset = ((spawnIndex % 5) - 2) * (IsChicken ? 0.12f : 0.22f);
            doorOutside = LivestockManager.GetDoorOutside(IsChicken, doorOffset);
            doorInside = LivestockManager.GetDoorInside(IsChicken, doorOffset);
            walkSpeed = IsChicken ? 0.85f : 0.55f;
            bobPhase = Random.Range(0f, 10f);
            enterDelay = spawnIndex * 0.28f;
            lastStuckPos = transform.position;
            _ = home;

            bool stayIn = LivestockManager.Instance != null && LivestockManager.Instance.ShouldAnimalsStayInside;
            if (stayIn)
            {
                transform.position = doorInside;
                transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                SetHidden(true);
                state = YardState.Hidden;
            }
            else
            {
                transform.position = LivestockManager.RandomYardPoint(IsChicken, minBound, maxBound);
                transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                SetHidden(false);
                PickWanderTarget();
                state = YardState.Wander;
                stateTimer = Random.Range(2f, 5f);
            }
        }

        private void Update()
        {
            bool stayIn = LivestockManager.Instance != null && LivestockManager.Instance.ShouldAnimalsStayInside;
            float dt = Time.deltaTime;

            if (stayIn)
            {
                HandleStayInside(dt);
                return;
            }

            HandleStayOutside(dt);
        }

        private void HandleStayInside(float dt)
        {
            if (state == YardState.Hidden)
            {
                queuedEnter = false;
                return;
            }

            if (state == YardState.ExitToYard)
            {
                queuedEnter = false;
                state = YardState.EnterInside;
                targetPos = doorInside;
            }

            if (state == YardState.Wander || state == YardState.Sit)
            {
                if (!queuedEnter)
                {
                    queuedEnter = true;
                    enterDelay = Random.Range(0.12f, 1.75f);
                }

                enterDelay -= dt;
                TickWanderOrIdle(dt);
                if (enterDelay > 0f) return;

                sitting = false;
                sitYOffset = 0f;
                queuedEnter = false;
                state = YardState.GoToDoor;
                targetPos = LivestockManager.NextWaypointAroundBuilding(IsChicken, transform.position, doorOutside, minBound, maxBound);
            }

            if (state == YardState.GoToDoor)
            {
                Vector3 next = LivestockManager.NextWaypointAroundBuilding(IsChicken, transform.position, doorOutside, minBound, maxBound);
                if (MoveToward(next, walkSpeed * 1.35f, dt, false))
                {
                    if (FlatDistance(transform.position, doorOutside) <= 0.28f)
                    {
                        state = YardState.EnterInside;
                        targetPos = doorInside;
                    }
                }
                return;
            }

            if (state == YardState.EnterInside)
            {
                if (MoveToward(doorInside, walkSpeed * 1.15f, dt, true))
                {
                    SetHidden(true);
                    transform.position = doorInside;
                    state = YardState.Hidden;
                }
            }
        }

        private void HandleStayOutside(float dt)
        {
            if (state == YardState.Hidden)
            {
                if (!queuedEnter)
                {
                    queuedEnter = true;
                    enterDelay = Random.Range(0.08f, 1.6f);
                }
                enterDelay -= dt;
                if (enterDelay > 0f) return;

                queuedEnter = false;
                transform.position = doorInside;
                SetHidden(false);
                transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                state = YardState.ExitToYard;
                targetPos = doorOutside;
            }
            else if (state == YardState.EnterInside)
            {
                queuedEnter = false;
                SetHidden(false);
                state = YardState.ExitToYard;
                targetPos = doorOutside;
            }
            else if (state == YardState.GoToDoor)
            {
                queuedEnter = false;
                state = YardState.Wander;
                PickWanderTarget();
                stateTimer = Random.Range(2f, 5f);
            }

            if (state == YardState.ExitToYard)
            {
                if (MoveToward(doorOutside, walkSpeed * 1.15f, dt, true))
                {
                    state = YardState.Wander;
                    PickWanderTarget();
                    stateTimer = Random.Range(3f, 6f);
                }
                return;
            }

            TickWanderOrIdle(dt);
        }

        private bool sitting;

        private void TickWanderOrIdle(float dt)
        {
            stateTimer -= dt;
            if (!sitting)
            {
                sitYOffset = Mathf.Lerp(sitYOffset, 0f, dt * 5f);
            }

            if (sitting)
            {
                sitYOffset = Mathf.Lerp(sitYOffset, IsChicken ? -0.06f : -0.04f, dt * 4f);
                ApplyBob(0f);
                if (stateTimer <= 0f)
                {
                    sitting = false;
                    sitYOffset = 0f;
                    PickWanderTarget();
                    stateTimer = Random.Range(3.5f, 8f);
                    state = YardState.Wander;
                }
                return;
            }

            Vector3 pos = transform.position;
            Vector3 toTarget = new Vector3(targetPos.x - pos.x, 0f, targetPos.z - pos.z);
            if (toTarget.sqrMagnitude > 0.04f)
            {
                Vector3 next = LivestockManager.NextWaypointAroundBuilding(IsChicken, pos, targetPos, minBound, maxBound);
                MoveToward(next, walkSpeed, dt, false);
            }
            else
            {
                ApplyBob(0f);
                if (stateTimer <= 0f)
                {
                    if (Random.value < 0.35f)
                    {
                        sitting = true;
                        state = YardState.Sit;
                        stateTimer = Random.Range(2.5f, 6f);
                    }
                    else
                    {
                        PickWanderTarget();
                        stateTimer = Random.Range(3f, 7f);
                    }
                }
            }
        }

        private bool MoveToward(Vector3 destination, float speed, float dt, bool doorTransit)
        {
            Vector3 pos = transform.position;
            Vector3 toTarget = new Vector3(destination.x - pos.x, 0f, destination.z - pos.z);
            if (toTarget.sqrMagnitude <= 0.04f)
            {
                pos = LivestockManager.ConstrainToYard(IsChicken, destination, minBound, maxBound, doorTransit);
                transform.position = pos;
                ApplyBob(0.35f);
                stuckTimer = 0f;
                return true;
            }

            Vector3 step = toTarget.normalized * (speed * dt);
            Vector3 next = LivestockManager.SlideMove(IsChicken, pos, step, minBound, maxBound, doorTransit);
            transform.position = next;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, dt * 5.5f);
            }
            ApplyBob(doorTransit ? 1.15f : 1f);

            if (!doorTransit && (state == YardState.Wander || state == YardState.GoToDoor))
            {
                if ((next - lastStuckPos).sqrMagnitude < 0.004f)
                {
                    stuckTimer += dt;
                    if (stuckTimer > 1.1f)
                    {
                        stuckTimer = 0f;
                        PickWanderTarget();
                        if (state == YardState.GoToDoor)
                        {
                            targetPos = LivestockManager.NextWaypointAroundBuilding(IsChicken, transform.position, doorOutside, minBound, maxBound);
                        }
                    }
                }
                else
                {
                    stuckTimer = 0f;
                    lastStuckPos = next;
                }
            }

            return FlatDistance(next, destination) <= 0.22f;
        }

        private void ApplyBob(float walkAmount)
        {
            bobPhase += Time.deltaTime * (8f + walkAmount * 6f);
            float bob = Mathf.Sin(bobPhase) * 0.025f * walkAmount;
            Vector3 p = transform.position;
            p.y = sitYOffset + bob;
            transform.position = p;
        }

        private void PickWanderTarget()
        {
            targetPos = LivestockManager.RandomYardPoint(IsChicken, minBound, maxBound);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void SetHidden(bool hide)
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] != null) rends[i].enabled = !hide;
            }
        }
    }
}
