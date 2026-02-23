# FM26 Ctrl+P Export Mod - Instalação

## Requisitos
- Football Manager 2026
- MelonLoader 0.6.x ou superior
- .NET 6.0 SDK (para compilar)

---

## Passo 1: Instalar MelonLoader

1. Baixe o instalador:
   ```
   https://github.com/LavaGang/MelonLoader/releases/latest
   ```
   Procure por `MelonLoader.Installer.exe`

2. Execute o instalador
3. Selecione o executável do FM26 (`FM26.exe`)
4. Clique em **Install**

5. Estrutura após instalação:
   ```
   Football Manager 26/
   ├── FM26.exe
   ├── version.dll
   ├── MelonLoader/
   ├── Mods/
   └── UserLibs/
   ```

---

## Passo 2: Compilar o Mod

### Opção A: Script de Build
```bash
# Windows
build.bat

# Linux/Mac
chmod +x build.sh
./build.sh
```

### Opção B: Manual
```bash
dotnet restore
dotnet build -c Release
```

O DLL será gerado em: `bin/Release/net6.0/FM26ExportMod.dll`

---

## Passo 3: Instalar o Mod

Copie o DLL compilado para a pasta `Mods/` do FM26:
```
De: bin/Release/net6.0/FM26ExportMod.dll
Para: [FM26]/Mods/FM26ExportMod.dll
```

---

## Passo 4: Testar

1. Abra o jogo
2. O console do MelonLoader deve aparecer
3. Procure pela mensagem:
   ```
   [FM26ExportMod] ========================================
   [FM26ExportMod] FM26 Ctrl+P Export Mod
   [FM26ExportMod] Versão: 1.0.0 (MelonLoader)
   ```

4. No jogo, selecione um jogador ou tabela
5. Pressione **Ctrl+P**
6. Verifique o console para mensagens de sucesso/erro

---

## Troubleshooting

### "MelonLoader não abre"
- Instale o Visual C++ Redistributable:
  ```
  https://aka.ms/vs/17/release/vc_redist.x64.exe
  ```

### "Mod não carrega"
- Verifique se `FM26ExportMod.dll` está na pasta `Mods/`
- Verifique o arquivo `MelonLoader/latest.log`

### "Ctrl+P não funciona"
- Verifique o console do MelonLoader para erros
- O mod precisa de um carousel ativo (jogador selecionado)
- Pode ser necessário ajustar o código para a versão específica do FM26

---

## Logs

Arquivo de log: `[FM26]/MelonLoader/latest.log`

---

## Desinstalar

1. Delete `FM26ExportMod.dll` da pasta `Mods/`
2. Para remover o MelonLoader, use o instalador e clique em **Uninstall**
