using System;
using Emerald.Runtime;

namespace Emerald.Builtins.Services
{
    /// <summary>
    /// Long-lived owner of the active vessel's fly-by-wire throttle override. Constructed once when
    /// the registry scans this assembly and disposed when the program reloads, which unsubscribes the
    /// callback. Transient <see cref="VesselControl"/> command controllers talk to it via the context.
    /// </summary>
    [CommandService]
    public sealed class FlyByWireService : IDisposable
    {
        private static Vessel ActiveVessel => FlightGlobals.ActiveVessel;

        public float? Throttle;

        public void Start() => ActiveVessel.OnFlyByWire += OnFlyByWire;
        
        public void Stop() => ActiveVessel.OnFlyByWire -= OnFlyByWire;
        
        public void Dispose() => Stop();

        public void ResetValues()
        {
            Throttle = null;
        }

        private void OnFlyByWire(FlightCtrlState s)
        {
            if (Throttle != null) s.mainThrottle = Throttle.Value;
        }
        
        
    }
}
