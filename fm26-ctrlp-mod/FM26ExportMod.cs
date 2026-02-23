using System;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(FM26ExportMod.FM26ExportMod), "FM26 Ctrl+P Export Mod", "1.0.0", "Koda Assistant")]

namespace FM26ExportMod
{
    public class FM26ExportMod : MelonMod
    {
        private int _frameCount = 0;
        private MethodInfo _getKeyMethod = null;
        private Type _keyCodeType = null;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("========================================");
            MelonLogger.Msg("FM26 Ctrl+P Export Mod - DEBUG");
            MelonLogger.Msg("========================================");
            
            FindInputSystem();
        }
        
        private void FindInputSystem()
        {
            MelonLogger.Msg("[Init] Listando TODOS os tipos com 'Input' ou 'Key'...");
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = assembly.GetName().Name;
                    if (!name.Contains("Unity") && !name.Contains("Melon"))
                        continue;
                        
                    MelonLogger.Msg("[Assembly] " + name);
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Input") || type.Name.Contains("Key"))
                        {
                            MelonLogger.Msg("[Tipo] " + type.FullName);
                            
                            // Se for Input, lista os métodos
                            if (type.Name == "Input")
                            {
                                MelonLogger.Msg("  >>> INPUT ENCONTRADO! Metodos:");
                                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                                {
                                    var ps = method.GetParameters();
                                    if (method.Name.Contains("Key") || method.Name.Contains("Mouse") || method.Name.Contains("Button"))
                                    {
                                        MelonLogger.Msg("    " + method.Name + "(" + string.Join(", ", Array.ConvertAll(ps, p => p.ParameterType.Name)) + ")");
                                    }
                                }
                            }
                        }
                        
                        if (type.Name == "KeyCode")
                        {
                            MelonLogger.Msg("[KeyCode] Enum encontrado!");
                            _keyCodeType = type;
                        }
                    }
                }
                catch { }
            }
            
            // Tenta encontrar Input diretamente
            var inputType = Type.GetType("UnityEngine.Input, UnityEngine.InputModule");
            if (inputType == null)
                inputType = Type.GetType("UnityEngine.Input, UnityEngine.CoreModule");
                
            if (inputType != null)
            {
                MelonLogger.Msg("[Init] Input encontrado via Type.GetType: " + inputType.AssemblyQualifiedName);
                
                foreach (var method in inputType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var ps = method.GetParameters();
                    MelonLogger.Msg("[Input." + method.Name + "] params: " + ps.Length);
                }
            }
            else
            {
                MelonLogger.Error("[Init] Input NAO encontrado!");
            }
        }
        
        public override void OnUpdate()
        {
            _frameCount++;
            
            if (_frameCount % 600 == 0)
            {
                MelonLogger.Msg("[OnUpdate] Frame " + _frameCount + " - Mod ainda rodando");
            }
        }
    }
}
