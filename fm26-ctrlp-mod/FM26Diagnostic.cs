using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace FM26Diagnostic
{
    [BepInPlugin("com.koda.fm26.diagnostic", "FM26 Diagnostic", "1.1.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        private static Harmony _harmony;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Diagnostic v1.1.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            _harmony = new Harmony("com.koda.fm26.diagnostic");
            
            // Patch no Update de Bindings para detectar inputs
            var bindingsType = Type.GetType("SI.Bindable.Bindings, SI.Bindable");
            if (bindingsType != null)
            {
                var updateMethod = bindingsType.GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
                if (updateMethod != null)
                {
                    var patchMethod = typeof(Plugin).GetMethod("OnUpdate", BindingFlags.Static | BindingFlags.Public);
                    _harmony.Patch(updateMethod, postfix: new HarmonyMethod(patchMethod));
                    Log.LogInfo("[Init] Patched SI.Bindable.Bindings.Update");
                }
            }
        }
        
        private static int _frameCount = 0;
        private static bool _initialized = false;
        
        public static void OnUpdate()
        {
            _frameCount++;
            
            if (!_initialized && _frameCount == 300)
            {
                _initialized = true;
                Log.LogInfo("[Init] Inicializando diagnóstico...");
            }
            
            if (!_initialized) return;
            if (Keyboard.current == null) return;
            
            // F9 - PanelManager
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26Diag] >>> F9 - PanelManager Test");
                TestPanelManager();
            }
            
            // F10 - Buscar tipos com Export
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26Diag] >>> F10 - Buscando tipos Export");
                FindExportTypes();
            }
            
            // F11 - Buscar tipos de tabela/lista
            if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26Diag] >>> F11 - Buscando tipos Table/List");
                FindTableTypes();
            }
            
            // F12 - Diagnóstico de UI
            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26Diag] >>> F12 - Diagnóstico UI");
                DiagnoseUI();
            }
            
            // Ctrl+Shift+D - Dump completo
            bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
            bool shift = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            bool d = Keyboard.current.dKey.wasPressedThisFrame;
            
            if (ctrl && shift && d)
            {
                Debug.Log("[FM26Diag] >>> Ctrl+Shift+D - Dump completo");
                FullDump();
            }
        }
        
        private static void TestPanelManager()
        {
            try
            {
                Log.LogInfo("[Diag] Buscando PanelManager...");
                
                // Tentar encontrar PanelManager via singleton
                var panelManagerType = Type.GetType("PanelManager, SI.Bindable");
                if (panelManagerType == null)
                {
                    panelManagerType = Type.GetType("PanelManager");
                }
                
                if (panelManagerType == null)
                {
                    Log.LogWarning("[Diag] PanelManager type não encontrado");
                    return;
                }
                
                Log.LogInfo($"[Diag] PanelManager type: {panelManagerType.FullName}");
                
                // Tentar acessar Instance
                var instanceProperty = panelManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    Log.LogInfo($"[Diag] PanelManager.Instance: {instance != null}");
                    
                    if (instance != null)
                    {
                        // Tentar chamar GetOpenPanelInHighestLayer
                        var method = panelManagerType.GetMethod("GetOpenPanelInHighestLayer", BindingFlags.Public | BindingFlags.Instance);
                        if (method != null)
                        {
                            var panel = method.Invoke(instance, new object?[] { null });
                            Log.LogInfo($"[Diag] HighestLayerPanel: {panel != null}");
                            
                            if (panel != null)
                            {
                                Log.LogInfo($"[Diag] Panel Type: {panel.GetType().FullName}");
                                ScanPanelForTables(panel);
                            }
                        }
                        
                        // Tentar acessar Panels list
                        var panelsProp = panelManagerType.GetProperty("Panels");
                        if (panelsProp != null)
                        {
                            var panels = panelsProp.GetValue(instance);
                            Log.LogInfo($"[Diag] Panels: {panels}");
                        }
                    }
                }
                else
                {
                    Log.LogWarning("[Diag] PanelManager.Instance não encontrado");
                    
                    // Tentar Object.FindObjectOfType
                    var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", BindingFlags.Public | BindingFlags.Static);
                    if (findMethod != null)
                    {
                        var genericFind = findMethod.MakeGenericMethod(panelManagerType);
                        var manager = genericFind.Invoke(null, null);
                        Log.LogInfo($"[Diag] FindObjectOfType<PanelManager>: {manager != null}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Diag] Erro: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private static void ScanPanelForTables(object panel)
        {
            try
            {
                // Panel é VisualElement, então podemos navegar pelos filhos
                if (panel is VisualElement visualPanel)
                {
                    Log.LogInfo("[Diag] Panel é VisualElement, navegando...");
                    ScanVisualElementRecursive(visualPanel, 0);
                }
                else
                {
                    Log.LogInfo($"[Diag] Panel não é VisualElement: {panel.GetType().Name}");
                    
                    // Tentar encontrar uma propriedade que seja VisualElement
                    var props = panel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        if (typeof(VisualElement).IsAssignableFrom(prop.PropertyType))
                        {
                            var value = prop.GetValue(panel);
                            if (value != null)
                            {
                                Log.LogInfo($"[Diag] Encontrado VisualElement em {prop.Name}");
                                ScanVisualElementRecursive((VisualElement)value, 0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Diag] Erro scan panel: {ex.Message}");
            }
        }
        
        private static void ScanVisualElementRecursive(VisualElement element, int depth)
        {
            if (depth > 10) return; // Limitar profundidade
            if (element == null) return;
            
            var typeName = element.GetType().Name;
            
            // Procurar por tabelas/listas específicas
            if (typeName.Contains("StreamedTable") || typeName.Contains("StreamedList") ||
                typeName.Contains("StreamedObjectList"))
            {
                Log.LogInfo($"[Diag]   {' '.Repeat(depth)}\u2b50 {typeName} : {element.name}");
                
                // Tentar chamar método de export
                TryExportTable(element);
            }
            else if (depth < 3)
            {
                // Mostrar primeiros elementos
                //Log.LogInfo($"[Diag]   {' '.Repeat(depth)}{typeName} : {element.name}");
            }
            
            // Recursão nos filhos
            for (int i = 0; i < element.childCount; i++)
            {
                ScanVisualElementRecursive(element[i], depth + 1);
            }
        }
        
        private static void TryExportTable(VisualElement table)
        {
            try
            {
                var tableType = table.GetType();
                Log.LogInfo($"[Diag] Tentando exportar {tableType.FullName}");
                
                // Procurar property que retorne StreamedTableView
                var props = tableType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (prop.PropertyType.Name.Contains("View"))
                    {
                        Log.LogInfo($"[Diag]   View property: {prop.Name} -> {prop.PropertyType.Name}");
                    }
                }
                
                // Procurar métodos de export
                var methods = tableType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    if (method.Name.Contains("Export") || method.Name.Contains("Data"))
                    {
                        Log.LogInfo($"[Diag]   Method: {method.Name}({string.Join(", ", GetParamTypes(method))}) -> {method.ReturnType.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Diag] Erro export: {ex.Message}");
            }
        }
        
        private static void FindExportTypes()
        {
            try
            {
                Log.LogInfo("[Diag] Buscando tipos com 'Export'...");
                int count = 0;
                
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = asm.GetName().Name;
                        if (!name.StartsWith("SI.") && !name.StartsWith("FM.")) continue;
                        
                        foreach (var type in asm.GetTypes())
                        {
                            if (type.Name.Contains("Export") || type.Name.Contains("export"))
                            {
                                Log.LogInfo($"[Diag] Tipo: {type.FullName}");
                                
                                // Listar métodos
                                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                                {
                                    if (method.Name.Contains("Export") || method.Name.Contains("export"))
                                    {
                                        Log.LogInfo($"[Diag]   Método: {method.Name}() -> {method.ReturnType.Name}");
                                    }
                                }
                                
                                count++;
                                if (count > 30) return;
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Diag] Total: {count} tipos encontrados");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Diag] Erro: {ex.Message}");
            }
        }
        
        private static void FindTableTypes()
        {
            try
            {
                Log.LogInfo("[Diag] Buscando tipos Table/List/View...");
                int count = 0;
                
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = asm.GetName().Name;
                        if (!name.StartsWith("SI.") && !name.StartsWith("FM.")) continue;
                        
                        foreach (var type in asm.GetTypes())
                        {
                            if (type.Name.Contains("Streamed"))
                            {
                                if (type.Name.Contains("Table") || type.Name.Contains("List") || 
                                    type.Name.Contains("View"))
                                {
                                    Log.LogInfo($"[Diag] Tipo: {type.FullName}");
                                    
                                    // Verificar se é VisualElement
                                    if (typeof(VisualElement).IsAssignableFrom(type))
                                    {
                                        Log.LogInfo("[Diag]   -> É VisualElement!");
                                    }
                                    
                                    count++;
                                    if (count > 30) return;
                                }
                            }
                        }
                    }
                    catch { }
                }
                
                Log.LogInfo($"[Diag] Total: {count} tipos encontrados");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Diag] Erro: {ex.Message}");
            }
        }
        
        private static void DiagnoseUI()
        {
            try
            {
                Log.LogInfo("[Diag] Diagnosticando UI...");
                
                // 1. UIDocuments
                var uiDocs = Resources.FindObjectsOfTypeAll<UIDocument>();
                Log.LogInfo($"[Diag] {uiDocs.Length} UIDocuments");
                
                foreach (var doc in uiDocs)
                {
                    if (doc == null) continue;
                    Log.LogInfo($"[Diag] UIDocument: {doc.name}");
                    
                    var root = doc.rootVisualElement;
                    if (root != null)
                    {
                        Log.LogInfo($"[Diag]   Root: {root.GetType().Name}");
                        Log.LogInfo($"[Diag]   ChildCount: {root.childCount}");
                        
                        // Listar primeiros filhos
                        for (int i = 0; i < Math.Min(5, root.childCount); i++)
                        {
                            var child = root[i];
                            if (child != null)
                            {
                                Log.LogInfo($"[Diag]     [{i}] {child.GetType().Name} ({child.name})");
                            }
                        }
                    }
                }
                
                // 2. Buscar VisualElements via UIDocuments
                Log.LogInfo("[Diag] Buscando VisualElements via UIDocuments...");
                
                int tableCount = 0;
                int listCount = 0;
                int carouselCount = 0;
                
                foreach (var doc in uiDocs)
                {
                    if (doc?.rootVisualElement == null) continue;
                    CountVisualElementsRecursive(doc.rootVisualElement, ref tableCount, ref listCount, ref carouselCount);
                }
                
                Log.LogInfo($"[Diag] Resumo: {tableCount} Tables, {listCount} Lists, {carouselCount} Carousels");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Diag] Erro: {ex.Message}");
            }
        }
        
        private static void FullDump()
        {
            Log.LogInfo("========== DUMP COMPLETO ==========");
            
            // Listar todos os assemblies SI.* e FM.*
            var assemblies = new List<string>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (name.StartsWith("SI.") || name.StartsWith("FM.") || name.Contains("Manager"))
                {
                    assemblies.Add(name);
                }
            }
            
            Log.LogInfo($"[Dump] {assemblies.Count} assemblies relevantes:");
            foreach (var name in assemblies)
            {
                Log.LogInfo($"[Dump]   {name}");
            }
            
            // Buscar todos os tipos que poderiam ser a tabela de jogadores
            Log.LogInfo("[Dump] Buscando tipos de dados de jogador...");
            
            var playerTypes = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = asm.GetName().Name;
                    if (!name.StartsWith("SI.") && !name.StartsWith("FM.")) continue;
                    
                    foreach (var type in asm.GetTypes())
                    {
                        var typeName = type.Name.ToLower();
                        if (typeName.Contains("player") || typeName.Contains("squad") || 
                            typeName.Contains("roster") || typeName.Contains("team"))
                        {
                            if (typeName.Contains("table") || typeName.Contains("list") || 
                                typeName.Contains("view") || typeName.Contains("data"))
                            {
                                playerTypes.Add(type);
                            }
                        }
                    }
                }
                catch { }
            }
            
            Log.LogInfo($"[Dump] {playerTypes.Count} tipos de dados de jogador:");
            foreach (var type in playerTypes)
            {
                Log.LogInfo($"[Dump]   {type.FullName}");
                
                // Listar métodos públicos
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
                    {
                        Log.LogInfo($"[Dump]     {method.Name}()");
                    }
                }
            }
            
            Log.LogInfo("========== FIM DUMP ==========");
        }
        
        private static string[] GetParamTypes(MethodInfo method)
        {
            var parms = method.GetParameters();
            var types = new string[parms.Length];
            for (int i = 0; i < parms.Length; i++)
            {
                types[i] = parms[i].ParameterType.Name;
            }
            return types;
        }
        
        private static void CountVisualElementsRecursive(VisualElement element, ref int tableCount, ref int listCount, ref int carouselCount)
        {
            if (element == null) return;
            
            var typeName = element.GetType().Name;
            
            if (typeName.Contains("Table") && tableCount < 5)
            {
                Log.LogInfo($"[Diag] Table: {typeName} ({element.name})");
                tableCount++;
            }
            if (typeName.Contains("List") && listCount < 5)
            {
                Log.LogInfo($"[Diag] List: {typeName} ({element.name})");
                listCount++;
            }
            if (typeName.Contains("Carousel") && carouselCount < 5)
            {
                Log.LogInfo($"[Diag] Carousel: {typeName} ({element.name})");
                carouselCount++;
            }
            
            // Recursivamente buscar nos filhos
            for (int i = 0; i < element.childCount; i++)
            {
                CountVisualElementsRecursive(element[i], ref tableCount, ref listCount, ref carouselCount);
            }
        }
    }
    
    // Extensão para string
    public static class StringExtensions
    {
        public static string Repeat(this string str, int count)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
                sb.Append(str);
            return sb.ToString();
        }
    }
}