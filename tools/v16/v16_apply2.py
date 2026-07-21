"""v1.6 apply - runs from the .PRE-V16 state.

1. three in-place fixes (2 leftover English lines + 1 broken <color=> key)
2. append ONLY keys that do not already exist, using the CORRECT XUnity key
   parser (first UNESCAPED '='). The naive first-'=' parse made 420 already
   translated <size\=NN> lines look missing.
"""
import sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from xkey import split

SC = os.path.dirname(os.path.abspath(__file__))
D = os.path.join(SC, "v16")
DICT = r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
PRE = DICT + ".PRE-V16"

raw = open(PRE, encoding="utf-8", newline="").read()
assert "\r" not in raw and not raw.startswith("﻿")
lines = raw.split("\n")
print("starting from PRE-V16:", len(lines), "lines")

# ---- 1. in-place fixes -------------------------------------------------
FIX = [
    (22808, "笑话，不过区区庸材罢了。", "เรื่องตลก ก็แค่คนสามัญไร้ความสามารถเท่านั้น"),
    (24381, "那也是难能可贵了。", "เช่นนั้นก็นับว่าหาได้ยากยิ่งแล้ว"),
]
for i, k, v in FIX:
    assert lines[i].startswith(k + "="), lines[i][:50]
    lines[i] = k + "=" + v
    print(f"  fixed L{i+1}: {k[:24]}")

i = 54182
assert lines[i].startswith("<color=#FF7979>"), lines[i][:40]
lines[i] = lines[i].replace("<color=#FF7979>", "<color\\=#FF7979>")
print(f"  fixed L{i+1}: escaped '=' in the <color> tag so the key parses at all")

# ---- 2. append genuinely-new keys --------------------------------------
existing = set()
for l in lines:
    if not l or l.startswith("//"):
        continue
    k, _ = split(l)
    if k:
        existing.add(k)
print("existing keys (correct parse):", len(existing))

groups = {"dice": [], "story": []}
for f in sorted(os.listdir(D)):
    if not (f.startswith("out_") and f.endswith(".tsv")):
        continue
    g = "dice" if "dice" in f else "story"
    for l in open(os.path.join(D, f), encoding="utf-8-sig"):
        if not l.strip():
            continue
        k, v = l.rstrip("\n").split("\t", 1)
        v = v.replace("？", "?")        # dictionary's dominant convention
        groups[g].append((k, v))

HDR = {"dice": "////-------------v1.6 DiceHeader (choice captions)////-------------",
       "story": "////-------------v1.6 gap-fill (story / misc)////-------------"}
block, added, skip, dup = [], 0, 0, 0
seen = set()
for g in ("dice", "story"):
    part = []
    for k, v in groups[g]:
        if k in existing:
            skip += 1
            continue
        if k in seen:
            dup += 1
            continue
        seen.add(k)
        part.append(k + "=" + v)
    if part:
        block.append(HDR[g])
        block += part
        added += len(part)
    print(f"  {g}: +{len(part)} new")
print(f"appending {added}   (already translated, left alone: {skip}; dup in batch: {dup})")

while lines and lines[-1] == "":
    lines.pop()
out = lines + block + [""]
open(DICT, "w", encoding="utf-8", newline="").write("\n".join(out))
print("dict lines:", len(out))
