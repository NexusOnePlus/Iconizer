using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Iconizer.Presentation.View.UserControls;

public partial class ClearTextBox : UserControl
{
    public event EventHandler? RequestRemove;
    public ClearTextBox()
    {
        InitializeComponent();
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {   
        RequestRemove?.Invoke(this, EventArgs.Empty);
    }

    private void IconInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        var ruta = IconInput.Text?.Trim();
        if (string.IsNullOrEmpty(ruta))
        {
            IconPreview.Source = null;
            return;
        }

        try
        {
            if (File.Exists(ruta))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(ruta, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                IconPreview.Source = bmp;
            }
            else
            {
                IconPreview.Source = null;
            }
        }
        catch
        {
            IconPreview.Source = null;
        }
    }
}