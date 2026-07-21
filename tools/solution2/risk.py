import json, sys, io, collections, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
DICT = r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
rows = [l.rstrip("\n").split("\t") for l in
        open(r"C:/ClaudeCode/LegendOfMortalENG/tools/solution2/gap_worklist.tsv",
             encoding="utf-8").read().split("\n")[1:] if l.strip()]
lines = open(DICT, encoding="utf-8").read().split("\n")
keys = [l[:l.find("=")] for l in lines if l and not l.startswith("//") and l.find("=") > 0]
plain = [r for r in rows if r[0] == "A-plain"]
print("A-plain distinct texts:", len(plain))
byl = collections.Counter(len(r[3]) for r in plain)
print("length histogram:", sorted(byl.items())[:12])
risky = []
for r in plain:
    t = r[3]
    if len(t) <= 4:
        n = sum(1 for k in keys if t in k and k != t)
        if n:
            risky.append((t, n, r[2]))
risky.sort(key=lambda x: -x[1])
print(f"\nshort texts (<=4 chars) that appear INSIDE other existing dict keys "
      f"= partial-substitution risk: {len(risky)}")
for t, n, k in risky[:25]:
    print(f"  {t!r} would be a substring of {n} existing keys   ({k})")
