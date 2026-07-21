import sys, io, os, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from xkey import split

D = r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
pre = {}
for i, l in enumerate(open(D, encoding="utf-8")):
    l = l.rstrip("\n")
    if not l or l.startswith("//"):
        continue
    k, v = split(l)
    if k and k not in pre:
        pre[k] = (i + 1, v)

V = os.path.join(os.path.dirname(os.path.abspath(__file__)), "v16")
c = collections.Counter()
clashes = []
for f in sorted(os.listdir(V)):
    if not (f.startswith("out_") and f.endswith(".tsv")):
        continue
    g = "dice" if "dice" in f else "story"
    for l in open(os.path.join(V, f), encoding="utf-8-sig"):
        if not l.strip():
            continue
        k, v = l.rstrip("\n").split("\t", 1)
        if k in pre:
            c[(g, "CLASH")] += 1
            clashes.append((k, pre[k][0], pre[k][1], v))
        else:
            c[(g, "NEW")] += 1
print("clash analysis:")
for k, n in sorted(c.items()):
    print(f"   {k[0]:<6}{k[1]:<6}{n}")
print(f"\ntotal NEW = {c[('dice','NEW')] + c[('story','NEW')]}")
print("\nsample clashes (existing dict value vs new worker value):")
for k, ln, old, new in clashes[:6]:
    print(f"  L{ln} {k[:60]}")
    print(f"     OLD {old[:80]}")
    print(f"     NEW {new[:80]}")
