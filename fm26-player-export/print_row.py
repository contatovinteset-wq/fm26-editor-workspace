import sys

with open("dump_7.txt", "r", encoding="utf-16le", errors="ignore") as f:
    lines = f.readlines()

in_view = False
view_indent = 0
for i, line in enumerate(lines):
    if "VisualElement 'View'" in line:
        in_view = True
        view_indent = len(line) - len(line.lstrip())
        print(f"FOUND VIEW AT LINE {i}")
        for j in range(i, i+50):
            print(f"{j}: {lines[j].strip()}")
        break
