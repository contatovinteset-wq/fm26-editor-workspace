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
        private Type _inputType = null;
        private Type _keyCodeType = null;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("========================================");
            MelonLogger.Msg("FM26 Ctrl+P Export Mod CARREGADO!");
            MelonLogger.Msg("========================================");
            
            FindInputSystem();
        }
        
        private void FindInputSystem()
        {
            MelonLogger.Msg("[Init] Procurando sistema de Input...");
            
            // Procura Input em todos os assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.FullName.Contains("UnityEngine"))
                    {
                        MelonLogger.Msg("[Assembly] " + assembly.GetName().Name);
                        
                        var inputType = assembly.GetType("UnityEngine.Input");
                        if (inputType != null)
                        {
                            MelonLogger.Msg("[Init] Encontrado UnityEngine.Input!");
                            
                            // Lista todos os métodos
                            foreach (var method in inputType.GetMethods())
                            {
                                if (method.Name.Contains("Key"))
                                {
                                    MelonLogger.Msg("[Method] " + method.Name + "(" + string.Join(", ", Array.ConvertAll(method.GetParameters(), p => p.ParameterType.Name)) + ")");
                                }
                            }
                            
                            _inputType = inputType;
                            
                            // Tenta encontrar GetKeyDown com KeyCode enum
                            var keyCodeType = assembly.GetType("UnityEngine.KeyCode");
                            if (keyCodeType != null)
                            {
                                MelonLogger.Msg("[Init] Encontrado KeyCode enum!");
                                _keyCodeType = keyCodeType;
                                
                                _getKeyMethod = inputType.GetMethod("GetKeyDown", new Type[] { keyCodeType });
                                if (_getKeyMethod != null)
                                {
                                    MelonLogger.Msg("[Init] GetKeyDown(KeyCode) encontrado!");
                                }
                            }
                            
                            // Tenta GetKeyDown com int
                            var getIntMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(int) });
                            if (getIntMethod != null)
                            {
                                MelonLogger.Msg("[Init] GetKeyDown(int) encontrado!");
                                _getKeyMethod = getIntMethod;
                            }
                            
                            // Tenta GetKey com string
                            var getStringMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(string) });
                            if (getStringMethod != null)
                            {
                                MelonLogger.Msg("[Init] GetKeyDown(string) encontrado!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Msg("[Error] " + assembly.GetName().Name + ": " + ex.Message);
                }
            }
            
            if (_getKeyMethod == null)
            {
                MelonLogger.Error("[Init] NAO encontrou GetKeyDown!");
            }
        }
        
        public override void OnUpdate()
        {
            _frameCount++;
            
            // Log a cada 300 frames (~5 segundos)
            if (_frameCount % 300 == 0)
            {
                MelonLogger.Msg("[OnUpdate] Frame " + _frameCount);
            }
            
            if (_getKeyMethod == null)
                return;
            
            try
            {
                // Se usa KeyCode enum
                if (_keyCodeType != null && _getKeyMethod.GetParameters()[0].ParameterType == _keyCodeType)
                {
                    // F10 = 10, P = 25, LeftControl = 27
                    object f10Key = Enum.ToObject(_keyCodeType, 291);  // F10
                    object pKey = Enum.ToObject(_keyCodeType, 112);    // P
                    object ctrlKey = Enum.ToObject(_keyCodeType, 306); // LeftControl
                    
                    bool f10Pressed = (bool)_getKeyMethod.Invoke(null, new object[] { f10Key });
                    bool pPressed = (bool)_getKeyMethod.Invoke(null, new object[] { pKey });
                    bool ctrlPressed = (bool)_getKeyMethod.Invoke(null, new object[] { ctrlKey });
                    
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
                else
                {
                    // Usa int
                    bool f10Pressed = (bool)_getKeyMethod.Invoke(null, new object[] { 291 });
                    bool pPressed = (bool)_getKeyMethod.Invoke(null, new object[] { 112 });
                    bool ctrlPressed = (bool)_getKeyMethod.Invoke(null, new object[] { 306 });
                    
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
            }
            catch (Exception ex)
            {
                if (_frameCount % 300 == 0)
                {
                    MelonLogger.Error("[OnUpdate] Erro: " + ex.Message);
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
            
            MelonLogger.Msg("[Export] Total de carousels encontrados: " + count);
        }
    }
}
