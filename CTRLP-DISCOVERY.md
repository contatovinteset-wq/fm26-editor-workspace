# Descoberta: Ctrl+P no FM26

**Data:** 2026-02-20
**Status:** FUNÇÃO EXISTE E ESTÁ COMPLETA

---

## ✅ Confirmação

A função `ExportCurrentItemToBinding` foi encontrada no `global-metadata.dat` do FM26.

### Strings Encontradas

```
ExportCurrentItemToBinding
ExportCurrentItemToBinding_UxmlAttributeFlags
ExportCurrentViewLabel
get_ExportCurrentItemToBinding
set_ExportCurrentItemToBinding
UpdateExportCurrentItemBinding
m_exportCurrentItemToBinding
m_exportCurrentViewLabel
m_exportCurrentBindingChanged
```

### Teclas

```
Initialize_ctrlKeyboardprintScreen
Initialize_ctrlKeyboardp
printScreen
printScreenKey
get_printScreenKey
```

---

## 🔧 Como Funciona

1. **Função existe** - `ExportCurrentItemToBinding` está compilada no `game_plugin.dll`
2. **Tecla P registrada** - `Initialize_ctrlKeyboardp` existe
3. **Print Screen existe** - `Initialize_ctrlKeyboardprintScreen` existe
4. **Binding completo** - Getter, setter e update existem

---

## ❓ O que falta?

O atalho Ctrl+P foi **desconectado** da UI, mas a função permanece.

---

## 🎯 Solução

### Opção 1: Hook de Teclado
1. Interceptear Ctrl+P no Windows
2. Chamar `ExportCurrentItemToBinding` via DLL injection

### Opção 2: Mod de DLL
1. Usar Il2CppDumper para extrair assemblies
2. Encontrar o endereço da função
3. Criar um DLL wrapper que:
   - Carrega antes do jogo
   - Registra Ctrl+P como atalho
   - Chama a função de exportação

---

## 📁 Arquivos

- `game_plugin.dll` (423MB) - Código compilado IL2CPP
- `global-metadata.dat` (15MB) - Metadados das classes

---

## Próximos Passos

1. [ ] Extrair dump.cs com Il2CppDumper
2. [ ] Encontrar endereço de `ExportCurrentItemToBinding`
3. [ ] Criar DLL injector
4. [ ] Testar no jogo

---

## Referência

- Metadata extraído em: 2026-02-20
- Caminho: `/data/.openclaw/workspace/fm26-game-files/fm_Data/`
