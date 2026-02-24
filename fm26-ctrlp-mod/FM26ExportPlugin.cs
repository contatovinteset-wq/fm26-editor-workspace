using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FM26ExportMod
{
    [BepInPlugin("com.koda.fm26.export", "FM26 Ctrl+P Export Mod", "1.0.0")]
    public class FM26ExportPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        
        void Awake()
        {
            Log = Logger;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export Mod loaded!");
            Log.LogInfo("========================================");
            
            // Aplica patches Harmony
            var harmony = new Harmony("com.koda.fm26.export");
            harmony.PatchAll();
            
            Log.LogInfo("[Harmony] Patches applied!");
        }
        
        void Update()
        {
            // Detecta Ctrl+P
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                Log.LogInfo(">>> Ctrl+P DETECTADO!");
                TryExportCurrentView();
            }
            
            // Debug: F10 para testar
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Log.LogInfo(">>> F10 PRESSIONADO - MOD FUNCIONANDO!");
                LogAllTypes();
            }
        }
        
        void TryExportCurrentView()
        {
            Log.LogInfo("[Export] Procurando tabelas/datasources...");
            
            try
            {
                // Procura todos os tipos relacionados a dados/tabelas
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            var name = type.Name.ToLower();
                            if (name.Contains("table") || 
                                name.Contains("grid") || 
                                name.Contains("datasource") ||
                                name.Contains("export") ||
                                name.Contains("playerlist") ||
                                name.Contains("squad"))
                            {
                                Log.LogInfo($"[Tipo] {type.FullName}");
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        void LogAllTypes()
        {
            Log.LogInfo("[Debug] Listando tipos com 'Export' ou 'Table'...");
            
            int count = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Export") || type.Name.Contains("Table"))
                        {
                            Log.LogInfo($"[Tipo] {type.FullName}");
                            count++;
                            if (count > 50) return;
                        }
                    }
                }
                catch { }
            }
            
            Log.LogInfo($"[Debug] Total encontrados: {count}");
        }
    }
}
