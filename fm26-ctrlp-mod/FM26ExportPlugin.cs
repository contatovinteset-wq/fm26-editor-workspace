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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.5.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.5.0 CARREGADO!");
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
        
        // Tipos
        private static Type _streamedTableType;
        private static Type _streamedListViewType;
        private static Type _streamedObjectListType;
        
        // Propriedades via reflection
        private static PropertyInfo _dataSourceProp;
        private static PropertyInfo _dataSourcePathProp;
        private static PropertyInfo _sourceDataProp;
        
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
            
            // Ctrl+P - EXPORTAR
            if (ctrl && p)
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P - EXPORTAR");
                Log.LogInfo(">>> Ctrl+P - EXPORTAR");
                DoExport();
            }
            
            // F9 - Investigar dataSource dos elementos
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F9 - Investigar dataSource");
                Log.LogInfo(">>> F9 - Investigar dataSource");
                InvestigateDataSource();
            }
            
            // F10 - Buscar StreamedListView ativos
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Buscar StreamedListView");
                Log.LogInfo(">>> F10 - Buscar StreamedListView");
                FindActiveStreamedLists();
            }
            
            // F11 - Investigar bindings
            if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F11 - Investigar bindings");
                Log.LogInfo(">>> F11 - Investigar bindings");
                InvestigateBindings();
            }
            
            // F12 - Dump completo do elemento Report
            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F12 - Dump do Report");
                Log.LogInfo(">>> F12 - Dump do Report");
                DumpReportElement();
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                // Tipos do SI.Bindable
                _streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                Log.LogInfo($"[Init] StreamedTable: {(_streamedTableType != null ? "OK" : "NÃO")}");
                
                _streamedListViewType = Type.GetType("SI.Bindable.StreamedListView, SI.Bindable");
                Log.LogInfo($"[Init] StreamedListView: {(_streamedListViewType != null ? "OK" : "NÃO")}");
                
                _streamedObjectListType = Type.GetType("SI.Bindable.StreamedObjectList, SI.Bindable");
                Log.LogInfo($"[Init] StreamedObjectList: {(_streamedObjectListType != null ? "OK" : "NÃO")}");
                
                // Propriedades do VisualElement
                var veType = typeof(VisualElement);
                _dataSourceProp = veType.GetProperty("dataSource", BindingFlags.Public | BindingFlags.Instance);
                _dataSourcePathProp = veType.GetProperty("dataSourcePath", BindingFlags.Public | BindingFlags.Instance);
                
                Log.LogInfo($"[Init] dataSource prop: {(_dataSourceProp != null ? "OK" : "NÃO")}");
                
                // Propriedade SourceData do StreamedListView
                if (_streamedListViewType != null)
                {
                    _sourceDataProp = _streamedListViewType.GetProperty("SourceData", BindingFlags.NonPublic | BindingFlags.Instance);
                    Log.LogInfo($"[Init] SourceData prop: {(_sourceDataProp != null ? "OK" : "NÃO")}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
        
        // F9 - Investigar dataSource dos elementos
        private static void InvestigateDataSource()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[DataSource] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Procurar Report
                    for (int i = 0; i < root.childCount; i++)
                    {
                        var child = root[i];
                        if (child?.name == "Report")
                        {
                            Log.LogInfo($"[DataSource] Report encontrado");
                            ScanDataSourceRecursive(child, 0, 8);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[DataSource] Erro: {ex.Message}");
            }
        }
        
        private static void ScanDataSourceRecursive(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                string name = element.name ?? "";
                var type = element.GetType();
                string typeName = type.FullName ?? type.Name ?? "";
                string indent = new string(' ', depth * 2);
                
                // Verificar se tem dataSource
                object dataSource = null;
                if (_dataSourceProp != null)
                {
                    try
                    {
                        dataSource = _dataSourceProp.GetValue(element);
                    }
                    catch { }
                }
                
                // Se tem dataSource, logar
                if (dataSource != null)
                {
                    Log.LogInfo($"[DataSource] {indent}{name} -> dataSource: {dataSource.GetType().FullName}");
                    InspectObject(dataSource, depth + 1);
                }
                
                // NOVO: Verificar pelo nome do tipo (IL2CPP-safe)
                bool isStreamedTable = typeName.Contains("StreamedTable");
                bool isStreamedList = typeName.Contains("StreamedListView") || 
                                      typeName.Contains("StreamedObjectList");
                
                if (isStreamedTable || isStreamedList)
                {
                    Log.LogInfo($"[DataSource] {indent}⭐ {name} É {typeName}!");
                    
                    // Procurar qualquer propriedade que retorne IList
                    var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                        {
                            try
                            {
                                var value = prop.GetValue(element);
                                if (value is IList list)
                                {
                                    Log.LogInfo($"[DataSource] {indent}  -> {prop.Name}: {list.Count} itens");
                                    
                                    if (list.Count > 0)
                                    {
                                        Log.LogInfo($"[DataSource] {indent}  -> Tipo do item: {list[0]?.GetType().FullName}");
                                        DumpObjectProperties(list[0], "PrimeiroItem");
                                        
                                        // Salvar para exportação
                                        _lastFoundData = list;
                                        _lastFoundElement = element;
                                        _lastFoundPropertyName = prop.Name;
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                
                // Recursão nos filhos - aumentar limite
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    ScanDataSourceRecursive(element[i], depth + 1, maxDepth);
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[DataSource] Erro no elemento: {ex.Message}");
            }
        }
        
        private static void InspectObject(object obj, int depth)
        {
            if (obj == null || depth > 3) return;
            
            try
            {
                var type = obj.GetType();
                
                // Se for lista, mostrar count
                if (obj is IList list)
                {
                    Log.LogInfo($"[Inspect] É IList com {list.Count} itens");
                    if (list.Count > 0)
                    {
                        Log.LogInfo($"[Inspect] Tipo do item: {list[0]?.GetType().FullName}");
                    }
                    return;
                }
                
                // Se for dicionário, mostrar count
                if (obj is IDictionary dict)
                {
                    Log.LogInfo($"[Inspect] É IDictionary com {dict.Count} itens");
                    return;
                }
                
                // Listar propriedades públicas relevantes
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                int count = 0;
                
                foreach (var prop in props)
                {
                    if (count >= 15) break;
                    if (prop.GetIndexParameters().Length > 0) continue;
                    
                    try
                    {
                        var value = prop.GetValue(obj);
                        if (value == null) continue;
                        
                        string info = $"[Inspect] {prop.Name}: {value.GetType().Name}";
                        
                        if (value is IList l)
                        {
                            info += $" (Count={l.Count})";
                        }
                        
                        Log.LogInfo(info);
                        count++;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Inspect] Erro: {ex.Message}");
            }
        }
        
        private static void DumpObjectProperties(object obj, string label)
        {
            if (obj == null) return;
            
            try
            {
                var type = obj.GetType();
                Log.LogInfo($"[{label}] Tipo: {type.FullName}");
                
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                int count = 0;
                foreach (var prop in props)
                {
                    if (count >= 30) break;
                    if (prop.GetIndexParameters().Length > 0) continue;
                    
                    try
                    {
                        var value = prop.GetValue(obj);
                        string valueStr = value?.ToString() ?? "null";
                        if (valueStr.Length > 50) valueStr = valueStr.Substring(0, 50) + "...";
                        
                        Log.LogInfo($"[{label}] {prop.Name} = {valueStr}");
                        count++;
                    }
                    catch { }
                }
                
                Log.LogInfo($"[{label}] {count} propriedades listadas");
            }
            catch (Exception ex)
            {
                Log.LogError($"[{label}] Erro: {ex.Message}");
            }
        }
        
        // F10 - Buscar instâncias ativas de StreamedListView
        private static void FindActiveStreamedLists()
        {
            try
            {
                Log.LogInfo("[ActiveLists] Buscando StreamedListView ativos...");
                
                if (_streamedListViewType == null)
                {
                    Log.LogError("[ActiveLists] Tipo StreamedListView não encontrado");
                    return;
                }
                
                // Buscar todos os VisualElements e verificar se são StreamedListView
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int found = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    found += ScanForStreamedList(root, 0, 15);
                }
                
                Log.LogInfo($"[ActiveLists] Total encontrados: {found}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[ActiveLists] Erro: {ex.Message}");
            }
        }
        
        private static int ScanForStreamedList(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return 0;
            
            int found = 0;
            
            try
            {
                var type = element.GetType();
                string typeName = type.FullName ?? type.Name ?? "";
                string elementName = element.name ?? "";
                
                // NOVO: Verificar pelo nome do tipo (IL2CPP-safe)
                bool isStreamedTable = typeName.Contains("StreamedTable") || 
                                       elementName.Contains("Table") ||
                                       elementName.Contains("List");
                
                bool isStreamedListView = typeName.Contains("StreamedListView") ||
                                          typeName.Contains("StreamedObjectList") ||
                                          elementName.Contains("ListView");
                
                if (isStreamedTable || isStreamedListView)
                {
                    found++;
                    Log.LogInfo($"[ActiveLists] ⭐ ENCONTRADO: {elementName} ({typeName})");
                    
                    // Tentar encontrar dados via reflection em propriedades comuns
                    TryExtractDataFromElement(element, depth);
                }
                
                // Recursão - aumentar limite para garantir que achamos tudo
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    found += ScanForStreamedList(element[i], depth + 1, maxDepth);
                }
            }
            catch { }
            
            return found;
        }
        
        private static void TryExtractDataFromElement(VisualElement element, int depth)
        {
            try
            {
                var type = element.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var prop in props)
                {
                    string propName = prop.Name.ToLower();
                    
                    // Procurar propriedades que parecem conter dados
                    if (propName.Contains("data") || propName.Contains("items") || 
                        propName.Contains("source") || propName.Contains("list"))
                    {
                        try
                        {
                            var value = prop.GetValue(element);
                            if (value == null) continue;
                            
                            if (value is IList list)
                            {
                                Log.LogInfo($"[ActiveLists]   -> {prop.Name}: {list.Count} itens");
                                
                                if (list.Count > 0)
                                {
                                    Log.LogInfo($"[ActiveLists]   -> Tipo do item: {list[0]?.GetType().FullName}");
                                    DumpObjectProperties(list[0], "Item0");
                                }
                                
                                // Salvar referência para exportação
                                _lastFoundData = list;
                                _lastFoundElement = element;
                                _lastFoundPropertyName = prop.Name;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
        
        private static IList _lastFoundData;
        private static VisualElement _lastFoundElement;
        private static string _lastFoundPropertyName;
        
        // F11 - Investigar bindings
        private static void InvestigateBindings()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Procurar Report
                    for (int i = 0; i < root.childCount; i++)
                    {
                        var child = root[i];
                        if (child?.name == "Report")
                        {
                            Log.LogInfo($"[Bindings] Report encontrado");
                            ScanBindingsRecursive(child, 0, 6);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Bindings] Erro: {ex.Message}");
            }
        }
        
        private static void ScanBindingsRecursive(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                string name = element.name ?? "";
                var type = element.GetType();
                
                // Procurar campos de binding
                var bindingFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (var field in bindingFields)
                {
                    string fieldName = field.Name.ToLower();
                    
                    if (fieldName.Contains("binding") || fieldName.Contains("data") || fieldName.Contains("source"))
                    {
                        try
                        {
                            var value = field.GetValue(element);
                            if (value != null)
                            {
                                Log.LogInfo($"[Bindings] {name}.{field.Name}: {value.GetType().Name}");
                                
                                // Se for lista, mostrar count
                                if (value is IList list)
                                {
                                    Log.LogInfo($"[Bindings] -> Count: {list.Count}");
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                // Recursão limitada
                for (int i = 0; i < element.childCount && i < 20; i++)
                {
                    ScanBindingsRecursive(element[i], depth + 1, maxDepth);
                }
            }
            catch { }
        }
        
        // F12 - Dump completo do Report
        private static void DumpReportElement()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    for (int i = 0; i < root.childCount; i++)
                    {
                        var child = root[i];
                        if (child?.name == "Report")
                        {
                            Log.LogInfo($"[Dump] === DUMP DO REPORT ===");
                            DumpElementRecursive(child, 0, 8);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Dump] Erro: {ex.Message}");
            }
        }
        
        private static void DumpElementRecursive(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                string indent = new string(' ', depth * 2);
                string name = element.name ?? "(sem nome)";
                var type = element.GetType();
                string typeName = type.FullName ?? type.Name ?? "";
                
                // Só mostrar elementos interessantes ou no início
                bool isInteresting = typeName.Contains("Streamed") || 
                                     typeName.Contains("Table") || 
                                     typeName.Contains("List") ||
                                     name.Contains("Table") ||
                                     name.Contains("List") ||
                                     depth <= 3;
                
                if (isInteresting)
                {
                    Log.LogInfo($"[Dump] {indent}{name} ({typeName})");
                    
                    // Se tiver dataSource, mostrar
                    if (_dataSourceProp != null)
                    {
                        try
                        {
                            var ds = _dataSourceProp.GetValue(element);
                            if (ds != null)
                            {
                                Log.LogInfo($"[Dump] {indent}  dataSource: {ds.GetType().FullName}");
                                
                                if (ds is IList list)
                                {
                                    Log.LogInfo($"[Dump] {indent}  -> IList com {list.Count} itens!");
                                }
                            }
                        }
                        catch { }
                    }
                    
                    // NOVO: Procurar propriedades que retornam IList
                    var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                        {
                            try
                            {
                                var val = prop.GetValue(element);
                                if (val is IList list && list.Count > 0)
                                {
                                    Log.LogInfo($"[Dump] {indent}  -> {prop.Name}: {list.Count} itens");
                                    
                                    // Salvar referência
                                    _lastFoundData = list;
                                    _lastFoundElement = element;
                                    _lastFoundPropertyName = prop.Name;
                                }
                            }
                            catch { }
                        }
                    }
                }
                
                // Recursão - explorar mais filhos
                int childLimit = depth <= 4 ? 50 : 20;
                for (int i = 0; i < element.childCount && i < childLimit; i++)
                {
                    DumpElementRecursive(element[i], depth + 1, maxDepth);
                }
            }
            catch { }
        }
        
        private static void DoExport()
        {
            try
            {
                Log.LogInfo("[Export] Iniciando exportação...");
                
                // Primeiro, buscar dados ativos
                FindActiveStreamedLists();
                
                // Se encontramos dados, exportar
                if (_lastFoundData != null && _lastFoundData.Count > 0)
                {
                    ExportToCsv(_lastFoundData);
                }
                else
                {
                    Log.LogWarning("[Export] Nenhum dado encontrado. Certifique-se de que uma tabela está visível.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void ExportToCsv(IList data)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    Log.LogWarning("[Export] Lista vazia, nada para exportar");
                    return;
                }
                
                Log.LogInfo($"[Export] Exportando {data.Count} registros...");
                
                var firstItem = data[0];
                if (firstItem == null)
                {
                    Log.LogError("[Export] Primeiro item é null");
                    return;
                }
                
                var type = firstItem.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                // Preparar CSV
                var csv = new System.Text.StringBuilder();
                
                // Header
                var headers = new List<string>();
                foreach (var prop in props)
                {
                    if (prop.GetIndexParameters().Length == 0)
                    {
                        headers.Add(prop.Name);
                    }
                }
                csv.AppendLine(string.Join(";", headers));
                
                // Dados
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
                            string str = value?.ToString() ?? "";
                            // Escapar ; e 
                            str = str.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
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
                
                // Salvar arquivo
                var path = System.IO.Path.Combine(
                    BepInEx.Paths.PluginPath, 
                    $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );
                
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[Export] ✅ {rowCount} linhas salvas em: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro ao gerar CSV: {ex.Message}");
            }
        }
    }
}
