using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Emerald.Runtime.Extensions;

namespace Emerald.Runtime.Services
{
    /// <summary>
    /// Discovers and holds the long-lived [CommandService] instances from the scanned assemblies and
    /// resolves them for command controllers. Disposed with the program, which disposes each service.
    /// </summary>
    public sealed class ServiceRegistry : IDisposable
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        public IEnumerable<object> Services => _services.Values;

        /// <summary>Discovers and constructs every [CommandService] across the given assemblies.</summary>
        public static ServiceRegistry FromAssemblies(params Assembly[] assemblies)
        {
            var registry = new ServiceRegistry();

            foreach (var type in assemblies.SelectMany(a => a.GetLoadableTypes()))
            {
                if (type.IsAbstract) continue;
                if (type.GetCustomAttribute<CommandServiceAttribute>() == null) continue;
                registry._services.Add(type, Activator.CreateInstance(type));
            }

            return registry;
        }

        /// <summary>The registered service of type <typeparamref name="T"/>, or null if there is none.</summary>
        public T Resolve<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? (T)service : null;
        }

        public void Dispose()
        {
            foreach (var service in _services.Values)
                (service as IDisposable)?.Dispose();

            _services.Clear();
        }
    }
}
