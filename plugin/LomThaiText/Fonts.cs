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
        private static Font _thaiAdjustedFont;
        private static TMP_FontAsset _thaiTmp;
        private static int _diagLeft = 14;
        private static int _fixedUgui;
        private static int _shapeInfoLogLeft = 8;
        private static int _shapeFailureLogLeft = 8;
        private static int _renderTraceLeft = 8;
        private static List<Text> _thaiUgui = new List<Text>();

        private static readonly Dictionary<Type, PropertyInfo> _textProp = new Dictionary<Type, PropertyInfo>();
        private static readonly Dictionary<Type, PropertyInfo> _fontProp = new Dictionary<Type, PropertyInfo>();

        internal static void InitializeThaiShaping(string pluginDir)
        {
            try { _thaiAdjustedFont = AdjustedThaiFont.Load(pluginDir); }
            catch (Exception e) { Plugin.Log.LogError("[thai-shape] initialization failed: " + e); }
        }

        internal static bool HasThai(string s)
        {
            return ThaiFontAdjuster.HasThai(s);
        }

        private static bool HasCjk(string s)
        {
            return ThaiFontAdjuster.HasCjk(s);
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
            int shapedUgui = 0;

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

                    // Legacy UGUI does not apply Thai GSUB/GPOS positioning. Convert its Thai
                    // clusters to the adjusted font's C90 private-use glyphs on every sweep,
                    // because localization components can replace both text and font later.
                    var ui2 = c as Text;
                    if (ui2 != null)
                    {
                        bool shaped;
                        bool shapingAttempted = TryShapeLegacy(ui2, s, out shaped);
                        if (shapingAttempted && !_thaiUgui.Contains(ui2)) _thaiUgui.Add(ui2);
                        if (shaped) shapedUgui++;

                        // The project's own UI.resizer.txt is authoritative where it has a rule;
                        // the generic fitter only covers what it does not.
                        var p = Resizer.PathOf(ui2.transform);
                        ApplyStoryDialogueColor(ui2, p);
                        var resized = Resizer.Apply(ui2, p);
                        var relationshipLayout = ApplyRelationshipPopupLayout(ui2, p);
                        var statPopupLayout = ApplyStatPopupLayout(ui2, p);
                        if (!resized && !relationshipLayout && !statPopupLayout)
                        {
                            Unruled.Note(ui2, p);
                            Fit(ui2);
                        }
                        if (shapingAttempted) continue;
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

                if (shapedUgui > 0 && _shapeInfoLogLeft > 0)
                {
                    _shapeInfoLogLeft--;
                    Plugin.Log.LogInfo("[thai-shape] shaped " + shapedUgui + " legacy UGUI Thai components");
                }
            }
            catch (Exception e) { Plugin.Log.LogError("Sweep failed: " + e); }
        }

        /// <summary>
        /// Localization/font components can overwrite Text after the periodic sweep.
        /// Reapply cached Thai UGUI state at Unity's final canvas-render boundary.
        /// </summary>
        internal static void ShapeBeforeRender()
        {
            if (_thaiAdjustedFont == null) return;

            for (int i = _thaiUgui.Count - 1; i >= 0; i--)
            {
                var ui = _thaiUgui[i];
                if (ui == null)
                {
                    _thaiUgui.RemoveAt(i);
                    continue;
                }

                string value;
                try { value = ui.text; } catch { continue; }
                if (!HasThai(value) || HasCjk(value)) continue;

                bool shaped;
                TryShapeLegacy(ui, value, out shaped);

                string path;
                try { path = Resizer.PathOf(ui.transform); } catch { path = ""; }
                ApplyStoryDialogueColor(ui, path);
                ApplyRelationshipPopupLayout(ui, path);
                ApplyStatPopupLayout(ui, path);

                if (_renderTraceLeft > 0)
                {
                    if (path.EndsWith("/Setting/Text", StringComparison.Ordinal))
                    {
                        _renderTraceLeft--;
                        if (path.Length > 160) path = path.Substring(path.Length - 160);
                        Plugin.Log.LogInfo("[thai-shape] pre-render " + path
                            + " font='" + (ui.font == null ? "<null>" : ui.font.name)
                            + "' codes=" + Codepoints(ui.text));
                    }
                }
            }
        }

        private static void ApplyStoryDialogueColor(Text ui, string path)
        {
            if (ui == null || !ThaiMeshAdjuster.ShouldForceWhiteStoryText(path)) return;
            try
            {
                var color = ui.color;
                if (color.r == 1f && color.g == 1f && color.b == 1f) return;
                color.r = 1f;
                color.g = 1f;
                color.b = 1f;
                ui.color = color;
                ui.SetAllDirty();
            }
            catch { }
        }

        private static bool ApplyRelationshipPopupLayout(Text ui, string path)
        {
            const string relationshipTextPath =
                "/[UI]/MainUI/Layer_5/RelationshipModPlaceholder/" +
                "RelationshipStat(Clone)/Text";
            if (ui == null || path != relationshipTextPath) return false;

            try
            {
                var textRect = ui.rectTransform;
                if (textRect == null) return false;

                var targetWidth = Math.Max(
                    textRect.rect.width,
                    (float)Math.Ceiling(ui.preferredWidth));
                var size = textRect.sizeDelta;
                size.x = targetWidth;
                textRect.sizeDelta = size;

                ui.resizeTextForBestFit = false;
                ui.horizontalOverflow = HorizontalWrapMode.Overflow;
                ui.verticalOverflow = VerticalWrapMode.Overflow;

                var parent = textRect.parent;
                var icon = parent == null
                    ? null
                    : parent.Find("StatModifyIcon") as RectTransform;
                if (icon != null)
                {
                    var iconPosition = icon.localPosition;
                    iconPosition.x =
                        textRect.localPosition.x +
                        targetWidth * (1f - textRect.pivot.x) +
                        icon.rect.width * icon.pivot.x;
                    icon.localPosition = iconPosition;
                }

                ui.SetAllDirty();
                return true;
            }
            catch { return false; }
        }

        internal static void ApplyTransientStatPopupLayout(Text ui)
        {
            if (ui == null) return;

            try
            {
                if (!HasThai(ui.text)) return;
                ApplyStatPopupLayoutCore(
                    ui,
                    Resizer.PathOf(ui.transform),
                    false);
            }
            catch { }
        }

        private static bool ApplyStatPopupLayout(Text ui, string path)
        {
            return ApplyStatPopupLayoutCore(ui, path, true);
        }

        private static bool ApplyStatPopupLayoutCore(
            Text ui,
            string path,
            bool markDirty)
        {
            const string statPopupRoot =
                "/[UI]/MainUI/Layer_5/StatModPlaceholder/";
            if (ui == null ||
                string.IsNullOrEmpty(path) ||
                !path.StartsWith(statPopupRoot, StringComparison.Ordinal))
                return false;

            try
            {
                var textRect = ui.rectTransform;
                var row = textRect == null ? null : textRect.parent as RectTransform;
                if (row == null) return false;

                var normal = IsPopupRowName(row.name, "NormalStat");
                var negativePositive =
                    IsPopupRowName(row.name, "NegativePositiveStat");
                var titleField =
                    textRect.name == "Title";
                var valueField =
                    textRect.name == "Value";
                if ((!normal || !titleField) &&
                    (!negativePositive || (!titleField && !valueField)))
                    return false;

                SetRectWidth(row.Find("Title") as RectTransform, 90f);

                var value = row.Find("Value") as RectTransform;
                SetRectWidth(value, 60f);
                SetAnchoredX(value, 183f);

                if (normal)
                {
                    var bar = row.Find("Bar") as RectTransform;
                    SetRectWidth(bar, 75f);
                    SetAnchoredX(bar, 102f);
                    SetAnchoredX(row.Find("Icon") as RectTransform, 139.5f);
                }
                else
                {
                    var slider = row.Find("Slider") as RectTransform;
                    SetRectWidth(slider, 75f);
                    SetAnchoredX(slider, 102f);

                    if (slider != null)
                    {
                        var handleArea =
                            slider.Find("Handle Slide Area") as RectTransform;
                        SetRectWidth(handleArea, 73.4f);
                        SetAnchoredX(handleArea, -3.4f);
                    }

                    var positiveBar =
                        row.Find("PositiveBar") as RectTransform;
                    var negativeBar =
                        row.Find("NegativeBar") as RectTransform;
                    SetRectWidth(positiveBar, 53f);
                    SetAnchoredX(positiveBar, 115.4f);
                    SetRectWidth(negativeBar, 53f);
                    SetAnchoredX(negativeBar, 115.4f);
                }

                FitStatPopupText(
                    ui,
                    valueField ? 60f : 90f,
                    markDirty);
                return true;
            }
            catch { return false; }
        }

        private static bool IsPopupRowName(string actual, string prefab)
        {
            if (string.IsNullOrEmpty(actual)) return false;
            return actual == prefab ||
                   actual.StartsWith(
                       prefab + "(Clone)",
                       StringComparison.Ordinal);
        }

        private static void SetRectWidth(RectTransform rect, float width)
        {
            if (rect == null) return;
            var size = rect.sizeDelta;
            size.x = width;
            rect.sizeDelta = size;
        }

        private static void SetAnchoredX(RectTransform rect, float x)
        {
            if (rect == null) return;
            var position = rect.anchoredPosition;
            position.x = x;
            rect.anchoredPosition = position;
        }

        private static void FitStatPopupText(
            Text ui,
            float columnWidth,
            bool markDirty)
        {
            const float horizontalPadding = 4f;
            const int minimumReadableSize = 12;

            ui.resizeTextForBestFit = false;
            ui.horizontalOverflow = HorizontalWrapMode.Overflow;
            ui.verticalOverflow = VerticalWrapMode.Overflow;

            var availableWidth = columnWidth - horizontalPadding;
            if (ui.fontSize > 0 && ui.preferredWidth > availableWidth)
            {
                var fittedSize = (int)Math.Floor(
                    ui.fontSize * availableWidth / ui.preferredWidth);
                ui.fontSize = Math.Max(
                    minimumReadableSize,
                    Math.Min(ui.fontSize, fittedSize));
            }

            if (markDirty) ui.SetAllDirty();
        }

        private static bool TryShapeLegacy(Text ui, string source, out bool shaped)
        {
            shaped = false;
            if (ui == null || _thaiAdjustedFont == null || HasCjk(source)) return false;

            string priorText = null;
            Font priorFont = null;
            Material priorMaterial = null;
            bool attempted = false;
            try
            {
                priorText = ui.text;
                priorFont = ui.font;
                priorMaterial = ui.material;
                attempted = true;

                // Keep the real Unicode marks. Runtime mesh evidence showed that the
                // adjusted font's C90 PUA marks become tiny 2x2 baseline quads in this
                // Unity build. MeshTrace positions stacked Unicode tones instead.
                var adjusted = source;
                bool changed = !string.Equals(ui.text, adjusted, StringComparison.Ordinal);
                if (changed) ui.text = adjusted;

                if (!ReferenceEquals(ui.font, _thaiAdjustedFont))
                {
                    ui.font = _thaiAdjustedFont;
                    changed = true;
                }

                if (!ReferenceEquals(ui.material, ui.defaultMaterial))
                {
                    ui.material = null;
                    changed = true;
                }

                if (changed) ui.SetAllDirty();
                shaped = true;
            }
            catch (Exception e)
            {
                if (attempted)
                {
                    try { ui.text = priorText; } catch { }
                    try { ui.font = priorFont; } catch { }
                    try { ui.material = priorMaterial; } catch { }
                    try { ui.SetAllDirty(); } catch { }
                }

                if (_shapeFailureLogLeft > 0)
                {
                    _shapeFailureLogLeft--;
                    Plugin.Log.LogWarning("[thai-shape] component failed: " + BoundedMessage(e));
                }
            }
            return attempted;
        }

        private static string Codepoints(string value)
        {
            if (string.IsNullOrEmpty(value)) return "<empty>";
            var sb = new StringBuilder();
            int count = Math.Min(value.Length, 24);
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(((int)value[i]).ToString("X4"));
            }
            if (value.Length > count) sb.Append(" ...");
            return sb.ToString();
        }

        private static string BoundedMessage(Exception exception)
        {
            const int maxLength = 240;
            string message;
            try { message = exception == null ? "" : exception.Message; }
            catch { message = ""; }
            if (string.IsNullOrEmpty(message))
                message = exception == null ? "unknown failure" : exception.GetType().Name;
            message = message.Replace("\r", "\\r").Replace("\n", "\\n");
            return message.Length <= maxLength ? message : message.Substring(0, maxLength - 3) + "...";
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
