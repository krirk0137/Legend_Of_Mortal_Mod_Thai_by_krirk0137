import os,sys,io,re,json
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
D=r"C:/ClaudeCode/LegendOfMortalENG/AssetRipper_Solution2/LegendOfMortal/Assets/TextAsset"
SIMP=set("门龙学个这来说时对国过还开们记发师无东华万运东爱亲")
TRAD=set("門龍學個這來說時對國過還開們記發師無東華萬運東愛親")
out={}
for f in sorted(os.listdir(D)):
    t=open(os.path.join(D,f),encoding="utf-8-sig",errors="replace").read()
    han=sum(1 for c in t if '가'<=c<='힯')
    s=sum(1 for c in t if c in SIMP); tr=sum(1 for c in t if c in TRAD)
    if han> len(t)*0.05: lang="kr"
    elif s>tr: lang="zh-cn"
    elif tr>s: lang="zh-tw"
    else: lang="?"
    out[f]=lang
import collections
print(collections.Counter(out.values()))
# show the Story_* classification
for f,l in sorted(out.items()):
    if f.startswith("Story_") and not f.startswith("Story_Message"): print(f"  {f:<22}{l}")
json.dump(out,open("s2_filelang.json","w"),ensure_ascii=False)
