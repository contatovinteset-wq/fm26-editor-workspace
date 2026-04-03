import glob

for filename in glob.glob('dump_*.txt'):
    with open(filename, 'r', encoding='utf-8') as f:
        for i, line in enumerate(f):
            if "StreamedTable" in line:
                print(f"{filename}:{i+1} : {line.strip()}")
