#!/usr/bin/env python3
"""
FM26 Asset Bundle Extractor
Extrai assets dos bundles do Football Manager 26
"""

import UnityPy
import os
import sys
from pathlib import Path

def extract_bundle(bundle_path: Path, output_dir: Path):
    """Extrai todos os assets de um bundle"""
    try:
        env = UnityPy.load(str(bundle_path))
        
        extracted = []
        for obj in env.objects:
            # Tipos de assets que queremos
            if obj.type.name in ['TextAsset', 'MonoBehaviour', 'Shader', 'Font', 'Sprite', 'Texture2D']:
                data = obj.read()
                
                # Criar pasta por tipo
                type_dir = output_dir / obj.type.name
                type_dir.mkdir(parents=True, exist_ok=True)
                
                # Nome do arquivo
                name = data.m_Name if hasattr(data, 'm_Name') else f"asset_{obj.path_id}"
                name = name.replace('/', '_').replace('\\', '_')
                
                # Salvar
                if obj.type.name == 'TextAsset':
                    # TextAsset pode ser .uxml, .uss, .json, etc
                    content = data.m_Script
                    if isinstance(content, bytes):
                        content = content.decode('utf-8', errors='replace')
                    
                    # Detectar extensão
                    ext = 'txt'
                    if '<?xml' in content or '<UXML' in content:
                        ext = 'uxml'
                    elif 'StyleSheet' in content or '.uss' in content:
                        ext = 'uss'
                    elif content.strip().startswith('{') or content.strip().startswith('['):
                        ext = 'json'
                    
                    out_path = type_dir / f"{name}.{ext}"
                    with open(out_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    extracted.append((name, ext, len(content)))
                    
                elif obj.type.name == 'MonoBehaviour':
                    # MonoBehaviour pode ter dados serializados
                    try:
                        tree = obj.read_typetree()
                        if tree:
                            out_path = type_dir / f"{name}.json"
                            import json
                            with open(out_path, 'w') as f:
                                json.dump(tree, f, indent=2, default=str)
                            extracted.append((name, 'json', len(str(tree))))
                    except:
                        pass
                        
        return extracted
        
    except Exception as e:
        return f"Erro: {str(e)}"

def main():
    game_path = Path("/data/.openclaw/workspace/fm26-game-files")
    output_path = Path("/data/.openclaw/workspace/fm26-editor-workspace/extracted-assets")
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Encontrar bundles
    bundles = list(game_path.glob("**/*.bundle"))
    print(f"Encontrados {len(bundles)} bundles\n")
    
    # Priorizar bundles importantes
    priority_keywords = ['ui', 'config', 'skin', 'panel', 'theme', 'database', 'match']
    priority_bundles = []
    other_bundles = []
    
    for b in bundles:
        name_lower = b.name.lower()
        if any(kw in name_lower for kw in priority_keywords):
            priority_bundles.append(b)
        else:
            other_bundles.append(b)
    
    print(f"Bundles prioritários: {len(priority_bundles)}")
    print(f"Outros bundles: {len(other_bundles)}\n")
    
    # Extrair prioritários primeiro
    print("=== EXTRAINDO BUNDLES PRIORITÁRIOS ===\n")
    for bundle in priority_bundles[:10]:  # Primeiros 10
        print(f"Bundle: {bundle.name}")
        result = extract_bundle(bundle, output_path / bundle.stem)
        if isinstance(result, list):
            print(f"  Extraídos: {len(result)} assets")
            for name, ext, size in result[:5]:
                print(f"    - {name}.{ext} ({size} bytes)")
        else:
            print(f"  {result}")
        print()
    
    # Resumo
    print("\n=== RESUMO ===")
    total_files = sum(1 for _ in output_path.rglob("*") if _.is_file())
    print(f"Total de arquivos extraídos: {total_files}")
    print(f"Pasta: {output_path}")

if __name__ == '__main__':
    main()
