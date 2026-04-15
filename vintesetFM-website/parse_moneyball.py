import pandas as pd
import json

file_path = r"c:\Users\Raphael\Downloads\Allan FCL - Moneyball FM26 (1)\1. Planilha - Moneyball\Moneyball FM26.xlsm"

try:
    # Read just the first few rows to get headers
    df = pd.read_excel(file_path, sheet_name=0, nrows=5)
    
    # Dump headers B to CK
    headers = list(df.columns)
    
    print("Found headers:", headers)
    
except Exception as e:
    print("Error:", e)
