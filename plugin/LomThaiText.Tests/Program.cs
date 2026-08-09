using System;
using System.Collections.Generic;
using System.Reflection;
using LomThaiText;
using UnityEngine;
using UnityEngine.UI;

internal static class Program
{
    private static int _failed;

    private static void Equal(string name, string want, string got)
    {
        if (want == got) return;
        _failed++;
        Console.Error.WriteLine(name + ": expected " + Codes(want) + ", got " + Codes(got));
    }

    private static void Same(string name, object want, object got)
    {
        if (ReferenceEquals(want, got)) return;
        _failed++;
        Console.Error.WriteLine(name + ": references differ");
    }

    private static void True(string name, bool value)
    {
        if (value) return;
        _failed++;
        Console.Error.WriteLine(name + ": expected true");
    }

    private static void False(string name, bool value)
    {
        if (!value) return;
        _failed++;
        Console.Error.WriteLine(name + ": expected false");
    }

    private static void Contains(string name, string expected, string actual)
    {
        if (actual != null && actual.Contains(expected)) return;
        _failed++;
        Console.Error.WriteLine(name + ": expected '" + expected + "' in '" + actual + "'");
    }

    private static void IntEqual(string name, int expected, int actual)
    {
        if (expected == actual) return;
        _failed++;
        Console.Error.WriteLine(name + ": expected " + expected + ", got " + actual);
    }

    private static void FloatEqual(string name, float expected, float actual)
    {
        if (Math.Abs(expected - actual) < 0.001f) return;
        _failed++;
        Console.Error.WriteLine(name + ": expected " + expected + ", got " + actual);
    }

    private static string Codes(string value)
    {
        var parts = new string[value.Length];
        for (var i = 0; i < value.Length; i++)
            parts[i] = "U+" + ((int)value[i]).ToString("X4");
        return string.Join(" ", parts);
    }

    private static void SetStatic(string name, object value)
    {
        var field = typeof(Fonts).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        if (field != null) field.SetValue(null, value);
    }

    private static void ResetSweep(Font adjusted, Font os, params Component[] components)
    {
        Plugin.Log.Clear();
        Plugin.OptTmpFonts = "";
        Plugin.OptUguiFont = "Tahoma";
        Plugin.OptDiag = false;
        Plugin.OptFit = false;
        Resources.Objects = components;

        SetStatic("_tmpDone", true);
        SetStatic("_osFont", os);
        SetStatic("_osTried", true);
        SetStatic("_thaiAdjustedFont", adjusted);
        SetStatic("_diagLeft", 0);
        SetStatic("_fixedUgui", 0);
        SetStatic("_shapeLogLeft", 8);
        SetStatic("_shapeInfoLogLeft", 8);
        SetStatic("_shapeFailureLogLeft", 8);
        SetStatic("_thaiUgui", new List<Text>());
    }

    private static Text NewText(string value, Font font)
    {
        return new Text
        {
            text = value,
            font = font,
            gameObject = new GameObject { name = "test-label" },
            transform = new Transform()
        };
    }

    private static void TestSweepRollsBackFailedShape()
    {
        var oldFont = new Font { name = "old" };
        var adjusted = new Font { name = "adjusted" };
        var os = new Font { name = "os" };
        var customMaterial = new Material { name = "custom" };
        var ui = NewText("ค่า", oldFont);
        ui.material = customMaterial;
        ui.ThrowOnFontSetNumber = ui.FontSetCount + 1;

        ResetSweep(adjusted, os, ui);
        Fonts.Sweep();

        Equal("failed shape restores text", "ค่า", ui.text);
        Same("failed shape restores font", oldFont, ui.font);
        Same("failed shape restores material", customMaterial, ui.material);
        True("failed shape dirties component", ui.DirtyCount > 0);
        False("failed shape never reaches generic OS font", ReferenceEquals(os, ui.font));
    }

