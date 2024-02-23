using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using System.IO;


// This script uses / adapts the BTMLModLoader (Public Domain)
// https://github.com/BattletechModders/BattleTechModLoader/releases
namespace AtlyssModLoader
{
    public static class AtlyssModLoader
    {
        private const BindingFlags PUBLIC_STATIC_BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static;
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

        public static void Init()
        {
            Harmony.DEBUG = false;
            FileLog.Log("AtlyssModLoader Init has begun");
            var loaderDirectory = Directory.GetCurrentDirectory();
            var modDirectory = Path.GetFullPath(Path.Combine(loaderDirectory, "Mods"));
            modDirectory = Path.GetFullPath(modDirectory);

            if (!Directory.Exists(modDirectory))
                Directory.CreateDirectory(modDirectory);

            var harmony = new Harmony("io.github.robocat999.AtlyssModLoader");

            FileLog.Log(modDirectory);
            var dllPaths = Directory.GetFiles(modDirectory).Where(x => Path.GetExtension(x).ToLower() == ".dll").ToArray();

            // Catch no mods loaded
            if (dllPaths.Length == 0)
            {
                FileLog.Log("AtlyssModLoader Init found no mods to load");
                return;
            }

            foreach ( var dllPath in dllPaths)
            {
                if (!IGNORE_FILE_NAMES.Contains(Path.GetFileName(dllPath)))
                    LoadDLL(dllPath);
            }
            FileLog.Log("AtlyssModLoader Init has completed normally");
        }


    }
}
