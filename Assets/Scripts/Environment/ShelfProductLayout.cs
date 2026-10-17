using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    // Authored from the imported mesh's four physical shelf surfaces, bottom to top.
    public sealed class ShelfProductLayout : MonoBehaviour
    {
        public Vector3[] cells = new Vector3[40];
        public Vector3[] cellSizes = new Vector3[4];

        public void GetPositions(int row, int stock, int capacity, List<Vector3> positions)
        {
            if (row < 0 || row >= 4) return;
            int count = Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(stock / (float)Mathf.Max(1, capacity)) * 10f), stock > 0 ? 1 : 0, 10);
            for (int i = 0; i < count; i++) positions.Add(cells[row * 10 + i]);
        }

        public void FitProduct(Transform product, int row, Vector3 position)
        {
            if (product == null || row < 0 || row >= 4) return;
            // Measure in row-container coordinates, so rotated shelves keep identical spacing.
            bool found = false;
            bool authoredDisplay = false;
            Bounds bounds = default;
            foreach (MeshFilter filter in product.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                authoredDisplay |= filter.sharedMesh.name.StartsWith("Hand designed product ", System.StringComparison.Ordinal);
                Bounds mesh = filter.sharedMesh.bounds;
                Matrix4x4 matrix = product.parent.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(new Vector3(
                        (corner & 1) == 0 ? mesh.min.x : mesh.max.x,
                        (corner & 2) == 0 ? mesh.min.y : mesh.max.y,
                        (corner & 4) == 0 ? mesh.min.z : mesh.max.z));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!found) return;
            Vector3 available = cellSizes[row];
            // Let the new display models occupy more of their own cell, preserving proportions.
            // Legacy/storage visuals retain their original shrink-only behaviour.
            if (authoredDisplay) available *= .94f;
            float factor = Mathf.Min(authoredDisplay ? 1.6f : 1f, available.x / Mathf.Max(0.001f, bounds.size.x),
                available.y / Mathf.Max(0.001f, bounds.size.y), available.z / Mathf.Max(0.001f, bounds.size.z));
            Vector3 pivot = product.localPosition;
            Vector3 center = pivot + (bounds.center - pivot) * factor;
            float bottom = pivot.y + (bounds.min.y - pivot.y) * factor;
            product.localScale *= factor;
            product.localPosition += new Vector3(position.x - center.x, position.y - bottom, position.z - center.z);
        }

        public Vector3 GetLabelPosition(int row)
        {
            Vector3 center = cells[Mathf.Clamp(row, 0, 3) * 10 + 2];
            return center + new Vector3(0f, -0.025f, -cellSizes[row].z * 0.65f);
        }
    }
}
