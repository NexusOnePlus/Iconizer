using Iconizer.Application.Services;
using Iconizer.Application.Validators;
using Iconizer.Infrastructure.Services;
using Iconizer.Presentation;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using System;
using Velopack;
using Velopack.Sources;

namespace Iconizer
{
    public partial class App : System.Windows.Application
    {
        private IServiceProvider? _provider;
        private ITrayIconService? _trayService;

        [STAThread]
        private static void Main(string[] args)
        {
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "IconizerAppMutex", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("App already running.", "Single instance", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    VelopackApp.Build().Run();

                    var app = new App();
                    app.InitializeComponent();
                    app.Run();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fatal error on startup runtime: {ex.Message}", "Start failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            // Application
            services.AddSingleton<IConfigService, ConfigService>();
            services.AddSingleton<IIconAssignmentService, IconAssignmentService>();
            services.AddSingleton<IExtensionValidator, ExtensionValidator>();
            services.AddSingleton<IDesktopWatcher, DesktopWatcher>();
            services.AddSingleton<ITrayIconService, TrayIconService>();
            services.AddSingleton<IFileIconService, FileIconService>();
            services.AddSingleton<ICleaner, Cleaner>();
            services.AddSingleton<MainWindow>();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            _provider = services.BuildServiceProvider();

            _trayService = _provider.GetRequiredService<ITrayIconService>();
            await _trayService.InitializeAsync();

            var watcher = _provider.GetRequiredService<IDesktopWatcher>();
            await watcher.StartAsync();

            var window = _provider.GetRequiredService<MainWindow>();
            MainInstance = window;
            MainInstance.Closing += MainIsClosing;
            MainInstance.Closed += (_, _) => MainInstance = null;
            //MainInstance.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayService?.Dispose();
            // Si alguno de tus servicios (IServiceProvider) implementa IDisposable,
            // puedes hacer algo como (_serviceProvider as IDisposable)?.Dispose();
            base.OnExit(e);
        }

        public static MainWindow? MainInstance { get; private set; }

        public static void ShowWindow()
        {
            Current.Dispatcher.Invoke(() =>
            {
                if (MainInstance == null &&
                    Current is App app &&
                    app._provider != null)
                {
                    MainInstance = app._provider.GetRequiredService<MainWindow>();
                    MainInstance.Closing += MainIsClosing;
                    MainInstance.Closed += (_, _) => MainInstance = null;
                }

                MainInstance!.Show();
                MainInstance.Activate();
                MainInstance.Focus();
            });
        }

        private static void MainIsClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            MainInstance!.Hide();
        }
    }
}
