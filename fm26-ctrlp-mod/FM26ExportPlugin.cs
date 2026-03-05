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
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.34.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v2.34.0 CARREGADO!");
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
        
        public static void OnUpdate()
        {
            try
            {
                _frameCount++;
                if (!_initialized && _frameCount == 300)
                {
                    _initialized = true;
                    Log.LogInfo("[Init] Pronto!");
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
                    Log.LogInfo(">>> F10 - Investigar TypedValue");
                    InvestigateTypedValue();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[OnUpdate] Erro: {ex.Message}");
            }
        }
        
        private static void ListBindingsDataSet()
        {
            try
            {
                // Buscar tipo Bindings
                var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
                if (bindingsType == null)
                {
                    Log.LogWarning("[Bind] Tipo Bindings não encontrado");
                    return;
                }
                
                // Buscar propriedade DataSet
                var dataSetProp = bindingsType.GetProperty("DataSet", BindingFlags.Public | BindingFlags.Instance);
                if (dataSetProp == null)
                {
                    Log.LogWarning("[Bind] Propriedade DataSet não encontrada");
                    return;
                }
                
                Log.LogInfo($"[Bind] DataSet encontrado: {dataSetProp.PropertyType.Name}");
                
                // Buscar instância ativa de Bindings
                var allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                object bindingsInstance = null;
                
                foreach (var obj in allObjects)
                {
                    if (obj == null) continue;
                    var type = obj.GetType();
                    if (type == bindingsType || type.BaseType == bindingsType)
                    {
                        bindingsInstance = obj;
                        Log.LogInfo($"[Bind] Instância encontrada: {type.Name}");
                        break;
                    }
                }
                
                if (bindingsInstance == null)
                {
                    Log.LogWarning("[Bind] Nenhuma instância de Bindings encontrada");
                    return;
                }
                
                // Obter DataSet
                var dataSet = dataSetProp.GetValue(bindingsInstance);
                if (dataSet == null)
                {
                    Log.LogWarning("[Bind] DataSet é null");
                    return;
                }
                
                Log.LogInfo($"[Bind] DataSet tipo: {dataSet.GetType().Name}");
                
                // DataSet deve ser IReadOnlyList<IReadOnlyData>
                if (dataSet is IEnumerable en)
                {
                    int count = 0;
                    var typeCounts = new Dictionary<string, int>();
                    
                    foreach (var item in en)
                    {
                        count++;
                        if (count >= 10000) break;
                        
                        var itemType = item?.GetType().Name ?? "null";
                        if (!typeCounts.ContainsKey(itemType))
                            typeCounts[itemType] = 0;
                        typeCounts[itemType]++;
                    }
                    
                    Log.LogInfo($"[Bind] DataSet: {count} itens");
                    
                    foreach (var kvp in typeCounts.OrderByDescending(x => x.Value).Take(20))
                    {
                        Log.LogInfo($"[Bind]   {kvp.Key}: {kvp.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Bind] Erro: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void InvestigateTypedValue()
        {
            try
            {
                // Buscar tipo IReadOnlyData
                var iReadOnlyData = Type.GetType("SI.Bindable.IReadOnlyData, SI.Bindable");
                if (iReadOnlyData == null)
                {
                    Log.LogWarning("[TV] IReadOnlyData não encontrado");
                    return;
                }
                
                // Propriedade Value
                var valueProp = iReadOnlyData.GetProperty("Value");
                if (valueProp == null)
                {
                    Log.LogWarning("[TV] Propriedade Value não encontrada");
                    return;
                }
                
                Log.LogInfo($"[TV] IReadOnlyData.Value: {valueProp.PropertyType.Name}");
                
                // Buscar tipo TypedValue
                var typedValueType = Type.GetType("SI.Bindable.TypedValue, SI.Bindable");
                if (typedValueType == null)
                {
                    Log.LogWarning("[TV] TypedValue não encontrado");
                    return;
                }
                
                Log.LogInfo($"[TV] TypedValue encontrado");
                
                // Propriedades de TypedValue
                var props = typedValueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Log.LogInfo($"[TV] {props.Length} propriedades:");
                
                foreach (var p in props)
                {
                    Log.LogInfo($"[TV]   {p.Name}: {p.PropertyType.Name}");
                }
                
                // Propriedade DataType
                var dataTypeProp = typedValueType.GetProperty("DataType", BindingFlags.Public | BindingFlags.Instance);
                if (dataTypeProp != null)
                {
                    Log.LogInfo($"[TV] DataType prop encontrada!");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[TV] Erro: {ex.Message}");
            }
        }
        
        private static void ExportViaBindingsDataSet()
        {
            try
            {
                // Buscar tipo Bindings
                var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
                if (bindingsType == null)
                {
                    Log.LogWarning("[Export] Bindings não encontrado");
                    return;
                }
                
                // Buscar instância
                var allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                object bindingsInstance = null;
                
                foreach (var obj in allObjects)
                {
                    if (obj == null) continue;
                    var type = obj.GetType();
                    if (type == bindingsType || type.BaseType == bindingsType)
                    {
                        bindingsInstance = obj;
                        break;
                    }
                }
                
                if (bindingsInstance == null)
                {
                    Log.LogWarning("[Export] Instância Bindings não encontrada");
                    return;
                }
                
                // Obter DataSet
                var dataSetProp = bindingsType.GetProperty("DataSet", BindingFlags.Public | BindingFlags.Instance);
                if (dataSetProp == null) return;
                
                var dataSet = dataSetProp.GetValue(bindingsInstance);
                if (dataSet == null || !(dataSet is IEnumerable en)) return;
                
                // Buscar tipos necessários
                var iReadOnlyData = Type.GetType("SI.Bindable.IReadOnlyData, SI.Bindable");
                var typedValueType = Type.GetType("SI.Bindable.TypedValue, SI.Bindable");
                var valueProp = iReadOnlyData?.GetProperty("Value");
                var dataTypeProp = typedValueType?.GetProperty("DataType", BindingFlags.Public | BindingFlags.Instance);
                
                if (valueProp == null || dataTypeProp == null)
                {
                    Log.LogWarning("[Export] Propriedades não encontradas");
                    return;
                }
                
                // Coletar dados por tipo
                var dataByType = new Dictionary<Type, List<object>>();
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
                        if (dataType == null) continue;
                        
                        // Verificar se é tipo de dados de jogador
                        var typeName = dataType.Name.ToLower();
                        if (typeName.Contains("player") || typeName.Contains("person") ||
                            typeName.Contains("squad") || typeName.Contains("club") ||
                            typeName.Contains("team") || typeName.Contains("match"))
                        {
                            if (!dataByType.ContainsKey(dataType))
                                dataByType[dataType] = new List<object>();
                            dataByType[dataType].Add(value);
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Export] {total} itens verificados");
                
                foreach (var kvp in dataByType)
                {
                    Log.LogInfo($"[Export] Tipo {kvp.Key.Name}: {kvp.Value.Count} itens");
                }
                
                // Exportar se encontrou dados
                if (dataByType.Count > 0)
                {
                    var first = dataByType.First();
                    if (first.Value.Count > 5)
                    {
                        ExportTypedValues(first.Value);
                    }
                }
                else
                {
                    Log.LogWarning("[Export] Nenhum dado relevante encontrado");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
            }
        }
        
        private static void ExportTypedValues(List<object> typedValues)
        {
            try
            {
                if (typedValues.Count == 0) return;
                
                // TypedValue<TVal> tem valor interno
                var typedValueType = typedValues[0].GetType();
                Log.LogInfo($"[CSV] Exportando {typedValues.Count} itens de {typedValueType.Name}");
                
                // Buscar campo ou propriedade que contém o valor
                var fields = typedValueType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                var props = typedValueType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                Log.LogInfo($"[CSV] Campos: {fields.Length}, Props: {props.Length}");
                
                foreach (var p in props)
                {
                    Log.LogInfo($"[CSV]   {p.Name}: {p.PropertyType.Name}");
                }
                
                // Tentar extrair valor via GetTypedValue ou similar
                var getValueMethod = typedValueType.GetMethod("GetTypedValue", BindingFlags.Public | BindingFlags.Instance);
                if (getValueMethod != null)
                {
                    Log.LogInfo($"[CSV] Método GetTypedValue encontrado!");
                }
                
                // Fallback: tentar converter para string
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Index;Type;Value");
                
                int count = 0;
                foreach (var tv in typedValues.Take(100))
                {
                    csv.AppendLine($"{count};{tv.GetType().Name};{tv.ToString()}");
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
