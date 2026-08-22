using System.Collections.Generic;
using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Hem dükkan raflarında (Shelf, Fridge, Freezer, Counters) hem de müşterilerin alışveriş sepetlerinde (Shopping Basket)
    /// tüm ürün kategorileri için %100 özgün, yüksek detaylı, hazır asset kalitesinde Low-Poly 3D modeller ve zengin PBR materyaller üreten merkez sınıf.
    /// </summary>
    public static class Procedural3DProductBuilder
    {
        private static Shader litShader;

        private static Shader GetLitShader()
        {
            if (litShader == null)
            {
                litShader = Shader.Find("Universal Render Pipeline/Lit") 
                         ?? Shader.Find("Lightweight Render Pipeline/Lit") 
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Color");
            }
            return litShader;
        }

        private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        public static Material CreateMaterial(Color color, float metallic = 0.1f, float smoothness = 0.5f, bool isTransparent = false)
        {
            string key = $"{color.r:F3}_{color.g:F3}_{color.b:F3}_{color.a:F3}_{metallic:F2}_{smoothness:F2}_{isTransparent}";
            if (matCache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            Shader s = GetLitShader();
            if (s == null) return null;

            Material mat = new Material(s)
            {
                name = "ProdMat_" + key,
                color = color
            };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (isTransparent || color.a < 0.99f)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }

            matCache[key] = mat;
            return mat;
        }

        public static void CreateProduct3DMesh(Transform parent, string productName, Vector3 localPos, Quaternion localRot, float scaleFactor = 1.0f, bool isStorageShelf = false)
        {
            if (string.IsNullOrEmpty(productName) || productName == "Boş" || productName.StartsWith("Ürün")) return;

            GameObject itemObj = new GameObject("Product_" + productName);
            itemObj.transform.SetParent(parent, false);
            itemObj.transform.localPosition = localPos;
            itemObj.transform.localRotation = localRot;

            if (isStorageShelf)
            {
                BuildWholesaleBox(itemObj.transform, productName, scaleFactor);
                return;
            }

            BuildSpecificProductModel(itemObj.transform, productName, scaleFactor);
        }

        public static void CreateProduct3DMesh(Transform parent, string productName, Vector3 localPos, bool isStorageShelf)
        {
            CreateProduct3DMesh(parent, productName, localPos, Quaternion.identity, 1.0f, isStorageShelf);
        }

        public static void CreateBasketProduct3DMesh(Transform parent, string productName, Vector3 localPos, int itemIndex)
        {
            GameObject itemObj = new GameObject("BasketItem_" + itemIndex + "_" + productName);
            itemObj.transform.SetParent(parent, false);
            itemObj.transform.localPosition = localPos;

            float randomYRot = (itemIndex * 37f) % 360f;
            itemObj.transform.localRotation = Quaternion.Euler(0f, randomYRot, 0f);

            if (string.IsNullOrEmpty(productName) || productName == "Boş" || productName.StartsWith("Ürün"))
            {
                string[] fallbackProducts = new string[] { "Somun Ekmek", "Tam Yağlı Süt", "Sütlü Çikolata", "Baharatlı Patates Cipsi", "Besleyici Şampuan", "Domates Salçası" };
                productName = fallbackProducts[itemIndex % fallbackProducts.Length];
            }

            BuildSpecificProductModel(itemObj.transform, productName, scaleFactor: 0.55f);
        }

        private static void BuildWholesaleBox(Transform parent, string productName, float scaleFactor)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "WholesaleBox";
            box.transform.SetParent(parent, false);
            box.transform.localPosition = new Vector3(0f, 0.08f * scaleFactor, 0f);
            box.transform.localScale = new Vector3(0.26f, 0.16f, 0.22f) * scaleFactor;
            ApplyMaterial(box, new Color(0.72f, 0.52f, 0.32f), 0.0f, 0.2f);
            DestroyCollider(box);

            // Koli Bandı
            GameObject tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tape.name = "BoxTape";
            tape.transform.SetParent(box.transform, false);
            tape.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            tape.transform.localScale = new Vector3(0.28f, 0.02f, 1.01f);
            ApplyMaterial(tape, new Color(0.88f, 0.74f, 0.42f), 0.0f, 0.4f);
            DestroyCollider(tape);

            // Ürün Etiket Kartı
            GameObject label = GameObject.CreatePrimitive(PrimitiveType.Cube);
            label.name = "ProductLabel";
            label.transform.SetParent(box.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.0f, -0.51f);
            label.transform.localScale = new Vector3(0.65f, 0.55f, 0.02f);
            ApplyMaterial(label, GetProductCategoryColor(productName), 0.1f, 0.6f);
            DestroyCollider(label);

            // Barkod Şeridi
            GameObject barcode = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barcode.name = "Barcode";
            barcode.transform.SetParent(label.transform, false);
            barcode.transform.localPosition = new Vector3(0f, -0.25f, -0.6f);
            barcode.transform.localScale = new Vector3(0.70f, 0.22f, 0.1f);
            ApplyMaterial(barcode, new Color(0.10f, 0.10f, 0.10f), 0.0f, 0.1f);
            DestroyCollider(barcode);
        }

        private static void BuildSpecificProductModel(Transform parent, string pName, float scaleFactor)
        {
            string p = pName.ToLower();

            // ==================== 1. MANAV & TARLA HASATLARI (Fresh Produce & Crops) ====================
            if (p.Contains("domates"))
            {
                // Domates (Parlak kırmızı küre + 5 yapraklı yeşil taç + sap)
                GameObject tom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tom.transform.SetParent(parent, false);
                tom.transform.localPosition = new Vector3(0f, 0.045f * scaleFactor, 0f);
                tom.transform.localScale = new Vector3(0.095f, 0.090f, 0.095f) * scaleFactor;
                ApplyMaterial(tom, new Color(0.95f, 0.16f, 0.12f), 0.15f, 0.85f);
                DestroyCollider(tom);

                GameObject calyx = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                calyx.transform.SetParent(tom.transform, false);
                calyx.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                calyx.transform.localScale = new Vector3(0.40f, 0.05f, 0.40f);
                ApplyMaterial(calyx, new Color(0.18f, 0.72f, 0.22f), 0.0f, 0.5f);
                DestroyCollider(calyx);

                GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.transform.SetParent(tom.transform, false);
                stem.transform.localPosition = new Vector3(0f, 0.60f, 0f);
                stem.transform.localScale = new Vector3(0.10f, 0.22f, 0.10f);
                ApplyMaterial(stem, new Color(0.12f, 0.55f, 0.16f), 0.0f, 0.4f);
                DestroyCollider(stem);
            }
            else if (p.Contains("salatalık") || p.Contains("pırasa") || p.Contains("kabak") || p.Contains("zucchini"))
            {
                // Salatalık / Kabak (Hafif eğimli koyu yeşil silindir)
                GameObject cuc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                cuc.transform.SetParent(parent, false);
                cuc.transform.localPosition = new Vector3(0f, 0.035f * scaleFactor, 0f);
                cuc.transform.localScale = new Vector3(0.045f, 0.085f, 0.045f) * scaleFactor;
                cuc.transform.localRotation = Quaternion.Euler(0f, 15f, 75f);
                Color cCol = p.Contains("pırasa") ? new Color(0.55f, 0.85f, 0.45f) : (p.Contains("kabak") ? new Color(0.15f, 0.65f, 0.35f) : new Color(0.12f, 0.68f, 0.25f));
                ApplyMaterial(cuc, cCol, 0.05f, 0.70f);
                DestroyCollider(cuc);
            }
            else if (p.Contains("çilek"))
            {
                // Çilek (Koni kırmızı meyve + yeşil yaka + sarı tohum noktaları)
                GameObject str = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                str.transform.SetParent(parent, false);
                str.transform.localPosition = new Vector3(0f, 0.035f * scaleFactor, 0f);
                str.transform.localScale = new Vector3(0.065f, 0.085f, 0.065f) * scaleFactor;
                ApplyMaterial(str, new Color(0.98f, 0.12f, 0.32f), 0.1f, 0.85f);
                DestroyCollider(str);

                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crown.transform.SetParent(str.transform, false);
                crown.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                crown.transform.localScale = new Vector3(0.48f, 0.06f, 0.48f);
                ApplyMaterial(crown, new Color(0.20f, 0.80f, 0.28f), 0.0f, 0.5f);
                DestroyCollider(crown);
            }
            else if (p.Contains("havuç") || p.Contains("turp") || p.Contains("şalgam") || p.Contains("pancar"))
            {
                // Havuç / Turp / Kök Sebzeler
                GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                root.transform.SetParent(parent, false);
                root.transform.localPosition = new Vector3(0f, 0.035f * scaleFactor, 0f);
                root.transform.localScale = new Vector3(0.045f, 0.080f, 0.045f) * scaleFactor;
                root.transform.localRotation = Quaternion.Euler(0f, 20f, 75f);
                Color rCol = p.Contains("havuç") ? new Color(0.98f, 0.52f, 0.08f) : (p.Contains("pancar") ? new Color(0.68f, 0.08f, 0.22f) : new Color(0.85f, 0.25f, 0.70f));
                ApplyMaterial(root, rCol, 0.05f, 0.60f);
                DestroyCollider(root);

                GameObject leafy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leafy.transform.SetParent(root.transform, false);
                leafy.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                leafy.transform.localScale = new Vector3(0.35f, 0.40f, 0.15f);
                ApplyMaterial(leafy, new Color(0.20f, 0.78f, 0.25f), 0.0f, 0.4f);
                DestroyCollider(leafy);
            }
            else if (p.Contains("marul") || p.Contains("lahana") || p.Contains("ıspanak") || p.Contains("brokoli") || p.Contains("karnabahar") || p.Contains("enginar"))
            {
                // Marul / Lahana / Brokoli (Katmanlı zengin yapraklı gövde)
                GameObject leafy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leafy.transform.SetParent(parent, false);
                leafy.transform.localPosition = new Vector3(0f, 0.045f * scaleFactor, 0f);
                leafy.transform.localScale = new Vector3(0.10f, 0.09f, 0.10f) * scaleFactor;
                Color lCol = p.Contains("brokoli") ? new Color(0.18f, 0.55f, 0.22f) : (p.Contains("karnabahar") ? new Color(0.92f, 0.94f, 0.88f) : new Color(0.42f, 0.85f, 0.25f));
                ApplyMaterial(leafy, lCol, 0.0f, 0.4f);
                DestroyCollider(leafy);

                GameObject heart = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                heart.transform.SetParent(leafy.transform, false);
                heart.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                heart.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
                ApplyMaterial(heart, new Color(lCol.r * 1.15f, lCol.g * 1.15f, lCol.b * 1.15f), 0.0f, 0.5f);
                DestroyCollider(heart);
            }
            else if (p.Contains("karpuz") || p.Contains("kavun") || p.Contains("balkabağı"))
            {
                // Karpuz / Kavun / Balkabağı (Büyük parlak küre + çizgiler)
                GameObject big = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                big.transform.SetParent(parent, false);
                big.transform.localPosition = new Vector3(0f, 0.06f * scaleFactor, 0f);
                big.transform.localScale = new Vector3(0.13f, 0.12f, 0.13f) * scaleFactor;
                Color bCol = p.Contains("karpuz") ? new Color(0.12f, 0.62f, 0.24f) : (p.Contains("kavun") ? new Color(0.94f, 0.82f, 0.25f) : new Color(0.95f, 0.48f, 0.08f));
                ApplyMaterial(big, bCol, 0.1f, 0.70f);
                DestroyCollider(big);

                if (p.Contains("karpuz"))
                {
                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stripe.transform.SetParent(big.transform, false);
                    stripe.transform.localPosition = Vector3.zero;
                    stripe.transform.localScale = new Vector3(1.02f, 0.20f, 1.02f);
                    ApplyMaterial(stripe, new Color(0.08f, 0.42f, 0.15f), 0.1f, 0.70f);
                    DestroyCollider(stripe);
                }
            }
            else if (p.Contains("patates") || p.Contains("soğan") || p.Contains("sarımsak"))
            {
                // Patates / Soğan / Sarımsak
                GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                root.transform.SetParent(parent, false);
                root.transform.localPosition = new Vector3(0f, 0.038f * scaleFactor, 0f);
                root.transform.localScale = new Vector3(0.08f, 0.07f, 0.08f) * scaleFactor;
                Color bulbCol = p.Contains("patates") ? new Color(0.78f, 0.54f, 0.28f) : (p.Contains("soğan") ? new Color(0.90f, 0.68f, 0.30f) : new Color(0.94f, 0.94f, 0.96f));
                ApplyMaterial(root, bulbCol, 0.0f, 0.35f);
                DestroyCollider(root);
            }
            else if (p.Contains("mısır") || p.Contains("patlıcan") || p.Contains("biber") || p.Contains("üzüm"))
            {
                // Mısır / Patlıcan / Biber / Üzüm
                GameObject veg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                veg.transform.SetParent(parent, false);
                veg.transform.localPosition = new Vector3(0f, 0.045f * scaleFactor, 0f);
                veg.transform.localScale = new Vector3(0.055f, 0.090f, 0.055f) * scaleFactor;
                Color vegCol = p.Contains("mısır") ? new Color(0.96f, 0.86f, 0.18f) : (p.Contains("patlıcan") ? new Color(0.42f, 0.12f, 0.52f) : (p.Contains("üzüm") ? new Color(0.45f, 0.15f, 0.75f) : new Color(0.90f, 0.18f, 0.15f)));
                ApplyMaterial(veg, vegCol, 0.15f, 0.75f);
                DestroyCollider(veg);

                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.transform.SetParent(veg.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                cap.transform.localScale = new Vector3(0.55f, 0.10f, 0.55f);
                ApplyMaterial(cap, new Color(0.20f, 0.72f, 0.25f), 0.0f, 0.5f);
                DestroyCollider(cap);
            }

            // ==================== 2. FIRIN & UNLU MAMÜLLER (Bakery & Pastry) ====================
            else if (p.Contains("ekmek"))
            {
                // Somun Ekmek (Taş fırın altın kabuk + 3 adet unlu çizik)
                GameObject loaf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                loaf.transform.SetParent(parent, false);
                loaf.transform.localPosition = new Vector3(0f, 0.045f * scaleFactor, 0f);
                loaf.transform.localScale = new Vector3(0.095f, 0.065f, 0.075f) * scaleFactor;
                loaf.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                ApplyMaterial(loaf, new Color(0.86f, 0.54f, 0.20f), 0.0f, 0.35f);
                DestroyCollider(loaf);

                for (int i = -1; i <= 1; i++)
                {
                    GameObject slit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    slit.transform.SetParent(parent, false);
                    slit.transform.localPosition = new Vector3(i * 0.025f * scaleFactor, 0.072f * scaleFactor, 0f);
                    slit.transform.localScale = new Vector3(0.012f, 0.010f, 0.065f) * scaleFactor;
                    ApplyMaterial(slit, new Color(0.96f, 0.88f, 0.68f), 0.0f, 0.5f);
                    DestroyCollider(slit);
                }
            }
            else if (p.Contains("simit"))
            {
                // Çıtır Sokak Simiti (Halka form + susam dokusu)
                GameObject simit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                simit.transform.SetParent(parent, false);
                simit.transform.localPosition = new Vector3(0f, 0.022f * scaleFactor, 0f);
                simit.transform.localScale = new Vector3(0.11f, 0.025f, 0.11f) * scaleFactor;
                ApplyMaterial(simit, new Color(0.78f, 0.44f, 0.15f), 0.0f, 0.4f);
                DestroyCollider(simit);

                GameObject hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hole.transform.SetParent(simit.transform, false);
                hole.transform.localPosition = Vector3.zero;
                hole.transform.localScale = new Vector3(0.46f, 1.25f, 0.46f);
                ApplyMaterial(hole, new Color(0.18f, 0.18f, 0.20f), 0.0f, 0.0f);
                DestroyCollider(hole);
            }
            else if (p.Contains("kruvasan") || p.Contains("poğaça") || p.Contains("börek"))
            {
                // Kruvasan / Poğaça (Altın tereyağlı hilal)
                GameObject past = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                past.transform.SetParent(parent, false);
                past.transform.localPosition = new Vector3(0f, 0.035f * scaleFactor, 0f);
                past.transform.localScale = new Vector3(0.11f, 0.055f, 0.085f) * scaleFactor;
                ApplyMaterial(past, new Color(0.92f, 0.64f, 0.20f), 0.1f, 0.65f);
                DestroyCollider(past);
            }
            else if (p.Contains("pasta") || p.Contains("kek"))
            {
                // Çikolatalı Pasta Dilimi (Üçgen dilim + vişne süsü)
                GameObject slice = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slice.transform.SetParent(parent, false);
                slice.transform.localPosition = new Vector3(0f, 0.045f * scaleFactor, 0f);
                slice.transform.localScale = new Vector3(0.09f, 0.07f, 0.09f) * scaleFactor;
                slice.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                ApplyMaterial(slice, new Color(0.38f, 0.20f, 0.12f), 0.1f, 0.7f);
                DestroyCollider(slice);

                GameObject cherry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cherry.transform.SetParent(slice.transform, false);
                cherry.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                cherry.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                ApplyMaterial(cherry, new Color(0.95f, 0.15f, 0.20f), 0.2f, 0.8f);
                DestroyCollider(cherry);
            }

            // ==================== 3. SÜTLÜK & ŞARKÜTERİ (Dairy, Beverages & Eggs) ====================
            else if (p.Contains("süt"))
            {
                // Tam Yağlı Süt (Tetra Pak Kutu + Mavi Şerit + Mavi Kapak)
                GameObject carton = GameObject.CreatePrimitive(PrimitiveType.Cube);
                carton.transform.SetParent(parent, false);
                carton.transform.localPosition = new Vector3(0f, 0.075f * scaleFactor, 0f);
                carton.transform.localScale = new Vector3(0.075f, 0.145f, 0.075f) * scaleFactor;
                ApplyMaterial(carton, new Color(0.96f, 0.96f, 0.98f), 0.0f, 0.75f);
                DestroyCollider(carton);

                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.transform.SetParent(carton.transform, false);
                band.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                band.transform.localScale = new Vector3(1.02f, 0.38f, 1.02f);
                ApplyMaterial(band, new Color(0.12f, 0.52f, 0.90f), 0.1f, 0.6f);
                DestroyCollider(band);

                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.transform.SetParent(carton.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.53f, 0f);
                cap.transform.localScale = new Vector3(0.40f, 0.07f, 0.40f);
                ApplyMaterial(cap, new Color(0.10f, 0.45f, 0.88f), 0.1f, 0.8f);
                DestroyCollider(cap);
            }
            else if (p.Contains("peynir") || p.Contains("kaşar") || p.Contains("tereyağı"))
            {
                // Peynir / Kaşar / Tereyağı Kalıbı
                GameObject cheese = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cheese.transform.SetParent(parent, false);
                cheese.transform.localPosition = new Vector3(0f, 0.038f * scaleFactor, 0f);
                cheese.transform.localScale = new Vector3(0.11f, 0.065f, 0.085f) * scaleFactor;
                Color chCol = p.Contains("tereyağı") ? new Color(0.95f, 0.82f, 0.22f) : new Color(0.96f, 0.88f, 0.35f);
                ApplyMaterial(cheese, chCol, 0.1f, 0.5f);
                DestroyCollider(cheese);
            }
            else if (p.Contains("yumurta"))
            {
                // Yumurta Kolisi (Karton Viyol + Yumurtalar)
                GameObject tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tray.transform.SetParent(parent, false);
                tray.transform.localPosition = new Vector3(0f, 0.025f * scaleFactor, 0f);
                tray.transform.localScale = new Vector3(0.12f, 0.035f, 0.09f) * scaleFactor;
                ApplyMaterial(tray, new Color(0.70f, 0.56f, 0.44f), 0.0f, 0.25f);
                DestroyCollider(tray);

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        GameObject egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        egg.transform.SetParent(parent, false);
                        egg.transform.localPosition = new Vector3(x * 0.028f * scaleFactor, 0.048f * scaleFactor, z * 0.022f * scaleFactor);
                        egg.transform.localScale = new Vector3(0.034f, 0.044f, 0.034f) * scaleFactor;
                        ApplyMaterial(egg, new Color(0.94f, 0.86f, 0.76f), 0.0f, 0.45f);
                        DestroyCollider(egg);
                    }
                }
            }
            else if (p.Contains("meyve suyu") || p.Contains("çay") || p.Contains("su") || p.Contains("kola") || p.Contains("gazoz") || p.Contains("enerji"))
            {
                // Meşrubat & Şişe / Kutu
                GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bot.transform.SetParent(parent, false);
                bot.transform.localPosition = new Vector3(0f, 0.075f * scaleFactor, 0f);
                bot.transform.localScale = new Vector3(0.065f, 0.135f, 0.065f) * scaleFactor;
                Color drinkCol = p.Contains("enerji") ? new Color(0.15f, 0.85f, 0.95f) : (p.Contains("meyve") ? new Color(0.96f, 0.55f, 0.15f) : (p.Contains("su") ? new Color(0.35f, 0.80f, 0.98f) : new Color(0.85f, 0.18f, 0.18f)));
                ApplyMaterial(bot, drinkCol, 0.35f, 0.85f);
                DestroyCollider(bot);

                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.transform.SetParent(bot.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                cap.transform.localScale = new Vector3(0.70f, 0.10f, 0.70f);
                ApplyMaterial(cap, new Color(0.92f, 0.92f, 0.92f), 0.6f, 0.9f);
                DestroyCollider(cap);
            }

            // ==================== 4. KASAP & TAZE ET (Meat & Butcher) ====================
            else if (p.Contains("et") || p.Contains("kıyma") || p.Contains("kuşbaşı") || p.Contains("antrikot") || p.Contains("tavuk") || p.Contains("pirzola") || p.Contains("köfte"))
            {
                // Siyah Kasap Tepsisi + Taze Kırmızı/Pembe Et
                GameObject tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tray.transform.SetParent(parent, false);
                tray.transform.localPosition = new Vector3(0f, 0.018f * scaleFactor, 0f);
                tray.transform.localScale = new Vector3(0.13f, 0.022f, 0.095f) * scaleFactor;
                ApplyMaterial(tray, new Color(0.12f, 0.12f, 0.14f), 0.1f, 0.6f);
                DestroyCollider(tray);

                GameObject meat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meat.transform.SetParent(parent, false);
                meat.transform.localPosition = new Vector3(0f, 0.040f * scaleFactor, 0f);
                meat.transform.localScale = new Vector3(0.115f, 0.038f, 0.080f) * scaleFactor;
                Color mCol = p.Contains("tavuk") ? new Color(0.95f, 0.80f, 0.75f) : new Color(0.85f, 0.18f, 0.22f);
                ApplyMaterial(meat, mCol, 0.15f, 0.55f);
                DestroyCollider(meat);
            }
            else if (p.Contains("sucuk"))
            {
                // Kangal Sucuk (Kırmızı halka)
                GameObject sausage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sausage.transform.SetParent(parent, false);
                sausage.transform.localPosition = new Vector3(0f, 0.025f * scaleFactor, 0f);
                sausage.transform.localScale = new Vector3(0.10f, 0.028f, 0.10f) * scaleFactor;
                ApplyMaterial(sausage, new Color(0.75f, 0.18f, 0.15f), 0.1f, 0.55f);
                DestroyCollider(sausage);
            }

            // ==================== 5. DONUK GIDALAR (Freezer Items) ====================
            else if (p.Contains("pizza"))
            {
                // Donuk Pizza Kutusu
                GameObject pBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pBox.transform.SetParent(parent, false);
                pBox.transform.localPosition = new Vector3(0f, 0.020f * scaleFactor, 0f);
                pBox.transform.localScale = new Vector3(0.13f, 0.025f, 0.13f) * scaleFactor;
                ApplyMaterial(pBox, new Color(0.92f, 0.45f, 0.15f), 0.0f, 0.5f);
                DestroyCollider(pBox);
            }
            else if (p.Contains("dondurma"))
            {
                // Dondurma Kutusu
                GameObject tub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tub.transform.SetParent(parent, false);
                tub.transform.localPosition = new Vector3(0f, 0.045f * scaleFactor, 0f);
                tub.transform.localScale = new Vector3(0.085f, 0.075f, 0.085f) * scaleFactor;
                ApplyMaterial(tub, new Color(0.20f, 0.72f, 0.92f), 0.1f, 0.7f);
                DestroyCollider(tub);
            }

            // ==================== 6. KURU GIDA, BAKLİYAT VE AMBALAJLAR (Dry Goods & Pantry) ====================
            else if (p.Contains("makarna"))
            {
                // Çubuk Makarna Paketi
                GameObject pack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pack.transform.SetParent(parent, false);
                pack.transform.localPosition = new Vector3(0f, 0.025f * scaleFactor, 0f);
                pack.transform.localScale = new Vector3(0.14f, 0.035f, 0.07f) * scaleFactor;
                ApplyMaterial(pack, new Color(0.12f, 0.35f, 0.78f), 0.1f, 0.6f);
                DestroyCollider(pack);
            }
            else if (p.Contains("pirinç") || p.Contains("un") || p.Contains("şeker") || p.Contains("fasulye") || p.Contains("mercimek"))
            {
                // Bakliyat Kese Kağıdı / Torba
                GameObject sack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sack.transform.SetParent(parent, false);
                sack.transform.localPosition = new Vector3(0f, 0.065f * scaleFactor, 0f);
                sack.transform.localScale = new Vector3(0.09f, 0.12f, 0.07f) * scaleFactor;
                Color sCol = p.Contains("pirinç") ? new Color(0.92f, 0.92f, 0.95f) : (p.Contains("mercimek") ? new Color(0.92f, 0.45f, 0.18f) : new Color(0.85f, 0.75f, 0.58f));
                ApplyMaterial(sack, sCol, 0.0f, 0.4f);
                DestroyCollider(sack);
            }
            else if (p.Contains("yağ") || p.Contains("zeytinyağı"))
            {
                // Sıvı Yağ Şişesi
                GameObject oil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                oil.transform.SetParent(parent, false);
                oil.transform.localPosition = new Vector3(0f, 0.075f * scaleFactor, 0f);
                oil.transform.localScale = new Vector3(0.06f, 0.14f, 0.06f) * scaleFactor;
                Color oCol = p.Contains("sızma") ? new Color(0.35f, 0.68f, 0.18f) : new Color(0.95f, 0.85f, 0.15f);
                ApplyMaterial(oil, oCol, 0.3f, 0.85f);
                DestroyCollider(oil);
            }
            else if (p.Contains("salça") || p.Contains("konserve"))
            {
                // Salça Teneke Konserve
                GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                can.transform.SetParent(parent, false);
                can.transform.localPosition = new Vector3(0f, 0.045f * scaleFactor, 0f);
                can.transform.localScale = new Vector3(0.075f, 0.08f, 0.075f) * scaleFactor;
                ApplyMaterial(can, new Color(0.88f, 0.15f, 0.15f), 0.4f, 0.75f);
                DestroyCollider(can);
            }
            else if (p.Contains("çikolata") || p.Contains("bisküvi") || p.Contains("cips") || p.Contains("kaju") || p.Contains("fıstık"))
            {
                // Atıştırmalık Paketleri
                GameObject snk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                snk.transform.SetParent(parent, false);
                snk.transform.localPosition = new Vector3(0f, 0.035f * scaleFactor, 0f);
                snk.transform.localScale = new Vector3(0.10f, 0.05f, 0.07f) * scaleFactor;
                Color snkCol = p.Contains("çikolata") ? new Color(0.42f, 0.22f, 0.14f) : (p.Contains("cips") ? new Color(0.95f, 0.78f, 0.15f) : new Color(0.88f, 0.45f, 0.20f));
                ApplyMaterial(snk, snkCol, 0.1f, 0.6f);
                DestroyCollider(snk);
            }

            // ==================== 7. KOZMETİK (Cosmetics & Personal Care) ====================
            else if (p.Contains("şampuan") || p.Contains("sabun") || p.Contains("krem") || p.Contains("parfüm") || p.Contains("serum") || p.Contains("macun"))
            {
                // Kozmetik Şişe / Pompa
                GameObject cos = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cos.transform.SetParent(parent, false);
                cos.transform.localPosition = new Vector3(0f, 0.065f * scaleFactor, 0f);
                cos.transform.localScale = new Vector3(0.06f, 0.11f, 0.06f) * scaleFactor;
                Color cosCol = p.Contains("parfüm") ? new Color(0.95f, 0.82f, 0.35f) : (p.Contains("şampuan") ? new Color(0.85f, 0.35f, 0.75f) : new Color(0.20f, 0.75f, 0.85f));
                ApplyMaterial(cos, cosCol, 0.25f, 0.85f);
                DestroyCollider(cos);

                GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pump.transform.SetParent(cos.transform, false);
                pump.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                pump.transform.localScale = new Vector3(0.35f, 0.18f, 0.55f);
                ApplyMaterial(pump, new Color(0.95f, 0.95f, 0.95f), 0.3f, 0.8f);
                DestroyCollider(pump);
            }

            // ==================== 8. ELEKTRONİK (Electronics & Tech) ====================
            else if (p.Contains("kulaklık") || p.Contains("powerbank") || p.Contains("fare") || p.Contains("saat") || p.Contains("hoparlör") || p.Contains("bellek") || p.Contains("kablo"))
            {
                // Teknoloji Kutusu (Mat Siyah + Neon Mavi LED)
                GameObject tech = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tech.transform.SetParent(parent, false);
                tech.transform.localPosition = new Vector3(0f, 0.045f * scaleFactor, 0f);
                tech.transform.localScale = new Vector3(0.10f, 0.065f, 0.08f) * scaleFactor;
                ApplyMaterial(tech, new Color(0.12f, 0.15f, 0.20f), 0.75f, 0.90f);
                DestroyCollider(tech);

                GameObject led = GameObject.CreatePrimitive(PrimitiveType.Cube);
                led.transform.SetParent(tech.transform, false);
                led.transform.localPosition = new Vector3(0f, 0.51f, 0f);
                led.transform.localScale = new Vector3(0.75f, 0.02f, 0.15f);
                ApplyMaterial(led, new Color(0.15f, 0.75f, 0.95f), 0.1f, 0.95f);
                DestroyCollider(led);
            }

            // ==================== 9. STANDART DİĞER ÜRÜNLER ====================
            else
            {
                GameObject gen = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gen.transform.SetParent(parent, false);
                gen.transform.localPosition = new Vector3(0f, 0.05f * scaleFactor, 0f);
                gen.transform.localScale = new Vector3(0.09f, 0.08f, 0.08f) * scaleFactor;
                ApplyMaterial(gen, GetProductCategoryColor(pName), 0.1f, 0.55f);
                DestroyCollider(gen);
            }
        }

        public static Color GetProductCategoryColor(string pName)
        {
            if (string.IsNullOrEmpty(pName)) return Color.gray;
            string p = pName.ToLower();

            if (p.Contains("ekmek") || p.Contains("simit") || p.Contains("kruvasan") || p.Contains("poğaça")) return new Color(0.88f, 0.58f, 0.22f);
            if (p.Contains("süt") || p.Contains("yumurta")) return new Color(0.95f, 0.95f, 0.98f);
            if (p.Contains("peynir") || p.Contains("tereyağı")) return new Color(0.96f, 0.85f, 0.22f);
            if (p.Contains("et") || p.Contains("kıyma") || p.Contains("sucuk")) return new Color(0.85f, 0.18f, 0.22f);
            if (p.Contains("meyve") || p.Contains("şeftali")) return new Color(0.96f, 0.55f, 0.15f);
            if (p.Contains("kulaklık") || p.Contains("powerbank") || p.Contains("saat")) return new Color(0.15f, 0.75f, 0.95f);
            if (p.Contains("şampuan") || p.Contains("krem") || p.Contains("parfüm")) return new Color(0.85f, 0.35f, 0.75f);
            if (p.Contains("çikolata") || p.Contains("cips")) return new Color(0.92f, 0.22f, 0.18f);

            return new Color(0.30f, 0.65f, 0.85f);
        }

        private static void ApplyMaterial(GameObject obj, Color color, float metallic = 0.1f, float smoothness = 0.5f)
        {
            if (obj == null) return;
            Renderer r = obj.GetComponent<Renderer>();
            if (r == null) return;
            r.sharedMaterial = CreateMaterial(color, metallic, smoothness);
        }

        private static void DestroyCollider(GameObject obj)
        {
            if (obj == null) return;
            Collider col = obj.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
    }
}
