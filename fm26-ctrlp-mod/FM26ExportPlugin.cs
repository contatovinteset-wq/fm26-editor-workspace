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
        private static Type _streamedObjectListType = null;
        private static Type _baseVerticalCollectionViewType = null;
        
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
                Debug.Log("[FM26CtrlP] >>> F10 - Debug COMPLETO");
                LogAllVisualElements();
            }
            
            if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F11 - Lista todos os tipos");
                ListAllTypes();
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                _sicarouselType = Type.GetType("SI.Bindable.SICarousel, SI.Bindable");
                _streamedObjectListType = Type.GetType("SI.Bindable.StreamedObjectList, SI.Bindable");
                _baseVerticalCollectionViewType = Type.GetType("UnityEngine.UIElements.BaseVerticalCollectionView, UnityEngine.UIElementsModule");
                
                if (_sicarouselType != null)
                {
                    Debug.Log($"[FM26CtrlP] SICarousel: {_sicarouselType.FullName}");
                    _exportMethod = _sicarouselType.GetMethod("UpdateExportCurrentItemBinding",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (_exportMethod != null)
                        Debug.Log($"[FM26CtrlP] Método UpdateExportCurrentItemBinding encontrado");
                }
                
                if (_streamedObjectListType != null)
                    Debug.Log($"[FM26CtrlP] StreamedObjectList: {_streamedObjectListType.FullName}");
                
                if (_baseVerticalCollectionViewType != null)
                    Debug.Log($"[FM26CtrlP] BaseVerticalCollectionView: {_baseVerticalCollectionViewType.FullName}");
                
                Log.LogInfo("[Init] Tipos carregados");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Init erro: {ex.Message}");
            }
        }
        
        private static void DoExport()
        {
            try
            {
                Log.LogInfo("[Export] Buscando VisualElements...");
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Export] {uiDocs.Length} UIDocuments");
                
                int exported = 0;
                var foundTypes = new HashSet<string>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    Debug.Log($"[FM26CtrlP] UIDocument: {doc.name}");
                    
                    // Buscar TODOS os VisualElements e verificar tipo
                    FindAndExportElements(root, foundTypes, ref exported);
                }
                
                Log.LogInfo($"[Export] Tipos encontrados: {string.Join(", ", foundTypes)}");
                Log.LogInfo($"[Export] Total exportados: {exported}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void FindAndExportElements(VisualElement element, HashSet<string> foundTypes, ref int exported)
        {
            if (element == null) return;
            
            var type = element.GetType();
            var typeName = type.Name;
            
            // Registrar tipos de lista/tabela/carousel
            if (typeName.Contains("Carousel") || typeName.Contains("List") || 
                typeName.Contains("Table") || typeName.Contains("View") ||
                typeName.Contains("Collection"))
            {
                foundTypes.Add(typeName);
                Debug.Log($"[FM26CtrlP] Encontrado: {typeName} ({element.name})");
                
                // Verificar se tem método de export
                if (_exportMethod != null && (type == _sicarouselType || type.IsSubclassOf(_sicarouselType)))
                {
                    try
                    {
                        _exportMethod.Invoke(element, new object[] { 0 });
                        exported++;
                        Debug.Log($"[FM26CtrlP] EXPORTADO: {typeName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FM26CtrlP] Erro ao exportar: {ex.Message}");
                    }
                }
            }
            
            // Recursão nos filhos
            int childCount = element.childCount;
            for (int i = 0; i < childCount; i++)
            {
                FindAndExportElements(element[i], foundTypes, ref exported);
            }
        }
        
        private static void LogAllVisualElements()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Debug.Log($"[FM26CtrlP] === {uiDocs.Length} UIDocuments ===");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    Debug.Log($"[FM26CtrlP] \n=== UIDocument: {doc.name} ===");
                    
                    var root = doc.rootVisualElement;
                    if (root != null)
                    {
                        LogElementTree(root, 0, 8);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Log erro: {ex.Message}");
            }
        }
        
        private static void LogElementTree(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            var indent = new string(' ', depth * 2);
            var type = element.GetType();
            var typeName = type.Name;
            
            // Logar TODOS os elementos
            Debug.Log($"[FM26CtrlP] {indent}{typeName} ({element.name}) childCount={element.childCount}");
            
            int childCount = element.childCount;
            for (int i = 0; i < childCount; i++)
            {
                LogElementTree(element[i], depth + 1, maxDepth);
            }
        }
        
        private static void ListAllTypes()
        {
            try
            {
                var types = new HashSet<string>();
                
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = assembly.GetName().Name;
                        if (!name.StartsWith("SI.") && !name.StartsWith("FM.") && 
                            !name.Contains("UIElements") && !name.Contains("Unity")) continue;
                        
                        foreach (var type in assembly.GetTypes())
                        {
                            var tName = type.Name;
                            if (tName.Contains("List") || tName.Contains("Table") || 
                                tName.Contains("View") || tName.Contains("Carousel") ||
                                tName.Contains("Export") || tName.Contains("Collection"))
                            {
                                types.Add($"{type.FullName}");
                            }
                        }
                    }
                    catch { }
                }
                
                foreach (var t in types)
                {
                    Debug.Log($"[FM26CtrlP] TYPE: {t}");
                }
                
                Debug.Log($"[FM26CtrlP] Total: {types.Count} tipos");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] ListAllTypes erro: {ex.Message}");
            }
        }
    }
}
