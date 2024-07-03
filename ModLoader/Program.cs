using System.Reflection;
using System.Reflection.Metadata;
using ModLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static ModLoader.ClassUtil;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;

namespace ModLoader;

public static class Program
{
    private static readonly string RootDirectory = Directory.GetCurrentDirectory();
    private static readonly string ModDirectory = $"{RootDirectory}/mods";
    
    private static string[] _modIds = [];
    
    private static void Main()
    {
        
        Directory.CreateDirectory(ModDirectory);
        
        var baseGame = ModuleDefinition.ReadModule($"{RootDirectory}/Game.dll");
        Console.WriteLine("--------[Apply Mods]--------");
        

        var modFiles = Directory.GetFiles(ModDirectory);
        var modIds = new List<string>(modFiles.Length);
        
        foreach (var file in modFiles)
        {
            var mod = ModuleDefinition.ReadModule(file);
            Console.WriteLine($"{mod.Name}:");
            
            foreach(var type in mod.Types)
            {
                if (type.FullName == "<Module>") continue;
                Console.WriteLine($" -> {type.Name}");
                
                if (ImplementsInterface(type, typeof(IModConfig))) // load mod conf
                {
                    modIds.Add("NYI");
                }
                else if (ImplementsInterface(type, typeof(IMixin))) // apply mixins
                {
                    Console.WriteLine("    -Mixins:");
                    foreach (var method in type.Methods)
                    {
                        if (method.Name == ".ctor") continue;
                        Console.WriteLine($"     -> {method.Name}");

                        try
                        {
                            var (tClass, tMethod) = GetMixinTarget(method);
                            
                            var c = baseGame.Types.First(t => t.FullName == tClass);
                            var m = c.Methods.First(m => m.Name == tMethod);

                            var instructions = method.Body.Instructions;
                            
                            
                            var processor = m.Body.GetILProcessor();
                            
                            var prev = m.Body.Instructions[^3];
                            foreach (var instruction in instructions)
                            {
                                if (instruction.OpCode == OpCodes.Ret) continue;
                                if (instruction.OpCode == OpCodes.Nop) continue;
                                
                                // import called methods
                                if (instruction.OpCode == OpCodes.Call)
                                {
                                    var cMethod = (MethodReference)instruction.Operand;
                                    Console.WriteLine(cMethod);
                                    var writeLine = baseGame.ImportReference(cMethod);
                                    instruction.Operand = writeLine;

                                }
                                
                                processor.InsertAfter(prev, instruction);
                                prev = instruction;
                            }


                            instructions = m.Body.Instructions;
                            
                            Console.WriteLine("========[DUMP]========");
                            foreach (var instruction in instructions)
                            {
                                Console.WriteLine($"[{instruction.Offset:0000}] {instruction.OpCode} {instruction.Operand}");
                            }
                            Console.WriteLine("========[DUMP]========");


                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Unable to load mixin {type.FullName}.{method.Name}(...): {e.Message}");
                        }

                    }

                }
                
                
            }
        }

        _modIds = modIds.ToArray();
        
        Console.WriteLine("--------[Build Assembly]--------");
        
        dynamic? t = 0;
        
        var gameAssemblyStream = new MemoryStream();
        baseGame.Assembly.Write(gameAssemblyStream);
        var game = Assembly.Load(gameAssemblyStream.ToArray());
        gameAssemblyStream.Dispose();
        
        Console.WriteLine("--------[Launch Game]--------");
        
        foreach (var type in game.ExportedTypes)
        {
            if (ImplementsInterface(type, typeof(IEntryPoint)))
            {
                t = (IEntryPoint?)Activator.CreateInstance(type);
                if (t == null) continue;
                t.Main(Array.Empty<string>());
            }
            
            // dispose if we can
            if (t is IDisposable disposable) 
                disposable.Dispose();
        }
        
        
        Console.WriteLine("--------[Result]--------");
        
        
        modIds.Clear();
        
        Console.WriteLine(_modIds[0]);
        
    }
}