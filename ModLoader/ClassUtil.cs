using Mono.Cecil;
using Mono.Collections.Generic;

namespace ModLoader;

public static class ClassUtil
{
    public static bool ImplementsInterface(TypeDefinition type, Type interf)
    {
        return type.Interfaces.Any(
            t => t.InterfaceType.FullName == interf.FullName
        );
    }
    
    public static bool ImplementsInterface(Type type, Type interf)
    {
        return type.GetInterface(interf.Name) == interf;
    }

    public static (string tClass, string tMethod) GetMixinTarget(MethodDefinition method)
    {
        if (!TryGetAttributeArgs(method, "ModLib.MixinAttribute", out var args))
            throw new Exception($"Method {method.FullName} is not a Mixin method");
        
        if (args[0].Value is not TypeReference c) throw new Exception($"Invalid tClass param in Mixin Method {method.FullName}");
        if (args[1].Value is not string m) throw new Exception($"Invalid tMethod param in Mixin Method {method.FullName}");
        
        return (c.FullName, m);
    }
    
    public static bool TryGetAttributeArgs(MethodDefinition type, string fullName, out Collection<CustomAttributeArgument> args)
    {
        foreach (var attribute in type.CustomAttributes) {
            if (attribute.AttributeType.FullName != fullName)
                continue;
            
            args = attribute.ConstructorArguments;
            return true;
        }
        
        args = [];
        return false;
    }
    
}
