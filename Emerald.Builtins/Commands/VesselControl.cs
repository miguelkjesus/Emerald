using Emerald.Builtins.Services;
using Emerald.Runtime;
using KSP.UI.Screens;

namespace Emerald.Builtins.Commands
{
    public sealed class VesselControl : CommandController
    {
        private FlyByWireService FlyByWire => Service<FlyByWireService>();

        [Command]
        private void StartFlyByWire() => FlyByWire.Start();

        [Command]
        private void StopFlyByWire() => FlyByWire.Stop();

        [Command]
        private void ResetFlyByWireValues() => FlyByWire.ResetValues();
        
        [Command]
        private void SetThrottle(float throttle) => FlyByWire.Throttle = throttle;

        [Command]
        private void Stage()
        {
            if (StageManager.CanSeparate) StageManager.ActivateNextStage();
        }
    }
}
