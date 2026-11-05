using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Jornalista
{
    public class WorkScreen : MonoBehaviour
    {
        static readonly Color ScreenBg = new Color(0.09f, 0.11f, 0.15f);
        static readonly Color Panel = new Color(0.15f, 0.18f, 0.24f);
        static readonly Color Option = new Color(0.24f, 0.28f, 0.36f);
        static readonly Color Accent = new Color(1f, 0.80f, 0.20f);
        static readonly Color Muted = new Color(0.65f, 0.70f, 0.78f);
        static readonly Color Bad = new Color(1f, 0.42f, 0.38f);

        GameManager gm;
        RectTransform monitor, screen;
        Vector2 monitorBase;

        GameObject filesView, manualPanel;
        Text filesHeader;
        RectTransform filesList;

        GameObject editorView;
        Text header, body, previewLocal, previewFrase, feedback;
        RectTransform slotsArea;
        Button publish;

        GameObject endPanel;
        Text endText, endTitle;
        Button nextButton;
        RectTransform homeButton, restartButton;
        Text errorsLabel;
        int errorsToday;

        List<Noticia> news = new List<Noticia>();
        bool[] done;
        int current = -1;
        int[] selected;
        string[][] shown;
        List<Image>[] optionImages;
        List<Text>[] optionTexts;
        string[] labels;
        bool[][] okOption;
        EditorPopup editor;

        public void Build(GameManager gm)
        {
            this.gm = gm;
            var rt = (RectTransform)transform;
            var art = ArtConfig.I;

            UI.Img(rt, "Fundo", new Color(0.20f, 0.30f, 0.32f), art.trabalhoFundo).rectTransform.Stretch();
            if (art.trabalhoFundo == null)
            {
                UI.Img(rt, "Mesa", new Color(0.42f, 0.30f, 0.20f)).rectTransform.Anchor(0, 0, 1, 0.08f);
                for (int i = 0; i < 6; i++)
                    UI.Img(rt, "Jornal", i % 2 == 0 ? new Color(0.92f, 0.90f, 0.84f) : new Color(0.82f, 0.80f, 0.74f))
                        .rectTransform.Anchor(0.015f, 0.08f + i * 0.025f, 0.085f, 0.08f + (i + 1) * 0.025f);
                UI.Img(rt, "Caneca", new Color(0.85f, 0.25f, 0.2f)).Round().rectTransform.Anchor(0.925f, 0.08f, 0.975f, 0.2f);
            }

            monitor = UI.Img(rt, "Monitor", new Color(0.12f, 0.12f, 0.13f)).Round().rectTransform.Anchor(0.1f, 0.06f, 0.9f, 0.97f);
            monitorBase = monitor.anchoredPosition;
            screen = UI.Img(monitor, "Tela", ScreenBg).rectTransform.Anchor(0.015f, 0.025f, 0.985f, 0.975f);

            BuildFilesView();
            BuildEditorView();
            BuildEndPanel();
            BuildManual();
            editor = EditorPopup.Create(rt);
        }

        void BuildFilesView()
        {
            filesView = UI.NewRect(screen, "Arquivos").Stretch().gameObject;
            filesHeader = UI.Txt(filesView.transform, "Titulo", "", 38, Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            filesHeader.rectTransform.Anchor(0.03f, 0.87f, 0.76f, 0.97f);
            UI.Txt(filesView.transform, "Sub", "Abra uma lauda para escrever a frase do GC.", 28, Muted, TextAnchor.MiddleLeft)
                .rectTransform.Anchor(0.03f, 0.80f, 0.97f, 0.87f);

            filesList = UI.NewRect(filesView.transform, "Lista").Anchor(0.03f, 0.05f, 0.97f, 0.78f);
            var v = filesList.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.UpperCenter;
        }

        void BuildManual()
        {
            var lists = gm.Banco.manual;
            if (lists == null || lists.Length == 0) return;

            var sb = new System.Text.StringBuilder();
            foreach (var l in lists)
            {
                if (l == null || l.itens == null || l.itens.Length == 0) continue;
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append("<b>").Append((l.chave ?? "").ToUpperInvariant()).Append("</b>");
                foreach (var item in l.itens)
                    if (!string.IsNullOrWhiteSpace(item)) sb.Append("\n• ").Append(item.Trim());
            }
            if (sb.Length == 0) return;

            UI.Btn(filesView.transform, "Manual", "MANUAL", Option, Color.white, 28, () => manualPanel.SetActive(true))
                .GetComponent<RectTransform>().Anchor(0.78f, 0.88f, 0.97f, 0.97f);

            var p = UI.Img(screen, "ManualRedacao", new Color(0.05f, 0.06f, 0.09f, 0.97f), null, true);
            p.rectTransform.Stretch();
            manualPanel = p.gameObject;
            UI.Txt(p.transform, "Titulo", "MANUAL DE REDAÇÃO", 40, Accent, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Anchor(0.04f, 0.88f, 0.75f, 0.98f);
            UI.Btn(p.transform, "Fechar", "FECHAR", Option, Color.white, 28, () => manualPanel.SetActive(false))
                .GetComponent<RectTransform>().Anchor(0.78f, 0.89f, 0.97f, 0.98f);
            var body = UI.Txt(p.transform, "Texto", sb.ToString(), 26, new Color(0.9f, 0.92f, 0.95f), TextAnchor.UpperLeft);
            body.rectTransform.Anchor(0.04f, 0.03f, 0.96f, 0.86f);
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 12;
            body.resizeTextMaxSize = 26;
            manualPanel.SetActive(false);
        }

        void BuildEditorView()
        {
            editorView = UI.NewRect(screen, "Lauda").Stretch().gameObject;
            var t = editorView.transform;

            UI.Btn(t, "Voltar", "< ARQUIVOS", Option, Color.white, 24, ShowFiles)
                .GetComponent<RectTransform>().Anchor(0.02f, 0.91f, 0.17f, 0.98f);
            header = UI.Txt(t, "Titulo", "", 36, Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            header.rectTransform.Anchor(0.19f, 0.90f, 0.78f, 0.99f);
            errorsLabel = UI.Txt(t, "Erros", "", 28, Bad, TextAnchor.MiddleRight, FontStyle.Bold);
            errorsLabel.rectTransform.Anchor(0.78f, 0.90f, 0.98f, 0.99f);

            var bodyBox = UI.Img(t, "TextoBox", Panel).Round();
            bodyBox.rectTransform.Anchor(0.02f, 0.53f, 0.98f, 0.89f);
            body = UI.Txt(bodyBox.transform, "Texto", "", 28, new Color(0.92f, 0.93f, 0.95f), TextAnchor.UpperLeft);
            body.rectTransform.Anchor(0.02f, 0.04f, 0.98f, 0.96f);
            body.verticalOverflow = VerticalWrapMode.Truncate;
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 16;
            body.resizeTextMaxSize = 30;

            slotsArea = UI.NewRect(t, "Campos").Anchor(0.02f, 0.27f, 0.98f, 0.51f);
            var v = slotsArea.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = true;

            var gcLocal = UI.Img(t, "GCLocal", new Color(0.70f, 0.08f, 0.08f));
            gcLocal.rectTransform.Anchor(0.02f, 0.205f, 0.98f, 0.255f);
            previewLocal = UI.Txt(gcLocal.transform, "Local", "", 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            previewLocal.rectTransform.Anchor(0.01f, 0.05f, 0.99f, 0.95f);
            previewLocal.resizeTextForBestFit = true;
            previewLocal.resizeTextMinSize = 10;
            previewLocal.resizeTextMaxSize = 24;
            var gcFrase = UI.Img(t, "GCFrase", new Color(0.97f, 0.97f, 0.97f));
            gcFrase.rectTransform.Anchor(0.02f, 0.12f, 0.98f, 0.205f);
            previewFrase = UI.Txt(gcFrase.transform, "Frase", "", 34, new Color(0.08f, 0.08f, 0.1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            previewFrase.rectTransform.Anchor(0.015f, 0, 0.985f, 1);
            previewFrase.resizeTextForBestFit = true;
            previewFrase.resizeTextMinSize = 16;
            previewFrase.resizeTextMaxSize = 34;

            feedback = UI.Txt(t, "Feedback", "", 26, Bad, TextAnchor.MiddleLeft, FontStyle.Bold);
            feedback.rectTransform.Anchor(0.02f, 0.01f, 0.72f, 0.11f);
            feedback.verticalOverflow = VerticalWrapMode.Truncate;
            feedback.resizeTextForBestFit = true;
            feedback.resizeTextMinSize = 14;
            feedback.resizeTextMaxSize = 26;

            publish = UI.Btn(t, "Publicar", "PUBLICAR", new Color(0.80f, 0.12f, 0.12f), Color.white, 36, Publish);
            publish.GetComponent<RectTransform>().Anchor(0.74f, 0.015f, 0.98f, 0.105f);
        }

        void BuildEndPanel()
        {
            var p = UI.Img(screen, "FimExpediente", new Color(0, 0, 0, 0.85f), null, true);
            p.rectTransform.Stretch();
            endPanel = p.gameObject;
            endTitle = UI.Txt(p.transform, "Titulo", "PUBLICADO!", 64, Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            endTitle.rectTransform.Anchor(0.05f, 0.6f, 0.95f, 0.78f);
            endText = UI.Txt(p.transform, "Texto", "", 38, Color.white);
            endText.rectTransform.Anchor(0.05f, 0.42f, 0.95f, 0.6f);
            homeButton = UI.Btn(p.transform, "VoltarCasa", "VOLTAR PARA CASA", new Color(0.2f, 0.55f, 0.35f), Color.white, 36,
                () => gm.LeaveWork()).GetComponent<RectTransform>();
            nextButton = UI.Btn(p.transform, "ProximaLauda", "PRÓXIMA LAUDA", Option, Color.white, 36, () =>
            {
                endPanel.SetActive(false);
                ShowFiles();
            });
            nextButton.GetComponent<RectTransform>().Anchor(0.52f, 0.18f, 0.84f, 0.32f);
            restartButton = UI.Btn(p.transform, "Recomecar", "COMEÇAR DE NOVO", new Color(0.8f, 0.12f, 0.12f), Color.white, 36,
                () => gm.RestartAfterFiring()).GetComponent<RectTransform>();
            restartButton.Anchor(0.32f, 0.18f, 0.68f, 0.32f);
            restartButton.gameObject.SetActive(false);
            endPanel.SetActive(false);
        }

        public void Enter(List<Noticia> todays)
        {
            news = todays ?? new List<Noticia>();
            done = new bool[news.Count];
            current = -1;
            errorsToday = 0;
            UpdateErrors();
            if (manualPanel != null) manualPanel.SetActive(false);
            editor.Hide();
            endPanel.SetActive(false);

            if (news.Count == 0)
            {
                ShowEndPanel(NewsLoader.LastError ?? "Nenhuma notícia cadastrada para hoje no noticias.json.");
                return;
            }
            ShowFiles();
        }

        int DoneCount()
        {
            int c = 0;
            foreach (var d in done) if (d) c++;
            return c;
        }

        void ShowFiles()
        {
            editorView.SetActive(false);
            filesView.SetActive(true);
            filesHeader.text = $"ARQUIVOS DO DIA - {DoneCount()}/{news.Count} publicadas";

            ClearChildren(filesList);
            for (int i = 0; i < news.Count; i++)
            {
                int idx = i;
                string title = string.IsNullOrEmpty(news[i].titulo) ? $"LAUDA {i + 1}" : news[i].titulo.ToUpperInvariant();
                string label = done[i] ? $"{title}   (PUBLICADA)" : title;
                var b = UI.Btn(filesList, "Arquivo", label, done[i] ? new Color(0.17f, 0.22f, 0.2f) : Option,
                    done[i] ? Muted : Color.white, 34, () => OpenFile(idx));
                var txt = b.GetComponentInChildren<Text>();
                txt.alignment = TextAnchor.MiddleLeft;
                txt.rectTransform.Anchor(0.03f, 0.05f, 0.97f, 0.95f);
                b.interactable = !done[i];
                var le = b.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 90;
                le.minHeight = 70;
            }
        }

        void OpenFile(int index)
        {
            current = index;
            var n = news[index];
            filesView.SetActive(false);
            editorView.SetActive(true);

            header.text = string.IsNullOrEmpty(n.titulo) ? "LAUDA" : n.titulo.ToUpperInvariant();
            body.text = (n.texto ?? "").Replace("\\n", "\n");
            previewLocal.text = NewsLoader.LocalLine(n.local);
            previewLocal.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(n.local));
            feedback.text = "";

            BuildSlots(n);
            AnalyzeCombos(n);
            UpdatePreview();
        }

        void AnalyzeCombos(Noticia n)
        {
            int count = shown.Length;
            okOption = null;
            if (count == 0) return;
            long total = 1;
            for (int s = 0; s < count; s++)
            {
                if (shown[s].Length == 0) return;
                total *= shown[s].Length;
            }
            if (total > 4096) return;

            okOption = new bool[count][];
            for (int s = 0; s < count; s++) okOption[s] = new bool[shown[s].Length];
            var idx = new int[count];
            var parts = new string[count];
            for (long c = 0; c < total; c++)
            {
                long rem = c;
                for (int s = 0; s < count; s++)
                {
                    int len = shown[s].Length;
                    idx[s] = (int)(rem % len);
                    rem /= len;
                    parts[s] = shown[s][idx[s]];
                }
                if (HeadlineValidator.IsCorrect(string.Join(" ", parts), n.frasesValidas))
                    for (int s = 0; s < count; s++) okOption[s][idx[s]] = true;
            }
        }

        List<int> WrongSlots()
        {
            var wrong = new List<int>();
            if (okOption == null) return wrong;
            for (int s = 0; s < selected.Length; s++)
                if (selected[s] >= 0 && !okOption[s][selected[s]]) wrong.Add(s);
            return wrong;
        }

        void BuildSlots(Noticia n)
        {
            ClearChildren(slotsArea);
            int count = n.campos != null ? n.campos.Length : 0;
            selected = new int[count];
            shown = new string[count][];
            labels = new string[count];
            optionImages = new List<Image>[count];
            optionTexts = new List<Text>[count];

            for (int s = 0; s < count; s++)
            {
                selected[s] = -1;
                var campo = n.campos[s];
                labels[s] = string.IsNullOrEmpty(campo.rotulo) ? $"bloco {s + 1}" : campo.rotulo;
                var opts = new List<string>(campo.opcoes ?? new string[0]);
                if (gm.Banco.embaralharOpcoes) Shuffle(opts);
                shown[s] = opts.ToArray();
                optionImages[s] = new List<Image>();
                optionTexts[s] = new List<Text>();

                var row = UI.NewRect(slotsArea, "Campo");
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 10;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;

                var label = UI.Txt(row, "Rotulo", (campo.rotulo ?? "").ToUpperInvariant(), 24, Muted,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                var lle = label.gameObject.AddComponent<LayoutElement>();
                lle.minWidth = 170;
                lle.preferredWidth = 170;
                lle.flexibleWidth = 0;

                for (int o = 0; o < shown[s].Length; o++)
                {
                    int si = s, oi = o;
                    var b = UI.Btn(row, "Opcao", shown[s][o], Option, Color.white, 28, () => Select(si, oi));
                    var le = b.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 10;
                    le.flexibleWidth = 1;
                    var txt = b.GetComponentInChildren<Text>();
                    txt.resizeTextForBestFit = true;
                    txt.resizeTextMinSize = 14;
                    txt.resizeTextMaxSize = 28;
                    optionImages[s].Add((Image)b.targetGraphic);
                    optionTexts[s].Add(txt);
                }
            }
        }

        void Select(int slot, int option)
        {
            selected[slot] = option;
            for (int o = 0; o < optionImages[slot].Count; o++)
            {
                bool on = o == option;
                optionImages[slot][o].color = on ? Accent : Option;
                optionTexts[slot][o].color = on ? new Color(0.1f, 0.1f, 0.1f) : Color.white;
            }
            feedback.text = "";
            UpdatePreview();
        }

        string CurrentPhrase(bool withBlanks)
        {
            var parts = new List<string>();
            for (int s = 0; s < selected.Length; s++)
            {
                if (selected[s] >= 0) parts.Add(shown[s][selected[s]]);
                else if (withBlanks) parts.Add("_____");
            }
            return string.Join(" ", parts);
        }

        void UpdatePreview()
        {
            bool complete = selected.Length > 0;
            foreach (var s in selected) if (s < 0) complete = false;
            previewFrase.text = CurrentPhrase(true).ToUpperInvariant();
            publish.interactable = complete;
        }

        void Publish()
        {
            if (current < 0) return;
            var n = news[current];
            string frase = CurrentPhrase(false);

            if (!HeadlineValidator.IsCorrect(frase, n.frasesValidas))
            {
                errorsToday++;
                UpdateErrors();
                int limit = gm.Banco.errosParaIrPraCasa;
                var wrong = WrongSlots();
                foreach (var w in wrong)
                {
                    optionImages[w][selected[w]].color = Bad;
                    optionTexts[w][selected[w]].color = Color.white;
                }
                if (errorsToday >= limit)
                {
                    SentHome();
                    return;
                }

                string warn = limit - errorsToday == 1 ? " Mais um erro e você vai pra casa." : "";
                string count = $"ERRO {errorsToday} DE {limit}";
                string mismatch = Agreement.Mismatch(frase);
                if (mismatch != null)
                {
                    Reject($"Erro de concordância: {mismatch}." + warn);
                    editor.ShowAgreement(mismatch, count);
                }
                else if (wrong.Count > 0)
                {
                    var quoted = new List<string>();
                    var where = new List<string>();
                    foreach (var w in wrong)
                    {
                        quoted.Add($"“{shown[w][selected[w]]}”");
                        where.Add($"{quoted[quoted.Count - 1]} ({labels[w]})");
                    }
                    Reject((wrong.Count == 1 ? "Erro em " : "Erros em ") + string.Join(", ", where) + ": não bate com a lauda." + warn);
                    editor.ShowFact(string.Join(" e ", quoted), count);
                }
                else
                {
                    Reject("Os blocos não combinam entre si." + warn);
                    editor.ShowCombination(count);
                }
                return;
            }

            if (gm.Banco.naoRepetirFrases)
            {
                int day = AiredHistory.AiredOnDay(frase);
                if (day > 0)
                {
                    string when = day == gm.Day ? "hoje" : $"no dia {day}";
                    Reject($"Essa frase já foi ao ar {when}. Troque um termo.");
                    editor.ShowRepeat(when);
                    return;
                }
            }

            AiredHistory.Add(frase, gm.Day);
            done[current] = true;
            gm.RegisterReport(n, frase);
            ShowEndPanel();
        }

        void ShowEndPanel(string message = null)
        {
            int doneCount = DoneCount();
            bool remaining = doneCount < news.Count;
            endTitle.text = message == null ? "PUBLICADO!" : "SEM PAUTA HOJE";
            endTitle.color = Accent;
            homeButton.gameObject.SetActive(true);
            restartButton.gameObject.SetActive(false);
            endText.text = message ?? (remaining
                ? $"{doneCount} de {news.Count} manchetes publicadas hoje.\nA reportagem já está no outdoor da rua."
                : $"Todas as {news.Count} manchetes do dia publicadas.\nA última está no outdoor da rua.");
            nextButton.gameObject.SetActive(remaining);
            if (remaining) homeButton.Anchor(0.16f, 0.18f, 0.48f, 0.32f);
            else homeButton.Anchor(0.32f, 0.18f, 0.68f, 0.32f);
            endPanel.SetActive(true);
        }

        void SentHome()
        {
            bool fired = gm.RegisterBadDay();
            int strikes = gm.Strikes;
            int limit = gm.Banco.advertenciasParaDemissao;

            nextButton.gameObject.SetActive(false);
            endTitle.color = Bad;
            if (fired)
            {
                endTitle.text = "DEMITIDO";
                endText.text = $"{limit} advertências.\nO jornal dispensou seus serviços.";
                homeButton.gameObject.SetActive(false);
                restartButton.gameObject.SetActive(true);
            }
            else
            {
                endTitle.text = "VÁ PRA CASA";
                endText.text = $"{errorsToday} manchetes erradas hoje. O editor te mandou embora.\n" +
                               $"Advertência {strikes} de {limit}." +
                               (strikes == limit - 1 ? " Mais uma e você está fora." : "") +
                               $"\n<size=26>{gm.Banco.diasCorretosParaRetirarAdvertencia} dias com manchetes certas retiram 1 advertência.</size>";
                homeButton.gameObject.SetActive(true);
                restartButton.gameObject.SetActive(false);
                homeButton.Anchor(0.32f, 0.18f, 0.68f, 0.32f);
            }
            endPanel.SetActive(true);
        }

        void UpdateErrors()
        {
            errorsLabel.text = errorsToday > 0 ? $"ERROS {errorsToday}/{gm.Banco.errosParaIrPraCasa}" : "";
        }

        void Reject(string msg)
        {
            feedback.text = msg;
            StopAllCoroutines();
            StartCoroutine(Shake());
        }

        IEnumerator Shake()
        {
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                monitor.anchoredPosition = monitorBase + new Vector2(Mathf.Sin(t * 70f) * 14f * (1f - t / 0.35f), 0);
                yield return null;
            }
            monitor.anchoredPosition = monitorBase;
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i).gameObject;
                c.SetActive(false);
                Destroy(c);
            }
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
