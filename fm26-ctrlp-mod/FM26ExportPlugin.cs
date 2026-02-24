using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FM26ExportMod
{
    [BepInPlugin("com.koda.fm26.ctrlp", "FM26 Ctrl+P Export", "1.0.0")]
    public class FM26ExportPlugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;
        
        private MethodInfo _updateExportMethod = null;
        private Type _carouselType = null;
        private bool _initialized = false;
        
        void Awake()
        {
            Log = Logger;
            Log.LogInfo("========================================");
            Log.LogInfo("FM26 Ctrl+P Export Mod v1.0.0");
            Log.LogInfo("========================================");
            
            // Harmony patches
            var harmony = new Harmony("com.koda.fm26.ctrlp");
            harmony.PatchAll();
            
            Log.LogInfo("[Init] Harmony patches applied");
        }
        
        void Start()
        {
            Log.LogInfo("[Init] Procurando ExportCurrentItemToBinding...");
            
            // Procura a função em todos os assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.GetName().Name.StartsWith("System")) continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        // Procura SICarousel ou similar
                        if (type.Name.Contains("Carousel") || type.Name.Contains("TableView"))
                        {
                            Log.LogInfo($"[Tipo] {type.FullName}");
                            
                            // Procura UpdateExportCurrentItemBinding
                            var method = type.GetMethod("UpdateExportCurrentItemBinding", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (method != null)
                            {
                                Log.LogInfo($"[OK] Metodo encontrado: {method.Name}");
                                _carouselType = type;
                                _updateExportMethod = method;
                            }
                        }
                    }
                }
                catch { }
            }
            
            if (_updateExportMethod == null)
            {
                Log.LogWarning("[Init] Metodo NAO encontrado - tentando em tempo de execucao");
            }
        }
        
        void Update()
        {
            // Tenta inicializar novamente se não encontrou
            if (!_initialized)
            {
                _initialized = true;
                Start();
            }
            
            // Ctrl+P
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                Log.LogInfo(">>> Ctrl+P DETECTADO!");
                TryExport();
            }
            
            // F10 - debug
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Log.LogInfo(">>> F10 - Listando todos os tipos...");
                LogAllTypes();
            }
        }
        
        void TryExport()
        {
            Log.LogInfo("[Export] Iniciando exportacao...");
            
            try
            {
                // Se encontrou o método, usa ele
                if (_updateExportMethod != null && _carouselType != null)
                {
                    Log.LogInfo("[Export] Usando UpdateExportCurrentItemBinding...");
                    
                    // Encontra instâncias ativas do carousel
                    var objects = FindObjectsOfType(_carouselType);
                    Log.LogInfo($"[Export] Encontrados {objects.Length} carousels ativos");
                    
                    foreach (var obj in objects)
                    {
                        Log.LogInfo($"[Export] Chamando export em: {obj.name}");
                        _updateExportMethod.Invoke(obj, new object[] { 0 });
                    }
                }
                else
                {
                    Log.LogWarning("[Export] Metodo nao encontrado - buscando alternativas...");
                    FindAndCallExport();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Export] Erro: {ex.Message}");
                Log.LogError(ex.StackTrace);
            }
        }
        
        void FindAndCallExport()
        {
            // Lista todos os métodos com "Export" no nome
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                        {
                            if (method.Name.Contains("Export") || method.Name.Contains("export"))
                            {
                                Log.LogInfo($"[Export] Metodo: {type.Name}.{method.Name}");
                            }
                        }
                    }
                }
                catch { }
            }
        }
        
        void LogAllTypes()
        {
            int count = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var name = assembly.GetName().Name;
                    if (name.StartsWith("System") || name.StartsWith("Mono")) continue;
                    
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains("Export") || 
                            type.Name.Contains("Carousel") || 
                            type.Name.Contains("Table") ||
                            type.Name.Contains("Grid"))
                        {
                            Log.LogInfo($"[Tipo] {type.FullName}");
                            count++;
                        }
                    }
                }
                catch { }
            }
            Log.LogInfo($"[Debug] Total: {count}");
        }
    }
}
