namespace Iconizer.Domain;

public  class ConfigData
{
    public List<string> Files { get; set; } = new();
    public List<string> Icons { get; set; } = new();
}

public class ConfigDiff
{
    public List<string> Targets { get; set; } = new();
    public List<string> Files { get; set; } = new();
    public List<string> Icons { get; set; } = new();
}