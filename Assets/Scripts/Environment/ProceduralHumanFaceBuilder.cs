using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Low-poly küp tabanlı yüz, saç ve sakal detayları.
    /// Head / limb pivot / collider hiyerarşisine dokunmaz; parçalar Head ve root altına eklenir.
    /// </summary>
    public static class ProceduralHumanFaceBuilder
    {
        private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        public enum HairStyle
        {
            ShortMale,
            LongFemale,
            PonytailFemale,
            ElderlyMale,
            ElderlyFemale,
            WavyMale,
            ModelFemale,
            CoveredShort
        }

        public struct FaceSettings
        {
            public Material Skin;
            public Material Hair;
            public bool IsFemale;
            public bool HasBeard;
            public bool HasMustache;
            public bool HasLipstick;
            public bool SkipHair;
            public bool SkipFace;
            public HairStyle HairStyle;
            public int Variant;
        }

        private static Material GetMaterial(string name, Color color, float metallic = 0.05f, float smoothness = 0.35f)
        {
            if (matCache.TryGetValue(name, out Material mat) && mat != null)
            {
                return mat;
            }

            Shader shader = ShaderHelper.GetLitShader();
            if (shader == null) shader = Shader.Find("Standard");

            Material newMat = new Material(shader)
            {
                name = name,
                color = color
            };

            if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", color);
            if (newMat.HasProperty("_Color")) newMat.SetColor("_Color", color);
            if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", metallic);
            if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", smoothness);

            matCache[name] = newMat;
            return newMat;
        }

        public static void BuildOnCharacter(Transform head, Transform characterRoot, FaceSettings settings)
        {
            if (head == null) return;

            if (!settings.SkipFace)
            {
                BuildFace(head, settings);
            }

            if (!settings.SkipHair && characterRoot != null)
            {
                BuildHair(characterRoot, settings);
            }
        }

        private static void BuildFace(Transform head, FaceSettings settings)
        {
            Material skin = settings.Skin;
            Material hair = settings.Hair;
            Material eyeWhite = GetMaterial("Mat_FaceEyeWhite", new Color(0.96f, 0.96f, 0.98f), 0.05f, 0.55f);
            Material iris = PickIris(settings.Variant);
            Material pupil = GetMaterial("Mat_FacePupil", new Color(0.05f, 0.05f, 0.07f), 0.1f, 0.2f);
            Material brow = hair != null ? hair : GetMaterial("Mat_FaceBrow", new Color(0.18f, 0.12f, 0.08f));
            Material innerEar = GetMaterial("Mat_FaceInnerEar", new Color(0.90f, 0.62f, 0.55f));
            Material lipNatural = GetMaterial("Mat_FaceLipNatural", new Color(0.78f, 0.42f, 0.42f), 0.05f, 0.45f);
            Material lipstick = settings.HasLipstick
                ? PickLipstick(settings.Variant)
                : lipNatural;
            Material lash = GetMaterial("Mat_FaceLash", new Color(0.08f, 0.07f, 0.08f));

            // Gözler (beyaz + iris + pupil)
            AddHeadPart(head, "EyeWhite_L", new Vector3(-0.075f, 0.045f, 0.155f), new Vector3(0.085f, 0.055f, 0.028f), eyeWhite);
            AddHeadPart(head, "EyeWhite_R", new Vector3(0.075f, 0.045f, 0.155f), new Vector3(0.085f, 0.055f, 0.028f), eyeWhite);
            AddHeadPart(head, "Iris_L", new Vector3(-0.075f, 0.045f, 0.172f), new Vector3(0.048f, 0.048f, 0.018f), iris);
            AddHeadPart(head, "Iris_R", new Vector3(0.075f, 0.045f, 0.172f), new Vector3(0.048f, 0.048f, 0.018f), iris);
            AddHeadPart(head, "Pupil_L", new Vector3(-0.075f, 0.045f, 0.184f), new Vector3(0.022f, 0.022f, 0.012f), pupil);
            AddHeadPart(head, "Pupil_R", new Vector3(0.075f, 0.045f, 0.184f), new Vector3(0.022f, 0.022f, 0.012f), pupil);

            // Kaşlar
            float browTilt = settings.IsFemale ? 8f : 0f;
            AddHeadPart(head, "Brow_L", new Vector3(-0.078f, 0.105f, 0.152f), new Vector3(0.10f, 0.022f, 0.024f), brow, new Vector3(0f, 0f, browTilt));
            AddHeadPart(head, "Brow_R", new Vector3(0.078f, 0.105f, 0.152f), new Vector3(0.10f, 0.022f, 0.024f), brow, new Vector3(0f, 0f, -browTilt));

            if (settings.IsFemale)
            {
                AddHeadPart(head, "Lash_L", new Vector3(-0.075f, 0.072f, 0.168f), new Vector3(0.09f, 0.012f, 0.016f), lash);
                AddHeadPart(head, "Lash_R", new Vector3(0.075f, 0.072f, 0.168f), new Vector3(0.09f, 0.012f, 0.016f), lash);
            }

            // Burun (köprü + uç + burun delikleri)
            AddHeadPart(head, "NoseBridge", new Vector3(0f, 0.02f, 0.168f), new Vector3(0.04f, 0.08f, 0.05f), skin);
            AddHeadPart(head, "NoseTip", new Vector3(0f, -0.02f, 0.195f), new Vector3(0.055f, 0.045f, 0.05f), skin);
            AddHeadPart(head, "Nostril_L", new Vector3(-0.018f, -0.035f, 0.188f), new Vector3(0.018f, 0.016f, 0.02f), GetMaterial("Mat_FaceNostril", new Color(0.55f, 0.32f, 0.28f)));
            AddHeadPart(head, "Nostril_R", new Vector3(0.018f, -0.035f, 0.188f), new Vector3(0.018f, 0.016f, 0.02f), GetMaterial("Mat_FaceNostril", new Color(0.55f, 0.32f, 0.28f)));

            // Dudaklar
            AddHeadPart(head, "LipUpper", new Vector3(0f, -0.095f, 0.168f), new Vector3(0.12f, 0.022f, 0.03f), lipstick);
            AddHeadPart(head, "LipLower", new Vector3(0f, -0.118f, 0.165f), new Vector3(0.11f, 0.024f, 0.032f), lipstick);

            // Kulaklar
            AddHeadPart(head, "Ear_L", new Vector3(-0.175f, 0.01f, -0.01f), new Vector3(0.045f, 0.12f, 0.07f), skin);
            AddHeadPart(head, "Ear_R", new Vector3(0.175f, 0.01f, -0.01f), new Vector3(0.045f, 0.12f, 0.07f), skin);
            AddHeadPart(head, "EarInner_L", new Vector3(-0.178f, 0.01f, 0.01f), new Vector3(0.02f, 0.07f, 0.04f), innerEar);
            AddHeadPart(head, "EarInner_R", new Vector3(0.178f, 0.01f, 0.01f), new Vector3(0.02f, 0.07f, 0.04f), innerEar);

            if (settings.HasMustache && !settings.IsFemale)
            {
                AddHeadPart(head, "Mustache", new Vector3(0f, -0.072f, 0.185f), new Vector3(0.14f, 0.028f, 0.04f), hair);
            }

            if (settings.HasBeard && !settings.IsFemale)
            {
                AddHeadPart(head, "BeardChin", new Vector3(0f, -0.155f, 0.12f), new Vector3(0.18f, 0.10f, 0.16f), hair);
                AddHeadPart(head, "BeardJaw_L", new Vector3(-0.12f, -0.12f, 0.06f), new Vector3(0.08f, 0.12f, 0.14f), hair);
                AddHeadPart(head, "BeardJaw_R", new Vector3(0.12f, -0.12f, 0.06f), new Vector3(0.08f, 0.12f, 0.14f), hair);
            }
        }

        private static void BuildHair(Transform root, FaceSettings settings)
        {
            Material hair = settings.Hair;
            if (hair == null) return;

            switch (settings.HairStyle)
            {
                case HairStyle.LongFemale:
                    CreateBlock(root, "Hair_Top", new Vector3(0f, 1.70f, -0.02f), new Vector3(0.34f, 0.10f, 0.32f), hair);
                    CreateBlock(root, "Hair_Bangs", new Vector3(0f, 1.64f, 0.14f), new Vector3(0.28f, 0.06f, 0.08f), hair);
                    CreateBlock(root, "Hair_Side_L", new Vector3(-0.18f, 1.48f, -0.02f), new Vector3(0.08f, 0.32f, 0.16f), hair);
                    CreateBlock(root, "Hair_Side_R", new Vector3(0.18f, 1.48f, -0.02f), new Vector3(0.08f, 0.32f, 0.16f), hair);
                    CreateBlock(root, "Hair_Back", new Vector3(0f, 1.42f, -0.18f), new Vector3(0.28f, 0.38f, 0.10f), hair);
                    break;

                case HairStyle.PonytailFemale:
                    CreateBlock(root, "Hair_Top", new Vector3(0f, 1.70f, -0.02f), new Vector3(0.33f, 0.09f, 0.31f), hair);
                    CreateBlock(root, "Hair_Bangs", new Vector3(0f, 1.63f, 0.13f), new Vector3(0.24f, 0.05f, 0.07f), hair);
                    CreateBlock(root, "Hair_Side_L", new Vector3(-0.17f, 1.52f, -0.02f), new Vector3(0.07f, 0.22f, 0.14f), hair);
                    CreateBlock(root, "Hair_Side_R", new Vector3(0.17f, 1.52f, -0.02f), new Vector3(0.07f, 0.22f, 0.14f), hair);
                    CreateBlock(root, "Ponytail", new Vector3(0f, 1.52f, -0.20f), new Vector3(0.12f, 0.32f, 0.12f), hair);
                    break;

                case HairStyle.ModelFemale:
                    CreateBlock(root, "Hair_Top", new Vector3(0f, 1.70f, -0.04f), new Vector3(0.34f, 0.12f, 0.30f), hair);
                    CreateBlock(root, "Hair_Bangs", new Vector3(0f, 1.62f, 0.14f), new Vector3(0.30f, 0.07f, 0.08f), hair);
                    CreateBlock(root, "Hair_Wave_L", new Vector3(-0.20f, 1.40f, 0.02f), new Vector3(0.10f, 0.42f, 0.16f), hair);
                    CreateBlock(root, "Hair_Wave_R", new Vector3(0.20f, 1.40f, 0.02f), new Vector3(0.10f, 0.42f, 0.16f), hair);
                    CreateBlock(root, "Hair_Back", new Vector3(0f, 1.38f, -0.18f), new Vector3(0.30f, 0.44f, 0.10f), hair);
                    break;

                case HairStyle.ElderlyFemale:
                    CreateBlock(root, "Hair_Top", new Vector3(0f, 1.69f, -0.02f), new Vector3(0.34f, 0.10f, 0.32f), hair);
                    CreateBlock(root, "Hair_Bun", new Vector3(0f, 1.66f, -0.16f), new Vector3(0.16f, 0.14f, 0.14f), hair);
                    CreateBlock(root, "Hair_Side_L", new Vector3(-0.17f, 1.50f, 0f), new Vector3(0.07f, 0.20f, 0.14f), hair);
                    CreateBlock(root, "Hair_Side_R", new Vector3(0.17f, 1.50f, 0f), new Vector3(0.07f, 0.20f, 0.14f), hair);
                    break;

                case HairStyle.ElderlyMale:
                    CreateBlock(root, "Hair_Side_L", new Vector3(-0.16f, 1.58f, -0.02f), new Vector3(0.08f, 0.16f, 0.22f), hair);
                    CreateBlock(root, "Hair_Side_R", new Vector3(0.16f, 1.58f, -0.02f), new Vector3(0.08f, 0.16f, 0.22f), hair);
                    CreateBlock(root, "Hair_Back", new Vector3(0f, 1.60f, -0.14f), new Vector3(0.26f, 0.12f, 0.08f), hair);
                    break;

                case HairStyle.WavyMale:
                    CreateBlock(root, "Hair_Top", new Vector3(0f, 1.72f, -0.02f), new Vector3(0.36f, 0.14f, 0.34f), hair);
                    CreateBlock(root, "Hair_Front", new Vector3(0f, 1.64f, 0.12f), new Vector3(0.22f, 0.08f, 0.08f), hair);
                    CreateBlock(root, "Hair_Side_L", new Vector3(-0.17f, 1.56f, 0f), new Vector3(0.08f, 0.18f, 0.18f), hair);
                    CreateBlock(root, "Hair_Side_R", new Vector3(0.17f, 1.56f, 0f), new Vector3(0.08f, 0.18f, 0.18f), hair);
                    break;

                case HairStyle.CoveredShort:
                    CreateBlock(root, "Hair_UnderHat", new Vector3(0f, 1.66f, -0.04f), new Vector3(0.30f, 0.06f, 0.26f), hair);
                    CreateBlock(root, "Hair_Side_L", new Vector3(-0.16f, 1.56f, -0.02f), new Vector3(0.06f, 0.12f, 0.16f), hair);
                    CreateBlock(root, "Hair_Side_R", new Vector3(0.16f, 1.56f, -0.02f), new Vector3(0.06f, 0.12f, 0.16f), hair);
                    break;

                default:
                    CreateBlock(root, "Hair_Top", new Vector3(0f, 1.70f, -0.02f), new Vector3(0.34f, 0.10f, 0.32f), hair);
                    CreateBlock(root, "Hair_Side_L", new Vector3(-0.17f, 1.56f, -0.02f), new Vector3(0.07f, 0.16f, 0.18f), hair);
                    CreateBlock(root, "Hair_Side_R", new Vector3(0.17f, 1.56f, -0.02f), new Vector3(0.07f, 0.16f, 0.18f), hair);
                    CreateBlock(root, "Hair_Back", new Vector3(0f, 1.58f, -0.16f), new Vector3(0.26f, 0.14f, 0.08f), hair);
                    break;
            }
        }

        private static Material PickIris(int variant)
        {
            int v = Mathf.Abs(variant) % 4;
            if (v == 1) return GetMaterial("Mat_FaceIrisBlue", new Color(0.28f, 0.48f, 0.78f), 0.1f, 0.5f);
            if (v == 2) return GetMaterial("Mat_FaceIrisGreen", new Color(0.28f, 0.55f, 0.32f), 0.1f, 0.5f);
            if (v == 3) return GetMaterial("Mat_FaceIrisHazel", new Color(0.55f, 0.38f, 0.18f), 0.1f, 0.5f);
            return GetMaterial("Mat_FaceIrisBrown", new Color(0.32f, 0.18f, 0.10f), 0.1f, 0.5f);
        }

        private static Material PickLipstick(int variant)
        {
            int v = Mathf.Abs(variant) % 3;
            if (v == 1) return GetMaterial("Mat_FaceLipBerry", new Color(0.72f, 0.18f, 0.38f), 0.15f, 0.65f);
            if (v == 2) return GetMaterial("Mat_FaceLipNude", new Color(0.82f, 0.42f, 0.40f), 0.12f, 0.55f);
            return GetMaterial("Mat_FaceLipRed", new Color(0.78f, 0.12f, 0.22f), 0.18f, 0.7f);
        }

        private static void AddHeadPart(Transform head, string name, Vector3 worldOffset, Vector3 worldSize, Material mat, Vector3 euler = default)
        {
            Vector3 hs = head.localScale;
            if (Mathf.Abs(hs.x) < 0.001f || Mathf.Abs(hs.y) < 0.001f || Mathf.Abs(hs.z) < 0.001f) return;

            Vector3 localPos = new Vector3(worldOffset.x / hs.x, worldOffset.y / hs.y, worldOffset.z / hs.z);
            Vector3 localScale = new Vector3(worldSize.x / hs.x, worldSize.y / hs.y, worldSize.z / hs.z);
            GameObject part = CreateBlock(head, name, localPos, localScale, mat);
            if (euler != Vector3.zero)
            {
                part.transform.localRotation = Quaternion.Euler(euler);
            }
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject cube = PrimitiveFactory.CreateVisualCube(name, parent);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = localScale;

            if (mat != null)
            {
                cube.GetComponent<Renderer>().sharedMaterial = mat;
            }

            return cube;
        }
    }
}
