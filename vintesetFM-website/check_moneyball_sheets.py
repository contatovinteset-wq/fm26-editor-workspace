import openpyxl
import json

file_path = r"c:\Users\Raphael\Downloads\Allan FCL - Moneyball FM26 (1)\1. Planilha - Moneyball\Moneyball FM26.xlsm"

try:
    wb = openpyxl.load_workbook(file_path, data_only=True, read_only=True)
    with open("moneyball_sheets.json", "w", encoding="utf-8") as file:
        json.dump(wb.sheetnames, file, ensure_ascii=False)
except Exception as e:
    import traceback
    traceback.print_exc()
