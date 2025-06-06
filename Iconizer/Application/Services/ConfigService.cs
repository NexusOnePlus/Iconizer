using System.IO;
using System.Text.Json;
using Iconizer.Domain;
using Microsoft.Extensions.Logging;

namespace Iconizer.Application.Services
{
    public class ConfigService : IConfigService
    {
        private readonly JsonSerializerOptions _options =
            new JsonSerializerOptions { WriteIndented = true };

        private readonly ILogger<ConfigService> _logger;

        public ConfigService(ILogger<ConfigService> logger)
        {
            _logger = logger;
        }

        public ConfigData? Load(string path)
        {
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ConfigData>(json);
        }

        public ConfigDiff? LoadDiff(string path)
        {
            if (!File.Exists(path))
                return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ConfigDiff>(json);
        }



        public void Save(ConfigData config, string path)
        {
            _logger.LogInformation($"Saving config to {path}");
            var dir = Path.GetDirectoryName(path);
            if (dir == null)
                throw new ArgumentException("The provided path does not contain a valid directory.", nameof(path));

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config, _options);
            File.WriteAllText(path, json);
        }

        public void SaveDiff(ConfigDiff configDiff, string path)
        {
            _logger.LogInformation($"Saving config diff to {path}");
            var dir = Path.GetDirectoryName(path);
            if (dir == null)
                throw new ArgumentException("The provided path does not contain a valid directory.", nameof(path));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(configDiff, _options);
            File.WriteAllText(path, json);
        }
    }
}