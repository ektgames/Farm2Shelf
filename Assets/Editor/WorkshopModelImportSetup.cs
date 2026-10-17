using System;
using System.IO;
using Farm2Shelf.Core;
using Farm2Shelf.Environment;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Farm2Shelf.Editor
{
    // Only the derived mobile assets are imported with these settings.
    // Original Meshy files in Assets/assetler and on the Desktop remain untouched.
    public class WorkshopModelImportSetup : AssetPostprocessor
    {
        internal const string SourceRoot = "Assets/WorkshopModels/";
        internal const string PrefabRoot = "Assets/Resources/WorkshopMachines/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(SourceRoot, StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Low;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.importBlendShapes = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SourceRoot, StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("/Normal.png", StringComparison.Ordinal);
            bool packed = assetPath.EndsWith("/MetallicSmoothness.png", StringComparison.Ordinal);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal && !packed;
            importer.alphaSource = packed ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.anisoLevel = 2;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "iPhone", overridden = true, maxTextureSize = 1024,
                format = TextureImporterFormat.ASTC_6x6, compressionQuality = 75
            });
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 1024,
                format = TextureImporterFormat.ETC2_RGBA8, compressionQuality = 75
            });
        }

        [InitializeOnLoadMethod]
        private static void ScheduleSetup()
        {
            EditorApplication.delayCall += GenerateMissing;
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
        {
            foreach (string path in imported)
            {
                if (path.StartsWith(SourceRoot, StringComparison.Ordinal) &&
                    (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
                {
                    ScheduleSetup();
                    break;
                }
            }
        }

        private static void GenerateMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            Generate(false);
        }

        [MenuItem("Farm2Shelf/Atölye/Mobil makine modellerini hazırla")]
        public static void Rebuild() => Generate(true);

        internal static void Generate(bool overwrite)
        {
            // Visual matches confirmed from previews; juice press confirmed by the user.
            Build(WorkshopMachineType.JamMaker, "1001195420", overwrite);
            Build(WorkshopMachineType.JuiceExtractor, "1001195452", overwrite);
            Build(WorkshopMachineType.Cannery, "1001195501", overwrite);
            Build(WorkshopMachineType.Dehydrator, "1001195443", overwrite);
            Build(WorkshopMachineType.SaladStation, "1001203659", overwrite);
            Build(WorkshopMachineType.OilPress, "1003173858", overwrite);
            Build(WorkshopMachineType.JamMaker, "1003180948", overwrite, true);
            Build(WorkshopMachineType.JamMaker, "1003180716", overwrite, cashier: true);
            Build(WorkshopMachineType.JamMaker, "1003180538", overwrite, basket: true);
            Build(WorkshopMachineType.JamMaker, "1003181443", overwrite, trashBin: true);
            Build(WorkshopMachineType.JamMaker, "1003182541", overwrite, atm: true);
            Build(WorkshopMachineType.JamMaker, "1003182145", overwrite);
            Build(WorkshopMachineType.JamMaker, "1003183810", overwrite);
            Build(WorkshopMachineType.JamMaker, "1010203921", overwrite);
            Build(WorkshopMachineType.JamMaker, "1003182731", overwrite);
            Build(WorkshopMachineType.JamMaker, "1003183347", overwrite);
            Build(WorkshopMachineType.JamMaker, "1003183600", overwrite);
            Build(WorkshopMachineType.JamMaker, "1011091448", overwrite);
            Build(WorkshopMachineType.JamMaker, "1011091336", overwrite);
            Build(WorkshopMachineType.JamMaker, "1011091705", overwrite);
            Build(WorkshopMachineType.JamMaker, "1011091217", overwrite);
            Build(WorkshopMachineType.JamMaker, "1011091258", overwrite);
            Build(WorkshopMachineType.JamMaker, "1011091411", overwrite);
            Build(WorkshopMachineType.JamMaker, "1011091538", overwrite);
            foreach (FurnitureType shelf in DisplayShelves)
                Build(WorkshopMachineType.JamMaker,
                    shelf == FurnitureType.ProduceShelf ? "1011080604" :
                    shelf == FurnitureType.GourmetShelf ? "1011080358" :
                    shelf == FurnitureType.ButcherCounter ? "1003182352" :
                    shelf == FurnitureType.BakeryCounter ? "1003182006" :
                    shelf == FurnitureType.Freezer ? "1003175103" :
                    shelf == FurnitureType.Fridge || shelf == FurnitureType.OrganicFridge ? "1003171444" : "1003175652",
                    overwrite, shelfType: shelf);
        }

        internal static readonly FurnitureType[] DisplayShelves = { FurnitureType.Shelf, FurnitureType.ProduceShelf,
            FurnitureType.GourmetShelf, FurnitureType.CosmeticShelf, FurnitureType.ElectronicsShelf,
            FurnitureType.Fridge, FurnitureType.OrganicFridge, FurnitureType.Freezer, FurnitureType.BakeryCounter, FurnitureType.ButcherCounter };

        private static void Build(WorkshopMachineType type, string sourceId, bool overwrite, bool customerService = false, bool cashier = false, bool basket = false, bool trashBin = false, FurnitureType? shelfType = null, bool atm = false)
        {
            bool plantPot = sourceId == "1003182145";
            bool arcade = sourceId == "1003183810";
            bool goldenStatue = sourceId == "1010203921";
            bool waterDispenser = sourceId == "1003182731";
            bool coffeeMachine = sourceId == "1003183347";
            bool snackMachine = sourceId == "1003183600";
            bool candyMachine = sourceId == "1011091448";
            bool slushieMachine = sourceId == "1011091336";
            bool iceCreamCart = sourceId == "1011091705";
            bool donutCabinet = sourceId == "1011091217";
            bool jukebox = sourceId == "1011091258";
            bool massageChair = sourceId == "1011091411";
            bool aquarium = sourceId == "1011091538";
            bool produceShelf = sourceId == "1011080604";
            bool gourmetShelf = sourceId == "1011080358";
            string prefabPath = trashBin ? "Assets/Resources/Furniture/TrashCan.prefab" : basket ? "Assets/Resources/Furniture/ShoppingBasket.prefab" : cashier ? "Assets/Resources/Furniture/Cashier.prefab" : customerService ? "Assets/Resources/Furniture/CustomerServiceDesk.prefab" : PrefabRoot + type + ".prefab";
            if (arcade) prefabPath = "Assets/Resources/Furniture/ArcadeMachine.prefab";
            if (goldenStatue) prefabPath = "Assets/Resources/Furniture/GoldenStatue.prefab";
            if (waterDispenser) prefabPath = "Assets/Resources/Furniture/WaterDispenser.prefab";
            if (coffeeMachine) prefabPath = "Assets/Resources/Furniture/CoffeeMachine.prefab";
            if (snackMachine) prefabPath = "Assets/Resources/Furniture/VendingSnack.prefab";
            if (candyMachine) prefabPath = "Assets/Resources/Furniture/GumballMachine.prefab";
            if (slushieMachine) prefabPath = "Assets/Resources/Furniture/SlushieMachine.prefab";
            if (iceCreamCart) prefabPath = "Assets/Resources/Furniture/IceCreamCart.prefab";
            if (donutCabinet) prefabPath = "Assets/Resources/Furniture/DonutDispenser.prefab";
            if (jukebox) prefabPath = "Assets/Resources/Furniture/Jukebox.prefab";
            if (massageChair) prefabPath = "Assets/Resources/Furniture/MassageChair.prefab";
            if (aquarium) prefabPath = "Assets/Resources/Furniture/AquariumGrand.prefab";
            if (atm) prefabPath = "Assets/Resources/Furniture/AtmMachine.prefab";
            if (plantPot) prefabPath = "Assets/Resources/Furniture/PlantPot.prefab";
            if (shelfType.HasValue) prefabPath = "Assets/Resources/Furniture/" + shelfType.Value + ".prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!overwrite && existingPrefab != null &&
                (!waterDispenser || existingPrefab.transform.Find("WaterDirectionAligned") != null) &&
                (!produceShelf || existingPrefab.transform.Find("ProduceShelf_1011080604") != null) &&
                (!gourmetShelf || existingPrefab.transform.Find("GourmetShelf_1011080358") != null)) return;
            string folder = SourceRoot + sourceId + "/";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Model.fbx");
            Texture2D color = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "BaseColor.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "Normal.png");
            Texture2D metal = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "MetallicSmoothness.png");
            if (model == null || color == null || normal == null || metal == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Atölye modelleri için URP Lit shader bulunamadı.");

            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            AssetDatabase.Refresh();
            string materialPath = folder + "MobileSurface.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 0.65f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", metal);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Smoothness", 0.65f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Cull", plantPot ? 0f : 2f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            // Build in a preview scene: never modify the user's currently open scene.
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = new GameObject("Machine_Model_" + type);
                SceneManager.MoveGameObjectToScene(root, preview);
                if (produceShelf) new GameObject("ProduceShelf_1011080604").transform.SetParent(root.transform, false);
                if (gourmetShelf) new GameObject("GourmetShelf_1011080358").transform.SetParent(root.transform, false);
                Bounds target;
                if (aquarium)
                {
                    target = new Bounds(new Vector3(0f, .9f, 0f), new Vector3(2.2f, 1.8f, .8f));
                    root.name = "AquariumGrand";
                }
                else if (massageChair)
                {
                    // Legacy seat bottom 0, headrest top 1.35 + .125; no ground arrow.
                    target = new Bounds(new Vector3(0f, .7375f, 0f), new Vector3(.9f, 1.475f, 1.1f));
                    root.name = "MassageChair";
                }
                else if (jukebox)
                {
                    // Legacy body and neon cylinder envelope, excluding the ground arrow.
                    // Z runs from -.71 (neon rim) to .35 (cabinet back).
                    target = new Bounds(new Vector3(0f, .8f, -.18f), new Vector3(1f, 1.6f, 1.06f));
                    root.name = "Jukebox";
                }
                else if (donutCabinet)
                {
                    // Legacy cabinet bottom 0 and cylinder top 1.3 + .8 = 2.1.
                    target = new Bounds(new Vector3(0f, 1.05f, 0f), new Vector3(.9f, 2.1f, .75f));
                    root.name = "DonutDispenser";
                }
                else if (iceCreamCart)
                {
                    // Legacy canopy/wheel envelope, excluding the arrow: 1.5 x 1.29 x 1.125.
                    // Keep the same footprint, placing the replacement wheels on the ground.
                    target = new Bounds(new Vector3(0f, .645f, .0625f), new Vector3(1.5f, 1.29f, 1.125f));
                    root.name = "IceCreamCart";
                }
                else if (slushieMachine)
                {
                    // Legacy base and bowls; Unity cylinder height is twice its Y scale.
                    target = new Bounds(new Vector3(0f, .725f, 0f), new Vector3(.85f, 1.45f, .65f));
                    root.name = "SlushieMachine";
                }
                else if (candyMachine)
                {
                    // Legacy base bottom 0, globe top 1.075; exclude the ground arrow.
                    target = new Bounds(new Vector3(0f, .5375f, 0f), new Vector3(.45f, 1.075f, .45f));
                    root.name = "GumballMachine";
                }
                else if (snackMachine)
                {
                    target = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, .8f));
                    root.name = "VendingSnack";
                }
                else if (coffeeMachine)
                {
                    target = new Bounds(new Vector3(0f, 0.8f, 0f), new Vector3(1.1f, 1.6f, 0.7f));
                    root.name = "CoffeeMachine";
                }
                else if (waterDispenser)
                {
                    // Original cabinet and bottle, excluding the old ground arrow.
                    target = new Bounds(new Vector3(0f, 0.85f, 0f), new Vector3(0.45f, 1.7f, 0.6f));
                    root.name = "WaterDispenser";
                }
                else if (goldenStatue)
                {
                    // Legacy marble base (0..0.8) and cylinder (0.3..2.3).
                    target = new Bounds(new Vector3(0f, 1.15f, 0f), new Vector3(0.7f, 2.3f, 0.7f));
                    root.name = "GoldenStatue";
                }
                else if (plantPot)
                {
                    // Legacy pot + leaves envelope: 0.6 x 1.0 x 0.6 metres.
                    target = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1f, 0.6f));
                    root.name = "PlantPot";
                }
                else if (trashBin)
                {
                    target = new Bounds(new Vector3(0f, 0.43f, 0f), new Vector3(0.47f, 0.86f, 0.47f));
                    root.name = "TrashCan";
                }
                else if (basket)
                {
                    // Exact legacy stand envelope, excluding the old ground arrow.
                    target = new Bounds(new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.9f, 0.8f));
                    root.name = "ShoppingBasketStand";
                }
                else
                {
                    GameObject legacy = arcade ? FurnitureModelBuilder.BuildProceduralDisplayShelf(FurnitureType.ArcadeMachine) : atm ? FurnitureModelBuilder.BuildProceduralDisplayShelf(FurnitureType.AtmMachine) : shelfType.HasValue ? FurnitureModelBuilder.BuildProceduralDisplayShelf(shelfType.Value) : cashier ? FurnitureModelBuilder.BuildProceduralCashierCounter() : customerService ? FurnitureModelBuilder.BuildProceduralCustomerServiceDesk()
                        : WorkshopMachineModelBuilder.BuildProceduralMachineModel(type);
                    SceneManager.MoveGameObjectToScene(legacy, preview);
                    target = RendererBounds(legacy);
                    UnityEngine.Object.DestroyImmediate(legacy);
                }

                GameObject visual = UnityEngine.Object.Instantiate(model, root.transform);
                visual.name = "Visual";
                Transform seat = null;
                Transform facing = null;
                if (waterDispenser || coffeeMachine || snackMachine || candyMachine || slushieMachine || iceCreamCart || donutCabinet || jukebox || massageChair || aquarium)
                {
                    Transform center = null, front = null;
                    foreach (Transform part in visual.GetComponentsInChildren<Transform>(true))
                    {
                        if (part.name == (aquarium ? "AquariumCenter" : massageChair ? "MassageCenter" : jukebox ? "JukeboxCenter" : donutCabinet ? "DonutCenter" : iceCreamCart ? "IceCreamCenter" : slushieMachine ? "SlushieCenter" : candyMachine ? "CandyCenter" : snackMachine ? "SnackCenter" : coffeeMachine ? "CoffeeCenter" : "WaterCenter")) center = part;
                        if (part.name == (aquarium ? "AquariumFront" : massageChair ? "MassageFront" : jukebox ? "JukeboxFront" : donutCabinet ? "DonutFront" : iceCreamCart ? "IceCreamFront" : slushieMachine ? "SlushieFront" : candyMachine ? "CandyFront" : snackMachine ? "SnackFront" : coffeeMachine ? "CoffeeFront" : "WaterFront")) front = part;
                    }
                    if (center == null || front == null)
                        throw new InvalidOperationException("İçecek otomatı kullanım yönü noktaları eksik: " + sourceId);
                    Vector3 direction = Vector3.ProjectOnPlane(front.position - center.position, Vector3.up);
                    visual.transform.rotation = Quaternion.FromToRotation(direction, Vector3.back) * visual.transform.rotation;
                    new GameObject(aquarium ? "AquariumDirectionAligned" : massageChair ? "MassageDirectionAligned" : jukebox ? "JukeboxDirectionAligned" : donutCabinet ? "DonutDirectionAligned" : iceCreamCart ? "IceCreamDirectionAligned" : slushieMachine ? "SlushieDirectionAligned" : candyMachine ? "CandyDirectionAligned" : snackMachine ? "SnackDirectionAligned" : coffeeMachine ? "CoffeeDirectionAligned" : "WaterDirectionAligned").transform.SetParent(root.transform, false);
                }
                if (customerService || cashier || basket || shelfType.HasValue || atm || arcade)
                {
                    foreach (Transform part in visual.GetComponentsInChildren<Transform>(true))
                    {
                        if (part.name == (shelfType.HasValue || atm || arcade ? "ShelfCenter" : basket ? "BasketCenter" : cashier ? "CashierStaff" : "CustomerServiceSeat")) seat = part;
                        if (part.name == (shelfType.HasValue || atm || arcade ? "ShelfFront" : basket ? "BasketFront" : cashier ? "CashierCustomer" : "CustomerServiceFacing")) facing = part;
                    }
                    if (seat == null || facing == null) throw new InvalidOperationException("Mobilya personel/müşteri hizalama noktaları eksik.");
                    Vector3 forward = Vector3.ProjectOnPlane(facing.position - seat.position, Vector3.up);
                    visual.transform.rotation = Quaternion.FromToRotation(forward, Vector3.back) * visual.transform.rotation;
                }
                Bounds source = RendererBounds(visual);
                if (source.size.x <= 0f || source.size.y <= 0f || source.size.z <= 0f)
                    throw new InvalidOperationException(type + " model boyutları geçersiz.");
                if (type == WorkshopMachineType.OilPress || customerService || cashier || basket || trashBin || shelfType.HasValue || atm || plantPot || arcade || goldenStatue || waterDispenser || coffeeMachine || snackMachine || candyMachine || slushieMachine || iceCreamCart || donutCabinet || jukebox || massageChair || aquarium)
                {
                    // Match the requested original width, height and depth exactly.
                    // Scale a world-aligned parent because FBX roots may have an axis rotation.
                    GameObject fit = new GameObject("OriginalMachineSize");
                    fit.transform.SetParent(root.transform, false);
                    visual.transform.SetParent(fit.transform, true);
                    fit.transform.localScale = new Vector3(target.size.x / source.size.x,
                        target.size.y / source.size.y, target.size.z / source.size.z);
                }
                else
                {
                    float scale = Mathf.Min(target.size.x / source.size.x, target.size.y / source.size.y, target.size.z / source.size.z);
                    visual.transform.localScale *= scale;
                }
                source = RendererBounds(visual);
                visual.transform.position += new Vector3(target.center.x - source.center.x, target.min.y - source.min.y, target.center.z - source.center.z);
                if (shelfType.HasValue)
                {
                    root.name = shelfType.Value.ToString();
                    var layout = root.AddComponent<ShelfProductLayout>();
                    var markers = new System.Collections.Generic.Dictionary<string, Transform>();
                    foreach (Transform part in visual.GetComponentsInChildren<Transform>(true)) markers[part.name] = part;
                    for (int row = 0; row < 4; row++)
                    {
                        for (int slot = 0; slot < 10; slot++)
                            layout.cells[row * 10 + slot] = root.transform.InverseTransformPoint(markers[$"Row{row}Cell{slot}"].position);
                        Vector3 min = root.transform.InverseTransformPoint(markers[$"Row{row}SizeMin"].position);
                        Vector3 max = root.transform.InverseTransformPoint(markers[$"Row{row}SizeMax"].position);
                        Vector3 size = max - min;
                        layout.cellSizes[row] = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                    }
                }
                if (arcade) root.name = "ArcadeMachine";
                if (atm) root.name = "AtmMachine";
                if (customerService)
                {
                    root.name = "CustomerServiceDesk";
                    Transform seatMarker = new GameObject("StaffSeat").transform;
                    seatMarker.SetParent(root.transform, false);
                    seatMarker.position = seat.position;
                    Transform facingMarker = new GameObject("StaffFacing").transform;
                    facingMarker.SetParent(root.transform, false);
                    facingMarker.position = facing.position;
                }
                if (cashier)
                {
                    root.name = "Cashier";
                    // Leave body clearance outside the counter, aligned with its register.
                    Transform staff = new GameObject("StaffStand").transform;
                    staff.SetParent(root.transform, false);
                    staff.localPosition = new Vector3(seat.position.x, 0.05f, target.max.z + 0.4f);
                    Transform customer = new GameObject("CustomerStand").transform;
                    customer.SetParent(root.transform, false);
                    customer.localPosition = new Vector3(facing.position.x, 0.05f, target.min.z - 0.4f);
                }
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    Material surfaceMaterial = material;
                    if (shelfType.HasValue && (shelfType.Value == FurnitureType.Fridge || shelfType.Value == FurnitureType.OrganicFridge || shelfType.Value == FurnitureType.Freezer) &&
                        renderer.name.StartsWith("ColdInterior", StringComparison.Ordinal))
                    {
                        string interiorPath = folder + "ColdInterior.mat";
                        surfaceMaterial = AssetDatabase.LoadAssetAtPath<Material>(interiorPath);
                        if (surfaceMaterial == null)
                        {
                            surfaceMaterial = new Material(shader);
                            surfaceMaterial.SetColor("_BaseColor", new Color(0.74f, 0.79f, 0.81f));
                            surfaceMaterial.SetFloat("_Metallic", 0.15f);
                            surfaceMaterial.SetFloat("_Smoothness", 0.4f);
                            surfaceMaterial.enableInstancing = true;
                            AssetDatabase.CreateAsset(surfaceMaterial, interiorPath);
                        }
                    }
                    var materials = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < materials.Length; i++) materials[i] = surfaceMaterial;
                    renderer.sharedMaterials = materials;
                }
                // Use the original machine's simple collision envelope, not an expensive mesh collider.
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = target.center;
                collider.size = target.size;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log("[WorkshopModels] " + (shelfType.HasValue ? shelfType.Value.ToString() : trashBin ? "TrashCan" : basket ? "ShoppingBasket" : cashier ? "Cashier" : customerService ? "CustomerServiceDesk" : type.ToString()) + " hazır: " + sourceId);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            AssetDatabase.SaveAssets();
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException(root.name + " modelinde görünür mesh yok.");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }

    public class WorkshopModelBuildSetup : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;
        public void OnPreprocessBuild(BuildReport report)
        {
            WorkshopModelImportSetup.Generate(false);
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/AquariumGrand.prefab") == null)
                throw new BuildFailedException("Akvaryum modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/MassageChair.prefab") == null)
                throw new BuildFailedException("Masaj koltuğu modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/Jukebox.prefab") == null)
                throw new BuildFailedException("Müzik dolabı modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/DonutDispenser.prefab") == null)
                throw new BuildFailedException("Donut dolabı modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/IceCreamCart.prefab") == null)
                throw new BuildFailedException("Dondurma arabası modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/SlushieMachine.prefab") == null)
                throw new BuildFailedException("Buzlu içecek otomatı modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/GumballMachine.prefab") == null)
                throw new BuildFailedException("Şeker otomatı modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/VendingSnack.prefab") == null)
                throw new BuildFailedException("Atıştırmalık otomatı modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/CoffeeMachine.prefab") == null)
                throw new BuildFailedException("Kahve otomatı modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/WaterDispenser.prefab") == null)
                throw new BuildFailedException("Su sebili modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/GoldenStatue.prefab") == null)
                throw new BuildFailedException("Altın başarı heykeli modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/ArcadeMachine.prefab") == null)
                throw new BuildFailedException("Arcade modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/PlantPot.prefab") == null)
                throw new BuildFailedException("Saksı çiçeği modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/AtmMachine.prefab") == null)
                throw new BuildFailedException("ATM modeli hazırlanamadı.");
            foreach (FurnitureType shelf in WorkshopModelImportSetup.DisplayShelves)
                if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/" + shelf + ".prefab") == null)
                    throw new BuildFailedException("Raf modeli hazırlanamadı: " + shelf);
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/TrashCan.prefab") == null)
                throw new BuildFailedException("Çöp kovası modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/ShoppingBasket.prefab") == null)
                throw new BuildFailedException("Alışveriş sepeti modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/Cashier.prefab") == null)
                throw new BuildFailedException("Kasa modeli hazırlanamadı.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Furniture/CustomerServiceDesk.prefab") == null)
                throw new BuildFailedException("Müşteri hizmetleri modeli hazırlanamadı.");
            foreach (var type in new[] { WorkshopMachineType.JamMaker, WorkshopMachineType.JuiceExtractor, WorkshopMachineType.Cannery, WorkshopMachineType.Dehydrator, WorkshopMachineType.SaladStation, WorkshopMachineType.OilPress })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(WorkshopModelImportSetup.PrefabRoot + type + ".prefab") == null)
                    throw new BuildFailedException("Atölye modeli hazırlanamadı: " + type);
        }
    }
}
