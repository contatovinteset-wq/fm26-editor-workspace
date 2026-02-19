# FM26 Wiki - Manual de Modificação

> Documentação completa dos arquivos do Football Manager 26

---

## 📚 Índice

1. [Visão Geral](#visão-geral)
2. [Estrutura de Arquivos](#estrutura-de-arquivos)
3. [Formatos de Arquivo](#formatos-de-arquivo)
4. [O que Pode Ser Editado](#o-que-pode-ser-editado)
5. [Ferramentas Necessárias](#ferramentas-necessárias)
6. [Tutoriais](#tutoriais)
7. [Referência de Arquivos](#referência-de-arquivos)

---

## Visão Geral

| Propriedade | Valor |
|-------------|-------|
| **Jogo** | Football Manager 26 |
| **Desenvolvedor** | Sports Interactive |
| **Engine** | Unity 6000.0.52f1 (URP) |
| **Compilação** | IL2CPP (C# → Nativo) |
| **Tamanho Total** | ~7.3 GB |

### Arquitetura do Jogo

```
┌─────────────────────────────────────────────────────────────┐
│                    CAMADA DE APLICAÇÃO                       │
│  (DLLs compilados via IL2CPP - não editáveis diretamente)   │
├─────────────────────────────────────────────────────────────┤
│  FMGame.dll │ FM.UI.dll │ FM.Match.dll │ FM.Graphics.dll   │
├─────────────────────────────────────────────────────────────┤
│                     CAMADA DE DADOS                          │
│        (Arquivos .fmf e Asset Bundles - editáveis)          │
├─────────────────────────────────────────────────────────────┤
│  data/*.fmf │ fm_Data/VietNorSteam/*.bundle                 │
└─────────────────────────────────────────────────────────────┘
```

---

## Estrutura de Arquivos

```
FM26/
├── Football Manager 2026.exe      # Executável principal
├── data/                          # 📁 Dados do jogo (344MB)
│   ├── achievements.fmf           # Conquistas
│   ├── filters.fmf               # Filtros
│   ├── languages.fmf             # Idiomas (197MB)
│   ├── leaderboards.fmf          # Placares
│   ├── media.fmf                 # Coletivas
│   ├── profanity_filter.fmf      # Filtro
│   ├── settings.fmf              # Configs
│   ├── store.fmf                 # Loja
│   └── training.fmf              # Treino
│
├── fm_Data/                       # 📁 Unity Data (2.5GB)
│   ├── globalgamemanagers        # Gerenciadores
│   ├── resources.assets          # Recursos
│   ├── il2cpp_data/              # Metadados IL2CPP
│   │   └── Metadata/global-metadata.dat
│   ├── Resources/                # DLLs nativas
│   ├── StreamingAssets/          # Assets streaming
│   └── VietNorSteam/             # Asset Bundles (1.9GB)
│       └── aa/StandaloneWindows64/
│           ├── art-characters-*.bundle    # Personagens
│           └── art-environments-*.bundle  # Ambientes
│
├── D3D12/                         # DirectX 12
└── dotnet/                        # .NET Runtime
```

---

## Formatos de Arquivo

### .FMF (Football Manager File)

**Formato proprietário da Sports Interactive**

| Offset | Tamanho | Conteúdo |
|--------|---------|----------|
| 0x00 | 2 bytes | Versão (02 01) |
| 0x02 | 4 bytes | Magic "fmf." |
| 0x06 | 20 bytes | Metadados |
| 0x1A | ... | Dados comprimidos (ZSTD) |

**Como extrair:**
```python
import zstandard as zstd

with open("arquivo.fmf", "rb") as f:
    data = f.read()

# Pular header de 26 bytes
zstd_data = data[26:]

# Descomprimir
dctx = zstd.ZstdDecompressor()
xml_content = dctx.decompress(zstd_data, max_output_size=10*1024*1024)
```

---

### .BUNDLE (Unity Asset Bundle)

**Formato padrão UnityFS**

| Campo | Valor |
|-------|-------|
| Assinatura | UnityFS |
| Versão Unity | 6000.0.52f1-fm26-05f1 |
| Compressão | LZ4/HC |

**Conteúdo típico:**
- Texture2D (texturas)
- Mesh (modelos 3D)
- Material (materiais)
- Animator (animações)
- GameObject (objetos)

---

### .DLL (Dynamic Link Library)

**DLLs do jogo compilados via IL2CPP**

| DLL | Função |
|-----|--------|
| FMGame.dll | Núcleo do jogo |
| FM.UI.dll | Interface do usuário |
| FM.Match.dll | Motor de partida |
| FM.Graphics.dll | Gráficos 3D |
| SI.Core.dll | Núcleo SI |
| SI.Bindable.dll | Sistema de binding |

**⚠️ Não editáveis diretamente** - requer reverse engineering avançado

---

## O que Pode Ser Editado

### ✅ Alta Viabilidade (Fácil)

| Arquivo | O que faz | Como editar |
|---------|-----------|-------------|
| achievements.fmf | Conquistas | Extrair → Editar XML → Reempacotar |
| leaderboards.fmf | Placares | Extrair → Editar XML → Reempacotar |
| store.fmf | Itens da loja | Extrair → Editar XML → Reempacotar |
| media.fmf | Coletivas | Extrair → Editar XML → Reempacotar |

### ⚠️ Média Viabilidade (Moderado)

| Arquivo | O que faz | Como editar |
|---------|-----------|-------------|
| *.bundle (texturas) | Uniformes, cabelos | AssetStudio → Editar PNG → Reinject |
| languages.fmf | Textos/idiomas | Formato binário complexo |
| *.bundle (modelos) | Personagens | Blender → Export → Reinject |

### ❌ Baixa Viabilidade (Difícil)

| Arquivo | O que faz | Por que é difícil |
|---------|-----------|-------------------|
| DLLs | Lógica do jogo | Compilados via IL2CPP |
| global-metadata.dat | Metadados | Binário proprietário |
| Executáveis | Código principal | Compilado nativo |

---

## Ferramentas Necessárias

### Para FMF
| Ferramenta | Uso | Link |
|------------|-----|------|
| Python + zstandard | Descompressão | `pip install zstandard` |
| Editor XML | Edição | VSCode, Notepad++ |

### Para Asset Bundles
| Ferramenta | Uso | Link |
|------------|-----|------|
| AssetStudio | Extração | github.com/Perfare/AssetStudio |
| UABE | Edição | github.com/SeriousCache/UABE |
| UnityPy | Python | `pip install UnityPy` |

### Para Modelos/Texturas
| Ferramenta | Uso | Link |
|------------|-----|------|
| Blender | Modelos 3D | blender.org |
| Photoshop/GIMP | Texturas | Adobe/GIMP |
| TexConv | Conversão | Microsoft |

### Para Análise
| Ferramenta | Uso | Link |
|------------|-----|------|
| IL2CPP Dumper | Metadados | github.com/Perfare/Il2CppDumper |
| dnSpy | Debug | github.com/dnSpy/dnSpy |
| Hex Editor | Binários | HxD, ImHex |

---

## Tutoriais

- [Tutorial: Extrair e editar arquivos FMF](./tutorials/fmf-extraction.md)
- [Tutorial: Extrair texturas de Asset Bundles](./tutorials/texture-extraction.md)
- [Tutorial: Substituir uniformes](./tutorials/kit-replacement.md)
- [Tutorial: Análise de DLLs](./tutorials/dll-analysis.md)

---

## Referência de Arquivos

- [Arquivos FMF](./reference/fmf-files.md)
- [Asset Bundles](./reference/asset-bundles.md)
- [DLLs e Assemblies](./reference/assemblies.md)
- [Formato XML](./reference/xml-format.md)

---

## Próximos Passos

1. [ ] Mapear todos os XMLs extraídos
2. [ ] Documentar estrutura de cada arquivo
3. [ ] Criar ferramenta de extração automatizada
4. [ ] Investigar exportação de dados (Ctrl+P)
5. [ ] Analisar sistema de UI (FM.UI.dll)

---

*Wiki criada em 2026-02-19*
