namespace ModLib;

[AttributeUsage(AttributeTargets.Method)]
public class MixinAttribute : Attribute
{
    public MixinAttribute(Type tClass, string tMethod)
    {
        
    }
    
}