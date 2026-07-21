import json, sys, io, re, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
g = json.load(open("s2_gaps.json", encoding="utf-8"))
kv = json.load(open("s2_kv.json", encoding="utf-8"))

SINGLE = re.compile(r'^[一-鿿]$')
FMT = re.compile(r'\{\d+[:}]')

buckets = collections.defaultdict(dict)   # bucket -> text -> [keys]
for ns, rows in g.items():
    for k, v in rows:
        if not v.strip():
            continue
        if SINGLE.match(v.strip()):
            b = "B-singlechar"
        elif FMT.search(v):
            b = "B-format"
        elif "{size=" in v or "{color=" in v or "{punch=" in v:
            b = "A-markup"
        else:
            b = "A-plain"
        buckets[b].setdefault(v, []).append(k)

print(f"{'bucket':<16}{'keys':>7}{'distinct texts':>16}")
for b in sorted(buckets):
    keys = sum(len(x) for x in buckets[b].values())
    print(f"{b:<16}{keys:>7}{len(buckets[b]):>16}")

OUT = r"C:/ClaudeCode/LegendOfMortalENG/tools/solution2/gap_worklist.tsv"
import os
os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, "w", encoding="utf-8", newline="\n") as f:
    f.write("#bucket\tn_keys\tsample_key\tchinese_text\n")
    for b in sorted(buckets):
        for v, ks in sorted(buckets[b].items(), key=lambda x: -len(x[1])):
            f.write(f"{b}\t{len(ks)}\t{ks[0]}\t{v}\n")
print("wrote", OUT)

# collision stats for the report
byval = collections.defaultdict(list)
for k, v in kv.items():
    if v.strip():
        byval[v].append(k)
coll = {v: ks for v, ks in byval.items() if len(ks) > 1}
print("\ncollision keys:", sum(len(x) for x in coll.values()), "of", len(kv))
