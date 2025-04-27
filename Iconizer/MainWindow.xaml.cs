using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Iconizer.View.UserControls;
using System.Text.Json;
using Path = System.IO.Path;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Iconizer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(
        int wEventId, int uFlags, string dwItem1, IntPtr dwItem2);

    private readonly string ConfigPath =
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Iconizer\config.json";

    private readonly string FolderPath =
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Iconizer";


    public MainWindow()
    {
        InitializeComponent();

        TbHello.Text = "Settings";
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }

        loadConfig();
    }

    private void loadConfig()
    {
        if (File.Exists(ConfigPath))
        {
            string json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<ConfigData>(json);

            if (config != null)
            {
                FilesPanel.Children.Clear();
                InputsPanel.Children.Clear();

                foreach (var file in config.Files)
                {
                    var tb = new ClearTextBox();
                    tb.MyTextBox.Text = file;
                    FilesPanel.Children.Add(tb);
                }

                foreach (var file in config.Icons)
                {
                    var tb = new ClearTextBox();
                    tb.MyTextBox.Text = file;
                    InputsPanel.Children.Add(tb);
                }
            }
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                FilesPanel.Children.Add(new ClearTextBox());
                InputsPanel.Children.Add(new ClearTextBox());
            }
        }
    }

    private void BtButton_OnClick(object sender, RoutedEventArgs e)
    {
        var config = new ConfigData();

        int i = 0;
        foreach (ClearTextBox tb in FilesPanel.Children)
        {
            bool fail = false;
            switch (tb.MyTextBox.Text)
            {
                case ".cpp":
                case ".js":
                case ".jsx":
                case "package.json":
                case ".py":
                case "config.toml":
                case "bunfig.toml":
                case "deno.json":
                case ".ts":
                case ".tsx":
                case ".yml":
                case ".json":
                case ".lock":
                case ".png":
                    break;
                default:
                    fail = true;
                    break;
            }

            var tc = InputsPanel.Children[i] as ClearTextBox;
            if (!File.Exists(tc.MyTextBox.Text))
            {
                fail = true;
            }

            if (!fail)
            {
                config.Files.Add(tb.MyTextBox.Text);
                config.Icons.Add(tc.MyTextBox.Text);
            }

            i++;
        }

        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
        SearchApply(config);
    }

    async void SearchApply(ConfigData config)
    {
        string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string[] folders = Directory.GetDirectories(rootPath, "*", SearchOption.TopDirectoryOnly);
        foreach (string folder in folders)
        {
            string[] dataFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly);
            if (dataFiles.Length > 0)
            {
                bool already = false;
                List<string> names = new List<string>();
                foreach (string file in dataFiles)
                {
                    if (Path.GetFileName(file) == "desktop.ini")
                    {
                        already = true;
                        break;
                    }

                    names.Add(Path.GetFileName(file));
                }

                if (!already)
                {
                    bool found = false;
                    foreach (string file in names)
                    {
                        int i = 0;
                        foreach (string ext in config.Files)
                        {
                            if (file.Contains(ext))
                            {
                                File.Copy(config.Icons[i], $"{folder}\\iconizer-{Path.GetFileName(config.Icons[i])}");
                                found = true;
                                string desktopinitPath = Path.Combine(folder, "desktop.ini");
                                string initContect =
                                    "[.ShellClassInfo]\n" +
                                    $"IconResource=iconizer-{Path.GetFileName(config.Icons[i])},0\n";
                                File.WriteAllText(desktopinitPath, initContect, Encoding.Unicode);
                                File.SetAttributes($"{folder}\\iconizer-{Path.GetFileName(config.Icons[i])}",
                                    FileAttributes.Hidden | FileAttributes.System);

                                File.SetAttributes(desktopinitPath, FileAttributes.Hidden | FileAttributes.System);
                                File.SetAttributes(folder, File.GetAttributes(folder) | FileAttributes.System);
                                Directory.SetLastWriteTime(folder, DateTime.Now);

                                //Reloading
                                SHChangeNotify(0x08000000, 0x0000, folder, (IntPtr)null);
                                await Task.Delay(2000);

                                File.SetAttributes(folder, File.GetAttributes(folder) | FileAttributes.ReadOnly);

                                File.SetAttributes(folder, File.GetAttributes(folder) & ~FileAttributes.System);


                                break;
                            }

                            i++;
                        }

                        if (found)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearTextBox tb = new ClearTextBox();
        InputsPanel.Children.Add(tb);
        ClearTextBox tc = new ClearTextBox();
        FilesPanel.Children.Add(tc);
    }

    private void AddFile_OnClick(object sender, RoutedEventArgs e)
    {
        ClearTextBox tc = new ClearTextBox();
        InputsPanel.Children.Add(tc);
        ClearTextBox tb = new ClearTextBox();
        FilesPanel.Children.Add(tb);
    }

    private class ConfigData
    {
        public List<string> Files { get; set; } = new();
        public List<string> Icons { get; set; } = new();
    }
}