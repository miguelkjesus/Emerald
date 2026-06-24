using Emerald.Builtins.Services;
using Emerald.Runtime.Commands;
using KSP.UI.Screens;

namespace Emerald.Builtins.Commands
{
    public sealed class VesselControl : CommandController
    {
        private FlyByWireService FlyByWire => Service<FlyByWireService>();

        [Command]
        public void StartFlyByWire() => FlyByWire.Start();

        [Command]
        public void StopFlyByWire() => FlyByWire.Stop();

        [Command]
        public void ResetFlyByWireValues() => FlyByWire.ResetValues();
        
        [Command]
        public void SetThrottle(float throttle) => FlyByWire.Throttle = throttle;

        [Command]
        public void Stage()
        {
            if (StageManager.CanSeparate) StageManager.ActivateNextStage();
        }
    }
}
