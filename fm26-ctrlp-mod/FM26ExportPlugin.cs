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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.19.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.19.0 CARREGADO!");
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
                    ExportData();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Mapear hierarquia PlayerSearchReport");
                    MapHierarchy("PlayerSearchReport");
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Mapear hierarquia TeamSquadReport");
                    MapHierarchy("TeamSquadReport");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void MapHierarchy(string reportName)
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
                        Log.LogInfo($"[Map] === {reportName} ===");
                        Log.LogInfo($"[Map] childCount: {report.childCount}");
                        
                        // Mapear hierarquia completa
                        MapElement(report, 0, 5);
                    }
                    else
                    {
                        Log.LogWarning($"[Map] {reportName} não encontrado");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Map] Erro: {ex.Message}");
            }
        }
        
        private static void MapElement(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            string indent = new string(' ', depth * 2);
            string dsInfo = "";
            
            // Tentar ler dataSource
            try
            {
                var dsProp = element.GetType().GetProperty("dataSource");
                if (dsProp != null)
                {
                    var ds = dsProp.GetValue(element);
                    if (ds != null)
                    {
                        dsInfo = $" [DS: {ds.GetType().Name}]";
                        
                        // Se tem dataSource, explorar
                        ExploreDataSource(ds, depth + 1, 2);
                    }
                }
            }
            catch { }
            
            // Verificar se parece com tabela/lista
            string typeHint = "";
            string nameLower = element.name.ToLower();
            if (nameLower.Contains("table") || nameLower.Contains("list") || 
                nameLower.Contains("row") || nameLower.Contains("item") ||
                nameLower.Contains("streamed"))
            {
                typeHint = " ⭐";
            }
            
            Log.LogInfo($"[Map] {indent}{element.name} ({element.GetType().Name}){dsInfo}{typeHint}");
            
            // Recursão nos filhos
            for (int i = 0; i < element.childCount && i < 20; i++)
            {
                MapElement(element[i], depth + 1, maxDepth);
            }
        }
        
        private static void ExploreDataSource(object ds, int depth, int maxDepth)
        {
            if (ds == null || depth > maxDepth) return;
            
            try
            {
                var type = ds.GetType();
                string indent = new string(' ', depth * 2);
                
                // Procurar propriedades que parecem listas
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    
                    string nameLower = p.Name.ToLower();
                    bool isList = nameLower.Contains("list") || nameLower.Contains("items") ||
                                  nameLower.Contains("rows") || nameLower.Contains("data") ||
                                  nameLower.Contains("players") || nameLower.Contains("source");
                    
                    if (isList || p.PropertyType.Name.Contains("List") || p.PropertyType.Name.Contains("IList"))
                    {
                        try
                        {
                            var val = p.GetValue(ds);
                            if (val is IList list)
                            {
                                Log.LogInfo($"[DS] {indent}{p.Name}: List<{list.Count} itens>");
                                if (list.Count > 0)
                                {
                                    var first = list[0];
                                    Log.LogInfo($"[DS] {indent}  Primeiro: {first?.GetType().Name ?? "null"}");
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
        
        private static void ExportData()
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
                        
                        Log.LogInfo($"[Export] Buscando dados em {targetName}...");
                        
                        // Buscar recursivamente
                        var data = FindDataInTree(target, 0, 10);
                        if (data != null)
                        {
                            Log.LogInfo($"[Export] ✅ Dados: {data.Count} itens");
                            ExportCsv(data);
                            return;
                        }
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
                        var list = FindListInObject(ds, 0, 3);
                        if (list != null && list.Count > 0) return list;
                    }
                }
            }
            catch { }
            
            // Recursão nos filhos
            for (int i = 0; i < element.childCount && i < 50; i++)
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
                            Log.LogInfo($"[Find] Encontrado: {p.Name} com {list.Count} itens");
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
