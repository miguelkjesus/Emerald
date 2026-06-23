using System;
using System.Collections.Generic;
using ChibiRuby;
using ChibiRuby.Serializer;
using JetBrains.Annotations;

namespace Emerald.Runtime
{
    /// <summary>
    /// Base class for formatters that marshal a CLR type <typeparamref name="T"/> to and from a Ruby
    /// object — e.g. a <c>Vector3d</c> as <c>Vector3.new(x:, y:, z:)</c>. A subclass declares the Ruby
    /// class name and how to map <typeparamref name="T"/> to a field → value map and back; this base
    /// constructs instances with <c>new(**fields)</c> and reads fields back through their reader methods
    /// (<c>obj.x</c>). The Ruby class itself is expected to already exist (defined by the prelude);
    /// serialization raises if it does not. Mark subclasses with <see cref="MRubyFormatterAttribute"/>.
    /// </summary>
    public abstract class MRubyObjectFormatter<T> : IMRubyValueFormatter<T>
    {
        // Cached per program: the marshaller (and therefore each formatter instance) is rebuilt on reload.
        private RClass _class;

        /// <summary>Ruby class this formatter materialises (e.g. "Vector3").</summary>
        protected abstract string ClassName { get; }

        /// <summary>Maps <paramref name="value"/> to a field → mruby value map, passed as keyword arguments.</summary>
        protected abstract IReadOnlyDictionary<string, MRubyValue> ToFields(T value, MRubyState mrb);

        /// <summary>Rebuilds a <typeparamref name="T"/>, reading fields off the object via <paramref name="fields"/>.</summary>
        protected abstract T FromFields(MRubyState mrb, MRubyValue value);

        public MRubyValue Serialize(T value, MRubyState mrb, MRubyValueSerializerOptions options)
        {
            var fields = ToFields(value, mrb);

            // `ClassName.new(field0: ..., field1: ...)` — real keyword arguments via Send's kargs overload.
            var kwargs = new KeyValuePair<Symbol, MRubyValue>[fields.Count];
            var i = 0;
            foreach (var field in fields)
                kwargs[i++] = new KeyValuePair<Symbol, MRubyValue>(mrb.Intern(field.Key), field.Value);

            return mrb.Send(ClassOf(mrb), mrb.Intern("new"), kargs: kwargs);
        }

        public T Deserialize(MRubyValue value, MRubyState mrb, MRubyValueSerializerOptions options)
        {
            MRubySerializationException.ThrowIfTypeMismatch(value, MRubyVType.Object, ClassName, mrb);
            return FromFields(mrb, value);
        }

        // The Ruby class is defined elsewhere (the prelude); look it up once and fail clearly if absent.
        private RClass ClassOf(MRubyState mrb)
        {
            if (_class != null) return _class;
            if (mrb.TryGetConst(mrb.Intern(ClassName), out var existing))
                return _class = existing.As<RClass>();

            throw new MRubySerializationException("Ruby class '" + ClassName + "' is not defined");
        }
    }
}