    private static void TestFailureLogBudgetIsIndependentAndBounded()
    {
        var oldFont = new Font { name = "old" };
        var adjusted = new Font { name = "adjusted" };
        var os = new Font { name = "os" };
        var ui = NewText("ค่า", oldFont);
        ResetSweep(adjusted, os, ui);

        for (var i = 0; i < 8; i++)
        {
            ui.text = "ค่า";
            ui.font = oldFont;
            Fonts.Sweep();
        }

        Plugin.Log.Warnings.Clear();
        ui.text = "ค่า";
        ui.font = oldFont;
        ui.FailureMessage = new string('x', 2000);
        ui.ThrowOnFontSetNumber = ui.FontSetCount + 1;
        Fonts.Sweep();

        True("failure log survives exhausted info budget", Plugin.Log.Warnings.Count == 1);
        True("component failure log is bounded",
            Plugin.Log.Warnings.Count == 1 && Plugin.Log.Warnings[0].Length <= 300);
    }

    private static void TestAllPuaTextIsRepairedOnLaterSweep()
    {
        var oldFont = new Font { name = "old" };
        var adjusted = new Font { name = "adjusted" };
        var os = new Font { name = "os" };
        var ui = NewText("\uF700\uF70A\uF71A", oldFont);
        ResetSweep(adjusted, os, ui);

        Fonts.Sweep();

        Same("all-PUA Thai text receives adjusted font", adjusted, ui.font);
    }

    private static void TestPreRenderKeepsUnicodeForMeshAdjustment()
    {
        var oldFont = new Font { name = "old" };
        var adjusted = new Font { name = "adjusted" };
        var os = new Font { name = "os" };
        var ui = NewText("ตั้งค่า", oldFont);
        ResetSweep(adjusted, os, ui);

        Fonts.Sweep();
        ui.text = "ตั้งค่า";
        ui.font = oldFont;
        Fonts.ShapeBeforeRender();

        Equal("pre-render keeps real Unicode marks", "ตั้งค่า", ui.text);
        Same("pre-render restores adjusted font", adjusted, ui.font);
    }

    private static void TestOnlyStackedToneNeedsVerticalLift()
    {
        var type = typeof(Fonts).Assembly.GetType("LomThaiText.ThaiMeshAdjuster");
        True("Thai mesh adjuster type exists", type != null);
        if (type == null) return;

        var method = type.GetMethod("ShouldRaiseTone", BindingFlags.NonPublic | BindingFlags.Static);
        True("Thai mesh stacked-tone predicate exists", method != null);
        if (method == null) return;

        var text = "ตั้งค่า";
        True("tone after upper vowel is raised",
            (bool)method.Invoke(null, new object[] { text, 2 }));
        False("standalone tone keeps normal font position",
            (bool)method.Invoke(null, new object[] { text, 5 }));
    }

    private static void TestQuadMappingSkipsSpacesNewlinesAndRichTextTags()
    {
        var method = typeof(ThaiMeshAdjuster).GetMethod(
            "TryGetQuadIndex", BindingFlags.NonPublic | BindingFlags.Static);
        True("Thai mesh quad mapper exists", method != null);
        if (method == null) return;

        var dialogue = "เจ้า เคยได้ยินชื่อ";
        var dialogueArgs = new object[] { dialogue, dialogue.IndexOf("ชื่อ") + 2, true, -1 };
        True("dialogue tone maps to a quad", (bool)method.Invoke(null, dialogueArgs));
        IntEqual("spaces do not consume quads", 15, (int)dialogueArgs[3]);

        var rich = "<b>ชื่อ</b>";
        var richArgs = new object[] { rich, rich.IndexOf("ชื่อ") + 2, true, -1 };
        True("rich-text tone maps to a quad", (bool)method.Invoke(null, richArgs));
        IntEqual("rich-text tags do not consume quads", 2, (int)richArgs[3]);

        var multiline = "เรื่อง\nชื่อ";
        var multilineArgs = new object[] { multiline, multiline.LastIndexOf("ชื่อ") + 2, true, -1 };
        True("multiline tone maps to a quad", (bool)method.Invoke(null, multilineArgs));
        IntEqual("newline does not consume a quad", 8, (int)multilineArgs[3]);
    }

