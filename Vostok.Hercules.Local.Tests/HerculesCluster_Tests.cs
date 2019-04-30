using System.Linq;
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

        [TestCase(1)]
        [TestCase(3)]
        public void Should_deploy_and_start_cluster_by_default(int size)
        {
            using (var cluster = HerculesCluster.DeployNew(new HerculesDeploySettings {HerculesGateCount = size, HerculesManagementApiCount = size, HerculesStreamApiCount = size, HerculesStreamManagerCount = size}, log))
            {
                cluster.ZooKeeperEnsemble.IsRunning.Should().BeTrue();
                cluster.KafkaInstance.IsRunning.Should().BeTrue();
                cluster.HerculesServices.All(service => service.IsRunning).Should().BeTrue();
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
    }
}