// Minimal stand-ins so the REAL ThaiShaping.cs can be compiled and exercised off-engine.
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector3 { public float x, y, z; }
    public struct UIVertex { public Vector3 position; }
}

namespace UnityEngine.UI
{
    public class Text
    {
        public string text;
        public int fontSize = 24;
        public bool supportRichText = true;
    }

    public class VertexHelper
    {
        private readonly List<UIVertex> _v = new List<UIVertex>();
        public VertexHelper(int quads) { for (int i = 0; i < quads * 4; i++) _v.Add(new UIVertex()); }
        public int currentVertCount { get { return _v.Count; } }
        public void PopulateUIVertex(ref UIVertex v, int i) { v = _v[i]; }
        public void SetUIVertex(UIVertex v, int i) { _v[i] = v; }
        public float Y(int i) { return _v[i].position.y; }
    }
}

namespace HarmonyLib
{
    public class Harmony
    {
        public void Patch(System.Reflection.MethodBase m, HarmonyMethod prefix = null,
                          HarmonyMethod postfix = null) { }
    }
    public class HarmonyMethod { public HarmonyMethod(System.Reflection.MethodInfo m) { } }
    public static class AccessTools
    {
        public static System.Reflection.MethodInfo Method(System.Type t, string n, System.Type[] a) { return null; }
    }
}

namespace LomThaiText
{
    internal static class Fonts
    {
        internal static bool HasThai(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (var c in s) if (c >= 0x0E00 && c <= 0x0E7F) return true;
            return false;
        }
    }

    internal static class Plugin
    {
        internal static readonly Logger Log = new Logger();
        internal class Logger
        {
            public void LogInfo(object o) { }
            public void LogWarning(object o) { }
        }
    }
}
