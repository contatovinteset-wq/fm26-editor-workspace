import json
import re
import os

SPEC_FILE = r"C:\Users\Raphael\Downloads\Allan FCL - Moneyball FM26 (1)\1. Planilha - Moneyball\Moneyball FM26 - Avancados_spec.json"

js_file = r"e:\fm26-editor-workspace-main\fm26-editor-workspace\vintesetFM-website\src\components\ferramentas\MoneyballAvancados.js"
colors_file = r"e:\fm26-editor-workspace-main\fm26-editor-workspace\vintesetFM-website\src\components\ferramentas\moneyball_colors.json"

with open(SPEC_FILE, "r", encoding="utf-8") as f:
    spec = json.load(f)

columns = spec.get("columns", [])
cond_formatting = spec.get("conditional_formatting", [])

# Translation logic for excel formulas to JS
def translate_formula(f_str, all_cols_dict):
    if f_str.startswith("="):
        f_str = f_str[1:]
    else:
        # Check if it was data mapping and substitute it with literal string
        if f_str.startswith("Data"):
            return "undefined" # we handle specific data mappings manually
        return f'"{f_str}"'
        
    def repl(m):
        col = m.group(1)
        return f"calc['{col}']"
    
    f_str = re.sub(r'\b([A-Z]{1,2})\d+\b', repl, f_str)
    
    f_str = f_str.replace("IFERROR(", "IFERROR(")
    f_str = f_str.replace("AVERAGE(L:L)", "0 /* skip average logic */") 
    f_str = f_str.replace("SUBSTITUTE(", "SUBSTITUTE(")
    f_str = f_str.replace("VALUE(", "VALUE(")
    f_str = f_str.replace("FIND(", "FIND(")
    f_str = f_str.replace("LEFT(", "LEFT(")
    f_str = f_str.replace("MID(", "MID(")
    f_str = f_str.replace("ISERROR(", "ISERROR(")
    f_str = f_str.replace("TEXT(", "TEXT(")
    f_str = f_str.replace("IF(", "IF(")
    
    # ISERROR Lazy Evaluation para Javascript Error Throwers
    f_str = re.sub(r"ISERROR\((.*?)\)", r"ISERROR(() => \1)", f_str)
    
    # Substituir concatenação do Excel (&) por (+)
    # Tomar cuidado para não substituir iferror ou outras coisas
    f_str = f_str.replace(' & ', ' + ')
    f_str = f_str.replace('&"º"', '+ "º"')
    
    # Corrigir divisões por zero ou nulas no IFERROR excel sem fallback: =IFERROR(calc['AT']/calc['AR'],)
    f_str = re.sub(r'IFERROR\((.*?),\)', r'IFERROR(\1, 0)', f_str)
    
    # Transformar a igualdade simples do Excel em igualdade JS para não causar SyntaxError
    f_str = re.sub(r'(?<![=<>!])=(?![=])', '==', f_str)

    # Very specific manual JS replacements for things our regex doesn't catch safely, matching exact formulas found earlier
    f_str = f_str.replace('IF(calc[\'ET\']=="-", "Sem Data Final", calc[\'ET\'])', 'IF(calc[\'ET\']=="-", "Sem Data Final", calc[\'ET\'])')
    f_str = f_str.replace('100%-calc[\'BH\']', '1-calc[\'BH\']')
    
    return f_str

# Manually identifying explicit Data sources based on prior debugging
manual_data_sources = {
    "Jogador": "EM",
    "Altura": "EP",
    "Idade": "EN",
    "Salário": "ES",
    "Salrio": "ES", # in case of encoding mess
    "Valor Estimado": "ER"
}

