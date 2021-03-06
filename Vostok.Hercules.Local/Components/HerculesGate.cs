using Vostok.Hercules.Local.Components.Bases;
using Vostok.Hercules.Local.Configuration;
using Vostok.Hercules.Local.Settings;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Components
{
    public class HerculesGate : HerculesService
    {
        internal HerculesGate(HerculesComponentSettings componentSettings, HerculesClusterSettings clusterSettings, ILog log)
            : base(componentSettings, log)
        {
            Properties.ConfigureZooKeeper(clusterSettings.ZooKeeperConnectionString);
            Properties.ConfigureKafkaProducer(clusterSettings.KafkaConnectionString);
        }
    }
}