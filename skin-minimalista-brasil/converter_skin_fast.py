#!/usr/bin/env python3
"""
Conversor de backgrounds FM26 para Skin Minimalista Brasil
Versão OTIMIZADA - usa numpy vectorized operations
"""

import os
import json
from PIL import Image
import numpy as np
from pathlib import Path

# Carregar paleta
with open('paleta.json', 'r') as f:
    PALETA = json.load(f)['paleta']

def hex_to_rgb(hex_color):
    hex_color = hex_color.lstrip('#')
    return np.array([int(hex_color[i:i+2], 16) for i in (0, 2, 4)], dtype=np.uint8)

# Cores da paleta como arrays numpy
CINZA_ESCURO = hex_to_rgb(PALETA['base_cinza_escuro']['hex'])
CINZA_MEDIO = hex_to_rgb(PALETA['cinza_medio']['hex'])
CINZA_CLARO = hex_to_rgb(PALETA['cinza_claro']['hex'])
AZUL = hex_to_rgb(PALETA['azul_principal']['hex'])
AMARELO = hex_to_rgb(PALETA['amarelo_ouro']['hex'])

def convert_background_fast(input_path, output_path):
    """Converte background usando operações vetorizadas (MUITO mais rápido)"""
    
    print(f"Convertendo: {input_path}")
    
    try:
        img = Image.open(input_path)
        
        if img.mode != 'RGB':
            img = img.convert('RGB')
        
        # Converter para array numpy
        data = np.array(img, dtype=np.float32)
        
        # Calcular luminosidade (vectorized)
        luminance = 0.299 * data[:,:,0] + 0.587 * data[:,:,1] + 0.114 * data[:,:,2]
        
        # Criar máscaras (vectorized)
        mask_dark = luminance < 30
        mask_medium = (luminance >= 30) & (luminance < 80)
        mask_light = (luminance >= 80) & (luminance < 150)
        
        # Aplicar cores baseado nas máscaras
        data[mask_dark] = CINZA_ESCURO
        data[mask_medium] = CINZA_MEDIO
        data[mask_light] = CINZA_CLARO
        
        # Converter de volta para uint8
        result = np.clip(data, 0, 255).astype(np.uint8)
        
        # Salvar
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        
        Image.fromarray(result).save(output_path, quality=95)
        print(f"✓ Salvo: {output_path}")
        return True
        
    except Exception as e:
        print(f"✗ Erro em {input_path}: {e}")
        return False

def batch_convert(input_dir, output_dir):
    """Converte todos os backgrounds de um diretório"""
    
    input_path = Path(input_dir)
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Encontrar todas as imagens PNG
    images = list(input_path.glob('**/*.png'))
    
    print(f"\n{'='*60}")
    print(f"Encontrados {len(images)} backgrounds para converter")
    print(f"{'='*60}\n")
    
    success = 0
    failed = 0
    
    for i, img_path in enumerate(images):
        print(f"\n[{i+1}/{len(images)}]")
        
        relative = img_path.relative_to(input_path)
        out_file = output_path / relative
        
        if convert_background_fast(str(img_path), str(out_file)):
            success += 1
        else:
            failed += 1
    
    print(f"\n{'='*60}")
    print(f"CONVERSÃO COMPLETA!")
    print(f"✓ Sucesso: {success}")
    print(f"✗ Falharam: {failed}")
    print(f"{'='*60}\n")

if __name__ == '__main__':
    import sys
    
    if len(sys.argv) >= 3:
        input_dir = sys.argv[1]
        output_dir = sys.argv[2]
        batch_convert(input_dir, output_dir)
    else:
        print("Uso: python converter_skin_fast.py <input_dir> <output_dir>")
        print("Exemplo: python converter_skin_fast.py ../skin-reference/darkside-v4.3-extracted/ui-backgrounds_assets_common ./backgrounds-convertidos")
