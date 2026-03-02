using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.2.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.2.0 CARREGADO!");
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
            
            // F9 - DUMP COMPLETO DO REPORT COM PROPRIEDADES (NOVO!)
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F9 - DUMP COMPLETO DO REPORT COM DADOS");
                Log.LogInfo(">>> F9 - DUMP COMPLETO DO REPORT COM DADOS");
                DumpReportWithData();
            }
            
            // F10 - Buscar tabelas
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
        
        // NOVO: Dump do Report com investigação de dados
        private static void DumpReportWithData()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Dump] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    if (doc.name == "PanelManager" || root.name == "PanelManager")
                    {
                        Log.LogInfo($"[Dump] === PanelManager encontrado ===");
                        
                        for (int i = 0; i < root.childCount; i++)
                        {
                            var child = root[i];
                            if (child == null) continue;
                            
                            if (child.name == "Report")
                            {
                                Log.LogInfo($"[Dump] === REPORT ENCONTRADO ===");
                                InvestigateReportWithData(child);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Dump] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateReportWithData(VisualElement reportElement)
        {
            try
            {
                Log.LogInfo($"[ReportData] Report: children={reportElement.childCount}");
                
                // Navegar no Body
                for (int i = 0; i < reportElement.childCount; i++)
                {
                    var child = reportElement[i];
                    if (child == null) continue;
                    
                    Debug.Log($"[FM26CtrlP] Report[{i}]: {child.name}");
                    Log.LogInfo($"[ReportData] Report[{i}]: {child.name}");
                    
                    // Se for Body, investigar mais
                    if (child.name == "Body")
                    {
                        InvestigateBodyElement(child);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[ReportData] Erro: {ex.Message}");
            }
        }
        
        private static void InvestigateBodyElement(VisualElement bodyElement)
        {
            try
            {
                Log.LogInfo($"[Body] Body: children={bodyElement.childCount}");
                
                for (int i = 0; i < bodyElement.childCount; i++)
                {
                    var child = bodyElement[i];
                    if (child == null) continue;
                    
                    var childType = child.GetType();
                    Debug.Log($"[FM26CtrlP] Body[{i}]: {child.name} ({childType.Name})");
                    Log.LogInfo($"[Body] Body[{i}]: {child.name} ({childType.Name})");
                    
                    // Verificar se é um Report específico
                    if (child.name.Contains("Report") || child.name.Contains("Search") || child.name.Contains("Squad"))
                    {
                        Log.LogInfo($"[Body] === INVESTIGANDO {child.name} ===");
                        DumpElementFull(child, 0, 3);
                    }
                    
                    // Navegar recursivamente procurando por elementos com dados
                    FindDataElements(child, 0, 10);
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Body] Erro: {ex.Message}");
            }
        }
        
        private static void FindDataElements(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            var elemType = element.GetType();
            string elemName = element.name ?? "";
            
            // Verificar se o elemento tem dados interessantes
            bool hasInterestingName = 
                elemName.Contains("Player") || 
                elemName.Contains("Squad") || 
                elemName.Contains("Team") ||
                elemName.Contains("List") ||
                elemName.Contains("Table") ||
                elemName.Contains("Grid") ||
                elemName.Contains("Data") ||
                elemName.Contains("Item") ||
                elemName.Contains("Row") ||
                elemName.Contains("Column");
            
            bool hasInterestingType = 
                elemType.Name.Contains("Streamed") ||
                elemType.Name.Contains("List") ||
                elemType.Name.Contains("Table") ||
                elemType.Name.Contains("Grid") ||
                elemType.Name.Contains("Data");
            
            if (hasInterestingName || hasInterestingType)
            {
                string indent = new string(' ', depth * 2);
                Log.LogInfo($"[FindData] {indent}>>> {elemName} ({elemType.Name})");
                DumpElementFull(element, depth, 2);
            }
            
            // Recursão
            for (int i = 0; i < element.childCount; i++)
            {
                FindDataElements(element[i], depth + 1, maxDepth);
            }
        }
        
        private static void DumpElementFull(object obj, int depth, int maxDepth)
        {
            if (obj == null) return;
            
            try
            {
                var objType = obj.GetType();
                string indent = new string(' ', depth * 2);
                
                Log.LogInfo($"{indent}[{objType.Name}] ========================");
                
                // Listar TODAS as propriedades
                var props = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"{indent}Propriedades ({props.Length}):");
                
                foreach (var prop in props)
                {
                    try
                    {
                        // Pular indexadores
                        if (prop.GetIndexParameters().Length > 0) continue;
                        
                        var propType = prop.PropertyType;
                        string propInfo = $"{indent}  {prop.Name}: {propType.Name}";
                        
                        // Tentar obter valor
                        object value = null;
                        try
                        {
                            value = prop.GetValue(obj);
                        }
                        catch { }
                        
                        if (value != null)
                        {
                            // Se for lista/coleção, mostrar count
                            if (propType.IsGenericType || propType.IsArray)
                            {
                                var countProp = propType.GetProperty("Count");
                                if (countProp != null)
                                {
                                    var count = countProp.GetValue(value);
                                    propInfo += $" (Count={count})";
                                    Debug.Log($"[FM26CtrlP] {propInfo}");
                                }
                                else if (propType.IsArray && value is Array arr)
                                {
                                    propInfo += $" (Length={arr.Length})";
                                    Debug.Log($"[FM26CtrlP] {propInfo}");
                                }
                            }
                            else if (propType.IsPrimitive || propType == typeof(string))
                            {
                                string valStr = value.ToString();
                                if (valStr.Length > 50) valStr = valStr.Substring(0, 50) + "...";
                                propInfo += $" = {valStr}";
                            }
                            else
                            {
                                propInfo += $" [instance]";
                            }
                        }
                        else
                        {
                            propInfo += " = null";
                        }
                        
                        Log.LogInfo(propInfo);
                    }
                    catch (Exception ex)
                    {
                        Log.LogInfo($"{indent}  {prop.Name}: ERRO - {ex.Message}");
                    }
                }
                
                // Listar TODOS os campos
                var fields = objType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"{indent}Campos ({fields.Length}):");
                
                foreach (var field in fields)
                {
                    try
                    {
                        var fieldType = field.FieldType;
                        string fieldInfo = $"{indent}  {field.Name}: {fieldType.Name}";
                        
                        object value = null;
                        try
                        {
                            value = field.GetValue(obj);
                        }
                        catch { }
                        
                        if (value != null)
                        {
                            if (fieldType.IsGenericType || fieldType.IsArray)
                            {
                                var countProp = fieldType.GetProperty("Count");
                                if (countProp != null)
                                {
                                    var count = countProp.GetValue(value);
                                    fieldInfo += $" (Count={count})";
                                }
                            }
                            else if (fieldType.IsPrimitive || fieldType == typeof(string))
                            {
                                string valStr = value.ToString();
                                if (valStr.Length > 50) valStr = valStr.Substring(0, 50) + "...";
                                fieldInfo += $" = {valStr}";
                            }
                        }
                        
                        Log.LogInfo(fieldInfo);
                    }
                    catch (Exception ex)
                    {
                        Log.LogInfo($"{indent}  {field.Name}: ERRO - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Dump] Erro: {ex.Message}");
            }
        }
        
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
            
            if (_streamedTableType != null && _streamedTableType.IsAssignableFrom(elemType))
            {
                tables++;
                Log.LogInfo($"[Scan] TABLE: {element.name} ({elemType.Name})");
            }
            
            if ((_streamedListViewType != null && _streamedListViewType.IsAssignableFrom(elemType)) ||
                (_streamedObjectListType != null && _streamedObjectListType.IsAssignableFrom(elemType)))
            {
                lists++;
                Log.LogInfo($"[Scan] LIST: {element.name} ({elemType.Name})");
            }
            
            for (int i = 0; i < element.childCount; i++)
            {
                var (childTables, childLists) = ScanVisualElementRecursive(element[i], depth + 1, maxDepth);
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
                    
                    if (doc.name == "PanelManager" || root.name == "PanelManager")
                    {
                        Log.LogInfo($"[Report] PanelManager encontrado!");
                        
                        for (int i = 0; i < root.childCount; i++)
                        {
                            var child = root[i];
                            if (child == null) continue;
                            
                            Debug.Log($"[FM26CtrlP] [{i}] {child.name} ({child.GetType().Name})");
                            Log.LogInfo($"[Report] [{i}] {child.name} ({child.GetType().Name})");
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
                
                var methods = _customViewExportDataType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                Log.LogInfo($"[ExportData] Métodos:");
                foreach (var m in methods)
                {
                    if (!m.Name.StartsWith("get_") && !m.Name.StartsWith("set_"))
                    {
                        Log.LogInfo($"[ExportData] - {m.Name}");
                    }
                }
                
                var props = _customViewExportDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                Log.LogInfo($"[ExportData] Propriedades:");
                foreach (var p in props)
                {
                    Log.LogInfo($"[ExportData] - {p.Name} ({p.PropertyType.Name})");
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
                
                // Buscar dados do Report ativo
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    if (doc.name == "PanelManager" || root.name == "PanelManager")
                    {
                        // Encontrar Report
                        for (int i = 0; i < root.childCount; i++)
                        {
                            var child = root[i];
                            if (child?.name == "Report")
                            {
                                ExportFromReport(child);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void ExportFromReport(VisualElement reportElement)
        {
            try
            {
                Log.LogInfo("[Export] Buscando dados no Report...");
                
                // Navegar no Body
                for (int i = 0; i < reportElement.childCount; i++)
                {
                    var child = reportElement[i];
                    if (child?.name == "Body")
                    {
                        // Investigar Body
                        for (int j = 0; j < child.childCount; j++)
                        {
                            var bodyChild = child[j];
                            if (bodyChild == null) continue;
                            
                            Log.LogInfo($"[Export] Body[{j}]: {bodyChild.name}");
                            
                            // Se for um Report, investigar dados
                            if (bodyChild.name.Contains("Report"))
                            {
                                ExtractDataFromElement(bodyChild);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void ExtractDataFromElement(object element)
        {
            if (element == null) return;
            
            try
            {
                var elemType = element.GetType();
                Log.LogInfo($"[Extract] Tipo: {elemType.FullName}");
                
                // Buscar propriedades que contenham dados
                var props = elemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var prop in props)
                {
                    try
                    {
                        if (prop.GetIndexParameters().Length > 0) continue;
                        
                        var propName = prop.Name.ToLower();
                        var propType = prop.PropertyType;
                        
                        // Propriedades que podem ter dados
                        if (propName.Contains("player") || propName.Contains("data") ||
                            propName.Contains("list") || propName.Contains("item") ||
                            propName.Contains("row") || propName.Contains("source") ||
                            propName.Contains("count") || propName.Contains("squad"))
                        {
                            var value = prop.GetValue(element);
                            if (value != null)
                            {
                                string info = $"[Extract] {prop.Name}: {propType.Name}";
                                
                                if (propType.IsGenericType)
                                {
                                    var countProp = propType.GetProperty("Count");
                                    if (countProp != null)
                                    {
                                        var count = countProp.GetValue(value);
                                        info += $" (Count={count})";
                                        
                                        // Se tem mais de 0 itens, investigar
                                        if (count != null && (int)count > 0)
                                        {
                                            Log.LogInfo(info);
                                            Debug.Log($"[FM26CtrlP] {info}");
                                            
                                            // Tentar pegar primeiro item
                                            var getItemMethod = propType.GetMethod("get_Item");
                                            if (getItemMethod != null)
                                            {
                                                var firstItem = getItemMethod.Invoke(value, new object[] { 0 });
                                                if (firstItem != null)
                                                {
                                                    Log.LogInfo($"[Extract] Primeiro item: {firstItem.GetType().Name}");
                                                    DumpElementFull(firstItem, 0, 1);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Log.LogInfo(info);
                                }
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
    }
}
