import sys

def extract_subtree(filepath, target_name):
    lines = open(filepath, 'r', encoding='utf-8', errors='ignore').readlines()
    in_target = False
    target_indent = 0
    subtree = []
    
    for line in lines:
        if "VisualElement" in line:
            parts = line.split("VisualElement '")
            if len(parts) > 1:
                indent_str = parts[0].split("[FM26Dump]")[1]
                indent = len(indent_str)
                name = parts[1].split("'")[0]
                
                if not in_target:
                    if target_name in name:
                        in_target = True
                        target_indent = indent
                        subtree.append(line.strip())
                else:
                    if indent <= target_indent:
                        break
                    subtree.append(line.strip())
    
    print("\n".join(subtree))

extract_subtree('dump_7.txt', 'MatchStatsStandAlone')
