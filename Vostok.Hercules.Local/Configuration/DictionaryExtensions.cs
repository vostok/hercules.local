using System.Collections.Generic;

namespace Vostok.Hercules.Local.Configuration
{
    internal static class DictionaryExtensions
    {
        public static void ConfigureZooKeeper(this Dictionary<string, string> properties, string zkConnectionString, string settingsPrefix = "curator")
        {
            properties[$"{settingsPrefix}.connectString"] = zkConnectionString;
            properties[$"{settingsPrefix}.connectionTimeout"] = "10000";
            properties[$"{settingsPrefix}.sessionTimeout"] = "30000";
            properties[$"{settingsPrefix}.retryPolicy.baseSleepTime"] = "1000";
            properties[$"{settingsPrefix}.retryPolicy.maxRetries"] = "3";
            properties[$"{settingsPrefix}.retryPolicy.maxSleepTime"] = "3000";
        }

        public static void ConfigureKafkaCommons(this Dictionary<string, string> properties, string settingsPrefix, string kafkaConnectionString)
        {
            properties[$"{settingsPrefix}.bootstrap.servers"] = kafkaConnectionString;
            properties[$"{settingsPrefix}.acks"] = "all";
            properties[$"{settingsPrefix}.batch.size"] = "65536";
            properties[$"{settingsPrefix}.linger.ms"] = "1";
            properties[$"{settingsPrefix}.buffer.memory"] = "335544320";
        }

        public static void ConfigureKafkaProducer(this Dictionary<string, string> properties, string kafkaConnectionString)
        {
            properties.ConfigureKafkaCommons("producer", kafkaConnectionString);
            properties["producer.retries"] = "4";
            properties["producer.retry.backoff.ms"] = "250";
        }

        public static void ConfigureKafkaConsumer(this Dictionary<string, string> properties, string kafkaConnectionString)
        {
            properties.ConfigureKafkaCommons("stream.api.pool.consumer", kafkaConnectionString);
            properties["stream.api.pool.consumer.retries"] = "0";
            properties["stream.api.pool.consumer.poll.timeout"] = "250";
            properties["stream.api.pool.size"] = "30";
        }
    }
}