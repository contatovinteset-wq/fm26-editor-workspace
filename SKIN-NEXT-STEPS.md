# 🎨 Skin FM26 Brasileira - Próximos Passos

**Status atual**: ✅ Análise técnica concluída  
**Data**: 2026-03-10

---

## 📊 O Que Descobrimos

### ✅ Materiais Obtidos:
1. **Darkside v4.3** - 242 MB de bundles atualizados
2. **MrTini v1.2** - 133 MB de bundles anteriores
3. **833 MonoBehaviours** extraídos (estilos compilados)
4. **70 texturas de backgrounds** sendo extraídas

### ⚠️ Limitação Técnica Crítica:
**FM26 NÃO usa XMLs editáveis!**

- UI é **programática** (MonoBehaviours C#)
- StyleSheets são **binários compilados**
- Modificação de estrutura/cores requer **hex editing arriscado**

**Ver**: `docs/fm26-skin-architecture.md` para detalhes técnicos

---

## 🎯 Estratégia Ajustada

### Opção Escolhida: **Skin de Texturas** (Viável + Segura)

Focamos em:
1. **Backgrounds brasileiros** (wallpapers, fundos de menu)
2. **Paleta visual coesa**
3. **Identidade brasileira elegante**

**Não faremos** (inviável sem DLL mod):
- ❌ Atributos em bolinhas coloridas
- ❌ Modificação de layouts
- ❌ Cores de UI programáticas

---

## 📋 Tarefas Imediatas

### 1. Análise Visual do Darkside
- [ ] Ver backgrounds extraídos (em processamento)
- [ ] Identificar padrão de cores/estética
- [ ] Mapear quais texturas são modificadas

**Pasta**: `skin-reference/darkside-backgrounds/`

### 2. Comparação MrTini vs Darkside
- [ ] Extrair backgrounds da MrTini
- [ ] Fazer diff visual
- [ ] Entender diferenças de abordagem

### 3. Conceito Visual Brasileiro
- [ ] Definir paleta (cores Brasil ou neutra com acentos?)
- [ ] Escolher referências visuais
- [ ] Sketch de 2-3 backgrounds principais

### 4. Primeiro Protótipo
- [ ] Modificar 1-2 backgrounds
- [ ] Re-empacotar bundle
- [ ] Testar no FM26
- [ ] Validar pipeline

---

## 🎨 Conceitos Visuais Propostos

### Opção A: "Brasil Elegante"
- Paleta: Verde musgo, dourado sutil, branco
- Inspiração: Arquitetura Niemeyer, formas orgânicas
- Texturas: Gradientes suaves, geometria tropical

### Opção B: "Seleção Moderna"
- Paleta: Azul marinho, amarelo vibrante, branco
- Inspiração: Uniformes modernos CBF
- Texturas: Linhas dinâmicas, energia

### Opção C: "Minimalista Brasileiro"
- Paleta: Cinza claro, verde Brasil (#009c3b), branco
- Inspiração: Design escandinavo + toque BR
- Texturas: Clean, acentos sutis

**Decisão**: Aguardar visualização dos backgrounds atuais

---

## 🛠️ Pipeline de Desenvolvimento

```
1. Extrair textura original
   ↓
2. Modificar no Gimp/Photoshop
   ↓
3. Manter dimensões/formato exatos
   ↓
4. Re-empacotar com UnityPy
   ↓
5. Substituir bundle original
   ↓
6. Testar no FM26
   ↓
7. Iterar até satisfeito
```

**Script Python**: A ser desenvolvido para automatizar re-empacotamento

---

## 📅 Timeline Revisado

- **Hoje (2026-03-10)**: ✅ Análise técnica
- **Amanhã**: Conceito visual + primeiros protótipos
- **2-3 dias**: Desenvolvimento de backgrounds principais
- **1 semana**: Skin v0.1 completa e testada
- **2 semanas**: Refinamento + documentação + release

---

## 🚀 Ação Imediata

**Aguardando**:
1. Extração de backgrounds Darkside finalizar
2. Sua aprovação de qual conceito visual seguir

**Pronto para**:
- Começar design de backgrounds
- Desenvolver script de re-empacotamento
- Criar primeiros protótipos

---

## 💬 Perguntas para Você

1. **Qual conceito visual prefere?** (A/B/C acima, ou outra ideia?)
2. **Quer focar em backgrounds primeiro, ou explorar fontes também?**
3. **Tem referências visuais específicas?** (sites, skins de outros jogos, etc.)

---

_Aguardando sua resposta para prosseguir! 🐺_
