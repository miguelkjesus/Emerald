using System;
using UnityEngine.Scripting;

namespace Emerald.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandAttribute : PreserveAttribute
    {
        public CommandAttribute(string slug)
        {
            Slug = slug;
        }

        public readonly string Slug;
    }
}