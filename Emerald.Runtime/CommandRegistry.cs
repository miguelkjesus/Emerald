using System;
using System.Collections.Generic;
using System.Reflection;

namespace Emerald.Runtime
{
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();
        
        public IEnumerable<Command> Commands => _commands.Values;

        public Command GetCommand(string id)
        {
            var found = _commands.TryGetValue(id, out var command);

            return !found ? throw new ArgumentException($"Unknown command: {id}") : command;
        }

        public bool RemoveCommand(string id)
        {
            return _commands.Remove(id);
        }

        public void AddFromService(object service)
        {
            var methods = service.GetType().GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<CommandAttribute>();
                if (attribute == null) continue;
                
                AddCommand(new Command(attribute.Id, attribute.RequiredCapabilities, service, method));
            }
        }

        private void AddCommand(Command command)
        {
            _commands.Add(command.Id, command);
        }
    }
}