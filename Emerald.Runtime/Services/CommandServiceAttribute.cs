using System;
using UnityEngine.Scripting;

namespace Emerald.Runtime.Services
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CommandServiceAttribute : PreserveAttribute
    {
    }
}
