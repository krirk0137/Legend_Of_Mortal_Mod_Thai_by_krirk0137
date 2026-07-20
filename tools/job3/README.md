# JOB 3 — speaker attribution pipeline (SOLVED 2026-07-20)

The blocker for gendered register was "the dict is a flat `key=value` store with no speaker
attribution". **That is no longer true.** The game ships its story as **Lua scripts** and its text
as a **LeanLocalization key/value table**, and the Lua binds them together:

```lua
setcharacter(characters.Get("girl9"), characters.GetPortrait("girl9", "nervous1"))
characters.Focus("girl9")
say(luamanager.GetStoryText("D_1_1_011"))
```

So `speaker tag → story ID → Chinese text → our dict key` is a complete, deterministic chain.

## Where the data lives (game install, no mod files involved)

| what | where |
|---|---|
| story Lua scripts (1,631 with `GetStoryText`) | `Mortal_Data/resources.assets` → TextAssets |
| text table (72,539 keys, `Story/D_1_1_011=…`) | `Mortal_Data/level0` → 130 `LeanLanguageCSV` MonoBehaviours (zh-CN / zh-TW / ko) |
| character tag → display name | `Mortal_Data/sharedassets2.assets` → `StoryMappingItem` (508) |
| character → portrait folder (`Girl9_上官瑩`) | same file → `StoryCharacterData` (429) |
| tag → Simplified name | text table key `Character/<tag>` |

Requires `pip install UnityPy`. MonoBehaviour typetrees are **stripped**, so the scripts hand-parse
the raw blobs (m_Name length prefix sits at offset 28; strings are u32-length + UTF-8 + 4-byte align).

## Run order

```
lua2.py       # dump the 1,631 story .lua from resources.assets -> lua/
speakers.py   # parse lua -> say_rows.json   (60,969 attributed say-lines, 343 speakers)
leanall.py    # level0 -> lean_tables.json
ns.py         # -> cs_kv.json  (ChineseSimplified key -> text)
chars2.py     # -> charmap.json (tag -> display name, portrait folder)
join4.py      # join to the dict -> line2sp.json (55,192 dict lines get a speaker)
gender.py     # -> gender_signals.json (portrait folder + name morpheme + 她/他 narration counts)
cand.py / prop.py / apply_job3.py / rev.py     # the v1.5 register fix
export.py     # -> tools/speaker_map.tsv + tools/speaker_gender.tsv
```

## Results (2026-07-20)

- **60,969** say-lines carry a speaker; 100% of their story IDs resolve to Chinese text.
- **60,247 / 60,969 (98.8%)** match a dict key. The 722 misses are `{size=24}` markup, which the
  dict stores as `<size\=24>`.
- **55,192 dict lines** now have speaker attribution → `tools/speaker_map.tsv`.
- Of the 1,405 dict lines carrying male register (`ขอรับ` / `ข้าน้อย`), **1,112 have exactly one
  speaker** (the rest are generic lines like `是。` shared by up to 48 characters — those are
  inherently un-genderable and must stay male).

## Gender determination (no guessing)

`tools/speaker_gender.tsv`. A speaker is marked **F** only when the evidence is unambiguous:
portrait folder prefix (`Girl*`, `TraineeGirl*`, `Female*`, `Women*`, `Sister`, `Fairy`), a female
morpheme in the name (`娘子` `夫人` `婦人` `女弟子` `姑娘` …), female self-reference in their own
lines (`妾身` `奴家` `小女子` `本宫`), or an ≥80% 她-vs-他 ratio in narration mentioning them.
Everything else stays **M?** (male, the safe default) — same discipline as JOB 1 Stage B.

Resolved by hand from `CharacterIntro` bios where signals conflicted:
`special999 瑞笙` = **male** (「英俊潇洒，磊落君子」), `special834 段智秀` = **male**,
`special103 南宫浅` = **male** (南宫世家的庶子), `girl1 瑞杏` = **female** (`本宫` ×29),
`special813 画中仙` = **female** (`奴家`), `girl8 魏菊` = **female** (`小女子`, 嫡传女弟子).

## `ขอรับ` polysemy guard

`ขอรับ` is both the MALE sentence particle and the verb "to receive". The fix only converts it when
the next character is **not** a Thai letter (i.e. it is sentence-final / followed by punctuation).
That correctly kept 8 verb uses (`ขอรับใช้`, `ขอรับคำสั่ง`, `ขอรับไว้`, `ขอรับน้ำ`, `ขอรับฟัง`, `ขอรับการ`…).

## Known non-obvious finding

`在下` in the Chinese key is **not** a male-only marker — 龙湘 (girl4, female) uses it
(`在下锦香宫龙湘，请陈公子赐教吧。`). JOB 1 treated `在下` as male and left those lines alone;
speaker attribution overrides that and is the stronger signal.

## Left alone on purpose

- `special4 樊啸天` (male, 丐帮「狂犬」) has 2 `เจ้าค่ะ` lines — one of them literally reads
  `俺……人家是女生` ("I'm a girl"), i.e. he is putting on a girl's voice. Comedic; ambiguous either way.
- `trainee231 玄功弟子` and `trainee49x 锦香弟子` — both sects are female-led (魏菊 / 龙湘) but the
  generic disciple tags carry no gender signal, so their `เจ้าค่ะ` lines were not touched.
