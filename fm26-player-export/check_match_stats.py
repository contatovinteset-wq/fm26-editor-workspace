import sys

with open('dump_7.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

in_match_stats = False
match_stats_indent = 0

for i, line in enumerate(lines):
    if "MatchStatsStandAlone" in line:
        in_match_stats = True
        match_stats_indent = len(line) - len(line.lstrip())
        print(f"--- MATCH STATS FOUND at line {i+1} ---")
        continue

    if in_match_stats:
        indent = len(line) - len(line.lstrip())
        if indent <= match_stats_indent and line.strip() != "":
            break
        print(f"{i+1}: {line.rstrip()}")