    private static void TestFatePopupShiftUsesRenderedOverflowForAnyWord()
    {
        var method = typeof(ThaiMeshAdjuster).GetMethod(
            "GetOverflowShift", BindingFlags.NonPublic | BindingFlags.Static);
        True("Thai mesh overflow shift helper exists", method != null);
        if (method == null) return;

        const string path =
            "/[UI]/MainUI/Layer_5/StatModPlaceholder/NormalStat(Clone)/Title";
        FloatEqual("future fate popup word shifts by measured overflow", -12f,
            (float)method.Invoke(null, new object[] { path, 42f, 30f }));
        FloatEqual("fate popup word that fits remains in place", 0f,
            (float)method.Invoke(null, new object[] { path, 28f, 30f }));
        const string mentalHeader =
            "/[UI]/MainUI/Layer_3/ActionInfo/MentalBar/Header";
        FloatEqual("mental header shifts left by measured overflow", -8f,
            (float)method.Invoke(null, new object[] { mentalHeader, 38f, 30f }));
        FloatEqual("other paths remain in place", 0f,
            (float)method.Invoke(null, new object[] { "/Other/Title", 42f, 30f }));
    }

    private static void TestMeshDiagnosticReportsExactThaiAndUnityCounts()
    {
        var type = typeof(Fonts).Assembly.GetType("LomThaiText.MeshDiagnostics");
        True("mesh diagnostic type exists", type != null);
        if (type == null) return;

        var method = type.GetMethod("Describe", BindingFlags.NonPublic | BindingFlags.Static);
        True("mesh diagnostic formatter exists", method != null);
        if (method == null) return;

        var report = method.Invoke(null, new object[]
        {
            "UI/Layer_1/Buttons/Setting/Text",
            "ตั\uF714งค\uF70Aา",
            7,
            7,
            32,
            28,
            42
        }) as string;

        Contains("mesh diagnostic exact code points",
            "codes=0E15 0E31 F714 0E07 0E04 F70A 0E32", report);
        Contains("mesh diagnostic generator counts",
            "chars=7 visible=7 genVerts=32 meshVerts=28 indices=42", report);
    }

    private static void TestOnlyStoryDialogueBodyIsForcedWhite()
    {
        var method = typeof(ThaiMeshAdjuster).GetMethod(
            "ShouldForceWhiteStoryText", BindingFlags.NonPublic | BindingFlags.Static);
        True("story dialogue white-style classifier exists", method != null);
        if (method == null) return;

        True("character dialogue body is white",
            (bool)method.Invoke(null, new object[] {
                "/StoryCore/SayDialogs/CharacterSayDialog/Panel/Background/StoryText"
            }));
        True("thought dialogue body is white",
            (bool)method.Invoke(null, new object[] {
                "/StoryCore/SayDialogs/ThinkSayDialog/Panel/Background/StoryText"
            }));
        True("narration body is white",
            (bool)method.Invoke(null, new object[] {
                "/StoryCore/SayDialogs/NarrativeSayDialog/Panel/Background/StoryText"
            }));
        True("center-screen narration body is white",
            (bool)method.Invoke(null, new object[] {
                "/StoryCore/SayDialogs/CenterNarrativeSayDialog/Panel/StoryText"
            }));
        False("speaker name keeps game styling",
            (bool)method.Invoke(null, new object[] {
                "/StoryCore/SayDialogs/CharacterSayDialog/Panel/Name/Title"
            }));
        False("other orange UI remains untouched",
            (bool)method.Invoke(null, new object[] {
                "/[UI]/TopPanel/StatusPanel/Container/Player/Title"
            }));
    }

