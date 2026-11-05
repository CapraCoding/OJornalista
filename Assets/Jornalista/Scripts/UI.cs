using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Jornalista
{
    public static class UI
    {
        static Font font;
        static Sprite circle;
        static Sprite rounded;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Anchor(this RectTransform rt, float minX, float minY, float maxX, float maxY)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt) => rt.Anchor(0, 0, 1, 1);

        public static RectTransform Place(this RectTransform rt, float x, float y, float w, float h, Vector2 pivot)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return rt;
        }

        public static Image Img(Transform parent, string name, Color color, Sprite art = null, bool raycast = false)
        {
            var rt = NewRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            if (art != null)
            {
                img.sprite = art;
                img.color = Color.white;
            }
            else
            {
                img.color = color;
            }
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Round(this Image img)
        {
            if (img.sprite != null) return img;
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            return img;
        }

        public static Image MakeCircle(this Image img)
        {
            img.sprite = CircleSprite;
            img.preserveAspect = true;
            return img;
        }

        public static Text Txt(Transform parent, string name, string content, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = NewRect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static T WithShadow<T>(this T g, float dist = 3f) where T : Graphic
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.6f);
            s.effectDistance = new Vector2(dist, -dist);
            return g;
        }

        public static Button Btn(Transform parent, string name, string label, Color bg, Color fg, int fontSize,
            UnityAction onClick, Sprite art = null)
        {
            var img = Img(parent, name, bg, art, true);
            if (art == null) img.Round();
            else img.preserveAspect = true;

            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var c = b.colors;
            c.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            c.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            c.selectedColor = Color.white;
            c.disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            b.colors = c;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;

            if (onClick != null) b.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(label))
            {
                var t = Txt(img.transform, "Texto", label, fontSize, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
                t.rectTransform.Anchor(0.04f, 0.06f, 0.96f, 0.94f);
            }
            return b;
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (circle == null) circle = BuildCircle(128);
                return circle;
            }
        }

        public static Sprite RoundedSprite
        {
            get
            {
                if (rounded == null) rounded = BuildRounded(64, 16);
                return rounded;
            }
        }

        static Sprite BuildCircle(int s)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            float r = s / 2f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                byte a = (byte)(Mathf.Clamp01(r - d) * 255);
                px[y * s + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite BuildRounded(int s, int radius)
        {
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, s - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, s - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d) * 255);
                px[y * s + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }

    public static class DevInput
    {
        public static float Horizontal()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k == null) return 0f;
            float v = 0f;
            if (k.leftArrowKey.isPressed || k.aKey.isPressed) v -= 1f;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed) v += 1f;
            return v;
#else
            return Input.GetAxisRaw("Horizontal");
#endif
        }
    }
}