# 1. GENERATE JS SCRIPT
js_lines = [
    "// Auto-generated MoneyballAvancados.js via Spec JSON",
    "// Excel Helpers",
    "const PARSE_GAMES = (val) => {",
    "  let str = String(val || '').trim();",
    "  if (!str) return { total: 0, pctTitular: 0 };",
    "  let hasSub = str.indexOf('(') !== -1;",
    "  if (!hasSub) {",
    "    let t = parseInt(str) || 0;",
    "    return { total: t, pctTitular: t > 0 ? 1 : 0 };",
    "  }",
    "  let starts = parseInt(str.substring(0, str.indexOf('(')).trim()) || 0;",
    "  let subs = parseInt(str.substring(str.indexOf('(') + 1, str.indexOf(')')).trim()) || 0;",
    "  let total = starts + subs;",
    "  return { total, pctTitular: total > 0 ? starts / total : 0 };",
    "};",
    "const ROW = () => 1; // Used for row counting placeholder",
    "const IFERROR = (val, errVal) => {",
    "  if (val === null || val === undefined) return errVal;",
    "  if (typeof val === 'string' && val.includes('%')) val = parseFloat(val.replace('%',''))/100;",
    "  if (isNaN(val) || !isFinite(val)) return errVal;",
    "  return val;",
    "};",
    "const SUBSTITUTE = (text, oldText, newText) => {",
    "  let res = String(text || '').split(oldText).join(newText);",
    "  if (oldText === '.' && newText === ',') return res.replace(',', '.');",
    "  return res;",
    "};",
    "const IF = (cond, t, f) => cond ? t : f;",
    "const VALUE = (text) => parseFloat(String(text || '').replace(',', '.'));",
    "const FIND = (findText, text) => {",
    "    let idx = String(text || '').indexOf(findText);",
    "    if (idx === -1) throw new Error('not found');",
    "    return idx + 1;",
    "};",
    "const LEFT = (text, num) => String(text || '').substring(0, num);",
    "const MID = (text, start, num) => String(text || '').substring(start-1, start-1+num);",
    "const ISERROR = (func) => { try { func(); return false; } catch { return true; } };",
    "const TEXT = (val, format) => {",
    "  if (format === '0%') return Math.round(val * 100) + '%';",
    "  return String(val);",
    "};",
    "",
    "export const processAvancadosRow = (row, index) => {",
    "  const n = (val) => { let v = parseFloat(val); return isNaN(v) ? 0 : v; };",
    "  const g = (r, key, k2) => {",
    "      let ptMap = { 'Golos marcados': 'Gols', 'Golos marcados de fora da área': 'Gols de fora da área', 'Remates': 'Finalizações', 'Fnt': 'Dribles', 'Sofridas': 'Faltas sofridas', 'Compr.': 'Comprimento' };",
    "      let v = r[key] !== undefined ? r[key] : (k2 && r[k2] !== undefined ? r[k2] : undefined);",
    "      if (v === undefined && ptMap[key] !== undefined && r[ptMap[key]] !== undefined) v = r[ptMap[key]];",
    "      if (v === undefined) v = '';",
    "      if (typeof v === 'string') {",
    "          if (v.trim() === '-' || v.trim() === '') return '';",
    "          let asNum = v.replace(',', '.').replace('%', '');",
    "          let n = Number(asNum);",
    "          if (!isNaN(n) && isFinite(n) && asNum.trim() !== '') return v.includes('%') ? n/100 : n;",
    "      }",
    "      return v;",
    "  };",
    "  const calc = {};",
    "  calc['ROW'] = ROW(index);",
    ""
]

# Write Input bindings
all_cols_dict = {}
for c in columns:
    col_str = c["letter"]
    all_cols_dict[col_str] = c

# Process inputs EK -> GH first
js_lines.append("  // Original Mappings based strictly on user output")
for c in columns:
    col = c["letter"]
    name = c["header"]
    esc_name = name.replace("'", "\\'")
    if col >= "EK" and len(col) == 2:
        # Standard exported column data mapped by exact name match
        js_lines.append(f"  calc['{col}'] = g(row, '{esc_name}'); // Input")
    if len(col) == 1 and col == "B": # Jogador mapping exception
        js_lines.append(f"  calc['{col}'] = g(row, 'Jogador', 'Nome');")


js_lines.append("\n  // Computed Values")
js_lines.append("  Object.defineProperties(calc, {")
for c in columns:
    col = c["letter"]
    name = c["header"]
    
    # We only compute those that rely on formulas from the spec
    if len(col) < 2 or (len(col) == 2 and col < "EK"):
        samples = c.get("sample_values", [])
        formula = samples[0] if samples else ""
        escaped_name = name.replace("'", "\\'")
        
        # Check if it's explicitly one of the static fallback mappings
        matched_data = None
        for dName, dCol in manual_data_sources.items():
            if dName in name:
                matched_data = dCol
                break
                
        if matched_data:
            js_lines.append(f"    '{col}': {{ get: function() {{ return this['{matched_data}']; }}, enumerable: true }}, // {escaped_name}")
        elif formula.startswith("="):
            translated = translate_formula(formula, all_cols_dict)
            translated = translated.replace("calc[", "this[")
            if col == 'M':
                js_lines.append(f"    '{col}': {{ get: function() {{ return PARSE_GAMES(this['EV']).total; }}, enumerable: true }}, // {escaped_name}")
            elif col == 'O':
                js_lines.append(f"    '{col}': {{ get: function() {{ return PARSE_GAMES(this['EV']).pctTitular; }}, enumerable: true }}, // {escaped_name}")
            else:
                js_lines.append(f"    '{col}': {{ get: function() {{ return {translated}; }}, enumerable: true }}, // {escaped_name}")
        elif formula.startswith("Data"):
            js_lines.append(f"    '{col}': {{ get: function() {{ return undefined; }}, enumerable: true }}, // Missed data mapping {escaped_name}")
        else:
            js_lines.append(f"  // No formula found for {escaped_name} ({col})")

js_lines.append("  });")

js_lines.append("\n  return {")
# Return array exactly as the JSON output ordered them (avoiding B mapping overwrite, return computed value if not input)
for c in columns:
    col = c["letter"]
    name = c["header"]
    esc_name = name.replace("'", "\\'")
    # We only return columns A to GH until EK natively 
    if len(col) < 2 or (len(col) == 2 and col < "EK"):
        js_lines.append(f"    '{esc_name}': calc['{col}'],")
