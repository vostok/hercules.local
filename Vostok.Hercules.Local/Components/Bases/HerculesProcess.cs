using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Vostok.Commons.Local;
using Vostok.Hercules.Local.Helpers;
using Vostok.Hercules.Local.Settings;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Components.Bases
{
    public class HerculesProcess
    {
        protected readonly Dictionary<string, string> Properties = new Dictionary<string, string>();
        private readonly ShellRunner shellRunner;
        protected readonly ILog Log;

        internal HerculesProcess(HerculesComponentSettings componentSettings, ILog log)
        {
            BaseDirectory = componentSettings.BaseDirectory;
            JarFileName = componentSettings.JarFileName;

            Log = log = log.ForContext(componentSettings.JarFileName);

            shellRunner = new ShellRunner(
                new ShellRunnerSettings("java")
                {
                    Arguments = $"-jar {JarFileName} application.properties=file://{PropertiesFileName} " + componentSettings.Arguments,
                    WorkingDirectory = BaseDirectory
                },
                log);
        }

        public string BaseDirectory { get; }
        public string JarFileName { get; }

        public virtual void Start()
        {
            Configure();
            shellRunner.Start();
        }

        public void Stop()
        {
            shellRunner.Stop();
        }

        public void Run(TimeSpan timeout)
        {
            Configure();
            shellRunner.Run(timeout, CancellationToken.None);
        }

        public bool IsRunning => shellRunner.IsRunning;
        
        private string PropertiesFileName => Path.Combine(BaseDirectory, "application.properties");

        private void Configure()
        {
            var properties = new JavaProperties(Properties);
            properties.Save(PropertiesFileName);
            Log.Info("Hercules process configured with properties:\n{properties}", properties);
        }
    }
}