import json,sys,io,collections
sys.stdout=io.TextIOWrapper(sys.stdout.buffer,encoding='utf-8',errors='replace')
l2s=json.load(open("line2sp.json",encoding="utf-8"))
gs=json.load(open("gender_signals.json",encoding="utf-8"))
FEMALE={"girl1","girl2","girl2_5","girl4","girl4_2","girl5","girl5_3","girl5_5","girl6","girl6_1","girl7","girl7_2","girl8","girl9","sister1","special810","special813","special815","special818","special832","lady1","women1","women3","women4","women411","aunt1","aunt2","aunt3","showgirl1","female1","ranger_girl1","fairy1","trainee_girl1","trainee_girl2","trainee_girl3","trainee_girl6","trainee_girl7","trainee_girl8","big_trainee_girl_1"}
OUT=r"C:/ClaudeCode/LegendOfMortalENG/tools/speaker_map.tsv"
with open(OUT,"w",encoding="utf-8",newline="\n") as f:
    f.write("#dict_line\tn_speakers\tspeaker_tags\tnames\tgender\n")
    for k in sorted(l2s,key=int):
        sp=l2s[k]
        names="|".join(str(gs.get(s,{}).get("name")) for s in sp)
        gl={"F" if s in FEMALE else "M?" for s in sp}
        g="F" if gl=={"F"} else ("M?" if gl=={"M?"} else "MIX")
        f.write(f"{int(k)+1}\t{len(sp)}\t{'|'.join(sp)}\t{names}\t{g}\n")
print("wrote",OUT)
G=r"C:/ClaudeCode/LegendOfMortalENG/tools/speaker_gender.tsv"
with open(G,"w",encoding="utf-8",newline="\n") as f:
    f.write("#tag\tname\tportrait_folder\tgender\tsignals\tshe\the\n")
    for t in sorted(gs):
        o=gs[t]
        f.write(f"{t}\t{o['name']}\t{o['folder']}\t{'F' if t in FEMALE else 'M?'}\t{' '.join(o['sig'])}\t{o['she']}\t{o['he']}\n")
print("wrote",G)
