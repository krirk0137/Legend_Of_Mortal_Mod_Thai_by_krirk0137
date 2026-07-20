import json,sys,io,re,collections
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
DICT=r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
lines=open(DICT,encoding="utf-8").read().split("\n")
cands=json.load(open("cands.json",encoding="utf-8"))
THAI=re.compile(r'[฀-๿]')
def fix(val):
    out=[]; i=0; nP=nV=0
    while i<len(val):
        if val.startswith("ขอรับ",i):
            nxt=val[i+5:i+6]
            if nxt and THAI.match(nxt):   # verb: ขอรับใช้ / ขอรับคำสั่ง / ขอรับไว้
                out.append("ขอรับ"); nV+=1
            else:
                out.append("เจ้าค่ะ"); nP+=1
            i+=5
        elif val.startswith("ข้าน้อย",i):
            out.append("ข้า"); i+=7; nP+=0
        else:
            out.append(val[i]); i+=1
    return "".join(out),nP,nV
props=[]; tp=tv=tn=0
for i,s in cands:
    L=lines[i]; k,v=L.split("=",1)
    nv,nP,nV=fix(v)
    nn=v.count("ข้าน้อย")
    if nv!=v: props.append((i,s,k,v,nv))
    tp+=nP; tv+=nV; tn+=nn
print(f"lines changed: {len(props)}   ขอรับ->เจ้าค่ะ: {tp}   ขอรับ kept as verb: {tv}   ข้าน้อย->ข้า: {tn}")
json.dump(props,open("proposal.json","w",encoding="utf-8"),ensure_ascii=False)
for i,s,k,v,nv in props:
    print(f"\nL{i+1} [{s}]  {k[:40]}")
    print("  -", v[:150])
    print("  +", nv[:150])
