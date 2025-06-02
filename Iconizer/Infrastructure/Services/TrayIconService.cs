using System.Reflection;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;
using System.Windows.Controls;
using Iconizer.Application.Services;
using Iconizer;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class TrayIconService : ITrayIconService, IDisposable
    {
        private TaskbarIcon? _trayIcon;
        private readonly ILogger<TrayIconService> _logger;
        public TrayIconService(ILogger<TrayIconService> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            // Icon loading
            _logger.LogInformation("Initializing Tray Icon Service");
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("Iconizer.Assets.extension_icon.ico");
            if (stream != null)
                _trayIcon = new TaskbarIcon { Icon = new System.Drawing.Icon(stream) };
            else
                _trayIcon = new TaskbarIcon { ToolTipText = "Iconizer" };

            _trayIcon.ToolTipText = "Iconizer Running";
            _trayIcon.ContextMenu = BuildMenu();
            _trayIcon.TrayLeftMouseDown += (_, _) => App.ShowWindow();

            await Task.CompletedTask;
        }

        private ContextMenu BuildMenu()
        {
            _logger.LogInformation("Building Tray Icon Context Menu");
            var menu = new ContextMenu();
            var open = new MenuItem { Header = "Open" };
            open.Click += (_, _) => App.ShowWindow();
            menu.Items.Add(open);
            menu.Items.Add(new Separator());
            var exit = new MenuItem { Header = "Exit" };
            exit.Click += (_, _) => Application.Current.Shutdown();
            menu.Items.Add(exit);
            return menu;
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing Tray Icon Service");
            _trayIcon?.Dispose();
        }
    }
}