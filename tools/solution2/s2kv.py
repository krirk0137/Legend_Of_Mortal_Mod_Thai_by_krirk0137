import os,sys,io,json,collections
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
D=r"C:/ClaudeCode/LegendOfMortalENG/AssetRipper_Solution2/LegendOfMortal/Assets/TextAsset"
lang=json.load(open("s2_filelang.json",encoding="utf-8"))
kv={}; conf=0; unp=0
for f,l in sorted(lang.items()):
    if l!="zh-cn": continue
    for line in open(os.path.join(D,f),encoding="utf-8-sig",errors="replace").read().split("\n"):
        line=line.rstrip("\r")
        if not line.strip() or line.lstrip().startswith("//"): continue
        p=line.find(" = ")
        if p<0: unp+=1; continue
        k,v=line[:p],line[p+3:]
        if k in kv and kv[k]!=v: conf+=1
        kv[k]=v
print(f"zh-cn keys: {len(kv)}  conflicts: {conf}  unparsed: {unp}")
game=json.load(open("cs_kv.json",encoding="utf-8"))
print(f"game runtime table (level0): {len(game)}")
onlyS2=set(kv)-set(game); onlyG=set(game)-set(kv)
print(f"only in AssetRipper: {len(onlyS2)}   only in level0 table: {len(onlyG)}")
print("  sample only-in-ripper:", list(onlyS2)[:8])
print("  sample only-in-level0:", list(onlyG)[:8])
json.dump(kv,open("s2_kv.json","w",encoding="utf-8"),ensure_ascii=False)
