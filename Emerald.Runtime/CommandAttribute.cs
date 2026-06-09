using System;

namespace Emerald.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandAttribute : Attribute
    {
        public CommandAttribute(string id, Capabilities requiredCapabilities = Capabilities.None)
        {
            Id = id;
            RequiredCapabilities = requiredCapabilities;
        }

        public readonly string Id;
        public readonly Capabilities RequiredCapabilities;
    }
}