using Emerald.Runtime;

namespace Emerald.Commands
{
    public sealed class Debug
    {
        [Command("debug_log", Capabilities.DebugLog)]
        public void Log(string msg)
        {
            ScreenMessages.PostScreenMessage(msg);
        }
    }
}