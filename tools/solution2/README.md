# Solution 2 — assessment of the AssetRipper dump (2026-07-21)

Input: `AssetRipper_Solution2/LegendOfMortal/` (2.5 GB, gitignored — do not commit).
It contains three things that matter:

| | what it is |
|---|---|
| `Assets/TextAsset/*.bytes` | **the game's own localization source CSVs**, 133 files = 43 groups × 3 languages (zh-cn / zh-tw / kr) |
| `Scripts/` | **decompiled C#** of every game assembly (Mortal.Core / .Story / .Combat / .Battle, Fungus, LeanLocalization) |
| `Assets/__Project/Images` | source PNGs (character portraits etc.) |

**⚠️ The language is NOT encoded in the filename.** AssetRipper de-duplicated names, so
`Story_5_0.bytes` is zh-cn but `Story_10_0.bytes` is zh-tw and `Story_12_0.bytes` is Korean.
Classify by **content** — `s2lang.py` does this (Hangul detector + simplified/traditional
character sets). Getting this wrong silently mixes Korean into the table (it produced 13,347
bogus "conflicts" on the first pass).

## Scripts (run in this order)

```
s2lang.py    # classify each .bytes by language      -> s2_filelang.json
s2kv.py      # build the zh-cn key->text table       -> s2_kv.json   (72,883 keys)
cov2.py      # compare against our dictionary        -> s2_gaps.json
worklist.py  # bucket the gaps                       -> gap_worklist.tsv
collide.py   # same-text-different-key statistics
hardcode.py  # Chinese string literals left in C#
risk.py      # which gaps are unsafe to add as dict keys
```

`cov2.py` must map game text → our dict-key form: `{size=24}` → `<size\=24>` and the literal
`\n` escape is **stripped** (our dict keys have no newline marker at all). Without the strip,
23,521 real matches look like gaps.

## Result: our coverage is 96.4%

72,883 zh-cn keys. Matched to a Thai dictionary value: **70,253 (96.4%)**.
Non-Thai values: 23 dict lines, of which only **2** hold real Chinese (`笑话，不过区区庸材罢了。`
L22809, `那也是难能可贵了。` L24382) — the other 21 are punctuation-only (`……。` → `...`), fine as-is.

**1,277 keys / 1,099 distinct texts are genuinely missing.** Full list: `gap_worklist.tsv`.

| bucket | texts | what it is | fixable by dictionary? |
|---|---|---|---|
| `A-markup` | 644 | story lines wrapped in `{size=NN}` / `{color=}` / `{punch=}` | ✅ yes |
| `A-plain` ≥5 chars | 261 | ordinary prose + **DiceHeader** | ✅ yes |
| `A-plain` <5 chars | 108 | short choice headers (`结果`, `说服`, `逃脱`) | ⚠️ risky — see below |
| `B-format` | 9 | `.NET` format templates (`近战伤害{0:N0}  爆击率{1:P0}`) | ❌ key-only |
| `B-singlechar` | 77 | one-character values (`买` `你` `钱`; `上下左右`; vertical `唐/门/暗/器/总/纲`) | ❌ key-only |

### The single biggest visible gap: `DiceHeader`

**389 of 398 keys missing → 288 distinct texts.** This is the caption above *every* choice in
the game — `你的抉择` (×11), `由谁出战` (×9), `你的对手` (×8), `你的反应` (×14) … Only 7 of them
ever reached the Google fallback, so the rest render as raw Chinese. One worker chunk fixes it.

## The three things the dictionary architecture can never fix

### 1. One Chinese text, many different keys — 12,727 keys (17%) collide

`好。` is 214 separate keys, `是。` is 152, `……。` is 1,072 (spanning both `Story/` and
`CombatTalking/`). A dictionary is keyed by the **text**, so all 214 must share one Thai
rendering forever. This is also exactly why JOB 3 could only gender 1,112 of 1,405 lines.
Key-based injection gives each line its own translation.

### 2. Single-character values

`System/Number_1 = 一`, `BattleKey/Name/Up = 上`, `Character/chicken1 = 鸡`,
`BattleSkill/Brother1/Skill_1_* = 唐 门 暗 器 总 纲` (a vertically-stacked skill name).
We **commented out 97 of these in v1.2** because `唐=ถัง` partial-substituted into composite
strings (`三师兄` → `สาม师兄`). Under key-based injection there is no text matching at all,
so they become trivially safe. `risk.py` shows 72 more short texts in the current worklist
that sit inside other existing keys — same hazard class, same fix.

