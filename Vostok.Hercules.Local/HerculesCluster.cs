using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Commons.Local.Helpers;
using Vostok.Hercules.Local.Components;
using Vostok.Hercules.Local.Components.Bases;
using Vostok.Hercules.Local.Settings;
using Vostok.Kafka.Local;
using Vostok.Logging.Abstractions;
using Vostok.ZooKeeper.LocalEnsemble;

namespace Vostok.Hercules.Local
{
    public class HerculesCluster : IDisposable
    {
        internal HerculesCluster()
        {
        }

        public static HerculesCluster DeployNew(ILog log, bool started = true)
        {
            return DeployNew(new HerculesDeploySettings(), log, started);
        }

        public static HerculesCluster DeployNew(string baseDirectory, ILog log, bool started = true)
        {
            return DeployNew(new HerculesDeploySettings {BaseDirectory = baseDirectory}, log, started);
        }

        public static HerculesCluster DeployNew(HerculesDeploySettings deploySettings, ILog log, bool started = true)
        {
            HerculesCluster cluster = null;

            Retrier.RetryOnException(() =>
            {
                cluster = new HerculesDeployer(deploySettings, log).Deploy();

                if (started)
                    cluster.Start();
            }, 
            3,
            "Unable to start Hercules.Local", 
            () =>
            {
                log.Warn("Retrying Hercules.Local deployment...");
                cluster?.Dispose();
            });

            return cluster;
        }

        public ZooKeeperEnsemble ZooKeeperEnsemble { get; internal set; }
        public KafkaInstance KafkaInstance { get; internal set; }

        public IReadOnlyList<HerculesGate> HerculesGateInstances => GetServices<HerculesGate>();
        public IReadOnlyList<HerculesManagementApi> HerculesManagementApiInstances => GetServices<HerculesManagementApi>();
        public IReadOnlyList<HerculesStreamApi> HerculesStreamApiInstances => GetServices<HerculesStreamApi>();
        public IReadOnlyList<HerculesStreamManager> HerculesStreamManagerInstances => GetServices<HerculesStreamManager>();

        public IClusterProvider HerculesGateTopology => GetTopology(HerculesGateInstances);
        public IClusterProvider HerculesManagementApiTopology => GetTopology(HerculesManagementApiInstances);
        public IClusterProvider HerculesStreamApiTopology => GetTopology(HerculesStreamApiInstances);
        public IClusterProvider HerculesStreamManagerTopology => GetTopology(HerculesStreamManagerInstances);

        public string ApiKey => ApiKeys.ApiKey;

        public void Start()
        {
            Parallel.ForEach(HerculesServices, service => service.Start());
        }

        public void Stop()
        {
            foreach (var service in HerculesServices)
                service.Stop();
        }

        public void Dispose()
        {
            Stop();
            HerculesDeployer.Cleanup(this);
            KafkaInstance?.Dispose();
            ZooKeeperEnsemble?.Dispose();
        }

        internal List<HerculesService> HerculesServices { get; } = new List<HerculesService>();

        private static IClusterProvider GetTopology(IEnumerable<HerculesService> services)
        {
            return new FixedClusterProvider(services.Select(service => new UriBuilder("http", service.Host, service.Port).Uri).ToArray());
        }

        private IReadOnlyList<T> GetServices<T>()
            where T : HerculesService
        {
            return HerculesServices.Where(service => service is T).Cast<T>().ToList();
        }
    }
}