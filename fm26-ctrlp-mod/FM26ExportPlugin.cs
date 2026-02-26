using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
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
        private static MethodInfo _exportMethod = null;
        private static Type _sicarouselType = null;
        private static Type _actionDispatcherType = null;
        private static MethodInfo _performActionMethod = null;
        
        public static void OnUpdate()
        {
            _frameCount++;
            
            if (!_initialized && _frameCount == 300)
            {
                _initialized = true;
                InitializeTypes();
            }
            
            if (!_initialized) return;
            if (Keyboard.current == null) return;
            
            bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
            bool p = Keyboard.current.pKey.wasPressedThisFrame;
            
            if (ctrl && p)
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                Log.LogInfo(">>> Ctrl+P PRESSIONADO!");
                DoExport();
            }
            
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Debug");
                TryActionDispatcher();
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                _sicarouselType = Type.GetType("SI.Bindable.SICarousel, SI.Bindable");
                
                if (_sicarouselType != null)
                {
                    Debug.Log($"[FM26CtrlP] Tipo: {_sicarouselType.FullName}");
                    Log.LogInfo($"[Init] Tipo: {_sicarouselType.FullName}");
                    
                    _exportMethod = _sicarouselType.GetMethod("UpdateExportCurrentItemBinding",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (_exportMethod != null)
                    {
                        Debug.Log($"[FM26CtrlP] Método: {_exportMethod.Name}");
                        Log.LogInfo($"[Init] Método: {_exportMethod.Name}");
                    }
                }
                
                // Buscar ActionDispatcher
                _actionDispatcherType = Type.GetType("FM.ActionSystem.ActionDispatcher, FM.ActionSystem");
                if (_actionDispatcherType != null)
                {
                    Log.LogInfo($"[Init] ActionDispatcher encontrado");
                    _performActionMethod = _actionDispatcherType.GetMethod("PerformAction",
                        BindingFlags.Public | BindingFlags.Static);
                    if (_performActionMethod != null)
                    {
                        Log.LogInfo($"[Init] PerformAction encontrado");
                    }
                }
                else
                {
                    Log.LogWarning("[Init] ActionDispatcher não encontrado");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Init erro: {ex.Message}");
            }
        }
        
        private static void DoExport()
        {
            if (_exportMethod == null || _sicarouselType == null)
            {
                Log.LogError("[Export] Tipos não inicializados");
                TryActionDispatcher();
                return;
            }
            
            try
            {
                Log.LogInfo("[Export] Buscando UIDocuments...");
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Export] {uiDocs.Length} UIDocuments encontrados");
                
                int exported = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    Debug.Log($"[FM26CtrlP] UIDocument: {doc.name}");
                    Log.LogInfo($"[Export] UIDocument: {doc.name}");
                    
                    var carousels = FindVisualElementsOfType(root, _sicarouselType);
                    Log.LogInfo($"[Export] {carousels.Count} carousels em {doc.name}");
                    
                    foreach (var carousel in carousels)
                    {
                        try
                        {
                            Debug.Log($"[FM26CtrlP] Carousel encontrado, exportando...");
                            _exportMethod.Invoke(carousel, new object[] { 0 });
                            exported++;
                            Debug.Log($"[FM26CtrlP] Exportado!");
                            Log.LogInfo($"[Export] Exportado!");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[FM26CtrlP] Erro ao exportar: {ex.Message}");
                        }
                    }
                }
                
                Log.LogInfo($"[Export] Total exportados: {exported}");
                
                if (exported == 0)
                {
                    Log.LogWarning("[Export] Nenhum carousel encontrado. Tentando ActionDispatcher...");
                    TryActionDispatcher();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
                Log.LogError($"[Export] Stack: {ex.StackTrace}");
            }
        }
        
        private static void TryActionDispatcher()
        {
            // TeamExport = 1719039301
            const uint teamExportActionId = 1719039301u;
            
            try
            {
                Log.LogInfo("[Action] Tentando executar TeamExport via ActionDispatcher...");
                
                if (_actionDispatcherType == null)
                {
                    _actionDispatcherType = Type.GetType("FM.ActionSystem.ActionDispatcher, FM.ActionSystem");
                    if (_actionDispatcherType == null)
                    {
                        Log.LogError("[Action] ActionDispatcher não encontrado");
                        return;
                    }
                    
                    _performActionMethod = _actionDispatcherType.GetMethod("PerformAction",
                        BindingFlags.Public | BindingFlags.Static);
                }
                
                if (_performActionMethod == null)
                {
                    Log.LogError("[Action] PerformAction não encontrado");
                    return;
                }
                
                // Criar TypedValue vazio
                var typedValueType = Type.GetType("SI.TypedValue, SI");
                object typedValue = null;
                if (typedValueType != null)
                {
                    typedValue = Activator.CreateInstance(typedValueType);
                }
                
                // Parâmetros: ActionID actionID, uint eventID, InteropReference action, TypedValue data, bool checkForConfirmation
                // TeamExport não tem eventID específico, vamos usar 0
                Log.LogInfo($"[Action] Chamando PerformAction com ID {teamExportActionId}");
                
                _performActionMethod.Invoke(null, new object[] { teamExportActionId, 0u, null, typedValue, false });
                
                Log.LogInfo("[Action] TeamExport executado!");
                Debug.Log("[FM26CtrlP] >>> TeamExport executado via ActionDispatcher!");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Action] Erro: {ex.Message}");
                Log.LogError($"[Action] Stack: {ex.StackTrace}");
            }
        }
        
        private static List<object> FindVisualElementsOfType(VisualElement root, Type targetType)
        {
            var result = new List<object>();
            FindVisualElementsRecursive(root, targetType, result);
            return result;
        }
        
        private static void FindVisualElementsRecursive(VisualElement element, Type targetType, List<object> result)
        {
            if (element == null) return;
            
            try
            {
                var elementType = element.GetType();
                if (elementType == targetType || elementType.IsSubclassOf(targetType))
                {
                    result.Add(element);
                }
                
                int childCount = element.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = element[i];
                    if (child != null)
                    {
                        FindVisualElementsRecursive(child, targetType, result);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro ao buscar elementos: {ex.Message}");
            }
        }
    }
}
