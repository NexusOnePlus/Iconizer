using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;
using Iconizer.Presentation;
using Iconizer.Utils;

namespace Iconizer.Infrastructure;

public class Background
{
    public static DesktopWatcher DesktopWatcherC { get; private set; } = null!;
    public static ConfigLoader ConfigLoaderC { get; private set; } = null!;
    private static TaskbarIcon? _trayIcon;

    public static async Task Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream("Iconizer.Assets.extension_icon.ico");
        if (stream == null)
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "extension_icon.ico");
            if (File.Exists(iconPath))
            {
                _trayIcon = new TaskbarIcon
                {
                    Icon = new Icon(iconPath),
                    ToolTipText = "Iconizer Running"
                };
            }
            else
            {
                MessageBox.Show(
                    "Icon Not Found");
                _trayIcon = new TaskbarIcon
                {
                    ToolTipText = "Iconizer"
                };
            }
        }
        else
        {
            _trayIcon = new TaskbarIcon
            {
                Icon = new System.Drawing.Icon(stream),
                ToolTipText = "Iconizer"
            };
        }

        _trayIcon.ContextMenu = CreateContextMenu();
        _trayIcon.TrayContextMenuOpen += (s, e) =>
        {
            if (_trayIcon.ContextMenu != null)
            {
                _trayIcon.ContextMenu.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    if (_trayIcon.ContextMenu.IsOpen)
                    {
                        _trayIcon.ContextMenu.Focus();
                    }
                }));
            }
        };

        _trayIcon.TrayLeftMouseDown += (_, _) => App.ShowWindow();
        ConfigLoaderC = new ConfigLoader();
        ConfigLoaderC.Load();

        DesktopWatcherC = new DesktopWatcher(
            ConfigLoaderC,
            new FileIconService()
        );
        await DesktopWatcherC.Start();
    }

    private static ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();
        var openItem = new MenuItem { Header = "Open" };
        openItem.Click += (_, _) => App.ShowWindow();
        menu.Items.Add(openItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => System.Windows.Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    public static void Dispose()
    {
        _trayIcon?.Dispose();
        DesktopWatcherC?.Dispose();
    }
}