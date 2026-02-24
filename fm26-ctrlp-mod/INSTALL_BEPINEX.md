# FM26 Ctrl+P Export Mod - BepInEx Version

## Instalação

### 1. Instalar BepInEx
Baixe e instale o BepInEx do Thunderstore:
```
https://thunderstore.io/c/football-manager-26/p/BepInEx/BepInExPack_FootballManager26/
```

### 2. Compilar o Mod
```bash
dotnet build -c Release
```

### 3. Copiar o DLL
```
bin\Release\net6.0\FM26ExportMod.dll
```
Para:
```
E:\Steam\steamapps\common\Football Manager 26\BepInEx\plugins\FM26ExportMod.dll
```

### 4. Rodar o jogo
O console do BepInEx vai abrir automaticamente.

## Teste

- **F10**: Testa se o mod está funcionando (deve aparecer mensagem no console)
- **Ctrl+P**: Tenta exportar

## Estrutura de Pastas

```
Football Manager 26\
├── BepInEx\
│   ├── core\           (DLLs do BepInEx)
│   ├── plugins\        <-- COLOQUE O MOD AQUI
│   │   └── FM26ExportMod.dll
│   └── config\
├── FM26_Data\
│   └── Managed\        (UnityEngine DLLs)
└── Football Manager 26.exe
```
