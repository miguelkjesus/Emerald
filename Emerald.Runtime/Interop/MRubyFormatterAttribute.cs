using System;
using UnityEngine.Scripting;

namespace Emerald.Runtime.Interop
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MRubyFormatterAttribute : PreserveAttribute
    {
    }
}
