using Game;
using ModLib;

namespace TestMod;

public class MainMixins : IMixin
{
    
    [Mixin(typeof(Program), "Main")]
    public void Test(string[] args)
    {
        Console.WriteLine("Injected Hello from test mod :D");
    }
    
}
