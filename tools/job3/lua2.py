import UnityPy, sys, io, os, re
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
G="C:/Program Files (x86)/Steam/steamapps/common/LegendOfMortal/Mortal_Data/"
env=UnityPy.load(G+"resources.assets")
n=0; hits=0
for o in env.objects:
    if o.type.name!="TextAsset": continue
    d=o.read(); s=d.m_Script
    if isinstance(s,str): b=s.encode('utf-8','surrogateescape')
    else: b=s
    n+=1
    if b'GetStoryText' in b:
        hits+=1
        name=re.sub(r'[^A-Za-z0-9_.\-]','_',d.m_Name)
        open(f"lua/{name}.lua","wb").write(b)
print("textassets",n,"with GetStoryText",hits)
