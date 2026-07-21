using System;
using System.Reflection;
using System.Text;
using Lean.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LomThaiText
{
    /// <summary>
    /// Forensic pass. Answers, without guessing:
    ///   1. does the Thai survive all the way into LeanLocalization's table?
    ///   2. which component draws our injected text, with what font AND what material?
    ///   3. what does a component that ALREADY renders Thai correctly look like?
    /// (2) vs (3) is the whole game: UI/Layer_1/CommunityLink/Text renders Thai fine,
    /// UI/Layer_1/Buttons/StartGame/Text does not — the difference between them is the fix.
    /// </summary>
    internal static class Probe
    {
        internal const string MARKER = "[S2]";

        internal static void Run(string tag)
        {
            var L = Plugin.Log;
            try
            {
                L.LogInfo("[probe:" + tag + "] ---- begin ----");

                foreach (var key in new[] { "System/Start/StartGame" })
                {
                    string v;
                    try { v = LeanLocalization.GetTranslationText(key); } catch (Exception e) { v = "<threw " + e.GetType().Name + ">"; }
                    L.LogInfo("[probe] table[" + key + "] = " + Show(v));
                }

                var texts = Resources.FindObjectsOfTypeAll<Text>();
                var tmps = Resources.FindObjectsOfTypeAll<TMP_Text>();
                L.LogInfo(string.Format("[probe] UGUI Text={0}  TMP_Text={1}", texts.Length, tmps.Length));

                // PASS A — everything living under the title menu and the one control that
                // already renders Thai correctly, whether or not it carries text. This is the
                // comparison that matters; it must not compete for budget with anything else.
                int a = 0;
                foreach (var c in Resources.FindObjectsOfTypeAll<Component>())
                {
                    if (c == null) continue;
                    string path;
                    try { path = Path(c); } catch { continue; }
                    if (path.IndexOf("Layer_1/Buttons", StringComparison.Ordinal) < 0
                        && path.IndexOf("CommunityLink", StringComparison.Ordinal) < 0) continue;
                    if (++a > 60) break;

                    string s = null;
                    var tp = Fonts.TextPropOf(c.GetType());
                    if (tp != null) { try { s = tp.GetValue(c, null) as string; } catch { } }
                    L.LogInfo("[probe] TITLE " + (s == null ? c.GetType().Name + " '" + path + "'" : Describe(c, s)));
                }
                if (a == 0) L.LogWarning("[probe] found nothing under Layer_1/Buttons — title scene not loaded?");

                // PASS B — anything else holding Thai or the marker, deduped so 20 identical
                // save slots cannot eat the budget.
                int shown = 0;
                var seen = new System.Collections.Generic.HashSet<string>();
                foreach (var c in Resources.FindObjectsOfTypeAll<Component>())
                {
                    if (c == null) continue;
                    var tp = Fonts.TextPropOf(c.GetType());
                    if (tp == null) continue;
                    string s;
                    try { s = tp.GetValue(c, null) as string; } catch { continue; }
                    if (string.IsNullOrEmpty(s)) continue;
                    bool marker = s.IndexOf(MARKER, StringComparison.Ordinal) >= 0;
                    if (!marker && !Fonts.HasThai(s)) continue;
                    string path;
                    try { path = Path(c); } catch { path = "?"; }
                    if (!seen.Add(Collapse(path))) continue;
                    if (++shown > 20) break;
                    L.LogInfo("[probe] " + (marker ? "MARK " : "thai ") + Describe(c, s));
                }
                if (shown == 0) L.LogWarning("[probe] nothing on screen holds Thai or the marker.");

                L.LogInfo("[probe:" + tag + "] ---- end ----");
            }
            catch (Exception e) { L.LogError("[probe] threw: " + e); }
        }

        /// <summary>type, path, font, whether that font can actually draw Thai, and the material —
        /// a custom material still pointing at the old font atlas renders new glyphs as nothing.</summary>
        private static string Describe(Component c, string s)
        {
            var sb = new StringBuilder();
            sb.Append(c.GetType().Name).Append(" '").Append(Path(c)).Append("'");

            var ui = c as Text;
            if (ui != null)
            {
                var f = ui.font;
                sb.Append("  font=").Append(f == null ? "<null>" : f.name);
                if (f != null)
                {
                    sb.Append(" dynamic=").Append(f.dynamic);
                    try { sb.Append(" hasThaiGlyph=").Append(f.HasCharacter('อ')); } catch { }
                }
                Material m = null;
                try { m = ui.material; } catch { }
                sb.Append("  mat=").Append(m == null ? "<null>" : m.name);
                if (m != null)
                {
                    try { sb.Append(" tex=").Append(m.mainTexture == null ? "<null>" : m.mainTexture.name); } catch { }
                }
                // The box, not the font, is what usually eats a translation: a label sized for
                // two Chinese glyphs wraps long Thai to a second line and then clips it.
                try
                {
                    var r = ui.rectTransform.rect;
                    sb.Append("  box=").Append((int)r.width).Append('x').Append((int)r.height)
                      .Append(" size=").Append(ui.fontSize)
                      .Append(" bestFit=").Append(ui.resizeTextForBestFit)
                      .Append(" hOver=").Append(ui.horizontalOverflow)
                      .Append(" vOver=").Append(ui.verticalOverflow)
                      .Append(" needs=").Append((int)ui.preferredWidth).Append('x').Append((int)ui.preferredHeight);
                }
                catch { }
            }
            else
            {
                var fp = Fonts.FontPropOf(c.GetType());
                UnityEngine.Object f = null;
                if (fp != null) { try { f = fp.GetValue(c, null) as UnityEngine.Object; } catch { } }
                sb.Append("  font=").Append(f == null ? "-" : f.name);
            }

            sb.Append("  text=").Append(Show(s));
            return sb.ToString();
        }

        /// <summary>Collapse "LoadGameSlot(Clone)" style repeats so 20 identical slots count once.</summary>
        private static string Collapse(string p)
        {
            var sb = new StringBuilder(p.Length);
            for (int i = 0; i < p.Length; i++) { char ch = p[i]; if (ch < '0' || ch > '9') sb.Append(ch); }
            return sb.ToString();
        }

        private static string Path(Component c)
        {
            try
            {
                var t = c.transform;
                var sb = new StringBuilder(t.name);
                t = t.parent;
                int guard = 0;
                while (t != null && guard++ < 8) { sb.Insert(0, t.name + "/"); t = t.parent; }
                return sb.ToString();
            }
            catch { return "?"; }
        }

        private static string Show(string s)
        {
            if (s == null) return "<null>";
            var sb = new StringBuilder();
            var head = s.Length > 28 ? s.Substring(0, 28) : s;
            sb.Append('\'').Append(head.Replace("\n", "\\n")).Append("' U+[");
            int n = Math.Min(s.Length, 16);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(((int)s[i]).ToString("X4"));
            }
            if (s.Length > n) sb.Append(" ...");
            sb.Append(']');
            return sb.ToString();
        }
    }
}
