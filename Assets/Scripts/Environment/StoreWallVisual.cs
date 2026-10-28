using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>Presentation only: the original wall transform and collider never move.</summary>
    public sealed class StoreWallVisual : MonoBehaviour
    {
        public static int Mode { get; private set; }
        public static void SetMode(int mode) => Mode = Mathf.Clamp(mode, 0, 2);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMode() => Mode = 0;

        private Transform body, plinth, cap;
        private Material plaster, stone, facade;
        private Texture2D texture, facadeTexture;
        private Mesh surfaceMesh;
        private Vector3 size, outward;
        private float bottom, top, visibleTop;
        private bool facing;
        private Camera view;
        public float VisibleCutHeight => visibleTop;

        public void Initialize(Material original)
        {
            size = transform.localScale;
            bottom = transform.position.y - size.y * .5f;
            top = bottom + size.y;
            visibleTop = top;
            // Exterior segments and their doorway lintels share the same normal.
            if (name.StartsWith("Front_")) outward = Vector3.back;
            else if (name.StartsWith("Store_Wall_Left")) outward = Vector3.left;
            else if (name.StartsWith("Storage_Wall_Right") || name.StartsWith("Partition_Store"))
                outward = Vector3.right;
            else outward = Vector3.forward;

            plaster = new Material(original) { name = "Store_RenderOnly_Plaster" };
            plaster.color = Color.Lerp(original.color, new Color(.57f, .55f, .51f), .32f);
            texture = BuildPlaster();
            plaster.mainTexture = texture;
            plaster.mainTextureScale = new Vector2(Mathf.Max(size.x, size.z) * 2f, size.y * 2f);
            if (plaster.HasProperty("_Smoothness")) plaster.SetFloat("_Smoothness", .12f);
            if (plaster.HasProperty("_Glossiness")) plaster.SetFloat("_Glossiness", .12f);
            stone = new Material(original) { name = "Store_RenderOnly_Limestone" };
            stone.color = new Color(.49f, .47f, .42f);
            if (stone.HasProperty("_Smoothness")) stone.SetFloat("_Smoothness", .2f);

            Mesh cube = Farm2Shelf.Utils.PrimitiveFactory.GetReadableCubeMesh();
            body = Part("Wall_Surface", cube, plaster);
            bool exterior = !name.StartsWith("Partition_") && !name.StartsWith("Staff_Partition_");
            if (exterior)
            {
                facade = new Material(plaster) { name = "Store_Exterior_Masonry" };
                facadeTexture = BuildMasonry();
                facade.mainTexture = facadeTexture;
                facade.mainTextureScale = new Vector2(Mathf.Max(size.x, size.z) / 1.2f, size.y / .6f);
                // Split the existing cube's outside face; no overlapping skins or new colliders.
                surfaceMesh = Instantiate(cube);
                var inside = new System.Collections.Generic.List<int>();
                var outside = new System.Collections.Generic.List<int>();
                int[] triangles = cube.triangles;
                Vector3[] normals = cube.normals;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var face = Vector3.Dot(normals[triangles[i]], outward) > .5f ? outside : inside;
                    face.Add(triangles[i]); face.Add(triangles[i + 1]); face.Add(triangles[i + 2]);
                }
                surfaceMesh.subMeshCount = 2;
                surfaceMesh.SetTriangles(inside, 0);
                surfaceMesh.SetTriangles(outside, 1);
                body.GetComponent<MeshFilter>().sharedMesh = surfaceMesh;
                body.GetComponent<MeshRenderer>().sharedMaterials = new[] { plaster, facade };
            }
            plinth = Part("Masonry_Base", cube, stone);
            cap = Part("Masonry_Coping", cube, stone);
            // Only the legacy renderer is replaced, not its collider or mesh.
            GetComponent<MeshRenderer>().enabled = false;
            Draw(top);
        }

        public void SetInteriorColor(Color color)
        {
            if (plaster != null) plaster.color = color;
        }

        public void SetExteriorColor(Color color)
        {
            if (facade != null) facade.color = color;
        }

        private static Texture2D BuildMasonry()
        {
            const int n = 128;
            var result = new Texture2D(n, n, TextureFormat.RGB24, true)
            { name = "Dressed stone staggered joints", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int row = y / 64;
                    int jointX = (x + row * 64) % 128;
                    int jointY = y % 64;
                    bool mortar = jointX < 3 || jointY < 3;
                    bool bevel = jointX < 5 || jointY < 5;
                    float grain = Mathf.PerlinNoise(x * .37f, y * .37f) * .06f;
                    float value = mortar ? .49f : (bevel ? .72f : .91f + grain);
                    pixels[y * n + x] = new Color(value, value, value);
                }
            result.SetPixels(pixels);
            result.Apply(true, true);
            return result;
        }

        private Transform Part(string partName, Mesh mesh, Material material)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
            return part.transform;
        }

        private static Texture2D BuildPlaster()
        {
            // Deterministic fine mineral grain; does not consume gameplay Random state.
            const int n = 64;
            var result = new Texture2D(n, n, TextureFormat.RGB24, true)
            { name = "Fine mineral plaster", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var pixels = new Color[n * n];
            uint seed = 7319;
            for (int i = 0; i < pixels.Length; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                float grain = .87f + ((seed >> 16) & 255) / 255f * .13f;
                pixels[i] = new Color(grain, grain, grain);
            }
            result.SetPixels(pixels);
            result.Apply(true, true);
            return result;
        }

        private void LateUpdate()
        {
            if (body == null) return;
            if (view == null) view = Camera.main;
            if (view != null)
            {
                // Camera forward works for both orthographic and perspective views.
                Vector3 direction = -view.transform.forward;
                direction.y = 0;
                float dot = Vector3.Dot(direction.normalized, transform.TransformDirection(outward));
                // Hysteresis prevents flicker near a side-on camera angle.
                if (dot > .15f) facing = true;
                else if (dot < .05f) facing = false;
            }
            float target = Mode == 0 || !facing ? top : (Mode == 1 ? 1.75f : 0f);
            target = Mathf.Clamp(target, bottom, top);
            float next = Mathf.MoveTowards(visibleTop, target, Time.unscaledDeltaTime * 7f);
            if (Mathf.Abs(next - visibleTop) < .00001f) return;
            visibleTop = next;
            Draw(visibleTop);
        }

        private void Draw(float cutHeight)
        {
            float height = Mathf.Max(0, cutHeight - bottom);
            bool shown = height > .015f;
            body.gameObject.SetActive(shown);
            plinth.gameObject.SetActive(shown && bottom < .1f);
            cap.gameObject.SetActive(shown);
            if (!shown) return;
            float capHeight=Mathf.Min(.09f,height*.3f);
            float surfaceHeight=height-capHeight;
            // Separate the top planes: overlapping coplanar faces shimmer when panning.
            SetPart(body,bottom,surfaceHeight,0);
            SetPart(plinth,bottom,Mathf.Min(.28f,surfaceHeight),.025f);
            SetPart(cap,cutHeight-capHeight,capHeight,.035f);
            // Keep the grain / masonry scale tied to metres as the cutaway animates.
            // Scaling a cube with unchanged UV tiling made the wall texture slide.
            plaster.mainTextureScale=new Vector2(Mathf.Max(size.x,size.z)*2f,surfaceHeight*2f);
            if(facade!=null)facade.mainTextureScale=new Vector2(Mathf.Max(size.x,size.z)/1.2f,surfaceHeight/.6f);
        }

        private void SetPart(Transform part, float baseY, float height, float overhang)
        {
            // Convert world-size details into the unchanged scaled wall's local space.
            part.localPosition = new Vector3(0, (baseY + height * .5f - transform.position.y) / size.y, 0);
            part.localScale = new Vector3(
                1f + (size.x < size.z ? overhang * 2f / size.x : 0),
                height / size.y,
                1f + (size.z <= size.x ? overhang * 2f / size.z : 0));
        }

        private void OnDestroy()
        {
            if (plaster != null) Destroy(plaster);
            if (stone != null) Destroy(stone);
            if (texture != null) Destroy(texture);
            if (facade != null) Destroy(facade);
            if (facadeTexture != null) Destroy(facadeTexture);
            if (surfaceMesh != null) Destroy(surfaceMesh);
        }
    }
}
