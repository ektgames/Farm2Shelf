using UnityEngine;
using UnityEngine.UI;
using Farm2Shelf.Environment;

namespace Farm2Shelf.UI
{
    /// <summary>Small safe-area controls on the existing HUD; no separate input system.</summary>
    public sealed class StoreWallViewButtons : MonoBehaviour
    {
        private readonly Image[] backgrounds = new Image[3];
        private RectTransform panel;
        private RectTransform pauseButton;
        private readonly RectTransform[] buttons = new RectTransform[3];
        private int displayed = -1;

        public static void Create(Transform parent)
        {
            var go = new GameObject("Wall_View_Controls", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<StoreWallViewButtons>().Build();
        }

        private void Build()
        {
            pauseButton = transform.parent.Find("Widget_PauseButton") as RectTransform;
            panel = (RectTransform)transform;
            if (pauseButton != null)
                panel.SetParent(pauseButton, false);
            panel.anchorMin = panel.anchorMax = new Vector2(1, 0);
            panel.pivot = Vector2.one;
            panel.anchoredPosition = new Vector2(0, -8);
            panel.sizeDelta = new Vector2(150, 46);
            for (int i = 0; i < 3; i++)
            {
                int mode = i;
                var go = new GameObject("Wall_Mode_" + (i + 1), typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                buttons[i] = rect;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(i * 52, 0);
                rect.sizeDelta = new Vector2(46, 46);
                backgrounds[i] = go.GetComponent<Image>();
                backgrounds[i].sprite = UIStyleUtility.CreateOutlinePillSprite(
                    92, 92, 22, 3, new Color(.70f, .76f, .80f), Color.white);
                var button = go.GetComponent<Button>();
                button.targetGraphic = backgrounds[i];
                button.onClick.AddListener(() => StoreWallVisual.SetMode(mode));

                // Masonry diagram: full, half and floor line. Children never intercept taps.
                int rows = 3 - i;
                for (int row = 0; row < rows; row++)
                    for (int col = 0; col < 3; col++)
                    {
                        var brick = new GameObject("Brick", typeof(RectTransform), typeof(Image));
                        brick.transform.SetParent(go.transform, false);
                        var br = (RectTransform)brick.transform;
                        br.anchorMin = br.anchorMax = new Vector2(.5f, 0);
                        br.sizeDelta = new Vector2(10, i == 2 ? 4 : 6);
                        br.anchoredPosition = new Vector2((col - 1) * 11, 20 + row * 8);
                        var image = brick.GetComponent<Image>();
                        image.color = new Color(.96f, .98f, 1f);
                        image.raycastTarget = false;
                    }
                var label = new GameObject("Number", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(go.transform, false);
                var lr = (RectTransform)label.transform;
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = new Vector2(1, .39f);
                lr.offsetMin = lr.offsetMax = Vector2.zero;
                var text = label.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 14;
                text.fontStyle = FontStyle.Bold;
                text.text = (i + 1).ToString();
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.raycastTarget = false;
            }
            Update();
        }

        private void Update()
        {
            if (panel == null) return;
            // Follow the actual Pause widget, rather than applying a second safe-area offset.
            if (pauseButton != null)
            {
                float width = pauseButton.rect.width;
                float side = (width - 12f) / 3f;
                panel.sizeDelta = new Vector2(width, side);
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].sizeDelta = new Vector2(side, side);
                    buttons[i].anchoredPosition = new Vector2(i * (side + 6f), 0);
                }
            }
            if (displayed == StoreWallVisual.Mode) return;
            displayed = StoreWallVisual.Mode;
            for (int i = 0; i < backgrounds.Length; i++)
                backgrounds[i].color = i == displayed
                    ? new Color(.52f, .39f, .10f, .98f)
                    : new Color(.09f, .14f, .18f, .96f);
        }
    }
}
