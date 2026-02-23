using System;
using System.Reflection;
using MelonLoader;

[assembly: MelonInfo(typeof(FM26ExportMod.FM26ExportMod), "FM26 Ctrl+P Export Mod", "1.0.0", "Koda Assistant")]

namespace FM26ExportMod
{
    public class FM26ExportMod : MelonMod
    {
        private int _frameCount = 0;
        private bool _inputFound = false;
        private MethodInfo _getKeyMethod = null;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("========================================");
            MelonLogger.Msg("FM26 Ctrl+P Export Mod CARREGADO!");
            MelonLogger.Msg("========================================");
            
            // Tenta encontrar o Input
            FindInput();
        }
        
        private void FindInput()
        {
            MelonLogger.Msg("[Init] Procurando UnityEngine.Input...");
            
            // Lista todos os assemblies com "Unity" no nome
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("Unity"))
                {
                    MelonLogger.Msg("[Assembly] " + assembly.FullName);
                }
            }
            
            // Tenta encontrar o tipo Input
            var inputType = Type.GetType("UnityEngine.Input, UnityEngine.InputModule");
            if (inputType != null)
            {
                MelonLogger.Msg("[Init] Encontrado UnityEngine.Input em InputModule");
                _getKeyMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(int) });
                if (_getKeyMethod != null)
                {
                    MelonLogger.Msg("[Init] GetKeyDown(int) encontrado!");
                    _inputFound = true;
                }
            }
            
            if (!_inputFound)
            {
                inputType = Type.GetType("UnityEngine.Input, UnityEngine.CoreModule");
                if (inputType != null)
                {
                    MelonLogger.Msg("[Init] Encontrado UnityEngine.Input em CoreModule");
                    _getKeyMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(int) });
                    if (_getKeyMethod != null)
                    {
                        MelonLogger.Msg("[Init] GetKeyDown(int) encontrado!");
                        _inputFound = true;
                    }
                }
            }
            
            if (!_inputFound)
            {
                MelonLogger.Error("[Init] NAO conseguiu encontrar Input.GetKeyDown!");
            }
        }
        
        public override void OnUpdate()
        {
            _frameCount++;
            
            // Log a cada 300 frames (~5 segundos) para confirmar que está rodando
            if (_frameCount % 300 == 0)
            {
                MelonLogger.Msg("[OnUpdate] Rodando... frame " + _frameCount + ", InputFound: " + _inputFound);
            }
            
            if (!_inputFound || _getKeyMethod == null)
                return;
            
            try
            {
                // F10 = 291
                bool f10Pressed = (bool)_getKeyMethod.Invoke(null, new object[] { 291 });
                if (f10Pressed)
                {
                    MelonLogger.Msg(">>> F10 PRESSIONADO - MOD FUNCIONANDO!");
                }
                
                // LeftControl = 306, P = 112
                bool ctrlPressed = (bool)_getKeyMethod.Invoke(null, new object[] { 306 });
                bool pPressed = (bool)_getKeyMethod.Invoke(null, new object[] { 112 });
                
                if (ctrlPressed && pPressed)
                {
                    MelonLogger.Msg(">>> Ctrl+P DETECTADO!");
                    TryExport();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[OnUpdate] Erro: " + ex.Message);
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
                            if (count > 20) return;
                        }
                    }
                }
                catch { }
            }
            
            if (count == 0)
            {
                MelonLogger.Msg("[Export] Nenhum carousel encontrado");
            }
        }
    }
}
