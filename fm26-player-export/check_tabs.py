with open('dump_7.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if "'Passing'" in line or "'Attacking'" in line or "'SetPieces'" in line:
        start = max(0, i - 5)
        end = min(len(lines), i + 10)
        print(f"--- Context for {line.strip()} at line {i+1} ---")
        for j in range(start, end):
            print(f"{j+1}: {lines[j].rstrip()}")
