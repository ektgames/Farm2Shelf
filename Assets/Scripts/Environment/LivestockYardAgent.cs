using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    public class LivestockYardAgent : MonoBehaviour
    {
        public LivestockType AnimalType;
        public bool IsChicken;

        private Vector3 minBound;
        private Vector3 maxBound;
        private Vector3 homePos;
        private Vector3 targetPos;
        private float stateTimer;
        private bool sitting;
        private bool hidden;
        private float walkSpeed;
        private float bobPhase;
        private float sitYOffset;

        public void Configure(LivestockType type, Vector3 min, Vector3 max, Vector3 home)
        {
            AnimalType = type;
            IsChicken = type == LivestockType.WhiteChicken || type == LivestockType.BlackChicken;
            minBound = min;
            maxBound = max;
            homePos = home;
            walkSpeed = IsChicken ? 0.85f : 0.55f;
            bobPhase = Random.Range(0f, 10f);
            PickWanderTarget();
            stateTimer = Random.Range(1.5f, 4f);
        }

        private void Update()
        {
            if (LivestockManager.Instance != null && LivestockManager.Instance.ShouldAnimalsStayInside)
            {
                GoInside(Time.deltaTime);
                return;
            }

            if (hidden)
            {
                hidden = false;
                transform.position = ClampToYard(homePos + new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-1.2f, 1.2f)));
                SetVisible(true);
                PickWanderTarget();
            }

            stateTimer -= Time.deltaTime;
            if (!sitting)
            {
                sitYOffset = Mathf.Lerp(sitYOffset, 0f, Time.deltaTime * 5f);
            }
            if (sitting)
            {
                sitYOffset = Mathf.Lerp(sitYOffset, IsChicken ? -0.06f : -0.04f, Time.deltaTime * 4f);
                ApplyBob(0f);
                if (stateTimer <= 0f)
                {
                    sitting = false;
                    sitYOffset = 0f;
                    PickWanderTarget();
                    stateTimer = Random.Range(3.5f, 8f);
                }
                return;
            }

            Vector3 pos = transform.position;
            Vector3 flatTarget = new Vector3(targetPos.x, pos.y, targetPos.z);
            Vector3 toTarget = flatTarget - pos;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.04f)
            {
                Vector3 step = toTarget.normalized * (walkSpeed * Time.deltaTime);
                pos += step;
                pos = ClampToYard(pos);
                transform.position = pos;
                if (toTarget.sqrMagnitude > 0.01f)
                {
                    Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 5f);
                }
                ApplyBob(1f);
            }
            else
            {
                ApplyBob(0f);
                if (stateTimer <= 0f)
                {
                    if (Random.value < 0.35f)
                    {
                        sitting = true;
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

        private void GoInside(float dt)
        {
            if (hidden) return;

            Vector3 pos = transform.position;
            Vector3 toHome = homePos - pos;
            toHome.y = 0f;
            if (toHome.sqrMagnitude > 0.35f)
            {
                pos += toHome.normalized * (walkSpeed * 1.35f * dt);
                transform.position = pos;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toHome.normalized, Vector3.up), dt * 6f);
                ApplyBob(1.2f);
            }
            else
            {
                hidden = true;
                SetVisible(false);
            }
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
            targetPos = new Vector3(
                Random.Range(minBound.x, maxBound.x),
                0f,
                Random.Range(minBound.z, maxBound.z));
        }

        private Vector3 ClampToYard(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, minBound.x, maxBound.x);
            pos.z = Mathf.Clamp(pos.z, minBound.z, maxBound.z);
            pos.y = 0f;
            return pos;
        }

        private void SetVisible(bool visible)
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] != null) rends[i].enabled = visible;
            }
        }
    }
}
