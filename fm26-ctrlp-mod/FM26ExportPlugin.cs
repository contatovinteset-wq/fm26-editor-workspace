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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.37.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.37.0 CARREGADO!");
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
                    Log.LogInfo(">>> Ctrl+P - Exportar StreamedTable.SourceData");
                    ExportFromStreamedTable();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Listar StreamedTables ativos");
                    ListStreamedTables();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Detalhes do primeiro item de SourceData");
                    ShowSourceDataItemDetails();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void ListStreamedTables()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int total = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.rootVisualElement == null) continue;
                    
                    var tables = new List<VisualElement>();
                    FindElementsByType(doc.rootVisualElement, "StreamedTable", tables, 0, 50);
                    
                    foreach (var table in tables)
                    {
                        total++;
                        var sourceData = GetSourceData(table);
                        var itemCount = GetItemCount(table);
                        
                        Log.LogInfo($"[ST] {table.name} - ItemCount: {itemCount}, SourceData: {(sourceData != null ? "present" : "null")}");
                    }
                }
                
                Log.LogInfo($"[ST] Total: {total} StreamedTables");
            }
            catch (Exception ex)
            {
                Log.LogError($"[ST] Erro: {ex.Message}");
            }
        }
        
        private static void ShowSourceDataItemDetails()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.rootVisualElement == null) continue;
                    
                    var tables = new List<VisualElement>();
                    FindElementsByType(doc.rootVisualElement, "StreamedTable", tables, 0, 50);
                    
                    foreach (var table in tables)
                    {
                        var sourceData = GetSourceData(table);
                        if (sourceData == null) continue;
                        
                        // Pegar primeiro item
                        var enumerator = sourceData.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            var first = enumerator.Current;
                            if (first != null)
                            {
                                var type = first.GetType();
                                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                
                                Log.LogInfo($"[Data] Tipo do item: {type.FullName}");
                                Log.LogInfo($"[Data] {props.Length} propriedades:");
                                
                                foreach (var p in props.Take(30))
                                {
                                    try
                                    {
                                        var val = p.GetValue(first);
                                        var valStr = val?.ToString() ?? "null";
                                        if (valStr.Length > 50) valStr = valStr.Substring(0, 50) + "...";
                                        Log.LogInfo($"[Data]   {p.Name}: {valStr}");
                                    }
                                    catch { }
                                }
                                return;
                            }
                        }
                    }
                }
                
                Log.LogWarning("[Data] Nenhum item encontrado");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Data] Erro: {ex.Message}");
            }
        }
        
        private static void ExportFromStreamedTable()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.rootVisualElement == null) continue;
                    
                    var tables = new List<VisualElement>();
                    FindElementsByType(doc.rootVisualElement, "StreamedTable", tables, 0, 50);
                    
                    foreach (var table in tables)
                    {
                        var sourceData = GetSourceData(table);
                        if (sourceData == null) continue;
                        
                        Log.LogInfo($"[Export] Tabela: {table.name}");
                        
                        var list = new List<object>();
                        foreach (var item in sourceData)
                        {
                            list.Add(item);
                            if (list.Count >= 50000) break;
                        }
                        
                        if (list.Count > 0)
                        {
                            Log.LogInfo($"[Export] {list.Count} itens encontrados");
                            ExportCsv(list);
                            return;
                        }
                    }
                }
                
                Log.LogWarning("[Export] Nenhum dado encontrado");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static IList GetSourceData(VisualElement element)
        {
            try
            {
                var type = element.GetType();
                var prop = type.GetProperty("SourceData", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    return prop.GetValue(element) as IList;
                }
            }
            catch { }
            return null;
        }
        
        private static int GetItemCount(VisualElement element)
        {
            try
            {
                var type = element.GetType();
                var prop = type.GetProperty("ItemCount", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    return (int)prop.GetValue(element);
                }
            }
            catch { }
            return 0;
        }
        
        private static void FindElementsByType(VisualElement element, string typeName, List<VisualElement> results, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            if (element.GetType().Name == typeName)
            {
                results.Add(element);
            }
            
            for (int i = 0; i < element.childCount; i++)
            {
                FindElementsByType(element[i], typeName, results, depth + 1, maxDepth);
            }
        }
        
        private static void ExportCsv(List<object> data)
        {
            try
            {
                if (data.Count == 0) return;
                
                var first = data[0];
                if (first == null) return;
                
                var type = first.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetIndexParameters().Length == 0 && p.Name.Length < 40)
                    .ToList();
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine(string.Join(";", props.Select(p => p.Name)));
                
                int count = 0;
                foreach (var item in data)
                {
                    if (item == null) continue;
                    
                    var values = props.Select(p =>
                    {
                        try
                        {
                            var val = p.GetValue(item);
                            var str = val?.ToString() ?? "";
                            return str.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                        }
                        catch { return ""; }
                    });
                    
                    csv.AppendLine(string.Join(";", values));
                    count++;
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[CSV] ✅ {count} linhas exportadas!");
                Log.LogInfo($"[CSV] Arquivo: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[CSV] Erro: {ex.Message}");
            }
        }
    }
}
