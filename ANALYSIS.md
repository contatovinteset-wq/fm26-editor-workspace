# FM26 Editor - Análise de Arquivos

## Visão Geral

**Jogo:** Football Manager 26  
**Desenvolvedor:** Sports Interactive  
**Engine:** Unity (IL2CPP)  
**Tamanho Total:** 2.9GB (parcial - aguardando Mega)

---

## Estrutura de Pastas

### `data/` (344MB)
Arquivos de dados do jogo em formato proprietário.

| Arquivo | Tamanho | Formato | Propósito |
|---------|---------|---------|-----------|
| languages.fmf | 197MB | FMF (zstd+XML) | Textos/idiomas |
| media.fmf | 3.7MB | FMF (zstd+XML) | Mídia (imagens, áudio) |
| filters.fmf | 14KB | FMF (zstd+XML) | Filtros de busca |
| training.fmf | 9.6KB | FMF (zstd+XML) | Configs de treinamento |
| achievements.fmf | 4.9KB | FMF (zstd+XML) | Conquistas |
| profanity_filter.fmf | 3.4KB | FMF (zstd+XML) | Filtro de palavrões |
| settings.fmf | 3.0KB | FMF (zstd+XML) | Configurações |
| leaderboards.fmf | 1.1KB | FMF (zstd+XML) | Placares |
| store.fmf | 1.4KB | FMF (zstd+XML) | Itens da loja |

**Formato FMF:**
- Header: `02 01 66 6d 66` (assinatura "fmf")
- Conteúdo: Dados comprimidos com **zstd**
- Interno: XML estruturado

**Editável?** SIM - precisa descompactar zstd → editar XML → recompactar

---

### `fm_Data/` (2.5GB)
Dados do Unity (asset bundles, configurações, recursos).

#### Raiz (7.8MB)
| Arquivo | Tamanho | Propósito |
|---------|---------|-----------|
| app.info | 38 bytes | Info do app (Sports Interactive, FM26) |
| boot.config | 207 bytes | Config de boot do Unity |
| globalgamemanagers | 328KB | Gerenciadores globais |
| globalgamemanagers.assets | 1.4MB | Assets globais |
| globalgamemanagers.assets.resS | 4.0MB | Recursos de assets globais |
| level0 | 40KB | Cena inicial |
| manifest.dat | 32KB | Manifesto de assets |
| resources.assets | 571KB | Recursos |
| resources.assets.resS | 1.1MB | Recursos de recursos |
| sharedassets0.assets | 383KB | Assets compartilhados |
| sharedassets0.assets.resS | 416 bytes | Recursos compartilhados |
| RuntimeInitializeOnLoads.json | 13KB | Inicialização de módulos |
| ScriptingAssemblies.json | 6.1KB | Lista de DLLs |

#### `fm_Data/il2cpp_data/` (52MB)
- `Metadata/global-metadata.dat` (15MB) - Metadados do IL2CPP
- `il2cpp.usym` (36MB) - Símbolos de depuração
- `Resources/` - Recursos do .NET

**Editável?** PARCIALMENTE - metadados podem ser extraídos com ferramentas IL2CPP

#### `fm_Data/Resources/` (454MB)
- DLLs nativas (x86_64)
- Bibliotecas do sistema

**Editável?** NÃO - arquivos de sistema compilados

#### `fm_Data/StreamingAssets/` (6.1MB)
- `unity default resources` (5.86MB)
- `unity_builtin_extra` (497KB)

**Editável?** PARCIALMENTE - assets do Unity

#### `fm_Data/VietNorSteam/` (1.9GB)
**36 Asset Bundles** contendo:

| Bundle | Tamanho | Conteúdo |
|--------|---------|----------|
| art-characters-male-outfits_assets_all | 361MB | Roupas masculinas |
| art-characters-female-outfits_assets_all | 356MB | Roupas femininas |
| art-environments-cityassets_assets_all | 185MB | Cidade/ambiente |
| art-characters-male-skin_assets_all | 206MB | Pele masculina |
| art-characters-female-skin_assets_all | 178MB | Pele feminina |
| art-characters-male-hair_assets_all | 163MB | Cabelo masculino |
| art-characters-female-player_assets_all | 68MB | Jogadoras |
| art-characters-male-player_assets_all | 58MB | Jogadores |
| ... | ... | ... |

**Editável?** SIM - Asset Bundles podem ser extraídos com AssetStudio ou UABE

---

### `D3D12/` (5.7MB)
- `D3D12Core.dll` - DirectX 12 runtime

**Editável?** NÃO - DLL de sistema

---

### `dotnet/` (13MB - incompleto)
DLLs do .NET Runtime (Microsoft).

**Editável?** NÃO - bibliotecas de sistema

---

## Arquitetura do Jogo

### Assemblies Principais (DLLs)
O jogo usa Unity IL2CPP, compilando C# para código nativo. Principais módulos:

| Assembly | Função |
|----------|--------|
| FMGame.dll | Núcleo do jogo |
| FM.GamePlugin.dll | Plugins do jogo |
| FM.Graphics.dll | Gráficos 3D |
| FM.UI.dll | Interface do usuário |
| FM.Match.dll | Motor de partida |
| FM.Input.dll | Sistema de input |
| FM.Performance.dll | Otimização |
| SI.Core.dll | Núcleo da SI |
| SI.Bindable.dll | Sistema de binding |
| SI.UI.dll | Componentes UI |
| SI.Match.dll | Lógica de partida |

### Sistema de UI
- **UI Toolkit** (Unity UIElements) - nova UI do FM26
- **Bindable System** - sistema de data binding proprietário da SI
- **Visual Scripting** - scripts visuais para UI

### Gráficos
- **URP** (Universal Render Pipeline)
- **Cinemachine** - sistema de câmeras
- **Addressables** - carregamento dinâmico de assets

---

## O que Pode Ser Editado

### ✅ Altamente Editável
1. **Arquivos .fmf** (data/) → descompactar zstd → editar XML
2. **Asset Bundles** (VietNorSteam/) → extrair com AssetStudio
3. **Configurações XML** (versus comps, templates)

### ⚠️ Moderadamente Editável
1. **Arquivos .assets** → Unity Assets, extração possível
2. **globalgamemanagers** → configurações globais
3. **StreamingAssets** → recursos do Unity

### ❌ Não Editável (Sistema)
1. **DLLs** (dotnet/, Resources/)
2. **IL2CPP metadata** (il2cpp_data/)
3. **Executáveis**

---

## Próximos Passos

1. [ ] Baixar pasta completa do Mega (7.33GB)
2. [ ] Criar ferramenta para descompactar .fmf
3. [ ] Extrair asset bundles com AssetStudio
4. [ ] Documentar formato dos arquivos
5. [ ] Criar editor visual para configs
6. [ ] Identificar bugs corrigíveis
7. [ ] Restaurar função de export de dados

---

## Ferramentas Necessárias

- **zstd** - descompressão dos .fmf
- **AssetStudio** - extração de Asset Bundles
- **UABE** - edição de Asset Bundles
- **IL2CPP Dumper** - análise de metadados
- **dnSpy** - análise de DLLs (se necessário)

---

*Análise gerada em 18/02/2026*
