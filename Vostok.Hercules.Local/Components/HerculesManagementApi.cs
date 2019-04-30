using Vostok.Hercules.Local.Components.Bases;
using Vostok.Hercules.Local.Configuration;
using Vostok.Hercules.Local.Settings;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Components
{
    public class HerculesManagementApi : HerculesService
    {
        internal HerculesManagementApi(HerculesComponentSettings componentSettings, HerculesClusterSettings clusterSettings, string adminApiKey, ILog log)
            : base(componentSettings, log)
        {
            Properties.ConfigureZooKeeper(clusterSettings.ZooKeeperConnectionString);
            Properties.ConfigureKafkaCommons("kafka", clusterSettings.KafkaConnectionString);
            Properties["keys"] = adminApiKey;
        }
    }
}