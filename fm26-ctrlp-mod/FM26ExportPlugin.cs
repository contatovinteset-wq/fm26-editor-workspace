using System;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            try
            {
                Log.LogInfo("========================================");
                Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
                Log.LogInfo("========================================");
                
                // Aplica Harmony patch para rodar código no Update
                Harmony.CreateAndPatchAll(typeof(Plugin));
                
                Log.LogInfo("[Init] Harmony patches aplicados!");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro ao carregar: {ex.Message}");
                Log.LogError($"[Init] StackTrace: {ex.StackTrace}");
            }
        }
        
        // Patch no Time.time para rodar nosso código periodicamente
        [HarmonyPatch(typeof(Time), "get_time")]
        [HarmonyPostfix]
        public static void OnTimeUpdate(ref float __result)
        {
            try
            {
                CtrlPLogic.Update();
            }
            catch { }
        }
    }
    
    public static class CtrlPLogic
    {
        private static MethodInfo _exportMethod = null;
        private static Type _targetType = null;
        private static float _searchTimer = 0f;
        private static bool _initialized = false;
        private static float _lastTime = 0f;
        
        // Input via reflection
        private static Type _inputType = null;
        private static MethodInfo _getKeyMethod = null;
        private static MethodInfo _getKeyDownMethod = null;
        private static bool _reflectionReady = false;
        
        public static void Update()
        {
            // Inicializa reflection do Input
            if (!_reflectionReady)
            {
                InitInputReflection();
            }
            
            float currentTime = Time.time;
            float deltaTime = currentTime - _lastTime;
            _lastTime = currentTime;
            
            // Busca método periodicamente
            _searchTimer += deltaTime;
            if (!_initialized && _searchTimer > 5f)
            {
                _searchTimer = 0f;
                SearchExportMethod();
            }
            
            // Ctrl+P
            if (GetKey(KeyCode.LeftControl) && GetKeyDown(KeyCode.P))
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                TryExport();
            }
            
            // F10 - Debug
            if (GetKeyDown(KeyCode.F10))
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Listando tipos...");
                ListTypes();
            }
        }
        
        private static void InitInputReflection()
        {
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        _inputType = assembly.GetType("UnityEngine.Input");
                        if (_inputType != null)
                        {
                            _getKeyMethod = _inputType.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
                            _getKeyDownMethod = _inputType.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
                            if (_getKeyMethod != null && _getKeyDownMethod != null)
                            {
                                _reflectionReady = true;
                                Debug.Log("[FM26CtrlP] Input reflection inicializado!");
                                return;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static bool GetKey(KeyCode key)
        {
            if (_getKeyMethod != null)
                return (bool)_getKeyMethod.Invoke(null, new object[] { key });
            return false;
        }
        
        private static bool GetKeyDown(KeyCode key)
        {
            if (_getKeyDownMethod != null)
                return (bool)_getKeyDownMethod.Invoke(null, new object[] { key });
            return false;
        }
        
        private static void SearchExportMethod()
        {
            if (_initialized) return;
            
            try
            {
                Debug.Log("[FM26CtrlP] Procurando UpdateExportCurrentItemBinding...");
                
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = assembly.GetName().Name;
                        if (name.StartsWith("System") || name.StartsWith("Mono") || name.StartsWith("mscorlib"))
                            continue;
                        
                        foreach (var type in assembly.GetTypes())
                        {
                            var method = type.GetMethod("UpdateExportCurrentItemBinding",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (method != null)
                            {
                                _targetType = type;
                                _exportMethod = method;
                                _initialized = true;
                                Debug.Log($"[FM26CtrlP] ENCONTRADO: {type.FullName}");
                                return;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro na busca: {ex.Message}");
            }
        }
        
        private static void TryExport()
        {
            if (_exportMethod == null || _targetType == null)
            {
                Debug.LogWarning("[FM26CtrlP] Método não encontrado ainda");
                SearchExportMethod();
                return;
            }
            
            try
            {
                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", Type.EmptyTypes);
                var generic = findMethod.MakeGenericMethod(_targetType);
                var results = generic.Invoke(null, null) as UnityEngine.Object[];
                
                Debug.Log($"[FM26CtrlP] {results?.Length ?? 0} objetos encontrados");
                
                if (results != null)
                {
                    foreach (var obj in results)
                    {
                        Debug.Log($"[FM26CtrlP] Exportando: {obj.name}");
                        _exportMethod.Invoke(obj, new object[] { 0 });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro no export: {ex.Message}");
            }
        }
        
        private static void ListTypes()
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
                        if (type.Name.Contains("Export") || type.Name.Contains("Carousel") || 
                            type.Name.Contains("Table") || type.Name.Contains("View"))
                        {
                            Debug.Log($"[FM26CtrlP] Tipo: {type.FullName}");
                            count++;
                            if (count > 30) return;
                        }
                    }
                }
                catch { }
            }
        }
    }
}
