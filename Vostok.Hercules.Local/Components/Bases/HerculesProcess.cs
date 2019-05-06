using System.Collections.Generic;
using System.IO;
using Vostok.Commons.Local;
using Vostok.Hercules.Local.Helpers;
using Vostok.Hercules.Local.Settings;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local.Components.Bases
{
    public class HerculesProcess : ProcessWrapper
    {
        protected readonly Dictionary<string, string> Properties = new Dictionary<string, string>();

        internal HerculesProcess(HerculesComponentSettings componentSettings, ILog log)
            : base(log, componentSettings.GetDisplayName(), true)
        {
            BaseDirectory = componentSettings.BaseDirectory;
            JarFileName = componentSettings.JarFileName;
        }

        public string BaseDirectory { get; }
        public string JarFileName { get; }

        public override void Start()
        {
            Configure();
            base.Start();
        }

        protected override string FileName => "java";
        protected override string Arguments =>
            $"-jar {JarFileName} application.properties=file://{PropertiesFileName}";
        protected override string WorkingDirectory => BaseDirectory;

        private string PropertiesFileName => Path.Combine(BaseDirectory, "application.properties");

        private void Configure()
        {
            var properties = new JavaProperties(Properties);
            properties.Save(PropertiesFileName);
            Log.Info("Hercules process configured with properties:\n{properties}", properties);    
        }
    }
}