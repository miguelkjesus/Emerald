using System;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Emerald.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandAttribute : PreserveAttribute
    {
        public CommandAttribute(string slug = null)
        {
            Slug = slug;
        }

        [CanBeNull] public readonly string Slug;
    }
}