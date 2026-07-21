"""JOB 4 — gendered register per KEY, not per text.

v1.5 (JOB 3) could only fix 1,112 of 1,405 dictionary lines carrying male register: a line
like `是。` is spoken by up to 48 different characters, and a text-keyed dictionary forces
them all to share one Thai rendering. Key-based injection removes that ceiling — every
`Story/<id>` has exactly one speaker, taken from the game's own Lua:

    setcharacter(characters.Get("girl9"), …)
    characters.Focus("girl9")
    say(luamanager.GetStoryText("D_1_1_011"))

So this reads the ripped story scripts directly (no UnityPy needed any more — AssetRipper
put them in Assets/Resources/story/), maps key -> speaker -> gender, and rewrites the
male-only forms on female speakers' lines.

    MALE                FEMALE
    ขอรับ                เจ้าค่ะ      acknowledgment particle
    ข้าน้อย               ข้า          humble self-reference (women never say ข้าน้อย)

`เจ้า`/`ท่าน`/`ข้า` are gender-neutral. Bare modern `ค่ะ` stays banned.

    python tools/solution2/s2_gender.py [--write]
"""
import sys, io, os, re, json, collections

if __name__ == "__main__":   # re-wrapping on import would close the importer's stdout
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))

LUA = os.path.join(ROOT, "AssetRipper_Solution2", "LegendOfMortal", "Assets", "Resources",
                   "story", "chinesetraditional")
GENDER = os.path.join(ROOT, "tools", "speaker_gender.tsv")
MERGED = os.path.join(ROOT, "plugin", "LomThaiText", "full", "Thai.merged.tsv")
OUT = os.path.join(HERE, "gender_fix.tsv")

# Same scan as tools/job3/speakers.py: the speaker is whatever setcharacter() last named,
# and a non-`character` say-dialog (narration) clears it.
SCAN = re.compile(r'setcharacter\(\s*characters\.Get\("([^"]+)"\)'
                  r'|setsaydialog\(saydialogs\.(\w+)\)'
                  r'|GetStoryText\("([^"]+)"\)')


def key_to_speaker():
    """Story key -> the single tag that says it (or None where it is shared/narrated)."""
    owners = collections.defaultdict(set)
    lines = 0
    for fn in sorted(os.listdir(LUA)):
        txt = open(os.path.join(LUA, fn), encoding="utf-8", errors="replace").read()
        cur = None
        for m in SCAN.finditer(txt):
            if m.group(1):
                cur = m.group(1)
            elif m.group(2):
                if m.group(2) != "character":
                    cur = None
            else:
                lines += 1
                owners["Story/" + m.group(3)].add(cur)
    print(f"say-lines: {lines}   distinct story keys: {len(owners)}")
    single = {k: next(iter(v)) for k, v in owners.items() if len(v) == 1 and next(iter(v))}
    print(f"keys with exactly one named speaker: {len(single)}")
    return owners, single


def load_gender():
    """v1.5's curated set, plus the 29 speakers it missed (female_extra.tsv keeps the evidence)."""
    f = {}
    for line in open(GENDER, encoding="utf-8"):
        if line.startswith("#"):
            continue
        p = line.rstrip("\n").split("\t")
        if len(p) >= 4:
            f[p[0]] = p[3]
    extra = 0
    path = os.path.join(HERE, "female_extra.tsv")
    if os.path.exists(path):
        for line in open(path, encoding="utf-8"):
            if line.startswith("#") or "\t" not in line:
                continue
            tag = line.split("\t")[0].strip()
            if tag and f.get(tag) != "F":
                f[tag] = "F"
                extra += 1
    print(f"female set: {sum(1 for g in f.values() if g == 'F')} "
          f"({extra} added from female_extra.tsv)")
    return f


def read_tsv(path):
    rows = {}
    for line in open(path, encoding="utf-8"):
        line = line.rstrip("\n")
        if not line or line.startswith("#") or "\t" not in line:
            continue
        k, v = line.split("\t", 1)
        rows[k] = v
    return rows


def main():
    owners, single = key_to_speaker()
    gender = load_gender()
    thai = read_tsv(MERGED)

    male_keys = [k for k, v in thai.items()
                 if k.startswith("Story/") and ("ขอรับ" in v or "ข้าน้อย" in v)]
    print(f"\nStory keys whose Thai carries MALE register: {len(male_keys)}")
    print("  (v1.5 could only reach 1,112 dictionary LINES; the ceiling was shared text)")

    c = collections.Counter()
    fixes = {}
    for k in male_keys:
        sp = single.get(k)
        if sp is None:
            c["no single speaker (narration / shared)"] += 1
            continue
        g = gender.get(sp)
        if g != "F":
            c["male or unknown speaker — left alone"] += 1
            continue
        v = thai[k]
        nv = v.replace("ขอรับ", "เจ้าค่ะ").replace("ข้าน้อย", "ข้า")
        if nv != v:
            fixes[k] = nv
            c["FEMALE -> rewritten"] += 1

    # The opposite error v1.5 had to undo: female forms on a male speaker.
    wrong = {}
    for k, v in thai.items():
        if not k.startswith("Story/") or "เจ้าค่ะ" not in v:
            continue
        sp = single.get(k)
        if sp and gender.get(sp) != "F":
            wrong[k] = v.replace("เจ้าค่ะ", "ขอรับ")
    if wrong:
        print(f"\nfemale particle on a MALE speaker (the v1.5 赵活 bug class): {len(wrong)}")
        fixes.update(wrong)
        c["MALE -> reverted"] += len(wrong)

    for x, n in c.most_common():
        print(f"  {x:<40}{n:>7}")

    by_speaker = collections.Counter(single[k] for k in fixes if k in single)
    print("\ntop speakers fixed:", dict(by_speaker.most_common(8)))

    if "--write" in sys.argv:
        with open(OUT, "w", encoding="utf-8", newline="\n") as f:
            f.write("# JOB 4 — per-key gendered register, generated by s2_gender.py\n")
            f.write(f"# {len(fixes)} keys\n")
            for k in sorted(fixes):
                f.write(k + "\t" + fixes[k] + "\n")
        print(f"\nwrote {OUT}  ({len(fixes)} keys)")
    else:
        print("\n(dry run — pass --write to emit gender_fix.tsv)")
        for k in list(fixes)[:6]:
            print(f"  {k}  [{single.get(k)}]\n     {thai[k][:70]}\n  -> {fixes[k][:70]}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
