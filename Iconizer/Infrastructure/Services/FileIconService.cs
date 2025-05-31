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
            _logger.LogInformation("Icon assignment completed. 😼");
            
        }

        private void ApplyIcon(string folderPath, string sourceIconPath) {

            string uniqueName = $"iconizer_{DateTime.Now:yyyyMMddHHmmss}.ico";
            var icoDest = Path.Combine(folderPath, uniqueName);
            var iniDest = Path.Combine(folderPath, "desktop.ini");

            //if (File.Exists(icoDest) && File.Exists(iniDest))
            //{
            //    // Si no hay icono ni ini, no hacemos nada
            //    _logger.LogInformation("icon and ini file found in {FolderPath}, skipping icon assignment.\n", folderPath);
            //    return;
            //}

            // 1) Borrado seguro de icoDest
            //_logger.LogDebug("Deleting existing icon file if exists: {IcoDest}", icoDest);
            //SafeDelete(icoDest, maxRetries: 5, delayMs: 200 , logger : _logger);

            var oldIcons = Directory.GetFiles(folderPath, "iconizer_*.ico", SearchOption.TopDirectoryOnly);
            foreach (var oldIconPath in oldIcons)
            {
                try
                {
                    // 1) Quita cualquier atributo (Hidden, System, ReadOnly) para que se pueda eliminar
                    File.SetAttributes(oldIconPath, FileAttributes.Normal);
                    File.Delete(oldIconPath);
                    _logger.LogDebug("Deleted old icon file: {OldIconPath}", oldIconPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old icon: {OldIconPath}", oldIconPath);
                    // Opcional: seguir intentando si quieres más lógica de retry
                }
            }



            // 2) Copia segura con overwrite
            File.SetAttributes(folderPath, FileAttributes.Normal);

            _logger.LogDebug("Copying icon from {SourceIconPath} to {IcoDest}", sourceIconPath, icoDest);
            SafeCopy(sourceIconPath, icoDest, overwrite: true , logger : _logger);

            // 3) Prepara y borra desktop.ini si existe
            _logger.LogDebug("Preparing desktop.ini at {IniDest}", iniDest);
            var iniContent = $"[.ShellClassInfo]\r\nIconResource={uniqueName},0\r\n";
            SafeDelete(iniDest, maxRetries: 5, delayMs: 200 , logger:_logger);
            File.WriteAllText(iniDest, iniContent, Encoding.Unicode);
            
            _logger.LogInformation("Setting attributes....");
            // 4) Ajuste de atributos
            File.SetAttributes(icoDest, FileAttributes.Hidden | FileAttributes.System);
            File.SetAttributes(iniDest, FileAttributes.Hidden | FileAttributes.System);
            var folderAttr = File.GetAttributes(folderPath);
            File.SetAttributes(folderPath, folderAttr | FileAttributes.System);

            // 5) Notificar al shell
            _logger.LogInformation("Notifying shell about changes in {FolderPath}", folderPath);
            SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
        }

        /// <summary>
        /// Intenta borrar un archivo, quitando atributos y reintentando si está bloqueado.
        /// </summary>
        private static void SafeDelete(string path, int maxRetries, int delayMs , ILogger logger)
        {
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
        /// Copia un archivo, opcionalmente sobrescribiendo sin lanzar excepción.
        /// </summary>
        private static void SafeCopy(string source, string dest, bool overwrite , ILogger logger)
        {
            try
            {
                File.Copy(source, dest, overwrite);
            }
            catch (IOException ex) when (overwrite && File.Exists(dest))
            {
                // Si era porque ya existe y pedimos overwrite, intenta de todos modos
                SafeDelete(dest, maxRetries: 3, delayMs: 100 , logger: logger);
                File.Copy(source, dest, false);
            }
        }


        private static  void RemoveIconConfig(string folderPath)
        {
            var files = Directory.GetFiles(folderPath, "*.ico", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    if(file.Contains("iconizer_", StringComparison.OrdinalIgnoreCase))
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
