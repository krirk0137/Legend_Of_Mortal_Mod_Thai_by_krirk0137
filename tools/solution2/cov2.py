import json,sys,io,collections,re
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
DICT=r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
kv=json.load(open("s2_kv.json",encoding="utf-8"))
lines=open(DICT,encoding="utf-8").read().split("\n")
dk={}
for i,l in enumerate(lines):
    if not l or l.startswith("//"): continue
    p=l.find("=")
    if p<=0: continue
    dk.setdefault(l[:p],i)
THAI=re.compile(r'[฀-๿]'); CJK=re.compile(r'[一-鿿]')
BSN="\\"+"n"
def tags(t):
    t=re.sub(r'\{size=(\d+)\}',lambda m:"<size\="+m.group(1)+">",t)
    return t.replace("{/size}","</size>").replace("{b}","<b>").replace("{/b}","</b>")
def variants(t):
    for base in (t, tags(t)):
        yield ("raw",base)
        yield ("strip-n", base.replace(BSN,""))
        yield ("real-n", base.replace(BSN,"\n"))
        yield ("crlf", base.replace(BSN,"\r\n"))
        yield ("space-n", base.replace(BSN," "))
stat=collections.Counter(); how=collections.Counter(); gaps=collections.defaultdict(list)
for k,v in kv.items():
    ns=k.split("/")[0]
    if not v.strip(): stat[(ns,"empty")]+=1; continue
    hit=None
    for tag,var in variants(v):
        if var in dk: hit=(tag,var); break
    if hit is None:
        stat[(ns,"MISSING")]+=1; gaps[ns].append([k,v])
    else:
        how[hit[0]]+=1
        val=lines[dk[hit[1]]].split("=",1)[1]
        if THAI.search(val): stat[(ns,"thai")]+=1
        elif CJK.search(val): stat[(ns,"cjk")]+=1
        else: stat[(ns,"en")]+=1
print("match method:",how.most_common())
rows=collections.defaultdict(dict)
for (ns,s),c in stat.items(): rows[ns][s]=c
print(f"\n{'namespace':<24}{'total':>7}{'thai':>7}{'EN':>6}{'CJK':>5}{'MISSING':>9}")
tot=collections.Counter()
for ns in sorted(rows,key=lambda n:-sum(rows[n].values())):
    r=rows[ns]; t=sum(r.values())
    for a,b in r.items(): tot[a]+=b
    if t>=15 or r.get("MISSING",0)>0:
        print(f"{ns:<24}{t:>7}{r.get('thai',0):>7}{r.get('en',0):>6}{r.get('cjk',0):>5}{r.get('MISSING',0):>9}")
print(f"{'TOTAL':<24}{sum(tot.values()):>7}{tot['thai']:>7}{tot['en']:>6}{tot['cjk']:>5}{tot['MISSING']:>9}")
json.dump(dict(gaps),open("s2_gaps.json","w",encoding="utf-8"),ensure_ascii=False)
