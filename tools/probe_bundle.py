#!/usr/bin/env python3
"""
Bundle Probe - Lista tipos de assets sem extrair
"""

import UnityPy
from pathlib import Path
from collections import Counter

def probe_bundle(bundle_path: Path):
    """Lista tipos de assets em um bundle"""
    try:
        env = UnityPy.load(str(bundle_path))
        types = Counter()
        
        for obj in env.objects:
            types[obj.type.name] += 1
        
        return types
    except Exception as e:
        return f"Erro: {str(e)}"

def main():
    bundles_path = Path("/data/.openclaw/workspace/darkside-skin-v4.3")
    
    bundles = [
        'ui-styles_assets_default.bundle',
        'ui-backgrounds_assets_common.bundle',
        'ui-fonts_assets_production.bundle',
        'ui-tableviews_assets_all.bundle'
    ]
    
    print("=" * 70)
    print("🔍 BUNDLE PROBE - Tipos de Assets")
    print("=" * 70)
    
    for bundle_name in bundles:
        bundle_path = bundles_path / bundle_name
        if not bundle_path.exists():
            continue
        
        print(f"\n📦 {bundle_name}")
        print(f"   Tamanho: {bundle_path.stat().st_size / 1024 / 1024:.1f} MB")
        
        types = probe_bundle(bundle_path)
        if isinstance(types, dict):
            print(f"   Assets:")
            for asset_type, count in types.most_common(10):
                print(f"      {asset_type}: {count}")
        else:
            print(f"   {types}")

if __name__ == '__main__':
    main()
