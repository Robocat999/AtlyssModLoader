using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security;


// This script uses / adapts the BTMLModLoader (Public Domain)
// https://github.com/BattletechModders/BattleTechModLoader/releases
namespace AtlyssModLoader
{
    public static class AtlyssModLoader
    {
        private const int JSON_VERSION = 1;
        private const BindingFlags PUBLIC_STATIC_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static;
        private const string LOAD_CONFIG_FILE_NAME = "AtlyssModLoader_Load_Order_Config.Json";
        private const string LOADER_VERSION = "AtlyssModLoader Version 0.2.2";
        private static readonly List<string> IGNORE_FILE_NAMES = new List<string>()
        {
            "0Harmony.dll",
            "AtlyssModLoader.dll"
        };
        private static bool LoaderInfoReported = false;

        /// <summary>
        /// Loads a DLL and calls its Init function so it may patch the game
        /// </summary>
        /// <param name="path"></param>
        /// <param name="methodName"></param>
        /// <param name="typeName"></param>
        /// <param name="prms"></param>
        /// <param name="bFlags"></param>
        public static void LoadDLL(string path, string methodName = "Init", string typeName = null,
            object[] prms = null, BindingFlags bFlags = PUBLIC_STATIC_BINDING_FLAGS)
        {
            DebugLog("AtlyssModLoader is attempting to load a DLL");
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
                    DebugLog("  DLL loader type count was 0");
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
                        DebugLog(  "AtlyssModLoader inserted a DLL method with no params");
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
                FileLog.Log($"{e}");
            }
        }

        /// <summary>
        /// Logs a message only if Harmony.DEBUG is true
        /// </summary>
        /// <param name="message"></param>
        private static void DebugLog(string message)
        {
            if (Harmony.DEBUG)
                FileLog.Log(message);
        }

        /// <summary>
        /// Used when the loader has encournted a major error. Indicates info useful for identifying user version
        /// Will refuse to display if info has already been displayed elsewhere
        /// </summary>
        private static void LogLoaderInfo()
        {
            if (LoaderInfoReported)
                return;

            FileLog.Log($"\n\n -- LOADER INFO --");
            FileLog.Log($"  Loader Version: {LOADER_VERSION}");
            FileLog.Log($"  Debugging Enabled: {Harmony.DEBUG}");
            FileLog.Log($"  \nThis info is being presented due to a major error occuring");
            FileLog.Log($"  Please include it if you are providing only a portion of the log as an error report");
            FileLog.Log($"  If AtlyssModLoader is giving you trouble, please contanct Robocat999 on the Atlyss Discord\n\n");

            LoaderInfoReported = true;
        }

        /// <summary>
        /// Reads a Json file
        /// Credit: https://stackoverflow.com/questions/13297563/read-and-parse-a-json-file-in-c-sharp
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static async Task<T> ReadJsonAsync<T>(string filePath)
        {
            DebugLog("Reading Json...");
            if (!File.Exists(filePath))
            {
                LogLoaderInfo();
                FileLog.Log($"  Could not find Json file at path {filePath}");
                FileLog.Log($"  Returing default value! Failure is likely imminent!");
                return default(T);
            }

            // TODO: Pull verification out of this function and instead return a workable error 
            
            try
            {
                using (FileStream firstStream = File.OpenRead(filePath))
                {
                    var firstAttemptRead = await JsonSerializer.DeserializeAsync<T>(firstStream).ConfigureAwait(false);
                    return firstAttemptRead;
                }
            }
            catch (JsonException e)
            {
                if (Path.GetFileName(filePath) == LOAD_CONFIG_FILE_NAME)
                {
                    DebugLog("  Config file was bad. Regenerating...");

                    // Regenerate a bad config
                    File.Delete(filePath);
                    GetLoadConfigFile(Path.GetDirectoryName(filePath));
                }
                else
                {
                    FileLog.Log("  Json file encountered an unknown error in reading!");
                    return default(T);
                }
            }

            // Second read for a regenerated file
            DebugLog("  Attempting the second read for Json file");

            try
            {
                using (FileStream secondStream = File.OpenRead(filePath))
                {
                    var secondAttemptRead = await JsonSerializer.DeserializeAsync<T>(secondStream).ConfigureAwait(false);
                    return secondAttemptRead;
                }
            }
            catch (JsonException e)
            {
                LogLoaderInfo();
                FileLog.Log("  Json file was unreadable on second read!");
            }

            LogLoaderInfo();
            FileLog.Log("  All reads failed! ReadJsonAsync is retuning a default value!");
            FileLog.Log("    Please contact Robocat999 on the Atlyss Discord!");
            return default(T);
        }

