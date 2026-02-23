# Como Compilar o Mod no Windows

## O problema
O NuGet não está encontrando o MelonLoader. Vamos usar as DLLs já instaladas no jogo.

---

## Passo 1: Baixar o Código

1. Acesse: https://github.com/contatovinteset-wq/fm26-editor-workspace
2. Clique em **Code** → **Download ZIP**
3. Extraia em uma pasta qualquer (ex: `C:\fm26-mod\`)

---

## Passo 2: Editar o .csproj

Abra o arquivo `fm26-ctrlp-mod\FM26ExportMod.csproj` no Bloco de Notas e **ajuste os caminhos** para a sua instalação do FM26:

```xml
<ItemGroup>
  <!-- MelonLoader -->
  <Reference Include="MelonLoader">
    <HintPath>C:\Program Files (x86)\Steam\steamapps\common\Football Manager 26\MelonLoader\MelonLoader.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <!-- UnityEngine -->
  <Reference Include="UnityEngine.CoreModule">
    <HintPath>C:\Program Files (x86)\Steam\steamapps\common\Football Manager 26\FM26_Data\Managed\UnityEngine.CoreModule.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

**Substitua** `C:\Program Files (x86)\Steam\steamapps\common\Football Manager 26\` pelo caminho onde está o seu FM26.

---

## Passo 3: Compilar

Abra o Prompt de Comando e execute:

```bash
cd C:\fm26-mod\fm26-editor-workspace\fm26-ctrlp-mod
dotnet build -c Release
```

---

## Passo 4: Instalar

Copie o DLL gerado:
```
De: bin\Release\net6.0\FM26ExportMod.dll
Para: [FM26]\Mods\FM26ExportMod.dll
```

---

## Alternativa: DLL Pré-Compilada

Se não conseguir compilar, me avise que eu gero o DLL aqui de outra forma e te envio.

---

## Verificar Caminhos

Para encontrar os caminhos corretos:

1. **MelonLoader.dll** está em:
   ```
   [FM26]\MelonLoader\MelonLoader.dll
   ```

2. **UnityEngine.CoreModule.dll** está em:
   ```
   [FM26]\FM26_Data\Managed\UnityEngine.CoreModule.dll
   ```

Se esses arquivos existirem, o build vai funcionar!
