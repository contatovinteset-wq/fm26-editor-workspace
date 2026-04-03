with open('LogOutput.log', 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()

dump_count = 0
out = []
for line in lines:
    if "F6 pressionado - iniciando dump" in line:
        if dump_count > 0:
            with open(f'dump_{dump_count}.txt', 'w', encoding='utf-8') as out_f:
                out_f.writelines(out)
        dump_count += 1
        out = []
    if dump_count > 0:
        out.append(line)

if dump_count > 0:
    with open(f'dump_{dump_count}.txt', 'w', encoding='utf-8') as out_f:
        out_f.writelines(out)
print(f"Split into {dump_count} files.")
