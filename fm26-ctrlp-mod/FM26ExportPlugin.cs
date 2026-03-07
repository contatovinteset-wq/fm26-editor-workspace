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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.55.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.55.0");
            Log.LogInfo("Base de Dados de Jogadores");
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
                        Log.LogInfo("[OK] Hook ativo");
                    }
                }
            }
            catch (Exception ex) { Log.LogError($"[ERRO] {ex.Message}"); }
        }
        
        private static object _bindingsInstance = null;
        private static int _frameCount = 0;
        private static bool _initialized = false;
        private static List<Dictionary<string, string>> _lastTableData = null;
        
        public static void OnUpdate(object __instance)
        {
            try
            {
                if (_bindingsInstance == null && __instance != null)
                {
                    _bindingsInstance = __instance;
                    Log.LogInfo("[OK] Bindings capturado");
                }
                
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[OK] Sistema pronto - F9=Buscar tabela, Ctrl+P=Exportar");
                }
                
                if (!_initialized) return;
                
                try { if (Keyboard.current == null) return; }
                catch { return; }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Buscar tabela de jogadores");
                    FindPlayerTable();
                }
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar tabela");
                    ExportTable();
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
        
        private static string AsString(object typedValue)
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
        
        private static string GetTypeName(object obj)
        {
            try
            {
                var getIl2CppType = obj.GetType().GetMethod("GetIl2CppType");
                if (getIl2CppType != null)
                {
                    var t = getIl2CppType.Invoke(obj, null);
                    return t?.ToString() ?? "Unknown";
                }
            }
            catch { }
            return obj.GetType().Name;
        }
        
        private static void FindPlayerTable()
        {
            try
            {
                var mData = GetMData();
                if (mData == null) { Log.LogWarning("[Tabela] m_data null"); return; }
                
                var listType = mData.GetType();
                var countProp = listType.GetProperty("Count");
                var indexer = listType.GetProperty("Item");
                
                if (countProp == null || indexer == null) return;
                
                int total = (int)countProp.GetValue(mData);
                Log.LogInfo($"[Tabela] {total} itens no sistema");
                
                // Buscar estruturas que parecem ser linhas de tabela
                // List<TypedValue> com múltiplos itens = provavelmente uma linha
                var tableRows = new List<List<string>>();
                
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
                        
                        var asString = AsString(mValue);
                        
                        // List<TypedValue> = linha de tabela
                        if (asString.Contains("List`1[SI.Core.TypedValue]"))
                        {
                            var row = ExtractListValues(mValue);
                            if (row.Count >= 5 && row.Count <= 30) // Linha de tabela típica
                            {
                                tableRows.Add(row);
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Tabela] {tableRows.Count} linhas encontradas");
                
                if (tableRows.Count > 0)
                {
                    // Mostrar estrutura da primeira linha
                    Log.LogInfo("[Tabela] Primeira linha:");
                    var firstRow = tableRows[0];
                    for (int j = 0; j < firstRow.Count; j++)
                    {
                        Log.LogInfo($"[Tabela]   Col {j}: {firstRow[j]}");
                    }
                    
                    _lastTableData = tableRows.Select((row, idx) => 
                    {
                        var dict = new Dictionary<string, string>();
                        for (int j = 0; j < row.Count; j++)
                        {
                            dict[$"Col{j}"] = row[j];
                        }
                        return dict;
                    }).ToList();
                }
            }
            catch (Exception ex) { Log.LogError($"[Tabela] {ex.Message}"); }
        }
        
        private static List<string> ExtractListValues(object typedValue)
        {
            var result = new List<string>();
            
            try
            {
                var obj = CallGet(typedValue);
                if (obj == null) return result;
                
                var objType = obj.GetType();
                
                // Tentar Cast para IEnumerable
                var castMethod = objType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "Cast" && m.IsGenericMethod);
                
                if (castMethod != null)
                {
                    var genericCast = castMethod.MakeGenericMethod(typeof(object));
                    var enumerable = genericCast.Invoke(obj, null) as IEnumerable;
                    
                    if (enumerable != null)
                    {
                        foreach (var item in enumerable)
                        {
                            if (item != null)
                            {
                                // É um TypedValue?
                                var itemAsString = item.GetType().GetMethod("AsString");
                                if (itemAsString != null)
                                {
                                    var val = itemAsString.Invoke(item, null)?.ToString() ?? "";
                                    result.Add(val);
                                }
                                else
                                {
                                    result.Add(item.ToString());
                                }
                            }
                        }
                        return result;
                    }
                }
                
                // Fallback: tentar acessar via reflection
                var fields = objType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    try
                    {
                        var val = f.GetValue(obj);
                        if (val != null)
                        {
                            result.Add(val.ToString());
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            return result;
        }
        
        private static void ExportTable()
        {
            try
            {
                if (_lastTableData == null || _lastTableData.Count == 0)
                {
                    Log.LogWarning("[Export] Nenhuma tabela encontrada. Aperte F9 primeiro.");
                    return;
                }
                
                // Detectar colunas
                var allCols = new HashSet<string>();
                foreach (var row in _lastTableData)
                {
                    foreach (var key in row.Keys) allCols.Add(key);
                }
                
                var sortedCols = allCols.OrderBy(c => int.Parse(c.Replace("Col", ""))).ToList();
                
                // CSV
                var csv = new System.Text.StringBuilder();
                csv.AppendLine(string.Join(";", sortedCols));
                
                foreach (var row in _lastTableData)
                {
                    var values = sortedCols.Select(c => 
                    {
                        if (row.TryGetValue(c, out var v))
                        {
                            return v.Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                        }
                        return "";
                    });
                    csv.AppendLine(string.Join(";", values));
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Jogadores_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[Export] ✅ {_lastTableData.Count} jogadores exportados");
                Log.LogInfo($"[Export] Arquivo: {path}");
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
    }
}
