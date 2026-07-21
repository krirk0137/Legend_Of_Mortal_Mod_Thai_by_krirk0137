import json,sys,io,collections,re
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
kv=json.load(open("s2_kv.json",encoding="utf-8"))
byval=collections.defaultdict(list)
for k,v in kv.items():
    if v.strip(): byval[v].append(k)
coll={v:ks for v,ks in byval.items() if len(ks)>1}
print(f"distinct texts: {len(byval)}   texts used by >1 key: {len(coll)}")
print(f"keys involved in a collision: {sum(len(k) for k in coll.values())}  ({sum(len(k) for k in coll.values())*100//len(kv)}% of all keys)")
top=sorted(coll.items(),key=lambda x:-len(x[1]))[:12]
for v,ks in top: print(f"  x{len(ks):<4} {v[:40]!r}   e.g. {ks[:3]}")
# how many collisions are ACROSS namespaces (different UI meaning)?
cross=[(v,ks) for v,ks in coll.items() if len({k.split('/')[0] for k in ks})>1]
print(f"\ncollisions spanning >1 namespace (genuinely different contexts): {len(cross)}")
for v,ks in sorted(cross,key=lambda x:-len(x[1]))[:12]:
    print(f"  x{len(ks):<4} {v[:32]!r}  ns={sorted({k.split('/')[0] for k in ks})}")
