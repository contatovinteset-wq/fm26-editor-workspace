with open('dump_1.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

out = []
for i, line in enumerate(lines):
    if "PersonSearch" in line or "table" in line.lower() or "list" in line.lower() or "Staff" in line:
        out.append(f"{i}: {line.strip()}\n")

with open('table_out8.txt', 'w', encoding='utf-8') as f:
    f.writelines(out)