    private static void TestRelationshipPopupExpandsWithoutCoveringItsIcon()
    {
        var method = typeof(Fonts).GetMethod(
            "ApplyRelationshipPopupLayout", BindingFlags.NonPublic | BindingFlags.Static);
        True("relationship popup layout helper exists", method != null);
        if (method == null) return;

        var container = new RectTransform();
        var label = NewText("ความชื่นชอบ", new Font { name = "adjusted" });
        label.preferredWidth = 191f;
        label.rectTransform = new RectTransform
        {
            name = "Text",
            rect = new Rect { width = 80f, height = 50f },
            sizeDelta = new Vector2 { x = 80f, y = 50f },
            pivot = new Vector2 { x = 0f, y = 1f },
            localPosition = new Vector3 { x = 91f, y = -15.79f }
        };
        label.transform = label.rectTransform;
        container.AddChild(label.rectTransform);

        var icon = new RectTransform
        {
            name = "StatModifyIcon",
            rect = new Rect { width = 50f, height = 50f },
            pivot = new Vector2 { x = 0.5f, y = 0.5f },
            localPosition = new Vector3 { x = 175.2f, y = -40.4f }
        };
        container.AddChild(icon);

        const string path =
            "/[UI]/MainUI/Layer_5/RelationshipModPlaceholder/" +
            "RelationshipStat(Clone)/Text";
        True("relationship popup layout is applied",
            (bool)method.Invoke(null, new object[] { label, path }));
        FloatEqual("relationship label grows to its rendered width",
            191f, label.rectTransform.sizeDelta.x);
        IntEqual("relationship label stays on one line",
            (int)HorizontalWrapMode.Overflow, (int)label.horizontalOverflow);
        FloatEqual("relationship icon moves after the expanded label",
            307f, icon.localPosition.x);
    }

    private static void TestRelationshipPopupLayoutLeavesOtherLabelsUntouched()
    {
        var method = typeof(Fonts).GetMethod(
            "ApplyRelationshipPopupLayout", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) return;

        var container = new RectTransform();
        var label = NewText("ความชื่นชอบ", new Font { name = "adjusted" });
        label.preferredWidth = 191f;
        label.rectTransform = new RectTransform
        {
            name = "Text",
            rect = new Rect { width = 80f, height = 50f },
            sizeDelta = new Vector2 { x = 80f, y = 50f },
            localPosition = new Vector3 { x = 91f }
        };
        label.transform = label.rectTransform;
        container.AddChild(label.rectTransform);

        var icon = new RectTransform
        {
            name = "StatModifyIcon",
            rect = new Rect { width = 50f, height = 50f },
            localPosition = new Vector3 { x = 175.2f }
        };
        container.AddChild(icon);

        False("unrelated popup layout is ignored",
            (bool)method.Invoke(null, new object[] {
                label,
                "/[UI]/MainUI/Layer_5/OtherPlaceholder/Other(Clone)/Text"
            }));
        FloatEqual("unrelated label width is unchanged",
            80f, label.rectTransform.sizeDelta.x);
        FloatEqual("unrelated icon position is unchanged",
            175.2f, icon.localPosition.x);
    }

