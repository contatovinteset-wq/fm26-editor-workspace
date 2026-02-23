using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using MelonLoader;

namespace FM26ExportMod
{
    /// <summary>
    /// FM26 Ctrl+P Export Mod - MelonLoader Version
    /// Restaura a funcionalidade de exportação via Ctrl+P
    /// </summary>
    public class FM26ExportMod : MelonMod
    {
        private bool _initialized = false;
        private Type _carouselType = null;
        private MethodInfo _exportMethod = null;
        
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("========================================");
            MelonLogger.Msg("FM26 Ctrl+P Export Mod");
            MelonLogger.Msg("Versão: 1.0.0 (MelonLoader)");
            MelonLogger.Msg("Autor: Koda Assistant");
            MelonLogger.Msg("========================================");
        }
        
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Reinicializa a cada cena carregada
            _initialized = false;
        }
        
        public override void OnUpdate()
        {
            // Inicializa apenas uma vez por cena
            if (!_initialized)
            {
                InitializeReflection();
                _initialized = true;
            }
            
            // Detecta Ctrl+P
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                MelonLogger.Msg(">>> Ctrl+P detectado! Tentando exportar...");
                TryExportCurrentItem();
            }
        }
        
        private void InitializeReflection()
        {
            try
            {
                MelonLogger.Msg("[Init] Procurando tipos de carousel...");
                
                // Procura o tipo SICarousel em todos os assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        // Tenta diferentes nomes possíveis
                        var type = assembly.GetType("SI.Bindable.SICarousel");
                        if (type == null)
                            type = assembly.GetType("Bindable.SICarousel");
                        if (type == null)
                            type = assembly.GetType("SICarousel");
                            
                        if (type != null)
                        {
                            _carouselType = type;
                            MelonLogger.Msg($"[Init] Encontrado tipo: {type.FullName}");
                            MelonLogger.Msg($"[Init] Assembly: {assembly.FullName}");
                            
                            // Lista todos os métodos para debug
                            MelonLogger.Msg("[Init] Métodos disponíveis:");
                            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                            {
                                if (method.Name.Contains("Export") || method.Name.Contains("Binding"))
                                {
                                    MelonLogger.Msg($"  - {method.Name}({string.Join(", ", method.GetParameters().Length)} params)");
                                }
                            }
                            
                            // Busca o método de exportação
                            _exportMethod = type.GetMethod("UpdateExportCurrentItemBinding", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (_exportMethod != null)
                            {
                                MelonLogger.Msg("[Init] ✓ Método UpdateExportCurrentItemBinding encontrado!");
                            }
                            else
                            {
                                MelonLogger.Warning("[Init] ✗ Método UpdateExportCurrentItemBinding não encontrado");
                            }
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ignora assemblies que não carregam
                    }
                }
                
                // Se não encontrou, lista tipos similares
                if (_carouselType == null)
                {
                    MelonLogger.Warning("[Init] SICarousel não encontrado. Listando tipos similares...");
                    LogAllCarouselTypes();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Init] Erro: {ex.Message}");
            }
        }
        
        private void LogAllCarouselTypes()
        {
            int count = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Carousel") || 
                            type.Name.Contains("Export") ||
                            type.Name.Contains("Binding"))
                        {
                            MelonLogger.Msg($"  [Tipo] {type.FullName}");
                            count++;
                            if (count > 50) return; // Limita output
                        }
                    }
                }
                catch { }
            }
        }
        
        private void TryExportCurrentItem()
        {
            try
            {
                if (_carouselType == null)
                {
                    MelonLogger.Error("[Export] Tipo SICarousel não inicializado");
                    MelonLogger.Msg("[Export] Tentando inicializar novamente...");
                    InitializeReflection();
                    
                    if (_carouselType == null)
                    {
                        MelonLogger.Error("[Export] Falha na reinicialização");
                        return;
                    }
                }
                
                // Encontra todos os objetos SICarousel ativos
                var carouselObjects = UnityEngine.Object.FindObjectsOfType(_carouselType);
                
                if (carouselObjects == null || carouselObjects.Length == 0)
                {
                    MelonLogger.Warning("[Export] Nenhum carousel ativo encontrado");
                    MelonLogger.Msg("[Export] Dica: Selecione um jogador/tabela antes de usar Ctrl+P");
                    return;
                }
                
                MelonLogger.Msg($"[Export] Encontrados {carouselObjects.Length} carousels ativos");
                
                // Tenta exportar de cada carousel ativo
                foreach (var carousel in carouselObjects)
                {
                    MelonLogger.Msg($"[Export] Processando carousel: {carousel.name}");
                    
                    if (_exportMethod != null)
                    {
                        // Chama o método com índice 0 (item atual)
                        _exportMethod.Invoke(carousel, new object[] { 0 });
                        MelonLogger.Msg("[Export] ✓ Comando de exportação enviado!");
                    }
                    else
                    {
                        // Tenta métodos alternativos
                        TryAlternativeExport(carousel);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Export] Erro: {ex.Message}");
                if (ex.InnerException != null)
                {
                    MelonLogger.Error($"[Export] Inner: {ex.InnerException.Message}");
                }
            }
        }
        
        private void TryAlternativeExport(object carousel)
        {
            MelonLogger.Msg("[Export] Tentando métodos alternativos...");
            
            var type = carousel.GetType();
            
            // Lista de possíveis métodos de exportação
            string[] possibleMethods = {
                "ExportCurrentItem",
                "ExportSelected",
                "DoExport",
                "OnExport",
                "Export"
            };
            
            foreach (var methodName in possibleMethods)
            {
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (method != null)
                {
                    MelonLogger.Msg($"[Export] Tentando: {methodName}");
                    try
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Length == 0)
                        {
                            method.Invoke(carousel, null);
                            MelonLogger.Msg($"[Export] ✓ {methodName} executado!");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning($"[Export] Falha em {methodName}: {ex.Message}");
                    }
                }
            }
            
            MelonLogger.Error("[Export] Nenhum método alternativo funcionou");
        }
    }
}
