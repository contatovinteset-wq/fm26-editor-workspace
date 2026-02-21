## FM26 Ctrl+P Export Mod - Status

### O que foi feito

#### Análise (Concluído)
- ✅ Descompilação via IL2CPP e dnSpy
- ✅ Identificado `ExportCurrentItemToBinding` na classe `SICarousel`
- ✅ Função de exportação existe mas está desconectada do input
- ✅ Sistema de shortcuts é gerenciado por `ShortcutData` (singleton)
- ✅ Não há atalho Ctrl+P definido nos keybindings

#### Mod Criado
- ✅ Código-fonte do plugin BepInEx criado
- ✅ Scripts de build (Windows e Linux/Mac)
- ✅ Instruções de instalação

### Arquivos Gerados
```
fm26-ctrlp-mod/
├── ExportModPlugin.cs      # Código do mod
├── FM26ExportMod.csproj    # Projeto .NET
├── build.sh                # Script build Linux/Mac
├── build.bat               # Script build Windows
├── README.md               # Documentação
└── INSTALL.md              # Guia de instalação
```

### Como Funciona
1. O mod usa BepInEx para hook no jogo
2. Cria um GameObject que escuta input de teclado
3. Quando Ctrl+P é pressionado:
   - Busca carousels ativos na cena
   - Usa reflection para chamar `UpdateExportCurrentItemBinding`
   - Executa a exportação do item selecionado

### Próximos Passos para Testar
1. **Instalar BepInEx-IL2CPP** no FM26
2. **Compilar o mod**: rodar `build.bat` (Windows) ou `./build.sh` (Linux/Mac)
3. **Copiar DLL** para `BepInEx/plugins/`
4. **Testar no jogo**

### Notas
- O mod usa reflection para evitar dependências rígidas das DLLs do jogo
- Isso torna o mod mais resistente a atualizações
- Se não funcionar, precisaremos ajustar o método de chamada
