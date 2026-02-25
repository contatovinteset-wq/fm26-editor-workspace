using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;

namespace FM26ExportMod
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class Plugin
    {
        internal static ManualLogSource Log;
        
        public Plugin()
        {
            Log = BepInEx.Logging.Logger.CreateLogSource("FM26CtrlP");
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export Mod v1.0.0");
            Log.LogInfo("========================================");
            
            // Apply Harmony patches
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            harmony.PatchAll();
            
            Log.LogInfo("[Harmony] Patches aplicados!");
        }
    }
    
    // Patch into GameObject.AddComponent to hook our UpdateRunner
    [HarmonyPatch(typeof(GameObject), "AddComponent", new Type[] { typeof(Type) })]
    public static class AddComponentPatch
    {
        private static GameObject _runner;
        private static bool _initialized = false;
        
        static void Postfix()
        {
            if (_initialized) return;
            
            try
            {
                _initialized = true;
                _runner = new GameObject("FM26CtrlPRunner");
                _runner.AddComponent<UpdateRunner>();
                UnityEngine.Object.DontDestroyOnLoad(_runner);
                Plugin.Log.LogInfo("[Init] UpdateRunner injetado via Harmony!");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
    }
    
    // MonoBehaviour for Update loop
    public class UpdateRunner : MonoBehaviour
    {
        private MethodInfo _updateExportMethod = null;
        private Type _carouselType = null;
        private float _searchTimer = 0f;
        
        void Awake()
        {
            Plugin.Log.LogInfo("[UpdateRunner] Awake!");
            InvokeRepeating(nameof(SearchMethod), 2f, 5f);
        }
        
        void SearchMethod()
        {
            if (_updateExportMethod != null) return;
            
            Plugin.Log.LogInfo("[Search] Procurando UpdateExportCurrentItemBinding...");
            
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
                                Plugin.Log.LogInfo($"[Search] ENCONTRADO: {type.FullName}");
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
                Plugin.Log.LogInfo(">>> Ctrl+P PRESSIONADO!");
                TryExport();
            }
            
            // F10
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Plugin.Log.LogInfo(">>> F10 - Debug tipos");
                LogAllTypes();
            }
        }
        
        void TryExport()
        {
            if (_updateExportMethod == null || _carouselType == null)
            {
                Plugin.Log.LogWarning("[Export] Metodo ainda nao encontrado");
                SearchMethod();
                return;
            }
            
            try
            {
                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", Type.EmptyTypes);
                var genericMethod = findMethod.MakeGenericMethod(_carouselType);
                var objects = (UnityEngine.Object[])genericMethod.Invoke(null, null);
                
                Plugin.Log.LogInfo($"[Export] {objects.Length} carousels encontrados");
                
                foreach (var obj in objects)
                {
                    Plugin.Log.LogInfo($"[Export] {obj.name}");
                    _updateExportMethod.Invoke(obj, new object[] { 0 });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[Export] Erro: {ex.Message}");
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
                            Plugin.Log.LogInfo($"[Tipo] {type.FullName}");
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
