using System;
using UnityEngine.Scripting;

namespace Emerald.Runtime
{
    /// <summary>
    /// Marks a class as an mruby value formatter: an <c>IMRubyValueFormatter&lt;T&gt;</c>
    /// implementation discovered during <see cref="MRubyMarshaller.FromAssemblies"/> and composed into the
    /// marshaller's resolver (ahead of the built-in primitive/collection formatters). Derives from
    /// PreserveAttribute (like <see cref="CommandAttribute"/>) so it is kept under stripping.
    /// Requires a public parameterless constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MRubyFormatterAttribute : PreserveAttribute
    {
    }
}
