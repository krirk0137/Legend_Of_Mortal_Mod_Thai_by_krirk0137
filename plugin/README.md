# LomThaiText — key-based translation injector (Solution 2)

A ~200-line BepInEx 5 plugin that writes Thai directly into the game's own
localization table, **keyed by the game's key** instead of by the Chinese text.

This is the architecture that removes the three hard ceilings of the
XUnity.AutoTranslator dictionary (`Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt`):

| ceiling | why the dictionary can't | why this can |
|---|---|---|
| 12,761 keys share their text with another key (17%) | one text → one translation, forever | each key is translated independently |
| single-character values (`唐`, `修`, `出`) | `唐=ถัง` partial-substitutes into `三师兄`; 97 entries had to be commented out in v1.2 | no text matching happens at all |
| `.NET` format templates (`近战伤害{0:N0}`) | infinite rendered variants | one key covers every number |

It does **not** replace the dictionary — both run at once. The dictionary keeps
handling everything it already handles; this handles what it cannot reach.

## How it works

`Mortal.Core.LeanLocalizationResolver` proves every game string funnels through one call:

```csharp
public string GetString(string key) => LeanLocalization.GetTranslationText(key);
```

which reads `LeanLocalization.CurrentTranslations[key].Data` — a **public static**.
`UpdateTranslations()` wipes that table, calls `RegisterAndBuild()` on every
`LeanLocalization` instance (compiling the game's 130 `LeanLanguageCSV` sources),
and only *then* fires `OnLocalizationChanged`, which is what refreshes live UI text.

So the plugin puts a **Harmony postfix on `LeanLocalization.RegisterAndBuild`** — after
the game has written all of its own values, before anything reads them. That is
deterministic; relying on `LeanSource` registration order is not (a plugin registers
*before* the scene's own sources, so the game would win). A `LeanSource` subclass is
registered too, purely as a redundant second path.

No asset patching. No `Unity.Addressables.dll` patch. No DLL is modified.
Remove the folder and the game is byte-for-byte stock.

## Files

```
plugin/
  install.ps1                 build + copy into the live game (-Tsv <file>, -Uninstall)
  LomThaiText/
    LomThaiText.csproj        net472; references the game's Managed DLLs + BepInEx core
    Plugin.cs                 config, loading, the RegisterAndBuild patch, hotkeys
    Fonts.cs                  Thai font install + the per-frame component sweep
    Resizer.cs                UI.resizer.txt rules
    Unruled.cs                labels with no sizing rule — the sizing worklist
    Leaks.cs                  untranslated text found while playing — the translation worklist
    Probe.cs                  one-off forensics
    poc/Thai.tsv              the proof-of-concept probe set
```

Installed layout: `BepInEx/plugins/LomThaiText/{LomThaiText.dll, Thai.tsv}`.

## Thai.tsv format

`key<TAB>value`, UTF-8 **no BOM**, LF. `#` starts a comment line.
Escapes inside the value: `\n`, `\t`, `\\`. A literal TAB in a value is impossible —
use `\t`. (Same TSV hazard that truncated a key in v1.6.)

## Hotkeys (configurable in `BepInEx/config/com.krirk0137.lomthaitext.cfg`)

| key | effect |
|---|---|
| **F8** | write the untranslated-text report now (it also autosaves every 5s) |
| **F10** | re-read `Thai.tsv` and re-apply, without restarting the game |
| **F11** | dump the game's entire **live** key→text table to `BepInEx/LomThaiText_dump.tsv` |

The F11 dump is the ground truth for building the full file — it is what the game
actually has in memory, not what a ripped asset says it should have.

## The leak report — "what is still Chinese", found by playing

`Leaks.cs` records untranslated text *as you play*, so finding the remaining gaps stops
being a matter of walking the game and squinting at it. Two files under `BepInEx/`:

| file | what it holds | how to act on it |
|---|---|---|
| `LomThaiText_missed_keys.tsv` | `key ⇥ hits ⇥ chinese` — every key the game asked for whose value is still Chinese, **sorted by hit count** | paste `key ⇥ thai` into `Thai.tsv`, press Ctrl+F10, done |
| `LomThaiText_missed_screen.tsv` | `chinese ⇥ scene path` — Chinese visible on screen that never came through the key table | no key exists: string literals compiled into the game, or text composed at runtime. Needs a code patch or an XUnity dictionary line |

The first file is the one that matters. Because the key net sits on
`LeanLocalization.GetTranslationText`, it reports the **key** rather than the rendered
text, and the hit count orders the worklist by what the player actually meets most —
so the next hour of translating is spent where it shows.

The second file is the honest accounting of what a key-based injector structurally
cannot reach. Anything landing there is a JOB-4-class problem, not a missing `Thai.tsv` line.

Both are deduped, capped (`Debug/TrackUntranslatedMax`, default 20 000), and can be turned
off with `Debug/TrackUntranslated`. The key net is patched separately from the injector and
its failure is swallowed — a diagnostic must never be able to take down the working
translation. If `GetTranslationText` ever moves, the log says so and the screen net carries on.

Borrowed from **ENMod Continued** (the 觅长生 Thai mod), which keeps a `MissedStrings.txt`
the same way. Its version scans rendered text only; resolving the key is what makes the
output paste-able.

## Build

Needs the .NET SDK and an installed copy of the game (the reference DLLs are read
straight out of it).

```powershell
.\plugin\install.ps1                                  # default Steam path
.\plugin\install.ps1 -GameDir 'D:\Games\LegendOfMortal'
.\plugin\install.ps1 -Uninstall
```

## Status — ✅ PoC PROVEN in game (2026-07-21)

`poc/Thai.tsv` injects 11 probe keys chosen so that *nothing* in it could have come from
the dictionary. Confirmed on screen:

- the four title-screen buttons read Thai (`เริ่ม`, `หอธรรม`, `ลาจาก`)
- the Load Game panel reads **`ปีที่ 1 ต้นเดือนเมษายน`** — that is
  `System/GameTimeText = 第{0}年{1}{2}`, a `.NET` format template with unbounded
  rendered variants. One key covers every date in the game; the text-keyed dictionary
  could never match it.

`LomThaiText_dump.tsv` (72,560 live keys) is written on first launch — the game's own
table straight out of memory, and the ground truth for building the full file.

### The four dead ends this cost, so nobody repeats them

1. **Blank Thai is not always a font problem.** Legacy UGUI `Text` uses *dynamic* fonts,
   and Unity sources missing glyphs from the OS font fallback — `hasThaiGlyph=True` even
   on the Chinese brush font `DFT_S5`. Thai renders game-wide with `OverrideFont=` empty.
2. **It was the box.** A title label is `box=150x45` with `hOver=Wrap vOver=Truncate`,
   sized for two Chinese glyphs. The `[S2] ` marker added for visibility filled line one,
   the Thai wrapped to line two, and line two was clipped. The marker *was* the bug.
3. **The plugin's own `Update()` never runs in this game.** Anything time-based must live
   on a separate `DontDestroyOnLoad` object (`Ticker`), re-created from `Apply()`. Symptom:
   `Instance != null` returns false via Unity's fake-null, so config reads silently
   default. Never Unity-null-check the plugin component; capture config into plain statics.
4. **The TMP font asset is not named after its bundle.** `FallbackFontTextMeshPro=Kanit_sdf`
   loads an asset actually named `Kanit-Regular SDF`. Match on a fragment, not the filename.

### Sizing, for the full rollout

Title labels already have `bestFit=True`, but `resizeTextMinSize` is too high for long
Thai, so Unity wraps and clips instead of shrinking. Lowering the min size on labels the
plugin feeds handles the whole game at once; `Mod/BepInEx/Translation/th/Text/UI.resizer.txt`
remains available for per-path exceptions.
