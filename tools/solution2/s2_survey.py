"""How much of the game's text can be round-tripped through the XUnity dictionary
without losing information (line breaks, Fungus command tags)?"""
import sys, io, json, re, collections, os

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
HERE = os.path.dirname(os.path.abspath(__file__))

kv = json.load(open(os.path.join(HERE, "s2_kv.json"), encoding="utf-8"))

CMD = re.compile(r'\{/?(?:punch|vpunch|hpunch|flash|w|wi|wc|wvo|wp|c|x|s)(?:=[^}]*)?\}')
DISP = re.compile(r'\{/?(?:size|color|link|b|i)(?:=[^}]*)?\}')
FMT = re.compile(r'\{\d+[:}]')
VAR = re.compile(r'\{\$[A-Za-z0-9_]+\}')
BSN = "\\" + "n"

c = collections.Counter()
cmds = collections.Counter()
for k, v in kv.items():
    nl, cmd = BSN in v, CMD.search(v)
    if nl:
        c["has literal \\n"] += 1
    if cmd:
        c["has Fungus command tag"] += 1
        for m in CMD.findall(v):
            cmds[m] += 1
    if DISP.search(v):
        c["has display tag {size}/{color}"] += 1
    if FMT.search(v):
        c["has {0} format arg"] += 1
    if VAR.search(v):
        c["has {$var}"] += 1
    if not nl and not cmd:
        c["LOSSLESS (no \\n, no cmd tag)"] += 1

for x, n in c.most_common():
    print(f"  {x:<34}{n:>7}  ({n*100//len(kv)}%)")
print(f"  {'total keys':<34}{len(kv):>7}")
print("\n  command tags actually used:", dict(cmds.most_common(12)))
