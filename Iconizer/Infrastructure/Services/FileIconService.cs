using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Iconizer.Domain;
using Microsoft.Extensions.Logging;

namespace Iconizer.Infrastructure.Services
{
    public interface IFileIconService
    {
        void AssignIconsToFolders(ConfigData config);
        IEnumerable<string> GetDesktopFolders();
    }

    public class FileIconService : IFileIconService
    {
        private readonly ILogger<FileIconService> _logger;
        public FileIconService(ILogger<FileIconService> logger)
        {
            _logger = logger;
        }
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
                        ApplyIcon(folder, iconPath);
                    }
                    else
                    {
                        _logger.LogInformation("No matching pattern found for folder {Folder}. Removing icon if exists.", folder);
                        // Si no coincide ningún patrón, elimina cualquier ícono anterior
                        RemoveIconConfig(folder);
                    }
                }

            }
            _logger.LogInformation("Icon assignment completed. 😼");

        }

        private void ApplyIcon(string folderPath, string sourceIconPath)
        {
            string uniqueName = $"iconizer_{DateTime.Now:yyyyMMddHHmmss}.ico";
            var icoDest = Path.Combine(folderPath, uniqueName);
            var iniDest = Path.Combine(folderPath, "desktop.ini");

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

            try
            {
                var folderAttr = File.GetAttributes(folderPath);
                var limpia = folderAttr & ~(FileAttributes.ReadOnly | FileAttributes.System);
                File.SetAttributes(folderPath, limpia);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ApplyIcon: Couldn't remove attributes '{folderPath}'", folderPath);
            }

            _logger.LogDebug("Copying icon from {SourceIconPath} to {IcoDest}", sourceIconPath, icoDest);
            SafeCopy(sourceIconPath, icoDest, overwrite: true, logger: _logger);

            Thread.Sleep(50);

            _logger.LogDebug("Preparing desktop.ini at {IniDest}", iniDest);
            var iniContent = $"[.ShellClassInfo]\r\nIconResource={uniqueName},0\r\n";
            SafeDelete(iniDest, maxRetries: 5, delayMs: 200, logger: _logger);

            try
            {
                SafeWriteAllText(
                    iniDest,
                    iniContent,
                    Encoding.Unicode,
                    maxRetries: 5,
                    delayMs: 200,
                    logger: _logger
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ApplyIcon: Coudn't write '{IniDest}'.", iniDest);
            }

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
                File.SetAttributes(iniDest, FileAttributes.Hidden | FileAttributes.System);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ApplyIcon: Error Apply Attributes Hidden System '{IniDest}'", iniDest);
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

            // 6) Notificar al shell
            _logger.LogInformation("Notifying shell about changes in {FolderPath}", folderPath);
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
