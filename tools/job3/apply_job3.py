import json,sys,io,re
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
DICT=r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
raw=open(DICT,encoding="utf-8",newline="").read()
lines=raw.split("\n")
props=json.load(open("proposal.json",encoding="utf-8"))
REVERT=[45272,73567,53065,31602,28812,31510,44376,44387,44592,43771,43775,40150]  # 1-based; player/waiter3/special103
THAI=re.compile(r'[฀-๿]')
nF=nM=0
for i,s,k,v,nv in props:
    cur=lines[i]
    assert cur==k+"="+v, f"line {i+1} drift"
    lines[i]=k+"="+nv; nF+=1
for ln in REVERT:
    i=ln-1; L=lines[i]; k,v=L.split("=",1)
    out=[]; j=0
    while j<len(v):
        if v.startswith("เจ้าค่ะ",j): out.append("ขอรับ"); j+=7
        else: out.append(v[j]); j+=1
    nv="".join(out)
    if nv!=v: lines[i]=k+"="+nv; nM+=1
print("female-fixed lines:",nF," male-reverted lines:",nM)
open(DICT,"w",encoding="utf-8",newline="").write("\n".join(lines))
