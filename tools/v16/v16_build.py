"""Build the v1.6 translation worklist: game text -> dict-key form, with context."""
import json, sys, io, re, os, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

SC = r"C:/Users/krirk/AppData/Local/Temp/claude/C--ClaudeCode-LegendOfMortalENG/81489534-700d-44f2-97a5-7fb4fd635b9b/scratchpad"
DICT = r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
gaps = json.load(open(os.path.join(SC, "s2_gaps.json"), encoding="utf-8"))
say = json.load(open(os.path.join(SC, "say_rows.json"), encoding="utf-8"))
gs = json.load(open(os.path.join(SC, "gender_signals.json"), encoding="utf-8"))

FEMALE = {"girl1","girl2","girl2_5","girl4","girl4_2","girl5","girl5_3","girl5_5","girl6",
          "girl6_1","girl7","girl7_2","girl8","girl9","sister1","special810","special813",
          "special815","special818","special832","lady1","women1","women3","women4","women411",
          "aunt1","aunt2","aunt3","showgirl1","female1","ranger_girl1","fairy1","trainee_girl1",
          "trainee_girl2","trainee_girl3","trainee_girl6","trainee_girl7","trainee_girl8",
          "big_trainee_girl_1"}

# story id -> speakers
sid2sp = collections.defaultdict(set)
for f, sid, s, d in say:
    if s:
        sid2sp["Story/" + sid].add(s)

BSN = "\\" + "n"


def to_dict_key(text):
    """Game text -> the exact key form our dictionary uses."""
    t = text
    # Fungus command tags that are consumed and never displayed
    t = re.sub(r'\{/?(?:punch|vpunch|hpunch|flash|w|wi|wc|wvo|wp|c|x|s)(?:=[^}]*)?\}', '', t)
    # displayed rich text: {size=24} -> <size\=24>
    t = re.sub(r'\{(size|color|link)=([^}]*)\}', lambda m: "<" + m.group(1) + "\\=" + m.group(2) + ">", t)
    t = re.sub(r'\{/(size|color|link)\}', lambda m: "</" + m.group(1) + ">", t)
    t = t.replace("{b}", "<b>").replace("{/b}", "</b>")
    t = t.replace("{i}", "<i>").replace("{/i}", "</i>")
    # our dict stores multi-line keys with the newline REMOVED (XUnity's
    # whitespace-insensitive lookup makes this match at runtime)
    t = t.replace(BSN, "")
    t = t.strip()
    # every '=' inside an XUnity key must be escaped, or the key/value split breaks
    t = re.sub(r'(?<!\\)=', "\\\\=", t)
    return t


# things a static key can never match, or that are not text at all
SKIP = re.compile(r'\{\$[A-Za-z0-9_]+\}'   # Fungus variable -> becomes a number at runtime
                  r'|^\{[a-zA-Z]+\}$'      # pure LeanToken placeholder
                  r'|\{/\d+\}')            # malformed close tag in the game's own data


existing = set()
for l in open(DICT, encoding="utf-8"):
    l = l.rstrip("\n")
    if not l or l.startswith("//"):
        continue
    p = l.find("=")
    if p > 0:
        existing.add(l[:p])

SINGLE = re.compile(r'^[一-鿿]$')
FMT = re.compile(r'\{\d+[:}]')

rows = {}
skipped = []
for ns, items in gaps.items():
    for k, v in items:
        if not v.strip():
            continue
        if SINGLE.match(v.strip()) or FMT.search(v):
            continue                        # bucket B: Solution 2 only
        if SKIP.search(v):
            skipped.append((k, v))
            continue
        dkey = to_dict_key(v)
        if not dkey or dkey in existing:
            continue
        r = rows.setdefault(dkey, {"ns": set(), "keys": [], "spk": set()})
        r["ns"].add(ns)
        r["keys"].append(k)
        for s in sid2sp.get(k, ()):
            r["spk"].add(s)

print("distinct new dict keys:", len(rows))
print("skipped (dynamic/token/malformed, cannot be a static key):", len(skipped))
for k, v in skipped:
    print("   SKIP", k, "=", v[:70])
nsc = collections.Counter()
for r in rows.values():
    nsc["+".join(sorted(r["ns"]))] += 1
print(nsc.most_common(12))

out = os.path.join(SC, "v16_input.tsv")
with open(out, "w", encoding="utf-8", newline="\n") as f:
    f.write("#dict_key\tns\tspeaker\n")
    for dkey, r in sorted(rows.items(), key=lambda x: (sorted(x[1]["ns"])[0], x[0])):
        sp = sorted(r["spk"])
        if len(sp) == 1:
            t = sp[0]
            nm = gs.get(t, {}).get("name") or t
            who = f"{nm} ({'หญิง' if t in FEMALE else 'ชาย?'})"
        elif len(sp) > 1:
            who = "หลายคน"
        else:
            who = "-"
        f.write(f"{dkey}\t{'+'.join(sorted(r['ns']))}\t{who}\n")
print("wrote", out)
lens = [len(k) for k in rows]
print("key length: min", min(lens), "max", max(lens), "total bytes",
      sum(len(k.encode('utf-8')) for k in rows))
