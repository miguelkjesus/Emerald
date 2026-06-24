using Emerald.Runtime.Commands;

namespace Emerald.Builtins.Commands
{
    public sealed class VesselTelemetry : CommandController
    {
        private static Vessel ActiveVessel => FlightGlobals.ActiveVessel;
        
        private static Vessel GetVessel(int id) => FlightGlobals.Vessels[id];

        [Command]
        public int ActiveVesselIndex() => FlightGlobals.Vessels.IndexOf(ActiveVessel);
        
        [Command]
        public double GetAltitude(int vesselIndex) => GetVessel(vesselIndex).altitude;
        
        [Command]
        public Vector3d SurfaceVelocity(int vesselIndex) => GetVessel(vesselIndex).srf_velocity;
        
        [Command]
        public double VerticalSpeed(int vesselIndex) => GetVessel(vesselIndex).verticalSpeed;
    }
}