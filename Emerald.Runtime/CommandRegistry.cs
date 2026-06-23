using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Emerald.Runtime
{
    /// <summary>
    /// Discovers and holds the [Command] methods on <see cref="CommandController"/> subclasses across
    /// the scanned assemblies, keyed by command slug.
    /// </summary>
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

        public IEnumerable<Command> Commands => _commands.Values;

        /// <summary>
        /// Discovers every command across <paramref name="assemblies"/>, building the marshaller that
        /// binds each command's arguments and return value (it also discovers the formatters there).
        /// </summary>
        public static CommandRegistry FromAssemblies(params Assembly[] assemblies)
        {
            var marshaller = MRubyMarshaller.FromAssemblies(assemblies);
            var registry = new CommandRegistry();

            foreach (var type in assemblies.SelectMany(a => a.GetLoadableTypes()))
            {
                if (type.IsAbstract || !typeof(CommandController).IsAssignableFrom(type)) continue;
                registry.RegisterController(type, marshaller);
            }

            return registry;
        }

        public Command GetCommand(string slug)
        {
            return _commands.TryGetValue(slug, out var command)
                ? command
                : throw new ArgumentException($"Unknown command: {slug}");
        }

        public bool TryGetCommand(string slug, out Command command)
        {
            return _commands.TryGetValue(slug, out command);
        }

        private void RegisterController(Type type, MRubyMarshaller marshaller)
        {
            var factory = CompileFactory(type); // compiled once per controller type

            foreach (var method in type.GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attribute = method.GetCustomAttribute<CommandAttribute>();
                if (attribute == null) continue;

                _commands.Add(attribute.Slug, new Command(attribute.Slug, factory, method, marshaller));
            }
        }

        // Expression-compiled `() => (CommandController)new T()` — reflection-free on the per-call path.
        // KSP runs Mono with JIT, so Expression.Compile is fine; if ever a concern, fall back to
        // `() => (CommandController)Activator.CreateInstance(type)`.
        private static Func<CommandController> CompileFactory(Type type)
        {
            var construct = Expression.New(type);
            return Expression.Lambda<Func<CommandController>>(
                Expression.Convert(construct, typeof(CommandController))).Compile();
        }
    }
}
