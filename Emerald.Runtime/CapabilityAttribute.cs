using System;

namespace Emerald.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CapabilityAttribute : Attribute
    {
        public CapabilityAttribute(string id, bool safe = false)
        {
            Id = id;
            Safe = safe;
        }

        public readonly string Id;
        public readonly bool Safe;
    }
}