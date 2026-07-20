import json,sys,io,collections,re
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
t=json.load(open("lean_tables.json",encoding="utf-8"))
cs=t["ChineseSimplified"]
kv={}
for pid,pairs in cs:
    for i in range(0,len(pairs)-1,2):
        kv[pairs[i]]=pairs[i+1]
print("total keys",len(kv))
ns=collections.Counter(k.split("/")[0] for k in kv)
for k,v in ns.most_common(30): print(f"  {v:>7} {k}")
json.dump(kv,open("cs_kv.json","w",encoding="utf-8"),ensure_ascii=False)
