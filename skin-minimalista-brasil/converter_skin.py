#!/usr/bin/env python3
"""
Conversor de backgrounds FM26 para Skin Minimalista Brasil
Substitui paleta de cores mantendo estrutura visual
"""

import os
import json
from PIL import Image
import numpy as np
from pathlib import Path

# Carregar paleta
with open('paleta.json', 'r') as f:
    PALETA = json.load(f)['paleta']

# Mapeamento de cores (de -> para)
# Detecta cores escuras e converte para nossa paleta
def rgb_to_hex(r, g, b):
    return f'#{r:02x}{g:02x}{b:02x}'

def hex_to_rgb(hex_color):
    hex_color = hex_color.lstrip('#')
    return tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4))

def get_luminance(r, g, b):
    """Calcula luminosidade perceptual"""
    return 0.299 * r + 0.587 * g + 0.114 * b

def map_color_to_palette(r, g, b):
    """Mapeia uma cor para a paleta Minimalista Brasil"""
    
    luminance = get_luminance(r, g, b)
    
    # Se for muito escuro (luminosidade < 50), usar cinza escuro
    if luminance < 30:
        return hex_to_rgb(PALETA['base_cinza_escuro']['hex'])
    
    # Se for escuro médio (30-80), usar cinza médio
    elif luminance < 80:
        return hex_to_rgb(PALETA['cinza_medio']['hex'])
    
    # Se for cinza claro (80-150), usar cinza claro
    elif luminance < 150:
        return hex_to_rgb(PALETA['cinza_claro']['hex'])
    
    # Se tiver dominância azul, usar azul da paleta
    elif b > r and b > g and luminance > 100:
        return hex_to_rgb(PALETA['azul_principal']['hex'])
    
    # Se tiver dominância amarela/dourada, usar amarelo da paleta
    elif r > 180 and g > 150 and b < 100:
        return hex_to_rgb(PALETA['amarelo_ouro']['hex'])
    
    # Manter cor original se não encaixar
    return (r, g, b)

def convert_background(input_path, output_path):
    """Converte um background para a paleta Minimalista Brasil"""
    
    print(f"Convertendo: {input_path}")
    
    # Abrir imagem
    img = Image.open(input_path)
    
    # Converter para RGB se necessário
    if img.mode != 'RGB':
        img = img.convert('RGB')
    
    # Converter para array numpy
    data = np.array(img)
    
    # Processar cada pixel
    height, width = data.shape[:2]
    
    for y in range(height):
        for x in range(width):
            r, g, b = data[y, x]
            new_color = map_color_to_palette(r, g, b)
            data[y, x] = new_color
    
    # Converter de volta para imagem
    result = Image.fromarray(data)
    
    # Salvar
    result.save(output_path, quality=95)
    print(f"Salvo: {output_path}")

def batch_convert(input_dir, output_dir, limit=None):
    """Converte todos os backgrounds de um diretório"""
    
    input_path = Path(input_dir)
    output_path = Path(output_dir)
    output_path.mkdir(parents=True, exist_ok=True)
    
    # Encontrar todas as imagens PNG
    images = list(input_path.glob('**/*.png'))
    
    if limit:
        images = images[:limit]
    
    print(f"Encontrados {len(images)} backgrounds para converter")
    
    for i, img_path in enumerate(images):
        print(f"\n[{i+1}/{len(images)}]")
        
        # Manter estrutura de diretórios
        relative = img_path.relative_to(input_path)
        out_file = output_path / relative
        out_file.parent.mkdir(parents=True, exist_ok=True)
        
        try:
            convert_background(str(img_path), str(out_file))
        except Exception as e:
            print(f"Erro em {img_path}: {e}")

if __name__ == '__main__':
    import sys
    
    if len(sys.argv) > 1:
        # Converter diretório específico
        input_dir = sys.argv[1]
        output_dir = sys.argv[2] if len(sys.argv) > 2 else './output'
        limit = int(sys.argv[3]) if len(sys.argv) > 3 else None
        batch_convert(input_dir, output_dir, limit)
    else:
        print("Uso: python converter_skin.py <input_dir> <output_dir> [limite]")
        print("Exemplo: python converter_skin.py ../skin-reference/darkside-backgrounds ./converted 5")
