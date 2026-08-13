using System;
using System.Collections.Generic;
using LomThaiText;
using UnityEngine.UI;

static class Program
{
    static int fails = 0;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "PASS  " : "FAIL  ") + what);
        if (!ok) fails++;
    }

    // Which string indexes should be lifted, as a readable list.
    static List<int> Marks(string s)
    {
        var r = new List<int>();
        for (int i = 0; i < s.Length; i++) if (ThaiShaping.ShouldRaiseTone(s, i)) r.Add(i);
        return r;
    }

    static string Join(List<int> l) { return "[" + string.Join(",", l) + "]"; }

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // ---- ShouldRaiseTone: the real words from the bug report ----
        // ตั้ง = ต ั ้ ง  -> the ้ at index 2 sits on ั and must lift
        Check(Join(Marks("ตั้ง")) == "[2]", "ตั้ง lifts only the tone at index 2");
        Check(Join(Marks("ชื่อ")) == "[2]", "ชื่อ lifts only the tone at index 2");
        Check(Join(Marks("เรื่อง")) == "[3]", "เรื่อง lifts only the tone at index 3");
        Check(Join(Marks("อื่น")) == "[2]", "อื่น lifts only the tone at index 2");
        Check(Join(Marks("ตั้งค่า")) == "[2]", "ตั้งค่า: ่ on a consonant is NOT lifted");

        // A tone mark directly on a consonant is already positioned correctly.
        Check(Join(Marks("ข้า")) == "[]", "ข้า lifts nothing");
        Check(Join(Marks("เจ้า")) == "[]", "เจ้า lifts nothing");
        Check(Join(Marks("ท่าน")) == "[]", "ท่าน lifts nothing");
        Check(Join(Marks("ขอรับ")) == "[]", "ขอรับ lifts nothing");

        // Guards
        Check(!ThaiShaping.ShouldRaiseTone("", 0), "empty string is safe");
        Check(!ThaiShaping.ShouldRaiseTone("้", 0), "a leading tone mark is not lifted (no index -1 read)");
        Check(!ThaiShaping.ShouldRaiseTone("ตั้ง", 99), "out-of-range index is safe");
        Check(!ThaiShaping.ShouldRaiseTone("abc", 1), "latin is untouched");

        // ---- TryGetQuadIndex: whitespace and rich text must not shift the mapping ----
        int q;
        Check(ThaiShaping.TryGetQuadIndex("ตั้ง", 2, false, out q) && q == 2,
              "no spaces: quad index == string index");

        // "ชื่อ ตั้ง" — the space emits no quad, so ้ at string index 7 is quad 6.
        var s = "ชื่อ ตั้ง";
        Check(s[7] == '้', "fixture: index 7 really is the tone mark");
        Check(ThaiShaping.TryGetQuadIndex(s, 7, false, out q) && q == 6,
              "a space consumes no quad (7 -> 6)");

        // Rich text: the tag emits no quads either.
        var rich = "<color=#fff>ตั้ง</color>";
        int toneAt = rich.IndexOf('ั') + 1;
        Check(rich[toneAt] == '้', "fixture: located the tone mark inside the tag");
        Check(ThaiShaping.TryGetQuadIndex(rich, toneAt, true, out q) && q == 2,
              "rich-text tags consume no quads");
        Check(!ThaiShaping.TryGetQuadIndex(rich, 3, true, out q),
              "an index inside a tag maps to no quad");
        Check(ThaiShaping.TryGetQuadIndex(rich, toneAt, false, out q) && q == 14,
              "with rich text OFF the tag characters do get quads");
        Check(!ThaiShaping.TryGetQuadIndex("ชื่อ ตั้ง", 4, false, out q),
              "the space itself maps to no quad");

        // ---- Raise: the vertices that actually move ----
        var t = new Text { text = "ตั้ง", fontSize = 50, supportRichText = false };
        var mesh = new VertexHelper(4);
        int raised = ThaiShaping.Raise(t, mesh);
        float lift = 50 * ThaiShaping.Lift;
        Check(raised == 1, "ตั้ง raises exactly one mark");
        Check(Math.Abs(mesh.Y(8) - lift) < 0.001f && Math.Abs(mesh.Y(11) - lift) < 0.001f,
              "all 4 verts of quad 2 moved up by fontSize*Lift");
        Check(mesh.Y(0) == 0f && mesh.Y(4) == 0f && mesh.Y(12) == 0f,
              "no other quad moved");

        // Guards that keep this from corrupting the game's own rendering.
        Check(ThaiShaping.Raise(new Text { text = "Hello" }, new VertexHelper(5)) == 0,
              "non-Thai text is skipped entirely");
        Check(ThaiShaping.Raise(new Text { text = "ตั้ง" }, new VertexHelper(0)) == 0,
              "empty mesh is skipped");
        Check(ThaiShaping.Raise(new Text { text = "ตั้งงงงง" }, new VertexHelper(2)) == 0,
              "a quad index past the mesh is skipped, not written out of bounds");

        Console.WriteLine(fails == 0 ? "\nALL PASS" : "\n" + fails + " FAILED");
        Environment.Exit(fails == 0 ? 0 : 1);
    }
}
