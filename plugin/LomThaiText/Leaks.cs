using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using HarmonyLib;
using Lean.Localization;
using UnityEngine;

namespace LomThaiText
{
    /// <summary>
    /// Records every Chinese string the game still shows while you play, so "what is left to
    /// translate" stops being something you find by walking the game and squinting at it.
    ///
    /// Two nets, because they catch different things:
    ///
    ///   1. <b>keys</b> — postfix on <c>LeanLocalization.GetTranslationText</c>. Any key whose
    ///      value still contains CJK is recorded with a hit count. This is the good net: it
    ///      yields the KEY, which drops straight into <c>Thai.tsv</c>, and it is ordered by how
    ///      often the game actually asked for it, so the worklist is sorted by player impact.
    ///
    ///   2. <b>screen</b> — anything with a <c>string text</c> property that is live in the
    ///      hierarchy and still contains CJK. This catches what net 1 structurally cannot:
    ///      string literals compiled into the game, and text composed at runtime out of pieces.
    ///      No key exists for those, so it reports the text and the scene path instead.
    ///
    /// Borrowed from ENMod Continued (觅长生), which keeps a <c>MissedStrings.txt</c> the same
    /// way. Its version scans text only; ours resolves the key, which is the part that makes the
    /// output directly actionable.
    /// </summary>
    internal static class Leaks
    {
        internal static bool Enabled = true;
        internal static int Cap = 20000;

        private sealed class Row { internal string Text; internal int Hits; }

        private static readonly Dictionary<string, Row> _keys = new Dictionary<string, Row>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> _screen = new Dictionary<string, string>(StringComparer.Ordinal);
        private static bool _dirty;
        private static float _nextWrite;
        private static bool _keyNetLive;

        internal static bool HasCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++)
            {
                int c = s[i];
                if (c >= 0x3400 && c <= 0x9FFF) return true;   // CJK Unified + Ext-A
                if (c >= 0xF900 && c <= 0xFAFF) return true;   // compatibility ideographs
            }
            return false;
        }

        // ------------------------------------------------------------------ net 1: keys

        /// <summary>
        /// Patched separately from <see cref="Patches"/> and swallowed on failure: the injector is
        /// proven and must not be taken down by a diagnostic. If the method signature ever moves,
        /// the key net goes quiet and the screen net still works.
        /// </summary>
        internal static void Hook(Harmony h)
        {
            if (!Enabled) return;
            try
            {
                // Matched by shape, not by an exact signature: the shipped LeanLocalization has
                // more than one GetTranslationText overload in some builds, and pinning the
                // argument list would silently disable the net on the next game update.
                System.Reflection.MethodInfo m = null;
                foreach (var cand in typeof(LeanLocalization).GetMethods(
                             System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                             | System.Reflection.BindingFlags.Static))
                {
                    if (cand.Name != "GetTranslationText" || cand.ReturnType != typeof(string)) continue;
                    var ps = cand.GetParameters();
                    if (ps.Length == 0 || ps[0].ParameterType != typeof(string)) continue;
                    if (m == null || ps.Length < m.GetParameters().Length) m = cand;
                }

                if (m == null)
                {
                    Plugin.Log.LogWarning("Leak tracking: no static string GetTranslationText(string, ...) on "
                                          + "LeanLocalization — key net disabled, screen net still active.");
                    return;
                }
                h.Patch(m, postfix: new HarmonyMethod(typeof(Leaks).GetMethod("AfterGetTranslationText",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
                _keyNetLive = true;
                Plugin.Log.LogInfo("Leak tracking: key net installed on " + m.Name + "("
                                   + m.GetParameters().Length + " args).");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Leak tracking: key net failed to install (" + e.GetType().Name
                                      + ") — screen net still active.");
            }
        }

        // __0 is the first argument positionally; matching on the parameter name would break if
        // the shipped LeanLocalization ever renames it.
        private static void AfterGetTranslationText(string __0, string __result)
        {
            if (!Enabled || __0 == null || !HasCjk(__result)) return;
            try
            {
                Row r;
                if (_keys.TryGetValue(__0, out r)) { r.Hits++; return; }
                if (_keys.Count >= Cap) return;
                _keys[__0] = new Row { Text = __result, Hits = 1 };
                _dirty = true;
            }
            catch { }
        }

        // ---------------------------------------------------------------- net 2: screen

        /// <summary>Called from the font sweep, which has already read the component's text.
        /// Deduped by text rather than by path: the same untranslated string showing in five
        /// places is one line of work, not five.</summary>
        internal static void NoteOnScreen(Component c, string s)
        {
            if (!Enabled || _screen.ContainsKey(s) || _screen.Count >= Cap) return;
            try
            {
                if (c == null) return;
                var go = c.gameObject;
                if (go == null || !go.activeInHierarchy) return;   // only what the player can actually see
                _screen[s] = Resizer.PathOf(c.transform);
                _dirty = true;
            }
            catch { }
        }

        // --------------------------------------------------------------------- writing

        internal static void FlushIfDue()
        {
            if (!_dirty || Time.unscaledTime < _nextWrite) return;
            _nextWrite = Time.unscaledTime + 5f;
            _dirty = false;
            Flush();
        }

        internal static void Flush()
        {
            WriteKeys();
            WriteScreen();
        }

        private static void WriteKeys()
        {
            if (_keys.Count == 0) return;
            try
            {
                // Sorted by hits so the top of the file is the text the player meets most.
                var order = new List<KeyValuePair<string, Row>>(_keys);
                order.Sort((a, b) => b.Value.Hits.CompareTo(a.Value.Hits));

                using (var w = Open("LomThaiText_missed_keys.tsv"))
                {
                    w.WriteLine("# Keys the game asked for whose value is STILL Chinese — the translation worklist.");
                    w.WriteLine("# Sorted by hits: the top lines are what the player sees most often.");
                    w.WriteLine("# Paste 'key<TAB>thai' into Thai.tsv, then Ctrl+F10 in game to see it.");
                    w.WriteLine("# key\thits\tchinese");
                    foreach (var kv in order)
                        w.WriteLine(kv.Key + "\t" + kv.Value.Hits + "\t" + Esc(kv.Value.Text));
                }
            }
            catch { /* Program Files can be unwritable; the counts survive in memory until F8 */ }
        }

        private static void WriteScreen()
        {
            if (_screen.Count == 0) return;
            try
            {
                using (var w = Open("LomThaiText_missed_screen.tsv"))
                {
                    w.WriteLine("# Chinese visible on screen that never came through the key table:");
                    w.WriteLine("# string literals compiled into the game, and text composed at runtime.");
                    w.WriteLine("# These have NO key — they need a code patch or an XUnity dictionary line.");
                    w.WriteLine("# chinese\tscene path");
                    foreach (var kv in _screen)
                        w.WriteLine(Esc(kv.Key) + "\t" + kv.Value);
                }
            }
            catch { }
        }

        private static StreamWriter Open(string name)
        {
            var w = new StreamWriter(Path.Combine(Paths.BepInExRootPath, name), false, new UTF8Encoding(false));
            w.NewLine = "\n";
            return w;
        }

        private static string Esc(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\r", "").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        internal static string Report()
        {
            return "leaks: " + _keys.Count + " untranslated keys"
                   + (_keyNetLive ? "" : " (key net OFF)")
                   + ", " + _screen.Count + " un-keyed strings on screen";
        }
    }
}
