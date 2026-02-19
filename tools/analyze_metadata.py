#!/usr/bin/env python3
"""
Analisador de global-metadata.dat do FM26 (IL2CPP)
Extrai classes, métodos, campos e strings do jogo.
"""

import struct
import sys
from pathlib import Path

def read_uint32(f):
    return struct.unpack('<I', f.read(4))[0]

def read_int32(f):
    return struct.unpack('<i', f.read(4))[0]

def read_string(f, offset):
    """Lê string null-terminated do offset"""
    old_pos = f.tell()
    f.seek(offset)
    chars = []
    while True:
        c = f.read(1)
        if c == b'\x00' or not c:
            break
        chars.append(c.decode('utf-8', errors='replace'))
    f.seek(old_pos)
    return ''.join(chars)

def analyze_metadata(filepath):
    print(f"Analisando: {filepath}")
    print("=" * 60)
    
    with open(filepath, 'rb') as f:
        # Header do global-metadata.dat
        magic = f.read(4)
        print(f"Magic: {magic}")
        
        version = read_int32(f)
        print(f"Versão: {version}")
        
        string_literal_offset = read_int32(f)
        string_literal_size = read_int32(f)
        string_literal_data_offset = read_int32(f)
        string_literal_data_size = read_int32(f)
        
        string_offset = read_int32(f)
        string_size = read_int32(f)
        
        events_offset = read_int32(f)
        events_size = read_int32(f)
        
        # Pular para ler mais campos...
        f.seek(0)
        header_data = f.read(200)
        
        print(f"\nOffset de strings: {string_offset}")
        print(f"Tamanho de strings: {string_size}")
        
        # Extrair algumas strings
        print("\n--- Strings encontradas (amostra) ---")
        f.seek(string_offset)
        string_data = f.read(min(string_size, 500000))
        
        # Encontrar strings legíveis
        strings = []
        current = b''
        for byte in string_data:
            if 32 <= byte < 127:
                current += bytes([byte])
            else:
                if len(current) > 10:
                    try:
                        s = current.decode('utf-8', errors='replace')
                        if s and not s.startswith('_') and len(s) > 5:
                            strings.append(s)
                    except:
                        pass
                current = b''
        
        # Mostrar strings interessantes
        keywords = ['player', 'team', 'match', 'injury', 'training', 'contract', 
                    'transfer', 'tactic', 'formation', 'stadium', 'finance',
                    'database', 'save', 'load', 'config', 'setting',
                    'brazil', 'brasil', 'brazilian', 'libertadores',
                    'FM', 'SI', 'Sports', 'Interactive']
        
        print("\nStrings com palavras-chave:")
        for s in strings[:500]:
            s_lower = s.lower()
            if any(kw in s_lower for kw in keywords):
                print(f"  - {s}")
        
        # Buscar classes/métodos específicos
        print("\n--- Buscando referências importantes ---")
        f.seek(string_offset)
        all_strings = f.read(string_size)
        
        important_patterns = [
            b'MatchEngine', b'InjuryRate', b'TransferValue', b'Newgen',
            b'Brazil', b'Brasil', b'Libertadores', b'SerieA', b'Brasileirao',
            b'TeamID', b'PlayerID', b'ClubID', b'PersonID',
            b'SkinPath', b'UITheme', b'UIColor',
            b'SavePath', b'LoadGame', b'DatabasePath'
        ]
        
        for pattern in important_patterns:
            count = all_strings.count(pattern)
            if count > 0:
                print(f"  '{pattern.decode()}' encontrado {count} vezes")
        
        # Mostrar todas as strings únicas interessantes
        print("\n--- Todas as strings únicas (primeiras 100) ---")
        unique_strings = sorted(set(strings))
        for s in unique_strings[:100]:
            print(f"  {s}")

if __name__ == '__main__':
    metadata_path = Path('/data/.openclaw/workspace/fm26-game-files/fm_Data/il2cpp_data/Metadata/global-metadata.dat')
    if metadata_path.exists():
        analyze_metadata(metadata_path)
    else:
        print(f"Arquivo não encontrado: {metadata_path}")
