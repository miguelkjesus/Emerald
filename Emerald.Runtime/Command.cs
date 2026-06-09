using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using ChibiRuby;
using ChibiRuby.Serializer;

namespace Emerald.Runtime
{
    public sealed class Command
    {
        public Command(string id, Capabilities requiredCapabilities, object target, MethodInfo method)
        {
            Id = id;
            _requiredCapabilities = requiredCapabilities;
            _target = target;
            _method = method;
        }

        public readonly string Id;
        private readonly Capabilities _requiredCapabilities;
        private readonly object _target;
        private readonly MethodInfo _method;

        public Capabilities GetMissingCapabilities(Capabilities capabilities)
        {
            return  _requiredCapabilities & ~capabilities;
        }

        public bool CanInvokeWith(Capabilities capabilities)
        {
            return (capabilities & _requiredCapabilities) == _requiredCapabilities;
        }

        public MRubyValue Invoke(MRubyState s, ReadOnlySpan<KeyValuePair<Symbol, MRubyValue>> arguments)
        {
            var parameters = _method.GetParameters();
            var args = new object[parameters.Length];
            var matched = 0;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var bound = false;

                foreach (var arg in arguments)
                {
                    // Symbol is a record struct, so arg.Key.ToString() yields "Symbol { Value = N }",
                    // not the keyword name. Resolve the actual name from the state's symbol table.
                    var key = s.NameOf(arg.Key).ToString();

                    if (!string.Equals(SnakeToCamel(key), parameter.Name, StringComparison.Ordinal))
                        continue;

                    try
                    {
                        args[i] = ConvertArgument(s, arg.Value, parameter.ParameterType);
                    }
                    catch (OverflowException)
                    {
                        Raise(s, "argument out of range: " + key);
                    }

                    bound = true;
                    matched++;
                    break;
                }

                if (!bound)
                {
                    if (parameter.HasDefaultValue) args[i] = parameter.DefaultValue;
                    else Raise(s, "missing keyword: " + ToSnakeCase(parameter.Name));
                }
            }

            if (matched < arguments.Length)
                Raise(s, "unknown keyword(s) for command: " + Id);

            object result;
            try
            {
                result = _method.Invoke(_target, args);
            }
            catch (TargetInvocationException ex)
            {
                Raise(s, (ex.InnerException ?? ex).Message);
                return MRubyValue.Nil;
            }

            if (_method.ReturnType == typeof(void) || result == null)
                return MRubyValue.Nil;

            return ToMRubyValue(s, result, _method.ReturnType);
        }
        
        private static readonly MethodInfo DeserializeOpen = typeof(MRubyValueSerializer).GetMethod(
            "Deserialize", 
            BindingFlags.Public | BindingFlags.Static);
        private static readonly MethodInfo SerializeOpen = typeof(MRubyValueSerializer).GetMethod(
            "Serialize", 
            BindingFlags.Public | BindingFlags.Static);

        private static readonly Dictionary<Type, MethodInfo> DeserializeCache = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> SerializeCache = new Dictionary<Type, MethodInfo>();

        private static MethodInfo CloseGeneric(Dictionary<Type, MethodInfo> cache, MethodInfo open, Type t)
        {
            if (cache.TryGetValue(t, out var closed)) return closed;
            
            closed = open.MakeGenericMethod(t);
            cache.Add(t, closed);
            
            return closed;
        }

        private static object ConvertArgument(MRubyState s, MRubyValue value, Type type)
        {
            var deserialize = CloseGeneric(DeserializeCache, DeserializeOpen, type);
            try
            {
                return deserialize.Invoke(null, new object[] { value, s, null });
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                if (inner is OverflowException)
                    ExceptionDispatchInfo.Capture(inner).Throw();
                
                if (inner is MRubySerializationException)
                    Raise(s, "invalid argument for '" + type.Name + "': " + inner.Message);
                
                ExceptionDispatchInfo.Capture(inner).Throw();
                return null;
            }
        }

        private static MRubyValue ToMRubyValue(MRubyState s, object result, Type returnType)
        {
            var serialize = CloseGeneric(SerializeCache, SerializeOpen, returnType);
            try
            {
                return (MRubyValue)serialize.Invoke(null, new [] { result, s, null });
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                if (inner is MRubySerializationException)
                    Raise(s, "cannot convert return value of type " + returnType.Name + ": " + inner.Message);
                
                ExceptionDispatchInfo.Capture(inner).Throw();
                return MRubyValue.Nil;
            }
        }

        private static void Raise(MRubyState s, string message)
        {
            s.Raise(s.StandardErrorClass, Encoding.UTF8.GetBytes(message));
        }

        private static string SnakeToCamel(string name)
        {
            if (name.IndexOf('_') < 0) return name;
            var sb = new StringBuilder(name.Length);
            var upperNext = false;
            foreach (var c in name)
            {
                if (c == '_') { upperNext = true; continue; }
                sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }
            return sb.ToString();
        }

        private static string ToSnakeCase(string name)
        {
            var sb = new StringBuilder(name.Length + 4);
            foreach (var c in name)
            {
                if (char.IsUpper(c)) { sb.Append('_'); sb.Append(char.ToLowerInvariant(c)); }
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
