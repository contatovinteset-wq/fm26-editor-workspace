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
using UnityEngine.UIElements;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.59.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.59.0");
            Log.LogInfo("Offsets do dump.cs aplicados!");
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
            catch { }
        }
        
        private static object _bindingsInstance = null;
        private static int _frameCount = 0;
        private static bool _initialized = false;
        private static List<Dictionary<string, string>> _exportData = null;
        
        // Offsets do dump.cs
        // Bindings.m_data: 0x70
        // Bindings.Data.key: 0x10
        // Bindings.Data.interest: 0x18
        // Bindings.Data.m_value: 0x30
        
        public static void OnUpdate(object __instance)
        {
            try
            {
                if (_bindingsInstance == null && __instance != null) _bindingsInstance = __instance;
                
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[OK] F9=Exportar ativos, F10=Diagnosticar, Ctrl+P=Salvar CSV");
                }
                
                if (!_initialized) return;
                
                try { if (Keyboard.current == null) return; } catch { return; }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Exportar itens ativos");
                    ExportActiveItems();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Diagnosticar estrutura");
                    Diagnose();
                }
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Salvar CSV");
                    SaveCSV();
                }
            }
            catch { }
        }
        
        private static void ExportActiveItems()
        {
            try
            {
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Export] Bindings não capturado");
                    return;
                }
                
                var bindingsType = _bindingsInstance.GetType();
                
                // m_data está no offset 0x70
                // Mas vamos usar reflexão para ser mais seguro
                var mDataField = bindingsType.GetField("m_data", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mDataField == null)
                {
                    // Tentar via propriedade DataSet
                    var dataSetProp = bindingsType.GetProperty("DataSet");
                    if (dataSetProp != null)
                    {
                        var dataSet = dataSetProp.GetValue(_bindingsInstance);
                        Log.LogInfo($"[Export] DataSet: {dataSet?.GetType().Name}");
                        ProcessDataSet(dataSet);
                        return;
                    }
                    
                    Log.LogWarning("[Export] m_data não encontrado");
                    return;
                }
                
                var mData = mDataField.GetValue(_bindingsInstance);
                Log.LogInfo($"[Export] m_data tipo: {mData?.GetType().Name}");
                ProcessDataSet(mData);
            }
            catch (Exception ex) { Log.LogError($"[Export] {ex.Message}"); }
        }
        
        private static void ProcessDataSet(object dataSet)
        {
            try
            {
                if (dataSet == null) { Log.LogWarning("[Process] dataSet null"); return; }
                
                var listType = dataSet.GetType();
                
                // List<T> tem Count e Item indexer
                var countProp = listType.GetProperty("Count");
                var indexerProp = listType.GetProperty("Item");
                
                if (countProp == null || indexerProp == null)
                {
                    Log.LogWarning($"[Process] Não é lista: {listType.Name}");
                    
                    // Tentar como IEnumerable
                    if (dataSet is IEnumerable enumerable)
                    {
                        int count = 0;
                        foreach (var item in enumerable) count++;
                        Log.LogInfo($"[Process] IEnumerable com {count} itens");
                    }
                    return;
                }
                
                int total = (int)countProp.GetValue(dataSet);
                Log.LogInfo($"[Process] {total} itens no DataSet");
                
                // Agrupar dados por tipo de TypedValue
                var typeGroups = new Dictionary<string, int>();
                var activeItems = new List<object>();
                var valuesByType = new Dictionary<string, List<string>>();
                
                for (int i = 0; i < Math.Min(total, 5000); i++)
                {
                    try
                    {
                        var item = indexerProp.GetValue(dataSet, new object[] { i });
                        if (item == null) continue;
                        
                        var itemType = item.GetType();
                        
                        // interest está em 0x18
                        var interestField = itemType.GetField("interest", BindingFlags.Public | BindingFlags.Instance);
                        if (interestField == null) continue;
                        
                        var interest = interestField.GetValue(item);
                        if (interest == null) continue;
                        
                        // Verificar se tem interesse (ativo na UI)
                        var interestCountProp = interest.GetType().GetProperty("Count");
                        if (interestCountProp == null) continue;
                        
                        int interestCount = (int)interestCountProp.GetValue(interest);
                        if (interestCount == 0) continue;
                        
                        // Item ativo! Extrair valor
                        var mValueField = itemType.GetField("m_value", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (mValueField == null) continue;
                        
                        var mValue = mValueField.GetValue(item);
                        if (mValue == null) continue;
                        
                        // key está em 0x10
                        var keyField = itemType.GetField("key", BindingFlags.Public | BindingFlags.Instance);
                        var keyValue = keyField?.GetValue(item);
                        
                        // AsString()
                        var asStringMethod = mValue.GetType().GetMethod("AsString");
                        if (asStringMethod == null) continue;
                        
                        var valueStr = asStringMethod.Invoke(mValue, null)?.ToString() ?? "";
                        
                        // DataType
                        var dataTypeProp = mValue.GetType().GetProperty("DataType");
                        var dataType = dataTypeProp?.GetValue(mValue)?.ToString() ?? "unknown";
                        
                        // Simplificar tipo
                        var shortType = dataType.Split('.').Last().Split('+').First();
                        
                        if (!typeGroups.ContainsKey(shortType)) typeGroups[shortType] = 0;
                        typeGroups[shortType]++;
                        
                        if (!valuesByType.ContainsKey(shortType)) valuesByType[shortType] = new List<string>();
                        
                        // Limitar valores por tipo
                        if (valuesByType[shortType].Count < 100)
                        {
                            valuesByType[shortType].Add(valueStr);
                        }
                        
                        activeItems.Add(item);
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Process] {activeItems.Count} itens ATIVOS (com interest)");
                Log.LogInfo("[Process] Tipos encontrados:");
                foreach (var kvp in typeGroups.OrderByDescending(x => x.Value).Take(20))
                {
                    Log.LogInfo($"[Process]   {kvp.Key}: {kvp.Value}");
                }
                
                // Mostrar exemplos de valores
                Log.LogInfo("[Process] Exemplos de valores:");
                foreach (var kvp in valuesByType.Take(10))
                {
                    var exemplos = string.Join(" | ", kvp.Value.Take(5));
                    Log.LogInfo($"[Process]   {kvp.Key}: {exemplos}");
                }
                
                // Salvar para exportação
                _exportData = new List<Dictionary<string, string>>();
                
                // Agrupar por índice de interesse (tentativa de formar linhas)
                // Cada item com interest indica uma ligação UI - podemos tentar agrupar
                // Por ora, vamos exportar tudo como lista de tipos/valores
                
                foreach (var item in activeItems.Take(1000))
                {
                    try
                    {
                        var row = new Dictionary<string, string>();
                        
                        var mValueField = item.GetType().GetField("m_value", BindingFlags.NonPublic | BindingFlags.Instance);
                        var keyField = item.GetType().GetField("key", BindingFlags.Public | BindingFlags.Instance);
                        
                        var mValue = mValueField?.GetValue(item);
                        var key = keyField?.GetValue(item);
                        
                        if (mValue != null)
                        {
                            var dataTypeProp = mValue.GetType().GetProperty("DataType");
                            var dataType = dataTypeProp?.GetValue(mValue)?.ToString() ?? "";
                            var shortType = dataType.Split('.').Last().Split('+').First();
                            
                            var asStringMethod = mValue.GetType().GetMethod("AsString");
                            var valueStr = asStringMethod?.Invoke(mValue, null)?.ToString() ?? "";
                            
                            row["Type"] = shortType;
                            row["Value"] = valueStr;
                            row["Key"] = key?.ToString() ?? "";
                            
                            _exportData.Add(row);
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Process] {_exportData.Count} itens preparados para exportação");
            }
            catch (Exception ex) { Log.LogError($"[Process] {ex.Message}"); }
        }
        
        private static void Diagnose()
        {
            try
            {
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Diag] Bindings não capturado");
                    return;
                }
                
                var type = _bindingsInstance.GetType();
                Log.LogInfo($"[Diag] Bindings tipo: {type.FullName}");
                
                // Listar todos os campos
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Log.LogInfo($"[Diag] {fields.Length} campos:");
                
                foreach (var f in fields.Take(30))
                {
                    try
                    {
                        var val = f.GetValue(_bindingsInstance);
                        var valType = val?.GetType().Name ?? "null";
                        Log.LogInfo($"[Diag]   {f.Name} ({valType})");
                    }
                    catch { }
                }
                
                // Listar propriedades
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[Diag] {props.Length} propriedades:");
                
                foreach (var p in props.Take(20))
                {
                    try
                    {
                        var val = p.GetValue(_bindingsInstance);
                        var valType = val?.GetType().Name ?? "null";
                        Log.LogInfo($"[Diag]   {p.Name} ({valType})");
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Log.LogError($"[Diag] {ex.Message}"); }
        }
        
        private static void SaveCSV()
        {
            try
            {
                if (_exportData == null || _exportData.Count == 0)
                {
                    Log.LogWarning("[CSV] Nenhum dado. Aperte F9 primeiro.");
                    return;
                }
                
                // Agrupar por tipo
                var byType = _exportData.GroupBy(x => x.GetValueOrDefault("Type", "unknown")).ToList();
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Type;Value;Key");
                
                foreach (var row in _exportData)
                {
                    var type = row.GetValueOrDefault("Type", "").Replace(";", ",");
                    var value = row.GetValueOrDefault("Value", "").Replace(";", ",").Replace("\n", " ").Replace("\r", "");
                    var key = row.GetValueOrDefault("Key", "").Replace(";", ",");
                    csv.AppendLine($"{type};{value};{key}");
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Active_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                
                Log.LogInfo($"[CSV] ✅ {_exportData.Count} linhas -> {path}");
            }
            catch (Exception ex) { Log.LogError($"[CSV] {ex.Message}"); }
        }
    }
}
