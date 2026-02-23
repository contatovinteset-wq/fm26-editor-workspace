## FM26 Ctrl+P Export Mod - Status

### O que foi feito

#### Análise (Concluído)
- ✅ Descompilação via IL2CPP e dnSpy
- ✅ Identificado `ExportCurrentItemToBinding` na classe `SICarousel`
- ✅ Função de exportação existe mas está desconectada do input
- ✅ Sistema de shortcuts é gerenciado por `ShortcutData` (singleton)
- ✅ Não há atalho Ctrl+P definido nos keybindings

#### Mod Criado
- ✅ Código-fonte para **MelonLoader** (BepInEx não suporta FM26)
- ✅ Scripts de build
- ✅ Instruções de instalação

### ⚠️ Problema com BepInEx

O FM26 usa **Unity 6000.0.52f1** com **metadata version 31**.
O BepInEx atual (versão 6 preview) só suporta até metadata version 29.

**Erro:**
```
Unsupported metadata version found! We support 23-29, got 31
```

**Solução:** Usar **MelonLoader** que tem suporte mais recente.

---

### Arquivos Gerados

```
fm26-ctrlp-mod/
├── FM26ExportMod.cs                    # Código do mod (MelonLoader)
├── FM26ExportMod_MelonLoader.csproj    # Projeto .NET para MelonLoader
├── FM26ExportMod.csproj                # Projeto original (BepInEx - não funciona)
├── build.sh                            # Script build Linux/Mac
├── build.bat                           # Script build Windows
├── README.md                           # Documentação
├── INSTALL.md                          # Guia BepInEx (não funciona)
└── INSTALL_MELONLOADER.md              # Guia MelonLoader ✅
```

---

### Como Funciona

1. O mod usa **MelonLoader** para hook no jogo
2. No `OnUpdate()`, detecta quando **Ctrl+P** é pressionado
3. Quando detectado:
   - Usa reflection para encontrar objetos `SICarousel` ativos
   - Chama `UpdateExportCurrentItemBinding(0)` via reflection
   - Executa a exportação do item selecionado

---

### Próximos Passos

1. **Instalar MelonLoader** no FM26
   - Download: https://github.com/LavaGang/MelonLoader/releases
   - Usar o Installer para instalar automaticamente

2. **Compilar o mod**:
   ```bash
   dotnet build FM26ExportMod_MelonLoader.csproj -c Release
   ```

3. **Copiar DLL** para `Mods/FM26ExportMod.dll`

4. **Testar no jogo**:
   - Abrir jogo
   - Verificar console do MelonLoader
   - Selecionar jogador
   - Pressionar Ctrl+P
   - Verificar logs

---

### Notas

- O mod usa reflection para evitar dependências rígidas das DLLs do jogo
- Isso torna o mod mais resistente a atualizações
- Se não funcionar, precisaremos ajustar o método de chamada
- Verificar logs em `MelonLoader/latest.log`
