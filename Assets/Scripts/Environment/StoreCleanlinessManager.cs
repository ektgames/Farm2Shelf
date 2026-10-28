using System.Collections.Generic;
using System.Collections;
using Farm2Shelf.Core;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    public class StoreCleanlinessManager : MonoBehaviour
    {
        public static StoreCleanlinessManager Instance { get; private set; }

        private readonly List<GameObject> activeTrashItems = new List<GameObject>();
        public IReadOnlyList<GameObject> TrashItems => activeTrashItems;
        private Transform trashParentGroup;
        public const float BinInteractionOffset = 0.75f;
        private const float BinRange = 6f;
        private readonly RaycastHit[] binPathHits = new RaycastHit[32];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            GameObject grp = new GameObject("Store_Trash_Group");
            grp.transform.SetParent(transform);
            trashParentGroup = grp.transform;
        }

        private static Material cachedTrashMat;

        private Material GetTrashMaterial()
        {
            if (cachedTrashMat != null) return cachedTrashMat;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            cachedTrashMat = new Material(shader)
            {
                name = "TrashPuddleMat",
                color = new Color(0.35f, 0.25f, 0.15f, 0.85f)
            };
            if (cachedTrashMat.HasProperty("_BaseColor")) cachedTrashMat.SetColor("_BaseColor", new Color(0.35f, 0.25f, 0.15f, 0.85f));
            return cachedTrashMat;
        }

        public int ActiveTrashCount
        {
            get
            {
                for (int i = activeTrashItems.Count - 1; i >= 0; i--)
                {
                    if (activeTrashItems[i] == null) activeTrashItems.RemoveAt(i);
                }
                return activeTrashItems.Count;
            }
        }

        public PlacedFurnitureController TrySpawnCustomerTrash(Vector3 customerPosition)
        {
            // %15 ihtimalle dükkan zeminine küçük çöp/leke düşer
            if (Random.value > 0.15f) return null;
            // Bins prevent a modest share of litter; extra bins improve coverage, not probability.
            if (Random.value < 0.30f)
            {
                PlacedFurnitureController bin = FindReachableBin(customerPosition);
                if (bin != null) return bin;
            }
            DropCustomerTrash(customerPosition);
            return null;
        }

        private PlacedFurnitureController FindReachableBin(Vector3 position)
        {
            PlacedFurnitureController nearest = null;
            float best = BinRange * BinRange;
            foreach (var bin in PlacedFurnitureController.AllPlacedFurniture)
            {
                if (bin == null || !bin.isActiveAndEnabled || bin.FurnitureType != FurnitureType.TrashCan) continue;
                Vector3 target = bin.GetFrontInteractionPosition(BinInteractionOffset);
                Vector3 direction = target - position;
                direction.y = 0f;
                float sqrDistance = direction.sqrMagnitude;
                if (sqrDistance > best) continue;
                int count = Physics.SphereCastNonAlloc(position + Vector3.up * 0.6f, 0.18f,
                    direction.normalized, binPathHits, Mathf.Sqrt(sqrDistance), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                bool blocked = count > 0;
                if (blocked) continue;
                best = sqrDistance;
                nearest = bin;
            }
            return nearest;
        }

        public void DropCustomerTrash(Vector3 customerPosition)
        {
            if (ActiveTrashCount >= 6) return;

            Vector3 spawnPos = customerPosition + new Vector3(Random.Range(-0.4f, 0.4f), 0.02f, Random.Range(-0.4f, 0.4f));

            if (PedestrianNavigation.IsBlocked(spawnPos)) spawnPos = customerPosition;
            if (PedestrianNavigation.IsBlocked(spawnPos)) return;
            GameObject trashObj = new GameObject("Customer_Trash_Puddle");
            trashObj.transform.SetParent(trashParentGroup, false);
            trashObj.transform.position = spawnPos;

            // Prosedürel Düz Çöp Lekesi Mesh
            GameObject puddleMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddleMesh.name = "Puddle_Mesh";
            puddleMesh.transform.SetParent(trashObj.transform, false);
            puddleMesh.transform.localPosition = Vector3.zero;
            puddleMesh.transform.localScale = new Vector3(0.45f, 0.01f, 0.45f);

            Renderer ren = puddleMesh.GetComponent<Renderer>();
            ren.sharedMaterial = GetTrashMaterial();

            Collider col = puddleMesh.GetComponent<Collider>();
            if (col != null) Destroy(col);

            activeTrashItems.Add(trashObj);
        }

        public void ShowBinDisposal(Vector3 customerPosition, PlacedFurnitureController bin)
        {
            if (bin != null) StartCoroutine(AnimateBinDisposal(customerPosition, bin));
        }

        private IEnumerator AnimateBinDisposal(Vector3 customerPosition, PlacedFurnitureController bin)
        {
            GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paper.name = "Disposed_Trash";
            paper.transform.SetParent(trashParentGroup, false);
            paper.transform.localScale = Vector3.one * 0.07f;
            Collider collider = paper.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            paper.GetComponent<Renderer>().sharedMaterial = GetTrashMaterial();
            Vector3 start = customerPosition + Vector3.up;
            float elapsed = 0f;
            while (elapsed < 0.45f && bin != null && paper != null)
            {
                float t = elapsed / 0.45f;
                Vector3 end = bin.transform.TransformPoint(new Vector3(0f, 0.68f, 0f));
                paper.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.2f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (paper != null) Destroy(paper);
        }

        public GameObject GetNearestTrashItem(Vector3 position, out float distance)
        {
            distance = float.MaxValue;
            GameObject nearest = null;

            for (int i = activeTrashItems.Count - 1; i >= 0; i--)
            {
                if (activeTrashItems[i] == null)
                {
                    activeTrashItems.RemoveAt(i);
                    continue;
                }

                float dist = Vector3.Distance(position, activeTrashItems[i].transform.position);
                if (dist < distance)
                {
                    distance = dist;
                    nearest = activeTrashItems[i];
                }
            }

            return nearest;
        }

        public void CleanTrashItem(GameObject trashObj)
        {
            if (trashObj != null)
            {
                activeTrashItems.Remove(trashObj);
                Destroy(trashObj);
            }
        }
    }
}
