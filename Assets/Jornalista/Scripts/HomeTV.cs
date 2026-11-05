using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Jornalista
{
    public class HomeTV : MonoBehaviour
    {
        Button button;
        Pulse pulse;
        RawImage noise;
        Texture2D noiseTex;
        Color32[] noisePx;
        GameObject onScreen, noSignal;
        Image glow, led, liveDot;
        Text urgent, frase;
        bool on;
        float t, noiseTimer;

        const int NoiseW = 80, NoiseH = 60;

        public static HomeTV Create(RectTransform parent, float minX, float minY, float maxX, float maxY, UnityAction onClick)
        {
            var root = UI.NewRect(parent, "TV").Anchor(minX, minY, maxX, maxY);
            var tv = root.gameObject.AddComponent<HomeTV>();
            tv.Build(root, onClick);
            return tv;
        }

        void Build(RectTransform root, UnityAction onClick)
        {
            var wood = new Color(0.42f, 0.26f, 0.14f);
            var woodDark = new Color(0.30f, 0.18f, 0.10f);
            var metal = new Color(0.55f, 0.55f, 0.58f);

            glow = UI.Img(root, "Brilho", new Color(0.5f, 0.75f, 1f, 0f)).MakeCircle();
            glow.preserveAspect = false;
            glow.rectTransform.Anchor(-0.25f, 0.15f, 1.25f, 1.0f);

            var cab = UI.Img(root, "Movel", woodDark).Round();
            cab.rectTransform.Anchor(0f, 0f, 1f, 0.17f);
            UI.Img(cab.transform, "Gaveta", wood).Round().rectTransform.Anchor(0.06f, 0.2f, 0.47f, 0.8f);
            UI.Img(cab.transform, "Gaveta2", wood).Round().rectTransform.Anchor(0.53f, 0.2f, 0.94f, 0.8f);

            Antenna(root, 0.47f, 24f, metal);
            Antenna(root, 0.53f, -24f, metal);
            UI.Img(root, "BaseAntena", new Color(0.15f, 0.15f, 0.16f)).MakeCircle().rectTransform.Anchor(0.43f, 0.755f, 0.57f, 0.8f);

            var body = UI.Img(root, "Gabinete", wood, null, true).Round();
            body.rectTransform.Anchor(0f, 0.17f, 1f, 0.78f);
            button = body.gameObject.AddComponent<Button>();
            button.targetGraphic = body;
            var colors = button.colors;
            colors.disabledColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            if (onClick != null) button.onClick.AddListener(onClick);

            var bezel = UI.Img(body.transform, "Moldura", new Color(0.12f, 0.12f, 0.13f)).Round();
            bezel.rectTransform.Anchor(0.04f, 0.07f, 0.74f, 0.93f);

            var screen = UI.Img(bezel.transform, "Tela", new Color(0.05f, 0.06f, 0.07f)).Round();
            screen.rectTransform.Anchor(0.06f, 0.07f, 0.94f, 0.93f);
            screen.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var s = screen.transform;

            noiseTex = new Texture2D(NoiseW, NoiseH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            noisePx = new Color32[NoiseW * NoiseH];
            noise = UI.NewRect(s, "Chiado").Stretch().gameObject.AddComponent<RawImage>();
            noise.texture = noiseTex;
            noise.raycastTarget = false;
            RegenerateNoise();

            var ns = UI.Img(s, "SemSinal", new Color(0, 0, 0, 0.6f));
            ns.rectTransform.Anchor(0.18f, 0.4f, 0.82f, 0.6f);
            UI.Txt(ns.transform, "Texto", "SEM SINAL", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Stretch();
            noSignal = ns.gameObject;

            var program = UI.Img(s, "Plantao", new Color(0.10f, 0.18f, 0.38f));
            program.rectTransform.Stretch();
            onScreen = program.gameObject;
            var p = program.transform;
            var bar = UI.Img(p, "Urgente", new Color(0.8f, 0.05f, 0.05f));
            bar.rectTransform.Anchor(0f, 0.74f, 1f, 0.96f);
            urgent = UI.Txt(bar.transform, "Texto", "URGENTE", 30, new Color(1f, 0.92f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
            urgent.rectTransform.Stretch();
            urgent.resizeTextForBestFit = true;
            urgent.resizeTextMinSize = 10;
            urgent.resizeTextMaxSize = 30;
            liveDot = UI.Img(p, "AoVivo", new Color(1f, 0.15f, 0.15f)).MakeCircle();
            liveDot.rectTransform.Anchor(0.05f, 0.5f, 0.13f, 0.66f);
            UI.Txt(p, "Logo", "TV PLANTÃO", 18, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleRight, FontStyle.BoldAndItalic)
                .rectTransform.Anchor(0.3f, 0.48f, 0.95f, 0.68f);
            var gc = UI.Img(p, "GC", new Color(0.97f, 0.97f, 0.97f));
            gc.rectTransform.Anchor(0.04f, 0.08f, 0.96f, 0.4f);
            frase = UI.Txt(gc.transform, "Frase", "", 20, new Color(0.06f, 0.06f, 0.08f), TextAnchor.MiddleCenter, FontStyle.Bold);
            frase.rectTransform.Anchor(0.03f, 0.05f, 0.97f, 0.95f);
            frase.resizeTextForBestFit = true;
            frase.resizeTextMinSize = 8;
            frase.resizeTextMaxSize = 20;

            var scan = UI.NewRect(s, "Scanlines").Stretch().gameObject.AddComponent<RawImage>();
            scan.texture = ScanlineTexture();
            scan.uvRect = new UnityEngine.Rect(0, 0, 1, 50);
            scan.raycastTarget = false;
            var glare = UI.Img(s, "Reflexo", new Color(1f, 1f, 1f, 0.08f));
            glare.rectTransform.Anchor(-0.2f, 0.55f, 0.7f, 1.3f);
            glare.rectTransform.localRotation = Quaternion.Euler(0, 0, 18f);

            var panel = UI.NewRect(body.transform, "Painel").Anchor(0.77f, 0.07f, 0.96f, 0.93f);
            Knob(panel, 0.68f, 0.92f);
            Knob(panel, 0.40f, 0.64f);
            for (int k = 0; k < 5; k++)
                UI.Img(panel, "Grade", woodDark).rectTransform.Anchor(0.12f, 0.08f + k * 0.055f, 0.88f, 0.1f + k * 0.055f);
            led = UI.Img(panel, "LED", Color.red).MakeCircle();
            led.rectTransform.Anchor(0.38f, 0.0f, 0.62f, 0.06f);

            pulse = root.gameObject.AddComponent<Pulse>();
            pulse.Amount = 0.025f;

            SetState(null, false);
        }

        static void Antenna(RectTransform root, float x, float angle, Color c)
        {
            var rod = UI.Img(root, "Antena", c);
            var r = rod.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(x, 0.77f);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(6f, 130f);
            r.anchoredPosition = Vector2.zero;
            r.localRotation = Quaternion.Euler(0, 0, angle);
            var tip = UI.Img(rod.transform, "Ponta", c).MakeCircle();
            tip.rectTransform.anchorMin = tip.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            tip.rectTransform.sizeDelta = new Vector2(16f, 16f);
            tip.rectTransform.anchoredPosition = Vector2.zero;
        }

        static void Knob(RectTransform panel, float yMin, float yMax)
        {
            var knob = UI.Img(panel, "Botao", new Color(0.18f, 0.18f, 0.19f)).MakeCircle();
            knob.rectTransform.Anchor(0.15f, yMin, 0.85f, yMax);
            var mark = UI.Img(knob.transform, "Marca", new Color(0.85f, 0.85f, 0.85f));
            mark.rectTransform.Anchor(0.46f, 0.55f, 0.54f, 0.92f);
        }

        static Texture2D ScanlineTexture()
        {
            var tex = new Texture2D(1, 4, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            tex.SetPixels32(new[]
            {
                new Color32(0, 0, 0, 70), new Color32(0, 0, 0, 0),
                new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0)
            });
            tex.Apply();
            return tex;
        }

        void RegenerateNoise()
        {
            for (int i = 0; i < noisePx.Length; i++)
            {
                byte v = (byte)Random.Range(30, 230);
                noisePx[i] = new Color32(v, v, v, 255);
            }
            noiseTex.SetPixels32(noisePx);
            noiseTex.Apply(false);
        }

        public void SetState(Reportagem report, bool fresh)
        {
            on = report != null && !string.IsNullOrEmpty(report.frase);
            button.interactable = on;
            onScreen.SetActive(on);
            noise.gameObject.SetActive(!on);
            noSignal.SetActive(!on);
            led.color = on ? new Color(0.3f, 1f, 0.3f) : new Color(0.9f, 0.15f, 0.1f);
            pulse.On = fresh;
            if (on)
            {
                urgent.text = report.chamadas != null && report.chamadas.Length > 0 ? report.chamadas[0].ToUpperInvariant() : "URGENTE";
                frase.text = report.frase.ToUpperInvariant();
            }
            var g = glow.color;
            g.a = 0f;
            glow.color = g;
        }

        public void StopPulse() => pulse.On = false;

        void Update()
        {
            t += Time.deltaTime;
            if (on)
            {
                urgent.color = Mathf.Sin(t * 8f) > 0 ? new Color(1f, 0.92f, 0.2f) : Color.white;
                liveDot.enabled = Mathf.Repeat(t, 1f) < 0.6f;
                var g = glow.color;
                g.a = 0.12f + 0.05f * Mathf.Sin(t * 3f) + Random.Range(0f, 0.03f);
                glow.color = g;
            }
            else
            {
                noiseTimer += Time.deltaTime;
                if (noiseTimer > 0.06f)
                {
                    noiseTimer = 0f;
                    RegenerateNoise();
                }
            }
        }

        void OnDestroy()
        {
            if (noiseTex != null) Destroy(noiseTex);
        }
    }
}
