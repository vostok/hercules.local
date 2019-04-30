using Vostok.Hercules.Local.Components.Bases;
using Vostok.Hercules.Local.Configuration;
using Vostok.Hercules.Local.Settings;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Components
{
    public class HerculesStreamManager : HerculesService
    {
        internal HerculesStreamManager(HerculesComponentSettings componentSettings, HerculesClusterSettings clusterSettings, ILog log)
            : base(componentSettings, log)
        {
            Properties["kafka.bootstrap.servers"] = clusterSettings.KafkaConnectionString;
            Properties.ConfigureZooKeeper(clusterSettings.ZooKeeperConnectionString);
        }
    }
}