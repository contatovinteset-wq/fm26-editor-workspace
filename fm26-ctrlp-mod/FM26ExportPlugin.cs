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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.51.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.51.0 CARREGADO!");
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
                
                try
                {
                    if (Keyboard.current.f9Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F9 - Listar itens com m_value não-null");
                        ListNonEmptyMValues();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Explorar DataType do TypedValue");
                        ExploreDataTypes();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F10] {ex.Message}"); }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P - Extrair e exportar valores");
                        ExtractAndExport();
                    }
                }
                catch (Exception ex) { Log.LogError($"[CtrlP] {ex.Message}"); }
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
        
        private static void ListNonEmptyMValues()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) { Log.LogWarning("[List] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) { Log.LogWarning("[List] Sem Count/Indexer"); return; }
                
                int total = (int)countProp.GetValue(mData);
                Log.LogInfo($"[List] Total: {total} itens");
                
                int withValue = 0;
                int withHandler = 0;
                var typeSamples = new Dictionary<string, int>();
                
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var itemType = item.GetType();
                        
                        // m_value
                        var mValueProp = itemType.GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mValueProp != null)
                        {
                            var mValue = mValueProp.GetValue(item);
                            if (mValue != null)
                            {
                                withValue++;
                                
                                // DataType do TypedValue
                                var tvType = mValue.GetType();
                                var dataTypeProp = tvType.GetProperty("DataType");
                                if (dataTypeProp != null)
                                {
                                    var dt = dataTypeProp.GetValue(mValue);
                                    var dtName = dt?.ToString() ?? "null";
                                    if (dtName.Contains(",")) dtName = dtName.Split(',')[0];
                                    
                                    if (!typeSamples.ContainsKey(dtName)) typeSamples[dtName] = 0;
                                    typeSamples[dtName]++;
                                }
                            }
                        }
                        
                        // handler
                        var handlerProp = itemType.GetProperty("handler", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (handlerProp != null && handlerProp.GetValue(item) != null) withHandler++;
                    }
                    catch { }
                }
                
                Log.LogInfo($"[List] Com m_value: {withValue}");
                Log.LogInfo($"[List] Com handler: {withHandler}");
                Log.LogInfo($"[List] DataTypes encontrados:");
                
                foreach (var kvp in typeSamples.OrderByDescending(x => x.Value).Take(20))
                {
                    Log.LogInfo($"[List]   {kvp.Key}: {kvp.Value}");
                }
            }
            catch (Exception ex) { Log.LogError($"[List] {ex.Message}"); }
        }
        
        private static void ExploreDataTypes()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) { Log.LogWarning("[Exp] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                int found = 0;
                
                for (int i = 0; i < total && found < 10; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var itemType = item.GetType();
                        var mValueProp = itemType.GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue == null) continue;
                        
                        found++;
                        
                        var tvType = mValue.GetType();
                        Log.LogInfo($"[Exp] Item [{i}] - TypedValue");
                        
                        // DataType
                        var dataTypeProp = tvType.GetProperty("DataType");
                        if (dataTypeProp != null)
                        {
                            var dt = dataTypeProp.GetValue(mValue);
                            Log.LogInfo($"[Exp]   DataType: {dt}");
                        }
                        
                        // IsNull
                        var isNullProp = tvType.GetProperty("IsNull");
                        if (isNullProp != null)
                        {
                            var isNull = isNullProp.GetValue(mValue);
                            Log.LogInfo($"[Exp]   IsNull: {isNull}");
                        }
                        
                        // IsValueType
                        var isValueTypeProp = tvType.GetProperty("IsValueType");
                        if (isValueTypeProp != null)
                        {
                            var isValueType = isValueTypeProp.GetValue(mValue);
                            Log.LogInfo($"[Exp]   IsValueType: {isValueType}");
                        }
                        
                        // AsString
                        try
                        {
                            var asStringMethod = tvType.GetMethod("AsString");
                            if (asStringMethod != null)
                            {
                                var str = asStringMethod.Invoke(mValue, null);
                                Log.LogInfo($"[Exp]   AsString: {str}");
                            }
                        }
                        catch (Exception ex) { Log.LogInfo($"[Exp]   AsString erro: {ex.Message}"); }
                        
                        // Get() sem parâmetros
                        try
                        {
                            var getMethod = tvType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                .FirstOrDefault(m => m.Name == "Get" && !m.IsGenericMethod && m.GetParameters().Length == 0);
                            
                            if (getMethod != null)
                            {
                                var obj = getMethod.Invoke(mValue, null);
                                if (obj != null)
                                {
                                    Log.LogInfo($"[Exp]   Get(): {obj.GetType().Name}");
                                    
                                    // Tentar ToString
                                    try
                                    {
                                        var toString = obj.ToString();
                                        if (!string.IsNullOrEmpty(toString) && toString.Length < 100)
                                        {
                                            Log.LogInfo($"[Exp]   Get().ToString(): {toString}");
                                        }
                                    }
                                    catch { }
                                    
                                    // Propriedades do objeto retornado
                                    var objType = obj.GetType();
                                    var objProps = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                                    Log.LogInfo($"[Exp]   Get() tem {objProps.Length} props");
                                    
                                    foreach (var p in objProps.Take(5))
                                    {
                                        try
                                        {
                                            var v = p.GetValue(obj);
                                            var vs = v?.ToString() ?? "null";
                                            if (vs.Length > 40) vs = vs.Substring(0, 40) + "...";
                                            Log.LogInfo($"[Exp]     {p.Name}: {vs}");
                                        }
                                        catch { }
                                    }
                                }
                                else
                                {
                                    Log.LogInfo($"[Exp]   Get(): null");
                                }
                            }
                        }
                        catch (Exception ex) { Log.LogInfo($"[Exp]   Get() erro: {ex.Message}"); }
                    }
                    catch { }
                }
                
                if (found == 0) Log.LogWarning("[Exp] Nenhum m_value encontrado");
            }
            catch (Exception ex) { Log.LogError($"[Exp] {ex.Message}"); }
        }
        
        private static void ExtractAndExport()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) { Log.LogWarning("[Export] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                
                // Coletar todos os TypedValues
                var typedValues = new List<object>();
                
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var itemType = item.GetType();
                        var mValueProp = itemType.GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue != null) typedValues.Add(mValue);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {typedValues.Count} TypedValues");
                
                // Extrair valores via Get()
                var extractedValues = new List<Tuple<object, string>>();
                
                foreach (var tv in typedValues)
                {
                    try
                    {
                        var tvType = tv.GetType();
                        var getMethod = tvType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                            .FirstOrDefault(m => m.Name == "Get" && !m.IsGenericMethod && m.GetParameters().Length == 0);
                        
                        if (getMethod == null) continue;
                        
                        var obj = getMethod.Invoke(tv, null);
                        if (obj == null) continue;
                        
                        // Tipo do objeto
                        var objType = obj.GetType();
                        var typeName = objType.Name;
                        
                        extractedValues.Add(Tuple.Create(obj, typeName));
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {extractedValues.Count} valores extraídos");
                
                // Agrupar por tipo
                var byType = extractedValues.GroupBy(v => v.Item2).ToDictionary(g => g.Key, g => g.ToList());
                
                foreach (var kvp in byType)
                {
                    Log.LogInfo($"[Export]   {kvp.Key}: {kvp.Value.Count}");
                }
                
                // Exportar cada tipo
                foreach (var kvp in byType)
                {
                    var typeName = kvp.Key;
                    var items = kvp.Value;
                    
                    if (items.Count == 0) continue;
                    
                    var first = items[0].Item1;
                    var firstType = first.GetType();
                    
                    var props = firstType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetIndexParameters().Length == 0)
                        .ToList();
                    
                    if (props.Count == 0)
                    {
                        // Tentar AsString
                        var strValues = new List<string>();
                        foreach (var item in items)
                        {
                            try
                            {
                                strValues.Add(item.Item1?.ToString() ?? "");
                            }
                            catch { }
                        }
                        
                        if (strValues.Count > 0)
                        {
                            string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_{typeName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                            System.IO.File.WriteAllLines(path, strValues);
                            Log.LogInfo($"[Export] ✅ {typeName}: {strValues.Count} linhas (texto)");
                        }
                        continue;
                    }
                    
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine(string.Join(";", props.Select(p => p.Name)));
                    
                    int exported = 0;
                    foreach (var item in items)
                    {
                        try
                        {
                            var row = props.Select(p =>
                            {
                                try
                                {
                                    var v = p.GetValue(item.Item1);
                                    var s = v?.ToString() ?? "";
                                    return s.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                                }
                                catch { return ""; }
                            });
                            
                            csv.AppendLine(string.Join(";", row));
                            exported++;
                        }
                        catch { }
                    }
                    
                    string safeName = string.Join("_", typeName.Split(System.IO.Path.GetInvalidFileNameChars()));
                    string csvPath = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                    System.IO.File.WriteAllText(csvPath, csv.ToString());
                    
                    Log.LogInfo($"[Export] ✅ {typeName}: {exported} linhas");
                }
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
    }
}
