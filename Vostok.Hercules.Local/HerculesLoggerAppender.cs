using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Local;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local
{
    internal static class HerculesLoggerAppender
    {
        public static void AppendLogger(string loggerPath, IEnumerable<string> componentPaths, ILog log)
        {
            var baseDirectory = Path.GetDirectoryName(loggerPath);
            var loggerName = Path.GetFileName(loggerPath);
            var extractor = new ShellRunner(new ShellRunnerSettings("jar")
                {
                    Arguments = $"-xf {loggerName} org",
                    WorkingDirectory = baseDirectory
                },
                log);
            log.Info("{loggerName}, {loggerPath}", loggerName, loggerPath);

            var updateTasks = componentPaths.Select(x =>
            {
                var filename = Path.GetFileName(x);
                var updater = new ShellRunner(new ShellRunnerSettings("jar")
                    {
                        Arguments = $"-uf {filename} org",
                        WorkingDirectory = baseDirectory
                    },
                    log);
                return updater.RunAsync(TimeSpan.FromSeconds(30), CancellationToken.None);
            }).ToArray();

            extractor.Run(TimeSpan.FromSeconds(30), CancellationToken.None);
            Task.WaitAll(updateTasks);
        }
    }
}