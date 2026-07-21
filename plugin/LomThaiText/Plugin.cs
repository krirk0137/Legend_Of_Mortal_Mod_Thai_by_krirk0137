using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Lean.Localization;
using UnityEngine;

namespace LomThaiText
{
    // ---------------------------------------------------------------------------
    //  LoM Thai Text — key-based translation injector ("Solution 2")
    //
    //  Every string in Legend of Mortal is fetched by KEY through
    //      Mortal.Core.LeanLocalizationResolver.GetString(key)
    //        -> Lean.Localization.LeanLocalization.GetTranslationText(key)
    //        -> CurrentTranslations[key].Data
    //
    //  LeanLocalization.UpdateTranslations() wipes CurrentTranslations, calls
    //  RegisterAndBuild() on every LeanLocalization instance (compiling the game's
    //  130 LeanLanguageCSV sources), and only THEN fires OnLocalizationChanged
    //  (which refreshes the live UI). A postfix on RegisterAndBuild is therefore the
    //  exact right window, and does not depend on LeanSource registration order.
    //
    //  Everything time-based lives on our own Ticker object, re-created from Apply()
    //  if it ever dies — the plugin component's own Update() was observed never to
    //  run in this game, so nothing may depend on it.
    // ---------------------------------------------------------------------------
    [BepInPlugin(GUID, NAME, "0.4.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.krirk0137.lomthaitext";
        public const string NAME = "LoM Thai Text (key-based injector)";

        internal static ManualLogSource Log;
        internal static Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.Ordinal);

        // Plain statics, never Unity-fake-null. Captured once in Awake.
        internal static bool OptEnabled = true;
        internal static string OptTmpFonts = "Kanit";
        internal static string OptUguiFont = "Tahoma";
        internal static float OptSweep = 1f;
        internal static bool OptAutoDump = true;
        internal static bool OptDiag = true;
        internal static KeyCode KeyReload = KeyCode.F10;
        internal static KeyCode KeyDump = KeyCode.F11;

        private static string _tsvPath;
        private static string _lastReport = "";
        private static bool _dumped;
        private static int _bigApplies;
        internal static bool Probed;

        private void Awake()
        {
            Log = Logger;

            OptEnabled = Config.Bind("General", "Enabled", true,
                "Master switch. False = the plugin loads but injects nothing.").Value;
            var file = Config.Bind("General", "File", "Thai.tsv",
                "Translation file, relative to this plugin's folder. Format: key<TAB>value, UTF-8, "
                + "'#' comments. Escapes in the value: \\n \\t \\\\ .").Value;
            OptTmpFonts = Config.Bind("Font", "TextMeshProFallback", "Kanit,Thai,Noto",
                "Comma-separated name fragments. The first matching TMP_FontAsset in memory is added "
                + "to TMP's GLOBAL fallback list. (XUnity's Kanit_sdf bundle is really named "
                + "'Kanit-Regular SDF' inside.)").Value;
            OptUguiFont = Config.Bind("Font", "UguiOSFont", "Tahoma",
                "Windows font used for legacy UGUI Text components showing Thai. Empty = off.").Value;
            OptSweep = Config.Bind("Font", "SweepSeconds", 1.0f,
                "How often to re-scan for components showing Thai in a font that cannot render it.").Value;
            OptAutoDump = Config.Bind("Debug", "AutoDumpOnce", true,
                "Dump the game's live key->text table to BepInEx/LomThaiText_dump.tsv once it is "
                + "populated. This is the ground truth for building the full translation file.").Value;
            OptDiag = Config.Bind("Debug", "LogInjectedComponents", true,
                "Log the components found displaying injected text, with type and font.").Value;
            KeyReload = Config.Bind("Hotkeys", "Reload", KeyCode.F10, "Ctrl + this key: reload the file.").Value;
            KeyDump = Config.Bind("Hotkeys", "Dump", KeyCode.F11, "Ctrl + this key: dump the live table.").Value;

            _tsvPath = Path.Combine(Path.GetDirectoryName(Info.Location), file);
            LoadFile();

            try
            {
                new Harmony(GUID).PatchAll(typeof(Patches));
                Log.LogInfo("Harmony patch on LeanLocalization.RegisterAndBuild installed.");
            }
            catch (Exception e) { Log.LogError("Harmony patch FAILED: " + e); }

            Log.LogInfo(string.Format("host component: go='{0}' activeInHierarchy={1} enabled={2} isActiveAndEnabled={3}",
                gameObject == null ? "<null>" : gameObject.name,
                gameObject != null && gameObject.activeInHierarchy, enabled, isActiveAndEnabled));

            EnsureTicker();
            LeanLocalization.OnLocalizationChanged += OnLocalizationChanged;
        }

        internal static void EnsureTicker()
        {
            if (Ticker.Inst != null) return;
            try
            {
                var go = new GameObject("LomThaiText");
                UnityEngine.Object.DontDestroyOnLoad(go);
                Ticker.Inst = go.AddComponent<Ticker>();
                go.AddComponent<ThaiSource>();
                Log.LogInfo("Ticker object created.");
            }
            catch (Exception e) { Log.LogError("EnsureTicker failed: " + e); }
        }

        private static void OnLocalizationChanged()
        {
            Fonts.InstallTmpFallback();
            Fonts.Sweep();
        }

        // -------------------------------------------------------------- loading

        internal static void LoadFile()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            int dup = 0, bad = 0, lineNo = 0;

            if (!File.Exists(_tsvPath))
            {
                Log.LogWarning("No translation file at " + _tsvPath + " — injecting nothing.");
                Map = map;
                return;
            }

            foreach (var raw in File.ReadAllLines(_tsvPath, new UTF8Encoding(false)))
            {
                lineNo++;
                var line = raw;
                if (lineNo == 1 && line.Length > 0 && line[0] == (char)0xFEFF) line = line.Substring(1);
                if (line.Length == 0 || line[0] == '#') continue;

                int t = line.IndexOf('\t');
                if (t <= 0) { bad++; continue; }

                var key = line.Substring(0, t);
                var val = Unescape(line.Substring(t + 1));
                if (map.ContainsKey(key)) dup++;
                map[key] = val;
            }

            Map = map;
            Log.LogInfo(string.Format("Loaded {0} keys from {1}{2}{3}",
                map.Count, Path.GetFileName(_tsvPath),
                dup > 0 ? "  (duplicate keys: " + dup + ")" : "",
                bad > 0 ? "  (unparsable lines: " + bad + ")" : ""));
        }

