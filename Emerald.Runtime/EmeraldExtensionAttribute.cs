using System;

namespace Emerald.Runtime
{
    /// <summary>
    /// Marks an assembly as an Emerald extension: a content pack whose <see cref="Commands.CommandController"/>s,
    /// services and formatters should be discovered and registered at startup. The host scans every
    /// loaded assembly carrying this attribute, so a content DLL dropped into GameData auto-registers
    /// with no change to the addon.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class EmeraldExtensionAttribute : Attribute
    {
    }
}
