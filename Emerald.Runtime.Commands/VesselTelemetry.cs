namespace Emerald.Runtime.Commands
{
    public sealed class VesselTelemetry : CommandController
    {
        private static Vessel ActiveVessel => FlightGlobals.ActiveVessel;
        
        private static Vessel GetVessel(int id) => FlightGlobals.Vessels[id];

        [Command("active_vessel_index")]
        private int ActiveVesselIndex() => FlightGlobals.Vessels.IndexOf(ActiveVessel);
        
        [Command("vessel_altitude")]
        private double GetAltitude(int vesselIndex) => GetVessel(vesselIndex).altitude;
        
        [Command("vessel_surface_velocity")]
        private Vector3d SurfaceVelocity(int vesselIndex) => GetVessel(vesselIndex).srf_velocity;
        
        [Command("vessel_vertical_speed")]
        private double VerticalSpeed(int vesselIndex) => GetVessel(vesselIndex).verticalSpeed;
    }
}