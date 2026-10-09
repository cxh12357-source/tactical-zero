using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TacticalZero
{
    public static class UIFactory
    {
        private static Font font;
        public static Font Font
        {
            get
            {
                if (!font) font=Resources.Load<Font>("Fonts/TacticalZeroCJK");
                if (!font)
                    font = UnityEngine.Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "Arial" }, 22);
                if (!font)
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = anchor;
            r.anchoredPosition = position;
            r.sizeDelta = size;
            return r;
        }

        public static RectTransform Fill(string name, Transform parent)
        {
            var r = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public static Text Text(string name, Transform parent, string value, Vector2 anchor, Vector2 position, Vector2 size, int fontSize = 22, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var r = Rect(name, parent, anchor, position, size);
            var t = r.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = fontSize;
            t.text = value;
            t.color = new Color(.91f, .94f, .87f);
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        public static Image Panel(RectTransform rect, Color color, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Button Button(string label, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, UnityAction callback)
        {
            var r = Rect(label, parent, anchor, position, size);
            var image = Panel(r, new Color(.14f, .23f, .27f, .98f), true);
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = image;
            b.onClick.AddListener(() =>
            {
                AudioService.Instance?.Play("click", Vector3.zero, .5f, false);
                callback();
            });
            Text("Label", r, label, new Vector2(.5f, .5f), Vector2.zero, size - new Vector2(12, 8), 22, TextAnchor.MiddleCenter);
            return b;
        }

        public static void Slider(string label, Transform parent, Vector2 position, float value, float minimum, float maximum, UnityAction<float> changed)
        {
            var area = Rect(label, parent, new Vector2(.5f, .5f), position, new Vector2(420, 70));
            var text = Text("Label", area, label + "  " + value.ToString("0.00"), new Vector2(.5f, 1), new Vector2(0, -14), new Vector2(420, 28), 19);
            var track = Rect("Slider", area, new Vector2(.5f, .5f), new Vector2(0, -14), new Vector2(410, 26));
            Panel(track, new Color(.21f, .31f, .33f), true);
            var handle = Rect("Handle", track, new Vector2(0, .5f), Vector2.zero, new Vector2(26, 34));
            var image = Panel(handle, new Color(.8f, .72f, .44f), true);
            var slider = track.gameObject.AddComponent<Slider>();
            slider.targetGraphic = image;
            slider.handleRect = handle;
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(x =>
            {
                text.text = label + "  " + x.ToString("0.00");
                changed(x);
            });
        }
    }
}
