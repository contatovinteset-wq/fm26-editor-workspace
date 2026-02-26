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
                LogUIDocuments();
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
                    Log.LogWarning("[Export] Nenhum carousel exportado");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
                Log.LogError($"[Export] Stack: {ex.StackTrace}");
            }
        }
        
        private static List<object> FindVisualElementsOfType(VisualElement root, Type targetType)
        {
            var result = new List<object>();
            
            try
            {
                // Verificar se o root é do tipo
                var rootType = root.GetType();
                if (rootType == targetType || rootType.IsSubclassOf(targetType))
                {
                    result.Add(root);
                }
                
                // Iterar sobre filhos usando GetEnumerator manualmente
                var children = root.Children();
                var enumerator = children.GetEnumerator();
                
                while (enumerator.MoveNext())
                {
                    var child = enumerator.Current;
                    if (child == null) continue;
                    
                    var childType = child.GetType();
                    if (childType == targetType || childType.IsSubclassOf(targetType))
                    {
                        result.Add(child);
                    }
                    
                    // Recursivamente buscar nos filhos
                    result.AddRange(FindVisualElementsOfType(child, targetType));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro ao buscar elementos: {ex.Message}");
            }
            
            return result;
        }
        
        private static void LogUIDocuments()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Debug.Log($"[FM26CtrlP] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    Debug.Log($"[FM26CtrlP] - {doc.name}");
                    
                    var root = doc.rootVisualElement;
                    if (root != null)
                    {
                        LogVisualElementTree(root, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Log erro: {ex.Message}");
            }
        }
        
        private static void LogVisualElementTree(VisualElement element, int depth)
        {
            if (element == null || depth > 5) return;
            
            var indent = new string(' ', depth * 2);
            var type = element.GetType();
            
            if (type.Name.Contains("Carousel") || type.Name.Contains("Table") || type.Name.Contains("List"))
            {
                Debug.Log($"[FM26CtrlP] {indent}{type.Name} ({element.name})");
            }
            
            var children = element.Children();
            var enumerator = children.GetEnumerator();
            
            while (enumerator.MoveNext())
            {
                LogVisualElementTree(enumerator.Current, depth + 1);
            }
        }
    }
}
