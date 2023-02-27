
namespace HydroColor.Models
{
    public enum HydroColorImageTag
    {
        GrayCard,
        Water,
        Sky
    }
    public class HydroColorImageType
    {
        public HydroColorImageTag HCImageTag { get; set; }
        public double TargetOffNadirAngle { get; set; }
        public double TargetAzimuthAngleFromSun { get; set; }
        public string DisplayName { get; set; }
    }
}
