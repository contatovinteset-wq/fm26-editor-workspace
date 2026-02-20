# Guia Prático: Exportar Dados do FM26

**Objetivo:** Viabilizar exportação de lista de jogadores (estilo Ctrl+P do FM24)

---

## 🎯 Solução Mais Viável: FM Live Editor 26

### O que é:
Ferramenta do FMScout que edita dados em tempo real.

### Verificar se tem exportação:
1. Abra o FM Live Editor 26
2. Carregue um save
3. Vá em uma lista de jogadores
4. Procure por botão "Export" ou "Copy to Clipboard"
5. Verifique se há opção de exportar para CSV/HTML

### Se tiver:
- ✅ Problema resolvido
- Use para exportar dados para moneyball

### Se NÃO tiver:
- Verifique a documentação do FMSE
- Procure por plugins ou scripts
- Considere a opção de skin abaixo

---

## 🛠️ Solução Alternativa: Mod de Skin

### Como funciona:
As skins do FM26 podem modificar bindings de teclado.

### Passos para testar:
1. Baixe uma skin existente (ex: MrTini23 FM26 Skin)
2. Extraia e analise a estrutura
3. Procure por arquivos de configuração de teclas
4. Adicione binding para Ctrl+P → ExportCurrentItem

### Arquivos prováveis:
- `config.xml` ou `settings.xml`
- `keyboard bindings.xml`
- Arquivos `.uxml` na pasta de UI

### Riscos:
- Pode não funcionar se a função foi completamente removida
- Pode causar instabilidade

---

## 📊 Solução Externa: Captura de Dados

### Opção A: FM Live Editor + Script
Se o FMSE não tem exportação direta:

1. Use FMSE para ver dados na tela
2. Crie script que lê a memória
3. Exporte para CSV

### Opção B: Análise de Save Game
1. Abra o arquivo .fm do save
2. Extraia dados dos jogadores
3. Converta para HTML/CSV

**Ferramentas:**
- FM Save Editor
- Genie Scout
- Analisadores de .fm

---

## 🧪 Testes a Fazer

### Teste 1: Print Screen
O metadata mostra `Initialize_ctrlKeyboardprintScreen`
- Tente **Print Screen** no jogo
- Veja se abre alguma opção de exportação

### Teste 2: Menu de Contexto
- Clique com botão direito em um jogador
- Procure por "Export" ou "Print"
- Verifique se há opções escondidas

### Teste 3: Arquivo de Treino
O metadata mostra `ExportTrainingSchedule`
- Vá em Treinos
- Procure por botão de exportação
- Se existe para treinos, pode existir para jogadores

---

## 📋 Checklist

- [ ] Testar Print Screen no jogo
- [ ] Verificar menu de contexto (botão direito)
- [ ] Testar exportação de treinos
- [ ] Instalar FM Live Editor 26
- [ ] Verificar se FMSE tem exportação
- [ ] Baixar e analisar uma skin existente
- [ ] Procurar por mods de exportação no FMScout

---

## 💡 Próximos Passos

1. **Imediato:** Testar Print Screen e menus
2. **Curto prazo:** Verificar FM Live Editor
3. **Médio prazo:** Criar mod de skin
4. **Longo prazo:** Desenvolver ferramenta própria

---

## 📞 Informações Necessárias

Para prosseguir, preciso saber:

1. **FM Live Editor 26 tem função de exportar lista?**
2. **Print Screen abre algo no jogo?**
3. **O botão direito mostra opções de exportação?**
4. **Existe exportação em Treinos?**

Com essas respostas, posso direcionar melhor a solução!
