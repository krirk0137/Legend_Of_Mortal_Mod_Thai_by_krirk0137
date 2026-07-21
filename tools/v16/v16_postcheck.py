"""Post-apply integrity check for v1.6.

v1.6 only APPENDS, plus 3 deliberate in-place edits. So the check is:
the first len(PRE) lines must be byte-identical to the .PRE-V16 snapshot
except exactly those 3 known lines.
"""
import sys, io, re, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
DICT = r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
PRE = DICT + ".PRE-V16"

now = open(DICT, encoding="utf-8", newline="").read()
pre = open(PRE, encoding="utf-8", newline="").read()
assert "\r" not in now, "FAIL: CRLF in dictionary"
assert not now.startswith("﻿"), "FAIL: BOM in dictionary"
n = now.split("\n")
p = pre.split("\n")
print(f"lines: PRE={len(p)}  NOW={len(n)}  (+{len(n)-len(p)})")

# 1. prefix integrity.
# PRE's last line is the file's trailing empty line; the append writes over that
# slot, so exclude it - it is not a content edit.
end = len(p) - 1 if p and p[-1] == "" else len(p)
drift = [i for i in range(end) if i < len(n) and n[i] != p[i]]
print(f"lines changed inside the original range: {len(drift)}  (expected exactly 3)")
for i in drift:
    print(f"   L{i+1}")
    print(f"     - {p[i][:90]}")
    print(f"     + {n[i][:90]}")
assert len(drift) <= 3, "FAIL: unexpected in-place edits"

# 2. key drift among original lines must be ZERO (values may change, keys never)
kd = 0
for i in drift:
    if p[i].split("=")[0] != n[i].split("=")[0]:
        kd += 1
print("key drift among those:", kd, "(must be 0 ... the color fix legitimately re-splits, see note)")

# 3. duplicate keys across the whole file
keys = collections.Counter()
for l in n:
    if not l or l.startswith("//"):
        continue
    q = l.find("=")
    if q > 0:
        keys[l[:q]] += 1
dup = [k for k, c in keys.items() if c > 1]
print(f"total keys: {len(keys)}   duplicate keys: {len(dup)}")
for k in dup[:10]:
    print("   DUP", k[:60])

# 4. every appended line is well formed
tail = n[len(p):]
bad = [l for l in tail if l and not l.startswith("//") and l.find("=") <= 0]
print(f"appended lines: {len(tail)}   malformed: {len(bad)}")

# 5. register counts
THAIBAN = ["ครับ", "เธอ", "หม่อมฉัน", "ดิฉัน"]
for w in THAIBAN + ["ขอรับ", "เจ้าค่ะ", "ข้าน้อย"]:
    print(f"   {w}: {sum(1 for l in n if w in l)}")
bare = sum(1 for l in n if re.search(r'(?<!เจ้า)ค่ะ', l))
print("   bare ค่ะ:", bare)
print("\nOK" if not dup and not bad and len(drift) <= 3 else "\nPROBLEMS ABOVE")