    private static void TestNormalStatPopupKeepsThaiTitleInsideItsRow()
    {
        var method = typeof(Fonts).GetMethod(
            "ApplyStatPopupLayout", BindingFlags.NonPublic | BindingFlags.Static);
        True("normal stat popup layout helper exists", method != null);
        if (method == null) return;

        var row = new RectTransform { name = "NormalStat(Clone)" };
        var title = NewText("การบ่มเพาะตน", new Font { name = "adjusted" });
        title.fontSize = 18;
        title.preferredWidth = 132f;
        title.rectTransform = new RectTransform
        {
            name = "Title",
            rect = new Rect { width = 40f, height = 30f },
            sizeDelta = new Vector2 { x = 40f, y = 30f },
            anchoredPosition = new Vector2 { x = 7f, y = 0f }
        };
        title.transform = title.rectTransform;
        row.AddChild(title.rectTransform);

        var bar = new RectTransform
        {
            name = "Bar",
            sizeDelta = new Vector2 { x = 120f, y = 14f },
            anchoredPosition = new Vector2 { x = 57.2f, y = -7.3f }
        };
        row.AddChild(bar);
        var icon = new RectTransform
        {
            name = "Icon",
            sizeDelta = new Vector2 { x = 50f, y = 50f },
            anchoredPosition = new Vector2 { x = 132.2f, y = 0f }
        };
        row.AddChild(icon);
        var value = new RectTransform
        {
            name = "Value",
            sizeDelta = new Vector2 { x = 40f, y = 28f },
            anchoredPosition = new Vector2 { x = 209.5f, y = -0.8f }
        };
        row.AddChild(value);

        const string path =
            "/[UI]/MainUI/Layer_5/StatModPlaceholder/" +
            "NormalStat(Clone)/Title";
        True("normal stat title layout is applied",
            (bool)method.Invoke(null, new object[] { title, path }));
        FloatEqual("normal stat title gets a readable column",
            90f, title.rectTransform.sizeDelta.x);
        FloatEqual("normal stat bar moves after the title",
            102f, bar.anchoredPosition.x);
        FloatEqual("normal stat bar keeps a useful width",
            75f, bar.sizeDelta.x);
        FloatEqual("normal stat marker stays centered over its bar",
            139.5f, icon.anchoredPosition.x);
        FloatEqual("normal stat value gets a wider column",
            60f, value.sizeDelta.x);
        FloatEqual("normal stat value remains before the sign",
            183f, value.anchoredPosition.x);
        IntEqual("long Thai title is fitted but remains readable",
            12, title.fontSize);
        IntEqual("normal stat title stays on one line",
            (int)HorizontalWrapMode.Overflow, (int)title.horizontalOverflow);
    }

    private static void TestNegativePositivePopupKeepsThaiValueOnOneLine()
    {
        var method = typeof(Fonts).GetMethod(
            "ApplyStatPopupLayout", BindingFlags.NonPublic | BindingFlags.Static);
        True("negative-positive popup layout helper exists", method != null);
        if (method == null) return;

        var row = new RectTransform { name = "NegativePositiveStat(Clone)" };
        var title = new RectTransform
        {
            name = "Title",
            sizeDelta = new Vector2 { x = 43.2f, y = 33.6f },
            anchoredPosition = new Vector2 { x = 7f, y = 0f }
        };
        row.AddChild(title);

        var slider = new RectTransform
        {
            name = "Slider",
            sizeDelta = new Vector2 { x = 140f, y = 20f },
            anchoredPosition = new Vector2 { x = 60.3f, y = -8.2f }
        };
        row.AddChild(slider);
        var handleArea = new RectTransform
        {
            name = "Handle Slide Area",
            sizeDelta = new Vector2 { x = 137f, y = 20f },
            anchoredPosition = new Vector2 { x = -6.3f, y = -8.3f }
        };
        slider.AddChild(handleArea);
        var positiveBar = new RectTransform
        {
            name = "PositiveBar",
            sizeDelta = new Vector2 { x = 99f, y = 14f },
            anchoredPosition = new Vector2 { x = 85.3f, y = -7.1f }
        };
        row.AddChild(positiveBar);
        var negativeBar = new RectTransform
        {
            name = "NegativeBar",
            sizeDelta = new Vector2 { x = 99f, y = 14f },
            anchoredPosition = new Vector2 { x = 85.3f, y = -7.1f }
        };
        row.AddChild(negativeBar);

        var value = NewText("ปานกลาง", new Font { name = "adjusted" });
        value.fontSize = 18;
        value.preferredWidth = 75f;
        value.rectTransform = new RectTransform
        {
            name = "Value",
            rect = new Rect { width = 50f, height = 55.06f },
            sizeDelta = new Vector2 { x = 50f, y = 55.06f },
            anchoredPosition = new Vector2 { x = 203.4f, y = 15.3f }
        };
        value.transform = value.rectTransform;
        row.AddChild(value.rectTransform);

        const string path =
            "/[UI]/MainUI/Layer_5/StatModPlaceholder/" +
            "NegativePositiveStat(Clone)/Value";
        True("negative-positive value layout is applied",
            (bool)method.Invoke(null, new object[] { value, path }));
        FloatEqual("negative-positive title gets a readable column",
            90f, title.sizeDelta.x);
        FloatEqual("negative-positive slider moves after the title",
            102f, slider.anchoredPosition.x);
        FloatEqual("negative-positive slider keeps a useful width",
            75f, slider.sizeDelta.x);
        FloatEqual("slider handle area scales with its parent",
            73.4f, handleArea.sizeDelta.x);
        FloatEqual("positive bar scales with the slider",
            53f, positiveBar.sizeDelta.x);
        FloatEqual("negative bar scales with the slider",
            53f, negativeBar.sizeDelta.x);
        FloatEqual("negative-positive value gets a wider column",
            60f, value.rectTransform.sizeDelta.x);
        FloatEqual("negative-positive value remains before the sign",
            183f, value.rectTransform.anchoredPosition.x);
        IntEqual("Thai level value remains readable",
            13, value.fontSize);
        IntEqual("Thai level value stays on one line",
            (int)HorizontalWrapMode.Overflow, (int)value.horizontalOverflow);
    }

