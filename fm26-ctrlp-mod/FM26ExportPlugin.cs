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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.35.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        internal static object _bindingsInstance = null;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.35.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            
            // Bindings está no namespace global, não em SI.Bindable
            var bindingsType = Type.GetType("Bindings, SI.Bindable");
            if (bindingsType != null)
            {
                Log.LogInfo($"[Init] Bindings type: {bindingsType.FullName}");
                
                // Hook no construtor para capturar instância
                var ctor = bindingsType.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    var ctorPatch = typeof(Plugin).GetMethod("OnBindingsCtor", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(ctor, postfix: new HarmonyMethod(ctorPatch));
                    Log.LogInfo("[Init] Hooked Bindings.ctor()");
                }
                
                // Hook no Update
                var updateMethod = bindingsType.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
                if (updateMethod != null)
                {
                    var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                    Log.LogInfo("[Init] Patched Bindings.Update");
                }
            }
            else
            {
                Log.LogWarning("[Init] Bindings type não encontrado");
            }
        }
        
        public static void OnBindingsCtor(object __instance)
        {
            _bindingsInstance = __instance;
            Log.LogInfo($"[Hook] Bindings instance capturada: {__instance.GetType().Name}");
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
                    if (_bindingsInstance != null)
                    {
                        Log.LogInfo($"[Init] Bindings capturada: {_bindingsInstance.GetType().Name}");
                    }
                }
                
                if (!_initialized || Keyboard.current == null) return;
                
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Log.LogInfo(">>> Ctrl+P - Exportar via Bindings.DataSet");
                    ExportViaBindingsDataSet();
                }
                
                if (Keyboard.current.f9Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F9 - Listar tipos em Bindings.DataSet");
                    ListBindingsDataSet();
                }
                
                if (Keyboard.current.f10Key.wasPressedThisFrame)
                {
                    Log.LogInfo(">>> F10 - Listar todos os tipos no assembly SI.Bindable");
                    ListAllTypesInBindable();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void ListAllTypesInBindable()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "SI.Bindable");
                
                if (asm == null)
                {
                    Log.LogWarning("[Asm] SI.Bindable não encontrado");
                    return;
                }
                
                var types = asm.GetTypes();
                Log.LogInfo($"[Asm] {types.Length} tipos em SI.Bindable");
                
                // Buscar tipos com Bindings, Data, TypedValue no nome
                var relevant = types.Where(t => 
                {
                    var name = t.Name.ToLower();
                    return name.Contains("bindings") || name.Contains("typedvalue") || 
                           name.Contains("ireadonlydata") || name.Contains("idata");
                }).Take(20);
                
                foreach (var t in relevant)
                {
                    Log.LogInfo($"[Asm] {t.FullName} ({t.Name})");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Asm] Erro: {ex.Message}");
            }
        }
        
        private static void ListBindingsDataSet()
        {
            try
            {
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Bind] Nenhuma instância de Bindings capturada");
                    return;
                }
                
                var bindingsType = _bindingsInstance.GetType();
                Log.LogInfo($"[Bind] Tipo: {bindingsType.FullName}");
                
                // Propriedades públicas
                var props = bindingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[Bind] {props.Length} propriedades:");
                
                foreach (var p in props.Take(20))
                {
                    Log.LogInfo($"[Bind]   {p.Name}: {p.PropertyType.Name}");
                }
                
                // Buscar DataSet
                var dataSetProp = props.FirstOrDefault(p => p.Name == "DataSet");
                if (dataSetProp != null)
                {
                    var dataSet = dataSetProp.GetValue(_bindingsInstance);
                    if (dataSet is IEnumerable en)
                    {
                        int count = 0;
                        var types = new Dictionary<string, int>();
                        
                        foreach (var item in en)
                        {
                            count++;
                            if (count >= 5000) break;
                            
                            var itemType = item?.GetType().Name ?? "null";
                            if (!types.ContainsKey(itemType))
                                types[itemType] = 0;
                            types[itemType]++;
                        }
                        
                        Log.LogInfo($"[Bind] DataSet: {count} itens");
                        
                        foreach (var kvp in types.OrderByDescending(x => x.Value).Take(15))
                        {
                            Log.LogInfo($"[Bind]   {kvp.Key}: {kvp.Value}");
                        }
                    }
                }
                else
                {
                    Log.LogWarning("[Bind] Propriedade DataSet não encontrada");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Bind] Erro: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void ExportViaBindingsDataSet()
        {
            try
            {
                if (_bindingsInstance == null)
                {
                    Log.LogWarning("[Export] Nenhuma instância de Bindings capturada");
                    return;
                }
                
                var bindingsType = _bindingsInstance.GetType();
                var dataSetProp = bindingsType.GetProperty("DataSet", BindingFlags.Public | BindingFlags.Instance);
                
                if (dataSetProp == null)
                {
                    Log.LogWarning("[Export] DataSet não encontrado");
                    return;
                }
                
                var dataSet = dataSetProp.GetValue(_bindingsInstance);
                if (!(dataSet is IEnumerable en))
                {
                    Log.LogWarning("[Export] DataSet não é IEnumerable");
                    return;
                }
                
                // Buscar tipos no assembly
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "SI.Bindable");
                
                var iReadOnlyData = asm?.GetTypes().FirstOrDefault(t => t.Name == "IReadOnlyData");
                var typedValueType = asm?.GetTypes().FirstOrDefault(t => t.Name == "TypedValue");
                
                if (iReadOnlyData == null || typedValueType == null)
                {
                    Log.LogWarning("[Export] Tipos não encontrados");
                    return;
                }
                
                var valueProp = iReadOnlyData.GetProperty("Value");
                var dataTypeProp = typedValueType.GetProperty("DataType", BindingFlags.Public | BindingFlags.Instance);
                
                if (valueProp == null || dataTypeProp == null)
                {
                    Log.LogWarning("[Export] Propriedades não encontradas");
                    return;
                }
                
                Log.LogInfo($"[Export] IReadOnlyData.Value: {valueProp.PropertyType.Name}");
                Log.LogInfo($"[Export] TypedValue.DataType: {dataTypeProp.PropertyType.Name}");
                
                // Coletar dados
                var dataByType = new Dictionary<string, List<object>>();
                int total = 0;
                
                foreach (var item in en)
                {
                    total++;
                    if (total >= 10000) break;
                    
                    try
                    {
                        var value = valueProp.GetValue(item);
                        if (value == null) continue;
                        
                        var dataType = (Type)dataTypeProp.GetValue(value);
                        var typeName = dataType?.Name ?? "null";
                        
                        // Filtrar tipos relevantes
                        var lower = typeName.ToLower();
                        if (lower.Contains("player") || lower.Contains("person") ||
                            lower.Contains("squad") || lower.Contains("club") ||
                            lower.Contains("team") || lower.Contains("match") ||
                            lower.Contains("string") || lower.Contains("int"))
                        {
                            if (!dataByType.ContainsKey(typeName))
                                dataByType[typeName] = new List<object>();
                            dataByType[typeName].Add(value);
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {total} itens processados");
                
                foreach (var kvp in dataByType.OrderByDescending(x => x.Value.Count).Take(15))
                {
                    Log.LogInfo($"[Export]   {kvp.Key}: {kvp.Value.Count}");
                }
                
                // Exportar se encontrou dados suficientes
                var playerData = dataByType.FirstOrDefault(x => 
                    x.Key.ToLower().Contains("player") || x.Key.ToLower().Contains("person"));
                
                if (playerData.Value != null && playerData.Value.Count > 5)
                {
                    ExportTypedValues(playerData.Key, playerData.Value);
                }
                else
                {
                    Log.LogWarning("[Export] Dados insuficientes para exportar");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void ExportTypedValues(string typeName, List<object> typedValues)
        {
            try
            {
                var csv = new System.Text.StringBuilder();
                csv.AppendLine($"Index;Type;Value");
                
                int count = 0;
                foreach (var tv in typedValues.Take(500))
                {
                    csv.AppendLine($"{count};{typeName};{tv?.ToString() ?? "null"}");
                    count++;
                }
                
                string path = System.IO.Path.Combine(BepInEx.Paths.PluginPath, $"FM26_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                System.IO.File.WriteAllText(path, csv.ToString());
                Log.LogInfo($"[CSV] ✅ {count} linhas: {path}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[CSV] Erro: {ex.Message}");
            }
        }
    }
}
