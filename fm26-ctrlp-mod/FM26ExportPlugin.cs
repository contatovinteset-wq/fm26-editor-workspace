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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.48.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.48.0 CARREGADO!");
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
                        Log.LogInfo(">>> F9 - Listar métodos Get disponíveis");
                        ListGetMethods();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F9] {ex.Message}"); }
                
                try
                {
                    if (Keyboard.current.f10Key.wasPressedThisFrame)
                    {
                        Log.LogInfo(">>> F10 - Testar Get() específico");
                        TestGetSpecific();
                    }
                }
                catch (Exception ex) { Log.LogError($"[F10] {ex.Message}"); }
                
                try
                {
                    bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                    bool p = Keyboard.current.pKey.wasPressedThisFrame;
                    
                    if (ctrl && p)
                    {
                        Log.LogInfo(">>> Ctrl+P - Exportar valores");
                        ExportValues();
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
            // Buscar método Get() que retorna Object e não é genérico
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
            
            // Tentar pelo nome completo
            return methods.FirstOrDefault(m => 
                m.Name == "Get" && 
                m.GetParameters().Length == 0 && 
                !m.ContainsGenericParameters);
        }
        
        private static void ListGetMethods()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                Log.LogInfo($"[Methods] {typedValues.Count} TypedValues");
                
                if (typedValues.Count == 0) { Log.LogWarning("[Methods] Nenhum"); return; }
                
                var tv = typedValues[0];
                var tvType = tv.GetType();
                
                Log.LogInfo($"[Methods] Tipo: {tvType.FullName}");
                
                var methods = tvType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                var getMethods = methods.Where(m => m.Name == "Get").ToList();
                
                Log.LogInfo($"[Methods] {getMethods.Count} métodos 'Get':");
                
                foreach (var m in getMethods)
                {
                    var pars = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    var genArgs = m.IsGenericMethod ? $"<{string.Join(", ", m.GetGenericArguments().Select(a => a.Name))}>" : "";
                    Log.LogInfo($"[Methods]   Get{genArgs}({pars}) -> {m.ReturnType.Name} (IsGeneric: {m.IsGenericMethod})");
                }
                
                // Encontrar o método correto
                var getMethod = FindGetNonGenericMethod(tvType);
                if (getMethod != null)
                {
                    Log.LogInfo($"[Methods] ✅ Método encontrado: {getMethod}");
                    
                    // Testar
                    try
                    {
                        var result = getMethod.Invoke(tv, null);
                        if (result != null)
                        {
                            Log.LogInfo($"[Methods] Resultado: {result.GetType().Name}");
                        }
                        else
                        {
                            Log.LogInfo($"[Methods] Resultado: null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"[Methods] Erro ao invocar: {ex.Message}");
                    }
                }
                else
                {
                    Log.LogWarning("[Methods] ❌ Método Get() não-genérico não encontrado");
                }
            }
            catch (Exception ex) { Log.LogError($"[Methods] {ex.Message}"); }
        }
        
        private static void TestGetSpecific()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                if (typedValues.Count == 0) { Log.LogWarning("[Test] Nenhum TypedValue"); return; }
                
                int success = 0;
                int tested = 0;
                
                foreach (var tv in typedValues.Take(30))
                {
                    try
                    {
                        var tvType = tv.GetType();
                        var getMethod = FindGetNonGenericMethod(tvType);
                        
                        if (getMethod == null) continue;
                        
                        tested++;
                        var result = getMethod.Invoke(tv, null);
                        
                        if (result == null)
                        {
                            Log.LogInfo($"[Test] [{tested}] = null");
                            continue;
                        }
                        
                        success++;
                        var resultType = result.GetType();
                        Log.LogInfo($"[Test] [{tested}] {resultType.Name}");
                        
                        // Propriedades
                        var props = resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        Log.LogInfo($"[Test]   {props.Length} props");
                        
                        foreach (var p in props.Take(6))
                        {
                            try
                            {
                                var v = p.GetValue(result);
                                var vs = v?.ToString() ?? "null";
                                if (vs.Length > 40) vs = vs.Substring(0, 40) + "...";
                                Log.LogInfo($"[Test]     {p.Name}: {vs}");
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"[Test] Erro: {ex.Message}");
                    }
                }
                
                Log.LogInfo($"[Test] Sucesso: {success}/{tested}");
            }
            catch (Exception ex) { Log.LogError($"[Test] {ex.Message}"); }
        }
        
        private static void ExportValues()
        {
            try
            {
                var typedValues = GetAllTypedValues();
                Log.LogInfo($"[Export] {typedValues.Count} TypedValues");
                
                // Coletar valores
                var values = new List<object>();
                var typeCounts = new Dictionary<string, int>();
                
                foreach (var tv in typedValues)
                {
                    try
                    {
                        var tvType = tv.GetType();
                        var getMethod = FindGetNonGenericMethod(tvType);
                        
                        if (getMethod == null) continue;
                        
                        var result = getMethod.Invoke(tv, null);
                        if (result == null) continue;
                        
                        values.Add(result);
                        
                        var typeName = result.GetType().Name;
                        if (!typeCounts.ContainsKey(typeName)) typeCounts[typeName] = 0;
                        typeCounts[typeName]++;
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {values.Count} valores extraídos");
                
                foreach (var kvp in typeCounts.OrderByDescending(x => x.Value).Take(15))
                {
                    Log.LogInfo($"[Export]   {kvp.Key}: {kvp.Value}");
                }
                
                if (values.Count == 0) { Log.LogWarning("[Export] Nenhum valor"); return; }
                
                // Agrupar por tipo e exportar
                var byType = values.GroupBy(v => v.GetType().Name).ToDictionary(g => g.Key, g => g.ToList());
                
                foreach (var kvp in byType)
                {
                    var typeName = kvp.Key;
                    var items = kvp.Value;
                    
                    if (items.Count == 0) continue;
                    
                    var first = items[0];
                    var props = first.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetIndexParameters().Length == 0)
                        .ToList();
                    
                    if (props.Count == 0) continue;
                    
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
                                    var v = p.GetValue(item);
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
}
