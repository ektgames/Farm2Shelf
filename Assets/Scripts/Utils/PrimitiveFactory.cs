using UnityEngine;

namespace Farm2Shelf.Utils
{
    /// <summary>
    /// Collider üretmeden paylaşımlı primitive mesh ile görsel nesne oluşturur.
    /// CreatePrimitive + Destroy(Collider) döngüsünden kaçınır.
    /// </summary>
    public static class PrimitiveFactory
    {
        private static Mesh cubeMesh;
        private static Mesh sphereMesh;
        private static Mesh cylinderMesh;
        private static bool warmed;
        private static Mesh readableCubeMesh;

        // Built-in primitive CPU data is not guaranteed to survive player builds.
        // Cutaway visuals need a mesh we own, with CPU data retained on iOS too.
        public static Mesh GetReadableCubeMesh()
        {
            if (readableCubeMesh != null) return readableCubeMesh;
            var vertices = new Vector3[24];
            var normals = new Vector3[24];
            var uv = new Vector2[24];
            var triangles = new int[36];
            Vector3[] faces = { Vector3.forward, Vector3.back, Vector3.right,
                Vector3.left, Vector3.up, Vector3.down };
            for (int face = 0; face < faces.Length; face++)
            {
                Vector3 normal = faces[face];
                Vector3 right = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.Cross(Vector3.up, normal);
                Vector3 up = Vector3.Cross(normal, right);
                int v = face * 4, t = face * 6;
                vertices[v] = (normal - right - up) * .5f;
                vertices[v + 1] = (normal + right - up) * .5f;
                vertices[v + 2] = (normal + right + up) * .5f;
                vertices[v + 3] = (normal - right + up) * .5f;
                for (int j = 0; j < 4; j++) normals[v + j] = normal;
                uv[v] = Vector2.zero; uv[v + 1] = Vector2.right;
                uv[v + 2] = Vector2.one; uv[v + 3] = Vector2.up;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
            readableCubeMesh = new Mesh { name = "Farm2Shelf_Readable_Cube" };
            readableCubeMesh.vertices = vertices;
            readableCubeMesh.normals = normals;
            readableCubeMesh.uv = uv;
            readableCubeMesh.triangles = triangles;
            readableCubeMesh.RecalculateBounds();
            return readableCubeMesh;
        }

        public static void Warmup()
        {
            if (warmed) return;
            cubeMesh = ExtractSharedMesh(PrimitiveType.Cube);
            sphereMesh = ExtractSharedMesh(PrimitiveType.Sphere);
            cylinderMesh = ExtractSharedMesh(PrimitiveType.Cylinder);
            warmed = cubeMesh != null;
        }

        public static GameObject CreateVisualCube(string name, Transform parent)
        {
            return CreateVisual(PrimitiveType.Cube, name, parent);
        }

        public static GameObject CreateVisual(PrimitiveType type, string name, Transform parent)
        {
            Warmup();
            GameObject go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = GetMesh(type);
            go.AddComponent<MeshRenderer>();
            return go;
        }

        private static Mesh GetMesh(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Sphere: return sphereMesh;
                case PrimitiveType.Cylinder: return cylinderMesh;
                default: return cubeMesh;
            }
        }

        private static Mesh ExtractSharedMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>() != null ? temp.GetComponent<MeshFilter>().sharedMesh : null;
            if (Application.isPlaying) Object.Destroy(temp);
            else Object.DestroyImmediate(temp);
            return mesh;
        }
    }
}