        private static string Unescape(string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length)
                {
                    char c = s[++i];
                    if (c == 'n') { sb.Append('\n'); continue; }
                    if (c == 't') { sb.Append('\t'); continue; }
                    if (c == '\\') { sb.Append('\\'); continue; }
                    sb.Append('\\').Append(c);
                    continue;
                }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------- applying

        internal static void Apply()
        {
            if (!OptEnabled || Map.Count == 0) return;

            int replaced = 0, created = 0;
            foreach (var kv in Map)
            {
                bool existed = LeanLocalization.CurrentTranslations.ContainsKey(kv.Key);
                var tr = LeanLocalization.RegisterTranslation(kv.Key);
                if (tr == null) continue;
                tr.Data = kv.Value;
                tr.Primary = true;
                if (existed) replaced++; else created++;
            }

            int total = LeanLocalization.CurrentTranslations.Count;
            var report = replaced + "/" + created + "/" + total;
            if (report != _lastReport)
            {
                _lastReport = report;
                Log.LogInfo(string.Format(
                    "Applied {0} keys — {1} overwrote an existing game string, {2} were new keys. "
                    + "Game table now holds {3} keys.", replaced + created, replaced, created, total));
            }

            EnsureTicker();

            if (total < 1000) return;
            _bigApplies++;

            if (!_dumped)
            {
                _dumped = true;
                Log.LogInfo("Table populated (" + total + " keys). AutoDumpOnce=" + OptAutoDump);
                if (OptAutoDump) DumpLiveTable();
            }

            // The plugin's own Update() does not run in this game, so drive the probe
            // from here — by the 2nd rebuild of a fully-populated table the UI exists.
            if (!Probed && _bigApplies >= 2)
            {
                Probed = true;
                Probe.Run("build");
            }
        }

