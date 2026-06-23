using System;
using ChibiRuby;
using ChibiRuby.Compiler;

namespace Emerald.Runtime
{
    /// <summary>
    /// One compiled program bound to one mruby VM for the lifetime of the instance. The constructor
    /// compiles the entrypoint and builds the program's fiber up front, throwing
    /// <see cref="ArgumentException"/> on a compile error rather than yielding a half-built host, so
    /// an instance is always in a runnable state. To reload, dispose the host and construct a new
    /// one — that discards the previous program and every bit of script-level state it accumulated.
    /// </summary>
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
            State = ScriptState.Running;
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
            if (State != ScriptState.Running) return;

            try { _mainFiber.Resume(ReadOnlySpan<MRubyValue>.Empty); }
            finally { State = _mainFiber.IsAlive ? ScriptState.Running : ScriptState.Ended; }
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
