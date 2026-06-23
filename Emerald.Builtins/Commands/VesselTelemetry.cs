using Emerald.Runtime;

namespace Emerald.Builtins.Commands
{
    public sealed class VesselTelemetry : CommandController
    {
        private static Vessel ActiveVessel => FlightGlobals.ActiveVessel;
        
        private static Vessel GetVessel(int id) => FlightGlobals.Vessels[id];

        [Command]
        private int ActiveVesselIndex() => FlightGlobals.Vessels.IndexOf(ActiveVessel);
        
        [Command]
        private double GetAltitude(int vesselIndex) => GetVessel(vesselIndex).altitude;
        
        [Command]
        private Vector3d SurfaceVelocity(int vesselIndex) => GetVessel(vesselIndex).srf_velocity;
        
        [Command]
        private double VerticalSpeed(int vesselIndex) => GetVessel(vesselIndex).verticalSpeed;
    }
}