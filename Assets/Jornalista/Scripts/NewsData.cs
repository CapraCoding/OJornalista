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
        public string[] chamadasGenericas;
        public Dia[] dias;
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

    public static class Synonyms
    {
        static Dictionary<string, List<string>> map;
        static List<string> termsByLength;

        public static string LastError { get; private set; }
        public static int GroupCount { get; private set; }

        static void Ensure()
        {
            if (map != null) return;
            map = new Dictionary<string, List<string>>();
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
