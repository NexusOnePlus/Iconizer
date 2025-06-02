/*
 * Refactorización de MainWindow.xaml.cs:
 *
 * Extracciones:
 *  - ConfigData                → Domain/ConfigData.cs
 *  - IConfigService, ConfigService      → Application/Services/ConfigService.cs
 *  - IIconAssignmentService, IconAssignmentService → Application/Services/IconAssignmentService.cs
 *  - File-system y atributos   → Infrastructure/Services/FileIconService.cs
 *  - Cleaner                   → Infrastructure/Services/Cleaner.cs
 *  - Validaciones de extensiones → Application/Validators/ExtensionValidator.cs
 */

using Iconizer.Application.Services;
using Iconizer.Application.Validators;
using Iconizer.Domain;
using Iconizer.Infrastructure;
using Iconizer.Presentation.View.UserControls;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace Iconizer.Presentation
{
    public partial class MainWindow : Window
    {
        private readonly IConfigService _configService;
        private readonly IIconAssignmentService _iconService;
        private readonly IExtensionValidator _validator;

        public MainWindow(
            IConfigService configService,
            IIconAssignmentService iconService,
            IExtensionValidator validator)
        {
            InitializeComponent();

            _configService = configService;
            _iconService = iconService;
            _validator = validator;

            LoadConfig();
        }

        private void LoadConfig()
        {
            var config = _configService.Load(ConfigPaths.ConfigFilePath);
            InputsPanel.Children.Clear();

            if (config?.Files.Count > 0)
            {
                for (var i = 0; i < config.Files.Count; i++)
                {
                    var tb = CreateControl(config.Files[i], config.Icons[i]);
                    InputsPanel.Children.Add(tb);
                }
            }
            else
            {
                // Empezar con 3 controles vacíos
                for (var i = 0; i < 3; i++)
                    InputsPanel.Children.Add(CreateControl());
            }
        }

        private ClearTextBox CreateControl(string fileExt = "", string iconPath = "")
        {
            var tb = new ClearTextBox();
            tb.MyTextBox.Text = fileExt;
            tb.IconInput.Text = iconPath;
            tb.RequestRemove += (s, e) => InputsPanel.Children.Remove(tb);
            return tb;
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            InputsPanel.Children.Add(CreateControl());
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            var config = new ConfigData();

            foreach (ClearTextBox tb in InputsPanel.Children)
            {
                var ext = tb.MyTextBox.Text;
                var icon = tb.IconInput.Text;

                if (!_validator.IsValidExtension(ext) || !File.Exists(icon))
                    continue;

                config.Files.Add(ext);
                config.Icons.Add(icon);
            }

            _configService.Save(config, ConfigPaths.ConfigFilePath);
            _iconService.ApplyIcons(config);
        }

        private void ResetConfigButton_OnClick(object sender, RoutedEventArgs e)
        {
            _iconService.CleanDesktop();
        }

        private void BtButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void LoadIcons_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Select Icon Images",
                Filter = "Supported Images|*.png;*.jpg;*.jpeg;*.bmp;*.ico;",
                Multiselect = true
            };
            if (openDialog.ShowDialog() == true)
            {
                foreach (var filename in openDialog.FileNames)
                {
                    Debug.WriteLine(filename);
                    if (!filename.Contains(".ico"))
                    {
                        InputsPanel.Children.Add(CreateControl("", ConvertToIco(filename)));
                    } else
                    {
                        InputsPanel.Children.Add(CreateControl("", filename));
                    }
                }
            }
        }

        string ConvertToIco(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                MessageBox.Show("File not found: " + imagePath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
            if (!Directory.Exists(ConfigPaths.IconsFolder))
            {
                Directory.CreateDirectory(ConfigPaths.IconsFolder);
            }
            string baseName = Path.GetFileNameWithoutExtension(imagePath);
            string uniqueName = $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}.ico";

            string outputIconPath = Path.Combine(ConfigPaths.IconsFolder, uniqueName);

            using (var originalBmp = new Bitmap(imagePath))
            {
                using (var resizedBmp = new Bitmap(originalBmp, new System.Drawing.Size(256, 256)))
                {
                    using (var fs = new FileStream(outputIconPath, FileMode.Create))
                    using (var writer = new BinaryWriter(fs))
                    {
                        using (var msPng = new MemoryStream())
                        {
                            resizedBmp.Save(msPng, ImageFormat.Png);
                            byte[] pngBytes = msPng.ToArray();

                            writer.Write((short)0);
                            writer.Write((short)1);
                            writer.Write((short)1);

                            writer.Write((byte)(resizedBmp.Width >= 256 ? 0 : resizedBmp.Width));
                            writer.Write((byte)(resizedBmp.Height >= 256 ? 0 : resizedBmp.Height));
                            writer.Write((byte)0);
                            writer.Write((byte)0);
                            writer.Write((short)1);
                            writer.Write((short)32);

                            writer.Write(pngBytes.Length);

                            writer.Write(22);

                            writer.Write(pngBytes);
                        }
                    }
                }
            }
            return outputIconPath;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
