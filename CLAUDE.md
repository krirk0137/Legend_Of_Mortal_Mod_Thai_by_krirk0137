# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

An **unofficial translation mod** for the Unity game **Legend of Mortal (活侠传 / 活俠傳)**. It contains **no game source code** — it is a runtime translation layer built on:

- **BepInEx** (mod loader / doorstop) — injected via `winhttp.dll`
- **XUnity.AutoTranslator** — hooks in-game text at runtime and replaces it using a translation dictionary, falling back to Google Translate for anything not in the dictionary
- **XUnity.ResourceRedirector** — swaps translated **textures** (character-name images, etc.)

The upstream project translates the game **zh-CN → English**. This repo is being extended to add **Thai**. **Locked strategy (supersedes the earlier "English-primary + Thai-accent" idea): translate Chinese-primary** — the CN key is the source of truth, the English value is reference-only. Everything becomes Thai (menus, dialogue, names): wuxia register (ข้า/เจ้า/ท่าน — never ฉัน/เธอ/คุณ/ผม/ดิฉัน), proper names transliterated to Thai script per `tools/glossary.tsv`. See `NEXT_STEPS.md` for the per-chapter pipeline, per-story status, and all learned failure modes.

**⚠️ Register is GENDERED (v1.3 fix — do not flatten):** acknowledgment particle = MALE `ขอรับ` / FEMALE `เจ้าค่ะ` (the period-correct female form — NOT the banned modern bare `ค่ะ`). Humble self-reference = MALE `ข้าน้อย` / FEMALE plain `ข้า` (**women never say `ข้าน้อย`**). `เจ้า`/`ท่าน`/`ข้า` are gender-neutral. Identify female speakers by: CN source pronoun in the key (`妾身`/`奴家`/`小女子`/`本宫` = female; `在下`/`小人`/`小的` = male), and the female cast in `tools/glossary.tsv`. So game-wide `ครับ`=0 and bare-`ค่ะ`=0, but `เจ้าค่ะ`>0 is CORRECT.

There is **no build / lint / test step** — the deliverable is data files copied into the game folder.

## Layout that matters

Everything lives under `Mod/`, which is what a player copies into their Steam game directory:

- `BepInEx/config/AutoTranslatorConfig.ini` — the control panel. Key settings: `FromLanguage=zh-CN`, `Language=en`, font overrides, `EnableFairyGUI/UGUI/TextMeshPro`.
- `BepInEx/Translation/th/Text/Translation zh-CN to en.txt` — **the main dictionary, ~85k lines / 15 MB.** This is where 99% of translation work happens.
- `BepInEx/Translation/th/Text/UI.resizer.txt` — per-UI-path font-size / resize commands (`ChangeFontSizeByPercentage(...)`), used when translated text overflows its box.
- `BepInEx/Translation/th/Text/regex.txt`, `_Substitutions.txt`, `_Pre/_Postprocessors.txt` — regex + substitution passes.
- `BepInEx/Translation/th/Texture/*.png` — translated image assets, filenames encode the original texture hash.
- `tools/apply_thai.pl` — reusable batch translator (see below).

## Dictionary file format — critical rules

Lines are `SOURCE=VALUE`. Lines starting with `//` are comments; `////-------------Name////-------------` are **section headers** (Characters, System, Stat, Items, UI Changes, Story 01–17, …). The file is roughly: systems/menus (top ~6k lines), `UI Changes` (~6k–18k), then `Story 01–17` (~18k–85k, ~80% of the file, the narrative bulk).

