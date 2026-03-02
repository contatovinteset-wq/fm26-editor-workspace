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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.1.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v1.1.0 CARREGADO!");
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
        private static Type _panelType;
        private static Type _streamedTableType;
        private static Type _streamedTableViewType;
        private static Type _streamedListViewType;
        private static Type _streamedObjectListType;
        private static Type _streamedTableViewSettingsCollectionType;
        private static Type _customViewExportDataType;
        
        // Métodos
        private static MethodInfo _getOpenPanelMethod;
        private static MethodInfo _tryGetPanelMethod;
        private static MethodInfo _createExportDataMethod;
        
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
            bool shift = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            
            // Ctrl+P - Exportar tabela visível
            if (ctrl && Keyboard.current.pKey.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                Log.LogInfo(">>> Ctrl+P PRESSIONADO!");
                DoExportAllTables();
            }
            
            // F9 - Buscar TODAS as tabelas em runtime (via Resources)
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F9 - Buscar todas as tabelas em runtime");
                Log.LogInfo(">>> F9 - Buscando todas as tabelas via Resources.FindObjectsOfTypeAll");
                FindAllTablesViaResources();
            }
            
            // F10 - Tentar export via CustomViewExportData
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Export via CustomViewExportData");
                Log.LogInfo(">>> F10 - Tentando export via CustomViewExportData");
                TryExportViaCustomView();
            }
            
            // F11 - Buscar tabelas no painel ativo
            if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F11 - Buscando tabelas no painel ativo");
                Log.LogInfo(">>> F11 - Buscando tabelas no painel ativo");
                FindTablesInActivePanel();
            }
            
            // F12 - Listar painéis abertos
            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F12 - Listando painéis abertos");
                Log.LogInfo(">>> F12 - Listando painéis abertos");
                ListOpenPanels();
            }
            
            // Ctrl+Shift+D - Dump completo
            if (ctrl && shift && Keyboard.current.dKey.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+Shift+D - DUMP COMPLETO");
                Log.LogInfo(">>> Ctrl+Shift+D - DUMP COMPLETO");
                FullDiagnostic();
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                // PanelManager
                _panelManagerType = Type.GetType("SI.Bindable.PanelManager, SI.Bindable");
                if (_panelManagerType != null)
                {
                    Log.LogInfo($"[Init] PanelManager encontrado: {_panelManagerType.FullName}");
                    
                    _getOpenPanelMethod = _panelManagerType.GetMethod("GetOpenPanelInHighestLayer", BindingFlags.Public | BindingFlags.Instance);
                    _tryGetPanelMethod = _panelManagerType.GetMethod("TryGetPanel", new Type[] { typeof(uint), _panelManagerType.MakeByRefType() });
                }
                
                // Panel
                _panelType = Type.GetType("SI.Bindable.Panel, SI.Bindable");
                if (_panelType != null)
                {
                    Log.LogInfo($"[Init] Panel encontrado: {_panelType.FullName}");
                }
                
                // StreamedTable
                _streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                if (_streamedTableType != null)
                {
                    Log.LogInfo($"[Init] StreamedTable encontrado");
                }
                
                // StreamedListView
                _streamedListViewType = Type.GetType("SI.Bindable.StreamedListView, SI.Bindable");
                if (_streamedListViewType != null)
                {
                    Log.LogInfo($"[Init] StreamedListView encontrado");
                }
                
                // StreamedObjectList
                _streamedObjectListType = Type.GetType("SI.Bindable.StreamedObjectList, SI.Bindable");
                if (_streamedObjectListType != null)
                {
                    Log.LogInfo($"[Init] StreamedObjectList encontrado");
                }
                
                // StreamedTableView
                _streamedTableViewType = Type.GetType("SI.Bindable.StreamedTableView, SI.Bindable");
                if (_streamedTableViewType != null)
                {
                    Log.LogInfo($"[Init] StreamedTableView encontrado");
                }
                
                // StreamedTableViewSettingsCollection
                _streamedTableViewSettingsCollectionType = Type.GetType("SI.Bindable.StreamedTableViewSettingsCollection, SI.Bindable");
                if (_streamedTableViewSettingsCollectionType != null)
                {
                    Log.LogInfo($"[Init] StreamedTableViewSettingsCollection encontrado");
                    _createExportDataMethod = _streamedTableViewSettingsCollectionType.GetMethod("CreateExportDataFromCustomView", BindingFlags.Public | BindingFlags.Instance);
                    if (_createExportDataMethod != null)
                    {
                        Log.LogInfo($"[Init] CreateExportDataFromCustomView encontrado");
                    }
                }
                
                // CustomViewExportData
                _customViewExportDataType = Type.GetType("SI.Bindable.CustomViewExportData, SI.Bindable");
                if (_customViewExportDataType != null)
                {
                    Log.LogInfo($"[Init] CustomViewExportData encontrado");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Init erro: {ex.Message}");
                Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
        
        private static object GetPanelManagerInstance()
        {
            if (_panelManagerType == null) return null;
            
            try
            {
                var baseType = _panelManagerType.BaseType;
                if (baseType != null && baseType.IsGenericType)
                {
                    var instanceProp = baseType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProp != null)
                    {
                        return instanceProp.GetValue(null);
                    }
                }
                
                var directInstanceProp = _panelManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (directInstanceProp != null)
                {
                    return directInstanceProp.GetValue(null);
                }
                
                var instanceField = _panelManagerType.GetField("s_instance", BindingFlags.NonPublic | BindingFlags.Static);
                if (instanceField != null)
                {
                    return instanceField.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro ao obter PanelManager: {ex.Message}");
            }
            
            return null;
        }
        
        // ============================================
        // NOVO: Buscar TODAS as tabelas via Resources
        // ============================================
        private static void FindAllTablesViaResources()
        {
            try
            {
                Log.LogInfo("[F9] === Buscando TODAS as tabelas em runtime ===");
                
                if (_streamedTableType != null)
                {
                    var tables = Resources.FindObjectsOfTypeAll(_streamedTableType);
                    Log.LogInfo($"[F9] StreamedTable encontrados: {tables.Length}");
                    
                    foreach (var table in tables)
                    {
                        if (table != null)
                        {
                            var ve = table as VisualElement;
                            var name = ve != null ? ve.name : "unnamed";
                            Log.LogInfo($"[F9]   - {table.GetType().Name}: {name}");
                            
                            // Tentar obter dados
                            InspectTable(table);
                        }
                    }
                }
                
                if (_streamedListViewType != null)
                {
                    var listViews = Resources.FindObjectsOfTypeAll(_streamedListViewType);
                    Log.LogInfo($"[F9] StreamedListView encontrados: {listViews.Length}");
                }
                
                if (_streamedObjectListType != null)
                {
                    var objLists = Resources.FindObjectsOfTypeAll(_streamedObjectListType);
                    Log.LogInfo($"[F9] StreamedObjectList encontrados: {objLists.Length}");
                }
                
                Log.LogInfo("[F9] === Fim da busca ===");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F9] Erro: {ex.Message}");
            }
        }
        
        // ============================================
        // NOVO: Tentar export via CustomViewExportData
        // ============================================
        private static void TryExportViaCustomView()
        {
            try
            {
                Log.LogInfo("[F10] === Tentando export via CustomViewExportData ===");
                
                // Buscar todas as instâncias de StreamedTableViewSettingsCollection
                if (_streamedTableViewSettingsCollectionType != null)
                {
                    var settingsCollections = Resources.FindObjectsOfTypeAll(_streamedTableViewSettingsCollectionType);
                    Log.LogInfo($"[F10] StreamedTableViewSettingsCollection encontrados: {settingsCollections.Length}");
                    
                    foreach (var settings in settingsCollections)
                    {
                        if (_createExportDataMethod != null)
                        {
                            Log.LogInfo("[F10] Tentando CreateExportDataFromCustomView...");
                            var result = _createExportDataMethod.Invoke(settings, null);
                            Log.LogInfo($"[F10] Resultado: {result?.GetType().Name ?? "null"}");
                        }
                    }
                }
                
                // Buscar CustomViewExportData diretamente
                if (_customViewExportDataType != null)
                {
                    var exportData = Resources.FindObjectsOfTypeAll(_customViewExportDataType);
                    Log.LogInfo($"[F10] CustomViewExportData encontrados: {exportData.Length}");
                }
                
                Log.LogInfo("[F10] === Fim da tentativa ===");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F10] Erro: {ex.Message}");
            }
        }
        
        // ============================================
        // NOVO: Exportar todas as tabelas (Ctrl+P)
        // ============================================
        private static void DoExportAllTables()
        {
            try
            {
                Log.LogInfo("[Ctrl+P] === EXPORTANDO TODAS AS TABELAS ===");
                
                int totalExported = 0;
                
                // 1. Buscar via Resources (todas as instâncias ativas)
                if (_streamedTableType != null)
                {
                    var tables = Resources.FindObjectsOfTypeAll(_streamedTableType);
                    Log.LogInfo($"[Ctrl+P] StreamedTable ativos: {tables.Length}");
                    
                    foreach (var table in tables)
                    {
                        if (ExportSingleTable(table))
                        {
                            totalExported++;
                        }
                    }
                }
                
                // 2. Buscar StreamedListView também
                if (_streamedListViewType != null)
                {
                    var listViews = Resources.FindObjectsOfTypeAll(_streamedListViewType);
                    Log.LogInfo($"[Ctrl+P] StreamedListView ativos: {listViews.Length}");
                }
                
                Log.LogInfo($"[Ctrl+P] === EXPORTAÇÃO COMPLETA: {totalExported} tabelas ===");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Ctrl+P] Erro: {ex.Message}");
            }
        }
        
        // ============================================
        // NOVO: Inspecionar uma tabela
        // ============================================
        private static void InspectTable(object table)
        {
            try
            {
                var tableType = table.GetType();
                
                // Propriedades relevantes
                var props = tableType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (prop.Name.Contains("Data") || prop.Name.Contains("Items") || prop.Name.Contains("List") || prop.Name.Contains("View"))
                    {
                        try
                        {
                            var value = prop.GetValue(table);
                            Log.LogInfo($"[Inspect]   {prop.Name} = {value?.GetType().Name ?? "null"}");
                        }
                        catch { }
                    }
                }
                
                // Campos relevantes
                var fields = tableType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.Name.Contains("data") || field.Name.Contains("items") || field.Name.Contains("_list"))
                    {
                        try
                        {
                            var value = field.GetValue(table);
                            Log.LogInfo($"[Inspect]   {field.Name} = {value?.GetType().Name ?? "null"}");
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Inspect] Erro: {ex.Message}");
            }
        }
        
        // ============================================
        // NOVO: Exportar tabela individual
        // ============================================
        private static bool ExportSingleTable(object table)
        {
            try
            {
                var tableType = table.GetType();
                var ve = table as VisualElement;
                var name = ve != null ? ve.name : tableType.Name;
                
                Log.LogInfo($"[Export] Exportando: {name}");
                
                // Tentar encontrar método GetData ou similar
                var getDataMethod = tableType.GetMethod("GetData", BindingFlags.Public | BindingFlags.Instance);
                if (getDataMethod != null)
                {
                    var data = getDataMethod.Invoke(table, null);
                    Log.LogInfo($"[Export]   GetData() = {data?.GetType().Name ?? "null"}");
                    return true;
                }
                
                // Tentar propriedade Data
                var dataProp = tableType.GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
                if (dataProp != null)
                {
                    var data = dataProp.GetValue(table);
                    Log.LogInfo($"[Export]   Data = {data?.GetType().Name ?? "null"}");
                    return true;
                }
                
                // Tentar propriedade Items
                var itemsProp = tableType.GetProperty("Items", BindingFlags.Public | BindingFlags.Instance);
                if (itemsProp != null)
                {
                    var items = itemsProp.GetValue(table);
                    Log.LogInfo($"[Export]   Items = {items?.GetType().Name ?? "null"}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
                return false;
            }
        }
        
        // ============================================
        // NOVO: Diagnóstico completo
        // ============================================
        private static void FullDiagnostic()
        {
            try
            {
                Log.LogInfo("[Diag] === DIAGNÓSTICO COMPLETO ===");
                
                // 1. Assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Log.LogInfo($"[Diag] Assemblies carregados: {assemblies.Length}");
                
                int siBindableCount = 0;
                int fmUiCount = 0;
                
                foreach (var asm in assemblies)
                {
                    var name = asm.GetName().Name;
                    if (name.StartsWith("SI.Bindable")) siBindableCount++;
                    if (name.StartsWith("FM.UI")) fmUiCount++;
                }
                
                Log.LogInfo($"[Diag] SI.Bindable*: {siBindableCount}");
                Log.LogInfo($"[Diag] FM.UI*: {fmUiCount}");
                
                // 2. Todos os tipos export
                var allTypes = new List<Type>();
                foreach (var asm in assemblies)
                {
                    try
                    {
                        allTypes.AddRange(asm.GetTypes());
                    }
                    catch { }
                }
                
                var exportTypes = allTypes.FindAll(t => t.Name.Contains("Export"));
                Log.LogInfo($"[Diag] Tipos com 'Export' no nome: {exportTypes.Count}");
                foreach (var t in exportTypes)
                {
                    Log.LogInfo($"[Diag]   - {t.FullName}");
                }
                
                Log.LogInfo("[Diag] === FIM DO DIAGNÓSTICO ===");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Diag] Erro: {ex.Message}");
            }
        }
        
        // ============================================
        // MÉTODOS EXISTENTES
        // ============================================
        private static void ListOpenPanels()
        {
            try
            {
                var panelManager = GetPanelManagerInstance();
                if (panelManager == null)
                {
                    Log.LogError("[Panels] PanelManager não encontrado");
                    return;
                }
                
                var panelsProp = _panelManagerType.GetProperty("Panels", BindingFlags.Public | BindingFlags.Instance);
                if (panelsProp != null)
                {
                    var panels = panelsProp.GetValue(panelManager) as System.Collections.IList;
                    if (panels != null)
                    {
                        Log.LogInfo($"[Panels] {panels.Count} painéis na lista");
                    }
                }
                
                if (_getOpenPanelMethod != null)
                {
                    var openPanel = _getOpenPanelMethod.Invoke(panelManager, new object[] { 9, null });
                    if (openPanel != null)
                    {
                        Log.LogInfo($"[Panels] Painel ativo: {openPanel.GetType().Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Panels] Erro: {ex.Message}");
            }
        }
        
        private static void FindTablesInActivePanel()
        {
            try
            {
                var panelManager = GetPanelManagerInstance();
                if (panelManager == null)
                {
                    Log.LogError("[Tables] PanelManager não encontrado");
                    return;
                }
                
                if (_getOpenPanelMethod == null) return;
                
                var activePanel = _getOpenPanelMethod.Invoke(panelManager, new object[] { 9, null });
                if (activePanel == null)
                {
                    Log.LogInfo("[Tables] Nenhum painel ativo");
                    return;
                }
                
                var panelElement = activePanel as VisualElement;
                if (panelElement == null) return;
                
                var tables = FindVisualElementsOfType(panelElement, _streamedTableType);
                Log.LogInfo($"[Tables] {tables.Count} StreamedTables no painel ativo");
                
                foreach (var t in tables)
                {
                    InspectTable(t);
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Tables] Erro: {ex.Message}");
            }
        }
        
        private static List<object> FindVisualElementsOfType(VisualElement root, Type targetType)
        {
            var result = new List<object>();
            if (root == null || targetType == null) return result;
            FindVisualElementsRecursive(root, targetType, result, 0, 50);
            return result;
        }
        
        private static void FindVisualElementsRecursive(VisualElement element, Type targetType, List<object> result, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var elementType = element.GetType();
                if (elementType == targetType || targetType.IsAssignableFrom(elementType))
                {
                    result.Add(element);
                }
                
                int childCount = element.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = element[i];
                    if (child != null)
                    {
                        FindVisualElementsRecursive(child, targetType, result, depth + 1, maxDepth);
                    }
                }
            }
            catch { }
        }
    }
}