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
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            // Patch SI.Bindable.Bindings.Update
            var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
            if (bindingsType != null)
            {
                var updateMethod = bindingsType.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
                if (updateMethod != null)
                {
                    var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                    Log.LogInfo("[Init] Patched SI.Bindable.Bindings.Update");
                }
            }
        }
        
        private static int _frameCount = 0;
        private static bool _initialized = false;
        private static Il2CppSystem.Type _sicarouselType = null;
        private static MethodInfo _exportMethod = null;
        
        public static void OnUpdate()
        {
            _frameCount++;
            
            // Inicializa depois de 300 frames
            if (!_initialized && _frameCount == 300)
            {
                _initialized = true;
                InitializeTypes();
            }
            
            if (!_initialized) return;
            
            // Ctrl+P - EXPORTA
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                DoExport();
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                // Obtém Il2CppSystem.Type para SICarousel
                _sicarouselType = Il2CppSystem.Type.GetType("SI.Bindable.SICarousel, SI.Bindable");
                
                if (_sicarouselType != null)
                {
                    Debug.Log($"[FM26CtrlP] Tipo Il2Cpp obtido: {_sicarouselType.FullName}");
                    
                    // Encontra o método via managed type
                    var managedType = Type.GetType("SI.Bindable.SICarousel, SI.Bindable");
                    if (managedType != null)
                    {
                        _exportMethod = managedType.GetMethod("UpdateExportCurrentItemBinding",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        if (_exportMethod != null)
                        {
                            Debug.Log($"[FM26CtrlP] Método encontrado: {_exportMethod.Name}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[FM26CtrlP] Falha ao obter Il2CppSystem.Type");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro na inicialização: {ex.Message}");
            }
        }
        
        private static void DoExport()
        {
            if (_sicarouselType == null || _exportMethod == null)
            {
                Debug.LogError("[FM26CtrlP] Tipos não inicializados");
                return;
            }
            
            try
            {
                // Usa Object.FindObjectsOfType com Il2CppSystem.Type
                var objects = UnityEngine.Object.FindObjectsOfType(_sicarouselType);
                
                if (objects != null)
                {
                    var count = objects.Length;
                    Debug.Log($"[FM26CtrlP] {count} objetos SICarousel encontrados");
                    
                    for (int i = 0; i < count; i++)
                    {
                        var obj = objects[i];
                        if (obj != null)
                        {
                            try
                            {
                                _exportMethod.Invoke(obj, new object[] { 0 });
                                Debug.Log($"[FM26CtrlP] Exportado {i + 1}/{count}");
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[FM26CtrlP] Erro ao exportar objeto {i}: {ex.Message}");
                            }
                        }
                    }
                    
                    Debug.Log("[FM26CtrlP] >>> EXPORTAÇÃO CONCLUÍDA!");
                }
                else
                {
                    Debug.LogWarning("[FM26CtrlP] Nenhum objeto encontrado");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro no export: {ex.Message}");
            }
        }
    }
}
