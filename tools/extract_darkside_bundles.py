#!/usr/bin/env python3
"""
Darkside v4.3 Bundle Extractor
Extrai UI, estilos, fontes e texturas dos bundles da skin Darkside
"""

import UnityPy
import os
import json
from pathlib import Path

def extract_bundle(bundle_path: Path, output_dir: Path):
    """Extrai assets relevantes de um bundle"""
    try:
        print(f"\n📦 Bundle: {bundle_path.name}")
        env = UnityPy.load(str(bundle_path))
        
        extracted = {
            'TextAsset': [],
            'Texture2D': [],
            'Sprite': [],
            'Font': [],
            'MonoBehaviour': []
        }
        
        for obj in env.objects:
            if obj.type.name not in extracted:
                continue
                
            try:
                data = obj.read()
                name = getattr(data, 'm_Name', f"asset_{obj.path_id}")
                name = name.replace('/', '_').replace('\\', '_')
                
                type_dir = output_dir / obj.type.name
                type_dir.mkdir(parents=True, exist_ok=True)
                
                # TextAsset → UXML/USS/JSON
                if obj.type.name == 'TextAsset':
                    content = data.m_Script
                    if isinstance(content, bytes):
                        content = content.decode('utf-8', errors='replace')
                    
                    ext = 'txt'
                    if '<?xml' in content or '<ui:UXML' in content or '<UXML' in content:
                        ext = 'uxml'
                    elif '.uss' in name.lower() or 'stylesheet' in content.lower():
                        ext = 'uss'
                    elif content.strip().startswith(('{', '[')):
                        ext = 'json'
                    
                    out_path = type_dir / f"{name}.{ext}"
                    with open(out_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    extracted['TextAsset'].append(f"{name}.{ext}")
                
                # Texture2D → PNG
                elif obj.type.name == 'Texture2D':
                    image = data.image
                    if image:
                        out_path = type_dir / f"{name}.png"
                        image.save(out_path)
                        extracted['Texture2D'].append(f"{name}.png")
                
                # Sprite → PNG
                elif obj.type.name == 'Sprite':
                    image = data.image
                    if image:
                        out_path = type_dir / f"{name}.png"
                        image.save(out_path)
                        extracted['Sprite'].append(f"{name}.png")
                
                # Font → metadata JSON
                elif obj.type.name == 'Font':
                    font_data = {
                        'name': name,
                        'size': getattr(data, 'm_FontSize', None),
                        'lineHeight': getattr(data, 'm_LineSpacing', None)
                    }
                    out_path = type_dir / f"{name}.json"
                    with open(out_path, 'w') as f:
                        json.dump(font_data, f, indent=2)
                    extracted['Font'].append(f"{name}.json")
                
                # MonoBehaviour → JSON
                elif obj.type.name == 'MonoBehaviour':
                    try:
                        tree = obj.read_typetree()
                        if tree:
                            out_path = type_dir / f"{name}.json"
                            with open(out_path, 'w') as f:
                                json.dump(tree, f, indent=2, default=str)
                            extracted['MonoBehaviour'].append(f"{name}.json")
                    except:
                        pass
                        
            except Exception as e:
                # Ignorar erros individuais
                pass
        
        # Resumo
        total = sum(len(v) for v in extracted.values())
        if total > 0:
            print(f"✅ Extraídos {total} assets:")
            for asset_type, files in extracted.items():
                if files:
                    print(f"   {asset_type}: {len(files)}")
        else:
            print("⚠️  Nenhum asset relevante")
        
        return extracted
        
    except Exception as e:
        print(f"❌ Erro: {str(e)}")
        return {}

def main():
    bundles_path = Path("/data/.openclaw/workspace/darkside-skin-v4.3")
    output_path = Path("/data/.openclaw/workspace/fm26-editor-workspace/skin-reference/darkside-v4.3-extracted")
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Bundles prioritários (UI, estilos, texturas)
    priority_bundles = [
        'ui-styles_assets_default.bundle',
        'ui-styles_assets_match.bundle',
        'ui-fonts_assets_production.bundle',
        'ui-backgrounds_assets_common.bundle',
        'ui-tableviews_assets_all.bundle',
        'ui-widgets_assets_all.bundle',
        'ui-tiles_assets_all.bundle',
        'ui-textures_assets_all.bundle',
        'ui-iconspriteatlases_assets_2x.bundle'
    ]
    
    print("=" * 60)
    print("🎨 DARKSIDE V4.3 BUNDLE EXTRACTOR")
    print("=" * 60)
    
    manifest = {}
    
    for bundle_name in priority_bundles:
        bundle_path = bundles_path / bundle_name
        if not bundle_path.exists():
            print(f"\n⚠️  Bundle não encontrado: {bundle_name}")
            continue
        
        bundle_output = output_path / bundle_path.stem
        extracted = extract_bundle(bundle_path, bundle_output)
        manifest[bundle_name] = extracted
    
    # Salvar manifest
    manifest_path = output_path / 'extraction_manifest.json'
    with open(manifest_path, 'w') as f:
        json.dump(manifest, f, indent=2)
    
    # Resumo final
    print("\n" + "=" * 60)
    print("📊 RESUMO FINAL")
    print("=" * 60)
    total_files = sum(1 for _ in output_path.rglob("*") if _.is_file())
    print(f"Total de arquivos extraídos: {total_files - 1}")  # -1 pelo manifest
    print(f"Pasta de saída: {output_path}")
    print(f"Manifest: {manifest_path}")
    
    # Listar UXML/USS encontrados
    uxml_files = list(output_path.rglob("*.uxml"))
    uss_files = list(output_path.rglob("*.uss"))
    if uxml_files or uss_files:
        print(f"\n🎯 Arquivos de UI encontrados:")
        print(f"   UXML: {len(uxml_files)}")
        print(f"   USS: {len(uss_files)}")
        
        if uxml_files:
            print("\n📄 Primeiros 10 UXML:")
            for f in uxml_files[:10]:
                print(f"   - {f.name}")

if __name__ == '__main__':
    main()
