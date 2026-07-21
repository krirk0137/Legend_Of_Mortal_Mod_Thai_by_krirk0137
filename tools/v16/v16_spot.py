"""Spot-check the classes cov3 still reports as missing - are they really missing?"""
import sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from xkey import load

DICT = r"C:/ClaudeCode/LegendOfMortalENG/Mod/BepInEx/Translation/th/Text/Translation zh-CN to en.txt"
dk = load(DICT)
probe = [
    ("punch stripped", "嗡嗡嗡嗡嗡。"),
    ("punch+size", "<size\\=50>嵩山寺十八铜人！喝！</size>"),
    ("unlock cond", "解锁条件：银两>\\=500"),
    ("unlock cond 2", "解锁条件：轻功>\\=40"),
    ("unlock bracket", "解锁条件：［学问］>\\=40"),
    ("color raw", "绝招：消耗大量的气力，造成强大的伤害。<color\\=#FF0000>可中断捅人、暗器行动</color>"),
]
for label, k in probe:
    hit = dk.get(k)
    print(f"{label:<16}{'FOUND L%d' % hit[0] if hit else 'not found':<14}{k[:46]}")
    if hit:
        print(f"{'':<16}-> {hit[1][:70]}")

# any dictionary line whose KEY contains a raw TAB (would break the TSV pipeline)
bad = [(ln, k) for k, (ln, v) in dk.items() if "\t" in k]
print(f"\ndict keys containing a raw TAB: {len(bad)}")
for ln, k in bad[:5]:
    print(f"   L{ln} {k[:70]!r}")

# and the specific line the game emits with a tab
t = "不过盘缠还是得省着花的，免得真要动身上路时穷得叮当响，\t一路直接上西天。"
print("\ntab-containing game string present as a key:", t in dk)
print("same string with the tab removed present:", t.replace("\t", "") in dk)
print("same string with tab -> space present:", t.replace("\t", " ") in dk)