- The **key (left of `=`) is the original Chinese and must never be altered** — XUnity matches it exactly against in-game text. Only edit the **value**.
- Both **Simplified and Traditional** variants of a string often appear as separate keys (e.g. `小师妹=` and `小師妹=`). The game is launched in **Simplified Chinese**, so only **simplified keys are actually triggered** — prioritize those.
- File encoding is **UTF-8, no BOM, LF line endings**. Preserve it. **Do NOT edit with tools that inject a BOM or CRLF** (e.g. PowerShell `Set-Content -Encoding utf8` adds a BOM and can break parsing). Use the perl tooling or byte-safe edits.
- **Hot-reload is ON** (`ReloadTranslationsOnFileChange=True`, set 2026-07-20): edits to the dictionary are picked up **without restarting the game**. (If it ever misbehaves, set it back to `False` — then changes need a restart.)

## Batch-translating values

`tools/apply_thai.pl` rewrites values keyed by the Chinese source, matching whole lines only (`^KEY=...`) so short keys never partial-match longer lines:

```
perl tools/apply_thai.pl "<path to Translation zh-CN to en.txt>" pairs.tsv
```

`pairs.tsv` is `chinese_key<TAB>thai_value` per line (`#` comments and blank lines ignored). It reports hit counts per key. Always work against a backup — the original English is preserved at `Translation zh-CN to en.txt.EN-BACKUP`.

**Data-integrity hazard (learned the hard way):** `apply_thai.pl` rewrites by regex `s/^\Q$k\E=[^\n]*/…/mg`. A malformed pair-row whose key is the fragment `<size\` (a TAB accidentally replacing the `=` inside `<size\=24>`) makes the pattern collapse to `^<size\=` and **carpet-overwrite every line starting with `<size\=`** — this silently destroyed 682 lines once. `apply_thai.pl` now **skips any key ending in `\`** as a guard, but still verify after every apply. Companion tools in `tools/`:
- `keydiff.pl "<file>.EN-BACKUP" "<file>"` — reports lines whose key drifted from the pristine backup. **Must print 0** after any apply (proves no carpet-overwrite). The file is line-aligned with `.EN-BACKUP`, so this is the definitive integrity check.
- `repair.pl "<file>.EN-BACKUP" "<file>" out.txt` — line-restores corrupted keys from `.EN-BACKUP`.
- `fixsize.pl in_gold.tsv out.tsv` — reconstructs `<size\`+TAB fragment rows a translation worker sometimes emits, back into proper `CN<TAB>THAI`.
Verify the *applied file* (keydiff + a no-Thai-value scan over the chapter's line range), not just the gold TSV — a clean TSV can still leave the file corrupted. Snapshot a `.POST-STORYNN` backup only AFTER those checks pass.

## Fonts (the make-or-break issue for Thai)

The game's default font has no Thai glyphs, so Thai renders as tofu (□) unless a font is supplied. Two mechanisms in `AutoTranslatorConfig.ini`:

- `OverrideFont=<OS font name>` — **UGUI text only**, uses a Windows font directly. **`OverrideFont=Tahoma` is the chosen, in-game-confirmed font** (renders Thai incl. stacked tone marks better than Leelawadee UI, which also works) — no asset bundle needed. Pairs with `ResizeUILineSpacingScale=1.2`.
- `FallbackFontTextMeshPro=<path>` — for **TextMeshPro** text; needs a TMP font-asset **AssetBundle built for the game's exact Unity version**. Only pursue this if some text turns out to be TMP/FairyGUI and stays tofu under `OverrideFont`.

`EnableConsole=True` under `[Debug]` opens a diagnostic console; logs also go to `BepInEx/LogOutput.log`.

## Testing a change

There is no automated test. Verification = run the game: copy the edited `Translation zh-CN to en.txt` and `AutoTranslatorConfig.ini` into the installed game folder (or the whole `Mod/` folder), launch, pick the 2nd title-screen option → 2nd dropdown option (Simplified Chinese), and eyeball the affected UI. Watch for text overflow (fix via `UI.resizer.txt`) and tofu glyphs (font issue).

## Reference

See `README.md` (install steps, game version `1.0.5000.13`) and `THAI_FONT_TEST_README.md` (current Thai pilot: what was changed and how to test/revert).
