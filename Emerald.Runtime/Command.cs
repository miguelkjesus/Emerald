using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ChibiRuby;

namespace Emerald.Runtime
{
    public sealed class Command
    {
        internal Command(string slug, Func<CommandController> factory, MethodInfo method, MRubyMarshaller marshaller)
        {
            Slug = slug;
            _factory = factory;
            _method = method;
            _marshaller = marshaller;
            _parameters = method.GetParameters();

            // The Ruby keyword for each parameter is its snake_case name; precompute once so binding
            // is just a string compare instead of a per-call case conversion.
            _keywords = new string[_parameters.Length];
            for (var i = 0; i < _parameters.Length; i++)
                _keywords[i] = _parameters[i].Name.ToSnakeCase();
        }

        public readonly string Slug;

        private readonly Func<CommandController> _factory;
        private readonly MethodInfo _method;
        private readonly MRubyMarshaller _marshaller;
        private readonly ParameterInfo[] _parameters;
        private readonly string[] _keywords;

        public MRubyValue Invoke(MRubyState s, ServiceRegistry services, ReadOnlySpan<KeyValuePair<Symbol, MRubyValue>> arguments)
        {
            // Bind before constructing the controller so a bad keyword costs no allocation.
            var args = BindArguments(s, arguments);

            var controller = _factory();
            controller.State = s;
            controller.Services = services;
            controller.Slug = Slug;

            var result = InvokeMethod(s, controller, args);

            if (_method.ReturnType == typeof(void) || result == null)
                return MRubyValue.Nil;

            return _marshaller.Serialize(s, result, _method.ReturnType);
        }

        private object[] BindArguments(MRubyState s, ReadOnlySpan<KeyValuePair<Symbol, MRubyValue>> arguments)
        {
            var keys = new string[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
                keys[i] = s.NameOf(arguments[i].Key).ToString();

            var args = new object[_parameters.Length];
            var matched = 0;

            for (var p = 0; p < _parameters.Length; p++)
            {
                var index = IndexOf(keys, _keywords[p]);

                if (index >= 0)
                {
                    args[p] = Convert(s, arguments[index].Value, _parameters[p], _keywords[p]);
                    matched++;
                }
                else if (_parameters[p].HasDefaultValue)
                {
                    args[p] = _parameters[p].DefaultValue;
                }
                else
                {
                    MRubyError.Raise(s, "missing keyword: " + _keywords[p]);
                }
            }

            if (matched < arguments.Length)
                MRubyError.Raise(s, "unknown keyword(s) for command: " + Slug);

            return args;
        }

        private object Convert(MRubyState s, MRubyValue value, ParameterInfo parameter, string keyword)
        {
            try
            {
                return _marshaller.Deserialize(s, value, parameter.ParameterType);
            }
            catch (OverflowException)
            {
                MRubyError.Raise(s, "argument out of range: " + keyword);
                return null;
            }
        }

        private object InvokeMethod(MRubyState s, CommandController controller, object[] args)
        {
            try
            {
                return _method.Invoke(controller, args);
            }
            catch (TargetInvocationException ex)
            {
                MRubyError.Raise(s, (ex.InnerException ?? ex).Message);
                return null;
            }
        }

        private static int IndexOf(string[] keys, string keyword)
        {
            for (var i = 0; i < keys.Length; i++)
                if (string.Equals(keys[i], keyword, StringComparison.Ordinal))
                    return i;

            return -1;
        }
    }
}
