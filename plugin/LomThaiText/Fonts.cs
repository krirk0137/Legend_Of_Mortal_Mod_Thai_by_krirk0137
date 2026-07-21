using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LomThaiText
{
    /// <summary>
    /// Text we inject never passes through XUnity.AutoTranslator, so it never receives
    /// XUnity's font treatment and Thai glyphs come out blank. We install the Thai font
    /// ourselves, two ways:
    ///   • TextMeshPro — add the Thai TMP_FontAsset to TMP's GLOBAL fallback list, which
    ///     fixes every TMP component at once and keeps CJK working (fallback, not replace).
    ///   • anything with a UnityEngine.Font — hand it a dynamic OS font, but only when the
    ///     string is Thai *without* CJK, so we can never turn Chinese into tofu.
    /// The scan is reflective (any component exposing `string text`), because the component
    /// that draws the title screen is not necessarily UGUI Text or TMP_Text.
    /// </summary>
    internal static class Fonts
    {
        private static bool _tmpDone;
        private static Font _osFont;
        private static bool _osTried;
        private static TMP_FontAsset _thaiTmp;
        private static int _diagLeft = 14;
        private static int _fixedUgui;

        private static readonly Dictionary<Type, PropertyInfo> _textProp = new Dictionary<Type, PropertyInfo>();
        private static readonly Dictionary<Type, PropertyInfo> _fontProp = new Dictionary<Type, PropertyInfo>();

        internal static bool HasThai(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++) { int c = s[i]; if (c >= 0x0E00 && c <= 0x0E7F) return true; }
            return false;
        }

        private static bool HasCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++) { int c = s[i]; if (c >= 0x3400 && c <= 0x9FFF) return true; }
            return false;
        }

        // ------------------------------------------------------------------ TMP

        internal static void InstallTmpFallback()
        {
            if (_tmpDone) return;
            var wanted = Plugin.OptTmpFonts;
            if (string.IsNullOrEmpty(wanted)) { _tmpDone = true; return; }

            try
            {
                var all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                TMP_FontAsset found = null;
                foreach (var token in wanted.Split(','))
                {
                    var want = token.Trim();
                    if (want.Length == 0) continue;
                    foreach (var fa in all)
                    {
                        if (fa == null || fa.name == null) continue;
                        if (fa.name.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0) { found = fa; break; }
                    }
                    if (found != null) break;
                }
                if (found == null) return;   // XUnity may not have loaded it yet — retry next sweep

                _thaiTmp = found;
                var list = TMP_Settings.fallbackFontAssets;
                if (list == null)
                {
                    Plugin.Log.LogWarning("TMP_Settings.fallbackFontAssets is null — no global fallback possible.");
                    _tmpDone = true;
                    return;
                }
                if (!list.Contains(found)) list.Add(found);
                _tmpDone = true;
                Plugin.Log.LogInfo("Installed '" + found.name + "' as a GLOBAL TextMeshPro fallback ("
                                   + list.Count + " fallbacks). Refreshing all TMP components.");

                foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
                {
                    if (t == null) continue;
                    try { t.SetAllDirty(); t.ForceMeshUpdate(true, true); } catch { }
                }
            }
            catch (Exception e)
            {
                _tmpDone = true;
                Plugin.Log.LogError("InstallTmpFallback failed: " + e);
            }
        }

        // ----------------------------------------------------------------- UGUI

        private static Font OsFont()
        {
            if (_osFont != null || _osTried) return _osFont;
            _osTried = true;
            var name = Plugin.OptUguiFont;
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                _osFont = Font.CreateDynamicFontFromOSFont(name, 24);
                if (_osFont != null)
                {
                    UnityEngine.Object.DontDestroyOnLoad(_osFont);
                    Plugin.Log.LogInfo("Created dynamic font from OS font '" + name + "'.");
                }
                else Plugin.Log.LogWarning("OS font '" + name + "' not found.");
            }
            catch (Exception e) { Plugin.Log.LogError("OS font creation failed: " + e); }
            return _osFont;
        }

        // ---------------------------------------------------------------- sweep

        internal static void Sweep()
        {
            InstallTmpFallback();
            var os = OsFont();

            try
            {
                foreach (var c in Resources.FindObjectsOfTypeAll<Component>())
                {
                    if (c == null) continue;
                    var type = c.GetType();
                    var tp = TextPropOf(type);
                    if (tp == null) continue;

                    string s;
                    try { s = tp.GetValue(c, null) as string; } catch { continue; }
                    if (!HasThai(s)) continue;

                    var fp = FontPropOf(type);
                    UnityEngine.Object cur = null;
                    if (fp != null) { try { cur = fp.GetValue(c, null) as UnityEngine.Object; } catch { } }

                    if (_diagLeft > 0 && Plugin.OptDiag)
                    {
                        _diagLeft--;
                        Plugin.Log.LogInfo(string.Format("[font] {0} obj='{1}' font='{2}' text='{3}'",
                            type.Name, SafeName(c), cur == null ? "<none>" : cur.name,
                            (s.Length > 30 ? s.Substring(0, 30) : s).Replace("\n", "\\n")));
                        if (_diagLeft == 0) Plugin.Log.LogInfo("[font] (further component logging suppressed)");
                    }

                    // TMP: never replace the font (that would strip CJK from mixed strings).
                    // Instead attach the Thai asset as a fallback ON THIS COMPONENT'S font asset —
                    // the same per-asset path XUnity uses, which is proven to work in this game.
                    var tmp = c as TMP_Text;
                    if (tmp != null)
                    {
                        if (_thaiTmp != null && tmp.font != null && tmp.font != _thaiTmp)
                        {
                            try
                            {
                                var fbs = tmp.font.fallbackFontAssetTable;
                                if (fbs == null) { fbs = new List<TMP_FontAsset>(); tmp.font.fallbackFontAssetTable = fbs; }
                                if (!fbs.Contains(_thaiTmp))
                                {
                                    fbs.Add(_thaiTmp);
                                    Plugin.Log.LogInfo("[font] added Thai fallback to TMP font asset '" + tmp.font.name + "'");
                                }
                                tmp.ForceMeshUpdate(true, true);
                            }
                            catch (Exception e) { Plugin.Log.LogWarning("[font] TMP fallback attach failed: " + e.Message); }
                        }
                        var tp2 = Resizer.PathOf(tmp.transform);
                        if (!Resizer.Apply(tmp, tp2)) Unruled.Note(tmp, tp2);
                        continue;
                    }

                    // The project's own UI.resizer.txt is authoritative where it has a rule;
                    // the generic fitter only covers what it does not.
                    var ui2 = c as Text;
                    if (ui2 != null)
                    {
                        var p = Resizer.PathOf(ui2.transform);
                        if (!Resizer.Apply(ui2, p)) { Unruled.Note(ui2, p); Fit(ui2); }
                    }

                    if (os == null || fp == null || HasCjk(s)) continue;
                    if (fp.PropertyType != typeof(Font) || !fp.CanWrite) continue;
                    if (ReferenceEquals(cur, os)) continue;

                    try
                    {
                        fp.SetValue(c, os, null);

                        // A custom material still points at the OLD font's atlas texture, so the
                        // new font's glyphs would draw as nothing. Fall back to the font's own
                        // material for these components.
                        string matNote = "";
                        var ui = c as Text;
                        if (ui != null)
                        {
                            try
                            {
                                if (!ReferenceEquals(ui.material, ui.defaultMaterial))
                                {
                                    matNote = " (reset custom material '" + (ui.material == null ? "?" : ui.material.name) + "')";
                                    ui.material = null;
                                }
                            }
                            catch { }
                        }

                        var g = c as Graphic;
                        if (g != null) g.SetAllDirty();
                        if (++_fixedUgui <= 8)
                            Plugin.Log.LogInfo("[font] gave '" + Plugin.OptUguiFont + "' to " + type.Name
                                               + " '" + SafeName(c) + "'" + matNote);
                    }
                    catch { }
                }
            }
            catch (Exception e) { Plugin.Log.LogError("Sweep failed: " + e); }
        }

        private static readonly HashSet<int> _fitted = new HashSet<int>();
        private static int _fitLog = 6;

        /// <summary>Thai is longer than the Chinese a label was laid out for, so a short label can
        /// wrap onto a line the box then clips. Shrink those — but ONLY those.
        ///
        /// The first version shrank anything whose text did not fit its rect, which was far too
        /// broad: paragraphs inside a scroll view and vertical stacked labels overflow by design,
        /// and they all collapsed to 8pt. So this now requires a genuinely single-line label,
        /// refuses to go below 70% of the author's size, and gives up when the text is more than
        /// twice the box — at that point shrinking would be unreadable anyway.</summary>
        private static void Fit(Text t)
        {
            if (!Plugin.OptFit || t == null) return;
            int id = t.GetInstanceID();
            if (_fitted.Contains(id)) return;
            try
            {
                var s = t.text;
                var box = t.rectTransform.rect;
                int baseSize = t.resizeTextForBestFit ? Mathf.Max(t.resizeTextMaxSize, 1) : Mathf.Max(t.fontSize, 1);
                int floor = Mathf.Max(Plugin.OptFitMin, Mathf.RoundToInt(baseSize * 0.7f));

                if (t.resizeTextForBestFit)
                {
                    // The game ships most labels with bestFit and a min size of 0, which is fine for
                    // two Chinese glyphs and catastrophic for Thai — it shrinks to unreadable rather
                    // than clip. Give it a floor; below that, let it overflow instead.
                    if (t.resizeTextMinSize >= floor) return;
                    _fitted.Add(id);
                    t.resizeTextMinSize = floor;
                }
                else
                {
                    if (s == null || s.IndexOf('\n') >= 0) return;      // multi-line: wrapping is intended
                    if (t.verticalOverflow == VerticalWrapMode.Overflow) return;   // nothing gets clipped
                    if (box.height > t.fontSize * 2.2f) return;         // a tall box is not a one-line label
                    if (t.preferredWidth <= box.width + 0.5f) return;   // fits already
                    if (t.preferredWidth > box.width * 2f) return;      // hopeless; leave the layout alone
                    _fitted.Add(id);
                    t.resizeTextForBestFit = true;
                    t.resizeTextMaxSize = baseSize;
                    t.resizeTextMinSize = floor;
                }
                t.SetAllDirty();

                if (_fitLog-- > 0)
                    Plugin.Log.LogInfo(string.Format("[fit] '{0}' box={1}x{2} needed={3} -> bestFit {4}..{5}  '{6}'",
                        SafeName(t), (int)box.width, (int)box.height, (int)t.preferredWidth,
                        t.resizeTextMinSize, t.resizeTextMaxSize,
                        s == null ? "" : (s.Length > 20 ? s.Substring(0, 20) : s)));
            }
            catch { }
        }

        internal static PropertyInfo TextPropOf(Type t)
        {
            PropertyInfo p;
            if (_textProp.TryGetValue(t, out p)) return p;
            try { p = t.GetProperty("text", BindingFlags.Public | BindingFlags.Instance, null, typeof(string), Type.EmptyTypes, null); }
            catch { p = null; }
            if (p != null && !p.CanRead) p = null;
            _textProp[t] = p;
            return p;
        }

        internal static PropertyInfo FontPropOf(Type t)
        {
            PropertyInfo p;
            if (_fontProp.TryGetValue(t, out p)) return p;
            try { p = t.GetProperty("font", BindingFlags.Public | BindingFlags.Instance); }
            catch { p = null; }
            _fontProp[t] = p;
            return p;
        }

        internal static string SafeName(Component c)
        {
            try { return c.gameObject == null ? "?" : c.gameObject.name; } catch { return "?"; }
        }
    }
}
