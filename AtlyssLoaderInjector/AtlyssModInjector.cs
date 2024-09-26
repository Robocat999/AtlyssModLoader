using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodDefinition = Mono.Cecil.MethodDefinition;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;

// This script uses / adapts the BTMLInjector (Public Domain)
// https://github.com/BattletechModders/BattleTechModLoader/releases
namespace AtlyssLoaderInjector
{
    internal class AtlyssModInjector
    {
        private const string ModLoaderDllFileName = "AtlyssModLoader.dll";

        private const string GameDllFileName = "Assembly-CSharp.dll";
        private const string BackupFileExt = ".orig";

        // TODO: These need to bedone for Atlyss
        private const string HookType = "GameManager";
        private const string HookMethod = "Awake";
        private const string InjectType = "AtlyssModLoader.AtlyssModLoader";
        private const string InjectMethod = "Init";

        private static int Main(string[] args)
        {
            Console.WriteLine("Press any key to begin the Atlyys Mod Injector");
            Console.ReadKey();
            Console.WriteLine("Started injector main");
            var directory = Directory.GetCurrentDirectory();
            var gameDllPath = Path.Combine(directory, "ATLYSS_Data", "Managed", GameDllFileName);
            gameDllPath = Path.GetFullPath(gameDllPath);
            var gameDllBackupPath = Path.Combine(directory, "ATLYSS_Data", "Managed", GameDllFileName + BackupFileExt);
            gameDllBackupPath = Path.GetFullPath(gameDllBackupPath);
            var modLoaderDllPath = Path.Combine(directory, "ATLYSS_Data", "Managed", ModLoaderDllFileName);
            modLoaderDllPath = Path.GetFullPath(modLoaderDllPath);

            var injected = IsInjected(gameDllPath);
            if (injected)
            {
                Console.WriteLine("Injector is already injected");
                Console.ReadKey();
                return 1;
            }

            Console.WriteLine("Attempting injection");
            Backup(gameDllPath, gameDllBackupPath);
            Inject(gameDllPath, modLoaderDllPath);

            Console.WriteLine("The Atlyss Mod Injector has successfully completed!");
            Console.ReadKey();
            return 0;
        }

        private static void Backup(string filePath, string backupFilePath)
        {
            Console.WriteLine("Backing up orginal .dll");
            File.Copy(filePath, backupFilePath, true);
        }

        private static void Inject(string hookFilePath, string injectFilePath)
        {
            Console.WriteLine("Injecting");
            string oldDirectory = Directory.GetCurrentDirectory();
            string newDirectory = Path.GetDirectoryName(hookFilePath);
            Directory.SetCurrentDirectory(newDirectory);
            Console.WriteLine(oldDirectory);
            Console.WriteLine(newDirectory);
            Console.WriteLine(hookFilePath);
            Console.WriteLine(injectFilePath);
            var success = true;
            using (var game = ModuleDefinition.ReadModule(hookFilePath, new ReaderParameters { ReadWrite = true }))
            using (var injecting = ModuleDefinition.ReadModule(injectFilePath))
            {
                success = InjectModHookPoint(game, injecting);
                if (success)
                {
                    success = WriteNewAssembly(hookFilePath, game);
                    if (!success)
                        Console.WriteLine("WriteNewAssembly FAILED!");
                } else
                    Console.WriteLine("InjectModHookPoint FAILED!");
            }
            Directory.SetCurrentDirectory(oldDirectory);
        }

        // Primarily used to save modified assembly
        private static bool WriteNewAssembly(string hookFilePath, ModuleDefinition game)
        {
            Console.WriteLine("Writing new assembly");
            game.Write();
            return true;
        }

        private static bool InjectModHookPoint(ModuleDefinition game, ModuleDefinition injecting)
        {
            Console.WriteLine("Injecting hookpoints");
            // get the methods that we're hooking and injecting
            var injectedMethod = injecting.GetType(InjectType).Methods.Single(x => x.Name == InjectMethod);
            var hookedMethod = game.GetType(HookType).Methods.First(x => x.Name == HookMethod);

            // If the return type is an iterator -- need to go searching for its MoveNext method which contains the actual code you'll want to inject
            if (hookedMethod.ReturnType.Name.Equals("IEnumerator"))
            {
                var nestedIterator = game.GetType(HookType).NestedTypes.First(x => x.Name.Contains(HookMethod) && x.Name.Contains("Iterator"));
                hookedMethod = nestedIterator.Methods.First(x => x.Name.Equals("MoveNext"));
            }

            // TODO: Evaluate if this is a thing Atlyss needs to deal with
            // We want to inject after the PrepareSerializer call -- so search for that call in the CIL
            int targetInstruction = -1;
            for (int i = 0; i < hookedMethod.Body.Instructions.Count; i++)
            {
                var instruction = hookedMethod.Body.Instructions[i];
                if (instruction.OpCode.Code.Equals(Code.Call) && instruction.OpCode.OperandType.Equals(OperandType.InlineMethod))
                {
                    MethodReference methodReference = (MethodReference)instruction.Operand;
                    if (methodReference.Name.Contains("DontDestroyOnLoad"))
                    {
                        targetInstruction = i;
                    }
                }
            }

            if (targetInstruction == -1)
            {
                Console.WriteLine("Target Instruction was left as -1, indicating a failuire to find DontDestroyOnLoad");
                return false;
            }
                
            hookedMethod.Body.GetILProcessor().InsertAfter(hookedMethod.Body.Instructions[targetInstruction],
                Instruction.Create(OpCodes.Call, game.ImportReference(injectedMethod)));
            return true;
        }

        private static bool IsInjected(string dllPath)
        {
            Console.WriteLine("Checks if .dll is injected");
            var detectedInject = false;

            using (var dll = ModuleDefinition.ReadModule(dllPath))
            {
                foreach (TypeDefinition type in dll.Types)
                {
                    // Standard methods
                    foreach (var methodDefinition in type.Methods)
                    {
                        if (IsHookInstalled(methodDefinition))
                        {
                            detectedInject = true;
                        }
                    }

                    // Also have to check in places like IEnumerator generated methods (Nested)
                    foreach (var nestedType in type.NestedTypes)
                    {
                        foreach (var methodDefinition in nestedType.Methods)
                        {
                            if (IsHookInstalled(methodDefinition))
                            {
                                detectedInject = true;
                            }
                        }
                    }
                }
            }
            return detectedInject;
        }

        private static bool IsHookInstalled(MethodDefinition methodDefinition)
        {
            if (methodDefinition.Body == null)
                return false;

            foreach (var instruction in methodDefinition.Body.Instructions)
            {
                if (instruction.OpCode.Equals(OpCodes.Call) &&
                    instruction.Operand.ToString().Equals($"System.Void {InjectType}::{InjectMethod}()"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
