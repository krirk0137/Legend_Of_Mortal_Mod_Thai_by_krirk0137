using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;

namespace LomThaiText
{
    /// <summary>
    /// Every label showing Thai that <c>UI.resizer.txt</c> has NO rule for, written to
    /// <c>BepInEx/LomThaiText_unruled.tsv</c> as you play.
    ///
    /// This exists because the alternative is guesswork: "screen X looks small" → find the path by
    /// eye → guess a rule → restart → look again. The report gives the exact scene path, the box,
    /// and the size range the game is choosing between, so a rule can be written once and verified
    /// with Ctrl+F10. It is also the tuning worklist for the parts of the game nobody has walked
    /// through yet.
    /// </summary>
    internal static class Unruled
    {
        private static readonly Dictionary<string, string> _rows = new Dictionary<string, string>(StringComparer.Ordinal);
        private static bool _dirty;
        private static float _nextWrite;

        internal static void Note(Text t, string path)
        {
            if (t == null || _rows.ContainsKey(path)) return;
            try
            {
                var box = t.rectTransform.rect;
                var s = t.text ?? "";
                if (s.Length > 40) s = s.Substring(0, 40);
                _rows[path] = string.Format("{0}x{1}\t{2}\t{3}\t{4}..{5}\t{6}",
                    (int)box.width, (int)box.height, t.fontSize,
                    t.resizeTextForBestFit ? "bestFit" : "fixed",
                    t.resizeTextMinSize, t.resizeTextMaxSize,
                    s.Replace("\t", " ").Replace("\n", "\\n"));
                _dirty = true;
            }
            catch { }
        }

        internal static void FlushIfDue()
        {
            if (!_dirty || Time.unscaledTime < _nextWrite) return;
            _nextWrite = Time.unscaledTime + 5f;
            _dirty = false;
            try
            {
                var path = Path.Combine(Paths.BepInExRootPath, "LomThaiText_unruled.tsv");
                using (var w = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    w.NewLine = "\n";
                    w.WriteLine("# Thai labels with NO rule in UI.resizer.txt — the sizing worklist.");
                    w.WriteLine("# Add a line to UI.resizer.txt, then press Ctrl+F10 in game to see it.");
                    w.WriteLine("#   /scene/path=AutoResize(true,<min>,<max>)");
                    w.WriteLine("# path\tbox\tfontSize\tmode\tmin..max\ttext");
                    foreach (var kv in _rows) w.WriteLine(kv.Key + "\t" + kv.Value);
                }
            }
            catch { /* Program Files may be unwritable; the log still carries the important cases */ }
        }
    }
}
