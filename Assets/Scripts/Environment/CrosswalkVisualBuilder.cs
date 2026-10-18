using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Farm2Shelf.Environment
{
    /// <summary>Shared road paint geometry. Local X crosses the road; Z follows traffic.</summary>
    public static class CrosswalkVisualBuilder
    {
        private const float StripeWidth = .5f;
        private const float StripeGap = .5f;
        private const float CrossingWidth = 2.4f;
        private static readonly Dictionary<int, Mesh> meshes = new Dictionary<int, Mesh>();
        private static Material paint;

        // Only road arms with pavement at both ends are listed here. Existing
        // crossings remain owned by their district builders.
        public static void CompleteMapConnections(Transform environment)
        {
            var root = new GameObject("Additional_Pedestrian_Crossings").transform;
            root.SetParent(environment, false);
            foreach (float x in new[] { -75f, 75f })
            {
                Create(root,"Ring_North_Approach",new Vector3(x,0,45.5f),true);
                Create(root,"Ring_Highway_North",new Vector3(x,0,-4.5f),true);
                Create(root,"Ring_Highway_South",new Vector3(x,0,-13.5f),true);
                Create(root,"Ring_South_North",new Vector3(x,0,-50.5f),true);
                Create(root,"Ring_South_South",new Vector3(x,0,-59.3f),true);
                Create(root,"South_End_Approach",new Vector3(x,0,-123.7f),true);
            }
            // Town-hall/courtyard junction, kept away from the central avenue.
            foreach (float x in new[] { -70.5f, -4.5f, 4.5f, 70.5f })
            {
                Create(root,"Townhall_South_Crossing",new Vector3(x,0,-55f),false);
                Create(root,"South_Perimeter_Crossing",new Vector3(x,0,-128f),false);
            }
            Create(root,"South_Middle_Exit",new Vector3(0,0,-123.7f),true);
            // Farm and northern neighbourhood: link sidewalks across the east/west roads.
            foreach (float x in new[] { -70.5f, -42f, -33f, -4.5f, 4.5f, 33f, 42f, 70.5f, 79.5f, 108f, 117f, 145.5f })
                Create(root,"North_Street_Crossing",new Vector3(x,0,50f),false);
            foreach (float x in new[] { -70.5f, -42f, -33f, -4.5f, 4.5f, 33f, 42f, 70.5f, 79.5f, 108f, 117f, 145.5f })
                Create(root,"Top_Street_Crossing",new Vector3(x,0,175f),false);
        }

        public static void ClearOverlappingLaneMarkings(Transform environment)
        {
            var crossings = new List<Bounds>();
            foreach (var mf in environment.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Crosswalk_Shared_"))
                    crossings.Add(mf.GetComponent<Renderer>().bounds);
            foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>())
            {
                string n = renderer.name;
                bool lane = n == "Road_Line" || n == "Road_Center_Line" || n.StartsWith("Lane_Stripe_") ||
                    ((n.StartsWith("NE_") || n.StartsWith("SE_")) && n.EndsWith("_Line"));
                if (!lane) continue;
                Bounds b = renderer.bounds;
                foreach (Bounds c in crossings)
                    if (b.max.x > c.min.x-.1f && b.min.x < c.max.x+.1f &&
                        b.max.z > c.min.z-.1f && b.min.z < c.max.z+.1f)
                    {
                        renderer.gameObject.SetActive(false);
                        if (Application.isPlaying) Object.Destroy(renderer.gameObject);
                        else Object.DestroyImmediate(renderer.gameObject);
                        break;
                    }
            }
        }

        public static GameObject Create(Transform parent, string name, Vector3 center,
            bool acrossVerticalRoad, float roadWidth = 6f)
        {
            // Leave equal margins at both kerbs. Narrow service entrances use fewer
            // stripes, never stretched paint or different stripe spacing.
            int count = Mathf.Max(1, Mathf.FloorToInt((roadWidth - .3f + StripeGap) / (StripeWidth + StripeGap)));
            if (!meshes.TryGetValue(count, out Mesh mesh) || mesh == null)
            {
                var vertices = new List<Vector3>(count * 4);
                var triangles = new List<int>(count * 6);
                float span = count * StripeWidth + (count - 1) * StripeGap;
                for (int i = 0; i < count; i++)
                {
                    float x = -span * .5f + i * (StripeWidth + StripeGap);
                    int v = vertices.Count;
                    vertices.Add(new Vector3(x, 0, -CrossingWidth * .5f));
                    vertices.Add(new Vector3(x, 0, CrossingWidth * .5f));
                    vertices.Add(new Vector3(x + StripeWidth, 0, CrossingWidth * .5f));
                    vertices.Add(new Vector3(x + StripeWidth, 0, -CrossingWidth * .5f));
                    triangles.AddRange(new[] { v, v + 1, v + 2, v, v + 2, v + 3 });
                }
                mesh = new Mesh { name = "Crosswalk_Shared_" + count };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.UploadMeshData(true);
                meshes[count] = mesh;
            }
            if (paint == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
                paint = new Material(shader) { name = "CrosswalkMat_Standard", color = new Color(.92f, .92f, .88f), enableInstancing = true };
                if (paint.HasProperty("_BaseColor")) paint.SetColor("_BaseColor", paint.color);
                if (paint.HasProperty("_Smoothness")) paint.SetFloat("_Smoothness", .15f);
                if (paint.HasProperty("_Glossiness")) paint.SetFloat("_Glossiness", .15f);
                if (paint.HasProperty("_Metallic")) paint.SetFloat("_Metallic", 0);
            }
            var crossing = new GameObject(name);
            crossing.transform.SetParent(parent, false);
            crossing.transform.position = new Vector3(center.x, .026f, center.z);
            crossing.transform.rotation = Quaternion.Euler(0, acrossVerticalRoad ? 0 : 90, 0);
            crossing.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = crossing.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = paint;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            // Paint has no collider, so it cannot intercept clicks or affect routes.
            return crossing;
        }
    }
}
