# Plano de Restauração do Ctrl+P - FM26

## Status: FUNÇÃO EXISTE NO CÓDIGO

### Evidências Encontradas no Metadata

```
Initialize_ctrlKeyboardprintScreen  ← Inicializador do atalho
PrintScreen                         ← Tecla referenciada
printScreenKey                      ← Propriedade
get_printScreenKey                  ← Getter
```

### Sistema de Context Menu

```
ContextMenuData                     ← Dados do menu de contexto
PluginContextMenuContributor        ← Contribuidor de plugins
DisableContextMenu                  ← Flag para desabilitar
m_contextMenuDescriptionManipulator ← Manipulador
```

---

## Estratégia de Restauração

### Opção 1: Modificar ContextMenu via Asset Bundle

O `contextMenuData` das tabelas (ex: `PlayerSearchResultsViewCollection.json`) define as opções do menu.

**Estrutura atual:**
```json
{
  "name": "Attributes",
  "PropertyValue": "attributes",
  "ChildList": [...]
}
```

**Teoria:** Adicionar um item com `PropertyValue` que chame a função de exportação.

**Problema:** Não sabemos qual `PropertyValue` dispara a exportação.

---

### Opção 2: Injetar Atalho de Teclado

**Arquivos envolvidos:**
- `Initialize_ctrlKeyboardprintScreen` - Função de inicialização
- `printScreenKey` - Propriedade do atalho

**Possibilidade:** Criar um plugin que registre o atalho na inicialização.

---

### Opção 3: Usar FM Live Editor

**O que sabemos:**
- FM Live Editor 26 lê dados do jogo em tempo real
- Pode acessar lista de jogadores selecionados
- Pode exportar para CSV/JSON

**Vantagem:** Não requer modificação do jogo.

**Desvantagem:** Não é integrado ao jogo nativamente.

---

## Próximos Passos de Investigação

1. **Analisar DLL com Il2CppDumper** (requer Windows)
   - Extrair classes completas
   - Ver assinatura de `Initialize_ctrlKeyboardprintScreen`
   - Descobrir parâmetros necessários

2. **Procurar PropertyValue de exportação**
   - Analisar `ExportCurrentItemToBinding`
   - Descobrir qual string dispara a função

3. **Investigar sistema de plugins**
   - `PluginContextMenuContributor`
   - Como registrar novos itens

4. **Testar modificação de bundle**
   - Criar bundle modificado com novo item no contextMenu
   - Verificar se o jogo carrega

---

## Descobertas Importantes

### Comparação FM24 → FM26

| Feature | FM24 | FM26 |
|---------|------|------|
| Ctrl+A (Select All) | ✅ Funciona | ✅ Funciona |
| Ctrl+P (Export) | ✅ Funciona | ❌ Removido da UI |
| Função no código | ✅ Existe | ✅ Existe (órfã) |
| Print Screen | ✅ Screenshot | ✅ Screenshot |

### Conclusão

A função de exportação **não foi deletada** - foi apenas **desconectada** da interface. Isso significa que:

1. **É possível reativar** via patch de DLL
2. **É possível criar alternativa** via contexto menu mod
3. **FM Live Editor pode suprir** a necessidade enquanto não resolvemos

---

## Arquivos Relacionados

- `/extracted-tables/PlayerSearchResultsViewCollection.json` - Tabela de busca
- `/skins-reference/StandaloneWindows64/ui-tableviews_assets_all.bundle` - Bundle de tabelas
- `global-metadata.dat` - Metadata com todas as funções

---

## Contato

Atualizado em: 2026-02-20
Autor: Koda (OpenClaw Assistant)
