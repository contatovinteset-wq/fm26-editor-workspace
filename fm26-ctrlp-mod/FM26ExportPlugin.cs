using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.38.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.38.0 CARREGADO!");
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
        
        public static void OnUpdate()
        {
            try
            {
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[Init] Pronto!");
                }
                
                if (!_initialized || Keyboard.current == null) return;
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Debug: Listar TODOS os VisualElements");
                    DebugAllVisualElements();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar propriedades 'data' em todos elementos");
                    FindDataProperties();
                }
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Tentar exportar via BindingMethod");
                    TryExportViaBinding();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void DebugAllVisualElements()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Debug] {uiDocs.Length} UIDocuments");
                
                var typeCounts = new Dictionary<string, int>();
                var elementsWithData = new List<VisualElement>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.rootVisualElement == null) continue;
                    
                    Log.LogInfo($"[Debug] Doc: {doc.name}");
                    
                    TraverseElement(doc.rootVisualElement, 0, 3, typeCounts, elementsWithData);
                }
                
                // Top 20 tipos
                Log.LogInfo("[Debug] Tipos mais comuns:");
                foreach (var kv in typeCounts.OrderByDescending(x => x.Value).Take(20))
                {
                    Log.LogInfo($"[Debug]   {kv.Key}: {kv.Value}");
                }
                
                // Elementos com dados
                Log.LogInfo($"[Debug] {elementsWithData.Count} elementos com 'SourceData' não-nulo");
                foreach (var el in elementsWithData.Take(10))
                {
                    Log.LogInfo($"[Debug]   {el.name} -> tem dados!");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Debug] Erro: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void TraverseElement(VisualElement element, int depth, int maxDepth, 
            Dictionary<string, int> typeCounts, List<VisualElement> elementsWithData)
        {
            if (element == null || depth > maxDepth) return;
            
            // Contar tipo
            var typeName = element.GetType().Name;
            if (!typeCounts.ContainsKey(typeName)) typeCounts[typeName] = 0;
            typeCounts[typeName]++;
            
            // Verificar SourceData
            try
            {
                var sourceDataProp = element.GetType().GetProperty("SourceData", BindingFlags.Public | BindingFlags.Instance);
                if (sourceDataProp != null)
                {
                    var data = sourceDataProp.GetValue(element);
                    if (data != null)
                    {
                        elementsWithData.Add(element);
                    }
                }
            }
            catch { }
            
            // Filhos
            for (int i = 0; i < element.childCount && i < 100; i++)
            {
                TraverseElement(element[i], depth + 1, maxDepth, typeCounts, elementsWithData);
            }
        }
        
        private static void FindDataProperties()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.rootVisualElement == null) continue;
                    
                    FindDataInTree(doc.rootVisualElement, 0, 5);
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Data] Erro: {ex.Message}");
            }
        }
        
        private static void FindDataInTree(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            var type = element.GetType();
            
            // Buscar propriedades que parecem conter dados
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name.ToLower().Contains("data") || 
                            p.Name.ToLower().Contains("source") ||
                            p.Name.ToLower().Contains("item") ||
                            p.Name.ToLower().Contains("list"))
                .ToList();
            
            foreach (var prop in props)
            {
                try
                {
                    var val = prop.GetValue(element);
                    if (val != null)
                    {
                        var valType = val.GetType();
                        Log.LogInfo($"[Data] {element.name}.{prop.Name}: {valType.Name}");
                        
                        // Se for lista, mostrar count
                        if (val is IList list)
                        {
                            Log.LogInfo($"[Data]   -> IList com {list.Count} itens!");
                            
                            if (list.Count > 0)
                            {
                                var first = list[0];
                                Log.LogInfo($"[Data]   -> Primeiro: {first?.GetType().FullName ?? "null"}");
                            }
                        }
                    }
                }
                catch { }
            }
            
            // Filhos
            for (int i = 0; i < element.childCount && i < 50; i++)
            {
                FindDataInTree(element[i], depth + 1, maxDepth);
            }
        }
        
        private static void TryExportViaBinding()
        {
            try
            {
                // Buscar Bindings.Update para pegar instância capturada
                var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
                if (bindingsType == null)
                {
                    Log.LogWarning("[Export] Bindings type não encontrado");
                    return;
                }
                
                // DataSet property
                var dataSetProp = bindingsType.GetProperty("DataSet", BindingFlags.Public | BindingFlags.Instance);
                if (dataSetProp == null)
                {
                    Log.LogWarning("[Export] DataSet property não encontrada");
                    return;
                }
                
                Log.LogInfo("[Export] Bindings.DataSet encontrado!");
                Log.LogInfo("[Export] Tipo: " + dataSetProp.PropertyType.FullName);
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
    }
}
