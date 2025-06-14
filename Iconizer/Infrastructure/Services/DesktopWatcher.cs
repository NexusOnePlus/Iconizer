
using System.Diagnostics;
using System.IO;
using Iconizer.Application.Services;

namespace Iconizer.Infrastructure.Services
{
    public class DesktopWatcher : IDesktopWatcher
    {
        private readonly string _desktopPath;
        private readonly IConfigService _configService;
        private readonly IFileIconService _iconService;
        private readonly FileSystemWatcher _rootWatcher;
        private readonly Dictionary<string, FileSystemWatcher> _folderWatchers;
        private readonly Dictionary<string, bool> _pendingUpdate;
        private readonly Dictionary<string, CancellationTokenSource?> _debounceTokens;
        private const int TimeDebounceDelay = 1; // seconds

        public DesktopWatcher(
            IConfigService configService,
            IFileIconService iconService)
        {
            _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _configService = configService;
            _iconService = iconService;
            _folderWatchers = [];
            _pendingUpdate = [];
            _debounceTokens = [];

            _rootWatcher = new FileSystemWatcher(_desktopPath)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.DirectoryName
            };
            _rootWatcher.Created +=  (_,  e) => AddFolderWatcher(e.FullPath);
            _rootWatcher.Deleted += (_, e) => RemoveFolderWatcher(e.FullPath);
            _rootWatcher.Renamed += (_, e) =>
            {
                RemoveFolderWatcher(e.OldFullPath);
                AddFolderWatcher(e.FullPath);
            };
        }

        private void DisableAllWatchers()
        {
            _rootWatcher.EnableRaisingEvents = false;
            foreach (var fsw in _folderWatchers.Values)
                fsw.EnableRaisingEvents = false;
        }

        private void EnableAllWatchers()
        {
            _rootWatcher.EnableRaisingEvents = true;
            foreach (var fsw in _folderWatchers.Values)
                fsw.EnableRaisingEvents = true;
        }

        public async Task StartAsync()
        {
            foreach (var folder in Directory.GetDirectories(_desktopPath))
                AddFolderWatcher(folder);
            _rootWatcher.EnableRaisingEvents = true;
            await Task.CompletedTask;
        }

        public async Task ReloadAsync()
        {
            foreach (var key in _folderWatchers.Keys)
                RemoveFolderWatcher(key);
            _folderWatchers.Clear();

            _configService.Load(ConfigPaths.ConfigFilePath);
            await StartAsync();
        }

        private void  AddFolderWatcher(string folder)
        {
            if (!Directory.Exists(folder) || _folderWatchers.ContainsKey(folder)) return;
            Debug.WriteLine($"Adding watcher for folder: {folder}");
            // Aplica ícono inicial o remueve
            //var config =  _configService.Load(ConfigPaths.ConfigFilePath);
            //_iconService.AssignIconsToFolders(config!);

            _pendingUpdate[folder] = false;
            _debounceTokens[folder] = null;

            var fsw = new FileSystemWatcher(folder)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName
            };
            
            fsw.Created += async (_, e) => await HandleFileChangeAsync(folder, Path.GetFileName(e.FullPath));
            fsw.Deleted += async (_, e) => await HandleFileChangeAsync(folder, Path.GetFileName(e.FullPath));
            fsw.Renamed += async (s, e) => await HandleFileChangeAsync(folder, Path.GetFileName(e.FullPath));
            fsw.EnableRaisingEvents = true;

            _folderWatchers[folder] = fsw;
        }
        
        private async Task HandleFileChangeAsync(string folder, string fileName)
        {
            if (IgnoreFile(fileName)) return;
            await Debounce(folder);
        }
        
        private static bool IgnoreFile(string fileName)
        {
            if (string.Equals(fileName, "desktop.ini", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(fileName, "node_modules", StringComparison.OrdinalIgnoreCase))
                return true;

            if (fileName.StartsWith("iconizer_", StringComparison.OrdinalIgnoreCase)
                && fileName.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }



        private void RemoveFolderWatcher(string folder)
        {
            if (_folderWatchers.TryGetValue(folder, out var watcher))
            {
                watcher.Dispose();
                _folderWatchers.Remove(folder);
                Debug.WriteLine($"Removing watcher for folder: {folder}");

            }
        }

        private async Task Debounce(string folder)
        {
            
            // Cancel previous if existe
            if (_debounceTokens[folder] is { } existing)
            {
                existing.Cancel();
                existing.Dispose();
                _pendingUpdate[folder] = true;
            }
            var cts = new CancellationTokenSource();
            _debounceTokens[folder] = cts;
            _pendingUpdate[folder] = false;

            try
            {

                await Task.Delay(TimeSpan.FromSeconds(TimeDebounceDelay), cts.Token).ConfigureAwait(false);
                DisableAllWatchers();
                try
                {
                    var config = _configService.Load(ConfigPaths.ConfigFilePath);
                    _iconService.ApplyOneFolder(folder, config!);
                }
                finally
                {
                    EnableAllWatchers(); // Asegura reactivar los watchers
                }
            }
            catch (TaskCanceledException)
            {
                // marcado para nueva corrida
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during debounce for folder {ex.Message}");
            }
            if (_pendingUpdate[folder])
                await Debounce(folder);
        }

        public void Dispose()
        {
            _rootWatcher.Dispose();
            foreach (var w in _folderWatchers.Values) w.Dispose();
            foreach (var c in _debounceTokens.Values) c?.Dispose();
            _folderWatchers.Clear();
            _debounceTokens.Clear();
        }
    }
}