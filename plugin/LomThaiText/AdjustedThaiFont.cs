using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LomThaiText
{
    internal static class AdjustedThaiFont
    {
        private const uint FR_PRIVATE = 0x10;
        private const string FontFileName = "NotoSansThai-Regular-Adjusted.ttf";
        private const int FontSize = 24;
        private const int MaxFailureLogs = 4;
        private const int MaxFailureMessageLength = 320;

        private static readonly string[] FamilyNames = { "Noto Sans Thai", "NotoSansThai" };

        private static Font _font;
        private static string _registeredPath;
        private static bool _loadAttempted;
        private static bool _processExitHooked;
        private static int _failureLogsLeft = MaxFailureLogs;

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true,
            ExactSpelling = true)]
        private static extern int AddFontResourceExW(string path, uint flags, IntPtr reserved);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveFontResourceExW(string path, uint flags, IntPtr reserved);

        internal static Font Load(string pluginDirectory)
        {
            if (_font != null) return _font;
            if (_loadAttempted) return null;
            _loadAttempted = true;

            try
            {
                if (string.IsNullOrEmpty(pluginDirectory))
                {
                    LogWarning("plugin directory is unavailable; adjusted Thai font was not loaded");
                    return null;
                }

                var path = Path.GetFullPath(Path.Combine(pluginDirectory, FontFileName));
                if (!File.Exists(path))
                {
                    LogWarning("adjusted Thai font is missing: " + path);
                    return null;
                }

                var added = AddFontResourceExW(path, FR_PRIVATE, IntPtr.Zero);
                if (added <= 0)
                {
                    LogError("private font registration failed (Win32 " +
                             Marshal.GetLastWin32Error() + ")");
                    return null;
                }

                _registeredPath = path;
                KeepRegistrationForProcessLifetime();

                for (int i = 0; i < FamilyNames.Length; i++)
                {
                    var family = FamilyNames[i];
                    Font candidate = null;
                    try
                    {
                        candidate = Font.CreateDynamicFontFromOSFont(family, FontSize);
                        if (candidate == null ||
                            !candidate.HasCharacter('\uF70A') ||
                            !candidate.HasCharacter('\u0E49'))
                        {
                            if (candidate != null) UnityEngine.Object.Destroy(candidate);
                            continue;
                        }

                        UnityEngine.Object.DontDestroyOnLoad(candidate);
                        _font = candidate;
                        LogInitialized(family);
                        return _font;
                    }
                    catch (Exception exception)
                    {
                        if (candidate != null) UnityEngine.Object.Destroy(candidate);
                        LogWarning("Unity rejected adjusted Thai font family '" + family +
                                   "': " + exception.Message);
                    }
                }

                LogError("private font registered, but Unity found no adjusted Thai family " +
                         "with U+F70A and U+0E49");
            }
            catch (Exception exception)
            {
                LogError("adjusted Thai font initialization failed: " + exception.Message);
            }

            return null;
        }

        private static void KeepRegistrationForProcessLifetime()
        {
            if (_processExitHooked) return;
            try
            {
                AppDomain.CurrentDomain.ProcessExit += RemoveRegistrationAtProcessExit;
                _processExitHooked = true;
            }
            catch (Exception exception)
            {
                LogWarning("could not attach process-exit font cleanup: " + exception.Message);
            }
        }

        private static void RemoveRegistrationAtProcessExit(object sender, EventArgs eventArgs)
        {
            var path = _registeredPath;
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                RemoveFontResourceExW(path, FR_PRIVATE, IntPtr.Zero);
            }
            catch
            {
                // Windows releases process-private font resources when the process exits.
            }
        }

        private static void LogInitialized(string family)
        {
            try
            {
                Plugin.Log.LogInfo("[thai-shape] initialized adjusted Thai font '" +
                                   family + "' from " + FontFileName);
            }
            catch { }
        }

        private static void LogWarning(string message)
        {
            if (!TakeFailureLog()) return;
            try { Plugin.Log.LogWarning("[thai-shape] " + Bound(message)); }
            catch { }
        }

        private static void LogError(string message)
        {
            if (!TakeFailureLog()) return;
            try { Plugin.Log.LogError("[thai-shape] " + Bound(message)); }
            catch { }
        }

        private static bool TakeFailureLog()
        {
            if (_failureLogsLeft <= 0) return false;
            _failureLogsLeft--;
            return true;
        }

        private static string Bound(string message)
        {
            if (string.IsNullOrEmpty(message)) return "unknown font loader failure";
            return message.Length <= MaxFailureMessageLength
                ? message
                : message.Substring(0, MaxFailureMessageLength);
        }
    }
}
