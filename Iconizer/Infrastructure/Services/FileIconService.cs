using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Iconizer.Domain;

namespace Iconizer.Infrastructure.Services
{
    public interface IFileIconService
    {
        void AssignIconsToFolders(ConfigData config);
        IEnumerable<string> GetDesktopFolders();
    }

    public class FileIconService : IFileIconService
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(
            int wEventId, int uFlags, string dwItem1, IntPtr dwItem2);

        public IEnumerable<string> GetDesktopFolders()
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            return Directory.Exists(desktop)
                ? Directory.GetDirectories(desktop)
                : Array.Empty<string>();
        }

        public void AssignIconsToFolders(ConfigData config)
        {
            foreach (var folder in GetDesktopFolders())
            {
                // Obtiene todos los archivos del folder
                var files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
                var matched = false;
                string iconPath = null!;

                // Busca el primer patrón que encaje
                for (var i = 0; i < config.Files.Count; i++)
                {
                    var pattern = config.Files[i];
                    if (files.Select(f => Path.GetFileName(f)).Any(name => name.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                    {
                        matched = true;
                        iconPath = config.Icons[i];
                    }
                    if (matched) break;
                }

                if (matched && File.Exists(iconPath))
                {
                    ApplyIcon(folder, iconPath);
                }
                else
                {
                    // Si no coincide ningún patrón, elimina cualquier ícono anterior
                    RemoveIconConfig(folder);
                }
            }
            
        }

        private void ApplyIcon(string folderPath, string sourceIconPath) {       
            var icoDest = Path.Combine(folderPath, "iconizer.ico");
            var iniDest = Path.Combine(folderPath, "desktop.ini");

            // 1) Borrado seguro de icoDest
            SafeDelete(icoDest, maxRetries: 5, delayMs: 200);

            // 2) Copia segura con overwrite
            SafeCopy(sourceIconPath, icoDest, overwrite: true);

            // 3) Prepara y borra desktop.ini si existe
            var iniContent = "[.ShellClassInfo]\r\nIconResource=iconizer.ico,0\r\n";
            SafeDelete(iniDest, maxRetries: 5, delayMs: 200);
            File.WriteAllText(iniDest, iniContent, Encoding.Unicode);

            // 4) Ajuste de atributos
            File.SetAttributes(icoDest, FileAttributes.Hidden | FileAttributes.System);
            File.SetAttributes(iniDest, FileAttributes.Hidden | FileAttributes.System);
            var folderAttr = File.GetAttributes(folderPath);
            File.SetAttributes(folderPath, folderAttr | FileAttributes.System);

            // 5) Notificar al shell
            SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
        }

        /// <summary>
        /// Intenta borrar un archivo, quitando atributos y reintentando si está bloqueado.
        /// </summary>
        private static void SafeDelete(string path, int maxRetries, int delayMs)
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
        }

        /// <summary>
        /// Copia un archivo, opcionalmente sobrescribiendo sin lanzar excepción.
        /// </summary>
        private static void SafeCopy(string source, string dest, bool overwrite)
        {
            try
            {
                File.Copy(source, dest, overwrite);
            }
            catch (IOException ex) when (overwrite && File.Exists(dest))
            {
                // Si era porque ya existe y pedimos overwrite, intenta de todos modos
                SafeDelete(dest, maxRetries: 3, delayMs: 100);
                File.Copy(source, dest, false);
            }
        }


        private static  void RemoveIconConfig(string folderPath)
        {
            var ini = Path.Combine(folderPath, "desktop.ini");
            var ico = Path.Combine(folderPath, "iconizer.ico");

            if (File.Exists(ini))
                File.Delete(ini);
            if (File.Exists(ico))
                File.Delete(ico);

            SHChangeNotify(0x00002000, 0x0005, folderPath, IntPtr.Zero);
        }
    }
}
