using UnityEngine;
using UnityEngine.UI;

namespace Farm2Shelf.UI
{
    /// <summary>Platform-independent UI spotlight; no dynamically located shader required.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TutorialSpotlightOverlay : MaskableGraphic, ICanvasRaycastFilter
    {
        private Rect currentHole;
        private Rect targetHole;
        private bool holeEnabled;
        private bool targetHoleEnabled;
        private float strength = 0.62f;
        private readonly Vector3[] outerCorners = new Vector3[4];
        private readonly Vector3[] innerCorners = new Vector3[4];
        public bool BlocksOutside => holeEnabled;

        public void Initialize()
        {
            raycastTarget = true;
            color = Color.white;
            material = null; // Unity's built-in UI material is included on every platform.
            SetVerticesDirty();
        }

        public void SetDimStrength(float alpha)
        {
            float value = Mathf.Clamp01(alpha);
            if (Mathf.Approximately(strength, value)) return;
            strength = value;
            SetVerticesDirty();
        }

        public void SetTarget(Rect screenHole, bool enableHole)
        {
            targetHole = screenHole;
            targetHoleEnabled = enableHole;
            if (!holeEnabled && enableHole) currentHole = screenHole;
            SetVerticesDirty();
        }

        public void ApplyImmediate(Rect screenHole, bool enableHole)
        {
            currentHole = targetHole = screenHole;
            holeEnabled = targetHoleEnabled = enableHole;
            SetVerticesDirty();
        }

        public bool BlocksScreenPoint(Vector2 point)
        {
            // Camera and free-placement steps must keep world gestures available.
            // During UI guidance only the current objective accepts input.
            return isActiveAndEnabled && targetHoleEnabled && !targetHole.Contains(point);
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return BlocksScreenPoint(screenPoint);
        }

        private void LateUpdate()
        {
            float t = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            Rect before = currentHole;
            bool changed = holeEnabled != targetHoleEnabled;
            holeEnabled = targetHoleEnabled;
            currentHole = new Rect(Vector2.Lerp(currentHole.position, targetHole.position, t),
                Vector2.Lerp(currentHole.size, targetHole.size, t));
            if (changed || currentHole != before) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect full = rectTransform.rect;
            Color dim = new Color(0.012f, 0.016f, 0.03f, strength);
            if (!holeEnabled)
            {
                Quad(vh, full, dim);
                return;
            }
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, currentHole.min, camera, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, currentHole.max, camera, out Vector2 max);
            Rect hole = Rect.MinMaxRect(Mathf.Clamp(min.x, full.xMin, full.xMax), Mathf.Clamp(min.y, full.yMin, full.yMax),
                Mathf.Clamp(max.x, full.xMin, full.xMax), Mathf.Clamp(max.y, full.yMin, full.yMax));
            if (hole.width <= 0f || hole.height <= 0f) { Quad(vh, full, dim); return; }
            float feather = Mathf.Min(10f, Mathf.Min(hole.width, hole.height) * 0.15f);
            Quad(vh, Rect.MinMaxRect(full.xMin, full.yMin, full.xMax, hole.yMin), dim);
            Quad(vh, Rect.MinMaxRect(full.xMin, hole.yMax, full.xMax, full.yMax), dim);
            Quad(vh, Rect.MinMaxRect(full.xMin, hole.yMin, hole.xMin, hole.yMax), dim);
            Quad(vh, Rect.MinMaxRect(hole.xMax, hole.yMin, full.xMax, hole.yMax), dim);
            Rect inner = Rect.MinMaxRect(hole.xMin + feather, hole.yMin + feather, hole.xMax - feather, hole.yMax - feather);
            Ring(vh, hole, inner, dim, new Color(dim.r, dim.g, dim.b, 0f));
            Rect rim = Rect.MinMaxRect(inner.xMin - 2f, inner.yMin - 2f, inner.xMax + 2f, inner.yMax + 2f);
            Ring(vh, rim, inner, new Color(1f, 0.82f, 0.28f, 0.65f), new Color(1f, 0.82f, 0.28f, 0f));
        }

        private static void Corners(Rect rect, Vector3[] points)
        {
            points[0] = new Vector3(rect.xMin, rect.yMin, 0f);
            points[1] = new Vector3(rect.xMin, rect.yMax, 0f);
            points[2] = new Vector3(rect.xMax, rect.yMax, 0f);
            points[3] = new Vector3(rect.xMax, rect.yMin, 0f);
        }

        private void Quad(VertexHelper vh, Rect rect, Color tint)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            Corners(rect, outerCorners);
            int start = vh.currentVertCount;
            for (int i = 0; i < 4; i++) vh.AddVert(outerCorners[i], tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private void Ring(VertexHelper vh, Rect outer, Rect inner, Color outerColor, Color innerColor)
        {
            Corners(outer, outerCorners);
            Corners(inner, innerCorners);
            int start = vh.currentVertCount;
            for (int i = 0; i < 4; i++)
            {
                vh.AddVert(outerCorners[i], outerColor, Vector2.zero);
                vh.AddVert(innerCorners[i], innerColor, Vector2.zero);
            }
            for (int i = 0; i < 4; i++)
            {
                int a = start + i * 2;
                int b = start + ((i + 1) % 4) * 2;
                vh.AddTriangle(a, b, b + 1);
                vh.AddTriangle(a, b + 1, a + 1);
            }
        }
    }
}
