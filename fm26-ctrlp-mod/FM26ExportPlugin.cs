using System;
using System.Reflection;
using System.Threading;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace FM26CtrlPExport
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            try
            {
                Log.LogInfo("========================================");
                Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
                Log.LogInfo("========================================");
                
                // Inicia thread separada para verificar input
                var thread = new Thread(InputThread);
                thread.IsBackground = true;
                thread.Start();
                
                Log.LogInfo("[Init] Thread de input iniciada!");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro ao carregar: {ex.Message}");
                Log.LogError($"[Init] StackTrace: {ex.StackTrace}");
            }
        }
        
        private static void InputThread()
        {
            // Espera o jogo carregar
            Thread.Sleep(5000);
            
            MethodInfo exportMethod = null;
            Type targetType = null;
            bool initialized = false;
            
            // Input via reflection
            Type inputType = null;
            MethodInfo getKeyMethod = null;
            MethodInfo getKeyDownMethod = null;
            
            // Inicializa reflection do Input
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    inputType = assembly.GetType("UnityEngine.Input");
                    if (inputType != null)
                    {
                        getKeyMethod = inputType.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
                        getKeyDownMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
                        if (getKeyMethod != null && getKeyDownMethod != null)
                            break;
                    }
                }
                catch { }
            }
            
            Debug.Log($"[FM26CtrlP] Input reflection: {inputType?.Name ?? "null"}");
            
            while (true)
            {
                Thread.Sleep(100); // 10 checks por segundo
                
                try
                {
                    // Busca método periodicamente
                    if (!initialized)
                    {
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                var name = assembly.GetName().Name;
                                if (name.StartsWith("System") || name.StartsWith("Mono") || name.StartsWith("mscorlib"))
                                    continue;
                                
                                foreach (var type in assembly.GetTypes())
                                {
                                    var method = type.GetMethod("UpdateExportCurrentItemBinding",
                                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                    
                                    if (method != null)
                                    {
                                        targetType = type;
                                        exportMethod = method;
                                        initialized = true;
                                        Debug.Log($"[FM26CtrlP] ENCONTRADO: {type.FullName}");
                                        break;
                                    }
                                }
                                if (initialized) break;
                            }
                            catch { }
                        }
                    }
                    
                    // Verifica Ctrl+P
                    bool ctrl = false;
                    bool p = false;
                    
                    if (getKeyMethod != null)
                    {
                        try
                        {
                            ctrl = (bool)getKeyMethod.Invoke(null, new object[] { KeyCode.LeftControl });
                            p = (bool)getKeyDownMethod.Invoke(null, new object[] { KeyCode.P });
                        }
                        catch { }
                    }
                    
                    if (ctrl && p)
                    {
                        Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                        
                        if (exportMethod != null && targetType != null)
                        {
                            try
                            {
                                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", Type.EmptyTypes);
                                var generic = findMethod.MakeGenericMethod(targetType);
                                var results = generic.Invoke(null, null) as UnityEngine.Object[];
                                
                                Debug.Log($"[FM26CtrlP] {results?.Length ?? 0} objetos encontrados");
                                
                                if (results != null)
                                {
                                    foreach (var obj in results)
                                    {
                                        Debug.Log($"[FM26CtrlP] Exportando: {obj.name}");
                                        exportMethod.Invoke(obj, new object[] { 0 });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[FM26CtrlP] Erro no export: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[FM26CtrlP] Método não encontrado ainda");
                        }
                    }
                    
                    // F10 - Debug
                    bool f10 = false;
                    if (getKeyDownMethod != null)
                    {
                        try
                        {
                            f10 = (bool)getKeyDownMethod.Invoke(null, new object[] { KeyCode.F10 });
                        }
                        catch { }
                    }
                    
                    if (f10)
                    {
                        Debug.Log("[FM26CtrlP] >>> F10 - Listando tipos...");
                        int count = 0;
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                var name = assembly.GetName().Name;
                                if (name.StartsWith("System") || name.StartsWith("Mono")) continue;
                                
                                foreach (var type in assembly.GetTypes())
                                {
                                    if (type.Name.Contains("Export") || type.Name.Contains("Carousel") || 
                                        type.Name.Contains("Table") || type.Name.Contains("View"))
                                    {
                                        Debug.Log($"[FM26CtrlP] Tipo: {type.FullName}");
                                        count++;
                                        if (count > 30) break;
                                    }
                                }
                                if (count > 30) break;
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FM26CtrlP] Erro no loop: {ex.Message}");
                }
            }
        }
    }
}
