using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.Remoting.Messaging;


// This script uses / adapts the BTMLModLoader (Public Domain)
// https://github.com/BattletechModders/BattleTechModLoader/releases
namespace AtlyssModLoader
{
    public static class AtlyssModLoader
    {
        private const int JSON_VERSION = 1;
        private const BindingFlags PUBLIC_STATIC_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static;
        private const string LOAD_CONFIG_FILE_NAME = "AtlyssModLoader_Load_Order_Config.Json";
        private static readonly List<string> IGNORE_FILE_NAMES = new List<string>()
        {
            "0Harmony.dll",
            "AtlyssModLoader.dll"
        };

        public static void LoadDLL(string path, string methodName = "Init", string typeName = null,
            object[] prms = null, BindingFlags bFlags = PUBLIC_STATIC_BINDING_FLAGS)
        {
            FileLog.Log("AtlyssModLoader is attempting to laod a DLL");
            var fileName = Path.GetFileName(path);
            
            try
            {
                var assembly = Assembly.LoadFrom(path);
                var name = assembly.GetName();
                var version = name.Version;
                var types = new List<Type>();

                // Find the type/s with our entry point/s
                if (typeName == null)
                {
                    types.AddRange(assembly.GetTypes().Where(x => x.GetMethod(methodName, bFlags) != null));
                }
                else
                {
                    types.Add(assembly.GetType(typeName));
                }

                if (types.Count == 0)
                {
                    FileLog.Log("  DLL loader type count was 0");
                    return;
                }

                foreach (var type in types)
                {
                    var entryMethod = type.GetMethod(methodName, bFlags);
                    var methodParams = entryMethod?.GetParameters();

                    if (methodParams == null)
                        continue;

                    if (methodParams.Length == 0)
                    {
                        FileLog.Log(  "AtlyssModLoader inserted a DLL method with no params");
                        entryMethod.Invoke(null, null);
                        continue;
                    }

                    // match up the passed in params with the method's params, if they match, call the method
                    if (prms != null && methodParams.Length == prms.Length)
                    {
                        var paramsMatch = true;
                        for (var i = 0; i < methodParams.Length; i++)
                        {
                            if (prms[i] != null && prms[i].GetType() != methodParams[i].ParameterType)
                            {
                                paramsMatch = false;
                            }
                        }

                        if (paramsMatch)
                        {
                            FileLog.Log("  AtlyssModLoader inserted a DLL method with params");
                            entryMethod.Invoke(null, prms);
                            continue;
                        }
                    }

                    FileLog.Log("  DLL loader fell through!!!!!");
                }
            }
            catch (Exception e)
            {
                FileLog.Log("  DLL hit an unexpected error!");
            }
        }

        /// <summary>
        /// 
        /// Credit: https://stackoverflow.com/questions/13297563/read-and-parse-a-json-file-in-c-sharp
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static async Task<T> ReadJsonAsync<T>(string filePath)
        {
            FileLog.Log("Reading Json...");
            if (!File.Exists(filePath))
            {
                FileLog.Log("  Could not find file");
                return await default(Task<T>).ConfigureAwait(false);
            }

            using FileStream stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<T>(stream).ConfigureAwait(false);
        }

        private static void WriteJson<T>(string filePath, T objectToSerialize)
        {
            FileLog.Log("Writing Json...");
            string jsonString = JsonSerializer.Serialize(objectToSerialize);
            File.WriteAllText(filePath, jsonString);
        }

        /// <summary>
        /// Mass-load newly added mods to the load order config.
        /// New mods will always come after already present entries.
        /// Order amongst the new mods is not controlled.
        /// </summary>
        /// <param name="modsToAdd"></param>
        private static void UpdateLoadOrder(string jsonDirectory, string modDirectory, List<string> modsToAdd, LoadOrderJsonData loadData)
        {
            FileLog.Log("UpdateLoadOrder Activated");
            // Clear out bad existing entries
            List<LoadOrderEntry> loadOrderEntries = loadData.LoadOrderEntries;
            for (int i = 0; i < loadData.LoadOrderEntries.Count; i++)
            {
                string dllPath = Path.Combine(modDirectory, loadOrderEntries[i].ModName);
                if (!File.Exists(dllPath)) 
                {
                    List<LoadOrderEntry> loadEntries = loadData.LoadOrderEntries;
                    loadEntries.RemoveAt(i);
                    loadOrderEntries = loadEntries.ToList();
                }
            }

            // Load in new entries
            foreach (string modName in modsToAdd)
            {
                FileLog.Log(modName);
                LoadOrderEntry newEntry = new LoadOrderEntry();
                newEntry.ModName = modName;
                newEntry.InternalVersion = 0;
                newEntry.ExternalVersion = "NOT IN USE";
                loadData.LoadOrderEntries.Add(newEntry);
            }

            WriteJson(jsonDirectory, loadData);
        }