    private static void TestTransientCloneVariantIsLaidOutBeforeFirstMesh()
    {
        var method = typeof(Fonts).GetMethod(
            "ApplyTransientStatPopupLayout",
            BindingFlags.NonPublic | BindingFlags.Static);
        True("transient stat popup render hook exists", method != null);
        if (method == null) return;

        var uiRoot = new RectTransform { name = "[UI]" };
        var mainUi = new RectTransform { name = "MainUI" };
        var layer = new RectTransform { name = "Layer_5" };
        var placeholder = new RectTransform { name = "StatModPlaceholder" };
        var row = new RectTransform
        {
            name = "NegativePositiveStat(Clone)(Clone)"
        };
        uiRoot.AddChild(mainUi);
        mainUi.AddChild(layer);
        layer.AddChild(placeholder);
        placeholder.AddChild(row);

        var title = new RectTransform
        {
            name = "Title",
            sizeDelta = new Vector2 { x = 43.2f, y = 33.6f }
        };
        row.AddChild(title);
        var slider = new RectTransform
        {
            name = "Slider",
            sizeDelta = new Vector2 { x = 140f, y = 20f }
        };
        row.AddChild(slider);

        var value = NewText("ปานกลาง", new Font { name = "adjusted" });
        value.fontSize = 18;
        value.preferredWidth = 75f;
        value.rectTransform = new RectTransform
        {
            name = "Value",
            rect = new Rect { width = 50f, height = 55.06f },
            sizeDelta = new Vector2 { x = 50f, y = 55.06f }
        };
        value.transform = value.rectTransform;
        row.AddChild(value.rectTransform);

        method.Invoke(null, new object[] { value });

        FloatEqual("first mesh render widens the transient value",
            60f, value.rectTransform.sizeDelta.x);
        FloatEqual("first mesh render narrows the transient slider",
            75f, slider.sizeDelta.x);
        IntEqual("first mesh render prevents Thai value wrapping",
            (int)HorizontalWrapMode.Overflow, (int)value.horizontalOverflow);
    }

