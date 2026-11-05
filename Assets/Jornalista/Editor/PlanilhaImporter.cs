using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Jornalista.EditorTools
{
    public static class PlanilhaImporter
    {
        const string ModelosPath = "Assets/Planilhas/modelos.xlsx";
        const string VocabularioPath = "Assets/Planilhas/vocabulario.xlsx";
        const string NoticiasJson = "Assets/Resources/noticias.json";
        const string SinonimosJson = "Assets/Resources/sinonimos.json";

        static readonly string[] ManualKeys =
        {
            "padrão de escrita", "cuidados", "ordem do resumo", "pontuação e leitura", "veículos"
        };

        [MenuItem("Jornalista/Importar planilhas")]
        public static void Import()
        {
            try
            {
                if (!File.Exists(ModelosPath) || !File.Exists(VocabularioPath))
                {
                    EditorUtility.DisplayDialog("Jornalista",
                        $"Coloque as planilhas em:\n{ModelosPath}\n{VocabularioPath}", "OK");
                    return;
                }

                var modelosBook = XlsxReader.Read(ModelosPath);
                var vocabBook = XlsxReader.Read(VocabularioPath);

                var banco = LoadExisting();
                banco.modelos = ReadModelos(First(modelosBook));
                banco.termos = ReadLists(Sheet(vocabBook, "termos"), ';');
                banco.valores = ReadLists(Sheet(vocabBook, "valores"), ';');
                banco.manual = PickManual(banco.termos);

                var sinonimos = new DicionarioSinonimos();
                var groups = new List<GrupoSinonimos>();
                foreach (var l in ReadLists(Sheet(vocabBook, "sin"), ','))
                    groups.Add(new GrupoSinonimos { nome = l.chave, termos = l.itens });
                sinonimos.grupos = groups.ToArray();

                var utf8 = new UTF8Encoding(false);
                File.WriteAllText(NoticiasJson, JsonUtility.ToJson(banco, true), utf8);
                File.WriteAllText(SinonimosJson, JsonUtility.ToJson(sinonimos, true), utf8);
                AssetDatabase.Refresh();

                string msg = $"{banco.modelos.Length} modelos, {banco.valores.Length} listas de valores, " +
                             $"{banco.termos.Length} termos, {sinonimos.grupos.Length} grupos de sinônimos.";
                Debug.Log("[Jornalista] Planilhas importadas: " + msg);
                EditorUtility.DisplayDialog("Jornalista", "Planilhas importadas!\n\n" + msg, "OK");
            }
            catch (IOException e)
            {
                EditorUtility.DisplayDialog("Jornalista", "Não consegui ler a planilha. Ela está aberta no Excel?\n\n" + e.Message, "OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Jornalista", "Erro ao importar: " + e.Message, "OK");
            }
        }

        static BancoDeNoticias LoadExisting()
        {
            if (File.Exists(NoticiasJson))
            {
                try
                {
                    var b = JsonUtility.FromJson<BancoDeNoticias>(File.ReadAllText(NoticiasJson));
                    if (b != null) return b;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Jornalista] noticias.json atual inválido, criando um novo: " + e.Message);
                }
            }
            return new BancoDeNoticias { manchetesPorDia = 1 };
        }

        static List<List<string>> First(Dictionary<string, List<List<string>>> book)
        {
            foreach (var kv in book) return kv.Value;
            return new List<List<string>>();
        }

        static List<List<string>> Sheet(Dictionary<string, List<List<string>>> book, string nameContains)
        {
            foreach (var kv in book)
                if (kv.Key.ToLowerInvariant().Contains(nameContains)) return kv.Value;
            return new List<List<string>>();
        }

        static Modelo[] ReadModelos(List<List<string>> rows)
        {
            var result = new List<Modelo>();
            if (rows.Count < 2) return result.ToArray();

            var header = rows[0];
            int Col(string name)
            {
                for (int i = 0; i < header.Count; i++)
                    if (string.Equals(header[i].Trim(), name, StringComparison.OrdinalIgnoreCase)) return i;
                return -1;
            }
            int cTitulo = Col("titulo"), cTexto = Col("texto"), cLocal = Col("local");
            int cFrases = Col("frasesValidas"), cChamadas = Col("chamadasSensacionalistas");

            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                string Get(int c) => c >= 0 && c < row.Count ? (row[c] ?? "").Trim() : "";
                if (Get(cTitulo).Length == 0 && Get(cTexto).Length == 0) continue;
                result.Add(new Modelo
                {
                    titulo = Get(cTitulo),
                    texto = Get(cTexto),
                    local = Get(cLocal),
                    frasesValidas = SplitList(Get(cFrases), ';'),
                    chamadasSensacionalistas = SplitList(Get(cChamadas), ';')
                });
            }
            return result.ToArray();
        }

        static Lista[] ReadLists(List<List<string>> rows, char separator)
        {
            var result = new List<Lista>();
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count < 2 || string.IsNullOrWhiteSpace(row[0])) continue;
                result.Add(new Lista { chave = row[0].Trim(), itens = SplitList(row[1], separator) });
            }
            return result.ToArray();
        }

        static Lista[] PickManual(Lista[] termos)
        {
            var result = new List<Lista>();
            foreach (var key in ManualKeys)
                foreach (var t in termos)
                    if (t.chave.Trim().ToLowerInvariant() == key) result.Add(t);
            return result.ToArray();
        }

        static string[] SplitList(string text, char separator)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(text)) return list.ToArray();
            foreach (var part in text.Split(separator))
                if (part.Trim().Length > 0) list.Add(part.Trim());
            return list.ToArray();
        }
    }

    public static class XlsxReader
    {
        const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public static Dictionary<string, List<List<string>>> Read(string path)
        {
            var book = new Dictionary<string, List<List<string>>>();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                var shared = new List<string>();
                var ssEntry = zip.GetEntry("xl/sharedStrings.xml");
                if (ssEntry != null)
                    foreach (XmlNode si in Load(ssEntry).GetElementsByTagName("si"))
                        shared.Add(TextOf(si));

                var rels = new Dictionary<string, string>();
                foreach (XmlElement rel in Load(zip.GetEntry("xl/_rels/workbook.xml.rels")).GetElementsByTagName("Relationship"))
                    rels[rel.GetAttribute("Id")] = rel.GetAttribute("Target");

                foreach (XmlElement sheet in Load(zip.GetEntry("xl/workbook.xml")).GetElementsByTagName("sheet"))
                {
                    string name = sheet.GetAttribute("name");
                    string rid = sheet.GetAttribute("id", RelNs);
                    if (!rels.TryGetValue(rid, out var target)) continue;
                    string entryPath = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
                    var entry = zip.GetEntry(entryPath);
                    if (entry != null) book[name] = ReadSheet(Load(entry), shared);
                }
            }
            return book;
        }

        static List<List<string>> ReadSheet(XmlDocument doc, List<string> shared)
        {
            var rows = new List<List<string>>();
            foreach (XmlElement row in doc.GetElementsByTagName("row"))
            {
                var cells = new List<string>();
                int next = 0;
                foreach (XmlNode node in row.ChildNodes)
                {
                    if (!(node is XmlElement c) || c.LocalName != "c") continue;
                    int col = ColumnIndex(c.GetAttribute("r"), next);
                    while (cells.Count < col) cells.Add("");
                    cells.Add(CellValue(c, shared));
                    next = col + 1;
                }
                rows.Add(cells);
            }
            return rows;
        }

        static string CellValue(XmlElement c, List<string> shared)
        {
            string type = c.GetAttribute("t");
            if (type == "inlineStr") return TextOf(c);
            string v = "";
            foreach (XmlNode n in c.ChildNodes)
                if (n.LocalName == "v") v = n.InnerText;
            if (type == "s" && int.TryParse(v, out int idx) && idx >= 0 && idx < shared.Count) return shared[idx];
            return v;
        }

        static int ColumnIndex(string reference, int fallback)
        {
            if (string.IsNullOrEmpty(reference)) return fallback;
            int col = 0;
            foreach (char ch in reference)
            {
                if (ch < 'A' || ch > 'Z') break;
                col = col * 26 + (ch - 'A' + 1);
            }
            return col > 0 ? col - 1 : fallback;
        }

        static string TextOf(XmlNode node)
        {
            var sb = new StringBuilder();
            foreach (XmlNode t in ((XmlElement)node).GetElementsByTagName("*"))
                if (t.LocalName == "t" && (t.ParentNode == null || t.ParentNode.LocalName != "rPh")) sb.Append(t.InnerText);
            return sb.ToString();
        }

        static XmlDocument Load(ZipArchiveEntry entry)
        {
            var doc = new XmlDocument();
            using (var s = entry.Open()) doc.Load(s);
            return doc;
        }
    }
}
