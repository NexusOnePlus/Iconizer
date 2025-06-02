using System.IO;
using Microsoft.Extensions.Logging;

namespace Iconizer.Infrastructure.Services
{
    public interface ICleaner
    {
        void Clean(IEnumerable<string> folders);
    }

    public class Cleaner : ICleaner
    {
        private readonly ILogger<Cleaner> _logger;
        public Cleaner(ILogger<Cleaner> logger)
        {
            _logger = logger;
        }
        public void Clean(IEnumerable<string>? folders)
        {
            if (folders == null)
            {
                _logger.LogWarning("No folders provided for cleaning.");
                return;
            }
            _logger.LogInformation("Starting cleaning process for folders.");
            foreach (var folder in folders)
            {
                string [] dataFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
                foreach (var file in dataFiles)
                {
                    if (Path.GetFileName(file).Contains("iconizer_") || Path.GetFileName(file).Contains("desktop.ini"))
                    {
                        File.Delete(file);
                    }
                }
            }
        }
    }
}