"""Coverage of the game's full key set - CORRECTED key parser.

cov2.py split dictionary lines at the first '=', which is wrong for any key
containing an escaped '=' (every <size\=NN> line). That made ~700 already
translated lines look missing. This uses the first UNESCAPED '='.
"""
import json, sys, io, os, re, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from xkey import load

SC = os.path.dirname(os.path.abspath(__file__))
DICT = r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
kv = json.load(open(os.path.join(SC, "s2_kv.json"), encoding="utf-8"))
dk = load(DICT)
print("dict keys (correct parse):", len(dk))

THAI = re.compile(r'[฀-๿]')
CJK = re.compile('[一-鿿]')
BSN = "\\" + "n"


def tags(t):
    t = re.sub(r'\{(size|color|link)=([^}]*)\}',
               lambda m: "<" + m.group(1) + "\\=" + m.group(2) + ">", t)
    t = re.sub(r'\{/(size|color|link)\}', lambda m: "</" + m.group(1) + ">", t)
    return t.replace("{b}", "<b>").replace("{/b}", "</b>")


def esc(t):
    """raw rich text already using '=' (e.g. <color=#FF0000>) still needs \\= in a key"""
    return re.sub(r'<(/?)(size|color|link)=', lambda m: "<" + m.group(1) + m.group(2) + "\\=", t)


def variants(t):
    for base in (t, tags(t), esc(t), esc(tags(t))):
        for x in (base, base.replace(BSN, ""), base.replace(BSN, " ")):
            yield x
            # XUnity's lookup #2 also tries the text with leading/trailing
            # whitespace removed, so a stripped key matches a padded string
            yield x.strip()


stat = collections.Counter()
gaps = collections.defaultdict(list)
for k, v in kv.items():
    ns = k.split("/")[0]
    if not v.strip():
        stat[(ns, "empty")] += 1
        continue
    hit = None
    for var in variants(v):
        if var in dk:
            hit = var
            break
    if hit is None:
        stat[(ns, "MISSING")] += 1
        gaps[ns].append([k, v])
    else:
        val = dk[hit][1]
        if THAI.search(val):
            stat[(ns, "thai")] += 1
        elif CJK.search(val):
            stat[(ns, "cjk")] += 1
        else:
            stat[(ns, "en")] += 1

rows = collections.defaultdict(dict)
for (ns, s), c in stat.items():
    rows[ns][s] = c
tot = collections.Counter()
print(f"\n{'namespace':<24}{'total':>7}{'thai':>7}{'EN':>6}{'MISSING':>9}")
for ns in sorted(rows, key=lambda n: -sum(rows[n].values())):
    r = rows[ns]
    t = sum(r.values())
    for a, b in r.items():
        tot[a] += b
    if t >= 15 or r.get("MISSING", 0) > 0:
        print(f"{ns:<24}{t:>7}{r.get('thai',0):>7}{r.get('en',0):>6}{r.get('MISSING',0):>9}")
n = sum(tot.values())
print(f"{'TOTAL':<24}{n:>7}{tot['thai']:>7}{tot['en']:>6}{tot['MISSING']:>9}")
print(f"\nThai coverage: {tot['thai']*100/n:.2f}%")
json.dump(dict(gaps), open(os.path.join(SC, "s2_gaps3.json"), "w", encoding="utf-8"),
          ensure_ascii=False)
