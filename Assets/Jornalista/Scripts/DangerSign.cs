using System;
using UnityEngine;
using UnityEngine.UI;

namespace Jornalista
{
    public class DangerSign : MonoBehaviour
    {
        public const int MaxLevel = 7;

        static readonly string[] Texts =
        {
            "PERIGO", "PERIGO!", "PERIGO!!", "PERIGO!!!",
            "MUITO PERIGO!!!", "PERIGO EXTREMO!!!", "PERIGO EXTREMO!!!!"
        };

        RectTransform root;
        Image fill, beacon, glow;
        Text label, mark;
        GameObject tape;
        GameObject[] cones;
        Vector2 basePos;
        int level = 1;
        float t;

        public static DangerSign Create(Transform world, float x, float groundFrac, Action onClick)
        {
            var r = UI.NewRect(world, "PlacaPerigo");
            r.anchorMin = r.anchorMax = new Vector2(0, groundFrac);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(260, 440);
            r.anchoredPosition = new Vector2(x, 0);
            var sign = r.gameObject.AddComponent<DangerSign>();
            sign.Build(r, onClick);
            return sign;
        }

        void Build(RectTransform r, Action onClick)
        {
            root = r;
            basePos = r.anchoredPosition;
            var center = new Vector2(0.5f, 0.5f);
            var bottom = new Vector2(0.5f, 0f);

            glow = UI.Img(r, "Brilho", new Color(1f, 0.1f, 0.1f, 0f)).MakeCircle();
            glow.rectTransform.Place(130, 330, 440, 440, center);

            cones = new GameObject[3];
            float[] coneX = { -30f, 290f, -100f };
            for (int i = 0; i < cones.Length; i++)
            {
                var cone = UI.Img(r, "Cone", new Color(1f, 0.45f, 0.05f)).Round();
                cone.rectTransform.Place(coneX[i], 0, 46, 80, bottom);
                UI.Img(cone.transform, "Faixa", Color.white).rectTransform.Anchor(0, 0.45f, 1, 0.6f);
                cones[i] = cone.gameObject;
            }

            UI.Img(r, "Poste", new Color(0.35f, 0.35f, 0.38f)).rectTransform.Place(130, 0, 18, 330, bottom);

            var tapeRt = UI.NewRect(r, "Fita").Place(130, 70, 560, 26, center);
            for (int k = 0; k < 14; k++)
            {
                var c = k % 2 == 0 ? new Color(1f, 0.85f, 0.1f) : new Color(0.1f, 0.1f, 0.1f);
                UI.Img(tapeRt, "Listra", c).rectTransform.Anchor(k / 14f, 0, (k + 1) / 14f, 1);
            }
            tapeRt.localRotation = Quaternion.Euler(0, 0, -6f);
            tape = tapeRt.gameObject;

            var border = UI.Img(r, "Placa", new Color(0.1f, 0.1f, 0.1f), null, true).Round();
            border.rectTransform.Place(130, 330, 190, 190, center).localRotation = Quaternion.Euler(0, 0, 45f);
            fill = UI.Img(border.transform, "Fundo", Color.yellow).Round();
            fill.rectTransform.Anchor(0.07f, 0.07f, 0.93f, 0.93f);

            var btn = border.gameObject.AddComponent<Button>();
            btn.targetGraphic = border;
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            btn.onClick.AddListener(() => onClick?.Invoke());

            mark = UI.Txt(r, "Exclamacao", "!", 80, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            mark.rectTransform.Place(130, 352, 140, 100, center);
            label = UI.Txt(r, "Texto", "PERIGO", 30, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Place(130, 286, 150, 44, center);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 30;

            beacon = UI.Img(r, "Giroflex", new Color(1f, 0.2f, 0.1f)).MakeCircle();
            beacon.rectTransform.Place(130, 478, 42, 42, center);

            SetLevel(1);
        }

        public void SetLevel(int day)
        {
            level = Mathf.Clamp(day, 1, MaxLevel);
            float k = (level - 1) / (float)(MaxLevel - 1);

            fill.color = Color.Lerp(new Color(1f, 0.85f, 0.1f), new Color(0.85f, 0.05f, 0.05f), k);
            var ink = level >= 4 ? Color.white : Color.black;
            label.color = ink;
            mark.color = ink;
            label.text = Texts[level - 1];
            mark.text = new string('!', Mathf.Min(level, 3));

            root.localScale = Vector3.one * (0.8f + 0.09f * (level - 1));
            root.localRotation = Quaternion.Euler(0, 0, level >= 3 ? -3f * (level - 2) : 0f);
            root.anchoredPosition = basePos;

            beacon.gameObject.SetActive(level >= 4);
            tape.SetActive(level >= 5);
            for (int i = 0; i < cones.Length; i++) cones[i].SetActive(i < level - 4);
            glow.gameObject.SetActive(level >= 6);
        }

        void Update()
        {
            t += Time.deltaTime;

            if (level >= 4)
            {
                float speed = level >= 6 ? 4f : 2f;
                beacon.color = Mathf.Repeat(t * speed, 1f) < 0.5f ? new Color(1f, 0.15f, 0.1f) : new Color(1f, 0.75f, 0.2f);
            }
            if (level >= 6)
            {
                var c = glow.color;
                c.a = 0.18f + 0.14f * Mathf.Sin(t * 6f);
                glow.color = c;
                root.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 45f) * 3f * (level - 5), 0);
            }
        }
    }
}
