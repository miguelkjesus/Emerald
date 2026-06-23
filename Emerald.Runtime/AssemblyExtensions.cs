using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Emerald.Runtime
{
    internal static class AssemblyExtensions
    {
        /// <summary>The assembly's types, falling back to the subset that loaded if some types can't.</summary>
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
