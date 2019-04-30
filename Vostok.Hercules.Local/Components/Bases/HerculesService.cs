using System;
using Vostok.Commons.Helpers.Network;
using Vostok.Commons.Time;
using Vostok.Hercules.Local.Helpers;
using Vostok.Hercules.Local.Settings;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Components.Bases
{
    public class HerculesService : HerculesProcess
    {
        private readonly HerculesComponentSettings componentSettings;

        internal HerculesService(HerculesComponentSettings componentSettings, ILog log)
            : base(componentSettings, log)
        {
            this.componentSettings = componentSettings;
            InstanceId = componentSettings.InstanceId.Value;

            Properties["metrics.graphite.server.addr"] = "localhost";
            Properties["metrics.graphite.server.port"] = "2003";
            Properties["metrics.graphite.prefix"] = "hercules";
            Properties["metrics.period"] = int.MaxValue.ToString();

            Properties["context.instance.id"] = $"{InstanceId}";
            Properties["context.environment"] = "dev";
            Properties["context.zone"] = "default";
        }

        public int InstanceId { get; }
        public string Host => "localhost";
        public int Port { get; private set; }

        public override void Start()
        {
            Port = FreeTcpPortFinder.GetFreePort();
            Properties["http.server.host"] = Host;
            Properties["http.server.port"] = Port.ToString();

            base.Start();

            if (!new HerculesHttpServiceHealthChecker(Log, Host, Port).WaitStarted(3.Minutes()))
                throw new TimeoutException($"{componentSettings.GetDisplayName()} has not warmed up in 3 minutes.");
        }
    }
}