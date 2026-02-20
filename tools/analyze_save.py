#!/usr/bin/env python3
"""
FM26 Save Game Analyzer
Analisa arquivos de save (.fm) para extrair dados de jogadores

Uso: python3 analyze_save.py <caminho_do_save.fm>

Nota: Arquivos .fm são compactados. Este script tenta extrair dados brutos.
"""

import sys
import os
import struct
import zlib
import json
from pathlib import Path

def analyze_fm_file(filepath):
    """Analisa um arquivo .fm do FM26"""
    print(f"Analisando: {filepath}")
    print("="*50)
    
    with open(filepath, 'rb') as f:
        # Ler header
        header = f.read(4)
        print(f"Header: {header}")
        
        # Verificar magic number
        if header[:3] == b'FM ':
            print("Formato: FM Save Game")
        elif header[:2] == b'PK':
            print("Formato: ZIP (pode ser .fm zipado)")
        else:
            print(f"Formato: Desconhecido ({header.hex()})")
        
        # Ler mais bytes para análise
        f.seek(0)
        data = f.read(8192)
        
        print(f"\nPrimeiros 100 bytes (hex):")
        print(data[:100].hex())
        
        # Procurar strings legíveis
        print(f"\nStrings encontradas:")
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
        
        for s in strings[:20]:
            print(f"  {s}")
        
        # Verificar se é compactado
        f.seek(0)
        try:
            decompressed = zlib.decompress(f.read(), -zlib.MAX_WBITS)
            print(f"\nArquivo compactado! Tamanho descompactado: {len(decompressed)}")
            
            # Salvar descompactado para análise
            out_path = filepath + '.extracted'
            with open(out_path, 'wb') as out:
                out.write(decompressed)
            print(f"Salvo em: {out_path}")
        except:
            print("\nArquivo não parece ser zlib compactado")
        
        # Procurar padrões de dados de jogador
        f.seek(0)
        full_data = f.read()
        
        patterns = [
            b'Player',
            b'Name',
            b'Club',
            b'Ability',
            b'Potential',
            b'Value',
            b'Wage',
        ]
        
        print(f"\nPadrões encontrados:")
        for pattern in patterns:
            count = full_data.count(pattern)
            if count > 0:
                print(f"  {pattern.decode()}: {count} ocorrências")

def main():
    if len(sys.argv) < 2:
        print("FM26 Save Game Analyzer")
        print("="*30)
        print("\nUso: python3 analyze_save.py <save.fm>")
        print("\nExemplo:")
        print("  python3 analyze_save.py 'My Documents/Sports Interactive/FM26/games/autosave.fm'")
        print("\nNota: Você precisa ter um arquivo de save do FM26")
        return
    
    filepath = sys.argv[1]
    if not os.path.exists(filepath):
        print(f"Erro: Arquivo não encontrado: {filepath}")
        return
    
    analyze_fm_file(filepath)

if __name__ == '__main__':
    main()
