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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.46.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.46.0 CARREGADO!");
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
                        Log.LogInfo(">>> F9 - Listar DataTypes únicos");
                        ListDataTypes();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Explorar métodos do TypedValue");
                        ExploreTypedValueMethods();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F10] {ex.Message}"); }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P - Extrair valores reais");
                        ExtractRealValues();
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
        
        private static void ListDataTypes()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                Log.LogInfo($"[Types] {typedValues.Count} TypedValues");
                
                var typeCounts = new Dictionary<string, int>();
                
                foreach (var tv in typedValues)
                {
                    try
                    {
                        var dataTypeProp = tv.GetType().GetProperty("DataType");
                        if (dataTypeProp == null) continue;
                        
                        var dt = dataTypeProp.GetValue(tv);
                        var name = dt?.ToString() ?? "null";
                        
                        // Simplificar nome
                        if (name.Contains(","))
                        {
                            var parts = name.Split(',');
                            name = parts[0];
                        }
                        
                        if (!typeCounts.ContainsKey(name)) typeCounts[name] = 0;
                        typeCounts[name]++;
                    }
                    catch { }
                }
                
                Log.LogInfo("[Types] Tipos encontrados:");
                foreach (var kvp in typeCounts.OrderByDescending(x => x.Value).Take(30))
                {
                    Log.LogInfo($"[Types]   {kvp.Key}: {kvp.Value}");
                }
            }
            catch (Exception ex) { Log.LogError($"[Types] {ex.Message}"); }
        }
        
        private static void ExploreTypedValueMethods()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                if (typedValues.Count == 0) { Log.LogWarning("[Meth] Nenhum TypedValue"); return; }
                
                var tv = typedValues[0];
                var tvType = tv.GetType();
                
                Log.LogInfo($"[Meth] Tipo: {tvType.FullName}");
                
                // Métodos
                var methods = tvType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[Meth] {methods.Length} métodos:");
                
                foreach (var m in methods)
                {
                    if (m.DeclaringType == tvType || m.DeclaringType?.Name == "TypedValue")
                    {
                        var pars = string.Join(", ", m.GetParameters().Select(p => p.Name));
                        Log.LogInfo($"[Meth]   {m.Name}({pars}) -> {m.ReturnType.Name}");
                    }
                }
                
                // Campos
                var fields = tvType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                Log.LogInfo($"[Meth] {fields.Length} campos:");
                
                foreach (var f in fields)
                {
                    Log.LogInfo($"[Meth]   {f.Name}: {f.FieldType.Name}");
                }
                
                // Tentar GetValue
                try
                {
                    var getValueMethod = tvType.GetMethod("GetValue");
                    if (getValueMethod != null)
                    {
                        var result = getValueMethod.Invoke(tv, null);
                        if (result != null)
                        {
                            Log.LogInfo($"[Meth] GetValue() = {result.GetType().Name}");
                            
                            // Explorar resultado
                            var resultType = result.GetType();
                            var resultProps = resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            Log.LogInfo($"[Meth] Resultado tem {resultProps.Length} propriedades:");
                            
                            foreach (var p in resultProps.Take(10))
                            {
                                try
                                {
                                    var v = p.GetValue(result);
                                    var vs = v?.ToString() ?? "null";
                                    if (vs.Length > 50) vs = vs.Substring(0, 50) + "...";
                                    Log.LogInfo($"[Meth]   {p.Name}: {vs}");
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch (Exception ex) { Log.LogWarning($"[Meth] GetValue erro: {ex.Message}"); }
            }
            catch (Exception ex) { Log.LogError($"[Meth] {ex.Message}"); }
        }
        
        private static void ExtractRealValues()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                Log.LogInfo($"[Extract] {typedValues.Count} TypedValues");
                
                // Encontrar TypedValues com dados de jogador
                int extracted = 0;
                
                foreach (var tv in typedValues.Take(100))
                {
                    try
                    {
                        var tvType = tv.GetType();
                        
                        // Tentar GetValue
                        var getValueMethod = tvType.GetMethod("GetValue");
                        if (getValueMethod == null) continue;
                        
                        var result = getValueMethod.Invoke(tv, null);
                        if (result == null) continue;
                        
                        var resultType = result.GetType();
                        
                        // Se tem propriedades que parecem dados de jogador
                        var props = resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        var propNames = props.Select(p => p.Name.ToLower()).ToList();
                        
                        bool hasPlayerProps = propNames.Any(n => 
                            n.Contains("name") || n.Contains("age") || n.Contains("club") || 
                            n.Contains("position") || n.Contains("value") || n.Contains("rating"));
                        
                        if (hasPlayerProps)
                        {
                            Log.LogInfo($"[Extract] Tipo: {resultType.Name}");
                            extracted++;
                            
                            foreach (var p in props.Take(15))
                            {
                                try
                                {
                                    var v = p.GetValue(result);
                                    var vs = v?.ToString() ?? "null";
                                    if (vs.Length > 60) vs = vs.Substring(0, 60) + "...";
                                    Log.LogInfo($"[Extract]   {p.Name}: {vs}");
                                }
                                catch { }
                            }
                            
                            if (extracted >= 5) break;
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Extract] {extracted} itens com dados relevantes");
            }
            catch (Exception ex) { Log.LogError($"[Extract] {ex.Message}"); }
        }
    }
}
