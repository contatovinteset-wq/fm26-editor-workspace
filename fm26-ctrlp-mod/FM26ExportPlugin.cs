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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.3.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.3.0 CARREGADO!");
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
            
            // F9 - Buscar DADOS no Report (versão segura)
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F9 - Buscar dados no Report");
                Log.LogInfo(">>> F9 - Buscar dados no Report");
                FindDataInReport();
            }
            
            // F10 - Buscar tabelas
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Buscar tabelas");
                Log.LogInfo(">>> F10 - Buscar tabelas");
                FindTablesViaUIDocuments();
            }
            
            // F11 - Listar filhos do Report
            if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F11 - Listar filhos do Report");
                Log.LogInfo(">>> F11 - Listar filhos do Report");
                ListReportChildren();
            }
            
            // F12 - Buscar tipos com Dados
            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26CtrlP] >>> F12 - Buscar tipos com Dados");
                Log.LogInfo(">>> F12 - Buscar tipos com Dados");
                FindDataTypes();
            }
        }
        
        private static void InitializeTypes()
        {
            try
            {
                _streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                Log.LogInfo($"[Init] StreamedTable: {(_streamedTableType != null ? "OK" : "NÃO")}");
                
                _streamedListViewType = Type.GetType("SI.Bindable.StreamedListView, SI.Bindable");
                Log.LogInfo($"[Init] StreamedListView: {(_streamedListViewType != null ? "OK" : "NÃO")}");
                
                _streamedObjectListType = Type.GetType("SI.Bindable.StreamedObjectList, SI.Bindable");
                Log.LogInfo($"[Init] StreamedObjectList: {(_streamedObjectListType != null ? "OK" : "NÃO")}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
        
        // Versão SEGURA - foca em encontrar dados, não em dump completo
        private static void FindDataInReport()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[FindData] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    if (doc.name != "PanelManager") continue;
                    
                    // Encontrar Report
                    for (int i = 0; i < root.childCount; i++)
                    {
                        var child = root[i];
                        if (child?.name == "Report")
                        {
                            Log.LogInfo($"[FindData] Report encontrado");
                            SearchForDataInElement(child, 0, 5);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[FindData] Erro: {ex.Message}");
            }
        }
        
        private static void SearchForDataInElement(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var elemType = element.GetType();
                string elemName = element.name ?? "";
                
                // Logar elemento atual
                Log.LogInfo($"[Search] {new string(' ', depth*2)}{elemName} ({elemType.Name})");
                
                // Se for um "Report", investigar propriedades de dados
                if (elemName.Contains("Report") || elemName.Contains("Search") || elemName.Contains("Squad"))
                {
                    Log.LogInfo($"[Search] === Investigando {elemName} ===");
                    SafeInspectDataProperties(element);
                }
                
                // Recursão nos filhos
                for (int i = 0; i < element.childCount && i < 20; i++)
                {
                    SearchForDataInElement(element[i], depth + 1, maxDepth);
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Search] Erro no elemento: {ex.Message}");
            }
        }
        
        // Inspeção SEGURA de propriedades - foca em dados
        private static void SafeInspectDataProperties(object obj)
        {
            if (obj == null) return;
            
            try
            {
                var objType = obj.GetType();
                Log.LogInfo($"[Inspect] Tipo: {objType.Name}");
                
                // Lista de nomes de propriedades que podem ter dados
                string[] dataProps = { "Data", "Items", "List", "Players", "Rows", "Columns", 
                    "Source", "Count", "Values", "Results", "Entities" };
                
                var props = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                int found = 0;
                foreach (var prop in props)
                {
                    if (found >= 10) break; // Limitar a 10 propriedades
                    
                    try
                    {
                        string propName = prop.Name;
                        
                        // Pular indexadores e propriedades com parâmetros
                        if (prop.GetIndexParameters().Length > 0) continue;
                        
                        // Verificar se o nome parece ter dados
                        bool isDataProp = false;
                        foreach (var dataName in dataProps)
                        {
                            if (propName.ToLower().Contains(dataName.ToLower()))
                            {
                                isDataProp = true;
                                break;
                            }
                        }
                        
                        if (!isDataProp) continue;
                        
                        var propType = prop.PropertyType;
                        
                        // Tentar obter valor com segurança
                        object value = null;
                        try
                        {
                            value = prop.GetValue(obj);
                        }
                        catch
                        {
                            continue; // Pular se não conseguir acessar
                        }
                        
                        if (value == null) continue;
                        
                        string info = $"[Inspect] {propName}: {propType.Name}";
                        
                        // Se for lista/coleção, mostrar count
                        if (propType.IsGenericType || propType.IsArray)
                        {
                            try
                            {
                                var countProp = propType.GetProperty("Count");
                                if (countProp != null)
                                {
                                    var count = countProp.GetValue(value);
                                    info += $" (Count={count})";
                                    
                                    // Se tiver itens, mostrar tipo do primeiro
                                    if (count != null && (int)count > 0)
                                    {
                                        var getItemMethod = propType.GetMethod("get_Item");
                                        if (getItemMethod != null)
                                        {
                                            var firstItem = getItemMethod.Invoke(value, new object[] { 0 });
                                            if (firstItem != null)
                                            {
                                                info += $" FirstItem: {firstItem.GetType().Name}";
                                            }
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                        
                        Debug.Log($"[FM26CtrlP] {info}");
                        Log.LogInfo(info);
                        found++;
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"[Inspect] Erro prop: {ex.Message}");
                    }
                }
                
                Log.LogInfo($"[Inspect] {found} propriedades de dados encontradas");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Inspect] Erro: {ex.Message}");
            }
        }
        
        private static void FindTablesViaUIDocuments()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Tables] {uiDocs.Length} UIDocuments");
                
                int tables = 0, lists = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var (t, l) = ScanForTables(root, 0, 10);
                    tables += t;
                    lists += l;
                }
                
                Log.LogInfo($"[Tables] Total: {tables} tabelas, {lists} listas");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Tables] Erro: {ex.Message}");
            }
        }
        
        private static (int tables, int lists) ScanForTables(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return (0, 0);
            
            int tables = 0, lists = 0;
            
            try
            {
                var elemType = element.GetType();
                
                if (_streamedTableType != null && _streamedTableType.IsAssignableFrom(elemType))
                {
                    tables++;
                    Log.LogInfo($"[Scan] TABLE: {element.name}");
                }
                
                if ((_streamedListViewType != null && _streamedListViewType.IsAssignableFrom(elemType)) ||
                    (_streamedObjectListType != null && _streamedObjectListType.IsAssignableFrom(elemType)))
                {
                    lists++;
                    Log.LogInfo($"[Scan] LIST: {element.name}");
                }
                
                for (int i = 0; i < element.childCount && i < 30; i++)
                {
                    var (t, l) = ScanForTables(element[i], depth + 1, maxDepth);
                    tables += t;
                    lists += l;
                }
            }
            catch { }
            
            return (tables, lists);
        }
        
        private static void ListReportChildren()
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
                            Log.LogInfo($"[List] Report encontrado, {child.childCount} filhos:");
                            
                            for (int j = 0; j < child.childCount && j < 10; j++)
                            {
                                var sub = child[j];
                                if (sub == null) continue;
                                Log.LogInfo($"[List] [{j}] {sub.name} ({sub.GetType().Name})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[List] Erro: {ex.Message}");
            }
        }
        
        private static void FindDataTypes()
        {
            try
            {
                Log.LogInfo("[Types] Buscando tipos com dados de jogador...");
                
                int found = 0;
                
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = asm.GetName().Name;
                        if (!name.StartsWith("SI.") && !name.StartsWith("FM.")) continue;
                        
                        foreach (var type in asm.GetTypes())
                        {
                            string typeName = type.Name.ToLower();
                            
                            if (typeName.Contains("playerdata") || 
                                typeName.Contains("playerlist") ||
                                typeName.Contains("squaddata") ||
                                typeName.Contains("teamdata") ||
                                typeName.Contains("searchresult"))
                            {
                                Log.LogInfo($"[Types] {type.FullName}");
                                found++;
                                
                                if (found >= 20) break;
                            }
                        }
                    }
                    catch { }
                    
                    if (found >= 20) break;
                }
                
                Log.LogInfo($"[Types] {found} tipos encontrados");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Types] Erro: {ex.Message}");
            }
        }
        
        private static void DoExport()
        {
            try
            {
                Log.LogInfo("[Export] Buscando dados para exportar...");
                FindDataInReport();
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
    }
}
