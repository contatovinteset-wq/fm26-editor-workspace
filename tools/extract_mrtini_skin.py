#!/usr/bin/env python3
"""
Extrai assets da skin MrTini FM26
"""
import UnityPy
import os
import sys

def extract_bundle(bundle_path, output_dir):
    """Extrai todos os assets de um bundle Unity"""
    print(f"Processando: {bundle_path}")
    
    try:
        env = UnityPy.load(bundle_path)
        
        for obj in env.objects:
            # Tipos de assets que queremos extrair
            if obj.type.name in ['TextAsset', 'MonoBehaviour', 'Font', 'Texture2D', 'Sprite']:
                data = obj.read()
                name = data.m_Name if hasattr(data, 'm_Name') else f"asset_{obj.path_id}"
                
                # Criar diretório de saída
                os.makedirs(output_dir, exist_ok=True)
                
                # Extrair baseado no tipo
                if obj.type.name == 'TextAsset':
                    # TextAssets podem ser XML, JSON, etc
                    content = data.m_Script
                    if isinstance(content, bytes):
                        try:
                            content = content.decode('utf-8')
                        except:
                            content = content.decode('latin-1')
                    
                    # Salvar com extensão apropriada
                    ext = '.xml' if '<?xml' in str(content)[:100] else '.txt'
                    if '{' in str(content)[:10]:
                        ext = '.json'
                    
                    out_path = os.path.join(output_dir, f"{name}{ext}")
                    with open(out_path, 'w', encoding='utf-8') as f:
                        f.write(content)
                    print(f"  -> {name}{ext}")
                    
                elif obj.type.name == 'Texture2D':
                    # Extrair texturas como PNG
                    try:
                        img = data.image
                        out_path = os.path.join(output_dir, f"{name}.png")
                        img.save(out_path)
                        print(f"  -> {name}.png")
                    except Exception as e:
                        print(f"  -> Erro ao extrair textura {name}: {e}")
                        
    except Exception as e:
        print(f"  Erro: {e}")

def main():
    skin_dir = "/data/.openclaw/workspace/fm26-editor-workspace/skin-reference/MrTini FM26 Skin v1.2/StandaloneWindows64"
    output_base = "/data/.openclaw/workspace/fm26-editor-workspace/skin-reference/mrtini-extracted"
    
    os.makedirs(output_base, exist_ok=True)
    
    for bundle_file in os.listdir(skin_dir):
        if bundle_file.endswith('.bundle'):
            bundle_path = os.path.join(skin_dir, bundle_file)
            output_dir = os.path.join(output_base, bundle_file.replace('.bundle', ''))
            extract_bundle(bundle_path, output_dir)
    
    print("\nExtração concluída!")

if __name__ == "__main__":
    main()
