using System;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(FM26ExportMod.FM26ExportMod), "FM26 Ctrl+P Export Mod", "1.0.0", "Koda Assistant")]

namespace FM26ExportMod
{
    public class FM26ExportMod : MelonMod
    {
        private MethodInfo _getKeyMethod = null;
        private Type _keyCodeType = null;
        private bool _initialized = false;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("========================================");
            MelonLogger.Msg("FM26 Ctrl+P Export Mod v1.0.0");
            MelonLogger.Msg("========================================");
            
            FindInput();
        }
        
        private void FindInput()
        {
            if (_initialized) return;
            _initialized = true;
            
            MelonLogger.Msg("[Init] Procurando Input...");
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;
                if (!name.Contains("Input")) continue;
                
                MelonLogger.Msg("[Assembly] " + name);
                
                var inputType = assembly.GetType("UnityEngine.Input");
                if (inputType == null) continue;
                
                MelonLogger.Msg("[Init] UnityEngine.Input encontrado!");
                
                // Lista métodos Key
                foreach (var method in inputType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (method.Name.Contains("Key"))
                    {
                        var ps = method.GetParameters();
                        MelonLogger.Msg("[Method] " + method.Name + "(" + (ps.Length > 0 ? ps[0].ParameterType.Name : "") + ")");
                    }
                }
                
                // Tenta KeyCode enum
                _keyCodeType = assembly.GetType("UnityEngine.KeyCode");
                if (_keyCodeType != null)
                {
                    MelonLogger.Msg("[Init] KeyCode encontrado!");
                    _getKeyMethod = inputType.GetMethod("GetKeyDown", new Type[] { _keyCodeType });
                    if (_getKeyMethod != null)
                    {
                        MelonLogger.Msg("[Init] GetKeyDown(KeyCode) OK!");
                        return;
                    }
                }
                
                // Tenta int
                _getKeyMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(int) });
                if (_getKeyMethod != null)
                {
                    MelonLogger.Msg("[Init] GetKeyDown(int) OK!");
                    return;
                }
            }
            
            MelonLogger.Error("[Init] Input NAO encontrado!");
        }
        
        public override void OnUpdate()
        {
            if (_getKeyMethod == null) return;
            
            try
            {
                bool ctrlPressed, pPressed, f10Pressed;
                
                if (_keyCodeType != null)
                {
                    ctrlPressed = (bool)_getKeyMethod.Invoke(null, new object[] { Enum.Parse(_keyCodeType, "LeftControl") });
                    pPressed = (bool)_getKeyMethod.Invoke(null, new object[] { Enum.Parse(_keyCodeType, "P") });
                    f10Pressed = (bool)_getKeyMethod.Invoke(null, new object[] { Enum.Parse(_keyCodeType, "F10") });
                }
                else
                {
                    ctrlPressed = (bool)_getKeyMethod.Invoke(null, new object[] { 306 });
                    pPressed = (bool)_getKeyMethod.Invoke(null, new object[] { 112 });
                    f10Pressed = (bool)_getKeyMethod.Invoke(null, new object[] { 291 });
                }
                
                if (f10Pressed)
                    MelonLogger.Msg(">>> F10 PRESSIONADO!");
                
                if (ctrlPressed && pPressed)
                {
                    MelonLogger.Msg(">>> Ctrl+P DETECTADO!");
                    TryExport();
                }
            }
            catch { }
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
