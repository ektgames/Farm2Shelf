using UnityEngine;

namespace Farm2Shelf.Environment
{
    /// <summary>
    /// Hem dükkan raflarında (Shelf, Fridge, Freezer, Counter) hem de müşterilerin alışveriş sepetlerinde (Shopping Basket)
    /// tüm ürün kategorileri için %100 özgün, yüksek kaliteli Low-Poly 3D modeller ve renk paletleri üreten merkez sınıf.
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
                         ?? Shader.Find("Standard");
            }
            return litShader;
        }

        private static readonly System.Collections.Generic.Dictionary<string, Material> matCache = new System.Collections.Generic.Dictionary<string, Material>();

        private static Material CreateMaterial(Color color, float metallic = 0.1f, float smoothness = 0.5f)
        {
            string key = $"{color.r:F3}_{color.g:F3}_{color.b:F3}_{color.a:F3}_{metallic:F2}_{smoothness:F2}";
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
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            matCache[key] = mat;
            return mat;
        }

        /// <summary>
        /// Dükkan raflarına yerleştirilecek 3D ürün modelini üretir.
        /// </summary>
        public static void CreateProduct3DMesh(Transform parent, string productName, Vector3 localPos, bool isStorageShelf)
        {
            if (string.IsNullOrEmpty(productName)) return;

            GameObject itemObj = new GameObject("Product_" + productName);
            itemObj.transform.SetParent(parent, false);
            itemObj.transform.localPosition = localPos;

            // 1. DEPO RAFI İÇİN TOPTANCI AMBALAJ KOLİSİ
            if (isStorageShelf)
            {
                BuildWholesaleBox(itemObj.transform, productName);
                return;
            }

            // 2. MAĞAZA VE REYON RAFLARI İÇİN DETAYLI LOW-POLY 3D MOBİL MODEL
            BuildSpecificProductModel(itemObj.transform, productName, scaleFactor: 1.0f);
        }

        /// <summary>
        /// Müşterinin alışveriş sepeti içine eklenen 3D mini ürün modelini üretir.
        /// </summary>
        public static void CreateBasketProduct3DMesh(Transform parent, string productName, Vector3 localPos, int itemIndex)
        {
            GameObject itemObj = new GameObject("BasketItem_" + itemIndex + "_" + productName);
            itemObj.transform.SetParent(parent, false);
            itemObj.transform.localPosition = localPos;

            // Rastgele hafif dönüş açısı vererek doğal istifleme görünümü sağla
            float randomYRot = (itemIndex * 37f) % 360f;
            itemObj.transform.localRotation = Quaternion.Euler(0f, randomYRot, 0f);

            if (string.IsNullOrEmpty(productName) || productName == "Boş" || productName.StartsWith("Ürün"))
            {
                // Varsayılan çeşitli renkli atıştırmalık kutuları
                string[] fallbackProducts = new string[] { "Somun Ekmek", "Tam Yağlı Süt", "Sütlü Çikolata", "Baharatlı Patates Cipsi", "Besleyici Şampuan", "Domates Salçası" };
                productName = fallbackProducts[itemIndex % fallbackProducts.Length];
            }

            BuildSpecificProductModel(itemObj.transform, productName, scaleFactor: 0.65f); // Sepet içi için ideal ölçek
        }

        private static void BuildWholesaleBox(Transform parent, string productName)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "WholesaleBox";
            box.transform.SetParent(parent, false);
            box.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            box.transform.localScale = new Vector3(0.24f, 0.14f, 0.20f);
            ApplyMaterial(box, new Color(0.74f, 0.54f, 0.34f), 0.0f, 0.2f);
            DestroyCollider(box);

            // Koli Bandı
            GameObject tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tape.name = "BoxTape";
            tape.transform.SetParent(box.transform, false);
            tape.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            tape.transform.localScale = new Vector3(0.30f, 0.02f, 1.01f);
            ApplyMaterial(tape, new Color(0.85f, 0.72f, 0.40f), 0.0f, 0.4f);
            DestroyCollider(tape);

            // Ön Ürün Renkli Etiketi
            GameObject label = GameObject.CreatePrimitive(PrimitiveType.Cube);
            label.name = "ProductLabel";
            label.transform.SetParent(box.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.0f, -0.51f);
            label.transform.localScale = new Vector3(0.60f, 0.50f, 0.02f);
            ApplyMaterial(label, GetProductCategoryColor(productName), 0.1f, 0.6f);
            DestroyCollider(label);
        }

        private static void BuildSpecificProductModel(Transform parent, string pName, float scaleFactor)
        {
            string p = pName.ToLower();

            // ==================== 0. MANAV VE TARLA HASAT MAHSULLERİ (Fresh Produce) ====================
            if (p.Contains("domates"))
            {
                GameObject tom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tom.transform.SetParent(parent, false);
                tom.transform.localPosition = new Vector3(0f, 0.06f * scaleFactor, 0f);
                tom.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f) * scaleFactor;
                ApplyMaterial(tom, new Color(0.92f, 0.18f, 0.15f), 0.1f, 0.7f);
                DestroyCollider(tom);

                GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.transform.SetParent(tom.transform, false);
                stem.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                stem.transform.localScale = new Vector3(0.30f, 0.10f, 0.30f);
                ApplyMaterial(stem, new Color(0.20f, 0.75f, 0.25f), 0.0f, 0.5f);
                DestroyCollider(stem);
            }
            else if (p.Contains("salatalık") || p.Contains("pırasa"))
            {
                GameObject cuc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cuc.transform.SetParent(parent, false);
                cuc.transform.localPosition = new Vector3(0f, 0.05f * scaleFactor, 0f);
                cuc.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f) * scaleFactor;
                cuc.transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
                ApplyMaterial(cuc, new Color(0.20f, 0.75f, 0.30f), 0.0f, 0.6f);
                DestroyCollider(cuc);
            }
            else if (p.Contains("çilek"))
            {
                GameObject str = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                str.transform.SetParent(parent, false);
                str.transform.localPosition = new Vector3(0f, 0.05f * scaleFactor, 0f);
                str.transform.localScale = new Vector3(0.11f, 0.14f, 0.11f) * scaleFactor;
                ApplyMaterial(str, new Color(0.95f, 0.15f, 0.35f), 0.1f, 0.8f);
                DestroyCollider(str);
            }
            else if (p.Contains("karpuz"))
            {
                GameObject wm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                wm.transform.SetParent(parent, false);
                wm.transform.localPosition = new Vector3(0f, 0.09f * scaleFactor, 0f);
                wm.transform.localScale = new Vector3(0.22f, 0.20f, 0.22f) * scaleFactor;
                ApplyMaterial(wm, new Color(0.15f, 0.65f, 0.25f), 0.1f, 0.6f);
                DestroyCollider(wm);
            }
            else if (p.Contains("kavun"))
            {
                GameObject melon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                melon.transform.SetParent(parent, false);
                melon.transform.localPosition = new Vector3(0f, 0.08f * scaleFactor, 0f);
                melon.transform.localScale = new Vector3(0.20f, 0.18f, 0.20f) * scaleFactor;
                ApplyMaterial(melon, new Color(0.95f, 0.82f, 0.20f), 0.1f, 0.6f);
                DestroyCollider(melon);
            }
            else if (p.Contains("balkabağı") || p.Contains("kabak"))
            {
                GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pump.transform.SetParent(parent, false);
                pump.transform.localPosition = new Vector3(0f, 0.08f * scaleFactor, 0f);
                pump.transform.localScale = new Vector3(0.22f, 0.17f, 0.22f) * scaleFactor;
                ApplyMaterial(pump, new Color(0.95f, 0.45f, 0.08f), 0.1f, 0.6f);
                DestroyCollider(pump);
            }
            else if (p.Contains("havuç"))
            {
                GameObject car = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                car.transform.SetParent(parent, false);
                car.transform.localPosition = new Vector3(0f, 0.05f * scaleFactor, 0f);
                car.transform.localScale = new Vector3(0.07f, 0.16f, 0.07f) * scaleFactor;
                car.transform.localRotation = Quaternion.Euler(0f, 0f, 75f);
                ApplyMaterial(car, new Color(0.95f, 0.50f, 0.10f), 0.0f, 0.5f);
                DestroyCollider(car);
            }
            else if (p.Contains("marul") || p.Contains("lahana") || p.Contains("ispanak") || p.Contains("pazı") || p.Contains("roka") || p.Contains("tere") || p.Contains("brokoli") || p.Contains("karnabahar"))
            {
                GameObject let = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                let.transform.SetParent(parent, false);
                let.transform.localPosition = new Vector3(0f, 0.06f * scaleFactor, 0f);
                let.transform.localScale = new Vector3(0.16f, 0.14f, 0.16f) * scaleFactor;
                Color vegCol = p.Contains("brokoli") ? new Color(0.20f, 0.60f, 0.25f) : (p.Contains("karnabahar") ? new Color(0.90f, 0.92f, 0.85f) : new Color(0.35f, 0.85f, 0.25f));
                ApplyMaterial(let, vegCol, 0.0f, 0.4f);
                DestroyCollider(let);
            }
            else if (p.Contains("mısır"))
            {
                GameObject corn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                corn.transform.SetParent(parent, false);
                corn.transform.localPosition = new Vector3(0f, 0.06f * scaleFactor, 0f);
                corn.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f) * scaleFactor;
                corn.transform.localRotation = Quaternion.Euler(0f, 0f, 85f);
                ApplyMaterial(corn, new Color(0.95f, 0.85f, 0.15f), 0.1f, 0.6f);
                DestroyCollider(corn);
            }
            else if (p.Contains("patlıcan"))
            {
                GameObject eg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                eg.transform.SetParent(parent, false);
                eg.transform.localPosition = new Vector3(0f, 0.06f * scaleFactor, 0f);
                eg.transform.localScale = new Vector3(0.09f, 0.17f, 0.09f) * scaleFactor;
                eg.transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
                ApplyMaterial(eg, new Color(0.45f, 0.15f, 0.55f), 0.2f, 0.7f);
                DestroyCollider(eg);
            }
            else if (p.Contains("biber"))
            {
                GameObject pep = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pep.transform.SetParent(parent, false);
                pep.transform.localPosition = new Vector3(0f, 0.05f * scaleFactor, 0f);
                pep.transform.localScale = new Vector3(0.07f, 0.14f, 0.07f) * scaleFactor;
                pep.transform.localRotation = Quaternion.Euler(0f, 0f, 75f);
                ApplyMaterial(pep, new Color(0.85f, 0.15f, 0.15f), 0.1f, 0.7f);
                DestroyCollider(pep);
            }

            // ==================== 1. UNLU MAMÜLLER & PASTA (Bakery) ====================
            else if (p.Contains("ekmek"))
            {
                // Somun Ekmek (Altın sarısı oval gövde + 3 adet çapraz çizik şerit)
                GameObject loaf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                loaf.transform.SetParent(parent, false);
                loaf.transform.localPosition = new Vector3(0f, 0.06f * scaleFactor, 0f);
                loaf.transform.localScale = new Vector3(0.16f, 0.09f, 0.11f) * scaleFactor;
                loaf.transform.localRotation = Quaternion.Euler(0f, 90f, 90f);
                ApplyMaterial(loaf, new Color(0.88f, 0.56f, 0.20f), 0.0f, 0.3f);
                DestroyCollider(loaf);

                for (int i = -1; i <= 1; i++)
                {
                    GameObject slit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    slit.transform.SetParent(parent, false);
                    slit.transform.localPosition = new Vector3(i * 0.04f * scaleFactor, 0.10f * scaleFactor, 0f);
                    slit.transform.localScale = new Vector3(0.02f, 0.02f, 0.09f) * scaleFactor;
                    ApplyMaterial(slit, new Color(0.98f, 0.88f, 0.65f), 0.0f, 0.5f);
                    DestroyCollider(slit);
                }
            }
            else if (p.Contains("simit"))
            {
                // Çıtır Sokak Simiti (Torus Halkası + Susam Dokusu)
                GameObject simit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                simit.transform.SetParent(parent, false);
                simit.transform.localPosition = new Vector3(0f, 0.03f * scaleFactor, 0f);
                simit.transform.localScale = new Vector3(0.18f, 0.03f, 0.18f) * scaleFactor;
                ApplyMaterial(simit, new Color(0.78f, 0.44f, 0.15f), 0.0f, 0.3f);
                DestroyCollider(simit);

                GameObject hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hole.transform.SetParent(simit.transform, false);
                hole.transform.localPosition = Vector3.zero;
                hole.transform.localScale = new Vector3(0.45f, 1.2f, 0.45f);
                ApplyMaterial(hole, new Color(0.20f, 0.20f, 0.20f), 0.0f, 0.0f); // Delik ilüzyonu
                DestroyCollider(hole);
            }
            else if (p.Contains("kruvasan") || p.Contains("poğaça") || p.Contains("börek"))
            {
                // Kruvasan / Poğaça (Altın Yumurtalı Ay Çöreği)
                GameObject pastry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pastry.transform.SetParent(parent, false);
                pastry.transform.localPosition = new Vector3(0f, 0.05f * scaleFactor, 0f);
                pastry.transform.localScale = new Vector3(0.18f, 0.08f, 0.13f) * scaleFactor;
                ApplyMaterial(pastry, new Color(0.92f, 0.65f, 0.22f), 0.1f, 0.6f);
                DestroyCollider(pastry);
            }
            else if (p.Contains("pasta"))
            {
                // Çikolatalı Pasta Dilimi (Üçgen Kalıp + Çilek Süsü)
                GameObject slice = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slice.transform.SetParent(parent, false);
                slice.transform.localPosition = new Vector3(0f, 0.07f * scaleFactor, 0f);
                slice.transform.localScale = new Vector3(0.15f, 0.10f, 0.15f) * scaleFactor;
                slice.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                ApplyMaterial(slice, new Color(0.40f, 0.22f, 0.15f), 0.1f, 0.7f);
                DestroyCollider(slice);

                GameObject topping = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                topping.transform.SetParent(slice.transform, false);
                topping.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                topping.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
                ApplyMaterial(topping, new Color(0.95f, 0.15f, 0.20f), 0.2f, 0.8f);
                DestroyCollider(topping);
            }

            // ==================== 2. SÜT, İÇECEK VE SU (Beverages & Dairy) ====================
            else if (p.Contains("süt"))
            {
                // Tam Yağlı Süt Kutusu (Beyaz Karton + Mavi Şerit + Mavi Kapak)
                GameObject carton = GameObject.CreatePrimitive(PrimitiveType.Cube);
                carton.transform.SetParent(parent, false);
                carton.transform.localPosition = new Vector3(0f, 0.12f * scaleFactor, 0f);
                carton.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f) * scaleFactor;
                ApplyMaterial(carton, new Color(0.96f, 0.96f, 0.98f), 0.0f, 0.7f);
                DestroyCollider(carton);

                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.transform.SetParent(carton.transform, false);
                band.transform.localPosition = new Vector3(0f, 0.10f, 0f);
                band.transform.localScale = new Vector3(1.02f, 0.40f, 1.02f);
                ApplyMaterial(band, new Color(0.15f, 0.55f, 0.92f), 0.1f, 0.6f);
                DestroyCollider(band);

                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.transform.SetParent(carton.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.54f, 0f);
                cap.transform.localScale = new Vector3(0.40f, 0.08f, 0.40f);
                ApplyMaterial(cap, new Color(0.10f, 0.45f, 0.88f), 0.1f, 0.8f);
                DestroyCollider(cap);
            }
            else if (p.Contains("meyve suyu") || p.Contains("şeftali"))
            {
                // Şeftali Meyve Suyu Kutusu (Turuncu Karton + Yeşil Yaprak Etiketi)
                GameObject juice = GameObject.CreatePrimitive(PrimitiveType.Cube);
                juice.transform.SetParent(parent, false);
                juice.transform.localPosition = new Vector3(0f, 0.12f * scaleFactor, 0f);
                juice.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f) * scaleFactor;
                ApplyMaterial(juice, new Color(0.96f, 0.55f, 0.15f), 0.0f, 0.6f);
                DestroyCollider(juice);

                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.transform.SetParent(juice.transform, false);
                leaf.transform.localPosition = new Vector3(0f, 0.20f, -0.52f);
                leaf.transform.localScale = new Vector3(0.60f, 0.35f, 0.05f);
                ApplyMaterial(leaf, new Color(0.20f, 0.75f, 0.25f), 0.0f, 0.5f);
                DestroyCollider(leaf);
            }
            else if (p.Contains("su") || p.Contains("maden suyu") || p.Contains("gazoz") || p.Contains("kola"))
            {
                // Şişe İçecek (Berrak Şişe + Renkli Sıvı + Şişe Kapağı)
                GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bottle.transform.SetParent(parent, false);
                bottle.transform.localPosition = new Vector3(0f, 0.11f * scaleFactor, 0f);
                bottle.transform.localScale = new Vector3(0.09f, 0.18f, 0.09f) * scaleFactor;
                Color bottleColor = p.Contains("maden") ? new Color(0.20f, 0.70f, 0.40f) : (p.Contains("su") ? new Color(0.30f, 0.75f, 0.95f) : new Color(0.85f, 0.15f, 0.15f));
                ApplyMaterial(bottle, bottleColor, 0.2f, 0.8f);
                DestroyCollider(bottle);

                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.transform.SetParent(bottle.transform, false);
                cap.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                cap.transform.localScale = new Vector3(0.70f, 0.12f, 0.70f);
                ApplyMaterial(cap, new Color(0.90f, 0.90f, 0.90f), 0.6f, 0.9f);
                DestroyCollider(cap);
            }

            // ==================== 3. PEYNİR, TEREYAĞI VE YUMURTA (Dairy & Eggs) ====================
            else if (p.Contains("peynir") || p.Contains("kaşar"))
            {
                // Taze Kaşar / Süzme Peynir (Sarı Kalıp Blok)
                GameObject cheese = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cheese.transform.SetParent(parent, false);
                cheese.transform.localPosition = new Vector3(0f, 0.06f * scaleFactor, 0f);
                cheese.transform.localScale = new Vector3(0.22f, 0.10f, 0.16f) * scaleFactor;
                ApplyMaterial(cheese, new Color(0.96f, 0.85f, 0.22f), 0.0f, 0.4f);
                DestroyCollider(cheese);
            }
            else if (p.Contains("tereyağı"))
            {
                // Trabzon Tereyağı (Altın Jelatin Ambalajlı Paket)
                GameObject butter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                butter.transform.SetParent(parent, false);
                butter.transform.localPosition = new Vector3(0f, 0.05f * scaleFactor, 0f);
                butter.transform.localScale = new Vector3(0.20f, 0.08f, 0.14f) * scaleFactor;
                ApplyMaterial(butter, new Color(0.92f, 0.78f, 0.18f), 0.4f, 0.7f);
                DestroyCollider(butter);
            }
            else if (p.Contains("yumurta"))
            {
                // Yumurta Viyol Koli (Karton Tepsi + 4 Adet Yumurta)
                GameObject tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tray.transform.SetParent(parent, false);
                tray.transform.localPosition = new Vector3(0f, 0.03f * scaleFactor, 0f);
                tray.transform.localScale = new Vector3(0.22f, 0.04f, 0.18f) * scaleFactor;
                ApplyMaterial(tray, new Color(0.68f, 0.54f, 0.42f), 0.0f, 0.2f);
                DestroyCollider(tray);

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        GameObject egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        egg.transform.SetParent(parent, false);
                        egg.transform.localPosition = new Vector3(x * 0.05f * scaleFactor, 0.07f * scaleFactor, z * 0.04f * scaleFactor);
                        egg.transform.localScale = new Vector3(0.06f, 0.08f, 0.06f) * scaleFactor;
                        ApplyMaterial(egg, new Color(0.92f, 0.85f, 0.74f), 0.0f, 0.4f);
                        DestroyCollider(egg);
                    }
                }
            }

            // ==================== 4. ET VE ŞARKÜTERİ (Meat & Butcher) ====================
            else if (p.Contains("et") || p.Contains("kıyma") || p.Contains("kuşbaşı") || p.Contains("antrikot") || p.Contains("tavuk"))
            {
                // Siyah Kasap Tepsisi + Kırmızı Et Kalıbı + Şeffaf Jelatin Görünümü
                GameObject tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tray.transform.SetParent(parent, false);
                tray.transform.localPosition = new Vector3(0f, 0.02f * scaleFactor, 0f);
                tray.transform.localScale = new Vector3(0.24f, 0.03f, 0.18f) * scaleFactor;
                ApplyMaterial(tray, new Color(0.12f, 0.12f, 0.14f), 0.1f, 0.5f);
                DestroyCollider(tray);

                GameObject meat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                meat.transform.SetParent(parent, false);
                meat.transform.localPosition = new Vector3(0f, 0.06f * scaleFactor, 0f);
                meat.transform.localScale = new Vector3(0.21f, 0.06f, 0.15f) * scaleFactor;
                Color meatCol = p.Contains("tavuk") ? new Color(0.95f, 0.80f, 0.75f) : new Color(0.82f, 0.18f, 0.22f);
                ApplyMaterial(meat, meatCol, 0.1f, 0.4f);
                DestroyCollider(meat);
            }
            else if (p.Contains("sucuk"))
            {
                // Kangal Sucuk (Kıvrımlı Kırmızı Halka)
                GameObject sausage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sausage.transform.SetParent(parent, false);
                sausage.transform.localPosition = new Vector3(0f, 0.04f * scaleFactor, 0f);
                sausage.transform.localScale = new Vector3(0.18f, 0.04f, 0.18f) * scaleFactor;
                ApplyMaterial(sausage, new Color(0.75f, 0.18f, 0.15f), 0.1f, 0.5f);
                DestroyCollider(sausage);
            }

            // ==================== 5. DONUK GIDALAR (Freezer Items) ====================
            else if (p.Contains("patates"))
            {
                // Dondurulmuş Patates Torbası (Kırmızı Poşet + Sarı Patates Süsü)
                GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bag.transform.SetParent(parent, false);
                bag.transform.localPosition = new Vector3(0f, 0.08f * scaleFactor, 0f);
                bag.transform.localScale = new Vector3(0.20f, 0.14f, 0.16f) * scaleFactor;
                ApplyMaterial(bag, new Color(0.88f, 0.18f, 0.15f), 0.1f, 0.6f);
                DestroyCollider(bag);
            }
            else if (p.Contains("pizza"))
            {
                // Donuk Pizza Kutusu (Yassı Kare Pizza Kutusu)
                GameObject pizzaBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pizzaBox.transform.SetParent(parent, false);
                pizzaBox.transform.localPosition = new Vector3(0f, 0.03f * scaleFactor, 0f);
                pizzaBox.transform.localScale = new Vector3(0.24f, 0.04f, 0.24f) * scaleFactor;
                ApplyMaterial(pizzaBox, new Color(0.92f, 0.45f, 0.15f), 0.0f, 0.5f);
                DestroyCollider(pizzaBox);
            }
            else if (p.Contains("dondurma"))
            {
                // Maraş Dondurma Kutusu (Mavi/Pembe Silindir Kutu)
                GameObject tub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tub.transform.SetParent(parent, false);
                tub.transform.localPosition = new Vector3(0f, 0.08f * scaleFactor, 0f);
                tub.transform.localScale = new Vector3(0.16f, 0.12f, 0.16f) * scaleFactor;
                ApplyMaterial(tub, new Color(0.20f, 0.70f, 0.90f), 0.1f, 0.7f);
                DestroyCollider(tub);
            }

            // ==================== 6. BAKLİYAT, MAKARNA VE AMBALAJLAR (Dry Goods) ====================
            else if (p.Contains("makarna"))
            {
                // Çubuk Makarna (Uzun Sarı Paket + Kırmızı Şerit)
                GameObject pack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pack.transform.SetParent(parent, false);
                pack.transform.localPosition = new Vector3(0f, 0.04f * scaleFactor, 0f);
                pack.transform.localScale = new Vector3(0.26f, 0.05f, 0.12f) * scaleFactor;
                ApplyMaterial(pack, new Color(0.95f, 0.82f, 0.18f), 0.1f, 0.6f);
                DestroyCollider(pack);
            }
            else if (p.Contains("pirinç") || p.Contains("un") || p.Contains("şeker") || p.Contains("fasulye") || p.Contains("mercimek"))
            {
                // Bakliyat Torbası (Dik Kese Kağıdı / Paket)
                GameObject sack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sack.transform.SetParent(parent, false);
                sack.transform.localPosition = new Vector3(0f, 0.10f * scaleFactor, 0f);
                sack.transform.localScale = new Vector3(0.16f, 0.18f, 0.12f) * scaleFactor;
                Color sackCol = p.Contains("pirinç") ? new Color(0.92f, 0.92f, 0.95f) : (p.Contains("mercimek") ? new Color(0.92f, 0.45f, 0.18f) : new Color(0.85f, 0.75f, 0.60f));
                ApplyMaterial(sack, sackCol, 0.0f, 0.4f);
                DestroyCollider(sack);
            }
            else if (p.Contains("yağ") || p.Contains("zeytinyağı"))
            {
                // Şeffaf Yağ Şişesi (Sarı/Yeşil Şeffaf Şişe + Sarı Kapak)
                GameObject oilBottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                oilBottle.transform.SetParent(parent, false);
                oilBottle.transform.localPosition = new Vector3(0f, 0.12f * scaleFactor, 0f);
                oilBottle.transform.localScale = new Vector3(0.10f, 0.22f, 0.10f) * scaleFactor;
                Color oilCol = p.Contains("sızma") ? new Color(0.35f, 0.65f, 0.18f) : new Color(0.95f, 0.85f, 0.15f);
                ApplyMaterial(oilBottle, oilCol, 0.3f, 0.8f);
                DestroyCollider(oilBottle);
            }
            else if (p.Contains("salça"))
            {
                // Kırmızı Salça Konserve Kutusu (Metal Konserve Kutusu + Kırmızı Etiket)
                GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                can.transform.SetParent(parent, false);
                can.transform.localPosition = new Vector3(0f, 0.08f * scaleFactor, 0f);
                can.transform.localScale = new Vector3(0.14f, 0.12f, 0.14f) * scaleFactor;
                ApplyMaterial(can, new Color(0.85f, 0.15f, 0.15f), 0.4f, 0.7f);
                DestroyCollider(can);
            }
            else if (p.Contains("çay") || p.Contains("kahve"))
            {
                // Çay / Kahve Kutusu (Kırmızı Çay Paketi veya Kahverengi Kahve Kutusu)
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(parent, false);
                box.transform.localPosition = new Vector3(0f, 0.09f * scaleFactor, 0f);
                box.transform.localScale = new Vector3(0.14f, 0.16f, 0.10f) * scaleFactor;
                Color teaCol = p.Contains("çay") ? new Color(0.85f, 0.15f, 0.18f) : new Color(0.45f, 0.28f, 0.18f);
                ApplyMaterial(box, teaCol, 0.1f, 0.5f);
                DestroyCollider(box);
            }

            // ==================== 7. ATIŞTIRMALIKLAR (Snacks & Chocolates) ====================
            else if (p.Contains("çikolata"))
            {
                // Sütlü Çikolata (Kırmızı Ambalajlı Tablet Çikolata)
                GameObject choco = GameObject.CreatePrimitive(PrimitiveType.Cube);
                choco.transform.SetParent(parent, false);
                choco.transform.localPosition = new Vector3(0f, 0.03f * scaleFactor, 0f);
                choco.transform.localScale = new Vector3(0.20f, 0.03f, 0.12f) * scaleFactor;
                ApplyMaterial(choco, new Color(0.85f, 0.15f, 0.15f), 0.2f, 0.7f);
                DestroyCollider(choco);
            }
            else if (p.Contains("cips"))
            {
                // Baharatlı Patates Cipsi (Kafes Şişkin Sarı Cips Poşeti)
                GameObject chips = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chips.transform.SetParent(parent, false);
                chips.transform.localPosition = new Vector3(0f, 0.09f * scaleFactor, 0f);
                chips.transform.localScale = new Vector3(0.18f, 0.16f, 0.10f) * scaleFactor;
                chips.transform.localRotation = Quaternion.Euler(0f, 15f, 5f);
                ApplyMaterial(chips, new Color(0.95f, 0.80f, 0.15f), 0.1f, 0.6f);
                DestroyCollider(chips);
            }

            // ==================== 8. KOZMETİK (Cosmetics & Personal Care) ====================
            else if (p.Contains("şampuan") || p.Contains("sabun") || p.Contains("krem") || p.Contains("macun") || p.Contains("parfüm") || p.Contains("serum"))
            {
                // Şampuan / Pompalı Kozmetik Şişesi (Mor/Pembe Şişe + Beyaz Pompa Başlığı)
                GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bot.transform.SetParent(parent, false);
                bot.transform.localPosition = new Vector3(0f, 0.10f * scaleFactor, 0f);
                bot.transform.localScale = new Vector3(0.10f, 0.16f, 0.10f) * scaleFactor;
                Color cosCol = p.Contains("şampuan") ? new Color(0.85f, 0.35f, 0.75f) : (p.Contains("parfüm") ? new Color(0.95f, 0.82f, 0.35f) : new Color(0.20f, 0.75f, 0.85f));
                ApplyMaterial(bot, cosCol, 0.2f, 0.8f);
                DestroyCollider(bot);

                GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pump.transform.SetParent(bot.transform, false);
                pump.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                pump.transform.localScale = new Vector3(0.40f, 0.15f, 0.60f);
                ApplyMaterial(pump, new Color(0.95f, 0.95f, 0.95f), 0.1f, 0.7f);
                DestroyCollider(pump);
            }

            // ==================== 9. ELEKTRONİK (Electronics) ====================
            else if (p.Contains("kulaklık") || p.Contains("powerbank") || p.Contains("fare") || p.Contains("saat") || p.Contains("hoparlör") || p.Contains("kablosu"))
            {
                // Parlak Siyah Teknoloji Kutusu (Mat Siyah Gövde + Mavi LED Işık Şeridi)
                GameObject techBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                techBox.transform.SetParent(parent, false);
                techBox.transform.localPosition = new Vector3(0f, 0.07f * scaleFactor, 0f);
                techBox.transform.localScale = new Vector3(0.18f, 0.10f, 0.14f) * scaleFactor;
                ApplyMaterial(techBox, new Color(0.12f, 0.15f, 0.20f), 0.7f, 0.9f);
                DestroyCollider(techBox);

                GameObject led = GameObject.CreatePrimitive(PrimitiveType.Cube);
                led.transform.SetParent(techBox.transform, false);
                led.transform.localPosition = new Vector3(0f, 0.51f, 0f);
                led.transform.localScale = new Vector3(0.80f, 0.02f, 0.20f);
                ApplyMaterial(led, new Color(0.15f, 0.75f, 0.95f), 0.1f, 0.9f);
                DestroyCollider(led);
            }

            // ==================== 10. VARSAYILAN STANDART LOW-POLY KUTU ====================
            else
            {
                GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.transform.SetParent(parent, false);
                box.transform.localPosition = new Vector3(0f, 0.08f * scaleFactor, 0f);
                box.transform.localScale = new Vector3(0.16f, 0.14f, 0.14f) * scaleFactor;
                ApplyMaterial(box, GetProductCategoryColor(pName), 0.1f, 0.5f);
                DestroyCollider(box);
            }
        }

        private static Color GetProductCategoryColor(string pName)
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
