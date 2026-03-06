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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.49.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.49.0 CARREGADO!");
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
                        Log.LogInfo(">>> F9 - Descobrir tipos reais via GetType()");
                        DiscoverRealTypes();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Explorar valor real de um Object");
                        ExploreRealObject();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F10] {ex.Message}"); }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P - Exportar valores desembrulhados");
                        ExportUnwrapped();
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
        
        private static List<object> GetAllTypedValues()
        {
            var values = new List<object>();
            var mData = GetMData();
            if (mData == null) return values;
            
            var listType = mData.GetType();
            var countProp = listType.GetProperty("Count");
            var indexer = listType.GetProperty("Item");
            
            if (countProp == null || indexer == null) return values;
            
            int total = (int)countProp.GetValue(mData);
            
            for (int i = 0; i < total; i++)
            {
                try
                {
                    var item = indexer.GetValue(mData, new object[] { i });
                    if (item == null) continue;
                    
                    var itemType = item.GetType();
                    var mValueProp = itemType.GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    
                    if (mValueProp == null) continue;
                    
                    var val = mValueProp.GetValue(item);
                    if (val != null) values.Add(val);
                }
                catch { }
            }
            
            return values;
        }
        
        private static MethodInfo FindGetNonGenericMethod(Type tvType)
        {
            var methods = tvType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var m in methods)
            {
                if (m.Name == "Get" && 
                    !m.IsGenericMethod && 
                    m.GetParameters().Length == 0 &&
                    m.ReturnType == typeof(object))
                {
                    return m;
                }
            }
            
            return null;
        }
        
        private static void DiscoverRealTypes()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                Log.LogInfo($"[Types] {typedValues.Count} TypedValues");
                
                var typeCounts = new Dictionary<string, int>();
                int tested = 0;
                
                foreach (var tv in typedValues)
                {
                    try
                    {
                        var tvType = tv.GetType();
                        var getMethod = FindGetNonGenericMethod(tvType);
                        if (getMethod == null) continue;
                        
                        var obj = getMethod.Invoke(tv, null);
                        if (obj == null) continue;
                        
                        tested++;
                        
                        // Descobrir tipo REAL via GetType()
                        var realType = obj.GetType();
                        var typeName = realType.FullName ?? realType.Name;
                        
                        // Simplificar
                        if (typeName.Contains(","))
                            typeName = typeName.Split(',')[0];
                        
                        if (!typeCounts.ContainsKey(typeName)) typeCounts[typeName] = 0;
                        typeCounts[typeName]++;
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Types] Testados: {tested}");
                Log.LogInfo("[Types] Tipos REAIS encontrados:");
                
                foreach (var kvp in typeCounts.OrderByDescending(x => x.Value).Take(30))
                {
                    Log.LogInfo($"[Types]   {kvp.Key}: {kvp.Value}");
                }
            }
            catch (Exception ex) { Log.LogError($"[Types] {ex.Message}"); }
        }
        
        private static void ExploreRealObject()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                if (typedValues.Count == 0) { Log.LogWarning("[Exp] Nenhum TypedValue"); return; }
                
                // Pegar o primeiro TypedValue com valor
                foreach (var tv in typedValues.Take(50))
                {
                    try
                    {
                        var tvType = tv.GetType();
                        var getMethod = FindGetNonGenericMethod(tvType);
                        if (getMethod == null) continue;
                        
                        var obj = getMethod.Invoke(tv, null);
                        if (obj == null) continue;
                        
                        // Descobrir tipo REAL
                        var realType = obj.GetType();
                        Log.LogInfo($"[Exp] Tipo REAL: {realType.FullName}");
                        
                        // Propriedades do tipo real
                        var props = realType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        Log.LogInfo($"[Exp] {props.Length} propriedades:");
                        
                        foreach (var p in props.Take(15))
                        {
                            try
                            {
                                var v = p.GetValue(obj);
                                var vs = v?.ToString() ?? "null";
                                if (vs.Length > 50) vs = vs.Substring(0, 50) + "...";
                                Log.LogInfo($"[Exp]   {p.Name}: {vs}");
                            }
                            catch (Exception ex) { Log.LogInfo($"[Exp]   {p.Name}: ERRO - {ex.Message}"); }
                        }
                        
                        // Campos do tipo real
                        var fields = realType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                        if (fields.Length > 0)
                        {
                            Log.LogInfo($"[Exp] {fields.Length} campos:");
                            foreach (var f in fields.Take(10))
                            {
                                try
                                {
                                    var v = f.GetValue(obj);
                                    var vs = v?.ToString() ?? "null";
                                    if (vs.Length > 50) vs = vs.Substring(0, 50) + "...";
                                    Log.LogInfo($"[Exp]   {f.Name}: {vs}");
                                }
                                catch { }
                            }
                        }
                        
                        // Tentar Unbox
                        try
                        {
                            var unboxMethod = realType.GetMethod("Unbox");
                            if (unboxMethod != null)
                            {
                                var unboxed = unboxMethod.Invoke(obj, null);
                                if (unboxed != null)
                                {
                                    Log.LogInfo($"[Exp] Unbox: {unboxed.GetType().Name}");
                                }
                            }
                        }
                        catch { }
                        
                        return; // Só explorar o primeiro
                    }
                    catch { }
                }
                
                Log.LogWarning("[Exp] Nenhum valor encontrado");
            }
            catch (Exception ex) { Log.LogError($"[Exp] {ex.Message}"); }
        }
        
        private static void ExportUnwrapped()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                Log.LogInfo($"[Export] {typedValues.Count} TypedValues");
                
                // Coletar valores com tipo real
                var valuesByType = new Dictionary<string, List<Tuple<object, Type>>>();
                
                foreach (var tv in typedValues)
                {
                    try
                    {
                        var tvType = tv.GetType();
                        var getMethod = FindGetNonGenericMethod(tvType);
                        if (getMethod == null) continue;
                        
                        var obj = getMethod.Invoke(tv, null);
                        if (obj == null) continue;
                        
                        var realType = obj.GetType();
                        var typeName = realType.Name;
                        
                        if (!valuesByType.ContainsKey(typeName))
                            valuesByType[typeName] = new List<Tuple<object, Type>>();
                        
                        valuesByType[typeName].Add(Tuple.Create(obj, realType));
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] Tipos encontridos:");
                foreach (var kvp in valuesByType.OrderByDescending(x => x.Value.Count).Take(15))
                {
                    Log.LogInfo($"[Export]   {kvp.Key}: {kvp.Value.Count}");
                }
                
                // Exportar cada tipo
                foreach (var kvp in valuesByType)
                {
                    var typeName = kvp.Key;
                    var items = kvp.Value;
                    
                    if (items.Count == 0) continue;
                    
                    var realType = items[0].Item2;
                    var props = realType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetIndexParameters().Length == 0)
                        .ToList();
                    
                    if (props.Count == 0)
                    {
                        // Tentar campos
                        var fields = realType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                        if (fields.Length == 0) continue;
                        
                        props = fields.Select(f => 
                        {
                            // Criar "propriedade falsa" para campos
                            return typeof(Dummy).GetProperty("DummyProp");
                        }).Where(p => p != null).ToList();
                        
                        if (props.Count == 0) continue;
                    }
                    
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine(string.Join(";", props.Select(p => p.Name)));
                    
                    int exported = 0;
                    foreach (var item in items)
                    {
                        try
                        {
                            var obj = item.Item1;
                            var row = props.Select(p =>
                            {
                                try
                                {
                                    var v = p.GetValue(obj);
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
                    string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                    System.IO.File.WriteAllText(path, csv.ToString());
                    
                    Log.LogInfo($"[Export] ✅ {typeName}: {exported} linhas");
                }
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
    }
    
    internal class Dummy { public object DummyProp { get; set; } }
}
