using System;

namespace Emerald.Runtime.Capabilities
{
    [Flags]
    public enum Capabilities
    {
        None = 0,

        [Capability("vessel.telemetry.read")]
        ReadVesselTelemetry = 1 << 1,
    }
}