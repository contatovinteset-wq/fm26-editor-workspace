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
    [BepInPlugin("com.koda.fm26.diagnostic", "FM26 Diagnostic", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        
        public override void Load()
        {
            Log = base.Log;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Diagnostic v1.0.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            var harmony = new Harmony("com.koda.fm26.diagnostic");
            
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
            _frameCount++;
            
            if (!_initialized && _frameCount == 300)
            {
                _initialized = true;
                Log.LogInfo("[Init] Inicializando diagnóstico...");
            }
            
            if (!_initialized) return;
            if (Keyboard.current == null) return;
            
            // F9 - Diagnóstico completo de assemblies
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                Debug.Log("[FM26Diag] >>> F9 - Listando assemblies");
                ListAllAssemblies();
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
        
        private static void ListAllAssemblies()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Log.LogInfo($"[Diag] {assemblies.Length} assemblies carregados");
                
                int count = 0;
                foreach (var asm in assemblies)
                {
                    var name = asm.GetName().Name;
                    if (name.StartsWith("SI.") || name.StartsWith("FM.") || 
                        name.StartsWith("Football") || name.Contains("Manager"))
                    {
                        Log.LogInfo($"[Diag] Assembly: {name}");
                        count++;
                        if (count > 50) break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Diag] Erro: {ex.Message}");
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
                                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                                {
                                    if (method.Name.Contains("Export") || method.Name.Contains("export"))
                                    {
                                        Log.LogInfo($"[Diag]   Método: {method.Name}({string.Join(", ", GetParamTypes(method))})");
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
                            if (type.Name.Contains("Table") || type.Name.Contains("List") || 
                                type.Name.Contains("View") || type.Name.Contains("Grid"))
                            {
                                if (type.Name.Contains("Streamed") || type.Name.Contains("Player") || 
                                    type.Name.Contains("Squad") || type.Name.Contains("Team"))
                                {
                                    Log.LogInfo($"[Diag] Tipo: {type.FullName}");
                                    
                                    // Verificar se é VisualElement
                                    if (typeof(VisualElement).IsAssignableFrom(type))
                                    {
                                        Log.LogInfo($"[Diag]   -> É VisualElement!");
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
}
