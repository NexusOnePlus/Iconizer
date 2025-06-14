using Iconizer.Application.Services;
using Iconizer.Domain;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Iconizer.Infrastructure.Services
{
    public interface IFileIconService
    {
        void AssignIconsToFolders(ConfigData config);
        void ApplyOneFolder(string folder, ConfigData config);
        IEnumerable<string> GetDesktopFolders();
    }

    public class FileIconService : IFileIconService
    {
        private readonly ILogger<FileIconService> _logger;
        private readonly IConfigService _configService;
        public FileIconService(ILogger<FileIconService> logger, IConfigService configDiff)
        {
            _logger = logger;
            _configService = configDiff;
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint SHGetSetFolderCustomSettings(ref SHFOLDERCUSTOMSETTINGS pfcs, string pszPath, uint dwReadWrite);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFOLDERCUSTOMSETTINGS
        {
            public uint dwSize;
            public uint dwMask;
            public IntPtr pvid;
            public string pszWebViewTemplate;
            public uint cchWebViewTemplate;
            public string pszWebViewTemplateVersion;
            public string pszInfoTip;
            public uint cchInfoTip;
            public IntPtr pclsid;
            public uint dwFlags;
            public string pszIconFile;
            public uint cchIconFile;
            public int iIconIndex;
            public string pszLogo;
            public uint cchLogo;
        }

        private const uint FCSM_ICONFILE = 0x10;
        private const uint FCS_FORCEWRITE = 0x00000002;

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(
           int wEventId, int uFlags, string dwItem1, IntPtr dwItem2);


        public IEnumerable<string> GetDesktopFolders()
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _logger.LogDebug("Getting desktop folders: {DesktopPath}", desktop);
            return Directory.Exists(desktop)
                ? Directory.GetDirectories(desktop)
                : Array.Empty<string>();
        }

        public void AssignIconsToFolders(ConfigData config)
        {


            ConfigDiff? diffs = _configService.LoadDiff(ConfigPaths.ConfigDiffs);
            if (diffs == null)
            {
                ConfigDiff newone = new ConfigDiff();

                _logger.LogInformation("Assigning icons to desktop folders...");
                foreach (var folder in GetDesktopFolders())
                {
                    // Obtiene todos los archivos del folder
                    var files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
                    var matched = false;
                    string iconPath = null!;

                    // Busca el primer patrón que encaje
                    if (config != null)
                    {
                        for (var i = 0; i < config.Files.Count; i++)
                        {
                            _logger.LogDebug("Processing folder: {Folder}", folder);
                            var pattern = config.Files[i];
                            if (files.Select(f => Path.GetFileName(f)).Any(name => name.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                            {
                                matched = true;
                                iconPath = config.Icons[i];
                                _logger.LogDebug("Pattern '{Pattern}' was found in {Folder}. it will use icon : {IconPath}", pattern, folder, iconPath);
                            }
                            if (matched) break;
                        }

                        if (matched && File.Exists(iconPath))
                        {
                            _logger.LogInformation("Applying icon {IconPath} to folder {Folder}", iconPath, folder);
                            OnlyIcon(folder, iconPath);
                            newone.Targets.Add(folder);
                            newone.Icons.Add(iconPath);
                            newone.Files.Add(config.Files[config.Icons.IndexOf(iconPath)]);
                        }
                        else
                        {
                            _logger.LogInformation("No matching pattern found for folder {Folder}. Removing icon if exists.", folder);
                            // Si no coincide ningún patrón, elimina cualquier ícono anterior
                            RemoveIconConfig(folder);
                        }
                    }

                }

                _configService.SaveDiff(newone, ConfigPaths.ConfigDiffs);

            }
            else
            {
                List<string> fupdate = new List<string>();

                List<string> folders = GetDesktopFolders().ToList();
                for (var i = 0; i < diffs.Targets.Count; i++)
                {
                    if (config.Files.IndexOf(diffs.Files[i]) > 0)
                    {
                        var index = config.Files.IndexOf(diffs.Files[i]);
                        if (config.Files[index] == diffs.Files[i] && config.Icons[index] == diffs.Icons[i])
                        {
                            fupdate.Add(diffs.Targets[i]);
                        }
                    }
                }


                _logger.LogInformation("Assigning icons to desktop folders...");
                foreach (var folder in GetDesktopFolders())
                {
                    if (fupdate.Contains(folder)) continue;
                    Debug.WriteLine(folder, " -- Not Ignored");
                    // Obtiene todos los archivos del folder
                    var files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
                    var matched = false;
                    var patterstaken = string.Empty;
                    string iconPath = null!;

                    // Busca el primer patrón que encaje
                    if (config != null)
                    {
                        for (var i = 0; i < config.Files.Count; i++)
                        {
                            _logger.LogDebug("Processing folder: {Folder}", folder);
                            var pattern = config.Files[i];
                            if (files.Select(f => Path.GetFileName(f)).Any(name => name.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                            {
                                matched = true;
                                patterstaken = pattern;
                                iconPath = config.Icons[i];
                                _logger.LogDebug("Pattern '{Pattern}' was found in {Folder}. it will use icon : {IconPath}", pattern, folder, iconPath);
                            }
                            if (matched) break;
                        }

                        if (matched && File.Exists(iconPath))
                        {
                            _logger.LogInformation("Applying icon {IconPath} to folder {Folder}", iconPath, folder);
                            OnlyIcon(folder, iconPath);
                            Task.Delay(10).Wait();
                            if (diffs.Targets.Contains(folder))
                            {
                                var index = diffs.Targets.IndexOf(folder);
                                diffs.Icons[index] = iconPath;
                                diffs.Files[index] = patterstaken;
                            }
                            else
                            {
                                diffs.Targets.Add(folder);
                                diffs.Icons.Add(iconPath);
                                diffs.Files.Add(patterstaken);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("No matching pattern found for folder {Folder}. Removing icon if exists.", folder);
                            // Si no coincide ningún patrón, elimina cualquier ícono anterior
                            RemoveIconConfig(folder);
                        }

                        _configService.SaveDiff(diffs, ConfigPaths.ConfigDiffs);

                    }

                }
            }
            //Task.Delay(1000).Wait();

            //SHChangeNotify(0x08000000, 0x1000, null, IntPtr.Zero);

            _logger.LogInformation("Icon assignment completed. 😼");

        }

        public void ApplyOneFolder(string folder, ConfigData config)
        {
            Debug.WriteLine("OneFOlder: " + folder);
            if (config != null)
            {
                ConfigDiff? diffs = _configService.LoadDiff(ConfigPaths.ConfigDiffs);

                ConfigDiff newone = new ConfigDiff();
                _logger.LogInformation("Assigning icons to desktop folders...");
                // Obtiene todos los archivos del folder
                if (!Directory.Exists(folder))
                {
                    _logger.LogWarning("ApplyOneFolder: la carpeta no existe: {Folder}", folder);
                    return;
                }
                var files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
                var matched = false;
                var patterstaken = string.Empty;
                string iconPath = null!;

                // Busca el primer patrón que encaje
                for (var i = 0; i < config.Files.Count; i++)
                {
                    _logger.LogDebug("Processing folder: {Folder}", folder);
                    var pattern = config.Files[i];
                    if (files.Select(f => Path.GetFileName(f)).Any(name => name.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                    {
                        matched = true;
                        patterstaken = pattern;
                        iconPath = config.Icons[i];
                        _logger.LogDebug("Pattern '{Pattern}' was found in {Folder}. it will use icon : {IconPath}", pattern, folder, iconPath);
                    }
                    if (matched) break;
                }

                if (matched && File.Exists(iconPath))
                {
                    _logger.LogInformation("Applying icon {IconPath} to folder {Folder}", iconPath, folder);
                    Debug.WriteLine("Applying icon to folder ", iconPath.ToString(), folder);


                    OnlyIcon(folder, iconPath);
                    if (diffs == null)
                    {
                        newone.Targets.Add(folder);
                        newone.Icons.Add(iconPath);
                        newone.Files.Add(patterstaken);
                        _configService.SaveDiff(newone, ConfigPaths.ConfigDiffs);
                    }
                    else
                    {
                        if (diffs.Targets.Contains(folder))
                        {
                            var index = diffs.Targets.IndexOf(folder);
                            diffs.Icons[index] = iconPath;
                            diffs.Files[index] = patterstaken;
                        }
                        else
                        {
                            diffs.Targets.Add(folder);
                            diffs.Icons.Add(iconPath);
                            diffs.Files.Add(patterstaken);
                        }
                        _configService.SaveDiff(diffs, ConfigPaths.ConfigDiffs);
                    }
                }
                else
                {
                    _logger.LogInformation("No matching pattern found for folder {Folder}. Removing icon if exists.", folder);
                    // Si no coincide ningún patrón, elimina cualquier ícono anterior
                    RemoveIconConfig(folder);
                }

                //SHChangeNotify(0x00002000, 0x0005, folder, IntPtr.Zero);
            }
        }


        private void OnlyIcon(string folderPath, string sourceIconPath)
        {
            string uniqueName = $"iconizer_{DateTime.Now:yyyyMMddHHmmss}.ico";
            var icoDest = Path.Combine(folderPath, uniqueName);
            var iniDest = Path.Combine(folderPath, "desktop.ini");
            Debug.WriteLine("OnlyIcon: " + folderPath + " " + sourceIconPath);


            var oldIcons = Directory.GetFiles(folderPath, "iconizer_*.ico", SearchOption.TopDirectoryOnly);
            foreach (var oldIconPath in oldIcons)
            {
                try
                {
                    File.SetAttributes(oldIconPath, FileAttributes.Normal);
                    File.Delete(oldIconPath);
                    _logger.LogDebug("Deleted old icon file: {OldIconPath}", oldIconPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old icon: {OldIconPath}", oldIconPath);
                }
            }

            SafeCopy(sourceIconPath, icoDest, overwrite: true, logger: _logger);
            File.SetAttributes(icoDest, FileAttributes.Hidden | FileAttributes.System);

            try
            {
                File.SetAttributes(icoDest, FileAttributes.Hidden | FileAttributes.System);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ApplyIcon: Error Set Attributes'{IcoDest}' .", icoDest);
            }

            try
            {
                var folderAttr2 = File.GetAttributes(folderPath);
                File.SetAttributes(folderPath, folderAttr2 | FileAttributes.System);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ApplyIcon: Error System Attr '{FolderPath}'", folderPath);
            }


            var fcs = new SHFOLDERCUSTOMSETTINGS
            {
                dwMask = FCSM_ICONFILE,
                pszIconFile = icoDest,
                iIconIndex = 0,
            };

            try
            {
                uint hr = SHGetSetFolderCustomSettings(ref fcs, folderPath, FCS_FORCEWRITE);
                int win32 = Marshal.GetLastWin32Error();

                if (hr != 0 || win32 != 0)
                {
                    Debug.WriteLine(
                        "SHGetSetFolderCustomSettings falló: HRESULT=0x{Hr:X8}, Win32Error={Win32}" +
                        hr + win32);
                }
                else
                {
                    Debug.WriteLine("Applied");
                }
                _logger.LogInformation("Icon applied via Shell API to {FolderPath}", folderPath);

            }
            catch (Exception er)
            {
                Debug.WriteLine("Error applying: " + er.Message);
            }



            SHChangeNotify(0x00001000, 0x0005, folderPath, IntPtr.Zero);
            SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
        }





        /// <summary>
        /// Intenta borrar un archivo, quitando atributos y reintentando si está bloqueado.
        /// </summary>
        private static void SafeDelete(string path, int maxRetries, int delayMs, ILogger? logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        // Quita atributos especiales
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.Delete(path);
                    }
                    return;
                }
                catch (IOException)
                {
                    // Archivo bloqueado: espera y reintenta
                    Thread.Sleep(delayMs);
                }
                catch (UnauthorizedAccessException)
                {
                    // A veces con sólo quitar atributos basta, pero espera y reintenta
                    Thread.Sleep(delayMs);
                }
            }
            // Si falla todas las veces, deja que la excepción burbujee o registra un warning
            logger.LogWarning("Failed to delete file {Path} after {MaxRetries} attempts.", path, maxRetries);
        }


        /// <summary>
        /// Escribe todo el texto en el archivo, abriendo con FileShare.ReadWrite.
        /// </summary>
        private static void SafeWriteAllText(
            string path,
            string contenido,
            Encoding encoding,
            int maxRetries = 5,
            int delayMs = 100,
            ILogger logger = null!)
        {
            for (int intento = 1; intento <= maxRetries; intento++)
            {
                try
                {
                    // Ensure directory
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var modo = File.Exists(path) ? FileMode.Truncate : FileMode.Create;

                    using (var fs = new FileStream(
                        path,
                        modo,
                        FileAccess.Write,
                        FileShare.ReadWrite
                    ))
                    using (var sw = new StreamWriter(fs, encoding))
                    {
                        sw.Write(contenido);
                    }

                    return;
                }
                catch (IOException ex)
                {
                    if (intento == maxRetries)
                    {
                        logger?.LogWarning(ex, "SafeWriteAllText: Cannot write '{Path}' after {MaxRetries} attempts.", path, maxRetries);
                        throw;
                    }
                    Thread.Sleep(delayMs);
                }
                catch (UnauthorizedAccessException ex)
                {
                    if (intento == maxRetries)
                    {
                        logger?.LogWarning(ex, "SafeWriteAllText: Denied Access '{Path}'.", path);
                        throw;
                    }
                    Thread.Sleep(delayMs);
                }
            }
        }

        /// <summary>
        /// Copia un archivo, opcionalmente sobrescribiendo sin lanzar excepción.
        /// </summary>
        private static void SafeCopy(
            string source,
            string dest,
            bool overwrite,
            ILogger logger,
            int maxRetries = 5,
            int delayMs = 100)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            for (int intento = 1; intento <= maxRetries; intento++)
            {
                try
                {
                    File.Copy(source, dest, overwrite);
                    return;
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    logger?.LogWarning(
                        uaEx,
                        "SafeCopy: Denied access '{Dest}', Attempt {Attempt}. Trying delete attr.",
                        dest,
                        intento);

                    try
                    {
                        if (File.Exists(dest))
                        {
                            File.SetAttributes(dest, FileAttributes.Normal);
                            File.Delete(dest);
                            logger?.LogDebug("SafeCopy: '{Dest}' deleted and new attr.", dest);
                        }
                    }
                    catch (Exception exBorrado)
                    {
                        logger?.LogWarning(
                            exBorrado,
                            "SafeCopy: Fail Deleting'{Dest}' UnauthorizedAccessException.",
                            dest);
                    }
                }
                catch (IOException ioEx) when (overwrite && File.Exists(dest))
                {
                    logger?.LogWarning(
                        ioEx,
                        "SafeCopy: IOException copyng to '{Dest}', Attempt {Attempt}. Now trying SafeDelete.",
                        dest,
                        intento);

                    SafeDelete(dest, maxRetries: 3, delayMs: delayMs, logger: logger);
                }
                catch (IOException ioExGeneral)
                {
                    logger?.LogWarning(
                        ioExGeneral,
                        "SafeCopy: IOException unexpected from '{Source}' to '{Dest}', Attempt {Attempt}.",
                        source,
                        dest,
                        intento);
                }

                Thread.Sleep(delayMs);

                if (intento == maxRetries)
                {
                    logger?.LogError(
                        "SafeCopy: no se pudo copiar '{Source}' a '{Dest}' tras {MaxRetries} intentos.",
                        source,
                        dest,
                        maxRetries);
                    //TODO: Logs  File
                    // throw new IOException($"Copy Error '{source}' to '{dest}' after {maxRetries} attempts.");
                }
            }
        }



        private static void RemoveIconConfig(string folderPath)
        {
            var files = Directory.GetFiles(folderPath, "*.ico", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    if (file.Contains("iconizer_", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle the exception as needed
                    Console.WriteLine($"Error deleting file {file}: {ex.Message}");
                }
            }
            var ini = Path.Combine(folderPath, "desktop.ini");
            //var ico = Path.Combine(folderPath, "iconizer.ico");

            if (File.Exists(ini))
                File.Delete(ini);
            //if (File.Exists(ico))
            //File.Delete(ico);

            SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
        }
    }
}
