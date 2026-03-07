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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.54.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.54.0 CARREGADO!");
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
                
                // F9 - Testa GetEnumerator nas listas
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Testar GetEnumerator");
                    TestGetEnumerator();
                }
                
                // F10 - Lista TODOS os métodos de um TypedValue
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Listar métodos do objeto Get()");
                    ListMethodsOfGetObject();
                }
                
                // Ctrl+P - Exporta usando nova abordagem
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar via GetEnumerator");
                    ExportViaEnumerator();
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
        
        private static void TestGetEnumerator()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) { Log.LogWarning("[Enum] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                int total = countProp != null ? (int)countProp.GetValue(mData) : 0;
                Log.LogInfo($"[Enum] Total via Count: {total}");
                
                // Procurar List<TypedValue>
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer?.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var mValueProp = item.GetType().GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue == null) continue;
                        
                        var asString = CallAsString(mValue);
                        if (!asString.Contains("List`1[SI.Core.TypedValue]")) continue;
                        
                        Log.LogInfo($"[Enum] === Lista em índice {i} ===");
                        
                        var obj = CallGet(mValue);
                        if (obj == null) { Log.LogWarning("[Enum] Get() null"); continue; }
                        
                        var objType = obj.GetType();
                        Log.LogInfo($"[Enum] Tipo: {objType.FullName}");
                        
                        // Tentar GetEnumerator
                        var getEnumMethod = objType.GetMethod("GetEnumerator");
                        if (getEnumMethod != null)
                        {
                            Log.LogInfo("[Enum] GetEnumerator() encontrado!");
                            
                            try
                            {
                                var enumerator = getEnumMethod.Invoke(obj, null);
                                if (enumerator != null)
                                {
                                    Log.LogInfo($"[Enum] Enumerator tipo: {enumerator.GetType().FullName}");
                                    
                                    // Métodos do enumerator
                                    var enumType = enumerator.GetType();
                                    var moveNextMethod = enumType.GetMethod("MoveNext");
                                    var currentProp = enumType.GetProperty("Current");
                                    
                                    if (moveNextMethod != null && currentProp != null)
                                    {
                                        Log.LogInfo("[Enum] Iterando:");
                                        int count = 0;
                                        
                                        while ((bool)moveNextMethod.Invoke(enumerator, null) && count < 20)
                                        {
                                            try
                                            {
                                                var current = currentProp.GetValue(enumerator);
                                                if (current == null) { Log.LogInfo($"[Enum]   [{count}]: null"); }
                                                else
                                                {
                                                    var currentAsString = CallAsString(current);
                                                    Log.LogInfo($"[Enum]   [{count}]: {currentAsString}");
                                                }
                                            }
                                            catch (Exception ex) { Log.LogInfo($"[Enum]   [{count}]: ERRO - {ex.Message}"); }
                                            
                                            count++;
                                        }
                                        
                                        Log.LogInfo($"[Enum] Total iterado: {count}");
                                    }
                                    else
                                    {
                                        Log.LogWarning("[Enum] MoveNext/Current não encontrados");
                                    }
                                }
                            }
                            catch (Exception ex) { Log.LogError($"[Enum] GetEnumerator erro: {ex.Message}"); }
                        }
                        else
                        {
                            Log.LogWarning("[Enum] GetEnumerator() não encontrado");
                        }
                        
                        // Listar TODOS os métodos do objeto
                        var allMethods = objType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        Log.LogInfo($"[Enum] {allMethods.Length} métodos públicos:");
                        foreach (var m in allMethods.Take(20))
                        {
                            Log.LogInfo($"[Enum]   {m.ReturnType.Name} {m.Name}({m.GetParameters().Length})");
                        }
                        
                        return; // Só testa o primeiro
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Log.LogError($"[Enum] {ex.Message}"); }
        }
        
        private static void ListMethodsOfGetObject()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) return;
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                
                // Pegar primeiro TypedValue com dados
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var mValueProp = item.GetType().GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue == null) continue;
                        
                        var obj = CallGet(mValue);
                        if (obj == null) continue;
                        
                        var objType = obj.GetType();
                        Log.LogInfo($"[Meth] Objeto tipo: {objType.FullName}");
                        
                        // TODOS os métodos
                        var methods = objType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        Log.LogInfo($"[Meth] {methods.Length} métodos:");
                        
                        foreach (var m in methods)
                        {
                            var pars = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name));
                            Log.LogInfo($"[Meth]   {m.ReturnType.Name} {m.Name}({pars})");
                        }
                        
                        // TODOS os campos
                        var fields = objType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        Log.LogInfo($"[Meth] {fields.Length} campos:");
                        
                        foreach (var f in fields)
                        {
                            try
                            {
                                var v = f.GetValue(obj);
                                Log.LogInfo($"[Meth]   {f.FieldType.Name} {f.Name} = {v}");
                            }
                            catch { Log.LogInfo($"[Meth]   {f.FieldType.Name} {f.Name}"); }
                        }
                        
                        return;
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Log.LogError($"[Meth] {ex.Message}"); }
        }
        
        private static void ExportViaEnumerator()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) return;
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                
                var allValues = new List<string>();
                
                for (int i = 0; i < total; i++)
                {
                    try
                    {
                        var item = indexer.GetValue(mData, new object[] { i });
                        if (item == null) continue;
                        
                        var mValueProp = item.GetType().GetProperty("m_value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        if (mValueProp == null) continue;
                        
                        var mValue = mValueProp.GetValue(item);
                        if (mValue == null) continue;
                        
                        var asString = CallAsString(mValue);
                        
                        // Se for lista, iterar
                        if (asString.Contains("List`1[SI.Core.TypedValue]"))
                        {
                            var obj = CallGet(mValue);
                            if (obj == null) continue;
                            
                            var getEnumMethod = obj.GetType().GetMethod("GetEnumerator");
                            if (getEnumMethod == null) continue;
                            
                            var enumerator = getEnumMethod.Invoke(obj, null);
                            if (enumerator == null) continue;
                            
                            var enumType = enumerator.GetType();
                            var moveNextMethod = enumType.GetMethod("MoveNext");
                            var currentProp = enumType.GetProperty("Current");
                            
                            if (moveNextMethod == null || currentProp == null) continue;
                            
                            var rowValues = new List<string>();
                            
                            while ((bool)moveNextMethod.Invoke(enumerator, null))
                            {
                                try
                                {
                                    var current = currentProp.GetValue(enumerator);
                                    if (current != null)
                                    {
                                        var currentAsString = CallAsString(current);
                                        rowValues.Add(currentAsString);
                                    }
                                }
                                catch { }
                            }
                            
                            if (rowValues.Count > 0)
                            {
                                allValues.Add(string.Join(";", rowValues));
                            }
                        }
                        else
                        {
                            allValues.Add(asString);
                        }
                    }
                    catch { }
                }
                
                if (allValues.Count == 0)
                {
                    Log.LogWarning("[Export] Nenhum valor");
                    return;
                }
                
                var csv = new System.Text.StringBuilder();
                foreach (var v in allValues)
                {
                    csv.AppendLine(v.Replace("\n", " ").Replace("\r", ""));
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {allValues.Count} linhas");
                Log.LogInfo($"[Export] Arquivo: {path}");
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
    }
}
