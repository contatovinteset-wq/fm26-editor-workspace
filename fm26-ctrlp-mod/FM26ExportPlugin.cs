using System;
using System.Reflection;
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
            try
            {
                Log.LogInfo("========================================");
                Log.LogInfo("FM26 Ctrl+P Export v1.0.0 CARREGADO!");
                Log.LogInfo("========================================");
                
                // Cria GameObject para rodar o MonoBehaviour
                var go = new GameObject("FM26CtrlPRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<CtrlPRunner>();
                
                Log.LogInfo("[Init] Componente adicionado com sucesso!");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Init] Erro ao carregar: {ex.Message}");
                Log.LogError($"[Init] StackTrace: {ex.StackTrace}");
            }
        }
    }
    
    public class CtrlPRunner : MonoBehaviour
    {
        private MethodInfo _exportMethod = null;
        private Type _targetType = null;
        private float _searchTimer = 0f;
        private bool _initialized = false;
        
        void Awake()
        {
            Debug.Log("[FM26CtrlP] CtrlPRunner Awake()");
        }
        
        void Start()
        {
            Debug.Log("[FM26CtrlP] CtrlPRunner Start() - Buscando método...");
            SearchExportMethod();
        }
        
        void Update()
        {
            // Busca método periodicamente se não encontrou
            _searchTimer += Time.deltaTime;
            if (!_initialized && _searchTimer > 5f)
            {
                _searchTimer = 0f;
                SearchExportMethod();
            }
            
            // Ctrl+P
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("[FM26CtrlP] >>> Ctrl+P PRESSIONADO!");
                TryExport();
            }
            
            // F10 - Debug
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Debug.Log("[FM26CtrlP] >>> F10 - Listando tipos...");
                ListTypes();
            }
        }
        
        void SearchExportMethod()
        {
            if (_initialized) return;
            
            try
            {
                Debug.Log("[FM26CtrlP] Procurando UpdateExportCurrentItemBinding...");
                
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var name = assembly.GetName().Name;
                        if (name.StartsWith("System") || name.StartsWith("Mono") || name.StartsWith("mscorlib"))
                            continue;
                        
                        foreach (var type in assembly.GetTypes())
                        {
                            var method = type.GetMethod("UpdateExportCurrentItemBinding",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (method != null)
                            {
                                _targetType = type;
                                _exportMethod = method;
                                _initialized = true;
                                Debug.Log($"[FM26CtrlP] ENCONTRADO: {type.FullName}");
                                return;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro na busca: {ex.Message}");
            }
        }
        
        void TryExport()
        {
            if (_exportMethod == null || _targetType == null)
            {
                Debug.LogWarning("[FM26CtrlP] Método não encontrado ainda");
                SearchExportMethod();
                return;
            }
            
            try
            {
                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", Type.EmptyTypes);
                var generic = findMethod.MakeGenericMethod(_targetType);
                var results = generic.Invoke(null, null) as UnityEngine.Object[];
                
                Debug.Log($"[FM26CtrlP] {results?.Length ?? 0} objetos encontrados");
                
                if (results != null)
                {
                    foreach (var obj in results)
                    {
                        Debug.Log($"[FM26CtrlP] Exportando: {obj.name}");
                        _exportMethod.Invoke(obj, new object[] { 0 });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FM26CtrlP] Erro no export: {ex.Message}");
            }
        }
        
        void ListTypes()
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
                        if (type.Name.Contains("Export") || type.Name.Contains("Carousel") || 
                            type.Name.Contains("Table") || type.Name.Contains("View"))
                        {
                            Debug.Log($"[FM26CtrlP] Tipo: {type.FullName}");
                            count++;
                            if (count > 30) return;
                        }
                    }
                }
                catch { }
            }
        }
    }
}
