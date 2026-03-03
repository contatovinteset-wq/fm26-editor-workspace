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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.11.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        // Nomes de propriedades que geralmente têm listas de dados
        private static readonly string[] _dataProps = new string[]
        {
            "items", "Items", "list", "List", "data", "Data",
            "rows", "Rows", "source", "Source", "dataSource", "DataSource",
            "itemsSource", "ItemsSource", "binding", "Binding"
        };
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.11.0 CARREGADO!");
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
                
                if (!_initialized) return;
                if (Keyboard.current == null) return;
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - EXPORTAR");
                    SafeExport();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Buscar props seguras");
                    FindSafeProps();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Info tipos Streamed");
                    InfoStreamedTypes();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        // F9 - Buscar propriedades seguras
        private static void FindSafeProps()
        {
            try
            {
                var streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                var streamedListViewType = Type.GetType("SI.Bindable.StreamedListView, SI.Bindable");
                
                if (streamedTableType != null)
                {
                    Log.LogInfo("[F9] === StreamedTable props ===");
                    var props = streamedTableType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var p in props)
                    {
                        Log.LogInfo($"[F9]   {p.PropertyType.Name} {p.Name}");
                    }
                }
                
                if (streamedListViewType != null)
                {
                    Log.LogInfo("[F9] === StreamedListView props ===");
                    var props = streamedListViewType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var p in props)
                    {
                        Log.LogInfo($"[F9]   {p.PropertyType.Name} {p.Name}");
                    }
                }
                
                // Verificar se há instâncias desses tipos
                Log.LogInfo("[F9] === Buscando instâncias ===");
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int found = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        CountStreamedElements(root, ref found, 0, 15, doc.name);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[F9] Total StreamedElements: {found}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F9] Erro: {ex.Message}");
            }
        }
        
        private static void CountStreamedElements(VisualElement element, ref int count, int depth, int maxDepth, string docName)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var type = element.GetType();
                string typeName = type.FullName ?? "";
                string elementName = element.name ?? "";
                
                if (typeName.Contains("StreamedTable") || typeName.Contains("StreamedListView"))
                {
                    count++;
                    Log.LogInfo($"[F9] ⭐ [{docName}] {elementName}: {typeName}");
                }
                
                // Recursão simples
                for (int i = 0; i < element.childCount && i < 50; i++)
                {
                    try
                    {
                        CountStreamedElements(element[i], ref count, depth + 1, maxDepth, docName);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // F10 - Info tipos Streamed
        private static void InfoStreamedTypes()
        {
            try
            {
                var types = new string[]
                {
                    "SI.Bindable.StreamedTable, SI.Bindable",
                    "SI.Bindable.StreamedListView, SI.Bindable",
                    "SI.Bindable.StreamedObjectList, SI.Bindable"
                };
                
                foreach (var typeName in types)
                {
                    var t = Type.GetType(typeName);
                    if (t == null)
                    {
                        Log.LogInfo($"[F10] {typeName}: NÃO ENCONTRADO");
                        continue;
                    }
                    
                    Log.LogInfo($"[F10] === {t.Name} ===");
                    
                    // Campos públicos
                    var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var f in fields)
                    {
                        Log.LogInfo($"[F10]   field: {f.FieldType.Name} {f.Name}");
                    }
                    
                    // Métodos que retornam algo
                    var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (var m in methods)
                    {
                        if (m.Name.StartsWith("get_") && m.GetParameters().Length == 0)
                        {
                            string propName = m.Name.Substring(4);
                            Log.LogInfo($"[F10]   prop: {m.ReturnType.Name} {propName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[F10] Erro: {ex.Message}");
            }
        }
        
        // Ctrl+P - Exportar
        private static void SafeExport()
        {
            try
            {
                Log.LogInfo("[Export] Buscando dados...");
                
                var streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                var streamedListViewType = Type.GetType("SI.Bindable.StreamedListView, SI.Bindable");
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                IList foundData = null;
                string foundInfo = "";
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        FindAndExport(root, ref foundData, ref foundInfo, 0, 15, doc.name, streamedTableType, streamedListViewType);
                        
                        if (foundData != null) break;
                    }
                    catch { }
                }
                
                if (foundData != null && foundData.Count > 0)
                {
                    Log.LogInfo($"[Export] ✅ {foundInfo} ({foundData.Count} itens)");
                    ExportToCsv(foundData);
                }
                else
                {
                    Log.LogWarning("[Export] Nenhum dado encontrado.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void FindAndExport(VisualElement element, ref IList foundData, ref string foundInfo, int depth, int maxDepth, string docName, Type stType, Type slvType)
        {
            if (element == null || depth > maxDepth || foundData != null) return;
            
            try
            {
                var type = element.GetType();
                string typeName = type.FullName ?? "";
                string elementName = element.name ?? "";
                
                // Verificar se é um StreamedTable/ListView
                bool isStreamed = typeName.Contains("StreamedTable") || typeName.Contains("StreamedListView");
                
                if (isStreamed)
                {
                    Log.LogInfo($"[Export] Encontrado: {elementName} ({typeName})");
                    
                    // Tentar pegar dados via método GetList ou similar
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var m in methods)
                    {
                        if (foundData != null) break;
                        
                        try
                        {
                            // Procurar métodos que retornam IList
                            if (typeof(IList).IsAssignableFrom(m.ReturnType) && m.GetParameters().Length == 0)
                            {
                                var result = m.Invoke(element, null);
                                if (result is IList list && list.Count > 0)
                                {
                                    foundData = list;
                                    foundInfo = $"[{docName}] {elementName}.{m.Name}()";
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                    
                    // Tentar campos
                    if (foundData == null)
                    {
                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var f in fields)
                        {
                            if (foundData != null) break;
                            
                            try
                            {
                                if (typeof(IList).IsAssignableFrom(f.FieldType))
                                {
                                    var result = f.GetValue(element);
                                    if (result is IList list && list.Count > 0)
                                    {
                                        foundData = list;
                                        foundInfo = $"[{docName}] {elementName}.{f.Name}";
                                        break;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 50; i++)
                {
                    try
                    {
                        FindAndExport(element[i], ref foundData, ref foundInfo, depth + 1, maxDepth, docName, stType, slvType);
                        if (foundData != null) return;
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void ExportToCsv(IList data)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    Log.LogWarning("[Export] Lista vazia");
                    return;
                }
                
                var firstItem = data[0];
                if (firstItem == null)
                {
                    Log.LogError("[Export] Primeiro item é null");
                    return;
                }
                
                var type = firstItem.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                var csv = new System.Text.StringBuilder();
                var headers = new List<string>();
                
                foreach (var prop in props)
                {
                    if (prop.GetIndexParameters().Length == 0)
                    {
                        headers.Add(prop.Name);
                    }
                }
                csv.AppendLine(string.Join(";", headers));
                
                int rowCount = 0;
                foreach (var item in data)
                {
                    if (item == null) continue;
                    
                    var values = new List<string>();
                    foreach (var prop in props)
                    {
                        if (prop.GetIndexParameters().Length > 0) continue;
                        
                        try
                        {
                            var value = prop.GetValue(item);
                            string str = (value?.ToString() ?? "").Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                            values.Add(str);
                        }
                        catch
                        {
                            values.Add("");
                        }
                    }
                    csv.AppendLine(string.Join(";", values));
                    rowCount++;
                }
                
                var path = System.IO.Path.Combine(
                    BepInEx.Paths.PluginPath, 
                    $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );
                
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[Export] ✅ {rowCount} linhas salvas em: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro CSV: {ex.Message}");
            }
        }
    }
}
