using UnityEngine;

namespace Farm2Shelf.Utils
{
    /// <summary>
    /// URP 3D mesh materyalleri için 'Universal Render Pipeline/Lit' shader'ının güvenle getirilmesini
    /// ve null-pointer exception (ArgumentNullException) oluşmadan materyal üretilmesini sağlayan yardımcı sınıf.
    /// UI veya Sprite shader'larına düşmez, yalnızca uygun 3D URP shader'larını (Lit / Simple Lit / Unlit) hedefler.
    /// </summary>
    public static class ShaderHelper
    {
        private static Shader _cachedLitShader;
        private static Shader _cachedUnlitShader;

        /// <summary>
        /// 3D mesh materyalleri için uygun URP 3D shader'ını arar ve döndürür.
        /// UI/Sprite shader'larına düşmez. Shader cihazda hiç bulunamazsa açık Debug.LogError basar ve null döner.
        /// </summary>
        public static Shader GetLitShader()
        {
            if (_cachedLitShader != null) return _cachedLitShader;

            // 1. Ana URP Lit Shader
            _cachedLitShader = Shader.Find("Universal Render Pipeline/Lit");

            // 2. Resources klasöründeki doğrulanan DefaultURPLit materyalinden yüklemeyi dene
            if (_cachedLitShader == null)
            {
                Material resMat = Resources.Load<Material>("Shaders/DefaultURPLit");
                if (resMat != null && resMat.shader != null)
                {
                    _cachedLitShader = resMat.shader;
                }
            }

            // 3. Yalnızca URP 3D mesh alternatiflerini dene
            if (_cachedLitShader == null) _cachedLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (_cachedLitShader == null) _cachedLitShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (_cachedLitShader == null)
            {
                Debug.LogError("[ShaderHelper] KRİTİK HATA: 'Universal Render Pipeline/Lit' 3D Shader cihaz ortamında bulunamadı!");
            }

            return _cachedLitShader;
        }

        /// <summary>
        /// Gece cam/ampul gibi kendi rengini göstermesi gereken yüzeyler için URP Unlit.
        /// Ortam ışığı ve gölgeye bağlı kalmaz; bloom/HDR gerektirmez.
        /// Built-in Unlit/Color'a düşmez — URP'de o shader magenta hata rengi üretir.
        /// </summary>
        public static Shader GetUnlitShader()
        {
            if (_cachedUnlitShader != null) return _cachedUnlitShader;

            _cachedUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (_cachedUnlitShader == null)
            {
                Material resMat = Resources.Load<Material>("Shaders/DefaultURPUnlit");
                if (resMat != null && resMat.shader != null && IsValidUrpShader(resMat.shader))
                {
                    _cachedUnlitShader = resMat.shader;
                }
            }
            if (_cachedUnlitShader == null) _cachedUnlitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (_cachedUnlitShader == null) _cachedUnlitShader = GetLitShader();
            return _cachedUnlitShader;
        }

        public static bool IsValidUrpShader(Shader shader)
        {
            if (shader == null) return false;
            string n = shader.name;
            if (string.IsNullOrEmpty(n)) return false;
            if (n.Contains("InternalError") || n == "Hidden/InternalErrorShader") return false;
            if (n == "Unlit/Color" || n == "Sprites/Default" || n.StartsWith("UI/")) return false;
            return true;
        }

        /// <summary>
        /// Mobil GLES'te _BaseMap boş URP Unlit/Lit magenta (hata pembe) basar.
        /// Emission keyword Unlit varyantını da kırar; gece camı için kullanma.
        /// </summary>
        public static Material CreateUnlitOpaqueMaterial(Color color, string name = "ProceduralUnlitMat")
        {
            Material mat = null;
            Material template = Resources.Load<Material>("Shaders/DefaultURPUnlit");
            if (template != null && IsValidUrpShader(template.shader))
            {
                mat = new Material(template);
            }
            else
            {
                Shader s = GetUnlitShader();
                if (s == null)
                {
                    Debug.LogError($"[ShaderHelper] Unlit materyal '{name}' oluşturulamadı.");
                    return CreateLitMaterial(color, name);
                }
                mat = new Material(s);
            }

            mat.name = name;
            BindOpaqueColorMaps(mat, color);
            if (mat.HasProperty("_ReceiveShadows")) mat.SetFloat("_ReceiveShadows", 0f);
            mat.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
            return mat;
        }

        public static void BindOpaqueColorMaps(Material mat, Color color)
        {
            if (mat == null) return;

            Texture2D white = Texture2D.whiteTexture;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", white);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", white);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;

            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.5f);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 2f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            if (mat.HasProperty("_SrcBlendAlpha")) mat.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
            if (mat.HasProperty("_DstBlendAlpha")) mat.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.Zero);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = 2000;
        }

        /// <summary>
        /// Verilen renk ile güvenli bir prosedürel 3D Material nesnesi oluşturur.
        /// Shader null ise new Material(null) fırlatmasını önler, açık LogError basar ve null döner.
        /// </summary>
        public static Material CreateLitMaterial(Color color, string name = "ProceduralLitMat")
        {
            Shader s = GetLitShader();
            if (s == null)
            {
                Debug.LogError($"[ShaderHelper] Materyal '{name}' oluşturulamadı çünkü uygun 3D URP Shader bulunamadı!");
                return null;
            }

            Material mat = new Material(s);
            mat.name = name;
            BindOpaqueColorMaps(mat, color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
            return mat;
        }
    }
}
