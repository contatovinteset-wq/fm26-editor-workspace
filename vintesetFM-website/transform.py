import json

data = json.load(open('excel_data.json', encoding='utf-8'))
colors = json.load(open('excel_colors.json', encoding='utf-8'))

cols = {c["col"]: c["name"] for c in data["columns"]}

out = {}
for range_str, rules in colors.items():
    if not range_str.startswith("<ConditionalFormatting "): continue
    range_val = range_str.replace("<ConditionalFormatting ", "").replace(">", "")
    parts = range_val.split(":")
    if not parts: continue
    col_start = "".join([c for c in parts[0] if c.isalpha()])
    
    # get the rules
    c_rules = [r for r in rules if r.get("colorScale")]
    if not c_rules: continue
    c_rule = c_rules[0]["colorScale"]
    
    col_name = cols.get(col_start)
    if col_name:
        out[col_name] = c_rule

with open('src/components/ferramentas/moneyball_colors.json', 'w', encoding='utf-8') as f:
    json.dump(out, f, ensure_ascii=False, indent=2)

print("Transformed!")
