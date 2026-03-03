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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.13.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.13.0 CARREGADO!");
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
                    Log.LogInfo(">>> F9 - Buscar prop m_rows");
                    FindRowsProperty();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Info prop m_rows no tipo");
                    InfoRowsProperty();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        // F9 - Buscar elementos que TÊM a propriedade m_rows
        private static void FindRowsProperty()
        {
            try
            {
                var streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                PropertyInfo rowsProp = null;
                
                // Buscar m_rows como PROPRIEDADE (não campo)
                if (streamedTableType != null)
                {
                    rowsProp = streamedTableType.GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    Log.LogInfo($"[F9] m_rows como prop: {rowsProp != null}");
                }
                
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int count = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    try
                    {
                        var root = doc.rootVisualElement;
                        if (root == null) continue;
                        
                        FindRowsRecursive(root, ref count, 0, 15, doc.name);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[F9] Total com m_rows: {count}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[F9] Erro: {ex.Message}");
            }
        }
        
        private static void FindRowsRecursive(VisualElement element, ref int count, int depth, int maxDepth, string docName)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var type = element.GetType();
                string elementName = element.name ?? "";
                
                // Buscar propriedade m_rows
                var rowsProp = type.GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (rowsProp != null)
                {
                    count++;
                    Log.LogInfo($"[F9] ⭐ [{docName}] {elementName}: tem m_rows!");
                    
                    try
                    {
                        var rows = rowsProp.GetValue(element) as IList;
                        if (rows != null)
                        {
                            Log.LogInfo($"[F9]    m_rows tem {rows.Count} itens");
                        }
                        else
                        {
                            Log.LogInfo($"[F9]    m_rows é null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogInfo($"[F9]    Erro ao ler: {ex.Message}");
                    }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 50; i++)
                {
                    try
                    {
                        FindRowsRecursive(element[i], ref count, depth + 1, maxDepth, docName);
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        // F10 - Info da propriedade m_rows no tipo StreamedTable
        private static void InfoRowsProperty()
        {
            try
            {
                var streamedTableType = Type.GetType("SI.Bindable.StreamedTable, SI.Bindable");
                if (streamedTableType == null)
                {
                    Log.LogWarning("[F10] StreamedTable não encontrado");
                    return;
                }
                
                Log.LogInfo($"[F10] Tipo: {streamedTableType.FullName}");
                
                // Buscar como propriedade
                var rowsProp = streamedTableType.GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (rowsProp != null)
                {
                    Log.LogInfo($"[F10] m_rows PROP: {rowsProp.PropertyType.FullName}");
                    Log.LogInfo($"[F10]   CanRead: {rowsProp.CanRead}");
                    Log.LogInfo($"[F10]   CanWrite: {rowsProp.CanWrite}");
                }
                else
                {
                    Log.LogWarning("[F10] m_rows não é propriedade");
                }
                
                // Buscar como campo
                var rowsField = streamedTableType.GetField("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (rowsField != null)
                {
                    Log.LogInfo($"[F10] m_rows FIELD: {rowsField.FieldType.FullName}");
                }
                else
                {
                    Log.LogWarning("[F10] m_rows não é campo");
                }
                
                // Listar todas as props com "row"
                Log.LogInfo("[F10] Props com 'row':");
                var props = streamedTableType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.Name.ToLower().Contains("row"))
                    {
                        Log.LogInfo($"[F10]   {p.PropertyType.Name} {p.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[F10] Erro: {ex.Message}");
            }
        }
        
        // Ctrl+P - Exportar
        private static void SafeExport()
        {
            try
            {
                Log.LogInfo("[Export] Buscando elementos com m_rows...");
                
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
                        
                        FindAndExport(root, ref foundData, ref foundInfo, 0, 15, doc.name);
                        
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
                    Log.LogWarning("[Export] Nenhum dado encontrado. Use F9 para ver se há m_rows.");
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
                
                // Buscar propriedade m_rows
                var rowsProp = type.GetProperty("m_rows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (rowsProp != null)
                {
                    Log.LogInfo($"[Export] Encontrado {elementName} com m_rows");
                    
                    try
                    {
                        var rows = rowsProp.GetValue(element) as IList;
                        if (rows != null && rows.Count > 0)
                        {
                            foundData = rows;
                            foundInfo = $"[{docName}] {elementName}.m_rows";
                            return;
                        }
                        else
                        {
                            Log.LogInfo($"[Export] m_rows vazio ou null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"[Export] Erro ao ler m_rows: {ex.Message}");
                    }
                }
                
                // Recursão
                for (int i = 0; i < element.childCount && i < 50; i++)
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
