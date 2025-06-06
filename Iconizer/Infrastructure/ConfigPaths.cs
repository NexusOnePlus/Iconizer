using System;
using System.IO;

namespace Iconizer.Infrastructure
{
    /// <summary>
    /// Rutas y nombres de archivo usados por toda la aplicación.
    /// </summary>
    public static class ConfigPaths
    {
        private static readonly string DocumentsFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private static readonly string BaseFolder = 
            Path.Combine(DocumentsFolder, "Iconizer");

        /// <summary>
        /// Carpeta donde se almacenan los archivos de configuración (y otros recursos).
        /// </summary>
        public static string ConfigFolder 
        { 
            get
            {
                // Asegura que exista la carpeta
                if (!Directory.Exists(BaseFolder))
                    Directory.CreateDirectory(BaseFolder);
                return BaseFolder;
            }
        }

        /// <summary>
        /// Ruta completa al JSON de configuración.
        /// </summary>
        public static string ConfigFilePath
            => Path.Combine(ConfigFolder, "config.json");

        public static string ConfigDiffs
            => Path.Combine(ConfigFolder, "diffs.json");

        public static string IconsFolder
            => Path.Combine(ConfigFolder, "Icons");
    }
}