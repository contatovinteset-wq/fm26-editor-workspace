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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.58.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.58.0");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            try
            {
                var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
                if (bindingsType != null)
                {
                    var updateMethod = bindingsType.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
                    if (updateMethod != null)
                    {
                        var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                        Log.LogInfo("[OK] Hook ativo");
                    }
                }
            }
            catch { }
        }
        
        private static object _bindingsInstance = null;
        private static int _frameCount = 0;
        private static bool _initialized = false;
        
        public static void OnUpdate(object __instance)
        {
            try
            {
                if (_bindingsInstance == null && __instance != null) _bindingsInstance = __instance;
                
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[OK] F9=Analisar PlayerSearchReport, F10=Tipos de elementos, Ctrl+P=Exportar Bindings");
                }
                
                if (!_initialized) return;
                
                try { if (Keyboard.current == null) return; } catch { return; }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Analisar PlayerSearchReport");
                    AnalyzePlayerSearchReport();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Listar tipos de elementos");
                    ListElementTypes();
                }
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar via Bindings");
                    ExportFromBindings();
                }
            }
            catch { }
        }
        
        private static void AnalyzePlayerSearchReport()
        {
            try
            {
                var docs = UnityEngine.Object.FindObjectsOfType<UIDocument>();
                
                foreach (var doc in docs)
                {
                    if (doc.name != "PanelManager") continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Encontrar PlayerSearchReport
                    VisualElement FindElement(VisualElement parent, string name)
                    {
                        if (parent == null) return null;
                        if (parent.name == name) return parent;
                        
                        for (int i = 0; i < parent.childCount; i++)
                        {
                            var found = FindElement(parent.ElementAt(i), name);
                            if (found != null) return found;
                        }
                        return null;
                    }
                    
                    var playerSearchReport = FindElement(root, "PlayerSearchReport");
                    
                    if (playerSearchReport == null)
                    {
                        Log.LogWarning("[Analyze] PlayerSearchReport não encontrado");
                        return;
                    }
                    
                    Log.LogInfo($"[Analyze] PlayerSearchReport encontrado!");
                    Log.LogInfo($"[Analyze] Tipo: {playerSearchReport.GetType().FullName}");
                    Log.LogInfo($"[Analyze] childCount: {playerSearchReport.childCount}");
                    
                    // Listar TODAS as propriedades
                    var type = playerSearchReport.GetType();
                    var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    Log.LogInfo($"[Analyze] {props.Length} propriedades:");
                    
                    foreach (var p in props.Take(30))
                    {
                        try
                        {
                            var val = p.GetValue(playerSearchReport);
                            var valStr = val?.ToString() ?? "null";
                            if (valStr.Length > 50) valStr = valStr.Substring(0, 50) + "...";
                            Log.LogInfo($"[Analyze]   {p.Name}: {valStr}");
                        }
                        catch { }
                    }
                    
                    // Listar TODOS os campos
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    Log.LogInfo($"[Analyze] {fields.Length} campos:");
                    
                    foreach (var f in fields.Take(20))
                    {
                        try
                        {
                            var val = f.GetValue(playerSearchReport);
                            var valStr = val?.ToString() ?? "null";
                            if (valStr.Length > 50) valStr = valStr.Substring(0, 50) + "...";
                            Log.LogInfo($"[Analyze]   {f.Name}: {valStr}");
                        }
                        catch { }
                    }
                    
                    // Explorar filhos recursivamente
                    void ExploreChildren(VisualElement el, int depth)
                    {
                        if (el == null || depth > 10) return;
                        
                        var prefix = new string(' ', depth * 2);
                        var elType = el.GetType().Name;
                        var elName = el.name ?? "(sem nome)";
                        
                        Log.LogInfo($"[Child] {prefix}{elType} ({elName}) children={el.childCount}");
                        
                        // Se tem poucos filhos, mostrar propriedades
                        if (el.childCount > 0 && el.childCount <= 5 && depth < 5)
                        {
                            var elProps = el.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var p in elProps)
                            {
                                if (p.Name == "text" || p.Name == "value" || p.Name == "name" || p.Name.Contains("Data"))
                                {
                                    try
                                    {
                                        var v = p.GetValue(el);
                                        Log.LogInfo($"[Child] {prefix}  {p.Name}: {v}");
                                    }
                                    catch { }
                                }
                            }
                        }
                        
                        for (int i = 0; i < el.childCount && i < 50; i++)
                        {
                            try { ExploreChildren(el.ElementAt(i), depth + 1); } catch { }
                        }
                    }
                    
                    ExploreChildren(playerSearchReport, 0);
                }
            }
            catch (Exception ex) { Log.LogError($"[Analyze] {ex.Message}"); }
        }
        
        private static void ListElementTypes()
        {
            try
            {
                var docs = UnityEngine.Object.FindObjectsOfType<UIDocument>();
                var typeCounts = new Dictionary<string, int>();
                
                foreach (var doc in docs)
                {
                    void CountTypes(VisualElement el, int depth)
                    {
                        if (el == null || depth > 15) return;
                        
                        var typeName = el.GetType().Name;
                        if (!typeCounts.ContainsKey(typeName)) typeCounts[typeName] = 0;
                        typeCounts[typeName]++;
                        
                        for (int i = 0; i < el.childCount && i < 100; i++)
                        {
                            try { CountTypes(el.ElementAt(i), depth + 1); } catch { }
                        }
                    }
                    
                    CountTypes(doc.rootVisualElement, 0);
                }
                
                Log.LogInfo("[Types] Tipos de elementos:");
                foreach (var kvp in typeCounts.OrderByDescending(x => x.Value).Take(30))
                {
                    Log.LogInfo($"[Types]   {kvp.Key}: {kvp.Value}");
                }
            }
            catch (Exception ex) { Log.LogError($"[Types] {ex.Message}"); }
        }
        
        private static void ExportFromBindings()
        {
            try
            {
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Export] Bindings não capturado");
                    return;
                }
                
                var type = _bindingsInstance.GetType();
                var mDataProp = type.GetProperty("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                var mDataField = type.GetField("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                
                object mData = null;
                if (mDataProp != null) mData = mDataProp.GetValue(_bindingsInstance);
                else if (mDataField != null) mData = mDataField.GetValue(_bindingsInstance);
                
                if (mData == null) { Log.LogWarning("[Export] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) { Log.LogWarning("[Export] Sem Count/Indexer"); return; }
                
                int total = (int)countProp.GetValue(mData);
                Log.LogInfo($"[Export] {total} itens no Bindings");
                
                // Buscar itens com "interest" (indica que estão sendo usados na tela atual)
                var activeItems = new List<int>();
                
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var interestProp = item.GetType().GetProperty("interest", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (interestProp == null) continue;
                        
                        var interest = interestProp.GetValue(item);
                        if (interest != null)
                        {
                            // interest é uma lista - se tem itens, está ativo
                            var interestType = interest.GetType();
                            var countMethod = interestType.GetProperty("Count");
                            if (countMethod != null)
                            {
                                int count = (int)countMethod.GetValue(interest);
                                if (count > 0) activeItems.Add(i);
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {activeItems.Count} itens com interest (ativos na tela)");
                
                // Mostrar primeiros 10 itens ativos
                foreach (var idx in activeItems.Take(10))
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { idx });
                        var mValueProp = item.GetType().GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue == null) continue;
                        
                        // AsString
                        var asStringMethod = mValue.GetType().GetMethod("AsString");
                        if (asStringMethod != null)
                        {
                            var str = asStringMethod.Invoke(mValue, null)?.ToString() ?? "";
                            Log.LogInfo($"[Export]   [{idx}]: {str}");
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
    }
}
