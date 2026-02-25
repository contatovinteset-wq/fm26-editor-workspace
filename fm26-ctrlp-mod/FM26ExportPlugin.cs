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
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
            Log.LogInfo("========================================");
            
            try
            {
                var go = new GameObject("FM26CtrlP_Test");
                UnityEngine.Object.DontDestroyOnLoad(go);
                
                // Converte para Il2CppSystem.Type
                var il2cppType = Il2CppSystem.Type.GetType("FM26CtrlPExport.CtrlPRunner");
                if (il2cppType != null)
                {
                    go.AddComponent(il2cppType);
                    Log.LogInfo("[Init] MonoBehaviour adicionado com sucesso!");
                }
                else
                {
                    Log.LogError("[Init] Não consegui obter Il2CppSystem.Type");
                }
            }
            catch (System.Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
                Log.LogError($"[Init] StackTrace: {ex.StackTrace}");
            }
        }
    }
    
    public class CtrlPRunner : MonoBehaviour
    {
        void Awake()
        {
            Debug.Log("[FM26CtrlP] Awake!");
        }
        
        void Start()
        {
            Debug.Log("[FM26CtrlP] Start!");
        }
        
        void Update()
        {
            Debug.Log("[FM26CtrlP] Update!");
        }
    }
}
