using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace LomThaiText
{
    /// <summary>
    /// Raises Thai tone marks that sit on top of an upper vowel, at mesh-render time.
    ///
    /// Unity's legacy UGUI <c>Text</c> emits a glyph quad per character but performs no
    /// OpenType GPOS mark positioning, which Thai needs: in <c>ตั้ง</c>, <c>ชื่อ</c>,
    /// <c>เรื่อง</c>, <c>อื่น</c> the tone mark and the upper vowel are both laid out at the
    /// same height and collide. Nothing in the translation pipeline can fix that — the text is
    /// correct, the layout of it is not.
    ///
    /// The fix keeps the original Unicode (no private-use glyph substitution, which collapses
    /// to tiny baseline quads in this game) and moves only the generated vertices.
    ///
    /// Contributed by @nemu90 in PR #2. Taken here as the shaping core alone; the PR's
    /// per-screen layout constants are deliberately left out, because
    /// <c>Mod/BepInEx/Translation/th/Text/UI.resizer.txt</c> already owns per-path layout and
    /// keeping it there means a tweak is a text edit plus Ctrl+F10, not a rebuild.
    /// </summary>
    internal static class ThaiShaping
    {
        /// <summary>Fraction of the font size a stacked mark is lifted by. Eyeballed, not derived
        /// from font metrics — which is exactly why it is config-tunable rather than a constant.</summary>
        internal static float Lift = 0.24f;

        private static bool _installed;
        private static int _logLeft = 3;

        internal static void Hook(Harmony h)
        {
            // Patched apart from the injector, and its failure swallowed: this runs on every
            // UGUI text mesh in the game, and a diagnostic-grade patch must never be able to
            // take the working translation down with it.
            if (Lift <= 0f) { Plugin.Log.LogInfo("Thai shaping: disabled (ToneMarkLift=0)."); return; }
            try
            {
                var m = AccessTools.Method(typeof(Text), "OnPopulateMesh", new[] { typeof(VertexHelper) });
                if (m == null)
                {
                    Plugin.Log.LogWarning("Thai shaping: Text.OnPopulateMesh(VertexHelper) not found — "
                                          + "stacked tone marks will render unpositioned.");
                    return;
                }
                h.Patch(m, postfix: new HarmonyMethod(typeof(ThaiShaping).GetMethod("AfterPopulateMesh",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
                _installed = true;
                Plugin.Log.LogInfo("Thai shaping: installed on Text.OnPopulateMesh (lift=" + Lift + ").");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Thai shaping: failed to install (" + e.GetType().Name + ").");
            }
        }

        private static void AfterPopulateMesh(Text __instance, VertexHelper __0)
        {
            if (__instance == null || __0 == null) return;
            try
            {
                var n = Raise(__instance, __0);
                if (n > 0 && _logLeft > 0)
                {
                    _logLeft--;
                    Plugin.Log.LogInfo("[thai] raised " + n + " stacked mark(s) in '"
                                       + Clip(__instance.text) + "'");
                }
            }
            catch { /* a throw here would break the game's text rendering, not just ours */ }
        }

        // ------------------------------------------------------------------- the shaping

        internal static int Raise(Text text, VertexHelper mesh)
        {
            var value = text.text;
            if (!Fonts.HasThai(value)) return 0;

            int verts = mesh.currentVertCount;
            if (verts == 0 || verts % 4 != 0) return 0;

            float lift = Math.Max(1f, text.fontSize * Lift);
            int raised = 0;

            for (int i = 0; i < value.Length; i++)
            {
                if (!ShouldRaiseTone(value, i)) continue;

                int quad;
                if (!TryGetQuadIndex(value, i, text.supportRichText, out quad)) continue;

                int first = quad * 4;
                if (first + 3 >= verts) continue;

                for (int o = 0; o < 4; o++)
                {
                    var v = new UIVertex();
                    mesh.PopulateUIVertex(ref v, first + o);
                    v.position.y += lift;
                    mesh.SetUIVertex(v, first + o);
                }
                raised++;
            }
            return raised;
        }

        /// <summary>True when <paramref name="index"/> is a tone mark (U+0E48..U+0E4C) stacked on
        /// top of an upper vowel — the only case that needs lifting. A tone mark straight on a
        /// consonant is already in the right place.</summary>
        internal static bool ShouldRaiseTone(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index <= 0 || index >= text.Length) return false;

            char tone = text[index];
            if (tone < '่' || tone > '์') return false;

            char prev = text[index - 1];
            return prev == 'ั'                          // ั  MAI HAN-AKAT
                   || (prev >= 'ิ' && prev <= 'ื')  // ิ ี ึ ื
                   || prev == '็'                        // ็  MAITAIKHU
                   || prev == 'ํ';                       // ํ  NIKHAHIT
        }

        /// <summary>Maps a string index to its quad index in the generated mesh. Unity emits no
        /// quad for whitespace, and none for the characters inside a rich-text tag, so a naive
        /// index would drift off by one per space in any line long enough to matter.</summary>
        internal static bool TryGetQuadIndex(string text, int textIndex, bool supportRichText, out int quadIndex)
        {
            quadIndex = -1;
            if (string.IsNullOrEmpty(text) || textIndex < 0 || textIndex >= text.Length) return false;

            int next = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (supportRichText && text[i] == '<')
                {
                    int close = text.IndexOf('>', i + 1);
                    if (close >= 0)
                    {
                        if (textIndex >= i && textIndex <= close) return false;
                        i = close;
                        continue;
                    }
                }

                if (char.IsWhiteSpace(text[i]))
                {
                    if (i == textIndex) return false;
                    continue;
                }

                if (i == textIndex) { quadIndex = next; return true; }
                next++;
            }
            return false;
        }

        internal static bool Installed { get { return _installed; } }

        private static string Clip(string s)
        {
            if (s == null) return "";
            s = s.Replace("\n", "\\n");
            return s.Length > 24 ? s.Substring(0, 24) : s;
        }
    }
}
