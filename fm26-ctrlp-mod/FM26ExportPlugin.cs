using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;

namespace FM26ExportMod
{
    // Plugin attribute - BepInEx 6 IL2CPP style
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class FM26ExportPlugin
    {
        internal static ManualLogSource Log;
        private static MethodInfo _updateExportMethod = null;
        private static Type _carouselType = null;
        private static GameObject _runnerObject;
        
        public FM26ExportPlugin()
        {
            Log = BepInEx.Logging.Logger.CreateLogSource("FM26CtrlP");
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export Mod v1.0.0 - BepInEx 6 IL2CPP");
            Log.LogInfo("========================================");
            
            // Create a GameObject to run Update loop
            _runnerObject = new GameObject("FM26CtrlPRunner");
            _runnerObject.AddComponent<UpdateRunner>();
            UnityEngine.Object.DontDestroyOnLoad(_runnerObject);
            
            Log.LogInfo("[Init] Plugin iniciado!");
        }
        
        public static void FindExportMethod()
        {
            Log.LogInfo("[Init] Procurando ExportCurrentItemToBinding...");
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = assembly.GetName().Name;
                    if (name.StartsWith("System") || name.StartsWith("Mono")) continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Carousel") || type.Name.Contains("TableView"))
                        {
                            Log.LogInfo($"[Tipo] {type.FullName}");
                            
                            var method = type.GetMethod("UpdateExportCurrentItemBinding", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (method != null)
                            {
                                Log.LogInfo($"[OK] Metodo encontrado em {type.Name}!");
                                _carouselType = type;
                                _updateExportMethod = method;
                            }
                        }
                    }
                }
                catch { }
            }
        }
        
        public static void TryExport()
        {
            Log.LogInfo("[Export] Iniciando...");
            
            try
            {
                if (_updateExportMethod != null && _carouselType != null)
                {
                    // Usa reflexão para chamar FindObjectsOfType<T>() genérico
                    var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", Type.EmptyTypes);
                    var genericMethod = findMethod.MakeGenericMethod(_carouselType);
                    var objects = (UnityEngine.Object[])genericMethod.Invoke(null, null);
                    
                    Log.LogInfo($"[Export] Encontrados {objects.Length} carousels");
                    
                    foreach (var obj in objects)
                    {
                        Log.LogInfo($"[Export] Exportando: {obj.name}");
                        _updateExportMethod.Invoke(obj, new object[] { 0 });
                    }
                }
                else
                {
                    Log.LogWarning("[Export] Metodo nao encontrado ainda. Procurando...");
                    FindExportMethod();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        public static void LogAllTypes()
        {
            int count = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = assembly.GetName().Name;
                    if (name.StartsWith("System") || name.StartsWith("Mono")) continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Export") || 
                            type.Name.Contains("Carousel") || 
                            type.Name.Contains("Table"))
                        {
                            Log.LogInfo($"[Tipo] {type.FullName}");
                            count++;
                            if (count > 30) return;
                        }
                    }
                }
                catch { }
            }
            Log.LogInfo($"[Debug] Total: {count}");
        }
    }
    
    // Helper MonoBehaviour to run Update loop
    public class UpdateRunner : MonoBehaviour
    {
        void Update()
        {
            // Ctrl+P
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl) && 
                UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.P))
            {
                FM26ExportPlugin.Log.LogInfo(">>> Ctrl+P DETECTADO!");
                FM26ExportPlugin.TryExport();
            }
            
            // F10 - debug
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F10))
            {
                FM26ExportPlugin.Log.LogInfo(">>> F10 - Listando tipos...");
                FM26ExportPlugin.LogAllTypes();
            }
        }
    }
}
