import openpyxl
import json
import warnings
warnings.simplefilter("ignore")

wb = openpyxl.load_workbook('c:/Users/Raphael/Downloads/Allan FCL - Moneyball FM26 (1)/1. Planilha - Moneyball/Moneyball FM26 - Avancados.xlsm', data_only=False)
sheet = wb.active
colors_info = {}

for rule_range, rules in sheet.conditional_formatting._cf_rules.items():
    rule_list = []
    for rule in rules:
        r = {"type": rule.type}
        if rule.colorScale:
            scales = []
            for i, cfvo in enumerate(rule.colorScale.cfvo):
                # sometimes color list is shorter than cfvo if it's default
                try:
                    c = rule.colorScale.color[i]
                    color_val = c.rgb if hasattr(c, 'rgb') and c.rgb else c.theme
                except:
                    color_val = "unknown"
                scales.append({
                    "type": cfvo.type,
                    "val": cfvo.val,
                    "color": str(color_val)
                })
            r["colorScale"] = scales
        rule_list.append(r)
    colors_info[str(rule_range)] = rule_list

with open('excel_colors.json', 'w') as f:
    json.dump(colors_info, f, indent=2)
print("Done extracting colors!")
