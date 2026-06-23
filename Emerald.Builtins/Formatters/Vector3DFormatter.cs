using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ChibiRuby;
using Emerald.Runtime;

namespace Emerald.Builtins.Formatters
{
    [MRubyFormatter]
    public sealed class Vector3DFormatter : MRubyObjectFormatter<Vector3d>
    {
        protected override string ClassName => "Vector3";

        protected override IReadOnlyDictionary<string, MRubyValue> ToConstructorKargs(Vector3d value, MRubyState mrb)
            => new Dictionary<string, MRubyValue>
            {
                ["x"] = value.x,
                ["y"] = value.y,
                ["z"] = value.z,
            };

        protected override Vector3d FromMRubyValue(MRubyState mrb, MRubyValue value)
        {
            var x = mrb.AsFloat(Send(mrb, value, "x"));
            var y = mrb.AsFloat(Send(mrb, value, "y"));
            var z = mrb.AsFloat(Send(mrb, value, "z"));
            
            return new Vector3d(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static MRubyValue Send(MRubyState mrb, MRubyValue value, string fieldName)
            => mrb.Send(value, mrb.Intern(fieldName));
    }
}