js_lines.append("  };")
js_lines.append("};")

# Read actual cell formats extracted from Excel
try:
    with open("col_formats.json", "r") as f:
        col_formats = json.load(f)
except:
    col_formats = {}

# Add the getAvancadosHeaders mapping 
js_lines.append("\nexport const getAvancadosHeaders = () => {")
js_lines.append("  return [")
for c in columns:
    col = c["letter"]
    name = c["header"]
    esc_name = name.replace("'", "\\'")
    type_inf = "text"
    
    nm_fmt = col_formats.get(col, "").lower()
    
    # Exclude COL_A completely from headers
    if col == "A":
        continue
        
    if "%" in nm_fmt:
        type_inf = "percentage"
    elif "0.0" in nm_fmt:
        type_inf = "float"
    elif "0" in nm_fmt:
        type_inf = "number" # UI renders ints correctly when formatting

    if len(col) < 2 or (len(col) == 2 and col < "EK"):
         js_lines.append(f"    {{ id: '{esc_name}', type: '{type_inf}' }},")
js_lines.append("  ];")
js_lines.append("};")

# Write to file and clean UTF issues
with open(js_file, "w", encoding="utf-8") as f:
    text = "\n".join(js_lines)
    # Perform strict encoding fixes because openpyxl messed up extracting some names from binary
    fixes = {
        'YZAvanados': 'Avançados',
        'P preferido': 'Pé preferido',
        'Salrio': 'Salário',
        'Mdia': 'Média',
        'rea': 'área',
        'Pnalti': 'Pênalti',
        'Pnaltis': 'Pênaltis',
        'Concluso': 'Conclusão',
        'Cobranas': 'Cobranças',
        'Converso': 'Conversão',
        'Finalizaes': 'Finalizações',
        'Finalizaes': 'Finalizações', # Sometimes represented as missing only one char
        'Finalizao': 'Finalização',
        'Finalizao': 'Finalização',
        'Assistncias': 'Assistências',
        'Eficcia': 'Eficácia',
        'Aes': 'Ações',
        'Aes': 'Ações',
        'Participao': 'Participação',
        'Participao': 'Participação',
        'ltimo': 'último',
        'tero': 'terço',
        'Desperdiada': 'Desperdiçada',
        'Classificao': 'Classificação',
        'Classificao': 'Classificação',
        'Nao': 'Nação',
        'Nao': 'Nação',
        'Distncia': 'Distância'
    }
    
    # Also clean some legacy bad fixes if they exist in the DB or JSON output
    cleanup_legacy = {
        'Áárea': 'Área',
        'áárea': 'área'
    }
    
    for old, new in fixes.items():
        text = text.replace(old, new)
        
    for old, new in cleanup_legacy.items():
        text = text.replace(old, new)
        
    f.write(text)

# 2. GENERATE moneyball_colors.json
colors_dict = {}

def get_col_name(letter):
    for c in columns:
        if c["letter"] == letter:
            return c["header"]
    return None

import ast
for r in cond_formatting:
    col_ref = r["range"].replace("<ConditionalFormatting ", "").split(":")[0] # extract col letter e.g., AC1
    col_letter = "".join(filter(str.isalpha, col_ref))
    header_name = get_col_name(col_letter)
    if not header_name:
        continue
    
    # We fix the encoding on header name
    for bad, good in fixes.items():
        header_name = header_name.replace(bad, good)
        
    rule_type = r["type"]
    extracted_rules = []
    
    if rule_type == "colorScale":
        scale = r.get("colors", [])
        if scale:
            if len(scale) >= 3:
                extracted_rules = [
                    {"type": "percentile", "val": 10, "color": scale[0].replace("#", "") + "FF" if "#" in scale[0] else "FF63BE7B"},
                    {"type": "percentile", "val": 50, "color": scale[1].replace("#", "") + "FF" if "#" in scale[1] else "FFFFEB84"},
                    {"type": "percentile", "val": 90, "color": scale[2].replace("#", "") + "FF" if "#" in scale[2] else "FFF8696B"}
                ]
            elif len(scale) == 2:
                extracted_rules = [
                    {"type": "percentile", "val": 20, "color": scale[0].replace("#", "") + "FF" if "#" in scale[0] else "FF63BE7B"},
                    {"type": "percentile", "val": 80, "color": scale[1].replace("#", "") + "FF" if "#" in scale[1] else "FFF8696B"}
                ]
    elif rule_type == "dataBar":
        # we treat as scale
        extracted_rules = [
            {"type": "percentile", "val": 10, "color": "FFF8696B"},
            {"type": "percentile", "val": 50, "color": "FFFFEB84"},
            {"type": "percentile", "val": 90, "color": "FF63BE7B"}
        ]
        
    if extracted_rules:
        # Prevent overriding with less accurate color matching
        if header_name not in colors_dict:
            colors_dict[header_name] = extracted_rules

with open(colors_file, "w", encoding="utf-8") as f:
    json.dump(colors_dict, f, indent=2, ensure_ascii=False)

print(f"Succefully built MoneyballAvancados.js and moneyball_colors.json!")
