using System.Threading;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport;
using Vostok.Commons.Local;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Helpers
{
    internal class HerculesHttpServiceHealthChecker : ServiceHealthChecker
    {
        private readonly string host;
        private readonly int port;

        public HerculesHttpServiceHealthChecker(ILog log, string host, int port)
            : base(log)
        {
            this.host = host;
            this.port = port;
        }

        protected override bool IsStarted()
        {
            var response = new UniversalTransport(Log)
                .SendAsync(Request.Get($"http://{host}:{port}/ping"), null, 5.Seconds(), CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            return response.Code == ResponseCode.Ok;
        }
    }
}