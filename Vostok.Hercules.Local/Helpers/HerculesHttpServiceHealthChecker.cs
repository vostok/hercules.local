using System;
using System.Diagnostics;
using System.Threading;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Helpers
{
    internal class HerculesHttpServiceHealthChecker
    {
        private readonly ILog log;
        private readonly string host;
        private readonly int port;

        public HerculesHttpServiceHealthChecker(ILog log, string host, int port)
        {
            this.log = log;
            this.host = host;
            this.port = port;
        }

        public bool WaitStarted(TimeSpan timeout)
        {
            log.Debug("Waiting for the service to start..");

            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (IsStarted())
                {
                    log.Debug($"Service has successfully started in {sw.Elapsed.TotalSeconds:0.##} second(s).");
                    return true;
                }

                Thread.Sleep(0.5.Seconds());
            }

            log.Warn($"Service hasn't started in {timeout}.");
            return false;
        }

        private bool IsStarted()
        {
            var response = new UniversalTransport(log)
                .SendAsync(Request.Get($"http://{host}:{port}/ping"), null, 5.Seconds(), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            return response.Code == ResponseCode.Ok;
        }
    }
}