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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.10.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.10.0 CARREGADO!");
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
                    Log.LogInfo(">>> F9 - Buscar IList em TODAS as props");
                    FindAllIListProperties();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Buscar StreamedTable/ListView");
                    FindStreamedElements();
                }
                
                if (Keyboard.current.f11Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F11 - Testar tipos StreamedTable");
                    TestStreamedTypes();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        // F9 - Procurar TODAS as propriedades IList
        private static void FindAllIListProperties()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int totalFound = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        FindIListRecursive(root, ref totalFound, 0, 15, doc.name);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[F9] Total IList encontradas: {totalFound}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F9] Erro: {ex.Message}");
            }
        }
        
        private static void FindIListRecursive(VisualElement element, ref int count, int depth, int maxDepth, string docName)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var type = element.GetType();
                string elementName = element.name ?? "(null)";
                
                // Procurar TODAS as propriedades
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                
                foreach (var prop in props)
                {
                    if (prop.GetIndexParameters().Length > 0) continue;
                    
                    try
                    {
                        var value = prop.GetValue(element);
                        if (value == null) continue;
                        
                        if (value is IList list && list.Count > 0)
                        {
                            count++;
                            string propType = value.GetType().Name;
                            string itemType = list[0]?.GetType().Name ?? "?";
                            Log.LogInfo($"[F9] ✅ [{docName}] {elementName}.{prop.Name}: {propType}<{itemType}> ({list.Count} itens)");
                        }
                    }
                    catch { }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    try
                    {
                        FindIListRecursive(element[i], ref count, depth + 1, maxDepth, docName);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // F10 - Buscar elementos que são StreamedTable/ListView
        private static void FindStreamedElements()
        {
            try
            {
                // Carregar tipos
                var streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                var streamedListViewType = Type.GetType("SI.Bindable.StreamedListView, SI.Bindable");
                var streamedObjectListType = Type.GetType("SI.Bindable.StreamedObjectList, SI.Bindable");
                
                Log.LogInfo($"[F10] StreamedTable: {streamedTableType != null}");
                Log.LogInfo($"[F10] StreamedListView: {streamedListViewType != null}");
                Log.LogInfo($"[F10] StreamedObjectList: {streamedObjectListType != null}");
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int totalFound = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        FindStreamedRecursive(root, ref totalFound, 0, 20, doc.name, streamedTableType, streamedListViewType, streamedObjectListType);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[F10] Total StreamedElements: {totalFound}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F10] Erro: {ex.Message}");
            }
        }
        
        private static void FindStreamedRecursive(VisualElement element, ref int count, int depth, int maxDepth, string docName, Type stType, Type slvType, Type solType)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var type = element.GetType();
                string typeName = type.FullName ?? type.Name;
                string elementName = element.name ?? "(null)";
                
                bool isStreamed = typeName.Contains("StreamedTable") || 
                                  typeName.Contains("StreamedListView") ||
                                  typeName.Contains("StreamedObjectList");
                
                if (isStreamed)
                {
                    count++;
                    Log.LogInfo($"[F10] ⭐ [{docName}] {elementName}: {typeName}");
                    
                    // Listar propriedades do elemento
                    try
                    {
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var prop in props)
                        {
                            if (prop.GetIndexParameters().Length > 0) continue;
                            if (prop.Name == "name" || prop.Name == "childCount") continue;
                            
                            try
                            {
                                var value = prop.GetValue(element);
                                if (value == null) continue;
                                
                                if (value is IList list)
                                {
                                    Log.LogInfo($"[F10]   {prop.Name}: IList ({list.Count} itens)");
                                }
                                else if (value is IEnumerable en && !(value is string))
                                {
                                    Log.LogInfo($"[F10]   {prop.Name}: {value.GetType().Name}");
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 100; i++)
                {
                    try
                    {
                        FindStreamedRecursive(element[i], ref count, depth + 1, maxDepth, docName, stType, slvType, solType);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // F11 - Testar tipos StreamedTable diretamente
        private static void TestStreamedTypes()
        {
            try
            {
                var streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                if (streamedTableType == null)
                {
                    Log.LogWarning("[F11] StreamedTable não encontrado");
                    return;
                }
                
                Log.LogInfo($"[F11] StreamedType: {streamedTableType.FullName}");
                Log.LogInfo("[F11] Propriedades:");
                
                var props = streamedTableType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var prop in props)
                {
                    Log.LogInfo($"[F11]   {prop.PropertyType.Name} {prop.Name}");
                }
                
                Log.LogInfo("[F11] Campos:");
                var fields = streamedTableType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    Log.LogInfo($"[F11]   {field.FieldType.Name} {field.Name}");
                }
                
                Log.LogInfo("[F11] Métodos:");
                var methods = streamedTableType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                    {
                        Log.LogInfo($"[F11]   {method.ReturnType.Name} {method.Name}()");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[F11] Erro: {ex.Message}");
            }
        }
        
        // Ctrl+P - Exportar
        private static void SafeExport()
        {
            try
            {
                Log.LogInfo("[Export] Buscando IList em todas as propriedades...");
                
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
                        
                        FindDataForExport(root, ref foundData, ref foundInfo, 0, 15, doc.name);
                        
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
                    Log.LogWarning("[Export] Nenhum dado encontrado. Use F9.");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void FindDataForExport(VisualElement element, ref IList foundData, ref string foundInfo, int depth, int maxDepth, string docName)
        {
            if (element == null || depth > maxDepth || foundData != null) return;
            
            try
            {
                var type = element.GetType();
                string elementName = element.name ?? "(null)";
                
                // Procurar propriedades IList
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var prop in props)
                {
                    if (prop.GetIndexParameters().Length > 0) continue;
                    if (foundData != null) break;
                    
                    try
                    {
                        var value = prop.GetValue(element);
                        if (value is IList list && list.Count > 5) // Pelo menos 5 itens
                        {
                            foundData = list;
                            foundInfo = $"[{docName}] {elementName}.{prop.Name}";
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
                        FindDataForExport(element[i], ref foundData, ref foundInfo, depth + 1, maxDepth, docName);
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
