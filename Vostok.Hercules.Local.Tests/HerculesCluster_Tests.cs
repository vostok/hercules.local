using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Hercules.Local.Settings;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.Hercules.Local.Tests
{
    [TestFixture]
    internal class HerculesCluster_Tests
    {
        private readonly ILog log = new SynchronousConsoleLog();

        [Test]
        public void Should_deploy_and_start_gate()
        {
            var settings = new HerculesDeploySettings
            {
                HerculesGateCount = 1,
                HerculesManagementApiCount = 0,
                HerculesStreamApiCount = 0,
                HerculesStreamManagerCount = 0
            };
            Check(settings);
        }

        [Test]
        public void Should_deploy_and_start_management_api()
        {
            var settings = new HerculesDeploySettings
            {
                HerculesGateCount = 0,
                HerculesManagementApiCount = 1,
                HerculesStreamApiCount = 0,
                HerculesStreamManagerCount = 0
            };
            Check(settings);
        }

        [Test]
        public void Should_deploy_and_start_management_stream_api()
        {
            var settings = new HerculesDeploySettings
            {
                HerculesGateCount = 0,
                HerculesManagementApiCount = 0,
                HerculesStreamApiCount = 1,
                HerculesStreamManagerCount = 0
            };
            Check(settings);
        }

        [Test]
        public void Should_deploy_and_start_management_stream_manager()
        {
            var settings = new HerculesDeploySettings
            {
                HerculesGateCount = 0,
                HerculesManagementApiCount = 0,
                HerculesStreamApiCount = 0,
                HerculesStreamManagerCount = 1
            };
            Check(settings);
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Should_deploy_and_start_cluster_by_default(int size)
        {
            var settings = new HerculesDeploySettings
            {
                HerculesGateCount = size,
                HerculesManagementApiCount = size,
                HerculesStreamApiCount = size,
                HerculesStreamManagerCount = size
            };
            try
            {
                Check(settings);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(-1);
            }
            
        }

        [Test]
        public void Should_deploy_cluster_and_start_only_external_dependencies_if_specified()
        {
            using (var cluster = HerculesCluster.DeployNew(log, false))
            {
                cluster.ZooKeeperEnsemble.IsRunning.Should().BeTrue();
                cluster.KafkaInstance.IsRunning.Should().BeTrue();
                cluster.HerculesServices.All(service => service.IsRunning).Should().BeFalse();
            }
        }

        private void Check(HerculesDeploySettings settings)
        {
            using (var cluster = HerculesCluster.DeployNew(settings, log))
            {
                cluster.ZooKeeperEnsemble.IsRunning.Should().BeTrue();
                cluster.KafkaInstance.IsRunning.Should().BeTrue();
                cluster.HerculesServices.All(service => service.IsRunning).Should().BeTrue();
            }
        }
    }
}