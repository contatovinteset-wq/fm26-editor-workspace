import sys

with open("dump_7.txt", "r", encoding="utf-16le", errors="ignore") as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if "StreamedTable" in line:
        print(f"[{i}] {line.strip()}")
        # print next 30 lines
        for j in range(1, 40):
            print(f"[{i+j}] {lines[i+j].strip()}")
        break
