#!/bin/bash

echo "========================================"
echo "FM26 Ctrl+P Export Mod - Build Script"
echo "========================================"
echo

echo "[1/3] Restaurando dependencias..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "ERRO: Falha ao restaurar dependencias"
    exit 1
fi

echo
echo "[2/3] Compilando mod (Release)..."
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "ERRO: Falha ao compilar"
    exit 1
fi

echo
echo "[3/3] Build concluido!"
echo
echo "========================================"
echo "DLL gerado em:"
echo "bin/Release/net6.0/FM26ExportMod.dll"
echo
echo "Copie para:"
echo "[FM26]/Mods/FM26ExportMod.dll"
echo "========================================"
