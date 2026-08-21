using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    public class StoreCleanlinessManager : MonoBehaviour
    {
        public static StoreCleanlinessManager Instance { get; private set; }

        private readonly List<GameObject> activeTrashItems = new List<GameObject>();
        private Transform trashParentGroup;

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

        public void TrySpawnCustomerTrash(Vector3 customerPosition)
        {
            // %15 ihtimalle dükkan zeminine küçük çöp/leke düşer
            if (Random.value > 0.15f) return;
            if (activeTrashItems.Count >= 6) return; // Azami 6 çöp lekesi sınırı

            Vector3 spawnPos = customerPosition + new Vector3(Random.Range(-0.4f, 0.4f), 0.02f, Random.Range(-0.4f, 0.4f));

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
