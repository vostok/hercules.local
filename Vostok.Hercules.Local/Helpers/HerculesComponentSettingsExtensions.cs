using Vostok.Hercules.Local.Settings;

namespace Vostok.Hercules.Local.Helpers
{
    internal static class HerculesComponentSettingsExtensions
    {
        public static string GetDisplayName(this HerculesComponentSettings componentSettings)
        {
            var result = componentSettings.JarFileName;
            if (componentSettings.InstanceId != null)
                result += $"-{componentSettings.InstanceId}";
            return result;
        }
    }
}