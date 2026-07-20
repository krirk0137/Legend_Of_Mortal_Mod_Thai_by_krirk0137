import json,sys,io,collections,re
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
kv=json.load(open("cs_kv.json",encoding="utf-8"))
cm=json.load(open("charmap.json",encoding="utf-8")); mapping=cm["mapping"]; folders=cm["folders"]
tags=json.load(open("say_rows.json",encoding="utf-8"))
alltags=sorted({r[2] for r in tags if r[2]})
corpus=list(kv.values())
FEM_FOLDER=re.compile(r'^(Girl|TraineeGirl|Female|Women|Sister|Fairy)')
MAL_FOLDER=re.compile(r'^(Boy|Brother|Man|Monk|Soldier|Blacksmith|Waiter|Badguy|Beggar|Master|Magistrate|Scholar|Merchant|Elder|Guard|retainer|Stone|Dio|Hero|BigTrainee|Shopman|Judgement|CowHorse|Lee|Artist|Ranger|Mask|Ghost|Stranger|Child|Player|Misc|Special|Trainee|Big|Tank|FireBird)')
FEMWORD=re.compile(r'(娘子|夫人|婦人|妇人|女弟子|女子|姑娘|小姐|嬤|媽|大媽|婢|丫鬟|仙姑|女俠|女侠|女樂|女乐|師妹|师妹|嫂)')
MALWORD=re.compile(r'(師兄|师兄|公子|先生|大俠|大侠|少俠|少侠|和尚|道人|老人|掌門|掌门|幫主|帮主|弟子\d|家丁|官兵|乞丐|店小二|鐵匠|铁匠|知縣|通判|少年|武師|武师|書生|书生|掌櫃|掌柜|山賊|山贼|海盜|海盗|歹人|路人)')
out={}
for t in alltags:
    name=kv.get("Character/"+t)
    mi=mapping.get(t); fol=folders.get(mi["asset"]) if mi else None
    she=he=0
    if name and len(name)>=2 and name not in ("？？？",):
        for v in corpus:
            p=0
            while True:
                j=v.find(name,p)
                if j<0: break
                w=v[j+len(name):j+len(name)+40]
                she+=w.count("她"); he+=w.count("他")
                p=j+1
    sig=[]
    if fol and FEM_FOLDER.match(fol): sig.append("F:folder")
    if name and FEMWORD.search(name): sig.append("F:word")
    if name and MALWORD.search(name): sig.append("M:word")
    tot=she+he
    if tot>=5:
        if she/tot>=0.8: sig.append(f"F:pron({she}/{tot})")
        elif he/tot>=0.8: sig.append(f"M:pron({he}/{tot})")
        else: sig.append(f"?:pron({she}/{tot})")
    out[t]={"name":name,"folder":fol,"she":she,"he":he,"sig":sig}
json.dump(out,open("gender_signals.json","w",encoding="utf-8"),ensure_ascii=False,indent=1)
# print for the fixable speakers
import collections
print(f"{'tag':<18}{'name':<12}{'folder':<26} signals")
for t in alltags:
    o=out[t]
    print(f"{t:<18}{str(o['name']):<12}{str(o['folder']):<26} {' '.join(o['sig'])}")