    private static int Main()
    {
        Equal("direct tone", "ค\uF70Aา", ThaiFontAdjuster.Adjust("ค่า"));
        Equal("stacked tone", "ตั\uF714ง", ThaiFontAdjuster.Adjust("ตั้ง"));
        Equal("thi with mai ek", "ที\uF713", ThaiFontAdjuster.Adjust("ที่"));
        Equal("phi with mai ek", "พี\uF713", ThaiFontAdjuster.Adjust("พี่"));
        Equal("ascender", "ป\uF702\uF713", ThaiFontAdjuster.Adjust("ปี่"));
        Equal("lower vowel", "ฎ\uF718", ThaiFontAdjuster.Adjust("ฎุ"));
        Equal("rich text", "<b>ค\uF70Aา</b>", ThaiFontAdjuster.Adjust("<b>ค่า</b>"));

        var once = ThaiFontAdjuster.Adjust("ตั้งค่า ปี่ ฎุ");
        Equal("idempotent", once, ThaiFontAdjuster.Adjust(once));

        True("Thai source range", ThaiFontAdjuster.HasThai("ตั้งค่า"));
        True("adjusted Thai PUA range", ThaiFontAdjuster.HasThai("\uF700\uF71A"));
        True("Fonts adjusted Thai PUA range", Fonts.HasThai("\uF700\uF71A"));
        False("Thai is not CJK", ThaiFontAdjuster.HasCjk("ภาษาไทย"));
        True("BMP unified CJK", ThaiFontAdjuster.HasCjk("ไทย中文"));
        True("BMP CJK compatibility", ThaiFontAdjuster.HasCjk("\uF900"));
        True("supplementary CJK extension", ThaiFontAdjuster.HasCjk("\uD840\uDC00"));
        True("supplementary CJK compatibility", ThaiFontAdjuster.HasCjk("\uD87E\uDC00"));

        TestSweepRollsBackFailedShape();
        TestFailureLogBudgetIsIndependentAndBounded();
        TestAllPuaTextIsRepairedOnLaterSweep();
        TestPreRenderKeepsUnicodeForMeshAdjustment();
        TestOnlyStackedToneNeedsVerticalLift();
        TestQuadMappingSkipsSpacesNewlinesAndRichTextTags();
        TestFatePopupShiftUsesRenderedOverflowForAnyWord();
        TestMeshDiagnosticReportsExactThaiAndUnityCounts();
        TestOnlyStoryDialogueBodyIsForcedWhite();
        TestRelationshipPopupExpandsWithoutCoveringItsIcon();
        TestRelationshipPopupLayoutLeavesOtherLabelsUntouched();
        TestNormalStatPopupKeepsThaiTitleInsideItsRow();
        TestNegativePositivePopupKeepsThaiValueOnOneLine();
        TestTransientCloneVariantIsLaidOutBeforeFirstMesh();

        Console.WriteLine(_failed == 0
            ? "PASS: integrated shaping tests"
            : "FAIL: " + _failed);
        return _failed == 0 ? 0 : 1;
    }
}

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void DontDestroyOnLoad(Object value) { }
    }

    public class GameObject : Object { }

    public class Transform : Object
    {
        private readonly Dictionary<string, Transform> _children =
            new Dictionary<string, Transform>(StringComparer.Ordinal);

        public Transform parent;
        public Vector3 localPosition;

        public void AddChild(Transform child)
        {
            child.parent = this;
            _children[child.name] = child;
        }

        public Transform Find(string childName)
        {
            Transform child;
            return _children.TryGetValue(childName, out child) ? child : null;
        }
    }

    public class Component : Object
    {
        private static int _nextId;
        private readonly int _id = ++_nextId;
        public GameObject gameObject = new GameObject();
        public Transform transform = new Transform();
        public int GetInstanceID() { return _id; }
    }

    public class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string fontName, int size)
        {
            return new Font { name = fontName };
        }
    }

    public class Material : Object { }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }

    public struct Rect
    {
        public float width;
        public float height;
    }

    public struct Vector2
    {
        public float x;
        public float y;
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
    }

    public class RectTransform : Transform
    {
        public Rect rect;
        public Vector2 sizeDelta;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
    }

    public enum HorizontalWrapMode
    {
        Wrap,
        Overflow
    }

    public enum VerticalWrapMode
    {
        Truncate,
        Overflow
    }

    public static class Mathf
    {
        public static int Max(int left, int right) { return left > right ? left : right; }
        public static int RoundToInt(float value) { return (int)Math.Round(value); }
    }

    public static class Resources
    {
        public static Object[] Objects = new Object[0];

        public static T[] FindObjectsOfTypeAll<T>() where T : Object
        {
            var found = new List<T>();
            foreach (var value in Objects)
            {
                var typed = value as T;
                if (typed != null) found.Add(typed);
            }
            return found.ToArray();
        }
    }
}

