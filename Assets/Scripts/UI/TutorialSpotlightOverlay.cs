using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Farm2Shelf.UI
{
    /// <summary>
    /// Yumuşak delikli eğitim karartması. Delik dışına tıklamayı keser, deliğin içini geçirir.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TutorialSpotlightOverlay : MonoBehaviour, ICanvasRaycastFilter
    {
        private static readonly int HoleCenterId = Shader.PropertyToID("_HoleCenter");
        private static readonly int HoleHalfId = Shader.PropertyToID("_HoleHalf");
        private static readonly int HoleEnabledId = Shader.PropertyToID("_HoleEnabled");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int CornerId = Shader.PropertyToID("_Corner");

        private Image image;
        private Material runtimeMat;
        private Rect currentHole;
        private Rect targetHole;
        private bool holeEnabled;
        private bool targetHoleEnabled;
        private bool ready;

        public bool BlocksOutside => holeEnabled;

        public void Initialize(Material source)
        {
            image = GetComponent<Image>();
            if (runtimeMat != null) Destroy(runtimeMat);
            runtimeMat = new Material(source);
            image.material = runtimeMat;
            image.color = Color.white;
            image.raycastTarget = false;
            ready = true;
            SetDimStrength(0.84f);
            ApplyImmediate(new Rect(0f, 0f, Screen.width, Screen.height), false);
        }

        public void SetDimStrength(float alpha)
        {
            if (runtimeMat == null) return;
            runtimeMat.SetColor("_Color", new Color(0.012f, 0.016f, 0.03f, Mathf.Clamp01(alpha)));
        }

        public void SetTarget(Rect screenHole, bool enableHole)
        {
            targetHole = screenHole;
            targetHoleEnabled = enableHole;
            if (!ready) return;
            if (!holeEnabled && enableHole)
            {
                currentHole = screenHole;
            }
        }

        public void ApplyImmediate(Rect screenHole, bool enableHole)
        {
            currentHole = screenHole;
            targetHole = screenHole;
            holeEnabled = enableHole;
            targetHoleEnabled = enableHole;
            PushMaterial();
        }

        private void LateUpdate()
        {
            if (!ready) return;

            float t = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            holeEnabled = targetHoleEnabled;
            currentHole.xMin = Mathf.Lerp(currentHole.xMin, targetHole.xMin, t);
            currentHole.xMax = Mathf.Lerp(currentHole.xMax, targetHole.xMax, t);
            currentHole.yMin = Mathf.Lerp(currentHole.yMin, targetHole.yMin, t);
            currentHole.yMax = Mathf.Lerp(currentHole.yMax, targetHole.yMax, t);
            PushMaterial();
        }

        private void PushMaterial()
        {
            if (runtimeMat == null) return;
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            Vector4 center = new Vector4(currentHole.center.x / w, currentHole.center.y / h, 0f, 0f);
            Vector4 half = new Vector4((currentHole.width * 0.5f) / w, (currentHole.height * 0.5f) / h, 0f, 0f);
            runtimeMat.SetVector(HoleCenterId, center);
            runtimeMat.SetVector(HoleHalfId, half);
            runtimeMat.SetFloat(HoleEnabledId, holeEnabled ? 1f : 0f);
            runtimeMat.SetFloat(SoftnessId, holeEnabled ? 0.014f : 0.02f);
            runtimeMat.SetFloat(CornerId, 0.012f);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return false;
        }

        private void OnDestroy()
        {
            if (runtimeMat != null) Destroy(runtimeMat);
        }
    }
}
