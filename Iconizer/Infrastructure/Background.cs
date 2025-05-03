using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;
using Iconizer.Presentation;

namespace Iconizer.Infrastructure;

public class Background
{
    public static class BackgroundApp
    {
        private static TaskbarIcon? TrayIcon;

        public static void Initialize()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream? stream = assembly.GetManifestResourceStream("Iconizer.Assets.extension_icon.ico");
            if (stream == null)
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "extension_icon.ico");
                if (File.Exists(iconPath))
                {
                    TrayIcon = new TaskbarIcon
                    {
                        Icon = new Icon(iconPath),
                        ToolTipText = "Iconizer Running"
                    };
                }
                else
                {
                    MessageBox.Show(
                        "Icon Not Found");
                    TrayIcon = new TaskbarIcon
                    {
                        ToolTipText = "Iconizer"
                    };
                }
            }
            else
            {
                TrayIcon = new TaskbarIcon
                {
                    Icon = new System.Drawing.Icon(stream),
                    ToolTipText = "Iconizer"
                };
            }

            TrayIcon.ContextMenu = CreateContextMenu();
            TrayIcon.TrayContextMenuOpen += (s, e) =>
            {
                if (TrayIcon.ContextMenu != null)
                {
                    TrayIcon.ContextMenu.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => {
                        if (TrayIcon.ContextMenu.IsOpen)
                        {
                            TrayIcon.ContextMenu.Focus();
                        }
                    }));
                }
            };

            TrayIcon.TrayLeftMouseDown += (_, _) => App.ShowWindow();
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
            TrayIcon?.Dispose();
        }
    }
}