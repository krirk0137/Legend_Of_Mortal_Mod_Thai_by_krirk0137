import os, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
SC = os.path.dirname(os.path.abspath(__file__))
rows = [l.split("\t") for l in
        open(os.path.join(SC, "v16_input.tsv"), encoding="utf-8").read().split("\n")[1:] if l.strip()]
D = os.path.join(SC, "v16")
os.makedirs(D, exist_ok=True)

# DiceHeader is a distinct register (short UI captions) -> its own chunks
dice = [r for r in rows if r[1] == "DiceHeader"]
story = [r for r in rows if r[1] != "DiceHeader"]
print("dice:", len(dice), " other:", len(story))

CAP = 12000   # bytes of source per chunk; well under the output-token ceiling


def write(prefix, items):
    n, cur, size = 0, [], 0
    out = []
    for r in items:
        b = len(r[0].encode("utf-8")) + 40
        if cur and size + b > CAP:
            out.append((f"{prefix}_{n:02d}", cur)); n += 1; cur, size = [], 0
        cur.append(r); size += b
    if cur:
        out.append((f"{prefix}_{n:02d}", cur))
    for name, items2 in out:
        with open(os.path.join(D, name + ".tsv"), "w", encoding="utf-8", newline="\n") as f:
            for r in items2:
                f.write(f"{r[0]}\t{r[2]}\n")
        print(f"  {name}.tsv  {len(items2)} lines  "
              f"{sum(len(x[0].encode('utf-8')) for x in items2)} bytes")
    return [n for n, _ in out]


print("\nDiceHeader chunks:")
a = write("dice", dice)
print("\nStory/misc chunks:")
b = write("story", story)
print("\ntotal chunks:", len(a) + len(b))
