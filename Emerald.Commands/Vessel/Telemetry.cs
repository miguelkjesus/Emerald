using Emerald.Runtime;

namespace Emerald.Commands.Vessel
{
    public class Telemetry
    {
        [Command("vessel_altitude", Capabilities.ReadVesselTelemetry)]
        public double GetAltitude()
        {
            return FlightGlobals.ActiveVessel.altitude;
        }

        [Command("vessel_surface_speed", Capabilities.ReadVesselTelemetry)]
        public double GetSurfaceSpeed()
        {
            return FlightGlobals.ActiveVessel.srf_velocity.magnitude;
        }
    }
}