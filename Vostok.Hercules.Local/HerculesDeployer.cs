using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Vostok.Commons.Time;
using Vostok.Hercules.Local.Components;
using Vostok.Hercules.Local.Components.Bases;
using Vostok.Hercules.Local.Helpers;
using Vostok.Hercules.Local.Settings;
using Vostok.Kafka.Local;
using Vostok.Logging.Abstractions;
using Vostok.ZooKeeper.LocalEnsemble;

namespace Vostok.Hercules.Local
{
    internal class HerculesDeployer
    {
        private static readonly Dictionary<Type, string> ComponentNames = new Dictionary<Type, string>
        {
            [typeof(HerculesInit)] = "hercules-init",
            [typeof(HerculesGate)] = "hercules-gate",
            [typeof(HerculesManagementApi)] = "hercules-management-api",
            [typeof(HerculesStreamApi)] = "hercules-stream-api",
            [typeof(HerculesStreamManager)] = "hercules-stream-manager"
        };

        private readonly HerculesDeploySettings deploySettings;
        private readonly ILog log;
        private readonly string baseDirectory;
        private Dictionary<string, string> herculesBinariesPaths;

        public HerculesDeployer(HerculesDeploySettings deploySettings, ILog log)
        {
            this.deploySettings = deploySettings;
            this.log = log;

            baseDirectory = GetHerculesBaseDirectory(deploySettings);
        }

        public static void Cleanup(HerculesCluster cluster)
        {
            foreach (var service in cluster.HerculesServices)
                TryDeleteDirectory(service.BaseDirectory);
        }

        public HerculesCluster Deploy()
        {
            Directory.CreateDirectory(baseDirectory);

            herculesBinariesPaths = new HerculesDownloader(baseDirectory, log).GetLatestBinaries(ComponentNames.Values.ToArray());

            var cluster = new HerculesCluster();

            try
            {
                cluster.ZooKeeperEnsemble = ZooKeeperEnsemble.DeployNew(new ZooKeeperEnsembleSettings {BaseDirectory = baseDirectory}, log);
                cluster.KafkaInstance = KafkaInstance.DeployNew(new KafkaSettings {BaseDirectory = baseDirectory, ZooKeeperConnectionString = cluster.ZooKeeperEnsemble.ConnectionString}, log);

                DeployHercules(cluster);

                return cluster;
            }
            catch (Exception error)
            {
                log.Error(error, "Error in deploy. Will try to cleanup.");
                cluster.Dispose();
                throw;
            }
        }

        private static string GetHerculesBaseDirectory(HerculesDeploySettings settings)
        {
            return string.IsNullOrEmpty(settings.BaseDirectory) ? Directory.GetCurrentDirectory() : settings.BaseDirectory;
        }

        private static string GetComponentName<TComponent>()
        {
            return GetComponentName(typeof(TComponent));
        }

        private static string GetComponentName(Type type)
        {
            return ComponentNames[type];
        }

        private static void AddServices(HerculesCluster cluster, int count, Func<int, HerculesService> constructor)
        {
            cluster.HerculesServices.AddRange(CreateServices(count, constructor));
        }

        private static T[] CreateServices<T>(int count, Func<int, T> constructor)
        {
            return Enumerable.Range(0, count).Select(constructor).ToArray();
        }

        private static void TryDeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(path, true);
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(500);
                }
            }
        }

        private void DeployHercules(HerculesCluster cluster)
        {
            var clusterSettings = new HerculesClusterSettings
            {
                ZooKeeperConnectionString = cluster.ZooKeeperEnsemble.ConnectionString,
                KafkaConnectionString = cluster.KafkaInstance.ConnectionString
            };

            InitializeHercules(clusterSettings);

            ApiKeyInitializer.AddApiKey(ApiKeys.ApiKey, clusterSettings.ZooKeeperConnectionString, log);

            AddServices(
                cluster,
                deploySettings.HerculesGateCount,
                id => new HerculesGate(GetHerculesComponentSettings<HerculesGate>(id), clusterSettings, log));

            AddServices(
                cluster,
                deploySettings.HerculesManagementApiCount,
                id => new HerculesManagementApi(GetHerculesComponentSettings<HerculesManagementApi>(id), clusterSettings, ApiKeys.AdminApiKey, log));

            AddServices(
                cluster,
                deploySettings.HerculesStreamApiCount,
                id => new HerculesStreamApi(GetHerculesComponentSettings<HerculesStreamApi>(id), clusterSettings, log));

            AddServices(
                cluster,
                deploySettings.HerculesStreamManagerCount,
                id => new HerculesStreamManager(GetHerculesComponentSettings<HerculesStreamManager>(id), clusterSettings, log));

            DeployServices(cluster.HerculesServices);
        }

        private void InitializeHercules(HerculesClusterSettings clusterSettings)
        {
            var initSettings = GetHerculesComponentSettings<HerculesInit>();
            try
            {
                DeployHerculesComponent(GetComponentName<HerculesInit>(), initSettings.BaseDirectory, initSettings.JarFileName);
                var init = new HerculesInit(initSettings, clusterSettings, log);
                init.Run(5.Seconds());
            }
            finally
            {
                TryDeleteDirectory(initSettings.BaseDirectory);
            }
        }

        private void DeployServices(IEnumerable<HerculesService> services)
        {
            foreach (var herculesService in services)
            {
                var componentName = GetComponentName(herculesService.GetType());
                DeployHerculesComponent(componentName, herculesService.BaseDirectory, herculesService.JarFileName);
            }
        }

        private void DeployHerculesComponent(string componentName, string componentBaseDirectory, string jarFileName)
        {
            if (Directory.Exists(componentBaseDirectory))
                TryDeleteDirectory(componentBaseDirectory);
            Directory.CreateDirectory(componentBaseDirectory);

            File.Copy(herculesBinariesPaths[componentName], Path.Combine(componentBaseDirectory, jarFileName));
        }

        private string GetServiceBaseDirectory(string componentName, int? instanceId = null)
        {
            var instanceIdSuffix = instanceId != null ? $"-{instanceId}" : "";
            return Path.Combine(baseDirectory, $"{componentName}{instanceIdSuffix}");
        }

        private HerculesComponentSettings GetHerculesComponentSettings<TComponent>(int? instanceId = null)
        {
            var componentName = GetComponentName<TComponent>();
            return new HerculesComponentSettings
            {
                BaseDirectory = GetServiceBaseDirectory(componentName, instanceId),
                JarFileName = Path.GetFileName(herculesBinariesPaths[componentName]),
                InstanceId = instanceId
            };
        }
    }
}