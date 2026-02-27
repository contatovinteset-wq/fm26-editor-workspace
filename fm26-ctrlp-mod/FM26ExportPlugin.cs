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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
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
        private static Type _streamedTableViewSettingsCollectionType;
        
        // Métodos
        private static MethodInfo _getOpenPanelMethod;
        private static MethodInfo _tryGetPanelMethod;
        private static MethodInfo _getRootVisualElementMethod;
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
            bool p = Keyboard.current.pKey.wasPressedThisFrame;
            
            if (ctrl && p)
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                Log.LogInfo(">>> Ctrl+P PRESSIONADO!");
                DoExportViaPanelManager();
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
                    
                    // Singleton instance
                    var instanceProp = _panelManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProp == null)
                    {
                        // Tentar get_GetInstance
                        var getInstanceMethod = _panelManagerType.GetMethod("get_Instance", BindingFlags.Public | BindingFlags.Static);
                        if (getInstanceMethod != null)
                        {
                            Log.LogInfo("[Init] get_Instance encontrado");
                        }
                    }
                    else
                    {
                        Log.LogInfo("[Init] Instance property encontrada");
                    }
                    
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
                // ManualSingleton<T>.Instance
                var baseType = _panelManagerType.BaseType;
                if (baseType != null && baseType.IsGenericType)
                {
                    var instanceProp = baseType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProp != null)
                    {
                        return instanceProp.GetValue(null);
                    }
                }
                
                // Tentar property direta
                var directInstanceProp = _panelManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (directInstanceProp != null)
                {
                    return directInstanceProp.GetValue(null);
                }
                
                // Tentar campo
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
                
                // Obter lista de painéis
                var panelsProp = _panelManagerType.GetProperty("Panels", BindingFlags.Public | BindingFlags.Instance);
                if (panelsProp != null)
                {
                    var panels = panelsProp.GetValue(panelManager) as System.Collections.IList;
                    if (panels != null)
                    {
                        Log.LogInfo($"[Panels] {panels.Count} painéis na lista");
                        
                        foreach (var panelId in panels)
                        {
                            Debug.Log($"[FM26CtrlP] PanelID: {panelId}");
                            Log.LogInfo($"[Panels] PanelID: {panelId}");
                        }
                    }
                }
                
                // Tentar GetOpenPanelInHighestLayer
                if (_getOpenPanelMethod != null)
                {
                    var openPanel = _getOpenPanelMethod.Invoke(panelManager, new object[] { 9, null });
                    if (openPanel != null)
                    {
                        Debug.Log($"[FM26CtrlP] Painel aberto: {openPanel.GetType().Name}");
                        Log.LogInfo($"[Panels] Painel aberto no topo: {openPanel.GetType().Name}");
                        
                        // Obter PanelID
                        var panelIdProp = _panelType?.GetProperty("PanelID", BindingFlags.Public | BindingFlags.Instance);
                        if (panelIdProp != null)
                        {
                            var panelId = panelIdProp.GetValue(openPanel);
                            Debug.Log($"[FM26CtrlP] PanelID: {panelId}");
                            Log.LogInfo($"[Panels] PanelID: {panelId}");
                        }
                    }
                    else
                    {
                        Log.LogInfo("[Panels] Nenhum painel aberto no topo");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Panels] Erro: {ex.Message}");
                Debug.LogError($"[FM26CtrlP] Erro: {ex.Message}");
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
                
                // Obter painel ativo
                if (_getOpenPanelMethod == null)
                {
                    Log.LogError("[Tables] Método GetOpenPanelInHighestLayer não encontrado");
                    return;
                }
                
                var activePanel = _getOpenPanelMethod.Invoke(panelManager, new object[] { 9, null });
                if (activePanel == null)
                {
                    Log.LogInfo("[Tables] Nenhum painel ativo");
                    return;
                }
                
                Debug.Log($"[FM26CtrlP] Painel ativo: {activePanel.GetType().Name}");
                Log.LogInfo($"[Tables] Painel ativo: {activePanel.GetType().Name}");
                
                // Panel é VisualElement, podemos buscar filhos
                var panelElement = activePanel as VisualElement;
                if (panelElement == null)
                {
                    Log.LogError("[Tables] Painel não é VisualElement");
                    return;
                }
                
                // Buscar tabelas
                var tables = FindVisualElementsOfType(panelElement, _streamedTableType);
                var listViews = FindVisualElementsOfType(panelElement, _streamedListViewType);
                
                Log.LogInfo($"[Tables] {tables.Count} StreamedTables encontrados");
                Log.LogInfo($"[Tables] {listViews.Count} StreamedListViews encontrados");
                
                foreach (var table in tables)
                {
                    var ve = table as VisualElement;
                    if (ve != null)
                    {
                        Debug.Log($"[FM26CtrlP] StreamedTable: {ve.name}");
                        Log.LogInfo($"[Tables] StreamedTable: {ve.name}");
                    }
                }
                
                // Buscar em todos os painéis do PanelManager
                Log.LogInfo("[Tables] Buscando em todos os painéis...");
                
                var panelsProp = _panelManagerType.GetProperty("Panels", BindingFlags.Public | BindingFlags.Instance);
                if (panelsProp != null)
                {
                    var panels = panelsProp.GetValue(panelManager) as System.Collections.IList;
                    if (panels != null)
                    {
                        foreach (var panelId in panels)
                        {
                            // TryGetPanel(uint id, out Panel panel)
                            if (_tryGetPanelMethod != null)
                            {
                                var parameters = new object[] { panelId, null };
                                var found = (bool)_tryGetPanelMethod.Invoke(panelManager, parameters);
                                if (found && parameters[1] != null)
                                {
                                    var panel = parameters[1] as VisualElement;
                                    if (panel != null)
                                    {
                                        var panelTables = FindVisualElementsOfType(panel, _streamedTableType);
                                        if (panelTables.Count > 0)
                                        {
                                            Log.LogInfo($"[Tables] {panelTables.Count} tabelas no painel {panelId}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Tables] Erro: {ex.Message}");
                Debug.LogError($"[FM26CtrlP] Erro: {ex.Message}");
            }
        }
        
        private static void DoExportViaPanelManager()
        {
            try
            {
                var panelManager = GetPanelManagerInstance();
                if (panelManager == null)
                {
                    Log.LogError("[Export] PanelManager não encontrado");
                    return;
                }
                
                // Obter painel ativo
                if (_getOpenPanelMethod == null)
                {
                    Log.LogError("[Export] Método GetOpenPanelInHighestLayer não encontrado");
                    return;
                }
                
                var activePanel = _getOpenPanelMethod.Invoke(panelManager, new object[] { 9, null });
                if (activePanel == null)
                {
                    Log.LogInfo("[Export] Nenhum painel ativo");
                    return;
                }
                
                var panelElement = activePanel as VisualElement;
                if (panelElement == null)
                {
                    Log.LogError("[Export] Painel não é VisualElement");
                    return;
                }
                
                // Buscar StreamedTable
                var tables = FindVisualElementsOfType(panelElement, _streamedTableType);
                Log.LogInfo($"[Export] {tables.Count} StreamedTables encontrados no painel ativo");
                
                if (tables.Count == 0)
                {
                    // Buscar em todos os UIDocuments também
                    var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                    foreach (var doc in uiDocs)
                    {
                        if (doc == null) continue;
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        var docTables = FindVisualElementsOfType(root, _streamedTableType);
                        tables.AddRange(docTables);
                    }
                    Log.LogInfo($"[Export] Total após busca em UIDocuments: {tables.Count}");
                }
                
                foreach (var table in tables)
                {
                    try
                    {
                        Debug.Log($"[FM26CtrlP] Processando tabela...");
                        Log.LogInfo($"[Export] Processando tabela...");
                        
                        // Verificar se tem método de export direto
                        var tableType = table.GetType();
                        
                        // Buscar método Export ou similar
                        var exportMethods = tableType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var method in exportMethods)
                        {
                            if (method.Name.Contains("Export") || method.Name.Contains("CreateExport"))
                            {
                                Debug.Log($"[FM26CtrlP] Método encontrado: {method.Name}");
                                Log.LogInfo($"[Export] Método encontrado: {method.Name}");
                            }
                        }
                        
                        // Verificar propriedades
                        var props = tableType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in props)
                        {
                            if (prop.Name.Contains("Export") || prop.Name.Contains("Data"))
                            {
                                Debug.Log($"[FM26CtrlP] Propriedade: {prop.Name}");
                                Log.LogInfo($"[Export] Propriedade: {prop.Name}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FM26CtrlP] Erro ao processar tabela: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
                Debug.LogError($"[FM26CtrlP] Erro: {ex.Message}");
            }
        }
        
        private static List<object> FindVisualElementsOfType(VisualElement root, Type targetType)
        {
            var result = new List<object>();
            if (root == null || targetType == null) return result;
            FindVisualElementsRecursive(root, targetType, result, 0, 20);
            return result;
        }
        
        private static void FindVisualElementsRecursive(VisualElement element, Type targetType, List<object> result, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var elementType = element.GetType();
                if (elementType == targetType || (targetType != null && targetType.IsAssignableFrom(elementType)))
                {
                    result.Add(element);
                    Debug.Log($"[FM26CtrlP] Encontrado: {elementType.Name} (depth: {depth})");
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
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro ao buscar elementos: {ex.Message}");
            }
        }
    }
}
