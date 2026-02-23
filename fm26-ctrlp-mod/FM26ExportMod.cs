using System;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(FM26ExportMod.FM26ExportMod), "FM26 Ctrl+P Export Mod", "1.0.0", "Koda Assistant")]

namespace FM26ExportMod
{
    public class FM26ExportMod : MelonMod
    {
        private int _frameCount = 0;
        private Type _keyboardType = null;
        private PropertyInfo _currentProperty = null;
        private PropertyInfo _leftCtrlKey = null;
        private PropertyInfo _pKey = null;
        private PropertyInfo _f10Key = null;
        private MethodInfo _isPressedMethod = null;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("========================================");
            MelonLogger.Msg("FM26 Ctrl+P Export Mod v1.0.0");
            MelonLogger.Msg("Unity 6 Input System");
            MelonLogger.Msg("========================================");
            
            InitializeInputSystem();
        }
        
        private void InitializeInputSystem()
        {
            MelonLogger.Msg("[Init] Procurando Input System...");
            
            // Novo Input System: UnityEngine.InputSystem
            var inputSystemAssembly = Type.GetType("UnityEngine.InputSystem.Keyboard, UnityEngine.InputSystem");
            
            if (inputSystemAssembly == null)
            {
                // Tenta carregar o assembly diretamente
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == "UnityEngine.InputSystem")
                    {
                        MelonLogger.Msg("[Init] Assembly encontrado: " + assembly.FullName);
                        _keyboardType = assembly.GetType("UnityEngine.InputSystem.Keyboard");
                        
                        if (_keyboardType != null)
                        {
                            MelonLogger.Msg("[Init] Keyboard type encontrado!");
                            
                            // Keyboard.current
                            _currentProperty = _keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                            if (_currentProperty != null)
                            {
                                MelonLogger.Msg("[Init] Keyboard.current encontrado!");
                            }
                            
                            // KeyControl.isPressed
                            var keyControlType = assembly.GetType("UnityEngine.InputSystem.Controls.KeyControl");
                            if (keyControlType != null)
                            {
                                _isPressedMethod = keyControlType.GetProperty("isPressed")?.GetMethod;
                                if (_isPressedMethod != null)
                                {
                                    MelonLogger.Msg("[Init] isPressed encontrado!");
                                }
                            }
                            
                            // leftCtrlKey, pKey, f10Key
                            _leftCtrlKey = _keyboardType.GetProperty("leftCtrlKey", BindingFlags.Public | BindingFlags.Instance);
                            _pKey = _keyboardType.GetProperty("pKey", BindingFlags.Public | BindingFlags.Instance);
                            _f10Key = _keyboardType.GetProperty("f10Key", BindingFlags.Public | BindingFlags.Instance);
                            
                            if (_leftCtrlKey != null) MelonLogger.Msg("[Init] leftCtrlKey encontrado!");
                            if (_pKey != null) MelonLogger.Msg("[Init] pKey encontrado!");
                            if (_f10Key != null) MelonLogger.Msg("[Init] f10Key encontrado!");
                            
                            return;
                        }
                    }
                }
            }
            
            MelonLogger.Error("[Init] Input System NAO encontrado!");
        }
        
        public override void OnUpdate()
        {
            _frameCount++;
            
            if (_currentProperty == null || _isPressedMethod == null)
            {
                if (_frameCount % 600 == 0)
                {
                    MelonLogger.Msg("[OnUpdate] Aguardando Input System...");
                }
                return;
            }
            
            try
            {
                // Keyboard.current
                var currentKeyboard = _currentProperty.GetValue(null);
                if (currentKeyboard == null)
                {
                    if (_frameCount % 600 == 0)
                    {
                        MelonLogger.Msg("[OnUpdate] Nenhum teclado detectado");
                    }
                    return;
                }
                
                // leftCtrlKey
                var leftCtrlControl = _leftCtrlKey?.GetValue(currentKeyboard);
                var pControl = _pKey?.GetValue(currentKeyboard);
                var f10Control = _f10Key?.GetValue(currentKeyboard);
                
                bool ctrlPressed = leftCtrlControl != null && (bool)_isPressedMethod.Invoke(leftCtrlControl, null);
                bool pPressed = pControl != null && (bool)_isPressedMethod.Invoke(pControl, null);
                bool f10Pressed = f10Control != null && (bool)_isPressedMethod.Invoke(f10Control, null);
                
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
                if (_frameCount % 600 == 0)
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
            
            MelonLogger.Msg("[Export] Total: " + count);
        }
    }
}
