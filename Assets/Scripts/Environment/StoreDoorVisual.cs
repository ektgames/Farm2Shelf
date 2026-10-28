using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Clips only door render meshes to their wall's animated height.
    /// Door leaves, triggers and the automatic opening controller stay untouched.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class StoreDoorVisual : MonoBehaviour
    {
        private sealed class Surface
        {
            public Renderer renderer;
            public MeshFilter filter;
            public Mesh original, visual;
            public Vector3[] vertices, clipped;
            public bool wasForcedOff;
        }

        private readonly List<Surface> surfaces = new List<Surface>();
        private StoreWallVisual wall;

        public void Initialize(StoreWallVisual connectedWall)
        {
            wall = connectedWall;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var surface = new Surface { renderer = renderer, wasForcedOff = renderer.forceRenderingOff };
                // TextMesh owns its mesh; it is hidden with the doorway instead of rewritten.
                var filter = renderer.GetComponent<MeshFilter>();
                if (renderer.GetComponent<TextMesh>() == null && filter != null && filter.sharedMesh != null)
                {
                    surface.filter = filter;
                    surface.original = filter.sharedMesh;
                    surface.visual = Instantiate(surface.original);
                    surface.visual.name = "Door_RenderOnly_Cutaway";
                    surface.visual.MarkDynamic();
                    surface.vertices = surface.original.vertices;
                    surface.clipped = new Vector3[surface.vertices.Length];
                    filter.sharedMesh = surface.visual;
                }
                surfaces.Add(surface);
            }
        }

        private void LateUpdate()
        {
            if (wall == null) return;
            float height = wall.VisibleCutHeight;
            foreach (var surface in surfaces)
            {
                if (surface.renderer == null) continue;
                if (surface.visual == null)
                {
                    surface.renderer.forceRenderingOff = surface.wasForcedOff ||
                        surface.renderer.bounds.max.y > height + .01f;
                    continue;
                }

                Transform t = surface.filter.transform;
                float lowest = float.PositiveInfinity;
                for (int i = 0; i < surface.vertices.Length; i++)
                {
                    Vector3 world = t.TransformPoint(surface.vertices[i]);
                    lowest = Mathf.Min(lowest, world.y);
                    world.y = Mathf.Min(world.y, height);
                    surface.clipped[i] = t.InverseTransformPoint(world);
                }
                surface.renderer.forceRenderingOff = surface.wasForcedOff || height <= lowest + .015f;
                surface.visual.vertices = surface.clipped;
                surface.visual.RecalculateBounds();
            }
        }

        private void OnDisable()
        {
            foreach (var surface in surfaces)
            {
                if (surface.renderer != null) surface.renderer.forceRenderingOff = surface.wasForcedOff;
                if (surface.visual != null)
                {
                    surface.visual.vertices = surface.vertices;
                    surface.visual.RecalculateBounds();
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var surface in surfaces)
            {
                if (surface.filter != null) surface.filter.sharedMesh = surface.original;
                if (surface.visual != null) Destroy(surface.visual);
            }
        }
    }
}
