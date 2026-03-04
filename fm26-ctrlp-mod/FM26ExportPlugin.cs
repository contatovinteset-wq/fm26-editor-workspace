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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.25.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.25.0 CARREGADO!");
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
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar");
                    FindAndExportTable();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Investigar FILHOS do playertable (L2)");
                    InvestigatePlayerTableChildren();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar IList em qualquer elemento");
                    FindAllLists();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigatePlayerTableChildren()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Buscar playertable (L2) com 4 filhos
                    var playerTables = new List<VisualElement>();
                    FindElementsWithPattern(root, playerTables, 0, 50, 
                        e => e.name == "playertable" && e.childCount >= 3);
                    
                    Log.LogInfo($"[Child] Encontrados {playerTables.Count} elementos 'playertable'");
                    
                    foreach (var pt in playerTables)
                    {
                        Log.LogInfo($"[Child] === playertable ({pt.childCount} filhos) ===");
                        
                        // Investigar cada filho
                        for (int i = 0; i < pt.childCount; i++)
                        {
                            try
                            {
                                var child = pt[i];
                                Log.LogInfo($"[Child]   [{i}] {child.name} ({child.GetType().Name}) [{child.childCount}]");
                                
                                // Verificar propriedades importantes
                                SafeCheckProperties(child, "    ");
                                
                                // Se tem filhos, mostrar netos
                                if (child.childCount > 0 && child.childCount < 20)
                                {
                                    for (int j = 0; j < child.childCount; j++)
                                    {
                                        try
                                        {
                                            var grandchild = child[j];
                                            Log.LogInfo($"[Child]      [{j}] {grandchild.name} ({grandchild.GetType().Name}) [{grandchild.childCount}]");
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.LogError($"[Child]   Erro no filho {i}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Child] Erro: {ex.Message}");
            }
        }
        
        private static void SafeCheckProperties(VisualElement element, string indent)
        {
            try
            {
                var type = element.GetType();
                
                // Apenas algumas propriedades seguras
                var safeProps = new[] { "dataSource", "userData", "name", "classList" };
                
                foreach (var propName in safeProps)
                {
                    try
                    {
                        var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (prop == null) continue;
                        
                        var val = prop.GetValue(element);
                        if (val == null)
                        {
                            Log.LogInfo($"[Child] {indent}{propName}: null");
                        }
                        else if (val is IList list)
                        {
                            Log.LogInfo($"[Child] {indent}{propName}: List<{list.Count}> ⭐");
                        }
                        else
                        {
                            string valStr = val.ToString();
                            if (valStr.Length > 40) valStr = valStr.Substring(0, 40) + "...";
                            Log.LogInfo($"[Child] {indent}{propName}: {valStr}");
                        }
                    }
                    catch { }
                }
                
                // Buscar campos com "data" ou "item" no nome
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    if (f.Name.StartsWith("<")) continue;
                    var nameLower = f.Name.ToLower();
                    if (!nameLower.Contains("data") && !nameLower.Contains("item") && !nameLower.Contains("row") && !nameLower.Contains("list")) continue;
                    
                    try
                    {
                        var val = f.GetValue(element);
                        if (val == null) continue;
                        
                        if (val is IList list)
                        {
                            Log.LogInfo($"[Child] {indent}FIELD {f.Name}: List<{list.Count}> ⭐⭐");
                        }
                        else
                        {
                            Log.LogInfo($"[Child] {indent}FIELD {f.Name}: {val.GetType().Name}");
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void FindAllLists()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var allElements = new List<VisualElement>();
                    FindAllElements(root, allElements, 0, 50);
                    
                    Log.LogInfo($"[List] Escaneando {allElements.Count} elementos...");
                    
                    int found = 0;
                    foreach (var el in allElements)
                    {
                        if (found >= 20) break;
                        
                        try
                        {
                            var type = el.GetType();
                            
                            // Propriedades
                            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                            foreach (var p in props)
                            {
                                if (p.GetIndexParameters().Length > 0) continue;
                                try
                                {
                                    var val = p.GetValue(el);
                                    if (val is IList list && list.Count > 0)
                                    {
                                        Log.LogInfo($"[List] {el.name}.{p.Name}: List<{list.Count}> ⭐⭐⭐");
                                        found++;
                                        break;
                                    }
                                }
                                catch { }
                            }
                            
                            // Campos
                            if (found < 20)
                            {
                                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                                foreach (var f in fields)
                                {
                                    if (f.Name.StartsWith("<")) continue;
                                    try
                                    {
                                        var val = f.GetValue(el);
                                        if (val is IList list && list.Count > 0)
                                        {
                                            Log.LogInfo($"[List] {el.name}.FIELD {f.Name}: List<{list.Count}> ⭐⭐⭐");
                                            found++;
                                            break;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }
                    }
                    
                    Log.LogInfo($"[List] Total: {found} listas encontradas");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[List] Erro: {ex.Message}");
            }
        }
        
        private static void FindAllElements(VisualElement element, List<VisualElement> results, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            results.Add(element);
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindAllElements(element[i], results, depth + 1, maxDepth);
            }
        }
        
        private static void FindElementsWithPattern(VisualElement element, List<VisualElement> results, int depth, int maxDepth, Func<VisualElement, bool> predicate)
        {
            if (element == null || depth > maxDepth) return;
            
            if (predicate(element)) results.Add(element);
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsWithPattern(element[i], results, depth + 1, maxDepth, predicate);
            }
        }
        
        private static void FindAndExportTable()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var found = FindListInTree(root, 0, 50);
                    if (found != null)
                    {
                        Log.LogInfo($"[Export] ✅ {found.Count} itens");
                        ExportCsv(found);
                        return;
                    }
                }
                
                Log.LogWarning("[Export] Nenhum dado encontrado.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static IList FindListInTree(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            
            var type = element.GetType();
            
            // Propriedades
            try
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var p in props)
                {
                    try
                    {
                        var val = p.GetValue(element);
                        if (val is IList list && list.Count > 0) return list;
                    }
                    catch { }
                }
            }
            catch { }
            
            // Campos
            try
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    if (f.Name.StartsWith("<")) continue;
                    try
                    {
                        var val = f.GetValue(element);
                        if (val is IList list && list.Count > 0) return list;
                    }
                    catch { }
                }
            }
            catch { }
            
            // Recursão
            for (int i = 0; i < element.childCount; i++)
            {
                var found = FindListInTree(element[i], depth + 1, maxDepth);
                if (found != null) return found;
            }
            
            return null;
        }
        
        private static void ExportCsv(IList data)
        {
            try
            {
                var first = data[0];
                if (first == null)
                {
                    Log.LogWarning("[CSV] Primeiro item é null");
                    return;
                }
                
                var item1Prop = first.GetType().GetProperty("Item1");
                object targetObj = item1Prop != null ? item1Prop.GetValue(first) : first;
                
                var type = targetObj.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                var csv = new System.Text.StringBuilder();
                var headers = new List<string>();
                
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length == 0 && p.Name.Length < 30) headers.Add(p.Name);
                }
                csv.AppendLine(string.Join(";", headers));
                
                int count = 0;
                foreach (var item in data)
                {
                    if (item == null) continue;
                    object rowObj = item1Prop != null ? item1Prop.GetValue(item) : item;
                    
                    var values = new List<string>();
                    foreach (var p in props)
                    {
                        if (p.GetIndexParameters().Length > 0 || p.Name.Length >= 30) continue;
                        try
                        {
                            var val = p.GetValue(rowObj);
                            values.Add((val?.ToString() ?? "").Replace(";", ","));
                        }
                        catch { values.Add(""); }
                    }
                    csv.AppendLine(string.Join(";", values));
                    count++;
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[CSV] ✅ {count} linhas em: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[CSV] Erro: {ex.Message}");
            }
        }
    }
}
