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
            MelonLogger.Msg("========================================");
        }
        
        public override void OnUpdate()
        {
            // Inicializa apenas uma vez
            if (!_initialized)
            {
                InitializeReflection();
                _initialized = true;
            }
            
            // Detecta Ctrl+P
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                MelonLogger.Msg("Ctrl+P detectado! Tentando exportar...");
                TryExportCurrentItem();
            }
        }
        
        private void InitializeReflection()
        {
            try
            {
                // Procura o tipo SICarousel em todos os assemblies
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var type = assembly.GetType("SI.Bindable.SICarousel");
                        if (type != null)
                        {
                            _carouselType = type;
                            MelonLogger.Msg($"Encontrado SICarousel em: {assembly.FullName}");
                            
                            // Busca o método de exportação
                            _exportMethod = type.GetMethod("UpdateExportCurrentItemBinding", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (_exportMethod != null)
                            {
                                MelonLogger.Msg("Método UpdateExportCurrentItemBinding encontrado!");
                            }
                            break;
                        }
                    }
                    catch { }
                }
                
                if (_carouselType == null)
                {
                    MelonLogger.Warning("SICarousel não encontrado. Procurando tipos similares...");
                    LogAllCarouselTypes();
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Erro na inicialização: {ex.Message}");
            }
        }
        
        private void LogAllCarouselTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Carousel") || type.Name.Contains("Export"))
                        {
                            MelonLogger.Msg($"Tipo encontrado: {type.FullName}");
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
                    MelonLogger.Error("Tipo SICarousel não inicializado");
                    return;
                }
                
                // Encontra todos os objetos SICarousel ativos
                var carouselObjects = UnityEngine.Object.FindObjectsOfType(_carouselType);
                
                if (carouselObjects == null || carouselObjects.Length == 0)
                {
                    MelonLogger.Warning("Nenhum carousel ativo encontrado");
                    return;
                }
                
                MelonLogger.Msg($"Encontrados {carouselObjects.Length} carousels ativos");
                
                // Tenta exportar do primeiro carousel ativo
                var carousel = carouselObjects[0];
                MelonLogger.Msg($"Carousel encontrado: {carousel.name}");
                
                if (_exportMethod != null)
                {
                    // Chama o método com índice 0 (item atual)
                    _exportMethod.Invoke(carousel, new object[] { 0 });
                    MelonLogger.Msg("Comando de exportação enviado com sucesso!");
                }
                else
                {
                    MelonLogger.Error("Método de exportação não encontrado");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Erro ao exportar: {ex.Message}");
                MelonLogger.Error(ex.StackTrace);
            }
        }
    }
}
