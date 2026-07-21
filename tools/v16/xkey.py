"""XUnity dictionary key/value split: the separator is the first UNESCAPED '='.

A naive line.find('=') is WRONG for any key containing an escaped '=', e.g.
    <size\=24>text</size>=<size\=24>ไทย</size>
where it yields the key '<size\' instead of the whole tagged string.
"""


def split(line):
    i = 0
    while i < len(line):
        if line[i] == "\\":
            i += 2
            continue
        if line[i] == "=":
            return line[:i], line[i + 1:]
        i += 1
    return None, None


def load(path, first_wins=True):
    """path -> {key: (1-based line number, value)}"""
    out = {}
    for i, l in enumerate(open(path, encoding="utf-8")):
        l = l.rstrip("\n")
        if not l or l.startswith("//"):
            continue
        k, v = split(l)
        if k is None:
            continue
        if first_wins and k in out:
            continue
        out[k] = (i + 1, v)
    return out
