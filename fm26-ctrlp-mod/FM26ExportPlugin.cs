using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using HarmonyLib;

namespace FM26ExportMod
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class FM26ExportPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export Mod v1.0.0");
            Log.LogInfo("========================================");
            
            // Add our MonoBehaviour to handle Update
            AddComponent<UpdateRunner>();
            
            Log.LogInfo("[Init] Plugin carregado!");
        }
    }
    
    // MonoBehaviour for Update loop
    public class UpdateRunner : MonoBehaviour
    {
        private MethodInfo _updateExportMethod = null;
        private Type _carouselType = null;
        
        void Start()
        {
            FM26ExportPlugin.Log.LogInfo("[UpdateRunner] Start!");
            InvokeRepeating(nameof(SearchMethod), 2f, 5f);
        }
        
        void SearchMethod()
        {
            if (_updateExportMethod != null) return;
            
            FM26ExportPlugin.Log.LogInfo("[Search] Procurando UpdateExportCurrentItemBinding...");
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = assembly.GetName().Name;
                    if (name.StartsWith("System") || name.StartsWith("Mono") || name.StartsWith("mscorlib")) continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Carousel") || type.Name.Contains("TableView"))
                        {
                            var method = type.GetMethod("UpdateExportCurrentItemBinding", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (method != null)
                            {
                                FM26ExportPlugin.Log.LogInfo($"[Search] ENCONTRADO: {type.FullName}");
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
            // Ctrl+P
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                FM26ExportPlugin.Log.LogInfo(">>> Ctrl+P PRESSIONADO!");
                TryExport();
            }
            
            // F10
            if (Input.GetKeyDown(KeyCode.F10))
            {
                FM26ExportPlugin.Log.LogInfo(">>> F10 - Debug tipos");
                LogAllTypes();
            }
        }
        
        void TryExport()
        {
            if (_updateExportMethod == null || _carouselType == null)
            {
                FM26ExportPlugin.Log.LogWarning("[Export] Metodo ainda nao encontrado");
                SearchMethod();
                return;
            }
            
            try
            {
                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", Type.EmptyTypes);
                var genericMethod = findMethod.MakeGenericMethod(_carouselType);
                var objects = (UnityEngine.Object[])genericMethod.Invoke(null, null);
                
                FM26ExportPlugin.Log.LogInfo($"[Export] {objects.Length} carousels encontrados");
                
                foreach (var obj in objects)
                {
                    FM26ExportPlugin.Log.LogInfo($"[Export] {obj.name}");
                    _updateExportMethod.Invoke(obj, new object[] { 0 });
                }
            }
            catch (Exception ex)
            {
                FM26ExportPlugin.Log.LogError($"[Export] Erro: {ex.Message}");
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
                        if (type.Name.Contains("Export") || type.Name.Contains("Carousel") || type.Name.Contains("Table"))
                        {
                            FM26ExportPlugin.Log.LogInfo($"[Tipo] {type.FullName}");
                            count++;
                            if (count > 50) return;
                        }
                    }
                }
                catch { }
            }
        }
    }
}
