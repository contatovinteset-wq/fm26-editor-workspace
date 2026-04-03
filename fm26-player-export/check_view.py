with open('dump_7.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if "StreamedTable" in line:
        print(f"--- StreamedTable found at line {i+1} ---")
        # print next 20 lines to see if View has children
        for j in range(20):
            if i + j < len(lines):
                print(f"{i+j+1}: {lines[i+j].rstrip()}")
