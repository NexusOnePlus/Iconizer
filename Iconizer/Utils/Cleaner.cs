using System.IO;

namespace Iconizer.Utils;

public class Cleaner
{
    public static void Clean(string[] folders)
    {
        foreach (var folder in folders)
        {
            string [] dataFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var file in dataFiles)
            {
                switch (Path.GetFileName(file))
                {
                    case "desktop.ini":
                    case "iconizer.ico":
                        File.Delete(file);
                        break;
                }
                
            }
            
        }
    }
    
}