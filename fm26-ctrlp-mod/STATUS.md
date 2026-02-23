## FM26 Ctrl+P Export Mod - Status

### ✅ Concluído

#### Análise
- ✅ Descompilação via IL2CPP e dnSpy
- ✅ Identificado `ExportCurrentItemToBinding` na classe `SICarousel`
- ✅ Função de exportação existe mas está desconectada do input

#### Mod
- ✅ Código-fonte convertido para **MelonLoader**
- ✅ Scripts de build atualizados
- ✅ Documentação atualizada

---

### ⚠️ Problema com BepInEx (Resolvido)

O FM26 usa **Unity 6000.0.52f1** com **metadata version 31**.

**BepInEx não funciona:**
```
Unsupported metadata version found! We support 23-29, got 31
```

**Solução:** Usar **MelonLoader** ✅

---

### Arquivos do Mod

```
fm26-ctrlp-mod/
├── FM26ExportMod.cs      # Código (MelonLoader)
├── FM26ExportMod.csproj  # Projeto .NET 6.0
├── build.bat             # Script Windows
├── build.sh              # Script Linux/Mac
├── README.md             # Documentação
├── INSTALL.md            # Guia de instalação
└── STATUS.md             # Este arquivo
```

---

### Próximos Passos

1. [ ] Compilar o mod
2. [ ] Copiar para `Mods/FM26ExportMod.dll`
3. [ ] Testar no jogo
4. [ ] Ajustar código se necessário

---

### Como Testar

1. Instalar MelonLoader ✅ (já feito pelo usuário)
2. Compilar: `build.bat` ou `dotnet build -c Release`
3. Copiar DLL para `Mods/`
4. Abrir jogo
5. Selecionar jogador
6. Pressionar Ctrl+P
7. Verificar console/log
