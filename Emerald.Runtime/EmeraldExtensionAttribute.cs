using System;

namespace Emerald.Runtime
{
    /// <summary>
    /// Marks an assembly as an Emerald extension. Emerald will load the commands, services, etc. from any assembly
    /// with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class EmeraldExtensionAttribute : Attribute
    {
    }
}
