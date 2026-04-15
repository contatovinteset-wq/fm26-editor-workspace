import openpyxl
import json

file_path = r"c:\Users\Raphael\Downloads\Allan FCL - Moneyball FM26 (1)\1. Planilha - Moneyball\Moneyball FM26.xlsm"

try:
    wb = openpyxl.load_workbook(file_path, data_only=False, read_only=True)
    sheet = wb.worksheets[0]
    
    headers = []
    formulas = []
    
    for row in sheet.iter_rows(min_row=1, max_row=5, values_only=False):
        if row[0].row == 1:
            headers = [cell.value for cell in row]
        elif row[0].row == 5:
            formulas = [cell.value for cell in row]
            
    header_formula_map = []
    for h, f in zip(headers, formulas):
        if h and str(h).strip() != '':
            header_formula_map.append({"Header": str(h), "Formula/Value": str(f)})
            
    with open("moneyball_logic.json", "w", encoding="utf-8") as file:
        json.dump(header_formula_map, file, indent=2, ensure_ascii=False)
        
    print("Logic dumped to moneyball_logic.json")
    
except Exception as e:
    print("Error:", e)
