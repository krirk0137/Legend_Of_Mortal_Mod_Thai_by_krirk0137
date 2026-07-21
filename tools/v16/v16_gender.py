"""Cross-check the gendered register in every finished chunk against the speaker hint."""
import sys, io, os, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
SC = os.path.dirname(os.path.abspath(__file__))
D = os.path.join(SC, "v16")

src = {}
for f in os.listdir(D):
    if f.startswith("out_") or not f.endswith(".tsv"):
        continue
    for l in open(os.path.join(D, f), encoding="utf-8"):
        if not l.strip():
            continue
        p = l.rstrip("\n").split("\t")
        if len(p) == 2:
            src[p[0]] = p[1]

bad, ok = [], collections.Counter()
for f in sorted(os.listdir(D)):
    if not (f.startswith("out_") and f.endswith(".tsv")):
        continue
    for i, l in enumerate(open(os.path.join(D, f), encoding="utf-8-sig")):
        if not l.strip():
            continue
        k, v = l.rstrip("\n").split("\t", 1)
        s = src.get(k, "-")
        fem = "(หญิง)" in s
        male_form = "ขอรับ" in v or "ข้าน้อย" in v
        fem_form = "เจ้าค่ะ" in v
        if fem and male_form:
            bad.append((f, i + 1, "FEMALE speaker using male form", s, k, v))
        if (not fem) and fem_form and s != "-":
            bad.append((f, i + 1, "NON-FEMALE speaker using เจ้าค่ะ", s, k, v))
        if fem and fem_form:
            ok["female เจ้าค่ะ correct"] += 1
        if (not fem) and male_form:
            ok["male ขอรับ/ข้าน้อย correct"] += 1

print("correct:", dict(ok))
print("violations:", len(bad))
for f, ln, why, s, k, v in bad:
    print(f"\n  {f}:{ln}  {why}")
    print(f"    speaker: {s}")
    print(f"    key    : {k[:80]}")
    print(f"    value  : {v[:110]}")
