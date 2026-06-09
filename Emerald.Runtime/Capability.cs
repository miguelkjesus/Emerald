using System;
using System.Collections.Generic;
using System.Reflection;

namespace Emerald.Runtime
{
    public static class Capability
    {
        public static string[] GetIds(Capabilities capabilities)
        {
            if (capabilities == Capabilities.None)
                return new string[] {};

            var ids = new List<string>();

            foreach (Capabilities capability in Enum.GetValues(typeof(Capabilities)))
            {
                if (capability == Capabilities.None)
                    continue;

                if ((capabilities & capability) != capability)
                    continue;

                var field = typeof(Capabilities).GetField(capability.ToString());
                var attribute = field?.GetCustomAttribute<CapabilityAttribute>();

                if (attribute != null)
                    ids.Add(attribute.Id);
            }

            return ids.ToArray();
        }
    }
}