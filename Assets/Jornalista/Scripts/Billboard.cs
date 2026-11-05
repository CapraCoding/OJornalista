using UnityEngine;
using UnityEngine.UI;

namespace Jornalista
{
    public class Billboard : MonoBehaviour
    {
        Text caption, local, frase;
        Image liveDot;
        GameObject localGo;
        string[] captions = { "TV PLANTÃO" };
        int index;
        float t, captionTimer;

        public static Billboard Create(Transform world, float cx, float width, float yMin, float yMax)
        {
            var frame = UI.Img(world, "Outdoor", new Color(0.10f, 0.10f, 0.12f)).Round();
            var r = frame.rectTransform;
            r.anchorMin = new Vector2(0, yMin);
            r.anchorMax = new Vector2(0, yMax);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(width, 0);
            r.anchoredPosition = new Vector2(cx, 0);

            var b = frame.gameObject.AddComponent<Billboard>();
            b.Build(frame);
            return b;
        }

        void Build(Image frame)
        {
            var screen = UI.Img(frame.transform, "Tela", new Color(0.08f, 0.13f, 0.28f));
            screen.rectTransform.Anchor(0.02f, 0.05f, 0.98f, 0.95f);
            var s = screen.transform;

            var capBg = UI.Img(s, "ChamadaFundo", new Color(0.8f, 0.05f, 0.05f));
            capBg.rectTransform.Anchor(0, 0.7f, 1, 1);
            caption = UI.Txt(capBg.transform, "Chamada", "", 56, new Color(1f, 0.92f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold)
                .WithShadow(3f);
            caption.rectTransform.Anchor(0.02f, 0.05f, 0.98f, 0.95f);
            caption.resizeTextForBestFit = true;
            caption.resizeTextMinSize = 18;
            caption.resizeTextMaxSize = 60;

            liveDot = UI.Img(s, "Ponto", new Color(1f, 0.15f, 0.15f)).MakeCircle();
            liveDot.rectTransform.Anchor(0.03f, 0.55f, 0.07f, 0.67f);
            UI.Txt(s, "AoVivo", "AO VIVO", 30, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Anchor(0.08f, 0.52f, 0.4f, 0.7f);
            UI.Txt(s, "Logo", "TV PLANTÃO", 32, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleRight, FontStyle.BoldAndItalic)
                .rectTransform.Anchor(0.5f, 0.52f, 0.97f, 0.7f);

            var loc = UI.Img(s, "GCLocal", new Color(0.75f, 0.08f, 0.08f));
            loc.rectTransform.Anchor(0.03f, 0.36f, 0.97f, 0.51f);
            localGo = loc.gameObject;
            local = UI.Txt(loc.transform, "Local", "", 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            local.rectTransform.Anchor(0.015f, 0.05f, 0.985f, 0.95f);
            local.verticalOverflow = VerticalWrapMode.Truncate;
            local.resizeTextForBestFit = true;
            local.resizeTextMinSize = 8;
            local.resizeTextMaxSize = 26;

            var fr = UI.Img(s, "GCFrase", new Color(0.97f, 0.97f, 0.97f));
            fr.rectTransform.Anchor(0.03f, 0.06f, 0.97f, 0.36f);
            frase = UI.Txt(fr.transform, "Frase", "", 44, new Color(0.06f, 0.06f, 0.08f), TextAnchor.MiddleLeft, FontStyle.Bold);
            frase.rectTransform.Anchor(0.02f, 0.05f, 0.98f, 0.95f);
            frase.resizeTextForBestFit = true;
            frase.verticalOverflow = VerticalWrapMode.Truncate;
            frase.resizeTextMinSize = 12;
            frase.resizeTextMaxSize = 46;

            SetReport(null);
        }

        public void SetReport(Reportagem r)
        {
            bool has = r != null && !string.IsNullOrEmpty(r.frase);
            if (has)
            {
                captions = r.chamadas != null && r.chamadas.Length > 0 ? r.chamadas : new[] { "URGENTE!" };
                captions = System.Array.ConvertAll(captions, c => (c ?? "").ToUpperInvariant());
                frase.text = r.frase.ToUpperInvariant();
                localGo.SetActive(!string.IsNullOrEmpty(r.local));
                local.text = NewsLoader.LocalLine(r.local);
            }
            else
            {
                captions = new[] { "TV PLANTÃO" };
                frase.text = "BOM DIA, CIDADE!";
                localGo.SetActive(false);
            }
            index = 0;
            captionTimer = 0f;
            caption.text = captions[0];
        }

        void Update()
        {
            t += Time.deltaTime;
            captionTimer += Time.deltaTime;
            if (captions.Length > 1 && captionTimer > 1.4f)
            {
                captionTimer = 0f;
                index = (index + 1) % captions.Length;
                caption.text = captions[index];
            }
            caption.color = Mathf.Sin(t * 10f) > 0 ? new Color(1f, 0.92f, 0.2f) : Color.white;
            liveDot.enabled = Mathf.Repeat(t, 1f) < 0.6f;
        }
    }
}
