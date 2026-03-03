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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.18.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.18.0 CARREGADO!");
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
                    Log.LogInfo(">>> Ctrl+P - Exportar via dataSource");
                    ExportFromDataSource();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Investigar dataSource do PlayerSearchReport");
                    InvestigateDataSource("PlayerSearchReport");
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Investigar dataSource do TeamSquadReport");
                    InvestigateDataSource("TeamSquadReport");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateDataSource(string reportName)
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var report = FindElementByName(root, reportName, 0, 30);
                    if (report != null)
                    {
                        Log.LogInfo($"[DS] === {reportName} ===");
                        
                        // Ler dataSource
                        var dsProp = report.GetType().GetProperty("dataSource");
                        if (dsProp != null)
                        {
                            try
                            {
                                var ds = dsProp.GetValue(report);
                                if (ds != null)
                                {
                                    Log.LogInfo($"[DS] dataSource: {ds.GetType().FullName}");
                                    ExploreObject(ds, "dataSource", 0, 3);
                                }
                                else
                                {
                                    Log.LogInfo($"[DS] dataSource é null");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.LogWarning($"[DS] Erro ao ler dataSource: {ex.Message}");
                            }
                        }
                        
                        // Ler bindings
                        var bindingsProp = report.GetType().GetProperty("bindings");
                        if (bindingsProp != null)
                        {
                            try
                            {
                                var bindings = bindingsProp.GetValue(report) as IList;
                                if (bindings != null)
                                {
                                    Log.LogInfo($"[DS] bindings: {bindings.Count} itens");
                                }
                            }
                            catch { }
                        }
                        
                        // Ler hierarquia de filhos
                        Log.LogInfo($"[DS] Filhos diretos: {report.childCount}");
                        for (int i = 0; i < report.childCount && i < 10; i++)
                        {
                            var child = report[i];
                            if (child != null)
                            {
                                Log.LogInfo($"[DS]   [{i}] {child.name} ({child.GetType().Name})");
                                
                                // Verificar dataSource do filho
                                var childDsProp = child.GetType().GetProperty("dataSource");
                                if (childDsProp != null)
                                {
                                    try
                                    {
                                        var childDs = childDsProp.GetValue(child);
                                        if (childDs != null)
                                        {
                                            Log.LogInfo($"[DS]      dataSource: {childDs.GetType().Name}");
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.LogWarning($"[DS] {reportName} não encontrado");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[DS] Erro: {ex.Message}");
            }
        }
        
        private static void ExploreObject(object obj, string path, int depth, int maxDepth)
        {
            if (obj == null || depth > maxDepth) return;
            
            try
            {
                var type = obj.GetType();
                
                // Propriedades comuns que podem ter dados
                string[] interestingProps = { "items", "rows", "data", "list", "players", "source", "value" };
                
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (p.Name == "Item") continue;
                    
                    string nameLower = p.Name.ToLower();
                    bool isInteresting = false;
                    foreach (var interest in interestingProps)
                    {
                        if (nameLower.Contains(interest))
                        {
                            isInteresting = true;
                            break;
                        }
                    }
                    
                    if (isInteresting || depth == 0)
                    {
                        try
                        {
                            var val = p.GetValue(obj);
                            if (val == null) continue;
                            
                            if (val is IList list)
                            {
                                Log.LogInfo($"[DS] {path}.{p.Name}: List com {list.Count} itens!");
                                if (list.Count > 0)
                                {
                                    Log.LogInfo($"[DS]    Primeiro: {list[0]?.GetType().Name ?? "null"}");
                                }
                            }
                            else if (!p.PropertyType.IsPrimitive && p.PropertyType != typeof(string))
                            {
                                Log.LogInfo($"[DS] {path}.{p.Name}: {p.PropertyType.Name}");
                                ExploreObject(val, $"{path}.{p.Name}", depth + 1, maxDepth);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
        
        private static void ExportFromDataSource()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    string[] targets = { "PlayerSearchReport", "TeamSquadReport" };
                    foreach (var targetName in targets)
                    {
                        var target = FindElementByName(root, targetName, 0, 30);
                        if (target == null) continue;
                        
                        Log.LogInfo($"[Export] Escaneando {targetName} e filhos...");
                        
                        // Tentar encontrar dados no target e seus filhos
                        var data = FindDataInElementTree(target, 0, 15);
                        if (data != null)
                        {
                            Log.LogInfo($"[Export] ✅ Dados encontrados: {data.Count} itens");
                            ExportCsv(data);
                            return;
                        }
                    }
                }
                
                Log.LogWarning("[Export] Nenhum dataSource com lista encontrado.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static IList FindDataInElementTree(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            
            try
            {
                // Verificar dataSource
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp != null)
                {
                    try
                    {
                        var ds = dsProp.GetValue(element);
                        if (ds != null)
                        {
                            var list = FindListInObject(ds, 0, 5);
                            if (list != null && list.Count > 0) return list;
                        }
                    }
                    catch { }
                }
                
                // Verificar propriedades diretas
                var props = element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (p.Name == "Item") continue;
                    
                    try
                    {
                        var val = p.GetValue(element);
                        if (val is IList list && list.Count > 5)
                        {
                            Log.LogInfo($"[Export] Lista em {element.name}.{p.Name}: {list.Count}");
                            return list;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            // Recursão nos filhos
            for (int i = 0; i < element.childCount && i < 50; i++)
            {
                var found = FindDataInElementTree(element[i], depth + 1, maxDepth);
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
        
        private static VisualElement FindElementByName(VisualElement element, string name, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            if (element.name == name) return element;
            
            for (int i = 0; i < element.childCount; i++)
            {
                var found = FindElementByName(element[i], name, depth + 1, maxDepth);
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
                
                // Tenta extrair Item1 de ValueTuple
                var item1Prop = first.GetType().GetProperty("Item1");
                object targetObj = item1Prop != null ? item1Prop.GetValue(first) : first;
                
                var type = targetObj.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                var csv = new System.Text.StringBuilder();
                var headers = new List<string>();
                
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length == 0) headers.Add(p.Name);
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
                        if (p.GetIndexParameters().Length > 0) continue;
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
