#!/usr/bin/env dotnet-script
#r "nuget: Mono.Cecil, 0.11.4"

using Mono.Cecil;
using System.IO;

var corePath = "BepInEx/core/BepInEx.Core.dll";
var commonPath = "BepInEx/core/BepInEx.Unity.Common.dll";
var il2cppPath = "BepInEx/core/BepInEx.Unity.IL2CPP.dll";

foreach (var path in new[] { corePath, commonPath, il2cppPath }) {
    if (!File.Exists(path)) {
        Console.WriteLine($"NAO ENCONTRADO: {path}");
        continue;
    }
    
    Console.WriteLine($"\n=== {path} ===");
    var assembly = AssemblyDefinition.ReadAssembly(path);
    
    foreach (var type in assembly.MainModule.Types) {
        if (type.Name == "BaseUnityPlugin" || type.Name.Contains("Plugin")) {
            Console.WriteLine($"  Tipo: {type.FullName}");
            Console.WriteLine($"  Base: {type.BaseType?.FullName ?? "none"}");
        }
    }
}
