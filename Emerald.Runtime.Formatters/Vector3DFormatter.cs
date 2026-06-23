using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ChibiRuby;
using ChibiRuby.Serializer;

namespace Emerald.Runtime.Formatters
{
    [MRubyFormatter]
    public sealed class Vector3DFormatter : MRubyObjectFormatter<Vector3d>
    {
        protected override string ClassName => "Vector3";

        protected override IReadOnlyDictionary<string, MRubyValue> ToFields(Vector3d value, MRubyState mrb)
            => new Dictionary<string, MRubyValue>
            {
                ["x"] = value.x,
                ["y"] = value.y,
                ["z"] = value.z,
            };

        protected override Vector3d FromFields(MRubyState mrb, MRubyValue value)
        {
            var x = FieldAsFloat(mrb, value, "x");
            var y = FieldAsFloat(mrb, value, "y");
            var z = FieldAsFloat(mrb, value, "z");
            
            return new Vector3d(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FieldAsFloat(MRubyState mrb, MRubyValue value, string fieldName)
            => mrb.AsFloat(mrb.Send(value, mrb.Intern(fieldName)));
    }
}
