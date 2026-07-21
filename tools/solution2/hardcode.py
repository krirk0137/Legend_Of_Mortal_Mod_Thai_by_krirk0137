import os, re, sys, io, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
ROOT = r"C:/ClaudeCode/LegendOfMortalENG/AssetRipper_Solution2/LegendOfMortal/Scripts"
DIRS = ["Mortal.Core", "Mortal.Story", "Mortal.Combat", "Mortal.Battle",
        "Mortal.Free", "Assembly-CSharp", "Fungus"]
CJK = re.compile('[一-鿿]')
LIT = re.compile(r'"((?:[^"\\]|\\.)*)"')
SKIP = ("[Label(", "CreateAssetMenu", "AddComponentMenu", "HelpURL", "[Tooltip(", "[Header(")
hits = collections.Counter()
byfile = collections.Counter()
for d in DIRS:
    p = os.path.join(ROOT, d)
    if not os.path.isdir(p):
        continue
    for r, _, fs in os.walk(p):
        for f in fs:
            if not f.endswith(".cs"):
                continue
            for line in open(os.path.join(r, f), encoding="utf-8", errors="replace"):
                if any(s in line for s in SKIP):
                    continue
                for m in LIT.finditer(line):
                    s = m.group(1)
                    if CJK.search(s):
                        hits[s] += 1
                        byfile[d + "/" + f] += 1
print("distinct hardcoded CJK literals:", len(hits), " occurrences:", sum(hits.values()))
print("\nfiles:")
for f, c in byfile.most_common(12):
    print(f"  {c:>4} {f}")
print("\nstrings:")
for s, c in hits.most_common(60):
    print(f"  x{c} {s[:70]!r}")
