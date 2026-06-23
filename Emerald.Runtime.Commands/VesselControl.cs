using Emerald.Runtime.Services;
using KSP.UI.Screens;

namespace Emerald.Runtime.Commands
{
    public sealed class VesselControl : CommandController
    {
        private FlyByWireService FlyByWire => Service<FlyByWireService>();

        [Command("start_fly_by_wire")]
        private void StartFlyByWire() => FlyByWire.Start();

        [Command("stop_fly_by_wire")]
        private void StopFlyByWire() => FlyByWire.Stop();

        [Command("reset_fly_by_wire_values")]
        private void ResetFlyByWireValues() => FlyByWire.ResetValues();
        
        [Command("set_throttle")]
        private void SetThrottle(float throttle) => FlyByWire.Throttle = throttle;

        [Command("stage")]
        private void Stage()
        {
            if (StageManager.CanSeparate) StageManager.ActivateNextStage();
        }
    }
}