### 3. Format templates

`UpgradeItemRelationDesc/BattleAttack_001 = 近战伤害{0:N0}  爆击率{1:P0}` is precisely the
`近战伤害NN 爆击率N %` string `NEXT_STEPS.md` filed as "infinite variants → regex.txt only".
With the key, **one** translation covers every number. Same for `CombatInfo/poison3` etc.

## Injection: how Solution 2 would actually work

`Scripts/Mortal.Core/Mortal/Core/LeanLocalizationResolver.cs` proves **every** piece of game
text funnels through one call:

```csharp
public string GetString(string key) => LeanLocalization.GetTranslationText(key);
public string GetStoryText(string key) => GetString("Story/" + key);
```

and `LeanLocalization` exposes everything we need as **public statics**:

```csharp
public static Dictionary<string, LeanTranslation> CurrentTranslations;
public static LeanTranslation RegisterTranslation(string name);   // returns existing or new
public class LeanTranslation { public object Data; }              // public field
```

`LeanLocalization.RegisterAndBuild()` walks `LeanSource.Instances` **in order** and each
`LeanLanguageCSV.Compile()` does `translation.Data = entry.Text`. So **the last source
compiled wins**, and `LeanSource.Register()` does `Instances.AddLast(this)`.

**Therefore: a ~50-line BepInEx plugin containing one `LeanSource` subclass that reads a loose
`Thai.csv` and sets `RegisterTranslation(key).Data = thai` is sufficient.** It re-applies
automatically on every `UpdateTranslations()`, needs **no** asset-file patching, **no**
`Unity.Addressables.dll` patch (unlike [[josh-stringtable-spike]]), and the CSV stays
hand-editable. Fonts are unaffected — `FallbackFontTextMeshPro=Kanit_sdf` / `OverrideFont`
act at the TMP/UGUI component level regardless of where the text came from.

Requires: .NET build toolchain, Unity 2020.3.49f1 reference DLLs (present in `Assemblies/`),
BepInEx 5 + HarmonyX. Publish target `netstandard2.0`.

## What even Solution 2 cannot fix: the battle log

`hardcode.py` finds 175 distinct Chinese literals still hardcoded in C#. Most are debug-only,
but **45 are the combat log** in `Mortal.Combat/CombatManager.cs` — in **Traditional** Chinese,
with interpolated values, never passing through LeanLocalization:

```csharp
AddGameLog($"回合數: {_round.Value}");
AddGameLog("骰子總數: " + string.Join("+", dice.ResultList) + " = " + dice.Result);
AddGameLog($"{target.CharacterName}的血量 {totalDamage}");
```

and `CombatUIManager.AddLog` is:

```csharp
logText.text = logText.text + log + "\n";     // appends to the SAME Text component
```

**This confirms the JOB 2-A root cause exactly** — the log is an append-only component, so
AutoTranslator re-scrapes an ever-growing string and compounds the language mix each round.
No dictionary entry can ever match it. Two real fixes:

1. `[Behaviour] GameLogTextPaths` = that component's UI path (no plugin needed), **or**
2. a Harmony prefix on `CombatUIManager.AddLog(string)` that translates `log` before it is
   appended — deterministic, and the only way to get it 100% right.

Other player-visible hardcoded strings: `GameStatUtils` (零一二三四… Chinese numerals for
dates), `Book.cs` (`書物`/`雜物`/`寶物`), `SaveSlotTitle`, `StoryCharacterController`.

## Recommendation

**Do not switch architectures yet.** Take the free win first:

- **v1.6 (dictionary, zero risk):** translate the 905 safe texts from `gap_worklist.tsv` —
  DiceHeader is the headline item — plus the 2 English leftovers. Add `GameLogTextPaths`.
- **v2.0 (Solution 2, only if you want the last 3%):** the LeanSource plugin, which unlocks
  the 108 short texts + 77 single chars + 9 templates + the 97 entries commented out in v1.2,
  and removes the 17% collision ceiling — which is also what would let JOB 3's register fix
  go from 1,112 gender-able lines to all 63,670.
