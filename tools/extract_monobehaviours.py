#!/usr/bin/env python3
"""
Extrai MonoBehaviours (estilos/configurações UI) como JSON
"""

import UnityPy
import json
from pathlib import Path

def extract_monobehaviours(bundle_path: Path, output_dir: Path):
    """Extrai MonoBehaviours de um bundle"""
    print(f"\n📦 {bundle_path.name}")
    
    try:
        env = UnityPy.load(str(bundle_path))
        count = 0
        
        output_dir.mkdir(parents=True, exist_ok=True)
        
        for obj in env.objects:
            if obj.type.name != 'MonoBehaviour':
                continue
            
            try:
                data = obj.read()
                name = getattr(data, 'm_Name', f"mb_{obj.path_id}")
                name = name.replace('/', '_').replace('\\', '_').replace(' ', '_')
                
                # Tentar extrair typetree
                try:
                    tree = obj.read_typetree()
                    if tree:
                        out_path = output_dir / f"{name}.json"
                        with open(out_path, 'w') as f:
                            json.dump(tree, f, indent=2, default=str)
                        count += 1
                except:
                    pass
                    
            except:
                pass
        
        print(f"✅ Extraídos {count} MonoBehaviours")
        return count
        
    except Exception as e:
        print(f"❌ ERRO: {str(e)}")
        return 0

def main():
    bundles_path = Path("/data/.openclaw/workspace/darkside-skin-v4.3")
    output_base = Path("/data/.openclaw/workspace/fm26-editor-workspace/skin-reference/darkside-monobehaviours")
    
    # Bundles com MonoBehaviours
    targets = [
        'ui-styles_assets_default.bundle',
        'ui-tableviews_assets_all.bundle'
    ]
    
    print("=" * 60)
    print("🔧 EXTRATOR DE MONOBEHAVIOURS")
    print("=" * 60)
    
    total = 0
    for bundle_name in targets:
        bundle_path = bundles_path / bundle_name
        if not bundle_path.exists():
            continue
        
        output_dir = output_base / bundle_path.stem
        count = extract_monobehaviours(bundle_path, output_dir)
        total += count
    
    print(f"\n📊 Total: {total} MonoBehaviours extraídos")
    print(f"📁 {output_base}")
    
    # Listar alguns JSONs
    jsons = list(output_base.rglob("*.json"))
    if jsons:
        print(f"\n📄 Primeiros 10 JSONs:")
        for f in sorted(jsons)[:10]:
            print(f"   - {f.name}")

if __name__ == '__main__':
    main()
