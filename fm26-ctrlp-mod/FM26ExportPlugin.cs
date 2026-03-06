using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.52.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.52.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            try
            {
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
            catch (Exception ex) { Log.LogError($"[Init] Erro: {ex.Message}"); }
        }
        
        private static object _bindingsInstance = null;
        private static int _frameCount = 0;
        private static bool _initialized = false;
        
        public static void OnUpdate(object __instance)
        {
            try
            {
                if (_bindingsInstance == null && __instance != null)
                {
                    _bindingsInstance = __instance;
                    Log.LogInfo("[Hook] Bindings capturada!");
                }
                
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[Init] Pronto!");
                }
                
                if (!_initialized) return;
                
                try { if (Keyboard.current == null) return; }
                catch { return; }
                
                // F9 - Conta e lista tipos via AsString
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Listar tipos via AsString");
                    ListTypesViaAsString();
                }
                
                // F10 - Explora List<TypedValue> aninhados
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Explorar List<TypedValue>");
                    ExploreNestedLists();
                }
                
                // Ctrl+P - Exporta usando AsString
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar via AsString");
                    ExportViaAsString();
                }
            }
            catch { }
        }
        
        private static object GetMData()
        {
            if (_bindingsInstance == null) return null;
            
            var type = _bindingsInstance.GetType();
            var mDataProp = type.GetProperty("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            if (mDataProp != null) return mDataProp.GetValue(_bindingsInstance);
            
            var mDataField = type.GetField("m_data", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            return mDataField?.GetValue(_bindingsInstance);
        }
        
        private static List<object> GetAllTypedValues()
        {
            var result = new List<object>();
            
            try
            {
                var mData = GetMData();
                if (mData == null) return result;
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return result;
                
                int total = (int)countProp.GetValue(mData);
                
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var mValueProp = item.GetType().GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue != null) result.Add(mValue);
                    }
                    catch { }
                }
            }
            catch { }
            
            return result;
        }
        
        private static string CallAsString(object typedValue)
        {
            try
            {
                var method = typedValue.GetType().GetMethod("AsString");
                return method?.Invoke(typedValue, null)?.ToString() ?? "";
            }
            catch { return ""; }
        }
        
        private static object CallGet(object typedValue)
        {
            try
            {
                var method = typedValue.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "Get" && !m.IsGenericMethod && m.GetParameters().Length == 0);
                return method?.Invoke(typedValue, null);
            }
            catch { return null; }
        }
        
        private static void ListTypesViaAsString()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                Log.LogInfo($"[List] {typedValues.Count} TypedValues");
                
                var typeCounts = new Dictionary<string, int>();
                var samples = new Dictionary<string, string>();
                
                foreach (var tv in typedValues)
                {
                    try
                    {
                        var asString = CallAsString(tv);
                        if (string.IsNullOrEmpty(asString)) continue;
                        
                        // Extrair tipo do AsString
                        string typeKey;
                        if (asString.StartsWith("RGBA(")) typeKey = "Color";
                        else if (asString == "True" || asString == "False") typeKey = "Boolean";
                        else if (int.TryParse(asString, out _)) typeKey = "Int32";
                        else if (float.TryParse(asString.Replace(".", ","), out _)) typeKey = "Float";
                        else if (asString.StartsWith("FM.UI.") || asString.StartsWith("SI.")) typeKey = asString.Split('(')[0].Split('[')[0];
                        else if (asString.StartsWith("System.Collections.Generic.List")) typeKey = "List<TypedValue>";
                        else typeKey = "Other";
                        
                        if (!typeCounts.ContainsKey(typeKey)) 
                        {
                            typeCounts[typeKey] = 0;
                            samples[typeKey] = asString.Length > 60 ? asString.Substring(0, 60) + "..." : asString;
                        }
                        typeCounts[typeKey]++;
                    }
                    catch { }
                }
                
                Log.LogInfo("[List] Tipos encontrados:");
                foreach (var kvp in typeCounts.OrderByDescending(x => x.Value))
                {
                    Log.LogInfo($"[List]   {kvp.Key}: {kvp.Value} - \"{samples[kvp.Key]}\"");
                }
            }
            catch (Exception ex) { Log.LogError($"[List] {ex.Message}"); }
        }
        
        private static void ExploreNestedLists()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                
                foreach (var tv in typedValues)
                {
                    try
                    {
                        var asString = CallAsString(tv);
                        if (string.IsNullOrEmpty(asString)) continue;
                        
                        // Procurar List<TypedValue>
                        if (asString.Contains("List`1[SI.Core.TypedValue]"))
                        {
                            Log.LogInfo("[Nest] Encontrado List<TypedValue>!");
                            
                            // Get() para pegar o objeto
                            var obj = CallGet(tv);
                            if (obj == null) { Log.LogWarning("[Nest] Get() retornou null"); continue; }
                            
                            var objType = obj.GetType();
                            Log.LogInfo($"[Nest] Tipo: {objType.FullName}");
                            
                            // Count
                            var countProp = objType.GetProperty("Count");
                            if (countProp != null)
                            {
                                int count = (int)countProp.GetValue(obj);
                                Log.LogInfo($"[Nest] Count: {count}");
                                
                                // Indexer
                                var indexer = objType.GetProperty("Item");
                                if (indexer != null)
                                {
                                    Log.LogInfo("[Nest] Itens:");
                                    for (int i = 0; i < Math.Min(count, 10); i++)
                                    {
                                        try
                                        {
                                            var inner = indexer.GetValue(obj, new object[] { i });
                                            if (inner == null) { Log.LogInfo($"[Nest]   [{i}]: null"); continue; }
                                            
                                            var innerAsString = CallAsString(inner);
                                            Log.LogInfo($"[Nest]   [{i}]: {innerAsString}");
                                        }
                                        catch (Exception ex) { Log.LogInfo($"[Nest]   [{i}]: erro - {ex.Message}"); }
                                    }
                                }
                            }
                            
                            return; // Só explorar o primeiro
                        }
                    }
                    catch { }
                }
                
                Log.LogWarning("[Nest] Nenhum List<TypedValue> encontrado");
            }
            catch (Exception ex) { Log.LogError($"[Nest] {ex.Message}"); }
        }
        
        private static void ExportViaAsString()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                Log.LogInfo($"[Export] {typedValues.Count} TypedValues");
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("index;value");
                
                int exported = 0;
                int index = 0;
                
                foreach (var tv in typedValues)
                {
                    try
                    {
                        var asString = CallAsString(tv);
                        if (string.IsNullOrEmpty(asString)) continue;
                        
                        // Escapar para CSV
                        var safeValue = asString.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                        csv.AppendLine($"{index};{safeValue}");
                        exported++;
                    }
                    catch { }
                    index++;
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {exported} valores exportados");
                Log.LogInfo($"[Export] Arquivo: {path}");
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
    }
}
