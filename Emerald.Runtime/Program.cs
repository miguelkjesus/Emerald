using System;
using System.Text;
using ChibiRuby;
using ChibiRuby.Compiler;

namespace Emerald.Runtime
{
    public class Program : IDisposable
    {
        public Program(string entrypointPath, Capabilities capabilities)
        {
            EntrypointPath = entrypointPath;
            Capabilities = capabilities;

            _mrb = MRubyState.Create();
            _compiler = MRubyCompiler.Create(_mrb);
            
            DefineBindings();
            UpdateIrep();
        }
        
        public readonly string EntrypointPath;
        public readonly Capabilities Capabilities;
        public readonly CommandRegistry CommandRegistry = new CommandRegistry();
        
        private readonly MRubyState _mrb;
        private readonly MRubyCompiler _compiler;
        private Irep _irep;

        public void Dispose()
        {
            _compiler.Dispose();
            _mrb.Dispose();
        }

        public void Execute()
        {
            _mrb.Execute(_irep);
        }

        private void UpdateIrep()
        {
            var compilation = _compiler.CompileFile(EntrypointPath);

            if (compilation.HasError)
                throw new ArgumentException("Could not compile program", EntrypointPath);

            _irep = compilation.ToIrep();
        }

        private void DefineBindings()
        {
            // __call__(command_id, **kwargs)
            _mrb.DefinePrivateMethod(_mrb.ObjectClass, _mrb.Intern("__call__"), (s, _) =>
            {
                s.EnsureArgumentCount(1, 1);
                var idSymbol = s.GetArgumentAsSymbolAt(0);
                var kwargs = s.GetKeywordArguments();

                var id = s.NameOf(idSymbol).ToString();

                var command = CommandRegistry.GetCommand(id);
                RaiseIfCannotInvoke(s, command);

                return command.Invoke(s, kwargs);
            });
        }
        
        private void RaiseIfCannotInvoke(MRubyState s, Command command)
        {
            if (!command.CanInvokeWith(Capabilities))
            {
                var missingIds = Capability.GetIds(command.GetMissingCapabilities(Capabilities));
                var message = Encoding.UTF8.GetBytes(
                    $"Missing capabilities: {string.Join(", ", missingIds)}");
                s.Raise(s.StandardErrorClass, message);
            }
        }
    }
}