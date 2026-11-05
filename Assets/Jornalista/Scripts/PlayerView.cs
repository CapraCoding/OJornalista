using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Jornalista
{
    public class PlayerView : MonoBehaviour
    {
        public bool Walking;
        public int Facing = 1;

        RectTransform visual, legL, legR;
        Image frameImage;
        float t, frameTimer;
        int frame;

        RectTransform bubble;
        Text bubbleText;
        Coroutine bubbleRoutine;

        public static PlayerView Create(Transform parent)
        {
            var root = UI.NewRect(parent, "Jornalista");
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(150, 320);
            var view = root.gameObject.AddComponent<PlayerView>();
            view.Build(root);
            return view;
        }

        void Build(RectTransform root)
        {
            visual = UI.NewRect(root, "Visual").Stretch();
            BuildBubble(root);
            var art = ArtConfig.I;

            if (art.HasCharacterFrames)
            {
                frameImage = UI.Img(visual, "Sprite", Color.white, art.jornalistaParado[0]);
                frameImage.rectTransform.Stretch();
                frameImage.preserveAspect = true;
                return;
            }

            var pants = new Color(0.20f, 0.22f, 0.28f);
            var coat = new Color(0.70f, 0.60f, 0.43f);
            var skin = new Color(0.93f, 0.78f, 0.64f);
            var hat = new Color(0.30f, 0.30f, 0.33f);

            legL = Part("PernaTras", 62, 125, 28, 125, pants, new Vector2(0.5f, 1f));
            legR = Part("PernaFrente", 88, 125, 28, 125, pants, new Vector2(0.5f, 1f));
            Part("Casaco", 75, 100, 104, 140, coat, new Vector2(0.5f, 0f)).GetComponent<Image>().Round();
            Part("Camisa", 82, 200, 26, 40, Color.white, new Vector2(0.5f, 0f));
            Part("Gravata", 82, 170, 10, 62, new Color(0.7f, 0.15f, 0.15f), new Vector2(0.5f, 0f));
            Part("Maleta", 115, 70, 52, 40, new Color(0.40f, 0.25f, 0.14f), new Vector2(0.5f, 0f)).GetComponent<Image>().Round();
            Part("Cabeca", 78, 234, 76, 76, skin, new Vector2(0.5f, 0f)).GetComponent<Image>().MakeCircle();
            Part("Olho", 98, 272, 9, 9, new Color(0.1f, 0.1f, 0.1f), new Vector2(0.5f, 0.5f)).GetComponent<Image>().MakeCircle();
            Part("Aba", 78, 290, 104, 10, hat, new Vector2(0.5f, 0f));
            Part("Copa", 78, 298, 68, 26, hat, new Vector2(0.5f, 0f));
        }

        void BuildBubble(RectTransform root)
        {
            bubble = UI.NewRect(root, "Pensamento").Place(125, 335, 96, 96, new Vector2(0.5f, 0f));
            UI.Img(bubble, "Bolinha1", Color.white).MakeCircle().rectTransform.Place(-8, -14, 18, 18, new Vector2(0.5f, 0.5f));
            UI.Img(bubble, "Bolinha2", Color.white).MakeCircle().rectTransform.Place(12, 2, 28, 28, new Vector2(0.5f, 0.5f));
            var main = UI.Img(bubble, "Balao", Color.white).MakeCircle();
            main.rectTransform.Stretch();
            bubbleText = UI.Txt(main.transform, "Texto", "?", 64, new Color(0.1f, 0.1f, 0.1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            bubbleText.rectTransform.Stretch();
            bubble.gameObject.SetActive(false);
        }

        public void Think(string text, float duration = 1.6f)
        {
            if (bubble == null || !isActiveAndEnabled) return;
            if (bubbleRoutine != null) StopCoroutine(bubbleRoutine);
            bubbleText.text = text;
            bubbleRoutine = StartCoroutine(ThinkRoutine(duration));
        }

        IEnumerator ThinkRoutine(float duration)
        {
            bubble.gameObject.SetActive(true);
            float t0 = 0f;
            while (t0 < 0.18f)
            {
                t0 += Time.deltaTime;
                bubble.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.1f, t0 / 0.18f);
                yield return null;
            }
            bubble.localScale = Vector3.one;
            yield return new WaitForSeconds(duration);
            bubble.gameObject.SetActive(false);
            bubbleRoutine = null;
        }

        void OnDisable()
        {
            if (bubble != null) bubble.gameObject.SetActive(false);
            bubbleRoutine = null;
        }

        RectTransform Part(string name, float x, float y, float w, float h, Color c, Vector2 pivot)
        {
            var img = UI.Img(visual, name, c);
            return img.rectTransform.Place(x, y, w, h, pivot);
        }

        void Update()
        {
            if (visual == null) return;

            var s = visual.localScale;
            s.x = Facing >= 0 ? 1f : -1f;
            visual.localScale = s;

            if (frameImage != null)
            {
                AnimateFrames();
                return;
            }

            if (Walking)
            {
                t += Time.deltaTime * 9f;
                float swing = Mathf.Sin(t) * 26f;
                legL.localRotation = Quaternion.Euler(0, 0, swing);
                legR.localRotation = Quaternion.Euler(0, 0, -swing);
                visual.anchoredPosition = new Vector2(0, Mathf.Abs(Mathf.Sin(t)) * 7f);
            }
            else
            {
                t = 0f;
                legL.localRotation = Quaternion.identity;
                legR.localRotation = Quaternion.identity;
                visual.anchoredPosition = Vector2.zero;
            }
        }

        void AnimateFrames()
        {
            var art = ArtConfig.I;
            var frames = Walking && art.jornalistaAndando != null && art.jornalistaAndando.Length > 0
                ? art.jornalistaAndando
                : art.jornalistaParado;

            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / Mathf.Max(1f, art.fps))
            {
                frameTimer = 0f;
                frame++;
            }
            var sprite = frames[frame % frames.Length];
            if (sprite != null) frameImage.sprite = sprite;
        }
    }
}