        // -------------------------------------------------------------- dumping

        internal static void DumpLiveTable()
        {
            var path = Path.Combine(Paths.BepInExRootPath, "LomThaiText_dump.tsv");
            try { WriteDump(path); return; }
            catch (Exception e) { Log.LogWarning("Dump to " + path + " failed (" + e.GetType().Name + "), retrying elsewhere."); }

            try { WriteDump(Path.Combine(Application.persistentDataPath, "LomThaiText_dump.tsv")); }
            catch (Exception e) { Log.LogError("Dump failed: " + e); }
        }

        private static void WriteDump(string path)
        {
            int n = 0;
            using (var w = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                w.NewLine = "\n";
                w.WriteLine("# LeanLocalization.CurrentTranslations — language=" + LeanLocalization.CurrentLanguage
                            + "  total=" + LeanLocalization.CurrentTranslations.Count);
                foreach (var kv in LeanLocalization.CurrentTranslations)
                {
                    var s = kv.Value.Data as string;
                    if (s == null) continue;
                    w.WriteLine(kv.Key + "\t" + s.Replace("\\", "\\\\").Replace("\r", "")
                                                 .Replace("\n", "\\n").Replace("\t", "\\t"));
                    n++;
                }
            }
            Log.LogInfo("Dumped " + n + " live keys -> " + path);
        }
    }

    // ------------------------------------------------------------------- ticker

    /// <summary>Our own always-on object: font sweeps, hotkeys, delayed probe.</summary>
    internal class Ticker : MonoBehaviour
    {
        internal static Ticker Inst;

        private bool _sawUpdate;
        private float _nextSweep;
        private float _probe1, _probe2;

        private void Start()
        {
            _probe1 = Time.unscaledTime + 12f;
            _probe2 = Time.unscaledTime + 25f;
        }

        private void Update()
        {
            if (!_sawUpdate)
            {
                _sawUpdate = true;
                Plugin.Log.LogInfo("[tick] Ticker.Update is running — timers and hotkeys are live.");
            }

            if (Plugin.OptSweep > 0f && Time.unscaledTime >= _nextSweep)
            {
                _nextSweep = Time.unscaledTime + Plugin.OptSweep;
                Fonts.Sweep();
            }

            if (_probe1 > 0f && Time.unscaledTime >= _probe1) { _probe1 = -1f; Probe.Run("t12"); }
            if (_probe2 > 0f && Time.unscaledTime >= _probe2) { _probe2 = -1f; Probe.Run("t25"); }

            try
            {
                if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) return;
                if (Input.GetKeyDown(Plugin.KeyReload))
                {
                    Plugin.Log.LogInfo("Ctrl+" + Plugin.KeyReload + " — reloading.");
                    Plugin.LoadFile();
                    LeanLocalization.UpdateTranslations();
                }
                else if (Input.GetKeyDown(Plugin.KeyDump)) Plugin.DumpLiveTable();
                else if (Input.GetKeyDown(KeyCode.F9)) Probe.Run("manual");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Hotkey handling failed: " + e.Message);
                enabled = false;
            }
        }
    }

    // ------------------------------------------------------------------ patches

    [HarmonyPatch]
    internal static class Patches
    {
        [HarmonyPatch(typeof(LeanLocalization), "RegisterAndBuild")]
        [HarmonyPostfix]
        private static void AfterBuild()
        {
            try { Plugin.Apply(); }
            catch (Exception e) { Plugin.Log.LogError("Apply() threw: " + e); }
        }
    }

    /// <summary>Redundant injection path — compiled by RegisterAndBuild along with the game's own CSVs.</summary>
    internal class ThaiSource : LeanSource
    {
        public override void Compile(string primaryLanguage, string defaultLanguage)
        {
            try { Plugin.Apply(); }
            catch (Exception e) { Plugin.Log.LogError("ThaiSource.Compile threw: " + e); }
        }
    }
}
