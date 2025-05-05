using System.IO;
using System.Text.Json;
using Path = System.IO.Path;

namespace Iconizer.Utils;

public class ConfigLoader
{
    public class Entry
    {
        public string Pattern { get; set; } = "";
        public string IconPath { get; set; } = "";
    }

    public class Raw
    {
        public List<string> Files { get; set; } = new();
        public List<string> Icons { get; set; } = new();
    }

    public List<Entry> Entries { get; private set; } = new();

    private readonly string _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Iconizer", "config.json");

    public void Load()
    {
        if (!File.Exists(_configPath))
        {
            Console.WriteLine($"Config file not found: {_configPath}");
            // Should create a file
            Entries = new List<Entry>();
            return;
        }

        var json = File.ReadAllText(_configPath);
        var rawJson = JsonSerializer.Deserialize<Raw>(json)
                      ?? new Raw();

        int count = Math.Min(rawJson.Files.Count, rawJson.Icons.Count);

        var dictList = new List<Entry>(capacity: count);
        for (int i = 0; i < count; i++)
        {
            dictList.Add(new Entry
            {
                Pattern = rawJson.Files[i],
                IconPath = rawJson.Icons[i],
            });
        }
        
        Entries = dictList;
        
    }
}