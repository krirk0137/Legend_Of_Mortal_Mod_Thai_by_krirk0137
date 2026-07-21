"""Verify a v1.6 worker output chunk against its source chunk."""
import sys, io, os, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
SC = os.path.dirname(os.path.abspath(__file__))
D = os.path.join(SC, "v16")

THAI = re.compile(r'[฀-๿]')
LATIN = re.compile(r'[A-Za-z]')
CJK = re.compile('[一-鿿]')
BANNED = ["ครับ", "ฉัน", "เธอ", "คุณ", "ผม", "ดิฉัน", "หม่อมฉัน"]
# legitimate words that merely CONTAIN a banned pronoun as a substring.
# Established project false positives: คุณหนู/คุณชาย (wuxia honorifics),
# ขอบคุณ/บุญคุณ/คุณธรรม, เส้นผม, ฉันมิตร.
FALSE_POS = ["คุณหนู", "คุณชาย", "คุณนาย", "คุณธรรม", "คุณภาพ", "คุณค่า", "คุณสมบัติ",
             "คุณประโยชน์", "คุณูปการ", "ขอบคุณ", "บุญคุณ", "พระคุณ", "คุณงาม",
             "เส้นผม", "ผมเผ้า", "ผมขาว", "ผมดำ", "เผ้าผม",
             "ฉันมิตร", "ฉันท์", "ฉันใด", "ฉันนั้น", "พลันฉัน"]


def strip_false_positives(v):
    for fp in FALSE_POS:
        v = v.replace(fp, "|")
    return v
TAG = re.compile(r'<(/?)(size|color|b|i|link)(\\=[^>]*)?>')


def tagsig(s):
    return "".join(m.group(0) for m in TAG.finditer(s))


def check(base):
    src = os.path.join(D, base + ".tsv")
    out = os.path.join(D, "out_" + base + ".tsv")
    if not os.path.exists(out):
        return f"{base}: OUTPUT MISSING"
    s = [l.split("\t")[0] for l in open(src, encoding="utf-8").read().split("\n") if l.strip()]
    raw = open(out, encoding="utf-8-sig").read()
    o = [l for l in raw.split("\n") if l.strip()]
    errs = []
    if raw.startswith("﻿"):
        errs.append("BOM present")
    if "\r" in raw:
        errs.append("CRLF present")
    if len(o) != len(s):
        errs.append(f"line count {len(o)} != {len(s)}")
    keys, vals = [], []
    for i, l in enumerate(o):
        if "\t" not in l:
            errs.append(f"L{i+1}: no TAB")
            continue
        k, v = l.split("\t", 1)
        keys.append(k)
        vals.append(v)
        if "\t" in v:
            errs.append(f"L{i+1}: extra TAB")
    # key fidelity
    ss, ks = set(s), set(keys)
    miss = ss - ks
    extra = ks - ss
    if miss:
        errs.append(f"{len(miss)} source keys MISSING, e.g. {list(miss)[:2]}")
    if extra:
        errs.append(f"{len(extra)} keys NOT IN SOURCE (corrupted), e.g. {list(extra)[:2]}")
    if len(keys) != len(set(keys)):
        errs.append("duplicate keys in output")
    # per-line checks
    sig = {x: tagsig(x) for x in s}
    for k, v in zip(keys, vals):
        # punctuation-only sources (e.g. <size\=50>！！</size>) legitimately have no Thai
        if not THAI.search(v) and CJK.search(k):
            errs.append(f"no Thai in value for {k[:30]!r}")
        if LATIN.search(v) and not TAG.search(v):
            errs.append(f"latin leak in value for {k[:30]!r}: {v[:40]!r}")
        vb = strip_false_positives(v)
        for b in BANNED:
            if b in vb:
                errs.append(f"BANNED {b!r} in value for {k[:30]!r}: {v[:50]!r}")
        if k in sig and tagsig(v) != sig[k]:
            errs.append(f"tag mismatch for {k[:30]!r}: src={sig[k][:40]!r} out={tagsig(v)[:40]!r}")
        if re.search(r'<[a-z]+=', v) or re.search(r'<[a-z]+=', k):
            errs.append(f"UNESCAPED tag = in {k[:30]!r}")
    status = "PASS" if not errs else "FAIL"
    head = f"{base}: {status}  ({len(o)} lines)"
    if errs:
        head += "\n    " + "\n    ".join(errs[:14])
        if len(errs) > 14:
            head += f"\n    ... +{len(errs)-14} more"
    return head


bases = sorted({f[:-4].replace("out_", "") for f in os.listdir(D)
                if f.startswith("out_") and f.endswith(".tsv")})
if len(sys.argv) > 1:
    bases = sys.argv[1:]
for b in bases:
    print(check(b))
