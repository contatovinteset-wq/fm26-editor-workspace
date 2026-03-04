using System;
using System.Reflection;
using System.Collections;
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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.21.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.21.0 CARREGADO!");
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
                    Log.LogInfo(">>> F9 - Buscar elementos com MUITOS filhos (tabelas)");
                    FindHighChildCountElements();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Investigar GridLayoutElementContent");
                    InvestigateElement("GridLayoutElementContent");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void FindHighChildCountElements()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    Log.LogInfo($"[High] Escaneando todos os elementos...");
                    
                    var candidates = new List<(VisualElement el, int count)>();
                    FindElementsWithHighChildCount(root, candidates, 0, 40);
                    
                    // Ordenar por childCount descendente
                    candidates.Sort((a, b) => b.count.CompareTo(a.count));
                    
                    Log.LogInfo($"[High] Top 20 elementos com mais filhos:");
                    for (int i = 0; i < Math.Min(20, candidates.Count); i++)
                    {
                        var (el, count) = candidates[i];
                        string dsInfo = GetDataSourceInfo(el);
                        Log.LogInfo($"[High] #{i+1}: {el.name} ({el.GetType().Name}) [{count} filhos]{dsInfo}");
                        
                        // Se tem dataSource, explorar
                        if (count > 10)
                        {
                            ExploreDataSource(el, "  ");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[High] Erro: {ex.Message}");
            }
        }
        
        private static void FindElementsWithHighChildCount(VisualElement element, List<(VisualElement, int)> results, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            if (element.childCount > 5)
            {
                results.Add((element, element.childCount));
            }
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsWithHighChildCount(element[i], results, depth + 1, maxDepth);
            }
        }
        
        private static string GetDataSourceInfo(VisualElement element)
        {
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp != null)
                {
                    var ds = dsProp.GetValue(element);
                    if (ds != null)
                    {
                        return $" [DS: {ds.GetType().Name}]";
                    }
                }
            }
            catch { }
            return "";
        }
        
        private static void ExploreDataSource(VisualElement element, string indent)
        {
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp == null) return;
                
                var ds = dsProp.GetValue(element);
                if (ds == null) return;
                
                Log.LogInfo($"[DS] {indent}dataSource: {ds.GetType().FullName}");
                
                // Procurar listas
                var props = ds.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (p.Name.Length > 25) continue;
                    
                    try
                    {
                        var val = p.GetValue(ds);
                        if (val is IList list)
                        {
                            Log.LogInfo($"[DS] {indent}  {p.Name}: List<{list.Count}> ⭐");
                        }
                        else if (val != null && !p.PropertyType.IsPrimitive && p.PropertyType != typeof(string))
                        {
                            // Explorar sub-objeto
                            var subProps = val.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var sp in subProps)
                            {
                                if (sp.GetIndexParameters().Length > 0) continue;
                                try
                                {
                                    var subVal = sp.GetValue(val);
                                    if (subVal is IList subList)
                                    {
                                        Log.LogInfo($"[DS] {indent}  {p.Name}.{sp.Name}: List<{subList.Count}> ⭐⭐");
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void InvestigateElement(string elementName)
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var elements = new List<VisualElement>();
                    FindElementsByName(root, elementName, elements, 0, 40);
                    
                    Log.LogInfo($"[Inv] Encontrados {elements.Count} '{elementName}'");
                    
                    foreach (var el in elements)
                    {
                        Log.LogInfo($"[Inv] === {el.name} ===");
                        Log.LogInfo($"[Inv] childCount: {el.childCount}");
                        Log.LogInfo($"[Inv] Tipo: {el.GetType().FullName}");
                        
                        ExploreDataSource(el, "");
                        
                        // Mostrar filhos
                        for (int i = 0; i < el.childCount && i < 15; i++)
                        {
                            var child = el[i];
                            Log.LogInfo($"[Inv]   [{i}] {child.name} ({child.GetType().Name}) [{child.childCount}]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Inv] Erro: {ex.Message}");
            }
        }
        
        private static void FindElementsByName(VisualElement element, string name, List<VisualElement> results, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            if (element.name.Contains(name))
            {
                results.Add(element);
            }
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsByName(element[i], name, results, depth + 1, maxDepth);
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
                    
                    // Buscar elementos com dataSource que contenha lista
                    var found = FindDataInTree(root, 0, 35);
                    if (found != null)
                    {
                        Log.LogInfo($"[Export] ✅ Dados: {found.Count} itens");
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
        
        private static IList FindDataInTree(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            
            // Verificar dataSource
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp != null)
                {
                    var ds = dsProp.GetValue(element);
                    if (ds != null)
                    {
                        var list = FindListInObject(ds, 0, 5);
                        if (list != null && list.Count > 0) return list;
                    }
                }
            }
            catch { }
            
            // Recursão nos filhos
            for (int i = 0; i < element.childCount; i++)
            {
                var found = FindDataInTree(element[i], depth + 1, maxDepth);
                if (found != null) return found;
            }
            
            return null;
        }
        
        private static IList FindListInObject(object obj, int depth, int maxDepth)
        {
            if (obj == null || depth > maxDepth) return null;
            
            try
            {
                var type = obj.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    
                    try
                    {
                        var val = p.GetValue(obj);
                        if (val is IList list && list.Count > 0)
                        {
                            return list;
                        }
                        else if (val != null && !p.PropertyType.IsPrimitive && p.PropertyType != typeof(string))
                        {
                            var found = FindListInObject(val, depth + 1, maxDepth);
                            if (found != null) return found;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
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
                
                // Tenta extrair Item1 de ValueTuple
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
