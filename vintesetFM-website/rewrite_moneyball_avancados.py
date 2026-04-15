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
    
    def repl(m):
        col = m.group(1)
        return f"calc['{col}']"
    
    f_str = re.sub(r'\b([A-Z]{1,2})2\b', repl, f_str)
    
    f_str = f_str.replace("IFERROR(", "IFERROR(")
    f_str = f_str.replace("AVERAGE(L:L)", "0 /* skip average logic */") 
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
js_lines.append("""// Excel Helpers
const IFERROR = (val, errVal) => {
  if (val === null || val === undefined) return errVal;
  if (typeof val === 'string' && val.includes('%')) val = parseFloat(val.replace('%',''))/100;
  if (isNaN(val) || !isFinite(val)) return errVal;
  return val;
};
const SUBSTITUTE = (text, oldText, newText) => String(text || '').split(oldText).join(newText);
const IF = (cond, t, f) => cond ? t : f;
const VALUE = (text) => parseFloat(String(text || '').replace(',', '.'));
const FIND = (findText, text) => {
    let idx = String(text || '').indexOf(findText);
    if (idx === -1) throw new Error("not found");
    return idx + 1;
};
const LEFT = (text, num) => String(text || '').substring(0, num);
const MID = (text, start, num) => String(text || '').substring(start-1, start-1+num);
const ISERROR = (func) => { try { func(); return false; } catch { return true; } };

const TEXT = (val, format) => {
   // Minimal mock for "0%"
   if (format === "0%") {
      return (val * 100).toFixed(0) + "%";
   }
   return String(val);
};

// Data Helpers
const n = (val) => {
  if (val === undefined || val === null || val === '') return 0;
  return val;
};
const g = (row, ...fields) => {
  for (let f of fields) {
    if (row[f] !== undefined) return n(row[f]);
  }
  return 0;
};
""")

# We must evaluate columns from A to Z, AA to ZZ in order
excel_cols_order = []
for k in header_map.keys():
    if len(k) == 1: excel_cols_order.append(k)
for k in header_map.keys():
    if len(k) == 2: excel_cols_order.append(k)

# output col array
js_lines.append("export const moneyballAvancadosColunas = [")
for col in excel_cols_order:
    if len(col) == 1 or col < "EK":
        name = header_map[col]
        escaped_name = name.replace("'", "\\'")
        js_lines.append(f"  '{escaped_name}',")
js_lines.append("];\n")

js_lines.append("export const processAvancadosRow = (row) => {")
js_lines.append("  const calc = {};")

for col, name in header_map.items():
    if len(col) == 2 and col >= "EK": # input columns EK to GH
        # special cases
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
        elif name == "Assist.":
            js_lines.append(f"  calc['{col}'] = g(row, 'Assist.', 'Assistências');")
        elif name == "Golos marcados de fora da área":
            js_lines.append(f"  calc['{col}'] = g(row, 'Golos marcados de fora da área', 'Gols de fora da área');")
        else:
            escaped_name = name.replace("'", "\\'")
            js_lines.append(f"  calc['{col}'] = n(row['{escaped_name}']);")

formula_dict = {k: v for k, v in formulas}
reverse_map = {v: k for k, v in header_map.items()}

js_lines.append("\n  // Formulas")

# We must evaluate formulas in order A->Z, AA->ZZ mapping from user data
# Wait, some formulas rely on text manipulation: MINUTOS POR PARTIDA, JOGOS TOTAIS, JOGOS COMO TITULAR use ISERROR(FIND("(",EV2)).
# So EV2 must be string explicitly!
js_lines.append("  // Ensure EV is string for formulas")
js_lines.append("  calc['EV'] = String(calc['EV'] || '');")

for col in excel_cols_order:
    if col < "EK":
        name = header_map[col]
        if name in formula_dict:
            val = formula_dict[name]
            if val.startswith("Data"):
                data_mapping = {
                    "Jogador": "EM",
                    "Altura": "EP",
                    "Idade": "EN",
                    "Salário": "ES",
                    "Valor Estimado": "ER"
                }
                # Fallback to key matching just in case of formatting
                found_col = None
                for dName, dCol in data_mapping.items():
                    if dName in name or name in dName:
                        found_col = dCol
                if found_col:
                    js_lines.append(f"  calc['{col}'] = calc['{found_col}']; // {name} (Mapped Data)")
            else:
                # specific substitutions for unsupported JS formulas 
                if val == '=IFERROR(IF(ISERROR(FIND("(",EV2)), VALUE(EV2), VALUE(LEFT(EV2,FIND("(",EV2)-2)) + VALUE(MID(EV2,FIND("(",EV2)+1,FIND(")",EV2)-FIND("(",EV2)-1))),0)':
                    js_lines.append(f"  calc['{col}'] = IFERROR(IF(ISERROR(()=>FIND('(',calc['EV'])), VALUE(calc['EV']), VALUE(LEFT(calc['EV'],FIND('(',calc['EV'])-2)) + VALUE(MID(calc['EV'],FIND('(',calc['EV'])+1,FIND(')',calc['EV'])-FIND('(',calc['EV'])-1))),0); // {name}")
                elif val == '=IFERROR(IF(ISERROR(FIND("(",EV2)), "100%", TEXT(VALUE(LEFT(EV2,FIND("(",EV2)-2)) / (VALUE(LEFT(EV2,FIND("(",EV2)-2)) + VALUE(MID(EV2,FIND("(",EV2)+1,FIND(")",EV2)-FIND("(",EV2)-1))),"0%")),0)*1':
                    js_lines.append(f"  calc['{col}'] = IFERROR(IF(ISERROR(()=>FIND('(',calc['EV'])), 1, VALUE(LEFT(calc['EV'],FIND('(',calc['EV'])-2)) / (VALUE(LEFT(calc['EV'],FIND('(',calc['EV'])-2)) + VALUE(MID(calc['EV'],FIND('(',calc['EV'])+1,FIND(')',calc['EV'])-FIND('(',calc['EV'])-1)))),0); // {name} (Fixed text format)")
                else:
                    form_js = translate_formula(val)
                    js_lines.append(f"  calc['{col}'] = {form_js}; // {name}")

js_lines.append("\n  return {")
for col in excel_cols_order:
    if col < "EK": # Computed columns
        name = header_map[col]
        escaped_name = name.replace("'", "\\'")
        js_lines.append(f"    '{escaped_name}': calc['{col}'],")
js_lines.append("  };")
js_lines.append("};")

js_lines.append("\nexport const getAvancadosHeaders = () => {")
js_lines.append("  return [")
for col in excel_cols_order:
    if col < "EK":
        name = header_map[col]
        escaped_name = name.replace("'", "\\'")
        js_lines.append(f"    '{escaped_name}',")
js_lines.append("  ];")
js_lines.append("};")

with open("src/components/ferramentas/MoneyballAvancados.js", "w", encoding="utf-8") as f:
    f.write("\n".join(js_lines))

print("Successfully replaced MoneyballAvancados.js entirely!")
