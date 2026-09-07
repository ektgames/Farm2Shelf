using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    public static class LivestockModelBuilder
    {
        public static GameObject BuildAnimal(LivestockType type, Transform parent)
        {
            GameObject root = new GameObject("Animal_" + type);
            if (parent != null) root.transform.SetParent(parent, false);

            switch (type)
            {
                case LivestockType.WhiteChicken:
                    BuildChicken(root.transform, new Color(0.96f, 0.95f, 0.90f), new Color(0.92f, 0.90f, 0.82f));
                    break;
                case LivestockType.BlackChicken:
                    BuildChicken(root.transform, new Color(0.10f, 0.10f, 0.12f), new Color(0.22f, 0.18f, 0.16f));
                    break;
                case LivestockType.HolsteinCow:
                    BuildCow(root.transform, new Color(0.95f, 0.95f, 0.93f), true);
                    break;
                default:
                    BuildCow(root.transform, new Color(0.55f, 0.32f, 0.16f), false);
                    break;
            }

            return root;
        }

        private static void BuildChicken(Transform parent, Color bodyCol, Color wingCol)
        {
            Color combCol = new Color(0.86f, 0.12f, 0.14f);
            Color beakCol = new Color(0.95f, 0.62f, 0.12f);
            Color legCol = new Color(0.92f, 0.68f, 0.18f);

            AddPart(parent, "Body", PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0f), new Vector3(0.34f, 0.28f, 0.42f), bodyCol);
            AddPart(parent, "Breast", PrimitiveType.Sphere, new Vector3(0f, 0.22f, 0.10f), new Vector3(0.28f, 0.24f, 0.28f), bodyCol);
            AddPart(parent, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.48f, 0.16f), new Vector3(0.20f, 0.20f, 0.20f), bodyCol);
            AddPart(parent, "Beak", PrimitiveType.Cube, new Vector3(0f, 0.46f, 0.28f), new Vector3(0.06f, 0.05f, 0.10f), beakCol);
            AddPart(parent, "Comb", PrimitiveType.Cube, new Vector3(0f, 0.60f, 0.16f), new Vector3(0.04f, 0.10f, 0.12f), combCol);
            AddPart(parent, "Wattle", PrimitiveType.Cube, new Vector3(0f, 0.40f, 0.24f), new Vector3(0.04f, 0.08f, 0.04f), combCol);
            AddPart(parent, "Eye_L", PrimitiveType.Sphere, new Vector3(-0.07f, 0.51f, 0.22f), new Vector3(0.04f, 0.04f, 0.04f), Color.black);
            AddPart(parent, "Eye_R", PrimitiveType.Sphere, new Vector3(0.07f, 0.51f, 0.22f), new Vector3(0.04f, 0.04f, 0.04f), Color.black);
            AddPart(parent, "Wing_L", PrimitiveType.Sphere, new Vector3(-0.18f, 0.28f, 0f), new Vector3(0.10f, 0.18f, 0.26f), wingCol);
            AddPart(parent, "Wing_R", PrimitiveType.Sphere, new Vector3(0.18f, 0.28f, 0f), new Vector3(0.10f, 0.18f, 0.26f), wingCol);
            AddPart(parent, "Tail", PrimitiveType.Sphere, new Vector3(0f, 0.36f, -0.22f), new Vector3(0.16f, 0.22f, 0.12f), wingCol);
            AddPart(parent, "Leg_L", PrimitiveType.Cylinder, new Vector3(-0.07f, 0.10f, 0.02f), new Vector3(0.05f, 0.10f, 0.05f), legCol);
            AddPart(parent, "Leg_R", PrimitiveType.Cylinder, new Vector3(0.07f, 0.10f, 0.02f), new Vector3(0.05f, 0.10f, 0.05f), legCol);
            AddPart(parent, "Foot_L", PrimitiveType.Cube, new Vector3(-0.07f, 0.02f, 0.04f), new Vector3(0.08f, 0.03f, 0.12f), legCol);
            AddPart(parent, "Foot_R", PrimitiveType.Cube, new Vector3(0.07f, 0.02f, 0.04f), new Vector3(0.08f, 0.03f, 0.12f), legCol);
        }

        private static void BuildCow(Transform parent, Color bodyCol, bool holsteinSpots)
        {
            Color snoutCol = new Color(0.78f, 0.52f, 0.48f);
            Color hoofCol = new Color(0.12f, 0.10f, 0.09f);
            Color hornCol = new Color(0.90f, 0.86f, 0.72f);
            Color udderCol = new Color(0.92f, 0.72f, 0.72f);
            Color spotCol = new Color(0.08f, 0.08f, 0.09f);

            AddPart(parent, "Body", PrimitiveType.Sphere, new Vector3(0f, 0.72f, 0f), new Vector3(0.70f, 0.58f, 1.15f), bodyCol);
            AddPart(parent, "Shoulder", PrimitiveType.Sphere, new Vector3(0f, 0.78f, 0.32f), new Vector3(0.62f, 0.52f, 0.55f), bodyCol);
            AddPart(parent, "Hip", PrimitiveType.Sphere, new Vector3(0f, 0.76f, -0.38f), new Vector3(0.64f, 0.50f, 0.52f), bodyCol);
            AddPart(parent, "Neck", PrimitiveType.Sphere, new Vector3(0f, 0.88f, 0.52f), new Vector3(0.32f, 0.28f, 0.28f), bodyCol);
            AddPart(parent, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.98f, 0.72f), new Vector3(0.34f, 0.30f, 0.32f), bodyCol);
            AddPart(parent, "Snout", PrimitiveType.Sphere, new Vector3(0f, 0.88f, 0.90f), new Vector3(0.22f, 0.16f, 0.18f), snoutCol);
            AddPart(parent, "Nostril_L", PrimitiveType.Sphere, new Vector3(-0.05f, 0.88f, 0.98f), new Vector3(0.04f, 0.03f, 0.03f), Color.black);
            AddPart(parent, "Nostril_R", PrimitiveType.Sphere, new Vector3(0.05f, 0.88f, 0.98f), new Vector3(0.04f, 0.03f, 0.03f), Color.black);
            AddPart(parent, "Eye_L", PrimitiveType.Sphere, new Vector3(-0.12f, 1.05f, 0.80f), new Vector3(0.06f, 0.06f, 0.05f), Color.black);
            AddPart(parent, "Eye_R", PrimitiveType.Sphere, new Vector3(0.12f, 1.05f, 0.80f), new Vector3(0.06f, 0.06f, 0.05f), Color.black);
            AddPart(parent, "Ear_L", PrimitiveType.Sphere, new Vector3(-0.22f, 1.08f, 0.68f), new Vector3(0.08f, 0.14f, 0.06f), bodyCol);
            AddPart(parent, "Ear_R", PrimitiveType.Sphere, new Vector3(0.22f, 1.08f, 0.68f), new Vector3(0.08f, 0.14f, 0.06f), bodyCol);
            AddPart(parent, "Horn_L", PrimitiveType.Cylinder, new Vector3(-0.12f, 1.20f, 0.68f), new Vector3(0.05f, 0.10f, 0.05f), hornCol);
            AddPart(parent, "Horn_R", PrimitiveType.Cylinder, new Vector3(0.12f, 1.20f, 0.68f), new Vector3(0.05f, 0.10f, 0.05f), hornCol);
            AddPart(parent, "Udder", PrimitiveType.Sphere, new Vector3(0f, 0.42f, -0.12f), new Vector3(0.28f, 0.20f, 0.32f), udderCol);
            AddPart(parent, "Tail", PrimitiveType.Cylinder, new Vector3(0f, 0.85f, -0.62f), new Vector3(0.05f, 0.28f, 0.05f), bodyCol);
            AddPart(parent, "TailTuft", PrimitiveType.Sphere, new Vector3(0f, 0.55f, -0.66f), new Vector3(0.10f, 0.12f, 0.10f), Color.black);

            AddPart(parent, "Leg_FL", PrimitiveType.Cylinder, new Vector3(-0.18f, 0.32f, 0.32f), new Vector3(0.12f, 0.32f, 0.12f), bodyCol);
            AddPart(parent, "Leg_FR", PrimitiveType.Cylinder, new Vector3(0.18f, 0.32f, 0.32f), new Vector3(0.12f, 0.32f, 0.12f), bodyCol);
            AddPart(parent, "Leg_BL", PrimitiveType.Cylinder, new Vector3(-0.18f, 0.32f, -0.32f), new Vector3(0.12f, 0.32f, 0.12f), bodyCol);
            AddPart(parent, "Leg_BR", PrimitiveType.Cylinder, new Vector3(0.18f, 0.32f, -0.32f), new Vector3(0.12f, 0.32f, 0.12f), bodyCol);
            AddPart(parent, "Hoof_FL", PrimitiveType.Cube, new Vector3(-0.18f, 0.04f, 0.32f), new Vector3(0.12f, 0.06f, 0.14f), hoofCol);
            AddPart(parent, "Hoof_FR", PrimitiveType.Cube, new Vector3(0.18f, 0.04f, 0.32f), new Vector3(0.12f, 0.06f, 0.14f), hoofCol);
            AddPart(parent, "Hoof_BL", PrimitiveType.Cube, new Vector3(-0.18f, 0.04f, -0.32f), new Vector3(0.12f, 0.06f, 0.14f), hoofCol);
            AddPart(parent, "Hoof_BR", PrimitiveType.Cube, new Vector3(0.18f, 0.04f, -0.32f), new Vector3(0.12f, 0.06f, 0.14f), hoofCol);

            if (holsteinSpots)
            {
                AddPart(parent, "Spot_A", PrimitiveType.Sphere, new Vector3(-0.18f, 0.86f, 0.10f), new Vector3(0.28f, 0.22f, 0.32f), spotCol);
                AddPart(parent, "Spot_B", PrimitiveType.Sphere, new Vector3(0.22f, 0.80f, -0.22f), new Vector3(0.30f, 0.24f, 0.28f), spotCol);
                AddPart(parent, "Spot_C", PrimitiveType.Sphere, new Vector3(0.05f, 0.92f, 0.30f), new Vector3(0.18f, 0.16f, 0.20f), spotCol);
            }
        }

        private static void AddPart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
        {
            GameObject go = PrimitiveFactory.CreateVisual(type, name, parent);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            MeshRenderer rend = go.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sharedMaterial = Procedural3DProductBuilder.CreateMaterial(color, 0.05f, 0.35f);
            }
        }
    }
}
