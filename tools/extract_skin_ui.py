#!/usr/bin/env python3
"""
FM26 UI Bundle Extractor - Extrai bundles de UI da skin
Foco em encontrar templates de tabela e exportação
"""

import UnityPy
import json
import re
from pathlib import Path

def extract_ui_bundle(bundle_path: Path, output_dir: Path):
    """Extrai assets de UI de um bundle"""
    print(f"\n📦 {bundle_path.name}")
    
    try:
        env = UnityPy.load(str(bundle_path))
    except Exception as e:
        print(f"  ❌ Erro: {e}")
        return []
    
    extracted = []
    
    for obj in env.objects:
        type_name = obj.type.name
        
        if type_name == 'TextAsset':
            try:
                data = obj.read()
                name = getattr(data, 'm_Name', f'asset_{obj.path_id}')
                content = getattr(data, 'm_Script', b'')
                
                if isinstance(content, bytes):
                    content_str = content.decode('utf-8', errors='replace')
                else:
                    content_str = str(content)
                
                # Detectar tipo
                ext = 'txt'
                if '<UXML' in content_str or '<ui:' in content_str or 'Uxml' in content_str:
                    ext = 'uxml'
                elif 'StyleSheet' in content_str or '.uss' in content_str:
                    ext = 'uss'
                elif content_str.strip().startswith('{') or content_str.strip().startswith('['):
                    ext = 'json'
                
                # Salvar
                out_path = output_dir / ext / f"{name.replace('/', '_')}.{ext}"
                out_path.parent.mkdir(parents=True, exist_ok=True)
                
                with open(out_path, 'w', encoding='utf-8') as f:
                    f.write(content_str)
                
                extracted.append({
                    'name': name,
                    'ext': ext,
                    'size': len(content_str),
                    'bundle': bundle_path.name
                })
                
                # Mostrar preview se for relevante
                if any(kw in name.lower() for kw in ['export', 'print', 'table', 'list', 'player']):
                    print(f"  ✅ {name}.{ext} ({len(content_str)} bytes)")
                
            except Exception as e:
                pass
    
    print(f"  📊 Extraídos: {len(extracted)} assets")
    return extracted

def search_export_references(output_dir: Path):
    """Busca referências a exportação nos arquivos extraídos"""
    print("\n" + "="*60)
    print("🔍 BUSCANDO REFERÊNCIAS DE EXPORTAÇÃO...")
    print("="*60)
    
    keywords = ['export', 'print', 'ctrl+p', 'ctrl p', 'copy', 'clipboard', 'html', 'pdf']
    
    for ext in ['uxml', 'uss', 'json', 'txt']:
        ext_dir = output_dir / ext
        if not ext_dir.exists():
            continue
        
        for file_path in ext_dir.glob(f'*.{ext}'):
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read().lower()
                
                for kw in keywords:
                    if kw in content:
                        print(f"\n📄 {file_path.name}")
                        print(f"   Encontrado: '{kw}'")
                        
                        # Mostrar contexto
                        lines = content.split('\n')
                        for i, line in enumerate(lines):
                            if kw in line:
                                start = max(0, i-2)
                                end = min(len(lines), i+3)
                                print(f"   Linha {i}: {line[:100]}...")
                        break
            except:
                pass

def main():
    skins_dir = Path("/data/.openclaw/workspace/fm26-editor-workspace/skins-reference/StandaloneWindows64")
    output_dir = Path("/data/.openclaw/workspace/fm26-editor-workspace/extracted-skin-ui")
    output_dir.mkdir(parents=True, exist_ok=True)
    
    print("="*60)
    print("FM26 UI SKIN BUNDLE EXTRACTOR")
    print("="*60)
    
    bundles = sorted(skins_dir.glob("*.bundle"))
    print(f"\nEncontrados {len(bundles)} bundles de UI\n")
    
    all_extracted = []
    
    for bundle in bundles:
        result = extract_ui_bundle(bundle, output_dir)
        all_extracted.extend(result)
    
    # Resumo
    print("\n" + "="*60)
    print("📊 RESUMO")
    print("="*60)
    print(f"Total extraído: {len(all_extracted)} assets")
    
    by_ext = {}
    for item in all_extracted:
        ext = item['ext']
        if ext not in by_ext:
            by_ext[ext] = []
        by_ext[ext].append(item)
    
    for ext, items in sorted(by_ext.items()):
        print(f"\n.{ext}: {len(items)} arquivos")
    
    # Buscar referências de exportação
    search_export_references(output_dir)
    
    # Salvar índice
    index_path = output_dir / 'index.json'
    with open(index_path, 'w') as f:
        json.dump(all_extracted, f, indent=2, ensure_ascii=False)
    print(f"\n✅ Índice salvo: {index_path}")

if __name__ == '__main__':
    main()
