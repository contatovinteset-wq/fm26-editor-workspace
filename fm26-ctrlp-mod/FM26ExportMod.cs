using System;
using System.Reflection;
using MelonLoader;

// Atributo MelonInfo - OBRIGATÓRIO
[assembly: MelonInfo(typeof(FM26ExportMod.FM26ExportMod), "FM26 Ctrl+P Export Mod", "1.0.0", "Koda Assistant")]
// REMOVIDO MelonGame - para funcionar com qualquer jogo

namespace FM26ExportMod
{
    public class FM26ExportMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("========================================");
            MelonLogger.Msg("FM26 Ctrl+P Export Mod CARREGADO!");
            MelonLogger.Msg("========================================");
        }
        
        public override void OnUpdate()
        {
            try
            {
                var inputType = Type.GetType("UnityEngine.Input, UnityEngine.InputModule");
                if (inputType == null)
                {
                    inputType = Type.GetType("UnityEngine.Input, UnityEngine.CoreModule");
                }
                
                if (inputType != null)
                {
                    var getKeyMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(int) });
                    if (getKeyMethod != null)
                    {
                        // F10 = 291 (para teste)
                        bool f10Pressed = (bool)getKeyMethod.Invoke(null, new object[] { 291 });
                        if (f10Pressed)
                        {
                            MelonLogger.Msg(">>> F10 PRESSIONADO - MOD FUNCIONANDO!");
                        }
                        
                        // LeftControl = 306, P = 112
                        bool ctrlPressed = (bool)getKeyMethod.Invoke(null, new object[] { 306 });
                        bool pPressed = (bool)getKeyMethod.Invoke(null, new object[] { 112 });
                        
                        if (ctrlPressed && pPressed)
                        {
                            MelonLogger.Msg(">>> Ctrl+P DETECTADO!");
                            TryExport();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Erro: " + ex.Message);
            }
        }
        
        private void TryExport()
        {
            MelonLogger.Msg("[Export] Procurando carousels...");
            
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                        {
                            if (type.Name.Contains("Carousel"))
                            {
                                MelonLogger.Msg("[Carousel] " + type.FullName);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Export] Erro: " + ex.Message);
            }
        }
    }
}
