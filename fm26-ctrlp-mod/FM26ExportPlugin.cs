using System;
using System.Reflection;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FM26ExportMod
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "2.0.0")]
    public class FM26ExportPlugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private static MethodInfo _exportMethod;
        private static Type _carouselType;
        
        void Awake()
        {
            Logger = base.Logger;
            
            Logger.LogInfo("========================================");
            Logger.LogInfo("FM26 Ctrl+P Export Mod v2.0.0");
            Logger.LogInfo("BepInEx Edition");
            Logger.LogInfo("========================================");
            
            // Aplica patches Harmony
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            harmony.PatchAll();
            
            Logger.LogInfo("[Init] Harmony patches applied!");
            
            // Inicia coroutine para encontrar métodos
            StartCoroutine(FindExportMethods());
        }
        
        IEnumerator FindExportMethods()
        {
            yield return new WaitForSeconds(2f); // Aguarda jogo carregar
            
            Logger.LogInfo("[Init] Procurando métodos de exportação...");
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = assembly.GetName().Name;
                    if (!name.Contains("SI") && !name.Contains("FM") && !name.Contains("Football"))
                        continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        // Procura SICarousel
                        if (type.Name.Contains("Carousel"))
                        {
                            Logger.LogInfo($"[Tipo] {type.FullName}");
                            _carouselType = type;
                            
                            // Lista todos os métodos
                            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            {
                                if (method.Name.Contains("Export"))
                                {
                                    Logger.LogInfo($"[Export] {method.Name}({string.Join(", ", Array.ConvertAll(method.GetParameters(), p => p.ParameterType.Name))})");
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            
            // Tenta encontrar o método específico
            if (_carouselType != null)
            {
                _exportMethod = _carouselType.GetMethod("UpdateExportCurrentItemBinding", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (_exportMethod != null)
                {
                    Logger.LogInfo("[Init] UpdateExportCurrentItemBinding encontrado!");
                }
            }
        }
        
        void Update()
        {
            // Detecta Ctrl+P
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                Logger.LogInfo(">>> Ctrl+P PRESSIONADO!");
                TryExport();
            }
        }
        
        void TryExport()
        {
            if (_carouselType == null)
            {
                Logger.LogError("[Export] Carousel type não encontrado");
                return;
            }
            
            // Encontra instâncias ativas do carousel
            var carousels = FindObjectsOfType(_carouselType);
            
            if (carousels == null || carousels.Length == 0)
            {
                Logger.LogWarning("[Export] Nenhum carousel ativo encontrado");
                Logger.LogInfo("[Export] Dica: Selecione uma tabela/lista de jogadores");
                return;
            }
            
            Logger.LogInfo($"[Export] Encontrados {carousels.Length} carousels");
            
            foreach (var carousel in carousels)
            {
                Logger.LogInfo($"[Export] Processando: {carousel.name}");
                
                if (_exportMethod != null)
                {
                    try
                    {
                        // Tenta chamar com parâmetro 0
                        _exportMethod.Invoke(carousel, new object[] { 0 });
                        Logger.LogInfo("[Export] UpdateExportCurrentItemBinding(0) executado!");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[Export] Erro: {ex.Message}");
                    }
                }
                else
                {
                    // Tenta métodos alternativos
                    TryAlternativeExport(carousel);
                }
            }
        }
        
        void TryAlternativeExport(UnityEngine.Object carousel)
        {
            var type = carousel.GetType();
            
            // Lista de possíveis métodos de exportação
            var exportMethods = new[] {
                "ExportCurrentItem",
                "Export",
                "DoExport",
                "ExportSelection",
                "ExportData"
            };
            
            foreach (var methodName in exportMethods)
            {
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    Logger.LogInfo($"[Export] Tentando: {methodName}");
                    try
                    {
                        var ps = method.GetParameters();
                        if (ps.Length == 0)
                        {
                            method.Invoke(carousel, null);
                            Logger.LogInfo($"[Export] {methodName}() executado!");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"[Export] Falha: {ex.Message}");
                    }
                }
            }
            
            Logger.LogError("[Export] Nenhum método de exportação funcionou");
        }
    }
}
