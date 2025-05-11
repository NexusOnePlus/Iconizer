using System.IO;
using System.Text.Json;
using Iconizer.Domain;

namespace Iconizer.Application.Services
{
    public class ConfigService : IConfigService
    {
        private readonly JsonSerializerOptions _options = 
            new JsonSerializerOptions { WriteIndented = true };

        public ConfigData? Load(string path)
        {
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ConfigData>(json);
        }

        public void Save(ConfigData config, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config, _options);
            File.WriteAllText(path, json);
        }
    }
}