using System.Text;
using System.Windows;
using System.IO;
using Iconizer.Presentation.View.UserControls;
using System.Text.Json;
using Path = System.IO.Path;
using System.Runtime.InteropServices;
using Iconizer.Utils;
using Iconizer.Domain;

namespace Iconizer.Infrastructure;

public class FileIconService
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(
        int wEventId, int uFlags, string dwItem1, IntPtr dwItem2);



    public async Task ApplyRemoveIcon(string folderPath, List<ConfigLoader.Entry> patterns)
    {
        Console.WriteLine(folderPath);
        foreach (var pattern in patterns)
        {
            Console.WriteLine(pattern.IconPath, " - ", pattern.Pattern);
        }

        if (patterns.Count == 0)
        {
            Console.WriteLine("No patterns found");
        }

        if (!Directory.Exists(folderPath))
            return;

        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
        bool matches = false;
        string iconToUse = "";

        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);
            foreach (var entry in patterns)
            {
                if (filename.Contains(entry.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    matches = true;
                    iconToUse = entry.IconPath;
                    break;
                }
            }

            if (matches) break;
        }

        string desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        string icoNewPath = Path.Combine(folderPath, "iconizer.ico");

        if (matches)
        {
            if (File.Exists(icoNewPath))
            {
                File.SetAttributes(icoNewPath, File.GetAttributes(icoNewPath) & ~FileAttributes.System);
                File.Delete(icoNewPath);
                SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
                await Task.Delay(2000);
            }

            File.Copy(iconToUse, icoNewPath);

            var iniContent = new StringBuilder();
            iniContent.AppendLine("[.ShellClassInfo]");
            iniContent.AppendLine($"IconResource=iconizer.ico,0");
            if (File.Exists(desktopIniPath))
            {
                File.SetAttributes(desktopIniPath, File.GetAttributes(desktopIniPath) & ~FileAttributes.System);
                File.Delete(desktopIniPath);
                SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
                await Task.Delay(2000);
            }

            Thread.Sleep(100);
            File.WriteAllText(desktopIniPath, iniContent.ToString(), Encoding.Unicode);

            File.SetAttributes(icoNewPath, FileAttributes.Hidden | FileAttributes.System);
            File.SetAttributes(desktopIniPath, FileAttributes.Hidden | FileAttributes.System);
            File.SetAttributes(folderPath, File.GetAttributes(folderPath) | FileAttributes.System);
            SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
            DirectoryInfo di = new DirectoryInfo(folderPath);
            di.Refresh();
            await Task.Delay(2000);
            File.SetAttributes(folderPath, File.GetAttributes(folderPath) | FileAttributes.ReadOnly);
            File.SetAttributes(folderPath, File.GetAttributes(folderPath) & ~FileAttributes.System);
            SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
        }
        else
        {
            RemoveIconConfig(folderPath);
        }
        
    }


    public void RemoveIconConfig(string folderPath)
    {
        string ini = Path.Combine(folderPath, "desktop.ini");
        string ico = Path.Combine(folderPath, "iconizer.ico");

        try
        {
            if (File.Exists(ini))
                File.Delete(ini);
            if (File.Exists(ico))
                File.Delete(ico);

            SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}

// public List<IconItem>? GetIcons(string folderPath)
// {
//     // Read icons from folder
//     return null;
// }
//
// public void ClearIcons(string folderPath)
// {
//     // Delete or clean icons
// }