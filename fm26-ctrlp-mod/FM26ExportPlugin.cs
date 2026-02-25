using System;
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
                
                // Obtém o tipo via System.Type primeiro
                var monoType = typeof(CtrlPRunner);
                Log.LogInfo($"[Init] Tipo System: {monoType.FullName}");
                Log.LogInfo($"[Init] Assembly: {monoType.Assembly.FullName}");
                
                // Tenta obter Il2CppSystem.Type via reflection
                var il2CppType = Il2CppSystem.Type.GetTypeFromHandle(
                    Il2CppSystem.RuntimeTypeHandle.op_Implicit(monoType.TypeHandle));
                
                if (il2CppType != null)
                {
                    go.AddComponent(il2CppType);
                    Log.LogInfo("[Init] MonoBehaviour adicionado com sucesso!");
                }
                else
                {
                    Log.LogError("[Init] Il2CppSystem.Type é null");
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
