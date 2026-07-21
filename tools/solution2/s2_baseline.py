"""Freeze the current game version's key set, so the NEXT game update produces an exact
work list instead of a guess.

    python tools/solution2/s2_baseline.py <dump.tsv> <game-version>

Stores key + a short hash of the Chinese text — small enough to commit, and enough to tell
"this key is new" from "this key's source text was rewritten, so our Thai is now stale".
That second case is the dangerous one: the key still matches, so nothing looks broken, but
the translation silently describes the old line.
"""
import sys, io, os, hashlib

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
HERE = os.path.dirname(os.path.abspath(__file__))


def read_dump(path):
    rows = {}
    for line in open(path, encoding="utf-8"):
        line = line.rstrip("\n")
        if not line or line.startswith("#"):
            continue
        if "\t" not in line:
            continue
        k, v = line.split("\t", 1)
        rows[k] = v
    return rows


def h(s):
    return hashlib.sha1(s.encode("utf-8")).hexdigest()[:10]


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 1
    dump, version = sys.argv[1], sys.argv[2]
    rows = read_dump(dump)
    out = os.path.join(HERE, "baseline_" + version + ".tsv")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("# key -> sha1(source text)[:10]   game version " + version + "\n")
        for k in sorted(rows):
            f.write(k + "\t" + h(rows[k]) + "\n")
    print(f"{len(rows)} keys -> {out}  ({os.path.getsize(out)/1048576:.1f} MB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