        /// <summary>
        /// Writes an object to a Json file
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="objectToSerialize"></param>
        private static void WriteJson<T>(string filePath, T objectToSerialize)
        {
            DebugLog("Writing Json...");

            string jsonString = JsonSerializer.Serialize(objectToSerialize);

            DebugLog($"  Json string is: {jsonString}");

            try
            {
                DebugLog($"  Attempting Write...");
                File.WriteAllText(filePath, jsonString);
                DebugLog($"  Write Completed with no errors!");
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                FileLog.Log("  Failed to write to Json file due to an UnauthorizedAccessExcpetion!");
                FileLog.Log("    Please attempt to run ATLYSS as an administrator!");
                FileLog.Log("    If encountering further issues, also attempt injecting as an administrator!");
                FileLog.Log("      Remember to repair the ATLYSS files through Steam to clear out the orignal injection!");
                FileLog.Log($"     \nRaw Exception: {e.ToString()}\n\n");
            }
            catch (PathTooLongException e)
            {
                FileLog.Log("  Failed to write to Json file due to the file path being too long!");
                FileLog.Log($"     Attempted Path: {filePath}");
                FileLog.Log("     Does this look right?");
                FileLog.Log($"     \nRaw Exception: {e.ToString()}\n\n");
            }
            catch (DirectoryNotFoundException e)
            {
                FileLog.Log("  Failed to write to Json file due to an issue in finding the directory!");
                FileLog.Log($"     Attempted Path: {filePath}");
                FileLog.Log("     Does this look right?");
                FileLog.Log($"     \nRaw Exception: {e.ToString()}\n\n");
            }
            catch (IOException e)
            {
                FileLog.Log("  Failed to write to Json file due to an IO Error!");
                FileLog.Log("     Is your PC overloaded?");
                FileLog.Log("     If not, please contact Robocat999 on the Atlyss Discord");
                FileLog.Log($"     \nRaw Exception: {e.ToString()}\n\n");
            }
            catch (SecurityException e)
            {
                FileLog.Log("  Failed to write to Json file due to a Security Error!");
                FileLog.Log("    Please attempt to run ATLYSS as an administrator!");
                FileLog.Log("    If encountering further issues, also attempt injecting as an administrator!");
                FileLog.Log("      Remember to repair the ATLYSS files through Steam to clear out the orignal injection!");
                FileLog.Log($"     \nRaw Exception: {e.ToString()}\n\n");
            }
            catch (NotSupportedException e)
            {
                FileLog.Log("  Failed to write to Json file due to a NotSuppourtedException!");
                FileLog.Log("    Please contact Robocat999 on the Atlyss Discord!");
                FileLog.Log($"     \nRaw Exception: {e.ToString()}\n\n");
            }
            catch (Exception e)
            {
                FileLog.Log("  Failed to write to Json file due to an unspecified error!");
                FileLog.Log("    Please contact Robocat999 on the Atlyss Discord!");
                FileLog.Log($"     \nRaw Exception: {e.ToString()}\n\n");
            }

            LogLoaderInfo();
            FileLog.Log("  Attempting emergency Json repair...");

            // Check if the JSON file already exists
            if (!File.Exists(filePath))
            {
                FileLog.Log($"    No known Json file at path: {filePath}");
                FileLog.Log("  Emergency repair failed!");
                return;
            }

            if (new FileInfo(filePath).Length == 0)
            {
                FileLog.Log("  Existing blank Json file detected!");
                FileLog.Log("    Attempting to append Json string instead...");

                try
                {
                    File.AppendAllText(filePath, jsonString);
                    FileLog.Log("    Append succesful!");
                    FileLog.Log("    If this is a rare occurance, please disregard this logfile");
                    FileLog.Log("    If this is a common occurance, please contact Robocat999 on the Atlyss Discord!");
                    return;
                }
                catch (Exception e)
                {
                    FileLog.Log("    Append failed with an unspecified error!");
                    FileLog.Log($"      \nRaw Exception: {e.ToString()}\n\n");
                }
            }

            FileLog.Log("  All emergency repairs failed! ");
        }

