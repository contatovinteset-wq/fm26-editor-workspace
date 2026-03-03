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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.17.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.17.0 CARREGADO!");
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
        
        // Cache de tipos Il2Cpp
        private static Type _streamedTableIl2Cpp;
        private static MethodInfo _getMRowsMethod;
        
        public static void OnUpdate()
        {
            try
            {
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    CacheIl2CppTypes();
                    Log.LogInfo("[Init] Pronto!");
                }
                
                if (!_initialized || Keyboard.current == null) return;
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - EXPORTAR");
                    ExportUsingIl2Cpp();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Listar TODOS os campos do PlayerSearchReport");
                    ListAllFields("PlayerSearchReport");
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Listar TODOS os campos do TeamSquadReport");
                    ListAllFields("TeamSquadReport");
                }
                
                if (Keyboard.current.f11Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F11 - Buscar 'rows' em qualquer propriedade");
                    SearchRowsProperty();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void CacheIl2CppTypes()
        {
            try
            {
                // Tentar obter o tipo Il2Cpp diretamente
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    if (asm.GetName().Name == "SI.Bindable")
                    {
                        Log.LogInfo($"[Cache] Assembly encontrado: {asm.FullName}");
                        _streamedTableIl2Cpp = asm.GetType("SI.Bindable.StreamedTable");
                        if (_streamedTableIl2Cpp != null)
                        {
                            Log.LogInfo($"[Cache] StreamedTable Il2Cpp: {_streamedTableIl2Cpp.FullName}");
                            
                            // Listar todos os métodos
                            var methods = _streamedTableIl2Cpp.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            foreach (var m in methods)
                            {
                                if (m.Name.Contains("get_m_rows") || m.Name.Contains("get_rows") || m.Name.Contains("get_Rows"))
                                {
                                    Log.LogInfo($"[Cache] Método getter encontrado: {m.Name}");
                                    _getMRowsMethod = m;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Cache] Erro: {ex.Message}");
            }
        }
        
        private static void ListAllFields(string reportName)
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    var report = FindElementByName(root, reportName, 0, 30);
                    if (report != null)
                    {
                        Log.LogInfo($"[Fields] === {reportName} ===");
                        
                        // Listar TODOS os campos
                        var type = report.GetType();
                        Log.LogInfo($"[Fields] Tipo: {type.FullName}");
                        Log.LogInfo($"[Fields] Assembly: {type.Assembly.GetName().Name}");
                        
                        // Campos
                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        Log.LogInfo($"[Fields] Campos ({fields.Length}):");
                        foreach (var f in fields)
                        {
                            Log.LogInfo($"[Fields]   {f.FieldType.Name} {f.Name}");
                        }
                        
                        // Propriedades
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        Log.LogInfo($"[Fields] Propriedades ({props.Length}):");
                        foreach (var p in props)
                        {
                            Log.LogInfo($"[Fields]   {p.PropertyType.Name} {p.Name}");
                        }
                        
                        // Métodos
                        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        Log.LogInfo($"[Fields] Métodos públicos ({methods.Length}):");
                        foreach (var m in methods)
                        {
                            if (!m.Name.StartsWith("get_") && !m.Name.StartsWith("set_") && !m.Name.StartsWith("add_") && !m.Name.StartsWith("remove_"))
                            {
                                Log.LogInfo($"[Fields]   {m.ReturnType.Name} {m.Name}()");
                            }
                        }
                    }
                    else
                    {
                        Log.LogWarning($"[Fields] {reportName} não encontrado");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Fields] Erro: {ex.Message}");
            }
        }
        
        private static void SearchRowsProperty()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                int found = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    SearchRowsRecursive(root, ref found, 0, 30);
                }
                
                Log.LogInfo($"[Search] Elementos com 'rows': {found}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Search] Erro: {ex.Message}");
            }
        }
        
        private static void SearchRowsRecursive(VisualElement element, ref int count, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return;
            
            try
            {
                var type = element.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (var p in props)
                {
                    string name = p.Name.ToLower();
                    if (name.Contains("rows") || name.Contains("row") || name.Contains("items") || name.Contains("data"))
                    {
                        count++;
                        Log.LogInfo($"[Search] ⭐ {element.name}.{p.Name} : {p.PropertyType.Name}");
                        
                        try
                        {
                            var val = p.GetValue(element);
                            if (val != null)
                            {
                                if (val is IList list)
                                {
                                    Log.LogInfo($"[Search]    IList com {list.Count} itens");
                                }
                                else
                                {
                                    Log.LogInfo($"[Search]    Valor: {val.GetType().Name}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.LogInfo($"[Search]    Erro ao ler: {ex.Message}");
                        }
                    }
                }
            }
            catch { }
            
            for (int i = 0; i < element.childCount && i < 100; i++)
            {
                SearchRowsRecursive(element[i], ref count, depth + 1, maxDepth);
            }
        }
        
        private static void ExportUsingIl2Cpp()
        {
            try
            {
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null || doc.name != "PanelManager") continue;
                    var root = doc.rootVisualElement;
                    if (root == null) continue;
                    
                    string[] targets = { "PlayerSearchReport", "TeamSquadReport" };
                    foreach (var targetName in targets)
                    {
                        var target = FindElementByName(root, targetName, 0, 30);
                        if (target != null)
                        {
                            Log.LogInfo($"[Export] Escaneando {targetName}...");
                            
                            // Buscar qualquer propriedade que seja lista
                            var data = FindAnyListData(target, 0, 20);
                            if (data != null)
                            {
                                Log.LogInfo($"[Export] ✅ Dados encontrados: {data.Count} itens");
                                ExportCsv(data);
                                return;
                            }
                        }
                    }
                }
                
                Log.LogWarning("[Export] Nenhum dado encontrado.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static IList FindAnyListData(VisualElement element, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            
            try
            {
                var type = element.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length > 0) continue;
                    
                    try
                    {
                        var val = p.GetValue(element);
                        if (val is IList list && list.Count > 5)
                        {
                            Log.LogInfo($"[Export] Lista encontrada: {p.Name} ({list.Count} itens)");
                            return list;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            for (int i = 0; i < element.childCount && i < 100; i++)
            {
                var found = FindAnyListData(element[i], depth + 1, maxDepth);
                if (found != null) return found;
            }
            
            return null;
        }
        
        private static VisualElement FindElementByName(VisualElement element, string name, int depth, int maxDepth)
        {
            if (element == null || depth > maxDepth) return null;
            if (element.name == name) return element;
            
            for (int i = 0; i < element.childCount; i++)
            {
                var found = FindElementByName(element[i], name, depth + 1, maxDepth);
                if (found != null) return found;
            }
            return null;
        }
        
        private static void ExportCsv(IList data)
        {
            try
            {
                var first = data[0];
                if (first == null) return;
                
                // Tenta extrair Item1 de ValueTuple
                var item1Prop = first.GetType().GetProperty("Item1");
                object targetObj = item1Prop != null ? item1Prop.GetValue(first) : first;
                
                var type = targetObj.GetType();
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                var csv = new System.Text.StringBuilder();
                var headers = new List<string>();
                foreach (var p in props)
                {
                    if (p.GetIndexParameters().Length == 0) headers.Add(p.Name);
                }
                csv.AppendLine(string.Join(";", headers));
                
                foreach (var item in data)
                {
                    if (item == null) continue;
                    object rowObj = item1Prop != null ? item1Prop.GetValue(item) : item;
                    
                    var values = new List<string>();
                    foreach (var p in props)
                    {
                        if (p.GetIndexParameters().Length > 0) continue;
                        try
                        {
                            var val = p.GetValue(rowObj);
                            values.Add((val?.ToString() ?? "").Replace(";", ","));
                        }
                        catch { values.Add(""); }
                    }
                    csv.AppendLine(string.Join(";", values));
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[Export] ✅ Salvo: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[CSV] Erro: {ex.Message}");
            }
        }
    }
}
