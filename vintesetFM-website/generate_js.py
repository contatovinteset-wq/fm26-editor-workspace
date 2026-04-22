import re

excel_to_js = {
    'ROW()-1 & "º"': 'ROW(index)-1 + "º"',
    'CP': 'g(row, "Jogador")',
    'CM': 'g(row, "NAC")',
    'CR': 'g(row, "Pé preferido")',
    'CN': 'g(row, "Clube")',
    'CO': 'g(row, "Idade")',
    'CQ': 'g(row, "Valor Estimado")',
    'CT': 'g(row, "Altura")',
    'CU': 'g(row, "Expira")',
    'CS': 'g(row, "Salário")',
    'CW': 'g(row, "Minutos")',
    'CV': 'g(row, "HdJ")',
    'DL': 'g(row, "Pas A")',
    'DM': 'g(row, "Ps C")',
    'DJ': 'g(row, "Passes em progressão")', # Not an input column actually... Wait, passes em progressao? 
    # Let me check the raw inputs from the output.
    # The raw inputs are at the bottom of the output of the previous script.
}

