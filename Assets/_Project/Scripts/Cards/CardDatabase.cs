using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PFN
{
    /// <summary>
    /// Loads cards from CSV text. Expected format, one card per line:
    ///   word,phrase
    /// A first line starting with "word" is treated as a header. Fields may be
    /// wrapped in double quotes (use "" for a literal quote). Blank lines and
    /// lines starting with # are ignored.
    /// </summary>
    public static class CardDatabase
    {
        public static List<Card> Parse(string csv)
        {
            var cards = new List<Card>();
            if (string.IsNullOrEmpty(csv)) return cards;

            var lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (i == 0 && line.ToLowerInvariant().StartsWith("word")) continue;

                var fields = SplitCsvLine(line);
                if (fields.Count < 2 || fields[0].Length == 0 || fields[1].Length == 0)
                {
                    Debug.LogWarning($"[PFN] Skipping malformed card on line {i + 1}: {line}");
                    continue;
                }
                cards.Add(new Card(fields[0], fields[1]));
            }
            return cards;
        }

        static List<string> SplitCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (c == '"') inQuotes = false;
                    else sb.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(sb.ToString().Trim()); sb.Clear(); }
                else sb.Append(c);
            }
            fields.Add(sb.ToString().Trim());
            return fields;
        }
    }
}
