# v1.6 — gap-fill from the game's own key set (2026-07-21)

Worklist source: `tools/solution2/` (the AssetRipper dump gave us the game's complete
localization table, so for the first time the gap list is **exhaustive**, not sampled).

## ⚠️ The bug that made this pass tricky — read before touching any dictionary tooling

**An XUnity dictionary line splits at the first *UNESCAPED* `=`.** A naive `line.find("=")`
is wrong for every key that contains an escaped `=`:

```
<size\=24>text</size>=<size\=24>ไทย</size>
        ^ naive split lands here -> key becomes "<size\"
```

The first coverage pass used the naive split, so all 683 existing `<size\=NN>` lines
registered under one bogus key `<size\`, and **~700 already-translated lines were reported
as missing**. 420 of the 977 translated strings turned out to be duplicates of entries that
already existed. Caught by a duplicate-key check before sync; rolled back and re-applied.

`xkey.py` has the correct `split()` / `load()` — **use it, never `line.find("=")`.**
This also corrects the headline number: real coverage before v1.6 was **97.22%**, not the
96.4% first reported in `tools/solution2/README.md`.

## Second gotcha: a TAB inside a source string

`Story/T_2_4_2_006` contains a literal TAB. The whole worker pipeline is TSV, so the tab
split the key mid-sentence and a **truncated key** reached the dictionary. Removed by hand.
`v16_build.py` should reject or escape tabs; check `awk -F'\t' 'NF!=3'` on the input file.

## Pipeline

```
v16_build.py    # gaps -> dict-key form + speaker/gender hint   -> v16_input.tsv
v16_chunk.py    # split into worker chunks (~12 KB source each) -> v16/*.tsv
                # ... run one Agent per chunk ...
v16_verify.py   # key fidelity, tag-structure match, banned register, BOM/CRLF
v16_gender.py   # cross-check the register against the speaker map
v16_clash.py    # which "new" keys already exist (needs the correct parser!)
v16_apply2.py   # 3 in-place fixes + append only genuinely-new keys
v16_postcheck.py# prefix integrity vs .PRE-V16 + duplicate keys
cov3.py         # coverage against the game's full key set (correct parser)
glb.py          # read a ripped .glb scene hierarchy -> UI object paths
```

### Key-form rules (`v16_build.py: to_dict_key`)

| game text | dictionary key |
|---|---|
| `{size=24}X{/size}` | `<size\=24>X</size>` |
| `{color=#FF0000}X{/color}` | `<color\=#FF0000>X</color>` |
| `{punch=..}` `{w}` `{wi}` … | removed — Fungus consumes them, they never render |
| literal `\n` | **deleted** (XUnity's whitespace-insensitive lookup makes this match) |
| any remaining `=` | escaped to `\=` |
| `{$var}` / `{title}` / `{/30}` | skipped — can never match a static key |

## What shipped

- **288 DiceHeader captions** — the heading above *every* choice in the game.
  389 of 398 keys were missing; the whole namespace now reads Thai. Translated as one
  chunk so `X的想法`/`X的决定`/`X的反应` render consistently game-wide.
- **267 story / misc** strings, incl. `解锁条件：银两>\=500`-style unlock conditions,
  `<size\=50>` battle shouts, and the `CombatInfo` skill explanations.
- **3 in-place fixes**: 2 leftover English lines, and `<color=#FF7979>` → `<color\=#FF7979>`
  (that key never parsed, so the entry had never worked).
- **`GameLogTextPaths=/[UI]/MainUI/Layer_2/GameLog/Viewport/Content`** — closes JOB 2-A.
  Path read from the ripped `Combat.glb` hierarchy and confirmed against a `Path :` line in
  the live `LogOutput.log`. `CombatUIManager.AddLog` does `logText.text += log`, which is
  exactly the append-log case this setting exists for.

Coverage **97.22% → 97.95%**. DiceHeader 2% → **100%**.

## Register came out right the first time

Every worker chunk carried a `SPEAKER (หญิง/ชาย?)` column derived from `tools/speaker_map.tsv`
(JOB 3). `v16_gender.py` found **1** violation in 977 lines — 南宫浅 (male) given `เจ้าค่ะ`,
the same character v1.5 had to fix retroactively. Fixed before apply.
v1.3 and v1.5 each needed a retroactive sweep; v1.6 needed none.

## The 137 keys still missing (all deliberate)

98 single-CJK-character values · 5 `.NET` format templates · 3 runtime variables/tokens ·
31 that `cov3.py` only *reports* as missing because its variant generator does not reproduce
every escaping rule (`v16_spot.py` confirms each of those classes is present in the file).
The first two classes are the ones only key-based injection can fix — see `tools/solution2/`.
