using BepInEx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FM26ExportMod
{
    [BepInPlugin("com.fm26.exportmod", "FM26 Ctrl+P Export", "1.0.0")]
    public class ExportModPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("FM26 Ctrl+P Export Mod carregado!");
            
            // Hook no update para verificar teclas
            var inputObj = new GameObject("FM26ExportInput");
            inputObj.AddComponent<ExportInputHandler>();
            DontDestroyOnLoad(inputObj);
        }
    }

    public class ExportInputHandler : MonoBehaviour
    {
        private void Update()
        {
            // Verificar Ctrl+P
            if (Keyboard.current != null)
            {
                bool ctrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                bool p = Keyboard.current.pKey.wasPressedThisFrame;
                
                if (ctrl && p)
                {
                    Debug.Log("[FM26Export] Ctrl+P detectado!");
                    TryExport();
                }
            }
        }

        private void TryExport()
        {
            // Usar reflection para encontrar e chamar o método de exportação
            // Isso evita dependências diretas das DLLs do jogo
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var asm in assemblies)
            {
                try
                {
                    var carouselType = asm.GetType("SI.Bindable.SICarousel");
                    if (carouselType != null)
                    {
                        var carousels = FindObjectsOfType(carouselType);
                        if (carousels.Length > 0)
                        {
                            var carousel = carousels[0];
                            var updateMethod = carouselType.GetMethod("UpdateExportCurrentItemBinding", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                            if (updateMethod != null)
                            {
                                // Obter SelectedIndex
                                var selectedIndexProp = carouselType.GetProperty("SelectedIndex");
                                int index = selectedIndexProp != null ? (int)selectedIndexProp.GetValue(carousel) : 0;
                                
                                updateMethod.Invoke(carousel, new object[] { index });
                                Debug.Log("[FM26Export] Exportação executada!");
                            }
                        }
                        break;
                    }
                }
                catch { }
            }
        }
    }
}
