using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Jornalista
{
    [Serializable]
    public class BancoDeNoticias
    {
        public int manchetesPorDia = 3;
        public bool embaralharOpcoes = true;
        public bool naoRepetirFrases = true;
        public int errosParaIrPraCasa = 3;
        public int advertenciasParaDemissao = 3;
        public int diasCorretosParaRetirarAdvertencia = 3;
        public int opcoesPorCampo = 4;
        public string sufixoFrase = "//";
        public string[] chamadasGenericas;
        public Modelo[] modelos;
        public Lista[] valores;
        public Lista[] termos;
        public Lista[] manual;
        public Dia[] dias;
    }

    [Serializable]
    public class Modelo
    {
        public string titulo;
        public string texto;
        public string local;
        public string[] frasesValidas;
        public string[] chamadasSensacionalistas;
    }

    [Serializable]
    public class Lista
    {
        public string chave;
        public string[] itens;
    }

    [Serializable]
    public class Dia
    {
        public string nome;
        public Noticia[] noticias;
    }

    [Serializable]
    public class Noticia
    {
        public string titulo;
        public string texto;
        public string local;
        public Campo[] campos;
        public string[] frasesValidas;
        public string[] chamadasSensacionalistas;
    }

    [Serializable]
    public class Campo
    {
        public string rotulo;
        public string[] opcoes;
    }

    [Serializable]
    public class Reportagem
    {
        public string titulo;
        public string texto;
        public string local;
        public string frase;
        public string[] chamadas;
        public int dia;
    }

    [Serializable]
    public class DicionarioSinonimos
    {
        public GrupoSinonimos[] grupos;
    }

    [Serializable]
    public class GrupoSinonimos
    {
        public string nome;
        public string[] termos;
    }

    public static class NewsLoader
    {
        public static string LastError { get; private set; }
        public static string Suffix { get; private set; } = "//";

        public static string LocalLine(string local)
        {
            if (string.IsNullOrWhiteSpace(local)) return "";
            return local.Trim().TrimEnd('.').ToUpperInvariant() + (string.IsNullOrEmpty(Suffix) ? "" : " " + Suffix);
        }

        public static BancoDeNoticias Load()
        {
            LastError = null;
            var asset = Resources.Load<TextAsset>("noticias");
            if (asset == null)
            {
                LastError = "Arquivo Assets/Resources/noticias.json não encontrado.";
                Debug.LogWarning(LastError);
                return new BancoDeNoticias();
            }
            try
            {
                var banco = JsonUtility.FromJson<BancoDeNoticias>(asset.text) ?? new BancoDeNoticias();
                if (banco.manchetesPorDia <= 0) banco.manchetesPorDia = 3;
                if (banco.errosParaIrPraCasa <= 0) banco.errosParaIrPraCasa = 3;
                if (banco.advertenciasParaDemissao <= 0) banco.advertenciasParaDemissao = 3;
                if (banco.diasCorretosParaRetirarAdvertencia <= 0) banco.diasCorretosParaRetirarAdvertencia = 3;
                if (banco.opcoesPorCampo < 2) banco.opcoesPorCampo = 4;
                if (banco.sufixoFrase == null) banco.sufixoFrase = "//";
                Suffix = banco.sufixoFrase.Trim();
                return banco;
            }
            catch (Exception e)
            {
                LastError = "Erro lendo noticias.json: " + e.Message;
                Debug.LogError(LastError);
                return new BancoDeNoticias();
            }
        }
    }

    public static class HeadlineValidator
    {
        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", " ");
            s = s.TrimEnd('.', '!', '?', ' ', '/');
            return s;
        }

        public static bool IsCorrect(string frase, string[] validas) =>
            IsValid(frase, validas) && Agreement.Mismatch(frase) == null;

        public static bool IsValid(string frase, string[] validas)
        {
            if (validas == null) return false;
            string n = Normalize(frase);
            foreach (var v in validas)
            {
                if (string.IsNullOrWhiteSpace(v)) continue;
                if (Regex.IsMatch(n, PatternToRegex(Normalize(v)))) return true;
            }
            return false;
        }

        static string PatternToRegex(string pattern)
        {
            var sb = new StringBuilder("^");
            int i = 0;
            while (i < pattern.Length)
            {
                int open = pattern.IndexOf('{', i);
                int close = open < 0 ? -1 : pattern.IndexOf('}', open);
                if (open < 0 || close < 0)
                {
                    sb.Append(Synonyms.LiteralToRegex(pattern.Substring(i)));
                    break;
                }
                sb.Append(Synonyms.LiteralToRegex(pattern.Substring(i, open - i)));
                var alts = pattern.Substring(open + 1, close - open - 1).Split('|');
                sb.Append("(?:");
                for (int a = 0; a < alts.Length; a++)
                {
                    if (a > 0) sb.Append('|');
                    sb.Append(Synonyms.LiteralToRegex(alts[a].Trim()));
                }
                sb.Append(')');
                i = close + 1;
            }
            sb.Append('$');
            return sb.ToString();
        }
    }

    public static class Agreement
    {
        static readonly Dictionary<string, char> Lexicon = new Dictionary<string, char>
        {
            { "homem", 'm' }, { "homens", 'm' }, { "mulher", 'f' }, { "mulheres", 'f' },
            { "condutor", 'm' }, { "condutores", 'm' }, { "cidadão", 'm' }, { "cidadã", 'f' },
            { "rapaz", 'm' }, { "ninguém", 'm' }, { "criança", 'f' }, { "vítima", 'f' }, { "vítimas", 'f' },
            { "pessoa", 'f' }, { "pessoas", 'f' }, { "polícia", 'f' }, { "moto", 'f' }, { "motos", 'f' },
            { "garupa", '?' }, { "motorista", '?' }, { "motoristas", '?' }, { "motociclista", '?' },
            { "motociclistas", '?' }, { "ciclista", '?' }, { "pedestre", '?' }, { "jovem", '?' },
            { "adolescente", '?' }, { "ocupante", '?' }, { "acompanhante", '?' }, { "idoso", 'm' }, { "idosa", 'f' }
        };

        static readonly HashSet<string> Determiners = new HashSet<string>
        {
            "um", "uma", "uns", "umas", "o", "a", "os", "as", "dois", "duas", "três", "quatro", "cinco"
        };

        static readonly Regex Predicate = new Regex(
            @"\b(?:fica|ficam|é|são|foi|foram|está|estão)\s+(\p{L}+)", RegexOptions.IgnoreCase);

        public static char GenderOf(string text)
        {
            foreach (var raw in (text ?? "").ToLowerInvariant().Split(' '))
            {
                var w = raw.Trim(',', '.', '!', '?');
                if (w.Length == 0 || Determiners.Contains(w)) continue;
                if (Lexicon.TryGetValue(w, out char g)) return g;
                if (w.EndsWith("ista") || w.EndsWith("istas")) return '?';
                if (w.EndsWith("ção") || w.EndsWith("ções") || w.EndsWith("são") || w.EndsWith("gem") || w.EndsWith("dade")) return 'f';
                if (w.EndsWith("ão")) return 'm';
                if (w.EndsWith("a") || w.EndsWith("as")) return 'f';
                if (w.EndsWith("o") || w.EndsWith("os")) return 'm';
                return '?';
            }
            return '?';
        }

        public static bool Compatible(char a, char b) => a == '?' || b == '?' || a == b;

        public static string Mismatch(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return null;
            var m = Predicate.Match(phrase);
            if (!m.Success) return null;
            string word = m.Groups[1].Value;
            string lw = word.ToLowerInvariant();
            if (lw.Length < 4 || !Regex.IsMatch(lw, @"[dst](?:o|a|os|as)$")) return null;
            char predicate = lw.EndsWith("o") || lw.EndsWith("os") ? 'm' : 'f';

            string subject = phrase.Substring(0, m.Index).Trim();
            char g = GenderOf(subject);
            if (Compatible(g, predicate)) return null;

            string head = subject.Split(' ')[0];
            foreach (var w in subject.Split(' '))
                if (!Determiners.Contains(w.ToLowerInvariant())) { head = w; break; }
            return $"“{head}” não combina com “{word}”";
        }
    }

    public static class Synonyms
    {
        static Dictionary<string, List<string>> map;
        static List<string> termsByLength;
        static List<KeyValuePair<string, List<string>>> groups;

        public static string LastError { get; private set; }
        public static int GroupCount { get; private set; }

        static void Ensure()
        {
            if (map != null) return;
            map = new Dictionary<string, List<string>>();
            groups = new List<KeyValuePair<string, List<string>>>();
            GroupCount = 0;
            LastError = null;

            var asset = Resources.Load<TextAsset>("sinonimos");
            if (asset != null)
            {
                try
                {
                    var dict = JsonUtility.FromJson<DicionarioSinonimos>(asset.text);
                    if (dict != null && dict.grupos != null)
                    {
                        foreach (var g in dict.grupos)
                        {
                            if (g == null || g.termos == null) continue;
                            var terms = new List<string>();
                            foreach (var raw in g.termos)
                            {
                                var n = HeadlineValidator.Normalize(raw);
                                if (n.Length > 0 && !terms.Contains(n)) terms.Add(n);
                            }
                            if (terms.Count < 2) continue;
                            GroupCount++;
                            groups.Add(new KeyValuePair<string, List<string>>(HeadlineValidator.Normalize(g.nome), terms));
                            foreach (var term in terms)
                            {
                                if (!map.TryGetValue(term, out var list))
                                {
                                    list = new List<string> { term };
                                    map[term] = list;
                                }
                                foreach (var other in terms)
                                    if (!list.Contains(other)) list.Add(other);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    LastError = "Erro lendo sinonimos.json: " + e.Message;
                    Debug.LogError(LastError);
                }
            }

            termsByLength = new List<string>(map.Keys);
            termsByLength.Sort((x, y) => y.Length.CompareTo(x.Length));
        }

        public static IReadOnlyList<string> Of(string term)
        {
            Ensure();
            var n = HeadlineValidator.Normalize(term);
            return map.TryGetValue(n, out var list) ? list : new List<string> { n };
        }

        public static List<string> Alternatives(string term)
        {
            Ensure();
            var n = HeadlineValidator.Normalize(term);
            List<string> best = null;
            foreach (var g in groups)
            {
                if (!g.Value.Contains(n)) continue;
                if (g.Key == n) { best = g.Value; break; }
                if (best == null) best = g.Value;
            }
            var result = new List<string>();
            if (best != null)
                foreach (var t in best) if (t != n) result.Add(t);
            return result;
        }

        public static string FindTerm(string lowerText, out int index)
        {
            Ensure();
            for (int i = 0; i < lowerText.Length; i++)
            {
                if (!IsBoundary(lowerText, i - 1)) continue;
                foreach (var term in termsByLength)
                {
                    if (i + term.Length > lowerText.Length) continue;
                    if (string.CompareOrdinal(lowerText, i, term, 0, term.Length) == 0 && IsBoundary(lowerText, i + term.Length))
                    {
                        index = i;
                        return term;
                    }
                }
            }
            index = -1;
            return null;
        }

        public static string LiteralToRegex(string text)
        {
            Ensure();
            var sb = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                string hit = null;
                if (IsBoundary(text, i - 1))
                {
                    foreach (var term in termsByLength)
                    {
                        if (i + term.Length > text.Length) continue;
                        if (string.CompareOrdinal(text, i, term, 0, term.Length) == 0 && IsBoundary(text, i + term.Length))
                        {
                            hit = term;
                            break;
                        }
                    }
                }

                if (hit != null)
                {
                    var alts = map[hit];
                    sb.Append("(?:");
                    for (int k = 0; k < alts.Count; k++)
                    {
                        if (k > 0) sb.Append('|');
                        sb.Append(Regex.Escape(alts[k]));
                    }
                    sb.Append(')');
                    i += hit.Length;
                }
                else
                {
                    sb.Append(Regex.Escape(text[i].ToString()));
                    i++;
                }
            }
            return sb.ToString();
        }

        static bool IsBoundary(string s, int index) =>
            index < 0 || index >= s.Length || !char.IsLetterOrDigit(s[index]);
    }

    public static class AiredHistory
    {
        const string Key = "jornalista_frases_no_ar";
        static Dictionary<string, int> aired;

        static Dictionary<string, int> Data
        {
            get
            {
                if (aired != null) return aired;
                aired = new Dictionary<string, int>();
                var raw = PlayerPrefs.GetString(Key, "");
                foreach (var line in raw.Split('\n'))
                {
                    int tab = line.IndexOf('\t');
                    if (tab <= 0) continue;
                    if (int.TryParse(line.Substring(0, tab), out int day))
                        aired[line.Substring(tab + 1)] = day;
                }
                return aired;
            }
        }

        public static int AiredOnDay(string frase)
        {
            return Data.TryGetValue(HeadlineValidator.Normalize(frase), out int d) ? d : 0;
        }

        public static void Add(string frase, int day)
        {
            Data[HeadlineValidator.Normalize(frase)] = day;
            Save();
        }

        public static void Clear()
        {
            Data.Clear();
            Save();
        }

        static void Save()
        {
            var sb = new StringBuilder();
            foreach (var kv in Data) sb.Append(kv.Value).Append('\t').Append(kv.Key).Append('\n');
            PlayerPrefs.SetString(Key, sb.ToString());
            PlayerPrefs.Save();
        }
    }
}
