using System;
using System.Reflection;
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
            
            // F10 - Buscar tabelas navegando pelos UIDocuments
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Buscar tabelas via UIDocuments");
                Log.LogInfo(">>> F10 - Buscar tabelas via UIDocuments");
                FindTablesViaUIDocuments();
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
            }
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
        
        // Buscar tabelas navegando pelos UIDocuments recursivamente
        private static void FindTablesViaUIDocuments()
        {
            try
            {
                Log.LogInfo("[UIDocs] Buscando tabelas via UIDocuments...");
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[UIDocs] {uiDocs.Length} UIDocuments encontrados");
                
                int totalTables = 0;
                int totalLists = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    Log.LogInfo($"[UIDocs] Documento: {doc.name}");
                    
                    // Navegar recursivamente
                    var (tables, lists) = ScanVisualElementRecursive(root, 0, 15);
                    totalTables += tables;
                    totalLists += lists;
                }
                
                Log.LogInfo($"[UIDocs] RESUMO: {totalTables} tabelas, {totalLists} listas");
            }
            catch (Exception ex)
            {
                Log.LogError($"[UIDocs] Erro: {ex.Message}");
            }
        }
        
        private static (int tables, int lists) ScanVisualElementRecursive(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return (0, 0);
            
            int tables = 0;
            int lists = 0;
            
            var elemType = element.GetType();
            
            // Verificar se é StreamedTable
            if (_streamedTableType != null && _streamedTableType.IsAssignableFrom(elemType))
            {
                tables++;
                var indent = new string(' ', depth * 2);
                Debug.Log($"[FM26CtrlP] {indent}TABLE: {element.name} ({elemType.Name})");
                Log.LogInfo($"[Scan] {indent}TABLE: {element.name} ({elemType.Name})");
                
                // Extrair dados da tabela
                ExtractDataFromTable(element);
            }
            
            // Verificar se é StreamedListView ou StreamedObjectList
            if ((_streamedListViewType != null && _streamedListViewType.IsAssignableFrom(elemType)) ||
                (_streamedObjectListType != null && _streamedObjectListType.IsAssignableFrom(elemType)))
            {
                lists++;
                var indent = new string(' ', depth * 2);
                Debug.Log($"[FM26CtrlP] {indent}LIST: {element.name} ({elemType.Name})");
                Log.LogInfo($"[Scan] {indent}LIST: {element.name} ({elemType.Name})");
            }
            
            // Recursão nos filhos
            for (int i = 0; i < element.childCount; i++)
            {
                var child = element[i];
                var (childTables, childLists) = ScanVisualElementRecursive(child, depth + 1, maxDepth);
                tables += childTables;
                lists += childLists;
            }
            
            return (tables, lists);
        }
        
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
                                ScanVisualElementRecursive(child, 0, 10);
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
                Log.LogInfo($"[ExportData] Métodos:");
                foreach (var m in methods)
                {
                    if (!m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
                    {
                        Log.LogInfo($"[ExportData] - {m.Name}");
                    }
                }
                
                // Listar propriedades
                var props = _customViewExportDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                Log.LogInfo($"[ExportData] Propriedades:");
                foreach (var p in props)
                {
                    Log.LogInfo($"[ExportData] - {p.Name} ({p.PropertyType.Name})");
                }
                
                // Listar campos
                var fields = _customViewExportDataType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                Log.LogInfo($"[ExportData] Campos:");
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
        
        private static void DoExport()
        {
            try
            {
                Log.LogInfo("[Export] Iniciando exportação...");
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Export] {uiDocs.Length} UIDocuments");
                
                int totalExported = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    totalExported += ExportFromVisualElement(root, 0, 15);
                }
                
                Log.LogInfo($"[Export] Total exportado: {totalExported}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static int ExportFromVisualElement(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return 0;
            
            int exported = 0;
            var elemType = element.GetType();
            
            // Se é StreamedTable, tentar exportar
            if (_streamedTableType != null && _streamedTableType.IsAssignableFrom(elemType))
            {
                Log.LogInfo($"[Export] Encontrada tabela: {element.name}");
                ExportTable(element);
                exported++;
            }
            
            // Recursão nos filhos
            for (int i = 0; i < element.childCount; i++)
            {
                exported += ExportFromVisualElement(element[i], depth + 1, maxDepth);
            }
            
            return exported;
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
                            propName.Contains("source") || propName.Contains("list") ||
                            propName.Contains("count"))
                        {
                            if (prop.GetIndexParameters().Length > 0) continue; // Pular indexers
                            
                            var value = prop.GetValue(table);
                            if (value != null)
                            {
                                Log.LogInfo($"[Extract] {prop.Name} = {value.GetType().Name}");
                            }
                        }
                    }
                    catch
                    {
                        // Ignorar erros individuais
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Extract] Erro: {ex.Message}");
            }
        }
        
        private static void ExportTable(object table)
        {
            try
            {
                var tableType = table.GetType();
                Log.LogInfo($"[ExportTable] Tipo: {tableType.FullName}");
                
                // Listar todos os métodos públicos
                var methods = tableType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[ExportTable] Métodos públicos:");
                foreach (var m in methods)
                {
                    if (!m.Name.StartsWith("get_") && !m.Name.StartsWith("set_") && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_"))
                    {
                        Log.LogInfo($"[ExportTable] - {m.Name}");
                    }
                }
                
                // Listar todas as propriedades
                var props = tableType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[ExportTable] Propriedades:");
                foreach (var p in props)
                {
                    Log.LogInfo($"[ExportTable] - {p.Name} ({p.PropertyType.Name})");
                }
                
                // Listar todos os campos
                var fields = tableType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[ExportTable] Campos:");
                foreach (var f in fields)
                {
                    Log.LogInfo($"[ExportTable] - {f.Name} ({f.FieldType.Name})");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[ExportTable] Erro: {ex.Message}");
            }
        }
    }
}
