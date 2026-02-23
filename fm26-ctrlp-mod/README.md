# FM26 Ctrl+P Export Mod

## Objetivo
Reativar a funcionalidade de exportação (Ctrl+P) no Football Manager 2026.

## Compatibilidade
- ✅ Football Manager 2026 (Unity 6)
- ✅ MelonLoader 0.6.x ou superior

## Como funciona
O mod usa MelonLoader para hook no sistema de input do Unity e detecta quando Ctrl+P é pressionado.
Quando detectado, chama a função `UpdateExportCurrentItemBinding` no carousel/tabela ativo.

## Instalação

### 1. Instalar MelonLoader
```
https://github.com/LavaGang/MelonLoader/releases
```
Baixe o `MelonLoader.Installer.exe` e instale no FM26.

### 2. Compilar o Mod
```bash
# Windows
build.bat

# Linux/Mac
./build.sh
```

Ou manualmente:
```bash
dotnet restore
dotnet build -c Release
```

### 3. Instalar o Mod
Copie `bin/Release/net6.0/FM26ExportMod.dll` para:
```
[Fm26]/Mods/FM26ExportMod.dll
```

### 4. Testar
1. Abra o jogo
2. Selecione um jogador ou tabela
3. Pressione **Ctrl+P**
4. Verifique o console do MelonLoader

## Status
- [x] Análise do código descompilado
- [x] Identificação da função `ExportCurrentItemToBinding`
- [x] Mod convertido para MelonLoader
- [ ] Teste no jogo

## Referências
- `SICarousel` classe (SI.Bindable namespace)
- `UpdateExportCurrentItemBinding(int index)` - método que exporta o item atual
- `ExportCurrentItemToBinding` - BindingPath para o destino da exportação

## Logs
Os logs ficam em: `[FM26]/MelonLoader/latest.log`

## Troubleshooting
- Se o mod não carregar, verifique se o DLL está na pasta `Mods/`
- Se Ctrl+P não funcionar, verifique o console para mensagens de erro
- O mod precisa de um carousel ativo (jogador/tabela selecionado)
