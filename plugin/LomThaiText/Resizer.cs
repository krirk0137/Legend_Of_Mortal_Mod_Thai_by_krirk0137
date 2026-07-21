using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LomThaiText
{
    /// <summary>
    /// Applies the project's own <c>UI.resizer.txt</c> — 1,085 hand-tuned lines built up over
    /// v1.0–v1.6 — to the components the plugin feeds.
    ///
    /// Why this has to exist: XUnity applies those rules only to components IT translates. Once a
    /// string arrives already in Thai, XUnity never touches the component, so the rules never fire
    /// and the game's own <c>bestFit</c> (min size 0) shrinks long Thai to microscopic. Reusing the
    /// tuned file is far better than re-deriving the sizing from scratch.
    ///
    /// Format: <c>/Scene/Path=Directive(args)</c>. A rule applies to the whole subtree beneath its
    /// path, and several rules may target the same path; they are applied in file order.
    /// </summary>
    internal static class Resizer
    {
        private struct Rule
        {
            public string Path;
            public string Op;
            public string[] Args;
        }

        private static readonly List<Rule> _rules = new List<Rule>();
        private static readonly HashSet<int> _done = new HashSet<int>();
        private static int _log = 8;
        internal static bool Loaded;

        internal static void Load(string path)
        {
            Loaded = true;
            _rules.Clear();
            _done.Clear();
            _log = 8;
            if (!File.Exists(path))
            {
                Plugin.Log.LogWarning("No UI.resizer.txt at " + path + " — labels keep the game's own sizing.");
                return;
            }
            int bad = 0;
            foreach (var raw in File.ReadAllLines(path, new UTF8Encoding(false)))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;                       // a bare path with no directive
                var p = line.Substring(0, eq);
                var call = line.Substring(eq + 1);
                int open = call.IndexOf('(');
                int close = call.LastIndexOf(')');
                if (open <= 0 || close <= open) { bad++; continue; }
                var op = call.Substring(0, open).Trim();
                var args = call.Substring(open + 1, close - open - 1).Split(',');
                for (int i = 0; i < args.Length; i++) args[i] = args[i].Trim();
                _rules.Add(new Rule { Path = p, Op = op, Args = args });
            }
            Plugin.Log.LogInfo("Loaded " + _rules.Count + " UI resizer rules"
                               + (bad > 0 ? " (" + bad + " unparsable)" : "") + ".");
        }

        /// <summary>Returns true if any rule matched, so the generic fitter can stand down.</summary>
        internal static bool Apply(Text t, string path)
        {
            if (_rules.Count == 0 || t == null) return false;
            int id = t.GetInstanceID();
            if (_done.Contains(id)) return true;

            bool matched = false;
            for (int i = 0; i < _rules.Count; i++)
            {
                var r = _rules[i];
                if (!path.StartsWith(r.Path, StringComparison.Ordinal)) continue;
                // a rule on /A/B must not match /A/Bcd
                if (path.Length > r.Path.Length && path[r.Path.Length] != '/') continue;
                matched = true;
                try { Run(t, r); }
                catch (Exception e) { Plugin.Log.LogWarning("[resize] " + r.Op + " failed: " + e.Message); }
            }
            if (!matched) return false;

            _done.Add(id);
            t.SetAllDirty();
            if (_log-- > 0)
                Plugin.Log.LogInfo(string.Format("[resize] {0} -> bestFit={1} {2}..{3} hOver={4}",
                    path, t.resizeTextForBestFit, t.resizeTextMinSize, t.resizeTextMaxSize, t.horizontalOverflow));
            return true;
        }

        /// <summary>Same rules, TextMeshPro components. These were invisible to the resizer until
        /// now, which is why no AutoResize value could move the biography panel — it is TMP, and
        /// so are the combat HUD and several headers.</summary>
        internal static bool Apply(TMPro.TMP_Text t, string path)
        {
            if (_rules.Count == 0 || t == null) return false;
            int id = t.GetInstanceID();
            if (_done.Contains(id)) return true;

            bool matched = false;
            for (int i = 0; i < _rules.Count; i++)
            {
                var r = _rules[i];
                if (!path.StartsWith(r.Path, StringComparison.Ordinal)) continue;
                if (path.Length > r.Path.Length && path[r.Path.Length] != '/') continue;
                matched = true;
                try { RunTmp(t, r); }
                catch (Exception e) { Plugin.Log.LogWarning("[resize/tmp] " + r.Op + " failed: " + e.Message); }
            }
            if (!matched) return false;

            _done.Add(id);
            try { t.SetAllDirty(); } catch { }
            if (_logTmp-- > 0)
                Plugin.Log.LogInfo(string.Format("[resize/tmp] {0} -> auto={1} {2}..{3}",
                    path, t.enableAutoSizing, t.fontSizeMin, t.fontSizeMax));
            return true;
        }

        private static int _logTmp = 8;

        private static void RunTmp(TMPro.TMP_Text t, Rule r)
        {
            switch (r.Op)
            {
                case "AutoResize":
                    t.enableAutoSizing = Bool(r.Args, 0, true);
                    if (r.Args.Length > 1) t.fontSizeMin = Float(r.Args, 1, t.fontSizeMin);
                    if (r.Args.Length > 2) t.fontSizeMax = Float(r.Args, 2, t.fontSizeMax);
                    break;
                case "ChangeFontSize":
                    t.fontSize = Float(r.Args, 0, t.fontSize);
                    break;
                case "ChangeFontSizeByPercentage":
                    t.fontSize = Mathf.Max(1f, t.fontSize * Float(r.Args, 0, 1f));
                    break;
                case "UGUI_ChangeLineSpacing":
                    t.lineSpacing = Float(r.Args, 0, t.lineSpacing);
                    break;
                case "UGUI_HorizontalOverflow":
                    t.enableWordWrapping = !Is(r.Args, "overflow");
                    break;
                case "UGUI_VerticalOverflow":
                case "TMP_Overflow":
                    t.overflowMode = Enum<TMPro.TextOverflowModes>(r.Args, t.overflowMode);
                    break;
                case "TMP_Alignment":
                    t.alignment = Enum<TMPro.TextAlignmentOptions>(r.Args, t.alignment);
                    break;
            }
        }

        private static T Enum<T>(string[] a, T dflt) where T : struct
        {
            if (a.Length == 0) return dflt;
            try { return (T)System.Enum.Parse(typeof(T), a[0], true); }
            catch { return dflt; }
        }

        private static void Run(Text t, Rule r)
        {
            switch (r.Op)
            {
                case "AutoResize":
                    t.resizeTextForBestFit = Bool(r.Args, 0, true);
                    if (r.Args.Length > 1) t.resizeTextMinSize = Int(r.Args, 1, t.resizeTextMinSize);
                    if (r.Args.Length > 2) t.resizeTextMaxSize = Int(r.Args, 2, t.resizeTextMaxSize);
                    break;
                case "UGUI_HorizontalOverflow":
                    t.horizontalOverflow = Is(r.Args, "overflow") ? HorizontalWrapMode.Overflow : HorizontalWrapMode.Wrap;
                    break;
                case "UGUI_VerticalOverflow":
                    t.verticalOverflow = Is(r.Args, "overflow") ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
                    break;
                case "UGUI_ChangeLineSpacing":
                    t.lineSpacing = Float(r.Args, 0, t.lineSpacing);
                    break;
                case "ChangeFontSize":
                    t.fontSize = Int(r.Args, 0, t.fontSize);
                    break;
                case "ChangeFontSizeByPercentage":
                    t.fontSize = Mathf.Max(1, Mathf.RoundToInt(t.fontSize * Float(r.Args, 0, 1f)));
                    break;
                // TMP_* directives are left to XUnity; only 6 lines use them and they target
                // TextMeshPro components, which this pass does not handle.
            }
        }

        private static bool Is(string[] a, string want)
        {
            return a.Length > 0 && a[0].Equals(want, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Bool(string[] a, int i, bool dflt)
        {
            bool v;
            return i < a.Length && bool.TryParse(a[i], out v) ? v : dflt;
        }

        private static int Int(string[] a, int i, int dflt)
        {
            int v;
            return i < a.Length && int.TryParse(a[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : dflt;
        }

        private static float Float(string[] a, int i, float dflt)
        {
            float v;
            return i < a.Length && float.TryParse(a[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : dflt;
        }

        /// <summary>Full scene path with a leading slash, the form UI.resizer.txt uses.</summary>
        internal static string PathOf(Transform t)
        {
            var sb = new StringBuilder(64);
            int guard = 0;
            while (t != null && guard++ < 24)
            {
                sb.Insert(0, t.name).Insert(0, '/');
                t = t.parent;
            }
            return sb.ToString();
        }
    }
}
