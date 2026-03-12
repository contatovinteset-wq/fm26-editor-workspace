#!/usr/bin/env python3
"""
Darkside v4.3 Safe Extractor
Processa bundles um por vez para evitar segfault
"""

import UnityPy
import json
import gc
from pathlib import Path

def extract_bundle_safe(bundle_path: Path, output_dir: Path):
    """Extrai um bundle com controle de memória"""
    print(f"\n📦 {bundle_path.name}")
    
    try:
        # Carregar bundle
        env = UnityPy.load(str(bundle_path))
        
        stats = {'UXML': 0, 'USS': 0, 'Texture': 0, 'Font': 0, 'Other': 0}
        
        # Processar objetos
        for obj in env.objects:
            try:
                if obj.type.name == 'TextAsset':
                    data = obj.read()
                    name = getattr(data, 'm_Name', f"asset_{obj.path_id}")
                    name = name.replace('/', '_').replace('\\', '_')
                    
                    content = data.m_Script
                    if isinstance(content, bytes):
                        content = content.decode('utf-8', errors='replace')
                    
                    # Detectar tipo
                    if '<ui:UXML' in content or '<UXML' in content:
                        ext = 'uxml'
                        stats['UXML'] += 1
                    elif '.uss' in name.lower() or 'stylesheet' in content.lower():
                        ext = 'uss'
                        stats['USS'] += 1
                    elif content.strip().startswith(('{', '[')):
                        ext = 'json'
                        stats['Other'] += 1
                    else:
                        ext = 'txt'
                        stats['Other'] += 1
                    
                    # Salvar
                    type_dir = output_dir / 'TextAsset'
                    type_dir.mkdir(parents=True, exist_ok=True)
                    out_path = type_dir / f"{name}.{ext}"
                    
                    with open(out_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                
                elif obj.type.name in ['Texture2D', 'Sprite']:
                    data = obj.read()
                    name = getattr(data, 'm_Name', f"asset_{obj.path_id}")
                    name = name.replace('/', '_').replace('\\', '_')
                    
                    image = data.image
                    if image:
                        type_dir = output_dir / obj.type.name
                        type_dir.mkdir(parents=True, exist_ok=True)
                        out_path = type_dir / f"{name}.png"
                        image.save(out_path)
                        stats['Texture'] += 1
                
                elif obj.type.name == 'Font':
                    data = obj.read()
                    name = getattr(data, 'm_Name', f"font_{obj.path_id}")
                    
                    type_dir = output_dir / 'Font'
                    type_dir.mkdir(parents=True, exist_ok=True)
                    out_path = type_dir / f"{name}.json"
                    
                    font_info = {'name': name, 'type': 'Font'}
                    with open(out_path, 'w') as f:
                        json.dump(font_info, f, indent=2)
                    stats['Font'] += 1
                    
            except Exception as e:
                # Ignorar erros individuais
                pass
        
        # Limpar memória
        del env
        gc.collect()
        
        # Resumo
        total = sum(stats.values())
        if total > 0:
            print(f"✅ {total} assets: ", end='')
            parts = [f"{k}={v}" for k, v in stats.items() if v > 0]
            print(", ".join(parts))
        else:
            print("⚠️  Nenhum asset")
        
        return stats
        
    except Exception as e:
        print(f"❌ ERRO: {str(e)}")
        return {}

def main():
    bundles_path = Path("/data/.openclaw/workspace/darkside-skin-v4.3")
    output_base = Path("/data/.openclaw/workspace/fm26-editor-workspace/skin-reference/darkside-v4.3-extracted")
    
    # Bundles críticos primeiro (só UI/estilos)
    priority = [
        'ui-styles_assets_default.bundle',
        'ui-styles_assets_match.bundle',
        'ui-tableviews_assets_all.bundle',
        'ui-widgets_assets_all.bundle',
        'ui-tiles_assets_all.bundle',
        'ui-tileslayouts_assets_all.bundle'
    ]
    
    print("=" * 60)
    print("🎨 DARKSIDE V4.3 - EXTRATOR SEGURO")
    print("=" * 60)
    
    total_stats = {}
    
    for bundle_name in priority:
        bundle_path = bundles_path / bundle_name
        if not bundle_path.exists():
            print(f"\n⚠️  {bundle_name} não encontrado")
            continue
        
        output_dir = output_base / bundle_path.stem
        stats = extract_bundle_safe(bundle_path, output_dir)
        total_stats[bundle_name] = stats
    
    # Salvar manifest
    output_base.mkdir(parents=True, exist_ok=True)
    manifest_path = output_base / 'extraction_manifest.json'
    with open(manifest_path, 'w') as f:
        json.dump(total_stats, f, indent=2)
    
    # Resumo
    print("\n" + "=" * 60)
    print("📊 RESUMO")
    print("=" * 60)
    
    all_uxml = list(output_base.rglob("*.uxml"))
    all_uss = list(output_base.rglob("*.uss"))
    all_textures = list(output_base.rglob("*.png"))
    
    print(f"UXML: {len(all_uxml)}")
    print(f"USS: {len(all_uss)}")
    print(f"Texturas: {len(all_textures)}")
    print(f"\n📁 {output_base}")
    
    if all_uxml:
        print(f"\n📄 UXMLs encontrados:")
        for f in sorted(all_uxml)[:15]:
            print(f"   - {f.name}")

if __name__ == '__main__':
    main()
