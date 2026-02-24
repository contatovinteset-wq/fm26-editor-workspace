using System;
using System.Reflection;
using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using UnityEngine;

namespace FM26ExportMod
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class FM26ExportPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private MethodInfo _updateExportMethod = null;
        private Type _carouselType = null;
        
        void Awake()
        {
            Log = Logger;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export Mod v1.0.0");
            Log.LogInfo("========================================");
            
            FindExportMethod();
        }
        
        void FindExportMethod()
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
                                Log.LogInfo($"[OK] Metodo encontrado!");
                                _carouselType = type;
                                _updateExportMethod = method;
                            }
                        }
                    }
                }
                catch { }
            }
        }
        
        void Update()
        {
            // Ctrl+P - usando KeyCode diretamente para evitar conflito
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl) && 
                UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.P))
            {
                Log.LogInfo(">>> Ctrl+P DETECTADO!");
                TryExport();
            }
            
            // F10 - debug
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F10))
            {
                Log.LogInfo(">>> F10 - Listando tipos...");
                LogAllTypes();
            }
        }
        
        void TryExport()
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
                    Log.LogWarning("[Export] Metodo nao encontrado");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        void LogAllTypes()
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
}
