using System.Configuration;
using System.Data;
using System.Windows;
using Iconizer.Infrastructure;
using Iconizer.Presentation;


namespace Iconizer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public static MainWindow? MainInstance { get; private set; }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await Background.Initialize();

        MainWindow window = new Presentation.MainWindow();
        MainInstance = window;
        MainInstance.Closing += MainIsClosing;
        MainInstance.Closed += (_, _) => { MainInstance = null; };
        // window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Background.Dispose();
        base.OnExit(e);
    }

    public static void ShowWindow()
    {
        Current.Dispatcher.Invoke(() =>
        {
            if (MainInstance == null)
            {
                MainInstance = new MainWindow();
                System.Windows.Application.Current.MainWindow = MainInstance;
                MainInstance.Closing += MainIsClosing;
                MainInstance.Closed += (_, _) => { MainInstance = null; };
            }

            MainInstance.Show();
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