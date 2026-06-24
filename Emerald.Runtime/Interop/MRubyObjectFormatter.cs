using System;
using System.Collections.Generic;
using ChibiRuby;
using ChibiRuby.Serializer;
using JetBrains.Annotations;

namespace Emerald.Runtime.Interop
{
    public abstract class MRubyObjectFormatter<T> : IMRubyValueFormatter<T>
    {
        // Cached per program: the marshaller (and therefore each formatter instance) is rebuilt on reload.
        private RClass _class;

        /// <summary>The Ruby class that this formatter constructs during serialisation.</summary>
        protected abstract string ClassName { get; }

        /// <summary>Maps <paramref name="value"/> to a field -> mruby value map, passed as keyword arguments.</summary>
        protected abstract IReadOnlyDictionary<string, MRubyValue> ToConstructorKwargs(T value, MRubyState mrb);

        /// <summary>Rebuilds a <typeparamref name="T"/> from a ruby value.</summary>
        protected abstract T FromMRubyValue(MRubyState mrb, MRubyValue value);

        public MRubyValue Serialize(T value, MRubyState mrb, MRubyValueSerializerOptions options)
        {
            var fields = ToConstructorKwargs(value, mrb);

            var kwargs = new KeyValuePair<Symbol, MRubyValue>[fields.Count];
            var i = 0;
            foreach (var field in fields)
                kwargs[i++] = new KeyValuePair<Symbol, MRubyValue>(mrb.Intern(field.Key), field.Value);

            return mrb.Send(ClassOf(mrb), mrb.Intern("new"), kargs: kwargs);
        }

        public T Deserialize(MRubyValue value, MRubyState mrb, MRubyValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfTypeMismatch(value, MRubyVType.Object, ClassName, mrb);
            return FromMRubyValue(mrb, value);
        }

        private RClass ClassOf(MRubyState mrb)
        {
            if (_class != null) return _class;
            if (mrb.TryGetConst(mrb.Intern(ClassName), out var existing))
                return _class = existing.As<RClass>();

            // Class should be defined in the prelude.
            throw new MRubySerializationException("Ruby class '" + ClassName + "' is not defined");
        }
    }
}
