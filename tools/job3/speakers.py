import re,os,json,sys,io,collections
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
SET=re.compile(r'setcharacter\(\s*characters\.Get\("([^"]+)"\)')
NAR=re.compile(r'setsaydialog\(saydialogs\.(\w+)\)')
SAY=re.compile(r'GetStoryText\("([^"]+)"\)')
rows=[]
for fn in sorted(os.listdir("lua")):
    txt=open("lua/"+fn,encoding='utf-8',errors='replace').read()
    cur=None; dlg=None
    for m in re.finditer(r'setcharacter\(\s*characters\.Get\("([^"]+)"\)|setsaydialog\(saydialogs\.(\w+)\)|GetStoryText\("([^"]+)"\)',txt):
        if m.group(1): cur=m.group(1)
        elif m.group(2):
            dlg=m.group(2)
            if dlg!='character': cur=None
        elif m.group(3):
            rows.append((fn[:-4],m.group(3),cur,dlg))
print("say-lines:",len(rows))
spk=collections.Counter(r[2] for r in rows)
print("distinct speakers:",len(spk))
print("top:",spk.most_common(15))
print("no-speaker:",spk.get(None,0))
json.dump(rows,open("say_rows.json","w",encoding="utf-8"),ensure_ascii=False)
