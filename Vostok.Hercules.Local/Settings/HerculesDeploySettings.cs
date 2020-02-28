namespace Vostok.Hercules.Local.Settings
{
    public class HerculesDeploySettings
    {
        public string BaseDirectory { get; set; }
        public int HerculesGateCount { get; set; } = 1;
        public int HerculesManagementApiCount { get; set; } = 1;
        public int HerculesStreamApiCount { get; set; } = 1;
    }
}