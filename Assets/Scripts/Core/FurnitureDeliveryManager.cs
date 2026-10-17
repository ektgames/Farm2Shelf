using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Farm2Shelf.UI;
using Farm2Shelf.Environment;
using Farm2Shelf.Utils;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Farm2Shelf.Core
{
    public class DeliveryPalletClickable : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.dragging || eventData.button != PointerEventData.InputButton.Left) return;
            TouchInputHelper.Dispatch3DClick(eventData.position);
        }
    }

    /// <summary>
    /// Haritada Mal Kabul Kapısı yanında sabit Ahşap Teslimat Paletini (Delivery Pallet) oluşturan ve
    /// TrendyShop'tan alınan siparişlerin koli (DeliveryBox) olarak palet üstünde birikmesini sağlayan yönetici.
    /// Kamyon yanaşma alanına (X: 13, Z: 2) KESİNLİKLE engel olmaz.
    /// </summary>
    public class FurnitureDeliveryManager : MonoBehaviour
    {
        public static FurnitureDeliveryManager Instance { get; private set; }

        [Header("Palet Konum Ayarları")]
        // X: 11.8 (Mal kabul kapısı yanı), Y: 0.01, Z: 6.0 (Kamyon yanaşma alanı Z: -1..5 dışı!)
        private Vector3 palletPosition = new Vector3(11.8f, 0.01f, 6.0f);

        private GameObject palletObj;
        private Transform boxContainer;
        private readonly List<DeliveryBoxController> activeBoxes = new List<DeliveryBoxController>();

        public bool TryGetTutorialPalletBounds(out Bounds bounds)
        {
            bounds = default;
            if (palletObj == null || !palletObj.activeInHierarchy) return false;
            Collider pallet = palletObj.GetComponent<Collider>();
            if (pallet == null || !pallet.enabled) return false;
            bounds = pallet.bounds;
            foreach (DeliveryBoxController box in activeBoxes)
            {
                if (box == null || !box.gameObject.activeInHierarchy) continue;
                Collider boxCollider = box.GetComponent<Collider>();
                if (boxCollider != null && boxCollider.enabled) bounds.Encapsulate(boxCollider.bounds);
            }
            return true;
        }

        public List<string> GetActiveBoxTypes()
        {
            List<string> types = new List<string>();
            foreach (var b in activeBoxes)
            {
                if (b != null) types.Add(b.FurnitureType.ToString());
            }
            return types;
        }

        public void ClearPendingBoxes()
        {
            RestorePendingBoxes(null);
        }

        public void RestorePendingBoxes(List<string> boxTypes)
        {
            for (int i = activeBoxes.Count - 1; i >= 0; i--)
            {
                if (activeBoxes[i] != null && activeBoxes[i].gameObject != null)
                {
                    Destroy(activeBoxes[i].gameObject);
                }
            }
            activeBoxes.Clear();

            if (boxTypes != null)
            {
                foreach (var tStr in boxTypes)
                {
                    if (Enum.TryParse<FurnitureType>(tStr, out FurnitureType fType) && fType != FurnitureType.DividerFence)
                    {
                        CreateCargoBoxOnPallet(fType);
                    }
                }
            }

            ReorganizeBoxesOnPallet();
        }

        private Material palletWoodMat;
        private Material cardboardMat;
        private Material tapeMat;
        private GameObject arrivalGlowRoot;
        private PalletCargoArrow cargoArrow;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            InitMaterials();
            CreateDeliveryPallet();
        }

        private void InitMaterials()
        {
            if (palletWoodMat != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Lightweight Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            palletWoodMat = new Material(shader);
            palletWoodMat.color = new Color(0.60f, 0.40f, 0.20f); // Ahşap palet rengi

            cardboardMat = new Material(shader);
            cardboardMat.color = new Color(0.78f, 0.62f, 0.42f); // Karton koli rengi

            tapeMat = new Material(shader);
            tapeMat.color = new Color(0.85f, 0.75f, 0.40f); // Koli bandı
        }

        private void CreateDeliveryPallet()
        {
            if (palletObj != null) return;

            palletObj = new GameObject("Delivery_Pallet_Root");
            palletObj.transform.position = palletPosition;

            BoxCollider pCol = palletObj.AddComponent<BoxCollider>();
            pCol.center = new Vector3(0f, 0.0725f, 0f);
            pCol.size = new Vector3(1.6f, 0.145f, 1.6f); // Match the visible pallet, not surrounding empty space.

            palletObj.AddComponent<DeliveryPalletClickable>();

            GameObject palletBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palletBase.name = "EuroPallet_Base";
            palletBase.transform.SetParent(palletObj.transform, false);
            palletBase.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            palletBase.transform.localScale = new Vector3(1.6f, 0.12f, 1.6f);
            palletBase.GetComponent<Renderer>().sharedMaterial = palletWoodMat;
            Destroy(palletBase.GetComponent<Collider>());

            for (float x = -0.6f; x <= 0.6f; x += 0.4f)
            {
                GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = "Pallet_Slat";
                slat.transform.SetParent(palletObj.transform, false);
                slat.transform.localPosition = new Vector3(x, 0.13f, 0f);
                slat.transform.localScale = new Vector3(0.12f, 0.03f, 1.6f);
                slat.GetComponent<Renderer>().sharedMaterial = palletWoodMat;
                Destroy(slat.GetComponent<Collider>());
            }

            GameObject labelObj = new GameObject("Pallet_Label");
            labelObj.transform.SetParent(palletObj.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.16f, 0.85f);
            labelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            boxContainer = new GameObject("Delivery_Boxes_Container").transform;
            boxContainer.SetParent(palletObj.transform, false);
        }

        public Dictionary<FurnitureType, int> GetPendingFurnitureCounts()
        {
            activeBoxes.RemoveAll(b => b == null || b.gameObject == null);
            Dictionary<FurnitureType, int> counts = new Dictionary<FurnitureType, int>();
            foreach (var b in activeBoxes)
            {
                if (b == null) continue;
                if (!counts.ContainsKey(b.FurnitureType))
                {
                    counts[b.FurnitureType] = 0;
                }
                counts[b.FurnitureType]++;
            }
            return counts;
        }

        public DeliveryBoxController GetFirstBoxOfType(FurnitureType type)
        {
            activeBoxes.RemoveAll(b => b == null || b.gameObject == null);
            return activeBoxes.Find(b => b != null && b.FurnitureType == type);
        }

        public void RemoveOneBoxOfType(FurnitureType type)
        {
            DeliveryBoxController box = GetFirstBoxOfType(type);
            if (box != null)
            {
                RemoveBox(box);
            }
        }

        public void AddOrdersToPallet(List<FurnitureType> items)
        {
            if (items == null || items.Count == 0) return;

            foreach (var itemType in items)
            {
                CreateCargoBoxOnPallet(itemType);
            }

            ReorganizeBoxesOnPallet();
        }

        public DeliveryBoxController CreateCargoBoxOnPallet(FurnitureType type)
        {
            if (type == FurnitureType.DividerFence) return null;
            InitMaterials();

            GameObject boxObj = new GameObject("CargoBox_" + type.ToString());
            boxObj.transform.SetParent(boxContainer, false);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "BoxBody";
            body.transform.SetParent(boxObj.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
            body.GetComponent<Renderer>().sharedMaterial = cardboardMat;
            Destroy(body.GetComponent<Collider>());

            GameObject tapeX = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tapeX.name = "TapeX";
            tapeX.transform.SetParent(boxObj.transform, false);
            tapeX.transform.localPosition = new Vector3(0f, 0.23f, 0f);
            tapeX.transform.localScale = new Vector3(0.56f, 0.01f, 0.12f);
            tapeX.GetComponent<Renderer>().sharedMaterial = tapeMat;
            Destroy(tapeX.GetComponent<Collider>());

            BoxCollider col = boxObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.005f, 0f);
            col.size = new Vector3(0.56f, 0.46f, 0.55f);

            DeliveryBoxController controller = boxObj.AddComponent<DeliveryBoxController>();
            controller.SetupBox(type);

            activeBoxes.Add(controller);
            ReorganizeBoxesOnPallet();

            return controller;
        }

        /// <summary>
        /// Kolileri palet üzerinde 2x2 ızgara şeklinde üst üste düzenli olarak istifler.
        /// </summary>
        public void ReorganizeBoxesOnPallet()
        {
            // Temizlik (Yok olmuş nesneleri listeden çıkar)
            activeBoxes.RemoveAll(b => b == null || b.gameObject == null);

            Vector3[] gridOffsets = new Vector3[]
            {
                new Vector3(-0.38f, 0f, -0.38f),
                new Vector3( 0.38f, 0f, -0.38f),
                new Vector3(-0.38f, 0f,  0.38f),
                new Vector3( 0.38f, 0f,  0.38f)
            };

            float boxHeight = 0.48f;
            float baseOffsetY = 0.38f; // Palet üst yüksekliği

            for (int i = 0; i < activeBoxes.Count; i++)
            {
                int gridIdx = i % 4;
                int layerIdx = i / 4;

                Vector3 targetLocalPos = gridOffsets[gridIdx] + new Vector3(0f, baseOffsetY + (layerIdx * boxHeight), 0f);
                activeBoxes[i].transform.localPosition = targetLocalPos;
                activeBoxes[i].transform.localRotation = Quaternion.identity;
            }

            RefreshArrivalGlow();
        }

        /// <summary>
        /// Yerleştirme moduna geçilen koliyi paletten kaldırır.
        /// </summary>
        public void RemoveBox(DeliveryBoxController controller)
        {
            if (controller != null && activeBoxes.Contains(controller))
            {
                activeBoxes.Remove(controller);
                Destroy(controller.gameObject);
                ReorganizeBoxesOnPallet();
            }
        }

        private void RefreshArrivalGlow()
        {
            bool hasCargo = false;
            for (int i = 0; i < activeBoxes.Count; i++)
            {
                if (activeBoxes[i] != null && activeBoxes[i].gameObject != null)
                {
                    hasCargo = true;
                    break;
                }
            }

            if (hasCargo)
            {
                EnsureArrivalGlow();
                if (cargoArrow == null && palletObj != null)
                {
                    GameObject arrowObj = new GameObject("Pallet_Cargo_Arrow");
                    arrowObj.transform.SetParent(palletObj.transform, false);
                    cargoArrow = arrowObj.AddComponent<PalletCargoArrow>();
                    cargoArrow.Initialize();
                }
                if (cargoArrow != null)
                {
                    float cargoTop = 0.62f + ((activeBoxes.Count - 1) / 4) * 0.48f;
                    cargoArrow.SetHeight(Mathf.Max(2.2f, cargoTop + 0.85f));
                }
            }

            if (cargoArrow != null) cargoArrow.gameObject.SetActive(hasCargo);

            if (arrivalGlowRoot != null)
            {
                arrivalGlowRoot.SetActive(hasCargo);
            }
        }

        private void EnsureArrivalGlow()
        {
            if (arrivalGlowRoot != null || palletObj == null) return;

            arrivalGlowRoot = new GameObject("Pallet_Arrival_Glow");
            arrivalGlowRoot.transform.SetParent(palletObj.transform, false);
            arrivalGlowRoot.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            arrivalGlowRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Light arrivalSpotLight = arrivalGlowRoot.AddComponent<Light>();
            arrivalSpotLight.type = LightType.Spot;
            arrivalSpotLight.color = new Color(1.00f, 0.84f, 0.32f);
            arrivalSpotLight.shadows = LightShadows.None;
            arrivalSpotLight.bounceIntensity = 0f;
            LightingPipelineBinder.ConfigureRealtimeLight(arrivalSpotLight);

            bool mobile = LightingPipelineBinder.IsMobileLightingProfile();
            arrivalSpotLight.intensity = mobile ? 0.78f : 1.15f;
            arrivalSpotLight.range = mobile ? 3.8f : 4.6f;
            arrivalSpotLight.spotAngle = 52f;
            arrivalSpotLight.innerSpotAngle = 24f;
            arrivalSpotLight.renderMode = mobile ? LightRenderMode.ForceVertex : LightRenderMode.ForcePixel;

            GameObject haloObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            haloObj.name = "Pallet_Arrival_Halo";
            haloObj.transform.SetParent(palletObj.transform, false);
            haloObj.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            haloObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            haloObj.transform.localScale = new Vector3(2.15f, 2.15f, 1f);
            Collider haloCol = haloObj.GetComponent<Collider>();
            if (haloCol != null) Destroy(haloCol);

            Renderer haloRend = haloObj.GetComponent<Renderer>();
            haloRend.sharedMaterial = CreateArrivalHaloMaterial();
            haloRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            haloRend.receiveShadows = false;
            haloObj.transform.SetParent(arrivalGlowRoot.transform, true);

            PalletArrivalGlowPulse pulse = arrivalGlowRoot.AddComponent<PalletArrivalGlowPulse>();
            pulse.Bind(arrivalSpotLight, haloObj.transform, mobile);
        }

        private static Material CreateArrivalHaloMaterial()
        {
            Shader shader = ShaderHelper.GetUnlitShader();
            Material mat = shader != null ? new Material(shader) : ShaderHelper.CreateUnlitOpaqueMaterial(new Color(1f, 0.82f, 0.28f, 0.35f), "PalletArrivalHaloMat");
            mat.name = "PalletArrivalHaloMat";

            Texture2D tex = CreateRadialGlowTexture(64);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);

            Color tint = new Color(1.00f, 0.86f, 0.38f, 0.42f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            mat.color = tint;

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
            mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            return mat;
        }

        private static Texture2D CreateRadialGlowTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "PalletArrivalHaloTex";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float half = (size - 1) * 0.5f;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - half) / half;
                    float ny = (y - half) / half;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (0.15f + 0.85f * a);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }

    internal sealed class PalletCargoArrow : MonoBehaviour
    {
        private Mesh arrowMesh;
        private Material yellowMaterial;
        private Material outlineMaterial;
        private float baseHeight = 2.2f;

        public void Initialize()
        {
            // A small unlit mesh stays legible at night without HDR emission or extra lights.
            arrowMesh = new Mesh { name = "PalletCargoArrowMesh" };
            arrowMesh.vertices = new[]
            {
                new Vector3(-0.42f, 0f, 0f), new Vector3(0f, -0.46f, 0f),
                new Vector3(0.42f, 0f, 0f), new Vector3(0.16f, 0f, 0f),
                new Vector3(0.16f, 0.55f, 0f), new Vector3(-0.16f, 0.55f, 0f),
                new Vector3(-0.16f, 0f, 0f)
            };
            arrowMesh.triangles = new[] { 0, 1, 2, 3, 4, 5, 3, 5, 6 };
            arrowMesh.RecalculateNormals();
            arrowMesh.RecalculateBounds();
            yellowMaterial = ShaderHelper.CreateUnlitOpaqueMaterial(new Color(1f, 0.79f, 0.06f), "PalletArrowYellow");
            outlineMaterial = ShaderHelper.CreateUnlitOpaqueMaterial(new Color(0.22f, 0.13f, 0.025f), "PalletArrowOutline");
            CreateLayer("Outline", outlineMaterial, 1.12f, 0.015f);
            CreateLayer("Yellow", yellowMaterial, 1f, -0.015f);
        }

        private void CreateLayer(string name, Material material, float scale, float depth)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(transform, false);
            layer.transform.localPosition = new Vector3(0f, 0f, depth);
            layer.transform.localScale = Vector3.one * scale;
            layer.AddComponent<MeshFilter>().sharedMesh = arrowMesh;
            MeshRenderer renderer = layer.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // No collider or UI raycast target: the indicator cannot intercept pallet taps.
        }

        public void SetHeight(float height)
        {
            baseHeight = height;
            transform.localPosition = new Vector3(0f, baseHeight, 0f);
        }

        private void LateUpdate()
        {
            float bounce = 0.5f - 0.5f * Mathf.Cos(Time.time * 4.5f);
            transform.localPosition = new Vector3(0f, baseHeight + bounce * 0.28f, 0f);
            Camera camera = GameplayPerf.MainCamera;
            if (camera != null) transform.rotation = camera.transform.rotation;
        }

        private void OnDestroy()
        {
            if (arrowMesh != null) Destroy(arrowMesh);
            if (yellowMaterial != null) Destroy(yellowMaterial);
            if (outlineMaterial != null) Destroy(outlineMaterial);
        }
    }

    internal sealed class PalletArrivalGlowPulse : MonoBehaviour
    {
        private Light spot;
        private Transform halo;
        private float baseIntensity = 1f;
        private Vector3 haloBaseScale = Vector3.one;

        public void Bind(Light light, Transform haloTransform, bool mobile)
        {
            spot = light;
            halo = haloTransform;
            baseIntensity = light != null ? light.intensity : 1f;
            haloBaseScale = haloTransform != null ? haloTransform.localScale : Vector3.one;
            if (mobile && spot != null)
            {
                baseIntensity = Mathf.Min(baseIntensity, 0.85f);
            }
        }

        private void LateUpdate()
        {
            if (spot == null && halo == null) return;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.1f);
            float pulse = 0.88f + 0.12f * wave;
            if (spot != null)
            {
                spot.intensity = baseIntensity * pulse;
            }

            if (halo != null)
            {
                float s = 0.96f + 0.06f * wave;
                halo.localScale = new Vector3(haloBaseScale.x * s, haloBaseScale.y * s, haloBaseScale.z);
            }
        }
    }
}
