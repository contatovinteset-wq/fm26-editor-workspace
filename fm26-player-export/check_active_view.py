import sys

with open('dump_7.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

in_view = False
view_indent = 0
found_active = False

for i, line in enumerate(lines):
    if not in_view:
        if "VisualElement 'View'" in line:
            # Check if next line is deeper indent
            indent = len(line) - len(line.lstrip())
            if i+1 < len(lines):
                next_indent = len(lines[i+1]) - len(lines[i+1].lstrip())
                if next_indent > indent:
                    # It has children!
                    print(f"--- ACTIVE VIEW FOUND at line {i+1} ---")
                    in_view = True
                    view_indent = indent
                    found_active = True
    else:
        indent = len(line) - len(line.lstrip())
        if indent <= view_indent:
            in_view = False
        else:
            print(f"{i+1}: {line.rstrip()}")

if not found_active:
    print("No active view found.")
