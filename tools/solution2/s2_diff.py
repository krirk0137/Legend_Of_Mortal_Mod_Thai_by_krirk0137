"""What a game update actually changed — the whole re-translation work list, exactly.

    python tools/solution2/s2_diff.py baseline_<old>.tsv <new dump.tsv>

After the game updates: launch it once (the plugin auto-writes BepInEx/LomThaiText_dump.tsv),
then run this. Three buckets come out:

  ADDED    key did not exist before                 -> needs a fresh translation
  CHANGED  key exists but its Chinese was rewritten -> our Thai is now STALE and must be redone
  REMOVED  key is gone                              -> drop it from Thai.tsv (harmless if left)

The CHANGED bucket is the one the old text-keyed dictionary could never surface: there, a
rewritten line simply stopped matching and fell through to Google, silently.
"""
import sys, io, os, hashlib

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
HERE = os.path.dirname(os.path.abspath(__file__))


def read_tsv(path):
    rows = {}
    for line in open(path, encoding="utf-8"):
        line = line.rstrip("\n")
        if not line or line.startswith("#") or "\t" not in line:
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
    old = read_tsv(sys.argv[1])
    new = read_tsv(sys.argv[2])
    newh = {k: h(v) for k, v in new.items()}

    added = sorted(set(newh) - set(old))
    removed = sorted(set(old) - set(newh))
    changed = sorted(k for k in newh if k in old and newh[k] != old[k])

    print(f"baseline {len(old)} keys   new {len(newh)} keys")
    print(f"  ADDED   {len(added)}")
    print(f"  CHANGED {len(changed)}   <- our Thai is stale for these")
    print(f"  REMOVED {len(removed)}")

    out = os.path.join(HERE, "update_worklist.tsv")
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("# bucket\tkey\tnew source text\n")
        for k in added:
            f.write("ADDED\t" + k + "\t" + new[k] + "\n")
        for k in changed:
            f.write("CHANGED\t" + k + "\t" + new[k] + "\n")
        for k in removed:
            f.write("REMOVED\t" + k + "\t\n")
    print(f"\nwrote {out}")

    for label, keys in (("ADDED", added), ("CHANGED", changed)):
        if keys:
            print(f"\nsample {label}:")
            for k in keys[:8]:
                print(f"  {k:<44}{new.get(k,'')[:60]}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
