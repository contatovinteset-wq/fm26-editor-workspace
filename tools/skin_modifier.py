#!/usr/bin/env python3
"""
FM26 Skin Modifier
Analisa e modifica skins do FM26 para adicionar funcionalidades

Uso: python3 skin_modifier.py <pasta_da_skin>

Funcionalidades planejadas:
- Analisar estrutura da skin
- Encontrar arquivos de binding de teclado
- Adicionar Ctrl+P para exportação
"""

import sys
import os
import json
import xml.etree.ElementTree as ET
from pathlib import Path

def analyze_skin(skin_path):
    """Analisa uma skin do FM26"""
    skin_path = Path(skin_path)
    
    print(f"Analisando Skin: {skin_path.name}")
    print("="*50)
    
    # Estrutura típica de skin FM26
    expected_files = [
        'skin_config.xml',
        'config.xml',
        'settings.xml',
        'colours.xml',
        'fonts.xml',
        'graphics/',
        'panels/',
        'skins/',
    ]
    
    print("\nArquivos esperados:")
    for f in expected_files:
        full_path = skin_path / f
        status = "✅" if full_path.exists() else "❌"
        print(f"  {status} {f}")
    
    # Procurar arquivos XML
    print("\nArquivos XML encontrados:")
    xml_files = list(skin_path.rglob("*.xml"))
    for xml in xml_files[:20]:
        print(f"  {xml.relative_to(skin_path)}")
    
    # Procurar arquivos UXML (Unity UI Toolkit)
    print("\nArquivos UXML encontrados:")
    uxml_files = list(skin_path.rglob("*.uxml"))
    for uxml in uxml_files[:20]:
        print(f"  {uxml.relative_to(skin_path)}")
    
    # Procurar arquivos USS (Unity Stylesheet)
    print("\nArquivos USS encontrados:")
    uss_files = list(skin_path.rglob("*.uss"))
    for uss in uss_files[:20]:
        print(f"  {uss.relative_to(skin_path)}")
    
    # Procurar menções a keyboard/bindings
    print("\nProcurando bindings de teclado...")
    for xml_file in xml_files:
        try:
            with open(xml_file, 'r', encoding='utf-8') as f:
                content = f.read()
            
            if any(kw in content.lower() for kw in ['keyboard', 'binding', 'shortcut', 'key=', 'ctrl', 'print']):
                print(f"\n  {xml_file.relative_to(skin_path)}:")
                # Mostrar linhas relevantes
                for line in content.split('\n'):
                    if any(kw in line.lower() for kw in ['keyboard', 'binding', 'shortcut', 'key=', 'ctrl', 'print']):
                        print(f"    {line.strip()}")
        except:
            pass
    
    # Criar arquivo de modificação
    print("\n" + "="*50)
    print("Para adicionar Ctrl+P, você precisaria:")
    print("1. Encontrar o arquivo de bindings de teclado")
    print("2. Adicionar uma entrada para Ctrl+P")
    print("3. Reempacotar a skin")
    
    return {
        'xml_files': [str(f) for f in xml_files],
        'uxml_files': [str(f) for f in uxml_files],
        'uss_files': [str(f) for f in uss_files],
    }

def find_export_references(skin_path):
    """Procura referências a exportação na skin"""
    skin_path = Path(skin_path)
    
    print("\nProcurando referências de exportação...")
    
    for file in skin_path.rglob("*"):
        if file.is_file():
            try:
                with open(file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                if 'export' in content.lower() or 'print' in content.lower():
                    print(f"\n{file.relative_to(skin_path)}:")
                    for line in content.split('\n'):
                        if 'export' in line.lower() or 'print' in line.lower():
                            print(f"  {line.strip()}")
            except:
                pass

def main():
    if len(sys.argv) < 2:
        print("FM26 Skin Modifier")
        print("="*30)
        print("\nUso: python3 skin_modifier.py <pasta_da_skin>")
        print("\nExemplo:")
        print("  python3 skin_modifier.py 'Downloads/FM26-Skin-v3'")
        print("\nNota: Extraia uma skin existente primeiro")
        return
    
    skin_path = sys.argv[1]
    if not os.path.exists(skin_path):
        print(f"Erro: Pasta não encontrada: {skin_path}")
        return
    
    result = analyze_skin(skin_path)
    find_export_references(skin_path)
    
    # Salvar resultado
    out_path = Path(skin_path) / 'skin_analysis.json'
    with open(out_path, 'w') as f:
        json.dump(result, f, indent=2)
    print(f"\nAnálise salva em: {out_path}")

if __name__ == '__main__':
    main()
