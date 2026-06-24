using System;
using ChibiRuby;
using ChibiRuby.Compiler;
using Emerald.Runtime.Commands;
using Emerald.Runtime.Services;

namespace Emerald.Runtime.Execution
{
    public sealed class ScriptHost : IDisposable
    {
        public ScriptHost(string entrypointPath, CommandRegistry commands, ServiceRegistry services)
        {
            EntrypointPath = entrypointPath;
            Commands = commands;
            Services = services;

            _mrb = MRubyState.Create();
            _compiler = MRubyCompiler.Create(_mrb);
            DefineBindings();

            var compilation = _compiler.CompileFile(EntrypointPath);
            if (compilation.HasError)
            {
                Dispose();
                throw new ArgumentException("Could not compile program", EntrypointPath);
            }

            _mainFiber = _mrb.CreateFiber(_mrb.CreateProc(compilation.ToIrep()));
            State = ScriptState.NotStarted;
        }

        public readonly string EntrypointPath;
        public readonly CommandRegistry Commands;
        public readonly ServiceRegistry Services;
        public ScriptState State { get; private set; }

        private readonly MRubyState _mrb;
        private readonly MRubyCompiler _compiler;
        private readonly RFiber _mainFiber;

        public void Dispose()
        {
            Services.Dispose();
            _compiler.Dispose();
            _mrb.Dispose();
        }

        public void Tick()
        {
            if (State == ScriptState.NotStarted) State = ScriptState.Running;

            try
            {
                _mainFiber.Resume(ReadOnlySpan<MRubyValue>.Empty);
            }
            finally
            {
                State = _mainFiber.IsAlive ? ScriptState.Running : ScriptState.Ended;
            }
        }

        private void DefineBindings()
        {
            _mrb.DefineModule(_mrb.Intern("Emerald"), DefineCommands);
        }

        private void DefineCommands(ClassDefineOptions module)
        {
            module.DefineModule(_mrb.Intern("Commands"), commands =>
            {
                foreach (var command in Commands.Commands)
                {
                    module.DefineClassMethod(_mrb.Intern(command.Slug),
                        (state, _) => command.Invoke(state, Services, state.GetKeywordArguments()));
                }
            });
        }
    }
}
