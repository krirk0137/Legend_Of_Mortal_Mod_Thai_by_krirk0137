# -*- coding: utf-8 -*-
# v1.3 JOB 1 Stage A — gendered register, line-aligned (never touches the key).
# BUG 1a: female-ack lines (snapshot value had ค่ะ/เจ้าค่ะ) whose live value became ขอรับ -> เจ้าค่ะ
# BUG 1b: ข้าน้อย -> ข้า on female lines (key has 妾身/奴家/小女子/本宫/本宮  OR  snapshot female-ack)
# Usage: PYTHONIOENCODING=utf-8 python v13_job1a.py <live> <snapshot(.POST-STORY01v11)> [--apply]
#   (persisted copy: tools/v13_job1a.py ; needs PYTHONIOENCODING=utf-8 for Thai stdout on Windows)
import io, sys, re

live_p, snap_p = sys.argv[1], sys.argv[2]
APPLY = "--apply" in sys.argv

def read(p):
    with io.open(p, "r", encoding="utf-8", newline="") as f:
        return f.readlines()

live = read(live_p)
snap = read(snap_p)
assert len(live) == len(snap), "NOT line-aligned: %d vs %d" % (len(live), len(snap))

FEMPRON = re.compile(r'(妾身|奴家|小女子|本宫|本宮)')

def split(line):
    body = line.rstrip("\r\n"); term = line[len(body):]
    if "=" in body:
        k, v = body.split("=", 1); return k, v, term, True
    return body, "", term, False

n_1a = n_1b = n_anom = 0
out = []
samples_1a = []; samples_1b = []
for i in range(len(live)):
    k, v, term, ok = split(live[i])
    if not ok:
        out.append(live[i]); continue
    sk, sv, _, sok = split(snap[i])
    female_ack = ("ค่ะ" in sv)                         # snapshot female acknowledgment
    female_pron = bool(FEMPRON.search(k))              # female pronoun in the (Chinese) key
    female = female_ack or female_pron
    nv = v

    # BUG 1a: restore female acknowledgment
    if female_ack:
        if "ขอรับ" in nv:
            nv = nv.replace("ขอรับ", "เจ้าค่ะ")
            nv = re.sub(r'(?<!เจ้า)ค่ะ', 'เจ้าค่ะ', nv)  # any bare ค่ะ -> เจ้าค่ะ (none exist now, safe)
            n_1a += 1
            if len(samples_1a) < 6: samples_1a.append((i+1, k[:22], v[:40], nv[:40]))
        else:
            n_anom += 1  # snapshot was female-ack but live has no ขอรับ (value diverged) -> skip, report

    # BUG 1b: women never say ข้าน้อย
    if female and "ข้าน้อย" in nv:
        before = nv
        nv = nv.replace("ข้าน้อย", "ข้า")
        n_1b += 1
        if len(samples_1b) < 6: samples_1b.append((i+1, k[:22], "F-pron" if female_pron else "F-ack"))

    out.append(k + "=" + nv + term)

print("=== DRY-RUN ===" if not APPLY else "=== APPLIED ===")
print("BUG 1a  ขอรับ->เจ้าค่ะ (female-ack lines): %d" % n_1a)
print("BUG 1b  ข้าน้อย->ข้า   (female lines)     : %d" % n_1b)
print("anomalies (snapshot female-ack but no ขอรับ in live, skipped): %d" % n_anom)
print("--- sample 1a (line, key, before -> after) ---")
for s in samples_1a: print("  L%d [%s] %s  ->  %s" % s)
print("--- sample 1b (line, key, reason) ---")
for s in samples_1b: print("  L%d [%s] %s" % s)

if APPLY:
    with io.open(live_p, "w", encoding="utf-8", newline="") as f:
        f.writelines(out)
    print(">>> written to", live_p)
