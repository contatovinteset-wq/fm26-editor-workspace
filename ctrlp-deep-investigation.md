# Investigação Ctrl+P (Exportação)

**Data:** 20/02/2026
**Status:** Em andamento

---

## 🔍 Descobertas no Metadata

### Funções de Exportação Encontradas

| Função | Descrição |
|--------|-----------|
| `ExportCurrentItemToBinding` | Exporta item atual para binding |
| `ExportCurrentItemToBinding_UxmlAttributeFlags` | Flags UI para exportação |
| `CreateExportDataFromCustomView` | Cria dados de view customizada |
| `CustomViewExportData` | Dados de exportação customizados |
| `ExportTrainingSchedule` | Exporta escala de treino |
| `BindableExportPaths` | Paths de exportação vinculáveis |
| `BindableExportData` | Dados de exportação vinculáveis |
| `ExportParameters` | Parâmetros de exportação |

### Teclas Relacionadas

| String | Contexto |
|--------|----------|
| `Initialize_ctrlKeyboardprintScreen` | Inicialização do Print Screen |
| `Print Screen` | Tecla Print Screen |
| `KeyPrint` | Código de tecla Print |

---

## 📝 Análise

### O que aconteceu com Ctrl+P?

1. **Não foi totalmente removido** - as funções de exportação ainda existem
2. **Binding pode ter mudado** - a tecla Ctrl+P pode não estar mais vinculada
3. **UI Toolkit** - o novo sistema de UI pode ter mudado como exportação funciona

### Possíveis Soluções

#### Opção 1: Hook na função
```python
# Injetar código que chama ExportCurrentItemToBinding()
# quando uma tecla específica é pressionada
```

#### Opção 2: Modificar binding
```xml
<!-- Adicionar binding de teclado no arquivo de UI -->
<Binding key="Ctrl+P" action="ExportCurrentItem" />
```

#### Opção 3: Plugin/Lua Script
- FM Live Editor pode ter função de exportação
- Verificar se existe API para chamar exportação

---

## 🧪 Testes Necessários

1. **Verificar se Print Screen funciona** (KeyPrint)
   - Pode ser alternativa ao Ctrl+P

2. **Verificar ExportTrainingSchedule**
   - Treinos ainda podem ser exportados?
   - Menu → Treinos → Exportar

3. **Investigar FM Live Editor**
   - Tem função de exportar dados/tabelas?
   - Pode substituir Ctrl+P

---

## 📁 Arquivos Relacionados

- `FM.UI.dll` - Contém classes de UI
- `FM.GameConfig.dll` - Configurações
- `.uxml` files - Templates de UI
- `.uss` files - Estilos de UI

---

## 🎯 Próximos Passos

- [ ] Extrair templates .uxml dos bundles
- [ ] Procurar bindings de teclado
- [ ] Testar Print Screen no jogo
- [ ] Verificar FM Live Editor para exportação
- [ ] Investigar se mods de UI podem restaurar Ctrl+P
