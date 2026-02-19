# Tutorial: Extrair e Editar Arquivos FMF

Este tutorial ensina como extrair, editar e reempacotar arquivos .fmf do FM26.

---

## Pré-requisitos

- Python 3.8+
- Biblioteca zstandard

```bash
pip install zstandard
```

---

## Passo 1: Localizar os Arquivos FMF

Os arquivos FMF estão em:
```
[FM26 Install]/data/
├── achievements.fmf
├── filters.fmf
├── languages.fmf
├── leaderboards.fmf
├── media.fmf
├── profanity_filter.fmf
├── settings.fmf
├── store.fmf
└── training.fmf
```

---

## Passo 2: Extrair o Conteúdo

### Script Python para Extração

```python
#!/usr/bin/env python3
"""extrair_fmf.py - Extrai arquivos .fmf do FM26"""

import zstandard as zstd
import sys
import os

def extrair_fmf(input_path, output_path=None):
    """
    Extrai um arquivo .fmf para XML.
    
    Args:
        input_path: Caminho do arquivo .fmf
        output_path: Caminho de saída (opcional, padrão: mesmo nome com .xml)
    """
    if output_path is None:
        output_path = input_path.replace('.fmf', '.xml')
    
    # Ler arquivo FMF
    with open(input_path, 'rb') as f:
        data = f.read()
    
    # Verificar assinatura
    if data[:2] != b'\x02\x01':
        print(f"⚠️ Assinatura inválida em {input_path}")
        return False
    
    if data[2:6] != b'fmf.':
        print(f"⚠️ Magic 'fmf.' não encontrado em {input_path}")
        return False
    
    # Pular header de 26 bytes
    zstd_data = data[26:]
    
    # Descomprimir
    try:
        dctx = zstd.ZstdDecompressor()
        xml_content = dctx.decompress(zstd_data, max_output_size=100*1024*1024)
    except Exception as e:
        print(f"❌ Erro ao descomprimir {input_path}: {e}")
        return False
    
    # Salvar XML
    with open(output_path, 'wb') as f:
        f.write(xml_content)
    
    print(f"✅ Extraído: {input_path} → {output_path}")
    return True

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Uso: python extrair_fmf.py <arquivo.fmf> [saida.xml]")
        sys.exit(1)
    
    input_path = sys.argv[1]
    output_path = sys.argv[2] if len(sys.argv) > 2 else None
    
    extrair_fmf(input_path, output_path)
```

### Uso

```bash
# Extrair um arquivo específico
python extrair_fmf.py achievements.fmf

# Extrair para local específico
python extrair_fmf.py achievements.fmf meu_achievements.xml

# Extrair todos os FMF da pasta data/
for f in data/*.fmf; do
    python extrair_fmf.py "$f"
done
```

---

## Passo 3: Editar o XML

### Ferramentas Recomendadas

- **VSCode** com extensão XML
- **Notepad++** (Windows)
- **Sublime Text**

### Exemplo: Adicionar uma Conquista

```xml
<!-- Abrir achievements.xml e adicionar após as existentes -->
<record>
    <string id="name" value="achievement_my_custom"/>
    <translation id="display_name" translation_id="999999" type="use" value="Minha Conquista Custom"/>
    <translation id="description" translation_id="999998" type="use" value="Uma conquista personalizada"/>
    <string id="enabled" value="Yes"/>
    <integer id="max_value" value="1"/>
    <string id="type" value="manager"/>
    <record id="mappings">
        <string id="steamworks" value="achievement_my_custom"/>
    </record>
</record>
```

### Exemplo: Desabilitar uma Conquista

```xml
<!-- Encontrar a conquista e mudar enabled para No -->
<record>
    <string id="name" value="achievement_beat_a_rival"/>
    ...
    <string id="enabled" value="No"/>  <!-- Era "Yes" -->
    ...
</record>
```

---

## Passo 4: Reempacotar para FMF

### Script Python para Reempacotamento

```python
#!/usr/bin/env python3
"""empacotar_fmf.py - Reempacota XML para .fmf"""

import zstandard as zstd
import sys

def empacotar_fmf(input_xml, output_fmf=None):
    """
    Reempacota um XML para .fmf.
    
    Args:
        input_xml: Caminho do arquivo XML
        output_fmf: Caminho de saída (opcional)
    """
    if output_fmf is None:
        output_fmf = input_xml.replace('.xml', '.fmf')
    
    # Ler XML
    with open(input_xml, 'rb') as f:
        xml_content = f.read()
    
    # Comprimir com ZSTD
    cctx = zstd.ZstdCompressor(level=19)  # Nível máximo
    compressed = cctx.compress(xml_content)
    
    # Construir header FMF
    # 02 01 = versão
    # 66 6d 66 2e = "fmf."
    # 20 bytes de zeros (metadados não críticos para edição simples)
    header = bytes([0x02, 0x01]) + b'fmf.' + bytes(20)
    
    # Salvar FMF
    with open(output_fmf, 'wb') as f:
        f.write(header + compressed)
    
    print(f"✅ Empacotado: {input_xml} → {output_fmf}")

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Uso: python empacotar_fmf.py <arquivo.xml> [saida.fmf]")
        sys.exit(1)
    
    input_xml = sys.argv[1]
    output_fmf = sys.argv[2] if len(sys.argv) > 2 else None
    
    empacotar_fmf(input_xml, output_fmf)
```

### Uso

```bash
# Reempacotar
python empacotar_fmf.py achievements.xml achievements.fmf

# Copiar de volta para o jogo
cp achievements.fmf "[FM26]/data/achievements.fmf"
```

---

## Passo 5: Testar no Jogo

1. **Backup original:**
   ```bash
   cp data/achievements.fmf data/achievements.fmf.backup
   ```

2. **Copiar arquivo modificado:**
   ```bash
   cp meu_achievements.fmf data/achievements.fmf
   ```

3. **Iniciar o jogo** e verificar se as mudanças funcionam

4. **Se der erro**, restaurar backup:
   ```bash
   cp data/achievements.fmf.backup data/achievements.fmf
   ```

---

## Troubleshooting

### Erro: "Arquivo corrompido"

- Verifique se o XML está bem formado
- Use um validador XML online
- Verifique se não há caracteres especiais

### Jogo não carrega

- Restaure o backup original
- Verifique se o tamanho do arquivo está correto
- Compare com o original extraído

### Conquistas não aparecem

- Verifique se `enabled` está como "Yes"
- Verifique os mapeamentos de plataforma
- Conquistas customizadas podem não sincronizar com Steam/Epic

---

## Próximos Passos

- [Tutorial: Extrair Texturas](./texture-extraction.md)
- [Tutorial: Substituir Uniformes](./kit-replacement.md)
- [Referência: Formato XML](../reference/xml-format.md)

---

*Tutorial criado em 2026-02-19*
