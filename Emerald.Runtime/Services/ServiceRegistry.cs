using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Emerald.Runtime.Extensions;

namespace Emerald.Runtime.Services
{
    public sealed class ServiceRegistry : IDisposable
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        public IEnumerable<object> Services => _services.Values;

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

        public T Resolve<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? (T)service : null;
        }

        public void Dispose()
        {
            foreach (var service in _services.Values)
                if (service is IDisposable disposable)
                    disposable.Dispose();

            _services.Clear();
        }
    }
}
