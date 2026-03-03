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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.15.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.15.0 CARREGADO!");
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
                    Log.LogInfo(">>> F9 - Buscar QUALQUER IList");
                    FindAnyIList();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Listar UIDocuments");
                    ListUIDocuments();
                }
                
                if (Keyboard.current.f11Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F11 - Dump hierarquia completa");
                    DumpHierarchy();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        // F9 - Buscar qualquer elemento que tenha uma propriedade IList
        private static void FindAnyIList()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int listsFound = 0;
                int elementsChecked = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        FindIListRecursive(root, ref listsFound, ref elementsChecked, 0, 50, doc.name);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[F9] Elementos: {elementsChecked}, ILists: {listsFound}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F9] Erro: {ex.Message}");
            }
        }
        
        private static void FindIListRecursive(VisualElement element, ref int count, ref int checkedCount, int depth, int maxDepth, string docName)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                checkedCount++;
                var type = element.GetType();
                string elementName = element.name ?? "";
                
                // Buscar TODAS as propriedades
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (var prop in props)
                {
                    if (prop.GetIndexParameters().Length > 0) continue;
                    if (prop.Name == "Item") continue;
                    
                    try
                    {
                        var propType = prop.PropertyType;
                        
                        // Verificar se é IList ou IEnumerable (mas não string)
                        if (typeof(IList).IsAssignableFrom(propType) || 
                            (propType.Name.Contains("List") && propType != typeof(string)))
                        {
                            var value = prop.GetValue(element) as IList;
                            if (value != null && value.Count > 0)
                            {
                                count++;
                                Log.LogInfo($"[F9] ⭐ [{docName}] {elementName}.{prop.Name} = {value.Count} itens");
                                Log.LogInfo($"[F9]    Tipo: {propType.Name}");
                                
                                var first = value[0];
                                if (first != null)
                                {
                                    Log.LogInfo($"[F9]    Item: {first.GetType().Name}");
                                }
                            }
                        }
                    }
                    catch { }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 200; i++)
                {
                    try
                    {
                        FindIListRecursive(element[i], ref count, ref checkedCount, depth + 1, maxDepth, docName);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // F10 - Listar todos os UIDocuments
        private static void ListUIDocuments()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[F10] UIDocuments encontrados: {uiDocs.Length}");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        Log.LogInfo($"[F10] - {doc.name}: root={root?.name ?? "null"}, children={root?.childCount ?? 0}");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[F10] Erro: {ex.Message}");
            }
        }
        
        // F11 - Dump da hierarquia (primeiros 100 elementos)
        private static void DumpHierarchy()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int total = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        Log.LogInfo($"[F11] === {doc.name} ===");
                        DumpElement(root, 0, 5, ref total, 100);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[F11] Total dumpado: {total}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F11] Erro: {ex.Message}");
            }
        }
        
        private static void DumpElement(VisualElement element, int depth, int maxDepth, ref int total, int limit)
        {
            if (element == null || depth > maxDepth || total >= limit) return;
            
            try
            {
                total++;
                string indent = new string(' ', depth * 2);
                string name = element.name ?? "(no name)";
                string type = element.GetType().Name;
                int children = element.childCount;
                
                Log.LogInfo($"[F11] {indent}{name} [{type}] ({children} filhos)");
                
                for (int i = 0; i < children && i < 20; i++)
                {
                    try
                    {
                        DumpElement(element[i], depth + 1, maxDepth, ref total, limit);
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
                Log.LogInfo("[Export] Buscando ILists...");
                
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
                        
                        FindAndExport(root, ref foundData, ref foundInfo, 0, 50, doc.name);
                        
                        if (foundData != null) break;
                    }
                    catch { }
                }
                
                if (foundData != null && foundData.Count > 0)
                {
                    Log.LogInfo($"[Export] ✅ {foundInfo}");
                    ExportToCsv(foundData);
                }
                else
                {
                    Log.LogWarning("[Export] Nenhum IList com dados. Use F9.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void FindAndExport(VisualElement element, ref IList foundData, ref string foundInfo, int depth, int maxDepth, string docName)
        {
            if (element == null || depth > maxDepth || foundData != null) return;
            
            try
            {
                var type = element.GetType();
                string elementName = element.name ?? "";
                
                // Priorizar m_rows
                var mRowsProp = type.GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (mRowsProp != null)
                {
                    try
                    {
                        var mRows = mRowsProp.GetValue(element) as IList;
                        if (mRows != null && mRows.Count > 0)
                        {
                            foundData = mRows;
                            foundInfo = $"[{docName}] {elementName}.m_rows ({mRows.Count} itens)";
                            return;
                        }
                    }
                    catch { }
                }
                
                // Buscar outras Listas
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (foundData != null) break;
                    if (prop.GetIndexParameters().Length > 0) continue;
                    if (prop.Name == "Item") continue;
                    
                    try
                    {
                        if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                        {
                            var value = prop.GetValue(element) as IList;
                            if (value != null && value.Count > 10) // Mínimo 10 itens
                            {
                                foundData = value;
                                foundInfo = $"[{docName}] {elementName}.{prop.Name} ({value.Count} itens)";
                                return;
                            }
                        }
                    }
                    catch { }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 200; i++)
                {
                    try
                    {
                        FindAndExport(element[i], ref foundData, ref foundInfo, depth + 1, maxDepth, docName);
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
                
                var itemType = firstItem.GetType();
                Log.LogInfo($"[Export] Tipo: {itemType.FullName}");
                
                // ValueTuple?
                if (itemType.Name.StartsWith("ValueTuple"))
                {
                    var item1Prop = itemType.GetProperty("Item1");
                    if (item1Prop != null)
                    {
                        var firstBinding = item1Prop.GetValue(firstItem);
                        if (firstBinding != null)
                        {
                            Log.LogInfo($"[Export] Item1: {firstBinding.GetType().FullName}");
                            ExportBindingRoots(data, item1Prop);
                            return;
                        }
                    }
                }
                
                ExportGeneric(data);
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
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
                
                Log.LogInfo($"[Export] {bindingRoots.Count} BindingRoots");
                
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
                Log.LogInfo($"[Export] ✅ {rowCount} linhas em: {path}");
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
                Log.LogInfo($"[Export] ✅ {rowCount} linhas em: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
    }
}
