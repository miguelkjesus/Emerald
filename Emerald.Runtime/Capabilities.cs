using System;

namespace Emerald.Runtime
{
    [Flags]
    public enum Capabilities
    {
        None = 0,

        [Capability("debug.log", true)]
        DebugLog = 1 << 0,

        [Capability("vessel.telemetry.read", true)]
        ReadVesselTelemetry = 1 << 1,
    }
}