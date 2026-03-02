using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.0.0 CARREGADO!");
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
        private static Type _panelManagerType;
        private static Type _streamedTableType;
        private static Type _streamedListViewType;
        private static Type _streamedObjectListType;
        private static Type _customViewExportDataType;
        private static Type _bindableExportDataType;
        
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
            
            // F10 - Buscar tabelas via Resources (NOVO!)
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Buscar tabelas via Resources.FindObjectsOfTypeAll");
                Log.LogInfo(">>> F10 - Buscar tabelas via Resources");
                FindTablesViaResources();
            }
            
            // F11 - Investigar painel Report
            if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F11 - Investigar painel Report");
                Log.LogInfo(">>> F11 - Investigar painel Report");
                InvestigateReportPanel();
            }
            
            // F12 - Testar CustomViewExportData
            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F12 - Testar CustomViewExportData");
                Log.LogInfo(">>> F12 - Testar CustomViewExportData");
                TestCustomViewExportData();
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                _panelManagerType = Type.GetType("SI.Bindable.PanelManager, SI.Bindable");
                Log.LogInfo($"[Init] PanelManager: {(_panelManagerType != null ? "OK" : "NÃO ENCONTRADO")}");
                
                _streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                Log.LogInfo($"[Init] StreamedTable: {(_streamedTableType != null ? "OK" : "NÃO ENCONTRADO")}");
                
                _streamedListViewType = Type.GetType("SI.Bindable.StreamedListView, SI.Bindable");
                Log.LogInfo($"[Init] StreamedListView: {(_streamedListViewType != null ? "OK" : "NÃO ENCONTRADO")}");
                
                _streamedObjectListType = Type.GetType("SI.Bindable.StreamedObjectList, SI.Bindable");
                Log.LogInfo($"[Init] StreamedObjectList: {(_streamedObjectListType != null ? "OK" : "NÃO ENCONTRADO")}");
                
                _customViewExportDataType = Type.GetType("SI.Bindable.CustomViewExportData, SI.Bindable");
                Log.LogInfo($"[Init] CustomViewExportData: {(_customViewExportDataType != null ? "OK" : "NÃO ENCONTRADO")}");
                
                _bindableExportDataType = Type.GetType("SI.Bindable.BindableProjectConfiguration+BindableExportData, SI.Bindable");
                Log.LogInfo($"[Init] BindableExportData: {(_bindableExportDataType != null ? "OK" : "NÃO ENCONTRADO")}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
        
        // NOVO: Buscar tabelas via Resources.FindObjectsOfTypeAll
        private static void FindTablesViaResources()
        {
            try
            {
                Log.LogInfo("[Resources] Buscando objetos em runtime...");
                
                // Método 1: Buscar VisualElements ativos
                var allVisualElements = Resources.FindObjectsOfTypeAll<VisualElement>();
                Log.LogInfo($"[Resources] Total VisualElements: {allVisualElements.Length}");
                
                int tableCount = 0;
                int listCount = 0;
                
                foreach (var ve in allVisualElements)
                {
                    if (ve == null) continue;
                    
                    var veType = ve.GetType();
                    
                    // Verificar se é StreamedTable
                    if (_streamedTableType != null && _streamedTableType.IsAssignableFrom(veType))
                    {
                        tableCount++;
                        Debug.Log($"[FM26CtrlP] TABLE ENCONTRADA: {ve.name} ({veType.Name})");
                        Log.LogInfo($"[Resources] TABLE: {ve.name} ({veType.Name})");
                        
                        // Listar propriedades
                        DumpObjectInfo(ve, "TABLE");
                    }
                    
                    // Verificar se é StreamedListView
                    if (_streamedListViewType != null && _streamedListViewType.IsAssignableFrom(veType))
                    {
                        listCount++;
                        Debug.Log($"[FM26CtrlP] LIST ENCONTRADA: {ve.name} ({veType.Name})");
                        Log.LogInfo($"[Resources] LIST: {ve.name} ({veType.Name})");
                    }
                    
                    // Verificar se é StreamedObjectList
                    if (_streamedObjectListType != null && _streamedObjectListType.IsAssignableFrom(veType))
                    {
                        listCount++;
                        Debug.Log($"[FM26CtrlP] OBJECT LIST ENCONTRADA: {ve.name} ({veType.Name})");
                        Log.LogInfo($"[Resources] OBJECT LIST: {ve.name} ({veType.Name})");
                    }
                }
                
                Log.LogInfo($"[Resources] RESUMO: {tableCount} tabelas, {listCount} listas");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Resources] Erro: {ex.Message}");
                Debug.LogError($"[FM26CtrlP] Erro: {ex.Message}");
            }
        }
        
        // NOVO: Investigar painel Report do PanelManager
        private static void InvestigateReportPanel()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Report] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    // Procurar PanelManager
                    if (doc.name == "PanelManager" || root.name == "PanelManager")
                    {
                        Log.LogInfo($"[Report] PanelManager encontrado!");
                        
                        // Navegar nos filhos
                        for (int i = 0; i < root.childCount; i++)
                        {
                            var child = root[i];
                            if (child == null) continue;
                            
                            Debug.Log($"[FM26CtrlP] [{i}] {child.name} ({child.GetType().Name})");
                            Log.LogInfo($"[Report] [{i}] {child.name} ({child.GetType().Name})");
                            
                            // Se for Report, investigar filhos
                            if (child.name == "Report")
                            {
                                Log.LogInfo($"[Report] === INVESTIGANDO REPORT ===");
                                InvestigateVisualElement(child, 0, 5);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Report] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateVisualElement(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            string indent = new string(' ', depth * 2);
            var elemType = element.GetType();
            
            Debug.Log($"[FM26CtrlP] {indent}- {element.name} ({elemType.Name})");
            Log.LogInfo($"[Report] {indent}- {element.name} ({elemType.Name})");
            
            // Verificar se é um tipo de tabela/lista
            if (_streamedTableType != null && _streamedTableType.IsAssignableFrom(elemType))
            {
                Log.LogInfo($"[Report] {indent}>>> É STREAMED TABLE!");
            }
            if (_streamedListViewType != null && _streamedListViewType.IsAssignableFrom(elemType))
            {
                Log.LogInfo($"[Report] {indent}>>> É STREAMED LIST VIEW!");
            }
            
            // Recursão nos filhos
            for (int i = 0; i < element.childCount; i++)
            {
                InvestigateVisualElement(element[i], depth + 1, maxDepth);
            }
        }
        
        // NOVO: Testar CustomViewExportData
        private static void TestCustomViewExportData()
        {
            try
            {
                if (_customViewExportDataType == null)
                {
                    Log.LogError("[ExportData] Tipo não encontrado");
                    return;
                }
                
                Log.LogInfo($"[ExportData] Tipo: {_customViewExportDataType.FullName}");
                
                // Listar métodos
                var methods = _customViewExportDataType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                Log.LogInfo($"[ExportData] {methods.Length} métodos:");
                foreach (var m in methods)
                {
                    if (!m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
                    {
                        Log.LogInfo($"[ExportData] - {m.Name}");
                    }
                }
                
                // Listar propriedades
                var props = _customViewExportDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                Log.LogInfo($"[ExportData] {props.Length} propriedades:");
                foreach (var p in props)
                {
                    Log.LogInfo($"[ExportData] - {p.Name} ({p.PropertyType.Name})");
                }
                
                // Listar campos
                var fields = _customViewExportDataType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                Log.LogInfo($"[ExportData] {fields.Length} campos:");
                foreach (var f in fields)
                {
                    Log.LogInfo($"[ExportData] - {f.Name} ({f.FieldType.Name})");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[ExportData] Erro: {ex.Message}");
            }
        }
        
        // EXPORTAR - Nova estratégia
        private static void DoExport()
        {
            try
            {
                Log.LogInfo("[Export] Iniciando exportação...");
                
                // Passo 1: Buscar tabelas via Resources
                var allVisualElements = Resources.FindObjectsOfTypeAll<VisualElement>();
                
                List<object> foundTables = new List<object>();
                List<object> foundLists = new List<object>();
                
                foreach (var ve in allVisualElements)
                {
                    if (ve == null) continue;
                    var veType = ve.GetType();
                    
                    if (_streamedTableType != null && _streamedTableType.IsAssignableFrom(veType))
                    {
                        foundTables.Add(ve);
                    }
                    if (_streamedListViewType != null && _streamedListViewType.IsAssignableFrom(veType))
                    {
                        foundLists.Add(ve);
                    }
                    if (_streamedObjectListType != null && _streamedObjectListType.IsAssignableFrom(veType))
                    {
                        foundLists.Add(ve);
                    }
                }
                
                Log.LogInfo($"[Export] Encontrados: {foundTables.Count} tabelas, {foundLists.Count} listas");
                
                // Passo 2: Se encontrou tabelas, tentar exportar
                foreach (var table in foundTables)
                {
                    var ve = table as VisualElement;
                    if (ve == null) continue;
                    
                    Debug.Log($"[FM26CtrlP] Processando tabela: {ve.name}");
                    Log.LogInfo($"[Export] Processando: {ve.name}");
                    
                    // Tentar extrair dados
                    ExtractDataFromTable(table);
                }
                
                // Passo 3: Se encontrou listas, tentar exportar
                foreach (var list in foundLists)
                {
                    var ve = list as VisualElement;
                    if (ve == null) continue;
                    
                    Debug.Log($"[FM26CtrlP] Processando lista: {ve.name}");
                    Log.LogInfo($"[Export] Processando: {ve.name}");
                    
                    ExtractDataFromList(list);
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
                Debug.LogError($"[FM26CtrlP] Erro: {ex.Message}");
            }
        }
        
        private static void ExtractDataFromTable(object table)
        {
            try
            {
                var tableType = table.GetType();
                Log.LogInfo($"[Extract] Tipo: {tableType.Name}");
                
                // Buscar propriedades que podem conter dados
                var props = tableType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var prop in props)
                {
                    try
                    {
                        var propName = prop.Name.ToLower();
                        
                        // Propriedades interessantes
                        if (propName.Contains("item") || propName.Contains("data") || 
                            propName.Contains("row") || propName.Contains("column") ||
                            propName.Contains("source") || propName.Contains("list"))
                        {
                            Log.LogInfo($"[Extract] Prop: {prop.Name} ({prop.PropertyType.Name})");
                            
                            // Tentar obter valor
                            var value = prop.GetValue(table);
                            if (value != null)
                            {
                                Debug.Log($"[FM26CtrlP] Valor: {value}");
                                Log.LogInfo($"[Extract] Valor tipo: {value.GetType().Name}");
                                
                                // Se é uma lista/coleção, mostrar count
                                var valueType = value.GetType();
                                if (valueType.IsGenericType || valueType.IsArray)
                                {
                                    var countProp = valueType.GetProperty("Count");
                                    if (countProp != null)
                                    {
                                        var count = countProp.GetValue(value);
                                        Log.LogInfo($"[Extract] Count: {count}");
                                    }
                                    else if (valueType.IsArray)
                                    {
                                        var arr = value as Array;
                                        Log.LogInfo($"[Extract] Length: {arr?.Length}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ignorar erros individuais de propriedade
                    }
                }
                
                // Buscar campos também
                var fields = tableType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    try
                    {
                        var fieldName = field.Name.ToLower();
                        
                        if (fieldName.Contains("item") || fieldName.Contains("data") || 
                            fieldName.Contains("row") || fieldName.Contains("source"))
                        {
                            Log.LogInfo($"[Extract] Field: {field.Name} ({field.FieldType.Name})");
                            
                            var value = field.GetValue(table);
                            if (value != null)
                            {
                                Debug.Log($"[FM26CtrlP] Field valor: {value.GetType().Name}");
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Extract] Erro: {ex.Message}");
            }
        }
        
        private static void ExtractDataFromList(object list)
        {
            try
            {
                var listType = list.GetType();
                Log.LogInfo($"[ExtractList] Tipo: {listType.Name}");
                
                var props = listType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var prop in props)
                {
                    try
                    {
                        var propName = prop.Name.ToLower();
                        
                        if (propName.Contains("item") || propName.Contains("data") || 
                            propName.Contains("source") || propName.Contains("element"))
                        {
                            Log.LogInfo($"[ExtractList] Prop: {prop.Name} ({prop.PropertyType.Name})");
                            
                            var value = prop.GetValue(list);
                            if (value != null)
                            {
                                Debug.Log($"[FM26CtrlP] List valor: {value.GetType().Name}");
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[ExtractList] Erro: {ex.Message}");
            }
        }
        
        private static void DumpObjectInfo(object obj, string prefix)
        {
            try
            {
                var type = obj.GetType();
                Log.LogInfo($"[{prefix}] Tipo: {type.FullName}");
                
                // Propriedades importantes
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (prop.Name.Length <= 20 && prop.GetIndexParameters().Length == 0)
                    {
                        try
                        {
                            var value = prop.GetValue(obj);
                            var valueStr = value?.ToString() ?? "null";
                            if (valueStr.Length > 50) valueStr = valueStr.Substring(0, 50) + "...";
                            
                            Debug.Log($"[FM26CtrlP] {prop.Name} = {valueStr}");
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }
}
