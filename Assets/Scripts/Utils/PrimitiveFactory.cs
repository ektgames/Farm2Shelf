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
