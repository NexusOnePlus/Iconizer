using System.Text;
using System.Windows;
using System.IO;
using Iconizer.Presentation.View.UserControls;
using System.Text.Json;
using Path = System.IO.Path;
using System.Runtime.InteropServices;

namespace Iconizer.Presentation;

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

        LoadConfig();
    }

    private void LoadConfig()
    {
        if (File.Exists(ConfigPath))
        {
            string json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<ConfigData>(json);

            if (config != null)
            {
                InputsPanel.Children.Clear();
                var i = 0;
                foreach (var file in config.Files)
                {
                    var tb = new ClearTextBox();
                    tb.MyTextBox.Text = file;
                    tb.IconInput.Text = config.Icons[i];
                    tb.RequestRemove += (s, args) =>
                    {
                        InputsPanel.Children.Remove((tb));
                    };
                    InputsPanel.Children.Add(tb);
                    i++;
                }
            }
        }
        else
        {
            for (var i = 0; i < 5; i++)
            {
              //  FilesPanel.Children.Add(new ClearTextBox());
              var tb = new ClearTextBox();
              tb.RequestRemove += (s, args) =>
              {
                  InputsPanel.Children.Remove((tb));
              };
                InputsPanel.Children.Add(tb);
            }
        }
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
                                File.Copy(config.Icons[i], $"{folder}\\iconizer.ico");
                                found = true;
                                string desktopinitPath = Path.Combine(folder, "desktop.ini");
                                string initContect =
                                    "[.ShellClassInfo]\n" +
                                    $"IconResource=iconizer.ico,0\n";
                                File.WriteAllText(desktopinitPath, initContect, Encoding.Unicode);
                                File.SetAttributes($"{folder}\\iconizer.ico",
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

     
    private class ConfigData
    {
        public List<string> Files { get; set; } = new();
        public List<string> Icons { get; set; } = new();
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearTextBox tb = new ClearTextBox();
        tb.RequestRemove += (s, args) =>
        {
            InputsPanel.Children.Remove((tb));
        };
        InputsPanel.Children.Add(tb);
    }

    private void SaveButtonMethod(object sender, RoutedEventArgs e)
    {
        var config = new ConfigData();
        foreach (ClearTextBox tb in InputsPanel.Children)
        {
            bool fail = false;
            switch (tb.MyTextBox.Text)
            {
                case ".cpp":
                case ".rs":
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

            if (!File.Exists(tb.IconInput.Text))
            {
                fail = true;
            }

            if (!fail)
            {
                config.Files.Add(tb.MyTextBox.Text);
                config.Icons.Add(tb.IconInput.Text);
            }
        }

        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
        SearchApply(config);
    }
}