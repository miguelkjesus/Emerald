using System;
using UnityEngine.Scripting;

namespace Emerald.Runtime
{
    /// <summary>
    /// Marks a class as a long-lived service: constructed once during <see cref="ServiceRegistry.FromAssemblies"/>,
    /// resolvable via the command context, and disposed with the host. Derives from PreserveAttribute
    /// (like <see cref="CommandAttribute"/>) so it is kept under stripping. Requires a public
    /// parameterless constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CommandServiceAttribute : PreserveAttribute
    {
    }
}
