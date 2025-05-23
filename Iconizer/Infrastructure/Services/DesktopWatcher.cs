
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

        public DesktopWatcher(
            IConfigService configService,
            IFileIconService iconService)
        {
            _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _configService = configService;
            _iconService = iconService;
            _folderWatchers = new();
            _pendingUpdate = new();
            _debounceTokens = new();

            _rootWatcher = new FileSystemWatcher(_desktopPath)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.DirectoryName
            };
            _rootWatcher.Created += async (s, e) => await AddFolderWatcherAsync(e.FullPath);
            _rootWatcher.Deleted += (s, e) => RemoveFolderWatcher(e.FullPath);
            _rootWatcher.Renamed += async (s, e) =>
            {
                RemoveFolderWatcher(e.OldFullPath);
                await AddFolderWatcherAsync(e.FullPath);
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
                await AddFolderWatcherAsync(folder);
            _rootWatcher.EnableRaisingEvents = true;
        }

        public async Task ReloadAsync()
        {
            foreach (var key in _folderWatchers.Keys)
                RemoveFolderWatcher(key);
            _folderWatchers.Clear();

            _configService.Load(ConfigPaths.ConfigFilePath);
            await StartAsync();
        }

        private async Task AddFolderWatcherAsync(string folder)
        {
            if (!Directory.Exists(folder) || _folderWatchers.ContainsKey(folder)) return;

            // Aplica ícono inicial o remueve
            var config = _configService.Load(ConfigPaths.ConfigFilePath);
            _iconService.AssignIconsToFolders(config!);

            _pendingUpdate[folder] = false;
            _debounceTokens[folder] = null;

            var fsw = new FileSystemWatcher(folder)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName
            };
            fsw.Created += async (s, e) => await Debounce(folder);
            fsw.Deleted += async (s, e) => await Debounce(folder);
            fsw.EnableRaisingEvents = true;

            _folderWatchers[folder] = fsw;
        }

        private void RemoveFolderWatcher(string folder)
        {
            if (_folderWatchers.TryGetValue(folder, out var watcher))
            {
                watcher.Dispose();
                _folderWatchers.Remove(folder);
            }
        }

        private async Task Debounce(string folder)
        {
            
            // Cancel previous if existe
            if (_debounceTokens[folder] is CancellationTokenSource existing)
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
                
                await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
                DisableAllWatchers();
                try
                {
                    var config = _configService.Load(ConfigPaths.ConfigFilePath);
                    _iconService.AssignIconsToFolders(config!);
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
            if (_pendingUpdate[folder])
                await Debounce(folder);
        }

        public void Dispose()
        {
            _rootWatcher.Dispose();
            foreach (var w in _folderWatchers.Values) w.Dispose();
            foreach (var c in _debounceTokens.Values) c?.Dispose();
        }
    }
}