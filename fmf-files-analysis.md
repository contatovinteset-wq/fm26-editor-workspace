# Arquivos FMF do FM26

## Lista de FMFs Encontrados

| Arquivo | Tamanho | Conteúdo |
|---------|---------|----------|
| languages.fmf | 197MB | Todas as traduções do jogo |
| media.fmf | 3.7MB | Coletivas de imprensa, notícias |
| filters.fmf | 14KB | Filtros de busca |
| training.fmf | 9.6KB | Configurações de treino |
| achievements.fmf | 4.9KB | Conquistas Steam/Plataformas |
| settings.fmf | 3.0KB | Configurações do jogo |
| store.fmf | 1.4KB | Itens da loja (Editor) |
| profanity_filter.fmf | 3.4KB | Filtro de palavrões |
| leaderboards.fmf | 1.1KB | Leaderboards online |

---

## Formato FMF

### Estrutura do Header:
```
Magic: "FMF\x00" (4 bytes)
Version: uint32 (4 bytes)
Data: zstd compressed XML
```

### Decompressão:
```bash
# Usar zstd
zstd -d languages.fmf -o languages.xml
```

---

## FMFs não encontrados

### Database Principal:
- **NÃO EXISTE** arquivo FMF de database no diretório analisado
- Database deve estar em:
  - Asset Bundle específico
  - Arquivos internos do jogo
  - Steam Workshop

### Match Engine:
- Configurações devem estar em bundles
- Possivelmente em `game_plugin.dll` (hardcoded)

---

## Extraindo FMFs

### Script Python:
```python
import zstandard as zstd
import struct

def extract_fmf(fmf_path, output_path):
    with open(fmf_path, 'rb') as f:
        magic = f.read(4)
        version = struct.unpack('<I', f.read(4))[0]
        compressed_data = f.read()
    
    dctx = zstd.ZstdDecompressor()
    decompressed = dctx.decompress(compressed_data)
    
    with open(output_path, 'wb') as f:
        f.write(decompressed)
```

---

## Modificando FMFs

### Processo:
1. Extrair FMF → XML
2. Editar XML
3. Comprimir com zstd
4. Recriar FMF com header correto

### Cuidados:
- Manter versão correta
- Não quebrar estrutura XML
- Testar no jogo após modificação

---

## Próximos Passos

1. Criar ferramenta de extração/compressão FMF
2. Investigar onde está a database
3. Encontrar configurações de Match Engine
4. Documentar estrutura completa
