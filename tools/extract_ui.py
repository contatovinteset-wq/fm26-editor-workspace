#!/usr/bin/env python3
"""
FM26 UI Extractor - Extrai especificamente arquivos de UI
Procura por .uxml, .uss, .json em todos os bundles
"""

import UnityPy
import os
import json
from pathlib import Path

def extract_ui_from_bundle(bundle_path: Path, output_dir: Path):
    """Extrai assets de UI de um bundle"""
    try:
        env = UnityPy.load(str(bundle_path))
    except Exception as e:
        return None, str(e)
    
    ui_assets = []
    
    for obj in env.objects:
        type_name = obj.type.name
        
        # TextAsset pode conter UXML, USS, JSON
        if type_name == 'TextAsset':
            try:
                data = obj.read()
                name = getattr(data, 'm_Name', f'asset_{obj.path_id}')
                content = getattr(data, 'm_Script', b'')
                
                if isinstance(content, bytes):
                    try:
                        content_str = content.decode('utf-8', errors='replace')
                    except:
                        continue
                else:
                    content_str = str(content)
                
                # Verificar se é UI
                is_ui = False
                ext = 'txt'
                
                if '<UXML' in content_str or '<ui:UXML' in content_str or 'Uxml' in content_str:
                    is_ui = True
                    ext = 'uxml'
                elif 'StyleSheet' in content_str or '.uss' in content_str or 'USS' in content_str:
                    is_ui = True
                    ext = 'uss'
                elif any(kw in name.lower() for kw in ['ui', 'panel', 'view', 'screen', 'menu', 'widget', 'button', 'dialog']):
                    is_ui = True
                    if content_str.strip().startswith('{'):
                        ext = 'json'
                    elif content_str.strip().startswith('<'):
                        ext = 'xml'
                
                if is_ui:
                    out_path = output_dir / ext / f"{name.replace('/', '_')}.{ext}"
                    out_path.parent.mkdir(parents=True, exist_ok=True)
                    
                    with open(out_path, 'w', encoding='utf-8') as f:
                        f.write(content_str)
                    
                    ui_assets.append({
                        'name': name,
                        'ext': ext,
                        'size': len(content_str),
                        'preview': content_str[:200]
                    })
                    
            except Exception as e:
                pass
        
        # MonoBehaviour pode ter dados de UI
        elif type_name == 'MonoBehaviour':
            try:
                tree = obj.read_typetree()
                if tree and isinstance(tree, dict):
                    name = tree.get('m_Name', f'mono_{obj.path_id}')
                    
                    # Verificar se parece UI
                    tree_str = str(tree).lower()
                    if any(kw in tree_str for kw in ['panel', 'view', 'ui', 'screen', 'widget']):
                        out_path = output_dir / 'MonoBehaviour' / f"{name}.json"
                        out_path.parent.mkdir(parents=True, exist_ok=True)
                        
                        with open(out_path, 'w') as f:
                            json.dump(tree, f, indent=2, default=str)
                        
                        ui_assets.append({
                            'name': name,
                            'ext': 'json',
                            'size': len(str(tree)),
                            'preview': str(tree)[:200]
                        })
            except:
                pass
    
    return ui_assets, None

def main():
    game_path = Path("/data/.openclaw/workspace/fm26-game-files")
    output_path = Path("/data/.openclaw/workspace/fm26-editor-workspace/extracted-ui")
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Todos os bundles
    bundles = list(game_path.glob("**/*.bundle"))
    print(f"Analisando {len(bundles)} bundles em busca de UI...\n")
    
    all_ui = []
    errors = []
    
    for i, bundle in enumerate(bundles, 1):
        result, error = extract_ui_from_bundle(bundle, output_path)
        
        if error:
            errors.append((bundle.name, error))
        elif result:
            all_ui.extend(result)
            print(f"[{i}/{len(bundles)}] {bundle.name}: {len(result)} assets UI")
    
    # Resumo
    print(f"\n{'='*50}")
    print("=== RESUMO ===")
    print(f"Total de assets UI extraídos: {len(all_ui)}")
    print(f"Erros: {len(errors)}")
    print(f"Pasta: {output_path}")
    
    # Por extensão
    by_ext = {}
    for item in all_ui:
        ext = item['ext']
        if ext not in by_ext:
            by_ext[ext] = []
        by_ext[ext].append(item)
    
    for ext, items in sorted(by_ext.items()):
        print(f"\n.{ext}: {len(items)} arquivos")
        for item in items[:3]:
            print(f"  - {item['name']} ({item['size']} bytes)")
    
    # Salvar índice
    index_path = output_path / 'ui_index.json'
    with open(index_path, 'w') as f:
        json.dump(all_ui, f, indent=2, ensure_ascii=False)
    print(f"\nÍndice salvo: {index_path}")

if __name__ == '__main__':
    main()
