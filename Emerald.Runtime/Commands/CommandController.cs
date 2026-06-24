using System;
using ChibiRuby;
using Emerald.Runtime.Interop;
using Emerald.Runtime.Services;

namespace Emerald.Runtime.Commands
{
    public abstract class CommandController
    {
        /// <summary>The mruby VM the current command is running in.</summary>
        public MRubyState State { get; internal set; }

        /// <summary>The slug the current command was invoked as.</summary>
        public string Slug { get; internal set; }

        internal ServiceRegistry Services { get; set; }

        /// <summary>Resolves a long-lived service registered with the host. Raises if missing.</summary>
        protected T Service<T>() where T : class
        {
            return Services.Resolve<T>()
                   ?? throw new InvalidOperationException("Service not registered: " + typeof(T).Name);
        }

        /// <summary>Raises a Ruby StandardError. Does not return (mruby longjmp-style raise).</summary>
        protected void Raise(string message)
        {
            MRubyError.Raise(State, message);
        }
    }
}
