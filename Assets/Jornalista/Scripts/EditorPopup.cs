using UnityEngine;
using UnityEngine.UI;

namespace Jornalista
{
    public class EditorPopup : MonoBehaviour
    {
        static readonly string[] FactLines =
        {
            "De onde você tirou {0}? Isso não está na lauda.",
            "{0}? Leia o texto de novo. A frase tem que bater com os fatos.",
            "Jornalismo é fato, não chute. Revise {0}.",
            "Se isso vai ao ar com {0}, amanhã eu tenho que publicar errata."
        };

        static readonly string[] AgreementLines =
        {
            "Concordância, jornalista! {0}.",
            "Isso não passa nem com o estagiário: {0}.",
            "Quer que o público ria da gente? {0}."
        };

        static readonly string[] CombinationLines =
        {
            "Cada pedaço até existe, mas junto não faz sentido. Monta essa frase direito.",
            "Essa frase está costurada errado. Leia em voz alta e me diga se faz sentido."
        };

        static readonly string[] RepeatLines =
        {
            "Essa frase já foi ao ar {0}. Ninguém quer reprise. Troca um termo.",
            "Já usamos essa {0}. Muda uma palavra e ninguém percebe."
        };

        Text speech, counter;
        RectTransform card;
        float t;

        public static EditorPopup Create(RectTransform parent)
        {
            var bg = UI.Img(parent, "EditorChefe", new Color(0, 0, 0, 0.6f), null, true);
            bg.rectTransform.Stretch();
            var popup = bg.gameObject.AddComponent<EditorPopup>();
            popup.Build(bg.rectTransform);
            bg.gameObject.SetActive(false);
            return popup;
        }

        void Build(RectTransform root)
        {
            card = UI.Img(root, "Cartao", new Color(0.96f, 0.94f, 0.88f)).Round().rectTransform.Anchor(0.15f, 0.22f, 0.85f, 0.78f);

            var frame = UI.Img(card, "Retrato", new Color(0.25f, 0.30f, 0.38f)).Round();
            frame.rectTransform.Anchor(0.03f, 0.10f, 0.29f, 0.90f);
            var f = frame.transform;
            var skin = new Color(0.90f, 0.75f, 0.62f);
            var gray = new Color(0.62f, 0.62f, 0.65f);
            var dark = new Color(0.12f, 0.12f, 0.14f);

            UI.Img(f, "Terno", new Color(0.15f, 0.15f, 0.18f)).Round().rectTransform.Anchor(0.12f, 0f, 0.88f, 0.36f);
            UI.Img(f, "Camisa", Color.white).rectTransform.Anchor(0.42f, 0.18f, 0.58f, 0.36f);
            UI.Img(f, "Gravata", new Color(0.7f, 0.1f, 0.1f)).rectTransform.Anchor(0.47f, 0.03f, 0.53f, 0.34f);
            UI.Img(f, "Rosto", skin).MakeCircle().rectTransform.Anchor(0.20f, 0.32f, 0.80f, 0.86f);
            UI.Img(f, "Cabelo", gray).Round().rectTransform.Anchor(0.27f, 0.79f, 0.73f, 0.87f);
            UI.Img(f, "LenteE", dark).MakeCircle().rectTransform.Anchor(0.30f, 0.56f, 0.46f, 0.66f);
            UI.Img(f, "LenteD", dark).MakeCircle().rectTransform.Anchor(0.54f, 0.56f, 0.70f, 0.66f);
            UI.Img(f, "Ponte", dark).rectTransform.Anchor(0.45f, 0.605f, 0.55f, 0.62f);
            var browL = UI.Img(f, "SobrancelhaE", gray).rectTransform.Anchor(0.31f, 0.69f, 0.46f, 0.715f);
            browL.localRotation = Quaternion.Euler(0, 0, -14f);
            var browR = UI.Img(f, "SobrancelhaD", gray).rectTransform.Anchor(0.54f, 0.69f, 0.69f, 0.715f);
            browR.localRotation = Quaternion.Euler(0, 0, 14f);
            UI.Img(f, "Bigode", gray).Round().rectTransform.Anchor(0.37f, 0.43f, 0.63f, 0.49f);

            UI.Txt(card, "Nome", "EDITOR-CHEFE", 30, new Color(0.7f, 0.1f, 0.1f), TextAnchor.UpperLeft, FontStyle.Bold)
                .rectTransform.Anchor(0.33f, 0.80f, 0.70f, 0.94f);
            counter = UI.Txt(card, "Contador", "", 24, new Color(0.45f, 0.45f, 0.5f), TextAnchor.UpperRight, FontStyle.Bold);
            counter.rectTransform.Anchor(0.70f, 0.80f, 0.96f, 0.94f);

            speech = UI.Txt(card, "Fala", "", 34, new Color(0.12f, 0.12f, 0.14f), TextAnchor.UpperLeft);
            speech.rectTransform.Anchor(0.33f, 0.28f, 0.96f, 0.78f);
            speech.verticalOverflow = VerticalWrapMode.Truncate;
            speech.resizeTextForBestFit = true;
            speech.resizeTextMinSize = 18;
            speech.resizeTextMaxSize = 34;

            UI.Btn(card, "Entendi", "ENTENDI", new Color(0.8f, 0.12f, 0.12f), Color.white, 30, Hide)
                .GetComponent<RectTransform>().Anchor(0.64f, 0.07f, 0.95f, 0.23f);
        }

        public void ShowFact(string quoted, string counterText) => Show(Pick(FactLines, quoted), counterText);
        public void ShowAgreement(string detail, string counterText) => Show(Pick(AgreementLines, detail), counterText);
        public void ShowCombination(string counterText) => Show(Pick(CombinationLines, ""), counterText);
        public void ShowRepeat(string when) => Show(Pick(RepeatLines, when), "");

        static string Pick(string[] lines, string arg) => string.Format(lines[Random.Range(0, lines.Length)], arg);

        public void Show(string text, string counterText)
        {
            speech.text = text;
            counter.text = counterText ?? "";
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            t = 0f;
        }

        public void Hide() => gameObject.SetActive(false);

        void Update()
        {
            if (t >= 0.2f) return;
            t += Time.deltaTime;
            card.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(t / 0.2f));
        }
    }
}
