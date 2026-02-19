#!/usr/bin/env python3
"""
Ferramenta de Extração de Asset Bundles do FM26
Analisa e extrai informações de bundles Unity (.bundle)
"""

import os
import struct
import sys
from pathlib import Path

def read_bundle_header(filepath):
    """Lê o cabeçalho de um Asset Bundle Unity"""
    with open(filepath, 'rb') as f:
        # Unity Asset Bundle signature
        signature = f.read(20).decode('utf-8', errors='replace').rstrip('\x00')
        f.seek(0)
        
        # Try to read basic info
        data = f.read(100)
        
        info = {
            'path': filepath,
            'size': os.path.getsize(filepath),
            'signature': signature[:20],
        }
        
        # Check if it's a valid Unity bundle
        if 'Unity' in signature or 'archive' in signature.lower():
            info['valid'] = True
        else:
            info['valid'] = False
            
        return info

def scan_bundles(directory):
    """Escaneia todos os bundles em um diretório"""
    bundles = []
    bundle_dir = Path(directory)
    
    if not bundle_dir.exists():
        print(f"Diretório não encontrado: {directory}")
        return bundles
    
    for bundle_file in bundle_dir.glob('*.bundle'):
        info = read_bundle_header(bundle_file)
        bundles.append(info)
        
    return bundles

def extract_strings_from_bundle(filepath, min_length=6):
    """Extrai strings legíveis de um bundle"""
    strings = []
    
    with open(filepath, 'rb') as f:
        data = f.read()
    
    current = b''
    for i, byte in enumerate(data):
        if 32 <= byte < 127:
            current += bytes([byte])
        else:
            if len(current) >= min_length:
                try:
                    s = current.decode('utf-8', errors='replace')
                    # Filter for relevant strings
                    if any(kw in s.lower() for kw in ['ui', 'skin', 'panel', 'theme', 'config', 'xml', 'json']):
                        strings.append({
                            'string': s[:100],
                            'offset': i - len(current)
                        })
                except:
                    pass
            current = b''
    
    return strings

def analyze_ui_bundles(bundles_dir, output_file=None):
    """Analisa bundles relacionados à UI"""
    bundles_dir = Path(bundles_dir)
    
    ui_keywords = ['ui', 'skin', 'theme', 'panel', 'menu', 'hud', 'interface']
    
    print("=" * 60)
    print("ANÁLISE DE ASSET BUNDLES - FM26")
    print("=" * 60)
    
    results = []
    
    for bundle_file in sorted(bundles_dir.glob('*.bundle')):
        name = bundle_file.name.lower()
        
        # Check if UI-related
        is_ui = any(kw in name for kw in ui_keywords)
        
        if is_ui:
            print(f"\n📦 {bundle_file.name}")
            print(f"   Tamanho: {bundle_file.stat().st_size / 1024 / 1024:.2f} MB")
            
            # Extract strings
            strings = extract_strings_from_bundle(bundle_file)
            
            if strings:
                print(f"   Strings relevantes encontradas: {len(strings)}")
                for s in strings[:10]:  # Show first 10
                    print(f"     - {s['string']}")
            
            results.append({
                'file': bundle_file.name,
                'size': bundle_file.stat().st_size,
                'strings_count': len(strings),
                'strings': strings[:50]  # Keep first 50
            })
    
    return results

def create_extraction_report(bundles_dir, output_path):
    """Cria relatório completo de extração"""
    bundles_dir = Path(bundles_dir)
    
    report = []
    report.append("# Relatório de Asset Bundles - FM26")
    report.append(f"\n## Diretório: {bundles_dir}")
    report.append(f"\n## Data: 2026-02-19")
    report.append("\n---\n")
    
    # UI Bundles
    report.append("## Bundles de UI Identificados\n")
    
    ui_bundles = []
    for bundle_file in sorted(bundles_dir.glob('*.bundle')):
        name = bundle_file.name.lower()
        if any(kw in name for kw in ['ui', 'skin', 'theme', 'panel', 'menu', 'hud']):
            ui_bundles.append({
                'name': bundle_file.name,
                'size_mb': bundle_file.stat().st_size / 1024 / 1024
            })
    
    for b in ui_bundles:
        report.append(f"- `{b['name']}` ({b['size_mb']:.2f} MB)")
    
    # All bundles summary
    report.append("\n## Todos os Bundles\n")
    
    all_bundles = list(bundles_dir.glob('*.bundle'))
    total_size = sum(b.stat().st_size for b in all_bundles) / 1024 / 1024
    
    report.append(f"Total: {len(all_bundles)} bundles, {total_size:.2f} MB\n")
    
    # Group by category
    categories = {
        'characters': [],
        'environments': [],
        'kits': [],
        'ui': [],
        'other': []
    }
    
    for bundle_file in all_bundles:
        name = bundle_file.name.lower()
        size = bundle_file.stat().st_size / 1024 / 1024
        
        if 'character' in name or 'player' in name or 'manager' in name:
            categories['characters'].append((bundle_file.name, size))
        elif 'environment' in name or 'stadium' in name:
            categories['environments'].append((bundle_file.name, size))
        elif 'kit' in name:
            categories['kits'].append((bundle_file.name, size))
        elif 'ui' in name or 'skin' in name or 'theme' in name:
            categories['ui'].append((bundle_file.name, size))
        else:
            categories['other'].append((bundle_file.name, size))
    
    for cat, bundles in categories.items():
        if bundles:
            cat_size = sum(b[1] for b in bundles)
            report.append(f"\n### {cat.upper()} ({len(bundles)} bundles, {cat_size:.2f} MB)\n")
            for name, size in bundles[:20]:  # Show max 20
                report.append(f"- {name} ({size:.2f} MB)")
    
    # Write report
    with open(output_path, 'w') as f:
        f.write('\n'.join(report))
    
    print(f"\nRelatório salvo em: {output_path}")
    return report

if __name__ == '__main__':
    BUNDLES_DIR = '/data/.openclaw/workspace/fm26-game-files/fm_Data/VietNorSteam/aa/StandaloneWindows64'
    OUTPUT_DIR = '/data/.openclaw/workspace/fm26-editor-workspace'
    
    # Create report
    create_extraction_report(BUNDLES_DIR, f"{OUTPUT_DIR}/bundle-report.md")
    
    # Analyze UI bundles
    print("\n" + "=" * 60)
    print("ANÁLISE DETALHADA DE UI")
    print("=" * 60)
    analyze_ui_bundles(BUNDLES_DIR)
