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
                // Tenta criar GameObject sem usar generics
                var go = new GameObject("FM26CtrlP_Test");
                UnityEngine.Object.DontDestroyOnLoad(go);
                Log.LogInfo("[Init] GameObject criado com sucesso!");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro: {ex.Message}");
            }
        }
    }
}
