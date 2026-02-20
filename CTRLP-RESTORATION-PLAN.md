# Ctrl+P Export - Plano de Restauração

**Data:** 20/02/2026
**Objetivo:** Restaurar funcionalidade de exportação de dados (Ctrl+P) no FM26

---

## 🔍 Análise Técnica

### O que encontramos no metadata:

| Função | Status | Descrição |
|--------|--------|-----------|
| `Initialize_ctrlKeyboardp` | ✅ Existe | Tecla P com Ctrl está registrada |
| `SelectAll` | ✅ Existe | Função de selecionar todos |
| `ExportCurrentItemToBinding` | ✅ Existe | Exporta item atual |
| `CreateExportDataFromCustomView` | ✅ Existe | Cria dados de exportação |
| `CustomViewExportData` | ✅ Existe | Dados de exportação customizada |
| `ExportCurrentViewLabel` | ✅ Existe | Label da view atual |
| `TableView` / `StreamedTableView` | ✅ Existe | Tabela de jogadores |

### Conclusão:
**As funções EXISTEM no código.** O problema é que o **binding** entre Ctrl+P e a função de exportação foi removido ou desabilitado na UI.

---

## 🛠️ Soluções Possíveis

### Opção 1: Mod de Skin (MAIS VIÁVEL)
As skins do FM26 podem adicionar/modificar bindings de teclado.

**Passos:**
1. Extrair skin padrão do jogo
2. Modificar arquivo de bindings
3. Adicionar: `<Binding key="Ctrl+P" action="ExportCurrentItem" />`
4. Reempacotar e instalar

**Referências:**
- FM26 usa Unity UI Toolkit (.uxml/.uss)
- Skins podem sobrescrever comportamentos

### Opção 2: FM Live Editor 26
O FM Live Editor pode ter função de exportação ou permitir hooks.

**Verificar:**
- Se tem função de "Export Squad"
- Se tem API para capturar dados
- Se pode injetar código

### Opção 3: Solução Externa (WORKAROUND)
Criar ferramenta que captura dados de outra forma.

**Alternativas:**
1. **Screenshot + OCR** - Não ideal (limitado)
2. **Captura de memória** - FM Live Editor faz isso
3. **Export via arquivo de save** - Analisar .fm files
4. **Clipping de dados** - Via FMSE/FMGE

---

## 📋 Plano de Ação

### Fase 1: Investigar Skins
- [ ] Extrair skin padrão do FM26
- [ ] Localizar arquivo de bindings de teclado
- [ ] Verificar se pode adicionar Ctrl+P

### Fase 2: FM Live Editor
- [ ] Verificar se tem função de exportação
- [ ] Testar se consegue exportar lista de jogadores
- [ ] Verificar documentação da API

### Fase 3: Solução Externa
- [ ] Criar script Python que lê save game
- [ ] Extrair dados de jogadores do .fm
- [ ] Converter para CSV/HTML

---

## 🎯 Próximos Passos Imediatos

1. **Testar FM Live Editor** - Verificar se tem exportação
2. **Investigar estrutura de skins** - Extrair e analisar
3. **Criar ferramenta de exportação** - Se necessário

---

## 💡 Perguntas para Responder

1. FM Live Editor tem função de exportar lista?
2. Skins podem adicionar bindings de teclado?
3. Qual formato o export HTML usava?
4. Dados estão acessíveis via save game?

---

## 📁 Arquivos Relacionados

- `ctrlp-deep-investigation.md` - Análise anterior
- `config-analysis.txt` - Todas as refs de export
- `fm_Data/` - Arquivos do jogo para extrair
