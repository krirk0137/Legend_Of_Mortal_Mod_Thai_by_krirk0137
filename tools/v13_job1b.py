# -*- coding: utf-8 -*-
# v1.3 JOB 1 Stage B — extend gendered ข้าน้อย->ข้า to female SELF-REFERENCE keys the
# pronoun list missed, EXCLUDING any line carrying a male self-ref marker (avoids the
# 妻妾成群/在下 and 赵活...上官萤 false positives where a MALE mentions a woman).
# Value-only edits, line-key never touched.
# Usage: PYTHONIOENCODING=utf-8 python v13_job1b.py <live> [--apply]
import io, sys, re

p = sys.argv[1]; APPLY = "--apply" in sys.argv
FEM  = re.compile(r'(民女|妾|上官萤|上官螢)')            # female self-reference / self-naming
MALE = re.compile(r'(在下|小人|小的|小可|小生|微臣|末将|末將|妻妾)')  # exclude: male marker or 妾-as-object

with io.open(p, "r", encoding="utf-8", newline="") as f:
    lines = f.readlines()

n = 0; out = []; samples = []
for ln in lines:
    body = ln.rstrip("\r\n"); term = ln[len(body):]
    if "=" not in body:
        out.append(ln); continue
    k, v = body.split("=", 1)
    if FEM.search(k) and not MALE.search(k) and "ข้าน้อย" in v:
        v = v.replace("ข้าน้อย", "ข้า"); n += 1
        if len(samples) < 20: samples.append(k[:40])
    out.append(k + "=" + v + term)

print("=== %s ===" % ("APPLIED" if APPLY else "DRY-RUN"))
print("female self-ref lines converted ข้าน้อย->ข้า:", n)
for s in samples: print("  [%s]" % s)

if APPLY:
    with io.open(p, "w", encoding="utf-8", newline="") as f:
        f.writelines(out)
    print(">>> written")
