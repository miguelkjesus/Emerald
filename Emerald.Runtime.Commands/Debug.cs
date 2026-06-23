using Emerald.Runtime;

namespace Emerald.Runtime.Commands
{
    public sealed class Debug : CommandController
    {
        [Command("debug_log")]
        private void Log(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }
        
        [Command("debug_screen_message")]
        private void ScreenMessage(string msg, float duration)
        {
            ScreenMessages.PostScreenMessage(msg, duration);
        }
    }
}