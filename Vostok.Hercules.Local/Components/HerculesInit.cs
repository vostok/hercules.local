using System;
using Vostok.Hercules.Local.Components.Bases;
using Vostok.Hercules.Local.Configuration;
using Vostok.Hercules.Local.Settings;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Components
{
    public class HerculesInit
    {
        private readonly HerculesInitProcess process;

        internal HerculesInit(HerculesComponentSettings componentSettings, HerculesClusterSettings clusterSettings, ILog log)
        {
            process = new HerculesInitProcess(componentSettings, clusterSettings, log);
        }

        public void Run(TimeSpan timeout) =>
            process.Run(timeout);

        private class HerculesInitProcess : HerculesProcess
        {
            public HerculesInitProcess(HerculesComponentSettings componentSettings, HerculesClusterSettings clusterSettings, ILog log)
                : base(componentSettings, log)
            {
                Properties.ConfigureZooKeeper(clusterSettings.ZooKeeperConnectionString, "zk");
                Properties["kafka.bootstrap.servers"] = clusterSettings.KafkaConnectionString;
                Properties["kafka.replication.factor"] = "3";
            }
        }
    }
}