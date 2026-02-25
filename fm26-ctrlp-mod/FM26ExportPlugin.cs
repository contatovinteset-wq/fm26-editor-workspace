using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace FM26ExportMod
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class FM26ExportPlugin : BasePlugin
    {        
        public override void Load()
        {
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
            Debug.Log("[FM26CtrlP] UpdateRunner Start!");
            InvokeRepeating(nameof(SearchMethod), 2f, 5f);
        }
        
        void SearchMethod()
        {
            if (_updateExportMethod != null) return;
            
            Debug.Log("[FM26CtrlP] Procurando UpdateExportCurrentItemBinding...");
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = assembly.GetName().Name;
                    if (name.StartsWith("System") || name.StartsWith("Mono") || name.StartsWith("mscorlib") || name.StartsWith("netstandard")) continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        var typeName = type.Name;
                        if (typeName.Contains("Carousel") || typeName.Contains("TableView") || typeName.Contains("Export"))
                        {
                            var method = type.GetMethod("UpdateExportCurrentItemBinding", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (method != null)
                            {
                                Debug.Log($"[FM26CtrlP] ENCONTRADO: {type.FullName}");
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
            // Ctrl+P - usa UnityEngine.Input explicitamente
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl) && 
                UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.P))
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                TryExport();
            }
            
            // F10 - debug
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F10))
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Debug tipos");
                LogAllTypes();
            }
        }
        
        void TryExport()
        {
            if (_updateExportMethod == null || _carouselType == null)
            {
                Debug.LogWarning("[FM26CtrlP] Metodo ainda nao encontrado");
                SearchMethod();
                return;
            }
            
            try
            {
                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", Type.EmptyTypes);
                if (findMethod == null)
                {
                    Debug.LogError("[FM26CtrlP] FindObjectsOfType nao encontrado");
                    return;
                }
                
                var genericMethod = findMethod.MakeGenericMethod(_carouselType);
                var objects = (UnityEngine.Object[])genericMethod.Invoke(null, null);
                
                if (objects == null || objects.Length == 0)
                {
                    Debug.LogWarning("[FM26CtrlP] Nenhum carousel encontrado");
                    return;
                }
                
                Debug.Log($"[FM26CtrlP] {objects.Length} carousels encontrados");
                
                foreach (var obj in objects)
                {
                    Debug.Log($"[FM26CtrlP] Exportando: {obj.name}");
                    _updateExportMethod.Invoke(obj, new object[] { 0 });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro: {ex.Message}\n{ex.StackTrace}");
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
                    if (name.StartsWith("System") || name.StartsWith("Mono") || name.StartsWith("netstandard")) continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        var typeName = type.Name;
                        if (typeName.Contains("Export") || typeName.Contains("Carousel") || typeName.Contains("Table"))
                        {
                            Debug.Log($"[FM26CtrlP] Tipo: {type.FullName}");
                            count++;
                            if (count > 50) return;
                        }
                    }
                }
                catch { }
            }
            Debug.Log($"[FM26CtrlP] Total de tipos: {count}");
        }
    }
}
