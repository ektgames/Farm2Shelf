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
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            return mat;
        }
    }
}
