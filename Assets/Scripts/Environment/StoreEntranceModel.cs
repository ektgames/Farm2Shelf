using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Farm2Shelf.Environment
{
    /// <summary>Render-only fittings on the existing sliding door leaves.</summary>
    public sealed class StoreEntranceModel : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        private Mesh cube;
        private bool alongX;

        public void Build(Mesh mesh, Material source, Transform left, Transform right,
            bool slideAlongX, float width, bool storefront)
        {
            cube = Farm2Shelf.Utils.PrimitiveFactory.GetReadableCubeMesh();
            alongX = slideAlongX;
            Material aluminium = Finish(source, "Brushed aluminium", new Color(.57f, .62f, .66f), .75f, .62f);
            Material seal = Finish(source, "Door rubber seals", new Color(.035f, .045f, .05f), 0, .2f);
            Material panel = Finish(source, "Warehouse powder coat", new Color(.38f, .46f, .49f), .35f, .4f);
            Material glass = Finish(source, "Entrance glazing", new Color(.46f, .73f, .79f, .32f), .05f, .9f);
            MakeTransparent(glass);
            Material frost = Finish(source, "Glass safety band", new Color(.83f, .91f, .92f), .05f, .4f);
            Material green = Finish(source, "Door sensor indicator", new Color(.15f, .85f, .46f), 0, .45f);

            // Jambs lie on the existing wall edge, keeping the full doorway clear.
            foreach (int sign in new[] { -1, 1 })
                Part(transform, "Entrance_Jamb", sign * (width * .5f + .045f), 1.28f, 0,
                    .09f, 2.56f, .28f, aluminium);
            Part(transform, "Automatic_Drive_Header", 0, 2.6f, 0, width + .18f, .20f, .34f, aluminium);
            Part(transform, "Header_Inset", 0, 2.6f, -.176f, width - .12f, .065f, .018f, seal);
            Part(transform, "Presence_Sensor", 0, 2.57f, -.205f, .20f, .075f, .07f, seal);
            Part(transform, "Sensor_LED", .055f, 2.57f, -.245f, .035f, .02f, .012f, green);
            Part(transform, "Flush_Threshold", 0, .018f, 0, width, .025f, .32f, aluminium);

            Transform[] leaves = { left, right };
            for (int i = 0; i < leaves.Length; i++)
            {
                Transform leaf = leaves[i];
                float w = width * .5f - .025f;
                // Leaves retain the old movement pivots at y=1.25.
                foreach (int sign in new[] { -1, 1 })
                    Part(leaf, "Leaf_Stile", sign * (w * .5f - .035f), 0, 0, .07f, 2.5f, .12f, aluminium);
                Part(leaf, "Leaf_UpperRail", 0, 1.21f, 0, w, .08f, .12f, aluminium);
                Part(leaf, "Leaf_LowerRail", 0, -1.17f, 0, w, .16f, .12f, aluminium);
                Part(leaf, "Closing_Seal", (i == 0 ? 1 : -1) * w * .5f, 0, 0, .018f, 2.48f, .09f, seal);
                if (storefront)
                {
                    Part(leaf, "Clear_Glass", 0, .02f, 0, w - .14f, 2.30f, .025f, glass);
                    Part(leaf, "Safety_Band", 0, -.12f, 0, w - .14f, .09f, .03f, frost);
                    Part(leaf, "Safety_Band_Thin", 0, -.26f, 0, w - .14f, .025f, .03f, frost);
                }
                else
                {
                    Part(leaf, "Lower_Steel_Panel", 0, -.50f, 0, w - .14f, 1.18f, .08f, panel);
                    Part(leaf, "Upper_Steel_Panel", 0, 1.04f, 0, w - .14f, .26f, .08f, panel);
                    float windowW = w * .52f;
                    foreach (int sign in new[] { -1, 1 })
                    {
                        Part(leaf, "Window_Frame_Side", sign * (windowW * .5f + .0175f), .50f, 0,
                            .035f, .84f, .11f, seal);
                        Part(leaf, "Window_Frame_Rail", 0, .50f + sign * .40f, 0,
                            windowW, .04f, .11f, seal);
                    }
                    Part(leaf, "Inspection_Window", 0, .50f, 0, windowW, .76f, .025f, glass);
                    float sideW = (w - .14f - windowW - .07f) * .5f;
                    foreach (int sign in new[] { -1, 1 })
                        Part(leaf, "Window_Side_Panel", sign * (windowW * .5f + .035f + sideW * .5f),
                            .50f, 0, sideW, .84f, .08f, panel);
                    Part(leaf, "Impact_Plate", 0, -.94f, -.055f, w - .22f, .24f, .025f, aluminium);
                    Part(leaf, "Impact_Plate_Back", 0, -.94f, .055f, w - .22f, .24f, .025f, aluminium);
                }
            }
        }

        private void Part(Transform parent, string partName, float u, float y, float depth,
            float width, float height, float thickness, Material material)
        {
            var go = new GameObject(partName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = alongX ? new Vector3(u, y, depth) : new Vector3(depth, y, u);
            go.transform.localScale = alongX ? new Vector3(width, height, thickness) : new Vector3(thickness, height, width);
            go.GetComponent<MeshFilter>().sharedMesh = cube;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private Material Finish(Material source, string label, Color color, float metal, float smooth)
        {
            var material = new Material(source) { name = label, color = color };
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metal);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smooth);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smooth);
            materials.Add(material);
            return material;
        }

        private static void MakeTransparent(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1);
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
        }

        private void OnDestroy()
        {
            foreach (Material material in materials)
                if (material != null) Destroy(material);
        }
    }
}
