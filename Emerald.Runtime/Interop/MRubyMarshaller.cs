using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using ChibiRuby;
using ChibiRuby.Serializer;
using Emerald.Runtime.Extensions;

namespace Emerald.Runtime.Interop
{
    /// <summary>
    /// Bridges CLR values and <see cref="MRubyValue"/> through ChibiRuby's serializer. The resolver
    /// is built from the <see cref="MRubyFormatterAttribute"/> formatters discovered in the scanned
    /// assemblies, layered over <see cref="StandardResolver"/> for primitives and collections — so a
    /// type with a registered formatter uses it, primitives keep working for free, and a type that
    /// resolves to no formatter raises a Ruby error (the serializer throws
    /// <see cref="MRubySerializationException"/>, which we translate below).
    ///
    /// The serializer only exposes generic <c>Serialize&lt;T&gt;</c>/<c>Deserialize&lt;T&gt;</c>, so
    /// we close those generic methods at runtime and cache the result per type. Options and caches are
    /// instance state rebuilt per program (each reload), so formatters never leak across programs.
    /// </summary>
    internal sealed class MRubyMarshaller
    {
        private static readonly MethodInfo DeserializeOpen = typeof(MRubyValueSerializer).GetMethod(
            "Deserialize", BindingFlags.Public | BindingFlags.Static);
        private static readonly MethodInfo SerializeOpen = typeof(MRubyValueSerializer).GetMethod(
            "Serialize", BindingFlags.Public | BindingFlags.Static);

        private readonly MRubyValueSerializerOptions _options;
        private readonly Dictionary<Type, MethodInfo> _deserializeCache = new Dictionary<Type, MethodInfo>();
        private readonly Dictionary<Type, MethodInfo> _serializeCache = new Dictionary<Type, MethodInfo>();

        private MRubyMarshaller(MRubyValueSerializerOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Builds a marshaller whose resolver tries every <see cref="MRubyFormatterAttribute"/>
        /// formatter in <paramref name="assemblies"/> first, then falls back to
        /// <see cref="StandardResolver"/>.
        /// </summary>
        public static MRubyMarshaller FromAssemblies(params Assembly[] assemblies)
        {
            var formatters = DiscoverFormatters(assemblies);
            var resolver = CompositeResolver.Create(
                formatters, new IMRubyValueFormatterResolver[] { StandardResolver.Instance });

            return new MRubyMarshaller(new MRubyValueSerializerOptions { Resolver = resolver });
        }

        /// <summary>Converts an mruby value into a CLR instance of <paramref name="type"/>.</summary>
        public object Deserialize(MRubyState s, MRubyValue value, Type type)
        {
            var deserialize = CloseGeneric(_deserializeCache, DeserializeOpen, type);
            try
            {
                return deserialize.Invoke(null, new object[] { value, s, _options });
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                if (inner is MRubySerializationException)
                    MRubyError.Raise(s, "invalid argument for '" + type.Name + "': " + inner.Message);

                ExceptionDispatchInfo.Capture(inner).Throw();
                return null;
            }
        }

        /// <summary>Converts a CLR value of <paramref name="type"/> into an mruby value.</summary>
        public MRubyValue Serialize(MRubyState s, object value, Type type)
        {
            var serialize = CloseGeneric(_serializeCache, SerializeOpen, type);
            try
            {
                return (MRubyValue)serialize.Invoke(null, new object[] { value, s, _options });
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                if (inner is MRubySerializationException)
                    MRubyError.Raise(s, "cannot convert return value of type " + type.Name + ": " + inner.Message);

                ExceptionDispatchInfo.Capture(inner).Throw();
                return MRubyValue.Nil;
            }
        }

        private static MethodInfo CloseGeneric(Dictionary<Type, MethodInfo> cache, MethodInfo open, Type type)
        {
            if (cache.TryGetValue(type, out var closed)) return closed;

            closed = open.MakeGenericMethod(type);
            cache.Add(type, closed);

            return closed;
        }

        /// <summary>Instantiates every non-abstract [MRubyFormatter] across the given assemblies.</summary>
        private static IReadOnlyList<IMRubyValueFormatter> DiscoverFormatters(Assembly[] assemblies)
        {
            var formatters = new List<IMRubyValueFormatter>();

            foreach (var assembly in assemblies)
            foreach (var type in assembly.GetLoadableTypes())
            {
                if (type.IsAbstract) continue;
                if (type.GetCustomAttribute<MRubyFormatterAttribute>() == null) continue;

                formatters.Add((IMRubyValueFormatter)Activator.CreateInstance(type));
            }

            return formatters;
        }
    }
}
