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
            ".c", ".h", ".cpp", ".cc", ".cxx", ".hpp", ".hh",
            ".cs", ".fs", ".vb",
            ".java", ".class", ".jar",
            ".py", ".pyw", ".ipynb",
            ".js", ".mjs", ".cjs", ".jsx",
            ".ts", ".tsx",
            ".go",
            ".php", ".phar",
            ".rb",
            ".rs",
            ".swift",
            ".kt", ".kts",
            ".scala",
            ".m", ".mm",
            ".pl", ".pm",
            ".sh", ".bash", ".zsh",
            ".ps1", ".psm1",
            ".dart",
            ".r",
            ".jl",
            // Web files
            ".html", ".htm",
            ".css", ".scss", ".sass", ".less",
            ".json", ".yaml", ".yml",
            ".xml",
            ".md", ".markdown",
            ".toml",
            ".ini",
            ".csv", ".tsv",
            ".txt",
            // Image formats
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".svg", ".ico", ".webp", ".tiff",
            // Audio formats
            ".mp3", ".wav", ".flac", ".aac",
            // Video formats
            ".mp4", ".mov", ".avi", ".mkv", ".webm",
            // Archive formats
            ".zip", ".tar", ".gz", ".bz2", ".rar", ".7z",
            // Document formats
            ".pdf", ".docx", ".xlsx", ".pptx",
            // Config and lock files
            "package.json",
            "package-lock.json",
            "yarn.lock",
            "pnpm-lock.yaml",
            "Cargo.toml",
            "Cargo.lock",
            "deno.json",
            "deno.jsonc",
            "bunfig.toml",
            "pyproject.toml",
            "Pipfile",
            "Pipfile.lock",
            // Scripts and makefiles
            ".bat", "Makefile",
            // Others
            ".cfg", ".conf", ".properties", ".gradle"
        };

        public bool IsValidExtension(string ext) => Allowed.Contains(ext);
    }
}