        /// <summary>
        /// Checks if there is a load order entry for a dll (mod).
        /// If there is not one, prep it to be added to the config.
        /// </summary>
        /// <param name="dllPath"></param>
        /// <returns></returns>
        private static void EnsureLoadOrder(string dllPath, LoadOrderJsonData loadOrder, ref List<string> modsToAdd)
        {
            FileLog.Log("EnsureLoadOrder Activated");
            string modName = Path.GetFileName(dllPath); // TODO: Figure out the proper name scheme 
            foreach(LoadOrderEntry loadEntry in loadOrder.LoadOrderEntries)
            {
                if (loadEntry.ModName == modName)
                {
                    FileLog.Log("  Mod was already tracked");
                    return;
                }
            }
            FileLog.Log($"  Adding new mod with name {modName} to modsToAdd");
            modsToAdd.Add(modName);
            FileLog.Log($"  Oh oh, lookie here {modsToAdd.Count}");
        }

        /// <summary>
        ///  Gets the path to the load order config.
        ///  Will create the file if it does not exist.
        /// </summary>
        /// <returns></returns>
        private static string GetLoadConfigFile(string modDirectoy)
        {
            string configFilePath = Path.Combine(modDirectoy, LOAD_CONFIG_FILE_NAME);
            if (!File.Exists(configFilePath))
            {
                File.Create(configFilePath);
                LoadOrderJsonData defaultData = new LoadOrderJsonData();
                defaultData.JsonVersion = JSON_VERSION;
                defaultData.LoadOrderEntries = new List<LoadOrderEntry>();
                WriteJson(configFilePath, defaultData);
            }
            return configFilePath;
        }

        /// <summary>
        ///  Gets the path to the mod folder
        ///  Will create the folder if it does not exist
        /// </summary>
        /// <returns></returns>
        private static string GetModDirectory(string loaderDirectory)
        {
            string modDirectoryPath = Path.GetFullPath(Path.Combine(loaderDirectory, "Mods"));
            if (!Directory.Exists(modDirectoryPath))
            {
                Directory.CreateDirectory(modDirectoryPath);
            }
            return modDirectoryPath;
        }

        private static async Task InitAsync()
        {
            Harmony.DEBUG = false;
            FileLog.Log("AtlyssModLoader Init has begun");
            string loaderDirectory = Directory.GetCurrentDirectory();
            string modDirectory = GetModDirectory(loaderDirectory);
            string loadConfigFilePath = GetLoadConfigFile(modDirectory);

            Harmony harmony = new Harmony("io.github.robocat999.AtlyssModLoader");
            List<string> dllPaths = Directory.GetFiles(modDirectory).Where(x => Path.GetExtension(x).ToLower() == ".dll").ToList();

            // Catch no mods loaded
            if (dllPaths.Count == 0)
            {
                FileLog.Log("AtlyssModLoader Init found no mods to load");
                return;
            }

            LoadOrderJsonData loadOrder = await ReadJsonAsync<LoadOrderJsonData>(loadConfigFilePath).ConfigureAwait(false);
            List<string> modsToAdd = new List<string>();
            FileLog.Log("Beat the Read Async");

            foreach (var dllPath in dllPaths)
            {
                if (IGNORE_FILE_NAMES.Contains(Path.GetFileName(dllPath)))
                    continue;

                EnsureLoadOrder(dllPath, loadOrder, ref modsToAdd); 
            }
            FileLog.Log($"  ModsToAdd length: {modsToAdd.Count}");
            UpdateLoadOrder(loadConfigFilePath, modDirectory, modsToAdd, loadOrder);
            
            foreach (LoadOrderEntry modEntry in loadOrder.LoadOrderEntries)
            {
                string dllPath = Path.Combine(modDirectory, modEntry.ModName);
                LoadDLL(dllPath);
            }

            FileLog.Log("AtlyssModLoader Init has completed normally");
        }

        // Entry Point
        public static void Init()
        {
            InitAsync().Wait();
        }
    }
}
