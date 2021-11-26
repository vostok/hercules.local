using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Hercules.Local
{
    internal class HerculesDownloader
    {
        // NOTE(tsup): Console logger implementation to add to Hercules jar files.
        private const string Slf4jLoggerUrl = "https://repo1.maven.org/maven2/org/slf4j/slf4j-simple/1.7.9/slf4j-simple-1.7.9.jar";
        // Note(kungurtsev): all releases can be found at https://api.github.com/repos/vostok/hercules/releases.
        private const string GithubReleasesUrl = "https://api.github.com/repos/vostok/hercules/releases/latest";
        private static readonly Regex AssetNameRegex = new Regex(@"(?<name>[a-z-]+)-(?<version>\d[\w\.-]*)\.jar");

        private readonly string baseDirectory;
        private readonly ILog log;
        private readonly UniversalTransport transport;

        public HerculesDownloader(string baseDirectory, ILog log)
        {
            this.baseDirectory = baseDirectory;
            this.log = log;

            transport = new UniversalTransport(new UniversalTransportSettings {AllowAutoRedirect = true}, log);
            Directory.CreateDirectory(CacheDirectoryPath);
        }

        public string GetLogger()
        {
            try
            {
                var loggerPath = GetLoggerPath();
                DownloadAndStore(Slf4jLoggerUrl, loggerPath, GetLoggerName()).GetAwaiter().GetResult();
                return loggerPath;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to download slf4j logger for Hercules", e);
            }
        }

        public Dictionary<string, string> GetLatestBinaries(string[] componentNames)
        {
            var assets = GetLatestReleaseAssets()
                .Where(asset => componentNames.Contains(GetComponentName(asset)))
                .ToArray();

            var downloads = assets
                .Where(asset => !File.Exists(GetAssetPath(asset)))
                .Select(asset => DownloadAndStore(asset.BrowserDownloadUrl, GetAssetPath(asset), asset.Name))
                .ToArray();

            Task.WaitAll(downloads);

            return assets.ToDictionary(GetComponentName, GetAssetPath);
        }

        private async Task DownloadAndStore(string url, string path, string name)
        {
            log.Info($"Begin downloading {name}..");

            var response = await SendRequestAsync(Request.Get(url), 5.Minutes()).ConfigureAwait(false);
            if (response.Code != ResponseCode.Ok)
                throw new Exception($"Request to {url} failed with code = {response.Code}.");

            if (!response.HasContent)
                throw new Exception($"Request to {url} failed: response is empty.");

            File.WriteAllBytes(path, response.Content.Buffer);

            log.Info($"Downloading {name} completed, stored at {path}.");
        }

        private string CacheDirectoryPath => Path.Combine(baseDirectory, ".hercules-cache");
        private string ReleaseCacheFileName => Path.Combine(CacheDirectoryPath, "release.json");

        private GithubAsset[] GetLatestReleaseAssets()
        {
            try
            {
                var request = Request.Get(GithubReleasesUrl);

                var cachedRelease = LoadReleaseFromCache();
                if (cachedRelease != null)
                    request = request.WithHeader("If-None-Match", $"W/{cachedRelease.ETag}");

                var response = SendRequestAsync(request, 5.Seconds()).GetAwaiter().GetResult();

                if (cachedRelease != null && response.Code == ResponseCode.NotModified)
                    return cachedRelease.Release.Assets;

                if (response.Code != ResponseCode.Ok)
                    throw new Exception($"Request to {GithubReleasesUrl} failed with code = {response.Code}.");

                var release = JsonConvert.DeserializeObject<GithubRelease>(response.Content.ToString());
                SaveReleaseToCache(new CachedRelease {ETag = response.Headers["ETag"], Release = release});
                return release.Assets;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to get hercules release info.", e);
            }
        }

        private CachedRelease LoadReleaseFromCache()
        {
            if (!File.Exists(ReleaseCacheFileName))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<CachedRelease>(File.ReadAllText(ReleaseCacheFileName));
            }
            catch (Exception error)
            {
                log.Warn(error, $"Failed to deserialize cached hercules release data from {ReleaseCacheFileName}.");
                return null;
            }
        }

        private void SaveReleaseToCache(CachedRelease cachedRelease)
        {
            File.WriteAllText(ReleaseCacheFileName, JsonConvert.SerializeObject(cachedRelease));
        }

        private string GetComponentName(GithubAsset asset)
        {
            return AssetNameRegex.Match(asset.Name).Groups["name"].Value;
        }

        private string GetAssetPath(GithubAsset asset)
        {
            return Path.Combine(CacheDirectoryPath, asset.Name);
        }

        private string GetLoggerPath() => Path.Combine(CacheDirectoryPath, GetLoggerName());

        private static string GetLoggerName() => Slf4jLoggerUrl.Split('/').Last();

        private Task<Response> SendRequestAsync(Request request, TimeSpan timeout)
        {
            return transport.SendAsync(request.WithHeader("User-Agent", "dotnet"), null, timeout, CancellationToken.None);
        }

        private class CachedRelease
        {
            public string ETag;
            public GithubRelease Release;
        }

        private class GithubRelease
        {
            [JsonProperty("assets")]
            public GithubAsset[] Assets;
        }

        private class GithubAsset
        {
            [JsonProperty("name")]
            public string Name;

            [JsonProperty("browser_download_url")]
            public string BrowserDownloadUrl;
        }
    }
}