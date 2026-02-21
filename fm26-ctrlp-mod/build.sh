#!/bin/bash
# Build script for FM26 Ctrl+P Export Mod

echo "Building FM26 Export Mod..."

cd "$(dirname "$0")"

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: dotnet SDK not found. Please install .NET 6.0 SDK"
    exit 1
fi

# Restore packages
echo "Restoring packages..."
dotnet restore

# Build
echo "Building..."
dotnet build -c Release

# Copy output
echo "Copying output..."
mkdir -p output
cp bin/Release/net6.0/FM26ExportMod.dll output/

echo "Build complete! Output: output/FM26ExportMod.dll"
