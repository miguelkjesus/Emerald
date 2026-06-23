using System;

namespace Emerald.Runtime.Capabilities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CapabilityAttribute : Attribute
    {
        public CapabilityAttribute(string id)
        {
            Id = id;
        }

        public readonly string Id;
    }
}