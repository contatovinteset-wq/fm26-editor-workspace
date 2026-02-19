# Investigação: Exportação de Dados (Ctrl+P)

## Contexto

O atalho **Ctrl+P** era usado em versões anteriores do FM para exportar dados do jogo em formato texto/CSV. Esta funcionalidade foi removida em versões recentes.

## Análise do Metadata

### Strings encontradas relacionadas a export:
- `ExportPlayerData`
- `ExportMatchData`
- `ExportCSV`
- `DumpToFile`

### No metadata IL2CPP:
```
TransferOffersOptions
PlayerInstructionEntries
DatabaseObjectImageHelpers
```

## Hipótese

A funcionalidade de exportação provavelmente existe no código mas está desabilitada. Possíveis métodos de reativação:

### 1. Hook de DLL (IL2CPP)
- Usar Il2CppDumper para gerar headers
- Identificar função de export
- Criar DLL injetora que chama a função

### 2. Modificação de UI
- Encontrar botão escondido no .uxml
- Reativar via modificação de bundle

### 3. Memory Editing
- Usar Cheat Engine ou similar
- Chamar função diretamente na memória

## Ferramentas Necessárias

1. **Il2CppDumper** ✓ (já baixado)
2. **dnSpy** ou **ILSpy** (para análise)
3. **BepInEx** (framework de modding Unity)
4. **MelonLoader** (alternativa para IL2CPP)

## Próximos Passos

1. Rodar Il2CppDumper no Windows com:
   - `game_plugin.dll` (GameAssembly)
   - `global-metadata.dat`

2. Analisar output:
   - `dump.cs` - classes e métodos
   - `script.json` - endereços

3. Procurar por:
   - `Export*` methods
   - `Print*` methods  
   - `Debug*` methods
   - `CtrlP` handlers

---

## Alternativa: FM Live Editor 26

O FM Live Editor do FMScout oferece funcionalidade similar:
- Exportar dados de jogadores
- Ver IDs de clubes/jogadores
- Editar atributos em tempo real

**Link:** https://www.fmscout.com/a-fm-live-editor-26.html
**Preço:** £5.49 (licença única)

---

## Conclusão

A melhor abordagem atual é:
1. Usar FM Live Editor para exportação imediata
2. Investigar reativação do Ctrl+P via modding
3. Criar ferramenta própria de exportação se necessário
