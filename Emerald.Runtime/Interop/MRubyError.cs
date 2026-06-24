using System.Text;
using ChibiRuby;

namespace Emerald.Runtime.Interop
{
    internal static class MRubyError
    {
        public static void Raise(MRubyState s, RClass exceptionClass, string message)
        {
            s.Raise(exceptionClass, Encoding.UTF8.GetBytes(message));
        }
        
        public static void Raise(MRubyState s, string message)
        {
            Raise(s, s.StandardErrorClass, message);
        }
    }
}
