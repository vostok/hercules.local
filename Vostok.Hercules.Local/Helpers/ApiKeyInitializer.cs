using System;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Transport;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;
using Vostok.ZooKeeper.Client;
using Vostok.ZooKeeper.Client.Abstractions.Model;
using Vostok.ZooKeeper.Client.Abstractions.Model.Request;

namespace Vostok.Hercules.Local.Helpers
{
    internal static class ApiKeyInitializer
    {
        public static void AddApiKey(string adminApiKey, string apiKey, IClusterProvider managementApiTopology, ILog log)
        {
            var client = new ClusterClient(
                log,
                configuration =>
                {
                    configuration.ClusterProvider = managementApiTopology;
                    configuration.Transport = new UniversalTransport(log);
                });

            var task = client.SendAsync(
                Request.Post("/rules/set")
                    .WithHeader("apiKey", adminApiKey)
                    .WithAdditionalQueryParameter("key", apiKey)
                    .WithAdditionalQueryParameter("pattern", "*")
                    .WithAdditionalQueryParameter("rights", "rwm"),
                5.Seconds()
            );

            var result = task.GetAwaiter().GetResult();

            if (result.Status != ClusterResultStatus.Success || result.Response.Code != ResponseCode.Ok)
            {
                throw new Exception($"Failed to add api key '{apiKey}'. Server responded with {result.Response.Code}: '{result.Response.Content}'.");
            }
        }

        public static void AddApiKey(string apiKey, string zooKeeperConnectionString, ILog log)
        {
            using (var client = new ZooKeeperClient(new ZooKeeperClientSettings(zooKeeperConnectionString), log))
            {
                try
                {
                    foreach (var accessRight in new[] {"read", "write", "manage"})
                    {
                        var path = $"/hercules/auth/rules/{apiKey}.*.{accessRight}";
                        client.CreateAsync(new CreateRequest(path, CreateMode.Persistent))
                            .GetAwaiter()
                            .GetResult()
                            .EnsureSuccess();
                    }
                }
                catch (Exception error)
                {
                    throw new Exception($"Failed to add api key '{apiKey}'.", error);
                }
            }
        }
    }
}