        /// <summary>
        /// Mass-load newly added mods to the load order config.
        /// New mods will always come after already present entries.
        /// Order amongst the new mods is not controlled.
        /// </summary>
        /// <param name="modsToAdd"></param>
        private static void UpdateLoadOrder(string jsonDirectory, string modDirectory, List<string> modsToAdd, LoadOrderJsonData loadData)
        {
            DebugLog("UpdateLoadOrder Activated");
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
            DebugLog("EnsureLoadOrder Activated");
            string modName = Path.GetFileName(dllPath); // TODO: Figure out the proper name scheme 
            foreach(LoadOrderEntry loadEntry in loadOrder.LoadOrderEntries)
            {
                if (loadEntry.ModName == modName)
                {
                    DebugLog("  Mod was already tracked");
                    return;
                }
            }

            DebugLog($"  Adding new mod with name {modName} to modsToAdd");
            modsToAdd.Add(modName);
        }

        /// <summary>
        ///  Gets the path to the load order config.
        ///  Will create the file if it does not exist.
        /// </summary>
        /// <returns></returns>
        private static string GetLoadConfigFile(string modDirectoy)
        {
            DebugLog("Getting Load Config File");

            string configFilePath = Path.Combine(modDirectoy, LOAD_CONFIG_FILE_NAME);
            if (!File.Exists(configFilePath))
            {
                DebugLog("  Generating a new Load Config File");

                File.Create(configFilePath);
                LoadOrderJsonData defaultData = new LoadOrderJsonData();
                defaultData.JsonVersion = JSON_VERSION;
                defaultData.LoadOrderEntries = new List<LoadOrderEntry>();
                WriteJson(configFilePath, defaultData);
            }
            else
            {
                DebugLog("  Load Config File already exists. Returning path");
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
            DebugLog("AtlyssModLoader Init has begun");
            string loaderDirectory = Directory.GetCurrentDirectory();
            string modDirectory = GetModDirectory(loaderDirectory);
            string loadConfigFilePath = GetLoadConfigFile(modDirectory);

            Harmony harmony = new Harmony("io.github.robocat999.AtlyssModLoader");
            List<string> dllPaths = Directory.GetFiles(modDirectory).Where(x => Path.GetExtension(x).ToLower() == ".dll").ToList();

            // Catch no mods loaded
            if (dllPaths.Count == 0)
            {
                DebugLog("AtlyssModLoader Init found no mods to load");
                return;
            }

            LoadOrderJsonData loadOrder = await ReadJsonAsync<LoadOrderJsonData>(loadConfigFilePath).ConfigureAwait(false);
            List<string> modsToAdd = new List<string>();

            foreach (var dllPath in dllPaths)
            {
                if (IGNORE_FILE_NAMES.Contains(Path.GetFileName(dllPath)))
                    continue;

                EnsureLoadOrder(dllPath, loadOrder, ref modsToAdd); 
            }

            UpdateLoadOrder(loadConfigFilePath, modDirectory, modsToAdd, loadOrder);
            
            foreach (LoadOrderEntry modEntry in loadOrder.LoadOrderEntries)
            {
                string dllPath = Path.Combine(modDirectory, modEntry.ModName);
                LoadDLL(dllPath);
            }

            DebugLog("AtlyssModLoader Init has completed normally");
        }

        // Entry Point
        public static void Init()
        {
            Harmony.DEBUG = true;
            InitAsync().Wait();
        }
    }
}