namespace UnityEngine.UI
{
    using UnityEngine;

    public class Graphic : Component
    {
        public int DirtyCount;
        public Color color;
        public virtual void SetAllDirty() { DirtyCount++; }
    }

    public class Text : Graphic
    {
        private Font _font;
        private Material _material;

        public string text { get; set; }

        public Font font
        {
            get { return _font; }
            set
            {
                FontSetCount++;
                if (ThrowOnFontSetNumber == FontSetCount)
                    throw new InvalidOperationException(FailureMessage);
                _font = value;
            }
        }

        public Material defaultMaterial = new Material { name = "default" };

        public Material material
        {
            get { return _material ?? defaultMaterial; }
            set { _material = value; }
        }

        public int FontSetCount;
        public int ThrowOnFontSetNumber = -1;
        public string FailureMessage = "injected font failure";
        public RectTransform rectTransform = new RectTransform();
        public bool resizeTextForBestFit;
        public int resizeTextMaxSize;
        public int resizeTextMinSize;
        public int fontSize = 24;
        public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
        public float preferredWidth;
    }
}

namespace TMPro
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class TMP_FontAsset : Object
    {
        public List<TMP_FontAsset> fallbackFontAssetTable;
    }

    public static class TMP_Settings
    {
        public static List<TMP_FontAsset> fallbackFontAssets = new List<TMP_FontAsset>();
    }

    public class TMP_Text : Graphic
    {
        public string text { get; set; }
        public TMP_FontAsset font { get; set; }
        public void ForceMeshUpdate(bool ignoreActiveState, bool forceTextReparsing) { }
    }
}

namespace LomThaiText
{
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;

    internal static class Plugin
    {
        internal static readonly TestLog Log = new TestLog();
        internal static string OptTmpFonts;
        internal static string OptUguiFont;
        internal static bool OptDiag;
        internal static bool OptFit;
        internal static int OptFitMin = 8;
    }

    internal sealed class TestLog
    {
        internal readonly List<string> Infos = new List<string>();
        internal readonly List<string> Warnings = new List<string>();
        internal readonly List<string> Errors = new List<string>();

        internal void Clear()
        {
            Infos.Clear();
            Warnings.Clear();
            Errors.Clear();
        }

        public void LogInfo(object value) { Infos.Add(Convert.ToString(value)); }
        public void LogWarning(object value) { Warnings.Add(Convert.ToString(value)); }
        public void LogError(object value) { Errors.Add(Convert.ToString(value)); }
    }

    internal static class AdjustedThaiFont
    {
        internal static Font Load(string pluginDir) { return null; }
    }

    internal static class Resizer
    {
        internal static string PathOf(Transform transform)
        {
            var path = "";
            var guard = 0;
            while (transform != null && guard++ < 24)
            {
                path = "/" + transform.name + path;
                transform = transform.parent;
            }
            return path;
        }
        internal static bool Apply(object component, string path) { return false; }
    }

    internal static class Unruled
    {
        internal static void Note(object component, string path) { }
    }
}
