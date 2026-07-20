import UnityPy, sys, io, struct, json, re
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
G="C:/Program Files (x86)/Steam/steamapps/common/LegendOfMortal/Mortal_Data/"
env=UnityPy.load(G+"sharedassets2.assets")
def seqstr(raw,start=28,limit=200):
    i=start; out=[]
    while i+4<=len(raw) and len(out)<limit:
        n=struct.unpack_from("<I",raw,i)[0]
        if 1<=n<=20000 and i+4+n<=len(raw):
            try:
                s=raw[i+4:i+4+n].decode('utf-8')
                if all(ord(c)>=32 or c in '\r\n\t' for c in s):
                    out.append(s); i+=4+n; i+=(-i)%4; continue
            except: pass
        i+=1
    return out
mapping={}; folders={}
for o in env.objects:
    if o.type.name!="MonoBehaviour": continue
    d=o.read(check_read=False); s=d.m_Script.read() if d.m_Script else None
    if not s: continue
    cn=s.m_ClassName; raw=o.get_raw_data()
    if cn=="StoryCharacterData":
        n=struct.unpack_from("<I",raw,28)[0]; nm=raw[32:32+n].decode('utf-8','replace')
        m=re.search(rb'Assets/__Project/Images/Characters/([^/]+)/',raw)
        folders[nm]=m.group(1).decode('utf-8','replace') if m else None
    elif cn=="StoryMappingItem":
        ss=seqstr(raw,28,4)
        if len(ss)>=3: mapping[ss[2]]={"asset":ss[0],"display":ss[1]}
print("tags",len(mapping),"folders",len(folders))
json.dump({"mapping":mapping,"folders":folders},open("charmap.json","w",encoding="utf-8"),ensure_ascii=False,indent=1)
