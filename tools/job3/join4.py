import json,sys,io,collections
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
DICT=r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
rows=json.load(open("say_rows.json",encoding="utf-8"))
kv=json.load(open("cs_kv.json",encoding="utf-8"))
lines=open(DICT,encoding="utf-8").read().split("\n")
dk={}
for i,l in enumerate(lines):
    if not l or l.startswith("//"): continue
    p=l.find("=")
    if p<=0: continue
    dk.setdefault(l[:p],[]).append(i)
def variants(t):
    return [t, t.replace("\r\n","\r\n"), t.replace("\r\n","\n"), t.replace("\r\n",""), t.replace("\r\n"," ")]
line2sp=collections.defaultdict(set)
for f,sid,s,d in rows:
    t=kv.get("Story/"+sid)
    if t is None or s is None: continue
    for v in variants(t):
        if v in dk:
            for i in dk[v]: line2sp[i].add(s)
            break
male=[i for i in line2sp if "ขอรับ" in lines[i] or "ข้าน้อย" in lines[i]]
print("dict lines w/ speaker info:",len(line2sp))
print("of those, carrying male register:",len(male))
c=collections.Counter(len(line2sp[i]) for i in male)
print("speaker-count distribution on male-register lines:",sorted(c.items())[:10],"...")
print("  exactly 1 speaker:",c[1])
json.dump({str(k):sorted(v) for k,v in line2sp.items()},open("line2sp.json","w"),ensure_ascii=False)
