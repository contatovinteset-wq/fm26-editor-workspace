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
        private bool _initialized = false;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("========================================");
            MelonLogger.Msg("FM26 Ctrl+P Export Mod v1.0.0");
            MelonLogger.Msg("========================================");
        }
        
        private void FindInput()
        {
            if (_initialized) return;
            _initialized = true;
            
            MelonLogger.Msg("[Init] Procurando Input...");
            
            // Lista todos os assemblies com Input no nome
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;
                if (name.Contains("Input"))
                {
                    MelonLogger.Msg("[Assembly] " + name);
                    
                    // Procura tipo Input
                    var inputType = assembly.GetType("UnityEngine.Input");
                    if (inputType != null)
                    {
                        MelonLogger.Msg("[Init] UnityEngine.Input encontrado em " + name);
                        
                        // Lista métodos
                        foreach (var method in inputType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (method.Name.Contains("Key"))
                            {
                                var ps = method.GetParameters();
                                MelonLogger.Msg("[Method] " + method.Name + "(" + (ps.Length > 0 ? ps[0].ParameterType.Name : "") + ")");
                            }
                        }
                        
                        // Tenta GetKeyDown com KeyCode
                        var keyCodeType = assembly.GetType("UnityEngine.KeyCode");
                        if (keyCodeType != null)
                        {
                            MelonLogger.Msg("[Init] KeyCode encontrado!");
                            _keyCodeType = keyCodeType;
                            _getKeyMethod = inputType.GetMethod("GetKeyDown", new Type[] { keyCodeType });
                            if (_getKeyMethod != null)
                            {
                                MelonLogger.Msg("[Init] GetKeyDown(KeyCode) OK!");
                                return;
                            }
                        }
                        
                        // Tenta GetKeyDown com int
                        _getKeyMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(int) });
                        if (_getKeyMethod != null)
                        {
                            MelonLogger.Msg("[Init] GetKeyDown(int) OK!");
                            return;
                        }
                    }
                }
            }
            
            MelonLogger.Error("[Init] Input NAO encontrado!");
        }
        
        public override void OnUpdate()
        {
            _frameCount++;
            
            if (!_initialized)
            {
                FindInput();
            }
            
            if (_frameCount % 300 == 0)
            {
                MelonLogger.Msg("[OnUpdate] Frame " + _frameCount);
                
                if (_getKeyMethod == null)
                {
                    FindInput();
                }
            }
            
            if (_getKeyMethod == null) return;
            
            try
            {
                bool ctrlPressed, pPressed, f10Pressed;
                
                if (_keyCodeType != null)
                {
                    // Usa KeyCode enum
                    ctrlPressed = (bool)_getKeyMethod.Invoke(null, new object[] { Enum.Parse(_keyCodeType, "LeftControl") });
                    pPressed = (bool)_getKeyMethod.Invoke(null, new object[] { Enum.Parse(_keyCodeType, "P") });
                    f10Pressed = (bool)_getKeyMethod.Invoke(null, new object[] { Enum.Parse(_keyCodeType, "F10") });
                }
                else
                {
                    // Usa int (key codes)
                    ctrlPressed = (bool)_getKeyMethod.Invoke(null, new object[] { 306 }); // LeftControl
                    pPressed = (bool)_getKeyMethod.Invoke(null, new object[] { 112 });   // P
                    f10Pressed = (bool)_getKeyMethod.Invoke(null, new object[] { 291 }); // F10
                }
                
                if (f10Pressed)
                {
                    MelonLogger.Msg(">>> F10 PRESSIONADO!");
                }
                
                if (ctrlPressed && pPressed)
                {
                    MelonLogger.Msg(">>> Ctrl+P DETECTADO!");
                    TryExport();
                }
            }
            catch (Exception ex)
            {
                if (_frameCount % 300 == 0)
                {
                    MelonLogger.Error("[Error] " + ex.Message);
                }
            }
        }
        
        private void TryExport()
        {
            MelonLogger.Msg("[Export] Procurando carousels...");
            
            int count = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Carousel"))
                        {
                            MelonLogger.Msg("[Carousel] " + type.FullName);
                            count++;
                        }
                    }
                }
                catch { }
            }
            
            MelonLogger.Msg("[Export] Total: " + count);
        }
    }
}
