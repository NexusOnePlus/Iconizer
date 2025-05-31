using System.Collections.Generic;
using System.IO;

namespace Iconizer.Infrastructure.Services
{
    public interface ICleaner
    {
        void Clean(IEnumerable<string> folders);
    }

    public class Cleaner : ICleaner
    {
        public void Clean(IEnumerable<string> folders)
        {
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