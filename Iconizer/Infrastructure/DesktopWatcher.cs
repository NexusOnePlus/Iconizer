using System.IO;
using Iconizer.Utils;

namespace Iconizer.Infrastructure;

public class DesktopWatcher : IDisposable
{
    private readonly string _desktopPath;
    private readonly ConfigLoader _config;
    private readonly FileIconService _iconService;
    private readonly FileSystemWatcher _rootWatcher;
    private readonly Dictionary<string, FileSystemWatcher> _folderWatchers;
    private readonly Dictionary<string, CancellationTokenSource?> _debounceTokens = new();
    private readonly Dictionary<string, bool> _pendingUpdate = new();

    public DesktopWatcher(
        ConfigLoader config,
        FileIconService iconService)
    {
        _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        _config = config;
        _iconService = iconService;
        _folderWatchers = new Dictionary<string, FileSystemWatcher>();

        _rootWatcher = new FileSystemWatcher(_desktopPath)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.DirectoryName
        };
        _rootWatcher.Created += async (o, ev) => { await OnFolderCreated(o, ev); };
        _rootWatcher.Deleted += OnFolderDeleted;
        _rootWatcher.Renamed += async (o, ev) => { await OnFolderRenamed(o, ev); };
    }

    public async Task Start()
    {
        foreach (var folder in Directory.GetDirectories(_desktopPath))
        {
            await AddFolderWatcher(folder);
        }

        _rootWatcher.EnableRaisingEvents = true;
    }

    private async Task OnFolderCreated(object _, FileSystemEventArgs e)
    {
        await AddFolderWatcher(e.FullPath);
    }


    private void OnFolderDeleted(object sender, FileSystemEventArgs e)
        => RemoveFolderWatcher(e.FullPath);

    private async Task OnFolderRenamed(object _, RenamedEventArgs e)
    {
        RemoveFolderWatcher(e.OldFullPath);
        await AddFolderWatcher(e.FullPath);
    }

    private async Task AddFolderWatcher(string folder)
    {
        if (!Directory.Exists(folder) || _folderWatchers.ContainsKey(folder))
            return;

        await _iconService.ApplyRemoveIcon(folder, _config.Entries);

        _pendingUpdate[folder] = false;
        _debounceTokens[folder] = null;
        var fsw = new FileSystemWatcher(folder)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName
        };
        fsw.Created += async (_, ev) =>
        {
            var fileName = Path.GetFileName(ev.FullPath);
            if (fileName is "desktop.ini" or "iconizer.ico")
                return;

            await DebounceApplyRemove(folder, TimeSpan.FromMilliseconds(10000));
        };
        fsw.Deleted += async (_, ev) =>
        {
            var fileName = Path.GetFileName(ev.FullPath);
            if (fileName is "desktop.ini" or "iconizer.ico")
                return;

            await DebounceApplyRemove(folder, TimeSpan.FromMilliseconds(10000));
        };


        fsw.EnableRaisingEvents = true;
        _folderWatchers[folder] = fsw;
    }

    private void RemoveFolderWatcher(string folder)
    {
        if (_folderWatchers.TryGetValue(folder, out var fsw))
        {
            fsw.Dispose();
            _folderWatchers.Remove(folder);
        }
    }

    public async Task ReloadWatchers()
    {
        foreach (var key in _folderWatchers.Keys)
        {
            RemoveFolderWatcher(key);
        }

        _folderWatchers.Clear();

        _config.Load();
        foreach (var folder in Directory.GetDirectories(_desktopPath))
        {
            await AddFolderWatcher(folder);
        }
    }

    private async Task DebounceApplyRemove(string folder, TimeSpan delay)
    {
        if (_debounceTokens.TryGetValue(folder, out var existingToken) && existingToken != null)
        {
            await existingToken.CancelAsync();
            existingToken.Dispose();
            _debounceTokens.Remove(folder);
            _pendingUpdate[folder] = true;
            Console.WriteLine($"Debounce off {folder}");
        }

        var cts = new CancellationTokenSource();
        _debounceTokens[folder] = cts;
        _pendingUpdate[folder] = false;

        try
        {
            Console.WriteLine($"Waiting  {folder}", delay.ToString());
            try
            {
                await Task.Delay(delay, cts.Token);
            } catch (
                Exception e)
            {
                Console.WriteLine(folder," - ", e.Message);
                _pendingUpdate[folder] = true;
            }
            await _iconService.ApplyRemoveIcon(folder, _config.Entries);
            if (_pendingUpdate.TryGetValue(folder, out var pending) && pending)
            {
                await DebounceApplyRemove(folder, delay);
            }
        }
        catch (TaskCanceledException e)
        {
            Console.WriteLine($"folder:  {folder} ", e.Message);
        }
    }

    public void Dispose()
    {
        _rootWatcher.Dispose();
        foreach (var fsw in _folderWatchers.Values) fsw.Dispose();
        foreach (var cts in _debounceTokens.Values) cts?.Dispose();
    }
}