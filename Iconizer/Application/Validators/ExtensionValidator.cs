using System.Collections.Generic;

namespace Iconizer.Application.Validators
{
    public interface IExtensionValidator
    {
        bool IsValidExtension(string ext);
    }

    public class ExtensionValidator : IExtensionValidator
    {
        private static readonly HashSet<string> Allowed = new()
        {
            ".cpp", ".rs", ".js", ".jsx", "package.json", ".py",
            "config.toml", "Cargo.toml", "bunfig.toml", "deno.json",
            ".ts", ".tsx", ".yml", ".json", ".lock", ".png" , ".java",".go",".php"
        };

        public bool IsValidExtension(string ext) => Allowed.Contains(ext);
    }
}