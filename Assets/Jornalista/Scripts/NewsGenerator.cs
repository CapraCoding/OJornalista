using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Jornalista
{
    public static class NewsGenerator
    {
        static readonly Regex Placeholder = new Regex(@"\[([^\]]+)\]");
        static readonly Regex Instruction = new Regex(
            @",?\s*(?:se informad[oa]s?|se houver|se necessári[oa]|quando necessári[oa])",
            RegexOptions.IgnoreCase);

        public static List<Noticia> ForDay(BancoDeNoticias banco, int day)
        {
            var list = new List<Noticia>();
            var models = banco.modelos;
            if (models == null || models.Length == 0) return list;

            int n = models.Length;
            int per = Mathf.Max(1, banco.manchetesPorDia);
            var used = new HashSet<int>();
            int offset = 0;
            for (int k = 0; k < per && used.Count < n; k++)
            {
                int index;
                do
                {
                    int slot = (day - 1) * per + k + offset;
                    var order = Permutation(n, 7919 * (slot / n + 1));
                    index = order[slot % n];
                    if (used.Contains(index)) offset++;
                }
                while (used.Contains(index));
                used.Add(index);
                var rng = new System.Random(day * 1009 + k * 31 + 17);
                list.Add(Generate(banco, models[index], index, rng));
            }
            return list;
        }

        public static int CycleDays(BancoDeNoticias banco)
        {
            if (banco.modelos == null || banco.modelos.Length == 0) return 0;
            int per = Mathf.Max(1, banco.manchetesPorDia);
            return Mathf.CeilToInt(banco.modelos.Length / (float)per);
        }

        static Noticia Generate(BancoDeNoticias banco, Modelo m, int modelIndex, System.Random rng)
        {
            var fill = new Filler(banco, rng);
            var noticia = new Noticia
            {
                titulo = fill.Fill(m.titulo),
                texto = ToLines(fill.Fill(m.texto)),
                local = fill.Fill(m.local).Trim().TrimEnd('.'),
                chamadasSensacionalistas = FillAll(fill, m.chamadasSensacionalistas)
            };

            var templates = Clean(m.frasesValidas);
            if (templates.Count == 0)
            {
                noticia.frasesValidas = new string[0];
                noticia.campos = new Campo[0];
                return noticia;
            }

            var valid = new string[templates.Count];
            for (int i = 0; i < templates.Count; i++)
            {
                var blocks = Blocks.Split(templates[i]);
                for (int b = 0; b < blocks.Count; b++) blocks[b] = fill.Fill(blocks[b]);
                valid[i] = Blocks.Join(blocks);
            }
            noticia.frasesValidas = valid;

            var target = Blocks.Split(templates[rng.Next(templates.Count)]);
            noticia.campos = BuildFields(banco, modelIndex, target, fill, rng);
            return noticia;
        }

        static Campo[] BuildFields(BancoDeNoticias banco, int modelIndex, List<string> templateBlocks, Filler fill, System.Random rng)
        {
            int count = templateBlocks.Count;
            int max = Mathf.Max(2, banco.opcoesPorCampo);
            var campos = new Campo[count];

            for (int b = 0; b < count; b++)
            {
                string correct = fill.Fill(templateBlocks[b]);
                var options = new List<string> { correct };
                var seen = new HashSet<string> { HeadlineValidator.Normalize(correct) };

                var synonym = SynonymVariant(correct, rng);
                if (synonym != null && options.Count < max && seen.Add(HeadlineValidator.Normalize(synonym)))
                    options.Add(synonym);

                if (options.Count < max && templateBlocks[b].IndexOf('[') >= 0)
                {
                    for (int tries = 0; tries < 6; tries++)
                    {
                        var other = new Filler(banco, rng).Fill(templateBlocks[b]);
                        if (seen.Add(HeadlineValidator.Normalize(other)))
                        {
                            options.Add(other);
                            break;
                        }
                    }
                }

                var pool = PoolFor(banco, modelIndex, b, count);
                Shuffle(pool, rng);
                foreach (var tpl in pool)
                {
                    if (options.Count >= max) break;
                    var o = fill.Fill(tpl);
                    if (o.Length > 0 && seen.Add(HeadlineValidator.Normalize(o))) options.Add(o);
                }

                campos[b] = new Campo { rotulo = Label(b, count), opcoes = options.ToArray() };
            }
            return campos;
        }

        static List<string> PoolFor(BancoDeNoticias banco, int modelIndex, int position, int count)
        {
            var same = new List<string>();
            var any = new List<string>();
            for (int j = 0; j < banco.modelos.Length; j++)
            {
                if (j == modelIndex || banco.modelos[j] == null) continue;
                foreach (var t in Clean(banco.modelos[j].frasesValidas))
                {
                    var blocks = Blocks.Split(t);
                    if (blocks.Count == count) same.Add(blocks[position]);
                    if (position < blocks.Count) any.Add(blocks[position]);
                }
            }
            return same.Count > 0 ? same : any;
        }

        static string Label(int index, int count)
        {
            if (count == 3) return new[] { "Quem / O quê", "Ação", "Complemento" }[index];
            if (count == 2) return new[] { "Quem / O quê", "Ação" }[index];
            return $"Bloco {index + 1}";
        }

        static string SynonymVariant(string text, System.Random rng)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string lower = text.ToLowerInvariant();
            string term = Synonyms.FindTerm(lower, out int index);
            if (term == null) return null;
            char gender = Agreement.GenderOf(term);
            var alts = Synonyms.Alternatives(term).FindAll(t => Agreement.Compatible(gender, Agreement.GenderOf(t)));
            if (alts.Count == 0) return null;
            string pick = alts[rng.Next(alts.Count)];
            if (index == 0 && char.IsUpper(text[0]))
                pick = char.ToUpperInvariant(pick[0]) + pick.Substring(1);
            var result = text.Substring(0, index) + pick + text.Substring(index + term.Length);
            return Agreement.Mismatch(result) == null ? result : null;
        }

        static string ToLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var sb = new StringBuilder();
            foreach (var paragraph in text.Replace("\\n", "\n").Split('\n'))
            {
                foreach (var sentence in Regex.Split(paragraph, @"(?<=[.!?])\s+"))
                {
                    var line = sentence.Trim();
                    if (line.Length == 0) continue;
                    if (!line.EndsWith("...")) line = line.TrimEnd('.') + "...";
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(line);
                }
            }
            return sb.ToString();
        }

        static string[] FillAll(Filler fill, string[] items)
        {
            var list = Clean(items);
            for (int i = 0; i < list.Count; i++) list[i] = fill.Fill(list[i]);
            return list.ToArray();
        }

        static List<string> Clean(string[] items)
        {
            var list = new List<string>();
            if (items == null) return list;
            foreach (var s in items)
                if (!string.IsNullOrWhiteSpace(s)) list.Add(s.Trim());
            return list;
        }

        static int[] Permutation(int n, int seed)
        {
            var arr = new int[n];
            for (int i = 0; i < n; i++) arr[i] = i;
            var r = new System.Random(seed);
            for (int i = n - 1; i > 0; i--)
            {
                int j = r.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr;
        }

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        static string Key(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = Instruction.Replace(s, "");
            return Regex.Replace(s.Trim().ToLowerInvariant(), @"\s+", " ");
        }

        class Filler
        {
            readonly BancoDeNoticias banco;
            readonly System.Random rng;
            readonly Dictionary<string, string> bindings = new Dictionary<string, string>();

            public Filler(BancoDeNoticias banco, System.Random rng)
            {
                this.banco = banco;
                this.rng = rng;
            }

            public string Fill(string s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                s = Instruction.Replace(s, "");
                s = Placeholder.Replace(s, m => Resolve(m.Groups[1].Value));
                s = Regex.Replace(s, @"\s{2,}", " ");
                s = Regex.Replace(s, @"\s+([,.])", "$1");
                return s.Trim();
            }

            string Resolve(string raw)
            {
                raw = raw.Trim();
                string key = Key(raw);
                if (!bindings.TryGetValue(key, out var value))
                {
                    value = Pick(key, raw);
                    bindings[key] = value;
                }
                if (raw.Length > 0 && value.Length > 0 && char.IsUpper(raw[0]))
                    value = char.ToUpperInvariant(value[0]) + value.Substring(1);
                return value;
            }

            string Pick(string key, string raw)
            {
                var list = Lookup(key);
                if (list.Count > 0) return Deref(list[rng.Next(list.Count)], 0);

                if (raw.IndexOf('/') >= 0)
                {
                    var parts = Instruction.Replace(raw, "").Split('/');
                    return parts[rng.Next(parts.Length)].Trim();
                }

                Debug.LogWarning($"[Jornalista] Sem valores para [{raw}]. Adicione na aba Valores da planilha de vocabulário.");
                return raw;
            }

            string Deref(string value, int depth)
            {
                value = value.Trim();
                if (!value.StartsWith("@") || depth > 3) return value;
                var list = Lookup(Key(value.Substring(1)));
                return list.Count > 0 ? Deref(list[rng.Next(list.Count)], depth + 1) : value.Substring(1);
            }

            List<string> Lookup(string key)
            {
                var found = Find(banco.valores, key);
                if (found.Count == 0) found = Find(banco.termos, key);
                return found;
            }

            static List<string> Find(Lista[] lists, string key)
            {
                var result = new List<string>();
                if (lists == null) return result;
                foreach (var l in lists)
                {
                    if (l == null || Key(l.chave) != key || l.itens == null) continue;
                    foreach (var item in l.itens)
                        if (!string.IsNullOrWhiteSpace(item)) result.Add(item.Trim());
                }
                return result;
            }
        }
    }

    public static class Blocks
    {
        static readonly HashSet<string> Verbs = new HashSet<string>
        {
            "é", "são", "fica", "ficam", "morre", "morrem", "foge", "fogem", "deixa", "deixam",
            "flagra", "flagram", "registra", "registram", "aborda", "abordam", "apreende", "apreendem",
            "cumpre", "cumprem", "termina", "terminam", "envolve", "envolvem", "capota", "capotam",
            "sai", "saem", "se", "mobiliza", "mobilizam", "revela", "revelam", "encontra", "encontram",
            "precisa", "precisam", "causa", "causam", "não", "atinge", "atingem", "bate", "batem",
            "cai", "caem", "tomba", "tombam", "recebe", "recebem"
        };

        static readonly HashSet<string> TwoWord = new HashSet<string> { "é", "são", "se", "não", "fica", "ficam" };

        public static List<string> Split(string phrase)
        {
            phrase = (phrase ?? "").Trim();
            var parts = SplitTopLevel(phrase, '|');
            if (parts.Count > 1)
            {
                var clean = new List<string>();
                foreach (var p in parts) if (p.Trim().Length > 0) clean.Add(p.Trim());
                return clean;
            }
            return Heuristic(phrase);
        }

        public static string Join(List<string> blocks)
        {
            return Regex.Replace(string.Join(" ", blocks), @"\s+", " ").Trim();
        }

        static List<string> Heuristic(string s)
        {
            var words = SplitTopLevel(s, ' ');
            words.RemoveAll(w => w.Length == 0);
            if (words.Count < 3) return new List<string> { s };

            int v = -1;
            for (int i = 1; i < words.Count; i++)
                if (Verbs.Contains(words[i].ToLowerInvariant())) { v = i; break; }
            if (v < 0) v = 1;

            int end = v + 1;
            if (TwoWord.Contains(words[v].ToLowerInvariant()) && end < words.Count - 1) end++;

            var result = new List<string>
            {
                string.Join(" ", words.GetRange(0, v)),
                string.Join(" ", words.GetRange(v, end - v))
            };
            if (end < words.Count) result.Add(string.Join(" ", words.GetRange(end, words.Count - end)));
            return result;
        }

        static List<string> SplitTopLevel(string s, char separator)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            int depth = 0;
            foreach (char c in s)
            {
                if (c == '{' || c == '[') depth++;
                else if ((c == '}' || c == ']') && depth > 0) depth--;
                if (c == separator && depth == 0)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(c);
            }
            result.Add(sb.ToString());
            return result;
        }
    }
}
