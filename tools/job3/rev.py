import json,sys,io,collections
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
DICT=r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
lines=open(DICT,encoding="utf-8").read().split("\n")
l2s=json.load(open("line2sp.json",encoding="utf-8"))
gs=json.load(open("gender_signals.json",encoding="utf-8"))
FEMALE={"girl1","girl2","girl2_5","girl4","girl4_2","girl5","girl5_3","girl5_5","girl6","girl6_1","girl7","girl7_2","girl8","girl9","sister1","special813","special815","special818","special832","lady1","women1","women3","women4","women411","aunt1","aunt2","aunt3","showgirl1","female1","ranger_girl1","fairy1","trainee_girl1","trainee_girl2","trainee_girl3","trainee_girl6","trainee_girl7","trainee_girl8","big_trainee_girl_1"}
tot=collections.Counter(); ex=[]
for k,v in l2s.items():
    i=int(k)
    if "เจ้าค่ะ" not in lines[i]: continue
    tot["lines_with_เจ้าค่ะ"]+=1
    if len(v)!=1: tot["ambiguous"]+=1; continue
    if v[0] in FEMALE: tot["female_OK"]+=1
    else:
        tot["NON-female speaker"]+=1
        ex.append((i,v[0],gs.get(v[0],{}).get("name"),lines[i][:110]))
print(tot.most_common())
for i,s,n,l in ex: print(f"L{i+1} [{s}={n}] {l}")
