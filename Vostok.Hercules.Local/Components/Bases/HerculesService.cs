using System;
using System.Collections.Concurrent;
using Vostok.Commons.Helpers.Network;
using Vostok.Commons.Time;
using Vostok.Hercules.Local.Helpers;
using Vostok.Hercules.Local.Settings;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Components.Bases
{
    public class HerculesService : HerculesProcess
    {
        private static readonly ConcurrentDictionary<int, bool> PortSet = new ConcurrentDictionary<int, bool>();
        private readonly HerculesComponentSettings componentSettings;
        private readonly TimeSpan startTimeout = 120.Seconds();

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

            Properties["consumer.metric.reporters"] = "ru.kontur.vostok.hercules.kafka.util.metrics.GraphiteReporter";
        }

        public int InstanceId { get; }
        public string Host => "localhost";
        public int Port { get; private set; }

        public override void Start()
        {
            var success = false;
            for (var i = 0; i < 100; ++i)
            {
                Port = FreeTcpPortFinder.GetFreePort();

                if (PortSet.TryAdd(Port, true))
                {
                    success = true;
                    break;
                }
            }

            if (!success)
                throw new Exception("Unable to find free port!");

            Properties["http.server.host"] = Host;
            Properties["http.server.port"] = Port.ToString();

            Properties["application.host"] = Host;
            Properties["application.port"] = Port.ToString();

            base.Start();

            if (!new HerculesHttpServiceHealthChecker(Log, Host, Port).WaitStarted(startTimeout))
                throw new TimeoutException($"{componentSettings.GetDisplayName()} has not warmed up in {startTimeout} minutes.");
        }
    }
}