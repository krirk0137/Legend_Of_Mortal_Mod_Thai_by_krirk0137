"""What the join could NOT fill — i.e. exactly the classes the dictionary architecture
never could express. Small enough to translate by hand."""
import sys, io, json, re, os, collections

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, os.path.join(ROOT, "tools", "v16"))
import xkey  # noqa: E402
sys.path.insert(0, HERE)
from s2_full import to_dict_key, to_native, THAI  # noqa: E402

DICT = os.path.join(ROOT, "Mod", "BepInEx", "Translation", "th", "Text", "Translation zh-CN to en.txt")

kv = json.load(open(os.path.join(HERE, "s2_kv.json"), encoding="utf-8"))
d = xkey.load(DICT)

missing, notthai = [], []
for k, text in kv.items():
    if not text.strip():
        continue
    dk = to_dict_key(text)
    if not dk:
        continue
    hit = d.get(dk)
    if hit is None:
        missing.append((k, text))
    elif not THAI.search(to_native(hit[1])):
        notthai.append((k, text, hit[1]))

print(f"no dictionary entry: {len(missing)}     entry not Thai: {len(notthai)}\n")

PUNCT = re.compile(r"^[\s。，、；：？！…·—\-·.,;:?!'\"()（）《》〈〉【】\[\]{}0-9]+$")
real = [(k, t, v) for k, t, v in notthai if not PUNCT.match(t)]
print(f"of the not-Thai ones, {len(notthai)-len(real)} are punctuation/number only (fine as-is),"
      f" {len(real)} hold real text\n")

print("=== A. no dictionary entry, by namespace ===")
for ns, n in collections.Counter(k.split("/")[0] for k, _ in missing).most_common():
    print(f"  {ns:<28}{n}")
print()
for k, t in sorted(missing):
    print(f"{k}\t{t}")

if real:
    print("\n=== B. dictionary entry exists but holds no Thai ===")
    for k, t, v in sorted(real):
        print(f"{k}\t{t}\t{v}")
