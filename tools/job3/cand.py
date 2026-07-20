import json,sys,io,collections,re
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
DICT=r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
lines=open(DICT,encoding="utf-8").read().split("\n")
l2s=json.load(open("line2sp.json",encoding="utf-8"))
kv=json.load(open("cs_kv.json",encoding="utf-8"))
FEMALE={"girl1","girl2","girl2_5","girl4","girl4_2","girl5","girl5_3","girl5_5","girl6","girl6_1","girl7","girl7_2","girl8","girl9","sister1","special813","special815","special818","special832","lady1","women1","women3","women4","women411","aunt1","aunt2","aunt3","showgirl1","female1","ranger_girl1","fairy1",
 "trainee_girl1","trainee_girl2","trainee_girl3","trainee_girl6","trainee_girl7","trainee_girl8","big_trainee_girl_1"}
cands=[]
for k,v in l2s.items():
    i=int(k)
    if len(v)!=1 or v[0] not in FEMALE: continue
    L=lines[i]
    if "ขอรับ" in L or "ข้าน้อย" in L: cands.append((i,v[0],L))
print("candidate lines:",len(cands))
print(collections.Counter(c[1] for c in cands).most_common())
# inspect ขอรับ contexts
ctx=collections.Counter()
for i,s,L in cands:
    val=L.split("=",1)[1]
    for m in re.finditer("ขอรับ",val):
        after=val[m.end():m.end()+6]
        ctx[after[:3] if after else "<END>"]+=1
print("\nchars right after ขอรับ:")
for a,c in ctx.most_common(25): print(f"  {c:>4} {a!r}")
json.dump([[i,s] for i,s,_ in cands],open("cands.json","w"),ensure_ascii=False)
