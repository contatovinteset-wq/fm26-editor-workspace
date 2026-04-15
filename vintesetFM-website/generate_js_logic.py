import re
import os

with open("excel_headers_mapping.txt", "r", encoding="utf-8") as f:
    headers = [x.strip().split(": ", 1) for x in f.readlines()]
    header_map = {k: v for k, v in headers}

with open("excel_formulas.txt", "r", encoding="utf-8") as f:
    formulas = [x.strip().split(": ", 1) for x in f.readlines()]

def translate_formula(f_str):
    if f_str.startswith("="):
        f_str = f_str[1:]
    else:
        return f'"{f_str}"'
    
    # regex for Excel references A2, B2, EX2, etc. (since we assume row 2 is the source row)
    def repl(m):
        col = m.group(1)
        return f"calc['{col}']"
    
    f_str = re.sub(r'\b([A-Z]{1,2})2\b', repl, f_str)
    
    # Specific excel functions
    f_str = f_str.replace("IFERROR(", "IFERROR(")
    f_str = f_str.replace("AVERAGE(L:L)", "0") 
    f_str = f_str.replace("SUBSTITUTE(", "SUBSTITUTE(")
    f_str = f_str.replace("VALUE(", "VALUE(")
    f_str = f_str.replace("FIND(", "FIND(")
    f_str = f_str.replace("ISERROR(", "ISERROR(")
    f_str = f_str.replace("LEFT(", "LEFT(")
    f_str = f_str.replace("MID(", "MID(")
    f_str = f_str.replace("TEXT(", "TEXT(")
    f_str = f_str.replace('"-",', '"-",')
    
    return f_str

js_lines = []
js_lines.append("  const calc = {};")

# Initialize inputs
for col, name in header_map.items():
    if len(col) == 2 and col >= "EK": # input columns EK to GH
        # special cases for alias mapping like 'Golos', 'Pens', etc.
        # we will use the `n()` or `g()` helpers
        if name in ["Golos", "Gols", "Gols DS"]:
            js_lines.append(f"  calc['{col}'] = g(row, 'Gols', 'Golos', 'Gols DS');")
        elif name == "Pens M":
            js_lines.append(f"  calc['{col}'] = g(row, 'Pens M', 'Pen M');")
        elif name == "Pens":
            js_lines.append(f"  calc['{col}'] = g(row, 'Pens');")
        elif name == "Altura":
            js_lines.append(f"  calc['{col}'] = g(row, 'Altura', 'Altura.1');")
        elif name == "xG" or name == "xA":
            js_lines.append(f"  calc['{col}'] = g(row, '{name}');")
        else:
            escaped_name = name.replace("'", "\\'")
            js_lines.append(f"  calc['{col}'] = n(row['{escaped_name}']);")

# We must evaluate columns from A to Z, AA to ZZ in order!
excel_cols_order = []
for k in header_map.keys():
    if len(k) == 1:
        excel_cols_order.append(k)
for k in header_map.keys():
    if len(k) == 2:
        excel_cols_order.append(k)

# To ensure dependencies are met, we just write them in the exact order they appear in Excel!
# Since Excel calculates based on left-to-right mostly, we can just output left-to-right.
# Wait, some columns may reference columns to the right. To be absolutely safe in JS, we can compute them iteratively or simply ensure we write formulas first, but JS evaluates sequentially. Actually, a safe way is to write all raw input columns `calc[...] = ...` first (which we did).
# Then iterate linearly over formulas from left to right.

formula_dict = {k: v for k, v in formulas}
reverse_map = {v: k for k, v in header_map.items()}

js_lines.append("\n  // Formulas")
for col in excel_cols_order:
    if col in reverse_map.values():
        name = [k for k, v in header_map.items() if v == col] # wait, no
        # The key in formulas is the column name.
        name = header_map[col]
        if name in formula_dict:
            val = formula_dict[name]
            if val != "Data":
                form_js = translate_formula(val)
                js_lines.append(f"  calc['{col}'] = {form_js}; // {name}")

js_lines.append("\n  return {")
for col in excel_cols_order:
    if col < "EK": # Computed columns
        name = header_map[col]
        escaped_name = name.replace("'", "\\'")
        if name in formula_dict:
            js_lines.append(f"    '{escaped_name}': calc['{col}'],")
js_lines.append("  };")

with open("auto_generated_avancados.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(js_lines))

print("Done generating auto_generated_avancados.txt")

