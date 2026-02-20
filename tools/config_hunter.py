#!/usr/bin/env python3
"""
FM26 Config Hunter - Busca configurações de gameplay
Procura por padrões de taxas, multiplicadores e configurações
"""

import re
from pathlib import Path

def hunt_configs():
    """Busca configurações em arquivos do jogo"""
    
    metadata_path = Path("/data/.openclaw/workspace/fm26-game-files/fm_Data/il2cpp_data/Metadata/global-metadata.dat")
    
    with open(metadata_path, 'rb') as f:
        data = f.read()
    
    # Extrair strings
    strings = []
    current = b''
    for byte in data:
        if 32 <= byte < 127:
            current += bytes([byte])
        else:
            if len(current) > 6:
                try:
                    s = current.decode('utf-8', errors='replace')
                    strings.append(s)
                except:
                    pass
            current = b''
    
    # Padrões de busca
    patterns = {
        'Injury': ['injury', 'injured', 'fitness', 'physio', 'treatment'],
        'Transfer': ['transfer', 'wage', 'salary', 'fee', 'clause', 'release'],
        'Development': ['development', 'growth', 'potential', 'ability', 'ca', 'pa'],
        'Newgen': ['newgen', 'regen', 'youth', 'academy', 'intake'],
        'Match': ['match', 'tactic', 'formation', 'strategy', 'instruction'],
        'Training': ['training', 'schedule', 'focus', 'intensity', 'workload'],
        'Finance': ['finance', 'budget', 'revenue', 'expense', 'profit'],
    }
    
    print("="*60)
    print("FM26 CONFIG HUNTER - Análise de Metadados")
    print("="*60)
    
    for category, keywords in patterns.items():
        print(f"\n=== {category.upper()} ===")
        found = set()
        for s in strings:
            s_lower = s.lower()
            if any(kw in s_lower for kw in keywords):
                # Filtrar resultados relevantes
                if len(s) < 80 and not s.startswith('<'):
                    # Ignorar strings muito genéricas
                    if not any(skip in s_lower for skip in ['unity', 'debug', 'test', 'editor']):
                        found.add(s)
        
        # Mostrar top 15
        for item in sorted(found)[:15]:
            print(f"  {item}")
        
        if len(found) > 15:
            print(f"  ... e mais {len(found) - 15} referências")
    
    # Buscar números específicos que podem ser taxas
    print("\n" + "="*60)
    print("NÚMEROS POTENCIALMENTE IMPORTANTES")
    print("="*60)
    
    # Padrões como "0.5", "50%", "1.0"
    number_patterns = [
        r'\b\d+\.\d+\b',  # Decimal
        r'\b\d+%\b',       # Porcentagem
        r'\brate\b',       # Rate
        r'\bfactor\b',     # Factor
        r'\bmultiplier\b', # Multiplier
    ]
    
    # Salvar resultados
    output_path = Path("/data/.openclaw/workspace/fm26-editor-workspace/config-analysis.txt")
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write("FM26 CONFIG ANALYSIS\n")
        f.write("="*60 + "\n\n")
        
        for category, keywords in patterns.items():
            f.write(f"\n{category.upper()}\n")
            f.write("-"*40 + "\n")
            
            found = set()
            for s in strings:
                s_lower = s.lower()
                if any(kw in s_lower for kw in keywords):
                    if len(s) < 80 and not s.startswith('<'):
                        found.add(s)
            
            for item in sorted(found):
                f.write(f"{item}\n")
    
    print(f"\nAnálise completa salva em: {output_path}")

if __name__ == '__main__':
    hunt_configs()
