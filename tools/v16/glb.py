import json, struct, sys, io, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
p = sys.argv[1]
pat = sys.argv[2] if len(sys.argv) > 2 else "log"
d = open(p, 'rb').read()
assert d[:4] == b'glTF', d[:8]
off = 12
js = None
while off < len(d):
    ln, ty = struct.unpack_from('<II', d, off)
    if ty == 0x4E4F534A:
        js = json.loads(d[off + 8:off + 8 + ln].decode('utf-8'))
        break
    off += 8 + ln
nodes = js.get("nodes", [])
print("nodes:", len(nodes))
parent = {}
for i, n in enumerate(nodes):
    for c in n.get("children", []):
        parent[c] = i


def path(i):
    out = []
    seen = set()
    while i is not None and i not in seen:
        seen.add(i)
        out.append(nodes[i].get("name", "?"))
        i = parent.get(i)
    return "/".join(reversed(out))


rx = re.compile(pat, re.I)
hits = [i for i, n in enumerate(nodes) if rx.search(n.get("name", ""))]
print(f"nodes matching /{pat}/i: {len(hits)}")
for i in hits[:40]:
    print("  ", path(i))
