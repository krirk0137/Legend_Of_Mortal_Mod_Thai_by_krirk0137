import UnityPy, sys, io, struct, collections, json, os
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
G="C:/Program Files (x86)/Steam/steamapps/common/LegendOfMortal/Mortal_Data/"
env=UnityPy.load(G+"level0")
def strings(raw):
    i=0; out=[]
    while i+4<=len(raw):
        n=struct.unpack_from("<I",raw,i)[0]
        if 1<=n<=200000 and i+4+n<=len(raw):
            b=raw[i+4:i+4+n]
            try:
                s=b.decode('utf-8')
                if all(ord(c)>=32 or c in '\r\n\t' for c in s):
                    out.append(s); i+=4+n; i+=(-i)%4; continue
            except: pass
        i+=1
    return out
tables={}
for o in env.objects:
    if o.type.name!="MonoBehaviour": continue
    d=o.read(check_read=False); s=d.m_Script.read() if d.m_Script else None
    if not s or s.m_ClassName!="LeanLanguageCSV": continue
    ss=strings(o.get_raw_data())
    # first strings: '[', LANG, ' = ', '\n', ' // ' then pairs
    if len(ss)<6: continue
    lang=ss[1]
    pairs=ss[5:]
    tables.setdefault(lang,[]).append((o.path_id,pairs))
for lang,lst in tables.items():
    tot=sum(len(p)//2 for _,p in lst)
    print(f"{lang}: {len(lst)} csv assets, ~{tot} pairs")
json.dump({l:[[pid,p] for pid,p in lst] for l,lst in tables.items()}, open("lean_tables.json","w",encoding="utf-8"), ensure_ascii=False)
print("saved lean_tables.json")
