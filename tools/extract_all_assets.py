#!/usr/bin/env python3
"""
FM26 Assets Extractor - Extrai de .assets e .bundle
"""

import UnityPy
import os
import json
from pathlib import Path

def extract_assets_file(assets_path: Path, output_dir: Path):
    """Extrai de um arquivo .assets"""
    print(f"Processando: {assets_path.name}")
    
    try:
        env = UnityPy.load(str(assets_path))
    except Exception as e:
        print(f"  Erro ao carregar: {e}")
        return []
    
    extracted = []
    
    for obj in env.objects:
        try:
            type_name = obj.type.name
            
            if type_name == 'TextAsset':
                data = obj.read()
                name = data.m_Name if hasattr(data, 'm_Name') else f"text_{obj.path_id}"
                content = data.m_Script
                
                if isinstance(content, bytes):
                    # Tentar decodificar
                    try:
                        content_str = content.decode('utf-8', errors='replace')
                    except:
                        content_str = str(content)
                else:
                    content_str = content
                
                # Detectar tipo
                ext = 'txt'
                if '<?xml' in content_str or '<UXML' in content_str or '<ui:UXML' in content_str:
                    ext = 'uxml'
                elif '.uss' in content_str or 'StyleSheet' in content_str:
                    ext = 'uss'
                elif content_str.strip().startswith('{') or content_str.strip().startswith('['):
                    ext = 'json'
                elif content_str.strip().startswith('<record'):
                    ext = 'xml'
                
                # Salvar
                out_path = output_dir / f"{name.replace('/', '_')}.{ext}"
                out_path.parent.mkdir(parents=True, exist_ok=True)
                
                with open(out_path, 'w', encoding='utf-8') as f:
                    f.write(content_str)
                
                extracted.append({
                    'name': name,
                    'type': type_name,
                    'ext': ext,
                    'size': len(content_str)
                })
                
            elif type_name == 'MonoBehaviour':
                # Tentar extrair dados serializados
                try:
                    tree = obj.read_typetree()
                    if tree and isinstance(tree, dict):
                        name = tree.get('m_Name', f"mono_{obj.path_id}")
                        out_path = output_dir / "MonoBehaviour" / f"{name}.json"
                        out_path.parent.mkdir(parents=True, exist_ok=True)
                        
                        with open(out_path, 'w') as f:
                            json.dump(tree, f, indent=2, default=str)
                        
                        extracted.append({
                            'name': name,
                            'type': type_name,
                            'ext': 'json',
                            'size': len(str(tree))
                        })
                except:
                    pass
                    
            elif type_name == 'PlayerSettings':
                data = obj.read()
                if hasattr(data, 'm_productName'):
                    print(f"  Produto: {data.m_productName}")
                    
        except Exception as e:
            pass
    
    return extracted

def main():
    game_path = Path("/data/.openclaw/workspace/fm26-game-files")
    output_path = Path("/data/.openclaw/workspace/fm26-editor-workspace/extracted-assets")
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Arquivos .assets principais
    assets_files = [
        game_path / "globalgamemanagers.assets",
        game_path / "sharedassets0.assets", 
        game_path / "resources.assets",
        game_path / "fm_Data/globalgamemanagers.assets",
        game_path / "fm_Data/sharedassets0.assets",
        game_path / "fm_Data/resources.assets",
    ]
    
    print("=== EXTRAINDO DE .ASSETS ===\n")
    
    total = []
    for assets in assets_files:
        if assets.exists():
            result = extract_assets_file(assets, output_path)
            total.extend(result)
            print(f"  Extraídos: {len(result)} assets\n")
    
    # Resumo
    print("\n=== RESUMO ===")
    print(f"Total extraído: {len(total)} assets")
    
    # Mostrar por extensão
    by_ext = {}
    for item in total:
        ext = item['ext']
        if ext not in by_ext:
            by_ext[ext] = []
        by_ext[ext].append(item)
    
    for ext, items in sorted(by_ext.items()):
        print(f"\n.{ext}: {len(items)} arquivos")
        for item in items[:5]:
            print(f"  - {item['name']} ({item['size']} bytes)")
    
    # Salvar índice
    index_path = output_path / "index.json"
    with open(index_path, 'w') as f:
        json.dump(total, f, indent=2)
    print(f"\nÍndice salvo: {index_path}")

if __name__ == '__main__':
    main()
