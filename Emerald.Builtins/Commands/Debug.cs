using Emerald.Runtime.Commands;

namespace Emerald.Builtins.Commands
{
    public sealed class Debug : CommandController
    {
        [Command]
        public void Log(string msg) => UnityEngine.Debug.Log(msg);
        
        [Command]
        private void ScreenMessage(string msg, float duration) => ScreenMessages.PostScreenMessage(msg, duration);
    }
}