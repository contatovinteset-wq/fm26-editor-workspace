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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.14.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.14.0 CARREGADO!");
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
        
        // Cache de tipos
        private static Type _streamedTableType;
        private static Type _streamedListViewType;
        private static Type _bindingRootType;
        private static PropertyInfo _mRowsProp;
        
        public static void OnUpdate()
        {
            try
            {
                _frameCount++;
                
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    CacheTypes();
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
                    Log.LogInfo(">>> F9 - Cast direto para StreamedTable");
                    FindByCast();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Listar TODOS os tipos de elementos");
                    ListAllElementTypes();
                }
                
                if (Keyboard.current.f11Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F11 - Buscar por nome 'table'");
                    FindByName();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void CacheTypes()
        {
            _streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
            _streamedListViewType = Type.GetType("SI.Bindable.StreamedListView, SI.Bindable");
            _bindingRootType = Type.GetType("SI.Bindable.BindingRoot, SI.Bindable");
            
            if (_streamedTableType != null)
            {
                _mRowsProp = _streamedTableType.GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Log.LogInfo($"[Cache] StreamedTable: {_streamedTableType != null}, m_rows prop: {_mRowsProp != null}");
            }
        }
        
        // F9 - Tentar cast direto
        private static void FindByCast()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int count = 0;
                int elementsChecked = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        FindByCastRecursive(root, ref count, ref elementsChecked, 0, 20, doc.name);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[F9] Elementos verificados: {elementsChecked}, StreamedTable encontrados: {count}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F9] Erro: {ex.Message}");
            }
        }
        
        private static void FindByCastRecursive(VisualElement element, ref int count, ref int checkedCount, int depth, int maxDepth, string docName)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                checkedCount++;
                var type = element.GetType();
                string typeName = type.FullName ?? type.Name;
                string elementName = element.name ?? "";
                
                // Tentar cast direto
                if (_streamedTableType != null && _streamedTableType.IsAssignableFrom(type))
                {
                    count++;
                    Log.LogInfo($"[F9] ⭐ CAST OK! [{docName}] {elementName} é StreamedTable!");
                    TryReadMRows(element);
                }
                // Verificar se o nome do tipo contém "Streamed"
                else if (typeName.Contains("StreamedTable") || typeName.Contains("StreamedList"))
                {
                    count++;
                    Log.LogInfo($"[F9] ⭐ Por nome! [{docName}] {elementName} tipo={typeName}");
                    TryReadMRows(element);
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    try
                    {
                        FindByCastRecursive(element[i], ref count, ref checkedCount, depth + 1, maxDepth, docName);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private static void TryReadMRows(object element)
        {
            try
            {
                var type = element.GetType();
                var mRowsProp = type.GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (mRowsProp == null)
                {
                    Log.LogInfo($"[F9]    m_rows não encontrado neste tipo");
                    return;
                }
                
                var mRows = mRowsProp.GetValue(element) as IList;
                if (mRows != null)
                {
                    Log.LogInfo($"[F9]    m_rows: {mRows.Count} itens!");
                    if (mRows.Count > 0)
                    {
                        var first = mRows[0];
                        Log.LogInfo($"[F9]    Primeiro item: {first?.GetType().Name ?? "null"}");
                    }
                }
                else
                {
                    Log.LogInfo($"[F9]    m_rows é null");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[F9]    Erro ao ler m_rows: {ex.Message}");
            }
        }
        
        // F10 - Listar todos os tipos de elementos
        private static void ListAllElementTypes()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                var types = new HashSet<string>();
                int totalElements = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        CollectTypes(root, types, ref totalElements, 0, 15);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[F10] Total elementos: {totalElements}");
                Log.LogInfo($"[F10] Tipos únicos: {types.Count}");
                
                foreach (var t in types)
                {
                    if (t.Contains("Table") || t.Contains("List") || t.Contains("Stream") || t.Contains("Row") || t.Contains("Data"))
                    {
                        Log.LogInfo($"[F10] ⭐ {t}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[F10] Erro: {ex.Message}");
            }
        }
        
        private static void CollectTypes(VisualElement element, HashSet<string> types, ref int count, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                count++;
                var type = element.GetType();
                types.Add(type.FullName ?? type.Name);
                
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    try
                    {
                        CollectTypes(element[i], types, ref count, depth + 1, maxDepth);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // F11 - Buscar por nome
        private static void FindByName()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int count = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        FindByNameRecursive(root, ref count, 0, 20, doc.name);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[F11] Elementos com nome relevante: {count}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F11] Erro: {ex.Message}");
            }
        }
        
        private static void FindByNameRecursive(VisualElement element, ref int count, int depth, int maxDepth, string docName)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                string name = (element.name ?? "").ToLower();
                var type = element.GetType();
                string typeName = (type.FullName ?? type.Name).ToLower();
                
                if (name.Contains("table") || name.Contains("list") || name.Contains("row") || 
                    typeName.Contains("table") || typeName.Contains("stream"))
                {
                    count++;
                    Log.LogInfo($"[F11] [{docName}] name={element.name} type={type.Name}");
                }
                
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    try
                    {
                        FindByNameRecursive(element[i], ref count, depth + 1, maxDepth, docName);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // Ctrl+P - Exportar
        private static void SafeExport()
        {
            try
            {
                Log.LogInfo("[Export] Buscando dados...");
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                IList foundData = null;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        FindAndExport(root, ref foundData, 0, 20);
                        
                        if (foundData != null) break;
                    }
                    catch { }
                }
                
                if (foundData != null && foundData.Count > 0)
                {
                    Log.LogInfo($"[Export] ✅ {foundData.Count} itens");
                    ExportToCsv(foundData);
                }
                else
                {
                    Log.LogWarning("[Export] Nenhum dado. Use F9/F10/F11 para diagnosticar.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void FindAndExport(VisualElement element, ref IList foundData, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth || foundData != null) return;
            
            try
            {
                var type = element.GetType();
                string typeName = type.FullName ?? type.Name;
                
                // Tentar ler m_rows
                var mRowsProp = type.GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (mRowsProp != null)
                {
                    try
                    {
                        var mRows = mRowsProp.GetValue(element) as IList;
                        if (mRows != null && mRows.Count > 0)
                        {
                            Log.LogInfo($"[Export] Encontrado m_rows com {mRows.Count} itens em {element.name}");
                            foundData = mRows;
                            return;
                        }
                    }
                    catch { }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    try
                    {
                        FindAndExport(element[i], ref foundData, depth + 1, maxDepth);
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
                
                // m_rows é List<ValueTuple<BindingRoot, VisualElement>>
                // Precisamos extrair BindingRoot de cada tupla
                var itemType = firstItem.GetType();
                Log.LogInfo($"[Export] Tipo do item: {itemType.FullName}");
                
                // Verificar se é ValueTuple
                if (itemType.Name.StartsWith("ValueTuple"))
                {
                    Log.LogInfo("[Export] Item é ValueTuple - extraindo Item1 (BindingRoot)...");
                    
                    var item1Prop = itemType.GetProperty("Item1");
                    if (item1Prop != null)
                    {
                        var bindingRoot = item1Prop.GetValue(firstItem);
                        if (bindingRoot != null)
                        {
                            Log.LogInfo($"[Export] BindingRoot: {bindingRoot.GetType().FullName}");
                            ExportBindingRoots(data, item1Prop);
                            return;
                        }
                    }
                }
                
                // Fallback: exportar propriedades diretas
                ExportGeneric(data);
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro CSV: {ex.Message}");
            }
        }
        
        private static void ExportBindingRoots(IList data, PropertyInfo item1Prop)
        {
            try
            {
                var bindingRoots = new List<object>();
                foreach (var item in data)
                {
                    if (item == null) continue;
                    var br = item1Prop.GetValue(item);
                    if (br != null) bindingRoots.Add(br);
                }
                
                if (bindingRoots.Count == 0)
                {
                    Log.LogWarning("[Export] Nenhum BindingRoot");
                    return;
                }
                
                var first = bindingRoots[0];
                var type = first.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                var csv = new System.Text.StringBuilder();
                var headers = new List<string>();
                
                foreach (var prop in props)
                {
                    if (prop.GetIndexParameters().Length == 0 && prop.Name != "Item")
                    {
                        headers.Add(prop.Name);
                    }
                }
                csv.AppendLine(string.Join(";", headers));
                
                int rowCount = 0;
                foreach (var br in bindingRoots)
                {
                    if (br == null) continue;
                    
                    var values = new List<string>();
                    foreach (var prop in props)
                    {
                        if (prop.GetIndexParameters().Length > 0 || prop.Name == "Item") continue;
                        
                        try
                        {
                            var value = prop.GetValue(br);
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
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void ExportGeneric(IList data)
        {
            try
            {
                var first = data[0];
                var type = first.GetType();
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
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
    }
}
