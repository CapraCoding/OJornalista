using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jornalista
{
    public class TVBroadcast : MonoBehaviour
    {
        static readonly string[] Fallback =
        {
            "URGENTE!", "IMAGENS FORTES!", "CIDADE EM ALERTA!", "VOCÊ PRECISA VER ISSO!"
        };

        Text caption, reading, gcLocal, gcFrase, ticker, continueLabel;
        Image liveDot, captionBg;
        RectTransform captionRt, tickerRt, tickerMask;
        GameObject gcLocalGo;
        Button continueBtn;
        Action onDone;
        Coroutine routine;

        public void Build()
        {
            var rt = (RectTransform)transform;
            UI.Img(rt, "Escurecer", new Color(0, 0, 0, 0.92f), null, true).rectTransform.Stretch();

            var tv = UI.Img(rt, "TV", new Color(0.08f, 0.08f, 0.09f)).Round();
            tv.rectTransform.Anchor(0.06f, 0.1f, 0.94f, 0.97f);
            var scr = UI.Img(tv.transform, "Tela", new Color(0.10f, 0.16f, 0.32f));
            scr.rectTransform.Anchor(0.012f, 0.02f, 0.988f, 0.98f);
            var s = scr.transform;

            UI.Img(s, "Faixa", new Color(0.14f, 0.24f, 0.45f)).rectTransform.Anchor(0, 0.3f, 1, 0.62f);

            liveDot = UI.Img(s, "Ponto", new Color(1f, 0.15f, 0.15f)).MakeCircle();
            liveDot.rectTransform.Anchor(0.02f, 0.905f, 0.045f, 0.965f);
            UI.Txt(s, "AoVivo", "AO VIVO", 34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Anchor(0.05f, 0.89f, 0.3f, 0.98f);
            UI.Txt(s, "Logo", "TV PLANTÃO", 36, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleRight, FontStyle.BoldAndItalic)
                .WithShadow().rectTransform.Anchor(0.6f, 0.89f, 0.98f, 0.98f);

            captionBg = UI.Img(s, "ChamadaFundo", new Color(0.8f, 0.05f, 0.05f));
            captionRt = captionBg.rectTransform.Anchor(0.04f, 0.70f, 0.96f, 0.87f);
            caption = UI.Txt(captionBg.transform, "Chamada", "", 64, new Color(1f, 0.92f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold)
                .WithShadow(4f);
            caption.rectTransform.Anchor(0.02f, 0.05f, 0.98f, 0.95f);
            caption.resizeTextForBestFit = true;
            caption.resizeTextMinSize = 24;
            caption.resizeTextMaxSize = 70;

            var readBox = UI.Img(s, "LeituraFundo", new Color(0, 0, 0, 0.45f)).Round();
            readBox.rectTransform.Anchor(0.06f, 0.33f, 0.94f, 0.66f);
            reading = UI.Txt(readBox.transform, "Leitura", "", 34, Color.white, TextAnchor.MiddleCenter);
            reading.rectTransform.Anchor(0.03f, 0.06f, 0.97f, 0.94f);
            reading.resizeTextForBestFit = true;
            reading.resizeTextMinSize = 18;
            reading.resizeTextMaxSize = 36;

            var loc = UI.Img(s, "GCLocal", new Color(0.75f, 0.08f, 0.08f));
            loc.rectTransform.Anchor(0.03f, 0.235f, 0.55f, 0.3f);
            gcLocalGo = loc.gameObject;
            gcLocal = UI.Txt(loc.transform, "Local", "", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            gcLocal.rectTransform.Anchor(0.03f, 0, 0.97f, 1);
            var fr = UI.Img(s, "GCFrase", new Color(0.97f, 0.97f, 0.97f));
            fr.rectTransform.Anchor(0.03f, 0.12f, 0.97f, 0.235f);
            gcFrase = UI.Txt(fr.transform, "Frase", "", 44, new Color(0.06f, 0.06f, 0.08f), TextAnchor.MiddleLeft, FontStyle.Bold);
            gcFrase.rectTransform.Anchor(0.015f, 0, 0.985f, 1);
            gcFrase.resizeTextForBestFit = true;
            gcFrase.resizeTextMinSize = 18;
            gcFrase.resizeTextMaxSize = 46;

            var bar = UI.Img(s, "Letreiro", new Color(1f, 0.85f, 0.2f));
            tickerMask = bar.rectTransform.Anchor(0, 0, 1, 0.1f);
            bar.gameObject.AddComponent<RectMask2D>();
            ticker = UI.Txt(bar.transform, "Texto", "", 34, new Color(0.1f, 0.05f, 0.05f), TextAnchor.MiddleLeft, FontStyle.Bold);
            ticker.horizontalOverflow = HorizontalWrapMode.Overflow;
            tickerRt = ticker.rectTransform;
            tickerRt.anchorMin = new Vector2(0, 0);
            tickerRt.anchorMax = new Vector2(0, 1);
            tickerRt.pivot = new Vector2(0, 0.5f);

            continueBtn = UI.Btn(rt, "Continuar", "DESLIGAR", new Color(0.2f, 0.55f, 0.35f), Color.white, 32, Finish);
            continueBtn.GetComponent<RectTransform>().Anchor(0.38f, 0.015f, 0.62f, 0.085f);
            continueLabel = continueBtn.GetComponentInChildren<Text>();

            gameObject.SetActive(false);
        }

        public void Show(Reportagem r, string[] genericas, Action done)
        {
            onDone = done;
            gameObject.SetActive(true);

            var captions = new List<string>();
            if (r.chamadas != null)
                foreach (var c in r.chamadas) if (!string.IsNullOrWhiteSpace(c)) captions.Add(c);
            if (captions.Count == 0 && genericas != null)
                foreach (var c in genericas) if (!string.IsNullOrWhiteSpace(c)) captions.Add(c);
            if (captions.Count == 0) captions.AddRange(Fallback);

            gcLocalGo.SetActive(!string.IsNullOrEmpty(r.local));
            gcLocal.text = string.IsNullOrEmpty(r.local) ? "" : r.local.ToUpperInvariant() + " ///";
            gcFrase.text = (r.frase ?? "").ToUpperInvariant();

            var tick = new List<string>();
            if (genericas != null) tick.AddRange(genericas);
            tick.AddRange(captions);
            ticker.text = "   " + string.Join("   •   ", tick).ToUpperInvariant() + "   •";

            var paragraphs = new List<string>();
            foreach (var p in (r.texto ?? "").Replace("\\n", "\n").Split('\n'))
                if (!string.IsNullOrWhiteSpace(p)) paragraphs.Add(p.Trim());
            if (paragraphs.Count == 0) paragraphs.Add("...");

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(captions, paragraphs));
        }

        IEnumerator Run(List<string> captions, List<string> paragraphs)
        {
            continueLabel.text = "DESLIGAR";
            int capIndex = 0, parIndex = 0;
            float capTimer = 0f, t = 0f;
            float viewW = tickerMask.rect.width;
            float tickerW = ticker.preferredWidth;
            tickerRt.sizeDelta = new Vector2(tickerW, 0);
            float tickerX = viewW;

            caption.text = captions[0];
            reading.text = "";
            Coroutine typing = StartCoroutine(TypeText(paragraphs[0]));
            float parTimer = 0f;
            float parDuration = ReadTime(paragraphs[0]);

            while (true)
            {
                float dt = Time.deltaTime;
                t += dt;

                capTimer += dt;
                if (capTimer > 1.4f)
                {
                    capTimer = 0f;
                    capIndex = (capIndex + 1) % captions.Count;
                    caption.text = captions[capIndex];
                }
                captionRt.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(t * 6f)) * 0.04f);
                caption.color = Mathf.Sin(t * 10f) > 0 ? new Color(1f, 0.92f, 0.2f) : Color.white;
                liveDot.enabled = Mathf.Repeat(t, 1f) < 0.6f;

                tickerX -= 280f * dt;
                if (tickerX < -tickerW) tickerX = viewW;
                tickerRt.anchoredPosition = new Vector2(tickerX, 0);

                parTimer += dt;
                if (parTimer > parDuration && parIndex < paragraphs.Count - 1)
                {
                    parIndex++;
                    parTimer = 0f;
                    parDuration = ReadTime(paragraphs[parIndex]);
                    if (typing != null) StopCoroutine(typing);
                    typing = StartCoroutine(TypeText(paragraphs[parIndex]));
                }
                if (parIndex == paragraphs.Count - 1 && parTimer > parDuration)
                    continueLabel.text = "DESLIGAR TV";

                yield return null;
            }
        }

        static float ReadTime(string p) => Mathf.Clamp(p.Length * 0.045f, 2.2f, 7f);

        IEnumerator TypeText(string p)
        {
            for (int i = 1; i <= p.Length; i++)
            {
                reading.text = p.Substring(0, i);
                yield return new WaitForSeconds(0.018f);
            }
        }

        void Finish()
        {
            StopAllCoroutines();
            routine = null;
            gameObject.SetActive(false);
            var cb = onDone;
            onDone = null;
            cb?.Invoke();
        }
    }
}
